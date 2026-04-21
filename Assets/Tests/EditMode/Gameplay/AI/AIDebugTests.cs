using System.Text.RegularExpressions;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Phase 10 — AI debug / introspection primitive tests. Covers
    /// <see cref="BrainPart.Think"/>, <see cref="BrainPart.LastThought"/>,
    /// <see cref="BrainPart.ThinkOutLoud"/>, and later commits' additions
    /// (<see cref="GoalHandler.GetDescription"/>, <see cref="GoalHandler.GetDetails"/>).
    /// Kept separate from <c>AIBehaviorPartTests</c> so behaviors and debug
    /// surface don't co-mingle — each can grow independently.
    /// </summary>
    [TestFixture]
    public class AIDebugTests
    {
        // ========================
        // Brain.Think primitive (Commit 1)
        // ========================

        [Test]
        public void Think_StoresLastThought()
        {
            var entity = new Entity { BlueprintName = "TestBrain" };
            var brain = new BrainPart();
            entity.AddPart(brain);

            brain.Think("I see a hostile");

            Assert.AreEqual("I see a hostile", brain.LastThought);
        }

        [Test]
        public void Think_WithNull_NoThrow_LastThoughtIsNull()
        {
            // Regression guard: Think(null) must not throw, and LastThought
            // must accept null (so "clear the thought" is expressible by
            // passing null explicitly).
            var entity = new Entity { BlueprintName = "TestBrain" };
            var brain = new BrainPart();
            entity.AddPart(brain);
            brain.LastThought = "some prior thought";

            Assert.DoesNotThrow(() => brain.Think(null));
            Assert.IsNull(brain.LastThought);
        }

        [Test]
        public void Think_WhenThinkOutLoudFalse_DoesNotEmitDebugLog()
        {
            // Counter-check for Think_WhenThinkOutLoudTrue_EmitsDebugLog:
            // with ThinkOutLoud=false (the default), Think must NOT log so
            // production builds don't spam the console. LogAssert.NoUnexpectedReceived()
            // would fire if any log landed during this test frame.
            var entity = new Entity { BlueprintName = "SilentBrain" };
            var brain = new BrainPart { ThinkOutLoud = false };
            entity.AddPart(brain);

            brain.Think("should be silent");

            // Asserting silence in NUnit: there should be no expected log, and
            // Unity's test runner will fail this test if an unexpected log fired.
            LogAssert.NoUnexpectedReceived();
            Assert.AreEqual("should be silent", brain.LastThought);
        }

        [Test]
        public void Think_WhenThinkOutLoudTrue_EmitsDebugLogInExpectedFormat()
        {
            // Pins the log format: "[Think:<displayName>] <thought>".
            // Future refactors of the prefix or separator would silently
            // break any tooling that greps Unity logs for "[Think:" — this
            // test pins the wire format.
            var entity = new Entity { BlueprintName = "LoudBrain" };
            entity.AddPart(new RenderPart { DisplayName = "loud brain" });
            var brain = new BrainPart { ThinkOutLoud = true };
            entity.AddPart(brain);

            // LogAssert.Expect matches the next Debug.Log with the given regex.
            LogAssert.Expect(LogType.Log, new Regex(@"^\[Think:loud brain\] attacking$"));
            brain.Think("attacking");
        }

        [Test]
        public void GoalHandlerThink_RoutesToParentBrain()
        {
            // The protected Think shim on GoalHandler must forward to the
            // parent brain so goals don't need to null-check ParentBrain at
            // every call site. Tests the shim via a concrete test goal.
            var entity = new Entity { BlueprintName = "ShimBrain" };
            var brain = new BrainPart();
            entity.AddPart(brain);

            var goal = new ThinkThroughShimGoal();
            brain.PushGoal(goal);
            goal.InvokeThink("routed via shim");

            Assert.AreEqual("routed via shim", brain.LastThought);
        }

        [Test]
        public void GoalHandlerThink_WithNullParentBrain_NoThrow()
        {
            // Defensive: a goal constructed but NOT yet pushed has
            // ParentBrain=null. Think called in that window must not throw —
            // e.g. if a test constructs a goal and inspects it standalone.
            var goal = new ThinkThroughShimGoal();
            Assert.DoesNotThrow(() => goal.InvokeThink("orphaned"));
        }

        /// <summary>
        /// Test-only goal that exposes the protected <see cref="GoalHandler.Think"/>
        /// shim for direct verification. TakeAction is a no-op — the tests
        /// never actually run the goal; they just call InvokeThink().
        /// </summary>
        private class ThinkThroughShimGoal : GoalHandler
        {
            public override void TakeAction() { /* no-op */ }
            public void InvokeThink(string thought) => Think(thought);
        }
    }
}
