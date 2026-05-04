# Quest System — plan (Tier 3, Qud-parity port)

> **NOT greenfield.** CoO already has a half-built scaffold:
> `StoryletPart` (singleton on world entity) tracks active quests +
> one-shot storylets; `QuestState`/`QuestData`/`QuestStageData` types
> are defined; storylet effect dispatch works; ISaveSerializable
> round-trip works. The trigger/effect dispatch loop for QUESTS
> specifically is "M4 territory" placeholder (`StoryletPart.cs:93`,
> `if (s.IsQuest) continue; // M4 territory`).
>
> This plan completes M4 + adds the conversation-side wiring,
> reward actions, quest log UI, showcase scenario, and diag channel,
> all with Qud `XRL.World.Quest` parity in mind.

## What CoO has TODAY (verified scaffolding survey)

| Subsystem | Status | File |
|---|---|---|
| `StoryletPart` (world singleton, `Current` static, ISaveSerializable) | ✅ M3 shipped | `Storylets/StoryletPart.cs` |
| `QuestState { QuestId, CurrentStageIndex, EnteredStageAtTurn }` | ✅ data shape | `Storylets/QuestState.cs` |
| `QuestData { List<QuestStageData> Stages }` + `QuestStageData { ID, Triggers, OnEnter }` | ✅ JSON shape | `Storylets/StoryletData.cs` |
| `StoryletPart.StartQuest / IsQuestActive / GetQuestState / GetActiveQuests` accessors | ✅ implemented | `StoryletPart.cs:42-64` |
| `StoryletPart.OnTickEnd` polls each tick, fires non-quest storylets | ✅ M3 (storylets only) | `StoryletPart.cs:84-111` |
| Quest stage dispatch (M4 placeholder) | ⚠️ TODO | `StoryletPart.cs:93` |
| ISaveSerializable for quests dict | ✅ shipped | `StoryletPart.cs:115-151` |
| `StoryletRegistry.GetAll()` (loads JSON) | ✅ existing | `Storylets/StoryletRegistry.cs` |
| `ConversationPredicates` registry (17 predicates) | ✅ existing | `Conversations/ConversationPredicates.cs` |
| `ConversationActions` registry (15+ actions) | ✅ existing | `Conversations/ConversationActions.cs` |
| `FactionManager` + `PlayerReputation.Modify(faction, delta)` | ✅ existing | `AI/FactionManager.cs`, `PlayerReputation.cs` |
| `LevelingSystem.AwardKillXP(killer, victim, zone)` | ✅ existing | `Stats/LevelingSystem.cs` |
| `TradeSystem.GetDrams / SetDrams` | ✅ existing | `Economy/TradeSystem.cs` |
| `ConversationActions.GiveItem` / `TakeItem` / `ChangeFactionFeeling` / `SetFact` / `AddFact` | ✅ existing | `Conversations/ConversationActions.cs` |
| `KnowledgePart.Reveal/Knows` for per-NPC knowledge tiers | ✅ existing | `NarrativeState/KnowledgePart.cs` |
| `NarrativeStatePart` + `FactBag` for global facts | ✅ existing | `NarrativeState/NarrativeStatePart.cs`, `FactBag.cs` |
| Diag substrate (6 default-on channels) | ✅ existing | `Shared/Utilities/Diag.cs` |

## Qud-parity matrix

