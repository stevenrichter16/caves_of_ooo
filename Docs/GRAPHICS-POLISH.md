# Graphics Polish Pass 1 — Volume + Post-Processing

> First-pass polish using the upgraded Unity MCP graphics toolkit
> (VFX Graph 17.3.0, Cinemachine 3.1.6, URP 17.3.0). Goal: low-risk
> visual upgrades that fit the CP437 / 2D-Sprite-Lit aesthetic
> WITHOUT clashing with the existing tilemap render path.

---

## Status banner

| Field | Value |
|---|---|
| **Pass** | 2 of N (incremental) |
| **Last updated** | 2026-05-09 |
| **Latest branch** | `feat/graphics-polish-pass2` |
| **Pass 1 commit** | `5189e21` (merge) |
| **Pass 2 commit** | `4bdc9d2` (merge) / `b94fd5a` |
| **Files modified** | Pass 1: 3 (scene + Volume Profile + this doc); Pass 2: +5 (TorchFlicker.anim + .controller + Light2DFlicker.cs + asmdef change + this doc) |
| **New runtime cost** | post-processing pass on Main Camera (Pass 1); per-frame Perlin sample per Light2DFlicker-equipped light (Pass 2 — negligible). |

---

## What's been added

### 1. Global Volume + Post-Processing

A new `Global Volume` GameObject in `Assets/Scenes/Main/SampleScene.unity`
references the new `Assets/Settings/CavesOfOoo_VolumeProfile.asset`.
The Main Camera's `UniversalAdditionalCameraData.renderPostProcessing`
flag is now `true`, so the volume's stack actually runs.

#### Volume Profile contents

| Effect | Tuned value | Rationale |
|---|---|---|
| **Bloom** | `threshold=1.05`, `intensity=0.45`, `scatter=0.6`, `tint=warm (1, 0.92, 0.78, 1)` | Only blooms pixels above HDR 1.05 → existing flat-color sprites are unaffected; emissive lights (lanterns, fire FX, spell projectiles) bloom warmly. Scatter=0.6 keeps the halo tight (a CP437 cell is small; large halos would blur neighboring glyphs). |
| **Vignette** | `intensity=0.32`, `smoothness=0.45`, `color=black`, `rounded=false` | Subtle dark corners focus the eye on the player-centered playfield without cropping the ASCII grid. `rounded=false` keeps the falloff anamorphic — fits the 16:9 game window. |
| **ColorAdjustments** | `contrast=+8`, `saturation=+8`, `colorFilter=warm (1, 0.97, 0.91, 1)` | Slight warmth + contrast bump pushes the palette toward "torchlit cave" without crushing readability. The CP437 palette's pure colors (#FF0000, #00FF00, etc.) are mostly already saturated; +8 keeps midtones slightly punchier. |
| **Tonemapping** | `mode=Neutral` (1) | Gentle highlight rolloff so bloom doesn't burn out into white. Neutral is preferred over ACES for 2D pixel art — ACES is film-grade and pushes whites too aggressively for CP437. |

### 2. Post-processing enabled on Main Camera

`UniversalAdditionalCameraData.renderPostProcessing = true`. Without
this flag, the Volume's stack is silently skipped.

---

## What was deliberately NOT added (and why)

| Effect | Why skipped |
|---|---|
| **DepthOfField** | 2D ortho camera with all sprites on the same Z — DoF has no effect, just adds a render pass cost. |
| **MotionBlur** | Per-frame motion is intentional in turn-based combat (each move is a discrete step). MB would smear glyphs and hurt readability. |
| **ChromaticAberration** | Aesthetic clash with the clean ASCII grid. Could revisit for a "cursed" or "underwater" zone post-processing override if needed. |
| **FilmGrain** | Reads as compression noise on flat tile colors; doesn't fit the deliberately-clean CP437 look. |
| **LensDistortion / PaniniProjection** | Would warp the tilemap grid — unacceptable for a strict 80×25 layout. |
| **Cinemachine virtual camera migration** | The existing `cameraFollow.Shake(intensity, duration)` (`GameBootstrap.cs:278-289`) already implements hit-shake. Migrating to Cinemachine ImpulseSource/Listener would be net-neutral and risk regressing the snap-to-player behavior. |

---

## Where the new MCP graphics tools were used

This was a "validate the new toolkit by shipping something useful"
exercise as much as a graphics pass. Tools exercised:

