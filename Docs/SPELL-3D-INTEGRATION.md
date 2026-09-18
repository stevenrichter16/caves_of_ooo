# Starter spell 3D integration

Status: complete, 2026-09-11. Blender art, native gameplay integration,
visual acceptance, reviews and full-suite baseline comparison are closed.
All 275 added tests pass; the 31 existing baseline failures remain unchanged.
User authorized integrating the seven Blender studies after their flash and
motion-comfort refinement. CoO-original visual work; no Qud parity claim.

## Scope and readiness

- 🟢 Six default actives and separately learnable Conjure Rain verified in
  STARTER-SPELL-3D-DESIGN.md; existing damage/status rules remain authoritative.
- 🟢 Blender P1 geometry, motion and actual exports imported. No studio
  platforms, targets, crops or extra casters enter the game as spell props.
- 🟢 Native runtime adapter, cast-animation binding and import tests pass;
  all seven spells pass actual GPU, native input, gesture and outcome checks.
  Final gameplay/profile capture S3D28c and its independent audit pass.
  Existing sprite/ASCII presentation remains available outside
  native 3D zones or if a native spell asset is unavailable.

## Pre-implementation verification sweep

| Verified source | Finding / implementation decision |
| --- | --- |
| WorldFxCoordinator.cs | Sole owner of both FX queues. Add native routing here; no second bus drain or gameplay replay. |
| SpriteSpellFxRenderer.cs / SpellFxSettings.cs | Preserve full/reduced/off, animation speed, blocking timeout and outcome-driven presentation. |
| NativeZone3DRenderSurface.cs | Supplies active XZ content, 56° camera and physical fog/light texture. Native FX own material clones; borrow the existing surface and never change source materials. |
| Village3DPresenter.cs / SpawnRing3DPresenter.cs | Cast hooks currently ignore spell and duration and play Interact for .22s. Integrate actual per-spell caster clips with correct facing, interruption and original-controller cleanup. |
| Village3DAssetBuilder.cs | Production FBX import has verified scale/axis and a five-state Generic controller. Keep existing controllers/assets intact; validate the new caster clips against actual imported bones and binding paths. |
| Blender design export | Whole-study offsets and different FBX axis convention are not drop-in gameplay assets. Export reusable semantic mesh/transform tracks, and independently validate Unity axis, winding, scale and placement. |
| SpellFxCapture / starter skills | Some impact cells use canonical anchors instead of first actual footprint contact. Record validated physical contacts before resolution; retain a private anchor snapshot to calculate actual contact displacement. |
| Ground Surge | A reachable empty line already writes ground charge but reports failure and drops capture. Reproduce, then consume and capture that work; zero-reachable-cell cases remain free refusals. |
| Rain capture | No-crop success can contain fallback AffectedCells=[source]. Crop rain must use actual recorded crop targets, not that fallback. |
| Existing ground writes | Their locations are simulation authority. Correcting physical impact capture must not silently relocate Ember/Rime/Jet ground state. |

## Implementation sequence

1. Preserve source/dirty-work baselines; run headless EditMode with MCP started
   and settled first. Record compiler errors before reading fresh XML, and
   compare later failures by exact case against this run.
2. Complete Blender P1 visuals and sampled-motion RED→GREEN checks. Export
   real meshes, static colors and 100-fps transform tracks with explicit role,
   phase, anchor and outcome conditions. Retain editable source and previews.
3. Add failing native import/renderer/caster and gameplay-contact tests.
   Implement a library/importer, pooled renderer and exact clip bindings.
   Preserve source/target/range independence and never stretch a whole studio
   assembly across a gameplay line.
4. Route through WorldFxCoordinator and the active native surface. Unknown
   spells and inactive 3D presentation follow the established fallback.
5. Verify every starter family through real gameplay commands, with paired
   success/counter outcomes, multi-cell contacts, actual displacement, no
   duplicate owner impact, changing visibility, mode/speed changes, loading,
   zone transitions and cleanup. Run a separate adversarial gate and cold-eye.
6. Inspect actual native screenshots and paced playback, profile 60–90 seconds,
   rerun appropriate regression/full suite, preserve user save/scene settings,
   update this plan and the daily work log, and reopen the game for play.

## Runtime and export contract

