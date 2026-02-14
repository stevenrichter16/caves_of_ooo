namespace CavesOfOoo.Core.Anatomy
{
    /// <summary>
    /// Integer constants for body part material categories.
    /// Mirrors Qud's BodyPartCategory: determines what a body part is made of,
    /// affects damage types, electrical conductivity, and display.
    /// Category 1 (Animal) is the default for living creatures.
    /// Category 6 (Cybernetic) is used for implant-added body parts.
    /// </summary>
    public static class BodyPartCategory
    {
        public const int ANIMAL = 1;
        public const int ARTHROPOD = 2;
        public const int PLANT = 3;
        public const int FUNGAL = 4;
        public const int PROTOPLASMIC = 5;
        public const int CYBERNETIC = 6;
        public const int MECHANICAL = 7;
        public const int METAL = 8;
        public const int WOODEN = 9;
        public const int STONE = 10;
        public const int GLASS = 11;
        public const int LEATHER = 12;
        public const int BONE = 13;
        public const int CHITIN = 14;
        public const int PLASTIC = 15;
        public const int CLOTH = 16;
        public const int PSIONIC = 17;
        public const int EXTRADIMENSIONAL = 18;
        public const int MOLLUSK = 19;
        public const int JELLY = 20;
        public const int CRYSTAL = 21;

        /// <summary>
        /// Categories that represent living/animate body parts.
        /// </summary>
        public static readonly int[] ANIMATE = { ANIMAL, ARTHROPOD, PLANT, FUNGAL, PROTOPLASMIC, MOLLUSK, JELLY };

        /// <summary>
        /// Categories that represent machine body parts.
        /// </summary>
        public static readonly int[] MACHINE = { CYBERNETIC, MECHANICAL };

        public static string GetName(int category)
        {
            switch (category)
            {
                case ANIMAL: return "Animal";
                case ARTHROPOD: return "Arthropod";
                case PLANT: return "Plant";
                case FUNGAL: return "Fungal";
                case PROTOPLASMIC: return "Protoplasmic";
                case CYBERNETIC: return "Cybernetic";
                case MECHANICAL: return "Mechanical";
                case METAL: return "Metal";
                case WOODEN: return "Wooden";
                case STONE: return "Stone";
                case GLASS: return "Glass";
                case LEATHER: return "Leather";
                case BONE: return "Bone";
                case CHITIN: return "Chitin";
                case PLASTIC: return "Plastic";
                case CLOTH: return "Cloth";
                case PSIONIC: return "Psionic";
                case EXTRADIMENSIONAL: return "Extradimensional";
                case MOLLUSK: return "Mollusk";
                case JELLY: return "Jelly";
                case CRYSTAL: return "Crystal";
                default: return "Unknown";
            }
        }

        public static int GetCode(string name)
        {
            switch (name)
            {
                case "Animal": return ANIMAL;
                case "Arthropod": return ARTHROPOD;
                case "Plant": return PLANT;
                case "Fungal": return FUNGAL;
                case "Protoplasmic": return PROTOPLASMIC;
                case "Cybernetic": return CYBERNETIC;
                case "Mechanical": return MECHANICAL;
                case "Metal": return METAL;
                case "Wooden": return WOODEN;
                case "Stone": return STONE;
                case "Glass": return GLASS;
                case "Leather": return LEATHER;
                case "Bone": return BONE;
                case "Chitin": return CHITIN;
                case "Plastic": return PLASTIC;
                case "Cloth": return CLOTH;
                case "Psionic": return PSIONIC;
                case "Extradimensional": return EXTRADIMENSIONAL;
                case "Mollusk": return MOLLUSK;
                case "Jelly": return JELLY;
                case "Crystal": return CRYSTAL;
                default: return 0;
            }
        }

        public static bool IsLiveCategory(int category)
        {
            return category >= ANIMAL && category <= PROTOPLASMIC
                || category == MOLLUSK || category == JELLY;
        }
    }
}
