using System;
using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>Rite of the Bloodletter's Ledger — bleeds the marked and
    /// heals the caster 4 HP per mark, paid even over a corpse. Port of
    /// <c>BloodletterLedgerMutation</c>. Taught by
    /// BloodletterLedgerGrimoire, buyable in Rites.</summary>
    public class Rites_BloodletterLedger : ConsumingRiteSkillBase
    {
        public override string Name => nameof(Rites_BloodletterLedger);
        public override string CommandName => "CommandBloodletterLedger";
        public override string Element => "Any";
        public override RiteShape Shape => RiteShape.SingleTarget;
        public override int Range => 5;
        public override int Cooldown => 40;
        public override int Slots => 2;
        public override int BaseDamage => 4;

        protected override void ApplyPayoff(
            Entity target, ResonanceSystem.Result res, Zone zone)
        {
            if (res.Consumed.Count == 0) return;
            target.ApplyEffect(
                new BleedingEffect(
                    saveTarget: 14 + 4 * res.Consumed.Count,
                    damageDice: res.Consumed.Count >= 2 ? "1d6" : "1d4"),
                ParentEntity, zone);
        }

        protected override void OnCastResolved(int totalMarks, Zone zone)
        {
            if (totalMarks <= 0) return;
            var hp = ParentEntity?.GetStat("Hitpoints");
            if (hp == null) return;
            int heal = 4 * totalMarks;
            int hpBefore = hp.BaseValue;
            hp.BaseValue = Math.Min(hp.Max, hp.BaseValue + heal);
            SpellFxCapture.RecordOutcome(zone, ParentEntity, "healing", "Hitpoints", hp.BaseValue - hpBefore);
            MessageLog.Add(ParentEntity.GetDisplayName()
                + " balances the ledger and closes " + heal + " of their own wounds.");
        }

        protected override string CastMessage(Entity caster, List<Entity> targets, int marks)
        {
            return marks > 0
                ? caster.GetDisplayName() + " writes " + marks + " mark"
                  + (marks == 1 ? "" : "s") + " into the bloodletter's ledger!"
                : caster.GetDisplayName() + " opens the ledger. The page stays blank.";
        }
    }
}
