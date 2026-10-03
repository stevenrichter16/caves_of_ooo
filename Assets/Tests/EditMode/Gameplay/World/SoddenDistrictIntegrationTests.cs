using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SoddenDistrictIntegrationTests
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        internal static void Version(OverworldZoneManager manager,int version)
        {
            var world=SpreadExplorationPlan.BindForSave(manager,null);
            string wire=world.GetProperty(SpreadExplorationPlan.PropertyKey);
            world.Properties[SpreadExplorationPlan.PropertyKey]=version+wire.Substring(wire.IndexOf('|'));
            var restored=typeof(SpreadExplorationPlan).GetMethod("Restore",Hidden).Invoke(null,new object[]{manager,world});
            typeof(OverworldZoneManager).GetProperty("Exploration",Hidden).SetValue(manager,restored);
        }
        [TestCase(14,true)][TestCase(13,false)][TestCase(12,false)][TestCase(11,false)]
        public void LiteralWorldVersionControlsOnlyNewDistrictGeneration(int version,bool expected)
        {
            using(var scope=new HaulingContentScope())
            {
                scope.Seed(64);var m=OverworldZoneManager.CreateDetached(scope.Factory,64,true);Version(m,version);
                Assert.AreEqual(version,m.Exploration.Version);
                foreach(string id in SoddenDistrictGenerationTests.Sites)
                {
                    Assert.AreEqual(expected,SoddenDistrict.Eligible(m,id));
                    var z=m.GetZone(id);Assert.NotNull(z);
                    Assert.AreEqual(expected,z.GetReadOnlyEntities().Any(e=>e.BlueprintName=="SoddenRouteNotice"));
                }
            }
        }
        [Test] public void MissingManifestForeignAddressAndMissingContentCannotAdoptSites()
        {
            using(var scope=new HaulingContentScope())
            {
                scope.Seed(64);var m=OverworldZoneManager.CreateDetached(scope.Factory,64);
                Assert.IsFalse(SoddenDistrict.Eligible(m,SoddenDistrictPlan.StopZoneID));
                m=OverworldZoneManager.CreateDetached(scope.Factory,64,true);
                Assert.IsTrue(SoddenDistrict.Eligible(m,SoddenDistrictPlan.StopZoneID));
                foreach(string id in new[]{"Overworld.15.6.0","Overworld.16.6.0","Overworld.17.5.0","Overworld.18.7.0","Overworld.15.7.1"})
                    Assert.IsFalse(SoddenDistrict.Eligible(m,id));
                scope.Factory.Blueprints.Remove("SoddenDressingBench");
                Assert.IsFalse(SoddenDistrict.Eligible(m,SoddenDistrictPlan.StopZoneID));
            }
        }
        [TestCase(BiomeType.Spread)][TestCase(BiomeType.Beating)][TestCase(BiomeType.Grovelands)]
        public void AChangedActualBiomeCannotAdmitAnExactDistrictAddress(BiomeType biome)
        {
            using(var scope=new HaulingContentScope())
            {
                scope.Seed(64);var m=OverworldZoneManager.CreateDetached(scope.Factory,64,true);
                Assert.IsTrue(SoddenDistrict.Eligible(m,SoddenDistrictPlan.StopZoneID));m.WorldMap.Tiles[15,7]=biome;
                Assert.IsFalse(SoddenDistrict.Eligible(m,SoddenDistrictPlan.StopZoneID));
                var zone=m.GetZone(SoddenDistrictPlan.StopZoneID);Assert.NotNull(zone);
                Assert.IsFalse(zone.GetReadOnlyEntities().Any(e=>e.BlueprintName=="SoddenRouteNotice"));
            }
        }
        [TestCase(POIType.Village)][TestCase(POIType.Lair)][TestCase(POIType.MerchantCamp)]
        public void AConflictingActualPoiCannotAdmitAnExactDistrictAddress(POIType type)
        {
            using(var scope=new HaulingContentScope())
            {
                scope.Seed(64);var m=OverworldZoneManager.CreateDetached(scope.Factory,64,true);
                Assert.IsTrue(SoddenDistrict.Eligible(m,SoddenDistrictPlan.StopZoneID));m.WorldMap.SetPOI(15,7,new PointOfInterest(type,"foreign place",tier:2,bossBlueprint:"MarlbackWallkeeper"));
                Assert.IsFalse(SoddenDistrict.Eligible(m,SoddenDistrictPlan.StopZoneID));
                var zone=m.GetZone(SoddenDistrictPlan.StopZoneID);Assert.NotNull(zone);
                Assert.IsFalse(zone.GetReadOnlyEntities().Any(e=>e.BlueprintName=="SoddenRouteNotice"));
            }
        }
        [TestCase(1)][TestCase(64)][TestCase(1729)]
        public void WholeDistrictSurvivesTheFinalPipelineWithoutOrdinaryCropAdditions(int seed)
        {
            using(var scope=new HaulingContentScope())
            {
                scope.Seed(seed);var m=OverworldZoneManager.CreateDetached(scope.Factory,seed,true);
                foreach(string id in SoddenDistrictGenerationTests.Sites)
                {
                    var zone=m.GetZone(id);Assert.NotNull(zone,id);
                    Assert.AreEqual(id==SoddenDistrictPlan.StopZoneID?2:0,zone.GetReadOnlyEntities().Count(e=>e.HasPart<CropPart>()));
                }
            }
        }
        [TestCase(14,true)][TestCase(13,false)]
        public void FreshTownTollRollsGiveThePhysicalCircuitLeadOnlyForEligibleWorlds(int version,bool expected)
        {
            using(var scope=new HaulingContentScope())
            {
                scope.Seed(64);var m=OverworldZoneManager.CreateDetached(scope.Factory,64,true);Version(m,version);
                var town=m.GetZone("Overworld.15.6.0");var rolls=town.GetReadOnlyEntities().Single(e=>e.BlueprintName=="TollRolls");
                Assert.AreEqual(expected,rolls.GetPart<ExaminablePart>().Text.Contains("SELLA'S DRESSING SHELTER"));
                Assert.IsFalse(m.CachedZones.ContainsKey(SoddenDistrictPlan.StopZoneID),"Reading a cold lead never materializes the destination.");
            }
        }
        [TestCase("map")][TestCase("manifest")][TestCase("remove-locker")]
        public void FinalCommitRejectsAuthorityOrRequiredOwnerChangedAfterGenerationHooks(string mode)
        {
            using(var scope=new HaulingContentScope())
            {
                scope.Seed(64);var m=new AfterGenerationManager(scope.Factory,64);
                m.After=zone=>
                {
                    if(mode=="map")m.WorldMap.Tiles[17,7]=BiomeType.Beating;
                    if(mode=="manifest")Version(m,13);
                    if(mode=="remove-locker")zone.RemoveEntity(zone.GetReadOnlyEntities().Single(e=>e.BlueprintName=="SoddenWorksLocker"));
                };
                Assert.IsNull(m.GetZone(SoddenDistrictPlan.WorksZoneID));Assert.IsFalse(m.CachedZones.ContainsKey(SoddenDistrictPlan.WorksZoneID));
            }
        }
        sealed class AfterGenerationManager:OverworldZoneManager
        {
            public Action<Zone> After;
            public AfterGenerationManager(CavesOfOoo.Data.EntityFactory f,int seed):base(f,seed){}
            protected override void OnZoneGenerated(Zone zone,string id){base.OnZoneGenerated(zone,id);After?.Invoke(zone);}
        }
        [Test] public void RealGeneratedBenchBindsKeeperAndThreeEmptiedGraphsSurviveRevisitAndSave()
        {
            using(var scope=new HaulingContentScope())
            {
                scope.Seed(64);var m=OverworldZoneManager.CreateDetached(scope.Factory,64,true);
                foreach(string id in SoddenDistrictGenerationTests.Sites)
                {
                    var z=m.GetZone(id);Assert.NotNull(z,id);
                    if(id==SoddenDistrictPlan.StopZoneID)
                    {
                        var bench=z.GetReadOnlyEntities().Single(e=>e.BlueprintName=="SoddenDressingBench");
                        var service=bench.GetPart<SoddenPreparationPart>();Assert.IsTrue(service.Configured);
                        Assert.AreSame(z.GetReadOnlyEntities().Single(e=>e.BlueprintName=="PeatCutter"),service.Worker);
                    }
                    foreach(var e in z.GetAllEntities().ToArray())z.RemoveEntity(e);
                    m.UnloadZone(id);Assert.AreSame(z,m.GetZone(id));Assert.Zero(z.EntityCount);
                }
                var state=GameSessionState.Capture("sodden-retention","test",m,null,null);GameSessionState loaded;
                using(var stream=new MemoryStream()){state.Save(new SaveWriter(stream));stream.Position=0;loaded=GameSessionState.Load(new SaveReader(stream,scope.Factory));}
                foreach(string id in SoddenDistrictGenerationTests.Sites)Assert.Zero(loaded.ZoneManager.GetZone(id).EntityCount,id);
            }
        }
        [Test] public void CachedLegacyGraphsAreNotRetrofittedEvenWithFreshManifest()
        {
            using(var scope=new HaulingContentScope())
            {
                scope.Seed(64);var m=OverworldZoneManager.CreateDetached(scope.Factory,64,true);
                var literal=new Zone(SoddenDistrictPlan.StopZoneID);var sign=scope.Factory.CreateEntity("Signpost");literal.AddEntity(sign,5,5);
                m.CachedZones[literal.ZoneID]=literal;Assert.AreSame(literal,m.GetZone(literal.ZoneID));
                CollectionAssert.AreEquivalent(new[]{sign},literal.GetReadOnlyEntities());
            }
        }
    }
}
