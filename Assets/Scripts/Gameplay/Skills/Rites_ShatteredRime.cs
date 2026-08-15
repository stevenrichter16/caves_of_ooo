using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>Rite of the Shattered Rime — a cone of brittle cold.
    /// Port of <c>ShatteredRimeMutation</c>; consumed marks shatter
    /// armor. Taught by ShatteredRimeGrimoire, buyable in Rites.</summary>
    public class Rites_ShatteredRime : ConsumingRiteSkillBase
    {
        public override string Name => nameof(Rites_ShatteredRime);
        public override string CommandName => "CommandShatteredRime";
        public override string Element => "Cold";
        public override RiteShape Shape => RiteShape.Cone;
        public override int Range => 3;
        public override int Cooldown => 40;
        public override int Slots => 2;
        public override int BaseDamage => 6;

        protected override void ApplyPayoff(
            Entity target, ResonanceSystem.Result res, Zone zone)
        {
            if (res.Consumed.Count > 0)
                target.ApplyEffect(new ShatterArmorEffect(), ParentEntity, zone);
        }

        protected override string CastMessage(Entity caster, List<Entity> targets, int marks)
        {
            return caster.GetDisplayName() + "'s rime shatters across " + targets.Count
                + " target" + (targets.Count == 1 ? "" : "s")
                + (marks > 0 ? ", spending " + marks + " mark" + (marks == 1 ? "" : "s") + "!" : " — nothing was brittle.");
        }
    }
}
