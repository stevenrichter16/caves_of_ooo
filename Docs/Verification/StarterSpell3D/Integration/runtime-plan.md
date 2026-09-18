# Native starter spell playback — runtime integration plan

Status: **complete for the seven-starter-spell integration**. Native playback and
visual corrections, including compact persistent auras/ground markers and
source-loss repainting, are implemented and verified. **S3D27: 319/319 targeted
tests PASS**, zero compiler errors; **S3D23: 21/21 actual GPU cases PASS**;
**S3D28c native gameplay acceptance PASS**, including seven imported gestures,
real keyboard casting and four measured profile phases. Its independent
artifact/performance audit passed **419/419** checks: 108 verified PNGs and
25,835 raw frames over 81.683 seconds.
**S3D29 final full suite: 11,177/11,208 passing**, zero compiler errors/skips;
all **31 failing names and messages exactly match S3D00b**, and all **275 added
test cases pass**. No baseline test was removed and no new failure was introduced.
S3D24 remains a source-specific pre-ground-refinement snapshot. S3D28 and S3D28b
remain failed timing-stall receipts; the 2.8098 ms Editor discovery warmup in
S3D28c does not explain or establish a fix for their 480/658 ms gaps. Large
whole-Editor frame outliers persist in the accepted run, with no runtime-clock
or acceptance-gate changes. This is CoO-original presentation, without a Qud
parity claim.

## Ownership

This work owns new `NativeSpellFxLibrary` data and `NativeSpellFxRenderer`, their
tests, and the `WorldFxCoordinator` adapter. Root owns exposing the current native
surface, presenter cast gestures, the editor importer, gameplay observation
corrections, Unity runs and live acceptance. The artist owns Blender extraction.
Existing design studies and their receipts are retained separately from native
runtime acceptance.

## Verified facts and corrections

| Verified code | Consequence |
| --- | --- |
| `WorldFxCoordinator.Update` alone drains `SpellFxBus` and owns clocks/wait/cancellation | Add a backend beneath it; never add another queue consumer. |
| `SpellFxSequence` copies visited cells and actual target results before removal/movement | Playback uses these values and never runs skills or reads live target positions. |
| `NativeZone3DRenderSurface` owns a borrowed camera's native XZ projection and fog texture | Root supplies the active surface. Effect objects share its layer and camera, without changing either. |
| Surface `MaterialFor` rejects unknown materials | Native effects own a small set of material clones using the existing fog-aware palette shader and bind the surface texture directly. No change to surface registration is required. |
| Palette shader clips actual world-XZ fragments in color, depth and shadows; `_Transient=1` rejects remembered-only cells | Preserve pixel-level fog clipping, and disable effect shadows. CPU anchor visibility is an additional check, not a substitute for fragment clipping. |
| `NativeSpellCastPlayer` overrides `Interact` with each imported seven-spell gesture and restores the borrowed controller | Town/ring rig-path bindings select the actual compatible clip; unknown definitions retain the existing interaction fallback. |
| Conjure Rain with no crops succeeds and capture falls back to `AffectedCells=[source]` | Crop effects use deduplicated recorded target results, never the fallback affected-cell list. Empty-handed casting can still animate. |
| Jet Blast affected cells contain reachable cone geometry plus later ground/outcome writes | Select native fan pieces by recorded cells matched to authored forward/lateral offsets; do not fill a fixed studio cone or reconstruct live wall tests. |
| Multi-cell owners may be touched in several cells | Deduplicate target-impact accents by captured owner identity while retaining independent actual ground-cell effects. |
| Existing samples are static meshes plus authored transform animation at 100 fps | Extract reusable semantic pieces and preserve their sampled motion; do not substitute generic runtime shapes. |

## Implemented asset contract

The library maps actual skill IDs to immutable mesh/color/sampled-pose data.
Each part has a stable ID and semantic role: source gather, projectile head/trail,
target impact, path cell, cone cell, or watered crop cell. Source/stage actors,
training recipients, plinths and fake crops are excluded. Local origins belong to
their semantic anchor, rather than the studio's actor X=-2.35 offset.

The uniform pose samples retain authored cast, contact and clear markers.
Runtime retiming maps the variable recorded path travel interval to the authored
travel interval; it preserves cast and post-contact choreography. The exporter
must verify any matrix-to-TRS conversion rather than losing parent shear silently.
Rime success clamps and Calm success arcs require the corresponding captured
applied status. Rejected/dead outcomes receive only the appropriate transient
contact/release branch, not an invented surviving status.

