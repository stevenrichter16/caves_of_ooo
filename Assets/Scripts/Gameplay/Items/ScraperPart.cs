using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// The scraper (Docs/MACHINE-INVENTED-USES.md 3u): a flat iron blade for taking scale, crust and rime from where a place gives it off, by hand and at once. It is the settling tray's quick cousin. Beside a source (the
    /// same <see cref="ProofingSources"/> rule: a steam vent or ash bed, a frost vent or ice sheet, a pool of brine, acid or mire) it takes <b>one</b> level-one reagent of that source's element from the source's own shared
    /// stock (<see cref="SourceStockPart"/>, the supply every settling tray at that source draws on), so a scraper and a tray at one source share one finite supply and neither adds to it. Where the tray waits and is quiet,
    /// the scraper is immediate and loud: every scrape rings through the stone (<see cref="Noise.Mark"/>), which is how eyeless sound-hunters find you. Because the frost vents and ice sheets are underground, it is also how
    /// a player carries cold out of the deep, where a camp kit cannot be set. Nothing is spent but the time and the noise; the catch is unsellable like every free reagent.
    /// <para>It is used where the source is: press C beside the vent, pool or ice and the source's own menu offers "scrape ..." (<see cref="AddCellRows"/>), run by the scraper in the pack, so the pack is never opened. The row names
    /// the exact source it was drawn for. The pack menu likewise quotes the nearest visible, reachable source within two cells.</para>
    /// </summary>
    public sealed class ScraperPart : Part, IDescribeUse, IInventoryActionReceiptOwner
    {
        public override string Name => "Scraper";
        public const string Command = "ScrapeSource";

        /// <summary>The world-menu form of the command: this prefix followed by the exact source's escaped ID.</summary>
        public const string CellPrefix = Command + "|";
        const string PackPrefix = "ScrapePack|";
        const string SkimPrefix = "SkimSource|";

        public bool OwnsInventoryReceipt(string command) => ParentEntity?.GetPart<ScraperPart>() == this
            && (command == Command || command?.StartsWith(CellPrefix, StringComparison.Ordinal) == true
                || command?.StartsWith(PackPrefix, StringComparison.Ordinal) == true);

        internal static bool IsSkimCommand(string command) => command?.StartsWith(SkimPrefix, StringComparison.Ordinal) == true;

        internal static void AddSkimRows(Entity tool, GameEvent e)
        {
            var actor = e.GetParameter<Entity>("Actor");
            var zone = e.GetParameter<Zone>("Zone");
            var cell = e.GetParameter<Cell>("Cell");
            var actions = e.GetParameter<InventoryActionList>("Actions");
            if (actions == null || !RouteWorkMenu.Visible(cell) || !CanAct(actor, zone)
                || !MaterialCuttingWorkReceipt.Usable(actor, tool, zone)) return;
            foreach (var source in cell.Objects)
            {
                if (!BesidePool(actor, zone, source) || PoolElement(source) is not { } kind) continue;
                var holder = ProofingSources.StockHolder(zone, source, kind);
                var stock = holder.GetPart<SourceStockPart>();
                var label = "skim " + SettlingTrayPart.ReagentName(kind) + " from the " + source.GetDisplayName();
                var command = $"{SkimPrefix}{Uri.EscapeDataString(source.ID)}|{zone.ZoneID}|{cell.X}|{cell.Y}";
                if (stock == null || stock.AvailableAt(WorldClock.CurrentTick) > 0)
                    actions.AddActionWithHint("SkimSource", label, command,
                        "One action for one crust from the shared deposit. Leaves the liquid and makes noise.", '\0', 20, false, ActionTime.OneAction);
                else
                    actions.AddDisabled("SkimSource", label, "spent", SpentReason(source), '\0', 20);
            }
        }

        internal static bool Skim(Entity tool, GameEvent e)
        {
            var actor = e.GetParameter<Entity>("Actor");
            var zone = e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone;
            var selected = e.GetStringParameter("Command").Split('|');
            if (selected.Length != 5 || zone?.ZoneID != selected[2] || !int.TryParse(selected[3], out int x)
                || !int.TryParse(selected[4], out int y) || zone.GetCell(x, y) is not { } cell)
                return false;
            return Scrape(tool, actor, zone, e.GetParameter<InventoryTransaction>("InventoryTransaction"), selected[1], 1, cell, skim: true);
        }

        static ProofingElement PoolElement(Entity source)
        {
            var kind = ProofingSources.ElementOf(source);
            return kind != null && string.IsNullOrEmpty(kind.SourceTile) && source.HasTag("Terrain")
                && source.GetPart<PhysicsPart>() is { Takeable: false }
                && source.GetPart<RenderPart>() is { Visible: true }
                && source.Parts.Count(part => part is LiquidPoolPart) == 1 ? kind : null;
        }

        static bool BesidePool(Entity actor, Zone zone, Entity source)
            => WorldResourceActions.Ground(source, zone) && RouteWorkMenu.Visible(zone.GetEntityCell(source))
                && SpatialQuery.Distance(zone, actor, source) == 1;

        const string Hint = "Takes one grain at once from the place's own supply, the one a settling tray at the same place draws on. It rings through the stone: eyeless hunters will hear it.";

        public int Order => 30;

        /// <summary>How to use it, what it needs and what it costs, from the live constants (the owner, 2026-11-21); the "right now" line is built from <see cref="AddRow"/>'s own row.</summary>
        public void DescribeUse(DescribeContext context, UseGuide guide)
        {
            guide.How.Add("Stand beside a visible source, open its interact (C) menu and choose scrape. The pack quotes the nearest visible source within " + ProofingSources.Reach + " cells with a clear path.");
            guide.Needs.Add("a source within one step for Interact: a steam vent or ash bed (heat), a frost vent or ice sheet (cold), or a pool of brine, acid or mire");
            guide.Needs.Add("the scraper in your pack");
            guide.Limits.Add("One grain a scrape, from the source's own supply (" + SourceStockPart.MinCapacity + " to " + SourceStockPart.MaxCapacity + " grains, one more each day), the same supply a settling tray there draws on.");
            guide.Limits.Add("Every scrape rings through the stone: eyeless hunters hear it. What you scrape cannot be sold.");
            var actor = context.Viewer; if (actor == null) return;
            guide.WhyNot = !UseGuides.CarriedBy(actor, ParentEntity) ? UseGuides.PickUpFirst : actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true ? "you cannot act just now"
                : "no steam vent, ash bed, frost vent, ice sheet or pool of brine, acid or mire is within " + ProofingSources.Reach + " cells that you can see and reach";
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions") { AddRow(e.GetParameter<Entity>("Actor"), e.GetParameter<Zone>("Zone") ?? e.GetParameter<Entity>("Actor")?.SpatialZone, e.GetParameter<InventoryActionList>("Actions")); return true; }
            if (e.ID == WorldInteractionSystem.WorldCellActionsEvent) { AddCellRows(e.GetParameter<Entity>("Actor"), e.GetParameter<Zone>("Zone"), e.GetParameter<Cell>("Cell"), e.GetParameter<InventoryActionList>("Actions")); return true; }
            if (e.ID == "InventoryAction")
            {
                var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone;
                if (Parse(e.GetStringParameter("Command"), zone, out string sourceId, out int reach, out Cell selectedCell)
                    && Scrape(ParentEntity, actor, zone, e.GetParameter<InventoryTransaction>("InventoryTransaction"), sourceId, reach, selectedCell))
                { e.Handled = true; return false; }
            }
            return true;
        }

        static string Quote(string prefix, Zone zone, Entity source)
        {
            var cell = zone.GetEntityCell(source);
            return prefix + Uri.EscapeDataString(source.ID) + "|" + Uri.EscapeDataString(zone.ZoneID) + "|" + cell.X + "|" + cell.Y;
        }

        static bool Parse(string command, Zone zone, out string sourceId, out int reach, out Cell selectedCell)
        {
            sourceId = null; reach = ProofingSources.Reach; selectedCell = null;
            if (command == Command) return true; // Legacy caller without a stored selection.
            if (command == null) return false;
            bool interact = command.StartsWith(CellPrefix, StringComparison.Ordinal);
            if (!interact && !command.StartsWith(PackPrefix, StringComparison.Ordinal)) return false;
            var fields = command.Split('|');
            if (interact)
            {
                reach = 1;
                if (fields.Length == 2)
                {
                    sourceId = fields[1];
                    return true; // Older exact-ID interaction.
                }
            }
            if (fields.Length != 5 || zone == null || WorldResourceActions.Decode(fields[2]) != zone.ZoneID
                || !int.TryParse(fields[3], out int x) || !int.TryParse(fields[4], out int y)) return false;
            sourceId = fields[1];
            selectedCell = zone.GetCell(x, y);
            return selectedCell != null;
        }

        static bool CanAct(Entity actor, Zone zone) => WorldResourceActions.ActorCurrent(actor, zone)
            && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true;

        static bool CanReachSource(Zone zone, Cell origin, Entity source, int reach)
        {
            if (origin == null || !WorldResourceActions.Ground(source, zone)
                || source.GetPart<RenderPart>() is not { Visible: true } render || render.ParentEntity != source) return false;
            var at = zone.GetEntityCell(source);
            return RouteWorkMenu.Visible(at) && Math.Max(Math.Abs(at.X - origin.X), Math.Abs(at.Y - origin.Y)) <= reach
                && AIHelpers.HasLineOfSight(zone, origin.X, origin.Y, at.X, at.Y);
        }

        static Entity FindPackSource(Zone zone, Cell origin, out ProofingElement kind)
            => ProofingSources.Find(zone, origin, out kind, e => CanReachSource(zone, origin, e, ProofingSources.Reach));

        bool Usable(Entity actor, Zone zone) => CanAct(actor, zone) && WorldResourceActions.Carried(actor, ParentEntity, single: true);

        /// <summary>The row's words for one source: what it would take and from what, and how much the source has left (read, never made: an untouched source's stock is not created by looking).</summary>
        static string Label(Zone zone, Entity near, ProofingElement kind, out int left)
        {
            var holder = ProofingSources.StockHolder(zone, near, kind); var stock = holder.GetPart<SourceStockPart>();
            left = stock != null ? stock.AvailableAt(WorldClock.CurrentTick) : SourceStockPart.CapacityFor(zone, holder);
            return "scrape " + SettlingTrayPart.ReagentName(kind) + " from the " + near.GetDisplayName();
        }

        static string SpentReason(Entity near) => "The " + near.GetDisplayName() + " is spent for now. It gives a little more each day.";

        void AddRow(Entity actor, Zone zone, InventoryActionList actions)
        {
            if (actions == null || !Usable(actor, zone)) return;
            var near = FindPackSource(zone, zone.GetEntityCell(actor), out var kind); if (near == null) return;
            string label = Label(zone, near, kind, out int left);
            if (left > 0) actions.AddActionWithHint("ScrapeSource", label, Quote(PackPrefix, zone, near), Hint, 's', 20, false, ActionTime.OneAction);
            else actions.AddDisabled("ScrapeSource", label, "spent", SpentReason(near), 's', 20);
        }

        /// <summary>
        /// The scrape rows of one cell's interact (C) menu, offered when the world-cell event asks this scraper about a cell (<see cref="WorldInteractionSystem.AppendToolActions"/> fires it on every carried item, runs the row through
        /// the scraper and fills in the rest): for each source in the cell that is beside you (or under you), one row that names that exact source, so the pack is never opened. A source with nothing left is a disabled row that says so.
        /// Nothing is added for a scraper that cannot be used now (not carried, the actor blocked or not current), for a cell more than one step away, or for what is not a source (a pool a flask made, one drawn dry, a creature).
        /// </summary>
        void AddCellRows(Entity actor, Zone zone, Cell cell, InventoryActionList actions)
        {
            if (actions == null || cell == null || !Usable(actor, zone)) return;
            var origin = zone.GetEntityCell(actor);
            foreach (var e in cell.Objects.ToArray())
            {
                var kind = ProofingSources.ElementOf(e); if (kind == null || !CanReachSource(zone, origin, e, 1)) continue;
                string label = Label(zone, e, kind, out int left);
                if (left > 0) actions.AddActionWithHint("ScrapeSource", label, Quote(CellPrefix, zone, e), Hint, '\0', 20, false, ActionTime.OneAction);
                else actions.AddDisabled("ScrapeSource", label, "spent", SpentReason(e), '\0', 20);
            }
        }

        /// <summary>Draw one grain from the quoted source, or use the nearest reachable source for the legacy unquoted command.</summary>
        static bool Scrape(Entity tool, Entity actor, Zone zone, InventoryTransaction tx, string sourceId, int reach,
            Cell selectedCell = null, bool skim = false)
        {
            var pack = actor?.GetPart<InventoryPart>();
            if (!CanAct(actor, zone) || pack == null || !skim && !WorldResourceActions.Carried(actor, tool))
                return Block(tool, actor, "invalid-context", "You cannot scrape anything now.");
            var origin = zone.GetEntityCell(actor);
            int now = WorldClock.CurrentTick; Entity near; ProofingElement kind;
            if (sourceId == null)
            {
                near = FindPackSource(zone, origin, out kind);
            }
            else
            {
                near = WorldResourceActions.ExactGround(zone, sourceId);
                kind = skim ? PoolElement(near) : ProofingSources.ElementOf(near);
            }
            if (near == null || kind == null) return Block(tool, actor, "no-source",
                sourceId == null ? "There is nothing here to scrape." : "There is nothing there to scrape.");
            bool ReachCurrent() => (selectedCell == null || zone.GetEntityCell(near) == selectedCell)
                && (skim ? BesidePool(actor, zone, near) : CanReachSource(zone, origin, near, reach));
            if (!ReachCurrent()) return Block(tool, actor, "out-of-reach", "That source is no longer within reach.");

            var blade = skim ? MaterialCuttingWorkReceipt.ForTool(actor, near, tool, zone) : null;
            if (skim && blade == null) return Block(tool, actor, "invalid-tool", "You need a sound blade to skim the pool.");
            var holder = ProofingSources.StockHolder(zone, near, kind);
            var sourceCell = zone.GetEntityCell(near);
            var priorStock = holder.GetPart<SourceStockPart>();
            bool ContextCurrent() => CanAct(actor, zone) && actor.GetPart<InventoryPart>() == pack
                && zone.GetEntityCell(actor) == origin && zone.GetEntityCell(near) == sourceCell && ReachCurrent()
                && (sourceId == null || WorldResourceActions.ExactGround(zone, sourceId) == near)
                && (skim ? blade.Current() && PoolElement(near) == kind : WorldResourceActions.Carried(actor, tool) && ProofingSources.ElementOf(near) == kind)
                && ProofingSources.StockHolder(zone, near, kind) == holder;
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                if (!tx.TryClaim(actor, actor, Command) || !tx.TryClaim(holder, actor, Command)
                    || (skim ? !blade.Begin(tx, Command) || !tx.TryClaim(near, actor, Command) : !tx.TryClaim(tool, actor, Command)))
                    return Block(tool, actor, "in-progress", "That is already being worked.");
                if (priorStock?.AvailableAt(now) <= 0) return Block(tool, actor, "spent", "The " + near.GetDisplayName() + " is spent for now.");
                var reagent = HarvestablePart.Factory?.CreateEntity(kind.Collects);
                if (reagent == null) return Block(tool, actor, "no-reagent", "The place gives you nothing you can take.");
                bool OriginalCrust(Entity item) => item?.BlueprintName == kind.Collects && item.HasTag("NoTrade")
                    && item.GetPart<ReagentPart>() is { } reagentPart && reagentPart.ParentEntity == item;
                if (!OriginalCrust(reagent))
                    return Block(tool, actor, "no-reagent", "The source gives you nothing you can take.");
                if (!ContextCurrent() || holder.GetPart<SourceStockPart>() != priorStock)
                    return Block(tool, actor, "changed-source", "The source changed before you could scrape it.");
                if (priorStock?.AvailableAt(now) <= 0) return Block(tool, actor, "spent", "The " + near.GetDisplayName() + " is spent for now.");
                var stock = SourceStockPart.For(zone, holder, now);
                int available = stock.Available, last = stock.LastTick;
                stock.Reconcile(now);
                int paid = --stock.Available, paidTick = stock.LastTick;
                InventoryUnitOutputReceipt output = null;
                tx.Do(null, () =>
                {
                    if (holder.GetPart<SourceStockPart>() != stock || output is { RetractedUnit: false }) return;
                    if (stock.Available == paid && stock.LastTick == paidTick)
                    {
                        if (priorStock == null) holder.RemovePart(stock);
                        else { stock.Available = available; stock.LastTick = last; }
                    }
                    else stock.Available = WaterQuantityUndo.Restore(stock.Available, -1, stock.Capacity);
                });
                if (!InventoryUnitOutputReceipt.TryPlace(pack, actor, reagent, tx, Command, out output))
                    return Block(tool, actor, "no-room", "You cannot carry any more.");
                tx.BeforeCommit(() => WorldClock.CurrentTick == now && ContextCurrent()
                    && holder.GetPart<SourceStockPart>() == stock && stock.Available == paid && stock.LastTick == paidTick
                    && output.Current() && OriginalCrust(output.Recipient));
                string label = SettlingTrayPart.ReagentName(kind), source = near.GetDisplayName(); string element = kind.Id;
                tx.AfterCommit(() =>
                {
                    SettlingTrayPart.InvalidateVisibleTrays(zone);
                    Noise.Mark(actor);   // the strike is heard, whatever follows
                    MessageLog.Add("You " + (skim ? "skim " : "scrape ") + label + " from the " + source + ". It rings.");
                    Diag.Record("furniture", "SourceScraped", actor, near, new { element, left = stock.Available });
                });
                if (own) tx.Commit();
                return true;
            }
            finally { if (own) tx.Rollback(); }
        }

        static bool Block(Entity tool, Entity actor, string code, string message)
        {
            MessageLog.Add(message);
            Diag.Record("furniture", "ScraperRefused", actor: actor, target: tool, payload: new { code });
            return false;
        }
    }
}
