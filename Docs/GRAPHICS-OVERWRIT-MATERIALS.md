# Overwrit native ground, shoots and rim objects

2026-10-27. **Current pass: source00959edf0 is verified on origin/main;
eight native cells/four owners, final533 native passes and bounded Editor
material/state/color replay are delivered in fresh Mac/Linux builds. Seven actual
Mac captures verify native readers, local movement/save and full restart;
restoration and independent Linux archive review passed. Delivery/evidence
commit587837b is verified on origin/main.** Whole-region fullness, crop/soil/stair coverage and natural
waymarker observation remain open. Root owns Unity, builds and Git. Historical
plans/failures are retained below and superseded by dated verification records.

Graphics completeness remains the user's main blocker. This is a bounded
static coverage slice across ordinary Overwrit, followed by Stump and the
observed Grovelands actors, within the continuing all-biome/all-area effort.
Keep accepted art and nuanced animations stable; do not make new animation a
prerequisite for missing body/material coverage.

## Real baseline and production inputs

The [before play receipt](Verification/Live16x24/overwrit-before-play-summary.json),
[owner census](Verification/Live16x24/overwrit-before-owner-census.json) and
[independent before review](Verification/Live16x24/overwrit-before-independent-review.md)
record seed65 ordinary keyboard travel from earned packaged Grovelands turn4590,
source `ab26e270cb376253c971a41c3449ecbbf4171444`. All eight inspected current
cached graphs report no saved lair and retain HP38/40.

| Actual area | Formation | Current shoots | Additional recorded owners |
| --- | --- | ---: | --- |
| 0,9 | GreenRim | 31 | StairsDown; two HollowchimeCrop and patch record |
| 0,10 | Blank | 8 | No other regional object |
| 3,10 | PilgrimageRim | 24 | No furniture |
| 4,11 | PilgrimageRim | 28 | No furniture |
| 4,12 | PilgrimageRim | 30 | Bench76,18; two MargincressCrop and patch record |
| 3,13 | PilgrimageRim | 23 | No furniture |
| 3,14 | PilgrimageRim | 32 | Bench75,5; two BinderrootCrop and patch record |
| 0,13 | DryRim | 27 | No other regional object |

Each census has2,000 OverwritGround owners and one player. Actual ground/growth
and the approached bench currently use fallback. At4,12, walking to75,18 and
C/Right/X inspected bench76,18: **inward-facing bench / Object / Wood /
Structure12/12**. The second bench is census-only, not visually approached.
**No waymarker appeared in any of the five natural PilgrimageRim candidates.**
Its source/native fixture coverage must remain separate from natural observation.

Eight retained `overwrit-before-01..08` PNGs are actual1280×800 game captures
covering all four formation roles, including bench approach/reader. The viewport
shows about59 world columns, not a complete80×25 panorama or one full capture
per visited area. Earned fixtures under `Verification/Live16x24/Fixtures/` are
`overwrit-blank-baseline65` turn4660, `overwrit-bench-baseline65` turn5260 and
`overwrit-dry-baseline65` turn5460. They preserve the normal-input branch.

Built-in imagegen used the actual bench approach, approved Grovelands gameplay
style and a narrowly selected NPS shoot/ground reference. Exact inputs, credit,
prompts and hashes are in `ArtSource/Live16x24/overwrit-materials/references.json`.
The [critique and parts map](../ArtSource/Live16x24/overwrit-materials/concepts/critique-and-parts.md)
reject v1's repeated floor noise and oversized bench. V2 supplies only a quiet
material hierarchy and compact shoot direction: reject generated gradients,
resampled player/UI, wrong placements, oversized bench and flower-like tips.
Native shoots must have the same clipped top line. Neither generated scene is
a gameplay screenshot, native sheet or completion evidence.

## Scope and verified corrections before implementation

Use the [native preflight](Verification/Live16x24/overwrit-native-preflight.md)
as the detailed owner/API checklist. This plan also rechecked the exact object
definitions, composition plan/builder and crop mutation path before runtime.

| Premise to avoid | Verified source and implementation consequence |
| --- | --- |
| A dedicated Overwrit generator contains only the four kit owners | `OverworldZoneManager.cs:87` appends `BiomeCropPlacement`; its `BiomeCropPlacement.cs:87–88` adds CultivatedSoilPart and Plantable to the **same terrain owner**. Real crop beds must refuse the plain-ground body. A separate hidden/foreign cultivation competitor must not erase valid disclosed ground. |
| Every terrain/object needs Material and Thermal | `Objects.json:37506` plain ground has neither. Marker at37554 has Stone/Destructible but no Thermal. Growth at37523 and bench at37587 own Plant/Wood and Thermal. Blanket copied guards would reject real content. |
| The bench is a rest station; the marker is a sign | The source bench is a solid breakable Wood object with no Restable service; marker is blank solid Stone with Rubble wreckage and no sign/travel service. Add no service icon, arrow, text or interaction. |
| An undamaged maximum HP is the only valid body | `OverwritCompositionBuilder.cs:99–105` accepts an extant destructible owner with0<HP≤MaxHP, nonnegative hardness and correct wreckage. Partial damage may retain the body; zero/Gone/removal must not. |
| Any Overwrit biome or null-POI address is ordinary | `OverwritCompositionPlan.cs:33–72` defines a finite22-address scope. Unsaying2,11 remains excluded even with null POI. Current map, graph and saved-lair authority must still be checked. |
| Concept shoots can have arbitrary height | `Objects.json:37547` describes every tip reaching the same height. Vary lower leaves/spacing, not the common top line; exclude flower/seed beads. |
| A reference image fixes a gameplay footprint/orientation | Keep one native16×24 foreground cell and real existing footprint. `OverwritCompositionPlan.InwardQuarterTurns` supplies existing facing; the five Pilgrimage scopes face inward west. Do not adopt the concept's enlarged bench or introduce facing mechanics. |

