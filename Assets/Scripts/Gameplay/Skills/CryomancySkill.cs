using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Cryomancy tree-root — cold mastery. Cold-element spells get a
    /// damage bonus when the target is already <see cref="WetEffect"/>
    /// OR <see cref="FrozenEffect"/>, rewarding the canonical "soak
    /// then chill" combo (any water-source mutation → IceLance/IceShard
    /// → big Cold spike).
    ///
    /// <para><b>Classification (CLAUDE.md §4.2): CoO-Original
    /// Extension.</b> Mirrors Qud's per-skill virtual-override pattern
    /// while filling a CoO-original niche (Qud has no parallel
    /// elemental-magic skill tree).</para>
    ///
    /// <para><b>Mechanic:</b> overrides
    /// <see cref="BaseSkillPart.OnGetSpellDamageModifier"/>. On every
    /// Cold-element spell, if defender has <see cref="WetEffect"/> OR
    /// <see cref="FrozenEffect"/>, this skill adds
    /// <c>baseDamage / <see cref="FREEZERBURN_DIVISOR"/></c> damage
    /// (floor 1). With divisor 4 = +25% Cold damage to Wet/Frozen
    /// targets. Stacks additively with <see cref="SpellcraftSkill"/>'s
    /// universal +1.</para>
    ///
    /// <para>The "Wet OR Frozen" gate is broader than Pyromancy's
    /// "Burning" because Cold magic naturally synergizes with both
    /// Hydromancy (Wet setup) and itself (Frozen lockdown). Mirrors
    /// the Qud pattern where Frostfire-style mages double-dip on
    /// soaked-then-chilled targets.</para>
    /// </summary>
    public class CryomancySkill : BaseSkillPart
    {
        public override string Name => nameof(CryomancySkill);

        /// <summary>
        /// Divisor on baseDamage for the freezerburn bonus. 4 = +25%.
        /// Tunable for balance.
        /// </summary>
        public const int FREEZERBURN_DIVISOR = 4;

        public override int OnGetSpellDamageModifier(Entity attacker, Entity defender,
            string elementAttribute, int baseDamage)
        {
            // Element gate: only Cold spells benefit. (DamageAttributeFlags
            // aliases — "Cold" and "Ice" and "Freeze" all hit the same
            // flag; we accept all three as element strings.)
            if (elementAttribute != "Cold" &&
                elementAttribute != "Ice" &&
                elementAttribute != "Freeze") return 0;

            // Defender gate: Wet OR Frozen.
            if (defender == null) return 0;
            var effects = defender.GetPart<StatusEffectsPart>();
            if (effects == null) return 0;
            bool wetOrFrozen = effects.HasEffect<WetEffect>() ||
                               effects.HasEffect<FrozenEffect>();
            if (!wetOrFrozen) return 0;

            int bonus = baseDamage / FREEZERBURN_DIVISOR;
            return System.Math.Max(1, bonus);
        }
    }
}
