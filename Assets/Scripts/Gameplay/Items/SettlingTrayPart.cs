using System;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// The settling tray (Docs/MACHINE-PROOFING.md, "the settling tray"): a shallow pan on a stand, set down within <see cref="ProofingSources.Reach"/> cells of a place that gives off one of the four elements (a steam
    /// vent or ash bed for heat, a frost vent or ice sheet for cold, a pool of brine for shock, a pool of acid or bog-mire for acid). While it stands it settles out, at most one level-one reagent of that
    /// element every <see cref="CatchTicks"/> and never more than <see cref="MaxHeld"/> at a time, drawing each from the source's own place-bound stock (<see cref="SourceStockPart"/>, shared by every tray at that
    /// source). It spends nothing and a spent source gives it nothing. One kit serves every element in the proofing table: it reads what the world declares, not names. A tray holding one element will not mix another
    /// into the same pan until it is emptied. Time is reconciled when it is read, emptied or taken up, never by a timer on a zone that is not loaded; reading it changes nothing.
    /// <para>Carried it is a kit (no rows, no catching); the clock starts at the set-down. What it holds stays in it when it is taken up.</para>
    /// </summary>
    public sealed class SettlingTrayPart : Part, IDescribeState, IDescribeUse
    {
        public override string Name => "SettlingTray";

        public const string Command = "GatherTray";
        public const int MaxHeld = 4, CatchTicks = WorldClock.DayLengthTicks / 2, SoonTicks = 120;

        /// <summary>The element id of what the pan holds (empty when it holds nothing), how many it holds, the tick it was last brought up to date (-1 never), and the part of a catch period already elapsed.</summary>
        public string Element = "";
        public int Held, LastTick = -1, Progress;

        bool Standing(Zone zone) => zone != null && ParentEntity?.SpatialZone == zone && !PackableFixturePart.IsInertKit(ParentEntity);

        /// <summary>The pure projection of the tray at <paramref name="now"/>: what it would hold, how far into the next catch it would be, and the source it draws from. Changes nothing.</summary>
        readonly struct Projection
        {
            public readonly int Held, Progress, Taken, StockLeft, Capacity; public readonly bool Source, Mixed; public readonly Entity Near, Holder; public readonly ProofingElement Kind;
            public Projection(int held, int progress, int taken, bool source, bool mixed, Entity near, Entity holder, ProofingElement kind, int stockLeft, int capacity)
            { Held = held; Progress = progress; Taken = taken; Source = source; Mixed = mixed; Near = near; Holder = holder; Kind = kind; StockLeft = stockLeft; Capacity = capacity; }
        }

        Projection Project(Zone zone, int now)
        {
            if (!Standing(zone)) return new Projection(Held, Progress, 0, false, false, null, null, null, 0, 0);
            int from = Math.Max(LastTick, ParentEntity.GetPart<PackableFixturePart>()?.SetTick ?? -1);
            var at = zone.GetEntityCell(ParentEntity);
            var near = ProofingSources.Find(zone, at, out var kind);
            if (near == null) return new Projection(Held, 0, 0, false, false, null, null, null, 0, 0);
            // The stock belongs to the whole source (the blob of cells around the nearest one), so every tray at it, from any edge, draws on the same supply.
            var holder = ProofingSources.StockHolder(zone, near, kind);
            bool mixed = Held > 0 && !string.IsNullOrEmpty(Element) && !string.Equals(Element, kind.Id, StringComparison.OrdinalIgnoreCase);
            // A source nobody has drawn on has no stock record yet and is full, at the capacity its stock will be made with (so what the reader promises is what the first gather gives).
            var stock = holder.GetPart<SourceStockPart>();
            int cap = stock?.Capacity ?? SourceStockPart.CapacityFor(zone, holder), available = stock != null ? stock.AvailableAt(now) : cap, left = available;
            // A pan that holds one element's catch settles nothing from a source of another until it is emptied.
            if (mixed) return new Projection(Held, 0, 0, true, true, near, holder, kind, left, cap);
            if (from < 0 || now <= from) return new Projection(Held, Progress, 0, true, false, near, holder, kind, left, cap);
            if (Held >= MaxHeld || available <= 0) return new Projection(Held, 0, 0, true, false, near, holder, kind, left, cap);
            int progress = Progress + (now - from), catches = progress / CatchTicks;
            if (catches <= 0) return new Projection(Held, progress, 0, true, false, near, holder, kind, left, cap);
            int take = Math.Min(Math.Min(catches, MaxHeld - Held), available);
            bool limited = take < catches || Held + take >= MaxHeld || available - take <= 0;
            return new Projection(Held + take, limited ? 0 : progress - catches * CatchTicks, take, true, false, near, holder, kind, available - take, cap);
        }

        /// <summary>How much the pan would hold now (read-only).</summary>
        public int HeldNow(Zone zone, int now) => Project(zone, now).Held;

        /// <summary>Brings the pan and its source up to date and commits the catch. Idempotent for the same tick. Returns how many it settled.</summary>
        public int Reconcile(Zone zone, int now)
        {
            if (!Standing(zone)) return 0;
            var p = Project(zone, now);
            int taken = 0;
            if (p.Source && !p.Mixed)
            {
                if (p.Taken > 0)
                {
                    var stock = SourceStockPart.For(zone, p.Holder, now); stock.Reconcile(now);
                    taken = Math.Min(p.Taken, Math.Min(MaxHeld - Held, stock.Available));
                    stock.Available -= taken;
                }
                // An emptied pan holds no element, so it is free to settle whatever source it stands by next; the clock toward the next catch keeps running.
                Held += taken; Progress = taken == p.Taken ? p.Progress : 0;
                if (taken > 0) Element = p.Kind.Id;
            }
            LastTick = now;
            return taken;
        }

        // ── reader ──

        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 40;

        /// <summary>What the element's catch is called, read from the blueprint's own display name. Reading never creates an entity (that would advance the factory's ID counter and fire creation events on every look).</summary>
        internal static string ReagentName(ProofingElement kind)
        {
            if (kind == null) return "reagent";
            var blueprint = string.IsNullOrEmpty(kind.Collects) ? null : HarvestablePart.Factory?.GetBlueprint(kind.Collects);
            if (blueprint != null && blueprint.Parts.TryGetValue("Render", out var render) && render.TryGetValue("DisplayName", out var name) && !string.IsNullOrEmpty(name)) return name;
            return kind.Label + " reagent";
        }

        public string DescribeState(DescribeContext context)
        {
            var zone = context.Zone ?? ParentEntity?.SpatialZone;
            var held = ProofingRegistry.ById(Element);
            string heldNote = Held > 0 ? " It holds " + Held + " " + ReagentName(held) + "." : "";
            if (!Standing(zone)) return Held > 0 ? heldNote.TrimStart() : null;
            int now = WorldClock.CurrentTick; var p = Project(zone, now);
            if (!p.Source) return "There is nothing near enough to settle from." + heldNote;
            string name = ReagentName(p.Kind), source = p.Near.GetDisplayName();
            if (p.Mixed) return "It holds " + Held + " " + ReagentName(held) + ", and the " + source + " beside it gives off something else. It settles nothing more until you empty it.";
            string look = SourceStockPart.Look(p.StockLeft, p.Capacity);
            string next = p.Held >= MaxHeld ? " It is full: it settles no more until you empty it." : NextCatch(now, p, " The next is ");
            return "Settling " + name + " from the " + source + ". Holds " + p.Held + " (it takes " + MaxHeld + "). The source looks " + look + "." + next;
        }

        /// <summary>How to use the tray, what it needs and what it gives, from the live constants (the owner, 2026-11-21). The "right now" line is built from <see cref="AddRows"/>'s rows.</summary>
        public void DescribeUse(DescribeContext context, UseGuide guide)
        {
            bool standing = !PackableFixturePart.IsInertKit(ParentEntity) && ParentEntity?.SpatialZone != null;
            guide.How.Add(standing ? "Wait while it settles, then press C beside it and choose gather."
                : "Set it down within " + ProofingSources.Reach + " cells of a source (the set-down row names what each cell would catch), wait, then press C beside it and choose gather.");
            guide.Needs.Add("a source within " + ProofingSources.Reach + " cells: a steam vent or ash bed (vent scale), a frost vent or ice sheet (rime flake), a pool of brine (brine crust), acid or mire (acid crust); one element to a tray");
            guide.Limits.Add("It settles one reagent about every half day, up to " + MaxHeld + " at a time, from the source's own supply (" + SourceStockPart.MinCapacity + " to " + SourceStockPart.MaxCapacity + " grains, one more each day), shared with the scraper and every other tray there. The catch cannot be sold.");
            guide.WhyNot = "you are not beside it";
        }

        /// <summary>When the next catch is due, in the time of day the sidebar shows and never a tick count; empty when nothing more is coming (full pan or a spent source: the look already says that).</summary>
        string NextCatch(int now, Projection p, string lead)
        {
            if (!p.Source || p.Held >= MaxHeld || p.StockLeft == 0) return "";
            int remaining = CatchTicks - p.Progress; if (remaining < 0) remaining = 0;
            if (remaining <= SoonTicks) return lead + "due soon.";
            return lead + "due around " + WorldClock.BandName(WorldClock.GetBand(now + remaining), WorldMap.GetDepth(ParentEntity?.SpatialZone?.ZoneID)) + ".";
        }

        // ── the verb ──

        public override bool HandleEvent(GameEvent e)
        {
            // Taking the tray up (or moving it) brings it up to date first, so what it settled while it stood is in it, and in the source's ledger, before the clock stops. It never vetoes.
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
            var kind = p.Held > 0 ? ProofingRegistry.ById(Element) ?? p.Kind : p.Kind;
            const string hint = "Empties the pan into your pack. It settles again while it stands, up to four at a time, from the source's own supply.";
            if (p.Held > 0) actions.AddActionWithHint("GatherTray", "gather the " + ReagentName(kind) + " (" + p.Held + ")", Command, hint, 'g', 20, false, ActionTime.OneAction);
            else if (!p.Source) actions.AddDisabled("GatherTray", "gather the reagent", "no-source", "There is nothing near enough to settle from. Move it within two cells of a steam vent, a frost vent, or a pool of brine, acid or mire.", 'g', 20);
            else actions.AddDisabled("GatherTray", "gather the reagent", "empty", p.StockLeft == 0 ? "The source is spent for now. It gives a little more each day." : "Nothing in it yet. It settles at most one every half day." + NextCatch(WorldClock.CurrentTick, p, " The next is "), 'g', 20);
        }

        bool Take(Entity actor, Zone zone, InventoryTransaction tx)
        {
            var pack = actor?.GetPart<InventoryPart>();
            if (actor == null || zone == null || pack == null || !Standing(zone) || !WorldResourceActions.Nearby(actor, ParentEntity, zone)) return Block(actor, "invalid-context", "You cannot reach the tray now.");
            int now = WorldClock.CurrentTick;
            var before = Project(zone, now); if (before.Held <= 0) return Block(actor, "empty", "There is nothing in the tray.");
            var kind = ProofingRegistry.ById(Element) ?? before.Kind;
            var factory = HarvestablePart.Factory; var reagent = kind == null ? null : factory?.CreateEntity(kind.Collects);
            if (reagent == null) return Block(actor, "no-reagent", "The tray holds nothing you can take.");
            int unit = Math.Max(1, HandlingService.GetWeight(reagent)), room = pack.MaxWeight < 0 ? int.MaxValue : Math.Max(0, (pack.MaxWeight - pack.GetCarriedWeight()) / unit);
            int want = Math.Min(before.Held, Math.Min(room, reagent.GetPart<StackerPart>()?.MaxStack ?? 1));
            if (want <= 0) return Block(actor, "no-room", "You cannot carry any more.");
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                if (!tx.TryClaim(ParentEntity, actor, Command) || !tx.TryClaim(actor, actor, Command)) return Block(actor, "in-progress", "The tray is already being worked.");
                string elementBefore = Element; int heldBefore = Held, progressBefore = Progress, lastBefore = LastTick;
                var stock = before.Holder == null || before.Mixed ? null : SourceStockPart.For(zone, before.Holder, now);
                int availBefore = stock?.Available ?? 0, stockLastBefore = stock?.LastTick ?? 0, capBefore = stock?.Capacity ?? 0;
                // Bring the pan and its source up to date first, so what you are given is exactly what the source gave (never more than the stock held).
                tx.Do(() => Reconcile(zone, now),
                      () => { Element = elementBefore; Held = heldBefore; Progress = progressBefore; LastTick = lastBefore; if (stock != null) { stock.Available = availBefore; stock.LastTick = stockLastBefore; stock.Capacity = capBefore; } });
                int give = Math.Min(want, Held); if (give <= 0) return Block(actor, "empty", "There is nothing in the tray.");
                if (reagent.GetPart<StackerPart>() != null) reagent.GetPart<StackerPart>().StackCount = give;
                var receipt = InventoryTransferSnapshot.Capture(pack, reagent);
                tx.Do(null, receipt.Restore);
                if (!receipt.Apply(() => pack.AddObject(reagent)) || !receipt.ClaimChanges(tx, actor, Command)) return Block(actor, "no-room", "You cannot carry it.");
                int heldAfterReconcile = Held;
                tx.Do(() => { Held = Math.Max(0, Held - give); if (Held == 0) Element = ""; }, () => { Held = heldAfterReconcile; });
                want = give;
                var owner = ParentEntity; string label = ReagentName(kind);
                tx.AfterCommit(() =>
                {
                    MessageLog.Add("You lift the pan and take " + want + " " + label + "." + (before.Held > want ? " " + (before.Held - want) + " stay in the tray: your pack is full." : ""));
                    Diag.Record("furniture", "TrayGathered", actor, owner, new { taken = want, left = Math.Max(0, before.Held - want), element = kind.Id });
                });
                if (own) tx.Commit();
                return true;
            }
            finally { if (own) tx.Rollback(); }
        }

        bool Block(Entity actor, string code, string message)
        {
            MessageLog.Add(message);
            Diag.Record("furniture", "SettlingTrayRefused", actor: actor, target: ParentEntity, payload: new { code });
            return false;
        }
    }
}
