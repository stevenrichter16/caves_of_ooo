using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SpreadWildernessSituationTests
    {
        DensityLootTestScope scope; OverworldZoneManager manager;
        [SetUp]public void Setup(){scope=new DensityLootTestScope();manager=OverworldZoneManager.CreateDetached(scope.Factory,64);}
        [TearDown]public void Cleanup(){scope.Dispose();}
        static Type PlanType(){var t=typeof(Entity).Assembly.GetType("CavesOfOoo.Core.SpreadWildernessSituationPlan");Assert.NotNull(t,"Missing bounded situation selection.");return t;}
        static bool Eligible(OverworldZoneManager m,string id,string excluded="")=>(bool)PlanType().GetMethod("IsEligible").Invoke(null,new object[]{m,id,excluded});
        static string Select(OverworldZoneManager m,string id,string excluded="")=>(string)PlanType().GetMethod("Select").Invoke(null,new object[]{m,id,excluded});
        static IEnumerable<string> Addresses(){for(int y=0;y<WorldMap.Height;y++)for(int x=0;x<WorldMap.Width;x++)yield return WorldMap.ToZoneID(x,y,0);}
        static uint Rank(int seed,string id){unchecked{uint h=2166136261^(uint)seed;foreach(char c in "SpreadWildernessSituation.v1|"+id)h=(h^c)*16777619;h^=h>>16;h*=0x7feb352d;h^=h>>15;h*=0x846ca68b;h^=h>>16;return h;}}
        static string ExpectedKind(Formation f)=>f==Formation.OldRoad?"cargo":f==Formation.FieldStrips?"gleanings":f==Formation.Hedgerow||f==Formation.Fallow?"shelter":"";
        string Selected(string kind)=>Addresses().First(id=>Select(manager,id)==kind);
        static Zone Source(SpreadCompositionBuilder b){var p=typeof(SpreadCompositionBuilder).GetProperty("SourceZone");Assert.NotNull(p,"Composition must retain exact successful source graph.");return (Zone)p.GetValue(b);}
        static IZoneBuilder Composer(OverworldZoneManager m,SpreadCompositionBuilder terrain,PopulationBuilder population,ContainerBuilder containers,string excluded="")
        {var t=typeof(Entity).Assembly.GetType("CavesOfOoo.Core.SpreadWildernessSituationBuilder");Assert.NotNull(t,"Missing late exact-owner composer.");return (IZoneBuilder)Activator.CreateInstance(t,new object[]{m,terrain,population,containers,excluded});}
        static string Result(IZoneBuilder b)=>(string)b.GetType().GetProperty("LastResult").GetValue(b);
        static string Graph(Zone z,bool locations)=>z.TileState.ToSaveString()+"|"+string.Join(";",z.GetReadOnlyEntities().Select(e=>e.ID+":"+e.BlueprintName+(locations?"@"+z.GetEntityPosition(e):"")+":"+e.GetStatValue("Hitpoints")+":"+string.Join(",",(e.GetPart<ContainerPart>()?.Contents??new List<Entity>()).Concat(DensityLootTestScope.Gear(e)).Select(i=>i.ID+":"+i.BlueprintName+":"+(i.GetPart<StackerPart>()?.StackCount??1)).OrderBy(x=>x))).OrderBy(x=>x));
        static PopulationTable Group(string blueprint="MarlbackScrabbler")=>new PopulationTable{Name="SpreadTier1",Entries=new List<PopulationEntry>{new PopulationEntry{BlueprintName=blueprint,EncounterGroup="SpreadTier1Encounter",MinCount=2,MaxCount=2}}};

        [TestCase(1)][TestCase(64)][TestCase(1729)]
        public void CurrentMapHashHasExactFormationKindsAndLeavesWorldCold(int seed)
        {manager=OverworldZoneManager.CreateDetached(scope.Factory,seed);string rare=manager.RareEncounters.PairZoneID+"|"+manager.RareEncounters.ViperZoneID;int selected=0;
            foreach(string id in Addresses()){string expected=Eligible(manager,id)&&Rank(seed,id)%8==0?ExpectedKind(FormationSelector.For(BiomeType.Spread,id)):"";Assert.AreEqual(expected,Select(manager,id),id);if(expected.Length>0)selected++;}
            Assert.Greater(selected,0);Assert.AreEqual(0,manager.CachedZoneCount);Assert.AreEqual(rare,manager.RareEncounters.PairZoneID+"|"+manager.RareEncounters.ViperZoneID);}
        [TestCase(null)][TestCase("")][TestCase("bad")][TestCase("Overworld.08.8.0")][TestCase("Overworld.8.8.1")][TestCase("Overworld.-1.8.0")][TestCase("Overworld.999999999999.8.0")]
        public void InvalidCanonicalSourceRefusesWithoutGenerating(string id){Assert.False(Eligible(manager,id));Assert.IsEmpty(Select(manager,id));Assert.AreEqual(0,manager.CachedZoneCount);}
        [Test]public void NullManagerRefuses(){Assert.False(Eligible(null,"Overworld.8.8.0"));}
        [Test]public void ProtectedSourcesAlwaysRemainExcluded()
        {foreach(string id in OverworldZoneManager.AuthoredWildernessZoneIDs.Concat(new[]{ReferenceGladePlan.ZoneID,MultiCellPilotRuntime.ZoneID,manager.RareEncounters.PairZoneID,manager.RareEncounters.ViperZoneID}).Concat(RegionalSituations.Definitions.SelectMany(d=>new[]{d.SourceZoneId,d.RecipientZoneId})))Assert.False(Eligible(manager,id),id);}
        [TestCase("wayhouse")][TestCase("biome")][TestCase("poi")]
        public void CurrentAuthorityChangesRefuseRatherThanReroll(string change)
        {string id=Selected("cargo");var xy=WorldMap.FromZoneID(id);if(change=="biome")manager.WorldMap.Tiles[xy.x,xy.y]=BiomeType.Beating;if(change=="poi")manager.WorldMap.SetPOI(xy.x,xy.y,new PointOfInterest(POIType.MerchantCamp,"protected"));Assert.IsEmpty(Select(manager,id,change=="wayhouse"?id:""));Assert.AreEqual(0,manager.CachedZoneCount);}
        [Test]public void SuccessfulTerrainReceiptIsExactAndReplacedByAnotherSameIdBuild()
        {var b=new SpreadCompositionBuilder(64);var a=new Zone("Overworld.8.8.0");Assert.True(b.BuildZone(a,scope.Factory,new Random(1)));Assert.AreSame(a,Source(b));var next=new Zone(a.ZoneID);Assert.True(b.BuildZone(next,scope.Factory,new Random(1)));Assert.AreSame(next,Source(b));Assert.AreNotSame(a,Source(b));}
        [Test]public void FailedTerrainBuildClearsExactSourceReceipt()
        {var b=new SpreadCompositionBuilder(64);var a=new Zone("Overworld.8.8.0");Assert.True(b.BuildZone(a,scope.Factory,new Random(1)));Assert.AreSame(a,Source(b));Assert.False(b.BuildZone(a,scope.Factory,new Random(1)));Assert.IsNull(Source(b));Assert.IsNull(b.Plan);}
        [Test]public void LateComposerRunsAfterAllRandomHaulables()
        {var b=Composer(manager,new SpreadCompositionBuilder(64),new PopulationBuilder(Group()),new ContainerBuilder(BiomeType.Spread,1,ContainerPlacementService.ZoneKind.Wilderness));Assert.Greater(b.Priority,new HaulablePropBuilder(BiomeType.Spread).Priority);Assert.AreEqual(4300,b.Priority);}
        [TestCase("cargo")][TestCase("shelter")]
        public void ActualSourcesCanComposeOnceWithoutChangingAnyOwnerStockOrCallerRng(string kind)
        {
            string id=Selected(kind);var zone=new Zone(id);var terrain=new SpreadCompositionBuilder(64);var p=new PopulationBuilder(Group());var c=new ContainerBuilder(BiomeType.Spread,1,ContainerPlacementService.ZoneKind.Wilderness);var b=Composer(manager,terrain,p,c);
            Assert.True(terrain.BuildZone(zone,scope.Factory,new Random(1)));var rng=new Random(14);p.BuildZone(zone,scope.Factory,rng);c.BuildZone(zone,scope.Factory,rng);new HaulablePropBuilder(BiomeType.Spread).BuildZone(zone,scope.Factory,rng);
            string original=Graph(zone,false);var unrelated=zone.GetReadOnlyEntities().Where(e=>!e.HasPart<ContainerPart>()&&e.BlueprintName!="MarlbackScrabbler").ToDictionary(e=>e,e=>zone.GetEntityPosition(e));
            var expectedRandom=new Random(711);var actualRandom=new Random(711);Assert.True(b.BuildZone(zone,scope.Factory,actualRandom));Assert.AreEqual(kind,Result(b),"Current actual source composition must establish a real positive, not optional no-op.");Assert.AreEqual(original,Graph(zone,false));Assert.AreEqual(expectedRandom.Next(),actualRandom.Next());
            foreach(var e in unrelated)Assert.AreEqual(e.Value,zone.GetEntityPosition(e.Key),e.Key.BlueprintName);string after=Graph(zone,true);Assert.True(b.BuildZone(zone,scope.Factory,actualRandom));Assert.AreEqual(after,Graph(zone,true),"A consumed build cannot reposition again.");
        }
        [TestCase("cache")][TestCase("same-id-zone")][TestCase("wrong-factory")][TestCase("reserved")][TestCase("hazard")][TestCase("viper")]
        public void InvalidLateSourceIsAnOptionalNoOpNotGenerationFailure(string change)
        {
            string id=Selected("shelter");var z=new Zone(id);var terrain=new SpreadCompositionBuilder(64);var p=new PopulationBuilder(Group(change=="viper"?"Viper":"MarlbackScrabbler"));var c=new ContainerBuilder(BiomeType.Spread,1,ContainerPlacementService.ZoneKind.Wilderness);var b=Composer(manager,terrain,p,c);terrain.BuildZone(z,scope.Factory,new Random(1));var rng=new Random(14);p.BuildZone(z,scope.Factory,rng);c.BuildZone(z,scope.Factory,rng);
            if(change=="cache")manager.SetActiveZone(z);if(change=="same-id-zone")z=new Zone(z.ZoneID);if(change=="reserved")z.ForEachCell((cell,x,y)=>z.GenReservedCells.Add((x,y)));if(change=="hazard")z.ForEachCell((cell,x,y)=>z.TileState.AddHeat(x,y,1));string before=Graph(z,true);Assert.True(b.BuildZone(z,change=="wrong-factory"?new EntityFactory():scope.Factory,rng));Assert.AreEqual(before,Graph(z,true));Assert.AreNotEqual("shelter",Result(b));
        }
    }
}
