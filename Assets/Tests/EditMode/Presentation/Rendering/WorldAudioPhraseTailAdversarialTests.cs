using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class WorldAudioPhraseTailAdversarialTests : WorldAudioPhraseTailFixture
    {
        [TestCase("hidden")][TestCase("unexplored")][TestCase("removed")][TestCase("render")]
        [TestCase("inventory")][TestCase("equipped")][TestCase("fuel")][TestCase("extinguished")]
        [TestCase("range")][TestCase("destroyed")]
        public void SameBandChangeCannotPreserveAnInvalidPhysicalSource(string mutation)
        {
            Start("fire");var owner=Zone.GetCell(11,10).Objects.First(e=>e.ID=="tail source");
            if(mutation=="hidden")Zone.GetEntityCell(owner).IsVisible=false;if(mutation=="unexplored")Zone.GetEntityCell(owner).Explored=false;
            if(mutation=="removed")Zone.RemoveEntity(owner);if(mutation=="render")owner.GetPart<RenderPart>().Visible=false;
            if(mutation=="inventory")owner.GetPart<PhysicsPart>().InInventory=Actor;if(mutation=="equipped")owner.GetPart<PhysicsPart>().Equipped=Actor;
            if(mutation=="fuel")owner.GetPart<FuelPart>().FuelMass=0;if(mutation=="extinguished")owner.GetPart<ThermalPart>().Temperature=25;
            if(mutation=="range")Assert.True(Zone.MoveEntity(Actor,20,10));if(mutation=="destroyed")owner.AddPart(new DestructiblePart{HP=0,MaxHP=10});
            Observe(DayBand.Height);Assert.AreEqual(0,Audio.ActiveDetailSources);Assert.AreEqual(0,Scene.SourceCount);Assert.AreEqual(1,Audio.AcceptedDetailCount);
        }
        [TestCase("depth")][TestCase("biome")][TestCase("interior")][TestCase("graph")]
        public void TerritoryOrGraphChangeRetainsItsConservativeTailReset(string change)
        {
            Start("fire");
            if(change=="interior")Zone.GetEntityCell(Actor).IsInterior=Zone.GetCell(11,10).IsInterior=true;
            if(change=="graph")Scene.Observe(new Zone(Zone.ZoneID),Actor,BiomeType.Ruins,0,DayBand.Height,true);
            else Observe(DayBand.Height,change=="biome"?BiomeType.Beating:BiomeType.Ruins,change=="depth"?1:0);
            Assert.AreEqual(0,Audio.ActiveDetailSources);
        }
        [TestCase(DayBand.Height,DayBand.Dusk)][TestCase(DayBand.Dark,DayBand.Dawn)]
        public void ChangedHabitatMeaningRetiresTheOldDayOrNightTexture(DayBand before,DayBand after)
        {Habitat(before);Observe(after,BiomeType.Spread);Assert.AreEqual(0,Audio.ActiveDetailSources);Assert.AreEqual(1,Audio.AcceptedDetailCount);}
        [Test] public void FinishingHabitatDoesNotLeaveAStaleTokenToKillNextValidFire()
        {
            Habitat(DayBand.Dawn);Audio.Update(3);Scene.Tick(.01f,true,false);Source("fire");Observe(DayBand.Height,BiomeType.Spread);Scene.Tick(30,true,false);
            Assert.AreEqual("fire",Audio.LastDetailId);Observe(DayBand.Dusk,BiomeType.Spread);Assert.AreEqual(1,Audio.ActiveDetailSources);
        }
        [Test] public void MutingStillImmediatelyRetiresAValidPreservedTail()
        {Start("fire");Observe(DayBand.Height);CavesOfOoo.Rendering.WorldAudioSettings.AmbienceVolume=0;Scene.Tick(.01f,true,false);Assert.AreEqual(0,Audio.ActiveDetailSources);}
        [Test] public void HiddenWorldStillRetiresAValidPreservedTail()
        {Start("fire");Observe(DayBand.Height);Scene.Tick(.01f,false,false);Assert.AreEqual(0,Audio.ActiveDetailSources);}
        [Test] public void KeepingATailDoesNotReplayOrRescanOnIdle()
        {Start("fire");Observe(DayBand.Height);int scans=Scene.ScanCount;Scene.Tick(.01f,true,false);Assert.AreEqual(scans,Scene.ScanCount);Assert.AreEqual(1,Audio.AcceptedDetailCount);Assert.AreEqual(16,Audio.AllocatedSources);}
    }
}