The exact ordinary scope, all at depth0, is:

| World y | Admitted x values |
| --- | --- |
| 9 | 0,1,2 |
| 10 | 0,1,2,3 |
| 11 | 0,1,3,4 |
| 12 | 0,1,2,3,4 |
| 13 | 0,1,2,3 |
| 14 | 2,3 |

These22 source candidates are **not22 accepted scenes**. Keep underground,
special sites, bleeds and the Unsaying outside this adapter. Do not alter
generation, expand opening `IsAdmitted`, move objects or add backdrop scenery.
The existing player, portable equipment/product art, crops and stairs remain
authoritative; crops/stairs are explicit remaining sprite obligations.

## Proposed native parts and essential states

Proposed resource `Sprites/Live16x24/overwrit-materials`: **8 cells,128×24**,
16×24 per cell,16PPU, centered pivot, full rectangle, Point/Clamp, uncompressed,
no mipmaps or tile colliders. Keep editable native source and a final mapping
manifest; preserve accepted atlases unchanged. Root freezes final frame/palette
details before implementation assertions. These counts are planned, not imported.

| Frame | Exact owner | Native requirement |
| --- | --- | --- |
| 0–3 | OverwritGround | Opaque quiet pale warm-grey scraped plane, rare low-contrast scuffs; no stones, cracks, borders, gradients or repeated bright flecks. Stable sparse variant distribution. |
| 4–5 | OverwritNewGrowth | Transparent small sage shoot clusters with identical clipped tip height; lower leaves/stems vary. No flower, crop or mature-grass identity. |
| 6 | OverwritPilgrimBench | Transparent plain wooden seat/feet silhouette, readable at one cell, restrained grain and honest inward read. No rest-service icon or invented state. |
| 7 | OverwritWaymarker | Transparent compact blank cool-grey stone, no lettering/arrow. Initial source/fixture-only coverage until naturally observed. |

Use muted taupe contact/outline, a restrained ground/scuff range, sage shoots,
desaturated wood and slightly cooler stone. Keep the existing player clearly
visible under actual lighting; a luminous pale carpet or noisy tiled surface
fails the scene comparison. No subtle animation, damage pose system or new
adjacency framework is part of this slice.

## Minimal adapter and integration contract

The following is a **proposed API**, not an implementation claim:

```csharp
FlatPixelOverwritMaterialsCatalog(FlatPixelFallbackCatalog borrowedSelector)
FlatPixelOverwritMaterialsCatalog(Texture2D atlas, FlatPixelFallbackCatalog borrowedSelector)
static bool IsSupportedZone(Zone zone)
bool TryResolveGround(Zone zone, int x, int y, out Tile tile, out Entity floor)
bool TryResolveForeground(Zone zone, int x, int y, Entity owner, out Tile tile)
bool TryGetSubjectSprite(Zone zone, Entity owner, int x, int y, out Sprite sprite)
void Dispose()
```

Cache a narrowly scoped `_overwritMaterialsArt` in Greybox and reuse existing
ground/foreground/subject/disposal paths. Each query must retain actual current
cached graph, valid current map dimensions/Overwrit/nullPOI, read-only saved-lair
exclusion, exact cell placement and current exploration/visibility. Foreground
and portrait require the actual selected disclosed owner. Public readers keep
their existing disclosure/document authority; adapter refusal may still yield
the truthful glyph through the public catalog.

Require exact blueprint/glyph/layer and actual owned Render/Physics/Examinable
parts; refuse foreign/duplicate essential parts, custody/takeability, custom
visual identities, unsupported footprint/stack and added semantic services.
Apply the distinct Plant/Wood/Stone/destructible/thermal contracts above rather
than one universal object predicate. Active render-affecting status and
unsupported thermal/material state retain honest fallback; unrelated effects
need not invent new sprites. Never repair an entity during rendering.

Ground requires one real visible valid floor and compatible live tile state.
Cultivated/Plantable source floor, duplicate/unknown Terrain, actual source or
hazard meanings refuse the dry picture. Exact neutral NewGrowth is the narrow
Terrain-overlay exception. Ignore hidden, foreign and carried competitors
before testing conflicts. Without a physical floor, do not invent substrate.
Opaque ground uses white tint, `GroundKind.Ground`, and `LocalGround` only on
success. Suppress only the same represented floor, never the player/crop/item.
Use separate resolver output locals; later misses must not erase prior success.
No water-representation flag, overlay priority or painter change is needed.

## Verification and actual acceptance gates

1. **Save genuine RED before production.** Root runs the bounded real-factory
   public-selector/submitted-painter fixture. Preserve raw outcomes; do not
   claim a complete XML if the service omitted it. No test count is promised.
2. **Implement and run GREEN with paired controls.** Cover four identities and
   same selected-owner readers;22 exact scopes/currentmap/POI/cache/savedlair
   revocation; reskin/ownership/visibility/custody/footprint; native ground under
   actors/items; actual crop-ground exclusion and hidden/foreign competitor
   controls; missing/malformed resource; idempotent disposal without destroying
   borrowed texture/selector; full then dirty paint after owner/state removal.
