using System;
using System.Collections.Generic;
using System.Reflection;

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
            {
                int oldValue = stat.Value;
                stat.Value = value;
                int newValue = stat.Value;

                if (newValue != oldValue)
                {
                    var statChanged = GameEvent.New("StatChanged");
                    statChanged.SetParameter("Stat", name);
                    statChanged.SetParameter("OldValue", oldValue);
                    statChanged.SetParameter("NewValue", newValue);
                    FireEvent(statChanged);
                }
            }
        }

        // --- MP Economy ---

        /// <summary>
        /// Qud-style check: entities can only gain mutation points if they have an MP stat.
        /// </summary>
        public bool CanGainMP()
        {
            return Statistics.ContainsKey("MP");
        }

        /// <summary>
        /// Gain mutation points by increasing MP.BaseValue and firing GainedMP.
        /// </summary>
        public bool GainMP(int amount)
        {
            if (Statistics.TryGetValue("MP", out Stat stat))
            {
                stat.BaseValue += amount;
                FireEvent(GameEvent.New("GainedMP", "Amount", amount));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Spend mutation points by increasing MP.Penalty and firing UsedMP with context.
        /// </summary>
        public bool UseMP(int amount, string context = "default")
        {
            if (Statistics.TryGetValue("MP", out Stat stat))
            {
                stat.Penalty += amount;
                var usedMP = GameEvent.New("UsedMP");
                usedMP.SetParameter("Amount", amount);
                usedMP.SetParameter("Context", context ?? "default");
                FireEvent(usedMP);
                return true;
            }

            return false;
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

        public void SetIntProperty(string name, int value, bool removeIfZero = false)
        {
            int oldValue = GetIntProperty(name, 0);

            if (removeIfZero && value == 0)
            {
                IntProperties.Remove(name);
            }
            else
            {
                IntProperties[name] = value;
            }

            int newValue = GetIntProperty(name, 0);
            if (newValue != oldValue)
            {
                var changed = GameEvent.New("IntPropertyChanged");
                changed.SetParameter("Name", name);
                changed.SetParameter("OldValue", oldValue);
                changed.SetParameter("NewValue", newValue);
                FireEvent(changed);
            }
        }

        public int ModIntProperty(string name, int delta, bool removeIfZero = false)
        {
            int value = GetIntProperty(name, 0) + delta;
            SetIntProperty(name, value, removeIfZero);
            return value;
        }

        public void RemoveIntProperty(string name)
        {
            int oldValue = GetIntProperty(name, 0);
            if (IntProperties.Remove(name))
            {
                var changed = GameEvent.New("IntPropertyChanged");
                changed.SetParameter("Name", name);
                changed.SetParameter("OldValue", oldValue);
                changed.SetParameter("NewValue", 0);
                FireEvent(changed);
            }
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

        // --- Cloning ---

        /// <summary>
        /// Create a structural clone of this entity suitable for stack splitting.
        /// Copies tags, properties, stats, and parts (public fields on derived types).
        /// </summary>
        public Entity CloneForStack()
        {
            var clone = new Entity();
            clone.BlueprintName = BlueprintName;

            foreach (var kvp in Tags)
                clone.Tags[kvp.Key] = kvp.Value;
            foreach (var kvp in Properties)
                clone.Properties[kvp.Key] = kvp.Value;
            foreach (var kvp in IntProperties)
                clone.IntProperties[kvp.Key] = kvp.Value;
            foreach (var kvp in Statistics)
            {
                clone.Statistics[kvp.Key] = new Stat(kvp.Value) { Owner = clone };
            }

            foreach (var part in Parts)
            {
                var type = part.GetType();
                var newPart = (Part)Activator.CreateInstance(type);
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (field.DeclaringType == typeof(Part)) continue;
                    field.SetValue(newPart, field.GetValue(part));
                }
                clone.AddPart(newPart);
            }

            // Clear context references — clone starts fresh
            var clonePhysics = clone.GetPart<PhysicsPart>();
            if (clonePhysics != null)
            {
                clonePhysics.InInventory = null;
                clonePhysics.Equipped = null;
            }

            return clone;
        }

        // --- Display ---

        /// <summary>
        /// Get the display name of this entity from its Render-equivalent part or blueprint name.
        /// Includes stack count if stacked (e.g. "torch (x5)").
        /// </summary>
        public string GetDisplayName()
        {
            var render = GetPart("Render");
            string name;
            if (render is RenderPart rp && !string.IsNullOrEmpty(rp.DisplayName))
                name = rp.DisplayName;
            else
                name = BlueprintName ?? ID ?? "unknown";

            var stacker = GetPart<StackerPart>();
            if (stacker != null && stacker.StackCount > 1)
                name += $" (x{stacker.StackCount})";

            return name;
        }

        public override string ToString()
        {
            return $"Entity({GetDisplayName()})";
        }
    }
}
