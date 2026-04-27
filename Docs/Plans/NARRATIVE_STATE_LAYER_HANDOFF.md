# Narrative State Layer — Handoff for Local Continuation

> **Purpose:** Hand off context from the cloud Claude Code session that
> implemented the narrative-state-layer foundation to a fresh local Claude
> Code session. Read this file plus
> `Docs/Plans/NARRATIVE_STATE_LAYER_IMPLEMENTATION_PLAN.md` to pick up
> where we left off.

---

## TL;DR

**Branch:** `claude/task-d-zKesU` (pushed to `origin`)
**Status:** All 5 milestones (M1–M5) shipped as 7 commits. 78 EditMode tests
written but **not yet executed in Unity** (cloud sandbox had no Unity).
**Next action:** open project locally in Unity, run Test Runner, fix any
failures, then proceed to the storylet/quest layer (deferred from this phase).

---

## What Shipped

| Milestone | Files | Tests |
|---|---|---|
| Plan | `Docs/Plans/NARRATIVE_STATE_LAYER_IMPLEMENTATION_PLAN.md` | — |
| M1 — World entity + save round-trip | `SaveSystem.cs`, `GameBootstrap.cs` | `WorldEntityTests.cs` (7) |
| M2 — Fact + Knowledge parts | `FactBag.cs`, `NarrativeStatePart.cs`, `KnowledgePart.cs`, `QualityRegistry.cs` | `FactBagTests.cs` (12), `NarrativeStatePartTests.cs` (10), `KnowledgePartTests.cs` (10) |
| M3 — Conversation hooks | `ConversationPredicates.cs`, `ConversationActions.cs` | `NarrativeConversationTests.cs` (30) |
| M4a — TickEnd event | `TurnManager.cs`, `GameBootstrap.cs` | `TickEndTests.cs` (3) |
| M4b — Reactor dispatch | `INarrativeReactor.cs`, `NarrativeStatePart.cs` | `NarrativeReactorTests.cs` (6) |
| M5 — Review + cleanup | (namespace cleanup; plan doc update) | — |

**Total:** 18 production+test files, ~1770 lines added, 78 new tests.

---

## Architecture Quick-Reference

```
World Entity (singleton, BlueprintName="World", tag "WorldEntity")
├── NarrativeStatePart   ← global FactBag + event log + reactor registry
│   └── (registered via NarrativeStatePart.Current static accessor)
└── (future Parts attach here)

NPC Entity (any blueprint)
├── KnowledgePart        ← per-NPC int-tier knowledge (monotone increasing)
└── (existing parts)

TurnManager.World (static)  ← set by GameBootstrap; drives TickEnd dispatch
```

**Wiring:**
1. `GameBootstrap.OnAfterBootstrap` (fresh boot): creates `_world`, attaches
   `NarrativeStatePart`, sets `NarrativeStatePart.Current` and `TurnManager.World`.
2. `GameBootstrap.ApplyLoadedGame` (load): re-assigns all three from the
   loaded `GameSessionState`.
3. `TurnManager.EndTurn`: after `SpendEnergy`, fires pooled `TickEnd`
   `GameEvent` on `World` entity.
4. `NarrativeStatePart.HandleEvent`: receives `TickEnd`, calls
   `OnTickEnd(this)` on every registered `INarrativeReactor`.

**Conversation hooks (registered in lazy-init `RegisterDefaults()`):**
- Predicates (fail-closed): `IfFact:key:op:value`, `IfSpeakerKnows:topic:tier`,
  plus auto-inverted `IfNotFact` / `IfNotSpeakerKnows`
- Actions (silent no-op on bad args): `SetFact:k:v`, `AddFact:k:Δ`,
  `ClearFact:k`, `Reveal:Listener|Speaker:topic:tier`

**Save format change:** `SaveWriter.FormatVersion` bumped 1 → 2. Old saves
will throw `InvalidDataException` on load — intentional per pre-1.0 dev policy.

---

## Verification Status

### What's verified (static analysis)

I traced every test against production code without Unity available. All
78 tests are logically correct: signatures match, predicates/actions
handle all edge cases, save round-trips preserve state, reactor dispatch
fires correctly. Notes from that trace are in
`Docs/Plans/NARRATIVE_STATE_LAYER_IMPLEMENTATION_PLAN.md` §M5.

### What's NOT verified

**Unity test execution.** The cloud sandbox had no Unity / dotnet / mono.
The 78 tests have never been run.

**Action required:** open the project in Unity Editor, navigate to
`Window → General → Test Runner → EditMode`, and run all tests under the
`CavesOfOoo.Tests.EditMode.Gameplay.NarrativeState` namespace. Existing
EditMode tests (~957 from prior work) should also still pass — most
likely impact site is `SaveGraph*Tests` and `SaveSystem*Tests` because
of the `FormatVersion` bump and added `World` section in the save graph.

---

## Known Issues / Follow-Ups

These are documented in the plan's M5 review table; carrying them forward:

