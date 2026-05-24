# Q4 — Quest GameEvents (PLAN)

> Phase Q4 of `Docs/QUEST-SYSTEM-QUD-PARITY.md`. Fire CoO `GameEvent`s for
> the quest lifecycle so **other systems can REACT** (not just observe via
> diag) — parity with Qud's `IQuestEvent` family (`QuestStartedEvent` /
> `QuestStepFinishedEvent` / `QuestFinishedEvent`, dispatched to the player
> via `The.Game.HandleEvent(..., DispatchPlayer:true)`). **Enables Q5**
> (world-object Parts that react to quest progress). **Status: PLAN.**

## Why (diag vs events)

Q1–Q3 already emit `quest/*` **diag** records — the *observability* hook
(debugging, `diag_query`). Q4 adds **events** — the *reaction* hook:
a Part can `HandleEvent("QuestCompleted")` to open a door, spawn an NPC,
flip a fact, etc. Diag records stay; events are additive.

## Design

Fire 5 string-keyed `GameEvent`s at `StoryletPart`'s existing lifecycle
call sites (right where the matching diag record is emitted), on
`StoryletPart.LocalPlayer` (Qud-parity: dispatched to the player so player
Parts react). Null-guarded — `LocalPlayer == null` (pre-bootstrap/tests)
skips the event but keeps the diag.

| Event | Params | Fired from |
|---|---|---|
| `QuestStarted` | `QuestId` | `StoryletPart.StartQuest` |
| `QuestObjectiveFinished` | `QuestId`, `ObjectiveId` | `StoryletPart.FinishObjective` |
| `QuestStageAdvanced` | `QuestId`, `FromIndex`, `ToIndex` | `StoryletPart.AdvanceQuestStage` (advance branch) |
| `QuestCompleted` | `QuestId` | `StoryletPart.CompleteQuest` |
| `QuestFailed` | `QuestId` | `StoryletPart.FailQuest` (NEW — see sweep) |

Firing helper (one private method on `StoryletPart`):
```csharp
private static void FireQuestEvent(string id, string questId,
    string objId = null, int from = -1, int to = -1)
{
    var p = LocalPlayer;
    if (p == null) return;
    var e = GameEvent.New(id);
    e.SetParameter("QuestId", (object)questId);
    if (objId != null) e.SetParameter("ObjectiveId", (object)objId);
    if (from >= 0) { e.SetParameter("FromIndex", from); e.SetParameter("ToIndex", to); }
    p.FireEventAndRelease(e);
}
```

## Verification sweep

- ✅ Call sites exist on `StoryletPart`: `StartQuest`, `FinishObjective`,
  `AdvanceQuestStage`, `CompleteQuest` (all instance methods with access to
  the static `LocalPlayer`).
- 🟡 **`FailQuest` lives in `ConversationActions`, not `StoryletPart`** — it
  inlines `RemoveActiveQuest` + the `quest/Failed` diag. To fire
  `QuestFailed` from the same single source (and keep parity with the other
  events' "StoryletPart owns the lifecycle + diag + event" shape), add a
  `StoryletPart.FailQuest(questId, actor)` that does remove + diag + event,
  and have the conversation action delegate to it (like `CompleteQuest`
  already delegates). Small refactor; pin the diag still fires.
- ✅ `GameEvent` API: `GameEvent.New(name)` + `SetParameter(key, obj)` /
  `SetParameter(key, int)` + `entity.FireEventAndRelease(e)`; listeners
  `WantEvent(GameEvent.GetID(name))` + `HandleEvent`.

## Parity divergence (documented)

Qud dispatches pooled `IQuestEvent`s to the player AND global game systems
(`DispatchPlayer:true`). CoO fires string `GameEvent`s on `LocalPlayer`
only (covers player Parts). World-level reactors would need the event also
fired on `TurnManager.World` — **deferred** until a consumer needs it
(Q5 world-object Parts live on the objects/zone, reachable via the player
event or their own triggers). Note in the docstring.

## Sub-milestones

- **Q4.1** — fire the 5 events (+ the `StoryletPart.FailQuest` refactor) at
  the call sites, on `LocalPlayer`, null-guarded. TDD: a capturing test
  Part installed as `LocalPlayer` asserts each event fires with the right
  params; counter-checks: `LocalPlayer == null` → no throw + diag still
  fires; no-op lifecycle paths (re-finish, inactive) fire no event.

## Implementation log

**Q4.1 — quest GameEvents (DONE).**
- `StoryletPart.FireQuestEvent(eventId, questId, objId?, from?, to?)`
  helper — fires on `LocalPlayer` (null-guarded), string params via the
  `SetParameter(string,string)` overload (→ `GetStringParameter`), indices
  via `SetParameter(string,int)` (→ `GetIntParameter`).
- Fires: `QuestStarted` (StartQuest, only on a genuinely-new start),
  `QuestObjectiveFinished` (FinishObjective), `QuestStageAdvanced`
  (AdvanceQuestStage advance branch, with From/ToIndex), `QuestCompleted`
  (CompleteQuest), `QuestFailed` (new `StoryletPart.FailQuest`).
- **Refactor:** added `StoryletPart.FailQuest(questId, actor)` (remove +
  quest/Failed diag + QuestFailed event); the `FailQuest` conversation
  action now delegates to it (mirrors the CompleteQuest action), so failure
  side-effects live in ONE place. (Resolved the plan's 🟡.)

### Self-review (§5)
- ✅ Symmetry: events fire right after the matching diag; `FailQuest`
  mirrors `CompleteQuest` (single-source + action delegates).
- ✅ Cross-feature consistency: all 5 events share the param vocabulary
  (`QuestId`/`ObjectiveId`/`FromIndex`/`ToIndex`) + typed overloads.
- ✅ Counter-checks: no-`LocalPlayer` → event skipped but diag still fires
  (+ no throw); no-op finish → no event; re-start → no re-fire.
- 🔵 The `QuestStarted` EVENT fires from `StoryletPart.StartQuest` (the
  single path all starts go through), while the quest/Started DIAG is
  emitted by the StartQuest *conversation action* — so for Started alone,
  diag + event aren't co-located (the other 4 co-locate in StoryletPart).
  Event placement is correct (single source); the diag placement is
  pre-existing. Left as-is to avoid a double-emit risk.
- **Adversarial:** a dedicated file is NOT warranted — Q4.1 is a thin
  additive event-firing layer; its surfaces (diag emission = the events
  themselves; boundary = null LocalPlayer; no-op paths; re-fire) are all
  covered by the 8 tests + counter-checks.

### Files
- MOD `StoryletPart.cs`: `FireQuestEvent` helper + `FailQuest` + 5 fire calls
- MOD `ConversationActions.cs`: `FailQuest` action delegates to `StoryletPart.FailQuest`
- NEW `QuestEventTests.cs`: 8 tests (capturing Part as LocalPlayer)

### Tests
8 (`QuestEventTests`): each of the 5 events fires with correct params;
no-refire-on-restart; no-LocalPlayer-no-throw-diag-still-fires;
no-op-finish-no-event.
