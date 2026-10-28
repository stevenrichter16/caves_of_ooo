# Stump environmental polish: worn floor and raised stone

**Planned only,2026-10-28.** The user's current priority is environmental
materials/composition across all biomes. This follows the Sodden pass; no
runtime, art, tests, Unity state or gameplay changes accompany this plan.

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
