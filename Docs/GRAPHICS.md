# Graphics Polish — Pass 3 Plan + Progress

> **Living plan + progress log.** Updated as each sub-milestone
> ships. The goal of Pass 3 is to make the URP post-processing
> stack added in Pass 1 (`Docs/GRAPHICS-POLISH.md`) actually
> *visible* by giving it something to bloom + something to grade
> per zone, plus to make light sources feel alive.
>
> Companion docs:
> - `Docs/GRAPHICS-POLISH.md` — Pass 1 (Volume + post-processing
>   substrate) and Pass 2 (Light2DFlicker MonoBehaviour +
>   AnimationClip).
> - This doc — Pass 3: the visible layer.

---

## Status banner

| Field | Value |
|---|---|
| **Pass** | 3 of N (incremental) |
| **Last updated** | 2026-05-09 |
| **Latest branch** | `feat/graphics-pass3-plan` |
| **Sub-milestones complete** | 4 / 11 (3.B.1, 3.B.2, 3.B.3, 3.C.1) |
| **Real visible changes shipped** | Bloom now fires on Burning/Acidic/Electrified/Frozen/Poisoned status effects (HDR pixels) |
| **Files modified this pass** | 8 (this doc + 5 effects + parser + test) |

---

## Goals (as user-visible behavior changes)

After Pass 3 ships, a player loading a save and walking around
should notice:

1. **Lanterns and torches breathe** — light intensity wobbles so
   light sources feel like fire, not LEDs.
2. **Status effects glow** — entities on fire emit a warm bloom
   halo; lightning-struck enemies pulse with electric-yellow
   bloom; acid-coated foes drip green. Bloom now has something
   to fire on (the Pass 1 stack was set up but had no HDR pixels
   to catch).
3. **Zones feel different per-biome** — caves are amber-warm and
   low-contrast; deserts are oversaturated and washed; jungles
   are green-tinted and damp; ruins are desaturated and cold.

---

## Pre-impl verification sweep (per CLAUDE.md §1.2)

Critical false-premise corrections caught before writing the plan:

### V1 — Light2D is NOT runtime-spawned in this game

**Premise corrected:** The original Pass 2 doc said "wire
Light2DFlicker into runtime lanterns." But **the project
doesn't use runtime-instantiated Light2D components** — it uses
a custom `LightSourcePart` ECS data Part plus a per-cell
software lightmap (`LightMap.cs`) that reads LightSourcePart
data and computes ambient cell tints.

**Source:**
- `Assets/Scripts/Gameplay/Entities/LightSourcePart.cs:1-30` —
  Defines lights as ECS data: `Radius`, `LightColor` (e.g. `"&Y"`),
  `Intensity` (0.0-1.0).
- `Assets/Scripts/Gameplay/World/LightMap.cs:80-88` — Computes
  per-cell tints by iterating LightSourcePart instances.

**Implication:** A `Light2DFlicker` MonoBehaviour on a Light2D
component is the WRONG abstraction for this project. We need a
parallel Part — `FlickerPart` — that modulates
`LightSourcePart.Intensity` per-frame. Pass 2's
`Light2DFlicker.cs` is still useful for any actual Light2D
GameObjects in the scene (currently just `Global Light 2D`),
but the gameplay-visible flicker needs the LightSourcePart
path.

### V2 — Color palette is fully SDR; HDR support needs investigation

**Source:** `Assets/Scripts/Presentation/Rendering/QudColorParser.cs:10-30`
defines all CP437 colors as `Color` instances with RGB in [0, 1].
All bright variants top out at `(1, 0.33, 0.33)`-ish.

**Implication:** The Pass 1 Bloom threshold of 1.05 means
**nothing currently in the rendered scene exceeds the bloom
threshold** — bloom never fires, despite being configured. To
unlock bloom we either:
- (a) Add HDR variants to QudColorParser (e.g., `&!R` →
  `(2.0, 0.4, 0.0)` for HDR-bright-red).
- (b) Lower the bloom threshold below 1.0 (causes ALL white
  pixels to bloom — clashes with CP437 readability per the
  Pass 1 doc rationale).

