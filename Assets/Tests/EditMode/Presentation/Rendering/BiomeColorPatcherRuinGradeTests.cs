using CavesOfOoo.Core;
using CavesOfOoo.Presentation.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed partial class BiomeColorPatcherTravelTests
    {
        static readonly BiomePalette RuinNeutral = new BiomePalette(BiomeType.Beating, Color.white, 12f, 0f, 0f);
        static void AssertRuinGrade(Fixture f, bool pilot, BiomeType biome=BiomeType.Beating)
        {
            Call(f.Patcher,"Update");Set(f.Patcher,"_fadeProgress",.999999f);Call(f.Patcher,"Update");
            Assert.True(Matches(f.Volume.profile,pilot?RuinNeutral:BiomePalette.GetForBiome(biome)));
            AssertInitial(f.Shared);
        }
        [Test] public void NativeRuinGradePreservesContrastAndRestoresRoadAndDune()
        {
            using(var f=new Fixture())
            {
                var r=f.CreateRenderer();EnableNative(r);var m=OverworldZoneManager.CreateDetached(null,64);
                Assert.AreEqual(BiomeType.Beating,m.WorldMap.GetBiome(19,12));
                var z=Attach(m,"Overworld.19.12.0",BiomeType.Beating);r.SetZone(z);AssertRuinGrade(f,true);
                r.SetZone(Attach(m,"Overworld.19.13.0",BiomeType.Beating));AssertRuinGrade(f,false);
                r.SetZone(Attach(m,"Overworld.19.14.0",BiomeType.Beating));AssertDuneGrade(f,true);
                r.SetZone(z);AssertRuinGrade(f,true);
            }
        }
        [TestCase("legacy")] [TestCase("sprites-off")] [TestCase("detached")] [TestCase("replaced-cache")] [TestCase("poi")]
        public void NativeRuinGradeLosesAndRecoversCurrentAuthority(string loss)
        {
            using(var f=new Fixture())
            {
                var r=f.CreateRenderer();var env=EnableNative(r);var m=OverworldZoneManager.CreateDetached(null,64);
                var z=Attach(m,"Overworld.19.12.0",BiomeType.Beating);r.SetZone(z);AssertRuinGrade(f,true);
                if(loss=="legacy")r.FlatPixelModeEnabled=false;
                if(loss=="sprites-off")env.RenderingEnabled=false;
                if(loss=="detached")m.CachedZones.Remove(z.ZoneID);
                if(loss=="replaced-cache")m.CachedZones[z.ZoneID]=new Zone(z.ZoneID);
                if(loss=="poi")m.WorldMap.POIs[19,12]=m.WorldMap.POIs[12,12];
                AssertRuinGrade(f,false);
                r.FlatPixelModeEnabled=true;env.RenderingEnabled=true;m.CachedZones[z.ZoneID]=z;m.WorldMap.POIs[19,12]=null;
                AssertRuinGrade(f,true);
            }
        }
        [Test] public void NativeRuinGradeFollowsReplacementWorldAndRejectsSavedLair()
        {
            using(var f=new Fixture())
            {
                var r=f.CreateRenderer();EnableNative(r);var old=OverworldZoneManager.CreateDetached(null,64);
                r.SetZone(Attach(old,"Overworld.19.12.0",BiomeType.Beating));AssertRuinGrade(f,true);
                var m=OverworldZoneManager.CreateDetached(null,65);var z=Attach(m,"Overworld.19.12.0",BiomeType.Beating);
                r.SetZone(z);AssertRuinGrade(f,true);Assert.AreSame(m,f.Patcher.ZoneManager);
                m.WorldMap.SetPOI(19,12,new PointOfInterest(POIType.Lair,"saved legacy",null,1,"DesertProwler"));
                LairStacks.Restore(m,null);Assert.NotNull(LairStacks.Inspect(m,z.ZoneID));
                m.WorldMap.SetPOI(19,12,null);AssertRuinGrade(f,false);
            }
        }
        [TestCase(BiomeType.Spread)] [TestCase(BiomeType.Sodden)] [TestCase(BiomeType.Grovelands)]
        public void NativeRuinGradeDoesNotChangeForeignSavedBiome(BiomeType biome)
        {
            using(var f=new Fixture())
            {
                var r=f.CreateRenderer();EnableNative(r);var m=OverworldZoneManager.CreateDetached(null,64);
                var z=Attach(m,"Overworld.19.12.0",BiomeType.Beating);r.SetZone(z);AssertRuinGrade(f,true);
                m.WorldMap.Tiles[19,12]=biome;AssertRuinGrade(f,false,biome);Assert.AreEqual(biome,m.WorldMap.GetBiome(19,12));
                m.WorldMap.Tiles[19,12]=BiomeType.Beating;AssertRuinGrade(f,true);
            }
        }
        [TestCase("Overworld.19.12.1",BiomeType.Cave)]
        [TestCase("Overworld.019.12.0",BiomeType.Beating)]
        [TestCase("Overworld.19.012.0",BiomeType.Beating)]
        public void NativeRuinGradeDoesNotAdoptDepthOrAddressAliases(string id,BiomeType palette)
        {
            using(var f=new Fixture())
            {
                var r=f.CreateRenderer();EnableNative(r);var m=OverworldZoneManager.CreateDetached(null,64);
                r.SetZone(Attach(m,id,BiomeType.Beating));AssertRuinGrade(f,false,palette);
            }
        }
    }
}
