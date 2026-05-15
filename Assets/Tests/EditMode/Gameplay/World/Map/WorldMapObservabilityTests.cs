using NUnit.Framework;
using UnityEngine.TestTools;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WM.7 observability tests. Pins the diag-emission contract:
    /// <list type="bullet">
    ///   <item><c>worldmap/Ascended</c> on player ascend (from →
    ///   to coords)</item>
    ///   <item><c>worldmap/Descended</c> on player descend (to →
    ///   from coords + usedSavedLocation flag)</item>
    ///   <item><c>worldmap/Stepped</c> on each move on the worldmap
    ///   zone (toWorldX/Y + turnsCost)</item>
    ///   <item>Ascend+Descend round trip emits both records with
    ///   matching coord data</item>
    /// </list>
    ///
    /// <para>The dump pattern follows the observability fixtures
    /// from feat/observability-driven-mechanics-coverage. Each test
    /// runs a scenario, queries Diag, dumps records to
    /// TestContext.WriteLine, and asserts.</para>
    /// </summary>
    public class WorldMapObservabilityTests
    {
        [SetUp]
        public void Setup()
        {
            LogAssert.ignoreFailingMessages = true;
            MessageLog.Clear();
            Diag.ResetAll();
        }

        private const string MinimalBlueprintsJson = @"{
          ""Objects"": [
            { ""Name"": ""Wall"", ""Parts"": [{ ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""true"" }]},{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""#"" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""Solid"", ""Value"": """" },{ ""Key"": ""Wall"", ""Value"": """" }] },
            { ""Name"": ""Floor"", ""Parts"": [{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""."" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""Terrain"", ""Value"": """" }] },
            { ""Name"": ""StoneFloor"", ""Parts"": [{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""."" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""Terrain"", ""Value"": """" }] },
            { ""Name"": ""StoneWall"", ""Parts"": [{ ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""true"" }]},{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""#"" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""Solid"", ""Value"": """" },{ ""Key"": ""Wall"", ""Value"": """" }] },
            { ""Name"": ""Sand"", ""Parts"": [{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""."" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""Terrain"", ""Value"": """" }] },
            { ""Name"": ""SandstoneWall"", ""Parts"": [{ ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""true"" }]},{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""#"" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""Solid"", ""Value"": """" },{ ""Key"": ""Wall"", ""Value"": """" }] },
            { ""Name"": ""Grass"", ""Parts"": [{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""."" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""Terrain"", ""Value"": """" }] },
            { ""Name"": ""VineWall"", ""Parts"": [{ ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""true"" }]},{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""#"" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""Solid"", ""Value"": """" },{ ""Key"": ""Wall"", ""Value"": """" }] },
            { ""Name"": ""Rubble"", ""Parts"": [{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": "","" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""Terrain"", ""Value"": """" }] },
            { ""Name"": ""StairsDown"", ""Parts"": [{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": "">"" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""StairsDown"", ""Value"": """" }] },
            { ""Name"": ""StairsUp"", ""Parts"": [{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""<"" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""StairsUp"", ""Value"": """" }] },
            { ""Name"": ""Tree"", ""Parts"": [{ ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""true"" }]},{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""T"" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""Solid"", ""Value"": """" }] },
            { ""Name"": ""Rock"", ""Parts"": [{ ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""true"" }]},{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""o"" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""Solid"", ""Value"": """" }] },
            { ""Name"": ""Stalagmite"", ""Parts"": [{ ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""true"" }]},{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""^"" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""Solid"", ""Value"": """" }] },
            { ""Name"": ""Pillar"", ""Parts"": [{ ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""true"" }]},{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""I"" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""Solid"", ""Value"": """" }] },
            { ""Name"": ""BrokenColumn"", ""Parts"": [{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": "","" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""Terrain"", ""Value"": """" }] }
          ]
        }";

        private static (OverworldZoneManager zm, Entity player) SetupPlayerOnGround()
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(MinimalBlueprintsJson);
            var zm = new OverworldZoneManager(factory, worldSeed: 42);
            var ground = zm.GetZone("Overworld.10.10.0");
            var player = new Entity { ID = "player", BlueprintName = "Player" };
            player.Tags["Player"] = "";
            player.AddPart(new RenderPart { DisplayName = "you", RenderString = "@" });
            player.AddPart(new PhysicsPart { Solid = false });
            player.AddPart(new WorldMapTravelCostPart { WorldMapStepTurns = 10 });
            ground.AddEntity(player, 40, 12);
            return (zm, player);
        }

        private static void DumpWorldMapRecords(string label)
        {
            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "worldmap",
                Limit = 20,
            }).Records;
            TestContext.WriteLine($"\n=== {label} ===");
            TestContext.WriteLine($"Records: {records.Count}");
            for (int i = 0; i < records.Count; i++)
            {
                var r = records[i];
                TestContext.WriteLine(
                    $"  [{i}] {r.Kind,-10} actor={r.ActorId,-8} :: {r.PayloadJson}");
            }
        }

        [Test]
        public void Ascend_EmitsAscendedDiagWithFromAndToCoords()
        {
            var (zm, player) = SetupPlayerOnGround();
            var ground = zm.GetZone("Overworld.10.10.0");

            WorldMapTraversal.Ascend(player, ground, zm);

            DumpWorldMapRecords("ascend from (40,12) in Overworld.10.10.0");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "worldmap", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("Ascended", records[0].Kind);
            StringAssert.Contains("\"fromZoneID\":\"Overworld.10.10.0\"", records[0].PayloadJson);
            StringAssert.Contains("\"fromZoneX\":40", records[0].PayloadJson);
            StringAssert.Contains("\"toWorldX\":10", records[0].PayloadJson);
            StringAssert.Contains("\"toWorldY\":10", records[0].PayloadJson);
        }

        [Test]
        public void Descend_EmitsDescendedDiagWithCoordsAndSavedFlag()
        {
            var (zm, player) = SetupPlayerOnGround();
            var ground = zm.GetZone("Overworld.10.10.0");

            // Ascend first to set the LastLocationOnSurface
            WorldMapTraversal.Ascend(player, ground, zm);
            Diag.ResetAll();  // discard the Ascended record; focus on Descended

            var worldMap = zm.GetZone(WorldMap.WorldMapZoneID);
            WorldMapTraversal.Descend(player, worldMap, zm);

            DumpWorldMapRecords("descend back to Overworld.10.10.0");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "worldmap", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("Descended", records[0].Kind);
            StringAssert.Contains("\"toZoneID\":\"Overworld.10.10.0\"", records[0].PayloadJson);
            StringAssert.Contains("\"fromWorldX\":10", records[0].PayloadJson);
            StringAssert.Contains("\"usedSavedLocation\":true", records[0].PayloadJson);
        }

        [Test]
        public void WorldMapMove_EmitsSteppedDiagWithWorldCoords()
        {
            var (zm, player) = SetupPlayerOnGround();
            var ground = zm.GetZone("Overworld.10.10.0");
            WorldMapTraversal.Ascend(player, ground, zm);
            Diag.ResetAll();

            var worldMap = zm.GetZone(WorldMap.WorldMapZoneID);
            // Player is at (40,13) = world (10,10). Move east to (41,13) = world (11,10).
            // Fresh TurnManager so AdvanceClock works.
            var tm = new TurnManager();
            tm.AddEntity(player);

            bool moved = MovementSystem.TryMove(player, worldMap, 1, 0);
            Assert.IsTrue(moved);

            DumpWorldMapRecords("step E on worldmap");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "worldmap", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("Stepped", records[0].Kind);
            StringAssert.Contains("\"toWorldX\":11", records[0].PayloadJson);
            StringAssert.Contains("\"toWorldY\":10", records[0].PayloadJson);
            StringAssert.Contains("\"turnsCost\":10", records[0].PayloadJson);
        }

        [Test]
        public void AscendThenStepEastThenWest_AndDescend_EmitsFourRecords()
        {
            // Ascend, step east then west (net zero), descend to the
            // SAME world cell we came from. Avoids triggering a new
            // ground-zone pipeline (which would cascade missing
            // blueprints). Pins the full ascend → step → step →
            // descend sequence + the usedSavedLocation=true flag
            // (we descend to the cell we ascended from).
            var (zm, player) = SetupPlayerOnGround();
            var ground = zm.GetZone("Overworld.10.10.0");
            var tm = new TurnManager();
            tm.AddEntity(player);

            WorldMapTraversal.Ascend(player, ground, zm);
            var worldMap = zm.GetZone(WorldMap.WorldMapZoneID);
            // East then west — net zero, player ends at world (10,10)
            MovementSystem.TryMove(player, worldMap, 1, 0);
            MovementSystem.TryMove(player, worldMap, -1, 0);
            WorldMapTraversal.Descend(player, worldMap, zm);

            DumpWorldMapRecords("full ascend → step E → step W → descend");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "worldmap", Limit = 20,
            }).Records;
            Assert.AreEqual(4, records.Count);
            Assert.AreEqual("Ascended", records[0].Kind);
            Assert.AreEqual("Stepped", records[1].Kind);
            Assert.AreEqual("Stepped", records[2].Kind);
            Assert.AreEqual("Descended", records[3].Kind);
            // Descended back to (10,10) — usedSavedLocation should be true
            StringAssert.Contains("\"fromWorldX\":10", records[3].PayloadJson);
            StringAssert.Contains("\"toZoneID\":\"Overworld.10.10.0\"", records[3].PayloadJson);
            StringAssert.Contains("\"usedSavedLocation\":true", records[3].PayloadJson);
        }
    }
}
