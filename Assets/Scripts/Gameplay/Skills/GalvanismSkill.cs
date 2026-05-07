using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Galvanism tree-root — lightning mastery. Electric-element spells
    /// get a damage bonus when the target is already
    /// <see cref="WetEffect"/> OR <see cref="ElectrifiedEffect"/>,
    /// rewarding the canonical "soak then shock" combo (Wet target
    /// takes amplified electric damage — same gameplay logic as the
    /// existing <c>ArcBoltMutation</c>'s wet-amplification).
    ///
    /// <para><b>Classification (CLAUDE.md §4.2): CoO-Original
    /// Extension.</b></para>
    ///
    /// <para><b>Mechanic:</b> on every Electric-element spell, if
    /// defender has <see cref="WetEffect"/> OR
    /// <see cref="ElectrifiedEffect"/>, adds
    /// <c>baseDamage / <see cref="CONDUCTOR_DIVISOR"/></c> damage
    /// (floor 1). Mirrors Pyromancy/Cryomancy's pattern. Stacks
    /// additively with <see cref="SpellcraftSkill"/>.</para>
    ///
    /// <para>Note: ElectrifiedEffect chains to nearby conductors via
    /// <see cref="ElectrifiedEffect.OnTurnEnd"/> — so a Galvanism caster
    /// hitting an already-electrified mob can keep the chain going at
    /// elevated damage. Synergy with the lightning_plus_conductor
    /// material reaction is intentional.</para>
    /// </summary>
    public class GalvanismSkill : BaseSkillPart
    {
        public override string Name => nameof(GalvanismSkill);

        public const int CONDUCTOR_DIVISOR = 4;

        public override int OnGetSpellDamageModifier(Entity attacker, Entity defender,
            string elementAttribute, int baseDamage)
        {
            // Element gate: only Electric spells. (DamageAttributeFlags
            // aliases — "Electric", "Shock", "Lightning", "Electricity"
            // all map to the same flag; accept the common ones.)
            if (elementAttribute != "Electric" &&
                elementAttribute != "Lightning" &&
                elementAttribute != "Shock") return 0;

            // Defender gate: Wet OR Electrified.
            if (defender == null) return 0;
            var effects = defender.GetPart<StatusEffectsPart>();
            if (effects == null) return 0;
            bool wetOrElec = effects.HasEffect<WetEffect>() ||
                             effects.HasEffect<ElectrifiedEffect>();
            if (!wetOrElec) return 0;

            int bonus = baseDamage / CONDUCTOR_DIVISOR;
            return System.Math.Max(1, bonus);
        }
    }
}
