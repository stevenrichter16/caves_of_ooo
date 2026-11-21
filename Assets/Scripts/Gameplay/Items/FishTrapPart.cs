using System;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// The camp fish trap (Docs/MACHINE-INVENTED-USES.md 3n): a wicker weir set down on the shore of a pond. While it stands it catches, at most one fish every <see cref="CatchTicks"/> and never more than
    /// <see cref="MaxHeld"/> at a time, drawing each from the pond's own place-bound supply (<see cref="FishStockPart"/>, shared by every trap on that water). It spends nothing and asks nothing, and a fished-out
    /// pond gives it nothing. Time is reconciled when you read it or empty it, never by a timer on a zone that is not loaded; reading it changes nothing.
    /// <para>Carried it is a kit (no rows, no catching); the clock starts at the set-down. Fish it holds stay in it when it is taken up.</para>
    /// </summary>
    public sealed class FishTrapPart : Part, IDescribeState, IDescribeUse
    {
        public override string Name => "FishTrap";

        public const string Command = "TakeTrapFish", FishBlueprint = "LakePerch";
        public const int MaxHeld = 4, CatchTicks = WorldClock.DayLengthTicks / 2, SoonTicks = 120;

        /// <summary>Fish in the trap; the tick it was last brought up to date (-1 never); and the part of a catch period already elapsed.</summary>
        public int Held, LastTick = -1, Progress;

        bool Standing(Zone zone) => zone != null && ParentEntity?.SpatialZone == zone && !PackableFixturePart.IsInertKit(ParentEntity);

        /// <summary>The pure projection of the trap at <paramref name="now"/>: what it would hold and how far into the next catch it would be, and the stock draw. Changes nothing.</summary>
        readonly struct Projection
        {
            public readonly int Held, Progress, Taken; public readonly bool Pond; public readonly Entity Anchor; public readonly int StockLeft, Capacity;
            public Projection(int held, int progress, int taken, bool pond, Entity anchor, int stockLeft, int capacity) { Held = held; Progress = progress; Taken = taken; Pond = pond; Anchor = anchor; StockLeft = stockLeft; Capacity = capacity; }
        }

        Projection Project(Zone zone, int now)
        {
            if (!Standing(zone)) return new Projection(Held, Progress, 0, false, null, 0, 0);
            int from = Math.Max(LastTick, ParentEntity.GetPart<PackableFixturePart>()?.SetTick ?? -1);
            var at = zone.GetEntityCell(ParentEntity);
            if (at == null || !FishPond.TryFind(zone, at.X, at.Y, out var anchor)) return new Projection(Held, 0, 0, false, null, 0, 0);
            var stock = anchor.GetPart<FishStockPart>();
            int capacity = stock?.Capacity ?? FishStockPart.MaxCapacity, available = stock != null ? stock.AvailableAt(now) : -1;
            if (stock == null) { /* a pond nobody has fished is full: its capacity is set when the stock is first made */ available = int.MaxValue; }
            if (from < 0 || now <= from) return new Projection(Held, Progress, 0, true, anchor, stock != null ? available : -1, stock?.Capacity ?? 0);
            if (Held >= MaxHeld || available <= 0) return new Projection(Held, 0, 0, true, anchor, stock != null ? available : -1, stock?.Capacity ?? 0);
            int progress = Progress + (now - from), catches = progress / CatchTicks;
            if (catches <= 0) return new Projection(Held, progress, 0, true, anchor, stock != null ? available : -1, stock?.Capacity ?? 0);
            int take = Math.Min(Math.Min(catches, MaxHeld - Held), available);
            bool limited = take < catches || Held + take >= MaxHeld || (available != int.MaxValue && available - take <= 0);
            return new Projection(Held + take, limited ? 0 : progress - catches * CatchTicks, take, true, anchor, stock != null ? available - take : -1, stock?.Capacity ?? 0);
        }

        /// <summary>How many fish the trap would hold now (read-only).</summary>
        public int HeldNow(Zone zone, int now) => Project(zone, now).Held;

        /// <summary>Brings the trap and its pond up to date and commits the catch. Idempotent for the same tick.</summary>
        public int Reconcile(Zone zone, int now)
        {
            if (!Standing(zone)) return 0;
            var p = Project(zone, now);
            int taken = 0;
            if (p.Pond && p.Taken > 0)
            {
                var stock = FishStockPart.For(zone, p.Anchor, now); stock.Reconcile(now);
                taken = Math.Min(p.Taken, Math.Min(MaxHeld - Held, stock.Available));
                stock.Available -= taken;
            }
            Held += taken; Progress = taken == p.Taken ? p.Progress : 0; LastTick = now;
            return taken;
        }

        // ── reader ──

        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 40;

        public string DescribeState(DescribeContext context)
        {
            var zone = context.Zone ?? ParentEntity?.SpatialZone;
            if (!Standing(zone)) return Held > 0 ? "It holds " + Held + " fish, and you can take them once it is set down beside a pond." : null;
            int now = WorldClock.CurrentTick; var p = Project(zone, now);
            if (!p.Pond) return "It is not beside water it can fish, so it catches nothing here.";
            // A pond nobody has fished has no stock record yet and is full.
            string look = p.StockLeft < 0 ? FishStockPart.Look(1, 1) : FishStockPart.Look(p.StockLeft, p.Capacity);
            string next = p.Held >= MaxHeld ? " It is full: it catches no more until you empty it." : NextFish(zone, now, p, " The next fish is ");
            return "Holds " + p.Held + " fish (it takes " + MaxHeld + "). The water beside it looks " + look + "." + next;
        }

        /// <summary>How to use the trap, what it needs and what it gives, from the live constants (the owner, 2026-11-21). The "right now" line is built from <see cref="AddRows"/>'s rows.</summary>
        public void DescribeUse(DescribeContext context, UseGuide guide)
        {
            bool standing = !PackableFixturePart.IsInertKit(ParentEntity) && ParentEntity?.SpatialZone != null;
            guide.How.Add(standing ? "Wait while it fills, then press C beside it and choose take the fish." : "Set it down on the shore of a pond, wait, then press C beside it and choose take the fish.");
            if (standing) guide.Needs.Add("a pond beside it big enough to fish: a wide pool of falling water, such as a cascade basin");   // a carried kit's ground text already says it
            guide.Limits.Add("It catches about one fish every half day, up to " + MaxHeld + " at a time, from the pond's own supply, which every trap on that water shares; it uses nothing up. A fished-out pond gives one fish a day again.");
            guide.WhyNot = "you are not beside it";
        }

        /// <summary>When the next fish is due, in the time of day the sidebar shows and never a tick count: "due soon" when about to come, else "due around Dusk". Empty when nothing more is coming
        /// (full trap, or a pond fished out for now: the water's own look already says that).</summary>
        string NextFish(Zone zone, int now, Projection p, string lead)
        {
            if (!p.Pond || p.Held >= MaxHeld || p.StockLeft == 0) return "";
            int remaining = CatchTicks - p.Progress; if (remaining < 0) remaining = 0;
            if (remaining <= SoonTicks) return lead + "due soon.";
            return lead + "due around " + WorldClock.BandName(WorldClock.GetBand(now + remaining), WorldMap.GetDepth(zone?.ZoneID)) + ".";
        }

        // ── the verb ──

        public override bool HandleEvent(GameEvent e)
        {
            // Taking the trap up (or moving it) brings it up to date first, so the fish it caught while it stood are in it, and in the pond's ledger, before the clock stops. It never vetoes.
            if (e.ID == InstallEvents.BeforeUninstall) { Reconcile(e.GetParameter<Zone>("Zone"), WorldClock.CurrentTick); return true; }
            if (e.ID == "GetInventoryActions") { AddRows(e.GetParameter<Entity>("Actor"), e.GetParameter<Zone>("Zone") ?? e.GetParameter<Entity>("Actor")?.SpatialZone, e.GetParameter<InventoryActionList>("Actions")); return true; }
            if (e.ID == "InventoryAction" && e.GetStringParameter("Command") == Command)
            {
                var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone;
                if (Take(actor, zone, e.GetParameter<InventoryTransaction>("InventoryTransaction"))) { e.Handled = true; return false; }
            }
            return true;
        }

        void AddRows(Entity actor, Zone zone, InventoryActionList actions)
        {
            if (actions == null || actor == null || zone == null || !Standing(zone) || !WorldResourceActions.Nearby(actor, ParentEntity, zone)) return;
            var p = Project(zone, WorldClock.CurrentTick);
            const string hint = "Empties the trap into your pack. It catches again while it stands, up to four at a time, from the pond's own supply.";
            if (p.Held > 0) actions.AddActionWithHint("TakeTrapFish", "take the fish (" + p.Held + ")", Command, hint, 't', 20, false, ActionTime.OneAction);
            else if (!p.Pond) actions.AddDisabled("TakeTrapFish", "take the fish", "no-pond", "It is not beside a pond big enough to fish. Move it to the shore of fishable water, a cascade basin.", 't', 20);
            else actions.AddDisabled("TakeTrapFish", "take the fish", "empty", p.StockLeft == 0 ? "The pond is fished out for now. It fills slowly, one fish a day." : "Nothing in it yet. It catches at most one fish every half day." + NextFish(zone, WorldClock.CurrentTick, p, " The next is "), 't', 20);
        }

        bool Take(Entity actor, Zone zone, InventoryTransaction tx)
        {
            var pack = actor?.GetPart<InventoryPart>();
            if (actor == null || zone == null || pack == null || !Standing(zone) || !WorldResourceActions.Nearby(actor, ParentEntity, zone)) return Block(actor, "invalid-context", "You cannot reach the trap now.");
            int now = WorldClock.CurrentTick;
            var before = Project(zone, now); if (before.Held <= 0) return Block(actor, "empty", "There is nothing in the trap.");
            var factory = HarvestablePart.Factory; var fish = factory?.CreateEntity(FishBlueprint);
            if (fish == null) return Block(actor, "no-fish", "The trap holds nothing you can take.");
            int unit = Math.Max(1, HandlingService.GetWeight(fish)), room = pack.MaxWeight < 0 ? int.MaxValue : Math.Max(0, (pack.MaxWeight - pack.GetCarriedWeight()) / unit);
            int want = Math.Min(before.Held, Math.Min(room, fish.GetPart<StackerPart>()?.MaxStack ?? 1));
            if (want <= 0) return Block(actor, "no-room", "You cannot carry any more fish.");
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                if (!tx.TryClaim(ParentEntity, actor, Command) || !tx.TryClaim(actor, actor, Command)) return Block(actor, "in-progress", "The trap is already being worked.");
                int heldBefore = Held, progressBefore = Progress, lastBefore = LastTick;
                var stock = before.Anchor == null ? null : FishStockPart.For(zone, before.Anchor, now);
                int availBefore = stock?.Available ?? 0, stockLastBefore = stock?.LastTick ?? 0, capBefore = stock?.Capacity ?? 0;
                if (fish.GetPart<StackerPart>() != null) fish.GetPart<StackerPart>().StackCount = want;
                var receipt = InventoryTransferSnapshot.Capture(pack, fish);
                tx.Do(null, receipt.Restore);
                if (!receipt.Apply(() => pack.AddObject(fish)) || !receipt.ClaimChanges(tx, actor, Command)) return Block(actor, "no-room", "You cannot carry the fish.");
                tx.Do(() => { Reconcile(zone, now); Held = Math.Max(0, Held - want); },
                      () => { Held = heldBefore; Progress = progressBefore; LastTick = lastBefore; if (stock != null) { stock.Available = availBefore; stock.LastTick = stockLastBefore; stock.Capacity = capBefore; } });
                var owner = ParentEntity;
                tx.AfterCommit(() =>
                {
                    MessageLog.Add("You lift the weir and take " + want + (want == 1 ? " fish." : " fish.") + (before.Held > want ? " " + (before.Held - want) + " stay in the trap: your pack is full." : ""));
                    Diag.Record("furniture", "FishTaken", actor, owner, new { taken = want, left = Math.Max(0, before.Held - want) });
                });
                if (own) tx.Commit();
                return true;
            }
            finally { if (own) tx.Rollback(); }
        }

        bool Block(Entity actor, string code, string message)
        {
            MessageLog.Add(message);
            Diag.Record("furniture", "FishTrapRefused", actor: actor, target: ParentEntity, payload: new { code });
            return false;
        }
    }
}
