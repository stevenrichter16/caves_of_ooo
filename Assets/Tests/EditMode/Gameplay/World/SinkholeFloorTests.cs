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

        /// <summary>Build a sima floor directly — the builder in
        /// isolation, complementing the end-to-end Ginmere tests below.
        /// (Historical note: between W5.4 and W5.6 the Drowned Sima had
        /// no named instance and this fixture was its ONLY coverage;
        /// Ginmere closed that gap.)</summary>
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

        // ════════════════════════════════════════════════════════════
        //   W5.6 — every shipped archetype exists somewhere
        // ════════════════════════════════════════════════════════════

        [Test]
        public void EveryShippedArchetype_HasAPlaceInTheWorld()
        {
            // The structural pin that would have caught the W5.4
            // orphaning: the canon correction reassigned Lampwell and
            // Spivenor to StrandedSettlement, which left the Drowned
            // Sima — all of W5.3's floor — reachable only "by the hash
            // for unnamed holes"… and the shipped world contains no
            // unnamed holes. An archetype the player cannot reach is
            // not shipped; it is stored.
            foreach (SinkholeArchetype archetype in
                     System.Enum.GetValues(typeof(SinkholeArchetype)))
            {
                bool placed = false;
                foreach (var (name, _, _) in SinkholeSites.All)
                    if (SinkholeArchetypes.For(name) == archetype) placed = true;
                Assert.IsTrue(placed,
                    archetype + " is a shipped floor with no mouth that " +
                    "leads to it — unreachable content");
            }
        }

        [Test]
        public void Ginmere_IsTheDrownedSima_EndToEnd()
        {
            // Not the builder in isolation (that is the SimaFloor
            // fixture above) — the whole stack: world map POI → mouth →
            // descent → floor, through the real routing.
            var zone = FloorOf("Ginmere");
            Assert.IsNotNull(zone, "Ginmere generates a floor");
            Assert.Greater(CountOf(zone, "MirePool"), 20,
                "and the floor is the sima's standing water");
            Assert.GreaterOrEqual(CountOf(zone, "GinFrog"), 2,
                "with the frogs the survey came for");
        }

        [Test]
        public void Ginmere_IsNotAVillage()
        {
            // Counter-check: adding the fifth mouth must not have
            // disturbed the other four's assignments — and Ginmere
            // itself must not roll a hearth.
            var zone = FloorOf("Ginmere");
            Assert.IsNotNull(zone);
            Assert.AreEqual(0, CountOf(zone, "HearthPatch"),
                "a sima has frogs, not a fire");
            Assert.AreEqual(SinkholeArchetype.StrandedSettlement,
                SinkholeArchetypes.For("Lampwell"));
            Assert.AreEqual(SinkholeArchetype.ChoirCathedral,
                SinkholeArchetypes.For("the Deepest Cathedral"));
        }

        [Test]
        public void Ginmere_SitsAtTheTepuisFoot_OnCleanGround()
        {
            // The siting rationale, pinned: canon puts simas in tepui
            // country ("Sima Interior — inside sinkholes", Lore/History/
            // 00_Canon.md:63), so the mouth opens in Grovelands at the
            // foot, like Olderdeep's. And the cell must be genuinely
            // empty — no authored place, no road, no river.
            int gx = -1, gy = -1;
            foreach (var (name, x, y) in SinkholeSites.All)
                if (name == "Ginmere") { gx = x; gy = y; }
            Assert.GreaterOrEqual(gx, 0, "Ginmere is an authored mouth");

            Assert.AreEqual(BiomeType.Grovelands,
                WorldMapAuthoring.BiomeAt(gx, gy));
            Assert.IsNull(WorldMapAuthoring.PlaceAt(gx, gy),
                "the mouth does not sit on a settlement");
            Assert.IsFalse(WorldMapAuthoring.IsRoad(gx, gy),
                "a hole in a road would eat the road");
            Assert.IsFalse(WorldMapAuthoring.IsRiver(gx, gy),
                "a river cell is already spoken for");
        }

        [Test]
        public void TheSimaFloor_IsALightWell()
        {
            // Close-out hypothesis H12 — the drowned sima shipped at
            // catacomb darkness (0.22) with ZERO light sources: the one
            // floor with no glow of its own was also the one at the
            // bottom of an open hole. A real sima is a light well —
            // daylight down the shaft is why anything grows there.
            var zone = FloorOf("Ginmere");
            Assert.IsNotNull(zone);
            Assert.AreEqual(OverworldZoneManager.ShaftLightAmbient,
                zone.AmbientLevel, 0.001f,
                "open sky above; the floor reads brighter than sealed stone");
        }

        [Test]
        public void TheVillageFloor_KeepsItsAuthoredDark()
        {
            // Counter-check: the shaft light is the SIMA's. The village
            // floor's darkness is authored — canon's reveal order ("the
            // GLOW before the people") needs the dark around the patch.
            var zone = FloorOf("Lampwell");
            Assert.IsNotNull(zone);
            Assert.AreEqual(OverworldZoneManager.GetDepthAmbient(2),
                zone.AmbientLevel, 0.001f,
                "the glow is the town, and it needs the dark to be seen");
        }

    }
}