| Qud feature | Qud file | CoO equivalent | Priority |
|---|---|---|---|
| `Quest` class with `StepsByID` + `Manager` + flags | `XRL.World/Quest.cs` | `QuestData.Stages` (CoO uses ordered list, not dict) | ⭐⭐⭐ KEEP CoO shape |
| `QuestStep` with `Finished`/`Failed`/`Awarded` flag bits | `XRL.World/QuestStep.cs` | NEW: extend `QuestState` to track stage-history flags | ⭐⭐⭐ |
| Dual registries: `Quests` (active) + `FinishedQuests` | `XRL/XRLGame.cs` | NEW: add `_completedQuests` HashSet to `StoryletPart` | ⭐⭐⭐ |
| `QuestLoader.QuestsByID` (XML blueprint cache) | `XRL.World/QuestLoader.cs` | `StoryletRegistry` already loads JSON quest definitions | ⭐⭐⭐ EXISTS |
| `StartQuest` / `FinishQuest` / `FinishQuestStep` API | `XRL/XRLGame.cs` | NEW conversation actions: `StartQuest`, `AdvanceQuestStage`, `CompleteQuest`, `FailQuest` | ⭐⭐⭐ |
| Conversation `<QuestHandler Action="start|step|finish">` | `Conversations.Parts/QuestHandler.cs` | Use existing `ConversationParam` actions on choice nodes (no new Part needed) | ⭐⭐⭐ |
| Conversation conditionals: `IfHaveQuest`, `IfFinishedQuest` | `XRL.World/DynamicQuestConversationHelper.cs` | NEW conversation predicates: `IfQuestActive`, `IfQuestStage`, `IfQuestNotStarted`, `IfQuestCompleted` | ⭐⭐⭐ |
| `QuestStartedEvent` (pooled, cascades to all systems) | `XRL.World/QuestStartedEvent.cs` | NEW diag channel `quest` (Started, StageAdvanced, Completed) | ⭐⭐⭐ |
| `IQuestSystem` abstract base for custom quest logic | `XRL/IQuestSystem.cs` | DEFER — CoO uses storylet `OnEnter` effects for per-stage logic instead | ⭐⭐ |
| `QuestManager` IPart with `OnStepComplete`/`OnQuestComplete` hooks | `XRL.World/QuestManager.cs` | DEFER — same coverage via `OnEnter` effects (M4 dispatch) | ⭐⭐ |
| `DynamicQuestReward` (StepXP + rewards[] + postrewards[]) | `XRL.World/DynamicQuestReward.cs` | Reuse existing actions (`GiveItem`, `ChangeFactionFeeling`, `AddFact`) + NEW `AwardXP`, `GiveDrams` actions | ⭐⭐⭐ |
| Quest log UI rendering steps with status icons | `XRL.UI/QuestLog.cs` | NEW `QuestLogUI` reading `StoryletPart.Current.GetActiveQuests()` | ⭐⭐ |
| Hagiography / accomplishments | `XRL.World/Quest.cs` (Hagiograph field) | DEFER — CoO doesn't have a hagiography system yet | ⭐ skip |
| HUD waypoint markers | (not present in Qud either) | DEFER | ⭐ skip |

## Design tensions + CoO divergence decisions

These are deliberate divergences from Qud's exact shape to fit CoO's
existing architecture:

1. **🟡 Stage list (CoO) vs Step dict (Qud).** Qud uses
   `StringMap<QuestStep> StepsByID` allowing arbitrary step navigation
   (skip, branch, parallel-completion). CoO's existing
   `QuestData.Stages` is `List<QuestStageData>` (ordered, linear).
   **Decision: keep CoO's linear shape for v1.** Branching quests
   are deferred. If we ever need branching, promote `Stages` to a
   `Dictionary<string, QuestStageData>` + add `NextStageId` to
   `QuestStageData`. v1 covers the canonical "step 1 → step 2 →
   complete" flow which is 90% of Qud's actual quests.

2. **🟡 No `IQuestSystem` abstract.** Qud's `IQuestSystem` lets
   custom code hook quest lifecycle (e.g., the `GolemQuestSystem`
   procedurally generates extra steps). CoO's existing convention is
   declarative: storylet/quest content lives in JSON, effects dispatch
   via `ConversationActions`. **Decision: rely on `OnEnter` action
   lists for v1.** A future commit can add an optional
   `Quest.SystemTypeName` field that resolves to an `IQuestSystem`
   instance via reflection if procedural quests are needed.

