# Ember-only native motion capture proposal

Status: parent-approved bounded implementation; E02 passed all35 recorder tests with0compiler errors. Corrected old-art tests retain11genuine failures/2controls; new Ember import is pending its art freeze. Actual GPU calibration/native movie remain pending. E00 is preserved; Unity is closed after E02.

## Verified sampling gap

`StarterSpell3DNativeAudit.CommandCase` records at `start + sample * .1` with up to 12 samples, calling `ScreenCapture.CaptureScreenshot` within the live cast loop (current lines230–247). Ember's renderer releases at22/100 seconds; its resolved three-cell path adds3×.025seconds, so flight occupies approximately.22–.295seconds of native playback. A normal.2→.3second capture interval can omit the whole flight. Increasing PNG frequency would also put encoding/file work inside this already short interval.

## Small optional appendix

Keep all existing ordinary keyboard, seven-command, denied-status, four20second profile, and15direction gates and capture behavior unchanged. Add an explicit command-line option enabling two separately labelled Ember motion fixtures **after** those workloads: one east and one northeast, using the same real command/queue/renderer/gesture path and existing clear-lane fixture helpers. Do not reuse a storyboard, sample an AnimationClip manually, move the camera, change AnimationSpeed, change application time settings, advance gameplay, or change the recovery clock. The appendix is media evidence, not part of the steady performance measurement.

The extension belongs in one new scenario-only helper such as `StarterSpell3DNativeMotionCapture.cs`, with a small optional driver/launcher hook. Extend `CommandCase` through an optional recorder parameter (default null), or an equally small owned wrapper; retain its current gameplay/gesture/result checks. Existing calls remain byte-for-byte equivalent in behavior. New rows use `ember-motion-*`, so the exact15 `direction-*` requirement is unchanged. A separate run-bound `-ember-motion.json` sidecar retains its own samples and acceptance rather than pretending the original94 checks proved higher-frequency motion.

## Buffer before command; encode after clear

Preflight and allocate only after the original profiles finish. Use1920×1080 end-of-frame `ScreenCapture.CaptureScreenshotIntoRenderTexture` and asynchronous GPU readback into owned preallocated buffers. The Unity documentation explicitly describes this combination for avoiding synchronous main-thread readback. Keep each ring texture unavailable until its request completes; copy completed request bytes immediately because `GetData` is only valid briefly. Never call `WaitForCompletion` during an active spell.

The approved policy is a120samples/second **maximum** concentrated around actual release minus.04seconds through contact plus.02seconds, then30samples/second during settling, with one preflight and one clear image instead of idle streaming. There are32fullRGBA slots and three ring textures:253.125MiB raw capacity plus23.73MiB ring capacity, approximately276.86MiB total. The old64+4 proposal was pruned before implementation. Exact allocation/preparation time must be reported. Slots and callback storage are prepared before the cast. Capture at most once per real rendered Unity frame, never catch up by duplicating an image, never fabricate constant60/120fps timestamps. Record request frame/time and completion frame/time separately. The intended frequency is not a guaranteed achieved rate. A32-slot overflow fails this media appendix; enlargement requires a new measured justification.

No PNG, image hashing, or filesystem writing during the live animation. Once `Native.ActiveCount==0`, no blocking FX remains, the gesture has returned Idle, and the original1.2second observer period has elapsed, drain outstanding requests with a bounded timeout, encode each retained frame, hash it, and release its buffer. Disposal/abort must retain resources while requests own them and release them exactly once afterward. Unsupported readback, request errors, exhausted slots/ring, or an unobserved flight produces explicit incomplete media evidence; it must not silently switch to a slower synchronous path or alter runtime timing to pass.

## Actual motion proof and color calibration

At end-of-frame, record live active mesh identities/roles and transforms using a prebuilt lookup from the imported Ember entry and cached native view components; no new renderer API is necessary. Record projectile head/trail counts, impact counts, the chosen authored head mesh's world position, current frame, actual wall timestamp, and actual native age/study frame if observed through audit-only reflection. Use the observed effect phase, not walltime alone, because legitimate hitch recovery may diverge from walltime.

Require at least three distinct rendered pre-contact samples containing both head and tail, with increasing forward travel along the copied actual path, followed by an actual impact sample and an actual clear frame. If that evidence is absent, retain the failure and timings rather than claiming the cone's flow was shown. Existing native acceptance remains independently reported.

Before this appendix's casts, compare a static same-end-of-frame buffered capture against Unity's normal full-GameView screenshot path. Verify full1920×1080 decoded RGB identity and orientation; a vertically flipped or doubly gamma-converted counter must fail. This prevents a fast capture API from quietly changing colors/orientation. Preserve any required explicit row-orientation normalization in metadata, without resampling or grading.

Offline composition uses the recorded variable walltime holds at1×, full-frame losslessRGB master and separately labelled yuv420p compatibility companion. It reports achieved spacing and every gap, including the actual flight samples. Higher-frequency media never implies higher production frame rate or improved game performance.

## Proposed TDD boundary before implementation

