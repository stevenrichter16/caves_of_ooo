# Grovelands: independent preflight and visual references

Root preservation note: the two actually viewed NPS files are also retained at
`ArtSource/Live16x24/grovelands-materials/concepts/references/` with their original
filenames. Scratch paths below record where the reviewer first viewed them.

2026-10-27. Read-only review of `Docs/Verification/Live16x24/grovelands-next-kit-preflight.md`, inherited boot object definitions, formation/composition builders, pool projection, finite well service, harvest service and the current fallback selector. No Unity, Git, production edits, tests or game execution. Root owns actual captures, imagegen, native implementation and acceptance. Two small reference images were downloaded to scratch and actually viewed; no large downloads.

## Material correction to carry into the plan

**Exhausted GroveSeep is not necessarily dry.** `Assets/Scripts/Gameplay/World/Map/Zone.cs:796–806` projects every ordinary `LiquidPoolPart` as permanent tile coating without checking Volume. The absence of a `TileStateSourcePart` does not prevent that projection. `Gameplay/Settlements/WellDrinkingService.cs:34` decrements finite stock but does not remove the water coating. Thus the real last draw can leave Volume0, unavailable drinking, and still-present water. The exhausted filament art must preserve that wet truth, through its own restrained wet pixels and/or the actual water overlay. It must not imply replenishing stock, dry safe ground or an inexhaustible well.

`Gameplay/Items/LiquidVesselService.cs:290–294` makes `LiquidSourceSafety.IsUnmixedPool` reject Volume<=0. That action-availability helper cannot be a blanket condition for resolving the empty-state sprite. Use the actual current coating/owner state separately. `LiquidSourcePhase` also checks existing ice, Frozen and thermal state; `WellPart.IsUsable` checks structural repair state. The neutral available-water frame must not advertise clean usable water through those states. Keep charge/heat/mixed-liquid cues independent of whether the water base has native pixels. These findings were sent to root and the test author immediately.

Strong paired check: start with a real added water seep at Volume1, use the actual draw transaction, then prove Volume0/unavailable drawing, retained water coating, same owner and truthful empty appearance. Do not manually clear the fixture coating to make a dry-state expectation pass. A separate genuinely removed coating is a different state.

## Other sharp semantic boundaries

- **Do not copy blanket plant-part requirements.** The inherited Sign, Row, Cache, RedGrowth, ChoirIronVein and Seep do not own Material/Thermal/Destructible. Column and FruitingBody own all three; WineLeafSundew owns Material and Destructible but no authored Thermal. Requiring those absent parts would silently exclude real ordinary owners. Runtime-added foreign or exceptional parts still require honest refusal rather than fabricated defaults.
- **Cache is an overlay, not a new ground substrate.** CompostRow and CompostCache both have layer1 and no Terrain tag. The generator inserts a cache after its actual row; `FlatPixelFallbackCatalog.cs:72` resolves equal layers with the later visible current owner. Preserve that selector truth, including covered/reversed-order counters, and keep the literal row after cache harvest/removal. Do not unconditionally force cache priority. A failed 60% yield consumes the cache too; neither the art nor test should promise coins.
- **Non-solid does not mean harmless.** WineLeafSundew is a low, repeatable step snare. Its exact GreatdewSnare values are45% grab, three turns, corrosion1.5; it is not a Creature, harvest source or one-shot mine. A darker wine rosette with sticky pale edge points should be visually distinct from the low red forage growth. No invented animation/readiness stage is necessary.
- **Ordinary shelves are not all fungal states.** FruitingBody is solid and destructible, without Harvestable. BloomingFruitingBody is a distinct exact identity with BurnOffGas producing bloom-spores on heat/fire. Never alias the latter to the former by a generic fungal tag or shared `%` glyph. A destroyed shelf reveals the actual substrate rather than a decorative stump fabricated by art.
- **Keep map and owner authority live.** The preflight's37 ordinary addresses are a source-derived candidate list, not observed visits or permanent admission. Preserve exact cached graph, current biome/POI and read-only saved-lair vetoes; exclude the authored special doll grove and site/underground graphs. Grass, visible neutral dry foreground owners and native connected VineWall can provide a coherent envelope without changing generation. Hidden neighbors cannot influence visible join shape. Unknown live coating, crops and hazards must not be painted away to make chunks seem fuller.

## Selected image references — actually viewed

### 1. Clustered fungal shelves: form, not a borrowed game palette

