# Generated ordinary doors — C4 follow-up

Status: reviewed core89/89 and32 native test-only cases are now published in the shared working tree. The explicit menu turn-cost branch and native rendering hunks remain held for actual native RED; Unity GREEN and native replay remain pending. This is a CoO interaction adaptation; no new Qud parity claim or lore. Parent owns publication, InputHandler coordination and Objects.json.

## Verified scope and corrections

| Premise | Actual source | Consequence |
| --- | --- | --- |
| Generated houses have door objects | VillageBuilder.BuildRoom removes one wall and leaves a gap; no ordinary door Part exists | Add a real open/close owner only to surviving recorded ordinary apertures |
| All village apertures are equivalent | OverworldZoneManager uses eleven authored composition builders with wider reserved approaches; Morrowfast has its own scene-owned door authority | Leave authored compositions and Morrowfast entirely separate |
| A door can be placed when the room is first carved | Connectivity runs later and the generic river/stamps can remove walls or occupy the aperture | Place after those builders and revalidate actual wall flanks, inside/outside, occupancy, liquid and reservation state |
| A failed move means no action occurred | PhysicsPart can successfully unlock a blocker but returns it through TryMoveEx; InputHandler then may strike the same breakable object | Add explicit consumed-action outcome with a native regression; preserve old moved-only APIs |
| Adding collision preserves daily NPC routes | FindPath rejects solids, while MoveToGoal retries and greedy helpers attempt alternate moves in one turn | Actor-operable ordinary doors need an explicit open-only action, and those callers must stop after it |
| Door state is only collision | Cell.IsWall feeds sight/light; existing Morrowfast stationary-door tests pin cache refresh | Closed ordinary doors must stop sight and collision, open doors release both; native stationary cache evidence is required |
| Cells.Objects includes all body occupants | Multi-cell bodies are anchored once; Cell.Occupants is the physical occupancy surface | Close refusal must use all occupied body contacts, not just anchors |
| Existing free inns imply public beds | Bed ownership/payments are a separate reviewed private slice | No BedPart, bed ownership, payment or lore edits here |

Ordinary sites still using VillageBuilder include Sill, Posy, Salt-Vault, Slip and the Quiet's Door; named composition branches are excluded. Natural Choir layouts are not new door sources.

## Bounded implementation design

1. A saved ordinary DoorPart and a single VillageDoor blueprint, initially open. Closed state is the authoritative movement/sight gate; a direct open/close action requires the actual living, action-capable actor, current nonportable owner, physical reach, ownership permission and a non-locked state. Closing refuses every non-ground occupant, including offset bodies and loose items. Refusals emit diagnostics and consume no ordinary action. Existing Morrowfast/archive contracts remain unchanged.
2. An additive detailed movement result distinguishes moved, blocked, and action performed while stationary. Existing bool/tuple APIs continue to report actual movement. Native input handles the consumed result before any bump attack/break fallback. NPC move/path callers stop after opening and enter only on a later action; actor-aware paths may plan through an operable ordinary door with an extra finite action cost. No implicit free movement or scheduler bypass.
3. VillageBuilder records the apertures it actually made. A late placement stage reads that provenance and validates the final local topology before placing initially-open doors. It skips widened/removed walls, reserved or liquid cells, foreign factory owners, missing blueprints and occupied approaches. A small deterministic multi-seed corpus proves actual generated sources and no new arrival/interior disconnections.
4. Native tests prove open/close and successful legacy unlocking each consume one action, refused actions consume none, and a successful unlock never also damages the owner. A finite native scenario uses an actual generated doorway, opens/closes, walks through, verifies stationary sight/light refresh and saves/reloads the door state. Renderer support and screenshots remain a separate visible acceptance gate.

## Test-first milestones and counters

- Core RED: actual open/closed gate, physical reach/current ownership, occupied close, saved state and diagnostics, moved-only versus consumed outcome, actor path/goal open-only then move. Counterpairs include locked/unlocked, dead/living, action-blocked/ordinary, hands/unsupported anatomy, owner/foreign, removed/current, identical object IDs but different references and multi-cell occupant anchors.
- Content/source RED: no doors in actual unmodified generic villages; preserved open topology after the candidate. Exclusions for authored compositions, profile stamps, rivers, reserved approaches, missing blueprint and stale/foreign factory products.
- Dedicated adversarial sweep after primary GREEN: stale selection, changed part identity, callbacks, duplicate/foreign objects, malformed footprint, save/reload and optional lock combinations.
- Native UI RED must precede any shared InputHandler route change; private source compilation does not substitute for this.

## Evidence boundary

Private runner proves core rules and deterministic stubbed generation, not Unity-identical seeds, native input, rendered door appearance, timing or feel. No global permission, theft/ownership morality, automatic reclosing, locks/key economy or general architecture redesign is planned.

## Private implementation and evidence

Primary15 RED→GREEN established the real DoorPart/collision/action contract. Navigation/footprint expanded to26 cases:7 intended RED/19 controls→26 GREEN. The first23-case adversarial pass found two ordinary+authored barrier authority failures, repaired by refusing hybrid owners. Two discarded fixture drafts were corrected without changing production: an offset body initially overlapped the actor, and an unrestricted A* correctly preferred a shorter open detour over a paid door. Those are not counted as product bugs.

