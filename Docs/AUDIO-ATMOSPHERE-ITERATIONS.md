# Sonic atmosphere: cumulative iterations

Status: all four iterations implemented and individually checked in fresh Play; final broader regression1786/1786 passed, 2026-10-11. Follow-up to AUDIO-PLACE-DESIGN.md. The user requests meaningful additional sonic atmosphere, a prompt for each iteration building on its predecessor, and actual game testing after EACH iteration. The additional twenty requested later are separate in AUDIO-POLISH-20.md.

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

All four exact prompts were saved before their corresponding production changes and after the preceding native game test, including source and live-route corrections.

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

### Iteration 3 verification and bounded live-route correction

Missing receipt/enums produced observed native compile RED. Two test-only API assumptions then required correction (internal crop service and nonexistent field-part grazer method); stale assembly returned0 tests and was explicitly rejected as evidence. Public crop-tick and actual grazer-turn paths replaced those calls. Initial38 native work tests passed; final40 include real paired resource binding and seed-only harvest. Broader native regression1766/1766 passed.

Fresh full Play `f201d1698f424750aa25c9d3d22e21ff` did NOT complete: live rotlings made the long crop-growth wait unsafe. Three material repairs and the original crop harvest all emitted their exact imported sounds with nonzero per-source samples; gathering, restored functions, harvest, planting and watering passed before the safe-wait guard stopped the route. The failed receipt/screenshots remain evidence, never counted as a successful gate. The rain screenshot was inspected and shows the real attacker and injured player.

Scope decision before further work: add a distinct `native-work-audio` route which finishes after the original harvest and real save/load, keeping the full growth route and its failure intact. No enemy, HP, crop clock or world state is edited to force success. This is sufficient to test the new sound paths in real play; crop growth remains covered by native unit regressions but is not claimed as newly proven live. Broadening this audio task into balancing an unsafe farming route would delay its actual purpose. Track the ordinary farming route's danger as an independent future usability check.

### Iteration 3 final live gate

The first bounded attempt `e3af2a66304d412982659442eea6fd43` also met real danger before reaching the crop. The work-only route now permits the existing ordinary walking helper to use at most two exact original starting tonics through inventory input at low HP. Full cultivation remains unchanged. No materials, health or creatures are granted or edited.

Fresh `native-work-audio` Play `f960c8915203426084b29babd8723a55`:21/21 passed, zero errors,23.16s. Three repairs and original harvest produced exact imported voices and nonzero per-source samples; real materials were consumed, repaired functions operated, and F5/F6 preserved world state without replaying work. Two original tonic units were consumed (26→34HP,26→35HP).16 voices, restored preferences, initial disclosed travel shortcut. Forge/portable repair and crop regrowth are unit-covered rather than newly live-proven here. All failed attempts remain recorded.

## Iteration 4 prompt — after iteration 3 live gate

The real work route confirms that contextual sounds can accompany paid actions, and its danger interruption shows why the combat demonstration should use a separate finite encounter. Add one bounded background-mix envelope triggered only by an accepted typed nearby melee sound. Lower music, bed and decorative details smoothly to60 percent over0.1s, hold until0.3s from the latest accepted cue, then restore over1.5s. Preserve action gains and user preferences. Refused/hidden/muted attacks, generic visual/raw-damage hooks, repairs and walking must never duck the background. Repeated attacks refresh the hold without compounding attenuation; graph resets and mute/pause cannot leave a queued dip. Use the existing16 voices, no new world queries. Execute native RED and integration/counter tests, then run the actual standalone northern-Scrabbler combat route and a separate fresh world-audio regression. Observe imported weapon sounds, actual mix dip/recovery and unchanged game time during the quiet recovery window; do not claim subjective audibility from counters. Preserve all earlier failure evidence and the fixed-pool limits. After this fourth earlier pass, begin the newly requested twenty additional iterations in AUDIO-POLISH-20.md; do not count this pass among them.

### Iteration 4 implementation and live gates

Observed native compiler RED for missing mix API, followed by20/20 focused native tests. Only accepted typed nearby melee refreshes the cosmetic envelope. Its hold-boundary split preserves recovery under different frame intervals; a refused cue, full action pool, mute, hidden target, generic hook or graph reset cannot queue a dip. Independent cold review found no material issue. Source count remains16, and there are no new scans, action gains, save fields or preference writes.

Fresh actual combat `31c75a02fa28427683af96e871bc3414`:18/18 checks passed, zero errors,10.84s. Six correlated keyboard attack windows,28 accepted local combat cues including ordinary retaliation, minimum background multiplier0.6; passive recovery0.6→1 over2.202s without a game turn, position, equipment or stat change. Player defeated the original northern scrabbler using the ordinary dagger and one actual starting Rime cast; no tonic/grant. Imported melee source observations,16 voices and preference restoration passed. Attack screenshot inspected: native world and original encounter render. The screenshot named `04-player-defeat-outcome` refers to the player's victory outcome; it is not evidence of player death.

Separate fresh actual world-audio regression `4141d504f1944c64aa9fd83651440406`:26/26 passed, zero errors,64.28s. Imported music, ambience, new generated vegetation texture, independent lanes, actual walk, controller settings/mutes and F5/F6 all passed. These are source and sampled-signal checks, not a subjective listening or physical Steam Deck verdict.

🧪 Previous full cultivation route remains unsafe during a long live growth wait; retained as separate failed evidence. The bounded work route covers this feature's actual repair/harvest sound requirements. 🧪 Newer twenty-pass request supersedes immediate delivery of a four-pass-only archive: finish the requested polish before producing the next current build. The four-pass packager remains prepared and tested, not falsely reported as a completed build.

Final native regression:1786/1786 passed (67 fixtures,286.19s). The extra20 are the new combat-mix tests. Cold review covered all four passes; prior portable-foliage issue fixed in iteration2, no additional material finding. Final work-harvest screenshot inspected: actual crop harvest and attacking rotlings visible; no safety/balance claim.
