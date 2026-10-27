# Shared ordinary furniture — Bed and Chair

**Status, 2026-10-27 22:59 UTC: two-body integration verified in Editor; source push and fresh packages pending.**
The user ended the cave detour and restored broad biome/area coverage with straight
wall silhouettes. The two unchanged East kitchen Bed/Chair bodies now resolve
for eligible ordinary owners globally. This bounded Editor slice has395 distinct
native passes and three inconclusive existing allocation probes, plus real
Bed Rest, Chair Sit/Stand, saved seating, fresh Continue and movement-release
observations. The East kitchen remains the unchanged visual control. The earlier
baseline and design contract below are preserved as historical preflight; the
implemented result and limitations are recorded in the final continuation.
Current packages still contain shared-storage source`d9f757841`; fresh furniture
packages and their actual standalone checks are the next delivery step.

Root owns Unity, gameplay, native tests, Git and packages. This is original
Caves of Ooo presentation work, with no new Qud-parity claim.

The objective is to carry the already-approved Bed and Chair identities into
their real placed owners throughout the world. Use two unchanged native cells,
not a new furniture pack or extra placements. This advances broad sprite
coverage before the queued slate7–8 material band. It does not complete any
settlement, biome or interior, and does not change any of the55 world inventory
or five Stump area polish statuses. Later serious area-by-area polish remains
required; optional nuanced animation remains deferred.

## Verified corrections before implementation

All production source references in this section were read for this preflight.
The proposed renderer must preserve the services below, not reimplement them.

| Assumption to avoid | Verified source and consequence |
| --- | --- |
| Beds and chairs already have native art everywhere | [EastKitchenCatalog:111](../Assets/Scripts/Presentation/Rendering/FlatPixelEastKitchenCatalog.cs:111) maps Bed to frame2 and Chair to frame3. [Greybox:1440](../Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs:1440) still admits this companion only at12.10.0 and12.11.0; world and reader calls additionally depend on local admission. Extend these exact bodies, not all eastern services. |
| Both furniture owners are solid wooden destructibles | [Chair:8316](../Assets/Resources/Content/Blueprints/Objects.json:8316) and [Bed:8372](../Assets/Resources/Content/Blueprints/Objects.json:8372) inherit PhysicalObject, explicitly use nonsolid Physics and Furniture tag, and have no authored Material, Thermal, Destructible or Hitpoints. PhysicalObject contributes Render/Physics/Examinable, not wood damage semantics. Do not copy storage's Wood/HP gates or invent breakability. |
| A bed icon promises an empty, usable bed | [BedPart:20](../Assets/Scripts/Gameplay/AI/BedPart.cs:20) adds real Sleep and SleepNextBand actions. Owner and Occupied can prohibit use; the static body depicts neither occupancy nor availability. Occupied/Owner changes must not alone remove its identity. |
| Sleeping leaves the player in a persistent sleeping pose | [PlayerBedService:16](../Assets/Scripts/Gameplay/AI/PlayerBedService.cs:16) uses instant RestSystem actions. The player must already stand on the bed; adjacent access does not teleport. It rejects dead actors, portable/custodied beds, ownership mismatch, occupied/reserved beds, sitting actors, physical creature occupants and invalid clocks. It claims actor/furniture across synchronous rest and releases Occupied in finally. No sleep animation or persistent occupied derivative is justified by this action. |
| Chair Occupied means an actor is visibly sitting there | [ChairPart:27](../Assets/Scripts/Gameplay/AI/ChairPart.cs:27) includes outstanding NPC reservations. Occupant identifies a seated player; null does not prove a free chair. Do not infer a body state from either field. |
| Rendering can ask IdleQuery whether a seat is usable | [BedPart:41](../Assets/Scripts/Gameplay/AI/BedPart.cs:41) and [ChairPart:68](../Assets/Scripts/Gameplay/AI/ChairPart.cs:68) synchronously set Occupied when making an NPC offer; the offer Cleanup releases a failed reservation. Rendering, portrait selection and profiling must not dispatch IdleQuery, call a service, advance time or mutate reservations. |
| Sit and Stand have the same preconditions | [PlayerSeatService:12](../Assets/Scripts/Gameplay/AI/PlayerSeatService.cs:12) requires actual same-cell distance for Sit and respects ownership/reservations. Stand instead requires the exact SittingEffect furniture and Occupant identity. Transactions restore effects and reservation fields on rollback. Real movement, not a blocked move, releases the player's seat through AfterMovement. Preserve this asymmetry. |
| Furniture availability follows only its own fields | [SittingEffect:23](../Assets/Scripts/Gameplay/Effects/Concrete/SittingEffect.cs:23) releases the appropriate chair occupant/reservation or bed reservation when removed. The renderer cannot clear these flags or repair stale occupancy. Bed and chair services are not identical; do not invent a common availability helper. |
| Shared art requires globally admitting every regional kit | [Fallback:111](../Assets/Scripts/Presentation/Rendering/FlatPixelFallbackCatalog.cs:111) already rejects replaced cached graphs for attached world contexts and intentionally permits standalone zones without a manager. [Greybox:816](../Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs:816) begins with actual fallback selection. Reuse this authority contract; do not widen IsAdmitted, invent map/POI restrictions for shared furniture, or demand a manager where the existing public path allows a standalone zone. |
| A semantic sprite helper authorizes an examine document | [Greybox:1099](../Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs:1099) explicitly separates sprite policy from current document/custody authorization. Retain the actual reader authorization path and selected owner, not a newly manufactured subject. |
| An old local companion can safely rescue a refused new owner | The existing East helper uses its own weaker furniture checks. Known Bed/Chair refusal in the shared contract must not fall through to the old local furniture route. Pair local and nonlocal counterchecks, and preserve deliberate injected-resource behavior in existing fixtures. |

