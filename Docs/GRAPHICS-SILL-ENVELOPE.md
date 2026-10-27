# Sill native static envelope — integration and gameplay record

Status: bounded Sill envelope integrated and exercised in ordinary Unity Editor
Play, 2026-10-27 UTC. Final native suite136/136 passed after the independent
hidden-ground correction; an earlier broader regression suite224/224 passed.
Actual route, both door axes, reader, interior, river approach, leave/return and
checkpoint reload evidence are retained. The current Mac package has also passed
ordinary boot/Continue, door interaction, inventory and full-process save/reload;
the matching Linux archive passed integrity review but was not executed. Sill
fullness, all Sill owners, all Spread and worldwide coverage are not complete.

This slice replaces disclosed Sill Grass, VineWall and ordinary VillageDoor
surfaces with native16×24 art, reusing accepted StoneWall, AlchemyStill and
StoneFloor pixels unchanged. It changes presentation only: no generation,
collision, turn rules, save schema, services or new animation. The original plan
was a static envelope; its bounded scope remains intact. Qud is an art/readability
reference, not a claim of source or mechanic parity.

## Input and original scope

Actual input: `Docs/Verification/Live16x24/portable-equipment-before-01-sill.png`.
The companion `portable-equipment-before-sill.json` records120 disclosed
on-screen selections: Grass80, StoneWall15, VineWall17; one each AlchemyStill,
Apothecary, LongSword, MarketStall, Player, VillageDoor, Well and WellGroundMarker.
These count selected cells, not unique entities or the whole zone.

The original PNG is player39,12 at1280×800; its receipt is player39,13, tick490,
at2321×1139. They are useful preimplementation inputs but are not a pixel-matched
before/after pair. The after-entry receipt repeats the original receipt's
position/tick/viewport and120 selections. No percentage extrapolation is valid.
Dark undiscovered space is not evidence of bare terrain.

## Pre-implementation corrections

| Initially plausible premise | Verified correction and consequence | Source |
|---|---|---|
| Extend the opening admission list to Sill. | Opening admission deliberately requires unclaimed six-scene addresses; Sill is an authored Village POI. Add an independent narrow presentation scope; do not change `IsAdmitted`. | `FlatPixelGreyboxCatalog.cs:181-200`; `WorldMapAuthoring.cs:209-212` |
| Native boundary atlas is generic StoneWall. | Connected masonry is exact `Wall` plus reference-glade material VisualIDs, restricted to the glade. Borrow cellar StoneWall frame8 instead. | `FlatPixelBoundaryCatalog.cs:58-63,102-124`; `FlatPixelOpeningArtCatalog.cs:501-504` |
| VineWall can use Hedge art. | Native Hedge requires exact Hedge. VineWall is a structural, combustible woven-stem Wall with14HP and Plant material. New regional static art is needed. | `FlatPixelHedgeCatalog.cs:77-94`; `Objects.json:8088` |
| A two-frame door covers ordinary doors. | Door has a saved aperture orientation. Two axes x open/closed requires four frames. The parts record was corrected from2 to4 before integration. | `DoorPart.cs:12-16`; `VillageDoorPlacementBuilder.cs:35` |
| Any floor inside a building may be painted stone. | Native material must follow an actual StoneFloor owner. Village interiors explicitly create StoneFloor; do not infer floor from a roof/wall rectangle or concept image. | `VillageBuilder.cs:210-223` |
| Existing ground adapter already supports generic StoneFloor. | Cellar adapter requires exact Floor and cellar scope; working-court adapter requires exact court properties. Borrow immutable pixels via a new ordinary-StoneFloor predicate, not those regional eligibility rules. | `FlatPixelCellarFloorCatalog.cs:42-74,100-128`; `FlatPixelWorkingCourtCatalog.cs:75-86` |
| Well is the glade repairable well. | Sill Well has settlement-site stages and adjacent markers. Glade RepairLinedWell uses a different Repairable recipe/boolean. Leave Sill service art for its own bounded state slice. | `VillagePopulationBuilder.cs:545-616`; `SettlementSiteVisuals.cs:45-68,135-163`; `FlatPixelOpeningArtCatalog.cs:588-592` |
| Apparent space needs more generated clutter. | The capture is mostly undiscovered; existing Sill has6-9 buildings plus five shop stamps. First expose and style real owners, then judge route/scene composition. | `VillageBuilder.cs:127-129`; `LandmarkBuilder.cs:88-95,144-150` |
| Fallback current-graph check alone proves Sill authority. | Fallback intentionally permits managerless fixture zones. Sill art must additionally require a nonnull actual WorldLocationContext manager and exact cached graph. | `FlatPixelFallbackCatalog.cs:111-116`; `WorldLocationContext.cs:14-17` |

