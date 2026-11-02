using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class StumpSummitPocketTests
 {
  DensityLootTestScope scope;
  [SetUp] public void Setup()=>scope=new DensityLootTestScope();
  [TearDown] public void Cleanup()=>scope.Dispose();
  sealed class Fixture
  {
   internal OverworldZoneManager Manager;internal Zone Zone;internal StumpCompositionBuilder Terrain;internal IZoneBuilder Pocket;
   internal Random Rng=new Random(901);
   internal Fixture(CavesOfOoo.Data.EntityFactory f,int seed=64,string id="Overworld.3.4.0")
   {
    Manager=OverworldZoneManager.CreateDetached(f,seed,true);Zone=new Zone(id);
    var pipeline=(ZoneGenerationPipeline)typeof(OverworldZoneManager).GetMethod("GetPipelineForZone",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(Manager,new object[]{id});
    Terrain=pipeline.Builders.OfType<StumpCompositionBuilder>().SingleOrDefault();Pocket=pipeline.Builders.SingleOrDefault(b=>b.Name=="StumpSummitPocket");
    foreach(var b in pipeline.Builders.OrderBy(b=>b.Priority).Where(b=>b.Priority<4350))Assert.True(b.BuildZone(Zone,f,Rng),b.Name);
   }
   internal bool Run(CavesOfOoo.Data.EntityFactory f){Assert.NotNull(Pocket,"Missing bounded cold SummitScrub shelter");return Pocket.BuildZone(Zone,f,Rng);}
  }
  static string[] Shape(Zone z)=>z.GetReadOnlyEntities().Select(e=>e.BlueprintName+"@"+z.GetEntityPosition(e)).OrderBy(x=>x).ToArray();
  [TestCase(17)][TestCase(64)][TestCase(65)]
  public void FreshPocketPreservesAllOriginalOwnersRoutesReservesAndRandom(int seed)
  {
   var f=new Fixture(scope.Factory,seed);var z=f.Zone;var original=z.GetReadOnlyEntities().ToDictionary(e=>e,e=>z.GetEntityPosition(e));
   string sig=f.Terrain.Plan.Signature(),state=z.TileState.ToSaveString();var reserves=z.GenReservedCells.ToArray();var reach=WaysiteBuilder.Reach(z);
   f.Rng=new Random(783);Assert.True(f.Run(scope.Factory));var added=z.GetReadOnlyEntities().Where(e=>!original.ContainsKey(e)).ToArray();
   Assert.That(added.Length,Is.EqualTo(11).Or.EqualTo(0));if(seed==64)Assert.AreEqual(11,added.Length,"Actual baseline shoulder must receive its bounded group");
   if(added.Length>0){Assert.AreEqual(4,added.Count(e=>e.BlueprintName=="StoneDome"));Assert.AreEqual(1,added.Count(e=>e.BlueprintName=="Tree"));Assert.AreEqual(6,added.Count(e=>e.BlueprintName=="Bush"));}
   foreach(var kv in original){Assert.AreEqual(kv.Value,z.GetEntityPosition(kv.Key));Assert.AreSame(kv.Key,z.GetReadOnlyEntities().Single(e=>e.ID==kv.Key.ID));}
   foreach(var e in added){var p=z.GetEntityPosition(e);Assert.IsNull(f.Terrain.Plan.ObjectAt(p.x,p.y));Assert.False(f.Terrain.Plan.IsHabitat(p.x,p.y));for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)Assert.False(f.Terrain.Plan.IsApproach(p.x+dx,p.y+dy));Assert.AreEqual(e.BlueprintName!="Bush",e.GetPart<PhysicsPart>().Solid);}
   Assert.AreEqual(sig,f.Terrain.Plan.Signature());Assert.AreEqual(state,z.TileState.ToSaveString());CollectionAssert.AreEquivalent(reserves,z.GenReservedCells);Assert.AreEqual(new Random(783).Next(),f.Rng.Next());
   var after=WaysiteBuilder.Reach(z);Assert.LessOrEqual(after.parts,reach.parts);Assert.GreaterOrEqual(after.cells,reach.cells-added.Count(e=>e.GetPart<PhysicsPart>().Solid));
   TestContext.WriteLine(string.Join(";",added.Select(e=>e.BlueprintName+"@"+z.GetEntityPosition(e))));
  }
  [Test] public void SameSeedIsDeterministicAndRepeatedInvocationDoesNotStack()
  {var a=new Fixture(scope.Factory);var b=new Fixture(scope.Factory);Assert.True(a.Run(scope.Factory));Assert.True(b.Run(scope.Factory));CollectionAssert.AreEqual(Shape(a.Zone),Shape(b.Zone));var before=Shape(a.Zone);Assert.True(a.Run(scope.Factory));CollectionAssert.AreEqual(before,Shape(a.Zone));}
  [TestCase("cached")][TestCase("active")][TestCase("foreign-factory")][TestCase("biome")][TestCase("source-zone")]
  public void ForeignOrLoadedAuthorityDeclines(string kind)
  {var f=new Fixture(scope.Factory);if(kind=="cached")f.Manager.CachedZones.Add(f.Zone.ZoneID,f.Zone);if(kind=="active")f.Manager.SetActiveZone(f.Zone);if(kind=="biome")f.Manager.WorldMap.Tiles[3,4]=BiomeType.Spread;if(kind=="source-zone")f.Terrain.BuildZone(new Zone("Overworld.3.4.0"),scope.Factory,new Random(8));var before=Shape(f.Zone);Assert.True(f.Run(kind=="foreign-factory"?GrovelandsCompositionTests.Factory():scope.Factory));CollectionAssert.AreEqual(before,Shape(f.Zone));}
  [TestCase("null")][TestCase("short")]
  public void MalformedPoiMapDeclines(string kind)
  {var f=new Fixture(scope.Factory);var before=Shape(f.Zone);f.Manager.WorldMap.POIs=kind=="null"?null:new PointOfInterest[1,1];Assert.True(f.Run(scope.Factory));CollectionAssert.AreEqual(before,Shape(f.Zone));}
  [TestCase("cultivated")][TestCase("finite-row")][TestCase("harvestable")]
  public void ExistingGroundMechanismsNeverBecomeShelter(string kind)
  {var f=new Fixture(scope.Factory);foreach(var e in f.Zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="TepuiStone")){if(kind=="cultivated")e.AddPart(new CultivatedSoilPart());else if(kind=="finite-row")e.AddPart(new FieldHarvestPart());else e.AddPart(new HarvestablePart());}var before=Shape(f.Zone);Assert.True(f.Run(scope.Factory));CollectionAssert.AreEqual(before,Shape(f.Zone));}
  [TestCase("StoneDome")][TestCase("Tree")][TestCase("Bush")]
  public void MissingPayloadDeclinesWholePocket(string bp)
  {var f=new Fixture(scope.Factory);var before=Shape(f.Zone);scope.Factory.Blueprints.Remove(bp);Assert.True(f.Run(scope.Factory));CollectionAssert.AreEqual(before,Shape(f.Zone));}
  [TestCase("reserved")][TestCase("wet")][TestCase("occupied")][TestCase("interior")]
  public void UnavailableShoulderPreservesOriginalGraph(string kind)
  {var f=new Fixture(scope.Factory);var z=f.Zone;for(int y=2;y<23;y++)for(int x=2;x<78;x++){if(kind=="reserved")z.GenReservedCells.Add((x,y));else if(kind=="wet")z.TileState.WriteCoating(x,y,"water",ZoneTileState.Permanent);else if(kind=="interior")z.GetCell(x,y).IsInterior=true;else z.AddEntity(new Entity{ID="fixture-"+x+"-"+y,BlueprintName="foreign"},x,y);}var before=Shape(z);var reserves=z.GenReservedCells.ToArray();var state=z.TileState.ToSaveString();Assert.True(f.Run(scope.Factory));CollectionAssert.AreEqual(before,Shape(z));CollectionAssert.AreEquivalent(reserves,z.GenReservedCells);Assert.AreEqual(state,z.TileState.ToSaveString());}
  [TestCase("wall")][TestCase("barren")][TestCase("empty-id")][TestCase("duplicate-id")][TestCase("footprint")][TestCase("tile")][TestCase("reserve")]
  public void PayloadAndCallbackCorruptionCannotPublishPocket(string mutation)
  {
   var f=new Fixture(scope.Factory);var before=f.Zone.GetReadOnlyEntities().ToArray();Entity first=null;
   scope.Factory.RegisterPartType<SpreadGenerationReceiptTests.ReceiptCreatedPart>("ReceiptCreated");
   foreach(string bp in new[]{"StoneDome","Tree","Bush"})scope.Factory.Blueprints[bp].Parts["ReceiptCreated"]=new Dictionary<string,string>();
   SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback=e=>{if(first==null){first=e;return;}if(mutation=="wall")first.Tags["Wall"]="true";else if(mutation=="barren")first.Tags["Barren"]="true";else if(mutation=="empty-id")first.ID="";else if(mutation=="duplicate-id")first.ID=e.ID;else if(mutation=="footprint"){if(!first.HasPart<SpatialFootprintPart>())first.AddPart(new SpatialFootprintPart());}else if(mutation=="tile")f.Zone.TileState.WriteCoating(3,3,"water",ZoneTileState.Permanent);else f.Zone.GenReservedCells.Add((3,3));};
   try{f.Run(scope.Factory);Assert.NotNull(first);CollectionAssert.AreEquivalent(before,f.Zone.GetReadOnlyEntities());}finally{SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback=null;}
  }
 }
}
