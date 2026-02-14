using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Qud-faithful stat modifier calculations.
    /// Modifier = Floor((score - 16) / 2)
    /// So: 16=+0, 18=+1, 20=+2, 14=-1, 12=-2, etc.
    /// </summary>
    public static class StatUtils
    {
        /// <summary>
        /// Get the modifier for a stat score.
        /// Qud formula: Floor((score - 16) / 2)
        /// </summary>
        public static int GetModifier(int score)
        {
            return (int)Math.Floor((score - 16) / 2.0);
        }

        /// <summary>
        /// Get the modifier for a named stat on an entity.
        /// </summary>
        public static int GetModifier(Entity entity, string statName)
        {
            int score = entity.GetStatValue(statName, 16);
            return GetModifier(score);
        }
    }
}