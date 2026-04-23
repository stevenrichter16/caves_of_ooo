using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    [TestFixture]
    public class IdleQueryTests
    {
        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
        }

        private Entity CreateCreature(string faction, int hp = 15)
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
            return entity;
        }

        private Entity CreatePlayer(int hp = 20)
        {
            var entity = CreateCreature(null, hp);
            entity.Tags["Player"] = "";
            return entity;
        }

        private Entity CreateChair(string owner = "")
        {
            var chair = new Entity { BlueprintName = "Chair" };
            chair.AddPart(new RenderPart { RenderString = "h", ColorString = "&w", RenderLayer = 5 });
            chair.AddPart(new ChairPart { Owner = owner });
            return chair;
        }

        private Entity CreateBed(string owner = "")
        {
            var bed = new Entity { BlueprintName = "Bed" };
            bed.AddPart(new RenderPart { RenderString = "=", ColorString = "&W", RenderLayer = 5 });
            bed.AddPart(new BedPart { Owner = owner });
            return bed;
        }

        // ========================
        // ChairPart IdleQuery
        // ========================

        [Test]
        public void ChairPart_RespondsToIdleQuery()
        {
            var zone = new Zone("TestZone");
            var chair = CreateChair();
            zone.AddEntity(chair, 5, 5);

            var creature = CreateCreature("Snapjaws");
            creature.Tags["AllowIdleBehavior"] = "";
            var brain = new BrainPart { CurrentZone = zone, Rng = new Random(42) };
            creature.AddPart(brain);
            zone.AddEntity(creature, 3, 5);

            var offer = IdleQueryEvent.QueryOffer(chair, creature);

            Assert.IsNotNull(offer, "Chair should offer idle behavior");
            Assert.AreEqual(5, offer.TargetX);
            Assert.AreEqual(5, offer.TargetY);
            Assert.IsNotNull(offer.Action);
            Assert.IsNotNull(offer.Cleanup, "Offer should include a cleanup callback");
            // Successful query reserves the chair synchronously (prevents double-booking)
            Assert.IsTrue(chair.GetPart<ChairPart>().Occupied,
                "Chair should be marked Occupied at query time");
        }

        [Test]
        public void ChairPart_OwnerFiltering_RejectsWrongNPC()
        {
            var zone = new Zone("TestZone");
            var chair = CreateChair(owner: "Innkeeper");
            zone.AddEntity(chair, 5, 5);

            var creature = CreateCreature("Snapjaws");
            creature.Tags["AllowIdleBehavior"] = "";
            var brain = new BrainPart { CurrentZone = zone, Rng = new Random(42) };
            creature.AddPart(brain);
            zone.AddEntity(creature, 3, 5);

            var offer = IdleQueryEvent.QueryOffer(chair, creature);

            Assert.IsNull(offer, "Chair should reject NPC without matching Owner tag");
        }

        [Test]
        public void ChairPart_OwnerFiltering_AcceptsMatchingTag()
        {
            var zone = new Zone("TestZone");
            var chair = CreateChair(owner: "Innkeeper");
            zone.AddEntity(chair, 5, 5);

            var creature = CreateCreature("Snapjaws");
            creature.Tags["AllowIdleBehavior"] = "";
            creature.Tags["Innkeeper"] = "";
            var brain = new BrainPart { CurrentZone = zone, Rng = new Random(42) };
            creature.AddPart(brain);
            zone.AddEntity(creature, 3, 5);

            var offer = IdleQueryEvent.QueryOffer(chair, creature);

            Assert.IsNotNull(offer, "Chair should accept NPC with matching tag");
        }

        [Test]
        public void ChairPart_OccupiedRejects()
        {
            var zone = new Zone("TestZone");
            var chair = CreateChair();
            chair.GetPart<ChairPart>().Occupied = true;
            zone.AddEntity(chair, 5, 5);

            var creature = CreateCreature("Snapjaws");
            creature.Tags["AllowIdleBehavior"] = "";
            var brain = new BrainPart { CurrentZone = zone, Rng = new Random(42) };
            creature.AddPart(brain);
            zone.AddEntity(creature, 3, 5);

            var offer = IdleQueryEvent.QueryOffer(chair, creature);

            Assert.IsNull(offer, "Occupied chair should reject query");
        }

        [Test]
        public void ChairPart_RequiresAllowIdleBehavior()
        {
            var zone = new Zone("TestZone");
            var chair = CreateChair();
            zone.AddEntity(chair, 5, 5);

            var creature = CreateCreature("Snapjaws");
            // No AllowIdleBehavior tag
            var brain = new BrainPart { CurrentZone = zone, Rng = new Random(42) };
            creature.AddPart(brain);
            zone.AddEntity(creature, 3, 5);

            var offer = IdleQueryEvent.QueryOffer(chair, creature);

            Assert.IsNull(offer, "Should require AllowIdleBehavior tag");
        }

        // ========================
        // DelegateGoal
        // ========================

        [Test]
        public void DelegateGoal_ExecutesCallback()
        {
            bool called = false;
            var goal = new DelegateGoal(g => { called = true; });
            var brain = new BrainPart { Rng = new Random(42) };
            brain.PushGoal(goal);

            Assert.IsFalse(called);
            goal.TakeAction();
            Assert.IsTrue(called);
            Assert.IsTrue(goal.Finished());
        }

        // ========================
        // SittingEffect
        // ========================

        [Test]
        public void SittingEffect_SittingNPC_StaysSeated_WhenNoHostile()
        {
            var zone = new Zone("TestZone");
            var creature = CreateCreature("Snapjaws");
            creature.Tags["AllowIdleBehavior"] = "";
            var brain = new BrainPart
            {
                Wanders = true,
                WandersRandomly = true,
                CurrentZone = zone,
                Rng = new Random(42)
            };
            creature.AddPart(brain);
            creature.AddPart(new StatusEffectsPart());
            zone.AddEntity(creature, 10, 10);

            // Apply sitting effect
            creature.ApplyEffect(new SittingEffect());

            creature.FireEvent(GameEvent.New("TakeTurn"));

            // Should NOT have moved
            var pos = zone.GetEntityPosition(creature);
            Assert.AreEqual(10, pos.x);
            Assert.AreEqual(10, pos.y);
            Assert.AreEqual(AIState.Idle, brain.CurrentState);
        }

        [Test]
        public void SittingEffect_RemovedOnHostile()
        {
            var zone = new Zone("TestZone");
            var creature = CreateCreature("Snapjaws");
            creature.Tags["AllowIdleBehavior"] = "";
            var brain = new BrainPart
            {
                SightRadius = 10,
                CurrentZone = zone,
                Rng = new Random(42)
            };
            creature.AddPart(brain);
            creature.AddPart(new StatusEffectsPart());
            zone.AddEntity(creature, 5, 5);

            var chair = CreateChair();
            zone.AddEntity(chair, 5, 5);
            creature.ApplyEffect(new SittingEffect(chair));

            var player = CreatePlayer(hp: 50);
            zone.AddEntity(player, 8, 5);

            creature.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsFalse(creature.HasEffect<SittingEffect>(), "Sitting should be removed on hostile");
            Assert.AreEqual(AIState.Chase, brain.CurrentState);
        }

        [Test]
        public void SittingEffect_OnRemove_ClearsOccupied()
        {
            var zone = new Zone("TestZone");
            var chair = CreateChair();
            zone.AddEntity(chair, 5, 5);
            var chairPart = chair.GetPart<ChairPart>();
            chairPart.Occupied = true;

            var creature = CreateCreature("Snapjaws");
            creature.AddPart(new StatusEffectsPart());
            zone.AddEntity(creature, 5, 5);

            var effect = new SittingEffect(chair);
            creature.ApplyEffect(effect);
            Assert.IsTrue(chairPart.Occupied);

            creature.RemoveEffect<SittingEffect>();
            Assert.IsFalse(chairPart.Occupied, "Removing SittingEffect should clear Occupied");
        }

        // ========================================================
        // Sit-forever regression — AIBored fires while seated
        // ========================================================

        /// <summary>
        /// Minimal stand-in AIBehaviorPart for tests. Consumes AIBoredEvent
        /// unconditionally and pushes a WaitGoal so the brain gets something
        /// other than BoredGoal on its stack. Mirrors AIUndertakerPart's
        /// HandleEvent shape without M5's corpse/graveyard dependencies.
        /// </summary>
        private class TestAIBoredConsumer : AIBehaviorPart
        {
            public override string Name => "TestAIBoredConsumer";
            public int Consumed;

            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == AIBoredEvent.ID)
                {
                    Consumed++;
                    var brain = ParentEntity.GetPart<BrainPart>();
                    brain?.PushGoal(new WaitGoal(5));
                    e.Handled = true;
                    return false; // consumed
                }
                return true;
            }
        }

        [Test]
        public void SittingEffect_AIBoredConsumerWantsNPC_NPCStandsUp()
        {
            // Regression pin for user-reported "Undertaker sits in a chair
            // forever, never leaves." Fix: BoredGoal.Step 1 now fires
            // AIBoredEvent while seated; if a handler consumes it (indicating
            // it has duty work for this NPC), stand the NPC up so their
            // pushed goal drives movement.
            var zone = new Zone("TestZone");
            var creature = CreateCreature("Villagers");
            creature.Tags["AllowIdleBehavior"] = "";
            var brain = new BrainPart
            {
                CurrentZone = zone,
                Rng = new Random(42),
                SightRadius = 8,
            };
            creature.AddPart(brain);
            creature.AddPart(new StatusEffectsPart());
            var consumer = new TestAIBoredConsumer();
            creature.AddPart(consumer);
            zone.AddEntity(creature, 10, 10);

            creature.ApplyEffect(new SittingEffect());
            Assert.IsTrue(creature.HasEffect<SittingEffect>(), "Precondition: NPC is seated.");

            creature.FireEvent(GameEvent.New("TakeTurn"));

            Assert.AreEqual(1, consumer.Consumed,
                "AIBoredEvent must fire on seated NPCs so AIBehaviorPart consumers can act.");
            Assert.IsFalse(creature.HasEffect<SittingEffect>(),
                "Sitting NPC must stand up (SittingEffect removed) when an AIBehaviorPart " +
                "consumes AIBored — duty work overrides leisure.");
        }

        [Test]
        public void SittingEffect_NoAIBoredConsumer_StaysSeated()
        {
            // Counter-check: if no AIBehaviorPart consumes AIBoredEvent,
            // the NPC should STAY seated (current behavior preserved for
            // generic villagers enjoying leisure). Without this preservation
            // path the sit-forever fix would flip every seated NPC into
            // pacing mode every turn.
            var zone = new Zone("TestZone");
            var creature = CreateCreature("Villagers");
            creature.Tags["AllowIdleBehavior"] = "";
            var brain = new BrainPart
            {
                CurrentZone = zone,
                Rng = new Random(42),
                SightRadius = 8,
            };
            creature.AddPart(brain);
            creature.AddPart(new StatusEffectsPart());
            // Intentionally NO AIBehaviorPart attached.
            zone.AddEntity(creature, 10, 10);

            creature.ApplyEffect(new SittingEffect());

            creature.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsTrue(creature.HasEffect<SittingEffect>(),
                "Without a duty-bound AIBehaviorPart consumer, the NPC must stay seated — " +
                "sit-forever is still the default for civilian NPCs with no AI role.");
            var pos = zone.GetEntityPosition(creature);
            Assert.AreEqual(10, pos.x, "Position unchanged.");
            Assert.AreEqual(10, pos.y, "Position unchanged.");
        }

        // ========================
        // BedPart
        // ========================

        [Test]
        public void BedPart_RespondsToIdleQuery()
        {
            var zone = new Zone("TestZone");
            var bed = CreateBed();
            zone.AddEntity(bed, 5, 5);

            var creature = CreateCreature("Snapjaws");
            creature.Tags["AllowIdleBehavior"] = "";
            var brain = new BrainPart { CurrentZone = zone, Rng = new Random(42) };
            creature.AddPart(brain);
            zone.AddEntity(creature, 3, 5);

            var offer = IdleQueryEvent.QueryOffer(bed, creature);

            Assert.IsNotNull(offer, "Bed should offer idle behavior");
            Assert.AreEqual(5, offer.TargetX);
            Assert.AreEqual(5, offer.TargetY);
            Assert.IsTrue(bed.GetPart<BedPart>().Occupied,
                "Bed should be marked Occupied at query time");
        }

        // ========================
        // Regression: Bug #2 — Chair double-book race
        // ========================

        [Test]
        public void DoubleBook_SecondQueryRejected()
        {
            var zone = new Zone("TestZone");
            var chair = CreateChair();
            zone.AddEntity(chair, 5, 5);

            // First NPC queries successfully and reserves the chair
            var creature1 = CreateCreature("Snapjaws");
            creature1.Tags["AllowIdleBehavior"] = "";
            creature1.AddPart(new BrainPart { CurrentZone = zone, Rng = new Random(1) });
            zone.AddEntity(creature1, 3, 5);

            var offer1 = IdleQueryEvent.QueryOffer(chair, creature1);
            Assert.IsNotNull(offer1, "First NPC should get an offer");

            // Second NPC should be rejected — chair is already reserved
            var creature2 = CreateCreature("Snapjaws");
            creature2.Tags["AllowIdleBehavior"] = "";
            creature2.AddPart(new BrainPart { CurrentZone = zone, Rng = new Random(2) });
            zone.AddEntity(creature2, 4, 5);

            var offer2 = IdleQueryEvent.QueryOffer(chair, creature2);
            Assert.IsNull(offer2, "Second NPC should be rejected — chair already reserved");
        }

        [Test]
        public void DoubleBook_CleanupReleasesReservation()
        {
            var zone = new Zone("TestZone");
            var chair = CreateChair();
            zone.AddEntity(chair, 5, 5);

            var creature = CreateCreature("Snapjaws");
            creature.Tags["AllowIdleBehavior"] = "";
            creature.AddPart(new BrainPart { CurrentZone = zone, Rng = new Random(1) });
            zone.AddEntity(creature, 3, 5);

            var offer = IdleQueryEvent.QueryOffer(chair, creature);
            Assert.IsNotNull(offer);
            Assert.IsTrue(chair.GetPart<ChairPart>().Occupied);

            // Invoke the cleanup callback directly (simulates abandonment)
            offer.Cleanup.Invoke(null);

            Assert.IsFalse(chair.GetPart<ChairPart>().Occupied,
                "Cleanup callback should release the reservation");

            // Third NPC should now be able to query successfully
            var creature2 = CreateCreature("Snapjaws");
            creature2.Tags["AllowIdleBehavior"] = "";
            creature2.AddPart(new BrainPart { CurrentZone = zone, Rng = new Random(2) });
            zone.AddEntity(creature2, 4, 5);

            var offer2 = IdleQueryEvent.QueryOffer(chair, creature2);
            Assert.IsNotNull(offer2, "Chair should be available after cleanup");
        }

        // ========================
        // Regression: Bug #1 — Sit-at-wrong-location
        // ========================

        [Test]
        public void SitAtWrongLocation_MoveFails_NoSittingEffect()
        {
            // Simulate MoveToGoal failing: push position-gated DelegateGoal + a MoveToGoal
            // whose target is unreachable. DelegateGoal should run cleanup instead of action
            // when it executes at the wrong position.
            var zone = new Zone("TestZone");
            var chair = CreateChair();
            zone.AddEntity(chair, 10, 10);

            var creature = CreateCreature("Snapjaws");
            creature.Tags["AllowIdleBehavior"] = "";
            creature.AddPart(new StatusEffectsPart());
            var brain = new BrainPart { CurrentZone = zone, Rng = new Random(42) };
            creature.AddPart(brain);
            zone.AddEntity(creature, 5, 5);

            // Manually create the idle offer to simulate a successful query
            var offer = IdleQueryEvent.QueryOffer(chair, creature);
            Assert.IsNotNull(offer);
            Assert.IsTrue(chair.GetPart<ChairPart>().Occupied);

            // Push position-gated DelegateGoal with chair's position requirement
            brain.PushGoal(new DelegateGoal(offer.Action, offer.Cleanup, offer.TargetX, offer.TargetY));

            // Manually trigger TakeAction WITHOUT moving the creature.
            // Since position doesn't match, the goal should run cleanup, not action.
            var delegateGoal = brain.PeekGoal();
            delegateGoal.TakeAction();

            Assert.IsFalse(creature.HasEffect<SittingEffect>(),
                "Creature should NOT have sitting effect — never reached chair");
            Assert.IsFalse(chair.GetPart<ChairPart>().Occupied,
                "Chair should be released via cleanup — NPC never arrived");
        }

        [Test]
        public void SitAtWrongLocation_MoveFails_ChairReopens()
        {
            // Full integration: BoredGoal pushes [DelegateGoal, MoveToGoal] pair,
            // MoveToGoal fails because the chair is unreachable (walled off),
            // DelegateGoal detects wrong position and rolls back.
            var zone = new Zone("TestZone");
            var chair = CreateChair();
            zone.AddEntity(chair, 10, 10);

            // Wall off the chair completely so A* fails
            void PlaceWall(int x, int y)
            {
                var wall = new Entity();
                wall.Tags["Solid"] = "";
                wall.AddPart(new RenderPart());
                wall.AddPart(new PhysicsPart { Solid = true });
                zone.AddEntity(wall, x, y);
            }
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (dx != 0 || dy != 0)
                        PlaceWall(10 + dx, 10 + dy);

            var creature = CreateCreature("Snapjaws");
            creature.Tags["AllowIdleBehavior"] = "";
            creature.AddPart(new StatusEffectsPart());
            var brain = new BrainPart { CurrentZone = zone, Rng = new Random(42) };
            creature.AddPart(brain);
            zone.AddEntity(creature, 5, 5);

            // Manually query the chair to get an offer
            var offer = IdleQueryEvent.QueryOffer(chair, creature);
            Assert.IsNotNull(offer);
            Assert.IsTrue(chair.GetPart<ChairPart>().Occupied, "Chair reserved at query time");

            // Push the same pair BoredGoal would: DelegateGoal (lower), MoveToGoal (top)
            brain.PushGoal(new DelegateGoal(offer.Action, offer.Cleanup, offer.TargetX, offer.TargetY));
            brain.PushGoal(new MoveToGoal(offer.TargetX, offer.TargetY, 10));

            // Run several turns — MoveToGoal should fail, then DelegateGoal runs cleanup
            for (int i = 0; i < 15; i++)
                creature.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsFalse(creature.HasEffect<SittingEffect>(),
                "Creature should never have sat — chair was unreachable");
            Assert.IsFalse(chair.GetPart<ChairPart>().Occupied,
                "Chair should be released after MoveToGoal failed");
        }

        [Test]
        public void DelegateGoal_OnPop_RunsCleanupIfNotExecuted()
        {
            // If a position-gated DelegateGoal is popped externally (stack cleared, death, etc.)
            // before it ever executes, cleanup must still run so reservations are released.
            bool cleanupRan = false;
            bool actionRan = false;

            var zone = new Zone("TestZone");
            var creature = CreateCreature("Snapjaws");
            var brain = new BrainPart { CurrentZone = zone, Rng = new Random(42) };
            creature.AddPart(brain);
            zone.AddEntity(creature, 5, 5);

            var goal = new DelegateGoal(
                action: g => { actionRan = true; },
                cleanup: g => { cleanupRan = true; },
                requireX: 10,
                requireY: 10);

            brain.PushGoal(goal);
            Assert.IsFalse(cleanupRan);
            Assert.IsFalse(actionRan);

            // Clear the stack — simulates entity death or goal abandonment
            brain.ClearGoals();

            Assert.IsTrue(cleanupRan, "Cleanup should run on OnPop if action never executed");
            Assert.IsFalse(actionRan, "Action should NOT have run");
        }
    }
}
