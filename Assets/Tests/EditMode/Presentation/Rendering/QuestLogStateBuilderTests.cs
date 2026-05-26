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
            NarrativeStatePart.Current = null; // hygiene: progress reads facts
        }

        [TearDown]
        public void TearDown()
        {
            NarrativeStatePart.Current = null;
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

        // ====================================================================
        // 7. Stages list: full per-stage rows with Done/Current/Pending
        //    status (Qud-parity, forward-compatible with the flat-step
        //    model). The renderer shows the whole objective list, not just
        //    the current step (Docs/QUEST-LOG-UI.md).
        // ====================================================================

        [Test]
        public void Build_ActiveEntry_PopulatesAllStagesWithStatus()
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
                CurrentStageIndex = 1, // "fetch_key"
                EnteredStageAtTurn = 3,
            });

            var entry = QuestLogStateBuilder.Build(sp).Active.First(e => e.QuestId == "Q1");

            Assert.AreEqual(3, entry.Stages.Count,
                "Stages must list ALL stages of the quest, not just the current one.");
            // Stage 0 (before current) = Done; stage 1 (current) = Current;
            // stage 2 (after current) = Pending.
            Assert.AreEqual("intro", entry.Stages[0].StageId);
            Assert.AreEqual(QuestLogStageStatus.Done, entry.Stages[0].Status,
                "Stages before the current index render as Done (✓).");
            Assert.AreEqual("fetch_key", entry.Stages[1].StageId);
            Assert.AreEqual(QuestLogStageStatus.Current, entry.Stages[1].Status,
                "The stage AT the current index renders as Current (►).");
            Assert.AreEqual("deliver", entry.Stages[2].StageId);
            Assert.AreEqual(QuestLogStageStatus.Pending, entry.Stages[2].Status,
                "Stages after the current index render as Pending (·).");
        }

        [Test]
        public void Build_ActiveEntry_FirstStage_NoneDone()
        {
            // Counter-check: at stage 0, NOTHING is Done — the first row is
            // Current and the rest Pending. (A buggy "i <= current => Done"
            // would wrongly mark the current stage Done.)
            var quest = new StoryletData
            {
                ID = "Q1",
                Quest = new QuestData
                {
                    Stages = new List<QuestStageData>
                    {
                        new QuestStageData { ID = "a" },
                        new QuestStageData { ID = "b" },
                    },
                },
            };
            StoryletRegistry.Register(quest);

            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "Q1", CurrentStageIndex = 0 });

            var entry = QuestLogStateBuilder.Build(sp).Active.First(e => e.QuestId == "Q1");

            Assert.AreEqual(QuestLogStageStatus.Current, entry.Stages[0].Status);
            Assert.AreEqual(QuestLogStageStatus.Pending, entry.Stages[1].Status);
            Assert.IsFalse(entry.Stages.Any(r => r.Status == QuestLogStageStatus.Done),
                "At the first stage, no row should be Done.");
        }

        [Test]
        public void Build_QuestNotInRegistry_EmptyStages()
        {
            // Defensive: unresolvable quest → empty Stages (not null), so
            // the renderer can iterate without a null check.
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "GhostQuest", CurrentStageIndex = 2 });

            var entry = QuestLogStateBuilder.Build(sp).Active.First(e => e.QuestId == "GhostQuest");

            Assert.IsNotNull(entry.Stages, "Stages must be empty, not null.");
            Assert.AreEqual(0, entry.Stages.Count,
                "Unresolvable quest contributes no stage rows.");
        }

        // ====================================================================
        // Q3.4 — current-stage objective sub-rows (CurrentObjectives)
        // ====================================================================

        private static StoryletData QuestWithObjectives(string id,
            params QuestObjectiveData[] stage0Objectives)
        {
            var s0 = new QuestStageData { ID = "s0" };
            s0.Objectives.AddRange(stage0Objectives);
            return new StoryletData
            {
                ID = id,
                Quest = new QuestData
                {
                    Stages = new List<QuestStageData> { s0, new QuestStageData { ID = "s1" } },
                },
            };
        }

        [Test]
        public void Build_CurrentStageObjectives_PopulatedWithDoneStatus()
        {
            StoryletRegistry.Register(QuestWithObjectives("Q1",
                new QuestObjectiveData { ID = "a", Text = "Do A" },
                new QuestObjectiveData { ID = "b", Text = "Do B" }));
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "Q1", CurrentStageIndex = 0 });
            sp.GetQuestState("Q1").FinishedObjectives.Add("a");

            var entry = QuestLogStateBuilder.Build(sp).Active.First(e => e.QuestId == "Q1");

            Assert.AreEqual(2, entry.CurrentObjectives.Count,
                "the current stage's objectives appear as sub-rows");
            var a = entry.CurrentObjectives.First(o => o.ObjectiveId == "a");
            var b = entry.CurrentObjectives.First(o => o.ObjectiveId == "b");
            Assert.IsTrue(a.Done, "finished objective → Done");
            Assert.AreEqual("Do A", a.Text);
            Assert.IsFalse(b.Done, "unfinished objective → not Done");
        }

        [Test]
        public void Build_HiddenUnfinishedObjective_FilteredUntilDone()
        {
            StoryletRegistry.Register(QuestWithObjectives("Q1",
                new QuestObjectiveData { ID = "visible" },
                new QuestObjectiveData { ID = "secret", Hidden = true }));
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "Q1", CurrentStageIndex = 0 });

            var entry = QuestLogStateBuilder.Build(sp).Active.First(e => e.QuestId == "Q1");
            Assert.AreEqual(1, entry.CurrentObjectives.Count,
                "hidden + unfinished objective is filtered out of the log");
            Assert.AreEqual("visible", entry.CurrentObjectives[0].ObjectiveId);

            // Counter: finishing the hidden objective reveals it (Done).
            sp.GetQuestState("Q1").FinishedObjectives.Add("secret");
            var entry2 = QuestLogStateBuilder.Build(sp).Active.First(e => e.QuestId == "Q1");
            Assert.IsTrue(entry2.CurrentObjectives.Any(o => o.ObjectiveId == "secret" && o.Done),
                "a hidden objective is revealed once finished");
        }

        [Test]
        public void Build_NoObjectivesStage_EmptyCurrentObjectives()
        {
            // Counter-check: a legacy linear stage (no objectives) yields an
            // empty (non-null) CurrentObjectives so the renderer skips cleanly.
            var quest = new StoryletData
            {
                ID = "Q1",
                Quest = new QuestData
                {
                    Stages = new List<QuestStageData>
                    { new QuestStageData { ID = "s0" }, new QuestStageData { ID = "s1" } },
                },
            };
            StoryletRegistry.Register(quest);
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "Q1", CurrentStageIndex = 0 });

            var entry = QuestLogStateBuilder.Build(sp).Active.First(e => e.QuestId == "Q1");
            Assert.IsNotNull(entry.CurrentObjectives);
            Assert.AreEqual(0, entry.CurrentObjectives.Count);
        }

        [Test]
        public void Build_OptionalObjective_FlaggedOptional()
        {
            StoryletRegistry.Register(QuestWithObjectives("Q1",
                new QuestObjectiveData { ID = "req" },
                new QuestObjectiveData { ID = "opt", Optional = true }));
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "Q1", CurrentStageIndex = 0 });

            var entry = QuestLogStateBuilder.Build(sp).Active.First(e => e.QuestId == "Q1");
            Assert.IsTrue(entry.CurrentObjectives.First(o => o.ObjectiveId == "opt").Optional);
            Assert.IsFalse(entry.CurrentObjectives.First(o => o.ObjectiveId == "req").Optional);
        }

        // ====================================================================
        // Live objective progress (counter / collect-N objectives)
        //
        // A counter objective gates on IfFact:<fact>:>=:<N>. The builder reads
        // the current fact value and surfaces current/target so the renderer
        // can show "Rout the dirt gnomes (1/3)" live. Only multi-counters
        // (Target > 1) get progress — single-target objectives (kill-1, reach,
        // fetch) stay clean.
        // ====================================================================

        private static QuestObjectiveData CounterObjective(
            string id, string text, string fact, string op, int threshold)
        {
            var o = new QuestObjectiveData { ID = id, Text = text };
            o.Triggers.Add(new ConversationParam { Key = "IfFact", Value = $"{fact}:{op}:{threshold}" });
            return o;
        }

        [Test]
        public void Build_CounterObjective_PopulatesLiveProgress()
        {
            StoryletRegistry.Register(QuestWithObjectives("Q1",
                CounterObjective("rout", "Rout the gnomes", "gnomes_routed", ">=", 3)));
            var ns = new NarrativeStatePart();
            ns.SetFact("gnomes_routed", 1);
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "Q1", CurrentStageIndex = 0 });

            var o = QuestLogStateBuilder.Build(sp, ns)
                .Active.First(e => e.QuestId == "Q1")
                .CurrentObjectives.First(x => x.ObjectiveId == "rout");

            Assert.IsTrue(o.HasProgress, "an IfFact:>=:N (N>1) objective shows live progress");
            Assert.AreEqual(1, o.Current, "Current reflects the live fact value");
            Assert.AreEqual(3, o.Target, "Target is the threshold from the trigger");
        }

        [Test]
        public void Build_CounterProgress_ClampsCurrentToTarget()
        {
            // If the fact overshoots the threshold (e.g. an extra kill), the
            // displayed Current must clamp to Target — never "(5/3)".
            StoryletRegistry.Register(QuestWithObjectives("Q1",
                CounterObjective("rout", "Rout the gnomes", "gnomes_routed", ">=", 3)));
            var ns = new NarrativeStatePart();
            ns.SetFact("gnomes_routed", 5);
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "Q1", CurrentStageIndex = 0 });

            var o = QuestLogStateBuilder.Build(sp, ns)
                .Active.First(e => e.QuestId == "Q1").CurrentObjectives.First();
            Assert.AreEqual(3, o.Current, "Current clamps to Target for display");
            Assert.AreEqual(3, o.Target);
        }

        [Test]
        public void Build_CounterProgress_ZeroWhenFactUnset()
        {
            StoryletRegistry.Register(QuestWithObjectives("Q1",
                CounterObjective("rout", "Rout the gnomes", "gnomes_routed", ">=", 3)));
            var ns = new NarrativeStatePart(); // fact never set → 0
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "Q1", CurrentStageIndex = 0 });

            var o = QuestLogStateBuilder.Build(sp, ns)
                .Active.First(e => e.QuestId == "Q1").CurrentObjectives.First();
            Assert.IsTrue(o.HasProgress);
            Assert.AreEqual(0, o.Current, "unset fact reads 0 → (0/3)");
        }

        [Test]
        public void Build_SingleTargetObjective_NoProgress()
        {
            // Counter-check: IfFact:>=:1 (single kill / reach) is NOT a counter
            // — showing "(0/1)/(1/1)" is noise. HasProgress must be false so the
            // renderer just shows done/pending.
            StoryletRegistry.Register(QuestWithObjectives("Q1",
                CounterObjective("slay", "Slay the beast", "beast_slain", ">=", 1)));
            var ns = new NarrativeStatePart();
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "Q1", CurrentStageIndex = 0 });

            var o = QuestLogStateBuilder.Build(sp, ns)
                .Active.First(e => e.QuestId == "Q1").CurrentObjectives.First();
            Assert.IsFalse(o.HasProgress, "single-target (threshold 1) objectives show no counter");
        }

        [Test]
        public void Build_NonCounterObjective_NoProgress()
        {
            // Counter-check: a fetch objective gates on IfHaveItem (not IfFact)
            // → no parseable counter → no progress.
            var fetch = new QuestObjectiveData { ID = "find", Text = "Find the locket" };
            fetch.Triggers.Add(new ConversationParam { Key = "IfHaveItem", Value = "Locket" });
            StoryletRegistry.Register(QuestWithObjectives("Q1", fetch));
            var ns = new NarrativeStatePart();
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "Q1", CurrentStageIndex = 0 });

            var o = QuestLogStateBuilder.Build(sp, ns)
                .Active.First(e => e.QuestId == "Q1").CurrentObjectives.First();
            Assert.IsFalse(o.HasProgress, "non-IfFact objectives carry no counter progress");
            Assert.AreEqual(0, o.Target);
        }

        [Test]
        public void Build_CounterObjective_NullNarrativeState_NoCrash_ZeroCurrent()
        {
            // Defensive: no narrative state (pre-bootstrap) → no crash; the
            // counter still shows its target with Current=0.
            StoryletRegistry.Register(QuestWithObjectives("Q1",
                CounterObjective("rout", "Rout the gnomes", "gnomes_routed", ">=", 3)));
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "Q1", CurrentStageIndex = 0 });

            QuestLogObjectiveRow o = default;
            Assert.DoesNotThrow(() => o = QuestLogStateBuilder.Build(sp, null)
                .Active.First(e => e.QuestId == "Q1").CurrentObjectives.First());
            Assert.IsTrue(o.HasProgress);
            Assert.AreEqual(0, o.Current);
            Assert.AreEqual(3, o.Target);
        }
    }
}
