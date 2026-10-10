# Steam Deck optimization — second measured slate

Status: accepted implementation, reviews, native regression and movement profile complete. Both slower rendering candidates removed. Linux package delivery follows. First-slate base: `bf3c2468b`.
CoO-original performance work; no Qud implementation or content changes.

## Evidence and priorities

The first slate removes the dominating idle HUD work, but the follow-up native
profile `ebe72af5349d429088786810a9522db9` still shows expensive movement frames.
At unchanged full-detail settings, active medians are 39.661 ms for full redraw,
9.700 ms for environment sprites, 8.542 ms for contact raster, and 5.467 ms for
tile cells. These markers overlap. Do not add them or infer Deck FPS.

The independently validated save probe captures twenty generated zones / 45,596
placed owners / 35.5 MB in 433–509 ms before background compression. Reducing
that latency remains important as exploration accumulates saved chunks.

## Implementation sequence

1. **Hidden tilemap painting.** Follow `STEAM-DECK-PERF-NATIVE-PAINT.md`.
   Separate render-event dispatch from tilemap writes. Skip only outputs proven
   covered by current native representation. Preserve unsupported items/actors,
   remembered terrain, hazard marks, surface failure, toggles and direct cell
   refresh. RED work-count tests precede edits; test mutable Render callbacks.
2. **Contact raster calculations.** Record the verified algorithm and byte oracle
   in the contact follow-up doc. Current opaque-pixel skipping already exists.
   Partition rectangular footprints into equal-distance bands so repeated pixels
   share distance/coverage calculations, while corners retain the original math.
   Require exact output equivalence, bounded work counters and adversarial shapes.
3. **Save serialization.** Follow `STEAM-DECK-PERF-SAVE-SLATE2.md`. Cache safe
   immutable serialization descriptors and repeated format encoding; retain exact
   field ordering and the existing binary format. Prefer measured AOT-safe wins
   before runtime code generation or cross-frame capture. Re-run the same world
   probe and roundtrip validation. Do not claim async IO solves capture latency.
4. **Review and verify.** Independent code review, counterchecks, dedicated
   adversarial cases, combined native regression sweep and the same isolated
   keyboard route. Compare active refresh costs as well as all-frame averages;
   record differing action counts and worst spikes. Inspect rendered captures.
5. **Deliver.** Update living docs in the implementation commit; fetch/rebase and
   push main. Build a non-development Linux/Vulkan player, package launcher,
   controller/graphics instructions, provenance and checksums into a new archive.

## Sweep corrections and safety boundaries

| Premise | Verified correction |
| --- | --- |
| Native surface ownership means any top object is covered | ClaimsCell covers the entire grid. Require exact current entity representation and membership. |
| Render events only describe appearance | Some decrement damage flash, alter flicker intensity and advance presentation counters. Preserve their cadence. |
| Cached IsRenderedEntity is fresh after Render | Mutable glyph/takeability/membership can change during the callback; handle stale claims explicitly. |
| Contact raster needs first opaque skip | Already implemented. Optimize repeated distance math, preserving that skip and frozen pixel oracle. |
| Reflection field caching finished save optimization | GetValue, runtime value dispatch and repeated names remain per entity. Measure each replacement. |
| Freezing input makes sliced live serialization safe | Other presentation callbacks mutate graph state; no cross-frame capture without a complete consistency design. |

## Deferred by measured priority

AI/pathfinding is not a leading cost in these captures. Shader/material-family
rewrites, global batching and incremental save format redesign need separate
evidence. Keep current 3D artwork, content, turn timing and save compatibility.

## Verification and self-review

Executed RED precedes all three slices. The broad native Unity job before candidate withdrawal
`59fe6ab7ac134b4795dc6d1ec31cb9b9` completed 1,903 cases: **1,902 passed,
zero failed, one skipped** (this Unity runtime does not expose a working per-thread
allocation counter). All 45 native-paint/owner/adversarial cases and 70 contact
cases were included; those candidate-only fixtures were later withdrawn with the
slower implementation and are not final-source test counts. The pre-withdrawal legacy-render job
`7644ab2381f14e12a0e98cc0c11ab443` passed **94/94**, covering stale backgrounds,
environment sprites, blueprint coverage, renderer harnesses and animated surfaces.
Raw summary receipts are in `Verification/SteamDeckPerformance/2026-10-10`.
Physical Steam Deck timing/thermal/controller-hardware certification remains
unavailable on this Mac.

### Evidence and review closure

- Contact raster preserves byte-for-byte output against an independent copy of
  the old algorithm while reducing repeated distance calculations. Positive and
  countercases cover opaque skip, overlap, malformed bounds and work admission.
- Native-paint RED job `0abcec556d634a0c8e758dca4aae2d76` had five intended
  failures. Later adversarial job `358f96c4fb414219a6b9b7186c3a856b` confirmed
  five more failures involving cross-cell render mutation, direct cell refresh
  and unsubmitted bodies. Current ownership is revalidated after callbacks;
  authoritative rejection bypasses stale authored and animated claims.
- A Village owner test originally selected an attached-equipment renderer instead
  of the native body. The fixture now targets the captured body renderer with
  the same expected rejection. All ten owner checks pass in the final sweep.
- Save descriptor tests preserve field order, hooks, reference tokens, live field
  reads and exact version-7 bytes. An adversarial test found a caller predicate
  invoked under the metadata lock; it now runs outside the lock. Native and
  independent source reviews found no remaining actionable correctness issues.
