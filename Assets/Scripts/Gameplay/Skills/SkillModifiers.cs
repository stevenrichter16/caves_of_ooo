using System;
using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Positive temporary skill ranks. Callers persist the returned receipt alongside
    /// the actor's already-saved BonusRank and remove only their own contribution.
    /// Skills use their authored rank cap; unused ranks are retained so removing one
    /// source never steals another's bonus. Learned ranks and purchase gates are separate.
    /// </summary>
    public static class SkillModifiers
    {
        public static IReadOnlyList<StatModifier> Parse(string definition)
        {
            var result = new List<StatModifier>();
            // Filter each entry before combining so a negative entry cannot cancel
            // an otherwise valid positive contribution.
            if (!string.IsNullOrWhiteSpace(definition))
                foreach (string entry in definition.Split(','))
                    foreach (var value in StatModifiers.Parse(entry))
                        if (value.Amount > 0 && IsKnown(value.StatName)) result.Add(value);
            return Normalize(result);
        }

        public static string Format(IEnumerable<StatModifier> values) => StatModifiers.Format(Normalize(values));
        public static string GetDisplayName(string className)
        {
            if (SkillRegistry.TryGetSkillByClass(className, out var root)) return root.Name;
            if (SkillRegistry.TryGetPowerByClass(className, out var power)) return power.Name;
            return className;
        }
        public static IReadOnlyList<StatModifier> Apply(Entity actor, IEnumerable<StatModifier> desired) => Replace(actor, null, desired);
        public static void Remove(Entity actor, IEnumerable<StatModifier> receipt) => Replace(actor, receipt, null);

        public static IReadOnlyList<StatModifier> Replace(Entity actor, IEnumerable<StatModifier> previous,
            IEnumerable<StatModifier> desired)
        {
            if (actor == null) return Array.Empty<StatModifier>();
            var old = ToDictionary(Normalize(previous));
            var next = Normalize(desired);
            var wanted = ToDictionary(next);
            var names = new SortedSet<string>(old.Keys, StringComparer.Ordinal);
            names.UnionWith(wanted.Keys);
            if (names.Count == 0) return next;
            var skills = actor.GetPart<SkillsPart>();
            bool created = skills == null;
            if (created) skills = new SkillsPart();
            var changes = new List<(string Name, int Before, int After)>();
            foreach (string name in names)
            {
                old.TryGetValue(name, out int was); wanted.TryGetValue(name, out int willBe);
                int before = skills.GetSkill(name)?.BonusRank ?? 0;
                long after = (long)before - was + willBe;
                if (after < 0 || after > int.MaxValue)
                    throw new InvalidOperationException("Invalid skill contribution receipt for " + name + ".");
                if (before != after) changes.Add((name, before, (int)after));
            }
            // Establish new capabilities before removing old ones; setup failure
            // should leave the old capability and its active ability untouched.
            changes.Sort((a, b) => (a.After == 0 ? 1 : 0).CompareTo(b.After == 0 ? 1 : 0));
            if (created) actor.AddPart(skills);
            int applied = 0;
            try
            {
                foreach (var change in changes)
                {
                    skills.SetBonusRank(change.Name, change.After);
                    applied++;
                }
            }
            catch
            {
                for (int i = applied - 1; i >= 0; i--)
                    skills.SetBonusRank(changes[i].Name, changes[i].Before);
                if (created && skills.SkillList.Count == 0) actor.RemovePart(skills);
                throw;
            }
            return next;
        }

        static bool IsKnown(string name) =>
            (SkillRegistry.TryGetSkillByClass(name, out _) || SkillRegistry.TryGetPowerByClass(name, out _))
            && SkillsPart.IsKnownSkillClass(name);
        static IReadOnlyList<StatModifier> Normalize(IEnumerable<StatModifier> values)
        {
            var valid = new List<StatModifier>();
            if (values != null)
                foreach (var value in values)
                    if (value.Amount > 0 && IsKnown(value.StatName)) valid.Add(value);
            return StatModifiers.Parse(StatModifiers.Format(valid));
        }
        static Dictionary<string, int> ToDictionary(IEnumerable<StatModifier> values)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var value in values) result[value.StatName] = value.Amount;
            return result;
        }
    }
}
