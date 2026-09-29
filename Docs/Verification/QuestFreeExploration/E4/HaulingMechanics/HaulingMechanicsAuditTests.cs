using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    // Private core audit, reconstructed after machine restart erased /tmp.
    // No native input, rendered pixel or original travel claim.
    public sealed class HaulingMechanicsAuditTests
    {
        EntityFactory factory;
        [SetUp] public void Setup()
        {
            MessageLog.OnMessage=null; MessageLog.Clear(); TurnManager.World=null;
            factory=new EntityFactory(); factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
        }
        void Fixture(string blueprint,out Zone zone,out Entity player,out Entity load,out TurnManager turns)
        {
            zone=new Zone("Overworld.10.10.0");player=factory.CreateEntity("Player");load=factory.CreateEntity(blueprint);
            Assert.AreEqual(18,player.GetStatValue("Strength"));Assert.AreEqual(100,player.GetStatValue("Speed"));
            Assert.True(zone.AddEntity(player,10,10));Assert.True(zone.AddEntity(load,11,10));
            turns=new TurnManager();turns.RestoreSavedState(17,true,player,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=player,Energy=1000}});
        }
        static void Command(Entity load,Entity player,Zone zone,string command)
        {var e=GameEvent.New("InventoryAction");e.SetParameter("Actor",(object)player);e.SetParameter("Zone",(object)zone);e.SetParameter("Command",command);load.FireEventAndRelease(e);}
        static void Step(Entity player,Zone zone,TurnManager turns)
        {Assert.True(MovementSystem.TryMove(player,zone,-1,0));turns.EndTurn(player,zone);Assert.AreSame(player,turns.ProcessUntilPlayerTurn());}
        static void OneOwner(Zone zone,Entity load)
        {Assert.AreEqual(1,zone.GetReadOnlyEntities().Count(e=>e.ID==load.ID));Assert.AreEqual(1,zone.GetEntityCell(load).Objects.Count(e=>ReferenceEquals(e,load)));}
        static GameSessionState RoundTrip(Zone zone,Entity player,TurnManager turns)
        {
            var manager=new OverworldZoneManager(null,64);manager.ReplaceLoadedState(new Dictionary<string,Zone>{{zone.ZoneID,zone}},zone.ZoneID,new Dictionary<string,List<ZoneConnection>>());
            var state=GameSessionState.Capture(Guid.NewGuid().ToString("N"),"private-haul-audit",manager,turns,player);
            using(var s=new MemoryStream()){state.Save(new SaveWriter(s));s.Position=0;return GameSessionState.Load(new SaveReader(s,null));}
        }
        [TestCase("FallenBeam",true,24,27,1052)][TestCase("FallenBeam",false,24,20,1000)]
        [TestCase("HaulBarrel",true,30,29,1030)][TestCase("HaulBarrel",false,30,20,1000)]
        public void TwoPaidPullsUseExactLoadAndReducedScheduler(string blueprint,bool grabbed,int penalty,int elapsed,int finalEnergy)
        {
            Fixture(blueprint,out var zone,out var player,out var load,out var turns);
            var physics=load.GetPart<PhysicsPart>();var handling=load.GetPart<HandlingPart>();var render=load.GetPart<RenderPart>();string id=load.ID;
            var properties=load.Properties.OrderBy(p=>p.Key).ToArray();int? hp=load.GetPart<DestructiblePart>()?.HP;
            if(grabbed)Command(load,player,zone,HandlingPart.HaulCommand);
            Assert.AreEqual(17,turns.TickCount);Assert.AreEqual(1000,turns.GetEnergy(player));Assert.AreEqual(grabbed?100-penalty:100,player.GetStatValue("Speed"));
            Step(player,zone,turns);Assert.AreEqual((9,10),zone.GetEntityPosition(player));Assert.AreEqual((grabbed?10:11,10),zone.GetEntityPosition(load));
            Step(player,zone,turns);Assert.AreEqual((8,10),zone.GetEntityPosition(player));Assert.AreEqual((grabbed?9:11,10),zone.GetEntityPosition(load));
            Assert.AreEqual(17+elapsed,turns.TickCount);Assert.AreEqual(finalEnergy,turns.GetEnergy(player));OneOwner(zone,load);
            Assert.AreEqual(id,load.ID);Assert.AreSame(physics,load.GetPart<PhysicsPart>());Assert.AreSame(handling,load.GetPart<HandlingPart>());Assert.AreSame(render,load.GetPart<RenderPart>());
            Assert.AreEqual(hp,load.GetPart<DestructiblePart>()?.HP);CollectionAssert.AreEqual(properties,load.Properties.OrderBy(p=>p.Key).ToArray());
        }
        [TestCase("FallenBeam",true,24)][TestCase("FallenBeam",false,24)][TestCase("HaulBarrel",true,30)][TestCase("HaulBarrel",false,30)]
        public void TwoPullSavedReplacementRetainsOneOwnerAndExactlyRefundsGrip(string blueprint,bool held,int penalty)
        {
            Fixture(blueprint,out var zone,out var player,out var load,out var turns);player.GetStat("Speed").Penalty+=7;
            Command(load,player,zone,HandlingPart.HaulCommand);Step(player,zone,turns);Step(player,zone,turns);if(!held)Command(load,player,zone,HandlingPart.ReleaseCommand);
            var oldPlayer=player;var oldLoad=load;var state=RoundTrip(zone,player,turns);zone=state.ZoneManager.ActiveZone;player=state.Player;turns=state.TurnManager;load=zone.GetReadOnlyEntities().Single(e=>e.ID==oldLoad.ID);
            Assert.AreNotSame(oldPlayer,player);Assert.AreNotSame(oldLoad,load);Assert.AreEqual((9,10),zone.GetEntityPosition(load));OneOwner(zone,load);Assert.AreEqual(held?93-penalty:93,player.GetStatValue("Speed"));
            if(held){Assert.AreSame(load,DragSystem.GetDragged(player));Assert.AreSame(player,DragSystem.GetDragger(load));Assert.AreEqual(penalty,player.GetPart<DragPart>().AppliedPenalty);}
            else{Assert.False(DragSystem.IsDragging(player));Assert.False(DragSystem.IsBeingDragged(load));}
            int tick=turns.TickCount,energy=turns.GetEnergy(player);Command(load,player,zone,HandlingPart.ReleaseCommand);
            Assert.AreEqual(93,player.GetStatValue("Speed"));Assert.AreEqual(7,player.GetStat("Speed").Penalty);Assert.AreEqual(tick,turns.TickCount);Assert.AreEqual(energy,turns.GetEnergy(player));Assert.False(DragSystem.Release(player));
            Step(player,zone,turns);Assert.AreEqual((9,10),zone.GetEntityPosition(load));OneOwner(zone,load);Assert.IsNull(DragSystem.GetDragged(player));Assert.IsNull(DragSystem.GetDragger(load));
        }
        [TestCase("FallenBeam",false)][TestCase("FallenBeam",true)][TestCase("HaulBarrel",false)][TestCase("HaulBarrel",true)]
        public void WorldVerbIsExclusiveAndBusyActorCannotOfferSecondGrip(string blueprint,bool grabbed)
        {
            Fixture(blueprint,out var zone,out var player,out var load,out var turns);var other=factory.CreateEntity(blueprint);Assert.True(zone.AddEntity(other,10,11));
            if(grabbed)Command(load,player,zone,HandlingPart.HaulCommand);
            var rows=WorldInteractionSystem.GatherActions(load,player);Assert.AreEqual(grabbed?0:1,rows.Count(a=>a.Command==HandlingPart.HaulCommand));Assert.AreEqual(grabbed?1:0,rows.Count(a=>a.Command==HandlingPart.ReleaseCommand));
            var verb=rows.Single(a=>a.Command==(grabbed?HandlingPart.ReleaseCommand:HandlingPart.HaulCommand));Assert.AreEqual('g',verb.Key);Assert.AreEqual(grabbed?"let go":"haul",verb.Display);
            var otherRows=WorldInteractionSystem.GatherActions(other,player);Assert.AreEqual(grabbed?0:1,otherRows.Count(a=>a.Command==HandlingPart.HaulCommand));Assert.False(otherRows.Any(a=>a.Command==HandlingPart.ReleaseCommand));Assert.AreEqual(17,turns.TickCount);Assert.AreEqual(1000,turns.GetEnergy(player));
        }
        [TestCase("FallenBeam",7,false)][TestCase("FallenBeam",8,true)][TestCase("HaulBarrel",9,false)][TestCase("HaulBarrel",10,true)]
        public void RealStrengthBoundaryMatchesAdvertisedAction(string blueprint,int strength,bool accepted)
        {
            Fixture(blueprint,out var zone,out var player,out var load,out var turns);player.GetStat("Strength").BaseValue=strength;
            Assert.AreEqual(accepted,WorldInteractionSystem.GatherActions(load,player).Any(a=>a.Command==HandlingPart.HaulCommand));Command(load,player,zone,HandlingPart.HaulCommand);
            Assert.AreEqual(accepted,DragSystem.IsDragging(player));Assert.AreEqual(accepted,DragSystem.IsBeingDragged(load));Assert.AreEqual(17,turns.TickCount);Assert.AreEqual(1000,turns.GetEnergy(player));Assert.AreEqual((11,10),zone.GetEntityPosition(load));OneOwner(zone,load);
        }
        [TestCase("FallenBeam",true)][TestCase("FallenBeam",false)][TestCase("HaulBarrel",true)][TestCase("HaulBarrel",false)]
        public void OrdinaryBlockedWalkDoesNotMoveEitherOwnerOrSpendTime(string blueprint,bool blocked)
        {
            Fixture(blueprint,out var zone,out var player,out var load,out var turns);Command(load,player,zone,HandlingPart.HaulCommand);if(blocked)Assert.True(zone.AddEntity(factory.CreateEntity("Hedge"),9,10));
            bool moved=MovementSystem.TryMove(player,zone,-1,0);Assert.AreEqual(!blocked,moved);if(moved){turns.EndTurn(player,zone);turns.ProcessUntilPlayerTurn();}
            Assert.AreEqual((blocked?10:9,10),zone.GetEntityPosition(player));Assert.AreEqual((blocked?11:10,10),zone.GetEntityPosition(load));Assert.AreEqual(blocked,turns.TickCount==17);Assert.AreSame(load,DragSystem.GetDragged(player));Assert.AreSame(player,DragSystem.GetDragger(load));OneOwner(zone,load);
        }
    }
}
