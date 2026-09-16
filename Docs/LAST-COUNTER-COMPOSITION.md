# Last Counter — an occupied limit

Status: complete and installed for fresh native generation. TC17 passes280/280 targeted checks. TC20 full suite: 14,042 passed,32 unchanged baseline failures,0 C# errors and no new failures. All280 added cases pass. TC21 reproduces all166 final asset/metadata files byte for byte; TC22 confirms installation and no task GUID collisions. CoO-original composition and art; no Qud-parity or live-FPS claim.

## Authority and shape

Exact fresh Overworld.18.18.0 under current Village/ConcordPost map authority.
Native Beating tier3, no mapped road or river. The scoped generic bottom river
is omitted; this is a dry frontier post. Display name is not routing authority.
Neighboring abandoned counters at19.18.0 and19.19.0 remain unchanged.

Three unequal masonry buildings cluster west of the disclaimer: SupplyPost,
RestShelter and WorkersHouse. UpperSupplyCourt, LowerSupplyCourt and SteppedPost
change their relative roles and loading-yard relationship across seeds. Two
unequal open masonry returns frame three actual loading Crates. Four interior
supply Crates distinguish work from the shared two-bed/two-chair resting shelter.
The actual resting fire and private household bed retain their native actions. The eastward approach opens into deliberately
unoccupied space; cells x62–79 exclude initial residents and drama. That is a
generation reservation, not an invisible movement barrier. The sign does not
create a world boundary or a delivery simulation.

Floor remains outdoor native ground. StoneFloor marks true shaded interiors.
No RoadStone or new authored road claim is introduced. Local reserved paths
connect four borders, room doors, native main well40,12 and each profile owner.
Two-cell route shoulders permit passing where the existing owners allow it.
Seed variation selects three different semantic arrangements, then adjusts room
positions/widths and the public disclaimer. Three western shoulder colonies
contain up to eighteen DryBrush and six Rubble owners, avoiding interiors and
protected approaches. Nothing is placed in the quiet eastern field.

## Native functions and false-premise corrections

- Exactly one late SaccharineEnvoy, LastCounterSign, stocked Chest and Campfire
  preserve the original guaranteed post. Generic village passes can add their
  own chests and fires; exact four-owner assertions apply to the profile stage.
- Envoy has native SaccharineEnvoy_1 conversation, Trader EnvoyStock, starting
  purse and ordinary restocking. It is not Cinderhold's pruning-contract Factor.
- The chest uses actual CampGoodsT1 and ContainerPart. Stage rolled supplies
  before publication. Respect already-stocked envoy inventory instead of
  rerolling its initial shelf when ObjectCreated already populated it.
- Sign is a solid examinable PhysicalObject. It has no Material or Destructible
  Part; art must not promise new sign breaking or a special menu.
- Campfire retains native Fuel, LightSource, Thermal and Campfire Parts. Its
  rest action remains subject to RestSystem rules; no safe-zone promise.
- No god, caravan transport, frontier barrier or guaranteed delivery mechanic
  is added. The older disclaimers remain descriptive history.

## Staging, lifetime and integration

LastCounterCompositionPlan.Create exposes bounded GroundAt/ObjectAt,
IsInterior/IsApproach/IsReserved, read-only Rooms/Profile, FormationName and Signature.
Invalid addresses throw; invalid coordinates return null/false.

Base1000 validates all required early and late native owners, plus initialized
CampGoodsT1/EnvoyStock dependency tables, before staging and committing terrain.
It publishes plan, shade and reservations only after success. Late3860 stages
all four owners and actual stock once for the exact realized Zone reference.
Populated/foreign reuse rejects; an explicitly cleared same-instance retry can
build fresh owners. Removing the chest never causes profile restamping.

CanPlaceCaveEntrance checks the full live3×3 footprint: clear, dry, exterior,
unreserved, same-zone/cell identity. Arrival3870 reserves actual stairs and
open neighbors before generic population. Root owns shared routing, service
reservation opt-ins, scope and rendering integration. No shared file is owned
by this module.

Success and rejection diagnostics use LastCounterCompositionPlanned/Rejected,
LastCounterProfilePlaced/Rejected, LastCounterArrivalsReserved/Rejected.
No generation work enters frame or turn loops; current entity owners and
existing render dirty hooks remain authoritative for visible lifetime.

## Verification and honesty

Core/dedicated adversarial cases cover scope, deterministic variation,
west/east seeding distinction, dry connectivity, real stock and owner Parts,
final generic services, missing/failed dependencies, no-replay and actual live
cave footprint controls. Tests load/reset native loot tables explicitly.
TC17 passes the targeted native and renderer gates. The full regression and
rebuild/installation receipts remain pending.
Static previews cannot establish live input feel or sustained frame timing.

## Implementation log

- Read CLAUDE and aggregate plan, surveyed source and native census. Corrected
  Last Counter to current tier3/no road, identified sign's nondestructible
  policy, native EnvoyStock trader and CampGoodsT1 chest separately.
