using CavesOfOoo.Core;
using CavesOfOoo.Presentation.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed partial class BiomeColorPatcherTravelTests
    {
        static readonly BiomePalette TineNeutral = new BiomePalette(BiomeType.Spread, Color.white, 5f, 0f, 0f);

        static Zone AuthoredTine(OverworldZoneManager manager)
        {
            Assert.AreEqual(BiomeType.Spread, manager.WorldMap.GetBiome(13, 7),
                "Tine is authored Spread; do not manufacture a different biome test premise.");
            var zone = new Zone(TineCompositionPlan.ZoneID);
            manager.SetActiveZone(zone);
            Assert.True(CavesOfOoo.Rendering.FlatPixelSettlementGroundCatalog.IsSupportedZone(zone));
            return zone;
        }

        static void AssertTineGrade(Fixture f, bool pilot, BiomeType biome = BiomeType.Spread)
        {
            Call(f.Patcher, "Update"); Set(f.Patcher, "_fadeProgress", .999999f); Call(f.Patcher, "Update");
            Assert.True(Matches(f.Volume.profile, pilot ? TineNeutral : BiomePalette.GetForBiome(biome)));
            AssertInitial(f.Shared);
        }

        [Test]
        public void NativeTineGradePreservesContrastAndRestoresSameBiomeTravel()
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredTine(manager); renderer.SetZone(town); AssertTineGrade(f, true);
                var outside = new Zone("Overworld.12.7.0"); manager.SetActiveZone(outside);
                Assert.AreEqual(BiomeType.Spread, manager.WorldMap.GetBiome(12, 7));
                renderer.SetZone(outside); AssertTineGrade(f, false);
                renderer.SetZone(town); AssertTineGrade(f, true);
            }
        }

        [TestCase("legacy")][TestCase("sprites-off")][TestCase("wrong-poi")]
        [TestCase("wrong-name")][TestCase("wrong-profile")][TestCase("wrong-type")]
        [TestCase("empty-profile")][TestCase("null-profile")][TestCase("null-pois")][TestCase("short-pois")][TestCase("detached")][TestCase("replaced-cache")]
        public void NativeTineGradeLosesAndRecoversCurrentLakesideVillageAuthority(string loss)
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); var env = EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredTine(manager); renderer.SetZone(town); AssertTineGrade(f, true);
                var poi = manager.WorldMap.GetPOI(13, 7); var name = poi.Name; var profile = poi.Profile; var type = poi.Type; var pois = manager.WorldMap.POIs;
                if (loss == "legacy") renderer.FlatPixelModeEnabled = false;
                if (loss == "sprites-off") env.RenderingEnabled = false;
                if (loss == "wrong-poi") manager.WorldMap.POIs[13, 7] = null;
                if (loss == "wrong-name") poi.Name = "another town";
                if (loss == "wrong-profile") poi.Profile = "another profile";
                if (loss == "wrong-type") poi.Type = POIType.Lair;
                if (loss == "empty-profile") poi.Profile = "";
                if (loss == "null-profile") poi.Profile = null;
                if (loss == "null-pois") manager.WorldMap.POIs = null;
                if (loss == "short-pois") manager.WorldMap.POIs = new PointOfInterest[1, 1];
                if (loss == "detached") manager.CachedZones.Remove(town.ZoneID);
                if (loss == "replaced-cache") manager.CachedZones[town.ZoneID] = new Zone(town.ZoneID);
                AssertTineGrade(f, false);
                renderer.FlatPixelModeEnabled = true; env.RenderingEnabled = true;
                manager.WorldMap.POIs = pois; poi.Type = type;
                manager.WorldMap.POIs[13, 7] = poi; poi.Name = name; poi.Profile = profile;
                manager.CachedZones[town.ZoneID] = town; AssertTineGrade(f, true);
            }
        }

        [Test]
        public void NativeTineGradeFollowsDisplayedReplacementAndExcludesSavedLair()
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var old = OverworldZoneManager.CreateDetached(null, 64);
                renderer.SetZone(AuthoredTine(old)); AssertTineGrade(f, true);
                var replacement = OverworldZoneManager.CreateDetached(null, 65);
                var town = AuthoredTine(replacement); renderer.SetZone(town);
                AssertTineGrade(f, true); Assert.AreSame(replacement, f.Patcher.ZoneManager);
                var poi = replacement.WorldMap.GetPOI(13, 7);
                replacement.WorldMap.SetPOI(13, 7, new PointOfInterest(POIType.Lair, "saved legacy", null, 1, "DesertProwler"));
                LairStacks.Restore(replacement, null);
                Assert.NotNull(LairStacks.Inspect(replacement, town.ZoneID));
                replacement.WorldMap.SetPOI(13, 7, poi); AssertTineGrade(f, false);
            }
        }

        [TestCase(BiomeType.Sodden)][TestCase(BiomeType.Beating)][TestCase(BiomeType.Grovelands)]
        public void NativeTineGradeDoesNotReinterpretForeignSavedBiomes(BiomeType savedBiome)
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredTine(manager); renderer.SetZone(town); AssertTineGrade(f, true);
                manager.WorldMap.Tiles[13, 7] = savedBiome;
                AssertTineGrade(f, false, savedBiome);
                Assert.AreEqual(savedBiome, manager.WorldMap.GetBiome(13, 7));
                manager.WorldMap.Tiles[13, 7] = BiomeType.Spread; AssertTineGrade(f, true);
            }
        }
        [TestCase("Overworld.013.7.0", BiomeType.Spread)]
        [TestCase("Overworld.13.7.0.extra", BiomeType.Cave)]
        [TestCase("Overworld.13.7.1", BiomeType.Cave)]
        public void NativeTineGradeRejectsAliasAndDepth(string zoneId, BiomeType biome)
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var zone = new Zone(zoneId); manager.SetActiveZone(zone); renderer.SetZone(zone);
                AssertTineGrade(f, false, biome);
            }
        }

        [Test]
        public void NativeTineGradeRejectsProfileAtAnotherAuthoredTown()
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                Assert.AreEqual(BiomeType.Spread, manager.WorldMap.GetBiome(14, 9));
                manager.WorldMap.GetPOI(14, 9).Profile = TineCompositionPlan.ProfileID;
                var zone = new Zone("Overworld.14.9.0"); manager.SetActiveZone(zone); renderer.SetZone(zone);
                AssertTineGrade(f, false);
            }
        }
    }
}