The native library contains seven entries and reusable pieces from Blender,
not hand-recreated stand-ins. Tracks sample frames 0–110 at 100 fps. Semantic
roles anchor to source, traveling path, recorded affected cell or actual target
contact; runtime maps variable travel to the authored contact phase. All
samples and bounds must be finite. Color is static opaque material data.

The importer creates owned meshes, a fog-aware FX material, the library and
seven normalized caster clips under Assets/Art3D/SpellFx with a Resources
library reference. It validates the entire input before replacement and keeps
stable GUIDs on rebuild. Existing production art and AnimatorControllers are
borrowed. New .meta GUIDs receive a collision audit.

The source pose is played on the existing actor; fixed roots and existing
equipment sockets remain intact. Facing derives from the copied cast direction.
The player may interrupt settling motion with their next accepted action;
gesture cleanup must not extend the simulation wait or leave Animator speed,
controller or transforms modified. Missing/incompatible rigs retain fallback.

Transient success accents never imply a denied status. Persistent statuses
stay owned by native state views. Rain affects only recorded crops; Calm does
not heal, recruit or sleep; frozen/wet/charge/fire variants follow copied
outcomes, including death and rejection. New geometry does not add collision,
damage, displacement, loot or occupancy.

## Performance and visibility

Reviewed Docs/PERF-FOUNDATION.md. Use bounded pools, reusable scratch lists and
preloaded seven-entry lookup; no LINQ, hierarchy search, resource load or
per-frame collection allocation. Prepare geometry once during import. Add
profiling markers around native effect update and measure normal gameplay
after implementation; Editor timing is not a build-performance guarantee.

Every fragment obeys physical-cell fog clipping in color and depth. No global
screen flash, light animation or camera shake is added by the art refinement.
Reduced mode lowers secondary detail while preserving the main spell reading;
Off disables transient spell views. Surface replacement/disposal, visibility
loss, pause, zone/load and external queue clear cancel all owned effects and
release blocking playback. The renderer never computes FOV or changes terrain.

## Honesty bounds

Automated checks can prove copied outcomes, transform placement, mesh/material
contracts, cleanup, timing and test comparisons. Actual native captures and
paced playback are required for visual acceptance; unit tests and Blender
previews alone cannot establish native readability, comfort or performance.
The first 3D coverage is the existing native town and surrounding ring; this
task does not convert every other world chunk to 3D.

## Implementation log

- Read CLAUDE.md, Unity orchestration skill, current design, native surface,
  coordinator, presenter and importer contracts, and performance foundation.
  Archived 7,058 source-file hashes plus pre-existing dirty status before
  runtime edits. Unity was idle, with SampleScene clean, before graceful close.
- S3D00 correctly refused to launch while closing Editor import workers still
  existed. No tests ran in that attempt. S3D00b baseline: **10,902/10,933 PASS**,
  exactly the same 31 failed cases as MC26, zero compiler errors and zero skips.
- Source/runtime export contract agreed: seven typed entries, mesh pieces with
  explicit source/projectile/target/cell anchors and outcome conditions, and
  111 uniform transform samples at 100 fps. Native library resource path is
  SpellFx3D/Library. Rain uses actual crop targets; an empty cast's fallback
  source cell must not produce crop rain.
- New import RED tests require the actual seven-entry resource, finite authored
  meshes/tracks and caster clips that move the existing native skeleton and
  return it to rest without root motion. Renderer and capture RED cases are
  being authored independently before any production implementation.
- S3D01 recorded missing-API compiler RED; no tests executed. After introducing
  a compilable API scaffold, S3D02 executed **23/119 PASS, 96 RED** before the
  implementation wave. The capture slice was **20/55 PASS, 35 RED**.
- S3D03: **117/157 PASS**, zero compiler errors. All initial renderer/coordinator
  and 55 capture tests passed. A reaction-first Flaming Hands probe exposed
  physical contact being replaced by a large owner's anchor; the reaction
  capture now registers its actual cell after live-owner and duplicate guards.
  New helper tests initially had an Activator overload error, and the town
  fixture incorrectly used the authored-owner lookup for the player. Both
  harness defects were corrected without changing their assertions.
- S3D04 executed corrected harnesses: **71/83 PASS**. All **59 capture tests
  passed**; two initial-speed cases confirmed double scaling in the new cast
  helper. Ten presenter cases reproduced missing gesture/surface hooks and
  diagonal facing. The helper now treats hook duration as already scaled wall
  seconds; presenter lifecycle and all-eight-direction hooks were implemented.
