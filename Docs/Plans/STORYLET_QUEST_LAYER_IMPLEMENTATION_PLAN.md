# Storylet / Quest Layer — Implementation Plan

> Successor to `Docs/Plans/NARRATIVE_STATE_LAYER_IMPLEMENTATION_PLAN.md`.
> Builds on the M1–M5 narrative-state foundation (FactBag, NarrativeStatePart,
> KnowledgePart, INarrativeReactor, TickEnd, conversation hooks).

---

## Context

The narrative state layer (shipped on `claude/task-d-zKesU`, 78 EditMode tests
green) gives the game a serializable, engine-integrated source of narrative
truth. It exposes `FactBag`, per-NPC `KnowledgePart`, conversation predicates
(`IfFact`, `IfSpeakerKnows`) and actions (`SetFact`, `Reveal`, …), and a
polled `INarrativeReactor` registry on `NarrativeStatePart` that ticks once
per `EndTurn`.

The narrative-state plan deferred two things to a follow-up: **storylets and
quests as first-class entities reading/writing the state layer**, and **the
full migration of HouseDrama**. This plan addresses the former. HouseDrama
explicitly **coexists** untouched.

The user-visible outcome: small modular content units ("storylets") fire
when their predicates pass, mutating the FactBag and announcing themselves
to the MessageLog; multi-stage variants ("quests") track per-stage progress,
survive save/load, and can be gated/advanced from dialogue.

This is the first real consumer of `INarrativeReactor` (currently 0
implementations).

---

## Scoping decisions (locked in by user)

| Decision | Choice |
|---|---|
| Architecture | **Combined** — one `StoryletData` type; quests are storylets with a non-null `Quest` sub-object holding stages |
| Quest visibility | **Notification-only** — events log to `MessageLog`; no journal UI |
| HouseDrama | **Coexist** — left untouched |
| Trigger model | **Polled** via `INarrativeReactor.OnTickEnd` |

---

## Goal

Add a generalized storylet/quest layer on top of the narrative state
foundation. A **storylet** is a small modular content unit with triggers
(predicates over FactBag/KnowledgePart) and effects (fact mutations, message
log lines, knowledge reveals). A **quest** is a storylet with a `Quest`
sub-object containing an ordered stage list, advancing one stage per tick
when stage triggers pass.

Both poll via a single `StoryletPart` registered on `NarrativeStatePart`'s
reactor list. Both serialize through `ISaveSerializable`. Both author in
JSON under `Resources/Content/Data/Storylets/`. Conversation predicates and
actions gate dialogue on storylet/quest state and let designers fire/advance
quests from dialogue.

---

## Scope

### In scope (this plan)

- `StoryletData` schema (`[Serializable]`, JSON-loaded), including optional
  `Quest: QuestData` sub-object with `Stages: List<QuestStageData>`
- `StoryletRegistry` static loader (mirrors `HouseDramaLoader`,
  `QualityRegistry`)
- `StoryletPart : Part, ISaveSerializable, INarrativeReactor` on the world
  entity — owns `_firedStorylets` (HashSet<string>) and `_quests`
  (Dictionary<string, QuestState>)
- `QuestState` — `[Serializable]` POCO: `QuestId`, `CurrentStageIndex`,
  `EnteredStageAtTurn`
- Single-pass per-tick dispatch: snapshot eligible storylets at start of
  `OnTickEnd`, fire effects after, cascading triggers land on the *next*
  tick
- Quest stages advance **once per tick per quest** (skip the quest after a
  stage transition for the remainder of that tick)
- New conversation predicates: `IfStoryletFired`, `IfQuestActive`,
  `IfQuestStage`, `IfQuestComplete` (auto-inverted `IfNot*`)
- New conversation actions: `StartQuest`, `AdvanceQuest`, `CompleteQuest`,
  `FireStorylet`
- New `IfPlayerHas*` predicate family (resolves player via
  `GameSessionState`/`Player.Current`) so storylets can gate on player
  inventory/tags without a dialogue listener — closes the null-listener hole
- `StoryletPart.GetActiveQuests()` accessor (free/cheap; future-proofs for a
  later journal UI without committing to one)
- Save format bump (`FormatVersion` 2→3) in M2; pre-1.0 dev policy means
  v2 saves intentionally fail to load
