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

        /// <summary>Declared turn cost; <see cref="ActionTime.Unspecified"/> means the legacy chains decide.</summary>
        public ActionTime Time = ActionTime.Unspecified;

        /// <summary>Stable token naming why the row is unavailable (for example "faulted"); empty when enabled.</summary>
        public string DisabledCode = "";

        /// <summary>One sentence shown instead of acting. A non-blank reason makes the row disabled.</summary>
        public string DisabledReason = "";

        /// <summary>A disabled row is listed dimmed; selecting it logs the reason, spends no turn, opens no transaction and keeps the menu open. It wins over <see cref="Time"/>.</summary>
        public bool IsDisabled => !string.IsNullOrWhiteSpace(DisabledReason);

        /// <summary>Command given to disabled rows so no consumer ever reads a null command; it is never dispatched.</summary>
        public const string DisabledCommandPrefix = "Disabled:";

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
