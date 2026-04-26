# Tile Aspect Ratio — Should We Switch to 16×24?

**Status:** Analysis + migration plan. Decision pending.
**Author:** AI investigation, 2026-04-18.
**TL;DR:** Yes, switch. Phase the work into two stages: plumbing (~half-day,
ships independently) then font/glyph art (~day, optional). Switching aligns
the project with its stated Qud-mirror goal and fits the ASCII-roguelike
aesthetic far better than the current square cells.

---

## Why this question matters

The project is explicitly modeled on **Caves of Qud**, which uses 16×24
(2:3) tiles. We currently use 16×16 (square). The mismatch shows up in
three places that compound over time:

1. **Aesthetic divergence from the source.** Square cells give a "modern
   indie tileset" feel (Stardew Valley, Terraria, Minecraft sprites).
   2:3 cells give a "classic roguelike / DOS terminal" feel — the entire
   genre we're mirroring.
2. **Letterform geometry.** ASCII / CP437 letters are inherently
   taller-than-wide. Squashing them into square cells either wastes
   horizontal space or distorts proportions. 2:3 cells fit `@`, `g`, `T`,
   `&` naturally.
3. **Future sprite art ceiling.** Adventure-Time-themed pixel sprites
   (Finn's hat, BMO's antenna, Jake's stretched proportions) need
   vertical room that 16×16 doesn't give. Decision is easier to make
   now than after the asset library grows.

The case against switching is real but narrow: square tiles are easier
for top-down or isometric sprite art. We don't render that, so it
doesn't apply.

---

## Current state — what's hardcoded where

**The good news:** tile pixel size is defined in **exactly one file**:

```
Assets/Scripts/Presentation/Rendering/CP437TilesetGenerator.cs
  Line 15-17:  GlyphSize = 16, Columns = 16, Rows = 16
  Line 104:    Sprite.Create(_atlasTexture, ..., GlyphSize) // PPU = 16
  Line 558-9:  TextGlyphW = 8, TextGlyphH = 16 (half-width sidebar text)
```

`grep` confirms **no other .cs file references `GlyphSize`, `TextGlyphW`,
or `TextGlyphH`**.

**The complication:** the rest of the codebase doesn't think in pixels —
it thinks in **abstract "1 cell = 1 world unit"** terms, with the cell's
square aspect baked in. That assumption is implicit across:

| File | Line(s) | What's hardcoded |
|------|---------|------------------|
| `Assets/Scenes/Main/SampleScene.unity` | 384 | Main grid `m_CellSize: {1, 1, 1}` |
| `Assets/Scenes/Main/SampleScene.unity` | 228 | Camera `orthographic size: 13` |
| `Presentation/Rendering/ZoneRenderer.cs` | 270, 290 | Sidebar/hotbar grids `cellSize = (0.5, 1, 0)` |
| `Presentation/Rendering/ZoneRenderer.cs` | 314 | Popup grid `cellSize = (1, 1, 0)` |
| `Presentation/Rendering/GameplayViewportLayout.cs` | 116 | `charWidthWorld = 0.5f * scale` (assumes 8/16 = half) |
| `Presentation/Rendering/GameplayHotbarLayout.cs` | 35 | `camera.orthographicSize = GridHeight * 0.5f` |
| `Presentation/Rendering/CenteredPopupLayout.cs` | 54 | Same `* 0.5f` ortho-size pattern |
| `Presentation/Rendering/CampfireEmberRenderer.cs` | 167 | `localScale = (0.5, 0.5, 1)` (half-cell) |
| `Presentation/Cameras/CameraFollow.cs` | 27, 90-97 | `TargetVisibleTileRows = 34`, ortho-size derivations |
| `Presentation/UI/LookOverlayRenderer.cs` | 32-41 | Viewport math relative to ortho size |

**No PNG sprite assets** are sized at 16×16 in-game. Everything renders
through the runtime atlas built by `CP437TilesetGenerator`.

The bitmap font in `DrawChar16` (lines 147–445) is **8×8 hex-encoded
data scaled 2× to fill 16×16**. Going to 16×24 means either centering
8×8 in a 16×24 box (visually loose) or using 8×12 source data scaled 2×.

---

## Qud reference — confirmed 16×24

```
qud_decompiled_project/GameManager.cs:148-150
  tileWidth = 16
  tileHeight = 24
qud_decompiled_project/LetterboxCamera.cs:62-64
  Same constants.
```

Used as multipliers in `GameManager.cs:674, 751, 1532, 1542, 1854, 1863`
with magic offsets `+8` (½ tileWidth) and `−12` (½ tileHeight). Qud
**works directly in pixel space**, threading `tileWidth` / `tileHeight`
through every world-position calculation.

We don't need to mirror Qud's pixel-space architecture — our cell-unit
abstraction is cleaner. We just need to acknowledge that the cell
**isn't square** and propagate a `cellHeight` factor through the math.

---

## Architectural implication

Two valid implementation paths:

### Path A — Non-square cell-units (recommended)

Cells become 1.0 units wide × 1.5 units tall. Atlas sprites are 16×24
pixels, generated at PPU = 16, so they're 1.0 × 1.5 world units. Half-
width text becomes 8×24 (sidebar uses tall thin glyphs) or stays 8×16
(text rows decouple from world rows — sidebar uses its own grid).

**Pros:** smallest diff, cleanly threads through existing math.
**Cons:** one more constant (`CellHeightUnits = 1.5f`) propagating
through camera + layout files.

### Path B — Pixel-space (Qud's approach)

Switch entirely to pixel-space math. Cell = 16×24 pixels, all positions
in pixels, camera ortho-size in pixels.

**Pros:** matches Qud's architecture exactly; no abstraction mismatch
ever again.
**Cons:** ~3× the diff — every cell-unit math site becomes pixel math.
Doesn't unlock anything Path A doesn't.

**Recommendation: Path A.** The cell-unit abstraction is a feature, not
a defect. We just need it to support non-square cells.

---

## Risk assessment

Things that aren't simple constant-bumps:

1. **Half-width text geometry decouples.** `charWidthWorld = 0.5f * scale`
   currently assumes the text glyph is exactly half-cell. With 16×24 world
   cells and 8×16 text glyphs, the ratio is **0.5 horizontal but 0.667
   vertical**. Sidebar/hotbar text rows stop aligning with world rows
   for free. **Mitigation:** introduce `charHeightWorld` and let sidebar
   use its own grid. The sidebar already has a separate Tilemap, so this
   isn't a deep refactor — it's a layout-math threading task.

2. **Bitmap font needs replacing OR centering.** The 8×8-scaled-2×-to-16×16
   `DrawChar16` font fills the cell now. In a 16×24 cell at 2× scale,
   8×8 fills only the top 16 rows of a 24-row cell — the bottom 8 rows
   are empty. Three options:
   - **Option F1 (lazy):** Center 8×8 in 16×24 (4px padding top + bottom).
     Glyph "floats" in the cell but everything works.
   - **Option F2 (correct):** Source an 8×12 DOS font (50-line VGA mode)
     and 2×-scale to 16×24. Authentic terminal aesthetic. Some glyph
     data exists in public domain.
   - **Option F3 (custom):** Hand-redraw the 26 glyphs in `DrawChar16`
     to fill 16×24. ~150 lines of bitmap arrays. Highest fidelity to
     our specific aesthetic.
   - Stage 1 ships with **F1** (no font work). Stage 2 picks F2 or F3.

3. **Pixel-perfect alignment needs rederivation.** Current
   `orthographic size = 13` works cleanly with 16×16 tiles at the target
   resolution. With 16×24 tiles, the new ortho-size depends on whether
   you want to keep the same number of visible rows (34) or the same
   visible world height. Camera math in `CameraFollow.cs:90-97` and
   `GetGameplayZoomSize()` needs explicit cell-height awareness.

4. **`FineWaterCellSize = 1/6` (subdivision)** in `ZoneRenderer.cs:82`
   is a 6×6 sub-grid per main cell. With non-square main cells, the
   sub-grid splits into x = 1/6, y = 1/4 (= 1.5/6). Either accept the
   non-square sub-cell or bump to 1/8 vertical for visual symmetry.

5. **`DrawChar16` patterns**. The player `@`, walls `#`, tree `T`, floor
   speckle, solid block `█`, etc. are explicit 16-row string arrays.
   Each needs 8 more rows or vertical centering. Lines 147–445.

**No rendering bug, save-file risk, or irreversible state lives in this
change.** Everything is visual. Rollback is a `git revert` away.

---

## Decision matrix

| Option | Effort | Visual outcome | Recommendation |
|--------|--------|----------------|----------------|
| **Stay at 16×16** | 0 | Square cells, "modern indie tileset" feel | Default if shipping soon and aesthetic isn't priority |
| **Stage 1 only** (16×24 cells, glyphs centered in tall box) | ~half-day | Tall cells, glyphs feel airy/loose, classic roguelike aspect | Best ROI — ships independently, gets 80% of the aesthetic win |
| **Stage 1 + Stage 2** (16×24 cells, redrawn 8×12 font) | ~1.5 days | Authentic Qud-class roguelike aesthetic | Right answer if you're investing in the look long-term |
| **Pixel-space rewrite (Path B)** | 2-3 days | Same as Stage 1+2 | Not recommended — 3× the work for zero additional capability |

---

# Migration plan

## Stage 1 — Plumbing (no font work)

**Goal:** Cells become non-square (1.0 × 1.5 world units, 16×24 pixels).
Existing 8×8 hex-font glyphs get centered with vertical padding. Game
runs, looks airy, has classic-roguelike vertical aspect, no asset work.

### Stage 1, Step 1 — Constants in CP437TilesetGenerator

**File:** `Assets/Scripts/Presentation/Rendering/CP437TilesetGenerator.cs`

Replace `GlyphSize = 16` with split width/height:

```csharp
public const int GlyphWidth  = 16;
public const int GlyphHeight = 24;
public const int Columns     = 16;
public const int Rows        = 16;
// Atlas is now 256 wide × 384 tall (was 256 × 256).

// Pixels-per-unit is GlyphWidth (16) so a 16×24 sprite = 1.0 × 1.5 world units.
public const int PixelsPerUnit = GlyphWidth;
```

Update `Sprite.Create` calls (lines 104, ~454-455, ~485-486, ~522-523,
~631-632) to use `(GlyphWidth, GlyphHeight)` rect dimensions and
`PixelsPerUnit` for PPU.

Update `TextGlyphH` decision: keep at 16 if sidebar uses its own grid
(decoupled rows), or bump to 24 if sidebar rows align with world rows.
**Recommendation:** keep at 16, decouple sidebar (less art rework, sidebar
already has its own Tilemap).

### Stage 1, Step 2 — Center existing glyphs in the tall cell

**File:** `Assets/Scripts/Presentation/Rendering/CP437TilesetGenerator.cs`

The `DrawChar16` patterns (lines 147–445) are 16-row string arrays.
For Stage 1, **don't redraw** — just modify `DrawFromHex` (line 516)
and the `DrawChar16` blitter to write into rows 4–19 of the 24-row
glyph cell, leaving rows 0–3 and 20–23 as transparent padding.

```csharp
// Old (square cell):
//   blit row r of pattern → row r of glyph
// New (centered in tall cell):
//   blit row r of pattern → row (r + 4) of glyph
const int VerticalPadding = (GlyphHeight - 16) / 2; // = 4
```

### Stage 1, Step 3 — World-grid cellSize

**File:** `Assets/Scenes/Main/SampleScene.unity`

Line 384: `m_CellSize: {x: 1, y: 1, z: 1}` → `{x: 1, y: 1.5, z: 1}`.

Note: line 113's `0.16666667` (`1/6`) is a *different* `cellSize` field
on `FineWaterTilemap` — leave it alone for now (or bump y to `0.25`
for visual symmetry; see Risk #4).

**File:** `Assets/Scripts/Presentation/Rendering/ZoneRenderer.cs`

- Line 270, 290: sidebar/hotbar grids `cellSize = (0.5, 1, 0)` →
  decision per Step 1 (decoupled, stays `(0.5, 1, 0)`)
- Line 314: popup grid `cellSize = (1, 1, 0)` → `(1, 1.5, 0)` (popup
  uses world-grid aspect)

### Stage 1, Step 4 — Camera orthographic-size math

**File:** `Assets/Scripts/Presentation/Cameras/CameraFollow.cs`

Add a constant at the top:

```csharp
public const float CellHeightUnits = 1.5f;
public const float CellWidthUnits  = 1.0f;
```

Update `GetGameplayZoomSize()` (around line 61) and the ortho-size
derivations at lines 90-97 to multiply by `CellHeightUnits` wherever
they previously assumed 1.0:

```csharp
// Old:
float halfH = TargetVisibleTileRows * 0.5f;
// New:
float halfH = TargetVisibleTileRows * CellHeightUnits * 0.5f;
```

Recompute the scene's `orthographic size`. With `TargetVisibleTileRows =
34` and `CellHeightUnits = 1.5f`, ortho-size becomes 25.5. Or reduce
`TargetVisibleTileRows` to 22-24 to keep ortho-size near 13 and preserve
crisp rendering at the current screen resolution.

### Stage 1, Step 5 — Layout files

**File:** `Assets/Scripts/Presentation/Rendering/GameplayHotbarLayout.cs`

Line 35: `camera.orthographicSize = GridHeight * 0.5f` →
`camera.orthographicSize = GridHeight * 0.5f * CellHeightUnits`
(or keep the hotbar's grid at 1.0 cellSize if it's decoupled from world
geometry — which it should be).

**File:** `Assets/Scripts/Presentation/Rendering/CenteredPopupLayout.cs`

Same pattern as hotbar (line 54).

**File:** `Assets/Scripts/Presentation/Rendering/GameplayViewportLayout.cs`

Line 116: introduce `charHeightWorld`:

```csharp
public float charWidthWorld  = 0.5f  * scale;
public float charHeightWorld = 1.0f  * scale; // text rows are 1.0 world units (16px @ PPU=16)
                                              // — independent of world-cell height
```

Propagate `charHeightWorld` to every site currently assuming text rows
align with world rows.

**File:** `Assets/Scripts/Presentation/UI/LookOverlayRenderer.cs`

Lines 32-41: rebase the reference zoom factor against the new ortho-size.

### Stage 1, Step 6 — Misc

**File:** `Assets/Scripts/Presentation/Rendering/CampfireEmberRenderer.cs`

Line 167: `localScale = (0.5, 0.5, 1)` → `(0.5, 0.75, 1)` for visual
parity in the tall cell.

### Stage 1, Step 7 — Test pass

In Play mode, verify:

- [ ] Player `@` renders centered in its cell (top/bottom padding visible)
- [ ] Walls render with same horizontal extent, taller vertical extent
- [ ] Sidebar/hotbar text still aligned, readable
- [ ] Popups (inventory, dialog, look-overlay) center correctly
- [ ] Camera follow tracks player smoothly
- [ ] FOV looks correct (no row clipping)
- [ ] Existing scenarios still launch + look right

If pixel-perfect alignment looks fuzzy: tune ortho-size to make
`pixels-per-world-unit` an integer. Formula:

```
PPU_target = screen_height_px / (2 * orthographicSize)
```

For 800px tall screen and target PPU=16: ortho-size = 25.0.

### Stage 1 acceptance criteria

- [ ] All existing tests pass (1462 baseline)
- [ ] In-Editor: scenario launches preserve all existing visual behavior
      EXCEPT the now-tall cells
- [ ] No regression in look-mode, inventory UI, hotbar, sidebar
- [ ] Camera follow and zoom behave correctly
- [ ] Commit message documents `CellHeightUnits = 1.5f` as the new
      convention, future PRs must thread it through any new math

---

## Stage 2 — Font work (optional, ships independently of Stage 1)

**Goal:** Replace the centered-8×8-in-tall-cell glyphs with a font that
*fills* the 16×24 cell. Pick one of the three options below; do not
attempt all three.

### Option F2 — Source 8×12 DOS font

The classic VGA 50-line text-mode font is 8×12. Public-domain
implementations exist (search "8x12 BIOS font hex"). Add the 256-byte
glyph data alongside the existing 8×8 data and switch a const flag in
`CP437TilesetGenerator`:

```csharp
public const int FontSourceWidth  = 8;
public const int FontSourceHeight = 12; // was 8 in Stage 1
```

`DrawFromHex` 2×-scales each source pixel to a 2×2 block, producing
16×24 output. **Best aesthetic for "Qud-class roguelike" feel** because
it's an authentic terminal font.

### Option F3 — Hand-redraw `DrawChar16` patterns at 16×24

For each of the ~26 hand-drawn glyphs in `DrawChar16` (player `@`,
walls, tree, floor, solid block, fill speckle, etc.), redraw the
bitmap as a 24-row string array. ~150 lines of edits across the file.
**Best for project-specific aesthetic control** (Adventure-Time
sprites get tall headroom for hats/antennae).

You can mix F2 + F3: F2 for letters/symbols (the bulk of CP437),
F3 for hand-drawn world glyphs. This is what Qud effectively does
(font for letters, art for world tiles).

### Option F1 — Stay with centered 8×8

Keep Stage 1's "8×8 centered in 16×24" forever. Acceptable but loose-
looking. Choose this if Stage 2 keeps getting deferred — it's better
than reverting Stage 1.

### Stage 2 acceptance criteria

- [ ] Letters fill 16×24 cells without obvious whitespace gaps
- [ ] World glyphs (player, walls, trees, doors) read clearly at the
      intended viewing zoom
- [ ] No regression in Stage 1 acceptance criteria
- [ ] Glyph baseline is consistent (letters sit at the same row across
      the alphabet — easier to parse text quickly)

---

## Validation checklist

Before shipping Stage 1:

- [ ] `mcp__unity__refresh_unity` returns no compile errors
- [ ] Full EditMode test suite passes (1462 / 1462 currently)
- [ ] Scenarios launch from menu without visual regression
- [ ] Camera follow smooth on player movement
- [ ] Look-mode renders cursor at correct cell
- [ ] Inventory popup centers correctly
- [ ] Hotbar text aligned with hotbar slots
- [ ] Sidebar text doesn't overlap world view
- [ ] Field-of-view boundary cells render correctly (no row clipping)
- [ ] Particle effects (DamageFlash, CampfireEmber, AsciiFx) scale
      proportionally

Before shipping Stage 2:

- [ ] All Stage 1 checks still pass
- [ ] `@` glyph reads as a player character at viewing zoom (not a
      floating dot)
- [ ] Letters in MessageLog / sidebar are crisp and readable at native
      resolution
- [ ] Letterforms have consistent baseline

---

## Rollback strategy

This change is purely visual. No save-file format changes, no scenario
contracts modified, no test infrastructure affected. Rollback is a
`git revert` of the Stage 1 commit (and Stage 2 commit if shipped).

If Stage 1 ships and a critical visual bug emerges in production, the
revert is safe at any time.

---

## Open questions / decisions needed

1. **`CellHeightUnits` value.** 1.5f mirrors Qud's 16×24 exactly. Could
   also pick 1.333f (16×21.3, less extreme) or 1.667f (16×26.7, more
   extreme). **Recommend 1.5f** — exact Qud match, clean fraction.

2. **`TargetVisibleTileRows` recalibration.** Current 34 with square
   cells × ortho 13 ≈ 26 visible rows on screen. With tall cells
   keeping the same camera, you'd see ~17 rows — a major UX shift.
   Either:
   - **Option Z1:** Drop `TargetVisibleTileRows` to 22 and ortho to ~16.5
     to keep visible-cells count similar to today. (Tighter view.)
   - **Option Z2:** Keep `TargetVisibleTileRows = 34` and let ortho jump
     to 25.5. (Wider, more zoomed-out view.)
   - **Recommend Z1** — preserves player-facing UX. Wider view changes
     gameplay feel (more visible enemies = different tactical experience).

3. **Sidebar text alignment.** Stage 1 plan keeps text at 16-pixel-tall
   while world cells are 24-pixel-tall. Sidebar text rows no longer
   align with world rows. Is this acceptable? Qud has this same
   decoupling. **Recommend yes** — sidebar is its own visual zone, no
   reason rows must match world.

4. **Stage 2 font choice (F2 vs F3 vs F2+F3 hybrid).** Decide at Stage 2
   start, not now.

5. **`FineWaterCellSize` aspect.** Currently 1/6 = 0.1667 (square sub-
   grid). With 1.5-unit-tall main cells, options:
   - Keep sub-cell square (1/6 × 1/6) → main cell holds 6 sub-cols × 9
     sub-rows. Easier math, asymmetric water grid.
   - Stretch sub-cell (1/6 × 1/4) → 6 × 6 sub-grid in tall cell.
     Symmetric grid, distorted sub-cells.
   - **Defer this decision** — the water sub-grid is a niche detail
     that can be tuned post-Stage 1 once the visual is concrete.

---

## Why this analysis exists

Started as "should we mirror Qud's 16×24?" — but the deeper question is
**"what aesthetic are we committing to?"** Answer: classic ASCII
roguelike, Adventure Time-themed. That answer makes the tile aspect
ratio a derived decision, not an arbitrary one. 16×24 is the right
answer because the genre we're in answered it 30 years ago.

The migration is bounded, reversible, and benefits compound the longer
we wait (every new layout file written against square cells is more
work to retrofit). **Recommend doing Stage 1 in the near term**, even
if Stage 2 sits in the backlog indefinitely.
