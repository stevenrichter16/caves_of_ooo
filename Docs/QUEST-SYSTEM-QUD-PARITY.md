# Quest / Objective System — Qud Parity Analysis

> **Status:** Investigation + analysis (no code yet). Produced 2026-05-23.
> **Purpose:** Deep-dive Caves of Qud's quest/mission/objective architecture
> and map it against what Caves of Ooo **already** has, so the next build
> phase can close the gap as a *Qud-parity extension* rather than a
> greenfield system.
> **Sources:** direct reads of the full Qud decompile at
> `/Users/steven/qud-decompiled-project/` (file:line cited inline) +
> firsthand cross-checks of CoO source under `Assets/Scripts/`.

---

## 0. Executive summary

**CoO is not greenfield here.** A storylet/quest layer is already
*shipped* — `StoryletPart` (world singleton) with a real quest API,
conversation-driven start/advance/complete, a per-turn dispatch loop,
reward actions, save/load, a live `quest` diag channel, ~113 tests, and
two pieces of quest content. The stale intro of `Docs/QUEST-SYSTEM.md`
calls it a "half-built scaffold / M4 placeholder"; that is **wrong** —
the dispatch loop and lifecycle are fully implemented (verified
firsthand at `StoryletPart.cs:253` `OnTickEnd`). So this effort is
**gap-closing toward Qud parity**, not a build-from-scratch.

**The single biggest architectural delta:**

| | Qud | CoO (today) |
|---|---|---|
| Objective model | **Flat SET of independent `QuestStep`s** — finished in any order, `~`-delimited multi-finish, per-step `XP`/`Optional`/`Hidden`/`Failed` flags; quest auto-finishes when all non-`Optional` steps done | **Ordered LINEAR list of `Stage`s** — one "current stage", strictly sequential advance, no per-objective flags, no XP-per-objective |

