# Stump environmental polish: worn floor and raised stone

**Bounded Editor material pass verified,2026-10-28.** Original21-frame floor/dome revision improves named material/readability defects. Whole-area polish remains open; source `ab45624ed7580a60ab0090a481b3f23e56e6cc9d` verified on origin/main. See [actual acceptance](Verification/Live16x24/stump-environment-acceptance.md).

## Actual defect and preferred first slice

Viewed actual `Verification/Live16x24/stump-canopy-after-07-summit.png` and
`stump-canopy-after-11-fresh-continue.png`: a nearly uniform pink plane dominates
the view, while solid dome masses look like faint grey patterned floor patches.
The sparse scene gains little readable material hierarchy from either. Actors
remain visible, but the blocker/walkable distinction is weak. These are actual
material/readability defects, not evidence that missing props should be invented.

**First polish the existing TepuiStone floor and StoneDome masses together.**
Use restrained pink-grey sandstone with occasional broad shallow fossil grain
and irregular quiet patches, not a dot carpet or identical hatch rectangles.
Keep the floor visually flat. Give the dome's exposed straight cell boundaries
a clear layered lip, a modest dark lower face and quieter lighter interior,
with grain crossing its mass. It should read as one solid stone knuckle rather
than masonry paving or a separate little rock in every cell. No projected 3D
geometry, new fake paths, harvestable-looking groundcover or decorative props.

TepuiWall outcrops and solid GrainRidge are the related next blocker checks.
They already have native perimeter frames, so do not invent a new wall system.
Confirm their present weakness in a fresh Gorge/Grainfield capture before
including their pixels in the first revision. DescentLedge is **passable**;
never give it the same blocking face as the three solid families.

## Verified ownership and reusable sources

- `Assets/Scripts/Presentation/Rendering/FlatPixelStumpMaterialsCatalog.cs`:
  exact current ordinary Stump graphs only; canonical addresses, live biome,
  null POI and no saved lair. Floor0–3; dome interiors20–21 and exposed masks
  56–70. Exposed mask N1/E2/S4/W8 differs from the Sodden wet connection mask.
  Fully joined interiors retain their existing quiet frame. Hidden/remembered
  neighbors are not queried for live geometry; dirty-neighbor painting exists.
- `Assets/Resources/Content/Blueprints/Objects.json`, TepuiStone/StoneDome:
  floor is worn smooth pink-grey stone; dome is a solid Terrain owner described
  as a former bough/knot. Dome has no destructible/material subsystem to add.
  TepuiWall is a real destructible Stone wall; GrainRidge is solid terrain.
- `StumpCompositionPlan.cs:150` makes four unequal elliptical dome masses;
  its repaired routes and summit shelter/ecology stay unchanged. Gorge has
  actual pools/walls/passable ledges; Grainfield has east-west fossil fibers.
- Reuse `ArtSource/Live16x24/stump-materials/source-cells.json`,
  `stump-materials.blend`, `manifest.json` and
  `Tools/Live16x24/build_stump_materials.py`. Existing71-frame1136×24 resource:
  `Assets/Resources/Sprites/Live16x24/stump-materials.png`. Preserve baseline
  source/art reversibly and keep non-target pool, ledge, tank, vein, Tree/Bush
  and actor pixels unchanged. No need for another generic generation tool.

## Earned starting point and ordinary proof

Best healthy preserved Stump checkpoint: `Verification/Live16x24/Fixtures/stump-final68`,
Gorge2.5, turn7360, HP38/40. `Quick.sav.gz` SHA256
`d5ea1e9487c01df57ea5861671bb684aaf269628dd90e53054629e1dc71d6ac7`.
The existing `stump-final-gameplay-receipt.json`/material doc records43,12 and
normal Continue persistence. Root must confirm current state on launch, then
use ordinary world-map travel2.5→3.4 for the summit. Closest matched alternative:
`Fixtures/stump-canopy-after74`, Summit3.4 at50,2, turn9040, HP29/40, verified in
`stump-canopy-after-receipt.json`; lower health, but no travel hunt needed.

Capture a current whole-view baseline, then imagegen from it: preserve exact
HUD, owners, routes, silhouette boundaries and camera; reinterpret only worn
floor and existing solid dome material, using layered/shaded straight blocker
edges and flatter quieter floor. Extract editable native cells, inspect tiled
patterns, integrate and compare normal zoom. Reject concept-added height,
plants, paths and furniture. A previous ordinary control walked Summit44,10
into dome44,9 and correctly remained44,10; reuse only if the fresh owner census
confirms that location. Walk around a disclosed dome, inspect its identity,
return/save/restart, and check Gorge floor/ledge contrast as a second formation.

