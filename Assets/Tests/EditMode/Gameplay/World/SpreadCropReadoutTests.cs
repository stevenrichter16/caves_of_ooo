using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SpreadCropReadoutTests
    {
        DensityLootTestScope scope;
        Zone zone, previous;
        Entity crop;

        [SetUp] public void Setup()
        {
            scope = new DensityLootTestScope();
            zone = new Zone("Overworld.11.8.0");
            previous = SettlementRuntime.ActiveZone;
            SettlementRuntime.ActiveZone = zone;
            crop = scope.Factory.CreateEntity("CandyCarrotCrop");
            Assert.True(zone.AddEntity(crop, 12, 10));
            zone.GetCell(12, 10).IsVisible = true;
        }
        [TearDown] public void Cleanup()
        {
            SettlementRuntime.ActiveZone = previous;
            scope.Dispose();
        }
        string Read(Zone source = null, Cell cell = null) => crop.GetPart<ExaminablePart>()
            .BuildWorldExamineLine(source ?? zone, cell ?? zone.GetCell(12, 10));

        [Test] public void DrySeedAndWetSproutDescribeDifferentCurrentActionsWithoutAdvancingGrowth()
        {
            var part = crop.GetPart<CropPart>();
            string dry = Read();
            Assert.That(dry, Does.Contain("Growth: seed"));
            Assert.That(dry, Does.Contain("dry; growth is paused"));
            Assert.That(dry, Does.Contain("Conjure Rain"));
            Assert.That(dry, Does.Contain("pick up"));
            Assert.That(dry, Does.Contain("At maturity, produce falls here to pick up."));
            Assert.AreEqual(0, part.MoistureTicks);
            Assert.AreEqual(0, part.TicksInStage);
            part.GrowthStage = 1;
            part.Water(40);
            string wet = Read();
            Assert.That(wet, Does.Contain("Growth: sprout"));
            Assert.That(wet, Does.Contain("moist; growth continues"));
            Assert.That(wet, Does.Not.Contain("growth is paused"));
            Assert.AreEqual(40, part.MoistureTicks);
            Assert.AreEqual(0, part.TicksInStage);
            part.MoistureTicks = 0;
            part.OnDriedOut();
            Assert.That(Read(), Does.Contain("dry; growth is paused"));
        }

        [TestCase("hidden")]
        [TestCase("inactive")]
        [TestCase("wrong-cell")]
        [TestCase("removed")]
        [TestCase("foreign-part")]
        [TestCase("invalid-stage")]
        public void UnavailableCropDoesNotAdvertiseCurrentGrowth(string state)
        {
            Assert.That(Read(), Does.Contain("Growth:"));
            if (state == "hidden") zone.GetCell(12, 10).IsVisible = false;
            if (state == "inactive") SettlementRuntime.ActiveZone = new Zone("Overworld.13.10.0");
            if (state == "removed") zone.RemoveEntity(crop);
            if (state == "foreign-part") crop.GetPart<CropPart>().ParentEntity = scope.Factory.CreateEntity("CandyCarrotCrop");
            if (state == "invalid-stage") crop.GetPart<CropPart>().GrowthStage = 2;
            Assert.That(Read(cell: state == "wrong-cell" ? zone.GetCell(13, 10) : null), Does.Not.Contain("Growth:"));
        }
    }
}
