using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    /// <summary>Same actual hot source as contact, but all calls are pure prospective
    /// body queries. A hot visual/name alone must never manufacture a hazard.</summary>
    public sealed class HotSteamNavigationTests : HotSteamContactFixture
    {
        int Cost(int x=11,int y=10) => TerrainNavigationWeight.ForStep(zone,x,y,actor);
        [TestCase("hot",true)][TestCase("cool",false)][TestCase("threshold",false)]
        [TestCase("no-steam",false)][TestCase("no-thermal",false)][TestCase("expired",false)]
        [TestCase("empty",false)][TestCase("immune",false)][TestCase("resistant",true)]
        [TestCase("detached",false)][TestCase("false-owner",false)]
        public void NavigationUsesTheSameActualLiveHotSourceAdmission(string state,bool harmful)
        {
            switch(state)
            {
                case "cool": source.GetPart<ThermalPart>().Temperature=25;break;
                case "threshold": source.GetPart<ThermalPart>().Temperature=100;break;
                case "no-steam": source.RemoveEffect<SteamEffect>();break;
                case "no-thermal": source.RemovePart(source.GetPart<ThermalPart>());break;
                case "expired": steam.Duration=0;break;
                case "empty": steam.Density=0;break;
                case "immune": actor.GetStat("HeatResistance").BaseValue=100;break;
                case "resistant": actor.GetStat("HeatResistance").BaseValue=99;break;
                case "detached": zone.RemoveEntity(source);break;
                case "false-owner": steam.Owner=new Entity();break;
            }
            int cost=Cost(); Assert.AreEqual(harmful,cost>0);
            Assert.That(cost,Is.InRange(0,TerrainNavigationWeight.MaxPenalty));
        }
        [TestCase(false)][TestCase(true)]
        public void CandidatePhysicalFootRatherThanCandidateAnchorDeterminesContact(bool body)
        {
            Assert.True(zone.RemoveEntity(actor));
            actor.AddPart(new SpatialFootprintPart{CellsRaw=body?"-4,0;-4,1":"0,0"});
            Assert.True(zone.AddEntity(actor,20,10));
            Assert.AreEqual(body,Cost(15,10)>0);
        }
        [TestCase(false)][TestCase(true)]
        public void SourcePhysicalFootRatherThanRemoteAnchorDeterminesContact(bool body)
        {
            Assert.True(zone.RemoveEntity(source));
            source.AddPart(new SpatialFootprintPart{CellsRaw=body?"-4,0":"0,0"});
            Assert.True(zone.AddEntity(source,14,10));
            Assert.AreEqual(body,Cost()>0);
        }
        [Test]public void QueriesDoNotDamageWetAdvanceOrMoveEitherOwner()
        {
            int hp=actor.GetStatValue("Hitpoints");float temperature=source.GetPart<ThermalPart>().Temperature,density=steam.Density;
            var before=zone.GetEntityPosition(actor);var messages=MessageLog.GetRecent(100).ToArray();
            for(int i=0;i<100;i++)Assert.Greater(Cost(),0);
            Assert.AreEqual(hp,actor.GetStatValue("Hitpoints"));Assert.IsNull(actor.GetEffect<WetEffect>());
            Assert.AreEqual(temperature,source.GetPart<ThermalPart>().Temperature);Assert.AreEqual(density,steam.Density);
            Assert.AreEqual(before,zone.GetEntityPosition(actor));CollectionAssert.AreEqual(messages,MessageLog.GetRecent(100));
        }
        [TestCase(false)][TestCase(true)]
        public void EqualAlternativeRouteAvoidsHotContactButImmuneActorKeepsDirectRoute(bool immune)
        {
            Assert.True(zone.MoveEntity(actor,5,10));actor.GetStat("HeatResistance").BaseValue=immune?100:0;
            var path=FindPath.Search(zone,5,10,15,10,actor:actor);Assert.True(path.Usable);
            int x=5,y=10;bool touches=false;
            foreach(var step in path.Steps){x+=step.dx;y+=step.dy;if(Math.Abs(x-10)<=1&&Math.Abs(y-10)<=1)touches=true;}
            Assert.AreEqual(immune,touches);
        }
        [Test]public void SoleHazardousPassageAndEscapeRemainPossible()
        {
            Assert.True(zone.MoveEntity(actor,5,10));
            for(int x=0;x<Zone.Width;x++)for(int y=0;y<Zone.Height;y++)if(y!=10){var wall=new Entity();wall.SetTag("Solid");Assert.True(zone.AddEntity(wall,x,y));}
            var through=FindPath.Search(zone,5,10,15,10,actor:actor);Assert.True(through.Usable);Assert.AreEqual(10,through.Steps.Count);
            Assert.True(zone.MoveEntity(actor,11,10));var escape=FindPath.Search(zone,11,10,15,10,actor:actor);Assert.True(escape.Usable);Assert.AreEqual(4,escape.Steps.Count);
        }
        [Test]public void SeparateVisualCloudAndTileCloudNeverAcquireContactCost()
        {
            source.RemoveEffect<SteamEffect>();
            zone.TileState.WriteCloud(11,10,"steam",3);
            Assert.Zero(Cost());Assert.AreEqual(1,zone.GetAllEntities().Count(e=>e.BlueprintName=="SteamCloud"));
        }
    }
}
