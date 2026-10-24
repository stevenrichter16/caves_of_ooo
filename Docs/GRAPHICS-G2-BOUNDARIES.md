# G2 — connected opening boundaries

Status: **native connected masonry integrated; bounded ordinary keyboard Play
and native-size review accepted**, 2026-10-24 UTC. The48-frame atlas now appears
through normal launch. Focused60/60 and overlapping bounded552/552 Unity tests
pass. [Actual Play record](Verification/Live16x24/g2-play-verification.json)
covers the route, damage/destruction, examine and visibility observations.
Parent: [graphics completion](GRAPHICS-COMPLETION-PLAN.md). This advances one
G2 family; pond/road/soil, full opening acceptance, packaged release and physical
Deck checks remain incomplete. Historical checkpoints below retain their bounds.

## Evidence and chosen first family

Reviewed the actual [opening capture](Verification/Live16x24/native-states-start.png),
[paintover prompt](../ArtSource/Live16x24/concepts/g2-boundary-paintover-prompt.md),
[generated paintover](../ArtSource/Live16x24/concepts/g2-boundary-paintover-v1.png),
[parts prompt](../ArtSource/Live16x24/concepts/g2-boundary-parts-prompt.md) and
[generated parts reference](../ArtSource/Live16x24/concepts/g2-boundary-parts-v1.png).
The actual capture shows low walls as repeated disconnected posts, while the
ruin's narrow vertical sprite does not describe its horizontal connections.
This is the first family to improve: **connected masonry belonging to actual
opening Wall entities**. Keep the entire `ReferenceGladePlan` geometry.

The paintover is rejected as layout and gameplay evidence: it raises the
passable RoadStone bands at the basin/garden into walls, changes scale/text
and adds soft shading. The parts sheet is reference only: its many-pixel,
shaded blocks and multi-cell illustrations are not native tiles. Retain only
the broad stone masses, sparse mortar and restrained material contrast. Do
not trace, crop, resize or import its pixels. Banks, paths and soil are later
separate packets; this first packet does not create them.

## Source sweep and corrected premises

| Premise | Verified source / required decision |
| --- | --- |
| Worn crossings are passable | `GleanersDistrict.cs:99` and `SpreadExplorationWorksites.cs:147` remove only the Wall tag. Solid tag and Physics.Solid remain. They block walking, permit sight and can be vaulted. Render them low, never as open floor; retain the path around. |
| VisualID alone identifies masonry | `ReferenceGladePlan.cs:49–51` assigns lit-wall to both Wall and GlowQuartzVein. `FlatPixelOpeningArtCatalog.AnchorFrameOf` already dispatches blueprint first. Quartz stays a harvestable resource and never connects a wall. |
| Decorative names prove collision | Wall has solid Physics and Destructible; BrokenColumn is explicitly passable rubble in `ReferenceGladePlan.cs:58–61`. Neither BrokenColumn nor Rubble is a masonry connector. Do not change tags/collision/FOV for art. |
| Quarter-turn metadata gives adjacency | `ReferenceGladeBuilder.cs:66` stages ReferenceGladeQuarterTurns for old presentation, including quartz. Use current disclosed cardinal owners for connectivity; never rotate/stretch a 16×24 raster or use this metadata to infer a missing neighbor. |
| Lit walls should become crystal sources | The builder adds a real LightSource to lit-wall placements. The existing wall frame is mineral-seamed masonry; quartz has distinct points and finite harvest. Keep a muted green material accent, no crystal points, halo or invented harvesting. Actual illumination remains the lighting system's job. |
| Damage needs several new wall states | Destructible.HP/MaxHP support future damage art, but this packet needs no arbitrary HP thresholds. Destruction removes the owner and creates WreckageBlueprint Rubble. Connected neighbors must cap after removal; rubble must not preserve the wall silhouette. |
| All adjacent objects connect | Only exact current, disclosed top Wall owners with one of the three approved VisualIDs qualify. RoadStone, wicket, beam, quartz, higher occupants, hidden/foreign/stale owners and ordinary StoneWall do not. |
| New topology requires a global watcher | `EnvironmentSpriteRenderer.PostRender` already expands dirty cells to the eight-neighborhood before releasing/re-resolving claims. Use this bounded path and verify real mutation/FOV dirtiness; no world scan or per-frame adjacency map. |

