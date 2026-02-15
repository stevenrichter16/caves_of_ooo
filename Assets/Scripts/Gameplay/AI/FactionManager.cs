using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Static registry for faction relationships and hostility checks.
    /// Loads faction definitions from Factions.json and tracks:
    /// - Faction-to-faction feelings (flat table, -100 to +100)
    /// - Player reputation via PlayerReputation (integrated into GetFeeling)
    /// Hostile at feeling <= -10, allied at >= 50.
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
        /// Faction metadata loaded from JSON.
        /// </summary>
        private static Dictionary<string, FactionEntry> _factionData
            = new Dictionary<string, FactionEntry>();

        /// <summary>
        /// Initialize from JSON faction data.
        /// </summary>
        public static void Initialize(string json)
        {
            Reset();

            var fileData = FactionLoader.Load(json);

            // Always register Player
            RegisterFaction("Player");

            // Register all factions from data and set inter-faction feelings
            for (int i = 0; i < fileData.Factions.Length; i++)
            {
                var entry = fileData.Factions[i];
                RegisterFaction(entry.Name);
                _factionData[entry.Name] = entry;

                if (entry.Feelings != null)
                {
                    for (int j = 0; j < entry.Feelings.Length; j++)
                    {
                        var feeling = entry.Feelings[j];
                        SetFactionFeeling(entry.Name, feeling.Faction, feeling.Value);
                    }
                }
            }

            // Initialize player reputation from faction data
            PlayerReputation.Initialize(fileData.Factions);
        }

        /// <summary>
        /// Initialize with hardcoded defaults (for tests or when no JSON available).
        /// </summary>
        public static void Initialize()
        {
            Reset();

            RegisterFaction("Player");
            RegisterFaction("Snapjaws");
            RegisterFaction("Villagers");

            // Snapjaws hate the player and villagers
            SetFactionFeeling("Snapjaws", "Villagers", -100);
            SetFactionFeeling("Villagers", "Snapjaws", -100);

            // Store minimal faction data for display
            _factionData["Snapjaws"] = new FactionEntry
            {
                Name = "Snapjaws",
                DisplayName = "the Snapjaws",
                Visible = true,
                InitialPlayerReputation = -100
            };
            _factionData["Villagers"] = new FactionEntry
            {
                Name = "Villagers",
                DisplayName = "the Villagers",
                Visible = true,
                InitialPlayerReputation = 50
            };

            // Initialize player reputation from hardcoded data
            PlayerReputation.Initialize(new[]
            {
                _factionData["Snapjaws"],
                _factionData["Villagers"]
            });
        }

        /// <summary>
        /// Clear all faction data (for test isolation).
        /// </summary>
        public static void Reset()
        {
            _factionFeelings.Clear();
            _registeredFactions.Clear();
            _factionData.Clear();
            PlayerReputation.Reset();
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
        /// Uses PlayerReputation when one entity is the player.
        /// </summary>
        public static int GetFeeling(Entity source, Entity target)
        {
            if (source == null || target == null) return 0;
            if (source == target) return 100; // self

            // Per-entity hostility overrides faction feeling (bidirectional)
            var sourceBrain = source.GetPart<BrainPart>();
            if (sourceBrain != null && sourceBrain.IsPersonallyHostileTo(target))
                return -100;
            var targetBrain = target.GetPart<BrainPart>();
            if (targetBrain != null && targetBrain.IsPersonallyHostileTo(source))
                return -100;

            // Player reputation integration
            if (source.HasTag("Player"))
                return PlayerReputation.GetFeeling(GetFaction(target));
            if (target.HasTag("Player"))
                return PlayerReputation.GetFeeling(GetFaction(source));

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

        /// <summary>
        /// Get metadata for a faction (display name, visibility, etc.).
        /// Returns null if the faction has no metadata.
        /// </summary>
        public static FactionEntry GetFactionData(string factionName)
        {
            if (string.IsNullOrEmpty(factionName)) return null;
            _factionData.TryGetValue(factionName, out var entry);
            return entry;
        }

        /// <summary>
        /// Get the display name for a faction. Falls back to the raw name.
        /// </summary>
        public static string GetDisplayName(string factionName)
        {
            var data = GetFactionData(factionName);
            return data?.DisplayName ?? factionName;
        }

        /// <summary>
        /// Get all registered faction names (excluding "Player").
        /// </summary>
        public static List<string> GetAllFactions()
        {
            var result = new List<string>();
            foreach (var name in _registeredFactions)
            {
                if (name != "Player")
                    result.Add(name);
            }
            return result;
        }

        /// <summary>
        /// Get all visible faction names (for UI display).
        /// </summary>
        public static List<string> GetAllVisibleFactions()
        {
            var result = new List<string>();
            foreach (var name in _registeredFactions)
            {
                if (name == "Player") continue;
                if (_factionData.TryGetValue(name, out var entry) && !entry.Visible) continue;
                result.Add(name);
            }
            return result;
        }
    }
}
