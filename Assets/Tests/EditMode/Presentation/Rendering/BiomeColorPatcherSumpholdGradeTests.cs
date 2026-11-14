using CavesOfOoo.Core;
using CavesOfOoo.Presentation.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed partial class BiomeColorPatcherTravelTests
    {
        static readonly BiomePalette SumpholdNeutral = new BiomePalette(BiomeType.Spread, Color.white, 5f, 0f, 0f);

        static Zone AuthoredSumphold(OverworldZoneManager manager)
        {
            Assert.AreEqual(BiomeType.Spread, manager.WorldMap.GetBiome(15, 6),
                "Sumphold is authored Spread; do not manufacture a Sodden test premise.");
            var zone = new Zone(SumpholdCompositionPlan.ZoneID);
            manager.SetActiveZone(zone);
            Assert.True(CavesOfOoo.Rendering.FlatPixelSettlementGroundCatalog.IsSupportedZone(zone));
            return zone;
        }

        static void AssertSumpholdGrade(Fixture f, bool pilot, BiomeType biome = BiomeType.Spread)
        {
            Call(f.Patcher, "Update"); Set(f.Patcher, "_fadeProgress", .999999f); Call(f.Patcher, "Update");
            Assert.True(Matches(f.Volume.profile, pilot ? SumpholdNeutral : BiomePalette.GetForBiome(biome)));
            AssertInitial(f.Shared);
        }

        [Test]
        public void NativeSumpholdGradePreservesContrastAndRestoresSameBiomeTravel()
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredSumphold(manager); renderer.SetZone(town); AssertSumpholdGrade(f, true);
                var outside = new Zone("Overworld.14.6.0"); manager.SetActiveZone(outside);
                Assert.AreEqual(BiomeType.Spread, manager.WorldMap.GetBiome(14, 6));
                renderer.SetZone(outside); AssertSumpholdGrade(f, false);
                renderer.SetZone(town); AssertSumpholdGrade(f, true);
            }
        }

        [TestCase("legacy")][TestCase("sprites-off")][TestCase("wrong-poi")]
        [TestCase("wrong-name")][TestCase("wrong-profile")][TestCase("detached")][TestCase("replaced-cache")]
        public void NativeSumpholdGradeLosesAndRecoversCurrentBoatyardAuthority(string loss)
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); var env = EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredSumphold(manager); renderer.SetZone(town); AssertSumpholdGrade(f, true);
                var poi = manager.WorldMap.GetPOI(15, 6); var name = poi.Name; var profile = poi.Profile;
                if (loss == "legacy") renderer.FlatPixelModeEnabled = false;
                if (loss == "sprites-off") env.RenderingEnabled = false;
                if (loss == "wrong-poi") manager.WorldMap.POIs[15, 6] = null;
                if (loss == "wrong-name") poi.Name = "another town";
                if (loss == "wrong-profile") poi.Profile = "another profile";
                if (loss == "detached") manager.CachedZones.Remove(town.ZoneID);
                if (loss == "replaced-cache") manager.CachedZones[town.ZoneID] = new Zone(town.ZoneID);
                AssertSumpholdGrade(f, false);
                renderer.FlatPixelModeEnabled = true; env.RenderingEnabled = true;
                manager.WorldMap.POIs[15, 6] = poi; poi.Name = name; poi.Profile = profile;
                manager.CachedZones[town.ZoneID] = town; AssertSumpholdGrade(f, true);
            }
        }

        [Test]
        public void NativeSumpholdGradeFollowsDisplayedReplacementAndExcludesSavedLair()
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var old = OverworldZoneManager.CreateDetached(null, 64);
                renderer.SetZone(AuthoredSumphold(old)); AssertSumpholdGrade(f, true);
                var replacement = OverworldZoneManager.CreateDetached(null, 65);
                var town = AuthoredSumphold(replacement); renderer.SetZone(town);
                AssertSumpholdGrade(f, true); Assert.AreSame(replacement, f.Patcher.ZoneManager);
                var poi = replacement.WorldMap.GetPOI(15, 6);
                replacement.WorldMap.SetPOI(15, 6, new PointOfInterest(POIType.Lair, "saved legacy", null, 1, "DesertProwler"));
                LairStacks.Restore(replacement, null);
                Assert.NotNull(LairStacks.Inspect(replacement, town.ZoneID));
                replacement.WorldMap.SetPOI(15, 6, poi); AssertSumpholdGrade(f, false);
            }
        }

        [TestCase(BiomeType.Sodden)][TestCase(BiomeType.Beating)][TestCase(BiomeType.Grovelands)]
        public void NativeSumpholdGradeDoesNotReinterpretForeignSavedBiomes(BiomeType savedBiome)
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredSumphold(manager); renderer.SetZone(town); AssertSumpholdGrade(f, true);
                manager.WorldMap.Tiles[15, 6] = savedBiome;
                AssertSumpholdGrade(f, false, savedBiome);
                Assert.AreEqual(savedBiome, manager.WorldMap.GetBiome(15, 6));
                manager.WorldMap.Tiles[15, 6] = BiomeType.Spread; AssertSumpholdGrade(f, true);
            }
        }
    }
}
