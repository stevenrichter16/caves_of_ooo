namespace CavesOfOoo.Core
{
    /// <summary>
    /// Armor mod: DV +2, AV -1.
    /// </summary>
    public sealed class FlexweaveTinkerModification : ITinkerModification
    {
        private const string ModTag = "ModFlexweave";

        public string Id => "mod_flexweave";

        public string DisplayName => "Flexweave";

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
            armor.DV += 2;
            armor.AV -= 1;

            ArmorTinkerModificationUtility.AddDisplayPrefix(item, "flexwoven");
            item.SetTag(ModTag, string.Empty);
            item.ModIntProperty("ModificationCount", 1);
            return true;
        }
    }
}
