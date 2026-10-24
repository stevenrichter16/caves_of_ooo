# G1.3 — normal launch and reversible presentation selection

Status: **foundation and launch/control native GREEN; ordinary default launch,
menu switching, uncovered-zone travel, QuickLoad and fresh-session Continue
observed, 2026-10-23 local / 2026-10-24 UTC**. Packaged release and physical
Steam Deck acceptance remain separate gates. The new bootstrap
applies the selected view before its first zone binding. Parent: [graphics completion plan](GRAPHICS-COMPLETION-PLAN.md).
Preceding slice: [G1 presentation](GRAPHICS-G1-PRESENTATION.md).

The user wants the new graphics in the actual game, with the previous
presentation recoverable. This is an original CoO settings/integration change;
there is no Qud source-parity claim. Native 16×24 art and retained assets follow
[the live art direction](LIVE-16X24-ART-DIRECTION.md).

## Decision

Use **two complete presentation choices**: `Flat16x24` and `Previous`.
`Previous` restores the existing 3D/sprite choices and their existing quality
settings. It is not another name for a single forced 3D setting.

Use a separate stable preference, key
`CavesOfOoo.WorldPresentation`, with explicit persisted integer values:

| Value | Runtime enum | Player label |
| --- | --- | --- |
| 0 | `Flat16x24` | `World view: 16x24` |
| 1 | `Previous` | `World view: Previous` |

Absent or invalid new preference means `Flat16x24`. Loading reads but does not
write preferences. A deliberate menu/F11 choice writes only the new mode key.
The old keys retain their exact values and presence, including absent optional
resolution/shadow overrides. Do not change old preference defaults, reinterpret
quality values as modes, delete old assets, or migrate save graphs.

An existing old `CavesOfOoo.Village3D` key does **not** count as a new explicit
request for Previous: existing quality-menu saves also write that key, so its
presence cannot reliably identify such intent. Defaulting absent new keys to
flat is deliberate for both fresh installations and existing users. Once the
player chooses Previous through the new selector, that explicit choice wins
on subsequent launches. Returning to Previous recovers their saved old setup.

## Verification sweep and corrections

Paths below are under `Assets/` unless prefixed `ProjectSettings/`. Line numbers
describe the source inspected before this slice; recheck them after G1.2 lands.

