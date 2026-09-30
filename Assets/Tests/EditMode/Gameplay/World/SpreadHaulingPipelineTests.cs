using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;
using Random=System.Random;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadHaulingPipelineTests
 {
  const BindingFlags All=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
  sealed class Probe:IZoneBuilder{readonly Action<Zone,Random>a;readonly int p;public Probe(int p,Action<Zone,Random>a){this.p=p;this.a=a;}public string Name=>"HaulingCensusProbe";public int Priority=>p;public bool BuildZone(Zone z,EntityFactory f,Random r){a(z,r);return true;}}
  sealed class Manager:OverworldZoneManager
  {
   readonly bool baseline;public SpreadCompositionBuilder Terrain;public HaulablePropBuilder Haul;public SpreadExplorationBuilder Composer;public Entity[] Before,After;public (int x,int y)[] BeforeAt;public string[] Shapes;public Part[][] Parts;public int Tail;public SpreadGenerationReceipt Receipt;public int EligibleHedges;public bool EligibleLoad;
   public Manager(EntityFactory f,int seed,bool baseline):base(f,seed){this.baseline=baseline;}
   protected override ZoneGenerationPipeline GetPipelineForZone(string id)
   {
    var p=base.GetPipelineForZone(id);if(Exploration.Find(id)?.Family.ToString()!="HeavySalvage")return p;Terrain=p.Builders.OfType<SpreadCompositionBuilder>().SingleOrDefault();Haul=p.Builders.OfType<HaulablePropBuilder>().SingleOrDefault();Composer=p.Builders.OfType<SpreadExplorationBuilder>().SingleOrDefault();if(baseline)p.RemoveBuilders<SpreadExplorationBuilder>();
    p.AddBuilder(new Probe(4299,(z,r)=>{Before=z.GetReadOnlyEntities().ToArray();BeforeAt=Before.Select(z.GetEntityPosition).ToArray();Shapes=Before.Select(Shape).ToArray();Parts=Before.Select(e=>e.Parts.ToArray()).ToArray();Receipt=(SpreadGenerationReceipt)typeof(HaulablePropBuilder).GetProperty("SourceReceipt",All)?.GetValue(Haul);
     var eligible=typeof(SpreadExplorationPassage).GetMethod("Eligible",All);EligibleHedges=((IEnumerable)typeof(SpreadCompositionBuilder).GetProperty("PassageSources",All).GetValue(Terrain)).Cast<SpreadGenerationReceipt>().Count(x=>x.IsCurrent&&(bool)eligible.Invoke(null,new object[]{z,x.Owners.Single()}));
     var load=Receipt?.Owners.Single();var helper=typeof(Zone).Assembly.GetType("CavesOfOoo.Core.SpreadExplorationHauling");EligibleLoad=load!=null&&helper!=null&&(bool)helper.GetMethod("EligibleLoad",All).Invoke(null,new object[]{z,load});
    }));p.AddBuilder(new Probe(int.MaxValue,(z,r)=>{After=z.GetReadOnlyEntities().ToArray();Tail=r.Next();}));return p;
   }
  }
  static string Shape(Entity e)=>e.BlueprintName+"|"+e.GetPart<RenderPart>()?.DisplayName+"|"+e.GetPart<PhysicsPart>()?.Weight+"|"+e.GetPart<DestructiblePart>()?.HP+"|"+string.Join(";",e.Parts.Select(p=>p.Name))+"|stock:"+string.Join(";",(e.GetPart<ContainerPart>()?.Contents??new List<Entity>()).Select(x=>x.BlueprintName+":"+(x.GetPart<StackerPart>()?.StackCount??1)))+"|gear:"+string.Join(";",DensityLootTestScope.Gear(e).Select(x=>x.BlueprintName+":"+(x.GetPart<StackerPart>()?.StackCount??1)));
  [Serializable]public sealed class Selection{public int seed,eligibleHedgerows,quiet;public string[] selected;public string[] families;}
  [Serializable]public sealed class Row{public int seed,hedges,moved,nextRng;public string zone,source,result;public bool compatible,committed;public double milliseconds;}
  [Serializable]public sealed class Report{public string boundary;public Selection[] selection;public List<Row> rows=new List<Row>();public int compatible,committed,seedsWithCommit;}
  static void Write(string name,object value){string folder=Environment.GetEnvironmentVariable("COO_HAULING_CENSUS_OUTPUT");if(string.IsNullOrEmpty(folder))return;Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,name),JsonUtility.ToJson(value,true));}
  [Test]public void FixedThreeSeedCurrentCohortUsesOnlyOrdinaryRollsAndMeetsReleaseFloor()
  {
   object priorSettlement=SettlementManager.Current;
   try{using(var scope=new HaulingContentScope())
   {
    var report=new Report{boundary="All current-v9 selected Hedgerow addresses frozen before any GetZone; paired same-version composer-disabled actual ordinary pipeline. F9 original source rules are unchanged; literal historical manifests remain separate controls. Standalone hash differs from Unity. No native input, travel safety, model or save claim."};
    report.selection=new[]{1,64,1729}.Select(seed=>{var m=OverworldZoneManager.CreateDetached(scope.Factory,seed,true);Assert.AreEqual(10,m.Exploration.Version);Assert.Zero(m.CachedZoneCount);var fields=m.Exploration.Entries.Where(e=>e.PlacementEligible&&FormationSelector.For(BiomeType.Spread,e.ZoneID)==Formation.Hedgerow).ToArray();return new Selection{seed=seed,eligibleHedgerows=fields.Length,quiet=fields.Count(e=>e.Family==SpreadExplorationFamily.None),selected=fields.Where(e=>e.Family.ToString()=="HeavySalvage").Select(e=>e.ZoneID).ToArray(),families=fields.GroupBy(e=>e.Family.ToString()).OrderBy(g=>g.Key).Select(g=>g.Key+":"+g.Count()).ToArray()};}).ToArray();
    Write("selection-before-generation.json",report);Assert.True(report.selection.All(s=>s.selected.Length>0));
    foreach(var sel in report.selection)foreach(var id in sel.selected)
    {
     int seed=sel.seed;scope.Seed(unchecked(seed^FormationSelector.StableIndex(id,int.MaxValue)));var baseline=new Manager(scope.Factory,seed,true);var b=baseline.GetZone(id);Assert.NotNull(b);var baselineRng=new[]{LoadoutPart.Rng.Next(),TraderPart.Rng.Next(),LootDropSystem.Rng.Next()};
     scope.Seed(unchecked(seed^FormationSelector.StableIndex(id,int.MaxValue)));var current=new Manager(scope.Factory,seed,false);var timer=System.Diagnostics.Stopwatch.StartNew();var z=current.GetZone(id);timer.Stop();Assert.NotNull(z);var currentRng=new[]{LoadoutPart.Rng.Next(),TraderPart.Rng.Next(),LootDropSystem.Rng.Next()};
     CollectionAssert.AreEqual(baseline.Shapes,current.Shapes);CollectionAssert.AreEqual(baseline.BeforeAt,current.BeforeAt);Assert.AreEqual(baseline.Tail,current.Tail);CollectionAssert.AreEqual(baselineRng,currentRng);CollectionAssert.AreEquivalent(current.Before,current.After);Assert.AreEqual(b.TileState.ToSaveString(),z.TileState.ToSaveString());
     var source=current.Receipt?.Owners.Single();bool compatible=current.EligibleLoad&&current.EligibleHedges>=12;bool committed=current.Exploration.DispositionFor(id)==2;int moved=current.Before.Count(e=>z.GetEntityPosition(e)!=current.BeforeAt[Array.IndexOf(current.Before,e)]);
     for(int i=0;i<current.Before.Length;i++){var e=current.Before[i];CollectionAssert.AreEqual(current.Parts[i],e.Parts);Assert.AreEqual(Shape(baseline.Before[i]),Shape(e));if(e!=source&&e.BlueprintName!="Hedge")Assert.AreEqual(current.BeforeAt[i],z.GetEntityPosition(e));}
     Assert.LessOrEqual(moved,13);if(!committed)Assert.Zero(moved);else{Assert.True(compatible);Assert.AreEqual("HeavySalvage",current.Composer.LastResult);}
     report.rows.Add(new Row{seed=seed,zone=id,source=source?.BlueprintName??"absent",hedges=current.EligibleHedges,compatible=compatible,committed=committed,result=current.Composer.LastResult,moved=moved,nextRng=current.Tail,milliseconds=timer.Elapsed.TotalMilliseconds});Write("census.json",report);
    }
    report.compatible=report.rows.Count(r=>r.compatible);report.committed=report.rows.Count(r=>r.committed);report.seedsWithCommit=report.rows.Where(r=>r.committed).Select(r=>r.seed).Distinct().Count();Write("census.json",report);
    Assert.GreaterOrEqual(report.committed,3,"Release gate, never permission to force a source.");Assert.GreaterOrEqual(report.seedsWithCommit,2);Assert.GreaterOrEqual(report.committed*2,report.compatible);
   }}finally{typeof(SettlementManager).GetProperty("Current",All).SetValue(null,priorSettlement);}
  }

  [TestCase("untouched")][TestCase("held")][TestCase("parked")][TestCase("removed")]
  public void ActualAcceptedLayoutAndGripOrAbsenceSurviveFullSaveWithoutReplay(string stateName)
  {
   var oldActive=SettlementRuntime.ActiveZone;
   try{using(var scope=new HaulingContentScope())
   {
    var selection=OverworldZoneManager.CreateDetached(scope.Factory,64,true).Exploration.Entries.Where(e=>e.Family.ToString()=="HeavySalvage").Select(e=>e.ZoneID).ToArray();Assert.IsNotEmpty(selection);
    Manager manager=null;Zone zone=null;Entity load=null;
    foreach(string id in selection){scope.Seed(unchecked(64^FormationSelector.StableIndex(id,int.MaxValue)));var candidate=new Manager(scope.Factory,64,false);var graph=candidate.GetZone(id);if(graph!=null&&candidate.Exploration.DispositionFor(id)==2){manager=candidate;zone=graph;load=candidate.Receipt.Owners.Single();break;}}
    Assert.NotNull(zone,"Fixed seed64 source absence is an honest native gate failure, never permission to manufacture a load.");
    var anchor=zone.GetEntityPosition(load);(int x,int y) axis=default;bool found=false;
    foreach(var d in new[]{(x:0,y:1),(x:1,y:0),(x:0,y:-1),(x:-1,y:0)})
    {bool wall=Enumerable.Range(-6,13).Where(i=>i!=0).All(i=>zone.GetCell(anchor.x+i*d.y,anchor.y-i*d.x)?.Occupants.Count(e=>e.BlueprintName=="Hedge")==1);if(!wall)continue;bool shoulder=true;for(int v=-4;v<=-1;v++)for(int u=-2;u<=2;u++){var cell=zone.GetCell(anchor.x+u*d.y+v*d.x,anchor.y-u*d.x+v*d.y);if(cell==null||cell.BlocksMovement())shoulder=false;}if(shoulder){axis=d;found=true;break;}}
    Assert.True(found);var player=scope.Factory.CreateEntity("Player");Assert.True(zone.AddEntity(player,anchor.x-axis.x,anchor.y-axis.y));manager.SetActiveZone(zone);SettlementRuntime.ActiveZone=zone;
    if(stateName=="held"||stateName=="parked"){Assert.AreEqual(DragVerdict.Ok,DragSystem.TryGrab(player,load,zone));Assert.True(MovementSystem.TryMove(player,zone,-axis.x,-axis.y));if(stateName=="parked"){Assert.True(MovementSystem.TryMove(player,zone,-axis.x,-axis.y));DragSystem.Release(player);}}
    if(stateName=="removed"){var health=load.GetPart<DestructiblePart>();if(health!=null)Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(load,health.HP,player,zone));else Assert.True(zone.RemoveEntity(load));Assert.Null(zone.GetEntityCell(load));}
    string idSaved=zone.ZoneID,loadID=load.ID;int speed=player.GetStatValue("Speed");var loadAt=zone.GetEntityPosition(load);
    if(stateName!="held"){var away=manager.GetZone(ReferenceGladePlan.ZoneID);Cell target=null;away.ForEachCell((c,x,y)=>{if(target==null&&away.CanPlaceFootprint(player,x,y))target=c;});Assert.NotNull(target);Assert.True(zone.RemoveEntity(player));Assert.True(away.AddEntity(player,target.X,target.Y));manager.SetActiveZone(away);SettlementRuntime.ActiveZone=away;}
    var expected=zone.GetReadOnlyEntities().ToDictionary(e=>e.ID,e=>(at:zone.GetEntityPosition(e),shape:Shape(e)));manager.UnloadZone(idSaved);Assert.AreSame(zone,manager.GetZone(idSaved));
    var state=GameSessionState.Capture("heavy-salvage",stateName,manager,null,player);GameSessionState loaded;using(var stream=new MemoryStream()){state.Save(new SaveWriter(stream));stream.Position=0;loaded=GameSessionState.Load(new SaveReader(stream,scope.Factory));}
    var restored=(OverworldZoneManager)loaded.ZoneManager;Assert.AreEqual(10,restored.Exploration.Version);Assert.AreEqual(2,restored.Exploration.DispositionFor(idSaved));var returned=restored.GetZone(idSaved);Assert.AreNotSame(zone,returned);Assert.AreEqual(expected.Count,returned.EntityCount);foreach(var e in returned.GetReadOnlyEntities()){Assert.True(expected.ContainsKey(e.ID));Assert.AreEqual(expected[e.ID].at,returned.GetEntityPosition(e));Assert.AreEqual(expected[e.ID].shape,Shape(e));}
    var saved=returned.GetReadOnlyEntities().SingleOrDefault(e=>e.ID==loadID);Assert.AreEqual(stateName!="removed",saved!=null);if(saved!=null){Assert.AreNotSame(load,saved);Assert.AreEqual(loadAt,returned.GetEntityPosition(saved));}Assert.AreEqual(speed,loaded.Player.GetStatValue("Speed"));
    if(stateName=="held"){Assert.AreSame(saved,DragSystem.GetDragged(loaded.Player));Assert.AreSame(loaded.Player,DragSystem.GetDragger(saved));DragSystem.Release(loaded.Player);Assert.AreEqual(100,loaded.Player.GetStatValue("Speed"));}
    else Assert.False(loaded.Player.HasPart<DragPart>());
    restored.UnloadZone(idSaved);Assert.AreSame(returned,restored.GetZone(idSaved));Assert.AreEqual(stateName=="removed"?0:1,returned.GetReadOnlyEntities().Count(e=>e.ID==loadID));
   }}finally{SettlementRuntime.ActiveZone=oldActive;}
  }
 }
}
