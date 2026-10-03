using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Geographic authority, malformed supply packets and literal finite ownership.</summary>
    public sealed class EquipmentDiscoverySourceAdversarialTests
    {
        [TestCase("sodden","PeatMalletHeadComponent")][TestCase("sodden","GroundwireScreen")]
        [TestCase("sodden","OakHaftComponent")][TestCase("sodden","LeatherBindingComponent")]
        [TestCase("cinderhold","CinderhookAxeHeadComponent")][TestCase("cinderhold","KilnfeltApron")]
        [TestCase("counter","CounterweightLongBladeComponent")]
        public void Adversarial_MissingRegionalDependencyRejectsBeforePublishingNewTerrain(string source,string id)
        {
            using(var scope=new HaulingContentScope())
            {
                scope.Seed(64);scope.Factory.Blueprints.Remove(id);var zone=new Zone(EquipmentDiscoverySourceTests.Address(source));
                IZoneBuilder builder=source=="sodden"?(IZoneBuilder)new SoddenDistrictBuilder(64):source=="cinderhold"?new CinderholdCompositionBuilder(64):new LastCounterCompositionBuilder(64);
                Assert.IsFalse(builder.BuildZone(zone,scope.Factory,new System.Random(64)),"A missing regional source must not publish an incomplete destination.");
                Assert.Zero(zone.EntityCount);Assert.IsEmpty(zone.GenReservedCells);
            }
        }
        [TestCase("sodden","PeatMalletHeadComponent","WeaponComponent")][TestCase("sodden","GroundwireScreen","Armor")]
        [TestCase("cinderhold","CinderhookAxeHeadComponent","WeaponComponent")][TestCase("cinderhold","KilnfeltApron","Armor")]
        [TestCase("counter","CounterweightLongBladeComponent","WeaponComponent")]
        public void Adversarial_MalformedRegionalEquipmentDoesNotPublishAsAUsefulSource(string source,string id,string part)
        {
            using(var scope=new HaulingContentScope())
            {
                scope.Seed(64);Assert.IsTrue(scope.Factory.Blueprints.ContainsKey(id));scope.Factory.Blueprints[id].Parts.Remove(part);
                var zone=new Zone(EquipmentDiscoverySourceTests.Address(source));
                IZoneBuilder builder=source=="sodden"?(IZoneBuilder)new SoddenDistrictBuilder(64):source=="cinderhold"?new CinderholdCompositionBuilder(64):new LastCounterCompositionBuilder(64);
                Assert.IsFalse(builder.BuildZone(zone,scope.Factory,new System.Random(64)));Assert.Zero(zone.EntityCount);
            }
        }
        [TestCase("sodden")][TestCase("cinderhold")][TestCase("counter")]
        public void Adversarial_ReplayingProfileNeverRestocksAnEmptiedDiscovery(string source)
        {
            using(var scope=new HaulingContentScope())
            {
                scope.Seed(64);var owner=EquipmentDiscoverySourceTests.Build(source,scope.Factory,64,out var zone,out var replay);
                Assert.IsTrue(EquipmentDiscoverySourceTests.Items(owner).Any(e=>e.BlueprintName==EquipmentDiscoverySourceTests.Kit(source)[0]));
                foreach(var item in EquipmentDiscoverySourceTests.Items(owner))
                    if(owner.HasPart<ContainerPart>())owner.GetPart<ContainerPart>().RemoveItem(item);else owner.GetPart<InventoryPart>().RemoveObject(item);
                var before=zone.GetAllEntities().ToArray();Assert.IsFalse(replay());Assert.IsEmpty(EquipmentDiscoverySourceTests.Items(owner));CollectionAssert.AreEquivalent(before,zone.GetAllEntities());
            }
        }
        [TestCase("sodden")][TestCase("cinderhold")][TestCase("counter")]
        public void Adversarial_RegionalSupplyNeverLeaksThroughChangedActualMapAuthority(string source)
        {
            using(var scope=new HaulingContentScope())
            {
                scope.Seed(64);var manager=OverworldZoneManager.CreateDetached(scope.Factory,64,true);string id=EquipmentDiscoverySourceTests.Address(source);var at=WorldMap.FromZoneID(id);
                if(source=="sodden")SoddenDistrictIntegrationTests.Version(manager,13);
                else manager.WorldMap.SetPOI(at.x,at.y,new PointOfInterest(POIType.Village,"ordinary control",tier:3));
                var zone=manager.GetZone(id);Assert.NotNull(zone);
                var found=zone.GetAllEntities().SelectMany(e=>e.HasPart<InventoryPart>()||e.HasPart<ContainerPart>()?EquipmentDiscoverySourceTests.Items(e):Array.Empty<Entity>()).Select(e=>e.BlueprintName);
                Assert.IsFalse(found.Intersect(EquipmentDiscoverySourceTests.Special).Any());
            }
        }
        [Test] public void Adversarial_SpecialtiesStayOutOfGlobalLootAndRenewableSmithTables()
        {
            string loot=File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Data/Loot/LootTables.json"));
            foreach(string id in EquipmentDiscoverySourceTests.Special)StringAssert.DoesNotContain('"'+id+'"',loot,"Regional first stocks are finite; broad pools must stay unchanged.");
        }
        [Test] public void Adversarial_CinderholdNormalRestockDoesNotRegenerateTheOneTimeRegionalKit()
        {
            using(var scope=new HaulingContentScope())
            {
                scope.Seed(64);var owner=EquipmentDiscoverySourceTests.Build("cinderhold",scope.Factory,64,out var zone,out _);
                Assert.IsTrue(EquipmentDiscoverySourceTests.Items(owner).Any(e=>e.BlueprintName=="CinderhookAxeHeadComponent"));
                Assert.AreEqual("WeaponsmithStock",owner.GetPart<TraderPart>().StockTable);Assert.AreEqual("WeaponsmithStock",owner.GetProperty(TraderRestockSystem.ShopStockTableProp));
                var shelf=owner.GetPart<InventoryPart>();foreach(var item in shelf.Objects.ToArray())shelf.RemoveObject(item);
                // The density scope wires opening trade stock, but intentionally
                // does not wire the separate periodic-restock factory.
                var oldFactory=TraderRestockSystem.Factory;
                try
                {
                    TraderRestockSystem.Factory=scope.Factory;
                    TraderRestockSystem.RestockZone(zone,TraderRestockSystem.RestockIntervalTurns+1);Assert.IsNotEmpty(shelf.Objects);
                    Assert.IsFalse(shelf.Objects.Any(e=>EquipmentDiscoverySourceTests.Special.Contains(e.BlueprintName)));
                }
                finally { TraderRestockSystem.Factory=oldFactory; }
            }
        }
        [Test] public void Adversarial_SoddenFinalReceiptNoticesAChangedNewComponent()
        {
            using(var scope=new HaulingContentScope())
            {
                scope.Seed(64);var zone=new Zone(SoddenDistrictPlan.WorksZoneID);var b=new SoddenDistrictBuilder(64);Assert.IsTrue(b.BuildZone(zone,scope.Factory,new System.Random(64)));Assert.IsTrue(b.ValidateFinal(zone));
                var locker=zone.GetAllEntities().Single(e=>e.BlueprintName=="SoddenWorksLocker");var item=EquipmentDiscoverySourceTests.Items(locker).SingleOrDefault(e=>e.BlueprintName=="PeatMalletHeadComponent");Assert.NotNull(item);
                item.GetPart<WeaponComponentPart>().Attributes="Cutting";Assert.IsFalse(b.ValidateFinal(zone));
            }
        }
    }
}
