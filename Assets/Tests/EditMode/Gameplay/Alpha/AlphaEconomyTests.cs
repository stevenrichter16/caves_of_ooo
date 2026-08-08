using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// ALPHA-READINESS item 7 — economy-renewables (P0). The economy
    /// dead-ended: crafting reagents/components had NO renewable source
    /// (the one-time dev kit was it), traders never regained drams after
    /// buying from the player, kills paid nothing, farming produce was
    /// inedible and near-worthless, and every village chest gave away
    /// ~10 grimoires free.
    /// </summary>
    [TestFixture]
    public class AlphaEconomyTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        private static string[] TradeGoodsArray(string field)
        {
            return (string[])typeof(TradeStockBuilder).GetField(field,
                BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        }

        // ── SM1: renewable crafting inputs ───────────────────────

        [Test]
        public void TradeGoods_ContainReagentsAndComponents()
        {
            var goods = TradeGoodsArray("TradeGoods");
            foreach (var item in new[] {
                "FireMoss", "MendleafSprig", "VenomGland", "LampOil",
                "SteelBladeComponent", "OakHaftComponent" })
            {
                CollectionAssert.Contains(goods, item,
                    $"{item} must be purchasable or the brew/forge loops dead-end");
            }
        }

        [Test]
        public void TradeGoods_ContainStarterGrimoires()
        {
            // Redistribution: the chest no longer gives the attack
            // grimoires away free; two circulate in trade.
            var goods = TradeGoodsArray("TradeGoods");
            CollectionAssert.Contains(goods, "KindleGrimoire");
            CollectionAssert.Contains(goods, "QuenchGrimoire");
        }

        // ── SM2: kills pay ───────────────────────────────────────

        [Test]
        public void Corpses_HaveSellValue()
        {
            foreach (var (name, min) in new[] { ("CreatureCorpse", 1), ("SnapjawCorpse", 1) })
            {
                var corpse = _factory.CreateEntity(name);
                var commerce = corpse.GetPart<CommercePart>();
                Assert.IsNotNull(commerce, $"{name} needs a Commerce part to appear in the sell panel");
                Assert.GreaterOrEqual(commerce.Value, min, name);
            }
        }

        // ── SM3: trader restock ──────────────────────────────────

        private Entity MakeTrader(Zone zone, int drams)
        {
            var t = new Entity { ID = "trader", BlueprintName = "Merchant" };
            t.Tags["Creature"] = "";
            t.Tags["Faction"] = "Villagers";
            t.SetIntProperty(TradeSystem.CURRENCY_PROP, drams);
            zone.AddEntity(t, 5, 5);
            return t;
        }

        [Test]
        public void Restock_BrokeTrader_StaleStamp_ToppedToFloor()
        {
            var zone = new Zone("z");
            var trader = MakeTrader(zone, 3);
            trader.SetIntProperty(TraderRestockSystem.LastRestockProp, 100);

            int n = TraderRestockSystem.RestockZone(zone, 100 + TraderRestockSystem.RestockIntervalTurns + 1);

            Assert.AreEqual(1, n);
            Assert.AreEqual(TraderRestockSystem.DramsFloor, TradeSystem.GetDrams(trader),
                "a trader the player sold out must eventually be able to buy again");
        }

        [Test]
        public void Restock_RecentStamp_DoesNothing()
        {
            // Counter-check: no infinite money pump inside the window.
            var zone = new Zone("z");
            var trader = MakeTrader(zone, 3);
            trader.SetIntProperty(TraderRestockSystem.LastRestockProp, 100);

            int n = TraderRestockSystem.RestockZone(zone, 150);

            Assert.AreEqual(0, n);
            Assert.AreEqual(3, TradeSystem.GetDrams(trader));
        }

        [Test]
        public void Restock_NonTrader_Untouched()
        {
            var zone = new Zone("z");
            var wolf = new Entity { ID = "w", BlueprintName = "CaveBear" };
            wolf.Tags["Creature"] = ""; wolf.Tags["Faction"] = "Beasts";
            zone.AddEntity(wolf, 3, 3);

            Assert.AreEqual(0, TraderRestockSystem.RestockZone(zone, 9999));
            Assert.AreEqual(0, TradeSystem.GetDrams(wolf));
        }

        // ── SM5: farming output is food + Ego pricing lever ──────

        [Test]
        public void Produce_IsEdible()
        {
            foreach (var name in new[] { "CandyCarrot", "Emberwheat" })
            {
                var item = _factory.CreateEntity(name);
                var food = item.GetPart<FoodPart>();
                Assert.IsNotNull(food, $"{name} must be edible — farming output was inert");
                Assert.IsFalse(string.IsNullOrEmpty(food.Healing), name);
            }
        }

        [Test]
        public void CandyCarrotCrop_YieldsTwo()
        {
            var crop = _factory.CreateEntity("CandyCarrotCrop");
            Assert.AreEqual(2, crop.GetPart<CropPart>().YieldCount,
                "one carrot per full grow cycle made farming strictly unprofitable");
        }

        [Test]
        public void Player_HasEgoSixteen()
        {
            var player = _factory.CreateEntity("Player");
            Assert.AreEqual(16, player.GetStat("Ego").Value,
                "the Qud-exact Ego pricing lever needs a real player stat");
        }
    }
}
