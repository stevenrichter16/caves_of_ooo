# Steam Deck optimization implementation — 2026-10-10

Status: first slate and measured save follow-up implemented, reviewed and verified. Both slower rendering proposals were removed. Linux package delivery follows. Baseline `5657211c5`. Scope is the seven
confirmed findings in STEAM-DECK-PERFORMANCE-AUDIT.md, with bounded follow-ups for
combat cues/FX and measured pathfinding costs. Original implementation work; no
Qud port. Keep Vulkan, gameplay/content, save recovery and the authored 3D style.

## Execution contract

Each milestone gets an executed failing regression/work-count test, minimum fix,
counter-checks, adversarial review and native Unity verification. Tests measure
observable work as well as output; wall-clock improvements are measured separately.
Root coordinates the one Unity editor. Bounded HUD/render/save units use separate
files and living notes; shared input/zone/UI integration remains root-owned.

1. **Baseline and measurement.** Retain the unchanged build and collect an isolated
   existing native movement profile before production changes. Use work-count and
   allocation tests for idle HUD. Record unavailable counters and editor/hardware
   limits explicitly. The Mac cannot run Mono Linux gameplay through its emulator;
   do not repeat software Vulkan as a Deck-speed benchmark.
2. **HUD.** Replace inventory-screen construction with direct sidebar vitals;
   cache hotbar state/metadata and gate painting. Preserve equipment-derived
   defenses, weight/currency, targeting/cooldowns, clear/layout and modal lifecycle.
3. **Rendering.** Suspend hidden contact/legacy water work. Reconcile known dirty
   owners incrementally while retaining a full conservative fallback. Distinguish
   visibility invalidation from geometry invalidation. Restore SRP-compatible
   uniform material families without stripping true per-instance data.
4. **Handheld controls/settings.** Add controller-accessible graphics settings,
   persistent independent world resolution/shadows/effects and sensible handheld
   defaults. Preserve readable UI and full-detail choice. Respect desktop settings.
5. **Saving.** Cache immutable reflection metadata. Capture consistent immutable
   data on the main thread, queue bounded background compression/atomic file IO,
   and handle completion, failures, load/new game/quit explicitly. Measure capture
   separately; do not call live serialization from a worker. Investigate bounded
   snapshot capture if synchronous serialization remains over budget.
6. **Simulation/cues.** Replace broad terrain-source and committed-actor discovery
   with maintained/queryable relevant-owner membership and reusable snapshots.
   Narrow material simulation safely without losing direct field changes or
   advancing new ignition twice. Preserve multi-cell and event ordering contracts.
7. **Diagnostics/FX.** Explicit economical release diagnostic policy with opt-in
   detail; preserve failure evidence and record-time payload values. Eliminate
   unconditional snapshot allocations. Avoid duplicate FX painting when no new
   request needs admission, preserving first-frame/contact/timeout behavior.
8. **Integration and delivery.** Native regression sweep and hypothesis-driven
   review; same-route post profile; save failure/round-trip and settings/input
   play checks. Fetch/rebase, commit with living docs, push main, build a new Linux
   archive, verify contents/checksums. Report measured improvements and limits.

## Sweep corrections and design risks

| Area | Verified premise | Design consequence |
| --- | --- | --- |
| HUD | Sidebar renderer skips paint only after its expensive builder. | Optimize construction, not just final painting. |
| Native dirt | Full dirty currently discards cell hints; source strings include unknown/bulk changes. | Preserve geometry dirt independently; only proven visibility sources use narrow refresh. |
| Saves | Current format uses a session-wide entity graph; Part fields mutate without EntityVersion. | EntityVersion is not an unchanged-chunk save cache key. No unsafe token-buffer reuse. |
| Materials | Thermal state is publicly mutable and serialized as fields. | Do not replace fields with properties or miss direct writes merely to build an active set. |
| Graphics | Some per-renderer overrides encode visibility/tint/ambient. | Retain exceptional blocks; uniform material-family changes require image/ownership tests. |
| Settings | Low detail only exists on Shift+F11; all Unity levels share URP. | Add real game settings to existing controller pause flow. |
| Profiling | Older editor timings are not current Deck timings. | Claim work eliminated and actual measured local differences separately. |

## Acceptance invariants

- Unchanged hotbar does no clear/repaint; sidebar never gathers carried-item
  actions for its vitals. Mutated visible data changes on the next presented frame.
- No contact raster/upload when hidden/disabled; all visibility and fallback
  paths restore correct current data without hidden-owner disclosure.
- Small known geometry changes process affected owners; unknown/bulk changes
  still safely perform full reconciliation. Model failure restores fallback.
- Settings are operable with D-pad/A/B, persist, and preserve HUD resolution.
- Background save jobs never touch Unity APIs or live entity graphs; bounded
  ownership, ordered commits, errors and shutdown/load/new-game drains are tested.
- Environment membership covers add/remove/effect/part/save-load changes; direct
  thermal writes and multi-cell objects are not silently skipped.
- Diagnostic exports capture event-time values; optimized FX timing/damage remains
  unchanged. Native regression results and actual runtime limits are recorded.

## Verification log

Native baseline run `8ff7c578fedc4ce7be1d7f6fa2195181` completed all 18
ordinary keyboard route checks, no unexpected errors, before production edits.
Same 1920×1080 editor workload: 60.322 seconds, 1,003 frames; wall frame mean
60.217 ms / p95 100.32 ms. Sidebar construction averaged 42.497 ms; native
refresh 5.174 ms, contact raster 1.476 ms, RenderCells .939 ms, environment
sprites 1.753 ms. Markers nest and cannot be added; editor timings are not Deck
performance. Raw baseline and screenshots are retained under
`Docs/Verification/DensityCompletion/ReferenceGlade/Native/8ff7c578fedc4ce7be1d7f6fa2195181`.