## Exact native asset contract

One optional group, proposed resource `Sprites/Live16x24/opening-boundaries`:
**48 frames, atlas256×72**,16 columns ×3 rows; every frame is16×24 at16PPU,
center pivot, Point/Clamp, no mipmaps, uncompressed, binary alpha. Existing
atlases and all approved character/prop inputs remain byte-identical and
recoverable. A valid owner with missing/malformed optional atlas retains its
prior native art. A semantically rejected known wall uses its actual glyph;
it must not bypass stricter checks through that older atlas. Neither case
forces legacy presentation or removes the owner's body.

Rows/material indices are fixed:

| Row | Exact owner | Material / structural reading |
| --- | --- | --- |
| 0 | Wall + `reference-glade-low-wall` | Low olive-grey cap: mortar `233C32`, stone `788D73`, edge `A3AE8C`. Six-pixel central course with restrained edge contrast. Remains an obstacle even where Wall tag is absent. |
| 1 | Wall + `reference-glade-dark-ruin` | Dark masonry: mortar `233C32`, stone `3C5148`, cap `526657`. Slightly broader central mass, no miniature detached pillar repeated along a run. |
| 2 | Wall + `reference-glade-lit-wall` | Same masonry geometry as dark ruin, using `233C32`, `526657`, muted mineral `70A68A`. Narrow mineral accents only; no source crystals or emission painted into neighboring cells. |

These are starting palettes from accepted base/anchor sources, subject to
native-size visual review; no palette changes to earlier images. Keep at most
three visible material values per frame. Limit mortar interruptions and chips
to the interior, so they cannot read as walkable gaps or break matching edges.
Do not add loose stones outside the owner's cell.

Column is the four-bit mask: **N=1, E=2, S=4, W=8**. Zone north is `(x,y-1)`;
authoring image north is row0. Frame index is `material*16 + mask`.

| Mask | Shape | Mask | Shape |
| --- | --- | --- | --- |
| 0 | isolated capped block | 8 | west end |
| 1 | north end | 9 | northwest corner |
| 2 | east end | 10 | west–east run |
| 3 | northeast corner | 11 | north/east/west tee |
| 4 | south end | 12 | southwest corner |
| 5 | north–south run | 13 | north/south/west tee |
| 6 | southeast corner | 14 | east/south/west tee |
| 7 | north/east/south tee | 15 | four-way intersection |

The first native review corrected the original common-six-pixel-port design:
a wider dark/lit core repeated every cell made long runs look beaded. Use
constant-width courses per material instead. Low N/S ports occupy x5–10 and
E/W ports y9–14; dark/lit N/S ports x4–11 and E/W ports y8–15. The core uses
the same width as its arms, without repeated bulges. Different materials
still share an opaque six-pixel center at their joint; each family's matching
ports must match both alpha and palette exactly. This is a visual correction,
not a new obstacle or gameplay height rule.

Only an enabled mask arm touches its cell boundary; an absent arm terminates
inside the cell with a clear cap. Design all shapes directly on the rectangular
lattice, not by rotating square art. Terrain connections intentionally may
touch rows0/23; the actor/prop blank baseline rule is not applicable. Mask0
must still read as a solid block.

## Owner, disclosure and state selection

Scope the new family to **current active opening glade**, using the same
current manager graph authority as G1 plus `ReferenceGladePlan.IsActive`.
Do not silently extend it to north, cellar StoneWall, other ruins or modded
VisualIDs. Exact source admission is distinct from global flat projection.

For the center and each of at most four neighbors, reuse G1's strict current
cell/owner selection. Require exact BlueprintName Wall, one of the three
VisualIDs, owned visible Render, no explicit VisualVariant/GlyphVariants,
owned non-takeable Physics with no inventory/equipment custody, and owned
Destructible. In visible cells additionally require `!Gone`, HP>0 and actual
solid identity (Physics.Solid or Solid tag); do not require Wall tag for the
low crossing. Remembered static cells must not read live HP/Gone/solidity or
light state: use only G1 memory eligibility, structural owned-part identity
and capped mask0. Do not pin HP, hardness, lighting enabled state or display
name to blueprint defaults.
Reject non-single-cell footprints for this first one-cell masonry family.

