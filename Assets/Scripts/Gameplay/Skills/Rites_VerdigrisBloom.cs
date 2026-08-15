using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>Rite of the Verdigris Bloom — an acid bloom that eats
    /// wards. Port of <c>VerdigrisBloomMutation</c>. Taught by
    /// VerdigrisBloomGrimoire, buyable in Rites.</summary>
    public class Rites_VerdigrisBloom : ConsumingRiteSkillBase
    {
        public override string Name => nameof(Rites_VerdigrisBloom);
        public override string CommandName => "CommandVerdigrisBloom";
        public override string Element => "Acid";
        public override RiteShape Shape => RiteShape.Radius;
        public override int Range => 2;
        public override int Cooldown => 40;
        public override int Slots => 2;
        public override int BaseDamage => 5;
        public override string[] DamageAttributes => new[] { "Acid" };

        protected override void ApplyPayoff(
            Entity target, ResonanceSystem.Result res, Zone zone)
        {
            if (res.Consumed.Count == 0) return;
            target.ApplyEffect(new ShatterArmorEffect(), ParentEntity, zone);
            target.ApplyEffect(new AcidicEffect(corrosion: 0.6f), ParentEntity, zone);
        }

        protected override string CastMessage(Entity caster, List<Entity> targets, int marks)
        {
            return caster.GetDisplayName() + "'s verdigris blooms across " + targets.Count
                + " target" + (targets.Count == 1 ? "" : "s")
                + (marks > 0 ? ", eating " + marks + " ward" + (marks == 1 ? "" : "s") + " away!" : ".");
        }
    }
}
