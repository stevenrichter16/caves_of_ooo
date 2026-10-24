# Live opening — native 16×24 parts brief

Prepared 2026-10-22/23 for the first actual-game greybox reinterpretation. This is an owner/state inventory and art-production proposal, **not finished art or a record of a completed Play capture**. Read [the live direction](../../Docs/LIVE-16X24-ART-DIRECTION.md) first. That document owns milestone status and verification; this file owns the practical parts mapping.

## Current pass and evidence

- **Pass:** playable-greybox integration → actual Game capture, then imagegen reinterpretation. The parent task is conducting Play/capture. Add its accepted capture and exact concept prompt below before producing detailed opening art.
- **Real target:** seed 64, ordinary fresh opening, `Overworld.11.10.0`, player starts at `(40,12)`. Native coordinates have Y increasing downward. The whole zone is 80×25 cells; the camera shows a subset.
- **Coverage evidence:** `FlatPixelGreyboxCatalogTests.RealSeed64OpeningHasBoundedGreyboxCoverage_WithoutChangingSimulation`, recorded in [flat-mode-green.xml](../../Docs/Verification/Live16x24/flat-mode-green.xml), inventories 28 top-owner blueprint types. The fixture enables exploration explicitly, exposes cells for enumeration, and checks the generated graph was not changed. This is a whole-zone inventory, not proof those objects are simultaneously visible to a fresh player. Bootstrap adds the player separately.
- **Current output:** one shared 128×72 procedural atlas, 17 cached native 16×24 rough shapes. One generic actor, vegetation and prop shape currently covers several different identities. This is sufficient for blocking and ownership checks, not visual acceptance.
- **Approved inputs inspected:** all five PNGs in the next section, at their actual native dimensions. Metadata/palette inspection was read-only. No approved image was changed.
- **Tools for this brief:** repository source/XML reads, native PNG inspection with `view_image`, read-only Pillow metadata/palette extraction. No Unity calls, image generation or production asset edits were performed for this brief.

Continuation slots, to be filled by the agent conducting each pass:

| Record | Current value |
| --- | --- |
| Actual Game greybox capture, mode and tested route | Pending parent Play session; link the 1280×800 capture and action record here. |
| Imagegen inputs, exact prompt, output and critique | [Prepared capture-first prompt](concepts/opening-reinterpretation-prompt.md); actual inputs/output pending accepted capture. Keep original output separate from native assets. |
| Selected reference IDs | First composition pass: R01; crop study: R04. Use at most two relevant references per iteration. |
| Extracted concept parts | Use the owner table below; amend it when the accepted concept suggests a better silhouette. |
| Editable native sources and imported assets | Pending. Preserve source PNGs and author derivatives in a separate directory. |
| Actual integrated capture and observed motion | Pending. A generated scene cannot fill this slot. |
| Unresolved issue / next action | Capture and critique real movement, interaction access, fog and collision before final art. |

## Approved style anchors

These are style/identity references, not permission to duplicate their owners in the opening. Every image is 16×24 with only alpha 0/255. Bounding boxes below use top-left image coordinates and an exclusive right/bottom edge.

