# Opening glade: vegetation hierarchy

Status: **195/195 native cases passing; final seed 64 and 65 composition,
ordinary interactions and plant-owner save-return accepted within scope**,
2026-10-26. This bounded glade vegetation pass is accepted. Graphics remains
the release priority; whole-opening, regional and device acceptance remain
open. Continue with the current East scene assessment rather than reopening
accepted vegetation without a named defect.

## Named defect and actual capture

The actual fresh Mac player capture
`Verification/Live16x24/packaged-flat-20261026-mac-new-game.jpg` shows the pond,
working field and ruin, but a broad rectangular pale-reed carpet competes with
them. Evenly sprinkled grass gives quiet ground the same visual weight everywhere.
The native art itself remains approved. The fixed geometry is already playable.

Built-in imagegen reinterpreted that exact screenshot using
`ArtSource/Live16x24/concepts/glade-composition/vegetation-prompt.txt`;
`vegetation-v1.png` is a concept only. Accept unequal tapered reed drifts and
quiet field approaches. Reject its softened pixels, altered UI and painted
texture. Production reuses existing native sprites and real Bush/Reeds owners.
No generated image is placed over the game.

## Verification sweep / corrections

| Source | Finding and implementation consequence |
| --- | --- |
| ReferenceGladePlan constructor | Dressing is actual entities generated after fixed discoveries. Recompose only designated dressing placements before the read-only plan is published. |
| AddDiscoveries / IsDiscoveryGround | Pond fringe, finite basin, crops, shelter and ruined doorway are protected. Keep their positions and order unchanged. |
| IsApproach | Border bands, central cross and y16 stay authoritative. Add quiet apron space without adding collision or a painted path. |
| ReferenceGladeBuilder | Entire zone is staged transactionally and occupied graphs are refused. No changes required to persistence, gameplay factory or renderer. |
| Bush / Reeds blueprints | Bush carries Destructible/Plant/Thermal parts; Reeds is ordinary vegetation terrain, not the same destructible/fuel shape. Preserve per-seed blueprint counts and the ordered blueprint/VisualID slots; placement changes can affect tactical fuel distribution and are disclosed. This plan-level pin does not promise identical runtime entity IDs between independently generated worlds. |
| Hash / caller RNG | Reuse coordinate hashing and stable ordering. No caller-RNG consumption, per-frame work, extra atlas or draw layer. |

## Bounded implementation plan

1. Record original pure layouts for seeds 0, 1, 64, 65, 1729 and int extremes, with
   fieldwork on/off. Write native tests first and confirm RED for new hierarchy.
2. Keep all non-dressing owners fixed. Relocate only grass-Bush dressing and
   eastern pale-Reeds dressing (x>=52), preserving their exact per-seed budget
   and original placement-list order. Preserve all pond/west reed placements.
3. Assign eastern reeds to three unequal overlapping/tapered drifts using
   normalized elliptical affinity. The walked review required a refinement:
   shared empty candidate cells plus coarse and fine edge variation, recorded
   below, replace the initially insufficient small edge variation.
   Leave a quiet field apron and the existing cross open. Cluster remaining
   grass-Bush dressing outside those gaps. Rank only valid unoccupied cells;
   retain all original Grass terrain, fixed rubble and broken-column owners.
4. Verify count/functional-footprint conservation, deterministic distinct seeds,
   unique occupied cells, quiet gaps versus populated cluster controls, four
   border/service reachability, ordinary outside/occupied authority and save
   preservation. Run original glade/adversarial/route suites with new tests.
5. Root owns Unity. Compare normal fresh seed 64/65 framing and ordinary movement,
   basin/field examine, western approach, cellar descent/return and save/reload.
   Keep original user saves isolated. Share actual captures and review the
   largest mismatch; adjust only if a named visual defect remains.

## Parts and owner/state mapping