Eighteen content/source cases ran16 intended RED/2 authored-composition controls before placement/content changes. The candidate adds one initially-open VillageDoor blueprint, records exact builder-run apertures with original wall-owner references, places only after river/stamps, reserves the door and both approaches, and leaves original RNG consumption and initially passable cells unchanged. Generic population now respects existing interior reservations; eleven authored compositions and Morrowfast keep their existing builder authorities. Actual Sill, Posy, Salt-Vault and Quiet Door pipelines produce ordinary doors. Existing saves reconstruct their old owner graphs and do not rerun generation.

Seven factory callback probes found two defects before publication: rebuilding the source invalidated a live aperture enumerator, and a callback could assign a foreign OwnerId to a fresh door. A recorded-aperture snapshot plus generation revision/current-zone checks handles reentry; placement refuses preowned latches and any foreign current owner. Eight optional lock cases then reproduced stale open-latch glyphs on bump/menu unlock and four owner/reach/action authority bypasses. The narrow LockPart integration only applies when an ordinary DoorPart is attached; existing lock content retains its behavior. The saved open latch is not rewritten by unlocking, and glyph/dirty state refreshes after actual lock change.

Final focused corpus is **82/82 GREEN**. The exact existing movement/lock/path/goal/village/save corpus is **315/315 before and315/315 after,0 newly failing and0 newly passing**. The same snapshot and selected test sources are recorded in `verification/existing-comparison-selection.json`; native-renderer-dependent fixtures are explicitly excluded and remain part of the eventual Unity sweep. Actual earlier native input RED is2 intended failures/2 controls: legacy successful unlocking damaged a breakable door22→20 and consumed no turn on its nonbreakable counterpart. The candidate's InputHandler now handles stationary actions before damage and guards its status fallback against charging a second action. Native GREEN remains pending.

All private runtime C# compiled against actual current Unity references with zero errors. That compiler caught one leftover moved-variable use in the new InputHandler branch; it was repaired before readiness. This is compile evidence only. No native generated-door interaction, stationary rendered light/FOV proof, art binding or screenshot acceptance is claimed.

### In-phase self-review

- Q1/authority: current references, exact part identity, actor reach/action/owner, portable/foreign/factory-mutated products, original flanks, builder generation revision and separate authored barriers are explicitly guarded and paired.
- Q2/action: moved-only APIs remain moved-only; richer navigation/input can report one stationary open/unlock action. MoveToGoal/StepGoal retain their pending movement step and greedy fallback returns after its action. Rooted may explicitly open a door while remaining unable to move, matching its AllowAction/AllowMovement distinction.
- Q3/persistence and source: open/closed state and OwnerId round-trip, current glyph derives from closure, source creation is limited to recorded ordinary apertures, initial topology and downstream RNG are unchanged. No rental, bed, lock/key economy, Choir structure, new lore or BitLocker change is included.
- Q4/limits: engine-free seeds differ from Unity. Full native tests, ordinary generated-door replay, stationary renderer caches and the new door's truthful visible representation are still required. Existing generic interior-reservation behavior is intentionally tightened to protect approaches; the exact existing315-case comparison is green, but native population/quest smoke remains an acceptance gate.

Private publication manifest `verification/proposed-publication.json` lists exact files, hashes, narrow runtime patch and one-block data proposal. The manager was refreshed to preserve root's concurrent legendary-keeper hook; do not replace it from an older private snapshot.

## Saved facing, native models and menu-route sweep

A further saved-field test ran83 cases with exactly1 intended failure for absent aperture orientation. The candidate now saves DoorPart.QuarterTurns from the actual recorded inward direction after all factory/source guards and before committing placement. The same five-seed test requires both wall axes and round-trips each value. Focused83/83 and the matched315/315 existing corpus remain GREEN; zero newly failing/passing. The blueprint now says simply “door” and describes a fitted panel/latch, matching the proposed borrowed Stillleaf panel art without inventing wood/material behavior.

The required visible route uses the already-shipped four closed/open door mesh pairs. Twenty-six private native tests cover state/orientation, exact owner/current membership/nonportable/render constraints, cached assets, real open/close model changes and global registration outside Stillleaf. Tests compile against actual Unity/project references; actual native RED is pending and the two renderer source hunks have not been implemented or published. The recipe will be independent beside the exact poured-pool branch.

Preparing the actual native replay exposed one additional real dispatch gap: the generic world-action path executes OpenDoor/CloseDoor without ending a turn. The four new private DensityGeneratedDoorMenuTests require exactly one action for open/close and zero for occupied/foreign refusal. Its source compiles, but actual native RED must run after core import before implementing the narrow menu branch. This is not covered by the earlier four legacy bump-unlock tests. It blocks claiming doors complete.

