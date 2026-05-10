# Graphics Pass 5 — Animated Environment Tiles

> **Living plan + progress doc.** Goal: water glyphs scroll, grass glyphs
> sway, torch glyphs flicker — visible motion on the 80×25 CP437 grid
> instead of frozen ASCII. Where Pass 1-4 added post-processing
> (vignette, bloom, CRT) on top of a static frame, Pass 5 makes the
> frame itself move.
>
> Companion docs:
> - `Docs/GRAPHICS-POLISH.md` — Pass 1+2 substrate
> - `Docs/GRAPHICS.md` — Pass 3 HDR colors + flicker + biome palettes
> - `Docs/GRAPHICS-PASS4.md` — HitStop + CRT phosphor
>
> Companion fixes (post-Pass-4 debug session):
> - `19ffaa6` — Volume profile sub-asset persistence
> - `57f2dc6` — URP colorGradingMode HDR
> - `9afefed` — CRT toggle binds to inactive Volume
> - `3666f1d` — bloom threshold + InputHelper

---

## Status banner

| Field | Value |
|---|---|
| **Pass** | 5 of N |
| **Last updated** | 2026-05-09 |
| **Branch** | `feat/graphics-pass5-animated-tiles` |
| **Sub-milestones complete** | 5 / 5 |

---

## Strategy: shader-driven, not frame-swap

The Pass 5 brainstorm proposed Piskel sprite generation for water/torch
frames. Re-evaluating: the existing CP437 glyph cache is already a
high-quality 16×24 sprite atlas. Three observations make
**shader-based animation** the better v1 path:

1. **No new art needed** — the `~`, `,`, `*`, etc. glyphs already
   exist as sprites in the CP437 cache. Custom shader animates the
   pixels of those sprites at draw-time.
2. **Smooth motion vs choppy frame-swap** — vertex displacement
   (grass sway) and UV scrolling (water flow) produce continuous
   60fps motion. 4-frame swap at 8fps gives jagged motion.
3. **Lower blast radius** — single shader + single overlay tilemap
   layer integrates without touching ZoneRenderer's hot per-cell
   render loop.

Frame-based art via Piskel is deferred to **Pass 6** if the v1
shader pass isn't enough.

---

## Pre-impl verification sweep

### V1 — Existing tilemap structure

`ZoneRenderer.cs` lines 245-285 show the layered tilemap setup:
- `_bgTilemap` (sortingOrder=-1) — solid color blocks
- `_tilemap` (sortingOrder=0) — main CP437 glyph tilemap
- `_fineWaterTilemap` (sortingOrder=1) — fine sub-cell water overlay
- `_fxTilemap` (sortingOrder=2) — combat FX overlay
- (NEW Pass 5) `_animatedEnvTilemap` (sortingOrder=0.5) — animated
  overlay for water/grass/torch glyphs

All tilemaps share the same `Grid` and inherit `Sprite-Lit-Default`
material.

### V2 — Glyph identification per cell type

`ZoneRenderer.DensityGlyph(val)` (line 1438) returns water glyphs
based on density: `=` → `-` → `~` → `.` → `' '`. So water cells
are identified by these glyph values.

Grass glyphs in this project are typically `,` (per CP437 convention).
Fire/torch glyphs are `*`. Confirmed by inspecting blueprint
`RenderString` values.

### V3 — Custom material on a tilemap

`Tilemap.material` accepts any material. A material assigned a
custom shader gets used by ALL tiles on that tilemap. By creating
ONE new tilemap layer with the animated material, every tile
painted there gets the shader treatment automatically — no
per-cell wiring needed beyond "paint this glyph here too."

### V4 — Vertex shader access via Sprite-Lit-Default URP 2D

URP's `Sprite-Lit-Default` is a `.shader` file. Forking it for
custom vertex displacement is straightforward:
1. Copy the URP source as a starting point
2. Modify the vertex shader to add `worldPos += sin(_Time.y +
   instancePos.x) * vec3(swayX, 0, 0) * vertexHeight`
3. Save as new `.shader` asset
4. Create material referencing it
5. Assign to new tilemap

The URP package source is at:
`Library/PackageCache/com.unity.render-pipelines.universal@d10049dfa479/Shaders/2D/Sprite-Lit-Default.shader`

---

## Sub-milestones (smallest blast radius first)

### 5A.1 — Custom URP 2D Sprite-Lit shader with sway + scroll uniforms