| Part | Owner / source | States and bounds |
| --- | --- | --- |
| Pale reed drift | Reeds / reference-glade-pale-reeds, existing opening atlas | Current live vegetation-terrain entity; removal and visibility still own its state. No new fire/destruction behavior or painted backdrop. |
| Olive grass cluster | Bush / reference-glade-green-grass, existing opening atlas | Existing destructible/combustible behavior and saved position. |
| Quiet apron | Original Grass / reference-glade-ground | Empty readable terrain, no new resource, route marker or collision. |
| Fixed landmarks | Existing pond, fieldwork, roads, rubble, walls, discoveries | Positions, stock, glyphs and sprites unchanged. |

No new raster production is needed: the concept extracts arrangement of approved
editable native assets, not a new sprite family. A new shape would need the full
native-art loop before admission.

## Current review / honesty bounds

- 🔵 Native history and independent source review found no remaining material
  source defect. Final seed 64 and 65 walked visual reviews accept the named
  grouping improvement; this does not certify the full scene or other regions.
- 🔵 Final seed 64 and 65 outside-zone save/load and return preserve their
  recorded plant-owner graphs exactly. The final seed 64 eastern view is at
  tick 730 and Dusk, after an unintended pond leg; it is not a matched-turn
  replay of seed 65 or iteration 1.
- ⚪ Fresh generation only; saved graphs keep their existing vegetation.
- ⚪ Exact fuel/plant budget is preserved, but tactical positions intentionally
  change. Do not call this entirely cosmetic or a balance/performance result.
- ⚪ Mac/Deck packages built from `c893ae58f` precede this layout. No updated
  package or physical Deck result is established by the editor checks.
- ⚪ No new atlas, shader or per-frame drawing work was added. This is a source
  property, not a new frame-time, draw-call or device-performance measurement.

Next action: retain the current East approach/worksite captures as the next
scene baseline, finish root's editor/save/view restoration, and review/preserve
the exact owned changes on main. Use the East evidence to choose the next
visible composition defect. Keep the remaining opening and regional graphics
gates explicit; this record does not claim the East review is already complete.

Pre-implementation correction: this Unity NUnit version lacks Assert.Multiple.
The first request compiled zero matching cases; retained preflight receipts
record that fixture error. Sequential assertions retain the same invariants.
No behavioral RED count is claimed from that request. Existing fieldwork tests
also pin a two-cell-only difference: exclude (46,4)/(46,6) from destination
selection in both modes so fieldwork does not reshuffle other dressing.

## Iteration 1 — native checks and ordinary walking, 2026-10-26

Native original-plan RED: 63 cases, 21 passed and 42 expected failures. Initial
candidate GREEN: all 167 cases passed across glade composition 63, original 24,
adversarial 33, fieldwork 10 and route 37. These checks preserve exact original
blueprint budgets, fixed footprints and ordered blueprint/VisualID slots across
seven seeds with both fieldwork settings. They do not establish identical
runtime entity IDs across independently generated worlds or visual quality.

Fresh ordinary Warden games at seeds 64 and 65 match their pre-change screen at
(40,12), tick 10, 40 HP and 50 drams. Seed 64 walks to the basin at (36,9) and
reads its real three-dram description from (36,10). It walks y=16 to the ruin;
the preexisting wall at (28,17) still blocks a straight south line, so the
actual route detours via (27,16) → (27,19) → (28,19). Cellar descent reaches
(40,12) at tick 300; F5/F6 occurs there, then ascent returns to (28,19) at
tick 310. All 450 Bush/Reeds IDs, positions and
VisualIDs match before/after, SHA256
`3bc0b51193c4378f346bd53a6326ca3d16e53a06298df683d46254c379786a68`.
This is ordinary controls plus read-only observation, not a forced scene.
The initial extra Escape opened Pause; no movement from that attempt is counted.

