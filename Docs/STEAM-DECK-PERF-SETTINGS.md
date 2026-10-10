# Steam Deck presentation settings

Status: implementation complete; native focused GREEN 10/10, surface GREEN
4/4 and dedicated adversarial GREEN 26/26 reported by root. The updated
AlphaOnboarding menu-shape pin also passed.
Qud reference: none. Preferences affect presentation, never simulation/save data.

## Goal and scope

Expose presentation controls through the existing pause menu, accessible with
native gamepad A/B/Dpad as well as keyboard/mouse. Offer explicit Handheld and
Full presets and independent world resolution, shadows, detail and spell effects.
Retain full-resolution HUD/popup cameras and the existing save/load/help/quit
behavior. Existing persisted preferences remain authoritative.

Owned files: Village3DSettings.cs, SpellFxSettings.cs, PauseMenuController.cs,
PauseMenuUI.cs and associated new tests/docs. Root owns InputHandler/ZoneRenderer;
rendering agent owns NativeZone3DRenderSurface/presenter integration. All native
Unity refresh/test/play calls are root-controlled. No commits or staging.

Content readiness: 🟢 existing fonts, popup tilemaps, gamepad mappings and effects
modes; no art/new camera/save migration needed. 🟡 submenu routing, preference
migration and legacy low-detail behavior need regression coverage.

## Verification sweep and corrections

| Premise | Verified source and decision |
| --- | --- |
| Pause menu has a reusable submenu | Controller has four flat entries and fixed navigation bounds; UI has fixed 28x9 geometry. Add explicit graphics state and visible row count; reuse the same UI/camera. |
| Native gamepad requires new mapping | NativeGamepadInput maps A to Return, B to Escape, Start to Tab and Dpad to arrow keys. Existing controller polling can consume these directly. |
| LowDetail only reduces decoration | NativeZone3DRenderSurface.Sync ties 75% target size and shadows-off to its bool argument. Keep that API unchanged for existing callers/tests; add unique SyncConfigured with independent inputs for ordinary presenters. |
| Add a Sync overload | Reflection-based tests look up Sync by name. Use a distinct method name so overload resolution does not break them. |
| Independent preferences should replace LowDetail | Keep nullable internal overrides; absent resolution/shadow keys derive the existing LowDetail behavior. Explicit new choices override only their dimension. |
| Deck preset must guess Linux hardware | Root will export COO_HANDHELD=1 in the Deck distribution launcher. Use that explicit hint only for absent preferences; never infer hardware from Linux. |
| Resolution change should resize UI | Only owned native world RenderTexture dimensions change. Borrowed source camera, HUD/popup cameras, viewport rect and projection remain unchanged. |

Sources read: CLAUDE.md, ADVERSARIAL_TESTING.md, PERF-FOUNDATION.md, audit finding
4; Village3DSettings, SpellFxSettings/Panel, Village3DControls and tests,
PauseMenuController/UI and controller tests, NativeGamepadInput, InputHandler's
pause wiring, NativeZone3DRenderSurface.Sync and its callers in both presenters.

## API and navigation contract

- Main menu: SaveIndex=0, LoadIndex=1, ControlsIndex=2, GraphicsIndex=3,
  QuitIndex=4, ItemCount=5. Preserve Quit as the last entry.
- IsGraphicsOpen identifies the submenu; VisibleItemCount is seven there and
  five on the main menu. SelectedIndex is local to the current page.
- Graphics rows: HandheldPresetIndex=0, FullPresetIndex=1,
  WorldResolutionIndex=2, WorldShadowsIndex=3, WorldDetailIndex=4,
  SpellEffectsIndex=5, GraphicsBackIndex=6.
- A/Return/click applies or cycles selected preference immediately. Dpad/arrow
  up/down chooses a row; left/right cycles that row in either direction.
  B/Escape returns to main Graphics; Start/Tab closes the entire pause menu.
- Handheld = Low detail, 75% world resolution, world shadows off, Reduced spell
  effects. Full = Full detail, 100%, shadows on, Full spell effects. These are
  named choices, not measured frame-time guarantees.
- Village3DSettings.WorldResolutionScale clamps finite runtime inputs to 0.5..1;
  nonfinite inputs fall back to 1. ShadowsEnabled is an independent bool.
  ResetQualityOverrides restores legacy LowDetail-derived behavior.
- New PlayerPrefs keys: CavesOfOoo.Village3D.WorldResolutionScale (float) and
  CavesOfOoo.Village3D.Shadows (int). Existing mode/detail/FX keys retain semantics.
  Runtime setters do not save; menu changes explicitly persist.
- Native surface SyncConfigured(Camera,bool,float,bool) receives effective values
  from ordinary presenters; legacy Sync(Camera,bool,bool) keeps old semantics.

## Milestones and performance approach

1. Existing-API/reflection regression tests for graphics entry, submenu back/close,
   independent persisted preferences, fallback and presets; root records RED.
2. Implement only after RED, preserving legacy API and preferences. UI redraws
   when the page/selection/preference revision changes and clears its previous
   rectangle before changing geometry. No new popup camera or UI scaling.
3. Root runs GREEN + existing controller/settings/native input tests. Dedicated
   adversarial coverage probes keyboard/controller routing, invalid preference
   values, independent changes, lifecycle reopen, null service and persistence.
4. Rendering agent verifies actual target sizes/shadows and unchanged borrowed
   camera, UI rects, projection and resource recovery. Root owns native live check.

## In-phase self-review and divergences