New tests only: default option produces no recorder; non-Ember/profile paths cannot activate it; missing/duplicate/stale timestamps, contact-only images, nonmoving heads, missing clear, buffer overflow, request error, pending-request disposal, and incorrect color/orientation evidence fail. Pair each report rejection with a matching valid record. Actual native GPU pixel calibration and subsequent observed flight remain required beyond unit tests. Existing449 focused and original94/native profile gates remain intact.

## API references

- [Unity6 ScreenCapture.CaptureScreenshotIntoRenderTexture](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ScreenCapture.CaptureScreenshotIntoRenderTexture.html)
- [Unity6 AsyncGPUReadback.RequestIntoNativeArray](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.AsyncGPUReadback.RequestIntoNativeArray.html)
- [Unity6 AsyncGPUReadbackRequest lifetime](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.AsyncGPUReadbackRequest.html)

These are API-supported design choices; their exact Metal/GameView color, orientation, throughput, and cleanup behavior is not yet verified by this proposal.

## Implementation log

2026-09-11: Parent accepted the optional two-cast appendix and reserved only small `CommandCase`/launcher hooks plus the new helper/tests to this subtask. Other runtime/presentation/camera code remains outside scope. All existing calls and original94/native profile/direction criteria remain unchanged. The six non-Ember normalized source exports from E00 are separate preservation gates owned by the art revision.

Pre-implementation correction: the first budget of64raw frames+4RTs was unnecessarily large. Parent capped the recorder at32raw frames+3RTs; sampling now concentrates on flight and settling rather than1.2seconds of idle footage. This changes the proposed audit's storage policy only, never gameplay duration or animation speed.

35 new reflection-based tests are staged in `Assets/Tests/EditMode/Presentation/Rendering/StarterSpell3DEmberMotionCaptureTests.cs`: exact opt-in; six isolation countercases; complete east+NE metadata;26 single-mutation evidence rejections; and an honest real-gap versus missing-flight pair. The helper is absent, so the intended first failure is a missing-feature assertion, not an unrelated compile error. GUID `b883f78867e7457f95f5c10b4adcb534` is unique. Production implementation waits for recorded RED and central lifecycle release.

These metadata tests intentionally do not claim asynchronous request lifetime, GPU pixel correctness, or native motion proof. Actual GPU/readback ownership tests and native calibration are required once the implementation exists; all new failures must be retained.

E01 completed48 tests:2passed,46assertion failures,0compiler errors/skips. The imported art slice was11RED/2GREEN; all35 recorder cases failed at the intentionally missing helper.997 frozen inputs remained byte-identical, including the imported Library and all seven Assets FBX files compared with E00. `E01-integration-recorder-red/receipt.json`, XML and source-preservation receipt retain the exact failures. The new Reduced halo art test had a fixture precondition mistake and was corrected by its owner for a separate old-import RED rerun; no original tests changed.

Implementation now uses32 preallocated persistent `NativeArray<byte>` buffers with `RequestIntoNativeArray`, avoiding an extra copied full image per callback. Three RTs stay reserved until callbacks finish; slots are not reused within a cast. PNG encoding occurs after the1.2second command observer and actual cast/gesture clear, with explicit request/completion/encoding timestamps. The two casts reuse buffers sequentially after cloning each completed receipt. Final disposal records actual remaining created buffers/RTs and pending count. Partial disposal writes an incomplete sidecar instead of claiming success. Calibration temporary reference/encoding images are separate from the persistent32+3 capture capacity and occur outside spell/profile timing.

Optional hooks are limited to the audit scenario and audit launcher. The command-line flag is exact `-emberMotionCapture`; default calls retain their null helper. Appendix rows are `ember-motion-east` and `ember-motion-northeast`, after all existing checks and captured-file hashes. The original direction validator still requires its exact15 rows. The launcher retains the original final native gate and adds a separate requested-sidecar metadata/file-hash/ownership gate. Game presentation, spell resolution, shaders, camera, imported art and existing test criteria are unchanged.

An isolated compiler shape check using installed Unity assemblies passed after adding the correct local netstandard reference to the temporary compile command; it did not launch Unity or replace the central test run. Independent read-only review found no blocker in request ownership, phase sampling, default behavior, or metadata rejection controls. Its defensive note was incorporated: Begin/capture refuse an already requested disposal even while callbacks are still draining. Actual Metal pixel fidelity, achieved flight sampling, and runtime cleanup remain unverified until the native appendix runs.

E02 completed48 tests:37passed,11failed,0compiler errors/skips. All35 recorder cases are GREEN. The corrected old-import Reduced impact test now reaches the intended real assertion: width3.67108154cells against≤1.15. The other old-art failures and two passing controls remain unchanged.999 frozen inputs remained identical; Unity exited before the lifecycle lock was released. Receipt/XML: `E02-helper-green-old-art-red`; XML SHA256 `506324d71e079ab723cc050222a9b3b63d66d95ba423b1bfa567ff46dbfb8b90`.

The EmberRevision runners copy the prior safe lifecycle scripts. `run_native.py NAME game --ember-motion` adds only the explicit appendix flag and verifies its separately returned sidecar/cleanup result; other native modes reject that flag. Its default path retains the ordinary native audit. No native launch or candidate import occurred while preparing these runners.
