using System;
using System.Collections.Generic;
using System.Text;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;

namespace CavesOfOoo.Concerns
{
    /// <summary>
    /// C2 (Docs/PLACE-WORK-AND-PROGRESSION.md §2): concerns in conversation, without
    /// touching any authored dialogue tree. On an authored start node, the owner of a
    /// concern in its own place offers it, asks after it while it is open, and hears it
    /// reported when done; a place's Elder answers "What troubles this place?". Choosing
    /// one of these runs <see cref="OpenAction"/>, which swaps the current node for a
    /// synthetic one (ids prefixed <see cref="NodePrefix"/>) that this class builds;
    /// "Back" returns to the authored Start node.
    ///
    /// Every no is spoken: refusing an offer undertakes the act and ends it Refused at
    /// once, so the closure ledger records an enacted refusal (Lore/11_SecondSpine.md C4).
    /// Diag: concern/Offered, Accepted, Refused, Reported, OfferTaken, SupplyHanded,
    /// CargoIssued, CargoDelivered.
    /// </summary>
    public static class ConcernConversation
    {
        public const string OpenAction = "OpenConcernNode";
        public const string AcceptAction = "AcceptConcern";
        public const string RefuseAction = "RefuseConcern";
        public const string ReportAction = "ReportConcern";
        public const string OfferAction = "TakeConcernOffer";
        public const string SupplyAction = "HandConcernSupply";
        public const string IssueCargoAction = "IssueConcernCargo";
        public const string DeliverCargoAction = "DeliverConcernCargo";
        public const string RepairAction = "RepairPlaceSite";
        public const string NodePrefix = "concern:";
        public const string ElderRole = "Elder";

        public enum State { Hidden, Offerable, Working, Reportable, Done }

        public static State StateOf(ConcernData c, Entity speaker, Entity listener)
        {
            var sp = StoryletPart.Current;
            if (c == null || sp == null) return State.Hidden;
            string q = ConcernRegistry.QuestIdFor(c.Id);
            if (sp.IsQuestCompleted(q)) return State.Done;
            var state = sp.GetQuestState(q);
            if (state != null && sp.IsQuestActive(q)) return state.CurrentStageIndex >= 1 ? State.Reportable : State.Working;
            var closure = sp.GetClosure(q);
            if (closure != null && closure.State == ClosureState.Refused) return State.Hidden;
            if (sp.IsQuestFailed(q)) return State.Hidden;
            foreach (var before in ConcernRegistry.Predecessors(c.Id))
                if (!sp.IsQuestCompleted(ConcernRegistry.QuestIdFor(before.Id))) return State.Hidden;
            return ConversationPredicates.CheckAll(c.Requires, speaker, listener) ? State.Offerable : State.Hidden;
        }

        static string PlaceOf(Entity speaker)
        {
            string zoneId = speaker?.SpatialZone?.ZoneID;
            if (zoneId == null) return null;
            foreach (var p in WorldMapAuthoring.Places)
                if (WorldMap.ToZoneID(p.X, p.Y, 0) == zoneId) return p.Name;
            return null;
        }

        static bool OwnedBy(ConcernData c, Entity speaker, string place)
            => c.Place == place && speaker.BlueprintName == c.Owner;

        /// <summary>Called by ConversationManager.RefreshVisibleChoices.</summary>
        public static void AppendChoices(List<ChoiceData> choices)
        {
            var speaker = ConversationManager.Speaker; var listener = ConversationManager.Listener;
            var node = ConversationManager.CurrentNode; var conversation = ConversationManager.CurrentConversation;
            if (choices == null || speaker == null || listener == null || node == null || conversation == null) return;
            if (node.ID != null && node.ID.StartsWith(NodePrefix, StringComparison.Ordinal)) return;
            if (!ReferenceEquals(node, conversation.GetStartNode())) return;
            string place = PlaceOf(speaker);
            if (place == null) return;
            bool anyOpen = false;
            foreach (var c in ConcernRegistry.All)
            {
                if (c.Place != place) continue;
                var state = StateOf(c, speaker, listener);
                if (state == State.Offerable || state == State.Working || state == State.Reportable) anyOpen = true;
                if (!OwnedBy(c, speaker, place)) continue;
                string text = state == State.Offerable ? "[Is something wrong here?]"
                    : state == State.Working ? "[About " + c.Title.ToLowerInvariant() + "...]"
                    : state == State.Reportable ? "[It's done: " + c.Title.ToLowerInvariant() + ".]" : null;
                if (text != null) choices.Add(Open(text, (state == State.Reportable ? "report|" : state == State.Working ? "status|" : "pitch|") + c.Id));
            }
            if (anyOpen && speaker.BlueprintName == ElderRole) choices.Add(Open("[What troubles this place?]", "list|" + place));
            AppendDeliveries(choices, speaker, listener, place);
        }