## Exact body scope and immutable art

| Exact owner | Authored body and proposed guard boundary | Existing immutable frame |
| --- | --- | --- |
| Bed | `=` / `&W`, RenderLayer5; Furniture; unique owned BedPart, Render, Physics and Examinable; nonsolid, nontakeable, not carried/equipped; one current cell. Owner and reservation values do not select another body. | `Sprites/Live16x24/east-kitchen`, frame2, rect`(32,0,16,24)` |
| Chair | `h` / `&w`, RenderLayer5; Furniture; unique owned ChairPart, Render, Physics and Examinable; same physical body boundary. Occupied/Occupant/Owner are real service fields, not artwork states. | Same texture, frame3, rect`(48,0,16,24)` |

The inherited [Physics defaults](../Assets/Scripts/Gameplay/Combat/PhysicsPart.cs:8)
are nonsolid and nontakeable. The proposed art contract rejects altered solid,
portable, carried/equipped, multicell, reskinned or conflicting-role bodies
without changing their mechanics. Do not map arbitrary Furniture-tagged
objects, mats, crops, desks, benches, stations, plants or actor disguises to
these two frames. Keep duplicate/foreign essential parts and cross-type
Bed+Chair claims out of native body selection. Actual active Render-event
color cues must survive through the existing one-event painter; the resolver
and portrait helper must never dispatch another event or evaluate service use.

The [source manifest](../ArtSource/Live16x24/east-kitchen/manifest.json) records
a288×24/18-cell atlas, centered pivot and16PPU. Bed's vertical cot has184 opaque
pixels/four colors; Chair has113/three. Both fit a single16×24 cell, with their
last occupied top-origin row22 and transparent row23. The bed source explicitly
claims no sleeping actor, bedding occupancy or claimed-use cue. Cache only two
FullRect Sprite/Tile views at original scale. Do not crop, rerender, stretch,
recolor, repack or resample the approved pixels.

Read-only pins observed for this plan:

| Existing input | SHA256 |
| --- | --- |
| Source and resource `east-kitchen.png` (byte-identical) | `594079f4dbfe9d17f0b58f8fcb6adc647d63dae9cfd385ce32fd8b6e31413c56` |
| Current resource `.png.meta` | `ccfb13792373e3272af8f764367984d40ad22bd435bece234f0317230bf0f1ee` |
| Editable `east-kitchen.blend` | `f45e8a930f870c1e29023bf51b045e2617ff079ae92c9c3d6010dcc020266b56` |
| Editable `source-cells.json` | `b3e6cdbdcb0a2e4240bf6307e01382900f92a5e0ac518b08168e7e5f1efe33df` |
| Existing `Tools/Live16x24/build_east_kitchen.py` | `7b3d6f5967b5b7011eb8a2e6052360b2fc64337677f5b1f4c04509b50f86f8ab` |