## Runtime invariants and tests

1. Native eligibility requires the matching ready visible surface, known valid
   library data and the current zone. Unknown definitions retain existing fallback.
2. One sequence resolves no gameplay a second time and consumes no gameplay RNG.
   Owner impacts deduplicate; path and crop anchors remain actual recorded cells.
3. Variable paths and cardinal/diagonal facings reposition semantic anchors without
   moving the caster or scaling an entire fixed-distance studio assembly.
4. Visibility clips every fragment, including overhang into hidden cells. Hidden
   anchors, unexplored cells and disposed surfaces do not reveal an effect.
5. Full/Reduced/Off and animation speed obey the coordinator. Reduced drops only
   nonessential decoration. Pending native work participates in input waiting.
6. A hard bounded lifetime, mode changes, surface/zone/load changes, cancellation,
   parent destruction and disposal reclaim all owned transient objects/materials.
   No meshes, materials or cameras borrowed from assets/surface are destroyed.
7. No physics collider or gameplay entity is created. An effect never changes
   occupancy, targeting, damage, cooldowns, ink, status lifetime or saves.

Author failing behavior tests before production, including counterconditions.
Then run a dedicated adversarial fixture for malformed data, reentrancy/lifecycle,
duplicate owners, hidden contacts, invalid paths, budget exhaustion and fallback.
Root records actual RED/GREEN and native acceptance rather than claiming an
unexecuted test plan is evidence. A post-green cold-eye pass checks symmetric
cleanup, per-outcome coverage and doc-versus-code agreement.

## Performance

Follow `Docs/PERF-FOUNDATION.md`: cache library entries and owned material clones;
pool transient draw records/views; no LINQ, collection construction or asset
lookups in the frame update; gate absent/hidden effects before frame work. Keep
explicit scheduled and live budgets. Shared meshes and authored samples remain
immutable. Root's final native workload must measure the actual spell presentation
and keep worst-frame/GC outliers; edit-mode timings are not GPU proof.

## Implementation log

- Read project methodology, current design/art contract, performance foundation,
  coordinator, sprite backend, native surface/fog shader and starter skill captures.
- Found the zero-crop fallback and mixed Jet affected-cell semantics during the
  pre-implementation sweep; communicated both to root and artist before coding.
- No production edits or Unity results are claimed by this initial plan.

### First native implementation, awaiting combined GREEN

- Parent archived the cleanly compiled **S3D02 behavioral RED**: 119 cases,
  23 pass / 96 fail, zero compiler errors. The runtime subset failed on the
  no-op renderer and missing validation, as expected; the actual XML was read
  before production code was written.
- Implemented immutable library validation/cache, native backend and coordinator
  routing. The exported 129 meshes remain borrowed; only transient views and one
  fog-bound material clone are owned. There is no second queue consumer.
- Accepted casts use copied target IDs/contact cells, unique paths, exact cone
  offsets matched to recorded affected cells, actual watered targets, and positive
  `freeze_water` reactions. Applied status strings are `FrozenEffect` and
  `Pacified`; success clamps require a living target with the actual applied result.
- Piecewise timing preserves the 0.22-second release and authored post-contact
  choreography while changing the copied path's travel interval. Head placement
  retains the real 0.36-metre muzzle, height, lateral and mesh-relative offsets,
  follows copied bends and allows the artist's small carrier follow-through.
- Per-cell authored geometry rotates with the cast. Diagonal casts use a
  horizontal 1/sqrt(2) fit, leaving height unchanged, so the clipped cell pieces
  stay within one native cell. The artist agreed this explicit native adaptation;
  all delivered per-cell tracks rotate only about the vertical axis, which makes
  that TRS fit valid for this library.
- Native transient meshes use the existing world palette shader, bound borrowed
  fog texture and `_Transient=1`. CPU checks re-evaluate visibility every frame;
  GPU world-fragment fog clipping remains the overhang protection. No new lights,
  shadows, colliders, entities or persistent ground-state visuals are created.
- Budgets: at most 64 accepted casts, 768 scheduled fragments, 384 live full-mode
  meshes or 96 reduced-mode meshes. Views are grown on cast acceptance and reused;
  the update loop does not instantiate objects, load assets, or allocate lists.
  Excess geometry is bounded cosmetic loss; it does not change gameplay outcomes.
