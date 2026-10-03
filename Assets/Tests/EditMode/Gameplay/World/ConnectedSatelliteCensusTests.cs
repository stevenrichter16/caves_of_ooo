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
     var actors=m.Population.SourceReceipt.Owners;int manuals=m.Containers.SourceReceipt.Owners.SelectMany(c=>c.GetPart<ContainerPart>().Contents).Count(v=>v.BlueprintName=="DitchkeepersFootwork");
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
    var cache=manager.Containers.SourceReceipt.Owners.Single(e=>e.GetPart<ContainerPart>().Contents.Any(v=>v.BlueprintName=="DitchkeepersFootwork"));
    var manual=cache.GetPart<ContainerPart>().Contents.Single(e=>e.BlueprintName=="DitchkeepersFootwork");
    var actor=factory.CreateEntity("Player");var at=zone.GetEntityPosition(cache);
    var front=new[]{(at.x-1,at.y),(at.x+1,at.y),(at.x,at.y-1),(at.x,at.y+1)}.First(p=>zone.CanPlaceFootprint(actor,p.Item1,p.Item2));
    Assert.True(zone.AddEntity(actor,front.Item1,front.Item2));manager.SetActiveZone(zone);SettlementRuntime.ActiveZone=zone;
    Assert.False(actor.GetPart<SkillsPart>().HasSkill("Acrobatics_Vault"));
    Assert.True(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(cache,manual),actor,zone).Success);
    Assert.True(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(manual,"ReadGrimoire"),actor,zone).Success);
    Assert.True(actor.GetPart<SkillsPart>().HasSkill("Acrobatics_Vault"));
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
    Assert.True(loaded.Player.GetPart<InventoryPart>().Objects.Any(e=>e.ID==manual.ID));Assert.True(loaded.Player.GetPart<SkillsPart>().HasSkill("Acrobatics_Vault"));
    restored.UnloadZone(returned.ZoneID);Assert.AreSame(returned,restored.GetZone(returned.ZoneID));
   }
   finally{SettlementRuntime.ActiveZone=oldActive;}
  }

 }
}
