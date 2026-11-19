using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// "Works here" (Docs/MACHINE-READERS.md): a forge or bench says what cold work it supports, from the shipped process data, so a player learns it before carrying a find.
    /// </summary>
    public sealed class StationReaderTests : FiftyWorldFixture
    {
        EntityFactory oldForge;
        [SetUp] public void SetupReader() { oldForge = ForgePart.Factory; ForgePart.Factory = Factory; MaterialProcessRegistry.ResetForTests(); }
        [TearDown] public void RestoreReader() { ForgePart.Factory = oldForge; MaterialProcessRegistry.ResetForTests(); }

        List<string> Lines(Entity e, bool full) { var into = new List<string>(); StateReader.Collect(e, full, Zone, Zone.GetEntityCell(e), Actor, into); return into; }

        [Test] public void AForgeNamesEveryColdWorkProcessAndWhatItNeedsInBothReaders()
        {
            var forge = Place("TinkersForge");
            foreach (bool full in new[] { false, true })
            {
                var line = Lines(forge, full).Single(l => l.StartsWith("Cold work here:"));
                foreach (var d in MaterialProcessRegistry.ForWorkKind("ForgeCold")) StringAssert.Contains(d.ActionText, line);
                StringAssert.Contains("released extraction tool", line); StringAssert.Contains("recovered iron blank", line); StringAssert.Contains("in your pack", line);
            }
        }

        [Test] public void HandCuttingRowsAreNotListedAtAForge_Counter()
        {
            var line = Lines(Place("TinkersForge"), false).Single(l => l.StartsWith("Cold work here:"));
            foreach (var d in MaterialProcessRegistry.ForWorkKind("HandCutting")) StringAssert.DoesNotContain(d.ActionText, line);
        }

        [Test] public void TheCampBenchAndTheRigSayTheSameBecauseItIsTheSamePart()
        {
            var bench = Place("CampBench", 12, 10); var rig = Place("AbandonedFieldWorkRig", 13, 10);
            Assert.AreEqual(Lines(Place("TinkersForge", 14, 10), false).Single(l => l.StartsWith("Cold work here:")), Lines(bench, false).Single(l => l.StartsWith("Cold work here:")));
            Assert.True(Lines(rig, true).Any(l => l.StartsWith("Cold work here:")));
        }

        [Test] public void NoLoadedRowsMeansNoLineAndAnObjectWithoutAForgeHasNone_Counter()
        {
            MaterialProcessRegistry.InitializeFromJson("{\"Processes\":[]}"); Assert.IsNull(ForgePart.ColdWorkLine(Factory)); Assert.IsEmpty(Lines(Place("TinkersForge"), false).Where(l => l.StartsWith("Cold work here:")));
            Assert.IsEmpty(Lines(Place("Chest", 15, 10), false));
        }
    }
}