- The coordinator keeps native work in its existing timeout/cancellation clock.
  Cast hooks receive the whole authored gesture divided by animation speed,
  while turn waiting uses the effect's own lifetime. Unknown/malformed native art
  uses the established fallback, with `LastEntry` cleared on refusal.
- Sparse `effect` diagnostics distinguish `NativeSpellScheduled`,
  `NativeSpellRejected`, `NativeSpellCancelled` and `NativeSpellCompleted` and
  include native backend, spell, zone, piece count, reason and duration. No
  per-fragment or per-frame diagnostics are emitted.
- Additional counterchecks cover actual callback timing, no extra turn wait,
  stale success metadata, variable range/cardinal/diagonal placement, copied bends,
  hidden source versus visible projectile cells, and lifecycle diagnostics.
- Assets were frozen for the parent's first combined GREEN attempt. No execution,
  GPU acceptance, allocation timing or final art quality is claimed by this entry.

Read-only cold-eye finding queued for the next fix wave: in the delivered data,
all Rime `Always` chips/threads are marked nonessential, so Reduced mode suppresses
all contact feedback after a rejected Frozen. Root and artist were notified;
recommend retaining the first actual drifting chip as essential. This does not
require new or substitute geometry.

### First GREEN and cold-eye expansion

- Read the archived **S3D03-first-implementation/results.xml**. The renderer,
  adversarial and existing coordinator slices were **59/59 passed**, zero compile
  errors. The combined run was 117/157; failures outside that slice remained in
  absent imported art/presenter hooks, the root's reflection harness and two
  gameplay capture cases. This is not a full-game or native GPU pass.
- Cold-eye found a real boundary not covered by the first direct-backend test:
  destruction of the FX root or loss of surface visibility cancels native
  fragments, while the coordinator's independent playback handle can still wait
  until its old duration. Added two coordinator-level negative assertions before
  fixing; the proposed remedy cancels corresponding native handles, preserving
  unrelated fallback playbacks and pending new work.
- Added malformed 100-fps-clock and excessive-duration counterchecks before
  hardening. A tiny positive sample rate previously remained structurally valid
  and would make direct backend playback unnecessarily long; imported schema is
  explicitly 100 fps. Added the actual imported Reduced-Rime rejected-status
  check. Artist captured independent canonical-data RED before changing the
  first neutral chip's essential flag.
- Parent found that ring humanoid rigs preserve bones but rename their rig root,
  so a town clip's binding paths do not resolve there. Extended the agreed schema
  with `NativeSpellCastBinding { RigPath, Clip }`, `Entry.CastBindings`, and
  `FindCastClip(Animator)`. It selects only a path present under the actual native
  animator; the compatibility `CastClip` fallback requires `VillageRig`. Unknown
  or unrigged models return null. Parent owns baking each compatible rig variant.
- Added the town/ring/unknown selector countercheck and all seven imported
  ring-player checks: every curve path resolves, actual bones move at release,
  return to their own rest, and actor root remains unkeyed. These and the new
  cold-eye cases await the parent's next execution; no pass claim yet.

### Second RED→GREEN and independent source review

- Read **S3D06-native-import-gate/results.xml**: the five new runtime failures were
  exactly the two non-100-fps clocks, one hour-long cast duration, and the two
  coordinator cancellation boundaries. All real imported motion, town/ring
  bindings, eight-direction facing and Reduced neutral Rime checks passed.
- Fixed those five runtime failures. Native cancellation now releases only native
  playback handles; unrelated fallback handles and newly pending work survive.
  Also covered acceptance of a new cast immediately after external hierarchy loss,
  so cancellation of an old cast cannot cancel the new handle. Existing cast and
  FX lifetimes remain independent.
- Replaced the provisional fallback rig name with the importer-verified native
  `character-teal/character-teal__Rig` path. Unknown/studio rig names cannot
  authorize a clip with different binding paths.
- Parent's **S3D07-cold-eye-gate** recorded **186/186 runtime/helper/capture/import/
  presenter cases GREEN**, zero compiler errors; twelve separate new audit-metadata
  tests were intentionally RED. Native pixels/performance remain separately gated.
- The requested independent importer/presenter review found one additional real
  handoff issue in both presenters: beginning a cast midway through the .10-second
  movement tween stopped the tween without placing the actor at its destination.
  That can separate the caster from FX anchored at the copied source cell. Root
  accepted the finding and is authoring actual-hook RED tests before fixing it.