The private actual-source replay is under /tmp/coo-door-native. It uses an ordinary40HP/50-dram actor, searches actual ordinary VillageBuilder/late-placement pipelines, records every inspected source, and takes one labelled transfer to a real doorway’s exterior. All subsequent close/open/bump/movement/save/load operations use native controls. It requires stationary paid bump-open before later movement, real threshold traversal, current model identity, and F5→actual native door-state mutation→F6 replacement actor/door graphs with exact state/facing/clock/gear restored. No door, key, permission, NPC, injury, health or equipment is granted. Reference compilation passes; native execution remains pending. Stationary FOV/light-cache and NPC autonomous usage are explicitly outside this Play driver's claim and require separate native tests.

## Final physical-ground countercheck

A last actual-content probe confirmed four failures: both Bush and StairsUp inherit Terrain, so the old predicate treated them as bare ground during closure and late placement. Two Grass controls passed. The narrow repair shares the closure predicate with placement and excludes destructible scenery and stair parts while retaining ordinary terrain. Final focused89/89 passed after89 total/85 pass/4 intended RED. This is a concrete classification correction, not a new furniture feature. The final native32 test-only cases and paid-menu/render implementations remain separate gates.

After the shared predicate repair, the exact existing315-case after-run is315/315; comparison with315/315 before again reports zero newly failing and zero newly passing. Final whole-runtime native-reference compilation has zero errors. Native execution remains pending.

## Shared core/test publication checkpoint

Published only the reviewed core source,89-case fixtures and32 native model/menu/cache tests. `published-data/content-parsed-diff.json` proves one VillageDoor addition and all500 preexisting blueprint objects unchanged. The existing legacy bump fixture was already shared after its actual RED. No native driver, explicit ordinary-door menu-cost branch, recipe or mesh-registration implementation was included. This is an integration checkpoint with expected native RED cases, not a finished visual/gameplay acceptance claim. The manager patch preserves the existing C10/C11 hooks.


## Native failures and private follow-up

The authoritative494-case native integration receipt (`Integration/native-seventh-liquid-bed-door-material-red.json`) established10 expected missing-model failures and2 expected explicit menu-payment failures. It also found5 unexpected door failures: four actual generic pipelines contained late HouseDrama villagers without CanOpenDoors, and stationary opening refreshed FOV but left the existing LightMap dark. Actual doors were present in all four generated villages. Native content initialization loaded late HouseDrama roles that the initial standalone fixture did not load, exposing a real omitted source.

A new explicit late-drama/source/light fixture ran13 cases with9 intended failures and4 controls before repair. An optional-lock light pair added1 intended failure and1 closed-latch control. The private candidate now passes **104/104** focused cases; the same329 existing movement/lock/path/village/save/light and HouseDrama cases pass329/329 before and after, with no newly failing or passing cases. Full runtime reference compilation has zero errors. Native follow-up GREEN remains pending.

The narrow candidate gives ordinary village drama builders explicit door permission and existing placement reservations; authored compositions keep their previous permissions. Successful stationary open/close and optional unlocking advance a runtime door-occlusion epoch used by LightMap, leaving EntityVersion and save data unchanged. The explicit world-menu route pays only successful TrySetOpen. Independent review found no concrete blocker. Q1 pairs state and lock changes with the same cache invalidation; Q2 preserves the existing part authority for menu/bump calls; Q3 includes the saved-closed latch control and disabled drama-context controls; Q4 keeps all native/visual acceptance claims pending.

The first mesh-registration control used `Overworld.4.6.0`, which is OlderDeep and already registers Stillleaf assets. That passing result did **not** prove ordinary ring coverage. The private expanded fixture retains it and adds `Overworld.2.5.0`, plus a malformed footprint refusal. The separate eight-mesh registration hunk stays held until the new ordinary-ring control runs native RED. The model recipe reuses cached Stillleaf open/closed panels with saved facing and real owner/state guards; it creates no art assets.

Existing moved-only APIs and stair auto-walk retain their old movement contract. A manually closed ordinary door may stop stair auto-walk; normal movement keys and the reviewed NPC approach/goal paths support paid stationary opening. No wider automatic-travel behavior is claimed.


Publication checkpoint: the reviewed follow-up is now in the shared working tree; native GREEN/replay remains pending. The door mesh-registration production hunk and all harvest timing production remain held for their native RED gates. The registry-only liquid audit correction is published; no optional visual guards were included.

## Ordinary-ring registration and reservation-pin closure

The actual245-case native gate (`Integration/native-contact-door-harvest-red`) passed the door core/follow-up/menu/cache groups and produced exactly the intended ordinary-ring art failure: all8 borrowed door meshes were absent from the ordinary ring registry. OlderDeep control remained. The isolated shared registration loop now includes only the4 closed/open pairs; native GREEN is pending. Exact per-fixture totals and before/after source hashes are preserved in `Everyday/Doors/native-followup/registration-and-pin/publication.json`.

The exact672-fixture standalone regression also found one obsolete Sumphold test expectation: an ordinary replacement village was required to ignore reservations. Real generated doorways now reserve their approaches, and late drama actors must respect them. The corrected test requires reservations on both composed sites and the ordinary source, but ordinary-door permission and actual VillageBuilder/door-placement builders only on the ordinary route. Direct raw-builder default-false controls remain. This intentional pin correction passes53/53 focused checks, with no production reversal or general authored-door permission expansion.
