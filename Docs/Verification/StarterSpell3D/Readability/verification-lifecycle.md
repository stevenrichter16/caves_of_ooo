# Readability verification lifecycle

The verification coordinator owns all Unity launches, imports and tests for this wave. Other agents may author outside Assets between explicit freeze/release messages; no concurrent Unity operation is permitted.

## Pre-change inputs

`R00-prechange/source-hashes-before.json` covers 8,336 files (540,317,394 bytes) under Assets, ProjectSettings, Packages and ArtSource/StarterSpell3D, plus the cited rules/plans/runners. Hashing found no concurrent input mutations. Exact copies of 51 canonical spell/settings inputs are retained under `preserved-inputs`; remaining files are tracked by SHA-256. HEAD is `1d2b5be8b7b9a67d8bd333b1ff6340f24d3505b3`, branch `claude/game-lore-analysis-jqa7ur`.

Unity was inspected read-only: one clean SampleScene, idle, not playing, no compilation/import/test running, no prefab stage or unsaved Editor window. An attempted in-memory exit was blocked by the MCP tool's pattern guard; no code exit ran. Normal application quit then completed through the native UI. No process was force-killed, no scene was saved, and no camera or user save mutation was requested.

`R01-baseline/launch-refused.json` is **NOT_RUN**: the existing runner correctly refused while the ordinary Editor exit was completing. No tests or MCP restart occurred in that attempt.

## Baseline run

`run_editmode.py` is an unchanged copy of the established Integration runner. It refuses an existing Editor, restarts MCP, waits two seconds for shutdown and five seconds for startup, then invokes Unity 6000.3.4f1 in EditMode batch testing. Compiler errors are counted before XML is trusted; named output directories are never reused.

```sh
python3 Docs/Verification/StarterSpell3D/Readability/run_editmode.py R01b-baseline
python3 Docs/Verification/StarterSpell3D/Readability/compare_baseline.py Docs/Verification/StarterSpell3D/Readability/R01b-baseline
```

R01b completed: **11,177/11,208 passed, 31 failures, zero compiler errors/skips**. Every failing fullname and unnormalized message exactly matches S3D29; no tests were added/removed. The actual test run took 206.905 seconds. Unity exited normally (test-failure exit code 2); no Unity process remains. The named comparison and XML SHA-256 are preserved.

Post-run hashing found only two generated `Assets/UnityMCP/Log` logs and Unity's automatic `ProjectSettings/QualitySettings.asset` schema rewrite changed. No camera, gameplay, scene, shader, asset library or package source changed. These three changes are explicitly recorded in `R01b-baseline/source-preservation.json`; the coordinator did not silently revert them. The exact pre-change QualitySettings bytes remain preserved for the parent to restore after review.

The existing Integration `run_native.py` was reviewed; it uses native Editor rendering, fresh run-bound reports and isolated gameplay saves. Its output-directory and NativeAudit discovery paths depend on its location, so it has not been blindly copied here. Later native receipts will use that verified entry point or an explicitly documented path-only adaptation.

## Readability RED and GPU probe construction

`R02-readability-red` is NOT_RUN: the runner refused a new ordinary Editor. A fresh read-only guard found clean idle SampleScene, no Play/compile/import, no dirty scene, prefab stage, or unsaved Editor windows. Normal native Quit completed; no process was killed or scene saved.

`R02b-readability-red` completed **71 tests: 6 passed, 65 assertion failures, zero compiler errors/skips**, 1.193 seconds of tests. Primary tests passed 2/15; adversarial 4/33; GPU receipt/fresh-emission-control tests 0/23. Missing shader/metadata/imported art and actual contact/priority behavior failures are preserved in the XML and receipt. All Unity processes exited before the Assets lock was released.

The GPU verifier was implemented only after this observed RED. It renders owned physical quad/fan fixtures in a preview scene using the actual two spell shaders. Nineteen paired cases cover visible local darkness, emission zero, split physical fog, memory/unseen, all four out-of-world sides, missing/wrong-size fog bindings, opaque occlusion, interpolated vertex alpha versus flat alpha, zero base alpha, and an explicit draw-order depth-write counter. It restores active target/scene/dirty flags/async compilation and disposes owned resources. It does not claim actual spell composition, gameplay, or performance acceptance.

