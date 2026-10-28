# Shared camp cloth: next visible family

Status: source triage, actual camp baseline and one imagegen reinterpretation complete; native sprite authoring/integration/actual after review pending. No acceptance or new runtime coverage claimed.

## Why next

BrineLens current disclosed camp has10 TentWall glyphs and1 GuestClothPole alongside the ordinary well and sack. That envelope is a larger visible gap than another isolated small prop. The same owners occur at FirstTent/Wellmeet/Gantry and DrownedLedger. Source patterns need no new spawns or density machinery; no cave work.

## Source corrections before implementation

| Owner | Actual source | Required boundaries |
|---|---|---|
| TentWall | Objects.json33942, Terrain, #/&y,layer2,solid,Cloth,8HP,thermal260/.8 | Preserve straight cell boundaries and actual walls. No roof across entrances, stone substitution or topology change. Damage/destruction and thermal remain mechanic-owned; use existing cues. |
| GuestClothPole | Objects.json33992,PhysicalObject, vertical-bar/&Y,layer2,passable,Furniture | Slender ochre post and small dark cloth. Description mentions three days but the owner does not implement a timer/service; no fake shop/interaction. |

The vertical bar in the source row means literal glyph `|`. No exact flat raster was found; FirstTentVoxelKitLibrary/WellmeetVoxelLibrary/SoddenNativeArtLibrary entries are3D, not usable provenance for a flat PNG. Re-read cited sources when implementing inherited/custom variants.

## Art extraction and scope

Actual baseline: `Docs/Verification/Live16x24/shared-camp-props-before-01-brine.png`, earned drifter136 seed65, normal GameBootstrap. Built-in imagegen result and exact prompt/hash/critique: `ArtSource/Live16x24/shared-camp-cloth/`. The concept changes footprints and some HUD text; neither is accepted. Carry only broad cloth folds, restrained charcoal/ochre contrast, tied posts and a small hanging cloth into original editable16×24 cells. Proposed minimum is two wall variants and one pole, no animation or connectivity framework unless actual comparison identifies a need. Keep floors quiet and existing mechanics/geometry untouched.

Root acquired exact current readers through ordinary Look at49,3 TentWall35204 (receipt capture shows wall8/8 Cloth) and54,6 GuestClothPole35213; retained `shared-camp-cloth-before-02-wall-reader.png` and `before-03-pole-reader.png`. The exact wall owner is35204 from current-paint receipt, not a spawned fixture. Native runtime RED/GREEN and integration remain pending. Preserve source-cells/palette and editable native project, immutable before PNG, current props, all normal saves. Follow actual after world/reader/whole-camp composition, damage/blocking/passable-pole and visibility checks. Coverage will still not equal full settlement polish. No source/drive cleanup.
