# Native starter-spell performance — Readability candidate2 native directional acceptance

Run `487d034a09814ec09d5e705b85cb852c`. **PASS: 974/974 independent checks.** Candidate2 native execution and directional verification passed; final visual acceptance and full-suite close-out remain pending. This source-specific receipt preserves every recorded profile outlier.

Verified the SHA-256 of the raw CSV and all **288 PNG captures**; all images decode at **1920×1080**. The same-run cleanup succeeded and its private save root was removed.

**82.232 seconds**, **25,788 raw frames**, **7,671 frames with native spell meshes**, **52 profile commands**. The complete run contains **80 deterministic command casts**, a separate **one real keyboard cast**, and successful sampled cast gestures for all seven spells.

| Phase | Seconds | Frames | Active frames | Casts | Peak meshes |
| --- | ---: | ---: | ---: | ---: | ---: |
| Town Full | 20.423 | 5,604 | 1,771 | 13 | 89 |
| Town Reduced | 20.425 | 5,167 | 1,440 | 13 | 49 |
| South Full | 20.963 | 7,261 | 2,177 | 13 | 89 |
| South Reduced | 20.421 | 7,756 | 2,283 | 13 | 49 |

Times below are **average / p95 / p99 / maximum milliseconds**, calculated from every raw phase frame. No outliers were discarded.

| Phase | Main Thread | Input.Update | ZoneRenderer.LateUpdate |
| --- | ---: | ---: | ---: |
| Town Full | 3.639 / 4.232 / 4.877 / 692.675 | 0.015 / 0.022 / 0.025 / 0.073 | 1.012 / 1.119 / 1.209 / 5.030 |
| Town Reduced | 3.947 / 4.802 / 7.314 / 495.895 | 0.016 / 0.023 / 0.026 / 0.149 | 1.060 / 1.195 / 1.624 / 5.853 |
| South Full | 2.883 / 3.279 / 3.678 / 584.521 | 0.010 / 0.013 / 0.017 / 0.033 | 0.891 / 0.986 / 1.061 / 6.279 |
| South Reduced | 2.629 / 3.143 / 3.596 / 22.508 | 0.009 / 0.013 / 0.016 / 0.035 | 0.840 / 0.929 / 1.031 / 6.385 |

GC values are **average / p95 / p99 / maximum KiB allocated per frame**, from `GC Allocated In Frame`; they are neither heap growth nor bytes allocated exclusively by spells.

| Phase | GC KiB/frame | Mean / max draw calls | Mean / max triangles | Wall frames >16.67 / >50 ms |
| --- | ---: | ---: | ---: | ---: |
| Town Full | 30.440 / 37.144 / 39.380 / 1178.497 | 244.6 / 321 | 673,138 / 682,303 | 11 / 1 |
| Town Reduced | 113.851 / 645.097 / 1177.948 / 74685.619 | 236.2 / 279 | 671,940 / 676,579 | 18 / 1 |
| South Full | 29.468 / 29.175 / 30.398 / 454.088 | 135.6 / 211 | 297,158 / 306,404 | 14 / 1 |
| South Reduced | 29.381 / 29.175 / 30.140 / 194.099 | 128.3 / 171 | 296,096 / 300,710 | 9 / 0 |

Preparation before input took **553.38 ms loading**, **12.47 ms validating**, and **3.99 ms creating the 384-view pool**. The first real cast kept the library count 1→1 and pool 384→384, displayed 47 peak meshes, and reached Normal input state. Its maximum Main Thread time was **24.81 ms**. This records startup work moved before the cast, not removed work.

Can verify: report/file provenance, raw numeric measurements and phase coverage, the recorded native mesh/command/gesture observations, and successful isolated-save cleanup.

Cannot verify from these numbers: subjective motion quality, comfort, performance of a standalone build or a process-cold launch, or long-session leak behavior. Whole-game/editor work, deterministic fixture resets and profiler overhead are included. The Full/Reduced samples ran sequentially and are not a controlled randomized benchmark; do not infer a speedup from their difference.

Machine: Unity 6000.3.4f1, Apple M5, 1920×1080, vSync 0, targetFrameRate -1. Quantiles use linear interpolation at `(N-1)×p`.

Raw receipt: `/Users/steven/caves-of-ooo/Docs/Verification/StarterSpell3D/Integration/NativeAudit/SSN-487d034a09814ec09d5e705b85cb852c-native.json`
CSV SHA-256: `77d7b29654d4a806861fd03e0c1a71be3d6596ade13bd94131c6f2a1a575cacf`
