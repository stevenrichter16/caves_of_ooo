# Q3 — Parallel objectives inside stages

> Phase Q3 of `Docs/QUEST-SYSTEM-QUD-PARITY.md`. Delivers Qud's
> **parallel-objective** gameplay parity (finish objectives in any order,
> Optional/Hidden flags) **additively** — stages stay, each may gain a
> list of objectives. Chosen over a literal flat-step rewrite to avoid
> the ~60-test / save-format break (see git history of the superseded
> QUEST-FLAT-STEP-MODEL plan). **Status: in progress (2026-05-23).**

## Design (additive, backward-compatible)

```
QuestStageData {                      // EXISTING + one new field
  string ID; Triggers[]; OnEnter[];
  List<QuestObjectiveData> Objectives // NEW (optional/empty = today's behavior)
}
QuestObjectiveData {                  // NEW
  string ID; string Text;             // Text = quest-log display
  List<ConversationParam> Triggers;   // predicates that finish THIS objective
  List<ConversationParam> OnEnter;    // effects when it finishes (e.g. AwardXP)
  bool Optional; bool Hidden;
}
QuestState {                          // EXISTING + one new field
  QuestId; CurrentStageIndex; EnteredStageAtTurn;
  HashSet<string> FinishedObjectives  // NEW: finished objective IDs in the
}                                     //      CURRENT stage; cleared on advance
```

**Dispatch (`StoryletPart.OnTickEnd`), backward-compatible:**
- Stage with **no** Objectives → unchanged: eval `stage.Triggers` → advance.
- Stage **with** Objectives → eval each *unfinished* objective's `Triggers`;
  finish those that pass (add to `FinishedObjectives`, run its `OnEnter`);
  when **all non-`Optional`** objectives are finished → advance the stage
  (clear `FinishedObjectives`, run `stage.OnEnter`). `stage.Triggers`, if
  present alongside objectives, is an additional gate.
- JsonUtility deserialization is automatic for the new `[Serializable]`
  nested types (verified — `StoryletRegistry` uses `JsonUtility.FromJson`).
- Save: append `FinishedObjectives` to the per-quest record with
  EOF-defensive load (old saves → empty set). **No hard save break.**

**API:** `FinishObjective(questId, objId, actor)`,
`IsObjectiveFinished(questId, objId)`.
**Conversation:** `FinishObjective` action (arg `questId:objId[~objId2…]`,
`~`-multi-finish); `IfObjectiveFinished` predicate (arg `questId:objId`) +
its auto-inverse `IfNotObjectiveFinished` (the registry's IfNot* mechanism
— *not* `IfObjectiveNotFinished`). Keep `IfQuest*`.
**Quest Log (Q1):** the current stage expands to show its objectives as
Done/Pending sub-rows (snapshot/builder enhancement; renderer reuses the
proven `GetTextTile` + marker path).

## Sub-milestones (smallest-blast-radius-first, each TDD'd + committed)

- **Q3.1** — Data model: `QuestObjectiveData` + `QuestStageData.Objectives`
  + `QuestState.FinishedObjectives` + save round-trip (EOF-defensive). TDD.
- **Q3.2** — Dispatch + API: `FinishObjective`, completion→advance,
  back-compat for no-objective stages. RED tests + counter-check
  (no-objective stage behaves exactly as before).
- **Q3.3** — Conversation `FinishObjective` action +
  `IfObjectiveFinished`/`…NotFinished` predicates (registry-validated).
- **Q3.4** — Quest Log: current-stage objective sub-rows in the snapshot/
  builder (+ renderer indent). TDD the builder.
- **Q3.5** — Content: a multi-objective quest (showcase) + live PlayMode
  bench; adversarial sweep (re-finish a finished objective; all-Optional
  stage auto-advances; save round-trip of the finished-set).

## Deferred items

