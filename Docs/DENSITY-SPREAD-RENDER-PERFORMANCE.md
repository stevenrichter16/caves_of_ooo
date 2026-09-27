# Spread renderer refresh performance

Status: lookup guards pass32/32 native cases; exact-byte contact optimization passes74/74 native cases. Matched movement profiles are complete; residual editor spikes are documented below. This is a bounded follow-up to the whole-biome art rollout, not a change to world generation, native entity rules, visibility or authored models.

## Verified observations and corrections

| Premise | Verified result | Consequence |
|---|---|---|
| The first slow biome route might reflect a changed editor/background state. | The controlled original glade route `3919fd89597f4cad81fb8fefbeb88759` also completed 12/12 with zero errors and exact restoration but averaged 34.7 ms/frame, p95 213.67 ms. Focus was true throughout the sampled interval. | Treat this as a real current renderer investigation; do not attribute the earlier route difference to a specific cause yet. |
| A new 868-model library might validate all entries each frame. | `SpreadNativeStyle3DLibrary.EnsureReady` validates only when its index is absent. Current ground build count stayed 40 throughout the controlled loop. | Do not optimize a nonexistent per-frame full import/validation or hide rebuilds. |
| Most frames are slow. | After excluding the first two recorder observations, 1,588 clean frames averaged 3.712 ms renderer time, p95 4.914; 149 dirty frames averaged 189.948 ms, p95 223.967. | Investigate changed-frame work while retaining current unchanging-frame behavior. |
| Catalog lookup is trivial. | The actual warmed native fixture measured about 18.024 ms for the glade's model lookups versus .261 ms for the identical frozen model-reference map. Neighbor lookup measured 13.823 versus .259 ms. | Repeated owning-library resolution is a measurable contributor, not proof of the entire 190 ms dirty cost. |
| Presenter refresh explains the whole dirty-frame cost. | Native fixture empty/full-fingerprint refresh averaged 23.285/47.19 ms in the glade and 27.426/47.333 ms next door. | Collect existing RenderZone, FOV/light, environment sprite and UI markers before claiming the remaining cost is solved. |
| A permanent model cache is necessary. | Each of the first eight extension libraries already rejects other namespaces before inspecting its own index, but callers perform `Resources.Load` first. | Prefer equivalent pure ID guards around resource loads. Preserve base-index priority, order, current owning-library validation and misses; do not add a global value cache. |

Raw live evidence: `Docs/Verification/DensityCompletion/ReferenceGlade/Native/3919fd89597f4cad81fb8fefbeb88759/profile-markers.csv.gz`, `profile-markers.json`, `marker-summary.json`. Warmed native fixture receipts are under `SpreadBiome/Performance/NativeRefresh`. These are editor measurements with borrowed runtime assets. Recorder values may refer to the most recently completed frame; snapshot frame IDs are separately recorded. Exclude the first two observations when interpreting setup-free samples. Nested markers are not additive.

## Test-first bounded lookup unit

1. Add a per-call resource-loader overload to the existing catalog and prefab lookup methods, leaving the current chain unchanged. Public runtime methods call that same body with a cached delegate to Unity's real loader. No mutable global override, persistent cache, asset mutation or loader replacement in normal play.
2. Run `SpreadNativeModelLookupTests`: real exact extension IDs must request only their owning resource and return the exact current source object. Current later-chain families should fail the request-count invariant. Pair those with native-base/no-load, null/empty/no-load, unknown/case-sensitive miss, repeated changed loader result, owning-library exception and base-index collision-priority controls.
3. Only after actual count RED, gate the existing loads using their own already-existing pure namespace/known-ID contracts. Apply the same change to model metadata and prefab lookup. Retain original fallbacks and order.
4. Rerun the exact regression, warmed native fixture, style/ownership regressions, and controlled native movement profile. Do not claim the 190 ms issue solved merely because one lookup is faster.

The prepared fixture has 32 cases. Its initial prediction was 16 request-count failures and 16 controls. Reading the full native assertion stacks later corrected that classification to 14 request-count failures, two test-setup null references, and 16 passing controls. Initial runtime/test reference compiles passed; this did not establish the reflection premise.

## Self-review and limits

- 🟡 Observed slowness alone is not a failing TDD assertion. The explicit resource-request budget must fail with the unchanged dispatch before the optimization.
- 🟡 The 190 ms versus 47 ms gap remains unassigned. Additional existing profiler markers are prepared; no legacy render pass is being removed on speculation.
- 🧪 The first proposed optimization is expected to remove redundant asset lookup, not geometry or effects. Exact model/source identity and all authority/refusal behavior remain acceptance gates.
- 🧪 Editor frame measurements are not a player-build throughput guarantee. No global graphics, profiler, focus, timer, input or background setting is altered by the measurement.