Source prefixes above: rendering files under `Assets/Scripts/Presentation/Rendering/`; world builders under `Assets/Scripts/Gameplay/World/Generation/Builders/`; map files under `Assets/Scripts/Gameplay/World/Map/`; settlement files under `Assets/Scripts/Gameplay/Settlements/`; Objects under `Assets/Resources/Content/Blueprints/`.

## Source study and immutable art mapping

`ArtSource/Live16x24/concepts/sill-envelope/{prompt.txt,concept.png,parts-and-decisions.json}`
retains the built-in imagegen reinterpretation, actual input, opening style input
and external woven-material reference. It is a concept, not gameplay evidence.
Invented walls tracing fog edges, extra flowers/resource implications and bright
bevels were rejected. Geometry comes only from real disclosed owners/adjacency.

The first native VineWall study read too much like mottled masonry/hedge. The
reviewed revision uses explicit winding living strands and sparse attached leaves.
`Tools/Live16x24/build_sill_envelope.py`,
`ArtSource/Live16x24/sill-envelope/sill-envelope.blend` and `source-cells.json`
preserve editable integer geometry, named parts and palette. The atlas was
rendered from that source; it is not a crop of the generated concept. The source
README retains its authoring-stage status; this document records later integration.

The unchanged accepted candidate is now imported at
`Assets/Resources/Sprites/Live16x24/sill-envelope.png` with its required `.meta`:
384×24,24 native16×24 cells, Point/Clamp, no mipmaps or compression. Foreground
has binary alpha; all four ground cells are opaque. Source verification reports
zero source/contact pixel mismatches,24 distinct cells,10 negative mutation
counters, independent decoded-RGBA rebuild equality, saved-Blend checks and72
unchanged approved runtime PNG/meta input hashes. These are authoring checks,
separate from native and gameplay evidence below.

| Atlas frame | Owner/state | Mapping |
|---|---|---|
|0..15|VineWall|Cardinal mask N=1,E=2,S=4,W=8; disclosed selected eligible VineWall neighbors only. No diagonal links or links to stone/hedge/door.|
|16|VillageDoor|Axis0 closed: passage north/south, closed panel spans east/west.|
|17|VillageDoor|Axis0 open; keep actual passage visibly open.|
|18|VillageDoor|Axis1 closed: passage east/west, closed panel spans north/south.|
|19|VillageDoor|Axis1 open.|
|20..23|Grass|Quiet low-contrast opaque substrate variants, no harvest/loot/extra object claim. Stable coordinate variation, no simulation RNG/time/entityID dependency.|
|Borrowed `opening-cellar` frame8|StoneWall|Existing accepted pixels unchanged; exact ordinary intact solid non-takeable StoneWall only.|
|Borrowed `north-works` frame0|AlchemyStill|Existing accepted pixels unchanged; exact ordinary non-solid non-takeable AlchemyStill with owned AlchemyStillPart.|
|Borrowed `cellar-floor` frames0..3|StoneFloor|Existing quiet native stone pixels unchanged; only an actual ordinary visible StoneFloor floor. Existing sparse deterministic selection may be reused.|


Door selection is `16 + (QuarterTurns & 1)*2 + (IsClosed ? 0 : 1)`.
QuarterTurns0/2 and1/3 select the two independently authored axes; values outside
0..3 refuse the new art. Lock-derived `IsClosed` is authoritative. No24×16
rotation, hinge animation or unlocked-passage promise is added.

## Saved implementation and constraints

`FlatPixelSillEnvelopeCatalog` is one optional finite adapter, routed by
`FlatPixelGreyboxCatalog`. Exact scope is `Overworld.10.10.0`, a nonnull live
WorldLocationContext, the same cached graph, valid world-map dimensions, Spread
biome and actual Village/Sill/Villagers POI with empty/null profile. No graph is
adopted or generated. Existing six-scene `IsAdmitted(Sill)` stays false. Other
settlements, spoofed context and unrepresented regions keep existing rendering.

Foreground uses the existing selector's current visible owner. Exact identities,
owned Render/Physics parts, canonical glyphs, non-custody state, single-cell
geometry and semantic guards reject reskins, stale owners and ambiguous roles.
Intact walls require real solidity/structure; doors use actual owned DoorPart and
exclude separate repairable, Morrowfast and sealed-library barriers. Separate
Sill output storage avoids overwriting a successful portable-equipment tile.

