using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    internal static class ArmorTinkerModificationUtility
    {
        internal static bool ValidateArmorTarget(Entity item, string modTag, string modName, out string reason)
        {
            reason = string.Empty;
            if (item == null)
            {
                reason = "Target item is missing.";
                return false;
            }

            if (!item.HasPart<ArmorPart>())
            {
                reason = modName + " can only be applied to armor.";
                return false;
            }

            if (item.HasTag(modTag))
            {
                reason = "Item already has " + modName + ".";
                return false;
            }

            var stacker = item.GetPart<StackerPart>();
            if (stacker != null && stacker.StackCount > 1)
            {
                reason = "Split the stack before applying a modification.";
                return false;
            }

            return true;
        }

        internal static void AddDisplayPrefix(Entity item, string prefix)
        {
            if (item == null || string.IsNullOrWhiteSpace(prefix))
                return;

            var render = item.GetPart<RenderPart>();
            if (render == null || string.IsNullOrWhiteSpace(render.DisplayName))
                return;

            string normalizedPrefix = prefix.Trim();
            string current = render.DisplayName.Trim();
            if (!current.StartsWith(normalizedPrefix + " ", StringComparison.OrdinalIgnoreCase))
                render.DisplayName = normalizedPrefix + " " + current;
        }

        internal static void ApplyEquippedSpeedPenaltyDelta(Entity item, int deltaPenalty)
        {
            if (item == null || deltaPenalty == 0)
                return;

            var equippedOn = item.GetPart<PhysicsPart>()?.Equipped;
            var speed = equippedOn?.GetStat("Speed");
            if (speed != null)
                speed.Penalty += deltaPenalty;
        }

        internal static void ApplyEquippedStatBonusDelta(Entity item, string statName, int deltaBonus)
        {
            if (item == null || string.IsNullOrWhiteSpace(statName) || deltaBonus == 0)
                return;

            var equippedOn = item.GetPart<PhysicsPart>()?.Equipped;
            if (equippedOn == null) return;
            var equippable = item.GetPart<EquippablePart>();
            var contribution = new StatModifier(statName, deltaBonus);
            if (equippable == null)
            {
                StatModifiers.Apply(equippedOn, new[] { contribution });
                return;
            }
            var previous = StatModifiers.Parse(equippable.AppliedEquipBonuses);
            var desired = new List<StatModifier>(previous) { contribution };
            equippable.AppliedEquipBonuses = StatModifiers.Format(StatModifiers.Replace(equippedOn, previous, desired));
        }

        internal static void AddOrUpdateEquipBonus(Entity item, string statName, int deltaAmount)
        {
            if (item == null || string.IsNullOrWhiteSpace(statName) || deltaAmount == 0)
                return;

            var equippable = item.GetPart<EquippablePart>();
            if (equippable == null)
                return;

            var before = StatModifiers.Parse(equippable.EquipBonuses);
            string updated = StatModifiers.Format(new List<StatModifier>(before) { new StatModifier(statName, deltaAmount) });
            var equippedOn = item.GetPart<PhysicsPart>()?.Equipped;
            if (equippedOn != null && equippable.AppliedEquipBonuses == null)
            {
                // Adopt the pre-modification definition of legacy equipped gear.
                // ApplyEquippedStatBonusDelta will extend this receipt by the exact
                // contribution that lands, without replaying its existing bonuses.
                var legacy = new List<StatModifier>();
                foreach (var modifier in before)
                    if (equippedOn.GetStat(modifier.StatName) != null) legacy.Add(modifier);
                equippable.AppliedEquipBonuses = StatModifiers.Format(legacy);
            }
            equippable.EquipBonuses = updated;
        }
    }
}
