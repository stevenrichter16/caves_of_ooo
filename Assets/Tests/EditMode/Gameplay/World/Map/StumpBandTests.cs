using NUnit.Framework;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W6.1 (Docs/FELLING-W6-PLAN.md §3) — elevation bands as data.
    /// Canon: "Elevation is the content key" (FELLING-WORLD-DESIGN.md
    /// §3.6); the Sarisariñama survey maps to bands, and a third of the
    /// bestiary exists to indicate a band. The band map is AUTHORED
    /// rows over the T region, the house style of BiomeRows/TierRows.
    ///
    /// <para>R1 (the plan's risk register): drift between BiomeRows'
    /// T region and the band rows is a silent hole — a T cell with no
    /// band would fall back to whatever default the consumer picks,
    /// invisibly. The two total-coverage pins below are that risk's
    /// fence, in both directions.</para>
    /// </summary>
    public class StumpBandTests
    {
        [Test]
        public void EveryTepuiCellHasABand()
        {
            // Direction one: no T cell may be bandless.
            for (int x = 0; x < WorldMap.Width; x++)
                for (int y = 0; y < WorldMap.Height; y++)
                    if (WorldMapAuthoring.BiomeAt(x, y) == BiomeType.Stump)
                        Assert.AreNotEqual(StumpBand.None, StumpBands.BandAt(x, y),
                            $"({x},{y}) is tepui with no band — the content key has a hole");
        }

        [Test]
        public void BandsExistOnlyOnTheTepui()
        {
            // Direction two: a band off the mountain would key content
            // (populations, tint) onto ground that is not the tepui.
            for (int x = 0; x < WorldMap.Width; x++)
                for (int y = 0; y < WorldMap.Height; y++)
                    if (WorldMapAuthoring.BiomeAt(x, y) != BiomeType.Stump)
                        Assert.AreEqual(StumpBand.None, StumpBands.BandAt(x, y),
                            $"({x},{y}) is not tepui but has a band");
            // And out-of-bounds is None, not a crash.
            Assert.AreEqual(StumpBand.None, StumpBands.BandAt(-1, -1));
            Assert.AreEqual(StumpBand.None, StumpBands.BandAt(99, 99));
        }

        [Test]
        public void TheRootSleepsUnderTheSummit()
        {
            // Canon: the summit is the petrified canopy at the heart;
            // the Root (3,3) sleeps beneath it.
            Assert.AreEqual(StumpBand.Summit, StumpBands.BandAt(3, 3));
        }

        [Test]
        public void TheOuterChunksAreFoothills()
        {
            // Canon: "Foothills (outer chunks)" — the northern edge and
            // the eastern shoulder both open onto other biomes.
            Assert.AreEqual(StumpBand.Foothills, StumpBands.BandAt(2, 1));
            Assert.AreEqual(StumpBand.Foothills, StumpBands.BandAt(5, 2));
        }

        [Test]
        public void TheFellingSiteSitsOnTheSlopes()
        {
            // The circle is at the mountain's base-slope (3,5) — an
            // authored Tier-5 place, but band-wise it is slope ground.
            Assert.AreEqual(StumpBand.Slopes, StumpBands.BandAt(3, 5));
        }

        [Test]
        public void AllThreeBandsActuallyOccur()
        {
            // Counter-check against a degenerate authored map: a table
            // that mapped everything to one band would pass coverage.
            int f = 0, s = 0, u = 0;
            for (int x = 0; x < WorldMap.Width; x++)
                for (int y = 0; y < WorldMap.Height; y++)
                    switch (StumpBands.BandAt(x, y))
                    {
                        case StumpBand.Foothills: f++; break;
                        case StumpBand.Slopes: s++; break;
                        case StumpBand.Summit: u++; break;
                    }
            Assert.Greater(f, 0, "foothills exist");
            Assert.Greater(s, 0, "slopes exist");
            Assert.Greater(u, 0, "a summit exists");
            Assert.Greater(f, u, "the mountain is wider at the bottom than the top");
        }

        [Test]
        public void TheTintClimbsCoolerAndBrighter()
        {
            // Canon: "warm base → cool bright summit". Pure half: the
            // per-band tint. Blue rises with altitude; red falls.
            var baseTint = new Color(0.98f, 0.93f, 0.92f);
            var foot = StumpBands.TintFor(StumpBand.Foothills, baseTint);
            var slope = StumpBands.TintFor(StumpBand.Slopes, baseTint);
            var summit = StumpBands.TintFor(StumpBand.Summit, baseTint);

            Assert.AreEqual(baseTint, foot, "the base IS the biome tint — warm");
            Assert.Greater(slope.b, foot.b, "cooler going up");
            Assert.Greater(summit.b, slope.b, "coolest at the top");
            Assert.Less(summit.r, foot.r, "and the warmth stays below");
            // None passes through untouched (the consumer guard).
            Assert.AreEqual(baseTint, StumpBands.TintFor(StumpBand.None, baseTint));
        }

        [Test]
        public void AStumpZoneWearsItsBandTint()
        {
            // Integration: OnZoneGenerated applies the band tint to a
            // generated Stump surface zone. Summit cell (3,3) — the
            // zone's AmbientTint must be the summit shift, not the flat
            // biome tint.
            var factory = new CavesOfOoo.Data.EntityFactory();
            factory.LoadBlueprints(System.IO.File.ReadAllText(System.IO.Path.Combine(
                UnityEngine.Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            var mgr = new OverworldZoneManager(factory, worldSeed: 42);

            var summitZone = mgr.GetZone("Overworld.3.3.0");
            var expected = StumpBands.TintFor(StumpBand.Summit,
                new Color(0.98f, 0.93f, 0.92f));
            Assert.AreEqual(expected.b, summitZone.AmbientTint.b, 0.001f,
                "the summit chunk is cool and bright");

            // Counter: a foothill chunk keeps the warm base tint.
            var footZone = mgr.GetZone("Overworld.2.1.0");
            Assert.AreEqual(0.92f, footZone.AmbientTint.b, 0.001f,
                "the foothills keep the biome's warmth");
        }
    }
}
