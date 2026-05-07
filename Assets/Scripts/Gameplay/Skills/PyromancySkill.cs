using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Pyromancy tree-root — fire mastery. Heat-element spells get a
    /// damage bonus when the target is already Burning, rewarding
    /// sustained fire focus and pairing naturally with FireBolt /
    /// FlamingHands / Kindle / KindleFlame / EmberVein / Conflagration.
    ///
    /// <para><b>Classification (CLAUDE.md §4.2): CoO-Original
    /// Extension.</b> Qud doesn't have an elemental-magic skill tree
    /// — Pyromancy is a CoO addition built on top of CoO's existing
    /// elemental damage system (DamageAttributeFlags.Heat) and effect
    /// system (BurningEffect). The implementation mirrors Qud's
    /// per-skill virtual-override pattern and uses the shared
    /// <see cref="BaseSkillPart.OnGetSpellDamageModifier"/> hook
    /// added in WSP7.0.</para>
    ///
    /// <para><b>Mechanic:</b> on every Heat-element spell cast (Fire,
    /// any Heat-tagged spell damage), if the defender already has
    /// <see cref="BurningEffect"/>, this skill adds
    /// <c>baseDamage / <see cref="CONFLAGRATION_DIVISOR"/></c>
    /// damage. With divisor 4, that's a +25% damage bonus on
    /// already-burning targets. Reward for chaining fire spells
    /// (or pairing with the FlamingSword melee-weapon's BurningEffect
    /// proc) into a follow-up cast.</para>
    ///
    /// <para>Element gate: returns 0 if the spell isn't Heat-tagged
    /// (so cold/electric/acid/light spells get no bonus from this
    /// skill — that's what the other elemental trees are for).
    /// Defender gate: returns 0 if the defender doesn't have
    /// BurningEffect — the skill rewards pre-existing fire status,
    /// not just any Heat damage.</para>
    /// </summary>
    public class PyromancySkill : BaseSkillPart
    {
        public override string Name => nameof(PyromancySkill);

        /// <summary>
        /// Divisor applied to baseDamage when the target is Burning.
        /// 4 = +25% Heat damage to Burning targets. Tunable for balance.
        /// </summary>
        public const int CONFLAGRATION_DIVISOR = 4;

        public override int OnGetSpellDamageModifier(Entity attacker, Entity defender,
            string elementAttribute, int baseDamage)
        {
            // Element gate: only Heat / Fire spells benefit.
            // (DamageAttributeFlags.Heat aliases include both "Heat"
            // and "Fire" — we accept either as the elementAttribute
            // string passed by mutations.)
            if (elementAttribute != "Heat" && elementAttribute != "Fire") return 0;

            // Defender gate: must already be Burning.
            if (defender == null) return 0;
            var effects = defender.GetPart<StatusEffectsPart>();
            if (effects == null || !effects.HasEffect<BurningEffect>()) return 0;

            // baseDamage / 4 = ~25% bonus. Floor at 1 so non-zero spell
            // damage always gets at least +1 from this skill.
            int bonus = baseDamage / CONFLAGRATION_DIVISOR;
            return System.Math.Max(1, bonus);
        }
    }
}