3. **Exercise real essential state.** Valid partial damage versus zero/Gone;
   actual applicable effect with an application-success precondition; real
   destruction removes growth/bench and produces marker Rubble exactly once.
   Rendering must not mutate those mechanics. Preserve lower floor/real hazard
   cues and clear stale submitted bodies. Fixture-created marker is not a
   natural-world observation.
4. **Repeat earned normal-input after views.** Compare GreenRim0,9, Blank0,10,
   observed bench area4,12 and DryRim0,13 at the same normal zoom/lighting where
   possible. Approach/read bench76,18 from75,18, inspect actual crop bed and
   floor layering, walk clear routes, F5, then fresh Continue the earned save.
   Match physical owner counts/positions and source states, not just colors.
   Record any naturally found marker separately; leave that gap open otherwise.
5. **Review whole visible compositions.** Replace the fallback glyph carpet
   with deliberate quiet material, coherent clipped-shoot clusters and readable
   rim objects. Blank's8 shoots and reserved approaches are actual source
   topology, not a fullness waiver. If still unfinished, name the defect and
   propose a separate tested real composition change; do not add fake scenery
   or call offscreen rim objects absent.59-column captures do not certify80×25.
6. **Independent review, preservation and delivery.** Fix Yellow+ findings,
   save before/after screenshots and measured bounds, update this plan and
   living coverage records in the **same commit** as source. Root fetches/
   rebases/pushes exact owned files and verifies remote preservation, then
   refreshes packages and exercises actual Mac launch/travel/restart. Linux
   archive checks do not establish Linux execution or physical Deck results.

## Performance and ownership

Follow [PERF-FOUNDATION](PERF-FOUNDATION.md) cached objects and granular dirty
tracking. Construct the finite address lookup and owned Tile/Sprite cache once.
Queries must not allocate lists/arrays, use LINQ/reflection, parse addresses,
construct plans/RNG, read pixels or rebuild sprites. Only current cell occupants
are examined; no new per-frame whole-world census or actor GameObjects.
Malformed/absent resources fail cheaply without repeated loading. Dispose owned
tiles/sprites once; retain borrowed atlas and selector. Existing dirty hooks
must handle removal/change without forced whole-zone redraws.

Record before/after real renderer/profiling evidence where measured. Source
inspection alone is not a zero-allocation or faster-frame claim. Any allocation
probe needs a positive allocation control; prior unsupported counters remain
inconclusive. Sustained hardware performance, Linux gameplay and physical Deck
remain separate gates rather than conclusions from static art/test counts.

## Classification, self-review and continuation

**CoO-original presentation of existing CoO owners. No Qud parity is claimed.**
The generator, crop mutation and object mechanics are retained; this work maps
native sprites to their already-authoritative state. Any later scope divergence
must be recorded with source references before the same-commit handoff.

| Readiness / review item | Current disposition |
| --- | --- |
| 🟢 Actual baseline and source preflight | Eight ordinary visits, four formation roles and one bench interaction retained with independent evidence review. |
| 🟢 Concept extraction corrections | Quiet v2 direction only; equal-height shoot tips, one-cell bench, flat pixels and crop-floor refusal specified. |
| 🟢 Native verification | Original capped material RED plus grading RED9/8failed/1passed retained; final533 XML passed, including501 prior,9 grading and23 existing palette/patcher cases. |
| 🟢/🧪 Actual acceptance | Editor replay and fresh Mac native reader/save/restart pass; matched world RGB identical. Independent Linux archive review and delivery/evidence push passed; whole-region/fullness open. |
| ⚪ Deliberate limit | Waymarker not naturally observed; second bench census-only; cropped viewport and remaining crops/stairs/site/actor identities explicit. |
| ⚪ Wider scope | Overwrit is one slice. Next Stump's eight owners, Grovelands actor bodies, then all remaining biomes/areas; no coverage percentage from frames. |

Implementation log: **2026-10-27 initial plan** — read CLAUDE, required pipeline,
native preflight, concept critique, before review and cited source semantics;
recorded the contract above before production. Root will append exact RED,
source corrections, final frame map, GREEN, independent review, actual captures,
profile limitations, preservation and push/package receipts as they occur.

Files changed by this planning task: **this new document only**. Planned
implementation files are the Overwrit companion catalog/test with Unity metadata,
bounded Greybox wiring, editable native source and resource atlas/import metadata.
Those are prospective paths, not claims that this task created them. Existing
five shared living records, runtime/tests/art, Unity and Git were untouched.
Last actual hourly report remains10:27UTC, next11:27UTC.


## Overwrit native material and state pass — 2026-10-27 10:56 UTC

**Eight native 16×24 cells now cover four exact owner identities across the
22-address ordinary Overwrit scope.** Ground0–3, clipped shoots4–5, inward-facing
wood bench6 and stone waymarker7 are integrated with the current owner, graph,
visibility and essential-state guards. Actual cultivated floors retain their
meaning; no generator, density, footprint, service or gameplay semantics changed.
The source atlas is128×24; existing approved art remains stable.

Genuine preimplementation [RED](Verification/Live16x24/overwrit-materials-native-red-job.json)
completed70 cases with failed status,25 capped failure details and null final
result. This is not evidence that all70 individually failed, and no RED XML is
claimed. The [focused native run](Verification/Live16x24/overwrit-materials-native-green-job.json)
passed70. The [combined run](Verification/Live16x24/overwrit-materials-related-green-job.json)
passed501: **70 new plus431 related**, with the focused70 included, zero failures
or skips. Cold runtime review reported no unresolved blocker.