Seed 65 reads the field sign at (43,11) from (42,11), crosses the field apron,
walks to (60,4) and observes the full eastern reed groups at tick 290, 40 HP
and 50 drams. The original
spawn pair improves field/ruin hierarchy, but this walked view reveals a notable
defect hidden by fog: solid elliptical/kite shapes and rectangular grass patches
look planted rather than wild. **Iteration 1 was rejected as unfinished.**
Independent visual review agrees; independent source review found no actionable
P1/P2 source issue. Pond cells are protected by the discovery rectangle;
fieldwork's two-cell exclusion and all-or-nothing placement are sound.

Next refinement: conserve the same owners, quiet aprons and y16 cross; fray the
shapes and admit small interior gaps. Add shape counter-checks before changing
the runtime; retain all previous pins and repeat native/ordinary checks at the
walked eastern view. No new sprites or changes to approved assets are needed.
Retain iteration 1 screenshots as rejected-candidate evidence. Seed 65 was stopped
and its scene/view/preferences/save isolation restored before refinement.

## Iteration 2 — frayed live-owner groups

New shape RED: 91 cases, 63 passed and 28 expected failures. All 14 seed/fieldwork
variants had too few interior gaps (0–2) and long solid eastern rows (15–17 cells).
Keep these broad visual-structure controls independent of the implementation;
retain actual Play as the quality gate.

The first refinement excluded 26% of otherwise eligible cells with a shared stable
coordinate mask, allowing genuine ground pockets rather than filling reed gaps
with Bush. It adds coarse and fine affinity variation to make group edges
uneven. The original number of each owner is redistributed to the remaining
valid cells. Fixed terrain, discoveries, west reeds, approaches and ordered
blueprint/VisualID slots remain protected. No per-frame rendering work is added.
If future fixed scenery makes capacity insufficient, the whole original layout
is preserved; no partial publication or silent resource reduction.

The first fray candidate (26% pockets) passed 191/195 native cases but retained
13–14 cell straight reed rows in seeds 1 and 64. It was not GREEN. The final
candidate retained the shape contract and widened shared empty candidates to
36%, conserving owner counts through redistribution. This is the percentage of
otherwise eligible candidate cells excluded by the mask, not a claim that 36%
of every visible patch is empty.

## Native verification history

All times below are UTC on 2026-10-26. Totals overlap between runs and must not
be added together as distinct coverage.

| Run | Observed result | Meaning |
| --- | --- | --- |
| Initial import request | Zero matching cases executed | NUnit lacked `Assert.Multiple`; this was a fixture compile error, not behavioral RED. |
| Original-layout RED, 04:20:26–04:20:28 | 63 total: 21 passed, 42 failed, 0 skipped | New quiet approach, upper drift and Bush clustering contracts failed against the original layout. Sequential assertions stop at their first failure; not every later assertion was separately demonstrated RED. |
| Initial candidate, 04:22:14–04:22:26 | 167/167 passed, 0 skipped | Code invariants passed, but actual walking later rejected its solid geometric plant groups. |
| Shape RED, 04:39:44–04:39:45 | 91 total: 63 passed, 28 failed, 0 skipped | Each of 14 variants failed the small interior-gap and long filled-row checks. |
| 26% gap candidate, 04:41:19–04:41:31 | 191/195 passed, 4 failed, 0 skipped | Straight-row failures remained for seeds 1 and 64 in both fieldwork modes. |
| Final 36% gap candidate, 04:43:13–04:43:24 | **195/195 passed, 0 skipped** | Final fixture 91, original glade 24, adversarial 33, fieldwork 10 and route 37. |

Authoritative summaries and native XML share these stems under
`Docs/Verification/Live16x24/`: `glade-vegetation-red`,
`glade-vegetation-green`, `glade-vegetation-fray-red`,
`glade-vegetation-fray-green` and `glade-vegetation-fray-final`.
The filename `fray-green` does not override its recorded four failures.

The pre-change native plans are retained in
`Verification/Live16x24/glade-vegetation-original-layouts.json.gz`; decompressed
SHA256 is `9cc4bb92ce02e56652a3e91d1fa83727a20738f3431f9f676bc63c26875397d3`.
The test pins derive from those original receipts, not the changed algorithm.
An independent source review found no material defect in the final bounded
implementation. Native results establish the asserted generation contracts;
actual visuals and gameplay remain separate evidence.

