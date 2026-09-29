using System;
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
 public sealed class SpreadHuntPipelineTests
 {
  const BindingFlags All=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
  sealed class Probe:IZoneBuilder{readonly int priority;readonly Func<Zone,EntityFactory,Random,bool>action;public Probe(int p,Func<Zone,EntityFactory,Random,bool>a){priority=p;action=a;}public string Name=>"HuntCensusProbe";public int Priority=>priority;public bool BuildZone(Zone z,EntityFactory f,Random r)=>action(z,f,r);}
  sealed class Manager:OverworldZoneManager
  {
   readonly bool baseline;public SpreadCompositionBuilder Terrain;public PopulationBuilder Population;public SpreadExplorationBuilder Composer;public Entity[] Before,Originals;public string[] Shapes;public (int x,int y)[] Anchors;public int Hostiles,Birds,Gear,OmittedStock,Tail;public bool Compatible,TransactionPreserved;public string HostileType;
   public Manager(EntityFactory f,int seed,bool baseline=false):base(f,seed){this.baseline=baseline;}
   public ZoneGenerationPipeline Inspect(string id)=>GetPipelineForZone(id);
   protected override ZoneGenerationPipeline GetPipelineForZone(string id)
   {
    var p=base.GetPipelineForZone(id);if(Exploration.Find(id)?.Family.ToString()!="HuntThroughCover")return p;
    Terrain=p.Builders.OfType<SpreadCompositionBuilder>().Single();Population=p.Builders.OfType<PopulationBuilder>().Single();Composer=p.Builders.OfType<SpreadExplorationBuilder>().Single();if(baseline)p.RemoveBuilders<SpreadExplorationBuilder>();
    p.RemoveBuilders<PopulationBuilder>();Func<bool> unchanged=null;HashSet<Entity> others=null;
    p.AddBuilder(new Probe(4000,(z,f,r)=>{if(!Population.BuildZone(z,f,r))return false;Before=z.GetReadOnlyEntities().ToArray();Shapes=Before.Select(Shape).ToArray();Anchors=Before.Select(z.GetEntityPosition).ToArray();var source=Population.SourceReceipt;var ambient=Population.AmbientSourceReceipt;var bird=ambient?.Owners.FirstOrDefault(e=>e.BlueprintName=="Magpie");Hostiles=source?.Owners.Count??0;Birds=ambient?.Owners.Count(e=>e.BlueprintName=="Magpie")??0;HostileType=source?.Owners.FirstOrDefault()?.BlueprintName;Originals=(source?.Owners??Array.Empty<Entity>()).Concat(bird==null?Array.Empty<Entity>():new[]{bird}).ToArray();Compatible=source?.IsCurrent==true&&ambient?.IsCurrent==true&&Hostiles>=1&&Hostiles<=2&&bird!=null;Gear=source?.Owners.Sum(e=>DensityLootTestScope.Gear(e).Count())??0;others=Before.Except(Originals).ToHashSet();unchanged=Proof(z,others.Where(e=>!HuntFixture.IsBareGround(e)));return true;}));
    p.AddBuilder(new Probe(4002,(z,f,r)=>{bool changed=Originals.Length>0&&Originals.All(e=>HuntFixture.Spatial(e)==null);TransactionPreserved=unchanged()&&others.All(e=>HuntFixture.Spatial(e)==z);if(changed){Assert.AreEqual(Before.Length-Originals.Length+2,z.EntityCount);Assert.AreEqual(2,z.GetReadOnlyEntities().Count(e=>!Before.Contains(e)));}else CollectionAssert.AreEquivalent(Before,z.GetReadOnlyEntities());return true;}));
    p.AddBuilder(new Probe(int.MaxValue,(z,f,r)=>{Tail=r.Next();if(baseline&&Originals.Length>0)OmittedStock=DensityLootTestScope.Gear(Originals.Last()).Count();return true;}));return p;
   }
  }
  static Func<bool> Proof(Zone z,IEnumerable<Entity> e)=>(Func<bool>)typeof(SpreadGenerationReceipt).GetMethod("CaptureFinalState",All).Invoke(null,new object[]{z,e});
  static string Shape(Entity e)=>e.BlueprintName+"|"+e.GetPart<RenderPart>()?.DisplayName+"|"+e.GetPart<PhysicsPart>()?.Weight+"|"+string.Join(";",e.Parts.Select(p=>p.Name))+"|gear:"+string.Join(";",DensityLootTestScope.Gear(e).Select(x=>x.BlueprintName+":"+(x.GetPart<StackerPart>()?.StackCount??1)));
  static void Content(HaulingContentScope s){string candidate=Environment.GetEnvironmentVariable("COO_HUNT_OBJECTS");if(!string.IsNullOrEmpty(candidate))s.Factory.LoadBlueprints(File.ReadAllText(candidate));Assert.True(s.Factory.Blueprints.ContainsKey("Furrowstalker"),"Actual content required; no fixture hunter for census.");}
  static string[] Selected(OverworldZoneManager m)=>m.Exploration.Entries.Where(e=>e.Family.ToString()=="HuntThroughCover").Select(e=>e.ZoneID).ToArray();
  [Serializable]public sealed class Selection{public int seed;public string[] fullAssignment,selected;}
  [Serializable]public sealed class Row{public int seed,hostiles,birds,gear,omittedMagpieStock,baselineTail,currentTail;public string zone,variant,hostile,result,hunter,prey;public bool compatible,committed,transactionPreserved;public double milliseconds;}
  [Serializable]public sealed class Report{public string boundary;public Selection[] selection;public List<Row> rows=new List<Row>();public int compatible,committed,seedsWithCommit;}
  static string CensusOutputFolder()
  {
   string folder=Environment.GetEnvironmentVariable("COO_HUNT_CENSUS_OUTPUT");
#if UNITY_EDITOR
   if(string.IsNullOrEmpty(folder))folder=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Docs","Verification","QuestFreeExploration","F11","NativeCensus",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ")+"-"+Guid.NewGuid().ToString("N")));
