using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Spellcraft tree-root — universal magic mastery. Owning the tree
    /// grants <see cref="SPELL_DAMAGE_BONUS"/> bonus damage on every
    /// spell cast, regardless of element. Foundational skill for any
    /// magic build; pairs with element-specific trees (Pyromancy /
    /// Cryomancy / Galvanism / Corrosion / Photomancy / Empathy /
    /// Hydromancy) for stacking bonuses.
    ///
    /// <para><b>Classification (CLAUDE.md §4.2): CoO-Original
    /// Extension.</b> Qud doesn't have a parallel "general spell power"
    /// tree — magic in Qud is mutation-based without a unified
    /// caster-stat-line. CoO adds Spellcraft because the elemental
    /// trees (Pyromancy etc.) are also CoO-original and a generic
    /// universal-buff tree fills the same niche the weapon trees'
    /// Expertise skills fill (a baseline competence the player can
    /// invest in).</para>
    ///
    /// <para><b>Mechanic:</b> overrides
    /// <see cref="BaseSkillPart.OnGetSpellDamageModifier"/>. Returns
    /// <see cref="SPELL_DAMAGE_BONUS"/> regardless of the element
    /// attribute or defender state — universal flat bonus. Stacks
    /// additively with element-specific trees (e.g. Spellcraft +1 +
    /// Pyromancy_Conflagration's +25%-of-base on Burning targets).</para>
    ///
    /// <para>Hook fires from
    /// <see cref="MutationDamageHelpers.ApplySpellDamage"/> — the
    /// shared damage path that all migrated mutations use (FireBolt,
    /// IceLance, IceShard, ArcBolt, AcidSpray, Kindle as of WSP7.0).
    /// Pre-WSP7 mutations didn't tag damage with element/Spell
    /// attributes, so neither resistances NOR magic skills could fire.
    /// The MutationDamageHelpers migration fixed both.</para>
    /// </summary>
    public class SpellcraftSkill : BaseSkillPart
    {
        public override string Name => nameof(SpellcraftSkill);

        public const int SPELL_DAMAGE_BONUS = 1;

        public override int OnGetSpellDamageModifier(Entity attacker, Entity defender,
            string elementAttribute, int baseDamage)
        {
            // Universal — fires for any spell, any element, any target.
            // Negative-damage cases (where baseDamage <= 0) are filtered
            // by the helper before this hook is called, so we don't need
            // to guard here.
            return SPELL_DAMAGE_BONUS;
        }
    }
}
