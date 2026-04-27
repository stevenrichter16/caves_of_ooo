using System;
using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Phase 1 SURGICAL audit of CombatSystem. Covers ONLY the well-formed,
    /// contract-stable mechanics. Sketched mechanics (off-hand value, nat-20
    /// behavior, strMod-feeds-only-pen, dismemberment formula, DV bestDV-summing)
    /// are intentionally deferred — those need design decisions before being
    /// locked in by tests. See Docs/COMBAT-AUDIT-PLAN.md, "Path B" outcome.
    ///
    /// In scope:
    ///   • RollPenetrations smoke checks (Qud-parity details in RollPenetrationsParityTests)
    ///   • SelectHitLocation Abstract/TargetWeight skip + weighted distribution
    ///   • GatherMeleeWeapons equipped/default-behavior fallback + primary sort + 2H dedup
    ///   • HandleDeath cascade ordering (drops → Died → RemoveEntity)
    ///   • GetPartAV per-part armor + natural armor sum
    ///
    /// Out of scope here (covered elsewhere or sketched):
    ///   • ApplyDamage already-dead/no-stat guards   → AdversarialColdEyeTests
    ///   • BroadcastDeathWitnessed Passive+LOS       → WitnessedEffectTests
    ///   • Off-hand penalty value                    → sketched (-2 magic number)
    ///   • Natural-20 behavior                       → sketched (just bypasses DV)
    ///   • Dismemberment chance formula              → sketched (arbitrary tunables)
    /// </summary>
    public class CombatSystemSpecTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
        }

        // ====================================================================
        // RollPenetrations — Qud-parity surface checks.
        //
        // Detailed Qud-parity behavior (per-set pen counting, exploding dice,
        // bonus decay, MaxBonus cap) is exercised in RollPenetrationsParityTests.
        // This file only retains a few high-level checks that survive the port.
        //
        // Two prior tests were removed:
        //   • NegativePV_HighAV_ReturnsZero (superseded by parity test
        //     LoopTerminates_AtPathologicalNegativeBonus)
        //   • BalancedPVAV_NoStreak_ReturnsZeroToThree (stale: Qud allows >3
        //     via per-set streak)
        // ====================================================================

        [Test]
        public void RollPenetrations_HighBonus_ZeroTarget_StreakTriggers_ReturnsMoreThan3()
        {
            // Qud's outer loop continues while last set was a clean sweep. With
            // bonus=20 vs target=0, every roll [-1,8]+20 > 0 → every set sweeps
            // for several iterations until bonus decays into failure range.
            // Without the per-set continuation, max would be 1 pen. So pens > 3
            // proves the multi-set loop is doing its job.
            int pens = CombatSystem.RollPenetrations(0, 20, 50, new Random(42));
            Assert.Greater(pens, 3,
                "Per-set streak continuation must produce >3 pens with bonus=20 vs target=0");
        }

        [Test]
        public void RollPenetrations_HighBonus_ZeroTarget_LoopTerminates()
        {
            // The bonus-decay each set guarantees termination: bonus drops by 2
            // every iteration, eventually rolls fail, loop exits. Test asserts
            // the call returns and the count is finite/bounded.
            int pens = CombatSystem.RollPenetrations(0, 20, 50, new Random(42));
            Assert.Less(pens, 100, "Bonus-decay must bound the streak; no runaway loops");
        }

        [Test]
        public void RollPenetrations_Deterministic_SameSeedSameResult()
        {
            // Replay safety: identical seeded RNG must yield identical results.
            for (int seed = 0; seed < 20; seed++)
            {
                int a = CombatSystem.RollPenetrations(5, 7, 7, new Random(seed));
                int b = CombatSystem.RollPenetrations(5, 7, 7, new Random(seed));
                Assert.AreEqual(a, b, $"Same seed must produce same penetration count (seed {seed})");
            }
        }

        // ====================================================================
        // SelectHitLocation — pure function, weighted-random selection
        // Excludes Abstract parts and parts with TargetWeight ≤ 0.
        // ====================================================================

        [Test]
        public void SelectHitLocation_AllAbstract_ReturnsNull()
        {
            var entity = CreateCreatureWithBody();
            var body = entity.GetPart<Body>();
            var parts = body.GetParts();

            // Force every part to Abstract
            for (int i = 0; i < parts.Count; i++)
                parts[i].Abstract = true;

            BodyPart selected = CombatSystem.SelectHitLocation(body, new Random(1));
            Assert.IsNull(selected, "All Abstract parts → no valid target → null");
        }

        [Test]
        public void SelectHitLocation_AllZeroWeight_ReturnsNull()
        {
            var entity = CreateCreatureWithBody();
            var body = entity.GetPart<Body>();
            var parts = body.GetParts();

            for (int i = 0; i < parts.Count; i++)
                parts[i].TargetWeight = 0;

            BodyPart selected = CombatSystem.SelectHitLocation(body, new Random(1));
            Assert.IsNull(selected, "All TargetWeight=0 → no valid target → null");
        }

        [Test]
        public void SelectHitLocation_SkipsAbstractAndZeroWeight_OnlyEligibleSelected()
        {
            // Make EXACTLY one part eligible; SelectHitLocation must always pick it.
            var entity = CreateCreatureWithBody();
            var body = entity.GetPart<Body>();
            var parts = body.GetParts();

            // Disqualify all parts...
            for (int i = 0; i < parts.Count; i++)
            {
                parts[i].Abstract = true;
                parts[i].TargetWeight = 0;
            }

            // ...except one specific Hand
            BodyPart eligible = null;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i].Type == "Hand") { eligible = parts[i]; break; }
            }
            Assert.IsNotNull(eligible, "Test setup: humanoid should have a Hand");
            eligible.Abstract = false;
            eligible.TargetWeight = 5;

            // Many seeds — every selection must be the only eligible part.
            for (int seed = 0; seed < 25; seed++)
            {
                BodyPart selected = CombatSystem.SelectHitLocation(body, new Random(seed));
                Assert.AreSame(eligible, selected,
                    $"Only one eligible part exists; selection must always equal it (seed {seed})");
            }
        }

        [Test]
        public void SelectHitLocation_DistributionRoughlyMatchesWeights()
        {
            // Two eligible parts with weight ratio 4:1 should hit roughly 80%/20% over many trials.
            var entity = CreateCreatureWithBody();
            var body = entity.GetPart<Body>();
            var parts = body.GetParts();

            // Disqualify everything first
            for (int i = 0; i < parts.Count; i++)
            {
                parts[i].Abstract = true;
                parts[i].TargetWeight = 0;
            }

            // Pick the two hands; weight 4 vs 1
            BodyPart heavy = null, light = null;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i].Type != "Hand") continue;
                if (heavy == null) { heavy = parts[i]; continue; }
                if (light == null) { light = parts[i]; break; }
            }
            Assert.IsNotNull(heavy);
            Assert.IsNotNull(light);
            heavy.Abstract = false; heavy.TargetWeight = 4;
            light.Abstract = false; light.TargetWeight = 1;

            int heavyHits = 0, lightHits = 0;
            var rng = new Random(2024);
            const int trials = 1000;
            for (int i = 0; i < trials; i++)
            {
                BodyPart sel = CombatSystem.SelectHitLocation(body, rng);
                if (sel == heavy) heavyHits++;
                else if (sel == light) lightHits++;
            }

            // Expected: heavy ~800, light ~200. Allow ±10% slack for RNG variance.
            Assert.AreEqual(trials, heavyHits + lightHits, "Both hands together should account for all hits");
            Assert.Greater(heavyHits, lightHits * 2,
                $"Weight 4:1 → heavy must dominate. Got heavy={heavyHits}, light={lightHits}");
            Assert.Greater(heavyHits, 700, "Heavy should be near 80% (allow slack)");
            Assert.Greater(lightHits, 100, "Light should be near 20% (allow slack)");
        }

        // ====================================================================
        // GatherMeleeWeapons (private) — tested indirectly via PerformMeleeAttack
        // log scraping. We can't call the private method directly; instead we
        // observe the resulting attack message stream which names each
        // hand/weapon source (e.g. "[right hand: short sword]").
        // ====================================================================

        [Test]
        public void GatherMeleeWeapons_TwoEquippedHands_BothAttack()
        {
            var zone = new Zone();
            var attacker = CreateCreatureWithBody();
            zone.AddEntity(attacker, 5, 5);

            var hands = GetHands(attacker);
            Assert.AreEqual(2, hands.Count, "Test setup: humanoid has 2 hands");

            var weapon1 = CreateWeapon("longsword", "1d6", penBonus: 2);
            var weapon2 = CreateWeapon("dagger", "1d4", penBonus: 1);

            var inv = attacker.GetPart<InventoryPart>();
            inv.EquipToBodyPart(weapon1, hands[0]);
            inv.EquipToBodyPart(weapon2, hands[1]);

            var defender = CreateCreatureWithBody();
            zone.AddEntity(defender, 6, 5);

            // Run with high hit-bias seeds; attacker must attempt with BOTH weapons.
            // We're testing iteration, not damage; even misses leave a log entry per weapon.
            int longswordRefs = 0;
            int daggerRefs = 0;
            for (int seed = 0; seed < 8; seed++)
            {
                MessageLog.Clear();
                CombatSystem.PerformMeleeAttack(attacker, defender, zone, new Random(seed));
                foreach (var msg in MessageLog.GetRecent(20))
                {
                    if (msg.Contains("longsword")) longswordRefs++;
                    if (msg.Contains("dagger")) daggerRefs++;
                }
                // Reset defender HP between trials
                defender.GetStat("Hitpoints").BaseValue = 30;
            }

            Assert.Greater(longswordRefs, 0, "longsword (one hand) must attack at least once across seeds");
            Assert.Greater(daggerRefs, 0, "dagger (other hand) must attack at least once across seeds");
        }

        [Test]
        public void GatherMeleeWeapons_PrimaryHandAttacksFirst()
        {
            var zone = new Zone();
            var attacker = CreateCreatureWithBody();
            zone.AddEntity(attacker, 5, 5);

            var hands = GetHands(attacker);
            Assert.AreEqual(2, hands.Count);

            // Mark hands[1] as Primary (default is hands[0] via DefaultPrimary).
            // After the explicit Primary flag, the sort should put hands[1] first.
            hands[0].Primary = false;
            hands[0].DefaultPrimary = false;
            hands[1].Primary = true;

            var weaponLeft = CreateWeapon("offhand_blade", "1d4", penBonus: 1);
            var weaponRight = CreateWeapon("primary_axe", "1d8", penBonus: 3);

            var inv = attacker.GetPart<InventoryPart>();
            inv.EquipToBodyPart(weaponLeft, hands[0]);
            inv.EquipToBodyPart(weaponRight, hands[1]);

            var defender = CreateCreatureWithBody();
            zone.AddEntity(defender, 6, 5);

            // Find a seed that produces both a primary_axe message AND an offhand_blade message
            // and verify the axe message appears first.
            for (int seed = 0; seed < 30; seed++)
            {
                MessageLog.Clear();
                defender.GetStat("Hitpoints").BaseValue = 50;
                CombatSystem.PerformMeleeAttack(attacker, defender, zone, new Random(seed));
                var msgs = MessageLog.GetRecent(20);

                int axeIdx = -1, bladeIdx = -1;
                for (int i = 0; i < msgs.Count; i++)
                {
                    if (axeIdx < 0 && msgs[i].Contains("primary_axe")) axeIdx = i;
                    if (bladeIdx < 0 && msgs[i].Contains("offhand_blade")) bladeIdx = i;
                }
                if (axeIdx >= 0 && bladeIdx >= 0)
                {
                    Assert.Less(axeIdx, bladeIdx,
                        $"Primary hand weapon (axe) must appear in log before off-hand (blade). seed={seed}");
                    return;
                }
            }
            Assert.Fail("No seed produced both weapon log entries — test setup may be wrong");
        }

        [Test]
        public void GatherMeleeWeapons_NoEquipped_FallsBackToDefaultBehavior()
        {
            // Empty hands: PerformBodyPartAwareAttack with no weapons → punch path,
            // single attack from primary, with damage dice "1d2" (default in PerformSingleAttack).
            var zone = new Zone();
            var attacker = CreateCreatureWithBody();
            zone.AddEntity(attacker, 5, 5);
            // No weapons equipped, no _DefaultBehavior set on hands.

            var defender = CreateCreatureWithBody();
            zone.AddEntity(defender, 6, 5);

            // We only need to confirm the call doesn't crash and produces a hit/miss line.
            MessageLog.Clear();
            CombatSystem.PerformMeleeAttack(attacker, defender, zone, new Random(42));
            var msgs = MessageLog.GetRecent(20);
            bool sawAttackLine = false;
            foreach (var m in msgs)
            {
                if (m.Contains("misses") || m.Contains("hits") || m.Contains("fails to penetrate"))
                {
                    sawAttackLine = true;
                    break;
                }
            }
            Assert.IsTrue(sawAttackLine, "Empty-hands attack must still resolve a single punch attempt");
        }

        // ====================================================================
        // HandleDeath cascade ordering
        //   1. equipment dropped
        //   2. inventory dropped
        //   3. splatter
        //   4. Died event fires
        //   5. witness broadcast
        //   6. RemoveEntity (last)
        // We probe ordering by hooking into the Died event and snapshotting state.
        // ====================================================================

        [Test]
        public void HandleDeath_DropsEquipmentBeforeFiringDiedEvent()
        {
            var zone = new Zone();
            var creature = CreateCreatureWithBody();
            zone.AddEntity(creature, 5, 5);

            var hands = GetHands(creature);
            var weapon = CreateWeapon("orderprobe_sword", "1d4", penBonus: 0);
            creature.GetPart<InventoryPart>().EquipToBodyPart(weapon, hands[0]);

            bool weaponInZoneAtDiedTime = false;
            var probe = new DeathOrderProbe();
            probe.OnDied = () => weaponInZoneAtDiedTime = (zone.GetEntityCell(weapon) != null);
            creature.AddPart(probe);

            CombatSystem.HandleDeath(creature, null, zone);

            Assert.IsTrue(weaponInZoneAtDiedTime,
                "Equipment must be dropped (visible in zone) BEFORE the Died event fires");
        }

        [Test]
        public void HandleDeath_RemovesEntityAfterDiedEvent()
        {
            var zone = new Zone();
            var creature = CreateCreatureWithBody();
            zone.AddEntity(creature, 5, 5);

            bool stillInZoneAtDiedTime = false;
            var probe = new DeathOrderProbe();
            probe.OnDied = () => stillInZoneAtDiedTime = (zone.GetEntityCell(creature) != null);
            creature.AddPart(probe);

            CombatSystem.HandleDeath(creature, null, zone);

            Assert.IsTrue(stillInZoneAtDiedTime,
                "Entity must STILL be in zone when Died fires (Died handlers may need death-cell)");
            Assert.IsNull(zone.GetEntityCell(creature),
                "Entity must be removed AFTER HandleDeath returns");
        }

        // ====================================================================
        // GetPartAV — per-part armor + natural armor sum
        // ====================================================================

        [Test]
        public void GetPartAV_SumsEquippedArmorPlusNaturalArmor()
        {
            var entity = CreateCreatureWithBody();
            // Natural armor on the entity
            entity.GetPart<ArmorPart>().AV = 2;

            var hands = GetHands(entity);
            var glove = CreateArmorItem("test_glove", av: 3, dv: 0);
            entity.GetPart<InventoryPart>().EquipToBodyPart(glove, hands[0]);

            int avOnGlovedHand = CombatSystem.GetPartAV(entity, hands[0]);
            int avOnBareHand = CombatSystem.GetPartAV(entity, hands[1]);

            Assert.AreEqual(5, avOnGlovedHand, "Equipped armor (3) + natural armor (2) = 5");
            Assert.AreEqual(2, avOnBareHand, "Natural armor (2) only when no equipment");
        }

        // ====================================================================
        // Helpers (mirrors BodyPartSystemTests.CreateCreatureWithBody pattern)
        // ====================================================================

        private Entity CreateCreatureWithBody()
        {
            var entity = new Entity();
            entity.BlueprintName = "TestCreature";
            entity.Tags["Creature"] = "";

            entity.Statistics["Hitpoints"] = new Stat { Owner = entity, Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30 };
            entity.Statistics["Strength"] = new Stat { Owner = entity, Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Owner = entity, Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Toughness"] = new Stat { Owner = entity, Name = "Toughness", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Owner = entity, Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };

            entity.AddPart(new RenderPart { DisplayName = "test creature" });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new ArmorPart());
            entity.AddPart(new InventoryPart { MaxWeight = 150 });

            var body = new Body();
            entity.AddPart(body);
            body.SetBody(AnatomyFactory.CreateHumanoid());

            return entity;
        }

        private Entity CreateWeapon(string name, string damage, int penBonus)
        {
            var entity = new Entity();
            entity.BlueprintName = name;
            entity.Tags["Item"] = "";
            entity.AddPart(new RenderPart { DisplayName = name });
            entity.AddPart(new PhysicsPart { Takeable = true, Weight = 5 });
            entity.AddPart(new MeleeWeaponPart { BaseDamage = damage, PenBonus = penBonus });
            entity.AddPart(new EquippablePart { Slot = "Hand" });
            return entity;
        }

        private Entity CreateArmorItem(string name, int av, int dv)
        {
            var entity = new Entity();
            entity.BlueprintName = name;
            entity.Tags["Item"] = "";
            entity.AddPart(new RenderPart { DisplayName = name });
            entity.AddPart(new PhysicsPart { Takeable = true, Weight = 2 });
            entity.AddPart(new ArmorPart { AV = av, DV = dv });
            entity.AddPart(new EquippablePart { Slot = "Hand" });
            return entity;
        }

        private List<BodyPart> GetHands(Entity entity)
        {
            var body = entity.GetPart<Body>();
            var all = body.GetParts();
            var hands = new List<BodyPart>();
            for (int i = 0; i < all.Count; i++)
                if (all[i].Type == "Hand") hands.Add(all[i]);
            return hands;
        }
    }

    /// <summary>
    /// Test part that observes the moment the Died event fires.
    /// Used to assert ordering invariants in HandleDeath.
    /// </summary>
    public class DeathOrderProbe : Part
    {
        public override string Name => "DeathOrderProbe";
        public Action OnDied;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "Died")
                OnDied?.Invoke();
            return true;
        }
    }
}
