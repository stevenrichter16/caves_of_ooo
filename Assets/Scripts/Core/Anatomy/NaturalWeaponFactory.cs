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
                    return CreateWeapon("fist", "1d2", 0, "&y");
                case "DefaultHoof":
                    return CreateWeapon("hoof", "1d3", 0, "&w");
                case "DefaultBite":
                    return CreateWeapon("bite", "1d3+1", 0, "&r");
                case "DefaultClaw":
                    return CreateWeapon("claw", "1d4", 0, "&r");
                case "DefaultTendril":
                    return CreateWeapon("tendril", "1d3", 0, "&g");
                default:
                    return CreateWeapon(blueprintName, "1d2", 0, "&y");
            }
        }

        private static Entity CreateWeapon(string name, string damage, int penBonus, string color)
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
                Stat = "Strength"
            });
            return entity;
        }
    }
}
