using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Spellcraft power: Empower — additional flat universal +damage
    /// on every spell. Stacks additively with
    /// <see cref="SpellcraftSkill"/>'s +1 root bonus, so a player
    /// who buys both gets +3 damage on every spell regardless of
    /// element.
    ///
    /// <para><b>Classification (CLAUDE.md §4.2): CoO-Original
    /// Extension.</b> First Spellcraft power skill — proves the
    /// power-skill pattern works for magic trees. Future powers
    /// (Spellcraft_Channel for cooldown reduction, Spellcraft_Resonance
    /// for repeat-cast bonus, etc.) will follow this shape.</para>
    ///
    /// <para><b>Mechanic:</b> universal flat <see cref="EMPOWER_BONUS"/>
    /// on every spell cast. No gates — fires for any element, any
    /// defender state. Identical shape to <see cref="SpellcraftSkill"/>'s
    /// override but with a separate constant so the two contributions
    /// can be tuned independently.</para>
    /// </summary>
    public class Spellcraft_Empower : BaseSkillPart
    {
        public override string Name => nameof(Spellcraft_Empower);

        public const int EMPOWER_BONUS = 2;

        public override int OnGetSpellDamageModifier(Entity attacker, Entity defender,
            string elementAttribute, int baseDamage)
        {
            return EMPOWER_BONUS;
        }
    }
}
