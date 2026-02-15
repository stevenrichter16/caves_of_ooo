using System;
using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Tracks the player's reputation with each faction.
    /// Reputation is a raw integer value mapped to attitude tiers.
    /// Integrated with FactionManager.GetFeeling() for AI hostility.
    /// </summary>
    public static class PlayerReputation
    {
        public const int HATED_THRESHOLD = -150;
        public const int DISLIKED_THRESHOLD = -50;
        public const int LIKED_THRESHOLD = 50;
        public const int LOVED_THRESHOLD = 150;

        public const int MIN_REP = -200;
        public const int MAX_REP = 200;

        public enum Attitude { Hated, Disliked, Neutral, Liked, Loved }

        private static Dictionary<string, int> _reputation = new Dictionary<string, int>();

        /// <summary>
        /// Initialize player reputation from faction data.
        /// Seeds each faction's reputation from InitialPlayerReputation.
        /// </summary>
        public static void Initialize(FactionEntry[] factions)
        {
            _reputation.Clear();
            if (factions == null) return;

            for (int i = 0; i < factions.Length; i++)
            {
                var entry = factions[i];
                if (!string.IsNullOrEmpty(entry.Name))
                    _reputation[entry.Name] = Clamp(entry.InitialPlayerReputation);
            }
        }

        /// <summary>
        /// Clear all reputation data.
        /// </summary>
        public static void Reset()
        {
            _reputation.Clear();
        }

        /// <summary>
        /// Get raw reputation value with a faction.
        /// </summary>
        public static int Get(string faction)
        {
            if (string.IsNullOrEmpty(faction) || faction == "Player") return 0;
            _reputation.TryGetValue(faction, out int value);
            return value;
        }

        /// <summary>
        /// Set raw reputation value with a faction.
        /// </summary>
        public static void Set(string faction, int value)
        {
            if (string.IsNullOrEmpty(faction) || faction == "Player") return;
            _reputation[faction] = Clamp(value);
        }

        /// <summary>
        /// Modify reputation by delta. Logs messages and threshold changes.
        /// </summary>
        public static void Modify(string faction, int delta, bool silent = false)
        {
            if (string.IsNullOrEmpty(faction) || faction == "Player" || delta == 0) return;

            var oldAttitude = GetAttitude(faction);
            int oldRep = Get(faction);
            int newRep = Clamp(oldRep + delta);
            _reputation[faction] = newRep;
            var newAttitude = GetAttitude(faction);

            if (!silent)
            {
                string displayName = FactionManager.GetDisplayName(faction);
                if (delta > 0)
                    MessageLog.Add($"Your reputation with {displayName} improves.");
                else
                    MessageLog.Add($"Your reputation with {displayName} worsens.");

                if (oldAttitude != newAttitude)
                    MessageLog.Add($"{displayName} now considers you: {GetAttitudeLabel(newAttitude)}.");
            }
        }

        /// <summary>
        /// Get the attitude tier for a faction based on current reputation.
        /// </summary>
        public static Attitude GetAttitude(string faction)
        {
            int rep = Get(faction);
            if (rep <= HATED_THRESHOLD) return Attitude.Hated;
            if (rep <= DISLIKED_THRESHOLD) return Attitude.Disliked;
            if (rep >= LOVED_THRESHOLD) return Attitude.Loved;
            if (rep >= LIKED_THRESHOLD) return Attitude.Liked;
            return Attitude.Neutral;
        }

        /// <summary>
        /// Convert attitude to a feeling value for FactionManager integration.
        /// </summary>
        public static int GetFeeling(string faction)
        {
            if (string.IsNullOrEmpty(faction) || faction == "Player") return 0;

            switch (GetAttitude(faction))
            {
                case Attitude.Hated: return -100;
                case Attitude.Disliked: return -50;
                case Attitude.Liked: return 50;
                case Attitude.Loved: return 100;
                default: return 0;
            }
        }

        /// <summary>
        /// Get all factions the player has reputation with and their values.
        /// </summary>
        public static Dictionary<string, int> GetAll()
        {
            return new Dictionary<string, int>(_reputation);
        }

        /// <summary>
        /// Human-readable label for an attitude.
        /// </summary>
        public static string GetAttitudeLabel(Attitude attitude)
        {
            switch (attitude)
            {
                case Attitude.Hated: return "Hated";
                case Attitude.Disliked: return "Disliked";
                case Attitude.Liked: return "Liked";
                case Attitude.Loved: return "Loved";
                default: return "Neutral";
            }
        }

        private static int Clamp(int value)
        {
            return Math.Max(MIN_REP, Math.Min(MAX_REP, value));
        }
    }
}
