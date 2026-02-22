using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Utilities for Qud-style string encoded costs where each character is one unit.
    /// Example: "BBCr" => B:2, C:1, r:1.
    /// </summary>
    public static class BitCost
    {
        public static string Normalize(string bits)
        {
            if (string.IsNullOrEmpty(bits))
                return string.Empty;

            var chars = new List<char>(bits.Length);
            for (int i = 0; i < bits.Length; i++)
            {
                char c = bits[i];
                if (char.IsWhiteSpace(c) || c == ',' || c == ';' || c == '|')
                    continue;
                chars.Add(c);
            }

            return chars.Count == 0 ? string.Empty : new string(chars.ToArray());
        }

        public static Dictionary<char, int> ToCounts(string bits)
        {
            var result = new Dictionary<char, int>();
            string normalized = Normalize(bits);
            for (int i = 0; i < normalized.Length; i++)
            {
                char c = normalized[i];
                if (result.TryGetValue(c, out int count))
                    result[c] = count + 1;
                else
                    result[c] = 1;
            }

            return result;
        }
    }
}
