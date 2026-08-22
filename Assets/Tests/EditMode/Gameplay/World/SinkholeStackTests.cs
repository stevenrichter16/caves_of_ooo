using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W5.1 (Docs/FELLING-W5-PLAN.md §3) — the sinkhole stack. A hole
    /// in the world you can climb into: the Mouth you find (z=0), the
    /// Descent you survive (z=1), the Floor that is a different place
    /// each time (z=2).
    ///
    /// <para>The sweep's 🔴 lives here: <c>GetPipelineForZone</c>
    /// returned the generic underground pipeline for ANY z &gt; 0
    /// BEFORE reading the POI, so a sinkhole's own depths generated as
    /// anonymous caves. POI identity must reach every level.</para>
    /// </summary>
    public class SinkholeStackTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        private sealed class ExposingManager : OverworldZoneManager
        {
            public ExposingManager(EntityFactory f, int seed = 42) : base(f, seed) { }
            public ZoneGenerationPipeline Pipe(string id) => GetPipelineForZone(id);
        }

        private static bool Carries<T>(ZoneGenerationPipeline p) where T : IZoneBuilder
        {
            foreach (var b in p.Builders) if (b is T) return true;
            return false;
        }

        // ════════════════════════════════════════════════════════════
        //   Routing — POI identity reaches every depth
        // ════════════════════════════════════════════════════════════

        [Test]
        public void TheFourMouths_AreSinkholePOIs()
        {
            var mgr = new OverworldZoneManager(_factory, worldSeed: 42);
            foreach (var (name, x, y) in SinkholeSites.All)
            {
                var poi = mgr.WorldMap.GetPOI(x, y);
                Assert.IsNotNull(poi, $"{name} ({x},{y}) is on the map");
                Assert.AreEqual(POIType.Sinkhole, poi.Type, name);
                Assert.AreEqual(name, poi.Name);
            }
        }

        [Test]
        public void EachDepth_RoutesToItsOwnPipeline()
        {
            // The 🔴: before this, z=1 and z=2 hit the generic
            // underground pipeline and the sinkhole may as well not
            // have existed below its lip.
            var mgr = new ExposingManager(_factory);
            var (name, x, y) = SinkholeSites.All[0];

            var mouth = mgr.Pipe($"Overworld.{x}.{y}.0");
            Assert.IsTrue(Carries<SinkholeMouthBuilder>(mouth),
                $"{name} z=0 is a mouth — a void in the world, with a lip");

            var descent = mgr.Pipe($"Overworld.{x}.{y}.1");
            Assert.IsTrue(Carries<SinkholeDescentBuilder>(descent),
                "z=1 is the descent — ledges, anchors, and the way down");
            Assert.IsFalse(Carries<SinkholeMouthBuilder>(descent),
                "and not a second lip");
        }

        [Test]
        public void OrdinaryUnderground_StillRoutesGenerically()
        {
            // Counter-check: the cave under Sill is still a cave.
            var mgr = new ExposingManager(_factory);
            var deep = mgr.Pipe("Overworld.10.10.1");
            Assert.IsFalse(Carries<SinkholeMouthBuilder>(deep));
            Assert.IsFalse(Carries<SinkholeDescentBuilder>(deep));
            Assert.IsTrue(Carries<SolidEarthBuilder>(deep),
                "ordinary depth is still solid earth and strata");
        }

        [Test]
        public void MouthCells_AreReservedFromOpportunisticPOIs()
        {
            // The W4.7 lesson applied before it can bite: a rolled lair
            // must never take a mouth cell and delete the hole.
            foreach (var (name, x, y) in SinkholeSites.All)
                for (int seed = 1; seed <= 30; seed++)
                {
                    var mgr = new OverworldZoneManager(_factory, worldSeed: seed);
                    var poi = mgr.WorldMap.GetPOI(x, y);
                    Assert.AreEqual(POIType.Sinkhole, poi?.Type,
                        $"seed {seed}: something else claimed {name}");
                }
        }

        // ════════════════════════════════════════════════════════════
        //   The mouth, and the way down
        // ════════════════════════════════════════════════════════════

        [Test]
        public void TheMouth_IsAVoid_WithAWayDown()
        {
            var mgr = new OverworldZoneManager(_factory, worldSeed: 42);
            var (name, x, y) = SinkholeSites.All[0];
            string mouthId = $"Overworld.{x}.{y}.0";
            var zone = mgr.GetZone(mouthId);
            Assert.IsNotNull(zone);

            int lip = 0, stairs = 0;
            foreach (var e in zone.GetAllEntities())
            {
                if (e.BlueprintName == "SinkholeLip") lip++;
                if (e.GetPart<StairsDownPart>() != null) stairs++;
            }
            Assert.Greater(lip, 12, "the lip rings the hole");
            Assert.AreEqual(1, stairs, "one way down, and it is findable");

            // The descent below must be registered, not merely implied.
            var conn = mgr.GetConnections(mouthId);
            Assert.IsNotNull(conn, "the way down is registered");
            bool down = false;
            foreach (var c in conn)
                if (c.Type == "StairsDown" && c.TargetZoneID == $"Overworld.{x}.{y}.1")
                    down = true;
            Assert.IsTrue(down, "and it leads into this sinkhole's own descent");
        }

        [Test]
        public void TheMouth_IsStillWalkable()
        {
            // A void in the world must not seal the zone: the ring is a
            // ring, and the rest of the chunk stays crossable.
            var mgr = new OverworldZoneManager(_factory, worldSeed: 42);
            var (_, x, y) = SinkholeSites.All[0];
            var zone = mgr.GetZone($"Overworld.{x}.{y}.0");
            var reached = FormationReachability.FloodFromWest(zone, out bool crossed);
            Assert.IsTrue(crossed, "you can still walk past a hole");
        }

        // ════════════════════════════════════════════════════════════
        //   Cold-eye pass — hypotheses about the shape, not the code
        // ════════════════════════════════════════════════════════════

        [Test]
        public void TheHole_IsTheOnlyWayDown_AtEverySeed()
        {
            // H10: the mouth pipeline reuses the biome's wilderness,
            // which carries CaveEntranceBuilder — so a sinkhole mouth
            // could sprout a SECOND, random staircase into the same
            // descent. The original "exactly 1" pin passed only because
            // the roll missed at seed 42; this sweeps seeds so the
            // guarantee is real rather than lucky.
            var (_, x, y) = SinkholeSites.All[0];
            for (int seed = 1; seed <= 12; seed++)
            {
                var mgr = new OverworldZoneManager(_factory, worldSeed: seed);
                var zone = mgr.GetZone($"Overworld.{x}.{y}.0");
                int down = 0;
                foreach (var e in zone.GetAllEntities())
                    if (e.GetPart<StairsDownPart>() != null) down++;
                Assert.AreEqual(1, down,
                    $"seed {seed}: a sinkhole's mouth IS the way down — " +
                    "a second random cave entrance beside it is nonsense");
            }
        }

        [Test]
        public void BelowTheFloor_IsNotAnotherCopyOfTheFloor()
        {
            // H9: the routing sent EVERY level below the descent through
            // the archetype branch, and StairsDownBuilder gives each one
            // a way further down — so a Drowned Sima had a Drowned Sima
            // under it, forever. Canon: the Floor is ONE level (z=2),
            // and "Z=3+ CATACOMBS / ROOTWAYS — below the floors, where
            // placed" is different content.
            var mgr = new ExposingManager(_factory);
            var (_, x, y) = SinkholeSites.All[2];   // Lampwell — a sima
            var floor = mgr.Pipe($"Overworld.{x}.{y}.2");
            Assert.IsTrue(Carries<DrownedSimaBuilder>(floor),
                "z=2 is the floor");
            var below = mgr.Pipe($"Overworld.{x}.{y}.3");
            Assert.IsFalse(Carries<DrownedSimaBuilder>(below),
                "and z=3 is not a second one");
        }

        // ════════════════════════════════════════════════════════════
        //   The descent
        // ════════════════════════════════════════════════════════════

        [Test]
        public void TheDescent_IsLedgedAndSurvivable()
        {
            // Generate the MOUTH first — that is how a descent is
            // reached, and StairsUpBuilder derives the way back out from
            // the connection the mouth registers. Testing the descent in
            // isolation would test a zone no player can be standing in.
            var mgr = new OverworldZoneManager(_factory, worldSeed: 42);
            var (_, x, y) = SinkholeSites.All[0];
            mgr.GetZone($"Overworld.{x}.{y}.0");
            var zone = mgr.GetZone($"Overworld.{x}.{y}.1");
            Assert.IsNotNull(zone);

            int anchors = 0, ledges = 0, up = 0, down = 0;
            foreach (var e in zone.GetAllEntities())
            {
                if (e.BlueprintName == "RopeAnchor") anchors++;
                if (e.BlueprintName == "DescentLedge") ledges++;
                if (e.GetPart<StairsUpPart>() != null) up++;
                if (e.GetPart<StairsDownPart>() != null) down++;
            }
            Assert.Greater(ledges, 5, "ledges to stand on");
            Assert.GreaterOrEqual(anchors, 1, "somebody came this way before you");
            Assert.AreEqual(1, up, "and you can climb back out");
            Assert.AreEqual(1, down, "the floor is below");
        }
    }
}
