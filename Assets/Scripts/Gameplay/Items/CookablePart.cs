using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    /// <summary>Authored inventory recipe; thermal ground recipes remain independent.</summary>
    public sealed class CookablePart : Part
    {
        public override string Name => "Cookable";
        public string Into = "";
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actor = e.GetParameter<Entity>("Actor");
                if (actor?.GetPart<InventoryPart>()?.CanConsumeOne(ParentEntity) == true)
                {
                    var row = e.GetParameter<InventoryActionList>("Actions")?.AddAction("Cook", "cook this stack beside a fire", "Cook", 'c', 19);
                    // Cooking is done at a fire: the interact menu of each campfire or hearth beside you offers it too.
                    if (row != null && actor.SpatialZone != null) foreach (var station in CookingService.StationsBeside(actor, actor.SpatialZone)) row.AtEntity(actor.SpatialZone, station);
                }
                return true;
            }
            if (e.ID != "InventoryAction" || e.GetStringParameter("Command") != "Cook") return true;
            // Bootstrap already wires the real content factory for food transformations.
            if (!CookingService.TryCook(e.GetParameter<Entity>("Actor"), ParentEntity, e.GetParameter<Zone>("Zone"),
                MaterialReactionResolver.Factory, e.GetParameter<InventoryTransaction>("InventoryTransaction"))) return true;
            e.Handled = true;
            return false;
        }
    }
}
