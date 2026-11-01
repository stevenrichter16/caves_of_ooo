using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Chunk density (Docs/CHUNK-ACTIVITY.md §9): sparse wild formations gain structure (copses,
    /// field trees, hedge runs, tuft drifts, outcrops) in their empty sectors only, from approved
    /// art, on bare ground, never cutting a route and never drawing from the pipeline's RNG.
    /// </summary>
    public sealed class ChunkDensityTests
    {
        static EntityFactory factory;
        const int Seed = 64;

        [OneTimeSetUp] public void LoadOnce()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            LiquidRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/LiquidDefinitions").Select(t => t.text));
            FactionManager.Initialize(Resources.Load<TextAsset>("Content/Data/Factions").text);
            LootTableRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/Loot").Select(t => t.text).ToList());
            LoadoutPart.Factory = factory; LootDropSystem.Factory = factory; ContainerPlacementService.Factory = factory;
        }
        [SetUp] public void Setup() { Diag.ResetAll(); Diag.SetChannel("worldgen", true); }
        [TearDown] public void TearDown() { Diag.SetChannel("worldgen", false); }

        static Zone Filled(string ground, System.Func<int, int, string> extra = null, string id = "Overworld.10.11.0")
        {
            var z = new Zone(id);
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
            {
                z.AddEntity(factory.CreateEntity(ground), x, y);
                string e = extra?.Invoke(x, y); if (e != null) z.AddEntity(factory.CreateEntity(e), x, y);
            }
            return z;
        }
        static List<Diag.Entry> Records(string kind) => DiagQuery.Apply(new DiagQuery.Filter { Category = "worldgen", Kind = kind, Limit = 500 }).Records.ToList();
        static HashSet<string> Names(Zone z) => new HashSet<string>(z.GetAllEntities().Select(e => e.BlueprintName));

        // ════════════════ sectors ════════════════

        [Test] public void AnEmptyFieldGetsOneFeatureInEverySector()
        {
            var zone = Filled("Grass");
            var builder = new ChunkDensityBuilder(BiomeType.Spread, Formation.FieldStrips, Seed);
            builder.BuildZone(zone, factory, new System.Random(1));
            Assert.AreEqual(ChunkDensityBuilder.SectorCount, builder.Placed.Count, "one feature per empty sector");
            for (int s = 0; s < ChunkDensityBuilder.SectorCount; s++)
                Assert.GreaterOrEqual(ChunkDensityBuilder.StructureIn(zone, s), 1, "sector " + s + " gained structure");
            Assert.AreEqual(ChunkDensityBuilder.SectorCount, Records("DensityFeaturePlaced").Count);
        }

        [Test] public void ASectorThatAlreadyHasStructureIsLeftAlone()
        {
            // A bush every third cell: every sector is already dressed.
            var zone = Filled("Grass", (x, y) => x % 3 == 0 && y % 3 == 0 ? "Bush" : null);
            int before = zone.GetAllEntities().Count;
            var builder = new ChunkDensityBuilder(BiomeType.Spread, Formation.FieldStrips, Seed);
            builder.BuildZone(zone, factory, new System.Random(1));
            CollectionAssert.IsEmpty(builder.Placed);
            Assert.AreEqual(before, zone.GetAllEntities().Count);
        }

        [Test] public void OnlyTheEmptySectorsFill()
        {
            // The west half dressed, the east half bare: counter-check that the rule is per sector.
            var zone = Filled("Grass", (x, y) => x < 40 && x % 3 == 0 && y % 3 == 0 ? "Bush" : null);
            var builder = new ChunkDensityBuilder(BiomeType.Spread, Formation.FieldStrips, Seed);
            builder.BuildZone(zone, factory, new System.Random(1));
            Assert.AreEqual(ChunkDensityBuilder.SectorCount / 2, builder.Placed.Count);
            foreach (var r in Records("DensityFeaturePlaced"))
            {
                int sector = int.Parse(System.Text.RegularExpressions.Regex.Match(r.PayloadJson, "\"sector\":(\\d+)").Groups[1].Value);
                Assert.GreaterOrEqual(ChunkDensityBuilder.SectorColumn(sector), 2, "only the east sectors: " + r.PayloadJson);
            }
        }

        // ════════════════ safety ════════════════

        /// <summary>The four northern sectors are bare country whose only way out is one gate cell, (40,12),
        /// in a wall row; walls in the margins and in row 12 are structure of other sectors or outside the
        /// play area, so the northern sectors are still dressed. A solid feature on (40,11) would seal them
        /// in. Over many zones some features are drawn there; every one must be refused and undone.</summary>
        static Zone GateZone(string id)
        {
            var z = new Zone(id);
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
            {
                bool margin = y <= 12 && (y <= 1 || x <= 1 || x >= 78);
                bool gateRow = y == 12 && x != 40;
                z.AddEntity(factory.CreateEntity(margin || gateRow ? "SandstoneWall" : "Grass"), x, y);
            }
            return z;
        }

        [Test] public void AFeatureNeverCutsTheOnlyRoute()
        {
            int north = 0;
            for (int i = 0; i < 80; i++)
            {
                var zone = GateZone("Overworld." + (i % 20) + "." + (i / 20) + ".0");
                var before = WaysiteBuilder.Reach(zone);
                var builder = new ChunkDensityBuilder(BiomeType.Spread, Formation.OldRoad, Seed);
                builder.BuildZone(zone, factory, new System.Random(i));
                north += builder.Placed.Count(p => int.Parse(p.Split(':')[0]) < ChunkDensityBuilder.Columns);
                var after = WaysiteBuilder.Reach(zone);
                Assert.True(zone.GetCell(40, 11).IsPassable(), zone.ZoneID + ": the gate is closed");
                Assert.AreEqual(before.parts, after.parts, zone.ZoneID + ": the route is still one piece");
                int solids = zone.GetAllEntities().Count(e => !e.HasTag("Terrain") && !zone.GetEntityCell(e).IsPassable());
                Assert.GreaterOrEqual(after.cells, before.cells - solids, zone.ZoneID + ": cells were sealed off");
            }
            Assert.Greater(north, 80, "precondition: the northern sectors are really dressed");
        }

        [Test] public void FeaturesTakeOnlyBareUnreservedGround()
        {
            var zone = Filled("Grass");
            // Reserved cells (roads, portals, water) are not structure: a sector beside a road is still
            // bare and may be dressed, but never on the reserved cells themselves.
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++) if (x % 4 == 0) zone.GenReservedCells.Add((x, y));
            var builder = new ChunkDensityBuilder(BiomeType.Spread, Formation.FieldStrips, Seed);
            builder.BuildZone(zone, factory, new System.Random(1));
            Assert.Greater(builder.Placed.Count, 0, "precondition: the reserved columns leave room");
            foreach (var e in zone.GetAllEntities().Where(e => e.BlueprintName != "Grass"))
            {
                var c = zone.GetEntityCell(e);
                Assert.False(zone.GenReservedCells.Contains((c.X, c.Y)), e.BlueprintName + " on a reserved cell");
                Assert.AreEqual(2, c.Objects.Count, e.BlueprintName + " shares its cell only with the ground");
            }
        }

        [Test] public void ThePipelinesRandomStreamIsNeverTouchedAndTheResultIsStable()
        {
            var a = Filled("Grass"); var b = Filled("Grass");
            var rng = new System.Random(99); var twin = new System.Random(99);
            new ChunkDensityBuilder(BiomeType.Spread, Formation.OldRoad, Seed).BuildZone(a, factory, rng);
            new ChunkDensityBuilder(BiomeType.Spread, Formation.OldRoad, Seed).BuildZone(b, factory, new System.Random(5));
            Assert.AreEqual(twin.Next(), rng.Next(), "later builders keep their own draws");
            string Layout(Zone z) => string.Join(";", z.GetAllEntities().Where(e => e.BlueprintName != "Grass").Select(e => e.BlueprintName + "@" + z.GetEntityCell(e).X + "," + z.GetEntityCell(e).Y).OrderBy(s => s));
            Assert.AreEqual(Layout(a), Layout(b), "a pure function of seed and zone");
        }

        /// <summary>Hypothesis (from the surviving "bare check removed" and "route check removed" mutants): a
        /// cell holding one Terrain object is not necessarily open ground. A wall, a dune crest, a pool and
        /// flooded grass each hold only their ground; density must never build on them, and a sector of
        /// them is not empty country.</summary>
        [Test] public void NoFeatureStandsOnSolidGroundWaterOrFlood()
        {
            var zone = Filled("Sand", id: "Overworld.1.15.0");
            void Ground(int x, int y, string bp) { var c = zone.GetCell(x, y); zone.RemoveEntity(c.Objects[0]); zone.AddEntity(factory.CreateEntity(bp), x, y); }
            var special = new Dictionary<(int, int), string>();
            for (int y = 2; y <= 22; y++)
                for (int x = 2; x <= 77; x++)
                {
                    string bp = x % 5 == 0 ? "DuneCrest" : x % 5 == 1 && y % 2 == 0 ? "SandstoneWall" : x % 5 == 2 && y % 3 == 0 ? "BrinePool" : null;
                    if (bp != null) { Ground(x, y, bp); special[(x, y)] = bp; }
                    else if (x % 5 == 3 && y % 4 == 0) { zone.TileState.WriteCoating(x, y, "water", ZoneTileState.Permanent); special[(x, y)] = "flood"; }
                }
            foreach (var kv in special) Assert.False(ChunkDensityBuilder.Bare(zone, kv.Key.Item1, kv.Key.Item2), kv.Value + " is not bare ground");
            Assert.True(ChunkDensityBuilder.Bare(zone, 4, 3), "counter-check: plain sand is bare");
            var dunes = new ChunkDensityBuilder(BiomeType.Beating, Formation.RuinField, Seed);
            dunes.BuildZone(zone, factory, new System.Random(1));
            CollectionAssert.IsEmpty(dunes.Placed, "a field of crests, walls and pools is already structured");

            // Every fifth column of crests only: sectors are dressed by their crests (structure), so open the
            // east half to bare sand and check nothing lands on the crests left in the west.
            var open = Filled("Sand", id: "Overworld.1.15.0");
            for (int y = 2; y <= 22; y++) for (int x = 2; x <= 38; x++) if (x % 5 == 0) { var c = open.GetCell(x, y); open.RemoveEntity(c.Objects[0]); open.AddEntity(factory.CreateEntity("DuneCrest"), x, y); }
            open.TileState.WriteCoating(60, 12, "water", ZoneTileState.Permanent);
            var east = new ChunkDensityBuilder(BiomeType.Beating, Formation.RuinField, Seed);
            east.BuildZone(open, factory, new System.Random(1));
            Assert.Greater(east.Placed.Count, 0, "counter-check: the bare east half is dressed");
            foreach (var e in open.GetAllEntities().Where(e => e.BlueprintName == "Rock" || e.BlueprintName == "DryBrush"))
            {
                var c = open.GetEntityCell(e);
                Assert.AreEqual(2, c.Objects.Count, e.BlueprintName + " stacked at " + c.X + "," + c.Y);
                Assert.AreNotEqual("DuneCrest", c.Objects[0].BlueprintName);
                Assert.False(c.X == 60 && c.Y == 12, "nothing grows in the flood");
            }
        }

        /// <summary>A sector holding a few owners (below <see cref="ChunkDensityBuilder.MinStructure"/>) is
        /// still dressed, but no feature cell lands on an existing owner.</summary>
        [Test] public void AFeatureNeverStacksOnAnExistingOwner()
        {
            var zone = Filled("Grass", (x, y) => (x * 7 + y * 13) % 53 == 0 ? "Bush" : null);
            for (int s = 0; s < ChunkDensityBuilder.SectorCount; s++)
                Assert.That(ChunkDensityBuilder.StructureIn(zone, s), Is.InRange(1, ChunkDensityBuilder.MinStructure - 1), "precondition: sector " + s + " has a few owners");
            var builder = new ChunkDensityBuilder(BiomeType.Spread, Formation.FlowerMeadow, Seed);
            builder.BuildZone(zone, factory, new System.Random(1));
            Assert.Greater(builder.Placed.Count, 0);
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                Assert.LessOrEqual(zone.GetCell(x, y).Objects.Count, 2, "stacked at " + x + "," + y);
        }

        /// <summary>Hypothesis: a road cell holds only its RoadStone ground, so a "bare ground" rule alone
        /// would let a hedge run lie across the old road; the route check only guards connectivity.</summary>
        [Test] public void NoFeatureIsBuiltOnTheRoadOrIndoors()
        {
            var zone = Filled("Grass");
            for (int x = 0; x < Zone.Width; x++)
                for (int y = 11; y <= 12; y++)
                {
                    var cell = zone.GetCell(x, y);
                    zone.RemoveEntity(cell.Objects[0]); zone.AddEntity(factory.CreateEntity("RoadStone"), x, y);
                }
            for (int x = 60; x <= 77; x++) for (int y = 2; y <= 8; y++) zone.GetCell(x, y).IsInterior = true;
            var builder = new ChunkDensityBuilder(BiomeType.Spread, Formation.OldRoad, Seed);
            builder.BuildZone(zone, factory, new System.Random(1));
            Assert.Greater(builder.Placed.Count, 0, "counter-check: the fields beside the road are dressed");
            for (int x = 0; x < Zone.Width; x++)
                for (int y = 0; y < Zone.Height; y++)
                {
                    var cell = zone.GetCell(x, y);
                    if (y == 11 || y == 12) Assert.AreEqual(1, cell.Objects.Count, "the road at " + x + "," + y + " stays clear");
                    if (cell.IsInterior) Assert.AreEqual(1, cell.Objects.Count, "indoors at " + x + "," + y);
                }
        }

        [TestCase(BiomeType.Spread, Formation.FieldStrips, "Grass")] [TestCase(BiomeType.Spread, Formation.FlowerMeadow, "Grass")]
        [TestCase(BiomeType.Spread, Formation.OldRoad, "Grass")] [TestCase(BiomeType.Spread, Formation.RiverMeadow, "Grass")]
        [TestCase(BiomeType.Beating, Formation.CaravanRoad, "Sand")]
        [TestCase(BiomeType.Beating, Formation.RuinField, "Sand")]
        public void OnlyApprovedArtIsPlaced(BiomeType biome, Formation formation, string ground)
        {
            var zone = Filled(ground, id: biome == BiomeType.Beating ? "Overworld.1.15.0" : "Overworld.10.11.0");
            var builder = new ChunkDensityBuilder(biome, formation, Seed);
            builder.BuildZone(zone, factory, new System.Random(1));
            Assert.Greater(builder.Placed.Count, 0, "precondition: something placed");
            var allowed = biome == BiomeType.Spread ? new[] { "Tree", "Bush", "Hedge" } : new[] { "Rock", "DryBrush" };
            var placed = Names(zone); placed.Remove(ground);
            CollectionAssert.IsSubsetOf(placed, allowed, biome + " " + formation);
            // The meadow already has its flowers: CharmFlowers drifts from the composition and FlowerField,
            // Urqu's bleed instrument (Docs/FELLING-W1-W2-PLAN.md SM3). Density adds none.
            CollectionAssert.DoesNotContain(placed, "CharmFlowers");
            CollectionAssert.DoesNotContain(placed, "FlowerField");
        }

        // ════════════════ eligibility and real chunks ════════════════

        [Test] public void OnlySparseFormationsAreDressed()
        {
            foreach (var f in new[] { Formation.FieldStrips, Formation.FlowerMeadow, Formation.OldRoad, Formation.RiverMeadow })
                Assert.True(ChunkDensityPlan.IsSparse(BiomeType.Spread, f), f.ToString());
            foreach (var f in new[] { Formation.Hedgerow, Formation.Fallow })
                Assert.False(ChunkDensityPlan.IsSparse(BiomeType.Spread, f), f + " is already structured");
            foreach (var f in new[] { Formation.CaravanRoad, Formation.RuinField })
                Assert.True(ChunkDensityPlan.IsSparse(BiomeType.Beating, f), f.ToString());
            Assert.False(ChunkDensityPlan.IsSparse(BiomeType.Beating, Formation.SaltPan), "the salt pan is a zero-cover duel floor by design");
            Assert.False(ChunkDensityPlan.IsSparse(BiomeType.Beating, Formation.DuneBelt), "the dune crests are the belt's structure");
            foreach (var f in new[] { Formation.WindBarrens, Formation.BrineLens })
                Assert.False(ChunkDensityPlan.IsSparse(BiomeType.Beating, f), f + " keeps its own grammar");
            Assert.False(ChunkDensityPlan.IsSparse(BiomeType.Sodden, Formation.OpenMire), "the Sodden reads as intended");
            Assert.False(ChunkDensityPlan.IsSparse(BiomeType.Overwrit, Formation.None), "the Overwrit's emptiness is canon");
        }

        [Test] public void CuratedAndExplorationOwnedZonesAreNeverDressed()
        {
            var manager = OverworldZoneManager.CreateDetached(factory, Seed, enableExploration: true);
            foreach (var id in new[] { "Overworld.11.9.0", "Overworld.11.8.0", "Overworld.12.10.0", "Overworld.12.11.0", ReferenceGladePlan.ZoneID })
                Assert.False(ChunkDensityPlan.IsEligible(manager, id), id);
            int owned = 0, eligible = 0;
            for (int x = 0; x < WorldMap.Width; x++) for (int y = 0; y < WorldMap.Height; y++)
            {
                string id = WorldMap.ToZoneID(x, y, 0);
                if (WorldMapAuthoring.BiomeAt(x, y) != BiomeType.Spread) continue;
                bool family = manager.Exploration.TryGetPlacement(manager, id, out var entry) && entry.Family != SpreadExplorationFamily.None;
                if (family) { owned++; Assert.False(ChunkDensityPlan.IsEligible(manager, id), id + " belongs to " + entry.Family); }
                else if (ChunkDensityPlan.IsEligible(manager, id)) eligible++;
            }
            Assert.Greater(owned, 0, "precondition: some chunks have exploration families");
            Assert.Greater(eligible, 10, "counter-check: ordinary sparse chunks are eligible");
        }

        [TestCase(BiomeType.Spread)] [TestCase(BiomeType.Beating)]
        public void EveryOwnerInADressedSectorDrawsNativeArtOnItsGround(BiomeType biome)
        {
            var manager = OverworldZoneManager.CreateDetached(factory, Seed, enableExploration: true);
            var structure = new HashSet<string> { "Tree", "Bush", "Hedge", "Rock", "DryBrush" };
            var failures = new List<string>(); int zones = 0, owners = 0;
            using (var world = new FlatPixelGreyboxCatalog())
                for (int x = 0; x < WorldMap.Width && zones < 6; x++) for (int y = 0; y < WorldMap.Height && zones < 6; y++)
                {
                    string id = WorldMap.ToZoneID(x, y, 0);
                    if (WorldMapAuthoring.BiomeAt(x, y) != biome || !ChunkDensityPlan.IsEligible(manager, id)) continue;
                    Diag.ResetAll(); Diag.SetChannel("worldgen", true);
                    var zone = manager.GetZone(id); zones++; manager.SetActiveZone(zone);
                    foreach (var c in zone.Cells) c.IsVisible = c.Explored = true;
                    foreach (var r in Records("DensityFeaturePlaced"))
                    {
                        int sector = int.Parse(System.Text.RegularExpressions.Regex.Match(r.PayloadJson, "\"sector\":(\\d+)").Groups[1].Value);
                        foreach (var e in zone.GetAllEntities())
                        {
                            if (!structure.Contains(e.BlueprintName)) continue;
                            var cell = zone.GetEntityCell(e);
                            if (ChunkDensityBuilder.SectorOf(cell.X, cell.Y) != sector) continue;
                            owners++;
                            if (!world.TryResolvePresentation(zone, cell.X, cell.Y, out var v) || !ReferenceEquals(v.Owner, e)) continue;   // covered by another owner
                            if (!v.NativeForeground) failures.Add(id + " " + e.BlueprintName + "@" + cell.X + "," + cell.Y + ": glyph");
                            else if (!v.LocalGround) failures.Add(id + " " + e.BlueprintName + "@" + cell.X + "," + cell.Y + ": not on its real ground");
                        }
                    }
                }
            Assert.Greater(zones, 2, "precondition: eligible " + biome + " chunks");
            Assert.Greater(owners, 10, "precondition: dressed sectors to check");
            CollectionAssert.IsEmpty(failures, biome + ": " + owners + " owners checked");
        }

        [Test] public void RealSparseChunksFillTheirEmptySectors()
        {
            var manager = OverworldZoneManager.CreateDetached(factory, Seed, enableExploration: true);
            int zones = 0, features = 0;
            for (int x = 0; x < WorldMap.Width && zones < 8; x++) for (int y = 0; y < WorldMap.Height && zones < 8; y++)
            {
                string id = WorldMap.ToZoneID(x, y, 0);
                if (!ChunkDensityPlan.IsEligible(manager, id)) continue;
                Diag.ResetAll(); Diag.SetChannel("worldgen", true);
                var zone = manager.GetZone(id); zones++;
                features += Records("DensityFeaturePlaced").Count;
                var refused = new HashSet<int>(Records("DensityFeatureRefused").Select(r => int.Parse(System.Text.RegularExpressions.Regex.Match(r.PayloadJson, "\"sector\":(\\d+)").Groups[1].Value)));
                for (int s = 0; s < ChunkDensityBuilder.SectorCount; s++)
                    if (!refused.Contains(s)) Assert.GreaterOrEqual(ChunkDensityBuilder.StructureIn(zone, s), 1, id + " sector " + s + " is still empty");
            }
            Assert.AreEqual(8, zones);
            Assert.Greater(features, 8, "the sparse chunks really gained structure");
        }
    }
}
