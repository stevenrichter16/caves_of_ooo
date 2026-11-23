using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    [TestFixture] public sealed class ProvisionerConsignmentTests : FiftyWorldFixture
    {
        EntityFactory oldTrader, oldRestock;
        static readonly string[] Choices = { "Mushroom", "RawMeat", "EmberwheatSeed", "WaterTonic", "CampHearth", "PoisonGasGrenade" };
        [SetUp] public void Prepare()
        {
            oldTrader = TraderPart.Factory; oldRestock = TraderRestockSystem.Factory;
            TraderPart.Factory = null; TraderRestockSystem.Factory = Factory;
            Pack.MaxWeight = 1000; TradeSystem.SetDrams(Actor, 10000);
        }
        [TearDown] public void Restore()
        {
            TraderPart.Factory = oldTrader; TraderRestockSystem.Factory = oldRestock;
            QuillholdFactoryProbe.Callback = null; QuillholdFactoryProbe.CreatedCallback = null;
        }
        Entity Keeper()
        {
            var e = Place("Provisioner"); e.ID = "provisioner-consignment";
            e.GetPart<InventoryPart>().MaxWeight = 1000; TradeSystem.SetDrams(e, 10000); return e;
        }
        MerchantStockPart Start(Entity e, int tick = 10)
        { Assert.True(MerchantStockService.Reconcile(e, Zone, tick)); return e.GetPart<MerchantStockPart>(); }
        static int Units(Entity e, string bp) => e.GetPart<InventoryPart>().Objects.Where(i => i.BlueprintName == bp).Sum(BarterDeal.Units);
        void Probe(string bp, Action<Entity> callback)
        {
            Factory.RegisterPartType<QuillholdFactoryProbe>();
            Factory.Blueprints[bp].Parts[nameof(QuillholdFactoryProbe)] = new Dictionary<string, string>();
            QuillholdFactoryProbe.CreatedCallback = callback;
        }
        [TestCase("LiquidFlask", 1)] [TestCase("Waterskin", 1)] [TestCase("DriedMeat", 2)]
        [TestCase("Starapple", 1)] [TestCase("Torch", 1)] [TestCase("CandyCarrotSeed", 1)] [TestCase("LampOil", 3)]
        public void EveryPreviouslyGuaranteedLineAndUsefulOilSupplyRemainsAvailable(string bp, int floor)
        { var e = Keeper(); Start(e); Assert.AreEqual(floor, Units(e, bp)); }
        [Test] public void EveryOriginalOptionalGoodAppearsInTheSixDayCycle()
        {
            var e = Keeper(); var s = Start(e); var seen = new HashSet<string>();
            for (int n = 0; n < 6; n++)
            {
                if (n > 0) Assert.True(MerchantStockService.Reconcile(e, Zone, 10 + n * 1200));
                Assert.AreEqual(2, s.Specials.Count); Assert.False(s.BonusApplied);
                foreach (var i in s.Specials) { seen.Add(i.BlueprintName); Assert.True(MerchantStockService.IsSpecial(e, i)); }
            }
            CollectionAssert.AreEquivalent(Choices, seen);
        }
        [Test] public void OilPurchasePaysForThreeRealUnitsAndOnlyDeficitsReturnAfterTheBoundary()
        {
            var e = Keeper(); var s = Start(e); var oil = e.GetPart<InventoryPart>().Objects.Single(i => i.BlueprintName == "LampOil");
            int purse = TradeSystem.GetDrams(Actor); Assert.True(TradeSystem.BuyFromTrader(Actor, e, oil));
            Assert.AreEqual(3, BarterDeal.Units(oil)); Assert.Less(TradeSystem.GetDrams(Actor), purse); Assert.Contains(oil, Pack.Objects);
            MerchantStockService.Reconcile(e, Zone, 310); Assert.AreEqual(0, Units(e, "LampOil"));
            Assert.True(MerchantStockService.Reconcile(e, Zone, 311)); Assert.AreEqual(3, Units(e, "LampOil"));
            Assert.AreEqual(0, s.Generation);
        }
        [Test] public void ExistingOilIsCountedAndMergedWithoutAnAdditiveGrant()
        {
            var e = Keeper(); var oil = Factory.CreateEntity("LampOil"); Assert.True(e.GetPart<InventoryPart>().AddObject(oil));
            Start(e); Assert.AreEqual(3, Units(e, "LampOil"));
            Assert.AreEqual(1, e.GetPart<InventoryPart>().Objects.Count(i => i.BlueprintName == "LampOil"));
            Assert.True(MerchantStockService.Reconcile(e, Zone, 311)); Assert.AreEqual(3, Units(e, "LampOil"));
        }
        [TestCase("oil")] [TestCase("torch")] [TestCase("skin")]
        public void AlteredResidentGoodsSurviveAndDoNotHideAUsableSupplyDeficit(string kind)
        {
            var e = Keeper(); Start(e); string bp = kind == "oil" ? "LampOil" : kind == "torch" ? "Torch" : "Waterskin";
            var old = e.GetPart<InventoryPart>().Objects.Single(i => i.BlueprintName == bp);
            if (kind == "oil") old.GetPart<ReagentPart>().PropertiesRaw = "different:7";
            if (kind == "torch") old.GetPart<FuelPart>().FuelMass = 1;
            if (kind == "skin") old.GetPart<WaterskinPart>().Charges = 1;
            Assert.True(MerchantStockService.Reconcile(e, Zone, 311)); Assert.Contains(old, e.GetPart<InventoryPart>().Objects);
            Assert.AreEqual(kind == "oil" ? 6 : 2, Units(e, bp));
        }
        [TestCase("reagent")] [TestCase("flicker")]
        public void UnknownPayloadSubclassesRefuseFreshPublication(string kind)
        {
            var e = Keeper(); Probe(kind == "reagent" ? "LampOil" : "Torch", item =>
            {
                if (kind == "reagent") { item.RemovePart(item.GetPart<ReagentPart>()); item.AddPart(new UnknownMerchantReagent()); }
                else { item.RemovePart(item.GetPart<LightSourceFlickerPart>()); item.AddPart(new UnknownMerchantFlicker()); }
            });
            Assert.False(MerchantStockService.Reconcile(e, Zone, 10)); Assert.IsNull(e.GetPart<MerchantStockPart>());
        }
        [TestCase("oil")] [TestCase("flicker")] [TestCase("hearth-fuel")] [TestCase("hearth-install")]
        [TestCase("hearth-no-glow")] [TestCase("hearth-no-debit")]
        public void MalformedFreshGoodsCannotEstablishTheSupplyBaseline(string kind)
        {
            var e = Keeper(); string bp = kind == "oil" ? "LampOil" : kind == "flicker" ? "Torch" : "CampHearth";
            bool fired = false;
            Probe(bp, item =>
            {
                fired = true;
                if (kind == "oil") item.GetPart<ReagentPart>().PropertiesRaw = "combustible:999";
                if (kind == "flicker") item.GetPart<LightSourceFlickerPart>().Speed = float.NaN;
                if (kind == "hearth-fuel") item.GetPart<FuelPart>().FuelMass = 10;
                if (kind == "hearth-install") item.GetPart<PackableFixturePart>().SetTick = 10;
                if (kind == "hearth-no-glow") item.RemovePart(item.GetPart<HearthGlowPart>());
                if (kind == "hearth-no-debit") item.GetPart<FuelPart>().BurnRate = 0;
            });
            for (int n = 0; n < 6 && !fired; n++)
            {
                var before = e.GetPart<InventoryPart>().Objects.ToArray();
                bool result = MerchantStockService.Reconcile(e, Zone, 10 + n * 1200);
                if (fired) { Assert.False(result); CollectionAssert.AreEqual(before, e.GetPart<InventoryPart>().Objects); }
                else Assert.True(result);
            }
            Assert.True(fired);
        }
        [TestCase("name")] [TestCase("set-tick")] [TestCase("fuel")] [TestCase("heat")]
        public void ChangedHearthConsignmentsArePreservedDuringRotation(string change)
        {
            var e = Keeper(); var s = Start(e); Entity hearth = null; int tick = 10;
            for (int n = 0; n < 6; n++)
            {
                hearth = s.Specials.FirstOrDefault(i => i.BlueprintName == "CampHearth"); if (hearth != null) break;
                tick += 1200; Assert.True(MerchantStockService.Reconcile(e, Zone, tick));
            }
            Assert.NotNull(hearth);
            if (change == "name") hearth.GetPart<PackableFixturePart>().CampName = "returned kit";
            if (change == "set-tick") hearth.GetPart<PackableFixturePart>().SetTick = 4;
            if (change == "fuel") hearth.GetPart<FuelPart>().FuelMass = 2;
            if (change == "heat") hearth.GetPart<ThermalPart>().Temperature = 99;
            Assert.True(MerchantStockService.Reconcile(e, Zone, tick + 1200)); Assert.Contains(hearth, e.GetPart<InventoryPart>().Objects);
            Assert.False(MerchantStockService.IsSpecial(e, hearth));
        }
        [TestCase("cloud")] [TestCase("turns")]
        public void GrenadeSightPayloadChangesPreventRetirement(string change)
        {
            var e = Keeper(); var s = Start(e); Entity grenade = null; int tick = 10;
            for (int n = 0; n < 6; n++)
            {
                grenade = s.Specials.FirstOrDefault(i => i.BlueprintName == "PoisonGasGrenade"); if (grenade != null) break;
                tick += 1200; Assert.True(MerchantStockService.Reconcile(e, Zone, tick));
            }
            Assert.NotNull(grenade); var gas = grenade.GetPart<GasGrenadePart>();
            if (change == "cloud") gas.SightCloud = "veil-mist"; else gas.SightCloudTurns = 17;
            Assert.True(MerchantStockService.Reconcile(e, Zone, tick + 1200)); Assert.Contains(grenade, e.GetPart<InventoryPart>().Objects);
        }
        [Test] public void BuyingAndResellingAHearthPermanentlyReleasesWithdrawalMembership()
        {
            var e = Keeper(); var s = Start(e); Entity hearth = null; int tick = 10;
            for (int n = 0; n < 6; n++)
            {
                hearth = s.Specials.FirstOrDefault(i => i.BlueprintName == "CampHearth"); if (hearth != null) break;
                tick += 1200; Assert.True(MerchantStockService.Reconcile(e, Zone, tick));
            }
            Assert.NotNull(hearth); Assert.True(TradeSystem.BuyFromTrader(Actor, e, hearth));
            Assert.True(TradeSystem.SellToTrader(Actor, e, hearth)); Assert.False(MerchantStockService.IsSpecial(e, hearth));
            Assert.True(MerchantStockService.Reconcile(e, Zone, tick + 1200)); Assert.Contains(hearth, e.GetPart<InventoryPart>().Objects);
        }
        [Test] public void OrdinaryGroupingIsNotExpandedByNewMerchantPayloads()
        {
            var e = Keeper(); Start(e); var method = typeof(InventoryItemGroups).GetMethod("CaptureIdentity", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(Entity), typeof(InventoryScreenData.ItemDisplay) }, null);
            foreach (var bp in new[] { "LampOil", "Torch", "Starapple", "CampHearth", "PoisonGasGrenade" })
            {
                var item = Factory.CreateEntity(bp); Assert.True(Pack.AddObject(item)); Assert.Contains(item, Pack.Objects);
                Assert.IsNull(method.Invoke(null, new object[] { Actor, new InventoryScreenData.ItemDisplay { Item = item,
                    Name = item.GetDisplayName(), Category = "", State = "", StackCount = 1 } }));
            }
        }
    }
    public sealed class UnknownMerchantReagent : ReagentPart { }
    public sealed class UnknownMerchantFlicker : LightSourceFlickerPart { }
}