| Item | Why | Phase |
|---|---|---|
| Objective `Ordinal` (explicit display order) | render in list order for now | later/UI |
| Objective `Failed` state | fail-tracking is its own phase | Q6 |
| Objective `Collapse` (fold finished in log) | UI nicety | later/UI |
| Dispatch + `FinishObjective` API | next sub-milestone | Q3.2 |
| Conversation `FinishObjective`/`IfObjective*` | | Q3.3 |
| Quest-log objective sub-rows | | Q3.4 |

## Implementation log

**Q3.1 — data model + state + save (DONE).**
- `QuestObjectiveData` (ID/Text/Triggers/OnEnter/Optional/Hidden) +
  `QuestStageData.Objectives`; `QuestState.FinishedObjectives`.
- Save: a SEPARATE trailing objectives section (keyed by quest), so the
  pre-Q3 byte layout is unchanged; Load reads it under an EOF guard
  mirroring the QS.2 completed-quests pattern → pre-Q3 saves load fine.
- **CLAUDE.md self-review (§5):**
  - 🟡→FIXED: the pre-Q3-save back-compat path (the whole reason for a
    trailing section) was initially untested. Added
    `Load_PreQ3Save_NoObjectivesSection_DefaultsEmptyWithoutThrowing`,
    which hand-writes the old byte layout + asserts the EOF guard.
  - 🔵 deferred: Qud `QuestStep` has `Ordinal`/`Failed`/`Collapse` that
    `QuestObjectiveData` omits (see Deferred table) — intentional subset.
  - ✅ Symmetry (Save writes / Load reads the section in mirror),
    counter-checks (omitted→empty-not-null; multi-quest no cross-wire),
    doc-vs-impl, Qud-parity (objectives ≈ scoped QuestSteps) all pass.
  - Adversarial: save/load-reach surface covered by round-trip +
    multi-quest + pre-Q3 tests; full adversarial file deferred to Q3.5
    (after dispatch adds the stacking/re-finish surface).
- **Tests:** 6 (JSON deserialize + omitted-counter + 3 round-trip +
  pre-Q3 back-compat). First 5 ran GREEN; pre-Q3 test added post-review.
- Files: `StoryletData.cs`, `QuestState.cs`, `StoryletPart.cs` (Save/Load),
  NEW `QuestParallelObjectivesModelTests.cs`.

