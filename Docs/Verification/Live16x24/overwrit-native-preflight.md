# Overwrit native material preflight

2026-10-27. **Planned, not implemented or accepted.** First cohesive static kit:
`OverwritGround`, `OverwritNewGrowth`, `OverwritPilgrimBench`,
`OverwritWaymarker`. Root owns concepts, native RED/GREEN, integration and
actual after views. This independent preflight changes only this document.
Do not change generation, physical footprints, interactions, existing art or
the opening-region admission list to obtain the visual result.

## Actual baseline and corrected premises

`overwrit-before-owner-census.json` records ordinary keyboard world-map travel
in seed65 from source `ab26e270cb376253c971a41c3449ecbbf4171444`. Every recorded
zone is the actual current graph, reports no saved lair and retains HP38.

| Actual zone | Ground | Shoots | Other observed owners |
| --- | ---: | ---: | --- |
| 0,9 Green Rim | 2000 | 31 | StairsDown; 2 HollowchimeCrop; crop record; Player |
| 0,10 Blank | 2000 | 8 | Player |
| 3,10 Pilgrimage Rim | 2000 | 24 | Player |
| 4,11 Pilgrimage Rim | 2000 | 28 | Player |
| 4,12 Pilgrimage Rim | 2000 | 30 | Bench at76,18; 2 MargincressCrop; crop record; Player |
| 3,13 Pilgrimage Rim | 2000 | 23 | Player |
| 3,14 Pilgrimage Rim | 2000 | 32 | Bench at75,5; 2 BinderrootCrop; crop record; Player |
| 0,13 Dry Rim | 2000 | 27 | Player |

The reviewer directly viewed captures01 Green Rim,02 Blank,06 bench approach
and08 Dry Rim. They show a repeated ground glyph field with small clustered
shoots; the bench has very little silhouette. Root also examined the actual
4,12 bench and retained capture07. **No Waymarker was observed in the five
Pilgrimage candidates.** Its initial native fixture/source coverage must stay
separate from gameplay acceptance. Do not inject a marker into the earned
comparison and label it naturally observed.

| Earlier risk/premise | Source correction and consequence |
| --- | --- |
| Dedicated Overwrit pipeline means only four source families | `OverworldZoneManager.GetPipelineForZone` appends `BiomeCropPlacement`; actual crops above confirm it. Placement adds `CultivatedSoilPart` and `Plantable` to the same OverwritGround owner. Those real beds must refuse the plain-ground body and retain crop/soil presentation. |
| A bench grants a resting action | This exact bench has no Restable part/tag; it is breakable solid Wood. Do not borrow an interactive resting-station identity. |
| A marker is a directional sign | It is blank Stone, has no RegionalSignpost and leaves Rubble on destruction. No lettering, arrow or invented service. |
| Material/Thermal are universal terrain requirements | Plain ground has neither; the marker has Material/Destructible but no Thermal. Shoots and bench have both Material and Thermal. |
| No POI implies ordinary admission | Unsaying2,11 is outside the finite ordinary table even with null POI. Reject it, underground zones and other graphs. |
| One untouched HP value defines the native body | Structural damage is legitimate. Keep an extant neutral body while `0 < HP <= MaxHP`; removal/Gone/zero HP must remove native eligibility. No optional cracked animation is required. |

## Proposed public adapter and integration boundary

Follow the small existing regional catalog pattern, without a new framework:

```csharp
FlatPixelOverwritMaterialsCatalog(FlatPixelFallbackCatalog borrowedSelector)
FlatPixelOverwritMaterialsCatalog(Texture2D atlas, FlatPixelFallbackCatalog borrowedSelector)
static bool IsSupportedZone(Zone zone)
bool TryResolveGround(Zone zone, int x, int y, out Tile tile, out Entity floor)
bool TryResolveForeground(Zone zone, int x, int y, Entity owner, out Tile tile)
bool TryGetSubjectSprite(Zone zone, Entity owner, int x, int y, out Sprite sprite)
void Dispose()
```

Suggested resource: `Sprites/Live16x24/overwrit-materials`; proposed8-frame
contract below is subject to root's source freeze. Injected resources never
fall back to Resources.Load. Null selector, null/wrong-sized atlas or disposed
catalog returns no native result. Cache only owned Tile/Sprite objects once;
destroy those idempotently, preserve borrowed texture/selector. Use point
sampling,16×24 cells,16PPU, centered pivot, full-rect sprites, no tile collider.
No pixel reads/rebuilds or plan/RNG invocation in per-cell queries.

Build the finite coordinate lookup once from
`OverwritCompositionPlan.IsWildernessZone` (22 canonical addresses). Each query
must still prove `WorldLocationContext.For(zone)`, the same instance in
`manager.CachedZones`, correctly sized current map arrays, current Overwrit
biome, null current POI and **read-only** `!LairStacks.HasSavedColumn`.
Do not call `Inspect`, `GetZone`, world generation or a parsing/allocation-heavy
helper on each paint. Source scope and mutable live authority are distinct.

