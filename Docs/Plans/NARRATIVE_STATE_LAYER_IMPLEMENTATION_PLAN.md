# Narrative State Layer — Implementation Plan

> **Status:** Implementation in progress.
> Tracking: milestones M1–M5. Progress log at bottom.
> Related: `Docs/HOUSE-DRAMA-SYSTEM.md`, `Docs/QUD-PARITY.md`

---

## Goal

Introduce a generalized **narrative state engine** — a flat quality/fact bag
on a singleton world entity plus per-NPC knowledge tracking — that gives the
game's conversation system, quest layer (future), and reactor hooks a
first-class, serializable, engine-integrated source of narrative truth.

The player-visible outcome: dialogue nodes can gate on and mutate named
integer facts (`IfFact`, `SetFact`, etc.); NPC knowledge levels can be
advanced through conversation; and the state survives save/load. The
existing House Drama system stays in place untouched; full migration is
deferred to the storylet/quest layer phase.

This implements the "everything is a Part" Qud idiom so the narrative layer
inherits Entity serialization, tagging, event routing, and the conversation
system's speaker/listener wiring for free.

---

## Scope

### In scope (this plan)

- Singleton `"World"` entity introduced in `GameBootstrap`; stored in
  `GameSessionState.World`; round-tripped through save/load
- `FactBag` — shared `Dictionary<string,int>` implementation with
  delta-event hooks
- `NarrativeStatePart` on the world entity — global fact store + append-only
  event log
- `KnowledgePart` on NPC entities — per-NPC int-tier knowledge
  (0 = ignorant, 1 = does-not-know, 2 = suspects, 3 = knows)
- `QualityRegistry` — static loader for `quality_definitions.json` (mirrors
  `HouseDramaLoader` / `MutationRegistry`)
- Conversation predicates: `IfFact`, `IfNotFact`, `IfSpeakerKnows`,
  `IfNotSpeakerKnows`
- Conversation actions: `SetFact`, `AddFact`, `ClearFact`, `Reveal`
- `TickEnd` GameEvent fired on world entity at end of each turn cycle
- `INarrativeReactor` registry + dispatch on `NarrativeStatePart`

### Explicitly out of scope

- Full migration of House Drama (deferred to storylet/quest layer)
- Storylets and quests as first-class entities
- Quality definitions JSON content beyond skeleton
- Bounded/capped event log
- Reactive (event-driven) evaluator — starting polled (tick-end)
- Save format migration layer

---

## Content-Readiness Analysis

| Deliverable | Status | Notes |
|---|---|---|
| `GameBootstrap` world entity | 🟡 | Must add; lifecycle vs. zone transitions verified |
| `GameSessionState.World` field | 🟡 | Must add; `Capture()` + `ApplyLoadedGame` both need updates |
| `FactBag` shared impl | 🟢 | No dependencies; pure new class |
| `NarrativeStatePart` | 🟢 | Depends on `FactBag` |
| `KnowledgePart` | 🟢 | Depends on `FactBag` |
| `QualityRegistry` | 🟢 | Static loader, mirrors `HouseDramaLoader` |
| Save round-trip (M1) | 🟡 | `ISaveSerializable` must be used; `WritePublicFields` can't handle `Dictionary` |
| Conversation predicates/actions | 🟢 | `ConversationPredicates` / `ConversationActions` lazy-init pattern confirmed |
| `TickEnd` event | 🟡 | `TurnManager.EndTurn` only fires on actor; ~5 lines to add world-entity fire |
| `INarrativeReactor` | 🟢 | New interface; no dependencies |

---

## Cross-Cutting Infrastructure Gaps

| Gap | Affected milestone | Fix |
|---|---|---|
| `GameSessionState` lacks `World` field | M1 | Add `Entity World` field; update `Capture(...)` and `ApplyLoadedGame` |
| `ApplyLoadedGame` closure-capture bug pattern | M1 | Assign `_world = state.World` alongside `_player`/`_zoneManager`/`_turnManager` |
| `TurnManager.EndTurn` fires only on actor | M4a | Add ~5-line block to also fire `TickEnd` on world entity |
| `ISaveSerializable` required for `Dictionary<string,int>` | M2 | `NarrativeStatePart` + `KnowledgePart` implement `ISaveSerializable` |
| `ConversationPredicates` is fail-open for unknown names | M3 | New predicates are explicitly fail-closed; document deviation |

---

## Effort-to-Impact Ordering

1. **M1** — World entity + `GameSessionState.World` (infrastructure; unblocks everything)
2. **M2** — `FactBag` + `NarrativeStatePart` + `KnowledgePart` + save (core state layer)
3. **M3** — Conversation predicates/actions (player-visible narrative gating)
4. **M4a** — `TurnManager` `TickEnd` event (unblocks reactor)
5. **M4b** — `INarrativeReactor` registry (enables reactive/polled consumers)
6. **M5** — Self-review + parity audit

