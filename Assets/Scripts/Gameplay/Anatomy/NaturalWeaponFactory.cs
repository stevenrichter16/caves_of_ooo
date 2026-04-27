namespace CavesOfOoo.Core.Anatomy
{
    /// <summary>
    /// Creates natural weapon entities for body part DefaultBehavior slots.
    /// These are simple entities with a MeleeWeaponPart — not full blueprint-derived objects.
    /// Follows the same in-code entity creation pattern as ExtraArmPrototypeMutation.
    /// </summary>
    public static class NaturalWeaponFactory
    {
        public static Entity Create(string blueprintName)
        {
            switch (blueprintName)
            {
                case "DefaultFist":
                    return CreateWeapon("fist", "1d2", 0, "&y", "Bludgeoning Unarmed");
                case "DefaultHoof":
                    return CreateWeapon("hoof", "1d3", 0, "&w", "Bludgeoning Animal");
                case "DefaultBite":
                    return CreateWeapon("bite", "1d3+1", 0, "&r", "Piercing Cutting Animal");
                case "DefaultClaw":
                    return CreateWeapon("claw", "1d4", 0, "&r", "Cutting Animal");
                case "DefaultTendril":
                    return CreateWeapon("tendril", "1d3", 0, "&g", "Bludgeoning Animal");
                case "SnapjawClaw":
                    return CreateWeapon("claw", "1d4", 1, "&w", "Cutting Animal");
                case "SnapjawHunterClaw":
                    return CreateWeapon("claw", "1d6", 2, "&w", "Cutting Animal");
                default:
                    return CreateWeapon(blueprintName, "1d2", 0, "&y", "");
            }
        }

        private static Entity CreateWeapon(string name, string damage, int penBonus, string color, string attributes)
        {
            var entity = new Entity();
            entity.BlueprintName = "NaturalWeapon_" + name;
            entity.SetTag("Natural");
            entity.AddPart(new RenderPart
            {
                DisplayName = name,
                RenderString = ")",
                ColorString = color
            });
            entity.AddPart(new MeleeWeaponPart
            {
                BaseDamage = damage,
                PenBonus = penBonus,
                MaxStrengthBonus = -1,
                Stat = "Strength",
                Attributes = attributes
            });
            return entity;
        }
    }
}
