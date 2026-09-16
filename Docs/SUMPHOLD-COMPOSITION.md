# Sumphold — dry work above patient water

Status: complete and installed. All 45 owned native checks pass within WB11’s282/282 focused run. WB14 full regression: 13,451 passes, 32 unchanged baseline failures, zero C# errors and no new failures. All 282 additional tests pass.

CoO-original named-site composition. See [the aggregate plan](CINDERHOLD-SUMPHOLD-COMPOSITION-PLAN.md) for source corrections, implementation, visual review and exact integration records.

## Scope and verified corrections

Only Overworld.15.6.0 under current Village/Boatyard authority is eligible.
WorldMapAuthoring makes it Spread tier1, without mapped road or river. Local
water cuts are therefore explicitly authored workshop-edge terrain, replacing
the generic village's unconditional destructive bottom river in this scope.
No world biome, neighboring village, current saved graph or spawn is changed.

The native profile is exactly two BoatFrames, two PeatCutters and one TollRolls
(LandmarkBuilder.SumpholdBoatyard). Hulls and records are solid examinable
fixtures without native Material/Destructible/vehicle/toll Parts. PeatCutters
retain their existing conversation and actual boots/gloves/Dagger loadout;
the descriptive spade does not create a new inventory tool or mining action.

## Intended native architecture

Three unequal stone shelters (cutters house, service house and records shelter)
and shared well commons sit on dry ground.
Three working fingers project beside shallow WaterPuddle cuts, with native
PeatBank faces and grouped reeds on their margins. Duckboards mark genuine
dry routes: no co-located water, hidden immunity or collapsing bridge logic.
Receiving, hull-work and record-reading spaces have accessible dry frontages.
The main native well remains at40,12, with open cardinal service approaches.

Ground is native Floor outdoors and StoneFloor only inside actual shelters.
SandstoneWall remains masonry. Bed/Chair/Crate supply functional furnishings;
shared VillagePopulation, stock, containers and house-drama remain authoritative.
This site does not silently enable repair tracking unavailable to ordinary towns.

## Assembly and lifetime

Base1000 stages required native owners and validates actual Parts before any
zone mutation, then publishes shade and protected cells. Profile3860 realizes
its five owners once under same-zone identity. Arrival3870 protects actual
rolled stairs and their open cardinal cells before village residents/services.
A cleared generation retry may rebuild fresh owners; a populated or foreign
zone must reject without replaying scenery over player changes. Each gate and
successful assembly emits a scoped worldgen diagnostic.

Plans allocate only during fresh generation; no frame or turn loop is added.
Rendering must follow current native owner identity and dirty cells, never the
original placement plan after movement or destruction.

## Verification gates

Tests require deterministic meaningful layouts, exact scope, connected dry
frontages, native water on wet cells, no water under boards, real peat/board
destruction, native profile counts/Parts, staged missing or malformed dependency
rejection, retries, actual final-pipeline stairs and service access. Visual
review will inspect three native seed renders; it cannot establish live input
feel or sustained gameplay frame time.

## Implementation log

- Preimplementation: read CLAUDE.md and aggregate plan, verified current map,
  stamp/profile, destruction policy and village pipeline. Wrote core plus
  adversarial fixtures before new production. Await actual RED capture.

- WB02: actual missing SumpholdCompositionPlan compiler RED captured before
  production. Implemented base1000, native profile3860 and diagnostic arrival
  reservation3870; all initial C# and docs remain scoped owned files.

- WB07: native realization/profile/destruction gates passed for the three
  preview seeds. Expanded actual-stair sweep exposed a seed where a service
  doorway atX40 targeted the future solid mainwell. Fixed the connection rule
  to use its clear north frontage, and added four explicit core seeds.
  Four additional real HouseDrama reservation REDs exposed later residents
  bypassing protected interiors/wet cells; root owns the scoped shared fix.

- WB09: all prior Sumphold native/route/HouseDrama gates passed. The new
  contact test exposed a false test premise: Zone.ProjectPool legitimately
  mirrors live LiquidPool owners as permanent water coatings (Zone.cs665–710),
  and UnprojectPool removes the last owner's projection. Corrected the test
  to preserve this authority, verify real movement coats the player only on
  water, and verify pool removal clears its projection but preserves ground.
  No baseline liquid or movement mechanics changed.

- WB10 refined preview: independently inspected all three Sumphold seeds.
  The dedicated deep-teal water surface now clearly separates real wet cuts
  from dry work fingers; the former olive checker ambiguity is resolved.
  Native stone shelters, open entrances, peat head faces and narrow dry boards
  remain readable. Shelter silhouettes and dock topology are intentionally
  simple named-site rules; this is not a general boatbuilding simulation.
- WB11: 282/282 combined targeted cases pass, including all45 owned Sumphold
  cases, native water contact/projection removal, expanded seed routing,
  board/peat destruction, and both composed-town HouseDrama opt-ins with the
  unchanged FirstTent countercontrol. No Unity execution performed by this
  module's author; results are the root's isolated run artifacts.
