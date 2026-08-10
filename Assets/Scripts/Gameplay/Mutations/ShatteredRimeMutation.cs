using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Cone of splintering ice. Frozen flesh does not absorb a blow — it
    /// breaks.
    ///
    /// <para>The rite's whole argument is that a frozen target is a
    /// BRITTLE one. Consuming Frozen converts the freeze into physical
    /// shatter damage, and consuming Wet works too because water is one
    /// step from ice. Against an unprepared enemy it is a weak swing at
    /// the air; against a rank you spent two turns freezing it is the
    /// hardest hit in the game.</para>
    /// </summary>
    public class ShatteredRimeMutation : ConsumingRiteBase
    {
        public override string Name => "ShatteredRime";
        public override string MutationType => "Rite";
        public override string DisplayName => "Rite of the Shattered Rime";

        public override string Command => "CommandShatteredRime";
        public override string Element => "Cold";
        public override RiteShape Shape => RiteShape.Cone;
        public override int Range => 3;
        public override int Cooldown => 40;
        public override int Slots => 2;
        public override int BaseDamage => 6;

        /// <summary>
        /// Untyped on purpose. The rite breaks ice rather than making
        /// more of it, so tagging it Cold would let a cold-immune target
        /// absorb its own shattering (CombatSystem.cs:1181 — resistance
        /// 100 zeroes the hit). Physical damage is both the correct
        /// fiction and the only version that works on an ice creature.
        /// </summary>
        public override string DamageAttribute => "";

        protected override void ApplyPayoff(
            Entity target, ResonanceSystem.Result res, Zone zone)
        {
            // Every mark spent adds a shard. ShatterArmor rather than
            // more damage: the point is that the NEXT hit lands harder
            // too, so a shattered rank stays shattered.
            if (res.Consumed.Count > 0)
                target.ApplyEffect(new ShatterArmorEffect(), ParentEntity, zone);
        }

        protected override string CastMessage(Entity caster, int targets, int marks)
        {
            return caster.GetDisplayName() + "'s rime shatters across " + targets
                + " target" + (targets == 1 ? "" : "s")
                + (marks > 0 ? ", spending " + marks + " mark" + (marks == 1 ? "" : "s") + "!" : " — nothing was brittle.");
        }
    }
}
