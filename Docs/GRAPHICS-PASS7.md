# Graphics Pass 7 — Hybrid sprite environment

> **Living plan + progress doc.** The biggest visual delta on the table:
> replace environmental CP437 glyphs (`#` walls, `.` floors, `~` water,
> `+` doors) with actual 16×24 pixel-art sprites. Creatures, items,
> and player keep their CP437 glyphs for readability. Toggleable via
> hotkey so ASCII purists can keep the original look.
>
> Where Pass 5 made the existing CP437 environment glyphs MOVE (water
> scrolls, grass sways, fire flickers via shader), Pass 7 REPLACES the
> static environment glyphs with actual tile sprites. Together they
> deliver:
>   - Stone wall sprites instead of `#`
>   - Dirt/cave floor sprites instead of `.`
>   - Animated water sprite (Pass 5 shader still applies → sprite scrolls)
>   - Door open/closed sprites instead of `+` / `'`
>
> Companion docs: Pass 1-6 documented in `Docs/GRAPHICS-POLISH.md`,
> `GRAPHICS.md`, `GRAPHICS-PASS4/5/6.md`.

---

## Status banner

| Field | Value |
|---|---|
| **Pass** | 7 of N |
| **Last updated** | 2026-05-09 |
| **Branch** | `feat/graphics-pass7-sprite-env` |
| **Sub-milestones complete** | 0 / 6 |

---

## Strategic decisions

### Hybrid approach (not full sprite mode)

Pass 7 ONLY converts the environmental layer to sprites:
  - Walls (`#`) → 16-variant auto-tiled stone wall sprites
  - Floors (`.`) → 1-3 random variant floor tiles
  - Water (`~`/`=`/`-`) → water sprite (animated by Pass 5 shader)
  - Doors (`+` closed, `'` open) → door sprites

Creatures + items + UI stay CP437. Why:
  - **Readability**: tactical perception of "what's that thing?" is
    hugely better with distinct CP437 glyphs than with similar-looking
    16×24 sprites.
  - **Asset budget**: hundreds of creature blueprints would each need a
    sprite. Pass 7 ships with ~25 environmental sprites total.
  - **Aesthetic identity**: the project has a clear ASCII identity
    that we're not abandoning, just enhancing.

### Toggle option

A new `SpriteEnvToggleController` (similar shape to
`CrtToggleController` from Pass 4 §4B) binds a hotkey (default `\` —
backslash) that toggles between hybrid sprite mode and pure CP437.
Default starts ON. State persists via PlayerPrefs.

### Sprite generation

I generate baseline sprites via the Piskel MCP — functional pixel
art (not professionally-painted gorgeous art). The architecture
allows dropping in better art later by replacing the PNG files.

Sprites are 16×16 (square) for environmental tiles to match Unity
2D tile conventions; the existing CP437 glyphs are 16×24 but we use
square tiles for environmental sprites so the auto-tiling math is
clean. Floor tiles render at slightly smaller world-cell size to fit
visually under the 16×24 entity glyphs.

---

## Sub-milestones

### 7A.1 — Generate baseline sprite atlas via Piskel

Create `Assets/Sprites/Environment/` containing:
  - `wall_atlas.png` — 16-variant 16×16 wall auto-tile sheet (4×4 grid)
    Each variant corresponds to a 4-bit neighbor mask
    (N=1, E=2, S=4, W=8). Variant 0 = isolated pillar; variant 15 =
    fully surrounded.
  - `floor_atlas.png` — 4 floor variants (16×16 each, 1×4 strip),
    randomly assigned per cell.
  - `water_tile.png` — single 16×16 water sprite (Pass 5 shader animates).
  - `door_closed.png`, `door_open.png` — 16×16 each.

Color palette: dark gray stones (#3a3838 / #4a4848 / #5a5858) +
ochre dirt floor (#5a4838 / #6a5848) + deep cyan water (#1a4858 /
#2a6878). Limited palette so it integrates with Pass 1's warm color
grading without fighting it.

### 7A.2 — Import + slice atlases as Unity Sprites

Each PNG imported as Sprite (Multiple) with sub-sprite slicing.
SpriteAtlas asset for batched draw calls.
Pixels-per-unit: 16 (one cell = 1 world unit).
Filter mode: Point (pixel-perfect).
Compression: None.

### 7B.1 — `EnvironmentSpriteRenderer` MonoBehaviour + 4-bit auto-tile

Same architectural pattern as Pass 5
(`AnimatedEnvironmentRenderer`) and Pass 6 (`GlyphGhostRenderer`):
post-render scan, paint to overlay tilemap, clear original cell on
main tilemap.

Auto-tile logic for walls:
```
mask = 0
if cellAt(x, y-1).isWall: mask |= 1 // N
if cellAt(x+1, y).isWall: mask |= 2 // E
if cellAt(x, y+1).isWall: mask |= 4 // S
if cellAt(x-1, y).isWall: mask |= 8 // W
spriteIndex = mask  // 0-15
```

Floor variant: hash the (x, y) cell coords to pick one of 4 floor
sprites deterministically. Same cell always shows same variant
across reloads.

### 7B.2 — `SpriteEnvToggleController`

Hotkey: backslash `\`. Toggles `EnvironmentSpriteRenderer.enabled`
between true/false. PlayerPrefs key
`CavesOfOoo.SpriteEnvironmentEnabled`.

### 7C.1 — Tests

  - `EnvironmentSpriteRenderer_WallNeighborMask_NorthOnly_Returns_1`
  - `EnvironmentSpriteRenderer_WallNeighborMask_AllSides_Returns_15`
  - `EnvironmentSpriteRenderer_FloorVariantHash_Deterministic`
  - `SpriteEnvToggleController_Toggle_FlipsEnabled`

### 7C.2 — Showcase scenario

`SpriteEnvironmentShowcase`: small chamber with walls + floor +
water pool + door. Visible side-by-side comparison when toggled
off (`\`) vs on.

---

## Deferred to future passes

- Per-biome environment palettes (cave moss vs desert sand vs
  jungle leaf-floor). Pass 8 follow-up — content-heavy.
- Proper hand-painted art replacing Piskel baseline. The wiring
  works; better PNGs drop in.
- Sprite-mode for creatures / items. Out of scope to preserve
  CP437 aesthetic identity.
- Auto-decoration (cracks, moss, blood stains) on floor sprites.
  Pass 9.

---

## Sub-milestone progress

| Sub-milestone | Status | Tests | Commit |
|---|---|---|---|
| 7A.1 Piskel sprite generation | ⏳ pending | n/a | — |
| 7A.2 Import + slice atlases | ⏳ pending | n/a | — |
| 7B.1 EnvironmentSpriteRenderer | ⏳ pending | 0 | — |
| 7B.2 SpriteEnvToggleController | ⏳ pending | 0 | — |
| 7C.1 Tests | ⏳ pending | 4 | — |
| 7C.2 Showcase scenario + commit | ⏳ pending | 1 (smoke) | — |
| **TOTAL** | **0 / 6** | **0** | — |

---

*Updated as each sub-milestone ships.*
