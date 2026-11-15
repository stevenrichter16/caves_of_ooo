using CavesOfOoo.Core;
using CavesOfOoo.Presentation.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed partial class BiomeColorPatcherTravelTests
    {
        static readonly BiomePalette SaltVaultNeutral = new BiomePalette(BiomeType.Beating, Color.white, 12f, 0f, 0f);

        static Zone AuthoredSaltVault(OverworldZoneManager manager)
        {
            Assert.AreEqual(BiomeType.Beating, manager.WorldMap.GetBiome(15, 15),
                "SaltVault is authored Beating; do not manufacture a different biome test premise.");
            var zone = new Zone(StillleafSaltVault.ZoneId);
            manager.SetActiveZone(zone);
            Assert.True(CavesOfOoo.Rendering.FlatPixelSettlementGroundCatalog.IsSupportedZone(zone));
            return zone;
        }

        static void AssertSaltVaultGrade(Fixture f, bool pilot, BiomeType biome = BiomeType.Beating)
        {
            Call(f.Patcher, "Update"); Set(f.Patcher, "_fadeProgress", .999999f); Call(f.Patcher, "Update");
            Assert.True(Matches(f.Volume.profile, pilot ? SaltVaultNeutral : BiomePalette.GetForBiome(biome)));
            AssertInitial(f.Shared);
        }

        [Test]
        public void NativeSaltVaultGradePreservesContrastAndRestoresSameBiomeTravel()
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredSaltVault(manager); renderer.SetZone(town); AssertSaltVaultGrade(f, true);
                var outside = new Zone("Overworld.16.15.0"); manager.SetActiveZone(outside);
                Assert.AreEqual(BiomeType.Beating, manager.WorldMap.GetBiome(16, 15));
                renderer.SetZone(outside); AssertSaltVaultGrade(f, false);
                renderer.SetZone(town); AssertSaltVaultGrade(f, true);
            }
        }

        [TestCase("legacy")][TestCase("sprites-off")][TestCase("wrong-poi")]
        [TestCase("wrong-name")][TestCase("wrong-profile")][TestCase("detached")][TestCase("replaced-cache")]
        public void NativeSaltVaultGradeLosesAndRecoversCurrentSaltVaultAuthority(string loss)
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); var env = EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredSaltVault(manager); renderer.SetZone(town); AssertSaltVaultGrade(f, true);
                var poi = manager.WorldMap.GetPOI(15, 15); var name = poi.Name; var profile = poi.Profile;
                if (loss == "legacy") renderer.FlatPixelModeEnabled = false;
                if (loss == "sprites-off") env.RenderingEnabled = false;
                if (loss == "wrong-poi") manager.WorldMap.POIs[15, 15] = null;
                if (loss == "wrong-name") poi.Name = "another town";
                if (loss == "wrong-profile") poi.Profile = "another profile";
                if (loss == "detached") manager.CachedZones.Remove(town.ZoneID);
                if (loss == "replaced-cache") manager.CachedZones[town.ZoneID] = new Zone(town.ZoneID);
                AssertSaltVaultGrade(f, false);
                renderer.FlatPixelModeEnabled = true; env.RenderingEnabled = true;
                manager.WorldMap.POIs[15, 15] = poi; poi.Name = name; poi.Profile = profile;
                manager.CachedZones[town.ZoneID] = town; AssertSaltVaultGrade(f, true);
            }
        }

        [Test]
        public void NativeSaltVaultGradeFollowsDisplayedReplacementAndExcludesSavedLair()
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var old = OverworldZoneManager.CreateDetached(null, 64);
                renderer.SetZone(AuthoredSaltVault(old)); AssertSaltVaultGrade(f, true);
                var replacement = OverworldZoneManager.CreateDetached(null, 65);
                var town = AuthoredSaltVault(replacement); renderer.SetZone(town);
                AssertSaltVaultGrade(f, true); Assert.AreSame(replacement, f.Patcher.ZoneManager);
                var poi = replacement.WorldMap.GetPOI(15, 15);
                replacement.WorldMap.SetPOI(15, 15, new PointOfInterest(POIType.Lair, "saved legacy", null, 1, "DesertProwler"));
                LairStacks.Restore(replacement, null);
                Assert.NotNull(LairStacks.Inspect(replacement, town.ZoneID));
                replacement.WorldMap.SetPOI(15, 15, poi); AssertSaltVaultGrade(f, false);
            }
        }

        [TestCase(BiomeType.Sodden)][TestCase(BiomeType.Spread)][TestCase(BiomeType.Grovelands)]
        public void NativeSaltVaultGradeDoesNotReinterpretForeignSavedBiomes(BiomeType savedBiome)
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredSaltVault(manager); renderer.SetZone(town); AssertSaltVaultGrade(f, true);
                manager.WorldMap.Tiles[15, 15] = savedBiome;
                AssertSaltVaultGrade(f, false, savedBiome);
                Assert.AreEqual(savedBiome, manager.WorldMap.GetBiome(15, 15));
                manager.WorldMap.Tiles[15, 15] = BiomeType.Beating; AssertSaltVaultGrade(f, true);
            }
        }
    }
}
