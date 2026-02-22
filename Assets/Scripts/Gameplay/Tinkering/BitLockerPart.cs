using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Stores Qud-style bits and known recipes on the crafter entity.
    /// Bits are represented as string-encoded symbols where each char is one unit.
    /// </summary>
    public class BitLockerPart : Part
    {
        public override string Name => "BitLocker";

        private readonly Dictionary<char, int> _bits = new Dictionary<char, int>();
        private readonly HashSet<string> _knownRecipes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public void AddBits(string bits)
        {
            string normalized = BitCost.Normalize(bits);
            for (int i = 0; i < normalized.Length; i++)
            {
                char bit = normalized[i];
                if (_bits.TryGetValue(bit, out int count))
                    _bits[bit] = count + 1;
                else
                    _bits[bit] = 1;
            }
        }

        public bool HasBits(string bits)
        {
            Dictionary<char, int> required = BitCost.ToCounts(bits);
            foreach (var kvp in required)
            {
                if (GetBitCount(kvp.Key) < kvp.Value)
                    return false;
            }

            return true;
        }

        public bool UseBits(string bits)
        {
            Dictionary<char, int> required = BitCost.ToCounts(bits);
            foreach (var kvp in required)
            {
                if (GetBitCount(kvp.Key) < kvp.Value)
                    return false;
            }

            foreach (var kvp in required)
            {
                int remaining = GetBitCount(kvp.Key) - kvp.Value;
                if (remaining > 0)
                    _bits[kvp.Key] = remaining;
                else
                    _bits.Remove(kvp.Key);
            }

            return true;
        }

        public int GetBitCount(char bit)
        {
            return _bits.TryGetValue(bit, out int count) ? count : 0;
        }

        public void LearnRecipe(string recipeId)
        {
            if (!string.IsNullOrWhiteSpace(recipeId))
                _knownRecipes.Add(recipeId);
        }

        public bool KnowsRecipe(string recipeId)
        {
            return !string.IsNullOrWhiteSpace(recipeId) && _knownRecipes.Contains(recipeId);
        }

        public IReadOnlyCollection<string> GetKnownRecipes()
        {
            return _knownRecipes;
        }
    }
}
