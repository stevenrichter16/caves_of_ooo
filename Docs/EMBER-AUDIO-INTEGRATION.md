# Ember Spit approved sound — game integration

Status: implemented and verified in game, 2026-09-11.49 new cases green; full baseline unchanged; native command/audio/profile acceptance passed.

## Scope and sweep

Import the approved revision3 wind/fire/impact WAVs unchanged. Playback belongs to the existing presentation coordinator; it must not resolve damage, consume simulation RNG, or extend the turn-blocking lifetime. Three matched variants preserve the approved layer gains. This is CoO-original audio, not a Qud parity feature.

| Verified source | Consequence |
| --- | --- |
| Main scene is `Assets/Scenes/Main/SampleScene.unity`, with enabled AudioListener | The old short scene path in CLAUDE.md is stale. Use the actual scene; no listener/camera replacement. |
| WorldFxCoordinator accepts copied spell outcomes after advancing old work | Start sound only after accepted presentation; update existing audio before accepting newly queued casts. |
| Native renderer uses filtered/deduplicated path and `.22 + .025 * cells` contact | Expose the actual selected renderer's contact time/cell instead of guessing from unfiltered input. Sprite and ASCII fallbacks have different timings. |
| Native long-frame recovery changes only its presentation delta | Audio contact observation must use the same native delta, while hard cleanup remains wall-clock bounded. Sound cannot block a turn. |
| Ember native impacts accept only copied targets at the final physical path cell | Ambient reactions, multiple occupied cells and duplicate copied targets must not create extra crushing hits. Recheck current visibility before contact playback. |
| Audio assets have a long natural decay beyond visual completion | Let a completed cast's sound tail finish independently; cancel on zone/load/hidden presentation/off/disposal. |
| Main scene has no game-specific sound player; settings panel already has effects controls | Add one bounded coordinator-owned player plus volume preference/slider. Use centered2D presentation to preserve the approved mix and avoid mixing native XZ coordinates with the main listener's2D camera coordinates. |

## Implementation plan

1. Preserve original edit targets, source hashes and approved audio. Run baseline before production edits.
2. Add/run failing tests for resource readiness, actual contact timing, matched variants, no extra impacts, cancellation, volume/speed, bounded overlap and no gameplay waits.
3. Import nine byte-identical WAVs (three wind, three fire, three event-relative impacts) using PCM/decompress-on-load. Pool at most four casts/12 sources. Start wind/fire at a common DSP time; trigger impact once when presentation reaches its actual contact. Audio-thread playback continues independently between frames; no claim of sample-perfect visual alignment across an Editor stall.
4. Run focused/full checks, counterchecks and cold-eye review. Exercise a real accepted Ember command in isolated Play, verify actual mixer output and lifecycle, profile bounded playback, then reopen ordinary Unity.

## Performance and honesty

Read `PERF-FOUNDATION.md`: prepare clips and source pool before casting, no per-frame LINQ/collections/component discovery, bounded four-voice pool, no added turn wait. Preserve the scene, original camera and unrelated working tree. Retain a raw-wall timeout even if presentation speed is slow. Audible hardware/headphone output and subjective listening are not established solely by unit tests; approved source audio is already user-reviewed.

## Review / implementation log

- Read CLAUDE.md, Unity skill, audio sources/manifest, coordinator, all three visual backend clocks, bus/target contracts, main scene listener and performance guidance. Current ordinary Editor is idle, not in Play, with one clean main scene. Preserve and ordinarily quit for requested headless test workflow; no forced termination.


## Implemented behavior

`WorldFxCoordinator` owns `EmberSpitAudioPlayer`, prepares its nine clips/twelve sources at zone binding, and starts matched wind/fire layers at one DSP timestamp. Each accepted Ember sequence gets at most one event-relative impact, on the selected visual backend's actual contact cell/time. The player's cosmetic seed chooses one of three matched variants without simulation RNG. Natural tails have no playback handle and cannot hold a turn. Overlap is bounded to four voices, with equal layer gain divided by the number of active voices; the fifth cast is refused for audio only.

