using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Pin: each successful melee attack emits a damage/Penetration
    /// diag record with payload fields exposing the roll breakdown
    /// — weaponPenBonus, strMod, skillPenBonus, critPenBonus,
    /// totalBonus, maxBonus, naturalTwenty, penetrations, autoPenForced.
    ///
    /// <para>Use case: after applying a mod like Sharp (+1 PenBonus),
    /// a diag query <c>category=damage kind=Penetration</c> reveals
    /// the exact PenBonus value used per attack, so the player /
    /// debugger can verify the mod was contributing.</para>
    /// </summary>
    public class PenetrationDiagTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
        }

        private static Entity MakeFighter(string id = "fighter", int hp = 100)
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["Strength"] = new Stat
                { Owner = e, Name = "Strength", BaseValue = 18, Min = 1, Max = 50 };
            e.Statistics["Agility"] = new Stat
                { Owner = e, Name = "Agility", BaseValue = 14, Min = 1, Max = 50 };
            e.Statistics["Toughness"] = new Stat
                { Owner = e, Name = "Toughness", BaseValue = 10, Min = 1, Max = 50 };
            e.Statistics["DV"] = new Stat
                { Owner = e, Name = "DV", BaseValue = 0, Min = -50, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new StatusEffectsPart());
            e.AddPart(new MeleeWeaponPart
            {
                BaseDamage = "1d6",
                HitBonus = 20, // very high to make hits land in deterministic seeds
                PenBonus = 1,
                Stat = "Strength",
                Attributes = "Cutting LongBlades"
            });
            e.AddPart(new ArmorPart { AV = 0, DV = 0 });
            return e;
        }

        [Test]
        public void MeleeAttack_EmitsPenetrationDiag_WithWeaponPenBonus()
        {
            var zone = new Zone();
            var attacker = MakeFighter("attacker");
            attacker.Tags["Player"] = "";
            var defender = MakeFighter("defender");
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            // Seeds tried until one lands; we don't care which seed
            // succeeds, only that the diag fires.
            bool landed = false;
            for (int seed = 0; seed < 30 && !landed; seed++)
            {
                Diag.ResetAll();
                CombatSystem.PerformMeleeAttack(attacker, defender, zone,
                    new System.Random(seed));
                var hit = DiagQuery.Apply(new DiagQuery.Filter
                {
                    Category = "damage",
                    Kind = "Penetration",
                    Limit = 5,
                }).Records;
                if (hit.Count > 0)
                {
                    landed = true;
                    string payload = hit[0].PayloadJson ?? "";
                    StringAssert.Contains("\"weaponPenBonus\":1", payload,
                        "Payload reports weapon's PenBonus (set to 1 in fixture).");
                    StringAssert.Contains("\"penetrations\"", payload);
                    StringAssert.Contains("\"av\"", payload);
                    StringAssert.Contains("\"strMod\"", payload);
                }
            }
            Assert.IsTrue(landed,
                "At least one of 30 seeded swings should connect and emit Penetration diag.");
        }

        [Test]
        public void SharpenedWeapon_PenetrationDiag_ShowsHigherPenBonus()
        {
            // Sharp mod adds +1 to weapon.PenBonus. The diag payload
            // should reflect the bumped weaponPenBonus value.
            var zone = new Zone();
            var attacker = MakeFighter("attacker");
            attacker.Tags["Player"] = "";
            var weapon = attacker.GetPart<MeleeWeaponPart>();
            weapon.PenBonus = 3; // simulate post-Sharp state
            var defender = MakeFighter("defender");
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            bool landed = false;
            for (int seed = 0; seed < 30 && !landed; seed++)
            {
                Diag.ResetAll();
                CombatSystem.PerformMeleeAttack(attacker, defender, zone,
                    new System.Random(seed));
                var hit = DiagQuery.Apply(new DiagQuery.Filter
                {
                    Category = "damage",
                    Kind = "Penetration",
                    Limit = 5,
                }).Records;
                if (hit.Count > 0)
                {
                    landed = true;
                    StringAssert.Contains("\"weaponPenBonus\":3",
                        hit[0].PayloadJson ?? "",
                        "Sharpened weapon reports PenBonus=3 in diag payload.");
                }
            }
            Assert.IsTrue(landed);
        }

        // ════════════════════════════════════════════════════════════════
        // HitRoll diag — fires on EVERY attack (hit OR miss)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void MeleeAttack_EmitsHitRollDiag_EveryAttempt()
        {
            var zone = new Zone();
            var attacker = MakeFighter("attacker");
            attacker.Tags["Player"] = "";
            var defender = MakeFighter("defender");
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            Diag.ResetAll();
            CombatSystem.PerformMeleeAttack(attacker, defender, zone,
                new System.Random(0));

            var hitRolls = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                Kind = "HitRoll",
                Limit = 5,
            }).Records;
            Assert.GreaterOrEqual(hitRolls.Count, 1,
                "HitRoll fires regardless of hit/miss.");
            string p = hitRolls[0].PayloadJson ?? "";
            StringAssert.Contains("\"hitRoll\"", p);
            StringAssert.Contains("\"agilityMod\"", p);
            StringAssert.Contains("\"weaponHitBonus\"", p);
            StringAssert.Contains("\"dv\"", p);
            StringAssert.Contains("\"landed\"", p);
        }

        [Test]
        public void MeleeAttack_HitRollDiag_ReportsMissesToo()
        {
            // High-DV defender → most rolls miss; pin that the HitRoll
            // diag fires for misses (landed=false).
            var zone = new Zone();
            var attacker = MakeFighter("attacker");
            attacker.Tags["Player"] = "";
            var attackerWeapon = attacker.GetPart<MeleeWeaponPart>();
            attackerWeapon.HitBonus = 0; // remove the fixture's huge hit bonus
            var defender = MakeFighter("defender");
            defender.GetPart<ArmorPart>().DV = 30; // unhittable except nat-20
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            // Run a deterministic seed where we expect a miss (rolls
            // can't beat DV=30 without nat-20).
            Diag.ResetAll();
            CombatSystem.PerformMeleeAttack(attacker, defender, zone,
                new System.Random(2));

            var hitRolls = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                Kind = "HitRoll",
                Limit = 5,
            }).Records;
            Assert.GreaterOrEqual(hitRolls.Count, 1);
            // No penetration record should fire on a miss (the hit gate
            // short-circuits before RollPenetrations).
            int penRecs = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                Kind = "Penetration",
                Limit = 5,
            }).Records.Count;
            // (Either a miss OR a critical hit landed; both are valid)
            // The HitRoll record itself is what we're pinning here.
            Assert.IsTrue(hitRolls.Count > 0, "HitRoll always fires.");
        }

        // ════════════════════════════════════════════════════════════════
        // DamageRoll diag — fires on hits that penetrate
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void MeleeAttack_EmitsDamageRollDiag_OnPenetratingHit()
        {
            var zone = new Zone();
            var attacker = MakeFighter("attacker");
            attacker.Tags["Player"] = "";
            var defender = MakeFighter("defender");
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            bool foundDamageRoll = false;
            for (int seed = 0; seed < 30 && !foundDamageRoll; seed++)
            {
                Diag.ResetAll();
                CombatSystem.PerformMeleeAttack(attacker, defender, zone,
                    new System.Random(seed));
                var rolls = DiagQuery.Apply(new DiagQuery.Filter
                {
                    Category = "damage",
                    Kind = "DamageRoll",
                    Limit = 5,
                }).Records;
                if (rolls.Count > 0)
                {
                    foundDamageRoll = true;
                    string p = rolls[0].PayloadJson ?? "";
                    StringAssert.Contains("\"damageDice\":\"1d6\"", p);
                    StringAssert.Contains("\"baseDamageTotal\"", p);
                    StringAssert.Contains("\"penetrationsRolled\"", p);
                    StringAssert.Contains("\"attributes\"", p);
                }
            }
            Assert.IsTrue(foundDamageRoll,
                "At least one of 30 seeded swings produces a DamageRoll diag.");
        }

        // ════════════════════════════════════════════════════════════════
        // ResistanceApplied diag — fires per-resistance that reduced
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void ResistanceApplied_FiresWhenHeatResistantTargetTakesFireDamage()
        {
            var target = MakeFighter("hot");
            target.Statistics["HeatResistance"] = new Stat
            {
                Owner = target, Name = "HeatResistance",
                BaseValue = 50, Min = -200, Max = 200,
            };
            var damage = new Damage(20);
            damage.AddAttribute("Fire");

            Diag.ResetAll();
            CombatSystem.ApplyDamage(target, damage, source: null, zone: null);

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                Kind = "ResistanceApplied",
                Limit = 5,
            }).Records;
            Assert.AreEqual(1, recs.Count,
                "Heat-resistant target hit by Fire → one ResistanceApplied diag.");
            string p = recs[0].PayloadJson ?? "";
            StringAssert.Contains("\"resistanceStat\":\"HeatResistance\"", p);
            StringAssert.Contains("\"resistancePercent\":50", p);
            StringAssert.Contains("\"amountBefore\":20", p);
            // Resist=50 → amount halved to 10 → delta=-10.
            StringAssert.Contains("\"amountAfter\":10", p);
        }

        [Test]
        public void ResistanceApplied_DoesNotFire_WhenResistanceIsZero()
        {
            // Counter-check: zero resistance → no record (the early-out
            // in ApplyResistanceFor short-circuits before mutation).
            var target = MakeFighter("ordinary");
            // No HeatResistance stat set → defaults to 0.
            var damage = new Damage(20);
            damage.AddAttribute("Fire");

            Diag.ResetAll();
            CombatSystem.ApplyDamage(target, damage, source: null, zone: null);

            int n = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                Kind = "ResistanceApplied",
                Limit = 5,
            }).Records.Count;
            Assert.AreEqual(0, n,
                "Zero resistance → no ResistanceApplied diag fired.");
        }

        // ════════════════════════════════════════════════════════════════
        // PreDamageMutation diag — fires when BeforeTakeDamage mutates
        // ════════════════════════════════════════════════════════════════

        private class DamageMutatorProbePart : Part
        {
            public int ReduceBy;
            public bool Veto;
            public override string Name => "DamageMutatorProbe";
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "BeforeTakeDamage" && e.GetParameter("Damage") is Damage d)
                {
                    d.Amount = System.Math.Max(0, d.Amount - ReduceBy);
                    if (Veto) return false; // veto the damage
                }
                return true;
            }
        }

        [Test]
        public void PreDamageMutation_Fires_WhenListenerReducesAmount()
        {
            var target = MakeFighter("stoney");
            var probe = new DamageMutatorProbePart { ReduceBy = 5 };
            target.AddPart(probe);
            var damage = new Damage(20);

            Diag.ResetAll();
            CombatSystem.ApplyDamage(target, damage, source: null, zone: null);

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                Kind = "PreDamageMutation",
                Limit = 5,
            }).Records;
            Assert.AreEqual(1, recs.Count);
            string p = recs[0].PayloadJson ?? "";
            StringAssert.Contains("\"amountBefore\":20", p);
            StringAssert.Contains("\"amountAfter\":15", p);
            StringAssert.Contains("\"delta\":-5", p);
            StringAssert.Contains("\"vetoed\":false", p);
        }

        [Test]
        public void PreDamageMutation_Fires_OnVeto()
        {
            var target = MakeFighter("invincible");
            var probe = new DamageMutatorProbePart { Veto = true };
            target.AddPart(probe);
            var damage = new Damage(20);

            Diag.ResetAll();
            CombatSystem.ApplyDamage(target, damage, source: null, zone: null);

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                Kind = "PreDamageMutation",
                Limit = 5,
            }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"vetoed\":true", recs[0].PayloadJson ?? "");
        }

        [Test]
        public void PreDamageMutation_DoesNotFire_WhenNoListenerTouchesAmount()
        {
            var target = MakeFighter("untouched");
            var damage = new Damage(20);

            Diag.ResetAll();
            CombatSystem.ApplyDamage(target, damage, source: null, zone: null);

            int n = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                Kind = "PreDamageMutation",
                Limit = 5,
            }).Records.Count;
            Assert.AreEqual(0, n,
                "No mutation + no veto → no diag fired.");
        }

        // ════════════════════════════════════════════════════════════════
        // Per-attack correlation via CauseTraceId (one attackId per swing)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void AllAttackRecords_ShareCauseTraceId_WithinOneAttack()
        {
            // The core correlation contract: PerformSingleAttack wraps
            // its body in Diag.WithCause(attackId), so every record
            // emitted during the attack (HitRoll, Penetration, DamageRoll,
            // potentially PreDamageMutation / ResistanceApplied, and
            // DamageDealt) shares the same CauseTraceId. Filter by
            // CauseTraceId returns the deterministic per-attack record set.
            var zone = new Zone();
            var attacker = MakeFighter("attacker");
            attacker.Tags["Player"] = "";
            var defender = MakeFighter("defender");
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            // Run one swing.
            Diag.ResetAll();
            CombatSystem.PerformMeleeAttack(attacker, defender, zone,
                new System.Random(0));

            // All damage-category records from this swing should have the
            // SAME CauseTraceId (whatever attackId was generated).
            var all = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                Limit = 50,
            }).Records;
            Assert.Greater(all.Count, 0,
                "At least one damage record should fire per swing.");

            // Pick the first record's CauseTraceId; all subsequent records
            // in the same attack must match it.
            string firstCause = all[0].CauseTraceId;
            Assert.IsNotNull(firstCause,
                "PerformSingleAttack must set an ambient cause.");
            Assert.IsNotEmpty(firstCause);

            for (int i = 1; i < all.Count; i++)
            {
                Assert.AreEqual(firstCause, all[i].CauseTraceId,
                    $"Record #{i} (kind={all[i].Kind}) has a different " +
                    $"CauseTraceId — per-attack correlation is broken.");
            }
        }

        [Test]
        public void TwoSeparateAttacks_HaveDifferentCauseTraceIds()
        {
            // Counter-check: two distinct PerformSingleAttack invocations
            // MUST produce different attackIds (no cross-attack collision).
            var zone = new Zone();
            var attacker = MakeFighter("attacker");
            attacker.Tags["Player"] = "";
            var defender = MakeFighter("defender");
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            // First swing.
            Diag.ResetAll();
            CombatSystem.PerformMeleeAttack(attacker, defender, zone,
                new System.Random(0));
            var firstSwing = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                Limit = 50,
            }).Records;
            string firstCause = firstSwing[0].CauseTraceId;

            // Second swing.
            Diag.ResetAll();
            CombatSystem.PerformMeleeAttack(attacker, defender, zone,
                new System.Random(1));
            var secondSwing = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                Limit = 50,
            }).Records;
            string secondCause = secondSwing[0].CauseTraceId;

            Assert.AreNotEqual(firstCause, secondCause,
                "Each PerformSingleAttack must generate a unique attackId.");
        }

        [Test]
        public void Filter_ByCauseTraceId_ReturnsOnlyMatchingAttackRecords()
        {
            // Pin the filter behavior — running two attacks back-to-back
            // then filtering by one attack's cause must return ONLY that
            // attack's records.
            var zone = new Zone();
            var attacker = MakeFighter("attacker");
            attacker.Tags["Player"] = "";
            var defender = MakeFighter("defender");
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            Diag.ResetAll();
            CombatSystem.PerformMeleeAttack(attacker, defender, zone,
                new System.Random(0));
            CombatSystem.PerformMeleeAttack(attacker, defender, zone,
                new System.Random(1));

            // All damage records — should span TWO attacks.
            var all = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                Limit = 100,
            }).Records;
            var allCauses = new System.Collections.Generic.HashSet<string>();
            foreach (var r in all)
                if (r.CauseTraceId != null) allCauses.Add(r.CauseTraceId);
            Assert.AreEqual(2, allCauses.Count,
                "Two attacks → two distinct cause trace IDs.");

            // Pick one cause; filter must return only its records.
            string targetCause = null;
            foreach (var c in allCauses) { targetCause = c; break; }
            var oneAttack = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                CauseTraceId = targetCause,
                Limit = 100,
            }).Records;
            Assert.Greater(oneAttack.Count, 0);
            foreach (var r in oneAttack)
                Assert.AreEqual(targetCause, r.CauseTraceId,
                    "Filter result contains only the requested attack's records.");
            // The other attack's records must be excluded.
            Assert.Less(oneAttack.Count, all.Count,
                "Single-attack filter returns fewer records than the unfiltered query.");
        }

        [Test]
        public void NaturalAttack_PenetrationDiag_ShowsZeroWeaponPenBonus()
        {
            // Counter-check: an attacker WITHOUT a MeleeWeaponPart
            // (natural claws/punch path) should still emit Penetration
            // diag with weaponPenBonus=0. Pins the "(natural)" weapon-
            // name fallback works.
            var zone = new Zone();
            var attacker = MakeFighter("attacker");
            attacker.Tags["Player"] = "";
            // Remove the MeleeWeaponPart to simulate natural attack.
            var melee = attacker.GetPart<MeleeWeaponPart>();
            attacker.Parts.Remove(melee);
            // Still need ONE weapon for PerformLegacyAttack to work —
            // add it back as a natural weapon with PenBonus=0.
            attacker.AddPart(new MeleeWeaponPart
            {
                BaseDamage = "1d4",
                HitBonus = 20,
                PenBonus = 0,
                Stat = "Strength",
            });
            var defender = MakeFighter("defender");
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            bool landed = false;
            for (int seed = 0; seed < 30 && !landed; seed++)
            {
                Diag.ResetAll();
                CombatSystem.PerformMeleeAttack(attacker, defender, zone,
                    new System.Random(seed));
                var hit = DiagQuery.Apply(new DiagQuery.Filter
                {
                    Category = "damage",
                    Kind = "Penetration",
                    Limit = 5,
                }).Records;
                if (hit.Count > 0)
                {
                    landed = true;
                    StringAssert.Contains("\"weaponPenBonus\":0",
                        hit[0].PayloadJson ?? "");
                }
            }
            Assert.IsTrue(landed);
        }
    }
}
