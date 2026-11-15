using CavesOfOoo.Core;
using CavesOfOoo.Presentation.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed partial class BiomeColorPatcherTravelTests
    {
        static readonly BiomePalette WellmeetNeutral = new BiomePalette(BiomeType.Beating, Color.white, 12f, 0f, 0f);

        static Zone AuthoredWellmeet(OverworldZoneManager manager)
        {
            Assert.AreEqual(BiomeType.Beating, manager.WorldMap.GetBiome(8, 16),
                "Wellmeet is authored Beating; do not manufacture a different biome test premise.");
            var zone = new Zone(WellmeetCompositionPlan.ZoneID);
            manager.SetActiveZone(zone);
            Assert.True(CavesOfOoo.Rendering.FlatPixelSettlementGroundCatalog.IsSupportedZone(zone));
            return zone;
        }

        static void AssertWellmeetGrade(Fixture f, bool pilot, BiomeType biome = BiomeType.Beating)
        {
            Call(f.Patcher, "Update"); Set(f.Patcher, "_fadeProgress", .999999f); Call(f.Patcher, "Update");
            Assert.True(Matches(f.Volume.profile, pilot ? WellmeetNeutral : BiomePalette.GetForBiome(biome)));
            AssertInitial(f.Shared);
        }

        [Test]
        public void NativeWellmeetGradePreservesContrastAndRestoresSameBiomeTravel()
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredWellmeet(manager); renderer.SetZone(town); AssertWellmeetGrade(f, true);
                var outside = new Zone("Overworld.9.16.0"); manager.SetActiveZone(outside);
                Assert.AreEqual(BiomeType.Beating, manager.WorldMap.GetBiome(9, 16));
                renderer.SetZone(outside); AssertWellmeetGrade(f, false);
                renderer.SetZone(town); AssertWellmeetGrade(f, true);
            }
        }

        [TestCase("legacy")][TestCase("sprites-off")][TestCase("wrong-poi")]
        [TestCase("wrong-name")][TestCase("wrong-profile")][TestCase("detached")][TestCase("replaced-cache")]
        public void NativeWellmeetGradeLosesAndRecoversCurrentTentCampAuthority(string loss)
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); var env = EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredWellmeet(manager); renderer.SetZone(town); AssertWellmeetGrade(f, true);
                var poi = manager.WorldMap.GetPOI(8, 16); var name = poi.Name; var profile = poi.Profile;
                if (loss == "legacy") renderer.FlatPixelModeEnabled = false;
                if (loss == "sprites-off") env.RenderingEnabled = false;
                if (loss == "wrong-poi") manager.WorldMap.POIs[8, 16] = null;
                if (loss == "wrong-name") poi.Name = "another town";
                if (loss == "wrong-profile") poi.Profile = "another profile";
                if (loss == "detached") manager.CachedZones.Remove(town.ZoneID);
                if (loss == "replaced-cache") manager.CachedZones[town.ZoneID] = new Zone(town.ZoneID);
                AssertWellmeetGrade(f, false);
                renderer.FlatPixelModeEnabled = true; env.RenderingEnabled = true;
                manager.WorldMap.POIs[8, 16] = poi; poi.Name = name; poi.Profile = profile;
                manager.CachedZones[town.ZoneID] = town; AssertWellmeetGrade(f, true);
            }
        }

        [Test]
        public void NativeWellmeetGradeFollowsDisplayedReplacementAndExcludesSavedLair()
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var old = OverworldZoneManager.CreateDetached(null, 64);
                renderer.SetZone(AuthoredWellmeet(old)); AssertWellmeetGrade(f, true);
                var replacement = OverworldZoneManager.CreateDetached(null, 65);
                var town = AuthoredWellmeet(replacement); renderer.SetZone(town);
                AssertWellmeetGrade(f, true); Assert.AreSame(replacement, f.Patcher.ZoneManager);
                var poi = replacement.WorldMap.GetPOI(8, 16);
                replacement.WorldMap.SetPOI(8, 16, new PointOfInterest(POIType.Lair, "saved legacy", null, 1, "DesertProwler"));
                LairStacks.Restore(replacement, null);
                Assert.NotNull(LairStacks.Inspect(replacement, town.ZoneID));
                replacement.WorldMap.SetPOI(8, 16, poi); AssertWellmeetGrade(f, false);
            }
        }

        [TestCase(BiomeType.Sodden)][TestCase(BiomeType.Spread)][TestCase(BiomeType.Grovelands)]
        public void NativeWellmeetGradeDoesNotReinterpretForeignSavedBiomes(BiomeType savedBiome)
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredWellmeet(manager); renderer.SetZone(town); AssertWellmeetGrade(f, true);
                manager.WorldMap.Tiles[8, 16] = savedBiome;
                AssertWellmeetGrade(f, false, savedBiome);
                Assert.AreEqual(savedBiome, manager.WorldMap.GetBiome(8, 16));
                manager.WorldMap.Tiles[8, 16] = BiomeType.Beating; AssertWellmeetGrade(f, true);
            }
        }
    }
}
