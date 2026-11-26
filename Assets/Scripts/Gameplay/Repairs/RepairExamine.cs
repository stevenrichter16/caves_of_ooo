using System.Collections.Generic;
using System.Linq;

namespace CavesOfOoo.Core
{
    internal static class RepairExamine
    {
        internal const string Heading = "Needs repair";
        internal const string NoTools = "Tools and machines\nNone required.";

        internal static AnnouncementSection Section(Entity owner)
        {
            if (owner == null || owner.HasTag("Creature") || owner.HasTag("Player")) return null;

            var site = owner.GetPart<PlaceSiteObjectPart>();
            if (site?.ParentEntity == owner && site.ExamineRepairRequirements() is string siteBill)
                return Warning(siteBill);
            if (LegacyPlaceBill(owner) is string legacyBill) return Warning(legacyBill);

            var repair = owner.GetPart<RepairablePart>();
            var broken = owner.GetEffect<BrokenEffect>();
            bool itemBroken = broken?.Owner == owner && broken.Duration != 0;
            bool needsRepair = itemBroken || RepairablePart.BlocksFunction(owner);
            if (!needsRepair) return null;

            var recipe = ApplicableRecipe(owner, repair, itemBroken);
            if (recipe == null)
                return Warning(repair == null ? "No known repair method for this object."
                    : "The damage cannot be repaired with a known method.");

            string text = recipe.Diagnosis + "\n\nMaterials needed\n- " + repair.Bill(recipe) + "\n\n" + NoTools;
            if (repair.PortableEquipment)
                text += "\n\nRepair one item from your pack. Unequip it first if worn or wielded.";
            return Warning(text);
        }

        internal static RepairRecipe ApplicableRecipe(Entity owner, RepairablePart repair, bool itemBroken)
        {
            var composition = owner.GetPart<CompositionPart>();
            if (repair?.ParentEntity != owner || composition?.ParentEntity != owner
                || (repair.PortableEquipment ? !itemBroken : repair.Repaired)) return null;
            if (owner.Parts.Count(part => part is RepairablePart) != 1
                || owner.Parts.Count(part => part is CompositionPart) != 1) return null;
            var recipe = RepairRecipeRegistry.Get(repair.RecipeId);
            return recipe != null && composition.Contains(recipe.Composition) ? recipe : null;
        }

        private static AnnouncementSection Warning(string text) =>
            new AnnouncementSection(Heading, text, AnnouncementTone.Warning, "Needs repair. See full details for the requirements.");

        private static string LegacyPlaceBill(Entity owner)
        {
            if (owner.SpatialZone is not Zone zone || WorldLocationContext.For(zone) is not OverworldZoneManager world
                || !world.CachedZones.TryGetValue(zone.ZoneID, out var current) || current != zone) return null;

            string settlement, site, material, guide, rite;
            int turns;
            switch (owner.GetPart<WellSitePart>(), owner.GetPart<OvenSitePart>(), owner.GetPart<LanternSitePart>())
            {
                case (WellSitePart well, null, null) when well.ParentEntity == owner:
                    settlement = well.SettlementId; site = well.SiteId;
                    material = SettlementRepairDefinitions.SilverSandBlueprint;
                    guide = "well maintenance manual"; rite = "water-purification rite";
                    turns = SettlementRepairDefinitions.PurifyRelapseTurns;
                    break;
                case (null, OvenSitePart oven, null) when oven.ParentEntity == owner:
                    settlement = oven.SettlementId; site = oven.SiteId;
                    material = SettlementRepairDefinitions.FireClayBlueprint;
                    guide = "oven builder's guide"; rite = "mending rite";
                    turns = SettlementRepairDefinitions.MendingRelapseTurns;
                    break;
                case (null, null, LanternSitePart lantern) when lantern.ParentEntity == owner:
                    settlement = lantern.SettlementId; site = lantern.SiteId;
                    material = SettlementRepairDefinitions.WardOilBlueprint;
                    guide = "lantern oil recipe"; rite = "kindle rite";
                    turns = SettlementRepairDefinitions.KindleRelapseTurns;
                    break;
                default:
                    return null;
            }

            if (settlement != zone.ZoneID || !world.SettlementManager.TryGetExistingSite(settlement, site, out var state)
                || state.Stage >= RepairStage.StableRepair) return null;
            var lines = new List<string>();
            if (state.Stage == RepairStage.TemporarilyPurified) lines.Add("Working temporarily. A lasting repair is still available.\n");
            lines.Add("Lasting repair\n- 1 " + PlaceSiteRepairs.NameOf(material) + " (used)\n- " + guide + " (kept)");
            lines.Add(NoTools);
            if (state.Stage == RepairStage.Fouled)
                lines.Add("Temporary remedy\n- Know the " + rite + " (lasts about " + turns + " turns).");
            return string.Join("\n\n", lines);
        }
    }
}
