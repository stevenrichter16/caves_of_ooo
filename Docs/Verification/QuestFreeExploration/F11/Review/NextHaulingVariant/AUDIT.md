# Second hauling variant: obstruction, not general cover

Read-only source audit, 29 September 2026. No Unity, tests, source edits or Git operations. Current F9 supplies a beam/barrel shoulder shortcut; F11 acceptance/publication is parent-owned. Historical test counts do not validate this proposal. Ordinary twenty-chunk discovery and player preference remain unmeasured.

## Recommendation

The strongest distinct hauling decision is **pull a load into a gap behind you to buy separation from an ordinary melee pursuer**, versus keep running through the open gap or fight. This reverses F9's existing “move the obstruction aside to shorten travel.” It uses the same actual beam/barrel and existing Haul/LetGo actions, with no new reward or actor.

**Do not register it yet.** First resolve one bounded feasibility question: does a real pursuer fairly route around the parked load, and does the separation gained exceed the slower paid pull? Source inspection exposes a planner/movement mismatch below. An enemy repeatedly failing to move is not successful tactical design. If fair pursuit requires a broader AI repair, defer this variant and take **F4 alternate access to the existing finite draw point** instead. Do not change all prop sight tags to rescue a “cover” premise.

## Actual mechanical limits

| Mechanism | Current source fact | Consequence |
|---|---|---|
| Movement | All three Spread haulables have `Physics.Solid`; `BlocksMovement` and actual `PhysicsPart.BeforeMove` honor it. | A parked load is a real obstruction. |
| Sight | They lack the `Solid` tag. AI LOS checks `Cell.IsSolid`/obscuring tile state; player FOV uses `IsWall`/obscuring state. | Beam, barrel and millstone do not hide you. Hedge alone also does not hide you. |
| Thrown first impact | `LineTargeting` treats a nonterrain Physics owner as a target, before continuing the line. | An intervening load can intercept that path. This does not establish damage, AOE protection or every projectile family. |
| Elemental lines | `SkillLine` stops on `IsSolid`; elemental target admission needs Destructible, Material or Thermal. Barrel has them; beam/millstone do not. | Beam/millstone do not provide general elemental cover. Barrel can be a single-target hit but does not stop all penetrating/cell effects. |
| Chase planning | `KillGoal` uses `TryApproachWithPathfinding(...ignoreCreatures:true)`. For a single-cell mover, `FindPath` then relies on tag-only `IsPassable` and skips its Physics check. Actual movement still refuses the load. | Source-confirmed planner/mover disagreement; bypass behavior, fallback oscillation and tactical value are **not executed/proven** by this audit. |

Exact anchors: `Cell.cs:99,128,146,236`; `PhysicsPart.cs:111`; `AIHelpers.cs:59,384–426`; `FindPath.cs:141–176`; `KillGoal.cs:55–75`; `FieldOfView.cs:95`; `LineTargeting.cs:124–187`; `SkillLine.cs:55–70,115–134`; `AbilityTargeting.cs:38–51`. Full paths/hashes are in `source-inputs.json`.

The ordinary Marlback Scrabbler is a melee source with its actual Dagger/Hatchet/Cudgel roll, not an archer to manufacture a ranged-cover encounter around. A barrel has structural HP8 and wood/thermal parts; FallenBeam has no Destructible, Material or Thermal. Therefore “break/burn either load” is also false. No barrel contents, salvage payout or millstone crafting use is implemented by these definitions (`Objects.json:28681,28808,29048`).

## Supply, cost and persistence

`HaulablePropBuilder.cs:30,45–60,72–108` rolls 350/1000, then one equal Spread pool choice, tries forty cells and places at most one. It deliberately requires open cardinal neighbors. This is not a 35% useful-encounter rate. The current native F9 corpus had fifteen assignments, five compatible beam/barrel sources, three millstones and seven absent rolls; all five compatible packets committed. The older accidental-gap census found only one marginal layout among sixty chunks/thirteen compatible loads. That private pre-F9 result explains why original terrain was authorized; it does not invalidate the shipped terrain variant.

`DragRules.cs:82,122` uses actual lift refusal, Strength×8 and the authored minimum. Ordinary Strength18 can drag weight60 beam/75 barrel but not weight150 millstone. Handling adds no tool requirement or lift/throw ability. Haul/LetGo are free; each pull gives the world paid time. `DragSystem.cs:250,373` applies speed penalties24/30 and moves the load into the vacated grip cell. Existing paired measurements were27/29 ticks for two pulls versus20 unheld—not free repositioning. Contextual Haul/LetGo cues and stable beam identity already exist (`HandlingPart.cs:68–81`; `SpawnRing3DRecipes.cs:464–467`). Reusing them needs no marker architecture or new model by default.

## Proposed bounded gate, before content/version work

1. **Real movement and fair response first.** A small paired fixture uses the actual single-cell pursuer/`KillGoal`, beam and barrel, real grab/pull/release and scheduler. Open-gap control must catch up; parked-load case must take a valid eight-direction bypass rather than repeatedly fail. Count pursuit and player actions. Check diagonal shortcuts and moving-player behavior. Source review alone cannot certify this.
2. **One inverse layout, existing budget.** Reuse the current one-load/twelve-original-Hedge allowance. A candidate pull from an adjacent holding pocket into the throat leaves the player on the retreat side; a substantial always-open bypass preserves all arrivals/exits and both actors' reachability. No new wall, load, hostile, stock or forced roll. Compare against the same untouched source packet. Require strictly positive net separation after hauling cost and a useful opportunity to leave; choose the threshold before measured execution.
3. **Admit actual sources honestly.** Freeze the same three-seed metadata corpus before generation, report original load and existing hostile availability separately, and retain refusals. Do not claim a pursuit encounter where no real pursuer exists. Measure the additional cold placement cost; no per-turn geometry search. Preserve literal existing versions and visited graphs.
4. **Player-facing and saved proof.** One disclosed local native witness keeps ordinary NPC turns, HP, strength, equipment and RNG. Show available Haul, slower pull, free release, real detour and retained parked owner/route after inactive save/load. No forced combat winner. Art changes are justified only if the real gap/load relation is unreadable; preserve the existing small, dark beam's known distance limitation.

## Alternate complete slice if the gate fails

F4 currently admits a finite `SpreadDrawPoint` with a connected dry approach, not a proved choice between two approaches (`SpreadExplorationBuilder.cs:252–312`; family `WateringMargin` in `SpreadExplorationPlan.cs:105`). A short exposed versus longer protected/dry route to **that same finite volume** would add a decision using accepted full/empty basin art. Require real route-cost/exposure differences; no guaranteed animal drinking, invented water or extra guardian. This is preferable to a second cosmetic hauling arrangement. Broader biome reuse remains behind regional source/art audits and the still-open ordinary experience gate.
