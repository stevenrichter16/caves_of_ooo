using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class CinderholdSmithConsignmentTests : FiftyWorldFixture
    {
        const string City = "Overworld.6.6.0";
        static readonly string[] Choices = { "SplitHaftTokenTrinket", "ShortSword", "Mace", "Spear", "Hatchet", "LongSword",
            "Battleaxe", "Warhammer", "SteelBladeComponent", "SerratedEdgeComponent", "StunGasGrenade" };
        CavesOfOoo.Data.EntityFactory oldTrader, oldRestock;
        [SetUp] public void Prepare()
        {
            oldTrader = TraderPart.Factory; oldRestock = TraderRestockSystem.Factory;
            TraderPart.Factory = null; TraderRestockSystem.Factory = Factory;
            Zone.ZoneID = City; Pack.MaxWeight = 1000; TradeSystem.SetDrams(Actor, 10000);
        }
        [TearDown] public void Restore()
        {
            TraderPart.Factory = oldTrader; TraderRestockSystem.Factory = oldRestock;
            QuillholdFactoryProbe.CreatedCallback = null; QuillholdFactoryProbe.Callback = null;
        }
        Entity Smith(string bp = "Weaponsmith")
        { var e = Place(bp); e.ID = "cinderhold-supply-smith"; e.GetPart<InventoryPart>().MaxWeight = 1000; TradeSystem.SetDrams(e, 10000); return e; }
        MerchantStockPart Start(Entity e, int tick = 10)
        { Assert.True(MerchantStockService.Reconcile(e, Zone, tick)); var s = e.GetPart<MerchantStockPart>(); Assert.NotNull(s); return s; }
        static int Units(Entity e, string bp) => e.GetPart<InventoryPart>().Objects.Where(i => i.BlueprintName == bp).Sum(BarterDeal.Units);
        void Probe(string bp, Action<Entity> callback)
        {
            Factory.RegisterPartType<QuillholdFactoryProbe>();
            Factory.Blueprints[bp].Parts[nameof(QuillholdFactoryProbe)] = new Dictionary<string, string>();
            QuillholdFactoryProbe.CreatedCallback = callback;
        }
        Entity Saved(Entity e)
        {
            using (var stream = new MemoryStream()) {
                var w = new SaveWriter(stream); w.WriteEntityReference(e); w.WriteQueuedEntityBodies(); stream.Position = 0;
                var r = new SaveReader(stream, Factory); var result = r.ReadEntityReference(); r.ReadEntityBodies(); return result;
            }
        }
        [TestCase("Dagger", 1)] [TestCase("IronSpikeComponent", 2)] [TestCase("OakHaftComponent", 1)] [TestCase("LeatherBindingComponent", 1)]
        public void GenuineLocalSmithKeepsPromisedRepairInputsAndGuaranteedDagger(string bp, int count)
        { var e = Smith(); Start(e); Assert.AreEqual(count, Units(e, bp)); }

        [Test] public void AllOriginalOptionalIdentitiesRemainAvailableAsStandardsOrSpecials()
        {
            var e = Smith(); var s = Start(e); var seen = new HashSet<string>(); string previous = null;
            for (int n = 0; n < Choices.Length; n++) {
                if (n > 0) Assert.True(MerchantStockService.Reconcile(e, Zone, 10 + n * 1200));
                Assert.AreEqual(2, s.Specials.Count); Assert.False(s.BonusApplied); Assert.AreNotEqual(previous, s.SelectionRaw); previous = s.SelectionRaw;
                foreach (var item in s.Specials) { seen.Add(item.BlueprintName); Assert.AreEqual(1, BarterDeal.Units(item)); Assert.True(MerchantStockService.IsSpecial(e, item)); }
            }
            Assert.AreEqual(9, seen.Count); seen.Add("ShortSword"); seen.Add("Spear");
            Assert.AreEqual(1, Units(e, "ShortSword")); Assert.AreEqual(1, Units(e, "Spear"));
            CollectionAssert.AreEquivalent(Choices, seen);
        }
        [TestCase("Overworld.6.6.1")] [TestCase("Overworld.10.10.0")] [TestCase("Overworld.14.9.0")]
        public void OtherGenuineSmithsJoinWithoutCinderholdRegionalSpikeGrant(string zone)
        { Zone.ZoneID = zone; var e = Smith(); Start(e); Assert.AreEqual(1, Units(e, "ShortSword")); Assert.AreEqual(1, Units(e, "Spear")); Assert.Zero(Units(e, "IronSpikeComponent")); }
        [TestCase("Tinker")] [TestCase("Quartermaster")]
        public void ExistingAlternateSuppliersDoNotJoinTheSmithProfile(string bp)
        { var e = Smith(bp); Assert.False(MerchantStockService.Reconcile(e, Zone, 10)); Assert.IsNull(e.GetPart<MerchantStockPart>()); }
        [TestCase(false)] [TestCase(true)]
        public void BothStockDeclarationsAreRequired(bool property)
        {
            var e = Smith(); if (property) e.Properties[TraderRestockSystem.ShopStockTableProp] = "MerchantStock"; else e.GetPart<TraderPart>().StockTable = "MerchantStock";
            Assert.False(MerchantStockService.Reconcile(e, Zone, 10)); Assert.IsNull(e.GetPart<MerchantStockPart>());
        }
        [Test] public void ExistingRegionalGoodsAndPartialFloorsSurviveWithoutAdditiveGrants()
        {
            var e = Smith(); var pack = e.GetPart<InventoryPart>();
            foreach (var bp in new[] { "IronSpikeComponent", "OakHaftComponent", "LeatherBindingComponent", "CinderhookAxeHeadComponent", "KilnfeltApron", "BracedHaftComponent" })
                Assert.True(pack.AddObject(Factory.CreateEntity(bp)));
            var old = pack.Objects.ToArray(); Start(e); Assert.AreEqual(2, Units(e, "IronSpikeComponent"));
            Assert.AreEqual(1, Units(e, "OakHaftComponent")); Assert.AreEqual(1, Units(e, "LeatherBindingComponent"));
            foreach (var item in old) Assert.Contains(item, pack.Objects);
            MerchantStockService.Reconcile(e, Zone, 311); Assert.AreEqual(1, Units(e, "CinderhookAxeHeadComponent")); Assert.AreEqual(1, Units(e, "BracedHaftComponent"));
        }
        [Test] public void BoughtInputsPayNormallyAndOnlyDeficitsReturnAfterStrictBoundary()
        {
            var e = Smith(); var s = Start(e); var spike = e.GetPart<InventoryPart>().Objects.Single(i => i.BlueprintName == "IronSpikeComponent");
            int before = TradeSystem.GetDrams(Actor); Assert.True(TradeSystem.BuyFromTrader(Actor, e, spike)); Assert.Less(TradeSystem.GetDrams(Actor), before);
            Assert.AreEqual(2, BarterDeal.Units(spike)); MerchantStockService.Reconcile(e, Zone, 310); Assert.AreEqual(0, Units(e, "IronSpikeComponent"));
            Assert.True(MerchantStockService.Reconcile(e, Zone, 311)); Assert.AreEqual(2, Units(e, "IronSpikeComponent")); Assert.AreEqual(0, s.Generation);
        }
        [Test] public void OldSaveActivatesOnOpeningAndCurrentLotRoundTripsWithoutReroll()
        {
            var e = Smith(); var old = Saved(e); Assert.IsNull(old.GetPart<MerchantStockPart>());
            Assert.True(Zone.RemoveEntity(e)); Assert.True(Zone.AddEntity(old, 11, 10)); var s = Start(old); var copy = Saved(old); var t = copy.GetPart<MerchantStockPart>();
            Assert.AreEqual(s.SelectionRaw, t.SelectionRaw); CollectionAssert.AreEqual(s.Specials.Select(i => i.ID), t.Specials.Select(i => i.ID));
            Assert.AreEqual(2, Units(copy, "IronSpikeComponent")); Assert.AreEqual(10, t.LastSpecialTick);
        }
        [Test] public void ViewAndTradeReservationsProtectTheExistingLot()
        {
            var e = Smith(); var s = Start(e); using (MerchantStockService.BeginView(e)) Assert.False(MerchantStockService.Reconcile(e, Zone, 1210));
            var deal = new BarterDeal(Actor, e); Assert.True(deal.SetTake(s.Specials[0], 1));
            Assert.False(MerchantStockService.Reconcile(e, Zone, 1210)); MerchantStockService.ForgetOffer(deal);
            Assert.True(MerchantStockService.Reconcile(e, Zone, 1210)); Assert.AreEqual(1, s.Generation);
        }
        [Test] public void BoughtAndResoldSpecialSurvivesLaterRotation()
        {
            var e = Smith(); var s = Start(e); var item = s.Specials[0]; Assert.True(TradeSystem.BuyFromTrader(Actor, e, item)); Assert.True(TradeSystem.SellToTrader(Actor, e, item));
            Assert.False(MerchantStockService.IsSpecial(e, item)); Assert.True(MerchantStockService.Reconcile(e, Zone, 1210)); Assert.Contains(item, e.GetPart<InventoryPart>().Objects);
        }
        [Test] public void ChangedResidentComponentSurvivesAndCannotHideTheCanonicalDeficit()
        {
            var e = Smith(); Start(e); var item = e.GetPart<InventoryPart>().Objects.Single(i => i.BlueprintName == "IronSpikeComponent");
            item.GetPart<WeaponComponentPart>().PenBonus = 99; Assert.True(MerchantStockService.Reconcile(e, Zone, 311));
            Assert.Contains(item, e.GetPart<InventoryPart>().Objects); Assert.AreEqual(4, Units(e, "IronSpikeComponent")); Assert.AreEqual(99, item.GetPart<WeaponComponentPart>().PenBonus);
        }
        public static IEnumerable<TestCaseData> ComponentFields()
        {
            foreach (string bp in new[] { "IronSpikeComponent", "OakHaftComponent", "LeatherBindingComponent" })
                foreach (string field in new[] { "Slot", "BaseDamage", "PenBonus", "HitBonus", "MaxStrengthBonus", "Attributes", "OnHitEffectSpec", "NameFragment" })
                    yield return new TestCaseData(bp, field);
        }
        [TestCaseSource(nameof(ComponentFields))]
        public void FactoryCannotRedefineAnyFreshRepairPayloadField(string bp, string name)
        {
            var e = Smith(); bool fired = false; Probe(bp, item => { fired = true; var field = typeof(WeaponComponentPart).GetField(name); field.SetValue(item.GetPart<WeaponComponentPart>(), field.FieldType == typeof(string) ? (object)"changed" : 99); });
            Assert.False(MerchantStockService.Reconcile(e, Zone, 10)); Assert.IsNull(e.GetPart<MerchantStockPart>()); Assert.IsEmpty(e.GetPart<InventoryPart>().Objects); Assert.True(fired);
        }
        [TestCase("GasId")] [TestCase("Density")] [TestCase("Level")] [TestCase("SightCloud")] [TestCase("SightCloudTurns")]
        public void FreshStunPayloadMustKeepEveryAuthoredGasField(string name)
        {
            var e = Smith(); bool fired = false;
            Probe("StunGasGrenade", item => { fired = true; var field = typeof(GasGrenadePart).GetField(name); field.SetValue(item.GetPart<GasGrenadePart>(), field.FieldType == typeof(string) ? (object)"wrong" : 99); });
            for (int n = 0; n < Choices.Length && !fired; n++) { var before = e.GetPart<InventoryPart>().Objects.ToArray(); bool result = MerchantStockService.Reconcile(e, Zone, 10 + n * 1200); if (fired) { Assert.False(result); CollectionAssert.AreEqual(before, e.GetPart<InventoryPart>().Objects); } else Assert.True(result); }
            Assert.True(fired);
        }
        [Test] public void FactoryChangingLocationCannotPublishLocalStock()
        {
            var e = Smith(); bool fired = false; Probe("IronSpikeComponent", item => { fired = true; Zone.ZoneID = "Overworld.10.10.0"; });
            Assert.False(MerchantStockService.Reconcile(e, Zone, 10)); Assert.IsNull(e.GetPart<MerchantStockPart>()); Assert.IsEmpty(e.GetPart<InventoryPart>().Objects); Assert.True(fired);
        }
        [Test] public void RefusedComponentPurchaseKeepsPursesAndSupply()
        {
            var e = Smith(); Start(e); var item = e.GetPart<InventoryPart>().Objects.Single(i => i.BlueprintName == "IronSpikeComponent");
            Actor.AddPart(new MerchantBeforeTradeProbe { Veto = true }); int buyer = TradeSystem.GetDrams(Actor), seller = TradeSystem.GetDrams(e);
            Assert.False(TradeSystem.BuyFromTrader(Actor, e, item)); Assert.Contains(item, e.GetPart<InventoryPart>().Objects);
            Assert.AreEqual(buyer, TradeSystem.GetDrams(Actor)); Assert.AreEqual(seller, TradeSystem.GetDrams(e));
        }
    }
}
