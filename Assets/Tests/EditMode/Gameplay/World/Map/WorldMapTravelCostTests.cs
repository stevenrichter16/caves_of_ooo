using NUnit.Framework;
using UnityEngine.TestTools;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WM.4 tests for time-cost-on-worldmap-step. Pins:
    /// <list type="bullet">
    ///   <item><see cref="TurnManager.AdvanceClock"/> only increments
    ///   when given a positive delta (no-op otherwise)</item>
    ///   <item><see cref="WorldMapTravelCostPart"/> advances the
    ///   global clock by <c>WorldMapStepTurns</c> when the player
    ///   moves on the worldmap zone</item>
    ///   <item>Counter-checks: ground-zone move does NOT trigger
    ///   the cost; non-player moves don't trigger it; blocked moves
    ///   don't trigger it</item>
    /// </list>
    /// </summary>
    public class WorldMapTravelCostTests
    {
        [SetUp]
        public void Setup()
        {
            LogAssert.ignoreFailingMessages = true;
        }

        private const string MinimalBlueprintsJson = @"{
          ""Objects"": [
            { ""Name"": ""Wall"", ""Parts"": [
                { ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""true"" }] },
                { ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""#"" }] }
              ], ""Stats"": [], ""Tags"": [{ ""Key"": ""Solid"", ""Value"": """" },{ ""Key"": ""Wall"", ""Value"": """" }]
            },
            { ""Name"": ""Floor"", ""Parts"": [{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""."" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""Terrain"", ""Value"": """" }] },
            { ""Name"": ""StoneFloor"", ""Parts"": [{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""."" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""Terrain"", ""Value"": """" }] },
            { ""Name"": ""StoneWall"", ""Parts"": [{ ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""true"" }]},{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""#"" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""Solid"", ""Value"": """" },{ ""Key"": ""Wall"", ""Value"": """" }] },
            { ""Name"": ""Rubble"", ""Parts"": [{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": "","" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""Terrain"", ""Value"": """" }] },
            { ""Name"": ""StairsDown"", ""Parts"": [{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": "">"" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""StairsDown"", ""Value"": """" }] },
            { ""Name"": ""StairsUp"", ""Parts"": [{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""<"" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""StairsUp"", ""Value"": """" }] }
          ]
        }";

        // ── AdvanceClock pure unit tests ──────────────────────

        [Test]
        public void AdvanceClock_PositiveDelta_IncrementsTickCount()
        {
            var tm = new TurnManager();
            int before = tm.TickCount;
            tm.AdvanceClock(10);
            Assert.AreEqual(before + 10, tm.TickCount);
        }

        [Test]
        public void AdvanceClock_ZeroOrNegative_IsNoOp()
        {
            var tm = new TurnManager();
            int before = tm.TickCount;
            tm.AdvanceClock(0);
            tm.AdvanceClock(-5);
            Assert.AreEqual(before, tm.TickCount);
        }

        // ── WorldMapTravelCostPart integration ─────────────────

        private static (Zone worldMap, Entity player, TurnManager tm) SetupWorldMapWithPlayer()
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(MinimalBlueprintsJson);
            var zm = new OverworldZoneManager(factory, worldSeed: 7);
            var worldMap = zm.GetZone(WorldMap.WorldMapZoneID);

            var player = new Entity { ID = "player", BlueprintName = "Player" };
            player.Tags["Player"] = "";
            player.Tags["Creature"] = "";
            player.AddPart(new RenderPart { DisplayName = "you", RenderString = "@" });
            player.AddPart(new PhysicsPart { Solid = false });
            player.AddPart(new WorldMapTravelCostPart { WorldMapStepTurns = 10 });
            // Place at world (10, 10) → zone (40, 13)
            worldMap.AddEntity(player, 40, 13);

            // Fresh TurnManager so TickCount starts at 0.
            var tm = new TurnManager();
            tm.AddEntity(player);
            return (worldMap, player, tm);
        }

        [Test]
        public void WorldMapMove_AdvancesClockBy10()
        {
            var (worldMap, player, tm) = SetupWorldMapWithPlayer();
            int before = tm.TickCount;

            // Move east: (40,13) → (41,13). All embedded cells are passable.
            bool moved = MovementSystem.TryMove(player, worldMap, 1, 0);
            Assert.IsTrue(moved);
            Assert.AreEqual(before + 10, tm.TickCount,
                "Each worldmap step should advance clock by WorldMapStepTurns=10.");
        }

        [Test]
        public void GroundZoneMove_DoesNotAdvanceClockExtra()
        {
            // Counter-check: a move on a ground zone (NOT the worldmap)
            // should not invoke the worldmap travel cost.
            var factory = new EntityFactory();
            factory.LoadBlueprints(MinimalBlueprintsJson);
            var zm = new OverworldZoneManager(factory, worldSeed: 7);
            var ground = zm.GetZone("Overworld.10.10.0");

            var player = new Entity { ID = "player", BlueprintName = "Player" };
            player.Tags["Player"] = "";
            player.AddPart(new RenderPart { DisplayName = "you" });
            player.AddPart(new PhysicsPart { Solid = false });
            player.AddPart(new WorldMapTravelCostPart { WorldMapStepTurns = 10 });

            // Find any passable cell to place the player.
            int placedX = -1, placedY = -1;
            for (int y = 1; y < Zone.Height - 1 && placedX < 0; y++)
                for (int x = 1; x < Zone.Width - 1; x++)
                {
                    var c = ground.GetCell(x, y);
                    if (c != null && c.IsPassable() && c.Objects.Count == 0)
                    { placedX = x; placedY = y; break; }
                    if (c != null && c.IsPassable())
                    { placedX = x; placedY = y; break; }
                }
            Assert.GreaterOrEqual(placedX, 0, "Need at least one passable cell on the ground zone.");
            ground.AddEntity(player, placedX, placedY);

            var tm = new TurnManager();
            tm.AddEntity(player);
            int before = tm.TickCount;

            // Try to step in any direction — find one that succeeds
            bool moved = false;
            int[,] dirs = new int[,] { { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } };
            for (int i = 0; i < dirs.GetLength(0); i++)
            {
                if (MovementSystem.TryMove(player, ground, dirs[i, 0], dirs[i, 1]))
                {
                    moved = true; break;
                }
            }
            Assert.IsTrue(moved, "Expected at least one direction to be passable.");
            Assert.AreEqual(before, tm.TickCount,
                "Ground zone move must NOT trigger worldmap travel cost.");
        }

        [Test]
        public void WorldMapBlockedMove_DoesNotAdvanceClock()
        {
            var (worldMap, player, tm) = SetupWorldMapWithPlayer();
            int before = tm.TickCount;

            // Move toward the wall: from (40,13), going west 30 cells would
            // hit the impassable border. One step toward the wall along the
            // x-axis: at (30, 13), going west (dx=-1) hits cell (29, 13)
            // which is the wall border.
            // First move player to (30, 13) which is the west edge of the
            // embedded region (one cell from the wall).
            // Repeat west moves until at edge.
            for (int i = 0; i < 10; i++)
            {
                if (!MovementSystem.TryMove(player, worldMap, -1, 0)) break;
            }
            int tickAtWall = tm.TickCount;

            // Now attempt another west step — should be blocked by wall.
            bool moved = MovementSystem.TryMove(player, worldMap, -1, 0);
            Assert.IsFalse(moved, "Move into wall must fail.");
            Assert.AreEqual(tickAtWall, tm.TickCount,
                "Blocked moves must NOT advance the clock.");
        }

        [Test]
        public void NonPlayerMove_DoesNotAdvanceClock()
        {
            // Counter-check: an NPC on the worldmap (theoretical) moving
            // should NOT trigger the player's travel cost. The cost is
            // attached to the player's Part instance.
            var (worldMap, player, tm) = SetupWorldMapWithPlayer();

            var npc = new Entity { ID = "npc", BlueprintName = "NPC" };
            npc.AddPart(new RenderPart { DisplayName = "n" });
            npc.AddPart(new PhysicsPart { Solid = false });
            // NPC has NO WorldMapTravelCostPart
            // Place NPC in a different worldmap cell
            worldMap.AddEntity(npc, 42, 13);
            int before = tm.TickCount;

            // Move the NPC east
            MovementSystem.TryMove(npc, worldMap, 1, 0);
            Assert.AreEqual(before, tm.TickCount,
                "NPC moves should not trigger player travel cost.");
        }

        [Test]
        public void WorldMapMove_DiagonalSameCost()
        {
            // Parity check: diagonal move costs the same WorldMapStepTurns.
            var (worldMap, player, tm) = SetupWorldMapWithPlayer();
            int before = tm.TickCount;

            // Diagonal move SE: (40,13) → (41,14)
            bool moved = MovementSystem.TryMove(player, worldMap, 1, 1);
            Assert.IsTrue(moved);
            Assert.AreEqual(before + 10, tm.TickCount);
        }
    }
}
