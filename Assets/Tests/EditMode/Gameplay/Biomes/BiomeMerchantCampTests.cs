using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using Application = UnityEngine.Application;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// BIOME-OVERHAUL Phase B1 — merchant camps become real
    /// (Docs/BIOME-OVERHAUL.md §6 B1, log in Docs/BIOME-OVERHAUL-LOG.md).
    /// WorldGenerator places 2-3 MerchantCamp POIs per world (glyph $
    /// on the map), but the pipeline fell through to plain wilderness:
    /// no merchant, no camp, nothing — a map marker pointing at
    /// disappointment. Camps now guarantee a per-biome camp stamp:
    /// tent walls, a campfire (restable — this is the wilderness
    /// reprieve node), a Merchant and a Warden guard (both auto-stocked
    /// by TradeStockBuilder, both with wallets), and a supply chest.
    /// </summary>
    [TestFixture]
    public class BiomeMerchantCampTests
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
        public void Setup()
        {
            LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Loot/LootTables.json")));
        }

        [TearDown]
        public void TearDown() => LootTableRegistry.ResetForTests();

        private static int CountByBlueprint(Zone zone, string bp)
        {
            int n = 0;
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == bp) n++;
            return n;
        }

        // ── 1. Camp stamps exist and validate per biome ──────────

        [Test]
        public void CampStamps_ExistForAllBiomes_AndValidate()
        {
            foreach (var biome in new[] { BiomeType.Cave, BiomeType.Desert, BiomeType.Jungle, BiomeType.Ruins })
            {
                var stamp = StampCatalog.MerchantCamp(biome);
                Assert.IsNotNull(stamp, biome.ToString());
                Assert.AreEqual(100, stamp.Chance,
                    $"{biome}: a POI camp is guaranteed, not a dice roll");

                foreach (var kvp in stamp.Legend)
                {
                    string marker = kvp.Value;
                    if (string.IsNullOrEmpty(marker)) continue;
                    if (marker.StartsWith("chest:"))
                        Assert.IsNotNull(LootTableRegistry.Get(marker.Substring(6)),
                            $"{biome}: table '{marker}'");
                    else
                    {
                        string bp = marker.StartsWith("spawn:") ? marker.Substring(6) : marker;
                        Assert.IsTrue(_factory.Blueprints.ContainsKey(bp),
                            $"{biome}: unknown blueprint '{bp}'");
                    }
                }
            }
        }

        // ── 2. A placed camp has the full reprieve kit ───────────

        [Test]
        public void PlacedCamp_HasTraderGuardFireAndStockedChest()
        {
            var zone = new Zone("Overworld.4.4.0");
            Assert.IsTrue(new CaveBuilder().BuildZone(zone, _factory, new Random(7)));
            new LandmarkBuilder(BiomeType.Cave, 1,
                new List<StructureStamp> { StampCatalog.MerchantCamp(BiomeType.Cave) })
                .BuildZone(zone, _factory, new Random(7));

            Assert.AreEqual(1, CountByBlueprint(zone, "Merchant"), "the trader");
            Assert.AreEqual(1, CountByBlueprint(zone, "Warden"), "the guard");
            Assert.AreEqual(1, CountByBlueprint(zone, "Campfire"), "the restable fire");

            Entity chest = null;
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == "Chest") { chest = e; break; }
            Assert.IsNotNull(chest, "the supply chest");
            Assert.Greater(chest.GetPart<ContainerPart>().Contents.Count, 0, "stocked");
        }

        // ── 3. End-to-end: a MerchantCamp POI zone contains a camp ──

        [Test]
        public void MerchantCampPOIZone_EndToEnd_ContainsTheCamp()
        {
            // The full routing chain: WorldGenerator's POI → the new
            // CreateMerchantCampPipeline → guaranteed camp stamp. Uses a
            // real OverworldZoneManager on the real blueprint set.
            var manager = new OverworldZoneManager(_factory, worldSeed: 1234);

            (int x, int y)? campCell = null;
            for (int x = 0; x < WorldMap.Width && campCell == null; x++)
                for (int y = 0; y < WorldMap.Height && campCell == null; y++)
                    if (manager.WorldMap.GetPOI(x, y)?.Type == POIType.MerchantCamp)
                        campCell = (x, y);
            Assert.IsNotNull(campCell, "every world places 2-3 merchant camps");

            var zone = manager.GetZone($"Overworld.{campCell.Value.x}.{campCell.Value.y}.0");
            Assert.IsNotNull(zone);

            // Diagnostic-rich failure: biome, reserved-cell count, and a
            // blueprint census beat a bare "0".
            var census = new Dictionary<string, int>();
            foreach (var e in zone.GetAllEntities())
            {
                census.TryGetValue(e.BlueprintName, out int n);
                census[e.BlueprintName] = n + 1;
            }
            var lines = new List<string>();
            foreach (var kvp in census) lines.Add($"{kvp.Key}x{kvp.Value}");
            lines.Sort();
            string diag = $"biome={manager.WorldMap.GetBiome(campCell.Value.x, campCell.Value.y)} " +
                $"cell={campCell.Value.x},{campCell.Value.y} reserved={zone.GenReservedCells.Count} " +
                $"census=[{string.Join(" ", lines)}]";

            Assert.GreaterOrEqual(CountByBlueprint(zone, "Merchant"), 1,
                "the $ on the map finally means a merchant — " + diag);
            // RE-BASELINED (W0.6): was AreEqual(1). The invariant is
            // that the camp HAS a hearth, not that the zone has exactly
            // one fire — under the authored map a camp can share its
            // chunk with an ambient stamp that brings its own (a hermit
            // hut, say). The camp's presence is asserted above.
            Assert.GreaterOrEqual(CountByBlueprint(zone, "Campfire"), 1,
                "camp hearth — " + diag);
        }

        [Test]
        public void PlainWildernessZone_HasNoCamp_CounterCheck()
        {
            // The guaranteed camp must be POI-routed, not ambient: a
            // non-POI zone built with the ordinary wilderness catalog
            // must not sprout Merchants.
            var zone = new Zone("Overworld.3.3.0");
            Assert.IsTrue(new CaveBuilder().BuildZone(zone, _factory, new Random(11)));
            new LandmarkBuilder(BiomeType.Cave, 1).BuildZone(zone, _factory, new Random(11));
            Assert.AreEqual(0, CountByBlueprint(zone, "Merchant"));
        }
    }
}
