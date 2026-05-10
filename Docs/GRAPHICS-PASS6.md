# Graphics Pass 6 — Motion-ghost trails + Biome color grading

> **Living plan + progress doc.** Pass 5 added shader-driven environment
> animation (water scrolls, grass sways, fire flickers). Pass 6 adds:
>
>   - **6A** — Motion-ghost trails. Entity moves leave a 2-frame
>     fading ghost glyph at the previous cell. Per-step motion polish
>     that makes turn-based movement feel less "snap-snap-snap" and
>     more "fluid." ToME / Cogmind / DCSS all do versions of this.
>
>   - **6B** — BiomeColorPatcher. Runtime application of the
>     Pass 3 §3.C `BiomePalette` data layer (already shipped + tested
>     but never wired). Each zone-load reads the active biome,
>     patches the global Volume's ColorAdjustments + Vignette
>     overrides. Cave / Desert / Jungle / Ruins now look immediately
>     distinct.

---

## Status banner

| Field | Value |
|---|---|
| **Pass** | 6 of N |
| **Last updated** | 2026-05-09 |
| **Branch** | `feat/graphics-pass6-shadows-biome` |
| **Sub-milestones complete** | 0 / 4 |

---

## Verification sweep findings

### V1 — `LightMap` already has wall-shadow occlusion

`LightMap.HasLineOfSight` (line 170-196) does Bresenham line-cast
between light source and each candidate cell, vetoing the contribution
if any intermediate cell is `IsWall()`. So torches DO cast shadows
through walls already — this was originally planned for Pass 6 but
turned out to already exist. The reason caves look "evenly bright"
is the global `AmbientLevel = 0.4f` (line 25), which means even
shadowed cells are 40% lit. That's a separate tuning question, not
a missing feature.

**Implication:** Pass 6 SCRAPS the wall-shadow milestone. Two new
sub-milestones (motion ghosts + biome patcher) replace it.

### V2 — `Cell` has glyph + color storage but no "ghost" history

`ZoneRenderer.RenderCell` reads the top entity per cell each frame
and paints accordingly. There's no per-cell "last frame's glyph"
state. To add motion-ghost trails we need either:
  - (a) Per-cell ghost cache stored on `Cell` or in the renderer
  - (b) Per-entity "previous cell" tracking + dedicated ghost overlay

Going with (b) for blast radius minimization — entities already
have a "last cell" via `MovementSystem`, and a new tilemap layer
for ghost overlays is symmetric with Pass 5's animated-environment
overlays.

### V3 — `BiomePalette.GetForBiome(BiomeType)` exists but no caller

Verified in Pass 3 §3.C tests (14 GREEN). The data table is sitting
unused. `Zone.AmbientTint` already exists per zone (line 19-22 of
`Zone.cs`); biome detection happens in `OverworldZoneManager`. We
need to:
  - Listen for "active zone changed" event/property
  - On change, look up the biome
  - Apply the BiomePalette to the global Volume's effects

The volume's effects are sub-asset components on `CavesOfOoo_VolumeProfile.asset`.
We modify them at runtime via `volume.profile.TryGet<ColorAdjustments>(...)`.

---

## Sub-milestones

### 6A.1 — `GlyphGhostRenderer` MonoBehaviour

New `Assets/Scripts/Presentation/Rendering/GlyphGhostRenderer.cs`.
- Sits alongside ZoneRenderer
- Listens for entity-moved events (already fired by the existing
  movement system)
- Paints a faded copy of the moving entity's glyph at the previous
  cell on a NEW overlay tilemap (sortingOrder=2.5 — between
  AnimatedEnvironment 2 and FX 3)
- Each ghost has a 2-frame lifetime; per-frame it fades alpha
- Uses a custom shader (or the existing AnimatedEnvironment shader
  with flicker keyword) that applies an alpha multiplier based
  on per-tile ghost age

### 6A.2 — Wire entity-move events to GlyphGhostRenderer

Subscribe to whatever event the project fires when an entity moves
between cells. Likely `EntityMoved` or `BeforeMove`. Inject a hook
that records `(prevX, prevY, glyph, color, ghostFrameCount)` to
the ghost renderer.

### 6A.3 — Tests + showcase
- Unit test: `GlyphGhostRenderer_RegisterMove_StoresAtPrevCell`
- Counter: `GlyphGhostRenderer_TickPastLifetime_GhostExpires`
- Counter: `WithoutMove_NoGhostsRendered`
- Smoke: `MotionGhostShowcase` puts a hostile snapjaw or two adjacent
  to the player; their wandering produces visible trails.

### 6B.1 — `BiomeColorPatcher` MonoBehaviour

New `Assets/Scripts/Presentation/Rendering/BiomeColorPatcher.cs`.
- On Awake, finds the global Volume + caches its
  ColorAdjustments + Vignette overrides
- Per-frame (or via zone-change event), reads the active zone's
  biome via `OverworldZoneManager` or `Zone.BiomeType`
- Maps to BiomePalette via `BiomePalette.GetForBiome(biome)`
- Smoothly lerps the volume's `colorFilter`, `contrast`,
  `saturation`, `vignetteIntensity` toward the target over ~1 second
  (avoids hard color pop on zone load)

### 6B.2 — Tests + integration into GameBootstrap

- Unit test: `BiomeColorPatcher_OnBiomeChange_LerpsVolumeProperties`
- Counter: `BiomeColorPatcher_NoVolume_NoCrash`
- GameBootstrap adds the component alongside other Pass 4 components

---

## Performance honesty

| Sub-milestone | Per-frame cost | Mitigation |
|---|---|---|
| 6A motion ghosts | 1 SetTile per moved entity per frame; ghost decay scan over O(active ghosts) which is typically ≤ 10 | Fine — combat is turn-based, only the player + maybe a couple of NPCs move per turn |
| 6B biome patcher | 1 lerp + 4 property writes per frame | Fine — Volume property writes are trivial |

---

## Findings log

| # | Severity | Item | Status |
|---|---|---|---|
| 1 | 🔵 (caught at planning) | LightMap already has wall-shadow occlusion via Bresenham line-cast (`HasLineOfSight`, line 170-196). Original Pass 6 plan claimed this as a new feature; verification sweep proved it's already shipped. Pass 6 pivoted away from wall shadows. | Pivoted — no code change needed. |

---

## Sub-milestone progress

| Sub-milestone | Status | Tests | Commit |
|---|---|---|---|
| 6A.1 GlyphGhostRenderer | ⏳ pending | 0 | — |
| 6A.2 Wire move events | ⏳ pending | 0 | — |
| 6A.3 Tests + showcase | ⏳ pending | 4 | — |
| 6B.1 BiomeColorPatcher | ⏳ pending | 0 | — |
| 6B.2 Tests + bootstrap | ⏳ pending | 2 | — |
| **TOTAL** | **0 / 5** | **0** | — |

---

*Updated as each sub-milestone ships.*
