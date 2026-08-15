using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Calm — a pacifying pulse. Skill-side port of <c>CalmMutation</c>,
    /// one of the two former starting mutations; granted by the starting
    /// kit and SP-buyable in Spellcraft.
    ///
    /// <para><b>Level-scaling frozen at level 1 (user decision, plan
    /// §5):</b> the pacify duration is a flat 50 turns (was
    /// BaseDuration 40 + Level×10).</para>
    ///
    /// <para>Everything else verbatim: range 6, cooldown 20, no damage
    /// (dice "0"), pushes <see cref="NoFightGoal"/> (no wandering) on the
    /// struck creature's brain. Re-calming an already-peaceful creature
    /// reports "is already at peace" and does not stack — but the bolt
    /// flew, so the cast is still consumed, exactly like the
    /// mutation.</para>
    /// </summary>
    public class Spellcraft_Calm : ProjectileSpellSkillBase
    {
        public override string Name => nameof(Spellcraft_Calm);

        public const int COOLDOWN = 20;
        public const int RANGE = 6;

        /// <summary>Flat pacify duration — the mutation's level-1 value
        /// (BaseDuration 40 + 1×10), frozen by the level-scaling decision.</summary>
        public const int CALM_DURATION = 50;

        protected override string CommandName => "CommandCalm";
        protected override AsciiFxTheme FxTheme => AsciiFxTheme.Arcane;
        protected override int CooldownTurns => COOLDOWN;
        protected override int AbilityRange => RANGE;
        protected override string DamageDice => "0";
        protected override string ImpactVerb => "calms";
        protected override string AbilityClass => "Spellcraft";

        protected override void ApplyOnHitEffect(Entity target, Zone zone, System.Random rng)
        {
            if (target == null) return;

            var brain = target.GetPart<BrainPart>();
            if (brain == null) return;

            if (brain.HasGoal<NoFightGoal>())
            {
                MessageLog.Add(target.GetDisplayName() + " is already at peace.");
                return;
            }

            brain.PushGoal(new NoFightGoal(CALM_DURATION, wander: false));
            MessageLog.Add(target.GetDisplayName() + " becomes peaceful.");
        }
    }
}
