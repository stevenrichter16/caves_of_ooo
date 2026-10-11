# Music and lifecycle sweep for the next sonic iterations

Status: read-only production audit, 2026-10-11. This is a proposed ordered contribution to the requested next twenty audible improvements, not twenty completed changes. Preserve the preceding four cumulative iterations and their individual live gates in [AUDIO-ATMOSPHERE-ITERATIONS.md](AUDIO-ATMOSPHERE-ITERATIONS.md). In particular, the iteration 4 mix draft at `/tmp/coo-audio-iteration4/WorldAudioMixTests.cs` remains unchanged. No Assets, Unity state, preferences or audio assets were changed by this sweep.

## What is actually present

The catalog contains four music identities but only three complete compositions:

| Music identity | Shipped composition | Actual file duration | Current use |
| --- | --- | --- | --- |
| `organic` | Ephemera | 338.362 seconds | Spread, Sodden, Grovelands, Jungle |
| `dry` | Shadows and Dust | 408.380 seconds | Beating, Overwrit, Desert |
| `underground` | Decoherence | 618.089 seconds | Any positive depth, Cave, Ruins, Stump |
| `danger` | Decoherence, same file | 618.089 seconds | Any visible nearby hostile, then a four-second release |

Durations and stereo 44.1 kHz format were read from the three shipped Ogg files with ffprobe, rather than estimated from filenames. Their total compressed size is 18,629,708 bytes. The original provenance retains entire compositions. This audit did not listen to them and cannot rank their emotional or stylistic quality from their titles.

Relevant source snapshot:

- `Assets/Resources/Audio/World/catalog.json`: all four mappings above; both expansion catalogs currently contain an empty music list.
- `Assets/Scripts/Presentation/Rendering/WorldAudioContext.cs:10–20`: music knows only biome, depth and danger. It has no town, day band, interior or world-map identity.
- `WorldAudioLibrary.cs:13–17,29,36–61` in the same rendering directory: one `Entry` per music ID; adding the same ID replaces the previous entry. Sound and detail variants already use lists, but music does not. Catalog resources resolve during initialization, not from the frame tick.
- `WorldAudioPlayer.cs:14–64,85–94,131–156`: two-second equal-power loop transitions, two music voices, two ambience voices, twelve shared short voices. Music sources are configured to loop forever. There is no track-end/intermission/playlist policy. A reversal reuses a still-live outgoing source, but a finished outgoing fade clears its clip and playback position. Mute/pause also clears loop sources.
- `WorldAudioRuntime.cs:37–67`: world changes, turns, map replacement or a resumed world trigger the bounded context scan; idle only rechecks a known danger witness. Music remains during ordinary menus. Graph replacement clears short sounds but need not restart an unchanged music entry.
- `WorldAudioSoundscape.cs:33–65,67–90,133–146`: local sound discovery is bounded to 169 cells and 512 owners. Nine retained source categories, three phrase lanes and the four-second shared interval are already implemented; do not sell these again as new improvements. It already distinguishes time of day, shelter and real flowing water for environmental audio.
- `WorldAudioSettings.cs:8–17` and `Assets/Scripts/Presentation/UI/PauseMenuController.cs:286–295`: independent music/ambience/SFX preferences, explicitly clamped and saved; menu changes are ten-percentage-point steps.

The repository uses Unity 6000.3.4f1. Unity's matching documentation supports reading/seeking playback in PCM samples through [`AudioSource.timeSamples`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AudioSource-timeSamples.html). Its [`AudioSource.time`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AudioSource-time.html) documentation cautions that compressed-packet timing is not exact. Any continuation implementation must therefore have a real imported-stream Play check; a synthetic EditMode playhead assertion alone is insufficient.

## Ordered candidate iterations

### M1. Preserve the correct musical region on the world map

**Verified gap.** `WorldAudioRuntime.Tick` parses every zone with `WorldMap.FromZoneID`, then defaults to Spread when the resulting coordinates are out of bounds. `WorldMap.WorldMapZoneID` is the literal `WorldMap`, and `FromZoneID` explicitly returns `(-1,-1,-1)` for that string (`Assets/Scripts/Gameplay/World/Map/WorldMap.cs:66,253–256`). Ascending from Beating or Stump therefore selects organic music. Local soundscape correctly rejects the world-map zone (`WorldAudioSoundscape.cs:48`); music lacks the equivalent explicit policy.

