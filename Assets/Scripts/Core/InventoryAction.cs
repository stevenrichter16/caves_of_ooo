using System.Collections.Generic;

namespace CavesOfOoo.Core
{
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

        public void AddAction(string name, string display, string command,
            char key = '\0', int priority = 0, bool fireOnActor = false)
        {
            Actions.Add(new InventoryAction(name, display, command, key, priority, fireOnActor));
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
