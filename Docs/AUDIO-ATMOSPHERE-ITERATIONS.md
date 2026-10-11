# Sonic atmosphere: cumulative iterations

Status: iterations 1–2 complete; iteration 3 released; 2026-10-11. Follow-up to AUDIO-PLACE-DESIGN.md. The user requests meaningful additional sonic atmosphere, a prompt for each iteration building on its predecessor, and actual game testing after EACH iteration. Four bounded iterations are selected; do not implement a later iteration before the preceding live gate has run and its findings are incorporated.

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

Iterations 3–4 are directions, not executed prompts. Their exact prompts will be written only after the preceding native game test, including what was learned and any scope correction.

## Iteration 1 result and review

Observed missing-counter compiler RED before production. Native focused run initially122/123 passed: the hidden-frog test accidentally put its control tree on the same hidden cell; corrected the fixture to leave the tree visible. Final123/123 passed. Source cold-eye review found no material scheduler regression. The shared four-second interval intentionally exceeds every shipped environmental phrase (old maximum3.02s, new maximum2.522s); later continuous/long phrases would require exact per-voice witnesses before relaxing that bound.

Actual fresh Play run `5d453ebd33ac413487935210e93d2220`:24/24 checks passed, zero unexpected errors,42.41s. Generated glade produced2 habitat and2 local material phrases with no injected wildlife;16 voices and no idle scan growth. Real movement, controller settings and save/load passed. Wildlife fairness is separately covered by native unit tests, not falsely claimed from this wildlife-free local view. Full receipt/screenshots under Docs/Verification/WorldAudio/Native/<runId>.

## Iteration 2 prompt — after iteration 1 live gate

Build on iteration1's verified independent phrase lanes and four-second silence gap. Give actual outdoor trees/bushes a leaf texture and reeds their own soft dry rustle; sheltered timber should settle quietly instead of sounding like a woodland canopy. Add sparse dry stone movement only near real stone structure underground, sheltered or in ruins. Give current positive nearby water a drop texture only underground or under shelter; oil/acid and dry stone must never imply water. Do not add generic room crowds or a new room loop merely to remove silence. Use the approved new20-clip CC0 palette through an explicit third catalog, include its credits in-game, and keep all old53 assets unchanged. Action recordings in the same palette remain dormant until iteration3. Keep discovery bounded; expand exact category slots only as needed. Write and observe failing native behavior and actual-resource-binding tests, implement, review source-loss contrasts, then run the actual game again and observe one of the new texture recordings playing near its real generated source. Record the result before composing iteration3's prompt.

### Iteration 2 implementation sweep and initial result

The source sweep found `Reeds` has no MaterialPart, so it uses its exact blueprint identity; tree/bush foliage also uses exact identity. Structural sounds require owned material plus a wall/door and non-takeable physics, rather than sounding every dropped wooden/stone item. Outdoor vegetation is suppressed under shelter and below ground. Nine fixed category witnesses replace seven; all other169-cell/512-owner/16-voice bounds remain. Existing test libraries now include the new material palette so old source-loss invariants still exercise live selection instead of missing-resource silence.

Executed native RED:23 cases,11 passed/12 expected failures (wrong/missing material IDs, unwanted underground vegetation, absent third runtime catalog). Implemented distinct selection and explicit catalog/credits loading; initial affected native sweep136/136 passed. The new ten work recordings are intentionally dormant until iteration3 appends its typed cue values. Material playback remains four seconds apart, with all new detail recordings shorter than2.53 seconds.

### Iteration 2 final gate and self-review

🟡 Independent review found portable foliage could bypass fixed-source refusal. Its dedicated21-case fixture produced18 passes/3 expected portable tree/bush/reed failures; moved the Takeable check before the foliage branch. Final affected native suite157/157 passed. Material removal/foreign ownership, changing shelter, source destruction, positive water vs oil/acid, hidden foliage and the reachable three-file credits all have counters.

Fresh actual Play `337a4038275e494a927b8adaf2caebb1`:26/26 passed, zero errors,63.94s. The generated glade played `leaf_rustle_02` through its real visible vegetation; no injected cue. Lane fairness,16 voices, idle scan bounds, actual movement, controller settings and save/load passed. Arrival screenshot inspected: native world renders with visible vegetation/water/stone and player; no blank viewport. 🧪 Indoor/underground contrasts are native-unit verified, not claimed as traversed here. Acoustic pleasantness/physical Deck speakers remain unverified.

## Iteration 3 prompt — after iteration 2 live gate

The actual glade now plays the new foliage texture without extra voices or idle discovery work. Build on those material distinctions with one-shot committed repairs, explicit field/cultivated harvest and actual visible-forge actions. Use paired WorkForge, WorkRepairWood, WorkRepairStone, WorkRepairMetal and HarvestPlant; Fiber/Leather use existing handling. Work must follow real transaction completion, preserve payment and gameplay, and reject rollback, stale/hidden sources, automatic crop maturity and idle forge presence. Preserve legal single-weapon assembly away from a forge but do not invent an anvil there. An explicit local player's menu completion may sound; hidden remote actors may not. Read AUDIO-ATMOSPHERE-WORK-SWEEP.md, execute missing-API/native RED before production, add counterchecks, implement and run native regressions. Immediately run a fresh actual-input generated-field route with three material repairs and the original ripe crop harvest; record actual imported sound acceptance and signal. Keep the existing disclosed initial travel shortcut and honest live-forge limits. Passive kitchen work is deferred because it needs a dedicated activity predicate and ordinary route. Do not begin iteration4 before this live gate.
