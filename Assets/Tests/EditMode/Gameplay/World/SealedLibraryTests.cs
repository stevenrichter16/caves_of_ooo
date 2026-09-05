using System;
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
    public sealed class SealedLibraryTests
    {
        private EntityFactory _factory;
        [OneTimeSetUp] public void Setup()
        {
            _factory=new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
        }
        [Test] public void AuthoredLibraryHasAnAppendOnlyArchetypeAndACleanSlopeMouth()
        {
            Assert.AreEqual("SealedLibrary",SinkholeArchetypes.For("Stillleaf").ToString());
            Assert.AreEqual(3,(int)SinkholeArchetypes.For("Stillleaf"));
            var map=WorldGenerator.Generate(66);var poi=map.GetPOI(2,4);
            Assert.IsNotNull(poi);Assert.AreEqual(POIType.Sinkhole,poi.Type);
            Assert.AreEqual("Stillleaf",poi.Name);Assert.AreEqual("SealedLibrary",poi.Profile);
            Assert.AreEqual(StumpBand.Slopes,StumpBands.BandAt(2,4));
            Assert.IsFalse(WorldMapAuthoring.IsRoad(2,4));Assert.IsFalse(WorldMapAuthoring.IsRiver(2,4));
            Assert.IsFalse(map.IsVisited(2,4));
        }
        [Test] public void UnnamedFloorsDoNotAcquireAnUnkeyedLibraryByHash()
        {
            for(int i=0;i<200;i++)Assert.Less((int)SinkholeArchetypes.For("unnamed-"+i),3);
            CollectionAssert.AreEqual(new[]{0,1,2},new[]{(int)SinkholeArchetype.DrownedSima,(int)SinkholeArchetype.StrandedSettlement,(int)SinkholeArchetype.ChoirCathedral});
        }
        [TestCase(false)] [TestCase(true)]
        public void ActualStackContainsOneLockedVaultAtTheFloorOnly(bool floorFirst)
        {
            var m=new OverworldZoneManager(_factory,66);
            if(!floorFirst){m.GetZone("Overworld.2.4.0");m.GetZone("Overworld.2.4.1");}
            var z=m.GetZone("Overworld.2.4.2");Assert.IsNotNull(z);
            var door=z.GetAllEntities().SingleOrDefault(e=>e.BlueprintName=="SealedLibraryDoor");
            Assert.IsNotNull(door);Assert.IsTrue(door.GetPart<LockPart>().IsLocked);
            Assert.AreEqual("coo.sealed-library.stillleaf",door.GetPart<LockPart>().KeyId);
            foreach(string bp in new[]{"LibraryTepuiboneWall","LibraryMemoryMarbleWall","LibraryChoirIronWall","SealedArchiveShelf","SealedLibraryFloor"})
                Assert.IsTrue(z.GetAllEntities().Any(e=>e.BlueprintName==bp),bp);
            foreach(int depth in new[]{0,1,3})Assert.IsFalse(m.GetZone("Overworld.2.4."+depth).GetAllEntities().Any(e=>e.BlueprintName=="SealedLibraryDoor"));
            z.RemoveEntity(door);Assert.AreSame(z,m.GetZone(z.ZoneID));
            Assert.IsFalse(z.GetAllEntities().Any(e=>e.BlueprintName=="SealedLibraryDoor"),"cached player state is preserved");
        }
        [TestCase("LibraryTepuiboneWall","library_tepuibone_wall",4)]
        [TestCase("LibraryMemoryMarbleWall","library_memory_marble_wall",4)]
        [TestCase("LibraryChoirIronWall","library_choir_iron_wall",4)]
        [TestCase("SealedLibraryFloor","sealed_library_floor",4)]
        [TestCase("SealedArchiveShelf","sealed_archive_shelf",4)]
        [TestCase("SealedLibraryDoor","sealed_library_door",1)]
        public void EveryNewObjectHasRealImportedArt(string bp,string file,int variants)
        {
            Assert.IsTrue(_factory.Blueprints.ContainsKey(bp));
            Assert.IsTrue(EnvironmentSpriteRenderer.FixtureSprites.Any(r=>r.Blueprint==bp&&r.File==file));
            for(int v=0;v<variants;v++)Assert.IsNotNull(Resources.Load<Sprite>("Sprites/Environment/"+file+(v==0?"":"_v"+v)));
            if(variants>1)Assert.AreEqual(variants,EnvironmentSpriteRenderer.FixtureVariantCounts[bp]);
        }
        [Test] public void AllShippedKeysLeaveTheVaultLocked()
        {
            var door=_factory.CreateEntity("SealedLibraryDoor");Assert.IsNotNull(door);
            var actor=_factory.CreateEntity("Player");
            foreach(var bp in _factory.Blueprints.Values)
                if(bp.Parts.ContainsKey("Key"))Assert.AreNotEqual("coo.sealed-library.stillleaf",_factory.CreateEntity(bp.Name).GetPart<KeyPart>().KeyId,bp.Name);
            var e=GameEvent.New("AttemptUnlock");e.SetParameter("Actor",(object)actor);door.FireEventAndRelease(e);
            Assert.IsTrue(door.GetPart<LockPart>().IsLocked);
        }
        private static Entity Marker(Zone z,int x,int y)
        {var p=WorldMap.WorldCellToZoneCell(x,y);return z.GetCell(p.zoneX,p.zoneY).Objects.Single(e=>e.HasPart<WorldMapCellPart>());}
        [Test] public void ReturningToACachedMapRevealsOnlyTheSurveyedMouth()
        {
            var m=new OverworldZoneManager(_factory,66);var map=m.GetZone("WorldMap");
            var marker=Marker(map,2,4);Assert.AreNotEqual("Stillleaf",marker.GetPart<RenderPart>().DisplayName);
            var ground=m.GetZone("Overworld.2.4.0");var actor=_factory.CreateEntity("Player");ground.AddEntity(actor,40,12);
            var r=WorldMapTraversal.Ascend(actor,ground,m);Assert.IsTrue(r.Success);
            Assert.AreSame(marker,Marker(map,2,4));Assert.AreEqual("Stillleaf",marker.GetPart<RenderPart>().DisplayName);
            Assert.AreNotEqual("Olderdeep",Marker(map,4,6).GetPart<RenderPart>().DisplayName);
            Assert.IsNotNull(map.GetEntityCell(actor));
        }
        [Test] public void FailedAscentDoesNotRevealTheMouth()
        {
            var m=new OverworldZoneManager(_factory,66);var map=m.GetZone("WorldMap");var p=WorldMap.WorldCellToZoneCell(2,4);
            var wall=new Entity();wall.SetTag("Solid");map.AddEntity(wall,p.zoneX,p.zoneY);
            var ground=m.GetZone("Overworld.2.4.0");var actor=_factory.CreateEntity("Player");ground.AddEntity(actor,40,12);
            Assert.IsFalse(WorldMapTraversal.Ascend(actor,ground,m).Success);
            Assert.IsFalse(m.WorldMap.IsVisited(2,4));Assert.IsNotNull(ground.GetEntityCell(actor));
        }
    }
}