Neighbors connect across the three material rows, only when their selected
current owner independently qualifies. No diagonal arm, connection across a
missing cell, or peek through a higher top occupant. A query must not recurse
through the completed topology resolver: use the existing pure current-owner
selection, then calculate four bits. No new render events, entity IDs as
ownership, mutable global topology or borrowed-catalog disposal.

Visible center: only currently visible neighbors can set bits. Unexplored or
remembered neighbors terminate the visible course. Remembered center: retain
G1's conservative static-terrain eligibility and use capped mask0; do not
query live hidden adjacency. This is not a historical wall-memory system.
Revealing the same current cells restores connected masks through existing
FOV dirtiness. A remembered low crossing without Terrain/Wall eligibility may
remain absent under that existing policy; do not manufacture permission.

Exact-subject examine uses the matching material's isolated mask0, after the
normal document/current-presentation authorization. A portrait need not
expose unseen neighbors or reproduce the transient world connection shape.

The buckled wicket at `(46,6)` stays its own accepted three-state art and never
supplies a connector bit, whether buckled, shut or open. Masonry end caps
flank it; do not paint a stone arm over the opening. FallenBeam at `(26,19)`
stays its own solid, haulable owner and never supplies a bit. Moving it exposes
the real doorway. Main passage y16 and the other authored gaps remain open.
Quartz at `(31,10)`/`(31,22)` is always excluded, before any VisualID match.

## Editable authoring and integration sequence

1. Add a new source-only `Tools/Live16x24/build_opening_boundaries.py`, writing only
   `ArtSource/Live16x24/boundaries/`. Reuse `add_quad`, `camera_for` and
   `emission_material` from `build_opening_base.py`; author named integer
   `Frame.cells` and mesh quads. Do not open/sample any input raster. Save
   editable `.blend`, native atlas, manifest/palettes and native verification.
2. Render contact layouts directly from the same geometry: all16 masks in
   each material, horizontal/vertical runs, all corners/tees, mixed-material
   joins, isolated caps, a one-cell empty gap and the real wicket/beam/quartz
   adjacency cases. Render native and integer contacts, no image resampling.
   Verify exact native pixels, binary alpha, palette limits and seam agreement.
3. Before runtime code, save focused native RED tests for mask routing and
   disclosure. Add a small disposable cached masonry catalog in the existing
   presentation selection path, with explicit borrowed texture injection and
   default Resources load. Prefer this one family adapter to a thirteenth
   positional texture argument across the already large opening constructor.
   Share strict G1 selection; do not add a renderer or change generation.
4. Test synthetic GREEN, then a genuine absent-resource RED before importing
   the accepted source PNG. Verify all48 actual imported frame rects, alpha,
   filtering, cache reuse, disposal and independent earlier resources.
5. Run the affected existing renderer/ground/ownership/camera controls once.
   Inspect real prepass/neighbor claim ordering, not just direct mask queries.
   Review independently; fix material findings before actual Play acceptance.

## Required counters and actual acceptance

Tests must include all16 masks; mixed material joins; top-owner/foreign Render
or Physics/custody/duplicate-ID/stale-cell and managed-graph replacements;
known VisualID on wrong blueprint; custom variants; RoadStone/BrokenColumn/
Rubble/quartz/wicket/beam/StoneWall negatives; low crossing with Wall tag
removed but walking still blocked and sight unchanged; hidden/unexplored
neighbors; conservative memory; missing atlas; no mutation of gameplay.

Exercise actual destruction/removal, cover/uncover and FOV dirty paths through
the real submitted renderer: surviving neighbors cap, old pixels disappear,
and a quiet frame does not trigger a full-zone redraw. Wicket state changes
and hauling the beam must leave their real route open after the owner changes;
they must never be turned into masonry. Warm topology resolution must allocate
zero after setup and inspect only the current cell plus four neighbors.

