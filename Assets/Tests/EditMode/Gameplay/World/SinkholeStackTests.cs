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
        public void TheAuthoredMouths_AreSinkholePOIs()
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

        [TestCase(4, 6, true)]
        [TestCase(12, 3, false)]
        public void EachDepth_RoutesToItsOwnPipeline(int x, int y, bool authoredOlderdeep)
        {
            // The 🔴: before this, z=1 and z=2 hit the generic
            // underground pipeline and the sinkhole may as well not
            // have existed below its lip.
            var mgr = new ExposingManager(_factory);
            var name = mgr.WorldMap.GetPOI(x, y).Name;

            var mouth = mgr.Pipe($"Overworld.{x}.{y}.0");
            Assert.IsTrue(Carries<SinkholeMouthBuilder>(mouth),
                $"{name} z=0 is a mouth — a void in the world, with a lip");

            var descent = mgr.Pipe($"Overworld.{x}.{y}.1");
            Assert.AreEqual(authoredOlderdeep, Carries<OlderdeepCompositionBuilder>(descent),
                "Olderdeep uses its authored descent; ordinary mouths keep their native descent builder.");
            Assert.AreEqual(!authoredOlderdeep, Carries<SinkholeDescentBuilder>(descent),
                "The authored and ordinary bases must not both stamp the same descent.");
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
            // Close-out 🔵 — this used to build 30 worlds PER MOUTH
            // (150 generations); one world per seed answers for all
            // five mouths at once.
            for (int seed = 1; seed <= 30; seed++)
            {
                var mgr = new OverworldZoneManager(_factory, worldSeed: seed);
                foreach (var (name, x, y) in SinkholeSites.All)
                {
                    var poi = mgr.WorldMap.GetPOI(x, y);
                    Assert.AreEqual(POIType.Sinkhole, poi?.Type,
                        $"seed {seed}: something else claimed {name}");
                }
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
            // Close-out hypothesis H13 — this guarantee was proven
            // only for All[0]; the other mouths sit in DIFFERENT biomes
            // whose wilderness recipes carry different builders, so
            // "the roll missed for Olderdeep" said nothing about
            // Lampwell (Spread) or Spivenor (Sodden). Every mouth, a
            // few seeds each.
            foreach (var (name, x, y) in SinkholeSites.All)
                for (int seed = 1; seed <= 4; seed++)
                {
                    var mgr = new OverworldZoneManager(_factory, worldSeed: seed);
                    var zone = mgr.GetZone($"Overworld.{x}.{y}.0");
                    int down = 0;
                    foreach (var e in zone.GetAllEntities())
                        if (e.GetPart<StairsDownPart>() != null) down++;
                    Assert.AreEqual(1, down,
                        $"{name} seed {seed}: a sinkhole's mouth IS the way down — " +
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
            var (_, x, y) = SinkholeSites.All[2];   // Lampwell — a village
            var floor = mgr.Pipe($"Overworld.{x}.{y}.2");
            Assert.IsTrue(Carries<StrandedSettlementBuilder>(floor),
                "z=2 is the floor, and it has the archetype's content");
            var below = mgr.Pipe($"Overworld.{x}.{y}.3");
            Assert.IsFalse(Carries<StrandedSettlementBuilder>(below),
                "and z=3 is not a second copy of it");
            Assert.IsFalse(Carries<DrownedSimaBuilder>(below),
                "nor any other floor archetype");
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

        // ════════════════════════════════════════════════════════════
        //   W5.7 close-out — the stack's contracts under pressure
        // ════════════════════════════════════════════════════════════

        [Test]
        public void TheVoid_IsReservedAgainstAmbientStamps()
        {
            // Close-out 🟡 — the biome's LandmarkBuilder runs at 3800,
            // AFTER the mouth, and its FootprintClear happily accepted
            // the cleared void: a Choir shrine (five NPCs, a campfire)
            // could generate standing INSIDE the hole. The ellipse
            // joins GenReservedCells, the same fence the village square
            // uses.
            var mgr = new OverworldZoneManager(_factory, worldSeed: 42);
            var (name0, x0, y0) = SinkholeSites.All[0];
            var zone = mgr.GetZone($"Overworld.{x0}.{y0}.0");
            int reservedInVoid = 0;
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == "SinkholeLip")
                {
                    var pos = zone.GetEntityPosition(e);
                    if (zone.GenReservedCells.Contains((pos.x, pos.y)))
                        reservedInVoid++;
                }
            Assert.Greater(reservedInVoid, 12,
                name0 + ": the hole and its lip are fenced against later spawners");
        }

        [Test]
        public void TheCacheLedge_HoldsWhatTheyPacked()
        {
            // Close-out 🔵 — canon, the plan and the docstring all
            // promised a cache ledge and the expedition; nothing built
            // one. Now the lowest terrace holds the sack of supplies
            // they did not get to use.
            var mgr = new OverworldZoneManager(_factory, worldSeed: 42);
            var (_, x, y) = SinkholeSites.All[0];
            mgr.GetZone($"Overworld.{x}.{y}.0");
            var descent = mgr.GetZone($"Overworld.{x}.{y}.1");

            foreach (var e in descent.GetAllEntities())
                if (e.BlueprintName == "Sack")
                {
                    var hold = e.GetPart<ContainerPart>();
                    Assert.IsNotNull(hold);
                    Assert.GreaterOrEqual(hold.Contents.Count, 3,
                        "torch, food, tonic — the kit of somebody who meant to climb out");
                    return;
                }
            Assert.Fail("the cache exists on the descent");
        }

        [Test]
        public void ArrivingLaterally_TheFloorStillHasItsWayUp()
        {
            // Close-out hypothesis H1 — a floor reached by walking
            // underground from the next chunk over used to generate
            // BEFORE its descent existed; StairsUpBuilder read the
            // connection registry, found nothing, and the floor was
            // born with no way up — a soft-lock in a recoverable-death
            // RPG. The stack now generates top-down: asking for z=2
            // cold generates z=0 and z=1 first.
            var mgr = new OverworldZoneManager(_factory, worldSeed: 77);
            var (name0, x0, y0) = SinkholeSites.All[0];
            var floor = mgr.GetZone($"Overworld.{x0}.{y0}.2");   // COLD — no z0/z1 call first

            int up = 0;
            foreach (var e in floor.GetAllEntities())
                if (e.GetPart<StairsUpPart>() != null) up++;
            Assert.GreaterOrEqual(up, 1,
                name0 + ": however you got down here, the way back up exists");
        }

        [Test]
        public void RegisteringTheSameStairsTwice_KeepsOneConnection()
        {
            // Close-out 🔵 — RegisterConnection appended blindly, and a
            // zone that regenerates re-registers its stairs: one
            // duplicate per unload/regen cycle, saved forever, and one
            // extra StairsUp entity placed per duplicate.
            var mgr = new OverworldZoneManager(_factory, worldSeed: 42);
            var conn = new ZoneConnection
            {
                SourceZoneID = "Overworld.9.9.0", SourceX = 5, SourceY = 5,
                TargetZoneID = "Overworld.9.9.1", TargetX = 5, TargetY = 5,
                Type = "StairsDown",
            };
            var again = new ZoneConnection
            {
                SourceZoneID = "Overworld.9.9.0", SourceX = 5, SourceY = 5,
                TargetZoneID = "Overworld.9.9.1", TargetX = 5, TargetY = 5,
                Type = "StairsDown",
            };
            mgr.RegisterConnection(conn);
            mgr.RegisterConnection(again);
            Assert.AreEqual(1,
                mgr.GetConnectionsTo("Overworld.9.9.1", "StairsDown").Count,
                "same stairs, one connection — regeneration is not accretion");
        }

        [Test]
        public void UnloadingAZone_DropsTheConnectionsItOwns()
        {
            // The other half of the dedup contract: an unloaded zone
            // will re-register on regeneration, so its OWN connections
            // leave with it; connections INTO it from other zones stay.
            var mgr = new OverworldZoneManager(_factory, worldSeed: 42);
            mgr.RegisterConnection(new ZoneConnection
            {
                SourceZoneID = "Overworld.9.9.0", SourceX = 5, SourceY = 5,
                TargetZoneID = "Overworld.9.9.1", TargetX = 5, TargetY = 5,
                Type = "StairsDown",
            });
            mgr.RegisterConnection(new ZoneConnection
            {
                SourceZoneID = "Overworld.8.9.0", SourceX = 2, SourceY = 2,
                TargetZoneID = "Overworld.9.9.0", TargetX = 3, TargetY = 3,
                Type = "StairsDown",
            });
            mgr.UnloadZone("Overworld.9.9.0");
            Assert.AreEqual(0,
                mgr.GetConnectionsTo("Overworld.9.9.1", "StairsDown").Count,
                "the unloaded zone took its own stairs with it");
            Assert.AreEqual(1,
                mgr.GetConnectionsTo("Overworld.9.9.0", "StairsDown").Count,
                "but the neighbour's stairs into it survive");
        }

        [Test]
        public void ALoadedWorld_GrowsMouthsAuthoredAfterItWasSaved()
        {
            // Close-out 🟡 — the POI stream is the only source on load,
            // and SinkholeSites.All was consumed only by fresh worldgen:
            // a pre-W5.6 save never grew Ginmere, so the Drowned Sima
            // stayed orphaned in every existing world. Same heal as the
            // W4.7 profile rehydration: the table is the source of truth.
            var map = WorldGenerator.Generate(42);
            var (nameG, xg, yg) = SinkholeSites.All[SinkholeSites.All.Length - 1];
            map.SetPOI(xg, yg, null);                       // a pre-W5.6 save
            map.RehydrateAuthoredSinkholes();
            var poi = map.GetPOI(xg, yg);
            Assert.IsNotNull(poi, nameG + " grew back on load");
            Assert.AreEqual(POIType.Sinkhole, poi.Type);
            Assert.AreEqual(nameG, poi.Name);
        }

        [Test]
        public void Rehydration_NeverDisplacesWhatIsThere()
        {
            // Counter-check: a cell that HOLDS something — whatever the
            // save put there — is never overwritten by the table.
            var map = WorldGenerator.Generate(42);
            var (nameG, xg, yg) = SinkholeSites.All[0];
            var squatter = new PointOfInterest(POIType.Lair, "squatter", null, 2);
            map.SetPOI(xg, yg, squatter);
            map.RehydrateAuthoredSinkholes();
            Assert.AreSame(squatter, map.GetPOI(xg, yg),
                "an occupied cell is the save's business, not the table's");
        }

    }
}
