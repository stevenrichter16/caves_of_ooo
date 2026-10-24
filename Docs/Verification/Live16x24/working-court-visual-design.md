# Working-court framing — preferred visual design

2026-10-24. Read-only design based on the actual seed64/65 kitchen entry and2× captures, plus the current `SpreadExplorationResidents` footprint and terrain blueprints. No Unity actions, source edits, asset changes or imagegen call. Parent owns implementation and concept generation.

## Preferred direction: an open-front remnant court with worn stepping stones

Give Orven’s existing kitchen a small working area joined by sparse worn stepping stones and one incomplete masonry edge. Keep the approved service sprites and their existing relative arrangement. The scene should read as **a usable corner of an older stone work court**, with open grass on two sides, plantable grass joints between the stones, and the cultivated bed outside the stone pattern. The Cook and pan still carry the warm focal colors; the stone pattern and masonry organize them into a place.

The actual images show the current limitation clearly: the pan/tray/board row, oven, Cook and cot are legible but appear suspended as separate props over uniform grass. The new landscape masks already reduced distant hedge repetition and are frozen. Do not alter those masks again or add containers, lamps, signs, stools, roofs, food, tools or NPCs. This pass changes the real immediate setting, not the amount of content.

### Settled scope after source audit

**Frozen preferred scope:9 actual wall cells +15 connected sparse stepping-stone cells on empty work space.** Service offsets, crop ground, approved sprites and the new landscape masks remain unchanged. The earlier broader under-owner apron is superseded. This scope is large enough to establish a real working edge and circulation surface; three extra isolated walls alone would not be.

### Why stone, not a painted dirt path

`StoneFloor` already exists and describes exposed stone worn by repeated footsteps. It is an appropriate real terrain owner for a remnant work surface. `RoadStone` describes a packed cart road that goes somewhere; using that blueprint as a rectangular floor would make the description and picture disagree. Do not color ordinary Grass as a road or convert `IsApproach` into a material.

The parent’s source audit found full `Barren` paving unsafe for this transaction: AddEntity would clear Grass and invalidate the original-owner proof. The settled design is therefore **sparse stepping stones in unchanged grass**, not a solid paved surface. Use actual `StoneFloor` owners with exact role `court-floor`, instance display/examine naming **worn stepping stones**, no Barren, and the existing plantable Grass substrate. The art must make this truthful: broad dark/desaturated stone pieces with visible green joints, never a full opaque slab or cultivated-soil tint. Existing planting between stones remains allowed; that is intentional, not an undocumented loophole. Root owns the bounded native ground/art integration and its state/selection tests.

Keep stones low-contrast and mostly flat; they must not look like polished flagstones, a carpet or a bright quest area. No baked shadows, gradients, fake material tint or new decorative owner layer. Do not widen the glade ground admission globally. The new exact court-floor treatment may be separate from accepted sources without repainting any approved sprite.

## Concrete preferred cell layout

Coordinates below are **relative to the successful resident origin** `(0,0)` in the source’s unrotated frame. Existing service offsets stay unchanged. The existing two shelter StoneWalls move within this fresh stamp to become part of a nine-cell incomplete masonry edge; seven additional StoneWall owners complete that one edge. The preferred tight scope uses15 real StoneFloor cells only on empty working-space cells, never beneath a service or crop owner. The wall cells are real solid owners; the stepping-stone cells are passable terrain over existing grass, not scenery painted behind the props. No wall is added directly behind the pan/trays: a full walkable row separates the work row from the rear wall.

```
             x = -3 -2 -1  0  1  2  3
 y = -4          #  #  #  #  #  #  .
     -3          #  .  f  f  f  f  .
     -2          #  .  f  P  I  D  .
     -1          #  .  f  f  f  H  f
      0          .  O  .  C  f  B  f
      1          .  .  .  f  f  f  f
      2          .  S  .  .  .  .  .
```

- `#`: actual StoneWall; the short return stops above the oven and never closes the front.
- `f`: actual StoneFloor owner, role `court-floor`, named worn stepping stones, walkable and non-Barren over existing Grass, on currently unoccupied work-space cells only. Service symbols retain their existing ground; no floor is stamped under them in this preferred tight scope.
- `P/I/D`: existing batch pan, input escrow, pickup board at `(0,-2)/(1,-2)/(2,-2)`.
- `H`: existing chair `(2,-1)`; `O`: oven `(-2,0)`; `C`: Cook spawn `(0,0)`; `B`: cot `(2,0)`.
- `S`: the one existing claspbean crop/prepared-soil cell `(-2,2)`, deliberately outside the floor.
- `.`: existing grass/open cells; not new pavement or forced clearing.

The15 stepping-stone cells form one connected circulation pattern rather than a complete paved rectangle:

- Rear aisle: `(-1,-3),(0,-3),(1,-3),(2,-3)` —4cells.
- Left connector: `(-1,-2)` —1cell.
- Main working aisle: `(-1,-1),(0,-1),(1,-1)` —3cells.
- Front connector: `(1,0)` —1cell.
- Front apron: `(0,1),(1,1),(2,1),(3,1)` —4cells.
- Right approach: `(3,-1),(3,0)` —2cells.

The full rear walk row is still open grass/floor from `x=-2..3,y=-3`; a material change is not needed on every traversable cell. The wall is9cells: `x=-3..2,y=-4` plus `(-3,-3),(-3,-2),(-3,-1)`. This yields one strong but incomplete edge, a rear working aisle and broad open front/right sides. It uses the real spaces where somebody stands and carries ingredients, rather than stone decorative patches placed arbitrarily beneath objects. There is no gate or roof. This settled15-empty-cell design supersedes the earlier29-cell under-owner apron considered during the source audit.

**Access reasoning, to be tested rather than assumed:** pan and escrow retain free cells above and below; pickup retains its rear aisle and right approach despite the chair below it. Chair has open cells left/right. Oven and cot have several ordinary approaches. The rear aisle has an open end at the right; it is not a one-cell sealed pocket. The crop bed remains surrounded by grass and does not become part of the work floor. An ordinary player can approach from the broad front or side and circulate behind the service row.

This orientation is a diagram, not a rule that every seed faces south. Apply the source’s actual permitted stamp rotation to all offsets together, without rotating the sprite pixels. Prefer the valid orientation whose open side is reached naturally from the existing approach/border network. In the current journey the player arrives from the north; do not choose a pose solely because its wall looks good if that wall hides the whole kitchen and forces an awkward detour. The open end/side and normal FOV must be checked in actual entry captures. The source currently tries0°/90° layouts; use those existing choices first rather than adding an orientation system for this pass.

## Visual hierarchy and expected payoff

1. The warm Cook and shallow pan row are the focal identity.
2. The dark stepping-stone pattern joins the existing objects into one roughly seven-cell-wide place, visible at1× without enlarging any sprite.
3. The incomplete masonry edge gives the group a back and one side. Its short return distinguishes the court from a random line of furniture, while the open sides show where the player can stand.
4. The small cultivated bed remains visually separate: brown soil/crop outside the worked stone, with no fake rows.
5. The frozen hedge remnants and trees stay background country. The court does not need another local hedge enclosure.

At2×, the player should see deliberate working space between the services and rear boundary. At1×, they should recognize one inhabited work area before inspecting individual objects. The gain is grounded spatial cohesion, not denser decoration. The sparse stone pattern must be noticeably distinct from grass but quieter than the actionable sprites and target outlines.

## Real-owner and generation constraints

- This is a fresh, exact connected kitchen stamp only, using the current world/biome/POI authority. Do not migrate, rearrange or repave a loaded saved kitchen for a screenshot.
- Validate the entire candidate—walls, actual floor ownership and open approach cells—before publication. Do not clear existing trees, hedges, water, hazards, creatures, other services, prepared beds or containers to fit it. If a footprint is invalid, try the next bounded candidate/rotation or the explicit fallback below.
- Each marked cell gets a real, passable, non-Barren StoneFloor owner with exact role `court-floor`; original Grass remains unchanged. Instance display/examine text says worn stepping stones with grass joints. Actual planting is intentionally retained between stones. The source/native ground tests must establish canonical selection, native world/examine agreement, crop/overlay priority, removal, rollback and save/Continue. Solid full-cell pavement would misrepresent this design and must be rejected.
- Exclude the crop’s actual prepared ground, every existing service/actor/object cell, and any already-cultivated/coated/ambiguous cell. Floor coordinates above are intended bare work spaces, not general replacement authority. The initial ordinary state may differ by seed, so validate all15 before publication rather than placing a partial/checkerboard apron.
- Existing source proofs currently allow only the crop soil’s Parts/tags to mutate. They do not already authorize arbitrary ground replacement or extra walls. The settled additive owned stepping stones avoid intentional original-ground replacement, but it still needs explicit fresh-owner/receipt handling, complete rejection/rollback and native-selection tests. Do not reuse a tint-only overlay or weaken receipts to make the screenshot work.
- Keep all inventory, meal terms, finite state, crop yield/moisture, repair state, cot behavior, stock and NPC AI unchanged. The Cook can move naturally; the floor describes a place, not a way to pin the actor.
- Preserve two usable approaches to each service, all previous reachable border exits and safe withdrawal. The current `Fits` loop skips later nonplot specs after index3, so it does **not** by itself establish access to the pan/trays or new wall/floor geometry. Test those actual action owners explicitly.
- Approved sprites, current hedge/flower masks, HUD, projection, source bytes and previous-presentation restore path remain frozen. The new work is the validated local court and truthful native ground integration.

## One fallback, if the full court will not fit