Existing imported-art GPU color references now preserve `_Emission` in fresh raw-vector blocks. Fresh blocks retain the previous independent linear-color test; they neither reuse Unity's Color-type conversion metadata nor change emission as an accidental second counter variable. The original 21 imported-art cases remain. New metadata GUIDs were checked against all Assets metadata: one owner per GUID.

Actual GPU rendering has not yet run for Readability; receipt-validator tests alone do not establish shader correctness. Root owns production fixes and chooses the coordinated native GPU gate after the combined implementation tests.

`Readability/run_native.py` is now a path/entry-only adaptation of the prior native runner. `gpu` preserves the original 21 imported-art cases; `readability-gpu` invokes the new isolated shader probe via `-spellReadabilityGpuReport`; `game` retains the existing actual-player launcher and explicitly discovers its fixed Integration/NativeAudit outputs. All modes retain fresh output directories, existing-Editor refusal, settled MCP restart, native GPU Editor rendering, and compiler-error-first receipts. No native probe was launched merely by writing/importing this runner.

## Combined implementation and lifecycle RED

`R03-import-guard-red` completed **85 tests: 67 passed, 18 failed, zero compiler errors/skips**, 1.208 seconds. All 33 existing readability adversarial cases and all 23 GPU metadata/emission-reference cases passed. Primary cases passed 10/15; the remaining five expect the not-yet-imported concept art. All seven new importer-preflight cases reproduced the missing validation entry point. Six owned-material lifecycle cases reproduced real idle recovery and active cancellation failures; their intact-readiness counter passed. Assets were released only after no Unity process remained. These RED receipts authorize the bounded importer/recovery implementation; GPU metadata GREEN alone still does not claim an actual render pass.

## Actual material GPU acceptance

`R04-readability-gpu` ran the native Editor on Metal with **19/19 physical shader cases passing**, no compiler errors or unexpected logged errors. Run `a2870f7316fe4c73a3821a6b0e0140d3`, combined shader source SHA-256 `3f93199d43e71193da1ff48cf4b8592f100946991e1da247ac000c37bb7955ec`. The 45 emitted 256×256 PNGs were independently decoded and SHA-256 checked; source hashes remained unchanged. `artifact-audit.json` records **69/69** paired/source/artifact checks.

Both split-fog cases drew 12,800 pixels in visible cells and zero in hidden cells. Every negative frame was black. Glow gradient mean levels were 0.44690 / 0.22510 / 0.03137 at center / middle / edge; flattening vertex alpha changed 25,596 pixels. The forced-order invisible-glow frame exactly preserved the rear plane, while the opaque depth-writing counter changed 25,600 pixels. Active scene/dirty flags, active render target and async compilation were preserved; owned resources were disposed. No Unity process remained before lock release.

This is actual material-level rendering acceptance, not approval of the newly composed spell art. The original 21 imported-art GPU cases and actual-game/1080p profile gate remain pending fresh art import.

## Frozen art import and combined regression

`R05-candidate-import` used the new reproducible `run_import.py` runner after all13 paths in `Art/23-import-candidate-freeze.json` matched their frozen SHA-256 values. Import **PASS**, zero compiler errors:285 meshes/pieces and24,628 triangles. Blend `c3290625f855e53a28a58bb8a1bc031efadc625d9b2892814d2c21ec6025c0af`, runtime JSON `c00ef612d778444239626518c21bf8a8ff73b1bdaf0231515fd3c356523ec4db`. Frozen source files remained unchanged. All seven actual gesture clips imported36 quaternion curves; maximum movement50.78–71.28 degrees, source/native rest-frame difference zero.

