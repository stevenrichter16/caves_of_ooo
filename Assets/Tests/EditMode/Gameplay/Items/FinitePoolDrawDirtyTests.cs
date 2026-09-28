using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class FinitePoolDrawDirtyTests
    {
        Zone zone; Entity actor, vessel, source; LiquidPoolPart pool;
        readonly List<(int x,int y,string reason)> dirty = new List<(int,int,string)>();
        Action<int,int,string> priorDirty; Action<string> priorFull, priorMessage; Func<int> priorTick;
        Dictionary<string,LiquidDefinition> priorLiquids; bool priorInitialized;
        readonly Dictionary<FieldInfo,object[]> priorLists = new Dictionary<FieldInfo,object[]>();
        int priorSerial;
        static FieldInfo Field(Type t,string n)=>t.GetField(n,BindingFlags.Static|BindingFlags.NonPublic);
        [SetUp] public void SetUp()
        {
            priorDirty=ZoneRenderHooks.CellDirtyCallback; priorFull=ZoneRenderHooks.FullDirtyCallback;
            priorMessage=MessageLog.OnMessage; priorTick=MessageLog.TickProvider;
            MessageLog.OnMessage=null; MessageLog.TickProvider=null;
            foreach(var name in new[]{"Messages","Ticks","Serials"})
            {var f=Field(typeof(MessageLog),name); priorLists[f]=((IEnumerable)f.GetValue(null)).Cast<object>().ToArray();}
            priorSerial=(int)Field(typeof(MessageLog),"NextSerial").GetValue(null);
            var registry=(Dictionary<string,LiquidDefinition>)Field(typeof(LiquidRegistry),"_byId").GetValue(null);
            priorLiquids=new Dictionary<string,LiquidDefinition>(registry); priorInitialized=LiquidRegistry.IsInitialized;
            LiquidRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,"Resources/Content/Data/LiquidDefinitions"),"*.json").Select(File.ReadAllText));
            zone=new Zone("finite-pool-dirty"); actor=Owner("actor"); actor.AddPart(new InventoryPart());
            Assert.True(zone.AddEntity(actor,10,10));
            source=Owner("SpreadDrawPoint"); pool=new LiquidPoolPart{LiquidId="water",Volume=3};source.AddPart(pool);
            Assert.True(zone.AddEntity(source,11,10));
            ZoneRenderHooks.CellDirtyCallback=(x,y,r)=>dirty.Add((x,y,r)); ZoneRenderHooks.FullDirtyCallback=r=>Assert.Fail("Finite source requested full redraw: "+r);
            dirty.Clear();
        }
        [TearDown] public void TearDown()
        {
            ZoneRenderHooks.CellDirtyCallback=priorDirty; ZoneRenderHooks.FullDirtyCallback=priorFull;
            MessageLog.OnMessage=priorMessage; MessageLog.TickProvider=priorTick;
            foreach(var p in priorLists){var list=(IList)p.Key.GetValue(null);list.Clear();foreach(var value in p.Value)list.Add(value);}priorLists.Clear();
            Field(typeof(MessageLog),"NextSerial").SetValue(null,priorSerial);
            if(priorLiquids!=null){var d=(Dictionary<string,LiquidDefinition>)Field(typeof(LiquidRegistry),"_byId").GetValue(null);d.Clear();foreach(var p in priorLiquids)d.Add(p.Key,p.Value);Field(typeof(LiquidRegistry),"_initialized").SetValue(null,priorInitialized);}
        }
        static Entity Owner(string blueprint)
        {var e=new Entity{ID=Guid.NewGuid().ToString("N"),BlueprintName=blueprint};e.AddPart(new PhysicsPart());e.AddPart(new RenderPart());return e;}
        void Vessel(bool skin)
        {
            vessel=Owner(skin?"Waterskin":"EmptyFlask");vessel.SetTag("Item");vessel.GetPart<PhysicsPart>().Takeable=true;
            if(skin)vessel.AddPart(new WaterskinPart{Capacity=3,Charges=0});else vessel.AddPart(new LiquidVesselPart{Capacity=3,Volume=0,LiquidId=""});
            Assert.True(actor.GetPart<InventoryPart>().AddObject(vessel));Assert.AreSame(actor,vessel.GetPart<PhysicsPart>().InInventory);dirty.Clear();
        }
        bool Fill(bool skin,InventoryTransaction tx=null)=>skin?WaterVesselService.TryAct(actor,vessel,zone,"FillWaterskin",tx)
            :LiquidVesselService.TryAct(actor,vessel,zone,"FillLiquidVessel|"+Uri.EscapeDataString(source.ID)+"|water",tx);
        int Carried(bool skin)=>skin?vessel.GetPart<WaterskinPart>().Charges:vessel.GetPart<LiquidVesselPart>().Volume;
        void OneSourceDirty(){Assert.AreEqual(1,dirty.Count);Assert.AreEqual((11,10),(dirty[0].x,dirty[0].y));}
        [TestCase(false,3)][TestCase(true,3)][TestCase(false,7)][TestCase(true,7)]
        public void CommittedFiniteDrawInvalidatesExactCellAndPreservesPersistentOwner(bool skin,int amount)
        {Vessel(skin);pool.Volume=amount;Assert.True(Fill(skin));Assert.AreEqual(3,Carried(skin));Assert.AreEqual(amount-3,pool.Volume);Assert.AreSame(source,zone.GetReadOnlyEntities().Single(e=>e.ID==source.ID));Assert.AreSame(pool,source.GetPart<LiquidPoolPart>());OneSourceDirty();}
        [TestCase(false)][TestCase(true)] public void OuterCommitIsOnlyPublicationBoundary(bool skin)
        {Vessel(skin);var tx=new InventoryTransaction();try{Assert.True(Fill(skin,tx));Assert.Zero(pool.Volume);Assert.IsEmpty(dirty);tx.Commit();OneSourceDirty();tx.Commit();OneSourceDirty();}finally{tx.Rollback();}}
        [TestCase(false)][TestCase(true)] public void OuterRollbackRestoresVolumeWithoutPublication(bool skin)
        {Vessel(skin);var tx=new InventoryTransaction();try{Assert.True(Fill(skin,tx));Assert.Zero(pool.Volume);}finally{tx.Rollback();}Assert.AreEqual(3,pool.Volume);Assert.Zero(Carried(skin));Assert.IsEmpty(dirty);}
        [TestCase(false)][TestCase(true)] public void FullVesselRefusalDoesNotInvalidateSource(bool skin)
        {Vessel(skin);if(skin)vessel.GetPart<WaterskinPart>().Charges=3;else{vessel.GetPart<LiquidVesselPart>().Volume=3;vessel.GetPart<LiquidVesselPart>().LiquidId="water";}Assert.False(Fill(skin));Assert.AreEqual(3,pool.Volume);Assert.IsEmpty(dirty);}
        [TestCase(false,"removed")][TestCase(true,"removed")][TestCase(false,"replacement")][TestCase(true,"replacement")]
        [TestCase(false,"part")][TestCase(true,"part")][TestCase(false,"moved")][TestCase(true,"moved")]
        [TestCase(false,"backlink")][TestCase(true,"backlink")]
        public void StaleOuterCommitDoesNotPublishAnotherSourceOrCell(bool skin,string mutation)
        {
            Vessel(skin);var tx=new InventoryTransaction();try
            {
                Assert.True(Fill(skin,tx));Assert.IsEmpty(dirty);
                if(mutation=="removed"||mutation=="replacement")
                {Assert.True(zone.RemoveEntity(source));if(mutation=="replacement"){var other=Owner("SpreadDrawPoint");other.ID=source.ID;other.AddPart(new LiquidPoolPart{LiquidId="water",Volume=3});Assert.True(zone.AddEntity(other,11,10));Assert.AreNotSame(source,zone.GetReadOnlyEntities().Single(e=>e.ID==source.ID));}}
                else if(mutation=="part"){source.RemovePart(pool);source.AddPart(new LiquidPoolPart{LiquidId="water",Volume=3});Assert.AreNotSame(pool,source.GetPart<LiquidPoolPart>());}
                else if(mutation=="moved"){Assert.True(zone.MoveEntity(source,12,10));Assert.AreEqual((12,10),zone.GetEntityPosition(source));}
                else{pool.ParentEntity=actor;Assert.AreNotSame(source,pool.ParentEntity);}
                dirty.Clear();tx.Commit();Assert.IsEmpty(dirty);
            }finally{tx.Rollback();}
        }
        [TestCase(false)][TestCase(true)] public void ExistingPouredPoolStillRetiresThroughItsExistingLifecycle(bool skin)
        {Vessel(skin);source.BlueprintName=LiquidVesselService.PoolBlueprint;Assert.True(Fill(skin));Assert.Zero(pool.Volume);Assert.IsNull(zone.GetEntityCell(source));Assert.True(dirty.Any(d=>d.reason=="LiquidVessel.SourceExhausted"));}
        [TestCase(false)][TestCase(true)] public void MovedPouredOwnerCannotRetireOrDirtyItsNewCellOnOuterCommit(bool skin)
        {Vessel(skin);source.BlueprintName=LiquidVesselService.PoolBlueprint;var tx=new InventoryTransaction();try{Assert.True(Fill(skin,tx));Assert.True(zone.MoveEntity(source,12,10));Assert.AreEqual((12,10),zone.GetEntityPosition(source));dirty.Clear();tx.Commit();Assert.AreSame(zone.GetCell(12,10),zone.GetEntityCell(source));Assert.AreEqual((12,10),zone.GetEntityPosition(source));Assert.IsEmpty(dirty);}finally{tx.Rollback();}}
        [Test] public void InfiniteWellWithoutFinitePoolDoesNotEmitFiniteVolumeInvalidation()
        {Vessel(true);source.RemovePart(pool);source.AddPart(new WellPart());Assert.True(Fill(true));Assert.AreEqual(3,Carried(true));Assert.IsEmpty(dirty);}
    }
}
