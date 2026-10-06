namespace CavesOfOoo.Core
{
    /// <summary>A reusable single-liquid vessel. Volume uses the same units as
    /// LiquidPoolPart; its identity and capacity persist through the normal save
    /// stream. Unlike the clean-water waterskin it has no drinking action.</summary>
    public sealed class LiquidVesselPart : Part
    {
        public override string Name => "LiquidVessel";
        public int Capacity = 12;
        public int Volume;
        public string LiquidId = "";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actor = e.GetParameter<Entity>("Actor");
                WaterTransferActions.AddActions(actor, ParentEntity, actor?.SpatialZone, e.GetParameter<InventoryActionList>("Actions"));
                DouseWorldActions.AddActions(actor, ParentEntity, actor?.SpatialZone, e.GetParameter<InventoryActionList>("Actions"));
                LiquidVesselService.AddActions(actor, ParentEntity, actor?.SpatialZone,
                    e.GetParameter<InventoryActionList>("Actions"));
                return true;
            }
            if (e.ID != "InventoryAction") return true;
            string command = e.GetStringParameter("Command");
            if (WaterTransferActions.IsCommand(command) || DouseWorldActions.IsCommand(command))
            {
                var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone"); var tx = e.GetParameter<CavesOfOoo.Core.Inventory.InventoryTransaction>("InventoryTransaction");
                bool success = WaterTransferActions.IsCommand(command) ? WaterTransferActions.TryAct(actor, ParentEntity, zone, command, tx)
                    : DouseWorldActions.TryAct(actor, ParentEntity, zone, command, tx);
                if (!success) return true; e.Handled = true; return false;
            }
            if (!LiquidVesselService.IsLiquidCommand(command)) return true;
            if (!LiquidVesselService.TryAct(e.GetParameter<Entity>("Actor"), ParentEntity,
                e.GetParameter<Zone>("Zone"), command,
                e.GetParameter<CavesOfOoo.Core.Inventory.InventoryTransaction>("InventoryTransaction"))) return true;
            e.Handled = true;
            return false;
        }
    }
}
