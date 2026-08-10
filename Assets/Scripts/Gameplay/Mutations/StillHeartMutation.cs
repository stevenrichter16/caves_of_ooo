using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Slows a frozen heart until it stops noticing the world.
    ///
    /// <para>The only rite whose payoff is REMOVAL rather than damage.
    /// Consuming Frozen puts the target into
    /// <see cref="HibernatingEffect"/> — a long sleep that breaks on
    /// damage. It does not kill an elite; it takes one out of the fight
    /// while you deal with everything else, and hands it back the moment
    /// anyone touches it.</para>
    ///
    /// <para>Deliberately low damage. Hitting it awake would defeat the
    /// entire purpose.</para>
    /// </summary>
    public class StillHeartMutation : ConsumingRiteBase
    {
        public override string Name => "StillHeart";
        public override string MutationType => "Rite";
        public override string DisplayName => "Rite of the Still Heart";

        public override string Command => "CommandStillHeart";
        public override string Element => "Cold";
        public override RiteShape Shape => RiteShape.SingleTarget;
        public override int Range => 5;
        public override int Cooldown => 45;
        /// <summary>
        /// Two, so the duration below can actually scale. At one slot
        /// the "8 turns per mark" rule would be a constant 8 and the
        /// comment on <see cref="ApplyPayoff"/> would be fiction.
        /// Soaked AND frozen sleeps twice as long as frozen alone.
        /// </summary>
        public override int Slots => 2;
        public override int BaseDamage => 2;
        public override string DamageAttribute => "Cold";

        protected override void ApplyPayoff(
            Entity target, ResonanceSystem.Result res, Zone zone)
        {
            // Duration scales with what was spent — a deeply frozen
            // target sleeps far longer.
            if (res.Consumed.Count > 0)
                target.ApplyEffect(
                    new HibernatingEffect(duration: 8 * res.Consumed.Count),
                    ParentEntity, zone);
        }

        protected override string CastMessage(Entity caster, int targets, int marks)
        {
            return marks > 0
                ? caster.GetDisplayName() + " stills a heart — it will not wake until struck."
                : caster.GetDisplayName() + "'s rite finds a heart already beating too fast to still.";
        }
    }
}
