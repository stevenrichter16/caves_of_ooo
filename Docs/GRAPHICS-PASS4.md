# Graphics Polish — Pass 4 Plan + Progress

> **Living plan + progress doc** for the "large visual overhaul"
> pass. Where Pass 1-3 was *substrate* (post-processing volume,
> HDR colors, light flicker, biome palettes), Pass 4 is the
> **moments**. Combat impacts, atmosphere overlay, and dynamic
> light occlusion. Targets are the items rated ⭐⭐⭐⭐⭐ from
> the brainstorm.
>
> Companion docs:
> - `Docs/GRAPHICS-POLISH.md` — Pass 1 (Volume substrate) + Pass 2
>   (Light2DFlicker MonoBehaviour).
> - `Docs/GRAPHICS.md` — Pass 3 (HDR colors + LightSourceFlickerPart
>   wiring + BiomePalette).
> - **This doc** — Pass 4 (HitStop + CRT phosphor + wall shadow casters).

---

## Status banner

| Field | Value |
|---|---|
| **Pass** | 4 of N |
| **Last updated** | 2026-05-09 |
| **Branch** | `feat/graphics-pass4-overhaul` |
| **Sub-milestones complete** | 0 / 8 |
| **Real visible changes shipped** | 0 (yet) |

---

## Goals (player-visible)

1. **Combat hits feel like they connect** — crits, dismembers, and
   kills produce a brief screen freeze + camera punch + shake.
   Hyper Light Drifter's hit-stop made small attacks feel huge;
   the same trick fits a turn-based roguelike just as well.
2. **The screen feels like a CRT** — toggleable phosphor overlay
   (subtle scanlines + slight curvature + bloom on bright pixels).
   Massively shifts the aesthetic from "Unity 2D" to "1990s ASCII
   ROM" without requiring any per-cell content changes.
3. **Walls block torchlight** *(stretch — may defer to Pass 5)* —
   URP 2D ShadowCaster2D on solid tiles. Lanterns now cast
   actual cone-of-light shadows; corners pull the eye dramatically.

---

## Pre-impl verification sweep

### V1 — `Time.timeScale` works in Unity turn-based games

**Confirmed:** No coroutines depend on real-time deltas in critical
gameplay paths. The `cameraFollow.Shake` (`CameraFollow.cs:209-228`)
uses `Time.deltaTime` which scales with timeScale — so when we
freeze, shake naturally pauses too (which is the CORRECT hit-stop
behavior: freeze the moment, then resume).

**Implication:** `Time.timeScale = 0` for ~6 frames is safe; we
just need a coroutine driver tied to `WaitForSecondsRealtime` so
the unfreeze fires reliably.

### V2 — Hook points in `CombatSystem.PerformSingleAttack`

**Confirmed (`CombatSystem.cs:285-330`):**
- `naturalTwenty` flag at the swing — used for the `_CRITICAL_HIT_TAG`
  message decoration. Available at the same scope where hit-stop
  needs to fire.
- `if (hpAfter > 0)` block at line 317 — the survivor branch.
- `HandleDeath` is called inside `ApplyDamage` for lethal blows.
- `damage.HasAttribute("Critical")` is set on nat-20s and consumed
  by the `WeaponMadeCriticalHit` skill hook (line 346).

**Three trigger tiers planned:**
- **Light hit-stop** (~80ms) — every melee hit. Maybe too much; tune.
- **Medium hit-stop** (~150ms) — crits.
- **Heavy hit-stop** (~250ms) — kills, dismembers.

### V3 — URP custom renderer features for CRT shader

**Confirmed (Pass 1 doc):** project's URP renderer is `Renderer2D`
with 0 features. The 2D renderer supports renderer features via
`feature_add` action of `manage_graphics`. CRT shader can ride
into the post-processing chain via either:
- A custom `ScriptableRendererFeature` registered on Renderer2D, OR
- A simpler approach: layered Volume effects (`LensDistortion` +
  `FilmGrain` + `Vignette` boost) approximating CRT without a
  custom shader.