| Assumption | Verified source / correction | Required consequence |
| --- | --- | --- |
| The normal build already enters flat mode | `ProjectSettings/EditorBuildSettings.asset:9` includes SampleScene only. `Scenes/Main/SampleScene.unity:561` holds ZoneRenderer without a serialized flat-mode opt-in. | Normal bootstrap must explicitly apply the selected presentation; an art launcher is not delivery. |
| New and continued games share one initial camera setup | `Scripts/Presentation/Bootstrap/GameBootstrap.cs:396` sets the fresh zone before binding the flat camera at444; loaded games call the separate `WirePresentationForLoadedGame` at1031, set zone at1046 and bind at1062. | Apply the current selection before either SetZone, then retain both camera bindings before SnapToPlayer. |
| Continue loads before any fresh world is presented | `GameBootstrap.cs:710–745` registers runtime and opens the boot menu after initial world wiring. Continue later calls `ApplyLoadedGame:910`. | The same mode must already govern the backdrop/build picker and continue without an old-style first frame. Do not reload preferences during QuickLoad. |
| F11 is reversible | `Scripts/Presentation/Rendering/Village3DControls.cs:25–29` exits flat and returns without persisting; later F11 toggles only the old 3D Boolean. | F11 becomes a true Flat/Previous toggle using the same operation as the menu. |
| Controls Awake is a harmless binding | `ZoneRenderer.cs:287–290` adds Village3DControls during Awake, before creating environment renderers. `Village3DControls.cs:11` reloads old settings. | Establish one explicit normal bootstrap load/apply order. Do not let a dynamically added control reload or override a selected mode later. |
| Setting the flat Boolean is enough | `GameBootstrap.cs:497–498` later adds SpriteEnvToggleController. Its `Awake:25–35` finds an environment renderer and applies `CavesOfOoo.SpriteEnvironmentEnabled`. Current FlatPixelModeActive requires RenderingEnabled. | A saved old sprite-off preference must not disable a requested native world. Keep saved legacy sprite choice separate from the effective renderer switch. |
| Temporarily changing old settings cannot persist | `Village3DSettings.Save:116–128` writes Enabled, detail, and optional resolution/shadows; `PauseMenuController.ChangeGraphics:340` calls it for quality edits. | Do not implement Flat by temporarily setting Village3DSettings.Enabled=false, then accidentally saving that value through quality controls. |
| One Boolean disables all old art | `ZoneRenderer.SyncVillagePresentation:1877–1909` arbitrates 3D, spawn-ring, Morrowfast and felling presentations. `Village3DPresenter:72,86`, `SpawnRing3DPresenter:96,115`, `VoxelWorldPresentation:314` also consume the old Enabled value. | Leave old mode semantics intact. Flat uses G1.2's shared ownership/suppression path; Previous restores the old request and environment visibility together. |
| GraphicsPolish should be the new mode switch | Its comments still describe false/pure CP437, but `Scripts/Presentation/Effects/GraphicsPolish.cs:38` is currently `true`; `ZoneRenderer:375–425` constructs environment, authored and actor systems under it. | Do not flip or repurpose the compile-time feature gate. Correct misleading comments only alongside affected implementation. |
| Controller settings need new mappings | `NativeGamepadInput.cs:42–47` already maps menu A/B/Start/D-pad to Return/Escape/Tab/arrows. `InputHandler.Controller.cs:45–51` chooses Menu while paused. | Reuse the existing input path; test actual queued Gamepad states and the real pause handler. |
| A new row can be added without layout checks | PauseMenuController has seven fixed graphics rows; `PauseMenuUI.cs:32` uses a28-column box, draws labels atcolumn3 and computes height from VisibleItemCount. | Keep labels short and test changed height, selection bounds, mouse rows and complete clearing. |
| “Return to 3D Spawn” selects 3D | `Editor/DefaultSceneLoader.cs:66–67` only opens SampleScene. Its play-start logic intentionally preserves explicit study scenes. | Rename this command to **Open Normal Game**; it must not silently rewrite player presentation preferences. Keep static studies explicitly named studies. |
| A leftover editor flag can be normal launch authority | `Editor/Scenarios/LiveFlatPixelGreyboxTools.cs:45–51` sets an unsaved scene Boolean and clears the play-start scene. Recovery restores scene/view/seed but the Boolean has no ownership token. | Ordinary bootstrap ignores stale serialized/unsaved study flags. An explicit audit override requires current isolation ownership, is never persisted, and expires on cleanup. |

## User experience

The first Graphics row is always `World view: 16x24` or
`World view: Previous`. Enter/A, left/right, or clicking the row switches the
presentation immediately without closing the menu or consuming a turn.
F11 performs exactly the same two-way selection and persists it. A concise
log says `World view: 16x24.` or `World view: previous graphics.`

Use the approved ten-row stable menu below in both modes. While flat is
selected, individual retained 3D quality labels start with **Previous**;
they do not imply that shadows or 3D resolution alter native pixel art.
The retained configuration and quality controls remain reachable:

| Row | Exact example label | Meaning |
| --- | --- | --- |
| Presentation | `World view: Previous` | Return to flat on the next change. |
| Old model request | `Previous 3D: On` | Existing Village3DSettings.Enabled choice. |
| Old sprite request | `Previous sprites: On` | Existing legacy sprite preference. Off restores the existing glyph-oriented path and suppresses competing scene/actor views. |
| Quality preset | `Handheld preset` | Existing quality and spell-effects preset, without selecting a presentation. |
| Quality preset | `Full preset` | Existing quality and spell-effects preset, without selecting a presentation. |
| 3D target | `3D resolution: 75%` | Retained world target resolution; not native cell magnification. |
| 3D lights | `3D shadows: Off` | Retained 3D shadow choice. |
| 3D dressing | `3D detail: Low` | Retained detail choice. |
| Effects | `Spell effects: Reduced` | Existing shared effect setting. |
| Exit | `Back` | Returns to the main pause menu. |

Changing Previous3D/sprites is an explicit edit to those old preferences;
merely entering/leaving Previous never edits them. The original combination
with sprites on and 3D off remains reachable, as does the glyph-oriented
configuration. Do not rename the 3D-off/sprites-on combination “pure glyphs.”

Prefer a small stable graphics-row ID mapping to visible indices, not repeated
mode-dependent index arithmetic. Switching modes keeps selection on the first
row, rebuilds/clears the popup, and clamps any stale mouse/selection index.
If conditional rows prove disproportionately risky during implementation,
the accepted fallback is always-visible **Previous...** labels with a clear
“saved for previous view” indication. Never leave unqualified shadow/resolution
rows implying that they alter the flat renderer. Preserve preset semantics;
do not silently make them stop changing spell effects.