3. **🟡 Storylet vs Quest distinction.** CoO's `StoryletPart` already
   distinguishes via `s.IsQuest` flag (= "has non-null Quest
   sub-object"). One-shot scripted events (storylets) and
   multi-stage progress trackers (quests) live side-by-side in the
   same part. **Decision: keep them in one part.** Quests just get
   their own dispatch loop in OnTickEnd's M4 branch.

4. **🟡 Reward shape.** Qud's `DynamicQuestReward` is a nested
   object with rewards[] of polymorphic award elements. **Decision:
   in CoO, rewards live in the quest's `OnEnter` action list of the
   *terminal* stage** (or a synthetic "completion" stage). Reuses
   existing action infrastructure. Future granularity (per-step XP
   like Qud's StepXP) goes in by adding stage-level `OnEnter`
   action lists which already exist.

5. **🔵 Per-NPC quest awareness.** Qud has no separate concept;
   conversation conditionals like `IfHaveQuest` are checked
   against the global registry. CoO can match this exactly (just
   read `StoryletPart.Current.IsQuestActive(id)`), but should ALSO
   integrate with `KnowledgePart` so an NPC can "know about" a
   quest topic. **Decision: v1 uses global registry only.**
   `KnowledgePart` integration is a polish pass.

## Sub-milestones (smallest blast radius first)

### QS.1 — Plan + branch (this commit)

- `Docs/QUEST-SYSTEM.md` (this file)
- Branch `feat/quest-system` cut from `main` at `5716632`

### QS.2 — Conversation predicates: `IfQuestActive` / `IfQuestStage` / `IfQuestNotStarted` / `IfQuestCompleted` (one commit)

Pure additive changes to `Conversations/ConversationPredicates.cs` —
4 new `Register(...)` calls. No new types.

```csharp
Register("IfQuestActive", (speaker, listener, arg) =>
    StoryletPart.Current?.IsQuestActive(arg) ?? false);

Register("IfQuestStage", (speaker, listener, arg) => {
    // arg = "questId:stageId" or "questId:stageIndex"
    var parts = arg.Split(':');
    if (parts.Length != 2) return false;
    var s = StoryletPart.Current?.GetQuestState(parts[0]);
    if (s == null) return false;
    if (int.TryParse(parts[1], out int idx)) return s.CurrentStageIndex == idx;
    // string compare against stage ID requires reading QuestData by ID
    var quest = StoryletRegistry.FindQuest(s.QuestId);
    if (quest == null) return false;
    return s.CurrentStageIndex < quest.Stages.Count
        && quest.Stages[s.CurrentStageIndex].ID == parts[1];
});

Register("IfQuestNotStarted", (speaker, listener, arg) =>
    !(StoryletPart.Current?.IsQuestActive(arg) ?? false)
    && !(StoryletPart.Current?.IsQuestCompleted(arg) ?? false));

Register("IfQuestCompleted", (speaker, listener, arg) =>
    StoryletPart.Current?.IsQuestCompleted(arg) ?? false);
```

**Also adds**: `StoryletPart.IsQuestCompleted(string)` + a
`HashSet<string> _completedQuests` field + save/load roundtrip.

**RED → GREEN tests** in `Tests/.../Storylets/QuestPredicateTests.cs`:
- `IfQuestActive_ReturnsTrue_WhenQuestStarted`
- `IfQuestActive_ReturnsFalse_WhenQuestNotStarted`
- `IfQuestStage_ReturnsTrue_WhenIndexMatches`
- `IfQuestStage_ReturnsTrue_WhenStageIdMatches`
- `IfQuestNotStarted_ReturnsTrue_WhenNeverStarted`
- `IfQuestCompleted_ReturnsTrue_AfterCompletion`
- Counter-check: `IfQuestActive_AfterCompletion_ReturnsFalse`
  (proves active and completed are disjoint sets)

### QS.3 — Conversation actions: `StartQuest` / `AdvanceQuestStage` / `CompleteQuest` / `FailQuest` (one commit)

4 new entries in `ConversationActions`. Each calls into `StoryletPart.Current`.

```csharp
Register("StartQuest", (speaker, listener, arg) => {
    if (StoryletPart.Current.IsQuestActive(arg)) return;        // idempotent
    if (StoryletPart.Current.IsQuestCompleted(arg)) return;     // no re-take
    var state = new QuestState {
        QuestId = arg,
        CurrentStageIndex = 0,
        EnteredStageAtTurn = TurnManager.Active?.TickCount ?? 0,
    };
    StoryletPart.Current.StartQuest(state);
    // Diag observability:
    Diag.Record(category: "quest", kind: "Started",
        actor: listener, payload: new { questId = arg });
    // Fire stage 0 OnEnter effects immediately so the player sees
    // the first "deliver this letter" message etc.
    var quest = StoryletRegistry.FindQuest(arg);
    if (quest != null && quest.Stages.Count > 0)
        ConversationActions.ExecuteAll(quest.Stages[0].OnEnter, speaker, listener);
});

Register("AdvanceQuestStage", (speaker, listener, arg) => { ... });
Register("CompleteQuest", (speaker, listener, arg) => { ... });
Register("FailQuest", (speaker, listener, arg) => { ... });
```

**`AdvanceQuestStage`**:
- Increment `CurrentStageIndex`
- If new index ≥ Stages.Count → call CompleteQuest (auto-graduation)
- Else fire new stage's `OnEnter` actions
- Record `quest/StageAdvanced` diag

**`CompleteQuest`**:
- Move from `_quests` to `_completedQuests`
- Record `quest/Completed` diag

**`FailQuest`**: Move to `_completedQuests` with a "failed" suffix? No
— v1 just removes from active without recording in completed. Failed
quests can be retaken. Document the choice as 🟡.

**RED → GREEN tests** in `Tests/.../Storylets/QuestActionTests.cs` (8):
- `StartQuest_AddsToActiveQuests`
- `StartQuest_FiresStage0OnEnterImmediately`
- `StartQuest_OnAlreadyActive_NoOp`
- `StartQuest_OnAlreadyCompleted_NoOp`
- `AdvanceQuestStage_IncrementsStageIndex`
- `AdvanceQuestStage_FiresNewStageOnEnter`
- `AdvanceQuestStage_AtTerminalStage_AutoCompletes`
- `CompleteQuest_RemovesFromActive_AddsToCompleted`

### QS.4 — Quest dispatch loop (M4) in `StoryletPart.OnTickEnd` (one commit)

Replaces the `if (s.IsQuest) continue;` skip at line 93 with active-
quest stage-trigger evaluation:

```csharp
// Quest dispatch (M4): for each active quest, evaluate the CURRENT
// stage's Triggers — if all match, advance to the next stage.
// Storylets and quests share the single-pass deterministic dispatch
// rule from M3: snapshot eligibility, then mutate.
var activeQuests = GetActiveQuests();
for (int i = 0; i < activeQuests.Count; i++)
{
    var qs = activeQuests[i];
    var qd = StoryletRegistry.FindQuest(qs.QuestId);
    if (qd == null) continue;
    if (qs.CurrentStageIndex >= qd.Stages.Count) continue;
    var stage = qd.Stages[qs.CurrentStageIndex];
    if (ConversationPredicates.CheckAll(stage.Triggers, null, null))
        _eligibleStageAdvances.Add(qs);
}
for (int i = 0; i < _eligibleStageAdvances.Count; i++)
    AdvanceQuestStage(_eligibleStageAdvances[i].QuestId);
_eligibleStageAdvances.Clear();
```

**RED → GREEN tests** in `Tests/.../Storylets/QuestDispatchTests.cs` (6):
- `QuestStage_TriggersCheckedEachTick_AdvanceWhenSatisfied`
- `QuestStage_TriggersUnsatisfied_NoAdvance`
- `QuestStage_AutoCompletesAtTerminal`
- `QuestStage_OnEnterEffects_FireOnAdvance`
- `QuestStage_DispatchSinglePass_DoesNotCascadeMultipleAdvancesPerTick`
  (counter-check: even if stage 1 effects flip stage 2's trigger,
  stage 2 doesn't fire this tick — fires next tick)
- `QuestState_EnteredStageAtTurn_UpdatesOnAdvance`

**Adversarial test**:
- `QuestDispatch_NullStoryletRegistry_NoCrash`

### QS.5 — Reward action wrappers: `AwardXP` / `GiveDrams` (one commit)

2 new `ConversationActions` entries. Wraps existing infrastructure:

```csharp
Register("AwardXP", (speaker, listener, arg) => {
    if (!int.TryParse(arg, out int amt) || amt <= 0) return;
    if (listener == null) return;
    var xp = listener.GetStat("Experience");
    if (xp == null) return;
    xp.BaseValue += amt;
    LevelingSystem.CheckLevelUp(listener, /*zone*/null);
    MessageLog.Add($"You gain {amt} XP.");
});

Register("GiveDrams", (speaker, listener, arg) => {
    if (!int.TryParse(arg, out int amt) || amt <= 0) return;
    if (listener == null) return;
    TradeSystem.SetDrams(listener, TradeSystem.GetDrams(listener) + amt);
    MessageLog.Add($"You receive {amt} drams.");
});
```

**RED → GREEN tests** (4):
- `AwardXP_AddsToExperienceStat`
- `AwardXP_TriggersLevelUpWhenThresholdCrossed`
- `GiveDrams_AddsToCurrencyProperty`
- `AwardXP_NegativeOrZero_NoOp` (defensive)

### QS.6 — Quest log UI (one commit)

New file `Presentation/UI/QuestLogUI.cs`. Reads
`StoryletPart.Current.GetActiveQuests()` + `_completedQuests`.
Renders title + current-stage description in a centered popup.

Hotkey binding: `q` → toggle quest log (gated to gameplay state).

Display format:
```
╔══ Active Quests (2) ══════════════════════╗
║                                             ║
║  ▸ The Iron Key                            ║
║      Stage: deliver_to_finn (entered T78)  ║
║                                             ║
║  ▸ Find Marceline                          ║
║      Stage: search_cave (entered T112)     ║
║                                             ║
╠══ Completed (1) ══════════════════════════╣
║  ✓ Goblin Hunt                             ║
╚═══════════════════════════════════════════╝
```

**RED → GREEN tests** (4):
- `QuestLogUI_BuildsLines_FromActiveQuests`
- `QuestLogUI_DisplaysCurrentStageOnly`
- `QuestLogUI_GroupsActiveAndCompleted`
- `QuestLogUI_HandlesEmptyState_NoActiveQuests`

### QS.7 — `QuestShowcase` scenario + scenario diag fixture (one commit)

Following the pattern of the 11 prior scenario diag fixtures from
this session. New file
`Scenarios/Custom/QuestShowcase.cs`:

- 2-stage quest: "find an iron key, return to Marceline"
- Player starts with no key
- NPC "Marceline" 3E of player with a `[Take quest]` conversation choice
- An IronKey 5E of player on the floor
- Quest stage 0 trigger: `IfHaveItem("IronKey")`
- Quest stage 1 (terminal) `OnEnter`: `AwardXP("100")`, `GiveDrams("50")`,
  `ChangeFactionFeeling("Players,Adventurers,+25")`

Scenario diag tests (4) in
`Tests/.../Scenarios/QuestShowcaseDiagTests.cs`:
- `Showcase_TakeQuest_RecordsQuestStarted_DiagEntry`
- `Showcase_PickUpKey_AdvanceTriggers_RecordsStageAdvanced`
- `Showcase_ReturnToMarceline_CompletesQuest_AwardsRewards`
- `Showcase_BuildScenario_NoQuestActive_NoQuestDiagRecords` (counter-check)

Smoke test in `ScenarioCustomSmokeTests.cs`.
Menu entry: `Caves Of Ooo / Scenarios / Combat Stress / Quest Showcase` priority 117.

### QS.8 — Self-review + roadmap update + merge + push

Cold-eye Q1-Q4 + roadmap flip from 💡 → ✅ + commit per §2.3 +
merge `--no-ff` + push.

## New diag channel: `quest`

```
quest/Started        { questId }                      — fired by StartQuest
quest/StageAdvanced  { questId, fromIndex, toIndex }  — fired by AdvanceQuestStage
quest/Completed      { questId, totalStages }         — fired by CompleteQuest
```

7th default-on channel. Adds to `Diag.DefaultOnCategories`. Same
shape as the other Diag.Record sites added this session.

## Critical files

### New files (QS.2–QS.7)

| Path | Purpose |
|---|---|
| `Docs/QUEST-SYSTEM.md` | Plan doc (this file, QS.1) |
| `Assets/Tests/EditMode/Gameplay/Storylets/QuestPredicateTests.cs` | QS.2 |
| `Assets/Tests/EditMode/Gameplay/Storylets/QuestActionTests.cs` | QS.3 |
| `Assets/Tests/EditMode/Gameplay/Storylets/QuestDispatchTests.cs` | QS.4 |
| `Assets/Tests/EditMode/Gameplay/Storylets/QuestRewardActionTests.cs` | QS.5 |
| `Assets/Tests/EditMode/Presentation/UI/QuestLogUITests.cs` | QS.6 |
| `Assets/Scripts/Presentation/UI/QuestLogUI.cs` | QS.6 quest log popup |
| `Assets/Scripts/Scenarios/Custom/QuestShowcase.cs` | QS.7 manual playtest |
| `Assets/Tests/EditMode/Gameplay/Scenarios/QuestShowcaseDiagTests.cs` | QS.7 scenario diag fixture |
| `Assets/Resources/Content/Storylets/IronKeyQuest.json` | QS.7 quest content |

### Modified files

| Path | Change |
|---|---|
| `Assets/Scripts/Gameplay/Storylets/StoryletPart.cs` | QS.2: + `IsQuestCompleted` + `_completedQuests`; QS.3: `AdvanceQuestStage` + `CompleteQuest` methods; QS.4: replace M4 placeholder with stage-dispatch loop; save/load extension |
| `Assets/Scripts/Gameplay/Conversations/ConversationPredicates.cs` | QS.2: 4 new predicates |
| `Assets/Scripts/Gameplay/Conversations/ConversationActions.cs` | QS.3: 4 new quest-lifecycle actions; QS.5: `AwardXP` + `GiveDrams` |
| `Assets/Scripts/Shared/Utilities/Diag.cs` | QS.4: + `quest` channel default-on |
| `Assets/Scripts/Gameplay/Storylets/StoryletRegistry.cs` | QS.4: + `FindQuest(questId)` lookup helper |
| `Assets/Editor/Scenarios/ScenarioMenuItems.cs` | QS.7 menu entry priority 117 |
| `Assets/Tests/EditMode/Gameplay/Scenarios/ScenarioCustomSmokeTests.cs` | QS.7 smoke test |
| `Docs/CONTENT-ROADMAP.md` | QS.8 flip Quest System 💡 → ✅ |

## Reusable utilities (don't reinvent)

| Utility | Path |
|---|---|
| `StoryletPart.Current` (world-singleton accessor) | `Storylets/StoryletPart.cs:24` |
| `StoryletPart.StartQuest / IsQuestActive / GetQuestState` | `Storylets/StoryletPart.cs:42-64` |
| `ConversationPredicates.Register/CheckAll` | `Conversations/ConversationPredicates.cs` |
| `ConversationActions.Register/Execute/ExecuteAll` | `Conversations/ConversationActions.cs` |
| `StoryletRegistry.GetAll()` | `Storylets/StoryletRegistry.cs` |
| `LevelingSystem.CheckLevelUp(entity, zone)` | `Stats/LevelingSystem.cs` |
| `TradeSystem.GetDrams / SetDrams` | `Economy/TradeSystem.cs` |
| `PlayerReputation.Modify(faction, delta)` | `Faction/PlayerReputation.cs` |
| `Diag.Record(channel, kind, ..., payload)` | `Shared/Utilities/Diag.cs` |
| `ScenarioTestHarness.CreateContext(playerBlueprint:"Player")` | `Tests/.../ScenarioTestHarness.cs` |

## Self-review pre-flagged 🟡 findings

- **🟡 Linear-stages-only.** v1 doesn't support branching. Documented
  above (Tension #1). Note in QS.4 commit body.
- **🟡 No `IQuestSystem` abstract.** Custom procedural quest logic
  isn't possible in v1 (Tension #2). Out of scope; future ship.
- **🟡 `FailQuest` removes without recording.** Failed quests can
  be retaken. Discuss in QS.3 commit body. If playtest reveals the
  player needs "you already failed this" feedback, add a
  `_failedQuests` HashSet (mirroring `_completedQuests`) in a
  follow-on commit.
- **🟡 Quest log UI is read-only.** No "abandon quest" affordance in
  v1. Add later if needed.
- **🔵 Diag channel `quest` chosen over `quests`.** Singular noun
  matches the existing 6 channel names (event/effect/damage/turn/
  furniture/trade — all single-word).
- **🔵 Reward actions called from terminal stage `OnEnter`.** Not a
  separate `Reward` field. Simpler shape; reuses existing action
  infrastructure. Documented in Tension #4.
- **⚪ No HUD waypoint markers.** Out of scope (Qud doesn't have
  this either).
- **⚪ No quest hagiography / accomplishments.** CoO doesn't have a
  hagiography system; defer until one exists.

## Verification (post-implementation)

Three layers:

1. **Per-fixture RED → GREEN** during QS.2-QS.7:
   - QS.2: 7 predicate tests
   - QS.3: 8 action tests
   - QS.4: 7 dispatch tests
   - QS.5: 4 reward action tests
   - QS.6: 4 UI tests
   - QS.7: 4 scenario diag tests + 1 smoke
   - **Total**: 35 new tests

2. **Targeted regression sweep**:
   ```
   run_tests EditMode group_names=[
     "QuestPredicateTests", "QuestActionTests", "QuestDispatchTests",
     "QuestRewardActionTests", "QuestLogUITests",
     "QuestShowcaseDiagTests",
     "StoryletPartTests", "StoryletDispatchTests",
     "ConversationPredicatesTests", "ConversationActionsTests",
     "ScenarioCustomSmokeTests"
   ]
   ```

3. **Manual playtest** via `QuestShowcase` (QS.7):
   - Click menu → scenario builds
   - Talk to Marceline → choose `[Take this quest]` (calls `StartQuest`)
   - Quest log popup (`q`) shows "The Iron Key — find an iron key" stage 0
   - Walk east, pick up the IronKey (auto-advances trigger via `IfHaveItem`)
   - Quest log shows "deliver to Marceline" stage 1
   - Talk to Marceline → choose `[Hand over the key]` (calls
     `AdvanceQuestStage`, terminal-stage auto-completes)
   - MessageLog shows "You gain 100 XP." + "You receive 50 drams."
   - Quest log shows quest in Completed section

## Implementation sequence

```
1. Plan to disk (QS.1, this commit)
2. Verification sweep against StoryletPart scaffold — done above
3. QS.2: 4 predicates + IsQuestCompleted + 7 tests
4. QS.3: 4 actions + Storylet helpers + 8 tests
5. QS.4: M4 dispatch loop + 7 tests
6. QS.5: AwardXP + GiveDrams + 4 tests
7. QS.6: QuestLogUI + 4 tests
8. QS.7: showcase + scenario diag fixture + 4 + 1 tests
9. Targeted regression sweep
10. Self-review + roadmap update + commit QS.8 + merge --no-ff + push
```

Expected total: ~120 lines new code + ~80 lines StoryletPart edits +
~280 lines new tests + ~120 lines scenario + UI + ~60 lines blueprint
JSON + this plan (~470 lines). ~1.5 days of focused work.

## What gets observable to the player after this ship

| Today | After QS |
|---|---|
| Quest scaffold exists in code; no quest content; no UI; no dispatch loop | Players can take a quest from an NPC, track progress in a quest log (`q`), get auto-advanced when triggers fire (e.g., picking up a quest item), receive XP+drams+rep rewards on completion |
| 6 diag channels (event/effect/damage/turn/furniture/trade) | + `quest` channel (Started/StageAdvanced/Completed) — every quest state change observable via diag_query |
| Conversation system has 17 predicates + 15 actions | + 4 quest predicates + 6 quest actions = 21 + 21 |
| `StoryletPart.OnTickEnd` skips quests at line 93 | M4 dispatch loop fires, advances active quests when stage triggers fire |
| 11 scenario diag fixtures from this session | + `QuestShowcaseDiagTests` makes 12 |

After this ship, the natural follow-on candidates are:
- **Branching quests** (Stages → Dictionary, NextStageId field)
- **`IQuestSystem` abstract** for procedural quests
- **Quest waypoint HUD markers** (zone-coords on quest stage data)
- **Failed-quest tracking** + "you already failed" UI feedback
- **Quest content authoring**: 5+ canonical quests (lost-letter, kill-N-snapjaws, fetch-rare-tonic, etc) using the new infrastructure