Eight actual after screenshots cover Blank0,10, GreenRim0,9, PilgrimageRim4,12
and DryRim0,13 through normal keyboard travel, HP38. They show continuous native
ground, clipped growth, a native bench reader with Wood/Structure12/12, a shoot
reader with Plant/Structure4/4, and the same bench at11/12 after a real Right-bump
strike. The [census/profile receipt](Verification/Live16x24/overwrit-after-census-and-profile.json)
and [independent actual-view review](Verification/Live16x24/overwrit-after-independent-review.md)
bind this **bounded material and essential-state pass**. That reviewer authored
the source pixels, but did not author the regional runtime or operate Unity.

Whole-region graphics/fullness remains open. GreenRim has conspicuous fallback
Hollowchime/stair black cells; the approached bench view has Margincress fallback
cells. Binderroot and the second bench are outside this after sample; the
waymarker remains naturally unobserved. The centered Pilgrimage view has almost
no eastern rim/destination cue within the59-column viewport. Neither deliberately
quiet ground nor a partial viewport certifies the full80×25 chunk. Preserve real
crop/soil meaning when filling these gaps; do not hide it under plain ground or
add arbitrary decoration. Stump's eight-owner kit remains the next regional
breadth slice, followed by observed Grovelands actors and the remaining world.

The actual warmed current-bench probe measured256 queries,0 allocated bytes and
39,141 Stopwatch ticks at10,000,000Hz. Eleven normal moves measured11 full
passes,22,000 cells resolved,110,718 writes and2,380,603 ticks in the instrumented
renderer. These are bounded samples, not FPS, general zero-allocation, sustained
performance or physical Deck acceptance.

**Source push, earned fresh Continue and Overwrit packages are pending.** The
current delivered package remains combined Grovelands source
`ab26e270cb376253c971a41c3449ecbbf4171444`; no new packaged claim replaces it.
Finish those verification/delivery gates, then the named crop/stair/fullness
follow-up alongside Stump breadth. Last actual hourly report remains
**10:27 UTC; next11:27 UTC**. Storage reclamation remains inspection-only.

Current implementation paths are the Overwrit companion catalog and metadata,
bounded Greybox routing, the70-case EditMode fixture and metadata, editable
`ArtSource/Live16x24/overwrit-materials` packet and imported runtime atlas. Atlas
SHA256: `b379fcec4e4f1a991029598c70053a24ff37c45cfef6cc71b21f60a5f9889cc7`.
The source/Editor record is prepared for the same implementation commit; its
commit SHA and remote verification are deliberately pending root's push.


### Historical travel/reload color blocker — 2026-10-27 11:01 UTC (resolved below)

Fresh Continue now confirms that the actual bench persists at **11/12**, with
normal saves protected, but it exposes a **blocking presentation mismatch**:
the same Height view changes from tan in after07 to green in after09/10. Owner
persistence does not establish color consistency. The earlier pending-restart
statement is superseded by this partial result; **Overwrit packages are held**.

The [actual diagnostic](Verification/Live16x24/overwrit-travel-reload-color-red.json)
shows renderer zone `Overworld.4.12.0` while the biome patcher retains
`Overworld.11.10.0`/Spread, a null renderer reference and `currentManager=false`.
The active filter is `[0.85,1.05,0.9]`, contrast5 and saturation12. The shared
Volume-profile lifecycle is also under investigation; it is a hypothesis, not
an established additional cause. After09 fresh Continue and after10 damaged
reader are real gameplay evidence of the discrepancy, not accepted color match.

**Immediate next work:** root's source/test correction for current biome binding
and filter lifecycle, independent review, and matched ordinary travel/fresh
Continue replay. Only then finish exact source push and package acceptance.
Retain the native501 and bounded material/bench-persistence results without
promoting them to a complete presentation pass. Crop/soil/stair coverage,
centered Pilgrimage fullness and Stump breadth remain next; all-world static
coverage continues. Last actual hourly report remains10:27UTC, next11:27UTC.


### Travel/reload color correction verified — 2026-10-27 11:16 UTC

**The active color-consistency blocker is resolved within the actual tested
route.** The [retained native RED](Verification/Live16x24/biome-grading-native-red.xml)
had9 cases:8 failed and1 passed. The final [native GREEN XML](Verification/Live16x24/biome-grading-related-green.xml)
records **533/533 passed**, zero failed, skipped or inconclusive: all501 prior
renderer cases,9 new grading travel/profile cases and23 existing palette/patcher
cases. Its SHA256 is
`9eb3a0655171d9b276a5cc6a090f5e9ebd2fd8c8ce04d9573dabad1ad0c1c2a7`.
The actual run completed11:11:41–11:12:29UTC despite an incorrect MCP
initialization timeout; the complete XML establishes completion. The focused70
and previous501 are included in533, not additional distinct coverage.

The [actual replay receipt](Verification/Live16x24/biome-grading-actual-replay.json)
and five `overwrit-grading-01..05` screenshots record normal Continue at5460
with the bench11/12, world-map4Left4Up to Grovelands0,8,4Right4Down back to
Overwrit4,12, then35Right6Down to75,18, F5 at6230 and fresh Continue, HP38.
No entities, travel or save state were injected. The renderer, patcher zone and
current manager now agree throughout. Grovelands uses filter[.88,.92,.85],
contrast8/saturation6; returned and reloaded Overwrit uses[.9,.9,.9],
contrast−10/saturation−40. The rendered instance is separate from the preserved
shared profile[1,.97,.91].

