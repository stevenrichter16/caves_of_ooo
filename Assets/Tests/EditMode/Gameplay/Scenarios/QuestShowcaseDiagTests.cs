using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Scenarios.Custom;
using CavesOfOoo.Storylets;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests.Scenarios
{
    /// <summary>
    /// QS.7 end-to-end verification for <see cref="QuestShowcase"/>.
    /// Drives the full QS substrate (predicates → actions → dispatch
    /// → rewards) the way a player would, asserting the diag substrate
    /// captures the expected `quest/*` records at each phase.
    ///
    /// Pattern follows the 11+ prior scenario diag fixtures
    /// (OnHit / Trap / Elemental / CombatHooks / CombatParity /
    /// ThrowableTonics / LockedDoor / MerchantShop). Real Player
    /// blueprint, Diag.ResetAll AFTER scenario setup, post-action
    /// diag_query assertions.
    ///
    /// Five pinnable contracts:
    ///   1. Building the scenario produces NO quest/* records on its
    ///      own (counter-check against passive logging).
    ///   2. StartQuest action emits exactly one quest/Started.
    ///   3. Pickup → tick advances stage 0→1 + emits quest/StageAdvanced
    ///      + fires stage-1 OnEnter rewards (XP + drams).
    ///   4. Explicit AdvanceQuestStage past terminal auto-completes,
    ///      emits quest/Completed.
    ///   5. Disjoint-sets contract holds end-to-end: completed quest
    ///      no longer in active.
    /// </summary>
    [TestFixture]
    public class QuestShowcaseDiagTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetUp() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown() => _harness?.Dispose();

        [SetUp]
        public void SetUp()
        {
            ConversationActions.Reset();
            ConversationPredicates.Reset();
            StoryletRegistry.Reset();
            Diag.ResetAll();
            // Tick-driven AddFact effects need a Current narrative state.
            NarrativeStatePart.Current = new NarrativeStatePart();
            // QuestShowcase.Apply re-creates StoryletPart.Current if null,
            // so reset to null to test the fallback path.
            StoryletPart.Current = null;
            StoryletPart.LocalPlayer = null;
        }

        [TearDown]
        public void TearDown()
        {
            StoryletPart.Current = null;
            StoryletPart.LocalPlayer = null;
            NarrativeStatePart.Current = null;
        }

        // ====================================================================
        // 1. Counter-check: applying the scenario alone produces NO
        //    quest/* records (rules out passive logging in showcase
        //    setup, like prior scenario fixtures' "build doesn't
        //    spam diag" pin).
        // ====================================================================

        [Test]
        public void ApplyShowcase_WithoutInteracting_ProducesNoQuestDiag()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            Diag.ResetAll();

            new QuestShowcase().Apply(ctx);

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "quest",
                Limit = 50,
            }).Records;

            Assert.AreEqual(0, records.Count,
                $"Building the scenario must NOT produce any quest/* diag " +
                $"records — scenarios shouldn't passively start quests on " +
                $"the player. Got {records.Count}.");
        }

        // ====================================================================
        // 2. StartQuest action emits quest/Started
        // ====================================================================

        [Test]
        public void StartQuest_RecordsQuestStartedDiag()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            new QuestShowcase().Apply(ctx);
            Diag.ResetAll();

            ConversationActions.Execute(
                "StartQuest", null, ctx.PlayerEntity, QuestShowcase.QuestId);

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "quest",
                Kind = "Started",
                Limit = 10,
            }).Records;

            Assert.AreEqual(1, records.Count,
                $"Showcase StartQuest must emit one quest/Started. " +
                $"Got {records.Count}.");
            Assert.IsTrue(
                records[0].PayloadJson.Contains(
                    $"\"questId\":\"{QuestShowcase.QuestId}\""),
                $"Payload questId. Got: {records[0].PayloadJson}");
        }

        // ====================================================================
        // 3. Pickup → tick advances stage + fires rewards (QS.4 + QS.5)
        // ====================================================================

        [Test]
        public void PickupKey_TickAdvancesStageAndFiresRewards()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            new QuestShowcase().Apply(ctx);

            // QS.7: tick-driven dispatch needs LocalPlayer set so the
            // IfHaveItem(IronKey) trigger can read the player's
            // inventory. GameBootstrap sets this in production; the
            // harness doesn't, so we set it here.
            StoryletPart.LocalPlayer = ctx.PlayerEntity;

            // Player accepts quest.
            ConversationActions.Execute(
                "StartQuest", null, ctx.PlayerEntity, QuestShowcase.QuestId);

            // Simulate the player picking up the iron key by giving
            // them a fresh IronKey directly. (The scenario's IronKey
            // is on the floor — picking it up via real movement +
            // pickup would require driving the input system. For the
            // diag test, putting an IronKey in inventory is sufficient
            // to satisfy the IfHaveItem trigger.)
            var key = ctx.Factory.CreateEntity("IronKey");
            ctx.PlayerEntity.GetPart<InventoryPart>().AddObject(key);

            int xpBefore = ctx.PlayerEntity.GetStatValue("Experience");
            int dramsBefore = TradeSystem.GetDrams(ctx.PlayerEntity);
            Diag.ResetAll();

            // Tick — QS.4 dispatch should fire the IfHaveItem trigger.
            StoryletPart.Current.OnTickEnd(NarrativeStatePart.Current);

            // Stage advance recorded.
            var advancedRecords = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "quest",
                Kind = "StageAdvanced",
                Limit = 10,
            }).Records;
            Assert.AreEqual(1, advancedRecords.Count,
                "Tick after pickup must record one quest/StageAdvanced " +
                "(QS.4 dispatch) — the trigger fired.");
            Assert.IsTrue(advancedRecords[0].PayloadJson.Contains("\"fromIndex\":0"));
            Assert.IsTrue(advancedRecords[0].PayloadJson.Contains("\"toIndex\":1"));

            // Stage-1 OnEnter rewards fired (QS.5 wrappers).
            int xpAfter = ctx.PlayerEntity.GetStatValue("Experience");
            int dramsAfter = TradeSystem.GetDrams(ctx.PlayerEntity);
            Assert.Greater(xpAfter, xpBefore,
                "Stage-1 OnEnter must include AwardXP — Experience must " +
                "have increased.");
            Assert.AreEqual(dramsBefore + 50, dramsAfter,
                "Stage-1 OnEnter must include GiveDrams 50 — drams " +
                "balance must have increased by exactly 50.");
        }

        // ====================================================================
        // 4. AdvanceQuestStage past terminal auto-completes
        //    (simulating Marceline's "hand over the key" dialogue)
        // ====================================================================

        [Test]
        public void AdvanceQuestStage_PastTerminal_AutoCompletes()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            new QuestShowcase().Apply(ctx);

            // Set up state mid-quest: stage 1 ("deliver_to_marceline").
            // Skip the picking-up-key tick — we just want to test the
            // terminal-stage dialogue advance contract.
            ConversationActions.Execute(
                "StartQuest", null, ctx.PlayerEntity, QuestShowcase.QuestId);
            // Manually set stage to 1 (the terminal stage) to bypass
            // the tick-driven advance covered in test 3.
            StoryletPart.Current.GetQuestState(QuestShowcase.QuestId)
                .CurrentStageIndex = 1;
            Diag.ResetAll();

            // Player talks to Marceline → conversation calls
            // AdvanceQuestStage — past terminal → auto-complete.
            ConversationActions.Execute(
                "AdvanceQuestStage", null, ctx.PlayerEntity, QuestShowcase.QuestId);

            var completed = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "quest",
                Kind = "Completed",
                Limit = 10,
            }).Records;
            Assert.AreEqual(1, completed.Count,
                "Past-terminal advance must auto-complete via the " +
                "centralized helper (QS.3 cold-eye fix #1). Got " +
                $"{completed.Count} quest/Completed records.");
            Assert.IsTrue(completed[0].PayloadJson.Contains("\"totalStages\":2"),
                $"Payload totalStages. Got: {completed[0].PayloadJson}");
        }

        // ====================================================================
        // 5. Disjoint-sets contract holds end-to-end (renderer pin)
        //
        // Pins that the QS.2 disjoint-sets invariant survives the
        // full lifecycle: a quest that's been started AND advanced
        // AND completed ends in _completedQuests, not _quests. The
        // QuestLogStateBuilder snapshot reflects this.
        // ====================================================================

        [Test]
        public void EndToEnd_QuestLifecycle_DisjointSetsAtRendererBoundary()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            new QuestShowcase().Apply(ctx);

            // Run through the full lifecycle.
            ConversationActions.Execute(
                "StartQuest", null, ctx.PlayerEntity, QuestShowcase.QuestId);
            // Skip pickup tick — test just needs the state-machine
            // to walk all stages.
            StoryletPart.Current.GetQuestState(QuestShowcase.QuestId)
                .CurrentStageIndex = 1;
            ConversationActions.Execute(
                "AdvanceQuestStage", null, ctx.PlayerEntity, QuestShowcase.QuestId);

            // Build the renderer snapshot.
            var snap = CavesOfOoo.Rendering.QuestLogStateBuilder.Build(
                StoryletPart.Current);

            Assert.AreEqual(0, snap.ActiveCount,
                "Completed quest must NOT appear in Active list — " +
                "renderer would lie about state.");
            Assert.AreEqual(1, snap.CompletedCount);
            Assert.AreEqual(QuestShowcase.QuestId, snap.Completed[0],
                "Completed list contains the quest ID.");
        }
    }
}
