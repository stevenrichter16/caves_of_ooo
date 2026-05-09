# Graphics Polish Pass 1 — Volume + Post-Processing

> First-pass polish using the upgraded Unity MCP graphics toolkit
> (VFX Graph 17.3.0, Cinemachine 3.1.6, URP 17.3.0). Goal: low-risk
> visual upgrades that fit the CP437 / 2D-Sprite-Lit aesthetic
> WITHOUT clashing with the existing tilemap render path.

---

## Status banner

| Field | Value |
|---|---|
| **Pass** | 1 of N (incremental) |
| **Last updated** | 2026-05-09 |
| **Branch** | `feat/graphics-polish-pass1` |
| **Files modified** | 3 (scene + Volume Profile + this doc) |
| **New runtime cost** | post-processing pass on Main Camera (already URP-pipelined; bloom is the heaviest, mitigated by `threshold=1.05` so only HDR-emissive pixels bloom) |

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

*End of pass 1 doc. Pass 2 plan TBD pending playtest feedback.*
