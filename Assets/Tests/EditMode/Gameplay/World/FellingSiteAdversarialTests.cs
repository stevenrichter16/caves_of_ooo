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
    // Hypotheses span placement atomicity, saved membership, effect timing,
    // source isolation and derived map data rather than mirroring the builder.
    public sealed class FellingSiteAdversarialTests
    {
        private EntityFactory _factory;
        [SetUp] public void Setup()
        {
            _factory=new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
        }
        private Entity Place(Zone z,string bp,int x=10,int y=10)
        { var e=_factory.CreateEntity(bp);Assert.IsNotNull(e,bp);Assert.IsTrue(z.AddEntity(e,x,y));return e; }
        private static HashSet<(int x,int y)> Flood(Zone z)
        {
            var seen=new HashSet<(int x,int y)>{(40,12)};var q=new Queue<(int x,int y)>();q.Enqueue((40,12));
            while(q.Count>0)
            {
                var p=q.Dequeue();foreach(var d in new[]{(0,-1),(0,1),(-1,0),(1,0)})
                {var n=(p.x+d.Item1,p.y+d.Item2);var c=z.GetCell(n.Item1,n.Item2);if(c!=null&&!c.BlocksMovement()&&seen.Add(n))q.Enqueue(n);}
            }
            return seen;
        }
        [TestCase(3)] [TestCase(61)] [TestCase(987)]
        public void Adversarial_EveryPerimeterCellAndPositionIsReachable(int seed)
        {
            var z=new OverworldZoneManager(_factory,seed).GetZone(FellingSiteBuilder.ZoneID);var seen=Flood(z);
            for(int x=0;x<Zone.Width;x++){Assert.IsTrue(seen.Contains((x,0)));Assert.IsTrue(seen.Contains((x,Zone.Height-1)));}
            for(int y=0;y<Zone.Height;y++){Assert.IsTrue(seen.Contains((0,y)));Assert.IsTrue(seen.Contains((Zone.Width-1,y)));}
            var positions=z.GetAllEntities().Where(e=>e.BlueprintName=="SeventhPosition"||e.BlueprintName=="FellingBarePosition").ToList();
            Assert.AreEqual(7,positions.Select(z.GetEntityPosition).Distinct().Count());
            Assert.IsTrue(positions.All(e=>seen.Contains(z.GetEntityPosition(e))));
        }
        [TestCase("Overworld.2.5.0",TransitionDirection.East,79,12)]
        [TestCase("Overworld.4.5.0",TransitionDirection.West,0,12)]
        [TestCase("Overworld.3.4.0",TransitionDirection.South,40,24)]
        [TestCase("Overworld.3.6.0",TransitionDirection.North,40,0)]
        public void Adversarial_ActualNeighborTransitionsReachTheSite(string id,TransitionDirection direction,int x,int y)
        {
            var m=new OverworldZoneManager(_factory,64);var old=m.GetZone(id);var actor=Place(old,"Player",x,y);
            var result=ZoneTransitionSystem.TransitionPlayer(actor,old,direction,x,y,m,m.WorldMap);
            Assert.IsTrue(result.Success);Assert.IsNull(old.GetEntityCell(actor));
            Assert.IsNotNull(m.GetZone(FellingSiteBuilder.ZoneID).GetEntityCell(actor));
        }
        [TestCase("TepuiStone")] [TestCase("FellingScar")] [TestCase("FellingBarePosition")] [TestCase("SeventhPosition")]
        public void Adversarial_MissingContentCannotClearAnExistingZone(string missing)
        {
            var z=new Zone();var kept=Place(z,"StoneFloor");_factory.Blueprints.Remove(missing);
            Assert.IsFalse(new FellingSiteBuilder().BuildZone(z,_factory,new System.Random(1)));
            CollectionAssert.AreEqual(new[]{kept},z.GetAllEntities());
        }
        [Test] public void Adversarial_IdempotencePreservesExistingEntityIdentities()
        {
            var z=new Zone();var b=new FellingSiteBuilder();Assert.IsTrue(b.BuildZone(z,_factory,new System.Random(1)));
            var before=z.GetAllEntities();Assert.IsTrue(b.BuildZone(z,_factory,new System.Random(2)));
            CollectionAssert.AreEquivalent(before,z.GetAllEntities());
        }
        [Test] public void Adversarial_RoutingUsesTypeAndNeverDisplayName()
        {
            var m=new OverworldZoneManager(_factory,64);m.WorldMap.GetPOI(3,5).Name="Renamed circle";
            Assert.AreEqual(1,m.GetZone(FellingSiteBuilder.ZoneID).GetAllEntities().Count(e=>e.HasPart<SeventhPositionPart>()));
            m=new OverworldZoneManager(_factory,64);m.WorldMap.SetPOI(3,5,new PointOfInterest((POIType)999,FellingSiteBuilder.SiteName));
            Assert.IsFalse(m.GetZone(FellingSiteBuilder.ZoneID).GetAllEntities().Any(e=>e.HasPart<SeventhPositionPart>()));
        }
        [Test] public void Adversarial_SaveOrdinalsRemainAppendOnly()
        {
            CollectionAssert.AreEqual(new[]{0,1,2,3,4,5},new[]{(int)POIType.Village,(int)POIType.Lair,(int)POIType.MerchantCamp,(int)POIType.RiverChunk,(int)POIType.Sinkhole,(int)POIType.FellingSite});
        }
        [Test] public void Adversarial_AllRootedFloraRefusesButFoodFurnitureAndAnimalsRemain()
        {
            var z=new Zone();Place(z,"FellingBarePosition");
            foreach(string bp in "CandyCarrotCrop EmberwheatCrop Grass VineWall Tree Cactus MendleafPlant Bush Reeds BerryBush MushroomRing Hedge CropRow CharmFlowers FlowerField Saltbriar Greatdew MycelialColumn FruitingBody HearthPatch DroseraRing PebbleSundewThreshold TankBrocchinia BloomingFruitingBody GroveRedGrowth WineLeafSundew FoundingPlume DryBrush DeadTree SubstrateVault ChoirNode".Split(' '))
            {
                var e=_factory.CreateEntity(bp);Assert.IsTrue(e.HasTag("Vegetation"),bp);
                Assert.IsFalse(z.AddEntity(e,10,10),bp);Assert.IsTrue(z.AddEntity(e,11,10),bp);
            }
            foreach(string bp in new[]{"Player","CandyCarrotSeed","Tepuibone","NicheHome","GroveSign","TheRooted","HollowLog"})
                Assert.IsTrue(z.AddEntity(_factory.CreateEntity(bp),10,10),bp);
        }
        [Test] public void Adversarial_RejectedVegetationMovePreservesSourceAndIndexes()
        {
            var z=new Zone();Place(z,"FellingBarePosition");var tree=Place(z,"Tree",11,10);
            int version=z.EntityVersion;int count=z.EntityCount;
            Assert.IsFalse(z.AddEntity(tree,10,10));Assert.AreEqual(version,z.EntityVersion);Assert.AreEqual(count,z.EntityCount);Assert.AreEqual((11,10),z.GetEntityPosition(tree));
            Assert.IsTrue(z.GetCell(11,10).Objects.Contains(tree));Assert.IsFalse(z.GetCell(10,10).Objects.Contains(tree));
            Assert.IsTrue(z.GetEntitiesWithTag("Vegetation").Contains(tree));
        }
        [Test] public void Adversarial_InstallingBarrenGroundClearsPlantsButKeepsOtherObjects()
        {
            var z=new Zone();var tree=Place(z,"Tree");var crop=Place(z,"CandyCarrotCrop");var actor=Place(z,"Player");var item=Place(z,"Tepuibone");
            Place(z,"FellingBarePosition");Assert.IsNull(z.GetEntityCell(tree));Assert.IsNull(z.GetEntityCell(crop));
            Assert.IsNotNull(z.GetEntityCell(actor));Assert.IsNotNull(z.GetEntityCell(item));Assert.AreEqual(0,z.GetEntitiesWithTag("Vegetation").Count);
        }
        private Entity TryBuilderPlace(Zone z,string bp,int x,int y)
        {
            var type=typeof(Zone).Assembly.GetType("CavesOfOoo.Core.BuilderSpawn",true);
            return (Entity)type.GetMethod("TryPlace").Invoke(null,new object[]{z,_factory,bp,x,y});
        }
        [Test] public void Adversarial_BuilderPlacementReportsRefusalRatherThanOrphanSuccess()
        {
            var z=new Zone();Place(z,"FellingBarePosition");
            Assert.IsNull(TryBuilderPlace(z,"FlowerField",10,10));
            Assert.IsNotNull(TryBuilderPlace(z,"FlowerField",11,10));
            Assert.IsNull(TryBuilderPlace(z,"FlowerField",-1,10));
        }
        [Test] public void Adversarial_FarmPlotConversionCannotEraseABarrenClaim()
        {
            var z=new Zone();var marker=new Entity();marker.SetTag("Barren");z.AddEntity(marker,10,10);var stone=Place(z,"StoneFloor");
            Assert.Greater(FarmPlotSeeder.EnsurePlantablePlot(z,10,10,_factory),0);
            Assert.IsNotNull(z.GetEntityCell(stone));Assert.IsNotNull(z.GetEntityCell(marker));
            Assert.IsFalse(z.GetCell(10,10).Objects.Any(BarrenGroundRules.IsVegetation));
        }
        private (Zone z,Entity actor,Entity point,TurnManager turns) Exposure()
        {
            var z=new Zone();var actor=Place(z,"Player");var point=Place(z,"SeventhPosition");
            var t=new TurnManager();t.AddEntity(actor);t.ProcessUntilPlayerTurn();return(z,actor,point,t);
        }
        [Test] public void Adversarial_DuplicateMarkersStillApplyOncePerAction()
        {
            var f=Exposure();Place(f.z,"SeventhPosition");int dv=f.actor.GetStatValue("DV");
            for(int i=0;i<8;i++){f.turns.EndTurn(f.actor,f.z);Assert.AreEqual(dv-2,f.actor.GetStatValue("DV"));}
        }
        [Test] public void Adversarial_ExistingConfusionKeepsItsDurationAndIdentity()
        {
            var f=Exposure();var effects=f.actor.GetPart<StatusEffectsPart>();if(effects==null){effects=new StatusEffectsPart();f.actor.AddPart(effects);}
            var existing=new ConfusedEffect(10);effects.ApplyEffect(existing);existing.JustApplied=false;
            f.turns.EndTurn(f.actor,f.z);Assert.AreSame(existing,effects.GetEffect<ConfusedEffect>());Assert.AreEqual(9,existing.Duration);
        }
        private sealed class RefuseEffectsPart:Part
        {public override bool HandleEvent(GameEvent e)=>e.ID!="BeforeApplyEffect";}
        [Test] public void Adversarial_ExposureRespectsNormalEffectVeto()
        {
            var f=Exposure();f.actor.AddPart(new RefuseEffectsPart());int dv=f.actor.GetStatValue("DV");f.turns.EndTurn(f.actor,f.z);
            Assert.IsFalse(f.actor.GetPart<StatusEffectsPart>()?.HasEffect<ConfusedEffect>()==true);Assert.AreEqual(dv,f.actor.GetStatValue("DV"));
        }
        [TestCase(true)] [TestCase(false)]
        public void Adversarial_RemovedOrOtherZoneMarkerCannotExpose(bool removed)
        {
            var f=Exposure();if(removed)f.z.RemoveEntity(f.point);
            Assert.IsFalse(f.point.GetPart<SeventhPositionPart>().TryAffectStandingPlayer(f.actor,removed?f.z:new Zone()));
        }
        [Test] public void Adversarial_DeadPlayerAndMissingArgumentsCannotExpose()
        {
            var f=Exposure();var part=f.point.GetPart<SeventhPositionPart>();
            Assert.IsFalse(part.TryAffectStandingPlayer(null,f.z));Assert.IsFalse(part.TryAffectStandingPlayer(f.actor,null));
            f.actor.GetStat("Hitpoints").Penalty=f.actor.GetStat("Hitpoints").BaseValue;
            Assert.IsFalse(part.TryAffectStandingPlayer(f.actor,f.z));
            Assert.DoesNotThrow(()=>SeventhPositionPart.OnPlayerTurnEnded(null,null));
        }
        [Test] public void Adversarial_OrdinaryActorEndsDoNotTickTheStandingPlayersExposure()
        {
            var f=Exposure();f.turns.EndTurn(f.actor,f.z);var effect=f.actor.GetPart<StatusEffectsPart>().GetEffect<ConfusedEffect>();
            var npc=Place(f.z,"Snapjaw",20,10);f.turns.AddEntity(npc);
            for(int i=0;i<20;i++)f.turns.EndTurn(npc,f.z);Assert.AreEqual(2,effect.Duration);
        }
        private GameSessionState Roundtrip(OverworldZoneManager m,Zone z,Entity actor)
        {
            m.SetActiveZone(z);var t=new TurnManager();t.AddEntity(actor);
            var state=GameSessionState.Capture("felling-adv","test",m,t,actor);
            using(var stream=new MemoryStream()){state.Save(new SaveWriter(stream));stream.Position=0;return GameSessionState.Load(new SaveReader(stream,_factory));}
        }
        [Test] public void Adversarial_OldSavedVegetationTagsCannotBypassBarrenGround()
        {
            var m=new OverworldZoneManager(_factory,64);var z=m.GetZone(FellingSiteBuilder.ZoneID);
            var tree=_factory.CreateEntity("Tree");tree.Tags.Remove("Vegetation");tree.SetIntProperty("SavedTree",1);
            Assert.IsTrue(z.AddEntity(tree,40,5));var actor=Place(z,"Player",40,12);
            var loaded=Roundtrip(m,z,actor);var lz=loaded.ZoneManager.GetZone(FellingSiteBuilder.ZoneID);
            Assert.IsFalse(lz.GetAllEntities().Any(e=>e.GetIntProperty("SavedTree")==1));
            Assert.AreEqual(6,lz.GetAllEntities().Count(e=>e.BlueprintName=="FellingBarePosition"));
            Assert.IsNotNull(lz.GetEntityCell(loaded.Player));Assert.IsFalse(lz.AddEntity(_factory.CreateEntity("Grass"),40,5));
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_SaveOnThePointHonorsActualPersistedPresence(bool removed)
        {
            var m=new OverworldZoneManager(_factory,64);var z=m.GetZone(FellingSiteBuilder.ZoneID);
            var point=z.GetAllEntities().Single(e=>e.HasPart<SeventhPositionPart>());var pos=z.GetEntityPosition(point);
            var actor=Place(z,"Player",pos.x,pos.y);if(removed)z.RemoveEntity(point);
            var loaded=Roundtrip(m,z,actor);var lz=loaded.ZoneManager.GetZone(FellingSiteBuilder.ZoneID);
            loaded.TurnManager.EndTurn(loaded.Player,lz);
            Assert.AreEqual(!removed,loaded.Player.GetPart<StatusEffectsPart>()?.HasEffect<ConfusedEffect>()==true);
        }
        [Test] public void Adversarial_PreexistingStatPenaltiesSurviveExposureAndRecovery()
        {
            var f=Exposure();f.actor.GetStat("DV").Penalty=3;f.actor.GetStat("Agility").Penalty=4;
            int dv=f.actor.GetStatValue("DV"),agi=f.actor.GetStatValue("Agility");f.turns.EndTurn(f.actor,f.z);
            Assert.AreEqual(dv-2,f.actor.GetStatValue("DV"));Assert.AreEqual(agi-2,f.actor.GetStatValue("Agility"));
            f.z.MoveEntity(f.actor,11,10);f.turns.EndTurn(f.actor,f.z);f.turns.EndTurn(f.actor,f.z);
            Assert.AreEqual(3,f.actor.GetStat("DV").Penalty);Assert.AreEqual(4,f.actor.GetStat("Agility").Penalty);
        }
        [Test] public void Adversarial_SaveActiveExposureRecoversWithTheSavedStats()
        {
            var m=new OverworldZoneManager(_factory,64);var z=m.GetZone(FellingSiteBuilder.ZoneID);
            var actor=Place(z,"Player",FellingSiteBuilder.SeventhX,FellingSiteBuilder.SeventhY);
            actor.GetStat("DV").Penalty=3;int dv=actor.GetStatValue("DV");var t=new TurnManager();t.AddEntity(actor);t.ProcessUntilPlayerTurn();t.EndTurn(actor,z);
            var loaded=Roundtrip(m,z,actor);var lz=loaded.ZoneManager.GetZone(FellingSiteBuilder.ZoneID);
            Assert.AreEqual(dv-2,loaded.Player.GetStatValue("DV"));
            lz.MoveEntity(loaded.Player,40,12);loaded.TurnManager.EndTurn(loaded.Player,lz);loaded.TurnManager.EndTurn(loaded.Player,lz);
            Assert.AreEqual(dv,loaded.Player.GetStatValue("DV"));Assert.AreEqual(3,loaded.Player.GetStat("DV").Penalty);
        }
        [Test] public void Adversarial_OldCachedGroundIsPreservedWhenMapGainsTheSite()
        {
            var m=new OverworldZoneManager(_factory,64);m.WorldMap.SetPOI(3,5,null);var z=m.GetZone(FellingSiteBuilder.ZoneID);
            var actor=Place(z,"Player",40,12);var item=Place(z,"Tepuibone",41,12);item.SetIntProperty("PlayerModification",8);int count=z.EntityCount;
            var loaded=Roundtrip(m,z,actor);var lz=loaded.ZoneManager.GetZone(FellingSiteBuilder.ZoneID);
            Assert.AreEqual(POIType.FellingSite,loaded.ZoneManager.WorldMap.GetPOI(3,5).Type);
            Assert.IsFalse(lz.GetAllEntities().Any(e=>e.HasPart<SeventhPositionPart>()));Assert.AreEqual(count,lz.EntityCount);
            Assert.IsTrue(lz.GetAllEntities().Any(e=>e.ID==item.ID&&e.GetIntProperty("PlayerModification")==8));
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_LoadedCellRepairDoesNotDependOnMarkerOrder(bool markerFirst)
        {
            var z=new Zone();var cell=z.GetCell(10,10);var marker=_factory.CreateEntity("FellingBarePosition");var tree=_factory.CreateEntity("Grass");var item=_factory.CreateEntity("Tepuibone");
            // Save LoadCell restores a raw list before entity indexes exist.
            cell.Objects.AddRange(markerFirst?new[]{marker,tree,item}:new[]{tree,item,marker});z.RebuildEntityCellsFromCells();
            Assert.IsNull(z.GetEntityCell(tree));CollectionAssert.AreEquivalent(new[]{marker,item},cell.Objects);
            Assert.AreEqual(2,z.EntityCount);Assert.AreEqual(0,z.GetEntitiesWithTag("Vegetation").Count);
        }
        [Test] public void Adversarial_ActualMapDescentArrivesOutsideTheSeventh()
        {
            var m=new OverworldZoneManager(_factory,64);var map=m.GetZone("WorldMap");var p=WorldMap.WorldCellToZoneCell(3,5);
            var actor=Place(map,"Player",p.zoneX,p.zoneY);var result=WorldMapTraversal.Descend(actor,map,m);
            Assert.IsTrue(result.Success);var ground=m.GetZone(FellingSiteBuilder.ZoneID);Assert.AreEqual((40,12),ground.GetEntityPosition(actor));
            Assert.IsFalse(ground.GetEntityCell(actor).HasObjectWithPart<SeventhPositionPart>());
        }
        [Test] public void Adversarial_MapRefreshPreservesUnknownOccupantsAndDiscoveryGates()
        {
            var m=new OverworldZoneManager(_factory,64);var z=m.GetZone("WorldMap");var item=Place(z,"Tepuibone",40,12);
            var before=item.GetPart<RenderPart>().DisplayName;var builder=new WorldMapZoneBuilder(m.WorldMap);
            var p=WorldMap.WorldCellToZoneCell(4,6);var marker=z.GetCell(p.zoneX,p.zoneY).Objects.Single(e=>e.HasPart<WorldMapCellPart>()).GetPart<RenderPart>();
            builder.RefreshAppearance(z);Assert.AreEqual(WorldMapZoneBuilder.GetBiomeRender(m.WorldMap.GetBiome(4,6)).displayName,marker.DisplayName);
            m.WorldMap.MarkVisited(4,6);builder.RefreshAppearance(z);Assert.AreEqual("Olderdeep",marker.DisplayName);
            Assert.AreEqual(before,item.GetPart<RenderPart>().DisplayName);Assert.AreEqual(400,builder.RefreshAppearance(z));
            Assert.AreEqual(0,builder.RefreshAppearance(new Zone("ordinary")));
        }
    }
}
