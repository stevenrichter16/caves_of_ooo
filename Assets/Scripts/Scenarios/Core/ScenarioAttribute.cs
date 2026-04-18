using System;

namespace CavesOfOoo.Scenarios
{
    /// <summary>
    /// Decorates an <see cref="IScenario"/> implementation with human-readable
    /// metadata used by the editor menu and (future) scenario browser window.
    ///
    /// Usage:
    ///   [Scenario("Five Snapjaw Ambush", category: "Combat Stress",
    ///       description: "Player surrounded by 5 snapjaws in a ring.")]
    ///   public class FiveSnapjawAmbush : IScenario { ... }
    ///
    /// The menu path is assembled as "Caves Of Ooo/Scenarios/{Category}/{Name}".
    /// A matching entry must still be added to <c>ScenarioMenuItems.cs</c> in the
    /// Editor assembly (Unity's [MenuItem] is compile-time and can't be generated
    /// from attributes alone).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ScenarioAttribute : Attribute
    {
        /// <summary>Display name shown in menu and logs. Keep it under ~40 chars.</summary>
        public string Name { get; }

        /// <summary>Sub-menu grouping, e.g. "Combat Stress", "AI Behavior".</summary>
        public string Category { get; }

        /// <summary>One-line description for future browser/tooltip display.</summary>
        public string Description { get; }

        /// <summary>Reserved for future filtering/search (e.g. "warden", "ambush").</summary>
        public string[] Tags { get; }

        /// <summary>If true, hide from the menu but still discoverable at runtime.</summary>
        public bool Hidden { get; }

        public ScenarioAttribute(
            string name,
            string category = "Uncategorized",
            string description = "",
            string[] tags = null,
            bool hidden = false)
        {
            Name = name ?? "Unnamed Scenario";
            Category = string.IsNullOrEmpty(category) ? "Uncategorized" : category;
            Description = description ?? string.Empty;
            Tags = tags ?? System.Array.Empty<string>();
            Hidden = hidden;
        }
    }
}