- Independent read-only provenance audit: **45/45 checks passed**, including
  byte-identical imported FBXs, recorded source/derived clip GUIDs, saved-study and
  runtime-JSON hashes, donor source and preserved v1 receipts. All **1,478** existing
  town/ring art files still match the pre-integration hashes. See
  `runtime-source-provenance.json`. The design manifest's `source` points to its
  donor village Blender file; its hash was checked against that declared path,
  not confused with the later spell-study source hash.
- The source caster keys bone rotations only. Its root offset is static and
  its pose locations/scales are not animated, so the importer's 67-frame local
  quaternion extraction does not discard an authored translation or scale track.
  Stage actors are instantiated only during editor baking and destroyed in
  `finally`; runtime library entries reference extracted meshes/clips, not studio
  prefab assemblies. GPU winding remains the native acceptance gate.

### Final semantic RED and native acceptance launcher

- Read **S3D10-semantic-red/results.xml** before changing production: seven
  malformed Role/Anchor pairs were silently accepted; Hands could aim toward a
  pre-existing ambient oil victim; Ember/Rime could replay their direct impact
  on that unrelated reaction victim. Three matched controls already passed.
- Added canonical Role/Anchor validation. Hands now takes its primary selected
  cell from the first recorded affected cell. Ember/Rime direct impact pieces
  require the final copied projectile path cell. Whole-zone reaction outcomes
  remain recorded, and separate reaction/crop effects retain their own semantics.
  These fixes await the next centrally controlled Unity test run.
- Reused the reviewed MultiCellPilot native launcher isolation/scene/view cleanup
  for `StarterSpell3DNativeAuditBatch`. The launcher requires a unique current-run
  receipt, all seven spells, four measured 20-second Full/Reduced town/south phases,
  real keyboard casting, independent counter availability, raw CSV SHA and every
  captured PNG's cast-owned SHA. Root owns the driver and actual Editor run.
- Launcher cold-eye caught the draft's 14-column check against the actual
  13-column CSV writer. Corrected it, checked named counter units/availability,
  monotonic raw samples, phase counts and maxima, and bijective PNG-to-cast
  provenance. Native pixel quality and timing remain unclaimed until that run.

### Native first-cast measurement and readiness RED

- **S3D14 native**, run `16ac5dab4d1a4e579536be63b2ff95f9`, preserved its failed
  acceptance receipt. Its LateUpdate samples contain actual native meshes even
  though the driver's later local observation missed them. Independently, it
  measured a **546.57 ms Main Thread maximum**, a **547.42 ms frame**, library
  instances **0→1**, native pool **0→14**, and total allocated memory
  **445,559,034→486,525,900 bytes** around the first keyboard cast. This is actual
  native-editor evidence, not a unit-test speed claim. The driver observation
  correction is parent-owned and does not excuse the measured hitch.
- Source inspection confirms the first `NativeSpellFxRenderer.Play` loads the
  complete Resources library and clip dependencies, validates all sampled poses
  and meshes, clones its material and creates views. The following Unity delta
  then includes that synchronous work and advances most of the effect at once.
- Proposed measured fix: prepare the complete library and bounded native pool
  when a valid visible native surface binds, before a cast is pending; skip
  Off/hidden/ASCII/absent surfaces and prepare when they become enabled. Full
  reserves the existing 384-view cap and Reduced 96. This moves unavoidable cold
  setup to presentation readiness; it does not claim to eliminate load cost.
  Record separate load/validation/pool seconds and final view count.
- Authored nine readiness RED cases before production changes: real cache and
  pool state before any cast, all disabled controls, mode transitions, repeat
  idempotence, malformed data without partial resources, and the actual imported
  Rain at maximum 49-cell crop coverage with no new first-cast views. Root owns
  the central S3D15 RED run and native remeasurement. Controller-specific first
  binding is independently reviewed by the parent's other agent.

- Read **S3D15-readiness-red/results.xml**: all nine new readiness assertions
  failed, zero compiler errors. Implemented `NativeSpellFxRenderer.Prepare` and
  coordinator binding/enable checks. Validation uses the existing cached `Find`
  guard; no-op readiness does no load, validation, allocation or new diagnostic.
  The original preparation receipt survives all no-op calls. Failed validation
  records a sparse refusal and cannot leave a partial pool or be retried on every
  frame. Direct backend users retain their supported lazy fallback path.
