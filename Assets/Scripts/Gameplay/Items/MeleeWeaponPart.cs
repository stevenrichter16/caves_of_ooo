namespace CavesOfOoo.Core
{
    /// <summary>
    /// Part representing a melee weapon's combat properties.
    /// Faithful to Qud's MeleeWeapon part.
    /// Can be attached to creatures (natural weapons) or items (equipped weapons).
    /// </summary>
    public class MeleeWeaponPart : Part
    {
        public override string Name => "MeleeWeapon";

        /// <summary>
        /// Dice expression for base damage (e.g. "1d4", "2d3+1").
        /// </summary>
        public string BaseDamage = "1d2";

        /// <summary>
        /// Bonus to penetration rolls.
        /// </summary>
        public int PenBonus = 0;

        /// <summary>
        /// Bonus to hit rolls.
        /// </summary>
        public int HitBonus = 0;

        /// <summary>
        /// Maximum Strength bonus that can apply to penetration.
        /// -1 means no cap.
        /// </summary>
        public int MaxStrengthBonus = -1;

        /// <summary>
        /// Stat used for damage bonus (usually "Strength").
        /// </summary>
        public string Stat = "Strength";

        /// <summary>
        /// Space-separated list of damage attributes this weapon contributes
        /// to a successful hit's <see cref="Damage"/> object — e.g.,
        /// <c>"Cutting LongBlades"</c> on a longsword, or <c>"Bludgeoning Cudgel"</c>
        /// on a club. Mirrors Qud's <c>MeleeWeapon.Attributes</c>.
        ///
        /// In addition to these, the combat path always tags damage with
        /// "Melee" and the weapon's <see cref="Stat"/> name.
        /// </summary>
        public string Attributes = "";
    }
}