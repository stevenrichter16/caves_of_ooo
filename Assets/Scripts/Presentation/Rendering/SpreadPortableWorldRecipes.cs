using System;
using CavesOfOoo.Core;
using CavesOfOoo.Storylets;
using UnityEngine;
namespace CavesOfOoo.Rendering
{
    /// <summary>Receiving graph authority for movable floor items. Existing
    /// native quest/scenario refusals remain authoritative before refinement.</summary>
    public static class SpreadPortableWorldRecipes
    {
        public static SpawnRing3DRecipe Refine(Zone zone, Entity owner, SpawnRing3DRecipe native)
        {
            if ((!SpreadPresentationScope.IsActive(zone) && !SoddenPresentationScope.IsActive(zone)) || owner == null
                || (owner.BlueprintName == "BeetleJar" && owner.GetPart<PhysicsPart>()?.Takeable != true)
                || !SpreadPortableRecipes.HandlesBlueprint(owner.BlueprintName)) return native;
            if (native.Failure != null && native.Failure != "unmodeled-native-blueprint") return native;
            var cell = zone.GetEntityCell(owner); var physics = owner.GetPart<PhysicsPart>();
            if (!ReferenceEquals(native.Owner, owner) || cell == null || !cell.Objects.Contains(owner)
                || physics == null || !ReferenceEquals(physics.ParentEntity, owner)
                || physics.InInventory != null || physics.Equipped != null
                || owner.HasPart<MultiCellPilotPropPart>() || owner.HasPart<FellingScenePropPart>()
                || owner.HasPart<SpatialFootprintPart>() || !ValidConstructedQuest(owner)
                || !SpreadPortableRecipes.TryRecipe(owner, out string modelId))
                return new SpawnRing3DRecipe(owner, null, null, Vector3.zero, false, false, "unsupported-current-spread-portable");
            return new SpawnRing3DRecipe(owner, modelId, null, Village3DProjection.CellCentre(cell.X, cell.Y), true, false);
        }
        private static bool ValidConstructedQuest(Entity owner)
        {
            bool notebook = owner.BlueprintName == "DetectiveNotebook";
            if (!notebook && owner.BlueprintName != "CrunchyLocket") return true;
            var objective = owner.GetPart<CompleteObjectiveOnTaken>(); var render = owner.GetPart<RenderPart>();
            return objective != null && ReferenceEquals(objective.ParentEntity, owner)
                && objective.Quest == (notebook ? "RootBeerGuyCase" : "CrunchyLocket")
                && objective.Objective == (notebook ? "find_notebook" : "find_locket")
                && render?.RenderString == (notebook ? "=" : "*");
        }
    }
}
