using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class WorldAudioSettlementMusicTests
    {
        Zone zone;WorldMap map;Entity marker;MorrowfastSceneStatePart state;
        [SetUp] public void Setup()
        {
            zone=new Zone(MorrowfastSceneRuntime.ZoneID);map=new WorldMap(64);map.SetPOI(3,6,new PointOfInterest(POIType.Village,"Morrowfast","Stillcord",profile:"Morrowfast"));
            marker=new Entity{ID="morrowfast-scene-state"};state=new MorrowfastSceneStatePart();marker.AddPart(state);zone.AddEntity(marker,0,0);
        }
        [Test] public void InstalledAuthoredSettlementHasIdentityWithoutCountingOrRevealingItsInhabitants()
        {Assert.True(WorldAudioSettlement.IsPreparationStop(zone,map));Assert.AreEqual(1,zone.EntityCount);foreach(var cell in zone.Cells){Assert.False(cell.Explored);Assert.False(cell.IsVisible);}Assert.False(map.IsVisited(3,6));}
        [Test] public void CoordinateWithoutTheInstalledSceneCannotInventATown(){zone.RemoveEntity(marker);Assert.False(WorldAudioSettlement.IsPreparationStop(zone,map));zone.AddEntity(marker,0,0);Assert.True(WorldAudioSettlement.IsPreparationStop(zone,map));}
        [Test] public void MatchingNameDoesNotReplaceTheAuthoredProfile(){map.GetPOI(3,6).Profile="ordinary";Assert.False(WorldAudioSettlement.IsPreparationStop(zone,map));map.GetPOI(3,6).Profile="Morrowfast";Assert.True(WorldAudioSettlement.IsPreparationStop(zone,map));}
        [Test] public void RenamingExistingPlaceDoesNotLoseItsAuthoredProfile(){map.GetPOI(3,6).Name="a renamed settlement";Assert.True(WorldAudioSettlement.IsPreparationStop(zone,map));}
        [Test] public void WrongPOITypeCannotUseAReusedProfile(){map.GetPOI(3,6).Type=POIType.Lair;Assert.False(WorldAudioSettlement.IsPreparationStop(zone,map));}
        [Test] public void NoPOIOrMapCannotInventAPreparationStop(){map.POIs[3,6]=null;Assert.False(WorldAudioSettlement.IsPreparationStop(zone,map));Assert.False(WorldAudioSettlement.IsPreparationStop(zone,null));}
        [Test] public void UnsupportedSavedRevisionIsNotTreatedAsInstalled(){state.Revision=2;Assert.False(WorldAudioSettlement.IsPreparationStop(zone,map));state.Revision=1;Assert.True(WorldAudioSettlement.IsPreparationStop(zone,map));}
        [Test] public void RemovedOrRelocatedMarkerInvalidatesIdentityImmediately(){zone.MoveEntity(marker,1,0);Assert.False(WorldAudioSettlement.IsPreparationStop(zone,map));zone.MoveEntity(marker,0,0);Assert.True(WorldAudioSettlement.IsPreparationStop(zone,map));}
        [TestCase("Overworld.3.6.1")][TestCase("Overworld.03.6.0")][TestCase("Overworld.4.6.0")][TestCase("WorldMap")]
        public void NearbyDeepAliasAndMapAddressesCannotClaimTheTown(string id){zone.ZoneID=id;Assert.False(WorldAudioSettlement.IsPreparationStop(zone,map));}
        [Test] public void DestroyedMapTilesHaveARealRestoredCounterpart(){var tiles=map.Tiles;map.Tiles=null;Assert.False(WorldAudioSettlement.IsPreparationStop(zone,map));map.Tiles=tiles;Assert.True(WorldAudioSettlement.IsPreparationStop(zone,map));}
        [Test] public void CalmSettlementMusicPreservesItsNaturalAmbientLayer(){var c=WorldAudioContext.Select(BiomeType.Grovelands,0,false,true);Assert.AreEqual("village",c.MusicId);Assert.AreEqual("field",c.AmbienceId);Assert.False(c.Danger);}
        [TestCase(true,0,"danger")][TestCase(false,1,"underground")][TestCase(true,1,"danger")]
        public void DangerAndDepthOverridePreparationMusic(bool danger,int depth,string expected){Assert.AreEqual(expected,WorldAudioContext.Select(BiomeType.Grovelands,depth,danger,true).MusicId);}
        [Test] public void OrdinaryWildlifeAndRegionRetainRegionalMusic(){Assert.AreEqual("grovelands",WorldAudioContext.Select(BiomeType.Grovelands,0,false,false).MusicId);}
        [Test] public void RealFullLicensedCompositionAndCreditsArePrepared()
        {var library=WorldAudioLibrary.LoadResources();var clip=library.Music("village")?.Clip;Assert.NotNull(clip);Assert.AreSame(Resources.Load<AudioClip>("Audio/World/Music/Hiraeth"),clip);Assert.Greater(clip.length,300);Assert.AreEqual(AudioClipLoadType.Streaming,clip.loadType);StringAssert.Contains("'Hiraeth' by Scott Buckley - released under CC-BY 4.0.",Resources.Load<TextAsset>("Audio/World/REGIONAL-MUSIC-CREDITS").text);}
    }
}
