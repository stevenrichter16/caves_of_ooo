# Steam Deck native presentation: evaluated and rejected hidden tile paint

Status: **rejected by the measured performance gate and reverted**. The candidate
passed its correctness tests but made active rendering substantially slower.
ZoneRenderer.cs and EnvironmentSpriteRenderer.cs were restored to first-slate
commit `bf3c2468b`; the coordinated presenter/ground ownership helpers and the
three candidate-only test files/metas are also removed. First-slate HUD,
graphics and save changes remain intact. No cache-based rescue is planned.

All Unity refresh, test, Play and profile actions were root-owned. This record
preserves the investigation and evidence; it does not describe shipping code.
Qud reference: none; this was an attempted presentation-work reduction.

## Measured rejection

The matching Full/1080 native route used the post-first-slate run
`ebe72af5349d429088786810a9522db9` as its baseline. The candidate run was
`3795617a6472467b8b49470d9110ef54`. These are Mac Editor observations, not Steam
Deck hardware measurements. Active-sample medians were:

| Marker | Before candidate | With candidate | Change |
| --- | ---: | ---: | ---: |
| RenderCells | 5.467 ms | 27.811 ms | +22.344 ms |
| EnvSprites.PostRender | 9.700 ms | 26.146 ms | +16.446 ms |
| Full RenderZone | 39.661 ms | 78.095 ms | +38.434 ms |

Raw candidate evidence:
[profile-markers.json](Verification/DensityCompletion/ReferenceGlade/Native/3795617a6472467b8b49470d9110ef54/profile-markers.json)
and [profile-markers.csv.gz](Verification/DensityCompletion/ReferenceGlade/Native/3795617a6472467b8b49470d9110ef54/profile-markers.csv.gz).

The overall frame mean includes idle samples and must not replace these active
render comparisons. Fewer tile writes did not produce a CPU timing benefit.
Root rejected this slice and authorized the bounded revert instead of expanding
invalidation caching. The matching post-revert profile remains a root-owned
verification step; no recovery timing is claimed here yet.

## What was evaluated

The candidate deferred glyph/background paint for current native bodies, then
omitted empty environment restore claims for the same cells. It retained one
Render event per qualifying redraw, the glyph selected before that event, and
colors/background selected afterward. A fixed 2,000-cell buffer held fallback
tiles/colors for one pass and flushed unfinished entries in a finally block.

Fresh ownership was checked before deferral and again after every cell's Render
handlers had run. A tri-state result distinguished submitted native output,
authoritative rejection, and conservative abstention. That distinction prevented
stale authored/animated predicates from hiding fresh fallback after cross-cell
mutations. Village terrain/portable categories retained their existing route.
Dirty background hygiene reused the pre-event decision rather than querying it
an extra time. No ownership result survived a frame.

Those proofs were expensive. Source review found repeated presentation/scope
checks, the full nested SpawnRing3DRecipes.Resolve refinement chain, and submitted
ground/body renderer checks in each proof. The two per-owner calls align with the
large regressions in both measured paint stages. The exact cost of each internal
operation was not isolated, so this is a source-backed diagnosis rather than a
claimed microprofile. Removing only one proof would also discard a correctness
boundary without establishing an overall win. The baseline paint path is the
accepted implementation.

## Correctness and review evidence before rejection

- The initial 14-case native suite ran against the previous implementation first.
  Five failures confirmed hidden glyph writes, nine empty restore claims, stale
  transition restoration and unsupported post-Render cached ownership. Nine
  existing-behavior counterchecks passed. Receipt:
  [native-paint-slate2-red.json](Verification/SteamDeckPerformance/2026-10-10/native-paint-slate2-red.json).
- The first implementation passed all 14 focused cases in native job
  `c32c1c5ecf0247169a88a28cd12135e3`. A separate Village owner fixture initially
  selected an attachment instead of a captured body renderer; correcting that
  fixture preserved its rejection expectation and the nine owner cases passed.
- The dedicated 21-case adversarial suite exposed five further failures:
  earlier-cell mutation, direct RefreshCell, disabled body renderer, inactive
  body root and forceRenderingOff. The other 16 adversarial and nine then-current
  owner cases passed. The tri-state correction followed this executed RED.
  Receipt: [native-paint-slate2-adversarial-red.json](Verification/SteamDeckPerformance/2026-10-10/native-paint-slate2-adversarial-red.json).
- Final combined native job `59fe6ab7ac134b4795dc6d1ec31cb9b9` completed 1,903 cases:
  1,902 passed, zero failed, one save-descriptor allocation check skipped because
  the runtime lacked a usable per-thread counter. All 14 focused, 21 adversarial
  and 10 ownership cases completed successfully. Receipt:
  [final-combined-native.json](Verification/SteamDeckPerformance/2026-10-10/final-combined-native.json).
- Independent final read-only review found no further actionable correctness
  issue. It checked tri-state authority versus abstention, exact Zone guards,
  synchronous direct refresh, shared dirty-background proof, fog/remembered early
  returns, finally cleanup and pre-/post-event glyph timing. The review explicitly
  kept the net cost of fresh ownership proofs subject to profiling.

The candidate-only files SteamDeckNativePaintTests.cs,
SteamDeckNativePaintAdversarialTests.cs and SteamDeckNativePaintOwnerTests.cs,
including their metas, were removed with the rejected implementation. Their
recorded passes demonstrate the tested candidate's correctness boundaries; they
do not turn its failed performance result into an accepted optimization.

## Findings retained for future work

The source sweep read CLAUDE.md, ADVERSARIAL_TESTING.md, PERF-FOUNDATION.md, the
performance audit, both painters, native presenters/surface, animation/light
hooks and Render handlers. These constraints remain relevant:

- Native cell coverage is not exact entity ownership. Unsupported runtime tops
  need sprites/glyphs above the native composite even inside a covered cell.
- Cached IsRenderedEntity and authored membership are not fresh recipe proof
  after arbitrary Render mutations. Repeating those queries cannot establish it.
- Render handlers have side effects: damage-flash lifetime, light-flicker
  intensity, campfire animation and settlement repair-stage behavior. Render is
  redraw-driven, including direct RefreshCell/RefreshMovement, not a general
  once-per-frame clock. Skipping the event changes established behavior.
- Remembered cells select terrain, not their current actor/item top. Unexplored
  cells and hazard marks have separate contracts that must stay readable.
- Full redraw already discards previous environment claims after clearing the
  main/background canvas. Incremental release and fullscreen cleanup must not
  resurrect stale occupants over new content.
- Native surface failure may require distant fallback in the same frame.
  Generic authored Felling/Morrowfast 2D presentation is a separate scope.
- Environment write counters cover that component's own operations, not every
  ZoneRenderer write/flag/color call. Visited-cell counts are not write counts,
  and either kind of work count is weaker evidence than the final native profile.

No source-level native-paint optimization from this candidate is retained.
The accepted plan divergence is the measured revert, preserving the simpler,
faster baseline while other independently validated performance work proceeds.


Final acceptance closure: after candidate withdrawal, native job
`1128918628c74d2f9d3c41a5864c8e4c` completed 240 passed / zero failed /
one unsupported allocation-counter skip. Final movement run
`ed533fbddfd04331a0adb5fcab235bd2` passed all 18 route cases; full-zone active
median 41.800 ms, tile cells 5.493 ms, environment sprites 9.627 ms. Captures
were visually inspected. Full evidence and limitations are in
`STEAM-DECK-OPTIMIZATION-SLATE2.md`; no physical Deck performance claim.