**Decision:** Start with the simpler Volume-only approach (4B.1).
If the look isn't convincing, add a custom shader feature in 4B.3
(deferred sub-milestone).

### V4 — URP 2D ShadowCaster2D on tilemap-rendered walls

**Risk identified:** Tilemaps don't natively support per-tile
ShadowCaster2D. The shipping pattern is to add a separate
ShadowCaster2D GameObject per wall, OR use a `CompositeShadowCaster2D`
on the tilemap parent.

**Implication:** Wall shadows (4C) is the riskiest sub-milestone.
Marked as stretch — may defer to Pass 5 if time tight.

---

## Sub-milestones (smallest blast radius first)

8 sub-milestones across 3 milestones.

### Milestone 4A — HitStop (combat impact frames)

**4A.1 — `HitStopController` MonoBehaviour** *(blast radius: 1 file)*
- New `Assets/Scripts/Presentation/Effects/HitStopController.cs`.
- Public static `Instance` for global access (or a singleton
  hook in `GameBootstrap`).
- `Punch(float durationMs, float intensity)` API:
  - Sets `Time.timeScale = 0` for the duration.
  - Optionally drives a brief camera-zoom punch.
  - Drives the existing `cameraFollow.Shake` alongside.
- Coroutine uses `WaitForSecondsRealtime` so it unfreezes
  even with timeScale=0.

**4A.2 — Wire into combat events**
- Hook into `CombatSystem.PerformSingleAttack`:
  - Light (~80ms) on every successful hit (probably too much —
    leave commented out, default off).
  - Medium (~150ms) on `naturalTwenty == true`.
  - Heavy (~250ms) on `hpAfter <= 0` (kill).
- Hook into `CheckCombatDismemberment` for dismember-stop.

**4A.3 — Tests**
- `HitStopController_Punch_FreezesTimeScale_ForDuration`.
- `HitStopController_Punch_RestoresTimeScaleAfterDuration`.
- `HitStopController_NestedPunches_ExtendsDuration`.
- Counter: `Without_Punch_TimeScaleStaysAt_1`.

### Milestone 4B — CRT phosphor overlay

**4B.1 — Volume Profile + Volume effects (no custom shader yet)**
- New `Assets/Settings/CavesOfOoo_CrtVolume.asset`.
- Effects:
  - `Vignette` boosted (`intensity: 0.55, smoothness: 0.7,
    rounded: true`) — round vignette = CRT bezel feel.
  - `LensDistortion` (`intensity: 0.05, scale: 1.02`) — subtle
    barrel distortion = CRT curvature.
  - `FilmGrain` (`intensityMultiplier: 0.3, response: 0.8`) —
    phosphor noise.
  - `ChromaticAberration` (`intensity: 0.1`) — subtle RGB
    fringe at corners (Pass 1 explicitly skipped this; for CRT
    it fits).
- New scene Volume "CRT Volume" with `priority: 1` (above
  global). Default-disabled.

**4B.2 — Toggle hotkey + persistence**
- New `Assets/Scripts/Presentation/Effects/CrtToggleController.cs`.
- Hotkey: `F12` (configurable). Toggles the CRT Volume
  GameObject's active state.
- Persists state via `PlayerPrefs` (no save-file plumbing
  needed — display preference, not gameplay state).

**4B.3 — Custom CRT shader (deferred to Pass 5)** ⏳
- Real scanlines + per-pixel phosphor mask need a fragment shader.
- Not strictly necessary; the 4B.1+2 Volume stack already gets
  ~70% of the way there.

### Milestone 4C — URP 2D wall shadow casters *(STRETCH)*

**4C.1 — `WallShadowCasterApplier` runtime component**
- Scans newly-loaded zones for cells tagged `Solid` (walls,
  closed doors, large boulders).
- For each, instantiates a `GameObject` with a `ShadowCaster2D`
  component sized to the cell.
- Parented under a `WallShadowsRoot` empty GameObject for
  cleanup on zone unload.

