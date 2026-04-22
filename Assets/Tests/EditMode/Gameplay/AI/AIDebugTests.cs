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

        // ========================
        // GoalHandler.GetDescription / GetDetails defaults (Commit 2)
        // ========================

        [Test]
        public void GetDescription_DefaultOnBoredGoal_IsTypeName()
        {
            // A concrete shipped goal that doesn't override GetDetails should
            // surface its type name verbatim — no ceremony, no empty ": ".
            var goal = new BoredGoal();
            Assert.AreEqual("BoredGoal", goal.GetDescription());
        }

        [Test]
        public void GetDetails_DefaultReturnsNull()
        {
            // Base contract: GetDetails defaults to null so subclasses opt in.
            // If this default ever becomes "" or something else, every
            // non-overriding goal's inspector line shape would shift.
            var goal = new BoredGoal();
            Assert.IsNull(goal.GetDetails());
        }

        [Test]
        public void GetDescription_WithDetailsOverride_FormatsAsTypeColonDetails()
        {
            // Pins the default format string: "TypeName: details" (single space
            // after colon, no trailing punctuation). If someone rewrites
            // GetDescription to use a different separator, inspector lines
            // drift silently — this test catches that.
            var goal = new DetailOnlyTestGoal { Details = "target=Snapjaw" };
            Assert.AreEqual(
                "DetailOnlyTestGoal: target=Snapjaw",
                goal.GetDescription());
        }

        [Test]
        public void GetDescription_WithEmptyDetails_FallsBackToTypeName()
        {
            // Counter-check: if a goal accidentally returns "" instead of
            // null, the inspector shouldn't render "BoredGoal: " with
            // trailing colon-space. GetDescription treats null and ""
            // identically via string.IsNullOrEmpty.
            var goal = new DetailOnlyTestGoal { Details = "" };
            Assert.AreEqual("DetailOnlyTestGoal", goal.GetDescription());
        }

        /// <summary>
        /// Test-only goal that returns a caller-supplied details string.
        /// Isolates the default-format behavior from the details logic of
        /// any production goal.
        /// </summary>
        private class DetailOnlyTestGoal : GoalHandler
        {
            public string Details;
            public override void TakeAction() { /* no-op */ }
            public override string GetDetails() => Details;
        }

        // ========================
        // GetDetails overrides on shipped goals (Commit 3)
        // ========================

        [Test]
        public void KillGoal_GetDetails_IncludesTargetDisplayName()
        {
            var target = new Entity { BlueprintName = "Snapjaw" };
            target.AddPart(new RenderPart { DisplayName = "snapjaw" });
            var goal = new KillGoal(target);
            Assert.AreEqual("target=snapjaw", goal.GetDetails());
        }

        [Test]
        public void KillGoal_GetDetails_NullTarget_ReturnsNull()
        {
            // Counter-check: null target → null details → GetDescription falls
            // back to "KillGoal" without dangling "target=" fragment.
            var goal = new KillGoal(null);
            Assert.IsNull(goal.GetDetails());
            Assert.AreEqual("KillGoal", goal.GetDescription());
        }

        [Test]
        public void MoveToGoal_GetDetails_IncludesTargetCoordsAndAge()
        {
            var goal = new MoveToGoal(42, 11, maxTurns: 100);
            goal.Age = 7;
            Assert.AreEqual("to=(42,11) age=7/100", goal.GetDetails());
        }

        [Test]
        public void GoFetchGoal_GetDetails_IncludesPhaseAttemptsItemName()
        {
            var bone = new Entity { BlueprintName = "Bone" };
            bone.AddPart(new RenderPart { DisplayName = "bone" });
            var goal = new GoFetchGoal(bone, returnHome: false);
            // Fresh goal: phase=WalkToItem, attempts=0/2, item=bone
            Assert.AreEqual(
                "phase=WalkToItem | attempts=0/2 | item=bone",
                goal.GetDetails());
        }

        [Test]
        public void GoFetchGoal_GetDetails_NullItem_ShowsNullMarker()
        {
            // Counter-check: a fetch with a null Item (item left zone between
            // push and inspector-read) should still render a readable details
            // string — no NullReferenceException.
            var goal = new GoFetchGoal(null, returnHome: true);
            string details = goal.GetDetails();
            Assert.IsNotNull(details);
            StringAssert.Contains("item=null", details);
        }

        [Test]
        public void FleeGoal_GetDetails_IncludesFromNameAndAge()
        {
            var threat = new Entity { BlueprintName = "Bandit" };
            threat.AddPart(new RenderPart { DisplayName = "bandit" });
            var goal = new FleeGoal(threat, maxTurns: 20);
            goal.Age = 3;
            Assert.AreEqual("from=bandit | age=3/20", goal.GetDetails());
        }

        [Test]
        public void RetreatGoal_GetDetails_IncludesPhaseWaypointAndAge()
        {
            var goal = new RetreatGoal(waypointX: 10, waypointY: 5,
                safeHpFraction: 0.75f, maxTurns: 200, healPerTick: 1);
            goal.Age = 12;
            Assert.AreEqual(
                "phase=Travel | waypoint=(10,5) | age=12/200",
                goal.GetDetails());
        }

        [Test]
        public void CommandGoal_GetDetails_IncludesCommandName()
        {
            var goal = new CommandGoal("CommandSubmerge");
            Assert.AreEqual("command=CommandSubmerge", goal.GetDetails());
        }

        [Test]
        public void CommandGoal_GetDetails_EmptyCommand_ReturnsNull()
        {
            // Counter-check: empty command should not produce "command=" with
            // dangling equals sign.
            var goal = new CommandGoal("");
            Assert.IsNull(goal.GetDetails());
        }

        [Test]
        public void GoFetchGoal_GetDetails_DoesNotExposePrivateFieldsAsPublic()
        {
            // Regression: the inspector needs to read _walkAttempts (private).
            // The plan's approach is GetDetails reading its OWN class's private
            // field directly — no new public property. This test catches a
            // future refactor that "fixes" access by adding `public int
            // WalkAttempts { get; }` — that would break encapsulation without
            // need, since GetDetails already has in-class access.
            var fi = typeof(GoFetchGoal).GetField(
                "_walkAttempts",
                System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(fi,
                "GoFetchGoal._walkAttempts must remain a private field.");

            var pi = typeof(GoFetchGoal).GetProperty("WalkAttempts");
            Assert.IsNull(pi,
                "GoFetchGoal must NOT have a public WalkAttempts property — " +
                "GetDetails reads the private field directly and no external " +
                "code should need the value.");
        }
    }
}
