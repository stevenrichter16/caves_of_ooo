using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class WorldAudioFrozenWaterAdversarialTests : WorldAudioFrozenWaterFixture
    {
        [TestCase("WaterPuddle",0)][TestCase("BrinePool",0)][TestCase("MirePool",0)]
        [TestCase("WaterPuddle",1)][TestCase("BrinePool",1)][TestCase("MirePool",1)]
        public void NearbyIceDoesNotFreezeTheActualSource(string blueprint,int level)
        {var pool=WetPool(blueprint);Zone.TileState.WriteCoating(12,10,"ice",4);Start(pool,level);Assert.AreEqual(1,Scene.SourceCount);}
        [TestCase("water")][TestCase("oil")][TestCase("brine")][TestCase("bog-mire")]
        public void UnrelatedActiveCoatingDoesNotSilenceLiquidPhase(string coating)
        {var pool=WetPool("WaterPuddle");Zone.TileState.WriteCoating(11,10,coating,4);Start(pool);Assert.AreEqual(1,Scene.SourceCount);}
        [TestCase("WaterPuddle")][TestCase("BrinePool")][TestCase("MirePool")]
        public void ColdTileWithoutIceDoesNotInventAFrozenPhase(string blueprint)
        {var pool=WetPool(blueprint);Zone.TileState.AddCold(11,10,1);Assert.False(Zone.TileState.HasCoating(11,10,"ice"));Start(pool);}
        [TestCase("empty")][TestCase("hidden")][TestCase("removed")][TestCase("inventory")]
        public void RemovingIceCannotReviveAnOtherwiseInvalidOwner(string change)
        {
            var pool=WetPool("WaterPuddle");Zone.TileState.WriteCoating(11,10,"ice",4);Observe();Assert.AreEqual(0,Scene.SourceCount);
            if(change=="empty")pool.GetPart<LiquidPoolPart>().Volume=0;
            if(change=="hidden")Zone.GetEntityCell(pool).IsVisible=false;
            if(change=="removed")Zone.RemoveEntity(pool);
            if(change=="inventory")pool.GetPart<PhysicsPart>().InInventory=Actor;
            Zone.TileState.RemoveCoating(11,10,"ice");Observe();Assert.AreEqual(0,Scene.SourceCount);
        }
        [TestCase("WaterPuddle")][TestCase("BrinePool")][TestCase("MirePool")]
        public void FailedFreezeInventoryTransactionKeepsTheWetSourceAudible(string blueprint)
        {
            var pool=WetPool(blueprint);var item=Carry("FrostLichen");string command=Freeze(item);FailAfter();
            Assert.False(Act(item,command));Assert.False(Zone.TileState.HasCoating(11,10,"ice"));Assert.Contains(item,Pack.Objects);Start(pool);
        }
        [Test] public void AnIndependentUnfrozenFlowStillOwnsTheRiverBed()
        {
            var near=WetPool("WaterPuddle");Zone.TileState.WriteCoating(11,10,"ice",4);
            var flow=Place("WaterPuddle",13,10);flow.SetTag("FlowsEast");Observe();Assert.AreEqual("water",Scene.AmbienceId);
            Assert.AreEqual(1,Scene.SourceCount);Assert.False(LiquidSourcePhase.CanDrawWater(Zone,near));Assert.True(LiquidSourcePhase.CanDrawWater(Zone,flow));
        }
        [Test] public void ThawingOnIdleDoesNotDiscoverANewSourceOrBurst()
        {
            WetPool("WaterPuddle");Zone.TileState.WriteCoating(11,10,"ice",4);Observe();int scans=Scene.ScanCount;
            Zone.TileState.RemoveCoating(11,10,"ice");for(int i=0;i<30;i++)Scene.Tick(1,true,false);Assert.AreEqual(0,Scene.MaterialPhrases);Assert.AreEqual(scans,Scene.ScanCount);
            Observe();Scene.Tick(10,true,false);Assert.AreEqual(1,Scene.MaterialPhrases);
        }
    }
}
