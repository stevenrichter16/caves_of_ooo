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

                // FUN-P0 M1.b — per-creature natural weapons. Dice are budgeted
                // for the two-swing humanoid anatomy (primary + off-hand at -2,
                // off-hand always swings): effective damage ≈ 2x the die shown.
                case "BatBite":
                    return CreateWeapon("bite", "1d2", 0, "&K", "Piercing Animal");
                case "SlimePseudopod":
                    return CreateWeapon("pseudopod", "1d3", 0, "&g", "Bludgeoning Animal");
                case "BearClaw":
                    return CreateWeapon("claw", "1d4", 1, "&w", "Cutting Animal");
                case "ScorpionSting":
                    return CreateWeapon("stinger", "1d3", 1, "&W", "Piercing Animal");
                case "WurmBite":
                    return CreateWeapon("maw", "1d6", 1, "&y", "Piercing Cutting Animal");
                case "SpiderFang":
                    return CreateWeapon("fang", "1d3", 0, "&K", "Piercing Animal");
                case "ViperFang":
                    return CreateWeapon("fang", "1d2", 0, "&G", "Piercing Animal");
                case "ApeFist":
                    return CreateWeapon("fist", "1d4", 0, "&w", "Bludgeoning Animal");
                case "ScavengerClaw":
                    return CreateWeapon("claw", "1d3", 0, "&y", "Cutting Animal");
                case "SentryBlade":
                    return CreateWeapon("bone blade", "1d4+1", 1, "&Y", "Cutting");
                case "GolemFist":
                    return CreateWeapon("stone fist", "1d6", 2, "&y", "Bludgeoning");
                case "BanditKnife":
                    return CreateWeapon("knife", "1d4", 0, "&w", "Cutting");
                case "AmbushKnife":
                    return CreateWeapon("knife", "1d4", 1, "&w", "Cutting");
                case "ProwlerClaw":
                    return CreateWeapon("claw", "1d4+1", 1, "&Y", "Cutting Animal");
                case "StalkerClaw":
                    return CreateWeapon("claw", "1d4+1", 1, "&G", "Cutting Animal");
                case "GuardianFist":
                    return CreateWeapon("ancient fist", "1d6", 2, "&C", "Bludgeoning");
                case "GlowmawBite":
                    return CreateWeapon("glowing maw", "1d4", 0, "&C", "Piercing Cutting Animal");
                case "TrollFist":
                    return CreateWeapon("fist", "1d6", 2, "&g", "Bludgeoning Animal");
                case "MimicBite":
                    return CreateWeapon("toothed lid", "1d4", 1, "&y", "Piercing Cutting Animal");
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
