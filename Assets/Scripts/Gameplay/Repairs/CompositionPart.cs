using System;

namespace CavesOfOoo.Core
{
    /// <summary>Saved structural composition, separate from reactive MaterialPart
    /// properties. Exact comma-separated categories select explicit repair recipes;
    /// composition does not imply a fault, damage, or an automatic repair.</summary>
    public sealed class CompositionPart : Part
    {
        public override string Name => "Composition";
        public string MaterialsRaw = "";
        public bool Contains(string category)
        {
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrEmpty(MaterialsRaw)) return false;
            foreach (var value in MaterialsRaw.Split(','))
                if (string.Equals(value.Trim(), category, StringComparison.Ordinal)) return true;
            return false;
        }
    }
}
