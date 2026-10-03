using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SoddenDistrictArrivalTests
    {
        [TestCase("first")][TestCase("saved")][TestCase("removed-notice")][TestCase("legacy")]
        public void NativeMapArrivalStartsAtTheCrossingShoreWithoutMovingSavedReturns(string mode)
        {
            using(var scope=new HaulingContentScope())
            {
                scope.Seed(64);var manager=OverworldZoneManager.CreateDetached(scope.Factory,64,true);
                var target=manager.GetZone(SoddenDistrictPlan.CrossingZoneID);Assert.NotNull(target);
                if(mode=="removed-notice")target.RemoveEntity(target.GetReadOnlyEntities().Single(e=>e.BlueprintName=="SoddenRouteNotice"));
                if(mode=="legacy")SoddenDistrictIntegrationTests.Version(manager,13);
                var map=new Zone(WorldMap.WorldMapZoneID);var marker=new Entity{ID="sodden-map-cell"};
                marker.AddPart(new WorldMapCellPart{WorldX=16,WorldY=7});map.AddEntity(marker,40,12);
                var actor=scope.Factory.CreateEntity("Player");map.AddEntity(actor,40,12);
                if(mode=="saved")actor.AddPart(new WorldMapPart{LastZoneIDOnSurface=target.ZoneID,LastZoneX=61,LastZoneY=12});
                var result=WorldMapTraversal.Descend(actor,map,manager);Assert.IsTrue(result.Success,result.ErrorReason);
                Assert.AreEqual(mode=="first"?(20,12):mode=="saved"?(61,12):(40,12),target.GetEntityPosition(actor));
                if(mode=="first")Assert.IsFalse(target.GetEntityCell(actor).Objects.Any(e=>e.HasPart<LiquidPoolPart>()));
            }
        }
    }
}