- TC02: root captured30 actual missing plan/builder compiler errors before
  production. Implemented owned plan/base/profile/arrival and initial tests.
  No Unity execution or shared file edits from this module.

- TC11: independently viewed all three initial LastCounter camera images.
  Confirmed uniform rooms and insufficient activity/terrain definition. No
  manual scene edits were used; proposed grammar refinements and wrote tests.
- TC12: actual assertion RED276 combined cases,259 passed,17 failed, zero C#
  errors. Seven owned formation/furnishing/forecourt assertions and two missing
  new dependency cases failed as expected. Added three named arrangements,
  unequal open loading returns, native role furnishings and bounded western
  brush/rubble colonies. The exact four late profile owners, initialized stock
  checks and quiet eastern field remain authoritative. Verified in TC17.
- TC13: all three refined native views independently inspected, zero uncovered
  owners reported. SteppedPost and UpperSupplyCourt show distinct role placement,
  work goods and rest mats; the quiet eastern field remains legible. Seed1
  selects LowerSupplyCourt and is added to final native coverage/preview work.
- TC14/TC15: one service-frontage assertion failed at a generic Campfire(17,24).
  Diagnostics showed playable Floor neighbors(16,24)/(18,24), masonry north and
  world boundary south. The test had reused FormationReachability's intentionally
  interior-only repair flood. Native Zone.InBounds includes row24; successful
  MovementSystem movement reaches that row, and InputHandler requests a zone
  transition only after an out-of-bounds movement failure. This was a test
  premise error, not an unusable fire. Replaced the fixture flood with complete
  80x25 cardinal traversal and added actual boundary movement plus campfire
  rest execution, with real blocked-standing-cell controls. No production
  geometry or boundary behavior changed. Both boundary cases passed in TC17.

- TC16/TC17 close-out: root reports eight final views with no missing native
  models. This module independently inspected all three TC13 LastCounter views
  plus TC16 LowerSupplyCourt seed1; all three arrangements now have a camera
  review. TC17 fresh XML confirms23 core and36 adversarial native cases pass,
  within280/280 combined targeted cases and zero C# errors. Four final-service
  seeds1,64,1729,729490642 pass; the base sweep covers0–31 plus both int extrema.
- 🧪 Boundary-fire classification: the two new player-flow cases are pins of
  already-correct native movement/rest behavior. Positive walks the actual
  route to(16,24), gathers the real fire action and heals while advancing60
  ticks. Negative closes both standing cells with real walls and proves no
  route/frontage exists; it does not falsely claim RestSystem itself enforces
  distance. Source: Zone.cs510–512 includes all boundary cells;
  MovementSystem.cs41–49 accepts in-bounds targets; InputHandler.cs734–785
  transitions only after failed out-of-bounds movement. No production fix.
- Independent final test review found no obvious vacuous positive: stock
  purchase is paired with no funds, fire rest with a real nearby hostile,
  boundaries with blocked standing cells, profile identity with wrong type/
  address and canonical restoration, and live cave clearance with adding and
  removing an actual WaterPuddle. Shared actor/mesh lifetime remains covered
  by the pair's renderer fixtures. No broader or exhaustive seed claim.


## Final integration and verification

The pending gates in the chronological entries above are closed. The first full run TC18 exposed eight older controls which still treated these now-converted sites as unconverted; assertions were retained and their scope fixtures corrected. TC19 verifies all288 selected new and corrected legacy cases before the final full run. TC17 passes280/280 cases:82 FirstTent native,59 LastCounter native,81 imported-art and58 owner-driven rendering checks. This includes96 native and24 rendering dedicated adversarial cases. TC20 adds no regressions to the verified baseline: all280 new cases pass; the existing32 failures retain identical test names and failure messages. Four old negative controls were readdressed to their still-unsupported depth1 counterparts, with none discarded.

[Final eight-view gallery](Verification/VoxelWorld/TC16-final-preview/index.html) shows four world seeds per area, collectively covering all three formations each. Every actual native graph has zero missing meshes and zero unmodeled visible owners. Independent visual and shared-source reviews are complete. The boundary-fire assertion was corrected using actual native walking and campfire-rest execution; it was not a terrain bug. The seeded FirstTent frontage collision and backward LastCounter sign were confirmed and fixed during development.

TC21 reproduces all40 models /166 asset and metadata files byte-identically, then TC22 installs those exact resources and audits GUID uniqueness. The source freeze verifies2033 source/content/assembly/meta inputs in the original and isolated project, unchanged throughout the final full run and rebuild. [Close-out evidence](Verification/VoxelWorld/TC22-closeout/README.md) records the complete case comparison, artifact hashes, integration delta and source audit.

Fresh native chunks at Overworld.5.17.0 and Overworld.18.18.0 receive these rules through the real world manager. Existing graph instances are preserved. Current spawn,1.2x camera and full-reveal preferences are unchanged. Original Unity was not restarted. Static previews establish composition and coverage; live input feel, animation and sustained FPS are not inferred. Existing mixed source files remain installed; only this phase's exact delta is recorded in the scoped implementation patch, preserving unrelated work.