        /// <summary>The named recipient, in the named place, takes sealed cargo the player
        /// actually carries for a concern that is still being worked.</summary>
        static void AppendDeliveries(List<ChoiceData> choices, Entity speaker, Entity listener, string place)
        {
            foreach (var c in ConcernRegistry.All)
            {
                if (StateOf(c, speaker, listener) != State.Working) continue;
                foreach (var a in c.Approaches)
                {
                    if (!a.HasCarry || a.Carry.Place != place || a.Carry.Recipient != speaker.BlueprintName) continue;
                    if (IsDone(c, a)) continue;
                    if (ConcernItems.FindCargo(listener, a.Carry.Item, ConcernItems.CargoKey(c.Id, a.Id)) == null) continue;
                    choices.Add(new ChoiceData { Text = "[" + a.Carry.Handover + "]", Target = "",
                        Actions = new List<ConversationParam> { new ConversationParam { Key = DeliverCargoAction, Value = c.Id + "|" + a.Id } } });
                }
            }
        }

        static bool IsDone(ConcernData c, ConcernApproachData a)
            => (NarrativeStatePart.Current?.GetFact(ConcernRegistry.DoneFact(c.Id, a.Id)) ?? 0) > 0;

        static ChoiceData Open(string text, string key) => new ChoiceData
        { Text = text, Target = "", Actions = new List<ConversationParam> { new ConversationParam { Key = OpenAction, Value = key } } };
        static ChoiceData Back() => new ChoiceData { Text = "[Back]", Target = "Start" };

        /// <summary>The OpenConcernNode action: replace the current node with a synthetic one.</summary>
        public static string Open(Entity speaker, Entity listener, string key)
        {
            var node = BuildNode(key, speaker, listener);
            if (node == null) return "unknown_node";
            ConversationManager.CurrentNode = node;
            return null;
        }

        public static NodeData BuildNode(string key, Entity speaker, Entity listener)
        {
            var parts = key?.Split(new[] { '|' }, 2);
            if (parts == null || parts.Length != 2) return null;
            if (parts[0] == "list") return ListNode(parts[1], speaker, listener);
            var c = ConcernRegistry.Get(parts[1]);
            if (c == null) return null;
            switch (parts[0])
            {
                case "pitch": return PitchNode(c, speaker, listener);
                case "status": return StatusNode(c, speaker, listener);
                case "report": return ReportNode(c);
                default: return null;
            }
        }

        static NodeData Node(string id, string text) => new NodeData { ID = NodePrefix + id, Text = text };

        static NodeData PitchNode(ConcernData c, Entity speaker, Entity listener)
        {
            Record("Offered", speaker, c, null);
            var text = new StringBuilder(c.Pitch);
            foreach (var a in c.Approaches) text.Append("\n\n- ").Append(a.Label);
            var n = Node("pitch|" + c.Id, text.ToString());
            n.Choices.Add(new ChoiceData { Text = "[I'll see to it.]", Target = "Start",
                Actions = new List<ConversationParam> { new ConversationParam { Key = AcceptAction, Value = c.Id } } });
            n.Choices.Add(new ChoiceData { Text = "[" + c.Refusal.Text + "]", Target = "",
                Actions = new List<ConversationParam> { new ConversationParam { Key = RefuseAction, Value = c.Id } } });
            n.Choices.Add(Back());
            return n;
        }

