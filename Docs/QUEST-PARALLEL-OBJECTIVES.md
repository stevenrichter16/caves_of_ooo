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
**Conversation:** `FinishObjective` action; `IfObjectiveFinished` /
`IfObjectiveNotFinished` predicates (keep `IfQuest*`).
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

**Pre-existing fix (separate commit):** `SaveWriter_FormatVersion_IsThree`
asserted `==3` but the constant is `4` on main (bumped after the M2 test,
never updated — red on main, unrelated to Q3). Refreshed to track the
current version (renamed `…_IsCurrentSchemaVersion`, asserts 4).
