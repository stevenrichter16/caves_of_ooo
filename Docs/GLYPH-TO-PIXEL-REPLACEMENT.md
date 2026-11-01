# Glyph → pixel replacement — plan and self-prompts

**Status:** planned 2026-10-31. Starts after P5 (`UPPER-TIER-BESTIARY.md`) lands.

**User direction (2026-10-31):** replace every object, NPC, monster and item that
still draws as an ASCII glyph in the game with an appropriate 2D pixel asset.
- Some already have an asset that was never wired in.
- Some need new art.
- New art takes Caves of Qud's sprites, tiles and objects, and the project's
  own sprites, tiles and object art, as references and inspiration.
- Each step runs from a written prompt that I give to myself and then carry
  out.

## Ground rules (from `CLAUDE.md` and the art direction)

- **Format.** The game target is flat 16×24 pixel art (`LIVE-16X24-ART-DIRECTION.md`).
  The shared 16×16 environment sprite is the accepted format for creatures and
  props that have no 16×24 body (the Merchant/Elder precedent).
- **New art is original and editable.** Each asset gets a pixel-row Python
  source under `ArtSource/Live16x24/<family>/` that writes the PNG.
  - **References:** Caves of Qud tiles and the project's approved sprites are
    studied for silhouette, readability at 16 px, palette discipline and how
    a creature or object is "said" in one tile.
  - **No copying:** no Qud tile is copied, traced or recoloured into the
    game. They are style references only.
- **Admission rules.** Admission happens in the flat catalogs. Only canonical
  owners are drawn natively: correct glyph and colour, visible, untinted, the
  owner of its own parts. Anything else keeps its glyph. Opening-admitted
  zones keep their own routes.
- **Verification.** Every slice runs RED → GREEN → counter-check → adversarial
  where it applies → review → living-doc update → commit. Each slice also
  needs offline renders through the real catalog, and live captures where
  reachable.
- **Preserve.** Approved art, normal saves, the 49 foreign paths and existing
  builds are not touched.

## The pipeline, as self-prompts

Each prompt below is the exact instruction I give myself for that step. A
step is done when its output exists and is checked, and not before.

### Prompt 0 — Reference shelf

> Build a reference shelf, read-only, outside `Assets/`. Extract the Caves of
> Qud creature, NPC, object and item tiles from the local Steam install.
> - Install UnityPy into a private venv in the scratchpad.
> - Never write into the Qud install.
> - Never put Qud images into `Assets/` or `ArtSource/`.
>
> Then build contact sheets by category (creatures, humanoids, fauna,
> furniture, containers, plants, weapons, armour, tools, consumables, books)
> at 8× scale. Build the matching sheets of the project's own approved sprites
> (`Sprites/Live16x24`, `Sprites/Environment`, `Sprites/Actors`).
>
> Write `ArtSource/Reference/README.md`: where the shelf lives, that it is
> reference only, and the palette and outline conventions the project already
> uses.

### Prompt 1 — Census: what still draws as a glyph

> Write an EditMode census bench, `GlyphCensusBenchTests`, using the
> place-census pattern:
> - Generate the whole world for seed 64: every surface zone, sinkhole stacks,
>   cave floors 1–3 under every fourth zone, every place and site.
> - In each zone, ask `FlatPixelGreyboxCatalog.TryResolvePresentation` for
>   every cell's top owner.
> - Record every owner that resolves as a glyph (not `NativeForeground`),
>   grouped by blueprint. Record count, zones, kind (creature, NPC, item,
>   prop, terrain), glyph and colour, and whether a matching sprite file
>   already exists under `Resources/Sprites`.
>
> Also enumerate every blueprint that can be carried, to cover items seen in
> inventories and on the ground. Write `Library/ArtCensus/glyph-census.txt`.
> Pin the counts in a test so later slices show the number falling.

### Prompt 2 — Triage

> From the census, write a triage table into this doc. Each glyph owner goes
> into one bucket:
> - **A — asset exists, unwired:** a sprite file or atlas frame already
>   exists; only admission is missing.
> - **B — shares an existing body:** a variant or reskin that should wear an
>   approved sibling's body (the elder pattern).
> - **C — needs new art.**
> - **D — deliberately glyph:** a UI-only marker, or something canon wants
>   unrendered.
>
> Order the work by how often the player sees it: creatures and NPCs first,
> then items on the ground and in hand, then props and furniture, then the
> rest.

