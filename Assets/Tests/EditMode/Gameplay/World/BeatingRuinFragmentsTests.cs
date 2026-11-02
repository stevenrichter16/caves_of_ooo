using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class BeatingRuinFragmentsTests
 {
  DensityLootTestScope scope;
  [SetUp] public void Setup()=>scope=new DensityLootTestScope();
  [TearDown] public void Cleanup()=>scope.Dispose();
  sealed class Fixture
  {
   internal OverworldZoneManager Manager;internal Zone Zone;internal BeatingCompositionBuilder Terrain;internal IZoneBuilder Pocket;
   internal Random Rng=new Random(901);
   internal Fixture(CavesOfOoo.Data.EntityFactory f,int seed=64,string id="Overworld.19.12.0")
   {
    Manager=OverworldZoneManager.CreateDetached(f,seed,true);Zone=new Zone(id);
    var pipeline=(ZoneGenerationPipeline)typeof(OverworldZoneManager).GetMethod("GetPipelineForZone",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(Manager,new object[]{id});
    Terrain=pipeline.Builders.OfType<BeatingCompositionBuilder>().SingleOrDefault();Pocket=pipeline.Builders.SingleOrDefault(b=>b.Name=="BeatingRuinFragments");
    foreach(var b in pipeline.Builders.OrderBy(b=>b.Priority).Where(b=>b.Priority<4350))Assert.True(b.BuildZone(Zone,f,Rng),b.Name);
   }
   internal bool Run(CavesOfOoo.Data.EntityFactory f){Assert.NotNull(Pocket,"Missing bounded cold RuinField fragments");return Pocket.BuildZone(Zone,f,Rng);}
  }
  static string[] Shape(Zone z)=>z.GetReadOnlyEntities().Select(e=>e.BlueprintName+"@"+z.GetEntityPosition(e)).OrderBy(x=>x).ToArray();
  [TestCase(17)][TestCase(64)][TestCase(65)]
  public void FreshFragmentsPreservesAllOriginalOwnersRoutesReservesAndRandom(int seed)
  {
   var f=new Fixture(scope.Factory,seed);var z=f.Zone;var original=z.GetReadOnlyEntities().ToDictionary(e=>e,e=>z.GetEntityPosition(e));
   string sig=f.Terrain.Plan.Signature(),state=z.TileState.ToSaveString();var reserves=z.GenReservedCells.ToArray();var reach=WaysiteBuilder.Reach(z);
   f.Rng=new Random(783);Assert.True(f.Run(scope.Factory));var added=z.GetReadOnlyEntities().Where(e=>!original.ContainsKey(e)).ToArray();
   Assert.That(added.Length,Is.InRange(0,14));if(seed==64)Assert.GreaterOrEqual(added.Length,6,"Actual baseline compounds must receive meaningful groups");
   Assert.True(added.All(e=>e.BlueprintName=="BrokenColumn"||e.BlueprintName=="Pillar"));
   if(seed==64)Assert.AreEqual(2,added.Count(e=>e.BlueprintName=="Pillar"),"A real colonnade shoulder must be visible in the baseline ruin");
   foreach(var kv in original){Assert.AreEqual(kv.Value,z.GetEntityPosition(kv.Key));Assert.AreSame(kv.Key,z.GetReadOnlyEntities().Single(e=>e.ID==kv.Key.ID));}
   foreach(var e in added){var p=z.GetEntityPosition(e);Assert.IsNull(f.Terrain.Plan.ObjectAt(p.x,p.y));for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)Assert.False(f.Terrain.Plan.IsApproach(p.x+dx,p.y+dy));Assert.AreEqual(e.BlueprintName=="Pillar",e.GetPart<PhysicsPart>().Solid);Assert.False(e.GetPart<PhysicsPart>().Takeable);Assert.True(Enumerable.Range(0,f.Terrain.Plan.RuinCount).Any(n=>f.Terrain.Plan.GetRuin(n).Contains(p.x,p.y)));}
   for(int room=0;room<f.Terrain.Plan.RuinCount;room++)
   {
    var ruin=f.Terrain.Plan.GetRuin(room);var cells=added.Select(e=>z.GetEntityPosition(e)).Where(p=>ruin.Contains(p.x,p.y)).ToArray();
    Assert.That(cells.Length,Is.EqualTo(0).Or.EqualTo(4-room).Or.EqualTo(7));
    foreach(var p in cells)Assert.True(Enumerable.Range(-2,5).Any(dy=>Enumerable.Range(-2,5).Any(dx=>z.GetCell(p.x+dx,p.y+dy).Occupants.Any(e=>e.BlueprintName=="SandstoneWall"))));
    if(cells.Length==7)Assert.AreEqual(2,added.Count(e=>e.BlueprintName=="Pillar"&&ruin.Contains(z.GetEntityPosition(e).x,z.GetEntityPosition(e).y)));
    if(cells.Length>0){var reached=new HashSet<(int x,int y)>{cells[0]};bool more;do{more=false;foreach(var p in cells)if(!reached.Contains(p)&&reached.Any(q=>Math.Abs(q.x-p.x)+Math.Abs(q.y-p.y)==1)){reached.Add(p);more=true;}}while(more);Assert.AreEqual(cells.Length,reached.Count);}
   }
   Assert.AreEqual(original.Keys.Count(e=>e.BlueprintName=="DryBrush"),z.GetReadOnlyEntities().Count(e=>e.BlueprintName=="DryBrush"));
   Assert.AreEqual(sig,f.Terrain.Plan.Signature());Assert.AreEqual(state,z.TileState.ToSaveString());CollectionAssert.AreEquivalent(reserves,z.GenReservedCells);Assert.AreEqual(new Random(783).Next(),f.Rng.Next());
   var after=WaysiteBuilder.Reach(z);Assert.LessOrEqual(after.parts,reach.parts);Assert.GreaterOrEqual(after.cells,reach.cells-added.Count(e=>e.GetPart<PhysicsPart>().Solid));
   TestContext.WriteLine(string.Join(";",added.Select(e=>e.BlueprintName+"@"+z.GetEntityPosition(e))));
  }

  [TestCase("poi")][TestCase("map")][TestCase("tiles-null")][TestCase("tiles-short")][TestCase("seed")][TestCase("formation")][TestCase("producer-factory")][TestCase("active-only")]
  public void ExactProducerAndMapAuthorityRequired(string fault)
  {
   var f=new Fixture(scope.Factory);var m=f.Manager;
   if(fault=="poi"){m.WorldMap.POIs[19,12]=m.WorldMap.GetPOI(10,10);Assert.NotNull(m.WorldMap.POIs[19,12]);}
   if(fault=="map")typeof(OverworldZoneManager).GetProperty("WorldMap").SetValue(m,WorldGenerator.Generate(64));
   if(fault=="tiles-null")m.WorldMap.Tiles=null;
   if(fault=="tiles-short")m.WorldMap.Tiles=new BiomeType[1,1];
   if(fault=="seed")typeof(ZoneManager).GetProperty("WorldSeed").SetValue(m,65);
   if(fault=="formation")f.Terrain.FormationOverride=Formation.RuinField;
   if(fault=="producer-factory")typeof(BeatingCompositionBuilder).GetProperty("SourceFactory").SetValue(f.Terrain,GrovelandsCompositionTests.Factory());
   if(fault=="active-only"){m.SetActiveZone(f.Zone);m.CachedZones.Remove(f.Zone.ZoneID);}
   var before=Shape(f.Zone);Assert.True(f.Run(scope.Factory));CollectionAssert.AreEqual(before,Shape(f.Zone));
  }
  [Test] public void SameSeedIsDeterministicAndRepeatedInvocationDoesNotStack()
  {var a=new Fixture(scope.Factory);var b=new Fixture(scope.Factory);Assert.True(a.Run(scope.Factory));Assert.True(b.Run(scope.Factory));CollectionAssert.AreEqual(Shape(a.Zone),Shape(b.Zone));var before=Shape(a.Zone);Assert.True(a.Run(scope.Factory));CollectionAssert.AreEqual(before,Shape(a.Zone));}
  [TestCase("cached")][TestCase("active")][TestCase("foreign-factory")][TestCase("biome")][TestCase("source-zone")]
  public void ForeignOrLoadedAuthorityDeclines(string kind)
  {var f=new Fixture(scope.Factory);if(kind=="cached")f.Manager.CachedZones.Add(f.Zone.ZoneID,f.Zone);if(kind=="active")f.Manager.SetActiveZone(f.Zone);if(kind=="biome")f.Manager.WorldMap.Tiles[19,12]=BiomeType.Spread;if(kind=="source-zone")f.Terrain.BuildZone(new Zone("Overworld.19.12.0"),scope.Factory,new Random(8));var before=Shape(f.Zone);Assert.True(f.Run(kind=="foreign-factory"?GrovelandsCompositionTests.Factory():scope.Factory));CollectionAssert.AreEqual(before,Shape(f.Zone));}
  [TestCase("null")][TestCase("short")]
  public void MalformedPoiMapDeclines(string kind)
  {var f=new Fixture(scope.Factory);var before=Shape(f.Zone);f.Manager.WorldMap.POIs=kind=="null"?null:new PointOfInterest[1,1];Assert.True(f.Run(scope.Factory));CollectionAssert.AreEqual(before,Shape(f.Zone));}
  [TestCase("cultivated")][TestCase("finite-row")][TestCase("harvestable")]
  public void ExistingGroundMechanismsNeverBecomeFragments(string kind)
  {var f=new Fixture(scope.Factory);foreach(var e in f.Zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="SandstoneFloor")){if(kind=="cultivated")e.AddPart(new CultivatedSoilPart());else if(kind=="finite-row")e.AddPart(new FieldHarvestPart());else e.AddPart(new HarvestablePart());}var before=Shape(f.Zone);Assert.True(f.Run(scope.Factory));CollectionAssert.AreEqual(before,Shape(f.Zone));}
  [TestCase("BrokenColumn")][TestCase("Pillar")]
  public void MissingPayloadDeclinesWholeFragmentSet(string bp)
  {var f=new Fixture(scope.Factory);var before=Shape(f.Zone);scope.Factory.Blueprints.Remove(bp);Assert.True(f.Run(scope.Factory));CollectionAssert.AreEqual(before,Shape(f.Zone));}
  [TestCase("reserved")][TestCase("wet")][TestCase("occupied")][TestCase("interior")]
  public void UnavailableCompoundPreservesOriginalGraph(string kind)
  {var f=new Fixture(scope.Factory);var z=f.Zone;for(int y=2;y<23;y++)for(int x=2;x<78;x++){if(kind=="reserved")z.GenReservedCells.Add((x,y));else if(kind=="wet")z.TileState.WriteCoating(x,y,"water",ZoneTileState.Permanent);else if(kind=="interior")z.GetCell(x,y).IsInterior=true;else z.AddEntity(new Entity{ID="fixture-"+x+"-"+y,BlueprintName="foreign"},x,y);}var before=Shape(z);var reserves=z.GenReservedCells.ToArray();var state=z.TileState.ToSaveString();Assert.True(f.Run(scope.Factory));CollectionAssert.AreEqual(before,Shape(z));CollectionAssert.AreEquivalent(reserves,z.GenReservedCells);Assert.AreEqual(state,z.TileState.ToSaveString());}

  [TestCase("duplicate-physics")][TestCase("destroyed")][TestCase("passable")][TestCase("material")][TestCase("wall")]
  public void MalformedPillarCannotPublishAnyGroup(string fault)
  {
   var f=new Fixture(scope.Factory);var before=Shape(f.Zone);
   scope.Factory.RegisterPartType<SpreadGenerationReceiptTests.ReceiptCreatedPart>("ReceiptCreated");
   scope.Factory.Blueprints["Pillar"].Parts["ReceiptCreated"]=new Dictionary<string,string>();
   SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback=e=>{e.RemovePart(e.GetPart<SpreadGenerationReceiptTests.ReceiptCreatedPart>());
    if(fault=="duplicate-physics")e.AddPart(new PhysicsPart());
    else if(fault=="destroyed")e.GetPart<DestructiblePart>().HP=0;
    else if(fault=="passable")e.GetPart<PhysicsPart>().Solid=false;
    else if(fault=="material")e.GetPart<MaterialPart>().MaterialID="Wood";
    else e.Tags["Wall"]="true";};
   try{Assert.True(f.Run(scope.Factory));CollectionAssert.AreEqual(before,Shape(f.Zone));}
   finally{SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback=null;}
  }

  [Test] public void BenignCreationCallbackStillPublishesCanonicalFragments()
  {var f=new Fixture(scope.Factory);var before=f.Zone.EntityCount;scope.Factory.RegisterPartType<SpreadGenerationReceiptTests.ReceiptCreatedPart>("ReceiptCreated");scope.Factory.Blueprints["BrokenColumn"].Parts["ReceiptCreated"]=new Dictionary<string,string>();int callbacks=0;SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback=e=>{callbacks++;e.RemovePart(e.GetPart<SpreadGenerationReceiptTests.ReceiptCreatedPart>());};try{Assert.True(f.Run(scope.Factory));Assert.That(callbacks,Is.InRange(10,12));Assert.AreEqual(before+callbacks+2,f.Zone.EntityCount);}finally{SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback=null;}}
  [TestCase("duplicate-physics")][TestCase("duplicate-render")][TestCase("duplicate-examinable")][TestCase("wall")][TestCase("solid")][TestCase("empty-id")][TestCase("duplicate-id")][TestCase("footprint")][TestCase("tile")][TestCase("reserve")]
  public void PayloadAndCallbackCorruptionCannotPublishPocket(string mutation)
  {
   var f=new Fixture(scope.Factory);var before=f.Zone.GetReadOnlyEntities().ToArray();Entity first=null;
   scope.Factory.RegisterPartType<SpreadGenerationReceiptTests.ReceiptCreatedPart>("ReceiptCreated");
   foreach(string bp in new[]{"BrokenColumn"})scope.Factory.Blueprints[bp].Parts["ReceiptCreated"]=new Dictionary<string,string>();
   SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback=e=>{e.RemovePart(e.GetPart<SpreadGenerationReceiptTests.ReceiptCreatedPart>());if(first==null){first=e;return;}if(mutation=="duplicate-physics")first.AddPart(new PhysicsPart());else if(mutation=="duplicate-render")first.AddPart(new RenderPart());else if(mutation=="duplicate-examinable")first.AddPart(new ExaminablePart());else if(mutation=="wall")first.Tags["Wall"]="true";else if(mutation=="solid")first.GetPart<PhysicsPart>().Solid=true;else if(mutation=="empty-id")first.ID="";else if(mutation=="duplicate-id")first.ID=e.ID;else if(mutation=="footprint"){if(!first.HasPart<SpatialFootprintPart>())first.AddPart(new SpatialFootprintPart());}else if(mutation=="tile")f.Zone.TileState.WriteCoating(3,3,"water",ZoneTileState.Permanent);else f.Zone.GenReservedCells.Add((3,3));};
   try{f.Run(scope.Factory);Assert.NotNull(first);CollectionAssert.AreEquivalent(before,f.Zone.GetReadOnlyEntities());}finally{SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback=null;}
  }
 }
}