Ground comes from actual ordinary Grass/StoneFloor, independently of a covering
actor/item. It never infers an interior rectangle or path. Actual StoneFloor
outranks lower Grass; duplicate/invalid actual floor owners refuse substitution.
Foreground is cleared only when that selected owner is the actual floor.
Cultivation, active tile state and valid visible competing liquid/gas, terrain,
marker or trap owners preserve their material priority. Ground variation is
stable coordinate-only selection: mostly the quiet first cell, occasional three
alternatives, without simulation RNG/time/entity-ID input.

Independent review found and fixed one P2 disclosure defect before acceptance:
a hidden or invalid competing owner could previously erase native ground. The
new `VisiblePhysicalOwner` check requires owned visible Render, owned Physics,
no carried/equipped custody and a positive owned stack when present. Current
spatial placement is separately checked. Hidden/foreign/custody marker or trap
entries now leave visible ground unchanged; valid visible ones still veto it.
Malformed actual floor owners continue to reject conservatively.

Vine connections require visible, explored, selected eligible cardinal VineWall
neighbors. Hidden, covered, destroyed, non-solid or different owners do not
extend a strand. Existing dirty-neighbor machinery updates broken runs and end
caps. The new envelope is deliberately visible-only: remembered cells keep the
existing dim glyph/fog policy and do not query hidden door/plant state. This
visible-to-remembered style change remains a declared presentation limit.

The authorized reader path can reuse these sprites only for the current selected
visible world subject; VineWall uses isolated mask0. The adapter grants no examine
document access. Missing/malformed resource groups independently fall back; an
injected missing group never reloads Resources. Cached sprites/tiles are owned
and disposed, while borrowed textures and selector remain untouched. No general
biome registry, new serialization or per-render sprite construction was added.

## Native verification and independent review

All raw XML paths below are under `Docs/Verification/Live16x24/`. Root executed
the native runs; the independent reviewer read the retained results. Suites
overlap and must not be summed as unique coverage.

| Retained run | Result | Meaning |
| --- | --- | --- |
| `sill-envelope-native-red.xml` |43 total;6 pass,37 fail|Public renderer assertions demonstrate Sill gaps before production code.|
| `sill-envelope-native-green.xml` |82/82 pass|Initial50 Sill cases plus32 HedgeContinuity cases.|
| `sill-envelope-native-regression.xml` |224/224 pass|Boundary60, CellarFloor42, ExaminePortrait42, GlobalPresentation52, WorkingCourt28; before the later narrow disclosure fix.|
| `sill-envelope-hidden-ground-native-red.xml` |52 total;50 pass,2 fail|Same-owner hidden-ground counters fail for WellGroundMarker and SpikeTrap before correction; genuine substitution failures, not fixture/compile errors.|
| `sill-envelope-final-native-green.xml` |136/136 pass|Final Sill52, HedgeContinuity32 and GlobalPresentation52 after the saved guard correction.|

Tests cover exact scope/POI/current-graph pairs, owner aliases/reskins/part
ownership, all16 vine masks and missing/hidden/covered neighbors, both door axes
and open/closed/lock truth, actual floor under foreground, material/hazard
precedence, resource injection/lifetime, selected reader authority and stable
simulation state. The destruction counter exercises actual neighboring vine end
caps, traversable gap, unchanged full-pass count and a quiet follow-up step.
Hidden-ground tests reuse one owner through hidden→visible→foreign Physics→
restored→carried/equipped→foreign Render→restored→removed states; the positive
visible veto and negative hidden/invalid cases are paired.

`sill-envelope-independent-review.md` records the source audit and P2 fix. No
additional significant source finding remained in that bounded review. Native
assertions support state/selection behavior; they do not establish whole-scene
polish or all-biome coverage.

## Actual ordinary Play and capture record

Root exclusively owned Unity. The isolated session continued the earned seed65
checkpoint through ordinary controls, without position/state injection or reveal.
The player walked Sill, used generated doors, entered a real StoneFloor room,
approached the river, travelled away and returned. Natural SootGremlin/NPC combat
continued; HP40→39→38 is visible, not a presentation fixture reset.

