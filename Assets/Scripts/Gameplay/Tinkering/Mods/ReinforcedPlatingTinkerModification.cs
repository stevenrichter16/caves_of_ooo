namespace CavesOfOoo.Core
{
    /// <summary>
    /// Armor mod: AV +1, DV -1.
    /// </summary>
    public sealed class ReinforcedPlatingTinkerModification : ITinkerModification
    {
        internal const int AVDelta = 1, DVDelta = -1;
        private const string ModTag = "ModReinforcedPlating";

        public string Id => "mod_reinforced_plating";

        public string DisplayName => "Reinforced Plating";

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
            armor.AV += AVDelta;
            armor.DV += DVDelta;

            ArmorTinkerModificationUtility.AddDisplayPrefix(item, "reinforced");
            item.SetTag(ModTag, string.Empty);
            item.ModIntProperty("ModificationCount", 1);
            return true;
        }
    }
}
