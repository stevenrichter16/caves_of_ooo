using System;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// ALPHA-READINESS item 5 — the single DISPLAY table of key
    /// bindings, feeding the F1/'?' help dump, the boot summary, and
    /// the pause menu's Controls entry. Display-only by design: input
    /// dispatch still lives in InputHandler's hard-coded checks (a
    /// rebind UI would need a dispatch refactor — out of alpha scope,
    /// noted by the plan verifier). When a binding changes, update it
    /// HERE and in the dispatch site; AlphaOnboardingTests pins the
    /// core rows so the table cannot silently rot to empty.
    /// </summary>
    public static class ControlsReference
    {
        public struct Row
        {
            public string Key;
            public string What;
            public Row(string key, string what) { Key = key; What = what; }
        }

        public static readonly Row[] Bindings =
        {
            new Row("WASD / arrows / numpad", "move (bump a hostile to attack)"),
            new Row(". / numpad-5", "wait a turn"),
            new Row("I", "inventory (equip, use, drop, throw)"),
            new Row("C", "interact / talk (pick a direction)"),
            new Row("G / ,", "pick up what's underfoot"),
            new Row("L", "look mode (Enter on a tile for actions)"),
            new Row("X", "skills (spend skill points)"),
            new Row("M", "ability manager (bind abilities to slots)"),
            new Row("1-0", "use hotbar ability"),
            new Row("Q", "quest log"),
            new Row("Tab / Esc", "pause menu (save, load, controls, quit)"),
            new Row("F5", "quick save"),
            new Row("F6", "quick load"),
            new Row("F12", "toggle player invincibility (debug; resets on load)"),
            new Row("< / Shift+,", "stairs up; from the surface, open the world map"),
            new Row("> / Shift+.", "stairs down; on the world map, enter the selected destination"),
            new Row("World map: !", "named settlement; move onto its marker, then > to enter"),
            new Row("F1 / ?", "this controls list"),
        };

        /// <summary>Dump the full table, one line per binding.</summary>
        public static void PrintHelp(Action<string> log)
        {
            if (log == null) return;
            log("── Controls ──");
            for (int i = 0; i < Bindings.Length; i++)
                log($"{Bindings[i].Key} — {Bindings[i].What}");
        }

        /// <summary>The glanceable first-boot text: core keys, the
        /// surface/map travel loop and the call-to-adventure.</summary>
        public static void PrintBootSummary(Action<string> log)
        {
            if (log == null) return;
            log("Move with WASD/arrows; bump enemies to attack. [I]nventory, [C] talk, [G]et, [L]ook.");
            log("[X] skills, [M] abilities, [F5] save, [F1] full controls.");
            log("Surface: [< / Shift+,] world map; move to a [!] settlement, [> / Shift+.] enter selected destination.");
            log("Villagers have work for you — press [Q] for your quest log.");
        }
    }
}
