using CavesOfOoo.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>
    /// Editor-only menu for toggling the Phase 10 AI goal-stack inspector.
    /// Flips <see cref="AIDebug.AIInspectorEnabled"/> so the next look-mode
    /// hover on a Creature-tagged entity will populate the inspector fields
    /// on <c>LookSnapshot</c>, which the sidebar FOCUS panel renders.
    ///
    /// The menu uses a checkmark (via the <c>validate</c> method) to show
    /// current state — consistent with Unity's <c>[MenuItem]</c> convention
    /// for toggle items.
    /// </summary>
    internal static class AIDebugMenu
    {
        private const string MenuPath = "Caves Of Ooo/Diagnostics/AI Goal Inspector";

        [MenuItem(MenuPath, priority = 200)]
        private static void ToggleInspector()
        {
            AIDebug.AIInspectorEnabled = !AIDebug.AIInspectorEnabled;
            Debug.Log(
                "[AIDebug] Goal inspector " +
                (AIDebug.AIInspectorEnabled ? "ENABLED." : "DISABLED.") +
                " Hover a creature in Look mode (L) to see their goal stack " +
                "+ last thought in the sidebar FOCUS panel.");
        }

        [MenuItem(MenuPath, validate = true)]
        private static bool ToggleInspector_Validate()
        {
            // Unity's menu system reads the return value to enable/disable
            // the item; the side effect we want is the checkmark. Unity
            // updates the checkmark from the Menu.SetChecked call below.
            Menu.SetChecked(MenuPath, AIDebug.AIInspectorEnabled);
            return true;
        }
    }
}