**Player payoff.** Checking a travel map no longer makes a desert or root-slope expedition suddenly sound like the starting region.

**Recommended policy.** On the world map, derive the *current occupied map tile's already-present biome* using the player's current cell and `WorldMap.ZoneCellToWorldCell` (`WorldMap.cs:366–372`), with bounds checks. Keep local source ambience and threat discovery disabled there. Ascending and descending on the same tile should retain its regional composition. Moving across actual map biome boundaries may request a new region, using the same transition policy as ground travel. Do not read POIs or enemies on unvisited destination zones merely to choose a score. An invalid/detached map cell should yield no new identity, rather than fabricate Spread.

**Native/live gate.** Add synthetic counterpart checks for a real map cell, invalid cell, absent actor, disabled world and same-tile ascent/descent. In a fresh actual game, travel to a real Beating surface via the isolated route's explicitly reported initial travel shortcut, then use normal ascend/map-step/descend inputs. Record current map coordinates, known biome, music identity, imported playing clip and nonzero source samples. The real route must prove no jump to organic just for ascending; it must not call `SetContext` or seek music itself. Sixteen voices and no idle scan increase remain required.

**Prompt to execute after prior live gate.** Fix only world-map musical identity using the current actor's actual occupied map tile. Execute failing policy/runtime counterchecks first, preserve hidden-source and source-budget behavior, then run the real map-input route and record its outcome before taking M2.

### M2. Let interrupted compositions continue beyond their introductions

**Verified gap.** The current reversal code preserves an outgoing track only while it still occupies a voice (`WorldAudioPlayer.LoopBus.Select:29–33`). At fade completion, `Tick:54` calls `Stop`, which clears the clip (`Stop:156`). A later return from a danger track starts the regional composition from zero (`StartTarget:39–40`). The same is true after explicit music mute. In a biome with repeated separate encounters, most of a five-to-ten-minute piece may never be heard. Underground/danger already share Decoherence and must retain that working same-clip gain transition.

**Player payoff.** The score develops during a longer expedition instead of replaying its opening after every fight or brief audio adjustment.

**Recommended policy.** Keep a bounded session-local bookmark per distinct registered music clip when retiring it. Restore the position when re-entering that clip's context, with a short incoming fade. Preserve the existing live-voice reversal when available. One clip shared by underground/danger must have one position, not two competing clocks. Explicit mute stops output immediately and queues no old actions; unmuting may resume only the *currently eligible* clip. Do not save the score in gameplay saves or resume a stale previous-region cue. Clear bookmarks on disposal/new audio lifetime. Use at most the existing two music voices.

**Native/live gate.** Unit tests cover completed fade versus immediate reversal, shared-clip aliases, mute with/without a context change, missing clip, disposal and same-context F5/F6. Fresh actual Play must allow the real regional stream to advance, enter and resolve the authored visible combat encounter, wait through the four-second release, and observe the regional stream return near its previous nonzero position. Existing combat input route is suitable; failure to survive remains a failed receipt. A real audio-menu mute/unmute control can separately prove the simpler continuation path. Do not advance `timeSamples` from the audit to manufacture success.

**Risk.** Compressed streaming seeks require native observation. Initial load or a destroyed clip must fail quietly, and new music must not be pinned into an unbounded cache.

**Prompt.** Preserve session-local music progression through complete interruptions and current-context mute/unmute without changing saves or the source count. Test real streamed continuation in play after the native counterchecks; preserve current combat mix behavior and the same-clip underground/danger case.

### M3. Give the canonical regions distinct musical priorities

**Verified gap.** Sodden and Grovelands currently receive exactly the same composition as Spread. Overwrit receives the Beating composition, and the exposed Stump surface receives the same score as every generic cave. This is a content gap in a deliberately small first soundtrack, not a missing generation system.

