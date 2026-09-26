# Density native capture — memoryless depth investigation

Status: capture-path repair verified in the native Metal editor. The game
renderer and render pipeline are unchanged. This follows Density Phase 1
T2.7–T2.8 after the full native suite and isolated lair audit passed.

## Problem and bounded plan

The editor log contains `Ignoring depth surface load action as it is memoryless`
and its matching store-action message during native screenshot capture. The
audit's main-thread application-log callback reported zero errors, so callback
counts alone are not sufficient evidence. Preserve the original messages and
compare whole-run editor-log ranges as well as numerical reports.

1. Replay the existing capture-enabled finite native audit unchanged.
2. Add a developer-only capture-disabled control with the same seed, route,
   waits, scene/save isolation and cleanup. Its report must explicitly say no
   capture was requested and must not claim a screenshot exists.
3. Run matched capture-on/off probes; retain run IDs, raw log ranges, checksums,
   callback counts, messages, and pre/post restoration evidence.
4. Only if evidence isolates a cause, write the smallest repair and repeat the
   failing condition plus its matched control. Do not silence logs, disable
   rendering, or change shared pipeline settings speculatively.

## Pre-implementation verification sweep

| Premise | Observed source | Consequence |
|---|---|---|
| Audit zero errors means the console was clean | The callback is `Application.logMessageReceived`; original two messages have no managed stack and were absent from that counter | Inspect raw editor output independently; do not recategorize messages as harmless |
| Density pool art owns the failing depth attachment | New density resources contain only mesh/prefab references; they create no targets or render passes | Do not modify pool art to address a backend attachment problem |
| Shared native camera explicitly requests memoryless depth | `NativeZone3DRenderSurface.Sync` creates ARGB32 with 24-bit depth and MSAA 1; no project script sets memoryless/load/store flags | No direct project-side memoryless misuse has been identified |
| Capture timing proves causation | Original pair lies after arrival and before the report; `ScreenCapture.CaptureScreenshot` is followed by a half-second wait | Screenshot/URP backend interaction is only a hypothesis until matched runs |
| Turning off capture is already a supported control | The current driver always calls ScreenCapture and checks the file | Add an explicit opt-in developer control; keep ordinary audit capture enabled |

## Scope and performance

This is a CoO-specific Unity investigation, not Qud parity or gameplay work.
All scene/save/preferences restoration remains owned by the existing isolated
launcher. Player travel, lair generation and rendering remain identical across
controls. Any extra instrumentation runs only during the finite developer
audit; no per-frame gameplay allocations, new shader, atlas, or art pack.

## Evidence and review

The unchanged capture-enabled replay `cb0b9a2cc20c4ccb9e579090208d63ca`
reproduced exactly the two messages. Its 18 numeric/8 native assertions passed,
callback errors remained zero, and every pre/post scene, save, preference and
input-setting field matched. Raw log bytes and their hash are preserved in
`CaptureProbe/capture-on-original.json` and the adjacent compressed log.

The experimental harness now offers an explicit no-capture control. It changes
only whether the screenshot call and its file assertion occur; the timing and
travel are identical. Reports expose `captureRequested`, and the negative
control records `capture_disabled_control` instead of `screenshot_written`.

Matched probes reproduced the cause boundary:

| Run | Capture | Native depth messages | Audit | Settings restored |
|---|---|---:|---|---|
| `cb0b9a2cc20c4ccb9e579090208d63ca` | Original enabled | 2 | 18 + 8 pass | Yes |
| `0acc731def3d4599a6564e6e2c5e14db` | Explicit disabled control | 0 | 18 + 8 pass, no screenshot claimed | Yes |
| `7513f6825b5f4eaa821a5b34b7243b3f` | Enabled after control instrumentation | 2 | 18 + 8 pass | Yes |

The enabled/disabled/enabled sequence isolates the `ScreenCapture.CaptureScreenshot`
invocation in this environment. It does not identify Unity's internal failing
attachment or establish that ordinary gameplay emits the error. A preflight
MCP connection error during the compile was outside the recorded Play log
ranges; it is not counted as a gameplay or screenshot failure.

Next bounded experiment: replace the file-oriented screenshot path with capture
into an explicitly owned, color-only RenderTexture, then read its pixels and
write the PNG. This keeps the same fully rendered frame and resolution, adds no
depth attachment, restores the previously active target, and releases both owned
textures even if writing fails. Confirm the positive run is quiet and rerun the
disabled control; do not infer success merely from the API name.

Seven focused native EditMode tests first failed on the missing helper
(`capture-fixture-red.json`). The new helper uses the same existing project
pattern as the Ember motion capture: ARGB32/sRGB, zero depth, MSAA 1. It captures
into that owned target, reads full-resolution RGBA pixels and encodes a PNG.
`finally` restores `RenderTexture.active` and releases the target and readback
texture. Tests exercise actual green/blue GPU pixels, repeated use, capture
failure, file failure and rejected dimensions. This is a one-shot synchronous
developer screenshot, not a per-frame or gameplay capture path.

