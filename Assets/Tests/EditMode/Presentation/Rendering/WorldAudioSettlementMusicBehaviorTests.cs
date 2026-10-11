using System;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class WorldAudioSettlementMusicBehaviorTests
    {
        static WorldAudioContext Select(int depth,bool danger,bool stop)
        {var m=typeof(WorldAudioContext).GetMethod("Select",new[]{typeof(BiomeType),typeof(int),typeof(bool),typeof(bool)});Assert.NotNull(m,"Known inhabited preparation stops cannot yet select their own score.");return (WorldAudioContext)m.Invoke(null,new object[]{BiomeType.Grovelands,depth,danger,stop});}
        static bool Town(Zone zone,WorldMap map)
        {var t=typeof(WorldAudioContext).Assembly.GetType("CavesOfOoo.Rendering.WorldAudioSettlement");Assert.NotNull(t,"Preparation music must require the actual installed authored settlement.");var m=t.GetMethod("IsPreparationStop",new[]{typeof(Zone),typeof(WorldMap)});Assert.NotNull(m);return (bool)m.Invoke(null,new object[]{zone,map});}
        [Test] public void CalmTownHasOwnMusicAndKeepsNaturalAmbience(){var c=Select(0,false,true);Assert.AreEqual("village",c.MusicId);Assert.AreEqual("field",c.AmbienceId);Assert.AreEqual("grovelands",Select(0,false,false).MusicId);}
        [TestCase(true,0,"danger")][TestCase(false,1,"underground")][TestCase(true,1,"danger")]
        public void TownCannotOverrideDangerOrUnderground(bool danger,int depth,string expected){Assert.AreEqual(expected,Select(depth,danger,true).MusicId);}
        [Test] public void OnlyCurrentInstalledTownMarkerAuthorizesThePlace()
        {
            var zone=new Zone(MorrowfastSceneRuntime.ZoneID);var map=new WorldMap(64);map.SetPOI(3,6,new PointOfInterest(POIType.Village,"Morrowfast","Stillcord",profile:"Morrowfast"));
            var marker=new Entity{ID="morrowfast-scene-state"};marker.AddPart(new MorrowfastSceneStatePart());zone.AddEntity(marker,0,0);
            Assert.True(Town(zone,map));zone.RemoveEntity(marker);Assert.False(Town(zone,map));zone.AddEntity(marker,0,0);Assert.True(Town(zone,map));map.GetPOI(3,6).Profile="ordinary";Assert.False(Town(zone,map));
        }
    }
}