- ⚪ Root-directed COO_HANDHELD=1 distribution hint defaults absent detail/FX
  preferences to handheld; existing saved values and independent overrides win.
  Load never creates preference keys. No OS/hardware detection is performed.
- ⚪ Frame pacing stays unchanged pending hardware measurement, as audit requires.
- 🟡 Resolved: submenu shrink must clear the old popup rectangle before changing
  origin/height. UI now records and clears its previously drawn bounds.
- 🟡 Resolved: stationary mouse hover previously could undo keyboard/Dpad movement
  each frame. Hover selection now requires actual mouse movement.
- 🟡 Resolved: menu detail toggle could change resolution/shadows through legacy
  fallback. That row captures the effective values before toggling detail and
  pins them as independent overrides. The explicit presets still change all four.
- Cold-eye review: scalar preferences preserve absent-key fallback; explicit keys
  win over the launcher hint; every submenu action stays within presentation;
  Back/Close/Open are symmetric; save/load/help/quit retain their dispatch paths.
- Tests restore PlayerPrefs, nullable override state, detail/mode, FX accessibility
  controls, environment hint and dirty callback. No test permanently changes the
  player's preferences.
- 🧪 No Steam Deck frame-time or visual-readability claim; no pacing policy change.
  The read-only full-resolution HUD contract is verified by native surface tests,
  not inferred from a lower world render-target pixel count.

## Implementation log

- Plan and source/API corrections recorded before production edits.
- Rendering integration contract sent to rendering agent and root.
- Root recorded native RED 10/10 for missing menu/API/hint behavior in job
  6579a486ab3847f4bdd049c273825e6e, then released production implementation.
- Root recorded GREEN for all ten focused tests plus four native surface tests in
  mixed job dcd7bb69e60e4e2186ba72ec5e2c4dcf.
- Root's broad 1,742-case native run, job e2b7486c6ca147138ec56075d2a974eb,
  accepted all 26 dedicated graphics adversarial cases, ten focused cases, four
  surface cases and the updated AlphaOnboarding pin. Five failures elsewhere in
  that mixed run were outside the settings/HUD cases; this is not an all-suite
  GREEN claim.
- 26 dedicated adversarial cases added: malformed scales, hint boundaries,
  explicit preference precedence, legacy/reset persistence, dimension isolation,
  effect wrap, menu boundaries/reopen, null service, dirty-count suppression,
  sound/accessibility preservation, real InputSystem gamepad navigation, tilemap
  geometry cleanup/no new camera and accurate resolution labels.
- Root verified InputHandler's ordinary pause route forwards native input directly
  to PauseMenuUI; it does not intercept B before the submenu controller.
- Rendering agent updated both ordinary presenters to SyncConfigured and retained
  the existing Sync(bool lowDetail) entry point for legacy callers.
- Independent settings-boundary cold-eye review by rendering agent found no issues:
  absent-key fallback, stale-override reset, null-key deletion, scale sanitization,
  same-effective-value overrides and both presenter routes were checked.
- Peer review additionally checked PauseMenuController/UI against InputHandler,
  submenu old-bounds erasure and stationary mouse/controller behavior; no
  actionable regression found.
- Updated AlphaOnboardingTests' intentionally superseded menu-shape pin from four
  rows/Quit=3 to five rows/Graphics=3/Quit=4. Controls remains index2 and the
  existing callback/save/load lifecycle assertions remain unchanged.

## Files changed

- Docs/STEAM-DECK-PERF-SETTINGS.md — plan, sweep, contracts and results.
- Assets/Scripts/Presentation/Rendering/Village3DSettings.cs — optional independent
  resolution/shadow preferences, legacy fallback, safe scale and launcher hint.
- Assets/Scripts/Presentation/Rendering/SpellFxSettings.cs — absent-preference
  Reduced default under the explicit distribution hint; existing keys preserved.
- Assets/Scripts/Presentation/UI/PauseMenuController.cs — graphics page, presets,
  independent controls and existing native/keyboard navigation.
- Assets/Scripts/Presentation/UI/PauseMenuUI.cs — same-camera graphics page,
  revision redraw, old-bounds cleanup and movement-gated mouse hover.
- Assets/Tests/EditMode/Presentation/UI/SteamDeckGraphicsSettingsTests.cs (+ meta)
  — ten focused tests, native RED then GREEN.
- Assets/Tests/EditMode/Presentation/UI/SteamDeckGraphicsSettingsAdversarialTests.cs
  (+ meta) — 26 boundary, lifecycle and native-input/UI tests.
- Assets/Tests/EditMode/Gameplay/Alpha/AlphaOnboardingTests.cs — update the previous
  flat-menu shape assertion for the intentionally added Graphics entry.

Can verify (script-observable): focused 10/10, surface 4/4 and dedicated
adversarial 26/26 native GREEN; preference defaults and preservation; menu
state/navigation through real InputSystem gamepad input, popup tilemap cleanup,
no new camera and world-only target settings. The AlphaOnboarding shape pin
passed. These checks do not substitute for the root-owned live route/profile.

Cannot verify (visual/feel): Steam Deck GPU/CPU speed, perceived graphics quality,
physical-controller comfort or full live-screen layout readability. The explicit
handheld preset is a user-selectable quality policy, not a performance guarantee.

- Root requested explicit COO_HANDHELD=1 launcher default after initial sweep.
  Without a saved detail preference: low detail=true, derived world resolution=.75,
  shadows=false. Without saved FX mode: Reduced. Existing Full detail/effects and
  explicit per-dimension preferences remain authoritative.
