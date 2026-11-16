using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class MachineLiquidTransferTests : FiftyWorldFixture
    {
        string Transfer(Entity from, Entity to, bool one = false) => Choice(from,
            one ? "TransferOneLiquid|" : "TransferLiquid|", a => a.Command.Split('|')[2] == Id(to));

        [TestCase("veined-pulse-mycelium")] [TestCase("felling-counter-resin")] [TestCase("choir-mirror-mucilage")]
        public void RegisteredLongLiquidPreviewsKeepAllQuantitiesWithinTheMenu(string liquid)
        {
            var from = Flask(12, liquid); Flask(0);
            var choices = Actions(from).Where(a => a.Command.StartsWith("TransferLiquid|") || a.Command.StartsWith("TransferOneLiquid|")).ToList();
            Assert.AreEqual(2, choices.Count);
            foreach (var action in choices)
            {
                Assert.LessOrEqual(action.Display.Length, 60, action.Display);
                StringAssert.Contains(LiquidRegistry.Get(liquid).DisplayName, action.Display);
                StringAssert.Contains("/12", action.Display);
            }
        }

        [TestCase("brine")] [TestCase("oil")] [TestCase("acid")] [TestCase("bog-mire")]
        public void OrdinaryMenuConservesLiquidWithoutGroundExposure(string liquid)
        {
            var from = Flask(7, liquid); var to = Flask(2, liquid, 5);
            string command = Transfer(from, to);
            Assert.True(WorldResourceActions.IsCommand(command), "Both existing UIs must charge the ordinary action");
            Assert.True(Act(from, command)); Assert.AreEqual(4, Units(from)); Assert.AreEqual(5, Units(to));
            Assert.AreEqual(liquid, to.GetPart<LiquidVesselPart>().LiquidId);
            Assert.AreEqual(0, Zone.TileState.WrittenCount); Assert.AreEqual(0, PoolUnits());
            Assert.Null(Actor.GetEffect<LiquidCoveredEffect>());
            Assert.False(Act(from, command), "A repeated preview cannot silently spend more liquid");
        }
        [Test] public void OneUnitSplitKeepsAnIndependentEmptyReceiverAndSurvivesGraphReload()
        {
            var from = Flask(6, "oil"); var to = Flask(0); var spare = Flask(0);
            Assert.True(Act(from, Transfer(from, to, true)));
            Assert.AreEqual(5, Units(from)); Assert.AreEqual(1, Units(to)); Assert.AreEqual(0, Units(spare));
            var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(Actor);
            var items = copy.GetPart<InventoryPart>().Objects;
            Assert.AreEqual(6, items.Sum(Units));
            Assert.AreEqual("oil", items.Single(e => e.ID == to.ID).GetPart<LiquidVesselPart>().LiquidId);
            Assert.AreEqual("", items.Single(e => e.ID == spare.ID).GetPart<LiquidVesselPart>().LiquidId);
        }
        [Test] public void EmptySourceRemainsReusableAndOneUnitOptionIsNotDuplicated()
        {
            var from = Flask(1, "brine"); var to = Flask(0);
            Assert.False(Actions(from).Any(a => a.Command.StartsWith("TransferOneLiquid|")));
            Assert.True(Act(from, Transfer(from, to))); Assert.AreEqual(0, Units(from));
            Assert.AreEqual("", from.GetPart<LiquidVesselPart>().LiquidId); Assert.Contains(from, Pack.Objects);
        }
        [Test] public void TransferredBrineStillHasItsOrdinaryGroundContactConsequences()
        {
            var from = Flask(6, "brine"); var to = Flask(0);
            Assert.True(Act(from, Transfer(from, to, true)));
            Assert.True(Act(to, Pour(to, true, 10, 10)));
            Assert.AreEqual(5, Units(from)); Assert.AreEqual(0, Units(to));
            Assert.AreEqual(1, PoolUnits(10, 10)); Assert.AreEqual("brine", Actor.GetEffect<LiquidCoveredEffect>()?.LiquidId);
            Assert.True(Zone.TileState.HasCoating(10, 10, "brine"));
        }
        [Test] public void ExistingWaterTransferAndWaterskinRemainTheOnlyWaterRoute()
        {
            var from = Flask(3); var to = Flask(0); var skin = Skin();
            Assert.False(Actions(from).Any(a => a.Command.StartsWith("TransferLiquid|") || a.Command.StartsWith("TransferOneLiquid|")));
            Assert.True(Act(from, "TransferWater|" + Id(skin))); Assert.AreEqual(3, Units(skin)); Assert.AreEqual(0, Units(to));
        }
        [TestCase("water")] [TestCase("acid")] [TestCase("unknown-liquid")]
        public void DifferentContentsCannotBeMixedOrRelabelled(string targetLiquid)
        {
            var from = Flask(5, "brine"); var to = Flask(2, targetLiquid);
            Assert.False(Actions(from).Any(a => a.Command.StartsWith("TransferLiquid|")));
            Assert.AreEqual(5, Units(from)); Assert.AreEqual(targetLiquid, to.GetPart<LiquidVesselPart>().LiquidId);
        }
        [TestCase("source-volume")] [TestCase("target-volume")] [TestCase("source-capacity")]
        [TestCase("target-capacity")] [TestCase("source-liquid")] [TestCase("target-liquid")]
        [TestCase("foreign")] [TestCase("equipped")] [TestCase("duplicate-id")] [TestCase("duplicate-part")]
        public void ChangedPreviewRefusesWithoutAnyFurtherMutation(string change)
        {
            var from = Flask(6, "brine"); var to = Flask(0); string command = Transfer(from, to);
            var a = from.GetPart<LiquidVesselPart>(); var b = to.GetPart<LiquidVesselPart>();
            switch (change)
            {
                case "source-volume": a.Volume--; break;
                case "target-volume": b.Volume = 1; b.LiquidId = "brine"; break;
                case "source-capacity": a.Capacity++; break;
                case "target-capacity": b.Capacity++; break;
                case "source-liquid": a.LiquidId = "oil"; break;
                case "target-liquid": b.LiquidId = "oil"; break;
                case "foreign": Pack.RemoveObject(to); break;
                case "equipped": to.GetPart<PhysicsPart>().Equipped = Actor; break;
                case "duplicate-id": Flask(0).ID = to.ID; break;
                case "duplicate-part": to.Parts.Add(new LiquidVesselPart()); break;
            }
            int av = a.Volume, bv = b.Volume; string ai = a.LiquidId, bi = b.LiquidId;
            Assert.False(Act(from, command)); Assert.AreEqual(av, a.Volume); Assert.AreEqual(bv, b.Volume);
            Assert.AreEqual(ai, a.LiquidId); Assert.AreEqual(bi, b.LiquidId);
        }
        [TestCase("source-negative")] [TestCase("source-overfull")] [TestCase("target-overfull")]
        [TestCase("empty-labelled")] [TestCase("unknown-source")] [TestCase("zero-capacity")]
        public void MalformedVesselsNeverOfferTransfers(string change)
        {
            var from = Flask(6, "brine"); var to = Flask(0);
            var a = from.GetPart<LiquidVesselPart>(); var b = to.GetPart<LiquidVesselPart>();
            if (change == "source-negative") a.Volume = -1;
            if (change == "source-overfull") a.Volume = 13;
            if (change == "target-overfull") { b.Volume = 13; b.LiquidId = "brine"; }
            if (change == "empty-labelled") b.LiquidId = "brine";
            if (change == "unknown-source") a.LiquidId = "unknown-liquid";
            if (change == "zero-capacity") b.Capacity = 0;
            Assert.False(Actions(from).Any(x => x.Command.StartsWith("TransferLiquid|") || x.Command.StartsWith("TransferOneLiquid|")));
        }
        [TestCase("before")] [TestCase("after")]
        public void NormalCommandCancellationOrExceptionRollsBackBothVessels(string when)
        {
            var from = Flask(6, "acid"); var to = Flask(0); string command = Transfer(from, to);
            Actor.AddPart(new DensityLiquidVesselAdversarialTests.Veto { When = when });
            Assert.False(Act(from, command)); Assert.AreEqual(6, Units(from)); Assert.AreEqual(0, Units(to));
            Assert.AreEqual("", to.GetPart<LiquidVesselPart>().LiquidId); Assert.AreEqual(0, Zone.TileState.WrittenCount);
        }
        [Test] public void LateReceiverCapacityChangeCannotCommitAnOverfilledFlask()
        {
            var from = Flask(6, "oil"); var to = Flask(0); string command = Transfer(from, to);
            Actor.AddPart(new MachineLiquidTransferCapacityProbe { Target = to });
            Assert.False(Act(from, command)); Assert.AreEqual(6, Units(from)); Assert.AreEqual(0, Units(to));
        }
    }
    public sealed class MachineLiquidTransferCapacityProbe : Part
    {
        public Entity Target;
        public override bool HandleEvent(GameEvent e)
        { if (e.ID == "AfterInventoryAction") Target.GetPart<LiquidVesselPart>().Capacity = 1; return true; }
    }
}
