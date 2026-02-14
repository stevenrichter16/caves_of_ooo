using System.Text;

namespace CavesOfOoo.Core.Anatomy
{
    /// <summary>
    /// Bitmask system for anatomical side/position of body parts.
    /// Mirrors Qud's Laterality: parts can be left/right, upper/lower, fore/hind.
    /// Multiple flags combine (e.g. upper-left = Upper | Left = 5).
    /// Value of 0 means no laterality. Value of 65535 means "any/match all".
    /// </summary>
    public static class Laterality
    {
        public const int NONE = 0;
        public const int LEFT = 1;
        public const int RIGHT = 2;
        public const int UPPER = 4;
        public const int LOWER = 8;
        public const int FORE = 16;
        public const int MID = 32;
        public const int HIND = 64;
        public const int ANY = 65535;

        /// <summary>
        /// Check if two laterality values match.
        /// ANY matches everything. Otherwise, checks if all bits in
        /// 'required' are present in 'actual'.
        /// </summary>
        public static bool Match(int actual, int required)
        {
            if (required == ANY || actual == ANY)
                return true;
            if (required == NONE)
                return true;
            return (actual & required) == required;
        }

        /// <summary>
        /// Get the adjective string for a laterality value (e.g. "left", "upper-right").
        /// Returns empty string for NONE.
        /// </summary>
        public static string GetAdjective(int laterality)
        {
            if (laterality == NONE || laterality == ANY)
                return "";

            var sb = new StringBuilder();

            if ((laterality & FORE) != 0) Append(sb, "fore");
            else if ((laterality & MID) != 0) Append(sb, "mid");
            else if ((laterality & HIND) != 0) Append(sb, "hind");

            if ((laterality & UPPER) != 0) Append(sb, "upper");
            else if ((laterality & LOWER) != 0) Append(sb, "lower");

            if ((laterality & LEFT) != 0) Append(sb, "left");
            else if ((laterality & RIGHT) != 0) Append(sb, "right");

            return sb.ToString();
        }

        /// <summary>
        /// Get the code for an adjective string. Returns NONE if not recognized.
        /// </summary>
        public static int GetCodeFromAdjective(string adj)
        {
            if (string.IsNullOrEmpty(adj))
                return NONE;

            int result = NONE;
            string lower = adj.ToLowerInvariant();

            if (lower.Contains("left")) result |= LEFT;
            if (lower.Contains("right")) result |= RIGHT;
            if (lower.Contains("upper")) result |= UPPER;
            if (lower.Contains("lower")) result |= LOWER;
            if (lower.Contains("fore")) result |= FORE;
            if (lower.Contains("mid")) result |= MID;
            if (lower.Contains("hind")) result |= HIND;

            return result;
        }

        /// <summary>
        /// Get the opposite laterality (left ↔ right, upper ↔ lower, etc.)
        /// </summary>
        public static int GetOpposite(int laterality)
        {
            int result = NONE;

            if ((laterality & LEFT) != 0) result |= RIGHT;
            else if ((laterality & RIGHT) != 0) result |= LEFT;

            if ((laterality & UPPER) != 0) result |= LOWER;
            else if ((laterality & LOWER) != 0) result |= UPPER;

            if ((laterality & FORE) != 0) result |= HIND;
            else if ((laterality & HIND) != 0) result |= FORE;

            // Preserve MID (no opposite)
            if ((laterality & MID) != 0) result |= MID;

            return result;
        }

        private static void Append(StringBuilder sb, string word)
        {
            if (sb.Length > 0)
                sb.Append('-');
            sb.Append(word);
        }
    }
}