**Q3.2 — dispatch + API (DONE).**
- `StoryletPart.FinishObjective(questId, objId, actor)`: marks finished
  (idempotent), runs the objective's `OnEnter` (player = listener),
  emits `quest/ObjectiveFinished`, then advances the stage via
  `AdvanceQuestStage` when `AllRequiredObjectivesFinished` (Optional
  objectives don't gate — Qud `CheckQuestFinishState` parity).
  `IsObjectiveFinished`. `AdvanceQuestStage` now clears
  `FinishedObjectives` (objectives are stage-scoped).
- `OnTickEnd` pass 1B branches: objective-based stage → snapshot each
  unfinished objective whose `Triggers` pass; no-objective stage →
  unchanged legacy stage-trigger advance. New pass 2C finishes the
  snapshot. Single-pass discipline preserved (advance doesn't cascade
  into the new stage's objectives the same tick).
- **CLAUDE.md self-review (§5):**
  - 🔵 documented edge: if the LAST required objective and an Optional
    one are both eligible the same tick, whether the optional's OnEnter
    runs is declaration-order-dependent (finishing the last required
    advances+clears, dropping not-yet-processed snapshot entries).
    Required objectives always all finish before advance. Acceptable v1;
    a deferred refinement could defer advancement to after the batch.
  - ⚪ `~`-delimited multi-finish (Qud `FinishQuestStep`) deferred to the
    Q3.3 conversation action (it will split + call `FinishObjective` per id).
  - ✅ Symmetry (pass 2C mirrors 2B; ObjectiveFinished diag matches the
    StageAdvanced/Completed shape), counter-checks (optional-doesn't-gate;
    no-objective-legacy-unchanged; idempotent; not-in-stage), doc-vs-impl,
    Qud-parity (`FinishObjective` ≈ `FinishQuestStep`+`CheckQuestFinishState`).
- **Tests:** 8 (`QuestObjectiveDispatchTests`) — mark / advance+clear /
  optional-doesn't-gate / idempotent / not-in-stage / OnEnter-runs /
  tick-finishes+advances / no-objective-legacy-unchanged.
- Files: `StoryletPart.cs` (FinishObjective + dispatch), NEW
  `QuestObjectiveDispatchTests.cs`.

**Q3.3 — conversation integration (DONE).**
- `ConversationActions.FinishObjective` (arg `questId:objId[~objId2…]`):
  splits the `~`-list (Qud `FinishQuestStep("a~b~c")` parity) and calls
  `StoryletPart.Current.FinishObjective` per id with `listener` as actor.
  Null/`:`-less arg → no-op.
- `ConversationPredicates.IfObjectiveFinished` (arg `questId:objId`) →
  `IsObjectiveFinished`; auto-inverse `IfNotObjectiveFinished` via the
  IfNot* mechanism (verified — `StartsWith("IfNot")` strips to the `If`
  base + negates).
- **Self-review (§5):** mirrors the `AdvanceQuestStage` action +
  `IfQuestStage` predicate patterns (arg `questId:…`, `StoryletPart.Current`,
  listener=actor). Counter-checks: malformed-arg no-op; predicate
  false-when-unfinished; auto-inverse both directions. No 🔴/🟡.
- **Tests:** 7 (`QuestObjectiveConversationTests`) — single + `~`-multi
  finish, all-required-advances, malformed no-op, predicate true/false,
  auto-inverse.
- Files: `ConversationActions.cs`, `ConversationPredicates.cs`, NEW
  `QuestObjectiveConversationTests.cs`.

**Q3 — adversarial sweep (DONE, CLAUDE.md gate).** Dedicated
`QuestObjectiveAdversarialTests` (21 tests) — the gate applies (≥2
surfaces: parser, state atomicity, stacking, save/load reach, diag).
Surfaces probed:
- **Parser** (action `questId:objId[~…]`, predicate `questId:objId`):
  empty-objId-after-colon, double/trailing `~` (skip empties), null/
  whitespace, predicate malformed → false.
- **Boundary**: null/empty questId+objId, inactive quest, no-objectives
  stage, `CurrentStageIndex` out-of-bounds (no crash).
- **Stacking**: re-finish → false; same objId across stages tracked
  per-stage (cleared on advance).
- **Save/load reach**: orphan objectives-section key (corrupt save) loads
  without crash; objectives cleared-on-advance round-trip empty; 50-
  objective set round-trips; finished-state is registry-independent.
- **Diag emission** (was an untested coverage gap): `quest/ObjectiveFinished`
  fires once on a real finish; no-op paths (not-found / inactive) emit
  nothing.
- **Dispatch**: cross-quest finishing; **single-pass cascade-prevention**
  (advance doesn't finish the new stage's objective the same tick); Hidden
  non-Optional counts as required; all-Optional stage advances on any finish.

**Result: 0 production bugs.** The sweep surfaced exactly **1 bug — in a
test, not the code**: `Dispatch_…NoCascade…` initially built a 3-stage
quest but expected completion after 2 ticks (off-by-one — tick 2 only
reaches the last stage). The cascade-prevention it was actually probing
PASSED; fixed the test to a 2-stage quest. (Per CLAUDE.md: every failure
investigated; this one was a test-expectation error, the production logic
is correct.) The genuine coverage win was the **diag-emission pin** — the
`quest/ObjectiveFinished` contract had no test before this sweep.

## Q3.4 — quest-log objective sub-rows (DONE, 2026-05-23)

Extended the Q1 quest log so the CURRENT stage expands into its objectives.
- `QuestLogObjectiveRow {ObjectiveId, Text, Done, Optional}` +
  `QuestLogActiveEntry.CurrentObjectives` (snapshot, additive optional
  ctor arg — Q1 tests unaffected).
- `QuestLogStateBuilder`: builds the current stage's objective rows;
  `Done` = in `FinishedObjectives`; **Hidden + unfinished filtered out**
  (revealed once finished).
- `QuestLogUI`: renders objectives indented under the current stage —
  `*` green (done) / `-` grey (pending), `(optional)` suffix, via the
  legible `GetTextTile` path.
- **Tests:** 4 (`QuestLogStateBuilderTests`) — done-status, hidden-until-
  done (+counter), no-objectives→empty, optional flag. Suite 174/174.
- **PlayMode-verified:** screenshot shows the current stage expanding to
  `* Cross the candy bridge` (green) / `- Find the iron key` (grey) /
  `- Loot the chest (optional)`, with a completed quest below. Legible
  text; persistent HUD band at the bottom (expected, all overlays).

## PlayMode runtime verification (Q3, 2026-05-23)

EditMode tests stub `TurnManager`/bootstrap/content-load; a live PlayMode
check exercised the full runtime. Seeded a 2-stage quest — stage 0 with a
required objective `kill` (OnEnter `SetFact boss_defeated:1`) + an optional
`loot`; stage 1 terminal — then finished `kill` via the REAL
`ConversationActions.Execute("FinishObjective", …, "RuntimeQ:kill")` path
and ticked. Result (all expected):

```
stageAfterKill=1 | bossFact=1 | lootFinished=False | completed=True
diag: objFin=1, stgAdv=1, comp=1 | player(LocalPlayer)=True
```

Confirms in the live runtime: `StoryletPart.Current`/`LocalPlayer`
wiring; the conversation action → `FinishObjective` → stage advance;
per-objective `OnEnter` effects actually run (`SetFact` landed in
`NarrativeStatePart.Current`); optional-doesn't-gate; the tick dispatch
completing the quest via the legacy (no-objective) s1 path; and all three
`quest/*` diag records emitted by the real `Diag`. (Disposable session;
no content/scene committed.)

## Test-coverage audit (all Qx phases)

| Phase | Tests | Notes |
|---|---|---|
| Q1 Quest Log UI | `QuestLogStateBuilderTests` (9) | state/status/counter/defensive; renderer is PlayMode-screenshot-verified (presentation glue, not unit-tested) |
| Q3.1 data model | `QuestParallelObjectivesModelTests` (6) | JSON deserialize + omitted-counter + save round-trip + pre-Q3 back-compat |
| Q3.2 dispatch/API | `QuestObjectiveDispatchTests` (8) | mark/advance/optional/idempotent/not-in-stage/OnEnter/tick/legacy |
| Q3.3 conversation | `QuestObjectiveConversationTests` (7) | single+`~`multi finish / advance / malformed / predicate ±/auto-inverse |
| Q3 adversarial | `QuestObjectiveAdversarialTests` (21) | parser/boundary/stacking/save-reach/diag/dispatch |

Quest/storylet suite total **162/162 GREEN** (incl. the pre-existing
FormatVersion fix). Q3 objective feature: **42 tests**. Q2 (display names)
was skipped by user choice. **Still deferred** (not regressions, scoped
out): Q3.4 quest-log objective sub-rows, Q3.5 content example + live
PlayMode bench (the bench is where objective rewards/OnEnter get an
end-to-end runtime check beyond the unit stubs).

**Pre-existing fix (separate commit):** `SaveWriter_FormatVersion_IsThree`
asserted `==3` but the constant is `4` on main (bumped after the M2 test,
never updated — red on main, unrelated to Q3). Refreshed to track the
current version (renamed `…_IsCurrentSchemaVersion`, asserts 4).
