using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Corrosion tree-root — acid mastery. Acid-element spells get a
    /// damage bonus when the target is already <see cref="AcidicEffect"/>,
    /// rewarding sustained acid focus (open with AcidSpray, follow up
    /// with another acid spell or melee on the now-acidic target).
    ///
    /// <para><b>Classification (CLAUDE.md §4.2): CoO-Original
    /// Extension.</b></para>
    ///
    /// <para><b>Mechanic:</b> on every Acid-element spell, if defender
    /// has <see cref="AcidicEffect"/>, adds
    /// <c>baseDamage / <see cref="DISSOLVE_DIVISOR"/></c> damage
    /// (floor 1). Synergizes with the acid_plus_organic material
    /// reaction (acidic organic targets take accelerated burn damage)
    /// and with future Corrosion power skills (Dissolve, AcidPool,
    /// Catalyst — see Docs/MAGIC-SKILLS-DESIGN.md §Class 5).</para>
    /// </summary>
    public class CorrosionSkill : BaseSkillPart
    {
        public override string Name => nameof(CorrosionSkill);

        public const int DISSOLVE_DIVISOR = 4;

        public override int OnGetSpellDamageModifier(Entity attacker, Entity defender,
            string elementAttribute, int baseDamage)
        {
            // Element gate: only Acid spells.
            if (elementAttribute != "Acid") return 0;

            // Defender gate: must already be Acidic-stacked.
            if (defender == null) return 0;
            var effects = defender.GetPart<StatusEffectsPart>();
            if (effects == null || !effects.HasEffect<AcidicEffect>()) return 0;

            int bonus = baseDamage / DISSOLVE_DIVISOR;
            return System.Math.Max(1, bonus);
        }
    }
}