All following PNGs are actual1280×800 Unity captures in
`Docs/Verification/Live16x24/`, not concepts or composed contact sheets.
Observational JSON reads existing paint receipts, current tilemap sprites and
active bodies without resolver calls or world mutation. It classifies disclosed
on-screen selected cells only, not covered owners/unique entities/quality. All
listed retained receipts have zero unmatched entries. Most receipts report the
live editor Screen2321×1139 while PNG export is1280×800; the bank receipt reports
1280×800. Do not claim all pairs are viewport-identical.

| Capture suffix (prefix `sill-envelope-after-`) | Observed evidence |
| --- | --- |
|`01-entry`, `02-lane`, `03-woven-door`|Native masonry shop shell, winding plant walls, grass substrate and actual lanes. Entry receipt is tick490,39,13; woven-door receipt tick700,57,13.|
|`04-door-menu`, `05-covered-door-reader`|Ordinary interaction/reader surfaces; a covered subject must not gain a native portrait from an unselected lower owner. These are not extra art-coverage claims.|
|`06-east-door-open`, `07-east-door-closed`|At70,11, open door tick850 and closed tick860. The east/west passage changes visibly; focus action changes close→open. Subsequent bump opens without moving on that first action.|
|`08-interior`, `09-stone-floor`|Entry71,11 tick880, then72,12 tick890 on actual StoneFloor with HP39. Native vine room perimeter and quieter stone floor; furniture/actors remain glyphs.|
|`10-north-door-closed`, `11-door-reader`|At67,11 tick1000, HP38. The other door axis is visibly closed; its examine panel shows the matching closed sprite and latch text.|
|`12-river-approach`|**Pause menu at67,11, not the river.** Key timing misstep retained accurately; similarly named JSON remains tick1000 and proves no new route progress.|
|`13-river-approach`, `14-river-bank`|Settled approach67,16 tick1050, then bank67,18 tick1070, HP38. `after-river-approach-settled.json` and `after-river-bank.json` are the corresponding observations.|
|`15-glade-countercheck`|Travel into the existing opening glade; bounded visual countercheck, not a new six-scene sweep.|
|`16-revisit`|**Cellar, Overworld.11.10.1 at40,12 tick1250; not a Sill revisit.** Existing native cellar material remains visible.|
|`17-sill-revisit`|**WorldMap at40,13 tick1300; not Sill.** Its map-cell glyphs must not be counted as Sill regression/coverage.|
|`18-sill-return`|Actual return to Sill/Overworld.10.10.0 at39,12 tick1310, HP38. Native masonry/still reappear with pending services.|
|`19-reload`, `20-reload-matched-zoom`|Fresh isolated load of earned tick1070 checkpoint restores Sill67,18, HP38.19 starts at1×; PageUp returns20 to2× and the earlier338 selected-cell/ground counts.|

The mismatched filenames12/16/17 are preserved as historical evidence, not
relabeled by deleting or disguising captures. Correct locations above govern
any reuse. The full20-frame run is not20 independent acceptance scenes.

Useful exact observations: after-entry has native StoneWall15, VineWall17,
VillageDoor1 and AlchemyStill1, Grass80 ground-only, the already-native Player
and LongSword, and four remaining service/marker glyph selections. The bank
has338 selected cells: Grass184 ground-only, VineWall26 native, VillageDoor3
native, Player1 native, StoneFloor1 ground-only; WaterPuddle120, Bed1,
Villager1 and WoodenBarrel1 remain glyph-fallback. Ground counts separately
include substrate under covering owners; do not add those to selected counts.

Reload at the same earned position/tick and2× produces exactly the bank's
selected-cell and ground-count maps. Live Screen metadata differs between those
receipts despite identically sized exports, so this supports restored state and
selection coverage, not pixel-identical rendering or universal save integrity.

## Scene coherence and fullness assessment

The reviewed actual lane, east open/closed door, interior, north door/reader,
riverbank and return views establish a usable first Sill material envelope.
Woven plant walls read differently from masonry shops; both door axes communicate
an opening; the real stone interior differs from surrounding grass. The earlier
approved sprites remain visible in the same flat native presentation.

**Fullness does not pass yet.** The disclosed grass lanes and bank have too much
repeated small surface texture and too little furnishing/landmark rhythm. A
coherent shell has been added, but the village still feels underfurnished in
these exposed views. Furniture, specialists and other NPC glyphs visibly clash
with the new surfaces. The river remains repeated blue WaterPuddle glyph rows
(120 selected cells in the matched bank view), a major unfinished material
family. These are concrete scene defects, not a reason to add noise everywhere.

