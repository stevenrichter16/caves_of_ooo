# R1 release stabilization — acceptance continuation

Status: accepted, 17 September 2026 local / 18 September UTC. Continues [RELEASE-STABILIZATION](RELEASE-STABILIZATION.md) after the detailed handoff. The user authorized sustained autonomous release work. The live Unity Editor remains open; tests use `/tmp/coo-regional-verification-20260917`.

## Scope and corrections before continuation

| Question | Verified result |
|---|---|
| Is implementation still missing? | The 99-file candidate already fixes the three inherited failure groups. R103 passed 14,901/14,901, zero compiler errors. Do not redo it. |
| Has the native opening run with its new assertions? | Not at handoff. R104 below executes both checks and the full existing journey. |
| Can current checkout act as the old renderer baseline? | No. A concurrent scheduled run switched it to `release-candidate`, applied the exact candidate in `7622d71f`, and committed docs in `17b12dc7`. Preserve those commits. The baseline is immutable `26fbe544:Assets/Scripts/Presentation/Rendering/EnvironmentSpriteRenderer.cs`. |
| Is the old equipment combat bench sufficient? | No. Ground-item idle/walk/pickup-drop rendering needs its own identical before/after workload. The new synthetic bench must identify that scope explicitly. |
| Does passing the suite complete R1? | No. Native art/performance, imported-model review, cold-eye review and verified integration remain required. |

No new Qud parity claim: this is CoO-original art and fallback behavior. The reviewed Qud Render reference classification remains in [EQUIPMENT-GROUND-SPRITES-PLAN](EQUIPMENT-GROUND-SPRITES-PLAN.md).

## Completed continuation evidence

- **R104 native journey:** 58/58 checks; 876 queued native input steps; zero C# errors, zero unexpected gameplay errors and zero unhandled log exceptions. Run `86a31e515535420a95bed6a1c379eb7c`; sources remained frozen. The ordinary opening starts vulnerable and finishes the cloth expedition and five interiors alive at **31 HP**, without F12 or synthetic healing. The later long regional journey still labels its F12 protection separately.
- The actual seed64 new-game, nine borders, delivery, notes, F5/F6, camera 1.2× view and full reveal all remain covered. Private saves, scenes, GameView and bootstrap setting restoration passed. This is one deterministic journey, not broad difficulty certification or an unfamiliar-player test.
- Seventeen native 1080p captures were produced. Root re-inspected western spawn, ropeshop interior and completed regional notes in this continuation. Earlier RS27 reviewed all seventeen corresponding views. The completed note still advertises outstanding delivery instructions; this is a subsequent R2 usability finding, not a false R1 art failure.
- Independent cold-eye review compared all 99 candidate hashes and immutable base hashes. Renderer preload/alias/missing-resource cases agree; generic controls, tint, fog, claim/release, incremental repaint and actual factory content remain covered by the existing 32-case adversarial fixture. Ten player-flow hypotheses mapped to substantive existing tests or the explicitly pending native sprite gate. No material source defect in that bounded review.

## Remaining acceptance

1. Inspect the installed building variants and regeneration/GUID evidence.
2. Run the dedicated ground-sprite before/after benchmark with the same harness. Require real valid timing/counter samples, bright/dim art, exact candidate identities and native inventory mutations. Synthetic stimulus is not keyboard input evidence.
3. Complete review of any new harness or resulting fixes, rerun the appropriate test suite, and compare the accepted runtime source to the branch being integrated.
4. Update living docs and integrate verified work into main without interrupting the user's Editor. Do not overwrite concurrent work or copy clone-only launch helpers/private project settings.

## Honesty bounds and performance

R104 is script-observable native input acceptance, with selected image inspection. Its 60-second aggregate Editor profile includes harness/capture work and cannot isolate these seven sprite changes. The dedicated A/B must report renderer marker sample availability/count/max/average and workload counters separately. A native image can establish the captured appearance, not general comfort or long-term balance.

Production remains seven preloaded resources and exact static routing; no new per-frame allocations, scans or gameplay state. Bench-only sampling and setup must not enter ordinary play.

## Ground benchmark development (native acceptance, not production changes)

- R105 exposed a test-assembly compile error in the first harness contract fixture; switched to the repository's reflection pattern instead of widening assembly dependencies. R105b: **49/49** (17 receipt-validator controls + 32 existing equipment adversarial), zero compiler errors.
- R106 was rejected, not counted as acceptance: the project Play start scene overrode the owned empty scene; the resulting normal bootstrap contaminated the fixture. The batch now snapshots, clears and restores `EditorSceneManager.playModeStartScene`, with exact settings restoration and loud absence-of-bootstrap/renderer preconditions. The same run exposed the requirement that the Main Thread profiler marker also use frame aggregation. All five recorders now aggregate without wrapping; no stale `LastValue` reuse.
- R106b confirmed these fixes (zero unexpected errors, exact cleanup/source restoration), but failed its first pickup check because the harness expected the obsolete `FloorTile_` family. The shipped floor is a `floor_m` macro tile; corrected that assertion and added the actual observed tile to failure output. No renderer behavior was changed to accommodate the benchmark.
- The BEFORE control is reconstructed from immutable source `26fbe544`, with all other content and the same harness held constant. It is not a preimplementation timing capture. Failed attempts remain archived as failures.

