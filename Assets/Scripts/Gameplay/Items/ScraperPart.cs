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
    /// the exact source it was drawn for, so a scrape comes from the one the player chose, not from whichever happens to be nearest. The pack menu keeps its own row for the nearest source within two cells.</para>
    /// </summary>
    public sealed class ScraperPart : Part, IDescribeUse
    {
        public override string Name => "Scraper";
        public const string Command = "ScrapeSource";

        /// <summary>The world-menu form of the command: this prefix followed by the exact source's escaped ID.</summary>
        public const string CellPrefix = Command + "|";

        const string Hint = "Takes one grain at once from the place's own supply, the one a settling tray at the same place draws on. It rings through the stone: eyeless hunters will hear it.";

        public int Order => 30;

        /// <summary>How to use it, what it needs and what it costs, from the live constants (the owner, 2026-11-21); the "right now" line is built from <see cref="AddRow"/>'s own row.</summary>
        public void DescribeUse(DescribeContext context, UseGuide guide)
        {
            guide.How.Add("Stand beside a source, open its interact (C) menu and choose scrape; the pack's scrape row also works on the nearest source within " + ProofingSources.Reach + " cells.");
            guide.Needs.Add("a source within one step of you: a steam vent or ash bed (heat), a frost vent or ice sheet (cold), or a pool of brine, acid or mire");
            guide.Needs.Add("the scraper in your pack");
            guide.Limits.Add("One grain a scrape, from the source's own supply (" + SourceStockPart.MinCapacity + " to " + SourceStockPart.MaxCapacity + " grains, one more each day), the same supply a settling tray there draws on.");
            guide.Limits.Add("Every scrape rings through the stone: eyeless hunters hear it. What you scrape cannot be sold.");
            var actor = context.Viewer; if (actor == null) return;
            guide.WhyNot = !UseGuides.CarriedBy(actor, ParentEntity) ? UseGuides.PickUpFirst : actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true ? "you cannot act just now"
                : "no steam vent, ash bed, frost vent, ice sheet or pool of brine, acid or mire is within " + ProofingSources.Reach + " cells of you";
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions") { AddRow(e.GetParameter<Entity>("Actor"), e.GetParameter<Zone>("Zone") ?? e.GetParameter<Entity>("Actor")?.SpatialZone, e.GetParameter<InventoryActionList>("Actions")); return true; }
            if (e.ID == WorldInteractionSystem.WorldCellActionsEvent) { AddCellRows(e.GetParameter<Entity>("Actor"), e.GetParameter<Zone>("Zone"), e.GetParameter<Cell>("Cell"), e.GetParameter<InventoryActionList>("Actions")); return true; }
            if (e.ID == "InventoryAction" && Parse(e.GetStringParameter("Command"), out string sourceId))
            {
                var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone;
                if (Scrape(actor, zone, e.GetParameter<InventoryTransaction>("InventoryTransaction"), sourceId)) { e.Handled = true; return false; }
            }
            return true;
        }

        /// <summary>Whether a command is this part's: the plain pack form (<paramref name="sourceId"/> null: the nearest source) or the world-menu form naming one source's escaped ID (an empty ID is still this part's, and is refused).</summary>
        static bool Parse(string command, out string sourceId)
        {
            sourceId = null; if (command == Command) return true;
            if (command == null || !command.StartsWith(CellPrefix, StringComparison.Ordinal)) return false;
            sourceId = command.Substring(CellPrefix.Length); return true;
        }

        bool Usable(Entity actor, Zone zone) => actor != null && zone != null && ParentEntity != null && WorldResourceActions.ActorCurrent(actor, zone)
            && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true && WorldResourceActions.Carried(actor, ParentEntity, single: true);

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
            var near = ProofingSources.Find(zone, zone.GetEntityCell(actor), out var kind); if (near == null) return;
            string label = Label(zone, near, kind, out int left);
            if (left > 0) actions.AddActionWithHint("ScrapeSource", label, Command, Hint, 's', 20, false, ActionTime.OneAction);
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
            foreach (var e in cell.Objects.ToArray())
            {
                var kind = ProofingSources.ElementOf(e); if (kind == null || !WorldResourceActions.Nearby(actor, e, zone)) continue;
                string label = Label(zone, e, kind, out int left);
                if (left > 0) actions.AddActionWithHint("ScrapeSource", label, CellPrefix + Uri.EscapeDataString(e.ID), Hint, '\0', 20, false, ActionTime.OneAction);
                else actions.AddDisabled("ScrapeSource", label, "spent", SpentReason(e), '\0', 20);
            }
        }

        /// <summary>Scrapes one grain. <paramref name="sourceId"/> null takes it from the nearest source within two cells (the pack row); otherwise from exactly the source with that escaped ID, which must still be a source and still be beside the actor.</summary>
        bool Scrape(Entity actor, Zone zone, InventoryTransaction tx, string sourceId = null)
        {
            var pack = actor?.GetPart<InventoryPart>();
            if (!Usable(actor, zone) || pack == null) return Block(actor, "invalid-context", "You cannot scrape anything now.");
            int now = WorldClock.CurrentTick; Entity near; ProofingElement kind;
            if (sourceId == null)
            {
                near = ProofingSources.Find(zone, zone.GetEntityCell(actor), out kind); if (near == null) return Block(actor, "no-source", "There is nothing here to scrape.");
            }
            else
            {
                near = WorldResourceActions.ExactGround(zone, sourceId); kind = ProofingSources.ElementOf(near);
                if (near == null || kind == null) return Block(actor, "no-source", "There is nothing there to scrape.");
                if (!WorldResourceActions.Nearby(actor, near, zone)) return Block(actor, "out-of-reach", "The " + near.GetDisplayName() + " is out of reach.");
            }
            var holder = ProofingSources.StockHolder(zone, near, kind);
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                if (!tx.TryClaim(actor, actor, Command) || !tx.TryClaim(ParentEntity, actor, Command) || !tx.TryClaim(holder, actor, Command))
                    return Block(actor, "in-progress", "That is already being worked.");
                var origin = zone.GetEntityCell(actor);
                var sourceCell = zone.GetEntityCell(near);
                var priorStock = holder.GetPart<SourceStockPart>();
                if (priorStock?.AvailableAt(now) <= 0) return Block(actor, "spent", "The " + near.GetDisplayName() + " is spent for now.");
                var reagent = HarvestablePart.Factory?.CreateEntity(kind.Collects);
                if (reagent == null) return Block(actor, "no-reagent", "The place gives you nothing you can take.");
                var receipt = InventoryTransferSnapshot.Capture(pack, reagent);
                tx.Do(null, receipt.Restore);
                if (!receipt.Apply(() => pack.AddObject(reagent)) || !receipt.ClaimChanges(tx, actor, Command)) return Block(actor, "no-room", "You cannot carry any more.");
                if (!Usable(actor, zone) || actor.GetPart<InventoryPart>() != pack || zone.GetEntityCell(actor) != origin
                    || zone.GetEntityCell(near) != sourceCell || ProofingSources.ElementOf(near) != kind
                    || ProofingSources.StockHolder(zone, near, kind) != holder || holder.GetPart<SourceStockPart>() != priorStock)
                    return Block(actor, "changed-source", "The source changed before you could scrape it.");
                if (priorStock?.AvailableAt(now) <= 0) return Block(actor, "spent", "The " + near.GetDisplayName() + " is spent for now.");
                var stock = SourceStockPart.For(zone, holder, now);
                int available = stock.Available, last = stock.LastTick;
                tx.Do(null, () =>
                {
                    if (holder.GetPart<SourceStockPart>() != stock) return;
                    if (priorStock == null) holder.RemovePart(stock);
                    else { stock.Available = available; stock.LastTick = last; }
                });
                stock.Reconcile(now);
                stock.Available--;
                string label = SettlingTrayPart.ReagentName(kind), source = near.GetDisplayName(); string element = kind.Id;
                tx.AfterCommit(() =>
                {
                    SettlingTrayPart.InvalidateVisibleTrays(zone);
                    Noise.Mark(actor);   // the strike is heard, whatever follows
                    MessageLog.Add("You scrape " + label + " from the " + source + ". It rings.");
                    Diag.Record("furniture", "SourceScraped", actor, near, new { element, left = stock.Available });
                });
                if (own) tx.Commit();
                return true;
            }
            finally { if (own) tx.Rollback(); }
        }

        bool Block(Entity actor, string code, string message)
        {
            MessageLog.Add(message);
            Diag.Record("furniture", "ScraperRefused", actor: actor, target: ParentEntity, payload: new { code });
            return false;
        }
    }
}
