using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Flat int-quality fact store. A fact with value 0 is equivalent to absent.
    /// Shared by NarrativeStatePart (global) and KnowledgePart (per-NPC).
    /// </summary>
    public sealed class FactBag
    {
        private readonly Dictionary<string, int> _facts = new Dictionary<string, int>();

        public int Get(string key)
        {
            _facts.TryGetValue(key, out int v);
            return v;
        }

        public void Set(string key, int value) => _facts[key] = value;

        public void Add(string key, int delta)
        {
            _facts.TryGetValue(key, out int current);
            _facts[key] = current + delta;
        }

        public void Clear(string key) => _facts.Remove(key);

        public bool Has(string key) => _facts.TryGetValue(key, out int v) && v != 0;

        public void Save(SaveWriter writer)
        {
            writer.Write(_facts.Count);
            foreach (var kvp in _facts)
            {
                writer.WriteString(kvp.Key);
                writer.Write(kvp.Value);
            }
        }

        public void Load(SaveReader reader)
        {
            _facts.Clear();
            int count = reader.ReadInt();
            for (int i = 0; i < count; i++)
            {
                string key = reader.ReadString();
                int value = reader.ReadInt();
                _facts[key] = value;
            }
        }
    }
}
