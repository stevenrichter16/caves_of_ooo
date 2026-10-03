using System;
using CavesOfOoo.Core;

namespace CavesOfOoo.Rendering
{
    /// <summary>Read-only art admission for the actual district owners. A saved
    /// repair flag selects the frame; plans never reconstruct missing objects.</summary>
    public static class SoddenDistrictRecipes
    {
        public static bool Handles(string blueprint)
        {
            switch (blueprint)
            {
                case "SoddenDressingBench": case "SoddenWorksSalvage": case "SoddenWorksLocker":
                case "SoddenRouteNotice": case "SoddenFieldDressing": return true;
                default: return false;
            }
        }

        static bool ExactPart(Entity owner, string name)
        {
            int count = 0;
            foreach (var part in owner.Parts)
                if (part.Name == name) { if (part.ParentEntity != owner) return false; count++; }
            return count == 1;
        }

        static bool Current(Zone zone, Entity owner)
        {
            if (zone == null || owner == null || !Handles(owner.BlueprintName)) return false;
            var cell = zone.GetEntityCell(owner); var render = owner.GetPart<RenderPart>(); var physics = owner.GetPart<PhysicsPart>();
            if (cell == null || cell.ParentZone != zone || owner.SpatialZone != zone || !cell.Objects.Contains(owner)
                || !AreaCompositionScope.Allows(zone) || !ExactPart(owner, "Render") || !ExactPart(owner, "Physics")
                || render == null || !render.Visible || physics == null || physics.InInventory != null || physics.Equipped != null
                || owner.HasTag("Creature") || owner.HasPart<SpatialFootprintPart>() || owner.HasPart<MultiCellPilotPropPart>()
                || !string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant) || !string.IsNullOrEmpty(render.GlyphVariants)) return false;
            var manager = WorldLocationContext.For(zone);
            if (manager != null && (!manager.CachedZones.TryGetValue(zone.ZoneID, out var live) || live != zone)) return false;
            if (owner.BlueprintName != "SoddenFieldDressing" && !SoddenDistrictPlan.IsSupportedZone(zone.ZoneID)) return false;
            var damage = owner.GetPart<DestructiblePart>();
            return damage == null || ExactPart(owner, "Destructible") && damage.ParentEntity == owner && !damage.Gone && damage.HP > 0;
        }

        static bool Appearance(Entity owner, string glyph, string color, int layer)
        {
            var render = owner.GetPart<RenderPart>();
            return render.RenderString == glyph && render.ColorString == color && render.RenderLayer == layer;
        }

        /// <summary>Returns an exact imported model only for a live native owner
        /// whose current role and physical state match the depicted object.</summary>
        public static string ResolveModel(Zone zone, Entity owner)
        {
            if (!Current(zone, owner)) return null;
            var physics = owner.GetPart<PhysicsPart>();
            if (owner.BlueprintName == "SoddenFieldDressing")
            {
                var stack = owner.GetPart<StackerPart>();
                if (!physics.Takeable || physics.Solid || !owner.HasTag("Item") || owner.HasTag("Natural") || owner.HasPart("Tonic")
                    || !ExactPart(owner, "SoddenDressing") || !Appearance(owner, "!", "&g", 5)
                    || stack != null && (!ExactPart(owner, "Stacker") || stack.StackCount <= 0)) return null;
                return SoddenDistrictArtLibrary.Dressing;
            }
            if (physics.Takeable || owner.HasTag("Item")) return null;
            switch (owner.BlueprintName)
            {
                case "SoddenDressingBench":
                    var repair = owner.GetPart<RepairablePart>(); var composition = owner.GetPart<CompositionPart>();
                    var recipe = RepairRecipeRegistry.Get("timber-dressing-bench");
                    if (!physics.Solid || !Appearance(owner, "=", "&y", 4) || !ExactPart(owner, "Repairable")
                        || repair == null || repair.RecipeId != "timber-dressing-bench" || !ExactPart(owner, "Composition")
                        || composition == null || recipe == null || !composition.Contains(recipe.Composition)
                        || !ExactPart(owner, "SoddenPreparation")) return null;
                    return repair.Repaired ? SoddenDistrictArtLibrary.WorkingBench : SoddenDistrictArtLibrary.BrokenBench;
                case "SoddenWorksSalvage":
                    var harvest = owner.GetPart<HarvestablePart>();
                    if (physics.Solid || !Appearance(owner, "=", "&y", 2) || !ExactPart(owner, "Harvestable") || harvest == null
                        || harvest.Harvested || harvest.YieldBlueprint != "SalvagedTimber" || harvest.YieldMin != 4
                        || harvest.YieldMax != 4 || harvest.YieldChance != 100) return null;
                    return SoddenDistrictArtLibrary.Salvage;
                case "SoddenWorksLocker":
                    var container = owner.GetPart<ContainerPart>();
                    if (!physics.Solid || !Appearance(owner, "=", "&w", 1) || !ExactPart(owner, "Container")
                        || container == null || container.Contents == null) return null;
                    return SoddenDistrictArtLibrary.Locker;
                case "SoddenRouteNotice":
                    if (!physics.Solid || !Appearance(owner, "I", "&y", 1) || !ExactPart(owner, "Examinable")) return null;
                    return SoddenDistrictArtLibrary.Notice;
                default: return null;
            }
        }

        internal static SpawnRing3DRecipe Recipe(Zone zone, Entity owner, string model)
        {
            var cell = zone.GetEntityCell(owner); bool portable = owner.GetPart<PhysicsPart>().Takeable;
            return new SpawnRing3DRecipe(owner, model, null, Village3DProjection.CellCentre(cell.X, cell.Y), portable, !portable);
        }
    }
}
