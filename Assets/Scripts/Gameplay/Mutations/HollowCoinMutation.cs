using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Pays out whatever is owed, in one lump.
    ///
    /// <para>The universal cash-out. Three slots and no element loyalty:
    /// it spends whatever it can reach and converts the lot into a flat
    /// untyped burst, which nothing resists. Deliberately LESS efficient
    /// per mark than a matched rite — the Storm Anvil pays better for
    /// two lightning-friendly statuses — so matching still matters. This
    /// is the rite for the player who stacked without a plan.</para>
    ///
    /// <para>Untyped damage on purpose: a rite that ignores resistances
    /// is the correct answer to an enemy you have no answer for.</para>
    /// </summary>
    public class HollowCoinMutation : ConsumingRiteBase
    {
        public override string Name => "HollowCoin";
        public override string MutationType => "Rite";
        public override string DisplayName => "Rite of the Hollow Coin";

        public override string Command => "CommandHollowCoin";

        /// <summary>
        /// Reads the wildcard table: this rite has no element loyalty and
        /// spends whatever it finds. That table pays 0.5 per mark against
        /// a matched table's 0.75, so building around an element still
        /// beats stacking at random.
        /// </summary>
        public override string Element => "Any";
        public override RiteShape Shape => RiteShape.SingleTarget;
        public override int Range => 4;
        public override int Cooldown => 50;
        public override int Slots => 3;
        public override int BaseDamage => 3;
        public override string DamageAttribute => "";

        protected override void ApplyPayoff(
            Entity target, ResonanceSystem.Result res, Zone zone)
        {
            // Nothing extra. The burst IS the payoff, and its size came
            // entirely from the multiplier.
            if (res.Consumed.Count >= 3)
                target.ApplyEffect(new BrokenEffect(), ParentEntity, zone);
        }

        protected override string CastMessage(Entity caster, int targets, int marks)
        {
            return marks > 0
                ? caster.GetDisplayName() + " turns the hollow coin over — " + marks
                  + " mark" + (marks == 1 ? "" : "s") + " called in at once!"
                : caster.GetDisplayName() + " turns the hollow coin over. Nothing is owed.";
        }
    }
}
