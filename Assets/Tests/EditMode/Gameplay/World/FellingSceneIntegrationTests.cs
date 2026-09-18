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
    public sealed class FellingSceneIntegrationTests
    {
        private EntityFactory factory;
        [SetUp] public void Setup()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }
        private Zone Fresh() => new OverworldZoneManager(factory, 64).GetZone(FellingSiteBuilder.ZoneID);
        private Entity Actor(Zone z, int x = 40, int y = 20)
        { var actor = factory.CreateEntity("Player"); Assert.IsTrue(z.AddEntity(actor, x, y)); return actor; }
        private static HashSet<(int x,int y)> Flood(Zone z, int x = 40, int y = 20)
        {
            var seen = new HashSet<(int,int)>(); var q = new Queue<(int x,int y)>();
            if (z.GetCell(x,y)?.BlocksMovement() != false) return seen;
            q.Enqueue((x,y)); seen.Add((x,y));
            while(q.Count>0)
            {
                var p=q.Dequeue(); foreach(var d in new[]{(1,0),(-1,0),(0,1),(0,-1)})
                { var n=(p.x+d.Item1,p.y+d.Item2); if(z.GetCell(n.Item1,n.Item2)?.BlocksMovement()==false&&seen.Add(n))q.Enqueue(n); }
            }
            return seen;
        }
        private static (int x,int y) Approach(Zone z, Entity target)
        {
            var p=z.GetEntityPosition(target);var seen=Flood(z);
            for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                if(seen.Contains((p.x+dx,p.y+dy)))return(p.x+dx,p.y+dy);
            Assert.Fail("No reachable approach for "+target.GetDisplayName());return(-1,-1);
        }
        [Test] public void DefinitionLoadsEveryLayerAndEveryLogicalCell()
        {
            var d=FellingSceneDefinition.Load();Assert.IsNotNull(d);Assert.DoesNotThrow(d.Validate);
            Assert.AreEqual(55,d.layers.Length);Assert.AreEqual(39,d.layers.Count(l=>l.mutable));Assert.AreEqual(2000,d.cells.Length);
            Assert.AreSame(d,FellingSceneDefinition.Load());
        }
        [TestCase("id")] [TestCase("resource")] [TestCase("cell")] [TestCase("revision")]
        public void InvalidDefinitionCannotReachTheBuilder(string failure)
        {
            var d=JsonUtility.FromJson<FellingSceneDefinition>(JsonUtility.ToJson(FellingSceneDefinition.Load()));
            if(failure=="id")d.layers[1].id=d.layers[0].id;
            if(failure=="resource")d.layers[0].resource="../../outside";
            if(failure=="cell")d.cells[1].x=d.cells[0].x=d.cells[1].y=d.cells[0].y=0;
            if(failure=="revision")d.revision=0;
            Assert.Throws<ArgumentException>(d.Validate);
        }
        [Test] public void FreshZoneContainsOneDurableOwnerPerSourceComponent()
        {
            var z=Fresh();Assert.IsTrue(FellingSceneRuntime.IsActive(z));
            Assert.AreEqual(55,z.GetAllEntities().Count(e=>e.HasPart<FellingScenePropPart>()));
            foreach(var layer in FellingSceneDefinition.Load().layers)
            {var owner=FellingSceneRuntime.FindOwner(z,layer.id);Assert.IsNotNull(owner,layer.id);Assert.IsTrue(FellingSceneRuntime.IsPresent(z,layer.id));Assert.IsTrue(WorldInteractionSystem.GatherActions(owner).Any(a=>a.Command=="Examine"));}
        }
        [Test] public void EveryMutableSourceComponentHasAnActualReachableApproach()
        {
            var z=Fresh();foreach(var layer in FellingSceneDefinition.Load().layers.Where(l=>l.mutable))Approach(z,FellingSceneRuntime.FindOwner(z,layer.id));
        }
        [Test] public void AllSevenSourceLandmarksAndSouthEntranceRemainReachable()
        {
            var z=Fresh();var seen=Flood(z);Assert.IsTrue(seen.Contains((40,24)));
            foreach(var landmark in FellingSceneDefinition.Load().landmarks)Assert.IsTrue(seen.Contains((landmark.x,landmark.y)),landmark.id);
            Assert.AreEqual(6,z.GetAllEntities().Count(e=>e.BlueprintName=="FellingBarePosition"));
            Assert.AreEqual((40,8),z.GetEntityPosition(z.GetAllEntities().Single(e=>e.HasPart<SeventhPositionPart>())));
        }
        [Test] public void ClearThroughTheRealWorldMenuRemovesOwnerAndRetainsBackingState()
        {
            var z=Fresh();var layer=FellingSceneDefinition.Load().layers.First(l=>l.mutable&&l.blocksMovement);var owner=FellingSceneRuntime.FindOwner(z,layer.id);
            var p=Approach(z,owner);var actor=Actor(z,p.x,p.y);int inventory=actor.GetPart<InventoryPart>().Objects.Count;
            Assert.IsTrue(WorldInteractionSystem.GatherActions(owner,actor).Any(a=>a.Command==FellingScenePropPart.ClearCommand));
            var action=GameEvent.New("InventoryAction");action.SetParameter("Command",FellingScenePropPart.ClearCommand);action.SetParameter("Actor",(object)actor);action.SetParameter("Zone",(object)z);owner.FireEventAndRelease(action);
            Assert.IsFalse(FellingSceneRuntime.IsPresent(z,layer.id));Assert.IsNull(z.GetEntityCell(owner));Assert.IsFalse(z.GetCell(layer.anchorX,layer.anchorY).BlocksMovement());
            Assert.AreEqual(inventory,actor.GetPart<InventoryPart>().Objects.Count);Assert.IsTrue(FellingSceneRuntime.GetState(z).WasRemoved(layer.id));
        }
        [Test] public void ActualMovementStopsAtALooseStoneAndEntersAfterClearing()
        {
            var z=Fresh();var layer=FellingSceneDefinition.Load().layers.First(l=>l.mutable&&l.blocksMovement);var owner=FellingSceneRuntime.FindOwner(z,layer.id);var p=Approach(z,owner);var actor=Actor(z,p.x,p.y);
            int dx=layer.anchorX-p.x,dy=layer.anchorY-p.y;
            Assert.IsFalse(MovementSystem.TryMove(actor,z,dx,dy));Assert.AreEqual((p.x,p.y),z.GetEntityPosition(actor));
            Assert.IsTrue(owner.GetPart<FellingScenePropPart>().TryClear(actor,z));Assert.IsTrue(MovementSystem.TryMove(actor,z,dx,dy));Assert.AreEqual((layer.anchorX,layer.anchorY),z.GetEntityPosition(actor));
        }
        [TestCase("remote")] [TestCase("wrong-zone")] [TestCase("detached")] [TestCase("fixed")] [TestCase("dead")]
        public void InvalidClearCannotMutateTheScene(string reason)
        {
            var z=Fresh();var layer=FellingSceneDefinition.Load().layers.First(l=>l.mutable!=(reason=="fixed"));var owner=FellingSceneRuntime.FindOwner(z,layer.id);
            var p=z.GetEntityPosition(owner);var actor=Actor(z,p.x,p.y);
            if(reason=="remote")z.MoveEntity(actor,79,24);
            if(reason=="detached")z.RemoveEntity(actor);
            if(reason=="dead")actor.GetStat("Hitpoints").Penalty=actor.GetStat("Hitpoints").BaseValue;
            Assert.IsFalse(owner.GetPart<FellingScenePropPart>().TryClear(actor,reason=="wrong-zone"?new Zone():z));Assert.IsTrue(FellingSceneRuntime.IsPresent(z,layer.id));
        }
        [Test] public void RepeatedClearCannotSucceedOrGrantAnythingTwice()
        {
            var z=Fresh();var layer=FellingSceneDefinition.Load().layers.First(l=>l.mutable);var owner=FellingSceneRuntime.FindOwner(z,layer.id);var p=Approach(z,owner);var actor=Actor(z,p.x,p.y);
            Assert.IsTrue(owner.GetPart<FellingScenePropPart>().TryClear(actor,z));Assert.IsFalse(owner.GetPart<FellingScenePropPart>().TryClear(actor,z));
        }
        [Test] public void MapEntryAndReturnUseExistingTravelAndPreserveClearedContent()
        {
            var manager=new OverworldZoneManager(factory,64);var map=manager.GetZone(WorldMap.WorldMapZoneID);var p=WorldMap.WorldCellToZoneCell(3,5);var actor=Actor(map,p.zoneX,p.zoneY);
            var down=WorldMapTraversal.Descend(actor,map,manager);Assert.IsTrue(down.Success);Assert.IsTrue(FellingSceneRuntime.IsActive(down.NewZone));
            var owner=FellingSceneRuntime.FindOwner(down.NewZone,FellingSceneDefinition.Load().layers.First(l=>l.mutable).id);var a=Approach(down.NewZone,owner);down.NewZone.MoveEntity(actor,a.x,a.y);
            Assert.IsTrue(owner.GetPart<FellingScenePropPart>().TryClear(actor,down.NewZone));var up=WorldMapTraversal.Ascend(actor,down.NewZone,manager);Assert.IsTrue(up.Success);
            var again=WorldMapTraversal.Descend(actor,up.NewZone,manager);Assert.IsTrue(again.Success);Assert.AreEqual((a.x,a.y),again.NewZone.GetEntityPosition(actor));Assert.IsFalse(FellingSceneRuntime.IsPresent(again.NewZone,owner.GetPart<FellingScenePropPart>().ComponentId));
        }
        [TestCase("Overworld.2.5.0",TransitionDirection.East,79,20,TransitionDirection.West)]
        [TestCase("Overworld.4.5.0",TransitionDirection.West,0,20,TransitionDirection.East)]
        [TestCase("Overworld.3.4.0",TransitionDirection.South,40,24,TransitionDirection.North)]
        [TestCase("Overworld.3.6.0",TransitionDirection.North,40,0,TransitionDirection.South)]
        public void NeighborEntryAndImmediateReturnRemainConnected(string id,TransitionDirection inward,int x,int y,TransitionDirection outward)
        {
            var m=new OverworldZoneManager(factory,64);var old=m.GetZone(id);var actor=Actor(old,x,y);var entry=ZoneTransitionSystem.TransitionPlayer(actor,old,inward,x,y,m,m.WorldMap);Assert.IsTrue(entry.Success);Assert.IsTrue(FellingSceneRuntime.IsActive(entry.NewZone));
            var back=ZoneTransitionSystem.TransitionPlayer(actor,entry.NewZone,outward,entry.NewPlayerX,entry.NewPlayerY,m,m.WorldMap);Assert.IsTrue(back.Success);Assert.AreEqual(id,back.NewZone.ZoneID);
        }
        [Test] public void LoadedActiveZonePreservesRemovalAndUnknownSavedOccupants()
        {
            var m=new OverworldZoneManager(factory,64);var z=m.GetZone(FellingSiteBuilder.ZoneID);var layer=FellingSceneDefinition.Load().layers.First(l=>l.mutable);var owner=FellingSceneRuntime.FindOwner(z,layer.id);var p=Approach(z,owner);var actor=Actor(z,p.x,p.y);
            Assert.IsTrue(owner.GetPart<FellingScenePropPart>().TryClear(actor,z));var item=factory.CreateEntity("Tepuibone");item.SetIntProperty("PersonalMark",37);z.AddEntity(item,40,20);m.SetActiveZone(z);var turns=new TurnManager();turns.AddEntity(actor);
            var state=GameSessionState.Capture("felling-layered","test",m,turns,actor);GameSessionState loaded;
            using(var s=new MemoryStream()){state.Save(new SaveWriter(s));s.Position=0;loaded=GameSessionState.Load(new SaveReader(s,factory));}
            var lz=loaded.ZoneManager.ActiveZone;Assert.IsTrue(FellingSceneRuntime.IsActive(lz));Assert.IsFalse(FellingSceneRuntime.IsPresent(lz,layer.id));Assert.IsNotNull(lz.GetEntityCell(loaded.Player));Assert.IsTrue(lz.GetAllEntities().Any(e=>e.ID==item.ID&&e.GetIntProperty("PersonalMark")==37));
        }
        [Test] public void LegacyUpgradePreservesActorsItemsAndTheMissingSeventh()
        {
            var z=new Zone(FellingSiteBuilder.ZoneID);var actor=Actor(z);var item=factory.CreateEntity("Tepuibone");z.AddEntity(item,40,20);
            z.AddEntity(factory.CreateEntity("FellingBarePosition"),40,5);z.AddEntity(factory.CreateEntity("TepuiStone"),40,20);
            Assert.IsTrue(FellingSceneRuntime.UpgradeCachedZone(z,factory));Assert.IsTrue(FellingSceneRuntime.IsActive(z));Assert.IsNotNull(z.GetEntityCell(actor));Assert.IsNotNull(z.GetEntityCell(item));Assert.IsFalse(z.GetAllEntities().Any(e=>e.HasPart<SeventhPositionPart>()));
            var ids=z.GetAllEntities().Select(e=>e.ID).OrderBy(x=>x).ToArray();Assert.IsTrue(FellingSceneRuntime.UpgradeCachedZone(z,factory));CollectionAssert.AreEqual(ids,z.GetAllEntities().Select(e=>e.ID).OrderBy(x=>x).ToArray());
        }
        [Test] public void NonFellingZoneIsNeverUpgraded()
        { var z=new Zone("Overworld.2.5.0");var actor=Actor(z);Assert.IsFalse(FellingSceneRuntime.UpgradeCachedZone(z,factory));Assert.AreEqual(1,z.EntityCount);Assert.AreSame(actor,z.GetAllEntities()[0]); }
    }
}
