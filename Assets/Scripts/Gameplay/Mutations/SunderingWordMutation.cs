using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// A word that leaves everything nearby less than it was.
    ///
    /// <para>Debuff-first: it barely hurts, but every mark it spends
    /// leaves the target Broken and Weakened across the whole radius.
    /// Cast into a clump you have been priming, it turns a dangerous
    /// pack into a manageable one — which is a different kind of power
    /// from killing them, and the reason it is worth a charge.</para>
    /// </summary>
    public class SunderingWordMutation : ConsumingRiteBase
    {
        public override string Name => "SunderingWord";
        public override string MutationType => "Rite";
        public override string DisplayName => "Rite of the Sundering Word";

        public override string Command => "CommandSunderingWord";

        /// <summary>
        /// Reads the wildcard table: this rite has no element loyalty and
        /// spends whatever it finds. That table pays 0.5 per mark against
        /// a matched table's 0.75, so building around an element still
        /// beats stacking at random.
        /// </summary>
        public override string Element => "Any";
        public override RiteShape Shape => RiteShape.Radius;
        public override int Range => 2;
        public override int Cooldown => 45;
        public override int Slots => 2;
        public override int BaseDamage => 4;
        public override string DamageAttribute => "";

        protected override void ApplyPayoff(
            Entity target, ResonanceSystem.Result res, Zone zone)
        {
            if (res.Consumed.Count > 0)
            {
                target.ApplyEffect(new BrokenEffect(), ParentEntity, zone);
                target.ApplyEffect(new WeakenedEffect(), ParentEntity, zone);
            }
        }

        protected override string CastMessage(Entity caster, int targets, int marks)
        {
            return caster.GetDisplayName() + " speaks the sundering word over " + targets
                + " target" + (targets == 1 ? "" : "s")
                + (marks > 0 ? " — " + marks + " unmade!" : ", and nothing answers.");
        }
    }
}
