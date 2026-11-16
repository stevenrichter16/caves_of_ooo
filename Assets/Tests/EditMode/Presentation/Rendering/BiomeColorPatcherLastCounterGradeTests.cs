using CavesOfOoo.Core;
using CavesOfOoo.Presentation.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed partial class BiomeColorPatcherTravelTests
    {
        static readonly BiomePalette LastCounterNeutral = new BiomePalette(BiomeType.Beating, Color.white, 12f, 0f, 0f);

        static Zone AuthoredLastCounter(OverworldZoneManager manager)
        {
            Assert.AreEqual(BiomeType.Beating, manager.WorldMap.GetBiome(18, 18),
                "LastCounter is authored Beating; do not manufacture a different biome test premise.");
            var zone = new Zone(LastCounterCompositionPlan.ZoneID);
            manager.SetActiveZone(zone);
            Assert.True(CavesOfOoo.Rendering.FlatPixelSettlementGroundCatalog.IsSupportedZone(zone));
            return zone;
        }

        static void AssertLastCounterGrade(Fixture f, bool pilot, BiomeType biome = BiomeType.Beating)
        {
            Call(f.Patcher, "Update"); Set(f.Patcher, "_fadeProgress", .999999f); Call(f.Patcher, "Update");
            Assert.True(Matches(f.Volume.profile, pilot ? LastCounterNeutral : BiomePalette.GetForBiome(biome)));
            AssertInitial(f.Shared);
        }

        [Test]
        public void NativeLastCounterGradePreservesContrastAndRestoresSameBiomeTravel()
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredLastCounter(manager); renderer.SetZone(town); AssertLastCounterGrade(f, true);
                var outside = new Zone("Overworld.17.18.0"); manager.SetActiveZone(outside);
                Assert.AreEqual(BiomeType.Beating, manager.WorldMap.GetBiome(17, 18));
                renderer.SetZone(outside); AssertLastCounterGrade(f, false);
                renderer.SetZone(town); AssertLastCounterGrade(f, true);
            }
        }

        [TestCase("legacy")][TestCase("sprites-off")][TestCase("wrong-poi")]
        [TestCase("wrong-name")][TestCase("wrong-profile")][TestCase("detached")][TestCase("replaced-cache")]
        public void NativeLastCounterGradeLosesAndRecoversCurrentLastCounterAuthority(string loss)
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); var env = EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredLastCounter(manager); renderer.SetZone(town); AssertLastCounterGrade(f, true);
                var poi = manager.WorldMap.GetPOI(18, 18); var name = poi.Name; var profile = poi.Profile;
                if (loss == "legacy") renderer.FlatPixelModeEnabled = false;
                if (loss == "sprites-off") env.RenderingEnabled = false;
                if (loss == "wrong-poi") manager.WorldMap.POIs[18, 18] = null;
                if (loss == "wrong-name") poi.Name = "another town";
                if (loss == "wrong-profile") poi.Profile = "another profile";
                if (loss == "detached") manager.CachedZones.Remove(town.ZoneID);
                if (loss == "replaced-cache") manager.CachedZones[town.ZoneID] = new Zone(town.ZoneID);
                AssertLastCounterGrade(f, false);
                renderer.FlatPixelModeEnabled = true; env.RenderingEnabled = true;
                manager.WorldMap.POIs[18, 18] = poi; poi.Name = name; poi.Profile = profile;
                manager.CachedZones[town.ZoneID] = town; AssertLastCounterGrade(f, true);
            }
        }

        [Test]
        public void NativeLastCounterGradeFollowsDisplayedReplacementAndExcludesSavedLair()
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var old = OverworldZoneManager.CreateDetached(null, 64);
                renderer.SetZone(AuthoredLastCounter(old)); AssertLastCounterGrade(f, true);
                var replacement = OverworldZoneManager.CreateDetached(null, 65);
                var town = AuthoredLastCounter(replacement); renderer.SetZone(town);
                AssertLastCounterGrade(f, true); Assert.AreSame(replacement, f.Patcher.ZoneManager);
                var poi = replacement.WorldMap.GetPOI(18, 18);
                replacement.WorldMap.SetPOI(18, 18, new PointOfInterest(POIType.Lair, "saved legacy", null, 1, "DesertProwler"));
                LairStacks.Restore(replacement, null);
                Assert.NotNull(LairStacks.Inspect(replacement, town.ZoneID));
                replacement.WorldMap.SetPOI(18, 18, poi); AssertLastCounterGrade(f, false);
            }
        }

        [TestCase(BiomeType.Sodden)][TestCase(BiomeType.Spread)][TestCase(BiomeType.Grovelands)]
        public void NativeLastCounterGradeDoesNotReinterpretForeignSavedBiomes(BiomeType savedBiome)
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredLastCounter(manager); renderer.SetZone(town); AssertLastCounterGrade(f, true);
                manager.WorldMap.Tiles[18, 18] = savedBiome;
                AssertLastCounterGrade(f, false, savedBiome);
                Assert.AreEqual(savedBiome, manager.WorldMap.GetBiome(18, 18));
                manager.WorldMap.Tiles[18, 18] = BiomeType.Beating; AssertLastCounterGrade(f, true);
            }
        }
        [TestCase("Overworld.018.18.0")][TestCase("Overworld.18.18.1")][TestCase("Overworld.18.18.00")]
        public void NativeLastCounterGradeRefusesAttachedAliasesAndDepths(string id)
        {
            using(var f=new Fixture())
            {
                var renderer=f.CreateRenderer();EnableNative(renderer);var manager=OverworldZoneManager.CreateDetached(null,64);
                var town=AuthoredLastCounter(manager);renderer.SetZone(town);AssertLastCounterGrade(f,true);
                var other=new Zone(id);manager.SetActiveZone(other);renderer.SetZone(other);AssertLastCounterGrade(f,false,id=="Overworld.18.18.1"?BiomeType.Cave:BiomeType.Beating);
            }
        }
    }
}
