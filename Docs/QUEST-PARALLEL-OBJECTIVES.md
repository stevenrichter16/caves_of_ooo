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
