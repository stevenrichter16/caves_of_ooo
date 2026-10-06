using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class FiftyWorldHarvestTests : FiftyWorldFixture
    {
        [Test] public void FailedFieldHarvestRestoresOriginalRowAndEveryOutput()
        {
            var row = Place("RipeCropRow"); string name = row.GetPart<RenderPart>().DisplayName; string examine = row.GetPart<ExaminablePart>().Text;
            FailAfter(); Assert.False(Act(row, "Harvest")); Assert.False(row.GetPart<FieldHarvestPart>().Harvested);
            Assert.AreEqual(name, row.GetPart<RenderPart>().DisplayName); Assert.AreEqual(examine, row.GetPart<ExaminablePart>().Text); Assert.AreEqual(0, Count("Emberwheat"));
        }
        [TestCase(false)] [TestCase(true)] public void OneFiniteRowConservesGrainEvenWithAFullPack(bool full)
        {
            var row = Place("RipeCropRow"); if (full) Pack.MaxWeight = 0;
            Assert.True(Act(row, "Harvest")); Assert.AreEqual(1, Count("Emberwheat")); Assert.True(row.GetPart<FieldHarvestPart>().Harvested);
            Assert.False(Act(row, "Harvest")); Assert.AreEqual(1, Count("Emberwheat"));
            var saved = PartRoundTripHelper.RoundTripEntityViaTokenGraph(row); Assert.True(saved.GetPart<FieldHarvestPart>().Harvested);
        }
        [TestCase(13, true)] [TestCase(14, false)] public void FieldReachUsesTheActualOccupiedBody(int anchor, bool expected)
        {
            var row = Factory.CreateEntity("RipeCropRow"); row.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;-1,0;-2,0" });
            Assert.True(Zone.AddEntity(row, anchor, 10)); Assert.AreEqual(expected, Act(row, "Harvest")); Assert.AreEqual(expected ? 1 : 0, Count("Emberwheat"));
        }
        [Test] public void CutFieldCanBecomeAPreparedBedWithoutReplacingOrRefillingItsOwner()
        {
            var row = Place("RipeCropRow"); Assert.True(Act(row, "Harvest")); Assert.True(Act(row, Choice(row, "PrepareFieldBed")));
            Assert.True(row.HasPart<CultivatedSoilPart>()); Assert.True(row.HasTag("Plantable")); Assert.True(row.GetPart<FieldHarvestPart>().Harvested);
            Assert.AreSame(row, Zone.GetCell(11, 10).Objects.Find(e => e == row)); Assert.AreEqual(1, Count("Emberwheat"));
            Assert.False(Act(row, "PrepareFieldBed")); var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(row);
            Assert.True(copy.HasPart<CultivatedSoilPart>()); Assert.True(copy.GetPart<FieldHarvestPart>().Harvested);
        }
        [TestCase("uncut")] [TestCase("flooded")] [TestCase("barren")]
        public void FieldPreparationCannotManufactureAFreeBedThroughInvalidGround(string state)
        {
            var row = Place("RipeCropRow"); if (state != "uncut") Assert.True(Act(row, "Harvest"));
            if (state == "flooded") Pool(); if (state == "barren") row.SetTag("Barren");
            Assert.False(Act(row, "PrepareFieldBed")); Assert.False(row.HasPart<CultivatedSoilPart>());
        }
    }
}