- `StoryletPart.LoadEntityBody` is idempotent: if a save graph has no
  StoryletPart entry (impossible at v3 but defensive), bootstrap reattaches
  a fresh empty one
- One demo storylet shipped in `Resources/Content/Data/Storylets/` for the
  end-to-end smoke test

### Explicitly out of scope

- Quest journal UI (notification-only this plan; future plan)
- Reactive (fact-change) trigger evaluation (polled-only; future plan)
- HouseDrama migration (coexists; future plan)
- Storylets with branching dialogue trees (use the conversation system for
  that — storylets are a single body of effects)
- Quest rewards as a typed concept (use existing `SetFact`/`AddXP`/
  `GiveItem` actions — the storylet effect list is fully open-ended)
- Cross-zone or time-of-day-gated storylets beyond what `IfFact` / a turn
  counter can express (no new predicate categories beyond the
  player/storylet/quest set)
- Migration of the throwaway `dev_met_warden` demo in `Wardens.json`
  (separate cleanup; revert when done playing)

---

## Key design decisions

1. **Combined type, sub-object schema.** One `StoryletData`; a non-null
   `Quest: QuestData` field marks it as a quest. Avoids a kitchen-sink shape
   where every storylet carries an empty `Stages` list.

2. **Reactor-list registration, not `Part.HandleEvent` override.**
   `NarrativeStatePart` already advertises `INarrativeReactor` as the seam
   for tick consumers. `StoryletPart` registers itself there (it's still a
   Part for save serialization). Two parts both hooking `TickEnd` via
   `HandleEvent` would work but invites order-dependence on parts list
   position.

3. **Single-pass dispatch, fact-cascades land next tick.** Snapshot the
   eligible storylet set at the top of `OnTickEnd`, then fire effects.
   A storylet whose effects mutate the FactBag in a way that flips another
   storylet's predicate doesn't re-enter dispatch — the second one fires
   on the next tick. This matches the polled-trigger philosophy and keeps
   tests deterministic.

4. **Quest stage advancement is one-step-per-tick-per-quest.** If stages
   0 and 1 are both eligible in the same tick, we advance only to stage 1;
   stage 2 evaluates next tick. Matches how a player-facing quest log feels.

