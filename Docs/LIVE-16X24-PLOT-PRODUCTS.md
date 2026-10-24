# Live 16×24 — north plot products

## Status and purpose

**Current status,2026-10-24:** ordinary acceptance is partial. Actual Pitchpod
harvest/pickup and carried resin Examine are verified; ground Examine,
planting, clod preparation and other product loops remain open. See the
[recovered ordinary continuation](#recovered-ordinary-continuation--2026-10-24)
below. The Mac lock is no longer the current blocker.

Initial implementation record,2026-10-23: native source, import, implementation and code review complete;
ordinary-play acceptance pending because the Mac is locked.
This bounded packet follows the accepted real harvest/revisit in
`LIVE-16X24-NORTH-PLOT.md`. Eight loose products still fall back to generic
marks after harvest or purchase. Give each a truthful native identity on the
ground and in authorized examine views, preserving current ownership and
all gameplay. It does not activate the presentation in ordinary builds.

Qud is a readability reference only. These are original CoO sprites; no
decompiled art, copied assets or implementation-parity claim.

## Verified source corrections before implementation

| Initial assumption | Source-verified contract / consequence |
|---|---|
| Every food has a preparation action | CandyCarrot owns Food, but no Cookable. Its `Cooking=Meal` is a flavor tag. Depict raw gladroot, not a cooked meal or healing promise. |
| All seeds require tilled ground | CandyCarrotSeed and EmberwheatSeed have `RequireCultivated=false`; MarlrootSeed and PitchpodSeed have it true. Preserve the exact owned crop target and cultivation flag. |
| Marlroot clod is refined fire clay | Its owned BotanicalProcessingPart produces FireClay, bounded to 1–8 units. Keep root strands on raw clod, distinct from the existing refined-clay frame. |
| Resin is a flaming object | PitchpodResin owns Reagent with positive heat and combustible properties; GetProperties caches by raw snapshot. Use semantic parsed properties, no string-order pin, no flame or vessel. |
| Sludge has a CompostPart | InertSludge is an ordinary portable exact blueprint consumed by existing compost logic, also a finite brewing fallback. No invented part, container, liquid pool or growth animation. |
| Native item art needs another world observer | Existing world/custody authorization and examine paths already share the art catalog. Add one independently optional cached group, no new observer or world scan. |

Verified files: Objects.json exact eight blueprints; Farming/SeedPart,
Farming/BotanicalProcessingPart, Alchemy/ReagentPart; rendering catalogs
FlatPixelOpeningArtCatalog and FlatPixelGreyboxCatalog; existing item,
plot, portrait and custody tests. Existing approved sprites stay unchanged.

## Parts and owner mapping

One borrowed128×24 atlas at `Sprites/Live16x24/plot-products`, eight16×24
frames, in this order. Each has native square pixels, binary alpha,3–6
colors and a grounded baseline; no invented motion for inert goods.

| Frame | Exact blueprint | Required owned semantics | Shape |
|---|---|---|---|
|0|CandyCarrot|Food|Golden root, short sage leaves|
|1|CandyCarrotSeed|Seed→CandyCarrotCrop; ordinary ground|Small pale seed|
|2|EmberwheatSeed|Seed→EmberwheatCrop; ordinary ground|Narrow golden grain|
|3|MarlrootClod|BotanicalProcessing→FireClay,1–8|Red clay and short knotted root strands|
|4|MarlrootSeed|Seed→MarlrootCrop; cultivated|Squat red seed, pale notch|
|5|PitchpodResin|Reagent, positive heat+combustible|Detached amber bead, no glow|
|6|PitchpodSeed|Seed→PitchpodCrop; cultivated|Tapered olive seed, amber seam|
|7|InertSludge|Exact portable item|Coherent matte grey-olive lump|

All require owned visible Render, owned takeable nonsolid Physics,
Item tag without Creature/Player/Terrain/Crop, positive valid owned stack
if present, and no explicit visual override. World placement is authorized
by the existing current-cell path; carried examine uses its separate
custody checks. No inventory item may become a ground entity by changing art.

## Required production pipeline and gates

1. Reuse actual unchanged play geometry/capture `north-plot-harvest.png`
   plus accepted native north-plot contact; no staged inventory or teleport.
2. Imagegen reinterpretation saved as `concepts/plot-products-v1.png` with
   exact sibling prompt. Concept is reference only, not imported pixels.
   Review: quiet resin highlight; coherent sludge without distant scatter;
   seeds smaller than produce, marlroot root/clay separation.
3. Extract the eight mappings above. Build original editable integer-cell
   Blender geometry and direct native PNG/contact, source manifest and
   independent read-only verifier. Source verifier must fail before assets.
4. Native Unity selector/resource/custody tests RED before production;
   implement eleventh optional atlas on both catalogs. Earlier injection
   overloads retain explicit null, never load omitted Resources. Dispose
   generated frames once while retaining borrowed texture ownership.
5. Import exact source bytes with Point/no mips/no compression/no resizing.
   GREEN plus bounded existing item/plot/portrait controls and cold review.
6. Ordinary live harvest/pickup/examine, seed planting and clod preparation
   where reachable; capture actual Game images and read-only receipts.
   Preserve prior captures. If OS lock blocks actual input, record the gate
   as pending rather than injecting state or claiming acceptance.
7. Review exact owned diffs, update this record and continuation, fetch/rebase
   and push exact owned changes to main. Keep seven-milestone heartbeat active.

## Implementation and test evidence

The eleventh independently optional group is integrated in both catalogs.
Default resource loading includes the new128×24 atlas; all prior injection
overloads omit it explicitly. Exact semantic matching shares the existing
world and authorized examine paths. No gameplay, blueprints, observers,
placement, saves or normal presentation defaults changed.

Original editable Blender geometry, source cell map, manifest, direct
native/contact PNGs and read-only verifier are in
`ArtSource/Live16x24/plot-products`. Both root and an independent reviewer
inspected the contact sheet. Four seed silhouettes differ in shape and
color; raw clod retains roots; resin is a solid bead and sludge a matte lump.
The generated concept's bevels, shine and scattered chips were excluded.
All previous approved PNGs are byte-identical. The128×24 source/resource
SHA256 is `453c1eef4fa91f7d607c037a7398f2c6b376ca653e692e4f7bd8782cce3cf7da`.

| Gate | Saved evidence / result |
|---|---|
| Source RED | `plot-products/source-red.txt`: expected missing source-cells before native authoring |
| Source GREEN | `plot-products/verification.json`:8 connected silhouettes,4 distinct normalized seed outlines,3–5 colors, binary alpha, zero source/native/contact mismatches |
| Native RED | `Verification/Live16x24/plot-products-red.xml`:65 tests,8 passed,57 expected missing-constructor/resource failures before production |
| Native GREEN | `plot-products-green.xml`:65/65,zero skipped; actual resource, custody, reskins, semantics, missing/malformed groups, disposal, actual planting/preparation and stale portraits |
| Regression | `plot-products-regression.xml`:285/285,zero skipped across9 existing catalog/item/plot/portrait/state fixtures |
| Exact import | `plot-products-import.json`: source hash preserved,Point,128×24,one mip,uncompressed |
| Warm selector observation | `plot-products-selector-profile.json`:80,000 cached selections,80,000 matching frames,98.0444ms total,0 managed bytes during measured loop |

The65 and285 suites are disjoint: **350 distinct native Unity EditMode
passes**, not the prior plot milestone's860 rerun or added again. An initial
test request selected zero cases before the new script had imported; that
request was discarded, compilation/type presence checked, and the actual
65-case RED rerun. No zero-test run is counted as verification.

The selector timing uses detached fixture owners in a stopped editor, after
warmup; it is not frame rendering, draw-call measurement, an improvement
comparison or a Deck benchmark. Exact repeatable snippet is
`Tools/Live16x24/profile_plot_products.cs`. Resources are created once;
Reagent owns and invalidates its parsed cache. Actual rendering/profile
acceptance remains with the ordinary-play gate.

## Implementation-stage review and honesty bounds — 2026-10-23

- 🔵 Independent cold review and root diff review found no remaining P1/P2.
  Tests exercise actual inventory consumption and invalidated documents,
  not only direct sprite selectors. Missing resources preserve older groups.
- 🔵 Imported one extra small atlas with8 cached frames; no new world scan,
  game object per item or extra frame-time observer. The measured warmed
  selector loop allocates0 managed bytes; no whole-frame speed claim.
- 🧪 Ordinary play with these new sprites is not yet verified. CUA reported
  the Mac locked; no attempt was made to bypass it or inject gameplay for a
  screenshot. No new Play session was launched this packet. This is the
  only scope divergence from the intended acceptance sequence.
- 🧪 Physical Steam Deck, packaged activation, whole-region coverage and
  complete release journey are outside this bounded packet.
- 🔵 Static inert goods do not need invented animations. Existing movement,
  crop growth and claim ground cues continue to use their current systems.

**Can verify:** original native source pixels, real Unity import/bindings,
350 native tests including actual planting/preparation transactions,
cached selector allocation observation and independent static review.
**Cannot verify:** ordinary input, new sprite readability in the real
harvest pile/trade flow, fresh Game screenshots, frame-time impact, Deck or
release coverage. The contact sheet is source-art evidence, not gameplay.

`plot-products-editor-state.json` records stopped Play, no pending compile,
isolation or view restore, original SampleScene. Console receipt retains
the ADTM realtime-thread timeout and an MCP-mislabeled Debug.Log that test
results were saved; no packet-specific compiler exception was observed.
This is not an empty-console claim. Eleven older import metas were restored
only after linewise trailing-whitespace equivalence was proved; unrelated
untracked/recovery files were untouched.

## Historical next action — 2026-10-23

Check ordinary input availability first. If unlocked, launch the existing
isolated seed64 opt-in session and repeat the accepted route to Nella. Use
ordinary harvest/pickup/ground and carried examine on resin/clod, prepare
one clod into existing FireClay, and plant a real carried seed into eligible
soil. Include public gladroot/seed and merchant compost where available;
do not claim all eight exercised if an item was not acquired. Preserve prior
captures and use new `plot-products-*` PNG/JSON names. Measure a bounded
active rendering window, then stop/restore. Share actual Game screenshots.
No ordinary-gameplay acceptance is implied by current native tests.

While the Mac remained locked, the independent seven-frame works-product
packet advanced using the previously accepted works captures. Its source,
integration and native evidence are in [LIVE-16X24-WORKS-PRODUCTS.md](LIVE-16X24-WORKS-PRODUCTS.md).
Both packets still require ordinary new-art acceptance: close this plot
gate first, then the works forage/brew/coin route. Do not redo their source
art or add more unverified areas before those checks.
Purchased crop families, incidental surroundings, north creature motion,
east/kitchen admission/art and full release journey remain unfinished.
Keep the finite seven-milestone heartbeat active.

## Initial implementation files

Changed: two rendering catalogs; FlatPixelPlotProductTests+meta; native
resource+meta; original source generator/assets/verifier; import/profile
snippets; concept/prompt; this record, main/north-plot continuation and
native test/import/profile/editor evidence. No live screenshot is included
in this packet.

## Recovered ordinary continuation — 2026-10-24

User-authorized source preservation and selective cleanup restored the editor
and ordinary input; source, saves and the selected current build remain intact.
The earlier locked-machine limit is historical. Native source/implementation
results above remain their original runs; this continuation adds actual
gameplay evidence without re-counting those tests.

Seed64 reached Overworld.11.8.0 at20,10 with40HP. The [arrival](Verification/Live16x24/plot-products-arrival.json)
records reserve rightsUnknown and50drams. Ordinary payment changed rights to
Granted and drams to42. At18,7, the player harvested the ripe Pitchpod at18,6;
the [harvest screenshot](Verification/Live16x24/plot-products-pitchpod-harvest.png)
and matching receipt show the crop removed, two PitchpodResin owners and one
PitchpodSeed on the ground. Pickup then put2resin and1seed in inventory.
The [carried resin Examine](Verification/Live16x24/plot-products-resin-carried-examine.png)
shows the native amber sprite, quantity2 and the actual descriptive/use text.
No gameplay state was injected to create this route.

**Evidence-label correction:** [plot-products-resin-ground-examine.png](Verification/Live16x24/plot-products-resin-ground-examine.png)
shows the three-item **Pick up** list, not an Examine portrait. Keep the raw
file unchanged; it verifies the ground selection/pickup flow and does not close
ground-portrait acceptance. All four PNGs are1280×800; three matching receipts
report an editor screen size2321×1139, which is not the saved image dimensions.

This is **ordinary_partial** for the new eight-product group. It does not prove
Marlroot/clod preparation, seed planting, use of the resin, all seed/produce
families, or the works products. The observed Marlroot remained stage0 during
this visit. The [route cleanup](Verification/Live16x24/g2-action-recovered-launch-cleanup.json)
records stopped Play, no isolation/art pending/borrowed view and saveRoot null.
The later action-capture [cleanup](Verification/Live16x24/g2-attribution-launch-cleanup.json)
also confirms those restored fields. Neither is a save/load acceptance of this
newly harvested inventory.

**Next:** exercise an actual ground Examine portrait, plant a real acquired
seed in eligible soil, acquire/prepare a Marlroot clod and verify its FireClay
output. Continue the remaining product families where ordinary play provides
them, then the works forage/preparation/brew/coin route. Preserve approved art
and current evidence. Packaged activation, physical Deck, active frame timing,
east/kitchen and the complete six-site journey remain separate open gates.
