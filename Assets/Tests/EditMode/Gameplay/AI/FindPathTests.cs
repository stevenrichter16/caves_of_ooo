using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    [TestFixture]
    public class FindPathTests
    {
        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
        }

        private Zone CreateZone()
        {
            return new Zone("TestZone");
        }

        private void PlaceWall(Zone zone, int x, int y)
        {
            var wall = new Entity();
            wall.Tags["Solid"] = "";
            wall.AddPart(new RenderPart());
            wall.AddPart(new PhysicsPart { Solid = true });
            zone.AddEntity(wall, x, y);
        }

        // ========================
        // FindPath.Search
        // ========================

        [Test]
        public void StraightLine_FindsDirectPath()
        {
            var zone = CreateZone();
            var result = FindPath.Search(zone, 5, 5, 10, 5);

            Assert.IsTrue(result.Usable);
            Assert.AreEqual(5, result.Steps.Count);
            foreach (var step in result.Steps)
            {
                Assert.AreEqual(1, step.dx);
                Assert.AreEqual(0, step.dy);
            }
        }

        [Test]
        public void DiagonalPath_FindsDiagonalSteps()
        {
            var zone = CreateZone();
            var result = FindPath.Search(zone, 5, 5, 10, 10);

            Assert.IsTrue(result.Usable);
            Assert.AreEqual(5, result.Steps.Count);
            foreach (var step in result.Steps)
            {
                Assert.AreEqual(1, step.dx);
                Assert.AreEqual(1, step.dy);
            }
        }

        [Test]
        public void SameCell_ReturnsUsableEmptyPath()
        {
            var zone = CreateZone();
            var result = FindPath.Search(zone, 5, 5, 5, 5);

            Assert.IsTrue(result.Usable);
            Assert.AreEqual(0, result.Steps.Count);
        }

        [Test]
        public void PathAroundWall_FindsDetour()
        {
            var zone = CreateZone();
            // Vertical wall at x=7, y=3 to y=7
            for (int wy = 3; wy <= 7; wy++)
                PlaceWall(zone, 7, wy);

            var result = FindPath.Search(zone, 5, 5, 10, 5);

            Assert.IsTrue(result.Usable);
            // Path must go around the wall — should be longer than 5 direct steps
            Assert.Greater(result.Steps.Count, 5);

            // Verify the path is valid by following it
            int x = 5, y = 5;
            foreach (var step in result.Steps)
            {
                x += step.dx;
                y += step.dy;
                Assert.IsTrue(zone.InBounds(x, y), $"Path goes out of bounds at ({x},{y})");
                // Intermediate cells should be passable (except goal cell)
                if (x != 10 || y != 5)
                {
                    var cell = zone.GetCell(x, y);
                    Assert.IsTrue(cell.IsPassable(), $"Path goes through solid cell at ({x},{y})");
                }
            }
            Assert.AreEqual(10, x);
            Assert.AreEqual(5, y);
        }

        [Test]
        public void FullyEnclosed_ReturnsNotUsable()
        {
            var zone = CreateZone();
            // Enclose position (5,5) completely
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (dx != 0 || dy != 0)
                        PlaceWall(zone, 5 + dx, 5 + dy);

            var result = FindPath.Search(zone, 5, 5, 10, 5);

            Assert.IsFalse(result.Usable);
        }

        [Test]
        public void PathToSolidGoal_IsUsable()
        {
            var zone = CreateZone();
            // Goal cell has a solid entity (e.g., a creature)
            PlaceWall(zone, 10, 5);

            var result = FindPath.Search(zone, 5, 5, 10, 5);

            Assert.IsTrue(result.Usable, "Should path TO a solid cell (goal is always passable)");
            Assert.AreEqual(5, result.Steps.Count);
        }

        [Test]
        public void MaxNodes_LimitsSearch()
        {
            var zone = CreateZone();
            // Very tight budget
            var result = FindPath.Search(zone, 0, 0, 79, 24, maxNodes: 10);

            // With only 10 nodes expanded, unlikely to find path across full grid
            // (but if it does, that's also fine — this tests the budget cap doesn't crash)
            Assert.IsNotNull(result);
        }

        [Test]
        public void LShapedWall_PathsAround()
        {
            var zone = CreateZone();
            // L-shaped wall:
            // x=7, y=3..7 (vertical bar)
            // x=7..10, y=3 (horizontal bar)
            for (int y = 3; y <= 7; y++)
                PlaceWall(zone, 7, y);
            for (int x = 8; x <= 10; x++)
                PlaceWall(zone, x, 3);

            var result = FindPath.Search(zone, 5, 5, 10, 5);

            Assert.IsTrue(result.Usable);

            // Follow path and verify destination
            int x2 = 5, y2 = 5;
            foreach (var step in result.Steps)
            {
                x2 += step.dx;
                y2 += step.dy;
            }
            Assert.AreEqual(10, x2);
            Assert.AreEqual(5, y2);
        }

        // ========================
        // MoveToGoal with A*
        // ========================

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

        [Test]
        public void MoveToGoal_NavigatesAroundWall()
        {
            var zone = CreateZone();
            // Wall blocking direct path
            for (int y = 3; y <= 7; y++)
                PlaceWall(zone, 7, y);

            var creature = CreateCreature("Snapjaws");
            var brain = new BrainPart
            {
                CurrentZone = zone,
                Rng = new Random(42),
                Wanders = false,
                WandersRandomly = false
            };
            creature.AddPart(brain);
            zone.AddEntity(creature, 5, 5);

            brain.PushGoal(new MoveToGoal(10, 5, 50));

            // Run turns until creature reaches destination or gives up
            for (int i = 0; i < 20; i++)
            {
                creature.FireEvent(GameEvent.New("TakeTurn"));
                var pos = zone.GetEntityPosition(creature);
                if (pos.x == 10 && pos.y == 5) break;
            }

            var finalPos = zone.GetEntityPosition(creature);
            Assert.AreEqual(10, finalPos.x, "Should have navigated around the wall");
            Assert.AreEqual(5, finalPos.y);
        }

        [Test]
        public void MoveToGoal_RecomputesOnBlock()
        {
            var zone = CreateZone();
            var creature = CreateCreature("Snapjaws");
            var brain = new BrainPart
            {
                CurrentZone = zone,
                Rng = new Random(42),
                Wanders = false,
                WandersRandomly = false
            };
            creature.AddPart(brain);
            zone.AddEntity(creature, 5, 5);

            brain.PushGoal(new MoveToGoal(10, 5, 50));

            // Take one step
            creature.FireEvent(GameEvent.New("TakeTurn"));
            var pos1 = zone.GetEntityPosition(creature);
            Assert.AreEqual(6, pos1.x);

            // Block the next cell
            PlaceWall(zone, 7, 5);

            // Should recompute and find alternate path
            creature.FireEvent(GameEvent.New("TakeTurn"));
            var pos2 = zone.GetEntityPosition(creature);
            Assert.IsTrue(pos2.x != 6 || pos2.y != 5, "Should have moved despite blocked path");
        }
    }
}