- In-phase self-review: preparation after external hierarchy loss cancels the old
  native handles before rebuilding; wrong/disabled surfaces create no resources;
  existing full pools remain reusable in Reduced; dispose cannot restart readiness.
  Added receipt-preservation and terminal disposal checks to the nine-case gate.
  Next GREEN and native cold/prepared comparison remain parent-owned.

### Native acceptance and single-linear color gate

- **S3D16** passed all **234/234** targeted cases with zero compiler errors.
  **S3D17** independently measured preparation at 513.59 ms loading, 3.60 ms
  validation and 2.72 ms pooling before input. The first cast kept library 1→1
  and pool 384→384; Main Thread maximum was 23.04 ms versus S3D14's 546.57 ms.
  Its later failure concerned the separately observed caster state, not readiness.
- **S3D18** completed native gameplay acceptance: all seven actual gestures and
  commands, one ordinary keyboard cast, and four measured profile phases. The
  independent intermediate report is `S3D18-native/performance.json`/`.md`:
  **418/418** checks, 108 verified PNG hashes/full decodes, 24,782 raw frames,
  81.780 profile seconds and 52 profile casts. Preserve the reported town
  Main Thread outliers (688.69/484.28 ms); these whole-editor counters do not
  attribute them to the spells. This capture precedes the final visual refinement.
- Art review found double-linear color conversion: the importer already converts
  each exported sRGB swatch with `.linear`; runtime then calls MPB `SetColor`.
  The [Unity 6 SetColor contract](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MaterialPropertyBlock.SetColor.html)
  specifies sRGB input and another conversion for a Linear project. The library
  contract remains correct. The proposed runtime fix uses a raw vector upload.
- Authored seven actual-imported-swatch RED cases before changing production:
  read shader-facing MPB vectors and discriminate a deliberately second-linear
  counter. Extended the isolated actual-GPU probe from 14 to 21 cases, preserving
  all face/fog tests and adding native versus same-pose single-linear reference
  versus double-linear counter PNGs for every spell. GPU receipt acceptance now
  rejects color mismatches and an indistinguishable or nonfinite counter. These
  tests await central RED; runtime remains on `SetColor` until that evidence.

- **S3D19** confirmed seven intended swatch RED failures (vector deviations
  0.345–0.467), with paired control prerequisites passing. However **S3D20 GPU
  passed all 21 cases while production still used `SetColor`**. Preserve that
  result; it did not demonstrate a GPU color defect. Cold-eye identified that
  the reference overwrote an existing Color-typed block, so it might retain the
  original conversion metadata rather than independently supply a raw vector.
- Instrumented the probe with imported swatches plus initial/retained/fresh
  `GetVector` and `GetColor` data. A separate retained-block diagnostic frame is
  saved; the independent reference and wrong-conversion counter now each use
  newly constructed MPBs containing only the owned `_BaseColor` property.
  Production remains unchanged until this actual GPU comparison is run.

- **S3D21 actual GPU** confirmed all seven intended color failures with fresh
  independent blocks; all fourteen face/fog cases stayed GREEN and compilation
  had zero errors. Initial blocks show extra conversion for all seven; retained
  vectors keep that deviation for six cases (Rain's retained-vector diagnostic
  differs). Freshly created vector blocks have zero CPU deviation in every case.
  Native pixels match the deliberate double-linear counter instead of the fresh
  single-linear reference. S3D20's prior PASS is retained as harness history.
- Applied the minimal runtime change: `_BaseColor` is now inserted with
  `MaterialPropertyBlock.SetVector` from the already-linear imported/interpolated
  value. This keeps its property type consistently Vector from first use, retains
  alpha and flash settings, and changes no imported swatch, geometry, timing,
  material ownership or gameplay. Combined unit and actual GPU GREEN are pending.

### Final targeted, GPU and native acceptance

- **S3D22** passed all **273/273** targeted tests with zero compiler errors.
- **S3D23** passed all **21/21** actual GPU cases. All seven native uploads now
  have zero initial vector error and zero native/reference pixel difference;
  every saved native/reference PNG pair has identical SHA-256. Each deliberate
  double-linear counter remains measurably different. The fourteen face/fog
  cases continue to pass. All earlier RED and flawed-reference receipts remain.