**Scope:** new `Assets/Shaders/AnimatedEnvironment.shader`.
- Forks `Sprite-Lit-Default` semantics (light receive, sprite color)
- Adds vertex offset based on per-instance world position + time
- Adds UV scroll for water glyphs
- Uses material properties:
  - `_SwayAmount` (float, 0-0.2) — vertex sway magnitude
  - `_SwayFrequency` (float, 1-5) — Hz
  - `_ScrollSpeedX` (float, -1 to 1) — UV.x drift
  - `_ScrollSpeedY` (float, -1 to 1) — UV.y drift
  - `_FlickerAmount` (float, 0-1) — color brightness wobble
- Uses keyword toggles:
  - `_ENABLE_SWAY` — sway on/off
  - `_ENABLE_SCROLL` — UV scroll on/off
  - `_ENABLE_FLICKER` — brightness wobble on/off

### 5A.2 — Three concrete materials (one per env type)

Three `.mat` assets configured for different effects:
- `AnimatedEnvironment_Water.mat` — `_ScrollSpeedX=0.3, _ScrollSpeedY=0.05`
- `AnimatedEnvironment_Grass.mat` — `_SwayAmount=0.08, _SwayFrequency=1.5`
- `AnimatedEnvironment_Fire.mat` — `_FlickerAmount=0.2,
  _ScrollSpeedY=0.5` (vertical flame drift)

### 5A.3 — `AnimatedEnvironmentRenderer` MonoBehaviour

New `Assets/Scripts/Presentation/Rendering/AnimatedEnvironmentRenderer.cs`.
- Scans the active zone every redraw
- For each cell with an animated glyph (`~`, `=`, `-` for water;
  `,` for grass; `*` for torch), paints that glyph onto the new
  `_animatedEnvTilemap` layer instead of the base `_tilemap`.
- Material assignment per glyph type uses a small lookup.

### 5A.4 — Showcase scenario `AnimatedEnvironmentShowcase`

`Assets/Scripts/Scenarios/Custom/AnimatedEnvironmentShowcase.cs`.
- Player at center
- Strip of grass tiles east (visible sway)
- Water pool south (visible scroll)
- Torch north (visible flicker on glyph + already-shipped Light2DFlicker)
- Smoke test in `ScenarioCustomSmokeTests`.

### 5A.5 — Tests + commit + merge + push

- Unit tests: shader parameter ranges + material assignment lookup
- Smoke test verifies showcase loads
- Play-mode visual verification via screenshots

---

## Performance honesty

| Cost | Per-frame |
|---|---|
| Custom shader | +1 vertex shader op per animated tile (cheap; vertex offset is 2 multiplications + sin) |
| New tilemap layer | +1 SetTile per animated cell (~30-100 cells × ~0.01ms each) |
| Material assignment | static, no per-frame cost |
| Total impact at 60fps | < 0.2ms expected |

If profiler shows >0.5ms, the cull path is to limit the animated
overlay to only the FOV (already computed for the main tilemap).

---

## Findings log

| # | Severity | Item | Status |
|---|---|---|---|
| 1 | 🟡 (caught + fixed) | Initial shader scrolled UV AFTER the atlas TRANSFORM_TEX, so `frac()` wrapped across the FULL CP437 sprite atlas instead of within the single glyph. Visible as cells cycling through unrelated glyphs (`}`, `~`, `(DEL)`, etc.) instead of the same glyph drifting. User reported "I see the glyphs in a pattern moving through the water" before the fix. Fix: move scroll to BEFORE the atlas-rect transform; vertex shader passes raw sprite-local UV; fragment shader scrolls in [0, 1] then maps to atlas. | Fixed in same pass. |

---

## Sub-milestone progress

| Sub-milestone | Status | Tests | Commit |
|---|---|---|---|
| 5A.1 Animated shader | ✅ done | 0 | TBD |
| 5A.2 Three materials | ✅ done | n/a | TBD |
| 5A.3 AnimatedEnvironmentRenderer | ✅ done | 8 | TBD |
| 5A.4 Showcase scenario | ✅ done | 1 (smoke) | TBD |
| 5A.5 Final tests + ship | ✅ done (51/51 sweep) | n/a | TBD |
| **TOTAL** | **5 / 5** | **9** | — |

---

## Out of scope (Pass 6+)

- Piskel-generated sprite frames (4-frame water cycle, 4-frame torch
  flame). Defer until v1 shader approach is verified visually.
- Per-biome animated environment palettes (cave drips vs jungle
  bioluminescence). Pass 7+.
- Lava bubble particles via VFX Graph. Pass 6.
- Cave drip + falling debris. Pass 7.
- Auto-tiled wall sprites. Pass 8 (separate large overhaul).

---

## Commit history

| Commit | Sub-milestone | Notes |
|---|---|---|
| TBD | 5.0 | Plan to disk |

---

*Updated as each sub-milestone ships.*
