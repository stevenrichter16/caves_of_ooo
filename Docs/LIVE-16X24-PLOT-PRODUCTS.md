# Live 16×24 — north plot products

## Status and purpose

**Current status,2026-10-24:** the bounded basic plot-product route is accepted
in ordinary keyboard Play. Harvest, pickup, exact product readers, acquired
seed planting, rain-fed growth, compost, preparation, eating and one resin
brew now have actual evidence. See the latest continuation below. This does
not cover every ground/carried permutation, purchased crop family or the
complete opening release. Carried MarlrootSeed remains unobserved; its exact
ground portrait and normal planting are verified. The Mac lock is resolved.

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


### Ordinary product-loop continuation plan — 2026-10-24 13:54 UTC

Next slice closes the already authored packet's remaining reachable uses.
Reuse the accepted plot composition, concept, extraction and native resources;
no new asset or gameplay change is planned. Start a fresh save-isolated ordinary
seed64 game, travel using controls, buy reserve rights, read the actual carried
watering grimoire, harvest/examine/pick up Pitchpod output, plant an acquired
seed on eligible soil, water/grow/harvest Marlroot, prepare its clod and inspect
FireClay. Capture actual states and source portraits. Recheck normal save/load
and return from menus for these new products. Record failed actions and exact
remaining output coverage instead of calling the whole group accepted. Root
owns Unity; independent source audit verifies input/menu prerequisites.

This is integration acceptance of existing tested code. If ordinary Play
exposes a significant defect, save its source sweep and run a behavioral RED
before changing production. No test re-run/count or new artwork is implied by
this plan. No supplied items, teleport, forced crop ticks or reveal shortcuts.


### Ordinary growing / product loop — 2026-10-24

Fresh normal seed64 launch, absent saved presentation key and no forced art
mode. Real keyboard travel reached the plot; paid reserve rights cost8drams
(50→42). Read the carried WateringGrimoire to learn ConjureRain. Harvested the
ripe Pitchpod at18,6, examined an exact ground resin owner and picked up2resin
plus1seed. Planted that acquired seed in its emptied prepared bed. No granted
items, teleport, crop-state injection or forced simulation ticks were used.

Two normal casts of ConjureRain with ordinary wait/movement turns showed
actual stage0→1→2 growth of the new Pitchpod and existing Marlroot. The
`plot-products-loop-sprout` and `-ripe` receipts record the stages/moisture.
Harvested Marlroot at17,6 into2clods+1seed, opened an exact ground clod Examine
portrait, picked up the products, then prepared one clod into1FireClay.
Remaining clod1, seed1 and FireClay1 appear in the `-prepared` receipt. The
FireClay portrait itself was not captured in that sequence.

F5 save / F6 reload preserved every recorded observation after sorting only
the complete owner rows:197owners and10inventory rows, tick1610, player18,7,
40HP,42drams, reserve Granted and replanted Pitchpod stage2/moisture12. See
[canonical comparison](Verification/Live16x24/plot-products-loop-save-reload-comparison.json).
This is bounded observation equivalence; runtime IDs, growth counters, binary
save fields, other zones and nested container details were not recorded.

Evidence labels: `-resin-ground` and `-clod-ground` are genuine shared reader
portraits. `-seed-carried` records a **failed** inventory Examine presentation
(log only); it does not accept the portrait. `-planted` shows the pause menu
after successful planting, so its receipt supports consumption/new crop but
it is not a clean world composition capture. The stale right-side FOCUS after
harvest is another observed defect. Both are repaired and rechecked under
[LIVE-16X24-PLOT-READER-POLISH.md](LIVE-16X24-PLOT-READER-POLISH.md); preserve
these original failed screenshots unchanged.

The loop's cleanup receipt confirms Play stopped, no pending art/isolation,
no borrowed view and saveRoot null. Native PNGs are1280×800; the capture JSON's
editorReportedScreenSize is an editor field, not image dimensions or Deck
evidence. These captures do not finish the whole eight-product packet:
CandyCarrot/EmberwheatSeed/MarlrootSeed/InertSludge identity-use coverage, resin
brewing, purchased crop families and works products retain explicit debt.