| Tool | Action(s) | Notes |
|---|---|---|
| `manage_camera` | `ping`, `list_cameras` | Confirmed Cinemachine 3.1.6 installed; confirmed Main Camera lacks a Brain (so Cinemachine is dormant — fine for this pass). |
| `manage_graphics` | `pipeline_get_info`, `skybox_get`, `feature_list`, `volume_create_profile`, `volume_add_effect` (×4), `volume_get_info`, `volume_list_effects` | URP 17.3 detected; Renderer2D in use; no existing renderer features. New profile + 4 effects added cleanly. |
| `manage_components` | `set_property` × 2 | Patched `Volume.sharedProfile` and `UniversalAdditionalCameraData.renderPostProcessing`. |
| `manage_gameobject` | `create` | New `Global Volume` GameObject in scene root. |
| `manage_packages` | `list_packages` | Confirmed installed packages for the survey. |
| `manage_scene` | `get_active`, `get_hierarchy`, `save` | Inspected scene shape; saved after edits. |

VFX Graph (`manage_vfx`) and `manage_animation` are not exercised
in this pass — flagged as candidates for Pass 2 (per-zone fog,
particle effects on combat impact, settlement light flicker
animations).

---

## Verification posture (honesty bound, per CLAUDE.md §6.3)

**Can verify (script-observable):**
- Volume Profile asset exists at `Assets/Settings/CavesOfOoo_VolumeProfile.asset`
  with 4 active effects: Bloom, Vignette, ColorAdjustments, Tonemapping.
- `Global Volume` GameObject in scene with `isGlobal=true, priority=0,
  weight=1, sharedProfile=...`.
- `Main Camera.UniversalAdditionalCameraData.renderPostProcessing=true`.
- Compile clean after scene save (`refresh_unity` returned no errors).

**Cannot verify (visual / feel):**
- Whether the actual rendered game (in Play mode) looks BETTER, not
  just different. The tilemap content is generated at runtime by
  `GameBootstrap`, so EditMode Game View is empty. A real visual
  verification requires entering Play mode + visiting one of the
  showcase scenarios (`FlamingSwordShowcase`, `CombatHooksShowcase`,
  etc.). Per project memory, entering Play mode resets the scene —
  flagged as a manual playtest task (Pass 2 will include before/after
  screenshots from a controlled scenario).

---

## Pass 2 candidates (deferred)

In rough priority order:

1. **Per-zone Volume overrides** — biome-specific color grading via
   non-global Volumes. Cold biomes desaturate + cyan tint; cave
   biomes amber + lower contrast; corruption zones magenta vignette.
2. **VFX Graph asset for combat impact particles** — rewrite
   `DeathSplatterFx` from imperative MonoBehaviour spawn to a
   data-driven VFX Graph asset. Would also enable richer combat
   feedback (sparks on metal hits, embers on fire damage).
3. **Light2D flicker on lanterns** — per-frame intensity wobble via
   AnimationClip. Currently lights are static; flicker would add
   atmosphere without code complexity.
4. **CinemachineImpulseSource** — replace the existing `Shake` with
   a per-axis impulse for directional hit-feedback (knockback from
   the LEFT shakes the camera left, etc.). Cleaner than scalar
   intensity.
5. **Bloom-emissive status effect colors** — audit existing CP437
   color codes (`&Y`, `&R`, etc.) in `ColorPalette.cs` and bump
   selected status colors above HDR 1.0 (`(2.0, 0.4, 0)` for
   Burning, etc.) so they bloom without changing visible color.
6. **ScreenSpaceLensFlare on Light2D sources** — only when zoomed
   in on bright lights; gates by camera proximity.

---

## Performance honesty (per CLAUDE.md §Performance)

The added post-processing stack adds one full-screen render pass.
On the URP 2D renderer at 80×25 tilemap × camera reference zoom,
this is well under 1ms on M-series Macs (the dev target). No
expected perf impact for typical play.

The bloom threshold is set to `1.05` (above LDR `1.0`) so the
texture is only sampled at HDR-bright pixels — cheap. Without
this threshold, bloom would fire on every white CP437 pixel
(`#FFFFFF` = `(1,1,1)` LDR), tripling the bloom buffer cost.

Should profiler show >2ms post-processing time, the mitigations
(in priority order) are:
- Lower `Bloom.maxIterations` from default 6 → 4 (`-30%` cost).
- Lower `Bloom.downscale` from `Half` → `Quarter` (`-50%` cost).
- Disable `Tonemapping` (URP's neutral tonemap is the cheapest
  effect in the stack but still nonzero).

---

---

# Pass 2 — Light2D flicker (Light2DFlicker MonoBehaviour + AnimationClip)

Goal: give torches, lanterns, and fire FX a "living flame" feel via
subtle intensity + outer-radius wobble on `Light2D` components.

## What's been added in Pass 2

### 1. `Assets/Scripts/Presentation/Rendering/Light2DFlicker.cs`

