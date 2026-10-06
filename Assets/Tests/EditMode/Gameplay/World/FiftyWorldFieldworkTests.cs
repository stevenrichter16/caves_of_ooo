using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class FiftyWorldFieldworkTests : FiftyWorldFixture
    {
        [Test] public void AdjacentPlantingUsesTheSelectedPreparedBedAndLeavesThePlayerInPlace()
        {
            Bed(); var seed = Seed(); string command = Choice(seed, "PlantSeedAt|", a => a.Command.EndsWith("|11|10"));
            Assert.True(Act(seed, command)); Assert.AreEqual((10, 10), Zone.GetEntityPosition(Actor));
            Assert.AreEqual(1, Zone.GetCell(11, 10).Objects.Count(e => e.HasPart<CropPart>())); Assert.AreEqual(0, Count("CandyCarrotSeed"));
        }
        [Test] public void AdjacentPlantingRechecksTheSelectedBedAfterMenuOpening()
        {
            var bed = Bed(); var seed = Seed(); string command = Choice(seed, "PlantSeedAt|", a => a.Command.EndsWith("|11|10"));
            Zone.RemoveEntity(bed); Assert.False(Act(seed, command)); Assert.AreEqual(1, Count("CandyCarrotSeed")); Assert.AreEqual(0, Count("CandyCarrotCrop"));
        }
        [Test] public void OriginalUnderfootPlantingStillConsumesExactlyOneSeed()
        { Bed(10, 10); var seed = Seed(); Assert.True(Act(seed, "PlantSeed")); Assert.AreEqual(1, Zone.GetCell(10, 10).Objects.Count(e => e.HasPart<CropPart>())); Assert.AreEqual(0, Count("CandyCarrotSeed")); }
        [TestCase(0)] [TestCase(1)] public void ClearingYoungCropKeepsItsBedAndReturnsNoSeedOrProduce(int stage)
        {
            var crop = Crop(stage); var bed = Zone.GetCell(11, 10).Objects.Single(e => e.HasPart<CultivatedSoilPart>());
            Assert.True(Act(crop, Choice(crop, "ClearCrop"))); Assert.Null(Zone.GetEntityCell(crop)); Assert.NotNull(Zone.GetEntityCell(bed));
            Assert.AreEqual(0, Count("CandyCarrot")); Assert.AreEqual(0, Count("CandyCarrotSeed"));
            var seed = Seed(); Assert.True(Act(seed, Choice(seed, "PlantSeedAt|", a => a.Command.EndsWith("|11|10"))));
        }
        [TestCase("ripe")] [TestCase("remote")] [TestCase("unprepared")]
        public void ClearRefusesRipeRemoteOrUnpreparedCrop(string refusal)
        {
            var crop = Crop(refusal == "ripe" ? 2 : 0, prepared: refusal != "unprepared"); if (refusal == "remote") Zone.MoveEntity(Actor, 20, 10);
            Assert.False(Actions(crop).Any(a => a.Command == "ClearCrop")); Assert.False(Act(crop, "ClearCrop")); Assert.NotNull(Zone.GetEntityCell(crop));
        }
        [Test] public void ClearRollbackRestoresExactPhysicalCrop()
        { var crop = Crop(); var cell = Zone.GetEntityCell(crop); FailAfter(); Assert.False(Act(crop, "ClearCrop")); Assert.AreSame(cell, Zone.GetEntityCell(crop)); Assert.AreSame(crop, cell.Objects.Single(e => e.HasPart<CropPart>())); }
        [TestCase(false)] [TestCase(true)] public void GatherPacksRealYieldAndSeedOrLeavesConservedOverflow(bool full)
        {
            var crop = Crop(2); if (full) Pack.MaxWeight = 0;
            Assert.True(Act(crop, Choice(crop, "GatherCrop"))); Assert.Null(Zone.GetEntityCell(crop));
            Assert.AreEqual(2, Count("CandyCarrot")); Assert.AreEqual(1, Count("CandyCarrotSeed"));
            // The authored seed weighs zero and fits even when no weight remains.
            Assert.AreEqual(full ? 1 : 3, Pack.Objects.Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1));
            Assert.False(Act(crop, "GatherCrop")); Assert.AreEqual(2, Count("CandyCarrot"));
        }
        [Test] public void GatherRollbackRestoresSourceAndRemovesAllStagedProducts()
        { var crop = Crop(2); FailAfter(); Assert.False(Act(crop, "GatherCrop")); Assert.NotNull(Zone.GetEntityCell(crop)); Assert.AreEqual(0, Count("CandyCarrot")); Assert.AreEqual(0, Count("CandyCarrotSeed")); }
        [Test] public void OriginalHarvestStillLeavesYieldOnTheGround()
        { var crop = Crop(2); Assert.True(Act(crop, "HarvestCultivatedCrop")); Assert.AreEqual(2, Count("CandyCarrot")); Assert.AreEqual(1, Count("CandyCarrotSeed")); Assert.IsEmpty(Pack.Objects); }
    }
}
