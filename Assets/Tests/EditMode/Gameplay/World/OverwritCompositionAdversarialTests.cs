using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Player-flow hypotheses: source rejection, native ownership,
    /// rare furniture, whole-pipeline traversal and accidental POI takeover.</summary>
    public class OverwritCompositionAdversarialTests
    {
        private static IEnumerable<string> Ids()
        {
            for (int y = 9; y <= 14; y++) for (int x = 0; x <= 4; x++)
            {
                string id = WorldMap.ToZoneID(x, y);
                if (OverwritCompositionPlan.IsWildernessZone(id)) yield return id;
            }
        }

        // H1: the permissive legacy parser accepts forms that must not expand
        // the finite new composition/presentation authority.
        [TestCase("overworld.1.10.0")] [TestCase("Overworld.+1.10.0")]
        [TestCase("Overworld.1.10.+0")] [TestCase("Overworld.1.10.00")]
        [TestCase("Overworld.2147483648.10.0")] [TestCase("Overworld.1.10.0 ")]
        public void Adversarial_NoncanonicalNamesHaveNoRoleOrFacing(string id)
        {
            Assert.IsFalse(OverwritCompositionPlan.IsWildernessZone(id));
            Assert.IsFalse(OverwritCompositionPlan.IsRimZone(id));
            Assert.AreEqual(0, OverwritCompositionPlan.InwardQuarterTurns(id));
            Assert.Throws<ArgumentException>(() => OverwritCompositionPlan.Create(id, int.MinValue));
        }

        // H2: renderer look-ahead/neighbor probes can cross a chunk boundary.
        [TestCase(-1, 0)] [TestCase(0, -1)] [TestCase(80, 24)]
        [TestCase(79, 25)] [TestCase(int.MinValue, int.MaxValue)]
        public void Adversarial_OutOfBoundsQueriesAreNeutral(int x, int y)
        {
            var p = OverwritCompositionPlan.Create("Overworld.4.11.0", int.MaxValue);
            Assert.IsNull(p.ObjectAt(x, y)); Assert.IsFalse(p.IsRim(x, y)); Assert.IsFalse(p.IsApproach(x, y));
            Assert.IsTrue(p.IsRim(79, 12));
        }

        // H3: consumed upstream RNG and extreme seeds must not change a plan's
        // identity or overflow cross-zone portal arithmetic.
        [TestCase(int.MinValue)] [TestCase(-1)] [TestCase(int.MaxValue)]
        public void Adversarial_GenerationIgnoresUnrelatedRngConsumption(int seed)
        {
            var f = GrovelandsCompositionTests.Factory(); var upstream = new Random(128);
            for (int i = 0; i < 200; i++) upstream.Next();
            var a = new Zone("Overworld.4.11.0"); var b = new Zone(a.ZoneID);
            var first = new OverwritCompositionBuilder(seed); var second = new OverwritCompositionBuilder(seed);
            Assert.IsTrue(first.BuildZone(a, f, upstream)); Assert.IsTrue(second.BuildZone(b, f, new Random(1)));
            Assert.AreEqual(first.Plan.Signature(), second.Plan.Signature());
            CollectionAssert.AreEquivalent(a.GenReservedCells, b.GenReservedCells);
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                CollectionAssert.AreEqual(a.GetCell(x, y).Objects.Select(e => e.BlueprintName), b.GetCell(x, y).Objects.Select(e => e.BlueprintName));
        }

        // H4: content loading fails softly. A missing family must not produce
        // a partially populated blank with misleading success state.
        [TestCase("OverwritGround")] [TestCase("OverwritNewGrowth")]
        public void Adversarial_MissingRequiredContentLeavesNoGroundOrReservations(string missing)
        {
            var f = GrovelandsCompositionTests.Factory(); f.Blueprints.Remove(missing);
            var z = new Zone("Overworld.1.10.0"); z.GenReservedCells.Add((7, 7));
            var b = new OverwritCompositionBuilder(64); int before = z.EntityVersion;
            Assert.IsFalse(b.BuildZone(z, f, new Random(1))); Assert.IsNull(b.Plan);
            Assert.AreEqual(before, z.EntityVersion); Assert.AreEqual(0, z.EntityCount);
            CollectionAssert.AreEquivalent(new[] { (7, 7) }, z.GenReservedCells);
        }

        // H10: a present blueprint can still fail softly: a missing Part or
        // typo-driven default gives the factory a non-null but unusable owner.
        // Reject the complete installation, including its reserved approaches.
        [TestCase("OverwritGround", "Render", null, null)]
        [TestCase("OverwritGround", "Physics", null, null)]
        [TestCase("OverwritNewGrowth", "Destructible", null, null)]
        [TestCase("OverwritNewGrowth", "Material", null, null)]
        [TestCase("OverwritNewGrowth", "Thermal", null, null)]
        [TestCase("OverwritPilgrimBench", "Physics", null, null)]
        [TestCase("OverwritWaymarker", "Render", null, null)]
        [TestCase("OverwritWaymarker", "Destructible", null, null)]
        [TestCase("OverwritGround", "Render", "Visible", "false")]
        [TestCase("OverwritGround", "Render", "RenderString", "?")]
        [TestCase("OverwritGround", "Render", "GlyphVariants", ".,'")]
        [TestCase("OverwritGround", "Physics", "Solid", "true")]
        [TestCase("OverwritGround", "Physics", "Takeable", "true")]
        [TestCase("OverwritNewGrowth", "Physics", "Solid", "true")]
        [TestCase("OverwritNewGrowth", "Physics", "Takeable", "true")]
        [TestCase("OverwritNewGrowth", "Destructible", "HP", "0")]
        [TestCase("OverwritNewGrowth", "Material", "MaterialTagsRaw", "")]
        [TestCase("OverwritNewGrowth", "Thermal", "HeatCapacity", "0")]
        [TestCase("OverwritWaymarker", "Physics", "Solid", "false")]
        [TestCase("OverwritWaymarker", "Destructible", "Indestructible", "true")]
        [TestCase("OverwritWaymarker", "Destructible", "WreckageBlueprint", "")]
        [TestCase("OverwritPilgrimBench", "Render", "Visible", "false")]
        [TestCase("OverwritPilgrimBench", "Material", "Combustibility", "0")]
        [TestCase("OverwritPilgrimBench", "Examinable", "Description", "")]
        public void Adversarial_MalformedNativeOwnerRejectsWithoutCommittingAnything(string bp, string part, string key, string value)
        {
            var f = GrovelandsCompositionTests.Factory();
            int seed = SeedContaining(bp);
            var b = new OverwritCompositionBuilder(seed);
            Assert.IsTrue(b.BuildZone(new Zone("Overworld.4.11.0"), f, new Random(1)), "Unmodified native content is the positive control.");
            if (key == null) Assert.IsTrue(f.Blueprints[bp].Parts.Remove(part));
            else f.Blueprints[bp].Parts[part][key] = value;
            Assert.NotNull(f.CreateEntity(bp), "This hypothesis concerns a soft failure hidden inside a real native owner.");
            var z = new Zone("Overworld.4.11.0"); z.GenReservedCells.Add((7, 7));
            int before = z.EntityVersion;
            Assert.IsFalse(b.BuildZone(z, f, new Random(1)), bp + ": " + part + "." + key);
            Assert.IsNull(b.Plan); Assert.AreEqual(0, z.EntityCount); Assert.AreEqual(before, z.EntityVersion);
            CollectionAssert.AreEquivalent(new[] { (7, 7) }, z.GenReservedCells);
        }

        // H11: flags and unsolicited Parts can bypass otherwise valid numeric
        // fields, turning the uniform floor into an obstacle or silent hazard.
        [TestCase("Solid")] [TestCase("LiquidPool")] [TestCase("TileStateSource")]
        public void Adversarial_UnexpectedGroundMechanicRejectsBeforePlacement(string corruption)
        {
            var f = GrovelandsCompositionTests.Factory();
            var control = new Zone("Overworld.1.10.0");
            Assert.IsTrue(new OverwritCompositionBuilder(64).BuildZone(control, f, new Random(1)));
            if (corruption == "Solid") f.Blueprints["OverwritGround"].Tags["Solid"] = "";
            else f.Blueprints["OverwritGround"].Parts[corruption] = new Dictionary<string, string>();
            var z = new Zone(control.ZoneID); z.GenReservedCells.Add((7, 7)); int before = z.EntityVersion;
            var b = new OverwritCompositionBuilder(64);
            Assert.IsFalse(b.BuildZone(z, f, new Random(1))); Assert.IsNull(b.Plan);
            Assert.AreEqual(0, z.EntityCount); Assert.AreEqual(before, z.EntityVersion);
            CollectionAssert.AreEquivalent(new[] { (7, 7) }, z.GenReservedCells);
        }

        private static int SeedContaining(string bp)
        {
            if (bp == "OverwritGround") return 64;
            for (int seed = 0; seed < 256; seed++)
            {
                var p = OverwritCompositionPlan.Create("Overworld.4.11.0", seed);
                for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                    if (p.ObjectAt(x, y) == bp) return seed;
            }
            Assert.Fail("No native placement found for the corruption positive control: " + bp);
            return -1;
        }

        [Test]
        public void Adversarial_FailedReuseDoesNotExposeAnOldPlanOrRepairAnExistingZone()
        {
            var f = GrovelandsCompositionTests.Factory(); var b = new OverwritCompositionBuilder(1729);
            var z = new Zone("Overworld.1.10.0"); Assert.IsTrue(b.BuildZone(z, f, new Random(1)));
            var ground = z.GetCell(20, 10).Objects.Single(e => e.BlueprintName == "OverwritGround");
            z.RemoveEntity(ground); int version = z.EntityVersion; int count = z.EntityCount;
            Assert.IsFalse(b.BuildZone(z, f, new Random(1))); Assert.IsNull(b.Plan);
            Assert.AreEqual(version, z.EntityVersion); Assert.AreEqual(count, z.EntityCount);
            Assert.IsFalse(z.GetCell(20, 10).Objects.Any(e => e.BlueprintName == "OverwritGround"));
            Assert.IsFalse(b.BuildZone(null, f, new Random(1)));
            Assert.IsFalse(b.BuildZone(new Zone("Overworld.1.10.0"), null, new Random(1)));
            Assert.IsFalse(b.BuildZone(new Zone("Overworld.1.10.0"), f, null));
        }

        // H5: runtime POIs are resolved before a static eligible-address query.
        [TestCase(POIType.Village)] [TestCase(POIType.MerchantCamp)]
        [TestCase(POIType.Lair)] [TestCase(POIType.RiverChunk)]
        public void Adversarial_RuntimePoiKeepsItsOwnPipeline(POIType kind)
        {
            var manager = new OverworldZoneManager(GrovelandsCompositionTests.Factory(), 64);
            Assert.IsTrue(OverwritCompositionTests.Pipeline(manager, "Overworld.4.11.0").Builders.Any(b => b is OverwritCompositionBuilder));
            manager.WorldMap.SetPOI(4, 11, new PointOfInterest(kind, "scope control", tier: 4));
            Assert.IsFalse(OverwritCompositionTests.Pipeline(manager, "Overworld.4.11.0").Builders.Any(b => b is OverwritCompositionBuilder));
            Assert.IsFalse(OverwritCompositionTests.Pipeline(manager, "Overworld.4.11.1").Builders.Any(b => b is OverwritCompositionBuilder));
        }

        // H6: destroying a cell-owned fixture must neither resurrect it nor
        // erase its floor, neighboring instance, or the native wreckage.
        [TestCase("OverwritNewGrowth", false)]
        [TestCase("OverwritWaymarker", true)]
        [TestCase("OverwritPilgrimBench", false)]
        public void Adversarial_DestructionKeepsNativeOwnersAndIsIdempotent(string blueprint, bool rubble)
        {
            var oldFactory = DestructionSystem.EntityFactoryRef;
            try
            {
                var f = GrovelandsCompositionTests.Factory(); DestructionSystem.EntityFactoryRef = f;
                var z = new Zone("Overworld.4.11.0");
                var floor = f.CreateEntity("OverwritGround"); var victim = f.CreateEntity(blueprint); var other = f.CreateEntity(blueprint);
                z.AddEntity(floor, 70, 8); z.AddEntity(victim, 70, 8); z.AddEntity(other, 71, 8);
                int hp = other.GetPart<DestructiblePart>().HP;
                Assert.AreEqual(DestroyVerdict.Destroyed, DestructionSystem.Damage(victim, 999, null, z));
                Assert.IsNull(z.GetEntityCell(victim)); Assert.NotNull(z.GetEntityCell(other)); Assert.NotNull(z.GetEntityCell(floor));
                Assert.AreEqual(hp, other.GetPart<DestructiblePart>().HP);
                Assert.AreEqual(rubble ? 1 : 0, z.GetCell(70, 8).Objects.Count(e => e.BlueprintName == "Rubble"));
                int count = z.EntityCount; DestructionSystem.Damage(victim, 999, null, z);
                Assert.AreEqual(count, z.EntityCount, "Repeated damage must not duplicate wreckage.");
            }
            finally { DestructionSystem.EntityFactoryRef = oldFactory; }
        }

        // H7: a technically passable edge can still strand another open pocket
        // or lose its blank identity to a later inherited scatter.
        [TestCase(2048)] [TestCase(64)] [TestCase(1729)]
        public void Adversarial_AllTwentyTwoNativeChunksRemainSparseAndTraversable(int seed)
        {
            var manager = new OverworldZoneManager(GrovelandsCompositionTests.Factory(), seed);
            int zones = 0;
            foreach (string id in Ids())
            {
                zones++; var z = manager.GetZone(id); var p = OverwritCompositionPlan.Create(id, seed);
                Assert.AreEqual(Zone.Width * Zone.Height, z.GetAllEntities().Count(e => e.BlueprintName == "OverwritGround"), id);
                Assert.IsTrue(FormationReachability.FullyReached(z, FormationReachability.FloodFromWest(z)), id);
                Assert.IsFalse(z.GetCell(0, p.WestY).BlocksMovement()); Assert.IsFalse(z.GetCell(79, p.EastY).BlocksMovement());
                Assert.IsFalse(z.GetCell(p.NorthX, 0).BlocksMovement()); Assert.IsFalse(z.GetCell(p.SouthX, 24).BlocksMovement());
                foreach (var e in z.GetAllEntities())
                {
                    Assert.IsFalse(e.HasTag("Creature"), id + ": inherited population returned");
                    Assert.IsFalse(e.HasPart<LiquidPoolPart>() || e.HasPart<TileStateSourcePart>(), id);
                    Assert.Contains(e.BlueprintName, new[] { "OverwritGround", "OverwritNewGrowth", "OverwritWaymarker", "OverwritPilgrimBench", "StairsDown" }, id);
                }
                Assert.LessOrEqual(z.EntityCount, Zone.Width * Zone.Height + 43, id);
            }
            Assert.AreEqual(22, zones);
        }

        // H8: rare benches can become effectively nonexistent or form an
        // inaccessible obstruction despite ordinary seeds looking empty.
        [Test]
        public void Adversarial_PilgrimageFurnitureIsRareAccessibleAndDoesNotInvadeTheBlank()
        {
            var f = GrovelandsCompositionTests.Factory(); int furnished = 0, benches = 0, markers = 0;
            for (int seed = 0; seed < 96; seed++)
            {
                var p = OverwritCompositionPlan.Create("Overworld.4.11.0", seed);
                var fixtures = new List<(int x, int y)>();
                for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                {
                    if (p.ObjectAt(x, y) == "OverwritPilgrimBench") { benches++; fixtures.Add((x, y)); }
                    if (p.ObjectAt(x, y) == "OverwritWaymarker") { markers++; fixtures.Add((x, y)); }
                }
                if (fixtures.Count == 0) continue;
                furnished++; Assert.LessOrEqual(fixtures.Count, 2);
                var z = new Zone(p.ZoneID); Assert.IsTrue(new OverwritCompositionBuilder(seed).BuildZone(z, f, new Random(1)));
                foreach (var at in fixtures)
                {
                    Assert.IsTrue(p.IsRim(at.x, at.y));
                    foreach (var d in new[] { (x: 1, y: 0), (x: -1, y: 0), (x: 0, y: 1), (x: 0, y: -1) })
                    {
                        Assert.IsFalse(z.GetCell(at.x + d.x, at.y + d.y).BlocksMovement());
                        Assert.IsTrue(z.GenReservedCells.Contains((at.x + d.x, at.y + d.y)));
                    }
                }
            }
            Assert.Greater(benches, 0); Assert.Greater(markers, 0);
            Assert.That(furnished, Is.InRange(1, 64), "Most sampled rim chunks should remain unfurnished.");
        }

        // H9: source circulation cannot merely consult a separate static catalog
        // while the retained actual pipeline has received an empty override.
        [Test]
        public void Adversarial_UnsayingPipelineCarriesTheExactLegacyCirculationCatalog()
        {
            var manager = new OverworldZoneManager(GrovelandsCompositionTests.Factory(), 64);
            var native = OverwritCompositionTests.Pipeline(manager, "Overworld.2.11.0").Builders.OfType<LandmarkBuilder>().Single();
            var catalog = (IReadOnlyList<StructureStamp>)typeof(LandmarkBuilder)
                .GetField("_catalog", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(native);
            CollectionAssert.AreEqual(StampCatalog.For(BiomeType.Ruins), catalog);
            foreach (string bp in new[] { "chest:LibraryShelfT1", "chest:WorkshopCacheT2", "spawn:PalimpsestEcho" })
                Assert.IsTrue(catalog.Any(s => s.Chance > 0 && s.MinTier <= 4 && s.Legend.Values.Contains(bp)), bp);
            Assert.IsFalse(OverwritCompositionPlan.IsWildernessZone("Overworld.2.11.0"));
        }
    }
}