Native broad run `e2b7486c6ca147138ec56075d2a974eb` completed 1,742 cases with
five failures. Four were real native empty-dirt compatibility regressions, fixed
and then all 58 selected old/new rendering adversarial cases passed in
`4546aed9331140e6bdcd01783f170b35`. One was an existing LampOil inspection pin:
commit `f239fb81b` already added its useful tactical popup, while this older test
still rejected any announcement. Updated the test to assert the actual oil-film
text and explicitly reject the repair-guidance command; no item production change.
That job also executed nine source-order review tests: four expected REDs, five
counterchecks passed. The source-order review fix passed the later 56-case follow-up. Individual RED/GREEN receipts
are in `Docs/Verification/SteamDeckPerformance/2026-10-10`.

The first save timing probe was rejected: nested fixture registration caused it
to serialize the wrong tiny session. No timing claim is based on that receipt.
The corrected probe requires capture ownership, plausible bytes and exact saved
zone/owner roundtrip before publishing samples.

Detailed unit plans:
STEAM-DECK-PERF-HUD.md, STEAM-DECK-PERF-RENDER.md, STEAM-DECK-PERF-SAVE.md.

## Self-review

🧪 Physical Deck profiling remains unavailable locally. No guaranteed FPS target.
🟡 Save snapshot/invalidations are correctness boundaries; optimize within them,
never suppress autosaving or use a stale cache to make a benchmark appear fast.


### First-slate post profile and decision

`ebe72af5349d429088786810a9522db9`: all 18 route checks passed, zero
unexpected errors. Same screen/target, full detail, MSAA, focus, frame-cap and
profiler settings as baseline. Ground builds remained 40 throughout both timed
intervals. The image still displays native 3D terrain, actor, objects and fog.
The new 60-second interval recorded 15,505 frames, wall mean 3.882 ms / p95
3.661 ms. Sidebar marker mean fell from 42.497 to .103 ms; hotbar .781 to
.008 ms. These are local editor observations, not Deck FPS.

The faster loop completed 306 movement refreshes versus 197 before; this is the
same route/input algorithm and duration, not an identical action-count replay.
All-frame averages therefore dilute movement costs differently. Active-only
marker medians are a better guide for the next slate:

| Marker | Before active median ms | After active median ms | After active p95 / max ms |
| --- | ---: | ---: | ---: |
| Native refresh | 25.800 | 12.268 | 14.119 / 79.242 |
| Contact raster | 7.440 | 8.542 | 9.646 / 14.551 |
| Tile cells | 4.633 | 5.467 | 6.443 / 66.321 |
| Environment sprites | 8.314 | 9.700 | 16.098 / 106.254 |
| Full zone redraw | 46.047 | 39.661 | 51.471 / 268.850 |

Nested markers are not exclusive. Existing allocation recorder startup outliers
remain in raw data; no allocation reduction claim is based on those averages.
The valid generated-world save probe shows 20 zones / 45,596 owners / 35.5 MB
capturing in 433–509 ms, then background gzip in 100–103 ms. It checks the actual
capture callback, payload size and full zone/owner roundtrip. Save capture remains
an explicit unresolved latency target, not hidden behind the async-write result.

Second slate: safe hidden native tilemap paint suppression, measured full-detail
contact raster work, and AOT-safe save serialization descriptors. Plans are separate
living docs. Do not spend time on AI/pathfinding until it becomes material in the
new profile; baseline AI averaged .162 ms across frames. Physical Deck validation
remains unavailable. The optional launcher now supplies COO_HANDHELD=1 while
preserving explicit caller and saved choices; direct executable users can select
Menu → Graphics → Handheld preset.

### First-slate review closure

- Fixed: late-attached terrain sources retain original dictionary visit position;
  nine review tests and all 28 simulation tests pass. The same native job
  `83272dc347de44d9934505dfbd771075` passes all 56 selected tests, including
  all material-guidance tests and the corrected save probe.
- Fixed: public empty native dirt stays conservative; explicit visibility refresh
  has a separate contract. Existing mutation regressions remain unchanged.
- Fixed: stale backup rollback cannot overwrite the last good primary checkpoint.
- Fixed: release diagnostics retain missing/unmapped model evidence and have a
  real COO_DIAGNOSTICS=1 opt-in. Retained payloads remain record-time snapshots.
- Independent cold-eye review covered save queue ownership/drains, membership and
  callback order, direct HUD parity, menu input/persistence, render ownership and
  diagnostic timing. Concrete findings above were resolved before commit.
- Deferred: per-instance ambient/transient material blocks stay intact. Uniform
  static blocks are omitted, but actual GPU batching and Deck speed need hardware
  evidence. No gameplay/content was reduced.


## Accepted scope and final verification

The second-slate save descriptors preserve version-7 bytes and improve paired
native capture medians by 53% for one zone and 69% for twenty zones. Native hidden
painting and contact-band proposals passed correctness checks but lost the timing
gate; they were removed completely. See `STEAM-DECK-OPTIMIZATION-SLATE2.md` for
plans, negative experiments, scope divergence, final-source test receipts and
matched configuration/active timings. The release keeps all seven first-slate
areas plus the accepted save follow-up and controller-driven graphics audit.

Final-source native regression: 240 passed / 0 failed / 1 allocation-counter skip.
Earlier broad sweep: 1,902 passed / 0 failed / 1 skip; counts overlap and are not
additive. Final ordinary input route: 18/18. Graphics menu audit: 12/12 with exact
preference/device cleanup. Local measurements establish improvements, not Deck FPS.
