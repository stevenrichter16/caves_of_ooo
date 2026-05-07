using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Pyromancy power: Cinder — extra Heat damage to Charred targets.
    /// CharredEffect is the post-Burning residue; a target who has been
    /// burned heavily AND survived the burn becomes Charred (more
    /// fragile to subsequent fire). Cinder rewards the player who
    /// stays on the same target with sustained fire — by the time the
    /// target is Charred (post-Burning expiry), Cinder doubles down.
    ///
    /// <para><b>Classification (CLAUDE.md §4.2): CoO-Original
    /// Extension.</b> Mirrors a Qud-style "second-stage damage type
    /// rewarder" (PyromancySkill rewards Burning, Cinder rewards
    /// Charred — the post-Burning state).</para>
    ///
    /// <para><b>Mechanic:</b> on every Heat-element spell, if defender
    /// has <see cref="CharredEffect"/>, adds
    /// <c>baseDamage / <see cref="CINDER_DIVISOR"/></c> damage (floor 1).
    /// With divisor 3 = ~33% bonus on Charred. Stacks ADDITIVELY with
    /// <see cref="PyromancySkill"/>'s +25%-on-Burning bonus, since
    /// Burning + Charred are different states that can both be present
    /// (e.g., a target who got Burned, took Charred residue, then got
    /// Burned again).</para>
    /// </summary>
    public class Pyromancy_Cinder : BaseSkillPart
    {
        public override string Name => nameof(Pyromancy_Cinder);

        /// <summary>
        /// Divisor on baseDamage for the Cinder bonus on Charred
        /// targets. 3 = ~33%. Pyromancy's root is +25% on Burning;
        /// Cinder is +33% on Charred — slightly bigger because Charred
        /// is harder to set up (requires surviving a Burning).
        /// </summary>
        public const int CINDER_DIVISOR = 3;

        public override int OnGetSpellDamageModifier(Entity attacker, Entity defender,
            string elementAttribute, int baseDamage)
        {
            // Element gate: Heat / Fire only.
            if (elementAttribute != "Heat" && elementAttribute != "Fire") return 0;

            // Defender gate: must be Charred.
            if (defender == null) return 0;
            var effects = defender.GetPart<StatusEffectsPart>();
            if (effects == null || !effects.HasEffect<CharredEffect>()) return 0;

            int bonus = baseDamage / CINDER_DIVISOR;
            return System.Math.Max(1, bonus);
        }
    }
}
