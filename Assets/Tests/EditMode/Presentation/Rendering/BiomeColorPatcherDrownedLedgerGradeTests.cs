using CavesOfOoo.Core;
using CavesOfOoo.Presentation.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed partial class BiomeColorPatcherTravelTests
    {
        static readonly BiomePalette DrownedLedgerNeutral = new BiomePalette(BiomeType.Sodden, Color.white, 6f, 0f, 0f);

        static Zone AuthoredDrownedLedger(OverworldZoneManager manager)
        {
            Assert.AreEqual(BiomeType.Sodden, manager.WorldMap.GetBiome(17, 5),
                "DrownedLedger is authored Sodden; do not manufacture a different biome test premise.");
            var zone = new Zone(DrownedLedgerCompositionPlan.ZoneID);
            manager.SetActiveZone(zone);
            Assert.True(CavesOfOoo.Rendering.FlatPixelSettlementGroundCatalog.IsSupportedZone(zone));
            return zone;
        }

        static void AssertDrownedLedgerGrade(Fixture f, bool pilot, BiomeType biome = BiomeType.Sodden)
        {
            Call(f.Patcher, "Update"); Set(f.Patcher, "_fadeProgress", .999999f); Call(f.Patcher, "Update");
            Assert.True(Matches(f.Volume.profile, pilot ? DrownedLedgerNeutral : BiomePalette.GetForBiome(biome)));
            AssertInitial(f.Shared);
        }

        [Test]
        public void NativeDrownedLedgerGradePreservesContrastAndRestoresSameBiomeTravel()
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredDrownedLedger(manager); renderer.SetZone(town); AssertDrownedLedgerGrade(f, true);
                var outside = new Zone("Overworld.18.5.0"); manager.SetActiveZone(outside);
                Assert.AreEqual(BiomeType.Sodden, manager.WorldMap.GetBiome(18, 5));
                renderer.SetZone(outside); AssertDrownedLedgerGrade(f, false);
                renderer.SetZone(town); AssertDrownedLedgerGrade(f, true);
            }
        }

        [TestCase("legacy")][TestCase("sprites-off")][TestCase("wrong-poi")]
        [TestCase("wrong-name")][TestCase("wrong-profile")][TestCase("detached")][TestCase("replaced-cache")]
        public void NativeDrownedLedgerGradeLosesAndRecoversCurrentDrownedLedgerAuthority(string loss)
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); var env = EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredDrownedLedger(manager); renderer.SetZone(town); AssertDrownedLedgerGrade(f, true);
                var poi = manager.WorldMap.GetPOI(17, 5); var name = poi.Name; var profile = poi.Profile;
                if (loss == "legacy") renderer.FlatPixelModeEnabled = false;
                if (loss == "sprites-off") env.RenderingEnabled = false;
                if (loss == "wrong-poi") manager.WorldMap.POIs[17, 5] = null;
                if (loss == "wrong-name") poi.Name = "another town";
                if (loss == "wrong-profile") poi.Profile = "another profile";
                if (loss == "detached") manager.CachedZones.Remove(town.ZoneID);
                if (loss == "replaced-cache") manager.CachedZones[town.ZoneID] = new Zone(town.ZoneID);
                AssertDrownedLedgerGrade(f, false);
                renderer.FlatPixelModeEnabled = true; env.RenderingEnabled = true;
                manager.WorldMap.POIs[17, 5] = poi; poi.Name = name; poi.Profile = profile;
                manager.CachedZones[town.ZoneID] = town; AssertDrownedLedgerGrade(f, true);
            }
        }

        [Test]
        public void NativeDrownedLedgerGradeFollowsDisplayedReplacementAndExcludesSavedLair()
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var old = OverworldZoneManager.CreateDetached(null, 64);
                renderer.SetZone(AuthoredDrownedLedger(old)); AssertDrownedLedgerGrade(f, true);
                var replacement = OverworldZoneManager.CreateDetached(null, 65);
                var town = AuthoredDrownedLedger(replacement); renderer.SetZone(town);
                AssertDrownedLedgerGrade(f, true); Assert.AreSame(replacement, f.Patcher.ZoneManager);
                var poi = replacement.WorldMap.GetPOI(17, 5);
                replacement.WorldMap.SetPOI(17, 5, new PointOfInterest(POIType.Lair, "saved legacy", null, 1, "DesertProwler"));
                LairStacks.Restore(replacement, null);
                Assert.NotNull(LairStacks.Inspect(replacement, town.ZoneID));
                replacement.WorldMap.SetPOI(17, 5, poi); AssertDrownedLedgerGrade(f, false);
            }
        }

        [TestCase(BiomeType.Beating)][TestCase(BiomeType.Spread)][TestCase(BiomeType.Grovelands)]
        public void NativeDrownedLedgerGradeDoesNotReinterpretForeignSavedBiomes(BiomeType savedBiome)
        {
            using (var f = new Fixture())
            {
                var renderer = f.CreateRenderer(); EnableNative(renderer);
                var manager = OverworldZoneManager.CreateDetached(null, 64);
                var town = AuthoredDrownedLedger(manager); renderer.SetZone(town); AssertDrownedLedgerGrade(f, true);
                manager.WorldMap.Tiles[17, 5] = savedBiome;
                AssertDrownedLedgerGrade(f, false, savedBiome);
                Assert.AreEqual(savedBiome, manager.WorldMap.GetBiome(17, 5));
                manager.WorldMap.Tiles[17, 5] = BiomeType.Sodden; AssertDrownedLedgerGrade(f, true);
            }
        }
        [TestCase("Overworld.017.5.0")][TestCase("Overworld.17.5.1")][TestCase("Overworld.17.5.00")]
        public void NativeDrownedLedgerGradeRefusesAttachedAliasesAndDepths(string id)
        {
            using(var f=new Fixture())
            {
                var renderer=f.CreateRenderer();EnableNative(renderer);var manager=OverworldZoneManager.CreateDetached(null,64);
                var town=AuthoredDrownedLedger(manager);renderer.SetZone(town);AssertDrownedLedgerGrade(f,true);
                var other=new Zone(id);manager.SetActiveZone(other);renderer.SetZone(other);AssertDrownedLedgerGrade(f,false,id=="Overworld.17.5.1"?BiomeType.Cave:BiomeType.Sodden);
            }
        }
    }
}
