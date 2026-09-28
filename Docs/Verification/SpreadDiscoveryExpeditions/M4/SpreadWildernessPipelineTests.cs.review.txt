using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class SpreadWildernessPipelineTests
    {
        DensityLootTestScope scope;
        [SetUp]public void Setup(){scope=new DensityLootTestScope();}
        [TearDown]public void Cleanup(){scope.Dispose();}
        [TestCase(1)][TestCase(64)][TestCase(1729)]
        public void ActualColdPipelineBindsOnlySelectedOrdinarySourceInstances(int seed)
        {
            var manager=OverworldZoneManager.CreateDetached(scope.Factory,seed);int expected=0,actual=0;
            var method=typeof(OverworldZoneManager).GetMethod("GetPipelineForZone",BindingFlags.Instance|BindingFlags.NonPublic);
            for(int y=0;y<WorldMap.Height;y++)for(int x=0;x<WorldMap.Width;x++)
            {
                string id=WorldMap.ToZoneID(x,y);var p=(ZoneGenerationPipeline)method.Invoke(manager,new object[]{id});
                string kind=SpreadWildernessSituationPlan.Select(manager,id,manager.Wayhouse.ZoneID);bool selected=kind=="cargo"||kind=="shelter";
                var candidates=p.Builders.OfType<SpreadWildernessSituationBuilder>().ToArray();Assert.AreEqual(selected?1:0,candidates.Length,id+" "+kind);if(!selected)continue;
                expected++;actual+=candidates.Length;var builder=candidates.Single();Assert.AreEqual(4300,builder.Priority);
                var terrain=p.Builders.OfType<SpreadCompositionBuilder>().Single();var actors=p.Builders.OfType<PopulationBuilder>().Single();var caches=p.Builders.OfType<ContainerBuilder>().Single();
                Assert.True(actors.CaptureSourceReceipts);Assert.True(caches.CaptureSourceReceipts);
                foreach(var pair in new[]{("manager",(object)manager),("terrain",terrain),("population",actors),("containers",caches)})
                    Assert.AreSame(pair.Item2,typeof(SpreadWildernessSituationBuilder).GetField(pair.Item1,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(builder),id+" "+pair.Item1);
                Assert.AreEqual(manager.Wayhouse.ZoneID,typeof(SpreadWildernessSituationBuilder).GetField("excluded",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(builder));
                Assert.Greater(Array.IndexOf(p.Builders.ToArray(),builder),Array.FindLastIndex(p.Builders.ToArray(),b=>b is HaulablePropBuilder),id);
                Assert.AreNotEqual(manager.Wayhouse.ZoneID,id);Assert.AreNotEqual(manager.RareEncounters.PairZoneID,id);Assert.AreNotEqual(manager.RareEncounters.ViperZoneID,id);
            }
            Assert.Greater(expected,0);Assert.AreEqual(expected,actual);Assert.AreEqual(0,manager.CachedZoneCount,"Pipeline inspection does not generate a graph.");
        }
    }
}
