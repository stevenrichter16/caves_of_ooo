namespace CavesOfOoo.Core
{
    /// <summary>
    /// Mental mutation — fires a calming bolt down a direction. The first
    /// creature struck gains a <see cref="NoFightGoal"/> for
    /// <c>BaseDuration + Level*10</c> turns, becoming temporarily pacifist.
    /// Wires M2's NoFight-via-player-ability trigger; the sibling dialogue
    /// trigger lives in <see cref="ConversationActions"/>.
    ///
    /// Parity notes:
    /// - First non-damaging Mental mutation in the player catalog.
    ///   <see cref="DamageDice"/> is <c>"0"</c>, which DiceRoller returns
    ///   as 0 via its invalid-pattern fallthrough — no damage is applied
    ///   or logged (see DirectionalProjectileMutationBase.Cast, gated on
    ///   <c>damage &gt; 0</c>).
    /// - The FX theme reuses <see cref="AsciiFxTheme.Arcane"/>. A dedicated
    ///   "Mental" theme would be a nicer long-term fit; deferred.
    /// - Idempotent: if the target already carries a NoFightGoal, the cast
    ///   is a no-op and does NOT extend the existing duration. Documented
    ///   trade-off so chained casts can't stack the pacification window.
    ///
    /// Side-effect warning: <see cref="NoFightGoal"/> suppresses
    /// <see cref="AIBoredEvent"/> while active, which means a pacified
    /// target's own <c>AISelfPreservationPart</c> won't fire to retreat
    /// even at low HP. See NoFightGoal's xml-doc for the broader gotcha.
    /// </summary>
    public class CalmMutation : DirectionalProjectileMutationBase
    {
        public const string COMMAND = "CommandCalm";

        // DirectionalProjectileMutationBase abstracts (7).
        protected override string CommandName   => COMMAND;
        protected override AsciiFxTheme FxTheme => AsciiFxTheme.Arcane;
        protected override int CooldownTurns    => 20;
        protected override int AbilityRange     => 6;
        protected override string DamageDice    => "0";
        protected override string AbilityClass  => "Mental Mutations";
        protected override string ImpactVerb    => "calms";

        // Part + BaseMutation abstracts (3).
        public override string Name         => "Calm";
        public override string MutationType => "Mental";
        public override string DisplayName  => "Calm";

        /// <summary>
        /// Base pacification duration in turns. Effective duration scales
        /// linearly with mutation level: <c>Duration = BaseDuration + Level*10</c>.
        /// </summary>
        public int BaseDuration = 40;

        protected override void ApplyOnHitEffect(Entity target, Zone zone, System.Random rng)
        {
            if (target == null) return;
            var brain = target.GetPart<BrainPart>();
            if (brain == null) return;

            // Idempotent: don't stack or replace an existing pacification.
            // The cast still consumed its cooldown (handled by the base
            // class) so emit a message so the player isn't confused by an
            // apparently-silent cast.
            if (brain.HasGoal<NoFightGoal>())
            {
                MessageLog.Add(target.GetDisplayName() + " is already at peace.");
                return;
            }

            int duration = BaseDuration + (Level * 10);
            brain.PushGoal(new NoFightGoal(duration, wander: false));
            MessageLog.Add(target.GetDisplayName() + " becomes peaceful.");
        }
    }
}
