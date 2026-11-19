using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The field work rig says why it cannot be worked (Docs/MACHINE-ACTION-ROWS.md, spec S4 adapter on the S3 mechanism). A rig that is not ready
    /// lists no craft rows; someone standing beside it now gets one disabled row with the reason instead of nothing. Every positive case has its
    /// counter: a ready rig, a rig out of reach, and a forge that is not a rig add no such row.
    /// </summary>
    public sealed class MachineRigDisabledRowTests : FiftyWorldFixture
    {
        const string Blueprint = "AbandonedFieldWorkRig";
        EntityFactory oldForge;
        [SetUp] public void SetupRig() { oldForge = ForgePart.Factory; ForgePart.Factory = Factory; RepairRecipeRegistry.ResetForTests(); Diag.ResetAll(); MessageLog.Clear(); }
        [TearDown] public void RestoreRig() { ForgePart.Factory = oldForge; RepairRecipeRegistry.ResetForTests(); }

        Entity Rig(bool repaired, int x = 11, int y = 10)
        {
            var e = Factory.Blueprints.ContainsKey(Blueprint) ? Factory.CreateEntity(Blueprint) : null;
            Assert.NotNull(e, "the authored rig blueprint ships");
            e.GetPart<RepairablePart>().Repaired = repaired; Assert.True(Zone.AddEntity(e, x, y)); return e;
        }
        static InventoryAction Unavailable(InventoryAction[] rows) => rows.SingleOrDefault(r => r.Name == "RigUnavailable");

        [Test] public void AFouledRigBesideYouExplainsWhyItCannotBeWorked()
        {
            var rig = Rig(false); var row = Unavailable(Actions(rig));
            Assert.NotNull(row); Assert.True(row.IsDisabled); Assert.AreEqual(StationReadiness.Faulted, row.DisabledCode);
            StringAssert.Contains("Repair it", row.DisabledReason); StringAssert.StartsWith(InventoryAction.DisabledCommandPrefix, row.Command);
            Assert.True(Actions(rig).Any(a => a.Command == RepairablePart.RepairCommand), "the real repair row is still offered");
            Assert.False(Actions(rig).Any(a => a.Command == "CraftKit"), "and the faulted rig still has no craft menu");
        }

        [Test] public void TheExplanationSortsAfterTheUsableRowsSoTheMenuOpensOnSomethingUsable()
        {
            var rig = Rig(false); var rows = Actions(rig); Assert.False(rows[0].IsDisabled, "the first row, where the cursor starts, is usable");
            Assert.True(rows.ToList().IndexOf(Unavailable(rows)) > rows.ToList().FindIndex(r => r.Command == RepairablePart.RepairCommand));
        }

        [Test] public void ARepairedRigListsItsCraftRowsAndNoDisabledRow_Control()
        {
            var rig = Rig(true); Assert.True(ForgePart.IsNearForge(Actor, Zone));
            Assert.Null(Unavailable(Actions(rig))); Assert.True(Actions(rig).Any(a => a.Command == "CraftKit"));
        }

        [Test] public void AFouledRigOutOfReachAddsNoRow_Control()
        {
            var rig = Rig(false, x: 16, y: 10); Assert.Null(Unavailable(Actions(rig)));
        }

        [Test] public void AnOrdinaryForgeNeverGetsTheRigRow_Control()
        {
            var forge = Place("TinkersForge"); Assert.Null(Unavailable(Actions(forge)));
        }

        [Test] public void ADraggedRigSaysItIsBeingDragged()
        {
            var rig = Rig(true); rig.AddPart(new DraggedPart());
            var row = Unavailable(Actions(rig)); Assert.NotNull(row); Assert.AreEqual(StationReadiness.Hauled, row.DisabledCode); StringAssert.Contains("dragged", row.DisabledReason);
        }

        [Test] public void ADismantledRigSaysItWasTakenApart()
        {
            var rig = Rig(true); rig.GetPart<HarvestablePart>().Harvested = true;
            var row = Unavailable(Actions(rig)); Assert.NotNull(row); Assert.AreEqual(StationReadiness.Harvested, row.DisabledCode);
        }

        [Test] public void ADeadActorIsNotToldAnythingAndNothingThrows()
        {
            var rig = Rig(false); Zone.RemoveEntity(Actor);
            Assert.DoesNotThrow(() => Actions(rig)); Assert.Null(Unavailable(Actions(rig)));
        }

        [TestCase(StationReadiness.Faulted)] [TestCase(StationReadiness.Hauled)] [TestCase(StationReadiness.Harvested)] [TestCase(StationReadiness.Destroyed)]
        [TestCase("rig-payload")] [TestCase("nonsense")] [TestCase(null)]
        public void EveryReasonIsAOneSentenceWithAFallback(string code)
        {
            string reason = ForgePart.RigUnavailableReason(code); Assert.False(string.IsNullOrWhiteSpace(reason)); Assert.True(reason.EndsWith("."));
        }

        [Test] public void ActingOnTheDisabledRowCannotRunAnything()
        {
            var rig = Rig(false); var row = Unavailable(Actions(rig));
            Assert.False(Act(rig, row.Command), "the sentinel command is never handled, so even a forced dispatch commits nothing");
            Assert.AreEqual(0, Count("SalvagedTimber"));
        }
    }
}