`R06-candidate-targeted` retained the prior319-case filter and added all87 readability cases: **403/406 passed, three failures, zero compiler errors/skips**,5.741 seconds. All five imported-art concept checks, importer-preflight tests, and owned-material recovery tests passed. The real empty-Rain regression failed as expected. Two prior canonical-anchor positive controls (ConeCell and ReactionCell) also failed because the new zero-fragment refusal rejects a valid entry whose captured outcome has no matching visual fragment. No test criterion or outcome was changed. All three failures were referred to the parent for the bounded valid-empty-cast fix before the original21 actual-art GPU/native-game gates.

## Restored valid-empty-cast behavior and imported-art GPU

`R07-gesture-empty-green` passed **406/406**, zero compiler errors/skips. The newly introduced zero-fragment refusal was removed, preserving the prior valid-entry/gesture contract. Four new experimental expectations were corrected to distinguish zero environmental meshes from a rejected native cast; pre-existing tests were unchanged. Those expectation corrections are a recorded false premise, not newly discovered game bugs.

`R08-imported-art-gpu` passed the original **21/21** actual mesh-facing/assembled-fog/linear-color cases with zero compiler errors. Run `62dfaafc6f7d4afbb1d94848dbed5d0e`, source runtime JSON exactly `c00ef612d778444239626518c21bf8a8ff73b1bdaf0231515fd3c356523ec4db`. Every spell's native-versus-fresh-linear color difference was exactly zero, with unchanged emission across all controls. All70 PNGs decoded at512×512 and matched their recorded hashes. The independently retained789 imported/runtime/probe source inputs were unchanged: **894/894 artifact and source checks passed**.

## First real-game candidate

`R09-native-candidate` passed78 native checks, zero compiler/unexpected errors, and all isolation/cleanup checks. Run `f83f27a1475f4e11866f7dde09c33331` captured108 PNGs at1920×1080,65 deterministic command casts plus one actual keyboard cast, and81.6863 seconds across the four Full/Reduced town/south phases. Peak showcase mesh counts were48/33/70/45/43/28/15 for the seven spells. The same-run private save root was removed, scenes/view/preferences restored, and no Unity process remained before lock release.

Independent `performance_audit.py` verification passed **419/419** checks:26,158 raw CSV frames,8,068 active frames,52 profile commands, all108 PNG hashes/decode checks, exact phase/count coverage and cleanup. Its stage note is explicit: this is the **first Readability candidate**, before the direction-proof extension and final visual acceptance. Historical receipts are not overwritten.

Main Thread average/p95/p99/max milliseconds were3.509/4.029/4.793/686.872 (Town Full),3.814/4.589/6.229/518.879 (Town Reduced),2.724/3.263/3.826/19.766 (South Full), and2.714/3.067/3.457/575.643 (South Reduced). Spikes are retained. Whole Editor/game/fixture work is included; sequential phases do not establish a Reduced-mode speedup or spell-specific causal cost. The detailed JSON/Markdown contains Input/Zone/GC/render counts, preparation, actual gesture sampling, and honesty bounds.

## Direction-audit tests

`R10-direction-red` safely refused an existing ordinary Editor and import workers: NOT_RUN. Read-only checks verified clean idle SampleScene, no Play/compile/import, no dirty scene or unsaved windows. Ordinary native Quit completed; no force termination or save. `R10b-direction-red` then captured15/15 genuine missing-implementation assertion failures, zero compiler errors. The scenario/launcher implementation was installed only after that RED.

`R11-direction-green` passed421/421 focused tests, zero compiler errors/skips. `R12-missed-capture-red` then passed15 controls and failed exactly the new contradictory-evidence counter: a direction case whose frame capture missed the effect still passed its receipt guard. This RED authorizes the narrow evidence-check fix; it does not represent a newly run failed native gameplay audit.

## Final-media and preservation tools

`compose_final_native_media.py` requires an explicit final acceptance pin tied to exact native report bytes and run ID. No candidate report has been composed as final media. The five approved spells are primary; Hands/Rain are an optional appendix. Complete1920×1080 native frames encode to lossless RGB H.264 MP4 with decoded pixel-hash equality, explicit square-pixel metadata, measured1× sample timing, and a1.1ms PTS tolerance. Full PNGs copy unchanged. A separate704×528 crop repeats each pixel2×; all labels sit outside that crop. Optional title pauses are separate records. Capture sampling remains about10 frames/sec; no missing motion is invented.

