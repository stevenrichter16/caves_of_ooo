using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class MachineContainedOilContentTests
    {
        const string Blueprint="CinderholdOilRecoveryTray";
        public sealed class CreationProbe : Part
        {
            public static Entity Tray;
            public override bool HandleEvent(GameEvent e)
            {
                if(e.ID!="ObjectCreated")return true;
                var owner=ParentEntity;owner.RemovePart(this);
                if(owner.BlueprintName==Blueprint)Tray=owner;
                else if(Tray!=null)Tray.RemovePart(Tray.GetPart<ContainerPart>());
                return true;
            }
        }
        [Test] public void OptionalHeadCallbackCannotCrashTownWhenItRemovesTrayContainer()
        {
            using(var scope=new DensityLootTestScope())
            {
                scope.Factory.RegisterPartType<CreationProbe>("CreationProbe");
                scope.Factory.Blueprints[Blueprint].Parts["CreationProbe"]=new System.Collections.Generic.Dictionary<string,string>();
                scope.Factory.Blueprints["CinderhookAxeHeadComponent"].Parts["CreationProbe"]=new System.Collections.Generic.Dictionary<string,string>();
                try
                {
                    var manager=OverworldZoneManager.CreateDetached(scope.Factory,64,true);
                    var zone=manager.GetZone(CinderholdCompositionPlan.ZoneID);
                    Assert.AreEqual(1,zone.GetReadOnlyEntities().Count(e=>e.BlueprintName=="TinkersForge"));
                    Assert.False(zone.GetReadOnlyEntities().Any(e=>e.BlueprintName==Blueprint));
                }
                finally { CreationProbe.Tray=null; }
            }
        }
        [TestCase(64)] [TestCase(704860946)] [TestCase(1729)]
        public void FreshTownHasOneReleasedTrayAtRelativeWorkshopAnchor(int seed)
        {
            using(var scope=new DensityLootTestScope())
            {
                var manager=OverworldZoneManager.CreateDetached(scope.Factory,seed,true);var zone=manager.GetZone(CinderholdCompositionPlan.ZoneID);
                var tray=zone.GetReadOnlyEntities().SingleOrDefault(e=>e.BlueprintName==Blueprint);Assert.NotNull(tray);
                var room=CinderholdCompositionPlan.Create(zone.ZoneID,seed).Rooms.Single(r=>r.Role=="ForgeWorkshop");
                Assert.AreEqual((room.X+8,room.Y+3),zone.GetEntityPosition(tray));Assert.True(zone.GenReservedCells.Contains((room.X+8,room.Y+3)));
                Assert.False(zone.GetCell(room.X+8,room.Y+4).BlocksMovement());Assert.AreEqual(12,tray.GetPart<ContainedLiquidSourcePart>().Volume);
                var head=tray.GetPart<ContainerPart>().Contents.Single();Assert.AreEqual("CinderhookAxeHeadComponent",head.BlueprintName);
                Assert.AreSame(tray,head.GetPart<PhysicsPart>().InInventory);Assert.False(tray.HasPart<LiquidPoolPart>());
                Assert.AreEqual(1,zone.GetReadOnlyEntities().Count(e=>e.BlueprintName=="TinkersForge"));
                Assert.AreEqual(1,zone.GetReadOnlyEntities().Count(e=>e.BlueprintName=="SmithAnvil"));
                // This gate measures authored routes, not transient NPC positions.
                // Keep every static solid, including the tray, in the flood.
                var actors=zone.GetReadOnlyEntities().Where(e=>e.HasTag("Creature") && e.GetPart<PhysicsPart>()?.Solid==true).ToArray();
                foreach(var actor in actors)actor.GetPart<PhysicsPart>().Solid=false;
                try { CinderholdCompositionTests.AssertConnected(zone); }
                finally { foreach(var actor in actors)actor.GetPart<PhysicsPart>().Solid=true; }
            }
        }
        [Test] public void MissingOptionalTrayKeepsExistingTownAndDoesNotRetrofitCache()
        {
            using(var scope=new DensityLootTestScope())
            {
                scope.Factory.Blueprints.Remove(Blueprint);var manager=OverworldZoneManager.CreateDetached(scope.Factory,64,true);
                var zone=manager.GetZone(CinderholdCompositionPlan.ZoneID);int count=zone.EntityCount;
                Assert.AreEqual(1,zone.GetReadOnlyEntities().Count(e=>e.BlueprintName=="TinkersForge"));
                Assert.False(new CinderholdCompositionBuilder(64).BuildZone(zone,scope.Factory,new Random(64)));Assert.AreEqual(count,zone.EntityCount);
                Assert.False(zone.GetReadOnlyEntities().Any(e=>e.BlueprintName==Blueprint));
            }
        }
    }
}
