using CavesOfOoo.Core;
using CavesOfOoo.Presentation.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed partial class BiomeColorPatcherTravelTests
    {
        static readonly BiomePalette TallyNeutral = new BiomePalette(BiomeType.Spread, Color.white, 5f, 0f, 0f);

        static Zone AuthoredTally(OverworldZoneManager manager)
        {
            Assert.AreEqual(BiomeType.Spread, manager.WorldMap.GetBiome(10, 14),
                "Tally is authored Spread; do not manufacture a Sodden test premise.");
            var zone = new Zone(TallyCompositionPlan.ZoneID);
            manager.SetActiveZone(zone);
            Assert.True(CavesOfOoo.Rendering.FlatPixelSettlementGroundCatalog.IsSupportedZone(zone));
            return zone;
        }

        static void AssertTallyGrade(Fixture f, bool pilot, BiomeType biome = BiomeType.Spread)
        {
            Call(f.Patcher, "Update"); Set(f.Patcher, "_fadeProgress", .999999f); Call(f.Patcher, "Update");
            Assert.True(Matches(f.Volume.profile, pilot ? TallyNeutral : BiomePalette.GetForBiome(biome)));
            AssertInitial(f.Shared);
        }

        [Test]
        public void NativeTallyGradePreservesContrastAndRestoresSameBiomeTravel()
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredTally(manager); renderer.SetZone(town); AssertTallyGrade(f, true);
                var outside = new Zone("Overworld.9.14.0"); manager.SetActiveZone(outside);
                Assert.AreEqual(BiomeType.Spread, manager.WorldMap.GetBiome(9, 14));
                renderer.SetZone(outside); AssertTallyGrade(f, false);
                renderer.SetZone(town); AssertTallyGrade(f, true);
            }
        }

        [TestCase("legacy")][TestCase("sprites-off")][TestCase("wrong-poi")]
        [TestCase("wrong-type")][TestCase("wrong-name")][TestCase("wrong-profile")][TestCase("detached")][TestCase("replaced-cache")]
        public void NativeTallyGradeLosesAndRecoversCurrentCentralExchangeAuthority(string loss)
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); var env = EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredTally(manager); renderer.SetZone(town); AssertTallyGrade(f, true);
                var poi = manager.WorldMap.GetPOI(10, 14); var name = poi.Name; var profile = poi.Profile; var type = poi.Type;
                if (loss == "legacy") renderer.FlatPixelModeEnabled = false;
                if (loss == "sprites-off") env.RenderingEnabled = false;
                if (loss == "wrong-poi") manager.WorldMap.POIs[10, 14] = null;
                if (loss == "wrong-type") poi.Type = POIType.Lair;
                if (loss == "wrong-name") poi.Name = "another town";
                if (loss == "wrong-profile") poi.Profile = "another profile";
                if (loss == "detached") manager.CachedZones.Remove(town.ZoneID);
                if (loss == "replaced-cache") manager.CachedZones[town.ZoneID] = new Zone(town.ZoneID);
                AssertTallyGrade(f, false);
                renderer.FlatPixelModeEnabled = true; env.RenderingEnabled = true;
                manager.WorldMap.POIs[10, 14] = poi; poi.Name = name; poi.Profile = profile; poi.Type = type;
                manager.CachedZones[town.ZoneID] = town; AssertTallyGrade(f, true);
            }
        }

        [Test]
        public void NativeTallyGradeFollowsDisplayedReplacementAndExcludesSavedLair()
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var old = OverworldZoneManager.CreateDetached(null, 64);
                renderer.SetZone(AuthoredTally(old)); AssertTallyGrade(f, true);
                var replacement = OverworldZoneManager.CreateDetached(null, 65);
                var town = AuthoredTally(replacement); renderer.SetZone(town);
                AssertTallyGrade(f, true); Assert.AreSame(replacement, f.Patcher.ZoneManager);
                var poi = replacement.WorldMap.GetPOI(10, 14);
                replacement.WorldMap.SetPOI(10, 14, new PointOfInterest(POIType.Lair, "saved legacy", null, 1, "DesertProwler"));
                LairStacks.Restore(replacement, null);
                Assert.NotNull(LairStacks.Inspect(replacement, town.ZoneID));
                replacement.WorldMap.SetPOI(10, 14, poi); AssertTallyGrade(f, false);
            }
        }

        [TestCase(BiomeType.Jungle)][TestCase(BiomeType.Sodden)][TestCase(BiomeType.Beating)][TestCase(BiomeType.Grovelands)]
        public void NativeTallyGradeDoesNotReinterpretForeignSavedBiomes(BiomeType savedBiome)
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredTally(manager); renderer.SetZone(town); AssertTallyGrade(f, true);
                manager.WorldMap.Tiles[10, 14] = savedBiome;
                AssertTallyGrade(f, false, savedBiome);
                Assert.AreEqual(savedBiome, manager.WorldMap.GetBiome(10, 14));
                manager.WorldMap.Tiles[10, 14] = BiomeType.Spread; AssertTallyGrade(f, true);
            }
        }

        [TestCase("Overworld.10.14.1")][TestCase("Overworld.010.14.0")][TestCase("Overworld.10.14.0.extra")]
        public void NativeTallyGradeExcludesDepthAndAddressAliases(string id)
        {
            using (var f = new Fixture())
            {
                var r = f.CreateRenderer(); EnableNative(r);
                var m = OverworldZoneManager.CreateDetached(null, 64);
                var zone = new Zone(id); m.SetActiveZone(zone); r.SetZone(zone);
                Call(f.Patcher, "Update"); Set(f.Patcher, "_fadeProgress", .999999f); Call(f.Patcher, "Update");
                Assert.False(Matches(f.Volume.profile, TallyNeutral));
                AssertInitial(f.Shared);
            }
        }
    }
}
