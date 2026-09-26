using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class DensityDocumentSourceTests
    {
        private EntityFactory factory, previousFactory;
        private Random previousRng;
        [SetUp] public void Setup()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
            LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                "Resources/Content/Data/Loot/LootTables.json")));
            previousFactory = TraderPart.Factory; previousRng = TraderPart.Rng;
            TraderPart.Factory = factory; ReadableDocumentCatalog.ResetForTests();
        }
        [TearDown] public void Cleanup()
        { TraderPart.Factory = previousFactory; TraderPart.Rng = previousRng; LootTableRegistry.ResetForTests(); ReadableDocumentCatalog.ResetForTests(); }

        [TestCase("01", "LibraryShelfT1", "Chest")]
        [TestCase("02", "DrifterStock", "GlassblownDrifter")]
        [TestCase("03", "BookshelfT2", "Bookshelf")]
        [TestCase("04", "ScribeStock", "Scribe")]
        [TestCase("05", "UndertakerStock", "Undertaker")]
        [TestCase("06", "MerchantStock", "Merchant")]
        [TestCase("07", "CuratorStock", "PaleCurator")]
        [TestCase("08", "BookshelfT1", "Bookshelf")]
        [TestCase("09", "TombVaultT2", "Chest")]
        [TestCase("10", "BookshelfT2", "Bookshelf")]
        [TestCase("11", "ChoirStock", "ChoirTendril")]
        [TestCase("12", "ElderStock", "Elder")]
        [TestCase("13", "SealedVaultT3", "Chest")]
        public void ActualStockedSourceContainsReadableCopy_ZeroSourceControl(string suffix, string tableName, string sourceBlueprint)
        {
            string blueprint = "Codex" + suffix;
            var table = LootTableRegistry.Get(tableName); Assert.NotNull(table);
            var entry = table.Entries.SingleOrDefault(e => e.Blueprint == blueprint);
            Assert.NotNull(entry, "a catalog entry alone does not make a book discoverable");
            int seed = Enumerable.Range(0, 4096).First(s => LootTableRegistry.Roll(tableName, new Random(s)).Contains(blueprint));
            Assert.IsTrue(StockAndFind(sourceBlueprint, tableName, blueprint, seed));
            if (table.PickOne) entry.Weight = 0; else entry.Chance = 0;
            Assert.IsFalse(StockAndFind(sourceBlueprint, tableName, blueprint, seed));
        }

        [Test] public void EveryShippedTextMatchesCanonicalWordsAndOrder()
        {
            foreach (var document in ReadableDocumentCatalog.All)
            {
                string path = Path.Combine(UnityEngine.Application.dataPath, "..", document.Source);
                var lines = File.ReadAllText(path).Replace("\r\n", "\n").Split('\n');
                int title = Array.FindIndex(lines, l => l.StartsWith("# "));
                Assert.GreaterOrEqual(title, 0);
                Assert.AreEqual(lines[title].Substring(2).Trim(), document.Title);
                string body = string.Join("\n", lines.Skip(title + 1).Where(l => l.Trim() != "---")
                    .Select(l => Regex.Replace(l, @"^#{1,6}\s+", "").Replace("*", ""))).Trim();
                Assert.AreEqual(body, document.Text, document.Id + " canonical prose must remain intact");
            }
        }

        private bool StockAndFind(string sourceBlueprint, string tableName, string blueprint, int seed)
        {
            TraderPart.Rng = new Random(seed);
            var source = factory.CreateEntity(sourceBlueprint);
            System.Collections.Generic.IEnumerable<Entity> contents;
            if (source.HasPart<TraderPart>())
            {
                Assert.AreEqual(tableName, source.GetPart<TraderPart>().StockTable);
                contents = source.GetPart<InventoryPart>().Objects;
            }
            else
            {
                LootStocker.StockContainer(source, tableName, factory, new Random(seed));
                contents = source.GetPart<ContainerPart>().Contents;
            }
            var found = contents.FirstOrDefault(e => e.BlueprintName == blueprint);
            if (found == null) return false;
            Assert.AreEqual("codex-" + blueprint.Substring(5), found.GetPart<ReadableDocumentPart>().DocumentId);
            return true;
        }
    }
}