**Existing lore anchors.** `Assets/Scripts/Gameplay/World/Map/WorldMap.cs:22–40` describes Spread as recovered river country, Sodden as the flood that never drained, Beating as exposed salt/ruins, Grovelands as Choir country, Overwrit as faint rather than empty, and Stump as the petrified god-tree. [FELLING-WORLD-DESIGN.md](FELLING-WORLD-DESIGN.md) elaborates those places; Grovelands is grown rather than manufactured, so a routine smith/workshop score would contradict it. These are score associations, not claims that an unseen choir or instrument-bearing NPC is physically audible.

| Region | Candidate musical role, to audition | Reliable selector | Important limit |
| --- | --- | --- | --- |
| Spread | Retain the existing exploration identity as the familiar baseline | Current map tile biome | Do not rescore every chunk |
| Sodden | Sparse, low-register, patient piece with space for water and animals | `BiomeType.Sodden` | No continuous rushing water implied by the score |
| Beating | Retain the existing dry identity while auditioning contrast | `BiomeType.Beating` | Do not infer heat damage or combat from music |
| Grovelands | Restrained sustained vocal/organic harmonic texture, if a licensed composition is actually suitable | `BiomeType.Grovelands` | Nondiegetic score; no fake Choir creature location |
| Overwrit | Lower musical density and longer intentional breathing room | `BiomeType.Overwrit` | Quieter policy is deliberate; no manufactured hidden event |
| Stump | A sparse monumental surface identity distinct from generic depth | Surface `BiomeType.Stump` | Underground remains depth-specific; no ending-spoiler score |

