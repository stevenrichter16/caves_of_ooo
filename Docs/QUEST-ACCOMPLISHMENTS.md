# Q7 — Quest accomplishments (journal history)

> Phase Q7 of `Docs/QUEST-SYSTEM-QUD-PARITY.md`. On quest completion,
> record a flavor "deed" line into the narrative event log — Qud parity
> with `Quest.Accomplishment` + `JournalAPI.AddAccomplishment` on
> `FinishQuest` (the hagiograph/mural history). **Status: Q7.1 done.**

## Design (additive, minimal)

- `QuestData.Accomplishment` (string) — author-written deed text on the
  quest blueprint. JsonUtility-deserialized; empty ⇒ nothing logged.
- `StoryletPart.CompleteQuest`: after the existing completion side-effects
  (mark + diag + `QuestCompleted` event), if the quest's `Accomplishment`
  is set, `NarrativeStatePart.Current?.LogEvent(accomplishment)` + emit a
  `quest/Accomplishment` diag. This is the single completion choke point —
  the auto-complete path (finishing the last objective/stage) also routes
  through `CompleteQuest`, so it logs too.
- **No new save format:** `NarrativeStatePart._eventLog` already
  round-trips (NarrativeStatePart.cs:85-97), so accomplishments persist
  for free. The parity doc's "atop `NarrativeStatePart._eventLog`" path.

## Verification sweep
- ✅ `NarrativeStatePart.LogEvent(string)` (public) appends to the
  persisted `_eventLog`; `EventLog` reads it. `NarrativeStatePart` is
  referenced unqualified in `StoryletPart` (the INarrativeReactor param).
- ✅ `CompleteQuest` already resolves the blueprint `QuestData` (for
  `totalStages`), so `quest.Accomplishment` is in hand. Valid after
  `MarkQuestCompleted` (it's the registry blueprint, not runtime state).
- Decision: route to the event log (parity doc) rather than a dedicated
  StoryletPart list — zero new save surface, and the narrative log is the
  natural "history of deeds" home.

## Tests (Q7.1)
complete-with-accomplishment → logged + `quest/Accomplishment` diag;
no-accomplishment → nothing logged (counter); null NarrativeState → no
throw; idempotent (re-complete logs once); auto-complete via last
objective also logs.

## Deferred
- Per-quest `Name`/`Gospel`/`HagiographCategory`/mural-weight (Qud has
  these) — only `Accomplishment` for now; the rest is Q2-metadata /
  cosmetic, no current consumer.
- A player-facing "accomplishments / journal" screen — out of scope (the
  data is queryable via `NarrativeStatePart.EventLog`).

## Self-review (Methodology Template §5)
*(Added retroactively in the QUEST-METHODOLOGY-AUDIT.md pass — the
counter-checks existed in `QuestAccomplishmentTests`; this records the §5
review the doc had omitted.)*
- 🔵 Single completion choke point: both the explicit `CompleteQuest`
  action AND the auto-complete (last objective/stage) route through
  `CompleteQuest`, so the accomplishment logs exactly once on either path —
  not duplicated. Pinned by `AutoComplete_ViaLastObjective_LogsAccomplishment`
  + `CompleteQuest_Idempotent_LogsAccomplishmentOnce`.
- ✓ Counter-checks: no-accomplishment quest → nothing logged
  (`CompleteQuest_NoAccomplishment_LogsNothing`); null `NarrativeStatePart`
  → no throw (`CompleteQuest_NullNarrativeState_NoThrow`); re-complete logs
  once (idempotent). Every positive assertion is paired.
- 🧪 No player-facing journal UI yet — the deed is `EventLog`-queryable
  only. Deferred (documented above); not a correctness gap.
- ✓ Observability: `CompleteQuest` emits `quest/Accomplishment`; the gate's
  reject path now emits `quest/Rejected` (observability-alignment fix, see
  QUEST-METHODOLOGY-AUDIT.md). No 🔴/🟡.

## Implementation log
- **Q7.1 (DONE, 2026-05-24):** `QuestData.Accomplishment` +
  `CompleteQuest` logs to `NarrativeStatePart` + `quest/Accomplishment`
  diag. Tests in `QuestAccomplishmentTests`. (filled at commit)
