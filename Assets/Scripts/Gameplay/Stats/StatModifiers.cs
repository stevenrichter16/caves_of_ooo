using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Shared signed flat modifiers for equipment, effects and other callers.
    /// Each caller owns and persists its returned receipt, and removes that receipt
    /// when its contribution ends. Saved Bonus already contains the contribution:
    /// restoring a receipt must never replay Apply. BaseValue is never changed.
    /// </summary>
    public static class StatModifiers
    {
        /// <summary>
        /// Reads the existing "Strength:2,Agility:-1" content format. Malformed
        /// entries are ignored; exact-case duplicates are combined. An overflowing
        /// combined amount is rejected rather than wrapping into a different bonus.
        /// </summary>
        public static IReadOnlyList<StatModifier> Parse(string definition)
        {
            var modifiers = new List<StatModifier>();
            if (!string.IsNullOrWhiteSpace(definition))
                foreach (string entry in definition.Split(','))
                {
                    int separator = entry.IndexOf(':');
                    if (separator <= 0 || separator == entry.Length - 1) continue;
                    string name = entry.Substring(0, separator).Trim();
                    if (!ValidName(name) || !int.TryParse(entry.Substring(separator + 1).Trim(),
                        NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount)) continue;
                    modifiers.Add(new StatModifier(name, amount));
                }
            return ToList(Combine(modifiers));
        }

        /// <summary>Stable content/receipt representation, suitable for saved Part fields.</summary>
        public static string Format(IEnumerable<StatModifier> modifiers)
        {
            var result = new StringBuilder();
            foreach (var modifier in ToList(Combine(modifiers)))
            {
                if (result.Length > 0) result.Append(',');
                result.Append(modifier.StatName).Append(':').Append(modifier.Amount.ToString(CultureInfo.InvariantCulture));
            }
            return result.ToString();
        }

        /// <summary>
        /// Adds an independent contribution and returns exactly what was applied.
        /// Missing stats are skipped, except the four implicit elemental resistances
        /// when explicitly requested. Keep this receipt to remove only this source.
        /// Arithmetic is validated before any statistic is changed.
        /// </summary>
        public static IReadOnlyList<StatModifier> Apply(Entity target, IEnumerable<StatModifier> modifiers,
            bool createMissingElementalResistances = false) =>
            Replace(target, null, modifiers, createMissingElementalResistances);

        /// <summary>Removes the recorded contribution without recalculating its definition.</summary>
        public static void Remove(Entity target, IEnumerable<StatModifier> applied) => Replace(target, applied, null);

        /// <summary>
        /// Atomically replaces one caller's prior receipt. Repeating the same replacement
        /// is idempotent; unrelated bonuses remain intact. Throws OverflowException
        /// before mutation if Bonus or the unclamped stat value would overflow an int.
        /// </summary>
        public static IReadOnlyList<StatModifier> Replace(Entity target, IEnumerable<StatModifier> previous,
            IEnumerable<StatModifier> desired, bool createMissingElementalResistances = false)
        {
            if (target == null) return Array.Empty<StatModifier>();
            var oldAmounts = Combine(previous);
            var newAmounts = Combine(desired);
            var names = new HashSet<string>(oldAmounts.Keys, StringComparer.Ordinal);
            names.UnionWith(newAmounts.Keys);
            var changes = new List<Change>();
            var applied = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (string name in names)
            {
                oldAmounts.TryGetValue(name, out int oldAmount);
                newAmounts.TryGetValue(name, out int newAmount);
                var stat = target.GetStat(name);
                bool create = stat == null && newAmount != 0 && createMissingElementalResistances && IsElementalResistance(name);
                if (create)
                {
                    stat = new Stat { Owner = target, Name = name, BaseValue = 0, Min = -100, Max = 100 };
                    // The previous stat no longer exists; a new zero-based stat has
                    // no previous contribution to remove.
                    oldAmount = 0;
                }
                if (stat == null) continue;
                long bonus = (long)stat.Bonus - oldAmount + newAmount;
                long rawValue = (long)stat.BaseValue + bonus - stat.Penalty + stat.Boost;
                if (bonus < int.MinValue || bonus > int.MaxValue || rawValue < int.MinValue || rawValue > int.MaxValue)
                    throw new OverflowException("Stat modifier exceeds the supported range for " + name + ".");
                changes.Add(new Change(name, stat, (int)bonus, create));
                if (newAmount != 0) applied[name] = newAmount;
            }
            foreach (var change in changes)
            {
                if (change.Create) target.Statistics[change.Name] = change.Stat;
                change.Stat.Bonus = change.Bonus;
            }
            return ToList(applied);
        }

        internal static bool ValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.IndexOf(',') >= 0 || name.IndexOf(':') >= 0) return false;
            foreach (char character in name) if (char.IsControl(character)) return false;
            return true;
        }

        static Dictionary<string, int> Combine(IEnumerable<StatModifier> modifiers)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            if (modifiers != null)
                foreach (var modifier in modifiers)
                {
                    if (!ValidName(modifier.StatName) || modifier.Amount == 0) continue;
                    result.TryGetValue(modifier.StatName, out int previous);
                    int sum = checked(previous + modifier.Amount);
                    if (sum == 0) result.Remove(modifier.StatName);
                    else result[modifier.StatName] = sum;
                }
            return result;
        }

        static IReadOnlyList<StatModifier> ToList(Dictionary<string, int> amounts)
        {
            var names = new List<string>(amounts.Keys);
            names.Sort(StringComparer.Ordinal);
            var result = new StatModifier[names.Count];
            for (int i = 0; i < names.Count; i++) result[i] = new StatModifier(names[i], amounts[names[i]]);
            return result;
        }

        static bool IsElementalResistance(string name) => name == "HeatResistance" || name == "ColdResistance"
            || name == "ElectricResistance" || name == "AcidResistance";

        readonly struct Change
        {
            internal readonly string Name;
            internal readonly Stat Stat;
            internal readonly int Bonus;
            internal readonly bool Create;
            internal Change(string name, Stat stat, int bonus, bool create)
            { Name = name; Stat = stat; Bonus = bonus; Create = create; }
        }
    }
}
