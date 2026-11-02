using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Presentation.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed partial class BiomeColorPatcherTravelTests
    {
        static readonly BiomePalette MorrowfastNeutral = new BiomePalette(BiomeType.Grovelands, Color.white, 8f, 0f, 0f);
        static void AssertMorrowfastGrade(Fixture f, bool pilot)
        {
            Call(f.Patcher, "Update"); Set(f.Patcher, "_fadeProgress", .999999f); Call(f.Patcher, "Update");
            Assert.True(Matches(f.Volume.profile, pilot ? MorrowfastNeutral : BiomePalette.Grovelands));
            AssertInitial(f.Shared);
        }
        static Zone AuthoredGroveZone(OverworldZoneManager manager, string id)
        {
            var (x, y, _) = WorldMap.FromZoneID(id);
            Assert.AreEqual(BiomeType.Grovelands, manager.WorldMap.Tiles[x,y], "Use canonical authored biome without overwriting it.");
            var zone = new Zone(id); manager.SetActiveZone(zone); return zone;
        }
        static Zone InstalledMorrowfast(OverworldZoneManager manager)
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            var zone = AuthoredGroveZone(manager, MorrowfastSceneRuntime.ZoneID);
            Assert.True(MorrowfastSceneRuntime.Install(zone, factory));
            Assert.True(CavesOfOoo.Rendering.FlatPixelSettlementGroundCatalog.IsSupportedZone(zone));
            return zone;
        }

        [Test]
        public void NativeMorrowfastGradeSeparatesMaterialsAndRestoresSameBiomeTravel()
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = InstalledMorrowfast(manager); renderer.SetZone(town); AssertMorrowfastGrade(f, true);
                renderer.SetZone(AuthoredGroveZone(manager, "Overworld.2.6.0")); AssertMorrowfastGrade(f, false);
                renderer.SetZone(town); AssertMorrowfastGrade(f, true);
            }
        }

        [TestCase("legacy")][TestCase("sprites-off")][TestCase("wrong-poi")][TestCase("wrong-name")]
        [TestCase("detached")][TestCase("replaced-graph")][TestCase("missing-scene")][TestCase("revision")]
        public void NativeMorrowfastGradeRequiresCurrentInstalledScene(string loss)
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); var env = EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = InstalledMorrowfast(manager); renderer.SetZone(town); AssertMorrowfastGrade(f, true);
                var poi = manager.WorldMap.POIs[3,6]; var name = poi.Name;
                var state = MorrowfastSceneRuntime.GetState(town); var owner = state.ParentEntity;
                if (loss == "legacy") renderer.FlatPixelModeEnabled = false;
                if (loss == "sprites-off") env.RenderingEnabled = false;
                if (loss == "wrong-poi") manager.WorldMap.POIs[3,6] = null;
                if (loss == "wrong-name") poi.Name = "another town";
                if (loss == "detached") manager.CachedZones.Remove(town.ZoneID);
                if (loss == "replaced-graph") manager.CachedZones[town.ZoneID] = new Zone(town.ZoneID);
                if (loss == "missing-scene") town.RemoveEntity(owner);
                if (loss == "revision") state.Revision = 2;
                AssertMorrowfastGrade(f, false);
                renderer.FlatPixelModeEnabled = true; env.RenderingEnabled = true;
                manager.WorldMap.POIs[3,6] = poi; poi.Name = name; manager.CachedZones[town.ZoneID] = town;
                if (loss == "missing-scene") Assert.True(town.AddEntity(owner, 0, 0));
                state.Revision = 1; AssertMorrowfastGrade(f, true);
            }
        }

        [Test]
        public void NativeMorrowfastGradeFollowsDisplayedReplacementAndSavedLairRefusal()
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var old = OverworldZoneManager.CreateDetached(null, 64);
                renderer.SetZone(InstalledMorrowfast(old)); AssertMorrowfastGrade(f, true);
                var replacement = OverworldZoneManager.CreateDetached(null, 65);
                var bare = AuthoredGroveZone(replacement, MorrowfastSceneRuntime.ZoneID);
                renderer.SetZone(bare); AssertMorrowfastGrade(f, false); Assert.AreSame(replacement, f.Patcher.ZoneManager);
                var town = InstalledMorrowfast(replacement); renderer.SetZone(town); AssertMorrowfastGrade(f, true);
                var poi = replacement.WorldMap.GetPOI(3,6);
                replacement.WorldMap.SetPOI(3,6, new PointOfInterest(POIType.Lair, "saved legacy", null, 1, "DesertProwler"));
                LairStacks.Restore(replacement, null); Assert.NotNull(LairStacks.Inspect(replacement, town.ZoneID));
                replacement.WorldMap.SetPOI(3,6, poi); AssertMorrowfastGrade(f, false);
            }
        }
    }
}
