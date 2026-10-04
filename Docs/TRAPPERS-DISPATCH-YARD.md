# Trappers' dispatch yard

Status: implemented and verified within the recorded bounds, 2026-10-04. Native affected tests and replacement-save fixtures pass; ordinary Play evidence is partial because real combat changed player anatomy/speed. Root observed ten native RED cases (eight expected failures, two neighbor controls passing) before authorizing production. Root alone runs Unity. This is original Caves of Ooo content using shipped mechanics, not a Qud parity claim.

## Player outcome and bounded scope

Expand the existing southern TrappersStore in `Overworld.11.11.0`, one world-map step south of the starting glade. A small abandoned store offers a shallow frost-lichen harvest and a deeper, finite cache. The direct entrance has visible spike teeth: spend one carried salvaged timber to jam them permanently, or accept the trap's ordinary consequences. The service entrance is blocked by a real fallen beam: a capable character can haul it into the clear outside apron, release it, and walk around it. That costs actions and temporarily lowers speed; opening either entrance can also release the existing occupants. Taking only the shallow plant, withdrawing, returning with timber or strength, and recovering the full cache are useful physical outcomes.

Keep the exact original rolled Crate/Sack and every actual item it contains. No new treasure table, invented supplies, free local repair timber, restocking, quest flag, morality flag, interaction framework, or damage rule. Original pair replacement remains one soursprayer plus one scrabbler for exactly two ordinary source actors; one source actor remains untouched. The yard never blocks ordinary world-border travel or forces trap damage to cross the zone. These are two authored entrances, not the only imaginable routes: the existing StoneWall owners remain normally destructible, so a player can also spend ordinary attacks/time breaching the structure. No wall is made artificially invulnerable.

## Exact geometry

Coordinates below are relative to the moved original cache at `(0,0)` before the existing quarter-turn rotation. The structure is seven by five cells; additional clear ground is validated, never cleared.

```text
          x -5 -4 -3 -2 -1  0  1  2  3  4
  y -2           #  #  #  #  #  #  #
    -1     .  .  #  .  .  .  R  .  #
     0     p  g  B  .  .  C  .  .  T  a
     1     .  .  #  .  M  .  .  .  #
     2        F  #  #  #  #  #  #  #
```

`#` is exact StoneWall, `B` exact FallenBeam at `(-3,0)`, `T` exact SpikeTrap at `(3,0)`, `C` the unchanged original cache, `F` the existing finite FrostLichenPatch at `(-4,2)`. `R=(1,-1)` and `M=(-1,1)` are the existing mixed pair when source authority permits it. Dots are validated bare ground, not new floor owners. `g=(-4,0)` is the grab square and `p=(-5,0)` the first pull destination. Continue one perpendicular step to `(-5,-1)`, moving the beam to `p`, then release. The six apron cells `x=-5..-4, y=-1..1` support a cardinal walk around the released beam through `(-4,-1)`, `g`, and the cleared entrance. These are two ordinary paid moves while hauling, not a custom interaction. `a=(4,0)` is the direct approach. All interior cells must be clear before placement, not just the cells receiving owners.

The stock sits far enough behind the perimeter that the existing Chebyshev-one interaction reach cannot loot it from outside. The closed-state reach proof treats the armed trap as unsafe and the beam/walls as blocked; no reachable exterior cell may touch the cache. Each alternative gets its own proof: treating the trap as safely crossed exposes a cache approach while leaving the beam, and moving the beam to `p` exposes a cardinal approach while leaving the trap unsafe. Test diagonal as well as orthogonal movement. The shallow patch retains at least two exterior approaches. Preserve every previously connected critical route, stairs approach, reserved cell and all four actual world-border exits; keep the existing conservative hostile-radius bypass check.

The previous store contract required a free six-step bypass and two exterior cache approaches. Replace that contract only for TrappersStore. The alembic and forge keep their existing access contracts. No owner is deleted to make room; a seed with no valid candidate keeps its original graph.

## Existing mechanics and corrections