| Asset | Observed constraints | Read-only measurements |
| --- | --- | --- |
| [Seed Keeper 3](../../Assets/Art/SeedKeeper3/seed-keeper-3.png) | Broad pointed yellow hat, dark face break, narrow green body. The hat/robe separation is readable without texture. Keep this approved version rather than silently substituting the earlier straw-hat concept. | Bounds `(1,0)-(15,24)`; 6 opaque colors. Green masses `#7AA153`, `#53683E`, `#638144`, `#97C768`; hat `#F9C706`; small white accent. |
| [Cook](../../Assets/Art/Cook16x24/cook-16x24.png) | Pale cap/apron against a rust body; compact silhouette with strong cream/rust separation. No gradients or dimensional relighting. | Bounds `(3,2)-(13,23)`; 8 colors. Cream `#E4D0A0`, rust `#9B442B`, dark brown `#342E25`, warm skin `#DFA06A`. |
| [Tatterjay](../../Assets/Art/Tatterjay16x24/tatterjay.png) | Slender diagonal bird silhouette, teal body and pale accents. Distinguish beak/tail with negative space, not additional shading. | Bounds `(1,4)-(15,21)`; 6 colors. Teal `#4A9991`/`#326964`, cream `#E4D0A0`, brown `#5F4830`/`#342E25`. |
| [Ridge / reedback grazer](../../Assets/Art/ReedbackGrazer16x24/03-ridge.png) | Wide, low quadruped silhouette with a warm brown back and separated feet. It must not be stretched upright to fill its frame. | Bounds `(0,9)-(16,21)`; 6 colors. Ochre `#A97542`/`#C59156`, brown `#7C522F`, cream/dark shared with cook and bird. |
| [Beetle jar](../../Assets/Art/BeetleJar16x24/beetle-jar.png) | Pale container edge, dark interior and olive contents; cap and vessel silhouette remain distinct. Its relatively large world icon is approved; do not shrink it solely because it is an item. | Bounds `(2,2)-(14,22)`; 10 colors. Interior `#292F28`, olive `#79805B`/`#4B5940`, edge `#C3D1BE`, cap `#B48B54`. |

The shared rule is **a few deliberate color masses, useful negative space and distinct silhouettes**. There is not one identical existing palette: Seed Keeper 3 is brighter than the other four. Preserve approved bytes and identity; make the surroundings quiet enough to accommodate that brightness. Do not recolor every approved sprite to force uniformity.

Nella is at `Overworld.11.8.0`; Orven is at `Overworld.12.11.0`. Their work sites belong to the connected-sites milestone. The bird, grazer and jar should likewise appear only where their real owners exist. The blank five-sprite study remains a recoverable art study, not the opening's population specification.

## Frame, footprint and palette contract

- **Default production frame:** 16×24 square pixels, RGBA with hard alpha; Point filtering, no mipmaps or lossy texture compression. Current world cell is 1×1.5 at 16 pixels per world unit. Do not stretch a 16×16 tile to pretend it is a native 16×24 asset.
- **Footprint:** every opening owner inventoried here occupies one authoritative cell. A visually connected ruin or pond uses many real cells; it is not one giant backdrop. Do not add collision, hide an adjacent walkable tile, or create an apparent doorway in a solid owner.
- **Pivots:** the current tile atlas uses a centered pivot `(8,12)`. Future dynamic actor views use the cell's bottom-center foot anchor; use a deliberate actor pivot/offset contract and verify it in the real renderer. Do not mix centered tile placement with an assumed bottom pivot. Preserve the approved frames' existing empty margins.
- **Style:** flat orthographic presentation, no perspective vanishing point, beveled cubes, cast volumetric shadows, painterly blur or subpixel edges. Small graphic dark cuts can describe a rim/overlap; they must not make the scene return to the old 3D look.
- **Ground:** retain quiet dark green `#032F23` as the default. Avoid pervasive dots, animated grass carpet and gridlines. Ground is visual breathing room and carries selection clearly.
- **Proposed environment ramp:** deep green ground; muted olive vegetation; desaturated green-grey stone; restrained brown/ochre wood and soil. Current greybox reference colors are road `#31412D`, dry soil `#45341F`, wet soil `#252319`, water `#0C4344`, stone `#809482`. These are provisional, not a mandate to tint finished sprites uniformly.
- **Hierarchy:** player/cast silhouette first; actual threats and useful interactables second; architecture/route boundaries third; incidental vegetation last. Use a small pale rim, head or tool mass for recognition, not universal glow. Living actors need brighter separation than wall interiors.
- **Semantic colors:** warm grain heads only when harvestable; cyan accents around actual water/quartz; charcoal cuts for missing/broken material. Reserve strong red/yellow screen outlines for the existing target/landing UI. Do not bake permanent hostility outlines or a yellow selection border into art.
- **Readability target:** clear at native sprite size and at the actual integer scale used in the 1280×800 Game view. A pleasing zoomed contact sheet cannot establish in-game readability.

## Three anchors for the first screenshot reinterpretation

