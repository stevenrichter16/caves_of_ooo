# Live opening object states — native source

Status: twelve original native static state illustrations authored, reviewed and imported.
330 native Unity tests pass. Ordinary-input torch extinguish/pickup and crop
harvest/replant checks are recorded in `Docs/LIVE-16X24-ART-DIRECTION.md`.
This is opt-in development presentation; remaining release gates are explicit there.

## Production record

- Existing layout/capture authority: `Docs/Verification/Live16x24/native-cast-start.png`.
- Imagegen design reference: `../concepts/state-parts-v1.png`, with exact prompt
  `../concepts/state-parts-prompt.md`. Its useful silhouette/state motifs are
  reconstructed with fewer flat clusters; imagegen shading and gradients are rejected.
- `Tools/Live16x24/build_opening_states.py` builds original integer XY mesh
  geometry in Blender. No raster is read, sampled, cropped, edited or downscaled
  by the authoring script. One authored grid unit renders one native square pixel.
- Editable source: `opening-states.blend`, named frame/part collections, exact
  palette/cells in `source-cells.json`, and the generator above. Rendering helpers
  are imported read-only from the accepted base generator.
- Native atlas: **192×24 RGBA**, twelve **16×24** cells in the order below;
  16 PPU, centered Tilemap pivot, Point filtering, binary alpha, no mipmaps or lossy
  compression. Every subject reaches image row22; row23 remains transparent.
- `opening-states-contact.png` is a direct Blender render of the same geometry
  at integer8×, with a separate dark-green background and labels. It is not a
  resized atlas or game screenshot.
- Static state variants only. No duplicate animation frames, invented readiness
  icons, water sources, clothing overlays, halos or projected lighting.
- Prior approved PNGs and the base/cast atlases remain byte-identical to HEAD.
  No Assets, scenes or Unity state were changed by this source task.

## Exact frame order and state contract

All routes require an actual visible current owner; borrowed/foreign/malformed
parts fall back rather than announcing unsupported function. Zone admission and
top-owner selection remain the renderer's authority.

| Index | Frame | Owner / actual state |
| --- | --- | --- |
| 0 | wicket-buckled | `GleanersBuckledWicket`; structural `RepairablePart.BlocksFunction(owner)` |
| 1 | wicket-closed | Same owner, function restored, `DoorPart.IsClosed` |
| 2 | wicket-open | Same owner, function restored, not `DoorPart.IsClosed` |
| 3 | campfire-cold | Exact `Campfire`, no actual `BurningEffect`, finite temperature below150° |
| 4 | campfire-hot-coals | No actual burning, finite temperature at least `CookingService.MinimumFiniteCookingTemperature` (150f) |
| 5 | campfire-burning | Current owner actually has `BurningEffect`; flame is not inferred from temperature alone |
| 6 | drawgourd-seed | Exact `DrawgourdCrop`, owned `CropPart.GrowthStage == 0` |
| 7 | drawgourd-sprout | Same crop, stage1 |
| 8 | drawgourd-ripe | Same crop, stage2 and `HarvestAtMaturity` |
| 9 | torch-unlit | Exact `Torch`, controlled light off/extinguished, no destructive burning |
| 10 | torch-lit | Actual controlled flame from owned TorchLight/LightSource with usable heat/fuel, or actual destructive burning |
| 11 | waterskin | Exact loose `Waterskin` with owned vessel part; neutral silhouette for any charge count |

State variants preserve the wicket posts, the six stone-ring clusters and the
torch's shaft/wrapped head exactly. Their functional centers change. An open
wicket has a truly empty central silhouette; buckled rails do not claim passage.

## State honesty and removal audit

- Repair sets `RepairablePart.Repaired`, not `DoorPart.IsOpen`. Structural
  destruction removes the wicket; its empty wreckage blueprint leaves a real gap.
  There is no persistent destroyed decorative gate frame.
- The initial opening Campfire is500°, fuel200, **without BurningEffect**: use
  hot coals. Cooking also needs positive usable fuel, so sparse embers indicate
  retained heat only. Relighting sets450° without directly adding BurningEffect.
  `FuelExhausted` currently has no consumer that creates an AshPile; do not invent
  that replacement. The retained ring can remain cold or hot after fuel exhaustion.
- Campfire is low-layer Terrain and must not expose current heat/burning state
  through remembered fog. The renderer must gate state lookup accordingly.
- Drawgourd moisture uses the independent existing wet/dry soil layer; there are
  no six duplicated stage/moisture frames. Dryness pauses growth and does not wilt
  or spoil a ripe crop. Harvest removes the plant, yields one DrawgourdShell and
  one DrawgourdSeed, and leaves the prepared bed. Clearing young crops removes them
  with no yield. Shell/seed product art is outside this twelve-frame batch.
- TorchLight is a genuine controlled loose/equipped flame, independent of the
  destructive BurningEffect. The actual torch starts enabled at450° with fuel50.
  Fuel exhaustion, freezing, excessive wetness or cooling extinguishes it. Pickup
  or equipment ownership removes the ground representation. Never infer a flame
  from a blueprint name/tag alone or depict a planted permanent lamp.
- Waterskins begin empty with capacity3. Fill/drink/water/douse actions retain
  the actual vessel while changing charges. Emptying does not remove it; pickup
  removes ground art. The sprite does not reveal contents or advertise a source.
- Integration must test stationary redraws: the finite fire's150° crossing already
  invalidates its cell, whereas torch switching currently bumps the equipment/light
  version and needs its foreground refresh checked. Actual BurningEffect changes
  likewise need explicit coverage rather than waiting for player movement.

Read sources: `DoorPart`, `RepairablePart`, `DestructionSystem`,
`ReferenceGladeBuilder`, `CampfirePart`, `CampfireWorkActions`, `CookingService`,
`ThermalPart`, `FuelPart`, `BurningEffect`, `CropPart`, `CropSystem`,
`CropYieldService`, `FieldworkActions`, `TorchLightPart`, `LightSourcePart`,
`InventoryPart`, `WaterskinPart`, and inherited Objects.json definitions.

## Visual passes and validation

1. Rendered all twelve native frames and8× contact. Reviewed the three gate
   silhouettes, heat-versus-flame contrast, gourd's neck/bulb, unchanged torch
   hardware and soft waterskin versus hard vessel shape.
2. Shortened the two side stones in every fire-ring state by one pixel to separate
   the six stone clusters; the first ring merged into a broad bar at native scale.
   Re-rendered both outputs and inspected the common contour and warm state cues.

`python3 ArtSource/Live16x24/states/verify.py` performs read-only PNG analysis:
exact source pixel matching, binary alpha,3–6 colors per frame, integer8× contact
matching, baseline, unchanged state-part pixels, open gate negative space,
three restrained hot embers versus flame, and prior approved-raster preservation.
It writes only `verification.json`; it never creates or edits images.

This proves editable original geometry and pixel integrity. It cannot prove
Unity import settings, gameplay routing, fog behavior, actual interactions or
hardware performance; parent/integration agents own native tests and Play checks.
