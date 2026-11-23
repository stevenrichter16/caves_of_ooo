using System;
using System.Globalization;
using CavesOfOoo.Core.Inventory;
namespace CavesOfOoo.Core
{
    internal static class WellDrinkingService
    {
        const string LegacyCommand = "DrawWaterAtWell", MeteredPrefix = "DrawMeteredWaterAtWell|";
        internal static bool IsDrinkCommand(string command) => command == LegacyCommand || ParseQuote(command, out _, out _);
        static bool ParseQuote(string command, out int units, out int debt)
        {
            units = debt = 0;
            if (command == null || !command.StartsWith(MeteredPrefix, StringComparison.Ordinal)) return false;
            var fields = command.Substring(MeteredPrefix.Length).Split('|');
            return fields.Length == 2 && int.TryParse(fields[0], NumberStyles.None, CultureInfo.InvariantCulture, out units)
                && int.TryParse(fields[1], NumberStyles.None, CultureInfo.InvariantCulture, out debt)
                && units >= 1 && units <= ParchedEffect.MaxStacks && debt >= 0 && debt <= ParchedEffect.MaxStacks;
        }
        static bool IsMetered(WellPart well) => well.MeteredHydration
            && (well.ParentEntity.GetPart<LiquidPoolPart>() != null || well.Stock >= 0);
        static int Water(WellPart well) => well.ParentEntity.GetPart<LiquidPoolPart>()?.Volume ?? well.Stock;
        static bool ValidMeteredDebt(Entity actor) => HydrationReliefReceipt.ValidContext(actor);
        internal static void AddAction(Entity actor, WellPart well, Zone zone, InventoryActionList rows)
        {
            if (rows == null || !Available(actor, well, zone)) return;
            if (!IsMetered(well)) { rows.AddAction("Draw water", "draw water", LegacyCommand, 'w', 20, time: ActionTime.Free); return; }
            if (!ValidMeteredDebt(actor)) return;
            int debt = actor.GetEffect<ParchedEffect>()?.Stacks ?? 0;
            if (debt < 0 || debt > ParchedEffect.MaxStacks) return;
            int units = Math.Min(Water(well), Math.Max(1, debt));
            string command = MeteredPrefix + units.ToString(CultureInfo.InvariantCulture) + "|" + debt.ToString(CultureInfo.InvariantCulture);
            string hint = "Uses " + units + " water " + (units == 1 ? "unit" : "units") + " of " + Water(well) + " available; "
                + (debt == 0 ? "there is no parch to relieve" : "relieves " + Math.Min(units, debt) + " of " + debt + " levels of parch") + ". One action.";
            rows.AddActionWithHint("Draw water", "draw water (" + units + (units == 1 ? " unit)" : " units)"), command, hint, 'w', 20, time: ActionTime.OneAction);
        }
        internal static bool Available(Entity actor, WellPart well, Zone zone)
        {
            var owner = well?.ParentEntity;
            return WorldResourceActions.Nearby(actor, owner, zone) && SourceAvailable(well, zone)
                && HydrationReliefReceipt.ValidContext(actor) && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true;
        }
        // Source-only admission is shared with filling. Literal fill sources may
        // lack Physics; direct drinking still requires Nearby's physical owner.
        internal static bool SourceAvailable(WellPart well, Zone zone, bool requireWater = true)
        {
            var owner = well?.ParentEntity;
            if (owner == null || owner.GetPart<WellPart>() != well || owner.HasTag("Creature") || !well.IsUsable
                || !LiquidSourcePhase.CanDrawWater(zone, owner)) return false;
            var pool = owner.GetPart<LiquidPoolPart>();
            // Stock is a drawn supply, separate from spills around its base.
            // An attached pool/emitter instead exposes the actual source cell:
            // it must never turn unlike liquid into clean water or fall back.
            // See WellSupplyProvenanceTests and DENSITY-LIQUID-CARRY-POUR.md.
            if ((pool != null || owner.HasPart<TileStateSourcePart>())
                && !LiquidSourceSafety.IsUnmixedSource(zone, owner, "water")) return false;
            return pool == null ? (!well.MeteredHydration || well.Stock >= -1) && (!requireWater || well.HasWater)
                : pool.ParentEntity == owner && pool.LiquidId == "water" && (!requireWater || pool.Volume > 0);
        }
        internal static bool TryDrink(Entity actor, WellPart well, Zone zone, InventoryTransaction tx, string command)
        {
            if (!Available(actor, well, zone)) return WorldResourceActions.Reject(actor, well?.ParentEntity, command, "invalid_or_unavailable_water_source");
            if (TentRightOath.RefusesWater(actor, zone))
            {
                MessageLog.Add(TentRightOath.RefusedWaterLine);
                return WorldResourceActions.Reject(actor, well.ParentEntity, command, "oathbreaker_refused");
            }
            bool metered = IsMetered(well), configured = well.MeteredHydration;
            if (metered && !ValidMeteredDebt(actor)) return WorldResourceActions.Reject(actor, well.ParentEntity, command, "invalid_parched_owner_or_depth");
            int stacks = actor.GetEffect<ParchedEffect>()?.Stacks ?? 0;
            int units = metered ? Math.Min(Water(well), Math.Max(1, stacks)) : 1;
            if (metered ? !ParseQuote(command, out int quotedUnits, out int quotedDebt) || quotedUnits != units || quotedDebt != stacks : command != LegacyCommand)
                return WorldResourceActions.Reject(actor, well.ParentEntity, command, "water_quote_changed");
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                var owner = well.ParentEntity;
                if (!tx.TryClaim(actor, actor, command) || !tx.TryClaim(owner, actor, command)) return false;
                var thirst = actor.GetEffect<ParchedEffect>();
                var pool = owner.GetPart<LiquidPoolPart>(); var sourceCell = zone.GetEntityCell(owner); var actorCell = zone.GetEntityCell(actor);
                int volume = pool?.Volume ?? 0, stock = well.Stock;
                tx.Do(null, () =>
                {
                    if (pool != null) pool.Volume = WaterQuantityUndo.Restore(pool.Volume, -units);
                    else if (stock > 0) well.Stock = WaterQuantityUndo.Restore(well.Stock, -units, endlessAllowed: true);
                });
                if (pool != null) pool.Volume -= units;
                else if (well.Stock > 0) well.Stock -= units;
                if (!HydrationReliefReceipt.TryApply(actor, metered ? units : ParchedEffect.MaxStacks, tx, out var relief)) return false;
                // Expected exhaustion is valid. Recheck context without asking
                // our just-paid source to still hold another drink.
                tx.BeforeCommit(() => WorldResourceActions.Nearby(actor, owner, zone) && SourceAvailable(well, zone, false) && HydrationReliefReceipt.ValidContext(actor)
                    && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true
                    && !TentRightOath.RefusesWater(actor, zone) && well.MeteredHydration == configured && IsMetered(well) == metered
                    && zone.GetEntityCell(actor) == actorCell && zone.GetEntityCell(owner) == sourceCell
                    && owner.GetPart<LiquidPoolPart>() == pool
                    && (pool != null ? pool.Volume == volume - units : well.Stock == stock - (stock > 0 ? units : 0)));
                tx.AfterCommit(() => LiquidVesselService.PublishCommittedPoolDraw(zone, owner, pool, sourceCell));
                tx.AfterCommit(() => MessageLog.Add(metered ? "You drink " + units + (units == 1 ? " unit" : " units") + " of water."
                    + (relief.Relieved == 0 ? "" : " (" + relief.Relieved + (relief.Relieved == 1 ? " level" : " levels") + " of parch relieved.)")
                    : thirst != null ? "You draw cool water and drink until the parch lets go." : "You drink. The well water is cool."));
                tx.AfterCommit(() => CavesOfOoo.Diagnostics.Diag.Record("furniture", "WaterDrawn", actor, owner,
                    new { curedParched = thirst != null && relief.Relieved == stacks, relievedStacks = relief.Relieved, metered,
                        finiteUnitsSpent = pool != null || stock > 0 ? units : 0, stockLeft = well.Stock }));
                if (own) tx.Commit(); return true;
            }
            finally { if (own) tx.Rollback(); }
        }
    }
}
