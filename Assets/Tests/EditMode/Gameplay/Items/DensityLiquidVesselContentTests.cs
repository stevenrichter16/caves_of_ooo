using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class DensityLiquidVesselContentTests
    {
        EntityFactory factory, previousFactory;
        System.Random previousRng;
        Dictionary<string,LootTableData> saved;
        bool wasInitialized;
        static readonly FieldInfo Tables=typeof(LootTableRegistry).GetField("_byName",BindingFlags.Static|BindingFlags.NonPublic);
        static readonly FieldInfo Initialized=typeof(LootTableRegistry).GetField("_initialized",BindingFlags.Static|BindingFlags.NonPublic);
        [SetUp]public void SetUp()
        {
            factory=new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
            saved=new Dictionary<string,LootTableData>((Dictionary<string,LootTableData>)Tables.GetValue(null));
            wasInitialized=(bool)Initialized.GetValue(null);
            LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Data/Loot/LootTables.json")));
            previousFactory=TraderPart.Factory;previousRng=TraderPart.Rng;
            TraderPart.Factory=factory;TraderPart.Rng=new System.Random(64);
        }
        [TearDown]public void TearDown()
        {
            TraderPart.Factory=previousFactory;TraderPart.Rng=previousRng;
            var tables=(Dictionary<string,LootTableData>)Tables.GetValue(null);tables.Clear();
            foreach(var entry in saved)tables.Add(entry.Key,entry.Value);
            Initialized.SetValue(null,wasInitialized);
        }
        [Test]public void ActualFlaskIsEmptyReusableTradeableAndNotAnAlchemyOrDrinkItem()
        {
            var item=factory.CreateEntity("LiquidFlask");Assert.NotNull(item);
            Assert.True(item.HasTag("Item"));Assert.AreEqual("1",item.GetTag("Tier"));
            Assert.True(item.GetPart<PhysicsPart>().Takeable);Assert.AreEqual(1,item.GetPart<PhysicsPart>().Weight);
            var p=item.GetPart<LiquidVesselPart>();Assert.NotNull(p);Assert.AreEqual(12,p.Capacity);Assert.AreEqual(0,p.Volume);Assert.AreEqual("",p.LiquidId);
            Assert.AreEqual(8,item.GetPart<CommercePart>().Value);
            foreach(string part in new[]{"Stacker","Tonic","Waterskin","Reagent","BrewItem","TinkerItem"})Assert.False(item.HasPart(part),part);
            StringAssert.Contains("tight stopper",item.GetPart<ExaminablePart>().Text);
        }
        [Test]public void ActualPuddleTemplateIsEmptyNonportableAndNonrenewing()
        {
            var puddle=factory.CreateEntity(LiquidVesselService.PoolBlueprint);Assert.NotNull(puddle);
            Assert.True(puddle.HasTag("Terrain"));Assert.False(puddle.HasTag("Item"));Assert.False(puddle.HasTag("Creature"));
            Assert.False(puddle.GetPart<PhysicsPart>().Takeable);Assert.False(puddle.GetPart<PhysicsPart>().Solid);
            Assert.AreEqual(0,puddle.GetPart<LiquidPoolPart>().Volume);Assert.AreEqual("",puddle.GetPart<LiquidPoolPart>().LiquidId);
            Assert.False(puddle.HasPart<TileStateSourcePart>());Assert.False(puddle.HasPart<ContainerPart>());
        }
        [TestCase("MorrowfastProvisionerStock")][TestCase("ProvisionerStock")][TestCase("WellKeeperStock")]
        public void EachActualSupplyTableGuaranteesExactlyOneEmptyFlaskWithoutLosingTheWaterSkin(string table)
        {
            Assert.False(LootTableRegistry.Get(table).PickOne);
            for(int seed=0;seed<64;seed++)
            {var rows=LootTableRegistry.Roll(table,new System.Random(seed));Assert.AreEqual(1,rows.Count(x=>x=="LiquidFlask"),seed.ToString());Assert.AreEqual(1,rows.Count(x=>x=="Waterskin"));}
        }
        [TestCase("Provisioner")][TestCase("WellKeeper")]
        public void ActualFactoryMerchantOwnsTheFlaskAndCanSellIt(string blueprint)
        {AssertPurchasable(factory.CreateEntity(blueprint));}
        [TestCase(false)][TestCase(true)]
        public void AuthoredStartingMerchantGetsExactlyOneFlaskWithOrWithoutGlobalTraderFactory(bool wired)
        {TraderPart.Factory=wired?factory:null;AssertPurchasable(MorrowfastContent.CreateResident("southern-food-vendor",factory));}
        [Test]public void NaturalChoirAndCraftRentalTablesDoNotGainManufacturedFlasks()
        {
            var rows=(Dictionary<string,LootTableData>)Tables.GetValue(null);
            var sources=rows.Where(x=>x.Value.Entries.Any(e=>e.Blueprint=="LiquidFlask")).Select(x=>x.Key).ToArray();
            CollectionAssert.AreEquivalent(new[]{"MorrowfastProvisionerStock","ProvisionerStock","WellKeeperStock"},sources);
        }
        [Test]public void ActualLootRegistryStillValidatesAllReferencedBlueprints()
        {Assert.IsEmpty(LootTableRegistry.Validate(factory.Blueprints.ContainsKey));}
        void AssertPurchasable(Entity merchant)
        {
            Assert.NotNull(merchant);var matches=merchant.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName=="LiquidFlask").ToArray();
            Assert.AreEqual(1,matches.Length);var flask=matches[0];Assert.AreSame(merchant,flask.GetPart<PhysicsPart>().InInventory);
            var buyer=factory.CreateEntity("Player");TradeSystem.SetDrams(buyer,100);int before=TradeSystem.GetDrams(buyer);
            Assert.True(TradeSystem.BuyFromTrader(buyer,merchant,flask));Assert.AreSame(buyer,flask.GetPart<PhysicsPart>().InInventory);
            Assert.True(buyer.GetPart<InventoryPart>().Objects.Contains(flask));Assert.False(merchant.GetPart<InventoryPart>().Objects.Contains(flask));
            Assert.Less(TradeSystem.GetDrams(buyer),before);Assert.AreEqual(0,flask.GetPart<LiquidVesselPart>().Volume);
        }
    }
}
