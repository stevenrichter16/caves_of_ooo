using System;
using System.Collections.Generic;
using System.Text;

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
            var stat = equippedOn?.GetStat(statName);
            if (stat != null)
                stat.Bonus += deltaBonus;
        }

        internal static void AddOrUpdateEquipBonus(Entity item, string statName, int deltaAmount)
        {
            if (item == null || string.IsNullOrWhiteSpace(statName) || deltaAmount == 0)
                return;

            var equippable = item.GetPart<EquippablePart>();
            if (equippable == null)
                return;

            var bonuses = ParseEquipBonuses(equippable.EquipBonuses);
            if (!bonuses.TryGetValue(statName, out int current))
                current = 0;

            int updated = current + deltaAmount;
            if (updated == 0)
                bonuses.Remove(statName);
            else
                bonuses[statName] = updated;

            equippable.EquipBonuses = SerializeEquipBonuses(bonuses);
        }

        private static Dictionary<string, int> ParseEquipBonuses(string raw)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(raw))
                return result;

            string[] pairs = raw.Split(',');
            for (int i = 0; i < pairs.Length; i++)
            {
                string entry = pairs[i].Trim();
                if (string.IsNullOrEmpty(entry))
                    continue;

                int colon = entry.IndexOf(':');
                if (colon <= 0 || colon >= entry.Length - 1)
                    continue;

                string stat = entry.Substring(0, colon).Trim();
                string amountRaw = entry.Substring(colon + 1).Trim();
                if (string.IsNullOrEmpty(stat))
                    continue;

                if (!int.TryParse(amountRaw, out int amount))
                    continue;

                if (!result.TryGetValue(stat, out int current))
                    current = 0;
                result[stat] = current + amount;
            }

            return result;
        }

        private static string SerializeEquipBonuses(Dictionary<string, int> bonuses)
        {
            if (bonuses == null || bonuses.Count == 0)
                return string.Empty;

            var keys = new List<string>(bonuses.Keys);
            keys.Sort(StringComparer.OrdinalIgnoreCase);

            var builder = new StringBuilder();
            bool first = true;
            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                int value = bonuses[key];
                if (value == 0)
                    continue;

                if (!first)
                    builder.Append(",");
                builder.Append(key);
                builder.Append(":");
                builder.Append(value);
                first = false;
            }

            return builder.ToString();
        }
    }
}