These are current file hashes, not a new HEAD comparison or rerun of the
original authoring verifier. The current meta requests Point, Clamp,16PPU,
centered pivot and uncompressed texture. The historical editable/source proof
is retained in [the source README](../ArtSource/Live16x24/east-kitchen/README.md)
and its verification records. Any future import normalization must be reported
separately from pixel changes; do not silently repin protected inputs.

## Practical ordinary before route and breadth

The existing [healthy earned fixture](Verification/Live16x24/Fixtures/sodden-before-room65/Quick.json)
has seed65, Overworld.15.7.0, turn2000 and38/40HP. Its adjacent README records
player34,8 in the shelter's north room. The retained
[matched capture receipt](Verification/Live16x24/sodden-after-12-shelter-room-matched.json)
later records the same player cell at2090 and contains no selected Bed or Chair.
Therefore neither south-room furniture owner is yet visually witnessed by this
phase; existing source coordinates are not disclosure or safe-route evidence.

[SoddenDistrictPlan:40](../Assets/Scripts/Gameplay/World/Generation/SoddenDistrictPlan.cs:40)
constructs a north entrance34,10, an east-west route at y12, and the south-room
entrance50,16. The finite ordinary route proposal is: leave north via34,10–12,
walk east along y12 to50,12, then south through50,16; approach the real Bed47,19
and Chair55,19. Stop and adapt to actual actors, hazards and visibility. Do not
teleport, alter fog, edit saves, spawn furniture or infer current occupancy from
this cold plan. Root must capture whole-room context and each actual reader,
then perform allowed real same-cell Sleep/Sit/Stand actions or record genuine
refusals. Sleep advances time and may change health; save those consequences
honestly rather than restoring only the convenient parts of the run.

This is a shared-owner slice, not a Sodden-only exception. For example,
[VillageBuilder:228](../Assets/Scripts/Gameplay/World/Generation/Builders/VillageBuilder.cs:228)
places exact Chair/Bed in passable interiors when the blueprints exist. The
existing East kitchen is the approved native control. A second actual region
or settlement should establish cross-region continuity, preferably through
ordinary travel or an unchanged earned checkpoint; no fixed count of covered
biomes or all settlements is asserted from these examples.

## Minimal implementation proposition — not yet code

### Frozen API and source correction, 22:42 UTC

Freeze `FlatPixelSharedFurnitureCatalog : IDisposable`, default and borrowed
`Texture2D` constructors, public `HasKnownRole(Entity)` and
`TryResolve(Entity,out Tile)`, and Greybox's `_sharedFurnitureArt` field.
The default loads the existing East atlas; explicit null/malformed injection
never reloads it. Existing East tests inject only `_eastKitchenArt` into a
no-Resources Greybox, including valid Bed/Chair. Preserve this deliberate
contract with a narrow disposed-aware borrowed-atlas accessor and run the
same stronger semantic checks; do not fall through to the old weak resolver.
Known refusal dominates all four world/reader entry points. Native rendering
retains the existing single Render-event cue and fallback authority.

The actual baseline and approved atlas were reinterpreted with the built-in
imagegen tool. The generated reference preserves the broad two-body placement
and suggests readable small furniture, but changes image resolution, lighting,
UI details and apparent scale. Those deviations are rejected. Extract only
the existing approved bed/chair identities and use their exact source frames;
no generated pixels enter runtime. Prompt, image, input hashes and critique
are retained in `ArtSource/Live16x24/shared-furniture/`. This is a concept
review, not gameplay acceptance or completed area polish.

First-effect chair prerequisite `39d03b26b742511f0e6f172706c79ce08ad18b7c`
and preservation `77c8bfbe9` are verified on origin/main. Current packages
still identify d9f757841; they do not contain the later chair fix.

