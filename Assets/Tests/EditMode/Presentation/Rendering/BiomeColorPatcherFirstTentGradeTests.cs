using CavesOfOoo.Core;
using CavesOfOoo.Presentation.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed partial class BiomeColorPatcherTravelTests
    {
        static readonly BiomePalette FirstTentNeutral = new BiomePalette(BiomeType.Beating, Color.white, 12f, 0f, 0f);

        static Zone AuthoredFirstTent(OverworldZoneManager manager)
        {
            Assert.AreEqual(BiomeType.Beating, manager.WorldMap.GetBiome(5, 17),
                "FirstTent is authored Beating; do not manufacture a different biome test premise.");
            var zone = new Zone(FirstTentCompositionPlan.ZoneID);
            manager.SetActiveZone(zone);
            Assert.True(CavesOfOoo.Rendering.FlatPixelSettlementGroundCatalog.IsSupportedZone(zone));
            return zone;
        }

        static void AssertFirstTentGrade(Fixture f, bool pilot, BiomeType biome = BiomeType.Beating)
        {
            Call(f.Patcher, "Update"); Set(f.Patcher, "_fadeProgress", .999999f); Call(f.Patcher, "Update");
            Assert.True(Matches(f.Volume.profile, pilot ? FirstTentNeutral : BiomePalette.GetForBiome(biome)));
            AssertInitial(f.Shared);
        }

        [Test]
        public void NativeFirstTentGradePreservesContrastAndRestoresSameBiomeTravel()
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredFirstTent(manager); renderer.SetZone(town); AssertFirstTentGrade(f, true);
                var outside = new Zone("Overworld.6.17.0"); manager.SetActiveZone(outside);
                Assert.AreEqual(BiomeType.Beating, manager.WorldMap.GetBiome(6, 17));
                renderer.SetZone(outside); AssertFirstTentGrade(f, false);
                renderer.SetZone(town); AssertFirstTentGrade(f, true);
            }
        }

        [TestCase("legacy")][TestCase("sprites-off")][TestCase("wrong-poi")]
        [TestCase("wrong-name")][TestCase("wrong-profile")][TestCase("detached")][TestCase("replaced-cache")]
        public void NativeFirstTentGradeLosesAndRecoversCurrentFirstTentAuthority(string loss)
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); var env = EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredFirstTent(manager); renderer.SetZone(town); AssertFirstTentGrade(f, true);
                var poi = manager.WorldMap.GetPOI(5, 17); var name = poi.Name; var profile = poi.Profile;
                if (loss == "legacy") renderer.FlatPixelModeEnabled = false;
                if (loss == "sprites-off") env.RenderingEnabled = false;
                if (loss == "wrong-poi") manager.WorldMap.POIs[5, 17] = null;
                if (loss == "wrong-name") poi.Name = "another town";
                if (loss == "wrong-profile") poi.Profile = "another profile";
                if (loss == "detached") manager.CachedZones.Remove(town.ZoneID);
                if (loss == "replaced-cache") manager.CachedZones[town.ZoneID] = new Zone(town.ZoneID);
                AssertFirstTentGrade(f, false);
                renderer.FlatPixelModeEnabled = true; env.RenderingEnabled = true;
                manager.WorldMap.POIs[5, 17] = poi; poi.Name = name; poi.Profile = profile;
                manager.CachedZones[town.ZoneID] = town; AssertFirstTentGrade(f, true);
            }
        }

        [Test]
        public void NativeFirstTentGradeFollowsDisplayedReplacementAndExcludesSavedLair()
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var old = OverworldZoneManager.CreateDetached(null, 64);
                renderer.SetZone(AuthoredFirstTent(old)); AssertFirstTentGrade(f, true);
                var replacement = OverworldZoneManager.CreateDetached(null, 65);
                var town = AuthoredFirstTent(replacement); renderer.SetZone(town);
                AssertFirstTentGrade(f, true); Assert.AreSame(replacement, f.Patcher.ZoneManager);
                var poi = replacement.WorldMap.GetPOI(5, 17);
                replacement.WorldMap.SetPOI(5, 17, new PointOfInterest(POIType.Lair, "saved legacy", null, 1, "DesertProwler"));
                LairStacks.Restore(replacement, null);
                Assert.NotNull(LairStacks.Inspect(replacement, town.ZoneID));
                replacement.WorldMap.SetPOI(5, 17, poi); AssertFirstTentGrade(f, false);
            }
        }

        [TestCase(BiomeType.Sodden)][TestCase(BiomeType.Spread)][TestCase(BiomeType.Grovelands)]
        public void NativeFirstTentGradeDoesNotReinterpretForeignSavedBiomes(BiomeType savedBiome)
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredFirstTent(manager); renderer.SetZone(town); AssertFirstTentGrade(f, true);
                manager.WorldMap.Tiles[5, 17] = savedBiome;
                AssertFirstTentGrade(f, false, savedBiome);
                Assert.AreEqual(savedBiome, manager.WorldMap.GetBiome(5, 17));
                manager.WorldMap.Tiles[5, 17] = BiomeType.Beating; AssertFirstTentGrade(f, true);
            }
        }
    }
}