| Premise | Verified source / correction |
| --- | --- |
| This would add the first trap-jamming journey | `Docs/TIMBER-TRAP-DESIGN.md` and `SpreadDiscoveryNativePlayer.TrapJamming.cs` already prove a complete ordinary-input cellar-timber → southern-store jam → safe crossing → F5/F6 journey. This milestone adds consequential geometry and hauling, not another sign. |
| A beam needs a new Part or blueprint | `Objects.json` already defines FallenBeam: weight 60, solid, noncarryable, Handling, no Destructible or Harvestable. Use that exact owner. Do not advertise smashing or timber extraction. |
| Every build can haul it | `DragRules.MaxDragWeight` is Strength × 8; weight 60 needs Strength 8. Strength 7 refuses. Low-strength players may jam, take shallow forage, or leave. |
| Hauling spends extra action energy | `DragSystem.PenaltyPerTenWeight=4` gives this beam a 24-point Speed penalty, clamped to leave Speed 20. Each action retains normal action cost. Moving takes the beam into the vacated player cell; release restores the recorded penalty. |
| Beam art is glade-only | `SpawnRing3DRecipes` has a glade override but also a global exact FallenBeam catalog binding with stable saved-ID variation; `Assets/Art3D/SpawnRing/Definitions/catalog.json` includes its original model. No renderer/art edit is planned. |
| The deeper cache guarantees valuable equipment | CrateT1 and SackT1 contain probabilistic original money, reagents, food and equipment. ContainerPlacementService already adds its existing pocket-change fallback when every entry misses. Preserve actual contents; do not claim guaranteed medicine or equipment, add a loot gate, or mint items. Actual native stock is recorded during acceptance. The initial empty-cache premise was corrected during the sweep. |
| One straight pull leaves an ordinary cardinal entry | The first pull parks the beam at `g`, leaving only a legal diagonal squeeze into the opening. Root approved a second perpendicular haul step within the same apron so the beam rests at `p` and the entry is cardinal-clear. No movement/pathfinding change. |
| The old trap clue remains correct | Its current “open gap leads around” text becomes false. Describe the visible direct teeth, the beam-blocked service opening, and timber jamming. Beam text explains the existing Haul action and open pull space, without a magical repair objective. |
| Walls make a guaranteed safe combat exercise | Walls provide actual enclosure/cover, but opening a lane permits ordinary enemy movement. No new patrol, defense AI or invulnerability is promised. An NPC may spring the one-shot trap; that real aftermath is retained. |

Read for this plan: `CLAUDE.md`; `Docs/GALLERY-AND-TACTICAL-COMBAT.md`; `Docs/TIMBER-TRAP-DESIGN.md`; `Docs/SPREAD-CONTENT-EXPANSION.md`; `SpreadExplorationPlan.cs`; `SpreadExplorationBuilder.cs`; `SpreadExplorationWorksites.cs`; `SpreadWildernessSituationBuilder.cs`; `ContainerBuilder.cs`; `ContainerPart.cs`; `InputHandler.ActionRequiresReach`; `DragRules.cs`; `DragSystem.cs`; `Objects.json`; `LootTables.json`; `SpawnRing3DRecipes.cs` and its catalog; existing worksite/drag/trap tests and native trap journey. Recheck exact contracts before production and append any correction here.

## Implementation files and acceptance

Production is confined to `Assets/Scripts/Gameplay/World/Generation/Builders/SpreadExplorationWorksites.cs`: store-only stamp, validated interior/apron, state-specific access proofs, accurate Examine text, and existing atomic receipt/final-state/rollback pattern. Avoid plan-version changes unless the compatibility sweep proves they are necessary: saved accepted graphs stay literal and are never rebuilt. No JSON, model, drag, trap, combat or renderer changes are presently needed.

Add `Assets/Tests/EditMode/Gameplay/World/TrappersDispatchYardTests.cs` and metadata; retain the existing worksite suite as a neighbor regression. Verify source cache/item identity, no duplicate stock/timber, closed deep access versus reachable shallow harvest, jam-open and beam-moved alternatives, real Strength-8 haul and Strength-7 refusal, penalty/release, failed placement atomicity, and seed-64/1729 actual southern generation. Adversarial follow-up covers blocked apron/interior, callback mutation, malformed beam, forged receipts, and diagonal interaction shortcuts. Root records native RED before production and runs affected GREEN.

