# Sodden travel material coverage

Status: the first integration has actual gameplay evidence. The revised 28-frame
atlas, ground continuity and direct address lookup pass 304 native cases. Three
allocation checks are inconclusive after their positive control returned zero.
Final Editor gameplay, restart and profiling are verified. Main delivery is being finalized; the current packaged build remains the earlier Sill batch.

## Purpose and scope

Graphics is the primary release blocker. Fill missing static sprites across every
biome and area, and judge whether whole chunks feel coherent and sufficiently
populated. Nuanced new animation is deferred. This slice covers the actual Sodden
crossing (16,7), dressing shelter (15,7) and peat works (17,7). Ordinary wilderness,
other regions and remaining object families stay explicitly open.

The original native atlas supplies Duckboard connections, filled and depleted
MirePool surfaces, PeatBank and DeadTree. Reeds, Grass, StoneFloor and StoneWall
borrow unchanged approved images. Current real owners, visibility, materials,
custody, state and graph identity authorize every substitution. The global
opening admission list, world generation and saved geometry are unchanged.

## Production pipeline and references

The actual seed-65 game was played through normal controls and captured before
artwork. Imagegen reinterpreted the east crossing capture with the shelter as
context and accepted Sill graphics as the style anchor. The exact prompt, concept
and extracted owner/state decisions are in
`ArtSource/Live16x24/concepts/sodden-travel`.

