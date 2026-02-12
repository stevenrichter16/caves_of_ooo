using System.Collections.Generic;

namespace CavesOfOoo.Core.Anatomy
{
    /// <summary>
    /// Factory for creating standard body part type definitions and anatomy templates.
    /// Mirrors Qud's Anatomies/Bodies XML data + Anatomy.ApplyTo logic, but
    /// defined in code rather than XML for now.
    ///
    /// Standard humanoid anatomy tree (matches Qud):
    ///   Body (root, mortal)
    ///     ├── Head (mortal, appendage)
    ///     │     └── Face
    ///     ├── Back
    ///     ├── Arm (left, appendage)
    ///     │     ├── Hand (left, appendage, supports "left hand")
    ///     │     └── Hands (left, abstract, requires Hand + left laterality)
    ///     ├── Arm (right, appendage)
    ///     │     ├── Hand (right, appendage, supports "right hand")
    ///     │     └── Hands (right, abstract, requires Hand + right laterality)
    ///     ├── Feet (appendage, mobility)
    ///     ├── Thrown Weapon (abstract, ignore position)
    ///     └── Floating Nearby (abstract, ignore position)
    /// </summary>
    public static class AnatomyFactory
    {
        // --- Standard body part types ---
        private static Dictionary<string, BodyPartType> _types;

        /// <summary>
        /// Get the registered body part types. Initializes on first call.
        /// </summary>
        public static Dictionary<string, BodyPartType> GetTypes()
        {
            if (_types == null)
                InitTypes();
            return _types;
        }

        /// <summary>
        /// Get a specific body part type by name.
        /// </summary>
        public static BodyPartType GetType(string type)
        {
            var types = GetTypes();
            types.TryGetValue(type, out BodyPartType result);
            return result;
        }

        /// <summary>
        /// Register standard body part type definitions.
        /// Mirrors the semantic data from Qud's Bodies.xml + BodyPartType defaults.
        /// </summary>
        private static void InitTypes()
        {
            _types = new Dictionary<string, BodyPartType>();

            // Body: the root/torso. Mortal (losing it kills you). Not an appendage.
            Register(new BodyPartType("Body")
            {
                Mortal = true,
                Contact = true,
            });

            // Head: mortal appendage. Losing head = decapitation death.
            Register(new BodyPartType("Head")
            {
                Mortal = true,
                Appendage = true,
                Contact = true,
            });

            // Face: non-appendage slot on head (masks, visors).
            Register(new BodyPartType("Face")
            {
                Contact = true,
            });

            // Back: non-appendage slot (cloaks, backpacks).
            Register(new BodyPartType("Back")
            {
                Contact = true,
            });

            // Arm: appendage that supports hand.
            Register(new BodyPartType("Arm")
            {
                Appendage = true,
                Contact = true,
            });

            // Hand: appendage for holding weapons/shields. Depends on arm.
            Register(new BodyPartType("Hand")
            {
                Appendage = true,
                Contact = true,
            });

            // Hands: abstract two-hand slot for two-handed weapons. Requires a Hand.
            Register(new BodyPartType("Hands")
            {
                Abstract = true,
                Plural = true,
                RequiresType = "Hand",
            });

            // Feet: appendage with mobility value. Losing feet = movement penalty.
            Register(new BodyPartType("Feet")
            {
                Appendage = true,
                Contact = true,
                Plural = true,
                Mobility = 2,
            });

            // Tail: optional appendage for tail-bearing creatures.
            Register(new BodyPartType("Tail")
            {
                Appendage = true,
                Contact = true,
            });

            // Thrown Weapon: abstract slot for throwable items. Always present.
            Register(new BodyPartType("Thrown Weapon", "Thrown Weapon", "thrown weapon")
            {
                Abstract = true,
                IgnorePosition = true,
            });

            // Floating Nearby: abstract slot for floating items/effects.
            Register(new BodyPartType("Floating Nearby", "Floating Nearby", "floating nearby")
            {
                Abstract = true,
                IgnorePosition = true,
            });

            // Missile Weapon: for ranged weapon hardpoints (cybernetics).
            Register(new BodyPartType("Missile Weapon", "Missile Weapon", "missile weapon hardpoint")
            {
                Integral = true,
                Contact = false,
            });

            // Roots: for plant creatures.
            Register(new BodyPartType("Roots")
            {
                Plural = true,
                Mobility = 2,
                Contact = true,
            });

            // Tendril: for tentacled creatures.
            Register(new BodyPartType("Tendril")
            {
                Appendage = true,
                Contact = true,
            });
        }

        private static void Register(BodyPartType type)
        {
            _types[type.Type] = type;
        }

        // --- Anatomy template builders ---