**Pond / finite basin, northwest of spawn:** the water occupies an ellipse around `(34,5)`, within X29–38/Y2–8, with reeds on its edge. The bank path is Y9/X29–38; the basin is `(36,9)` and the waterskin `(37,10)`. Give the pond a broad quiet dark-teal shape, broken pale reeds, and a clearly distinct stone basin mouth. The water is actual ground coating; only the basin owns three drawable drams. Do not suggest the entire pond is a refill station.

**Working garden / repair well, northeast and east:** the well is `(41,8)`, cooking fire `(42,8)`, and road X41–51/Y9. Cut field strips occupy X44/46/48/50, Y10–14, with three ripe rows. The shelter wall runs X43–50/Y6 and has a damaged wicket `(46,6)`; the separate ripe Drawgourd bed is `(46,4)`. Make orderly, sparse cultivated marks contrast with the pond's irregular fringe. Keep the well's split lining visible and give the fire a low working shape rather than inventing an oven/shelter owner.

**Ruin / cellar, southwest and west:** dark stone segments at X20–31/Y11–22 form a recognizable broken enclosure, with a charcoal/green-grey interior and a sparse luminous seam on the eastern side. The actual stairs are `(28,19)`, the haulable beam `(26,19)`, and low vaultable crossing `(28,17)`. Distinguish low, passable broken columns from solid wall faces. Keep the main Y16 corridor and the longer way around the beam legible. This anchor can carry the strongest architectural silhouette without becoming an opaque painted building.

Preserve the X40/Y6–18 central approach, all of row Y16, west approach Y12/X<20, and the outer two-column travel bands. The first reinterpretation changes art, not generation. If a composition genuinely needs a new wall or moved service, propose that as a separate native-layout change with path/collision tests.

## Owner-to-part mapping

`reference-glade-` is the literal VisualID prefix below. **Blueprint route** means the glade gives that owner no explicit VisualID override; the blueprint name is the mapping key. There are no authored `SpatialFootprintPart` expansions in this opening. Priority reflects how much a recognizable asset improves the first playable image, not an asset quota.

