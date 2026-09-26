using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;
using Random=System.Random;
namespace CavesOfOoo.Tests
{
    public class DensityReferenceGladeTests
    {
        public static EntityFactory Factory()
        { var f=new EntityFactory();f.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));return f; }
        public static Zone Build(int seed=64)
        { var z=new Zone(ReferenceGladePlan.ZoneID);Assert.IsTrue(new ReferenceGladeBuilder(seed).BuildZone(z,Factory(),new System.Random(8)));return z; }
        [Test] public void AddressIsOrdinarySpreadBesideSill()
        { Assert.AreEqual("Overworld.11.10.0",ReferenceGladePlan.ZoneID);Assert.AreEqual(BiomeType.Spread,WorldMapAuthoring.BiomeAt(11,10));Assert.IsNull(WorldMapAuthoring.PlaceAt(11,10));Assert.IsFalse(SinkholeSites.IsMouth(11,10)); }
        [Test] public void LayoutRepeatsWithoutRandomStateAndVariesOnlyDressing()
        { var a=ReferenceGladePlan.Create(64);Assert.AreEqual(a.Signature(),ReferenceGladePlan.Create(64).Signature());Assert.AreNotEqual(a.Signature(),ReferenceGladePlan.Create(65).Signature()); }
        [Test] public void EveryPlacementIsInBoundsAndGroundCoversTheMap()
        { var p=ReferenceGladePlan.Create(64);Assert.AreEqual(2000,p.Placements.Count(e=>e.Blueprint=="Grass"));Assert.IsTrue(p.Placements.All(e=>e.X>=0&&e.X<80&&e.Y>=0&&e.Y<25));Assert.IsFalse(p.Placements.GroupBy(e=>e.X+":"+e.Y+":"+e.Blueprint).Any(g=>g.Count()>1)); }
        [TestCase("reference-glade-ground")][TestCase("reference-glade-pale-reeds")][TestCase("reference-glade-green-grass")]
        [TestCase("reference-glade-dark-ruin")][TestCase("reference-glade-low-wall")][TestCase("reference-glade-lit-wall")][TestCase("reference-glade-gravel")]
        public void ReferenceFamiliesHaveActualNativeOwners(string visual)
        { var z=Build();Assert.IsTrue(z.GetAllEntities().Any(e=>e.GetPart<RenderPart>()?.VisualID==visual)); }
        [Test] public void ExactWallAndOpenGateCollision()
        { var z=Build();Assert.IsTrue(z.GetCell(53,5).BlocksMovement());Assert.IsTrue(z.GetCell(31,13).BlocksMovement());Assert.IsFalse(z.GetCell(31,16).BlocksMovement());Assert.IsFalse(z.GetCell(40,12).BlocksMovement()); }
        [Test] public void StartingAreaHasNoHostileAtImmediateMeleeRange()
        { var z=Build();foreach(var e in z.GetAllEntities().Where(e=>e.BlueprintName.StartsWith("Marlback"))){var c=z.GetEntityCell(e);Assert.Greater(Math.Max(Math.Abs(c.X-40),Math.Abs(c.Y-12)),5);}Assert.That(z.GetAllEntities().Count(e=>e.BlueprintName.StartsWith("Marlback")),Is.InRange(2,4)); }
        [Test] public void ContainerHasFiniteNativeSuppliesAndNoDebugKit()
        { var z=Build();var c=z.GetAllEntities().Single(e=>e.BlueprintName=="Chest").GetPart<ContainerPart>();CollectionAssert.AreEquivalent(new[]{"HealingTonic","Torch","DriedMeat"},c.Contents.Select(e=>e.BlueprintName));Assert.IsFalse(z.GetAllEntities().Any(e=>e.HasPart<BitLockerPart>())); }
        [Test] public void GreenLightBelongsToRealDestructibleWallAndFiniteQuartz()
        { var z=Build();var lights=z.GetAllEntities().Where(e=>e.BlueprintName=="Wall"&&e.GetPart<RenderPart>().VisualID=="reference-glade-lit-wall").ToArray();Assert.Greater(lights.Length,4);Assert.IsTrue(lights.All(e=>e.HasPart<DestructiblePart>()&&e.GetPart<LightSourcePart>()?.LightColor=="&G"));Assert.AreEqual(2,z.GetAllEntities().Count(e=>e.BlueprintName=="GlowQuartzVein")); }
        [Test] public void AllExitsAndInteractionApproachesAreReachable()
        { var z=Build();var reached=Reach(z,40,12);Assert.IsTrue(reached.Any(c=>c.Item1==0));Assert.IsTrue(reached.Any(c=>c.Item1==79));Assert.IsTrue(reached.Any(c=>c.Item2==0));Assert.IsTrue(reached.Any(c=>c.Item2==24));foreach(var e in z.GetAllEntities().Where(e=>e.HasPart<ContainerPart>()||e.HasPart<HarvestablePart>())){var p=z.GetEntityCell(e);Assert.IsTrue(reached.Any(c=>Math.Max(Math.Abs(c.Item1-p.X),Math.Abs(c.Item2-p.Y))<=1),e.BlueprintName);} }
        public static HashSet<(int,int)> Reach(Zone z,int x,int y)
        { var set=new HashSet<(int,int)>();var q=new Queue<(int,int)>();q.Enqueue((x,y));set.Add((x,y));while(q.Count>0){var c=q.Dequeue();foreach(var d in new[]{(1,0),(-1,0),(0,1),(0,-1)}){int nx=c.Item1+d.Item1,ny=c.Item2+d.Item2;var cell=z.GetCell(nx,ny);if(cell!=null&&!cell.BlocksMovement()&&set.Add((nx,ny)))q.Enqueue((nx,ny));}}return set; }
        [TestCase("Overworld.10.10.0")][TestCase("Overworld.11.10.1")][TestCase("other")]
        public void OtherAddressesAreNotReplaced(string id)
        { var z=new Zone(id);Assert.IsFalse(new ReferenceGladeBuilder(64).BuildZone(z,Factory(),new Random(9)));Assert.AreEqual(0,z.EntityCount);Assert.IsFalse(ReferenceGladePlan.IsActive(z)); }
        [Test] public void RebuildingOccupiedZoneDoesNotRefillOrOverwrite()
        { var z=Build();var before=z.GetAllEntities().ToArray();Assert.IsFalse(new ReferenceGladeBuilder(64).BuildZone(z,Factory(),new Random(9)));CollectionAssert.AreEquivalent(before,z.GetAllEntities()); }
        [Test] public void MissingContentRefusesBeforeAnyWorldWrite()
        {var z=new Zone(ReferenceGladePlan.ZoneID);Assert.IsFalse(new ReferenceGladeBuilder(64).BuildZone(z,new EntityFactory(),new Random(9)));Assert.AreEqual(0,z.EntityCount);Assert.AreEqual(0,z.GenReservedCells.Count);}
        [Test] public void CallerRngIsNotConsumedByLayoutOrBuilder()
        {var rng=new Random(19);var z=new Zone(ReferenceGladePlan.ZoneID);Assert.IsTrue(new ReferenceGladeBuilder(64).BuildZone(z,Factory(),rng));Assert.AreEqual(new Random(19).Next(),rng.Next());}
        [Test] public void ActualWorldManagerUsesGladeButHonorsChangedBiome()
        {var f=Factory();var a=OverworldZoneManager.CreateDetached(f,64);var z=a.GetZone(ReferenceGladePlan.ZoneID);Assert.IsTrue(z.GetAllEntities().Any(e=>e.GetPart<RenderPart>()?.VisualID=="reference-glade-ground"));Assert.IsTrue(ReferenceGladePlan.IsActive(z));a.WorldMap.Tiles[11,10]=BiomeType.Sodden;Assert.IsFalse(ReferenceGladePlan.IsActive(z));}
        [Test] public void RuntimePoiIsNotOverwritten()
        {var m=OverworldZoneManager.CreateDetached(Factory(),64);m.WorldMap.SetPOI(11,10,new PointOfInterest(POIType.Village,"changed","Villagers",1));var z=m.GetZone(ReferenceGladePlan.ZoneID);Assert.IsFalse(ReferenceGladePlan.IsActive(z));Assert.IsFalse(z.GetAllEntities().Any(e=>e.GetPart<RenderPart>()?.VisualID=="reference-glade-ground"));}
        [Test] public void ProceduralPoisNeverClaimTheGlade()
        {for(int seed=0;seed<80;seed++)Assert.IsNull(WorldGenerator.Generate(seed).GetPOI(11,10));}
    }
}