Keyboard: Tab/Escape opens pause; arrows choose Graphics; Enter confirms;
left/right change the current setting; Escape backs out; Tab closes.
Deck: Select/View opens Pause from the world; Start opens Character there.
Once the pause menu is open, Start closes it; D-pad or left stick navigates,
A changes, and B backs.
No additional Steam Input profile or F11 binding is required. The existing
menu context must quarantine a held world button so changing graphics cannot
also move, attack, wait, save or load. Retain mouse hit-testing on the square
popup canvas.

Shift+F11 remains the existing detail shortcut. It must not switch mode. In
flat mode its log should explicitly say `Previous 3D detail: low/full.` to
avoid implying a native visual change. Backslash changes the preserved legacy
sprite choice only; while flat it must not disable the effective native renderer.
Its message should say that the choice applies to previous graphics. Both
shortcuts and corresponding menu rows share the same stored legacy value.

## Minimal integration design

### One settings authority

Add a bounded `WorldPresentationSettings` with the explicit two-value enum,
load/current/save operations and exact legacy-sprite preference handling.
Do not make this an art registry, quality replacement or save-game property.
Mode assignment requests a redraw only on change. Read-only Load does not
write, erase or normalize the raw old keys on disk. Invalid new mode uses a
safe runtime flat default; a deliberate later selection writes a valid value.

Keep `Village3DSettings.Enabled`, LowDetail and nullable quality overrides as
their existing legacy values. The effective environment request is:

```csharp
bool nativeRequested = mode == WorldPresentationMode.Flat16x24;
bool environmentRequested = nativeRequested || legacySpriteEnabled;
// Apply together through the owning ZoneRenderer, then use its existing
// synchronization, projection restoration and claim invalidation.
```

Add a narrow owner-bound apply method on ZoneRenderer; it sets the effective
environment request and FlatPixelModeEnabled in the correct order and performs
one coherent synchronization. No FindObjectsByType selection of an arbitrary
environment renderer, global camera lookup, every-frame preference polling or
whole-zone scan is needed for settings. Static semantic mode remains distinct
from whether the current world is temporarily hidden by a fullscreen UI.

Normal bootstrap loads legacy settings plus the new settings once, before
first presentation binding. Controls Awake becomes binding-only for this
responsibility; it must not reload after a user/audit choice. Both fresh and
loaded wiring call the same apply seam before SetZone, and continue binds the
existing flat camera before SnapToPlayer. QuickLoad uses the current session
choice rather than loading potentially stale PlayerPrefs again. Standalone
test renderers can continue explicitly setting their runtime flag.

The menu receives one explicit apply callback from its existing bootstrap/
InputHandler owner; F11 uses its own known ZoneRenderer. Both call the same
mode-setting/persistence function. Avoid a permanently subscribed static
renderer listener unless it has demonstrated value and verified disposal.
Changes while the menu owns a square UI canvas must redraw that canvas without
temporarily giving the world back to old painters.

Replace SpriteEnvToggleController's direct saved-Boolean-to-effective-renderer
assignment with the same legacy setting/apply path. Preserve the existing
preference key and explicit save behavior. Normal startup must be correct
whether that control is added before or after camera binding. Ensure it binds
to the bootstrap's actual renderer, not whichever environment object a scene
search happens to return first.

### Explicit editor audit override, not scene authority

Normal SampleScene and ReferenceGlade bootstrap always honor the new
preference/default, regardless of a stale `_flatPixelModeEnabled` scene value.
The static SeedKeeper study/clearing have no GameBootstrap and remain unchanged.
Opening them does not write any player preference; the normal-scene command
does not force a view. Preserve their existing explicit play-start routing.

If the live art or old3D native audits need a forced view, add a narrow
editor-only override paired with the exact current private save-isolation root
(or its existing owned token), not a naked process-static Boolean. Validate
that the override's owner is still the current isolated session. A pending
flag, nonzero seed, or arbitrary saved root by itself is insufficient. No valid
override exists in a player build. Cleanup restores the prior override and
preference state; reload recovery may reconstruct it only from the owned active
audit record. A stale record must never override an ordinary user's preference.

Update explicit old3D acceptance launchers to request Previous before their
existing runtime settings overrides; do not force all scenarios into Previous
or rely on their later Enabled=true call to defeat the new flat mode. Existing
isolated live launch must also use this explicit seam instead of treating an
unsaved scene flag as preference authority. Do not touch save contents.

## RED tests before production

