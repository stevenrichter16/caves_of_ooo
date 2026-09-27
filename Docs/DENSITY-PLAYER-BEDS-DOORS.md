# Player beds and generated door follow-up

Bed slice published for native acceptance; private RED/GREEN history is retained
below. Generated doors remain a separate candidate. Original CoO behavior, not a
Qud-parity claim.

## Verification sweep before code

| Planned premise | Actual source | Decision |
| --- | --- | --- |
| Beds are absent | VillageBuilder puts real Bed at each room's northwest interior corner | Source player sleep from existing actual furniture; no new blueprint or extra bed generation |
| Beds can already be used by the player | BedPart only handles NPC IdleQuery; occupied is reserved synchronously at query time and released by offer cleanup | Add two player world actions, refuse existing NPC reservations, preserve old idle behavior |
| Rest is simulated turn-by-turn | RestSystem instantly heals/cures Bleeding and advances the active clock without NPC turns | Reuse exactly that bounded primitive: 60 ticks or one next-band interval; do not claim overnight simulation |
| A bed should grant inn benefits | WellRested is granted by paid RestAtInn conversation, outside RestSystem | Free ordinary bed must not grant WellRested, currency or rewards |
| Rest can be rolled back like chair seating | RestSystem directly advances the clock and has no transaction undo | First slice is a direct world action like campfire; refuse an outer InventoryTransaction before mutation, rather than invent clock rewind |
| General locked doors do nothing | LockPart plus Physics already handles bump/menu unlock and drops Solid | Separate source/close/reopen gap from existing locked movement |
| Generated villages have doors to replace | VillageBuilder currently carves real wall gaps and path floors | General doors require their own source/path/AI plan; do not repurpose authored MorrowfastDoor ownership |

## First slice contract

Only a live Player standing on an actual unowned or owner-authorized free bed can
sleep. Both owners must exist in the same actual zone; bed must be nonportable,
not carried/equipped, and not reserved/occupied by another creature. An already
seated player must stand first. Reserve synchronously across RestSystem call and
release in finally. Hostile/death/clock-overflow refusal must not heal, cure,
advance time or consume another occupant's reservation. No hidden movement or
persistent new sleeping status. Existing saves already have BedPart and acquire
the actions without blueprint migration. Native C-menu timing and real bed
appearance remain parent-owned acceptance gates.

Tests first: direct world query/action, next-band and60-tick timing, owner ID/tag,
NPC offer contention and cleanup, physical occupant without stale flag, missing
actor/bed/clock, dead/tagged-dead, distance/foreign zone, portable/stale ownership,
outer transaction refusal, repeated rest time, hostile refusal and no inn buff.
Existing chair, IdleQuery, ownership, rest and save tests are regression controls.

Status: actual private RED 30 cases / 16 intended failures / 14 refusal controls passed (`bed-red.xml`). Minimum implementation now private; no shared publication or native execution.

## In-phase self-review and evidence

- Initial implementation: 30/30 private tests GREEN (`bed-green.xml`).
- Expanded bed/nearby run: 128 total, 126 pass, two real save failures
  (`bed-related.xml`). `SaveSystem.GetSerializablePublicFields` filters any field
  named `Owner`, discarding both `BedPart.Owner` and `ChairPart.Owner` strings.
  The new bed action exposes the consequence: an owned bed becomes freely usable
  after a save round-trip. This is a pre-existing serialization defect; the bed
  implementation did not write new ownership state.
- A deliberately supplied outer transaction refuses before mutation. The direct
  world menu does not supply that transaction, as confirmed by independent source
  review. The old chair fixture's bed pin is revised to retain its meaningful
  no-free-inn/no-inventory-heal invariant while accepting the new world actions.
- Independent review found no concrete P0–P2 issue in the bounded bed service.
  It confirmed real world action dispatch, synchronous actor/furniture claims,
  same-cell reach, NPC reservations, and the explicit rest compatibility boundary.
- Throwing old rest observers can throw after healing/time has committed. The
  service releases only its own reservation and claims in `finally`, and the test
  pins that existing irreversible behavior. This slice does not advertise
  callback rollback or NPC simulation.
- The standalone neighboring selection cannot compile `FoundingRestTests` because
  it directly uses native InputHandler. Excluded from the standalone selection;
  it remains required in native acceptance. Two private compile attempts also
  required explicitly adding the existing PartRoundTripHelper test-support file;
  those compiler errors are not counted as behavioral RED evidence.

Native acceptance still owed: use a real generated bed through C → underfoot →
bed → Sleep; record +60 clock ticks, healed HP, cleared Bleeding, no WellRested,
unchanged coins and no extra tactical tick. Then next-band timing, owner/reserved
refusal, and loaded owner refusal. Check actual cell/entity rendering and menu
visibility. Standalone tests cannot establish these input/rendering properties.

## General-door sweep and proposed separate slice

No ordinary-door production implementation has been written. This is a separate
movement/UI/AI unit, not a late data addition to player beds.

1. `World/Generation/Builders/VillageBuilder.BuildRoom` already creates one real
   wall gap per room; `CarvePath` avoids existing walls and does not put door
   entities at those gaps. Existing beds are already sourced inside these rooms.
2. `Items/LockPart` and `Combat/PhysicsPart.CheckCellBeforeMove` already unlock a
   real locked blocker, dropping Physics.Solid while keeping the current movement
   blocked. `Presentation/Input/InputHandler` then routes a breakable blocker into
   `DestructionSystem.StrikeStructure`. Because LockedDoor is destructible, a
   successful unlock bump can also strike it. A door outcome must be recognized
   before bump-to-break and must spend exactly one action.
