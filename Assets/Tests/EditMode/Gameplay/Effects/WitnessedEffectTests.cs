using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// M2.3 — WitnessedEffect lifecycle (OnApply/OnRemove/OnStack) plus
    /// CombatSystem.HandleDeath's BroadcastDeathWitnessed filter
    /// (Passive flag + radius + LOS + deceased/killer exclusion).
    ///
    /// Intentional coverage: every failure mode flagged in the consolidated
    /// M2 plan gets pinned by a dedicated test — snapshot safety is
    /// implicit (HandleDeath iterates GetAllEntities snapshot, and
    /// ApplyEffect inside the loop doesn't bounce back to AddEntity /
    /// RemoveEntity in the default Effect lifecycle).
    /// </summary>
    public class WitnessedEffectTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
        }

        // ========================
        // Effect-only lifecycle
        // ========================

        [Test]
        public void WitnessedEffect_OnApply_PushesWanderDurationGoal()
        {
            var witness = CreatePassiveCreature();

            witness.ApplyEffect(new WitnessedEffect(duration: 20));

            var brain = witness.GetPart<BrainPart>();
            Assert.IsTrue(brain.HasGoal<WanderDurationGoal>(),
                "WitnessedEffect.OnApply must push a WanderDurationGoal onto the brain.");
            var goal = brain.FindGoal<WanderDurationGoal>();
            Assert.AreEqual(20, goal.Duration,
                "Pushed goal must carry the effect's Duration.");
            Assert.AreEqual("shaken", goal.Thought,
                "Pushed goal must carry the 'shaken' thought so the Phase 10 inspector " +
                "reflects why the NPC is pacing. User-reported: WanderDurationGoal without " +
                "a thought showed blank in the inspector during 20-turn pace.");
        }

        [Test]
        public void WitnessedEffect_ShakenThought_WrittenEachTickDuringPace()
        {
            // Regression pin for the user-reported "no thought while pacing"
            // bug. Advancing turns must re-assert the "shaken" thought on
            // each tick so the inspector shows it for the full duration,
            // not just at OnApply time.
            var zone = new Zone("TestZone");
            var witness = CreatePassiveCreature();
            zone.AddEntity(witness, 10, 10);
            var brain = witness.GetPart<BrainPart>();
            brain.CurrentZone = zone;
            brain.Rng = new Random(42);

            witness.ApplyEffect(new WitnessedEffect(duration: 20));

            // Advance one turn — WanderDurationGoal should run TakeAction
            // and Think("shaken").
            witness.FireEvent(GameEvent.New("TakeTurn"));

            Assert.AreEqual("shaken", brain.LastThought,
                "After one TakeTurn tick, the witness's LastThought must be \"shaken\" " +
                "so the Phase 10 inspector reflects their state.");
        }

        [Test]
        public void WitnessedEffect_ShakenThought_ClearsOnGoalPop()
        {
            // Counter-check for the above: when the wander-duration goal
            // finishes and pops, the thought must clear. Otherwise "shaken"
            // would stick in LastThought indefinitely after pacing ends —
            // the same class of sticky-thought bug M5.2 fixed for
            // DisposeOfCorpseGoal.
            var zone = new Zone("TestZone");
            var witness = CreatePassiveCreature();
            zone.AddEntity(witness, 10, 10);
            var brain = witness.GetPart<BrainPart>();
            brain.CurrentZone = zone;
            brain.Rng = new Random(42);

            // Duration=1 so the goal finishes after one TakeAction cycle.
            witness.ApplyEffect(new WitnessedEffect(duration: 1));

            // Tick 1: WanderDurationGoal.TakeAction fires, _ticksTaken=1,
            // Think("shaken"), push WanderRandomlyGoal child which finishes
            // same tick via child-chain.
            witness.FireEvent(GameEvent.New("TakeTurn"));
            Assume.That(brain.LastThought, Is.EqualTo("shaken"),
                "Precondition: tick 1 writes the thought.");

            // Tick 2: cleanup phase sees WanderDurationGoal.Finished()=true
            // (_ticksTaken=1 >= Duration=1), pops it, OnPop calls Think(null).
            // Stack becomes empty → BoredGoal pushed → runs, no Think call.
            witness.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsNull(brain.LastThought,
                "\"shaken\" must clear on WanderDurationGoal.OnPop — sticky past-tense " +
                "thoughts are UX debt (same lesson as DisposeOfCorpseGoal's OnPop fix).");
        }

        [Test]
        public void WanderDurationGoal_WithoutThought_DoesNotOverwriteLastThought()
        {
            // The Thought field is opt-in. A WanderDurationGoal pushed without
            // a thought (e.g., from a future ambient system) must NOT touch
            // LastThought — otherwise it would clobber whatever the outer
            // context was showing.
            var zone = new Zone("TestZone");
            var witness = CreatePassiveCreature();
            zone.AddEntity(witness, 10, 10);
            var brain = witness.GetPart<BrainPart>();
            brain.CurrentZone = zone;
            brain.Rng = new Random(42);
            brain.LastThought = "pre-existing";

            // Push without thought (null default).
            brain.PushGoal(new WanderDurationGoal(duration: 5));

            witness.FireEvent(GameEvent.New("TakeTurn"));

            Assert.AreEqual("pre-existing", brain.LastThought,
                "WanderDurationGoal with Thought=null must NOT overwrite LastThought. " +
                "Otherwise calling it from ambient contexts would clobber the narrative " +
                "signal the outer goal was maintaining.");
        }

        [Test]
        public void WitnessedEffect_OnRemove_RemovesPushedGoal()
        {
            var witness = CreatePassiveCreature();
            witness.ApplyEffect(new WitnessedEffect(duration: 20));
            Assume.That(witness.GetPart<BrainPart>().HasGoal<WanderDurationGoal>());

            // Force removal through the effects part (same path expiry uses).
            bool removed = witness.GetPart<StatusEffectsPart>().RemoveEffect<WitnessedEffect>();

            Assert.IsTrue(removed, "Effect should have been removed.");
            Assert.IsFalse(witness.GetPart<BrainPart>().HasGoal<WanderDurationGoal>(),
                "OnRemove must clean up the pushed WanderDurationGoal.");
        }

        [Test]
        public void WitnessedEffect_OnRemove_SafeIfGoalAlreadyPoppedNaturally()
        {
            // WanderDurationGoal can pop naturally via Finished()+cleanup
            // while WitnessedEffect is still active on the status list —
            // e.g. if BrainPart's cleanup loop ran more often than the
            // Effect's Duration decrement. Simulate that out-of-band pop
            // by removing the goal from the brain directly, then trigger
            // the effect's OnRemove. The effect holds a stale reference
            // to the goal and must handle it without throwing.
            var witness = CreatePassiveCreature();
            witness.ApplyEffect(new WitnessedEffect(duration: 20));

            var brain = witness.GetPart<BrainPart>();
            var goal = brain.FindGoal<WanderDurationGoal>();
            Assert.IsNotNull(goal);

            brain.RemoveGoal(goal); // pre-empts the effect's OnRemove path
            Assert.IsFalse(brain.HasGoal<WanderDurationGoal>());

            // The effect's _pushedGoal reference is now stale. OnRemove
            // should silently no-op rather than throw or re-remove.
            Assert.DoesNotThrow(() =>
                witness.GetPart<StatusEffectsPart>().RemoveEffect<WitnessedEffect>(),
                "OnRemove must tolerate the pushed goal having been removed independently.");
        }

        [Test]
        public void WitnessedEffect_OnApply_IdempotentWithExistingWanderGoal()
        {
            // If a WanderDurationGoal is already on the brain (from any
            // source), OnApply must not push a second one. The effect
            // itself still gets added to the status list (controlled by
            // OnStack only against WitnessedEffect specifically).
            var witness = CreatePassiveCreature();
            var brain = witness.GetPart<BrainPart>();

            // Pre-push an unrelated WanderDurationGoal.
            brain.PushGoal(new WanderDurationGoal(duration: 100));
            int goalCountBefore = brain.GoalCount;

            witness.ApplyEffect(new WitnessedEffect(duration: 20));

            Assert.AreEqual(goalCountBefore, brain.GoalCount,
                "OnApply must NOT push a duplicate WanderDurationGoal when one is already present.");
            var existingGoal = brain.FindGoal<WanderDurationGoal>();
            Assert.AreEqual(100, existingGoal.Duration,
                "Existing WanderDurationGoal must retain its duration — no overwrite.");
        }

        [Test]
        public void WitnessedEffect_OnStack_ExtendsDurationOnLongerIncoming_NoDuplicate()
        {
            // A second witness event (longer duration) should extend the
            // existing effect's remaining duration, NOT add a duplicate to
            // the status list.
            var witness = CreatePassiveCreature();
            var statusPart = witness.GetPart<StatusEffectsPart>();

            witness.ApplyEffect(new WitnessedEffect(duration: 10));
            int effectsAfterFirst = CountEffectsOfType<WitnessedEffect>(witness);

            witness.ApplyEffect(new WitnessedEffect(duration: 30));
            int effectsAfterSecond = CountEffectsOfType<WitnessedEffect>(witness);

            Assert.AreEqual(effectsAfterFirst, effectsAfterSecond,
                "OnStack must return true so the second effect is discarded (no duplicate).");

            var existing = FindFirstEffectOfType<WitnessedEffect>(witness);
            Assert.IsNotNull(existing);
            Assert.AreEqual(30, existing.Duration,
                "Longer incoming duration must extend the existing effect.");
        }

        [Test]
        public void WitnessedEffect_OnStack_ShorterIncomingDoesNotShortenExistingDuration()
        {
            var witness = CreatePassiveCreature();
            witness.ApplyEffect(new WitnessedEffect(duration: 30));
            witness.ApplyEffect(new WitnessedEffect(duration: 5));

            var existing = FindFirstEffectOfType<WitnessedEffect>(witness);
            Assert.AreEqual(30, existing.Duration,
                "A shorter incoming duration must NEVER reduce the existing duration.");
        }

        // ========================
        // CombatSystem.HandleDeath broadcast
        // ========================

        [Test]
        public void HandleDeath_BroadcastsWitness_ToNearbyPassiveNpcs()
        {
            var zone = new Zone();
            var victim  = CreateCombatant();
            var witness = CreatePassiveCreature();
            zone.AddEntity(victim, 10, 5);
            zone.AddEntity(witness, 12, 5); // 2 cells away, clear LOS

            CombatSystem.HandleDeath(victim, killer: null, zone: zone);

            Assert.IsTrue(witness.HasEffect<WitnessedEffect>(),
                "Passive witness within radius + LOS should receive WitnessedEffect on death.");
        }

        [Test]
        public void HandleDeath_DoesNotShakeActiveCombatants()
        {
            var zone = new Zone();
            var victim    = CreateCombatant();
            var combatant = CreateCombatant(); // Passive=false
            zone.AddEntity(victim, 10, 5);
            zone.AddEntity(combatant, 12, 5);

            CombatSystem.HandleDeath(victim, killer: null, zone: zone);

            Assert.IsFalse(combatant.HasEffect<WitnessedEffect>(),
                "Non-Passive NPCs are hardened to violence and must not receive WitnessedEffect.");
        }

        [Test]
        public void HandleDeath_Broadcast_RespectsLineOfSight()
        {
            var zone = new Zone();
            var victim  = CreateCombatant();
            var witness = CreatePassiveCreature();

            // Put a wall between them. Witness at (12,5), victim at (10,5),
            // wall at (11,5) — LOS blocked.
            zone.AddEntity(victim, 10, 5);
            zone.AddEntity(CreateWall(), 11, 5);
            zone.AddEntity(witness, 12, 5);

            CombatSystem.HandleDeath(victim, killer: null, zone: zone);

            Assert.IsFalse(witness.HasEffect<WitnessedEffect>(),
                "A wall between witness and death cell must block the witness effect.");
        }

        [Test]
        public void HandleDeath_Broadcast_RespectsRadius()
        {
            // Radius is 8 cells (Chebyshev). Place witness at 10 cells away —
            // outside the radius — and assert no effect.
            var zone = new Zone();
            var victim  = CreateCombatant();
            var witness = CreatePassiveCreature();
            zone.AddEntity(victim, 5, 5);
            zone.AddEntity(witness, 15, 5); // Chebyshev distance = 10 > radius 8

            CombatSystem.HandleDeath(victim, killer: null, zone: zone);

            Assert.IsFalse(witness.HasEffect<WitnessedEffect>(),
                "Witnesses beyond the 8-cell radius must not receive WitnessedEffect.");
        }

        [Test]
        public void HandleDeath_Broadcast_SkipsDeceasedAndKiller()
        {
            // Contrived: make the killer itself Passive (unusual, but
            // possible if a Passive NPC was attacked and retaliated). The
            // killer must not shake themselves.
            var zone = new Zone();
            var victim = CreateCombatant();
            var passiveKiller = CreatePassiveCreature();
            zone.AddEntity(victim, 10, 5);
            zone.AddEntity(passiveKiller, 11, 5);

            CombatSystem.HandleDeath(victim, killer: passiveKiller, zone: zone);

            Assert.IsFalse(passiveKiller.HasEffect<WitnessedEffect>(),
                "The killer must not receive WitnessedEffect from their own kill.");
            // Also verify deceased wouldn't: it's about to be removed, but
            // the broadcast runs before RemoveEntity. Check it's still not
            // "shaken" when we look it up (effect would have been applied
            // before removal).
            Assert.IsFalse(victim.HasEffect<WitnessedEffect>(),
                "The deceased must not receive WitnessedEffect from their own death.");
        }

        [Test]
        public void HandleDeath_Broadcast_IncludesWitnessAtExactRadius()
        {
            // Boundary test: radius == 8 (Chebyshev). The broadcast filter
            // is `if (dist > radius) continue;`, so exactly 8 must INCLUDE
            // the witness. Pins the boundary so a future flip to `>=`
            // regresses noisily instead of silently.
            var zone = new Zone();
            var victim  = CreateCombatant();
            var witness = CreatePassiveCreature();
            zone.AddEntity(victim, 5, 5);
            zone.AddEntity(witness, 13, 5); // Chebyshev distance exactly 8

            CombatSystem.HandleDeath(victim, killer: null, zone: zone);

            Assert.IsTrue(witness.HasEffect<WitnessedEffect>(),
                "Witness at dist == radius (inclusive boundary) must receive the effect.");
        }

        [Test]
        public void HandleDeath_Broadcast_SkipsCreatureWithoutBrain()
        {
            // A Creature-tagged entity with no BrainPart (e.g. a future
            // plant/corpse/construct) must not throw AND must not receive
            // the effect. The `brain == null` guard inside the broadcast
            // loop handles it — pinning here because the surrounding
            // test `SkipsNonCreatures` tests a different filter (Creature
            // tag, not brain presence).
            var zone = new Zone();
            var victim = CreateCombatant();
            var mindless = CreateBaseCreature("mindless_thing");
            // Intentionally: no BrainPart added.
            Assert.IsNull(mindless.GetPart<BrainPart>(),
                "Precondition: entity has no BrainPart.");
            Assert.IsTrue(mindless.HasTag("Creature"),
                "Precondition: entity IS Creature-tagged so the tag guard doesn't short-circuit the brain guard.");

            zone.AddEntity(victim, 10, 5);
            zone.AddEntity(mindless, 12, 5);

            Assert.DoesNotThrow(() =>
                CombatSystem.HandleDeath(victim, killer: null, zone: zone),
                "Broadcast must tolerate a Creature-tagged entity with no BrainPart.");
            Assert.IsFalse(mindless.HasEffect<WitnessedEffect>(),
                "Brainless Creature must not receive the witness effect.");
        }

        [Test]
        public void WitnessedEffect_OnRemove_PreservesExternallyPushedWanderGoal()
        {
            // Regression: if another system pushed a WanderDurationGoal onto
            // the brain before WitnessedEffect applied, OnApply correctly
            // skipped its own push (_pushedGoal stays null). When the effect
            // later expires, OnRemove must leave the external goal alone —
            // _pushedGoal's null-guard is the mechanism, this test pins it.
            var witness = CreatePassiveCreature();
            var brain = witness.GetPart<BrainPart>();
            var externalGoal = new WanderDurationGoal(duration: 100);
            brain.PushGoal(externalGoal);
            Assert.AreSame(externalGoal, brain.FindGoal<WanderDurationGoal>(),
                "Precondition: external goal is on the brain.");

            witness.ApplyEffect(new WitnessedEffect(duration: 20));
            // Effect is on the status list but _pushedGoal is null because
            // the HasGoal<WanderDurationGoal> guard hit.

            witness.GetPart<StatusEffectsPart>().RemoveEffect<WitnessedEffect>();

            Assert.IsTrue(brain.HasGoal<WanderDurationGoal>(),
                "External WanderDurationGoal must survive the effect's OnRemove.");
            Assert.AreSame(externalGoal, brain.FindGoal<WanderDurationGoal>(),
                "The SAME external goal instance must still be on the stack — " +
                "OnRemove must not have nuked an unrelated goal of the same type.");
        }

        [Test]
        public void HandleDeath_Broadcast_SkipsNonCreatures()
        {
            // An item dropped on the ground (not a Creature) must not be
            // treated as a witness even if in range. Guards the HasTag
            // check inside BroadcastDeathWitnessed.
            var zone = new Zone();
            var victim   = CreateCombatant();
            var itemNear = new Entity { BlueprintName = "dropped_potion" };
            itemNear.AddPart(new RenderPart { DisplayName = "potion" });
            // Intentionally no Creature tag, no BrainPart.

            zone.AddEntity(victim, 10, 5);
            zone.AddEntity(itemNear, 12, 5);

            Assert.DoesNotThrow(() =>
                CombatSystem.HandleDeath(victim, killer: null, zone: zone),
                "Broadcast must not throw when a non-Creature entity is in range.");
        }

        // ========================
        // Helpers
        // ========================

        private static int CountEffectsOfType<T>(Entity e) where T : Effect
        {
            var part = e.GetPart<StatusEffectsPart>();
            if (part == null) return 0;
            int count = 0;
            foreach (var effect in part.GetAllEffects())
                if (effect is T) count++;
            return count;
        }

        private static T FindFirstEffectOfType<T>(Entity e) where T : Effect
        {
            var part = e.GetPart<StatusEffectsPart>();
            if (part == null) return null;
            foreach (var effect in part.GetAllEffects())
                if (effect is T typed) return typed;
            return null;
        }

        private static Entity CreatePassiveCreature()
        {
            var e = CreateBaseCreature("passive_npc");
            var brain = new BrainPart { Passive = true };
            e.AddPart(brain);
            return e;
        }

        private static Entity CreateCombatant()
        {
            var e = CreateBaseCreature("combatant");
            var brain = new BrainPart { Passive = false };
            e.AddPart(brain);
            return e;
        }

        private static Entity CreateBaseCreature(string name)
        {
            var e = new Entity { BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30, Owner = e };
            e.Statistics["Speed"]     = new Stat { Name = "Speed",     BaseValue = 100, Min = 25, Max = 200, Owner = e };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new PhysicsPart { Solid = true });
            return e;
        }

        private static Entity CreateWall()
        {
            var e = new Entity { BlueprintName = "wall" };
            e.Tags["Solid"] = "";
            e.Tags["Wall"] = "";
            e.AddPart(new RenderPart { DisplayName = "wall" });
            e.AddPart(new PhysicsPart { Solid = true });
            return e;
        }
    }
}
