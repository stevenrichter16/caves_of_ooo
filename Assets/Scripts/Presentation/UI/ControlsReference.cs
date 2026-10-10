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
            new Row("Tab / Esc", "pause menu (save, load, controls, graphics, quit)"),
            new Row("F5", "quick save"),
            new Row("F6", "quick load"),
            new Row("F12", "toggle player invincibility (debug; resets on load)"),
            new Row("< / Shift+,", "stairs up; from the surface, open the world map"),
            new Row("> / Shift+.", "stairs down; on the world map, enter the selected destination"),
            new Row("World map: !", "named settlement; move onto its marker, then > to enter"),
            new Row("F1 / ?", "this controls list"),
        };

        /// <summary>Full optional reader; never logs or opens a UI on its own.</summary>
        public static string BuildReaderText()
        {
            var lines = new System.Collections.Generic.List<string> { "Controls", "" };
            lines.Add("Controller - world");
            lines.Add("Left stick: select a direction for free. RT: step (diagonals supported); neutral stick waits one turn. Held RT repeats until danger; release to rearm.");
            lines.Add("A: use the indicated cell, or a contextual action underfoot/nearby. LT + A: nearby interaction picker, or use the indicated cell.");
            lines.Add("B: recover at a nearby bed/campfire when injured; full health does nothing. LT + B: wait 1/10/100 turns, stopping for danger. Waiting does not heal.");
            lines.Add("D-pad left/right: select ability. X: use selected ability. LT + X: all abilities. LT + D-pad up/down: one page of ten slots; no other hotbar pages.");
            lines.Add("LB: attack nearest adjacent hostile. LT + LB: force attack in a chosen direction. LT + RB: pick a carried item to throw.");
            lines.Add("Y: walk toward a local edge. LT + Y: safe local exploration. Left stick click: visible known points of interest. Travel stops for danger/input, stays in this zone and does not auto-loot.");
            lines.Add("Right stick: look. D-pad up/down: stairs/world map. LT + D-pad left/right: zoom in/out. LT + RT: highlight visible points of interest.");
            lines.Add("Menu: character (inventory, attributes, skills, abilities, quests, factions, controls). View: pause (save/load, graphics, quit). LT + Menu: controls.");
            lines.Add("RB / right stick click / LT + right stick click: reserved fire / reload / replace cell. Missile weapons, reload and energy cells are unavailable in this build.");
            lines.Add("Controller - menus and targeting");
            lines.Add("D-pad/left stick: navigate menus. A: confirm. B: back. Y: tab; LB/RB: pages where supported. Right stick up/down: page readers. LT + RT: item details where supported.");
            lines.Add("Abilities screen: select an ability, then Y opens its hotbar-slot chooser. Choose a slot and press A to assign, or B to cancel.");
            lines.Add("Targeting: sticks/D-pad select a cell; A or RT confirms, B cancels. Release held controls after changing screens; release stair directions between actions.");
            lines.Add("At save/death prompts: A continue/load; X new game/restart. At character selection: D-pad browse, A begin.");
            lines.Add("");
            foreach (var row in Bindings) lines.Add(row.Key + " - " + row.What);
            lines.Add("\nInside inventory: / search names; F1 selected item/craft details; F2 current effects. Escape first exits search.");
            lines.Add("Inside loot and trade: F1 selected item details. Reading is free; Escape returns to the same selection.");
            lines.Add("Skills and abilities: D full details. Ability manager: P optional rite preview, then direction when requested.");
            return string.Join("\n", lines);
        }

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
            if (NativeGamepadInput.IsConnected)
                log("Controller: LS direction; RT step/wait; A use; X ability; Menu character; View pause; LT + Menu controls.");
            log("Move with WASD/arrows; bump enemies to attack. [I]nventory, [C] talk, [G]et, [L]ook.");
            log("[X] skills, [M] abilities, [F5] save, [F1] full controls.");
            log("Surface: [< / Shift+,] world map; move to a [!] settlement, [> / Shift+.] enter selected destination.");
            log("Villagers have work for you — press [Q] for your quest log.");
        }
    }
}
