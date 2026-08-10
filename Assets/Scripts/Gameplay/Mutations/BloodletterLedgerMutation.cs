using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Every mark spent is written against the target and credited to
    /// the caster.
    ///
    /// <para>The only rite that gives something back. Consuming statuses
    /// opens deep <see cref="BleedingEffect"/> on the target AND heals
    /// the caster for each mark — a sustain tool for a build that has no
    /// other one. Cast cold it does almost nothing and heals nothing,
    /// which keeps it from being a free top-up between fights.</para>
    /// </summary>
    public class BloodletterLedgerMutation : ConsumingRiteBase
    {
        public override string Name => "BloodletterLedger";
        public override string MutationType => "Rite";
        public override string DisplayName => "Rite of the Bloodletter's Ledger";

        public override string Command => "CommandBloodletterLedger";

        /// <summary>
        /// Reads the wildcard table: this rite has no element loyalty and
        /// spends whatever it finds. That table pays 0.4 per mark against
        /// a matched table's 0.5-0.75, so building around an element
        /// strictly beats stacking at random. (It shipped at 0.5, which
        /// TIED three matched entries — Electric/Frozen, Heat/Frozen,
        /// Acid/Wet — and made this comment false for them. Caught by a
        /// cold-eye audit.)
        /// </summary>
        public override string Element => "Any";
        public override RiteShape Shape => RiteShape.SingleTarget;
        public override int Range => 5;
        public override int Cooldown => 40;
        public override int Slots => 2;
        public override int BaseDamage => 4;
        public override string DamageAttribute => "";

        protected override void ApplyPayoff(
            Entity target, ResonanceSystem.Result res, Zone zone)
        {
            if (res.Consumed.Count == 0) return;

            // VERIFICATION SWEEP CORRECTION: BleedingEffect has no
            // duration — it is indefinite and recovery is a Toughness
            // save that gets 1 easier per turn. So marks scale the two
            // knobs it DOES have: a bigger die and a save you are
            // unlikely to make early. (Same trap as the OnHit
            // Magnitude-vs-DurationTurns bug — see
            // OnHitEffectFactory.cs.)
            target.ApplyEffect(
                new BleedingEffect(
                    saveTarget: 14 + 4 * res.Consumed.Count,
                    damageDice: res.Consumed.Count >= 2 ? "1d6" : "1d4"),
                ParentEntity, zone);
        }

        /// <summary>
        /// The credit side of the ledger — heals the CASTER, per mark
        /// spent.
        ///
        /// <para>This lives in <see cref="OnCastResolved"/> rather than
        /// <see cref="ApplyPayoff"/> because the base skips ApplyPayoff
        /// when the target dies. It shipped in ApplyPayoff and so paid
        /// nothing whenever the rite's own damage finished the target —
        /// which meant the better your setup, the more likely the
        /// sustain silently vanished, exactly inverting the design. The
        /// marks and the ink are spent either way.</para>
        /// </summary>
        protected override void OnCastResolved(int totalMarks, Zone zone)
        {
            if (totalMarks <= 0) return;

            var hp = ParentEntity?.GetStat("Hitpoints");
            if (hp == null) return;

            int heal = 4 * totalMarks;
            hp.BaseValue = Math.Min(hp.Max, hp.BaseValue + heal);
            MessageLog.Add(ParentEntity.GetDisplayName()
                + " balances the ledger and closes " + heal + " of their own wounds.");
        }

        protected override string CastMessage(Entity caster, int targets, int marks)
        {
            return marks > 0
                ? caster.GetDisplayName() + " writes " + marks + " mark"
                  + (marks == 1 ? "" : "s") + " into the bloodletter's ledger!"
                : caster.GetDisplayName() + " opens the ledger. The page stays blank.";
        }
    }
}
