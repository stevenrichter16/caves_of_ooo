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
        [TestCase("Starapple", 1)] [TestCase("Torch", 1)] [TestCase("CandyCarrotSeed", 1)] [TestCase("LampOil", 3)] [TestCase("FieldSatchel", 1)]
        public void EveryPreviouslyGuaranteedLineAndUsefulOilSupplyRemainsAvailable(string bp, int floor)
        { var e = Keeper(); Start(e); Assert.AreEqual(floor, Units(e, bp)); }
        [Test]
        public void CleanSatchelIsOneRealEmptyShellAndRetainsItsIdentityAcrossRestockAndSave()
        {
            var e = Keeper(); Start(e);
            var satchel = e.GetPart<InventoryPart>().Objects.Single(i => i.BlueprintName == "FieldSatchel");
            string id = satchel.ID;
            Assert.AreSame(e, satchel.GetPart<PhysicsPart>().InInventory);
            Assert.IsTrue(CarriedStorage.IsCarried(e, satchel));
            Assert.IsEmpty(satchel.GetPart<ContainerPart>().Contents);
            Assert.AreEqual(6, satchel.GetPart<ContainerPart>().MaxItems);
            Assert.AreEqual("in", satchel.GetPart<ContainerPart>().Preposition);
            Assert.AreEqual(1, satchel.GetPart<PhysicsPart>().Weight);
            Assert.AreEqual(8, satchel.GetPart<CommercePart>().Value);
            Assert.IsNull(satchel.GetPart<StackerPart>());
            Assert.IsNull(typeof(InventoryItemGroups).GetMethod("CaptureIdentity", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(Entity), typeof(InventoryScreenData.ItemDisplay) }, null).Invoke(null,
                new object[] { e, new InventoryScreenData.ItemDisplay { Item = satchel, Name = satchel.GetDisplayName(), StackCount = 1 } }),
                "Merchant identity must not grant ordinary grouping permission.");
            Assert.True(MerchantStockService.Reconcile(e, Zone, 311));
            Assert.AreSame(satchel, e.GetPart<InventoryPart>().Objects.Single(i => i.BlueprintName == "FieldSatchel"));
            Entity copy;
            using (var stream = new MemoryStream())
            {
                var writer = new SaveWriter(stream); writer.WriteEntityReference(e); writer.WriteQueuedEntityBodies();
                stream.Position = 0; var reader = new SaveReader(stream, Factory); copy = reader.ReadEntityReference(); reader.ReadEntityBodies();
            }
            Assert.True(Zone.RemoveEntity(e)); Assert.True(Zone.AddEntity(copy, 11, 10));
            Assert.True(MerchantStockService.Reconcile(copy, Zone, 612));
            var saved = copy.GetPart<InventoryPart>().Objects.Single(i => i.BlueprintName == "FieldSatchel");
            Assert.AreEqual(id, saved.ID); Assert.IsTrue(CarriedStorage.IsCarried(copy, saved));
            Assert.IsEmpty(saved.GetPart<ContainerPart>().Contents); Assert.AreEqual(3, Units(copy, "LampOil"));
        }

        [TestCase("capacity")] [TestCase("preposition")] [TestCase("locked")] [TestCase("filled")]
        [TestCase("null-contents")] [TestCase("null-entry")] [TestCase("nested")]
        [TestCase("foreign-container")] [TestCase("duplicate-container")] [TestCase("container-subclass")]
        [TestCase("missing-load")] [TestCase("foreign-load")] [TestCase("weight")] [TestCase("value")] [TestCase("label")]
        public void MalformedFreshSatchelRefusesTheWholeLotWithoutSpendingExistingShelfGoods(string fault)
        {
            var e = Keeper(); var pack = e.GetPart<InventoryPart>(); var old = Factory.CreateEntity("LampOil");
            Assert.True(pack.AddObject(old)); var before = pack.Objects.ToArray(); int purse = TradeSystem.GetDrams(e);
            Entity made = null, child = null;
            Probe("FieldSatchel", item =>
            {
                made = item; var container = item.GetPart<ContainerPart>();
                switch (fault)
                {
                    case "capacity": container.MaxItems = 7; break;
                    case "preposition": container.Preposition = "on"; break;
                    case "locked": container.Locked = true; break;
                    case "filled": case "nested":
                        child = Factory.CreateEntity(fault == "nested" ? "Sack" : "Starapple"); Assert.True(container.AddItem(child)); break;
                    case "null-contents": container.Contents = null; break;
                    case "null-entry": container.Contents.Add(null); break;
                    case "foreign-container": container.ParentEntity = Actor; break;
                    case "duplicate-container": item.Parts.Add(new ContainerPart { ParentEntity = item }); break;
                    case "container-subclass": item.RemovePart(container); item.AddPart(new UnknownMerchantContainer { MaxItems = 6 }); break;
                    case "missing-load": item.RemovePart(item.GetPart<ContainerLoadPart>()); break;
                    case "foreign-load": item.GetPart<ContainerLoadPart>().ParentEntity = Actor; break;
                    case "weight": item.GetPart<PhysicsPart>().Weight = 2; break;
                    case "value": item.GetPart<CommercePart>().Value = 99; break;
                    case "label": item.AddPart(new StorageLabelPart { Mode = (int)StorageLabelMode.Text, Text = "reserved" }); break;
                }
            });
            Assert.False(MerchantStockService.Reconcile(e, Zone, 10)); Assert.NotNull(made);
            Assert.IsNull(e.GetPart<MerchantStockPart>()); CollectionAssert.AreEqual(before, pack.Objects);
            Assert.AreSame(e, old.GetPart<PhysicsPart>().InInventory); Assert.AreEqual(1, BarterDeal.Units(old));
            Assert.AreEqual(purse, TradeSystem.GetDrams(e)); Assert.False(pack.Objects.Contains(made));
            if (child != null) Assert.AreSame(made, child.GetPart<PhysicsPart>().InInventory);
        }

        [TestCase("filled")] [TestCase("capacity")] [TestCase("locked")] [TestCase("label")]
        public void ChangedResidentSatchelSurvivesAndDoesNotHideTheCleanShellDeficit(string change)
        {
            var e = Keeper(); Start(e); var pack = e.GetPart<InventoryPart>();
            var old = pack.Objects.Single(i => i.BlueprintName == "FieldSatchel"); var before = pack.Objects.ToArray();
            Entity child = null;
            if (change == "filled") { child = Factory.CreateEntity("Starapple"); Assert.True(old.GetPart<ContainerPart>().AddItem(child)); }
            if (change == "capacity") old.GetPart<ContainerPart>().MaxItems = 9;
            if (change == "locked") old.GetPart<ContainerPart>().Locked = true;
            if (change == "label") old.AddPart(new StorageLabelPart { Mode = (int)StorageLabelMode.Text, Text = "my roots" });
            Assert.True(MerchantStockService.Reconcile(e, Zone, 311));
            CollectionAssert.IsSubsetOf(before, pack.Objects); Assert.AreEqual(before.Length + 1, pack.Objects.Count);
            Assert.AreSame(e, old.GetPart<PhysicsPart>().InInventory);
            var fresh = pack.Objects.Single(i => !before.Contains(i)); Assert.AreEqual("FieldSatchel", fresh.BlueprintName);
            Assert.IsEmpty(fresh.GetPart<ContainerPart>().Contents); Assert.AreEqual(6, fresh.GetPart<ContainerPart>().MaxItems);
            Assert.False(fresh.GetPart<ContainerPart>().IsLocked); Assert.IsNull(fresh.GetPart<StorageLabelPart>());
            if (child != null) { Assert.AreSame(child, old.GetPart<ContainerPart>().Contents.Single()); Assert.AreSame(old, child.GetPart<PhysicsPart>().InInventory); }
            if (change == "capacity") Assert.AreEqual(9, old.GetPart<ContainerPart>().MaxItems);
            if (change == "locked") Assert.True(old.GetPart<ContainerPart>().Locked);
            if (change == "label") Assert.AreEqual("my roots", old.GetPart<StorageLabelPart>().Text);
        }

        [Test]
        public void SatchelFilledAfterItsFactoryCheckCannotPublishTheStagedLot()
        {
            var e = Keeper(); var pack = e.GetPart<InventoryPart>(); var old = Factory.CreateEntity("Dagger");
            Assert.True(pack.AddObject(old)); Entity satchel = null, child = null;
            Factory.RegisterPartType<QuillholdFactoryProbe>();
            foreach (string bp in Choices.Append("FieldSatchel")) Factory.Blueprints[bp].Parts[nameof(QuillholdFactoryProbe)] = new Dictionary<string, string>();
            QuillholdFactoryProbe.CreatedCallback = item =>
            {
                if (item.BlueprintName == "FieldSatchel") { satchel = item; return; }
                if (satchel == null || child != null) return;
                child = Factory.CreateEntity("Starapple"); Assert.True(satchel.GetPart<ContainerPart>().AddItem(child));
                old.GetPart<MeleeWeaponPart>().HitBonus = 7;
            };
            Assert.False(MerchantStockService.Reconcile(e, Zone, 10)); Assert.NotNull(child);
            CollectionAssert.AreEqual(new[] { old }, pack.Objects); Assert.IsNull(e.GetPart<MerchantStockPart>());
            Assert.AreEqual(7, old.GetPart<MeleeWeaponPart>().HitBonus);
            Assert.AreSame(child, satchel.GetPart<ContainerPart>().Contents.Single()); Assert.AreSame(satchel, child.GetPart<PhysicsPart>().InInventory);
        }
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
    public sealed class UnknownMerchantContainer : ContainerPart { }
}
