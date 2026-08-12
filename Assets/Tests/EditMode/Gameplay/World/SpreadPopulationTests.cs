using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The Spread's own bestiary (W1).
    ///
    /// <para>The Spread used to borrow <c>JungleTier1/2/3</c>, which is the
    /// retrofit the world overhaul exists to undo: settled river country was
    /// spawning giant spiders in somebody's barley. What makes the Spread
    /// itself is that the danger is not the wilderness — it is the road and
    /// the hedge — so the table leans on ordinary fauna and forage, with a
    /// thin thread of human trouble that thickens with tier.</para>
    /// </summary>
    public class SpreadPopulationTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadBlueprintsOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        private static HashSet<string> NamesIn(PopulationTable table)
        {
            var names = new HashSet<string>();
            foreach (var e in table.Entries) names.Add(e.BlueprintName);
            return names;
        }

        private static int WeightOf(PopulationTable table, string blueprint)
        {
            int total = 0;
            foreach (var e in table.Entries)
                if (e.BlueprintName == blueprint) total += e.Weight;
            return total;
        }

        [Test]
        public void TheSpreadNoLongerBorrowsTheJungle()
        {
            // The whole point. If these ever come back equal, the retrofit
            // has been reinstated.
            Assert.AreEqual("SpreadTier1", PopulationTable.GetBiomeTable(BiomeType.Spread, 1).Name);
            Assert.AreEqual("SpreadTier2", PopulationTable.GetBiomeTable(BiomeType.Spread, 2).Name);
            Assert.AreEqual("SpreadTier3", PopulationTable.GetBiomeTable(BiomeType.Spread, 3).Name);
        }

        [Test]
        public void EveryBlueprintTheSpreadNamesActuallyExists()
        {
            // A table entry naming a blueprint that does not exist is a
            // silent spawn failure — the zone just comes out emptier than
            // intended and nothing says so.
            for (int tier = 1; tier <= 3; tier++)
            {
                var table = PopulationTable.GetBiomeTable(BiomeType.Spread, tier);
                foreach (var entry in table.Entries)
                    Assert.IsTrue(_factory.Blueprints.ContainsKey(entry.BlueprintName),
                        $"SpreadTier{tier} names '{entry.BlueprintName}', which does not exist");
            }
        }

        [Test]
        public void TheSpreadIsSettledCountry_NotWilderness()
        {
            // Tier 1 should read as inhabited: more weight on fauna and
            // forage than on things that want to kill you.
            var t1 = PopulationTable.GetBiomeTable(BiomeType.Spread, 1);

            int settled = WeightOf(t1, "Magpie") + WeightOf(t1, "PetDog")
                        + WeightOf(t1, "BerryBush") + WeightOf(t1, "Beehive")
                        + WeightOf(t1, "HollowStump") + WeightOf(t1, "Signpost");
            int hostile = WeightOf(t1, "Snapjaw") + WeightOf(t1, "SnapjawScavenger")
                        + WeightOf(t1, "SnapjawHunter") + WeightOf(t1, "Viper")
                        + WeightOf(t1, "GiantSpider");

            Assert.Greater(settled, hostile,
                "near Spread should feel lived-in, not hunted");
        }

        [Test]
        public void TheRoadGetsWorseTheFurtherOutYouGo()
        {
            // Counter-check to the above, and the actual progression: the
            // human trouble thickens with tier while the fauna thins.
            int Hostiles(int tier)
            {
                var t = PopulationTable.GetBiomeTable(BiomeType.Spread, tier);
                return WeightOf(t, "Snapjaw") + WeightOf(t, "SnapjawScavenger")
                     + WeightOf(t, "SnapjawHunter") + WeightOf(t, "GiantSpider");
            }

            Assert.Less(Hostiles(1), Hostiles(2), "tier 2 must be rougher than tier 1");
            Assert.Less(Hostiles(2), Hostiles(3), "tier 3 must be rougher than tier 2");
        }

        [Test]
        public void TheJungleKeepsItsOwnCreatures()
        {
            // Counter-check that the edit did not simply rename the jungle
            // table: the jungle must still be the jungle.
            var jungle = PopulationTable.GetBiomeTable(BiomeType.Jungle, 1);
            Assert.AreEqual("JungleTier1", jungle.Name);
            CollectionAssert.Contains(NamesIn(jungle), "GiantSpider");
        }

        [Test]
        public void NoJungleOnlyCreatureWandersIntoTheNearSpread()
        {
            // The specific absurdity that motivated this: a giant spider in
            // the barley at tier 1.
            CollectionAssert.DoesNotContain(
                NamesIn(PopulationTable.GetBiomeTable(BiomeType.Spread, 1)), "GiantSpider");
        }

        [Test]
        public void TheOtherFellingBiomesStillFallBackCleanly()
        {
            // W2+ biomes have not been authored yet and must keep working
            // off their borrowed tables rather than crashing or coming back
            // empty.
            foreach (BiomeType biome in new[]
            {
                BiomeType.Sodden, BiomeType.Beating, BiomeType.Grovelands,
                BiomeType.Overwrit, BiomeType.Stump,
            })
                for (int tier = 1; tier <= 3; tier++)
                {
                    var table = PopulationTable.GetBiomeTable(biome, tier);
                    Assert.IsNotNull(table, $"{biome} tier {tier} has no table");
                    Assert.IsNotEmpty(table.Entries, $"{biome} tier {tier} is empty");
                }
        }
    }
}
