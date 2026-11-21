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
    /// </summary>
    public sealed class ScraperPart : Part, IDescribeState
    {
        public override string Name => "Scraper";
        public const string Command = "ScrapeSource";

        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 30;
        public string DescribeState(DescribeContext context)
            => "A flat iron blade. Beside a steam vent or ash bed, a frost vent or ice sheet, or a pool of brine, acid or mire, it takes one grain of what the place gives off, from the same supply a settling tray draws on. Every scrape is loud: eyeless hunters hear it.";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions") { AddRow(e.GetParameter<Entity>("Actor"), e.GetParameter<Zone>("Zone") ?? e.GetParameter<Entity>("Actor")?.SpatialZone, e.GetParameter<InventoryActionList>("Actions")); return true; }
            if (e.ID == "InventoryAction" && e.GetStringParameter("Command") == Command)
            {
                var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone;
                if (Scrape(actor, zone, e.GetParameter<InventoryTransaction>("InventoryTransaction"))) { e.Handled = true; return false; }
            }
            return true;
        }

        bool Usable(Entity actor, Zone zone) => actor != null && zone != null && ParentEntity != null && WorldResourceActions.ActorCurrent(actor, zone)
            && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true && WorldResourceActions.Carried(actor, ParentEntity, single: true);

        void AddRow(Entity actor, Zone zone, InventoryActionList actions)
        {
            if (actions == null || !Usable(actor, zone)) return;
            var near = ProofingSources.Find(zone, zone.GetEntityCell(actor), out var kind); if (near == null) return;
            var holder = ProofingSources.StockHolder(zone, near, kind); var stock = holder.GetPart<SourceStockPart>();
            int left = stock != null ? stock.AvailableAt(WorldClock.CurrentTick) : SourceStockPart.CapacityFor(zone, holder);
            string label = "scrape " + SettlingTrayPart.ReagentName(kind) + " from the " + near.GetDisplayName();
            if (left > 0) actions.AddActionWithHint("ScrapeSource", label, Command, "Takes one grain at once from the place's own supply, the one a settling tray at the same place draws on. It rings through the stone: eyeless hunters will hear it.", 's', 20, false, ActionTime.OneAction);
            else actions.AddDisabled("ScrapeSource", label, "spent", "The " + near.GetDisplayName() + " is spent for now. It gives a little more each day.", 's', 20);
        }

        bool Scrape(Entity actor, Zone zone, InventoryTransaction tx)
        {
            var pack = actor?.GetPart<InventoryPart>();
            if (!Usable(actor, zone) || pack == null) return Block(actor, "invalid-context", "You cannot scrape anything now.");
            int now = WorldClock.CurrentTick;
            var near = ProofingSources.Find(zone, zone.GetEntityCell(actor), out var kind); if (near == null) return Block(actor, "no-source", "There is nothing here to scrape.");
            var holder = ProofingSources.StockHolder(zone, near, kind);
            var reagent = HarvestablePart.Factory?.CreateEntity(kind.Collects); if (reagent == null) return Block(actor, "no-reagent", "The place gives you nothing you can take.");
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                if (!tx.TryClaim(actor, actor, Command) || !tx.TryClaim(ParentEntity, actor, Command)) return Block(actor, "in-progress", "That is already being worked.");
                var stock = SourceStockPart.For(zone, holder, now); stock.Reconcile(now);
                if (stock.Available <= 0) return Block(actor, "spent", "The " + near.GetDisplayName() + " is spent for now.");
                int availBefore = stock.Available, lastBefore = stock.LastTick, capBefore = stock.Capacity;
                var receipt = InventoryTransferSnapshot.Capture(pack, reagent);
                tx.Do(null, receipt.Restore);
                if (!receipt.Apply(() => pack.AddObject(reagent)) || !receipt.ClaimChanges(tx, actor, Command)) return Block(actor, "no-room", "You cannot carry any more.");
                tx.Do(() => { stock.Available -= 1; }, () => { stock.Available = availBefore; stock.LastTick = lastBefore; stock.Capacity = capBefore; });
                string label = SettlingTrayPart.ReagentName(kind), source = near.GetDisplayName(); string element = kind.Id;
                tx.AfterCommit(() =>
                {
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
