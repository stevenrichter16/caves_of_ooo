# S3D28b retained Rime profile failure

The run remains **failed** after 10,515 partial-profile rows. Raw CSV SHA-256 matches the report. The seven showcases and prior successful/denied Reduced Rime cases all passed. The final Reduced Rime command hit its actual recipient, dealt 4 damage, applied Frozen, retained a stable copied result and selected the correct native entry; its observed mesh peak is zero.

Frame **13608** records **480.188 ms unscaled delta**, **478.663 ms Main Thread**, and **66,465,973 bytes GC Allocated In Frame**. Input is 0.024 ms; ZoneRenderer is 1.128 ms. These numbers do not establish that garbage collection caused the stall. Adjacent wall timestamps locate the long interval one sample earlier, consistent with completed-frame profiler reporting.

The failed command starts between raw frames 13578–13579 by its recorded duration/end, consistent with acceptance in frame 13579. Summing subsequent unscaled deltas moves its age **0.123307→0.603495 seconds** across the stall. Rime's first Reduced positive-scale sample is at 0.18 s and its clear at 0.60 s. The exact acceptance frame is inferred because the driver does not record it explicitly. **Every raw row throughout the failed interval has zero active native meshes.** This is stronger than a screenshot loop merely missing visible poses.

MCP resource-discovery reflection is logged earlier in this run, before later successful casts, so no timing evidence identifies it as the cause of this final stall. Explicitly timing and warming cached Editor resource/tool discovery before the workload is a bounded diagnostic experiment, outside steady profiling. Retain the failure and require all existing gates afterward; do not declare warming a proven general fix, weaken acceptance, or retry repeatedly without new evidence.

A visual delta cap is not recommended for this diagnostic step: it changes the tested wall-time contract and would require matching the independently evaluated actor Animator clock. Runtime, gameplay and art remain unchanged. Isolated save, scenes, Game view, preferences and private-root cleanup succeeded, with no unexpected errors during the observed run.
