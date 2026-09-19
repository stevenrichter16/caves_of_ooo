using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>Read-only, carried-item guidance for the three native repair
    /// materials. Built on explicit inspection, never world generation or a
    /// repair dry run. False means the item is outside this bounded help surface
    /// or no longer has a usable unit in this actor's carried inventory.</summary>
    public static class MaterialUseDescription
    {
        public static bool TryDescribe(Entity actor, Entity item, EntityFactory factory, out string text)
        {
            text = null;
            var inventory = actor?.GetPart<InventoryPart>();
            if (item == null || inventory == null || !inventory.CanConsumeOne(item)) return false;
            string guide, fallback, use, resident;
            switch (item.BlueprintName)
            {
                case SettlementRepairDefinitions.FireClayBlueprint:
                    guide = SettlementRepairDefinitions.OvenBuildersGuideBlueprint;
                    fallback = "oven builder's guide"; use = "rebuild a damaged settlement oven"; resident = "farmer";
                    break;
                case SettlementRepairDefinitions.SilverSandBlueprint:
                    guide = SettlementRepairDefinitions.WellMaintenanceManualBlueprint;
                    fallback = "well maintenance manual"; use = "repair a damaged settlement well's filtration ring"; resident = "keeper";
                    break;
                case SettlementRepairDefinitions.WardOilBlueprint:
                    guide = SettlementRepairDefinitions.LanternOilRecipeBlueprint;
                    fallback = "lantern oil recipe"; use = "reforge a settlement watch lantern"; resident = "warden";
                    break;
                default: return false;
            }
            string guideName = fallback;
            if (factory != null && factory.Blueprints.TryGetValue(guide, out var blueprint)
                && blueprint.Parts.TryGetValue("Render", out var render)
                && render.TryGetValue("DisplayName", out var name) && !string.IsNullOrWhiteSpace(name)) guideName = name;
            // Match SettlementManager's exact blueprint and CanConsumeOne gate.
            bool carried = false;
            foreach (var candidate in inventory.Objects)
                if (candidate != null && candidate.BlueprintName == guide && inventory.CanConsumeOne(candidate))
                { carried = true; break; }
            text = item.GetDisplayName()+"\n\nOne measure can "+use+". Speak to its "+resident
                +" where this repair is offered. The material is spent; the guide is kept.\n\nRequired guide: "+guideName
                +(carried?" (carried).":" (missing). Ask the settlement elder about the damaged site for its guide.");
            if (item.BlueprintName == SettlementRepairDefinitions.FireClayBlueprint)
                text += "\n\nAnother use: Morrowfast's bell. Ask Nemm about it and inspect the reserve cord first."
                    +" After diagnosis, one fire clay makes a quiet clapper; the bare, loud setting costs no material.";
            return true;
        }
    }
}
