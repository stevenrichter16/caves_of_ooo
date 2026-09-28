using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using Newtonsoft.Json;
namespace CavesOfOoo.Tests
{
 // Private source feasibility. Stable-hash/Unity-shim core runner, not native or ordinary travel.
 public sealed class HaulingFeasibilityCensus
 {
  sealed class Probe:IZoneBuilder
  {readonly Action<Zone> action;public Probe(int priority,Action<Zone> a){Priority=priority;action=a;}public string Name=>"PrivateHaulSourceObservation";public int Priority{get;}public bool BuildZone(Zone z,EntityFactory f,Random r){action(z);return true;}}
  sealed class Manager:OverworldZoneManager
  {
   public Entity[] Produced=Array.Empty<Entity>();public int ProducerCount;
   public Manager(EntityFactory f,int seed):base(f,seed,activate:false){}
   protected override ZoneGenerationPipeline GetPipelineForZone(string id)
   {
    var p=base.GetPipelineForZone(id);ProducerCount=p.Builders.OfType<HaulablePropBuilder>().Count();Entity[] before=null;
    p.AddBuilder(new Probe(4199,z=>before=z.GetReadOnlyEntities().ToArray()));
    p.AddBuilder(new Probe(4201,z=>Produced=z.GetReadOnlyEntities().Except(before).ToArray()));return p;
   }
  }
  [TestCase(1)][TestCase(64)][TestCase(1729)]
  public void ObserveFixedMetadataCohortBeforeAnyHaulableProductionChange(int seed)
  {
   using(var scope=new DensityLootTestScope())
   {
    var manager=new Manager(scope.Factory,seed);
    var selected=manager.Exploration.Entries.Where(e=>e.PlacementEligible&&(FormationSelector.For(BiomeType.Spread,e.ZoneID)==Formation.Fallow||FormationSelector.For(BiomeType.Spread,e.ZoneID)==Formation.Hedgerow))
      .OrderBy(e=>SpreadExplorationPlan.Rank(seed,e.ZoneID,"F9-feasibility-v1")).ThenBy(e=>e.ZoneID,StringComparer.Ordinal).Take(20).Select(e=>e.ZoneID).ToArray();
    Assert.Zero(manager.CachedZoneCount);var rows=new List<object>();int sourceCount=0,compatible=0,admitted=0;
    foreach(string id in selected)
    {
     scope.Seed(unchecked(seed^FormationSelector.StableIndex(id,int.MaxValue)));var z=manager.GetZone(id);Assert.AreEqual(1,manager.ProducerCount);
     var sources=manager.Produced;Assert.LessOrEqual(sources.Length,1,"Only4200 haulable producer is observed between exact priority sentinels.");sourceCount+=sources.Length;
     if(sources.Length==0){rows.Add(new{zone=id,source=(string)null,reason="ordinary-roll-or-placement-empty"});continue;}
     var e=sources.Single();var origin=z.GetEntityPosition(e);var player=scope.Factory.CreateEntity("Player");var verdict=DragRules.CanDrag(player,e);
     if(e.BlueprintName!="FallenBeam"&&e.BlueprintName!="HaulBarrel"){rows.Add(new{zone=id,source=e.BlueprintName,sourceId=e.ID,origin,verdict=verdict.ToString(),reason="source-not-beam-or-barrel"});continue;}
     compatible++;int gaps=0,trials=0;var options=new List<object>();
     var g=new SpreadWildernessSituationBuilder.Geometry(z,new HashSet<Entity>{e});var original=new SpreadWildernessSituationBuilder.Geometry(z,new HashSet<Entity>());
     for(int y=4;y<Zone.Height-4&&trials<256&&options.Count==0;y++)for(int x=4;x<Zone.Width-4&&trials<256&&options.Count==0;x++)
     {
      if(!g.Place(x,y)||(x,y)==origin)continue;
      foreach(var axis in new[]{(x:1,y:0),(x:-1,y:0),(x:0,y:1),(x:0,y:-1)})
      {
       int px=-axis.y,py=axis.x;
       if(!FixedBarrier(z,x+px,y+py)||!FixedBarrier(z,x-px,y-py))continue;
       var start=(x:x-axis.x,y:y-axis.y);var goal=(x:x+axis.x,y:y+axis.y);
       if(!g.Place(start.x,start.y)||!g.Place(goal.x,goal.y))continue;gaps++;
       foreach(bool turn in new[]{false,true})
       {
        if(++trials>256)break;
        var trail=new[]{(x:x-2*axis.x,y:y-2*axis.y),turn?(x:x-2*axis.x+px,y:y-2*axis.y+py):(x:x-3*axis.x,y:y-3*axis.y),turn?(x:x-2*axis.x+2*px,y:y-2*axis.y+2*py):(x:x-4*axis.x,y:y-4*axis.y)};
        if(trail.Any(a=>!g.Place(a.x,a.y)))continue;var park=trail[1];var cut=(x,y);
        if(!g.PreservesAgainst(original,new[]{cut})||!g.PreservesAgainst(original,new[]{park}))continue;
        int before=Distance(g,start,goal,cut),after=Distance(g,trail[2],goal,park);
        if(before<0||before>60||after<0||before<=trail.Length*2+after)continue;
        var proof=Prove(z,scope.Factory,e,player,origin,cut,start,trail,goal);
        Assert.True(proof.success,"Admitted virtual layout must survive actual DragSystem/MovementSystem core commands.");
        options.Add(new{cut,start,goal,parking=park,trail,variant=turn?"roomy-corner":"straight-pull",beforeSteps=before,afterSteps=after,haulSteps=trail.Length,conservativeUsefulMargin=before-(trail.Length*2+after),proof.success,proof.initialSpeed,proof.heldSpeed,proof.restoredSpeed,proof.finalLoad});
        break;
       }
       if(options.Count>0||trials>=256)break;
      }
     }
     if(options.Count>0)admitted++;
     rows.Add(new{zone=id,formation=FormationSelector.For(BiomeType.Spread,id).ToString(),source=e.BlueprintName,sourceId=e.ID,origin,verdict=verdict.ToString(),strength=player.GetStatValue("Strength"),weight=DragRules.WeightOf(e),narrowGapScreens=gaps,trials,reason=options.Count>0?"useful-actual-drag-proof":trials>=256?"layout-budget":"no-useful-existing-gap-and-parking",options});
    }
    Directory.CreateDirectory("/tmp/coo-questfree-implementation/e4/hauling-plan/census");
    File.WriteAllText("/tmp/coo-questfree-implementation/e4/hauling-plan/census/seed-"+seed+".json",JsonConvert.SerializeObject(new{seed,selection="First20 eligible Fallow/Hedgerow metadata ranks, F9-feasibility-v1, frozen before GetZone",selected,sourceCount,compatible,admitted,rows,boundary="Private copied core runner with disclosed stable hash and Unity shims. Real pipeline/drag commands, no native keyboard, frame, scheduler or ordinary travel claim. No gameplay source changes. Exact4200 owner observed rather than granted receipt authority. Candidate diagnostic relocates/restores only that source and a factory Player; no source grant."},Formatting.Indented));
   }
  }
  static bool FixedBarrier(Zone z,int x,int y)=>z.GetCell(x,y).Objects.Any(e=>(e.BlueprintName=="Hedge"||e.BlueprintName=="Tree")&&e.GetPart<PhysicsPart>()?.Solid==true&&!e.HasTag("Creature"));
  static int Distance(SpreadWildernessSituationBuilder.Geometry g,(int x,int y)start,(int x,int y)goal,(int x,int y)block)
  {
   var blocked=new HashSet<(int,int)>{block};if(!g.Walk(start.x,start.y,blocked)||!g.Walk(goal.x,goal.y,blocked))return-1;
   var seen=new bool[Zone.Width,Zone.Height];var q=new Queue<(int x,int y,int d)>();q.Enqueue((start.x,start.y,0));seen[start.x,start.y]=true;
   while(q.Count>0){var p=q.Dequeue();if((p.x,p.y)==goal)return p.d;if(p.d==60)continue;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){int x=p.x+dx,y=p.y+dy;if(dx==0&&dy==0||!g.Walk(x,y,blocked)||seen[x,y])continue;seen[x,y]=true;q.Enqueue((x,y,p.d+1));}}return-1;
  }
  static (bool success,int initialSpeed,int heldSpeed,int restoredSpeed,(int,int)finalLoad) Prove(Zone z,EntityFactory f,Entity load,Entity player,(int x,int y)origin,(int x,int y)cut,(int x,int y)start,(int x,int y)[]trail,(int x,int y)goal)
  {
   var original=z.GetReadOnlyEntities().ToArray();var positions=original.Select(z.GetEntityPosition).ToArray();int initial=player.GetStatValue("Speed"),held=initial;bool success=false;var landed=(-1,-1);
   try
   {
    Assert.True(z.MoveEntity(load,cut.x,cut.y));Assert.True(z.AddEntity(player,start.x,start.y));Assert.AreEqual(DragVerdict.Ok,DragSystem.TryGrab(player,load,z));held=player.GetStatValue("Speed");
    foreach(var p in trail){var old=z.GetEntityPosition(player);Assert.True(MovementSystem.TryMove(player,z,p.x-old.x,p.y-old.y));Assert.AreSame(load,DragSystem.GetDragged(player));Assert.AreEqual(old,z.GetEntityPosition(load));}
    landed=z.GetEntityPosition(load);DragSystem.Release(player);Assert.AreEqual(initial,player.GetStatValue("Speed"));Assert.False(z.GetCell(cut.x,cut.y).BlocksMovement());Assert.True(z.GetCell(landed.Item1,landed.Item2).BlocksMovement());success=true;
   }
   finally
   {DragSystem.Release(player);if(z.GetEntityCell(player)!=null)z.RemoveEntity(player);Assert.True(z.MoveEntity(load,origin.x,origin.y));CollectionAssert.AreEquivalent(original,z.GetReadOnlyEntities());for(int i=0;i<original.Length;i++)Assert.AreEqual(positions[i],z.GetEntityPosition(original[i]));}
   return(success,initial,held,player.GetStatValue("Speed"),landed);
  }
 }
}