Follow the bounded shared-storage companion pattern: a default constructor
loads only the existing East texture; an injected constructor accepts that
borrowed Texture2D without hidden Resource loading. Proposed public shapes are
`HasKnownRole(Entity)`, `TryResolve(Entity,out Tile)` and `Dispose()`, with a
lazy `_sharedFurnitureArt` hook in Greybox. This API remains a proposition until
root freezes the test contract. No per-zone furniture registry or framework is
needed. The semantic catalog does not grant world or reader authority.

Keep source selection independent of all other East frames/services. World
selection must retain actual current placement, visible/explored-cell and
current selected-owner rules; a covering player/NPC remains the foreground.
Remembered/unexplored views retain their established policy without newly
revealing current occupancy. Removal, carrying, foreign placement and stale
graphs cannot leave a native furniture ghost. The existing underlying terrain,
water/hazard overlay and movement rules remain unchanged.

The catalog owns its two cached Sprite/Tile views and disposes them once; it
does not destroy the borrowed texture. Resolve has no resource loads, sprite
creation, LINQ, per-cell collections, gameplay event dispatch, RNG, service
queries or world scans. Reuse the existing paint receipt path. Profile actual
ordinary movement and repeated lookup after warmup; do not infer speed or zero
allocations from source inspection alone.

## Native RED and paired counterchecks — pending execution

Write a bounded public-path fixture, run genuine native RED before production,
then retain corrected RED/GREEN and related regressions. Proposed invariants:

1. Both real factory owners resolve the exact immutable frame in a nonlocal
   current graph, submitted world paint and authorized examine; compare the
   unchanged East control and deliberate injected atlas route.
2. Missing/null/wrong-sized atlas, disposed catalog and malformed semantic
   owner refuse truthfully in both East and nonlocal zones. No old East rescue
   bypass; unrelated East services and resources continue working.
3. Real ownership/reservation/occupant changes preserve the neutral body.
   Missing/duplicate/foreign essential parts, cross-role service claims,
   VisualID/variant/custom glyph or layer, solid/takeable/custody/multicell
   mutations refuse without changing those fields.
4. Hidden/unexplored/remembered, stale cached graph, foreign or removed owner
   and actual covering actor preserve existing disclosure/selection. Full and
   dirty submission must clear/reveal the proper sprite while ground and
   independently submitted hazards remain intact.
5. Repeated world/reader queries leave Owner, Occupied, Occupant, goals, effects,
   HP, position and clock unchanged and dispatch no IdleQuery/service actions.
   Pair a real NPC offer (reservation becomes true) with rendering (no change)
   and real offer cleanup (reservation becomes false).
6. Exercise actual player Sit/Stand plus same-cell/remote and occupied/owned
   refusals; real movement releases the seat while blocked movement retains it.
   Exercise actual bed rest plus ownership/reservation refusal; preserve real
   clock/HP change and no lasting bed reservation. This tests existing services
   through the new presentation, not a new furniture mechanic.
7. Preserve one real Render-event color cue with an explicit successful effect
   or test-event precondition; portraits do not dispatch extra events. Save and
   reload the actual seated/owned state and exact furniture identity where the
   existing services permit it. No fabricated persistent sleeping state.
8. Cached views are stable; disposing the companion does not destroy the shared
   texture or a caller-owned world catalog. Restore test hooks, clock, factory,
   save isolation and borrowed renderer ownership on teardown.

Existing service tests are regression controls, not new evidence for this
slice. Avoid duplicating their entire mechanics suite; add only necessary
cross-presentation assertions and meaningful flipped-precondition counters.

## Production and actual acceptance gates

1. Root obtains the real Sodden before scene/readers/actions and records exact
   owners, route, visibility and state; also retains the East native control.
2. Use the actual scene and unchanged approved art for the required imagegen
   reinterpretation/parts review. Save inputs, prompt, output and critique.
   Reject invented occupants, furniture positions, room expansion, altered fog,
   new services and generated replacement pixels. Existing original native
   source reuse is the intended production result; new drawings require a named
   actual defect and separate review.
3. Freeze the two-owner contract, confirm native RED, implement minimally and
   pass focused/related counterchecks plus an independent review. Preserve all
   approved source/resource pins and the previous presentation setting.
