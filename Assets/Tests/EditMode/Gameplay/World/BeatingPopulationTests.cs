using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W2.2 (Docs/FELLING-W1-W2-PLAN.md §7.6) — the Beating's bestiary.
    ///
    /// <para>The Beating borrowed DesertTier1/2/3 wholesale. Its own
    /// roster is the STATIC one only: the indicator species (Sari-Snake,
    /// Sky-Sari, Wardline) are deliberately absent — "only when Urqu is
    /// active" is their whole design (plan D3), and shipping them static
    /// would falsify it. These tests pin both halves: the roster that IS
    /// here, and the deliberate absence of the roster that is not.</para>
    /// </summary>
    public class BeatingPopulationTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadBlueprintsOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        private static int WeightOf(PopulationTable table, string blueprint)
        {
            int total = 0;
            foreach (var e in table.Entries)
                if (e.BlueprintName == blueprint) total += e.Weight;
            return total;
        }

        [Test]
        public void TheBeatingNoLongerBorrowsTheDesert()
        {
            Assert.AreEqual("BeatingTier1", PopulationTable.GetBiomeTable(BiomeType.Beating, 1).Name);
            Assert.AreEqual("BeatingTier2", PopulationTable.GetBiomeTable(BiomeType.Beating, 2).Name);
            Assert.AreEqual("BeatingTier3", PopulationTable.GetBiomeTable(BiomeType.Beating, 3).Name);
            Assert.AreEqual("BeatingLairGuards", PopulationTable.LairGuards(BiomeType.Beating).Name);
        }

        [Test]
        public void EveryBlueprintTheBeatingNamesActuallyExists()
        {
            // Fail-loud against real content — a table entry naming a
            // missing blueprint is a silently emptier zone. (Caught
            // "Waterskin" during authoring; the guard earns its keep.)
            for (int tier = 1; tier <= 3; tier++)
            {
                var table = PopulationTable.GetBiomeTable(BiomeType.Beating, tier);
                foreach (var entry in table.Entries)
                    Assert.IsTrue(_factory.Blueprints.ContainsKey(entry.BlueprintName),
                        $"BeatingTier{tier} names '{entry.BlueprintName}', which does not exist");
            }
            foreach (var entry in PopulationTable.LairGuards(BiomeType.Beating).Entries)
                Assert.IsTrue(_factory.Blueprints.ContainsKey(entry.BlueprintName),
                    $"BeatingLairGuards names '{entry.BlueprintName}', which does not exist");
        }

        [Test]
        public void TheDangerThickensWithTier()
        {
            // Same invariant the Spread earned: hostile weight strictly
            // increases as the pan deepens — the counter-check that stops
            // "reads as lived-in" being satisfied by an empty table.
            var hostiles = new[]
            {
                "Scorpion", "GlassScorpion", "BrittleHound", "DuneLurker",
                "SnapjawHunter", "SunStriker",
            };

            int[] weight = new int[4];
            for (int tier = 1; tier <= 3; tier++)
            {
                var table = PopulationTable.GetBiomeTable(BiomeType.Beating, tier);
                foreach (var h in hostiles) weight[tier] += WeightOf(table, h);
            }

            Assert.Greater(weight[2], weight[1], "tier 2 should bite harder than tier 1");
            Assert.Greater(weight[3], weight[2], "tier 3 should bite harder than tier 2");
        }

        [Test]
        public void TheWastelandStillFeedsYou()
        {
            // Saltbriar is the forage thread — present at every tier, so
            // survival is scarce but never impossible (canon: "water,
            // shade" are the culture's own lexicon; the land supports the
            // people who know it).
            for (int tier = 1; tier <= 3; tier++)
                Assert.Greater(WeightOf(
                    PopulationTable.GetBiomeTable(BiomeType.Beating, tier), "Saltbriar"), 0,
                    $"tier {tier} has no forage at all");
        }

        [Test]
        public void TheIndicatorSpeciesAreDeliberatelyAbsent()
        {
            // Plan D3: Sari-Snake / Sky-Sari / Wardline land with
            // state-reactive spawning, not as static spawns. If one of
            // these names appears in a static table, the indicator design
            // has been quietly falsified — this failure is a design
            // conversation, not a test fix.
            var gated = new[] { "SariSnake", "SkySari", "Wardline" };
            for (int tier = 1; tier <= 3; tier++)
            {
                var table = PopulationTable.GetBiomeTable(BiomeType.Beating, tier);
                foreach (var entry in table.Entries)
                    CollectionAssert.DoesNotContain(gated, entry.BlueprintName,
                        $"BeatingTier{tier} statically spawns an Urqu indicator species");
            }
        }

        [Test]
        public void SunStrikerAndSaltbriar_AreRealSpawnableContent()
        {
            var lizard = _factory.CreateEntity("SunStriker");
            Assert.IsNotNull(lizard);
            Assert.IsTrue(lizard.HasTag("Creature"), "SunStriker should be a creature");

            var briar = _factory.CreateEntity("Saltbriar");
            Assert.IsNotNull(briar);
            Assert.IsNotNull(briar.GetPart<HarvestablePart>(), "Saltbriar should be forageable");
            Assert.IsTrue(_factory.Blueprints.ContainsKey("SaltbriarSprig"),
                "the harvest yield must exist too");
        }
    }
}
