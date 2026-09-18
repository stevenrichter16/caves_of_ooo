# Native starter-spell performance — intermediate-pre-refinement

Run `35e60ec4b5cc46a18f90b7d33ab2b8dc`. **PASS: 418/418 independent checks.** This is the intermediate capture before the final persistent-aura visual refinement; preserve it as history.

Verified the SHA-256 of the raw CSV and all **108 PNG captures**; all images decode at **1920×1080**. The same-run cleanup succeeded and its private save root was removed.

**81.780 seconds**, **24,782 raw frames**, **7,743 frames with native spell meshes**, **52 profile commands**. The complete run contains **65 deterministic command casts**, a separate **one real keyboard cast**, and successful sampled cast gestures for all seven spells.

| Phase | Seconds | Frames | Active frames | Casts | Peak meshes |
| --- | ---: | ---: | ---: | ---: | ---: |
| Town Full | 20.425 | 5,496 | 1,749 | 13 | 35 |
| Town Reduced | 20.423 | 5,156 | 1,578 | 13 | 27 |
| South Full | 20.509 | 6,824 | 2,167 | 13 | 35 |
| South Reduced | 20.423 | 7,306 | 2,249 | 13 | 27 |

Times below are **average / p95 / p99 / maximum milliseconds**, calculated from every raw phase frame. No outliers were discarded.

| Phase | Main Thread | Input.Update | ZoneRenderer.LateUpdate |
| --- | ---: | ---: | ---: |
| Town Full | 3.711 / 4.265 / 6.763 / 688.694 | 0.016 / 0.022 / 0.026 / 0.064 | 1.049 / 1.139 / 1.237 / 5.455 |
| Town Reduced | 3.956 / 4.641 / 6.371 / 484.280 | 0.017 / 0.023 / 0.026 / 0.089 | 1.120 / 1.252 / 1.658 / 6.037 |
| South Full | 2.911 / 3.354 / 3.793 / 21.182 | 0.011 / 0.014 / 0.019 / 0.044 | 0.948 / 1.022 / 1.114 / 7.022 |
| South Reduced | 2.791 / 3.215 / 3.651 / 25.927 | 0.010 / 0.014 / 0.018 / 0.040 | 0.905 / 0.978 / 1.072 / 6.731 |

GC values are **average / p95 / p99 / maximum KiB allocated per frame**, from `GC Allocated In Frame`; they are neither heap growth nor bytes allocated exclusively by spells.

| Phase | GC KiB/frame | Mean / max draw calls | Mean / max triangles | Wall frames >16.67 / >50 ms |
| --- | ---: | ---: | ---: | ---: |
| Town Full | 30.332 / 37.144 / 39.380 / 1241.750 | 234.0 / 266 | 671,300 / 671,747 | 15 / 1 |
| Town Reduced | 106.437 / 594.824 / 1140.178 / 63911.502 | 232.6 / 259 | 671,258 / 671,609 | 17 / 1 |
| South Full | 29.426 / 29.175 / 30.560 / 186.163 | 126.1 / 157 | 295,419 / 295,850 | 12 / 0 |
| South Reduced | 29.399 / 29.175 / 30.284 / 197.456 | 124.6 / 149 | 295,394 / 295,748 | 12 / 0 |

Preparation before input took **555.62 ms loading**, **9.44 ms validating**, and **4.31 ms creating the 384-view pool**. The first real cast kept the library count 1→1 and pool 384→384, displayed 14 peak meshes, and reached Normal input state. Its maximum Main Thread time was **24.41 ms**. This records startup work moved before the cast, not removed work.

Can verify: report/file provenance, raw numeric measurements and phase coverage, the recorded native mesh/command/gesture observations, and successful isolated-save cleanup.

Cannot verify from these numbers: subjective motion quality, comfort, performance of a standalone build or a process-cold launch, or long-session leak behavior. Whole-game/editor work, deterministic fixture resets and profiler overhead are included. The Full/Reduced samples ran sequentially and are not a controlled randomized benchmark; do not infer a speedup from their difference.

Machine: Unity 6000.3.4f1, Apple M5, 1920×1080, vSync 0, targetFrameRate -1. Quantiles use linear interpolation at `(N-1)×p`.

Raw receipt: `/Users/steven/caves-of-ooo/Docs/Verification/StarterSpell3D/Integration/NativeAudit/SSN-35e60ec4b5cc46a18f90b7d33ab2b8dc-native.json`
CSV SHA-256: `c1764e0052d2a860013513ec215742f1a488df7fc1007db42bee262fa8d9bc1e`

Retained outlier context: Town Full's 688.69 ms Main Thread sample occurs near
Unity frames 4144–4145 (the adjacent frame records 683.54 ms delta); Town Reduced's
484.28 ms sample occurs at frame 13260 with 65,445,378 GC allocation bytes. Input
and Zone markers stay small around both. `ProfilerRecorder.LastValue` and frame
delta may reflect different completion boundaries. These counters establish the
spikes and preserve their nearby samples; they do not identify their cause.
