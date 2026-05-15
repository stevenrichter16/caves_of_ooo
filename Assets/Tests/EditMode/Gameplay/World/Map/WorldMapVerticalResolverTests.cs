using NUnit.Framework;
using UnityEngine.TestTools;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WM.8.1 tests for the pure vertical-intent resolver
    /// <see cref="WorldMapTraversal.TryWorldMapVertical"/>. This is
    /// the testable seam the InputHandler stairs-fallback calls when
    /// no stairs are present. Pins:
    /// <list type="bullet">
    ///   <item>Ascend (`&lt;`) from a ground Overworld zone at z==0 →
    ///   transitions to the worldmap zone</item>
    ///   <item>Descend (`&gt;`) from the worldmap zone → transitions
    ///   to the ground zone</item>
    ///   <item>Counter-checks: ascend refused from underground (z&gt;0),
    ///   ascend refused already-on-worldmap, descend refused from a
    ///   ground zone, null-safety</item>
    ///   <item><see cref="WorldMapTravelCostPart"/> is auto-attached
    ///   to the player on the first ascend (so the 10-tick travel
    ///   cost applies without manual wiring)</item>
    /// </list>
    /// </summary>
    public class WorldMapVerticalResolverTests
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
            { ""Name"": ""StairsUp"", ""Parts"": [{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""<"" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""StairsUp"", ""Value"": """" }] },
            { ""Name"": ""SolidEarth"", ""Parts"": [{ ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""true"" }]},{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""#"" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""Solid"", ""Value"": """" },{ ""Key"": ""Wall"", ""Value"": """" }] },
            { ""Name"": ""SandstoneWall"", ""Parts"": [{ ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""true"" }]},{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""#"" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""Solid"", ""Value"": """" },{ ""Key"": ""Wall"", ""Value"": """" }] },
            { ""Name"": ""SandstoneFloor"", ""Parts"": [{ ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""."" }]}], ""Stats"": [], ""Tags"": [{ ""Key"": ""Terrain"", ""Value"": """" }] }
          ]
        }";

        private static OverworldZoneManager NewZoneManager(int seed = 42)
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(MinimalBlueprintsJson);
            return new OverworldZoneManager(factory, worldSeed: seed);
        }

        private static Entity MakePlayer()
        {
            var e = new Entity { ID = "player", BlueprintName = "Player" };
            e.Tags["Player"] = "";
            e.Tags["Creature"] = "";
            e.AddPart(new RenderPart { DisplayName = "you", RenderString = "@" });
            e.AddPart(new PhysicsPart { Solid = false });
            return e;
        }

        // ── Ascend resolution ──────────────────────────────────

        [Test]
        public void TryWorldMapVertical_AscendFromGroundZone_TransitionsToWorldMap()
        {
            var zm = NewZoneManager();
            var ground = zm.GetZone("Overworld.10.10.0");
            var player = MakePlayer();
            ground.AddEntity(player, 40, 12);

            var result = WorldMapTraversal.TryWorldMapVertical(
                player, ground, goingDown: false, zm);

            Assert.IsTrue(result.Success, $"Ascend should succeed: {result.ErrorReason}");
            Assert.AreEqual(WorldMap.WorldMapZoneID, result.NewZone.ZoneID);
        }

        [Test]
        public void TryWorldMapVertical_AscendFromUnderground_Refused()
        {
            // z > 0 is underground — the player should use StairsUp first;
            // ascending to the worldmap directly from underground is wrong.
            // The resolver only reads currentZone.ZoneID, so a bare Zone
            // with the right ID is sufficient (no need to run the
            // underground pipeline + its population blueprints).
            var zm = NewZoneManager();
            var underground = new Zone("Overworld.10.10.1");
            var player = MakePlayer();
            underground.AddEntity(player, 40, 12);

            var result = WorldMapTraversal.TryWorldMapVertical(
                player, underground, goingDown: false, zm);

            Assert.IsFalse(result.Success,
                "Ascend from an underground zone (z>0) must be refused.");
        }

        [Test]
        public void TryWorldMapVertical_AscendAlreadyOnWorldMap_Refused()
        {
            var zm = NewZoneManager();
            var wm = zm.GetZone(WorldMap.WorldMapZoneID);
            var player = MakePlayer();
            wm.AddEntity(player, 40, 13);

            var result = WorldMapTraversal.TryWorldMapVertical(
                player, wm, goingDown: false, zm);

            Assert.IsFalse(result.Success,
                "Cannot ascend further when already on the world map.");
        }

        // ── Descend resolution ─────────────────────────────────

        [Test]
        public void TryWorldMapVertical_DescendFromWorldMap_TransitionsToGround()
        {
            var zm = NewZoneManager();
            var wm = zm.GetZone(WorldMap.WorldMapZoneID);
            var player = MakePlayer();
            wm.AddEntity(player, 40, 13);  // world (10,10)

            var result = WorldMapTraversal.TryWorldMapVertical(
                player, wm, goingDown: true, zm);

            Assert.IsTrue(result.Success, $"Descend should succeed: {result.ErrorReason}");
            Assert.AreEqual("Overworld.10.10.0", result.NewZone.ZoneID);
        }

        [Test]
        public void TryWorldMapVertical_DescendFromGroundZone_Refused()
        {
            // Counter-check: > on a ground zone with no stairs is NOT a
            // worldmap descend (you'd need to be on the worldmap to
            // descend FROM it).
            var zm = NewZoneManager();
            var ground = zm.GetZone("Overworld.10.10.0");
            var player = MakePlayer();
            ground.AddEntity(player, 40, 12);

            var result = WorldMapTraversal.TryWorldMapVertical(
                player, ground, goingDown: true, zm);

            Assert.IsFalse(result.Success,
                "Descend from a ground zone must be refused (not on world map).");
        }

        // ── Null-safety ────────────────────────────────────────

        [Test]
        public void TryWorldMapVertical_NullArgs_FailsCleanly()
        {
            Assert.DoesNotThrow(() =>
                WorldMapTraversal.TryWorldMapVertical(null, null, false, null));
            var r = WorldMapTraversal.TryWorldMapVertical(null, null, false, null);
            Assert.IsFalse(r.Success);
        }

        [Test]
        public void TryWorldMapVertical_NullZone_FailsCleanly()
        {
            var zm = NewZoneManager();
            var player = MakePlayer();
            var r = WorldMapTraversal.TryWorldMapVertical(player, null, false, zm);
            Assert.IsFalse(r.Success);
        }

        // ── Cost-part auto-attach ──────────────────────────────

        [Test]
        public void Ascend_AutoAttachesWorldMapTravelCostPart()
        {
            // The flagship player-facing wiring: after the first ascend
            // the player carries a WorldMapTravelCostPart so subsequent
            // worldmap steps burn the 10-tick travel cost without manual
            // setup in PlayerBuilder.
            var zm = NewZoneManager();
            var ground = zm.GetZone("Overworld.10.10.0");
            var player = MakePlayer();
            ground.AddEntity(player, 40, 12);
            Assert.IsNull(player.GetPart<WorldMapTravelCostPart>(),
                "Player should NOT have the cost part before first ascend.");

            WorldMapTraversal.TryWorldMapVertical(player, ground, goingDown: false, zm);

            Assert.IsNotNull(player.GetPart<WorldMapTravelCostPart>(),
                "Ascend should auto-attach WorldMapTravelCostPart.");
            // Idempotent: a second ascend round-trip does not add a 2nd.
            var wm = zm.GetZone(WorldMap.WorldMapZoneID);
            WorldMapTraversal.TryWorldMapVertical(player, wm, goingDown: true, zm);
            var ground2 = zm.GetZone("Overworld.10.10.0");
            WorldMapTraversal.TryWorldMapVertical(player, ground2, goingDown: false, zm);
            int count = 0;
            foreach (var p in player.Parts)
                if (p is WorldMapTravelCostPart) count++;
            Assert.AreEqual(1, count, "Cost part must not be duplicated across ascends.");
        }
    }
}
