using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// D2.2 hook integration tests — every <c>CombatSystem.ApplyDamage</c>
    /// call that lands ≥1 HP of damage produces a <c>damage/DamageDealt</c>
    /// diag record.
    ///
    /// Plan ref: <c>Docs/D2-HOOKS-PLAN.md</c> §4 D2.2.
    ///
    /// The hook is broader than the existing <c>DamageDealt</c> event —
    /// that event only fires when <c>source != null</c>. The diag hook
    /// records environmental damage (traps, status DoT) too, so a
    /// turn-by-turn reconstruction picks up every HP-changing action.
    ///
    /// Five invariants:
    ///   1. ApplyDamage produces a DamageDealt record.
    ///   2. Payload includes hpAfter (post-damage HP).
    ///   3. lethal=true when damage drops HP ≤ 0.
    ///   4. Records fire even for source-less environmental damage —
    ///      counter-check vs. the existing DamageDealt event behavior.
    ///   5. Counter-check: zero-amount damage (vetoed/fully-resisted)
    ///      returns early and emits NO record.
    /// </summary>
    public class DiagDamageHookTests
    {
        [SetUp]
        public void SetUp() => Diag.ResetAll();

        // ====================================================================
        // 1. ApplyDamage produces a DamageDealt record
        // ====================================================================

        [Test]
        public void ApplyDamage_ProducesDamageDealtRecord()
        {
            var attacker = MakeMinimalCreature("attacker-001");
            var target = MakeMinimalCreature("target-001");

            CombatSystem.ApplyDamage(target, 5, attacker, zone: null);

            var records = Diag.Snapshot(2000);
            var dmg = records.FirstOrDefault(r =>
                r.Category == "damage" &&
                r.Kind == "DamageDealt" &&
                r.TargetId == "target-001");
            Assert.IsNotNull(dmg,
                $"Expected damage/DamageDealt record with TargetId=target-001. " +
                $"Got: [{string.Join(", ", records.Select(r => r.Category + "/" + r.Kind))}]");
            Assert.AreEqual("attacker-001", dmg.ActorId,
                "ActorId must match the source argument.");
            Assert.IsTrue(dmg.PayloadJson.Contains("\"amount\":5"),
                $"Payload must include the actual damage amount. Got: {dmg.PayloadJson}");
        }

        // ====================================================================
        // 2. Payload includes hpAfter
        // ====================================================================

        [Test]
        public void DamageRecord_PayloadIncludesHpAfter()
        {
            var target = MakeMinimalCreature("target-002", hp: 100);
            CombatSystem.ApplyDamage(target, 30, source: null, zone: null);

            var dmg = Diag.Snapshot(2000).First(r =>
                r.Category == "damage" && r.Kind == "DamageDealt");
            // 100 - 30 = 70
            Assert.IsTrue(dmg.PayloadJson.Contains("\"hpAfter\":70"),
                $"hpAfter must reflect post-damage HP (100 - 30 = 70). " +
                $"Got: {dmg.PayloadJson}");
        }

        // ====================================================================
        // 3. lethal=true on kill
        // ====================================================================

        [Test]
        public void DamageRecord_LethalFlagTrueOnKill()
        {
            var target = MakeMinimalCreature("target-003", hp: 5);
            CombatSystem.ApplyDamage(target, 10, source: null, zone: null);

            var dmg = Diag.Snapshot(2000).First(r =>
                r.Category == "damage" && r.Kind == "DamageDealt");
            Assert.IsTrue(dmg.PayloadJson.Contains("\"lethal\":true"),
                $"lethal=true when damage drops HP ≤ 0. Got: {dmg.PayloadJson}");
        }

        [Test]
        public void DamageRecord_LethalFlagFalseOnSurvivableHit()
        {
            var target = MakeMinimalCreature("target-004", hp: 100);
            CombatSystem.ApplyDamage(target, 5, source: null, zone: null);

            var dmg = Diag.Snapshot(2000).First(r =>
                r.Category == "damage" && r.Kind == "DamageDealt");
            Assert.IsTrue(dmg.PayloadJson.Contains("\"lethal\":false"),
                $"lethal=false when target survives. Got: {dmg.PayloadJson}");
        }

        // ====================================================================
        // 4. Records fire even for source-less environmental damage —
        //    broader than the existing DamageDealt event.
        // ====================================================================

        [Test]
        public void DamageRecord_FiresEvenWhenSourceIsNull()
        {
            var target = MakeMinimalCreature("target-005");

            // Source-less damage: traps, environmental hazards, status DoT
            // call ApplyDamage with source=null. The existing DamageDealt
            // GameEvent intentionally does NOT fire in this case (no
            // attacker to be notified), but the diag hook should — for
            // turn-reconstruction purposes the HP delta still matters.
            CombatSystem.ApplyDamage(target, 7, source: null, zone: null);

            var dmg = Diag.Snapshot(2000).First(r =>
                r.Category == "damage" && r.Kind == "DamageDealt");
            Assert.IsNull(dmg.ActorId,
                "Source-less damage must record with ActorId=null.");
            Assert.AreEqual("target-005", dmg.TargetId,
                "TargetId must still be populated.");
            Assert.IsTrue(dmg.PayloadJson.Contains("\"amount\":7"),
                $"Amount must be captured. Got: {dmg.PayloadJson}");
        }

        // ====================================================================
        // 5. Counter-check: zero-amount damage emits NO record
        //    (ApplyDamage returns early at the `if (amount <= 0) return;`
        //     guard before the diag hook runs)
        // ====================================================================

        [Test]
        public void ZeroDamage_DoesNotRecord()
        {
            var target = MakeMinimalCreature("target-006");

            // Zero damage: e.g., fully-resisted attack
            CombatSystem.ApplyDamage(target, 0, source: null, zone: null);

            var dmgRecords = Diag.Snapshot(2000)
                .Where(r => r.Category == "damage" && r.Kind == "DamageDealt")
                .ToList();
            Assert.AreEqual(0, dmgRecords.Count,
                "ApplyDamage with amount=0 must return early and emit NO " +
                "diag record. If this fails, the hook moved before the " +
                "amount-clamp guard.");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private static Entity MakeMinimalCreature(string id, int hp = 100)
        {
            var e = new Entity { BlueprintName = "TestCreature", ID = id };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
            { Name = "Hitpoints", BaseValue = hp, Max = hp, Owner = e };
            e.Statistics["DV"] = new Stat { BaseValue = 4, Owner = e };
            e.Statistics["Toughness"] = new Stat { BaseValue = 10, Owner = e };
            e.Statistics["Speed"] = new Stat { BaseValue = 100, Owner = e };
            e.AddPart(new RenderPart { DisplayName = "test creature" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }
    }
}