Post-fix replay additionally accepts carried PitchpodSeed, EmberwheatSeed and
CandyCarrotSeed (gladroot) portraits with free reading and correct inventory
return. Clean `plot-products-reader-replanted.png` shows the actual same-bed
plant after reading. The two reader faults are closed within the linked
native/ordinary gates. Remaining product coverage is in release-coverage;
do not continue treating these three carried portraits as pending.


## Ordinary remaining-use continuation — 2026-10-24 15:00 UTC

Planned acceptance pass, not completed yet: resume the previous real F5 save
in a new owned isolation root, use ordinary Continue, and retain the user's
presentation setting. Checkpoint hashes are verified against the prior receipt;
no grants, teleports, crop-state writes or injected turns. Acquire sludge through
Nella's actual trade before watering the fresh Pitchpod bed, examine it, then
use the crop's compost action. Continue remaining Marlroot seed/plant, gladroot
produce/use and resin brew gates where their real prerequisites permit. Reuse
accepted native source art; only a named observed defect warrants edits.

Record exact input/visual evidence, independent native-size review and truthful
coverage. Prior captures are not new acceptance. Cleanup audit currently finds
only useful regenerated caches and the protected current build; no deletion is
planned unless later inspection proves a genuinely stale candidate. Verify main
again before any cleanup. Root owns Unity; delegated work is read-only.

## Ordinary product uses and persistence — 2026-10-24 15:00 UTC continuation

**Bounded route accepted; no new runtime or art changes.** This closes the
original packet's reachable harvest/pickup/examine, planting and preparation
gate, using the earlier reader replay plus this pass. The original concept,
extraction, editable source and native atlas remain the accepted production
pipeline inputs; they were not redrawn to manufacture another milestone.

Root resumed the previous actual F5 checkpoint inside new owned save isolation
through ordinary **C Continue**, with no saved presentation override or forced
art mode. All subsequent changes came from real keyboard input. No grants,
teleport, revealed fog, injected growth ticks or direct commands supplied the
outcomes. [Source route audit](Verification/Live16x24/plot-products-uses-source-route.md)
checked the current menus, stock and costs first. Coordinates describe this
seed64 run, not a universal layout.

| Ordinary action | Observed state and actual screenshot |
| --- | --- |
| Trade and read | Bought Nella's two sludge for6drams,42→36. [Carried sludge](Verification/Live16x24/plot-products-uses-sludge-carried.png) shows the native matte lump and compost explanation. |
| Compost two fresh reserves | Each action consumes one sludge. Pitchpod24→18 and Marlroot26→20 wet units/stage; both remain dry at stage0 with zero progress. [First crop](Verification/Live16x24/plot-products-uses-pitchpod-composted.json), [both](Verification/Live16x24/plot-products-uses-both-composted.json). |
| Rain, grow and harvest | Read the actual carried watering book, cast Conjure Rain and take ordinary waits. [Sprouting](Verification/Live16x24/plot-products-uses-sprouting.png) then [mature](Verification/Live16x24/plot-products-uses-mature.png) receipts show reserve stage1→2; public gladroots automatically leave four roots across two beds. |
| Inspect and eat gladroot | [Exact ground](Verification/Live16x24/plot-products-uses-gladroot-ground.png) and [carried](Verification/Live16x24/plot-products-uses-gladroot-carried.png) native portraits match. Pickup2→eat1 leaves1; HP stays40/40. This proves consumption, not a healing gain. |
| Harvest and replant marlroot | Harvest leaves two clods and one seed. [Exact ground seed](Verification/Live16x24/plot-products-uses-seed-ground.png) has its red notched portrait. Pickup and Plant consume that seed, creating a fresh dry, uncomposted stage0 crop at17,6. |
| Prepare repair material | One of two clods becomes one FireClay. [Carried reader](Verification/Live16x24/plot-products-uses-fireclay-reader.png) shows its native clay silhouette and paginated repair uses. No repair was performed in this pass. |
| Brew harvested resin | [Preview](Verification/Live16x24/plot-products-uses-resin-brew-preview.png) names burning coating. Normal Enter consumes1of2resin and creates1BrewedTonic. [Actual reader](Verification/Live16x24/plot-products-uses-coating-reader.png) displays the native flask and truthful burning/throwing text. Brewing stays at tick1540; no paid turn is claimed. |
| Save and reload | Actual F5/F6 retains198 recorded owner rows,11 inventory rows, tick1550, player18,7,36drams and40HP. [Comparison](Verification/Live16x24/plot-products-uses-save-comparison.json) and [reloaded world](Verification/Live16x24/plot-products-uses-reloaded.png). The final rows explicitly preserve `Coating` / `Burning:1`. |

