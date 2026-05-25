# Questing in normal play — "Root Beer Guy's Case"

> The first quest a player encounters in NORMAL PLAY (not a launched dev
> scenario). A quest-giver + his lost item are placed into the **starting
> village** by the generation pipeline, so a fresh game drops the player
> next to a complete, robust fetch quest. **Status: done, live-validated
> 13/13 in a fresh session.**

## What the player does
Start a new game → you spawn in the starting village (`Overworld.10.10.0`).
**Root Beer Guy** (the `r`) is placed among the villagers; his **detective
notebook** (`=`) is dropped somewhere in the village.
1. Talk to Root Beer Guy (`c` + direction) → "[Accept]" → quest starts.
2. Explore the buildings, find the notebook, pick it up (`G`).
3. Return to Root Beer Guy → "[Return it]" → quest completes, **+150 XP,
   +60 drams**, accomplishment logged.

## How it's wired (smallest-blast-radius path)
`VillagePopulationBuilder` already had a `StartingVillageZoneId` const and
gated starting-village-only content (the grimoire chest, the material
sandbox). We followed that precedent:
- `VillagePopulationBuilder.PlaceStartingVillageQuestGiver` (gated on
  `zone.ZoneID == StartingVillageZoneId`) places the quest-giver via the
  existing `PlaceNPCInInterior` + `SetConversation` helpers, and drops the
  notebook (built inline, `BlueprintName = "DetectiveNotebook"`, carrying a
  `CompleteObjectiveOnTaken` Part) in an open cell.
- The quest + dialogue are real auto-loaded JSON
  (`RootBeerGuyCase.json` / `RootBeerGuy_Quest.json`).

Chosen over the alternatives: a dev scenario (still launched from the editor
menu — defeats the goal) and a global hook in every village (repetitive +
one quest can't be owned by N givers). Starting-village placement = the
player meets it immediately, once, in real play.

## Robust-world-quest design (why this differs from the scenario quests)

The scenario quests (Cinnamon Bun) use **Part-only** objectives with a
never-passing sentinel guard — fine when *timing is controlled* (the
scenario drives the player). In the **wild**, the player acts in any order,
so world quests must be **order-independent**:

| | Scenario quest | World quest |
|---|---|---|
| Fetch objective | sentinel-guard, Part-only | **`IfHaveItem` trigger** (polled, robust) + `CompleteObjectiveOnTaken` fast-path |
| Why | controlled timing | player might grab the item *before* accepting → a Part-only objective would never re-fire → soft-lock. `IfHaveItem` completes whenever the player holds it, regardless of order. |

So `find_notebook` carries `IfHaveItem:DetectiveNotebook` (the robust
mechanism) AND the notebook carries `CompleteObjectiveOnTaken` (the instant
fast-path). Picking it up after accepting → instant via the Part; grabbing
it first → the next tick's `IfHaveItem` poll catches it. No soft-lock. This
is the Angle-B "fast-path + polled fallback" relationship, applied.

The **report stage** uses a sentinel guard (`IfFact:rbg_report_guard:>=:1`,
never set) so it doesn't auto-complete before the player returns — the
dialogue `CompleteQuest` is the sole completion (controlled timing is fine
here: the NPC is always present, no soft-lock).

## Known limitation — world KILL objectives
There is **no robust polled kill-detection predicate** (`IfActorDead` is
referenced in a Q3.1 parse-test but is NOT registered). `FinishObjectiveWhenSlain`
no-ops if the quest isn't active, so a *world* kill objective could soft-lock
(player kills the mob before accepting). That's why this first world quest is
fetch-only. **Future:** add an `IfActorDead` predicate or a `SetFactWhenSlain`
Part (mirror of `FinishObjectiveWhenSlain` that sets a fact) + an `IfFact`
trigger — then world kill/clear objectives become order-independent too.

## Validation
- **EditMode content-integrity** (`QuestRootBeerGuyContentTests`, 3 tests):
  the fetch objective has the `IfHaveItem:DetectiveNotebook` trigger (the
  robustness contract); `IfFact` args well-formed; dialogue actions/predicates
  registered + quest/stage references resolve. The string consts mirror
  `VillagePopulationBuilder.PlaceStartingVillageQuestGiver` (the builder↔content seam).
- **Broad regression:** 516/516 across RootBeerGuy + Quest + Storylet +
  Village + Generation + Population + World — the gen change broke nothing.
- **Live (rule 7), fresh session:** entered Play → the starting village
  generated → confirmed the quest-giver + notebook are **placed in the
  zone**, the quest + conversation **auto-load**, then drove the loop
  (dialogue StartQuest → real PickupCommand → advance → dialogue CompleteQuest).
  **RESULT: 13/13, 0 fails (runId b6203a70), console clean.**

  **Honesty bound / dev-env note:** a first Play run in the *same* editor
  session showed `content_convo_loaded=false` — a **stale static cache**
  (with domain-reload-off, `ConversationLoader` had cached its dict in a
  prior Play session before this file existed; `_loaded=true` blocked a
  reload). Proven benign: `ConversationLoader.Reset()` → it loads, and a
  fresh session (how a player launches) loads it on first access. Not a
  content bug.
