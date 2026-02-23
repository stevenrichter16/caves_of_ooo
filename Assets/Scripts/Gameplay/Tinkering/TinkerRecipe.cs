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
    }
}
