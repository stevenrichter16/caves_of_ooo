# Final refined ring — native Editor performance

Accepted run `67d54c3651e840d2a0d59d0d3a5ba651` on Unity 6000.3.4f1, Apple M5/Apple M5, Metal, actual1920×1080 GameView. Full-detail ring view; no simultaneous Blender workload. Raw counters and cleanup were independently recomputed successfully by `Tools/SpawnRing3D/recompute_profile.py`.

Measured 80.023s of sustained segments: eight5s idle controls and eight5s paced keyboard-walking phases, 26,501 sampled frames including separate bind phases. Minimum0.8s between commands;55 successful profile moves,0 rejected targets/unmoved inputs in this accepted run. Native AI/health/FOV/turns remain unchanged. Player remained40HP. Separate24 border APIs and native Harvest/one bump action follow profiling.

| Zone | Steps | Frame p95 ms | Frame p99 ms | Frame max ms | ZoneRenderer max ms | GPU p99 ms |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Overworld.2.5.0 | 7 | 3.22 | 3.63 | 25.46 | 19.32 | 1.64 |
| Overworld.3.5.0 | 7 | 3.61 | 4.34 | 25.28 | 19.48 | 1.70 |
| Overworld.4.5.0 | 7 | 3.25 | 3.58 | 25.27 | 19.12 | 1.62 |
| Overworld.2.6.0 | 6 | 3.76 | 4.38 | 674.38 | 23.17 | 2.93 |
| Overworld.4.6.0 | 7 | 4.09 | 5.14 | 390.88 | 21.07 | 3.04 |
| Overworld.2.7.0 | 7 | 4.02 | 4.62 | 32.00 | 24.08 | 3.13 |
| Overworld.3.7.0 | 7 | 3.91 | 4.57 | 25.40 | 18.55 | 3.39 |
| Overworld.4.7.0 | 7 | 3.93 | 4.50 | 29.05 | 22.12 | 3.42 |

## Interpretation and limits

First-binding phases total10.597s, including ordinary binder/autosave/readiness settling, not pure GPU or cold asset loading. The required ZoneRenderer/Input/MainThread/GC counters are available. GPU/draw/triangle counts are available; Render Thread is unavailable and remains NA. Per-phase raw percentiles/counts use their actual declared units and retain missing/positive sample counts.

Typical frame percentiles in this paced run are low, but isolated full-Editor frames reach674.38ms and390.88ms. These exceed a16.7ms frame budget and are retained. Corresponding named ZoneRenderer/Input markers are much smaller; this capture cannot attribute the remaining whole-Editor time to a specific subsystem. Do not call this stable60fps, a production-build budget, or a proven speedup. Whole-process GC includes Editor/UI/observer and has a57MB frame in the E segment; it is not an isolated ring allocation measurement.

The earlier69.314s fixed-lane run and80.682s rapid adaptive run are both invalid/incomplete: the first encountered a blocked lane, the second ended in real player death after244 successful profile moves. Their raw samples remain. The paced run is a different workload, not retroactive acceptance of either failed stress attempt. Route choice can read current native occupancy and is explicitly diagnostic, not player-knowledge behavior.

Sampling spans separate local idle/walk phases, with fixture placement only between phases. It is not an uninterrupted traversal or combat benchmark. Latest completed profiler samples may lag frame/phase boundaries. Normal FOV and viewport cropping mean full-zone source triangle totals are not per-frame rendered counts. Low detail was functionally exercised by native acceptance, but no full-versus-low performance comparison or mesh-LOD claim is made.

Gameplay/cleanup scope reports0 unexpected errors and all checks pass. The complete Editor process log still contains the separately documented MCP startup connection noise; this is not a clean whole-process environment assertion.

Evidence: `R3D-67d54c3651e840d2a0d59d0d3a5ba651-profile-full.json`, `R3D-67d54c3651e840d2a0d59d0d3a5ba651-profile-full-frames.csv.gz`, `R3D-67d54c3651e840d2a0d59d0d3a5ba651-profile-full-recomputed.json`, matching native/actions/cleanup receipts, and `native-launch-462ccc2113084d22a18058d86e146386/`.