The returned/fresh-Continue captures04/05 have **564,000 byte-identical RGB
pixels** in the world field bounded by(0,40,940,640), with zero changed channels;
world RGB SHA256
`8b43758ba102c04ee3b8f35dc368bf294936ae8a5fb3ceccdf180165e264ece7`.
This is a bounded actual material/owner-state/color-consistency pass, not a
claim about every biome, whole UI, all lighting or sustained performance.
The earned fixture is `Verification/Live16x24/Fixtures/overwrit-grading-return65`.
The earlier tan/green mismatch remains historical failure evidence, superseded
by this corrective replay.

**Source commit/push and new Mac/Linux package verification remain pending.**
The current delivered package remains combined Grovelands `ab26e270c`.
Finish exact reviewed delivery, then retain the truthful crop/soil/stair gaps,
Binderroot and unobserved marker, centered Pilgrimage destination/fullness
question, Stump eight-owner breadth and the remaining world queue. No generation
density change, whole-region acceptance, Linux execution, physical Deck or
sustained-performance claim is added. Last actual hourly report remains
**10:27UTC; next11:27UTC**. Storage remains inspection-only.


## Overwrit source and packaged Mac delivery — 2026-10-27 11:26 UTC

**Source `00959edf0f7a154acf03fd8e521326099d652414` is verified on origin/main.**
It contains the eight-cell/four-owner material slice and the current biome/profile
lifecycle correction, backed by the retained533-case GREEN and matched actual
Editor travel/restart evidence. The future delivery/evidence commit is pending;
no hash for that commit is asserted here.

Fresh tag `2026-10-27-overwrit-materials-00959edf0-current` builds succeeded:
Mac47.702916s and Linux49.60084s, each **zero errors/four warnings**. The
[Mac play receipt](Verification/Live16x24/packaged-overwrit-materials-mac-play-summary.json)
and [independent review](Verification/Live16x24/packaged-overwrit-materials-mac-independent-review.md)
retain seven actual original1280×856 CUA JPEGs00–06. Ordinary Continue at6230
loads Overwrit4,12, player75,18 HP38; the real bench reads11/12 and the shoot4/4.
A normal Left/Right step, F5 at6250, full process quit/relaunch and Continue
preserve the bench11/12 and corrected neutral field. The decoded world rectangle
[0,96,940,696] in01/04/05 matches all564,000 RGB pixels, SHA256
`975b642dee150bc5ace48a1ed5201be7adb03355582db2b2731578b10599b868`.
This is a captured JPEG comparison, not a lossless renderer-buffer assertion.
Each process log retains **five premature-finalizer notices and one ComputeBuffer
notice**, with no Error/Exception lines; no clean-log/leak-free claim is made.

The [Linux archive receipt](Verification/Live16x24/packaged-overwrit-materials-linux-archive.json)
records **128,780,457 bytes,189 regular files and202 archive members**, SHA256
`0aa1f10174b3ed860021ae9b58153546a24073cb18338cfd5cf278e2951849da`.
The helper verified all file/directory hashes, sizes/modes, safe membership,
checksum manifest and gzip EOF; **independent Linux review is still pending**.
It has not been executed on Linux or a physical Deck.

[Restoration](Verification/Live16x24/packaged-overwrit-materials-restoration.json)
verified all50,524 tracked source objects against the snapshot,38 foreign files,
normal Editor saves, restored standalone-save absence/preferences and preserved
prior builds/archives. Ten build-generated metadata/settings files were restored;
test saves were archived. No storage cleanup occurred. The earned package fixture
is `Verification/Live16x24/Fixtures/packaged-overwrit65` at6250, HP38.

This is a bounded native Mac owner/color/restart pass; whole-region fullness,
Hollowchime/Margincress/Binderroot crop and truthful soil graphics, stairs,
second-bench area, naturally unobserved Waymarker and centered Pilgrimage cue
remain open. Finish independent archive review and exact evidence push. **Next
regional breadth remains Stump's eight-owner static kit** through actual
baseline/concept/native/integration gates, followed by observed Grovelands actors
and every remaining biome/area. Stump's18 candidates are source-based and are not
visited/accepted by this record. Retain Overwrit crop/stair/fullness follow-up
without displacing regional breadth. Last actual hourly report remains
**10:27UTC; next11:27UTC**. No sustained-performance or physical Deck claim.


### Independent archive completion — 2026-10-27

The [independent Linux archive review](Verification/Live16x24/packaged-overwrit-materials-linux-independent-review.json)
now **passes within archive-integrity/retained-evidence scope**:189 files,
13 directories,202 members, full gzip EOF, actual tree/hash/size/mode, internal
and external checksums, both fresh build/output bindings,19 support-source copies
and533 distinct native identities. Its retained1034 test occurrences overlap by
501; they are not1034 distinct cases. The pending-review statement above is now
superseded. No Linux or physical Deck execution is claimed.

Overwrit source, bounded Mac play/restart, archive integrity and restoration are
verified; the exact delivery/evidence push remains pending. Proceed with Stump's
eight-owner baseline/native gates and retain the named Overwrit crop/stair and
fullness follow-up. No Stump sprite coverage is accepted by this package record.
The actual hourly timestamp remains10:27UTC until root sends the next report;
next due11:27UTC. No future evidence-commit SHA is invented.