The tool smoke used synthetic calibration images only:16/16 checks passed. A real initial metadata failure exposed unspecified video pixel aspect; `setsar=1` fixed metadata without changing decoded RGB. Maximum synthetic PTS error was0.000372 seconds. FFmpeg decode is verified; playback in every hardware player is not claimed. Synthetic source frames/MP4/ffprobe evidence are retained in `ReviewToolSmoke-synthetic`.

`check_r00_preservation.py` hashes the exact R00 roots/references, detects unstable reads, and requires a reviewer-owned exact path/before/after hash pin for every change. No folder wildcard or generated-log exception grants approval automatically. Gameplay/content/camera/settings/packages and modifications to existing authoring revisions remain protected. It emits a blank review template and flags all unknown changes; it never restores files. Its PASS scope is explicitly bounded by R00's recorded files/roots, with supplementary git-status changes reported separately. Final frozen-source scan and final media encoding are pending final acceptance.


## Refined candidate import and final directional execution

`R13-candidate2-import` passed with zero compiler errors after the exact `Art/46-candidate2-import-freeze.json` inputs matched. The source blend is `2e8dda06d7ce02ebbdad1f77d24d0200e450aa928f986c26d2335eddc1254e4d`; runtime JSON is `bbf06a52ed9ee416066ea5422cbcc87fa35d8fe9ee89426812aeaacc64cbdc0f`. The import contains 380 mesh pieces and 47,352 triangles. Unity exited before the parent archived and pruned 33 unused candidate1-only meshes (66 files). That separate receipt proves they were absent from R00 and the final JSON, archived byte-exact, and externally unreferenced.

`R14-final-candidate-targeted` passed **422/422**, zero compiler errors/skips, including the missed-direction-capture regression. `R15-final-art-gpu` passed the original **21/21** physical imported-art cases against the refined JSON. All 70 native PNGs and 983 frozen imported/runtime/probe inputs passed independent checks: **1,088/1,088**. Every native-versus-fresh-linear comparison matched; emission stayed unchanged between controls.

`R16-final-native-directions` passed with zero compiler/native/unexpected errors and complete restoration of scenes, GameView, preferences and isolated save state. Run `487d034a09814ec09d5e705b85cb852c` produced 288 unmodified 1920×1080 PNGs, 80 command casts plus one actual keyboard cast, and all 15 additional directional cases. Independent verification passed **974/974**: 25,788 raw profile frames, 7,671 active frames and 82.2318 seconds across four Full/Reduced town/south phases. Every one of the 983 frozen R14 inputs remained byte-identical after the run.

Main Thread average/p95/p99/max milliseconds were 3.639/4.232/4.877/692.675 (Town Full), 3.947/4.802/7.314/495.895 (Town Reduced), 2.883/3.279/3.678/584.521 (South Full), and 2.629/3.143/3.596/22.508 (South Reduced). Every outlier is preserved; this whole-Editor sequential workload does not prove spell-specific costs or a Reduced-mode speedup. The source-specific performance receipt remains labelled candidate2 until the parent's final visual acceptance. No Unity process remained before the lock was released. Full-suite close-out and final accepted media are pending.


## Candidate motion review and optional playback compatibility

At the parent's request, R16's five main showcases and separate Jet/Surge directional cases were composed as explicitly labelled candidate review videos. `compose_candidate_native_review.py` does not invoke or weaken the final acceptance gate. The two full-frame RGB montages retain 60/72 actual samples, 5.97875/7.16277 seconds of recorded holds, and separately labelled title pauses. Every decoded pixel matched its source, with maximum PTS errors of 0.495/0.481 milliseconds. No candidate was represented as final art acceptance.

