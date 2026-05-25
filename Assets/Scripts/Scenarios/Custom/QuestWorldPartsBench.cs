using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Q5 — DETERMINISTIC SELF-AUDITING BENCH for the WORLD-OBJECT quest Parts
    /// (Docs/QUEST-WORLD-PARTS.md; workflow step 7). One press of Play drives a
    /// multi-objective quest through the THREE world-side completion mechanisms
    /// against the REAL runtime — the real PickupCommand fires the M1 "Taken"
    /// event, real entities carry the Parts, the real StoryletPart tracks state
    /// — and emits one machine-checkable <c>questbench/Cell</c> diag per
    /// assertion (incl. a CONTROL row), stamped with a per-run <c>runId</c>.
    ///
    /// <para><b>Audit it:</b> after launching,
    /// <c>diag_query category=questbench kind=Cell</c> — scope to the newest
    /// <c>runId</c> (read the <c>questbench/MatrixAuditRun</c> marker, which
    /// carries <c>bench=worldparts</c>) and assert no <c>pass=false</c>.</para>
    ///
    /// <para>The three mechanisms, each finishing a DIFFERENT required
    /// objective so none advances the stage until all are done (keeping each
    /// "finished" observable before the advance clears the set):
    /// <list type="bullet">
    /// <item>QuestStarter — picking up a scroll STARTS the quest.</item>
    /// <item>CompleteObjectiveOnTaken — picking up a relic finishes an objective.</item>
    /// <item>FinishObjectiveWhenSlain — slaying a guard finishes an objective.</item>
    /// </list>
    /// Plus a direct FinishObjective (the conversation-action path) finishes the
    /// last required objective, triggering the stage advance.</para>
    /// </summary>
    [Scenario(
        name: "Quest World Parts Bench",
        category: "Quest",
        description: "Self-auditing: drives QuestStarter + CompleteObjectiveOnTaken + FinishObjectiveWhenSlain end-to-end in the live runtime (real PickupCommand) and emits one questbench/Cell diag per assertion. diag_query category=questbench.")]
    public class QuestWorldPartsBench : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            string runId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
            Diag.Record("questbench", "MatrixAuditRun", payload: new { runId, bench = "worldparts" });
            int pass = 0, fail = 0;

            void Cell(string cell, string expected, object actual, bool ok)
            {
                if (ok) pass++; else fail++;
                Diag.Record("questbench", "Cell", payload:
                    new { runId, bench = "worldparts", cell, expected, actual = actual?.ToString() ?? "null", pass = ok });
            }

            // Rule 4 — guard preconditions LOUDLY.
            var sp = StoryletPart.Current;
            if (sp == null) { Cell("precondition_storyletpart", "non-null", "null", false); ctx.Log("[QWPBench] SKIPPED: StoryletPart.Current null (not in Play?)."); return; }
            var player = StoryletPart.LocalPlayer ?? ctx.PlayerEntity;
            if (player == null) { Cell("precondition_player", "non-null", "null", false); ctx.Log("[QWPBench] SKIPPED: no player."); return; }
            if (StoryletPart.LocalPlayer == null) StoryletPart.LocalPlayer = player; // gate needs it
            var zone = ctx.Zone;
            if (zone == null) { Cell("precondition_zone", "non-null", "null", false); ctx.Log("[QWPBench] SKIPPED: no zone."); return; }
            if (player.GetPart<InventoryPart>() == null) { Cell("precondition_inventory", "non-null", "null", false); ctx.Log("[QWPBench] SKIPPED: player has no InventoryPart."); return; }

            var (px, py) = zone.GetEntityPosition(player);
            var exec = new InventoryCommandExecutor();

            // ── Build the quest (NOT started — the starter item starts it). ──
            // Three REQUIRED objectives so finishing any two does NOT advance,
            // keeping each individual finish observable. Plus an OPTIONAL one
            // that is never driven (the control's twin, also a non-gate proof).
            string Q = "QWP_" + runId;
            var sd = new StoryletData { ID = Q, Quest = new QuestData() };
            var s0 = new QuestStageData { ID = "gather" };
            s0.Objectives.Add(new QuestObjectiveData { ID = "take_relic" });   // CompleteObjectiveOnTaken
            s0.Objectives.Add(new QuestObjectiveData { ID = "slay_guard" });   // FinishObjectiveWhenSlain
            s0.Objectives.Add(new QuestObjectiveData { ID = "turn_in" });      // direct FinishObjective (convo path)
            s0.Objectives.Add(new QuestObjectiveData { ID = "bonus", Optional = true }); // never driven
            sd.Quest.Stages.Add(s0);
            sd.Quest.Stages.Add(new QuestStageData { ID = "complete" }); // terminal
            StoryletRegistry.Register(sd);

            // ── QuestStarter: pick up the scroll → quest starts. ──
            Cell("before_starter_inactive", "false", sp.IsQuestActive(Q), !sp.IsQuestActive(Q));
            var scroll = new Entity { ID = "qwp_scroll_" + runId, BlueprintName = "QuestScroll" };
            scroll.Tags["Item"] = "";
            scroll.AddPart(new PhysicsPart { Takeable = true, Weight = 1 });
            scroll.AddPart(new QuestStarter { Quest = Q });
            zone.AddEntity(scroll, px, py);
            exec.Execute(new PickupCommand(scroll), new InventoryContext(player, zone));
            Cell("starter_started_quest", "true", sp.IsQuestActive(Q), sp.IsQuestActive(Q));

            // ── CompleteObjectiveOnTaken: pick up the relic → take_relic done. ──
            var relic = new Entity { ID = "qwp_relic_" + runId, BlueprintName = "Relic" };
            relic.Tags["Item"] = "";
            relic.AddPart(new PhysicsPart { Takeable = true, Weight = 1 });
            relic.AddPart(new CompleteObjectiveOnTaken { Quest = Q, Objective = "take_relic" });
            zone.AddEntity(relic, px, py);
            exec.Execute(new PickupCommand(relic), new InventoryContext(player, zone));
            Cell("taken_finished_objective", "true", sp.IsObjectiveFinished(Q, "take_relic"), sp.IsObjectiveFinished(Q, "take_relic"));

            // ── FinishObjectiveWhenSlain: slay the guard → slay_guard done. ──
            var guard = new Entity { ID = "qwp_guard_" + runId, BlueprintName = "Guard" };
            guard.AddPart(new FinishObjectiveWhenSlain { Quest = Q, Objective = "slay_guard" });
            zone.AddEntity(guard, px, py);
            var died = GameEvent.New("Died");
            died.SetParameter("Killer", (object)player);
            died.SetParameter("Target", (object)guard);
            guard.FireEventAndRelease(died);
            Cell("slain_finished_objective", "true", sp.IsObjectiveFinished(Q, "slay_guard"), sp.IsObjectiveFinished(Q, "slay_guard"));

            // ── Two required done, one pending → stage must NOT have advanced. ──
            Cell("no_advance_until_all_required", "0", sp.GetQuestState(Q)?.CurrentStageIndex, (sp.GetQuestState(Q)?.CurrentStageIndex ?? -1) == 0);

            // ── Direct FinishObjective (conversation-action path) → last required
            //    → all done → advance to the terminal stage. ──
            sp.FinishObjective(Q, "turn_in", player);
            Cell("all_required_advanced", "1", sp.GetQuestState(Q)?.CurrentStageIndex, (sp.GetQuestState(Q)?.CurrentStageIndex ?? -1) == 1);

            // ── CONTROL (Rule 3): a separate quest's undriven objective stays
            //    unfinished — proves the instrument discriminates. ──
            string qc = "QWPControl_" + runId;
            var sdc = new StoryletData { ID = qc, Quest = new QuestData() };
            var sc0 = new QuestStageData { ID = "s0" };
            sc0.Objectives.Add(new QuestObjectiveData { ID = "never" });
            sdc.Quest.Stages.Add(sc0);
            StoryletRegistry.Register(sdc);
            sp.StartQuest(new QuestState { QuestId = qc });
            Cell("CONTROL_undriven_unfinished", "false", sp.IsObjectiveFinished(qc, "never"), sp.IsObjectiveFinished(qc, "never") == false);

            ctx.Log($"[QWPBench] run {runId}: {pass} pass / {fail} fail. " +
                $"Audit: diag_query category=questbench kind=Cell (runId={runId}, bench=worldparts; assert no pass=false).");
        }
    }
}
