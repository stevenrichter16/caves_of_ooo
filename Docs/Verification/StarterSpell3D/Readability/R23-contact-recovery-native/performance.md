# Native starter-spell performance — Readability continuity art with contact recovery native acceptance

Run `e29cae1c81ba4cac8fa92f275c1b719b`. **PASS: 968/968 independent checks.** Complete native execution and15direction gates passed with frozen Art59 and the contact-recovery source. Parent final directional visual review and full-suite close-out remain pending. R19/R20 actual visual-skip failures remain retained; this receipt does not claim the underlying Editor stalls were eliminated.

Verified the SHA-256 of the raw CSV and all **286 PNG captures**; all images decode at **1920×1080**. The same-run cleanup succeeded and its private save root was removed.

**81.693 seconds**, **25,674 raw frames**, **7,828 frames with native spell meshes**, **52 profile commands**. The complete run contains **80 deterministic command casts**, a separate **one real keyboard cast**, and successful sampled cast gestures for all seven spells.

| Phase | Seconds | Frames | Active frames | Casts | Peak meshes |
| --- | ---: | ---: | ---: | ---: | ---: |
| Town Full | 20.426 | 5,722 | 1,746 | 13 | 89 |
| Town Reduced | 20.426 | 5,167 | 1,635 | 13 | 49 |
| South Full | 20.419 | 7,387 | 2,221 | 13 | 89 |
| South Reduced | 20.422 | 7,398 | 2,226 | 13 | 49 |

Times below are **average / p95 / p99 / maximum milliseconds**, calculated from every raw phase frame. No outliers were discarded.

| Phase | Main Thread | Input.Update | ZoneRenderer.LateUpdate |
| --- | ---: | ---: | ---: |
| Town Full | 3.564 / 4.476 / 6.794 / 23.043 | 0.015 / 0.022 / 0.026 / 0.044 | 1.016 / 1.143 / 1.283 / 7.227 |
| Town Reduced | 3.948 / 4.629 / 5.614 / 761.029 | 0.016 / 0.023 / 0.026 / 0.066 | 1.051 / 1.191 / 1.605 / 5.789 |
| South Full | 2.760 / 3.302 / 3.790 / 19.774 | 0.010 / 0.014 / 0.019 / 0.033 | 0.894 / 1.009 / 1.103 / 6.668 |
| South Reduced | 2.756 / 3.308 / 4.090 / 21.560 | 0.010 / 0.014 / 0.018 / 0.060 | 0.890 / 1.002 / 1.115 / 7.316 |

GC values are **average / p95 / p99 / maximum KiB allocated per frame**, from `GC Allocated In Frame`; they are neither heap growth nor bytes allocated exclusively by spells.

| Phase | GC KiB/frame | Mean / max draw calls | Mean / max triangles | Wall frames >16.67 / >50 ms |
| --- | ---: | ---: | ---: | ---: |
| Town Full | 30.173 / 37.144 / 39.380 / 405.772 | 244.1 / 321 | 673,063 / 682,305 | 13 / 0 |
| Town Reduced | 116.558 / 655.192 / 1202.556 / 75988.647 | 237.0 / 280 | 672,035 / 676,599 | 16 / 2 |
| South Full | 29.414 / 29.175 / 30.560 / 180.228 | 135.8 / 211 | 297,159 / 306,416 | 11 / 0 |
| South Reduced | 29.430 / 29.175 / 30.298 / 435.998 | 128.6 / 171 | 296,115 / 300,732 | 12 / 0 |

Preparation before input took **592.73 ms loading**, **12.98 ms validating**, and **2.82 ms creating the 384-view pool**. The first real cast kept the library count 1→1 and pool 384→384, displayed 47 peak meshes, and reached Normal input state. Its maximum Main Thread time was **24.71 ms**. This records startup work moved before the cast, not removed work.

Can verify: report/file provenance, raw numeric measurements and phase coverage, the recorded native mesh/command/gesture observations, and successful isolated-save cleanup.

Cannot verify from these numbers: subjective motion quality, comfort, performance of a standalone build or a process-cold launch, or long-session leak behavior. Whole-game/editor work, deterministic fixture resets and profiler overhead are included. The Full/Reduced samples ran sequentially and are not a controlled randomized benchmark; do not infer a speedup from their difference.

Machine: Unity 6000.3.4f1, Apple M5, 1920×1080, vSync 0, targetFrameRate -1. Quantiles use linear interpolation at `(N-1)×p`.

Raw receipt: `/Users/steven/caves-of-ooo/Docs/Verification/StarterSpell3D/Integration/NativeAudit/SSN-e29cae1c81ba4cac8fa92f275c1b719b-native.json`
CSV SHA-256: `574c1ada6eed3fa0c73852195c7c34034967693c1b1ec8dd81966adbd8e61de9`