### Actual hourly report — 2026-10-27 11:27 UTC

**Root sent the requested itemized report at11:27UTC; next due12:27UTC.**
Completed: Overwrit's8 native cells/4 owners,533 native passes, actual grading
travel/restart replay, fresh Mac/Linux builds with bounded Mac gameplay,
independent archive review/restoration and source `00959edf0` on main. The
separate exact delivery/evidence push remains pending. In progress: Stump's
ordinary gameplay baseline. The coming session targets its **five formations
and eight owner families**, then concept/native/integration gates; none is yet
accepted as Stump native coverage. Overwrit crop/soil/stair and centered
Pilgrimage fullness remain unfinished. Preserve the all-biome/area queue and
defer nuanced animation. Earlier report times above are historical.


## Stump actual before survey and next production gate — 2026-10-27 11:40 UTC

**Overwrit delivery/evidence commit
`587837b255e90fc642adef38291483c5de09b782` is verified on origin/main**, alongside
source `00959edf0`. Its prior pending evidence-push statements are now historical.
The current delivered Mac/Linux package remains that bounded Overwrit release.

Stump's ordinary-controls **BEFORE survey now covers all five formation roles**:
CascadeGorge2,5; Grainfield2,2; ButtressRidge4,5; SummitScrub3,4; RimForest2,3.
The [actual census](Verification/Live16x24/stump-before-owner-census.json) binds
all five to current graphs without saved lairs, HP38, and2,000TepuiStone owners
per area. The six `stump-before-01..06` PNGs comprise five world views plus the
real tank reader. [Independent composition review](Verification/Live16x24/stump-before-composition-review.md)
finds distinct existing landform structure obscured by glyphs; it does not
justify arbitrary added density. SummitScrub's exposed center is the strongest
current fullness risk, to reassess after truthful native materials.

The earned `Verification/Live16x24/Fixtures/stump-before66` checkpoint is
**turn6810,RimForest2,3,player37,11,HP38**. TankBrocchinia36,11 was approached and
examined; no drinking or harvesting action has yet been verified. Gorge pools,
Grainfield gaps, Buttress fingers, Summit domes and RimForest vegetation/crack
remain actual geometry to preserve. Centered views show59of80columns, not every
80×25 cell or all18 ordinary source candidates. Actors, crops/soil, hazards,
stairs, props and special sites remain explicit gaps.

The [Stump implementation plan](GRAPHICS-STUMP-MATERIALS.md) defines eight new
owner families and two exact unchanged Tree/Bush sprite reuses. Native source
and meaningful RED-test preparation are now underway. Conceptv1 was rejected
for3D/elevation and cactus imagery; v2 contributes only palette/quiet-ground
direction, with changed placements and HUD rejected. **No Stump native runtime
integration, test pass, after-view, fullness or package acceptance is claimed.**
Proceed through genuine native RED, bounded integration, GREEN and matched
five-formation actual travel/state/restart review. Keep Overwrit crop/soil/stair
and Pilgrimage-fullness follow-up, then Grovelands actors and the all-world queue;
nuanced animation remains deferred.

Root archived the owned save session at
`/private/tmp/coo-stump-baseline-archived-4fc73aaf` and finished isolation; Unity
was idle/clean in SampleScene with no owned root. Normal saves and38 foreign
files were unchanged; approximately429.56GB free was reported, with no deletion.
This is protection evidence, not authorization for storage cleanup. **No new
hourly report is recorded: last11:27UTC,next12:27UTC remain unchanged.**


### Actual hourly report — 2026-10-27 12:27 UTC

Root sent the requested itemized report at12:27UTC; next due13:27UTC.
Completed: Overwrit delivery/evidence587837b verified on origin/main; Stump
five-formation before survey, eight material-owner integration, initial644
native passes and actual tank examination/drinking plus dome collision.
In progress: palette correction and71-frame connected-solid perimeter, with
revised related native tests running after111-case RED (17fail/94pass).
No final Stump visual, whole-region, package or push acceptance yet.
Next: corrected five views, ordinary harvest and fresh Continue, reviewed
exact Stump main push, then missing fauna identities and all-world breadth.
Creature/crop/hazard/stair/prop and settlement/underground/special-site gaps
remain open; nuanced animation remains deferred.


## 2026-10-27 Stump perimeter continuation — native GREEN, final gameplay pending

The current Stump material slice is integrated but is **not yet accepted for
final gameplay or delivery**. Its 71-cell native atlas covers eight material
owners, with two unchanged Tree/Bush reuses. The 26 palette-v2 cells are retained;
45 appended cells give the three solid families restrained connected perimeters.
The atlas SHA256 is
`fd33497368615b60f09ccc2d16efe25dd0033370034c95ce93fe0445bb2af7f1`.

Genuine perimeter RED retained **111 cases:94 passed,17 failed**. The subsequent
native run at12:29:02–12:30:00UTC passed **663 distinct cases:111 Stump
(92 original plus19 perimeter) and552 related**, with no failures, skips or
inconclusive results. Evidence is
`Docs/Verification/Live16x24/stump-perimeter-related-green.xml` (SHA256
`ee0a5cca8aa3fa3ec40e52a1d263f68e69ab97d016af917b087410b4a5538ba3`).
The earlier92-case RED/candidate GREEN and644-case related GREEN remain history;
these overlapping runs are not added together.

