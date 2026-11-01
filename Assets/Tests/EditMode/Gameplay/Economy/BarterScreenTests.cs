using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// EC6c (Docs/ECONOMY-SYSTEM.md §12.3): the barter screen. A barter trader opens straight into deal mode;
    /// a dram trader opens the shop and [D] switches. Keys mark units on either side, [S] balances with salt,
    /// [O] offers, and the balance and any refusal are painted on the screen itself. Also the salt purse a
    /// barter trader is restocked with instead of drams.
    /// </summary>
    public sealed class BarterScreenTests
    {
        static EntityFactory factory;
        EntityFactory oldTrader, oldLoadout, oldRestock;
        readonly List<GameObject> objects = new List<GameObject>();
        const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        [OneTimeSetUp] public void LoadOnce()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp] public void Setup()
        {
            FactionManager.Initialize(); PlayerReputation.Reset(); MessageLog.Clear(); Diag.ResetAll();
            oldTrader = TraderPart.Factory; oldLoadout = LoadoutPart.Factory; oldRestock = TraderRestockSystem.Factory;
            TraderPart.Factory = null; LoadoutPart.Factory = null; TraderRestockSystem.Factory = null;
        }
        [TearDown] public void TearDown()
        {
            foreach (var o in objects) Object.DestroyImmediate(o);
            objects.Clear();
            TraderPart.Factory = oldTrader; LoadoutPart.Factory = oldLoadout; TraderRestockSystem.Factory = oldRestock;
            FactionManager.Reset(); PlayerReputation.Reset(); MessageLog.Clear(); Diag.ResetAll();
        }

        static (int, int) Cell(System.Func<int, int, bool> where)
        {
            for (int y = 0; y < WorldMap.Height; y++) for (int x = 0; x < WorldMap.Width; x++) if (where(x, y)) return (x, y);
            Assert.Fail("no such cell"); return (-1, -1);
        }
        static (int, int) Deep => Cell((x, y) => Tender.IsDeepWasteland(WorldMap.ToZoneID(x, y, 0)));
        static (int, int) Spread => Cell((x, y) => WorldMapAuthoring.BiomeAt(x, y) == BiomeType.Spread);

        (Zone zone, Entity player, Entity trader, TradeUI ui) Screen((int x, int y) cell)
        {
            var zone = new Zone(WorldMap.ToZoneID(cell.x, cell.y, 0));
            var trader = factory.CreateEntity("Merchant"); zone.AddEntity(trader, 10, 10);
            trader.GetPart<InventoryPart>().Objects.Clear();
            var player = factory.CreateEntity("Player"); zone.AddEntity(player, 11, 10);
            var ego = player.GetStat("Ego"); ego.Max = System.Math.Max(ego.Max, 40); ego.BaseValue = 34;
            TradeSystem.SetDrams(player, 100); TradeSystem.SetDrams(trader, 100);
            var host = new GameObject("TradeUI"); host.SetActive(false); objects.Add(host);
            var ui = host.AddComponent<TradeUI>();
            var mapHost = new GameObject("Tilemap"); mapHost.SetActive(false); objects.Add(mapHost);
            ui.Tilemap = mapHost.AddComponent<Tilemap>(); ui.PlayerEntity = player; ui.CurrentZone = zone;
            return (zone, player, trader, ui);
        }
        static Entity Give(Entity holder, string blueprint, int count = 1)
        {
            var item = factory.CreateEntity(blueprint);
            var stacker = item.GetPart<StackerPart>(); if (stacker != null) stacker.StackCount = count;
            Assert.True(holder.GetPart<InventoryPart>().AddObject(item));
            return item;
        }
        static string Row(TradeUI ui, int y)
        {
            string text = "";
            for (int x = 0; x < 80; x++)
            {
                var tile = ui.Tilemap.GetTile(new Vector3Int(x, 44 - y, 0)); char glyph = ' ';
                if (tile != null) for (int c = 33; c < 127; c++) if (tile == CP437TilesetGenerator.GetUiTile((char)c)) { glyph = (char)c; break; }
                text += glyph;
            }
            return text;
        }
        static void Select(TradeUI ui, int panel, int index)
        {
            typeof(TradeUI).GetField("_panel", Private).SetValue(ui, panel);
            typeof(TradeUI).GetField(panel == 0 ? "_leftCursor" : "_rightCursor", Private).SetValue(ui, index);
        }

        [Test] public void ABarterTraderOpensTheBarterScreen()
        {
            var (_, _, trader, ui) = Screen(Deep);
            Give(trader, "HealingTonic");
            ui.Open(trader);
            Assert.True(ui.IsDealMode);
            StringAssert.Contains("Barter with", Row(ui, 0));
            StringAssert.Contains("not drams", Row(ui, 1));
            StringAssert.DoesNotContain("$", Row(ui, 1), "no purse is shown where drams are dead weight");
            StringAssert.Contains("Mark goods on either side", Row(ui, 41));
            StringAssert.Contains("healing tonic", Row(ui, 3));
            StringAssert.DoesNotContain("$", Row(ui, 3), "values, not drams, on a barter row (found in live play)");
            Assert.False(ui.DealKey(KeyCode.D), "a barter trader has no shop to return to");
            Assert.True(ui.IsDealMode);
        }

        [Test] public void ADramTraderOpensTheShopAndDSwitchesToADeal()
        {
            var (_, _, trader, ui) = Screen(Spread);
            Give(trader, "HealingTonic");
            ui.Open(trader);
            Assert.False(ui.IsDealMode, "counter-check: a dram trader opens the shop");
            StringAssert.Contains("Buy from", Row(ui, 0)); StringAssert.Contains("[D]deal", Row(ui, 44));
            StringAssert.Contains("$", Row(ui, 3), "counter-check: a dram shop prices in drams");
            Assert.True(ui.DealKey(KeyCode.D)); Assert.True(ui.IsDealMode);
            StringAssert.Contains("Deal with", Row(ui, 0)); StringAssert.Contains("Trader: $", Row(ui, 1));
            Assert.True(ui.DealKey(KeyCode.D)); Assert.False(ui.IsDealMode);
        }

        [Test] public void MarkingAndOfferingThroughTheScreenTradesTheGoods()
        {
            var (_, player, trader, ui) = Screen(Deep);
            var tonic = Give(trader, "HealingTonic"); var sword = Give(player, "LongSword");
            ui.Open(trader);
            Select(ui, 0, 0); Assert.True(ui.DealKey(KeyCode.Return)); Assert.AreEqual(1, ui.Deal.Taking(tonic));
            StringAssert.Contains("Short", Row(ui, 41));
            Select(ui, 1, 0); Assert.True(ui.DealKey(KeyCode.Return)); Assert.AreEqual(1, ui.Deal.Giving(sword));
            StringAssert.Contains("They keep", Row(ui, 41));
            StringAssert.Contains("*", Row(ui, 3), "a marked row shows its mark");
            Assert.True(ui.DealKey(KeyCode.O));
            StringAssert.Contains("Offer Deal", string.Join("\n", Enumerable.Range(15, 12).Select(y => Row(ui, y))));
            ui.ConfirmPendingDeal();
            Assert.True(player.GetPart<InventoryPart>().Contains(tonic)); Assert.True(trader.GetPart<InventoryPart>().Contains(sword));
            Assert.True(ui.Deal.IsEmpty, "a fresh deal after success");
            Assert.AreEqual(100, TradeSystem.GetDrams(player));
        }

        [Test] public void AShortOfferIsExplainedOnTheScreen()
        {
            var (_, player, trader, ui) = Screen(Deep);
            var sword = Give(trader, "LongSword");
            ui.Open(trader);
            Select(ui, 0, 0); ui.DealKey(KeyCode.Return);
            Assert.True(ui.DealKey(KeyCode.O));
            Assert.AreEqual(Barter.Explain("short"), ui.StatusMessage);
            StringAssert.Contains("Not enough", Row(ui, 44));
            Assert.True(trader.GetPart<InventoryPart>().Contains(sword), "nothing moved");
        }

        [Test] public void LeftRemovesAndAllTogglesTheWholeStack()
        {
            var (_, player, trader, ui) = Screen(Deep);
            var salt = Give(player, "PaleSalt", 9);
            ui.Open(trader);
            Select(ui, 1, 0);
            ui.DealKey(KeyCode.RightArrow); ui.DealKey(KeyCode.RightArrow); Assert.AreEqual(2, ui.Deal.Giving(salt));
            StringAssert.Contains("2/9", Row(ui, 3));
            ui.DealKey(KeyCode.LeftArrow); Assert.AreEqual(1, ui.Deal.Giving(salt));
            ui.DealKey(KeyCode.A); Assert.AreEqual(9, ui.Deal.Giving(salt));
            ui.DealKey(KeyCode.A); Assert.AreEqual(0, ui.Deal.Giving(salt));
            StringAssert.Contains("in deal 0 of 9", Row(ui, 43));
        }

        [Test] public void TheSaltKeyBalancesAShortDeal()
        {
            var (_, player, trader, ui) = Screen(Deep);
            Give(player, "PaleSalt", 40); Give(trader, "LongSword");
            ui.Open(trader);
            Select(ui, 0, 0); ui.DealKey(KeyCode.Return);
            Assert.Less(ui.Deal.Balance, 0, "precondition");
            ui.DealKey(KeyCode.S);
            Assert.GreaterOrEqual(ui.Deal.Balance, 0);
            StringAssert.DoesNotContain("Short", Row(ui, 41));
        }

        [Test] public void TheSaltKeySaysSoWhenThereIsNoSalt()
        {
            var (_, _, trader, ui) = Screen(Deep);
            Give(trader, "LongSword");
            ui.Open(trader);
            Select(ui, 0, 0); ui.DealKey(KeyCode.Return); ui.DealKey(KeyCode.S);
            StringAssert.Contains("no salt", ui.StatusMessage);
        }

        // ════════════════ the salt purse ════════════════

        [Test] public void ABarterTraderIsRestockedWithSaltNotDrams()
        {
            TraderRestockSystem.Factory = factory;
            var (zone, _, trader, _) = Screen(Deep);
            TradeSystem.SetDrams(trader, 0);
            Assert.GreaterOrEqual(TraderRestockSystem.RestockZone(zone, 1000), 1, "the salt purse (and the emptied shelf) restock");
            Assert.AreEqual(0, TradeSystem.GetDrams(trader), "no drams where drams are dead weight");
            int floor = TraderRestockSystem.SaltFloorUnits(trader, factory.CreateEntity("PaleSalt"));
            Assert.Greater(floor, 0);
            int Salt() => trader.GetPart<InventoryPart>().Objects.Where(o => o.HasTag(RegionalValue.SaltTag)).Sum(o => BarterDeal.Units(o));
            Assert.AreEqual(floor, Salt(), "salt worth the dram floor");
            Assert.AreEqual(0, TraderRestockSystem.RestockZone(zone, 1100), "not again within the interval");
            trader.GetPart<InventoryPart>().Objects.RemoveAll(o => o.HasTag(RegionalValue.SaltTag));
            TraderRestockSystem.RestockZone(zone, 1000 + TraderRestockSystem.RestockIntervalTurns + 1);
            Assert.AreEqual(floor, Salt(), "spent salt is topped up after the interval");
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "trade", Kind = "SaltRestocked", Limit = 1 }).Records.Count);
        }

        [Test] public void ADramTraderIsRestockedWithDramsNotSalt()
        {
            TraderRestockSystem.Factory = factory;
            var (zone, _, trader, _) = Screen(Spread);
            TradeSystem.SetDrams(trader, 0);
            TraderRestockSystem.RestockZone(zone, 1000);
            Assert.AreEqual(TraderRestockSystem.DramsFloor, TradeSystem.GetDrams(trader));
            Assert.False(trader.GetPart<InventoryPart>().Objects.Any(o => o.HasTag(RegionalValue.SaltTag)));
        }

        /// <summary>The purse is worth the same everywhere: fewer units where salt is precious.</summary>
        [Test] public void TheSaltPurseIsWorthTheDramFloorAtTheLocalPrice()
        {
            var salt = factory.CreateEntity("PaleSalt");
            var zone = new Zone(WorldMap.ToZoneID(Deep.Item1, Deep.Item2, 0));
            var trader = factory.CreateEntity("Merchant"); zone.AddEntity(trader, 10, 10);
            int units = TraderRestockSystem.SaltFloorUnits(trader, salt);
            double unit = 12 * RegionalValue.Modifier(salt, trader);
            Assert.GreaterOrEqual(units * unit, TraderRestockSystem.DramsFloor);
            Assert.Less((units - 1) * unit, TraderRestockSystem.DramsFloor);
        }
    }
}
