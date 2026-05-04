using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// ST.5 — StatShifter helper unit tests. Pins the SetStatShift /
    /// RemoveStatShifts contract in isolation from BaseSkillPart so
    /// regressions in the helper surface here, not in the concrete-skill
    /// integration tests.
    /// </summary>
    public class StatShifterTests
    {
        private static Entity MakeEntityWithStat(string statName, int initialValue, int min = -50, int max = 200)
        {
            var e = new Entity { ID = "tgt", BlueprintName = "TestTarget" };
            e.AddPart(new RenderPart { DisplayName = "target" });
            e.Statistics[statName] = new Stat
            { Owner = e, Name = statName, BaseValue = initialValue, Min = min, Max = max };
            return e;
        }

        // ====================================================================
        // 1. Positive: SetStatShift adds amount to BaseValue + tracks
        // ====================================================================

        [Test]
        public void SetStatShift_AddsAmountAndTracks()
        {
            var entity = MakeEntityWithStat("DV", initialValue: 5);
            var shifter = new StatShifter(entity);

            bool ok = shifter.SetStatShift("DV", 2);

            Assert.IsTrue(ok, "SetStatShift should succeed when stat exists.");
            Assert.AreEqual(7, entity.GetStatValue("DV"),
                "BaseValue should be 5 + 2 = 7 after the shift.");
            Assert.IsTrue(shifter.HasStatShifts());
            Assert.AreEqual(2, shifter.ActiveShifts["DV"]);
        }

        // ====================================================================
        // 2. Counter-check: SetStatShift on missing stat returns false cleanly
        // ====================================================================

        [Test]
        public void SetStatShift_MissingStat_ReturnsFalseDoesNotMutate()
        {
            var entity = MakeEntityWithStat("DV", initialValue: 5);
            var shifter = new StatShifter(entity);

            bool ok = shifter.SetStatShift("NonExistentStat", 2);

            Assert.IsFalse(ok, "SetStatShift on missing stat should return false.");
            Assert.IsFalse(shifter.HasStatShifts(),
                "No shift should be tracked for a missing stat.");
            Assert.AreEqual(5, entity.GetStatValue("DV"),
                "Other stats should be untouched.");
        }

        // ====================================================================
        // 3. RemoveStatShifts rolls back applied shifts
        // ====================================================================

        [Test]
        public void RemoveStatShifts_RollsBackToPreShiftBaseValue()
        {
            var entity = MakeEntityWithStat("DV", initialValue: 5);
            var shifter = new StatShifter(entity);
            shifter.SetStatShift("DV", 2);
            Assert.AreEqual(7, entity.GetStatValue("DV"), "Precondition: shift applied.");

            shifter.RemoveStatShifts();

            Assert.AreEqual(5, entity.GetStatValue("DV"),
                "BaseValue should be back to original 5 after rollback.");
            Assert.IsFalse(shifter.HasStatShifts(),
                "ActiveShifts should be cleared after RemoveStatShifts.");
        }

        // ====================================================================
        // 4. Idempotent-replace: same-amount second call doesn't double-apply
        // ====================================================================

        [Test]
        public void SetStatShift_CalledTwiceWithSameAmount_NetsToOneShift()
        {
            var entity = MakeEntityWithStat("DV", initialValue: 5);
            var shifter = new StatShifter(entity);

            shifter.SetStatShift("DV", 2);
            shifter.SetStatShift("DV", 2);

            Assert.AreEqual(7, entity.GetStatValue("DV"),
                "Two SetStatShift(stat, 2) calls should net to a single +2 shift " +
                "(idempotent-replace), not stack to +4.");
            Assert.AreEqual(1, shifter.ActiveShifts.Count,
                "Only one shift entry should be tracked, not two.");
        }

        // ====================================================================
        // 5. Replacing a shift with a different amount applies the delta
        // ====================================================================

        [Test]
        public void SetStatShift_DifferentAmount_AppliesNewAmount()
        {
            var entity = MakeEntityWithStat("DV", initialValue: 5);
            var shifter = new StatShifter(entity);
            shifter.SetStatShift("DV", 2);

            shifter.SetStatShift("DV", 4);

            Assert.AreEqual(9, entity.GetStatValue("DV"),
                "5 + 4 = 9. Replace must undo +2 first, then apply +4 — " +
                "NOT stack to +6 (5+2+4).");
            Assert.AreEqual(4, shifter.ActiveShifts["DV"],
                "ActiveShifts should track the new amount, not the old.");
        }

        // ====================================================================
        // 6. RemoveStatShifts on no-shifts is a clean no-op (counter-check)
        // ====================================================================

        [Test]
        public void RemoveStatShifts_NoActiveShifts_DoesNotCrash()
        {
            var entity = MakeEntityWithStat("DV", initialValue: 5);
            var shifter = new StatShifter(entity);
            // Note: never called SetStatShift — ActiveShifts is empty.

            Assert.DoesNotThrow(() => shifter.RemoveStatShifts(),
                "RemoveStatShifts on a fresh shifter must be a clean no-op.");
            Assert.AreEqual(5, entity.GetStatValue("DV"));
        }
    }
}