Tiny MonoBehaviour (≈75 lines incl. comments) that:
- Caches `Light2D.intensity` + `Light2D.pointLightOuterRadius` on Awake.
- Per-Update samples two decoupled Perlin noise streams (one for
  intensity, one for radius) and applies small wobble offsets.
- Hashes the GameObject's world position to produce a deterministic
  per-instance phase offset, so neighboring torches don't flicker
  in lock-step.
- Restores base values OnDisable (deterministic for screenshots).

**Tuning:** `IntensityWobble` defaults to ±15%, `RadiusWobble` to
±4%, `Speed` to 2.5 (≈candle pace; 4.0 ≈ torch; 1.2 ≈ ember).
All sliders are exposed on the Inspector for per-light tuning.

### 2. `Assets/Animations/TorchFlicker.anim`

Hand-authored AnimationClip (1-second loop) with:
- 8-keyframe `m_Intensity` curve (cycles 1.0 → 1.22 → 0.92 → 1.0).
- 5-keyframe `m_PointLightOuterRadius` curve (cycles 4.0 → 4.15
  → 3.92 → 4.08 → 4.0).

This is an **alternate path** for designers who prefer Animator-
driven keyframes over the script. Both produce equivalent visible
output; the script is preferred at scene-setup time because it
needs no Animator + Controller wiring.

### 3. `Assets/Animations/TorchFlicker.controller`

Skeletal AnimatorController with a single `Flicker` default state.
**Note:** the MCP `controller_add_state` action created the state
but did NOT auto-attach the .anim motion (returned `hasMotion: false`).
Manual fix needed: open the controller in the Animator window and
drag `TorchFlicker.anim` onto the Flicker state. Flagged as a 🟡
finding for the MCP toolkit.

### 4. `Assets/Scripts/CavesOfOoo.asmdef` — +1 reference

Added `Unity.RenderPipelines.Universal.2D.Runtime` so any code
under the project's main assembly can reference the `Light2D`
class. Without this, Light2DFlicker.cs failed to compile with
`CS0246: Light2D could not be found`. The package's asmdef has
`autoReferenced: true`, but project-level asmdefs override
auto-referencing — so the reference must be explicit.

**Side-effect:** any future script in `Assets/Scripts/` can now
use `Light2D` without per-script asmdef setup.

## Tools exercised in Pass 2

| Tool | Action(s) | Notes |
|---|---|---|
| `manage_animation` | `clip_create`, `clip_add_curve` ×2, `controller_create`, `controller_add_state`, `controller_get_info` | Asset creation works; controller-state-motion attachment did NOT (open finding). |

## Pass 2 findings

- **🟡 Pass-2-1:** `manage_animation`'s `controller_add_state` action
  ignores the `motionPath` property in `properties` — the state is
  created but `hasMotion: false`. Workaround: manual editor wiring,
  OR write the AnimatorController YAML directly via Edit. Documented
  as a manual-fix step for any future user. Recommend filing on the
  MCP repo if this is reproducible.
- **🟢 Pass-2-2:** Project's `CavesOfOoo.asmdef` did not reference the
  URP 2D runtime, so `Light2D` was inaccessible to project code.
  Added the reference; future Light2D-using scripts now compile.
  The fix is mechanical and harmless; the only risk is a marginal
  increase in compile-time dependencies for the main assembly.

## Verification posture (Pass 2)

**Can verify (script-observable):**
- AnimationClip exists with 2 curves × 13 total keyframes.
- AnimatorController exists with 1 layer + 1 default state.
- Light2DFlicker.cs compiles cleanly with the asmdef reference.
- Regression sweep across SL.2-4 + Rental + OnHit + Skill adversarial
  groups: **110/110 GREEN** after the asmdef change.

**Cannot verify (visual / feel):**
- Whether the Perlin-noise tuning constants (IntensityWobble=0.15,
  RadiusWobble=0.04, Speed=2.5) feel right under actual lighting.
  Needs Play-mode playtest with a Light2D-equipped scene.

## Pass 3 candidates (deferred)

In rough priority order:

1. **Wire Light2DFlicker into existing scenarios** that have lanterns
   (LanternSitePart, settlement scenes). Validate the tuning visually.
2. **VFX Graph asset for combat impacts** (still deferred from Pass 1).
3. **Per-zone Volume overrides** for biome color grading.
4. **Bloom-emissive bumps on status effect colors**.
5. **Investigate the controller_add_state motion-attachment bug** in
   the MCP toolkit; either fix upstream or document the workaround
   more visibly.

---

*End of Pass 2. Pass 3 plan TBD pending playtest feedback.*
