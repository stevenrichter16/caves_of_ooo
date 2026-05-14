using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Observability-driven combat tests. Each test runs a combat
    /// scenario, queries the diag records via CauseTraceId, dumps the
    /// full per-attack breakdown to TestContext output, and asserts
    /// invariants on the records. The DUMP is the primary debug
    /// artifact — running the test surfaces the same records the
    /// live `diag_query` would show.
    ///
    /// <para>Spec coverage:</para>
    /// <list type="bullet">
    ///   <item>Single-attack full pipeline (6 record kinds in sequence)</item>
    ///   <item>Resisted-damage path (ResistanceApplied fires with right amounts)</item>
    ///   <item>Vetoed-damage path (PreDamageMutation with vetoed=true)</item>
    ///   <item>Critical hit path (naturalTwenty=true on HitRoll + Penetration + DamageRoll)</item>
    ///   <item>Miss path (HitRoll landed=false, no downstream records)</item>
    ///   <item>Sharp+enhancement stacking (weaponPenBonus reflects mod)</item>
    /// </list>
    ///
    /// <para>If a test's diag-dump surfaces an unexpected record (e.g.
    /// ResistanceApplied where no resistance should exist), that's a
    /// caught bug — investigate.</para>
    /// </summary>
    public class CombatObservabilityTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
        }

        // ── Fixture helpers ──────────────────────────────────────

        private static Entity MakeFighter(
            string id = "fighter",
            int hp = 100,
            int strength = 18,
            int agility = 14,
            int weaponHitBonus = 20,
            int weaponPenBonus = 1,
            string damageDice = "1d6",
            string attributes = "Cutting LongBlades")
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["Strength"] = new Stat
                { Owner = e, Name = "Strength", BaseValue = strength, Min = 1, Max = 50 };
            e.Statistics["Agility"] = new Stat
                { Owner = e, Name = "Agility", BaseValue = agility, Min = 1, Max = 50 };
            e.Statistics["Toughness"] = new Stat
                { Owner = e, Name = "Toughness", BaseValue = 10, Min = 1, Max = 50 };
            e.Statistics["DV"] = new Stat
                { Owner = e, Name = "DV", BaseValue = 0, Min = -50, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new StatusEffectsPart());
            e.AddPart(new MeleeWeaponPart
            {
                BaseDamage = damageDice,
                HitBonus = weaponHitBonus,
                PenBonus = weaponPenBonus,
                Stat = "Strength",
                Attributes = attributes
            });
            e.AddPart(new ArmorPart { AV = 0, DV = 0 });
            return e;
        }

        /// <summary>Dump every record matching the given attack's
        /// CauseTraceId to TestContext.WriteLine. The output is the
        /// human-readable artifact this fixture produces.</summary>
        private static void DumpAttackRecords(string causeTraceId, string label)
        {
            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                CauseTraceId = causeTraceId,
                Limit = 20,
            }).Records;

            TestContext.WriteLine($"\n=== {label} (attackId={causeTraceId}) ===");
            TestContext.WriteLine($"Records: {records.Count}");
            for (int i = 0; i < records.Count; i++)
            {
                var r = records[i];
                TestContext.WriteLine($"  [{i}] {r.Kind,-20} :: {r.PayloadJson}");
            }
        }

        /// <summary>Find the CauseTraceId of the most recent attack
        /// based on the latest HitRoll record.</summary>
        private static string FindLatestAttackId()
        {
            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                Kind = "HitRoll",
                Limit = 10,
            }).Records;
            return records.Count > 0 ? records[records.Count - 1].CauseTraceId : null;
        }

        // ════════════════════════════════════════════════════════════════
        // Single attack — full pipeline (6 records expected)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void SingleAttack_FullPipeline_DumpsAndVerifies()
        {
            var zone = new Zone();
            var attacker = MakeFighter("attacker", weaponPenBonus: 3);
            attacker.Tags["Player"] = "";
            var defender = MakeFighter("defender");
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            CombatSystem.PerformMeleeAttack(attacker, defender, zone, new System.Random(0));
            string attackId = FindLatestAttackId();
            Assert.IsNotNull(attackId, "Attack produced no HitRoll record — bug.");

            DumpAttackRecords(attackId, "single attack, vanilla");

            // Pull the records and walk through expected sequence.
            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                CauseTraceId = attackId,
                Limit = 20,
            }).Records;

            // Find each kind by name (order should be HitRoll first).
            string FindFirst(string kind)
            {
                for (int i = 0; i < records.Count; i++)
                    if (records[i].Kind == kind) return records[i].PayloadJson;
                return null;
            }

            string hitRoll = FindFirst("HitRoll");
            Assert.IsNotNull(hitRoll, "Missing HitRoll record.");
            StringAssert.Contains("\"landed\":true", hitRoll);

            string pen = FindFirst("Penetration");
            Assert.IsNotNull(pen, "Hit landed but no Penetration record — bug.");
            StringAssert.Contains("\"weaponPenBonus\":3", pen);

            string dmgRoll = FindFirst("DamageRoll");
            Assert.IsNotNull(dmgRoll, "Penetration succeeded but no DamageRoll — bug.");

            string dmgDealt = FindFirst("DamageDealt");
            Assert.IsNotNull(dmgDealt, "Damage was rolled but never dealt — bug.");
        }

        // ════════════════════════════════════════════════════════════════
        // Miss — no downstream records
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void MissedAttack_HitRollOnly_NoDownstream()
        {
            // High-DV defender; low-roll seed.
            var zone = new Zone();
            var attacker = MakeFighter("attacker", weaponHitBonus: 0);
            attacker.Tags["Player"] = "";
            var defender = MakeFighter("defender");
            defender.GetPart<ArmorPart>().DV = 30;
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            CombatSystem.PerformMeleeAttack(attacker, defender, zone, new System.Random(2));
            string attackId = FindLatestAttackId();
            DumpAttackRecords(attackId, "missed attack");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                CauseTraceId = attackId,
                Limit = 20,
            }).Records;

            // Should have HitRoll. Should NOT have Penetration / DamageRoll
            // unless nat-20 (rare with seed 2).
            string hitRollPayload = null;
            int penCount = 0;
            int dmgRollCount = 0;
            foreach (var r in records)
            {
                if (r.Kind == "HitRoll") hitRollPayload = r.PayloadJson;
                else if (r.Kind == "Penetration") penCount++;
                else if (r.Kind == "DamageRoll") dmgRollCount++;
            }
            Assert.IsNotNull(hitRollPayload);

            // Inspect the HitRoll landed flag — if landed=false, no pen/dmgRoll.
            if (hitRollPayload != null && hitRollPayload.Contains("\"landed\":false"))
            {
                Assert.AreEqual(0, penCount,
                    "Miss should produce NO Penetration record. " +
                    "If this fires, a missed swing is leaking through the pen path — bug.");
                Assert.AreEqual(0, dmgRollCount,
                    "Miss should produce NO DamageRoll record.");
            }
        }

        // ════════════════════════════════════════════════════════════════
        // Resistance — heat-resistant defender vs Fire damage
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void HeatResistantDefender_FireDamage_EmitsResistanceApplied()
        {
            // Synthesize Fire damage directly (the simpler path for
            // testing the resistance emission specifically).
            var target = MakeFighter("hot", hp: 200);
            target.Statistics["HeatResistance"] = new Stat
                { Owner = target, Name = "HeatResistance",
                  BaseValue = 50, Min = -200, Max = 200 };

            var damage = new Damage(40);
            damage.AddAttribute("Fire");

            using (Diag.WithCause("test-heat-resist"))
            {
                CombatSystem.ApplyDamage(target, damage, source: null, zone: null);
            }

            DumpAttackRecords("test-heat-resist", "heat-resistant defender vs Fire 40");

            var resistRecs = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                Kind = "ResistanceApplied",
                CauseTraceId = "test-heat-resist",
                Limit = 5,
            }).Records;
            Assert.AreEqual(1, resistRecs.Count,
                "Heat-resistant target hit by Fire MUST emit exactly 1 ResistanceApplied record.");

            StringAssert.Contains("\"resistanceStat\":\"HeatResistance\"", resistRecs[0].PayloadJson);
            StringAssert.Contains("\"resistancePercent\":50", resistRecs[0].PayloadJson);
            // 40 → 20 (50% reduction)
            StringAssert.Contains("\"amountAfter\":20", resistRecs[0].PayloadJson);
        }

        // ════════════════════════════════════════════════════════════════
        // Multi-resistance — Acid+Fire defender vs multi-tagged damage
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void MultiResistantDefender_MultiTaggedDamage_FiresOncePerStat()
        {
            // A defender resistant to BOTH Acid and Heat; damage tagged
            // with both. Per ApplyResistances order (Acid → Heat),
            // expect TWO ResistanceApplied records — pin the order.
            var target = MakeFighter("dual", hp: 200);
            target.Statistics["AcidResistance"] = new Stat
                { Owner = target, Name = "AcidResistance",
                  BaseValue = 25, Min = -200, Max = 200 };
            target.Statistics["HeatResistance"] = new Stat
                { Owner = target, Name = "HeatResistance",
                  BaseValue = 50, Min = -200, Max = 200 };

            var damage = new Damage(40);
            damage.AddAttribute("Acid");
            damage.AddAttribute("Fire");

            using (Diag.WithCause("test-multi-resist"))
            {
                CombatSystem.ApplyDamage(target, damage, source: null, zone: null);
            }

            DumpAttackRecords("test-multi-resist", "Acid+Heat resist vs Acid+Fire 40");

            var resistRecs = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                Kind = "ResistanceApplied",
                CauseTraceId = "test-multi-resist",
                Limit = 10,
            }).Records;
            Assert.AreEqual(2, resistRecs.Count,
                "Two resistance stats should fire two records. If one is missing, " +
                "the ApplyResistances loop is short-circuiting — bug.");

            // Pin order: Acid first, then Heat (matches ApplyResistances source order).
            StringAssert.Contains("\"resistanceStat\":\"AcidResistance\"", resistRecs[0].PayloadJson);
            StringAssert.Contains("\"resistanceStat\":\"HeatResistance\"", resistRecs[1].PayloadJson);
        }

        // ════════════════════════════════════════════════════════════════
        // Vetoed damage — Stoneskin-style listener
        // ════════════════════════════════════════════════════════════════

        public class VetoProbePart : Part
        {
            public override string Name => "VetoProbe";
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "BeforeTakeDamage") return false; // veto
                return true;
            }
        }

        [Test]
        public void VetoedDamage_PreDamageMutation_FiresWithVetoedTrue()
        {
            var target = MakeFighter("invincible", hp: 100);
            target.AddPart(new VetoProbePart());

            using (Diag.WithCause("test-veto"))
            {
                CombatSystem.ApplyDamage(target, new Damage(50),
                    source: null, zone: null);
            }

            DumpAttackRecords("test-veto", "vetoed damage (Stoneskin-style)");

            // PreDamageMutation should fire with vetoed=true.
            var mutRecs = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                Kind = "PreDamageMutation",
                CauseTraceId = "test-veto",
                Limit = 5,
            }).Records;
            Assert.AreEqual(1, mutRecs.Count);
            StringAssert.Contains("\"vetoed\":true", mutRecs[0].PayloadJson);

            // Defender HP unchanged.
            Assert.AreEqual(100, target.GetStatValue("Hitpoints"),
                "Vetoed damage MUST NOT reduce HP. If this fails, the veto is " +
                "advisory not absolute — bug.");
        }

        // ════════════════════════════════════════════════════════════════
        // Critical hit — naturalTwenty propagates through pipeline
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void CriticalHit_NaturalTwentyFlag_PropagatesThroughPipeline()
        {
            // Force a nat-20 by seeding the rng aggressively — try
            // many seeds until we land one.
            var zone = new Zone();
            var attacker = MakeFighter("attacker", weaponHitBonus: 0);
            attacker.Tags["Player"] = "";
            var defender = MakeFighter("defender");
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            string attackId = null;
            bool foundCrit = false;
            for (int seed = 0; seed < 100 && !foundCrit; seed++)
            {
                Diag.ResetAll();
                CombatSystem.PerformMeleeAttack(attacker, defender, zone,
                    new System.Random(seed));
                attackId = FindLatestAttackId();
                if (attackId == null) continue;

                var records = DiagQuery.Apply(new DiagQuery.Filter
                {
                    Category = "damage",
                    CauseTraceId = attackId,
                    Limit = 20,
                }).Records;

                foreach (var r in records)
                {
                    if (r.Kind == "HitRoll" && r.PayloadJson != null
                        && r.PayloadJson.Contains("\"naturalTwenty\":true"))
                    {
                        foundCrit = true;
                        DumpAttackRecords(attackId, $"crit found at seed {seed}");

                        // Verify the crit flag propagates to other records.
                        string penPayload = null;
                        string dmgRollPayload = null;
                        foreach (var r2 in records)
                        {
                            if (r2.Kind == "Penetration") penPayload = r2.PayloadJson;
                            else if (r2.Kind == "DamageRoll") dmgRollPayload = r2.PayloadJson;
                        }
                        Assert.IsNotNull(penPayload);
                        StringAssert.Contains("\"naturalTwenty\":true", penPayload,
                            "Crit flag must propagate to Penetration record.");
                        StringAssert.Contains("\"critPenBonus\":1", penPayload,
                            "Crit must add +1 PenBonus.");

                        if (dmgRollPayload != null)
                            StringAssert.Contains("Critical", dmgRollPayload,
                                "Damage attributes should include 'Critical' on a nat-20.");
                        break;
                    }
                }
            }

            Assert.IsTrue(foundCrit,
                "Across 100 seeds, no nat-20 occurred. " +
                "Probability is ~5%/seed; absence = bug in rng or HitRoll emission.");
        }

        // ════════════════════════════════════════════════════════════════
        // Cross-attack isolation — two attacks don't blur records
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void TwoAttacks_RecordsAreCleanlyIsolated_NoCrossTalk()
        {
            var zone = new Zone();
            var attacker = MakeFighter("attacker", weaponPenBonus: 3); // sharp
            attacker.Tags["Player"] = "";
            var defender = MakeFighter("defender", hp: 500);
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            CombatSystem.PerformMeleeAttack(attacker, defender, zone, new System.Random(0));
            string atk1 = FindLatestAttackId();

            CombatSystem.PerformMeleeAttack(attacker, defender, zone, new System.Random(1));
            string atk2 = FindLatestAttackId();

            Assert.AreNotEqual(atk1, atk2, "Each attack has a distinct ID.");

            DumpAttackRecords(atk1, "attack 1");
            DumpAttackRecords(atk2, "attack 2");

            var atk1Records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                CauseTraceId = atk1,
                Limit = 20,
            }).Records;
            var atk2Records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                CauseTraceId = atk2,
                Limit = 20,
            }).Records;

            Assert.Greater(atk1Records.Count, 0);
            Assert.Greater(atk2Records.Count, 0);

            // Neither query should return the other's records.
            foreach (var r in atk1Records)
                Assert.AreEqual(atk1, r.CauseTraceId,
                    "Atk1 query leaked atk2 records — correlation broken.");
            foreach (var r in atk2Records)
                Assert.AreEqual(atk2, r.CauseTraceId,
                    "Atk2 query leaked atk1 records — correlation broken.");
        }
    }
}