Retain the original accepted kitchen stamp unchanged if no complete9-wall/15-stone candidate can be safely placed. Do not retain the old two wall positions while adding this exact stone pattern: the old wall at `(-1,-2)` overlaps a new stone coordinate. Do not place a partial/checkerboard court, erase original owners, or paint a fake surface after failure. Record that the full court did not fit. A future smaller design would need its own explicit nonoverlapping layout; it is not part of this frozen scope.

No second alternative is recommended. More unrelated designs would delay testing the one clear spatial idea.

## Capture and imagegen brief

Primary close input: `Docs/Verification/Live16x24/working-landscape-64-kitchen-2x.png`.
Whole-view input: `working-landscape-64-kitchen-entry.png`.
Second-layout comparison: `working-landscape-65-kitchen-entry.png` and `working-landscape-65-kitchen-2x.png`.

Use the64 close capture for one constrained study and the whole view as scale/context reference. The new floor and wall are a **proposed real cold-generation edit**, so the concept may depict those specific cells; it must not make an old screenshot look like already-completed gameplay. Do not infer hidden geometry from fog or change the HUD. Save exact inputs, prompt, output and rejected additions.

Suggested prompt:

> CONCEPT STUDY of a proposed real world-generation edit, not a finished game screenshot. Use this actual flat16×24 Caves of Ooo kitchen capture. Preserve the approved Cook/player and existing oven, pan, input tray, pickup board, chair and cot artwork, their relative layout, scale and colors. Preserve the dark-green grass, existing distant hedges/trees, HUD and black hidden areas. Make this small service group read as an open-front remnant working court by introducing only a quiet sparse dark worn stepping stones with green grass joints and an incomplete real masonry back edge. The stepping-stone pattern occupies15 connected cells in empty working space around the existing group: the rear aisle, one left connector, the central working aisle, a front connector/apron and the right approach. Do not put the new stone under the existing services or crop; keep the actual claspbean soil cell outside it. Put a full walkable aisle between the pan/tray row and the rear masonry. The masonry has a six-cell back run and a short three-cell return on one side, leaving the front and other side broad and open. This geometry is only a design proposal to be validated and authored as real StoneFloor/StoneWall owners. Keep native square pixels and the existing small sprite scale; the material must be darker/quieter than the warm Cook and service objects. No added furniture, containers, lamps, signposts, doors, roof, food, water, tools, NPCs, active fire, smoke, gradients, 3D lighting, drop shadows, glowing markers, road painted on grass, texture carpet or new UI. Do not repaint approved sprites or produce another hedge-room pattern. Do not invent or reveal the world behind black fog. Show purposeful work space and a readable open approach, not more clutter.

The generated image cannot validate the exact cells or source preservation. Extract only the selected material hierarchy and large shape, then judge actual ordinary gameplay. If imagegen turns it into a house, adds an anvil/fire, blocks service access, brightens the floor excessively or redraws the Cook, reject those changes.

## Acceptance and stop criteria

Use a failing generation/integration case before runtime changes. Cover exact scope, deterministic candidate/rotation, two approaches to every service, preservation of exits/threat withdrawal, missing/ambiguous ground rejection, complete rollback, old save stability, crop-soil exclusion and actual sparse-stone identity, unchanged Grass substrate and intentional planting in joints. Ground art must agree with real current/remembered owners and coatings; no hidden-neighbor leakage.

Capture actual fresh64/65 entries and close views at1×/2×, with selection off for the main comparison. Walk onto and around the court, access pan/escrow/pickup/oven/cot, inspect the real ground, harvest/leave the crop bed, depart/return and save/Continue. The court should improve whole-view place recognition without enlarging objects or defeating the safe approach. If the north-entry view hides every useful object behind the new masonry, fix orientation/edge placement rather than claiming the close screenshot proves success.

Freeze when the service reads as one usable place, ground/state remains truthful and the ordinary route passes. Do not add more props or redraw the frozen landscape to fill remaining grass. This closes local framing only; it cannot establish complete opening coverage, motion, packaged release or physical Deck quality.

## Frozen coordinate/access confirmation

The exact9-wall and15-stone coordinates above were forwarded to `/root/projection_cold_review` for generation RED before implementation. All15 stone cells are cardinally connected and disjoint from the9 new walls and8 unchanged service/crop offsets. The old `(-1,-2)` shelter is replaced in cold specs; it is not retained beneath a stepping-stone owner.

Ten explicit new court-access points: `(0,-3),(1,-3),(2,-3),(0,-1),(1,-1),(3,-2),(3,-1),(3,0),(1,0),(0,1)`. Shared points establish north/south access for pan and escrow, north/east for pickup, west/east for chair and cot, and south/east for Cook. These do not replace the existing Oven, crop, border and withdrawal checks. The rear row `x=-2..3,y=-3` must remain open; its end cells are grass. Root’s final decision is non-Barren stepping stones with visible green joints and intentional existing planting, not full paving. No production files were edited by this design review.
