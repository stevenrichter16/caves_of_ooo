# G2 opening ground — pond banks, routes and truthful field soil

Status: integrated and bounded ordinary Play accepted after masonry `e6b6609f1`;
wet prepared soil has native tests but its ordinary-play gate remains open. Graphics remains the
project's primary release blocker. This is one scene finishing family within
G2, not an opening-release or regional-coverage claim. Root owns Unity.

## Goal and production loop

Use the real playable opening captured in
`Verification/Live16x24/g2-normal-start.png` as the unchanged greybox. Preserve
approved characters, walls, geometry, camera, UI and interactions. Improve the
three named ground defects: the pond is an unbounded flat color, traversable
paths lack material definition, and fixed field soil disappears under occupants.

Built-in imagegen reinterpreted that actual capture in
`ArtSource/Live16x24/concepts/g2-ground-paintover-v1.png`; exact prompt is adjacent.
A second parts extraction (`g2-ground-parts-v1.png`, adjacent prompt) isolates
water/bank/path/soil motifs. These are design references only, not game captures
or production pixels. Retain thin muted water-side edges, low broken path marks,
and quiet soil. Reject their soft gradients, glow, altered text/scale and
non-native grid. Rebuild exact native cells; never crop or downsample the concepts.
The reference library's R01 read hierarchy and R07 irregular shoreline/open
passages inform this pass; no copied assets, tropical roots or new downloads.

## Pre-implementation verification sweep and corrections

Source audits were read before production edits:
`FlatPixelGreyboxCatalog`, `FlatPixelFallbackCatalog`, `ReferenceGladePlan`,
`ReferenceGladeBuilder`, current blueprint definitions, crop/fieldwork services,
`ZoneTileState` and reaction rules. Independent audit notes are incorporated here.

| Premise or risk | Verified correction | Decision |
| --- | --- | --- |
| Pond is a water-source owner | Builder writes51 initial permanent water coatings; Grass remains underneath. SpreadDrawPoint36,9 separately holds3drams. | Select actual current coating; never original pond coordinates or replenishment. |
| Permanent water cannot change | Freeze/steam consume it; ice can melt to temporary water. | Removal/other liquids/ice retire clean-water art. No automatic regeneration. |
| Missing water neighbor means shore | Fog, invalid cell and other liquid do not disclose dry ground. | Only current visible explored coating-free cardinal neighbor supplies a dry-edge bit. |
| All field rows grow |17CropRow and3RipeCropRow are fixed scenery/finite FieldHarvest, not CropPart. | No invented growth/moisture; preserve existing ripe/stubble foreground. |
| Soil follows foreground | Current GroundOf checks top field owner; cover can erase fixed-row soil. | Demonstrate RED with factory row and player/item cover; fix current lower-row lookup. |
| Every crop implies a prepared bed | Drawgourd's Grass has CultivatedSoil; other seeds may use plain Plantable ground. | New furrows require actual eligible marker. Generic crop colors remain fallback. |
| Cultivated Grass has empty visual metadata | Opening Grass uses reference-glade-ground and authored glyph variants. | Allow that exact known class contract; don't reject real beds or authorize arbitrary reskins. |
| RoadStone is repairable masonry |25initial RoadStone owners have Terrain/Physics but no material, durability or repair part. | Flat quiet paths, no raised blocks or damage states. |
| Wetness persists after crop harvest | Moisture is held by removed CropPart, not the soil. | Empty prepared bed becomes dry unless real coating remains. |
| Another overlay or observer is needed | Existing background claims and dirty-neighbor expansion already support changes. | Reuse GroundTile/GroundColor, keep reserve cords and hazards independent. |

## Bounded source and runtime contract

One optional cached `FlatPixelOpeningGroundCatalog`, borrowed source texture and
strict fallback selector; owns only its21tiles/sprites. Atlas336×24, one row of
16×24 fully opaque frames:

| Index | Native treatment | Runtime authority |
| --- | --- | --- |
|0–15| Shallow water, dry-side edge mask N1/E2/S4/W8 inset inside this cell | Sole live exact water coating at current visible opening cell; disclosed coating-free neighbors only. |
|16–17| Two restrained flat road textures | Eligible current lower RoadStone; variation `(x+y)&1`, no RNG. |
|18| Old field soil | Eligible current lower CropRow/RipeCropRow, regardless of a covering actor or drop. |
|19| Prepared dry furrows | Eligible exact Grass/field owner with owned CultivatedSoil and valid planting substrate. |
|20| Same prepared furrows, darker wet material | Same bed plus eligible current CropPart with positive moisture. |

