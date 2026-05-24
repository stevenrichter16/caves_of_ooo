# Q5 — World-object quest Parts

> Phase Q5 of `Docs/QUEST-SYSTEM-QUD-PARITY.md`. CoO ports of Qud's
> world-side quest Parts that advance objectives from world interaction
> (not dialogue) — `FinishQuestStepWhenSlain` / `CompleteQuestOnTaken` /
> `QuestStarter`. Each is an `IPart` on an object that, on a world event,
> calls `StoryletPart.Current.FinishObjective` (the Q3.2 API). **Status:
> Q5.1 in progress.**

## Verification sweep (events available)

| World hook | CoO event | Status |
|---|---|---|
| **slain** | `"Died"` fired on the dying entity (`CombatSystem.cs:1072`, params `Target`/`Killer`/`Zone`); dispatched to all parts via `Entity.FireEvent` (no `WantEvent` gate) | ✅ exists → **Q5.1** |
| taken / picked up | (none — no `"Taken"`/`"PickedUp"` event in production) | ❌ → defer Q5.2 |
| created / seen / on-screen | (none of Qud's `Created`/`Seen`/`OnScreen` exist) | ❌ → defer Q5.3 |

So Q5 ships the **slain** Part now (the most universal objective — "kill
X"); take/spawn-triggered Parts are deferred until those events exist
(adding a pickup event is its own change, out of Q5 scope).

## Q5.1 — `FinishObjectiveWhenSlain`

`Part` (namespace `CavesOfOoo.Storylets`) with `Quest` + `Objective`
string fields (blueprint-configured; public → reflection save round-trip).
`HandleEvent`: on `"Died"`, calls
`StoryletPart.Current?.FinishObjective(Quest, Objective, actor: killer)`.
The killer (from the event) is the diag/reward actor; if null,
`FinishObjective` falls back to `LocalPlayer`. No killer gate (Qud parity:
the objective is "X is dead", regardless of who killed it). Safely no-ops
when the quest isn't active or the objective isn't in the current stage
(FinishObjective's own guards).

**Tests (Q5.1):** slain → objective finished; no-active-quest → no-op;
objective-not-in-current-stage → no-op; empty Quest/Objective → no-op;
non-"Died" event → no-op; killer threaded as actor.

## Deferred
- **Q5.2** `CompleteObjectiveOnTaken` — needs a `"Taken"`/pickup event.
- **Q5.3** `QuestStarter` (auto-start on created/seen/taken) — needs those
  triggers. (Quests currently start via dialogue or tick predicates.)

## Implementation log

**Q5.1 — FinishObjectiveWhenSlain (DONE, 2026-05-23).**
- New `Part` (`Assets/Scripts/Gameplay/Storylets/FinishObjectiveWhenSlain.cs`),
  `Quest`/`Objective` public string fields. `HandleEvent`: on `"Died"`,
  `StoryletPart.Current?.FinishObjective(Quest, Objective, actor: killer)`.
  Mirrors `GivesRepPart` (HandleEvent-only; `Entity.FireEvent` dispatches
  to all parts, no `WantEvent` gate — verified at `Entity.cs:255`).
- **Tests:** 6 (`QuestObjectiveWorldPartTests`) — slain finishes its
  objective; last-required slain advances the stage; no-active-quest /
  objective-not-in-current-stage / empty-fields → no-op-no-throw;
  non-"Died" event ignored. The "Died" event is fired with the real
  CombatSystem shape (`Target`/`Killer`/`Zone`, CombatSystem.cs:1072).
- **Self-review:** safely no-ops via FinishObjective's guards; no killer
  gate (Qud parity); counter-checks for every no-op path. No 🔴/🟡.
- **PlayMode:** EditMode tests fire the exact `"Died"` event the
  CombatSystem emits on a real kill, and the FinishObjective runtime path
  was confirmed live in the Q3 session — so the real-kill→event→objective
  path is covered without a bespoke combat PlayMode run.
- Suite 180/180 GREEN; 0 CS errors.

## Status
Q5.1 ✅. Q5.2 (`CompleteObjectiveOnTaken`) + Q5.3 (`QuestStarter`) remain
**deferred** — they need pickup / created-seen-taken events that don't
exist in CoO yet (adding those is out of Q5 scope).
