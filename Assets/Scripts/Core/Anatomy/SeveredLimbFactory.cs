namespace CavesOfOoo.Core.Anatomy
{
    /// <summary>
    /// Creates severed limb item entities when body parts are dismembered.
    /// The resulting entity can be placed in the zone, picked up, and inspected.
    /// </summary>
    public static class SeveredLimbFactory
    {
        /// <summary>
        /// Create a severed limb entity from a dismembered body part.
        /// </summary>
        public static Entity Create(BodyPart part)
        {
            string displayName = "severed " + part.GetDisplayName();
            string glyph = GetGlyph(part);
            string color = GetColor(part.Category);
            int weight = GetWeight(part);

            var entity = new Entity();
            entity.BlueprintName = "SeveredLimb";
            entity.SetTag("Item");
            entity.SetTag("SeveredLimb");

            entity.AddPart(new RenderPart
            {
                DisplayName = displayName,
                RenderString = glyph,
                ColorString = color,
                RenderLayer = 5
            });

            entity.AddPart(new PhysicsPart
            {
                Takeable = true,
                Weight = weight
            });

            entity.AddPart(new SeveredLimbPart
            {
                PartType = part.Type,
                PartDisplayName = part.GetDisplayName(),
                Category = part.Category,
                WasMortal = part.Mortal
            });

            return entity;
        }

        private static string GetGlyph(BodyPart part)
        {
            switch (part.Type)
            {
                case "Head": return "%";
                case "Arm": return "~";
                case "Hand": return ")";
                case "Feet": return "&";
                case "Tail": return "~";
                default: return "~";
            }
        }

        private static string GetColor(int category)
        {
            switch (category)
            {
                case BodyPartCategory.ANIMAL: return "&r";
                case BodyPartCategory.PLANT: return "&g";
                case BodyPartCategory.FUNGAL: return "&m";
                case BodyPartCategory.ARTHROPOD: return "&w";
                case BodyPartCategory.CYBERNETIC: return "&c";
                case BodyPartCategory.MECHANICAL: return "&C";
                default: return "&r";
            }
        }

        private static int GetWeight(BodyPart part)
        {
            switch (part.Type)
            {
                case "Head": return 8;
                case "Arm": return 6;
                case "Hand": return 2;
                case "Feet": return 4;
                case "Tail": return 3;
                default: return 4;
            }
        }
    }
}
