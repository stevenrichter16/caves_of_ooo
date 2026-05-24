# Q6 — Failed-quest tracking (PLAN + LOG)

> Phase Q6 of `Docs/QUEST-SYSTEM-QUD-PARITY.md`. Resolves the QUEST-SYSTEM.md
> 🟡: today `FailQuest` (Q4.1) removes the quest from active + emits diag +
> fires `QuestFailed`, but failure is **untracked** — a failed quest reads
> as "never started" (`IfQuestNotStarted` true), so a quest-giver can't tell
> "you failed this." Q6 adds a `_failedQuests` set + queries + a predicate.
> Qud parity: the `Failed` flag (here at quest granularity). **Status:
> in progress.**

## Design (additive)

- `StoryletPart._failedQuests` HashSet. `IsQuestFailed(id)`,
  `GetFailedQuests()`.
- `FailQuest` ALSO adds to `_failedQuests` (still removes from active +
  diag + `QuestFailed` event, unchanged).
- **State consistency:** `StartQuest` REMOVES from `_failedQuests` (re-take
  clears the failed flag → a re-active quest isn't "failed");
  `MarkQuestCompleted` removes from `_failedQuests` (completed-wins).
- **Re-takeable preserved:** `IfQuestNotStarted` is left UNCHANGED
  (`!active && !completed`) — a failed quest stays re-offerable by default.
  Content that wants "don't re-offer / show failed dialogue" uses the new
  `IfQuestFailed` / auto-inverse `IfNotQuestFailed`.
- **Save:** a new trailing `_failedQuests` section AFTER the Q3 objectives
  section, with its own EOF guard (mirrors `_completedQuests` / objectives)
  → pre-Q6 saves load with an empty set, no format break.

## Verification sweep
- ✅ `FailQuest` (Q4.1) + `_completedQuests` pattern + the Save/Load
  trailing-section + EOF-guard idiom (StoryletPart.cs:528-616) — mirror it.
- ✅ `ConversationPredicates` registration + IfNot* auto-invert (used for
  `IfQuestFailed`).
- Decision: keep failed quests re-takeable (don't touch `IfQuestNotStarted`)
  — additive, zero blast radius on existing content/tests.

## Tests (Q6.1)
`FailQuest` → `IsQuestFailed` true; re-`StartQuest` clears failed;
`CompleteQuest`/`MarkQuestCompleted` clears failed; `IfQuestFailed` ±  +
auto-inverse; save round-trip; pre-Q6-save back-compat (hand-written old
layout → EOF guard → empty failed set); adversarial (re-fail idempotent-ish,
fail-then-retake-then-fail).

## Implementation log

**Q6.1 — failed-quest tracking (DONE, 2026-05-23).**
- `StoryletPart._failedQuests` + `IsQuestFailed` + `GetFailedQuests`.
  `FailQuest` adds to the set (keeps remove + diag + `QuestFailed` event).
  `StartQuest` clears it (re-take), `MarkQuestCompleted` clears it
  (completed-wins).
- Save: trailing `_failedQuests` section after the Q3 objectives section,
  own EOF guard → pre-Q6 saves load empty (no format break).
- `IfQuestFailed` predicate (+ auto-inverse `IfNotQuestFailed`).
  `IfQuestNotStarted` left UNCHANGED (failed quests stay re-takeable).
- **Self-review:** purely additive; the only behavior change is FailQuest
  now tracks (vs silently dropping) — covered. State invariants
  (active/completed/failed mutually consistent via the clears).
  Counter-checks: inactive-fail not-tracked, re-take clears, completed-
  wins, IfQuestNotStarted-still-true-for-failed, auto-inverse, pre-Q6
  back-compat (hand-written old layout → EOF → empty).
- **Tests:** 9 (`QuestFailedTrackingTests`); suite 189/189 GREEN; 0 CS errors.
- Adversarial: fail→retake→fail cycle + inactive-fail covered inline (save-
  reach + stacking surfaces); no separate file warranted (thin additive set).

## Status
Q6.1 ✅ (quest-level failed tracking). Objective-level `Failed` state
(Qud `QuestStep.Failed`) is a possible Q6.2 if content needs per-objective
fail; deferred (no current consumer).
