using System;
using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Dedicated adversarial sweep for
    /// Docs/COMBAT-AUDIT-BUGFIX-PLAN-2026-07.md (SM1-SM14), per CLAUDE.md's
    /// mandatory adversarial-sweep gate and the plan's own §3 applicability
    /// check (state atomicity, cross-actor flows, RNG-gated behavior, diag
    /// emission contracts, save/load reach — all named there).
    ///
    /// This file does NOT duplicate the per-sub-milestone tests already
    /// shipped alongside each fix (CombatSystemTests, AdversarialColdEyeTests,
    /// CanBeDismemberedTests, OnHitClassEffectsTests, OnHitWeaponEffectsTests,
    /// StatusEffectTests, CombatSystemSpecTests, MultiWeaponPenaltyTests,
    /// GasSystemTests, EffectRoundTripPrivateStateTests). Each of those
    /// proves ONE gate/fix fires correctly in isolation. This file probes
    /// what happens when fixes COMPOSE: cross-gate mutual exclusivity,
    /// cross-entity scratch-list reuse (the SM11-13 perf changes), full
    /// multi-effect save/load round-trips, and boundary inputs the
    /// per-gate tests didn't try (negative Magnitude, null Creator, exact
    /// 0%/100% RNG boundaries).
    /// </summary>
    public class CombatAuditBugfixAdversarialTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
        }

        // ====================================================================
        // Helpers (mirror CombatSystemSpecTests/CombatSystemTests fixtures)
        // ====================================================================

        private Entity CreateCreatureWithBody(int hp = 30)
        {
            var entity = new Entity();
            entity.BlueprintName = "TestCreature";
            entity.Tags["Creature"] = "";
            entity.Statistics["Hitpoints"] = new Stat { Owner = entity, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
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

        private Entity CreateWeapon(string name, string damage, string attributes = "", int hitBonus = 0, int penBonus = 20)
        {
            // penBonus defaults high (not 0): HitBonus alone only guarantees
            // the ACCURACY roll lands -- RollPenetrations is a separate gate,
            // and with a fixture creature's Strength=16 (StatUtils.GetModifier
            // floors (16-16)/2 = 0), a 0 PenBonus weapon can still roll 0
            // penetrations (no damage at all) purely by bad luck on some
            // seeds. Tests that need a GUARANTEED hit need both.
            var entity = new Entity();
            entity.BlueprintName = name;
            entity.Tags["Item"] = "";
            entity.AddPart(new RenderPart { DisplayName = name });
            entity.AddPart(new PhysicsPart { Takeable = true, Weight = 5 });
            entity.AddPart(new MeleeWeaponPart { BaseDamage = damage, Attributes = attributes, HitBonus = hitBonus, PenBonus = penBonus });
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

        // ════════════════════════════════════════════════════════════
        //   GROUP A — State atomicity: DEATH_HANDLED_TAG idempotency
        // ════════════════════════════════════════════════════════════

        [Test]
        public void HandleDeath_CalledTwiceDirectly_OnlyFiresOnce()
        {
            var zone = new Zone();
            var target = CreateCreatureWithBody(1);
            zone.AddEntity(target, 5, 5);
            var killer = CreateCreatureWithBody();
            zone.AddEntity(killer, 6, 5);

            CombatSystem.HandleDeath(target, killer, zone);
            CombatSystem.HandleDeath(target, killer, zone); // simulates a second trigger racing in

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "damage", Kind = "DeathHandled", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count, "DEATH_HANDLED_TAG must make a second HandleDeath call a no-op");

            int killMessages = 0;
            foreach (var msg in MessageLog.GetRecent(20))
                if (msg.Contains("is killed by")) killMessages++;
            Assert.AreEqual(1, killMessages, "the kill message must not double-log");
        }

        [Test]
        public void HandleDeath_ViaLethalApplyDamage_ThenDirectCall_OnlyFiresOnce()
        {
            // Simulates the concrete SM6 trigger: an ordinary lethal hit
            // runs the full HandleDeath lifecycle, then something downstream
            // in the same attack (e.g. a dismemberment check) independently
            // tries to invoke HandleDeath again on the now-dead target.
            var zone = new Zone();
            var target = CreateCreatureWithBody(5);
            zone.AddEntity(target, 5, 5);
            var killer = CreateCreatureWithBody();
            zone.AddEntity(killer, 6, 5);

            var dmg = new Damage(10);
            CombatSystem.ApplyDamage(target, dmg, killer, zone);
            CombatSystem.HandleDeath(target, killer, zone); // redundant second trigger

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "damage", Kind = "DeathHandled", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
        }

        [Test]
        public void HandleDeath_TwoIndependentTargets_EachGetsOwnDeathHandledRecord()
        {
            // Counter-check: the idempotency tag is per-entity, not a global
            // latch. A buggy static-flag implementation would suppress the
            // second entity's legitimate death entirely.
            var zone = new Zone();
            var targetA = CreateCreatureWithBody(1);
            var targetB = CreateCreatureWithBody(1);
            zone.AddEntity(targetA, 5, 5);
            zone.AddEntity(targetB, 8, 8);
            var killer = CreateCreatureWithBody();
            zone.AddEntity(killer, 6, 5);

            CombatSystem.HandleDeath(targetA, killer, zone);
            CombatSystem.HandleDeath(targetB, killer, zone);

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "damage", Kind = "DeathHandled", Limit = 5 }).Records;
            Assert.AreEqual(2, recs.Count, "each independent death must get its own record");
        }

        // ════════════════════════════════════════════════════════════
        //   GROUP B — Cross-actor: Shatter-armor / GetAV-GetPartAV parity
        // ════════════════════════════════════════════════════════════

        [Test]
        public void ShatterArmor_AppliedByOneAttacker_ReducesAVForAnyLaterQuery()
        {
            // Cross-actor: ShatterArmorEffect is defender-state, not scoped
            // to the attacker who applied it. A later, unrelated query
            // (e.g. a different attacker's hit-location AV check) must see
            // the same reduction.
            var defender = CreateCreatureWithBody();
            // CreateCreatureWithBody already adds a default ArmorPart -- mutate
            // it directly (AddPart would append a SECOND ArmorPart that
            // GetPart<ArmorPart>() never sees, since it returns the first match).
            defender.GetPart<ArmorPart>().AV = 10;
            int avBefore = CombatSystem.GetAV(defender);

            defender.ForceApplyEffect(new ShatterArmorEffect());
            int avAfter = CombatSystem.GetAV(defender);

            Assert.AreEqual(avBefore - ShatterArmorEffect.AV_REDUCTION, avAfter);
        }

        [Test]
        public void ShatterArmor_GetAVAndGetPartAV_ApplySameReduction()
        {
            var defender = CreateCreatureWithBody();
            defender.GetPart<ArmorPart>().AV = 10;
            defender.ForceApplyEffect(new ShatterArmorEffect());
            var shatter = defender.GetEffect<ShatterArmorEffect>();
            shatter.StackCount = 2;

            var body = defender.GetPart<Body>();
            var hitPart = CombatSystem.SelectHitLocation(body, new Random(1));
            Assert.IsNotNull(hitPart, "test setup: humanoid must have a severable/targetable part");

            int viaGetAV = CombatSystem.GetAV(defender);
            int viaGetPartAV = CombatSystem.GetPartAV(defender, hitPart);

            // Both paths share ApplyShatterArmorReduction — SM5's whole point
            // was that GetPartAV (the path virtually all real combat uses)
            // previously never called it at all.
            Assert.AreEqual(viaGetAV, viaGetPartAV,
                "GetAV and GetPartAV must apply the identical Shatter reduction");
        }

        [Test]
        public void ShatterArmor_StackedReduction_ClampsAtZero_NotNegative()
        {
            var defender = CreateCreatureWithBody();
            defender.GetPart<ArmorPart>().AV = 1; // small AV, large stack should overshoot to negative pre-clamp
            defender.ForceApplyEffect(new ShatterArmorEffect());
            var shatter = defender.GetEffect<ShatterArmorEffect>();
            shatter.StackCount = 5; // 5 * AV_REDUCTION(2) = 10, way more than AV=1

            int av = CombatSystem.GetAV(defender);
            Assert.AreEqual(0, av, "AV must clamp at 0, never go negative");
        }

        // ════════════════════════════════════════════════════════════
        //   GROUP C — RNG boundaries + cross-entity scratch-list safety
        //   (SM11/SM12/SM13's shared static scratch lists)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void OffHandChance_ExactlyZero_NeverAttacksAcrossManySeeds()
        {
            var attacker = CreateCreatureWithBody();
            attacker.Statistics["MultiWeaponSkillBonus"] = new Stat
            { Owner = attacker, Name = "MultiWeaponSkillBonus", BaseValue = -CombatSystem.BASE_SECONDARY_ATTACK_CHANCE_PERCENT, Min = -1000, Max = 1000 };
            Assert.AreEqual(0, CombatSystem.GetOffHandAttackChancePercent(attacker));

            for (int seed = 0; seed < 30; seed++)
                Assert.IsFalse(new Random(seed).Next(100) < CombatSystem.GetOffHandAttackChancePercent(attacker),
                    $"chance=0 must never pass the roll (seed={seed})");
        }

        [Test]
        public void OffHandChance_ExactlyOneHundred_AlwaysAttacksAcrossManySeeds()
        {
            var attacker = CreateCreatureWithBody();
            attacker.Statistics["MultiWeaponSkillBonus"] = new Stat
            { Owner = attacker, Name = "MultiWeaponSkillBonus", BaseValue = 100 - CombatSystem.BASE_SECONDARY_ATTACK_CHANCE_PERCENT, Min = -1000, Max = 1000 };
            int chance = CombatSystem.GetOffHandAttackChancePercent(attacker);
            Assert.AreEqual(100, chance);

            for (int seed = 0; seed < 30; seed++)
                Assert.IsTrue(new Random(seed).Next(100) < chance,
                    $"chance=100 must always pass the roll (seed={seed}, rng.Next(100) is in [0,99])");
        }

        [Test]
        public void GatherMeleeWeapons_ScratchList_DoesNotLeakBetweenDifferentAttackers()
        {
            // SM11/SM13: GatherMeleeWeapons reuses a static scratch List<WeaponSlot>.
            // If a subsequent call for a DIFFERENT attacker didn't Clear() it
            // (or cleared the wrong list), attacker B's attack could pick up
            // attacker A's stale weapon slots.
            var zone = new Zone();
            var attackerA = CreateCreatureWithBody();
            zone.AddEntity(attackerA, 5, 5);
            var handsA = GetHands(attackerA);
            var invA = attackerA.GetPart<InventoryPart>();
            var weaponA1 = CreateWeapon("weaponA1", "1d4", hitBonus: 100);
            var weaponA2 = CreateWeapon("weaponA2", "1d4", hitBonus: 100);
            invA.AddObject(weaponA1); invA.AddObject(weaponA2);
            invA.EquipToBodyPart(weaponA1, handsA[0]);
            invA.EquipToBodyPart(weaponA2, handsA[1]);
            var defenderA = CreateCreatureWithBody(50);
            zone.AddEntity(defenderA, 6, 5);

            var attackerB = CreateCreatureWithBody();
            zone.AddEntity(attackerB, 10, 10);
            var handsB = GetHands(attackerB);
            var invB = attackerB.GetPart<InventoryPart>();
            var weaponB1 = CreateWeapon("weaponB1", "1d4", hitBonus: 100);
            invB.AddObject(weaponB1);
            invB.EquipToBodyPart(weaponB1, handsB[0]);
            var defenderB = CreateCreatureWithBody(50);
            zone.AddEntity(defenderB, 11, 10);

            // Attacker A swings first (populates the shared scratch list),
            // then attacker B swings immediately after.
            CombatSystem.PerformMeleeAttack(attackerA, defenderA, zone, new Random(1));
            MessageLog.Clear();
            CombatSystem.PerformMeleeAttack(attackerB, defenderB, zone, new Random(1));

            bool sawWeaponAName = false;
            foreach (var msg in MessageLog.GetRecent(20))
                if (msg.Contains("weaponA1") || msg.Contains("weaponA2")) sawWeaponAName = true;

            Assert.IsFalse(sawWeaponAName,
                "attacker B's attack must not reference attacker A's stale weapon names");
        }

        [Test]
        public void GetAV_EquippedPartsScratchList_DoesNotLeakBetweenDifferentEntities()
        {
            // SM12: GetAV/GetDV reuse static scratch lists for equipped body
            // parts. Querying entity B's AV right after entity A's must not
            // include A's armor.
            var entityA = CreateCreatureWithBody();
            var armorItemA = new Entity { BlueprintName = "ArmorA" };
            armorItemA.Tags["Item"] = "";
            armorItemA.AddPart(new RenderPart { DisplayName = "armorA" });
            armorItemA.AddPart(new PhysicsPart { Takeable = true, Weight = 2 });
            armorItemA.AddPart(new ArmorPart { AV = 20 });
            armorItemA.AddPart(new EquippablePart { Slot = "Hand" });
            var invA = entityA.GetPart<InventoryPart>();
            invA.AddObject(armorItemA);
            invA.EquipToBodyPart(armorItemA, GetHands(entityA)[0]);

            int avA = CombatSystem.GetAV(entityA);
            Assert.GreaterOrEqual(avA, 20);

            var entityB = CreateCreatureWithBody(); // no equipped armor at all
            int avB = CombatSystem.GetAV(entityB);

            Assert.AreEqual(0, avB, "entity B must not inherit entity A's equipped-armor AV via a leaked scratch list");
        }

        [Test]
        public void SelectHitLocation_ScratchList_DoesNotLeakBetweenDifferentBodies()
        {
            var bodyA = CreateCreatureWithBody().GetPart<Body>();
            var bodyB = CreateCreatureWithBody().GetPart<Body>();

            var partsA = bodyA.GetParts();
            var partsB = bodyB.GetParts();

            // Call SelectHitLocation for A, then immediately for B, then
            // confirm B's returned part actually belongs to B's own tree
            // (not a stale reference surviving from A's scratch-populated call).
            var hitA = CombatSystem.SelectHitLocation(bodyA, new Random(1));
            var hitB = CombatSystem.SelectHitLocation(bodyB, new Random(1));

            Assert.IsNotNull(hitA);
            Assert.IsNotNull(hitB);
            Assert.Contains(hitB, partsB, "SelectHitLocation(bodyB) must return a part from bodyB's own tree");
            CollectionAssert.DoesNotContain(partsA, hitB, "bodyB's hit part must not be one of bodyA's stale parts");
        }

        // ════════════════════════════════════════════════════════════
        //   GROUP D — Diag emission contracts: cross-gate mutual exclusivity
        // ════════════════════════════════════════════════════════════

        [Test]
        public void VetoedMeleeAttack_EmitsOnlyMeleeAttackVetoed_NoDownstreamDiag()
        {
            // A BeforeMeleeAttack veto means the ENTIRE downstream pipeline
            // never runs. No per-gate SM10 test checked that the OTHER 7
            // gates all stay silent for this exact scenario.
            var zone = new Zone();
            var attacker = CreateCreatureWithBody();
            attacker.AddPart(new CancelAttackPart());
            zone.AddEntity(attacker, 5, 5);
            var defender = CreateCreatureWithBody();
            zone.AddEntity(defender, 6, 5);

            CombatSystem.PerformMeleeAttack(attacker, defender, zone, new Random(1));

            var vetoed = DiagQuery.Apply(new DiagQuery.Filter { Category = "damage", Kind = "MeleeAttackVetoed", Limit = 5 }).Records;
            Assert.AreEqual(1, vetoed.Count);

            string[] downstreamKinds = { "ApplyDamageRejected", "DamageFullyResisted", "DeathHandled", "Dismemberment" };
            foreach (var kind in downstreamKinds)
            {
                var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "damage", Kind = kind, Limit = 5 }).Records;
                Assert.AreEqual(0, recs.Count, $"{kind} must not fire when BeforeMeleeAttack vetoed the whole attack");
            }
        }

        [Test]
        public void LethalHit_EmitsDeathHandled_NotApplyDamageRejected()
        {
            // Proves the DEATH_HANDLED_TAG mechanism doesn't cause the
            // FIRST, legitimate kill to be mis-classified as "already dead".
            var zone = new Zone();
            var target = CreateCreatureWithBody(5);
            zone.AddEntity(target, 5, 5);
            var killer = CreateCreatureWithBody();
            zone.AddEntity(killer, 6, 5);

            CombatSystem.ApplyDamage(target, new Damage(10), killer, zone);

            var death = DiagQuery.Apply(new DiagQuery.Filter { Category = "damage", Kind = "DeathHandled", Limit = 5 }).Records;
            Assert.AreEqual(1, death.Count);
            var rejected = DiagQuery.Apply(new DiagQuery.Filter { Category = "damage", Kind = "ApplyDamageRejected", Limit = 5 }).Records;
            Assert.AreEqual(0, rejected.Count, "the killing hit itself must not also be flagged as rejected/already-dead");
        }

        [Test]
        public void CheckCombatDismemberment_AcrossManySeeds_ExactlyOneRecordPerCall_OutcomeAlwaysKnownValue()
        {
            var knownOutcomes = new HashSet<string> { "not_severable", "below_threshold", "roll_failed", "vetoed", "fired" };
            for (int seed = 0; seed < 25; seed++)
            {
                Diag.ResetAll();
                var defender = CreateCreatureWithBody(100);
                var body = defender.GetPart<Body>();
                var hitPart = CombatSystem.SelectHitLocation(body, new Random(seed));
                if (hitPart == null) continue;

                CombatSystem.CheckCombatDismemberment(defender, body, hitPart, damage: 60, zone: null, rng: new Random(seed));

                var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "damage", Kind = "Dismemberment", Limit = 5 }).Records;
                Assert.AreEqual(1, recs.Count, $"seed={seed}: exactly one Dismemberment record per call");
                StringAssert.IsMatch("\"outcome\":\"(" + string.Join("|", knownOutcomes) + ")\"", recs[0].PayloadJson);
            }
        }

        [Test]
        public void FullyResistedToZero_DoesNotAlsoEmitApplyDamageRejected()
        {
            var zone = new Zone();
            var target = CreateCreatureWithBody(100);
            target.Statistics["AcidResistance"] = new Stat { Owner = target, Name = "AcidResistance", BaseValue = 100, Min = 0, Max = 200 };
            zone.AddEntity(target, 5, 5);
            var damage = new Damage(20);
            damage.AddAttribute("Acid");

            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);

            var resisted = DiagQuery.Apply(new DiagQuery.Filter { Category = "damage", Kind = "DamageFullyResisted", Limit = 5 }).Records;
            Assert.AreEqual(1, resisted.Count);
            StringAssert.Contains("\"reason\":\"resisted_to_zero\"", resisted[0].PayloadJson);
            var rejected = DiagQuery.Apply(new DiagQuery.Filter { Category = "damage", Kind = "ApplyDamageRejected", Limit = 5 }).Records;
            Assert.AreEqual(0, rejected.Count, "resisted-to-zero and rejected are distinct exit branches, must not conflate");
        }

        [Test]
        public void TwoIndependentAttacks_DiagRecordsCorrectlyScopedPerActorTarget()
        {
            var zone = new Zone();
            var attackerA = CreateCreatureWithBody();
            var defenderA = CreateCreatureWithBody(1);
            zone.AddEntity(attackerA, 5, 5);
            zone.AddEntity(defenderA, 6, 5);
            var attackerB = CreateCreatureWithBody();
            var defenderB = CreateCreatureWithBody(1);
            zone.AddEntity(attackerB, 20, 20);
            zone.AddEntity(defenderB, 21, 20);

            CombatSystem.ApplyDamage(defenderA, new Damage(10), attackerA, zone);
            CombatSystem.ApplyDamage(defenderB, new Damage(10), attackerB, zone);

            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "damage", Kind = "DeathHandled", Limit = 5 }).Records;
            Assert.AreEqual(2, recs.Count);
            bool sawA = false, sawB = false;
            foreach (var r in recs)
            {
                if (r.TargetId == defenderA.ID) sawA = true;
                if (r.TargetId == defenderB.ID) sawB = true;
            }
            Assert.IsTrue(sawA, "defenderA's death record must be present");
            Assert.IsTrue(sawB, "defenderB's death record must be present");
        }

        // ════════════════════════════════════════════════════════════
        //   GROUP E — Save/load reach: multi-effect entity round-trips
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Entity_WithHibernatingAndShatterArmor_BothEffects_RoundTrip()
        {
            // Previously each effect's SL.6.4 round-trip fix (SM1/SM2) was
            // only pinned in isolation. A real save happens with whatever
            // effect combination the character has at that moment.
            var actor = new Entity { ID = "multi-effect-actor", BlueprintName = "Test" };
            actor.ForceApplyEffect(new HibernatingEffect(duration: 8));
            actor.GetEffect<HibernatingEffect>().PriorHeatResistance = 25;
            actor.GetEffect<HibernatingEffect>().PriorColdResistance = -10;
            actor.ForceApplyEffect(new ShatterArmorEffect(duration: 4));
            actor.GetEffect<ShatterArmorEffect>().StackCount = 3;

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);

            var loadedHib = loaded.GetEffect<HibernatingEffect>();
            Assert.IsNotNull(loadedHib);
            Assert.AreEqual(25, loadedHib.PriorHeatResistance);
            Assert.AreEqual(-10, loadedHib.PriorColdResistance);

            var loadedShatter = loaded.GetEffect<ShatterArmorEffect>();
            Assert.IsNotNull(loadedShatter);
            Assert.AreEqual(3, loadedShatter.StackCount);
        }

        [Test]
        public void Entity_WithFungalInfection_RoundTrips_AndOnTurnStartStillFunctionsPostLoad()
        {
            var actor = new Entity { ID = "fungal-actor", BlueprintName = "Test" };
            actor.Statistics["Hitpoints"] = new Stat { Owner = actor, Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30 };
            actor.ForceApplyEffect(new FungalInfectionEffect());
            var live = actor.GetEffect<FungalInfectionEffect>();
            live.TurnsInfected = 12;

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var loadedInfection = loaded.GetEffect<FungalInfectionEffect>();
            Assert.IsNotNull(loadedInfection);
            Assert.AreEqual(12, loadedInfection.TurnsInfected);

            // Must not crash post-load even with no Zone context available.
            Assert.DoesNotThrow(() => loadedInfection.OnTurnStart(loaded, null));
        }

        // ════════════════════════════════════════════════════════════
        //   GROUP F — Parser/malformed boundary inputs
        // ════════════════════════════════════════════════════════════

        [Test]
        public void OnHitEffectFactory_StunnedSpec_NegativeMagnitude_ClampsSaveTargetToZero()
        {
            // SM4's fix used `spec.Magnitude > 0f ? (int)spec.Magnitude : 0`
            // -- only ever exercised with a positive or absent Magnitude.
            // A malformed/negative content value must clamp to 0 (auto-fail
            // save target), not go negative.
            var spec = new OnHitEffectSpec { EffectName = "stunned", Magnitude = -5f, DurationTurns = 3 };
            var effect = OnHitEffectFactory.Create(spec, source: null, rng: new Random(1));
            var stunned = effect as StunnedEffect;
            Assert.IsNotNull(stunned);
            Assert.AreEqual(0, stunned.SaveTarget);
        }

        [Test]
        public void GasFactory_SpawnGas_NullCreator_DoesNotThrow_AndDiagGuardHandlesNullCreator()
        {
            var zone = new Zone();
            Assert.DoesNotThrow(() =>
                GasFactory.SpawnGas(zone, 5, 5, "unregistered-gas-id", creator: null));

            var rejected = DiagQuery.Apply(new DiagQuery.Filter { Category = "gas", Kind = "SpawnRejected", Limit = 5 }).Records;
            Assert.AreEqual(1, rejected.Count);
            Assert.IsNull(rejected[0].ActorId, "null creator must serialize as a null ActorId, not throw");
        }

        [Test]
        public void StunnedEffect_OnStack_RepeatedDeterministicMerge_StaysZero()
        {
            // SM3 fixed the two-different-instances merge; pin the
            // self-merge-repeatedly case too (three merges in a row, all
            // deterministic SaveTarget=0).
            var stunA = new StunnedEffect(duration: 2, saveTarget: 0);
            var stunB = new StunnedEffect(duration: 2, saveTarget: 0);
            var stunC = new StunnedEffect(duration: 2, saveTarget: 0);

            Assert.IsTrue(stunA.OnStack(stunB));
            Assert.AreEqual(0, stunA.SaveTarget);
            Assert.IsTrue(stunA.OnStack(stunC));
            Assert.AreEqual(0, stunA.SaveTarget, "repeated deterministic merges must stay at the sentinel, not drift");
        }

        // ════════════════════════════════════════════════════════════
        //   GROUP G — Cross-system integration (SM7+SM9, SM7+SM10 composing)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void DualWield_TwoDifferentDamageClasses_BothEmitOwnClassEffectRolledRecord()
        {
            // Proves SM7 (unconditional on-hit dispatch) + SM9 (off-hand
            // chance-to-swing) compose: with the off-hand forced to always
            // attack, BOTH weapons' hits each independently roll their own
            // class effect -- not a single shared roll for the "attack" as
            // a whole.
            var zone = new Zone();
            var attacker = CreateCreatureWithBody();
            attacker.Statistics["MultiWeaponSkillBonus"] = new Stat
            { Owner = attacker, Name = "MultiWeaponSkillBonus", BaseValue = 100, Min = -1000, Max = 1000 };
            zone.AddEntity(attacker, 5, 5);
            var hands = GetHands(attacker);
            var inv = attacker.GetPart<InventoryPart>();
            var mace = CreateWeapon("mace", "2d4", attributes: "Bludgeoning", hitBonus: 100);
            var dagger = CreateWeapon("dagger", "1d4", attributes: "Cutting", hitBonus: 100);
            inv.AddObject(mace); inv.AddObject(dagger);
            inv.EquipToBodyPart(mace, hands[0]);
            inv.EquipToBodyPart(dagger, hands[1]);
            var defender = CreateCreatureWithBody(200);
            zone.AddEntity(defender, 6, 5);

            CombatSystem.PerformMeleeAttack(attacker, defender, zone, new Random(7));

            // ClassEffectRolled is emitted under category "damage" (shares
            // OnHitClassEffects.cs's EmitClassEffectRollDiag helper with the
            // rest of the combat diag stream), not "effect".
            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "damage", Kind = "ClassEffectRolled", Limit = 10 }).Records;
            Assert.AreEqual(2, recs.Count,
                "each hand's hit must independently roll its own class effect (2 hits -> 2 records)");
        }

        [Test]
        public void OffHandKillingBlow_StillDispatchesOnHitWeaponEffects()
        {
            // SM7 removed the survivor-gate on weapon-mod dispatch; SM9 made
            // the off-hand a real independent attacker. This proves the two
            // compose: when the OFF-HAND's swing is the one that kills, its
            // own weapon-mod on-hit effects still fire (not just the
            // primary's).
            var zone = new Zone();
            var attacker = CreateCreatureWithBody();
            attacker.Statistics["MultiWeaponSkillBonus"] = new Stat
            { Owner = attacker, Name = "MultiWeaponSkillBonus", BaseValue = 100, Min = -1000, Max = 1000 };
            zone.AddEntity(attacker, 5, 5);
            var hands = GetHands(attacker);
            var inv = attacker.GetPart<InventoryPart>();
            // penBonus: -1000 forces RollPenetrations to 0 every time (bonus
            // deeply negative vs av=0), so the primary "hits" (accuracy roll
            // lands) but deals guaranteed zero damage -- the off-hand must
            // be the one that actually kills.
            var weakPrimary = CreateWeapon("weak_primary", "1d1", hitBonus: 100, penBonus: -1000);
            var lethalOffhand = CreateWeapon("lethal_offhand", "50d1", hitBonus: 100, penBonus: 20); // guaranteed overkill
            inv.AddObject(weakPrimary); inv.AddObject(lethalOffhand);
            inv.EquipToBodyPart(weakPrimary, hands[0]);
            inv.EquipToBodyPart(lethalOffhand, hands[1]);
            var probe = new AlwaysFiresEnhancementProbe();
            lethalOffhand.AddPart(probe);
            var defender = CreateCreatureWithBody(3); // dies to the off-hand, survives the weak primary

            zone.AddEntity(defender, 6, 5);

            CombatSystem.PerformMeleeAttack(attacker, defender, zone, new Random(3));

            Assert.IsTrue(probe.Fired, "the off-hand's killing blow must still dispatch its item enhancement on-hit hook");
        }
    }
}
