# Sodden travel material coverage

Status: the first integration has actual gameplay evidence. The revised 28-frame
atlas, ground continuity and direct address lookup pass 304 native cases. Three
allocation checks are inconclusive after their positive control returned zero.
District Editor gameplay, restart and profiling are verified and pushed as d48f623f5. Ordinary six-formation reuse is now also verified below; current packages remain the earlier Sill batch until the next delivery step.

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

## Ordinary formation extension — started 2026-10-27 05:54 UTC

District source d48f623f5e66f4c9f3bb3a3b47a880e9eb9a5ea5 is verified on origin/main.
Reuse the frozen kit in six naturally generated ordinary Sodden formations. The
preimplementation plan is `Verification/Live16x24/sodden-ordinary-next-plan.md`.
Root is capturing real before views from the earned works checkpoint; the
separate native cases will fail against the current district-only catalog before
the scope change. Keep a bounded canonical coordinate table and current live
map/POI/cached-graph/saved-lair checks. Copse uses plain water, not mire. No new
asset, generation, animation or content admission framework is required.

Ordinary baseline reached all six exact candidates through real world-map
controls with unchanged HP38. Before captures and earned checkpoints cover
15,8 BogFace tick2210;16,8 ReedMaze2250;17,8 DrownedCopse2290;18,8 OpenMire2330;
19,7 Causeway2390;15,5 PeatCuts2590. Two input-route corrections are recorded,
not hidden. The ordinary native scope is still unchanged at this baseline.
All sources remain actual seed65 graphs; no POI erasure or forced reveal.

Ordinary scope RED: all22 new native cases failed against the district-only
implementation after a clean compile. Saved report: `sodden-ordinary-native-red.xml`.
The runtime change is confined to the catalog scope: one bounded canonical
coordinate table, the same live biome/POI/current-graph checks, and read-only
saved-lair exclusion. Exact owner/state/material and resource handling are
unchanged. The full Sodden plus global presentation suites are now running.

Plan refinement: the proposed extra ordinary missing-atlas test is not duplicated.
Existing district injection tests already exercise the unchanged constructor,
optional-resource independence and truthful hazard fallback; the extension adds
no separate resource branch. The complete Sodden fixture retains those checks.

Ordinary extension native GREEN:186 cases total,183 passed,0 failed,3 pre-existing allocation probes inconclusive. All22 new ordinary cases pass. Full134-case Sodden fixture and52 global presentation cases include actual generated six-formation owners, plain-water submitted marks, legacy/current saved-lair exclusion and dirty board destruction.

### Ordinary Editor acceptance — 2026-10-27 06:13 UTC

All six natural formation entries now render the accepted material set. Twelve
original before/after1280×800 captures preserve the same seed, coordinate, tick,
view and selected-blueprint totals. Independent inspection found no changes in
blueprint or serialized tile state at common detailed-cell coordinates. Ground-only
Grass is omitted from after detail arrays by receipt design; numeric entity IDs
differ after the first entry as later zones are independently reached/generated.
No identical-ID or entire-world equality claim is made. See
`sodden-ordinary-matched-summary.json` and the independent visual review.

The real route includes2×causeway inspection, PeatCuts40,12→40,17 and a native
PeatBank Examine10/10, then walking east and crossing15,5→16,5→15,5. A bandfrog
encounter and mire coating caused actual HP loss38→4. That state was retained,
not healed for a picture. F5 saved turn3120 at79,12; fresh Play Continue restored
the exact zone/position/health and compared paint cells, ground and counts.
Captures07–12 and the earned after-route fixture preserve these observations.
This verifies ordinary gameplay rendering; it adds no combat mechanic.

The80.45-second Editor profile covers BogFace→ReedMaze→Copse world travel plus
idle/tool time, not all six scenes or the later walking/combat. Renderer mean
0.103ms,p990.207ms,max63.20ms; main mean4.30ms,p9915.64ms,max920.00ms; batches
and draws peak34. No controlled baseline, allocation, Linux execution, physical
Deck or sustained-hardware performance claim. Native results remain183passes,
0failures,3allocation-inconclusive; all22 additions passed after22 genuine RED.

Fullness is **reviewed but incomplete** in every formation. Reed braids, peat
faces and routes are legible; plain water remains blue-wave glyphs with black
backing, regular grass repeats, and wide duckboard layouts repeat a lattice.
Actual missing owner families include frogs/Greatdew/MawToad, containers, acid
and brine, VineWall, SteamVent/StairsDown, Merchant/CopperPipe/PeatBog, plants
and crops. Fog/memory and intentional tactical approaches are not bare-land
evidence. No new scatter or generation was justified by these views.

Self-review: 🟡 fixed the district-only coordinate restriction and retained live
saved-lair exclusion; independent source review has no actionable defect. 🧪
Allocation probes remain inconclusive. ⚪ Named visual residuals and all-region
coverage remain open. The next source packet plan is
`sodden-services-next-plan.md` (preimplementation plan only); broader Beating,
settlements, underground and other regions remain in the world ledger. First
refresh ordinary packaged delivery after exact source push. Approved pixels,
normal saves, foreign edits and prior builds remain protected; no cleanup.
Last hourly report05:25UTC; next06:25UTC.
