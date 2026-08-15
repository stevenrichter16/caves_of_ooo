using System;
using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>Fulmination — cracks a bolt into one creature and writes
    /// charge into the ground under it, where it goes looking for
    /// somewhere to run. Port of <c>FulminationMutation</c>, converged
    /// onto the spine; the tile write runs whether or not the victim
    /// survived (<see cref="OnTargetResolved"/>). Taught by
    /// FulminationGrimoire, buyable in Rites.</summary>
    public class Rites_Fulmination : ConsumingRiteSkillBase
    {
        public const int WRITTEN_CHARGE = 2;

        public override string Name => nameof(Rites_Fulmination);
        public override string CommandName => "CommandFulmination";
        public override string Element => "Electric";
        public override RiteShape Shape => RiteShape.SingleTarget;
        public override int Range => 5;
        public override int Cooldown => 25;
        public override int Slots => 1;
        public override int BaseDamage => 3;
        public override string[] DamageAttributes => new[] { "Electric", "Lightning" };

        /// <summary>The mutation's exact double-rounded hybrid:
        /// flat base + round(base × (mult − 1)). NOT the same as
        /// round(base × mult) under banker's rounding — preserved
        /// verbatim.</summary>
        protected override int ComputeDamage(ResonanceSystem.Result res)
            => BaseDamage + (int)Math.Round(BaseDamage * (res.Multiplier - 1f));

        protected override void OnTargetResolved(
            Entity target, ResonanceSystem.Result res, Zone zone)
        {
            var tpos = zone.GetEntityPosition(target);
            if (tpos.x >= 0)
            {
                ZoneTileStateSystem.AddCharge(
                    zone, tpos.x, tpos.y, WRITTEN_CHARGE, ParentEntity, Name);
                ZoneTileStateSystem.ResolveAfterAbility(zone, ParentEntity);
            }
        }

        protected override void ApplyPayoff(
            Entity target, ResonanceSystem.Result res, Zone zone)
        {
            // No survivor-gated rider — the written charge is the payoff.
        }

        protected override string CastMessage(Entity caster, List<Entity> targets, int marks)
        {
            return caster.GetDisplayName() + "'s fulmination cracks into "
                + targets[0].GetDisplayName() + ", and the charge goes looking for somewhere to run.";
        }
    }
}