| Priority / real owner | Actual VisualID or discriminator | Rough result now | Production parts and truthful states |
| --- | --- | --- | --- |
| 1 — `Grass` | `reference-glade-ground`; prepared bed has `GleanersFieldwork.Role=soil` plus `CultivatedSoilPart` | Solid dark ground; wet coating or soil changes its color | Quiet base tile; true water/soil treatment. Prepared bed persists after crop removal. No crop graphic glued to its ground. |
| 1 — pond surface | Current visible `Zone.TileState` water coating over native ground; not a separate pond blueprint | Dark-teal ground rectangle per coated cell | Interior, bank and occasional ripple parts driven by actual neighboring coating, not hardcoded pond coordinates. No water highlight on drained/removed coating. |
| 1 — `Wall` | `reference-glade-dark-ruin`, `reference-glade-low-wall`, `reference-glade-lit-wall`; saved `ReferenceGladeQuarterTurns` where assigned | Striped solid wall block | Dark ruin and lighter low-wall material families; straight, end, corner/junction masks derived from actual adjacent compatible owners. Lit seam is a local variant, not a whole-map glow. Destruction removes the wall and may create `Rubble`; never retain an intact silhouette. |
| 1 — crossing `Wall` | `GleanersDistrict.Role=crossing`, `(28,17)`, `reference-glade-low-wall`; **Wall tag deliberately removed** | Generic prop, not the normal Wall category | A low broken stone course with visible landing beyond. Still a solid obstacle, but no full-height wall face or opaque canopy. This role must override broad Wall-blueprint art assumptions. |
| 1 — `RoadStone` | Blueprint route | Muted road-colored ground | Sparse flat slabs/edge fragments; three or four coherent variants, not a cobbled carpet. Ground cannot imply a block. Give the ruin approach and bank the same material grammar. |
| 1 — `Reeds` | `reference-glade-pale-reeds` | Generic three-stem vegetation | Pale bent reed cluster with open gaps, short and tall silhouettes within a cell. Derive edge treatment from actual owners; no extra blockers or looping identical sway in every patch. |
| 1 — `Bush` | `reference-glade-green-grass` | Same generic vegetation | Low olive sprig/leaf cluster, clearly shorter and darker than reeds. This VisualID is a small glade vegetation treatment, not authorization for a full-height opaque bush. |
| 1 — `Player` | Bootstrap owner, Player tag; no glade placement | Gold generic humanoid | Original traveler's readable head/body/feet; visible facing and movement/action frames. Use the approved cast's restrained masses, but do not impersonate Nella or Orven. Derive equipment cues only from actual equipment or keep the base equipment-neutral. |
| 1 — `Warden` | Blueprint route; initial `(35,15)` | Same pale generic humanoid as every non-player creature | Upright protective silhouette, clear boot/shoulder masses. Default loadout includes long sword/leather armor/boots; a static sword must not remain after disarm/removal. Either bind equipment overlays or keep base art independent of the held weapon. |
| 1 — `Villager` | Blueprint route; initial `(38,21)` | Generic humanoid | Distinct civilian silhouette, subdued olive/cream work clothing, no warden's martial profile. Trading status is dialogue/game state, not a permanently floating merchant icon. |
| 1 — `PetDog` | Blueprint route; initial `(33,23)` | Generic humanoid — especially misleading placeholder | Low canine body, visible muzzle and tail; purposeful sniff/pause idle, legible step frames. Never use the grazer sprite as a substitute. Follow current owner position/fog. |
| 1 — `MarlbackScrabbler` | Blueprint route; initial `(42,19)` and `(22,5)` | Generic humanoid | Short-faced bank burrower; broad overlapping shale back plates, grasping foreclaws. Use an unmistakable hunched original silhouette. No borrowed snapjaw identity. A tool may vary with real loadout, so avoid an unconditionally baked blade. |
| 1 — `MarlbackGleaner` | Blueprint route; initial `(44,20)` | Same generic humanoid | Related marlback silhouette with a different plated back/foraging posture and restrained rust accent. Lore places cutting tools among the plates, but combat-held weapon depiction still follows actual loadout. Distinguish from scrabbler at native size without relying only on red tint. |
| 1 — `RepairLinedWell` | Blueprint route; `GleanersDistrict.Role=well`, `(41,8)` | Cracked prop while unrepaired; generic prop afterward | Broad circular stone rim, visible fired-clay interior: split lining versus sealed lining from `RepairablePart.Repaired`. Service readiness comes from `WellPart`/repair rules; no flame or magical upgrade glow. |
| 1 — `SpreadDrawPoint` | Blueprint route; `(36,9)` | Filled rectangle if `LiquidPoolPart.Volume>0`; rim-only basin otherwise | Shallow bank basin distinct from well. Filled and empty bowl interiors. **Empty supply can still have wet ground:** pool projection leaves a permanent water coating; do not erase that coating visually merely because Volume reaches zero. |
| 1 — `GleanersBuckledWicket` | Blueprint route; `GleanersFieldwork.Role=wicket`, `(46,6)` | Cracked prop; generic prop after repair | Buckled rail, repaired closed rail, repaired open gate; use `RepairablePart` and `DoorPart.IsClosed` independently. Repair does not automatically mean open. Destroyed owner leaves a real gap; do not retain decorative rails. |
| 1 — `StairsDown` | Blueprint route; `GleanersDistrict.Role=stairs`, `(28,19)` | Generic prop | Three descending dark cuts with a pale worn lip. Reads as a walkable cellar entrance, never a chest or solid black monolith. Preserve the actual linked endpoint; an ornamental arch must not suggest a second entrance. |
| 1 — `FallenBeam` | Blueprint route; initial `(26,19)` | Generic prop | Low charred timber with graspable end and clear direction. One-cell solid/haulable owner; no apparent three-cell obstruction. Moving it moves all its art. It is not a carryable/throwable loose stick. |
| 2 — `GlowQuartzVein` | `reference-glade-lit-wall`; `(31,10)` and `(31,22)` | Generic prop, currently no distinct quartz | Stone body with a few green-cyan crystal facets. Native builder gives it a real LightSource. Harvest consumes the vein; yield is 1–2 `GlowQuartz`, not an inexhaustible lamp. Do not leave a glowing mineral painting at the vacated location. |
| 2 — `BrokenColumn` | `reference-glade-dark-ruin` | Two low rubble rectangles | Broad low fragment with broken top, visibly smaller than standing walls. Deliberately passable; no full-height opaque column. |
| 2 — `Rubble` | `reference-glade-gravel` for glade dressing; destroyed-wall rubble can have no override | Two rubble rectangles | Low chips with substantial background holes. Two/three sparse arrangements; no confusing creature eyes or bright item glints. Preserve actual debris replacement after wall destruction. |
| 2 — `CropRow` | Blueprint route | Three short stubble stalks over dry soil | Cut strip in tilled soil. It has no ready harvest and no independent growth clock; do not animate it into ripe grain. |
| 2 — `RipeCropRow` | Blueprint route; `(44,10)`, `(46,12)`, `(50,14)` | Generic ripe heads; stubble when harvested | Emberwheat heads above the same cut-row foundation; read `FieldHarvestPart.Harvested`. A successful harvest leaves stubble on the existing owner. Separate this from `CropPart` growth art. |
| 2 — `DrawgourdCrop` | Blueprint route; `GleanersFieldwork.Role=garden`, `(46,4)` | Seed dots / generic sprout / generic ripe heads | Seed marks, two-leaf sprout, then a narrow-necked vessel-shaped gourd. `CropPart` stages 0/1/2 and MoistureTicks choose state; stage 2 is valid because HarvestAtMaturity is true. Harvest removes the plant and gives shell plus seed; the prepared bed remains. Soil dampness is a separate layer. |
| 2 — `Campfire` | Blueprint route; `(42,8)`; `FiniteCooking=true` | Generic prop | Low stone ring and fuel; small ember/active-flame derivatives. Read actual Thermal/Fuel/Burning state; cooking readiness is stored heat plus fuel and is **not equivalent to currently burning**. Stop flame animation when not burning. A remaining hot ring may still be useful. Follow actual ash/replacement owners, not a permanent fire stamp. |
| 2 — `Chest` | Blueprint route; `(51,8)` | Generic prop | Compact reinforced box and unmistakable hinged-lid seam. Current container has no persistent open-lid state: do not invent one, or show magical contents through it. Its initial finite contents are HealingTonic/Torch/DriedMeat. |
| 2 — `WoodenBarrel` | Blueprint route; `(32,22)`, `(31,23)` | Generic prop | Vertical staves and two broad hoops, rounded enough to differ from chest. Destructible/fuel behavior must remove or replace the art correctly; empty content is not automatically an open/broken barrel. |
| 2 — `Signpost` | Blueprint route; several locations and district roles | Generic prop | Narrow post with one clear crossboard; warm pale edge, no unreadable tiny lettering. Pointing silhouette should not invent a travel direction different from examine text. Give the cellar note/working notice recognizable small variants only through their real role. |
| 2 — `MushroomRing` | Blueprint route; `(34,19)`, `(50,21)` | Same generic vegetation as reeds/bushes | Three low cream/olive caps around a dark center. Harvestable source is consumed for 2–4 mushrooms; no persistent unharvestable mushroom painting at the same location. |
| 2 — `Torch` | Blueprint route; `(52,7)` plus possible dropped/container items | Small loose-item rectangle | Recognizable stick/wrapped head. Use real light/effect state for flame, not an always-burning fixture. Pickup removes ground art; carried/equipped representation follows actual ownership. |
| 2 — `Waterskin` | Blueprint route; `(37,10)` | Same loose-item rectangle | Warm leather pouch, short neck and dark strap. Distinguish from rigid gourd/jar by silhouette. Ground art belongs only to the dropped object; content state must not create an invented world water source. |