Ordinary Play gate: same seed,1280×800 and framing as a fresh before capture;
start view, northern shelter/wicket, western ruin passage, basin approach and
one destruction or beam movement. Verify keyboard/Deck targeting, collision,
Vault landing and examine identity. Capture native frames at the same route
and scale, with raw state receipts; no concept may substitute. A reviewer must
recognize continuous walls and real openings without cursor outlines. The
native packet is not complete until these comparisons pass. This document
keeps that full acceptance target separate from the bounded results below.
Physical Deck and ordinary Vault checks are still pending, not waived.

## Offline production receipt — 2026-10-23

Source files live in [ArtSource/Live16x24/boundaries](../ArtSource/Live16x24/boundaries/README.md).
The [builder](../Tools/Live16x24/build_opening_boundaries.py) authors named
integer-cell Blender geometry, saves an editable `.blend`, and renders the
native256×72 atlas plus a direct4× contact. No input raster is opened, sampled
or traced. The context image is a detached source-layout study using the
actual planned wall coordinates and unchanged existing quartz/beam/open-wicket
geometry; it omits other game objects and fog and is **not a game capture**.

The source-contract verifier was run before the artifacts existed and failed
for the six missing required files: [source RED](../ArtSource/Live16x24/boundaries/source-red.json).
The first render passed exact-pixel checks, but visual review rejected repeated
dark/lit center bulges along straight runs. Its atlas, contact and numerical
receipt remain under `*-pass1` names. The second pass keeps a constant course
width, preserving material contrast without changing any source-world geometry.

The [final source receipt](../ArtSource/Live16x24/boundaries/source-green.json)
verifies all48 frames, zero native/source pixel mismatches, binary alpha,
three colors per frame, connected opaque masses, matching cardinal alpha and
color ports, and exact direct-rendered contact pixels. Native atlas SHA-256:
`7ad89c28fa6d9dcc3be83ed9935aa257f5d10b1d3ba41100f0dc18051a89dff2`.
The contact is4× rather than the initially suggested8×; it still checks every
enlarged pixel against the same authored geometry, and the native atlas is
unchanged by presentation scale. See [visual review](../ArtSource/Live16x24/boundaries/visual-review.md).

This proves the offline source contract only. Root reviewed the final contact
and source-layout context and approved a **runtime trial only**: the continuous
courses read as connected barriers, while quartz, the beam and real gaps remain
separate. This is not final art or gameplay acceptance. Resource import, exact
runtime routing, fog and destruction repaint, frame cost, and ordinary
keyboard/Deck gameplay checks remain pending. Earlier atlas files were not
modified. G1 continuity and normal launch remain the higher-priority integration
gate; no Assets import is authorized by this source-review record alone.

## Later small families, deliberately not bundled now

- **Shoreline:** only disclosed current actual water coating/ground, never
  `IsPond` geometry alone on revisits. Sparse1px inset/bank clusters within the
  selected ground cell; no vessel/service/source implication and no erasure of
  oil/acid/charge/frost cues. Water-supply basin remains independent.
- **Path:** exact passable RoadStone, flat low-contrast chips/edge ends. No raised
  blocks, new collision or continuous wall band around the garden/pond.
- **Soil:** actual cultivated ground and its moisture, with sparse furrows;
  crop stage and harvest owner remain separate. Keep reserve cords readable,
  never imply permission, ripeness or readiness from decoration.

Keep at least80% of otherwise quiet ground plain. Deliver and inspect the
masonry improvement first; use its actual capture to choose which later family
most improves the scene. Do not expand this into a generic biome asset pack.

## Integration continuation — 2026-10-24 UTC

G1 normal launch/travel is accepted in the editor and pushed in `1644aea51`.
The root imported the proposed G2 fixture before production implementation.
The first scripts-only refresh did not import the new test file and discovered
zero tests; this is not RED evidence. Full import caught four scratch uses of
nonexistent `Entity.RemoveTag`; they were corrected to the actual `Tags.Remove`
API before any behavioral result. A source review did not prove compilation.

