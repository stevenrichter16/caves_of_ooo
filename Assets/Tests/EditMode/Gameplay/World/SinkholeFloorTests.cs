using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W5.3 (Docs/FELLING-W5-PLAN.md §3) — the floor is a different
    /// place per sinkhole. Canon: "one archetype per sinkhole (rolled at
    /// worldgen, then FIXED — the world persists)".
    ///
    /// <para><b>R3 honored:</b> the archetype is a pure function of the
    /// sinkhole's identity, never the builder rng (pipeline-retry
    /// reseeding) and never a saved field — so it is fixed for free and
    /// survives a load without a format bump. The W4.7 profile lesson,
    /// applied before it can bite.</para>
    ///
    /// <para>Where canon NAMED a place, canon wins: the Deepest
    /// Cathedral is a Choir Cathedral because it is called that. The
    /// roll is the fallback for holes nobody named.</para>
    /// </summary>
    public class SinkholeFloorTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        // ════════════════════════════════════════════════════════════
        //   The archetype: authored where named, pure where not
        // ════════════════════════════════════════════════════════════

        [Test]
        public void CanonsNamedHoles_GetTheFloorTheirNameClaims()
        {
            Assert.AreEqual(SinkholeArchetype.ChoirCathedral,
                SinkholeArchetypes.For("the Deepest Cathedral"),
                "a place called the Deepest Cathedral is a cathedral");
            // W5.4 sweep correction: Lampwell and Spivenor are canon
            // catacomb hubs, not simas — see CatacombVillageTests.
            // DrownedSima remains a real archetype, reachable by the
            // hash for holes nobody named, and tested directly below.
            Assert.AreEqual(SinkholeArchetype.StrandedSettlement,
                SinkholeArchetypes.For("Lampwell"),
                "the bioluminescent catacomb hub");
            Assert.AreEqual(SinkholeArchetype.StrandedSettlement,
                SinkholeArchetypes.For("Olderdeep"),
                "somebody is down there, and has been a long time");
        }

        [Test]
        public void TheArchetype_IsFixedForever()
        {
            // Pure function: same answer every call, every session, with
            // no saved field to drift or be dropped by a format bump.
            for (int i = 0; i < 5; i++)
                Assert.AreEqual(SinkholeArchetypes.For("Spivenor"),
                                SinkholeArchetypes.For("Spivenor"));
        }

        [Test]
        public void AnUnnamedHole_StillGetsAStableFloor()
        {
            // The fallback roll, for holes a later phase adds without
            // authoring: stable, and a real archetype.
            var a = SinkholeArchetypes.For("some-unnamed-hole");
            Assert.AreEqual(a, SinkholeArchetypes.For("some-unnamed-hole"));
            CollectionAssert.Contains(
                new[] { SinkholeArchetype.DrownedSima,
                        SinkholeArchetype.StrandedSettlement,
                        SinkholeArchetype.ChoirCathedral }, a);
        }

        // ════════════════════════════════════════════════════════════
        //   The Drowned Sima
        // ════════════════════════════════════════════════════════════

        private static Zone FloorOf(string name)
        {
            var mgr = new OverworldZoneManager(_factory, worldSeed: 42);
            foreach (var site in SinkholeSites.All)
                if (site.Name == name)
                {
                    mgr.GetZone($"Overworld.{site.X}.{site.Y}.0");
                    mgr.GetZone($"Overworld.{site.X}.{site.Y}.1");
                    return mgr.GetZone($"Overworld.{site.X}.{site.Y}.2");
                }
            return null;
        }

        /// <summary>Build a sima floor directly. All four AUTHORED
        /// sinkholes are catacomb villages or the Cathedral (canon), so
        /// the Drowned Sima has no named instance in the shipped map —
        /// it is reached by the hash for unnamed holes. Its content is
        /// therefore tested on its own builder rather than through a
        /// named place.</summary>
        private static Zone SimaFloor(int seed = 5)
        {
            var zone = new Zone("Overworld.9.9.2");
            for (int x = 1; x < Zone.Width - 1; x++)
                for (int y = 1; y < Zone.Height - 1; y++)
                {
                    var f = _factory.CreateEntity("StoneFloor");
                    if (f != null) zone.AddEntity(f, x, y);
                }
            new DrownedSimaBuilder().BuildZone(zone, _factory, new System.Random(seed));
            return zone;
        }

        private static int CountOf(Zone zone, string blueprint)
        {
            int n = 0;
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == blueprint) n++;
            return n;
        }

        [Test]
        public void TheDrownedSima_HasStandingWater_AndWhatLivesInIt()
        {
            // The survey's ground truth: standing water, and the frogs
            // that were the reason anybody wrote the survey down.
            var zone = SimaFloor();
            Assert.Greater(CountOf(zone, "MirePool"), 20,
                "the floor of a sima is mostly water");
            Assert.GreaterOrEqual(CountOf(zone, "GinFrog"), 2,
                "and the frogs the survey came for");
        }

        [Test]
        public void TheDrownedSima_IsStillSomewhereYouCanStand()
        {
            // A floor that is entirely water is a screenshot, not a
            // room. Dry ground must remain.
            var zone = SimaFloor();
            int dry = 0;
            for (int x = 1; x < Zone.Width - 1; x++)
                for (int y = 1; y < Zone.Height - 1; y++)
                {
                    if (!FormationReachability.IsOpenGround(zone, x, y)) continue;
                    var cell = zone.GetCell(x, y);
                    bool wet = false;
                    foreach (var e in cell.Objects)
                        if (e.BlueprintName == "MirePool") wet = true;
                    if (!wet) dry++;
                }
            Assert.Greater(dry, 100, "there is bank to stand on");
        }

        [Test]
        public void ACathedralFloor_IsNotASima()
        {
            // Counter-check: the archetype actually branches. The
            // Cathedral's own content is W5.5; what matters here is that
            // it did NOT get the sima's.
            var zone = FloorOf("the Deepest Cathedral");
            Assert.IsNotNull(zone);
            Assert.AreEqual(0, CountOf(zone, "GinFrog"),
                "no frogs in the vault");
        }
    }
}
