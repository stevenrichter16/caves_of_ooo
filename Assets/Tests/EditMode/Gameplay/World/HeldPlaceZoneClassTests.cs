using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The held-place zone classifier and the waysite-eligibility extraction it needs (Docs/HOME-BASE-DESIGN.md sections 7.1, PB1).
    /// The extraction is a pure refactor of accepted generation code, pinned here by a seed sweep against a copy of the old function.
    /// </summary>
    public sealed class HeldPlaceZoneClassTests
    {
        HaulingContentScope scope;
        [SetUp] public void Setup() { scope = new HaulingContentScope(); scope.Seed(64); }
        [TearDown] public void Cleanup() { scope.Dispose(); }

        OverworldZoneManager Manager(int seed = 64) => OverworldZoneManager.CreateDetached(scope.Factory, seed, true);
        static IEnumerable<(int x, int y, string id)> Cells()
        {
            for (int x = 0; x < WorldMap.Width; x++) for (int y = 0; y < WorldMap.Height; y++) yield return (x, y, WorldMap.ToZoneID(x, y, 0));
        }

        /// <summary>The body of <c>WaysitePlan.IsEligible</c> exactly as it was before the extraction.</summary>
        static bool LegacyIsEligible(OverworldZoneManager manager, string zoneId, HashSet<string> opening)
        {
            if (manager == null || string.IsNullOrEmpty(zoneId) || !zoneId.StartsWith("Overworld.", StringComparison.Ordinal)) return false;
            var (x, y, z) = WorldMap.FromZoneID(zoneId);
            if (z != 0 || !WorldMapAuthoring.InBounds(x, y) || manager.WorldMap?.GetPOI(x, y) != null) return false;
            if (opening.Contains(zoneId) || zoneId == ReferenceGladePlan.ZoneID || zoneId == GleanersCellarBuilder.ZoneID) return false;
            if (zoneId == manager.Wayhouse?.ZoneID || zoneId == manager.RareEncounters?.PairZoneID || zoneId == manager.RareEncounters?.ViperZoneID) return false;
            if (zoneId == OverworldZoneManager.TenthFireZoneID || zoneId == OverworldZoneManager.WovenDollZoneID
                || zoneId == OverworldZoneManager.AbandonedCounterZoneA || zoneId == OverworldZoneManager.AbandonedCounterZoneB) return false;
            foreach (var d in RegionalSituations.Definitions)
                if (d.SourceZoneId == zoneId || d.RecipientZoneId == zoneId) return false;
            if (manager.Exploration != null && manager.Exploration.TryGetPlacement(manager, zoneId, out var entry)
                && entry.Family != SpreadExplorationFamily.None) return false;
            return true;
        }

        static readonly HashSet<string> Opening = new HashSet<string> { "Overworld.11.9.0", "Overworld.11.8.0", "Overworld.12.10.0", "Overworld.12.11.0" };

        [TestCase(64)] [TestCase(65)] [TestCase(704860946)]
        public void IsEligibleEqualsTheOldFunctionAndTheConjunctionOnEveryCell(int seed)
        {
            var manager = Manager(seed); int eligible = 0, family = 0, protectedCells = 0;
            foreach (var (x, y, id) in Cells())
            {
                bool old = LegacyIsEligible(manager, id, Opening);
                Assert.AreEqual(old, WaysitePlan.IsEligible(manager, id), id);
                Assert.AreEqual(old, WaysitePlan.IsEligibleIgnoringFamily(manager, id) && !WaysitePlan.IsFamilyOwned(manager, id), id + " conjunction");
                if (old) eligible++;
                if (WaysitePlan.IsEligibleIgnoringFamily(manager, id) && WaysitePlan.IsFamilyOwned(manager, id)) family++;
                if (!WaysitePlan.IsEligibleIgnoringFamily(manager, id)) protectedCells++;
            }
            Assert.Greater(eligible, 0, "the sweep saw eligible cells"); Assert.Greater(protectedCells, 0, "and excluded ones");
            TestContext.WriteLine($"seed {seed}: eligible {eligible}, family-owned {family}, excluded {protectedCells}");
        }

        [Test] public void FamilyOwnedAndNullArgumentsAreSafe()
        {
            Assert.False(WaysitePlan.IsFamilyOwned(null, "Overworld.5.5.0")); Assert.False(WaysitePlan.IsFamilyOwned(Manager(), null)); Assert.False(WaysitePlan.IsFamilyOwned(Manager(), ""));
            Assert.False(WaysitePlan.IsEligibleIgnoringFamily(null, "Overworld.5.5.0")); Assert.False(WaysitePlan.IsEligibleIgnoringFamily(Manager(), "garbage")); Assert.False(WaysitePlan.IsEligibleIgnoringFamily(Manager(), null));
            Assert.False(WaysitePlan.IsEligible(null, null));
        }

        [Test] public void EveryClassIsReachedByTheSweepAndNothingIsVacuous()
        {
            var counts = new Dictionary<HeldPlaceZoneClass, int>();
            foreach (int seed in new[] { 64, 65, 704860946 })
            {
                var manager = Manager(seed);
                foreach (var (x, y, id) in Cells())
                {
                    var c = HeldPlaceZones.Classify(manager, id); counts[c] = counts.TryGetValue(c, out int n) ? n + 1 : 1;
                }
            }
            foreach (var c in new[] { HeldPlaceZoneClass.PoiZone, HeldPlaceZoneClass.ProtectedGround, HeldPlaceZoneClass.DisallowedBiome, HeldPlaceZoneClass.FamilyOwned, HeldPlaceZoneClass.WildSurface })
                Assert.True(counts.ContainsKey(c) && counts[c] > 0, c + " is reached by the three-seed sweep");
            TestContext.WriteLine(string.Join(", ", counts.Select(p => p.Key + "=" + p.Value)));
        }

        [TestCase(64)] [TestCase(65)] [TestCase(704860946)]
        public void WildSurfaceMeansEligibleAllowedBiomeAndNotAPoi(int seed)
        {
            var manager = Manager(seed);
            foreach (var (x, y, id) in Cells())
            {
                var c = HeldPlaceZones.Classify(manager, id);
                if (c != HeldPlaceZoneClass.WildSurface) { Assert.NotNull(HeldPlaceZones.RefusalCode(c), id); continue; }
                Assert.Null(HeldPlaceZones.RefusalCode(c)); Assert.True(WaysitePlan.IsEligible(manager, id), id);
                Assert.Null(manager.WorldMap.GetPOI(x, y), id); CollectionAssert.Contains(HeldPlaceZones.AllowedBiomes, manager.WorldMap.GetBiome(x, y), id);
            }
        }

        [Test] public void NoAdmittedZoneIsEverGrovelandsOrOverwritAndThoseAreRefusedAsBiome()
        {
            foreach (int seed in new[] { 64, 65, 704860946 })
            {
                var manager = Manager(seed);
                foreach (var (x, y, id) in Cells())
                {
                    var biome = manager.WorldMap.GetBiome(x, y); var c = HeldPlaceZones.Classify(manager, id);
                    if (biome == BiomeType.Grovelands || biome == BiomeType.Overwrit) Assert.AreNotEqual(HeldPlaceZoneClass.WildSurface, c, id + " " + biome);
                    if (c == HeldPlaceZoneClass.DisallowedBiome) CollectionAssert.DoesNotContain(HeldPlaceZones.AllowedBiomes, biome, id);
                }
            }
        }

        [TestCase("Overworld.13.7.0", HeldPlaceZoneClass.PoiZone)]
        [TestCase("Overworld.7.8.0", HeldPlaceZoneClass.PoiZone)]
        [TestCase("Overworld.11.9.0", HeldPlaceZoneClass.ProtectedGround)]
        [TestCase("Overworld.12.10.0", HeldPlaceZoneClass.ProtectedGround)]
        public void NamedZonesClassifyAsDesigned(string id, HeldPlaceZoneClass expected)
            => Assert.AreEqual(expected, HeldPlaceZones.Classify(Manager(), id));

        [Test] public void AuthoredScenesAndThePilotAreProtected()
        {
            var manager = Manager();
            foreach (string id in OverworldZoneManager.AuthoredWildernessZoneIDs.Concat(new[] { MultiCellPilotRuntime.ZoneID }))
            {
                var c = HeldPlaceZones.Classify(manager, id);
                Assert.True(c == HeldPlaceZoneClass.ProtectedGround || c == HeldPlaceZoneClass.PoiZone, id + " is " + c);
                Assert.NotNull(HeldPlaceZones.RefusalCode(c));
            }
        }

        [TestCase("Overworld.13.7.1")] [TestCase("Overworld.5.5.3")]
        public void EveryUndergroundZoneIsAColumnAndIsRefusedUntilThePlacementAudit(string id)
        {
            var c = HeldPlaceZones.Classify(Manager(), id); Assert.AreEqual(HeldPlaceZoneClass.Column, c); Assert.AreEqual(PlacementCheck.ColumnPending, HeldPlaceZones.RefusalCode(c));
        }

        [TestCase("WorldMap")] [TestCase("fifty-world-tests")] [TestCase("")] [TestCase(null)] [TestCase("Overworld.99.99.0")] [TestCase("Overworld.5.5.-1")]
        public void NonOverworldOutOfMapAndNullIdsAreUnsupportedWithoutThrowing(string id)
        {
            Assert.AreEqual(HeldPlaceZoneClass.Unsupported, HeldPlaceZones.Classify(Manager(), id)); Assert.AreEqual(HeldPlaceZoneClass.Unsupported, HeldPlaceZones.Classify(null, "Overworld.5.5.0"));
            Assert.AreEqual(PlacementCheck.UnsupportedZone, HeldPlaceZones.RefusalCode(HeldPlaceZoneClass.Unsupported));
        }

        [Test] public void ClassificationIsAPureFunctionOfTheSeedAndNeverGeneratesAZone()
        {
            var manager = Manager(); int before = manager.CachedZones.Count;
            var first = Cells().Select(c => HeldPlaceZones.Classify(manager, c.id)).ToArray();
            var second = Cells().Select(c => HeldPlaceZones.Classify(manager, c.id)).ToArray();
            CollectionAssert.AreEqual(first, second); Assert.AreEqual(before, manager.CachedZones.Count, "classifying must not load or generate a zone");
        }
    }
}
