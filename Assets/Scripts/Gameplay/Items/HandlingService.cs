using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Central query surface for item handling. This keeps carry/throw logic and
    /// grip-derived defaults in one place while UsesSlots remains equip-authoritative.
    /// </summary>
    public static class HandlingService
    {
        public static GripType GetGripType(Entity item)
        {
            if (item == null)
                return GripType.OneHand;

            var handling = item.GetPart<HandlingPart>();
            if (handling != null)
                return handling.GripType;

            var equippable = item.GetPart<EquippablePart>();
            if (equippable != null && CountSlots(equippable.UsesSlots, "Hand") >= 2)
                return GripType.TwoHand;

            return GripType.OneHand;
        }

        public static bool IsCarryable(Entity item)
        {
            if (item == null)
                return false;

            var handling = item.GetPart<HandlingPart>();
            if (handling != null)
                return handling.Carryable;

            return item.GetPart<PhysicsPart>()?.Takeable ?? false;
        }

        public static bool IsThrowable(Entity item)
        {
            if (item == null)
                return false;

            var handling = item.GetPart<HandlingPart>();
            return handling?.Throwable ?? false;
        }

        public static int GetWeight(Entity item)
        {
            if (item == null)
                return 0;

            var handling = item.GetPart<HandlingPart>();
            var physics = item.GetPart<PhysicsPart>();
            if (handling != null && handling.Weight > 0)
                return handling.Weight;

            return physics?.Weight ?? 0;
        }

        public static int GetLiftStrengthRequirement(Entity item)
        {
            if (item == null)
                return 0;

            int effectiveWeight = Math.Max(0, GetWeight(item));
            int baseLift = (int)Math.Ceiling(effectiveWeight / 2.0);
            if (GetGripType(item) == GripType.OneHand)
                baseLift += 1;

            var handling = item.GetPart<HandlingPart>();
            int minLiftStrength = handling?.MinLiftStrength ?? 0;
            return Math.Max(baseLift, minLiftStrength);
        }

        public static int GetThrowStrengthRequirement(Entity item)
        {
            if (item == null)
                return 0;

            int requiredLift = GetLiftStrengthRequirement(item);
            int baseThrow = requiredLift + (GetGripType(item) == GripType.TwoHand ? 2 : 1);

            var handling = item.GetPart<HandlingPart>();
            int minThrowStrength = handling?.MinThrowStrength ?? 0;
            return Math.Max(baseThrow, minThrowStrength);
        }

        public static bool CanLift(Entity actor, Entity item, out string reason)
        {
            if (item == null)
            {
                reason = "There is nothing to carry.";
                return false;
            }

            var physics = item.GetPart<PhysicsPart>();
            if (physics == null || !physics.Takeable || !IsCarryable(item))
            {
                reason = $"You can't carry {item.GetDisplayName()}.";
                return false;
            }

            int requiredStrength = GetLiftStrengthRequirement(item);
            int strength = actor?.GetStatValue("Strength", 0) ?? 0;
            if (requiredStrength > 0 && strength < requiredStrength)
            {
                reason = $"You can't carry {item.GetDisplayName()}: it requires Strength {requiredStrength}.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public static bool CanThrow(Entity actor, Entity item, out string reason)
        {
            if (!CanLift(actor, item, out reason))
                return false;

            if (!IsThrowable(item))
            {
                reason = $"{item.GetDisplayName()} cannot be thrown.";
                return false;
            }

            int requiredStrength = GetThrowStrengthRequirement(item);
            int strength = actor?.GetStatValue("Strength", 0) ?? 0;
            if (requiredStrength > 0 && strength < requiredStrength)
            {
                reason = $"You can't throw {item.GetDisplayName()}: it requires Strength {requiredStrength}.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public static int GetThrowRange(Entity actor, Entity item)
        {
            int requiredThrow = GetThrowStrengthRequirement(item);
            int strength = actor?.GetStatValue("Strength", 0) ?? 0;
            return Math.Max(1, 3 + (strength - requiredThrow));
        }

        public static string[] GetDefaultSlots(Entity item)
        {
            return GetDefaultSlots(GetGripType(item));
        }

        public static string[] GetDefaultSlots(GripType gripType)
        {
            return gripType == GripType.TwoHand
                ? new[] { "Hand", "Hand" }
                : new[] { "Hand" };
        }

        private static int CountSlots(string slots, string slotName)
        {
            if (string.IsNullOrWhiteSpace(slots))
                return 0;

            string[] parts = slots.Split(',');
            int count = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                if (string.Equals(parts[i].Trim(), slotName, StringComparison.OrdinalIgnoreCase))
                    count++;
            }

            return count;
        }
    }
}