Extend the existing `Assets/Scripts/Scenarios/Custom/SpreadDiscoveryNativePlayer.TrapJamming.cs` only after the production slice is green. Preserve its earned cellar timber, unrepaired competing garden wicket and actual southern travel. Capture actual stock and lichen before acting. Use one canonical visit: harvest the outside lichen, grab/pull/release the beam with both earned timber preserved and the original trap still armed, enter through the beam opening, and recover the actual cache once. Then deliberately pay one timber to open the opposite direct entrance and cross it safely. Leave and revisit, then F5/F6; prove cache/forage depletion, beam position, trap state, IDs and carried stock without replenishment. Independent fresh-fixture unit tests prove that either opening alone gives access. Native acceptance must not claim jamming was the sole remaining route after the beam had moved. Root approved this simpler continuous journey instead of saved alternative branches because it preserves literal earned state and avoids replaying the long approach. No hand-authored substitute zone or injected repair material.

Existing `SpreadExplorationPlan.Retain` retains installed accepted graphs; final acceptance must verify real leave/revisit and saved aftermath, not infer persistence from a cached getter alone. Inspect native screenshots for existing models and readable openings. Script checks establish physical state and ownership, not enjoyment or all-build balance.

## Three working prompts and execution record

### 1. Brainstorm prompt

Read current generated content and working mechanics. Rank the next substantial milestone beyond the inspection gallery by ordinary reachability, meaningful costs, partial outcomes and persistence. Compare a mechanical checkpoint, defended medical cache and unusable well. Reject framework work and duplicate demonstrations. Identify exact existing owners, generation receipts and native acceptance routes.

Executed: ranked the expanded southern TrappersStore first and a Patchbearer medicine encounter second. The medicine option needs a new ordinary-depth source/persistence proof; the store already owns an earned-material native route and retained generation graph. Selected the existing store for a bounded dispatch-yard expansion.

### 2. Design prompt

Turn the selected store into a physical choice between finite timber and hauling effort. Preserve actual source stock and the original hostile budget. Specify a small footprint, outside pull space, shallow forage, deep cache access, observable clues, failed placement, saved aftermath and original-model reuse. Verify source APIs and record false premises before code. Obtain root agreement on geometry and scope.

Executed: this document records the seven-by-five ring, two different entrances, unchanged cache, existing lichen, Strength/Speed tradeoff, global beam model and store-only replacement of the old free-bypass contract. Root approved the layout and scoped files on 2026-10-03. The pre-production source sweep completed, including the ordinary pocket-change loot fallback correction. Root observed eight expected failures and two passing neighbor controls in `Docs/Verification/TrappersDispatchYard/red.xml`. The two-step haul clarification was approved before production.

### 3. Implementation prompt

Write behavioral failing tests first and send their class names to the root Unity runner. After root observes RED, implement only the approved store content and access proof. Preserve receipt authority, callbacks, graph identity and rollback. Run affected GREEN, then adversarial checks; exercise the canonical haul, cache recovery, later direct jam and literal persistent aftermath through real input. Inspect screenshots, review implementation against this contract, record corrections and evidence limits, and publish only through the root's existing authorized workflow.

Execution started: production is on disk after observed RED; the ten original behavior/neighbor cases now include the approved two-step haul. Fifteen additional adversarial cases cover malformed/revoked factories and independently blocked working spaces. These adversarial cases were added after production and have no separate prior-RED claim. Root native affected GREEN passed; gameplay acceptance remains pending. This prompt is deliberately not marked complete; update this record as each gate is actually observed.

## Readiness, performance and review

🟢 Existing substrate: exact trap jamming, finite source cache, finite lichen, handling/dragging, receipt-backed optional generation, persistent accepted graph and original model bindings.

🟡 Pending verification: final canonical ordinary-input journey. Both selected generated seeds and the affected test sweep passed; the first native journey recorded useful original stock but failed final acceptance. The expanded stamp and access proofs are implemented. No claim of all-seed placement.

⚪ Deliberate exclusions: no new loot, new enemy behavior, beam destruction, save migration, farming, restocking or guaranteed combat-free haul. Content remains optional.

Placement uses the existing bounded 256-trial cold-generation search. No per-frame/per-turn listener, cache or new renderer is added. Floods and scratch allocations are generation-only; preserve the bounded search and avoid scanning alternatives after acceptance. Actual runtime performance beyond unchanged existing systems is not yet measured.