All 28 generated top-owner names are covered by this table. `Player`, pond coating, and prepared-soil state are additional presentation entries, not inflated new-blueprint counts. The greybox intentionally ignores most VisualID distinctions; the next resolver needs those distinctions to select native final art.

## First extraction batches

1. **Landform and boundaries:** dark ground, water interior/edge, bank stone, road fragments, pale reeds, short olive vegetation; dark ruin wall/ends/corners, low wall, low crossing, low rubble and column. R01 supplies spacing/hierarchy. Keep silhouettes large enough that the three anchors are recognizable before detailed props.
2. **Complete visible cast:** player, warden, villager, dog, scrabbler, gleaner. Review each alone at 16×24 and together on the actual dark ground. Create action derivatives only after base silhouettes read correctly.
3. **Functional anchors:** cracked/sealed well, filled/empty basin, buckled/repaired open/closed wicket, descending stairs, movable beam, quartz seam and cooking ring. Each part has a state owner in the table; none is a detached scenery stamp.
4. **Cultivation and small things:** stubble/ripe field strips, Drawgourd seed/sprout/ripe frames, dry/wet prepared soil, storage/signs/mushrooms/torch/waterskin. R04 helps separate stalk silhouettes, but a gourd is not a generic seedhead. Use R03 at Nella's later real storage site; do not populate this opening with invented sorting tables or jars.
5. **Interaction-generated objects:** `Emberwheat`, `DrawgourdShell`, `DrawgourdSeed`, `GlowQuartz`, `Mushroom`, `HealingTonic`, `DriedMeat`, actual dropped equipment and `MarlbackCorpse` when created. These may enter the visible graph through play although absent from the initial top-owner inventory. Add exact blueprint/native-state mappings as each route is exercised. Retain an explicit readable fallback for unreviewed foreign objects; do not hide them or claim coverage by recoloring one generic blob.

