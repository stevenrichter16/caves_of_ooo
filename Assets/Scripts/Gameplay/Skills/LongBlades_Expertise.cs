using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// LongBlades-class proficiency passive: +2 to-hit when wielding a
    /// LongBlades-attribute weapon (LongSword / Greatsword / ShortSword
    /// / Claymore / Sporeblade / FlamingSword / IceSword / etc.).
    ///
    /// <para>Self-contained per WSP3.3 — overrides
    /// <see cref="BaseSkillPart.OnGetToHitModifier"/>. CombatSystem.PerformSingleAttack
    /// sums modifiers from all owned skills via SkillEventDispatcher
    /// during the hit-roll calculation; the +2 contribution feeds
    /// directly into <c>totalHit</c>.</para>
    ///
    /// <para><b>Classification: CoO-original (Extension).</b> Qud has
    /// Cudgel_Expertise / Axe_Expertise / ShortBlades_Expertise but no
    /// LongBlades_Expertise — that genre slot is unfilled in the source.
    /// CoO adds one to maintain the cross-tree +to-hit symmetry the
    /// player perceives (every weapon-class root tree has a parallel
    /// Expertise). Magnitude chosen at +2 to match Cudgel/Axe; Qud's
    /// ShortBlades uses +1, but LongBlades-class weapons in CoO are
    /// closer in feel to Cudgel/Axe (two-handed greatswords, claymores)
    /// than to ShortBlades (daggers, stilettos), so +2 reads correctly.
    /// Per CLAUDE.md §4.2 — Extension is the right tag here, not "Match"
    /// or "verbatim port".</para>
    /// </summary>
    public class LongBlades_Expertise : BaseSkillPart
    {
        public override string Name => nameof(LongBlades_Expertise);

        public const int HIT_BONUS = 2;

        public override int OnGetToHitModifier(Entity actor, MeleeWeaponPart weapon)
        {
            if (weapon == null || string.IsNullOrEmpty(weapon.Attributes)) return 0;
            if (!weapon.Attributes.Contains("LongBlades")) return 0;
            return HIT_BONUS;
        }
    }
}
