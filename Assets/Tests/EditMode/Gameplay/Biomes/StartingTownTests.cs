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
    /// STARTING TOWN (Docs/STARTING-TOWN.md) — the playtest-driven town
    /// pass: five themed shops (weapons / armor / tonics / enchantments
    /// / provisions) as stamp-built buildings scattered among a larger
    /// starting town, keepers with themed stock and in-house crafting
    /// stations (the smith owns a forge, the apothecary a still), the
    /// long-missing armor ladder, and shelf restock so shops stay alive.
    /// </summary>
    [TestFixture]
    public class StartingTownTests
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
            MessageLog.Clear();
            LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Loot/LootTables.json")));
        }

        [TearDown]
        public void TearDown() => LootTableRegistry.ResetForTests();

        private static readonly (string keeper, int wallet, string conv, string table)[] Shops =
        {
            ("Weaponsmith", 300, "Shop_Weaponsmith", "WeaponsmithStock"),
            ("Armorer", 300, "Shop_Armorer", "ArmorerStock"),
            ("Apothecary", 250, "Shop_Apothecary", "ApothecaryStock"),
            ("Arcanist", 400, "Shop_Arcanist", "ArcanistStock"),
            ("Provisioner", 200, "Shop_Provisioner", "ProvisionerStock"),
        };

        // ── 1. Keepers ───────────────────────────────────────────

        [Test]
        public void Shopkeepers_VillagerLineage_WalletsVoicesAndNoRandomStock()
        {
            foreach (var (keeper, wallet, conv, _) in Shops)
            {
                var npc = _factory.CreateEntity(keeper);
                Assert.IsNotNull(npc, keeper);
                Assert.AreEqual("Villagers", npc.Tags["Faction"], keeper);
                Assert.AreEqual(wallet, npc.GetIntProperty(TradeSystem.CURRENCY_PROP, -1), keeper);
                Assert.AreEqual(conv, npc.GetPart<ConversationPart>()?.ConversationID, keeper);
                Assert.IsTrue(npc.HasTag("NoRandomStock"),
                    $"{keeper}: themed shelves must not get random junk");
            }

            var json = File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Conversations/Shopkeepers.json"));
            foreach (var (_, _, conv, _) in Shops)
                StringAssert.Contains($"\"{conv}\"", json);
        }

        // ── 2. The armor ladder ──────────────────────────────────

        [Test]
        public void ArmorLadder_FourNewPieces_FillTheEmptySlots()
        {
            foreach (var (name, av, dv, value) in new[]
            {
                ("LeatherCap", 1, 0, 8),
                ("IronshodBoots", 2, 0, 28),
                ("WardedCloak", 0, 2, 40),
                ("IronBuckler", 2, 1, 45),
            })
            {
                var item = _factory.CreateEntity(name);
                Assert.IsNotNull(item, name);
                var armor = item.GetPart<ArmorPart>();
                Assert.IsNotNull(armor, name);
                Assert.AreEqual(av, armor.AV, name);
                Assert.AreEqual(dv, armor.DV, name);
                Assert.AreEqual(value, item.GetPart<CommercePart>()?.Value, name);
            }
        }

        // ── 3. Stock tables ──────────────────────────────────────

        [Test]
        public void StockTables_ShipThemed()
        {
            bool Has(string table, string bp)
            {
                foreach (var e in LootTableRegistry.Get(table).Entries)
                    if (e.Blueprint == bp) return true;
                return false;
            }
            foreach (var (_, _, _, table) in Shops)
                Assert.IsNotNull(LootTableRegistry.Get(table), table);

            Assert.IsTrue(Has("WeaponsmithStock", "SteelBladeComponent"), "smith sells forge parts");
            Assert.IsTrue(Has("ArmorerStock", "IronBuckler") && Has("ArmorerStock", "PlateArmor"),
                "armorer carries the new ladder and the rare plate shelf");
            Assert.IsTrue(Has("ApothecaryStock", "Antidote"), "apothecary cures");
            Assert.IsTrue(Has("ArcanistStock", "SchematicHonedEdge") && Has("ArcanistStock", "InkVial"),
                "arcanist sells enchantment schematics and ink");
            Assert.IsTrue(Has("ProvisionerStock", "CandyCarrotSeed"), "provisioner seeds the farms");
        }

        // ── 4. shop: marker ──────────────────────────────────────

        [Test]
        public void ShopMarker_SpawnsStockedKeeper()
        {
            var stamp = new StructureStamp
            {
                Name = "ShopTest",
                Chance = 100,
                Rows = new[] { "W" },
                Legend = new Dictionary<char, string> { { 'W', "shop:Weaponsmith:WeaponsmithStock" } },
            };
            var zone = new Zone("T");
            Assert.IsTrue(new CaveBuilder().BuildZone(zone, _factory, new Random(6)));
            new LandmarkBuilder(BiomeType.Cave, 1,
                new List<StructureStamp> { stamp }).BuildZone(zone, _factory, new Random(6));

            Entity keeper = null;
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == "Weaponsmith") { keeper = e; break; }
            Assert.IsNotNull(keeper, "the shop: marker spawns the keeper");
            Assert.Greater(keeper.GetPart<InventoryPart>().Objects.Count, 0,
                "with shelves already stocked from the table");
            Assert.AreEqual("WeaponsmithStock", keeper.GetProperty("ShopStockTable"),
                "and the table remembered for restock");
        }

        // ── 5. The town catalog ──────────────────────────────────

        [Test]
        public void TownCatalog_FiveGuaranteedShops_StationsInHouse()
        {
            var town = StampCatalog.Town();
            Assert.AreEqual(5, town.Count, "five shops");

            var keepersSeen = new HashSet<string>();
            bool forgeInSmithy = false, stillInAlembic = false;
            foreach (var stamp in town)
            {
                Assert.AreEqual(100, stamp.Chance, $"{stamp.Name}: a town shop is guaranteed");
                foreach (var kvp in stamp.Legend)
                {
                    if (kvp.Value.StartsWith("shop:"))
                        keepersSeen.Add(kvp.Value.Split(':')[1]);
                    if (stamp.Name == "TheSmithy" && kvp.Value == "TinkersForge") forgeInSmithy = true;
                    if (stamp.Name == "TheAlembic" && kvp.Value == "AlchemyStill") stillInAlembic = true;
                }
            }
            foreach (var (keeper, _, _, _) in Shops)
                Assert.IsTrue(keepersSeen.Contains(keeper), keeper);
            Assert.IsTrue(forgeInSmithy, "the smith owns a forge");
            Assert.IsTrue(stillInAlembic, "the apothecary owns a still");
        }

        [Test]
        public void StartingVillage_EndToEnd_IsATownWithAllFiveShops()
        {
            var manager = new OverworldZoneManager(_factory, worldSeed: 777);
            var zone = manager.GetZone("Overworld.10.10.0");
            Assert.IsNotNull(zone);

            foreach (var (keeper, _, _, _) in Shops)
            {
                Entity found = null;
                foreach (var e in zone.GetAllEntities())
                    if (e.BlueprintName == keeper) { found = e; break; }
                Assert.IsNotNull(found, $"{keeper} keeps shop in Sill");
                Assert.Greater(found.GetPart<InventoryPart>().Objects.Count, 0,
                    $"{keeper}: open for business");
            }
        }

        [Test]
        public void OtherVillages_StayHamlets_CounterCheck()
        {
            var manager = new OverworldZoneManager(_factory, worldSeed: 777);
            (int x, int y)? other = null;
            for (int x = 0; x < WorldMap.Width && other == null; x++)
                for (int y = 0; y < WorldMap.Height && other == null; y++)
                    if (manager.WorldMap.GetPOI(x, y)?.Type == POIType.Village && !(x == 10 && y == 10))
                        other = (x, y);
            Assert.IsNotNull(other, "worlds have more than one village");

            var zone = manager.GetZone($"Overworld.{other.Value.x}.{other.Value.y}.0");
            foreach (var e in zone.GetAllEntities())
                Assert.AreNotEqual("Weaponsmith", e.BlueprintName,
                    "shops are the starting town's identity");
        }

        // ── 6. Guards ────────────────────────────────────────────

        [Test]
        public void TradeStockBuilder_SkipsNoRandomStock_CounterPair()
        {
            FactionManager.Initialize();
            try
            {
                var zone = new Zone("T");
                var keeper = _factory.CreateEntity("Weaponsmith");
                var merchant = _factory.CreateEntity("Merchant");
                zone.AddEntity(keeper, 3, 3);
                zone.AddEntity(merchant, 5, 5);
                int keeperBefore = keeper.GetPart<InventoryPart>().Objects.Count;

                new TradeStockBuilder().BuildZone(zone, _factory, new Random(2));

                Assert.AreEqual(keeperBefore, keeper.GetPart<InventoryPart>().Objects.Count,
                    "themed shelves stay themed");
                Assert.Greater(merchant.GetPart<InventoryPart>().Objects.Count, 0,
                    "ordinary traders still get the random pool");
            }
            finally
            {
                FactionManager.Reset();
            }
        }

        [Test]
        public void StampFootprints_RejectWater()
        {
            // Pre-existing exposure the town surfaced: nothing stopped a
            // stamp straddling the river. Liquid cells now veto anchors.
            var zone = new Zone("T");
            Assert.IsTrue(new CaveBuilder().BuildZone(zone, _factory, new Random(8)));
            // Flood a band of the zone with water.
            for (int x = 2; x < 78; x++)
                for (int y = 8; y < 14; y++)
                {
                    var cell = zone.GetCell(x, y);
                    if (cell != null && cell.IsPassable())
                        zone.AddEntity(_factory.CreateEntity("WaterPuddle"), x, y);
                }

            var stamp = new StructureStamp
            {
                Name = "DryFeetOnly",
                Chance = 100,
                Rows = new[] { "#####", "#...#", "#####" },
                Legend = new Dictionary<char, string> { { '#', "Wall" } },
            };
            new LandmarkBuilder(BiomeType.Cave, 3,
                new List<StructureStamp> { stamp }).BuildZone(zone, _factory, new Random(8));
            Assert.Greater(zone.GenReservedCells.Count, 0,
                "dry land exists — the stamp must have placed somewhere");

            foreach (var (x, y) in zone.GenReservedCells)
            {
                var cell = zone.GetCell(x, y);
                foreach (var e in cell.Objects)
                    Assert.IsNull(e.GetPart<LiquidPoolPart>(),
                        $"stamp claimed a wet cell at {x},{y}");
            }
        }

        // ── 7. Shelf restock ─────────────────────────────────────

        [Test]
        public void ShopRestock_RefillsAnEmptyShelf_FactoryGated()
        {
            var saved = TraderRestockSystem.Factory;
            FactionManager.Initialize();
            try
            {
                TraderRestockSystem.Factory = _factory;
                var zone = new Zone("T");
                var keeper = _factory.CreateEntity("Weaponsmith");
                keeper.Properties["ShopStockTable"] = "WeaponsmithStock";
                keeper.SetIntProperty(TradeSystem.CURRENCY_PROP, 300);
                zone.AddEntity(keeper, 4, 4);
                Assert.AreEqual(0, keeper.GetPart<InventoryPart>().Objects.Count, "shelf starts bare");

                TraderRestockSystem.RestockZone(zone, currentTurn: 1000);

                Assert.Greater(keeper.GetPart<InventoryPart>().Objects.Count, 2,
                    "an empty shop restocks on the tick");

                // Counter-check: a re-tick inside the interval must not
                // stack more stock on a full shelf.
                int after = keeper.GetPart<InventoryPart>().Objects.Count;
                TraderRestockSystem.RestockZone(zone, currentTurn: 1010);
                Assert.AreEqual(after, keeper.GetPart<InventoryPart>().Objects.Count);
            }
            finally
            {
                TraderRestockSystem.Factory = saved;
                FactionManager.Reset();
            }
        }

        [Test]
        public void LargeTown_BuildsMoreHouses()
        {
            var poi = new PointOfInterest(POIType.Village, "Sill", "Villagers");
            int smallBeds = CountBeds(new VillageBuilder(BiomeType.Cave, poi));
            int largeBeds = CountBeds(new VillageBuilder(BiomeType.Cave, poi, largeTown: true));
            Assert.Greater(largeBeds, smallBeds,
                "the town has visibly more roofs than a hamlet");
        }

        private int CountBeds(VillageBuilder builder)
        {
            var zone = new Zone("T");
            Assert.IsTrue(builder.BuildZone(zone, _factory, new Random(21)));
            int beds = 0;
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == "Bed") beds++;
            return beds;
        }
    }
}