Do not require every theoretically possible death/loot combination before the first art integration. Finish the actual capture's visible owners, then exercise the named routes and cover their new objects. Preserve the rest with a truthful fallback and a recorded coverage gap.

## Motion and state priorities

First motion work should explain a player action: directional steps with stable feet, melee anticipation/contact/recovery, hit response, parry/guard distinction, and owner removal/death. These must use actual hooks and duration, not consume game turns or shift collision. Start/finish poses remain in the same cell contract and cannot slide into adjacent targets.

Then add sparse character idle behavior, occasional reed movement and subdued water motion. Use separate animation derivatives of the approved PNGs; do not pad all sheets with identical copies and call them complete. The current actor catalog expects animation sheets, so explicit static-frame admission or a documented compatible resolver is required before reusing the approved single PNGs. Do not accidentally interpret a 16×24 PNG as a 4×16 sheet.

Status/effect color response is not yet art-complete in the rough catalog. Preserve target outlines, damage/status feedback and visibility while introducing final colors. A constantly pulsing full scene is less readable than a short, specific event cue. No animation should expose a hidden actor, remote crop stage or finite supply state through fog.

## Review of the prepared imagegen prompt

Use [the parent's prepared prompt](concepts/opening-reinterpretation-prompt.md) as the single execution prompt. Its quiet ground, three anchors, native-pixel direction, preserved fog and ban on extra service props match this inventory. Use the accepted actual Unity greybox capture as image input 1, with unchanged approved art as style references. Save exact inputs, expanded prompt and output.

Three useful refinements for the actual input: if an anchor is outside the visible capture, do not force it into frame; keep the low crossing visibly lower than standing walls and the movable beam inside its one-cell footprint; distinguish the shallow finite basin from the cracked well, and grain heads from vessel-shaped Drawgourd. These are shape/readability changes supported by existing owners, not new props.

Judge the concept against the actual screenshot. Capture a second real route view for an unseen anchor. Reject generated changes to room openings, unexplored areas or object count. UI redesign is outside this pass.

## Verification and acceptance before calling an asset group integrated

- Link the actual capture and tested actions, exact concept prompt/output, native sources, imported assets and final Game capture. Keep concepts visibly labeled.
- Verify base PNG dimensions, alpha, PPU/filtering and pivots. Compare approved source hashes before/after. Inspect actual integer-scale pixels, not just source files.
- Walk the pond path, garden approach and main ruin corridor. Move/target on either side of the low crossing; move the beam and verify its old cell loses all related art. Cursor/cell outlines remain aligned.
- Draw the basin empty and keep the distinction between empty basin and wet ground. Harvest ripe rows, harvest/replant/water the gourd, and inspect the remaining soil/stubble. No state is inferred just from blueprint name.
- Repair the well and wicket; open/close the gate separately. Destroy an ordinary wall and verify rubble follows the replacement owner. Harvest quartz/mushrooms and confirm their source art disappears.
- Move the cast, fight a marlback, inspect actual corpse/drop results, and pick up a loose item. No duplicated actor, permanent corpse or ghost loot painting.
- Check unexplored/remembered/visible transitions. Current remembered terrain deliberately excludes live actors, crops, liquid supplies and repairable state; do not introduce a hidden-state preview to make the scene prettier.
- Open inventory/fullscreen skill screens and a popup, return to the world, travel out/back, toggle old presentation and return. Geometry, claims and previous art restore correctly.
- Record rendering measurements on the actual editor/play configuration. Shared atlas/materials and incremental claims are design constraints, not proof of Deck performance.

This brief is a source/state audit and visual production proposal. It can establish which native owners and states the art must represent. It cannot establish final appearance, animation feel or gameplay usability before the corresponding real Play evidence exists.

## Source trail

- [ReferenceGladePlan.cs](../../Assets/Scripts/Gameplay/World/Generation/ReferenceGladePlan.cs): real layout, VisualIDs, approaches and pond mask.
- [ReferenceGladeBuilder.cs](../../Assets/Scripts/Gameplay/World/Generation/Builders/ReferenceGladeBuilder.cs): owner staging, coating, lights, finite fire and crop/wicket setup.
- [GleanersDistrict.cs](../../Assets/Scripts/Gameplay/World/GleanersDistrict.cs): well, stairs, notices and the low crossing's deliberate Wall-tag removal.
- [FlatPixelGreyboxCatalog.cs](../../Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs): current rough classifications, atlas, palettes and fog filtering.
- [Objects.json](../../Assets/Resources/Content/Blueprints/Objects.json): authored identity, inherited parts, material/harvest/loadout semantics.
- [CropPart.cs](../../Assets/Scripts/Gameplay/Farming/CropPart.cs), [FieldHarvestPart.cs](../../Assets/Scripts/Gameplay/Farming/FieldHarvestPart.cs), [HarvestablePart.cs](../../Assets/Scripts/Gameplay/Items/HarvestablePart.cs): different crop/source lifecycles. Legacy EmberwheatCrop seed/sprout behavior must not be confused with standing RipeCropRow or Drawgourd.
- [CampfirePart.cs](../../Assets/Scripts/Gameplay/Settlements/CampfirePart.cs), [FuelPart.cs](../../Assets/Scripts/Gameplay/Materials/FuelPart.cs), [DoorPart.cs](../../Assets/Scripts/Gameplay/World/DoorPart.cs): heat/fuel versus burning, and repair versus opening.
- [SpreadExplorationResidents.cs](../../Assets/Scripts/Gameplay/World/Generation/Builders/SpreadExplorationResidents.cs): actual Nella/Orven sites.
- [Art reference library](../../Docs/ART-REFERENCE-LIBRARY.md): source pages, observed features and rights notes. These external images are observational references, not imported game assets.

Review: source inventory and native sprite metadata checked; no gameplay/code changes in this brief. Proposed final-art and animation states remain subject to the existing test-first integration workflow and actual screenshot review.


## East/kitchen companion — 2026-10-24

See [source contract](east-kitchen/README.md) and [living integration record](../../Docs/LIVE-16X24-EAST-KITCHEN.md).
18 exact16×24 frames in one288×24 strip: forge,oven,cot,chair,damaged/repaired
claypan,neutralinput/pickuptrays,Claspbean stages0–2,pulp,seed,FieldMeal,
CharmFlowers,CopperPipe,neutralSteamVent,MillStone. ApprovedCook is a separate
byte-identical16×24 resource. Editable Blender+source-cell records and verifier
are included. Built-in imagegen v1/v2 are concepts; no generatedpixels imported.

Actual two graphs12.10/12.11 now admit exact native roles.495 distinct native
tests passed. Ordinary keyboard repair/harvest/collect/F5F6/eastreturn/kitchen
revisit captures recorded; paid meal and full forge craft remain unproved.
Pan title/flavor contradiction is fixed and confirmed in the actual reader. Trays
remain neutral intentionally, not an inferred working/ready display. The
surrounding repetitive hedge rooms and ground/framing remain scene-level debt.
Keep approved frames stable; do not substitute additional assetcounts for
closing scene composition and the remaining authoritative service states.
