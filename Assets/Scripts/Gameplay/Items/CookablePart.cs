using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>Authored inventory recipe; thermal ground recipes remain independent.</summary>
    public sealed class CookablePart : Part
    {
        public override string Name => "Cookable";
        public string Into = "";

        internal static void RestoreLakePerchRecipe(Entity item, EntityFactory factory)
        {
            // Older catches have the original food Parts and no saved recipe.
            // Restore this authored addition once without replacing any saved
            // Parts or granting recipes to other historical food items.
            const string perch = "LakePerch", roasted = "RoastedLakePerch";
            if (item.BlueprintName != perch || item.HasPart<CookablePart>()
                || item.GetPart<FoodPart>()?.ParentEntity != item || !item.HasTag("Item")
                || item.HasTag("Creature") || item.HasTag("Terrain") || factory == null
                || !factory.Blueprints.TryGetValue(perch, out var blueprint)
                || !blueprint.Parts.TryGetValue("Cookable", out var values)
                || !values.TryGetValue(nameof(Into), out string output) || output != roasted
                || !factory.Blueprints.ContainsKey(output)) return;
            item.AddPart(new CookablePart { Into = output });
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actor = e.GetParameter<Entity>("Actor");
                if (actor?.GetPart<InventoryPart>()?.CanConsumeOne(ParentEntity) == true)
                {
                    var row = e.GetParameter<InventoryActionList>("Actions")?.AddAction("Cook", "cook this stack beside a fire", "Cook", 'c', 19);
                    // Cooking is done at a fire: the interact menu of each campfire or hearth beside you offers it too.
                    if (row != null && actor.SpatialZone != null) foreach (var station in CookingService.StationsBeside(actor, actor.SpatialZone)) row.AtEntity(actor.SpatialZone, station).InGroup(ToolGroups.Cook);
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
