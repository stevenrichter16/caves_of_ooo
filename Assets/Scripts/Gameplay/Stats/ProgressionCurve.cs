using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// The target progression curve (Docs/PLACE-WORK-AND-PROGRESSION.md §3): which
    /// player levels each Strangeness Tier is played at, which creature levels it
    /// spawns, and what a creature of a given level is worth. CoO-original, except
    /// <see cref="ScaleKillXP"/>, which ports Qud's level-band award scaling
    /// (XRL.World.Parts/Experience.cs:33-50).
    /// </summary>
    public static class ProgressionCurve
    {
        public const int MaxTier = 5;

        // Bands meet: the top of one tier is the bottom of the next.
        static readonly int[] PlayerBandEdges = { 1, 5, 9, 13, 17, 20 };
        static readonly int[] CreatureBandEdges = { 1, 4, 8, 12, 16, 20 };

        /// <summary>XPValue = round(XPCubic · L³ + XPFloor). Tuned by the progression bench.</summary>
        public const double XPCubic = 0.6;
        public const int XPFloor = 4;

        static int ClampTier(int tier) => Math.Max(1, Math.Min(MaxTier, tier));

        /// <summary>Player levels a tier is meant to be played through.</summary>
        public static (int Min, int Max) PlayerLevelBand(int tier)
        {
            int t = ClampTier(tier);
            return (PlayerBandEdges[t - 1], PlayerBandEdges[t]);
        }

        /// <summary>Levels a creature of this tier may be authored at.</summary>
        public static (int Min, int Max) CreatureLevelBand(int tier)
        {
            int t = ClampTier(tier);
            return (CreatureBandEdges[t - 1], CreatureBandEdges[t]);
        }

        /// <summary>A creature's authored Tier tag, clamped to 1..MaxTier; untagged is tier 1.</summary>
        public static int CreatureTier(Entity creature)
        {
            string tag = creature?.GetTag("Tier");
            return int.TryParse(tag, out int tier) ? ClampTier(tier) : 1;
        }

        /// <summary>XP a creature of this level is worth before band scaling.</summary>
        public static int XPValueForLevel(int level)
        {
            int l = Math.Max(1, level);
            return (int)Math.Round(XPCubic * l * l * l + XPFloor);
        }

        /// <summary>Total XP needed to stand at <paramref name="level"/> from level 1.</summary>
        public static int CumulativeXPToReach(int level)
        {
            int total = 0;
            for (int l = 1; l < level; l++) total += LevelingSystem.XPToNextLevel(l);
            return total;
        }

        /// <summary>Qud's level band: a fifth of the level.</summary>
        public static int LevelBand(int level) => Math.Max(0, level) / 5;

        /// <summary>
        /// Qud parity (Experience.cs:33-50): a killer whose level band exceeds the
        /// victim's by one earns half, by two a tenth, by more nothing. Equal or
        /// lower bands earn the full value.
        /// </summary>
        public static int ScaleKillXP(int xp, int killerLevel, int victimLevel)
        {
            if (xp <= 0) return 0;
            int gap = LevelBand(killerLevel) - LevelBand(victimLevel);
            if (gap > 2) return 0;
            if (gap > 1) return xp / 10;
            if (gap > 0) return xp / 2;
            return xp;
        }
    }
}