5. **Effect/predicate dispatch reuses `ConversationPredicates.Evaluate`
   and `ConversationActions.Execute` with `speaker=null, listener=null`.**
   Most existing handlers fail-closed on null contexts (which is fine —
   it just narrows the storylet author's vocabulary). A few have
   NRE risk that the verification sweep must check.

6. **Storylet-safe predicates/actions enumerated as an allowlist.** The
   conversation registry is a catch-all; not all entries are safe to call
   without a speaker/listener. The plan ships a doc table of "safe from
   storylets" entries to guide authors. New `IfPlayerHas*` family fills
   the most common gap (gating on player inventory).

7. **`Tracked` flag is independent of `IsQuest`.** A tracked non-quest
   storylet ("the tea kettle whistled") logs to MessageLog without being
   a multi-stage thing. Keeps the schema honest.

8. **Save format bump in M2.** Per project pre-1.0 dev policy, v2 saves
   throw `InvalidDataException` on a v3 build. No migration layer.

---

## Cross-cutting infrastructure gaps

| Gap | Affected milestone | Fix |
|---|---|---|
| `IfNotHostile` and other `IfNot*` auto-inversions fail-OPEN with null contexts (M0 finding C2) | M5 conversation hooks (allowlist doc) | Document the storylet-safe allowlist in `StoryletData.cs` xmldoc; storylet authors must avoid context-dependent predicates of either polarity |
| Unknown predicates fail-OPEN (return `true`) — typos cause runaway storylets (M0 finding C3) | M1 schema validation | `StoryletRegistry.LoadFromJson` validates trigger/effect names against the predicate/action registries at load time and rejects/warns on unknown names |
| No static `Player` accessor — `GameSessionState.Player` is an instance field (M0 finding C1) | M5 + cross-cutting | Introduce `PlayerEntity.Current` static (new file `Assets/Scripts/Core/PlayerEntity.cs`); wire in `GameBootstrap.OnAfterBootstrap` and `ApplyLoadedGame`; reset to null in test cleanup paths |
| No `IfPlayerHas*` family — `IfHaveItem` etc. fail-closed when `listener` is null | M5 conversation hooks | Add `IfPlayerHasItem`, `IfPlayerHasTag`, `IfPlayerHasProperty`, resolving via `PlayerEntity.Current` (depends on the C1 fix above) |
| `StoryletPart.OnAfterLoad` must register the part as an `INarrativeReactor` on `NarrativeStatePart.Current` (load path skips `Initialize` — confirmed M0) | M2 | Mirror `NarrativeStatePart`'s own pattern: register in both `Initialize` and `OnAfterLoad` |
| First-tick race: `OnTickEnd` fires before `StoryletRegistry.LoadAll` completes | M3 | `LoadAll` runs in `OnAfterBootstrap` step 1c per existing pattern; bootstrap order already serializes this |
| Conversation predicate/action registries call `Reset()` in tests | M5 | New entries register inside `EnsureInitialized()` like the narrative-state M3 commit (`0a845e5`) |

---

## Effort-to-impact ordering

1. **M0** — Pre-implementation verification sweep (no code; updates this plan with corrections)
2. **M1** — Schema + Registry (StoryletData, QuestData, QuestStageData, StoryletRegistry; JSON round-trip)
3. **M2** — `StoryletPart` on world entity, `ISaveSerializable`, save/load round-trip; FormatVersion 2→3
4. **M3** — Trigger evaluation + dispatch (storylets fire when predicates pass; OneShot fires once; single-pass-per-tick semantics)
5. **M4** — Quest stage advancement (ordered stages, OnEnter effects, completion semantics)
6. **M5** — Conversation predicates + actions + new `IfPlayer*` family
7. **M6** — Self-review + parity audit + end-to-end test + one shipped demo storylet

---

## Pre-implementation verification sweep (M0)

Run these checks **before writing M1**, log corrections inline. Lifted from
the narrative-state plan's §1.2 pattern.

| Claim | How to verify |
|---|---|
| `FactionManager.GetFeeling(listener, speaker)` is null-safe (or at least fail-closed, not NRE) | Read `FactionManager.cs`; if NRE, add to allowlist exclusion |
| `FactionManager.IsHostile` same | Same |
| `GameSessionState.Player` is a public static / accessor for the new `IfPlayer*` family | Grep `GameSessionState.cs` |
| `NarrativeStatePart` exposes a `RegisterReactor` API publicly (M4b commit `c96123e`) | Read `NarrativeStatePart.cs` |
| `StoryletRegistry`'s `LoadAll` path mirrors `HouseDramaLoader.LoadAll` exactly (incl. the `_loaded` flag fix from `83e9522`) | Diff against `HouseDramaLoader.cs` post-fix |
| `SaveWriter.FormatVersion` is currently `2`; `SaveSystem.cs` strict-equality check rejects mismatched versions | Confirm in `SaveSystem.cs` |
| `LoadEntityBody` does not call `Initialize` (so `OnAfterLoad` is the right hook for re-registering reactor) | Confirm in `SaveGraphSerializer.cs` |
| `ConversationPredicates.Evaluate` and `ConversationActions.Execute` accept null speaker/listener without NRE for the storylet-safe allowlist | Audit each handler in source |
| `MessageLog.Add(string)` is callable from a tick-end context (no UI thread restrictions) | Read `MessageLog.cs` |
| Narrative-state `EnsureInitialized` pattern is the registration site for new predicates/actions | Confirm in M3 commit `0a845e5` |

---

## M0 findings (sweep run on 2026-04-26)

### Confirmed (no plan changes)

- `FactionManager.GetFeeling`/`IsHostile` are null-safe — fail-closed to 0/false at `FactionManager.cs:168`
- `NarrativeStatePart.RegisterReactor` is public + duplicate-guarded (`NarrativeStatePart.cs:51`)
- `SaveGraphSerializer.LoadEntityBody` (`SaveSystem.cs:629`) does **not** call `Initialize()` — `OnAfterLoad` is the correct hook for reactor re-registration
- `SaveWriter.FormatVersion = 2` (`SaveSystem.cs:22`); strict-equality reject at line 131 — v2→v3 bump is a hard break for v2 saves per pre-1.0 dev policy
- `MessageLog.Add` is plain `List.Add` + delegate invoke — safe from any context including `OnTickEnd`
- `ConversationPredicates.EnsureInitialized` (`ConversationPredicates.cs:20`) is the right registration seam; idempotent

### Corrections (incorporated below)

**C1 — No static `Player` accessor exists.** `GameSessionState.Player` is an instance field, not a static. The `IfPlayer*` family needs a new static accessor (mirroring `NarrativeStatePart.Current` and `TurnManager.World`). M5 scope expands: introduce `PlayerEntity.Current` (new file
`Assets/Scripts/Core/PlayerEntity.cs`, simple `public static Entity Current;`)
wired by `GameBootstrap` in `OnAfterBootstrap` and `ApplyLoadedGame`. Reset to
null on `Reset()` paths used by tests.

**C2 — `IfNot*` auto-inversion fails-OPEN for predicates that need a non-null listener/speaker.** `IfHaveItem` fails-closed to `false` when `listener == null`; auto-inversion turns that into `IfNotHaveItem` returning `true` always for storylets. The storylet allowlist must filter **both polarities** of context-dependent predicates. Specifically NOT safe from storylets:
`IfHaveItem`/`IfNotHaveItem`, `IfHaveTag`/`IfNotHaveTag`, `IfHaveProperty`/`IfNotHaveProperty`, `IfHaveIntProperty`/`IfNotHaveIntProperty`, `IfHaveItemWithTag`/`IfNotHaveItemWithTag`, `IfStatAtLeast`/`IfNotStatAtLeast`, `IfSpeakerHaveTag`/`IfNot*`, `IfSpeakerHaveProperty`/`IfNot*`, `IfFactionFeelingAtLeast`/`IfNot*`, `IfNotHostile` (specifically — fails-OPEN with null contexts), `IfSettlementSiteStage`/`IfNot*`. The `IfPlayer*` family resolves this for inventory/tag/property checks.

**C3 — Unknown predicates fail-OPEN.** `ConversationPredicates.Evaluate` returns `true` for unregistered names (`ConversationPredicates.cs:48`, with a `Debug.LogWarning`). A typo in a storylet trigger fires it forever. M1 scope expands: `StoryletRegistry.LoadFromJson` validates each storylet's trigger predicate names against `ConversationPredicates._predicates` keys at load time (or against a published "known names" snapshot) and rejects/warns on unknown names — fail-fast at content-load, not at trigger-eval. Same check for action names against `ConversationActions._actions`.

**Storylet-safe predicate allowlist** (post-M5 — to be documented in `StoryletData.cs` xmldoc):

```
IfFact / IfNotFact
IfReputationAtLeast / IfNotReputationAtLeast
IfDramaActive / IfNotDramaActive
IfPressurePointState / IfNotPressurePointState
IfPathTaken / IfNotPathTaken
IfWitnessKnows / IfNotWitnessKnows
IfCorruptionAtLeast / IfNotCorruptionAtLeast
IfPlayerHasItem / IfNotPlayerHasItem    (new in M5)
IfPlayerHasTag / IfNotPlayerHasTag      (new in M5)
IfPlayerHasProperty / IfNotPlayerHasProperty (new in M5)
IfStoryletFired / IfNotStoryletFired    (new in M5)
IfQuestActive / IfNotQuestActive        (new in M5)
IfQuestStage / IfNotQuestStage          (new in M5)
IfQuestComplete / IfNotQuestComplete    (new in M5)
```

**Storylet-safe action allowlist:**

```
SetFact, AddFact, ClearFact
Reveal (with caveat: requires Speaker|Listener target — listener defaults
        to player if PlayerEntity.Current is non-null, but storylet authors
        should prefer SetFact on the player's KnowledgePart directly via
        a future RevealToPlayer action; tracked as a follow-up)
AddMessage
ChangeFactionFeeling
AdvancePressurePoint, RevealWitnessFact, AddCorruption,
StartDrama, TriggerCrossover
StartQuest, AdvanceQuest, CompleteQuest, FireStorylet (new in M5)
```

---

## Milestone breakdown (TDD)

### M1 — Schema + Registry

**Invariant:** A JSON file in `Resources/Content/Data/Storylets/` is loaded
into `StoryletRegistry`; storylet IDs round-trip through `Get`/`GetAll`;
malformed JSON does not throw; HouseDramaLoader-style `_loaded` semantics
are preserved (no auto-reload after `Reset()` + populate); **trigger and
effect names are validated against the conversation predicate/action
registries at load time** (M0 finding C3) — unknown names rejected with a
warning rather than fail-OPEN at trigger eval.

**TDD plan:**
1. Write failing tests: registry `LoadFromJson` registers single + multi
   storylets; missing ID skipped with warning; duplicate ID overwrites;
   malformed JSON does not throw; `Reset` then `Register` survives next
   `Get` (the bug class fixed in `83e9522`); **storylet with unknown
   trigger predicate name is rejected with a warning** (new from C3);
   storylet with unknown effect action name is rejected
2. Implement `StoryletData`, `QuestData`, `QuestStageData`, `StoryletRegistry`
3. Add validation pass: for each loaded storylet, check every
   `Triggers[].Key` against `ConversationPredicates._predicates` (or via a
   new `ConversationPredicates.IsRegistered(name)` accessor) and every
   `Effects[].Key` against `ConversationActions._actions`; emit warnings
   and skip the storylet if any name is unknown
4. Run loader tests against an empty `Resources/Content/Data/Storylets/`

**Files created:**
- `Assets/Scripts/Gameplay/Storylets/StoryletData.cs`
- `Assets/Scripts/Gameplay/Storylets/QuestData.cs`
- `Assets/Scripts/Gameplay/Storylets/QuestStageData.cs`
- `Assets/Scripts/Gameplay/Storylets/StoryletRegistry.cs`
- `Assets/Tests/EditMode/Gameplay/Storylets/StoryletRegistryTests.cs`

---

### M2 — `StoryletPart` + save round-trip

**Invariant:** A storylet recorded as fired and a quest at stage N survive
`Capture → Save → Load → ApplyLoadedGame`; `StoryletPart.Current` and the
reactor registration are re-wired on load.

**TDD plan:**
1. Write failing tests: world entity has `StoryletPart` after bootstrap;
   `_firedStorylets` round-trips; `_quests` round-trip preserves
   `CurrentStageIndex` and `EnteredStageAtTurn`; `StoryletPart.Current`
   is non-null after both bootstrap AND load
2. Implement `StoryletPart`, `QuestState`, `ISaveSerializable` Save/Load
3. Wire `GameBootstrap.OnAfterBootstrap` to attach the part and register
   the reactor; wire `ApplyLoadedGame` to re-assign `StoryletPart.Current`
   and re-register the reactor on the loaded `NarrativeStatePart`
4. Bump `SaveWriter.FormatVersion` 2→3

**Files created/modified:**
- `Assets/Scripts/Gameplay/Storylets/StoryletPart.cs`
- `Assets/Scripts/Gameplay/Storylets/QuestState.cs`
- `Assets/Scripts/Presentation/Bootstrap/GameBootstrap.cs` (+attach +load wire)
- `Assets/Scripts/Gameplay/Save/SaveSystem.cs` (FormatVersion bump)
- `Assets/Tests/EditMode/Gameplay/Storylets/StoryletPartTests.cs`
- `Assets/Tests/EditMode/Gameplay/Storylets/StoryletSaveRoundTripTests.cs`

---

### M3 — Trigger evaluation + dispatch

**Invariant:** A registered non-quest storylet whose predicates all evaluate
true in `OnTickEnd` fires its effects. A `OneShot:true` storylet fires once.
Effects from storylet A that flip storylet B's predicate land on the next
tick (single-pass dispatch).

**TDD plan:**
1. Write failing tests: storylet fires when single-predicate true; doesn't
   fire when predicate false; OneShot fires exactly once across many ticks;
   non-OneShot fires every tick its predicate is true; effects execute via
   `ConversationActions.Execute(speaker=null, listener=null, …)`; cascading
   storylet B does NOT fire in same tick as A — fires next tick
2. Implement `StoryletPart.OnTickEnd`: snapshot eligibility, fire effects,
   record fires
3. Confirm `IfFact` predicate works with `speaker=null, listener=null`
   (already verified in M0)

**Files modified/created:**
- `Assets/Scripts/Gameplay/Storylets/StoryletPart.cs` (+OnTickEnd)
- `Assets/Tests/EditMode/Gameplay/Storylets/StoryletReactorTests.cs`

---

### M4 — Quest stage advancement

**Invariant:** A quest with stages [Start, Mid, End] advances one stage
per tick when each stage's triggers pass; OnEnter effects fire on
transition; `CurrentStageIndex == Stages.Count` means complete; complete
quests don't re-evaluate.

**TDD plan:**
1. Write failing tests: starting a quest sets stage 0 and fires stage 0
   OnEnter; advancing through 3 stages takes 3 ticks (one-step-per-tick);
   `IfQuestStage:Q:Mid` evaluates true at the right time; completed quest
   stops evaluating; if multiple stages eligible same tick, only first
   advances
2. Implement quest dispatch in `StoryletPart.OnTickEnd`: after non-quest
   storylets, iterate active quests; advance at most one stage per quest
   per tick; mark complete when past last stage

**Files modified/created:**
- `Assets/Scripts/Gameplay/Storylets/StoryletPart.cs` (+quest advancement)
- `Assets/Tests/EditMode/Gameplay/Storylets/QuestStageTests.cs`

---

### M5 — Conversation hooks + `IfPlayer*` family + `PlayerEntity.Current`

**Invariant:** Dialogue can gate choices on storylet/quest state and fire
storylet/quest manipulations from action hooks. Storylets can gate on
player inventory/tags without a listener context. A static
`PlayerEntity.Current` is set on bootstrap and on load.

**TDD plan:**
1. Introduce `PlayerEntity` (new file `Assets/Scripts/Core/PlayerEntity.cs`):
   `public static class PlayerEntity { public static Entity Current; public static void Reset() => Current = null; }`. Wire in
   `GameBootstrap.OnAfterBootstrap` (after `_player` is created) and in
   `ApplyLoadedGame` (after `_player = state.Player`). Reset null in test
   cleanup paths that call `FactionManager.Reset()` etc.
2. Write failing tests for new predicates: `IfStoryletFired:id`,
   `IfQuestActive:id`, `IfQuestStage:id:stageId`, `IfQuestComplete:id` —
   each correct on positive and negative cases; auto-inverted `IfNot*`
   covered for free; new `IfPlayerHasItem`, `IfPlayerHasTag`,
   `IfPlayerHasProperty` resolving via `PlayerEntity.Current` (NOT the
   `listener` argument)
3. Write failing tests for new actions: `StartQuest:id` activates a quest
   from dialogue; `AdvanceQuest:id:stageId` jumps to a stage; `CompleteQuest:id`
   marks complete; `FireStorylet:id` triggers a one-shot manually
4. Add `ConversationPredicates.IsRegistered(name)` and
   `ConversationActions.IsRegistered(name)` accessors used by M1's load-time
   validation
5. Implement predicates/actions inside `EnsureInitialized()` (so they
   survive `Reset()` like narrative-state M3)

**Files modified/created:**
- `Assets/Scripts/Core/PlayerEntity.cs` (new)
- `Assets/Scripts/Presentation/Bootstrap/GameBootstrap.cs` (+wire `PlayerEntity.Current`)
- `Assets/Scripts/Gameplay/Conversations/ConversationPredicates.cs`
- `Assets/Scripts/Gameplay/Conversations/ConversationActions.cs`
- `Assets/Tests/EditMode/Gameplay/Storylets/StoryletConversationTests.cs`
- `Assets/Tests/EditMode/Gameplay/Storylets/PlayerEntityTests.cs`

---

### M6 — Self-review + parity audit + e2e + demo

Per Methodology Template §5:
- Audit each new file against the narrative-state and HouseDrama precedents
- Parity audit table (CoO-original vs Qud-mirrored — almost everything here
  is CoO-original; the substrate uses the "everything is a Part" Qud idiom)
- One end-to-end test: bootstrap → register a storylet that triggers on
  `IfFact:demo:>=:1` → set the fact → tick → assert effects fired → save →
  load → assert `IfStoryletFired:demo` evaluates true
- One demo storylet shipped in `Resources/Content/Data/Storylets/Demo.json`
  (the throwaway warden demo from `Wardens.json` can stay or go independently
  — note in commit message)

**Files created:**
- `Assets/Tests/EditMode/Gameplay/Storylets/StoryletEndToEndTests.cs`
- `Assets/Resources/Content/Data/Storylets/Demo.json`

---

## Verification checklist

- [x] M0: FactionManager null-safety verified (fail-closed); 3 corrections
  (C1 PlayerEntity.Current, C2 IfNot* fail-OPEN, C3 unknown-name fail-OPEN)
  incorporated into M1/M5
- [ ] M1: `StoryletRegistry.LoadFromJson` round-trips storylets; matches
  `HouseDramaLoader` post-fix `_loaded` semantics; **rejects storylets with
  unknown trigger/effect names** (C3 fix)
- [ ] M2: `StoryletPart` survives `Capture → Save → Load → ApplyLoadedGame`;
  `Current` re-set on both bootstrap AND load; reactor re-registered
- [ ] M2: `SaveWriter.FormatVersion = 3`
- [ ] M3: `OneShot` fires exactly once; non-OneShot every tick predicate
  passes; cascade storylets fire next tick (not same tick)
- [ ] M4: One stage per tick per quest; `IfQuestStage` and
  `IfQuestComplete` evaluate correctly; OnEnter effects fire on entry
- [ ] M5: All new predicates and actions registered inside
  `EnsureInitialized()`; survive `ConversationPredicates.Reset()` and
  `ConversationActions.Reset()` paths
- [ ] M5: `PlayerEntity.Current` set on bootstrap AND load; reset on test
  cleanup; `IfPlayerHas*` family resolves via `PlayerEntity.Current` (not
  `listener`) — closes the C1 finding
- [ ] M5: `ConversationPredicates.IsRegistered`/`ConversationActions.IsRegistered`
  exist and are consumed by M1's storylet-load validation
- [ ] M6: End-to-end test passes; demo storylet visible in Play Mode
  (sets a fact via dialogue → tick → MessageLog announces fire)
- [ ] All existing 2242 EditMode tests still green

---

## Critical files (modify or read)

**Will modify or extend:**
- `Assets/Scripts/Presentation/Bootstrap/GameBootstrap.cs` — attach
  `StoryletPart`, register reactor, re-wire on load
- `Assets/Scripts/Gameplay/Save/SaveSystem.cs` — `FormatVersion=3`
- `Assets/Scripts/Gameplay/Conversations/ConversationPredicates.cs` —
  new predicates inside `EnsureInitialized()`
- `Assets/Scripts/Gameplay/Conversations/ConversationActions.cs` —
  new actions inside `EnsureInitialized()`

**Read-only references (existing patterns to mirror):**
- `Assets/Scripts/Gameplay/NarrativeState/NarrativeStatePart.cs` — Part
  shape, `Current` accessor, `ISaveSerializable`, reactor list, register-on-load
- `Assets/Scripts/Gameplay/NarrativeState/INarrativeReactor.cs`
- `Assets/Scripts/Gameplay/HouseDrama/HouseDramaLoader.cs` (post-fix
  `83e9522`) — registry pattern, `_loaded` semantics, `Reset` behavior
- `Assets/Scripts/Gameplay/HouseDrama/HouseDramaData.cs` — JSON-loaded
  data model with Validate(), `[Serializable]` discipline

---

## Verification (end-to-end)

Per the prior plan's pattern: run Unity Test Runner → EditMode → all green
(target 2242 + ~50 new tests = ~2290 total). Then a Play Mode smoke test:

1. Author `Resources/Content/Data/Storylets/Demo.json`:
   ```json
   {
     "Storylets": [{
       "ID": "demo_first_warden",
       "OneShot": true,
       "Triggers": [{"Key": "IfFact", "Value": "dev_met_warden:>=:1"}],
       "Effects": [{"Key": "AddMessage", "Value": "Word spreads of the warden..."}]
     }]
   }
   ```
2. Boot the game, talk to a warden, pick "Farewell." (sets
   `dev_met_warden:1` from the existing throwaway demo)
3. End the next turn → `MessageLog` shows "Word spreads of the warden..."
4. Save (F5) → reload (F6) → end a turn → message does **not** repeat
   (OneShot honored across save/load round-trip)

If this works, the storylet/quest layer is shipping. The throwaway warden
demo in `Wardens.json` can be reverted at any point afterward — its only
job (proving FactBag survives save/load) is now redundant with the storylet
test coverage.

---

## Progress log

| Date | Milestone | Status | Notes |
|---|---|---|---|
| 2026-04-26 | Plan | ✅ | Plan document committed |
| 2026-04-26 | M0 | ✅ | Verification sweep: 6 claims confirmed, 3 corrections (C1 PlayerEntity.Current static needed; C2 IfNot* family fails-OPEN with null contexts; C3 unknown predicates/actions fail-OPEN — load-time validation added to M1) |
| 2026-04-26 | M1 | ✅ | Schema + StoryletRegistry shipped with load-time name validation (commit 2938000); 19 EditMode tests green; 2261/2261 total. Includes ConversationPredicates/Actions.IsRegistered accessors. |