Qud expresses *parallel* objectives ("kill 3 snapjaws, fetch the relic,
and talk to the elder — in any order"); CoO today expresses only
*sequential* stages ("do A, then B, then C"). **Closing that gap is the
heart of "Qud parity."**

**Other notable gaps (detail in §3):** no Quest Log UI renderer (state
layer exists, no MonoBehaviour + no `q` hotkey — quests are currently
*invisible to the player*, same class of bug as the gas-visibility
issue); no quest `GameEvent`s for cross-system reactions (diag only);
no world-object quest triggers (kill/take/interact Parts); no quest
metadata (name/giver/level/reward fields on the quest itself); no
failed-quest tracking; no procedural/dynamic quests.

**Recommended first phase: Q1 — Quest Log UI.** Highest player-visible
value, lowest risk, foundation already exists. Then Q3 (parallel
objectives) as the core model-parity work. Full phasing in §5.

---

## 1. Qud's quest architecture

### 1.1 Data model — `Quest` + `QuestStep`

**`Quest`** (`XRL.World/Quest.cs`): a quest instance.
- Identity/flavor: `ID`, `Name`, `Level`, `Finished`, plus narrative
  fields `Accomplishment`, `Achievement`, `Hagiograph`,
  `HagiographCategory`, `Gospel`.
- Quest-giver metadata: `QuestGiverName`, `QuestGiverLocationName`,
  `QuestGiverLocationZoneID`.
- Auto-reward fields: `Factions` (comma-list) + `Reputation` (int) →
  applied on finish.
- **`StepsByID`** : `Dictionary<string, QuestStep>` — the objectives,
  keyed by ID (a SET, not a sequence).
- Extensibility: `SystemType` (a `Type` → `IQuestSystem`, §1.4),
  `Manager` (a `QuestManager` IPart, §1.4), `dynamicReward`
  (`DynamicQuestReward`, §1.7).
- Free-form state: `Properties` (`Dictionary<string,object>`) +
  `IntProperties` (`Dictionary<string,int>`) with typed
  `GetProperty/SetProperty` helpers — arbitrary per-quest scratch state.
- `ReadyToTurnIn()` = all steps `Finished`. `Copy()` clones a blueprint
  into a live instance (drops `Base`-flagged steps).

**`QuestStep`** (`XRL.World/QuestStep.cs`): one objective. Fields
`ID`, `Name`, `Text` (the journal body), `Value` (free string), `XP`,
`Ordinal` (UI order). State is a **`Flags` bitfield**:
`Base`(1, blueprint-only) / `Finished`(2) / `Failed`(4) /
`Collapse`(8, fold body in UI when done) / `Hidden`(16, until revealed) /
`Awarded`(32, XP already granted) / `Optional`(64).

### 1.2 Definition + loading — `Quests.xml`

`QuestLoader` (`XRL.World/QuestLoader.cs`) parses `Quests.xml` into a
blueprint registry `QuestLoader.Loader.QuestsByID`. Schema:

```xml
<quests>
  <quest ID="..." Name="..." Level="5" Achievement="..." Accomplishment="..."
         Hagiograph="..." Gospel="..." Factions="Joppa" Reputation="200"
         System="XRL.World.Quests.MyQuestSystem" Manager="...">
    <step ID="kill_x" Name="..." Value="..." XP="250" Optional="false"
          Ordinal="0" Collapse="true" Hidden="false">
      <text>Body text shown in the journal.</text>
    </step>
    <property Name="key"><value>...</value></property>      <!-- list/dict/scalar -->
    <intproperty Name="count" Value="3" />
  </quest>
</quests>
```

`System` resolves a Type in namespace `XRL.World.Quests`; `Manager` in
`XRL.World.QuestManagers`. `<property>/<intproperty>/<floatproperty>`
seed `Properties`/`IntProperties` (support `<entry Key=.. Value=..>`
dicts and `<value>` lists).

### 1.3 Lifecycle — the Game-level API (`XRL/XRLGame.cs`)

The orchestration lives on the game object (`The.Game`), backed by two
`StringMap<Quest>`: `Quests` (active) + `FinishedQuests`.

- **`StartQuest(Quest, giverName?, giverLoc?, giverZone?)`** (:538):
  dedup → `Quests.Add` → `Quest.InitializeSystem()` → set giver metadata
  → `PopupStartQuest` → `system.Start()` → `Manager?.OnQuestAdded()` →
  **`QuestStartedEvent.Send(Quest)`** → `Manager?.AfterQuestAdded()`.
  Overload `StartQuest(string ID, …)` (:576) looks the blueprint up in
  `QuestLoader…QuestsByID` **or** `DynamicQuestsGameState`, then
  `StartQuest(blueprint.Copy())`.
- **`FinishQuestStep(Quest, QuestStepList, XP=-1, CanFinishQuest=true, ZoneID)`**
  (:770): `QuestStepList` is **`~`-delimited** (finish several at once).
  Per step: `Finished=true`, `Failed=false`; if not `Awarded` → set XP,
  popup, **`The.Player.AwardXP(step.XP, … influencer …)`**, `Awarded=true`;
  then `Quest.FinishStep(step)` → `Manager?.OnStepComplete(id)` →
  **`QuestStepFinishedEvent.Send(Quest, step)`**. Finally, if
  `CanFinishQuest`, `CheckQuestFinishState`.
- **`CheckQuestFinishState(Quest)`** (:833): if every **non-`Optional`**
  step is `Finished` → `FinishQuest`.
- **`FinishQuest(Quest)`** (:651): dedup → `Finished=true` → add to
  `FinishedQuests` → record finish time → `PopupFinishQuest` →
  `Quest.Finish()` (→ `system.Finish()` + `dynamicReward.award()`) →
  `Manager?.OnQuestComplete()` → `ItemNaming.Opportunity` → `FinishPost`
  → **`JournalAPI.AddAccomplishment(...)`** (hagiograph/mural, §1.8) →
  **`PlayerReputation.Modify(faction, amount, "Quest")`** per `Factions` →
  `AchievementManager.SetAchievement` → **`QuestFinishedEvent.Send(Quest)`**.
- **`FailQuestStep(Quest, StepID, ShowMessage)`** (:870): `Finished=false`,
  `Failed=true`, `Quest.FailStep(step)`.
- Convenience: `CompleteQuest(id)` (force-finish every step),
  `ForceFinishQuest(id)` (mark finished even if never started).
- Queries: `HasQuest`, `HasFinishedQuest`, `HasFinishedQuestStep`,
  `HasUnfinishedQuest`, `FinishedQuest(id)`, `TryGetQuest`,
  `HasQuestProperty`. Persistence: `SaveQuests`/`LoadQuests` (both maps).

### 1.4 Two extensibility layers

- **`IQuestSystem`** (`XRL/IQuestSystem.cs`, abstract, `IPlayerSystem`):
  per-quest **code logic**, registered as a Game system. Virtual hooks
  `Start/Finish/FinishStep/Fail/FailStep` + `Show*Popup`. Resolves its
  live `Quest` from `The.Game.Quests[QuestID]` and its `Blueprint` from
  `QuestLoader`. Property accessors fall back live→blueprint.
  `RemoveWithQuest` (default true) flags the system for GC on finish.
- **`QuestManager`** (`XRL.World/QuestManager.cs`, `IPart`): lighter
  per-quest **hooks** — `AfterQuestAdded`, `OnQuestAdded`,
  `OnStepComplete(name)`, `OnQuestComplete`, `GetQuestInfluencer`,
  `GetQuestZoneID`. Serialized with the quest.

### 1.5 Events (the cross-system hooks)

Pooled `MinEvent`s under base `IQuestEvent` (`XRL.World/IQuestEvent.cs`,
carries `Quest`). All sent via `The.Game.HandleEvent(E, DispatchPlayer:true)`
— i.e. delivered to the player object and its parts + global systems.
- **`QuestStartedEvent`** (`Send(Quest)`).
- **`QuestStepFinishedEvent`** (`Send(Quest, Step)` — adds `Step` field).
- **`QuestFinishedEvent`** (`Send(Quest)`).
- `OnQuestAddedEvent` is the obsolete alias of `QuestStartedEvent`.

Any Part can `HandleEvent` these to react (open doors, spawn NPCs, etc.).

### 1.6 Conversation integration *(Agent-mapped)*

- **`QuestHandler`** (`XRL.World.Conversations.Parts/QuestHandler.cs`,
  `IConversationPart`): the dialogue→quest bridge. An `Action` attribute
  (`start`/`step`/`finish`/`complete`) selects the `XRLGame` call when
  the player **enters** that node. Reacts to `GetChoiceTagEvent` (paints
  `[Accept Quest]` / `[Complete Quest Step]` / `[Complete Quest]` choice
  suffixes) and `EnteredElementEvent` (fires `StartQuest` /
  `FinishQuestStep(QuestID, StepID, XP)` / `FinishQuest` / `CompleteQuest`).
  Fields `QuestID`, `StepID`, `Text`, `XP`.
- **`QuestSignpost`**: cosmetic — rewrites a `=questgivers=` token into
  directions to nearby same-faction quest-givers (`PrepareTextEvent`).
- **`DynamicQuestConversationHelper`**: programmatically builds accept/
  turn-in nodes for procedural quests; gates choices with conversation
  attributes `IfHaveQuest`/`IfNotHaveQuest`/`IfFinishedQuest`/`IfNotFinishedQuest`.

### 1.7 World-object integration *(Agent-mapped)* — `IPart`s on `GameObject`s

- **`QuestStarter`**: auto-`StartQuest(Quest)` on a world trigger
  (`Created`/`Seen`/`OnScreen`/`Taken`); optional `IfFinishedQuestStep`
  gate. Self-removes after firing.
- **`QuestStepFinisher`**: same trigger machinery, calls
  `FinishQuestStep(Quest, Step)`.
- **`FinishQuestStepWhenSlain`**: on `AfterDieEvent` for itself →
  `FinishQuestStep(Quest, Step)` (optionally `StartQuest` first if
  `RequireQuest`); self-cleans via `ZoneActivatedEvent` once done.
- **`CompleteQuestOnTaken`**: on `Taken`/`Equipped`/`Unequipped`/`Dropped`
  + `QuestStartedEvent` → `FinishQuestStep(Quest, QuestStep)`. *(Gotcha:
  finishes a STEP despite the name; fires on many inventory transitions.)*
- **`DynamicQuestTarget`**: tags an object as a quest item (un-AI-equip,
  doubles trade value, retags category); the *payload* `InteractQuestTarget`
  / delivery flow keys on.

### 1.8 Procedural / dynamic quests *(Agent-mapped — large, optional)*

`DynamicQuestFactory.fabricateQuestTemplate(blueprint, context)`
instantiates a `BaseDynamicQuestTemplate` subclass, assigns a monotonic
`id`, and calls `init(DynamicQuestContext)`. The template randomly picks
a story sub-type, pulls content from the context (giver/target zones,
delivery item, interactable, reward), caches generated objects, and
queues zone-builders that place the quest-giver + target when those
zones generate. Story types: `FindASite`, `FindASpecificItem`,
`InteractWithAnObject`, `GoNegotiateWithAnNPC` (stub). Reward elements
(`DynamicQuestRewardElement_*`): `ChoiceFromPopulation` (pick 1-of-N),
`GameObject` (specific item), `Quest` (unlock follow-up), `Reputation`,
plus Qud-main-story-specific `VillageZero*`. **This whole stack sits on
top of the static primitives + world/zone-builder + population infra —
defer until static quests work.**

### 1.9 Journal / accomplishments

On finish, `JournalAPI.AddAccomplishment(accomplishment, muralText,
gospel, …, category, weight, …)` writes a history/"mural" entry (the
hagiograph flavor). The quest *log* UI is `Qud.UI/QuestsStatusScreen.cs`
+ `QuestsLine*` (renders `StepsByID` ordered by `Ordinal`, checkmarks
from `Finished`/`Failed`, folds `Collapse` bodies, hides `Hidden`).

---

## 2. CoO's existing storylet/quest system *(verified firsthand)*

### 2.1 Data model — `Assets/Scripts/Gameplay/Storylets/StoryletData.cs`

```
StoryletData { ID, OneShot, Tracked, Triggers[], Effects[], QuestData Quest;
               IsQuest => Quest != null && Quest.Stages.Count > 0 }
QuestData      { List<QuestStageData> Stages }                  // ORDERED, LINEAR
QuestStageData { ID, Triggers[], OnEnter[] }                    // predicates + actions
```

`Triggers`/`OnEnter`/`Effects` are `List<ConversationParam>` (`Key`/`Value`
string pairs) — **reusing the conversation predicate/action vocabulary**.
A quest is just a storylet whose `Quest` has ≥1 stage. `QuestState`
(`QuestState.cs`) is the live instance: `QuestId`, `CurrentStageIndex`,
`EnteredStageAtTurn`. **Note what's absent vs Qud:** no per-stage
objective list, no `Optional`/`Hidden`/`Failed`/`Collapse` flags, no
per-objective `XP`/`Text`, no quest-level `Name`/`Level`/giver/reward
fields.

### 2.2 Runtime — `StoryletPart` (world singleton)

`StoryletPart.cs` (`: Part, ISaveSerializable, INarrativeReactor`).
Collections: `_firedStorylets` (HashSet), `_quests`
(`Dictionary<string,QuestState>`), `_completedQuests` (HashSet). API
(verified): `StartQuest(QuestState)`, `IsQuestActive`, `GetQuestState`,
`GetActiveQuests`, `IsQuestCompleted`, `MarkQuestCompleted`,
`CompleteQuest(id, actor)`, `AdvanceQuestStage(id, currentTurn, actor)`,
`RemoveActiveQuest`, `GetCompletedQuests`. **`OnTickEnd`** (`:253`) is a
deterministic two-phase (snapshot eligibility → fire) loop; Pass 1B/2B
advance active-quest stages when the current stage's `Triggers` all pass,
running the stage's `OnEnter` actions and auto-completing on the last
stage. Cascades land next tick by design.

### 2.3 Definition + loading — `StoryletRegistry`

Loads JSON from `Resources/Content/Data/Storylets/`, and **validates
trigger/effect names against the conversation registries at load** (fail-
closed on unknown names). `FindQuest(id)` resolves a storylet's `Quest`.

### 2.4 Conversation integration *(verified)*

The dialogue→game hook is **`ConversationManager.SelectChoice` →
`ConversationActions.ExecuteAll(choice.Actions, speaker, listener)`**
(`ConversationManager.cs:94`; speaker=NPC, listener=player). Already
registered:
- Actions (`ConversationActions.cs`): `StartQuest` (:447),
  `AdvanceQuestStage` (:509), `CompleteQuest` (:541), `FailQuest` (:556),
  plus reward verbs `AwardXP` (:589), `GiveDrams` (:611), `GiveInk`,
  `GiveItem`/`TakeItem`, `ChangeFactionFeeling` (:178), narrative
  `SetFact`/`AddFact`/`ClearFact`/`Reveal`.
- Predicates (`ConversationPredicates.cs`): `IfQuestActive` (:275),
  `IfQuestCompleted` (:281), `IfQuestNotStarted` (:291), `IfQuestStage`
  (:303), plus `IfHaveItem`, `IfStatAtLeast`, `IfReputationAtLeast`,
  `IfFact`, `IfSpeakerKnows`. *(Footgun: unknown predicates fail-OPEN;
  unknown actions silently no-op — the storylet loader guards this.)*

### 2.5 Supporting infra *(verified)*

- **Per-turn hook:** `TurnManager.EndTurn` fires `World.FireEvent("TickEnd")`
  (`TurnManager.cs:371`) → `NarrativeStatePart.HandleEvent` →
  `DispatchTickEnd()` → every `INarrativeReactor.OnTickEnd` →
  `StoryletPart.OnTickEnd`. New quest watchers should reuse the
  `INarrativeReactor` seam (avoids parts-order coupling).
- **Narrative flags:** `NarrativeStatePart.Current` `FactBag`
  (`GetFact/SetFact/AddFact/ClearFact`) + per-NPC `KnowledgePart`.
- **Reputation reward sink:** `PlayerReputation.Modify(faction, delta)`
  (`PlayerReputation.cs:73`); faction-faction via `FactionManager`.
- **Diag:** `quest` channel is **default-on** (`Diag.cs:120`); emits
  `quest/Started`, `quest/StageAdvanced`, `quest/Completed`,
  `quest/Failed`.
- **Save:** `ISaveSerializable` auto-dispatched in
  `SaveGraphSerializer.SavePart`; `StoryletPart` + `NarrativeStatePart`
  manage their own dict/set serialization with old-save EOF tolerance.
- **Tests:** ~113 across `Assets/Tests/EditMode/Gameplay/Storylets/`
  (predicates, actions, dispatch, rewards, save round-trip, registry,
  Marceline dialogue) + `QuestShowcaseDiagTests` + `QuestLogStateBuilderTests`.
- **Content:** `Resources/Content/Data/Storylets/IronKeyQuest.json`
  (2-stage fetch), `Resources/Content/Conversations/Marceline_Quest.json`,
  `Scenarios/Custom/QuestShowcase.cs`.

### 2.6 The one real UI gap *(verified)*

`Presentation/Rendering/QuestLogStateBuilder.cs` +`QuestLogSnapshot.cs`
exist (pure-data, tested), but there is **no `QuestLogUI` renderer
MonoBehaviour and no `q` hotkey** (zero refs in `Presentation/Input/`).
Quest state is queryable in code but **not visible to the player** —
the same "fully-simulated but invisible" class of bug as the gas clouds.

---

## 3. Parity gap matrix

Legend: ✅ match · 🟡 partial · ❌ missing · 🟦 CoO-divergent (deliberate)

| Capability | Qud | CoO today | Gap |
|---|---|---|---|
| Quest instance + state store | `Quest` + `The.Game.Quests` | `QuestState` + `StoryletPart._quests` | ✅ |
| Start / complete lifecycle | `StartQuest`/`FinishQuest` | `StartQuest`/`CompleteQuest` | ✅ |
| Dialogue start/advance/complete | `QuestHandler` part | `ConversationActions` verbs | ✅ |
| Dialogue gating on quest state | `IfHaveQuest` etc. | `IfQuest*` predicates | ✅ |
| Per-turn evaluation | n/a (event-driven) | `INarrativeReactor.OnTickEnd` | 🟦 |
| Reward: XP / currency / rep | `AwardXP`+`PlayerReputation` | `AwardXP`/`GiveDrams`/`ChangeFactionFeeling` | ✅ |
| Save/load | `SaveQuests` | `ISaveSerializable` | ✅ |
| Diag/observability | MetricsManager | `quest` diag channel | ✅ |
| **Objective model** | **parallel `QuestStep` SET + flags** | **linear `Stage` sequence** | ❌ **(core gap)** |
| Per-objective text / XP / order | `QuestStep.Text/XP/Ordinal` | — | ❌ |
| Optional / Hidden / Collapse / Failed flags | `QuestStep.Flags` | — | ❌ |
| Quest metadata (Name/Level/giver) | `Quest.*` fields | — | ❌ |
| **Quest Log UI** | `QuestsStatusScreen` | snapshot only, no renderer/hotkey | ❌ **(quick win)** |
| Quest **GameEvents** for reactions | `QuestStarted/StepFinished/Finished` | diag only | ❌ |
| World-object triggers (kill/take/interact/zone) | `QuestStarter`/`…StepFinisher`/`FinishQuestStepWhenSlain`/`CompleteQuestOnTaken` | tick-polled stage predicates only | 🟡 |
| Failed-quest tracking | `Failed` flag + `FailQuestStep` | `FailQuest` action, no fail model | 🟡 |
| Code-side per-quest logic | `IQuestSystem` / `QuestManager` | pure-data storylets | 🟦 (likely keep data-driven) |
| Accomplishments / journal history | `JournalAPI.AddAccomplishment` | narrative `_eventLog` | 🟡 |
| Procedural / dynamic quests | full `DynamicQuest*` stack | — | ❌ (defer; large) |

---

## 4. Key architectural decisions to make

1. **Objective model (the big one).** Adopt Qud's *parallel-objective*
   expressiveness while keeping CoO's clean data-driven stages. Proposed:
   give each `QuestStageData` a `List<QuestObjectiveData>` where each
   objective has `ID`, `Text`, `Triggers[]`, `Optional`, `Hidden`; the
   stage advances when **all non-`Optional` objectives** are finished
   (mirrors `CheckQuestFinishState`). This is a *superset* — a stage with
   one objective == today's behavior, so existing content keeps working.
   (CoO keeps *stages* as a sequencing layer Qud lacks; Qud parity is at
   the *objective* level, not by replacing stages with a flat set.)

2. **Keep data-driven; do NOT port `IQuestSystem`.** CoO's
   storylet+conversation model already covers scripted behavior via
   actions/predicates + `FactBag`. `IQuestSystem` (arbitrary per-quest
   C#) is power CoO hasn't needed; porting it would fork the model.
   Classify as a deliberate 🟦 divergence. Revisit only if a quest needs
   logic no action/predicate can express.

3. **Tick-polled predicates vs event-driven Parts.** Qud advances quests
   from world *events* (`AfterDieEvent` etc.); CoO polls stage
   `Triggers` each `TickEnd`. Polling is simpler and already works, but
   needs predicates for the trigger conditions (e.g. `IfActorDead`,
   `IfInteracted`). World-object Parts (Q5) are the event-driven
   complement for cases polling can't see.

4. **Quest `GameEvent`s.** Add `QuestStarted`/`ObjectiveFinished`/
   `QuestCompleted`/`QuestFailed` CoO `GameEvent`s (string-keyed, fired
   on the player/world) so other systems can react — parity with Qud's
   `IQuestEvent` family. Diag records stay; events are the *reaction*
   hook, diag is the *observability* hook.

---

## 5. Recommended phased plan (Qud-parity extension)

Smallest-blast-radius first; each phase ships one testable behavior and
follows the CLAUDE.md TDD + adversarial + cold-eye cadence. Content-
readiness: 🟢 ready · 🟡 needs content · 🔴 needs design.

- **Q1 — Quest Log UI** 🟢 *(start here)*. Renderer MonoBehaviour over the
  existing `QuestLogSnapshot` + `q` hotkey in `InputHandler`/`GameBootstrap`.
  Closes the "quests are invisible" gap. No model change; foundation +
  tests already exist. *Highest value/risk ratio.*
- **Q2 — Quest + objective display metadata** 🟡. Add `Name`/`Description`/
  `QuestGiver` to `QuestData`; add `Text`/`Optional`/`Hidden` to the
  objective (with Q3) so the log reads well. Additive, low risk.
- **Q3 — Parallel objectives within a stage** 🔴→🟢. The core model
  parity (decision §4.1): `QuestObjectiveData` list per stage, advance
  when all non-Optional finished. Per-objective `Triggers` reuse the
  predicate vocabulary. Adversarial sweep warranted (stacking/atomicity/
  save-reach). *Heart of "Qud parity."*
- **Q4 — Quest `GameEvent`s** 🟢. Fire `QuestStarted`/`ObjectiveFinished`/
  `QuestCompleted`/`QuestFailed`; pin with tests + diag. Enables Q5.
- **Q5 — World-object quest Parts** 🟡. Port `QuestStarter`,
  `QuestObjectiveFinisher`, `FinishObjectiveWhenSlain`,
  `CompleteObjectiveOnTaken` as CoO `Part`s (advance quests from
  kill/take/interact/enter-zone, not just dialogue). Depends on Q3+Q4.
- **Q6 — Failed-quest tracking + fail conditions** 🟡. `Failed` state on
  objectives/quests; fail triggers; `_failedQuests`. Parity with Qud's
  `Failed` flag + `FailQuestStep`.
- **Q7 — Accomplishments / journal history** 🟡 *(optional flavor)*.
  Hagiograph-style entry on completion atop `NarrativeStatePart._eventLog`.
- **Q8 — Procedural / dynamic quests** 🔴 *(optional, large; defer)*. The
  full `DynamicQuest*` stack — only if procedural quests are wanted.
  Port reward primitives (`ChoiceFromPopulation`/`GameObject`/`Reputation`/
  `Quest`) but treat Qud's `VillageZero*` as CoO-original substitutions.

---

## 6. Verification-sweep notes (false premises caught)

- **`Docs/QUEST-SYSTEM.md` intro is stale.** It frames the system as a
  "half-built scaffold / M4 placeholder at `StoryletPart.cs:93`"; the code
  there is the *fully-implemented* dispatch loop. The doc body (QS.2–QS.8
  specs) matches what shipped. Trust the code; fix the doc intro when Q1
  lands.
- **Qud `CompleteQuestOnTaken` finishes a STEP, not a quest**, and fires
  on take/equip/unequip/drop — port the behavior, not the name.
- **Qud `FindASiteDynamicQuestTemplate.init` enumerates the wrong enum**
  (`QuestStoryType_FindASpecificItem` while assigning a `…_FindASite`).
  Harmless (both 2-valued) but don't copy blindly.
- **Two predecessor plans exist** (`Docs/Plans/STORYLET_QUEST_LAYER_…`,
  `…NARRATIVE_STATE_LAYER_…`) — superseded in part by `QUEST-SYSTEM.md`'s
  QS.* milestones. This doc supersedes the parity-analysis portion.

## 7. Open questions for the user

1. **Objective model:** confirm the §4.1 approach (objectives *inside*
   stages) vs a flat Qud-style step set replacing stages.
2. **Scope:** is procedural/dynamic quest generation (Q8) in scope, or
   author-only static quests for now?
3. **First phase:** Q1 (Quest Log UI) recommended — agree, or prioritize
   Q3 (parallel objectives) first?
