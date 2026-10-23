# Opening loose-item artwork

Twelve compact static item identities inside native 16×24 cells. `opening-items.png` is 192×24, transparent with binary alpha, square pixels, Point filtering, 16 pixels per unit and centered Tilemap pivots. Every object touches image row22 and leaves row23 clear; seed, shard and supplies remain smaller than characters or scenery. There are no animation frames or quantity variants.

| Index | Blueprint | Real source and use |
| --- | --- | --- |
| 0 | Emberwheat | Opening ripe field rows each yield one; eat or cook. Cut sheaf, not another growing row. |
| 1 | ToastedEmberwheat | Cooking carried grain replaces it with carried food. Eating heals and gives the existing temporary heat-resistance meal. No flame or extra food beside the fire. |
| 2 | DrawgourdShell | One from the ripe opening gourd. Reusable three-drink water vessel, initially empty. Neutral hollow-neck identity does not display charges or promise water. |
| 3 | DrawgourdSeed | One accompanies the shell. Consume a carried seed to plant an eligible empty prepared bed. One small seed, no planted mound or seed packet. |
| 4 | Mushroom | Opening mushroom rings yield two to four and are removed; eat or cook. One cap rather than a live ring. |
| 5 | RoastedMushroom | Cooking carried mushrooms. Eating heals and gives the existing temporary acid-resistance meal. Browned/curling rim, no poison or fire. |
| 6 | GlowQuartz | Opening veins yield one to two and are removed. Infuse at a forge or consume one via CrackQuartz. A single matte detached shard has **no light source**. |
| 7 | HealingTonic | One in the opening chest; two in starting builds. Apply/throw its actual healing dose. No reusable empty bottle is produced. |
| 8 | DriedMeat | One in the opening chest; two in starting builds. Eat, or use as finite ground bait for eligible hunting furrowstalkers elsewhere. No promise of opening dog/marlback bait behavior. |
| 9 | FireClay | Two in the connected cellar supply crate. Repair the opening well or smother existing flames through real actions. Matte clay, no flame or lasting fire protection. |
| 10 | SalvagedTimber | Dismantle the finite cellar pallet for two. Repairs, supported trap jams, haft preparation and campfire fuel remain gameplay-owned. Two illustrated split pieces identify material, not stack quantity. |
| 11 | MarlbackCorpse | Actual Scrabbler/Gleaner death product with a 70% chance and recorded source identity. Carryable remains; no default harvest yield, food, stack or decay timer. Collapsed shale anatomy, no gore or baked equipment. |

Related world owners—field rows, gourd plant stages, mushroom ring, quartz vein, `CrackedGlowQuartz`, pallet and the living creatures—must retain their own art/state routes. This atlas adds no spawns or material supply. The crystal beacon in particular is a separate non-takeable owner with radius 4 and a thirty-turn lifespan, outside this item group.

## Ownership and lifecycle limits

None of these twelve types is a foreground owner in the initial opening census. They become visible through real harvest, overflow, deliberate drops or death. Field/ordinary harvesting packs first and leaves genuine overflow; cultivated gourd Harvest leaves shell and seed on its bed, while Gather tries the pack. Cooking only replaces carried food and rolls back if the result cannot fit.

The visible top owner controls drawing. Equal-layer ground outputs may cover one another until picked up; a seed can cover its shell. Ordinary items use layer 5, corpse layer 3 can sit under dropped equipment. Do not draw fictional combined piles or duplicate carried outputs. Ten types stack, default maximum 99 except timber 20; shell and corpse do not. Native ground placement does not merge item owners. Exact stack count remains in gameplay/UI, not these pictures.

Admit only the real opening's current visible top owner, owned visible Render and takeable Physics, with neither inventory nor equipped ownership. Reject empty/foreign stack state. Match exact blueprint and appropriate owned semantic part (Food/Cookable, Seed, Waterskin, MineralPreparation, Tonic); corpses retain their corpse/source identity. Clay and timber must not acquire invented material-part requirements. Remembered or hidden cells must not leak current loose items. Sprite removal follows actual owner transfer/removal, never an artificial art timer.

## Pipeline and review

Inputs were the actual `Docs/Verification/Live16x24/native-states-gourd-harvest.png` capture, accepted anchor/state contact sheets, and `../concepts/loose-items-v1.png`. Exact generated prompt and critique: `../concepts/loose-items-prompt.md`. Kept the open-neck vessel, tied meat strips, compact clay and collapsed plate motifs. Rejected generated shading, irregular pixels, three-prong loose crystal and cylindrical/isometric timber.

`Tools/Live16x24/build_opening_items.py` authors integer mesh cells and renders native PNG and the same geometry at integer 8× scale. It never reads, samples, crops, resizes or edits raster images. `opening-items.blend` holds editable named frame collections. `source-cells.json` records every authored pixel, and `manifest.json` includes real sources, uses, limits, palette, bounds and atlas hash.

Independent first-render review found the small seed readable at native size, the detached shard distinct from its vein, and loose items distinct from source scenery. The second pass made the seed contiguous, narrowed and colored the raw mushroom stem to the accepted olive palette, trimmed the clay's lower corners and restricted corpse rust to covered joint pixels so its baseline does not resemble fresh blood. Actual ground visibility remains a separate Play acceptance gate.

Rebuild from the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Live16x24/build_opening_items.py
python3 ArtSource/Live16x24/items/verify.py
```

The verifier performs read-only PNG analysis and writes only `verification.json`: exact native/source/contact pixels, binary transparency, compact bounds, original palette values, open neck, one contiguous seed and quartz shard, two separate timber pieces, distinct cooked silhouettes and prior accepted atlas hashes. No Unity or runtime integration is claimed by these source files.
