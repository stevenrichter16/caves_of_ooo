using CavesOfOoo.Core;
using CavesOfOoo.Presentation.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed partial class BiomeColorPatcherTravelTests
    {
        static readonly BiomePalette CinderholdNeutral = new BiomePalette(BiomeType.Grovelands, Color.white, 8f, 0f, 0f);

        static Zone AuthoredCinderhold(OverworldZoneManager manager)
        {
            Assert.AreEqual(BiomeType.Grovelands, manager.WorldMap.GetBiome(6, 6),
                "Cinderhold is authored Grovelands; do not manufacture a Sodden test premise.");
            var zone = new Zone(CinderholdCompositionPlan.ZoneID);
            manager.SetActiveZone(zone);
            Assert.True(CavesOfOoo.Rendering.FlatPixelSettlementGroundCatalog.IsSupportedZone(zone));
            return zone;
        }

        static void AssertCinderholdGrade(Fixture f, bool pilot, BiomeType biome = BiomeType.Grovelands)
        {
            Call(f.Patcher, "Update"); Set(f.Patcher, "_fadeProgress", .999999f); Call(f.Patcher, "Update");
            Assert.True(Matches(f.Volume.profile, pilot ? CinderholdNeutral : BiomePalette.GetForBiome(biome)));
            AssertInitial(f.Shared);
        }

        [Test]
        public void NativeCinderholdGradePreservesContrastAndRestoresSameBiomeTravel()
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredCinderhold(manager); renderer.SetZone(town); AssertCinderholdGrade(f, true);
                var outside = new Zone("Overworld.5.6.0"); manager.SetActiveZone(outside);
                Assert.AreEqual(BiomeType.Grovelands, manager.WorldMap.GetBiome(5, 6));
                renderer.SetZone(outside); AssertCinderholdGrade(f, false);
                renderer.SetZone(town); AssertCinderholdGrade(f, true);
            }
        }

        [TestCase("legacy")][TestCase("sprites-off")][TestCase("wrong-poi")]
        [TestCase("wrong-type")][TestCase("wrong-name")][TestCase("wrong-profile")][TestCase("detached")][TestCase("replaced-cache")]
        public void NativeCinderholdGradeLosesAndRecoversCurrentPruningPostAuthority(string loss)
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); var env = EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredCinderhold(manager); renderer.SetZone(town); AssertCinderholdGrade(f, true);
                var poi = manager.WorldMap.GetPOI(6, 6); var name = poi.Name; var profile = poi.Profile; var type = poi.Type;
                if (loss == "legacy") renderer.FlatPixelModeEnabled = false;
                if (loss == "sprites-off") env.RenderingEnabled = false;
                if (loss == "wrong-poi") manager.WorldMap.POIs[6, 6] = null;
                if (loss == "wrong-type") poi.Type = POIType.Lair;
                if (loss == "wrong-name") poi.Name = "another town";
                if (loss == "wrong-profile") poi.Profile = "another profile";
                if (loss == "detached") manager.CachedZones.Remove(town.ZoneID);
                if (loss == "replaced-cache") manager.CachedZones[town.ZoneID] = new Zone(town.ZoneID);
                AssertCinderholdGrade(f, false);
                renderer.FlatPixelModeEnabled = true; env.RenderingEnabled = true;
                manager.WorldMap.POIs[6, 6] = poi; poi.Name = name; poi.Profile = profile; poi.Type = type;
                manager.CachedZones[town.ZoneID] = town; AssertCinderholdGrade(f, true);
            }
        }

        [Test]
        public void NativeCinderholdGradeFollowsDisplayedReplacementAndExcludesSavedLair()
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var old = OverworldZoneManager.CreateDetached(null, 64);
                renderer.SetZone(AuthoredCinderhold(old)); AssertCinderholdGrade(f, true);
                var replacement = OverworldZoneManager.CreateDetached(null, 65);
                var town = AuthoredCinderhold(replacement); renderer.SetZone(town);
                AssertCinderholdGrade(f, true); Assert.AreSame(replacement, f.Patcher.ZoneManager);
                var poi = replacement.WorldMap.GetPOI(6, 6);
                replacement.WorldMap.SetPOI(6, 6, new PointOfInterest(POIType.Lair, "saved legacy", null, 1, "DesertProwler"));
                LairStacks.Restore(replacement, null);
                Assert.NotNull(LairStacks.Inspect(replacement, town.ZoneID));
                replacement.WorldMap.SetPOI(6, 6, poi); AssertCinderholdGrade(f, false);
            }
        }

        [TestCase(BiomeType.Jungle)][TestCase(BiomeType.Sodden)][TestCase(BiomeType.Beating)][TestCase(BiomeType.Spread)]
        public void NativeCinderholdGradeDoesNotReinterpretForeignSavedBiomes(BiomeType savedBiome)
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredCinderhold(manager); renderer.SetZone(town); AssertCinderholdGrade(f, true);
                manager.WorldMap.Tiles[6, 6] = savedBiome;
                AssertCinderholdGrade(f, false, savedBiome);
                Assert.AreEqual(savedBiome, manager.WorldMap.GetBiome(6, 6));
                manager.WorldMap.Tiles[6, 6] = BiomeType.Grovelands; AssertCinderholdGrade(f, true);
            }
        }

        [TestCase("Overworld.6.6.1")][TestCase("Overworld.06.6.0")][TestCase("Overworld.6.6.0.extra")]
        public void NativeCinderholdGradeExcludesDepthAndAddressAliases(string id)
        {
            using (var f = new Fixture())
            {
                var r = f.CreateRenderer(); EnableNative(r);
                var m = OverworldZoneManager.CreateDetached(null, 64);
                var zone = new Zone(id); m.SetActiveZone(zone); r.SetZone(zone);
                Call(f.Patcher, "Update"); Set(f.Patcher, "_fadeProgress", .999999f); Call(f.Patcher, "Update");
                Assert.False(Matches(f.Volume.profile, CinderholdNeutral));
                AssertInitial(f.Shared);
            }
        }
    }
}
