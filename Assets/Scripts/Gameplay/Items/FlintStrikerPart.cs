using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// A finite fire-starter (Docs/MACHINE-INVENTED-USES.md, "Spark"). It adds exactly two things and invents no fire rules of its own:
    /// <list type="bullet">
    /// <item>It answers the shared "is there a flame in reach" lookup (<see cref="IgnitionSources"/>) when a carried striker has a strike left and is dry, so a torch or a finite
    /// cooking fire lights from it through their ordinary verbs; a burning fire in reach is always used first and costs nothing.</item>
    /// <item>It offers "strike a spark" toward an adjacent cell only where one point of heat would light something (<see cref="TileReactionSystem.HeatWouldIgnite"/>), and writes that
    /// heat through <see cref="ZoneTileStateSystem.ApplyFireToTile"/>, the routine every fire ability uses, so an oil slick burns by the shared reactions and whatever the world already does for those fires happens here too. No law, damage or spread
    /// rule is written or bypassed in this class.</item>
    /// </list>
    /// Strikes are finite and renewed by spending an iron spike. Wet or frozen, it will not spark.
    /// </summary>
    public sealed class FlintStrikerPart : Part, IDescribeState, IDescribeUse, IInventoryActionReceiptOwner
    {
        public override string Name => "FlintStriker";

        public const string Blueprint = "FlintStriker";

        public int Strikes = 20;
        public int MaxStrikes = 20;

        public const string SparkPrefix = "StrikeSpark|", KnapPrefix = "KnapStriker|", KnapBlueprint = "IronSpikeComponent";
        public const int KnapStrikes = 10;
        const int BenchStrikes = 15;
        const string Service = "ServiceStriker", ColdWork = "cold-work";
        const float WetLimit = 0.35f;

        static readonly (int x, int y, string label)[] Directions =
        {
            (0,-1,"north"),(1,-1,"northeast"),(1,0,"east"),(1,1,"southeast"),
            (0,1,"south"),(-1,1,"southwest"),(-1,0,"west"),(-1,-1,"northwest")
        };

        // ── what the shared ignition lookup asks ──

        /// <summary>When a carried striker is worn smooth, a sentence saying to knap it (with the spike carried, or that it needs one); null when none is worn. Appended to the refusals
        /// where a flame was wanted, so the way out is stated where the problem is felt.</summary>
        internal static string WornHint(Entity actor)
        {
            var pack = actor?.GetPart<InventoryPart>(); if (pack == null) return null;
            bool worn = pack.Objects.Any(o => o?.GetPart<FlintStrikerPart>() is FlintStrikerPart p && p.Strikes <= 0);
            if (!worn) return null;
            return Spike(actor) != null ? " Your flint is worn smooth: knap it with an iron spike." : " Your flint is worn smooth; knapping it needs an iron spike.";
        }

        /// <summary>A striker that can make a flame for this actor right now: carried by them, a strike left, and neither wet nor frozen.</summary>
        internal static bool Usable(Entity actor, Entity item)
        {
            var part = item?.GetPart<FlintStrikerPart>();
            return part != null && part.ParentEntity == item && part.Strikes > 0 && WorldResourceActions.Carried(actor, item, false) && Dry(item);
        }

        static bool Dry(Entity item) => !(item.GetEffect<WetEffect>()?.Moisture > WetLimit) && !(item.GetEffect<FrozenEffect>()?.Cold > 0);

        /// <summary>Spends one strike inside the caller's transaction, undone with it. The message and diag run only on commit.</summary>
        internal bool Spend(Entity actor, InventoryTransaction tx, string use)
        {
            if (tx == null || !Usable(actor, ParentEntity)) return false;
            int before = Strikes; var owner = ParentEntity;
            tx.Do(() => Strikes = before - 1, () => Strikes = before);
            tx.AfterCommit(() => { MessageLog.Add("You strike the flint."); Diag.Record("event", "StrikerLit", actor, owner, new { use, remaining = Strikes }); });
            return true;
        }

        // ── the reader ──

        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 40;
        public string DescribeState(DescribeContext context)
        {
            if (Strikes <= 0) return "The flint is worn smooth. Knapping a new edge needs an iron spike.";
            return Strikes + (Strikes == 1 ? " strike left." : " strikes left.");
        }

        /// <summary>How to use it, what it needs and costs, from the live constants (the owner, 2026-11-21). The "right now" line is built from <see cref="AddActions"/>'s own rows.</summary>
        public void DescribeUse(DescribeContext context, UseGuide guide)
        {
            guide.How.Add("Press C on a cell beside you that is coated with oil or another liquid that burns and choose light it with the flint striker (or choose strike a spark from the pack).");
            guide.How.Add("It also lights a torch or a camp hearth when no fire is near: use their light row and the striker gives the flame.");
            guide.How.Add("When the flint is worn, choose knap an edge with an iron spike in your pack.");
            guide.How.Add("An accessible cold-work bench or forge can restore up to " + BenchStrikes + " strikes from the same spike.");
            guide.Needs.Add("a strike left (" + Math.Max(0, Strikes) + " of " + MaxStrikes + ")");
            guide.Needs.Add("a dry flint (it will not spark wet or frozen)");
            guide.Needs.Add("a cell beside you coated with oil or another liquid that burns");
            guide.Limits.Add("Each spark spends one strike; an iron spike knapped against it brings back " + KnapStrikes + ". A burning fire in reach is always used first and costs nothing.");
            guide.WhyNot = !UseGuides.CarriedBy(context.Viewer, ParentEntity) ? UseGuides.PickUpFirst : "no cell beside you is coated with oil or another liquid that burns";   // shown only when no row is: a worn flint always has its knap row
        }

        // ── rows and verbs ──

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actor = e.GetParameter<Entity>("Actor");
                var actions = e.GetParameter<InventoryActionList>("Actions");
                AddActions(actor, actions);
                var zone = PreparationActions.ZoneFor(e, actor);
                if (ParentEntity != null) RouteWorkMenu.Rows(ParentEntity, actor, zone, actions, BenchChoices(actor, zone), true);
                return true;
            }
            if (e.ID == "InventoryAction")
            {
                string command = e.GetStringParameter("Command");
                if (command == null) return true;
                if (OwnsInventoryReceipt(command))
                {
                    e.Handled = RouteWorkMenu.Act(ParentEntity, e);
                    return !e.Handled;
                }
                bool spark = command.StartsWith(SparkPrefix, StringComparison.Ordinal), knap = command.StartsWith(KnapPrefix, StringComparison.Ordinal);
                if (!spark && !knap) return true;
                var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone"); var tx = e.GetParameter<InventoryTransaction>("InventoryTransaction");
                if (spark ? TrySpark(actor, zone, command, tx) : TryKnap(actor, zone, command, tx)) { e.Handled = true; return false; }
            }
            return true;
        }

        public bool OwnsInventoryReceipt(string command) => ParentEntity?.GetPart<FlintStrikerPart>() == this
            && command?.StartsWith(Service + "|", StringComparison.Ordinal) == true;

        IEnumerable<RouteWorkMenu.Entry> BenchChoices(Entity actor, Zone zone)
        {
            int before = Strikes, maximum = MaxStrikes;
            int worn = Math.Max(0, before);
            if (maximum <= worn || maximum - worn <= KnapStrikes || !PreparationActions.CurrentActor(actor, zone)) yield break;
            var striker = ParentEntity;
            if (!WorldResourceActions.Carried(actor, striker) || !PreparationActions.Carried(actor, striker)
                || LocalPorterReservationPart.Blocks(striker)) yield break;
            var candidates = new List<StationCandidate>();
            StationQuery.Collect(actor, zone, ColdWork, candidates);
            var manager = WorldLocationContext.For(zone);
            bool Ready(Entity station) => WorldResourceActions.Ground(station, zone) && (manager == null || manager.ActiveZone == zone)
                && RouteWorkMenu.Visible(zone.GetEntityCell(station))
                && StationQuery.Near(actor, station, zone, ColdWork)
                && StationAccess.Allowed(actor, station, zone, ColdWork, Service, out _, out _);
            var bench = candidates.FirstOrDefault(candidate => candidate.Ready && Ready(candidate.Station)).Station;
            if (bench == null) yield break;
            var pack = actor.GetPart<InventoryPart>();
            var factory = manager?.Factory ?? HarvestablePart.Factory;
            var template = MaterialProcessTemplate.Capture(factory, KnapBlueprint);
            if (template == null) yield break;
            var spike = pack.Objects.FirstOrDefault(item => item?.BlueprintName == KnapBlueprint
                && PreparationActions.Carried(actor, item) && !LocalPorterReservationPart.Blocks(item) && template.Matches(item));
            if (spike == null || new[] { actor.ID, striker.ID, spike.ID, bench.ID }.Distinct().Count() != 4
                || template.Snapshot(spike) is not { } spikePayload) yield break;

            var from = zone.GetEntityCell(actor);
            var at = zone.GetEntityCell(bench);
            var strikerIdentity = RouteWorkMenu.Identity(striker);
            var stationIdentity = RouteWorkMenu.Identity(bench);
            var spikeStack = spike.GetPart<StackerPart>();
            var spikePhysics = spike.GetPart<PhysicsPart>();
            string strikerId = striker.ID, strikerBlueprint = striker.BlueprintName;
            int spikeCount = spikeStack?.StackCount ?? 1;
            int gain = Math.Min(BenchStrikes, maximum - worn);
            int restored = worn + gain;
            string stationName = bench.GetPart<PackableFixturePart>()?.Kind == CampKinds.Bench ? "bench" : bench.GetDisplayName();

            bool OwnsStriker() => ParentEntity == striker && strikerIdentity() && striker.GetPart<FlintStrikerPart>() == this;
            bool Context() => OwnsStriker() && MaxStrikes == maximum && stationIdentity() && Ready(bench)
                && zone.GetEntityCell(actor) == from && zone.GetEntityCell(bench) == at && actor.GetPart<InventoryPart>() == pack
                && PreparationActions.CurrentActor(actor, zone) && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true
                && PreparationActions.Carried(actor, striker) && !LocalPorterReservationPart.Blocks(striker)
                && !LocalPorterReservationPart.Blocks(spike) && spikePayload.Matches()
                && !zone.GetReadOnlyEntities().Any(item => item.ID == strikerId || item.ID == spike.ID);
            bool Before() => Context() && Strikes == before && PreparationActions.Carried(actor, spike)
                && (spikeStack?.StackCount ?? 1) == spikeCount;
            bool After() => Context() && Strikes == restored && (spikeCount > 1
                ? PreparationActions.Carried(actor, spike) && spikeStack.StackCount == spikeCount - 1
                : spike.SpatialZone == null && spikePhysics.InInventory == null && spikePhysics.Equipped == null
                    && !pack.Objects.Concat(pack.EquippedItems.Values).Any(item => item?.ID == spike.ID));

            yield return new RouteWorkMenu.Entry
            {
                Actor = actor, Zone = zone, Cell = at, Key = Service, Claims = new[] { bench, spike },
                Label = $"service striker at {stationName} (1 spike, +{gain} strikes)",
                Hint = $"Use the cold-work fixture to restore {gain} strikes. Spends one iron spike and keeps the striker.",
                Before = Before, After = After,
                Apply = tx =>
                {
                    bool creditReversed = true;
                    if (StationReceipt.Begin(actor, zone, tx, Service, ColdWork, bench) == null || !Before()
                        || !MaterialProcessService.ReserveInput(spike, tx)
                        || !MaterialProcessPayment.Spend(actor, spike, actor, zone, tx, spikePayload.Matches,
                            refundAllowed: () => creditReversed)) return false;
                    tx.Do(() => { creditReversed = false; Strikes = restored; }, () =>
                    {
                        if (ParentEntity != striker || striker.ID != strikerId || striker.BlueprintName != strikerBlueprint
                            || striker.GetPart<FlintStrikerPart>() != this || Strikes != restored) return;
                        Strikes = before;
                        creditReversed = true;
                    });
                    return true;
                },
                Publish = () => MessageLog.Add($"You service the striker and restore {gain} strikes.")
            };
        }

        void AddActions(Entity actor, InventoryActionList actions)
        {
            var zone = actor?.SpatialZone;
            if (actions == null || zone == null || !WorldResourceActions.Carried(actor, ParentEntity, false)) return;
            var origin = zone.GetEntityCell(actor);
            if (origin != null && WorldResourceActions.ActorCurrent(actor, zone) && !Usable(actor, ParentEntity) && Catchable(zone, origin))
                actions.AddDisabled("StrikeSpark", "strike a spark", "striker_unusable", Strikes <= 0 ? "The flint is worn smooth." : "The flint will not spark while it is wet or frozen.", '\0', 16);
            if (origin != null && WorldResourceActions.ActorCurrent(actor, zone) && Usable(actor, ParentEntity))
            {
                foreach (var d in Directions)
                {
                    int x = origin.X + d.x, y = origin.Y + d.y;
                    if (zone.GetCell(x, y) == null || !TileReactionSystem.HeatWouldIgnite(zone, x, y)) continue;
                    // A slick you are standing in burns toward you: say so on the row, before the strike is spent.
                    string warning = TileReactionSystem.HeatWouldIgnite(zone, origin.X, origin.Y) ? " (underfoot)" : "";
                    actions.AddActionWithHint("StrikeSpark", "strike a spark " + d.label + warning, SparkCommand(zone, origin, x, y, ParentEntity), "Spends 1 strike to throw a spark into that cell; it lights what burns.", '\0', 16, false, ActionTime.OneAction);
                }
            }
            if (Strikes >= MaxStrikes) return;
            if (Spike(actor) != null)
                actions.AddActionWithHint("KnapStriker", "knap an edge (1 spike, +" + Math.Min(KnapStrikes, MaxStrikes - Math.Max(0, Strikes)) + " strikes)", KnapPrefix + ParentEntity.ID, "Chips a fresh edge with an iron spike (used up). Restores strikes.", '\0', 15, false, ActionTime.OneAction);
            else
                actions.AddDisabled("KnapStriker", "knap an edge", "no-spike", "Knapping a new edge needs an iron spike.", '\0', 15);
        }

        /// <summary>Some adjacent cell would catch a spark, so a refusal row is worth showing.</summary>
        static bool Catchable(Zone zone, Cell origin)
        {
            foreach (var d in Directions) if (zone.GetCell(origin.X + d.x, origin.Y + d.y) != null && TileReactionSystem.HeatWouldIgnite(zone, origin.X + d.x, origin.Y + d.y)) return true;
            return false;
        }

        internal static string SparkCommand(Zone zone, Cell origin, int x, int y, Entity striker) => FireStarting.CellCommand(SparkPrefix, zone, origin, x, y, striker);

        static string N(int value) => value.ToString(CultureInfo.InvariantCulture);
        static bool Int(string text, out int value) => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

        static Entity Spike(Entity actor)
            => actor?.GetPart<InventoryPart>()?.Objects.FirstOrDefault(o => o != null && o.BlueprintName == KnapBlueprint && WorldResourceActions.Carried(actor, o, false));

        bool TrySpark(Entity actor, Zone zone, string command, InventoryTransaction transaction)
        {
            string[] f = command.Split('|');
            if (zone == null || !WorldResourceActions.ActorCurrent(actor, zone) || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true) return Reject(actor, "actor_unavailable", "You cannot strike a light right now.");
            if (f.Length != 7 || !Int(f[2], out int ox) || !Int(f[3], out int oy) || !Int(f[4], out int x) || !Int(f[5], out int y)) return Reject(actor, "malformed_selection", "That is not a spark you can strike.");
            var origin = zone.GetEntityCell(actor);
            if (Uri.UnescapeDataString(f[1]) != zone.ZoneID || f[6] != ParentEntity.ID || origin == null || origin.X != ox || origin.Y != oy || !WorldResourceActions.Carried(actor, ParentEntity, false))
                return Reject(actor, "stale_selection", "You have moved since you chose that.");
            if (Math.Abs((long)x - origin.X) > 1 || Math.Abs((long)y - origin.Y) > 1 || (x == origin.X && y == origin.Y) || zone.GetCell(x, y) == null)
                return Reject(actor, "not_adjacent", "You can only strike a spark at the cell beside you.");
            if (!Usable(actor, ParentEntity)) return Reject(actor, "striker_unusable", Strikes <= 0 ? "The flint is worn smooth." : "The flint will not spark while it is wet or frozen.");
            if (!TileReactionSystem.HeatWouldIgnite(zone, x, y)) return Reject(actor, "nothing_to_catch", "A spark there would catch on nothing.");
            bool own = transaction == null; transaction ??= new InventoryTransaction();
            try
            {
                if (!transaction.TryClaim(actor, actor, command) || !transaction.TryClaim(ParentEntity, actor, command) || !transaction.TryClaimAction(actor)) return Reject(actor, "in_progress", "The flint is already in use.");
                int before = Strikes; var owner = ParentEntity;
                transaction.Do(() => Strikes = before - 1, () => Strikes = before);
                // The world write happens once, after the action commits: tile heat cannot be undone, so an outer failure must never leave a fire with the strike returned.
                FireStarting.QueueIgnite(actor, zone, x, y, transaction, owner, "You strike a spark.", "FlintStriker", "StrikerSpark", () => new { x, y, remaining = Strikes });
                if (own) transaction.Commit();
                return true;
            }
            finally { if (own) transaction.Rollback(); }
        }

        bool TryKnap(Entity actor, Zone zone, string command, InventoryTransaction transaction)
        {
            string[] f = command.Split('|');
            if (zone == null || !WorldResourceActions.ActorCurrent(actor, zone) || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true) return Reject(actor, "actor_unavailable", "You cannot knap the flint right now.");
            if (f.Length != 2 || f[1] != ParentEntity.ID || !WorldResourceActions.Carried(actor, ParentEntity, false)) return Reject(actor, "stale_selection", "You are no longer holding that.");
            if (Strikes >= MaxStrikes) return Reject(actor, "already_keen", "The flint already has its full edge.");
            var spike = Spike(actor); if (spike == null) return Reject(actor, "no_spike", "Knapping a new edge needs an iron spike.");
            bool own = transaction == null; transaction ??= new InventoryTransaction();
            try
            {
                if (!transaction.TryClaim(actor, actor, command) || !transaction.TryClaim(ParentEntity, actor, command) || !transaction.TryClaim(spike, actor, command))
                    return Reject(actor, "in_progress", "The flint is already in use.");
                var pack = actor.GetPart<InventoryPart>(); var receipt = InventoryTransferSnapshot.Capture(pack); int before = Strikes; var owner = ParentEntity;
                transaction.Do(null, () => { receipt.Restore(); Strikes = before; });
                if (!receipt.Apply(() => pack.TryConsumeOne(spike)) || !receipt.ClaimChanges(transaction, actor, command)) return Reject(actor, "spike_unavailable", "The spike could not be spent.");
                Strikes = Math.Min(MaxStrikes, Math.Max(0, before) + KnapStrikes); // a corrupt negative count is treated as worn, not as a debt
                transaction.AfterCommit(() =>
                {
                    MessageLog.Add("You knap a fresh edge on the flint with an iron spike.");
                    Diag.Record("event", "StrikerKnapped", actor, owner, new { strikes = Strikes });
                });
                if (own) transaction.Commit();
                return true;
            }
            finally { if (own) transaction.Rollback(); }
        }

        bool Reject(Entity actor, string reason, string message)
        {
            MessageLog.Add(message);
            Diag.Record("event", "StrikerRejected", actor, ParentEntity, new { reason });
            return false;
        }
    }
}