F10 now includes a persisted sound-volume slider. Zero volume, effects Off, hidden world, zone transitions, coordinator cancellation/disposal and native-surface loss stop the relevant sound. The audio observer follows native hitch recovery for contact, with an independent five-second wall timeout. AudioSource pitch follows animation speed up to Unity's positive pitch cap of3; the visual speed may still reach4. Default speed1 preserves the approved pitch and layer proportions. Wind/fire clips last1.1 seconds, impact .7 seconds. A15ms shared DSP scheduling lead avoids starting the two bed layers at different mixer times. Exact sample-level alignment to rendered frames across Editor stalls is not claimed.

Official API checks: [PlayScheduled](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AudioSource.PlayScheduled.html), [pitch](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AudioSource-pitch.html), [import settings](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AudioImporterSampleSettings.html). Import is scoped to `Assets/Resources/Audio/EmberSpit`: PCM, decompress-on-load, preserve48kHz, preload enabled, no background loading or mono renormalization.

## TDD and adversarial receipts

- A00c: pre-change baseline11386 total,11355 passed,31 failed, no compiler errors. Existing failures are retained as the comparison baseline, not declared green.
- A01: genuine compile RED: missing `EmberSpitAudioPlayer`; no tests executed and no XML pass/fail claim.
- A02: implementation compile error in a settings accessor caused by an overbroad text replacement; fixed surgically before rerunning.
- A03:28/28 audio contract cases passed, no compiler errors.
- A04:49/49 audio cases passed:28 contract +21 dedicated adversarial cases. The latter pin malformed contacts/deltas, hierarchy loss/recovery, death/resistance snapshots, visibility, listener pause, null/disposed lifecycle, importer configuration, allocation-free steady ticks, and native filtered-route integration. These21 passed on their first run: regression pins, not21 discovered bugs.
- Imported WAV hashes:9/9 byte-identical to revision3 manifest.14 new asset/script GUIDs scanned against all Assets metas, no collisions. Receipt: `Verification/EmberAudioIntegration/asset-checks.json`.

## In-phase review

- ⚪ This is CoO-original audio. No Qud sound parity claim or Qud source dependency.
- ⚪ Centered2D playback deliberately preserves the approved mix and avoids distance attenuation from the zoomed-out camera. Positional audio is outside this Ember integration.
- ⚪ Concurrent casts share headroom. Default one-cast gain remains exactly the approved source gain; this is not a compressor or resynthesis.
- ✅ Native acceptance established actual source playback and nonzero mixer output, a silent mute countercheck, exact command/impact counts, and bounded pool/tail behavior. Hardware listening remains outside the measured claims.
- ✅ Full-suite comparison:11404/11435 passed; the31 baseline failures have identical names and messages. No added or changed failures.

## Changed files

- New runtime `EmberSpitAudioPlayer.cs`, scoped Editor `EmberSpitAudioImporter.cs`, nine Resources WAVs and metas.
- `WorldFxCoordinator.cs`: audio ownership, backend contact wiring, cancellation; `NativeSpellFxRenderer.cs` and `SpriteSpellFxRenderer.cs`: exact accepted contact metadata.
- `SpellFxSettings.cs` / `SpellFxSettingsPanel.cs`: persisted volume preference and F10 slider.
- `EmberSpitAudioTests.cs` / `EmberSpitAudioAdversarialTests.cs`:49 cases.
- `EmberAudioNativeAudit.cs`: explicit isolated real-command/mixer/profile scenario; menu under Caves Of Ooo → Scenarios → Magic. Fixture resets cooldown/owned target between commands, does not advance ordinary turns, and makes no keyboard-input or subjective-listening claim.


## Final verification and cold-eye review

