using System;
using System.Collections.Generic;
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
        const string StoredPrefix = "RefuelStoredVesselTorch|";
        const float FuelPerOil = 25;
        internal static bool IsCommand(string command) => command?.StartsWith(Prefix, StringComparison.Ordinal) == true;
        internal static bool IsStoredCommand(string command) => command?.StartsWith(StoredPrefix, StringComparison.Ordinal) == true;
        static T Unique<T>(Entity owner) where T : Part
        {
            var parts = owner?.Parts.OfType<T>().ToArray();
            return parts?.Length == 1 && parts[0].ParentEntity == owner ? parts[0] : null;
        }
        internal static bool OwnsReceipt(Entity owner, string command) => IsCommand(command) && Unique<LiquidVesselPart>(owner) != null;
        static bool Packed(Entity actor, Entity owner) => PreparationActions.Carried(actor, owner)
            && WorldResourceActions.Carried(actor, owner) && Unique<PhysicsPart>(owner) != null
            && owner.Parts.Count(p => p is StackerPart) <= 1;
        static bool Torch(Entity actor, Entity owner, bool allowFull = false)
        {
            var fuel = Unique<FuelPart>(owner); var heat = Unique<ThermalPart>(owner);
            return Packed(actor, owner) && Unique<TorchLightPart>(owner) != null && !owner.HasEffect<BurningEffect>()
                && Unique<LightSourcePart>(owner) is LightSourcePart light && !light.Enabled
                && fuel != null && heat != null && WorldResourceActions.Finite(fuel.FuelMass)
                && WorldResourceActions.Finite(fuel.MaxFuel) && fuel.FuelMass >= 0 && fuel.MaxFuel > 0
                && (allowFull ? fuel.FuelMass <= fuel.MaxFuel : fuel.FuelMass < fuel.MaxFuel)
                && WorldResourceActions.Finite(fuel.BurnRate) && fuel.BurnRate > 0
                && WorldResourceActions.Finite(heat.Temperature) && WorldResourceActions.Finite(heat.FlameTemperature)
                && !(owner.GetStat("Hitpoints") is Stat hp && hp.Value <= 0);
        }
        static bool TorchAtBox(Entity actor, Entity owner, Zone zone, bool allowFull = false)
            => Torch(actor, owner, allowFull) && !LocalPorterReservationPart.Blocks(owner)
                && !zone.GetReadOnlyEntities().Any(entity => entity.ID == owner.ID)
                && !MaterialProcessStock.ListedInGroundedContainer(zone, owner);
        static string Number(float n) => n.ToString("R", CultureInfo.InvariantCulture);
        static string Display(float n) => n.ToString("G5", CultureInfo.InvariantCulture);
        static string ItemName(Entity item)
        {
            string name = InventoryPart.GetUnitDisplayName(item);
            string label = StorageLabels.Read(item).Text;
            return string.IsNullOrEmpty(label) ? name : name + " [" + label + "]";
        }
        static float RoundFuelDown(double amount)
        {
            float result = (float)amount;
            // Amounts are nonnegative. Use the preceding float if rounding would exceed the quoted fuel.
            return result <= amount ? result : BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(result) - 1);
        }
        static float Refueled(FuelPart fuel) => RoundFuelDown(Math.Min(fuel.MaxFuel, (double)fuel.FuelMass + FuelPerOil));
        static string Quote(Entity source, Entity torch)
        {
            var liquid = source.GetPart<LiquidVesselPart>(); var fuel = torch.GetPart<FuelPart>();
            return Prefix + Uri.EscapeDataString(source.ID) + "|" + Uri.EscapeDataString(torch.ID) + "|"
                + liquid.Volume.ToString(CultureInfo.InvariantCulture) + "|" + liquid.Capacity.ToString(CultureInfo.InvariantCulture)
                + "|" + Number(fuel.FuelMass) + "|" + Number(fuel.MaxFuel);
        }
        internal static void AddActions(Entity actor, Entity source, Zone zone, InventoryActionList actions)
        {
            if (actions == null || !PreparationActions.CurrentActor(actor, zone) || !LiquidVesselPart.HasCarriedOil(actor, source)) return;
            foreach (var torch in actor.GetPart<InventoryPart>().Objects)
            {
                if (torch == source || !Torch(actor, torch)) continue;
                var fuel = torch.GetPart<FuelPart>(); float result = Refueled(fuel);
                if (result <= fuel.FuelMass) continue;
                actions.AddActionWithHint("RefuelVesselTorch", "1 oil: " + Display(fuel.FuelMass) + " to " + Display(result)
                    + "/" + Display(fuel.MaxFuel) + " fuel, " + InventoryPart.GetUnitDisplayName(torch), Quote(source, torch),
                    "Spend one liquid oil unit. Adds up to 25 fuel; any amount above the torch's capacity is lost. The torch stays out. Lamp-oil measures also work.",
                    '\0', 30, false, ActionTime.OneAction);
            }
        }

        internal static bool HandleStored(Entity box, GameEvent e)
        {
            if (box?.BlueprintName != "CampBox") return true;
            if (e.ID == "GetInventoryActions")
            {
                var actor = e.GetParameter<Entity>("Actor");
                var zone = PreparationActions.ZoneFor(e, actor);
                RouteWorkMenu.Rows(box, actor, zone, e.GetParameter<InventoryActionList>("Actions"), StoredChoices(actor, box, zone), true);
            }
            if (e.ID != "InventoryAction" || !IsStoredCommand(e.GetStringParameter("Command")) || !RouteWorkMenu.Act(box, e)) return true;
            e.Handled = true;
            return false;
        }

        static IEnumerable<RouteWorkMenu.Entry> StoredChoices(Entity actor, Entity box, Zone zone)
        {
            var stock = MaterialProcessStock.Resolve(actor, box, zone);
            if (stock == null || !RouteWorkMenu.ActorReady(actor, zone)) yield break;
            var cell = zone.GetEntityCell(box);
            var pack = actor.GetPart<InventoryPart>();
            var definition = LiquidRegistry.IsInitialized ? LiquidRegistry.Get("oil") : null;
            foreach (var source in stock.Container.Contents)
            {
                if (!stock.ContainsOilFlask(source, false)) continue;
                var liquid = source.GetPart<LiquidVesselPart>();
                int amount = liquid.Volume, capacity = liquid.Capacity;
                var sameSource = RouteWorkMenu.Identity(source);
                foreach (var torch in pack.Objects)
                {
                    if (!TorchAtBox(actor, torch, zone)) continue;
                    var sameTorch = RouteWorkMenu.Identity(torch);
                    var fuel = torch.GetPart<FuelPart>();
                    var heat = torch.GetPart<ThermalPart>();
                    float before = fuel.FuelMass, maximum = fuel.MaxFuel, rate = fuel.BurnRate;
                    float temperature = heat.Temperature, flameTemperature = heat.FlameTemperature;
                    float after = Refueled(fuel);
                    if (after <= before) continue;
                    string key = StoredPrefix + Uri.EscapeDataString(source.ID) + "|" + Uri.EscapeDataString(torch.ID);
                    bool Current(int expectedOil, float expectedFuel)
                        => RouteWorkMenu.ActorReady(actor, zone) && actor.GetPart<InventoryPart>() == pack
                            && sameSource() && stock.ContainsOilFlask(source, expectedOil == 0)
                            && sameTorch() && TorchAtBox(actor, torch, zone, true) && fuel.FuelMass == expectedFuel && fuel.MaxFuel == maximum
                            && fuel.BurnRate == rate && heat.Temperature == temperature && heat.FlameTemperature == flameTemperature
                            && liquid.Volume == expectedOil && liquid.Capacity == capacity && LiquidRegistry.Get("oil") == definition;
                    yield return new RouteWorkMenu.Entry
                    {
                        Actor = actor, Zone = zone, Cell = cell, Key = key, Claims = new[] { source, torch },
                        Label = "refuel " + ItemName(torch) + " from " + ItemName(source)
                            + " (1 oil, " + Display(before) + " to " + Display(after) + "/" + Display(maximum) + " fuel)",
                        Hint = "Uses one of " + amount + " oil units. The flask stays in the box and the torch stays out. Any fuel above capacity is lost.",
                        Before = () => Current(amount, before),
                        After = () => Current(amount - 1, after),
                        Apply = tx =>
                        {
                            if (!stock.Begin(source, tx, key)) return false;
                            Spend(actor, source, torch, after, tx);
                            return true;
                        },
                        Publish = () => Publish(actor, source, torch, box, after - before, after)
                    };
                }
            }
        }

        static void Spend(Entity actor, Entity source, Entity torch, float result, InventoryTransaction tx)
        {
            var liquid = source.GetPart<LiquidVesselPart>();
            var fuel = torch.GetPart<FuelPart>();
            string sourceID = source.ID, torchID = torch.ID;
            int capacity = liquid.Capacity;
            float before = fuel.FuelMass;
            double gain = (double)result - before;
            tx.Do(() =>
            {
                liquid.Volume--;
                liquid.LiquidId = liquid.Volume == 0 ? "" : "oil";
                fuel.FuelMass = result;
            }, () =>
            {
                // Only return the oil once the exact credited fuel has been retracted.
                if (torch.ID != torchID || Unique<FuelPart>(torch) != fuel
                    || !WorldResourceActions.Finite(fuel.FuelMass) || fuel.FuelMass < gain)
                {
                    Diag.Record("event", "OilRefuelRollbackLimited", actor, torch, new { reason = "fuel_credit_could_not_be_retracted" });
                    return;
                }
                float current = fuel.FuelMass;
                float restored = current == result ? before : RoundFuelDown((double)current - gain);
                if ((double)current - restored < gain)
                {
                    Diag.Record("event", "OilRefuelRollbackLimited", actor, torch, new { reason = "fuel_credit_could_not_be_represented" });
                    return;
                }
                fuel.FuelMass = restored;
                if (source.ID == sourceID && Unique<LiquidVesselPart>(source) == liquid
                    && (liquid.LiquidId == "oil" || liquid.Volume == 0 && liquid.LiquidId == ""))
                {
                    liquid.Volume = WaterQuantityUndo.Restore(liquid.Volume, -1, Math.Min(capacity, Math.Max(0, liquid.Capacity)));
                    liquid.LiquidId = liquid.Volume == 0 ? "" : "oil";
                }
                else Diag.Record("event", "OilRefuelRollbackLimited", actor, source, new { reason = "source-part-or-liquid-changed" });
            });
        }

        static void Publish(Entity actor, Entity source, Entity torch, Entity box, float gain, float result)
        {
            string from = box == null ? "the flask" : "the flask in the camp box";
            MessageLog.Add("You feed one oil unit from " + from + " into the unlit torch (+" + Display(gain) + " fuel).");
            Diag.Record("event", "VesselOilTorchRefueled", actor, torch, new { source = source.ID, box = box?.ID, oil = 1, gain, fuel = result });
        }

        internal static bool TryAct(Entity actor, Entity source, Zone zone, string command, InventoryTransaction tx)
        {
            if (!IsCommand(command) || tx == null || !PreparationActions.CurrentActor(actor, zone)
                || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true || !LiquidVesselPart.HasCarriedOil(actor, source)) return false;
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
            float result = Refueled(fuel), gain = result - beforeFuel;
            if (!(gain > 0)) return false;
            var oilDefinition = LiquidRegistry.Get("oil");
            Spend(actor, source, torch, result, tx);
            tx.BeforeCommit(() => PreparationActions.CurrentActor(actor, zone) && zone.GetEntityCell(actor) == cell
                && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true
                && source.ID == sourceID && torch.ID == torchID && LiquidVesselPart.HasCarriedOil(actor, source, true) && Torch(actor, torch, true)
                && Unique<LiquidVesselPart>(source) == liquid && Unique<FuelPart>(torch) == fuel
                && Unique<LightSourcePart>(torch) == light && Unique<ThermalPart>(torch) == heat && Unique<TorchLightPart>(torch) == flame
                && source.GetPart<PhysicsPart>() == sourcePhysics && torch.GetPart<PhysicsPart>() == torchPhysics
                && liquid.Volume == beforeOil - 1 && liquid.Capacity == capacity && fuel.FuelMass == result && fuel.MaxFuel == maximum
                && LiquidRegistry.Get("oil") == oilDefinition);
            tx.AfterCommit(() => Publish(actor, source, torch, null, gain, result));
            return true;
        }
    }
}
