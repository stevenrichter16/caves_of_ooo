using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Corrosion blooms outward, eating armour off everything nearby.
    ///
    /// <para>A support rite rather than a killing one: consuming Acidic
    /// strips armour across a whole radius, so the party's ordinary
    /// attacks start landing properly. Consuming Wet works too — water
    /// carries acid outward, which is why soaking a clump before this
    /// makes it hit far more of them.</para>
    /// </summary>
    public class VerdigrisBloomMutation : ConsumingRiteBase
    {
        public override string Name => "VerdigrisBloom";
        public override string MutationType => "Rite";
        public override string DisplayName => "Rite of the Verdigris Bloom";

        public override string Command => "CommandVerdigrisBloom";
        public override string Element => "Acid";
        public override RiteShape Shape => RiteShape.Radius;
        public override int Range => 2;
        public override int Cooldown => 40;
        public override int Slots => 2;
        public override int BaseDamage => 5;
        public override string DamageAttribute => "Acid";

        protected override void ApplyPayoff(
            Entity target, ResonanceSystem.Result res, Zone zone)
        {
            if (res.Consumed.Count == 0) return;

            target.ApplyEffect(new ShatterArmorEffect(), ParentEntity, zone);

            // Yes, this re-applies the status the rite may have just
            // eaten. That IS the bloom: §7.4 wanted Wet to SPREAD acid
            // across the radius, and re-seeding every target it caught
            // is how the spread happens. It re-seeds WEAKER (0.6) than a
            // full application and the rite is on a 40-turn cooldown, so
            // it cannot be pumped for free fuel.
            target.ApplyEffect(new AcidicEffect(corrosion: 0.6f), ParentEntity, zone);
        }

        protected override string CastMessage(Entity caster, int targets, int marks)
        {
            return caster.GetDisplayName() + "'s verdigris blooms across " + targets
                + " target" + (targets == 1 ? "" : "s")
                + (marks > 0 ? ", eating " + marks + " ward" + (marks == 1 ? "" : "s") + " away!" : ".");
        }
    }
}
