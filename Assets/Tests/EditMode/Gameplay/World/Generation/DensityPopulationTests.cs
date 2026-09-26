using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class DensityPopulationTests
    {
        [SetUp] public void SetUp() { NarrativeStatePart.Current = null; Diag.ResetAll(); }
        [TearDown] public void TearDown() { NarrativeStatePart.Current = null; Diag.ResetAll(); }

        [Test]
        public void SpreadTierOneAlwaysHasOneSmallEncounterAndKeepsOrdinaryFauna()
        {
            var seen = new HashSet<string>();
            for (int seed = 0; seed < 200; seed++)
            {
                var roll = PopulationTable.SpreadTier1().Roll(new Random(seed));
                var enemies = roll.Where(n => n == "MarlbackScrabbler" || n == "Viper").ToList();
                Assert.That(enemies.Count, Is.InRange(1, 2), "seed " + seed);
                Assert.AreEqual(1, enemies.Distinct().Count(), "one group, not both kinds");
                CollectionAssert.Contains(roll, "Magpie");
                CollectionAssert.DoesNotContain(roll, "GiantSpider");
                seen.UnionWith(enemies);
            }
            CollectionAssert.AreEquivalent(new[] { "Viper", "MarlbackScrabbler" }, seen);
        }

        [TestCase(BiomeType.Sodden, 2)]
        [TestCase(BiomeType.Sodden, 3)]
        [TestCase(BiomeType.Grovelands, 2)]
        [TestCase(BiomeType.Grovelands, 3)]
        public void LocalEncounterVariesWithoutCompulsoryMonoculture(BiomeType biome, int tier)
        {
            string[] pool = biome == BiomeType.Sodden
                ? new[] { "Bandfrog", "MawToad", "Viper" }
                : new[] { "Shambler", "Rotling", "Mosshulk" };
            var seen = new HashSet<string>();
            for (int seed = 0; seed < 240; seed++)
            {
                var chosen = PopulationTable.GetBiomeTable(biome, tier).Roll(new Random(seed))
                    .Where(pool.Contains).ToList();
                Assert.That(chosen.Count, Is.InRange(1, 3), "seed " + seed);
                Assert.AreEqual(1, chosen.Distinct().Count(), "pick one local group");
                seen.UnionWith(chosen);
            }
            CollectionAssert.AreEquivalent(pool, seen);
        }

        [TestCase(3, "CaveBear")]
        [TestCase(3, "Rotling")]
        [TestCase(6, "SkeletalSentry")]
        [TestCase(6, "CharredHusk")]
        [TestCase(6, "PaleStalker")]
        [TestCase(9, "StoneGolem")]
        [TestCase(9, "ObsidianBrute")]
        public void DepthCreatureIsARealEncounterChoice(int depth, string blueprint)
        {
            int appearances = 0;
            for (int seed = 0; seed < 300; seed++)
            {
                var roll = PopulationTable.UndergroundTier(depth).Roll(new Random(seed));
                if (roll.Contains(blueprint)) appearances++;
                // Counter-check: a special encounter isn't three compulsory marlback packs plus one rare extra.
                if (roll.Contains(blueprint)) Assert.IsFalse(roll.Any(n => n.StartsWith("Marlback")));
            }
            Assert.Greater(appearances, 15, "new depth species should appear in >5% of zones");
            Assert.Less(appearances, 180, "variety, not a new monoculture");
        }

        [Test]
        public void ShallowCavesExcludeDeepCreatures()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                var roll = PopulationTable.UndergroundTier(1).Roll(new Random(seed));
                Assert.IsFalse(roll.Any(n => new[] { "StoneGolem", "ObsidianBrute", "PaleStalker", "CaveBear" }.Contains(n)));
                Assert.That(roll.Count(n => n.StartsWith("Marlback")), Is.InRange(1, 3));
            }
        }

        private static PopulationEntry Grouped(string name, int weight = 1, string requires = null)
        {
            var e = new PopulationEntry { BlueprintName = name, Weight = weight, MinCount = 1, MaxCount = 1, RequiresWorldFlag = requires };
            var field = typeof(PopulationEntry).GetField("EncounterGroup");
            Assert.IsNotNull(field, "encounter grouping is an explicit opt-in; ordinary rows keep their semantics");
            field.SetValue(e, "test");
            return e;
        }

        [Test]
        public void PickOneRenormalizesAfterWorldFlagGate()
        {
            var t = new PopulationTable { Name = "GatedGroup", Entries = new List<PopulationEntry> { Grouped("ordinary"), Grouped("rare", 9, "active") } };
            for (int seed = 0; seed < 20; seed++) CollectionAssert.AreEqual(new[] { "ordinary" }, t.Roll(new Random(seed)));
            NarrativeStatePart.Current = new NarrativeStatePart(); NarrativeStatePart.Current.SetFact("active", 1);
            Assert.IsTrue(Enumerable.Range(0, 20).Any(seed => t.Roll(new Random(seed)).Contains("rare")));
        }

        [Test]
        public void AllGatedGroupIsEmptyButDoesNotSuppressAmbientMinima()
        {
            var t = new PopulationTable { Entries = new List<PopulationEntry> { Grouped("rare", 1, "active"), new PopulationEntry { BlueprintName = "ambient", MinCount = 1, MaxCount = 1 } } };
            CollectionAssert.AreEqual(new[] { "ambient" }, t.Roll(new Random(1)));
        }

        [Test]
        public void UngroupedGuaranteedRowsStillBothSpawn()
        {
            var t = new PopulationTable { Entries = new List<PopulationEntry> {
                new PopulationEntry { BlueprintName = "a", MinCount = 1, MaxCount = 1 },
                new PopulationEntry { BlueprintName = "b", MinCount = 1, MaxCount = 1 } } };
            CollectionAssert.AreEqual(new[] { "a", "b" }, t.Roll(new Random(3)));
        }

        [Test]
        public void PopulationRollDiagnosesChosenSkippedAndWorldGatedRows()
        {
            var t = new PopulationTable { Name = "diagnostic", Entries = new List<PopulationEntry> {
                new PopulationEntry { BlueprintName = "yes", MinCount = 1, MaxCount = 1 },
                new PopulationEntry { BlueprintName = "no", MinCount = 0, MaxCount = 0 },
                new PopulationEntry { BlueprintName = "gated", MinCount = 1, MaxCount = 1, RequiresWorldFlag = "unset" } } };
            t.Roll(new Random(1));
            var records = DiagQuery.Apply(new DiagQuery.Filter { Category = "worldgen", Kind = "PopulationRolled" }).Records;
            Assert.AreEqual(3, records.Count);
            Assert.IsTrue(records.Any(r => r.PayloadJson.Contains("world_flag")));
            Assert.IsTrue(records.Any(r => r.PayloadJson.Contains("\"count\":1")));
            Assert.IsTrue(records.Any(r => r.PayloadJson.Contains("\"count\":0")));
        }

        [Test]
        public void LairRollDiagnosesBothSuccessfulAndMissedAmbushers()
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            bool hit = false, miss = false;
            for (int seed = 0; seed < 40 && !(hit && miss); seed++)
            {
                Diag.ResetAll();
                new LairPopulationBuilder(BiomeType.Beating, new PointOfInterest(POIType.Lair, "test", "OutlandRaiders", 2, null))
                    .BuildZone(new Zone("density-lair"), factory, new Random(seed));
                var records = DiagQuery.Apply(new DiagQuery.Filter { Category = "worldgen", Kind = "AmbusherRolled" }).Records;
                var record = records.FirstOrDefault(r => r.PayloadJson.Contains("AmbushBandit"));
                Assert.IsNotNull(record);
                StringAssert.Contains("\"threshold\":30", record.PayloadJson);
                hit |= record.PayloadJson.Contains("\"placed\":1");
                miss |= record.PayloadJson.Contains("\"placed\":0");
            }
            Assert.IsTrue(hit && miss);
        }
    }
}
