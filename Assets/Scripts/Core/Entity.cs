using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// The core game object. Every item, creature, wall, and effect is an Entity.
    /// Mirrors Qud's GameObject: a bag of Parts with Stats and Tags.
    /// Entities have no behavior of their own — all behavior comes from Parts.
    /// </summary>
    public class Entity
    {
        public string ID;
        public string BlueprintName;

        /// <summary>
        /// All parts attached to this entity.
        /// </summary>
        public List<Part> Parts = new List<Part>();

        /// <summary>
        /// Named statistics (HP, Strength, Speed, etc.)
        /// </summary>
        public Dictionary<string, Stat> Statistics = new Dictionary<string, Stat>();

        /// <summary>
        /// String key-value tags for metadata (Tier, Faction, etc.)
        /// </summary>
        public Dictionary<string, string> Tags = new Dictionary<string, string>();

        /// <summary>
        /// String properties copied from blueprint.
        /// </summary>
        public Dictionary<string, string> Properties = new Dictionary<string, string>();

        /// <summary>
        /// Integer properties copied from blueprint.
        /// </summary>
        public Dictionary<string, int> IntProperties = new Dictionary<string, int>();

        // --- Part Management ---

        public void AddPart(Part part)
        {
            part.ParentEntity = this;
            Parts.Add(part);
            part.Initialize();
        }

        public bool RemovePart(Part part)
        {
            if (Parts.Remove(part))
            {
                part.Remove();
                part.ParentEntity = null;
                return true;
            }
            return false;
        }

        public T GetPart<T>() where T : Part
        {
            for (int i = 0; i < Parts.Count; i++)
            {
                if (Parts[i] is T typed)
                    return typed;
            }
            return null;
        }

        public Part GetPart(string name)
        {
            for (int i = 0; i < Parts.Count; i++)
            {
                if (Parts[i].Name == name)
                    return Parts[i];
            }
            return null;
        }

        public bool HasPart<T>() where T : Part
        {
            return GetPart<T>() != null;
        }

        public bool HasPart(string name)
        {
            return GetPart(name) != null;
        }

        // --- Stat Access ---

        public Stat GetStat(string name)
        {
            Statistics.TryGetValue(name, out Stat stat);
            return stat;
        }

        public int GetStatValue(string name, int defaultValue = 0)
        {
            if (Statistics.TryGetValue(name, out Stat stat))
                return stat.Value;
            return defaultValue;
        }

        public void SetStatValue(string name, int value)
        {
            if (Statistics.TryGetValue(name, out Stat stat))
                stat.Value = value;
        }

        // --- Tag Access ---

        public string GetTag(string name, string defaultValue = null)
        {
            if (Tags.TryGetValue(name, out string value))
                return value;
            return defaultValue;
        }

        public bool HasTag(string name)
        {
            return Tags.ContainsKey(name);
        }

        public void SetTag(string name, string value = "")
        {
            Tags[name] = value;
        }

        // --- Property Access ---

        public string GetProperty(string name, string defaultValue = null)
        {
            if (Properties.TryGetValue(name, out string value))
                return value;
            return defaultValue;
        }

        public int GetIntProperty(string name, int defaultValue = 0)
        {
            if (IntProperties.TryGetValue(name, out int value))
                return value;
            return defaultValue;
        }

        // --- Event System ---

        /// <summary>
        /// Fire an event on this entity. All parts get a chance to handle it.
        /// Returns true if no part stopped propagation (event was not "consumed").
        /// Returns false if a part returned false from HandleEvent (event was consumed).
        /// </summary>
        public bool FireEvent(GameEvent e)
        {
            for (int i = 0; i < Parts.Count; i++)
            {
                if (!Parts[i].HandleEvent(e))
                    return false;
                if (e.Handled)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Fire an event by ID with no parameters.
        /// </summary>
        public bool FireEvent(string eventID)
        {
            return FireEvent(GameEvent.New(eventID));
        }

        // --- Display ---

        /// <summary>
        /// Get the display name of this entity from its Render-equivalent part or blueprint name.
        /// </summary>
        public string GetDisplayName()
        {
            var render = GetPart("Render");
            if (render is RenderPart rp && !string.IsNullOrEmpty(rp.DisplayName))
                return rp.DisplayName;
            return BlueprintName ?? ID ?? "unknown";
        }

        public override string ToString()
        {
            return $"Entity({GetDisplayName()})";
        }
    }
}