- S3D05 explicit import: **PASS**, zero compiler errors. Imported 129 authored
  meshes / 2,928 triangles and seven gestures from polished source SHA
  `39ea9e4134d839be88f9f44fcb87c4bd7ee2936ec6300d6fadfd3930165d3e39`.
  Actual imported source/native bone bases differ by **0 degrees**. The native
  town path is `character-teal/character-teal__Rig`, and ring exports rename
  armatures; the importer bakes a verified clip binding for each compatible
  native rig path, preserving existing shared rigs and controllers.
- S3D06: **176/183 PASS**, zero compiler errors. Actual town/ring bone motion,
  rest return, imported geometry, presenter interruption and eight directions
  passed. Seven new cold-eye cases correctly failed: private controller reuse
  (two), malformed animation clocks / excessive cast duration (three), and
  coordinator waits surviving native hierarchy/surface loss (two). These
  bounded fixes are undergoing their next GREEN gate.
- Art P1 replaces abrupt reveals with eased growth/settling, adds brighter local
  accents, and removes Rain's visible upward reset. Canonical Reduced Rime now
  keeps one actual neutral ice chip when Frozen is denied. Runtime export SHA
  `b01aa7c1f5f4e5a42e88b4efd6e8e90d7eb3a5be1078845c05ebad24ff0e3448`;
  source/FBX/media remain at their recorded P1 hashes. Art receipts are
  separate from pending rendered native acceptance and performance evidence.
- S3D07: **186 runtime/capture/import/helper/presenter tests GREEN**, with the
  12 new native audit metadata tests correctly RED. S3D08 then established
  two actual movement-handoff REDs (0.35-cell visual offset); presenters now
  snap to their resolved destination before the gesture starts. The 12 new
  GPU report tests were also RED before their probe implementation.
- S3D09 actual Metal GPU probe: **129/129 imported mesh faces** render from
  outside and disappear from inside; six of seven full contact assemblies
  render, all memory/unseen/hidden counterframes are blank, and borrowed scene,
  settings and GPU state are restored. **Rain failed at frame 22**: its first
  drop was only 3.55% scale, about 0.09 by 0.52 pixels in the probe. Nonzero
  geometry was insufficient visual evidence. The source reveal is being fixed
  without weakening the pixel gate or changing contact time. The native Editor
  process returned zero despite the failed report; acceptance always reads the
  fresh report, never process exit alone.
- S3D10: **203/213 PASS**, zero compiler errors. Movement handoff and GPU
  metadata tests pass. Ten new semantic REDs exposed unchecked role/anchor
  combinations (seven) and ambient oil reactions redirecting the Hands fan or
  receiving extra Ember/Rime direct-hit art (three). Original simulation
  reactions remain authoritative; the renderer's primary cast geometry is
  being separated from unrelated captured secondary victims.
- S3D11 imported the final Rain reveal correction from source SHA
  `cfaf8301d5829669f5026eea89b4d46201fad80bb4ebd33496bdfa817bea03e3`
  and runtime JSON SHA `f4f11c82ded4d4fa15ba93d58159fc3506369abfd0f12b23f36c8fbd55fc2d30`.
  S3D12 then passed **225/225 targeted cases**, zero compiler errors/skips.
  S3D13 actual Metal GPU acceptance passed **14/14 rows**: all 129 mesh
  exteriors, seven contact assemblies and paired hidden-cell controls. Rain
  now produces 33 visible contact pixels. Scene/settings preservation passes.
- S3D14 failed its first ordinary keyboard cast gate. Per-frame evidence shows
  ten native meshes rendered, but the harness began its local peak/blocking
  observation after the key-release helper had already waited out the effect.
  Its observation now spans the actual direction press and records each
  gameplay predicate explicitly. The failed receipt remains unchanged.
- S3D14 also measured a real **546.57 ms Main Thread / 547.42 ms frame** first
  cast, with the library appearing during the cast and about 41 MB additional
  allocated memory. The next frame's elapsed time skipped most of the new
  animation. Verified cold path: synchronous resource/dependency load,
  validation and view creation inside Play. Next bounded milestone: idempotent
  preparation on a usable native surface before casting, with no work for
  hidden/absent/Off views; reserve the existing Full/Reduced pool cap and record
  preparation timings separately. This moves initial preparation into view
  setup; it does not claim that loading costs disappear. Add readiness REDs,
  rerun native first-cast timing and then the complete command/profile gate.