## Files and ownership

Private package: `/tmp/coo-c15-rendering/performance/lookup-red/manifest.json`. Parent owns shared publication and all Unity runs. The proposed source paths are `SpawnRing3DCatalog.cs`, `SpawnRing3DLibrary.cs`, the new native test fixture/meta, and a marker-name-only delta to `ReferenceGladeNativePlayer.ProfileMarkers.cs`. The model art, source libraries and real simulation graph are unchanged by this unit.

## Actual lookup RED and private repair

The observer-only implementation ran natively in `Integration/native-spread215-effects-and-lookup`. The first summary incorrectly called all 16 failures resource-count assertions. Inspection of the full XML stacks after the post-gate run corrected this to **14 failed resource-count assertions, two fixture null references, and 16 passing controls**. Poured-liquid entries store `ColorCode`, not an `Id` field; the test now obtains each library’s actual validated identity through `Spec.id` and separately checks that the poured identity matches `ModelId(ColorCode)`. The original receipts are preserved. The 14 genuine failures requested preceding unrelated libraries before their own exact resource; they are executed pre-optimization RED.

The private repair applies the same pure ID guards already present inside the eight earlier library `Find` methods, before loading those resources. It preserves the existing sequence, base-index priority, case-sensitive semantics, per-call current loader result/exception, current owning-library validation and every later regional fallback. The model-metadata and prefab paths use identical guards. No result cache, model rebuild, shared asset change or native recipe memoization is introduced. The first post-gate native run `Integration/native-spread259-effects-green-pools-marks-red` passed all 14 formerly failing count cases and 16 controls; the same two fixture errors remained. The corrected 32-case rerun, source/visibility regressions and matched movement measurements remain pending.

## Expanded baseline and next diagnostic scopes

The unchanged pre-gate route `1eff33093df740a39bc8495b4e8da91c` completed 12/12 with zero errors and exact restoration, averaging 34.898 ms/frame, p95 215.472 ms. Its recorder values complete one row later than the current renderer snapshot: matching snapshot row i to recorder row i+1 gives only .00691 ms mean absolute difference, while same-row matching differs by about 32.5 ms. Consequently this analysis groups redraws by the completed `RenderZone > 0` marker, not the same row’s current `rendererDirty`.

Across 150 completed redraw samples: LateUpdate 190.403 ms (interpolated p95 229.954), RenderZone 180.454 ms (interpolated p95 218.383), FOV 17.865 ms (interpolated p95 22.198), light computation 4.261 ms (interpolated p95 5.699), environment post-render 11.241 ms (interpolated p95 14.356), sidebar 7.233 ms (interpolated p95 9.075), hotbar 2.423 ms (interpolated p95 3.092). These markers nest and must not be summed as exclusive time. `RenderCell` is zero because detailed cell profiling is disabled, not because cell work costs zero. Raw-derived groups, units, alignment checks, source hash and settings are retained in `SpreadBiome/Performance/expanded-baseline-analysis.json`; `analyze_markers.py` reproduces them.

Next diagnostic-only scopes measure native presenter refresh, the aggregate full cell loop, and contact Refresh/Rasterize/Upload without changing their statements, order, guards or settings. The contact signature includes current visible owners; moving FOV may rerasterize and upload its 640×200 field even while ground patch build count is unchanged. This is a hypothesis to measure, not a reason to remove contacts or widen remembered-cell shading. The private five-file marker delta and one fixture-premise correction compile against the current native/runtime references; native execution remains parent-owned.

Pure contact complexity review: the existing raster is bounded by 32,768 footprints and 1,000,000 tested pixels and validates every placement/work budget before publishing any contribution. If its new native marker establishes a material cost, skipping arithmetic for an already-255 output pixel and hoisting row-invariant distance/index work are possible exact-output optimizations; neither has been implemented. Reusing precomputed local stamps is not automatically exact because adding world coordinates can change float-to-byte rounding. Any accepted candidate must preserve every output byte, complete malformed/budget refusal, all yaw/edge controls and current visibility behavior.

## Post-lookup native observation and contact test-first candidate

