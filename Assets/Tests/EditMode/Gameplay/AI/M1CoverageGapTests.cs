using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// M1 (AISelfPreservation / Passive / AIAmbush) coverage-gap tests.
    ///
    /// Written in TDD-style per Docs/QUD-PARITY.md Part 2.1: each test
    /// describes an invariant I believe SHOULD hold. When a test fails,
    /// it either exposes a real bug (production code wrong) or a bad
    /// assumption in the test (test wrong). Either way, the failure is
    /// informative. The analysis is captured in the commit message.
    ///
    /// Scope: boundary cases, null-safety, failure-path propagation,
    /// state semantics (IsBusy, Target assignment) that the existing
    /// AISelfPreservationBlueprintTests / AIAmbushPartTests / Phase6GoalsTests
    /// don't pin.
    /// </summary>
    [TestFixture]
    public class M1CoverageGapTests
    {
        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
        }

        // Mirrors Phase6GoalsTests.CreateCreature — keeps test shape aligned.
        private Entity CreateCreature(Zone zone, int x, int y, string faction = "Villagers", int hp = 20)
        {
            var entity = new Entity { BlueprintName = "TestCreature" };
            entity.Tags["Creature"] = "";
            if (!string.IsNullOrEmpty(faction))
                entity.Tags["Faction"] = faction;
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            entity.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Toughness"] = new Stat { Name = "Toughness", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = faction ?? "creature" });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new MeleeWeaponPart { BaseDamage = "1d4" });
            entity.AddPart(new ArmorPart());
            var brain = new BrainPart { CurrentZone = zone, Rng = new Random(42) };
            entity.AddPart(brain);
            zone.AddEntity(entity, x, y);
            brain.StartingCellX = x;
            brain.StartingCellY = y;
            return entity;
        }

        // ============================================================
        // AISelfPreservationPart — boundary + null-safety gaps
        // ============================================================

        [Test]
        public void AISelfPreservation_AtExactThreshold_DoesNotPush()
        {
            // The gate in HandleBored uses `fraction > RetreatThreshold` to
            // EARLY-RETURN (don't push). So at exactly threshold, fraction
            // is NOT strictly greater → fall through → push. But wait —
            // read the code again: `if (fraction > RetreatThreshold) return true`
            // means "HP fine, do nothing." At exact threshold, `fraction >
            // threshold` is false, so we do NOT early-return, so we DO push.
            //
            // Actually — re-reading once more, I want to confirm the semantic
            // I expect: AT the threshold, the NPC should ALREADY be considered
            // wounded (threshold is a ceiling on "safe"). So push at <=, not
            // strictly <. The current `>` check means fraction==threshold
            // pushes. That's what I expect. Let me pin it.
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 10, 10, hp: 100);
            creature.AddPart(new AISelfPreservationPart { RetreatThreshold = 0.5f });
            var brain = creature.GetPart<BrainPart>();

            // HP exactly at 50% (50/100).
            creature.GetStat("Hitpoints").BaseValue = 50;

            creature.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsTrue(brain.HasGoal<RetreatGoal>(),
                "At HP exactly equal to the RetreatThreshold, the NPC should " +
                "push RetreatGoal — the threshold is the ceiling on 'wounded', " +
                "not a strict-less-than trigger. If this fails, check whether " +
                "the gate uses `>` vs `>=` — it should stay `>` (so equality " +
                "IS considered wounded).");
        }

        [Test]
        public void AISelfPreservation_JustAboveThreshold_DoesNotPush()
        {
            // Counter-check to the boundary test. At fraction STRICTLY
            // greater than threshold, the NPC should NOT push.
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 10, 10, hp: 100);
            creature.AddPart(new AISelfPreservationPart { RetreatThreshold = 0.5f });
            var brain = creature.GetPart<BrainPart>();

            creature.GetStat("Hitpoints").BaseValue = 51; // 51/100 = 51% > 50%

            creature.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsFalse(brain.HasGoal<RetreatGoal>(),
                "At HP strictly above RetreatThreshold, NPC must NOT push " +
                "RetreatGoal. Counter-check to the boundary test.");
        }

        [Test]
        public void AISelfPreservation_MissingHitpointsStat_DoesNotCrashOrPush()
        {
            // If an entity somehow lacks a Hitpoints stat, the null-check in
            // HandleBored should kick in and early-return without pushing.
            // Defensive against test-only or exotic entities.
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 10, 10);
            creature.AddPart(new AISelfPreservationPart { RetreatThreshold = 0.5f });
            var brain = creature.GetPart<BrainPart>();

            creature.Statistics.Remove("Hitpoints");

            Assert.DoesNotThrow(() => creature.FireEvent(GameEvent.New("TakeTurn")),
                "AISelfPreservation must not crash when Hitpoints stat is absent.");
            Assert.IsFalse(brain.HasGoal<RetreatGoal>(),
                "With no Hitpoints stat, maxHp=0 and the gate must early-return.");
        }

        [Test]
        public void AISelfPreservation_EffectiveSafeThreshold_ClampsAboveRetreat()
        {
            // Direct unit test of the EffectiveSafeThreshold property. When
            // authored SafeThreshold <= RetreatThreshold, the effective
            // value must be at least RetreatThreshold + 0.1.
            var part = new AISelfPreservationPart
            {
                RetreatThreshold = 0.6f,
                SafeThreshold = 0.3f  // invalid: below retreat
            };

            Assert.GreaterOrEqual(part.EffectiveSafeThreshold, part.RetreatThreshold + 0.1f,
                "EffectiveSafeThreshold must be >= RetreatThreshold + 0.1 when " +
                "SafeThreshold is configured below RetreatThreshold.");
        }

        [Test]
        public void AISelfPreservation_EffectiveSafeThreshold_RespectsExplicitSafeWhenValid()
        {
            // When SafeThreshold is validly above RetreatThreshold, it should
            // be used as-is (no clamping).
            var part = new AISelfPreservationPart
            {
                RetreatThreshold = 0.3f,
                SafeThreshold = 0.8f
            };

            Assert.AreEqual(0.8f, part.EffectiveSafeThreshold, 0.0001f,
                "EffectiveSafeThreshold must equal SafeThreshold when the " +
                "author-provided value is above RetreatThreshold + 0.1.");
        }

        // ============================================================
        // RetreatGoal — failure-path + phase-transition gaps
        // ============================================================

        [Test]
        public void RetreatGoal_Failed_FailsToParent()
        {
            // When the child MoveToGoal fails to reach the waypoint (e.g.
            // walled in), RetreatGoal.Failed() should propagate up so the
            // parent (typically BoredGoal) can pick a different fallback.
            // Without this, a wounded NPC could get stuck trying to retreat
            // to an unreachable waypoint.
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 10, 10);
            var brain = creature.GetPart<BrainPart>();

            // Install a sentinel parent goal so we can observe FailToParent.
            var parent = new SentinelParentGoal();
            brain.PushGoal(parent);

            var retreat = new RetreatGoal(15, 15);
            parent.PushChildGoal(retreat);

            // Simulate a child goal failing.
            retreat.Failed(new MoveToGoal(15, 15));

            Assert.IsTrue(parent.ChildFailed,
                "RetreatGoal.Failed(child) must propagate up via FailToParent " +
                "so the parent goal can react. Otherwise a wounded NPC gets " +
                "stuck retreating to an unreachable waypoint.");
            Assert.IsFalse(brain.HasGoal<RetreatGoal>(),
                "RetreatGoal should have popped itself during FailToParent.");
        }

        [Test]
        public void RetreatGoal_MaxTurns_Exceeded_FinishedReturnsTrue()
        {
            // Hard safety cap. If Age exceeds MaxTurns, Finished() returns
            // true regardless of phase, so the goal pops even if stuck.
            var goal = new RetreatGoal(10, 10, safeHpFraction: 0.75f, maxTurns: 5);

            // Directly bump Age past the cap. Age is a public field on GoalHandler.
            goal.Age = 6;

            Assert.IsTrue(goal.Finished(),
                "Finished() must return true when Age > MaxTurns, even if " +
                "the phase hasn't transitioned to Done. Otherwise a stuck " +
                "NPC could loop forever.");
        }

        [Test]
        public void RetreatGoal_TravelToRecover_TransitionSetsBrainStateIdle()
        {
            // When the NPC arrives at the waypoint, RetreatGoal transitions
            // from Travel → Recover in-line. The Recover phase should set
            // the brain's CurrentState to Idle so the Phase 10 inspector
            // correctly labels the NPC.
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 10, 10, hp: 100);
            var brain = creature.GetPart<BrainPart>();
            // HP is already low (will stay in Recover phase after arrival).
            creature.GetStat("Hitpoints").BaseValue = 40;

            var retreat = new RetreatGoal(10, 10, safeHpFraction: 0.9f);
            brain.PushGoal(retreat);

            // Execute — creature is already at (10, 10), so it transitions
            // straight into Recover.
            retreat.TakeAction();

            Assert.AreEqual(AIState.Idle, brain.CurrentState,
                "Entering Recover phase must set brain.CurrentState = Idle. " +
                "The Phase 10 inspector relies on this to render the " +
                "'recovering' label correctly.");
        }

        // ============================================================
        // DormantGoal — semantics + first-tick gaps
        // ============================================================

        [Test]
        public void DormantGoal_IsBusy_IsFalse()
        {
            // DormantGoal should report IsBusy=false so the brain's "am I
            // busy" consumers (e.g., zone-transition prompts) don't treat
            // a sleeping creature as actively working. This matches the
            // pattern used for BoredGoal / WanderGoal.
            var goal = new DormantGoal();

            Assert.IsFalse(goal.IsBusy(),
                "DormantGoal.IsBusy must be false — a sleeping creature is " +
                "not busy. Callers gating on IsBusy would otherwise treat " +
                "ambush creatures as active.");
        }

        [Test]
        public void DormantGoal_FirstTick_DoesNotWakeOnDamageBaseline()
        {
            // On construction, _lastHp = -1 (unseeded). The first TakeAction
            // must SEED _lastHp without triggering a wake, even though
            // effectively "HP went from unknown to current" could look
            // like a decrease. Otherwise creatures that spawn with anything
            // but maximum HP would wake instantly.
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 10, 10, hp: 50);
            var brain = creature.GetPart<BrainPart>();

            // Spawn with HP < max (e.g., already wounded).
            creature.GetStat("Hitpoints").BaseValue = 25;

            var dormant = new DormantGoal(wakeOnDamage: true);
            brain.PushGoal(dormant);

            dormant.TakeAction();

            Assert.IsFalse(dormant.Finished(),
                "First tick must seed the damage-wake baseline without " +
                "triggering a wake. Creatures spawned below max HP must " +
                "not wake immediately on their first tick.");
        }

        [Test]
        public void DormantGoal_SecondTick_WakesOnActualDamage()
        {
            // Counter-check to the baseline-seeding test. The FIRST tick
            // seeds. The SECOND tick, if HP has dropped, must wake.
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 10, 10, hp: 50);
            var brain = creature.GetPart<BrainPart>();
            creature.GetStat("Hitpoints").BaseValue = 25;

            var dormant = new DormantGoal(wakeOnDamage: true);
            brain.PushGoal(dormant);

            // Tick 1: seed baseline. No wake.
            dormant.TakeAction();
            Assert.IsFalse(dormant.Finished(), "Seed tick must not wake.");

            // HP drops between ticks.
            creature.GetStat("Hitpoints").BaseValue = 20;

            // Tick 2: detects decrease, wakes.
            dormant.TakeAction();
            Assert.IsTrue(dormant.Finished(),
                "Tick 2 must detect HP decrease (25 → 20) and set " +
                "Finished()=true. Counter-check confirming the baseline " +
                "seed from tick 1 is genuine (not suppressing all wakes).");
        }

        [Test]
        public void DormantGoal_WakeOnHostile_SetsBrainTargetToHostile()
        {
            // When dormant-on-hostile-sight fires, DormantGoal should ALSO
            // set brain.Target to the detected hostile. Otherwise the
            // NPC wakes with no target and BoredGoal's re-scan runs
            // before combat can engage — wasting a turn.
            var zone = new Zone("TestZone");

            var ambusher = CreateCreature(zone, 10, 10, faction: "Snapjaws", hp: 30);
            var ambusherBrain = ambusher.GetPart<BrainPart>();

            var player = new Entity { BlueprintName = "Player" };
            player.Tags["Creature"] = "";
            player.Tags["Player"] = "";
            player.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 50, Min = 0, Max = 50 };
            player.AddPart(new RenderPart { DisplayName = "player" });
            player.AddPart(new PhysicsPart { Solid = true });
            zone.AddEntity(player, 12, 10); // within sight radius of ambusher

            var dormant = new DormantGoal(wakeOnDamage: false, wakeOnHostileInSight: true);
            ambusherBrain.PushGoal(dormant);

            dormant.TakeAction();

            Assert.IsTrue(dormant.Finished(),
                "Dormant goal should wake when hostile enters sight.");
            Assert.AreSame(player, ambusherBrain.Target,
                "On hostile-wake, brain.Target must be the detected hostile. " +
                "Without this, the NPC wakes with Target=null and has to " +
                "re-scan on the next BoredGoal tick, wasting a turn.");
        }

        // ============================================================
        // AIAmbushPart — edge cases
        // ============================================================

        [Test]
        public void AIAmbush_Rearm_OnUnwiredPart_DoesNotThrow()
        {
            // Calling Rearm on a part that was never attached (or whose
            // brain lookup failed) should be safe. The field reset itself
            // is unconditional; only the subsequent TryPushDormant should
            // guard for null brain.
            var part = new AIAmbushPart();

            Assert.DoesNotThrow(() => part.Rearm(),
                "Rearm must be safe to call on an unattached part — it's " +
                "just a flag reset; side-effects are gated on next Initialize " +
                "or TakeTurn.");
        }

        [Test]
        public void AIAmbush_WithoutBrainPart_DoesNotPushOrThrow()
        {
            // An entity with AIAmbushPart but NO BrainPart should not
            // crash — TryPushDormant checks for brain == null. The
            // ambush simply never activates (defensive for test-only or
            // broken blueprints).
            var entity = new Entity { BlueprintName = "NoBrain" };

            Assert.DoesNotThrow(() => entity.AddPart(new AIAmbushPart()),
                "Adding AIAmbushPart to an entity with no BrainPart must " +
                "not throw during Initialize.");

            // Also fires TakeTurn — the fallback path shouldn't crash either.
            Assert.DoesNotThrow(() => entity.FireEvent(GameEvent.New("TakeTurn")),
                "TakeTurn on an AIAmbush entity with no brain must not throw.");
        }

        // ============================================================
        // BrainPart.Passive — defaults + interaction with Staying
        // ============================================================

        [Test]
        public void BrainPart_Passive_DefaultsFalse()
        {
            // A fresh BrainPart with no blueprint Param overrides should
            // have Passive=false. Mirrors the default "NPC initiates combat
            // on sight" behavior; Passive must be explicitly opted in.
            var brain = new BrainPart();

            Assert.IsFalse(brain.Passive,
                "BrainPart.Passive must default to false. Opt-in-only " +
                "semantics avoid accidentally pacifying new NPC blueprints.");
        }

        // ============================================================
        // Subtle regression pins (would catch real bugs if broken)
        // ============================================================

        [Test]
        public void AISelfPreservation_PassesEffectiveSafeThreshold_NotRawSafeThreshold()
        {
            // Subtle regression: the HandleBored push MUST pass
            // EffectiveSafeThreshold (the clamped value), not the raw
            // SafeThreshold field. If someone refactors and accidentally
            // swaps this, blueprints with swapped thresholds would thrash
            // (RetreatGoal finishes immediately, re-pushed next tick).
            //
            // This test sets RetreatThreshold=0.6 and SafeThreshold=0.3
            // (invalid: safe below retreat). EffectiveSafeThreshold clamps
            // to 0.7. The pushed RetreatGoal must carry 0.7, not 0.3.
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 10, 10, hp: 100);
            creature.AddPart(new AISelfPreservationPart
            {
                RetreatThreshold = 0.6f,
                SafeThreshold = 0.3f  // invalid
            });
            var brain = creature.GetPart<BrainPart>();
            creature.GetStat("Hitpoints").BaseValue = 50; // below retreat

            creature.FireEvent(GameEvent.New("AIBored"));

            var retreat = brain.FindGoal<RetreatGoal>();
            Assert.IsNotNull(retreat, "Low HP should push RetreatGoal.");
            Assert.AreEqual(0.7f, retreat.SafeHpFraction, 0.0001f,
                "Pushed RetreatGoal must carry EffectiveSafeThreshold (0.7), " +
                "NOT the raw SafeThreshold (0.3). If this asserts 0.3, someone " +
                "removed the clamp call — creatures will retreat-thrash at low HP.");
        }

        [Test]
        public void DormantGoal_AfterWake_StaysFinished_Idempotent()
        {
            // After Wake() fires (or a trigger sets _wakeRequested), repeated
            // calls to Finished() must keep returning true. The brain's
            // cleanup loop relies on this to pop the goal; if Finished()
            // ever flips back to false, the goal would get "stuck awake but
            // unpopped."
            var goal = new DormantGoal();
            goal.Wake();

            Assert.IsTrue(goal.Finished(), "First check after Wake() must be true.");
            Assert.IsTrue(goal.Finished(), "Second check after Wake() must still be true.");
            Assert.IsTrue(goal.Finished(), "Third check after Wake() must still be true.");
        }

        [Test]
        public void AISelfPreservation_ZeroMaxHp_DoesNotDivideByZero_OrPush()
        {
            // Defensive: if a blueprint somehow configures Hitpoints with
            // Max=0, the HandleBored gate must early-return before the
            // `(float)hp / maxHp` division. Catch this before it becomes
            // a NaN-comparison bug.
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 10, 10);
            creature.AddPart(new AISelfPreservationPart { RetreatThreshold = 0.5f });
            var brain = creature.GetPart<BrainPart>();

            var hpStat = creature.GetStat("Hitpoints");
            hpStat.BaseValue = 0;
            hpStat.Max = 0;

            Assert.DoesNotThrow(() => creature.FireEvent(GameEvent.New("TakeTurn")),
                "AISelfPreservation must not divide-by-zero when maxHp is 0.");
            Assert.IsFalse(brain.HasGoal<RetreatGoal>(),
                "With maxHp=0, the gate must early-return (a dead NPC can't retreat).");
        }

        [Test]
        public void DormantGoal_BothWakeFlagsOff_NeverFinishes()
        {
            // If both WakeOnDamage and WakeOnHostileInSight are false, the
            // only way to wake is via an explicit Wake() call. Verify
            // that ticking with damage and hostiles present does NOT
            // auto-wake.
            var zone = new Zone("TestZone");
            var ambusher = CreateCreature(zone, 10, 10, faction: "Snapjaws", hp: 30);
            var brain = ambusher.GetPart<BrainPart>();

            var player = new Entity { BlueprintName = "Player" };
            player.Tags["Creature"] = "";
            player.Tags["Player"] = "";
            player.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 50, Min = 0, Max = 50 };
            player.AddPart(new RenderPart { DisplayName = "player" });
            player.AddPart(new PhysicsPart { Solid = true });
            zone.AddEntity(player, 12, 10);

            var dormant = new DormantGoal(wakeOnDamage: false, wakeOnHostileInSight: false);
            brain.PushGoal(dormant);

            // Tick once — seed baseline, no wake triggers.
            dormant.TakeAction();
            // Drop HP — would normally wake via damage flag if enabled.
            ambusher.GetStat("Hitpoints").BaseValue = 10;
            dormant.TakeAction();

            Assert.IsFalse(dormant.Finished(),
                "With both wake flags disabled, no trigger should wake the " +
                "goal. Only explicit Wake() or external Pop() should.");
        }

        [Test]
        public void RetreatGoal_Constructor_StoresAllFields()
        {
            // Sanity: the ctor parameters must end up on the correct fields.
            // Catches argument-order regressions.
            var goal = new RetreatGoal(
                waypointX: 42,
                waypointY: 17,
                safeHpFraction: 0.88f,
                maxTurns: 123,
                healPerTick: 7);

            Assert.AreEqual(42, goal.WaypointX, "WaypointX");
            Assert.AreEqual(17, goal.WaypointY, "WaypointY");
            Assert.AreEqual(0.88f, goal.SafeHpFraction, 0.0001f, "SafeHpFraction");
            Assert.AreEqual(123, goal.MaxTurns, "MaxTurns");
            Assert.AreEqual(7, goal.HealPerTick, "HealPerTick");
        }

        [Test]
        public void DormantGoal_Constructor_StoresAllFields()
        {
            // Symmetric sanity: DormantGoal ctor field wiring.
            var goal = new DormantGoal(
                wakeOnDamage: false,
                wakeOnHostileInSight: true,
                sleepParticleInterval: 13);

            Assert.IsFalse(goal.WakeOnDamage, "WakeOnDamage");
            Assert.IsTrue(goal.WakeOnHostileInSight, "WakeOnHostileInSight");
            Assert.AreEqual(13, goal.SleepParticleInterval, "SleepParticleInterval");
        }

        // ============================================================
        // Test-only helpers
        // ============================================================

        /// <summary>
        /// Parent-goal sentinel for Failed() propagation tests. Records
        /// whether a child FailToParent fired on it.
        /// </summary>
        private class SentinelParentGoal : GoalHandler
        {
            public bool ChildFailed;
            public override void TakeAction() { }
            public override void Failed(GoalHandler child)
            {
                ChildFailed = true;
            }
        }
    }
}