        static NodeData StatusNode(ConcernData c, Entity speaker, Entity listener)
        {
            var text = new StringBuilder("\"Any word on it?\"\n");
            var quest = StoryletPart.Current?.GetQuestState(ConcernRegistry.QuestIdFor(c.Id));
            foreach (var a in c.Approaches)
            {
                text.Append("\n").Append(quest != null && quest.FinishedObjectives.Contains(a.Id) ? "[x] " : "[ ] ").Append(a.Objective);
                if (a.HasConfront && ConcernWorld.SiteOf(c.Id, a.Id) != null && !string.IsNullOrEmpty(a.Confront.Site))
                    text.Append(" - ").Append(a.Confront.Site);
                if (a.HasRestore && PlaceSiteCatalog.Get(c.Place, a.Restore.Site) is PlaceSiteData site)
                    text.Append(" (needs ").Append(Needs(site.Lasting)).Append(")");
                if (a.HasSupply && !IsDone(c, a))
                    text.Append(" (you carry ").Append(ConcernItems.CountCarried(listener, a.Supply.Blueprint)).Append(" of ").Append(a.Supply.Count).Append(")");
            }
            var n = Node("status|" + c.Id, text.ToString());
            foreach (var a in c.Approaches)
            {
                if (IsDone(c, a)) continue;
                if (a.HasRestore && PlaceSiteCatalog.Get(c.Place, a.Restore.Site) is PlaceSiteData restore
                    && PlaceSites.Site(c.Place, restore.Id) is RepairableSiteState state && state.Stage < RepairStage.StableRepair)
                {
                    if (CanFix(listener, restore.Lasting))
                        n.Choices.Add(new ChoiceData { Text = "[Repair the " + restore.Name + ".]", Target = "",
                            Actions = new List<ConversationParam> { new ConversationParam { Key = RepairAction, Value = c.Place + ":" + restore.Id + ":Lasting" } } });
                    if (restore.HasPatch && state.Stage == RepairStage.Fouled && CanFix(listener, restore.Patch))
                        n.Choices.Add(new ChoiceData { Text = "[Patch the " + restore.Name + " for now.]", Target = "",
                            Actions = new List<ConversationParam> { new ConversationParam { Key = RepairAction, Value = c.Place + ":" + restore.Id + ":Patch" } } });
                }
                if (a.HasSupply && ConcernItems.CountCarried(listener, a.Supply.Blueprint) >= a.Supply.Count)
                    n.Choices.Add(new ChoiceData { Text = "[Hand over " + a.Supply.Count + " " + UnitName(listener, a.Supply.Blueprint) + ".]", Target = "",
                        Actions = new List<ConversationParam> { new ConversationParam { Key = SupplyAction, Value = c.Id + "|" + a.Id } } });
                if (a.HasCarry && ConcernItems.FindCargo(listener, a.Carry.Item, ConcernItems.CargoKey(c.Id, a.Id)) == null)
                    n.Choices.Add(new ChoiceData { Text = "[I'll carry " + a.Carry.ItemName + " to the " + SplitWords(a.Carry.Recipient).ToLowerInvariant() + " at " + a.Carry.Place + ".]", Target = "",
                        Actions = new List<ConversationParam> { new ConversationParam { Key = IssueCargoAction, Value = c.Id + "|" + a.Id } } });
            }
            foreach (var a in c.Approaches)
                if (a.Offer != null && a.Offer.Count > 0 && !string.IsNullOrEmpty(a.OfferText))
                    n.Choices.Add(new ChoiceData { Text = "[" + a.OfferText + "]", Target = "Start",
                        Actions = new List<ConversationParam> { new ConversationParam { Key = OfferAction, Value = c.Id + "|" + a.Id } } });
            n.Choices.Add(new ChoiceData { Text = "[I'm giving this up. I'm telling you so.]", Target = "",
                Actions = new List<ConversationParam> { new ConversationParam { Key = RefuseAction, Value = c.Id } } });
            n.Choices.Add(Back());
            return n;
        }

        static NodeData ReportNode(ConcernData c)
        {
            var n = Node("report|" + c.Id, "\"" + c.Thanks + "\"");
            n.Choices.Add(new ChoiceData { Text = "[Glad to help.]", Target = "Start",
                Actions = new List<ConversationParam> { new ConversationParam { Key = ReportAction, Value = c.Id } } });
            return n;
        }

        static NodeData ListNode(string place, Entity speaker, Entity listener)
        {
            var text = new StringBuilder("\"Troubles? Here's what people are carrying:\"\n");
            int count = 0;
            foreach (var c in ConcernRegistry.All)
            {
                if (c.Place != place) continue;
                var state = StateOf(c, speaker, listener);
                if (state != State.Offerable && state != State.Working && state != State.Reportable) continue;
                count++;
                text.Append("\n- ").Append(c.Title).Append(" (ask the ").Append(OwnerName(c)).Append(")")
                    .Append(state == State.Working ? " - you are on it" : state == State.Reportable ? " - waiting to hear from you" : "");
            }
            if (count == 0) text.Append("\nNothing anyone's said out loud.");
            var n = Node("list|" + place, text.ToString());
            n.Choices.Add(Back());
            return n;
        }

        static string OwnerName(ConcernData c) => string.IsNullOrEmpty(c.Owner) ? "keeper" : SplitWords(c.Owner).ToLowerInvariant();
        static string SplitWords(string s)
        {
            var b = new StringBuilder();
            for (int i = 0; i < s.Length; i++) { if (i > 0 && char.IsUpper(s[i])) b.Append(' '); b.Append(s[i]); }
            return b.ToString();
        }

