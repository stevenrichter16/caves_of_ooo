using System;
using CavesOfOoo.Core;
using UnityEngine;
namespace CavesOfOoo.Rendering
{
    /// <summary>Missing-only scenery refinement for the actual receiving owner.
    /// Existing native successes and named refusals retain their authority.</summary>
    internal static class SpreadSceneryWorldRecipes
    {
        internal static SpawnRing3DRecipe Refine(Zone zone, Entity owner, SpawnRing3DRecipe native)
        {
            if (native.Failure != "unmodeled-native-blueprint") return native;
            if ((!SpreadPresentationScope.IsActive(zone) && !SoddenPresentationScope.IsActive(zone)) || owner == null
                || !ReferenceEquals(native.Owner, owner)) return native;
            var cell = zone.GetEntityCell(owner);
            if (cell == null || !cell.Objects.Contains(owner)
                || owner.HasPart<FellingScenePropPart>()) return native;
            int variant = Variant(zone.ZoneID, owner.BlueprintName, owner.ID, cell.X, cell.Y);
            if (!SpreadSceneryRecipes.TryModel(owner, variant, out string model)) return native;
            return new SpawnRing3DRecipe(owner, model, null,
                Village3DProjection.CellCentre(cell.X, cell.Y), false, true);
        }
        private static int Variant(string zone, string blueprint, string id, int x, int y)
        {
            // Explicit stable address/owner hash. Never consumes simulation RNG.
            unchecked
            {
                uint value = 2166136261;
                Hash(ref value, zone); Hash(ref value, blueprint); Hash(ref value, id);
                value = (value ^ (uint)x) * 16777619;
                value = (value ^ (uint)y) * 16777619;
                return (int)(value % 2);
            }
        }
        private static void Hash(ref uint value, string text)
        {
            unchecked
            {
                if (text != null) foreach (char c in text) value = (value ^ c) * 16777619;
                value = (value ^ 0xff) * 16777619;
            }
        }
    }
}
