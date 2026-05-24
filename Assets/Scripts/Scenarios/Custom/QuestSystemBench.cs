using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Q3.5 — DETERMINISTIC SELF-AUDITING BENCH for the quest system
    /// (Docs/MCP_PlayMode_Testing_Strategy.md). One press of Play drives a
    /// multi-objective quest through its WHOLE lifecycle against the REAL
    /// runtime (StoryletPart / ConversationActions / NarrativeStatePart /
    /// quest GameEvents / tick dispatch) — the path EditMode stubs — and
    /// emits one machine-checkable <c>questbench/Cell</c> diag record per
    /// assertion (incl. a CONTROL row), stamped with a per-run <c>runId</c>.
    ///
    /// <para><b>Audit it:</b> after launching,
    /// <c>diag_query category=questbench kind=Cell</c> — every record has a
    /// <c>pass</c> bool; scope to the newest <c>runId</c> (read the
    /// <c>questbench/MatrixAuditRun</c> marker) and assert no
    /// <c>pass=false</c>. Per-run unique quest IDs avoid stale-buffer
    /// cross-run pollution (Rule 8). Preconditions are guarded LOUDLY
    /// (Rule 4): a missing runtime emits an explicit FAILED cell, never a
    /// silent pass.</para>
    /// </summary>
    [Scenario(
        name: "Quest System Bench",
        category: "Quest",
        description: "Self-auditing: drives a multi-objective quest end-to-end in the live runtime and emits one questbench/Cell diag record per lifecycle assertion (+ control). diag_query category=questbench.")]
    public class QuestSystemBench : IScenario
    {
        /// <summary>Captures quest GameEvents fired on the player.</summary>
        private sealed class QuestEventProbe : Part
        {
            public override string Name => "QuestEventProbe";
            public readonly HashSet<string> Seen = new HashSet<string>();
            public override bool HandleEvent(GameEvent e)
            {
                switch (e.ID)
                {
                    case "QuestStarted":
                    case "QuestObjectiveFinished":
                    case "QuestStageAdvanced":
                    case "QuestCompleted":
                    case "QuestFailed":
                        Seen.Add(e.ID + ":" + e.GetStringParameter("QuestId"));
                        break;
                }
                return true;
            }
        }

        public void Apply(ScenarioContext ctx)
        {
            string runId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
            Diag.Record("questbench", "MatrixAuditRun", payload: new { runId });
            int pass = 0, fail = 0;

            void Cell(string cell, string expected, object actual, bool ok)
            {
                if (ok) pass++; else fail++;
                Diag.Record("questbench", "Cell", payload:
                    new { runId, cell, expected, actual = actual?.ToString() ?? "null", pass = ok });
            }

            var sp = StoryletPart.Current;
            var ns = NarrativeStatePart.Current;
            // Rule 4 — guard preconditions LOUDLY (explicit FAILED cell).
            if (sp == null) { Cell("precondition_storyletpart", "non-null", "null", false); ctx.Log("[QuestBench] SKIPPED: StoryletPart.Current is null (not in Play?)."); return; }
            if (ns == null) { Cell("precondition_narrativestate", "non-null", "null", false); ctx.Log("[QuestBench] SKIPPED: NarrativeStatePart.Current is null."); return; }

            // Probe quest events on the player (events fire on LocalPlayer).
            var player = StoryletPart.LocalPlayer ?? ctx.PlayerEntity;
            if (player != null && StoryletPart.LocalPlayer == null) StoryletPart.LocalPlayer = player;
            var probe = new QuestEventProbe();
            if (player != null) player.AddPart(probe);

            // ── Build a deterministic multi-objective quest (unique per run). ──
            string Q = "QBench_" + runId;
            string deed = "Bench hero closed the quest loop (" + runId + ").";
            var sd = new StoryletData { ID = Q, Quest = new QuestData { Accomplishment = deed } };
            var s0 = new QuestStageData { ID = "gather" };
            var objA = new QuestObjectiveData { ID = "a", Text = "Objective A" };
            objA.OnEnter.Add(new ConversationParam { Key = "SetFact", Value = "qb_" + runId + ":1" });
            s0.Objectives.Add(objA);
            s0.Objectives.Add(new QuestObjectiveData { ID = "b", Text = "Objective B" });
            s0.Objectives.Add(new QuestObjectiveData { ID = "opt", Text = "Bonus", Optional = true });
            sd.Quest.Stages.Add(s0);
            sd.Quest.Stages.Add(new QuestStageData { ID = "turn_in" }); // terminal, no objectives
            StoryletRegistry.Register(sd);

            // ── Lifecycle cells ──
            sp.StartQuest(new QuestState { QuestId = Q });
            Cell("start_active", "true", sp.IsQuestActive(Q), sp.IsQuestActive(Q));

            sp.FinishObjective(Q, "opt"); // optional, any order
            Cell("optional_finished", "true", sp.IsObjectiveFinished(Q, "opt"), sp.IsObjectiveFinished(Q, "opt"));
            Cell("optional_no_advance", "0", sp.GetQuestState(Q)?.CurrentStageIndex, sp.GetQuestState(Q)?.CurrentStageIndex == 0);

            sp.FinishObjective(Q, "a"); // required #1 (runs OnEnter SetFact)
            Cell("a_finished", "true", sp.IsObjectiveFinished(Q, "a"), sp.IsObjectiveFinished(Q, "a"));
            Cell("a_onenter_ran", "1", ns.GetFact("qb_" + runId), ns.GetFact("qb_" + runId) == 1);
            Cell("a_no_advance", "0", sp.GetQuestState(Q)?.CurrentStageIndex, sp.GetQuestState(Q)?.CurrentStageIndex == 0);

            sp.FinishObjective(Q, "b"); // last required → advance
            Cell("all_required_advance", "1", sp.GetQuestState(Q)?.CurrentStageIndex, sp.GetQuestState(Q)?.CurrentStageIndex == 1);
            Cell("objectives_cleared_on_advance", "0", sp.GetQuestState(Q)?.FinishedObjectives.Count, (sp.GetQuestState(Q)?.FinishedObjectives.Count ?? -1) == 0);

            sp.OnTickEnd(ns); // terminal stage (no objectives, empty triggers) → completes
            Cell("completed", "true", sp.IsQuestCompleted(Q), sp.IsQuestCompleted(Q));

            bool deedLogged = ns.EventLog.Contains(deed);
            Cell("accomplishment_logged", "true", deedLogged, deedLogged);

            Cell("event_QuestStarted", "true", probe.Seen.Contains("QuestStarted:" + Q), probe.Seen.Contains("QuestStarted:" + Q));
            Cell("event_QuestObjectiveFinished", "true", probe.Seen.Contains("QuestObjectiveFinished:" + Q), probe.Seen.Contains("QuestObjectiveFinished:" + Q));
            Cell("event_QuestStageAdvanced", "true", probe.Seen.Contains("QuestStageAdvanced:" + Q), probe.Seen.Contains("QuestStageAdvanced:" + Q));
            Cell("event_QuestCompleted", "true", probe.Seen.Contains("QuestCompleted:" + Q), probe.Seen.Contains("QuestCompleted:" + Q));

            // ── CONTROL (Rule 3): an objective never driven stays unfinished. ──
            string qc = "QBenchControl_" + runId;
            var sdc = new StoryletData { ID = qc, Quest = new QuestData() };
            var sc0 = new QuestStageData { ID = "s0" };
            sc0.Objectives.Add(new QuestObjectiveData { ID = "never" });
            sdc.Quest.Stages.Add(sc0);
            StoryletRegistry.Register(sdc);
            sp.StartQuest(new QuestState { QuestId = qc });
            Cell("CONTROL_undriven_unfinished", "false", sp.IsObjectiveFinished(qc, "never"), sp.IsObjectiveFinished(qc, "never") == false);

            // ── Fail tracking (Q6). ──
            string qf = "QBenchFail_" + runId;
            var sdf = new StoryletData { ID = qf, Quest = new QuestData { Stages = new List<QuestStageData> { new QuestStageData { ID = "s0" } } } };
            StoryletRegistry.Register(sdf);
            sp.StartQuest(new QuestState { QuestId = qf });
            sp.FailQuest(qf);
            Cell("fail_tracked", "true", sp.IsQuestFailed(qf), sp.IsQuestFailed(qf));

            // ── Authored content loads (the EnchiridionQuest example). ──
            var content = StoryletRegistry.FindQuest("EnchiridionQuest");
            int contentObjs = content?.Stages != null && content.Stages.Count > 0
                ? content.Stages[0].Objectives.Count : -1;
            Cell("content_EnchiridionQuest_loaded", "3", contentObjs, contentObjs == 3);

            ctx.Log($"[QuestBench] run {runId}: {pass} pass / {fail} fail. " +
                $"Audit: diag_query category=questbench kind=Cell (filter runId={runId}, assert no pass=false).");
        }
    }
}
