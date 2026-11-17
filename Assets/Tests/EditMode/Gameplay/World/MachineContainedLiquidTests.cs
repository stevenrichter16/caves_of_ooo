using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class MachineContainedLiquidTests : FiftyWorldFixture
    {
        const string Recovery = "RecoverThroughLiquid|";
        Entity Tray(int volume = 12)
        {
            var tray = Factory.CreateEntity("PhysicalObject");
            tray.GetPart<PhysicsPart>().Solid = true;
            tray.GetPart<PhysicsPart>().Takeable = false;
            tray.AddPart(new ContainerPart());
            var type = typeof(Part).Assembly.GetType("CavesOfOoo.Core.ContainedLiquidSourcePart");
            Assert.NotNull(type, "A contained source must not borrow ground-pool projection/contact semantics.");
            var part = (Part)Activator.CreateInstance(type);
            type.GetField("LiquidId").SetValue(part, "oil");
            type.GetField("Volume").SetValue(part, volume);
            type.GetField("Capacity").SetValue(part, 12);
            tray.AddPart(part);
            Assert.True(tray.GetPart<ContainerPart>().AddItem(Factory.CreateEntity("CinderhookAxeHeadComponent")));
            Assert.True(Zone.AddEntity(tray, 11, 10));
            Zone.GetCell(11, 10).IsVisible = Zone.GetCell(11, 10).Explored = true;
            return tray;
        }
        static int Volume(Entity tray) => (int)tray.GetPart("ContainedLiquidSource").GetType().GetField("Volume").GetValue(tray.GetPart("ContainedLiquidSource"));
        static Entity Head(Entity tray) => tray.GetPart<ContainerPart>().Contents.Single();
        static string Fill(Entity tray) => "FillLiquidVessel|" + Id(tray) + "|oil";
        static string Reach(Entity head) => Recovery + Id(head) + "|" + Id(head.GetPart<PhysicsPart>().InInventory) + "|oil|12";

        [Test] public void TwoLimitedReceiversConserveTwelveUnitsAndUnlockTheSameHead()
        {
            var tray = Tray(); var head = Head(tray); var a = Flask(6, "oil"); var b = Flask(0);
            Assert.True(Actions(a).Any(x => x.Command == Fill(tray)));
            Assert.True(Act(a, Fill(tray))); Assert.AreEqual(6, Volume(tray)); Assert.AreEqual(12, Units(a));
            Assert.False(InventorySystem.TakeFromContainer(Actor, tray, head));
            Assert.True(Act(b, Fill(tray))); Assert.AreEqual(0, Volume(tray)); Assert.AreEqual(6, Units(b));
            Assert.True(InventorySystem.TakeFromContainer(Actor, tray, head)); Assert.True(Pack.Objects.Contains(head));
            Assert.False(InventorySystem.TakeFromContainer(Actor, tray, head)); Assert.Null(Actor.GetEffect<LiquidCoveredEffect>());
        }
        [Test] public void FullFlaskDrawLeavesEmptyTrayAndOutsideOilStillUsesGroundRules()
        {
            var tray = Tray(); var flask = Flask(0);
            Assert.True(Act(flask, Fill(tray))); Assert.AreEqual(12, Units(flask)); Assert.AreEqual(0, Volume(tray));
            Assert.False(Zone.TileState.HasCoating(11, 10, "oil")); Assert.True(Zone.GetCell(11, 10).BlocksMovement());
            Assert.True(Act(flask, Pour(flask, false, 10, 11))); Assert.AreEqual(12, PoolUnits(10, 11));
            Assert.True(Zone.TileState.HasCoating(10, 11, "oil")); Assert.AreEqual(0, Units(flask));
        }
        [TestCase("water", 1)] [TestCase("oil", 12)]
        public void IncompatibleOrFullFlaskCannotDraw(string liquid, int amount)
        {
            var tray = Tray(); var flask = Flask(amount, liquid);
            Assert.False(Act(flask, Fill(tray))); Assert.AreEqual(amount, Units(flask)); Assert.AreEqual(12, Volume(tray));
        }
        [TestCase("open")] [TestCase("take")] [TestCase("all")] [TestCase("direct")]
        public void OrdinaryContainerPathsCannotBypassWetRecovery(string path)
        {
            var tray = Tray(); var head = Head(tray);
            if(path == "open") { Assert.False(Actions(tray).Any(a => a.Command == "OpenContainer")); Assert.False(Act(tray, "OpenContainer")); }
            if(path == "take") Assert.False(InventorySystem.TakeFromContainer(Actor, tray, head));
            if(path == "all") Assert.AreEqual(0, InventorySystem.TakeAllFromContainer(Actor, tray));
            if(path == "direct") Assert.False(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(tray, head), Actor, Zone).Success);
            Assert.AreSame(head, Head(tray)); Assert.False(Pack.Objects.Contains(head)); Assert.Null(Actor.GetEffect<LiquidCoveredEffect>());
        }
        [TestCase(0)] [TestCase(12)] public void DepositsAreRefusedEvenAfterDrainage(int amount)
        {
            var tray = Tray(amount); var carried = Carry("OakHaftComponent");
            Assert.False(InventorySystem.ExecuteCommand(new PutInContainerCommand(tray, carried), Actor, Zone).Success);
            Assert.True(Pack.Objects.Contains(carried)); Assert.AreEqual(1, tray.GetPart<ContainerPart>().Contents.Count);
        }
        [Test] public void ExplicitWetRecoveryTransfersExistingHeadThenExposesWithoutConsumingLiquid()
        {
            var tray = Tray(); var head = Head(tray); Assert.AreEqual(Reach(head), Choice(tray, Recovery));
            Assert.True(Act(tray, Reach(head))); Assert.True(Pack.Objects.Contains(head)); Assert.IsEmpty(tray.GetPart<ContainerPart>().Contents);
            Assert.AreEqual(12, Volume(tray)); Assert.AreEqual("oil", Actor.GetEffect<LiquidCoveredEffect>()?.LiquidId);
            Assert.False(Act(tray, Reach(head))); Assert.AreEqual(1, Pack.Objects.Count(x => x == head));
        }
        [TestCase("weight")] [TestCase("outer")] public void FailedWetTransferHasNoExposureOrLostHead(string failure)
        {
            var tray = Tray(); var head = Head(tray); if(failure == "weight") Pack.MaxWeight = 0; else FailAfter();
            Assert.False(Act(tray, Reach(head))); Assert.AreSame(head, Head(tray)); Assert.False(Pack.Objects.Contains(head));
            Assert.Null(Actor.GetEffect<LiquidCoveredEffect>()); Assert.AreEqual(12, Volume(tray));
        }
        [Test] public void FillOuterFailureRollsBackBothVolumes()
        {
            var tray = Tray(); var flask = Flask(0); FailAfter(); Assert.False(Act(flask, Fill(tray)));
            Assert.AreEqual(12, Volume(tray)); Assert.AreEqual(0, Units(flask)); Assert.AreEqual("", flask.GetPart<LiquidVesselPart>().LiquidId);
        }
        [Test] public void SupportingGroundFilmDoesNotChangeContainedLiquidIdentityOrGetErased()
        {
            Zone.TileState.WriteCoating(11, 10, "acid", 6); var tray = Tray(); var flask = Flask(0);
            Assert.False(tray.HasPart<LiquidPoolPart>()); Assert.True(Act(flask, Fill(tray)));
            Assert.AreEqual("oil", flask.GetPart<LiquidVesselPart>().LiquidId); Zone.RemoveEntity(tray);
            Assert.True(Zone.TileState.HasCoating(11, 10, "acid"));
        }
        [Test] public void TokenSavePreservesFiniteQuantityAndOneHeadIdentity()
        {
            var tray = Tray(); var head = Head(tray); var flask = Flask(6, "oil"); Assert.True(Act(flask, Fill(tray)));
            var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(tray); Assert.AreEqual(6, Volume(copy));
            Assert.AreEqual(tray.ID, copy.ID); Assert.AreEqual(head.ID, Head(copy).ID);
            Assert.AreSame(copy, Head(copy).GetPart<PhysicsPart>().InInventory);
        }
    }
}