Run native assertion/compile RED for new contracts before implementing. Old
controls tests that intentionally pin the former F11 two-legacy-mode behavior
must be updated with the behavior change explained; retain their no-turn,
held-key, resource lifetime and legacy-quality counterchecks.

1. **Preferences:** absent new key yields flat with no keys written; old
   3D/sprite/quality keys present still yield flat; valid Previous round-trips;
   invalid values fall back at runtime without rewriting old raw values;
   fresh load, mode switch, F11, menu and re-load preserve every old key's
   presence/value, including independently absent nullable quality overrides.
   Compare custom floats and unusual saved integer values, not just defaults.
2. **Quality separation:** switch Flat→Previous→Flat with old Enabled on/off
   and sprites on/off. Assert exact effective renderer state, camera/grid,
   old presenter visibility and preserved legacy values. Quality changes do
   not select a mode; mode changes do not alter detail/resolution/shadows/FX.
   An existing quality Save after flat selection cannot persist a temporary
   override because there must be no such override in legacy settings.
3. **Real bootstrap wiring:** load the actual SampleScene in an isolated
   fixture and invoke the actual presentation-binding seams. Saved sprite-off
   must not defeat default flat, including the late control Awake order.
   Exercise both fresh and ApplyLoadedGame, then an uncovered current zone
   supplied by G1.2. Test a stale true/false scene flag against each persisted
   mode. A helper-only Load test cannot establish these paths.
4. **Menu and input:** actual keyboard F11 toggles both directions exactly
   once; held/other keys do not repeat. Native Gamepad Start→Graphics→A and
   left/right switch both ways, B and Start exit correctly, no keyboard
   mapping. Preserve tick, energy, position and gameplay command counts.
   Previous rows are reachable, conditional count matches hit-test/draw bounds,
   and returning to flat clears every extra row. Shared effects remain usable.
5. **Fullscreen/portrait round trip:** open inventory/skills/popup with each
   mode, switch where input permits, close and inspect world/cursor/portrait.
   UI stays square, world returns to the correct geometry, previous actors
   do not leak into native and native claims do not survive Previous.
6. **Editor authority/isolation:** valid owned override wins without writing;
   stale token/root, inactive isolation and normal launch ignore it. Cleanup
   and reload restore exact keys and play-start scene. “Open Normal Game”
   does not select 3D, while explicit study routing remains preserved. Borrowed
   test preferences, hooks, domain-reload settings and input devices restore
   in finally/teardown. Do not stop a user's active audit to run a fixture.
7. **Restart/continue acceptance:** ordinary process launch without the art
   launcher enters native; explicit Previous survives restart and Continue;
   switching back survives restart. Save/load does not encode presentation into
   the world graph. No first-frame flash of old painting, blank world, stale
   portrait or shifted mouse/cursor during mode/zone changes.

Relevant existing suites: Village3DControlsTests, SteamDeckGraphicsSettingsTests
and AdversarialTests, PauseMenuControllerTests, ReferenceGladeSceneTests,
LiveFlatPixelGreyboxRecoveryTests, FullscreenUiCanvasOwnershipTests,
FlatPixelWorldProjectionTests and the current G1.2 continuity fixtures. Existing
old3D native audit preference snapshots must include any newly borrowed key
and override; they currently commonly snapshot only mode/detail.

## Delivery and acceptance

Implement preference/owner binding first, then menu/shortcut round trip, then
normal/editor launch routing, each with real RED→GREEN and independent review.
Do not enable the new normal default before G1.2's uncovered-region world,
hazard, fog, actor, portrait and projection gates pass.

Actual acceptance: normal new game → open Graphics → Previous → old3D and
glyph-oriented configurations → native → inventory/skills/examine → travel to
an uncovered zone → save/quit → Continue. Repeat with keyboard and native pad
at1280×800. Capture the actual menu and world in both modes, with build/version,
preference values, projection and scenario state recorded. Keep normal launch
distinct from the isolated art launcher. A packaged executable launch and
physical Steam Deck verification remain separate gates from Unity EditMode or
queued Gamepad tests; do not claim them from editor evidence.

## In-phase review and handoff

- 🔵 Two-value mode was approved by the parent during this source audit; old
  3D and sprite choices remain explicit and preserved.
- 🟡 The initial late SpriteEnvToggle Awake and one-way F11 launch/recovery
  defects were repaired after native RED; implementation and receipts follow.
- 🔵 The approved ten-row stable fallback replaced conditional rows; native
  clearing/selection tests and the actual popup capture verify this choice.
