using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Cross-wave player flows, deliberately combining generation,
    /// saved identity, live actions and terrain mutation rather than rechecking rows.</summary>
    public sealed class FellingW6AdversarialTests
    {
        private EntityFactory _factory, _oldHarvest;
        private NarrativeStatePart _oldNarrative;
        private Zone _oldZone;
        private Entity _world, _oldWorld;
        private readonly List<GameObject> _objects = new List<GameObject>();
        [SetUp] public void Setup()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
            _oldNarrative = NarrativeStatePart.Current; _oldZone = SettlementRuntime.ActiveZone; _oldWorld = TurnManager.World;
            _oldHarvest = HarvestablePart.Factory; HarvestablePart.Factory = _factory;
            MessageLog.Clear(); FactionManager.Initialize(); PlayerReputation.Set("CatacombFolk", 0); ConversationLoader.Reset();
            _world = new Entity(); _world.AddPart(new NarrativeStatePart());
            NarrativeStatePart.Current = _world.GetPart<NarrativeStatePart>();
        }
        [TearDown] public void Cleanup()
        {
            ConversationManager.EndConversation(); ConversationLoader.Reset(); FactionManager.Reset();
            NarrativeStatePart.Current = _oldNarrative; SettlementRuntime.ActiveZone = _oldZone;
            HarvestablePart.Factory = _oldHarvest; TurnManager.World = _oldWorld;
            foreach (var go in _objects) Object.DestroyImmediate(go); _objects.Clear();
        }
        private Entity Actor(Zone zone, int x = 10, int y = 10)
        {
            var actor = _factory.CreateEntity("Player");
            actor.GetPart<InventoryPart>().MaxWeight = 100;
            var hp = actor.GetStat("Hitpoints"); hp.Max = hp.BaseValue = 1000; hp.Penalty = 0;
            zone.AddEntity(actor, x, y); return actor;
        }
        private TurnManager Clock(Entity actor, Zone zone)
        {
            SettlementRuntime.ActiveZone = zone;
            TurnManager.World = _world; var turns = new TurnManager(); turns.AddEntity(actor); turns.ProcessUntilPlayerTurn(); return turns;
        }
        private GameSessionState SaveLoad(OverworldZoneManager manager, Zone zone, Entity actor, TurnManager turns = null)
        {
            manager.SetActiveZone(zone); turns = turns ?? Clock(actor, zone);
            var state = GameSessionState.Capture("w67-adversarial", "test", manager, turns, actor, world: _world);
            using (var stream = new MemoryStream())
            {
                state.Save(new SaveWriter(stream)); stream.Position = 0;
                var loaded = GameSessionState.Load(new SaveReader(stream, _factory));
                _world = loaded.World; NarrativeStatePart.Current = _world.GetPart<NarrativeStatePart>();
                TurnManager.World = _world; SettlementRuntime.ActiveZone = loaded.ZoneManager.ActiveZone;
                return loaded;
            }
        }
        private static Entity Marker(Zone map, int x, int y)
        { var p = WorldMap.WorldCellToZoneCell(x,y); return map.GetCell(p.zoneX,p.zoneY).Objects.Single(e=>e.HasPart<WorldMapCellPart>()); }
        [TestCase(2,4,false)] [TestCase(2,4,true)] [TestCase(4,6,false)] [TestCase(4,6,true)]
        public void OldMapMigrationThenActualDiscoveryPreservesIdentityAndCachedGround(int x,int y,bool blocked)
        {
            var m = new OverworldZoneManager(_factory,67);
            foreach (var p in new[]{(2,4),(4,6),(3,5)}) m.WorldMap.SetPOI(p.Item1,p.Item2,null);
            var map = m.GetZone("WorldMap"); var marker = Marker(map,x,y);
            marker.ID = $"saved-map-marker:{x},{y}"; // Test preservation of an existing identity; missing IDs repair on body load.
            marker.SetIntProperty("PlayerMark",17);
            var ground = m.GetZone($"Overworld.{x}.{y}.0"); var stone = _factory.CreateEntity("Tepuibone"); ground.AddEntity(stone,40,12);
            var actor = Actor(ground,40,12); var load = SaveLoad(m,ground,actor);
            m=load.ZoneManager; ground=m.ActiveZone; actor=load.Player; map=m.GetZone("WorldMap");
            Assert.AreEqual(marker.ID,Marker(map,x,y).ID); Assert.AreEqual(17,Marker(map,x,y).GetIntProperty("PlayerMark"));
            Assert.IsTrue(ground.GetAllEntities().Any(e=>e.ID==stone.ID));
            Assert.AreEqual("the Felling-Site",Marker(map,3,5).GetPart<RenderPart>().DisplayName);
            Assert.AreNotEqual("Stillleaf",Marker(map,2,4).GetPart<RenderPart>().DisplayName);
            Assert.AreNotEqual("Olderdeep",Marker(map,4,6).GetPart<RenderPart>().DisplayName);
            var cell = WorldMap.WorldCellToZoneCell(x,y); var wall = new Entity(); wall.SetTag("Solid");
            if(blocked) map.AddEntity(wall,cell.zoneX,cell.zoneY);
            var result=WorldMapTraversal.Ascend(actor,ground,m);
            Assert.AreEqual(!blocked,result.Success); Assert.AreEqual(!blocked,m.WorldMap.IsVisited(x,y));
            if(blocked)
            { Assert.IsNotNull(ground.GetEntityCell(actor)); map.RemoveEntity(wall); Assert.IsTrue(WorldMapTraversal.Ascend(actor,ground,m).Success); }
            Assert.AreEqual(x==2?"Stillleaf":"Olderdeep",Marker(map,x,y).GetPart<RenderPart>().DisplayName);
            Assert.IsFalse(m.WorldMap.IsVisited(x==2?4:2,x==2?6:4));
        }
        private sealed class TwoChips : System.Random
        { public override int Next(int minValue,int maxValue)=>maxValue-1; }
        private static void Action(Entity target,Entity actor,Zone zone,string command)
        {
            var e=GameEvent.New("InventoryAction"); e.SetParameter("Actor",actor); e.SetParameter("Zone",zone);
            e.SetParameter("Command",command); e.SetParameter("Random",new TwoChips()); target.FireEventAndRelease(e);
        }
        [TestCase(12,false,true)] [TestCase(12,true,true)] [TestCase(24,false,true)] [TestCase(24,true,true)]
        [TestCase(12,false,false)] [TestCase(12,true,false)]
        public void RealHarvestOfferingAndSavedDreamPreserveUnitsAndOneTimeFacts(int capacity,bool saveBeforeSleep,bool quiet)
        {
            var m=new OverworldZoneManager(_factory,67); var slope=m.GetZone("Overworld.2.2.0");
            var vein=slope.GetAllEntities().First(e=>e.BlueprintName=="TepuiboneVein"); var vc=slope.GetEntityCell(vein);
            var actor=Actor(slope,vc.X-1,vc.Y); actor.GetPart<InventoryPart>().MaxWeight=capacity;
            Action(vein,actor,slope,"Harvest"); Assert.IsNull(slope.GetEntityCell(vein));
            var dropped=slope.GetAllEntities().Where(e=>e.BlueprintName=="Tepuibone").Select(e=>e.ID).ToArray();
            Assert.AreEqual(capacity==12?1:0,dropped.Length);
            var home=m.GetZone("Overworld.4.6.2"); var tender=home.GetAllEntities().Single(e=>e.BlueprintName=="FoundingPlaqueTender");
            var tc=home.GetEntityCell(tender); slope.RemoveEntity(actor); home.AddEntity(actor,tc.X-1,tc.Y); var turns=Clock(actor,home);
            Assert.IsTrue(ConversationManager.StartConversation(tender,actor));
            int choice=ConversationManager.VisibleChoices.ToList().FindIndex(c=>c.Actions!=null&&c.Actions.Any(a=>a.Key=="OfferFoundingStone"));
            Assert.GreaterOrEqual(choice,0); ConversationManager.SelectChoice(choice); ConversationManager.EndConversation();
            Assert.AreEqual(50,PlayerReputation.Get("CatacombFolk"));
            Assert.AreEqual(capacity/12-1,actor.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName=="Tepuibone").Sum(e=>e.GetPart<StackerPart>().StackCount));
            if(saveBeforeSleep)
            { var load=SaveLoad(m,home,actor,turns); m=load.ZoneManager; home=m.ActiveZone; actor=load.Player; turns=load.TurnManager; }
            var plume=home.GetAllEntities().First(e=>e.BlueprintName=="FoundingPlume"); var pc=home.GetEntityCell(plume);
            home.RemoveEntity(actor); home.AddEntity(actor,pc.X,pc.Y); int before=turns.TickCount;
            // Generated cave enemies outside the chamber may legitimately block rest through a wall.
            // Stage the safe/unsafe comparison explicitly; do not weaken the production safety gate.
            foreach(var enemy in home.GetAllEntities().Where(e=>e.HasPart<BrainPart>()&&FactionManager.IsHostile(e,actor)).ToArray()) home.RemoveEntity(enemy);
            if(!quiet) home.AddEntity(_factory.CreateEntity("Snapjaw"),pc.X-1,pc.Y);
            Action(plume,actor,home,FoundingPlumePart.SleepCommand);
            if(!quiet)
            {Assert.AreEqual(before,turns.TickCount); Assert.AreEqual(0,NarrativeStatePart.Current.GetFact("RootedMet")); Assert.IsFalse(FoundingPlumePart.HasBloom(actor)); return;}
            Assert.AreEqual(before+60,turns.TickCount,string.Join(" | ",MessageLog.GetMessages())); Assert.AreEqual(1,NarrativeStatePart.Current.GetFact("RootedMet"));
            var final=SaveLoad(m,home,actor,turns); home=final.ZoneManager.ActiveZone; actor=final.Player;
            Assert.AreEqual(1,NarrativeStatePart.Current.EventLog.Count(s=>s=="RootedMet"));
            Assert.IsTrue(FoundingPlumePart.HasBloom(actor));
            var replacement=_factory.CreateEntity("FoundingPlaqueTender"); var ac=home.GetEntityCell(actor); home.AddEntity(replacement,ac.X+1,ac.Y);
            ConversationActions.Execute("OfferFoundingStone",replacement,actor,"");
            Assert.AreEqual(50,PlayerReputation.Get("CatacombFolk"));
            CollectionAssert.AreEquivalent(dropped,final.ZoneManager.GetZone(slope.ZoneID).GetAllEntities().Where(e=>e.BlueprintName=="Tepuibone").Select(e=>e.ID));
        }
        [TestCase("same")] [TestCase("adjacent")] [TestCase("none")]
        public void CachedSleepMenuRevalidatesPlumeAfterBarrenGroundChanges(string placement)
        {
            var zone=new Zone("Overworld.3.5.0"); var actor=Actor(zone); var plume=_factory.CreateEntity("FoundingPlume"); zone.AddEntity(plume,10,10);
            PlayerReputation.Set("CatacombFolk",50); var turns=Clock(actor,zone); actor.GetStat("Hitpoints").BaseValue=990; int before=turns.TickCount;
            var go=new GameObject("W67Input"); _objects.Add(go); var input=go.AddComponent<InputHandler>();
            var menu=new GameObject("W67Menu"); _objects.Add(menu); input.WorldActionMenuUI=menu.AddComponent<WorldActionMenuUI>();
            input.PlayerEntity=actor; input.CurrentZone=zone; input.TurnManager=turns;
            Call(input,"InteractInDirection",0,0);
            var pick=Actions(input).Single(a=>a.Command==WorldInteractionSystem.PickTargetCommandPrefix+plume.ID);
            Call(input,"ExecuteWorldActionSelection",pick,actor,zone.GetCell(10,10),false);
            var sleep=Actions(input).Single(a=>a.Command==FoundingPlumePart.SleepCommand);
            if(placement!="none") zone.AddEntity(_factory.CreateEntity("FellingBarePosition"),placement=="same"?10:11,10);
            Call(input,"ExecuteWorldActionSelection",sleep,plume,zone.GetCell(10,10),false);
            bool success=placement!="same";
            Assert.AreEqual(before+(success?60:0),turns.TickCount); Assert.AreEqual(success?1000:990,actor.GetStatValue("Hitpoints"));
            Assert.AreEqual(success?1:0,NarrativeStatePart.Current.GetFact("RootedMet"));
            Assert.AreEqual("Normal",typeof(InputHandler).GetField("_inputState",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(input).ToString());
        }
        [TestCase(false,false)] [TestCase(false,true)] [TestCase(true,false)] [TestCase(true,true)]
        public void BarrenCoverRemovalKeepsSentinelIndexedScheduledAndOutOfMissingCover(bool barren,bool save)
        {
            var m=new OverworldZoneManager(_factory,67); var z=new Zone("Overworld.3.3.0");
            var actor=Actor(z,9,10); var sentinel=_factory.CreateEntity("BrocchiniaSentinel"); z.AddEntity(sentinel,10,10);
            z.AddEntity(_factory.CreateEntity("TankBrocchinia"),11,10); var turns=Clock(actor,z); turns.AddEntity(sentinel); sentinel.GetPart<BrainPart>().CurrentZone=z; sentinel.GetPart<BrainPart>().Rng=new System.Random(4);
            if(barren) z.AddEntity(_factory.CreateEntity("FellingBarePosition"),11,10);
            string id=sentinel.ID;
            if(save) {var load=SaveLoad(m,z,actor,turns); m=load.ZoneManager; z=m.ActiveZone; actor=load.Player; turns=load.TurnManager; sentinel=z.GetAllEntities().Single(e=>e.ID==id);}
            Assert.IsTrue(z.GetEntitiesWithTag("Creature").Contains(sentinel)); Assert.IsTrue(turns.IsRegistered(sentinel));
            var brain=sentinel.GetPart<BrainPart>(); Assert.AreSame(z,brain.CurrentZone); brain.Rng=new System.Random(4);
            sentinel.FireEventAndRelease(GameEvent.New("TakeTurn"));
            Assert.AreEqual(barren?(10,10):(11,10),z.GetEntityPosition(sentinel));
            Assert.AreEqual(!barren,StumpFaunaHabitat.Contains(z.GetCell(11,10),"TankBrocchinia"));
        }
        private (OverworldZoneManager,Zone) Passage()
        {
            var site=SinkholeSites.All.First(s=>s.Name=="Ginmere");
            for(int seed=1;seed<=32;seed++)
            {var m=new OverworldZoneManager(_factory,seed); var z=m.GetZone($"Overworld.{site.X}.{site.Y}.2"); if(z.GetAllEntities().Any(e=>e.HasPart<WaterPassagePart>()))return(m,z);}
            Assert.Fail("No actual passage seed"); return default;
        }
        [TestCase(false,false)] [TestCase(false,true)] [TestCase(true,false)] [TestCase(true,true)]
        public void PassageUnloadAndSecondSaveDoNotKeepAnOrphanReverseIndex(bool saveFirst,bool removeIndicator)
        {
            var (m,z)=Passage(); var marker=z.GetAllEntities().Single(e=>e.HasPart<WaterPassagePart>()); var pc=z.GetEntityCell(marker);
            var actor=Actor(z,pc.X,pc.Y); string markerID=marker.ID; string surface=WorldMap.GetZoneAbove(WorldMap.GetZoneAbove(z.ZoneID));
            if(removeIndicator) foreach(var e in z.GetAllEntities().Where(e=>e.BlueprintName=="HelmwoodFrog").ToArray()) z.RemoveEntity(e);
            if(saveFirst) {var load=SaveLoad(m,z,actor); m=load.ZoneManager; z=m.ActiveZone; actor=load.Player;}
            m.UnloadZone(surface);
            Assert.IsFalse(m.GetConnections(z.ZoneID).Any(c=>c.SourceZoneID==surface),"opposite index must not keep unloaded source-owned connections");
            var second=SaveLoad(m,z,actor); m=second.ZoneManager; z=m.ActiveZone; actor=second.Player;
            Assert.IsFalse(m.GetConnections(z.ZoneID).Any(c=>c.SourceZoneID==surface));
            for(int i=0;i<2;i++)
            {
                var result=HelmwoodPassages.TryTravel(actor,z,false,m); Assert.IsTrue(result.Success,result.ErrorReason);
                Assert.AreEqual(1,m.GetConnections(z.ZoneID).Count(c=>c.Type=="WaterPassage"));
                Assert.AreEqual(1,m.GetConnections(surface).Count(c=>c.Type=="WaterPassage"));
                var back=HelmwoodPassages.TryTravel(actor,result.NewZone,true,m); Assert.IsTrue(back.Success,back.ErrorReason);
                Assert.AreEqual(markerID,z.GetAllEntities().Single(e=>e.HasPart<WaterPassagePart>()).ID);
                if(removeIndicator) Assert.IsFalse(z.GetAllEntities().Any(e=>e.BlueprintName=="HelmwoodFrog"));
                m.UnloadZone(surface);
            }
        }
        [TestCase(67,false)] [TestCase(67,true)] [TestCase(68,false)] [TestCase(68,true)]
        public void CrossSiteGenerationOrderAndSavedCacheKeepLocalContentIsolated(int seed,bool reverse)
        {
            var m=new OverworldZoneManager(_factory,seed); var ids=new[]{"Overworld.4.6.2","Overworld.3.5.0","Overworld.2.4.2","Overworld.3.3.0"};
            foreach(string id in reverse?ids.Reverse():ids) m.GetZone(id);
            var z=m.GetZone(ids[1]); var actor=Actor(z,40,12); var load=SaveLoad(m,z,actor); m=load.ZoneManager;
            foreach(string id in ids)
            {
                z=m.GetZone(id); var entities=z.GetAllEntities();
                Assert.AreEqual(id==ids[0]?1:0,entities.Count(e=>e.BlueprintName=="TheRooted"));
                Assert.AreEqual(id==ids[2]?1:0,entities.Count(e=>e.BlueprintName=="SealedLibraryDoor"));
                Assert.AreEqual(id==ids[1]?1:0,entities.Count(e=>e.BlueprintName=="SeventhPosition"));
                if(id==ids[1])
                {
                    var wildlife=entities.Where(e=>e.HasTag("Creature")&&e!=load.Player).ToArray();
                    Assert.AreEqual(3,wildlife.Length);
                    Assert.IsTrue(wildlife.All(e=>e.HasTag(FellingScenePopulation.FaunaTag)&&e.GetPart<BrainPart>()?.Passive==true));
                }
                if(id==ids[2]) Assert.IsFalse(entities.Any(e=>e.HasPart<WaterPassagePart>()||e.BlueprintName=="PricklebrowNest"));
            }
        }
        [TestCase(2,4)] [TestCase(4,6)]
        public void EnteringBelowTheSiteBeforeItsFloorKeepsActualReturnStairs(int x,int y)
        {
            var m=new OverworldZoneManager(_factory,67); var deep=m.GetZone($"Overworld.{x}.{y}.3");
            var floor=m.GetZone($"Overworld.{x}.{y}.2");
            var edge=m.GetConnections(deep.ZoneID).Single(c=>c.SourceZoneID==floor.ZoneID&&c.Type=="StairsDown");
            Assert.IsTrue(deep.GetCell(edge.TargetX,edge.TargetY).Objects.Any(e=>e.HasPart<StairsUpPart>()),"actual upstairs must exist in the already generated deeper floor");
        }
        [TestCase(3)] [TestCase(12)] [TestCase(1000)]
        public void DirectDeepGenerationRegistersOnlyItsImmediateParentWithoutBuildingIt(int depth)
        {
            var m=new OverworldZoneManager(_factory,67); string id=$"Overworld.2.4.{depth}";
            var z=m.GetZone(id); var up=z.GetAllEntities().Single(e=>e.HasPart<StairsUpPart>());
            Assert.AreEqual(1,m.CachedZoneCount,"no recursive column generation");
            var edge=m.GetConnections(id).Single(c=>c.TargetZoneID==id&&c.Type=="StairsDown");
            Assert.AreEqual(WorldMap.GetZoneAbove(id),edge.SourceZoneID);
            Assert.AreEqual((edge.TargetX,edge.TargetY),z.GetEntityPosition(up));
            Assert.AreSame(z,m.GetZone(id)); Assert.AreEqual(1,m.CachedZoneCount);
        }
        [TestCase(false)] [TestCase(true)]
        public void LateParentBuildKeepsCachedChildIdentityAndMovesOnlyItsConflictingEndpoint(bool save)
        {
            var m=new OverworldZoneManager(_factory,67); var child=new Zone("Overworld.2.4.3");
            var up=_factory.CreateEntity("StairsUp"); child.AddEntity(up,10,10); var actor=Actor(child,11,10); m.SetActiveZone(child);
            m.RegisterConnection(new ZoneConnection{SourceZoneID="Overworld.2.4.2",SourceX=10,SourceY=10,TargetZoneID=child.ZoneID,TargetX=10,TargetY=10,Type="StairsDown"});
            if(save){var load=SaveLoad(m,child,actor); m=load.ZoneManager; child=m.ActiveZone; actor=load.Player;}
            var ids=child.GetAllEntities().Select(e=>e.ID).ToArray();
            var parent=new Zone("Overworld.2.4.2"); parent.AddEntity(_factory.CreateEntity("StairsUp"),10,10);
            Assert.IsTrue(new StairsDownBuilder(m).BuildZone(parent,_factory,new System.Random(67)));
            var down=parent.GetAllEntities().Single(e=>e.HasPart<StairsDownPart>());
            var edge=m.GetConnections(child.ZoneID).Single(c=>c.SourceZoneID==parent.ZoneID&&c.Type=="StairsDown");
            Assert.AreEqual((10,10),(edge.TargetX,edge.TargetY));
            Assert.AreEqual(parent.GetEntityPosition(down),(edge.SourceX,edge.SourceY)); Assert.AreNotEqual((10,10),parent.GetEntityPosition(down));
            Assert.AreEqual(1,m.GetConnections(parent.ZoneID).Count(c=>c.TargetZoneID==child.ZoneID));
            CollectionAssert.AreEquivalent(ids,child.GetAllEntities().Select(e=>e.ID));
        }
        [TestCase(false)] [TestCase(true)]
        public void CachedNeighborWithoutCounterpartIsPreservedByFreshStairBuilder(bool parentCached)
        {
            var m=new OverworldZoneManager(_factory,67); var old=new Zone(parentCached?"Overworld.2.4.3":"Overworld.2.4.4");
            var mark=_factory.CreateEntity("Tepuibone"); old.AddEntity(mark,10,10); m.SetActiveZone(old);
            var fresh=new Zone(parentCached?"Overworld.2.4.4":"Overworld.2.4.3");
            if(parentCached) new StairsUpBuilder(m).BuildZone(fresh,_factory,new System.Random(67));
            else new StairsDownBuilder(m).BuildZone(fresh,_factory,new System.Random(67));
            Assert.AreEqual(1,old.GetAllEntities().Count); Assert.AreSame(mark,old.GetAllEntities()[0]);
            Assert.IsFalse(fresh.GetAllEntities().Any(e=>e.HasPart<StairsUpPart>()||e.HasPart<StairsDownPart>()));
            Assert.AreEqual(0,m.GetConnections(old.ZoneID).Count);
        }
        [TestCase("StairsUp")] [TestCase("StairsDown")]
        public void MissingStairBlueprintCannotRegisterAPhantomDeferredPair(string missing)
        {
            var m=new OverworldZoneManager(_factory,67); var z=new Zone("Overworld.2.4.3"); _factory.Blueprints.Remove(missing);
            new StairsUpBuilder(m).BuildZone(z,_factory,new System.Random(67));
            Assert.AreEqual(0,m.GetConnections(z.ZoneID).Count); Assert.AreEqual(0,z.GetAllEntities().Count);
        }
        [TestCase(true,false)] [TestCase(true,true)] [TestCase(false,false)] [TestCase(false,true)]
        public void ActualStairRefusesRemovedReturnMarkerWithoutTouchingEitherZone(bool down,bool returned)
        {
            var m=new OverworldZoneManager(_factory,67); var source=new Zone(down?"Overworld.2.4.2":"Overworld.2.4.3");
            var target=new Zone(down?"Overworld.2.4.3":"Overworld.2.4.2"); m.CachedZones[source.ZoneID]=source; m.CachedZones[target.ZoneID]=target;
            var actor=Actor(source); source.AddEntity(_factory.CreateEntity(down?"StairsDown":"StairsUp"),10,10);
            if(returned)target.AddEntity(_factory.CreateEntity(down?"StairsUp":"StairsDown"),10,10);
            int v=source.EntityVersion;
            var result=ZoneTransitionSystem.TransitionPlayerVertical(actor,source,down,10,10,m);
            Assert.AreEqual(returned,result.Success);
            Assert.IsNotNull((returned?target:source).GetEntityCell(actor)); Assert.IsNull((returned?source:target).GetEntityCell(actor));
            if(!returned)Assert.AreEqual(v,source.EntityVersion);
        }
        [TestCase(false,false)] [TestCase(false,true)] [TestCase(true,false)] [TestCase(true,true)]
        public void SavedDeepFirstPairAndParentRegenerationRespectTheSurvivingChild(bool save,bool removed)
        {
            var m=new OverworldZoneManager(_factory,67); var child=m.GetZone("Overworld.2.4.17");
            var up=child.GetAllEntities().Single(e=>e.HasPart<StairsUpPart>()); var actor=Actor(child,5,5); string upID=up.ID;
            if(save){var load=SaveLoad(m,child,actor); m=load.ZoneManager; child=m.ActiveZone; actor=load.Player; up=child.GetAllEntities().Single(e=>e.ID==upID);}
            string parentID=WorldMap.GetZoneAbove(child.ZoneID); m.GetZone(parentID); m.UnloadZone(parentID);
            if(removed)child.RemoveEntity(up);
            var ids=child.GetAllEntities().Select(e=>e.ID).ToArray(); var parent=m.GetZone(parentID);
            CollectionAssert.AreEquivalent(ids,child.GetAllEntities().Select(e=>e.ID));
            Assert.AreEqual(removed?0:1,parent.GetAllEntities().Count(e=>e.HasPart<StairsDownPart>()));
            var edges=m.GetConnections(child.ZoneID).Where(c=>c.SourceZoneID==parentID&&c.Type=="StairsDown").ToList();
            Assert.AreEqual(removed?0:1,edges.Count);
            if(!removed)Assert.AreEqual(child.GetEntityPosition(up),(edges[0].TargetX,edges[0].TargetY));
        }
        [TestCase(false)] [TestCase(true)]
        public void CrossSiteGenerationAndClockOnlyRestTimeDoNotExpireOwnedConfusion(bool onSeventh)
        {
            var m=new OverworldZoneManager(_factory,67); var z=m.GetZone("Overworld.3.5.0");
            var marker=z.GetAllEntities().Single(e=>e.HasPart<SeventhPositionPart>()); var mc=z.GetEntityCell(marker);
            var actor=Actor(z,mc.X+(onSeventh?0:1),mc.Y); var turns=Clock(actor,z); m.SetActiveZone(z);
            turns.EndTurn(actor,z); turns.ProcessUntilPlayerTurn();
            Assert.AreEqual(onSeventh,actor.HasEffect<ConfusedEffect>());
            actor.SetIntProperty(FoundingPlumePart.BloomExpiryProperty,turns.TickCount+10); int tick=turns.TickCount;
            foreach(string id in new[]{"Overworld.2.4.3","Overworld.4.6.2","Overworld.3.3.0"})m.GetZone(id);
            Assert.AreSame(z,m.ActiveZone); Assert.AreEqual(tick,turns.TickCount); Assert.AreEqual(onSeventh,actor.HasEffect<ConfusedEffect>());
            var load=SaveLoad(m,z,actor,turns); actor=load.Player; turns=load.TurnManager;
            Assert.IsTrue(FoundingPlumePart.HasBloom(actor)); Assert.AreEqual(onSeventh,actor.HasEffect<ConfusedEffect>());
            turns.AdvanceClock(10);
            Assert.IsFalse(FoundingPlumePart.HasBloom(actor)); Assert.AreEqual(onSeventh,actor.HasEffect<ConfusedEffect>(),"world-clock travel/rest is not an owner status turn");
        }
        private static List<InventoryAction> Actions(InputHandler input)=>(List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(input.WorldActionMenuUI);
        private static void Call(object obj,string name,params object[] args)=>obj.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(obj,args);
    }
}
