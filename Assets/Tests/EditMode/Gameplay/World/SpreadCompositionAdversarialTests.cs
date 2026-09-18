using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public class SpreadCompositionAdversarialTests
    {
        [TestCase("garbage")] [TestCase("Overworld.30.2.0")] [TestCase("Overworld.8.4.1")]
        public void InvalidAddressesRejected(string id)=>Assert.Throws<ArgumentException>(()=>SpreadCompositionPlan.Create(id,1));
        [TestCase(Formation.Grove)] [TestCase(Formation.OpenMire)] [TestCase((Formation)999)]
        public void OtherBiomeFormationsRejected(Formation f)=>Assert.Throws<ArgumentException>(()=>SpreadCompositionPlan.Create(SpreadCompositionTests.Id,1,f));
        [TestCase("Overworld.8.4.0",true)] [TestCase("Overworld.9.4.0",true)]
        [TestCase("Overworld.10.10.0",false)] [TestCase("Overworld.7.8.0",false)]
        [TestCase("Overworld.2.6.0",false)] [TestCase("Overworld.8.4.1",false)]
        [TestCase("Overworld.4.6.0",false)] [TestCase("garbage",false)] [TestCase(null,false)]
        public void CoverageIsFiniteAndExcludesSites(string id,bool expected)=>Assert.AreEqual(expected,SpreadCompositionPlan.IsWildernessZone(id));
        [Test] public void NonemptyInputPreservesItsEntitiesAndClearsOldPlan()
        {
            var z=new Zone(SpreadCompositionTests.Id);var f=GrovelandsCompositionTests.Factory();var b=new SpreadCompositionBuilder(1);
            Assert.IsTrue(b.BuildZone(z,f,new System.Random(1)));var before=z.GetAllEntities().ToArray();
            Assert.IsFalse(b.BuildZone(z,f,new System.Random(1)));Assert.IsNull(b.Plan);
            CollectionAssert.AreEquivalent(before,z.GetAllEntities());
        }
        [TestCase(Formation.Hedgerow)] [TestCase(Formation.FieldStrips)] [TestCase(Formation.OldRoad)]
        [TestCase(Formation.FlowerMeadow)] [TestCase(Formation.Fallow)] [TestCase(Formation.RiverMeadow)]
        public void RealizationIgnoresCallerRngHistoryAndMatchesIntent(Formation form)
        {
            var f=GrovelandsCompositionTests.Factory();var a=new Zone(SpreadCompositionTests.Id);var b=new Zone(SpreadCompositionTests.Id);
            var random=new System.Random(67);for(int i=0;i<500;i++)random.Next();
            var builder=new SpreadCompositionBuilder(17){FormationOverride=form};
            Assert.IsTrue(builder.BuildZone(a,f,random));Assert.IsTrue(new SpreadCompositionBuilder(17){FormationOverride=form}.BuildZone(b,f,new System.Random(1)));
            CollectionAssert.AreEqual(Snapshot(a),Snapshot(b));
            int wet=0,ripe=0,cut=0;
            for(int x=0;x<Zone.Width;x++)for(int y=0;y<Zone.Height;y++)
            {
                Assert.AreEqual(builder.Plan.IsWater(x,y),SpawnRing3DRecipes.HasPermanentWater(a,x,y));
                if(builder.Plan.IsWater(x,y))wet++;
                var bp=builder.Plan.ObjectAt(x,y);
                if(bp=="CropRow")
                {
                    var rows=a.GetCell(x,y).Objects.Where(e=>e.BlueprintName=="CropRow"||e.BlueprintName=="RipeCropRow").ToArray();
                    Assert.AreEqual(1,rows.Length,"Sparse gleanings must replace one planned row, never add a second owner.");
                    if(rows[0].BlueprintName=="RipeCropRow")
                    {
                        ripe++;Assert.NotNull(rows[0].GetPart<FieldHarvestPart>());Assert.IsFalse(rows[0].GetPart<FieldHarvestPart>().Harvested);
                        Assert.IsFalse(builder.Plan.IsApproach(x,y));
                    }
                    else{cut++;Assert.IsNull(rows[0].GetPart<FieldHarvestPart>());}
                }
                else
                {
                    if(bp!=null)Assert.AreEqual(1,a.GetCell(x,y).Objects.Count(e=>e.BlueprintName==bp));
                    Assert.IsFalse(a.GetCell(x,y).Objects.Any(e=>e.BlueprintName=="RipeCropRow"),"No grain outside the planned rows.");
                }
            }
            if(form==Formation.FieldStrips)
            {
                Assert.AreEqual(builder.Plan.Condition=="tended"?3:builder.Plan.Condition=="returning scrub"?2:1,ripe);
                Assert.Greater(cut,ripe*5,"Most of the established field remains visibly cut.");
            }
            else Assert.AreEqual(0,ripe);
            if(form==Formation.RiverMeadow)Assert.Greater(wet,20);else Assert.AreEqual(0,wet);
        }
        [TestCase("Hedge")] [TestCase("CropRow")] [TestCase("FlowerField")]
        [TestCase("Reeds")] [TestCase("RoadStone")]
        public void SpreadRecipesRetainNativeIdentityAndRespectRemoval(string bp)
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone(SpreadCompositionTests.Id);
            var e=Place(z,f,bp,10,10);Assert.NotNull(e);
            var catalog=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
            var r=SpawnRing3DRecipes.Resolve(z,e,catalog);Assert.NotNull(r.ModelId,r.Failure);Assert.AreSame(e,r.Owner);
            Assert.IsTrue(r.Batched);Assert.IsFalse(r.Transient);
            var before=z.EntityCount;SpawnRing3DRecipes.Resolve(z,e,catalog);Assert.AreEqual(before,z.EntityCount);
            z.RemoveEntity(e);Assert.IsNull(SpawnRing3DRecipes.Resolve(z,e,catalog).ModelId);
        }
        [Test] public void FlowerCharmAndDestructibleHedgeKeepTheirNativeParts()
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone(SpreadCompositionTests.Id);
            var flowers=Place(z,f,"FlowerField",10,10);Assert.IsTrue(flowers.HasPart<FlowerCharmPart>());
            var hedge=Place(z,f,"Hedge",11,10);Assert.IsTrue(hedge.HasPart<DestructiblePart>());
            Assert.IsTrue(hedge.GetPart<PhysicsPart>().Solid);
        }
        [Test] public void SharedPoiPipelineRetainsLegacyTerrain()
        {
            var manager=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),64);
            var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
            var method=typeof(OverworldZoneManager).GetMethod("CreateMerchantCampPipeline",flags);
            var p=(ZoneGenerationPipeline)method.Invoke(manager,new object[]{BiomeType.Spread,1});
            Assert.IsFalse(p.Builders.Any(b=>b is SpreadCompositionBuilder));
            Assert.IsTrue(p.Builders.Any(b=>b is SpreadFormationBuilder));
            var get=typeof(OverworldZoneManager).GetMethod("GetPipelineForZone",flags);
            bool sawComposed=false;
            for(int x=0;x<20;x++)for(int y=0;y<20;y++)
            {
                string id=WorldMap.ToZoneID(x,y);
                if(!SpreadCompositionPlan.IsWildernessZone(id))continue;
                p=(ZoneGenerationPipeline)get.Invoke(manager,new object[]{id});
                bool poi=manager.WorldMap.GetPOI(x,y)!=null;
                Assert.AreEqual(!poi,p.Builders.Any(b=>b is SpreadCompositionBuilder),id);
                if(!poi)sawComposed=true;
            }
            Assert.IsTrue(sawComposed);
        }
        [Test] public void WaterAndApproachesAreReservedAgainstLaterStamps()
        {
            var z=new Zone(SpreadCompositionTests.Id);var b=new SpreadCompositionBuilder(64){FormationOverride=Formation.RiverMeadow};
            Assert.IsTrue(b.BuildZone(z,GrovelandsCompositionTests.Factory(),new System.Random(1)));
            int water=0,unreserved=0;
            for(int x=0;x<Zone.Width;x++)for(int y=0;y<Zone.Height;y++)
            {
                if(b.Plan.IsWater(x,y)||b.Plan.IsApproach(x,y))Assert.IsTrue(z.GenReservedCells.Contains((x,y)),x+","+y);
                else if(!z.GenReservedCells.Contains((x,y)))unreserved++;
                if(b.Plan.IsWater(x,y))water++;
            }
            Assert.Greater(water,20);Assert.Greater(unreserved,1000,"Keep space for ambient life and landmarks.");
        }
        [TestCase(Formation.Hedgerow)] [TestCase(Formation.Fallow)]
        public void EveryOpenPocketConnectsBeforeConnectivityRepair(Formation form)
        {
            var f=GrovelandsCompositionTests.Factory();
            for(int seed=0;seed<100;seed++)
            {
                var z=new Zone(SpreadCompositionTests.Id);new SpreadCompositionBuilder(seed){FormationOverride=form}.BuildZone(z,f,new System.Random(1));
                Assert.IsTrue(FormationReachability.FullyReached(z,FormationReachability.FloodFromWest(z)),form+" seed "+seed);
            }
        }
        [Test] public void ReservedBackwaterRejectsStructuralStampWithUnreservedCountercheck()
        {
            var z=new Zone(SpreadCompositionTests.Id);var b=new SpreadCompositionBuilder(64){FormationOverride=Formation.RiverMeadow};
            Assert.IsTrue(b.BuildZone(z,GrovelandsCompositionTests.Factory(),new System.Random(1)));
            var method=typeof(LandmarkBuilder).GetMethod("FootprintClear",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
            Assert.NotNull(method);var stamp=new StructureStamp{Rows=new[]{"#"}};
            for(int y=1;y<24;y++)for(int x=1;x<79;x++)if(b.Plan.IsWater(x,y))
            {
                Assert.IsFalse((bool)method.Invoke(null,new object[]{z,stamp,x,y}));
                z.GenReservedCells.Remove((x,y));
                Assert.IsTrue((bool)method.Invoke(null,new object[]{z,stamp,x,y}),"Control proves the reservation is what protects coating-only water.");
                return;
            }
            Assert.Fail("No backwater precondition.");
        }
        [Test] public void SuccessAndRejectedRebuildEmitDistinctDiagnostics()
        {
            Diag.ResetAll();var z=new Zone(SpreadCompositionTests.Id);var b=new SpreadCompositionBuilder(64);var f=GrovelandsCompositionTests.Factory();
            Assert.IsTrue(b.BuildZone(z,f,new System.Random(1)));Assert.IsFalse(b.BuildZone(z,f,new System.Random(1)));
            Assert.AreEqual(1,DiagQuery.Apply(new DiagQuery.Filter{Category="worldgen",Kind="SpreadCompositionPlanned",Limit=10}).Records.Count);
            var rejected=DiagQuery.Apply(new DiagQuery.Filter{Category="worldgen",Kind="SpreadCompositionRejected",Limit=10}).Records;
            Assert.AreEqual(1,rejected.Count);StringAssert.Contains("nonempty-zone",rejected[0].PayloadJson);Diag.ResetAll();
        }
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void FullNativePipelineStillCrossesWorkedCountry(int seed)
        {
            var m=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),seed);int checkedZones=0;
            for(int x=0;x<20&&checkedZones<6;x++)for(int y=0;y<20&&checkedZones<6;y++)
            {
                string id=WorldMap.ToZoneID(x,y);
                if(!SpreadCompositionPlan.IsWildernessZone(id)||m.WorldMap.GetPOI(x,y)!=null)continue;
                var z=m.GetZone(id);Assert.NotNull(z);FormationReachability.FloodFromWest(z,out bool crossed);
                Assert.IsTrue(crossed,id+" seed "+seed);checkedZones++;
            }
            Assert.AreEqual(6,checkedZones);
        }
        private static Entity Place(Zone z,CavesOfOoo.Data.EntityFactory f,string bp,int x,int y)
        {var e=f.CreateEntity(bp);z.AddEntity(e,x,y);return e;}
        private static string[] Snapshot(Zone z)=>z.GetAllEntities().Select(e=>e.BlueprintName+":"+z.GetEntityPosition(e)).OrderBy(s=>s,StringComparer.Ordinal).ToArray();
    }
}
