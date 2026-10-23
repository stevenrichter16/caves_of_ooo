# North-route native vegetation

Status: original source art rendered, statically reviewed and integrated in the opt-in native route, 2026-10-23. This packet supplies four native identities for the already captured north connector and Nella route. Unity import, exact-owner selection and ordinary live checks subsequently passed; see the [integration record](../../../Docs/LIVE-16X24-NORTH-VEGETATION.md) for312 distinct native test cases, captures and limits. This source packet does not claim that the route's art is complete.

## Rebuild and inspect

From the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Live16x24/build_north_vegetation.py
python3 ArtSource/Live16x24/north-vegetation/verify.py
```

- `north-vegetation.blend`: named, editable mesh collections and flat emission materials. One mesh unit corresponds to one native pixel. The separate contact scene instances the same geometry.
- `north-vegetation.png`: 64×24 atlas, four 16×24 cells in the fixed order below.
- `north-vegetation-contact.png`: a direct 8× geometry render, 1280×1152. It includes four adjacent horizontal hedges, three vertical hedges and a three-cell elbow. This is not a resized atlas.
- `source-cells.json`: exact named-part/color cells, top-left origin, generated from the original geometry instructions rather than sampled from an image.
- `manifest.json`: source references, rectangles, palettes, bounds, uses, limits and current PNG hash.
- `verify.py` / `verification.json`: read-only source/native/contact and prior-art preservation checks; the validator writes only its JSON receipt.

Intended runtime resource: `Sprites/Live16x24/north-vegetation`. Import at 16PPU, centered tile pivot, Point filtering, Clamp, no mipmaps or compression. Every frame stays inside one 16×24 cell. Tree, berry bush and dry brush touch row22 and leave row23 transparent. Hedge deliberately touches all four edges, including row23, to represent a continuous solid boundary.

Frozen PNG SHA256: `f6d1ede626402f9b7963b4aba7434044f10a925376e4da74c73e4dfb94e68777`.
Decoded RGBA SHA256: `2c04f244a468fb235fe2bf82be2ba891c52c84c926e09ff5a2d389495eea9979`.
Cycles writes variable timing text metadata in PNG files; rebuilding can change the whole-file checksum while preserving every decoded pixel. The manifest records each rendered artifact's current hash. No raster metadata stripping or rewriting is performed.

| Index | Exact identity | Readable shape | Actual semantic contract |
| --- | --- | --- | --- |
| 0 | `Tree` | Tall three-lobe crown over an exposed branching trunk and small roots. | Ordinary solid, destructible wood/vegetation owner. No fruit, harvest, canopy footprint expansion or new collision is implied. |
| 1 | `Hedge` | Dense matte bramble mass with restrained woven stems and contacts on both axes. | Existing solid, non-takeable hedgerow. One frame serves rows, columns and corners without adjacency variants or a neighbor scan. It promises no berries or fence service. |
| 2 | `BerryBush` | Low broad foliage with three small rust-red fruit accents. | Non-solid plant whose unspent `HarvestablePart` yields 1–3 `WildBerries` and removes the source on success. The accents identify the plant, not the yield count. No standing spent-bush state exists. |
| 3 | `DryBrush` | Sparse warm brown forks with pale broken tips and open negative space. | Non-solid dry, combustible, destructible vegetation. No green leaves, harvest produce, permanent fire or extra obstacle. |

Ordinary `Bush` and `Reeds` do not receive redundant new frames in this packet. Their low-bush and tall/short-reed silhouettes can deliberately reuse accepted base frames5 and3/4 through exact semantic routing. Their owner checks, variant choice and invalidation are runtime responsibilities. Existing authored VisualIDs and user-approved characters are not rewritten.

## Source sweep and production decisions

The route and owner/state inventory are recorded in `ArtSource/Live16x24/concepts/north-parts-plan.md`. Blueprint contracts were checked in `Assets/Resources/Content/Blueprints/Objects.json`: `Tree`, `Hedge`, `BerryBush` and `DryBrush`. Harvest source removal was checked in `Assets/Scripts/Gameplay/Items/HarvestablePart.cs`. There is no new gameplay rule, population, yield or state in this art packet.

The real inputs are the saved `north-greybox-entry.png`, `north-greybox-works.png` and `north-greybox-plot.png` captures in `Docs/Verification/Live16x24`. The parent walked the actual route, examined the works and completed Nella's terms/trade interaction before authoring. That establishes existing owners and geometry; this packet does not relocate them or create decorative stock.

Direction references are `north-works-concept.png`, `north-plot-concept.png` and `north-parts-reference.png` under `ArtSource/Live16x24/concepts`, with their saved prompts. The extraction's broad tree crown, woven hedge, low fruit bush and dry forks inform the native shapes. Generated gradients, partially transparent halos, irregular pixel grain, oversized hedge width and green leaves on dry brush were rejected. No reference raster was sampled, sliced, imported, resized or edited.

The first production pass created original integer-coordinate quads in the accepted muted leaf/wood/rust palette. Only 3–6 opaque colors occur in any cell. Tree height and exposed trunk distinguish the obstacle from the low harvest bush; dry stems have much less occupied area. The hedge uses a dense core with broad flat leaf clusters and short brown weave marks. There are no glossy highlights, shadow planes, light haze or cross-cell branches.

The second visual review inspected both the native64×24 atlas and direct8× contact beside the accepted base art and actual works capture. Horizontal, vertical and corner repeats remain connected without full-cell-looking passages. Independent review found no blocking silhouette issue; the repeated leaf pattern is visible at8× but restrained. No extra surface detail was added merely to make another revision. Actual world contrast, density and interaction readability remain the parent's live capture gate.

## Verification and bounds

The verifier was written first and failed because the source atlas did not yet exist. After rendering, it passes:

- Exact64×24 native dimensions, correct frame order and rectangles, binary alpha and3–6 colors per frame.
- Zero source/native pixel mismatches; zero contact/native mismatches for every8×8 pixel block and all repeated hedge instances.
- Four distinct, connected silhouettes. Object baselines and per-cell side bounds are respected; only the explicit hedge boundary may reach all four edges.
- Tree22 pixels high with a visible trunk; berry bush10 pixels high; dry brush49 occupied pixels, berry bush90, hedge364.
- Hedge has18 matching horizontal edge contacts and10 vertical contacts; repeated row/column/elbow silhouettes remain connected.
- Dry brush contains no green leaf colors, and hedge contains no berry colors.
- Approved Seed Keeper, cook, tatterjay, grazer, beetle jar and base/cast PNG bytes match HEAD. Anchors, states, motion, items and cellar match their accepted manifest hashes.

These checks establish source integrity and the stated static shape constraints. They do not prove Unity binding, current owner/state authorization, fog, harvesting, fire, collision, UI portraits, renderer allocation or performance. There is no motion or state-specific fire/damage art in this packet. The full north-route works, actors, crop states and products remain separate production groups. Integrate only after the runtime team's native RED gate, then inspect the real route at1280×800 before treating this packet as accepted in-game.


## Integration continuation

The original source-only gate above is now followed by native Unity RED→GREEN and actual gameplay evidence in [the vegetation record](../../../Docs/LIVE-16X24-NORTH-VEGETATION.md). Import retained64×24 Point/Clamp/no mip/uncompressed; exact semantic selectors and resource lifetime passed. Ordinary seed64 travel checked the native entrance, hedge collision/open gaps, berry portrait and harvest/removal, and trees/reeds at Nella's plot. The same four source frames remain frozen. No damaged/burning animation or physical Deck acceptance is implied, and source-only verification should still report only the checks it actually performs.
