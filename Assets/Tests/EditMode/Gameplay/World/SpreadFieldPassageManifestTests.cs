using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class SpreadFieldPassageManifestTests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
        readonly List<(FieldInfo f,object value,List<DictionaryEntry> entries)> globals=new List<(FieldInfo,object,List<DictionaryEntry>)>();
        DensityLootTestScope scope; object settlement;
        [SetUp] public void Setup()
        {
            foreach(var f in typeof(LootTableRegistry).GetFields(All)){if(!f.IsStatic)continue;var value=f.GetValue(null);var rows=new List<DictionaryEntry>();if(value is IDictionary d)foreach(DictionaryEntry row in d)rows.Add(row);globals.Add((f,value,rows));}
            settlement=SettlementManager.Current;scope=new DensityLootTestScope();
        }
        [TearDown] public void Cleanup()
        {
            try{scope?.Dispose();}finally{foreach(var g in globals){if(!g.f.IsInitOnly&&!g.f.IsLiteral)g.f.SetValue(null,g.value);if(g.value is IDictionary d){d.Clear();foreach(var row in g.entries)d.Add(row.Key,row.Value);}}globals.Clear();typeof(SettlementManager).GetProperty("Current",All).SetValue(null,settlement);}
        }
        static SpreadExplorationPlan Restore(OverworldZoneManager manager,string wire)
        {
            var world=new Entity();world.Properties[SpreadExplorationPlan.PropertyKey]=wire;
            if(wire.StartsWith("11|",StringComparison.Ordinal)||wire.StartsWith("12|",StringComparison.Ordinal))world.Properties[SpreadExplorationPlan.WorldKeyProperty]=manager.Exploration.WorldKey;
            try{return (SpreadExplorationPlan)typeof(SpreadExplorationPlan).GetMethod("Restore",All).Invoke(null,new object[]{manager,world});}
            catch(TargetInvocationException e)when(e.InnerException!=null){throw e.InnerException;}
        }
        static void Bind(OverworldZoneManager manager,SpreadExplorationPlan plan)=>typeof(OverworldZoneManager).GetProperty("Exploration",All).SetValue(manager,plan);
        static uint Variant(int seed,string id)
        {unchecked{uint h=2166136261u^(uint)seed;foreach(char c in "SpreadExploration.v2|family-variant|"+id)h=(h^c)*16777619u;h^=h>>16;h*=0x7feb352du;h^=h>>15;h*=0x846ca68bu;return (h^(h>>16))%4;}}
        [TestCase(1)][TestCase(64)][TestCase(1729)]
        public void FreshCurrentUsesFrozenHedgerowFourWayAllocationWithoutGeneratingGraphs(int seed)
        {
            var m=OverworldZoneManager.CreateDetached(scope.Factory,seed,true);var plan=m.Exploration;
            Assert.AreEqual(12,plan.Version);Assert.AreEqual(9,Convert.ToInt32(Enum.Parse(typeof(SpreadExplorationFamily),"FieldPassage")),"Roadmap F12 appends after persisted enum8; no reserved holes.");
            var rows=plan.Entries.Where(e=>e.Family.ToString()=="FieldPassage").ToArray();Assert.IsNotEmpty(rows,"Fixed corpus must provide real selected opportunities before any geometry claim.");
            foreach(var e in plan.Entries.Where(e=>e.Family!=SpreadExplorationFamily.None&&FormationSelector.For(BiomeType.Spread,e.ZoneID)==Formation.Hedgerow))
            {
                // v10 fills previously quiet rows with domestic sites. Every
                // other hedgerow still follows the exact shipped four-way rule;
                // full native v9 preservation is frozen in SpreadResidentsManifestTests.
                if(e.Family==SpreadExplorationFamily.SeedKeepersPlot||e.Family==SpreadExplorationFamily.WaysideKitchen)continue;
                Assert.AreEqual(new[]{"OccupiedBank","CollectorReturn","FieldPassage","HeavySalvage"}[Variant(seed,e.ZoneID)],e.Family.ToString(),e.ZoneID);
            }
            foreach(var e in rows){Assert.True(e.PlacementEligible);Assert.AreEqual(Formation.Hedgerow,FormationSelector.For(BiomeType.Spread,e.ZoneID));Assert.AreNotEqual(ReferenceGladePlan.ZoneID,e.ZoneID);Assert.AreNotEqual(m.RareEncounters?.PairZoneID,e.ZoneID);Assert.AreNotEqual(m.RareEncounters?.ViperZoneID,e.ZoneID);Assert.AreNotEqual(m.Wayhouse?.ZoneID,e.ZoneID);}
            string wire=SpreadExplorationPlan.BindForSave(m,null).GetProperty(SpreadExplorationPlan.PropertyKey);Bind(m,Restore(m,wire));
            Assert.AreEqual(wire,SpreadExplorationPlan.BindForSave(m,null).GetProperty(SpreadExplorationPlan.PropertyKey));Assert.Zero(m.CachedZoneCount);Assert.Zero(m.Exploration.RetainedGraphCount);
        }
        [TestCase(2,2)][TestCase(3,2)][TestCase(4,7)][TestCase(5,7)]
        public void LiteralEarlierHedgerowAssignmentsRemainByteExactWithoutBackfill(int version,int family)
        {
            var m=OverworldZoneManager.CreateDetached(scope.Factory,64,false);const string id="Overworld.13.11.0";
            Assert.AreEqual(Formation.Hedgerow,FormationSelector.For(BiomeType.Spread,id));string wire=version+"|64|1\n"+id+"|3|"+family+"|1|0";
            Bind(m,Restore(m,wire));Assert.AreEqual(version,m.Exploration.Version);Assert.AreEqual(family,(int)m.Exploration.Entries.Single().Family);
            Assert.AreEqual(wire,SpreadExplorationPlan.BindForSave(m,null).GetProperty(SpreadExplorationPlan.PropertyKey));Assert.Zero(m.CachedZoneCount);
        }
        [TestCase(999,"Hedgerow",0)][TestCase(4,"Hedgerow",9)][TestCase(5,"FlowerMeadow",9)]
        public void FutureHeaderOldVersionNewFamilyAndWrongHabitatRefuse(int version,string formation,int family)
        {
            var m=OverworldZoneManager.CreateDetached(scope.Factory,64,false);
            string id=SpreadExplorationPlan.Create(m).Entries.First(e=>e.PlacementEligible&&FormationSelector.For(BiomeType.Spread,e.ZoneID).ToString()==formation).ZoneID;
            Assert.Throws<InvalidDataException>(()=>Restore(m,version+"|64|1\n"+id+"|3|"+family+"|1|0"));Assert.False(m.Exploration.Enabled);Assert.Zero(m.CachedZoneCount);
        }
    }
}
