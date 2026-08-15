using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>Rite of the Scalding Veil — boils the water off your own
    /// skin into a veil that scalds and confuses attackers. Self-shaped:
    /// spends from the CASTER, and refuses for free unless the caster is
    /// Wet (checked by Preview before ink). Port of
    /// <c>ScaldingVeilMutation</c>, converged onto the spine. Taught by
    /// ScaldingVeilGrimoire, buyable in Rites.</summary>
    public class Rites_ScaldingVeil : ConsumingRiteSkillBase
    {
        public const int VEIL_DURATION = 8;
        public const int VEIL_SCALD = 3;
        public const int VEIL_CONFUSE = 2;

        public override string Name => nameof(Rites_ScaldingVeil);
        public override string CommandName => "CommandScaldingVeil";
        public override string Element => "Heat";
        public override RiteShape Shape => RiteShape.Self;
        public override int Range => 0;
        public override int Cooldown => 30;
        public override int Slots => 1;

        /// <summary>The veil deals no direct damage on cast.</summary>
        public override int BaseDamage => 0;

        protected override bool ValidateBeforeInk(
            Entity caster, List<Entity> targets, out string reason, out string message)
        {
            var preview = ResonanceSystem.Preview(caster, Element, Slots);
            if (!preview.Consumed.Contains("Wet"))
            {
                reason = "caster_not_wet";
                message = caster.GetDisplayName() + " is dry — there is nothing to boil.";
                return false;
            }
            reason = null; message = null;
            return true;
        }

        protected override void ApplyPayoff(
            Entity target, ResonanceSystem.Result res, Zone zone)
        {
            // target == caster (Self shape).
            target.ApplyEffect(
                new ScaldingVeilEffect(VEIL_DURATION, VEIL_SCALD, VEIL_CONFUSE),
                ParentEntity, zone);
        }

        protected override string CastMessage(Entity caster, List<Entity> targets, int marks)
            => null; // the veil effect announces itself
    }
}
