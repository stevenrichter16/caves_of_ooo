# Q5 — World-object quest Parts

> Phase Q5 of `Docs/QUEST-SYSTEM-QUD-PARITY.md`. CoO ports of Qud's
> world-side quest Parts that advance objectives from world interaction
> (not dialogue) — `FinishQuestStepWhenSlain` / `CompleteQuestOnTaken` /
> `QuestStarter`. Each is an `IPart` on an object that, on a world event,
> calls `StoryletPart.Current.FinishObjective` (the Q3.2 API). **Status:
> Q5.1 ✅. M1 (Taken event) ✅. Q5.2/Q5.3 in progress — Taken-only scope.**

## Verification sweep (events available)

| World hook | CoO event | Status |
|---|---|---|
| **slain** | `"Died"` fired on the dying entity (`CombatSystem.cs:1072`, params `Target`/`Killer`/`Zone`); dispatched to all parts via `Entity.FireEvent` (no `WantEvent` gate) | ✅ exists → **Q5.1** |
| taken / picked up | **FALSE-PREMISE CORRECTION (verified 2026-05-24):** CoO already has item-side `"BeforeBeingPickedUp"` + actor-side `"BeforePickup"`/`"AfterPickup"` (`PickupCommand`), and equip/drop lifecycle events — only the *item-side after-acquisition* hook was missing. The original sweep searched for the literal strings `"Taken"`/`"PickedUp"` and missed the existing surface. | ✅ → **M1 adds `"Taken"`** |
| created / seen / on-screen | (none of Qud's `Created`/`Seen`/`OnScreen` exist — these need per-render/per-turn hooks) | ⚪ deferred (Taken-only scope) |

So Q5.1 shipped the **slain** Part. The take-triggered Parts were
documented as blocked on a missing pickup event — **that premise was
wrong** (see the corrected row above). M1 adds the one genuinely-missing
piece (an item-side `Taken` after-event) so Q5.2/Q5.3 can hook it. The
zone-presence triggers (Created/Seen/OnScreen) remain deferred by the
chosen Taken-only scope (they'd touch perf-sensitive per-render/per-turn
paths — `Docs/PERF-FOUNDATION.md`).

## M1 — item-side `"Taken"` event (Q5.2/Q5.3 prerequisite)

The two **acquisition** commands fire `"Taken"` ON THE ITEM after a
*successful* add, naming the taker:

| Choke point | Source | Fires |
|---|---|---|
| Ground pickup | `PickupCommand.cs` (after `AddObject`, before AutoEquip) | `Taken` on item — `Actor`=taker, `Item`=self |
| Container/corpse take | `TakeFromContainerCommand.cs` (after `AddObject`) | same shape |

CoO analog of Qud's item-side `TakenEvent` (the Parts in
`XRL.World.Parts/QuestStarter.cs` + `CompleteQuestOnTaken.cs` hook it).
Naming note: `"Taken"` (vs CoO's `Before…`/`After…` convention) is the
Qud-parity name; it unifies ground-pickup + container-take and is
item-side like the existing `"BeforeBeingPickedUp"`. Fired before
AutoEquip so "taken" = acquisition, independent of auto-equip.

**Tests (`ItemTakenEventTests`, 6):** pickup fires Taken with taker;
container-take fires Taken; overweight pickup + locked container fire
NONE (counter-checks); item-side-not-actor-side (mutation); two items →
own Taken each, no crosstalk (cross-instance).

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