- 🧪 Native input tests can prove route dispatch and simulation invariants;
  real screenshots/Play must establish readability and first-frame continuity.
- ⚪ No old save migration, global art-completion claim, quality rewrite,
  arbitrary release date, or new gameplay scope is introduced.

Initial handoff: source sweep and design were saved before any test or
production change. The chronological records below retain the original RED
and intermediate failures; corrected GREEN and ordinary Play appear later. Re-read
shared rendering files at implementation time: G1.2 is being implemented
concurrently and this document intentionally does not freeze its private APIs.

### First bounded foundation — native RED, then corrected GREEN

The initial `WorldPresentationSettingsTests.cs` fixture contained 15 methods /
21 cases and its fresh hand-written `.meta`. The combined native Unity receipt
`Docs/Verification/Live16x24/g1-global-settings-red.xml` has 50 cases, 2 pass /
48 fail; all 21 settings cases fail at their intended missing-API assertions.
No fixture errors were reported. Production followed this RED. Two subsequent
real InputHandler binding/unbound counters bring this fixture to 17 methods /
23 cases; these are GREEN-review additions, not separately demonstrated RED.
The fixture deliberately separates this foundation from normal launch and full
menu acceptance. Parent owns native execution. The corrected combined run subsequently passed
all 52 foundation/G1.2 cases, as recorded below.

Frozen first-slice APIs:

```csharp
enum WorldPresentationMode { Flat16x24 = 0, Previous = 1 }
// New WorldPresentationSettings, runtime-only assignment:
WorldPresentationMode Mode { get; set; }
bool LegacySpritesEnabled { get; set; }
void Load();               // reads only the new mode and old sprite choice
void SaveMode();           // writes only CavesOfOoo.WorldPresentation
void SaveLegacySprites();  // writes only the existing sprite preference
// New ZoneRenderer.PresentationSettings.cs, no edits to the G1.2 partial:
void ApplyWorldPresentation(WorldPresentationMode mode, bool legacySpritesEnabled);
// Actual PauseMenuController dispatch seam:
const int WorldPresentationIndex = 0;
Action WorldPresentationChanged; // InputHandler binds its current renderer explicitly
```

The settings expose stable `PreferenceKey` and `LegacySpritesPreferenceKey`
constants. `Load` must not reload Village3DSettings; the bootstrap separately
owns that existing call. SaveMode cannot serialize the effective environment
flag into the legacy sprite preference. Legacy Save cannot normalize an invalid
new mode key as an unrelated side effect.

Coverage: absent/invalid/explicit mode, repeat read-only load, exact old key
presence/raw values, runtime quality isolation, independent old sprite storage,
old-settings reload, owner-bound repeated apply with an unrelated renderer,
borrowed grid restoration and fullscreen/no-zone counters, and the real graphics
controller row through ClickSelect/Tick in both directions. The fixture restores
all borrowed preferences, runtime values and dirty hooks. The owner harness is
an inactive initialized-field unit harness: it proves the public apply seam and
existing projection metrics, **not** a normal scene, real camera, painted frame
or live old3D restoration. The menu tests use IInputProbe, **not** queued native
keyboard/Gamepad events. Those wider gates remain explicitly above.

Implemented after RED: new `WorldPresentationSettings.cs` stores the two-mode
choice independently and reads the old sprite choice without touching old 3D
quality. New `ZoneRenderer.PresentationSettings.cs` applies both flags to its
own renderer and synchronizes the existing projection/presenters. The primary
pause-menu row uses an explicit InputHandler callback bound whenever its renderer
or borrowed UI changes; an unbound row neither changes nor saves the mode. A
failed callback restores the runtime mode and does not persist it. Mode dispatch
returns before old quality Save calls, preserving exact old key presence/values.

The temporary eight-row working-tree menu labels old quality choices as
“Previous” while flat is selected. Three existing menu pins were adjusted for
the new first row/count; native-pad preset coverage still explicitly navigates
to and activates the handheld preset. Dynamic previous-only rows, old 3D and
sprite controls, physical F11, bootstrap/new/load wiring and late SpriteEnvToggle
binding are the next checkpoint. This foundation will **not** be committed or
shipped separately before that full normal-launch binding is implemented and
accepted. No menu inference from a possibly stale editor renderer was added.

Self-review: exact old key custody and no global owner search are covered by the
initial RED fixture; the additional InputHandler tests cover actual callback
wiring and detachment. The pure owner rig remains bounded as described above.
No gameplay or per-frame loop was added. The explicit editor audit override,
actual popup/device acceptance and normal first-paint continuity remain pending;
G1.3 is not complete from these 23 cases.

