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
    public sealed class SpreadCoolingManifestTests
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
            try{return (SpreadExplorationPlan)typeof(SpreadExplorationPlan).GetMethod("Restore",All).Invoke(null,new object[]{manager,world});}
            catch(TargetInvocationException e)when(e.InnerException!=null){throw e.InnerException;}
        }
        static void Bind(OverworldZoneManager manager,SpreadExplorationPlan plan)=>typeof(OverworldZoneManager).GetProperty("Exploration",All).SetValue(manager,plan);
        static uint Variant(int seed,string id)
        {unchecked{uint h=2166136261u^(uint)seed;foreach(char c in "SpreadExploration.v2|family-variant|"+id)h=(h^c)*16777619u;h^=h>>16;h*=0x7feb352du;h^=h>>15;h*=0x846ca68bu;return (h^(h>>16))%2;}}
        [TestCase(1)][TestCase(64)][TestCase(1729)]
        public void FreshCurrentSelectsOneFieldPrincipalWithoutGeneratingGraphs(int seed)
        {
            var m=OverworldZoneManager.CreateDetached(scope.Factory,seed,true);var p=m.Exploration;
            Assert.AreEqual(10,p.Version);Assert.AreEqual(10,Convert.ToInt32(Enum.Parse(typeof(SpreadExplorationFamily),"CoolingWorkPatch")));
            var fields=p.Entries.Where(e=>e.Family!=SpreadExplorationFamily.None&&FormationSelector.For(BiomeType.Spread,e.ZoneID)==Formation.FieldStrips).ToArray();
            Assert.IsNotEmpty(fields);Assert.True(fields.Any(e=>e.Family==SpreadExplorationFamily.CoolingWorkPatch));
            Assert.True(fields.Any(e=>e.Family==SpreadExplorationFamily.LastGleanings));
            // v10 adds residents to previously quiet rows in any formation. Keep
            // checking the exact v6 field variant for every nonresident row;
            // SpreadResidentsManifestTests freezes the complete native v9 map.
            foreach(var e in fields)
            {
                if(e.Family==SpreadExplorationFamily.SeedKeepersPlot||e.Family==SpreadExplorationFamily.WaysideKitchen)continue;
                Assert.AreEqual(Variant(seed,e.ZoneID)==0?"LastGleanings":"CoolingWorkPatch",e.Family.ToString(),e.ZoneID);
            }
            string wire=SpreadExplorationPlan.BindForSave(m,null).GetProperty(SpreadExplorationPlan.PropertyKey);Bind(m,Restore(m,wire));
            Assert.AreEqual(wire,SpreadExplorationPlan.BindForSave(m,null).GetProperty(SpreadExplorationPlan.PropertyKey));Assert.Zero(m.CachedZoneCount);
        }
        [TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)]
        public void LiteralOlderFieldAssignmentsRemainUnchanged(int version)
        {
            var m=OverworldZoneManager.CreateDetached(scope.Factory,64,false);var id=SpreadExplorationPlan.Create(m).Entries.First(e=>e.PlacementEligible&&FormationSelector.For(BiomeType.Spread,e.ZoneID)==Formation.FieldStrips).ZoneID;
            string wire=version+"|64|1\n"+id+"|3|3|1|0";Bind(m,Restore(m,wire));Assert.AreEqual(version,m.Exploration.Version);
            Assert.AreEqual(wire,SpreadExplorationPlan.BindForSave(m,null).GetProperty(SpreadExplorationPlan.PropertyKey));Assert.Zero(m.CachedZoneCount);
        }
        sealed class ObservedManager:OverworldZoneManager
        {public ObservedManager(EntityFactory f,int seed):base(f,seed){}public SpreadCompositionBuilder Terrain(string id)=>base.GetPipelineForZone(id).Builders.OfType<SpreadCompositionBuilder>().Single();}
        [Test] public void OnlySelectedCurrentCookingFamilyOptsIntoTerrainRowReceipts()
        {
            var m=new ObservedManager(scope.Factory,64);Assert.AreEqual(10,m.Exploration.Version);
            var fields=m.Exploration.Entries.Where(e=>e.PlacementEligible&&FormationSelector.For(BiomeType.Spread,e.ZoneID)==Formation.FieldStrips).ToArray();
            Assert.True(fields.Any(e=>e.Family.ToString()=="CoolingWorkPatch"));Assert.True(fields.Any(e=>e.Family.ToString()=="LastGleanings"));Assert.True(fields.Any(e=>e.Family==SpreadExplorationFamily.None));
            var flag=typeof(SpreadCompositionBuilder).GetField("CaptureCookingSources",All);Assert.NotNull(flag);
            foreach(var row in fields)Assert.AreEqual(row.Family.ToString()=="CoolingWorkPatch",flag.GetValue(m.Terrain(row.ZoneID)));
            string id=fields.First().ZoneID;Bind(m,Restore(m,"5|64|1\n"+id+"|3|3|1|0"));Assert.False((bool)flag.GetValue(m.Terrain(id)));Assert.Zero(m.CachedZoneCount);
        }
        [TestCase(999,"FieldStrips",0)][TestCase(5,"FieldStrips",10)][TestCase(6,"Hedgerow",10)]
        public void UnsupportedWireRefusesWithoutInventingGraphs(int version,string formation,int family)
        {
            var m=OverworldZoneManager.CreateDetached(scope.Factory,64,false);string id=SpreadExplorationPlan.Create(m).Entries.First(e=>e.PlacementEligible&&FormationSelector.For(BiomeType.Spread,e.ZoneID).ToString()==formation).ZoneID;
            Assert.Throws<InvalidDataException>(()=>Restore(m,version+"|64|1\n"+id+"|3|"+family+"|1|0"));Assert.False(m.Exploration.Enabled);Assert.Zero(m.CachedZoneCount);
        }
    }
}
