using System;

namespace CavesOfOoo.Core
{
    /// <summary>A flat, signed contribution to an existing statistic's Bonus.</summary>
    public readonly struct StatModifier
    {
        public string StatName { get; }
        public int Amount { get; }

        public StatModifier(string statName, int amount)
        {
            statName = statName?.Trim();
            if (!StatModifiers.ValidName(statName))
                throw new ArgumentException("A modifier requires a statistic name without commas, colons or control characters.", nameof(statName));
            StatName = statName;
            Amount = amount;
        }
    }
}
