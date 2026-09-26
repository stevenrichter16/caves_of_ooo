using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Density Phase 1 (Docs/DENSITY-PHASE-1.md §T1.2). Since the Felling
    /// world went authored (W0.6), every lair sits in a canon biome
    /// (Spread, Sodden, Beating, Grovelands; never the Overwrit or the
    /// Stump), but <c>WorldGenerator.GetBossForBiome</c> only had cases for
    /// the four retired biomes. Every lair in every world fell through to
    /// <c>default: "SnapjawChieftain"</c>, so the desert prowler and the
    /// jungle stalker, both finished bosses, could never be met.
    ///
    /// User-visible invariant: "a lair's boss belongs to the country it
    /// is dug into: a prowler dens in the Beating, a stalker in the
    /// Grovelands."
    /// </summary>
    public class LairBossBiomeTests
    {
        private const int Seeds = 200;

        private static readonly Dictionary<BiomeType, string> Expected = new Dictionary<BiomeType, string>
        {
            { BiomeType.Spread, "SnapjawChieftain" },
            { BiomeType.Sodden, "SnapjawChieftain" },
            { BiomeType.Beating, "DesertProwler" },
            { BiomeType.Grovelands, "JungleStalker" },
        };

        private static List<(BiomeType biome, PointOfInterest poi)> LairsAcrossSeeds()
        {
            var lairs = new List<(BiomeType, PointOfInterest)>();
            for (int seed = 0; seed < Seeds; seed++)
            {
                var map = WorldGenerator.Generate(seed);
                for (int x = 0; x < WorldMap.Width; x++)
                    for (int y = 0; y < WorldMap.Height; y++)
                    {
                        var poi = map.GetPOI(x, y);
                        if (poi != null && poi.Type == POIType.Lair)
                            lairs.Add((map.GetBiome(x, y), poi));
                    }
            }
            return lairs;
        }

        [Test]
        public void EveryLair_GetsTheBossOfItsOwnBiome()
        {
            var observed = new Dictionary<BiomeType, int>();
            foreach (var (biome, poi) in LairsAcrossSeeds())
            {
                Assert.IsTrue(Expected.ContainsKey(biome),
                    $"a lair was placed in {biome}; this test's table needs a row for it");
                Assert.AreEqual(Expected[biome], poi.BossBlueprint,
                    $"{poi.Name} in the {biome} has the wrong boss");
                observed.TryGetValue(biome, out int n);
                observed[biome] = n + 1;
            }

            // Non-vacuity: the two biomes whose bosses changed must actually
            // have been exercised, or the loop above proved nothing about them.
            Assert.Greater(observed.TryGetValue(BiomeType.Beating, out int beating) ? beating : 0, 0,
                "no Beating lair in 200 worlds; the Beating mapping went untested");
            Assert.Greater(observed.TryGetValue(BiomeType.Grovelands, out int groves) ? groves : 0, 0,
                "no Grovelands lair in 200 worlds; the Grovelands mapping went untested");
        }

        [Test]
        public void LairBosses_AreNoLongerOneCreatureEverywhere()
        {
            // Counter-check phrased as the player sees it: across many
            // worlds, more than one kind of boss is waiting in the lairs.
            var bosses = new HashSet<string>();
            foreach (var (_, poi) in LairsAcrossSeeds())
                bosses.Add(poi.BossBlueprint);
            Assert.GreaterOrEqual(bosses.Count, 3,
                "lairs still seat the same boss everywhere: " + string.Join(", ", bosses));
        }

        [Test]
        public void EveryMappedBoss_IsARealBossBlueprint()
        {
            // Guards the mapping against a typo that would make
            // LairBuilder's CreateEntity return null and leave an empty
            // boss chamber (it fails silently at LairBuilder.cs:102-107).
            FactionManager.Initialize();
            try
            {
                var factory = new EntityFactory();
                factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                    Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
                foreach (var boss in new HashSet<string>(Expected.Values))
                {
                    var e = factory.CreateEntity(boss);
                    Assert.IsNotNull(e, boss + " must resolve");
                    Assert.IsTrue(e.HasTag("Boss"), boss + " must carry the Boss tag");
                }
            }
            finally
            {
                FactionManager.Reset();
            }
        }
    }
}