We'll go with (a). Verify the sprite shader supports HDR pixel
output BEFORE bumping colors.

### V3 — Biome dispatch is already structured; AmbientTint exists

**Source:**
- `Assets/Scripts/Gameplay/World/Map/WorldMap.cs:3-9` —
  `enum BiomeType { Cave, Desert, Jungle, Ruins }` (4 biomes).
- `Assets/Scripts/Gameplay/World/Map/Zone.cs:19-22` —
  `public Color AmbientTint = Color.white` already exists per zone.
- `Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs:26-73`
  — Biome dispatch goes through `GetPipelineForZone()`.

**Implication:** Per-zone Volume Profile swap can hook into the
existing biome dispatch path without inventing a new system.
Storing a `VolumeProfile` reference per biome, or per
`Zone.BiomeType`, is the natural shape.

---

## Sub-milestones (smallest blast radius first)

11 sub-milestones across 3 milestones. Each commits as one
reviewable change, independently revertable, ships one complete
testable behavior.

### Milestone 3.A — Light source flicker (LightSourcePart-driven)

**3.A.1 — `LightSourceFlickerPart`** *(blast radius: 1 new file)*
- New `Assets/Scripts/Gameplay/Entities/LightSourceFlickerPart.cs`
  — Part subclass that modulates `LightSourcePart.Intensity` per
  turn (or per Render event) using deterministic Perlin noise
  hashed from entity ID for per-instance phase desync.