The final composer now supports optional `--compatibility-mp4`: a clearly named standard H264 High/yuv420p companion to the authoritative lossless RGB master. It preserves full-frame dimensions, sample count/order and variable recorded walltime holds, with no motion interpolation. RGB-to-YUV conversion, chroma subsampling and lossy encoding are explicitly disclosed; decoded RGB mismatches are measured rather than misrepresented as lossless. Codec-level FFmpeg verification does not assert that every app can play it.

`Compatibility-codec-red` preserves the missing-encoder RED. Synthetic-only GREEN checks passed 8/8, including a 568ms recorded hold, actual decoded sample identity/order and explicit compression evidence; maximum PTS error was 0.456ms. Three additional final-pin controls confirm candidate/wrong-run refusals and matching-final acceptance remain intact. No final game media was generated by this preparation.


## Continuity refinement import and rendering gates

The parent requested a final Blender-only continuity refinement after reviewing R16 motion: shared Jet follow-through and four backward-only Surge stitches. No runtime, material, camera or gameplay change was included. The exact source freeze is `Art/59-continuity-import-freeze.json`, blend `61957bbd37cb8100b6e672c5d7d2169077bb97748c1fdd6e16d9f5d1a2216b19`, runtime JSON `971a07ef686af7207ce7d1903c8446061c5ca0200d29e6ebb8731c5551a3bf95`.

A user-reopened ordinary Unity Editor was inspected read-only, then freshly guarded again immediately before the authorized import. It was clean and idle, with no Play transition, dirty scene, prefab stage or unsaved window. Normal Command-Q closed it and its import workers; no force kill or scene save. `R17-editor-exit.json` retains the guard.

`R17-continuity-import` passed with zero compiler errors: 384 pieces and 47,672 triangles. Frozen source hashes remained unchanged. `R18a-continuity-targeted` passed **422/422**, zero compiler errors/skips. `R18b-continuity-art-gpu` passed the original **21/21** cases against that exact imported JSON. Independent verification passed **1,097/1,097** checks: 70 PNG hashes/decode, actual independent color-reference equality versus different double-linear controls, unchanged emission, and 991 frozen imported/runtime/probe files. `audit_imported_gpu_artifacts.py` records this repeatable check.

The two readability shaders and borrowed physical-fog include remain exactly identical to R04's combined SHA `3f93199d43e71193da1ff48cf4b8592f100946991e1da247ac000c37bb7955ec`; the material-level 19-case GPU proof is retained rather than unnecessarily rerun. R19's actual-game execution, visual acceptance and final full suite remain separate gates.


## Retained native failures and contact recovery

`R19-continuity-native-directions` failed a south Full Jet profile cast after all seven captured showcases passed. The copied command had correct path, cone, damage, Wet and displacement, but observed zero meshes. Raw frame16855 recorded560.8244ms unscaled delta and562.608ms between sample walltimes; the following profiler sample reported563.773ms MainThread. The gap spanned its visible phase. The partial13,106-frame profile and all images remain retained; no direction cases ran. No source change or timing/gate relaxation was made.

One explicitly authorized unchanged retry, `R20-continuity-native-retry`, completed all four20second phases and52profile commands, then passed13direction cases. Northeast Rime applied its real Frozen status/damage but observed no effect meshes, with a583.658125ms screenshot observation gap and only13gesture samples. Its profile CSV ended before that direction case, so a precise offending frame cost is not available. The failed conditional check precedes assignment of nativeEntry/resultStable/seconds; their defaults do not prove fallback or gameplay mutation. Northeast Calm never ran. All991 inputs remained unchanged. R19/R20 remain failed workloads, not accepted final media or merely missed screenshots. No further automatic retry was made, and the stall cause remains unattributed.

The parent then authorized a narrow native visual recovery rather than another retry: when a qualifying long raw walltime step would wholly skip an unseen visible contact phase, preserve contact once for the native meshes and their playback handles. Ordinary timing, raw5second watchdog, sprite/legacy clocks, gameplay, art/shaders and camera remain separate unchanged contracts. This does not remove the underlying Editor stall.