4. Repeat the same ordinary room/read/action route. Verify actual ground
   contrast, scale, owner/reader identity, actors over furniture, state cues,
   occupancy/refusal and save/fresh Continue. Share actual screenshots inline;
   contact sheets and tests do not establish gameplay acceptance.
5. Review whole-room and approach composition at normal zoom. This slice may
   reveal empty rooms, glyph services, crops, routes or uneven material contrast;
   record those defects separately instead of claiming furniture count equals
   fullness or adding speculative decoration.
6. Record bounded source/Editor results in the same commit as implementation,
   push exact owned files to main, then verify fresh packages and restoration.
   No Linux execution, physical Deck, performance or whole-area polish claim
   follows merely from a build or archive check.

## Self-review and continuation

- **Source-corrected:** state-invariant body versus use permission; instant bed
  rest versus persistent chair effect; NPC query reservation side effects;
  current local art restriction; no authored wood/damage parts; old-local
  refusal bypass risk; standalone graph behavior; exact unchanged frame reuse.
- **Pending:** new actual before, concept/parts review, frozen API, native RED,
  implementation, GREEN, independent review, ordinary after/persistence/profile
  and fresh packaged delivery. No gate is silently counted as complete.
- **Independent source cross-check:** the build/comparison agent confirmed the
  instant bed-rest distinction, real chair Sit/Stand and movement release, and
  absence of a SittingEffect Render override. This was read-only source review,
  not test execution or ordinary gameplay acceptance.
- **Files changed in this preflight:** this new document only. No runtime,
  tests, art, Unity, Git, shared living ledger or hourly clock was changed.

## Resumed ordinary before route — 2026-10-27 22:27UTC

Root used unchanged earned room65 and ordinary keys through34,12→50,12→50,17.
Eight actual captures show the north/south-room glyph baseline, exact Bed/Chair
readers and real use outcomes; baseline107 preserves50,17/HP38/turn2250.
Bed12657 at47,19 restored2HP, advanced60clock and released occupancy.
Chair12666 at55,19 offered Sit but refused `invalid_context`; read-only checks
confirm both owners placed, a Player actor, valid ChairPart, and no actor
StatusEffectsPart. No effects part was fabricated for the screenshot.
The [raw diagnostics](Verification/Live16x24/shared-furniture-before-diag.json)
and [ordinary-play receipt](Verification/Live16x24/shared-furniture-before-play.json)
retain the failure. Final earned108 keeps55,19/HP40/turn2440. The whole isolated
session was copied byte-for-byte before stopping.

This exposes a pre-existing first-effect use defect, not new sprite coverage.
A focused RED/source correction precedes the furniture adapter. Native body
integration, concept/parts review, second-region control, after/restart and
packaging remain pending. The broad sparse south room and weak night contrast
remain explicit later composition/polish debt; no area acceptance advances.

### First-effect prerequisite corrected

The chair refusal was a service bug: normal players legitimately gain their
status container on the first effect. The focused fix now has70 native passes
and ordinary unchanged108 Sit/Stand replay, captured with actual reservations
and PlayerSat/PlayerStood diagnostics. See
[the scoped correction](GRAPHICS-FURNITURE-FIRST-EFFECT-FIX.md). Saved109 and
the isolated session are preserved; no new furniture sprite/area acceptance.
Next: actual baseline/concept parts review and immutable two-frame integration
through world/examine, paired refusal tests, after/restart and other-region
controls. Keep straight walls; layered shaded walls/quiet floors guide later
biome design. Shared-storage/cave preservation77c8bfbe9 is verified onmain.

## Implemented coverage and ordinary comparison — 22:59 UTC

The shared companion now reuses Bed frame2 and Chair frame3 from the unchanged
East atlas in current world graphs. All four Greybox world/subject entry points
honor known-role refusal. The narrow East borrowed-atlas accessor preserves
explicit injection without calling the old weaker resolver. Two cached views,
no new art/resources, no service calls, no terrain or collision changes and no
regional admission expansion. The existing painter supplies one Render cue.

