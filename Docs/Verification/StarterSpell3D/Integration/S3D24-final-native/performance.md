# Native starter-spell performance — final-post-refinement

Run `b89c353ebeba43cdbbdfb69b16949f83`. **PASS: 419/419 independent checks.** This is the final post-refinement native capture; prior intermediate receipts are preserved separately.

Verified the SHA-256 of the raw CSV and all **108 PNG captures**; all images decode at **1920×1080**. The same-run cleanup succeeded and its private save root was removed.

**81.689 seconds**, **26,346 raw frames**, **8,184 frames with native spell meshes**, **52 profile commands**. The complete run contains **65 deterministic command casts**, a separate **one real keyboard cast**, and successful sampled cast gestures for all seven spells.

| Phase | Seconds | Frames | Active frames | Casts | Peak meshes |
| --- | ---: | ---: | ---: | ---: | ---: |
| Town Full | 20.423 | 5,970 | 1,854 | 13 | 35 |
| Town Reduced | 20.424 | 5,178 | 1,650 | 13 | 27 |
| South Full | 20.422 | 7,388 | 2,297 | 13 | 35 |
| South Reduced | 20.419 | 7,810 | 2,383 | 13 | 27 |

Times below are **average / p95 / p99 / maximum milliseconds**, calculated from every raw phase frame. No outliers were discarded.

| Phase | Main Thread | Input.Update | ZoneRenderer.LateUpdate |
| --- | ---: | ---: | ---: |
| Town Full | 3.416 / 4.116 / 4.662 / 21.653 | 0.015 / 0.021 / 0.025 / 0.034 | 1.034 / 1.125 / 1.199 / 5.487 |
| Town Reduced | 3.939 / 4.664 / 7.174 / 716.734 | 0.016 / 0.022 / 0.027 / 0.048 | 1.091 / 1.222 / 1.639 / 14.514 |
| South Full | 2.760 / 3.179 / 3.525 / 23.173 | 0.010 / 0.014 / 0.017 / 0.025 | 0.913 / 0.979 / 1.061 / 7.088 |
| South Reduced | 2.611 / 3.093 / 3.511 / 19.567 | 0.009 / 0.013 / 0.017 / 0.034 | 0.873 / 0.954 / 1.049 / 7.679 |

GC values are **average / p95 / p99 / maximum KiB allocated per frame**, from `GC Allocated In Frame`; they are neither heap growth nor bytes allocated exclusively by spells.

| Phase | GC KiB/frame | Mean / max draw calls | Mean / max triangles | Wall frames >16.67 / >50 ms |
| --- | ---: | ---: | ---: | ---: |
| Town Full | 30.124 / 37.144 / 39.380 / 402.909 | 234.2 / 265 | 671,291 / 671,745 | 13 / 0 |
| Town Reduced | 107.343 / 548.509 / 1109.201 / 64968.867 | 232.9 / 259 | 671,259 / 671,595 | 20 / 2 |
| South Full | 29.408 / 29.173 / 30.514 / 173.720 | 126.1 / 157 | 295,418 / 295,864 | 12 / 0 |
| South Reduced | 29.382 / 29.146 / 30.149 / 172.120 | 124.7 / 149 | 295,394 / 295,726 | 12 / 0 |

Preparation before input took **487.84 ms loading**, **3.31 ms validating**, and **2.61 ms creating the 384-view pool**. The first real cast kept the library count 1→1 and pool 384→384, displayed 14 peak meshes, and reached Normal input state. Its maximum Main Thread time was **21.53 ms**. This records startup work moved before the cast, not removed work.

Can verify: report/file provenance, raw numeric measurements and phase coverage, the recorded native mesh/command/gesture observations, and successful isolated-save cleanup.

Cannot verify from these numbers: subjective motion quality, comfort, performance of a standalone build or a process-cold launch, or long-session leak behavior. Whole-game/editor work, deterministic fixture resets and profiler overhead are included. The Full/Reduced samples ran sequentially and are not a controlled randomized benchmark; do not infer a speedup from their difference.

Machine: Unity 6000.3.4f1, Apple M5, 1920×1080, vSync 0, targetFrameRate -1. Quantiles use linear interpolation at `(N-1)×p`.

Raw receipt: `/Users/steven/caves-of-ooo/Docs/Verification/StarterSpell3D/Integration/NativeAudit/SSN-b89c353ebeba43cdbbdfb69b16949f83-native.json`
CSV SHA-256: `67e9cb662c4ade5b1bd755d9422b0fd7b372955d97694fe09d0a8e6aa602fd1e`
