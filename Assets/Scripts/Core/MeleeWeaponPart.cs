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
    }
}