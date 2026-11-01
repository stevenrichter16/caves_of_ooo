using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Adversarial sweep for waysites (Docs/CHUNK-ACTIVITY.md WA6, ADVERSARIAL_TESTING.md). Probes
    /// bug classes the per-feature tests cannot see: malformed palettes and zone IDs, tier and
    /// weight boundaries, rollback fidelity (interior, reservations, pre-existing state), route
    /// splits the cell count alone misses, determinism across generation order, and the shipped
    /// content's canon and integrity in really generated chunks.
    /// </summary>
    public sealed class WaysiteAdversarialTests
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

        static Zone Filled(System.Func<int, int, string> blueprintAt, string id = "Overworld.10.11.0")
        {
            var z = new Zone(id);
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++) z.AddEntity(factory.CreateEntity(blueprintAt(x, y)), x, y);
            return z;
        }
        static Zone Open() => Filled((x, y) => "Grass");
        /// <summary>Rock everywhere but one open column, edge to edge: any solid on it splits the zone in two.</summary>
        static Zone Column(int cx = 40) => Filled((x, y) => x == cx ? "Grass" : "SandstoneWall");

        static WaysiteKind Kind(string id, string[] rows, Dictionary<char, string> legend, params string[] notes)
            => new WaysiteKind { Id = id, Weight = 10, Notes = notes, Stamp = new StructureStamp { Name = id, Rows = rows, Legend = legend, Chance = 100 } };
        static readonly Dictionary<char, string> Post = new Dictionary<char, string> { { 'h', "HitchingPost" } };
        static readonly Dictionary<char, string> Roll = new Dictionary<char, string> { { 'b', "Bedroll" } };

        static List<Diag.Entry> Records(string kind) => DiagQuery.Apply(new DiagQuery.Filter { Category = "worldgen", Kind = kind, Limit = 500 }).Records.ToList();
        static int Count(Zone z, string bp) => z.GetAllEntities().Count(e => e.BlueprintName == bp);
        static List<string> Ids(IEnumerable<WaysiteKind> k) => k.Select(x => x.Id).ToList();

        // ════════════════ selection: malformed and boundary inputs ════════════════

        [Test] public void Adversarial_Order_NullOrEmptyPalette_IsEmpty()
        {
            CollectionAssert.IsEmpty(WaysitePlan.Order(Seed, "Overworld.4.4.0", null, 1));
            CollectionAssert.IsEmpty(WaysitePlan.Order(Seed, "Overworld.4.4.0", new WaysiteKind[0], 1));
            CollectionAssert.IsEmpty(WaysitePlan.Order(Seed, "Overworld.4.4.0", new WaysiteKind[] { null }, 1), "a null entry is skipped, not dereferenced");
        }

        [TestCase(null)] [TestCase("")] [TestCase("Cave.3.4.1")] [TestCase("Overworld.x.y.0")]
        [TestCase("Overworld.-3.4.0")] [TestCase("Overworld.999.999.0")] [TestCase("Overworld.4.4")]
        public void Adversarial_Order_MalformedZoneIds_StillOrderEveryKind(string zoneId)
        {
            var kinds = WaysiteCatalog.For(BiomeType.Spread);
            var order = WaysitePlan.Order(Seed, zoneId, kinds, 1);
            Assert.AreEqual(kinds.Count, order.Distinct().Count(), "no throw, no loss: an unplaceable ID still gets a full order");
        }

        [Test] public void Adversarial_Order_MinTierGatesAKind()
        {
            var low = new WaysiteKind { Id = "Low", Weight = 10, MinTier = 1, Stamp = new StructureStamp { Rows = new[] { "b" }, Legend = Roll } };
            var high = new WaysiteKind { Id = "High", Weight = 10, MinTier = 3, Stamp = new StructureStamp { Rows = new[] { "b" }, Legend = Roll } };
            var kinds = new[] { low, high };
            CollectionAssert.DoesNotContain(Ids(WaysitePlan.Order(Seed, "Overworld.5.5.0", kinds, 2)), "High", "tier 2 is below the floor");
            CollectionAssert.Contains(Ids(WaysitePlan.Order(Seed, "Overworld.5.5.0", kinds, 3)), "High", "counter-check: tier 3 admits it");
        }

        [Test] public void Adversarial_Order_ZeroOrNegativeWeight_IsNeverOffered()
        {
            var kinds = new[]
            {
                new WaysiteKind { Id = "Zero", Weight = 0, Stamp = new StructureStamp { Rows = new[] { "b" }, Legend = Roll } },
                new WaysiteKind { Id = "Negative", Weight = -5, Stamp = new StructureStamp { Rows = new[] { "b" }, Legend = Roll } },
                new WaysiteKind { Id = "Real", Weight = 1, Stamp = new StructureStamp { Rows = new[] { "b" }, Legend = Roll } },
            };
            for (int x = 0; x < 20; x++)
                CollectionAssert.AreEqual(new[] { "Real" }, Ids(WaysitePlan.Order(Seed, WorldMap.ToZoneID(x, 3, 0), kinds, 1)));
        }

        // ════════════════ selection: neighbour variety and determinism ════════════════

        [TestCase(BiomeType.Spread)] [TestCase(BiomeType.Sodden)] [TestCase(BiomeType.Beating)]
        public void Adversarial_FirstKinds_NeverRepeatTheWestNeighbour_AnywhereInTheWorld(BiomeType biome)
        {
            var kinds = WaysiteCatalog.For(biome);
            Assume.That(kinds.Count, Is.GreaterThanOrEqualTo(2));
            int westRepeats = 0, northRepeats = 0;
            for (int y = 0; y < WorldMap.Height; y++) for (int x = 0; x < WorldMap.Width; x++)
            {
                string here = WaysitePlan.Order(Seed, WorldMap.ToZoneID(x, y, 0), kinds, 1)[0].Id;
                if (x > 0 && here == WaysitePlan.Order(Seed, WorldMap.ToZoneID(x - 1, y, 0), kinds, 1)[0].Id) westRepeats++;
                if (y > 0 && here == WaysitePlan.Order(Seed, WorldMap.ToZoneID(x, y - 1, 0), kinds, 1)[0].Id) northRepeats++;
            }
            Assert.AreEqual(0, westRepeats, biome + ": walking east never meets the same lead site twice");
            if (kinds.Count >= 3) Assert.AreEqual(0, northRepeats, biome + ": nor walking south, with three kinds to choose from");
        }

        [Test] public void Adversarial_FirstKinds_IsStableAndSeedDependent()
        {
            var kinds = WaysiteCatalog.For(BiomeType.Spread);
            Assert.AreSame(WaysitePlan.FirstKinds(Seed, kinds, 1), WaysitePlan.FirstKinds(Seed, kinds, 1), "computed once per world and palette");
            Assert.AreSame(WaysiteCatalog.For(BiomeType.Spread), WaysiteCatalog.For(BiomeType.Spread), "the shipped palette is one instance, so the cache hits");
            var a = WaysitePlan.FirstKinds(Seed, kinds, 1); var b = WaysitePlan.FirstKinds(Seed + 1, kinds, 1);
            int differ = 0;
            for (int x = 0; x < WorldMap.Width; x++) for (int y = 0; y < WorldMap.Height; y++) if (a[x, y] != b[x, y]) differ++;
            Assert.Greater(differ, WorldMap.Width * WorldMap.Height / 4, "counter-check: another world draws another map");
        }

        [Test] public void Adversarial_GenerationOrderNeverChangesAZonesSites()
        {
            var ids = SpreadEligible(OverworldZoneManager.CreateDetached(factory, Seed, enableExploration: true), 3);
            Assume.That(ids.Count, Is.EqualTo(3));
            string Sites(OverworldZoneManager m, string id)
            {
                Diag.ResetAll(); Diag.SetChannel("worldgen", true);
                m.GetZone(id);
                return string.Join(";", Records("WaysitePlaced").Select(r => r.PayloadJson));
            }
            var forward = OverworldZoneManager.CreateDetached(factory, Seed, enableExploration: true);
            Sites(forward, ids[0]); Sites(forward, ids[1]);
            string afterOthers = Sites(forward, ids[2]);
            var alone = OverworldZoneManager.CreateDetached(factory, Seed, enableExploration: true);
            string first = Sites(alone, ids[2]);
            Assert.IsNotEmpty(first, "precondition: the zone has sites");
            Assert.AreEqual(first, afterOthers, "a zone's sites do not depend on which zones were visited first");
        }

        // ════════════════ builder boundaries ════════════════

        [Test] public void Adversarial_BuildZone_NullZoneOrFactory_DoesNotThrow()
        {
            var b = new WaysiteBuilder(BiomeType.Spread, 1, Seed);
            Assert.DoesNotThrow(() => b.BuildZone(null, factory, new System.Random(1)));
            Assert.DoesNotThrow(() => b.BuildZone(Open(), null, new System.Random(1)));
            Assert.DoesNotThrow(() => b.BuildZone(Open(), factory, null), "the builder never uses the pipeline RNG, so null is harmless");
        }

        [Test] public void Adversarial_ABudgetOfZeroOrTheOverwritPlacesNothing()
        {
            var zone = Open(); int before = zone.GetAllEntities().Count;
            new WaysiteBuilder(BiomeType.Spread, 1, Seed, budgetOverride: 0).BuildZone(zone, factory, new System.Random(1));
            new WaysiteBuilder(BiomeType.Overwrit, 1, Seed).BuildZone(zone, factory, new System.Random(1));
            Assert.AreEqual(before, zone.GetAllEntities().Count);
            CollectionAssert.IsEmpty(Records("WaysitePlaced"));
            new WaysiteBuilder(BiomeType.Spread, 1, Seed).BuildZone(zone, factory, new System.Random(1));
            Assert.Greater(zone.GetAllEntities().Count, before, "counter-check: the shipped Spread budget does place");
        }

        [Test] public void Adversarial_ABudgetBeyondThePalette_NeverRepeatsAKindInOneZone()
        {
            var zone = Open();
            var builder = new WaysiteBuilder(BiomeType.Spread, 1, Seed, new[] { Kind("Roll", new[] { "b" }, Roll) }, budgetOverride: 5);
            builder.BuildZone(zone, factory, new System.Random(1));
            CollectionAssert.AreEqual(new[] { "Roll" }, builder.Placed);
            Assert.AreEqual(1, Count(zone, "Bedroll"));
        }

        [Test] public void Adversarial_MalformedKinds_AreSkippedOrRefusedNeverThrown()
        {
            var zone = Open(); int before = zone.GetAllEntities().Count;
            var kinds = new[]
            {
                new WaysiteKind { Id = "NoStamp", Stamp = null },
                new WaysiteKind { Id = "NoRows", Stamp = new StructureStamp { Rows = new string[0], Legend = Roll } },
                new WaysiteKind { Id = "NoLegend", Stamp = new StructureStamp { Rows = new[] { "b" }, Legend = null } },
                new WaysiteKind { Id = "NoteButNullNotes", Notes = null, Stamp = new StructureStamp { Rows = new[] { "n" }, Legend = new Dictionary<char, string> { { 'n', WaysiteKind.NoteMarker } } } },
            };
            var builder = new WaysiteBuilder(BiomeType.Spread, 1, Seed, kinds, budgetOverride: 4);
            Assert.DoesNotThrow(() => builder.BuildZone(zone, factory, new System.Random(1)));
            CollectionAssert.IsEmpty(builder.Placed);
            Assert.AreEqual(before, zone.GetAllEntities().Count, "nothing partial");
        }

        [Test] public void Adversarial_AStampWiderThanTheZone_IsRefusedTooLarge()
        {
            var zone = Open();
            var kind = Kind("Wall", new[] { new string('h', Zone.Width) }, Post);
            new WaysiteBuilder(BiomeType.Spread, 1, Seed, new[] { kind }, budgetOverride: 1).BuildZone(zone, factory, new System.Random(1));
            Assert.AreEqual(0, Count(zone, "HitchingPost"));
            StringAssert.Contains("too-large", Records("WaysiteRefused").Single().PayloadJson);
        }

        // ════════════════ atomicity and rollback fidelity ════════════════

        [Test] public void Adversarial_SplittingARoadIntoTwoEdgeTouchingHalves_IsACutRoute()
        {
            // The post removes exactly one reachable cell, which its own footprint accounts for, so
            // the cell count alone passes it; the road is still in two pieces.
            var zone = Column();
            var (cells, parts) = WaysiteBuilder.Reach(zone);
            Assume.That(cells, Is.EqualTo(Zone.Height)); Assume.That(parts, Is.EqualTo(1));
            var builder = new WaysiteBuilder(BiomeType.Spread, 1, Seed, new[] { Kind("Post", new[] { "h" }, Post) }, budgetOverride: 1);
            builder.BuildZone(zone, factory, new System.Random(1));
            CollectionAssert.IsEmpty(builder.Placed);
            Assert.AreEqual(0, Count(zone, "HitchingPost"));
            Assert.AreEqual((Zone.Height, 1), WaysiteBuilder.Reach(zone), "the road is whole again");
            StringAssert.Contains("cuts-route", Records("WaysiteRefused").Single().PayloadJson);
        }

        [Test] public void Adversarial_EnclosingAPocket_IsACutRoute()
        {
            var zone = Open();
            var ring = Kind("Ring", new[] { "hhh", "h.h", "hhh" }, Post);
            var builder = new WaysiteBuilder(BiomeType.Spread, 1, Seed, new[] { ring }, budgetOverride: 1);
            builder.BuildZone(zone, factory, new System.Random(1));
            CollectionAssert.IsEmpty(builder.Placed, "a ring of posts walls off its middle cell");
            Assert.AreEqual(0, Count(zone, "HitchingPost"));
            var horseshoe = Kind("Horseshoe", new[] { "hhh", "h.h", "h.." }, Post);
            builder = new WaysiteBuilder(BiomeType.Spread, 1, Seed, new[] { horseshoe }, budgetOverride: 1);
            builder.BuildZone(zone, factory, new System.Random(1));
            CollectionAssert.AreEqual(new[] { "Horseshoe" }, builder.Placed, "counter-check: an open side keeps the middle reachable");
        }

        [Test] public void Adversarial_AnUndoneSite_RestoresInteriorAndReservationsExactly()
        {
            var zone = Column();
            var reservedBefore = new HashSet<(int, int)>(zone.GenReservedCells);
            var kind = Kind("Shelter", new[] { "i", "h", "i" }, new Dictionary<char, string> { { 'i', "interior" }, { 'h', "HitchingPost" } });
            new WaysiteBuilder(BiomeType.Spread, 1, Seed, new[] { kind }, budgetOverride: 1).BuildZone(zone, factory, new System.Random(1));
            Assert.AreEqual(0, Count(zone, "HitchingPost"));
            for (int y = 0; y < Zone.Height; y++) Assert.False(zone.GetCell(40, y).IsInterior, "row " + y + " was never interior and is not now");
            CollectionAssert.AreEquivalent(reservedBefore, zone.GenReservedCells);
        }

        [Test] public void Adversarial_AnUndoneSite_LeavesAlreadyInteriorCellsInterior()
        {
            var zone = Column();
            for (int y = 0; y < Zone.Height; y++) zone.GetCell(40, y).IsInterior = true;
            var kind = Kind("Shelter", new[] { "i", "h", "i" }, new Dictionary<char, string> { { 'i', "interior" }, { 'h', "HitchingPost" } });
            new WaysiteBuilder(BiomeType.Spread, 1, Seed, new[] { kind }, budgetOverride: 1).BuildZone(zone, factory, new System.Random(1));
            for (int y = 0; y < Zone.Height; y++) Assert.True(zone.GetCell(40, y).IsInterior, "undo only reverts what the site changed (row " + y + ")");
        }

        [Test] public void Adversarial_AnUndoneSite_LeavesTheGroundBeneathIt()
        {
            var zone = Column();
            int grass = Count(zone, "Grass");
            new WaysiteBuilder(BiomeType.Spread, 1, Seed, new[] { Kind("Post", new[] { "h" }, Post) }, budgetOverride: 1).BuildZone(zone, factory, new System.Random(1));
            Assert.AreEqual(grass, Count(zone, "Grass"), "undo removes the site's owners, never what stood there before");
        }

        [Test] public void Adversarial_ARefusedKind_FallsThroughToTheNext()
        {
            var zone = Column();
            var builder = new WaysiteBuilder(BiomeType.Spread, 1, Seed, new[] { Kind("Post", new[] { "h" }, Post), Kind("Roll", new[] { "b" }, Roll) }, budgetOverride: 2);
            builder.BuildZone(zone, factory, new System.Random(1));
            CollectionAssert.AreEqual(new[] { "Roll" }, builder.Placed, "the post cuts the road; the bedroll does not");
            Assert.AreEqual(1, Records("WaysiteRefused").Count(r => r.PayloadJson.Contains("Post") && r.PayloadJson.Contains("cuts-route")));
        }

        [Test] public void Adversarial_ASpawnTheTierFloorRefuses_UndoesTheWholeSite()
        {
            string zoneId = null;
            for (int x = 0; x < WorldMap.Width && zoneId == null; x++) for (int y = 0; y < WorldMap.Height && zoneId == null; y++)
                if (ZoneTierPolicy.TierFor(WorldMap.ToZoneID(x, y, 0)) >= 4) zoneId = WorldMap.ToZoneID(x, y, 0);
            Assume.That(zoneId, Is.Not.Null, "a high-tier zone exists");
            int tier = ZoneTierPolicy.TierFor(zoneId);
            string weak = new[] { "Rotling", "Snapjaw", "GiantRat", "Mudcrab", "Snail" }
                .FirstOrDefault(b => factory.Blueprints.ContainsKey(b) && !ZoneTierFloor.Admits(factory, b, tier));
            Assume.That(weak, Is.Not.Null, "a creature below the floor exists");
            var zone = Filled((x, y) => "Grass", zoneId);
            var kind = Kind("Guarded", new[] { "bc" }, new Dictionary<char, string> { { 'b', "Bedroll" }, { 'c', "spawn:" + weak } });
            var builder = new WaysiteBuilder(BiomeType.Spread, tier, Seed, new[] { kind }, budgetOverride: 1);
            builder.BuildZone(zone, factory, new System.Random(1));
            CollectionAssert.IsEmpty(builder.Placed, "a site missing an owner is not that site");
            Assert.AreEqual(0, Count(zone, "Bedroll"), "the bedroll that did place is taken back");
            StringAssert.Contains("incomplete", Records("WaysiteRefused").Last().PayloadJson);
        }

        [Test] public void Adversarial_AVegetationClearingStamp_NeverBuildsIntoATree()
        {
            // LandmarkBuilder's clearing stamps fell trees before building; a recorded (undoable)
            // placement does not, so a waysite must never accept a blocked anchor.
            var zone = Filled((x, y) => x % 4 == 0 && y % 3 == 0 ? "Grass" : "Tree");
            var kind = Kind("Camp", new[] { "fb" }, new Dictionary<char, string> { { 'f', "Campfire" }, { 'b', "Bedroll" } });
            kind.Stamp.ClearsVegetation = true;
            new WaysiteBuilder(BiomeType.Spread, 1, Seed, new[] { kind }, budgetOverride: 1).BuildZone(zone, factory, new System.Random(1));
            foreach (var e in zone.GetAllEntities().Where(e => e.BlueprintName == "Bedroll" || e.BlueprintName == "Campfire"))
                Assert.False(zone.GetEntityCell(e).Objects.Any(o => o.BlueprintName == "Tree"), e.BlueprintName + " stands inside a tree");
        }

        [Test] public void Adversarial_ASiteStandsOnlyOnOpenGround()
        {
            // Reeds, crop rows and hedge gaps are passable, so footprint passability alone let a trough
            // stand in the reeds and a berry bush on a crop row (WaysitePresentationTests, 2026-10-31).
            var zone = Filled((x, y) => "Grass");
            foreach (var c in zone.Cells)
                if (!(c.X >= 30 && c.X <= 59 && c.Y >= 8 && c.Y <= 16)) zone.AddEntity(factory.CreateEntity("Reeds"), c.X, c.Y);
            var kind = Kind("Camp", new[] { "f.b" }, new Dictionary<char, string> { { 'f', "Campfire" }, { 'b', "Bedroll" } });
            var builder = new WaysiteBuilder(BiomeType.Spread, 1, Seed, new[] { kind }, budgetOverride: 1);
            builder.BuildZone(zone, factory, new System.Random(1));
            CollectionAssert.AreEqual(new[] { "Camp" }, builder.Placed, "the open clearing takes the camp");
            foreach (var e in zone.GetAllEntities().Where(e => e.BlueprintName == "Campfire" || e.BlueprintName == "Bedroll"))
                CollectionAssert.AreEquivalent(new[] { "Grass", e.BlueprintName }, zone.GetEntityCell(e).Objects.Select(o => o.BlueprintName), "nothing beneath but the ground");
            var reedy = Filled((x, y) => "Grass");
            foreach (var c in reedy.Cells) reedy.AddEntity(factory.CreateEntity("Reeds"), c.X, c.Y);
            builder.BuildZone(reedy, factory, new System.Random(1));
            CollectionAssert.IsEmpty(builder.Placed, "counter-check: no open ground, no camp");
        }

        // ════════════════ notes ════════════════

        [Test] public void Adversarial_ANoteKindWhoseNotesAllAreMissing_IsRefused()
        {
            var zone = Open();
            var kind = Kind("Lost", new[] { "bn" }, new Dictionary<char, string> { { 'b', "Bedroll" }, { 'n', WaysiteKind.NoteMarker } }, "NoSuchNote");
            var builder = new WaysiteBuilder(BiomeType.Spread, 1, Seed, new[] { kind }, budgetOverride: 1);
            builder.BuildZone(zone, factory, new System.Random(1));
            CollectionAssert.IsEmpty(builder.Placed);
            Assert.AreEqual(0, Count(zone, "Bedroll"));
            StringAssert.Contains("unknown-blueprint", Records("WaysiteRefused").Single().PayloadJson);
        }

        [Test] public void Adversarial_ANoteKindUsesOnlyNotesThatExist()
        {
            for (int seed = 0; seed < 12; seed++)
            {
                var zone = Open();
                var kind = Kind("Half", new[] { "bn" }, new Dictionary<char, string> { { 'b', "Bedroll" }, { 'n', WaysiteKind.NoteMarker } }, "NoSuchNote", "WaysiteNoteDrover1");
                var builder = new WaysiteBuilder(BiomeType.Spread, 1, seed, new[] { kind }, budgetOverride: 1);
                builder.BuildZone(zone, factory, new System.Random(1));
                CollectionAssert.AreEqual(new[] { "Half" }, builder.Placed, "seed " + seed);
                Assert.AreEqual(1, Count(zone, "WaysiteNoteDrover1"), "seed " + seed);
            }
        }

        [Test] public void Adversarial_ResolvingANote_NeverRewritesTheShippedStamp()
        {
            var camp = WaysiteCatalog.For(BiomeType.Spread).Single(k => k.Id == "HedgeCamp");
            var before = new Dictionary<char, string>(camp.Stamp.Legend);
            for (int i = 0; i < 4; i++) new WaysiteBuilder(BiomeType.Spread, 1, Seed + i).BuildZone(Open(), factory, new System.Random(1));
            CollectionAssert.AreEquivalent(before, camp.Stamp.Legend, "the note marker survives for the next zone");
        }

        // ════════════════ diag ════════════════

        [Test] public void Adversarial_PlacementDoesNotDependOnTheDiagChannel()
        {
            var on = Open(); new WaysiteBuilder(BiomeType.Spread, 1, Seed).BuildZone(on, factory, new System.Random(1));
            Diag.ResetAll(); Diag.SetChannel("worldgen", false);   // ResetAll restores the default channels
            var off = Open(); new WaysiteBuilder(BiomeType.Spread, 1, Seed).BuildZone(off, factory, new System.Random(1));
            CollectionAssert.IsEmpty(Records("WaysitePlaced"), "a closed channel records nothing");
            CollectionAssert.AreEquivalent(on.GetAllEntities().Select(e => e.BlueprintName + "@" + on.GetEntityCell(e).X + "," + on.GetEntityCell(e).Y),
                off.GetAllEntities().Select(e => e.BlueprintName + "@" + off.GetEntityCell(e).X + "," + off.GetEntityCell(e).Y));
        }

        [Test] public void Adversarial_EveryRecordedRefusal_NamesAKnownReason()
        {
            var manager = OverworldZoneManager.CreateDetached(factory, Seed, enableExploration: true);
            foreach (var id in SpreadEligible(manager, 8).Concat(Eligible(manager, BiomeType.Sodden, 4)).Concat(Eligible(manager, BiomeType.Beating, 4))) manager.GetZone(id);
            var known = new[] { "unknown-blueprint", "too-large", "no-anchor", "incomplete", "cuts-route" };
            foreach (var r in Records("WaysiteRefused"))
                Assert.True(known.Any(k => r.PayloadJson.Contains("\"" + k + "\"")), r.PayloadJson);
            Assert.False(Records("WaysiteRefused").Any(r => r.PayloadJson.Contains("unknown-blueprint")), "the shipped palettes never name a missing blueprint");
            Assert.Greater(Records("WaysitePlaced").Count, 12, "counter-check: the scan placed sites");
        }

        // ════════════════ shipped content and canon ════════════════

        [Test] public void Adversarial_TheGrovelandsSitesLightNoFire()
        {
            // Canon: fire costs standing under the Choir's law.
            foreach (var k in WaysiteCatalog.For(BiomeType.Grovelands))
                CollectionAssert.DoesNotContain(k.Stamp.Legend.Values.ToList(), "Campfire", k.Id);
        }

        [Test] public void Adversarial_ShippedKindsAreUniqueWellFormedAndNeverClearVegetation()
        {
            var ids = new HashSet<string>();
            foreach (BiomeType biome in System.Enum.GetValues(typeof(BiomeType)))
                foreach (var k in WaysiteCatalog.For(biome))
                {
                    Assert.True(ids.Add(k.Id), "kind id " + k.Id + " is unique");
                    Assert.False(k.Stamp.ClearsVegetation, k.Id + " fells nothing");
                    Assert.Greater(k.Weight, 0, k.Id);
                    Assert.LessOrEqual(k.Stamp.Width, 5, k.Id + " is a small site"); Assert.LessOrEqual(k.Stamp.Height, 3, k.Id);
                    foreach (var row in k.Stamp.Rows)
                        foreach (char c in row)
                            if (c != '.') Assert.True(k.Stamp.Legend.ContainsKey(c), k.Id + ": '" + c + "' is in the legend");
                }
        }

        [Test] public void Adversarial_EveryWaysiteTableYieldsRealThings()
        {
            foreach (var table in new[] { "WaysiteCampSack", "WaysiteCartGoods", "WaysiteTokenDish", "WaysiteBothyBasket", "WaysiteFrogOilCache" })
            {
                Assert.NotNull(LootTableRegistry.Get(table), table);
                var rng = new System.Random(5); int yielded = 0;
                for (int i = 0; i < 40; i++)
                    foreach (var bp in LootTableRegistry.Roll(table, rng)) { Assert.True(factory.Blueprints.ContainsKey(bp), table + ": " + bp); yielded++; }
                Assert.Greater(yielded, 20, table + " is seldom empty");
            }
        }

        [Test] public void Adversarial_EveryWaysiteNote_IsAPortableReadableWithItsOwnDocument()
        {
            var docs = new HashSet<string>();
            foreach (BiomeType biome in System.Enum.GetValues(typeof(BiomeType)))
                foreach (var k in WaysiteCatalog.For(biome))
                    foreach (var note in k.Notes)
                    {
                        var e = factory.CreateEntity(note);
                        Assert.True(e.GetPart<PhysicsPart>().Takeable, note + " can be carried");
                        var doc = e.GetPart<ReadableDocumentPart>();
                        Assert.NotNull(doc, note);
                        Assert.True(docs.Add(doc.DocumentId), note + " has a document of its own");
                    }
        }

        // ════════════════ really generated chunks ════════════════

        [Test] public void Adversarial_RealSites_NeverStandOnRoadsStairsOrWater()
        {
            var manager = OverworldZoneManager.CreateDetached(factory, Seed, enableExploration: true);
            var ids = SpreadEligible(manager, 10).Concat(Eligible(manager, BiomeType.Sodden, 5)).Concat(Eligible(manager, BiomeType.Beating, 5)).ToList();
            var siteOwners = new HashSet<string> { "Bedroll", "HitchingPost", "DroversTrough", "OverturnedCart", "TokenDish", "BoatHookPost", "Cistern" };
            int checkedOwners = 0;
            foreach (var id in ids)
            {
                var zone = manager.GetZone(id);
                foreach (var e in zone.GetAllEntities())
                {
                    if (!siteOwners.Contains(e.BlueprintName) && !e.BlueprintName.StartsWith("WaysiteNote")) continue;
                    var cell = zone.GetEntityCell(e); if (cell == null) continue;
                    checkedOwners++;
                    foreach (var o in cell.Objects)
                    {
                        Assert.AreNotEqual("RoadStone", o.BlueprintName, id + ": " + e.BlueprintName + " on the road at " + cell.X + "," + cell.Y);
                        Assert.Null(o.GetPart<StairsDownPart>(), id + ": " + e.BlueprintName + " on stairs");
                        Assert.Null(o.GetPart<StairsUpPart>(), id + ": " + e.BlueprintName + " on stairs");
                        Assert.Null(o.GetPart<LiquidPoolPart>(), id + ": " + e.BlueprintName + " in water");
                    }
                }
            }
            Assert.Greater(checkedOwners, 20, "counter-check: the scan found sites to check");
        }

        [Test] public void Adversarial_TheOpeningAndPlacesGenerateWithoutSites()
        {
            var manager = OverworldZoneManager.CreateDetached(factory, Seed, enableExploration: true);
            var place = WorldMapAuthoring.Places.First();
            foreach (var id in new[] { "Overworld.11.9.0", "Overworld.12.10.0", WorldMap.ToZoneID(place.X, place.Y, 0) })
            {
                Diag.ResetAll(); Diag.SetChannel("worldgen", true);
                manager.GetZone(id);
                CollectionAssert.IsEmpty(Records("WaysitePlaced"), id + " keeps its authored content");
            }
        }

        [Test] public void Adversarial_ACachedZoneIsNeverRebuilt()
        {
            var manager = OverworldZoneManager.CreateDetached(factory, Seed, enableExploration: true);
            string id = SpreadEligible(manager, 1).Single();
            var zone = manager.GetZone(id);
            int placed = Records("WaysitePlaced").Count; Assume.That(placed, Is.GreaterThan(0));
            Assert.AreSame(zone, manager.GetZone(id));
            Assert.AreEqual(placed, Records("WaysitePlaced").Count, "a second visit adds nothing");
        }

        static List<string> SpreadEligible(OverworldZoneManager m, int n) => Eligible(m, BiomeType.Spread, n);
        static List<string> Eligible(OverworldZoneManager m, BiomeType biome, int n)
        {
            var ids = new List<string>();
            for (int x = 0; x < WorldMap.Width && ids.Count < n; x++) for (int y = 0; y < WorldMap.Height && ids.Count < n; y++)
            {
                string id = WorldMap.ToZoneID(x, y, 0);
                if (WorldMapAuthoring.BiomeAt(x, y) == biome && WaysitePlan.IsEligible(m, id)) ids.Add(id);
            }
            return ids;
        }
    }
}
