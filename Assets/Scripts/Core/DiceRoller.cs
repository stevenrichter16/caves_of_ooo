using System;
using System.Text.RegularExpressions;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Parses and rolls dice expressions like "1d4", "2d6+3", "1d8-1".
    /// Faithful to Qud's dice notation. Seedable RNG for deterministic tests.
    /// </summary>
    public static class DiceRoller
    {
        private static readonly Regex DicePattern = new Regex(
            @"^(\d+)d(\d+)([+-]\d+)?$",
            RegexOptions.Compiled);

        /// <summary>
        /// Roll a dice expression string. Returns the total.
        /// Format: NdS[+/-M] where N=count, S=sides, M=modifier.
        /// </summary>
        public static int Roll(string expression, Random rng)
        {
            if (string.IsNullOrEmpty(expression))
                return 0;

            var match = DicePattern.Match(expression.Trim());
            if (!match.Success)
                return 0;

            int count = int.Parse(match.Groups[1].Value);
            int sides = int.Parse(match.Groups[2].Value);
            int modifier = 0;
            if (match.Groups[3].Success)
                modifier = int.Parse(match.Groups[3].Value);

            int total = modifier;
            for (int i = 0; i < count; i++)
                total += rng.Next(1, sides + 1);

            return total;
        }

        /// <summary>
        /// Roll a single die: 1 to sides inclusive.
        /// </summary>
        public static int Roll(int sides, Random rng)
        {
            return rng.Next(1, sides + 1);
        }

        /// <summary>
        /// Parse a dice expression into its components.
        /// Returns (count, sides, modifier). Returns (0,0,0) if invalid.
        /// </summary>
        public static (int count, int sides, int modifier) Parse(string expression)
        {
            if (string.IsNullOrEmpty(expression))
                return (0, 0, 0);

            var match = DicePattern.Match(expression.Trim());
            if (!match.Success)
                return (0, 0, 0);

            int count = int.Parse(match.Groups[1].Value);
            int sides = int.Parse(match.Groups[2].Value);
            int modifier = 0;
            if (match.Groups[3].Success)
                modifier = int.Parse(match.Groups[3].Value);

            return (count, sides, modifier);
        }
    }
}