- A05 full:11435 total,11404 passed,31 failed, zero compiler errors,205.125 seconds. `full-comparison.json` reports no new, resolved or message-changed failures. All49 added cases pass, including the allocation assertion with the new profiler marker present.
- N01 native: exit0, no compiler errors, `NativeSaveIsolation` cleanup exit0. Main scene boot, normal Ember skill command routing and the existing coordinator were used.31 actual commands included30 audible casts and one muted countercheck.30 accepted voices produced30 impacts; all natural tails cleared. The pool stayed at12 AudioSources. Wind/fire and impact sources were observed playing with progressing sample positions; live mixer peak was0.43056947, muted peak0.
-61.001 seconds of native repeated-cast profiling recorded18223 frames/samples. `COO.EmberAudio.Update` mean2.3455µs, max39.125µs. These are this Editor run's audio-update measurements, not whole-game or build-performance guarantees. The separate steady-clock test measures0 managed allocations over1000 warmed updates.
-36 decoded source recombinations cover native and ASCII route contacts plus delayed-contact controls; all remain unclipped with4× oversampling. Worst peak−2.67135dBFS. Imported source files retain their approved hashes.
- Cold-eye symmetry: begin/clear/zone/dispose paths paired; native cancellation leaves unrelated fallback voices alone; normal visual completion leaves sound tails alone. Timing metadata comes from the selected renderer and deduplicated native path. The new sprite contact property's XML comment initially inherited the Play method's description; moved the method comment back and gave the property its own contract. No behavior change.
- Cross-feature/negative branch review:21 hypothesis/adversarial pins cover invalid clocks/contacts, dead/resisted copied recipients, current visibility, missing hierarchy, disposal, mute/pause and native filtered-route timing. No new gameplay behavior or save serialization was introduced. Qud-parity review is inapplicable to original sound assets.
- Preservation: original main scene unchanged. Unity automatically upgraded QualitySettings serialization during verification; the exact schema-only change was checked and restored from this task's snapshot. No quality/camera changes ship.17 integration metas, including folders, were scanned against all Assets metas; no GUID collisions. The private native save root was removed and the save-isolation service restored the inherited root/last-game preference.
- Commits are not bundled with the unrelated mixed working tree. Source changes remain available for review alongside the pre-existing work.

### Can verify

The nine imported assets match the approved revision3 WAVs. Actual Unity AudioSources and mixer output worked during real command routing. Contact counts, silence when muted, independent tails, bounded pool, profile timings and cleanup are recorded in `Verification/EmberAudioIntegration/Native/1ffe9a6cc3634e39b39f9c8fc0d77ecc.json` and `N01/receipt.json`.

### Cannot verify

These measurements do not establish headphone/speaker hardware output or subjective listening comfort; the source design was approved by the user. This check uses explicit fixture cooldown resets and a durable owned recipient, not ordinary turn progression or keyboard targeting. F10's volume preference code was reviewed; no automated visual-layout or slider-drag claim is made.

- Final ordinary Editor reopened in native mode on `Assets/Scenes/Main/SampleScene.unity`; MCP reports idle, not in Play, not compiling, no pending refresh/tests.
- Ordinary reopen console contains one MCP plugin startup exception: `[WebSocket] HandleSocketClosureAsync called. Reason: WebSocket is not initialised`. MCP subsequently answers state/scene/console queries; the main scene is clean and idle. This is recorded separately from N01, which completed with zero unexpected errors. No game-audio/compiler error was reported; the plugin exception was not cleared or described as fixed.


Starter-family follow-up: all six additional approved sounds now share the common overlap budget with Ember. A source-loss RED test justified a null guard in Ember RefreshMix so a destroyed old source cannot interrupt a newly accepted other-family voice. All49 original Ember cases plus102 new audio cases pass in A06; nine approved Ember WAVs remain byte-identical. See STARTER-SPELL-AUDIO-INTEGRATION.md.
