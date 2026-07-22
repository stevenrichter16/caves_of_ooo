using System;
using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    public class LootTableRegistryTests
    {
        private const string TestJson = @"{
  ""Tables"": [
    {
      ""ID"": ""table_always_one_rock"",
      ""Entries"": [
        { ""BlueprintName"": ""Rock"", ""Weight"": 100, ""MinCount"": 1, ""MaxCount"": 1 }
      ]
    },
    {
      ""ID"": ""table_never_drops"",
      ""Entries"": [
        { ""BlueprintName"": ""Rock"", ""Weight"": 0, ""MinCount"": 1, ""MaxCount"": 1 }
      ]
    },
    {
      ""ID"": ""table_range_two_to_four"",
      ""Entries"": [
        { ""BlueprintName"": ""FireMoss"", ""Weight"": 100, ""MinCount"": 2, ""MaxCount"": 4 }
      ]
    },
    {
      ""ID"": ""table_multi_entry"",
      ""Entries"": [
        { ""BlueprintName"": ""Rock"", ""Weight"": 100, ""MinCount"": 1, ""MaxCount"": 1 },
        { ""BlueprintName"": ""FireMoss"", ""Weight"": 100, ""MinCount"": 1, ""MaxCount"": 1 }
      ]
    },
    {
      ""ID"": ""table_malformed_entries"",
      ""Entries"": [
        { ""BlueprintName"": """", ""Weight"": 100, ""MinCount"": 1, ""MaxCount"": 1 },
        { ""BlueprintName"": ""Rock"", ""Weight"": 100, ""MinCount"": 1, ""MaxCount"": 1 }
      ]
    }
  ]
}";

        [SetUp]
        public void Setup()
        {
            LootTableRegistry.ResetForTests();
            LootTableRegistry.InitializeFromJson(TestJson);
        }

        [TearDown]
        public void TearDown()
        {
            LootTableRegistry.ResetForTests();
        }

        [Test]
        public void TryGetTable_KnownId_Succeeds()
        {
            bool found = LootTableRegistry.TryGetTable("table_always_one_rock", out LootTable table);
            Assert.IsTrue(found);
            Assert.AreEqual("table_always_one_rock", table.ID);
        }

        [Test]
        public void TryGetTable_UnknownId_Fails()
        {
            bool found = LootTableRegistry.TryGetTable("no_such_table", out LootTable table);
            Assert.IsFalse(found);
            Assert.IsNull(table);
        }

        [Test]
        public void TryGetTable_IsCaseInsensitive()
        {
            bool found = LootTableRegistry.TryGetTable("TABLE_ALWAYS_ONE_ROCK", out LootTable table);
            Assert.IsTrue(found);
        }

        [Test]
        public void Roll_Weight100_AlwaysDrops()
        {
            LootTableRegistry.TryGetTable("table_always_one_rock", out LootTable table);
            var rng = new Random(1);

            for (int i = 0; i < 20; i++)
            {
                List<string> result = table.Roll(rng);
                Assert.AreEqual(1, result.Count, "Weight=100 entry must drop on every roll.");
                Assert.AreEqual("Rock", result[0]);
            }
        }

        [Test]
        public void Roll_Weight0_NeverDrops()
        {
            // Counter-check for Roll_Weight100_AlwaysDrops: a zero-weight
            // entry must never appear, proving Weight actually gates the
            // roll rather than the loop being vacuously satisfied.
            LootTableRegistry.TryGetTable("table_never_drops", out LootTable table);
            var rng = new Random(1);

            for (int i = 0; i < 20; i++)
            {
                List<string> result = table.Roll(rng);
                Assert.AreEqual(0, result.Count, "Weight=0 entry must never drop.");
            }
        }

        [Test]
        public void Roll_CountRange_StaysWithinMinMax()
        {
            LootTableRegistry.TryGetTable("table_range_two_to_four", out LootTable table);
            var rng = new Random(2);

            for (int i = 0; i < 30; i++)
            {
                List<string> result = table.Roll(rng);
                Assert.GreaterOrEqual(result.Count, 2);
                Assert.LessOrEqual(result.Count, 4);
                foreach (string bp in result)
                    Assert.AreEqual("FireMoss", bp);
            }
        }

        [Test]
        public void Roll_MultipleEntries_CanYieldBothIndependently()
        {
            LootTableRegistry.TryGetTable("table_multi_entry", out LootTable table);
            var rng = new Random(3);

            List<string> result = table.Roll(rng);

            Assert.AreEqual(2, result.Count, "Both weight=100 entries should drop independently in one roll.");
            CollectionAssert.Contains(result, "Rock");
            CollectionAssert.Contains(result, "FireMoss");
        }

        [Test]
        public void Roll_MalformedEntry_IsSkipped_NotThrown()
        {
            LootTableRegistry.TryGetTable("table_malformed_entries", out LootTable table);
            var rng = new Random(4);

            List<string> result = null;
            Assert.DoesNotThrow(() => result = table.Roll(rng));
            Assert.AreEqual(1, result.Count, "The blank-blueprint entry must be skipped; the valid entry still rolls.");
            Assert.AreEqual("Rock", result[0]);
        }

        [Test]
        public void Roll_NullRng_ReturnsEmptyList_NotThrown()
        {
            LootTableRegistry.TryGetTable("table_always_one_rock", out LootTable table);

            List<string> result = null;
            Assert.DoesNotThrow(() => result = table.Roll(null));
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void ResetForTests_ClearsLoadedTables()
        {
            LootTableRegistry.ResetForTests();

            // No JSON re-initialized after reset, and EnsureInitialized will
            // try (and fail gracefully) to load the real Resources asset —
            // the test-only table must not survive the reset.
            bool found = LootTableRegistry.TryGetTable("table_always_one_rock", out _);
            Assert.IsFalse(found, "ResetForTests must clear previously-loaded test tables.");
        }
    }
}
