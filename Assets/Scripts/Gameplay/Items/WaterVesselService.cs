using System;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Transactional water transfer; no arbitrary-liquid ingestion or mixing.</summary>
    public static class WaterVesselService
    {
        public static bool TryAct(Entity actor, Entity vessel, Zone zone, string command, InventoryTransaction transaction = null)
        {
            if (CombatSystem.IsDeathHandled(actor) || (actor?.GetStat("Hitpoints") is Stat hp && hp.Value <= 0))
                return Reject(actor, vessel, command, "actor_dead", "You cannot use water while dead.");
            var inventory = actor?.GetPart<InventoryPart>();
            var skin = vessel?.GetPart<WaterskinPart>();
            if (skin == null || inventory == null || !inventory.Objects.Contains(vessel)
                || (vessel.GetPart<StackerPart>()?.StackCount ?? 1) != 1)
                return Reject(actor, vessel, command, "not_carried", "You must carry that waterskin.");
            if (skin.Capacity <= 0 || skin.Charges < 0 || skin.Charges > skin.Capacity)
                return Reject(actor, vessel, command, "invalid_capacity", "That waterskin cannot hold water.");
            if (command != "FillWaterskin" && command != "DrinkWaterskin") return false;
            bool own = transaction == null;
            transaction ??= new InventoryTransaction();
            try
            {
                if (!transaction.TryClaim(actor, actor, command) || !transaction.TryClaim(vessel, actor, command))
                    return Reject(actor, vessel, command, "in_progress", "That waterskin is already in use.");
                int before = skin.Charges;
                if (command == "FillWaterskin")
                {
                    if (before == skin.Capacity) return Reject(actor, vessel, command, "full", "The waterskin is already full.");
                    Entity source = FindSource(actor, zone);
                    if (source == null) return Reject(actor, vessel, command, "no_fresh_water", "Stand beside a well, cistern, or pool of fresh water to fill it.");
                    if (!transaction.TryClaim(source, actor, command)) return Reject(actor, vessel, command, "source_in_use", "That water is already being drawn.");
                    var pool = source.GetPart<LiquidPoolPart>();
                    var sourceCell = zone.GetEntityCell(source);
                    int oldVolume = pool?.Volume ?? 0;
                    int amount = pool == null ? skin.Capacity - before : Math.Min(skin.Capacity - before, oldVolume);
                    transaction.Do(null, () => { skin.Charges = before; if (pool != null) pool.Volume = oldVolume; });
                    skin.Charges += amount;
                    if (pool != null) pool.Volume -= amount;
                    transaction.AfterCommit(() => LiquidVesselService.PublishCommittedPoolDraw(zone, source, pool, sourceCell));
                    int filled = skin.Charges;
                    transaction.AfterCommit(() => Diag.Record("event", "WaterskinFilled", actor, vessel,
                        new { source = source.BlueprintName, amount, remaining = filled }));
                    transaction.AfterCommit(() => MessageLog.Add("You fill the waterskin. (" + filled + "/" + skin.Capacity + " drinks)"));
                }
                else
                {
                    if (before == 0) return Reject(actor, vessel, command, "empty", "The waterskin is empty.");
                    var effects = actor.GetPart<StatusEffectsPart>();
                    var parched = effects?.GetEffect<ParchedEffect>();
                    int stacks = parched?.Stacks ?? 0;
                    string cause = parched?.LastRemovalCause;
                    int index = parched == null ? -1 : IndexOf(effects, parched);
                    // Undo only this drink's delta, so independent effect changes
                    // made while a caller owns the transaction survive rollback.
                    transaction.Do(null, () =>
                    {
                        skin.Charges = before;
                        if (parched == null) return;
                        var current = effects.GetEffect<ParchedEffect>();
                        int restoredStacks;
                        if (current == null)
                        {
                            // Only this drink's single relieved stack is ours to undo.
                            // An independent cure must not resurrect the old full depth.
                            restoredStacks = 1;
                            parched.Stacks = restoredStacks; parched.LastRemovalCause = cause;
                            effects.RestoreRemovedEffectForInventoryUndo(parched, index);
                        }
                        else
                        {
                            // A new exposure may have created a replacement instance,
                            // or already brought the remaining instance to its cap.
                            restoredStacks = current.Stacks < ParchedEffect.MaxStacks ? 1 : 0;
                            current.Stacks += restoredStacks;
                        }
                        int penalty = restoredStacks * ParchedEffect.PenaltyPerStack;
                        if (actor.GetStat("Strength") != null) actor.GetStat("Strength").Penalty += penalty;
                        if (actor.GetStat("Agility") != null) actor.GetStat("Agility").Penalty += penalty;
                    });
                    skin.Charges--;
                    ParchedEffect.ReduceOneStack(actor);
                    int remaining = skin.Charges;
                    transaction.AfterCommit(() => Diag.Record("event", "WaterskinDrunk", actor, vessel,
                        new { remaining, parchedRelieved = stacks > 0 }));
                    transaction.AfterCommit(() => MessageLog.Add("You drink water. (" + remaining + "/" + skin.Capacity + " drinks remain)"));
                }
                if (own) transaction.Commit();
                return true;
            }
            finally { if (own) transaction.Rollback(); }
        }

        private static int IndexOf(StatusEffectsPart effects, Effect target)
        { var all = effects.GetAllEffects(); for (int i = 0; i < all.Count; i++) if (ReferenceEquals(all[i], target)) return i; return -1; }
        private static Entity FindSource(Entity actor, Zone zone)
        {
            if (zone?.GetEntityCell(actor) == null) return null;
            foreach (var source in zone.GetReadOnlyEntities())
            {
                if (SpatialQuery.Distance(zone, actor, source) > 1 || source.HasTag("Creature")
                    || source.GetPart<PhysicsPart>()?.Takeable == true
                    || source.GetPart<PhysicsPart>()?.InInventory != null
                    || source.GetPart<PhysicsPart>()?.Equipped != null) continue;
                // A finite pool takes precedence over terrain's renewing coating.
                var pool = source.GetPart<LiquidPoolPart>();
                if (pool != null)
                { if (pool.LiquidId == "water" && pool.Volume > 0 && LiquidSourceSafety.IsUnmixedPool(zone, source)) return source; continue; }
                if (source.HasPart<WellPart>()) return source;
                var spring = source.GetPart<TileStateSourcePart>();
                if (spring?.Coating == "water" && spring.CoatingTurns > 0
                    && LiquidSourceSafety.IsUnmixedSource(zone, source, "water")) return source;
            }
            return null;
        }
        private static bool Reject(Entity actor, Entity vessel, string command, string reason, string message)
        { MessageLog.Add(message); Diag.Record("event", "WaterskinRejected", actor, vessel, new { command, reason }); return false; }
    }
}
