using NUnit.Framework;
using UnityEngine.TestTools;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WM.5 tests for the Visited[,] fog-of-war flag on
    /// <see cref="WorldMap"/>. Pins:
    /// <list type="bullet">
    ///   <item>Fresh WorldMap has all-false Visited array</item>
    ///   <item><see cref="WorldMap.MarkVisited"/> flips the flag;
    ///   out-of-bounds is silent no-op</item>
    ///   <item><see cref="WorldMap.IsVisited"/> reads correctly;
    ///   out-of-bounds returns false</item>
    ///   <item><see cref="WorldMapTraversal.Ascend"/> marks the
    ///   destination world-cell visited</item>
    ///   <item>Save/load round-trip preserves the Visited pattern</item>
    /// </list>
    /// </summary>
    public class WorldMapVisitedTests
    {
        [SetUp]
        public void Setup()
        {
            LogAssert.ignoreFailingMessages = true;
        }

        private const string MinimalBlueprintsJson = @"{
          ""Objects"": [
            { ""Name"": ""Wall"", ""Parts"": [{ ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""true"" }]},{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""#"" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""Solid"", ""Value"": """" },{ ""Key"": ""Wall"", ""Value"": """" }] },
            { ""Name"": ""Floor"", ""Parts"": [{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""."" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""Terrain"", ""Value"": """" }] },
            { ""Name"": ""StoneFloor"", ""Parts"": [{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""."" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""Terrain"", ""Value"": """" }] },
            { ""Name"": ""StoneWall"", ""Parts"": [{ ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""true"" }]},{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""#"" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""Solid"", ""Value"": """" },{ ""Key"": ""Wall"", ""Value"": """" }] },
            { ""Name"": ""Rubble"", ""Parts"": [{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": "","" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""Terrain"", ""Value"": """" }] },
            { ""Name"": ""StairsDown"", ""Parts"": [{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": "">"" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""StairsDown"", ""Value"": """" }] },
            { ""Name"": ""StairsUp"", ""Parts"": [{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""<"" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""StairsUp"", ""Value"": """" }] }
          ]
        }";

        // ── Pure WorldMap unit tests ───────────────────────────

        [Test]
        public void FreshWorldMap_HasAllVisitedFalse()
        {
            var map = new WorldMap(seed: 1);
            for (int y = 0; y < WorldMap.Height; y++)
                for (int x = 0; x < WorldMap.Width; x++)
                    Assert.IsFalse(map.IsVisited(x, y), $"({x},{y}) should default to unvisited.");
        }

        [Test]
        public void MarkVisited_FlipsFlag()
        {
            var map = new WorldMap(seed: 1);
            map.MarkVisited(5, 7);
            Assert.IsTrue(map.IsVisited(5, 7));
            // Counter-check: neighbors stay false
            Assert.IsFalse(map.IsVisited(4, 7));
            Assert.IsFalse(map.IsVisited(5, 8));
        }

        [Test]
        public void MarkVisited_OutOfBounds_NoOp()
        {
            var map = new WorldMap(seed: 1);
            Assert.DoesNotThrow(() => map.MarkVisited(-1, 0));
            Assert.DoesNotThrow(() => map.MarkVisited(20, 5));
            Assert.DoesNotThrow(() => map.MarkVisited(5, -3));
            Assert.DoesNotThrow(() => map.MarkVisited(5, 100));
            // Nothing should have been flipped
            for (int y = 0; y < WorldMap.Height; y++)
                for (int x = 0; x < WorldMap.Width; x++)
                    Assert.IsFalse(map.IsVisited(x, y));
        }

        [Test]
        public void IsVisited_OutOfBounds_ReturnsFalse()
        {
            var map = new WorldMap(seed: 1);
            Assert.IsFalse(map.IsVisited(-1, 0));
            Assert.IsFalse(map.IsVisited(20, 5));
            Assert.IsFalse(map.IsVisited(5, -3));
        }

        [Test]
        public void MarkVisited_Idempotent()
        {
            var map = new WorldMap(seed: 1);
            map.MarkVisited(8, 8);
            map.MarkVisited(8, 8);
            map.MarkVisited(8, 8);
            Assert.IsTrue(map.IsVisited(8, 8));
        }

        // ── Ascend marks visited ───────────────────────────────

        [Test]
        public void Ascend_MarksDestinationWorldCellVisited()
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(MinimalBlueprintsJson);
            var zm = new OverworldZoneManager(factory, worldSeed: 42);

            // Before ascend: cell (10,10) is unvisited
            Assert.IsFalse(zm.WorldMap.IsVisited(10, 10),
                "Cell should be unvisited on a fresh map.");

            var ground = zm.GetZone("Overworld.10.10.0");
            var player = new Entity { ID = "p", BlueprintName = "Player" };
            player.Tags["Player"] = "";
            player.AddPart(new RenderPart { DisplayName = "you" });
            player.AddPart(new PhysicsPart { Solid = false });
            ground.AddEntity(player, 40, 12);

            WorldMapTraversal.Ascend(player, ground, zm);

            Assert.IsTrue(zm.WorldMap.IsVisited(10, 10),
                "Ascend should mark the world-cell visited.");
            // Counter-check: neighbors NOT visited
            Assert.IsFalse(zm.WorldMap.IsVisited(11, 10));
            Assert.IsFalse(zm.WorldMap.IsVisited(10, 11));
        }

        // ── Save / load round-trip ─────────────────────────────

        [Test]
        public void SaveLoad_PreservesVisitedBitmap()
        {
            // Round-trip a WorldMap with a specific visited pattern
            // through the SaveWriter / SaveReader and confirm Visited
            // survives.
            var src = new WorldMap(seed: 1234);
            // Fill some cells
            src.MarkVisited(0, 0);
            src.MarkVisited(10, 10);
            src.MarkVisited(19, 19);
            src.MarkVisited(7, 13);

            var factory = new EntityFactory();
            factory.LoadBlueprints(MinimalBlueprintsJson);
            var sourceMgr = new OverworldZoneManager(factory, worldSeed: 1234);
            // Replace the regenerated map with our hand-pattern map
            // so the round-trip carries our Visited bits.
            sourceMgr.ReplaceLoadedOverworldState(src, sourceMgr.SettlementManager, null);

            using var ms = new System.IO.MemoryStream();
            var writer = new SaveWriter(ms);
            writer.WriteHeader("test-version");
            SaveGraphSerializer.SaveOverworldZoneManager(sourceMgr, writer);

            ms.Position = 0;
            var reader = new SaveReader(ms, factory);
            reader.ReadHeader();
            var loaded = SaveGraphSerializer.LoadOverworldZoneManager(reader);

            Assert.IsNotNull(loaded);
            Assert.IsTrue(loaded.WorldMap.IsVisited(0, 0));
            Assert.IsTrue(loaded.WorldMap.IsVisited(10, 10));
            Assert.IsTrue(loaded.WorldMap.IsVisited(19, 19));
            Assert.IsTrue(loaded.WorldMap.IsVisited(7, 13));
            // Counter-check: at least one known-unvisited cell stays false
            Assert.IsFalse(loaded.WorldMap.IsVisited(5, 5));
            Assert.IsFalse(loaded.WorldMap.IsVisited(15, 0));
        }

        [Test]
        public void SaveLoad_AllUnvisited_PreservesAllFalse()
        {
            // Counter-check: a fresh map's all-false Visited array
            // round-trips correctly (no false-positives).
            var factory = new EntityFactory();
            factory.LoadBlueprints(MinimalBlueprintsJson);
            var sourceMgr = new OverworldZoneManager(factory, worldSeed: 999);

            using var ms = new System.IO.MemoryStream();
            var writer = new SaveWriter(ms);
            writer.WriteHeader("test-version");
            SaveGraphSerializer.SaveOverworldZoneManager(sourceMgr, writer);

            ms.Position = 0;
            var reader = new SaveReader(ms, factory);
            reader.ReadHeader();
            var loaded = SaveGraphSerializer.LoadOverworldZoneManager(reader);

            for (int y = 0; y < WorldMap.Height; y++)
                for (int x = 0; x < WorldMap.Width; x++)
                    Assert.IsFalse(loaded.WorldMap.IsVisited(x, y),
                        $"Cell ({x},{y}) should be unvisited after fresh-map round-trip.");
        }
    }
}
