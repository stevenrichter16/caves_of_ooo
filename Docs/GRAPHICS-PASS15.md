# GRAPHICS PASS 15 — Sprite Reintegration: Flowing Ground, Honest Objects, Verified by Eye

> Status: PROPOSED — awaiting user go-ahead.
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

## 1. Phase R — repair the substrate (blockers found by audit)

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
