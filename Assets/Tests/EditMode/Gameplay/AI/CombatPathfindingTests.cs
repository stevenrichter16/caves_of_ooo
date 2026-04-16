using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Tier 2c tests: verifies that hostile creatures (KillGoal) navigate
    /// around walls to reach their target using A* fallback instead of
    /// getting stuck on greedy-step obstacles.
    /// </summary>
    [TestFixture]
    public class CombatPathfindingTests
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
            return entity;
        }

        private void PlaceWall(Zone zone, int x, int y)
        {
            var wall = new Entity();
            wall.Tags["Solid"] = "";
            wall.AddPart(new RenderPart());
            wall.AddPart(new PhysicsPart { Solid = true });
            zone.AddEntity(wall, x, y);
        }

        private (Entity creature, BrainPart brain) CreateAttacker(Zone zone, int x, int y, int sightRadius = 20)
        {
            var entity = CreateCreature("Snapjaws", hp: 50);
            var brain = new BrainPart
            {
                SightRadius = sightRadius,
                CurrentZone = zone,
                Rng = new Random(42)
            };
            entity.AddPart(brain);
            zone.AddEntity(entity, x, y);
            return (entity, brain);
        }

        // ========================
        // AIHelpers.TryApproachWithPathfinding
        // ========================

        [Test]
        public void TryApproach_OpenTerrain_UsesGreedyStep()
        {
            var zone = new Zone("TestZone");
            var creature = CreateCreature("Snapjaws");
            zone.AddEntity(creature, 5, 5);

            bool moved = AIHelpers.TryApproachWithPathfinding(creature, zone, 5, 5, 10, 5);

            Assert.IsTrue(moved);
            var pos = zone.GetEntityPosition(creature);
            Assert.AreEqual(6, pos.x, "Greedy step-toward should have moved east");
            Assert.AreEqual(5, pos.y);
        }

        [Test]
        public void TryApproach_ThinWall_UsesGreedyDiagonalFallback()
        {
            // Single-cell wall at (6,5). Creature at (5,5), target at (10,5).
            // Greedy: try (6,5) → blocked. Try diagonals (6,6) or (6,4) → passable.
            var zone = new Zone("TestZone");
            PlaceWall(zone, 6, 5);

            var creature = CreateCreature("Snapjaws");
            zone.AddEntity(creature, 5, 5);

            bool moved = AIHelpers.TryApproachWithPathfinding(creature, zone, 5, 5, 10, 5);

            Assert.IsTrue(moved, "Should move around single-cell wall via diagonal fallback");
            var pos = zone.GetEntityPosition(creature);
            Assert.IsTrue(pos.x != 5 || pos.y != 5, "Should have moved from start");
        }

        [Test]
        public void TryApproach_LargeWall_FallsBackToAStar()
        {
            // Large vertical wall blocks greedy movement entirely.
            // Greedy cardinal/diagonal fallbacks all hit walls.
            // A* must find the path around the wall.
            var zone = new Zone("TestZone");
            // Vertical wall at x=7, y=3 to y=10 (8 cells tall — bigger than any diagonal fallback can handle)
            for (int wy = 3; wy <= 10; wy++)
                PlaceWall(zone, 7, wy);

            var creature = CreateCreature("Snapjaws");
            zone.AddEntity(creature, 5, 6);

            bool moved = AIHelpers.TryApproachWithPathfinding(creature, zone, 5, 6, 10, 6);

            Assert.IsTrue(moved, "A* should find a path around the large wall");
            var pos = zone.GetEntityPosition(creature);
            Assert.IsTrue(pos.x != 5 || pos.y != 6, "Creature should have moved");
        }

        [Test]
        public void TryApproach_Surrounded_ReturnsFalse()
        {
            // Creature boxed in — no path exists anywhere
            var zone = new Zone("TestZone");
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (dx != 0 || dy != 0)
                        PlaceWall(zone, 5 + dx, 5 + dy);

            var creature = CreateCreature("Snapjaws");
            zone.AddEntity(creature, 5, 5);

            bool moved = AIHelpers.TryApproachWithPathfinding(creature, zone, 5, 5, 20, 20);

            Assert.IsFalse(moved, "No path exists — should return false");
            var pos = zone.GetEntityPosition(creature);
            Assert.AreEqual(5, pos.x);
            Assert.AreEqual(5, pos.y);
        }

        // ========================
        // KillGoal — combat scenarios with walls
        // ========================

        [Test]
        public void KillGoal_OpenTerrain_ApproachesTargetOverTurns()
        {
            // Baseline: no walls, creature should still approach normally
            var zone = new Zone("TestZone");
            var (attacker, brain) = CreateAttacker(zone, 3, 5);
            var player = CreatePlayer();
            zone.AddEntity(player, 10, 5);

            // 6 turns should close the distance
            for (int i = 0; i < 6; i++)
                attacker.FireEvent(GameEvent.New("TakeTurn"));

            var pos = zone.GetEntityPosition(attacker);
            Assert.IsTrue(AIHelpers.IsAdjacent(pos.x, pos.y, 10, 5),
                $"Attacker at ({pos.x},{pos.y}) should be adjacent to player after 6 turns");
        }

        [Test]
        public void KillGoal_NavigatesAroundLargeWall()
        {
            // The critical test: BEFORE Tier 2c this scenario was impossible.
            // Large wall between attacker and target. Greedy gets stuck.
            // A* fallback should navigate around.
            //
            // We push KillGoal directly (not via BoredGoal's hostile scan) because
            // the wall blocks line-of-sight. This test is about NAVIGATION, not DETECTION.
            var zone = new Zone("TestZone");

            // Build a vertical wall from (7,2) to (7,10) — 9 cells
            for (int wy = 2; wy <= 10; wy++)
                PlaceWall(zone, 7, wy);

            var (attacker, brain) = CreateAttacker(zone, 3, 6);
            var player = CreatePlayer(hp: 100);
            zone.AddEntity(player, 11, 6);

            // Push KillGoal directly — bypass BoredGoal's LOS-gated hostile scan
            brain.PushGoal(new KillGoal(player));

            // Run many turns — attacker must route around the wall
            for (int i = 0; i < 30; i++)
                attacker.FireEvent(GameEvent.New("TakeTurn"));

            var pos = zone.GetEntityPosition(attacker);

            // Attacker should have crossed the wall line (x >= 8) or reached the player
            bool crossedWall = pos.x >= 8;
            bool reachedPlayer = AIHelpers.IsAdjacent(pos.x, pos.y, 11, 6);
            Assert.IsTrue(crossedWall || reachedPlayer,
                $"Attacker at ({pos.x},{pos.y}) should have crossed the wall (x>=8) or reached the player. " +
                "This is the key Tier 2c behavior: A* fallback navigates around obstacles.");
        }

        [Test]
        public void KillGoal_LShapedWall_NavigatesAround()
        {
            // L-shaped wall forcing a longer detour.
            // Push KillGoal directly to isolate navigation from LOS detection.
            var zone = new Zone("TestZone");

            // Vertical wall at x=7, y=3..7
            for (int wy = 3; wy <= 7; wy++)
                PlaceWall(zone, 7, wy);
            // Horizontal wall at y=7, x=8..11
            for (int wx = 8; wx <= 11; wx++)
                PlaceWall(zone, wx, 7);

            var (attacker, brain) = CreateAttacker(zone, 3, 5);
            var player = CreatePlayer(hp: 100);
            zone.AddEntity(player, 12, 5);

            brain.PushGoal(new KillGoal(player));

            for (int i = 0; i < 40; i++)
            {
                attacker.FireEvent(GameEvent.New("TakeTurn"));
                var cp = zone.GetEntityPosition(attacker);
                if (AIHelpers.IsAdjacent(cp.x, cp.y, 12, 5)) break;
            }

            var pos = zone.GetEntityPosition(attacker);
            Assert.IsTrue(pos.x >= 8 || AIHelpers.IsAdjacent(pos.x, pos.y, 12, 5),
                $"Attacker at ({pos.x},{pos.y}) should have pathed around the L-wall");
        }

        [Test]
        public void KillGoal_UnreachableTarget_DoesNotCrash()
        {
            // Target fully enclosed — no path exists
            var zone = new Zone("TestZone");
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (dx != 0 || dy != 0)
                        PlaceWall(zone, 15 + dx, 5 + dy);

            var (attacker, brain) = CreateAttacker(zone, 3, 5);
            var player = CreatePlayer();
            zone.AddEntity(player, 15, 5);

            // Attacker should fail gracefully — not crash, not infinite-loop
            for (int i = 0; i < 10; i++)
                attacker.FireEvent(GameEvent.New("TakeTurn"));

            Assert.Pass("No crash — graceful failure when target is unreachable");
        }

        [Test]
        public void KillGoal_TargetMoves_ReplansEachTick()
        {
            // Moving target: A* runs fresh each tick, so the attacker adapts.
            // Push KillGoal directly to isolate from LOS (wall blocks sight).
            var zone = new Zone("TestZone");

            // Wall forces A* pathfinding
            for (int wy = 3; wy <= 10; wy++)
                PlaceWall(zone, 7, wy);

            var (attacker, brain) = CreateAttacker(zone, 3, 6);
            var player = CreatePlayer(hp: 100);
            zone.AddEntity(player, 11, 6);

            brain.PushGoal(new KillGoal(player));

            // 5 ticks of chasing
            for (int i = 0; i < 5; i++)
                attacker.FireEvent(GameEvent.New("TakeTurn"));

            var posBeforeMove = zone.GetEntityPosition(attacker);

            // Teleport the player south — attacker should re-plan toward new position
            zone.MoveEntity(player, 11, 15);

            // More ticks — attacker should adjust
            for (int i = 0; i < 20; i++)
                attacker.FireEvent(GameEvent.New("TakeTurn"));

            var posAfterPlayerMoved = zone.GetEntityPosition(attacker);
            Assert.IsTrue(posAfterPlayerMoved.y >= posBeforeMove.y,
                $"Attacker at ({posAfterPlayerMoved.x},{posAfterPlayerMoved.y}) was at ({posBeforeMove.x},{posBeforeMove.y}) before player moved. " +
                "Attacker should be moving south to follow the displaced player.");
        }
    }
}
