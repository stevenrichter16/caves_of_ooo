using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Registry of conversation action functions.
    /// Each action takes (speaker, listener, argument) and performs a side effect.
    /// Actions are executed when a player selects a dialogue choice.
    /// </summary>
    public static class ConversationActions
    {
        public delegate void ActionFunc(Entity speaker, Entity listener, string argument);

        private static Dictionary<string, ActionFunc> _actions
            = new Dictionary<string, ActionFunc>();

        private static bool _initialized;

        /// <summary>
        /// Optional: EntityFactory for GiveItem action. Set by GameBootstrap.
        /// </summary>
        public static Data.EntityFactory Factory;

        public static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            RegisterDefaults();
        }

        public static void Register(string name, ActionFunc func)
        {
            _actions[name] = func;
        }

        /// <summary>
        /// Returns true if an action with this name is registered. Used by
        /// content loaders (e.g. StoryletRegistry) to fail-fast on unknown
        /// action names at load time, since Execute() silently no-ops with
        /// a warning for unknown names.
        /// </summary>
        public static bool IsRegistered(string name)
        {
            EnsureInitialized();
            return !string.IsNullOrEmpty(name) && _actions.ContainsKey(name);
        }

        public static void Execute(string name, Entity speaker, Entity listener, string argument)
        {
            EnsureInitialized();

            if (_actions.TryGetValue(name, out var func))
            {
                func(speaker, listener, argument);
                return;
            }

            Debug.LogWarning($"[Conversation] Unknown action: '{name}'");
        }

        /// <summary>
        /// Execute all actions on a choice.
        /// </summary>
        public static void ExecuteAll(List<Data.ConversationParam> actions,
            Entity speaker, Entity listener)
        {
            if (actions == null) return;
            for (int i = 0; i < actions.Count; i++)
            {
                Execute(actions[i].Key, speaker, listener, actions[i].Value);
            }
        }

        private static void RegisterDefaults()
        {
            // Add a message to the game log
            Register("AddMessage", (speaker, listener, arg) =>
            {
                MessageLog.Add(arg);
            });

            // Set a tag on the listener (player): "TagName" or "TagName:Value"
            Register("SetTag", (speaker, listener, arg) =>
            {
                if (listener == null) return;
                int colon = arg.IndexOf(':');
                if (colon >= 0)
                    listener.SetTag(arg.Substring(0, colon), arg.Substring(colon + 1));
                else
                    listener.SetTag(arg, "");
            });

            // Set a property on the listener (player): "PropName:Value"
            Register("SetProperty", (speaker, listener, arg) =>
            {
                if (listener == null) return;
                int colon = arg.IndexOf(':');
                if (colon >= 0)
                    listener.Properties[arg.Substring(0, colon)] = arg.Substring(colon + 1);
                else
                    listener.Properties[arg] = "";
            });

            // Set a property on the speaker: "PropName:Value"
            Register("SetSpeakerProperty", (speaker, listener, arg) =>
            {
                if (speaker == null) return;
                int colon = arg.IndexOf(':');
                if (colon >= 0)
                    speaker.Properties[arg.Substring(0, colon)] = arg.Substring(colon + 1);
                else
                    speaker.Properties[arg] = "";
            });

            // Set an int property on the listener: "PropName:IntValue"
            Register("SetIntProperty", (speaker, listener, arg) =>
            {
                if (listener == null) return;
                int colon = arg.IndexOf(':');
                if (colon < 0) return;
                string key = arg.Substring(0, colon);
                if (int.TryParse(arg.Substring(colon + 1), out int val))
                    listener.SetIntProperty(key, val);
            });

            // Give an item to the listener (player) by blueprint name
            Register("GiveItem", (speaker, listener, arg) =>
            {
                if (listener == null || Factory == null) return;
                var item = Factory.CreateEntity(arg);
                if (item == null)
                {
                    Debug.LogWarning($"[Conversation] GiveItem: blueprint '{arg}' not found.");
                    return;
                }
                var inv = listener.GetPart<InventoryPart>();
                if (inv != null)
                {
                    inv.AddObject(item);
                    MessageLog.Add($"You receive {item.GetDisplayName()}.");
                }
            });

            // Take an item from the listener (player) by blueprint name
            Register("TakeItem", (speaker, listener, arg) =>
            {
                if (listener == null) return;
                var inv = listener.GetPart<InventoryPart>();
                if (inv == null) return;
                for (int i = 0; i < inv.Objects.Count; i++)
                {
                    if (inv.Objects[i].BlueprintName == arg)
                    {
                        var item = inv.Objects[i];
                        inv.RemoveObject(item);
                        MessageLog.Add($"You hand over {item.GetDisplayName()}.");
                        return;
                    }
                }
            });

            // Open the trade screen after conversation ends
            Register("StartTrade", (speaker, listener, arg) =>
            {
                ConversationManager.PendingTradePartner = speaker;
            });

            // Initiate an attack on the NPC after conversation ends
            Register("StartAttack", (speaker, listener, arg) =>
            {
                ConversationManager.PendingAttackTarget = speaker;
            });

            // Change faction feeling: "FactionA:FactionB:Delta"
            // When one side is "Player", routes through PlayerReputation.
            Register("ChangeFactionFeeling", (speaker, listener, arg) =>
            {
                var parts = arg.Split(':');
                if (parts.Length < 3) return;
                if (!int.TryParse(parts[2], out int delta)) return;

                if (parts[1] == "Player")
                {
                    // "RotChoir:Player:5" → modify player rep with RotChoir
                    PlayerReputation.Modify(parts[0], delta);
                }
                else if (parts[0] == "Player")
                {
                    // "Player:RotChoir:5" → modify player rep with RotChoir
                    PlayerReputation.Modify(parts[1], delta);
                }
                else
                {
                    // NPC-to-NPC faction feeling
                    int current = FactionManager.GetFactionFeeling(parts[0], parts[1]);
                    FactionManager.SetFactionFeeling(parts[0], parts[1], current + delta);
                    if (delta > 0)
                        MessageLog.Add($"Relations between {FactionManager.GetDisplayName(parts[0])} and {FactionManager.GetDisplayName(parts[1])} improve.");
                    else if (delta < 0)
                        MessageLog.Add($"Relations between {FactionManager.GetDisplayName(parts[0])} and {FactionManager.GetDisplayName(parts[1])} worsen.");
                }
            });

            Register("ResolveSettlementSite", (speaker, listener, arg) =>
            {
                if (speaker == null || listener == null || string.IsNullOrWhiteSpace(arg) || SettlementManager.Current == null)
                    return;

                string[] parts = arg.Split(':');
                if (parts.Length != 2)
                    return;

                string settlementId = ResolveSettlementId(speaker);
                if (string.IsNullOrEmpty(settlementId))
                    return;

                RepairMethodId method;
                if (!Enum.TryParse(parts[1], out method))
                    return;

                if (SettlementManager.Current.ApplyRepairMethod(settlementId, parts[0], method, listener))
                {
                    SettlementManager.Current.RefreshActiveZonePresentation(SettlementRuntime.ActiveZone);
                    SettlementRuntime.MarkZoneDirty();
                }
            });

            // Copy the first grimoire in the player's inventory, producing a GrimoireCopy
            Register("CopyGrimoire", (speaker, listener, arg) =>
            {
                if (listener == null || Factory == null) return;

                var inv = listener.GetPart<InventoryPart>();
                if (inv == null) return;

                Entity grimoire = null;
                for (int i = 0; i < inv.Objects.Count; i++)
                {
                    if (inv.Objects[i].HasTag("Grimoire") && !inv.Objects[i].HasTag("GrimoireCopy"))
                    {
                        grimoire = inv.Objects[i];
                        break;
                    }
                }

                if (grimoire == null)
                {
                    MessageLog.Add("You don't have a grimoire to copy.");
                    return;
                }

                var grimoirePart = grimoire.GetPart<GrimoirePart>();
                if (grimoirePart == null) return;

                Entity copy = Factory.CreateEntity("GrimoireCopy");
                if (copy == null) return;

                var copyPart = copy.GetPart<GrimoirePart>();
                if (copyPart != null)
                {
                    copyPart.KnowledgeProperty = grimoirePart.KnowledgeProperty;
                    copyPart.LearnMessage = grimoirePart.LearnMessage;
                    copyPart.AlreadyKnownMessage = grimoirePart.AlreadyKnownMessage;
                }

                var copyRender = copy.GetPart<RenderPart>();
                var origRender = grimoire.GetPart<RenderPart>();
                if (copyRender != null && origRender != null)
                    copyRender.DisplayName = "copy of " + origRender.DisplayName;

                inv.AddObject(copy);
                MessageLog.AddAnnouncement("The scribe carefully copies the grimoire. You receive the copy.");
            });

            // Remove the first item matching a tag from the player's inventory
            Register("TakeItemWithTag", (speaker, listener, arg) =>
            {
                if (listener == null || string.IsNullOrEmpty(arg)) return;
                var inv = listener.GetPart<InventoryPart>();
                if (inv == null) return;
                for (int i = 0; i < inv.Objects.Count; i++)
                {
                    if (inv.Objects[i].HasTag(arg))
                    {
                        var item = inv.Objects[i];
                        inv.RemoveObject(item);
                        MessageLog.Add($"You hand over {item.GetDisplayName()}.");
                        return;
                    }
                }
            });

            // M2.1: Pacify the speaker (the NPC) for N turns — e.g., a
            // Charisma-gated "Stand down" dialogue branch can non-violently
            // resolve a combat scenario. Argument is an integer duration in
            // turns; default 100 when empty/invalid. Idempotent: if a
            // NoFightGoal is already present on the speaker, this is a
            // no-op (no stacking, no duration extension) so chained calls
            // can't accidentally reset an ongoing pacification.
            //
            // Note: NoFightGoal suppresses AIBoredEvent on the pacified
            // entity, so AISelfPreservation won't push RetreatGoal while the
            // NPC is calmed. See NoFightGoal's xml-doc for the broader
            // gotcha.
            Register("PushNoFightGoal", (speaker, listener, arg) =>
            {
                if (speaker == null) return;
                var brain = speaker.GetPart<BrainPart>();
                if (brain == null) return;

                // Idempotent: if already pacified, emit visible feedback
                // so the player sees why the "Stand down, friend" choice
                // looks like it did nothing.
                if (brain.HasGoal<NoFightGoal>())
                {
                    MessageLog.Add($"{speaker.GetDisplayName()} is already at peace.");
                    return;
                }

                // `int.TryParse` writes 0 to the out param on failure, which
                // NoFightGoal treats as INFINITE — so a typo in the dialogue
                // JSON would silently pacify the NPC forever. Guard by only
                // taking the parsed value when parse succeeds AND the value
                // is positive. Rejects "0" for the same reason (authors
                // wanting infinite should use the auto-pacify path, not
                // this action).
                int duration = 100;
                if (!string.IsNullOrEmpty(arg)
                    && int.TryParse(arg, out int parsed)
                    && parsed > 0)
                {
                    duration = parsed;
                }

                brain.PushGoal(new NoFightGoal(duration, wander: false));
                MessageLog.Add($"{speaker.GetDisplayName()} stands down.");
            });

            // ── House Drama actions ───────────────────────────────────────────

            // arg: "DramaID:PointID:NewState" or "DramaID:PointID:NewState:PathID"
            Register("AdvancePressurePoint", (speaker, listener, arg) =>
            {
                var parts = arg.Split(':');
                if (parts.Length < 3) return;
                string pathId = parts.Length >= 4 ? parts[3] : null;
                HouseDramaRuntime.AdvancePressurePoint(parts[0], parts[1], parts[2], pathId);
            });

            // arg: "DramaID:NpcID:FactID"
            Register("RevealWitnessFact", (speaker, listener, arg) =>
            {
                var parts = arg.Split(':');
                if (parts.Length < 3) return;
                HouseDramaRuntime.RevealWitnessFact(parts[0], parts[1], parts[2]);
            });

            // arg: "DramaID:Amount"
            Register("AddCorruption", (speaker, listener, arg) =>
            {
                int colon = arg.IndexOf(':');
                if (colon < 0) return;
                if (!int.TryParse(arg.Substring(colon + 1), out int amount)) return;
                HouseDramaRuntime.AddCorruption(arg.Substring(0, colon), amount);
            });

            // arg: "DramaID"
            Register("StartDrama", (speaker, listener, arg) =>
            {
                if (string.IsNullOrEmpty(arg)) return;
                if (HouseDramaRuntime.IsDramaActive(arg)) return;
                if (!HouseDramaRuntime.IsDramaRegistered(arg))
                {
                    var data = Data.HouseDramaLoader.Get(arg);
                    if (data == null) return;
                    HouseDramaRuntime.RegisterDrama(data);
                }
                HouseDramaRuntime.ActivateDrama(arg);
            });

            // arg: "DramaID:EffectString" — e.g. "Thresker:close:InheritanceHinge:SilentBargain"
            Register("TriggerCrossover", (speaker, listener, arg) =>
            {
                int colon = arg.IndexOf(':');
                if (colon < 0) return;
                string dramaId = arg.Substring(0, colon);
                string effect   = arg.Substring(colon + 1);
                HouseDramaRuntime.ApplyCrossoverEffect(dramaId, effect);
            });

            // ── Narrative state actions ───────────────────────────────────────

            // arg: "key:value"
            Register("SetFact", (speaker, listener, arg) =>
            {
                var ns = NarrativeStatePart.Current;
                if (ns == null) return;
                int colon = arg.IndexOf(':');
                if (colon < 0) return;
                if (!int.TryParse(arg.Substring(colon + 1), out int value)) return;
                ns.SetFact(arg.Substring(0, colon), value);
            });

            // arg: "key:delta"
            Register("AddFact", (speaker, listener, arg) =>
            {
                var ns = NarrativeStatePart.Current;
                if (ns == null) return;
                int colon = arg.IndexOf(':');
                if (colon < 0) return;
                if (!int.TryParse(arg.Substring(colon + 1), out int delta)) return;
                ns.AddFact(arg.Substring(0, colon), delta);
            });

            // arg: "key"
            Register("ClearFact", (speaker, listener, arg) =>
            {
                var ns = NarrativeStatePart.Current;
                if (ns == null || string.IsNullOrEmpty(arg)) return;
                ns.ClearFact(arg);
            });

            // arg: "Target:topic:tier"  Target ∈ {Listener, Speaker}
            Register("Reveal", (speaker, listener, arg) =>
            {
                var parts = arg.Split(':', 3);
                if (parts.Length < 3) return;
                if (!int.TryParse(parts[2], out int tier)) return;
                Entity target = parts[0] == "Speaker" ? speaker : listener;
                var kp = target?.GetPart<KnowledgePart>();
                kp?.Reveal(parts[1], tier);
            });

            // ── QS.3 quest-lifecycle actions ─────────────────────────────
            // Per Docs/QUEST-SYSTEM.md. All four delegate to
            // StoryletPart.Current. If the storylet system isn't
            // bootstrapped (Current == null), each action is a no-op
            // — defensive, mirrors the predicate side's behavior.

            // StartQuest(questId) — adds quest to active dict at
            // stage 0, fires stage-0 OnEnter effects immediately,
            // records quest/Started diag. Idempotent: no-op if the
            // quest is already active OR already completed (player
            // can't re-take a finished quest).
            Register("StartQuest", (speaker, listener, arg) =>
            {
                if (string.IsNullOrEmpty(arg)) return;
                var sp = CavesOfOoo.Storylets.StoryletPart.Current;
                if (sp == null) return;
                if (sp.IsQuestActive(arg)) return;
                if (sp.IsQuestCompleted(arg)) return;

                // QS.3 cold-eye fix #4+#5: validate registry membership
                // BEFORE adding to _quests. Without this, a typo in arg
                // (or a save-game referencing a since-removed quest)
                // would silently brick the slot — the QuestState would
                // be added with no matching QuestData, IfQuestActive
                // would return true forever, and AdvanceQuestStage would
                // see Stages.Count=0 and auto-complete on first advance.
                // Better: refuse to start, log a warning, leave the
                // slot free for content fixes.
                var quest = CavesOfOoo.Storylets.StoryletRegistry.FindQuest(arg);
                if (quest == null)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[Conversation] StartQuest: unknown quest id '{arg}' " +
                        $"(no matching QuestData in StoryletRegistry).");
                    return;
                }

                int currentTurn = TurnManager.Active?.TickCount ?? 0;
                var state = new CavesOfOoo.Storylets.QuestState
                {
                    QuestId = arg,
                    CurrentStageIndex = 0,
                    EnteredStageAtTurn = currentTurn,
                };
                sp.StartQuest(state);

                if (CavesOfOoo.Diagnostics.Diag.IsChannelEnabled("quest"))
                {
                    CavesOfOoo.Diagnostics.Diag.Record(
                        category: "quest", kind: "Started",
                        actor: listener, payload: new { questId = arg });
                }

                // Fire stage-0 OnEnter effects immediately so the
                // player sees the first scripted message ("deliver
                // this letter to Marceline" etc) without waiting for
                // a tick.
                if (quest.Stages != null && quest.Stages.Count > 0
                    && quest.Stages[0].OnEnter != null)
                {
                    ExecuteAll(quest.Stages[0].OnEnter, speaker, listener);
                }
            });

            // AdvanceQuestStage(questId) — bump the current stage
            // index by 1 and fire the new stage's OnEnter effects.
            // If past the terminal stage, auto-complete the quest
            // (StoryletPart.AdvanceQuestStage handles both branches +
            // diag records). No-op on quests that aren't active.
            //
            // QS.3 cold-eye fix #3: pass listener as `actor` so the
            // diag substrate records player-driven advances/completions
            // distinctly from tick-driven ones (which pass null).
            Register("AdvanceQuestStage", (speaker, listener, arg) =>
            {
                if (string.IsNullOrEmpty(arg)) return;
                var sp = CavesOfOoo.Storylets.StoryletPart.Current;
                if (sp == null) return;
                if (!sp.IsQuestActive(arg)) return;

                int currentTurn = TurnManager.Active?.TickCount ?? 0;
                int newIndex = sp.AdvanceQuestStage(arg, currentTurn, actor: listener);
                if (newIndex < 0) return;  // auto-completed

                // Fire OnEnter for the NEW stage.
                var quest = CavesOfOoo.Storylets.StoryletRegistry.FindQuest(arg);
                if (quest != null && newIndex < quest.Stages.Count
                    && quest.Stages[newIndex].OnEnter != null)
                {
                    ExecuteAll(quest.Stages[newIndex].OnEnter, speaker, listener);
                }
            });

            // CompleteQuest(questId) — explicit completion (instead
            // of letting AdvanceQuestStage auto-complete at the
            // terminal stage). Useful when content wants to short-
            // circuit a quest (e.g., player chooses an "abandon"
            // branch but the system tracks it as completed).
            //
            // QS.3 cold-eye fix #1: delegates to the centralized
            // StoryletPart.CompleteQuest helper so the quest/Completed
            // diag is fired from ONE place — same as the
            // AdvanceQuestStage auto-complete branch. Pre-fix the
            // action duplicated the helper's payload shape; a future
            // payload-shape change would have needed updates in both.
            Register("CompleteQuest", (speaker, listener, arg) =>
            {
                if (string.IsNullOrEmpty(arg)) return;
                var sp = CavesOfOoo.Storylets.StoryletPart.Current;
                if (sp == null) return;
                sp.CompleteQuest(arg, actor: listener);
            });

            // FailQuest(questId) — remove from active set without
            // recording in completed. v1: failed quests can be
            // retaken (the predicate side returns IfQuestNotStarted
            // = true again). If playtest reveals the player needs
            // "you already failed this" feedback, add a separate
            // _failedQuests HashSet in a follow-on. Documented as
            // 🟡 in Docs/QUEST-SYSTEM.md.
            Register("FailQuest", (speaker, listener, arg) =>
            {
                if (string.IsNullOrEmpty(arg)) return;
                var sp = CavesOfOoo.Storylets.StoryletPart.Current;
                if (sp == null) return;
                if (!sp.IsQuestActive(arg)) return;

                sp.RemoveActiveQuest(arg);

                if (CavesOfOoo.Diagnostics.Diag.IsChannelEnabled("quest"))
                {
                    CavesOfOoo.Diagnostics.Diag.Record(
                        category: "quest", kind: "Failed",
                        actor: listener, payload: new { questId = arg });
                }
            });

            // ── QS.5 reward actions ──────────────────────────────────────
            // Per Docs/QUEST-SYSTEM.md. Wraps existing infrastructure
            // (LevelingSystem + TradeSystem) so quest content can grant
            // XP and drams from a stage's OnEnter list — the canonical
            // place to put rewards on a terminal stage's `OnEnter`.
            //
            // These are the v1 reward action set. Existing actions
            // (GiveItem, ChangeFactionFeeling, AddFact, SetFact, Reveal)
            // already cover the rest of the standard quest-reward
            // surface. Future content authoring will surface the next
            // necessary reward type.

            // AwardXP(amount) — grant the listener (typically the
            // player) `amount` XP and trigger the level-up check.
            // Defensive: parse failures + non-positive amounts no-op
            // so a typo in JSON content can't grant absurd XP.
            Register("AwardXP", (speaker, listener, arg) =>
            {
                if (!int.TryParse(arg, out int amt) || amt <= 0) return;
                if (listener == null) return;

                var xp = listener.GetStat("Experience");
                if (xp == null) return;
                xp.BaseValue += amt;

                MessageLog.Add($"You gain {amt} XP.");

                // Trigger the level-up check. Zone passed as null —
                // tick-driven advances may not have a zone reference.
                // CheckLevelUp guards null zone for the FX path.
                LevelingSystem.CheckLevelUp(listener, /*zone*/null);
            });

            // GiveDrams(amount) — grant the listener `amount` drams
            // (CoO's currency, IntProperty "Drams"). Mirrors the
            // shape of AwardXP — same parse-then-validate-then-mutate
            // flow. Defensive against negative/zero (use TakeDrams
            // for cost actions if/when added; not in v1).
            Register("GiveDrams", (speaker, listener, arg) =>
            {
                if (!int.TryParse(arg, out int amt) || amt <= 0) return;
                if (listener == null) return;

                int before = TradeSystem.GetDrams(listener);
                TradeSystem.SetDrams(listener, before + amt);

                MessageLog.Add($"You receive {amt} drams.");
            });

            // ── WRS.M2 weapon-rental actions ─────────────────────────
            // Per Docs/WEAPON-RENTAL-SYSTEM.md. Mirror the GiveDrams
            // shape for the Ink wallet, plus dialogue verbs that drive
            // RentalSystem.TryRent / TryReturn from a conversation
            // choice. Deliberately kept thin: the Quartermaster
            // dialogue offers one choice per stocked blueprint
            // (RentItem:RentalDagger etc.) instead of a list-picker UI,
            // matching how Qud quest-reward menus are structured.

            // GiveInk(amount) — grant the listener `amount` Ink.
            // Same defensive shape as GiveDrams: parse, validate
            // positive, mutate.
            Register("GiveInk", (speaker, listener, arg) =>
            {
                if (!int.TryParse(arg, out int amt) || amt <= 0) return;
                if (listener == null) return;

                RentalSystem.AddInk(listener, amt);
                MessageLog.Add($"You receive {amt} ink.");
            });

            // RentItem(blueprintName) — find the first item in the
            // speaker's inventory whose blueprint matches `arg` and
            // route through RentalSystem.TryRent. Silently no-ops if
            // the speaker has no matching stock; TryRent itself emits
            // the player-visible failure messages (insufficient ink,
            // not rentable, etc.).
            Register("RentItem", (speaker, listener, arg) =>
            {
                if (string.IsNullOrEmpty(arg)) return;
                if (speaker == null || listener == null) return;

                var speakerInv = speaker.GetPart<InventoryPart>();
                if (speakerInv == null) return;

                Entity stock = null;
                for (int i = 0; i < speakerInv.Objects.Count; i++)
                {
                    if (speakerInv.Objects[i].BlueprintName == arg)
                    {
                        stock = speakerInv.Objects[i];
                        break;
                    }
                }
                if (stock == null)
                {
                    MessageLog.Add($"{speaker.GetDisplayName()} has none of those left.");
                    return;
                }

                RentalSystem.TryRent(listener, speaker, stock);
            });

            // ReturnRentals — iterate the listener's inventory and
            // return every item whose RentalPart.LessorBlueprintName
            // matches the speaker. Items rented from a different
            // lessor are left alone — the v1 design has at most one
            // Quartermaster blueprint, but the per-item check makes
            // the action safe to use anywhere. Iterate in reverse so
            // RemoveObject inside the loop doesn't shift later
            // indices.
            Register("ReturnRentals", (speaker, listener, arg) =>
            {
                if (speaker == null || listener == null) return;

                var inv = listener.GetPart<InventoryPart>();
                if (inv == null) return;

                int returnedCount = 0;
                for (int i = inv.Objects.Count - 1; i >= 0; i--)
                {
                    var item = inv.Objects[i];
                    var rental = item.GetPart<RentalPart>();
                    if (rental == null) continue;
                    if (rental.LessorBlueprintName != speaker.BlueprintName) continue;

                    if (RentalSystem.TryReturn(listener, speaker, item))
                        returnedCount++;
                }

                if (returnedCount == 0)
                    MessageLog.Add("You have no rentals to return here.");
            });
        }

        private static string ResolveSettlementId(Entity speaker)
        {
            if (speaker == null)
                return null;

            string settlementId;
            return speaker.Properties.TryGetValue("SettlementId", out settlementId)
                ? settlementId
                : null;
        }

        public static void Reset()
        {
            _actions.Clear();
            _initialized = false;
        }
    }
}