**Intermediate result, not accepted as complete:** seven helper tests passed.
The replacement capture-on run `f7f82dc841de4eeca0eba4d0a60907d9` and disabled
control `9c5bf53bd28748099493b15869208a83` emitted zero depth messages, passed
18 + 8 audit assertions, restored every observed setting, and left zero owned
capture textures. However, actual image inspection found the captured PNG
upside-down. That failed visual artifact and receipt are retained. Metal reports
`graphicsUVStartsAtTop = true`; the existing Ember motion audit already accounts
for GPU row order. Two new asymmetric-pixel tests now require explicit top-origin
row reversal and a bottom-origin no-reversal counter-path before accepting the
capture replacement.

The first orientation-test dispatch did not execute: Unity stayed in an actual
`EditorApplication.isCompiling = true` state after a refresh, while
`TestRunnerApi.IsRunActive()` was false and the compile console showed no errors.
The refresh timed out and test dispatch returned busy or waited without a job
ID. These transport/readiness attempts count as zero executed tests, not RED or
GREEN; editor recovery precedes another dispatch.

After a clean editor restart, orientation RED executed all nine cases: seven
existing controls passed and both row-origin cases failed on the missing
correction (`capture-orientation-red.json`). The correction now reverses rows
only when `SystemInfo.graphicsUVStartsAtTop` is true and preserves every RGBA
byte. A temporary same-frame reference capture will compare the replacement PNG
against Unity's texture screenshot before the instrumentation is removed and
the ordinary enabled/disabled probes repeat.

After restart, the actual running Unity process held `Editor-prev.log` open,
although `Application.consoleLogPath` still named `Editor.log`. Subsequent probes
resolve the process's live log file descriptor before recording byte ranges;
the rotated, inactive filename cannot prove that a run was quiet.

Tests can establish native execution, captured log
messages, screenshot-file existence and restoration. A rendered still cannot
establish absence of every GPU defect, animation quality, balance or sustained
performance. Keep unresolved causes explicit rather than claiming a fix from
timing or a single quiet run.

## Accepted result and counter-checks

Native focused job `0625b069c36e415285971400651ccecc` passed 83/83 cases:
capture 9, item inventory 13, popup layout 4, material guidance 18, and existing
action-feedback adversarial coverage 39. The capture cases verify both origin
branches, unchanged RGBA bytes, full dimensions, real GPU colors, no depth or
memoryless allocation, repeat captures, borrowed-target restoration, and cleanup
when capture or file output throws. Receipt: `capture-item-layout-green.json`.
The later additional legacy-tonic test is outside this 83-case receipt and is
left for the final whole-project suite.

Same-frame probe `89e5db7eae634c83baca3303f65c6105` called Unity's texture
screenshot API and then the replacement without yielding between them. Every
RGBA byte of both 1920×1080 PNGs matched: zero different bytes, shared decoded
pixel SHA-256 `b6c2c88275acb36c65d5e7109c682f140f6e13bb89c72eda152751ddccc53672`.
A vertically inverted counter-comparison did not match. Visual inspection also
confirmed upright HUD text, the lair, player, boss, and the full game frame.
The reference API reproduced the two depth messages during this experimental
run; it is fidelity evidence, **not** a clean-console run. That temporary
reference capture was removed before the final probes; both original PNGs and
raw log are preserved.

| Final probe | Run | Audit | Native depth messages | Settings |
|---|---|---|---:|---|
| Default capture enabled | `1d2b8c0184f8454482ef258c83e4eef5` | 18 + 8 pass | 0 | Restored |
| Capture disabled | `748995604b6a4bcd981106560ec27bbe` | 18 + 8 pass | 0 | Restored |

Both final whole-run log ranges were nonempty (74,186 and 74,135 bytes), read
from the running editor's verified FD 9, and contained no error, exception,
assertion, failed, or memoryless lines after excluding only a known method-name
substring. They each completed in about seven seconds. The final enabled PNG
was inspected as upright and full resolution; zero owned capture textures
remained. The disabled control produced no screenshot and claims none.

One intervening disabled attempt was interrupted at step 14 by an actual script
assembly reload. It is retained as `capture-off-interrupted.json`, not counted
as a successful control. The existing three-minute watchdog exited with code 1;
scene, save root, preference, seed, background state and original InputSettings
instance `-910` (including its full JSON) were restored. A forced asset refresh
then completed idle before the accepted repeat. The interrupted raw log also
contains unrelated Unity Connect authorization messages. The final control's
diagnostic last-exit status moved from 1 to 0; the receipt distinguishes this
status from user/editor settings, all of which matched.

Self-review: the repair owns only a one-shot color target and CPU readback,
restores the caller's active target in `finally`, does not clone or modify any
gameplay camera/pipeline asset, and does not suppress messages. Orientation uses
the backend origin flag rather than a hard-coded operating system; the
bottom-origin branch is tested with asymmetric pixels, but other GPU backends
have not been run. This establishes the bounded screenshot workaround on this
editor, not a fix to Unity internals or proof about sustained gameplay graphics.

## Files

- This investigation log.
- Developer capture control: `DensityPhase1BenchPlayer.cs` and
  `DensityPhase1BenchBatch.cs`.
- New capture helper: `Assets/Scripts/Scenarios/Custom/DensityNativeScreenshot.cs`
  and metadata.
- Native tests: `Assets/Tests/EditMode/Presentation/Rendering/DensityNativeScreenshotTests.cs`
  and metadata.
- Evidence: `Docs/Verification/DensityPhase1/CaptureProbe/`.
- Exact owned file list and checksums: `CaptureProbe/manifest.json`.