The corrected [API RED](Verification/Live16x24/g2-boundaries-api-red.xml) ran
48 cases:0 passed,48 failed, with missing adapter/integration seams and a genuine
missing Resources atlas. The actual result precedes adapter implementation.
Independent review added one factory-backed Damage→Destroy→Rubble submitted-
world counter; unlike generated gravel, real wreckage has no native VisualID
and must retain its truthful fallback identity. It has not yet run.

Current pass: adapter lifetime and pure topology implementation, prior to world
routing and resource import. Root owns Unity; source edits are scoped to the
new boundary catalog and existing Greybox presentation integration. Old scoped
APIs, world generation, approved prior atlases and simulation remain unchanged.

The stage1 receipt `g2-boundaries-cadence-red.xml` and first stage2 receipt
`g2-boundaries-preimport.xml` each ran49 cases (44 passed,5 failed), but four
submitted-renderer failures were a **fixture defect**, not valid routing RED:
ZoneRenderer's RequireComponent had already created a Tilemap; the fixture
added a duplicate, got null and injected it into the renderer. The corrected
fixture borrows its existing owned Tilemap and asserts it exists.

A cold review found a production policy bypass: a rejected known Wall role
fell through to the older atlas, whose semantic checks were looser. Eight new
world/portrait refusal cases plus missing-resource and legacy-API controls
produced `g2-boundaries-fallback-red.xml`:60 total,51 passed,9 failed. Eight
failures directly show a true glyph replaced by older native wall art; the
ninth is the still-absent resource. The corrected four submitted-renderer
cases, including real factory Wall→Rubble and one-pass memory tint, now pass.
Fix in progress: distinguish semantic rejection from unavailable optional art,
only within the new boundary-enabled presentation path.

The semantic fix passed the pre-import gate:60 total,59 passed, only the genuine
absent-resource assertion failed (`g2-boundaries-resource-red.xml`). Import used
the exact source PNG, SHA unchanged. The first importer attempt returned null;
the next exposed a cubemap shape inherited by newly created metadata. Explicit
Texture2D shape corrected it. The existing SpriteImportPostprocessor still
enforces Sprite type and alpha transparency for this directory; the asset is
a runtime Texture2D,256×72,RGBA32,Point/Clamp,one mip,uncompressed,sRGB,NPOT None.
The successful raw receipt is `g2-boundaries-import-3.json`; failed earlier
attempts are retained, not presented as successes. No importer production code
changed. Saved metadata pins the correct 2D shape for future imports.

`g2-boundaries-green.xml` now passes **60/60** native Unity cases, including the
actual Resources atlas/default world/default portrait bindings and all48 frame
rectangles. This is native integration verification; ordinary Play and the
bounded regression are still pending at this continuation checkpoint.

## Bounded runtime and ordinary Play acceptance — 2026-10-24 UTC

The final [regression](Verification/Live16x24/g2-boundaries-regression.xml)
passes **552/552** cases across20 relevant fixtures, including the60 focused
cases. Do not sum those counts. MCP reported an initialization timeout for
this run, but the fresh native XML completed successfully; TestRuns returned
to0 and SampleScene restored before Play. This is not the full project suite.

Root used the existing save-isolation/view helper with seed64, but no forced
art preference. Ordinary N started at40,12/tick10. See the matched
[G1 before](Verification/Live16x24/g1-normal-start.png) and
[G2 after](Verification/Live16x24/g2-normal-start.png). All8 new saved PNGs are
1280×800. Raw Screen observations2321×1139 are retained verbatim; the editor's
zoomed Game panel is not a physical Deck display.

Walking to44,7, bumping the wall and using C/Up/examine displayed Stone and
structure39/40 with the correct isolated portrait. Continued ordinary bump
attacks destroyed44,6; current owners became Grass/Rubble, and43/45 repainted
to clean separate end caps. The actual breach was walkable. Forty ordinary
strike turns still advance the world: enemies killed nearby NPCs and later
hit the player, whose HP reached26. No combat immunity or synthetic damage
was used to obtain the wall screenshot.