[NPS, Shelf Fungus in Denali — Robbie Hannawacker](https://npgallery.nps.gov/AssetDetail/ad84799e-3f6f-403c-890c-8987d42b9797). Metadata identifies the photographer and explicitly lists public-domain/full granting rights. [Official small image](https://npgallery.nps.gov/GetAsset/ad84799e-3f6f-403c-890c-8987d42b9797/proxylores.jpg).

Viewed500×375 photo,89,051bytes, scratch `/tmp/coo-grovelands-reference-images/shelf-fungus-nps.jpg`, SHA-256 `60669ca2dfa4b9b253960c529828a3cdc1d457ff482caa5b019fc8e5ba15054a`.

Three irregular cauliflower-like shelf clusters interrupt a dark trunk. Their useful feature is the broad folded lobe and broken, shallow overlap, not an evenly spaced stack of discs. Translate each16×24 FruitingBody into two or three interlocking lobes with a firm base and selective lip highlights; retain the authored violet-over-ochre palette. Avoid tracing the photograph, tiny repeated cream frills, a masonry outline or repeating a bright circular head in every shelf cell. This is a temperate fungal-form reference, not evidence that Grovelands should reproduce Denali ecology.

### 2. Wet forest structure: pale bases, dark connected water and quiet openings

[NPS, Big Cypress National Preserve, Florida — NPS staff](https://npgallery.nps.gov/AssetDetail/1d5ce823-4f85-48e8-91d7-de3d877c37aa). The official asset metadata identifies NPS staff and public-domain status. [Official small image](https://npgallery.nps.gov/GetAsset/1d5ce823-4f85-48e8-91d7-de3d877c37aa/proxylores.jpg). [NPS ecosystem context](https://www.nps.gov/ever/learn/nature/cypress.htm) describes cypress formations and elongated strands aligned with water flow.

Viewed333×500 photo,49,059bytes, scratch `/tmp/coo-grovelands-reference-images/cypress-swamp-nps.jpg`.

The photo contains broad pale trunk bases, slimmer receding trunks, dark olive water, sparse foreground leaves and readable open water between the masses. Borrow that value hierarchy: low-detail dark ground/wet lanes; medium-value connected green vegetation; infrequent pale fungal columns as landmarks. Adapt the widened rooted base into a compact column silhouette, without turning columns into stone pillars. Keep the actual TendrilFen water veins connected and darker than banks. Do not reproduce photographic reflections, dense single-pixel twigs, perspective depth, or trees spilling over traversable cells. This subtropical freshwater-swamp reference informs composition; it does not replace the game's actual terrain, biome name or hydrology.

### Supplemental scientific context — text/metadata only, not a viewed image

[Kew, The hidden world of fungi](https://www.kew.org/read-and-watch/the-hidden-world-of-fungi) explains branching mycelium through soil/organic material and identifies a root-associated false-truffle photograph credited to Laura M. Suz, ©RBG Kew. The page and caption were verified, but the direct image request returned HTTP403; it was not viewed or saved, and no bypass was attempted. Use this only as conceptual support for sparse branching filaments, not as a claimed visual reference or freely reusable texture. The two NPS images above are the selected production-study references.

## Whole-chunk art direction for the first trial

1. Keep accepted Grass/Tree/Bush/VineWall pixels stable unless a named mismatch is demonstrated in actual captures. Establish the region through its existing fungal columns, low compost rows, shelves, seep and danger plants. Do not tint every reused asset violet merely to signal a new biome.
2. Solid shelves/columns need connected opaque body masses. Non-solid red growth, compost and cache need low porous silhouettes with visible substrate. Keep the sundew's radial dark-wine outline distinct from food-looking red clusters. Pale seep filaments and column veins are accents, not a luminous carpet.
3. The Grove center and three-cell approaches are intentional breathing room. TendrilFen water/bank braids, FruitingWall broken shelf beds and CompostingField straight low rows already provide four distinct compositions. Preserve those structures; use sparse static variation to break repetition inside their actual occupied cells. Do not add unrelated ruins, new roots across paths or filler furniture.
4. Judge actual ordinary-zoom whole views in all four formations. Ask whether routes and water are readable, column/shelf/plant roles separate, foreground/background contrast survives real light, cache removal exposes the real row, and repeated bright motifs become a carpet. Missing resident/creature sprites and encountered optional hazards remain explicit next work; successful material art is not all-biome or chunk-fullness completion. Fog is not counted as bare space.

Bounded conclusion: the cohesive material/envelope scope is sound after correcting finite stock versus persistent wetness and respecting the unequal inherited owner shapes. No production behavior or visual acceptance is established by this preflight.
