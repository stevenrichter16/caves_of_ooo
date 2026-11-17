using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class MachineLeatherSupplyTests : FiftyWorldFixture
    {
        const string Input="CuredLeatherOffcut";
        EntityFactory oldFactory;System.Random oldRng;
        Dictionary<object,object> oldTables;bool oldInitialized;
        static FieldInfo Tables=>typeof(LootTableRegistry).GetField("_byName",BindingFlags.NonPublic|BindingFlags.Static);
        static FieldInfo Initialized=>typeof(LootTableRegistry).GetField("_initialized",BindingFlags.NonPublic|BindingFlags.Static);
        [SetUp] public void SetupSupply()
        {
            oldFactory=TraderPart.Factory;oldRng=TraderPart.Rng;TraderPart.Factory=Factory;
            oldTables=new Dictionary<object,object>();foreach(DictionaryEntry pair in (IDictionary)Tables.GetValue(null))oldTables[pair.Key]=pair.Value;
            oldInitialized=(bool)Initialized.GetValue(null);
            LootTableRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/Loot").Select(t=>t.text));
        }
        [TearDown] public void RestoreSupply()
        {
            TraderPart.Factory=oldFactory;TraderPart.Rng=oldRng;var tables=(IDictionary)Tables.GetValue(null);tables.Clear();foreach(var p in oldTables)tables.Add(p.Key,p.Value);Initialized.SetValue(null,oldInitialized);
        }
        sealed class Rolls : System.Random
        {
            public int Count;public Rolls():base(77){}
            public override int Next(int max){Count++;return base.Next(max);}
            public override int Next(int min,int max){Count++;return base.Next(min,max);}
            public override double NextDouble(){Count++;return base.NextDouble();}
        }
        static int Units(Entity trader)=>trader.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName==Input).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
        static string Ordinary(Entity trader)=>string.Join(",",trader.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName!=Input).Select(e=>e.BlueprintName+":"+(e.GetPart<StackerPart>()?.StackCount??1)).OrderBy(x=>x));
        [Test] public void OpeningPacketPreservesTheExistingShelfPurseAndRandomSequence()
        {
            Assert.True(Factory.Blueprints.ContainsKey(Input),"New explicit feedstock must exist.");
            var parameters=Factory.Blueprints["Weaponsmith"].Parts["Trader"];var original=new Dictionary<string,string>(parameters);
            parameters.Remove("OpeningStockBlueprint");parameters.Remove("OpeningStockUnits");var baselineRolls=new Rolls();TraderPart.Rng=baselineRolls;var baseline=Factory.CreateEntity("Weaponsmith");
            parameters.Clear();foreach(var pair in original)parameters[pair.Key]=pair.Value;
            var packetRolls=new Rolls();TraderPart.Rng=packetRolls;var trader=Factory.CreateEntity("Weaponsmith");
            Assert.IsNotEmpty(Ordinary(baseline));Assert.AreEqual(Ordinary(baseline),Ordinary(trader));Assert.AreEqual(TradeSystem.GetDrams(baseline),TradeSystem.GetDrams(trader));Assert.AreEqual(baselineRolls.Count,packetRolls.Count);Assert.AreEqual(2,Units(trader));
        }
        [Test] public void PurchaseRemovalRepeatedCreationAndReloadNeverReplenishThePacket()
        {
            TraderPart.Rng=new Rolls();var trader=Factory.CreateEntity("Weaponsmith");Assert.AreEqual(2,Units(trader));var pack=trader.GetPart<InventoryPart>();
            foreach(var item in pack.Objects.Where(e=>e.BlueprintName==Input).ToArray())pack.RemoveObject(item);
            for(int i=0;i<3;i++)trader.FireEventAndRelease(GameEvent.New("ObjectCreated"));Assert.AreEqual(0,Units(trader));
            var saved=PartRoundTripHelper.RoundTripEntityViaTokenGraph(trader);Assert.AreEqual(0,Units(saved));
            saved.FireEventAndRelease(GameEvent.New("ObjectCreated"));Assert.AreEqual(0,Units(saved));
        }
        [Test] public void OldSavedTraderWithoutOpeningFieldsIsNotRetrofitted()
        {
            var parameters=Factory.Blueprints["Weaponsmith"].Parts["Trader"];parameters.Remove("OpeningStockBlueprint");parameters.Remove("OpeningStockUnits");TraderPart.Rng=new Rolls();var old=Factory.CreateEntity("Weaponsmith");
            parameters["OpeningStockBlueprint"]=Input;parameters["OpeningStockUnits"]="2";
            var saved=PartRoundTripHelper.RoundTripEntityViaTokenGraph(old);Assert.AreEqual(0,Units(saved));
        }
        [Test] public void MissingOptionalFeedPreservesTheOrdinaryTrader()
        {
            Factory.Blueprints.Remove(Input);TraderPart.Rng=new Rolls();var trader=Factory.CreateEntity("Weaponsmith");Assert.NotNull(trader);Assert.IsNotEmpty(Ordinary(trader));Assert.Greater(TradeSystem.GetDrams(trader),0);Assert.AreEqual(0,Units(trader));
        }
        [TestCase(0)] [TestCase(4)]
        public void InsufficientRoomOmitsTheWholeOpeningPacketWithoutOverfillingOrLosingOldStock(int room)
        {
            int priorDrams=TradeSystem.GetDrams(Actor);var old=Carry("DriedMeat");Pack.MaxWeight=Pack.GetCarriedWeight()+room;var trader=new TraderPart{Drams=90};
            typeof(TraderPart).GetField("OpeningStockBlueprint").SetValue(trader,Input);typeof(TraderPart).GetField("OpeningStockUnits").SetValue(trader,2);Actor.AddPart(trader);
            Actor.FireEventAndRelease(GameEvent.New("ObjectCreated"));Assert.AreEqual(0,Units(Actor));Assert.True(Pack.Objects.Contains(old));Assert.LessOrEqual(Pack.GetCarriedWeight(),Pack.MaxWeight);Assert.AreEqual(priorDrams,TradeSystem.GetDrams(Actor));
        }
        [Test] public void TheOptionalTinkerDoesNotReceiveTheSmithsFiniteOpeningPacket()
        {TraderPart.Rng=new Rolls();Assert.AreEqual(0,Units(Factory.CreateEntity("Tinker")));}
        [Test] public void OrdinaryRestockDoesNotReplenishSpentOffcuts()
        {
            TraderPart.Rng=new Rolls();var smith=Factory.CreateEntity("Weaponsmith");var pack=smith.GetPart<InventoryPart>();
            foreach(var item in pack.Objects.ToArray())pack.RemoveObject(item);Assert.True(Zone.AddEntity(smith,12,10));
            var previous=TraderRestockSystem.Factory;try{TraderRestockSystem.Factory=Factory;TraderRestockSystem.RestockZone(Zone,10000);Assert.AreEqual(0,Units(smith));}
            finally{TraderRestockSystem.Factory=previous;}
        }
        [Test] public void FreshCinderholdCompositionRetainsRegionalShelfAndTheOptionalPacket()
        {
            using(var scope=new DensityLootTestScope())
            {
                TraderPart.Factory=scope.Factory;
                var manager=OverworldZoneManager.CreateDetached(scope.Factory,704860946,true);var zone=manager.GetZone(CinderholdCompositionPlan.ZoneID);
                var smith=zone.GetReadOnlyEntities().Single(e=>e.BlueprintName=="Weaponsmith");var shelf=smith.GetPart<InventoryPart>().Objects;
                Assert.AreEqual(2,Units(smith));Assert.True(shelf.Any(e=>e.BlueprintName=="CinderhookAxeHeadComponent"));Assert.True(shelf.Any(e=>e.BlueprintName=="KilnfeltApron"));
                Assert.True(shelf.Any(e=>e.BlueprintName=="LeatherBindingComponent"));Assert.Greater(TradeSystem.GetDrams(smith),0);
                Assert.AreEqual(1,zone.GetReadOnlyEntities().Count(e=>e.BlueprintName=="TinkersForge"));
            }
        }
    }
}