- S3D15 executed **0/9 PASS, nine readiness REDs**, zero compiler errors.
  Implemented bounded preparation at visible native binding/enabling. Initial
  load, validation and pool construction have separate retained timings;
  hidden/Off/ASCII views skip cold work. Added actual runtime gesture evidence
  to the native driver: expected override/state/weighted clip, multiple frames
  of bone motion, then original controller/speed/update mode and Idle. These
  observations never manually advance animations and are excluded from steady
  profile phases. S3D16 is the combined targeted GREEN gate.
- S3D16 passed **234/234 targeted cases**, zero compiler errors. S3D17's
  ordinary keyboard cast passed with library 1→1 and pool 384→384; first-cast
  Main Thread maximum fell to **23.04 ms**. Initial preparation separately
  measured load **513.59 ms**, validation **3.60 ms**, pool **2.72 ms**.
  Its next gesture comparison failed because the observer could snapshot the
  preceding cast's still-active override: effect lifetime .60 s, gesture .66 s,
  keyboard helper returned at .6119 s. Actual Ember motion was 67.55° over 73
  samples. The isolated comparison now waits for natural Idle first; normal
  gameplay still permits interrupting that short settling tail.
- S3D18 passed **78/78 native checks**, all seven actual gestures, applied and
  denied status pairs, empty Rain, ordinary keyboard input and the south seam.
  It retained **24,782 frames / 81.780 s**, 65 real skill-command fixtures and
  108 PNGs; all run/save/scene/view/preference/hash cleanup gates passed with
  zero unexpected errors. This is native Editor evidence, not a build or
  long-session performance guarantee.
- Final native visual review found persistent ASCII aura '+' particles large
  enough to crowd the imported contact art. Verified source is the two private
  SpawnAuraParticle branches and the static Off-mode aura marker, separate from
  explicit particles/damage-number requests. Bounded refinement plan: compact
  aura decorations to .35 scale only under the actual visible native surface;
  keep damage numbers and fallback presentation at full size. Mark generated
  aura particles internally, preserve status timing/cells/RNG, and explicitly
  restore identity matrices for other glyphs. Actual Tilemap RED/control tests
  precede changes, followed by native visual/profile and full regression.
- The same actual-native review identified a color-upload mismatch: imported
  rgbaSrgb is already converted to linear, while MaterialPropertyBlock.SetColor
  applies an additional conversion in this Linear project. Unity's primary API
  documentation confirms the distinction between [SetColor](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MaterialPropertyBlock.SetColor.html)
  and [SetVector](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MaterialPropertyBlock.SetVector.html).
  Add seven actual imported MPB swatch REDs and seven actual GPU comparisons
  against a single-linear vector reference and an explicit double-linear
  counterimage before changing the runtime upload. Keep the imported asset
  contract and shared shader intact.
- Showcase isolation correction: prior Ground Surge charge, Jet water and
  Ember residue remained legitimately on the lane because command fixtures do
  not advance world turns. Non-profile showcase/control cases now restore only
  the bounded lane's pre-fixture ground before each command, preserving all
  outside cells and current spell writing. Profile cases retain accumulated
  interactions. This is labelled fixture setup, not a change to gameplay decay.
- S3D19 recorded **244/273 PASS, 29 RED**, zero compiler errors: 22 compact-
  aura failures and seven imported-color failures. Seven aura controls and all
  earlier targeted cases passed. S3D20 initially reported 21 GPU cases passing,
  but its color reference reused a block whose property had first been typed
  by SetColor. That was not an independent raw-vector reference.
- S3D21 used newly constructed reference/counter blocks and retained explicit
  initial/retained/fresh property values and four images per spell. All seven
  actual color cases turned RED; all 14 face/fog cases stayed GREEN. Initial
  shader-vector deviation was .345–.467, fresh-reference deviation zero. Native
  pixels matched the deliberate double-linear counter. Retained-block behavior
  differed for Rain, so no universal retained-type claim is made.
