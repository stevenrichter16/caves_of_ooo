using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class FiftyWorldLiquidTests : FiftyWorldFixture
    {
        [TestCase(false)] [TestCase(true)] public void CarriedWaterTransferConservesBothOwnersAndCapsDestination(bool fromFlask)
        {
            var from = fromFlask ? Flask() : Skin(3); var to = fromFlask ? Skin(0, 2) : Flask(0, capacity: 2);
            string command = Choice(from, "TransferWater|", a => a.Command == "TransferWater|" + Id(to));
            Assert.True(Act(from, command)); Assert.AreEqual(1, Units(from)); Assert.AreEqual(2, Units(to));
            var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(Actor);
            Assert.AreEqual(3, copy.GetPart<InventoryPart>().Objects.Sum(Units));
            Assert.False(Act(from, command)); Assert.AreEqual(3, Units(from) + Units(to));
        }
        [Test] public void EmptyingAFlaskIntoAnotherVesselKeepsTheReusableOwner()
        {
            var from = Flask(1); var to = Skin(); Assert.True(Act(from, Choice(from, "TransferWater|", a => a.Command == "TransferWater|" + Id(to))));
            Assert.AreEqual(0, Units(from)); Assert.AreEqual("", from.GetPart<LiquidVesselPart>().LiquidId); Assert.Contains(from, Pack.Objects); Assert.AreEqual(1, Units(to));
        }
        [TestCase("acid")] [TestCase("foreign")] [TestCase("self")]
        public void WaterTransferRefusesUnsafeOrUnownedDestination(string refusal)
        {
            var from = Flask(3, refusal == "acid" ? "acid" : "water"); var to = Skin();
            if (refusal == "foreign") Pack.RemoveObject(to); if (refusal == "self") to = from;
            Assert.False(Act(from, "TransferWater|" + Id(to))); Assert.AreEqual(3, Units(from));
        }
        [Test] public void WaterTransferJoinsOuterRollback()
        { var from = Flask(); var to = Skin(); FailAfter(); Assert.False(Act(from, "TransferWater|" + Id(to))); Assert.AreEqual(3, Units(from)); Assert.AreEqual(0, Units(to)); }
        [TestCase("water")] [TestCase("oil")] public void MeasuredPourSpendsOneActualUnitAndKeepsTheRest(string liquid)
        { var flask = Flask(3, liquid); Assert.True(Act(flask, Pour(flask, true))); Assert.AreEqual(2, Units(flask)); Assert.AreEqual(1, PoolUnits()); Assert.True(Zone.TileState.HasCoating(11, 10, liquid)); }
        [Test] public void PourAllRemainsAvailableAndSpendsTheWholePayload()
        { var flask = Flask(); Assert.True(Act(flask, Pour(flask, false))); Assert.AreEqual(0, Units(flask)); Assert.AreEqual(3, PoolUnits()); }
        [Test] public void MeasuredPourRejectsAStaleVolumeSelection()
        { var flask = Flask(); string command = Pour(flask, true); flask.GetPart<LiquidVesselPart>().Volume = 2; Assert.False(Act(flask, command)); Assert.AreEqual(2, Units(flask)); Assert.AreEqual(0, PoolUnits()); }
        [Test] public void MeasuredPourRollsBackVolumePoolAndCoatingTogether()
        { var flask = Flask(); string command = Pour(flask, true); FailAfter(); Assert.False(Act(flask, command)); Assert.AreEqual(3, Units(flask)); Assert.AreEqual(0, PoolUnits()); Assert.False(Zone.TileState.HasCoating(11, 10, "water")); }
        [TestCase(1)] [TestCase(3)] public void PouringWaterIntoPreparedCropTransfersOneUnitToMoisture(int amount)
        {
            var crop = Crop(); var flask = Flask(amount); Assert.True(Act(flask, Pour(flask, false)));
            Assert.AreEqual(40, crop.GetPart<CropPart>().MoistureTicks); Assert.AreEqual(0, Units(flask)); Assert.AreEqual(amount - 1, PoolUnits());
            Assert.AreEqual(amount == 1, CultivatedSoilPart.IsCultivated(Zone, Zone.GetCell(11, 10)));
        }
        [TestCase("ripe")] [TestCase("wet")] [TestCase("unprepared")] [TestCase("acid")]
        public void UnusableCropDoesNotSilentlyConsumeAnIrrigationUnit(string condition)
        {
            var crop = Crop(condition == "ripe" ? 2 : 0, condition == "wet" ? 40 : 0, prepared: condition != "unprepared");
            var flask = Flask(1, condition == "acid" ? "acid" : "water"); Assert.True(Act(flask, Pour(flask, false)));
            Assert.AreEqual(condition == "wet" ? 40 : 0, crop.GetPart<CropPart>().MoistureTicks); Assert.AreEqual(1, PoolUnits());
        }
        [Test] public void PouredIrrigationRollsBackWithTheActualPourReceipt()
        { var crop = Crop(); var flask = Flask(1); FailAfter(); Assert.False(Act(flask, Pour(flask, false))); Assert.AreEqual(0, crop.GetPart<CropPart>().MoistureTicks); Assert.AreEqual(1, Units(flask)); Assert.AreEqual(0, PoolUnits()); }
        [TestCase("cold", false)] [TestCase("frozen", false)] [TestCase("ice", false)]
        [TestCase("cold", true)] [TestCase("frozen", true)] [TestCase("ice", true)]
        public void NaturalFrozenWaterCannotBeDrawnUntilItsActualColdStateIsCleared(string state, bool flask)
        {
            var source = Place("WaterPuddle"); var vessel = flask ? Flask(0) : Skin(); int original = source.GetPart<LiquidPoolPart>().Volume;
            if (state == "cold") source.GetPart<ThermalPart>().Temperature = -10;
            else if (state == "frozen") source.ApplyEffect(new FrozenEffect(1)); else Zone.TileState.WriteCoating(11, 10, "ice", 5);
            string command = flask ? "FillLiquidVessel|" + Id(source) + "|water" : "FillWaterskin";
            Assert.False(Act(vessel, command)); Assert.AreEqual(0, Units(vessel)); Assert.AreEqual(original, source.GetPart<LiquidPoolPart>().Volume);
        }
        [TestCase(false)] [TestCase(true)] public void WarmFiniteWaterStillFillsWithoutCreatingVolume(bool flask)
        {
            var source = Place("WaterPuddle"); var vessel = flask ? Flask(0) : Skin(); int original = source.GetPart<LiquidPoolPart>().Volume;
            Assert.True(Act(vessel, flask ? "FillLiquidVessel|" + Id(source) + "|water" : "FillWaterskin")); Assert.AreEqual(original, Units(vessel) + source.GetPart<LiquidPoolPart>().Volume);
        }
    }
}
