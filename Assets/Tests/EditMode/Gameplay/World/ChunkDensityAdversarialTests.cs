using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Adversarial sweep for chunk density (Docs/CHUNK-ACTIVITY.md §9). Surfaces probed: boundary
    /// inputs, unknown content, shape invariants, route and pocket safety, play-area margins,
    /// atomic undo residue, determinism and order independence, diag accounting, and the
    /// cross-system interaction with waysites in the real pipeline.
    /// </summary>
    public sealed class ChunkDensityAdversarialTests
    {
        static EntityFactory factory;
        const int Seed = 64;
        static readonly Formation[] SpreadSparse = { Formation.FieldStrips, Formation.FlowerMeadow, Formation.OldRoad, Formation.RiverMeadow };
        static readonly Formation[] BeatingSparse = { Formation.CaravanRoad, Formation.RuinField };

        [OneTimeSetUp] public void LoadOnce()
        {
            factory = Load();
            LiquidRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/LiquidDefinitions").Select(t => t.text));
            FactionManager.Initialize(Resources.Load<TextAsset>("Content/Data/Factions").text);
            LootTableRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/Loot").Select(t => t.text).ToList());
            LoadoutPart.Factory = factory; LootDropSystem.Factory = factory; ContainerPlacementService.Factory = factory;
        }
        static EntityFactory Load()
        {
            var f = new EntityFactory();
            f.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            return f;
        }
        [SetUp] public void Setup() { Diag.ResetAll(); Diag.SetChannel("worldgen", true); }
        [TearDown] public void TearDown() { Diag.SetChannel("worldgen", false); }

        static Zone Filled(string ground, string id = "Overworld.10.11.0", EntityFactory f = null)
        {
            f = f ?? factory;
            var z = new Zone(id);
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++) z.AddEntity(f.CreateEntity(ground), x, y);
            return z;
        }
        static List<Diag.Entry> Records(string kind) => DiagQuery.Apply(new DiagQuery.Filter { Category = "worldgen", Kind = kind, Limit = 2000 }).Records.ToList();
        static int Field(Diag.Entry r, string name) => int.Parse(Regex.Match(r.PayloadJson, "\"" + name + "\":(-?\\d+)").Groups[1].Value);
        static string Text(Diag.Entry r, string name) => Regex.Match(r.PayloadJson, "\"" + name + "\":\"([^\"]*)\"").Groups[1].Value;
        static string Layout(Zone z, string ground) => string.Join(";", z.GetAllEntities().Where(e => e.BlueprintName != ground)
            .Select(e => e.BlueprintName + "@" + z.GetEntityCell(e).X + "," + z.GetEntityCell(e).Y).OrderBy(s => s));

        /// <summary>Every passable cell is reachable from a passable edge cell (4-way).</summary>
        static int Pockets(Zone zone)
        {
            var seen = new bool[Zone.Width, Zone.Height]; var queue = new Queue<(int, int)>();
            void Push(int x, int y)
            {
                if (x < 0 || y < 0 || x >= Zone.Width || y >= Zone.Height || seen[x, y] || !zone.GetCell(x, y).IsPassable()) return;
                seen[x, y] = true; queue.Enqueue((x, y));
            }
            for (int x = 0; x < Zone.Width; x++) { Push(x, 0); Push(x, Zone.Height - 1); }
            for (int y = 0; y < Zone.Height; y++) { Push(0, y); Push(Zone.Width - 1, y); }
            while (queue.Count > 0) { var (x, y) = queue.Dequeue(); Push(x + 1, y); Push(x - 1, y); Push(x, y + 1); Push(x, y - 1); }
            int pockets = 0;
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++) if (zone.GetCell(x, y).IsPassable() && !seen[x, y]) pockets++;
            return pockets;
        }

        // ════════════════ boundary inputs ════════════════

        /// <summary>A null zone or factory is a no-op, never a throw: the pipeline treats a false or a
        /// throw from a builder as a failed generation.</summary>
        [Test] public void Adversarial_NullZoneOrFactoryIsANoOp()
        {
            var builder = new ChunkDensityBuilder(BiomeType.Spread, Formation.FieldStrips, Seed);
            Assert.True(builder.BuildZone(null, factory, new System.Random(1)));
            CollectionAssert.IsEmpty(builder.Placed);
            var zone = Filled("Grass"); int before = zone.GetAllEntities().Count;
            Assert.True(builder.BuildZone(zone, null, new System.Random(1)));
            Assert.AreEqual(before, zone.GetAllEntities().Count);
            Assert.True(builder.BuildZone(zone, factory, null), "a null pipeline RNG is never read");
            Assert.Greater(builder.Placed.Count, 0, "counter-check: with a factory it dresses");
        }

        /// <summary>A builder made for a formation that is not sparse places nothing, even on bare ground.</summary>
        [TestCase(BiomeType.Spread, Formation.Hedgerow)] [TestCase(BiomeType.Spread, Formation.Fallow)]
        [TestCase(BiomeType.Beating, Formation.SaltPan)] [TestCase(BiomeType.Beating, Formation.WindBarrens)] [TestCase(BiomeType.Beating, Formation.DuneBelt)]
        [TestCase(BiomeType.Sodden, Formation.OpenMire)] [TestCase(BiomeType.Overwrit, Formation.None)]
        [TestCase(BiomeType.Spread, Formation.CaravanRoad)]
        public void Adversarial_ANonSparseFormationIsNeverDressed(BiomeType biome, Formation formation)
        {
            var zone = Filled("Grass"); int before = zone.GetAllEntities().Count;
            var builder = new ChunkDensityBuilder(biome, formation, Seed);
            Assert.True(builder.BuildZone(zone, factory, new System.Random(1)));
            CollectionAssert.IsEmpty(builder.Placed);
            Assert.AreEqual(before, zone.GetAllEntities().Count);
            CollectionAssert.IsEmpty(ChunkDensityCatalog.For(biome, formation));
        }

        /// <summary>Unknown content: a factory without Hedge refuses the hedge run with a reason and places
        /// none of it, but the sector still tries the formation's other features.</summary>
        [Test] public void Adversarial_AMissingBlueprintRefusesOnlyItsFeature()
        {
            var thin = Load(); thin.Blueprints.Remove("Hedge");
            var zone = Filled("Grass", f: thin);
            var builder = new ChunkDensityBuilder(BiomeType.Spread, Formation.FieldStrips, Seed);
            builder.BuildZone(zone, thin, new System.Random(1));
            Assert.False(zone.GetAllEntities().Any(e => e.BlueprintName == "Hedge"));
            Assert.Greater(builder.Placed.Count, 0, "field trees and tufts still dress the field");
            CollectionAssert.DoesNotContain(builder.Placed.Select(p => p.Split(':')[1]).ToList(), "HedgeRun");
            var full = Filled("Grass");
            var counter = new ChunkDensityBuilder(BiomeType.Spread, Formation.FieldStrips, Seed);
            Diag.ResetAll(); Diag.SetChannel("worldgen", true);
            counter.BuildZone(full, factory, new System.Random(1));
            Assert.True(counter.Placed.Any(p => p.EndsWith(":HedgeRun")), "counter-check: with Hedge known, hedge runs appear");
        }

        // ════════════════ shape invariants ════════════════

        /// <summary>Over many draws: no duplicate cells, the anchor is part of the shape, every blueprint
        /// belongs to its biome's approved set, and each feature stays inside its documented size.</summary>
        [Test] public void Adversarial_ShapesAreWellFormedOverManyDraws()
        {
            var spread = new HashSet<string> { "Tree", "Bush", "Hedge" };
            var beating = new HashSet<string> { "Rock", "DryBrush" };
            var sizes = new Dictionary<DensityFeature, (int min, int max)>
            {
                { DensityFeature.CopseKnot, (5, 8) }, { DensityFeature.FieldTree, (2, 3) }, { DensityFeature.HedgeRun, (5, 9) },
                { DensityFeature.TuftDrift, (5, 8) }, { DensityFeature.Outcrop, (4, 8) }, { DensityFeature.BrushClump, (3, 6) },
            };
            foreach (DensityFeature feature in System.Enum.GetValues(typeof(DensityFeature)))
                for (int seed = 0; seed < 300; seed++)
                foreach (int lee in new[] { -1, 1 })
                {
                    var cells = ChunkDensityCatalog.Shape(feature, new System.Random(seed), lee);
                    string at = feature + " seed " + seed + " lee " + lee;
                    Assert.AreEqual(cells.Count, cells.Select(c => (c.dx, c.dy)).Distinct().Count(), at + ": duplicate cell");
                    Assert.True(cells.Any(c => c.dx == 0 && c.dy == 0), at + ": anchor missing");
                    var allowed = feature == DensityFeature.Outcrop || feature == DensityFeature.BrushClump ? beating : spread;
                    Assert.True(cells.All(c => allowed.Contains(c.blueprint)), at + ": foreign blueprint");
                    if (feature != DensityFeature.HedgeRun)
                        Assert.That(cells.Count, Is.InRange(sizes[feature].min, sizes[feature].max), at + ": size");
                    Assert.True(cells.All(c => System.Math.Abs(c.dx) <= 12 && System.Math.Abs(c.dy) <= 12), at + ": runaway shape");
                }
        }

        /// <summary>A long hedge run keeps a gate: a field boundary you can never pass through reads as a
        /// wall. Short runs are short enough to walk round.</summary>
        [Test] public void Adversarial_ALongHedgeRunHasAGate()
        {
            int longRuns = 0;
            for (int seed = 0; seed < 300; seed++)
            {
                var cells = ChunkDensityCatalog.Shape(DensityFeature.HedgeRun, new System.Random(seed), 1);
                bool vertical = cells.All(c => c.dx == 0);
                var along = cells.Select(c => vertical ? c.dy : c.dx).OrderBy(v => v).ToList();
                int span = along.Last() - along.First() + 1;
                Assert.True(vertical || cells.All(c => c.dy == 0), "a run is straight");
                if (span >= 7) { longRuns++; Assert.Less(cells.Count, span, "seed " + seed + ": a " + span + "-cell run has no gap"); }
                else Assert.AreEqual(span, cells.Count, "a short run is solid");
            }
            Assert.Greater(longRuns, 0, "precondition: long runs are drawn");
        }

        /// <summary>The outcrop's brush gathers on the lee side of its rocks, whichever side that is: a
        /// shape that ignored the lee would put brush on the windward face.</summary>
        [Test] public void Adversarial_OutcropBrushLiesInTheLee()
        {
            foreach (int lee in new[] { -1, 1 })
                for (int seed = 0; seed < 200; seed++)
                {
                    var cells = ChunkDensityCatalog.Shape(DensityFeature.Outcrop, new System.Random(seed), lee);
                    var rocks = new HashSet<(int, int)>(cells.Where(c => c.blueprint == "Rock").Select(c => (c.dx, c.dy)));
                    foreach (var b in cells.Where(c => c.blueprint == "DryBrush"))
                        Assert.True(rocks.Contains((b.dx - lee, b.dy)), "seed " + seed + " lee " + lee + ": brush at " + b.dx + "," + b.dy + " has no rock to windward");
                }
        }

        // ════════════════ route, pocket and margin safety ════════════════

        /// <summary>Over many zones and every sparse formation: no feature seals off a passable cell, and no
        /// feature cell lies outside the play area (the zone edges carry the transitions).</summary>
        [Test] public void Adversarial_NoFeatureMakesAPocketOrTouchesTheEdges()
        {
            int placed = 0;
            foreach (var (biome, formations, ground) in new[] { (BiomeType.Spread, SpreadSparse, "Grass"), (BiomeType.Beating, BeatingSparse, "Sand") })
                foreach (var formation in formations)
                    for (int i = 0; i < 6; i++)
                    {
                        string id = "Overworld." + (3 + i) + "." + (4 + i) + ".0";
                        var zone = Filled(ground, id);
                        var builder = new ChunkDensityBuilder(biome, formation, Seed);
                        builder.BuildZone(zone, factory, new System.Random(i));
                        placed += builder.Placed.Count;
                        Assert.AreEqual(0, Pockets(zone), id + " " + formation + ": a sealed pocket");
                        foreach (var e in zone.GetAllEntities().Where(e => e.BlueprintName != ground))
                        {
                            var c = zone.GetEntityCell(e);
                            Assert.That(c.X, Is.InRange(2, 77), id + " " + e.BlueprintName); Assert.That(c.Y, Is.InRange(2, 22), id + " " + e.BlueprintName);
                        }
                    }
            Assert.Greater(placed, 100, "precondition: features were really placed");
        }

        /// <summary>A refused feature leaves no residue. The northern sectors' only way out is one gate cell,
        /// so a feature drawn across it is placed, measured, refused and undone; across many zones the zone
        /// gains exactly the cells of the features that were kept.</summary>
        [Test] public void Adversarial_ARefusedFeatureLeavesNoResidue()
        {
            int kept = 0;
            for (int i = 0; i < 60; i++)
            {
                var zone = new Zone("Overworld." + (i % 15) + "." + (5 + i / 15) + ".0");
                for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                {
                    bool wall = y <= 12 && (y <= 1 || x <= 1 || x >= 78) || y == 12 && x != 40;
                    zone.AddEntity(factory.CreateEntity(wall ? "SandstoneWall" : "Grass"), x, y);
                }
                int before = zone.GetAllEntities().Count;
                Diag.ResetAll(); Diag.SetChannel("worldgen", true);
                new ChunkDensityBuilder(BiomeType.Spread, Formation.FieldStrips, Seed).BuildZone(zone, factory, new System.Random(i));
                int cells = Records("DensityFeaturePlaced").Sum(r => Field(r, "cells"));
                kept += cells;
                Assert.AreEqual(before + cells, zone.GetAllEntities().Count, zone.ZoneID + ": only kept features remain");
                Assert.AreEqual(0, Pockets(zone), zone.ZoneID + ": sealed");
            }
            Assert.Greater(kept, 0, "precondition: features were kept");
        }

        // ════════════════ determinism ════════════════

        /// <summary>Order independence: dressing zone B first does not change zone A, and two zones with
        /// different IDs do not share a layout (the seed really keys on the zone).</summary>
        [Test] public void Adversarial_ALayoutDependsOnlyOnSeedAndZone()
        {
            string A(Zone first)
            {
                if (first != null) new ChunkDensityBuilder(BiomeType.Spread, Formation.OldRoad, Seed).BuildZone(first, factory, new System.Random(3));
                var a = Filled("Grass", "Overworld.7.7.0");
                new ChunkDensityBuilder(BiomeType.Spread, Formation.OldRoad, Seed).BuildZone(a, factory, new System.Random(9));
                return Layout(a, "Grass");
            }
            Assert.AreEqual(A(null), A(Filled("Grass", "Overworld.8.7.0")));
            var b = Filled("Grass", "Overworld.8.7.0");
            new ChunkDensityBuilder(BiomeType.Spread, Formation.OldRoad, Seed).BuildZone(b, factory, new System.Random(9));
            Assert.AreNotEqual(A(null), Layout(b, "Grass"), "counter-check: another zone gets its own layout");
            var c = Filled("Grass", "Overworld.7.7.0");
            new ChunkDensityBuilder(BiomeType.Spread, Formation.OldRoad, Seed + 1).BuildZone(c, factory, new System.Random(9));
            Assert.AreNotEqual(A(null), Layout(c, "Grass"), "counter-check: another world gets its own layout");
        }

        /// <summary>The lee is a property of the zone, and both sides occur across a world.</summary>
        [Test] public void Adversarial_BothLeeSidesOccur()
        {
            var sides = new HashSet<int>();
            for (int i = 0; i < 40 && sides.Count < 2; i++)
                sides.Add((WaysitePlan.Rank(Seed, "Overworld." + i + ".15.0", "lee") & 1) == 0 ? -1 : 1);
            Assert.AreEqual(2, sides.Count);
        }

        // ════════════════ diag accounting ════════════════

        /// <summary>Every sector is accounted for: dressed already, a placed record, or a refused record with
        /// a reason. Placed matches the records one for one.</summary>
        [Test] public void Adversarial_EverySectorIsAccountedFor()
        {
            var zone = Filled("Sand", "Overworld.1.15.0");
            for (int x = 2; x < 20; x++) for (int y = 2; y < 12; y++) if ((x + y) % 2 == 0) zone.AddEntity(factory.CreateEntity("Rock"), x, y);
            var dressed = Enumerable.Range(0, ChunkDensityBuilder.SectorCount).Where(s => ChunkDensityBuilder.StructureIn(zone, s) >= ChunkDensityBuilder.MinStructure).ToList();
            var builder = new ChunkDensityBuilder(BiomeType.Beating, Formation.RuinField, Seed);
            builder.BuildZone(zone, factory, new System.Random(1));
            var placed = Records("DensityFeaturePlaced"); var refused = Records("DensityFeatureRefused");
            Assert.AreEqual(builder.Placed.Count, placed.Count);
            CollectionAssert.AreEquivalent(builder.Placed, placed.Select(r => Field(r, "sector") + ":" + Text(r, "feature")));
            foreach (var r in refused) Assert.IsNotEmpty(Text(r, "reason"));
            var seen = dressed.Concat(placed.Select(r => Field(r, "sector"))).Concat(refused.Select(r => Field(r, "sector"))).ToList();
            CollectionAssert.AreEquivalent(Enumerable.Range(0, ChunkDensityBuilder.SectorCount), seen, "each sector exactly once");
            Assert.IsNotEmpty(dressed, "precondition: one sector was already dressed");
        }

        /// <summary>With the channel off nothing is recorded, and the layout is the same: diagnostics never
        /// steer generation.</summary>
        [Test] public void Adversarial_DiagnosticsNeverSteerTheLayout()
        {
            var on = Filled("Grass");
            new ChunkDensityBuilder(BiomeType.Spread, Formation.RiverMeadow, Seed).BuildZone(on, factory, new System.Random(1));
            Assert.Greater(Records("DensityFeaturePlaced").Count, 0);
            Diag.ResetAll(); Diag.SetChannel("worldgen", false);
            var off = Filled("Grass");
            new ChunkDensityBuilder(BiomeType.Spread, Formation.RiverMeadow, Seed).BuildZone(off, factory, new System.Random(1));
            Assert.AreEqual(0, Records("DensityFeaturePlaced").Count + Records("DensityFeatureRefused").Count);
            Assert.AreEqual(Layout(on, "Grass"), Layout(off, "Grass"));
        }

        // ════════════════ the real pipeline ════════════════

        sealed class NoDensity : OverworldZoneManager
        {
            public NoDensity(EntityFactory f, int seed) : base(f, seed) { }
            protected override ZoneGenerationPipeline GetPipelineForZone(string id)
            {
                var p = base.GetPipelineForZone(id); p?.RemoveBuilders<ChunkDensityBuilder>(); return p;
            }
        }

        sealed class Probe : IZoneBuilder
        {
            readonly int priority; readonly System.Action<Zone> act;
            public Probe(int priority, System.Action<Zone> act) { this.priority = priority; this.act = act; }
            public string Name => "DensityProbe"; public int Priority => priority;
            public bool BuildZone(Zone zone, EntityFactory f, System.Random rng) { act(zone); return true; }
        }

        /// <summary>Records every owner just before density runs and checks it just after: density only adds,
        /// on bare cells, and never moves or removes what the pipeline placed before it.</summary>
        sealed class Bracketed : OverworldZoneManager
        {
            public readonly List<string> Failures = new List<string>(); public int Added, Zones;
            List<(Entity e, int x, int y)> before;
            public Bracketed(EntityFactory f, int seed) : base(f, seed) { }
            protected override ZoneGenerationPipeline GetPipelineForZone(string id)
            {
                var p = base.GetPipelineForZone(id);
                if (p == null || !p.Builders.OfType<ChunkDensityBuilder>().Any()) return p;
                p.AddBuilder(new Probe(3859, z => before = z.GetAllEntities().Select(e => (e, z.GetEntityCell(e).X, z.GetEntityCell(e).Y)).ToList()));
                p.AddBuilder(new Probe(3861, z =>
                {
                    Zones++;
                    var kept = new HashSet<Entity>();
                    foreach (var (e, x, y) in before)
                    {
                        var c = z.GetEntityCell(e);
                        if (c == null || c.X != x || c.Y != y) Failures.Add(z.ZoneID + " " + e.BlueprintName + "@" + x + "," + y + " moved or removed");
                        kept.Add(e);
                    }
                    foreach (var e in z.GetAllEntities().Where(e => !kept.Contains(e)))
                    {
                        Added++;
                        var c = z.GetEntityCell(e);
                        if (c.Objects.Count != 2) Failures.Add(z.ZoneID + " " + e.BlueprintName + "@" + c.X + "," + c.Y + " stacked");
                    }
                }));
                return p;
            }
        }

        /// <summary>Cross-system ordering: density runs after the landmarks and waysites, so it only takes ground
        /// they left. A dressed chunk keeps exactly the waysites it would have had, and nothing placed before
        /// density moves or disappears. Moving density earlier in the pipeline would break this pin.</summary>
        [Test] public void Adversarial_DensityNeverCostsAChunkItsWaysites()
        {
            var with = new Bracketed(factory, Seed); var without = new NoDensity(factory, Seed);
            int zones = 0, sites = 0;
            for (int x = 0; x < WorldMap.Width && zones < 10; x++) for (int y = 0; y < WorldMap.Height && zones < 10; y++)
            {
                string id = WorldMap.ToZoneID(x, y, 0);
                if (!ChunkDensityPlan.IsEligible(with, id)) continue;
                Diag.ResetAll(); Diag.SetChannel("worldgen", true);
                without.GetZone(id); int expected = Records("WaysitePlaced").Count;
                Diag.ResetAll(); Diag.SetChannel("worldgen", true);
                with.GetZone(id); int actual = Records("WaysitePlaced").Count;
                Assert.AreEqual(expected, actual, id + " lost a waysite to density");
                zones++; sites += actual;
            }
            Assert.AreEqual(10, zones); Assert.Greater(sites, 0, "precondition: these chunks have waysites");
            Assert.AreEqual(10, with.Zones, "precondition: the probes bracketed every density run");
            Assert.Greater(with.Added, 0, "precondition: density added owners");
            CollectionAssert.IsEmpty(with.Failures);
        }

        /// <summary>A fresh manager regenerates the same density: a chunk looks the same when the world is
        /// rebuilt from its seed.</summary>
        [Test] public void Adversarial_ARealChunkRegeneratesIdentically()
        {
            var a = OverworldZoneManager.CreateDetached(factory, Seed, enableExploration: true);
            string id = null; var first = new List<string>();
            for (int x = 0; x < WorldMap.Width && id == null; x++) for (int y = 0; y < WorldMap.Height && id == null; y++)
            {
                string candidate = WorldMap.ToZoneID(x, y, 0);
                if (!ChunkDensityPlan.IsEligible(a, candidate)) continue;
                Diag.ResetAll(); Diag.SetChannel("worldgen", true);
                a.GetZone(candidate); first = Records("DensityFeaturePlaced").Select(r => r.PayloadJson).ToList();
                if (first.Count > 0) id = candidate;   // a chunk already structured everywhere is not dressed
            }
            Assert.NotNull(id, "precondition: some real chunk is dressed");
            Diag.ResetAll(); Diag.SetChannel("worldgen", true);
            OverworldZoneManager.CreateDetached(factory, Seed, enableExploration: true).GetZone(id);
            var second = Records("DensityFeaturePlaced").Select(r => r.PayloadJson).ToList();
            Assert.IsNotEmpty(first, "precondition: the chunk was dressed");
            CollectionAssert.AreEqual(first, second);
        }

        /// <summary>The real SaltPan chunks of the world stay a duel floor: never dressed.</summary>
        [Test] public void Adversarial_RealSaltPansStayOpen()
        {
            var manager = OverworldZoneManager.CreateDetached(factory, Seed, enableExploration: true);
            int pans = 0, eligibleSparse = 0;
            for (int x = 0; x < WorldMap.Width; x++) for (int y = 0; y < WorldMap.Height; y++)
            {
                string id = WorldMap.ToZoneID(x, y, 0);
                if (WorldMapAuthoring.BiomeAt(x, y) != BiomeType.Beating || !WaysitePlan.IsEligible(manager, id)) continue;
                var formation = ChunkDensityPlan.FormationOf(BiomeType.Beating, id);
                if (formation == Formation.SaltPan) { pans++; Assert.False(ChunkDensityPlan.IsEligible(manager, id), id); }
                else if (ChunkDensityPlan.IsEligible(manager, id)) eligibleSparse++;
            }
            Assert.Greater(pans, 0, "precondition: the world has wild salt pans");
            Assert.Greater(eligibleSparse, 0, "counter-check: other Beating formations are dressed");
        }
    }
}
