using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Data definition for one tinkering recipe.
    /// Mirrors Qud's data-driven recipes: ID + output + bit cost + optional ingredient.
    /// </summary>
    [Serializable]
    public class TinkerRecipe
    {
        public string ID;
        public string DisplayName;
        public string Blueprint;
        public string Type;
        public string Cost;
        public string Ingredient;
        public int NumberMade = 1;

        // Optional mod-target constraints used when Type == "Mod".
        // These are intentionally simple for V1.
        public string TargetPart;
        public string TargetTag;
        public string TargetBlueprint;

        /// <summary>
        /// Optional player-facing effect summary shown in the Tinker
        /// description panel when the recipe is highlighted. Tier-agnostic —
        /// the recipe describes the EFFECT KIND ("Bonus damage vs Undead"),
        /// not the specific number (which depends on the mineral's tier).
        /// Per-instance tier-aware numbers surface when the player examines
        /// the enhanced item itself (via IItemEnhancement.GetEffectDescription).
        /// Empty for recipes that don't yet have a description.
        /// </summary>
        public string Description;
    }
}
