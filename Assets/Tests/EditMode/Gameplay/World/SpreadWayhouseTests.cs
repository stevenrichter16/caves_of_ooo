using System;
using System.Reflection;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SpreadWayhouseTests
    {
        [TestCase(1)] [TestCase(64)] [TestCase(1729)]
        public void GeneratedDestinationHasOneFiniteRewardAndDistinctKeyWithBothEntrances(int seed)
        {
            using(var scope=new DensityLootTestScope())
            {
                var manager=OverworldZoneManager.CreateDetached(scope.Factory,seed);
                var zone=manager.GetZone(manager.Wayhouse.ZoneID);Assert.NotNull(zone);
                var owners=zone.GetReadOnlyEntities().ToArray();
                var anchor=owners.SingleOrDefault(e=>e.GetProperty("SpreadWayhouse.Role")=="anchor");
                Assert.NotNull(anchor,"The selected native zone must admit a real complete expedition.");
                var door=owners.Single(e=>e.GetProperty("SpreadWayhouse.Role")=="door");
                Assert.IsTrue(door.GetPart<DoorPart>().IsClosed);Assert.IsTrue(door.GetPart<LockPart>().IsLocked);
                var cache=owners.Single(e=>e.GetProperty("SpreadWayhouse.Role")=="cache");
                var reward=cache.GetPart<ContainerPart>().Contents.Single();Assert.AreEqual("Buckler",reward.BlueprintName);
                var sack=owners.Single(e=>e.GetProperty("SpreadWayhouse.Role")=="key-sack");
                var key=sack.GetPart<ContainerPart>().Contents.Single();Assert.AreEqual("IronKey",key.BlueprintName);
                Assert.AreEqual(door.GetPart<LockPart>().KeyId,key.GetPart<KeyPart>().KeyId);
                Assert.AreNotEqual("iron",key.GetPart<KeyPart>().KeyId);StringAssert.Contains("Turnbank",key.GetDisplayName());
                Assert.AreEqual(1,owners.Count(e=>e.BlueprintName=="MarlbackScrabbler"));
                Assert.IsFalse(owners.Any(e=>e.BlueprintName=="Viper"));
                manager.UnloadZone(zone.ZoneID);Assert.AreSame(zone,manager.GetZone(zone.ZoneID),"Fixed reward graph must not regenerate.");
            }
        }

        [TestCase(null)] [TestCase("")] [TestCase("2|Overworld.1.1.0")] [TestCase("1|Overworld.01.1.0")]
        public void MissingOrMalformedSelectionDoesNotBackfill(string value)
        {
            var world=new Entity();if(value!=null)world.Properties[SpreadWayhousePlan.PropertyKey]=value;
            var plan=SpreadWayhousePlan.Restore(world);Assert.IsFalse(plan.Initialized);Assert.IsEmpty(plan.ZoneID);
        }

        [Test] public void ExplicitEmptySelectionRemainsInitializedAndEmpty()
        {
            var world=new Entity();world.Properties[SpreadWayhousePlan.PropertyKey]="1|";
            var plan=SpreadWayhousePlan.Restore(world);Assert.IsTrue(plan.Initialized);Assert.IsEmpty(plan.ZoneID);
        }
        [TestCase(1)] [TestCase(64)] [TestCase(1729)]
        public void FreshWorldOwnsOneIndependentExpeditionDestinationWithoutGeneratingIt(int seed)
        {
            using(var scope=new DensityLootTestScope())
            {
                var manager=OverworldZoneManager.CreateDetached(scope.Factory,seed);
                var property=typeof(OverworldZoneManager).GetProperty("Wayhouse");
                Assert.NotNull(property,"Fresh worlds need an independent saved expedition source.");
                var plan=property.GetValue(manager);Assert.NotNull(plan);
                string id=(string)plan.GetType().GetProperty("ZoneID").GetValue(plan);
                Assert.IsNotEmpty(id,"This native seed has eligible Spread roadside ground.");
                Assert.AreNotEqual(manager.RareEncounters.PairZoneID,id);
                Assert.AreNotEqual(manager.RareEncounters.ViperZoneID,id);
                Assert.IsTrue(SpreadRareEncounterPlan.IsEligible(manager,id));
                Assert.AreEqual(0,manager.CachedZoneCount);
                var second=OverworldZoneManager.CreateDetached(scope.Factory,seed);
                Assert.AreEqual(id,(string)plan.GetType().GetProperty("ZoneID").GetValue(property.GetValue(second)));
            }
        }
    }
}
