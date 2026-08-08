using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using Application = UnityEngine.Application;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// BIOME-OVERHAUL Phase A1 — the loot-table system
    /// (Docs/BIOME-OVERHAUL.md §2 A1, log in Docs/BIOME-OVERHAUL-LOG.md).
    /// Before this system every item placement in the game was a
    /// hardcoded array (lair pool: 5 items; village chest: 3 inline
    /// grimoire adds; zero containers anywhere else). LootTableRegistry
    /// is the JSON-driven replacement: named tables, independent-roll and
    /// weighted-pick modes, one-level+ acyclic TableRef nesting, and
    /// LOUD load-time validation (mirroring StoryletRegistry's posture,
    /// NOT the conversation system's silent fail-open).
    /// </summary>
    [TestFixture]
    public class BiomeLootTableTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadBlueprintsOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        [SetUp]
        public void Setup() => LootTableRegistry.ResetForTests();

        [TearDown]
        public void TearDown() => LootTableRegistry.ResetForTests();

        private static void LoadRealTables()
        {
            LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Loot/LootTables.json")));
        }

        // ── 1. Parse + roll semantics ────────────────────────────

        private const string TestJson = @"{
          ""Tables"": [
            {
              ""Name"": ""AlwaysBoth"",
              ""Entries"": [
                { ""Blueprint"": ""Dagger"" },
                { ""Blueprint"": ""Torch"", ""MinCount"": 2, ""MaxCount"": 2 }
              ]
            },
            {
              ""Name"": ""NeverRolls"",
              ""Entries"": [ { ""Blueprint"": ""Dagger"", ""Chance"": 0 } ]
            },
            {
              ""Name"": ""PickTwo"",
              ""PickOne"": true, ""MinPicks"": 2, ""MaxPicks"": 2,
              ""Entries"": [
                { ""Blueprint"": ""Dagger"", ""Weight"": 5 },
                { ""Blueprint"": ""Torch"", ""Weight"": 5 },
                { ""Blueprint"": ""LongSword"", ""Weight"": 0 }
              ]
            },
            {
              ""Name"": ""Outer"",
              ""Entries"": [ { ""TableRef"": ""AlwaysBoth"" } ]
            }
          ]
        }";

        [Test]
        public void Roll_IndependentMode_GuaranteedEntriesAlwaysLand()
        {
            LootTableRegistry.Initialize(TestJson);
            var rolled = LootTableRegistry.Roll("AlwaysBoth", new Random(7));
            Assert.AreEqual(1, rolled.FindAll(b => b == "Dagger").Count, "default 1x at 100%");
            Assert.AreEqual(2, rolled.FindAll(b => b == "Torch").Count, "fixed 2x at 100%");
        }

        [Test]
        public void Roll_ChanceZero_NeverLands_CounterCheck()
        {
            LootTableRegistry.Initialize(TestJson);
            for (int seed = 0; seed < 50; seed++)
                Assert.IsEmpty(LootTableRegistry.Roll("NeverRolls", new Random(seed)),
                    $"Chance 0 must never fire (seed {seed})");
        }

        [Test]
        public void Roll_PickMode_RespectsPickCountAndWeights()
        {
            LootTableRegistry.Initialize(TestJson);
            for (int seed = 0; seed < 50; seed++)
            {
                var rolled = LootTableRegistry.Roll("PickTwo", new Random(seed));
                Assert.AreEqual(2, rolled.Count, $"exactly MinPicks==MaxPicks==2 (seed {seed})");
                Assert.IsFalse(rolled.Contains("LongSword"),
                    $"weight-0 entries are never picked (seed {seed})");
            }
        }

        [Test]
        public void Roll_TableRef_ResolvesNestedTable()
        {
            LootTableRegistry.Initialize(TestJson);
            var rolled = LootTableRegistry.Roll("Outer", new Random(3));
            Assert.Contains("Dagger", rolled);
            Assert.AreEqual(2, rolled.FindAll(b => b == "Torch").Count);
        }

        [Test]
        public void Roll_UnknownTable_ReturnsEmpty()
        {
            LootTableRegistry.Initialize(TestJson);
            Assert.IsEmpty(LootTableRegistry.Roll("NoSuchTable", new Random(1)));
        }

        // ── 2. Validation is LOUD ────────────────────────────────

        [Test]
        public void Validate_FlagsUnknownBlueprintAndTableRef()
        {
            LootTableRegistry.Initialize(@"{ ""Tables"": [
                { ""Name"": ""Bad"", ""Entries"": [
                    { ""Blueprint"": ""NoSuchThing"" },
                    { ""TableRef"": ""NoSuchTable"" }
                ]}]}");
            var problems = LootTableRegistry.Validate(bp => _factory.Blueprints.ContainsKey(bp));
            Assert.AreEqual(2, problems.Count, string.Join("; ", problems));
            StringAssert.Contains("NoSuchThing", problems[0]);
            StringAssert.Contains("NoSuchTable", problems[1]);
        }

        [Test]
        public void Validate_FlagsCycles()
        {
            LootTableRegistry.Initialize(@"{ ""Tables"": [
                { ""Name"": ""A"", ""Entries"": [ { ""TableRef"": ""B"" } ] },
                { ""Name"": ""B"", ""Entries"": [ { ""TableRef"": ""A"" } ] }
            ]}");
            var problems = LootTableRegistry.Validate(bp => true);
            Assert.IsTrue(problems.Exists(p => p.Contains("cycle") || p.Contains("Cycle")),
                string.Join("; ", problems));
        }

        [Test]
        public void Validate_FlagsEntryWithBothOrNeitherTarget()
        {
            LootTableRegistry.Initialize(@"{ ""Tables"": [
                { ""Name"": ""Both"", ""Entries"": [ { ""Blueprint"": ""Dagger"", ""TableRef"": ""Both"" } ] },
                { ""Name"": ""Neither"", ""Entries"": [ { ""Weight"": 3 } ] }
            ]}");
            var problems = LootTableRegistry.Validate(bp => true);
            Assert.GreaterOrEqual(problems.Count, 2, string.Join("; ", problems));
        }

        [Test]
        public void Validate_CleanContent_NoProblems()
        {
            LootTableRegistry.Initialize(TestJson);
            Assert.IsEmpty(LootTableRegistry.Validate(bp => _factory.Blueprints.ContainsKey(bp)));
        }

        // ── 3. The SHIPPED content validates against SHIPPED blueprints ──

        [Test]
        public void ShippedLootTables_ValidateAgainstShippedBlueprints()
        {
            // The content-integrity pin: any future table entry naming a
            // blueprint that doesn't exist fails THIS test, not a silent
            // in-game no-op.
            LoadRealTables();
            Assert.Greater(LootTableRegistry.Count, 0, "shipped tables load");
            var problems = LootTableRegistry.Validate(bp => _factory.Blueprints.ContainsKey(bp));
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        // ── 4. LootStocker fills containers ──────────────────────

        [Test]
        public void LootStocker_FillsChestFromTable_AndEmitsDiag()
        {
            LoadRealTables();
            var chest = _factory.CreateEntity("Chest");
            Diag.ResetAll();

            int added = LootStocker.StockContainer(chest, "CaveSupplyT1", _factory, new Random(11));

            Assert.Greater(added, 0, "supply table stocks at least one item");
            var contents = chest.GetPart<ContainerPart>().Contents;
            Assert.Greater(contents.Count, 0);

            var records = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "loot", Kind = "TableRolled", Limit = 5 }).Records;
            Assert.AreEqual(1, records.Count, "one loot/TableRolled record per stocking");
            StringAssert.Contains("CaveSupplyT1", records[0].PayloadJson);
        }

        [Test]
        public void LootStocker_UnknownTable_StocksNothing_CounterCheck()
        {
            LoadRealTables();
            var chest = _factory.CreateEntity("Chest");
            Assert.AreEqual(0, LootStocker.StockContainer(chest, "NoSuchTable", _factory, new Random(1)));
            Assert.AreEqual(0, chest.GetPart<ContainerPart>().Contents.Count);
        }

        // ── 5. Retrofits: lair pool + grimoire chest are table-driven ──

        [Test]
        public void LairLoot_RolledFromTable_WhenRegistryLoaded()
        {
            LoadRealTables();
            var table = LootTableRegistry.Get("LairLoot");
            Assert.IsNotNull(table, "LairLoot table ships");

            // The lair builder's ground drops must come from this table.
            var zone = new Zone("Overworld.3.3.0");
            var poi = new PointOfInterest(POIType.Lair, "Test Lair", "Beasts")
            { BossBlueprint = "SnapjawChieftain" };
            Assert.IsTrue(new LairBuilder(BiomeType.Cave, poi).BuildZone(zone, _factory, new Random(9)));
            Assert.IsTrue(new LairPopulationBuilder(BiomeType.Cave, poi).BuildZone(zone, _factory, new Random(9)));

            var tableNames = new HashSet<string>();
            foreach (var e in table.Entries) tableNames.Add(e.Blueprint);
            int lootFound = 0;
            foreach (var e in zone.GetAllEntities())
                if (tableNames.Contains(e.BlueprintName)) lootFound++;
            Assert.Greater(lootFound, 0, "lair drops 1-2 items from the LairLoot table");
        }

        [Test]
        public void GrimoireChest_StockedFromTable_SameThreeRites()
        {
            // Retrofit must not change the player-visible contract:
            // exactly the three utility rites, one each.
            LoadRealTables();
            FactionManager.Initialize();
            try
            {
                var poi = new PointOfInterest(POIType.Village, "Test Village", "Villagers");
                var zone = new Zone("Overworld.10.10.0");
                Assert.IsTrue(new VillageBuilder(BiomeType.Cave, poi).BuildZone(zone, _factory, new Random(42)));
                Assert.IsTrue(new VillagePopulationBuilder(poi).BuildZone(zone, _factory, new Random(42)));

                Entity chest = null;
                foreach (var e in zone.GetAllEntities())
                    if (e.BlueprintName == "Chest") { chest = e; break; }
                Assert.IsNotNull(chest);

                var names = new List<string>();
                foreach (var item in chest.GetPart<ContainerPart>().Contents)
                    names.Add(item.BlueprintName);
                names.Sort();
                CollectionAssert.AreEqual(
                    new[] { "KindleRiteGrimoire", "MendingRiteGrimoire", "PurifyWaterGrimoire" },
                    names, "the three utility rites, exactly");
            }
            finally
            {
                FactionManager.Reset();
            }
        }
    }
}