**Recommended scope.** First implement Sodden's identity and one contrasting Grovelands identity, retaining safe fallbacks for legacy biomes. Choose any further region split only after hearing those two in actual play. Metadata names are not evidence that a piece fits. New compositions must have verified distribution rights, source and output hashes, full credits, original/modified-duration declarations and streaming import settings. The existing artist library's [current usage page](https://www.scottbuckley.com.au/library/using-this-music/) confirms attribution-based licensing for its music library while excluding copyrighted-theme orchestral remixes; every new track still needs its own page checked. No Caves of Qud recording is licensed merely because it is the stylistic reference.

**Native/live gate.** A fresh isolated route visits real generated Spread, Sodden and Grovelands zones; disclose any initial travel shortcuts. Subsequent native movement, settings, F5/F6 and source observation run normally. Capture each actual imported clip identity and sampled output, with a separate human listening note before calling the selection artistically satisfactory. Do not classify a passing nonzero-PCM test as a musical-quality verdict.

**Prompt.** Select and license a small contrasting regional score palette, map it only to current known region/depth, preserve legacy fallbacks and danger authority, execute binding/policy tests and audition the imported compositions in their real generated regions before expanding further.

### M4. Add more than one eligible composition to long expeditions

**Verified gap.** `WorldAudioLibrary.AddMusic` overwrites the existing entry for an ID; `WorldAudioPlayer` loops one entry forever. Catalog rows alone cannot produce a playlist, and assigning every existing track to every biome would erase their intentional identities.

**Player payoff.** Staying in one region or revisiting it does not mean hearing one composition on indefinite repeat.

**Recommended scope.** Add a small ordered/weighted list of eligible *complete* compositions for the primary exploration identities, with an explicit last-played rule preventing immediate repeats when two valid choices exist. Selection should occur only at the end of a composition or an intentional new-context transition, never per turn/cell. Use a bounded presentation-only cursor or PRNG; do not consume simulation RNG or reseed on each same-biome chunk/load. Missing entries must skip deterministically and a one-entry catalog must still work. Preserve M2's clip continuation and define whether a temporarily interrupted completed track advances the list exactly once.

**Live gate.** At least one fresh actual game must reach a natural complete track boundary and observe the next eligible streamed clip; a synthetic short-clip fixture and a directly manipulated playhead do not prove this. Ephemera is approximately 5m38s, so a real long-enough observation requires a dedicated runner deadline beyond the existing launcher's 270 seconds (`Assets/Editor/Scenarios/ReferenceGladeNativeBatch.cs`, deadline assignment). The player can remain idle safely without advancing simulation; no accelerated playback, direct music calls or clock grants are needed. Other acceptance routes may observe only entry selection, but must say so.

**Risk.** More full compositions increase download size. Preserve two streamed music sources, avoid per-frame catalog work, and report actual new compressed bytes rather than adding an arbitrary music quota.

**Prompt.** Extend music entries into bounded per-context composition lists, preserve full pieces and progression, forbid immediate repeats and simulation-RNG use, then watch one natural playlist handoff in a fresh actual game before claiming this iteration complete.

### M5. Let the environment carry deliberate quiet passages

**Verified gap.** Every selected music source has `loop=true` and restarts its composition indefinitely. Apart from settings, transitions, missing resources and a dead/absent actor, there is no intentional musical silence. The now-richer environment has few opportunities to occupy the foreground by itself.

**Recommended policy.** After a complete exploration composition, use a short bounded breathing interval before the next one. Keep ambience and grounded action sounds active. The initial tuning could be roughly 12–25 seconds in populated/green exploration and longer in Overwrit, but these are audition starting points, not source-derived correct values. Derive only from current known context. Do not let ordinary movement restart the silence countdown, do not accumulate backlogged cues in menus/mutes, and do not suppress the typed combat-action focus feedback. Define danger interruption and post-danger return explicitly so a distant seen threat does not permanently eliminate all quiet passages.

**Live gate.** Build on M4's complete natural boundary observation in a *fresh* run for this iteration. Record music fade/end, an interval with zero music voices but live ambient/source output and ordinary input, followed by an eligible music return. Muting music must remain indefinite until the user raises it; it must not be confused with a scheduled intermission. Use actual real-time waiting, not a direct scheduler tick.

**Risk.** Silence must read as intentional rather than broken loading. Report the schedule/cue state in diagnostics, not a visible countdown or intrusive UI. Subjective interval length needs listening; unit tests verify state and bounds only.

**Prompt.** Add bounded intentional music intermissions after complete pieces, preserve independent environment/action playback and current mute meaning, then validate and audition a natural silence-and-return passage in actual play before the next iteration.

### M6. Let inhabited places differ without inventing an invisible crowd

**Verified gap.** Music context has no settlement identity. Reliable authored data already exists: `WorldMap.GetPOI` (`WorldMap.cs:146–149`), `PointOfInterest.Type/Name/Profile` (`PointOfInterest.cs:39–49`) and current cell `IsInterior` (`Map/Cell.cs:50`). `WorldMapAuthoring.cs:211,223` names Morrowfast/Stillcord and Sumphold/Boatyard, and village construction explicitly marks interior cells (`Generation/Builders/VillageBuilder.cs:217–223`). These selectors do not establish that anyone is currently working, singing or talking.

**Recommended scope.** A small inhabited-place musical profile may apply only after actual arrival in a known surface village, with a slower/more restrained mix under shelter. Keep the bog and adjacent wilderness on the Sodden exploration profile. Use actual POI type/profile and current zone, not guessed settlement strings or remote faction reputation. Recheck source data only at existing context boundaries. Start with Morrowfast versus its neighboring field and Sumphold versus its wetland. Do not add diegetic chatter, anvils or kitchen work merely because a village profile or tool exists; those require the separately audited real work predicates.

**Live gate.** Use the established actual resident/door input route to cross a real inhabited interior threshold and leave into the neighboring field. In Sumphold, disclose any route travel setup and keep all local keys/gameplay live. Confirm correct score choice, gradual transition, world-menu continuity and return to wilderness; record source and listener cells. A hidden/unvisited remote POI must have no effect. Choosing a village score is not a claim that the village economy or population changed.

**Risk.** Additional themes can fragment compositions if every doorway restarts them. Preserve M2/M4 progression; use shelter for gain/eligible-next-piece policy rather than resetting on each threshold. Defer a profile if no suitable licensed piece is found instead of shipping an unrelated cheerful town loop.

**Prompt.** Connect a restrained inhabited-place score to actual current authored village/profile and shelter, preserve regional wilderness and composition continuity, refuse remote POI influence, then test the real settlement-to-field transition before broadening the profile map.

### M7. Smooth positive volume adjustments while keeping mute immediate

**Verified gap.** The menu changes music/ambience gains in 0.1 steps (`PauseMenuController.ChangeAudio:286–295`). Loop gain targets smooth changes in *catalog entry gain*, but the preference multiplier goes straight into source volume each frame (`WorldAudioPlayer.LoopBus.Tick:52–53`). Thus positive menu adjustments are discrete changes even while a loop plays. This is a lower-priority polish candidate; no audible click was reproduced by this read-only audit.

**Recommended scope.** Interpolate only the audible positive music/ambience preference multipliers over a brief bounded real-time window. Display/store the user's exact selected value immediately; explicit zero must still silence immediately. Do not delay action refusal, change the saved value, adjust pitch, introduce a mixer asset or add voices. Keep the iteration 4 combat envelope as an independent multiplier, so menu smoothing and combat recovery compose rather than reset one another.

**Live gate.** Open the actual gamepad audio page, adjust music and ambience independently while real imported loops play, and observe the source gains traverse the interval without restart or overshoot. A zero setting must immediately remove audible output; raising it follows the current-context continuation rule. Add a listening comparison if making a perceptual click claim. Saved preferences and prior defaults must restore exactly after the isolated route.

**Prompt.** Smooth only positive loop-volume adjustments with a bounded presentation envelope, preserve immediate mute and exact preferences, test its composition with combat focus, then exercise the actual controller settings page in a fresh game and record the gain/playhead behavior.

## Boundaries and evidence standards for every selected iteration

1. Save the specific prompt and intended audible change before code. Run native RED, implement one bounded change, run native counterpart/regression checks, then run a fresh actual game test. A shared final smoke test cannot substitute for the requested test after each iteration.
2. Never count the prior material/phrase/work gains again. The upcoming combat-envelope iteration already owns temporary action focus; the proposals above extend music identity/progression rather than duplicate it.
3. Keep 16 world-audio sources, at most two streamed music and two streamed ambience voices during transitions, no new per-frame world scans, no world-generation/RNG calls from audio and no changes to gameplay clocks/saves.
4. Existing local scans are bounded; a new POI/region/day-band selector must use current cached authority on the existing turn/zone/resume boundary. Music playlist/bookmark work is fixed-size presentation state, not an all-world soundtrack search.
5. Avoid `Resources.Load`, LINQ, growing collections and catalog parsing from the per-frame player update. New music loads as streamed assets during the existing library lifetime; report file size and actual profile observations. Native editor evidence is not physical Steam Deck performance.
6. Source existence, cue acceptance, a nonzero sample and a musically satisfying result are different claims. Keep specific imported-playback observations, sampled PCM limitations and subjective audition notes separate. Full-song transitions require enough real elapsed time; do not relabel a shortened synthetic check as a natural composition ending.
7. Every new composition needs a verified primary source page and license, exact attribution, preserved notices, download/prepared hashes, declared changes, importer checks and packaged credits. Existing [AUDIO-ASSET-RESEARCH.md](AUDIO-ASSET-RESEARCH.md), `Tools/Audio/README.md` and the shipped credits establish the current process. No new assets were sourced or licensed by this audit.

## What was deliberately not recommended as an immediate fix

- There is already a two-second equal-power crossfade, four-second threat release and same-clip danger-gain ramp. Rewriting those without an observed issue would be refactoring, not new player depth.
- There is no evidence here of a broken streaming decoder, focus-resume bug or need for additional voices. Do not invent platform fixes without a failing native/player observation.
- Day band already changes the environmental bed. A separate musical restart every 300 ticks (about 30 player actions; `Gameplay/Turns/WorldClock.cs:39–50`) risks making the introduction problem worse. A later next-piece eligibility rule can use the real band, but it should not displace the stronger continuity/region work above.
- Indoor silence is not inherently a defect. Real timber/stone/water details are already justified by local sources; a room without those sources does not require a fake crowd or a generic looping hum.

Sweep evidence: static reads of the cited code/catalogs/docs; ffprobe metadata for three shipped music files; official Unity 6.3 API and composer usage pages checked on 2026-10-11. No Unity invocation, live sound test, production edit or musical-quality verdict was performed in this sweep.
