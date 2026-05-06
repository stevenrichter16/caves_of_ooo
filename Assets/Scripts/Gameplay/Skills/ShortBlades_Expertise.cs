using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Piercing-class proficiency passive: +1 to-hit when wielding a
    /// Piercing-attribute weapon. Per Qud's <c>ShortBlades_Expertise.cs</c> —
    /// note Qud uses <c>HitBonus = 1</c> (not 2 like Cudgel/Axe).
    /// CoO matches the Qud value verbatim.
    /// </summary>
    public class ShortBlades_Expertise : BaseSkillPart
    {
        public override string Name => nameof(ShortBlades_Expertise);

        public const int HIT_BONUS = 1;

        public override int OnGetToHitModifier(Entity actor, MeleeWeaponPart weapon)
        {
            if (weapon == null || string.IsNullOrEmpty(weapon.Attributes)) return 0;
            if (!weapon.Attributes.Contains("Piercing")) return 0;
            return HIT_BONUS;
        }
    }
}
