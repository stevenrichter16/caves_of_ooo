using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>Existing places, traps and liquid terrain must be reachable in
    /// the authored world without filling its deliberately blank regions.</summary>
    public class DensityPhase1PlacementTests
    {
        private EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadContent()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
        }

        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(Application.dataPath,
                "Resources/Content/Data/Loot/LootTables.json")));
            ContainerPlacementService.Factory = _factory;
        }

        [TearDown]
        public void Cleanup()
        {
            FactionManager.Reset();
            LootTableRegistry.ResetForTests();
            ContainerPlacementService.Factory = null;
        }

        [TestCase(BiomeType.Beating, "GlassblownObelisk")]
        [TestCase(BiomeType.Beating, "BanditDugout")]
        [TestCase(BiomeType.Beating, "SealedVault")]
        [TestCase(BiomeType.Beating, "ClockworkWorkshop")]
        [TestCase(BiomeType.Beating, "RuneCultSite")]
        [TestCase(BiomeType.Beating, "PalimpsestArchive")]
        [TestCase(BiomeType.Beating, "CollapsedLibrary")]
        [TestCase(BiomeType.Cave, "Ziggurat")]
        public void StrandedStamp_HasALiveBiomeHome(BiomeType biome, string name)
        {
            var stamp = (biome == BiomeType.Cave ? StampCatalog.Underground(3) : StampCatalog.For(biome))
                .SingleOrDefault(s => s.Name == name);
            Assert.IsNotNull(stamp, name + " has no route into the authored surface world");
            Assert.Greater(stamp.Chance, 0);
            Assert.LessOrEqual(stamp.MinTier, biome == BiomeType.Beating ? 2 : 3);
        }

        [TestCase(BiomeType.Spread)]
        [TestCase(BiomeType.Sodden)]
        [TestCase(BiomeType.Beating)]
        [TestCase(BiomeType.Grovelands)]
        public void ChoirZiggurat_StaysOutOfOtherCountries(BiomeType biome)
        {
            Assert.IsFalse(StampCatalog.For(biome).Any(s => s.Name == "Ziggurat"));
        }

        [TestCase("GlassblownDrifter")]
        [TestCase("PalimpsestEcho")]
        public void BeatingLandmarks_ActuallyPlaceStrandedTalkers(string blueprint)
        {
            bool found = false;
            for (int seed = 0; seed < 200 && !found; seed++)
            {
                var zone = new Zone();
                new LandmarkBuilder(BiomeType.Beating, 2).BuildZone(zone, _factory, new Random(seed));
                found = zone.GetAllEntities().Any(e => e.BlueprintName == blueprint);
            }
            Assert.IsTrue(found, blueprint + " never leaves its catalog entry");
        }

        [TestCase(BiomeType.Beating, false, "OilSlick")]
        [TestCase(BiomeType.Beating, false, "OilSeep")]
        [TestCase(BiomeType.Sodden, false, "AcidPool")]
        [TestCase(BiomeType.Grovelands, false, "MirrorMucilagePool")]
        [TestCase(BiomeType.Cave, true, "ConvalescencePool")]
        [TestCase(BiomeType.Cave, true, "MemoryBathPool")]
        public void StrandedLiquidTerrain_ActuallyGenerates(BiomeType biome, bool underground, string blueprint)
        {
            bool found = false;
            for (int seed = 0; seed < 200 && !found; seed++)
            {
                var zone = new Zone();
                new HazardTerrainBuilder(biome, underground).BuildZone(zone, _factory, new Random(seed));
                found = zone.GetAllEntities().Any(e => e.BlueprintName == blueprint);
            }
            Assert.IsTrue(found, blueprint + " has no generated terrain source");
        }

        [Test]
        public void SpreadHazards_DoNotInheritRareDeepOrChoirLiquids()
        {
            var foreign = new HashSet<string> { "AcidPool", "MirrorMucilagePool", "MemoryBathPool", "ConvalescencePool" };
            for (int seed = 0; seed < 100; seed++)
            {
                var zone = new Zone();
                new HazardTerrainBuilder(BiomeType.Spread).BuildZone(zone, _factory, new Random(seed));
                Assert.IsFalse(zone.GetAllEntities().Any(e => foreign.Contains(e.BlueprintName)));
            }
        }

        [TestCase("SealedVault", "SpikeTrap")]
        [TestCase("ClockworkWorkshop", "FireTrap")]
        [TestCase("BanditDugout", "BearTrap")]
        [TestCase("Ziggurat", "PressurePlate")]
        public void RuinStamps_PlaceTheirTrapsAwayFromDoorsAndOccupants(string name, string trap)
        {
            var biome = name == "Ziggurat" ? BiomeType.Cave : BiomeType.Beating;
            var stamp = (biome == BiomeType.Cave ? StampCatalog.Underground(3) : StampCatalog.For(biome))
                .SingleOrDefault(s => s.Name == name);
            Assert.IsNotNull(stamp);
            var zone = new Zone();
            new LandmarkBuilder(biome, 3, new[] { StampCatalog.Forced(stamp) })
                .BuildZone(zone, _factory, new Random(7));
            var placed = zone.GetAllEntities().SingleOrDefault(e => e.BlueprintName == trap);
            Assert.IsNotNull(placed);
            var cell = zone.GetEntityCell(placed);
            Assert.IsFalse(cell.Objects.Any(e => e.HasTag("Creature") || e.HasPart<ContainerPart>()));
            Assert.IsFalse(cell.Objects.Any(e => e.HasPart<StairsDownPart>() || e.HasPart<StairsUpPart>()));
        }

        [TestCase(BiomeType.Spread)]
        [TestCase(BiomeType.Sodden)]
        [TestCase(BiomeType.Beating)]
        [TestCase(BiomeType.Grovelands)]
        public void ActualLairPipeline_PlacesSparseUnoccupiedTraps(BiomeType biome)
        {
            OverworldZoneManager manager = null;
            (int x, int y)? target = null;
            for (int seed = 1; seed <= 100 && target == null; seed++)
            {
                manager = OverworldZoneManager.CreateDetached(_factory, seed);
                var map = manager.WorldMap;
                for (int x = 0; x < WorldMap.Width && target == null; x++)
                    for (int y = 0; y < WorldMap.Height && target == null; y++)
                        if (map.GetBiome(x, y) == biome && map.GetPOI(x, y)?.Type == POIType.Lair)
                            target = (x, y);
            }
            Assert.IsTrue(target.HasValue, "seed must exercise a real lair in " + biome);
            var zone = manager.GetZone($"Overworld.{target.Value.x}.{target.Value.y}.0");
            var traps = zone.GetAllEntities().Where(e => e.HasTag("Trap")).ToList();
            Assert.That(traps.Count, Is.InRange(1, 2));
            foreach (var trap in traps)
            {
                var cell = zone.GetEntityCell(trap);
                Assert.IsFalse(cell.Objects.Any(e => e.HasTag("Creature") || e.HasPart<ContainerPart>()));
                Assert.IsFalse(zone.GenReservedCells.Contains((cell.X, cell.Y)));
                Assert.That(cell.X, Is.InRange(3, Zone.Width - 4));
                Assert.That(cell.Y, Is.InRange(3, Zone.Height - 4));
            }
        }
        [TestCase(BiomeType.Beating)]
        [TestCase(BiomeType.Cave)]
        public void NativeWilderness_ReachesEveryReclaimedLandmark(BiomeType biome)
        {
            // A catalog entry alone is insufficient: composed terrain can reserve
            // every valid anchor. Probe the actual authored wilderness pipelines.
            var remaining = new HashSet<string>(biome == BiomeType.Beating
                ? new[] { "GlassblownObelisk", "BanditDugout", "SealedVault", "ClockworkWorkshop",
                    "RuneCultSite", "PalimpsestArchive", "CollapsedLibrary" }
                : new[] { "Ziggurat" });
            int generated = 0;
            for (int seed = 1; seed <= 4 && remaining.Count > 0; seed++)
            {
                var manager = OverworldZoneManager.CreateDetached(_factory, seed);
                for (int x = 0; x < WorldMap.Width && remaining.Count > 0; x++)
                    for (int y = 0; y < WorldMap.Height && remaining.Count > 0; y++)
                    {
                        if ((biome != BiomeType.Cave && manager.WorldMap.GetBiome(x, y) != biome)
                            || manager.WorldMap.GetPOI(x, y) != null) continue;
                        Diag.ResetAll();
                        int depth = biome == BiomeType.Cave ? 3 : 0;
                        var zone = manager.GetZone($"Overworld.{x}.{y}.{depth}");
                        Assert.IsNotNull(zone);
                        generated++;
                        foreach (var record in DiagQuery.Apply(new DiagQuery.Filter
                            { Category = "worldgen", Kind = "StructurePlaced", Limit = 20 }).Records)
                            remaining.RemoveWhere(name => record.PayloadJson.Contains("\"" + name + "\""));
                    }
            }
            Assert.Greater(generated, 0);
            Assert.IsEmpty(remaining, "unreachable through authored terrain: " + string.Join(", ", remaining));
        }

        [Test]
        public void ShallowUnderground_DoesNotRollTheReclaimedZiggurat()
        {
            for (int seed = 0; seed < 30; seed++)
            {
                var zone = new Zone();
                new LandmarkBuilder(BiomeType.Cave, 1, StampCatalog.Underground(2))
                    .BuildZone(zone, _factory, new Random(seed));
                Assert.IsFalse(zone.GetAllEntities().Any(e => e.BlueprintName == "ChoirTendril"
                    || e.BlueprintName == "PressurePlate"));
            }
        }

        [Test]
        public void OverwritWilderness_RemainsFreeOfAmbientStructuresAndTraps()
        {
            var manager = OverworldZoneManager.CreateDetached(_factory, 21);
            bool exercised = false;
            for (int x = 0; x < WorldMap.Width && !exercised; x++)
                for (int y = 0; y < WorldMap.Height && !exercised; y++)
                    if (manager.WorldMap.GetBiome(x, y) == BiomeType.Overwrit && manager.WorldMap.GetPOI(x, y) == null)
                    {
                        Diag.ResetAll();
                        var zone = manager.GetZone($"Overworld.{x}.{y}.0");
                        Assert.IsNotNull(zone);
                        Assert.IsFalse(zone.GetAllEntities().Any(e => e.HasTag("Trap")));
                        Assert.AreEqual(0, DiagQuery.Apply(new DiagQuery.Filter
                            { Category = "worldgen", Kind = "StructurePlaced", Limit = 20 }).Records.Count);
                        exercised = true;
                    }
            Assert.IsTrue(exercised);
        }

        // Locate the new optional builder by name so the full baseline runner can
        // execute these RED tests while the production file is temporarily absent.
        private static IZoneBuilder TrapBuilder()
        {
            var type = typeof(LandmarkBuilder).Assembly.GetType("CavesOfOoo.Core.TrapPlacementBuilder");
            Assert.IsNotNull(type, "lairs need a trap placement builder");
            return (IZoneBuilder)Activator.CreateInstance(type);
        }

        [Test]
        public void Traps_AllFourShippedKindsAreReachable_AndDeterministic()
        {
            var seen = new HashSet<string>();
            for (int seed = 0; seed < 30; seed++)
            {
                var a = new Zone();
                var b = new Zone();
                TrapBuilder().BuildZone(a, _factory, new Random(seed));
                TrapBuilder().BuildZone(b, _factory, new Random(seed));
                var signatureA = a.GetAllEntities().Select(e => e.BlueprintName + "@" + a.GetEntityPosition(e)).OrderBy(s => s);
                var signatureB = b.GetAllEntities().Select(e => e.BlueprintName + "@" + b.GetEntityPosition(e)).OrderBy(s => s);
                CollectionAssert.AreEqual(signatureA, signatureB);
                foreach (var trap in a.GetAllEntities()) seen.Add(trap.BlueprintName);
            }
            CollectionAssert.AreEquivalent(new[] { "SpikeTrap", "FireTrap", "BearTrap", "PressurePlate" }, seen);
        }

        [TestCase("reserved")]
        [TestCase("Wall")]
        [TestCase("StairsDown")]
        [TestCase("StairsUp")]
        [TestCase("WaterPuddle")]
        [TestCase("AshBed")]
        [TestCase("Chest")]
        [TestCase("Player")]
        public void Traps_RejectUnsafeCells(string obstacle)
        {
            var zone = new Zone();
            for (int x = 0; x < Zone.Width; x++)
                for (int y = 0; y < Zone.Height; y++)
                    zone.GenReservedCells.Add((x, y));
            if (obstacle != "reserved")
            {
                zone.GenReservedCells.Remove((40, 12));
                zone.AddEntity(_factory.CreateEntity(obstacle), 40, 12);
            }
            Diag.ResetAll();
            Assert.IsTrue(TrapBuilder().BuildZone(zone, _factory, new Random(1)));
            Assert.IsFalse(zone.GetAllEntities().Any(e => e.HasTag("Trap")));
            var records = DiagQuery.Apply(new DiagQuery.Filter
                { Category = "worldgen", Kind = "TrapsSkipped", Limit = 5 }).Records;
            Assert.AreEqual(1, records.Count);
            StringAssert.Contains("NoSafeCells", records[0].PayloadJson);
        }

        [Test]
        public void Traps_UseTheOnlySafeCell_AndEmitPlacement()
        {
            var zone = new Zone();
            for (int x = 0; x < Zone.Width; x++)
                for (int y = 0; y < Zone.Height; y++)
                    if (x != 40 || y != 12) zone.GenReservedCells.Add((x, y));
            Diag.ResetAll();
            Assert.IsTrue(TrapBuilder().BuildZone(zone, _factory, new Random(1)));
            var placed = zone.GetAllEntities().Single(e => e.HasTag("Trap"));
            Assert.AreEqual((40, 12), zone.GetEntityPosition(placed));
            var records = DiagQuery.Apply(new DiagQuery.Filter
                { Category = "worldgen", Kind = "TrapPlaced", Limit = 5 }).Records;
            Assert.AreEqual(1, records.Count);
            StringAssert.Contains(placed.BlueprintName, records[0].PayloadJson);
        }

        [Test]
        public void Traps_MinimalContentPack_IsASafeNoOp()
        {
            var zone = new Zone();
            Diag.ResetAll();
            Assert.IsTrue(TrapBuilder().BuildZone(zone, new EntityFactory(), new Random(1)));
            Assert.AreEqual(0, zone.GetAllEntities().Count);
            var records = DiagQuery.Apply(new DiagQuery.Filter
                { Category = "worldgen", Kind = "TrapsSkipped", Limit = 5 }).Records;
            Assert.AreEqual(1, records.Count);
            StringAssert.Contains("NoTrapBlueprints", records[0].PayloadJson);
        }

        [Test]
        public void Traps_NullInputs_AreRefusedAndReported()
        {
            var builder = TrapBuilder();
            Diag.ResetAll();
            Assert.IsFalse(builder.BuildZone(null, _factory, new Random(1)));
            Assert.IsFalse(builder.BuildZone(new Zone(), null, new Random(1)));
            Assert.IsFalse(builder.BuildZone(new Zone(), _factory, null));
            Assert.AreEqual(3, DiagQuery.Apply(new DiagQuery.Filter
                { Category = "worldgen", Kind = "TrapsSkipped", Limit = 5 }).Records.Count);
        }
    }
}
