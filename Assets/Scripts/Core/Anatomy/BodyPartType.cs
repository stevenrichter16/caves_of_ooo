namespace CavesOfOoo.Core.Anatomy
{
    /// <summary>
    /// Metadata definition for a body part type (e.g. "Head", "Hand", "Arm").
    /// Mirrors Qud's BodyPartType: carries behavior-defining metadata that gets
    /// stamped onto runtime BodyPart instances via ApplyTo().
    ///
    /// Key fields:
    /// - Appendage: can be severed/dismembered
    /// - Integral: cannot be severed even if appendage
    /// - Mortal: severing this part kills the creature (head/body)
    /// - Abstract: not a physical part (e.g. "Thrown Weapon" slot)
    /// - Extrinsic: added dynamically, not part of base anatomy
    /// </summary>
    public class BodyPartType
    {
        public string Type;
        public string Description;
        public string Name;
        public string DefaultBehavior;
        public string RequiresType;
        public string ImpliedBy;

        public int? Category;
        public int? Laterality;
        public int? ImpliedPer;
        public int? RequiresLaterality;
        public int? Mobility;
        public int? TargetWeight;

        public bool? Appendage;
        public bool? Integral;
        public bool? Abstract;
        public bool? Mortal;
        public bool? Extrinsic;
        public bool? Plural;
        public bool? Mass;
        public bool? Contact;
        public bool? IgnorePosition;

        public BodyPartType(string type, string description = null, string name = null)
        {
            Type = type;
            Description = description ?? type;
            Name = name ?? type.ToLowerInvariant();
        }

        /// <summary>
        /// Copy constructor for creating variants.
        /// </summary>
        public BodyPartType(BodyPartType baseType)
        {
            Type = baseType.Type;
            Description = baseType.Description;
            Name = baseType.Name;
            DefaultBehavior = baseType.DefaultBehavior;
            RequiresType = baseType.RequiresType;
            ImpliedBy = baseType.ImpliedBy;
            Category = baseType.Category;
            Laterality = baseType.Laterality;
            ImpliedPer = baseType.ImpliedPer;
            RequiresLaterality = baseType.RequiresLaterality;
            Mobility = baseType.Mobility;
            TargetWeight = baseType.TargetWeight;
            Appendage = baseType.Appendage;
            Integral = baseType.Integral;
            Abstract = baseType.Abstract;
            Mortal = baseType.Mortal;
            Extrinsic = baseType.Extrinsic;
            Plural = baseType.Plural;
            Mass = baseType.Mass;
            Contact = baseType.Contact;
            IgnorePosition = baseType.IgnorePosition;
        }

        /// <summary>
        /// Stamp this type's metadata onto a runtime BodyPart instance.
        /// Only sets fields where this type has a value (nullable pattern).
        /// Mirrors Qud's BodyPartType.ApplyTo().
        /// </summary>
        public void ApplyTo(BodyPart part)
        {
            part.Type = Type;
            part.Description = Description;
            part.Name = Name;
            part.DefaultBehaviorBlueprint = DefaultBehavior;

            if (RequiresType != null)
                part.RequiresType = RequiresType;
            if (Category.HasValue)
                part.Category = Category.Value;
            if (Laterality.HasValue)
                part.SetLaterality(Laterality.Value);
            if (RequiresLaterality.HasValue)
                part.RequiresLaterality = RequiresLaterality.Value;
            if (Mobility.HasValue)
                part.Mobility = Mobility.Value;
            if (Appendage.HasValue)
                part.Appendage = Appendage.Value;
            if (Integral.HasValue)
                part.Integral = Integral.Value;
            if (Abstract.HasValue)
                part.Abstract = Abstract.Value;
            if (Mortal.HasValue)
                part.Mortal = Mortal.Value;
            if (Extrinsic.HasValue)
                part.Extrinsic = Extrinsic.Value;
            if (Plural.HasValue)
                part.Plural = Plural.Value;
            if (Mass.HasValue)
                part.Mass = Mass.Value;
            if (Contact.HasValue)
                part.Contact = Contact.Value;
            if (IgnorePosition.HasValue)
                part.IgnorePosition = IgnorePosition.Value;
            if (TargetWeight.HasValue)
                part.TargetWeight = TargetWeight.Value;
        }
    }
}
