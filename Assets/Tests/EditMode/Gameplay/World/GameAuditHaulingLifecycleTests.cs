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
    internal sealed class HaulLifecycleFixture : IDisposable
    {
        private readonly HotbarSaveFixture _scope=new HotbarSaveFixture(false,false);
        private readonly Action<string> _oldMessage=MessageLog.OnMessage;
        private readonly List<(FieldInfo field,object value)> _hooks=new List<(FieldInfo,object)>();
        private static EntityFactory _factory;
        public readonly Zone Zone=new Zone("Overworld.10.10.0"),Other=new Zone("Overworld.11.10.0");
        public readonly Entity Actor,Load;
        public readonly int OriginalSpeed;
        public HaulLifecycleFixture(int speed=100,int unrelatedPenalty=0)
        {
            try
            {
                MessageLog.OnMessage=null;DestructionSystem.EntityFactoryRef=null;
                foreach(var type in new[]{typeof(EntityVisualHooks),typeof(ZoneRenderHooks)})
                    foreach(var field in type.GetFields(BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic))
                        if(!field.IsInitOnly&&typeof(Delegate).IsAssignableFrom(field.FieldType)){_hooks.Add((field,field.GetValue(null)));field.SetValue(null,null);}
                if(_factory==null){_factory=new EntityFactory();_factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));}
                Actor=MakeActor("hauler",speed,unrelatedPenalty);Load=MakeLoad();Assert.IsTrue(Zone.AddEntity(Actor,5,5));Assert.IsTrue(Zone.AddEntity(Load,6,5));OriginalSpeed=Actor.GetStatValue("Speed");
                Assert.AreEqual(75,DragRules.WeightOf(Load));Assert.AreEqual(8,Load.GetPart<DestructiblePart>().HP);Assert.AreEqual(0,Load.GetPart<DestructiblePart>().Hardness);Assert.AreEqual(DragVerdict.Ok,DragSystem.TryGrab(Actor,Load,Zone));
                Assert.AreEqual(Math.Max(20,OriginalSpeed-30),Actor.GetStatValue("Speed"));
            }
            catch{Dispose();throw;}
        }
        public static Entity MakeActor(string name,int speed=100,int penalty=0)
        {
            var actor=new Entity{ID=Guid.NewGuid().ToString("N"),BlueprintName=name};actor.Tags["Creature"]="";
            actor.Statistics["Strength"]=new Stat{Name="Strength",Owner=actor,BaseValue=16,Min=0,Max=40};actor.Statistics["Speed"]=new Stat{Name="Speed",Owner=actor,BaseValue=speed,Penalty=penalty,Min=0,Max=999};
            actor.Statistics["Hitpoints"]=new Stat{Name="Hitpoints",Owner=actor,BaseValue=20,Min=0,Max=20};actor.AddPart(new PhysicsPart{Weight=100,Takeable=false});return actor;
        }
        public static Entity MakeLoad()=>_factory.CreateEntity("HaulBarrel");
        public bool Move(bool forced,int x=4,int y=5)=>forced?MovementSystem.ForceMoveTo(Actor,Zone,x,y):MovementSystem.TryMoveTo(Actor,Zone,x,y);
        public void Released()
        {Assert.IsNull(Actor.GetPart<DragPart>());Assert.IsNull(Load.GetPart<DraggedPart>());Assert.AreEqual(OriginalSpeed,Actor.GetStatValue("Speed"));}
        public void Healthy()
        {Assert.AreSame(Load,Actor.GetPart<DragPart>()?.Dragged);Assert.AreSame(Actor,Load.GetPart<DraggedPart>()?.Dragger);Assert.AreEqual(Math.Max(20,OriginalSpeed-30),Actor.GetStatValue("Speed"));}
        public static void RawRemove(Zone zone,Entity entity)
        {Assert.IsTrue(zone.GetEntityCell(entity).Objects.Remove(entity));zone.RebuildEntityCellsFromCells();}
        public GameSessionState RoundTrip(bool otherFirst=false)
        {
            var manager=new OverworldZoneManager(null,333);var zones=new Dictionary<string,Zone>();if(otherFirst){zones.Add(Other.ZoneID,Other);zones.Add(Zone.ZoneID,Zone);}else{zones.Add(Zone.ZoneID,Zone);zones.Add(Other.ZoneID,Other);}manager.ReplaceLoadedState(zones,Zone.ZoneID,new Dictionary<string,List<ZoneConnection>>());
            var turns=new TurnManager();turns.RestoreSavedState(17,true,Actor,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=Actor,Energy=1000}});
            var state=GameSessionState.Capture(Guid.NewGuid().ToString("N"),"haul-audit",manager,turns,Actor);
            using(var stream=new MemoryStream()){state.Save(new SaveWriter(stream));stream.Position=0;return GameSessionState.Load(new SaveReader(stream,null));}
        }
        public void Dispose()
        {for(int i=_hooks.Count-1;i>=0;i--)_hooks[i].field.SetValue(null,_hooks[i].value);MessageLog.OnMessage=_oldMessage;_scope.Dispose();}
    }
    public sealed class HaulMoveObserver : Part
    {
        public Action OnMove;
        public override bool HandleEvent(GameEvent e){if(e.ID=="AfterMove")OnMove?.Invoke();return true;}
    }
    public sealed class HaulDestroyObserver : Part
    {
        public string ObserveEvent;
        public Action Observe;
        public bool Veto;
        public override bool HandleEvent(GameEvent e){if(e.ID!=ObserveEvent)return true;Observe?.Invoke();return !Veto;}
    }
    public class GameAuditHaulingLifecycleTests
    {
        [TestCase(false)] [TestCase(true)]
        public void RemovedLoadIsReleasedImmediatelyAndNeverReappearsOnMovement(bool forced)
        {using(var f=new HaulLifecycleFixture()){Assert.IsTrue(f.Zone.RemoveEntity(f.Load));f.Released();Assert.IsTrue(f.Move(forced));Assert.AreEqual((-1,-1),f.Zone.GetEntityPosition(f.Load));f.Released();}}
        [Test]
        public void RemovedHaulerMakesItsLoadAvailableImmediately()
        {using(var f=new HaulLifecycleFixture()){Assert.IsTrue(f.Zone.RemoveEntity(f.Actor));f.Released();Assert.AreEqual((6,5),f.Zone.GetEntityPosition(f.Load));}}
        [TestCase(false)] [TestCase(true)]
        public void WrongZoneRemovalPreservesExactHealthyRelationship(bool actor)
        {using(var f=new HaulLifecycleFixture()){var grip=f.Actor.GetPart<DragPart>();var held=f.Load.GetPart<DraggedPart>();Assert.IsFalse(f.Other.RemoveEntity(actor?f.Actor:f.Load));Assert.AreSame(grip,f.Actor.GetPart<DragPart>());Assert.AreSame(held,f.Load.GetPart<DraggedPart>());f.Healthy();}}
        [TestCase(100,7)] [TestCase(35,7)]
        public void RemovalRefundsOnlyStoredPenaltyAndNeverTwice(int speed,int unrelated)
        {using(var f=new HaulLifecycleFixture(speed,unrelated)){var grip=f.Actor.GetPart<DragPart>();Assert.IsTrue(f.Zone.RemoveEntity(f.Load));f.Released();Assert.AreEqual(0,grip.AppliedPenalty);Assert.AreEqual(unrelated,f.Actor.GetStat("Speed").Penalty);Assert.IsFalse(f.Zone.RemoveEntity(f.Load));Assert.IsFalse(DragSystem.Release(f.Actor));Assert.AreEqual(unrelated,f.Actor.GetStat("Speed").Penalty);}}
        [Test]
        public void NullLoadPointerCannotKeepOrphanPenaltyAfterMovement()
        {using(var f=new HaulLifecycleFixture()){f.Actor.GetPart<DragPart>().Dragged=null;Assert.IsTrue(f.Move(false));Assert.IsNull(f.Actor.GetPart<DragPart>());Assert.AreEqual(100,f.Actor.GetStatValue("Speed"));}}
        [Test]
        public void MissingReciprocalLinkRefusesToPullTheLoad()
        {using(var f=new HaulLifecycleFixture()){Assert.IsTrue(f.Load.RemovePart(f.Load.GetPart<DraggedPart>()));Assert.IsTrue(f.Move(false));Assert.AreEqual((6,5),f.Zone.GetEntityPosition(f.Load));f.Released();}}
        [Test]
        public void StaleHaulerDoesNotStealAnotherHaulersLoad()
        {using(var f=new HaulLifecycleFixture()){f.Load.RemovePart(f.Load.GetPart<DraggedPart>());var other=HaulLifecycleFixture.MakeActor("second-hauler");Assert.IsTrue(f.Zone.AddEntity(other,5,6));Assert.AreEqual(DragVerdict.Ok,DragSystem.TryGrab(other,f.Load,f.Zone));var grip=other.GetPart<DragPart>();var held=f.Load.GetPart<DraggedPart>();Assert.IsTrue(f.Move(false));Assert.IsNull(f.Actor.GetPart<DragPart>());Assert.AreEqual(100,f.Actor.GetStatValue("Speed"));Assert.AreEqual((6,5),f.Zone.GetEntityPosition(f.Load));Assert.AreSame(grip,other.GetPart<DragPart>());Assert.AreSame(held,f.Load.GetPart<DraggedPart>());Assert.AreEqual(70,other.GetStatValue("Speed"));}}
        [Test]
        public void RemovingStaleLoadDoesNotReleaseItsNamedHaulersDifferentLoad()
        {using(var f=new HaulLifecycleFixture()){var other=HaulLifecycleFixture.MakeLoad();Assert.IsTrue(f.Zone.AddEntity(other,5,6));f.Actor.GetPart<DragPart>().Dragged=other;other.AddPart(new DraggedPart{Dragger=f.Actor});var grip=f.Actor.GetPart<DragPart>();Assert.IsTrue(f.Zone.RemoveEntity(f.Load));Assert.IsNull(f.Load.GetPart<DraggedPart>());Assert.AreSame(grip,f.Actor.GetPart<DragPart>());Assert.AreSame(other,grip.Dragged);Assert.AreSame(f.Actor,other.GetPart<DraggedPart>().Dragger);Assert.AreEqual(70,f.Actor.GetStatValue("Speed"));}}
        [TestCase(false,4,5)] [TestCase(true,4,5)] [TestCase(false,4,4)]
        public void HealthyVoluntaryForcedAndDiagonalFollowStillWork(bool forced,int x,int y)
        {using(var f=new HaulLifecycleFixture()){Assert.IsTrue(f.Move(forced,x,y));Assert.AreEqual((5,5),f.Zone.GetEntityPosition(f.Load));Assert.AreEqual((x,y),f.Zone.GetEntityPosition(f.Actor));f.Healthy();}}
        [Test]
        public void RemovingInvalidDragPartDoesNotSkipTrailingMoveListener()
        {using(var f=new HaulLifecycleFixture()){int calls=0;f.Actor.AddPart(new HaulMoveObserver{OnMove=()=>calls++});f.Actor.GetPart<DragPart>().Dragged=null;Assert.IsTrue(f.Move(false));Assert.AreEqual(1,calls);Assert.IsNull(f.Actor.GetPart<DragPart>());Assert.AreEqual(100,f.Actor.GetStatValue("Speed"));}}
        [TestCase(false)] [TestCase(true)]
        public void DestroyedVersusVetoedBarrelCannotBeConfusedAtCallbackTime(bool veto)
        {
            using(var f=new HaulLifecycleFixture())
            {
                bool called=false;f.Load.AddPart(new HaulDestroyObserver{ObserveEvent=veto?"BeforeDestroy":"Destroyed",Veto=veto,Observe=()=>
                {called=true;Assert.AreEqual(veto,!f.Load.GetPart<DestructiblePart>().Gone);Assert.AreEqual(0,f.Load.GetPart<DestructiblePart>().HP);Assert.IsTrue(f.Move(true));Assert.AreEqual(veto?(5,5):(6,5),f.Zone.GetEntityPosition(f.Load));if(veto)f.Healthy();else f.Released();}});
                Assert.AreEqual(veto?DestroyVerdict.Vetoed:DestroyVerdict.Destroyed,DestructionSystem.Damage(f.Load,8,f.Actor,f.Zone));Assert.IsTrue(called);if(veto)f.Healthy();else{f.Released();Assert.AreEqual((-1,-1),f.Zone.GetEntityPosition(f.Load));}
            }
        }
        [Test]
        public void NonlethalRealBarrelDamageKeepsFollowing()
        {using(var f=new HaulLifecycleFixture()){Assert.AreEqual(DestroyVerdict.Damaged,DestructionSystem.Damage(f.Load,1,f.Actor,f.Zone));Assert.AreEqual(7,f.Load.GetPart<DestructiblePart>().HP);Assert.IsTrue(f.Move(false));f.Healthy();Assert.AreEqual((5,5),f.Zone.GetEntityPosition(f.Load));}}
        [Test]
        public void DirectDestroyReleasesEvenWhenStructuralHpStaysPositive()
        {using(var f=new HaulLifecycleFixture()){Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Destroy(f.Load,f.Actor,f.Zone,"audit"));Assert.Greater(f.Load.GetPart<DestructiblePart>().HP,0);Assert.IsTrue(f.Load.GetPart<DestructiblePart>().Gone);f.Released();}}
        [Test]
        public void HealthySessionRoundTripKeepsExactPenaltyAndReciprocalAliases()
        {using(var f=new HaulLifecycleFixture(100,7)){var loaded=f.RoundTrip();var actor=loaded.Player;var load=actor.GetPart<DragPart>().Dragged;Assert.AreEqual(f.Load.ID,load.ID);Assert.AreSame(actor,load.GetPart<DraggedPart>().Dragger);Assert.AreEqual(63,actor.GetStatValue("Speed"));Assert.AreEqual(30,actor.GetPart<DragPart>().AppliedPenalty);Assert.AreEqual((6,5),loaded.ZoneManager.ActiveZone.GetEntityPosition(load));}}
        [TestCase("load-unplaced")] [TestCase("actor-unplaced")] [TestCase("cross-zone")] [TestCase("gone")] [TestCase("dead-actor")] [TestCase("both-unplaced")]
        public void HistoricalInvalidSavedRelationshipsAreRepairedAfterWorldRebuild(string shape)
        {
            using(var f=new HaulLifecycleFixture(100,7))
            {
                if(shape=="load-unplaced"||shape=="both-unplaced"||shape=="cross-zone")HaulLifecycleFixture.RawRemove(f.Zone,f.Load);
                if(shape=="actor-unplaced"||shape=="both-unplaced")HaulLifecycleFixture.RawRemove(f.Zone,f.Actor);
                if(shape=="cross-zone")Assert.IsTrue(f.Other.AddEntity(f.Load,6,5));
                if(shape=="gone")f.Load.GetPart<DestructiblePart>().Gone=true;
                if(shape=="dead-actor")f.Actor.GetStat("Hitpoints").BaseValue=0;
                Assert.NotNull(f.Actor.GetPart<DragPart>());Assert.NotNull(f.Load.GetPart<DraggedPart>());Assert.AreEqual(63,f.Actor.GetStatValue("Speed"));
                var loaded=f.RoundTrip(otherFirst:true);Assert.IsNull(loaded.Player.GetPart<DragPart>());Assert.AreEqual(93,loaded.Player.GetStatValue("Speed"));
                foreach(var zone in loaded.ZoneManager.CachedZones.Values)foreach(var entity in zone.GetReadOnlyEntities())if(entity.ID==f.Load.ID)Assert.IsNull(entity.GetPart<DraggedPart>());
                SaveGraphSerializer.RebuildLoadedWorld(loaded);Assert.AreEqual(93,loaded.Player.GetStatValue("Speed"));
            }
        }
    }
}