Actual initial five-formation trials failed stone/floor color and collision
readability. Palette-v2 repaired color in two views, but solid domes/ridges still
read as floor patches; a normal blocked bump toward StoneDome44,9 preserved
player44,10 andHP38. The real tank reader/drink trial is recorded. The current
perimeter revision addresses that named visual defect; its final five-view
comparison, ordinary state/harvest checks, profile and fresh Continue remain
pending. See `Docs/Verification/Live16x24/stump-trial-composition-review.md`,
`stump-trial-gameplay-receipt.json` and `stump-palette-v2-gameplay-receipt.json`
under the same verification directory, and `Docs/GRAPHICS-STUMP-MATERIALS.md`.

The user played during the first final session. Root saved turn7640 with F5 and
reports all six files persistently backed up and verified. Those interrupted
captures/profile are **nonacceptance evidence**. Root is restarting the unchanged
earned6810 fixture at `Docs/Verification/Live16x24/Fixtures/stump-before66` for
a clean review; the user's progress must remain protected. No new source commit
or package is claimed. The delivered Overwrit source remains
`00959edf0f7a154acf03fd8e521326099d652414`, with delivery/evidence
`587837b255e90fc642adef38291483c5de09b782` verified on main.

Next: complete corrected material gameplay/delivery, then the nine source-grounded
Stump fauna identities in `Docs/GRAPHICS-STUMP-FAUNA.md` (eighteen census encounters;
preflight only), followed by Grovelands actors and the remaining all-world queue.
Overwrit crop/soil/stair and Pilgrimage composition gaps, other actors, props,
hazards, settlements, underground and special sites remain open. No generation
density change or whole-biome/fullness acceptance is inferred from tests or
partial views. Nuanced animation remains deferred. Completed trial isolation
preserved normal saves and38 foreign files; no storage cleanup or source deletion
was performed.

**Last actual hourly report:12:27UTC; next due:13:27UTC.** The later native result
updates current evidence only; this entry does not claim another report was sent.


## 2026-10-27 Stump clean material checkpoint — source and packages pending

The corrected 71-frame material kit now has a **bounded clean Editor integration
checkpoint**. Root traversed all five formations through ordinary controls:
RimForest (`stump-final-01`), SummitScrub (`05`), ButtressRidge (`07`), Grainfield
(`08`) and CascadeGorge (`18`). These are five actual formation views, not all
18 eligible addresses or full 80×25 chunk acceptance. Consolidated evidence is
`Docs/Verification/Live16x24/stump-final-gameplay-receipt.json`; all20 final
captures are in that verification directory. The earlier interrupted session
remains excluded, with the user's separately backed-up turn7640 progress protected.

Actual tank reader/drink is shown in final03/04. A normal Up bump at44,10 toward
the dome kept44,10 andHP38. At Grainfield35,18, ordinary harvesting removed the
vein and yielded **Tepuibone ID69509,count1**; final16 inventory and17 reader
verify the selected native product. Interim inventory-navigation images do not
establish that product acceptance. The Gorge route crossed wet39,11 towet43,11,
then dry DescentLedge43,12, retainingHP38. F5 saved
`Docs/Verification/Live16x24/Fixtures/stump-final68` atturn7360. Fresh-session
Continue in final20 restored43,12, HP38, the same product/count and the removed
vein. Root's decoded RGB comparison of the world viewport `(0,40,940,640)` in
final19/20 is equal, as recorded in the receipt.

Native663/663 remains the current test result. The final Editor profile covers
56.10377seconds with11,607 valid samples for each of four counters and no capacity
overflow (`stump-final-profile.json`). It includes MCP/capture overhead and has no
matched performance baseline: it is not an optimization, build-FPS, GPU, Linux or
physical Deck claim.

`Docs/Verification/Live16x24/stump-final-composition-review.md` records the bounded
material checkpoint. Full regional polish remains open: dome blocking semantics
are still subtle, Summit composition is sparse, and glyph actors/black-backed
effects still interrupt native coverage. No generation density change or complete
biome/fullness claim is made. **Exact main push and fresh packages are next**;
source commit is not yet assigned and the current delivered package remains
Overwrit00959edf0/evidence587837b. Final-session restoration is root-owned and is
not inferred solely from gameplay completion.

After material delivery, continue the nine-identity Stump fauna plan in
`Docs/GRAPHICS-STUMP-FAUNA.md`, then Grovelands actors and all remaining world
families. Keep Overwrit crop/soil/stair/Pilgrimage and every settlement,
underground, special-site and whole-scene composition gap visible. Nuanced
animation remains deferred. **Last actual hourly report remains12:27UTC;
next due13:27UTC.** No new hourly report or storage cleanup is claimed here.


## Explicit user requirement — separate all-world polish phase

The user requires a **serious biome-by-biome and area-by-area polish pass after
broad sprite coverage**. This is a separate required phase, not a synonym for
sprite integration. Current material checkpoints, native-test totals, package
verification and asset counts do not establish polished areas. The current
coverage order stays in place: finish the Stump material delivery, fill the
planned Stump fauna and Grovelands actor gaps, and continue missing families
through every biome/area. Optional nuanced animation does not come first.