- R106c reached **67/67** native checks and the full 75-second workload, but was rejected because teardown recomposed the report using the restored Editor's resolution and reset clock. Preserve the first measured dimensions/time on later error/cleanup writes. R106d repeats the whole workload with that correction: **67/67**, exit0, zero C# errors/unhandled errors, exact source/settings restoration, valid report/counterchecks/artifacts. Run `4e3cbdd6c10b4ad999301f8992ef20cb`. The AFTER side follows with the identical harness.

- Root rejected R106d/R107 **visual** acceptance despite their 67/67 logical passes: the test camera culled default-layer overlays/light, producing black screenshots. The synthetic camera now uses the actual gameplay mask (default+world); captured first-item pixels must contain visible lit art. Before/after are repeated with identical corrected setup. Cold-eye also found a Play→Edit reload gap in error subscription; the finishing branch now reattaches `OnLog`, matching the existing native audit pattern. Neither fix changes normal gameplay.

## Final native art and profile pair

Accepted BEFORE: **R106e**, run `958a8772110a41529b19641b9029e316`. Accepted AFTER: **R107b**, run `c24fc9e53abb46a0892ad828c102c229`. Each:67/67 checks, zero C# or unexpected errors, 75 seconds across 3 phases, 100 walking actions and 50 pickup + 50 drop actions, 100 overlay lifecycle checks, complete private-save/scene/view/preferences/settings restoration, source hashes frozen. Both use the identical final harness. Root inspected BEFORE enlarged normal art and AFTER native1920×1080 plus normal/dim enlarged art. Seven equipment silhouettes are distinct; repeated aliases retain consistent tint; transparent edges, alignment and outlines are intact. Dim lighting intentionally loses interior detail while retaining silhouettes. These are 16px fallback ground assets, not a replacement for voxel presentation or worn equipment.

| Environment sprite metric | BEFORE | AFTER |
|---|---:|---:|
| Idle calls / resolved cells |0 /0|0 /0|
| Walk calls / resolved cells / tile writes |100 /200,000 /817,100|100 /200,000 /817,100|
| Walk total marker time |384.025ms|379.732ms|
| Walk p99 /max observation |4.006 /4.473ms|4.102 /5.029ms|
| Pickup/drop calls /resolved cells /tile writes |100 /900 /7,050|100 /900 /7,050|
| Pickup/drop total marker time |5.684ms|6.290ms|
| Pickup/drop p99 /max observation |0.061 /0.070ms|0.067 /0.095ms|

All 5 markers have valid handles and approximately 1,500 observations per phase. Raw frames and complete per-marker count/average/p99/max/GC bytes are retained beside receipts. The active sprite marker has exactly 100 actual invocations per phase; Unity's aggregated zero samples in idle do not imply work. This one Editor pair shows unchanged work counts, not statistical equivalence, no-allocation proof, an FPS claim or an optimization. Main-thread maxima and GC vary with Editor/harness overhead. No added per-frame work was required by the production mapping patch.

Building art acceptance is also complete: all 72 installed FBXs match fresh source geometry/topology/UV fingerprints; all 18 families have 4 distinct variants. Three installed-model contact sheets were independently reviewed and inspected by root. 154 existing kit GUIDs remain intact; 6,767 metadata files had no collision/malformed GUID. 15 offline tests pass. This closes publication/art acceptance, not player construction or native block destruction.

R108 is the final full-suite gate before main integration. The 12,997 tracked Assets/Packages paths in live release-candidate and the clone were byte-compared before R108; all match except deliberately ignored MCP logs. The eight new benchmark files are separate additions to publish after acceptance.

## Acceptance and review disposition

R108 full suite: **14,917 / 14,918 pass**, zero compiler errors, one known `Diag_DisabledChannelOverhead_BoundedPerCall` timing failure (252ns versus200ns). No threshold or gameplay code changed. R108b reran the entire three-case DiagPerf fixture and seventeen benchmark contract cases: **20/20 pass**, zero compiler errors. The previous R103 full candidate was14,901/14,901. Report the full run and isolated retry separately; do not rename R108 green.

Final independent review reconciled every raw metric sum/max/p99, confirmed consecutive rendered frames, proved BEFORE matches immutable source, and found only the expected renderer file difference across final source manifests. No material issue remains in this bounded acceptance. Root verified all99 candidate hashes match both live and tested copies. The final source/asset acceptance includes the existing32-case adversarial gate,17benchmark receipt controls, native ordinary opening and imported-block review.

Self-review: 🟡 harness scene/profile/report/camera/lifecycle findings fixed and rerun; 🔵 exact scope/provenance documented; ⚪ single-run timing variation and inherited nanosecond flake remain recorded, not optimized away; 🧪 subjective comfort, broad balance and player construction remain outside this repair. Qud classification remains CoO-original. Scope divergence: original BEFORE capture was not taken before implementation; the final comparison reconstructs that renderer from immutable committed source with all other content/harness held constant. Native inventory stimuli are direct API calls, while the separate R104 opening is actual queued input.

All R1 runtime repairs and acceptance infrastructure are promoted together to local main, preserving the external release-candidate history. No remote push, live save reset, camera change, visibility change or Editor restart is part of this promotion. Next implementation: R2 truthful regional outcomes.