## Tests and acceptance bounds

For pixel-only revisions, retain native selection tests and deterministic
source validation; require non-target frame/pixel identity. Existing
`FlatPixelStumpMaterialsTests` covers all exposed masks, unknown/hidden/foreign
owners, wave/energy state, incremental neighbor updates and real wall destruction.
If a demonstrated floor-pattern or frame-selection change is needed, first add
one focused RED for that behavior plus unchanged StoneDome/ledge/stock controls;
do not repeat an entire framework. Preserve cached views and no render-time
world mutation. Whole-view hierarchy, repetitive stamp runs and blocker clarity
need actual visual review; native counts cannot accept them. All other Stump
addresses, special sites, biome polish, packages and physical Deck remain open.

## Pre-implementation corrections,15:16UTC

Baseline actual01 confirms Summit50,2HP29, currentordinary3.4. Original71-frame atlas exactlymatches27264sourcepixels (fd33497368615b60f09ccc2d16efe25dd0033370034c95ce93fe0445bb2af7f1). Only21frames0–3/20–21/56–70change; protectother50frames/19200pixels and sharedpalettekeys. Existingbuilder writes Assets and oldverifier assumesfirst26unchanged/one-pixelrims; use a narrow source-only wrapper withtarget-specificpalettekeys, originalquads andexactprotected-framechecks. Do not executeoldbuilder unchanged.

Floor0dominates75% and localcoordinatepatternrepeatsbetweenzones; keepitquiet, withno stampedmotif. Domeinterior20/21 similarlymustformonecoherentmass. ExposedbitsN1/E2/S4/W8 are oppositeSodden connectionbits; world/reader currentlyshareselectedperimeterframe. No runtime/testchange isneeded forpixel-only revision. Existing FlatPixelStumpMaterialsTests plus non-targetpixelidentity willverify selection/lifetime/dirty/fog contracts. Native111 is historicalonly untilrootrunsfinal.

Actual baseline controls: from Summit74, Down moves50,2→50,3HP29; anotherDown is blocked by exactStoneDome65504at50,4. L/Down/Return/X opens its ordinary reader. Captures01/02/03 and read-onlystate receipts preserve this. No invented collision/no gameplay edits. Correct native test file is Assets/Tests/EditMode/FlatPixelStumpMaterialsTests.cs (not the newer Presentation/Rendering test subfolder); initial pin script failed beforewriting and then used the verifiedpath.

## Final bounded result,2026-10-28

The21-frame pixel-only pass is Editor-verified. Source atlas00d7f0c1ac90bd8dab781e257048c5a94e8555eb139eee61f0533a723438d46a;50 non-target frames/19,200pixels unchanged. Approved baseline and rejected first loop-like marks stay preserved. Two exports match; both editable Blender files reopen with25,750exactquads/300groups. The labeled imagegen study informed color/relief only; original native art contains no imagegen raster extraction.

111/111 existing native Stump cases pass. No runtime behavior changed and no new runtime RED claim is made. Real Summit movement, blocked dome, reader, side approach, F5 and fresh Continue172 pass;0/564,000worldpixels differ on restart. Gorge's matched before/after retains walls/pool/ledge identity; an ordinaryRight crosses43,12→44,12HP38 on the passable ledge. No fresh inter-zone travel was performed. Actual captures and read-only owner receipts are linked by stump-environment-final-receipt.json.

Independent source and actual review:0P1/P2. The floor now has restrained fossil wear instead of a blank pink plane, and domes have a darker connected cap with inside-cell relief instead of repetitive hatch. Their tops still look like flat cut-stone platforms, and broad empty scene composition is unresolved. This accepts the named material/readability improvement only, not whole-area polish, naturalistic domes or reference parity.

A39.79s Editor profile retained the last4096samples and wrapped: renderermean0.092745ms/p990.180833ms. It covers Summitworld/readerexit/idle/tool time; rapid arrow attempts during profiling did not move the player, so no movement/Gorge-profile claim. The later sustained east input moved two cells; subsequentDown moved one before the solid corner. Capture05 is a no-movement attempt,06 the verified side approach. Console query returned memoryless depth load/store messages; no managed exception observed. No packaged/Deck performance claim.

Preservation:49foreignhashes and1000normalEditor-saveentries/0standaloneentries unchanged; only four Unity bookkeeping preference keys changed. Eight generated metadata whitespace rewrites were backed up/restored. Source/current/prior builds preserved; storage deletion0. Packages remain43a151ddb and exclude this art. Next: Grovelands forestfloor/groundcover, with wider environmental polish still required.
