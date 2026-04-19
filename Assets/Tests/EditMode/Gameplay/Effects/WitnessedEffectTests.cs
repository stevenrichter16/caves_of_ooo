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
