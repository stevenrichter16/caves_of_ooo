# Sonic atmosphere: cumulative iterations

Status: iteration 1 planned; 2026-10-11. Follow-up to AUDIO-PLACE-DESIGN.md. The user requests meaningful additional sonic atmosphere, a prompt for each iteration building on its predecessor, and actual game testing after EACH iteration. Four bounded iterations are selected; do not implement a later iteration before the preceding live gate has run and its findings are incorporated.

## Direction and gates

1. **Living foreground:** independent sparse wildlife, physical-place and habitat phrases. A nearby animal should not lose its voice because scenery won a random lottery. Preserve quiet gaps, make selection fair within each lane, and avoid restarted schedules when walking.
2. **Material character:** build on the first mix with separate leaves, reeds, settling timber, dry stone and water drops justified by actual surroundings. Retain differences among outdoor vegetation, sheltered rooms, underground and dry ruins.
3. **Inhabited work:** build on material character with sounds of committed repair, harvest and real forge work; inspect paid kitchen activity rather than inventing a town crowd. No work loop just because a tool or forge exists.
4. **Audible priorities:** use the richer scene to tune transitions and combat readability. Briefly lower environmental/music levels for accepted nearby weapon action, recover smoothly, preserve volume preferences and all refusal/visibility gates. Let quiet return rather than accumulating sound.

Each gate: save the exact prompt below, verify cited source behavior, write and observe RED, implement, run native unit/counter checks, then enter the actual game using the isolated native launcher. Expand the live route to exercise each feature. Retain run IDs, checks, screenshots and observed sample/pool bounds. A deterministic isolated bench may be added where ordinary randomized routes cannot guarantee all contrasts; label staged setup, never claim it is natural discovery. No gameplay writes through execute_code. Read-only source observation is acceptable. No final claim of subjective listening, physical Deck performance or all-chunk coverage from audio-source counters.

## Verified source sweep before iteration 1

- WorldAudioSoundscape currently schedules exactly one random choice among all retained source categories plus habitat every10–22 seconds. This has no category fairness bound; a wildlife call can repeatedly lose to scenery. Graph changes seed its cosmetic RNG; ordinary Observe refreshes do not reseed.
- Source discovery is bounded to169 cells/512 owners; seven category witnesses and two active-detail witnesses prevent hidden/dead/stale sound. Retain these limits and tests.
- WorldAudioPlayer already caps ambient details at two voices inside the same16-source pool and permits actions to preempt them. Counters are lifetime totals; clips last at most3.02 seconds.
- Source authority, day/night, interior and water invalidation were already fixed in the previous pass. Do not repeat those fixes or weaken their refusal checks.
- Asset and work-mechanic sweeps are separate referenced documents. New source recordings require CC0 provenance and an independent expansion catalog; preserve all existing53 clips.

## Performance

Follow PERF-FOUNDATION.md: no whole-zone/per-frame discovery, allocation-free cosmetic scheduling, fixed arrays and existing voices. Three lane timers are constant work, no simulation RNG. New resources load once on initialization. Each live route checks pool count and no idle rescan; retain a real gameplay profiling window in the final gate before making any performance claim.

## Iteration 1 prompt — execute now

Read AUDIO-PLACE-DESIGN.md and the actual WorldAudioSoundscape/Player/Runtime code. Replace the single scenery-versus-animal lottery with three restrained lanes: nearby living animals, local material/objects and background habitat phrases. Give wildlife an earlier first opportunity and a fair turn among available animal categories; keep slower environmental and habitat pacing, a shared minimum gap and the existing two-detail limit. Ordinary movement must not restart the schedule; menus/mutes must not accumulate bursts. Do not add hidden-enemy hearing, new animal species, random-gameplay consumption or additional AudioSources. Add executed failing tests with counters for each lane and alternating animal kinds, then implement and run native regressions. Test the actual game immediately, record observed lane activity and movement/settings/save-load behavior, and use those results to write the next iteration prompt. Fix material regressions before continuing.

## Later prompt status

Iterations 2–4 are directions, not executed prompts. Their exact prompts will be written only after the preceding native game test, including what was learned and any scope correction.

## Iteration 1 result and review

Observed missing-counter compiler RED before production. Native focused run initially122/123 passed: the hidden-frog test accidentally put its control tree on the same hidden cell; corrected the fixture to leave the tree visible. Final123/123 passed. Source cold-eye review found no material scheduler regression. The shared four-second interval intentionally exceeds every shipped environmental phrase (old maximum3.02s, new maximum2.522s); later continuous/long phrases would require exact per-voice witnesses before relaxing that bound.

Actual fresh Play run `5d453ebd33ac413487935210e93d2220`:24/24 checks passed, zero unexpected errors,42.41s. Generated glade produced2 habitat and2 local material phrases with no injected wildlife;16 voices and no idle scan growth. Real movement, controller settings and save/load passed. Wildlife fairness is separately covered by native unit tests, not falsely claimed from this wildlife-free local view. Full receipt/screenshots under Docs/Verification/WorldAudio/Native/<runId>.

## Iteration 2 prompt — after iteration 1 live gate

Build on iteration1's verified independent phrase lanes and four-second silence gap. Give actual outdoor trees/bushes a leaf texture and reeds their own soft dry rustle; sheltered timber should settle quietly instead of sounding like a woodland canopy. Add sparse dry stone movement only near real stone structure underground, sheltered or in ruins. Give current positive nearby water a drop texture only underground or under shelter; oil/acid and dry stone must never imply water. Do not add generic room crowds or a new room loop merely to remove silence. Use the approved new20-clip CC0 palette through an explicit third catalog, include its credits in-game, and keep all old53 assets unchanged. Action recordings in the same palette remain dormant until iteration3. Keep discovery bounded; expand exact category slots only as needed. Write and observe failing native behavior and actual-resource-binding tests, implement, review source-loss contrasts, then run the actual game again and observe one of the new texture recordings playing near its real generated source. Record the result before composing iteration3's prompt.
