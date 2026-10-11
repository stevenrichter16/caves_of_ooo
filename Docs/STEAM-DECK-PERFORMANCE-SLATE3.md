# Steam Deck performance, third measured slate

Status: correctness gates GREEN; timing comparisons inconclusive, Linux delivery verified (2026-10-11).

User confirmed current controls work after identifying an old build. Performance remains the priority. Music and world sounds are being added in parallel, with bounded sources and streaming music so they do not add a new recurring world scan.

## Plan and verification sweep

1. Verify the current 60-second native movement route, retain raw markers and all failure receipts.
2. Make direct Deck executable launches use the existing handheld defaults when Steam supplies its explicit `SteamDeck=1` hint; honor explicit application override and saved preferences. See `STEAM-DECK-DIRECT-LAUNCH-DEFAULTS.md`.
3. Remove redundant region-authority checks from non-gas rendering queries, retaining exact current mutable-state validation for actual gases. `TryGas` currently checks regional authority before inspecting the owner; it is also called while testing every ordinary owner for native rendering. Do not substitute the tag index: raw tag mutation is a current supported query case and the index requires notification.
4. Publish fog pixels only when their exact packed bytes change, retaining all current light/visibility sampling. Preserve initial upload, changed/replaced zone, reveal, light-only edits, null clearing and external texture mutation recovery. No assumed simulation revision.
   The audit also found repeated canonical cave-address parsing in surface visibility queries. Cache only the immutable address with weak zone ownership, retaining live manager/POI/lair authority; see `STEAM-DECK-CAVE-ADDRESS-CACHE.md`.
5. Profile matched settings after changes. Keep only improvements; reject slower candidates. Review next GPU or paint opportunities using evidence, not count of edits.
6. Verify native tests, actual movement/controller/audio route, build Linux `-current`, inspect package contents and publish only validated work.

Qud reference: none for these changes; engine optimization preserves the project's current behavior.

Corrections: the existing presenter already incrementally reconciles native owner geometry. Earlier paint suppression and contact distance-cache experiments were slower and rolled back; neither will be repeated. A gas tag index does not automatically observe raw `Tags` writes. The handheld launcher hint is absent when an executable is launched directly. Main/UI redraw markers overlap and must not be added as exclusive costs.

## Baseline

Native route `e6c5d38cbb5b4a6f92b29bdb753ebf3b`, 60.009 seconds, 18 checks passed, no failures. Raw files under `Docs/Verification/DensityCompletion/ReferenceGlade/Native/<id>/`. Focused full-detail 1920x1080 route, 320 active redraws:

| Marker | median ms | p95 ms |
| --- | ---: | ---: |
| Full redraw | 38.508 | 47.816 |
| Native refresh | 11.872 | 13.496 |
| Contact raster | 8.500 | 9.729 |
| Tile cells | 5.240 | 6.161 |
| Environment sprites | 9.355 | 12.673 |
| Turn processing | .026 | .034 |

All-frame median 2.337 ms, p95 3.407 ms, maximum 987.321 ms. Garbage-collection/editor allocations contaminate spikes; do not call those gameplay-exclusive or use the all-frame median as a movement latency result. Approximately130 draw calls in this route. This is not physical Steam Deck or Linux player timing.

## Review / evidence

Native receipts are under `Verification/SteamDeckPerformance/2026-10-10/slate3/`:

- `deck-defaults-red.xml`: 55 cases, five expected failures before direct-launch default support; later combined run passes all55.
- `gas-work-red-deck-green.xml`: three missing-counter failures before changing the gas query guard. All63 gas-source cases subsequently pass, including raw tag removal/restoration and biome changes. Only non-gas early rejection moves; actual gas authority remains current.
- `fog-cave-red-gas-green.xml`: ten fog work-counter failures and nine cave parse-counter failures before implementation; gas63 and cave authority20 already pass.
- `fog-adversarial-green-60.xml`: all60 surface/fog cases pass, including ten new upload cases and twenty added adverse visibility, external texture edit, disposal and independent-owner cases. Every comparison checks packed cell colors against the existing visibility sampler. Texture `updateCount` observes externally published edits; no world revision is invented.
- Audio's `Verification/WorldAudio/initial-native-198.xml` passes all29 cave cache and55 settings cases; it has one independent audio fixture failure and is not described as an all-green receipt.