Cell gates: current zone cell, actual x/y identity, explored and visible.
Foreground and subject sprite require the same non-remembered owner selected
by `FlatPixelFallbackCatalog.TryResolve`. Current world/examine disclosure
stays with the existing caller; a sprite helper must not authorize a document.
`FlatPixelGreyboxCatalog.TryGetPresentationSubjectSprite` may still return a
glyph after adapter refusal—test native texture identity, not blanket false.

Wire a narrowly scoped cached `_overwritMaterialsArt` into Greybox ground,
foreground, subject and disposal paths. Native opaque ground uses white tint,
`GroundKind.Ground` and `LocalGround=true`. Suppress the foreground only if it
is **the same represented floor**, never a crop, player, stair or item. Store
new resolver outputs in separate locals so later false `out` calls cannot
erase a previous native success. This dry kit needs no new represented-water,
brine or mire flag and no painter/animation change.

## Exact owner and state predicates

For each matched owner, require one owned Render and Physics part (duplicates
or foreign `ParentEntity` refuse), visible Render, non-takeable and not carried
or equipped. Check actual current placement; a supplied stale owner/foreign
anchor is not enough. If a footprint exists, it must be uniquely owned,
single-offset zero and currently registered. Optional stack must be uniquely
owned with positive count. Empty VisualID/VisualVariant/GlyphVariants preserve
explicit custom-identity refusal. Require exact blueprint, glyph and layer;
authored base colors below document identity, while ordinary lighting remains
the renderer's responsibility. Do not dispatch Render events in the catalog.

| Exact owner | Render / physics / tags | Required semantic state |
| --- | --- | --- |
| OverwritGround | `.` layer0, authored`&w`; non-solid; Terrain; no Vegetation/Wall/Solid/Plantable/Barren | Owned Examinable; no Material, Thermal or Destructible; no cultivation/service/source/hazard meaning. |
| OverwritNewGrowth | `,` layer1,`&g`; non-solid; Terrain + Vegetation, no Wall/Solid | Owned Plant material with Organic/Plant tags and positive finite combustibility; live destructible, no wreckage; neutral owned Thermal. |
| OverwritPilgrimBench | `_` layer1,`&y`; solid; not Terrain/Vegetation/Wall (no Solid tag required) | Owned Wood with Organic/Wood tags and positive finite combustibility; live destructible, no wreckage; neutral owned Thermal; no Restable. |
| OverwritWaymarker | `|` layer1,`&w`; solid; not Terrain/Vegetation/Wall (no Solid tag required) | Owned Stone with Stone/Mineral tags and zero combustibility; live destructible with exact`Rubble` wreckage; no Thermal; no sign/service authority. |

Require the exact owned Examinable part for all four, without matching flavor
prose byte-for-byte. Destructible means non-Gone, non-Indestructible,
`0 < HP <= MaxHP`, nonnegative Hardness and the stated wreckage identity; do
not freeze HP at its blueprint maximum. Thermal means finite temperature and
thresholds, above FreezeTemperature and below FlameTemperature; validate the
owned part before reading it. A StatusEffects part must be owned/unique and
have no active render-color override. Test actual applicable Wet/Burning/
Frozen effects with a positive application assertion; do not claim a refusal
counter from an effect the material system never applied. Non-rendering
unrelated effects need not force a new art state.

Reject named foreign meanings that would make these pictures misleading:
Creature/Player/Item, Brain or creature Hitpoints, crop/field-harvest/cultivated
soil/harvestable, Restable, repairable, door, container, conversation/trade or
regional sign, campfire/torch/light/workstation, trigger/snare, liquid pool,
well, tile-state source, finite film, gas/burn-off gas or service-ground marker.
Keep this as explicit existing mechanism checks, not a reflection-based
all-parts policy. State refusal must preserve the real fallback owner and
physics; never repair or remove the mutated entity during rendering.

Ground resolution scans only current **visible physical** occupants; ignore
hidden, foreign-part or carried competitors before applying conflict tests.
Require exactly one valid OverwritGround and empty live tile state. A visible
malformed matching floor, second floor, cultivation, real liquid/source/trigger
or unknown Terrain vetoes native dry ground. Exact neutral NewGrowth is the
narrow Terrain overlay exception; bench/marker and covering actors/items keep
the actual substrate beneath them without being suppressed. Without an actual
floor, foreground may resolve but the adapter must not invent ground. Hidden
competing cultivation must not cause a black hole in otherwise disclosed
ground; current visible cultivation on the actual floor must still refuse.

## Proposed native cells and palette

One small original atlas, **8 cells /128×24**, no new animation or adjacency
system. Root may adjust this map before tests/source are frozen:

| Cells | Parts and material intent |
| --- | --- |
| 0–3 | Opaque scraped ground: quiet pale warm-grey base plus three very low-contrast, sparse smooth scuffs. No stones, tiles, cracked paving, regular crosshatch or bright repeated flecks. Reuse the existing stable75%-base variant distribution. |
| 4–5 | Transparent low shoots: small sage clusters near the foot of the cell, equal clipped tip height; two modest cluster shapes. No tall grass, flowers, harvest fruit or swaying state. |
| 6 | Transparent plain wooden bench: clear seat/feet silhouette, restrained grain, no upholstery or service icon. All five current Pilgrimage scopes have outward east, so its inward read is west; verify actual view, not3D yaw. |
| 7 | Transparent short blank stone marker: compact rounded/chipped silhouette, slightly cooler than floor, no engraved symbols. Source/fixture-only until naturally seen. |

Palette roles: dark muted taupe outline/contact, pale chalk-grey ground,
slightly warmer grey scuff, muted sage shoots with a restrained light tip,
desaturated brown bench with a warm edge, cool grey marker. Keep contrast
concentrated in interactable silhouettes. Compare actual day/night lighting;
avoid a luminous white sheet. Accepted atlases need no edits or forced reuse
for these distinct owners. Preserve the existing shared player/equipment/item
art. Stairs and the three observed crop species remain explicit coverage gaps.

The Blank's actual8 shoots are sparse by source. Native material can make that
space deliberate, but cannot itself prove the chunk feels sufficiently full.
Review actual visible screens at normal zoom; keep a named fullness defect if
needed. Do not add phantom rocks/furniture or treat black fog borders as empty
generation. No composition change is part of this proposed sprite slice. If
matched gameplay still feels unfinished, a separately tested change to real
generation is within the user's existing authorization; retain the source's
deliberately sparse regional identity and do not request routine permission.

## Bounded native tests and counterchecks

Use current factory content and the existing regional submitted-paint fixture;
no new test harness. Run/save genuine RED before runtime. Parameterize only
meaningful distinct contracts rather than copying every unrelated suite.

1. **Four identities + portrait:** exact native world/subject frame families,
   real owner preserved; wrong blueprint/glyph/layer, reskin and foreign/duplicate
   owned parts refuse. Ground-only public result suppresses its own glyph;
   actor/item above it remains the selected foreground.
2. **Scope authority:** natural seed65 Green/Blank/Pilgrimage/Dry generation
   plus exact22 canonical admission; excluded2,11/depth1/other biome; mutate
   current map/POI, replace cached graph and restore; saved lair with POI
   cleared still refuses. Probe no state mutation during classification.
3. **Placement/disclosure:** visible→hidden→visible, unexplored, detached or
   stale owner, custody, zero/foreign stack, single-current versus enlarged or
   stale footprint. Tests use public `zone.GetEntityCell` and
   `zone.TryChangeFootprint`; `Entity.SpatialZone` is internal to runtime.
4. **Actual ground layering:** each exact dry foreground above real floor;
   remove floor and prove no invented substrate; duplicate/unknown terrain,
   live coating/heat/charge and cultivated floor refusal. Include the real
   seeded crop beds (Plantable + CultivatedSoilPart) rather than only an invented
   competitor. Pair visible cultivation veto with hidden/foreign/custodied
   competitor ignoring, then restoration.
5. **Essential object state:** partial structural damage retains truthful body;
   zero/Gone/Indestructible/wrong wreckage refuses. Native source thermal
   thresholds/nonfinite values and applicable render-affecting status refuse.
   Execute real `DestructionSystem.Damage` for growth/bench/marker: removal,
   floor remains, marker produces Rubble exactly once; renderer does not cause
   it. Restore the test factory static afterward.
6. **Submitted result and optional lifetime:** first full paint then ordinary
   dirty-cell repaint shows the exact floor/body, removes a destroyed body,
   retains covering player/crop and tile-state glyphs; no false broad redraw
   claim. Missing/wrong atlas and null selector fall back, never suppress a
   real owner; dispose twice, borrowed texture/selector survive. Authoritative
   public fallback returning true is not a native-art pass.

No allocation measurement is promised by source inspection. Avoid per-frame
construction and validate any future allocation counter with a positive
allocation control; this platform previously returned unusable zero readings.
Root's actual after comparison must repeat Green/Blank/Pilgrimage/Dry, the
observed4,12 bench reader, normal movement/action, F5 and fresh Continue,
preserving saves and reporting remaining crops/stairs/marker limits.

## Pre-implementation verdict and source trail

No source blocker to a small four-owner kit. Main corrections are the real
cultivated source-floor mutation, non-service furniture and unobserved marker.
This is a proposed contract, not a native test result, runtime implementation,
gameplay acceptance or packaged release.

Reviewed: `Objects.json` inherited definitions; `OverwritCompositionPlan.cs`;
`Builders/OverwritCompositionBuilder.cs`; `OverworldZoneManager.cs`;
`BiomeCropPlacement.cs`; `LairStacks.cs`; `FlatPixelFallbackCatalog.cs`;
`FlatPixelGrovelandsMaterialsCatalog.cs`; relevant Greybox routing;
`RenderPart`, `ThermalPart`, `DestructiblePart`, `DestructionSystem`,
`ExaminablePart`; existing regional test setup/submitted paint conventions;
the census and four direct actual baseline views named above.
