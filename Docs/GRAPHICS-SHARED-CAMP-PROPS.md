# Next surface props: shared camp storage and water fixtures

Recommend **Sack + Urn + ordinary Well**, using three unchanged legacy16×16 bodies. This closes visible camp functions and reuses existing resources across settlements/regions without a new art packet. No implementation or acceptance is claimed.

**No duplicate scope:** `Docs/GRAPHICS-SHARED-STORAGE.md` explicitly limits the shipped catalog to Chest/LockedChest/StrongBox/Crate/OreCache plus mimic disguise. Sacks and urns remain excluded. No current FlatPixel catalog match for Sack/Urn/Well was found.

## Exact contracts and reusable art

| Owner | Source correction | Existing immutable resource |
|---|---|---|
| Sack | `Objects.json:30388`: PhysicalObject, `u/&w`, layer1, **passable**, Container capacity6. No authored Material/Thermal/Destructible; do not invent cloth-health, empty/open sprites or Solid=true. | `Assets/Resources/Sprites/Environment/cont_sack.png`,16×16, SHA`d1a4c73c85a9aa5d115456cbb37fa106746d9a2cfcb6298fc6911d912488c9b4` |
| Urn | `Objects.json:30442`: `8/&y`, layer1, solid Container capacity5. No authored Material/Thermal/Destructible despite “clay” description; contents remain mechanic-owned. | `.../cont_urn.png`,16×16, SHA`02c6e0906e1789fbd070edb6267a6303585cc045d86f4e70220befe559d15690` |
| Well | `Objects.json:24332`: `O/&c`, layer1, solid, WellPart; **no LiquidPool** in ordinary blueprint. No invented finite reservoir or depletion state. | `.../well.png`,16×16, SHA`4d8a33eced838078f84360ecd8d04a9d4921b06ee8ca1c4ec4a22c72406940c4` |

All three viewed directly. `EnvironmentSpriteRenderer.cs:210–211,756` confirms established mappings. Cache centered FullRect16PPU views; preserve raster/meta originals. Exact editable raster provenance remains unverified; `ArtSource/Clearing16x24/kit/stone-well.png` and 3D well/sack models are distinct assets, not demonstrated source files for these PNGs.

`ContainerPlacementService.cs:59–106,123–129` places Sack across Beating, Spread, Sodden, Grovelands and settlement pools; Urn also occurs in desert/jungle pools. `VillagePopulationBuilder.cs:545–574` and `FirstTentCompositionPlan.cs:54–55` place wells.

**Well caveat:** `WellDrinkingService.cs:7–14,35–38` supports unlimited ordinary wells and finite pools only when actually present. `WellPart.cs:18` respects repair faults. `SettlementSiteVisuals.cs:45–71` deliberately changes Well base color/name through four repair stages. First scope should exclude WellSite/custom-state wells or explicitly preserve their existing stage cues; never replace them with an unconditional white native “working well.” No query may drink, repair or alter stock.

## Practical actual baseline

Current `beating-actors-after-06-brine-lens.png` still visibly contains camp glyphs. Historical current-paint receipt `beating-final-03-brine-lens.json` identifies Sack35227 at41,2, Well35212 at51,6, GuestClothPole35213 at54,6 and nearby TentWalls. Reacquire via ordinary controls from earned `beating-actors-drifter136` (17,13,turn3290,HP38). CaravanRoad final05 receipt identifies Urn39386 at47,7; earned road135 is19,13,turn3550,HP33. Historical disclosure is not current access proof; obtain exact readers before RED.

TentWall/GuestClothPole remain important next visual gaps, but no exact existing flat PNG was identified. TentWall is solid Cloth8HP terrain; pole is passable Furniture, not a service (`Objects.json:33942,33992`). Existing Wellmeet/FirstTent/Sodden libraries are3D. Do not borrow stone walls or signposts deceptively. After this reusable batch, commission a coherent tent/cloth kit if actual views still prioritize it. Whole-area fullness remains separately open.