No new art while remembered/unexplored, including roads; old conservative memory
continues. No reading hidden moisture, cultivation or neighbors. GroundKind
keeps its existing semantic meaning for hazard policy. Clean native colors draw
white; fallback colors and memory dim remain unchanged. Mixed liquid/ice uses
the existing color/hazard path. Plain green stays quiet and unchanged.

Scope is the exact active opening graph with current manager/cell authority.
Lower owners require canonical placement, current membership, owned visible
Render/Physics, single-cell footprint, non-solid/non-takeable/no custody and
appropriate exact blueprint/visual metadata. No impersonation via display name,
stale lists or reused IDs. Existing planting permissions are not changed.
Default Greybox construction lazily loads this group; its12old injected
constructors stay resource-free. Missing/malformed atlas preserves old color.
No new ground portrait is required; actual subject portraits stay unchanged.

## Implementation and acceptance order

1. Save native covered-row RED, including submitted background and removal
   counters; implement smallest lower-row correction.
2. Save companion contract RED; author/export editable native source after
   source-validator RED. Integrate optional cached background selection.
3. Native tests: masks/disclosure, graph/owner/custody/variants, water mutation,
   bed moisture and real field actions, missing resources, lifetime/cache,
   allocation/no state writes, dirty producer and submitted layer restoration.
4. Run targeted relevant regressions. Independent source/visual review; fix
   significant findings before accepting.
5. Ordinary seed64 Play: compare same opening composition, walk pond/path,
   cover field, harvest/prepare when available; exercise a real water reaction
   if ordinary starting abilities permit. Inspect overlays/selection and UI
   return. Capture actual game output and bounded profile; restore owned state.
6. Update this record, live continuation and coverage; fetch/rebase and push
   exact owned files. Continue G2 framing/motion and connected-site gates.

## Review, evidence and limits

Root and independent review accept the actual start, pond reaction/walk and
field Harvest→Prepare route below. Native tests establish additional owner/state/
layer counters; physical Steam Deck and full scene/release remain unverified.
Generated references and source counts are not gameplay acceptance.

## Implementation log

- Plan/sweep saved before production. Prior masonry screenshots supply real
  greybox baseline; new imagegen references saved and critiqued above.
- Root reserved runtime integration/Unity. Independent agents own focused
  tests, new native source and read-only design review, respectively.

## Files

Planned: this living record; new ground catalog/tests/meta; Greybox integration;
new Ground source/export/validator/README; opening-ground PNG/meta; concept
prompts/references; actual verification receipts/captures; continuation/ledger.

### First behavior RED → GREEN

`g2-ground-covered-red.xml`: four genuine native failures (CropRow/RipeCropRow
under Player/HealingTonic), expectedDrySoil versus actualGround. No missing
resource/API gate caused these failures. The minimum current lower-field lookup
then passed all4 in `g2-ground-covered-green.xml`, including submitted background,
row-removal counter and quiet frame. Placement dirtiness in this fixture is
explicitly published by the fixture; this does not prove placement producer hooks.

Independent design review confirmed the existing renderer seam. Corrections
incorporated before adapter code: any active non-clean-water coating prevents
native soil/road fallback; prepared retained RipeCropRow outranks old-field art;
an unprepared real crop keeps existing moisture color instead of falling through
to an underlying road. Bind the marker to the exact eligible lower owner.

### Native companion RED

`g2-ground-contract-red.xml`:66cases,11passing color/injection cases and55failing
assertions because the companion API did not yet exist. Production copied only
after this native result. This is the new API contract RED, separate from the
four earlier real covered-soil failures. The atlas itself was already imported;
no missing-texture failure is represented as runtime semantic evidence.

### First integrated native run

`g2-ground-green-attempt2.xml`:68/69passed, including actual Harvest→Prepare
commands with normal/full inventory and real coating dirty-neighbor updates.
The remaining failure is a fixture exception: adding a multi-cell footprint to
an already covered road is refused by actual placement rules. Correct the setup,
not those rules. The prior attempt job was refused as tests_running; the API
job's initialization-timeout status disagrees with fresh native XML and is not
used as the assertion result. No green result is claimed for the remaining case.

Review also found a non-authored lower-field footprint edge: anchor membership
alone could paint soil at an anchor physically excluded by the body's footprint.
Add a genuine placement counter before the narrow fallback correction. This does
not justify changing multi-cell gameplay or unrelated crop/road semantics.

### Review correction RED

The corrected footprint fixture and two new actual anchor-change cases produced
`g2-ground-footprint-red.xml`:69passed/2failed, expectedGround but actualDrySoil
at a physically unoccupied field anchor. The new lower-field branch now requires
owned/current footprint contact before its soil contribution. Existing crop/road
footprint behavior is outside this narrow correction. No gameplay placement
rules changed. Independent final review and subsequent green evidence follow.