Native RED `69541e08e01145eda0a93c42a13c4b34` failed both new public body cases
before production. They passed after integration. Final expanded evidence has
33 focused passes and 395 distinct passes across focused/related suites;
three existing Sodden allocation probes are inconclusive because their positive
counter did not detect allocations. They are not passes. Zero failed tests.
Initial two GREEN cases overlap the final33 and are not added twice. Tests cover
submitted world/authorized reader, semantic/refusal/resource counters, actual
NPC reservation and cleanup, disclosure, changed Render cue, removal and lifetime.
The standalone-named fixture is manager-attached, so it does not independently
establish the manager-null case. Existing source authority is unchanged.

Actual ordinary Play used unchanged earned107, then saved seated110 and released111:

- Sodden15.7.0: same50,17/HP38 viewpoint; real Bed12657 at47,19 and
  Chair12666 at55,19 use native bodies. Both real readers match those bodies.
- Real bed rest healed2HP and advanced60 ticks, releasing its reservation.
  Adjacent chair use rejected `not_on_seat`; same-cell Sit/Stand/Sit succeeded.
- F5 saved chair12666 occupied by player2695 at55,19/HP40/turn2500.
  Fresh Continue restored the same SittingEffect/furniture/occupant identity;
  a real Left move to54,19 released it. F5 saved earned111 atturn2510.
- Unchanged earned East kitchen12.11.0 control shows Bed13760 at25,8 and
  Chair13761 at25,7 with the same source rectangles and both actual readers.
  This is a second-region checkpoint control, not a new cross-region journey.
- All three isolated sessions were preserved byte-for-byte before Finish.
  Root verified Playfalse, save-root null and scene clean after restoration.

See `shared-furniture-after-play.json`, diagnostics, session preservation,
matched comparison, profile and independent review in `Verification/Live16x24`.
Fifteen actual captures include rejected02(Skills) and05(stone-floor control),
which are not furniture evidence. Early batched input also cast En Garde;
its real consequence was retained instead of editing state. Individual key/menu
observations resolved the test-control issue. Actual03/06 are the real readers.

### Comparison, performance and acceptance limits

Before02/after01 share camera/player position andHP, but differ in3274 of564000
world pixels, including subtle changes beyond the two furniture cells. They
are not an exact tick/animation freeze; no furniture-only pixel claim follows.
Actual seated save10/fresh Continue11 match all564000 world pixels exactly.
The approved source/resource/editable/native metadata pins remain unchanged.

An Editor recorder ran22:50:36–22:52:53 over movement, readers/rest and tool/idle
intervals. Its rolling10000samples can omit earlier activity: retained LateUpdate
max2.573ms and Sidebar max2.430ms do not establish sustainedFPS or an improvement.
RenderZone recorded zero in that retained window, not a claim of free redraws.
The three inconclusive allocation probes also prohibit an allocation guarantee.

Independent source/test/actual review found no significant furniture regression.
The Sodden room remains broad/sparse with weak nighttime contrast; East fields
also retain sparse/mixed coverage. No layout, biome or area-polish acceptance
advances. All55 world inventory and five Stump polish states stay unchanged.
Current packages still contain shared-storage d9f; fresh furniture packages,
Mac playback/restart and Linux archive checks remain the next delivery gate.
Optional animation and parked cave work remain deferred.

### In-phase self-review

- 🔵 Confirmed API correction: ProfilerRecorder has no Description property in
  this Unity version; the read-only export uses its already-known marker names.
- ⚪ Explicit source reuse instead of new generated pixels is intentional and
  reviewed; the concept's lighting/resolution/UI/scale drift was rejected.
- ⚪ Catalog rejects altered role/shape/custody and conflicting services; it does
  not repair owners or promise availability. No new gameplay/diag event is added
  for pure per-cell presentation; existing paint/service receipts remain intact.
- 🧪 Whole-world coverage, area polish, sustained performance and physical Deck
  acceptance remain open. No cleanup or unrelated files were changed.
- Cold-eye symmetry/consistency/countercheck/doc pass and independent review
  complete for this bounded source slice. No significant unresolved finding.

Files: new SharedFurnitureCatalog and33-case fixture plus required Unity metadata;
modified Greybox/East borrowed routing; new concept provenance/actual evidence
and same-commit coverage records. No native pixel/source/meta changes.