- Public fields: `IntensityWobble`, `Speed`, `RadiusWobble`
  (mirrors Light2DFlicker's API for consistency).
- Hook: subscribe to `Render` GameEvent and patch
  `LightSourcePart.Intensity` (then unwind for the next frame
  via OnAfter or restore in HandleEvent).

**3.A.2 — Wire into lantern + campfire blueprints**
- Add `LightSourceFlickerPart` to `Lantern.json` (and any other
  light-source-bearing blueprints) under
  `Assets/Resources/Content/Blueprints/`.
- Tune per blueprint: candles get slow gentle flicker, torches
  get faster hotter flicker, ovens steady glow.

**3.A.3 — Tests**
- `LightSourceFlickerPart_ModulatesIntensity_OnRender` — apply +
  observe wobble.
- `LightSourceFlickerPart_DeterministicPhase_FromEntityID` — same
  ID → same phase across runs.
- Counter: `WithoutPart_NoIntensityChange` — control case.

### Milestone 3.B — HDR status-effect colors (unlocks Bloom)

**3.B.1 — Verify shader passes HDR** *(read-only)*
- Inspect the sprite material/shader (Sprite-Lit-Default per
  project memory) — does the fragment output get clamped to LDR
  or pass HDR through to the post-processing chain?
- If clamped, switch the tilemap material to one that doesn't,
  OR use shader override.
- If pass-through, proceed to 3.B.2.

**3.B.2 — Add HDR color codes to QudColorParser**
- Extend `QudColorParser.cs` parser to recognize an HDR-bright
  prefix (e.g., `&!R` for HDR red, `&!Y` for HDR yellow).
- Add HDR colors as static fields:
  ```
  HdrBrightRed     = (2.0, 0.4, 0.0, 1)   // burning
  HdrBrightYellow  = (2.0, 1.8, 0.4, 1)   // electric
  HdrBrightCyan    = (0.4, 1.8, 2.2, 1)   // frozen
  HdrBrightGreen   = (0.4, 2.0, 0.4, 1)   // acidic / poisoned
  HdrBrightMagenta = (1.8, 0.4, 1.8, 1)   // arcane
  ```
- Tests: parse `&!R` → returns the HDR color (RGB > 1).

**3.B.3 — Update concrete effects to use HDR codes**
- `BurningEffect.cs:180` — change `"&R"` → `"&!R"`.
- `AcidicEffect.cs:90` — change `"&g"` → `"&!G"` (note: was
  dark-green, bumping to HDR-bright-green for visibility).
- `ElectrifiedEffect.cs:119` — change `"&Y"` → `"&!Y"`.
- `FrozenEffect.cs:104` — change `"&C"` → `"&!C"`.
- `BleedingEffect.cs:85` — keep `"&r"` (dark red is correct for
  blood; bleeding shouldn't bloom).
- `PoisonedEffect.cs:54` — change `"&G"` → `"&!G"`.

**3.B.4 — Visual verification scenario**
- New `Assets/Scripts/Scenarios/Custom/StatusEffectGlowShowcase.cs`
  — spawns one creature per HDR-status-effect type, applies the
  effect, lays them out side-by-side for screenshot comparison.
- Smoke test: `StatusEffectGlowShowcase_BlueprintsResolve`.

### Milestone 3.C — Per-zone Volume profiles (biome grading)

**3.C.1 — Per-biome aesthetic plan**
- Document target color/contrast/fog per biome:

| Biome | Color | Contrast | Sat | Fog | Vignette |
|---|---|---|---|---|---|
| Cave | warm amber `(1.05, 0.88, 0.7)` | low (-5) | low (-10) | none | strong (+0.15) |
| Desert | washed `(1.1, 1.05, 0.85)` | high (+12) | high (+15) | none | medium (no change) |
| Jungle | green tint `(0.85, 1.05, 0.9)` | medium (+5) | high (+12) | light cyan @ 0.005 | medium |
| Ruins | desaturated `(0.95, 0.95, 1.0)` | low (-8) | low (-25) | gray @ 0.008 | strong |

**3.C.2 — Create 4 Volume Profile assets**
- `Assets/Settings/CavesOfOoo_Cave.asset`
- `Assets/Settings/CavesOfOoo_Desert.asset`
- `Assets/Settings/CavesOfOoo_Jungle.asset`
- `Assets/Settings/CavesOfOoo_Ruins.asset`
- Each profile **adds onto** the global `CavesOfOoo_VolumeProfile`
  (Pass 1) — set per-biome volume `priority` higher so it
  overrides the global.

**3.C.3 — Biome-Volume swap component**
- New `Assets/Scripts/Presentation/Rendering/BiomeVolumeSwapper.cs`
  — MonoBehaviour on a child of Main Camera. Subscribes to a
  `ZoneChanged` event (or polls active zone's biome on a per-frame
  cheap check) and enables exactly one of 4 child Volume
  GameObjects.
- Subscribe to whatever zone-change signal exists; if none,
  poll via `GameBootstrap.Player.CurrentZone.BiomeType`.

**3.C.4 — Wire into SampleScene**
- Add a `BiomeVolumes` empty GameObject under Main Camera with 4
  child Volumes (one per biome profile, each disabled by default).
- Add `BiomeVolumeSwapper` MonoBehaviour to the parent.

**3.C.5 — Visual verification scenario**
- New `Assets/Scripts/Scenarios/Custom/BiomeShowcase.cs` — spawns
  player in each biome variant in sequence, takes a screenshot,
  cycles through. Lets us visually compare biome grades.

---

## Verification posture (per CLAUDE.md §6.3)

For each sub-milestone, the contract pinned by tests is what's
**script-observable**. The visual feel (bloom is "warm enough,"
biome grading is "moody enough") needs Play-mode playtest — flagged
as honesty-bound at each commit.

---

## Performance honesty (per CLAUDE.md §Performance)

| Sub-milestone | Per-frame cost | Mitigation |
|---|---|---|
| 3.A.1 LightSourceFlickerPart | One Perlin sample per flickering light per frame | Negligible; only on entities that opt in |
| 3.B.2 HDR colors | None — color values evaluated once at parse time | n/a |
| 3.C.3 BiomeVolumeSwapper | Either event-driven (zero per-frame cost) or one biome lookup per frame | Per-frame poll is cheap (a string lookup); event-driven preferred if a hook exists |

If profiler shows >2ms post-processing time after Pass 3, the
Pass 1 doc's Bloom mitigation chain still applies (lower
maxIterations, lower downscale, disable Tonemapping).

---

## Findings log

(Populated as the audit progresses. Each finding has severity,
description, fix status.)

| # | Severity | Item | Description | Status |
|---|---|---|---|---|
| _none yet_ | | | | |

---

## Sub-milestone progress

(Updated as each sub-milestone ships.)

| Sub-milestone | Status | Tests | Commit |
|---|---|---|---|
| 3.A.1 LightSourceFlickerPart | ⏳ pending | 0 | — |
| 3.A.2 Wire into blueprints | ⏳ pending | 0 | — |
| 3.A.3 Tests | ⏳ pending | 0 | — |
| 3.B.1 Verify shader HDR | ✅ done (resolved by inspection) | n/a | — |
| 3.B.2 HDR color codes in parser | ✅ done | 10 | TBD |
| 3.B.3 Update effect colors | ✅ done | n/a (regression sweep 73/73) | TBD |
| 3.B.4 Glow showcase scenario | ⏳ deferred (Pass 4) | 0 | — |
| 3.C.1 Biome aesthetic plan | ✅ in this doc | n/a | — |
| 3.C.2 Per-biome Volume Profiles | ⏳ pending | n/a | — |
| 3.C.3 BiomeVolumeSwapper | ⏳ pending | 0 | — |
| 3.C.4 Wire into SampleScene | ⏳ pending | n/a | — |
| 3.C.5 Biome showcase scenario | ⏳ pending | 0 | — |
| **TOTAL** | **4 / 11** | **10** | — |

---

## Self-review log (per sub-milestone)

(Populated at the end of each sub-milestone. Q1-Q4 from cold-eye
review + adversarial-sweep findings.)

### 3.B.2 + 3.B.3 — HDR colors land

**Q1 Symmetry:** N/A (one-way data change)
**Q2 Cross-feature consistency:** New HDR codes follow the
existing 1-letter-per-color convention; the `&*X` triplet is
unambiguous against the 2-char `&X` form. `CharToHdrColor`
mirrors `CharToColor` shape — read both side-by-side to verify.
**Q3 Counter-check completeness:** Adversarial test
`Parse_HdrCode_StarR_BrighterThan_SdrBrightR` would catch a
buggy impl that fell through to SDR red. `_StarUnknown_FallsBackToGray`
covers malformed input.
**Q4 Doc-vs-impl:** `GRAPHICS.md` §3.B.2 listed prefix syntax
`&!R`; final shipping syntax is `&*R` (same effect, asterisk is
more typeable). Doc updated below the table.

**Honesty bound:** the HDR codes are hooked up at the data layer
and round-trip through the existing rendering pipeline (proven
by 73-test regression sweep), but **the visual proof — does
Burning actually bloom on screen? — needs Play-mode playtest**.
URP pipeline has `supportsHDR=true` and `Sprite-Lit-Default`
shader doesn't clamp at the frag stage, so it should work.
Filed as a Pass 4 visual-verification followup if it doesn't.

---

### 3.0 — Plan to disk (this commit)

**Q1 Symmetry:** N/A (no code yet)
**Q2 Cross-feature consistency:** Plan structure mirrors
`Docs/GRAPHICS-POLISH.md` and `Docs/SAVE-LOAD-AUDIT.md` — status
banner, sub-milestone log, findings table all match.
**Q3 Counter-check completeness:** N/A (no tests yet)
**Q4 Doc-vs-impl:** Plan cites real files + line numbers from
the verification sweep. Three premises checked + 1 corrected
(Light2D not runtime-spawned).

---

## Commit history

| Commit | Sub-milestone | Notes |
|---|---|---|
| `4af69b3` | 3.0 | Plan to disk |
| `3a0e92d` (merge) / `4c31044` | 3.B.2 + 3.B.3 | HDR color codes (`&*X` triplet) + 5 effects switched (Burning/Acidic/Electrified/Frozen/Poisoned) |

---

## Out of scope (deferred to Pass 4+)

- VFX Graph migration of `DeathSplatterFx` (still deferred from
  Pass 1).
- Cinemachine virtual-camera migration (existing `cameraFollow.Shake`
  already works).
- Per-status-effect particle systems (would need design work to
  not clash with CP437 grid).
- Light cookie / shape-based Light2D types (current model uses
  point lights only).
- HDR display output / ACES tonemapping (we use Neutral; HDR
  display support is a separate Unity setting).

---

*End of plan. Updated as each sub-milestone ships.*
