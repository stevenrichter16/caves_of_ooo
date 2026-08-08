# GRAPHICS PASS 15 — Sprite Reintegration: Flowing Ground, Honest Objects, Verified by Eye

> Status: APPROVED + IN PROGRESS (user sign-off 2026-08-08; monsters
> stay ASCII). Brainstorm additions folded in: **wall top-face**
> (lighter 3-4px top band where no wall above — walls read as solid
> blocks), **overlap skirts + priority stacking** for ground fringes
> (soft material overhangs hard: grass laps sand laps stone),
> **tall 16×20 actors** (bottom pivot, overhang the cell above —
> actors break the tile plane dimensionally), **value zoning**
> (terrain 30-60% brightness, actors get the extremes; enforced by
> generator gate), and an **automated squint test** in the V-loop
> (downscale 4×, assert actor-vs-neighborhood contrast). Deferred to
> Pass 16: dual-grid rendering, day/night light curve, wall-edge AO,
> drop-shadow bg pass, monster token plates, zone fade + POI banners,
> per-biome ambient particles.
> Sources: (a) full audit of the dormant Pass 7-14 sprite system,
> (b) deep study of the farming project
> (`/Users/steven/farming/.claude/worktrees/farming-sim-sprite-setup-d2ab60`),
> whose rendering the user called "very clear and seamless."
> The prior attempt failed the eyeball test because it was never
> eyeballed: the gate went off 2026-05-10 and Passes 12-14 were all
> authored AFTER that — compile-verified, never once seen running.

## 0. What the farming game taught us (the five laws)

1. **Macro-field slicing.** Ground variation must live LARGER than the
   tile: author one seamless toroidal 64×64 image, slice 4×4, index by
   `(x mod 4, y mod 4)`. The grid disappears at the source. One modulo.
2. **Flat fills; boundaries are a 1-2px darker LIP, never a blend,
   never noise.** Per-tile speckle prints the same mark every 16px and
   reads as damage — our Muted Overgrowth tiles are speckled; that
   speckle must go (style guide gets amended, sprites regenerated).
3. **Objects get a 1px dark ink outline; terrain NEVER does.** This
   single contrast separates "thing on ground" from "ground" better
   than any saturation trick.
4. **Edges live on a transparent overlay, generated as a complete
   family** (SDF/scallop sets), over an untouched base — N materials
   need N fringe families, not N×N transition tiles.
5. **Pixel-perfect discipline enforced in code**: import postprocessor
   (Point/uncompressed/no-mips/FullRect/extrude-0), atlas padding,
   integer sorting bands, snap-to-pixel camera.

## 1. Phase R — repair the substrate (blockers found by audit) — ✅ SHIPPED

R1-R5 all landed (2026-08-08): sprites moved to
`Assets/Resources/Sprites/Environment/` (git mv — GUIDs intact) with a
runtime `Resources.Load` path; the vertical-mirror fix (`zoneY =
Height-1-y` feeds every zone lookup, plus the player-torch fix in
LightSourceSpriteHook); release-restores-glyph claims (struct Claim
remembers displaced tile+color; `NotifyMainTilemapCleared` handles the
full-repaint path so stale glyphs never resurrect); the
SpriteImportPostprocessor (enforced pixel-perfect settings, kills the
24 stale per-platform compression overrides, preserves atlas Multiple
mode); single top-entity fetch per cell + crop scan only on `*Crop` +
`COO.EnvSprites.PostRender` ProfilerMarker + claim capacity 2048.
Verified by 5 NEW harness tests driving the real component end-to-end
(real Resources sprites, real tilemaps, real zone) — the class of test
whose absence hid all three defects. Suite 5915 → 5920.

## V-loop round 1 (2026-08-08) — three live-screenshot fixes

The eyeball loop immediately earned its existence. Three defects no
test saw, each found in a screenshot, diagnosed with read-only
execute_code queries against the RUNNING game, fixed, and re-verified:

1. **The river was still ASCII** — the animated-water renderer strips
   water glyphs off the main tilemap before our pass, so the
   glyph-keyed water branch never fired. → Water claims by BLUEPRINT
   in the pre-pass.
2. **"Specks" — scattered dark holes across the ground** — same root
   cause generalized: the animated env also claims grass/floor
   glyphs; overlay-tile census (execute_code) showed the holes were
   exactly its claimed cells. → ALL ground claims are now
   blueprint-driven and glyph-independent; our overlay (order 3)
   covers its layers (order 2).
3. **Objects floating in dark boxes** — sprites' transparent margins
   revealed the bg contrast box. → Every claim also paints the cell's
   ground material into the BG tilemap; ASCII actor letters likewise
   stand on terrain now.

Live result (zoomed screenshots, starting town): continuous flowing
ground, river with scalloped shoreline lips, wall runs reading as
solid capped blocks, objects/actors sitting directly ON the ground.

**Punch list for round 2 (Phase S):**
- FOV/lighting: ground claims tint with Color.white — fog-of-war
  dimming doesn't reach sprite terrain yet (sample the bg box color
  instead).
