using System;
using CavesOfOoo.Core.Inventory;
namespace CavesOfOoo.Core
{
    internal static class DouseWorldActions
    {
        const string Prefix = "DouseWorld|";
        internal static bool IsCommand(string command) => command?.StartsWith(Prefix, StringComparison.Ordinal) == true;
        static bool Target(Entity actor, Entity target, Zone zone)
        {
            var heat = target?.GetPart<ThermalPart>(); var burn = target?.GetEffect<BurningEffect>();
            return WorldResourceActions.Nearby(actor, target, zone) && !target.HasTag("Creature") && !CombatSystem.IsDeathHandled(target)
                && !(target.GetPart<DestructiblePart>() is DestructiblePart structure && (structure.Gone || structure.HP <= 0))
                && heat?.ParentEntity == target && WorldResourceActions.Finite(heat.Temperature) && WorldResourceActions.Finite(heat.FlameTemperature)
                && burn != null && burn.Intensity > 0 && burn.Duration != 0;
        }
        internal static void AddActions(Entity actor, Entity vessel, Zone zone, InventoryActionList actions)
        {
            if (actions == null || !WorldResourceActions.ActorCurrent(actor, zone) || !WaterTransferActions.Vessel(actor, vessel, out int units, out _) || units == 0) return;
            foreach (var target in zone.GetReadOnlyEntities()) if (Target(actor, target, zone))
                actions.AddAction("DouseWorld", "douse " + target.GetDisplayName() + " (1 water)", Prefix + Uri.EscapeDataString(target.ID), '\0', 16);
        }
        internal static bool TryAct(Entity actor, Entity vessel, Zone zone, string command, InventoryTransaction tx)
        {
            if (!IsCommand(command)) return false; var fields = command.Split('|'); if (fields.Length != 2) return WorldResourceActions.Reject(actor, vessel, command, "malformed_selection");
            var target = WorldResourceActions.ExactGround(zone, fields[1]);
            if (!Target(actor, target, zone) || !WaterTransferActions.Vessel(actor, vessel, out int units, out _) || units == 0) return WorldResourceActions.Reject(actor, target, command, "invalid_douse_target_or_water");
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                if (!tx.TryClaim(actor, actor, command) || !tx.TryClaim(vessel, actor, command) || !tx.TryClaim(target, actor, command)) return false;
                var effects = target.GetPart<StatusEffectsPart>(); var burn = target.GetEffect<BurningEffect>(); WetEffect wet = null;
                int index = -1; for (int i = 0; i < effects.EffectCount; i++) if (effects.GetAllEffects()[i] == burn) index = i;
                string cause = burn.LastRemovalCause; float moisture = 0;
                var heat = target.GetPart<ThermalPart>(); float temperature = heat.Temperature;
                WetEffect addedWet = null;
                tx.Do(null, () =>
                {
                    WaterTransferActions.SetUnits(vessel, units); heat.Temperature = temperature;
                    burn.LastRemovalCause = cause; effects.RestoreRemovedEffectForInventoryUndo(burn, index);
                    if (wet != null) wet.Moisture = moisture; else if (addedWet != null) effects.RemoveEffect(addedWet);
                });
                WaterTransferActions.SetUnits(vessel, units - 1);
                heat.Temperature = Math.Min(temperature, Math.Min(25f, heat.FlameTemperature - 1f));
                effects.RemoveEffect(burn);
                // Removal listeners may add their own wetness. Undo only this action's
                // dampening of the actual effect that exists after those listeners.
                wet = target.GetEffect<WetEffect>(); moisture = wet?.Moisture ?? 0;
                if (wet != null) wet.Moisture = Math.Max(wet.Moisture, 1f);
                else { addedWet = new WetEffect(1); target.ApplyEffect(addedWet); }
                tx.AfterCommit(() => { target.FireEvent("Extinguished"); ZoneRenderHooks.MarkCellDirty(zone.GetEntityCell(target), "Doused"); MessageLog.Add("You douse " + target.GetDisplayName() + " with one water."); });
                if (own) tx.Commit(); return true;
            }
            finally { if (own) tx.Rollback(); }
        }
    }
}