## Final seed 65 visual review and ordinary return

The actual walked comparison uses
`glade-vegetation-after65-east-drifts.png` (rejected iteration 1) and
`glade-vegetation-final65-east-drifts.png` (final candidate), with their adjacent
JSON receipts. Both are 1280×800 in `Overworld.11.10.0` at (60,4), tick 290,
40 HP and 50 drams. This is matched framing and player state, not an identical
world/combat comparison: the NPC combat logs and outcomes differ.

Independent visual review accepts the bounded final improvement. The three
unequal reed groups now have real interior green gaps and irregular edges;
the solid upper oval, middle kite and lower oval no longer dominate. Small
Bush patches also lose their rigid planted-bed appearance. The final spawn
capture `glade-vegetation-final65.png`, at (40,12) and tick 10, preserves the
quiet field and ruin approaches, player prominence and the distinction between
cultivated rows and wild vegetation. Native row repetition remains visible in
dense patches. Thin masonry, broad fog silhouettes and small interactable
silhouettes are preexisting limitations outside this pass.

Ordinary keyboard input reads the field sign from (42,11) at tick 40, crosses
the eastern apron and reaches the walked comparison. The return follows
(27,16) → (27,19) → (28,19), uses Shift+period to descend to cellar (40,12)
at tick 790, saves and reloads there with F5/F6, then uses Shift+comma to return
to glade (28,19) at tick 800. All recorded stages retain 40 HP and 50 drams.
Receipts and actual screenshots are `glade-vegetation-final65-field`,
`glade-vegetation-final65-cellar`, `glade-vegetation-final65-loaded-cellar` and
`glade-vegetation-final65-return` under `Verification/Live16x24/`.

The 447 recorded Bush/Reeds owners retain exact IDs, coordinates, blueprints and
VisualIDs through this outside-zone save/load and return. The before and after
files, `glade-vegetation65-final-owners-before-return.json` and
`glade-vegetation65-final-owners-after-return.json`, are byte-identical with
SHA256 `087d81d56738c7e9c1f1b88751d06c04c3bea7168dc23b0dd3c41ac62aada6a2`.
That exact runtime-ID assertion applies to this one preserved world across
return, not to separate new games or the native plan-level sequence pins.

## Final seed 64 visual and ordinary return acceptance

Final-candidate fresh seed 64 is captured at (40,12), tick 10, 40 HP and
50 drams in `glade-vegetation-final64.png` and its JSON receipt. Ordinary
movement then reaches (36,10), tick 70, and opens the real basin at (36,9);
`glade-vegetation-final64-basin.png` and its receipt record the interaction.
The verified eastern capture is `glade-vegetation-final64-east-verified.png`,
with its adjacent JSON receipt: actual (60,4), tick 730, 40 HP and 50 drams.
The HUD reads Dusk and discloses Soaked after an unintended pond leg. The
earlier attempted image initially named `glade-vegetation-final64-east-drifts`
actually records (36,4), tick 250. Its PNG and JSON are now retained as
`glade-vegetation-final64-unintended-pond-route`; that failed route is excluded
from eastern-view acceptance.
Do not claim a matched tick, identical lighting or identical world/combat
against the other walked captures.

Independent review accepts the bounded final seed 64 composition under these
observed conditions. Spawn retains clear field/ruin approaches; the real basin
reader remains legible. In the verified eastern view, visible gaps and uneven
edges break the solid plant stamps without losing the upper, middle and lower
groups. The player remains distinguishable within the upper reeds, and no
material noise regression is apparent. Some dense-patch row rhythm remains;
the preexisting masonry, fog and small interaction silhouettes are not accepted
as finished by this check. These are actual editor captures, not concept renders.

