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

## Deferred (post-Taken-only scope)
- **Equip/Unequip/Dropped triggers** for `CompleteObjectiveOnTaken` (Qud
  hooks these too) — CoO's equip/drop fire actor-side `AfterEquip`/`AfterDrop`,
  so an item-side variant would need new item-side events. Out of the
  Taken-only scope.
- **Zone-presence triggers** `Created`/`Seen`/`OnScreen` for `QuestStarter`
  — need per-render/per-turn hooks (perf-sensitive; PERF-FOUNDATION.md).
- **`QuestStarted` immediate-complete handler** (Qud's CompleteQuestOnTaken
  completes if you ALREADY HOLD the item when the quest starts). CoO covers
  this case DIFFERENTLY (and arguably better): the objective's polled
  `IfHaveItem` trigger (e.g. `EnchiridionQuest.find_enchiridion`) finishes
  it on the next tick. `CompleteObjectiveOnTaken` is the event-driven
  fast-path; `IfHaveItem` is the declarative polled fallback. They compose
  safely (FinishObjective is idempotent), so no QuestStarted handler is
  needed. See "when to use which" below.

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

**Q5.2 — CompleteObjectiveOnTaken (DONE, 2026-05-24).**
- New `Part` (`Assets/Scripts/Gameplay/Storylets/CompleteObjectiveOnTaken.cs`),
  `Quest`/`Objective` public string fields. `HandleEvent`: on the M1 `"Taken"`
  event, routes to `StoryletPart.Current?.FinishObjective(Quest, Objective,
  actor: taker)`. Mirrors Q5.1's FinishObjectiveWhenSlain.
- **Player gate (Qud parity — the key difference from Q5.1):** only the
  PLAYER taking the item counts. Qud's `CompleteQuest` gates on
  `Actor.IsPlayer()`; CoO maps that to `taker == StoryletPart.LocalPlayer`
  with a non-null LocalPlayer guard (so the null==null pre-bootstrap trap
  can't sneak a completion through).
- **Tests:** 9 (`QuestObjectiveTakenPartTests`) — by-player finishes; by-NPC
  / null-taker / no-LocalPlayer do NOT (player-gate counter-checks);
  last-required advances; no-active-quest / not-in-stage / empty-fields →
  no-op-no-throw; non-Taken ignored.
- **When to use vs `IfHaveItem` objective trigger:** `CompleteObjectiveOnTaken`
  = instant, event-driven completion on the take action (good when the item
  is then consumed/transformed). `IfHaveItem` objective trigger = polled,
  declarative, catches already-holding + any acquisition path. Use either or
  both (they're idempotent together).

**Q5.3 — QuestStarter (DONE, 2026-05-24).**
- New `Part` (`Assets/Scripts/Gameplay/Storylets/QuestStarter.cs`): on the
  M1 `"Taken"` event (player only), `StoryletPart.Current.StartQuest(Quest)`.
  The world-side complement to dialogue "take the quest" choices.
- **Qud→CoO mapping:** Qud `IfFinishedQuestStep` → `IfQuestCompleted` (CoO
  tracks completed quests permanently, not finished objectives); Qud
  `Activated` + self-removal → an `Activated` flag (a flag, not a Part
  removal, avoids mutating the entity's part list mid event-dispatch — the
  CLAUDE.md iterator hazard). Player gate as in Q5.2.
- **Fires once ever:** the `Activated` guard matters for the fail→re-take
  case — without it, re-grabbing the item after failing would restart the
  quest (StartQuest re-activates a failed quest). `Activated` round-trips
  (public bool → SaveSystem WritePublicFields), so a spent starter stays
  spent across save/load.
- **Tests:** 9 (`QuestStarterPartTests`) — by-player starts; by-NPC / null /
  no-LocalPlayer don't; fires-once-no-restart-after-fail; IfQuestCompleted
  gate (met → starts, unmet → doesn't); empty-quest / non-Taken no-op.

**Adversarial sweep (DONE, 2026-05-24).** `QuestTakenWorldPartsAdversarialTests`
(14 tests) — 0 bugs. Surfaces: save/load reflection (both Parts + `Activated`
round-trip via `SaveSystem.WritePublicFields`); cross-actor / multi-instance
(two starters same quest; two complete-items same objective; composite item
with both Parts); malformed event params (no Actor / null Current); re-fire /
atomicity (repeated Taken fires once; idempotent re-take); boundary
(whitespace quest, all-null). **Two initial "failures" were test-expectation
errors** (asserted completion where a 2-stage quest only advances to the
terminal stage) — corrected to single-stage quests; production behavior was
correct (already pinned by `QuestObjectiveHypothesisTests` + the Q3.5 bench).

**Cold-eye review (both angles):** Angle A taxonomy — 0 🔴/🟡 (null safety,
atomicity, no iterator mutation, counter-check completeness all clean). Angle
B Qud-parity-first — surfaced the `IfHaveItem`-trigger-vs-`CompleteObjectiveOnTaken`
relationship (documented above); no QuestStarted handler needed.

**Integration / rule-7 validation (DONE, 2026-05-24).** `QuestTakenIntegrationTests`
(3 tests) drives the FULL production chain — the real `PickupCommand` fires
the M1 `Taken` event, the item's Part hooks it, routing to the real
`StoryletPart`: real-pickup→objective-finished, real-pickup→quest-started,
and the non-player-pickup counter-check, all through the actual command
layer the pickup UI invokes (not synthetic events). This closes the
M1↔M2/M3 seam without a bespoke PlayMode scenario — every component on the
path is production code. (Honesty bound: the live input/bootstrap layer
isn't separately scripted; it only sets `StoryletPart.LocalPlayer = _player`,
GameBootstrap.cs:262.)

**Live self-auditing bench (DONE, 2026-05-24; workflow step 7).**
`QuestWorldPartsBench` (`Assets/Scripts/Scenarios/Custom/`) — one press of
Play drives a 3-required-objective quest through ALL three world-side
mechanisms in the LIVE runtime (real `ScenarioContext`, real zone, real
`PickupCommand`, real `StoryletPart`): QuestStarter scroll → quest starts;
CompleteObjectiveOnTaken relic → objective finished; FinishObjectiveWhenSlain
guard → objective finished; direct FinishObjective (convo path) → advance.
Emits one `questbench/Cell` diag per assertion (+ a CONTROL row), stamped
`bench=worldparts` + a per-run `runId`. **Live run validated: 7 cells, 0
fails (runId acdaa33b), console clean** — the rule-7 proof that the Parts
work attached to real entities driven by the real command in the bootstrapped
runtime, not just EditMode fixtures. Re-runnable (unique per-run IDs). Smoke:
`ScenarioCustomSmokeTests.QuestWorldPartsBench_Applies_WithoutThrowing`.

## Status
Q5.1 ✅. M1 (`"Taken"` event) ✅. Q5.2 (`CompleteObjectiveOnTaken`) ✅.
Q5.3 (`QuestStarter`) ✅. Scope: **Taken-only** (user-chosen). Equip/drop +
zone-presence triggers deferred (see Deferred section).
