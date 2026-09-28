using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Bounded native generation census, not ordinary acquisition or balance evidence.</summary>
    public sealed class SpreadDiscoveryCensusTests
    {
        [Serializable] public sealed class Row
        {
            public string zone, formation;
            public int containers, locked, items, value, creatures, ripeRows, rareOwners;
        }
        [Serializable] public sealed class Report
        {
            public int seed, eligible;
            public string pair, viper, runtime;
            public List<string> candidates = new List<string>();
            public List<Row> rows = new List<Row>();
        }
        [TestCase(1)] [TestCase(64)] [TestCase(1729)]
        public void RecordCurrentBoundedGeneratedSources(int seed)
        {
            using(var scope=new DensityLootTestScope())
            {
                var manager=OverworldZoneManager.CreateDetached(scope.Factory,seed);
                var report=new Report{seed=seed,pair=manager.RareEncounters.PairZoneID,
                    viper=manager.RareEncounters.ViperZoneID,runtime=Application.unityVersion};
                var ids=new List<string>();
                for(int y=0;y<WorldMap.Height;y++)for(int x=0;x<WorldMap.Width;x++)
                {
                    string id=WorldMap.ToZoneID(x,y,0);
                    if(!SpreadRareEncounterPlan.IsEligible(manager,id))continue;
                    ids.Add(id);report.candidates.Add(id+"|"+FormationSelector.For(BiomeType.Spread,id));
                }
                report.eligible=ids.Count;
                // Same fixed geographic cohort on every run; never select higher-value containers.
                var sample=ids.GroupBy(id=>FormationSelector.For(BiomeType.Spread,id)).SelectMany(g=>g.Take(3))
                    .Concat(new[]{report.pair,report.viper}).Where(id=>!string.IsNullOrEmpty(id)).Distinct().ToArray();
                foreach(string id in sample)
                {
                    scope.Seed(seed^id.GetHashCode());
                    var zone=manager.GetZone(id);Assert.NotNull(zone,id);
                    var owners=zone.GetReadOnlyEntities().ToArray();
                    var containers=owners.Where(e=>e.HasPart<ContainerPart>()).ToArray();
                    var items=containers.SelectMany(e=>e.GetPart<ContainerPart>().Contents).ToArray();
                    report.rows.Add(new Row{zone=id,formation=FormationSelector.For(BiomeType.Spread,id).ToString(),
                        containers=containers.Length,locked=containers.Count(e=>e.GetPart<ContainerPart>().IsLocked),
                        items=items.Length,value=items.Sum(e=>e.GetPart<CommercePart>()?.Value??0),
                        creatures=owners.Count(e=>e.HasTag("Creature")),ripeRows=owners.Count(e=>e.HasPart<FieldHarvestPart>()),
                        rareOwners=owners.Count(e=>e.HasTag("Creature")&&e.Properties.ContainsKey(SpreadRareEncounterBuilder.SourceKey))});
                }
                Assert.Greater(report.eligible,0);Assert.Greater(report.rows.Count,6);
                string phase=Environment.GetEnvironmentVariable("COO_DISCOVERY_CENSUS_PHASE")??"current";
                Assert.That(phase,Does.Match("^[a-z-]+$"));
                string dir=Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/SpreadDiscoveryExpeditions/Census"));
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir,phase+"-"+seed+".json"),JsonUtility.ToJson(report,true));
                TestContext.WriteLine("seed="+seed+" eligible="+report.eligible+" generated="+report.rows.Count+" phase="+phase);
            }
        }
    }
}