- Claims ignore Explored — chests/water may reveal through fog
  (pre-existing from Pass 10, now wider; gate claims on cell.Explored).
- Ink outlines on all object/actor sprites; player highlight.
- False-identity guards (traps/viper/troll/veins/grimoires).
- 9 shopkeeper/hermit sprites; bush sprite restyle (reads as popcorn).
- Automated squint test on saved screenshots.
GraphicsPolish gate: ON in working tree for the loop; ships committed
only after the full checklist passes.

| # | Defect | Fix |
|---|---|---|
| R1 | `LoadSprites` is `#if UNITY_EDITOR` + `AssetDatabase` — null in any build, assets strippable | Move PNGs to `Assets/Resources/Sprites/Environment/`, load via `Resources.Load`; keep paths in one manifest |
| R2 | Blueprint tier reads the zone VERTICALLY MIRRORED (`PostRender` passes tilemap-y into `zone.GetCell`; ZoneRenderer paints at `Height-1-y`) — same bug in `LightSourceSpriteHook` player torch | One `zoneY = Height-1-y` feeding all zone lookups; harness test that paints a marked zone and asserts overlay/zone correspondence |
| R3 | Dirty-path repaints blank every non-dirty claimed cell (overlay released, main glyph was nulled last frame) | Stop nulling the main tilemap; paint the overlay ABOVE the glyph (it fully covers 16×16), release = just clear overlay cell |
| R4 | 24 stale `.meta`s carry per-platform DXT compression — banding on the exact tiles covering most screen | Normalize all metas uncompressed; add our own `ArtImportPostprocessor` (farming's, adapted to PPU 16) so it can never regress |
| R5 | Perf: 2-3 redundant `GetTopVisibleObject`/`GetPart` scans per cell ×2000, zero ProfilerMarkers | Single top-entity fetch per cell threaded through tiers; `ProfilerMarker` around `PostRender`; re-baseline PERF-FOUNDATION table |

## 2. Phase G — ground that flows

- **G1 Macro fields** replace the hash-picked floor atlas: seamless
  4×4 toroidal fields (adapted `macro_field()` from the farming
  ArtTools, PIL) for: grass, sand, generic stone floor, and the five
  strata floors as value-shifted recolors of one stone macro
  (sandstone/limestone/shale/slate/quartzite/obsidian finally look
  different — today five strata collapse to one ochre atlas and
  SlateFloor literally renders as the BONES sprite).
- **G2 Edge fringes on a new overlay tilemap** (order between floor
  and objects): generated scalloped fringe families — water shoreline
  first (the river!), then grass-over-sand and grass-over-stone.
  Grass is the universal base, priority model, no N×N matrix.
- **G3 Walls restyled flat+lip** using the existing 4-variant
  classifier; full 16-variant autotile only if screenshots demand it.

## 3. Phase S — honest, readable objects

- **S1 Outline pass**: every actor/fixture/item sprite gets the 1px
  ink ring (regenerated via `outline()`; bodies keep their Pass 13-14
  designs); all terrain speckle removed per law #2. STYLE-GUIDE.md
  amended accordingly.
- **S2 Kill the false-identity table**: the glyph tier gets blueprint
  guards so Viper≠water, traps≠stalagmite, DesertBandit≠chair,
  SleepingTroll≠tree, ore veins≠campfire (they currently even spawn
  fire lights), grimoires≠door, seeds≠bones. Mismatch → honest CP437
  glyph (an honest letter beats a false sprite).
- **S3 Coverage for the town**: sprite the 9 sprite-less friendly
  NPCs the player meets most (5 shopkeepers + 4 hermits — cheap
  palette-kin of the villager base). **Monsters stay glyphs for now**
  — outlined-sprite NPCs + flat ground makes letters MORE readable
  than before; the ~40-creature bestiary is its own future art pass.
- **S4 Player findability**: player sprite gets the strongest ink
  outline + a subtle 1-cell ground highlight underneath (reuses the
  proven bg-tint pipeline).

## 4. Phase V — the eyeball loop (the step that never happened)

Computer-use screen access to Unity is granted. Loop per change-batch:
enter Play (⚠ resets the scene — flagged each time) → screenshot the
Game view → `zoom` into seams/actors → judge against a fixed checklist
(no grid pattern? player found <1s? actor/terrain separation? strata
distinct? no false identities? water edge clean?) → adjust generators →
repeat. Before/after screenshots saved to `Assets/Screenshots/`.
Verification is BLOCKING: the gate ships ON only after the checklist
passes on live screenshots of: starting town, cave wilderness, a
strata zone, the river, and one combat scene.

## 5. Order & estimates (agent-pace)

R1-R5 (~3h) → G1-G2 (~4h art+code) → S1-S2 (~3h) → V loop interleaved
from first R-complete build → S3-S4 (~2h) → G3 + polish (~2h).
Tests: harness tests for R2/R3 (the class of bug 44 resolver pins
couldn't see), resolver-guard tests for S2, generator self-validation
(palette/size/opacity gates like the farming ArtTools).