| Severity | Issue | Where | Recommendation |
|---|---|---|---|
| 🟡 | `Reveal` Target parsing is permissive — anything ≠ `"Speaker"` silently routes to listener | `ConversationActions.cs` Register("Reveal") | Tighten to explicit allow-list with warning on unknown target |
| 🟡 | `Part.WantEvent` doc claims optimization that `Entity.FireEvent` doesn't honor | `Part.cs` base doc + `Entity.FireEvent` | Either implement the filter or update the doc |
| 🟡 | `FactBag.Add` always inserts the key — `Add(k,+1)` then `Add(k,-1)` leaves `k=0` entry | `FactBag.cs` | Decide: prune zero values, or document |
| 🟡 | No end-to-end integration test (bootstrap → dialogue → save → load → predicate) | (new test file needed) | Add `NarrativeStateLayerEndToEndTests.cs` |
| 🟡 | No test driving `TickEnd` through `ProcessUntilPlayerTurn` (tests call `EndTurn` directly) | `TickEndTests.cs` | Add a `Tick → ProcessUntilPlayerTurn → reactor saw events` test |
| 🟡 | No test that reactors registered on `NarrativeStatePart.Current` (the production singleton) receive dispatch | `NarrativeReactorTests.cs` | Add a test exercising `Current` not a hand-built instance |
| ⚪ | Pre-existing: `endTurn` event in `TurnManager.EndTurn` is fired but never released | `TurnManager.cs` | Out of scope for this branch; plug in a follow-up |
| ⚪ | `Reveal` ignores negative tiers silently (`tier > current` excludes them) | `KnowledgePart.cs` | Add doc note |

---

## What's Out of Scope (Deferred)

- **Storylet/quest layer** — the next major plan. Storylets and quests as
  first-class entities reading/writing the state layer.
- **Full migration of House Drama** — kept untouched in this phase. Will
  fold into the storylet plan.
- **`quality_definitions.json` content** — `QualityRegistry` is wired but
  the `Resources/Content/Data/Qualities/` directory is empty by design.
- **Bounded event log** — currently unbounded by user request; revisit if
  memory pressure becomes real.

---

## Resuming Locally

```bash
cd ~/caves_of_ooo  # or wherever you cloned
git fetch origin
git checkout claude/task-d-zKesU
```

In Unity Hub: open the project. Run Test Runner → EditMode → all tests.

Then start a local Claude Code session in the project root:

```bash
cd ~/caves_of_ooo
claude
```

First prompt suggestion:

> Read `Docs/Plans/NARRATIVE_STATE_LAYER_HANDOFF.md` and
> `Docs/Plans/NARRATIVE_STATE_LAYER_IMPLEMENTATION_PLAN.md`. Tests have
> [passed | failed: <paste failures>]. [Continue with follow-ups | Begin
> the storylet/quest layer plan].

---

## Commit Trail (newest first)

```
f4ed208 docs+fix(narrative/M5): self-review, parity audit, namespace cleanup
c96123e feat(narrative/M4b): INarrativeReactor registry + TickEnd dispatch
6356c27 feat(narrative/M4a): fire TickEnd on world entity after each EndTurn
0a845e5 feat(narrative/M3): conversation predicates and actions for narrative state
63e2546 feat(narrative/M2): FactBag, NarrativeStatePart, KnowledgePart, QualityRegistry
0487255 feat(narrative/M1): introduce singleton world entity in GameBootstrap
a14266c docs(narrative): add Narrative State Layer implementation plan
```

Each commit is independently revertable per Methodology Template §1.4.

---

## Key Design Decisions (for context)

1. **Part-based, not static-manager-based.** Unlike `HouseDramaRuntime`
   (static singleton), `NarrativeStatePart` is a Part on the world entity.
   This gets serialization, tagging, and event routing for free —
   matching the Qud "everything is a Part" idiom.

2. **Polled, not reactive.** Reactors fire on `TickEnd` (end of every
   `EndTurn`), not on individual fact mutations. Simpler to reason about,
   easier to debounce, no risk of mid-mutation re-entrancy.

3. **`ISaveSerializable`, not `WritePublicFields`.** The default
   reflection-based save path can't handle `Dictionary<string,int>` or
   `List<string>`. Both `NarrativeStatePart` and `KnowledgePart` opt into
   custom Save/Load.

4. **Static `Current` accessor pattern.** Matches existing
   `ConversationActions.Factory`, `MessageLog.TickProvider`,
   `SettlementManager.Current`. Set by `GameBootstrap` on boot AND on
   load (because `LoadEntityBody` doesn't call `Initialize()`).

5. **Fail-closed predicates, silent-no-op actions.** Predicates returning
   false makes typos visible in dialogue routing. Actions failing silent
   matches existing `SetTag`/`AddFact` convention. Designer typos in
   action args won't crash, but won't have effect either — flag for
   future "lint this conversation JSON" tooling.

6. **`FormatVersion` bumped, no migration.** Pre-1.0 dev — old saves are
   intentionally invalidated. Document policy: every save-shape change
   bumps the version and breaks compatibility.
