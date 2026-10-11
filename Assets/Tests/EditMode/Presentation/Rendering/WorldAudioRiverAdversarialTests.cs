using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class WorldAudioRiverAdversarialTests : WorldAudioRiverFixture
    {
        [TestCase("hidden")][TestCase("unexplored")][TestCase("removed")][TestCase("render")]
        [TestCase("inventory")][TestCase("equipped")][TestCase("empty")][TestCase("negative")]
        [TestCase("brine")][TestCase("acid")][TestCase("marker")][TestCase("physics")]
        [TestCase("render-part")][TestCase("pool-part")][TestCase("sheltered")]
        public void LostExactFlowOwnerCannotBorrowAuthorityFromRemainingPuddle(string mutation)
        {
            var flow=Pool();Observe();Assert.AreEqual("water",Scene.AmbienceId);
            StillNear();Observe();int scans=Scene.ScanCount;
            switch(mutation)
            {
                case "hidden":Zone.GetEntityCell(flow).IsVisible=false;break;
                case "unexplored":Zone.GetEntityCell(flow).Explored=false;break;
                case "removed":Zone.RemoveEntity(flow);break;
                case "render":flow.GetPart<RenderPart>().Visible=false;break;
                case "inventory":flow.GetPart<PhysicsPart>().InInventory=Actor;break;
                case "equipped":flow.GetPart<PhysicsPart>().Equipped=Actor;break;
                case "empty":flow.GetPart<LiquidPoolPart>().Volume=0;break;
                case "negative":flow.GetPart<LiquidPoolPart>().Volume=-1;break;
                case "brine":flow.GetPart<LiquidPoolPart>().LiquidId="brine";break;
                case "acid":flow.GetPart<LiquidPoolPart>().LiquidId="acid";break;
                case "marker":flow.Tags.Remove("FlowsEast");break;
                case "physics":flow.RemovePart(flow.GetPart<PhysicsPart>());break;
                case "render-part":flow.RemovePart(flow.GetPart<RenderPart>());break;
                case "pool-part":flow.RemovePart(flow.GetPart<LiquidPoolPart>());break;
                case "sheltered":Zone.GetEntityCell(flow).IsInterior=true;break;
            }
            Tick();Assert.AreEqual("marsh",Scene.AmbienceId);Assert.AreEqual(scans,Scene.ScanCount);
            Assert.AreEqual(1,Scene.SourceCount,"the actual standing source survives independently");
        }
        [TestCase("brine")][TestCase("bog-mire")][TestCase("oil")]
        public void FlowMarkerDoesNotConvertOtherLiquidsIntoRiverWater(string liquid)
        {Pool(liquid:liquid);StillNear();Observe();Assert.AreEqual("marsh",Scene.AmbienceId);}
        [Test] public void UndergroundStillUsesCavePolicy()
        {Pool();StillNear();Observe(depth:1);Assert.AreEqual("cave",Scene.AmbienceId);}
        [Test] public void InteriorStillUsesShelterPolicy()
        {Pool();StillNear();foreach(var c in Zone.Cells)c.IsInterior=true;Observe();Assert.Null(Scene.AmbienceId);}
        [Test] public void NewlyVisibleRiverIsNotDiscoveredOnIdleTick()
        {
            var flow=Pool();Zone.GetEntityCell(flow).IsVisible=false;StillNear();Observe();Assert.AreEqual("marsh",Scene.AmbienceId);
            Zone.GetEntityCell(flow).IsVisible=true;Tick();Assert.AreEqual("marsh",Scene.AmbienceId);
            Observe();Assert.AreEqual("water",Scene.AmbienceId);
        }
        [Test] public void HiddenWorldDropsTheSourceDependentBed()
        {Pool();StillNear();Observe();Scene.Tick(.1f,false,false);Assert.Null(Scene.AmbienceId);Assert.AreEqual(0,Scene.SourceCount);}
        [Test] public void SameZoneIdGraphReplacementDoesNotCarryOldFlowReference()
        {
            Pool();StillNear();Observe();var replacement=new Zone(Zone.ZoneID);Zone.RemoveEntity(Actor);Assert.True(replacement.AddEntity(Actor,10,10));
            foreach(var c in replacement.Cells)c.Explored=c.IsVisible=true;
            Scene.Observe(replacement,Actor,BiomeType.Sodden,0,DayBand.Dawn,true);Assert.AreEqual("marsh",Scene.AmbienceId);
            Assert.AreEqual(0,Scene.SourceCount);
        }
        [Test] public void RemovedFlowIsNotReplacedByAnUnobservedMatchingId()
        {
            var flow=Pool();Observe();string id=flow.ID;Zone.RemoveEntity(flow);var replacement=Pool();replacement.ID=id;
            Tick();Assert.AreEqual("marsh",Scene.AmbienceId,"identity text is not ownership");Observe();Assert.AreEqual("water",Scene.AmbienceId);
        }
        [Test] public void OutOfRangeFlowMustNotPersistWhenActorMoves()
        {
            Pool();StillNear();Observe();Assert.True(Zone.MoveEntity(Actor,6,10));Tick();Assert.AreEqual("marsh",Scene.AmbienceId);
        }
    }
}
