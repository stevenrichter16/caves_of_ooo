# Contact raster experiment — rejected after measurement

Status: rejected and fully rolled back on 2026-10-10. The disjoint-band candidate produced exact pixels and passed 70 new tests, but failed the matched runtime timing gate. `ReferenceGladeContactGeometry.cs` is restored to the first-slate `bf3c2468b` implementation. The two candidate-only band test files and their metadata were removed; existing contact geometry, opaque-skip, frozen byte-oracle and native visibility tests remain. No shader, mesh, contributor selection, tint or visibility change survives this experiment.

Root owns all Unity execution. The final regression run and movement profile after rollback are pending. These Mac measurements do not establish Steam Deck hardware performance.

## Original measurement, plan and sweep

The matched full-detail 1080p first-slate route `ebe72af5349d429088786810a9522db9` passed 18 route checks without errors. Across 306 active refreshes, native refresh median was 12.27 ms and contact raster median was 8.54 ms (p95 9.65 ms, maximum 14.55 ms). The target was the raster calculation itself: its marker wraps `ReferenceGladeContactGeometry.Rasterize`, excluding contributor gathering and texture upload.

The plan, recorded before production, was to reuse identical rectangular distance/falloff calculations without changing any submitted byte. Source inspection established:

- The target is cleared before refusal; every placement and the full original bounding-box work budget are validated before rasterization.
- First-slate code already skips destinations whose byte is 255 and hoists row coordinates. Its evaluated-sample counter counts nonopaque candidates, not mathematical distance evaluations.
- Inside a rectangle, distance is zero; along an axis-aligned edge, one axis is zero and some falloff values repeat. Quarter-turn rotation, clipping and float operation order must remain exact.
- Existing rasterization uses no temporary buffers. Pooling or contributor caches would not address the measured raster work and were outside scope.

The candidate partitioned each rectangle into outside-x columns and inside-x rows. Each band lazily reused its exact zero-axis falloff; fully interior samples used byte 255. Corner calculations retained the original square-root/smoothstep/round expression. A new observation overload counted actual distance calculations while preserving the established candidate count. No retained cache or array was added.

## Correctness evidence for the withdrawn candidate

Root ran RED job `49045281cd2b4ed1875fd33471738d3b` before production: 38 cases completed and the capped first 25 failures all identified the missing observation seam. Root then released the implementation.

The final candidate test set had 39 pure cases, two current native mesh-placement cases and 29 adversarial cases. It covered exact complete fields, independent sample counts, all yaws, clipping, subpixel/adjacent-float boundaries, thin and soft-only shapes, overlap order, malformed input, limits, source immutability, independent targets, cleared refusals, visibility masks and submitted texture bytes. Large byte comparisons used complete loops rather than NUnit per-byte boxing; no samples were omitted.

All 70 passed in native job `0154611e0cd24d4bbcea0e5cfd9be032`, recorded in `Docs/Verification/SteamDeckPerformance/2026-10-10/slate2-contact-save-green.json`. The pre-rollback combined job `59fe6ab7ac134b4795dc6d1ec31cb9b9` also completed 1,903 cases: 1,902 passed, zero failed, one unrelated save allocation-counter check skipped. Its receipt is `Docs/Verification/SteamDeckPerformance/2026-10-10/final-combined-native.json`. Those historical gates prove candidate correctness, not runtime benefit, and are not claims about the reduced suite after rollback.

## Failed runtime gate

Matched route `3795617a6472467b8b49470d9110ef54`, using the same full-detail configuration, measured:

| Active median | First-slate baseline | Candidate slate |
| --- | ---: | ---: |
| Contact raster | 8.542 ms | 10.016 ms |
| Native refresh | 12.268 ms | 13.320 ms |

The separate native-paint candidate also regressed and was rejected independently; see `STEAM-DECK-PERF-NATIVE-PAINT.md`. Its much larger painting cost does not change the fact that the contact marker itself was slower.

Code review explained the risk: outside-x columns replace contiguous row traversal with strided writes and recompute vertical distance/row addressing per candidate. Tiny soft-only shapes save no square roots, so added branching/bookkeeping can only add work. Reduced arithmetic counts do not prove lower CPU time.

## Paired local diagnostic

To check that explanation without conflating scene changes, a Release .NET 10.0.5 ARM64 diagnostic compared the exact first-slate body, the disjoint bands and a local row-only reuse alternative. All three used the identical borrowed placement list and separate reused targets. Full 128,000-byte equality was checked before timing. The run used 12 warmups per algorithm, then 21 batches of 24 calls with balanced rotating order. Inputs use seed 177 and 4,000 positions; repeated overlap uses 16,000 placements.

| Synthetic input | Baseline | Bands | Row-only experiment |
| --- | ---: | ---: | ---: |
| Solid rectangles | 1.156 ms | 1.190 ms | 1.106 ms |
| Tiny soft-only rectangles | 0.294 ms | 0.391 ms | 0.299 ms |
| Thin rectangles | 0.486 ms | 0.596 ms | 0.488 ms |
| Mixed rectangles | 0.787 ms | 0.925 ms | 0.794 ms |
| Repeated mixed overlap | 2.889 ms | 3.349 ms | 2.917 ms |

Bands were slower in every set. Row-only reuse showed a small gain only for solid rectangles and no broad improvement; an earlier exploratory pass was slower throughout. Neither result justifies additional production branches. The row-only alternative never landed in Assets.

Reproducible diagnostic and machine-readable receipt:

- `Docs/Verification/SteamDeckPerformance/2026-10-10/contact-paired-offline.cs`
- `Docs/Verification/SteamDeckPerformance/2026-10-10/contact-paired-offline.json`

The receipt records runtime, warmup/order, inputs and limitations. Synthetic .NET timing does not establish Unity Mono/IL2CPP or Steam Deck parity, and no claim depends on that parity: the matched native gate already failed.

## Final decision and retained behavior

Restore the existing first-slate row traversal. Retain opaque-destination skipping, complete-field clearing, atomic validation, unchanged work admission and the disabled-contact early-out. Remove the withdrawn candidate's distance-counter API and its two dedicated band fixtures instead of weakening their expectations to fit a reverted implementation. Existing first-slate byte-oracle and native contact coverage remain intact.

All four rendering sources changed by the withdrawn contact/paint slices now match `bf3c2468b`; the accepted graphics settings integration is already in that baseline. No contact speedup is claimed. Final post-rollback regression and matched movement profile remain pending.


Final acceptance closure: after candidate withdrawal, native job
`1128918628c74d2f9d3c41a5864c8e4c` completed 240 passed / zero failed /
one unsupported allocation-counter skip. Final movement run
`ed533fbddfd04331a0adb5fcab235bd2` passed all 18 route cases; full-zone active
median 41.800 ms, tile cells 5.493 ms, environment sprites 9.627 ms. Captures
were visually inspected. Full evidence and limitations are in
`STEAM-DECK-OPTIMIZATION-SLATE2.md`; no physical Deck performance claim.
