using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Tests for the Qud-style goal stack system.
    /// </summary>
    [TestFixture]
    public class GoalStackTests
    {
        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
        }

        // --- Helpers ---

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
            entity.GetPart<RenderPart>().DisplayName = "you";
            return entity;
        }

        private (Entity creature, BrainPart brain) CreateCreatureWithBrain(
            string faction, Zone zone, int x, int y, int sightRadius = 10,
            bool wanders = true, bool wandersRandomly = true)
        {
            var entity = CreateCreature(faction);
            var brain = new BrainPart
            {
                SightRadius = sightRadius,
                Wanders = wanders,
                WandersRandomly = wandersRandomly,
                CurrentZone = zone,
                Rng = new Random(42)
            };
            entity.AddPart(brain);
            zone.AddEntity(entity, x, y);
            return (entity, brain);
        }

        private Zone CreateZone()
        {
            return new Zone("TestZone");
        }

        private void FireTakeTurn(Entity entity)
        {
            entity.FireEvent(GameEvent.New("TakeTurn"));
        }

        // ========================
        // Goal Stack Mechanics
        // ========================

        [Test]
        public void EmptyStack_PushesBoredOnTakeTurn()
        {
            var zone = CreateZone();
            var (creature, brain) = CreateCreatureWithBrain("Snapjaws", zone, 10, 10);

            Assert.AreEqual(0, brain.GoalCount);
            FireTakeTurn(creature);
            Assert.IsTrue(brain.HasGoal<BoredGoal>(), "Should have BoredGoal after TakeTurn");
        }

        [Test]
        public void FinishedGoal_IsPopped()
        {
            var zone = CreateZone();
            var (creature, brain) = CreateCreatureWithBrain("Snapjaws", zone, 10, 10);

            // Push a WaitGoal that finishes immediately (duration 0, age starts at 1 after first tick)
            brain.PushGoal(new WaitGoal(0));
            Assert.AreEqual(1, brain.GoalCount);

            FireTakeTurn(creature);
            // WaitGoal(0) should be finished (Age >= Duration after age increment)
            // and BoredGoal pushed as replacement
            Assert.IsTrue(brain.HasGoal<BoredGoal>());
        }

        [Test]
        public void PushChildGoal_SetsParentBrainAndHandler()
        {
            var zone = CreateZone();
            var (creature, brain) = CreateCreatureWithBrain("Snapjaws", zone, 10, 10);

            var parent = new BoredGoal();
            brain.PushGoal(parent);

            var child = new WaitGoal(5);
            parent.PushChildGoal(child);

            Assert.AreEqual(brain, child.ParentBrain);
            Assert.AreEqual(parent, child.ParentHandler);
        }

        [Test]
        public void ClearGoals_EmptiesStack()
        {
            var zone = CreateZone();
            var (creature, brain) = CreateCreatureWithBrain("Snapjaws", zone, 10, 10);

            brain.PushGoal(new BoredGoal());
            brain.PushGoal(new WaitGoal(5));
            Assert.AreEqual(2, brain.GoalCount);

            brain.ClearGoals();
            Assert.AreEqual(0, brain.GoalCount);
        }

        [Test]
        public void HasGoal_FindsGoalByType()
        {
            var zone = CreateZone();
            var (creature, brain) = CreateCreatureWithBrain("Snapjaws", zone, 10, 10);

            brain.PushGoal(new BoredGoal());
            Assert.IsTrue(brain.HasGoal<BoredGoal>());
            Assert.IsFalse(brain.HasGoal<KillGoal>());
        }

        // ========================
        // BoredGoal
        // ========================

        [Test]
        public void BoredGoal_DetectsHostile_PushesKillGoal()
        {
            var zone = CreateZone();
            var (creature, brain) = CreateCreatureWithBrain("Snapjaws", zone, 5, 5);
            var player = CreatePlayer();
            zone.AddEntity(player, 8, 5);

            FireTakeTurn(creature);

            Assert.IsTrue(brain.HasGoal<KillGoal>(), "Should have KillGoal on stack");
            Assert.IsTrue(brain.HasGoal<BoredGoal>(), "BoredGoal should still be on stack");
            Assert.AreEqual(AIState.Chase, brain.CurrentState);
        }

        [Test]
        public void BoredGoal_NoHostile_WandersRandomly()
        {
            var zone = CreateZone();
            var (creature, brain) = CreateCreatureWithBrain("Snapjaws", zone, 10, 10);

            FireTakeTurn(creature);

            Assert.AreEqual(AIState.Wander, brain.CurrentState);
            var pos = zone.GetEntityPosition(creature);
            Assert.IsTrue(pos.x != 10 || pos.y != 10, "Should have moved");
        }

        [Test]
        public void BoredGoal_NoHostile_WanderDisabled_Idles()
        {
            var zone = CreateZone();
            var (creature, brain) = CreateCreatureWithBrain("Snapjaws", zone, 10, 10,
                wanders: false, wandersRandomly: false);

            FireTakeTurn(creature);

            Assert.AreEqual(AIState.Idle, brain.CurrentState);
            var pos = zone.GetEntityPosition(creature);
            Assert.AreEqual(10, pos.x);
            Assert.AreEqual(10, pos.y);
        }

        // ========================
        // KillGoal
        // ========================

        [Test]
        public void KillGoal_Finished_WhenTargetRemoved()
        {
            var zone = CreateZone();
            var (creature, brain) = CreateCreatureWithBrain("Snapjaws", zone, 5, 5);
            var player = CreatePlayer();
            zone.AddEntity(player, 8, 5);

            var kill = new KillGoal(player);
            brain.PushGoal(kill);

            Assert.IsFalse(kill.Finished());
            zone.RemoveEntity(player);
            Assert.IsTrue(kill.Finished());
        }

        [Test]
        public void KillGoal_AttacksAdjacent()
        {
            var zone = CreateZone();
            var (creature, brain) = CreateCreatureWithBrain("Snapjaws", zone, 5, 5);
            var player = CreatePlayer(hp: 50);
            zone.AddEntity(player, 6, 5);

            brain.PushGoal(new BoredGoal());

            FireTakeTurn(creature);

            // Creature should not have moved (attacks instead)
            var pos = zone.GetEntityPosition(creature);
            Assert.AreEqual(5, pos.x);
            Assert.AreEqual(5, pos.y);
            Assert.Greater(MessageLog.Count, 0, "Should have combat messages");
        }

        [Test]
        public void KillGoal_StepsToward_WhenNotAdjacent()
        {
            var zone = CreateZone();
            var (creature, brain) = CreateCreatureWithBrain("Snapjaws", zone, 5, 5);
            var player = CreatePlayer();
            zone.AddEntity(player, 10, 5);

            FireTakeTurn(creature);

            var pos = zone.GetEntityPosition(creature);
            Assert.AreEqual(6, pos.x, "Should have stepped toward player");
            Assert.AreEqual(5, pos.y);
        }

        // ========================
        // FleeGoal
        // ========================

        [Test]
        public void FleeGoal_StepsAway()
        {
            var zone = CreateZone();
            var entity = CreateCreature("Snapjaws", hp: 1);
            var brain = new BrainPart
            {
                SightRadius = 10,
                FleeThreshold = 0.5f,
                CurrentZone = zone,
                Rng = new Random(42)
            };
            entity.AddPart(brain);
            // Set max HP higher so 1/15 < 0.5f threshold
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 1, Min = 0, Max = 15 };
            zone.AddEntity(entity, 5, 5);

            var threat = CreatePlayer();
            zone.AddEntity(threat, 6, 5);

            var flee = new FleeGoal(threat);
            brain.PushGoal(flee);
            flee.ParentBrain = brain;

            FireTakeTurn(entity);

            var pos = zone.GetEntityPosition(entity);
            Assert.Less(pos.x, 5, "Should have stepped away from threat");
        }

        [Test]
        public void FleeGoal_FinishesWhenThreatGone()
        {
            var zone = CreateZone();
            var (creature, brain) = CreateCreatureWithBrain("Snapjaws", zone, 5, 5);

            var threat = CreatePlayer();
            zone.AddEntity(threat, 10, 5);

            var flee = new FleeGoal(threat);
            brain.PushGoal(flee);

            zone.RemoveEntity(threat);
            Assert.IsTrue(flee.Finished());
        }

        // ========================
        // WaitGoal
        // ========================

        [Test]
        public void WaitGoal_IdlesForDuration()
        {
            var zone = CreateZone();
            var (creature, brain) = CreateCreatureWithBrain("Snapjaws", zone, 10, 10,
                wanders: false, wandersRandomly: false);

            brain.PushGoal(new WaitGoal(3));
            FireTakeTurn(creature);

            var pos = zone.GetEntityPosition(creature);
            Assert.AreEqual(10, pos.x);
            Assert.AreEqual(10, pos.y);
            Assert.AreEqual(AIState.Idle, brain.CurrentState);
        }

        // ========================
        // MoveToGoal
        // ========================

        [Test]
        public void MoveTo_StepsTowardTarget()
        {
            var zone = CreateZone();
            var (creature, brain) = CreateCreatureWithBrain("Snapjaws", zone, 5, 5);

            brain.PushGoal(new MoveToGoal(10, 5));
            FireTakeTurn(creature);

            var pos = zone.GetEntityPosition(creature);
            Assert.AreEqual(6, pos.x, "Should have stepped toward target");
            Assert.AreEqual(5, pos.y);
        }

        [Test]
        public void MoveTo_FinishesOnArrival()
        {
            var zone = CreateZone();
            var (creature, brain) = CreateCreatureWithBrain("Snapjaws", zone, 5, 5);

            var moveTo = new MoveToGoal(6, 5);
            brain.PushGoal(moveTo);
            FireTakeTurn(creature);

            var pos = zone.GetEntityPosition(creature);
            Assert.AreEqual(6, pos.x);
            Assert.IsTrue(moveTo.Finished());
        }

        // ========================
        // GuardGoal
        // ========================

        [Test]
        public void Guard_AttacksHostile()
        {
            var zone = CreateZone();
            var (creature, brain) = CreateCreatureWithBrain("Snapjaws", zone, 5, 5);
            var player = CreatePlayer(hp: 50);
            zone.AddEntity(player, 6, 5);

            brain.PushGoal(new GuardGoal(5, 5));
            FireTakeTurn(creature);

            Assert.IsTrue(brain.HasGoal<KillGoal>(), "Should have pushed KillGoal");
            Assert.Greater(MessageLog.Count, 0, "Should have combat messages");
        }

        [Test]
        public void Guard_ReturnsToPost()
        {
            var zone = CreateZone();
            var (creature, brain) = CreateCreatureWithBrain("Snapjaws", zone, 7, 5);

            brain.PushGoal(new GuardGoal(5, 5));
            FireTakeTurn(creature);

            var pos = zone.GetEntityPosition(creature);
            Assert.AreEqual(6, pos.x, "Should step back toward guard post");
        }

        [Test]
        public void Guard_IdlesAtPost()
        {
            var zone = CreateZone();
            var (creature, brain) = CreateCreatureWithBrain("Snapjaws", zone, 5, 5);

            brain.PushGoal(new GuardGoal(5, 5));
            FireTakeTurn(creature);

            var pos = zone.GetEntityPosition(creature);
            Assert.AreEqual(5, pos.x, "Should stay at post");
            Assert.AreEqual(5, pos.y);
        }

        // ========================
        // Integration
        // ========================

        [Test]
        public void MultiTurnChase_ApproachesTarget()
        {
            var zone = CreateZone();
            var (creature, brain) = CreateCreatureWithBrain("Snapjaws", zone, 5, 5);
            var player = CreatePlayer(hp: 50);
            zone.AddEntity(player, 10, 5);

            // 5 turns of chasing
            for (int i = 0; i < 4; i++)
                FireTakeTurn(creature);

            var pos = zone.GetEntityPosition(creature);
            Assert.AreEqual(9, pos.x, "Should be adjacent to player after 4 steps");
            Assert.AreEqual(AIState.Chase, brain.CurrentState);

            // 5th turn: should attack
            FireTakeTurn(creature);
            Assert.Greater(MessageLog.Count, 0, "Should have combat messages from attack");
        }

        [Test]
        public void KillGoal_PersistsAcrossTurns()
        {
            var zone = CreateZone();
            var (creature, brain) = CreateCreatureWithBrain("Snapjaws", zone, 5, 5);
            var player = CreatePlayer(hp: 50);
            zone.AddEntity(player, 8, 5);

            // First turn: detect hostile, push KillGoal
            FireTakeTurn(creature);
            Assert.IsTrue(brain.HasGoal<KillGoal>());
            int killCountAfterFirst = 0;
            for (int i = 0; i < brain.GoalCount; i++)
                if (brain.PeekGoal() is KillGoal) killCountAfterFirst++;

            // Second turn: KillGoal should still be on stack (target still alive)
            FireTakeTurn(creature);
            Assert.IsTrue(brain.HasGoal<KillGoal>(), "KillGoal should persist while target alive");
        }
    }
}
