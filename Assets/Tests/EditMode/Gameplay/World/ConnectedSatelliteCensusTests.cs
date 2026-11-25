using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Skills;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class ConnectedSatelliteCensusTests
 {
  const BindingFlags All=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
  sealed class Probe:IZoneBuilder{internal Action<Zone> Run;public string Name=>"SatelliteSourceCensus";public int Priority=>4299;public bool BuildZone(Zone z,EntityFactory f,Random r){Run(z);return true;}}
  sealed class Manager:OverworldZoneManager
  {
   internal PopulationBuilder Population;internal ContainerBuilder Containers;internal SpreadExplorationBuilder Composer;internal Entity[] Before;internal Dictionary<Entity,(int,int)> At;internal Dictionary<Entity,int> Stock;internal int Sources;
   internal Manager(EntityFactory f,int seed):base(f,seed){}
   protected override ZoneGenerationPipeline GetPipelineForZone(string id)
   {
    var p=base.GetPipelineForZone(id);Population=p.Builders.OfType<PopulationBuilder>().SingleOrDefault();Containers=p.Builders.OfType<ContainerBuilder>().SingleOrDefault();Composer=p.Builders.OfType<SpreadExplorationBuilder>().SingleOrDefault();
    p.AddBuilder(new Probe{Run=z=>{Before=z.GetReadOnlyEntities().ToArray();At=Before.ToDictionary(e=>e,z.GetEntityPosition);Sources=Population.SourceReceipt.Owners.Count;Stock=Containers.SourceReceipt.Owners.SelectMany(e=>e.GetPart<ContainerPart>().Contents).ToDictionary(e=>e,e=>e.GetPart<StackerPart>()?.StackCount??1);}});return p;
   }
  }
  [Test]public void PredeclaredSixtyEightSeedCorpusUsesActualReceiptsAndIncludesBothAlternatives()
  =>RunCorpus(Enumerable.Range(1,65).Concat(new[]{1729,729490642,9091}),"sixty-eight-seed-census.tsv",true);
  [Test]public void NineObservedNativePairSeedsExposeExactRefusalStage()
  =>RunCorpus(new[]{6,12,15,19,24,29,40,46,56},"nine-pair-refusal-diagnostics.tsv",false);

  [TestCase(1,true)]
  [TestCase(14,false)]
  public void GeneratedTimberOpeningCanBeClearedAndKeepsItsResult(int seed,bool dismantle)
  {
   using var scope=new HaulingContentScope();
   var manager=OverworldZoneManager.CreateDetached(scope.Factory,seed,true);
   var entry=manager.Exploration.Entries.Single(e=>e.Family==SpreadExplorationFamily.HeavyFrame);
   scope.Seed(seed^FormationSelector.StableIndex(entry.ZoneID,int.MaxValue));
   var zone=manager.GetZone(entry.ZoneID);
   Assert.That(manager.Exploration.DispositionFor(entry.ZoneID),Is.EqualTo(2));
   var load=zone.GetReadOnlyEntities().Single(e=>e.GetProperty(ConnectedSpreadSatellite.RoleKey)=="frame");
   Assert.That(load.BlueprintName,Is.EqualTo("GleanersTimberPallet"));
   Assert.That(load.GetPart<ExaminablePart>().Text,Does.Not.Contain("garden wicket"));
   Assert.That(load.GetPart<ExaminablePart>().Text,Does.Not.Contain("blocks the opening"));
   var (x,y)=zone.GetEntityPosition(load);
   Assert.True(zone.GetCell(x,y).BlocksMovement());

   var actor=scope.Factory.CreateEntity("Player");
   actor.Statistics["Strength"].BaseValue=dismantle?11:12;
   Assert.True(zone.AddEntity(actor,x,y-1));
   string loadId=load.ID;
   var cache=zone.GetReadOnlyEntities().Single(e=>e.GetPart<ContainerPart>()?.Contents.Any(v=>v.BlueprintName=="ShatteredRimeGrimoire")==true);
   var stockIds=cache.GetPart<ContainerPart>().Contents.Select(e=>e.ID).ToArray();
   if(dismantle)
   {
    var oldFactory=HarvestablePart.Factory;
    try
    {
     HarvestablePart.Factory=scope.Factory;
     Assert.False(WorldInteractionSystem.GatherActions(load,actor).Any(a=>a.Command==HandlingPart.HaulCommand));
     Assert.True(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(load,"Harvest"),actor,zone).Success);
     Assert.False(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(load,"Harvest"),actor,zone).Success);
    }
    finally { HarvestablePart.Factory=oldFactory; }
    Assert.That(Timber(actor),Is.EqualTo(2));
    Assert.Null(zone.GetEntityCell(load));
   }
   else
   {
    Assert.That(DragSystem.TryGrab(actor,load,zone),Is.EqualTo(DragVerdict.Ok));
    Assert.True(MovementSystem.TryMove(actor,zone,0,-1));
    Assert.True(MovementSystem.TryMove(actor,zone,0,-1));
    Assert.True(DragSystem.Release(actor));
    Assert.That(zone.GetEntityPosition(load),Is.EqualTo((x,y-2)));
    Assert.False(load.GetPart<HarvestablePart>().Harvested);
    Assert.That(Timber(actor),Is.Zero);
    Assert.True(MovementSystem.TryMove(actor,zone,1,0));
    Assert.True(MovementSystem.TryMove(actor,zone,0,1));
    Assert.True(MovementSystem.TryMove(actor,zone,-1,1));
   }
   Assert.False(zone.GetCell(x,y).BlocksMovement());
   Assert.True(MovementSystem.TryMove(actor,zone,0,1));
   Assert.True(MovementSystem.TryMove(actor,zone,0,1));
   Assert.That(zone.GetEntityPosition(actor),Is.EqualTo((x,y+1)));

   manager.SetActiveZone(zone);
   var loaded=HotbarSaveFixture.RoundTrip(GameSessionState.Capture("satellite-timber","timber opening",manager,null,actor));
   var returned=loaded.ZoneManager.GetZone(entry.ZoneID);
   Assert.False(returned.GetCell(x,y).BlocksMovement());
   Assert.That(Timber(loaded.Player),Is.EqualTo(dismantle?2:0));
   var savedLoad=returned.GetReadOnlyEntities().SingleOrDefault(e=>e.ID==loadId);
   if(dismantle)Assert.Null(savedLoad);
   else
   {
    Assert.NotNull(savedLoad);
    Assert.That(returned.GetEntityPosition(savedLoad),Is.EqualTo((x,y-2)));
    Assert.False(savedLoad.GetPart<HarvestablePart>().Harvested);
   }
   var savedCache=returned.GetReadOnlyEntities().Single(e=>e.ID==cache.ID);
   CollectionAssert.AreEqual(stockIds,savedCache.GetPart<ContainerPart>().Contents.Select(e=>e.ID));
  }

  [Test]
  public void GeneratedTimberOpeningShowsAndFitsARealLoadedTurn()
  {
   const int seed = 1;
   using var scope = new HaulingContentScope();
   var manager = OverworldZoneManager.CreateDetached(scope.Factory, seed, true);
   var entry = manager.Exploration.Entries.Single(e => e.Family == SpreadExplorationFamily.HeavyFrame);
   scope.Seed(seed ^ FormationSelector.StableIndex(entry.ZoneID, int.MaxValue));
   var zone = manager.GetZone(entry.ZoneID);
   var pallet = zone.GetReadOnlyEntities().Single(e => e.GetProperty(ConnectedSpreadSatellite.RoleKey) == "frame");
   Assert.That(pallet.BlueprintName, Is.EqualTo("GleanersTimberPallet"));
   Assert.That(pallet.GetPart<ExaminablePart>().Text, Does.Contain("south shoulder").And.Contain("cargo sled"));
   var (x, y) = zone.GetEntityPosition(pallet);
   var actor = scope.Factory.CreateEntity("Player");
   actor.Statistics["Strength"].BaseValue = 12;
   var sled = scope.Factory.CreateEntity("CargoSled");
   var pack = actor.GetPart<InventoryPart>();
   Assert.That(pack.AddObject(sled), Is.True);
   Assert.That(zone.AddEntity(actor, x, y + 1), Is.True);
   zone.GetCell(x, y).IsVisible = true;
   zone.GetCell(x, y).Explored = true;
   var fit = InventorySystem.GetActions(actor, sled).Single(a => a.Command.StartsWith("FitCargoSled|", StringComparison.Ordinal));
   Assert.That(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(sled, fit.Command), actor, zone).Success, Is.True);
   Assert.That(DragSystem.TryGrab(actor, pallet, zone), Is.EqualTo(DragVerdict.Ok));
   foreach (var (dx, dy) in new[] { (0, 1), (1, 0), (1, 0), (1, 0) })
   {
    var from = zone.GetEntityCell(actor);
    Assert.That(MovementSystem.TryMove(actor, zone, dx, dy), Is.True);
    Assert.That(zone.GetEntityCell(pallet), Is.SameAs(from));
    Assert.That(zone.GetEntityCell(sled), Is.SameAs(from));
   }
   Assert.That(zone.GetEntityPosition(pallet), Is.EqualTo((x + 2, y + 2)));
   Assert.That(DragSystem.Release(actor), Is.True);
   var recover = InventorySystem.GetActions(actor, sled).Single(a => a.Command.StartsWith("TakeCargoSled|", StringComparison.Ordinal));
   Assert.That(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(sled, recover.Command), actor, zone).Success, Is.True);
   Assert.That(pack.Objects, Does.Contain(sled));
   Assert.That(zone.GetCell(x, y).BlocksMovement(), Is.False);
   Assert.That(Timber(actor), Is.Zero);
  }

  [TestCase(23,true,false)]
  [TestCase(24,false,false)]
  [TestCase(24,true,true)]
  public void OriginalPalletAndCacheSurviveWithoutTheParkingGuide(int version, bool hasSled, bool blockedBay)
  {
   const int seed = 1;
   using var scope = new HaulingContentScope();
   var manager = OverworldZoneManager.CreateDetached(scope.Factory, seed, true);
   if(version < SpreadExplorationPlan.CurrentVersion)
   {
    var world = SpreadExplorationPlan.BindForSave(manager, null);
    string wire = world.GetProperty(SpreadExplorationPlan.PropertyKey);
    world.Properties[SpreadExplorationPlan.PropertyKey] = version.ToString(System.Globalization.CultureInfo.InvariantCulture)
     + wire.Substring(wire.IndexOf('|'));
    var restore = typeof(SpreadExplorationPlan).GetMethod("Restore", All);
    typeof(OverworldZoneManager).GetProperty("Exploration").SetValue(manager, restore.Invoke(null, new object[]{manager,world}));
   }
   if(!hasSled)scope.Factory.Blueprints.Remove("CargoSled");
   var entry = manager.Exploration.Entries.Single(e => e.Family == SpreadExplorationFamily.HeavyFrame);
   scope.Seed(seed ^ FormationSelector.StableIndex(entry.ZoneID, int.MaxValue));
   var zone = manager.GetZone(entry.ZoneID);
   Assert.That(manager.Exploration.DispositionFor(entry.ZoneID), Is.EqualTo(2));
   var pallet = zone.GetReadOnlyEntities().Single(e => e.GetProperty(ConnectedSpreadSatellite.RoleKey) == "frame");
   Assert.That(pallet.BlueprintName, Is.EqualTo("GleanersTimberPallet"));
   if(blockedBay)
   {
    var reader = pallet.GetPart<ExaminablePart>();
    int guideStart = reader.Text.IndexOf("\n\n", StringComparison.Ordinal);
    Assert.That(guideStart, Is.GreaterThan(0));
    reader.Text = reader.Text.Substring(0, guideStart);
    var (x, y) = zone.GetEntityPosition(pallet);
    var wall = scope.Factory.CreateEntity("StoneWall");
    Assert.That(zone.AddEntity(wall, x+2, y+2), Is.True);
    typeof(ConnectedSpreadSatellite).GetMethod("AddSideParkingGuide", All).Invoke(null, new object[]{manager,zone});
    Assert.That(manager.GetZone(entry.ZoneID), Is.SameAs(zone));
    Assert.That(zone.GetEntityPosition(pallet), Is.EqualTo((x,y)));
    Assert.That(zone.GetEntityPosition(wall), Is.EqualTo((x+2,y+2)));
   }
   Assert.That(pallet.GetPart<ExaminablePart>().Text, Does.Contain("Dismantling").And.Not.Contain("south shoulder"));
   Assert.That(zone.GetReadOnlyEntities().Count(e => e.GetPart<ContainerPart>()?.Contents.Any(
    item => item.BlueprintName == "ShatteredRimeGrimoire") == true), Is.EqualTo(1));
  }

  [TestCase(1,true)]
  [TestCase(16,false)]
  public void OriginalFrameRemainsOnRetainedVersionOrAlternateSeed(int seed,bool retained)
  {
   using var scope=new HaulingContentScope();
   var manager=OverworldZoneManager.CreateDetached(scope.Factory,seed,true);
   if(retained)
   {
    var world=SpreadExplorationPlan.BindForSave(manager,null);
    string manifest=world.GetProperty(SpreadExplorationPlan.PropertyKey);
    world.Properties[SpreadExplorationPlan.PropertyKey]="19"+manifest.Substring(manifest.IndexOf('|'));
    var legacy=(SpreadExplorationPlan)typeof(SpreadExplorationPlan).GetMethod("Restore",All).Invoke(null,new object[]{manager,world});
    typeof(OverworldZoneManager).GetProperty("Exploration").SetValue(manager,legacy);
   }
   var entry=manager.Exploration.Entries.Single(e=>e.Family==SpreadExplorationFamily.HeavyFrame);
   scope.Seed(seed^FormationSelector.StableIndex(entry.ZoneID,int.MaxValue));
   var zone=manager.GetZone(entry.ZoneID);
   Assert.That(manager.Exploration.DispositionFor(entry.ZoneID),Is.EqualTo(2));
   var load=zone.GetReadOnlyEntities().Single(e=>e.GetProperty(ConnectedSpreadSatellite.RoleKey)=="frame");
   Assert.That(load.BlueprintName,Is.EqualTo("ConnectedHeavyFrame"));
   Assert.Null(load.GetPart<HarvestablePart>());
  }

  static int Timber(Entity actor)=>actor.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName=="SalvagedTimber")
   .Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);

  [TestCase("missing")]
  [TestCase("wrong-count")]
  [TestCase("fixed-timber")]
  [TestCase("creature-timber")]
  [TestCase("multi-unit-timber")]
  public void UnavailableOrMalformedTimberLeavesTheOriginalGraph(string defect)
  {
   using var scope=new HaulingContentScope();
   const int seed=1;
   if(defect=="missing")Assert.True(scope.Factory.Blueprints.Remove("SalvagedTimber"));
   if(defect=="wrong-count")scope.Factory.Blueprints["GleanersTimberPallet"].Parts["Harvestable"]["YieldMax"]="3";
   if(defect=="fixed-timber")scope.Factory.Blueprints["SalvagedTimber"].Parts["Physics"]["Takeable"]="false";
   if(defect=="creature-timber")scope.Factory.Blueprints["SalvagedTimber"].Tags["Creature"]="";
   if(defect=="multi-unit-timber")scope.Factory.Blueprints["SalvagedTimber"].Parts["Stacker"]["StackCount"]="2";
   var manager=new Manager(scope.Factory,seed);
   var entry=manager.Exploration.Entries.Single(e=>e.Family==SpreadExplorationFamily.HeavyFrame);
   scope.Seed(seed^FormationSelector.StableIndex(entry.ZoneID,int.MaxValue));
   var zone=manager.GetZone(entry.ZoneID);
   Assert.That(manager.Exploration.DispositionFor(entry.ZoneID),Is.Not.EqualTo(2));
   Assert.False(zone.GetReadOnlyEntities().Any(e=>e.Properties.ContainsKey(ConnectedSpreadSatellite.RoleKey)));
   foreach(var entity in manager.Before)Assert.That(zone.GetEntityPosition(entity),Is.EqualTo(manager.At[entity]));
   CollectionAssert.AreEquivalent(manager.Stock.Keys,
    manager.Containers.SourceReceipt.Owners.SelectMany(e=>e.GetPart<ContainerPart>().Contents));
  }

  static void RunCorpus(IEnumerable<int> seeds,string file,bool requireBoth)
  {
   bool worldgen=Diag.IsChannelEnabled("worldgen");Diag.SetChannel("worldgen",true);
   try
   {
   using(var scope=new HaulingContentScope())
   {
    string output=Environment.GetEnvironmentVariable("COO_SATELLITE_OUTPUT");
    if(string.IsNullOrEmpty(output))output=Path.Combine(Path.GetDirectoryName(UnityEngine.Application.dataPath),"Docs/Verification/ConnectedSpread/Tests");
    Directory.CreateDirectory(output);
    var lines=new List<string>{"seed\tzone\tfamily\tdisposition\tactorRoll\tcacheRoll\taddedOwners\tmanuals\tresult\trefusalDetail"};int wet=0,heavy=0;
    // Seed0 requests Environment.TickCount in ZoneManager; use the next fixed seed65 instead.
    foreach(int seed in seeds)
    {
     var m=new Manager(scope.Factory,seed);Assert.AreEqual(seed,m.WorldSeed);var e=m.Exploration.Entries.Single(v=>(int)v.Family>=18);scope.Seed(seed^FormationSelector.StableIndex(e.ZoneID,int.MaxValue));var z=m.GetZone(e.ZoneID);Assert.NotNull(z,e.ZoneID);bool placed=m.Exploration.DispositionFor(e.ZoneID)==2;int addedOwners=z.EntityCount-m.Before.Length;
     Assert.True(m.Before.All(v=>z.GetEntityCell(v)!=null));foreach(var item in m.Stock)Assert.AreEqual(item.Value,item.Key.GetPart<StackerPart>()?.StackCount??1);
     Assert.True(m.Stock.Keys.All(item=>m.Containers.SourceReceipt.Owners.Any(c=>c.GetPart<ContainerPart>().Contents.Contains(item))));
     var actors=m.Population.SourceReceipt.Owners;int manuals=m.Containers.SourceReceipt.Owners.SelectMany(c=>c.GetPart<ContainerPart>().Contents).Count(v=>v.BlueprintName=="ShatteredRimeGrimoire");
     if(placed){Assert.AreEqual(1,manuals);if(e.Family.ToString()=="WetCrossing"){if(wet==0)SavedPacket(m,z,scope.Factory);wet++;Assert.AreEqual(2,actors.Count);Assert.True(actors.All(v=>v.BlueprintName=="MarlbackScrabbler"));}else{if(heavy==0)SavedPacket(m,z,scope.Factory);heavy++;}}
     else foreach(var v in m.Before)Assert.AreEqual(m.At[v],z.GetEntityPosition(v),v.BlueprintName);
     lines.Add(string.Join("\t",seed,e.ZoneID,e.Family,m.Exploration.DispositionFor(e.ZoneID),string.Join(",",actors.Select(v=>v.BlueprintName)),m.Containers.SourceReceipt.Owners.Count,addedOwners,manuals,m.Composer.LastResult,placed?"accepted":Refusal(e.ZoneID,seed)));
     File.WriteAllText(Path.Combine(output,file),string.Join("\n",lines));
     m.UnloadZone(z.ZoneID);Assert.AreSame(z,m.GetZone(z.ZoneID));
    }
    string report=string.Join("\n",lines);TestContext.WriteLine(report);File.WriteAllText(Path.Combine(output,file),report);
    if(requireBoth){Assert.GreaterOrEqual(wet,1,"At least one actual rolled wet situation is a release requirement, never permission to add attackers.");Assert.GreaterOrEqual(heavy,1);}
   }
   }finally{Diag.SetChannel("worldgen",worldgen);}
  }
  [Serializable] sealed class RefusalData { public string zone; public int seed; }
  static string Refusal(string id,int seed)
  {
   foreach(var row in Diag.Snapshot(8192).Reverse())
   {if(row.Kind!="ConnectedSatelliteRefused"||string.IsNullOrEmpty(row.PayloadJson))continue;var data=UnityEngine.JsonUtility.FromJson<RefusalData>(row.PayloadJson);if(data.zone==id&&data.seed==seed)return row.PayloadJson.Replace("\t"," ").Replace("\n"," ").Replace("\r"," ");}
   return "none";
  }
  static string Tile(Zone z,int i)
  {var s=z.TileState.Get(i%Zone.Width,i/Zone.Width);return s==null||s.IsEmpty?"":string.Join("|",string.Join(",",s.Coatings.OrderBy(v=>v.Id).Select(v=>v.Id+":"+v.Turns)),string.Join(",",s.Residues.OrderBy(v=>v.Id).Select(v=>v.Id+":"+v.Turns)),s.Heat,s.Cold,s.Charge,s.Cloud,s.CloudTurns);}
  static void SavedPacket(Manager manager,Zone zone,EntityFactory factory)
  {
   // Core graph/command fixture: this places a test player at the local packet,
   // and makes no ordinary-start/native-input journey claim.
   var oldActive=SettlementRuntime.ActiveZone;
   try
   {
    var cache=manager.Containers.SourceReceipt.Owners.Single(e=>e.GetPart<ContainerPart>().Contents.Any(v=>v.BlueprintName=="ShatteredRimeGrimoire"));
    var manual=cache.GetPart<ContainerPart>().Contents.Single(e=>e.BlueprintName=="ShatteredRimeGrimoire");
    var actor=factory.CreateEntity("Player");var at=zone.GetEntityPosition(cache);
    var front=new[]{(at.x-1,at.y),(at.x+1,at.y),(at.x,at.y-1),(at.x,at.y+1)}.First(p=>zone.CanPlaceFootprint(actor,p.Item1,p.Item2));
    Assert.True(zone.AddEntity(actor,front.Item1,front.Item2));manager.SetActiveZone(zone);SettlementRuntime.ActiveZone=zone;
    Assert.False(actor.GetPart<SkillsPart>().HasSkill("Rites_ShatteredRime"));
    Assert.True(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(cache,manual),actor,zone).Success);
    Assert.True(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(manual,"ReadGrimoire"),actor,zone).Success);
    Assert.True(actor.GetPart<SkillsPart>().HasSkill("Rites_ShatteredRime"));
    var frame=zone.GetReadOnlyEntities().SingleOrDefault(e=>e.BlueprintName=="ConnectedHeavyFrame");
    if(frame!=null)
    {
     var p=zone.GetEntityPosition(frame);Assert.True(zone.MoveEntity(actor,p.x,p.y-1));int normalSpeed=actor.GetStatValue("Speed");
     Assert.AreEqual(DragVerdict.Ok,DragSystem.TryGrab(actor,frame,zone));Assert.Less(actor.GetStatValue("Speed"),normalSpeed);
     Assert.True(MovementSystem.TryMove(actor,zone,0,-1));Assert.True(MovementSystem.TryMove(actor,zone,0,-1));Assert.AreEqual((p.x,p.y-2),zone.GetEntityPosition(frame));
     Assert.True(DragSystem.Release(actor));Assert.AreEqual(normalSpeed,actor.GetStatValue("Speed"));
    }
    string remaining=string.Join("|",cache.GetPart<ContainerPart>().Contents.Select(e=>e.ID+":"+(e.GetPart<StackerPart>()?.StackCount??1)));
    var expected=zone.GetReadOnlyEntities().ToDictionary(e=>e.ID,e=>zone.GetEntityPosition(e));var tiles=Enumerable.Range(0,Zone.Width*Zone.Height).Select(i=>Tile(zone,i)).ToArray();
    var state=GameSessionState.Capture("connected-satellite","core save evidence",manager,null,actor);GameSessionState loaded;
    using(var stream=new MemoryStream()){state.Save(new SaveWriter(stream));stream.Position=0;loaded=GameSessionState.Load(new SaveReader(stream,factory));}
    var restored=(OverworldZoneManager)loaded.ZoneManager;var returned=restored.GetZone(zone.ZoneID);Assert.AreNotSame(zone,returned);Assert.AreEqual(2,restored.Exploration.DispositionFor(zone.ZoneID));
    Assert.AreEqual(expected.Count,returned.EntityCount);foreach(var e in returned.GetReadOnlyEntities()){Assert.True(expected.ContainsKey(e.ID));Assert.AreEqual(expected[e.ID],returned.GetEntityPosition(e));}
    CollectionAssert.AreEqual(tiles,Enumerable.Range(0,Zone.Width*Zone.Height).Select(i=>Tile(returned,i)).ToArray());var current=returned.GetReadOnlyEntities().Single(e=>e.ID==cache.ID);
    Assert.AreEqual(remaining,string.Join("|",current.GetPart<ContainerPart>().Contents.Select(e=>e.ID+":"+(e.GetPart<StackerPart>()?.StackCount??1))));
    Assert.True(loaded.Player.GetPart<InventoryPart>().Objects.Any(e=>e.ID==manual.ID));Assert.True(loaded.Player.GetPart<SkillsPart>().HasSkill("Rites_ShatteredRime"));
    restored.UnloadZone(returned.ZoneID);Assert.AreSame(returned,restored.GetZone(returned.ZoneID));
   }
   finally{SettlementRuntime.ActiveZone=oldActive;}
  }

 }
}