Cold-eye review: skipped fog uploads still sample every cell and never alter FOV. A second surface has its own byte buffer and native texture revision. Disposed surfaces stay inert. Non-gas queries do no region-authority work; actual gas queries keep mutable membership, visibility and biome checks. The weak cave cache never stores a manager or eligibility answer. Independent review found no actionable issue in these three changes.

Final native regression passes1359/1359 in45 complete fixtures; audio fixture-only corrections subsequently pass232/232 focused audio cases. Receipts are in `Verification/WorldAudio/`. Image/feel, audio quality and physical Deck performance are separate from script-observable state and work-count tests. No content or 3D art reduction is planned.

## Profile review correction

The first audio-enabled candidate `e899074c89004dd387a6ce06dcf38c79` completed18checks with zero errors and matched screen/target/MSAA/focus settings, but its timed character drifted from X40/41 to X32/33 after12.5seconds. The old alternating D/A loop could step left after a blocked right input. The original baseline stayed in its intended corridor. Draw calls and live AI costs also differ. Therefore38.508→29.691ms is retained as an observation, **not a causal23% optimization claim**. `paired-profile.json` independently records the raw position counts and rejects a fixed-corridor equivalence claim.

A second audio-on control (`01f4a565203c461baa16302730672d92`) with only the four rendering source files temporarily restored to HEAD also drifted. Both invalid workload comparisons are retained. The benchmark now chooses its next actual cardinal input from the current position, requires X40/41,Y12 after every attempt, and reports actual moves versus blocked/action attempts. It does not freeze actors, alter gameplay RNG or teleport inside the timed loop. Bootstrap/load currently give NPCs unseeded RNG; seed64 controls geography, not identical AI outcomes. Even a confined profile requires reporting those remaining population/workload bounds.

The temporary control covers only `BiomeCropPlan`, `BiomeCropRecipes`, `NativeZone3DRenderSurface`, and `SpreadTransientSource`. Exact improved-source backups and hashes are retained outside the repo during the control; all four were restored and their exact improved SHA256 hashes verified before the final smoke profile and before committing/building.

The bounded old-renderer/audio-on control `5c16c11e81ea43098ecf949f979406fa` completed18/18 with zero errors, but recorded only11 successful moves and417 blocked/action attempts. Confinement fixed the drift, not live population variability. It is unsuitable as a matched movement timing baseline. This slate ships independently demonstrated work elimination (zero warmed address parses, zero non-gas authority checks, no unchanged fog upload), not a claimed measured FPS gain. Physical Deck profiling remains open.

Final improved-source smoke `66964c81172048a7a7861bf06a15b5f1`:18/18 checks, zero unexpected errors,60.108s interval,21,101 frames,139 moves and247 blocked/action attempts. Settings match the audio-on control; movement count does not (11 versus139). `bounded-audio-profile.json` records exact positions and raw marker observations without a causal timing claim. Final compile error console is clear; no runtime code changes followed the broad/focused GREEN except the audit-only corridor correction.

Linux delivery is complete at `Builds/SteamDeck/2026-10-10-performance-audio-current/CavesOfOoo-SteamDeck-performance-audio-current.tar.gz`, built from `33e79d4012e960d36115fedbf7602c2a94836024`. Zero build errors;170 packaged payload files and executable permissions verified. Full receipt and audio integration limits: `Verification/WorldAudio/Delivery/`. Older builds remain intact. Hardware Deck performance remains unmeasured.