        // ── actions ──────────────────────────────────────────────────────────

        public static string Accept(Entity speaker, Entity listener, string id)
        {
            var c = ConcernRegistry.Get(id); var sp = StoryletPart.Current;
            if (c == null || sp == null) return "unknown_concern";
            if (StateOf(c, speaker, listener) != State.Offerable) return "not_offerable";
            string q = ConcernRegistry.QuestIdFor(id);
            sp.StartQuest(new QuestState { QuestId = q });
            sp.SetGiver(q, speaker, speaker?.SpatialZone);
            Record("Accepted", speaker, c, null);
            ConcernWorld.ResolveSites(c, speaker?.SpatialZone);
            // A document to read in the place the player is standing in appears now, not on
            // re-entry. Foes are placed only on a real arrival.
            ConcernWorld.OnZoneEntered(listener, listener?.SpatialZone, arriving: false);
            return null;
        }

        public static string Refuse(Entity speaker, Entity listener, string id)
        {
            var c = ConcernRegistry.Get(id); var sp = StoryletPart.Current;
            if (c == null || sp == null) return "unknown_concern";
            var state = StateOf(c, speaker, listener);
            if (state != State.Offerable && state != State.Working && state != State.Reportable) return "nothing_to_refuse";
            string q = ConcernRegistry.QuestIdFor(id);
            if (state == State.Offerable) { sp.StartQuest(new QuestState { QuestId = q }); sp.SetGiver(q, speaker, speaker?.SpatialZone); }
            if (!sp.RefuseQuest(q, listener)) return "refuse_failed";
            ConversationActions.ExecuteAll(c.Refusal.Effects, speaker, listener);
            var n = Node("refused|" + id, "\"" + c.Refusal.Reply + "\"");
            n.Choices.Add(Back());
            ConversationManager.CurrentNode = n;
            Record("Refused", speaker, c, state.ToString());
            return null;
        }

        public static string Report(Entity speaker, Entity listener, string id)
        {
            var c = ConcernRegistry.Get(id); var sp = StoryletPart.Current;
            if (c == null || sp == null) return "unknown_concern";
            if (StateOf(c, speaker, listener) != State.Reportable) return "not_reportable";
            // Complete first and pay only on success: a reward must never be paid for a
            // concern the journal still shows open.
            NarrativeStatePart.Current?.SetFact(ConcernRegistry.ReportedFact(id), 1);
            if (!sp.CompleteQuest(ConcernRegistry.QuestIdFor(id), listener))
            {
                NarrativeStatePart.Current?.SetFact(ConcernRegistry.ReportedFact(id), 0);
                return "complete_failed";
            }
            var reward = new List<ConversationParam>();
            if (c.Reward.XP > 0) reward.Add(new ConversationParam { Key = "AwardXP", Value = c.Reward.XP.ToString() });
            if (c.Reward.Drams > 0) reward.Add(new ConversationParam { Key = "GiveDrams", Value = c.Reward.Drams.ToString() });
            reward.AddRange(c.Reward.Effects);
            ConversationActions.ExecuteAll(reward, speaker, listener);
            Record("Reported", speaker, c, null);
            return null;
        }

        public static string TakeOffer(Entity speaker, Entity listener, string arg)
        {
            var parts = arg?.Split('|');
            var c = parts != null && parts.Length == 2 ? ConcernRegistry.Get(parts[0]) : null;
            if (c == null) return "unknown_concern";
            if (StateOf(c, speaker, listener) != State.Working) return "not_working";
            var a = c.Approaches.Find(x => x.Id == parts[1]);
            if (a == null || a.Offer == null || a.Offer.Count == 0) return "no_offer";
            if (!ConversationActions.TryExecuteAll(a.Offer, speaker, listener)) return "offer_refused";
            Record("OfferTaken", speaker, c, a.Id);
            return null;
        }

        static string Needs(PlaceSiteFixData fix)
        {
            var b = new StringBuilder();
            foreach (var m in fix.Materials)
            {
                if (b.Length > 0) b.Append(", ");
                b.Append(m.Count).Append(' ').Append(SplitWords(m.Blueprint).ToLowerInvariant());
            }
            if (!string.IsNullOrEmpty(fix.Tool)) b.Append("; with a ").Append(SplitWords(fix.Tool).ToLowerInvariant());
            return b.ToString();
        }

        static bool CanFix(Entity actor, PlaceSiteFixData fix)
        {
            foreach (var m in fix.Materials) if (ConcernItems.CountCarried(actor, m.Blueprint) < m.Count) return false;
            return string.IsNullOrEmpty(fix.Tool) || ConcernItems.CountCarried(actor, fix.Tool) > 0;
        }