The material reference is the [NPS Forest Loop boardwalk](https://www.nps.gov/places/forest-loop-trail-boardwalk.htm).
The page and official image-search results informed weathered timber; the direct
image request timed out. No photographic or concept pixels became production
sprites. Original integer-cell Blender geometry, editable collections, source
cells, manifests, contacts and source verification are retained in
`ArtSource/Live16x24/sodden-travel`. `Tools/Live16x24/build_sodden_travel.py`
reproduces the native pixels. Blender's PNG metadata varies between renders;
identical decoded pixels do not imply identical encoded file hashes.

### Verified corrections before implementation

| Initial assumption | Source-grounded correction |
| --- | --- |
| Mire is ordinary water | Its literal liquid is bog-mire. Keep its hazardous identity and distinct olive material. |
| Empty stock means dry land | The independent tile source can retain or renew mire at zero stock. Depleted art still depicts wet film. |
| Source.Seed is the only coating producer | Zone.AddEntity also projects the real pool coating. Hidden-source tests must distinguish that independent state from disclosure of the owner. |
| A matching biome or glyph is enough | Require the exact current graph, district address, biome/no POI, owned semantic parts and a supported visible state. |
| The old wave exposes charge | The generic coating wave precedes energy marks. Relinquish only the duplicate sole-live mire wave when its native body is actually submitted. |
| More scattered objects will fix empty chunks | First render existing routes, colonies, structures and service owners; then judge actual placement gaps in whole views. |

The detailed source sweep is retained in
`Docs/Verification/Live16x24/sodden-travel-preflight.md`.

## Actual baseline

An unchanged earned Sill checkpoint (seed65, turn1170, player67,10,38HP) entered
world-map cell16,7 and descended to crossing20,12. F5 saved turn1370. Ordinary
walking followed20,12→20,3→60,3. World travel then reached shelter15,7 at40,12;
walking reached the real dressing room34,8. There was no teleport, forced reveal,
regeneration, invincibility or tool-created scenery.

Five original 1280×800 captures are named `sodden-before-01` through `05` in the
verification directory. Existing submitted-paint receipts observed the crossing
entry's433 MirePool,19 Duckboard,20 PeatBank,12 Reeds and560 Grass cells as fallback
art. These are visible cell counts, not a whole-world denominator. The receipt's
Screen dimensions reflect the tool's Editor context; PNG dimensions establish the
captured Game output. Earned before-entry, shelter and room fixtures are retained.

## Native implementation and checks

The 99-case initial test import needed one fixture correction: internal
Entity.SpatialZone was replaced by public Zone.GetEntityCell identity. The genuine
behavioral RED then failed96 and passed3 cases. Saved runtime implementation
passed99/99. A scripts-only refresh initially missed the new source file; full
asset refresh resolved that import issue without changing production logic.

Independent review found the cultivation gameplay helper could erase native
ground because of a hidden owner. Two native counterexamples failed in Sodden
and Sill. Removing the early helper call retained the existing disclosed-owner
checks, and295 selected cases passed. Root started Play before the test runner
finished its post-run scene restoration, causing an InvalidOperationException.
This workflow error is retained; subsequent transitions wait until the native
TestRunnerApi reports no active run. The295 individual assertions passed.

The exact-mire representation flag is separate from water and brine. Full and
incremental drawing agree. Filled and empty wet pools retain actual coating
state; charge, heat, cold, residue and mist markers remain visible. Mixtures,
unsupported owner effects, cover, fog, stale graphs and missing resources keep
truthful fallback. Board destruction updates neighboring connections without a
full redraw. Queries reuse cached tiles and leave simulation state unchanged.

## Actual first integration and iteration

The unchanged turn1370 fixture replayed the crossing route. Native Duckboard
Examine displayed its actual material and8/8 structure. After returning to the
world, the player moved to59,3 and used ordinary Break on58,3:8→5HP, then removal.
The next interaction selected underlying Grass, and the rendered route had a gap.
Capture06 records a reader-exit step, not destruction. Capture08 records removal.

An intended world-map west step repeated twice and entered14,7; capture09 is
explicitly this outside-district fallback view. Ordinary travel corrected to15,7.
Capture10 shows shelter entry40,12;11 is room34,9;12 is the matched34,8 room view.
F5 atturn2090 preserved the earned board removal and shelter state. All owned
session saves were archived, and normal saves, gameplay preferences and38 foreign
tracked changes were verified unchanged.

Whole-view review found two named visual defects: bright mire shapes repeated
across every tile like carpet, and black backing interrupted actual Grass beneath
Reeds, DeadTree and Duckboard. The original22-frame source is preserved in pass2.
The revised28-frame atlas keeps all20 non-mire frames unchanged:16/17 are quiet
filled/wet-film bases,22–24 are sparse filled variants,25–27 are sparse depleted
variants. A stable coordinate choice uses the quiet base roughly75% of the time.

Ten native tests failed before the visual correction, including the actual
submitted ground path. Qualified dry overlays now retain the real Grass or
StoneFloor beneath them. Live coating, cultivated soil, unknown terrain, custom
states and absence of a real lower floor still prevent that substitution. The
revised source and runtime passed304 selected native cases. Final actual views
and the small scope-check optimization remain in progress.

## Remaining coverage and continuation

Fullness is not yet accepted. Actual room receipts show70 selected floor cells
but only a fallback dressing bench and peat cutter. The exterior notice and
Sumpsieve stages, and the crossing guard/post, are also observed gaps. Render
these real functional identities before deciding whether new furnishings are
needed. Existing grass stippling is a named scene-scale review candidate; do not
change accepted source art without inspecting the corrected whole view.

The next source audit identifies six ordinary Sodden formations and57 candidate
addresses before live POI/lair exclusions. DrownedCopse uses plain-water coating,
which must remain distinct from mire. No generation defect is established by
that read-only audit. Beating and every other regional, underground and special
area remain on the broader coverage ledger.

Next: final gameplay comparisons, real save/revisit and profiling, independent
review, exact main push, then ordinary Sodden coverage. Current packaged builds
contain the delivered Sill batch; new Sodden package delivery is pending. No
Linux execution, physical Deck or sustained hardware performance claim is made.
Storage remains inspection-only. Last hourly report:05:25UTC; next:06:25UTC.

### Final source check — 2026-10-27 05:41 UTC

The combined final run has 307 cases: 304 passed, 0 failed and 3 inconclusive.
The native XML is retained as `sodden-travel-native-final.xml`. The three
allocation probes also read changed live map authority, but their retained-array
positive control returned zero, so they provide no measured allocation claim.
Independent review identified repeated address splitting in the cell lookup;
the finite validated district addresses now map directly to x=15/16/17, y=7.
The live cached graph, biome and POI checks remain. The review closes this
source-level finding without claiming a measured performance improvement.

### Final gameplay and fullness review — 2026-10-27 05:48 UTC

Thirteen original Game captures show the final 28-frame source. Matched crossing
entry and east-board views keep the original owner counts and positions. The
quiet mire base occupies 74.6% and 76.0% of their selected pool cells. The bright
repeated mound pattern is gone, and actual Grass now continues under visible
boards, reeds and the dead tree. Existing dark remembered cells remain fallback.

Ordinary travel reached all three districts. The shelter room at 34,8 again
shows 70 floor and 39 wall cells, with the real bench and worker still glyphs.
The peat works at 40,12 shows 37 boards, 108 mire cells, 59 peat banks, 13 reeds,
41 selected floor cells and 49 walls; Merchant and salvage remain glyphs. Normal
Examine shows the board's Wood/8 HP and peat bank's Peat/10 HP with native portraits.
Walking to works 39,16, F5 at tick 2110 and a new isolated Play session's ordinary
Continue preserve HP38, position and the complete visible paint receipt exactly.
This is an Editor restart; no new packaged-process result is implied.

**Fullness remains open.** The corrected materials pass this bounded iteration,
but regular grass stippling and missing service, crop and actor sprites still
weaken scene composition. Next render those real identities and reuse this kit
in the six ordinary Sodden formations. Only then judge whether specific areas
need additional generated objects. Do not add random clutter to clear routes.

### Performance and honesty bounds

A 67.1-second Editor record includes 49 normal route steps, zoom and Examine,
plus tool and idle time. Renderer marker mean 0.306 ms, p99 0.257 ms, maximum
59.71 ms; main-thread mean 4.99 ms, p99 21.07 ms, maximum 1476.42 ms. Draw calls
and batches peak at 38. The maxima are retained, not averaged away; there is no
controlled baseline or isolated attribution for tool-time stalls. This is not
sustained hardware performance acceptance. All recorders were disposed.

Can verify: saved source/import, 304 native passes, actual ordinary traversal,
matching native readers, literal paint and save/reload continuity. Can inspect
visually: the 13 original stills, cleared dry-ground gaps and reduced mire motif
repetition. Cannot claim: all-world coverage, finished chunk fullness, ordinary
wet-film depletion play, Linux execution, physical Deck or packaged Sodden delivery.

### Self-review and next action

🟡 Fixed hidden cultivation ground leak, bright mire repetition, missing dry
substrates and repeated address splitting. Independent source/runtime/whole-view
review retained. 🧪 Allocation measurement remains inconclusive; retain all
three raw results. ⚪ Actual fallback owners and scene fullness remain explicit
work, not accepted coverage. No core mechanics or generation changed.

Owned files comprise the Sodden catalog/texture/test and metadata, four shared
renderer files, editable source/concept/generator, bounded captures and native
receipts, and these living coverage records. Normal saves, preferences, approved
source assets, current packages and foreign tracked work are protected. Storage
inspection only. Next: push exact reviewed source; extend ordinary Sodden scope
using `Verification/Live16x24/sodden-ordinary-next-plan.md`; then regional services,
Beating and the remaining full-world queue. Last hourly report remains 05:25 UTC.

Final protection: normal Editor save tree997 entries unchanged, standalone saves still absent, gameplay preferences unchanged in both domains,38 foreign tracked files unchanged. Earlier protected north-motion packages and current Sill Linux files/archive verify exactly. Current Sill Mac exists; its prior receipt lacks a tree digest, so historical byte identity is not claimed. Unity is outside Play/tests, clean SampleScene and null isolation root; owned test roots are archived. No cleanup performed.