### Final tests and ordinary Play — 2026-10-24 UTC

`g2-ground-green.xml` passes71/71; `g2-ground-regression.xml` passes563/563,
including those71. There are563 distinct current passes, zero skips; these are
Unity EditMode results, not the external runner or an all-project sweep.
Independent final code review found no remaining P1/P2. The source/atlas/native
contract review and direct native-size screenshot review found no art correction
warranted by a named defect. Source and resource hash are identical.

Ordinary SampleScene N launch used private save isolation and seed64 only,
with the presentation preference absent and no forced art override. Keyboard
input supplied every move/action; no entity, health, inventory, skill, coating
or field-state injection was used. All PNGs are actual1280×800 Game output;
raw Screen fields retain the editor's2321×1139 reports without conflating them.

| Capture | Actual result |
| --- | --- |
| `g2-ground-start` | Player40,12/tick10/40HP;92 visible native ground claims:52 water,20road,20old-field. The extra live water includes separate basin projection;51 is only the builder's initial pond count. |
| `g2-ground-pond-approach` | Player38,7/tick80, ordinary road/shore approach. |
| `g2-ground-pond-frozen` | Starting Rime Grip freezes33–37,7 at tick90; five water claims retire for existing ice cue. No new ice-art claim. |
| `g2-ground-thaw-attempt` | Ember Spit sputters without a tick/state change. This is explicitly an unsuccessful attempt, not thaw evidence. |
| `g2-ground-water-walk` | Player36,8/tick120 on water. Three ordinary movement turns expire ice to dry ground;32,7 now uses east-bank mask2. No automatic water regeneration or successful melt claimed. |
| `g2-ground-checkpoint-return` | Actual F6 returns to40,12/tick10 and original pond/field state. |
| `g2-ground-field-covered` | Player46,12/tick90 stands above ripe row; old-field frame18 remains underneath. |
| `g2-ground-field-harvest-menu`, `...-harvested` | Adjacent C→Right→Enter gathers one grain, retains row with Harvested=true, frame18, tick110. |
| `g2-ground-field-prepare-menu`, `...-prepared` | Ordinary Prepare action marks that same row cultivated; frame19 and retained cut foreground, tick120. |
| `g2-ground-prepared-covered` | Player returns onto46,12; prepared frame19 remains underneath. |

Root and independent art review accepted the calm banks, low path wear, intact
foreground/HUD and truthful old-field→prepared contrast. See
`ArtSource/Live16x24/Ground/visual-review.md`. Wet frame20 remains source/native
accepted only. Full-pack overflow, moisture, mixed liquids, hidden neighbors
and footprint counters have native tests, not ordinary visual acceptance here.

The703.899s editor recorder wrapped its131072sample buffers (566309 total
frames), so its aggregate is the **retained tail only**, not a full-session
average. Tail renderer mean0.05734ms/p990.10917ms/max36.03471ms; main-thread
max841.23625ms; draw calls mean39.8206/max43. It includes active actions and
idle/tool time, no matched baseline, no GPU/standalone/physical-Deck evidence,
and establishes no speedup. Preserve maxima and wrapped status in the receipt.

A launch attempted during test cleanup was correctly refused without starting
Play. The successful retry waited for zero tests and a clean saved scene.
Stopped cleanup restores scene/settings/view/save root; all pending/isolation/
view flags are false. Console retains two Metal memoryless-depth diagnostic
messages; no C# exception was observed. Caffeinate PID79931 remains running.

### Self-review and continuation

- 🟡 Fixed: an occupant erased fixed-field substrate;4 real failing cases.
- 🟡 Fixed: the new lower-field query colored a physically unoccupied anchor;
  2 actual footprint-change RED cases, then GREEN.
- 🔵 Preserved: no gameplay placement, reaction, yield, crop growth or planting
  rule changed; native art follows actual owners/state. Accepted wall/actor
  sprites and original3D setting are untouched.
- 🧪 Deferred ordinary evidence: wet frame20, successful melt/steam, full-pack
  spill, physical Deck and complete opening/connected-site release.
- ⚪ Existing interaction issue observed: C+period while standing on a field
  describes the row but offers Player actions; adjacent C+Right offers Harvest
  and Prepare correctly. This is target selection, not a ground-art failure.
  Record for a later focused interaction fix rather than claiming it solved.

Next: close G2 active framing/motion/combat acceptance and remaining opening
coverage, then the plot/works product ordinary gates and east/kitchen journey.
Do not add disconnected sprite families or reopen these accepted pixels without
an observed defect. Graphics remains the primary blocker and work continues.

Raw native XML retains Unity stack-trace whitespace in the rejected fixture
receipt. Import metadata is normalized without changing YAML values.