3. `AI/FindPath.Search` uses Cell.IsPassable and a separate Physics.Solid blocker
   path; wide bodies use Zone.CanPlaceFootprint. It needs an actor-aware predicate
   for authorized openable doors, with unrelated solids/locked/foreign-owned
   doors still blocking. The actor-less/global cell passability API must not
   simply be weakened.
4. `AI/Goals/MoveToGoal` retries a blocked step and can fail to its parent in the
   same action, releasing NPC furniture reservations. A paid successful door
   opening must be a recognized action that keeps the path goal alive and moves
   only on a later turn. Do not silently open and walk in one step.
5. Proposed ordinary door part: explicit Owner ID/tag and open state, adjacent
   open/close commands, refusal to close on any occupying body/footprint, no
   portability/stale-owner use, save-stable state, render/FOV invalidation. Keep
   authored MorrowfastDoorPart and sealed archive barriers separate.
6. Add generated doors only after core and native UI movement tests pass. Place
   one in the existing gap, preserve the room's approach cell and connectivity,
   avoid overwriting path-reserved cells or authored settlements, and verify real
   NPC bed/chair goals traverse and release their reservations correctly.

Required RED/counterchecks: unlock does not also strike; close/open each costs one
turn; refused ownership/key/occupied close costs none; NPC opening costs a turn
without losing goal; blocked/wide-body paths stay blocked; old authored door
contracts unchanged; saved state/owner preserved; generated room and global exit
connectivity unchanged. Root coordination is required for shared InputHandler,
PhysicsPart, pathing, blueprint and source changes.

## Final private candidate state

The bed slice and narrow furniture-owner serialization correction are private and
stable. Save RED:42 cases/38 pass/4 failures (`bed-save-red.xml`), all four
failures were lost Owner strings. Final focused and adjacent tests:153/153 GREEN
(`bed-related-green.xml`), including42 bed cases, existing chair/NPC reservation,
world action, rest, entity identity and Effect.Owner rebinding controls.

The serializer writes named fields with their count; no field-order/schema
version change is required. Old streams without Owner continue with the existing
empty default. This fix cannot reconstruct owner information already absent
from an old save. Authored new or currently loaded owner strings survive future
saves. Effect.Owner and other Entity owner backlinks retain the old exclusion.

Files: new PlayerBedService.cs + meta, DensityPlayerBedTests.cs + meta; modified
BedPart.cs, WorldInteractionSystem.cs, SaveSystem.cs and one old chair bed pin.
No blueprints, shared scripts, input handlers, doors, or native editor state were
modified. Publication remains held until root releases the foundation freeze.


## Bed publication checkpoint

The reviewed bed service, existing furniture actions and narrow Owner-string save fix are now shared. C10 lair save hooks remain intact. Private RED→GREEN and153-case nearby receipts are archived in `Verification/DensityCompletion/Beds`. Two new native driver sources and their metas use real generated beds, labelled travel, actual menus and F5/mutate/F6. Full-health timing cannot demonstrate healing; core tests cover healing and bleeding. Native execution and new launcher-restoration cases remain pending. Doors remain a separate unpublished unit.

## Native bed route: verified input and persistence

Run `Everyday/Beds/Native/db9158a268f04dda80baaebdb8c1c0a8` completed15/15 checks with zero failures/errors in6.406seconds. The first inspected ordinary generated village (`Overworld.16.1.0`) supplied the real unowned bed4577 at60,5. One labelled approach transfer shortened travel. Actual C/underfoot menus refused adjacent use, accepted standing on the bed, advanced60 ticks and then to the next time band, and preserved ordinary40HP,50drams, equipment, action energy and absence of an inn buff.

A separately labelled ownership control assigned that same real bed the ID of existing NPC5033. Native use refused both before and after F5 → real movement plus changed owner → F6. The checkpoint proof requires replacement actor/bed references and exact owner, inventory/body/Physics links, clock and energy. The complete editor-log byte range and exact prior scene/start-scene/seed/save-root/preferences/input restoration are archived with the report.

Root inspected `03-sixty-tick-rest.png` and `06-restored-owned-bed.png`: the ordinary HUD shows40/40HP and50drams, the time changes Dawn→Height, and the visible log reports rest and the loaded ownership refusal. The other four captures were not inspected. These are the native main-scene furniture/input path, not a claim that the bed has new glade art. Full-health play cannot demonstrate healing/bleeding removal or natural prevalence of owned beds; the core tests cover those mechanics, while the owner metadata was explicitly diagnostic. Instant rest does not simulate NPC turns.

First Unity import also exposed two test-only assembly differences hidden by the standalone runner: ambiguous `Random` and an internal transaction-claim helper. The fixture now names `System.Random` and accesses the existing internal helper through the same reflection pattern as nearby ownership tests; runtime visibility is unchanged. The exact errors are retained in `Beds/native-compile-corrections.json`. Compilation is green; the full native bed/core/neighboring selection remains to run.

### Integrated native EditMode verification after bed replay

The actual 494-case Unity selection passed all 42 bed tests, 19 player-chair tests, 7 chair-ownership tests and 16 FoundingRest tests. The same run passed all 16 scene-restoration and 14 native-save-isolation tests. The overall selection is intentionally RED for pending door/art/material work; it is not an all-green integration result. Receipt: `Verification/DensityCompletion/Integration/native-seventh-liquid-bed-door-material-red-by-fixture.json`.