The24 actual1280×800 EOF PNGs have paired observational JSON, listed with
hashes in [capture index](Verification/Live16x24/plot-products-uses-index.json).
The saved capture source documents what the receipts measure. World owners
continued acting during the waits; an unrelated scrabbler killed a magpie.
No claim is made that the entire world stayed fixed during farming.

### Review, corrections and limits

- 🔵 Independent review found no significant graphical mismatch. Six sampled
  portraits exactly match all opaque native source pixels at their displayed
  integer scale: sludge, ground MarlrootSeed, ground/carried gladroot,
  FireClay and the brewed flask. See [review](Verification/Live16x24/plot-products-uses-independent-review.md).
- 🔵 Counter-observations: compost supplies no water or immediate progress;
  the first compost leaves the other crop's duration unchanged; replanting
  creates a dry/uncomposted crop with default duration; single brewing leaves
  one resin; eating at full health does not claim an HP increase. Reader
  snapshots cost no observed turn. These are ordinary observations, not a
  replacement for the existing native adversarial tests.
- 🧪 An attempted carried MarlrootSeed reader selected **Plant** because its
  `a` shortcut differs from the other items. The evidence is correctly named
  `seed-planted`; no carried-seed portrait is claimed. The exact ground
  reader and planting satisfy the bounded original route. Do not restart a
  whole growing cycle solely to fill this optional duplicate view.
- 🧪 The coating offers drink/throw. This pass accepts brewing, identity and
  reading only, not drinking, throwing, damage, quenching or enemy use. That
  one flask is partial evidence for the works-products packet, not its other
  six outputs. No undocumented gameplay redesign was slipped into graphics.
- 🔵 No code or art changed, so no new RED/GREEN or native-suite rerun is
  claimed. Earlier packet and reader-fix results remain their own evidence.
- 🔵 Final [cleanup](Verification/Live16x24/plot-products-uses-cleanup.json)
  confirms stopped Play, released isolation, original save root and no
  borrowed view or pending art. Preview scale restored to2× through UI.
  [Console](Verification/Live16x24/plot-products-uses-console.json):0errors,
  2existing Metal memoryless-depth warnings. A read-only scratch reflection
  probe used a nonexistent scale property; it affected no project source.
- 🔵 Fifteen imported metas had only trailing-whitespace rewrites; each was
  compared linewise before restoring exact HEAD bytes. Unrelated local
  AllotmentNotice PNGs remain untouched and separately archived in main.

**Can verify:** named ordinary keyboard routes, actual screenshots/native
portrait pixels, finite item/state changes and equality of recorded fields
across this save/load. **Cannot verify:** every serialized field or exact
object identity, every item/view permutation, controller/physical Deck,
packaged launch, continuous animation/audio feel, performance or completed
six-site/world graphics. No new image-generation or art-creation claim.

### Preservation and next action

A hash-verified local copy of this actual F5 save is retained at the location
in [checkpoint receipt](Verification/Live16x24/plot-products-uses-local-checkpoint.json).
It may be resumed only inside owned isolation. The game ends at the north
plot18,7 with1resin,1burning coating,1FireClay,1clod,1gladroot; the new Marlroot
is dry stage0 and the Pitchpod remains ripe. User saves were not changed.

[Storage audit](Verification/Live16x24/plot-products-uses-storage-audit.md)
found no worthwhile stale cache/build deletion: the seven old packages are
already gone, active caches were freshly rebuilt, and the current delivered
archive and all source/save backups are retained. **0bytes removed this
pass.** Exact owned records and evidence are pushed and remotely verified
before any future cleanup; no global Library wipe is planned.

**Next:** proceed to the north works with this real checkpoint. Close finite
forage→preparation→brew and coin discovery with native portraits, then the
east connector/kitchen using the saved source audit and full playable art
pipeline. Keep G2 scene/framing and defence/hurt coverage explicit. Purchased
crop kits, all-seed variants, continuous motion, packaged delivery, physical
Deck and the complete six-zone release remain open. Do not keep replaying
this accepted basic plot loop or redraw its accepted assets without a named
new defect. Continue the active hourly graphics work.