Foundation first combined implementation run: `g1-global-settings-green2.xml`
has 52 total / 43 pass / 9 fail. Of these, 3 settings geometry cases exposed a
fixture error: its Grid ancestor was inactive, so the production
`GetComponentInParent<Grid>()` could not discover it. The fixture now activates
only the Grid ancestor, keeps the renderer child inactive to avoid Awake, and
asserts that the actual Tilemap can find that Grid before applying settings.
All geometry/restoration expectations remain unchanged; no production behavior
was weakened. The other 20 settings cases passed. The next native run confirmed
all 52 corrected foundation/G1.2 cases GREEN.

### Second bounded checkpoint — launch/control native RED, implementation written

New native fixtures were executed before production: 19 cases in
`WorldPresentationLaunchTests` and 6 in `WorldPresentationAuditOverrideTests`.
They cover the actual ApplyLoadedGame path using the existing isolated
HotbarSaveFixture, a shared fresh-binding seam before SetZone, one-time initial
preference loading, queued native Keyboard F11/backslash, real InputHandler
keyboard and Gamepad pause routes, retained-view controls, popup bounds/clearing,
and the existing NativeSaveIsolation owner/token/root authority. The fresh
binding test checks the actual seam's resulting state; full DoStart first-frame
ordering still requires ordinary Play. No launch/control implementation precedes
this checkpoint's actual RED.

Source correction: NativeGamepadInput world Start is Character, while Select/View
is Pause. The previous design's Start-opens-Pause sentence was wrong; the new
fixture uses Select, enters the real InputHandler menu context, and then exercises
D-pad/A/B. The mappings themselves remain unchanged.

The parent approved the simpler ten-row fallback for this checkpoint: retain
stable visible indices, add explicit Previous 3D and Previous sprites rows, and
label quality choices as Previous while flat is active. This avoids a separate
mode-dependent row-ID layer. Flat/Previous changes do not erase these choices;
only an explicit retained-setting action edits them. Labels must fit the existing
28-column box and the actual 1280×800 popup remains a live acceptance gate.

The planned editor override reuses NativeSaveIsolation's existing ownership and
SessionState, with no new save framework: explicit SetPresentation validates the
active owner/token; launch resolution checks its exact current root. Finish and
new Begin invalidate the request. Normal boot loads settings once, QuickLoad
keeps the session choice, and stale serialized scene flags are never authority.


Native receipt `g1-corrected-foundation-launch-red.xml`: **77 total, 54 pass,
23 fail**. All 52 corrected foundation cases passed. Of the 25 new launch/
control cases, 23 failed at the intended missing behavior and the two existing
keyboard/Gamepad pause-route controls passed. Production below followed this
RED; no implementation GREEN or normal-launch Play is claimed yet.

Implementation now written: bootstrap loads old quality and new presentation
once before generation, applies the runtime choice before fresh/loaded SetZone,
and binds the exact late sprite-toggle owner. QuickLoad keeps the current
selection. F11 and the primary menu row share one apply-before-save operation.
Backslash edits only the preserved previous-view sprite choice; flat keeps its
required layer even when that choice is off. The final ten stable rows include
Previous 3D and Previous sprites, and qualify individual retained 3D quality labels in flat. Handheld preset and
Full preset keep their shared labels because they also change Spell effects.
NativeSaveIsolation carries an explicit editor-only mode request using its
existing owner/token/root; normal boot ignores stale scene flags. Existing
3D acceptance launchers explicitly request Previous, while the live native-art
launcher requests Flat. The normal-scene menu command now says Open Normal Game.

The broader `g1-renderer-regression.xml` run completed **663 total, 661 pass,
2 fail** before this launch implementation. One camera-owned old-scope pin is
separate. The old pure-menu Gamepad counter exposed a borrowed-context leak:
the new real InputHandler test left NativeGamepadInput in World. Its teardown
now restores its captured context, and the older pure-menu fixture establishes
and restores its own Menu precondition. Actual queued pad assertions remain.
Existing graphics count/index pins now intentionally expect ten rows; previous
F11 and sprite-toggle tests use an explicit local renderer, preserved runtime
choices, and exact preference snapshots. Their next native result is pending.