F6 restored the initial checkpoint for a fresh western walk. The player crossed
y16 to27,16 and then22,16, remaining40HP. Dark ruin courses were readable,
beam/quartz were separate, and walking/revealing changed connection masks.
At28,16 a separate Down press was blocked by the real worn crossing28,17;
tick220→230 and the log's strike confirm collision. The selected normal build
has no Vault on its hotbar; no skill was granted, so an ordinary Vault landing
and physical Deck targeting remain pending.

[Raw Play record](Verification/Live16x24/g2-play-verification.json) maps actions,
PNG hashes, state receipts and limits. The native submitted FOV test explicitly
marks dirty cells and proves **consumption** of FOV dirtiness; it alone does
not prove the producer. This separate ordinary walking/reveal route supplies
live producer evidence, without changing visibility through a tool.

### Performance and cleanup

The81.902s unwrapped editor profile contains23,629 recorded samples (23,627
frame-counter delta): renderer mean0.1891ms/p990.3323ms/max66.9321ms;
main-thread mean3.4567ms/p997.0273ms/max1084.4596ms; draws/batches mean38.68,
max42. It includes popup/pause, held destruction input, walking/incoming combat,
captures and idle/tool time. The max spikes are retained, not excluded.
There is no controlled matching baseline, isolated atlas-cost attribution,
GPU or physical Deck conclusion. The native warm adapter allocation counter
is a narrower, separate result.

The console exposed two Metal memoryless depth load/store messages (MCP labels
errors, editor UI2warnings/0errors). No production C# exception or compilation
error was observed during this Play pass. These messages are recorded rather
than claimed fixed. After stopping, the owned isolation/view/settings were
restored: playing/isolation/artPending/viewBorrowed allfalse, saveRootnull,
originalSampleScene. Caffeinate remains active by user request.

### Self-review and scope divergence

- 🟡 Fixed: rejected known walls previously escaped into less strict old art.
  Eight world/portrait RED cases drove the semantic guard; valid missing-art
  and old injected/scoped API counters pass.
- 🟡 Fixed fixture: duplicate Tilemap returnednull because RequireComponent
  already supplied one. Those four initial failures are not implementation
  RED evidence. Corrected fixtures use the actual owned Tilemap.
- 🟡 Fixed import: newly generated metadata imported the atlas as a cubemap.
  Explicit2D shape plus full import corrected it; the existing postprocessor
  still chooses Sprite/alphaTransparency. Source bytes remain unchanged.
- 🔵 Independent final source and actual-size visual reviews found no remaining
  significant issue. Visible runs, y16 passage, missing-wall caps and portrait
  read correctly; approved sprites and original presentation remain intact.
- 🧪 Bounded acceptance: keyboard collision/destruction/reveal/examine and normal
  launch passed. Physical Deck, Vault landing, all seeds/mutations and complete
  G2 scene acceptance remain explicit later gates. This is a CoO-original
  presentation change, not a claim of Qud source parity.
- ⚪ Conservative remembered caps intentionally do not preserve historical
  adjacency; this avoids querying live hidden topology. They may look broken
  in memory until revisited. No gameplay or historical-map system was added.

Implementation: FlatPixelBoundaryCatalog borrows one atlas and the canonical
selector, owns48 cached sprite/tile pairs, checks at most five distinct cells,
and resolves N/E/S/W masks. Greybox default construction lazy-loads the optional
family;12 existing injected constructors/old scoped APIs remain unchanged.
World and authorized subject portraits share eligibility; memory dim applies
once. Existing neighbor expansion handles repaint and disposal releases only
owned objects. There is no new renderer, frame-wide scan or gameplay mutation.

Files: new boundary catalog/meta and focused test fixture/meta; modified Greybox
integration; new Resources atlas/meta; editable source and exporter/validator;
concept prompts/references, this record, live brief/completion/coverage updates,
and named native/ordinary receipts. Next: pond banks, road and soil finishing
from the accepted actual opening views. Keep the accepted masonry stable unless
a specific new defect is demonstrated; the opening release remains incomplete.
