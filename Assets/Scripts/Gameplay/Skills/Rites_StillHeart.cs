using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>Rite of the Still Heart — stills one heart into a sleep
    /// that lasts 8 turns per mark consumed. Port of
    /// <c>StillHeartMutation</c>. Taught by StillHeartGrimoire, buyable
    /// in Rites.</summary>
    public class Rites_StillHeart : ConsumingRiteSkillBase
    {
        public override string Name => nameof(Rites_StillHeart);
        public override string CommandName => "CommandStillHeart";
        public override string Element => "Cold";
        public override RiteShape Shape => RiteShape.SingleTarget;
        public override int Range => 5;
        public override int Cooldown => 45;
        public override int Slots => 2;
        public override int BaseDamage => 2;
        public override string[] DamageAttributes => new[] { "Cold" };

        protected override void ApplyPayoff(
            Entity target, ResonanceSystem.Result res, Zone zone)
        {
            if (res.Consumed.Count > 0)
                target.ApplyEffect(
                    new AsleepByGasEffect(duration: 8 * res.Consumed.Count),
                    ParentEntity, zone);
        }

        protected override string CastMessage(Entity caster, List<Entity> targets, int marks)
        {
            return marks > 0
                ? caster.GetDisplayName() + " stills a heart — it will not wake until struck."
                : caster.GetDisplayName() + "'s rite finds a heart already beating too fast to still.";
        }
    }
}
