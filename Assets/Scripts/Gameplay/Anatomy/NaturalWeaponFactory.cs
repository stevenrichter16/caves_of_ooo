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

                // ALPHA-READINESS combat-stakes SM3: natural weapons for
                // the bruisers stuck on the 1d2 default fist (see
                // Docs/ALPHA-READINESS.md item 1). Venomous species carry
                // Poisoned on-hit specs via the shipped spec format.
                case "BatBite":
                    return CreateWeapon("bite", "1d2", 0, "&y", "Piercing Animal");
                case "SlimePseudopod":
                    return CreateWeapon("pseudopod", "1d3", 0, "&g", "Bludgeoning Animal");
                case "CaveBearClaw":
                    return CreateWeapon("claw", "2d4", 1, "&w", "Cutting Animal");
                case "ScorpionSting":
                    return CreateWeapon("sting", "1d3", 1, "&g", "Piercing Animal", "Poisoned,50,1d4,6,0");
                case "WurmBite":
                    return CreateWeapon("maw", "2d6+1", 1, "&y", "Piercing Cutting Animal");
                case "SpiderBite":
                    return CreateWeapon("fangs", "1d4", 0, "&g", "Piercing Animal", "Poisoned,35,1d4,6,0");
                case "ViperBite":
                    return CreateWeapon("fangs", "1d3", 1, "&G", "Piercing Animal", "Poisoned,75,1d6,8,0");
                case "ApeFist":
                    return CreateWeapon("fist", "1d6+1", 0, "&w", "Bludgeoning Animal");
                case "ScavengerClaw":
                    return CreateWeapon("claw", "1d4", 1, "&w", "Cutting Animal");
                case "BoneBlade":
                    return CreateWeapon("bone blade", "1d6+1", 1, "&Y", "Cutting");
                case "GolemFist":
                    return CreateWeapon("stone fist", "2d6", 2, "&w", "Bludgeoning");
                case "ProwlerClaw":
                    return CreateWeapon("claw", "2d4", 1, "&y", "Cutting Animal");
                case "StalkerClaw":
                    return CreateWeapon("claw", "2d4", 1, "&g", "Cutting Animal");
                case "GuardianFist":
                    return CreateWeapon("ancient fist", "2d6+2", 2, "&W", "Bludgeoning");
                case "ChoirLash":
                    return CreateWeapon("tendril", "2d4", 0, "&m", "Bludgeoning Animal");
                case "WightTouch":
                    return CreateWeapon("frozen touch", "1d6", 1, "&C", "Bludgeoning");
                case "HuskTouch":
                    return CreateWeapon("charred touch", "1d6", 1, "&r", "Bludgeoning");
                // Conversions of the four DEAD entity-level MeleeWeapon
                // parts the body-part-aware path never read (verifier
                // finding — these four also punched 1d2):
                case "GlowmawBite":
                    return CreateWeapon("maw", "2d4", 1, "&r", "Piercing Cutting Animal");
                case "TrollFist":
                    return CreateWeapon("fist", "2d6", 1, "&w", "Bludgeoning");
                case "MimicBite":
                    return CreateWeapon("maw", "1d8", 1, "&y", "Piercing Cutting");
                case "BanditBlade":
                    return CreateWeapon("blade", "1d6", 1, "&w", "Cutting");

                // BIOME-OVERHAUL A5: the last five 1d2-fist hostiles get
                // real natural weapons. HuskFist arcs on touch (same
                // Electrified spec ThunderHammer ships); SporeTouch puffs
                // fungal spores through the EmitGasOnHitRaw channel
                // (gas id from Content/Data/GasDefinitions).
                case "HuskFist":
                    return CreateWeapon("brass fist", "1d6", 1, "&y", "Bludgeoning", "Electrified,20,,3,1.0");
                case "GlassSting":
                    return CreateWeapon("glass stinger", "1d4", 2, "&W", "Piercing Animal");
                case "SporeTouch":
                    return CreateWeapon("spored touch", "1d4", 0, "&g", "Bludgeoning Animal", "", "fungal-spores,25");
                case "CultistKnife":
                    return CreateWeapon("ritual knife", "1d4", 1, "&M", "Cutting");

                // BIOME-OVERHAUL C-G: the nine biome-pass creatures
                // (Docs/BIOME-OVERHAUL.md §4.1). Registered together so
                // each phase's blueprint drop-in finds its case waiting.
                case "WarlordCleaver":
                    return CreateWeapon("cleaver", "2d5", 2, "&M", "Cutting Axe");
                case "MosshulkSlam":
                    return CreateWeapon("mossy fist", "2d5", 2, "&g", "Bludgeoning Animal");
                case "LurkerMaw":
                    return CreateWeapon("maw", "2d6", 2, "&y", "Piercing Cutting Animal");
                case "BrittleFangs":
                    return CreateWeapon("glass fangs", "1d6", 1, "&W", "Piercing Animal", "Bleeding,25,1d2,10,0");
                case "RotlingClaw":
                    return CreateWeapon("rotted claw", "1d3", 0, "&g", "Cutting Animal", "Poisoned,10,1d2,4,0");
                case "StranglerLash":
                    return CreateWeapon("strangling vine", "2d4", 2, "&G", "Bludgeoning Animal");
                case "SentinelHalberd":
                    return CreateWeapon("vault halberd", "2d6", 3, "&W", "Cutting Piercing");
                case "StalkerTalon":
                    return CreateWeapon("pale talon", "2d5", 2, "&C", "Cutting Animal");
                case "ObsidianFist":
                    return CreateWeapon("obsidian fist", "3d6", 3, "&m", "Bludgeoning");

                default:
                    return CreateWeapon(blueprintName, "1d2", 0, "&y", "");
            }
        }

        private static Entity CreateWeapon(string name, string damage, int penBonus, string color, string attributes, string onHitEffectsRaw = "", string emitGasOnHitRaw = "")
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
                Attributes = attributes,
                OnHitEffectsRaw = onHitEffectsRaw,
                EmitGasOnHitRaw = emitGasOnHitRaw
            });
            return entity;
        }
    }
}
