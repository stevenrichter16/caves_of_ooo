using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class FiniteCookingThermalInvalidationTests
    {
        Action<int,int,string> oldCell;Action<string> oldFull;
        Zone zone;Entity owner;ThermalPart heat;CampfirePart fire;
        readonly List<(int x,int y)> cells=new List<(int,int)>();int full;
        [SetUp] public void Setup()
        {
            oldCell=ZoneRenderHooks.CellDirtyCallback;oldFull=ZoneRenderHooks.FullDirtyCallback;
            ZoneRenderHooks.CellDirtyCallback=null;ZoneRenderHooks.FullDirtyCallback=null;
            zone=new Zone("Overworld.12.10.0");owner=new Entity{ID="owned-residual-source",BlueprintName="SpreadCookingCoals"};
            owner.AddPart(new PhysicsPart());fire=new CampfirePart{FiniteCooking=true,AllowRest=false};owner.AddPart(fire);
            heat=new ThermalPart{Temperature=150,AmbientTemperature=25,AmbientDecayRate=.02f,HeatCapacity=1,FlameTemperature=400};owner.AddPart(heat);
            Assert.True(zone.AddEntity(owner,11,10));cells.Clear();full=0;
            ZoneRenderHooks.CellDirtyCallback=(x,y,_)=>cells.Add((x,y));ZoneRenderHooks.FullDirtyCallback=_=>full++;
        }
        [TearDown] public void Cleanup(){ZoneRenderHooks.CellDirtyCallback=oldCell;ZoneRenderHooks.FullDirtyCallback=oldFull;}
        void Dispatch(string name,float joules=0,Zone eventZone=null)
        {var e=GameEvent.New(name);try{e.SetParameter("Zone",eventZone??zone);e.SetParameter("Joules",joules);e.SetParameter("Radiant",false);heat.HandleEvent(e);}finally{e.Release();}}
        void Notifications(int expected){Assert.AreEqual(0,full,"No full-zone redraw.");Assert.AreEqual(expected,cells.Count);if(expected>0)Assert.AreEqual((11,10),cells[0]);}
        [TestCase(150f,25f,true,1)][TestCase(500f,25f,true,0)][TestCase(149f,25f,true,0)]
        [TestCase(149f,200f,true,1)][TestCase(150f,25f,false,0)]
        public void PassiveBoundaryOnlyInvalidatesExactFiniteSource(float temperature,float ambient,bool finite,int expected)
        {heat.Temperature=temperature;heat.AmbientTemperature=ambient;fire.FiniteCooking=finite;Dispatch("EndTurn");Assert.AreEqual(temperature-(temperature-ambient)*.02f,heat.Temperature,.0001f);Notifications(expected);}
        [TestCase(149f,1f,true,1)][TestCase(150f,-1f,true,1)][TestCase(500f,-1f,true,0)]
        [TestCase(149f,0f,true,0)][TestCase(149f,1f,false,0)]
        public void ActualApplyHeatSharesBoundaryAndPreservesLegacy(float temperature,float joules,bool finite,int expected)
        {heat.Temperature=temperature;fire.FiniteCooking=finite;Dispatch("ApplyHeat",joules);Assert.AreEqual(temperature+joules,heat.Temperature,.0001f);Notifications(expected);}
        [TestCase("foreign-zone")][TestCase("removed")][TestCase("foreign-physics")]
        [TestCase("foreign-campfire")][TestCase("replaced-thermal")][TestCase("carried")][TestCase("foreign-cell")]
        public void InvalidCurrentSourceCannotInvalidateAnotherGraph(string fault)
        {
            Zone eventZone=zone;var original=zone.GetEntityCell(owner);var other=new Entity{ID="foreign"};
            if(fault=="foreign-zone")eventZone=new Zone(zone.ZoneID);
            if(fault=="removed")Assert.True(zone.RemoveEntity(owner));
            if(fault=="foreign-physics")owner.GetPart<PhysicsPart>().ParentEntity=other;
            if(fault=="foreign-campfire")fire.ParentEntity=other;
            if(fault=="replaced-thermal"){Assert.True(owner.RemovePart(heat));owner.AddPart(new ThermalPart{Temperature=150});}
            if(fault=="carried")owner.GetPart<PhysicsPart>().InInventory=other;
            if(fault=="foreign-cell")original.ParentZone=new Zone(zone.ZoneID);
            cells.Clear();Dispatch("EndTurn",eventZone:eventZone);Notifications(0);
        }
        [TestCase("unchanged",1)][TestCase("move",0)][TestCase("remove",0)]
        [TestCase("replace-campfire",0)][TestCase("clear-opt-in",0)][TestCase("replace-thermal",0)]
        public void IgnitionCallbackCannotPublishStaleOriginalCell(string mutation,int expected)
        {
            heat.Temperature=149;heat.FlameTemperature=150;int callbacks=0;
            owner.AddPart(new BeforeIgnition(()=>
            {
                callbacks++;
                if(mutation=="move"){Assert.True(zone.MoveEntity(owner,12,10));Assert.AreEqual((12,10),zone.GetEntityPosition(owner));}
                if(mutation=="remove")Assert.True(zone.RemoveEntity(owner));
                if(mutation=="replace-campfire"){Assert.True(owner.RemovePart(fire));owner.AddPart(new CampfirePart{FiniteCooking=true});}
                if(mutation=="clear-opt-in")fire.FiniteCooking=false;
                if(mutation=="replace-thermal"){Assert.True(owner.RemovePart(heat));owner.AddPart(new ThermalPart{Temperature=151});}
                cells.Clear(); // Ignore the callback's own independent spatial notifications.
            }));
            Dispatch("ApplyHeat",2);Assert.AreEqual(1,callbacks);Assert.AreEqual(151,heat.Temperature);
            Notifications(expected);Assert.False(owner.HasEffect<BurningEffect>(),"Callback veto is the paired source mutation seam, not a new ignition policy.");
            if(mutation=="move")Assert.AreEqual((12,10),zone.GetEntityPosition(owner));if(mutation=="remove")Assert.IsNull(zone.GetEntityCell(owner));
        }
        sealed class BeforeIgnition:Part
        {
            readonly Action callback;public BeforeIgnition(Action callback){this.callback=callback;}
            public override bool HandleEvent(GameEvent e){if(e.ID!="TryIgnite")return true;callback();return false;}
        }
    }
}