Implementation log: documentation, 25 behavioral/neighbor/adversarial cases and the scoped worksite production change are on disk. Source review confirms no new loot/model/plan version, no source graph clearing, unchanged critical route checks, state-specific store access, and final callback revalidation. This agent has performed no Unity operations. Native acceptance is owned by the root and the native-scenario agent; final evidence and cold-eye review follow.


### Verification corrections before GREEN

A new adversarial test initially referenced runtime-internal `Entity.SpatialZone` from the test assembly. It now verifies the same public zone-cell ownership instead. The root's concurrent 526-case run used the previous compiled assembly (eight old RED failures) and is **not** current production GREEN evidence. The existing trap-content readout test was intentionally migrated from its old free-gap text to the new beam-blocked service-opening clue; its permanent-jam and conditional-trigger checks remain.

The independent cold review found one further factory contract gap: a new beam with `Handling.MinLiftStrength=9` would still be accepted although the design promises Strength 8 suffices. Added this adversarial case before changing production; root observed its RED before the guard was tightened.

The reviewer also found that later factories can retain and mutate an earlier staged beam or wall before its accepted snapshot. Added three further RED cases (beam weight, missing Handling, missing wall collision) before tightening final semantic validation. The shared current-owner guard now rechecks structural/mechanism semantics immediately, after all factories and in the final proof, rather than accepting a snapshot of already-bad staged state.

Root observed the review sweep: **541 cases, 537 passed, four expected failures** (MinLiftStrength, late beam weight, removed Handling, removed wall collision), recorded in `Docs/Verification/TrappersDispatchYard/review-red.xml`. Both ordinary southern seeds, original geometry/hauling cases, migrated clue checks and unaffected neighbors passed. The minimum shared store-owner semantic guard is now implemented: promised beam strength/weight/carry behavior, structural collision and a supported armed traversable one-shot trap are rechecked at creation, after staging and at final acceptance. The affected GREEN subsequently passed as recorded below.

## Native affected regression

Unity EditMode job `b7e2705a29bf46b1ad911d43b2c814f6`: **541/541 passed**, zero failures/skips, 66.98 seconds. [Complete XML](Verification/TrappersDispatchYard/affected-green.xml). This includes all 25 new yard cases and affected generation receipts, geometry, source policy, containers, hauling, trap-jamming, input and presentation neighbors. This is an affected native Unity sweep, not the full project suite. The original ten-case RED and later four-defect review RED are preserved separately. The initial stale-assembly attempt is not counted as verification of current source.

## First native journey and observer correction

`Verification/SpreadDiscoveryExpeditions/Native/a9fc3161baa34b92aca3ca25f422898d/report.json` completed 13 named checks before refusing a later approach to the missing trap. This is a failed run, not acceptance. It recorded actual two-step hauling, release, finite lichen harvest and original Sack stock recovery (one DriedMeat and two GoldCoin); one ordinary starting healing tonic was spent during real defense.

Closer inspection corrected the apparent sequence: the original trap killed the Soursprayer at tick 960, immediately after southern arrival at tick 950. It did not survive until the outside loop. Earlier observer assertions used `!IsJammed(null)` and therefore incorrectly labelled absence as armed. Those assertions cannot prove an armed alternative remained during hauling; the report is retained explicitly as failed evidence. The next native revision uses exact current presence or an independently evidenced NPC-consumed branch. No trap is respawned, no NPC is pacified and no timing/health rule is changed. The original native eleven-check jamming witness remains historical evidence for the unchanged action; the new independent entrance tests prove jamming-only sufficiency in the expanded geometry.

The corrected observer is now authored: an NPC-consumed branch requires retained lethal `DamageDealt` and matching `DeathHandled` evidence for the same original trap and NPC at its cell, then proves both earned timber remain, the empty gate is crossed through ordinary input, and trap absence survives map revisit and F5/F6. Live armed checks now require an actual non-null SpikeTrap owner. Root approved this 20-check branch; its new native result is pending. These are observer changes, not gameplay, timing or source changes.

## Second live outcome: real combat injury

`Verification/SpreadDiscoveryExpeditions/Native/74355329f8df4633adcaa58ec5cde635/report.json` correctly identified the NPC-consumed trap using retained damage/death events, then passed the finite forage, actual native Haul, both paid hauling moves and free release checks. It completed 11 of the consumed-branch's 20 required checks before the existing strict action-clock observer stopped the run. This is partial evidence, not a complete round trip.