- Corrected standalone paired capture job `78c7ef2ffa804de1b810f37a351e0303`
  passes byte identity and roundtrip on one and twenty actual generated zones.
  Paired medians: 30.62 to 14.29 ms (one zone); 2279.27 to 699.59 ms
  (twenty zones / 45,596 placed owners / about 35.5 MB). These are noisy local
  paired measurements, not Deck timings or a comparison against unrelated runs.
  Capture remains synchronous and can still cause a hitch in a large world.
- Expanded native graphics audit `dafb9c430adf4cf5a43414be7266d18c` passes
  **12/12**, including actual synthetic gamepad menu input, unchanged HUD
  resolution, settings changes and exact preference/device cleanup. Captures
  were visually inspected. Physical controller hardware is not verified.

### Review boundaries

This is CoO-original performance work, with no parity claims. Source review checks
both lifecycle symmetry (queue/drain, claim/release, fixture/cleanup) and consistency
with existing game contracts. Dedicated mutation tests cover callback changes,
unknown fields, stale renderer ownership, overlapping contact footprints and
invalid inputs. The final profile is the acceptance gate for the extra ownership
checks: fewer tilemap writes alone would not prove a faster redraw.


## Performance gate: rejected candidate

Run `3795617a6472467b8b49470d9110ef54` passed all 18 native route cases with
no errors and visibly retained the 3D world. At the same full-detail settings,
active median full redraw nevertheless rose from **39.661 to 78.095 ms**;
cell painting from 5.467 to 27.811 ms and environment post-render from 9.700 to
26.146 ms. The fresh owner proof performs the complete recipe resolution chain
for every represented visible cell before and after callbacks. Its cost outweighs
the removed tilemap writes. The faster all-frame mean (3.628 versus 3.882 ms)
is misleading here because the 60-second loop completed only 245 refreshes versus
306. The acceptance gate uses active work, not that average.

**Decision:** remove the native-paint candidate and its new APIs/tests before
release, retaining the prior rendering behavior and all first-slate improvements.
Keep this negative experiment and its RED/GREEN receipts as design evidence.
No persistent invalidation cache is being added to rescue the candidate: arbitrary
mutable render callbacks make that a larger correctness project. Contact raster
also rose from 8.542 to 10.016 ms; it needs a paired algorithm measurement before
retention. Save serialization and the graphics menu verification are independent.


The paired Release .NET experiment also rejects both the contact bands and a
simpler row-reuse variation. Shared immutable inputs, alternating execution order
and full byte comparisons confirmed that the added bookkeeping costs more than
the avoided square roots across solid, tiny, thin, mixed and overlapping shapes.
The existing raster body is restored, preserving its already-shipped opaque skip
and disabled-contact early-out. No band-specific APIs/tests remain in the shipped
source. The second slate therefore ships **the measured save serialization win**;
the two rendering proposals remain documented negative experiments. This is an
explicit scope divergence based on the performance acceptance gate.


### Final-source regression after rendering candidate withdrawal

Native Unity job `1128918628c74d2f9d3c41a5864c8e4c` completed 241 cases:
**240 passed, zero failed, one skipped** (unsupported per-thread allocation
counter). This includes current save descriptor/adversarial tests, native render
work/adversarial tests, contact geometry/native/raster checks, fallback invalidation,
water rendering and the legacy environment/stale-background suites. Receipt:
`Verification/SteamDeckPerformance/2026-10-10/final-post-withdrawal-native.json`.
Rendering sources now match first-slate commit `bf3c2468b` exactly. The accepted
save serializer is unchanged from its byte-identity and paired timing verification.
Counts overlap earlier suites and must not be added together.


## Final accepted-source movement profile

Run `ed533fbddfd04331a0adb5fcab235bd2`: **18/18 route checks**, zero failures
or unexpected errors; arrival and final captures visually inspected. Full-detail
1920×1080 screen / 1461×918 world target, MSAA 1, v-sync off, no frame cap,
focused, run-in-background enabled, time scale 1 and profiler disabled match the
earlier captures. Ground build count stays 40 to 40. The same 60-second input
algorithm produced 304 active redraws, versus 306 after slate one and 245 with
the rejected candidates. This is not an identical action/position replay.

| Active marker | First accepted slate median ms | Rejected candidate median ms | Final median ms | Final p95 / max ms |
| --- | ---: | ---: | ---: | ---: |
| Native refresh | 12.268 | 13.320 | 13.668 | 16.256 / 84.845 |
| Contact raster | 8.542 | 10.016 | 9.823 | 11.927 / 15.785 |
| Tile cells | 5.467 | 27.811 | 5.493 | 6.816 / 77.001 |
| Environment sprites | 9.700 | 26.146 | 9.627 | 12.206 / 92.528 |
| Full zone redraw | 39.661 | 78.095 | 41.800 | 50.337 / 134.593 |

Final wall-frame mean 3.135 ms, p95 3.169 ms, maximum **964.122 ms** across
19,196 frames. Raw outliers remain in the receipt. Sidebar all-frame mean is
.084 ms (original baseline 42.497 ms); hotbar .006 ms (original .781 ms).
Nested markers cannot be added. Restoring the original paint path removes the
measured regression; contact timing remains variable despite identical restored
code, so no second-slate contact speedup is claimed. The large wall outlier and
large-world synchronous capture remain limitations. These are Mac Editor
measurements, not Steam Deck FPS, battery or thermal certification.

Raw captures/markers: `Verification/DensityCompletion/ReferenceGlade/Native/ed533fbddfd04331a0adb5fcab235bd2`.
Compact receipt: `Verification/SteamDeckPerformance/2026-10-10/final-post-withdrawal-profile-summary.json`.