`R21-hitch-recovery-red` captured19cases:12passed/7assertion failures/0compiler errors. One new fallback control had an incorrectly distant affected cell, extending its sprite lifetime; this false premise was corrected only in that new fixture. `R21b-corrected-fallback-red` safely refused a user-reopened Editor before testing; a fresh clean-idle guard and ordinary native Quit are retained. `R21c-corrected-fallback-red` then failed exactly the intended native Playing assertion after strict legacy/sprite completion controls passed, with unchanged production and0compiler errors.

`R22-hitch-recovery-targeted` stopped at compilation: three repeated CS0266 lines exposed assigning fractional StudyContactFrame to an integer. No tests executed. The parent corrected contact-pose interpolation and the auditor added eight boundary controls. `R22b-hitch-boundary-targeted` passed **449/449**, zero compiler errors/skips: the prior422 plus27 hitch/recovery cases. All993 frozen inputs remained unchanged; Unity exited before lock release. Actual native acceptance and the full suite remain pending the parent's separate review/run decisions. Historical RED and failed-native receipts are preserved without overwriting.


## Final accepted native evidence, full suite and media

`R23-contact-recovery-native` passed the unchanged actual-game gate: all seven spells, fifteen directional cases, original keyboard cast, four paired profile phases, and complete scene/view/preferences/save-root cleanup. The parent and independent artist accepted the recorded static visual review; exact report/frame hashes and limitations are pinned in `R23-final-visual-acceptance.json`.

Independent verification passed **968/968** checks: 286 native1080p PNGs,80command casts plus one actual keyboard cast,25,674raw profile frames,7,828active frames and81.6928seconds. All993 frozen source inputs remained unchanged. Main Thread average/p95/p99/max milliseconds were3.564/4.476/6.794/23.043 (Town Full),3.948/4.629/5.614/761.029 (Town Reduced),2.760/3.302/3.790/19.774 (South Full), and2.756/3.308/4.090/21.560 (South Reduced). Every outlier is retained. The underlying stalls were not eliminated or causally identified; this run does not establish a Reduced-mode speedup or natural recovery activation.

`motion-gap-review.json` includes every interval over0.2seconds, including cast-start to first capture and final tails: south Jet had693.737ms between samples after89meshes were already captured, and south Surge had492.937ms before its first capture. Exact timestamps and source frames remain intact. These are observation gaps, not proof of an exact per-frame cost or an observed recovery diagnostic.

`R24-final-full` completed **11,307/11,338 passed,31failures,zero compiler errors/skips**,218.443seconds. The exact R01b comparison passed: all31fullname/raw-message pairs match, all130added tests pass, and no tests were removed. The known baseline failures remain failures, not ignored or reclassified. Unity exited before the lifecycle lock was released.

Final read-only GUID audit passed **4,299/4,299** unique valid metadata GUIDs. The same993inputs remained unchanged after the suite. The first R24 preservation scan retained the freshly recognized QualitySettings automatic schema rewrite; while Unity was closed, only that exact known7ec…de3 byte sequence was restored from the R00e7dc…883 copy under the parent's standing instruction. `R24b-preservation-review` then found615review-required changes and zero protected/unstable changes. All255added mesh assets and their255metadata files are grouped by actual final-export owners and serialized Library GUID references; the imported384mesh names exactly equal the final export with no missing/extras. Approval is still exclusively the root reviewer's exact path/hash decision; grouping grants none.

After full-suite completion, the explicit final visual pin authorized `Final-native-media-e29cae1c81ba4cac8fa92f275c1b719b`. It contains the five main accepted town showcases plus Hands/Rain appendix:84original captures,seven byte-identical full1080p PNGs,8.364032seconds of native sample timing, and8.365second videos. Lossless full-frame and separate nearest-neighbor inspection MP4s decode to their source RGB pixels exactly. An optional standard H264/yuv420p companion is explicitly labelled chroma-compressed. All retain85decoded frames including the final hold sentinel and maximum0.519ms PTS error; no motion interpolation, retouching or camera change. Only selected town showcase samples are composed; uncomposed directional gaps remain documented in the native receipt. Original reports, captures and final acceptance pin are unchanged. The README and media-verification.json retain complete source/timing/codec evidence.
