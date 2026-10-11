using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    [TestFixture] public class UsefulTraderRestockAdversarialTests : UsefulTraderRestockFixture
    {
        [TestCase(99, false)] [TestCase(100, false)] [TestCase(399, false)] [TestCase(400, false)] [TestCase(401, true)] [TestCase(int.MaxValue, true)]
        public void IntervalBoundaryDoesNotRefillEarly(int now, bool fills)
        { var zone = new Zone("trade"); var merchant = Merchant(zone); TraderRestockSystem.RestockZone(zone, now); Assert.AreEqual(fills ? 5 : 0, Units(merchant)); }
        [Test] public void LargeForwardElapsedDoesNotOverflowIntoEarlyRefusal()
        { var zone = new Zone("trade"); var merchant = Merchant(zone); merchant.SetIntProperty(TraderRestockSystem.LastRestockProp, -1000); TraderRestockSystem.RestockZone(zone, int.MaxValue); Assert.AreEqual(5, Units(merchant)); }
        [TestCase("factory")] [TestCase("table")] [TestCase("inventory")] [TestCase("purse")] [TestCase("faction")]
        public void MissingRequirementsDoNotGrantGoods(string missing)
        {
            var zone = new Zone("trade"); var merchant = Merchant(zone); var inv = merchant.GetPart<InventoryPart>();
            switch (missing) { case "factory": TraderRestockSystem.Factory = null; break; case "table": merchant.Properties.Remove(TraderRestockSystem.ShopStockTableProp); break;
                case "inventory": merchant.RemovePart(inv); break; case "purse": merchant.IntProperties.Remove(TradeSystem.CURRENCY_PROP); break; case "faction": merchant.SetTag("Faction", "Beasts"); break; }
            TraderRestockSystem.RestockZone(zone, 401); Assert.IsEmpty(inv.Objects);
        }
        [TestCase(0)] [TestCase(-1)]
        public void EmptyMatchingStacksDoNotSuppressSupply(int count)
        {
            var zone = new Zone("trade"); var merchant = Merchant(zone); var item = Add(merchant, "DriedMeat"); item.GetPart<StackerPart>().StackCount = count;
            TraderRestockSystem.RestockZone(zone, 401); Assert.GreaterOrEqual(Units(merchant), 4);
        }
        [Test] public void FullCapacityDoesNotDeleteSoldGoods()
        {
            var zone = new Zone("trade"); var merchant = Merchant(zone); var junk = Add(merchant, "SpareIronKey"); var inv = merchant.GetPart<InventoryPart>(); inv.MaxWeight = inv.GetCarriedWeight();
            TraderRestockSystem.RestockZone(zone, 401); Assert.AreEqual(0, Units(merchant)); Assert.Contains(junk, inv.Objects); Assert.AreSame(merchant, junk.GetPart<PhysicsPart>().InInventory);
        }
        [Test] public void NoRollRetryForZeroChance()
        { Table("{\"Blueprint\":\"DriedMeat\",\"Chance\":0}"); var zone = new Zone("trade"); var merchant = Merchant(zone); TraderRestockSystem.RestockZone(zone, 401); Assert.AreEqual(0, Units(merchant)); Assert.AreEqual(401, merchant.GetIntProperty(TraderRestockSystem.LastRestockProp)); }
        [Test] public void ZeroChanceResidentDoesNotCountAsStock()
        {
            Table("{\"Blueprint\":\"Dagger\",\"Chance\":0},{\"Blueprint\":\"DriedMeat\",\"MinCount\":5,\"MaxCount\":5}");
            var zone = new Zone("trade"); var merchant = Merchant(zone); for (int i = 0; i < 3; i++) Add(merchant, "Dagger");
            TraderRestockSystem.RestockZone(zone, 401); Assert.AreEqual(5, Units(merchant));
        }
        [Test] public void UnselectableWeightedEntryDoesNotCountAsStock()
        {
            Table("{\"Blueprint\":\"Dagger\",\"Weight\":0},{\"Blueprint\":\"DriedMeat\",\"MinCount\":5,\"MaxCount\":5}", properties: "\"PickOne\":true,");
            var zone = new Zone("trade"); var merchant = Merchant(zone); for (int i = 0; i < 3; i++) Add(merchant, "Dagger");
            TraderRestockSystem.RestockZone(zone, 401); Assert.AreEqual(5, Units(merchant));
        }
        [Test] public void NestedReferenceTakesPrecedenceOverBlueprint()
        {
            Table("{\"Blueprint\":\"Dagger\",\"TableRef\":\"Inner\"}", ",{\"Name\":\"Inner\",\"Entries\":[{\"Blueprint\":\"DriedMeat\",\"MinCount\":5,\"MaxCount\":5}]}");
            var zone = new Zone("trade"); var merchant = Merchant(zone); for (int i = 0; i < 3; i++) Add(merchant, "Dagger");
            TraderRestockSystem.RestockZone(zone, 401); Assert.AreEqual(5, Units(merchant));
        }
        [Test] public void WeightedModeIgnoresChanceLikeTheExistingRoller()
        {
            Table("{\"Blueprint\":\"DriedMeat\",\"Chance\":0}", properties: "\"PickOne\":true,");
            var zone = new Zone("trade"); var merchant = Merchant(zone); Add(merchant, "DriedMeat", 3);
            TraderRestockSystem.RestockZone(zone, 401); Assert.AreEqual(3, Units(merchant));
        }
        [Test] public void CyclicReferenceMembershipTerminatesWithoutRerollingAFullShelf()
        {
            Table("{\"TableRef\":\"Inner\"}", ",{\"Name\":\"Inner\",\"Entries\":[{\"TableRef\":\"Stock\"},{\"Blueprint\":\"DriedMeat\"}]}");
            var zone = new Zone("trade"); var merchant = Merchant(zone); Add(merchant, "DriedMeat", 3);
            TraderRestockSystem.RestockZone(zone, 401); Assert.AreEqual(3, Units(merchant));
        }
        [Test] public void ReplacedTableIsReadFreshRatherThanCached()
        {
            var zone = new Zone("trade"); var merchant = Merchant(zone); Add(merchant, "DriedMeat", 3);
            TraderRestockSystem.RestockZone(zone, 401); Assert.AreEqual(3, Units(merchant));
            Table("{\"Blueprint\":\"SpareIronKey\"}"); TraderRestockSystem.RestockZone(zone, 702);
            Assert.AreEqual(1, Units(merchant, "SpareIronKey")); Assert.AreEqual(3, Units(merchant));
        }
        [Test] public void ZeroYieldResidentDoesNotCountAsStock()
        {
            Table("{\"Blueprint\":\"Dagger\",\"MinCount\":0,\"MaxCount\":0},{\"Blueprint\":\"DriedMeat\",\"MinCount\":5,\"MaxCount\":5}");
            var zone = new Zone("trade"); var merchant = Merchant(zone); for (int i = 0; i < 3; i++) Add(merchant, "Dagger");
            TraderRestockSystem.RestockZone(zone, 401); Assert.AreEqual(5, Units(merchant));
        }
    }
}