#endif
   return folder;
  }
  static void Write(string folder,string name,object value){if(string.IsNullOrEmpty(folder))return;Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,name),JsonUtility.ToJson(value,true));}
  [Test]public void HuntComposerRunsAfterOriginalPopulationAndBeforeMagpieStock()
  {using(var s=new HaulingContentScope()){var m=new Manager(s.Factory,64);string id=Selected(m).First();var p=m.Inspect(id);var composer=p.Builders.OfType<SpreadExplorationBuilder>().Single();Assert.AreEqual(4001,composer.Priority);Assert.True((bool)typeof(SpreadCompositionBuilder).GetField("CaptureCoverSources",All).GetValue(m.Terrain));Assert.Less(composer.Priority,p.Builders.OfType<TradeStockBuilder>().Single().Priority);}}
  [Test]public void FixedThreeSeedVersionEightActualSourcesMeetBothVariantReleaseFloor()
  {using(var s=new HaulingContentScope())
   {
    string outputFolder=CensusOutputFolder();if(!string.IsNullOrEmpty(outputFolder))TestContext.WriteLine("Hunt census output: "+outputFolder);
    Content(s);var report=new Report{boundary="All prospective v8 full assignments frozen before GetZone. Actual blueprints and original population at4000; same-v8 composer-disabled comparator. New pair replaces whole hostile group and one pre-stock Magpie, intentionally reducing later stock/RNG draws. No native AI, input or visual proof.",selection=new[]{1,64,1729}.Select(seed=>{var m=OverworldZoneManager.CreateDetached(s.Factory,seed,true);Assert.AreEqual(8,m.Exploration.Version);Assert.Zero(m.CachedZoneCount);return new Selection{seed=seed,fullAssignment=m.Exploration.Entries.Select(e=>e.ZoneID+"|"+e.PlacementEligible+"|"+e.Family+"|"+e.Topology).ToArray(),selected=Selected(m)};}).ToArray()};Write(outputFolder,"selection-before-generation.json",report);
    foreach(var selection in report.selection)foreach(string id in selection.selected)
    {
     int seed=selection.seed,local=unchecked(seed^FormationSelector.StableIndex(id,int.MaxValue));s.Seed(local);var baseline=new Manager(s.Factory,seed,true);var stock=baseline.GetZone(id);Assert.NotNull(stock);
     s.Seed(local);var current=new Manager(s.Factory,seed);var timer=System.Diagnostics.Stopwatch.StartNew();var z=current.GetZone(id);timer.Stop();Assert.NotNull(z,id);CollectionAssert.AreEqual(baseline.Shapes,current.Shapes,id);CollectionAssert.AreEqual(baseline.Anchors,current.Anchors,id);Assert.True(current.TransactionPreserved,id);bool committed=current.Exploration.DispositionFor(id)==2;
     var pair=z.GetReadOnlyEntities().Where(e=>e.Parts.Any(p=>p.Name=="SpreadPredator")).ToArray();if(committed){Assert.True(current.Compatible);Assert.AreEqual(1,pair.Length);Assert.AreEqual("HuntThroughCover",current.Composer.LastResult);Assert.True(current.Originals.All(e=>HuntFixture.Spatial(e)==null));}else{Assert.Zero(pair.Length);Assert.True(current.Originals.All(e=>HuntFixture.Spatial(e)==z));Assert.AreEqual(baseline.Tail,current.Tail);}
     var role=pair.FirstOrDefault()?.Parts.Single(p=>p.Name=="SpreadPredator");var prey=role?.GetType().GetField("Prey").GetValue(role) as Entity;
     report.rows.Add(new Row{seed=seed,zone=id,variant=HuntFixture.Rank(seed,id,"hunt-variant")%2==0?"covered":"open",hostiles=current.Hostiles,birds=current.Birds,gear=current.Gear,hostile=current.HostileType,omittedMagpieStock=committed?baseline.OmittedStock:0,compatible=current.Compatible,committed=committed,transactionPreserved=current.TransactionPreserved,result=current.Composer.LastResult,hunter=pair.Length==0?"":z.GetEntityPosition(pair[0]).ToString(),prey=prey==null?"":z.GetEntityPosition(prey).ToString(),baselineTail=baseline.Tail,currentTail=current.Tail,milliseconds=timer.Elapsed.TotalMilliseconds});Write(outputFolder,"census.json",report);
    }
    report.compatible=report.rows.Count(r=>r.compatible);report.committed=report.rows.Count(r=>r.committed);report.seedsWithCommit=report.rows.Where(r=>r.committed).Select(r=>r.seed).Distinct().Count();Write(outputFolder,"census.json",report);
    Assert.GreaterOrEqual(report.committed,3);Assert.GreaterOrEqual(report.seedsWithCommit,2);Assert.GreaterOrEqual(report.committed*2,report.compatible);CollectionAssert.AreEquivalent(new[]{"covered","open"},report.rows.Where(r=>r.committed).Select(r=>r.variant).Distinct());
   }}
 }
}