The final route continues through the eastern border into
`Overworld.12.10.0`, at (0,16), tick 1050. Ordinary F5/F6 there retains the same
position, tick, 40 HP and 50 drams, recorded in
`glade-vegetation-final64-loaded-east.png` and its JSON receipt. A Left input
then returns to glade (79,16), tick 1060, with the same HP and currency;
`glade-vegetation-final64-return.png` and its receipt record that arrival.

All 450 recorded Bush/Reeds owners preserve their IDs, coordinates, blueprints
and VisualIDs across this outside-zone save/load and return. The final files
`glade-vegetation64-final-owners-before-return.json` and
`glade-vegetation64-final-owners-after-return.json` are byte-identical, SHA256
`fc8be8528b22c561c02d400cdc2bfa89838b3b9f520d247b3989eadf51313c41`.
This is separate final-candidate evidence; the iteration 1 cellar return above
remains historical and is not counted again. The shared native coverage remains
195/195; visual review and ordinary return do not add another native test run.

## Bounded acceptance and next scene

Both observed seeds now pass the final native-scale composition review and
their own real plant-owner save-return checks. Keep the quiet apron, grouped
but interrupted vegetation and approved sprite assets stable. Root obtained
current East entry and contested near-forge captures and restored normal editor
state at 05:04:43 UTC; those captures start the next assessment, not a claim of
East completion. Select the
next scene-level improvement from actual play and its existing owners, using
the same capture, concept, native integration and review loop.

This work can verify bounded native generation contracts, the observed final
seed 64 and 65 native-scale compositions, actual input/interactions and the
recorded seed 64 and 65 plant-owner save-return continuity. It cannot verify
all seeds, every owner state, the full opening, other regions, new packaged
execution or physical
Steam Deck readability/performance. The existing Mac/Deck packages from
`c893ae58f` still precede this layout. Approved sprite pixels, editable sources
and the reversible former presentation remain preserved.


## Final self-review and source preservation

- 🟡 Fixed before acceptance: initial native-passing groups still looked like
  solid ellipse/kite stamps during real walking. The second pass adds real
  ground pockets and uneven edges; the final36%-pocket tests and both
  observed views now pass the bounded contract.
- 🔵 Independent review found no actionable source defect. Shared gap masking
  prevents Bush from filling reed clearings; stable sorting preserves ordered
  Blueprint/VisualID slots, fixed discovery footprints and exact budgets.
- 🧪 Direct synthetic insufficient-capacity fallback injection is deferred;
  reviewed code reserves cloned occupancy and publishes neither family unless
  both destination lists are complete. Current14seed/fieldworkvariants pass.
- ⚪ Saved graphs keep old vegetation. Bush fuel positions change in fresh
  worlds. Sparse-patch row rhythm, wall/fog treatment, full regional coverage,
  packaged glade execution and physical Deck checks remain outside acceptance.

`glade-vegetation-final-preservation.json` records26 importer whitespace-only
metadata changes backed up and restored, four unchanged normal save/backup
hashes, two unchanged unrelated PNG variants, and about430GiB free. No source,
cache, build or save was deleted. Both final root-owned play isolations ended,
normal scene/view/preferences/save pointers restored; dedicated receipts are
`glade-vegetation-final64-restored.json` and `final65-restored.json`.

Files: `ReferenceGladePlan.cs` is the only runtime change;
`GladeVegetationCompositionTests.cs` and its fresh `.meta` add91cases. This
living record, the master art brief and release-coverage ledger accompany the
change. `concepts/glade-composition` holds prompt/concept; `glade-vegetation*`
verification records retain original baseline, RED/GREEN, rejected candidates,
ordinary captures and restoration evidence. The next East plan/concept and
current real before-captures are explicitly unfinished design evidence.

Scope divergence: no new sprite pixels were necessary. The concept's useful
part was arrangement of existing approved native owners. Built-in imagegen's
soft texture and modified UI are rejected; the actual game uses unchanged
editable sprites. The redundant5.55MB raw baseline remains local and is exactly
represented by the committed121KB gzip; its decompressed SHA is listed above.
