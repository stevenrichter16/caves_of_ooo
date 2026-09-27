# Current-source movement profile and scope boundary

Actual `ReferenceGladeNativeBatch.LaunchProfile()` run
`4df42af629784075b9203381f782ee2b` passes12/12 functional checks with zero
unexpected errors. Its timed interval is60.240 seconds,12,040 frames, in the
seed64 dense glade. All eight retained frames were inspected: visible voxel
world/player/props, real chest pickup, solid wall, depleted seam, return and final
normal input. The two full-reveal frames are labeled composition views.
The exact original editor/save/input state is restored (`restoration.json`).

The raw report, recorder metadata and compressed frame rows are under `Native/`.
`comparison.json` compares the same recorded screen/world-target/render settings
with the preceding post-contact run `b5fe9ac349d145b79870ddc96b82f17c`.

| Measurement | Earlier sample | Current sample |
|---|---:|---:|
| Frame mean |4.252ms|5.112ms|
| Frame p95 |4.014ms|7.199ms|
| Completed redraw renderer p95 |43.007ms|53.865ms|
| Completed redraw presenter median |20.982ms|24.695ms|

This is a measured slowdown, not evidence of improved performance or a causal
attribution to this feature. These are two noncontemporaneous Editor samples,
with different session/GC/system histories; the latest follows a full23-minute
suite. The completed redraws remain the costlier path. No source, quality,
profiler, focus or timing setting was changed to hide the result. GC/allocation
markers include Editor-wide activity, and nested recorder values are not additive.
The complete comparator retains allocations and both settings snapshots.

## Importance and next useful check

Record this as a medium-priority performance follow-up. The roughly54ms redraw p95 can produce occasional visible stalls. No functional failure, lost input, broken
save or frame-budget guarantee is demonstrated by the samples. The absolute
redraw cost warrants a matched current-versus-baseline capture under comparable
session conditions before choosing a rendering change; an unsupported broad
optimization would delay this content slice without isolating the cause.

The plan's combined movement/dense-view/repeated-reader profiling scope is
narrowed explicitly: this existing60-second driver measures dense movement;
`M2/Native/77cf480c9c004615a8b078373f3cf7ea` separately verifies actual repeated
reader/F1/Compare navigation in11 inspected frames. Repeated-reader performance
is **unmeasured**. No new long-running UI harness or general performance claim
ships. Core/native functional readers and all20,406 integration tests remain green.

Script-observable: native input/ownership/checkpoint checks, exact available
recorder values/units, fixed interval, state restoration. Visually inspected:
these eight frames, not every transition or player-build smoothness. No natural
encounter discovery, all-seed balancing, isolated game allocation or standalone
build performance is claimed.

Independent source/measurement reread found no new repeated scan, resource load or
geometry rebuild for ordinary glade owners. Ground build count remains40;304
versus311 completed redraws were sampled. Unchanged hotbar work also slowed. This
review supports preserving the follow-up without attributing it to the new hooks.
