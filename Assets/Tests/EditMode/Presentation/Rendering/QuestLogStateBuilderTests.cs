using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// QS.6 tests (Docs/QUEST-SYSTEM.md) for the
    /// <see cref="QuestLogStateBuilder"/> — the pure-data state
    /// snapshot consumed by the quest log UI.
    ///
    /// Parity reference: Qud's <c>XRL.UI.QuestLog.GetLinesForQuest</c>
    /// emits per-stage rows with status decoration. CoO v1 emits
    /// per-quest rows with the CURRENT stage only (linear stages
    /// design — Docs/QUEST-SYSTEM.md design tension #1). Future
    /// branching support would add stage-history per quest, matching
    /// Qud more closely.
    /// </summary>
    [TestFixture]
    public class QuestLogStateBuilderTests
    {
        [SetUp]
        public void SetUp()
        {
            StoryletRegistry.Reset();
        }

        // ====================================================================
        // 1. Builds active-quest entries from StoryletPart state
        // ====================================================================

        [Test]
        public void Build_ActiveQuests_EmitsOneEntryPerActiveQuest()
        {
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState
            {
                QuestId = "Q1",
                CurrentStageIndex = 0,
                EnteredStageAtTurn = 5,
            });
            sp.StartQuest(new QuestState
            {
                QuestId = "Q2",
                CurrentStageIndex = 1,
                EnteredStageAtTurn = 10,
            });

            var snap = QuestLogStateBuilder.Build(sp);

            Assert.AreEqual(2, snap.ActiveCount,
                "Active list must contain one entry per active quest.");
            Assert.IsTrue(snap.Active.Any(e => e.QuestId == "Q1"));
            Assert.IsTrue(snap.Active.Any(e => e.QuestId == "Q2"));
        }

        // ====================================================================
        // 2. Resolves current stage's name + index correctly
        //
        // Pinning that the snapshot displays ONLY the current stage's
        // info — not the full stage history. Per the linear-stages
        // design decision, players see "where am I now" not "which
        // steps are done".
        // ====================================================================

        [Test]
        public void Build_ActiveEntry_ShowsCurrentStageOnly()
        {
            var quest = new StoryletData
            {
                ID = "Q1",
                Quest = new QuestData
                {
                    Stages = new List<QuestStageData>
                    {
                        new QuestStageData { ID = "intro" },
                        new QuestStageData { ID = "fetch_key" },
                        new QuestStageData { ID = "deliver" },
                    },
                },
            };
            StoryletRegistry.Register(quest);

            var sp = new StoryletPart();
            sp.StartQuest(new QuestState
            {
                QuestId = "Q1",
                CurrentStageIndex = 1,  // "fetch_key" — the middle one
                EnteredStageAtTurn = 78,
            });

            var snap = QuestLogStateBuilder.Build(sp);
            var entry = snap.Active.FirstOrDefault(e => e.QuestId == "Q1");

            Assert.AreEqual("Q1", entry.QuestId);
            Assert.AreEqual(1, entry.CurrentStageIndex,
                "CurrentStageIndex must reflect the active stage in QuestState.");
            Assert.AreEqual("fetch_key", entry.CurrentStageId,
                "CurrentStageId must resolve to the stage AT the current " +
                "index (not the first or last). The renderer shows the " +
                "PLAYER's current step, not the whole journey.");
            Assert.AreEqual(78, entry.EnteredStageAtTurn,
                "EnteredStageAtTurn must come through verbatim — used by " +
                "the renderer to display 'how long has the player been " +
                "stuck on this stage'.");
        }

        // ====================================================================
        // 3. Groups active vs completed correctly
        //
        // Counter-check that a completed quest does NOT appear in the
        // Active list (would clutter the renderer + lie about state).
        // Pin the disjoint-sets contract from QS.2 at the renderer
        // boundary.
        // ====================================================================

        [Test]
        public void Build_GroupsActiveAndCompleted_Correctly()
        {
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "ActiveQuest" });
            sp.StartQuest(new QuestState { QuestId = "FinishedQuest" });
            sp.MarkQuestCompleted("FinishedQuest");

            var snap = QuestLogStateBuilder.Build(sp);

            // Active section contains ONLY the still-active quest.
            Assert.AreEqual(1, snap.ActiveCount);
            Assert.AreEqual("ActiveQuest", snap.Active[0].QuestId);

            // Completed section contains ONLY the finished quest.
            Assert.AreEqual(1, snap.CompletedCount);
            Assert.AreEqual("FinishedQuest", snap.Completed[0]);

            // Counter-check: the FinishedQuest must NOT appear in Active.
            Assert.IsFalse(snap.Active.Any(e => e.QuestId == "FinishedQuest"),
                "Completed quest must NEVER appear in the Active list — " +
                "would lie about quest state. Pins the QS.2 disjoint-sets " +
                "contract at the renderer boundary.");
        }

        // ====================================================================
        // 4. Empty-state — no quests active OR completed
        //
        // Renderer needs to handle "you have no quests yet" cleanly.
        // Build returns empty lists (not null) so callers can iterate
        // without null checks.
        // ====================================================================

        [Test]
        public void Build_EmptyState_NoQuestsActive_ReturnsEmptySnapshot()
        {
            var sp = new StoryletPart();
            // No StartQuest, no MarkQuestCompleted.

            var snap = QuestLogStateBuilder.Build(sp);

            Assert.AreEqual(0, snap.ActiveCount);
            Assert.AreEqual(0, snap.CompletedCount);
            Assert.IsNotNull(snap.Active,
                "Active list must be empty (not null) — callers can iterate.");
            Assert.IsNotNull(snap.Completed,
                "Completed list must be empty (not null).");
        }

        // ====================================================================
        // 5. Defensive: null part returns empty snapshot without crashing
        //    (covers pre-bootstrap rendering — the UI may try to build
        //    a snapshot before StoryletPart.Current is set up)
        // ====================================================================

        [Test]
        public void Build_NullPart_ReturnsEmptySnapshotWithoutCrashing()
        {
            QuestLogSnapshot snap = default;
            Assert.DoesNotThrow(() => snap = QuestLogStateBuilder.Build(null));

            Assert.AreEqual(0, snap.ActiveCount);
            Assert.AreEqual(0, snap.CompletedCount);
        }

        // ====================================================================
        // 6. Defensive: quest in StoryletPart but missing from registry
        //    (save-game forward-compat — content might've been removed)
        //
        // The entry still appears in Active (we don't drop the quest
        // just because the registry doesn't know it), but
        // CurrentStageId is empty since the registry can't resolve it.
        // ====================================================================

        [Test]
        public void Build_QuestNotInRegistry_StillEmitsEntryWithEmptyStageId()
        {
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState
            {
                QuestId = "GhostQuest",
                CurrentStageIndex = 2,
                EnteredStageAtTurn = 50,
            });
            // No StoryletRegistry.Register call — the registry has no
            // QuestData for GhostQuest.

            var snap = QuestLogStateBuilder.Build(sp);

            Assert.AreEqual(1, snap.ActiveCount,
                "Quest still appears in Active even if registry can't " +
                "resolve its stages — defensive against content removal " +
                "between save sessions.");
            var entry = snap.Active[0];
            Assert.AreEqual("GhostQuest", entry.QuestId);
            Assert.AreEqual(2, entry.CurrentStageIndex,
                "CurrentStageIndex comes from QuestState — always valid.");
            Assert.AreEqual(string.Empty, entry.CurrentStageId,
                "CurrentStageId is empty when registry can't resolve it. " +
                "The renderer can show '?' or just the index.");
        }
    }
}