In-phase review found existing Village3D/SpawnRing live audit routines still
expected F11 to modify the old 3D Boolean and failed to borrow the new persisted
mode. Both now assert the Flat/Previous transition while retaining that Boolean,
restore exact mode/sprite key presence and runtime values, and preserve their
no-turn/detail controls. SpawnRing's launcher also explicitly selects Previous
before its initial WaitForRing. These are compatibility repairs; neither old
live audit has been rerun in this checkpoint.

Self-review: 🟡 native input context custody and old live-audit preference leaks
were repaired before the next test run. 🔵 the approved always-visible ten-row
menu is used instead of conditional rows, with explicit Previous labels. 🧪
ordinary startup first-paint ordering, Continue, popup fit and real view switching
remain parent-owned live acceptance. No physical Steam Deck performance or
complete-world graphics claim follows from these tests.


First launch implementation receipt `g1-launch-green.xml`: **168 total,
166 pass, 2 fail**. Only the two flat fresh-binding geometry cases failed:
their HotbarSaveFixture root was inactive, and its co-located Grid was not
discoverable by the production active-parent query. The fixture now uses an
active Grid parent and retains an inactive bootstrap/renderer child; it
explicitly asserts discovery before invoking the real bootstrap binding.
Existing mode/stale-flag/environment and1×1.5 geometry expectations are
unchanged. No production fix was required. The166 passing cases include
normal load/session choice, F11, backslash, native menu routes, editor override
and the affected prior controls. Corrected focused rerun and normal Play remain
pending; this receipt is not labeled all GREEN.


Corrected focused receipt `g1-launch-final-green.xml`: **126/126 pass,
zero skipped**. It includes all25 launch/owned-override cases,23 settings
cases and78 specific controls/fullscreen/adversarial cases. The earlier
`g1-launch-green.xml` separately passed29 global-presentation and13 ordinary
graphics cases, yielding168 distinct passing cases across the corrected
focused run and those unchanged controls. No extra cases or repeated runs are
counted as new coverage. Both grid-fixture corrections preserve production
behavior and the original geometry assertions.

All Assets are frozen for parent-owned ordinary SampleScene Play using a
private save root, an absent new mode key and no forced presentation override.
The explicit art launcher is not used as proof of normal default delivery.
Actual first paint, visible switching and Continue remain pending.


### Ordinary normal-launch Play — first acceptance slice

Parent ran ordinary `Assets/Scenes/Main/SampleScene.unity` with seed64 and the
existing private save isolation. `g1-normal-launch.json` records the new mode
key **absent** and `forcedPresentation=false`; this was not the forced-flat
art launcher. `g1-normal-start.png/json` then records Flat16x24 active in
`Overworld.11.10.0`, player(40,12),40HP, tick10, retained previous3D/sprites
both true, and grid(1,1.5,1). This establishes normal default delivery in the
editor session rather than only an explicitly selected art-study mode.

The following are actual ordinary keyboard interactions; paired PNG/JSON
receipts are under `Docs/Verification/Live16x24/`:

| Receipt | Observed behavior | Bound |
| --- | --- | --- |
| `g1-graphics-menu` | Esc→Graphics displays all ten rows, with explicit Previous 3D/sprites and previous-view resolution/shadows/detail labels; labels fit the popup. | Keyboard route and screenshot, not physical Deck input. |
| `g1-previous-roundtrip` | Selecting Previous writes mode1, restores the square world grid and retains the old3D/sprite choices at tick10. Returning to Flat writes mode0. | Does not establish every old quality combination or retained-region model. |
| `g1-flat-inventory` | Actual inventory borrows the square grid while Flat remains the selected mode, with tick10 unchanged. | Fullscreen UI borrowing, not a claim about every screen. |
| `g1-flat-selection` | On returning to the world the grid is1×1.5; the actual Look cursor selects(39,11). Tick remains10. | Selection position was observed in Play; JSON records LookMode and projection, not the cursor coordinates. |
| `g1-west-outside-native-kit` | Ordinary movement via the northern y9 route enters `Overworld.10.10.0` at(79,9), nativeAdmitted=false, Flat=true and grid1×1.5. | This region uses the honest fallback; no complete native art claim. |
| `g1-return-to-opening` | Ordinary eastward return enters `Overworld.11.10.0` at(0,9), nativeAdmitted=true and the same flat projection. F5 save was issued afterwards. | Successful save/restore acceptance awaits its later receipt. |

Travel was not a no-turn or no-danger test: its receipts show tick450/460 and
27HP, versus tick10/40HP at the opening. No movement or combat results were
replaced by art-only state setup. The captured PNGs are1280×800; the paired
script receipts report `Screen.width/height`2321×1139. Preserve this distinction:
the images establish the observed captured popup/world, not a physical Steam
Deck framebuffer or a packaged executable.