- **S3D24** native run `b89c353ebeba43cdbbdfb69b16949f83` completed all seven
  real command/gesture observations, one ordinary keyboard cast, the town/south
  transition, four paired Full/Reduced profile phases and isolated cleanup.
  Independent `S3D24-final-native/performance.json`/`.md` passed **419/419**:
  **108 PNG hashes and full 1920×1080 decodes**, **26,346 raw CSV frames** over
  **81.689 seconds**, **8,184 active-mesh frames**, and **52 computed profile
  commands** within 65 total command fixtures. The generalized script independently
  cross-checks profile casts against the sum of phase counts. S3D18 is retained as
  an intermediate pre-refinement comparison, not overwritten by final evidence.
- Final Main Thread p99: Town Full **4.662 ms**, Town Reduced **7.174 ms**,
  South Full **3.525 ms**, South Reduced **3.511 ms**. The Town Reduced maximum
  **716.734 ms** remains visible in raw samples and the summary; these whole
  native-editor counters do not identify its cause. Do not infer exclusive spell
  costs, guaranteed hitch-free play, standalone build performance or a randomized
  Full-versus-Reduced speedup from this sequential workload.

### Full-suite comparison and pending ground-marker review

- Compared exact NUnit `fullname` values in **S3D25-final-full** against the
  original **S3D00b-baseline**, including XML hashes. All **31 failing names and
  all 31 failure messages match exactly**. No new failures, removed baseline
  cases or duplicate names; all **259 added cases pass**. Totals:
  **10,902/10,933 → 11,161/11,192**, both with zero compiler errors and zero skips.
  See `S3D25-final-full/baseline-comparison.json` and `.md`.
- Read-only current-runtime review found no additional concrete blocker. The
  pending ground-marker refinement must use current-zone visible Village/SpawnRing
  coverage, per-cell matrices with explicit identity resets, and preserve fog,
  state/color/priority and authored-river suppression. Verified normal redraws for
  native/sprite display toggles, unpause, zone changes and ring claim loss. Review
  source hashes and requirements are in `S3D25-final-full/runtime-ground-review.json`.
  This review is not a pass claim for the not-yet-implemented ground-marker patch.

## Final ground refinement and native acceptance — S3D27–28c

The independent production review found no additional concrete blocker in the
compact per-cell marker transform or source-loss/recovery invalidation. Identity
is restored for fallback, 2D authored scenes retain their original full-cell
markers, and fog, state priorities, tints and gameplay writes are unchanged.
The reviewed source hashes and 319/319 targeted receipt are retained in
`S3D27-ground-green/independent-ground-review.json`; the accepted S3D28c source
comparison is in `S3D28c-warm-editor-native/warmup-and-ground-review.json`.

S3D28 Rain failed around a 657.88 ms capture gap; unchanged S3D28b failed Rime
with a measured 480.188 ms frame delta and no native meshes in its cast window.
Both failures remain preserved with independent analyses. Optional installed
MCP discovery is now timed once during Editor audit setup, before the driver:
18 resources and 34 tools took 2.8098 ms in S3D28c. This does not account for the
prior long gaps, and no causal fix is claimed. No game clock was changed.

S3D28c passed all existing command, sampled gesture, native mesh, PNG, profiler
and isolated cleanup gates. The independent raw/artifact audit passed 419/419:
65 deterministic command fixtures plus the ordinary keyboard cast, 52 profile
commands, 108 hashed/decoded 1920×1080 PNGs, 81.683 seconds, 25,835 frames,
and 7,901 frames with native meshes. Main Thread maximum times remain
603.20/482.73/19.26/570.39 ms in town Full/Reduced and south Full/Reduced.
These whole-game/Editor outliers are included; they cannot be attributed to
spell rendering alone or described as eliminated. The final S3D29 full suite
and exact baseline comparison are now complete, as recorded below.

## Final full-suite closure — S3D29

Independent exact NUnit comparison against the original S3D00b baseline passes:
**10,902/10,933 → 11,177/11,208**, with the same **31 fullnames and unnormalized
failure messages**. All **275 added cases pass**. There are no new failures,
removed baseline cases, duplicate fullnames, skipped cases or compiler errors.
The exact names/messages, XML hashes and reusable comparison script are saved in
`S3D29-final-full/baseline-comparison.json`, `baseline-comparison.md` and
`baseline_comparison.py`. This closes the final source verification for the
seven-spell integration; no Assets changes were made during the comparison.
The prior failed native attempts and the accepted run's measured Editor stall
limitations remain recorded rather than treated as resolved.
