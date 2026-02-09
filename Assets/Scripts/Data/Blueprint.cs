using System.Collections.Generic;

namespace CavesOfOoo.Data
{
    /// <summary>
    /// Template for creating entities. Loaded from JSON data files.
    /// Mirrors Qud's GameObjectBlueprint: defines what parts, stats, and tags
    /// an entity should have. Supports single inheritance via Inherits field.
    /// </summary>
    public class Blueprint
    {
        public string Name;
        public string Inherits;

        /// <summary>
        /// Part definitions: part type name -> parameter dictionary.
        /// e.g. "Render" -> { "DisplayName": "Dagger", "RenderString": "/" }
        /// </summary>
        public Dictionary<string, Dictionary<string, string>> Parts = new Dictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// Stat definitions: stat name -> stat template.
        /// </summary>
        public Dictionary<string, StatBlueprint> Stats = new Dictionary<string, StatBlueprint>();

        /// <summary>
        /// Tags: name -> value.
        /// </summary>
        public Dictionary<string, string> Tags = new Dictionary<string, string>();

        /// <summary>
        /// String properties.
        /// </summary>
        public Dictionary<string, string> Props = new Dictionary<string, string>();

        /// <summary>
        /// Integer properties.
        /// </summary>
        public Dictionary<string, int> IntProps = new Dictionary<string, int>();

        /// <summary>
        /// Cached reference to the resolved parent blueprint (set during baking).
        /// </summary>
        public Blueprint Parent;

        /// <summary>
        /// Whether inheritance has been resolved.
        /// </summary>
        public bool Baked;
    }

    /// <summary>
    /// Blueprint data for a single stat.
    /// </summary>
    public class StatBlueprint
    {
        public string Name;
        public int Value;
        public int Min = 0;
        public int Max = 999;
        public int Boost;
        public string sValue = "";
    }
}
