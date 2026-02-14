using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Static registry for faction relationships and hostility checks.
    /// Mirrors Qud's Factions/Faction system (simplified):
    /// - Flat faction-to-faction feeling table (-100 to +100)
    /// - Hostile at feeling <= -10, allied at >= 50
    /// - Same faction = +100 (implicit)
    /// </summary>
    public static class FactionManager
    {
        public const int HOSTILE_THRESHOLD = -10;
        public const int ALLIED_THRESHOLD = 50;

        /// <summary>
        /// Outer key = faction that feels, inner key = faction being felt about, value = feeling.
        /// </summary>
        private static Dictionary<string, Dictionary<string, int>> _factionFeelings
            = new Dictionary<string, Dictionary<string, int>>();

        private static HashSet<string> _registeredFactions = new HashSet<string>();

        /// <summary>
        /// Initialize default faction relationships.
        /// </summary>
        public static void Initialize()
        {
            Reset();

            RegisterFaction("Player");
            RegisterFaction("Snapjaws");

            // Snapjaws hate the player, player hates snapjaws
            SetFactionFeeling("Snapjaws", "Player", -100);
            SetFactionFeeling("Player", "Snapjaws", -100);
        }

        /// <summary>
        /// Clear all faction data (for test isolation).
        /// </summary>
        public static void Reset()
        {
            _factionFeelings.Clear();
            _registeredFactions.Clear();
        }

        /// <summary>
        /// Register a faction name. Idempotent.
        /// </summary>
        public static void RegisterFaction(string factionName)
        {
            if (string.IsNullOrEmpty(factionName)) return;
            if (_registeredFactions.Add(factionName))
            {
                _factionFeelings[factionName] = new Dictionary<string, int>();
            }
        }

        /// <summary>
        /// Set how factionA feels about factionB (-100 to +100).
        /// </summary>
        public static void SetFactionFeeling(string factionA, string factionB, int feeling)
        {
            if (string.IsNullOrEmpty(factionA) || string.IsNullOrEmpty(factionB)) return;
            RegisterFaction(factionA);
            if (!_factionFeelings.TryGetValue(factionA, out var feelings))
            {
                feelings = new Dictionary<string, int>();
                _factionFeelings[factionA] = feelings;
            }
            feelings[factionB] = feeling;
        }

        /// <summary>
        /// Get how factionA feels about factionB.
        /// Same faction returns +100. Unknown returns 0 (neutral).
        /// </summary>
        public static int GetFactionFeeling(string factionA, string factionB)
        {
            if (string.IsNullOrEmpty(factionA) || string.IsNullOrEmpty(factionB)) return 0;

            // Same faction = allies
            if (factionA == factionB) return 100;

            if (_factionFeelings.TryGetValue(factionA, out var feelings))
            {
                if (feelings.TryGetValue(factionB, out int feeling))
                    return feeling;
            }

            return 0; // neutral
        }

        /// <summary>
        /// Get how entityA feels about entityB, based on their factions.
        /// </summary>
        public static int GetFeeling(Entity source, Entity target)
        {
            if (source == null || target == null) return 0;
            if (source == target) return 100; // self

            string factionA = GetFaction(source);
            string factionB = GetFaction(target);

            if (string.IsNullOrEmpty(factionA) || string.IsNullOrEmpty(factionB))
                return 0;

            return GetFactionFeeling(factionA, factionB);
        }

        /// <summary>
        /// Is source hostile toward target? (feeling <= -10)
        /// </summary>
        public static bool IsHostile(Entity source, Entity target)
        {
            return GetFeeling(source, target) <= HOSTILE_THRESHOLD;
        }

        /// <summary>
        /// Is source allied with target? (feeling >= 50)
        /// </summary>
        public static bool IsAllied(Entity source, Entity target)
        {
            return GetFeeling(source, target) >= ALLIED_THRESHOLD;
        }

        /// <summary>
        /// Get an entity's primary faction.
        /// Entities with the "Player" tag return "Player".
        /// Otherwise reads the "Faction" tag.
        /// </summary>
        public static string GetFaction(Entity entity)
        {
            if (entity == null) return null;
            if (entity.HasTag("Player")) return "Player";
            return entity.GetTag("Faction");
        }
    }
}
