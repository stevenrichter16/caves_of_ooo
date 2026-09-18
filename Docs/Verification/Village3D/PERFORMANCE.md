# Native village presentation measurements

Before: `a73bb40e320042c8a22b99babfa1a10b`; refined3D: `ac97b7f2d82f4e91ab34d676b6f064f3`. Both are actual1920×1080 Unity Editor play captures on AppleM5/Metal with a fixed world seed. The75-second workload is25seconds each of idle, native walking and native door commands; whole cycles complete, so sample counts vary. The new-game location changed approach history, so this is descriptive evidence, not an isolated renderer benchmark.

| Phase | Main-thread p95 before→after (ms) | Main-thread p99 before→after (ms) | After max (ms) | After GPU p95 (ms) |
| --- | --- | --- | --- | --- |
| idle | 3.67 → 4.57 | 4.14 → 5.76 | 23.51 | 3.84 |
| walk | 4.12 → 5.12 | 21.48 → 24.24 | 712.08 | 3.70 |
| door | 5.05 → 4.31 | 8.88 → 6.85 | 31.21 | 1.85 |

Normal-frame costs are below16.7ms at p95. Walking p99 and occasional long Editor/runtime spikes exceed that budget, including a712ms maximum. Stable60fps is **not established**. The native route includes ordinary AI, rendering, UI, autosave and the observer; these counters do not isolate the source of a spike. No causal improvement or regression claim follows from this pair.

Observed3D draw count is roughly222, above the provisional150 target, including UI and shadow passes. Roughly653k rendered triangles include multipass work; source mesh estimates are363k whole-zone/329k crop, counting mutually hidden roofs and interiors. These measures are not interchangeable. The larger foliage and cloth revision is a documented fidelity-budget exception. Lower detail disables cosmetic clusters and owned shadows and reduces both target dimensions to75%; functional native checks verified that toggle without altering game time.

The actual35-case refined audit passes with0 unexpected errors and automatic cleanup. GPU12-group acceptance separately verifies visibility, remembered surfaces, shadows, compositor orientation and exposure. Source hashes during the capture changed only the generated UnityMCP log; no application source or assets changed. Raw CSV, screenshots, logs and receipts remain alongside this report.
