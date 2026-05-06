using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Cudgel-class proficiency passive: +2 to-hit when wielding a
    /// Cudgel-attribute weapon (Mace / Warhammer / Cudgel / OldWorldPipe).
    ///
    /// <para>Self-contained per WSP3.3 — overrides
    /// <see cref="BaseSkillPart.OnGetToHitModifier"/>. CombatSystem.PerformSingleAttack
    /// sums modifiers from all owned skills via SkillEventDispatcher
    /// during the hit-roll calculation; the +2 contribution feeds
    /// directly into <c>totalHit</c>.</para>
    ///
    /// <para>Per Qud's <c>Cudgel_Expertise.cs</c> — verbatim mechanic
    /// (<c>HitBonus = 2</c>, gated on <c>E.Skill == "Cudgel"</c>).</para>
    /// </summary>
    public class Cudgel_Expertise : BaseSkillPart
    {
        public override string Name => nameof(Cudgel_Expertise);

        public const int HIT_BONUS = 2;

        public override int OnGetToHitModifier(Entity actor, MeleeWeaponPart weapon)
        {
            if (weapon == null || string.IsNullOrEmpty(weapon.Attributes)) return 0;
            if (!weapon.Attributes.Contains("Cudgel")) return 0;
            return HIT_BONUS;
        }
    }
}
