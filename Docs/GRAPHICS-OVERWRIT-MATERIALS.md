# Overwrit native ground, shoots and rim objects

2026-10-27. **Current pass: eight native cells/four exact owner families are
integrated. Final533 native cases passed; bounded actual material, owner-state
and travel/fresh-Continue color consistency are verified. Source commit/push
and new package checks remain pending.** Root owns Unity, captures, execution,
integration and Git. Whole-region fullness, crop/soil/stair coverage and natural
waymarker observation remain open. The original preimplementation contract and
failed color replay are retained; the dated resolution below supersedes their
planning-time or blocked statements.

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
| 🟢/🧪 Actual acceptance | Material/state views and corrected real travel/fresh-Continue grading pass; matched world RGB identical. New packages pending; whole-region/fullness remains open. |
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
