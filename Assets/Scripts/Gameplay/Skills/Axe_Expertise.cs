using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Axe-class proficiency passive: +2 to-hit when wielding an
    /// Axe-attribute weapon (Battleaxe / Hatchet).
    /// Per Qud's <c>Axe_Expertise.cs</c> — verbatim
    /// (<c>HitBonus = 2</c>, gated on <c>E.Skill == "Axe"</c>).
    /// </summary>
    public class Axe_Expertise : BaseSkillPart
    {
        public override string Name => nameof(Axe_Expertise);

        public const int HIT_BONUS = 2;

        public override int OnGetToHitModifier(Entity actor, MeleeWeaponPart weapon)
        {
            if (weapon == null || string.IsNullOrEmpty(weapon.Attributes)) return 0;
            if (!weapon.Attributes.Contains("Axe")) return 0;
            return HIT_BONUS;
        }
    }
}