### Prompt 3 — Bucket A and B slices (wiring)

> For each family of bucket A/B owners, extend the matching flat catalog (or
> add a table-driven one, as P5 did in `FlatPixelWildFaunaCatalog`):
> - canonical admission: glyph, colour, untinted, single cell, alive for
>   creatures, not carried for props;
> - world and reader portraits from the same sprite.
>
> Tests:
> - every owner resolves natively with the right texture and rect;
> - a recoloured or dead owner keeps its glyph;
> - opening zones are untouched;
> - the census count falls by exactly the admitted owners.

### Prompt 4 — Bucket C: art direction per family

> For one family at a time (for example, the "upper beasts" or the "kitchen
> props"), write a short brief before drawing. For each asset it states:
> - the silhouette in one sentence;
> - the two to four colours that say what it is;
> - the Qud tiles and the project sprites studied as references (by name and
>   category), and what each one teaches (pose, outline weight, highlight
>   placement);
> - its read at 1× and at 2× next to the player.
>
> Then draw it.

### Prompt 5 — Drawing

> Write the asset as pixel rows in `ArtSource/Live16x24/<family>/make_<family>.py`,
> in the established style: a near-black outline, flat fills with one
> highlight and one shade, bottom-aligned, a 16×24 cell (or 16×16 shared
> format for small props and fauna).
> - Generate it.
> - Composite an 8× preview on each biome's ground beside the player and the
>   nearest approved sibling sprite.
> - Look at it. Fix what does not read.
> - Keep each round's preview in the scratchpad.

### Prompt 6 — Independent art review

> Give the previews to a blind reviewer, a fresh agent with only the art
> direction doc. It judges, per asset: whether it reads as the named thing,
> whether it fits the shared style beside the existing sprites, and whether
> it is recognisably a copy of any Caves of Qud tile (it must not be).
> Redraw what fails.

### Prompt 7 — Integration

> Import with the shared sprite meta: point filter, 16 pixels per unit, no
> compression. Admit the asset through the catalog (Prompt 3's contract) and
> add it to `world-sprite-coverage.json`. Run the FlatPixel suites and the
> family's tests.

### Prompt 8 — Live verification

> Where the place is reachable, capture actual play through the Unity Editor
> (`live-editor-play-capture` memory): the world view, the reader portrait,
> and one interaction (fight, pick up, talk). Compare against the offline
> render. Record exactly what was verified and what was not.

### Prompt 9 — Close the slice

> Update this doc's ledger:
> - the census count before and after;
> - assets added;
> - reviewer verdicts;
> - live evidence.
>
> Commit with the §2.3 template, staging explicit paths and never Live16x24
> `png.meta` whitespace churn. Push, and verify that HEAD equals origin/main.
> Then return to Prompt 2's queue.

## Census (static, 2026-10-31; the runtime census confirms it)

A blueprint is counted here when no flat catalog names it.

| Kind | Blueprints | Not named by any flat catalog | Of those, a sprite exists |
|---|---|---|---|
| Creatures | 79 | 35 | 23 |
| NPCs | 50 | 34 | 24 |
| Items | 286 | 237 | 0 |
| Props | 227 | 149 | 11 |

## Slice 1 — bucket A people and creatures (wired)

47 shared 16×16 bodies under `Sprites/Environment` were never admitted to the flat view.
They join the `FlatPixelWildFaunaCatalog` shared-bodies table:
- **creatures:** bandits, husks, cave bat and bear, slime, prowler, spider, glowmaw,
  lantern moth, helmwood frog, ice wight, jungle ape and stalker, three marlbacks,
  ruin scavenger, sand wurm, skeletal sentry, sleeping troll, spore shambler;
- **people:** apothecary, arcanist, armorer, catacomb warden and tenders, the hermits,
  encased elder, Grib, Marceline, Mogu, Nam, Sien, Sopp, the pale curator, provisioner,
  envoy, the Stillleaf pair, undertaker.

Test: `FlatPixelSharedBodiesTests`.

## Slice 2 brief — twelve creatures with no art (bucket C)

Format: shared 16×16, the bucket-A siblings' format. Outline (22, 27, 24), flat fill, one
highlight, one shade.

**References studied**
- **Qud** creature atlas silhouettes:
  - low quadrupeds: tail line and head wedge;
  - birds in profile: one wing raised to separate it from the body;
  - coiled snakes: an S in three loops;
  - squat humanoids: wide shoulders, small head.
- **Project:** the marlback domes (`marlback_tunnelguard`, `_wallkeeper`, `_breacher`),
  `viper`, `magpie`, `rotling`.

| Creature | Silhouette | Colours that say it |
|---|---|---|
| Marlback soursprayer | marlback dome, hooded | green-stained throat wrap, a brine pouch |
| Marlback stormbinder | marlback dome | blue-stained shale plates, one spark pixel |
| Ditch-cutter mate | short-faced marlback | pale diagonal rake marks; a cudgel beside it |
| Notched ditch-cutter | broad marlback | a pale notch in the top plate; a blade beside it |
| Half-set intake | a human frame pulled by a tendril | one shoulder salt-white, green growth across the ribs, a tag |
| Dirt gnome | squat, wide claws | soil-brown skin, darker creases |
| Furrowstalker | low narrow quadruped in profile, tapering tail | dark brow, pale jaw |
| Cutbank watcher | a standing person with a pole | grey cloak, a pale face |
| Soot gremlin | small, round, blunt claws | soot-black, a grey nose |
| Chalk-ring latchcoil | a low coiled viper | green coils, a broken chalk-white ring |
| Sun-striker | a big flat lizard, top-down | yellow bands on a dark back |
| Tatterjay | a perched bird, broad bill | blue-green, pale breast, an uneven tail fan |

Test: `FlatPixelSharedBodiesTests` (slice 2 rows).

## Slice 3 — ten people with no art (bucket C)

The working people of the tier-1/2 places, drawn in the slice-1 people's format
(`ArtSource/Live16x24/glyph-slice3`): Concord factor, Curation intake filer, junior indexer
and sorter, filer clerk, Gantry registrar, peat cutter, recension scribe, salt master and the
Tent-Right host. Each carries its trade: a ledger, a salt sack, a peat spade, a cloth.

## Slice 4 — bucket-A props and the Rooted

Eleven 16×16 bodies existed under `Sprites/Environment` with no admission: the Rooted, the
hollow stump, the ice stalactite, the three library walls, the pricklebrow nest, the rope
anchor, the sealed archive shelf and door, and the shrine. The props join `SharedProps`, a
separate admission rule: placed, not carried and not a creature (the cactus rule), instead
of the living-creature rule.

## Items — one generated atlas (`ArtSource/Live16x24/item-icons`)

**Scope.** Every item no flat catalog already draws: blueprints derived from `Item`, plus
takeable non-creature objects that do not derive from it (keys, corpses, cups, writs, the
oath cord). 251 icons in 48 families: seeds, books, flasks, codices, weapons by kind,
armour by slot, keys, corpses, cups and others.

**Format.** 16×16 frames, centred in the cell like the other small portable art. A/a/H take
the item's own colour code, mapped to a muted palette that sits beside the project's
sprites. Wood, metal, glass, paper, cord and flame keep fixed colours.

**Admission (the item branch).** Lying where it fell: takeable, not carried or worn,
canonical glyph and colour, untinted.

**In a pack or worn.** The readers show the same icon: the inventory ledger's selected-item
portrait and examine (`FlatPixelWildFaunaCatalog.TryResolvePortableItem`, routed from
`FlatPixelGreyboxCatalog.TryGetPresentationSubjectSprite`).
- Before this, a carried item showed its glyph even when its ground icon existed.
- Disclosure stays the caller's.
- An item lying in a zone never takes this route. Its reader follows its cell, so the
  floor and the portrait cannot disagree, even in opening zones.

Test: `FlatPixelItemIconsTests` (ground; pack; worn; recoloured carried; floor never via
the pack route).

## Props — one generated atlas, with states (`ArtSource/Live16x24/prop-icons`)

**Scope.** Every placed, non-takeable, non-creature prop that no flat catalog already
draws: 117 props in 189 frames.
- Takeable things belong to the item atlas, so no blueprint is in both.
- **TheRoot is deliberately left to its glyph (bucket D).** It is the god-tree's root face, a
  unique landmark that deserves composed site art, not a template.

**Format.** 16×24 frames, the game's cell.
- Small props sit on the cell floor (16-row templates, bottom-aligned).
- Walls, plank walls, the chalked wall, iron rails and barred gates fill the full height.
  Their courses repeat every 8 rows, so stacked and side-by-side cells tile.

**States — the picture never lies.**
- **Crops:** one frame per growth stage, read exactly as `CropPart.GlyphForStage` and
  `ColorForStage` read the stage CSVs.
  - Stage 0: a sown mound.
  - Stage 1: a sprout.
  - Stage 2: the mature plant in its yield's colour. Its form follows the mature glyph:
    `T` tall with fruit, `%` bulbs at the soil, `#` fibre stalks, `"` a leaf rosette.
- **Doors and gates:** a closed frame and an open frame, keyed by `DoorPart.IsClosed`.

**Briefs drawn from each prop's own description.**

| Prop | Description says | Drawn as |
|---|---|---|
| Curation gates | salt-white bars interrupt the masonry | posts in the glyph colour, salt-white bars with the ground showing through |
| Quarantine rail | close-set iron rails, salted heavy at the base, a fresh pale cord and inspection card | 1-pixel gaps between iron bars, a salt heap, a card on a cord |
| Field and allotment gates | timber rails between short posts, a brace, a latch under the top rail | short posts, two rails, a brace in the gap, a metal latch |
| Gantry timber wall | timber | weathered vertical boards on two battens with nail heads |
| Chalked wall (Slip) | whitewashed, names chalked low down, over and over | whitewash with dense grey scrawl in the lower half |
| Boat frame | hull upside-down on trestles, ribs bare | inverted hull with rib lines on two trestles |
| Receiving bay | a low frame with a slot for a final remark | a low open frame with a chalk label strip |
| Bone cache | bones | bone-pale, not the glyph's yellow |

**References studied.** Qud furniture, door and terrain tiles, for silhouette economy only:
- how a gate reads by its bars;
- how a wall course hides its tiling seam;
- how a crop says "ready" with one accent colour.

The project's own sprites were studied too: the Sill masonry, the shared storage chests and
the settlement keepers' furniture. No Qud pixels are copied.

**Admission (the prop branch).** All of these must hold:
- placed (not takeable, carried or worn) and not a creature, the player or an item;
- canonical for its current state: glyph, colour and background equal the index row for
  `(blueprint, state)`;
- untinted, one cell, and not wrecked (a destructible at 0 HP or gone keeps its glyph).

Counter-checks: a recoloured prop, a wall without its canonical background, a prop made
takeable, a wrecked door, a crop whose glyph disagrees with its stage, a stage outside the
index, and an open glyph on a closed gate each keep their glyph.

Test: `FlatPixelPropIconsTests`.

## Independent art review (Prompt 6)

**Round 1, 2026-10-31.** A fresh reviewer judged the previews blind, with only the art
direction and the approved player and keeper as the style anchor.

Copy check: **no copies.** 36 key frames were compared, in both mirror orientations, against
every isolated tile in the shelf's object and terrain atlases. Matches above 0.85 came only from
near-rectangles. The anvil, headstone and shelf tiles differ in construction.

Verdicts:
- **FAIL:** the mature-crop template (it read as a paw print), bear trap, tatterjay corpse,
  graveyard, gallery gate (it read as a ladder), whitewashed wall, floor runes and wrapped bodies.
- **FIX:** most furniture.
- **PASS:** anvil, closed field gate, lantern, coals, cup, candle, bone cache, quartz, and
  the hearthbulb, knotflax and seamleaf crops.

Problems across the whole set, all redrawn:

| Finding | Redraw |
|---|---|
| Near-black outlines made up 30–71% of each prop; the approved actors have none | One outer outline in the opening chest's warm #342e25. Interior lines take the neighbouring material's own dark, automatically (`soften` in item-icons). Tiling walls treat the frame border as interior |
| The player's ochre was reused on props | `&W` muted to straw and `&Y` to toned whites. Gates use salt-white material, the notice board a brown board with paper, the case glass |
| Items floated 4–6 px above the floor | Items are 16×24 frames, bottom-aligned on row 22 where the player stands |
| Identical frames in runs | Per-cell variants (three plank walls, two rails: the inspection card on one cell in two). Name-hashed variants for documents, keys and codices |

Per-family redraws:
- wrapped bodies as lying figures in linen, peat or salt-grey;
- the jaw trap as two toothed jaws, a pan and a chain;
- arched grey headstones;
- grey menhirs with 5×7 letters;
- six mature crop forms on one soil bed that stays fixed across stages, sprouts drawn as
  small versions of the mature leaf: tiers, vine, mat, bells, grass, shrub;
- planks 5–6 px wide with one dark batten;
- a toned, staggered whitewash;
- irregular ice;
- stone coffer and slab;
- shelves: a full-height archive shelf with spines, and a toppled one;
- a rack of blades;
- a woven basket;
- runes as line glyphs;
- a cylinder capacitor;
- legs-up corpses, including a bird form;
- boots and seed pouches.

The tests read variants and frame heights from the indexes. A plank run is pinned to vary,
and a cell never to flicker between variants.

**Round 2.** The same reviewer was resumed on the redraw.
- **Settled:** near-black pixels fell from 30–71% to 0% in every asset. The yellow clash is
  gone and items rest on row 22.
- **Now pass:** the crops (six forms on one soil bed, sprouts in kind), bookshelf, rack,
  coffer, slab, notice board, case, graveyard, ice, whitewash, rail, gate and tatterjay.

It found four problems the redraw introduced, all fixed:
- the open field gate's right post shrank; the open frame now keeps the closed frame's posts
  pixel for pixel;
- the compass stones read as graves; they are now tapered menhirs with an off-centre peak,
  a notched shoulder and moss at the foot;
- the Curation barrier outshone the player; salt and rail highlights are toned below it;
- the sealed body vanished on forest floor; its peat wrap was lifted.

Remaining FIXes, also done:
- the boat is an upturned hull with a flat keel, a tapering bow and X-trestles;
- the basket is straw;
- the bodies have a 4×3 head with a neck notch;
- the furrowstalker's legs are two bent pairs;
- documents take book, sheaf, scroll and clasp silhouettes in turn, and keys three shapes;
- the plank speckle is removed.

A third round was not run.

## Rank 2 slice — berry bushes, hives and river shrines beyond the opening

- The Spread rule and the settlement rule admit `BerryBush` exactly as the opening does,
  held to its canonical `;` `&r` body.
- The hive and river-shrine catalogs are consulted outside the opening on live, disclosed
  cells, in both the world and reader routes.

Pin revisions, deliberate:
- `FlatPixelKitchenOrchardTests` and `FlatPixelRiverShrineTests` used to require the glyph
  outside the opening. They now require the same approved image, with memory still
  disclosing nothing.
- `FlatPixelGlobalPresentationTests`: Battleaxe and ChainMail now wear their own generated
  icons, never a borrowed approved identity.
- `FlatPixelSillEnvelopeTests`: the Apothecary leaves the pending list and wears its own
  shared body.

Evidence:
- **GREEN:** 311/311 across the slice, the Spread, settlement, north-vegetation, orchard and
  shrine suites, and the bestiary.
- **RED:** with the slice reverted, all three positive cases fail.

## Ledger

| Slice | Owners admitted | Census glyph owners after | Evidence |
|---|---|---|---|
| (P5 B9) upper bestiary | 15 creature kinds | — | `FlatPixelUpperBestiaryTests` |
| 1 — bucket-A people and creatures | 47 | | `FlatPixelSharedBodiesTests` |
| 2 — twelve creatures (new art) | 12 | | `FlatPixelSharedBodiesTests` |
| 3 — ten people (new art) | 10 | | `FlatPixelSharedBodiesTests` |
| 4 — bucket-A props | 11 | | `FlatPixelSharedBodiesTests` |
| Items atlas | 251 | | `FlatPixelItemIconsTests` |
| Props atlas | 117 props / 189 frames | | `FlatPixelPropIconsTests` |

**Static census, before → after** (`ArtSource/Live16x24/glyph-census/static_census.py`; a
blueprint counts as named when a flat catalog or a generated index names it):

| Kind | Before (not named) | After (not named) |
|---|---|---|
| Creatures | 35 of 79 | 0 of 81 |
| NPCs | 34 of 50 | 0 of 50 |
| Items | 237 of 286 | 0 of 304 (takeable non-Item objects now counted as items) |
| Props | 149 of 227 | 1 of 203 (TheRoot, bucket D) |

"Named" is not "drawn": each catalog admits only canonical owners, and opening zones keep
their own routes.

## Runtime census (Prompt 1), 2026-10-31, after slices 1–4, items and props

`GlyphCensusBenchTests` (explicit) generated seed 64 and resolved every visible non-terrain
owner through the real `FlatPixelGreyboxCatalog`:
- **709 zones:** all surface zones; depths 1–3 under every sinkhole and every fourth column.
- **Blueprint pass:** every blueprint placed alone in quiet Spread country.

| | Blueprints still drawing as glyphs | Owners |
|---|---|---|
| World (seen in seed 64) | 58 | 26,226 |
| Blueprint pass (not seen in seed 64) | 50 | 50 |

The slices above (the 47+12+10 bodies, 251 items and 117 props) are absent from the glyph
list, with two exceptions, both by design:
- **Wickrush crops:** 2 of 4 stand in the opening zone `Overworld.11.9.0`, which keeps its
  own route. That route has no art for them.
- **The glowmaw:** it lurks invisible until it drops.

What remains is owned by older catalogs whose admission stops at a zone boundary. The
census's per-place breakdown shows where:

### Triage (Prompt 2), ranked by what the player sees

| # | Gap | Owners (seed 64) | Where | Bucket |
|---|---|---|---|---|
| 1 | Morrowfast and the Felling-Site in flat mode | 118 scene props + 8 Morrowfast people | Grovelands 3,6 and Stump 3,5, surface. Their painted scene art (1536×1024, 32 px per cell) is a legacy presenter that flat mode hides, so every scene prop is a `*`/`#` glyph | C (whole-place art) |
| 2 | Surface props inside sites and settlements | berry bush 153, trees 567, rocks 407, plain walls 375+143, beehive 30, signpost 29, fallen beam 30, compost row 28, river shrine 19, barrel 18, oven 16, millstone 13, village door 11, crops 22, mendleaf 85 | Spread, Grovelands, Sodden, Stump, Beating surface | A (most have sprites: berry bush, beehive, signpost, oven, tree, well, chest) / B |
| 3 | Sinkhole inner floors and lairs | sandstone walls 14,025, limestone 5,881, vine 2,102, plain wall ~700, descent ledges 991, veins ~240, stalagmites 98 | depths 1–3 under columns the ordinary cave catalogs exclude (a POI or a saved lair column) | B (reuse the cave bands' approved walls) |
| 4 | Portable identities outside their home | harvest products, seeds, corpses, Cindercaller, Reedback | the blueprint pass: approved art whose catalog is local-only | B (make portable) |
| 5 | Villager variants | 13 of 61 | Spread settlements | A |

Ranks 2 and 3 are the special-sites checkpoint's own queue: sinkhole inner floors, lairs,
beehives and berry bushes. Rank 1 is the largest single visible gap.

### Slice brief: berry bushes and hives beyond the opening (rank 2, bucket B) — implemented

The river shrine joined the slice: the same pattern, 19 glyphs. Implementation and evidence
are in "Rank 2 slice" above. The plan's RED-first order was compressed: the routing landed
before the RED run, because the verification clone was busy. RED was shown afterwards by
reverting the slice.

**Verified premises.**
- **Berry bush.** The approved art is `north-vegetation` frame 2.
  - The opening admits it only with an unspent `WildBerries` harvest in range
    (`FlatPixelOpeningArtCatalog.VegetationFrameOf`).
  - A real harvest removes the bush. Tests deliberately pin that no spent frame is
    invented (`FlatPixelNorthVegetationTests`).
- **Hive.** The approved art is `orchard-hive` (16×24). `FlatPixelOrchardCatalog` is
  semantic-only, but the greybox consults it only inside the opening (`local`).
- **Where.** The Spread environment catalog (ordinary Spread, sites) and the settlement
  catalog (its Spread rule) each draw only Tree, Bush, Hedge and flowers.

**Plan.**
1. Add BerryBush to the Spread rule, admitted exactly as the opening admits it.
2. Consult the orchard catalog outside the opening, through the same world and reader
   routes.
3. Tests: RED first, the opening's mutation list as counter-checks, and the census
   count falling by the admitted owners.

### Rank 1 plan: the painted places in flat 16×24 (Morrowfast and the Felling-Site)

**Verified premises (2026-10-31).**
- **Scene art.** Both places carry authored scene art: a 1536×1024 painted canvas at 32 px
  per cell, drawn by a legacy presenter. Flat mode hides it (`HideLegacyForFlatMode`).
- **Flat is the default.** `WorldPresentationMode.Flat16x24 = 0`. In a normal launch both
  places therefore draw as glyphs. The painted art shows only under the `Previous`
  preference.
- **Scene props.** The runtime owners are generic `FellingSceneProp` / `MorrowfastSceneProp`
  entities rendered `*`, `#` or `+` `&y`. Their kind lives in the scene definition, keyed by
  `ComponentId`.
- **Ground.** It is TepuiStone named per cell (wall, floor, interior, ground). The Stump and
  Grovelands material catalogs exclude any column with a POI, so in flat mode both places'
  ground is glyphs too.
- **Morrowfast:**
  - 2,000 cells: 1,757 open ground, 95 interior floor, 142 solid (61 of them water) and 6
    open water;
  - 71 owners: containers 17, NPCs 6, building shells and roofs 5+5, doors 5, tables 4,
    planters 3, workstations 3, stools 3, stalls 2, creatures 2, beds 2, hearths 2,
    bookshelves 2, and gate, bowl, cistern, bridge, cart, salvage, oven, bench, workbench
    and rope at 1 each.
- **Felling-Site:** 55 layers: plant clusters (red branching 13, cyan stems 8+1, pink
  mushrooms 5, pink-red fungus 3, blue rosettes 2, pale tubes and spires 2), loose boulders
  5, petrified buttresses 3, moss-capped columns 3, foreground root/rock structures 4, a
  petrified trunk, a bank stone, and the waterfall, mist and river.

**Sub-milestones, smallest blast radius first.**
1. **Ground (bucket B).** A painted-places adapter keyed by each cell's authored role.
   `FlatPixelSiteAdmission` deliberately excludes unique places, so these need their own
   adapter.
   - **Morrowfast.** The runtime names its TepuiStone by role (`MorrowfastSceneRuntime.NameTerrain`):
     - "grove turf" → the approved Grovelands turf;
     - "flagstone floor" (interior) → the Sill flagstone floor;
     - "house wall" (solid and opaque) → the Sill masonry;
     - WaterPuddle → approved water.
   - **Felling-Site.** TepuiStone ground and solid, opaque root cells take the Stump materials'
     floor and dome. The WaterPuddle river takes approved water.
   - Each cell keeps the authored solidity and opacity it already has. Art never changes
     collision.
2. **Morrowfast furniture.** Generated 16×24 frames per owner kind, keyed by `ComponentId`
   → kind, with the prop atlas's admission. Doors get open and closed frames. Multi-cell
   shells and roofs stay structure, not props.
3. **Felling layers.** One frame per layer kind, mutable growth and loose stone included.
   Clearing removes the owner, so no cleared frame is invented.
4. **Morrowfast people.** Bodies for the eight named residents, in the slice-3 people format.

Each sub-milestone runs RED → GREEN → counter-check → review, plus a census rerun on the two
places' cells.

### Rank 1, sub-milestone 1a: Morrowfast's ground and walls (implemented)

**Route.** The settlement ground catalog now admits Morrowfast while its painted scene is
installed (`MorrowfastSceneRuntime.IsActive`). It used to exclude Morrowfast by name.

**Roles.** `MorrowfastRole` reads the runtime's own named roles and nothing else:
- the authored-terrain tag;
- the canonical `.` `&y` TepuiStone;
- collision that matches the role.

| Role | Approved art |
|---|---|
| grove turf | the Grovelands forest floor |
| flagstone floor | Sill floor frames 4–7 |
| house wall | Sill masonry, frame 8, in world, reader and memory alike |

**Pin revision.** `FlatPixelSettlementGroundTests` no longer lists Morrowfast among the
addresses that are never admitted.

**Water.** It still falls back. The settlement water rule needs plain ground under the
puddle; Morrowfast's water *is* its terrain.

**Tests** (`FlatPixelMorrowfastGroundTests`, 7/7):
- **RED first:** 0 of about 1,930 land cells drew native.
- **GREEN:** the three roles; memory keeps the masonry; nine in ten land cells draw native.
- **Counter-checks:** a renamed or untagged stone keeps its glyph; the address without its
  installed scene is not admitted.

### Rank 1, sub-milestones 2–3: the painted places' furnishings and layers (implemented)

**Keying.** Scene owners are generic entities, so the prop index keys them by component:
- `MorrowfastSceneProp` by `MorrowfastPropPart.ComponentId`; doors add `|open` or `|closed`
  from `MorrowfastDoorPart.IsOpen`;
- `FellingSceneProp` by `FellingScenePropPart.ComponentId`.

Door keys are built once per component, so the per-cell path still allocates nothing.

**Frames.** One frame per placed owner, 113 in all, drawn in each place's own palette
(the owners' glyph colour is a uniform `&y`).
- **The Felling-Site.** Colours sampled from the painted scene:
  - mauve petrified wood: trunk, buttresses, root arches and moss-capped columns;
  - red branching plants, cyan stems, pink caps, cream stalk fungus and blue rosettes;
  - pale spires and tubes; boulders, rock clusters and the bank stone;
  - the waterfall, its mist and the blackwater.
- **Morrowfast.**
  - Containers by name: barrel, bucket, basket, crate, chest.
  - Desks and tables, beds, stools, a bench, bookshelves, an offering bowl, the footbridge,
    a handcart, the cistern.
  - The oath arch, the full height of the cell.
  - Stoves, a masonry hearth, the bread oven, planters and an herb pot.
  - Rope coils, an awning stall, a workbench, a chopping block.
  - Timber doors, closed and open.

**Left out.** Shells and roofs are structure, not furniture, and stay as they are. The
town's people and animals come later (sub-milestone 4).

**Tests** (`FlatPixelPropIconsTests`):
- every Morrowfast furnishing that is the top owner of its cell draws natively, and an
  owner covered by another draws that other;
- a door shows open and closed, and a closed glyph on an open door keeps its glyph;
- every Felling layer draws natively, and an unknown component keeps its glyph;
- the index pins each scene row to a real component of its place's definition, with that
  component's glyph.

### Rank 1, sub-milestone 4: Morrowfast's people (implemented)

Eight residents, drawn in the slice-3 people format from their own descriptions
(`ArtSource/Live16x24/morrowfast-people`):

| Resident | Who | Drawn with |
|---|---|---|
| Nemm Hushbell | west watchkeeper | spear, cloth-wound bell cord |
| Hesta Brack | east watchkeeper | spear, return slate |
| Vennit Rusk | Recension clerk | teal-cyan robe, open ledger |
| Pell Drowse | keeper of the Dry Hem | damp boots carried |
| Orrit Coilstitch | rope mender | rope coil |
| Sella Kettle | the Second Bowl | apron, kettle |
| Farra Sprig | Bower-taught gardener | leaf-green smock, sprig |
| Edden Brack | traveller home from the north | pack, walking stick |

**Blueprints.** Their blueprints are built at runtime (`MorrowfastContent.CreateResident`).
The shared-bodies rows admit them by those names and glyph colours.

**Visual identity.** Farra and Edden carry an authored `VisualID`, which is their canon
(`AuthoredVisual`); any other VisualID still refuses.

**Tests** (`FlatPixelMorrowfastGroundTests.EveryResidentWearsTheirOwnBody`): every
uncovered resident wears their own body. Counter-check: Farra with someone else's
VisualID keeps the `@`.

### Following slice: opening identities beyond the opening (rank 2b, bucket B)

Approved opening and East identities whose catalogs admit them only in their home zones (the
glyph owners in seed 64):

| Identity | Glyphs | Home |
|---|---|---|
| glow-quartz vein | 151 | `FlatPixelOpeningArtCatalog` |
| mendleaf | 85 | `FlatPixelOpeningArtCatalog` |
| fallen beam | 30 | `FlatPixelOpeningArtCatalog` |
| signpost | 29 | `FlatPixelOpeningArtCatalog` — **done in ordinary Spread country, 2026-10-31** (below) |
| wooden barrel | 18 | `FlatPixelOpeningArtCatalog` |
| oven | 16 | `FlatPixelEastKitchenCatalog` |
| millstone | 13 | `FlatPixelEastKitchenCatalog` |

Each needs a semantic accessor that keeps its home catalog's exact rule, as the orchard and
river-shrine catalogs already do. The world and reader routes then consult it outside the
home zone.

**Signpost, ordinary Spread (2026-10-31).** Boundary-stone waysites (`Docs/CHUNK-ACTIVITY.md`)
put signposts into wild Spread chunks, where they drew as `I` glyphs.
`FlatPixelSpreadEnvironmentCatalog.IsEligibleSignpost` admits the canonical body to
opening-anchors frame 8, on the real grass:
- the solid `I` `&y` post;
- whose only meaning is its carved directions (`RegionalSignpostPart`);
- with no material, structure or other state.

The Beating already borrows the same frame, so a signpost reads the same in both countries.
Tests: `FlatPixelSpreadSignpostTests`, with 9 mutation counter-checks and a consistency pin.
Signposts elsewhere (Sodden, Grovelands, Stump) are not admitted yet.

**Not yet verified:** live captures (Prompt 8).

The blind art review (Prompt 6) is in progress.