**Can verify:** the normal scene honors the absent-key default without an
owned forced view; real keyboard menu switching persists the mode, preserves
old choices and consumes no turns; inventory borrows/restores geometry; flat
projection continues through an uncovered adjacent region and back. Native
tests additionally cover owner-specific rebinding, existing-key choices,
queued Gamepad controls and no-turn invariants.

**Remaining at this first acceptance slice:** QuickLoad and boot-menu Continue
were still pending; the following section records their completion. Every
previous-graphics configuration, packaged startup, physical Steam Deck controls
or performance, complete art coverage, and frame-by-frame first-paint timing
remain outside this evidence. Startup ordering is source/native-test verified;
the first ordinary captured Play world is flat.


### Ordinary save/load, Continue and final cleanup

The saved position was (0,9), 27 HP, tick 460 in `Overworld.11.10.0`.
After ordinary Down movement, `g1-before-quickload.png/json` records (0,10),
14 HP, tick 470: the pursuing enemy actually attacked. Pressing F6 produced
`g1-after-quickload.png/json`, restoring (0,9), 27 HP, tick 460 with
Flat16x24 active, saved mode 0, no forced view, and grid (1,1.5,1). This is a
real changed-state recovery, not a reload of an identical untouched graph.
Both these receipts now report Screen 1280×800 as well as 1280×800 captures.

The first Play session was stopped before a fresh normal-scene launch.
`g1-normal-cleanup.json` records playing=false, isolation=false,
artPending=false, viewBorrowed=false, saveRoot=null and SampleScene restored.
The copied fixture for the next session was the privately owned actual save
just produced by gameplay; it was not the user's save directory or a fabricated
new graph. `g1-continue-launch.json` records the persisted mode key 0 and
forcedPresentation=false. Ordinary C at the boot menu successfully continues
that save: `g1-ordinary-continue.png/json` records (0,9), 27 HP, tick 460,
Flat16x24, grid (1,1.5,1), Screen 1280×800 and retained previous3D/sprites true.

`g1-continue-cleanup.json` records final stopped/restored state at
2026-10-24T02:04:28Z: playing=false, isolation=false, artPending=false,
viewBorrowed=false, saveRoot=null, normal SampleScene. The live sessions used
ordinary keyboard gameplay and the existing isolated save harness. Neither
required a forced presentation override.

### Bounded editor profile and diagnostics

`g1-profile-start.json` and `g1-delivery-profile.json` retain the recorder
configuration and unwrapped raw summary. The observation lasted **98.363869
seconds**, spanning ordinary movement, Graphics menu, inventory, save/reload,
idle and tool/editor time. Each recorder has 85,353 samples, capacity 131,072,
and wrapped=false; the separate frame delta is 85,352. The final sampled
zone is the uncovered `Overworld.10.10.0` at (79,9), 27 HP.

| Recorder | Mean | p99 | Maximum |
| --- | ---: | ---: | ---: |
| COO.ZoneRenderer.LateUpdate | 0.052781 ms | 0.116375 ms | 15.772750 ms |
| Main Thread | 1.149152 ms | 2.198875 ms | 849.262667 ms |
| Draw Calls Count | 29.162584 | 34 | 34 |

The draw-call counter is a count, not milliseconds. These are editor
observations with tool/idle time included, not a controlled baseline, isolated
renderer attribution, frame-rate guarantee, measured speedup, standalone
benchmark or Steam Deck result. The large main-thread maximum remains visible
in the report rather than being excluded.

The console tool mislabeled an ordinary `[Combat] Autosaved.` entry as an
Exception. The raw Unity Editor.log stack identifies `UnityEngine.Debug:Log`
and `SaveGameService.PublishAutosave`; a stack trace or the method's Exception
parameter is not an exception event. The actual Console UI showed zero errors
and two Metal memoryless depth-surface warnings. This distinction prevents a
false error claim without hiding the warnings.

Final bounded review: no remaining actionable P1/P2 was found in settings,
bootstrap, explicit owner controls, or Village/SpawnRing audit compatibility.
The latter routines were source-reviewed and their affected control tests
passed; their full older live workloads were not rerun. G1.3 now has native
verification plus ordinary default/new-session Continue evidence. Packaged
startup, physical Steam Deck controls/performance, all retained-view
combinations, full-world final artwork and frame-by-frame first-paint capture
remain separate release or graphics-completion gates.