The retained `rejected-native-action-clock` window shows a real dagger attack for five damage at tick 1197. The Scrabbler retaliated for 16 damage at tick 1200 and severed the player's feet (dismemberment chance 12, roll 1); Speed changed from 100 to 40. The next turn arrived at 1217 with Energy 1032. The observer requires constant Speed across its window, so it correctly refused to certify the old equation. This is an actual combat consequence, not a missed input or a production hauling defect. No damage, anatomy, timing, enemy behavior or clock rule was changed to make this route pass.

Per the user's instruction to move past low-value verification detours, further retries of this journey are deferred. A focused generated-yard serialization test supplies the remaining persistence check below. The earlier complete jamming witness remains evidence for the unchanged action, and independent new unit fixtures prove both authored entrances are sufficient. Current expanded-yard Play evidence does **not** prove a complete surviving player round trip or player-jammed route in this layout.

## Cold review

Q1 — Selection, source detachment and rollback retain the existing ownership transaction. Only the original rolled cache moves; no death, XP, duplicate contents or new timber is granted. The same accepted physical packet is preserved by existing graph retention.

Q2 — The yard reuses the same native trap, drag, harvest, stock and wall mechanics as their existing locations. The store alone replaces the old free-bypass layout; other worksites keep their service approaches. Walls remain ordinarily destructible, an additional systemic route.

Q3 — Counters include Strength 7 versus 8, closed versus independently jam-open/haul-open paths, both actual southern seeds, unrelated worksite families, invalid factories, late structural mutations and independent late blockers. Four concrete review defects were observed failing before their final guard. Native armed and consumed branches now require different actual world evidence; absent is never treated as armed.

Q4 — Corrected the actual one/two-step beam route, original pocket-change fallback, conditional trap clue, wall destruction, NPC-consumed timeline and changed-Speed evidence limit. No all-seed, effortless-discovery, permanent-safety or full native completion claim remains. Pixel inspection confirms actual thick walls, separate openings and an existing beam/cache silhouette; the small trap itself is much less legible at the overview camera scale.

## Final persistence acceptance and files

Native Unity job `d7ad69e8800b428abfc11e76777c3c62`: **108/108 passed**, zero failures/skips, 62.53 seconds. [XML](Verification/TrappersDispatchYard/persistence-and-presentation-green.xml) combines 25 yard cases, two new generated-yard persistence cases, two existing native-launcher guards and 79 independent allotment-presentation checks. These are not 108 new yard tests. The 541-case affected sweep remains the broader production regression evidence.

Both persistence cases use the actual cold-generated seed-64 yard and its original stock. Explicit controlled player placement and two starting timber allow canonical commands to harvest once, haul/release, recover exact stock, then either jam the actual trap or trigger it with an ordinary nonplayer mover. Cached return and complete replacement session serialization preserve the moved beam, empty cache, harvested absence, actual item IDs/units/backlinks, and distinct jammed-present versus sprung-absent trap outcomes. The two tests passed on their first native run: existing persistence pinned as correct, no new save behavior or separate prior-RED claim.

**Can verify:** affected native tests; actual physical commands and exact replacement saved graphs in controlled fixtures; ordinary-input Play through hauling/release, and cache recovery in the first partial attempt; independently inspected native screenshots showing the actual site. The corrected second witness establishes actual NPC-trigger damage and disappearance.

**Cannot verify:** a complete surviving ordinary Play round trip in the expanded yard, unaided discoverability, all-seed frequency, final reward/difficulty balance, or effortless small-trap readability. The first witness's earlier null-as-armed assertions are invalid. The second witness stops on a real changed-Speed combat consequence. These limits are explicit rather than repaired with synthetic game state.

Files: `SpreadExplorationWorksites.cs`; new `TrappersDispatchYardTests.cs/.meta` and `TrappersDispatchYardPersistenceTests.cs/.meta`; migrated `TrapJammingContentTests.cs`; `SpreadDiscoveryNativePlayer.TrapJamming.cs` and new `SpreadDiscoveryNativePlayer.DispatchYard.cs/.meta`; this document, historical-design crosslinks and the referenced verification receipts. No new blueprints, item stocks, models or save fields.
