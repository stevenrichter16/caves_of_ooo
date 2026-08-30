using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W6.2a (Docs/FELLING-W6-PLAN.md §3) — the mountain reads as one
    /// object. The Grainfield: parallel stone ridges that are the wood
    /// grain of a bole a mile wide, "running the same compass direction
    /// across every slope chunk". The Cascade gorge: terrace shelves
    /// and spray pools in the foothills — the biodiversity hotspot.
    ///
    /// <para>Formation CHOICE is a pure function of the zone id
    /// (FormationSelector's contract — builder rng would re-roll the
    /// world on any pipeline change); INTRA-chunk layout may use the
    /// builder rng like every other formation.</para>
    /// </summary>
    public class StumpFormationTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        private static Zone StumpZone(int wx, int wy, int seed = 42)
        {
            var mgr = new OverworldZoneManager(_factory, worldSeed: seed);
            return mgr.GetZone($"Overworld.{wx}.{wy}.0");
        }

        private static int CountOf(Zone zone, string bp)
        {
            int n = 0;
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == bp) n++;
            return n;
        }

        // ════════════════════════════════════════════════════════════
        //   The choice is stable and band-keyed
        // ════════════════════════════════════════════════════════════

        [Test]
        public void TheChoiceIsAPureFunctionOfTheAddress()
        {
            Assert.AreEqual(
                FormationSelector.ForStump(StumpBand.Slopes, "Overworld.2.2.0"),
                FormationSelector.ForStump(StumpBand.Slopes, "Overworld.2.2.0"),
                "same chunk, same place, forever");
        }

        [Test]
        public void OffTheMountainThereIsNoFormation()
        {
            // Counter-check: the None band must not roll ANY formation.
            Assert.AreEqual(Formation.None,
                FormationSelector.ForStump(StumpBand.None, "Overworld.10.10.0"));
        }

        [Test]
        public void TheSlopesRollOnlySlopeFormations()
        {
            // W6.2b widened the pool: a slope is Grainfield or a
            // ButtressRidge — never a gorge, never summit scrub.
            foreach (var id in new[] { "Overworld.2.2.0", "Overworld.4.2.0",
                                       "Overworld.2.4.0", "Overworld.3.5.0" })
            {
                var f = FormationSelector.ForStump(StumpBand.Slopes, id);
                Assert.IsTrue(f == Formation.Grainfield || f == Formation.ButtressRidge,
                    id + " rolled " + f);
            }
        }

        // ════════════════════════════════════════════════════════════
        //   The Grainfield
        // ════════════════════════════════════════════════════════════

        /// <summary>The slope addresses whose formation is the
        /// Grainfield, found by asking the selector — the pool is
        /// weighted data, and hard-coding addresses breaks every time
        /// it widens.</summary>
        private static System.Collections.Generic.List<(int x, int y)> GrainfieldChunks()
        {
            var outp = new System.Collections.Generic.List<(int x, int y)>();
            foreach (var (x, y) in new[] { (2, 2), (4, 2), (2, 4), (3, 5), (4, 5) })
                if (StumpBands.BandAt(x, y) == StumpBand.Slopes
                    && FormationSelector.ForStump(StumpBand.Slopes, $"Overworld.{x}.{y}.0")
                        == Formation.Grainfield)
                    outp.Add((x, y));
            return outp;
        }

        [Test]
        public void ASlopeChunk_CarriesTheGrain()
        {
            var grain = GrainfieldChunks();
            Assert.Greater(grain.Count, 0, "some slope carries the grain");
            var (gx, gy) = grain[0];
            var zone = StumpZone(gx, gy);
            Assert.Greater(CountOf(zone, "GrainRidge"), 30,
                "the wood grain of a bole a mile wide is not subtle");
        }

        [Test]
        public void TheGrainRunsTheSameWay_InEverySlopeChunk()
        {
            // The signature: one world-constant compass direction. In
            // grid terms — ridges dominate ROWS, never columns, and do
            // so in every slope chunk, so the mountain reads as one
            // object rather than a tiling of unrelated chunks.
            var grain = GrainfieldChunks();
            Assert.Greater(grain.Count, 0, "some slope carries the grain");
            foreach (var (wx, wy) in grain)
            {
                var zone = StumpZone(wx, wy);
                var perRow = new System.Collections.Generic.Dictionary<int, int>();
                var perCol = new System.Collections.Generic.Dictionary<int, int>();
                foreach (var e in zone.GetAllEntities())
                {
                    if (e.BlueprintName != "GrainRidge") continue;
                    var p = zone.GetEntityPosition(e);
                    perRow[p.y] = perRow.TryGetValue(p.y, out int r) ? r + 1 : 1;
                    perCol[p.x] = perCol.TryGetValue(p.x, out int c) ? c + 1 : 1;
                }
                int maxRow = 0, maxCol = 0;
                foreach (var v in perRow.Values) if (v > maxRow) maxRow = v;
                foreach (var v in perCol.Values) if (v > maxCol) maxCol = v;
                Assert.Greater(maxRow, 12, $"({wx},{wy}): long ridges run with the grain");
                Assert.Less(maxCol, 8, $"({wx},{wy}): and never across it");
            }
        }

        [Test]
        public void TheGrainfieldDoesNotSealTheChunk()
        {
            // Ridges are walls; the field must still be crossable and
            // pocket-free. Multi-seed, because the gaps are rng-placed.
            var grain = GrainfieldChunks();
            Assert.Greater(grain.Count, 0, "some slope carries the grain");
            var (gx, gy) = grain[0];
            for (int seed = 1; seed <= 4; seed++)
            {
                var zone = StumpZone(gx, gy, seed);
                FormationReachability.FloodFromWest(zone, out bool crossed);
                Assert.IsTrue(crossed, $"seed {seed}: the slope can be walked");
            }
        }

        // ════════════════════════════════════════════════════════════
        //   The Cascade gorge
        // ════════════════════════════════════════════════════════════

        [Test]
        public void AFoothillChunk_HasWaterAndShelves()
        {
            // (2,1) is Foothills. Canon: 20m waterfalls, spray-zone
            // pools — the biodiversity hotspots.
            var zone = StumpZone(2, 1);
            Assert.Greater(CountOf(zone, "SprayPool"), 6,
                "the spray zone is wet");
            Assert.Greater(CountOf(zone, "DescentLedge"), 8,
                "and terraced — water finds the shelves and goes on down");
        }

        // ════════════════════════════════════════════════════════════
        //   W6.2b — the summit family
        // ════════════════════════════════════════════════════════════

        [Test]
        public void TheSummitRollsOnlySummitFormations()
        {
            foreach (var (x, y) in new[] { (3, 2), (2, 3), (3, 3), (4, 3), (3, 4) })
            {
                var f = FormationSelector.ForStump(StumpBand.Summit, $"Overworld.{x}.{y}.0");
                Assert.IsTrue(f == Formation.SummitScrub || f == Formation.RimForest,
                    $"({x},{y}) rolled {f}");
            }
        }

        [Test]
        public void ASummitChunk_HasDomesAndTanks()
        {
            // Find a SummitScrub summit cell by asking the selector.
            foreach (var (x, y) in new[] { (3, 2), (2, 3), (3, 3), (4, 3), (3, 4) })
            {
                if (FormationSelector.ForStump(StumpBand.Summit, $"Overworld.{x}.{y}.0")
                    != Formation.SummitScrub) continue;
                var zone = StumpZone(x, y);
                Assert.Greater(CountOf(zone, "StoneDome"), 12,
                    "the petrified canopy is domed stone");
                Assert.Greater(CountOf(zone, "TankBrocchinia"), 3,
                    "and the tanks hold drinkable rain");
                return;
            }
            Assert.Fail("no summit cell rolled SummitScrub — the pool weighting is off");
        }

        [Test]
        public void ARimChunk_IsAGreenCrack()
        {
            foreach (var (x, y) in new[] { (3, 2), (2, 3), (3, 3), (4, 3), (3, 4) })
            {
                if (FormationSelector.ForStump(StumpBand.Summit, $"Overworld.{x}.{y}.0")
                    != Formation.RimForest) continue;
                var zone = StumpZone(x, y);
                int green = CountOf(zone, "Tree") + CountOf(zone, "Bush");
                Assert.Greater(green, 20,
                    "humid dwarf-forest winds through the stone");
                return;
            }
            Assert.Fail("no summit cell rolled RimForest — the pool weighting is off");
        }

        [Test]
        public void TheSummitFormationsDoNotSealTheChunk()
        {
            foreach (var (x, y) in new[] { (3, 2), (3, 3) })
                for (int seed = 1; seed <= 3; seed++)
                {
                    var zone = StumpZone(x, y, seed);
                    FormationReachability.FloodFromWest(zone, out bool crossed);
                    Assert.IsTrue(crossed, $"({x},{y}) seed {seed}: the canopy can be walked");
                }
        }

        [Test]
        public void TheBandsDoNotSwapContent()
        {
            // Counter-checks across the widened pools: domes never on
            // the slopes, grain never at the canopy, the gorge only in
            // the foothills.
            var grain = GrainfieldChunks();
            if (grain.Count > 0)
            {
                var slope = StumpZone(grain[0].x, grain[0].y);
                Assert.AreEqual(0, CountOf(slope, "StoneDome"), "no domes on the slopes");
            }
            var summit = StumpZone(3, 3);
            Assert.AreEqual(0, CountOf(summit, "GrainRidge"), "no grain at the canopy");
            Assert.AreEqual(0, CountOf(summit, "SprayPool"), "no gorge at the canopy");
        }

        [Test]
        public void TheDomesHaveArt_WithMoreThanOneFace()
        {
            bool dome = false, tank = false;
            foreach (var (bp, _) in CavesOfOoo.Rendering.EnvironmentSpriteRenderer.FixtureSprites)
            {
                if (bp == "StoneDome") dome = true;
                if (bp == "TankBrocchinia") tank = true;
            }
            Assert.IsTrue(dome, "StoneDome is mapped to art");
            Assert.IsTrue(tank, "TankBrocchinia is mapped to art");
            Assert.IsTrue(
                CavesOfOoo.Rendering.EnvironmentSpriteRenderer.FixtureVariantCounts
                    .TryGetValue("StoneDome", out int n) && n >= 3,
                "a field of identical domes is wallpaper");
        }

        // ════════════════════════════════════════════════════════════
        //   Render contracts
        // ════════════════════════════════════════════════════════════

        [Test]
        public void SprayWater_IsWater()
        {
            // The spray pool rides the water ground tileset + the
            // existing water animation (glyph family '~').
            Assert.AreEqual(CavesOfOoo.Rendering.EnvironmentSpriteRenderer.GroundMaterial.Water,
                CavesOfOoo.Rendering.EnvironmentSpriteRenderer.ResolveGroundMaterial("SprayPool"));
        }

        [Test]
        public void TheGrainHasArt_AndMoreThanOneFace()
        {
            // Standing rule + the W5 wallpaper lesson: ridges are
            // stamped in long rows, so the fixture ships with variants.
            bool mapped = false;
            foreach (var (bp, _) in CavesOfOoo.Rendering.EnvironmentSpriteRenderer.FixtureSprites)
                if (bp == "GrainRidge") mapped = true;
            Assert.IsTrue(mapped, "GrainRidge is mapped to art");
            Assert.IsTrue(
                CavesOfOoo.Rendering.EnvironmentSpriteRenderer.FixtureVariantCounts
                    .TryGetValue("GrainRidge", out int n) && n >= 3,
                "a mile of the same tile is wallpaper");
        }

        // ════════════════════════════════════════════════════════════
        //   The mountain is pink stone, not a desert (cold-eye 🔴)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void TheTepuiIsNotACactusDesert()
        {
            // The 🔴: W6.2a swapped the base terrain to DesertBuilder
            // and inherited its stock content — a HARD-CODED 3% cactus
            // scatter (~48 per chunk, summit included) on the petrified
            // god-tree. Canon's flora is sundews, spear-leaf, bromeliad
            // scrub and dwarf forest; no cactus anywhere on the tepui.
            foreach (var (x, y) in new[] { (2, 1), (2, 2), (3, 3) })
                Assert.AreEqual(0, CountOf(StumpZone(x, y), "Cactus"),
                    $"({x},{y}) is the god-tree's stump, not the Beating");
        }

        [Test]
        public void TheDesertStillHasItsCacti()
        {
            // Counter-check: the cactus scatter became a FIELD, not a
            // deletion. A desert with no cacti would be the same bug
            // wearing the other shoe.
            var zone = new Zone("CactusControl");
            new DesertBuilder().BuildZone(zone, _factory, new System.Random(3));
            Assert.Greater(CountOf(zone, "Cactus"), 5,
                "the wasteland keeps what it always had");
        }

        [Test]
        public void TheGroundIsPinkStone_NotSand()
        {
            // Canon: "Pink-grey sandstone (the real tepui is pink
            // sandstone)". The ground tier paints macro tiles at
            // AUTHORED art colour — it is not multiplied by the
            // blueprint's glyph colour the way the fixture tier is —
            // so pink-grey needs its own material, not a recoloured
            // Sand blueprint.
            var zone = StumpZone(2, 2);
            Assert.Greater(CountOf(zone, "TepuiStone"), 200,
                "the mountain is floored in its own stone");
            Assert.AreEqual(0, CountOf(zone, "Sand"),
                "and not in the wasteland's sand");
            Assert.AreEqual(
                CavesOfOoo.Rendering.EnvironmentSpriteRenderer.GroundMaterial.Tepui,
                CavesOfOoo.Rendering.EnvironmentSpriteRenderer
                    .ResolveGroundMaterial("TepuiStone"));
        }

        [Test]
        public void TheWallsAreTepuiStoneToo()
        {
            var zone = StumpZone(2, 2);
            Assert.Greater(CountOf(zone, "TepuiWall"), 5,
                "the outcrops are the mountain's own stone");
            Assert.AreEqual(0, CountOf(zone, "SandstoneWall"),
                "not the desert's");
        }

        [Test]
        public void ThePinkStoneTilesetLoads()
        {
            // A declared ground material with no art silently falls
            // through to the ASCII glyph — the exact failure mode the
            // terrain-coverage guard exists for.
            for (int i = 0; i < 16; i++)
                Assert.IsNotNull(
                    UnityEngine.Resources.Load<UnityEngine.Sprite>(
                        $"Sprites/Environment/tepui_m{i:00}"),
                    $"tepui_m{i:00} must load");
            for (int v = 0; v < 4; v++)
                Assert.IsNotNull(
                    UnityEngine.Resources.Load<UnityEngine.Sprite>(
                        $"Sprites/Environment/tepui_wall_v{v}"),
                    $"tepui_wall_v{v} must load");
        }

        [Test]
        public void TheBeatingIsStillSand()
        {
            // Counter-check: the new material must not have been wired
            // by hijacking the desert's. Sand is still Sand.
            Assert.AreEqual(
                CavesOfOoo.Rendering.EnvironmentSpriteRenderer.GroundMaterial.Sand,
                CavesOfOoo.Rendering.EnvironmentSpriteRenderer
                    .ResolveGroundMaterial("Sand"));
        }

    }
}
