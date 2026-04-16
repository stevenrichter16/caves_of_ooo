using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Tests for Phase 2: Brain.StartingCell, Staying flag, WhenBoredReturnToOnce property.
    /// These validate that NPCs can be anchored to a "home" cell and return there when idle.
    /// </summary>
    [TestFixture]
    public class StartingCellTests
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

        private (Entity creature, BrainPart brain) CreateBrainyCreature(
            Zone zone, int x, int y,
            bool wanders = true, bool wandersRandomly = true, bool staying = false)
        {
            var entity = CreateCreature("Snapjaws");
            var brain = new BrainPart
            {
                SightRadius = 10,
                Wanders = wanders,
                WandersRandomly = wandersRandomly,
                Staying = staying,
                CurrentZone = zone,
                Rng = new Random(42)
            };
            entity.AddPart(brain);
            zone.AddEntity(entity, x, y);
            return (entity, brain);
        }

        // ========================
        // StartingCell initialization
        // ========================

        [Test]
        public void StartingCell_DefaultsToInvalid()
        {
            var brain = new BrainPart();
            Assert.AreEqual(-1, brain.StartingCellX);
            Assert.AreEqual(-1, brain.StartingCellY);
            Assert.IsFalse(brain.HasStartingCell);
        }

        [Test]
        public void StartingCell_SetOnFirstTakeTurn()
        {
            var zone = new Zone("TestZone");
            var (creature, brain) = CreateBrainyCreature(zone, 10, 7);

            // StartingCell should be uninitialized before the first turn
            Assert.IsFalse(brain.HasStartingCell);

            creature.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsTrue(brain.HasStartingCell);
            Assert.AreEqual(10, brain.StartingCellX);
            Assert.AreEqual(7, brain.StartingCellY);
        }

        [Test]
        public void StartingCell_NotOverwrittenOnSubsequentTurns()
        {
            var zone = new Zone("TestZone");
            var (creature, brain) = CreateBrainyCreature(zone, 10, 10);

            // First turn sets StartingCell
            creature.FireEvent(GameEvent.New("TakeTurn"));
            int originalX = brain.StartingCellX;
            int originalY = brain.StartingCellY;

            // Force the creature to a different cell
            zone.MoveEntity(creature, 15, 15);

            // Another turn — StartingCell must NOT change
            creature.FireEvent(GameEvent.New("TakeTurn"));

            Assert.AreEqual(originalX, brain.StartingCellX);
            Assert.AreEqual(originalY, brain.StartingCellY);
        }

        // ========================
        // Stay(x, y) method
        // ========================

        [Test]
        public void Stay_SetsStartingCellAndFlag()
        {
            var brain = new BrainPart();
            Assert.IsFalse(brain.Staying);
            Assert.IsFalse(brain.HasStartingCell);

            brain.Stay(20, 15);

            Assert.IsTrue(brain.Staying);
            Assert.AreEqual(20, brain.StartingCellX);
            Assert.AreEqual(15, brain.StartingCellY);
            Assert.IsTrue(brain.HasStartingCell);
        }

        // ========================
        // Staying behavior in BoredGoal
        // ========================

        [Test]
        public void Staying_ReturnsToStartingCellWhenDisplaced()
        {
            var zone = new Zone("TestZone");
            var (creature, brain) = CreateBrainyCreature(zone, 10, 10, staying: true);

            // Prime StartingCell
            creature.FireEvent(GameEvent.New("TakeTurn"));
            Assert.AreEqual(10, brain.StartingCellX);
            Assert.AreEqual(10, brain.StartingCellY);

            // Teleport the creature far from home
            zone.MoveEntity(creature, 20, 10);

            // Run many ticks — should walk back toward home
            for (int i = 0; i < 30; i++)
                creature.FireEvent(GameEvent.New("TakeTurn"));

            var pos = zone.GetEntityPosition(creature);
            Assert.AreEqual(10, pos.x, "Creature should have returned to StartingCellX");
            Assert.AreEqual(10, pos.y, "Creature should have returned to StartingCellY");
        }

        [Test]
        public void Staying_AtHome_Idles_DoesNotWander()
        {
            var zone = new Zone("TestZone");
            var (creature, brain) = CreateBrainyCreature(zone, 10, 10, staying: true);

            // Prime StartingCell via first turn
            creature.FireEvent(GameEvent.New("TakeTurn"));

            // Run many ticks — creature should stay put (not wander)
            for (int i = 0; i < 20; i++)
                creature.FireEvent(GameEvent.New("TakeTurn"));

            var pos = zone.GetEntityPosition(creature);
            Assert.AreEqual(10, pos.x, "Staying creature at home should not wander");
            Assert.AreEqual(10, pos.y);
        }

        [Test]
        public void NotStaying_WandersNormally()
        {
            var zone = new Zone("TestZone");
            var (creature, brain) = CreateBrainyCreature(zone, 10, 10, staying: false);

            // Many turns — with Wanders=true and Staying=false, creature should drift
            bool moved = false;
            for (int i = 0; i < 30; i++)
            {
                creature.FireEvent(GameEvent.New("TakeTurn"));
                var pos = zone.GetEntityPosition(creature);
                if (pos.x != 10 || pos.y != 10)
                {
                    moved = true;
                    break;
                }
            }
            Assert.IsTrue(moved, "Non-Staying creature should wander away from spawn");
        }

        [Test]
        public void Staying_StopsAtHostileEncounter()
        {
            var zone = new Zone("TestZone");
            var (creature, brain) = CreateBrainyCreature(zone, 10, 10, staying: true);
            creature.FireEvent(GameEvent.New("TakeTurn"));  // prime StartingCell

            // Hostile enters range — Staying should yield to combat
            var player = CreateCreature(null);
            player.Tags["Player"] = "";
            player.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 50, Min = 0, Max = 50 };
            zone.AddEntity(player, 12, 10);

            creature.FireEvent(GameEvent.New("TakeTurn"));
            Assert.AreEqual(AIState.Chase, brain.CurrentState,
                "Staying should yield to combat when hostile appears");
        }

        // ========================
        // WhenBoredReturnToOnce property
        // ========================

        [Test]
        public void WhenBoredReturnToOnce_WalksToTargetAndClears()
        {
            var zone = new Zone("TestZone");
            // Disable wandering so the creature stays put after reaching the destination
            var (creature, brain) = CreateBrainyCreature(
                zone, 5, 5, wanders: false, wandersRandomly: false);

            // Prime StartingCell
            creature.FireEvent(GameEvent.New("TakeTurn"));

            // Set the one-shot return destination
            creature.Properties["WhenBoredReturnToOnce"] = "15,5";

            // Run enough ticks to walk from (5,5) to (15,5) — 10 diagonal-or-cardinal steps
            for (int i = 0; i < 15; i++)
                creature.FireEvent(GameEvent.New("TakeTurn"));

            var pos = zone.GetEntityPosition(creature);
            Assert.AreEqual(15, pos.x, "Creature should have walked toward WhenBoredReturnToOnce");
            Assert.AreEqual(5, pos.y);

            // Property should have been cleared on first consumption
            Assert.IsFalse(creature.Properties.ContainsKey("WhenBoredReturnToOnce"),
                "Property should be cleared after being consumed");
        }

        [Test]
        public void WhenBoredReturnToOnce_ClearedEvenOnFirstBoredCheck()
        {
            // Subtle: the property should be cleared the FIRST time BoredGoal sees it,
            // not after the NPC actually arrives. Otherwise NPCs would loop back
            // if combat or other goals interrupt the walk.
            var zone = new Zone("TestZone");
            var (creature, brain) = CreateBrainyCreature(zone, 5, 5);
            creature.FireEvent(GameEvent.New("TakeTurn"));  // prime StartingCell

            creature.Properties["WhenBoredReturnToOnce"] = "15,5";
            creature.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsFalse(creature.Properties.ContainsKey("WhenBoredReturnToOnce"),
                "Property should be cleared on first BoredGoal encounter, not on arrival");
        }

        [Test]
        public void WhenBoredReturnToOnce_IgnoresMalformedValue()
        {
            var zone = new Zone("TestZone");
            var (creature, brain) = CreateBrainyCreature(zone, 5, 5);
            creature.FireEvent(GameEvent.New("TakeTurn"));

            // Malformed values should be cleared (since BoredGoal removes the property before parsing)
            // and the NPC should fall through to normal wander/idle behavior.
            creature.Properties["WhenBoredReturnToOnce"] = "not-a-coordinate";
            creature.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsFalse(creature.Properties.ContainsKey("WhenBoredReturnToOnce"),
                "Malformed value should still be cleared");
            // Entity should still exist at a valid position (didn't crash)
            var pos = zone.GetEntityPosition(creature);
            Assert.IsTrue(pos.x >= 0);
        }

        // ========================
        // HasStartingCell
        // ========================

        [Test]
        public void HasStartingCell_FalseWhenUninitialized()
        {
            var brain = new BrainPart();
            Assert.IsFalse(brain.HasStartingCell);
        }

        [Test]
        public void HasStartingCell_TrueAfterSet()
        {
            var brain = new BrainPart
            {
                StartingCellX = 0,
                StartingCellY = 0
            };
            Assert.IsTrue(brain.HasStartingCell, "0,0 is a valid starting cell");
        }

        [Test]
        public void HasStartingCell_FalseWhenOnlyOneCoordSet()
        {
            var brain = new BrainPart { StartingCellX = 5, StartingCellY = -1 };
            Assert.IsFalse(brain.HasStartingCell, "Both coords required");
        }
    }
}
