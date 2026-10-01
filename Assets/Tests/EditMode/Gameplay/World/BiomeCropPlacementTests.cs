using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class BiomeCropPlacementTests
    {
        HaulingContentScope scope;
        [SetUp] public void Setup() { scope = new HaulingContentScope(); scope.Seed(64); }
        [TearDown] public void Cleanup() { scope.Dispose(); }
        static readonly BiomeType[] Surface = { BiomeType.Spread, BiomeType.Sodden, BiomeType.Beating,
            BiomeType.Grovelands, BiomeType.Overwrit, BiomeType.Stump };
        [TestCase(64)] [TestCase(1729)]
        public void StableSparseAssignmentsCoverFiveSpeciesPerSurfaceBiome(int seed)
        {
            var a = OverworldZoneManager.CreateDetached(scope.Factory, seed, true);
            var b = OverworldZoneManager.CreateDetached(scope.Factory, seed, true);
            var sites = BiomeCropPlan.SurfaceSites(a);
            CollectionAssert.AreEqual(sites.Select(s => s.Key), BiomeCropPlan.SurfaceSites(b).Select(s => s.Key));
            Assert.AreEqual(sites.Count, sites.Select(s => s.ZoneID).Distinct().Count());
            foreach (var biome in Surface)
            {
                var rows = sites.Where(s => s.Biome == biome).ToArray();
                Assert.That(rows.Length, Is.InRange(5, 10), biome.ToString());
                CollectionAssert.AreEquivalent(Enumerable.Range(0, 5), rows.Select(s => s.SpeciesIndex).Distinct());
                foreach (var site in rows)
                {
                    var p = WorldMap.FromZoneID(site.ZoneID);
                    Assert.Null(a.WorldMap.GetPOI(p.x, p.y));
                    Assert.AreEqual(biome, a.WorldMap.GetBiome(p.x, p.y));
                    if (biome == BiomeType.Overwrit) Assert.True(OverwritCompositionPlan.IsRimZone(site.ZoneID));
                    if (biome == BiomeType.Spread) Assert.AreEqual(SpreadExplorationFamily.None, a.Exploration.Entries.Single(e => e.ZoneID == site.ZoneID).Family);
                }
            }
        }
        [TestCase("Overworld.3.6.0")] [TestCase("Overworld.2.6.0")] [TestCase("Overworld.1.6.0")]
        [TestCase("Overworld.2.19.0")] [TestCase("Overworld.3.3.0")] [TestCase("Overworld.5.4.1")]
        [TestCase("WorldMap")] [TestCase("Overworld.3.6.1")]
        public void SpecialSitesNeverReceiveGenericCrops(string id)
        { Assert.Null(BiomeCropPlan.ForZone(OverworldZoneManager.CreateDetached(scope.Factory,64,true),id)); }
        Zone Plain(string id, string ground = "Grass")
        {
            var zone = new Zone(id);
            for (int y=0;y<Zone.Height;y++) for(int x=0;x<Zone.Width;x++) Assert.True(zone.AddEntity(scope.Factory.CreateEntity(ground),x,y));
            return zone;
        }
        static Dictionary<Entity,(string id,int x,int y)> Snapshot(Zone zone) => zone.GetReadOnlyEntities().ToDictionary(e=>e,e=>
        { var p=zone.GetEntityPosition(e);return(e.ID,p.x,p.y); });
        static void Unchanged(Zone zone,Dictionary<Entity,(string id,int x,int y)> snapshot)
        { foreach(var row in snapshot){Assert.AreEqual(row.Value.id,row.Key.ID);Assert.AreEqual((row.Value.x,row.Value.y),zone.GetEntityPosition(row.Key));} }
        [Test] public void AdditivePatchKeepsEveryTerrainOwnerAndCannotRefill()
        {
            var manager=OverworldZoneManager.CreateDetached(scope.Factory,64,true);
            var site=BiomeCropPlan.SurfaceSites(manager).First(s=>s.Biome==BiomeType.Grovelands);
            var zone=Plain(site.ZoneID); var old=scope.Factory.CreateEntity("FireClay");Assert.True(zone.AddEntity(old,40,12));
            var before=Snapshot(zone); Assert.True(BiomeCropPlacement.TryInstall(manager,zone,site)); Unchanged(zone,before);
            Assert.AreEqual(2,zone.GetReadOnlyEntities().Count(e=>e.HasPart<CropPart>()));
            Assert.AreEqual(2,zone.GetReadOnlyEntities().Count(e=>e.HasPart<CultivatedSoilPart>()));
            foreach(var crop in zone.GetReadOnlyEntities().Where(e=>e.HasPart<CropPart>()).ToArray())Assert.True(zone.RemoveEntity(crop));
            var depleted=Snapshot(zone);Assert.False(BiomeCropPlacement.TryInstall(manager,zone,site));Unchanged(zone,depleted);Assert.AreEqual(depleted.Count,zone.EntityCount);
            manager.SetActiveZone(zone);manager.UnloadZone(zone.ZoneID);Assert.AreSame(zone,manager.GetZone(zone.ZoneID));
        }
        [Test] public void MissingCropBlueprintPreservesAllOriginalState()
        {
            var manager=OverworldZoneManager.CreateDetached(scope.Factory,64,true);var site=BiomeCropPlan.SurfaceSites(manager).First();var zone=Plain(site.ZoneID);
            var crop=BiomeCropCatalog.ForBiome(site.Biome)[site.SpeciesIndex].CropBlueprint;
            var bp=scope.Factory.Blueprints[crop];scope.Factory.Blueprints.Remove(crop);var before=Snapshot(zone);
            try { Assert.False(BiomeCropPlacement.TryInstall(manager,zone,site));Unchanged(zone,before);Assert.AreEqual(before.Count,zone.EntityCount);Assert.False(zone.GetReadOnlyEntities().Any(e=>e.HasPart<CultivatedSoilPart>())); }
            finally { scope.Factory.Blueprints.Add(crop,bp); }
        }
        [Test] public void CachedGraphIsNeverRetrofitted()
        {
            var manager=OverworldZoneManager.CreateDetached(scope.Factory,64,true);var site=BiomeCropPlan.SurfaceSites(manager).First();var zone=Plain(site.ZoneID);manager.SetActiveZone(zone);
            var before=Snapshot(zone);Assert.False(BiomeCropPlacement.TryInstall(manager,zone,site));Unchanged(zone,before);Assert.AreEqual(before.Count,zone.EntityCount);
        }
        [Test] public void NoSafeSpaceRefusesWithoutClearingReservedOrOccupiedGround()
        {
            var manager=OverworldZoneManager.CreateDetached(scope.Factory,64,true);var site=BiomeCropPlan.SurfaceSites(manager).First();var zone=Plain(site.ZoneID);
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)zone.GenReservedCells.Add((x,y));
            var before=Snapshot(zone);Assert.False(BiomeCropPlacement.TryInstall(manager,zone,site));Unchanged(zone,before);Assert.AreEqual(before.Count,zone.EntityCount);
        }
        [Test] public void CavePatchRequiresActualStairAccessAndAcceptsInteriorFloor()
        {
            var manager=OverworldZoneManager.CreateDetached(scope.Factory,64,true);string column=BiomeCropPlan.CaveColumns(manager).First();
            var p=WorldMap.FromZoneID(column);string id=WorldMap.ToZoneID(p.x,p.y,1);var site=BiomeCropPlan.ForZone(manager,id);Assert.NotNull(site);
            var zone=Plain(id,"SandstoneFloor");for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)zone.GetCell(x,y).IsInterior=true;
            Assert.False(BiomeCropPlacement.TryInstall(manager,zone,site));
            manager.RegisterConnection(new ZoneConnection{SourceZoneID=column,SourceX=40,SourceY=12,TargetZoneID=id,TargetX=40,TargetY=12,Type="StairsDown"});
            Assert.True(zone.AddEntity(scope.Factory.CreateEntity("StairsUp"),40,12));
            var before=Snapshot(zone);Assert.True(BiomeCropPlacement.TryInstall(manager,zone,site));Unchanged(zone,before);
            Assert.AreEqual(2,zone.GetReadOnlyEntities().Count(e=>e.HasPart<CropPart>()));
        }
    }
}
