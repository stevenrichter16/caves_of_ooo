using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;
namespace CavesOfOoo.Tests
{
    public sealed class SpreadWildernessPipelineCensusTests
    {
        DensityLootTestScope scope;
        [SetUp]public void Setup(){scope=new DensityLootTestScope();}
        [TearDown]public void Cleanup(){scope.Dispose();}
        sealed class Probe:IZoneBuilder
        {readonly Action<Zone,Random> act;readonly int priority;internal Probe(int p,Action<Zone,Random>a){priority=p;act=a;}public string Name=>"TestOnlySnapshot";public int Priority=>priority;public bool BuildZone(Zone z,EntityFactory f,Random r){act(z,r);return true;}}
        sealed class ObservedManager:OverworldZoneManager
        {
            readonly bool baseline;internal Entity[] Before;internal (int x,int y)[] Positions;internal string[] ExactFacts;
            internal int NextRng;internal SpreadWildernessSituationBuilder Composer;internal PopulationBuilder Population;internal ContainerBuilder Containers;
            internal ObservedManager(EntityFactory f,int seed,bool baseline):base(f,seed){this.baseline=baseline;}
            protected override ZoneGenerationPipeline GetPipelineForZone(string id)
            {
                var p=base.GetPipelineForZone(id);Composer=p.Builders.OfType<SpreadWildernessSituationBuilder>().SingleOrDefault();
                Population=p.Builders.OfType<PopulationBuilder>().SingleOrDefault();Containers=p.Builders.OfType<ContainerBuilder>().SingleOrDefault();
                if(baseline)p.RemoveBuilders<SpreadWildernessSituationBuilder>();
                p.AddBuilder(new Probe(4299,(z,r)=>{Before=z.GetReadOnlyEntities().ToArray();Positions=Before.Select(e=>z.GetEntityPosition(e)).ToArray();ExactFacts=Before.Select(e=>Exact(e)).ToArray();}));
                p.AddBuilder(new Probe(int.MaxValue,(z,r)=>NextRng=r.Next()));return p;
            }
        }
        [Serializable]public sealed class Row{public string zone,kind,result;public int owners,containers,creatures,moved,nextRng;public string[] movedIds,rolledActors,rolledContainers;}
        [Serializable]public sealed class Report{public int seed,selected,committed,refused;public string boundary;public List<Row> rows=new List<Row>();}
        static string Stats(Entity e)=>string.Join("/",e.Statistics.OrderBy(s=>s.Key,StringComparer.Ordinal).Select(s=>s.Key+":"+s.Value.BaseValue+":"+s.Value.Bonus+":"+s.Value.Penalty+":"+s.Value.Value));
        static string Item(Entity e)=>e.BlueprintName+":"+(e.GetPart<StackerPart>()?.StackCount??1)+":"+Stats(e)+":"+string.Join(",",e.Tags.OrderBy(p=>p.Key).Select(p=>p.Key+"="+p.Value));
        static string Shape(Entity e)=>Item(e)+";stock="+string.Join(",",(e.GetPart<ContainerPart>()?.Contents??new List<Entity>()).Select(Item).OrderBy(x=>x,StringComparer.Ordinal))+";gear="+string.Join(",",DensityLootTestScope.Gear(e).Select(Item).OrderBy(x=>x,StringComparer.Ordinal));
        static string Exact(Entity e)=>e.ID+":"+Shape(e)+";links="+string.Join(",",(e.GetPart<ContainerPart>()?.Contents??new List<Entity>()).Concat(DensityLootTestScope.Gear(e)).Select(i=>i.ID+":"+i.GetPart<PhysicsPart>()?.InInventory?.ID+":"+i.GetPart<PhysicsPart>()?.Equipped?.ID).OrderBy(x=>x,StringComparer.Ordinal));
        static IEnumerable<string> IDs(){for(int y=0;y<WorldMap.Height;y++)for(int x=0;x<WorldMap.Width;x++)yield return WorldMap.ToZoneID(x,y);}
        [TestCase(1)][TestCase(64)][TestCase(1729)]
        public void ActualManagerColdGenerationChangesOnlyAuthorizedCoordinates(int seed)
        {
            var selection=OverworldZoneManager.CreateDetached(scope.Factory,seed);var ids=IDs().Where(id=>{var kind=SpreadWildernessSituationPlan.Select(selection,id,selection.Wayhouse.ZoneID);return kind=="cargo"||kind=="shelter";}).ToArray();
            Assert.Greater(ids.Length,0);var report=new Report{seed=seed,selected=ids.Length,boundary="Actual full manager cold GetZone, paired no-composer subclass vs current wiring; per-zone global loadout/trader/death RNG reset. Test-only last builder samples caller next RNG equally. No journey, player balance, save replay or native pixels claimed."};
            foreach(string id in ids)
            {
                int globalSeed=unchecked(seed^FormationSelector.StableIndex(id,int.MaxValue));scope.Seed(globalSeed);
                var baseline=new ObservedManager(scope.Factory,seed,true);var before=baseline.GetZone(id);Assert.NotNull(before,id+" baseline");
                var original=before.GetReadOnlyEntities().ToArray();var shapes=original.Select(Shape).ToArray();var positions=original.Select(e=>before.GetEntityPosition(e)).ToArray();string tiles=before.TileState.ToSaveString();
                scope.Seed(globalSeed);var current=new ObservedManager(scope.Factory,seed,false);var after=current.GetZone(id);Assert.NotNull(after,id+" current");Assert.NotNull(current.Composer,id+" missing actual pipeline integration");
                var owners=after.GetReadOnlyEntities().ToArray();Assert.AreEqual(original.Length,owners.Length,id+" owner count");CollectionAssert.AreEqual(shapes,owners.Select(Shape),id+" ordinary stock/loadouts/stats/order");Assert.AreEqual(baseline.NextRng,current.NextRng,id+" caller RNG");Assert.AreEqual(tiles,after.TileState.ToSaveString(),id+" tile state");
                CollectionAssert.AreEqual(current.Before,owners,id+" exact current references retained");CollectionAssert.AreEqual(current.ExactFacts,owners.Select(Exact),id+" exact current IDs/state/stock/links retained");
                var allowed=new HashSet<Entity>();string kind=SpreadWildernessSituationPlan.Select(current,id,current.Wayhouse.ZoneID);
                var cache=current.Containers.SourceReceipt.Owners.FirstOrDefault(e=>(e.BlueprintName=="Crate"||e.BlueprintName=="Sack")&&!e.GetPart<ContainerPart>().IsLocked);if(cache!=null)allowed.Add(cache);
                if(kind=="shelter")foreach(var actor in current.Population.SourceReceipt.Owners)allowed.Add(actor);
                var moved=new List<Entity>();for(int i=0;i<owners.Length;i++)
                {
                    Assert.AreEqual(positions[i],current.Positions[i],id+" source coordinate before late pass "+i);
                    var pos=after.GetEntityPosition(owners[i]);if(pos!=positions[i]){Assert.Contains(owners[i],allowed.ToList(),id+" unauthorized owner moved");moved.Add(owners[i]);}
                }
                bool committed=current.Composer.LastResult==kind;
                if(committed){report.committed++;Assert.Greater(moved.Count,0);Assert.Contains(cache,moved);}
                else{report.refused++;Assert.AreEqual(0,moved.Count,id+" optional refusal changed owner");}
                var snapshot=owners.Select(Exact).ToArray();Assert.AreSame(after,current.GetZone(id));CollectionAssert.AreEqual(snapshot,after.GetReadOnlyEntities().Select(Exact),id+" attach does not recompose");
                report.rows.Add(new Row{zone=id,kind=kind,result=current.Composer.LastResult,owners=owners.Length,containers=owners.Count(e=>e.HasPart<ContainerPart>()),creatures=owners.Count(e=>e.HasTag("Creature")),moved=moved.Count,movedIds=moved.Select(e=>e.ID).ToArray(),nextRng=current.NextRng,rolledActors=current.Population.SourceReceipt.Owners.Select(e=>e.BlueprintName).ToArray(),rolledContainers=current.Containers.SourceReceipt.Owners.Select(e=>e.BlueprintName).ToArray()});
            }
            string json=JsonUtility.ToJson(report,true);TestContext.WriteLine(json);string output=Environment.GetEnvironmentVariable("COO_M4_PIPELINE_CENSUS_OUTPUT");if(!string.IsNullOrEmpty(output)){Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"seed-"+seed+".json"),json);}
            Assert.Greater(report.committed,0,"At least one real complete pipeline must realize the bounded feature for this measured cohort.");
        }
    }
}
