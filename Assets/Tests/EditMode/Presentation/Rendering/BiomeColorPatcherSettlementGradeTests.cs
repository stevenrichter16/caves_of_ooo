using CavesOfOoo.Core;
using CavesOfOoo.Presentation.Rendering;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed partial class BiomeColorPatcherTravelTests
    {
        static readonly BiomePalette SettlementNeutral = new BiomePalette(BiomeType.Spread, Color.white, 5f, 0f, 0f);
        static EnvironmentSpriteRenderer EnableNative(ZoneRenderer renderer)
        {
            var environment = renderer.gameObject.AddComponent<EnvironmentSpriteRenderer>();
            typeof(ZoneRenderer).GetField("_envSpriteRenderer", Private).SetValue(renderer, environment);
            typeof(ZoneRenderer).GetField("_flatPixelModeEnabled", Private).SetValue(renderer, true);
            return environment;
        }
        static void AssertGrade(Fixture f, bool pilot)
        {
            Call(f.Patcher, "Update");
            Set(f.Patcher, "_fadeProgress", .999999f);
            Call(f.Patcher, "Update");
            Assert.True(Matches(f.Volume.profile, pilot ? SettlementNeutral : BiomePalette.Jungle), "Actual Volume writer must select the expected bounded grade.");
            AssertInitial(f.Shared);
        }
        [Test]
        public void NativeMarrowstyeRestoresAuthoredMaterialHueAndSameBiomeTravelRestoresGrade()
        {
            using(var f = new Fixture())
            {
                var renderer=f.CreateRenderer(); EnableNative(renderer);
                var manager=OverworldZoneManager.CreateDetached(null,65);
                var town=Attach(manager,"Overworld.12.12.0",BiomeType.Spread);
                Assert.True(FlatPixelSettlementGroundCatalog.IsSupportedZone(town)); renderer.SetZone(town);
                Assert.True(renderer.FlatPixelModeActive); AssertGrade(f,true);
                renderer.SetZone(Attach(manager,"Overworld.12.13.0",BiomeType.Spread)); AssertGrade(f,false);
                renderer.SetZone(town); AssertGrade(f,true);
            }
        }
        [TestCase("legacy")][TestCase("sprites-off")][TestCase("wrong-poi")][TestCase("detached")][TestCase("wrong-profile")]
        public void NativeSettlementGradeLosesAndRecoversAuthority(string loss)
        {
            using(var f=new Fixture())
            {
                var renderer=f.CreateRenderer();var env=EnableNative(renderer);var manager=OverworldZoneManager.CreateDetached(null,65);
                var town=Attach(manager,"Overworld.12.12.0",BiomeType.Spread);renderer.SetZone(town);AssertGrade(f,true);
                var poi=manager.WorldMap.POIs[12,12];var originalProfile=poi.Profile;
                if(loss=="legacy") renderer.FlatPixelModeEnabled=false;
                if(loss=="sprites-off") env.RenderingEnabled=false;
                if(loss=="wrong-profile") poi.Profile="TentCamp";
                if(loss=="wrong-poi") manager.WorldMap.POIs[12,12]=null;
                if(loss=="detached") manager.CachedZones.Remove(town.ZoneID);
                AssertGrade(f,false);
                renderer.FlatPixelModeEnabled=true;env.RenderingEnabled=true;manager.WorldMap.POIs[12,12]=poi;poi.Profile=originalProfile;manager.CachedZones[town.ZoneID]=town;
                AssertGrade(f,true);
            }
        }
        [Test]
        public void ReplacementGraphAtSameAddressReevaluatesSettlementGrade()
        {
            using(var f=new Fixture())
            {
                var renderer=f.CreateRenderer();EnableNative(renderer);var manager=OverworldZoneManager.CreateDetached(null,65);
                var town=Attach(manager,"Overworld.12.12.0",BiomeType.Spread);renderer.SetZone(town);AssertGrade(f,true);
                var replacement=OverworldZoneManager.CreateDetached(null,64);var next=Attach(replacement,town.ZoneID,BiomeType.Spread);replacement.WorldMap.POIs[12,12]=null;
                renderer.SetZone(next);AssertGrade(f,false);Assert.AreSame(replacement,f.Patcher.ZoneManager);
            }
        }
        [TestCase("Overworld.12.13.0")][TestCase("Overworld.10.10.0")][TestCase("Overworld.3.6.0")]
        public void OtherSpreadAreasKeepTheirExistingPalette(string id)
        {
            using(var f=new Fixture())
            {
                var renderer=f.CreateRenderer();EnableNative(renderer);var manager=OverworldZoneManager.CreateDetached(null,65);
                renderer.SetZone(Attach(manager,id,BiomeType.Spread));AssertGrade(f,false);
            }
        }
    }
}