        /// <summary>
        /// Create a standard humanoid anatomy (human, True Kin, most humanoids).
        /// Matches Qud's Humanoid anatomy from Bodies.xml.
        /// </summary>
        public static BodyPart CreateHumanoid(int category = BodyPartCategory.ANIMAL)
        {
            var body = CreatePart("Body");
            body.Category = category;

            // Head → Face
            var head = CreatePart("Head");
            body.AddPart(head);
            head.AddPart(CreatePart("Face"));

            // Back
            body.AddPart(CreatePart("Back"));

            // Left Arm → Left Hand → Left Hands
            var leftArm = CreatePart("Arm", Laterality.LEFT);
            body.AddPart(leftArm);
            var leftHand = CreatePart("Hand", Laterality.LEFT);
            leftHand.SupportsDependent = "left hand";
            leftHand.DefaultPrimary = true;
            leftArm.AddPart(leftHand);
            var leftHands = CreatePart("Hands", Laterality.LEFT);
            leftHands.DependsOn = "left hand";
            leftArm.AddPart(leftHands);

            // Right Arm → Right Hand → Right Hands
            var rightArm = CreatePart("Arm", Laterality.RIGHT);
            body.AddPart(rightArm);
            var rightHand = CreatePart("Hand", Laterality.RIGHT);
            rightHand.SupportsDependent = "right hand";
            rightArm.AddPart(rightHand);
            var rightHands = CreatePart("Hands", Laterality.RIGHT);
            rightHands.DependsOn = "right hand";
            rightArm.AddPart(rightHands);

            // Feet (mobility = 2)
            body.AddPart(CreatePart("Feet"));

            // Thrown Weapon (abstract, always present)
            body.AddPart(CreatePart("Thrown Weapon"));

            // Floating Nearby (abstract, always present)
            body.AddPart(CreatePart("Floating Nearby"));

            // Mark all as native
            MarkNative(body);

            return body;
        }

        /// <summary>
        /// Create a quadruped anatomy (dogs, cats, horses, etc.).
        /// Four legs with fore/hind laterality, head, back, tail.
        /// </summary>
        public static BodyPart CreateQuadruped(int category = BodyPartCategory.ANIMAL)
        {
            var body = CreatePart("Body");
            body.Category = category;

            // Head → Face
            var head = CreatePart("Head");
            body.AddPart(head);
            head.AddPart(CreatePart("Face"));

            // Back
            body.AddPart(CreatePart("Back"));

            // Four feet with laterality (fore-left, fore-right, hind-left, hind-right)
            body.AddPart(CreatePart("Feet", Laterality.FORE | Laterality.LEFT));
            body.AddPart(CreatePart("Feet", Laterality.FORE | Laterality.RIGHT));
            body.AddPart(CreatePart("Feet", Laterality.HIND | Laterality.LEFT));
            body.AddPart(CreatePart("Feet", Laterality.HIND | Laterality.RIGHT));

            // Tail
            body.AddPart(CreatePart("Tail"));

            // Thrown Weapon + Floating Nearby
            body.AddPart(CreatePart("Thrown Weapon"));
            body.AddPart(CreatePart("Floating Nearby"));

            MarkNative(body);
            return body;
        }

        /// <summary>
        /// Create a simple anatomy with just a body (for objects, simple creatures).
        /// </summary>
        public static BodyPart CreateSimple(int category = BodyPartCategory.ANIMAL)
        {
            var body = CreatePart("Body");
            body.Category = category;
            MarkNative(body);
            return body;
        }

        /// <summary>
        /// Create an insectoid anatomy (6 legs, mandibles, antennae).
        /// </summary>
        public static BodyPart CreateInsectoid(int category = BodyPartCategory.ARTHROPOD)
        {
            var body = CreatePart("Body");
            body.Category = category;

            var head = CreatePart("Head");
            body.AddPart(head);
            head.AddPart(CreatePart("Face"));

            // Back (for wings or shells)
            body.AddPart(CreatePart("Back"));

            // Six legs (fore pair, mid pair, hind pair)
            body.AddPart(CreatePart("Feet", Laterality.FORE | Laterality.LEFT));
            body.AddPart(CreatePart("Feet", Laterality.FORE | Laterality.RIGHT));
            body.AddPart(CreatePart("Feet", Laterality.MID | Laterality.LEFT));
            body.AddPart(CreatePart("Feet", Laterality.MID | Laterality.RIGHT));
            body.AddPart(CreatePart("Feet", Laterality.HIND | Laterality.LEFT));
            body.AddPart(CreatePart("Feet", Laterality.HIND | Laterality.RIGHT));

            body.AddPart(CreatePart("Thrown Weapon"));
            body.AddPart(CreatePart("Floating Nearby"));

            MarkNative(body);
            return body;
        }

        // --- Helpers ---

        /// <summary>
        /// Create a BodyPart from a registered type with optional laterality override.
        /// </summary>
        public static BodyPart CreatePart(string type, int laterality = 0)
        {
            var part = new BodyPart();
            var partType = GetType(type);
            if (partType != null)
            {
                partType.ApplyTo(part);
            }
            else
            {
                // Fallback: set basic fields from type string
                part.Type = type;
                part.Name = type.ToLowerInvariant();
                part.Description = type;
            }

            if (laterality != 0)
                part.SetLaterality(laterality);

            return part;
        }

        /// <summary>
        /// Mark all parts in the tree as Native.
        /// </summary>
        private static void MarkNative(BodyPart part)
        {
            part.Native = true;
            if (part.Parts != null)
            {
                for (int i = 0; i < part.Parts.Count; i++)
                    MarkNative(part.Parts[i]);
            }
        }
    }
}
