using CavesOfOoo.Core;
using CavesOfOoo.Presentation.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed partial class BiomeColorPatcherTravelTests
    {
        static readonly BiomePalette GantryNeutral = new BiomePalette(BiomeType.Spread, Color.white, 5f, 0f, 0f);

        static Zone AuthoredGantry(OverworldZoneManager manager)
        {
            Assert.AreEqual(BiomeType.Spread, manager.WorldMap.GetBiome(7, 8),
                "Gantry is authored Spread; do not manufacture a Sodden test premise.");
            var zone = new Zone(GantryCompositionPlan.ZoneID);
            manager.SetActiveZone(zone);
            Assert.True(CavesOfOoo.Rendering.FlatPixelSettlementGroundCatalog.IsSupportedZone(zone));
            return zone;
        }

        static void AssertGantryGrade(Fixture f, bool pilot, BiomeType biome = BiomeType.Spread)
        {
            Call(f.Patcher, "Update"); Set(f.Patcher, "_fadeProgress", .999999f); Call(f.Patcher, "Update");
            Assert.True(Matches(f.Volume.profile, pilot ? GantryNeutral : BiomePalette.GetForBiome(biome)));
            AssertInitial(f.Shared);
        }

        [Test]
        public void NativeGantryGradePreservesContrastAndRestoresSameBiomeTravel()
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredGantry(manager); renderer.SetZone(town); AssertGantryGrade(f, true);
                var outside = new Zone("Overworld.6.8.0"); manager.SetActiveZone(outside);
                Assert.AreEqual(BiomeType.Spread, manager.WorldMap.GetBiome(6, 8));
                renderer.SetZone(outside); AssertGantryGrade(f, false);
                renderer.SetZone(town); AssertGantryGrade(f, true);
            }
        }

        [TestCase("legacy")][TestCase("sprites-off")][TestCase("wrong-poi")]
        [TestCase("wrong-type")][TestCase("wrong-name")][TestCase("wrong-profile")][TestCase("detached")][TestCase("replaced-cache")]
        public void NativeGantryGradeLosesAndRecoversCurrentCrossroadsAuthority(string loss)
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); var env = EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredGantry(manager); renderer.SetZone(town); AssertGantryGrade(f, true);
                var poi = manager.WorldMap.GetPOI(7, 8); var name = poi.Name; var profile = poi.Profile; var type = poi.Type;
                if (loss == "legacy") renderer.FlatPixelModeEnabled = false;
                if (loss == "sprites-off") env.RenderingEnabled = false;
                if (loss == "wrong-poi") manager.WorldMap.POIs[7, 8] = null;
                if (loss == "wrong-type") poi.Type = POIType.Lair;
                if (loss == "wrong-name") poi.Name = "another town";
                if (loss == "wrong-profile") poi.Profile = "another profile";
                if (loss == "detached") manager.CachedZones.Remove(town.ZoneID);
                if (loss == "replaced-cache") manager.CachedZones[town.ZoneID] = new Zone(town.ZoneID);
                AssertGantryGrade(f, false);
                renderer.FlatPixelModeEnabled = true; env.RenderingEnabled = true;
                manager.WorldMap.POIs[7, 8] = poi; poi.Name = name; poi.Profile = profile; poi.Type = type;
                manager.CachedZones[town.ZoneID] = town; AssertGantryGrade(f, true);
            }
        }

        [Test]
        public void NativeGantryGradeFollowsDisplayedReplacementAndExcludesSavedLair()
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var old = OverworldZoneManager.CreateDetached(null, 64);
                renderer.SetZone(AuthoredGantry(old)); AssertGantryGrade(f, true);
                var replacement = OverworldZoneManager.CreateDetached(null, 65);
                var town = AuthoredGantry(replacement); renderer.SetZone(town);
                AssertGantryGrade(f, true); Assert.AreSame(replacement, f.Patcher.ZoneManager);
                var poi = replacement.WorldMap.GetPOI(7, 8);
                replacement.WorldMap.SetPOI(7, 8, new PointOfInterest(POIType.Lair, "saved legacy", null, 1, "DesertProwler"));
                LairStacks.Restore(replacement, null);
                Assert.NotNull(LairStacks.Inspect(replacement, town.ZoneID));
                replacement.WorldMap.SetPOI(7, 8, poi); AssertGantryGrade(f, false);
            }
        }

        [TestCase(BiomeType.Jungle)][TestCase(BiomeType.Sodden)][TestCase(BiomeType.Beating)][TestCase(BiomeType.Grovelands)]
        public void NativeGantryGradeDoesNotReinterpretForeignSavedBiomes(BiomeType savedBiome)
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredGantry(manager); renderer.SetZone(town); AssertGantryGrade(f, true);
                manager.WorldMap.Tiles[7, 8] = savedBiome;
                AssertGantryGrade(f, false, savedBiome);
                Assert.AreEqual(savedBiome, manager.WorldMap.GetBiome(7, 8));
                manager.WorldMap.Tiles[7, 8] = BiomeType.Spread; AssertGantryGrade(f, true);
            }
        }

        [TestCase("Overworld.7.8.1")][TestCase("Overworld.07.8.0")][TestCase("Overworld.7.8.0.extra")]
        public void NativeGantryGradeExcludesDepthAndAddressAliases(string id)
        {
            using (var f = new Fixture())
            {
                var r = f.CreateRenderer(); EnableNative(r);
                var m = OverworldZoneManager.CreateDetached(null, 64);
                var zone = new Zone(id); m.SetActiveZone(zone); r.SetZone(zone);
                Call(f.Patcher, "Update"); Set(f.Patcher, "_fadeProgress", .999999f); Call(f.Patcher, "Update");
                Assert.False(Matches(f.Volume.profile, GantryNeutral));
                AssertInitial(f.Shared);
            }
        }
    }
}