Dark unexplored shapes and dim remembered glyph regions are excluded from the
bare-chunk judgment. They are discovery/memory presentation, not empty land to
fill. No generation-density change was made. A future composition pass must
inspect actual disclosed routes, existing furniture/props and appropriate flora,
then make a named, bounded placement/material change if still necessary. Keep
accepted art stable unless that review identifies a specific defect.

## Profile, saves and restoration

`sill-envelope-profile.json` covers ordinary Editor walking/door/terrain activity
plus idle/tool time from04:32:12.743868Z to04:38:03.722501Z:350.979 seconds and
183,368 total frames. Each recorder holds only the latest131,072 samples and is
**wrapped**; figures describe that retained tail, not every frame in the window.

| Recorder | Tail mean | Tail p99 | Tail max |
| --- | --- | --- | --- |
|`COO.ZoneRenderer.LateUpdate`|0.1068ms|0.2323ms|29.5109ms|
|Main Thread|1.9484ms|5.7004ms|2505.7717ms|
|Draw Calls Count|35.706|39|39|
|Batches Count|35.331|39|39|
|Triangles Count|9395.071|11481|11481|

These are bounded Editor observations with tool/idle outliers, no controlled
before/after baseline and no isolated attribution to the new adapter. They do
not prove a speed improvement, standalone performance or physical Steam Deck
acceptance. No additional batching framework was introduced.

`Fixtures/sill-envelope-after65/{Quick.json,Quick.sav.gz,README.md}` under the
verification folder preserves the earned67,18 tick1070 HP38 checkpoint. The two
owned session roots were archived at `/tmp/coo-sill-envelope-play-root` and
`/tmp/coo-sill-envelope-reload-root`. `sill-envelope-restoration.json` records
stopped Play, SaveRootOverride=null and a clean SampleScene;38 foreign baseline
files, normal saves, standalone preferences and previous current builds remain
unchanged, with editor gameplay preferences restored. Root verified these
protection checks. Disk inspection reported411GiB free. No cleanup deletion was
performed; storage reclamation remains inspection-only.

## Current packaged delivery — 2026-10-27 04:55 UTC

Current source: `246bda61e67d58d147e87e781737f32c54d25aa2`, verified on
origin/main by the root operator. Fresh Mac build succeeded with0 errors,
5 warnings in34.18s; Linux succeeded with0 errors,4 warnings in28.70s. Build
success is not a zero-warning or execution claim. Source-to-binary provenance
is asserted by the root build operator, not independently reproduced.

- Mac: `Builds/Verification/2026-10-27-sill-envelope-246bda61e-current/CavesOfOoo.app`.
- Linux archive: `Builds/SteamDeck/2026-10-27-sill-envelope-246bda61e-current/CavesOfOoo-SteamDeck-flat16x24-current.tar.gz`.
- Archive:128,678,885 bytes,182 regular files; SHA256
  `6994a7ebbc92f6521a3ea1192c6cf72c32a22b24cf5b3dac64dbbec7f1a918dc`.
  Every file hash/size/mode, directory mode, checksum list, credits/licenses and
  full gzip footer were checked independently. Linux was not executed.

The [Mac play summary](Verification/Live16x24/packaged-sill-envelope-mac-play-summary.json)
records12 actual native-window JPEG captures00..11, each1280×856 including the
1280×800 game and56px titlebar. Ordinary launch uses flat presentation. Continue
loads the earned tick1070 Sill checkpoint at67,18, HP38. PageUp selects2×; seven
north moves reach67,11 while a resident opens the real north door naturally.
`C`, Up, `O` closes67,10; `C`, Up, `X` shows its native closed-door portrait.
An Up bump opens it without moving; the next Up steps onto67,10. `I`, Tab shows
the existing native LongSword portrait,1d8 damage and equipped `[E]` at the far
right. This verifies the portable sword mapping, not all four portable families.

F5 saves tick1170. A confirmed full process quit, relaunch and Continue restore
67,10/HP38 and the same earned state. The reloaded view starts at1×; PageUp makes
the final2× comparison. The earned final checkpoint is retained at
`Docs/Verification/Live16x24/Fixtures/packaged-sill-envelope65`. Screenshots05,
08 and11 provide the door-reader, inventory and matched-zoom reload views.
No gameplay state, image pixels or routes were patched to manufacture them.
Known premature-thread-finalization and ComputeBuffer shutdown notices remain.

