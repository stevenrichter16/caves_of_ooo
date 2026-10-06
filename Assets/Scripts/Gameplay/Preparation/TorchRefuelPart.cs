using System;
using CavesOfOoo.Core.Inventory;
namespace CavesOfOoo.Core
{
    public sealed class TorchRefuelPart : Part
    {
        public override string Name => "TorchRefuel";
        public override bool HandleEvent(GameEvent e)
        {
            var actor = e.GetParameter<Entity>("Actor"); var oil = ParentEntity; var zone = PreparationActions.ZoneFor(e, actor);
            if (e.ID == "GetInventoryActions")
            {
                if (oil.BlueprintName == "LampOil" && PreparationActions.Carried(actor, oil))
                    foreach (var item in actor.GetPart<InventoryPart>().Objects)
                        if (CanRefuel(actor, item)) e.GetParameter<InventoryActionList>("Actions")?.AddAction("RefuelTorch" + item.ID,
                            "1 oil: +25 fuel (cap), " + InventoryPart.GetUnitDisplayName(item), "RefuelTorch|" + PreparationActions.Escaped(item), '\0', 17);
                return true;
            }
            string command = e.GetStringParameter("Command");
            if (e.ID != "InventoryAction" || command == null || !command.StartsWith("RefuelTorch|", StringComparison.Ordinal)) return true;
            if (e.GetParameter<bool>("PreparationAttempted")) return true; e.SetParameter("PreparationAttempted", true);
            var torch = WorldResourceActions.ExactCarried(actor, command.Substring(12));
            var tx = e.GetParameter<InventoryTransaction>("InventoryTransaction");
            if (oil.BlueprintName != "LampOil" || oil.GetPart<TorchRefuelPart>() != this || !CanRefuel(actor, torch)
                || !PreparationActions.Begin(actor, oil, zone, tx, command) || !tx.TryClaim(torch, actor, command))
            { PreparationActions.Reject(actor, oil, command, "unlit_torch_required"); return true; }
            var pack = actor.GetPart<InventoryPart>(); var fuel = torch.GetPart<FuelPart>(); float before = fuel.FuelMass;
            var receipt = InventoryTransferSnapshot.Capture(pack); bool restored = false;
            Action restore = () => { if (!restored) { restored = true; fuel.FuelMass = before; receipt.Restore(); } }; tx.Do(null, restore);
            if (!receipt.Apply(() => { if (!pack.TryConsumeOne(oil)) return false; fuel.FuelMass = Math.Min(fuel.MaxFuel, before + 25); return true; })
                || !receipt.ClaimChanges(tx, actor, command)) { restore(); PreparationActions.Reject(actor, oil, command, "transfer"); return true; }
            PreparationActions.Complete(tx, actor, torch, command, "You feed one lamp-oil measure into the unlit torch.");
            e.Handled = true; return false;
        }
        static bool CanRefuel(Entity actor, Entity item)
        {
            if (!PreparationActions.Carried(actor, item) || (item.GetPart<StackerPart>()?.StackCount ?? 1) != 1
                || item.GetPart<TorchLightPart>()?.ParentEntity != item || item.GetPart<LightSourcePart>() is not LightSourcePart light || light.Enabled) return false;
            var fuel = item.GetPart<FuelPart>();
            return fuel?.ParentEntity == item && WorldResourceActions.Finite(fuel.FuelMass) && WorldResourceActions.Finite(fuel.MaxFuel)
                && fuel.FuelMass >= 0 && fuel.MaxFuel > fuel.FuelMass && !(item.GetStat("Hitpoints") is Stat hp && hp.Value <= 0);
        }
    }
}