- Changed only the native color upload to raw SetVector, preserving the library's
  linear data contract. Compact aura flags affect only generated decorations and
  static state markers; ordinary glyphs explicitly reset to identity. S3D22
  passes **273/273 targeted cases**, zero compiler errors/skips. Final GPU,
  native gameplay/profile and whole-suite checks follow on these exact sources.


- S3D23 passed **21/21 actual GPU cases**. All seven native contact renders
  match the fresh single-linear references exactly; deliberate double-linear
  counterimages differ. All 129 mesh exteriors and fog controls still pass.
- S3D24 passed **78/78 native checks** after the color/aura refinements, with
  **81.689 seconds / 26,346 frames**, 108 PNGs and all seven evaluated gestures.
  Independent artifact/profile verification passed **419/419** checks. A
  716.734 ms whole-Editor outlier remains in the report and is not attributed
  specifically to spells. This is the pre-ground-indicator refinement capture.
- S3D25 full suite: **11,161/11,192 PASS**, zero compiler errors/skips. All
  **31 failure names and messages exactly match S3D00b**; all 259 added cases
  pass, no baseline case is missing. The full comparison and XML hashes are
  retained under S3D25-final-full.
- Actual S3D24 visual review found a separate remaining obstruction: the
  current cast's ground charge/water/ice glyphs in ZoneRenderer's tile-state
  pass cover the new meshes. This is distinct from stale fixture state and
  generated aura particles. Bounded plan: keep glyph, color, priority, fog and
  simulation state; use a .35-scale upper-corner marker only for a matching
  visible native Village/SpawnRing cell, with identity restored in fallback.
  Generic authored-scene predicates also include 2D Felling/Morrowfast and must
  not select this treatment. Mode settings already request a full redraw;
  source/surface transitions require explicit stale-paint coverage. Actual
  Tilemap RED/control tests precede production edits, then fresh native visual
  acceptance and regression close out the change.


- S3D26 executed **3/16 PASS, 13 RED**, zero compiler errors. The three full-
  size fallback/2D controls passed; compact native matrix and same-zone camera
  loss/recovery assertions failed as intended. Implemented only a cached
  per-cell transform and a one-time full redraw on native visibility changes.
  The shared CP437 assets and simulation state remain unchanged; every draw
  writes either the compact matrix or identity. S3D27 checks the combined
  native spell suite and neighboring fallback/authored-scene behavior.


- S3D27 passed **319/319 focused cases**, zero compiler errors/skips: the
  273 native-spell cases, 16 new ground-marker cases and 30 neighboring
  authored-scene/fallback checks. Independent cold-eye review found no
  additional defect in the ground-marker refinement: native/current-zone
  selection, fallback identity, unchanged state/glyph/tint/priority/fog and
  source-loss/recovery invalidation before the normal redraw gate are verified.
  Fixture teardown detaches borrowed surfaces before their owner releases them.
  These 16 cases use actual Tilemap output and visibility predicates with
  explicitly supplied presenter readiness; visual acceptance remains the fresh
  S3D28 native capture, not the unit fixture.


- S3D28 retained as **FAIL**: Rain's correct crop outcome, native entry and
  restored cast gesture were observed, but a **657.88 ms capture gap** from
  .104 to .762 seconds spanned the remaining .60-second effect, so no rain
  meshes were sampled. Other six showcases passed with ~102–103 ms maximum
  capture intervals. No steady profile phase was reached; the receipt cannot
  attribute the gap to CPU, GPU, screenshots or MCP. There were zero compiler
  or unexpected runtime errors, and scene/save/preferences were restored.
  Independent review supports one unchanged repeat with the same visual gates;
  no runtime clock or acceptance threshold was modified. Diagnostic images
  already confirm the ground marks no longer hide Jet, Surge or Rime, but the
  partial run is not used as a completed preview. S3D28b is the fresh retry.


- S3D28b also retained as **FAIL**. All seven showcases and earlier status
  controls passed, but a Reduced Rime profile cast had no visible samples.
  Raw frame 13,608 recorded **480.188 ms elapsed**, **478.663 ms Main Thread**
  and **66,465,973 allocated bytes**; Input/ZoneRenderer were .024/1.128 ms.
  The interval advanced this .60-second effect past clear. Save/scene/preference
  cleanup succeeded. This is a measured whole-Editor stall; its source is not
  established by those counters. No broad animation-clock change is justified.