---

## Implementation Tiers

| Tier | Contents |
|---|---|
| A (hours) | Conversation predicates/actions wiring into existing lazy-init registries |
| B (small, 1 day) | `FactBag` + `NarrativeStatePart` + `KnowledgePart`; world entity in bootstrap |
| C (medium, days) | `QualityRegistry` loader; `ISaveSerializable` save round-trip; `INarrativeReactor` dispatch |
| D (deferred) | Storylets, quests, House Drama migration |

---

## Pre-Implementation Verification Sweep (§1.2)

Verified before writing M1 code:

| Claim | Verified | Correction |
|---|---|---|
| `GameSessionState` has `Player`, `ZoneManager`, `TurnManager` — needs `World` | ✅ | Add `Entity World` field + update `Capture(positional args)` + `SaveGraphRoundTripTests.cs:57-62` caller |
| `ApplyLoadedGame` at ~line 545–576 of `GameBootstrap.cs` reassigns all fields | ✅ | Must add `_world = state.World` |
| `SaveGraphSerializer.LoadPart` uses `Activator.CreateInstance(type)` | ✅ | New Parts need public parameterless constructors |
| `LoadEntityBody` does NOT call `Initialize()` | ✅ | Reactor registration runs from both `Initialize()` AND `OnAfterLoad()` |
| `EndTurn` GameEvent fires per-actor only (`TurnManager.cs:214`) | ✅ | Add `TickEnd` / `WorldEndTurn` fire on world entity after actor loop |
| `ConversationPredicates` fail-open for unknown names | ✅ | New predicates are fail-closed by explicit registration |
| `ConversationPredicates.Register(name, PredicateFunc)` pattern | ✅ | Lazy-init via `EnsureInitialized()` |
| `ConversationActions.Factory` is a static field reset at bootstrap AND load | ✅ | New actions registered in both paths |
| `Part.OnBeforeSave` / `OnAfterLoad` / `FinalizeLoad` hooks exist | ✅ | Post-merge on `claude/task-d-zKesU` |
| `ISaveSerializable` preferred over `WritePublicFields` for `Dictionary` | ✅ | Both `NarrativeStatePart` and `KnowledgePart` must implement it |
| `SaveGameService.RegisterRuntime(Func<GameSessionState>, Action<GameSessionState>)` at line 401 | ✅ | |
| `OnAfterBootstrap` fires before `RegisterRuntime` | ✅ | World entity created at bootstrap; re-registered on load via `ApplyLoadedGame` |
| `FormatVersion` bump needed when save shape changes | ✅ | Bump in M1 commit; document in commit message |

---

## Milestone Breakdown (TDD)

### M1 — World entity + `GameSessionState` integration

**Invariant (user-visible):** after save/load, the world entity exists and
has the `"WorldEntity"` tag.

**TDD plan:**
1. Write failing tests: world entity exists after bootstrap; round-trips
   through `GameSessionState.Capture/Save/Load`; `ApplyLoadedGame` re-assigns it
2. Add `Entity World` to `GameSessionState`; update `Capture()`; update
   `ApplyLoadedGame` in `GameBootstrap`; create world entity on boot
3. Bump `FormatVersion`

**Files touched:**
- `Assets/Scripts/Gameplay/Save/SaveSystem.cs`
- `Assets/Scripts/Presentation/Bootstrap/GameBootstrap.cs`
- `Assets/Tests/EditMode/Gameplay/Save/SaveGraphRoundTripTests.cs`
- New: `Assets/Tests/EditMode/Gameplay/NarrativeState/WorldEntityTests.cs`

---

### M2 — `FactBag` + `NarrativeStatePart` + `KnowledgePart` + save round-trip

**Invariant:** facts set on `NarrativeStatePart` survive save/load with
correct values; knowledge set on `KnowledgePart` survives save/load.

**TDD plan:**
1. Write failing tests: `FactBag` get/set/clear; `NarrativeStatePart` add/get;
   `KnowledgePart` tiers; both round-trip through save serializer
2. Implement `FactBag`, `NarrativeStatePart`, `KnowledgePart`, `QualityRegistry`
3. Implement `ISaveSerializable` on both Parts

