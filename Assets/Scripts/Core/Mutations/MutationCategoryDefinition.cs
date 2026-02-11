using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Metadata for a mutation category (Physical, Mental, etc).
    /// </summary>
    [Serializable]
    public class MutationCategoryDefinition
    {
        public string Name = "";
        public string DisplayName = "";
        public string Stat = "";
        public string CategoryModifierProperty = "";

        public string GetCategoryModifierPropertyName()
        {
            if (!string.IsNullOrEmpty(CategoryModifierProperty))
                return CategoryModifierProperty;

            if (string.IsNullOrEmpty(Name))
                return "UnknownMutationLevelModifier";

            return Name + "MutationLevelModifier";
        }
    }
}
