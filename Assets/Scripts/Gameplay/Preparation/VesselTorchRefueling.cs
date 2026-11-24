using System;
using System.Globalization;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>One finite liquid-oil unit feeds an existing controlled torch.
    /// No new fuel item, flame authority or saved state.</summary>
    internal static class VesselTorchRefueling
    {
        const string Prefix = "RefuelVesselTorch|";
        const float FuelPerOil = 25;
        internal static bool IsCommand(string command) => command?.StartsWith(Prefix, StringComparison.Ordinal) == true;
        static T Unique<T>(Entity owner) where T : Part
        {
            var parts = owner?.Parts.OfType<T>().ToArray();
            return parts?.Length == 1 && parts[0].ParentEntity == owner ? parts[0] : null;
        }
        internal static bool OwnsReceipt(Entity owner, string command) => IsCommand(command) && Unique<LiquidVesselPart>(owner) != null;
        static bool Packed(Entity actor, Entity owner) => PreparationActions.Carried(actor, owner)
            && WorldResourceActions.Carried(actor, owner) && Unique<PhysicsPart>(owner) != null
            && owner.Parts.Count(p => p is StackerPart) <= 1;
        static bool Source(Entity actor, Entity owner, bool allowEmpty = false)
        {
            var liquid = Unique<LiquidVesselPart>(owner);
            return Packed(actor, owner) && liquid != null && !owner.HasPart<WaterskinPart>()
                && liquid.Capacity > 0 && liquid.Volume >= (allowEmpty ? 0 : 1) && liquid.Volume <= liquid.Capacity
                && (liquid.Volume == 0 ? liquid.LiquidId == "" : liquid.LiquidId == "oil")
                && LiquidRegistry.IsInitialized && LiquidRegistry.Get("oil") != null;
        }
        static bool Torch(Entity actor, Entity owner, bool allowFull = false)
        {
            var fuel = Unique<FuelPart>(owner); var heat = Unique<ThermalPart>(owner);
            return Packed(actor, owner) && Unique<TorchLightPart>(owner) != null
                && Unique<LightSourcePart>(owner) is LightSourcePart light && !light.Enabled
                && fuel != null && heat != null && WorldResourceActions.Finite(fuel.FuelMass)
                && WorldResourceActions.Finite(fuel.MaxFuel) && fuel.FuelMass >= 0 && fuel.MaxFuel > 0
                && (allowFull ? fuel.FuelMass <= fuel.MaxFuel : fuel.FuelMass < fuel.MaxFuel)
                && WorldResourceActions.Finite(fuel.BurnRate) && fuel.BurnRate > 0
                && WorldResourceActions.Finite(heat.Temperature) && WorldResourceActions.Finite(heat.FlameTemperature)
                && !(owner.GetStat("Hitpoints") is Stat hp && hp.Value <= 0);
        }
        static string Number(float n) => n.ToString("R", CultureInfo.InvariantCulture);
        static string Display(float n) => n.ToString("G5", CultureInfo.InvariantCulture);
        static string Quote(Entity source, Entity torch)
        {
            var liquid = source.GetPart<LiquidVesselPart>(); var fuel = torch.GetPart<FuelPart>();
            return Prefix + Uri.EscapeDataString(source.ID) + "|" + Uri.EscapeDataString(torch.ID) + "|"
                + liquid.Volume.ToString(CultureInfo.InvariantCulture) + "|" + liquid.Capacity.ToString(CultureInfo.InvariantCulture)
                + "|" + Number(fuel.FuelMass) + "|" + Number(fuel.MaxFuel);
        }
        internal static void AddActions(Entity actor, Entity source, Zone zone, InventoryActionList actions)
        {
            if (actions == null || !PreparationActions.CurrentActor(actor, zone) || !Source(actor, source)) return;
            foreach (var torch in actor.GetPart<InventoryPart>().Objects)
            {
                if (torch == source || !Torch(actor, torch)) continue;
                var fuel = torch.GetPart<FuelPart>(); float result = Math.Min(fuel.MaxFuel, fuel.FuelMass + FuelPerOil);
                if (result <= fuel.FuelMass) continue;
                actions.AddActionWithHint("RefuelVesselTorch", "1 oil: " + Display(fuel.FuelMass) + "→" + Display(result)
                    + "/" + Display(fuel.MaxFuel) + " fuel, " + InventoryPart.GetUnitDisplayName(torch), Quote(source, torch),
                    "Spend one liquid oil unit. Adds up to 25 fuel; any amount above the torch's capacity is lost. The torch stays out. Lamp-oil measures also work.",
                    '\0', 30, false, ActionTime.OneAction);
            }
        }
        internal static bool TryAct(Entity actor, Entity source, Zone zone, string command, InventoryTransaction tx)
        {
            if (!IsCommand(command) || tx == null || !PreparationActions.CurrentActor(actor, zone)
                || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true || !Source(actor, source)) return false;
            var fields = command.Split('|'); if (fields.Length != 7) return false;
            var torch = WorldResourceActions.ExactCarried(actor, fields[2]);
            if (torch == source || !Torch(actor, torch) || command != Quote(source, torch)) return false;
            if (!tx.TryClaim(actor, actor, command) || !tx.TryClaim(source, actor, command) || !tx.TryClaim(torch, actor, command)) return false;
            var liquid = source.GetPart<LiquidVesselPart>(); var fuel = torch.GetPart<FuelPart>();
            var light = torch.GetPart<LightSourcePart>(); var heat = torch.GetPart<ThermalPart>(); var flame = torch.GetPart<TorchLightPart>();
            var sourcePhysics = source.GetPart<PhysicsPart>(); var torchPhysics = torch.GetPart<PhysicsPart>();
            var cell = zone.GetEntityCell(actor); string sourceID = source.ID, torchID = torch.ID;
            int beforeOil = liquid.Volume, capacity = liquid.Capacity;
            float beforeFuel = fuel.FuelMass, maximum = fuel.MaxFuel;
            float result = Math.Min(maximum, beforeFuel + FuelPerOil), gain = result - beforeFuel;
            if (!(gain > 0)) return false;
            var oilDefinition = LiquidRegistry.Get("oil");
            tx.Do(() => { liquid.Volume--; liquid.LiquidId = liquid.Volume == 0 ? "" : "oil"; fuel.FuelMass = result; }, () =>
            {
                bool liquidOwned = Unique<LiquidVesselPart>(source) == liquid
                    && (liquid.LiquidId == "oil" || liquid.Volume == 0 && liquid.LiquidId == "");
                if (liquidOwned)
                {
                    liquid.Volume = WaterQuantityUndo.Restore(liquid.Volume, -1, Math.Min(capacity, Math.Max(0, liquid.Capacity)));
                    liquid.LiquidId = liquid.Volume == 0 ? "" : "oil";
                }
                else Diag.Record("event", "OilRefuelRollbackLimited", actor, source, new { reason = "source-part-or-liquid-changed" });
                if (Unique<FuelPart>(torch) == fuel && WorldResourceActions.Finite(fuel.FuelMass) && WorldResourceActions.Finite(fuel.MaxFuel))
                    fuel.FuelMass = (float)Math.Max(0, Math.Min(Math.Max(0, Math.Min(maximum, fuel.MaxFuel)), (double)fuel.FuelMass - gain));
                else Diag.Record("event", "OilRefuelRollbackLimited", actor, torch, new { reason = "fuel-part-or-scalar-changed" });
            });
            tx.BeforeCommit(() => PreparationActions.CurrentActor(actor, zone) && zone.GetEntityCell(actor) == cell
                && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true
                && source.ID == sourceID && torch.ID == torchID && Source(actor, source, true) && Torch(actor, torch, true)
                && Unique<LiquidVesselPart>(source) == liquid && Unique<FuelPart>(torch) == fuel
                && Unique<LightSourcePart>(torch) == light && Unique<ThermalPart>(torch) == heat && Unique<TorchLightPart>(torch) == flame
                && source.GetPart<PhysicsPart>() == sourcePhysics && torch.GetPart<PhysicsPart>() == torchPhysics
                && liquid.Volume == beforeOil - 1 && liquid.Capacity == capacity && fuel.FuelMass == result && fuel.MaxFuel == maximum
                && LiquidRegistry.Get("oil") == oilDefinition);
            tx.AfterCommit(() =>
            {
                MessageLog.Add("You feed one oil unit from the flask into the unlit torch (+" + Display(gain) + " fuel).");
                Diag.Record("event", "VesselOilTorchRefueled", actor, torch, new { source = sourceID, oil = 1, gain, fuel = result });
            });
            return true;
        }
    }
}