**Files touched / created:**
- New: `Assets/Scripts/Gameplay/NarrativeState/FactBag.cs`
- New: `Assets/Scripts/Gameplay/NarrativeState/NarrativeStatePart.cs`
- New: `Assets/Scripts/Gameplay/NarrativeState/KnowledgePart.cs`
- New: `Assets/Scripts/Gameplay/NarrativeState/QualityRegistry.cs`
- New: `Assets/Tests/EditMode/Gameplay/NarrativeState/FactBagTests.cs`
- New: `Assets/Tests/EditMode/Gameplay/NarrativeState/NarrativeStatePartTests.cs`
- New: `Assets/Tests/EditMode/Gameplay/NarrativeState/KnowledgePartTests.cs`

---

### M3 — Conversation predicates and actions

**Invariant:** `IfFact:key:≥:3` returns false when fact < 3 and true when
≥ 3; `SetFact:key:5` sets fact to 5; `Reveal:Listener:key:3` sets
`KnowledgePart` on listener.

**TDD plan:**
1. Write failing conversation predicate/action tests
2. Register `IfFact`, `IfNotFact`, `IfSpeakerKnows`, `IfNotSpeakerKnows`
3. Register `SetFact`, `AddFact`, `ClearFact`, `Reveal`
4. Arg parsing: `Split(':', N)` with explicit N limit

**Predicate arg format:** `IfFact:key:op:value` where op ∈ `{=, !=, <, >, <=, >=}`
**Action arg format:** `SetFact:key:value`, `AddFact:key:delta`, `ClearFact:key`,
`Reveal:Target:key:tier` (Target ∈ `Listener`, `Speaker`)

**Files touched / created:**
- `Assets/Scripts/Gameplay/Conversations/ConversationPredicates.cs`
- `Assets/Scripts/Gameplay/Conversations/ConversationActions.cs`
- New: `Assets/Tests/EditMode/Gameplay/NarrativeState/NarrativeConversationTests.cs`

---

### M4a — `TurnManager` `TickEnd` event

**Invariant:** after every call to `TurnManager.EndTurn`, a `TickEnd`
GameEvent is fired on the world entity.

**TDD plan:**
1. Write failing test: mock world entity with a counting part; assert
   `TickEnd` received after `EndTurn`
2. Add ~5-line block in `TurnManager.EndTurn` to fire `TickEnd` on world entity
3. World entity obtained from `GameBootstrap.World` static accessor or
   via service locator

**Files touched:**
- `Assets/Scripts/Gameplay/Turns/TurnManager.cs`
- New test in `WorldEntityTests.cs` or separate `TickEndTests.cs`

---

### M4b — `INarrativeReactor` registry + dispatch

**Invariant:** a registered `INarrativeReactor` has its `OnFactChanged`
called when a fact on `NarrativeStatePart` changes.

**TDD plan:**
1. Write failing test: register reactor, set fact, assert `OnFactChanged` called
2. Implement `INarrativeReactor` interface
3. Registry + dispatch in `NarrativeStatePart` on `TickEnd` event

**Files touched / created:**
- New: `Assets/Scripts/Gameplay/NarrativeState/INarrativeReactor.cs`
- `Assets/Scripts/Gameplay/NarrativeState/NarrativeStatePart.cs`
- New test in `NarrativeStatePartTests.cs`

---

### M5 — Self-review + parity audit

Per Methodology Template §5:
- Review all new code against the decompiled Qud reference for divergences
- Produce a parity audit table (CoO-original vs Qud-mirrored)
- Run PlayMode sanity smoke test
- Document post-review findings

---

## Verification Checklist

- [ ] M1: `GameSessionState` has `Entity World` field
- [ ] M1: World entity survives `Capture → Save → Load → ApplyLoadedGame` round-trip
- [ ] M1: World entity has `"WorldEntity"` tag after load
- [ ] M2: `FactBag.Get` returns 0 for unknown keys
- [ ] M2: `NarrativeStatePart` facts survive save/load
- [ ] M2: `KnowledgePart` tiers (0–3) survive save/load
- [ ] M3: `IfFact:x:>=:3` returns false when x=2, true when x=3
- [ ] M3: `SetFact:x:5` sets world fact x to 5
- [ ] M3: `Reveal:Listener:x:2` sets listener `KnowledgePart` key x to 2
- [ ] M4a: `TickEnd` event fires on world entity after every `EndTurn`
- [ ] M4b: `INarrativeReactor.OnFactChanged` called on fact mutation
- [ ] All new EditMode tests pass (`dotnet test` or Unity Test Runner)
- [ ] `FormatVersion` bumped in save system (M1)
- [ ] Existing `SaveGraphRoundTripTests` still pass with updated `Capture(...)` signature

---

## Progress Log

| Date | Milestone | Status | Notes |
|---|---|---|---|
| 2026-04-27 | Plan | ✅ Written | Plan document committed |
| 2026-04-27 | M1 | 🔄 In progress | — |