The [restoration receipt](Verification/Live16x24/packaged-sill-envelope-restoration.json)
records all49,791 tracked snapshot objects matching after five reviewed generated
file restorations;38 original dirty files remain preserved. Normal Editor saves
and gameplay preferences match, the full standalone preference map is logically
restored, and standalone Saves absence is restored after archiving the owned
test saves. Previous current builds/archive are unchanged. This is a bounded
snapshot comparison, not an atomic filesystem freeze or plist byte-identity
claim. No cache/storage cleanup was performed.

Previously recorded final Sill136/136 and portable-equipment222/222 suites share
52 identities (306 distinct final-suite identities). The earlier224/224 regression
is separate and overlaps; these counts must not be added as unique new tests.
See [archive receipt](Verification/Live16x24/packaged-sill-envelope-linux-archive.json)
and [independent package review](Verification/Live16x24/packaged-sill-envelope-independent-review.md).
No new native tests were run merely for this documentation update.

Can verify: ordinary Mac launch and earned load, bounded Sill material/door
interaction and portrait, native sword inventory/stats, save and full-process
restart continuity. Cannot verify: whole Sill fullness or all sprites, all-biome
completion, every animation, Linux execution, physical Steam Deck or sustained
hardware performance. River blue-wave rows, furniture/services/NPC glyphs and
repetitive sparse disclosed grass lanes remain explicitly unfinished.

Last hourly report remains04:25UTC; next05:25UTC. This is a package milestone,
not an additional hourly report. Continue with the Sodden actual-play baseline.

## Self-review, remaining gates and next slice

- 🟡 **Fixed:** hidden/invalid competing markers/traps could change visible ground.
  Genuine two-case RED preceded the narrow ownership/disclosure guard; final GREEN
  includes both same-owner counters.
- 🧪 **Unfinished graphical coverage:** water, service state families, NPCs and
  furniture remain visible glyphs. This packet is not a completed Sill release.
- 🧪 **Limited evidence:** visible-only memory, unobserved Sill owners/seeds, other
  settlements/biomes, Linux execution and physical Deck remain outside this
  acceptance. The bounded Mac package route below is now verified. Existing224 regression preceded the final narrow guard; final136
  reran its affected Sill/global/dirty-neighbor suites.
- ⚪ **Composition follow-up:** actual exposed lanes/bank are repetitive and
  underfurnished. Their fullness gate remains open; do not count fog as bare land.

Next scoped coverage work is the actual Sodden crossing16,7 and shelter15,7
static-material baseline and production cycle, continuing the user’s worldwide
sprite-coverage priority. The disclosed Sill water/river material family and
service/furniture/actor identities remain open; their later pass must re-evaluate
the same actual lanes/interiors/bank for coherent grouping and readable routes.
Well+WellGroundMarker requires its settlement-site stages, not the glade repair
recipe. MarketStall, Apothecary/other specialists, Bed/Chair/Chest/WoodenBarrel,
Shrine and other unrepresented owners stay explicitly pending. Road/bridge and
unobserved site families also require inventory and real-view checks. All-biome
coverage continues under `Docs/GRAPHICS-WORLD-SPRITE-COVERAGE.md`; no subtle new
animation is a prerequisite for these missing sprites.

Root verified source `246bda61e67d58d147e87e781737f32c54d25aa2` on
origin/main before the current package builds. The package receipts below now
record that delivery; final staging/push of these documentation and evidence
updates remains root-owned. Capture sharing is separate from this document update.

## Files and evidence for this slice

- Runtime: `Assets/Scripts/Presentation/Rendering/FlatPixelSillEnvelopeCatalog.cs`
  and `.meta`; narrow `FlatPixelGreyboxCatalog.cs` routing.
- Runtime art: new `Assets/Resources/Sprites/Live16x24/sill-envelope.png` and
  `.meta`; borrowed accepted atlas pixels unchanged.
- Native tests: `FlatPixelSillEnvelopeTests.cs` and `.meta`; added actual-vine
  destruction case in `FlatPixelHedgeContinuityTests.cs`, under the existing
  EditMode presentation/rendering test folder.
- Editable art/study: `ArtSource/Live16x24/sill-envelope/`,
  `ArtSource/Live16x24/concepts/sill-envelope/`, and source build script above.
- Actual evidence: `Docs/Verification/Live16x24/sill-envelope-*`, the earned
  checkpoint fixture, independent source review, and this integration record.

The portable-equipment fix is separately owned and documented; its existing
art is reused rather than counted as new Sill assets.
