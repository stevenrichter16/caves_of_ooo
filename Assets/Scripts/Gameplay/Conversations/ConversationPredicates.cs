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

            // Check player reputation with a faction: "FactionName:AttitudeLevel"
            // e.g., "RotChoir:Liked" checks if player attitude >= Liked
            Register("IfReputationAtLeast", (speaker, listener, arg) =>
            {
                int colon = arg.IndexOf(':');
                if (colon < 0) return false;
                string faction = arg.Substring(0, colon);
                string levelStr = arg.Substring(colon + 1);
                if (!System.Enum.TryParse<PlayerReputation.Attitude>(levelStr, out var required))
                    return false;
                return (int)PlayerReputation.GetAttitude(faction) >= (int)required;
            });

            // Check if player has any item with a specific tag in inventory
            Register("IfHaveItemWithTag", (speaker, listener, arg) =>
            {
                if (listener == null || string.IsNullOrEmpty(arg)) return false;
                var inv = listener.GetPart<InventoryPart>();
                if (inv == null) return false;
                for (int i = 0; i < inv.Objects.Count; i++)
                {
                    if (inv.Objects[i].HasTag(arg))
                        return true;
                }
                return false;
            });

            Register("IfSettlementSiteStage", (speaker, listener, arg) =>
            {
                if (speaker == null || string.IsNullOrWhiteSpace(arg) || SettlementManager.Current == null)
                    return false;

                string[] parts = arg.Split(':');
                if (parts.Length != 2)
                    return false;

                string settlementId = ResolveSettlementId(speaker);
                if (string.IsNullOrEmpty(settlementId))
                    return false;

                RepairableSiteState site = SettlementManager.Current.GetSite(settlementId, parts[0]);
                if (site == null)
                    return false;

                RepairStage stage;
                if (!Enum.TryParse(parts[1], out stage))
                    return false;

                return site.Stage == stage;
            });

            // Check a drama pressure point's activation state: "DramaId:PressurePointId:StateName"
            // e.g., "OrdrenDrama:Wound:Active"
            Register("IfDramaPressurePointState", (speaker, listener, arg) =>
            {
                if (speaker == null || string.IsNullOrWhiteSpace(arg) || SettlementManager.Current == null)
                    return false;

                string[] parts = arg.Split(':');
                if (parts.Length != 3)
                    return false;

                string settlementId = ResolveSettlementId(speaker);
                if (string.IsNullOrEmpty(settlementId))
                    return false;

                HouseDramaState drama = SettlementManager.Current.GetDrama(settlementId, parts[0]);
                if (drama == null)
                    return false;

                HousePressurePointState pp = drama.GetPressurePoint(parts[1]);
                if (pp == null)
                    return false;

                HouseDramaActivationState state;
                if (!Enum.TryParse(parts[2], out state))
                    return false;

                return pp.State == state;
            });

            // Check the drama's end state: "DramaId:EndStateName"
            // e.g., "OrdrenDrama:Restored"
            Register("IfDramaEndState", (speaker, listener, arg) =>
            {
                if (speaker == null || string.IsNullOrWhiteSpace(arg) || SettlementManager.Current == null)
                    return false;

                int colon = arg.IndexOf(':');
                if (colon < 0)
                    return false;

                string settlementId = ResolveSettlementId(speaker);
                if (string.IsNullOrEmpty(settlementId))
                    return false;

                HouseDramaState drama = SettlementManager.Current.GetDrama(settlementId, arg.Substring(0, colon));
                if (drama == null)
                    return false;

                HouseDramaEndState endState;
                if (!Enum.TryParse(arg.Substring(colon + 1), out endState))
                    return false;

                return drama.EndState == endState;
            });

            // Check if drama corruption score meets a minimum: "DramaId:MinValue"
            // e.g., "OrdrenDrama:3"
            Register("IfDramaCorruptionAtLeast", (speaker, listener, arg) =>
            {
                if (speaker == null || string.IsNullOrWhiteSpace(arg) || SettlementManager.Current == null)
                    return false;

                int colon = arg.IndexOf(':');
                if (colon < 0)
                    return false;

                string settlementId = ResolveSettlementId(speaker);
                if (string.IsNullOrEmpty(settlementId))
                    return false;

                HouseDramaState drama = SettlementManager.Current.GetDrama(settlementId, arg.Substring(0, colon));
                if (drama == null)
                    return false;

                int minScore;
                if (!int.TryParse(arg.Substring(colon + 1), out minScore))
                    return false;

                return drama.CorruptionScore >= minScore;
            });
        }

        private static string ResolveSettlementId(Entity speaker)
        {
            if (speaker == null)
                return null;

            string settlementId;
            return speaker.Properties.TryGetValue("SettlementId", out settlementId)
                ? settlementId
                : null;
        }

        public static void Reset()
        {
            _predicates.Clear();
            _initialized = false;
        }
    }
}
