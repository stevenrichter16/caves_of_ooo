# Native starter-spell performance — Readability R09 first native candidate

Run `f83f27a1475f4e11866f7dde09c33331`. **PASS: 419/419 independent checks.** This is the first Readability native candidate capture before direction-proof extension and final visual acceptance. Preserve it as source-specific history.

Verified the SHA-256 of the raw CSV and all **108 PNG captures**; all images decode at **1920×1080**. The same-run cleanup succeeded and its private save root was removed.

**81.686 seconds**, **26,158 raw frames**, **8,068 frames with native spell meshes**, **52 profile commands**. The complete run contains **65 deterministic command casts**, a separate **one real keyboard cast**, and successful sampled cast gestures for all seven spells.

| Phase | Seconds | Frames | Active frames | Casts | Peak meshes |
| --- | ---: | ---: | ---: | ---: | ---: |
| Town Full | 20.422 | 5,812 | 1,838 | 13 | 70 |
| Town Reduced | 20.423 | 5,347 | 1,684 | 13 | 46 |
| South Full | 20.421 | 7,485 | 2,238 | 13 | 70 |
| South Reduced | 20.420 | 7,514 | 2,308 | 13 | 46 |

Times below are **average / p95 / p99 / maximum milliseconds**, calculated from every raw phase frame. No outliers were discarded.

| Phase | Main Thread | Input.Update | ZoneRenderer.LateUpdate |
| --- | ---: | ---: | ---: |
| Town Full | 3.509 / 4.029 / 4.793 / 686.872 | 0.015 / 0.022 / 0.025 / 0.041 | 0.997 / 1.102 / 1.210 / 5.335 |
| Town Reduced | 3.814 / 4.589 / 6.229 / 518.879 | 0.016 / 0.023 / 0.026 / 0.097 | 1.071 / 1.217 / 1.648 / 6.316 |
| South Full | 2.724 / 3.263 / 3.826 / 19.766 | 0.010 / 0.014 / 0.018 / 0.046 | 0.886 / 0.991 / 1.098 / 6.961 |
| South Reduced | 2.714 / 3.067 / 3.457 / 575.643 | 0.009 / 0.013 / 0.016 / 0.027 | 0.865 / 0.946 / 1.046 / 7.371 |

GC values are **average / p95 / p99 / maximum KiB allocated per frame**, from `GC Allocated In Frame`; they are neither heap growth nor bytes allocated exclusively by spells.

| Phase | GC KiB/frame | Mean / max draw calls | Mean / max triangles | Wall frames >16.67 / >50 ms |
| --- | ---: | ---: | ---: | ---: |
| Town Full | 30.348 / 37.144 / 39.380 / 1167.707 | 240.6 / 301 | 672,211 / 678,537 | 14 / 1 |
| Town Reduced | 110.130 / 642.285 / 1088.252 / 72950.558 | 235.6 / 276 | 671,763 / 676,015 | 16 / 1 |
| South Full | 29.406 / 29.168 / 30.285 / 178.634 | 132.1 / 192 | 296,288 / 302,654 | 12 / 0 |
| South Reduced | 29.443 / 29.124 / 30.551 / 430.023 | 127.7 / 168 | 295,886 / 300,166 | 12 / 1 |

Preparation before input took **535.12 ms loading**, **17.09 ms validating**, and **3.08 ms creating the 384-view pool**. The first real cast kept the library count 1→1 and pool 384→384, displayed 31 peak meshes, and reached Normal input state. Its maximum Main Thread time was **274.70 ms**. This records startup work moved before the cast, not removed work.

Can verify: report/file provenance, raw numeric measurements and phase coverage, the recorded native mesh/command/gesture observations, and successful isolated-save cleanup.

Cannot verify from these numbers: subjective motion quality, comfort, performance of a standalone build or a process-cold launch, or long-session leak behavior. Whole-game/editor work, deterministic fixture resets and profiler overhead are included. The Full/Reduced samples ran sequentially and are not a controlled randomized benchmark; do not infer a speedup from their difference.

Machine: Unity 6000.3.4f1, Apple M5, 1920×1080, vSync 0, targetFrameRate -1. Quantiles use linear interpolation at `(N-1)×p`.

Raw receipt: `/Users/steven/caves-of-ooo/Docs/Verification/StarterSpell3D/Integration/NativeAudit/SSN-f83f27a1475f4e11866f7dde09c33331-native.json`
CSV SHA-256: `063fed06004a9eb2eef4dafd8a908be4e70b930409e94d1a759547b25fa6a9da`