**4C.2 — Composite shadow caster on tilemap parent**
- Add `CompositeShadowCaster2D` to the ZoneGrid GameObject so
  Light2D batches the shadow geometry efficiently.

**4C.3 — Tests + visual showcase**
- `WallShadowCasterApplier_AddsCastersForSolidCells`.
- `WallShadowCasterApplier_DoesNotAddForFloors`.
- `WallShadowCasterApplier_OnZoneUnload_CleansUpCasters`.
- Showcase scenario `WallShadowsShowcase`: small walled room +
  central torch. Player sees light cone hitting walls and
  shadowing the corners.

---

## Verification posture

For each sub-milestone:
- **Can verify (script-observable):** unit + integration tests
  pinning the data + behavior contracts.
- **Cannot verify (visual):** all three milestones add visible
  screen effects. Final visual feel needs Play-mode playtest.
  Showcase scenarios where applicable.

Honesty bound: Pass 3 wiring tests proved the data + event flow
were correct, but the user reported they didn't see the effects.
That means the runtime render-pipeline preserves SOME values but
maybe clamps HDR to LDR. The HitStop work doesn't depend on HDR
at all, so it's safer. The CRT overlay uses standard URP volume
effects (LensDistortion, FilmGrain, Vignette) which definitely
work in LDR — so visual change is guaranteed there.

---

## Performance honesty

| Sub-milestone | Per-frame cost | Mitigation |
|---|---|---|
| 4A HitStop | ~0 — only fires on combat events | n/a |
| 4B CRT Volume | +0.3-0.6ms (4 extra post-fx) | `LensDistortion` + `ChromaticAberration` are the heaviest; can disable individually if budget tight. Toggleable. |
| 4C Wall shadows | Per-zone-load: O(n) scan; per-frame: URP 2D batches casters | `CompositeShadowCaster2D` keeps draw calls flat. Only cells with `Solid` tag get casters. |

---

## Findings log

(Populated as the audit progresses.)

| # | Severity | Item | Description | Status |
|---|---|---|---|---|
| _none yet_ | | | | |

---

## Sub-milestone progress

| Sub-milestone | Status | Tests | Commit |
|---|---|---|---|
| 4A.1 HitStopController | ⏳ pending | 0 | — |
| 4A.2 Wire into combat | ⏳ pending | 0 | — |
| 4A.3 Tests | ⏳ pending | 0 | — |
| 4B.1 CRT Volume Profile | ⏳ pending | n/a | — |
| 4B.2 CRT toggle hotkey | ⏳ pending | 0 | — |
| 4C.1 WallShadowCasterApplier | ⏳ stretch | 0 | — |
| 4C.2 Composite shadow caster | ⏳ stretch | n/a | — |
| 4C.3 Tests + showcase | ⏳ stretch | 0 | — |
| **TOTAL** | **0 / 8** | **0** | — |

---

## Self-review log

### 4.0 — Plan to disk (this commit)

**Q1 Symmetry:** N/A (no code yet)
**Q2 Cross-feature consistency:** Plan structure mirrors the
Pass 3 doc's status-banner / sub-milestone-table / findings-log
shape. New addition: V1-V4 verification sweep section explicitly
called out before sub-milestones (Pass 3 had this implicitly).
**Q3 Counter-check completeness:** N/A (no tests yet)
**Q4 Doc-vs-impl drift:** plan cites real files + line numbers
for every claim. Verified via grep before writing.

---

## Commit history

| Commit | Sub-milestone | Notes |
|---|---|---|
| TBD | 4.0 | Pass 4 plan to disk |

---

## Out of scope (Pass 5+)

- Custom CRT shader (real scanlines per-pixel) — defer to Pass 5
  if Volume-only approximation isn't enough.
- 60fps interpolated turn movement — biggest perceived-quality
  upgrade overall but high-risk for Pass 4. Solo Pass 5 candidate.
- VFX Graph spell impacts — needs spell-system audit first.
- Day/night cycle — gameplay-affecting, needs design.
- Animated CP437 glyphs — content-heavy.

---

*End of plan. Updated as each sub-milestone ships.*