The dedicated pass must review each ordinary formation, settlement and real
interior, underground/depth/layout role, lair, opening-journey area and special
site as its own scope. At normal gameplay zoom, inspect the whole traversable
chunk through multiple real views. Dense sections and intentionally quiet
sections must both have coherent composition, readable routes and a deliberate
visual identity; neither random clutter nor a density number proves quality.
Review silhouettes, palette, scale and materials; landmarks, interiors,
services, entrances, stairs and hazards; neighboring-area transitions; FOV,
occlusion and remembered cells; essential state changes; and the same owner's
world/inventory/examine identity. Preserve saves and real gameplay systems.

Each area requires actual before/after captures with seed, zone, zoom, route,
state and source revision; normal control/interaction checks; an honestly
scoped profile; and relevant regressions. Independent visual review should
assess the result where practical. Acceptance remains bounded to the actual
views, states, routes and platforms tested. Atlas counts, concepts, contact
sheets and tests alone cannot replace this evidence.

Track polish independently in
`ArtSource/Live16x24/world-sprite-coverage.json` using these states:

- **not_audited**: the dedicated review for this exact scope has not happened.
- **in_progress**: actual review or a named correction is underway.
- **reviewed**: findings and evidence are recorded, but unresolved defects remain.
- **bounded_accepted**: the named area/state/route meets the gate with explicit
  evidence and limits; other areas and the biome as a whole remain separate.

Every biome and area needs its own state, named defects and disposition.
Concrete zone/depth/interior instances must be expanded from the source-grounded
inventory before acceptance; unexpanded and unknown coverage remains explicit.
Open-ended generated areas require bounded variants and retained unknowns,
not a claim that every possible seed was visited. The new phase is **not
complete or retrospectively accepted**. Prior bounded material receipts remain
valid without being promoted to polish acceptance.

Known actual priorities include sparse SummitScrub composition and subtle
blocking-dome cues in Stump, plus remaining glyph actors/black-backed effects.
Overwrit crop/soil/stair and centered Pilgrimage composition gaps remain open;
its deliberately quiet aesthetic does not exempt it from polish review. These
named defects must survive the coverage handoff into the dedicated polish pass.
The current12:27UTC hourly report and13:27UTC next-report clock are unchanged.


## 2026-10-27 Stump package checkpoint — source on main, delivery record push pending

Source `98e3e167b8789608f783701490822a024ffa9963` is verified on origin/main by
root. Both fresh builds succeeded with **zero errors and four warnings each**:
Mac62.817728seconds, Linux56.168376seconds. The current package tag is
`2026-10-27-stump-materials-98e3e167b-current`; prior packages are preserved.
The later delivery/evidence commit has not yet been assigned or pushed.

Actual Mac normal controls continued the earned Stump save, showed one native
Tepuibone in inventory and its reader, traveled CascadeGorge→Grainfield, saved
turn7460, fully quit and relaunched, then used fresh Continue. The result is
Grainfield40,12,HP38 with native materials and the retained harvested-seam row.
The earned fixture is `Docs/Verification/Live16x24/Fixtures/packaged-stump69`.
Six JPEG capture files and process/input provenance are recorded in
`Docs/Verification/Live16x24/packaged-stump-materials-mac-play-summary.json`;
the independent image review is the matching `-mac-independent-review.md`.

Saved05 versus Continue06 has identical decoded world RGB in `[0,96,940,696]`
(564,000pixels). This is a JPEG window comparison, not a lossless render-buffer
or Editor/package identity claim. Travel04 and Continue06 are byte-identical,
so they are not two distinct visual states; root's independently recorded new
process/path provenance supports the restart. A mistakenly launched older app
and its screenshot are excluded. The first log overlaps that old launch and
cannot support a sole-process clean-log claim. The restart log has zero
exception lines but retains six premature-finalizer notices and one
ComputeBuffer-disposal notice; no clean-log claim is made.

The Linux archive is **128,791,153bytes**,189regular files/13directories/202members,
SHA256 `429a9b9c30ba6ae47b4671c3d670b0a4933c9c98a8a3401acefe874e695956ef`.
Independent verification passed the full archive/build file hashes, sizes,
modes, safe paths, checksum manifests, gzip/footer and source/build/native-test
bindings. Evidence is
`Docs/Verification/Live16x24/packaged-stump-materials-linux-independent-review.json`
and the corresponding `-linux-archive.json`. Linux was not executed; physical
Deck and sustained FPS/GPU behavior were not tested.

`Docs/Verification/Live16x24/packaged-stump-materials-restoration.json` confirms
all50,712 tracked files match the source snapshot,38 foreign files are unchanged,
normal Editor saves and standalone save absence/preferences are restored, ten
build-serialization/owned-whitespace files were restored, and old builds/archives
are preserved. Test saves were archived. No cache cleanup or deletion occurred.

This completes a **bounded material package checkpoint**, following663 native
passes and five Editor formation views. It does not accept all18eligible Stump
addresses, whole80×25chunks, remaining actor/effect/crop/prop/site families or
regional polish. The separate required polish phase, all55inventory-scope states
and five concrete Stump area states remain **not_audited**. Sparse Summit,
subtle dome blocking cues, glyph actors/effects and the other all-world defects
remain open.

Next: push the reviewed delivery/evidence records, then continue the saved
nine-identity Stump fauna plan, beginning with the five existing crisp16×24 reuse
candidates after real close baselines; retain the remaining adaptations/MawToad
work, Grovelands actors and all-world coverage queue. Serious area-by-area polish
follows broad coverage; nuanced animation does not move ahead of it.
**Last actual hourly report12:27UTC; next due13:27UTC remain unchanged.**
