using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class RouteWorkCreationProbe : ExaminablePart
    {public static Action<Entity> Run;public override bool HandleEvent(GameEvent e){if(e.ID=="ObjectCreated")Run?.Invoke(ParentEntity);return base.HandleEvent(e);}}
    public sealed class RouteWorkAfterProbe : Part
    {public Action Run;public override bool HandleEvent(GameEvent e){if(e.ID=="AfterInventoryAction")Run?.Invoke();return true;}}
    public sealed class RouteWorkToolsTests : FiftyWorldFixture
    {
        CavesOfOoo.Data.EntityFactory previousForge;
        System.Collections.Generic.Dictionary<string,GasDefinition> previousGases;bool previousGasInitialized;
        static readonly System.Reflection.FieldInfo GasRows=typeof(GasRegistry).GetField("_byId",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        static readonly System.Reflection.FieldInfo GasReady=typeof(GasRegistry).GetField("_initialized",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        [SetUp] public void ReadyRouteWork()
        {
            previousForge=ForgePart.Factory;ForgePart.Factory=Factory;MaterialProcessRegistry.ResetForTests();
            previousGases=new System.Collections.Generic.Dictionary<string,GasDefinition>((System.Collections.Generic.Dictionary<string,GasDefinition>)GasRows.GetValue(null));previousGasInitialized=GasRegistry.IsInitialized;
            foreach(var c in Zone.Cells){c.IsVisible=true;c.Explored=true;}
            GasRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,"Resources/Content/Data/GasDefinitions"),"*.json").Select(File.ReadAllText));
        }
        [TearDown] public void RestoreRouteWork()
        {
            ForgePart.Factory=previousForge;MaterialProcessRegistry.ResetForTests();
            var rows=(System.Collections.Generic.Dictionary<string,GasDefinition>)GasRows.GetValue(null);rows.Clear();foreach(var pair in previousGases)rows[pair.Key]=pair.Value;GasReady.SetValue(null,previousGasInitialized);
        }
        Entity Cloud(int density=30,int x=11,int y=10)
        {var e=GasFactory.SpawnGas(Zone,x,y,"poison-vapor",density,2,Actor);Assert.NotNull(e);return e;}
        Entity Door(bool closed=true)
        {var e=Place("SalvagedTimber");e.GetPart<PhysicsPart>().Takeable=false;e.AddPart(new DoorPart{IsOpen=!closed});return e;}
        [Test] public void BellowsMovesSameThinCloudWithoutSpawningOrLosingDensity()
        {
            var tool=Carry("HandBellows");var gas=Cloud();var parts=gas.Parts.ToArray();var pool=gas.GetPart<GasPoolPart>();
            Assert.True(Act(tool,Choice(tool,"PushGas|")));Assert.AreSame(Zone.GetCell(12,10),Zone.GetEntityCell(gas));
            Assert.AreEqual(30,pool.Density);Assert.AreSame(Actor,pool.Creator);Assert.AreEqual(2,pool.Level);CollectionAssert.AreEqual(parts,gas.Parts);
            Assert.AreEqual(1,Zone.GetReadOnlyEntities().Count(e=>e.HasPart<GasPoolPart>()));
        }
        [TestCase("dense")] [TestCase("stable")] [TestCase("hidden")] [TestCase("door")]
        public void BellowsRefusesUnsupportedOrBlockedGasPath(string mode)
        {
            var tool=Carry("HandBellows");var gas=Cloud();
            if(mode=="dense")gas.GetPart<GasPoolPart>().Density=41;
            if(mode=="stable")gas.GetPart<GasPoolPart>().Stable=true;
            if(mode=="hidden")Zone.GetCell(12,10).IsVisible=false;
            if(mode=="door"){var d=Place("SalvagedTimber",12,10);d.GetPart<PhysicsPart>().Takeable=false;d.AddPart(new DoorPart{IsOpen=false});}
            Assert.False(Actions(tool).Any(a=>a.Command.StartsWith("PushGas|")));
        }
        [Test] public void InterruptedBellowsDoesNotMoveOrThinGas()
        {var tool=Carry("HandBellows");var gas=Cloud();var choice=Choice(tool,"PushGas|");FailAfter();Assert.False(Act(tool,choice));Assert.AreSame(Zone.GetCell(11,10),Zone.GetEntityCell(gas));Assert.AreEqual(30,gas.GetPart<GasPoolPart>().Density);}
        [Test] public void RepeatedBellowsGatherRetainsDisplayedChoice()
        {var tool=Carry("HandBellows");Cloud();var choice=Choice(tool,"PushGas|");Actions(tool);Assert.True(Act(tool,choice));}
        [Test] public void PhantomBellowsWithDuplicateIdCannotOperateRealGas()
        {
            var phantom=Carry("HandBellows");Cloud();Assert.True(Pack.RemoveObject(phantom));
            var actual=Carry("HandBellows");actual.ID=phantom.ID;phantom.GetPart<PhysicsPart>().InInventory=Actor;
            Assert.False(Actions(phantom).Any(a=>a.Command.StartsWith("PushGas|")));
        }
        [Test] public void SandMarkSpendsOneUnitAndPersistsItsPlayerMeaning()
        {
            Bed(10,10,false);var sand=Carry("SilverSand");sand.GetPart<StackerPart>().StackCount=2;
            Assert.True(Act(sand,Choice(sand,"MakeTrailMark|",a=>a.Display.Contains("water"))));Assert.AreEqual(1,sand.GetPart<StackerPart>().StackCount);
            var mark=Zone.GetCell(10,10).Objects.Single(e=>e.BlueprintName=="TrailMark");Assert.False(mark.GetPart<PhysicsPart>().Solid);Assert.False(mark.HasPart<LightSourcePart>());
            StringAssert.Contains("water",mark.GetPart<TrailMarkPart>().DescribeState(default));
            var fresh=PartRoundTripHelper.RoundTripEntityViaTokenGraph(mark);Assert.AreEqual(mark.ID,fresh.ID);StringAssert.Contains("water",fresh.GetPart<TrailMarkPart>().DescribeState(default));
        }
        [Test] public void WetFloorAndDuplicateMarkRefuseSandPayment()
        {
            Bed(10,10,false);var sand=Carry("SilverSand");Zone.TileState.WriteCoating(10,10,"water",5);
            Assert.False(Actions(sand).Any(a=>a.Command.StartsWith("MakeTrailMark|")));
        }
        [Test] public void InterruptedMarkLeavesSandAndNoGroundMarker()
        {Bed(10,10,false);var sand=Carry("SilverSand");var choice=Choice(sand,"MakeTrailMark|",a=>a.Display.Contains("exit"));FailAfter();Assert.False(Act(sand,choice));Assert.True(Pack.Objects.Contains(sand));Assert.False(Zone.GetReadOnlyEntities().Any(e=>e.BlueprintName=="TrailMark"));}
        [Test] public void SameIronSpikeBracesSharedDoorAndCanBeRecovered()
        {
            var door=Door();var spike=Carry("IronSpikeComponent");string id=spike.ID;
            Assert.True(Act(spike,Choice(spike,"BraceDoor|")));Assert.False(door.GetPart<DoorPart>().CanOperate(Actor,Zone));Assert.False(door.GetPart<DoorPart>().TrySetOpen(Actor,Zone,true));
            Assert.AreSame(Zone.GetCell(11,10),Zone.GetEntityCell(spike));Assert.False(spike.GetPart<PhysicsPart>().Takeable);
            Assert.True(Act(spike,Choice(spike,"RemoveDoorBrace|")));Assert.AreEqual(id,spike.ID);Assert.True(Pack.Objects.Contains(spike));Assert.True(door.GetPart<DoorPart>().TrySetOpen(Actor,Zone,true));
        }
        [TestCase("open")] [TestCase("locked")] [TestCase("owned")] [TestCase("stack")]
        public void DoorBracingRefusesUnsupportedDoorOrAmbiguousSpike(string mode)
        {
            var door=Door();var spike=Carry("IronSpikeComponent");
            if(mode=="open")door.GetPart<DoorPart>().IsOpen=true;
            if(mode=="locked")door.AddPart(new LockPart{IsLocked=true});
            if(mode=="owned")door.GetPart<DoorPart>().OwnerId="other";
            if(mode=="stack")spike.GetPart<StackerPart>().StackCount=2;
            Assert.False(Actions(spike).Any(a=>a.Command.StartsWith("BraceDoor|")));
        }
        [Test] public void OrdinaryDoorOpeningAiAlsoRespectsAndRecoversFromBrace()
        {
            var door=Door();var spike=Carry("IronSpikeComponent");var npc=Place("Villager",12,10);npc.SetTag("CanOpenDoors");
            Assert.True(door.GetPart<DoorPart>().CanOperate(npc,Zone));Assert.True(Act(spike,Choice(spike,"BraceDoor|")));
            Assert.False(door.GetPart<DoorPart>().CanOperate(npc,Zone));Assert.False(door.GetPart<DoorPart>().TrySetOpen(npc,Zone,true));
            Assert.True(Act(spike,Choice(spike,"RemoveDoorBrace|")));Assert.True(door.GetPart<DoorPart>().TrySetOpen(npc,Zone,true));
        }
        [Test] public void BrushingMarkDoesNotReturnSandAndAllowsLaterMarking()
        {
            Bed(10,10,false);var sand=Carry("SilverSand");Assert.True(Act(sand,Choice(sand,"MakeTrailMark|",a=>a.Display.Contains("north"))));
            var mark=Zone.GetCell(10,10).Objects.Single(e=>e.BlueprintName=="TrailMark");Assert.True(Act(mark,Choice(mark,"ClearTrailMark|")));
            Assert.AreEqual(0,Count("SilverSand"));Assert.IsNull(Zone.GetEntityCell(mark));
        }
        [Test] public void ExistingMarkRefusesSecondUnit()
        {
            Bed(10,10,false);var sand=Carry("SilverSand");sand.GetPart<StackerPart>().StackCount=2;
            Assert.True(Act(sand,Choice(sand,"MakeTrailMark|",a=>a.Display.Contains("exit"))));
            Assert.False(Actions(sand).Any(a=>a.Command.StartsWith("MakeTrailMark|")));Assert.AreEqual(1,sand.GetPart<StackerPart>().StackCount);
        }
        [Test] public void LateMarkerCustodyChangeCannotLeaveHeldAndGroundAlias()
        {
            Bed(10,10,false);var sand=Carry("SilverSand");Entity mark=null;
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>{mark=Zone.GetReadOnlyEntities().Single(e=>e.BlueprintName=="TrailMark");Assert.True(Pack.AddObject(mark));}});
            Assert.False(Act(sand,Choice(sand,"MakeTrailMark|",a=>a.Display.Contains("water"))));
            Assert.IsNull(Zone.GetEntityCell(mark));Assert.True(Pack.Objects.Contains(mark));Assert.AreEqual(0,Count("SilverSand"),"a retained paid output does not also refund material");
        }
        [Test] public void LateDoorRemovalRefusesBraceWithoutRestoringDoorOrLosingSpike()
        {
            var door=Door();var spike=Carry("IronSpikeComponent");var choice=Choice(spike,"BraceDoor|");
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>Zone.RemoveEntity(door)});Assert.False(Act(spike,choice));
            Assert.IsNull(Zone.GetEntityCell(door));Assert.True(Pack.Objects.Contains(spike));Assert.True(spike.GetPart<PhysicsPart>().Takeable);
        }
        [Test] public void RecoveryKeepsOriginalIdentityBesideAnotherSpikeStack()
        {
            var door=Door();var spike=Carry("IronSpikeComponent");Assert.True(Act(spike,Choice(spike,"BraceDoor|")));
            var spare=Carry("IronSpikeComponent");spare.GetPart<StackerPart>().StackCount=2;
            Assert.True(Act(spike,Choice(spike,"RemoveDoorBrace|")));Assert.True(Pack.Objects.Contains(spike));Assert.AreEqual(1,spike.GetPart<StackerPart>().StackCount);Assert.AreEqual(2,spare.GetPart<StackerPart>().StackCount);
        }
        [Test] public void BracedDoorReferenceRoundTripsWithTheOriginalSpikeIdentity()
        {
            var door=Door();var spike=Carry("IronSpikeComponent");Assert.True(Act(spike,Choice(spike,"BraceDoor|")));
            var fresh=PartRoundTripHelper.RoundTripEntityViaTokenGraph(spike);Assert.AreEqual(spike.ID,fresh.ID);
            Assert.AreEqual(door.ID,fresh.GetPart<LodgedSpikePart>().Door.ID);Assert.True(fresh.GetPart<LodgedSpikePart>().Door.GetPart<DoorPart>().IsClosed);Assert.False(fresh.GetPart<PhysicsPart>().Takeable);
        }
        [Test] public void FullPackRefusesSpikeRecoveryWithoutLosingTheBrace()
        {
            var door=Door();var spike=Carry("IronSpikeComponent");Assert.True(Act(spike,Choice(spike,"BraceDoor|")));Pack.MaxWeight=0;
            Assert.False(Actions(spike).Any(a=>a.Command.StartsWith("RemoveDoorBrace|")));Assert.False(door.GetPart<DoorPart>().CanOperate(Actor,Zone));Assert.AreSame(Zone.GetCell(11,10),Zone.GetEntityCell(spike));
        }
        [Test] public void UnadmittedFactoryPartsRefuseMarkerBeforeCreationAndPayment()
        {
            Bed(10,10,false);var sand=Carry("SilverSand");Entity created=null;
            Factory.RegisterPartType<RouteWorkCreationProbe>("Examinable");
            RouteWorkCreationProbe.Run=e=>{if(e.BlueprintName=="TrailMark"){created=e;Assert.True(Pack.AddObject(e));}};
            try{Assert.False(Actions(sand).Any(a=>a.Command.StartsWith("MakeTrailMark|")));Assert.IsNull(created);Assert.True(Pack.Objects.Contains(sand));}
            finally{RouteWorkCreationProbe.Run=null;}
        }
        [Test] public void DoorClosingBeyondGasBeforeCommitKeepsCloudAtSource()
        {
            var tool=Carry("HandBellows");var gas=Cloud();string command=Choice(tool,"PushGas|");
            Actor.AddPart(new RouteWorkAfterProbe{Run=()=>{var d=Place("VillageDoor",12,10);d.GetPart<DoorPart>().IsOpen=false;}});
            Assert.False(Act(tool,command));Assert.AreSame(Zone.GetCell(11,10),Zone.GetEntityCell(gas));Assert.AreEqual(30,gas.GetPart<GasPoolPart>().Density);
        }
        [Test] public void BellowsForgeManifestSpendsExactThreeComponentsAndSurvivesHintRefresh()
        {
            Place("TinkersForge");var spike=Carry("IronSpikeComponent");Carry("OakHaftComponent");Carry("LeatherBindingComponent");
            string command=Choice(spike,"MaterialProcess|make_hand_bellows|");UseGuides.SectionText(spike,Zone,null,Actor);Actions(spike);
            Assert.True(Act(spike,command));Assert.AreEqual(0,Count("IronSpikeComponent"));Assert.AreEqual(0,Count("OakHaftComponent"));Assert.AreEqual(0,Count("LeatherBindingComponent"));Assert.AreEqual(1,Count("HandBellows"));
        }
        [Test] public void DestroyedDoorLeavesExactSpikeRecoverable()
        {
            var door=Door();var spike=Carry("IronSpikeComponent");Assert.True(Act(spike,Choice(spike,"BraceDoor|")));Assert.True(Zone.RemoveEntity(door));
            Assert.True(Act(spike,Choice(spike,"RemoveDoorBrace|")));Assert.True(Pack.Objects.Contains(spike));Assert.AreEqual(1,Count("IronSpikeComponent"));
        }
        [Test] public void InterruptedBraceLeavesOrdinaryDoorAndOriginalSpike()
        {var door=Door();var spike=Carry("IronSpikeComponent");var choice=Choice(spike,"BraceDoor|");FailAfter();Assert.False(Act(spike,choice));Assert.True(Pack.Objects.Contains(spike));Assert.True(spike.GetPart<PhysicsPart>().Takeable);Assert.True(door.GetPart<DoorPart>().CanOperate(Actor,Zone));}
    }
}
