# Bounded performance evidence — Spread discovery and expeditions

Status: the same-process native component measurement completed all 60 sample rows and its paired source/state checks. The existing dense glade profile separately completed 12/12 checks. These receipts establish bounded observed costs; they do not establish zero cost, a causal whole-frame regression result, universal terrain performance, or allocation neutrality.

## Native component measurement

`native-components.json` is the raw executed output; `native-summary.json` gives per-operation medians, ranges, paired deltas, and collection counts. There are five alternating enabled/disabled pairs following one excluded warm pair. Each UI sample batches 200 calls; each cold sample generates one fresh graph. No forced GC or timing threshold is used. The disabled arm runs the current runtime with just its report selections or relevant late builder removed; it is not a historical checkout.

| Component | Disabled control median | Current median | Median of paired deltas | Current late builder median |
| --- | ---: | ---: | ---: | ---: |
| Report refresh and full text | 0.0541 ms/call | 0.0793 ms/call | +0.0259 ms/call | — |
| Three historical notes: read and format | 0.00055 ms/call | 0.0177 ms/call | +0.0171 ms/call | — |
| Cargo, seed 64 6.9 | 31.67 ms/zone | 43.18 ms/zone | +10.51 ms/zone | 12.10 ms |
| Shelter, seed 64 10.7 | 30.76 ms/zone | 38.66 ms/zone | +8.48 ms/zone | 7.45 ms |
| Refused source, seed 64 11.1 | 30.18 ms/zone | 30.47 ms/zone | +1.50 ms/zone | 0.258 ms |
| Actual frozen seed 64 wayhouse selection | 29.64 ms/zone | 50.92 ms/zone | +21.14 ms/zone | 16.10 ms |

The last column measures priorities 4299→4301, which brackets the late builder only. It excludes the later manager commit/final packet and route validation; the whole `GetZone` column includes them. Whole-zone timing also includes equal diagnostic source snapshots in each arm, so it is not a production-only generation stopwatch. The pair-delta median need not equal the difference of the two arm medians.

The cargo cohort used 62 layout attempts and moved exactly one original cache; shelter used 1 and moved exactly three receipt owners; the source refusal used 0 and moved none. The wayhouse installed its actual packet. Its layout-attempt count is not exposed, recorded as −1 rather than fabricated. The paired source checks pin the before priority 4300 roll/stock/coordinate projection, real owner references, unrelated state, and the allowed movement/removal set. Composer RNG and tile-state equality are required; the wayhouse intentionally has new authored guard/loadout and replacement stock, so no unchanged-RNG/economy claim is made for it. The separate three-seed economy census remains authoritative for those deltas.

Native allocation data is **unavailable**. Every `GetAllocatedBytesForCurrentThread` field returned 0, and `native-allocation-probe.json` confirms 0 before and after an actual 1 MiB allocation. These zeros must not be interpreted as zero allocation. Collection counters did advance during one report batch, one shelter sample, one refusal sample and one baseline wayhouse sample. The resulting outliers and only five pairs preclude tight confidence or microsecond claims; use the absolute scale and raw ranges. Current whole cold ranges were 40.33–46.60 ms cargo, 37.85–71.27 ms shelter, 29.52–62.87 ms refusal, 48.12–52.23 ms wayhouse.

Report timing measures `RefreshVisibleChoices` plus `CurrentText`, and note timing measures the actual bounded three-record parse/format path. It excludes rendering, wrapping/typewriter, input dispatch and GPU. Current source rebuilds dialogue text on open/node changes and notes on journal rebuild, not every frame. Cold generation and retained-zone behavior likewise do not imply recurring frame costs.

## Existing dense glade native profile

Root ran existing `LaunchProfile`, run 284ca63df6b04a20a00d36e7396d4c53, with 12/12 checks, 0 errors and 60.2356 s / 14,355 frames. Reported frame mean 4.202 ms and p95 4.152 ms. The marker analysis is `../current-dense-profile.json`; its first two observations are excluded, and completed marker values align one row after the snapshot. Do not attribute dirty flags to the same-row marker or add overlapping/nested marker durations.

There were 300 completed redraws: actual `RenderZone` mean 39.67 ms, p95 44.49 ms, max 97.66 ms; `NativePresenter.Refresh` p95 26.95 ms and `GroundContact.Refresh` p95 11.80 ms are nested observations. A whole-frame p95 dominated by the 14,053 other rows does not show this redraw tail. LateUpdate on redraw rows max 101.71 ms. Seven managed collection-count increments occurred during the interval. Render settings stayed 1461×918 target, 1920×1080 screen, MSAA 1, vSync 0, targetFrameRate −1, Bilinear, focused, background enabled, profiling disabled and timeScale 1.

This is a current uncapped editor glade movement observation. It does not visit the new reports or wayhouse and cannot assign a cause to differences from earlier noncontemporaneous profiles. No new renderer change is proposed by this measurement.

## Execution, preservation, and limits

The private standalone .NET 8 run first completed 60 rows plus initialized/nonempty and false/empty loot-registry restoration controls. Its stable-hash adapters and Unity stubs differ from Mono, so its times and allocation counts are retained separately and are not Unity evidence. The final native DLL and successful execute script were compiled against current Unity references without using the editor. A first execute-script attempt encountered dynamic `AssemblyBuilder.Location`; root guarded lookup with `!a.IsDynamic`, leaving the measurement DLL unchanged, then ran the single successful native measurement. The first and successful scripts are archived separately; no duplicate successful native run was used.

`native-execution-state.json` proves observed scene setup/dirty state, save-root override, active game and LastGame preference restoration. The existing reused test fixtures construct/delete isolated temporary saves and fixture objects outside every timer; no user save graph is read or mutated, no Assets are written, and this is not a player journey. The outer wrapper restores the exact prior loot-registry dictionary entries and initialized state as well as fixture cleanup. Numeric native output serializes/parses exactly 60 rows before completion.

Gate disposition: the planned bounded measurement is complete. Measured report/note projection costs are sub-millisecond in this cohort, and no unbounded generation path was observed. This does not measure rendered UI latency. Current late site work has a finite measured cost, especially the wayhouse and cargo rows. Wider seeds, editor/runtime platforms, dense retained-site rendering, memory growth and an actual old-build frame comparison remain unmeasured. A future performance fix should start from a reproducible player-visible problem rather than treating these five pairs as universal budgets.
