using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class DensityHarvestAdversarialTests
    {
        DensityLootTestScope scope; EntityFactory oldFactory; bool oldChannel;
        EntityFactory Factory => scope.Factory;
        [SetUp] public void Setup(){scope=new DensityLootTestScope();oldFactory=HarvestablePart.Factory;HarvestablePart.Factory=Factory;oldChannel=Diag.IsChannelEnabled("loot");Diag.SetChannel("loot",true);}
        [TearDown] public void Cleanup(){HarvestablePart.Factory=oldFactory;Diag.SetChannel("loot",oldChannel);DensityHarvestSecurityTests.HarvestCreationProbe.Callback=null;scope.Dispose();}
        Entity Actor(Zone zone=null,int x=10){var e=Factory.CreateEntity("Player");e.ID=Guid.NewGuid().ToString("N");if(zone!=null)Assert.True(zone.AddEntity(e,x,10));return e;}
        Entity Source(Entity actor=null,Zone zone=null,int x=11)
        {var e=Factory.CreateEntity("CreatureCorpse");e.AddPart(new HarvestablePart{YieldBlueprint="RawMeat"});if(actor!=null)Assert.True(actor.GetPart<InventoryPart>().AddObject(e));if(zone!=null)Assert.True(zone.AddEntity(e,x,10));return e;}
        static bool Act(Entity source,Entity actor,Zone zone=null)=>DensityHarvestSecurityTests.Act(source,actor,zone);
        static int Meat(IEnumerable<Entity> entries)=>entries.Where(e=>e.BlueprintName=="RawMeat").Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
        static int Packed(Entity actor)=>Meat(actor.GetPart<InventoryPart>().Objects);
        static IReadOnlyList<Diag.Entry> Records(Entity actor,string kind)=>DiagQuery.Apply(new DiagQuery.Filter{Category="loot",Kind=kind,Actor=actor.ID,Limit=100}).Records;
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_SavedSpentStateCannotBecomeAnotherHarvest(bool spent)
        {
            var actor=Actor();var source=Source(actor);if(spent)Assert.True(Act(source,actor));else actor.GetPart<InventoryPart>().RemoveObject(source);
            var loaded=PartRoundTripHelper.RoundTripEntity(source);var other=Actor();Assert.True(other.GetPart<InventoryPart>().AddObject(loaded));
            Assert.AreEqual(!spent,Act(loaded,other));Assert.AreEqual(spent?0:1,Packed(other));
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_ReachUsesFootprintAndRemovalClearsEveryOccupiedCell(bool distant)
        {
            var zone=new Zone("footprint");var actor=Actor(zone,distant?14:13);var source=Source();source.AddPart(new SpatialFootprintPart{CellsRaw="0,0;1,0;2,0"});Assert.True(zone.AddEntity(source,10,10));
            var floor=Factory.CreateEntity("Floor");zone.AddEntity(floor,12,10);
            Assert.AreEqual(!distant,Act(source,actor,zone));Assert.AreEqual(distant?0:1,Packed(actor));
            for(int x=10;x<=12;x++)Assert.AreEqual(distant,zone.GetOccupants(x,10).Contains(source));Assert.NotNull(zone.GetEntityCell(floor));
        }
        [TestCase("backref")] [TestCase("equipped")]
        public void Adversarial_ContradictoryOwnershipRefusesWithoutTrustingTheCarriedListAlone(string mode)
        {
            var actor=Actor();var other=Actor();var source=Source(actor);var physics=source.GetPart<PhysicsPart>();
            if(mode=="backref")physics.InInventory=other;else physics.Equipped=other;
            Assert.False(Act(source,actor));Assert.AreEqual(0,Packed(actor));Assert.That(actor.GetPart<InventoryPart>().Objects,Does.Contain(source));
            physics.InInventory=actor;physics.Equipped=null;Assert.True(Act(source,actor));
        }
        [Test] public void Adversarial_NoInventoryLeavesTheEntireHarvestOnTheGround()
        {
            var zone=new Zone("bare");var actor=new Entity();zone.AddEntity(actor,10,10);var source=Source(zone:zone);source.GetPart<HarvestablePart>().YieldMin=source.GetPart<HarvestablePart>().YieldMax=2;
            Assert.True(Act(source,actor,zone));Assert.AreEqual(2,Meat(zone.GetAllEntities()));Assert.IsNull(zone.GetEntityCell(source));
        }
        [TestCase("Player")] [TestCase("Floor")]
        public void Adversarial_CreaturesAndNonPortableTerrainAreNotHarvestProducts(string product)
        {
            var actor=Actor();var source=Source(actor);source.GetPart<HarvestablePart>().YieldBlueprint=product;
            Assert.False(Act(source,actor));Assert.That(actor.GetPart<InventoryPart>().Objects,Does.Contain(source));Assert.AreEqual(1,actor.GetPart<InventoryPart>().Objects.Count);
        }
        [Test] public void Adversarial_FactoryCallbackChangingRecipeRejectsBeforeConsumption()
        {
            var actor=Actor();var source=Source(actor);Factory.RegisterPartType<DensityHarvestSecurityTests.HarvestCreationProbe>();Factory.Blueprints["RawMeat"].Parts["HarvestCreationProbe"]=new Dictionary<string,string>();
            DensityHarvestSecurityTests.HarvestCreationProbe.Callback=()=>source.GetPart<HarvestablePart>().YieldMax=3;
            Assert.False(Act(source,actor));Assert.AreEqual(0,Packed(actor));Assert.That(actor.GetPart<InventoryPart>().Objects,Does.Contain(source));
        }
        [Test] public void Adversarial_HeavierExistingStackCannotMakeOverflowDisappearOrOverfillPack()
        {
            var zone=new Zone("heavy-stack");var actor=Actor(zone);var resident=Factory.CreateEntity("RawMeat");resident.GetPart<PhysicsPart>().Weight=10;resident.GetPart<HandlingPart>().Weight=10;actor.GetPart<InventoryPart>().AddObject(resident);actor.GetPart<InventoryPart>().MaxWeight=12;Assert.AreEqual(10,actor.GetPart<InventoryPart>().GetCarriedWeight(),"The resident must actually weigh ten under Handling authority.");
            var source=Source(zone:zone);Assert.True(Act(source,actor,zone));Assert.AreEqual(1,Packed(actor));Assert.AreEqual(1,Meat(zone.GetAllEntities()));Assert.AreEqual(10,actor.GetPart<InventoryPart>().GetCarriedWeight());
        }
        [Test] public void Adversarial_OuterRollbackRemovesOverflowAndPreservesExistingInventoryStacks()
        {
            var zone=new Zone("overflow-rollback");var actor=Actor(zone);var resident=Factory.CreateEntity("RawMeat");actor.GetPart<InventoryPart>().AddObject(resident);actor.GetPart<InventoryPart>().MaxWeight=2;var source=Source(zone:zone);
            actor.AddPart(new DensityHarvestSecurityTests.ThrowAfterHarvest());
            Assert.False(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(source,"Harvest"),actor,zone).Success);
            Assert.AreEqual(1,Packed(actor));Assert.AreEqual(0,Meat(zone.GetAllEntities()));Assert.NotNull(zone.GetEntityCell(source));Assert.AreEqual(0,Records(actor,"Harvested").Count);
        }
        [Test] public void Adversarial_MaximumLegalYieldIsFiniteAndComplete()
        {var actor=Actor();var source=Source(actor);source.GetPart<HarvestablePart>().YieldMin=source.GetPart<HarvestablePart>().YieldMax=64;Assert.True(Act(source,actor));Assert.AreEqual(64,Packed(actor));}
        [Test] public void Adversarial_SuccessMessageCallbackCannotRepeatCommittedHarvest()
        {
            var actor=Actor();var source=Source(actor);var old=MessageLog.OnMessage;bool? nested=null;
            try{MessageLog.OnMessage=m=>{if(m.StartsWith("You harvest"))nested=Act(source,actor);};Assert.True(Act(source,actor));Assert.AreEqual(false,nested);Assert.AreEqual(1,Packed(actor));}
            finally{MessageLog.OnMessage=old;}
        }
        [TestCase(100,true)] [TestCase(0,true)] [TestCase(100,false)]
        public void Adversarial_SuccessAndRefusalDiagnosticsAreDistinctAndChannelGated(int chance,bool enabled)
        {
            Diag.SetChannel("loot",enabled);var actor=Actor();var source=Source(actor);source.GetPart<HarvestablePart>().YieldChance=chance;Assert.True(Act(source,actor));Assert.False(Act(source,actor));
            Assert.AreEqual(enabled?1:0,Records(actor,"Harvested").Count);Assert.AreEqual(enabled?1:0,Records(actor,"HarvestRejected").Count);
            if(enabled){StringAssert.Contains("\"rollPassed\":"+(chance==100?"true":"false"),Records(actor,"Harvested")[0].PayloadJson);StringAssert.Contains("spent",Records(actor,"HarvestRejected")[0].PayloadJson);}
        }
        [Test] public void Adversarial_GroveConsequenceOccursOnlyAfterTheWholeCommandCommits()
        {
            var rep=PlayerReputation.GetAll();try
            {
                PlayerReputation.Reset();var zone=new Zone("Overworld.0.0.0");var actor=Actor(zone);var source=Factory.CreateEntity("ChoirIronVein");zone.AddEntity(source,11,10);var fail=new DensityHarvestSecurityTests.ThrowAfterHarvest();actor.AddPart(fail);
                Assert.False(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(source,"Harvest"),actor,zone).Success);Assert.AreEqual(0,PlayerReputation.Get("RotChoir"));Assert.NotNull(zone.GetEntityCell(source));
                actor.RemovePart(fail);Assert.True(Act(source,actor,zone));Assert.AreEqual(GroveLaw.DigRepLoss,PlayerReputation.Get("RotChoir"));
            }finally{PlayerReputation.Restore(rep);}
        }
        [Test] public void Adversarial_ZeroSizedYieldIsInvalidEvenWithZeroSuccessChance()
        {var actor=Actor();var source=Source(actor);var p=source.GetPart<HarvestablePart>();p.YieldMin=p.YieldMax=p.YieldChance=0;Assert.False(Act(source,actor));Assert.That(actor.GetPart<InventoryPart>().Objects,Does.Contain(source));}
        [Test] public void Adversarial_CallerOwnedTransactionDefersReceiptAndCanUndoBeforeCommit()
        {
            var actor=Actor();var source=Source(actor);var tx=new InventoryTransaction();var e=GameEvent.New("InventoryAction");
            try{e.SetParameter("Command","Harvest");e.SetParameter("Actor",actor);e.SetParameter("InventoryTransaction",tx);source.FireEvent(e);Assert.True(e.Handled);Assert.AreEqual(1,Packed(actor));Assert.IsEmpty(Records(actor,"Harvested"));tx.Rollback();Assert.AreEqual(0,Packed(actor));Assert.That(actor.GetPart<InventoryPart>().Objects,Does.Contain(source));}
            finally{e.Release();tx.Rollback();}
            Assert.True(Act(source,actor));Assert.AreEqual(1,Records(actor,"Harvested").Count);
        }
    }
}
