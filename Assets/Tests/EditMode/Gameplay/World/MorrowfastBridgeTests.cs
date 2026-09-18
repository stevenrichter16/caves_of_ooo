using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class MorrowfastBridgeTests
    {
        [Test] public void BridgeCanBeRemovedFromTheBankAndItsWaterSupportCloses()
        {
            var f=MorrowfastTestWorld.Factory();var z=new OverworldZoneManager(f,64).GetZone(MorrowfastSceneRuntime.ZoneID);var actor=MorrowfastTestWorld.Actor(f,z);
            var spec=MorrowfastSceneDefinition.Load().FindOwner("western-footbridge");var owner=MorrowfastSceneRuntime.FindOwner(z,spec.id);
            var reachable=MorrowfastTestWorld.Flood(z,actor);
            var bank=reachable.First(c=>!spec.bridgeSupport.Any(p=>p.x==c.x&&p.y==c.y)&&spec.bridgeSupport.Any(p=>Math.Abs(p.x-c.x)+Math.Abs(p.y-c.y)==1));
            z.MoveEntity(actor,bank.x,bank.y);
            Assert.IsTrue(owner.GetPart<MorrowfastPropPart>().TryRemove(actor,z),"The unoccupied crossing must be removable from either adjacent bank.");
            Assert.IsTrue(spec.bridgeSupport.All(c=>z.GetCell(c.x,c.y).BlocksMovement(actor)));
            Assert.IsFalse(z.GetCell(bank.x,bank.y).BlocksMovement(actor));
            Assert.IsFalse(owner.GetPart<MorrowfastPropPart>().TryRemove(actor,z));
        }
    }
}
