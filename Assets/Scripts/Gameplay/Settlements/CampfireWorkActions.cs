using System;
using System.Linq;
using CavesOfOoo.Core.Inventory;
namespace CavesOfOoo.Core
{
    internal static class CampfireWorkActions
    {
        const string DryPrefix = "DryGear|";
        internal static bool IsCommand(string command) => command == "FeedCookingFire" || command == "RelightCookingFire" || command?.StartsWith(DryPrefix, StringComparison.Ordinal) == true;
        static bool Source(Entity actor, CampfirePart fire, Zone zone)
        {
            var owner = fire?.ParentEntity; var heat = owner?.GetPart<ThermalPart>();
            return WorldResourceActions.Nearby(actor, owner, zone) && owner.GetPart<CampfirePart>() == fire && !owner.GetPart<PhysicsPart>().Takeable
                && !owner.HasTag("Creature") && heat?.ParentEntity == owner && WorldResourceActions.Finite(heat.Temperature);
        }
        static bool Fuel(CampfirePart fire, out FuelPart fuel)
        {
            fuel = fire.ParentEntity.GetPart<FuelPart>();
            return fire.FiniteCooking && fuel?.ParentEntity == fire.ParentEntity && WorldResourceActions.Finite(fuel.FuelMass)
                && WorldResourceActions.Finite(fuel.MaxFuel) && fuel.FuelMass >= 0 && fuel.MaxFuel > 0 && fuel.FuelMass <= fuel.MaxFuel;
        }
        static bool Hot(CampfirePart fire) => fire.ParentEntity.GetPart<ThermalPart>().Temperature >= CookingService.MinimumFiniteCookingTemperature
            && (!fire.FiniteCooking || Fuel(fire, out var fuel) && fuel.FuelMass > 0);
        static Entity Timber(Entity actor) => actor.GetPart<InventoryPart>()?.Objects.FirstOrDefault(e => e.BlueprintName == "SalvagedTimber" && WorldResourceActions.Carried(actor, e, false));
        static bool Gear(Entity actor, Entity gear) => WorldResourceActions.Carried(actor, gear) && gear.GetEffect<WetEffect>() is WetEffect wet
            && WorldResourceActions.Finite(wet.Moisture) && wet.Moisture > 0 && !(gear.GetEffect<FrozenEffect>()?.Cold > 0);
        internal static void AddActions(Entity actor, CampfirePart fire, Zone zone, InventoryActionList actions)
        {
            if (actions == null || !Source(actor, fire, zone)) return;
            if (Hot(fire) && actor.GetPart<InventoryPart>() is InventoryPart pack)
                foreach (var gear in pack.Objects) if (Gear(actor, gear))
                    actions.AddAction("DryGear", "dry " + gear.GetDisplayName() + " (one quarter)", DryPrefix + Uri.EscapeDataString(gear.ID), '\0', 17);
            if (!Fuel(fire, out var fuel)) return;
            if (fuel.FuelMass < fuel.MaxFuel && Timber(actor) != null)
                actions.AddAction("FeedCookingFire", "feed coals (1 salvaged timber, up to 10 fuel)", "FeedCookingFire", '\0', 18);
            if (fuel.FuelMass > 0 && fire.ParentEntity.GetPart<ThermalPart>().Temperature < 450 && IgnitionSources.Find(actor, zone, fire.ParentEntity) != null)
                actions.AddAction("RelightCookingFire", "relight coals from flame", "RelightCookingFire", '\0', 18);
        }
        internal static bool TryAct(Entity actor, CampfirePart fire, Zone zone, string command, InventoryTransaction tx)
        {
            if (!IsCommand(command)) return false;
            if (!Source(actor, fire, zone)) return WorldResourceActions.Reject(actor, fire?.ParentEntity, command, "invalid_fire_context");
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                var owner = fire.ParentEntity;
                if (!tx.TryClaim(actor, actor, command) || !tx.TryClaim(owner, actor, command)) return false;
                if (command.StartsWith(DryPrefix, StringComparison.Ordinal))
                {
                    var fields = command.Split('|'); if (fields.Length != 2) return WorldResourceActions.Reject(actor, owner, command, "malformed_selection");
                    var gear = WorldResourceActions.ExactCarried(actor, fields[1]);
                    if (!Hot(fire) || !Gear(actor, gear)) return WorldResourceActions.Reject(actor, gear, command, "heat_or_dryable_gear_unavailable");
                    if (!tx.TryClaim(gear, actor, command)) return false;
                    var wet = gear.GetEffect<WetEffect>(); var effects = gear.GetPart<StatusEffectsPart>(); float before = wet.Moisture;
                    string cause = wet.LastRemovalCause; int index = 0;
                    for (int i = 0; i < effects.EffectCount; i++) if (effects.GetAllEffects()[i] == wet) index = i;
                    float removed = Math.Min(before, 0.25f);
                    tx.Do(null, () =>
                    {
                        var current = gear.GetEffect<WetEffect>();
                        if (current != null) current.Moisture = Math.Min(1f, current.Moisture + removed);
                        else { wet.Moisture = before; effects.RestoreRemovedEffectForInventoryUndo(wet, index); }
                        wet.LastRemovalCause = cause;
                    });
                    wet.Moisture = before - removed; if (wet.Moisture == 0) effects.RemoveEffect(wet);
                    tx.AfterCommit(() => MessageLog.Add("You dry " + gear.GetDisplayName() + " beside the heat."));
                }
                else
                {
                    if (!Fuel(fire, out var fuel)) return WorldResourceActions.Reject(actor, owner, command, "invalid_finite_fuel");
                    if (command == "FeedCookingFire")
                    {
                        var timber = Timber(actor);
                        if (timber == null || fuel.FuelMass >= fuel.MaxFuel) return WorldResourceActions.Reject(actor, owner, command, "timber_or_fuel_room_unavailable");
                        if (!tx.TryClaim(timber, actor, command)) return false;
                        var pack = actor.GetPart<InventoryPart>(); var receipt = InventoryTransferSnapshot.Capture(pack); float before = fuel.FuelMass;
                        tx.Do(null, () => { receipt.Restore(); fuel.FuelMass = before; });
                        if (!receipt.Apply(() => pack.TryConsumeOne(timber)) || !receipt.ClaimChanges(tx, actor, command)) return false;
                        if (!Source(actor, fire, zone) || !Fuel(fire, out var current) || current != fuel || fuel.FuelMass != before) return WorldResourceActions.Reject(actor, owner, command, "fire_changed_during_payment");
                        fuel.FuelMass = Math.Min(fuel.MaxFuel, before + 10);
                        tx.AfterCommit(() => MessageLog.Add("You feed one salvaged timber to the coals. The fuel still needs flame."));
                    }
                    else
                    {
                        var heat = owner.GetPart<ThermalPart>(); var source = IgnitionSources.Find(actor, zone, owner);
                        if (fuel.FuelMass <= 0 || heat.Temperature >= 450 || source == null) return WorldResourceActions.Reject(actor, owner, command, "fuel_or_ignition_unavailable");
                        if (!tx.TryClaim(source, actor, command)) return false;
                        float before = heat.Temperature; tx.Do(() => heat.Temperature = 450, () => heat.Temperature = before);
                        tx.AfterCommit(() => { ZoneRenderHooks.MarkCellDirty(zone.GetEntityCell(owner), "CookingRelit"); MessageLog.Add("You relight the cooking coals from the flame."); });
                    }
                }
                if (own) tx.Commit(); return true;
            }
            finally { if (own) tx.Rollback(); }
        }
    }
}
