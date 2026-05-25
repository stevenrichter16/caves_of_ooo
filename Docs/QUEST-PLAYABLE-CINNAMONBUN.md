# Playable Quest — "Cinnamon Bun's Favor"

> The first quest that wires the **Q5 world-object Parts** into a complete,
> player-exercisable NPC loop. The pre-existing IronKey showcase uses the
> older `IfHaveItem` trigger + `AdvanceQuestStage` turn-in; this one uses
> `CompleteObjectiveOnTaken` (Q5.2) + `FinishObjectiveWhenSlain` (Q5.1) for
> objectives and a dialogue `CompleteQuest` turn-in. **Status: done, live
> validated 15/0.**

## The loop
1. Talk to **Cinnamon Bun** (`Villager` blueprint + our `Conversation` part)
   → "[Accept]" runs the dialogue `StartQuest` action → quest begins
   (stage `errands`).
2. Take the **scorched keepsake** → its `CompleteObjectiveOnTaken` Part
   finishes `recover_keepsake` the instant you pick it up (Q5.2).
3. Slay the **soot gremlin** → its `FinishObjectiveWhenSlain` Part finishes
   `drive_off_gremlin` (Q5.1). Both required done → stage advances to
   `report`.
4. Return to Cinnamon Bun → "[Report]" (gated `IfQuestStage:…:report`) →
   `CompleteQuest` + 250 XP + 100 drams; the Q7 accomplishment is logged.

## Files
| File | Role |
|---|---|
| `Resources/Content/Data/Storylets/CinnamonBunFavor.json` | quest (2 stages, sentinel-guarded) |
| `Resources/Content/Conversations/CinnamonBun_Quest.json` | dialogue tree (offer / status / report / done) |
| `Scripts/Scenarios/Custom/QuestCinnamonBunPlayable.cs` | places the NPC + keepsake + gremlin, wires the Parts (launchable) |
| `Tests/.../QuestCinnamonBunContentTests.cs` | content-integrity (cross-reference pins) |

Everything is real auto-loaded content; the scenario only PLACES the actors
(matching how all CoO quests are currently played — via a launched scenario).
The objective/quest IDs are exposed as `QuestCinnamonBunPlayable` consts and
pinned against the JSON by the content test, so a typo in any file fails a
test rather than silently breaking the loop.

## The sentinel-guard pattern (reusable content insight)

CoO's tick dispatch **auto-finishes empty-trigger objectives** and
**auto-advances empty-trigger non-objective stages** (pinned by
`OnTickEnd_EmptyTriggerObjective_FinishesAndAdvances`). So a naïve
Part-driven objective (no trigger) would be auto-finished on the first tick
*before the player acts*, and a naïve "report" stage would auto-complete
*before the player returns* (the IronKey showcase has this latent quirk).

To make a stage/objective driven SOLELY by an external mechanism (a Q5
world-object Part, or a dialogue action), give it a **never-passing guard
trigger** — a sentinel fact that nothing ever sets:

```json
"Triggers": [ { "Key": "IfFact", "Value": "cbq_external:>=:1" } ]
```

`cbq_external` is never `SetFact`-ed, so the tick never auto-fires it. The
Q5 Part finishes the objective directly via `FinishObjective` (which bypasses
triggers), and the dialogue completes the report stage via `CompleteQuest`.
This is the key to a dialogue-gated turn-in that actually waits for the player.

> **IfFact format gotcha:** the arg is `key:OP:threshold` (3 colon-parts),
> e.g. `cbq_external:>=:1`. A 2-part `key:value` silently always-evaluates
> false (`ConversationPredicates.cs:238`). The content test pins every
> `IfFact` arg to 3 parts.

## Validation
- **Content-integrity (EditMode, 4 tests):** quest loads with the expected
  stages/objectives matching the scenario consts; every `IfFact` well-formed;
  every dialogue action/predicate registered; every quest/stage reference
  resolves.
- **Live drive (rule 7):** `ScenarioContext.FromLiveGame()` + the scenario,
  then drove the whole loop — dialogue `StartQuest`, real `PickupCommand`
  pickup, `Died` slay, dialogue `CompleteQuest` — auditing quest state +
  predicate gating + rewards + accomplishment at each step.
  **RESULT: 15 cells, 0 fails (runId 9e9ee512), console clean.** Proves the
  Q5 Parts + dialogue vocabulary + sentinel guards + Q7 accomplishment all
  compose in the bootstrapped runtime with auto-loaded content.

  **Honesty bound:** the live drive invokes the conversation ACTIONS/PREDICATES
  directly (the same calls the dialogue UI makes) rather than simulating key
  presses through `ConversationManager`/`DialogueUI`; the dialogue rendering
  itself is exercised by the existing DialogueUI/ConversationManager tests.

## Showcased
Dialogue `StartQuest` / `CompleteQuest` / `AwardXP` / `GiveDrams`;
predicates `IfQuestNotStarted` / `IfQuestStage` (string stage IDs) /
`IfQuestCompleted`; Q5.1 `FinishObjectiveWhenSlain`; Q5.2
`CompleteObjectiveOnTaken`; Q7 `Accomplishment`; the sentinel-guard pattern.
(`QuestStarter` (Q5.3) is showcased separately in `QuestWorldPartsBench`.)