The post-gate route `a9af7a901cfb42678a896c09ec0f782b` completed 12/12, zero errors, over 60.206 seconds/7,479 sampled frames: 8.057 ms mean and 8.304 ms p95. Its completed-redraw group contains 251 samples, compared with 150 in the earlier route. Screen/target dimensions, MSAA, filtering, focus, background execution, vSync and time scale remain equal. This is an observed editor improvement, not an attribution of the entire difference to lookup guards: unchanged FOV/UI stages also ran faster and the finite time interval admitted more moves. Exact request-count tests establish the removed lookup work independently.

The new scopes assign actual redraw cost: native presenter 41.924 ms mean/p95 54.781; contact refresh 18.883/24.111, within it raster 16.753/21.355 and upload .619/.844; full cell loop 7.338/9.739. The reproducible complete analysis is `Performance/post-lookup-gate-analysis.json`, retaining the one-row completed-recorder alignment and nested-marker bounds.

A private contact unit now has executed test-first evidence. The original helper first failed 14 missing-observer cases (38 controls passed). An observation-only overload then ran the unchanged raster and produced **three genuine redundant-work RED cases and 49 passing controls**. Those failures repeated exactly opaque contact at counts 2/32/128; all original-budget and byte-output controls passed. The minimum repair skips computations at an already-255 target, then hoists row-invariant pz/dz and row-index arithmetic. Public calls and the internal per-call count share the same body; no global state/cache is added. Complete batch admission remains ahead of all raster writes.

The private repaired helper passes **52/52 pure cases**, including the prior 25 geometry controls and 27 new cases. A SHA-pinned frozen original implementation is the output oracle, comparing every field byte over translated/edge/extreme positions, every yaw, six seeded large footprint batches, overlap order, soft-only fields, incoming opaque targets and malformed/budget refusals. Two additional native tests compare actual current generated presenter placements and submitted mask pixels in glade/neighbor zones across full reveal and two visibility halves. Those tests have compiled but have not yet run. The actual performance benefit is still pending a native route; work-count reduction alone is insufficient to claim it.

Independent bounded source review found no semantic blocker. Raw pure RED/GREEN XML/logs, source oracle, preimplementation tests and compile receipts are preserved under `Performance/ContactRaster`. Parent controls exact three-path publication and native verification.

Native contact validation subsequently completed **74/74 passing** in `Integration/native-spread411-budget-red-contact-green`: the 27 new pure cases, both actual generated-placement/current-mask controls and all 45 prior contact tests. The combined run’s four failures were independent transient-budget RED cases, not contact failures. The exact-byte native field gate is closed; a matched movement profile remains the performance gate.

The same actual 411-case XML also closes the corrected lookup fixture: **32/32 native PASS**, including both real poured-liquid metadata/prefab identities. This result is distinct from the earlier30-pass/two-fixture-error run.

## Post-contact native profile and reporting limits

The post-contact route b5fe9ac349d145b79870ddc96b82f17c completed12/12 with zero errors and exact restoration:60.257 seconds,14,180 frames, mean4.252 ms/p954.014 ms. Latest-completed profiler values again align one row after the renderer snapshot (mean absolute difference .00121 ms). Across311 completed redraws, raster averages7.628 ms/p959.030 (previous16.753/21.355), contact refresh8.791/10.074, native presenter21.795/23.694, RenderZone36.101/39.953 and renderer LateUpdate40.227/43.007.

These are observed editor measurements, not proof that the source change caused the full timing reduction. Unmodified FOV, cell-loop and upload stages also cost roughly55–58% of their previous measurements; the two finite intervals contain differing redraw counts/world turns. Exact work-count and full-field native tests establish the eliminated redundant calculations and preserved image independently. Render/focus/dimensions/filter/MSAA/time settings are unchanged.

**Residual spike:** the largest post-contact renderer sample is506.677 ms (observer UnityFrame19830), alongside global GC.Collect186.488 ms and GC Allocated In Frame2,468,209,064 bytes; sidebar/hotbar markers in that completed sample are92.355/264.334 ms. These globally collected editor values do not identify an allocation source and are not attributed to the contact helper. Overall frame p95 excludes most redraws because they occupy fewer than5% of sampled frames; report redraw p95 and maxima alongside it. This work does not claim all stalls eliminated.

Raw-derived final analysis and before/after comparison are Performance/post-contact-analysis.json and Performance/lookup-contact-profile-comparison.json. Exact profile evidence paths/hashes, including all four run folders and restoration/raw-log records, are in render-completion-profile-evidence-files.json. No further production change is proposed from the isolated spike; larger coverage and unrelated effect-budget validation continue under the parent.
