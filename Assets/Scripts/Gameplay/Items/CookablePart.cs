using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>Authored inventory recipe; thermal ground recipes remain independent.</summary>
    public sealed class CookablePart : Part, IDescribeUse, IInventoryActionReceiptOwner
    {
        public override string Name => "Cookable";
        public int Order => 30;
        public string Into = "";

        public bool OwnsInventoryReceipt(string command)
            => ParentEntity?.GetPart<CookablePart>() == this && CookingService.IsCookAt(command);

        public void DescribeUse(DescribeContext context, UseGuide guide)
        {
            guide.How.Add("Stand beside a usable fire and choose Cook in your pack or from the fire's menu.");
            guide.Limits.Add("Cooking prepares the whole stack. Camp hearths and coals need heat and fuel.");
            guide.Limits.Add("A burning object such as a cart can cook food while its flames last.");
            guide.WhyNot = UseGuides.PickUpFirst;
        }

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
                var actions = e.GetParameter<InventoryActionList>("Actions");
                if (actions == null || actor?.GetPart<InventoryPart>()?.CanConsumeOne(ParentEntity) != true) return true;
                const string label = "cook this stack beside a fire";
                var zone = actor.SpatialZone;
                var stations = CookingService.StationsBeside(actor, zone);
                if (stations.Count == 0)
                    actions.AddDisabled("Cook", label, "no_fire", CookingService.NoReadyFire, 'c', 19);
                else
                {
                    InventoryAction authored = null;
                    char key = 'c';
                    foreach (var station in stations)
                    {
                        if (station.HasPart<CampfirePart>())
                        {
                            authored ??= actions.AddAction("Cook", label, "Cook", 'c', 19).InGroup(ToolGroups.Cook);
                            authored.AtEntity(zone, station);
                            key = '\0';
                            continue;
                        }
                        var row = actions.AddAction("Cook", "cook this stack over " + station.GetDisplayName(),
                            CookingService.CookAt(zone, station), key, 18, time: ActionTime.OneAction)
                            .InGroup(ToolGroups.Cook);
                        key = '\0';
                        foreach (var cell in zone.GetOccupiedCells(station))
                            if (CookingService.VisibleContact(actor, cell)) row.AtCell(cell.X, cell.Y);
                    }
                }
                return true;
            }
            if (e.ID != "InventoryAction") return true;
            string command = e.GetStringParameter("Command");
            if (command != "Cook" && !CookingService.IsCookAt(command)) return true;
            // Bootstrap already wires the real content factory for food transformations.
            if (!CookingService.TryCook(e.GetParameter<Entity>("Actor"), ParentEntity, e.GetParameter<Zone>("Zone"),
                MaterialReactionResolver.Factory, e.GetParameter<InventoryTransaction>("InventoryTransaction"), command)) return true;
            e.Handled = true;
            return false;
        }
    }
}
