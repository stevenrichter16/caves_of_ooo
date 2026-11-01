# Manual Editor performance observations

These are bounded mixed walking/menu/idle intervals, not packaged frame-rate or GPU benchmarks.
The existing diagnostic menu was extended with bounded native recorder buffers and raw-unit percentiles; it issues no input or gameplay writes.
Recorder setup precedes the stopwatch slightly; stopping is sequential. Main Thread includes Editor scheduling.
Independent source review found no actionable P1/P2. Native compilation and Start/Stop were exercised.

| Run | Zone | Seconds | Screen | Renderer p50/p95/p99/max ms | Main Thread p95/max ms | GC p95/max bytes | Status |
|---|---|---:|---|---|---|---|---|
| [manual-20261101-205332-959.json](manual-20261101-205332-959.json) | none | 16.52 | 2321×1285 | 0.010/0.031/0.043/0.087 | 2.244/27.071 | 15919/9526894 | Invalid boot/reload observation |
| [manual-20261101-205558-103.json](manual-20261101-205558-103.json) | Overworld.3.5.0 | 67.27 | 2321×1285 | 0.105/0.212/0.317/223.780 | 5.842/2098.829 | 492133/275728726 | Contaminated by in-interval CodeDOM inspection |
| [manual-20261101-205808-799.json](manual-20261101-205808-799.json) | Overworld.3.5.0 | 71.09 | 2321×1285 | 0.092/0.189/0.251/65.184 | 5.633/169.114 | 18271/9530058 | Clean of in-interval MCP code inspection; comparison pending |
| [manual-20261101-210043-381.json](manual-20261101-210043-381.json) | Overworld.10.15.0 | 81.43 | 1280×800 | 0.095/0.200/0.250/95.670 | 5.396/152.649 | 18341/9529064 | Clean of in-interval MCP code inspection; comparison pending |

Clean Felling run: Apple M5, Unity6000.3.4f1 OSXEditor;25,679 samples,35–40 draw calls, no wrapped buffers. Continue from earned turn420; ordinary north/south movement and underfoot Examine, with idle observation between actions.
Clean field run: same machine/editor;31,364 samples,draw-call p9544/max45, no wrapped buffers. Start38,18HP40; ordinary movement/action-menu use ended38,24HP10. Actual encounter damage occurred; this interval is not a safe-route or encounter-balance acceptance and did not replace the preserved harvest checkpoint.

The clean runs still contain65.18ms and95.67ms renderer maxima. No matched pre-change profile exists yet, so attribution and regression remain unresolved. Different measured resolutions prevent comparing these two areas directly. R11 remains open pending matched baseline, spike investigation and current packaged performance. No claim of allocation-free overall gameplay: the GC marker is Editor-wide.

Reproduction: launch the isolated Live16x24 greybox, copy the documented earned save pair into its marker directory, Continue and verify the actual InputHandler zone before profiling. Start Manual Gameplay Profile; use ordinary keys for60–90s without execute_code or captures inside the interval; Stop Manual Gameplay Profile. Record current Screen resolution and exact inputs. Finish launcher to restore normal preferences/root.
