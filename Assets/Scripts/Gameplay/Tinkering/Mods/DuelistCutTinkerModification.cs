namespace CavesOfOoo.Core
{
    /// <summary>
    /// Armor mod: AV -1 and Agility +2 while equipped.
    /// </summary>
    public sealed class DuelistCutTinkerModification : ITinkerModification
    {
        private const string ModTag = "ModDuelistCut";
        private const string BonusStat = "Agility";
        private const int BonusAmount = 2;

        public string Id => "mod_duelist_cut";

        public string DisplayName => "Duelist Cut";

        public bool CanApply(Entity item, out string reason)
        {
            if (!ArmorTinkerModificationUtility.ValidateArmorTarget(item, ModTag, DisplayName, out reason))
                return false;

            if (item.GetPart<EquippablePart>() == null)
            {
                reason = "Duelist Cut requires wearable armor.";
                return false;
            }

            return true;
        }

        public bool Apply(Entity item, out string reason)
        {
            reason = string.Empty;
            if (!CanApply(item, out reason))
                return false;

            var armor = item.GetPart<ArmorPart>();
            armor.AV -= 1;

            ArmorTinkerModificationUtility.AddOrUpdateEquipBonus(item, BonusStat, BonusAmount);
            ArmorTinkerModificationUtility.ApplyEquippedStatBonusDelta(item, BonusStat, BonusAmount);
            ArmorTinkerModificationUtility.AddDisplayPrefix(item, "duelist");
            item.SetTag(ModTag, string.Empty);
            item.ModIntProperty("ModificationCount", 1);
            return true;
        }
    }
}
