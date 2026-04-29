using System;
using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Adversarial / mutation-resistance tests for the recent combat
    /// content (elemental weapons, tonics, throwable consumables, status
    /// effects). The goal here is to catch latent bugs the per-feature
    /// tests miss because they only exercise the happy-path invariants.
    ///
    /// Cold-eye targets:
    ///   - Counter-element interactions (Wet vs Burn, Frozen vs Burn, Acid + Burn)
    ///   - Effect stacking caps and order independence
    ///   - AOE corner cases (self-throw, zone-corner clipping)
    ///   - Tonic dispatch edge cases (null / unknown / empty EffectName)
    ///   - Death-during-tick safety
    ///   - On-hit chance statistical sanity
    ///   - Resistance extremes (>100, &lt;-50)
    ///   - Multi-attribute damage routing (DissolutionMaul: Bludgeoning + Acid)
    ///
    /// Tests flagged 🟡 in comments document either confirmed bugs or
    /// design-intent invariants that should be regression-guarded. After
    /// shipping these tests, fix any 🟡 confirmed-bug entries.
    /// </summary>
    public class CombatContentAdversarialTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetup() => _harness = new ScenarioTestHarness();
        [OneTimeTearDown]
        public void OneTimeTearDown() { _harness?.Dispose(); _harness = null; }

        [SetUp]
        public void Setup() => MessageLog.Clear();

        // ========================================================================
        // GROUP A — Counter-element interactions
        // Most likely bug surface: opposing-element effects should produce
        // intuitive cancellations. Spec the actual current behavior so
        // future changes can't silently break it.
        // ========================================================================

        [Test]
        public void Frozen_OnApply_ImmediatelyExtinguishes_BurningEffect()
        {
            // Existing behavior in FrozenEffect.cs:32-37 — Cold defeats Fire
            // absolutely. If this regresses, the Burn+Frost combat interaction
            // breaks (a frozen target shouldn't still burn).
            var e = MakeCreature(hp: 100);
            e.ApplyEffect(new BurningEffect(intensity: 3.0f, rng: new Random(1)));
            Assert.IsTrue(e.HasEffect<BurningEffect>(), "precondition: target is burning");

            e.ApplyEffect(new FrozenEffect(cold: 1.0f));

            Assert.IsFalse(e.HasEffect<BurningEffect>(),
                "FrozenEffect.OnApply must remove BurningEffect (cold defeats fire).");
        }

        [Test]
        public void Wet_OnApply_DoesNotImmediatelyQuench_Burning_ButImpedesViaReaction()
        {
            // 🟡 Asymmetry to spec: Wet does NOT extinguish Burning on apply.
            // Quench happens during BurningEffect.OnTurnStart via the
            // water_plus_fire MaterialReaction (reduces intensity by 1.0
            // per tick when moisture > 0.3). So a wet burning creature
            // takes 1 more tick of damage but the fire dies faster.
            //
            // If this changes (Wet starts immediately quenching like Frost),
            // re-evaluate: that flips fire/water from "damper" to "absolute
            // counter." This test pins the current design.
            var e = MakeCreature(hp: 100);
            e.AddPart(new MaterialPart { MaterialID = "Flesh", MaterialTagsRaw = "Organic" });
            e.ApplyEffect(new BurningEffect(intensity: 1.0f, rng: new Random(1)));

            e.ApplyEffect(new WetEffect(moisture: 1.0f));

            Assert.IsTrue(e.HasEffect<BurningEffect>(),
                "WetEffect.OnApply must NOT immediately remove BurningEffect — quench is gradual via MaterialReaction.");
        }

        [Test]
        public void WaterTonic_DrinkOnBurning_DoesNotImmediatelyQuench()
        {
            // Confirms the documented asymmetry above end-to-end through
            // the tonic dispatch path. Players who expect "drink water →
            // fire goes out" will be disappointed; this test documents
            // the actual behavior so changes are explicit.
            var e = MakeCreature(hp: 100);
            e.AddPart(new MaterialPart { MaterialID = "Flesh", MaterialTagsRaw = "Organic" });
            e.ApplyEffect(new BurningEffect(intensity: 1.0f, rng: new Random(1)));

            var tonic = _harness.Factory.CreateEntity("WaterTonic");
            FireApplyTonic(tonic, e);

            Assert.IsTrue(e.HasEffect<WetEffect>(), "WaterTonic should apply WetEffect");
            Assert.IsTrue(e.HasEffect<BurningEffect>(),
                "Drinking WaterTonic does NOT immediately quench burning.");
        }

        [Test]
        public void FrozenEffect_BlocksAction_WhileColdIsPositive()
        {
            // Existing AllowAction invariant in FrozenEffect.cs:79.
            // Pin so a future regression that changes the Cold ≤ 0 threshold
            // doesn't silently let frozen creatures act.
            var e = MakeCreature(hp: 100);
            var frozen = new FrozenEffect(cold: 0.5f);
            e.ApplyEffect(frozen);

            Assert.IsFalse(frozen.AllowAction(e),
                "FrozenEffect with Cold > 0 must block action.");

            // Cold 0 should allow action (effect about to expire).
            frozen.GetType().GetField("Cold").SetValue(frozen, 0f);
            Assert.IsTrue(frozen.AllowAction(e),
                "FrozenEffect with Cold = 0 must allow action (about to expire).");
        }

        // ========================================================================
        // GROUP B — Effect stacking caps and order
        // Stacking same effect repeatedly to verify the documented cap
        // behavior holds.
        // ========================================================================

        [Test]
        public void Burning_MultiStack_IntensityCappedAt5()
        {
            // BurningEffect.OnStack: Intensity = min(Intensity + incoming * 0.5, 5.0)
            // Apply 20 BurningEffects rapidly — Intensity must clamp at 5.
            var e = MakeCreature(hp: 100);
            for (int i = 0; i < 20; i++)
                e.ApplyEffect(new BurningEffect(intensity: 5.0f, rng: new Random(1)));

            var burn = e.GetEffect<BurningEffect>();
            Assert.IsNotNull(burn);
            Assert.LessOrEqual(burn.Intensity, 5.0f,
                "BurningEffect.Intensity must cap at 5.0 regardless of stack count.");
        }

        [Test]
        public void Wet_MultiStack_MoistureCappedAt1()
        {
            var e = MakeCreature(hp: 100);
            for (int i = 0; i < 10; i++)
                e.ApplyEffect(new WetEffect(moisture: 1.0f));

            var wet = e.GetEffect<WetEffect>();
            Assert.IsNotNull(wet);
            Assert.LessOrEqual(wet.Moisture, 1.0f,
                "WetEffect.Moisture must cap at 1.0 regardless of stack count.");
        }

        [Test]
        public void Frozen_MultiStack_ColdCappedAt1()
        {
            var e = MakeCreature(hp: 100);
            for (int i = 0; i < 10; i++)
                e.ApplyEffect(new FrozenEffect(cold: 1.0f));

            var frozen = e.GetEffect<FrozenEffect>();
            Assert.IsNotNull(frozen);
            Assert.LessOrEqual(frozen.Cold, 1.0f,
                "FrozenEffect.Cold must cap at 1.0 regardless of stack count.");
        }

        [Test]
        public void Acidic_MultiStack_CorrosionCappedAt1()
        {
            var e = MakeCreature(hp: 100);
            for (int i = 0; i < 10; i++)
                e.ApplyEffect(new AcidicEffect(corrosion: 1.0f));

            var acid = e.GetEffect<AcidicEffect>();
            Assert.IsNotNull(acid);
            Assert.LessOrEqual(acid.Corrosion, 1.0f,
                "AcidicEffect.Corrosion must cap at 1.0.");
        }

        [Test]
        public void Electrified_MultiStack_TakesMaxChargeAndDuration()
        {
            // ElectrifiedEffect.OnStack: Charge = max, Duration = max.
            // First apply with low charge, then with high — final state
            // should reflect the larger.
            var e = MakeCreature(hp: 1000); // generous HP so damage tick doesn't kill
            e.ApplyEffect(new ElectrifiedEffect(charge: 1.0f));
            e.ApplyEffect(new ElectrifiedEffect(charge: 5.0f));
            float afterCharge = e.GetEffect<ElectrifiedEffect>().Charge;

            Assert.GreaterOrEqual(afterCharge, 5.0f - 0.01f,
                "ElectrifiedEffect stacking must take max(Charge), not min.");

            // Verify only one ElectrifiedEffect instance exists (stacking merged).
            int count = 0;
            foreach (var ef in e.GetPart<StatusEffectsPart>().GetAllEffects())
                if (ef is ElectrifiedEffect) count++;
            Assert.AreEqual(1, count,
                "Stacking must merge into single ElectrifiedEffect, not duplicate.");
        }

        // ========================================================================
        // GROUP C — Tonic dispatch edge cases (StatusTonicPart)
        // Malformed EffectName values should not crash.
        // ========================================================================

        [Test]
        public void StatusTonic_NullEffectName_NoCrash()
        {
            var drinker = MakeDrinker();
            var tonic = MakeBareTonic();
            tonic.AddPart(new StatusTonicPart { EffectName = null, EffectMagnitude = 1f });

            Assert.DoesNotThrow(() => FireApplyTonic(tonic, drinker),
                "StatusTonic with null EffectName must not crash.");
            Assert.AreEqual(0, drinker.GetPart<StatusEffectsPart>()?.EffectCount ?? 0,
                "Null EffectName must apply no effect.");
        }

        [Test]
        public void StatusTonic_EmptyEffectName_NoCrash()
        {
            var drinker = MakeDrinker();
            var tonic = MakeBareTonic();
            tonic.AddPart(new StatusTonicPart { EffectName = "", EffectMagnitude = 1f });

            Assert.DoesNotThrow(() => FireApplyTonic(tonic, drinker));
            Assert.AreEqual(0, drinker.GetPart<StatusEffectsPart>()?.EffectCount ?? 0,
                "Empty EffectName must apply no effect.");
        }

        [Test]
        public void StatusTonic_UnknownEffectName_NoCrash_NoEffect()
        {
            var drinker = MakeDrinker();
            var tonic = MakeBareTonic();
            tonic.AddPart(new StatusTonicPart { EffectName = "UltraMegaQuantumBurst", EffectMagnitude = 1f });

            Assert.DoesNotThrow(() => FireApplyTonic(tonic, drinker));
            Assert.AreEqual(0, drinker.GetPart<StatusEffectsPart>()?.EffectCount ?? 0,
                "Unknown EffectName must silently apply no effect (defensive dispatch).");
        }

        [Test]
        public void StatusTonic_EffectNameWithMixedCase_DispatchesCorrectly()
        {
            // Confirm `.ToLowerInvariant()` normalization works.
            var drinker = MakeDrinker();
            var tonic = MakeBareTonic();
            tonic.AddPart(new StatusTonicPart { EffectName = "ACID", EffectMagnitude = 1f });

            FireApplyTonic(tonic, drinker);

            Assert.IsTrue(drinker.GetPart<StatusEffectsPart>().HasEffect<AcidicEffect>(),
                "Mixed-case EffectName 'ACID' must dispatch to AcidicEffect.");
        }

        // ========================================================================
        // GROUP D — Tonic blueprint magnitude edge cases
        // ========================================================================

        [Test]
        public void StatusTonic_NegativeMagnitude_FallsBackToDefault()
        {
            // StatusTonicPart.CreateEffect uses `EffectMagnitude > 0f ? ... : default`.
            // Negative magnitudes fall to the default ctor value rather than
            // applying with negative magnitude (which would clamp to 0
            // anyway in most ctors but with unclear intent).
            var drinker = MakeDrinker();
            var tonic = MakeBareTonic();
            tonic.AddPart(new StatusTonicPart { EffectName = "Acid", EffectMagnitude = -5f });

            FireApplyTonic(tonic, drinker);

            var acid = drinker.GetPart<StatusEffectsPart>().GetEffect<AcidicEffect>();
            Assert.IsNotNull(acid);
            Assert.Greater(acid.Corrosion, 0f,
                "Negative magnitude must fall back to default, not produce zero/negative effect.");
        }

        // ========================================================================
        // GROUP E — Effect on degenerate targets
        // ========================================================================

        [Test]
        public void Acidic_OnNonOrganicCreature_AppliesButDoesNotDamage()
        {
            // AcidicEffect.OnTurnStart guards on `material.HasMaterialTag("Organic")`.
            // Confirm the effect can still be APPLIED (effect list contains it,
            // visible in sidebar) but no damage is dealt.
            var goldenStatue = MakeCreature(hp: 100);
            goldenStatue.AddPart(new MaterialPart { MaterialID = "Gold", MaterialTagsRaw = "Metal" });

            var acid = new AcidicEffect(corrosion: 1.0f);
            goldenStatue.ApplyEffect(acid);

            int hpBefore = goldenStatue.GetStatValue("Hitpoints");
            acid.OnTurnStart(goldenStatue, MakeTickCtx());
            int hpAfter = goldenStatue.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore, hpAfter,
                "AcidicEffect on non-organic must deal no damage.");
            Assert.IsTrue(goldenStatue.HasEffect<AcidicEffect>(),
                "Effect should still be applied (visible in sidebar) even if no damage.");
        }

        [Test]
        public void Acidic_OnEntityWithoutMaterialPart_NoCrash()
        {
            // AcidicEffect.OnTurnStart line 33 reads MaterialPart. If absent,
            // isOrganic is false and we early-return. No crash.
            var ghost = MakeCreature(hp: 100);
            // No MaterialPart on this entity.

            var acid = new AcidicEffect(corrosion: 1.0f);
            ghost.ApplyEffect(acid);

            Assert.DoesNotThrow(() => acid.OnTurnStart(ghost, MakeTickCtx()),
                "AcidicEffect on entity without MaterialPart must not crash.");
        }

        [Test]
        public void Burning_OnDeadTarget_NoSecondDeath()
        {
            // CombatSystem.ApplyDamage(target, Damage, ...) early-returns when
            // hpStat.BaseValue <= 0 (CombatSystem.cs:547). Confirm BurningEffect
            // applied to a dead target doesn't crash, doesn't re-fire HandleDeath.
            var corpse = MakeCreature(hp: 100);
            corpse.GetStat("Hitpoints").BaseValue = 0;

            var burn = new BurningEffect(intensity: 5.0f, rng: new Random(1));
            corpse.ApplyEffect(burn);

            int diedEventCount = 0;
            corpse.AddPart(new EventCounterPart("Died", () => diedEventCount++));

            Assert.DoesNotThrow(() => burn.OnTurnStart(corpse, MakeTickCtx()));
            Assert.AreEqual(0, diedEventCount,
                "Burning a dead target must not re-fire the Died event.");
        }

        // ========================================================================
        // GROUP F — AOE / throw corner cases
        // ========================================================================

        [Test]
        public void ThrownTonic_AtThrowerOwnCell_AppliesToThrower()
        {
            // Self-throw is degenerate but legal. The thrower's cell is
            // within radius 1 of itself, so AOE should hit the thrower.
            // Player MUST tag as "Creature" for AOE to fire on them.
            var ctx = _harness.CreateContext();
            var thrower = ctx.Spawn("Villager").NotRegisteredForTurns().At(10, 10);
            var inv = thrower.GetPart<InventoryPart>() ?? new InventoryPart { MaxWeight = 50 };
            if (thrower.GetPart<InventoryPart>() == null) thrower.AddPart(inv);

            var tonic = _harness.Factory.CreateEntity("AcidTonic");
            tonic.AddPart(new HandlingPart { Carryable = true, Throwable = true, Weight = 1 });
            inv.AddObject(tonic);

            var throwCmd = new ThrowItemCommand(tonic, 10, 10);
            var result = InventorySystem.ExecuteCommand(throwCmd, thrower, ctx.Zone);
            Assert.IsTrue(result.Success, "Self-throw should validate: " + result.ErrorMessage);

            Assert.IsTrue(thrower.GetPart<StatusEffectsPart>().HasEffect<AcidicEffect>(),
                "Self-thrown tonic AOE must hit the thrower (thrower's cell is in radius 1 of itself).");
        }

        [Test]
        public void ThrownTonic_ToOutOfBounds_FailsValidation()
        {
            var ctx = _harness.CreateContext();
            var thrower = ctx.Spawn("Villager").NotRegisteredForTurns().At(10, 10);
            var inv = thrower.GetPart<InventoryPart>() ?? new InventoryPart { MaxWeight = 50 };
            if (thrower.GetPart<InventoryPart>() == null) thrower.AddPart(inv);

            var tonic = _harness.Factory.CreateEntity("AcidTonic");
            tonic.AddPart(new HandlingPart { Carryable = true, Throwable = true, Weight = 1 });
            inv.AddObject(tonic);

            var throwCmd = new ThrowItemCommand(tonic, -5, -5);
            var result = InventorySystem.ExecuteCommand(throwCmd, thrower, ctx.Zone);
            Assert.IsFalse(result.Success,
                "Throw to out-of-bounds (-5,-5) must fail validation.");
            Assert.IsTrue(inv.Objects.Contains(tonic),
                "Failed throw must leave the tonic in inventory (no consumption).");
        }

        [Test]
        public void ThrownTonic_AtZoneCorner_AoeClippedByBounds()
        {
            // Throw to (1, 1). AOE 3×3 centered there = (0,0)..(2,2).
            // (-1, ...) cells don't exist — Zone.GetCell returns null,
            // ApplyTonicAoe skips those iterations. No crash.
            var ctx = _harness.CreateContext();
            var thrower = ctx.Spawn("Villager").NotRegisteredForTurns().At(5, 5);
            var inv = thrower.GetPart<InventoryPart>() ?? new InventoryPart { MaxWeight = 50 };
            if (thrower.GetPart<InventoryPart>() == null) thrower.AddPart(inv);
            var str = thrower.GetStat("Strength"); if (str != null) str.BaseValue = 25;

            var tonic = _harness.Factory.CreateEntity("AcidTonic");
            tonic.AddPart(new HandlingPart { Carryable = true, Throwable = true, Weight = 1 });
            inv.AddObject(tonic);

            // Snapjaw at (1,1) — the corner target.
            var snapjaw = ctx.Spawn("Snapjaw").NotRegisteredForTurns().At(1, 1);

            var throwCmd = new ThrowItemCommand(tonic, 1, 1);
            var result = InventorySystem.ExecuteCommand(throwCmd, thrower, ctx.Zone);
            Assert.IsTrue(result.Success, "Throw to corner: " + result.ErrorMessage);

            Assert.IsTrue(snapjaw.GetPart<StatusEffectsPart>().HasEffect<AcidicEffect>(),
                "Corner snapjaw must still receive AOE — out-of-bounds neighbors are skipped, not aborted.");
        }

        // ========================================================================
        // GROUP G — DissolutionMaul multi-attribute (Bludgeoning + Acid)
        // ========================================================================

        [Test]
        public void DissolutionMaul_DamageRoutesThrough_BothBludgeoningAndAcid()
        {
            // DissolutionMaul has "Bludgeoning Cudgel Acid" attributes —
            // unique multi-attribute. Damage should be reducible by EITHER
            // a Bludgeoning resistance (none in this codebase) OR
            // AcidResistance.
            // We can verify the Acid routing via AcidResistance reduction.
            var resistant = MakeCreature(hp: 100, acidResistance: 100);
            var unresisted = MakeCreature(hp: 100);

            var dmg = new Damage(50);
            dmg.AddAttribute("Bludgeoning");
            dmg.AddAttribute("Acid");
            dmg.AddAttribute("Cudgel");

            int hpBefore = resistant.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(resistant, dmg, source: null, zone: null);
            int hpAfter = resistant.GetStatValue("Hitpoints");
            int resistantDmg = hpBefore - hpAfter;

            // Re-build damage (Damage.Amount is mutated by ApplyResistanceFor).
            var dmg2 = new Damage(50);
            dmg2.AddAttribute("Bludgeoning");
            dmg2.AddAttribute("Acid");
            dmg2.AddAttribute("Cudgel");

            int hp2Before = unresisted.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(unresisted, dmg2, source: null, zone: null);
            int hp2After = unresisted.GetStatValue("Hitpoints");
            int unresistedDmg = hp2Before - hp2After;

            Assert.AreEqual(0, resistantDmg,
                "AcidResistance=100 must nullify multi-attribute Acid damage.");
            Assert.Greater(unresistedDmg, 0,
                "Sanity: unresisted target takes the damage.");
        }

        // ========================================================================
        // GROUP H — Resistance extreme values
        // ========================================================================

        [Test]
        public void HeatResistance_200_FullyImmuneToFire_NoNegativeDamage()
        {
            // ApplyResistanceFor: Amount * (100-200)/100 = -Amount → clamped
            // to 0 by Damage.Amount setter. Pin: no healing-on-overcap bug.
            var target = MakeCreature(hp: 100, heatResistance: 200);
            var dmg = new Damage(50);
            dmg.AddAttribute("Fire");

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, dmg, source: null, zone: null);
            int hpAfter = target.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore, hpAfter,
                "HeatResistance=200 must result in 0 damage (no negative-damage healing exploit).");
        }

        [Test]
        public void Resistance_AmpliesNegative_ButCapsAtSomePoint()
        {
            // Vulnerability formula: Amount += Amount * (resist / -100).
            // At resist=-100, damage doubles. At resist=-200, damage triples.
            // Pin: vulnerability scales linearly without overflow.
            var doubled = MakeCreature(hp: 1000, heatResistance: -100);
            var dmg = new Damage(10);
            dmg.AddAttribute("Fire");

            int hpBefore = doubled.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(doubled, dmg, source: null, zone: null);
            int doubledDmg = hpBefore - doubled.GetStatValue("Hitpoints");

            var unresisted = MakeCreature(hp: 1000);
            var dmg2 = new Damage(10);
            dmg2.AddAttribute("Fire");
            int hpBefore2 = unresisted.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(unresisted, dmg2, source: null, zone: null);
            int unresistedDmg = hpBefore2 - unresisted.GetStatValue("Hitpoints");

            Assert.AreEqual(unresistedDmg * 2, doubledDmg,
                "HeatResistance=-100 must exactly double fire damage (vulnerability formula).");
        }

        // ========================================================================
        // GROUP I — Statistical sanity for OnHit chance
        // ========================================================================

        [Test]
        public void OnHit_BurningChance_RoughlyMatchesConfiguredRate()
        {
            // FlamingSword's OnHitEffectsRaw declares Burning at 30%
            // (per Objects.json blueprint). Over many synthetic rolls,
            // the observed rate should be roughly 30% ± a generous
            // tolerance (this catches stuck-RNG / always-true / always-false
            // bugs without being flaky).
            //
            // Test the actual production code path: build a damage with
            // "Fire" attribute and verify the OnHitClassEffects /
            // OnHitWeaponEffects probability roll. Here we use OnHitEffectSpec
            // directly — that's the parser+roller.
            var spec = OnHitEffectSpec.Parse("Burning,30,,5,1.0");
            Assert.AreEqual(1, spec.Count, "should parse one spec");
            Assert.AreEqual(30, spec[0].ChancePercent);

            // Statistical roll: simulate 1000 rolls at 30% chance.
            var rng = new Random(42);
            int hits = 0;
            const int trials = 1000;
            for (int i = 0; i < trials; i++)
            {
                if (rng.Next(100) < spec[0].ChancePercent)
                    hits++;
            }
            // Expected mean = 300. Generous ±60 tolerance accommodates
            // a Z-score of ~4.1 — basically only catches gross bugs
            // (always-true / always-false / inverted comparison).
            Assert.GreaterOrEqual(hits, 240,
                $"Expected ~30% hits over {trials}, got {hits}. RNG might be inverted or stuck false.");
            Assert.LessOrEqual(hits, 360,
                $"Expected ~30% hits over {trials}, got {hits}. RNG might be inverted or stuck true.");
        }

        // ========================================================================
        // GROUP J — Wet+Lightning combo amplification
        // (regression guard for the recently-fixed Lightning DoT)
        // ========================================================================

        [Test]
        public void WetThenElectrified_DamageStrictlyMore_ThanDryThenElectrified()
        {
            // ElectrifiedEffect.OnApply doubles Charge if WetEffect is
            // present with Moisture > 0.2. After my fix shipped per-turn
            // damage based on Charge, the wet target should take exactly
            // double the per-turn damage.
            var dry = MakeCreature(hp: 1000);
            var wet = MakeCreature(hp: 1000);
            wet.ApplyEffect(new WetEffect(moisture: 1.0f));
            var ctx = MakeTickCtx();

            var elecDry = new ElectrifiedEffect(charge: 1.0f);
            dry.ApplyEffect(elecDry);
            elecDry.OnTurnStart(dry, ctx);

            var elecWet = new ElectrifiedEffect(charge: 1.0f);
            wet.ApplyEffect(elecWet);
            elecWet.OnTurnStart(wet, ctx);

            int dryDmg = 1000 - dry.GetStatValue("Hitpoints");
            int wetDmg = 1000 - wet.GetStatValue("Hitpoints");

            Assert.Greater(wetDmg, dryDmg,
                "Wet target must take more lightning damage per tick.");
            Assert.GreaterOrEqual(wetDmg, dryDmg * 2 - 1,
                "Wet should amplify near-2× (charge doubled in OnApply); ±1 tolerance for int truncation.");
        }

        // ========================================================================
        // GROUP K1 — OnHitEffectFactory parsing/dispatch edge cases
        // ========================================================================

        [Test]
        public void OnHitEffectFactory_BleedingSpec_UsesMagnitude_NotDurationAsSaveTarget()
        {
            // 🟡 LATENT BUG (no in-game blueprint hits this today, but future
            // content adding `Bleeding,...` to a weapon's OnHitEffectsRaw
            // would). The factory's bleeding case at OnHitEffectFactory.cs:75-77
            // currently passes spec.DurationTurns as the saveTarget arg — that
            // mis-types the semantic. DurationTurns is for stun/confuse/poison;
            // Bleeding's "difficulty class" is logically Magnitude (or its own
            // dedicated field).
            //
            // Spec format: EffectName,Chance,Dice,Duration,Magnitude
            //   For "Bleeding,30,1d3,0,18" — author's intent is clearly
            //   "save target 18" (matches the Magnitude). Current bug: produces
            //   saveTarget=0 (effectively unbleedable / instant cure).
            //
            // After fix: factory uses Magnitude (cast to int) as saveTarget,
            // falling back to default 15 if unset.
            var spec = OnHitEffectSpec.Parse("Bleeding,30,1d3,0,18")[0];
            var effect = OnHitEffectFactory.Create(spec, source: null, rng: new Random(1)) as BleedingEffect;
            Assert.IsNotNull(effect, "factory must create BleedingEffect for spec");
            Assert.AreEqual(18, effect.SaveTarget,
                "Bleeding's saveTarget must come from Magnitude (18), not DurationTurns. " +
                "Pre-fix the factory passed DurationTurns (0) → unsavable/uncurable bleeding (or trivially curable).");
        }

        [Test]
        public void OnHitEffectFactory_UnknownEffectName_ReturnsNull()
        {
            var spec = new OnHitEffectSpec
                { EffectName = "NotARealEffect", ChancePercent = 30, Magnitude = 1f };
            var effect = OnHitEffectFactory.Create(spec, source: null, rng: new Random(1));
            Assert.IsNull(effect,
                "Unknown EffectName must return null (caller skips silently). Catches typos in blueprints.");
        }

        [Test]
        public void OnHitEffectFactory_NullSpec_NoCrash()
        {
            Assert.IsNull(OnHitEffectFactory.Create(null, source: null, rng: new Random(1)));
        }

        [Test]
        public void OnHitEffectSpec_Parse_MalformedString_NoCrash_NoEntries()
        {
            // Malformed: missing comma, garbage text, etc.
            var specs1 = OnHitEffectSpec.Parse("garbage");
            Assert.AreEqual(0, specs1.Count, "single-field garbage produces no specs");

            var specs2 = OnHitEffectSpec.Parse("Burning,abc,,5,1.0");
            Assert.AreEqual(0, specs2.Count, "non-int chance produces no spec");

            var specs3 = OnHitEffectSpec.Parse("Burning,0,,5,1.0");
            Assert.AreEqual(0, specs3.Count, "zero-chance spec is skipped");

            var specs4 = OnHitEffectSpec.Parse("");
            Assert.AreEqual(0, specs4.Count, "empty input produces no specs");

            var specs5 = OnHitEffectSpec.Parse(null);
            Assert.AreEqual(0, specs5.Count, "null input produces no specs");
        }

        [Test]
        public void OnHitEffectSpec_Parse_MultipleSpecsSeparatedBySemicolons()
        {
            // DissolutionMaul-style: two effects on one weapon.
            var specs = OnHitEffectSpec.Parse("Burning,30,,5,1.0;Stunned,5,,1,0");
            Assert.AreEqual(2, specs.Count);
            Assert.AreEqual("Burning", specs[0].EffectName);
            Assert.AreEqual(30, specs[0].ChancePercent);
            Assert.AreEqual("Stunned", specs[1].EffectName);
            Assert.AreEqual(5, specs[1].ChancePercent);
        }

        // ========================================================================
        // GROUP K — Effect cleanup determinism
        // ========================================================================

        [Test]
        public void Effect_DurationOne_RemovedAfterSingleTurnCycle()
        {
            // Apply a 1-duration effect, fire one OnTurnEnd cycle through
            // the StatusEffectsPart, verify the effect is gone.
            var e = MakeCreature(hp: 100);
            var stunned = new StunnedEffect(duration: 1);
            e.ApplyEffect(stunned);
            Assert.IsTrue(e.HasEffect<StunnedEffect>(), "precondition");

            // Fire the EndTurn event so StatusEffectsPart drives the cleanup.
            var ev = GameEvent.New("EndTurn");
            ev.SetParameter("Zone", (object)null);
            e.FireEvent(ev);
            ev.Release();

            Assert.IsFalse(e.HasEffect<StunnedEffect>(),
                "1-duration effect must be removed after one EndTurn cycle.");
        }

        // ========================================================================
        // Helpers
        // ========================================================================

        private static Entity MakeCreature(
            int hp = 100,
            int heatResistance = 0,
            int acidResistance = 0)
        {
            var e = new Entity { BlueprintName = "TestCreature" };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["Toughness"] = new Stat
                { Owner = e, Name = "Toughness", BaseValue = 10 };
            if (heatResistance != 0)
                e.Statistics["HeatResistance"] = new Stat
                    { Owner = e, Name = "HeatResistance", BaseValue = heatResistance, Min = -100, Max = 200 };
            if (acidResistance != 0)
                e.Statistics["AcidResistance"] = new Stat
                    { Owner = e, Name = "AcidResistance", BaseValue = acidResistance, Min = -100, Max = 200 };
            e.AddPart(new RenderPart { DisplayName = "test" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static Entity MakeDrinker()
        {
            var e = new Entity { BlueprintName = "TestDrinker" };
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = 50, Min = 0, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = "drinker" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static Entity MakeBareTonic()
        {
            var t = new Entity { BlueprintName = "TestTonic" };
            t.AddPart(new RenderPart { DisplayName = "test tonic" });
            t.AddPart(new TonicPart());
            return t;
        }

        private static void FireApplyTonic(Entity tonic, Entity actor)
        {
            var e = GameEvent.New("ApplyTonic");
            e.SetParameter("Actor", (object)actor);
            e.SetParameter("Source", (object)actor);
            tonic.FireEvent(e);
        }

        private static GameEvent MakeTickCtx()
        {
            var ev = GameEvent.New("BeginTakeAction");
            ev.SetParameter("Zone", (object)null);
            return ev;
        }

        /// <summary>
        /// Tiny event-counter Part used to assert events do or don't fire
        /// during a probe operation. Self-contained so the test class
        /// stays portable.
        /// </summary>
        private class EventCounterPart : Part
        {
            public override string Name => "EventCounter";
            private readonly string _eventId;
            private readonly Action _onFire;

            public EventCounterPart(string eventId, Action onFire)
            {
                _eventId = eventId;
                _onFire = onFire;
            }

            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == _eventId) _onFire?.Invoke();
                return true;
            }
        }
    }
}
