using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    /// <summary>A reusable, water-only vessel. Units are individual drinks.
    /// Public fields persist through the existing part save stream.</summary>
    public sealed class WaterskinPart : Part
    {
        public override string Name => "Waterskin";
        public int Capacity = 3;
        public int Charges;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actor = e.GetParameter<Entity>("Actor");
                if (actor?.GetPart<InventoryPart>()?.Objects.Contains(ParentEntity) != true) return true;
                var actions = e.GetParameter<InventoryActionList>("Actions");
                WaterTransferActions.AddActions(actor, ParentEntity, actor?.SpatialZone, actions);
                DouseWorldActions.AddActions(actor, ParentEntity, actor?.SpatialZone, actions);
                actions?.AddAction("DrinkWater", "drink water (" + Charges + "/" + Capacity + ")", "DrinkWaterskin", 'd', 20);
                actions?.AddAction("FillWater", "fill from nearby fresh water", "FillWaterskin", 'f', 19);
                return true;
            }
            if (e.ID != "InventoryAction") return true;
            string command = e.GetStringParameter("Command");
            if (WaterTransferActions.IsCommand(command) || DouseWorldActions.IsCommand(command))
            {
                var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone"); var tx = e.GetParameter<InventoryTransaction>("InventoryTransaction");
                bool success = WaterTransferActions.IsCommand(command) ? WaterTransferActions.TryAct(actor, ParentEntity, zone, command, tx)
                    : DouseWorldActions.TryAct(actor, ParentEntity, zone, command, tx);
                if (!success) return true; e.Handled = true; return false;
            }
            if (command != "FillWaterskin" && command != "DrinkWaterskin") return true;
            if (!WaterVesselService.TryAct(e.GetParameter<Entity>("Actor"), ParentEntity,
                e.GetParameter<Zone>("Zone"), command, e.GetParameter<InventoryTransaction>("InventoryTransaction"))) return true;
            e.Handled = true;
            return false;
        }
    }
}
