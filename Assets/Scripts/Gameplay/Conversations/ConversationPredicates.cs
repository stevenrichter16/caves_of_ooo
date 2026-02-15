using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Registry of conversation predicate functions.
    /// Each predicate takes (speaker, listener, argument) and returns true if the condition is met.
    /// Predicates prefixed with "IfNot" are auto-generated inverses.
    /// </summary>
    public static class ConversationPredicates
    {
        public delegate bool PredicateFunc(Entity speaker, Entity listener, string argument);

        private static Dictionary<string, PredicateFunc> _predicates
            = new Dictionary<string, PredicateFunc>();

        private static bool _initialized;

        public static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            RegisterDefaults();
        }

        public static void Register(string name, PredicateFunc func)
        {
            _predicates[name] = func;
        }

        public static bool Evaluate(string name, Entity speaker, Entity listener, string argument)
        {
            EnsureInitialized();

            if (_predicates.TryGetValue(name, out var func))
                return func(speaker, listener, argument);

            // Auto-handle IfNot* by inverting the base predicate
            if (name.StartsWith("IfNot") && name.Length > 5)
            {
                string baseName = "If" + name.Substring(5);
                if (_predicates.TryGetValue(baseName, out var baseFunc))
                    return !baseFunc(speaker, listener, argument);
            }

            UnityEngine.Debug.LogWarning($"[Conversation] Unknown predicate: '{name}'");
            return true; // unknown predicates pass by default
        }

        /// <summary>
        /// Check all predicates on a choice. Returns true if all pass (AND logic).
        /// </summary>
        public static bool CheckAll(List<Data.ConversationParam> predicates,
            Entity speaker, Entity listener)
        {
            if (predicates == null || predicates.Count == 0) return true;

            for (int i = 0; i < predicates.Count; i++)
            {
                if (!Evaluate(predicates[i].Key, speaker, listener, predicates[i].Value))
                    return false;
            }
            return true;
        }

        private static void RegisterDefaults()
        {
            // Tag checks (on listener/player)
            Register("IfHaveTag", (speaker, listener, arg) =>
                listener != null && listener.HasTag(arg));

            // Property checks (on listener/player)
            Register("IfHaveProperty", (speaker, listener, arg) =>
                listener != null && listener.Properties.ContainsKey(arg));

            // IntProperty checks
            Register("IfHaveIntProperty", (speaker, listener, arg) =>
                listener != null && listener.IntProperties.ContainsKey(arg));

            // Tag on speaker
            Register("IfSpeakerHaveTag", (speaker, listener, arg) =>
                speaker != null && speaker.HasTag(arg));

            // Property on speaker
            Register("IfSpeakerHaveProperty", (speaker, listener, arg) =>
                speaker != null && speaker.Properties.ContainsKey(arg));

            // Faction feeling check: "FactionA:FactionB:MinValue"
            // or simpler: just minimum feeling as int
            Register("IfFactionFeelingAtLeast", (speaker, listener, arg) =>
            {
                if (!int.TryParse(arg, out int minFeeling)) return false;
                int feeling = FactionManager.GetFeeling(listener, speaker);
                return feeling >= minFeeling;
            });

            // Check if player has item by blueprint name in inventory
            Register("IfHaveItem", (speaker, listener, arg) =>
            {
                if (listener == null) return false;
                var inv = listener.GetPart<InventoryPart>();
                if (inv == null) return false;
                for (int i = 0; i < inv.Objects.Count; i++)
                {
                    if (inv.Objects[i].BlueprintName == arg)
                        return true;
                }
                return false;
            });

            // Stat check: "StatName:MinValue"
            Register("IfStatAtLeast", (speaker, listener, arg) =>
            {
                if (listener == null) return false;
                int colon = arg.IndexOf(':');
                if (colon < 0) return false;
                string stat = arg.Substring(0, colon);
                if (!int.TryParse(arg.Substring(colon + 1), out int minVal)) return false;
                return listener.GetStatValue(stat) >= minVal;
            });

            // Check if not hostile
            Register("IfNotHostile", (speaker, listener, arg) =>
                !FactionManager.IsHostile(speaker, listener));
        }

        public static void Reset()
        {
            _predicates.Clear();
            _initialized = false;
        }
    }
}
