using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// What a row declares about its turn cost (machine architecture spec C4 / S3). <see cref="Unspecified"/> leaves the decision to the
    /// legacy command chains in the world menu and the pack menu, so every shipped row keeps charging exactly what it charged before; a
    /// producer that declares a time bypasses both chains and needs no edit to either.
    /// </summary>
    public enum ActionTime : sbyte
    {
        Unspecified = -1,
        /// <summary>Runs in the command transaction and spends no turn.</summary>
        Free = 0,
        /// <summary>Runs in the command transaction and spends one action only when it commits.</summary>
        OneAction = 1
    }

    public enum PackRecipeCategory : byte
    {
        None, WeaponParts, MedicalSupplies, ToolsAndGear, Food,
        AlchemySupplies, FieldSupplies, Materials, Reclamation, EquipmentWork
    }

    /// <summary>
    /// Describes a single action that can be performed on an inventory item.
    /// Mirrors Qud's InventoryAction: parts on the item declare actions via
    /// GetInventoryActions event, and the selected action fires an InventoryAction
    /// event back on the item with the Command string.
    /// </summary>
    public class InventoryAction
    {
        /// <summary>Internal action name (e.g., "Eat", "Apply", "Drink").</summary>
        public string Name;

        /// <summary>Display text for UI (e.g., "eat", "apply to self").</summary>
        public string Display;

        /// <summary>Command string fired as InventoryAction event.</summary>
        public string Command;

        /// <summary>Hotkey character ('\0' = no hotkey).</summary>
        public char Key;

        /// <summary>Sort priority. Higher = more prominent in menu.</summary>
        public int Priority;

        /// <summary>
        /// If true, fires InventoryAction event on the actor instead of the item.
        /// </summary>
        public bool FireOnActor;

        /// <summary>
        /// A carried item that performs this row instead of the menu's target. A world menu opened on a cell with nothing to carry the row (an oil slick is a tile coating) names the
        /// striker or torch in the actor's hands here, and runs the same command the pack menu would. Null for every ordinary row.
        /// </summary>
        public Entity Via;

        /// <summary>
        /// A short plain sentence saying what the row does, shown as small text in the menu while the row is highlighted (a stoke "feeds the fire and lights it"). Empty for rows whose
        /// label already says everything. Presentation only: it is never read by any rule, so it cannot change what a row does.
        /// </summary>
        public string Hint = "";

        /// <summary>Optional presentation for the Recipes tab. Both menus dispatch this same action.</summary>
        public PackRecipeCategory RecipeCategory { get; private set; }
        public string RecipeResult { get; private set; }
        public string RecipeTitle { get; private set; }
        public string RecipeEffects { get; private set; }
        public IReadOnlyList<string> RecipeHighlights { get; private set; }
        public string RecipeInstructions { get; private set; }

        public InventoryAction AsRecipe(PackRecipeCategory category, string title, string result = null,
            string effects = null, string instructions = null, IReadOnlyList<string> highlights = null)
        {
            RecipeCategory = category;
            RecipeResult = result ?? title;
            RecipeTitle = title;
            RecipeEffects = effects;
            RecipeHighlights = highlights;
            RecipeInstructions = instructions;
            return this;
        }

        /// <summary>Declared turn cost; <see cref="ActionTime.Unspecified"/> means the legacy chains decide.</summary>
        public ActionTime Time = ActionTime.Unspecified;

        /// <summary>
        /// The cells this row acts on, when it acts on a place or on a thing standing in one: where a device would be laid, the pool to draw from, the fire to douse. A row that acts on the item itself or on the actor leaves it null.
        /// The pack menu ignores it. The interact (C) menu of one of these cells offers the row, run by the carried item that produced it, so the tool never needs the pack opened (Docs/WORLD-TOOL-ROWS.md). A tool opts in by
        /// tagging the row it already shows in the pack; the command, its checks and its cost are the pack row's own.
        /// </summary>
        public List<(int x, int y)> TargetCells;

        /// <summary>
        /// True for a row that is about the place or the whole kit rather than the item that offers it ("make camp here" is offered by every kit you carry and does the same whichever runs it). The interact menu
        /// keeps one such row, does not name an item on it, and does not tell items apart by what they hold.
        /// </summary>
        public bool Shared;

        /// <summary>
        /// A heading shared by rows that are the same kind of work ("set down a kit here"). When the interact menu of a cell would show <see cref="WorldInteractionSystem.ToolGroupAt"/> or more rows under one heading, it
        /// shows a single row with the heading and the count that opens them instead, so a loaded pack never makes a wall of rows. Empty: never grouped (a row about one particular thing, such as a vent to scrape).
        /// </summary>
        public string Group = "";

        /// <summary>Puts this row under the heading <paramref name="heading"/> (see <see cref="ToolGroups"/>); returns the row so a producer can chain.</summary>
        public InventoryAction InGroup(string heading) { Group = heading ?? ""; return this; }

        /// <summary>Marks this row as acting on the cell (<paramref name="x"/>, <paramref name="y"/>); returns the row so a producer can tag as it adds.</summary>
        public InventoryAction AtCell(int x, int y) { (TargetCells ??= new List<(int x, int y)>()).Add((x, y)); return this; }

        /// <summary>Marks this row as acting on <paramref name="target"/>: every cell it occupies in <paramref name="zone"/>, so a big thing is offered the row from any side. A target that is not in the zone tags nothing.</summary>
        public InventoryAction AtEntity(Zone zone, Entity target)
        {
            if (zone == null || target == null) return this;
            foreach (var cell in zone.GetOccupiedCells(target)) if (cell != null) AtCell(cell.X, cell.Y);
            return this;
        }

        /// <summary>Whether this row acts on the cell (<paramref name="x"/>, <paramref name="y"/>).</summary>
        public bool ActsOnCell(int x, int y)
        {
            if (TargetCells == null) return false;
            for (int i = 0; i < TargetCells.Count; i++) if (TargetCells[i].x == x && TargetCells[i].y == y) return true;
            return false;
        }

        /// <summary>Stable token naming why the row is unavailable (for example "faulted"); empty when enabled.</summary>
        public string DisabledCode = "";

        /// <summary>One sentence shown instead of acting. A non-blank reason makes the row disabled.</summary>
        public string DisabledReason = "";

        /// <summary>A disabled row is listed dimmed; selecting it logs the reason, spends no turn, opens no transaction and keeps the menu open. It wins over <see cref="Time"/>.</summary>
        public bool IsDisabled => !string.IsNullOrWhiteSpace(DisabledReason);

        /// <summary>Command given to disabled rows so no consumer ever reads a null command; it is never dispatched.</summary>
        public const string DisabledCommandPrefix = "Disabled:";

        /// <summary>The command of an inert row: a section header or a "none carried" placeholder in a picker menu. It is shown but does nothing, so a summary of what the thing offers leaves it out.</summary>
        public const string InertCommand = "CraftNoop";

        public InventoryAction() { }

        public InventoryAction(string name, string display, string command,
            char key = '\0', int priority = 0, bool fireOnActor = false)
        {
            Name = name;
            Display = display;
            Command = command;
            Key = key;
            Priority = priority;
            FireOnActor = fireOnActor;
        }
    }

    /// <summary>The headings producers put placement-style rows under, so rows from different parts that are the same kind of work (a hush strip, a spring trap and a cord snare are all "lay a device") group together.</summary>
    public static class ToolGroups
    {
        public const string Lay = "lay a device here", SetDown = "set down a kit here", Pour = "pour out a liquid here", Spread = "spread a material here",
            Fill = "fill a flask from here", Plant = "plant a seed here", Cook = "cook food here";
    }

    /// <summary>
    /// Implemented by a Part whose item's rows differ in effect with what the item holds (a flask, a waterskin). The interact menu uses it only to tell two items of the same name apart when their rows read
    /// alike ("fill with water [liquid flask] (empty)" and "(11/12 water)"); items that hold the same collapse to one row. An empty string means nothing worth telling.
    /// </summary>
    public interface IToolMenuState
    {
        string ToolMenuState();
    }

    /// <summary>
    /// Helper to collect InventoryActions from a GetInventoryActions event.
    /// Stored as a parameter on the event; parts call AddAction() to register.
    /// </summary>
    public class InventoryActionList
    {
        public List<InventoryAction> Actions = new List<InventoryAction>();

        public InventoryAction AddAction(string name, string display, string command,
            char key = '\0', int priority = 0, bool fireOnActor = false, ActionTime time = ActionTime.Unspecified)
        {
            var action = new InventoryAction(name, display, command, key, priority, fireOnActor) { Time = time };
            Actions.Add(action);
            return action;
        }

        /// <summary>The same as <see cref="AddAction"/>, with the one-line description the menu shows while the row is highlighted.</summary>
        public InventoryAction AddActionWithHint(string name, string display, string command, string hint,
            char key = '\0', int priority = 0, bool fireOnActor = false, ActionTime time = ActionTime.Unspecified)
        {
            var action = AddAction(name, display, command, key, priority, fireOnActor, time);
            action.Hint = hint ?? "";
            return action;
        }

        /// <summary>
        /// Lists a row that cannot be used now, with a stable code and one sentence of why. It has no dispatchable command: selecting it only
        /// reports the reason. A blank reason would make an enabled, dead row, so it is replaced by a generic sentence rather than dropped.
        /// </summary>
        public InventoryAction AddDisabled(string name, string display, string code, string reason,
            char key = '\0', int priority = 0)
        {
            code = string.IsNullOrWhiteSpace(code) ? "unavailable" : code.Trim();
            if (string.IsNullOrWhiteSpace(reason)) reason = "That cannot be done right now.";
            var action = new InventoryAction(name, display, InventoryAction.DisabledCommandPrefix + code, key, priority)
            { DisabledCode = code, DisabledReason = reason.Trim() };
            Actions.Add(action);
            return action;
        }

        /// <summary>
        /// Sort actions by priority (descending), then by key, then by display name.
        /// </summary>
        public void Sort()
        {
            Actions.Sort((a, b) =>
            {
                int cmp = b.Priority.CompareTo(a.Priority);
                if (cmp != 0) return cmp;
                if (a.Key != '\0' && b.Key == '\0') return -1;
                if (a.Key == '\0' && b.Key != '\0') return 1;
                if (a.Key != b.Key) return a.Key.CompareTo(b.Key);
                return string.Compare(a.Display, b.Display, System.StringComparison.Ordinal);
            });
        }
    }
}
