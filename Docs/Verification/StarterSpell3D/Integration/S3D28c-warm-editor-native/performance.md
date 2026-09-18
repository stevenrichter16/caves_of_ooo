# Native starter-spell performance — final-ground-refinement-warm-editor

Run `0d7a74963b314b6f9c9270a00d13846d`. **PASS: 419/419 independent checks.** This is the final post-refinement native capture; prior intermediate receipts are preserved separately.

Verified the SHA-256 of the raw CSV and all **108 PNG captures**; all images decode at **1920×1080**. The same-run cleanup succeeded and its private save root was removed.

**81.683 seconds**, **25,835 raw frames**, **7,901 frames with native spell meshes**, **52 profile commands**. The complete run contains **65 deterministic command casts**, a separate **one real keyboard cast**, and successful sampled cast gestures for all seven spells.

| Phase | Seconds | Frames | Active frames | Casts | Peak meshes |
| --- | ---: | ---: | ---: | ---: | ---: |
| Town Full | 20.421 | 5,767 | 1,694 | 13 | 35 |
| Town Reduced | 20.423 | 5,312 | 1,599 | 13 | 27 |
| South Full | 20.421 | 7,317 | 2,245 | 13 | 35 |
| South Reduced | 20.419 | 7,439 | 2,363 | 13 | 27 |

Times below are **average / p95 / p99 / maximum milliseconds**, calculated from every raw phase frame. No outliers were discarded.

| Phase | Main Thread | Input.Update | ZoneRenderer.LateUpdate |
| --- | ---: | ---: | ---: |
| Town Full | 3.536 / 4.161 / 6.584 / 603.200 | 0.015 / 0.022 / 0.025 / 0.047 | 1.028 / 1.119 / 1.199 / 22.827 |
| Town Reduced | 3.839 / 4.615 / 5.829 / 482.731 | 0.016 / 0.023 / 0.026 / 0.058 | 1.100 / 1.227 / 1.600 / 11.560 |
| South Full | 2.787 / 3.221 / 3.651 / 19.257 | 0.009 / 0.014 / 0.017 / 0.031 | 0.920 / 0.989 / 1.081 / 6.775 |
| South Reduced | 2.741 / 3.238 / 3.770 / 570.393 | 0.009 / 0.014 / 0.018 / 0.032 | 0.883 / 0.983 / 1.094 / 7.448 |

GC values are **average / p95 / p99 / maximum KiB allocated per frame**, from `GC Allocated In Frame`; they are neither heap growth nor bytes allocated exclusively by spells.

| Phase | GC KiB/frame | Mean / max draw calls | Mean / max triangles | Wall frames >16.67 / >50 ms |
| --- | ---: | ---: | ---: | ---: |
| Town Full | 30.365 / 37.144 / 39.380 / 1136.529 | 233.8 / 265 | 671,282 / 671,747 | 14 / 1 |
| Town Reduced | 105.643 / 594.011 / 1139.975 / 65025.817 | 232.7 / 259 | 671,260 / 671,613 | 17 / 1 |
| South Full | 29.416 / 29.210 / 30.289 / 173.907 | 126.0 / 157 | 295,418 / 295,864 | 12 / 0 |
| South Reduced | 29.448 / 29.210 / 30.268 / 522.313 | 124.8 / 149 | 295,395 / 295,732 | 13 / 1 |

Preparation before input took **534.92 ms loading**, **3.59 ms validating**, and **2.66 ms creating the 384-view pool**. The first real cast kept the library count 1→1 and pool 384→384, displayed 14 peak meshes, and reached Normal input state. Its maximum Main Thread time was **24.29 ms**. This records startup work moved before the cast, not removed work.

Can verify: report/file provenance, raw numeric measurements and phase coverage, the recorded native mesh/command/gesture observations, and successful isolated-save cleanup.

Cannot verify from these numbers: subjective motion quality, comfort, performance of a standalone build or a process-cold launch, or long-session leak behavior. Whole-game/editor work, deterministic fixture resets and profiler overhead are included. The Full/Reduced samples ran sequentially and are not a controlled randomized benchmark; do not infer a speedup from their difference.

Machine: Unity 6000.3.4f1, Apple M5, 1920×1080, vSync 0, targetFrameRate -1. Quantiles use linear interpolation at `(N-1)×p`.

Raw receipt: `/Users/steven/caves-of-ooo/Docs/Verification/StarterSpell3D/Integration/NativeAudit/SSN-0d7a74963b314b6f9c9270a00d13846d-native.json`
CSV SHA-256: `5ec74216b8ecb71ad5bd56f8a39f687eec3ea2280e1e4e31f7f39dd98f02c81d`


Editor discovery setup was recorded separately for this exact run/private root: **2.8098 ms total**, 18 resources and 34 tools. Both discoveries succeeded before the driver started. Its heap deltas are net used managed memory, not total allocations. This does **not** explain the prior 480.188 ms frame delta or 657.88 ms capture gap, nor demonstrate a stall fix. The accepted run still records Main Thread maxima of **603.20 / 482.73 / 19.26 / 570.39 ms** across the four phases; every raw outlier remains included. S3D28 and S3D28b are retained as failed runs. Runtime clocks and all acceptance gates remain unchanged.
