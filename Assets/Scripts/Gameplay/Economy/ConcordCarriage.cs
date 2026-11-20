using System;
using System.Collections.Generic;
using System.Text;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// The Concord's carriage (Docs/MACHINE-INVENTED-USES.md, "Carriage"). Lore/Codex/06_ConcordCarriageContract.md already writes the contract: the Concord carries goods whole, dry
    /// and unopened for the rate agreed at the desk; delivery is guaranteed to the Last Counter and no further; it will not carry unboxed spores, unsealed bodies or anything that
    /// sings. This is that desk. At a Concord post (Cinderhold's pruning post and the Last Counter, the two places that keep a ledger today) the keeper takes one carried stack, a
    /// fee, and hands it back at the other post after the days the road takes.
    /// <para><b>Why it is an honest machine.</b> The goods are the real entity, held in <see cref="ConcordCarriagePart"/> on the player and saved with the pack; the fee leaves through
    /// <see cref="Ledger.TryDebit"/> (a sink, nothing is minted); arrival is the clock compared at the destination, not a timer on an unloaded zone; a post refuses what clause five
    /// refuses and anyone whose ledger is closed (in default, or standing too low to be offered credit); the ordinary path (carry it yourself) stays free.</para>
    /// <para><b>Conversation.</b> A post-keeper's greeting reads what is in your name and offers "collect" when something has arrived and "send a parcel to X". The send node lists the
    /// stacks that could go with the fee beside each; choosing one shows the whole bargain (fee, days, your purse) before anything is spent. Synthetic node ids are prefixed
    /// <see cref="NodePrefix"/>; "Back" returns to the authored Start node. Diag (trade): CarriageSent, CarriageCollected, CarriageRefused.</para>
    /// </summary>
    public static class ConcordCarriage
    {
        public const string OpenAction = "CarriageOpen", SendAction = "CarriageSend", CollectAction = "CarriageCollect";
        public const string NodePrefix = "carriage:";
        /// <summary>The profiles of the posts that keep a ledger today. Tally (CentralExchange) joins when it keeps one.</summary>
        static readonly string[] PostProfiles = { "PruningPost", "ConcordPost" };
        /// <summary>The Last Counter is where the contract's guarantee ends: the cart stops there.</summary>
        public const string LastCounterProfile = "ConcordPost";

        public const int BaseFee = 2, WeightPerDram = 5, CellsPerDay = 4, MaxParcels = 6, PageSize = 8;

        // Clause five, by what the thing is and not by how its blueprint is spelled: a tag (Spore, Corpse, Sings) is the general door so new content can opt in, and these named sets cover what
        // exists today. A name that merely contains "Spore" (the Sporeblade is a sword) is not a spore.
        static readonly HashSet<string> UnboxedSpores = new HashSet<string> { "BlastcapSpore", "ShamblerSporeSac" };
        static readonly HashSet<string> UnsealedBodies = new HashSet<string> { "SaltCuredBody", "BogTakenBody", "PreFellingBody" };
        static readonly HashSet<string> Singers = new HashSet<string> { "ChoirCutting" };

        public readonly struct Post
        {
            public readonly string Name, ZoneId, Profile; public readonly int X, Y;
            public Post(string name, string zoneId, string profile, int x, int y) { Name = name; ZoneId = zoneId; Profile = profile; X = x; Y = y; }
        }

        static List<Post> _posts;
        public static IReadOnlyList<Post> Posts
        {
            get
            {
                if (_posts != null) return _posts;
                var list = new List<Post>();
                foreach (var p in WorldMapAuthoring.Places)
                    if (Array.IndexOf(PostProfiles, p.Profile) >= 0) list.Add(new Post(p.Name, WorldMap.ToZoneID(p.X, p.Y, 0), p.Profile, p.X, p.Y));
                return _posts = list;
            }
        }

        public static bool TryPost(string zoneId, out Post post)
        {
            foreach (var p in Posts) if (p.ZoneId == zoneId) { post = p; return true; }
            post = default; return false;
        }

        /// <summary>The post the keeper stands in, when the keeper keeps the ledger and is alive: the only place the carriage runs.</summary>
        public static bool AtPost(Entity keeper, out Post post)
        {
            post = default;
            if (keeper == null || !ConcordAccount.KeepsLedger(keeper) || keeper.GetStatValue("Hitpoints") <= 0) return false;
            return TryPost(keeper.SpatialZone?.ZoneID, out post);
        }

        public static int Days(Post from, Post to)
        {
            int cells = Math.Max(Math.Abs(from.X - to.X), Math.Abs(from.Y - to.Y));
            return Math.Max(1, (cells + CellsPerDay - 1) / CellsPerDay);
        }

        public static int StackOf(Entity item) => Math.Max(1, item?.GetPart<StackerPart>()?.StackCount ?? 1);

        /// <summary>The fee for <paramref name="count"/> units of a stack (the whole stack when 0): the base rate plus one dram for every <see cref="WeightPerDram"/> weight, rounded up.</summary>
        public static int Fee(Entity item, int count = 0)
        {
            int stack = StackOf(item); if (count <= 0 || count > stack) count = stack;
            return BaseFee + (HandlingService.GetWeight(item) * count + WeightPerDram - 1) / WeightPerDram;
        }

        // ── what the carriage will and will not take ──

        /// <summary>The ledger's gate: the Concord's carriage runs on the same book as its credit, so a closed ledger closes the cart. Null when open.</summary>
        public static string GateRefusal(Entity player)
        {
            if (player == null) return "nobody";
            ConcordAccount.Review(player, WorldClock.CurrentTick);   // a day missed while resting is found now, as Borrow and Repay find it
            if (ConcordAccount.Line(player) <= 0) return "The ledger has no room for your name. Settle what you owe, or mend your standing, and the cart will take your parcels.";
            var part = player.GetPart<ConcordCarriagePart>();
            if (part != null && part.WholeCount >= MaxParcels) return "The cart holds " + MaxParcels + " parcels in your name already. Collect one before you send another.";
            return null;
        }

        /// <summary>A keeper who is burning, stunned, frozen or otherwise unable to do business takes no parcels and hands none over, as at any counter. Null when the keeper can work.</summary>
        public static string KeeperRefusal(Entity keeper)
            => TradeSystem.TraderUnableToTrade(keeper, out string reason) ? "The clerk " + reason + " and cannot see to the cart." : null;

        /// <summary>Clause five and the carrying rules, as one sentence per refusal; null when the stack can go. The player, not the keeper, is the trade party, so a rental, a
        /// quest item or anything bound to its owner vetoes exactly as it does at any counter.</summary>
        public static string ItemRefusal(Entity player, Entity keeper, Entity item)
        {
            if (item == null) return "nothing";
            string name = item.GetDisplayName();
            if (item.GetPart<PhysicsPart>()?.Equipped != null) return "You wear " + name + ". Take it off first.";
            if (MaterialProcessService.IsProtected(item) || ContainedLiquidSourcePart.IsRecoveryCustodyProtected(item)) return name + " is held for work in progress. The cart cannot take it.";
            if (!TradeSystem.CanBeTraded(item, player, keeper, "Carriage")) return "The Concord carries only what can change hands. " + name + " cannot.";
            string bp = item.BlueprintName ?? "";
            if (UnboxedSpores.Contains(bp) || item.HasTag("Spore")) return "Clause the fifth: no unboxed spores.";
            if (UnsealedBodies.Contains(bp) || item.HasTag("Corpse")) return "Clause the fifth: no bodies not sealed to Curation intake standard.";
            if (Singers.Contains(bp) || item.HasTag("Sings")) return "Clause the fifth: nothing that sings.";
            return null;
        }

        /// <summary>Every stack in the pack the keeper would take, heaviest first. The send node shows them <see cref="PageSize"/> at a time.</summary>
        public static List<Entity> Eligible(Entity player, Entity keeper)
        {
            var result = new List<Entity>();
            var inv = player?.GetPart<InventoryPart>();
            if (inv == null) return result;
            for (int i = 0; i < inv.Objects.Count; i++)
                if (inv.Objects[i] != null && ItemRefusal(player, keeper, inv.Objects[i]) == null) result.Add(inv.Objects[i]);
            result.Sort((a, b) => { int c = InventoryPart.GetItemWeight(b).CompareTo(InventoryPart.GetItemWeight(a)); return c != 0 ? c : string.CompareOrdinal(a.GetDisplayName(), b.GetDisplayName()); });
            return result;
        }

        // ── the account of parcels ──

        public static int ReadyHere(Entity player, string zoneId, int now)
        {
            var part = player?.GetPart<ConcordCarriagePart>(); if (part == null) return 0;
            int n = 0;
            for (int i = 0; i < part.Count; i++) if (part.Whole(i) && part.To[i] == zoneId && now >= part.ReadyTick[i]) n++;
            return n;
        }

        static string PlaceName(string zoneId) => TryPost(zoneId, out var p) ? p.Name : PlaceSiteCatalog.PlaceAt(zoneId) ?? "somewhere";

        static string When(int ticks)
        {
            if (ticks <= 0) return "ready";
            int days = (ticks + WorldClock.DayLengthTicks - 1) / WorldClock.DayLengthTicks;
            return days <= 1 ? "due within the day" : "due in " + days + " days";
        }

        /// <summary>What the parcels in the player's name are doing, one line each; null when there are none.</summary>
        public static string Statement(Entity player, string hereZoneId, int now)
        {
            var part = player?.GetPart<ConcordCarriagePart>(); if (part == null || part.Count == 0) return null;
            var sb = new StringBuilder("In your name on the cart:");
            int shown = 0;
            for (int i = 0; i < part.Count; i++)
            {
                if (!part.Whole(i)) continue;
                shown++;
                bool here = part.To[i] == hereZoneId; int left = part.ReadyTick[i] - now;
                sb.Append("\n- ").Append(part.Goods[i].GetDisplayName()).Append(" for ").Append(here ? "this post" : PlaceName(part.To[i])).Append(", ")
                  .Append(left <= 0 ? (here ? "ready to collect." : "waiting at " + PlaceName(part.To[i]) + ".") : When(left) + ".");
            }
            return shown == 0 ? null : sb.ToString();
        }

        /// <summary>One line for the world map: how many parcels are on the cart in the player's name, and where any have arrived. Null when there are none.</summary>
        public static string MapNote(Entity player, int now)
        {
            var part = player?.GetPart<ConcordCarriagePart>(); if (part == null) return null;
            int total = 0, ready = 0; var where = new List<string>();
            for (int i = 0; i < part.Count; i++)
            {
                if (!part.Whole(i)) continue;
                total++;
                if (now < part.ReadyTick[i]) continue;
                ready++; string name = PlaceName(part.To[i]); if (!where.Contains(name)) where.Add(name);
            }
            if (total == 0) return null;
            string text = "The Concord's cart carries " + total + (total == 1 ? " parcel" : " parcels") + " in your name";
            if (ready > 0) text += "; " + ready + (ready == 1 ? " has" : " have") + " arrived at " + string.Join(" and ", where);
            return text + ".";
        }

        public static void OnMapOpened(Entity player)
        {
            string note = MapNote(player, WorldClock.CurrentTick);
            if (note != null) MessageLog.Add(note);
        }

        // ── conversation ──

        public static void AppendChoices(List<ChoiceData> choices, Entity speaker, Entity listener, string nodeId)
        {
            if (choices == null || listener == null || nodeId != "Start" || !AtPost(speaker, out var here)) return;
            int ready = ReadyHere(listener, here.ZoneId, WorldClock.CurrentTick);
            if (ready > 0) choices.Add(new ChoiceData { Text = "[Carriage] Collect what has arrived (" + ready + ").", Target = "Start", Actions = Params(CollectAction, here.ZoneId) });
            foreach (var to in Posts)
                if (to.ZoneId != here.ZoneId)
                    choices.Add(new ChoiceData { Text = "[Carriage] Send a parcel to " + to.Name + " (about " + Days(here, to) + " days).", Target = "", Actions = Params(OpenAction, "send|" + to.ZoneId) });
        }

        public static string AppendText(string original, Entity speaker, Entity listener, string nodeId, int now)
        {
            if (nodeId != "Start" || listener == null || !AtPost(speaker, out var here)) return original;
            string statement = Statement(listener, here.ZoneId, now);
            if (statement == null) return original;
            return string.IsNullOrEmpty(original) ? statement : original + "\n\n" + statement;
        }

        static List<ConversationParam> Params(string key, string value) => new List<ConversationParam> { new ConversationParam { Key = key, Value = value } };
        static NodeData Node(string id, string text) => new NodeData { ID = NodePrefix + id, Text = text };
        static ChoiceData Back() => new ChoiceData { Text = "[Back]", Target = "Start" };

        /// <summary>The CarriageOpen action: replace the current node with a synthetic one (the send list, or the bargain for one stack).</summary>
        public static string Open(Entity speaker, Entity listener, string key)
        {
            var node = BuildNode(key, speaker, listener);
            if (node == null) return "unknown_node";
            ConversationManager.CurrentNode = node;
            return null;
        }

        public static NodeData BuildNode(string key, Entity keeper, Entity player)
        {
            var parts = key?.Split('|');
            if (parts == null || parts.Length < 2 || !AtPost(keeper, out var here) || !TryPost(parts[1], out var to) || to.ZoneId == here.ZoneId) return null;
            if (parts[0] == "send" && parts.Length == 2) return SendNode(keeper, player, here, to, 0);
            if (parts[0] == "send" && parts.Length == 3) return int.TryParse(parts[2], out int page) && page >= 0 ? SendNode(keeper, player, here, to, page) : null;
            if (parts[0] == "confirm" && parts.Length == 3) return ConfirmNode(keeper, player, here, to, parts[2]);
            return null;
        }

        static string Terms(Post here, Post to)
            => "\"The rate is the rate: " + BaseFee + " drams, and one more for every " + WeightPerDram + " weight. " + Days(here, to) + " days to " + to.Name + ", whole, dry and unopened.\""
               + (to.Profile == LastCounterProfile ? "\nThe cart stops at the Last Counter: past it the Concord guarantees only that the caravan will have departed." : "");

        static NodeData SendNode(Entity keeper, Entity player, Post here, Post to, int page)
        {
            string id = "send|" + to.ZoneId + (page > 0 ? "|" + page : "");
            string gate = KeeperRefusal(keeper) ?? GateRefusal(player);
            if (gate != null) { var closed = Node(id, "\"" + gate + "\""); closed.Choices.Add(Back()); return closed; }
            var items = Eligible(player, keeper);
            int pages = Math.Max(1, (items.Count + PageSize - 1) / PageSize); if (page >= pages) page = pages - 1; id = "send|" + to.ZoneId + (page > 0 ? "|" + page : "");
            var text = new StringBuilder(Terms(here, to));
            if (items.Count == 0) text.Append("\n\nNothing you carry will go: the cart takes what can change hands, unworn, and refuses spores, unsealed bodies and anything that sings.");
            else text.Append("\n\nWhich stack?").Append(pages > 1 ? " (page " + (page + 1) + " of " + pages + ", heaviest first)" : "");
            var node = Node(id, text.ToString());
            for (int i = page * PageSize; i < items.Count && i < (page + 1) * PageSize; i++)
                node.Choices.Add(new ChoiceData { Text = "[" + items[i].GetDisplayName() + " - " + Fee(items[i]) + " drams]", Target = "", Actions = Params(OpenAction, "confirm|" + to.ZoneId + "|" + items[i].ID) });
            if (page + 1 < pages) node.Choices.Add(new ChoiceData { Text = "[More goods...]", Target = "", Actions = Params(OpenAction, "send|" + to.ZoneId + "|" + (page + 1)) });
            if (page > 0) node.Choices.Add(new ChoiceData { Text = "[Earlier goods...]", Target = "", Actions = Params(OpenAction, "send|" + to.ZoneId + "|" + (page - 1)) });
            node.Choices.Add(Back());
            return node;
        }

        /// <summary>The counts offered for a stack: all of it, and for a stack of more than one that can be divided, half and one (each once: a stack of three has half = one). Each is its own row with its own fee.</summary>
        public static List<int> Quantities(Entity item)
        {
            int stack = StackOf(item); var q = new List<int> { stack };
            if (stack > 1 && item.GetPart<StackerPart>()?.CanSplit() == true)
            {
                int half = stack / 2; if (half > 1) q.Add(half);
                q.Add(1);
            }
            return q;
        }

        static NodeData ConfirmNode(Entity keeper, Entity player, Post here, Post to, string itemId)
        {
            var item = Find(player, itemId);
            if (item == null) return null;
            string refusal = KeeperRefusal(keeper) ?? ItemRefusal(player, keeper, item) ?? GateRefusal(player);
            int drams = TradeSystem.GetDrams(player);
            var text = new StringBuilder();
            text.Append(item.GetDisplayName()).Append(" to ").Append(to.Name).Append(", ready in ").Append(Days(here, to)).Append(" days. You carry ").Append(drams).Append(" drams.");
            if (refusal != null) text.Append("\n\n\"").Append(refusal).Append('"');
            var node = Node("confirm|" + to.ZoneId + "|" + itemId, text.ToString());
            bool any = false; int stack = StackOf(item);
            if (refusal == null)
                foreach (int count in Quantities(item))
                {
                    int fee = Fee(item, count);
                    if (drams < fee) continue;
                    any = true;
                    string what = stack == 1 ? "it" : count == stack ? "all " + count : count == 1 ? "one" : "half (" + count + ")";
                    node.Choices.Add(new ChoiceData { Text = "[Send " + what + " - " + fee + " drams.]", Target = "Start",
                        Actions = Params(SendAction, to.ZoneId + "|" + itemId + (count == stack ? "" : "|" + count)) });
                }
            if (refusal == null && !any) node.Text += "\n\nYou do not have the fee.";
            node.Choices.Add(new ChoiceData { Text = "[Back]", Target = "", Actions = Params(OpenAction, "send|" + to.ZoneId) });
            return node;
        }

        static Entity Find(Entity player, string id)
        {
            var inv = player?.GetPart<InventoryPart>();
            if (inv == null || string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < inv.Objects.Count; i++) if (inv.Objects[i] != null && inv.Objects[i].ID == id) return inv.Objects[i];
            return null;
        }

        // ── actions ──

        /// <summary>Entrust a carried stack, or part of it. Everything is checked first; the goods leave the pack (a split stack leaves its remainder behind), the fee leaves the purse, and the
        /// parcel is recorded, in that order with a complete undo, so a refusal at any step changes nothing. Argument: "destinationZoneId|itemId" or "destinationZoneId|itemId|count".
        /// Null on success, else the refusal code.</summary>
        public static string Send(Entity keeper, Entity player, string arg)
        {
            var parts = arg?.Split('|');
            if (parts == null || parts.Length < 2 || parts.Length > 3) return Refuse(keeper, player, "invalid_argument", null);
            if (!AtPost(keeper, out var here)) return Refuse(keeper, player, "not_a_post", "This is not a Concord post that runs a cart.");
            if (!TryPost(parts[0], out var to) || to.ZoneId == here.ZoneId) return Refuse(keeper, player, "unknown_destination", "The cart does not run there.");
            string unable = KeeperRefusal(keeper); if (unable != null) return Refuse(keeper, player, "keeper_unable", unable);
            string gate = GateRefusal(player);
            if (gate != null) return Refuse(keeper, player, "ledger_closed", gate);
            var item = Find(player, parts[1]);
            if (item == null) return Refuse(keeper, player, "not_carried", "You are not carrying that any more.");
            string refusal = ItemRefusal(player, keeper, item);
            if (refusal != null) return Refuse(keeper, player, "not_carried_by_cart", refusal);
            int stack = StackOf(item), count = stack;
            if (parts.Length == 3 && (!int.TryParse(parts[2], out count) || count < 1 || count > stack)) return Refuse(keeper, player, "invalid_argument", "The clerk cannot send that many.");
            int fee = Fee(item, count);
            if (TradeSystem.GetDrams(player) < fee) return Refuse(keeper, player, "insufficient_drams", "The fee is " + fee + " drams; you carry " + TradeSystem.GetDrams(player) + ".");

            var inv = player.GetPart<InventoryPart>(); var stacker = item.GetPart<StackerPart>();
            int now = WorldClock.CurrentTick, ready = now + Days(here, to) * WorldClock.DayLengthTicks;
            // The parcel is the whole stack, or the units split off it. A stack that will not divide (a worked or custody-held one) is refused whole.
            Entity parcel = item; bool split = count < stack;
            if (split)
            {
                parcel = stacker?.SplitStack(count);
                if (parcel == null) return Refuse(keeper, player, "cannot_divide", "That stack cannot be divided. Send it whole.");
            }
            else if (!inv.RemoveObject(item)) return Refuse(keeper, player, "not_carried", "You are not carrying that any more.");
            string label = parcel.GetDisplayName();
            if (!Ledger.TryDebit(player, fee, "concord-carriage", out _))
            {
                // Nothing here can fail after the checks above; if it ever did, the goods go back to the pack (a split part is merged back, or held as its own stack if it will not merge).
                if (split) { stacker.MergeFrom(parcel); if ((parcel.GetPart<StackerPart>()?.StackCount ?? 0) > 0) inv.AddObject(parcel); } else inv.AddObject(item);
                return Refuse(keeper, player, "insufficient_drams", "The fee is " + fee + " drams.");
            }
            var carriage = player.GetPart<ConcordCarriagePart>();
            if (carriage == null) { carriage = new ConcordCarriagePart(); player.AddPart(carriage); }
            carriage.Add(parcel, here.ZoneId, to.ZoneId, now, ready, fee);
            MessageLog.Add("The clerk ties and seals " + label + " in your name. It will be at " + to.Name + " in " + Days(here, to) + " days. Fee " + fee + " drams.");
            Record("CarriageSent", keeper, player, new { item = parcel.BlueprintName, label, count, split, from = here.ZoneId, to = to.ZoneId, fee, ready });
            return null;
        }

        /// <summary>Hand over every parcel that has arrived at this post. A parcel that does not fit the pack stays on the cart, and the message says so. Argument: the post zone id.</summary>
        public static string Collect(Entity keeper, Entity player, string arg)
        {
            if (!AtPost(keeper, out var here) || arg != here.ZoneId) return Refuse(keeper, player, "not_a_post", "This is not a Concord post that runs a cart.");
            string unable = KeeperRefusal(keeper); if (unable != null) return Refuse(keeper, player, "keeper_unable", unable);
            var part = player?.GetPart<ConcordCarriagePart>(); var inv = player?.GetPart<InventoryPart>();
            if (part == null || inv == null) return Refuse(keeper, player, "nothing_to_collect", "Nothing is in your name here.");
            int now = WorldClock.CurrentTick, collected = 0, tooHeavy = 0;
            for (int i = part.Count - 1; i >= 0; i--)
            {
                if (!part.Whole(i) || part.To[i] != here.ZoneId || now < part.ReadyTick[i]) continue;
                var goods = part.Goods[i]; string label = goods.GetDisplayName();
                if (!inv.AddObject(goods)) { tooHeavy++; continue; }
                part.RemoveAt(i); collected++;
                MessageLog.Add("The clerk hands over " + label + ", seal unbroken.");
                Record("CarriageCollected", keeper, player, new { item = goods.BlueprintName, label, at = here.ZoneId });
            }
            if (tooHeavy > 0) MessageLog.Add(tooHeavy == 1 ? "One parcel stays on the cart: you cannot carry it. Set something down and ask again." : tooHeavy + " parcels stay on the cart: you cannot carry them. Set something down and ask again.");
            if (collected > 0) return null;
            return Refuse(keeper, player, tooHeavy > 0 ? "too_heavy" : "nothing_to_collect", tooHeavy > 0 ? null : "Nothing in your name has arrived here yet.");
        }

        static string Refuse(Entity keeper, Entity player, string reason, string message)
        {
            if (message != null) MessageLog.Add(message);
            Record("CarriageRefused", keeper, player, new { reason });
            return reason;
        }

        static void Record(string kind, Entity keeper, Entity player, object payload)
        {
            if (Diag.IsChannelEnabled("trade")) Diag.Record("trade", kind, actor: player, target: keeper, payload: payload);
        }
    }
}
