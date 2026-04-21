using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Phase 10 Commit 5 — verifies <see cref="SidebarTextFormatter.FormatFocus"/>
    /// emits the goal-stack + last-thought block when the inspector toggle
    /// is on, and stays silent when off. These tests exercise the actual
    /// text layout end-to-end: scenario builds creature → LookQueryService
    /// populates LookSnapshot → FormatFocus produces the rendered lines.
    ///
    /// Separate from <c>LookQueryServiceAIInspectorTests</c> — those test
    /// the LookSnapshot fields directly; these test the text output a player
    /// would actually see in the sidebar.
    /// </summary>
    [TestFixture]
    public class SidebarInspectorFormatterTests
    {
        private static ScenarioTestHarness _harness;
        private const int DefaultWidth = 40;
        private const int DefaultMaxLines = 14;

        [OneTimeSetUp]
        public void OneTimeSetUp() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown() => _harness?.Dispose();

        [TearDown]
        public void TearDown() => AIDebug.AIInspectorEnabled = false;

        // ========================
        // Inspector off → no extra lines
        // ========================

        [Test]
        public void FormatFocus_InspectorOff_EmitsBaseLinesOnly()
        {
            // Baseline: when the toggle is off, FormatFocus should produce
            // only the pre-Phase-10 lines (header, summary, detail lines).
            // No "Goals:" header, no "Thought:" trailer. Protects the
            // production rendering path from accidentally picking up the
            // inspector block.
            AIDebug.AIInspectorEnabled = false;
            var ctx = _harness.CreateContext();
            var snapjaw = ctx.Spawn("Snapjaw").AtPlayerOffset(3, 0);

            var pos = ctx.Zone.GetEntityPosition(snapjaw);
            var snap = LookQueryService.BuildSnapshot(
                ctx.PlayerEntity, ctx.Zone, pos.x, pos.y);
            List<string> lines = SidebarTextFormatter.FormatFocus(
                snap, DefaultWidth, DefaultMaxLines);

            CollectionAssert.DoesNotContain(lines, "Goals:",
                "Inspector OFF → no 'Goals:' header in focus output.");
            foreach (var line in lines)
                StringAssert.DoesNotStartWith("Thought: ", line,
                    "Inspector OFF → no 'Thought:' trailer in focus output.");
        }

        // ========================
        // Inspector on → renders block
        // ========================

        [Test]
        public void FormatFocus_InspectorOn_EmitsGoalsHeaderAndThoughtTrailer()
        {
            // Positive path: toggle on + Creature target → focus output
            // contains the section header "Goals:" and a "Thought:" line.
            AIDebug.AIInspectorEnabled = true;
            var ctx = _harness.CreateContext();
            var snapjaw = ctx.Spawn("Snapjaw").AtPlayerOffset(3, 0);
            // Fire one tick so the brain pushes at least BoredGoal
            // (brand-new creatures have Goals.Count == 0 until first turn).
            snapjaw.FireEvent(GameEvent.New("TakeTurn"));

            var pos = ctx.Zone.GetEntityPosition(snapjaw);
            var snap = LookQueryService.BuildSnapshot(
                ctx.PlayerEntity, ctx.Zone, pos.x, pos.y);
            List<string> lines = SidebarTextFormatter.FormatFocus(
                snap, DefaultWidth, DefaultMaxLines);

            CollectionAssert.Contains(lines, "Goals:",
                "Inspector ON → 'Goals:' header must be in the output.");
            bool hasThought = false;
            foreach (var line in lines)
            {
                if (line.StartsWith("Thought: ")) { hasThought = true; break; }
            }
            Assert.IsTrue(hasThought,
                "Inspector ON → at least one line must start with 'Thought: '.");
        }

        [Test]
        public void FormatFocus_GoalStackLines_AreIndentedUnderHeader()
        {
            // Pins the rendering shape: each goal-stack line gets a
            // two-space indent under "Goals:" so the hierarchy reads
            // visually. If a refactor drops the indent, this fires.
            AIDebug.AIInspectorEnabled = true;
            var ctx = _harness.CreateContext();
            var snapjaw = ctx.Spawn("Snapjaw").AtPlayerOffset(3, 0);
            snapjaw.FireEvent(GameEvent.New("TakeTurn"));

            var pos = ctx.Zone.GetEntityPosition(snapjaw);
            var snap = LookQueryService.BuildSnapshot(
                ctx.PlayerEntity, ctx.Zone, pos.x, pos.y);
            List<string> lines = SidebarTextFormatter.FormatFocus(
                snap, DefaultWidth, DefaultMaxLines);

            int goalsHeaderIdx = lines.IndexOf("Goals:");
            Assert.GreaterOrEqual(goalsHeaderIdx, 0, "Should contain 'Goals:' header.");
            // The next line after the header should start with the two-space indent.
            int nextIdx = goalsHeaderIdx + 1;
            Assert.Less(nextIdx, lines.Count,
                "At least one line expected after 'Goals:' header.");
            StringAssert.StartsWith("  ", lines[nextIdx],
                "Goal-stack lines must be indented under the header.");
        }

        [Test]
        public void FormatFocus_OverflowIndicator_WhenStackExceeds8Lines()
        {
            // Pins the cap: a stack with > 8 entries collapses the extras
            // into "... (N more)" rather than pushing the log off-screen.
            // Uses synthetic goals with distinct descriptions so run-length
            // collapse doesn't interfere.
            AIDebug.AIInspectorEnabled = true;
            var ctx = _harness.CreateContext();
            var creature = ctx.Spawn("Snapjaw").AtPlayerOffset(3, 0);
            var brain = creature.GetPart<BrainPart>();
            brain.ClearGoals();
            // Push 11 goals with unique descriptions. Expected: 8 rendered
            // + "... (3 more)".
            for (int i = 0; i < 11; i++)
                brain.PushGoal(new SyntheticDescGoal("G" + i));

            var pos = ctx.Zone.GetEntityPosition(creature);
            var snap = LookQueryService.BuildSnapshot(
                ctx.PlayerEntity, ctx.Zone, pos.x, pos.y);
            List<string> lines = SidebarTextFormatter.FormatFocus(
                snap, DefaultWidth, DefaultMaxLines);

            bool hasOverflow = false;
            foreach (var line in lines)
            {
                if (line.Contains("... (3 more)")) { hasOverflow = true; break; }
            }
            Assert.IsTrue(hasOverflow,
                "Stack of 11 goals should emit '... (3 more)' overflow line " +
                "when cap is 8. Rendered lines: \n  " + string.Join("\n  ", lines));
        }

        [Test]
        public void FormatFocus_ThoughtLine_ReflectsBrainLastThought()
        {
            // End-to-end: whatever the brain's LastThought is, it must
            // surface verbatim on the "Thought: <x>" line.
            AIDebug.AIInspectorEnabled = true;
            var ctx = _harness.CreateContext();
            var creature = ctx.Spawn("Snapjaw").AtPlayerOffset(3, 0);
            var brain = creature.GetPart<BrainPart>();
            brain.Think("chasing prey");

            var pos = ctx.Zone.GetEntityPosition(creature);
            var snap = LookQueryService.BuildSnapshot(
                ctx.PlayerEntity, ctx.Zone, pos.x, pos.y);
            List<string> lines = SidebarTextFormatter.FormatFocus(
                snap, DefaultWidth, DefaultMaxLines);

            CollectionAssert.Contains(lines, "Thought: chasing prey",
                "Thought line must reflect the current Brain.LastThought verbatim.");
        }

        /// <summary>
        /// Test-only goal with a caller-supplied description. Lets the
        /// overflow test assert deterministic counts without depending on
        /// any particular shipped goal's description string.
        /// </summary>
        private sealed class SyntheticDescGoal : GoalHandler
        {
            private readonly string _desc;
            public SyntheticDescGoal(string desc) { _desc = desc; }
            public override void TakeAction() { /* no-op */ }
            public override string GetDescription() => _desc;
        }
    }
}
