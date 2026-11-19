using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Why a process row is not offered (Docs/MACHINE-MULTI-STACK-CLAIMS.md, spec S12a). A near-miss (carried, permitted source; exactly one failing requirement group) gets one dimmed
    /// row with the reason; nothing else does. Counters throughout: nothing missing, two things missing, a ground source and an unready rig produce no row of ours.
    /// </summary>
    public sealed class MachineProcessMissTests : FiftyWorldFixture
    {
        const string Spine = "SpurgrassSpine", Cord = "KnotflaxCord", Prefix = "MaterialProcess|serrated_kit|";
        EntityFactory oldForge;
        [SetUp] public void SetupMiss() { oldForge = ForgePart.Factory; ForgePart.Factory = Factory; MaterialProcessRegistry.ResetForTests(); }
        [TearDown] public void RestoreMiss() { ForgePart.Factory = oldForge; MaterialProcessRegistry.ResetForTests(); }

        Entity Separate(string id, int count = 1)
        {
            var e = Factory.CreateEntity(Spine); e.ID = id; if (count > 1) e.GetPart<StackerPart>().StackCount = count;
            Pack.Objects.Add(e); e.GetPart<PhysicsPart>().InInventory = Actor; return e;
        }
        InventoryAction[] Rows(Entity e, string process = "serrated_kit") => Actions(e).Where(a => a.Name == process).ToArray();
        static InventoryAction Miss(InventoryAction[] rows) => rows.SingleOrDefault(r => r.IsDisabled);

        [Test] public void OneSpineShortOfTwoExplainsHowManyYouHave()
        {
            Carry("Dagger"); var cord = Carry(Cord); Separate("a"); var row = Miss(Rows(cord));
            Assert.NotNull(row); Assert.AreEqual("short", row.DisabledCode); StringAssert.Contains("need 2", row.DisabledReason); StringAssert.Contains("have 1", row.DisabledReason);
        }

        [Test] public void NoUsableCuttingToolIsTheNearMissWhenEverythingElseIsThere()
        {
            var cord = Carry(Cord); Separate("a", 2); var row = Miss(Rows(cord)); Assert.NotNull(row); Assert.AreEqual("no-tool", row.DisabledCode);
        }

        [Test] public void EnoughSpinesThatCannotBePooledSayTheyAreNotAlike()
        {
            Carry("Dagger"); var cord = Carry(Cord); var a = Separate("a"); a.AddPart(new ThrownRecoveryPart { ThrowerId = Actor.ID }); Separate("b");
            var row = Miss(Rows(cord)); Assert.NotNull(row); Assert.AreEqual("stacks-incompatible", row.DisabledCode); StringAssert.Contains("not alike", row.DisabledReason);
        }

        [Test] public void NothingMissingMeansARealRowAndNoDisabledOne_Counter()
        {
            Carry("Dagger"); var cord = Carry(Cord); Separate("a"); Separate("b"); var rows = Rows(cord);
            Assert.AreEqual(1, rows.Length); Assert.False(rows[0].IsDisabled, "two single stacks pool, so the row is real");
        }

        [Test] public void TwoThingsMissingIsNotANearMissAndShowsNothing_Counter()
        {
            var cord = Carry(Cord); Separate("a"); Assert.IsEmpty(Rows(cord), "no knife and one spine: too far from runnable to list");
        }

        [Test] public void ASourceOnTheGroundNeverGetsADisabledRow_Counter()
        {
            Carry("Dagger"); var cord = Place(Cord); Separate("a"); Assert.IsEmpty(Rows(cord));
        }

        [Test] public void TheDisabledRowIsLowPriorityNeverDispatchesAndSpendsNothing()
        {
            Carry("Dagger"); var cord = Carry(Cord); Separate("a"); var actions = Actions(cord); var row = actions.Single(a => a.Name == "serrated_kit" && a.IsDisabled);
            Assert.LessOrEqual(row.Priority, 5); StringAssert.StartsWith(InventoryAction.DisabledCommandPrefix, row.Command);
            Assert.False(Act(cord, row.Command)); Assert.AreEqual(1, Count(Cord)); Assert.AreEqual(1, Count(Spine));
            Assert.Less(row.Priority, 19, "below the priority of a real process row, so it never becomes the menu's first entry");
        }

        [Test] public void AForgeRowWithNoForgeSaysSoAndAnUnreadyRigDoesNot_AndAReadyForgeMakesItReal()
        {
            var blank = Carry("RecoveredIronBlank"); var none = Miss(Rows(blank, "point")); Assert.NotNull(none); Assert.AreEqual("no-station", none.DisabledCode);
            var rig = Place("AbandonedFieldWorkRig"); rig.GetPart<RepairablePart>().Repaired = false; Assert.IsNull(Miss(Rows(blank, "point")), "an unready rig explains itself in its own menu");
            Zone.RemoveEntity(rig); var forge = Place("TinkersForge"); var fc = Zone.GetEntityCell(forge); fc.IsVisible = true; fc.Explored = true; var real = Rows(blank, "point"); Assert.AreEqual(1, real.Length); Assert.False(real[0].IsDisabled);
        }
    }
}