        static string UnitName(Entity actor, string blueprint)
        {
            foreach (var item in actor.GetPart<InventoryPart>().Objects)
                if (item != null && item.BlueprintName == blueprint) return InventoryPart.GetUnitDisplayName(item);
            return SplitWords(blueprint).ToLowerInvariant();
        }

        static (ConcernData c, ConcernApproachData a) Resolve(string arg)
        {
            var parts = arg?.Split('|');
            var c = parts != null && parts.Length == 2 ? ConcernRegistry.Get(parts[0]) : null;
            return (c, c?.Approaches.Find(x => x.Id == parts[1]));
        }

        /// <summary>HandConcernSupply "concern|approach": all of the count or nothing.</summary>
        public static string HandSupply(Entity speaker, Entity listener, string arg)
        {
            var (c, a) = Resolve(arg);
            if (c == null) return "unknown_concern";
            if (a == null || !a.HasSupply) return "no_supply";
            if (StateOf(c, speaker, listener) != State.Working) return "not_working";
            if (IsDone(c, a)) return "already_done";
            if (!ConcernItems.TakeCarried(listener, a.Supply.Blueprint, a.Supply.Count)) return "short";
            NarrativeStatePart.Current?.SetFact(ConcernRegistry.DoneFact(c.Id, a.Id), 1);
            MessageLog.Add("You hand over " + a.Supply.Count + " " + SplitWords(a.Supply.Blueprint).ToLowerInvariant() + ".");
            Record("SupplyHanded", speaker, c, a.Id);
            ConversationManager.CurrentNode = StatusNode(c, speaker, listener);
            return null;
        }

        /// <summary>IssueConcernCargo "concern|approach": the owner hands over the sealed item,
        /// named and tagged for this concern; again only if the last one is lost.</summary>
        public static string IssueCargo(Entity speaker, Entity listener, string arg)
        {
            var (c, a) = Resolve(arg);
            if (c == null) return "unknown_concern";
            if (a == null || !a.HasCarry) return "no_cargo";
            if (StateOf(c, speaker, listener) != State.Working) return "not_working";
            if (IsDone(c, a)) return "already_done";
            string key = ConcernItems.CargoKey(c.Id, a.Id);
            if (ConcernItems.FindCargo(listener, a.Carry.Item, key) != null) return "already_carried";
            var item = ConversationActions.Factory?.CreateEntity(a.Carry.Item);
            if (item == null) return "missing_factory_or_blueprint";
            item.Properties[ConcernItems.CargoProperty] = key;
            var render = item.GetPart<RenderPart>(); if (render != null) render.DisplayName = a.Carry.ItemName;
            var inventory = listener?.GetPart<InventoryPart>();
            if (inventory == null || !inventory.AddObject(item)) return "cannot_carry";
            MessageLog.Add("You take " + a.Carry.ItemName + ".");
            Record("CargoIssued", speaker, c, a.Id);
            ConversationManager.CurrentNode = StatusNode(c, speaker, listener);
            return null;
        }

        /// <summary>DeliverConcernCargo "concern|approach": the recipient takes it and answers.</summary>
        public static string DeliverCargo(Entity speaker, Entity listener, string arg)
        {
            var (c, a) = Resolve(arg);
            if (c == null) return "unknown_concern";
            if (a == null || !a.HasCarry) return "no_cargo";
            if (StateOf(c, speaker, listener) != State.Working) return "not_working";
            if (speaker?.BlueprintName != a.Carry.Recipient || PlaceOf(speaker) != a.Carry.Place) return "wrong_recipient";
            var item = ConcernItems.FindCargo(listener, a.Carry.Item, ConcernItems.CargoKey(c.Id, a.Id));
            if (item == null) return "not_carried";
            if (!listener.GetPart<InventoryPart>().RemoveObject(item)) return "cannot_hand_over";
            NarrativeStatePart.Current?.SetFact(ConcernRegistry.DoneFact(c.Id, a.Id), 1);
            var n = Node("delivered|" + c.Id + "|" + a.Id, "\"" + a.Carry.Reply + "\"");
            n.Choices.Add(Back());
            ConversationManager.CurrentNode = n;
            Record("CargoDelivered", speaker, c, a.Id);
            return null;
        }

        static void Record(string kind, Entity speaker, ConcernData c, string detail)
        {
            if (!Diagnostics.Diag.IsChannelEnabled("concern")) return;
            Diagnostics.Diag.Record(category: "concern", kind: kind, actor: speaker,
                payload: new { concern = c.Id, place = c.Place, owner = c.Owner, detail });
        }
    }
}
