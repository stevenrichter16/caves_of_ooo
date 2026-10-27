using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Presentation.Rendering;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    // Scope gate only. Native candidate selection and actual visual parity have
    // separate acceptance gates; these tests neither render nor write saves.
    public sealed class FlatPixelShaleParityTests
    {
        const string Target = "Overworld.16.13.6";
        const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
        Type study;
        FieldInfo enabled;
        bool oldEnabled, rootChanged;
        string oldRoot, isolatedRoot;
        OverworldZoneManager manager;
        Zone zone;

        [SetUp]
        public void Setup()
        {
            rootChanged = false;
            isolatedRoot = null;
            enabled = null;
            // Changing the public save-root property flushes pending writes.
            // A gate test must never cause a background save to be committed.
            Assert.False(SaveGameService.HasPendingSave, "Native runner must begin with no pending save.");
            oldRoot = SaveGameService.SaveRootOverride;
            study = typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelShaleParityStudy");
            enabled = study?.GetField("Enabled", PublicStatic);
            oldEnabled = enabled != null && enabled.FieldType == typeof(bool) && (bool)enabled.GetValue(null);
            isolatedRoot = Path.Combine(Path.GetTempPath(), "coo-shale-parity-gate-" + Guid.NewGuid().ToString("N"));
            Assert.False(Directory.Exists(isolatedRoot));
            SaveGameService.SaveRootOverride = isolatedRoot;
            rootChanged = true;
            manager = OverworldZoneManager.CreateDetached(new EntityFactory(), 65, true);
            Assert.IsNull(manager.WorldMap.GetPOI(16, 13), "Ordinary target-column precondition.");
            zone = Attach(Target);
            Assert.True(FlatPixelShaleCavesCatalog.IsSupportedZone(zone));
        }

        [TearDown]
        public void Cleanup()
        {
            if (enabled != null && enabled.FieldType == typeof(bool)) enabled.SetValue(null, oldEnabled);
            if (rootChanged) SaveGameService.SaveRootOverride = oldRoot;
            if (isolatedRoot != null)
                Assert.False(Directory.Exists(isolatedRoot), "A presentation query must not create save storage.");
        }

        Zone Attach(string id)
        {
            var graph = new Zone(id);
            manager.SetActiveZone(graph);
            return graph;
        }

        void SetEnabled(bool value)
        {
            Assert.NotNull(study, "Missing explicitly opt-in shale parity study gate.");
            Assert.NotNull(enabled, "Study must expose public static bool Enabled.");
            Assert.AreEqual(typeof(bool), enabled.FieldType);
            Assert.False(enabled.IsInitOnly);
            enabled.SetValue(null, value);
        }

        bool Active(Zone graph, bool flatMode = true)
        {
            Assert.NotNull(study, "Missing explicitly opt-in shale parity study gate.");
            var method = study.GetMethod("IsActive", PublicStatic, null, new[] { typeof(Zone), typeof(bool) }, null);
            Assert.NotNull(method, "Expected IsActive(Zone, bool).");
            Assert.AreEqual(typeof(bool), method.ReturnType);
            return (bool)method.Invoke(null, new object[] { graph, flatMode });
        }

        void Positive()
        {
            SetEnabled(true);
            Assert.True(FlatPixelShaleCavesCatalog.IsSupportedZone(zone));
            Assert.True(Active(zone), "Exact current isolated flat-world target must activate.");
        }

        [Test]
        public void FreshStudyIsDisabledEvenInsideOtherwiseEligibleIsolatedWorld()
        {
            Assert.NotNull(study, "Missing explicitly opt-in shale parity study gate.");
            Assert.NotNull(enabled);
            Assert.False(oldEnabled, "The untouched runtime default must be disabled; setup never resets it.");
            Assert.False(Active(zone));
            Positive();
        }

        [Test]
        public void EnabledAndFlatModeAreBothLiveRequirements()
        {
            Positive();
            SetEnabled(false);
            Assert.False(Active(zone));
            SetEnabled(true);
            Assert.False(Active(zone, false));
            Assert.True(Active(zone));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("\t\r\n")]
        public void NormalOrBlankSaveRootRevokesAnOtherwiseActiveStudy(string root)
        {
            Positive();
            SaveGameService.SaveRootOverride = root;
            Assert.False(Active(zone));
            SaveGameService.SaveRootOverride = isolatedRoot;
            Assert.True(Active(zone));
        }

        [TestCase("Overworld.16.13.5")]
        [TestCase("Overworld.16.12.6")]
        [TestCase("Overworld.0.9.6")]
        [TestCase("Overworld.016.13.6")]
        public void OtherDepthColumnAndNoncanonicalAliasNeverActivate(string id)
        {
            Positive();
            var other = Attach(id);
            Assert.False(Active(other));
            manager.SetActiveZone(zone);
            Assert.True(Active(zone));
        }

        [Test]
        public void NullOrUnattachedGraphCannotBorrowTheTargetAddress()
        {
            Positive();
            Assert.False(Active(null));
            var unattached = new Zone(Target);
            Assert.IsNull(WorldLocationContext.For(unattached));
            Assert.False(Active(unattached));
            Assert.True(Active(zone));
        }

        [Test]
        public void SameAddressReplacementRevokesOldGraphAndAdmitsOnlyCurrentGraph()
        {
            Positive();
            var replacement = Attach(Target);
            Assert.False(FlatPixelShaleCavesCatalog.IsSupportedZone(zone));
            Assert.False(Active(zone));
            Assert.True(Active(replacement));
            manager.SetActiveZone(zone);
            Assert.True(Active(zone));
            Assert.False(Active(replacement));
        }

        [TestCase("poi")]
        [TestCase("tiles-shape")]
        [TestCase("pois-shape")]
        public void CurrentShaleAuthorityStillRejectsAuthoredOrMalformedWorld(string fault)
        {
            Positive();
            var map = manager.WorldMap;
            var tiles = map.Tiles;
            var pois = map.POIs;
            try
            {
                if (fault == "poi") map.POIs[16, 13] = new PointOfInterest(POIType.MerchantCamp, "authored target");
                if (fault == "tiles-shape") map.Tiles = new BiomeType[1, 1];
                if (fault == "pois-shape") map.POIs = new PointOfInterest[1, 1];
                Assert.False(FlatPixelShaleCavesCatalog.IsSupportedZone(zone));
                Assert.False(Active(zone));
            }
            finally { map.Tiles = tiles; map.POIs = pois; pois[16, 13] = null; }
            Assert.True(Active(zone));
        }

        [Test]
        public void RetainedSurfaceLairClaimRejectsTargetAfterItsPoiLabelIsRemoved()
        {
            Positive();
            manager.WorldMap.SetPOI(16, 13, new PointOfInterest(POIType.Lair, "retained claim", null, 1, "DesertProwler"));
            LairStacks.Restore(manager, null);
            Assert.NotNull(LairStacks.Inspect(manager, "Overworld.16.13.0"));
            manager.WorldMap.SetPOI(16, 13, null);
            Assert.False(FlatPixelShaleCavesCatalog.IsSupportedZone(zone));
            Assert.False(Active(zone));
        }

        [TestCase(65)]
        [TestCase(777)]
        public void EligibilityUsesCurrentGraphAndColumnRatherThanASeedWhitelist(int seed)
        {
            SetEnabled(true);
            manager = OverworldZoneManager.CreateDetached(new EntityFactory(), seed, true);
            manager.WorldMap.SetPOI(16, 13, null);
            zone = Attach(Target);
            Assert.True(FlatPixelShaleCavesCatalog.IsSupportedZone(zone));
            Assert.True(Active(zone));
        }

        [Test]
        public void RepeatedScopeQueriesDoNotGenerateZonesRevealCellsOrChangeGlobalCavePalette()
        {
            Positive();
            var palette = BiomePalette.Cave;
            var cell = zone.GetCell(10, 10);
            bool explored = cell.Explored, visible = cell.IsVisible;
            int count = manager.CachedZoneCount, version = zone.EntityVersion;
            string state = zone.TileState.ToSaveString();
            var ledgers = (ConditionalWeakTable<OverworldZoneManager, LairStackLedgerPart>)typeof(LairStacks)
                .GetField("Ledgers", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            Assert.False(ledgers.TryGetValue(manager, out _));
            for (int i = 0; i < 5; i++) Assert.True(Active(zone));
            Assert.AreEqual(count, manager.CachedZoneCount);
            Assert.AreEqual(version, zone.EntityVersion);
            Assert.AreEqual(state, zone.TileState.ToSaveString());
            Assert.AreEqual(explored, cell.Explored);
            Assert.AreEqual(visible, cell.IsVisible);
            Assert.False(ledgers.TryGetValue(manager, out _));
            Assert.AreEqual(palette.ColorFilter, BiomePalette.Cave.ColorFilter);
            Assert.AreEqual(palette.Contrast, BiomePalette.Cave.Contrast);
            Assert.AreEqual(palette.Saturation, BiomePalette.Cave.Saturation);
            Assert.AreEqual(palette.VignetteIntensity, BiomePalette.Cave.VignetteIntensity);
            Assert.AreEqual(isolatedRoot, SaveGameService.SaveRootOverride);
        }
    }
}
