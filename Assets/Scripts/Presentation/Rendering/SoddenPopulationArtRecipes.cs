using CavesOfOoo.Core;
using UnityEngine;
namespace CavesOfOoo.Rendering
{
    /// <summary>Regional missing-only art for the real non-solid dew snare.
    /// Native failures, membership, damage and trap ownership remain authoritative.</summary>
    internal static class SoddenPopulationArtRecipes
    {
        internal static SpawnRing3DRecipe Refine(Zone zone, Entity owner, SpawnRing3DRecipe native)
        {
            if (!SoddenPresentationScope.IsActive(zone) || owner?.BlueprintName != "Greatdew"
                || !ReferenceEquals(native.Owner, owner) || native.Failure != "unmodeled-native-blueprint") return native;
            var cell = zone.GetEntityCell(owner); var render = owner.GetPart<RenderPart>();
            var physics = owner.GetPart<PhysicsPart>(); var snare = owner.GetPart<GreatdewSnarePart>();
            var damage = owner.GetPart<DestructiblePart>();
            if (cell == null || !cell.Objects.Contains(owner) || owner.SpatialZone != zone || cell.ParentZone != zone
                || render?.ParentEntity != owner || !render.Visible || render.RenderString != "(" || render.ColorString != "&C"
                || render.RenderLayer != 2 || !string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant)
                || !string.IsNullOrEmpty(render.GlyphVariants) || physics?.ParentEntity != owner || physics.Solid || physics.Takeable
                || physics.InInventory != null || physics.Equipped != null || snare?.ParentEntity != owner
                || damage?.ParentEntity != owner || damage.Gone || damage.HP <= 0 || !owner.HasTag("Vegetation")
                || owner.HasTag("Creature") || owner.HasTag("Item") || owner.HasTag("Solid")
                || owner.HasPart<SpatialFootprintPart>() || owner.HasPart<MultiCellPilotPropPart>()
                || owner.HasPart<FellingScenePropPart>()) return native;
            int variant = SpawnRing3DRecipes.Variant(zone.ZoneID,owner.BlueprintName,owner.ID,cell.X,cell.Y,2);
            return new SpawnRing3DRecipe(owner, "sodden-native-greatdew-" + variant, null,
                Village3DProjection.CellCentre(cell.X, cell.Y), false, true);
        }
    }
}
