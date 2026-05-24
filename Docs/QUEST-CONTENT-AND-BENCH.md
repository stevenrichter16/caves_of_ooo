# Q3.5 — Authored content example + deterministic self-auditing bench

> Phase Q3.5 of `Docs/QUEST-SYSTEM-QUD-PARITY.md`. Two deliverables that
> close the quest buildout (Q1–Q7) with (a) a real authored multi-objective
> quest that exercises the JSON content path end-to-end, and (b) a
> **deterministic self-auditing bench** (per `CLAUDE.md` →
> `Docs/MCP_PlayMode_Testing_Strategy.md`) that drives a quest through its
> WHOLE lifecycle in the live runtime and emits one machine-checkable diag
> record per assertion. **Status: Q3.5 done — live matrix confirmed 17/0.**

## Why a bench (not just more unit tests)

The quest system is *measurable* (stage indices, objective-finished bools,
fact deltas, event firings) — exactly the class `CLAUDE.md` says MUST get a
**self-auditing scenario**, not a player-exercised demo. EditMode tests stub
the runtime: they construct a bare `StoryletPart` and never touch
`StoryletRegistry` JSON loading, `NarrativeStatePart` wiring,
`ConversationActions` OnEnter effects against a live narrative state, or the
quest `GameEvent`s dispatched on the real player Entity. The bench exercises
all of that in one launch and turns "the unit tests probably cover this"
into a one-query end-to-end audit.

## Deliverables

### 1. `EnchiridionQuest.json` (authored content)
`Assets/Resources/Content/Data/Storylets/EnchiridionQuest.json` — a
multi-objective, multi-stage quest using only the shipped vocabulary:
- Stage `recover` with three parallel objectives:
  - `find_enchiridion` — trigger `IfHaveItem:Enchiridion`, OnEnter `AwardXP:150`
  - `best_the_guardian` — trigger `IfFact:guardian_bested:1`
  - `loot_the_vault` — **Optional**, trigger `IfHaveItem:VaultKey`, OnEnter `GiveDrams:25`
- Terminal stage `return_to_guardian` — OnEnter `AwardXP:300` + `GiveDrams:100`
- `Accomplishment` deed text (Q7) recorded on completion.

This is the first piece of content proving the Q3 parallel-objective schema,
Q7 accomplishment, and the per-objective trigger/OnEnter vocabulary all
deserialize and run from a real Resources JSON file (not a hand-built
`StoryletData` in a test).

### 2. `QuestSystemBench.cs` (self-auditing scenario)
`Assets/Scripts/Scenarios/Custom/QuestSystemBench.cs` —
`[Scenario(name:"Quest System Bench", category:"Quest")]`. One press of Play
(or the sanctioned `ScenarioContext.FromLiveGame()` MCP path) drives a
**unique-per-run** quest `QBench_<runId>` through its full lifecycle and
emits one `questbench/Cell` diag per assertion. Audit with
`diag_query category=questbench kind=Cell`.

## Adherence to the eight self-auditing rules
(`Docs/MCP_PlayMode_Testing_Strategy.md` §Deterministic Self-Auditing Scenarios)

1. **Synthetic, not manual** — `Apply()` calls the quest API directly
   (`StartQuest`/`FinishObjective`/`OnTickEnd`); no player aiming/RNG.
2. **Snapshot → stimulate → measure → restore** — measures the whole
   pipeline (registry → state → events → narrative log); quests are
   throwaway per-run so nothing leaks into the player's real save.
3. **Control row** — `CONTROL_undriven_unfinished`: an objective that is
   never driven stays `IsObjectiveFinished == false`. Proves the instrument
   discriminates (not vacuously green).
4. **Guard preconditions LOUDLY** — null `StoryletPart`/`NarrativeStatePart`
   emit an explicit `precondition_*` FAILED cell + early-return, never a
   silent neutral pass.
5. **One machine-checkable record per cell** — `Cell(name, expected, actual,
   ok)` → `Diag.Record("questbench","Cell", payload:{runId,cell,expected,actual,pass})`.
6. **Neutralize confounds** — unique per-run quest IDs (`QBench_<runId>`,
   `QBenchControl_<runId>`, `QBenchFail_<runId>`) + a per-run fact key
   `qb_<runId>` so a re-run can't read a previous run's state.
7. **Validate-before-merge** — the EditMode world has no runtime registries,
   so the real proof is a live Play run + diag audit. Held the merge until
   the live matrix confirmed (see Validation below).
8. **Stamp every run** — emits a `questbench/MatrixAuditRun` marker carrying
   `runId`; the audit scopes Cell records to the newest `runId` so the
   persistent diag buffer (domain-reload-off) can't show stale numbers.

## Cells (17)
`start_active`, `optional_finished`, `optional_no_advance` (Optional doesn't
gate), `a_finished`, `a_onenter_ran` (OnEnter `SetFact` ran), `a_no_advance`,
`all_required_advance` (stage advances only when all *required* objectives
done), `objectives_cleared_on_advance`, `completed`, `accomplishment_logged`
(Q7 deed in `NarrativeStatePart.EventLog`), four event cells
(`QuestStarted`/`QuestObjectiveFinished`/`QuestStageAdvanced`/`QuestCompleted`
fired on the player), `CONTROL_undriven_unfinished`, `fail_tracked` (Q6),
`content_EnchiridionQuest_loaded` (authored JSON deserialized with 3
objectives in stage 0).

## Validation (Rule 7 — live Play run, 2026-05-24)
Ran via `ScenarioContext.FromLiveGame()` + `QuestSystemBench().Apply(ctx)` in
a live Play session, then `DiagQuery` over `questbench/Cell`:

```
RESULT|runId=374adb93|cells=17|fails=0
```

All 17 cells `pass=true`, including the CONTROL row and the authored-content
cell. Console: 0 errors.

**Honesty bounds:**
- **Can verify (script-observable):** all 17 lifecycle invariants above —
  start/optional-ordering/required-gating/OnEnter-effect/advance-clearing/
  completion/accomplishment-log/4 quest events/fail-tracking/JSON-content-load.
  The CONTROL row proves the harness isn't vacuously passing.
- **Cannot verify (visual/feel):** the bench does NOT exercise the quest-log
  UI rendering (that was covered visually in Q1 via PlayMode screenshot) or
  the in-game conversational flow that would fire these objectives during
  real play — only the underlying state machine + content + events.

## Divergences
| Item | CoO | Qud | Note |
|---|---|---|---|
| Bench harness | CoO-original (scenario + diag) | n/a | Test infra, not a parity surface |
| Content shape | Stage→Objectives JSON | Quest→Steps XML | Q3 documented schema divergence; bench just exercises it |

## Deferred
- A bench cell for the quest-log UI snapshot (the UI has its own EditMode
  builder tests + Q1 visual proof; not re-driven here).
- Driving objectives through the *conversation* path inside the bench (the
  conversation vocabulary is unit-tested in `QuestObjectiveConversationTests`;
  the bench drives the API directly to keep the matrix deterministic).

## Implementation log
- **Q3.5 (DONE, 2026-05-24):** `EnchiridionQuest.json` authored content +
  `QuestSystemBench.cs` self-auditing scenario + `"questbench"` added to
  `Diag.DefaultOnCategories`. Live matrix 17/0 (runId 374adb93), console
  clean. Bench compiled with 0 CS errors; smoke is implicit (Apply runs in
  the live runtime without throwing — the FromLiveGame path).
