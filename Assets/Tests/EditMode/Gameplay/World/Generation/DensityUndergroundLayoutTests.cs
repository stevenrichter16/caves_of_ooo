using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class DensityUndergroundLayoutTests
 {
  DensityLootTestScope scope;
  [SetUp]public void Setup(){scope=new DensityLootTestScope();}
  [TearDown]public void Cleanup(){scope.Dispose();}
  UndergroundLayoutPlan Plan(int seed=64,string id="Overworld.9.11.3",BiomeType biome=BiomeType.Spread,PointOfInterest poi=null)=>UndergroundLayoutPlan.Select(seed,id,biome,poi,scope.Factory);
  int RoomSeed(int depth=3){for(int seed=1;seed<500;seed++)if(Plan(seed,"Overworld.9.11."+depth).IsRoomed)return seed;throw new Exception("missing roomed positive control");}
  static ZoneGenerationPipeline Pipeline(OverworldZoneManager m,string id)=>(ZoneGenerationPipeline)typeof(OverworldZoneManager).GetMethod("GetPipelineForZone",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(m,new object[]{id});
  [Test]public void DeterministicCorpusContainsBothFamiliesAtBoundedRoomedShare(){int roomed=0;for(int s=1;s<=128;s++){var p=Plan(s);Assert.AreEqual(p.IsRoomed,Plan(s).IsRoomed);if(p.IsRoomed)roomed++;}Assert.That(roomed,Is.InRange(16,48));}
  [TestCase(0)][TestCase(1)][TestCase(2)]public void NearSurfaceDepthsStayNatural(int depth){for(int s=1;s<=40;s++)Assert.False(Plan(s,"Overworld.9.11."+depth).IsRoomed);}
  [TestCase(BiomeType.Grovelands)][TestCase(BiomeType.Overwrit)][TestCase(BiomeType.Stump)][TestCase(BiomeType.Cave)][TestCase(BiomeType.Desert)][TestCase(BiomeType.Jungle)]
  public void ChoirAuthoredAndLegacyBiomesStayNatural(BiomeType biome){Assert.False(Plan(RoomSeed(),biome:biome).IsRoomed);}
  [TestCase(POIType.Village)][TestCase(POIType.Lair)][TestCase(POIType.MerchantCamp)][TestCase(POIType.RiverChunk)][TestCase(POIType.Sinkhole)][TestCase(POIType.Root)][TestCase(POIType.FellingSite)]
  public void AllPoiColumnsStayOutsideGenericRoomSelection(POIType type){Assert.False(Plan(RoomSeed(),poi:new PointOfInterest(type,"actual claimed column")).IsRoomed);}
  [TestCase(null)][TestCase("other")][TestCase("Overworld.99.1.3")][TestCase("Overworld.1.1.-3")]
  public void InvalidAddressesFailToNaturalWithoutThrowing(string id){Assert.DoesNotThrow(()=>Assert.False(Plan(id:id).IsRoomed));}
  [TestCase(3,"LimestoneWall","LimestoneFloor")][TestCase(5,"ShaleWall","ShaleFloor")][TestCase(7,"SlateWall","SlateFloor")][TestCase(9,"QuartziteWall","QuartziteFloor")][TestCase(11,"ObsidianWall","ObsidianFloor")]
  public void RoomedGeometryKeepsTheExistingDepthPalette(int depth,string wall,string floor){var p=Plan(RoomSeed(depth),"Overworld.9.11."+depth);Assert.True(p.IsRoomed);Assert.AreEqual(wall,p.WallBlueprint);Assert.AreEqual(floor,p.FloorBlueprint);var room=p.CreateGeometryBuilder() as RuinsBuilder;Assert.NotNull(room);Assert.AreEqual(wall,room.StoneWallBlueprint);Assert.AreEqual(floor,room.StoneFloorBlueprint);Assert.That(room.MinRooms,Is.GreaterThanOrEqualTo(4));Assert.That(room.MaxRooms,Is.LessThanOrEqualTo(7));}
  [TestCase("Overworld.2.19.3")][TestCase("Overworld.1.6.3")][TestCase("Overworld.11.10.3")][TestCase("Overworld.3.7.3")]
  public void AuthoredWildernessColumnsStayNaturalEvenWithoutAPoi(string id){var p=Plan(RoomSeed(),id);Assert.False(p.IsRoomed);Assert.False(p.IsOrdinaryColumn);}
  [Test]public void ContextDistinguishesOrdinaryNaturalGeometryFromClaimedColumns(){var p=Plan(RoomSeed());Assert.AreEqual(BiomeType.Spread,p.SurfaceBiome);Assert.True(p.IsOrdinaryColumn);Assert.False(Plan(RoomSeed(),poi:new PointOfInterest(POIType.Village,"claimed")).IsOrdinaryColumn);}
  [Test]public void AllActualPoiRoutesRemainOutsideTheNewRoomedBuilder(){var m=OverworldZoneManager.CreateDetached(scope.Factory,64);int checkedColumns=0;for(int x=0;x<20;x++)for(int y=0;y<20;y++)if(m.WorldMap.GetPOI(x,y)!=null){checkedColumns++;foreach(int depth in new[]{1,2,3,7})Assert.False(Pipeline(m,WorldMap.ToZoneID(x,y,depth)).Builders.Any(b=>b is RuinsBuilder||b is UndergroundRouteReservationBuilder),WorldMap.ToZoneID(x,y,depth));}Assert.Greater(checkedColumns,10);}
  [TestCase(BiomeType.Grovelands)][TestCase(BiomeType.Overwrit)][TestCase(BiomeType.Stump)]
  public void PreservedCanonicalBiomesDoNotAcquireTheNewRecipeNode(BiomeType biome){var m=OverworldZoneManager.CreateDetached(scope.Factory,64);m.WorldMap.Tiles[9,11]=biome;m.WorldMap.POIs[9,11]=null;Assert.False(Pipeline(m,"Overworld.9.11.3").Builders.Any(b=>b is UndergroundRouteReservationBuilder));}
  [Test]public void NumericAliasesUseTheSameCanonicalSelection(){for(int s=1;s<80;s++)Assert.AreEqual(Plan(s).IsRoomed,Plan(s,"Overworld.09.011.03").IsRoomed);}
  [Test]public void SelectionReadsNoGameplayRandomOrFactoryCallbacks(){int seed=RoomSeed();var oldL=LoadoutPart.Rng;var oldT=TraderPart.Rng;try{var rng=new DensitySariAdversarialTests.ForbiddenRandom();LoadoutPart.Rng=TraderPart.Rng=rng;Assert.True(Plan(seed).IsRoomed);Assert.AreSame(rng,LoadoutPart.Rng);Assert.AreSame(rng,TraderPart.Rng);}finally{LoadoutPart.Rng=oldL;TraderPart.Rng=oldT;}}
  [Test]public void IncompleteRoomDecorationPackRetainsNaturalGeometry(){int seed=RoomSeed();scope.Factory.Blueprints.Remove("Pillar");Assert.False(Plan(seed).IsRoomed);Assert.IsInstanceOf<StrataBuilder>(Plan(seed).CreateGeometryBuilder());}
  [Test]public void ManagerChangesOnlyGeometryAndKeepsOneSharedPopulationAndTravelTail()
  {var m=OverworldZoneManager.CreateDetached(scope.Factory,64);var room=Find(m,3,true);var cave=Find(m,3,false);var rp=Pipeline(m,room);var cp=Pipeline(m,cave);Assert.AreEqual(1,rp.Builders.Count(b=>b is RuinsBuilder));Assert.AreEqual(1,cp.Builders.Count(b=>b is StrataBuilder));CollectionAssert.AreEqual(cp.Builders.Where(b=>!(b is StrataBuilder)).Select(b=>b.GetType()),rp.Builders.Where(b=>!(b is RuinsBuilder)).Select(b=>b.GetType()));Assert.AreEqual(1,rp.Builders.Count(b=>b is PopulationBuilder));Assert.AreEqual(1,rp.Builders.Count(b=>b is ContainerBuilder));}
  string Find(OverworldZoneManager m,int depth,bool roomed)
  {for(int x=0;x<WorldMap.Width;x++)for(int y=0;y<WorldMap.Height;y++){var b=m.WorldMap.GetBiome(x,y);if(b!=BiomeType.Spread&&b!=BiomeType.Sodden&&b!=BiomeType.Beating)continue;if(m.WorldMap.GetPOI(x,y)!=null)continue;string id=WorldMap.ToZoneID(x,y,depth);if(UndergroundLayoutPlan.Select(m.WorldSeed,id,b,null,scope.Factory).IsRoomed==roomed)return id;}throw new Exception("missing selected column");}
  [TestCase(1)][TestCase(64)][TestCase(1729)][TestCase(2026)][TestCase(729490642)]
  public void ActualBothLayoutFamiliesKeepConnectedSafeStairsAndFourTerrainExits(int seed)
  {foreach(int depth in new[]{3,7,11})foreach(bool roomed in new[]{false,true}){scope.Seed(seed);var m=OverworldZoneManager.CreateDetached(scope.Factory,seed);string id=Find(m,depth,roomed);var z=m.GetZone(id);Assert.NotNull(z,id);var stairs=z.GetReadOnlyEntities().Where(e=>e.HasPart<StairsUpPart>()||e.HasPart<StairsDownPart>()).ToArray();Assert.AreEqual(2,stairs.Length,id);var first=z.GetEntityCell(stairs[0]);var reach=Reach(z,first.X,first.Y);foreach(var st in stairs){var c=z.GetEntityCell(st);Assert.True(reach.Contains((c.X,c.Y)),id+" unreachable stair "+c.X+","+c.Y+" roomed="+roomed);Assert.False(c.Occupants.Any(e=>e.HasTag("Creature")||e.HasPart<TriggerOnStepPart>()||e.HasTag("Wall")),id+" unsafe stair "+c.X+","+c.Y);}Assert.True(reach.Any(c=>c.x==0),id+" west");Assert.True(reach.Any(c=>c.x==Zone.Width-1),id+" east");Assert.True(reach.Any(c=>c.y==0),id+" north");Assert.True(reach.Any(c=>c.y==Zone.Height-1),id+" south");var palette=SolidEarthBuilder.GetMaterialsForDepth(depth);Assert.True(z.GetReadOnlyEntities().Any(e=>e.BlueprintName==palette.wallBP),id+" missing wall palette");Assert.True(z.GetReadOnlyEntities().Any(e=>e.BlueprintName==palette.floorBP),id+" missing floor palette");}}
  public static HashSet<(int x,int y)> Reach(Zone z,int x,int y){var found=new HashSet<(int,int)>{(x,y)};var q=new Queue<(int x,int y)>();q.Enqueue((x,y));while(q.Count>0){var at=q.Dequeue();for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){if(dx==0&&dy==0)continue;int nx=at.x+dx,ny=at.y+dy;var c=z.GetCell(nx,ny);if(c==null||c.Occupants.Any(e=>!e.HasTag("Creature")&&(e.HasTag("Solid")||e.GetPart<PhysicsPart>()?.Solid==true)))continue;if(found.Add((nx,ny)))q.Enqueue((nx,ny));}}return found;}
 }
}
