using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// M2 (NoFightGoal-via-dialogue / CalmMutation / WitnessedEffect)
    /// adversarial cold-eye tests. Per QUD-PARITY.md §3.9.
    ///
    /// Companion to M2CoverageGapTests (commit 17665e5, 0/16 bugs found
    /// because tests were written while reading the code). This file
    /// targets behaviors I'm genuinely unsure about, written cold.
    /// </summary>
    [TestFixture]
    public class M2AdversarialTests
    {
        [SetUp]
        public void Setup()
        {
            AsciiFxBus.Clear();
            FactionManager.Initialize();
            MessageLog.Clear();
        }

        [TearDown]
        public void Teardown()
        {
            ConversationManager.EndConversation();
            FactionManager.Reset();
        }

        // ============================================================
        // NoFightGoal — adversarial edges
        // ============================================================

        /// <summary>
        /// PREDICTION: NoFightGoal with Duration=1 finishes after EXACTLY
        /// one tick (Age=1 ≥ Duration=1). One-tick pacification is the
        /// minimal-duration boundary.
        /// CONFIDENCE: high — `Age >= Duration` is the obvious check.
        /// </summary>
        [Test]
        public void NoFightGoal_DurationOne_FinishesAfterOneTick()
        {
            var goal = new NoFightGoal(duration: 1);
            Assert.IsFalse(goal.Finished(), "Brand-new goal not yet expired (Age=0).");

            goal.Age = 1;
            Assert.IsTrue(goal.Finished(),
                "At Age=1, Duration=1, the goal must finish. Off-by-one here " +
                "would make a 1-turn pacification last 2 turns.");
        }

        /// <summary>
        /// PREDICTION: Duration mutated externally mid-flight is respected by
        /// the next Finished() check. The field is public, the check reads it
        /// each call.
        /// CONFIDENCE: medium — this is true if Finished re-reads each call.
        /// If something cached Duration on push, this would fail.
        /// </summary>
        [Test]
        public void NoFightGoal_DurationMutatedExternally_FinishedReflectsChange()
        {
            var goal = new NoFightGoal(duration: 100);
            goal.Age = 50;
            Assert.IsFalse(goal.Finished(), "Halfway through.");

            // Externally shorten the duration.
            goal.Duration = 40;

            Assert.IsTrue(goal.Finished(),
                "After Duration is reduced below Age, Finished must reflect " +
                "the new value. If this fails, Duration is being cached " +
                "somewhere it shouldn't be.");
        }

        /// <summary>
        /// PREDICTION: Wander=true pushes a WanderRandomlyGoal child each
        /// TakeAction tick, even when one already exists on the stack —
        /// because each WanderRandomlyGoal pops itself after one step
        /// (Finished() = true after action). So a fresh one per tick is
        /// the right shape.
        /// CONFIDENCE: low — there might be an idempotency check, or a
        /// double-wander bug.
        /// </summary>
        [Test]
        public void NoFightGoal_Wander_PushesFreshChildEachTick()
        {
            var zone = new Zone("AdvZone");
            var entity = new Entity { BlueprintName = "TestWanderer" };
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = "wanderer" });
            entity.AddPart(new PhysicsPart { Solid = true });
            var brain = new BrainPart { CurrentZone = zone, Rng = new Random(42) };
            entity.AddPart(brain);
            zone.AddEntity(entity, 5, 5);

            var goal = new NoFightGoal(duration: 100, wander: true);
            brain.PushGoal(goal);

            goal.TakeAction();
            int afterFirst = brain.GoalCount;

            goal.TakeAction();
            int afterSecond = brain.GoalCount;

            Assert.GreaterOrEqual(afterFirst, 2,
                "First TakeAction must push WanderRandomlyGoal child.");
            // The exact behavior on second-tick is what I'm probing.
            // If there's already a child, does it push another?
            // Production should NOT double-stack — but I'm uncertain.
            // Whatever the answer, ensure the count doesn't blow up.
            Assert.LessOrEqual(afterSecond, afterFirst + 1,
                "Successive TakeActions on a Wander=true NoFightGoal must not " +
                "stack-leak goals each tick. If goal count grew unbounded, " +
                "we have a leak.");
        }

        // ============================================================
        // CalmMutation — adversarial edges
        // ============================================================

        /// <summary>
        /// PREDICTION: when the line of fire is clear and a target is in
        /// range, only the FIRST creature in line gets pacified — typical
        /// projectile semantics. Targets behind the first do not.
        /// CONFIDENCE: medium. DirectionalProjectileMutationBase is supposed
        /// to be single-target, but I haven't verified it isn't piercing.
        /// </summary>
        [Test]
        public void CalmMutation_TwoTargetsInLine_OnlyFirstIsPacified()
        {
            var zone = new Zone("CalmZone");
            var caster = CreateCalmCaster();
            var t1 = CreateCalmTargetWithBrain();
            var t2 = CreateCalmTargetWithBrain();
            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(t1, 7, 5);
            zone.AddEntity(t2, 9, 5);

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new CalmMutation(), 1);
            var calm = mutations.GetMutation<CalmMutation>();

            calm.Cast(zone, zone.GetCell(5, 5), 1, 0, new Random(42));

            bool t1Pacified = t1.GetPart<BrainPart>().HasGoal<NoFightGoal>();
            bool t2Pacified = t2.GetPart<BrainPart>().HasGoal<NoFightGoal>();

            Assert.IsTrue(t1Pacified, "First target in line should be pacified.");
            Assert.IsFalse(t2Pacified,
                "Second target behind the first must NOT be pacified — " +
                "projectile is single-target by base-class design. If both " +
                "are pacified, the projectile is piercing, which is a real " +
                "design bug for the M2.2 single-target spec.");
        }

        /// <summary>
        /// PREDICTION: CalmMutation cast on a target that just had its
        /// NoFightGoal removed externally (mid-flight, e.g. by EndConversation)
        /// successfully pacifies — the idempotency check sees no goal on the
        /// stack.
        /// CONFIDENCE: high.
        /// </summary>
        [Test]
        public void CalmMutation_AfterPriorNoFightGoalRemoved_PushesNewOne()
        {
            var zone = new Zone("CalmZone");
            var caster = CreateCalmCaster();
            var target = CreateCalmTargetWithBrain();
            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(target, 7, 5);

            var brain = target.GetPart<BrainPart>();
            // Pre-existing goal that gets manually removed mid-game.
            var prior = new NoFightGoal(duration: 30);
            brain.PushGoal(prior);
            brain.RemoveGoal(prior);
            Assume.That(brain.HasGoal<NoFightGoal>(), Is.False,
                "Setup: prior goal should be cleared.");

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new CalmMutation(), 1);
            var calm = mutations.GetMutation<CalmMutation>();

            calm.Cast(zone, zone.GetCell(5, 5), 1, 0, new Random(42));

            Assert.IsTrue(brain.HasGoal<NoFightGoal>(),
                "After a prior NoFightGoal was removed, a fresh Calm cast " +
                "must succeed in pacifying again. Otherwise the player loses " +
                "the ability to re-pacify after manual goal cleanup.");
        }

        /// <summary>
        /// PREDICTION: BaseDuration field can be changed at runtime and
        /// affects the next cast.
        /// CONFIDENCE: medium. The mutation might cache it.
        /// </summary>
        [Test]
        public void CalmMutation_BaseDurationRuntimeChange_AffectsNextCast()
        {
            var zone = new Zone("CalmZone");
            var caster = CreateCalmCaster();
            var target = CreateCalmTargetWithBrain();
            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(target, 7, 5);

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new CalmMutation(), 1);
            var calm = mutations.GetMutation<CalmMutation>();

            // Mutate the base duration before casting.
            calm.BaseDuration = 100;

            calm.Cast(zone, zone.GetCell(5, 5), 1, 0, new Random(42));

            var brain = target.GetPart<BrainPart>();
            var goal = brain.FindGoal<NoFightGoal>();
            Assert.IsNotNull(goal);
            // Level=1 → expected = 100 + 10 = 110
            Assert.AreEqual(110, goal.Duration,
                "Runtime change to BaseDuration must affect the next cast. " +
                "If the value is cached at construction, this test catches it.");
        }

        // ============================================================
        // WitnessedEffect — adversarial edges
        // ============================================================

        /// <summary>
        /// PREDICTION: Duration=0 effect, when applied, immediately marks
        /// for cleanup at the next EndTurn — same as any Duration=0
        /// expirable. The OnApply still fires though (the cleanup happens
        /// on the OWNER's EndTurn).
        /// CONFIDENCE: medium. Duration=0 might be treated as "indefinite"
        /// in some effect base-class; or it might immediately self-expire.
        /// If "indefinite," the test fails — surprising.
        /// </summary>
        [Test]
        public void WitnessedEffect_DurationZero_ExpiresOnFirstEndTurn()
        {
            var zone = new Zone("AdvZone");
            var npc = CreateBrainEntity(zone, 5, 5);

            var effect = new WitnessedEffect(duration: 0);
            npc.ApplyEffect(effect);

            // Some effect-bearing systems treat Duration<=0 as "indefinite"
            // (NoFightGoal does). If WitnessedEffect inherits the same
            // semantics, this test would surprise me.
            Assert.IsTrue(npc.HasEffect<WitnessedEffect>(),
                "Setup: effect should be on the entity right after apply.");

            npc.FireEvent(GameEvent.New("EndTurn"));

            // Honest prediction: Duration=0 means "immediately expirable"
            // for Effect-derived things — different semantic from NoFightGoal
            // (which is a GoalHandler). If the effect is gone, my prediction
            // is right. If still present, Duration=0 is treated as indefinite
            // and that's worth knowing.
            bool stillPresent = npc.HasEffect<WitnessedEffect>();
            // Either outcome is documented behavior, but pin which it is.
            Assert.IsFalse(stillPresent,
                "Predicted: Duration=0 means 'expire on next EndTurn.' If " +
                "this fails, Effect treats Duration=0 as indefinite — note " +
                "the divergence from GoalHandler.Finished() semantics for " +
                "future readers.");
        }

        /// <summary>
        /// PREDICTION: OnStack is called with a same-type WitnessedEffect.
        /// If the incoming has a SHORTER duration, the existing duration is
        /// preserved (max-merge). The existing duration is NOT shortened.
        /// CONFIDENCE: high — already covered by an existing test, but I'm
        /// going to also verify the behavior across multiple stacks.
        /// </summary>
        [Test]
        public void WitnessedEffect_StackingMonotonic_DurationOnlyGrows()
        {
            var zone = new Zone("AdvZone");
            var npc = CreateBrainEntity(zone, 5, 5);

            npc.ApplyEffect(new WitnessedEffect(duration: 10));
            var existing = npc.GetEffect<WitnessedEffect>();

            // Series of incoming durations: 5, 30, 8, 50.
            npc.ApplyEffect(new WitnessedEffect(duration: 5));
            Assert.AreEqual(10, existing.Duration, "Shorter (5) must not shorten.");

            npc.ApplyEffect(new WitnessedEffect(duration: 30));
            Assert.AreEqual(30, existing.Duration, "Longer (30) must extend.");

            npc.ApplyEffect(new WitnessedEffect(duration: 8));
            Assert.AreEqual(30, existing.Duration, "Shorter (8) after stack must not shorten.");

            npc.ApplyEffect(new WitnessedEffect(duration: 50));
            Assert.AreEqual(50, existing.Duration, "Longer (50) after stack must extend.");
        }

        /// <summary>
        /// PREDICTION: applying WitnessedEffect to a target with NO BrainPart
        /// is a no-op, NOT a crash. The OnApply guards on brain.
        /// CONFIDENCE: high.
        /// </summary>
        [Test]
        public void WitnessedEffect_OnApplyToBrainlessEntity_DoesNotCrash()
        {
            var entity = new Entity { BlueprintName = "Statue" };
            entity.AddPart(new RenderPart { DisplayName = "statue" });

            Assert.DoesNotThrow(() => entity.ApplyEffect(new WitnessedEffect(20)),
                "Applying WitnessedEffect to a brainless entity must not crash.");
        }

        /// <summary>
        /// PREDICTION: OnStack with an INCOMING that is NOT a WitnessedEffect
        /// (different type) — well, this should never happen because
        /// StatusEffectsPart only stacks within the same Type. So this code
        /// path is unreachable in production. But the code does
        /// `if (incoming is WitnessedEffect w && ...)` which gracefully
        /// handles it — return true to signal handled, no mutation.
        /// CONFIDENCE: medium — pin the defensive behavior.
        /// </summary>
        [Test]
        public void WitnessedEffect_OnStackWithDifferentType_ReturnsTrueNoMutation()
        {
            var existing = new WitnessedEffect(duration: 20);
            var wrongType = new StunnedEffect(5);

            // Direct call to OnStack with wrong type.
            bool handled = existing.OnStack(wrongType);

            Assert.IsTrue(handled,
                "OnStack must return true (handled) even on type-mismatch — " +
                "discarding the incoming is safer than letting it flow through " +
                "to be added to the effects list.");
            Assert.AreEqual(20, existing.Duration,
                "Mismatched-type OnStack must not mutate Duration.");
        }

        // ============================================================
        // Helpers
        // ============================================================

        private static Entity CreateCalmCaster()
        {
            var entity = CreateCalmCreatureBase("caster", hp: 20);
            entity.AddPart(new ActivatedAbilitiesPart());
            entity.AddPart(new MutationsPart());
            return entity;
        }

        private static Entity CreateCalmTargetWithBrain()
        {
            var entity = CreateCalmCreatureBase("target", hp: 20);
            entity.Tags["Faction"] = "Monsters";
            entity.AddPart(new BrainPart());
            return entity;
        }

        private static Entity CreateCalmCreatureBase(string name, int hp)
        {
            var entity = new Entity { BlueprintName = name };
            entity.Tags["Creature"] = "";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            entity.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 18, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = 18, Min = 1, Max = 50 };
            entity.Statistics["Toughness"] = new Stat { Name = "Toughness", BaseValue = 18, Min = 1, Max = 50 };
            entity.Statistics["Ego"] = new Stat { Name = "Ego", BaseValue = 10, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = name });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new MeleeWeaponPart { BaseDamage = "1d2" });
            entity.AddPart(new ArmorPart());
            entity.AddPart(new InventoryPart { MaxWeight = 150 });
            return entity;
        }

        private static Entity CreateBrainEntity(Zone zone, int x, int y)
        {
            var entity = new Entity { BlueprintName = "TestNpc" };
            entity.Tags["Creature"] = "";
            entity.Tags["Faction"] = "Villagers";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = "npc" });
            entity.AddPart(new PhysicsPart { Solid = true });
            var brain = new BrainPart { CurrentZone = zone, Rng = new Random(42) };
            entity.AddPart(brain);
            zone.AddEntity(entity, x, y);
            return entity;
        }
    }
}