- Bounded audit-environment experiment: explicitly warm the installed Editor
  MCP tool/resource discovery caches after Play's domain reload and before
  initializing the acceptance driver. Record the setup separately, including
  absence or actual discovery counts and time; keep native clocks, outcomes,
  captures, acceptance gates and all subsequent performance outliers unchanged.
  This controls a known one-time Editor setup operation, not a demonstrated
  repair of the later allocation pause. S3D28c will test that environment.


- S3D28c passed **78/78 native checks** and **419/419 independent artifact/
  profile checks**, zero compiler/unexpected runtime errors. All 108 PNGs and
  the raw CSV were verified by SHA-256; **25,835 frames / 81.683 seconds**, 65
  deterministic command fixtures plus a real keyboard cast, and 52 profile
  commands cover Full/Reduced town and south views. All seven actual gestures
  moved and restored, and all copied gameplay outcomes stayed stable. First
  keyboard cast reused its library/pool and had a 24.29 ms Main maximum.
- Separate Editor discovery warmup measured **2.8098 ms**, discovering 18
  resources and 34 tools. It does **not** explain or eliminate previous stalls:
  final Main maxima still include **603.20 / 482.73 / 19.26 / 570.39 ms** across
  the four phases. The earlier failed runs remain evidence that a sufficiently
  long Editor pause can skip a short wall-clock effect. No timing cap or test
  threshold was changed to hide this. Diagnosing those whole-Editor stalls or
  making standalone-build performance guarantees is outside this bounded art
  integration; the measured limitations remain open.
- Independent final visual review accepted all seven identities, clear target
  faces, compact ground indicators and absence of stale fixture overlays.
  Delivery media uses only the complete S3D28c run, with recorded timestamps
  and explicit inspection crops, without interpolated or invented motion.


- S3D29 final full suite: **11,177/11,208 PASS**, zero compiler errors/skips.
  Exact failure names and unnormalized messages match all 31 S3D00b failures;
  all **275 added cases pass**, with no removed or duplicate test names.
  The comparison is reproducible from retained XML and its hashes.
- Final preservation audit passes against 7,058 pre-existing files: changes
  are confined to the 12 authorized integration files, with no unrelated
  changes or missing files. All 4,031 asset metadata files have unique GUIDs.
  Unity's automatic QualitySettings schema rewrite was restored only after
  matching the original working-tree hash to HEAD; no user settings edits
  were discarded. Unity was reopened on the real SampleScene, confirmed idle,
  neither compiling nor importing, with no tests running.

## Delivery

- [Actual Unity animation preview](Verification/StarterSpell3D/Integration/NativeMedia/SSN-0d7a74963b314b6f9c9270a00d13846d/starter-spells-unity-native-timing.mp4)
  and [inline loop](Verification/StarterSpell3D/Integration/NativeMedia/SSN-0d7a74963b314b6f9c9270a00d13846d/starter-spells-unity-native-timing.gif).
- [Refined inspection sheet](Verification/StarterSpell3D/Integration/NativeMedia/SSN-0d7a74963b314b6f9c9270a00d13846d/starter-spells-unity-contact-sheet-refined.png)
  and [capture provenance](Verification/StarterSpell3D/Integration/NativeMedia/SSN-0d7a74963b314b6f9c9270a00d13846d/README.md). Calm and Rain use clearly marked
  4× detail crops; the other local scene cards use 2×. Rain remains a subtle
  crop-local effect at the actual game scale.
- [Final native performance and limitations](Verification/StarterSpell3D/Integration/S3D28c-warm-editor-native/performance.md),
  [full-suite comparison](Verification/StarterSpell3D/Integration/S3D29-final-full/baseline-comparison.md),
  and [daily change list](WORK-LOG-2026-09-11.md).

The new resources load automatically in the existing native town and ring
views. Normal new games retain their six default actives; Conjure Rain remains
separately learnable. No save migration or manual scene setup is required.

## Files and verification

Runtime ownership: native renderer/library and WorldFxCoordinator; presenter
surface/cast hooks and importer; scoped starter capture fixes and their tests.
Blender ownership: ArtSource/StarterSpell3D. New receipts live under
Docs/Verification/StarterSpell3D/Polish01 and Integration. Historical design
receipts remain evidence for their recorded source hashes.
