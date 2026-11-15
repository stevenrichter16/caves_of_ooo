using CavesOfOoo.Core;
using CavesOfOoo.Presentation.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed partial class BiomeColorPatcherTravelTests
    {
        static readonly BiomePalette PosyNeutral = new BiomePalette(BiomeType.Spread, Color.white, 5f, 0f, 0f);

        static Zone AuthoredPosy(OverworldZoneManager manager)
        {
            Assert.AreEqual(BiomeType.Spread, manager.WorldMap.GetBiome(5, 9),
                "Posy is authored Spread; do not manufacture a different biome test premise.");
            var zone = new Zone("Overworld.5.9.0");
            manager.SetActiveZone(zone);
            Assert.True(CavesOfOoo.Rendering.FlatPixelSettlementGroundCatalog.IsSupportedZone(zone));
            return zone;
        }

        static void AssertPosyGrade(Fixture f, bool pilot, BiomeType biome = BiomeType.Spread)
        {
            Call(f.Patcher, "Update"); Set(f.Patcher, "_fadeProgress", .999999f); Call(f.Patcher, "Update");
            Assert.True(Matches(f.Volume.profile, pilot ? PosyNeutral : BiomePalette.GetForBiome(biome)));
            AssertInitial(f.Shared);
        }

        [Test]
        public void NativePosyGradePreservesContrastAndRestoresSameBiomeTravel()
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredPosy(manager); renderer.SetZone(town); AssertPosyGrade(f, true);
                var outside = new Zone("Overworld.4.9.0"); manager.SetActiveZone(outside);
                Assert.AreEqual(BiomeType.Spread, manager.WorldMap.GetBiome(4, 9));
                renderer.SetZone(outside); AssertPosyGrade(f, false);
                renderer.SetZone(town); AssertPosyGrade(f, true);
            }
        }

        [TestCase("legacy")][TestCase("sprites-off")][TestCase("wrong-poi")]
        [TestCase("wrong-name")][TestCase("wrong-profile")][TestCase("wrong-type")]
        [TestCase("null-pois")][TestCase("short-pois")][TestCase("detached")][TestCase("replaced-cache")]
        public void NativePosyGradeLosesAndRecoversCurrentPosyAuthority(string loss)
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); var env = EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredPosy(manager); renderer.SetZone(town); AssertPosyGrade(f, true);
                var poi = manager.WorldMap.GetPOI(5, 9); var name = poi.Name; var profile = poi.Profile; var type = poi.Type; var pois = manager.WorldMap.POIs;
                if (loss == "legacy") renderer.FlatPixelModeEnabled = false;
                if (loss == "sprites-off") env.RenderingEnabled = false;
                if (loss == "wrong-poi") manager.WorldMap.POIs[5, 9] = null;
                if (loss == "wrong-name") poi.Name = "another town";
                if (loss == "wrong-profile") poi.Profile = "another profile";
                if (loss == "wrong-type") poi.Type = POIType.Lair;
                if (loss == "null-pois") manager.WorldMap.POIs = null;
                if (loss == "short-pois") manager.WorldMap.POIs = new PointOfInterest[1, 1];
                if (loss == "detached") manager.CachedZones.Remove(town.ZoneID);
                if (loss == "replaced-cache") manager.CachedZones[town.ZoneID] = new Zone(town.ZoneID);
                AssertPosyGrade(f, false);
                renderer.FlatPixelModeEnabled = true; env.RenderingEnabled = true;
                manager.WorldMap.POIs = pois; poi.Type = type;
                manager.WorldMap.POIs[5, 9] = poi; poi.Name = name; poi.Profile = profile;
                manager.CachedZones[town.ZoneID] = town; AssertPosyGrade(f, true);
            }
        }

        [Test]
        public void NativePosyGradeFollowsDisplayedReplacementAndExcludesSavedLair()
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var old = OverworldZoneManager.CreateDetached(null, 64);
                renderer.SetZone(AuthoredPosy(old)); AssertPosyGrade(f, true);
                var replacement = OverworldZoneManager.CreateDetached(null, 65);
                var town = AuthoredPosy(replacement); renderer.SetZone(town);
                AssertPosyGrade(f, true); Assert.AreSame(replacement, f.Patcher.ZoneManager);
                var poi = replacement.WorldMap.GetPOI(5, 9);
                replacement.WorldMap.SetPOI(5, 9, new PointOfInterest(POIType.Lair, "saved legacy", null, 1, "DesertProwler"));
                LairStacks.Restore(replacement, null);
                Assert.NotNull(LairStacks.Inspect(replacement, town.ZoneID));
                replacement.WorldMap.SetPOI(5, 9, poi); AssertPosyGrade(f, false);
            }
        }

        [TestCase(BiomeType.Sodden)][TestCase(BiomeType.Beating)][TestCase(BiomeType.Grovelands)]
        public void NativePosyGradeDoesNotReinterpretForeignSavedBiomes(BiomeType savedBiome)
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredPosy(manager); renderer.SetZone(town); AssertPosyGrade(f, true);
                manager.WorldMap.Tiles[5, 9] = savedBiome;
                AssertPosyGrade(f, false, savedBiome);
                Assert.AreEqual(savedBiome, manager.WorldMap.GetBiome(5, 9));
                manager.WorldMap.Tiles[5, 9] = BiomeType.Spread; AssertPosyGrade(f, true);
            }
        }
        [TestCase("Overworld.05.9.0", BiomeType.Spread)]
        [TestCase("Overworld.5.9.0.extra", BiomeType.Cave)]
        [TestCase("Overworld.5.9.1", BiomeType.Cave)]
        public void NativePosyGradeRejectsAliasAndDepth(string zoneId, BiomeType biome)
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var zone = new Zone(zoneId); manager.SetActiveZone(zone); renderer.SetZone(zone);
                AssertPosyGrade(f, false, biome);
            }
        }

        [TestCase("")][TestCase(null)] public void PosyAllowsBothEmptyProfileRepresentations(string profile)
        {
            using(var f=new Fixture())
            {
                var renderer=f.CreateRenderer();EnableNative(renderer);
                var manager=OverworldZoneManager.CreateDetached(null,64);var zone=AuthoredPosy(manager);
                manager.WorldMap.GetPOI(5,9).Profile=profile;renderer.SetZone(zone);AssertPosyGrade(f,true);
            }
        }
    }
}
