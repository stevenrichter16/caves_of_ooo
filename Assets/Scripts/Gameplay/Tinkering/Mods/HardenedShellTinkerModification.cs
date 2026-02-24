namespace CavesOfOoo.Core
{
    /// <summary>
    /// Armor mod: AV +2 and +10 speed penalty while equipped.
    /// </summary>
    public sealed class HardenedShellTinkerModification : ITinkerModification
    {
        private const string ModTag = "ModHardenedShell";
        private const int SpeedPenaltyDelta = 10;

        public string Id => "mod_hardened_shell";

        public string DisplayName => "Hardened Shell";

        public bool CanApply(Entity item, out string reason)
        {
            return ArmorTinkerModificationUtility.ValidateArmorTarget(item, ModTag, DisplayName, out reason);
        }

        public bool Apply(Entity item, out string reason)
        {
            reason = string.Empty;
            if (!CanApply(item, out reason))
                return false;

            var armor = item.GetPart<ArmorPart>();
            armor.AV += 2;
            armor.SpeedPenalty += SpeedPenaltyDelta;

            ArmorTinkerModificationUtility.ApplyEquippedSpeedPenaltyDelta(item, SpeedPenaltyDelta);
            ArmorTinkerModificationUtility.AddDisplayPrefix(item, "hardened");
            item.SetTag(ModTag, string.Empty);
            item.ModIntProperty("ModificationCount", 1);
            return true;
        }
    }
}
