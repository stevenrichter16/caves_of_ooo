# Native audit draft handoff

Status: authored outside Assets during baseline, then copied into Assets by root for central compilation/live verification. This pending copy is kept identical to the native audit during the source-phase correction. No passing native result is claimed here.

Integration target: `Assets/Editor/Scenarios/StarterSpellAudioNativeAudit.cs`, with an existing editor-script `.meta` copied and only its GUID changed. Launcher method: `CavesOfOoo.Editor.StarterSpellAudioNativeAudit.RunFromCommandLine`. `run_native.py` was updated by root to launch the starter audit.

Verified API requirements:

- `WorldFxCoordinator.StarterAudio` exposes `Root`, `ActiveVoices`, `AllocatedSources`, cumulative `AcceptedCount` and `ContactLayerCount`. Fixed pool: 32.
- Existing `EmberAudio` APIs remain intact. Fixed pool: 12.
- Imported starter AudioClip names end in `_pre` or `_post`, and contain their layer stem (`palms_catch`, etc.). Runtime plays the clips through AudioSources rather than renaming them.
- `COO.StarterSpellAudio.Update` is a Scripts-category profiler marker.
- Native sound success suffix counts: Hands 3; Jet 4; Surge 3 on actual push for variants 1/3, 4 for variant 2; Rime 3; Calm 3 on pacification; Rain 3 for a real watered crop. Already-peaceful Calm has 2 (no resolved overtone); blocked-push Surge has 2 for variants 1/3, 3 for variant 2 (no displacement); no-crop Rain is an accepted game cast with no audio acceptance. Rime dry refusal queues no game sequence. Surge charging_current is prefix-only in variants 1/3; the audit records the accepted variant and requires actual charging prefix playback without inventing silent suffix assets.

Audited flow:

1. Require idle native Editor, clean Main SampleScene. Start `NativeSaveIsolation`, use the ordinary new-game boot menu path. No normal save load or overwrite.
2. Find existing open floor and a crop-free radius; refuse instead of clearing authored scenery. Place an owned durable creature, crop and optional solid blocker. Starter skills must already exist; only Rain may be learned for the fixture.
3. Route seven real successful commands (six new sounds plus preserved Ember), checking actual HP/status/movement/crop/frozen-water outcomes and normal command cooldown/FX queue behavior.
4. Observe normal coordinator audio, DSP sample progress, every eligible suffix stem plus observed prefix stems and the actual selected variant, nonzero mixer output, exact accepted/contact increments and natural tail completion. Never drain the spell queue or invoke a presentation player's `Play` to manufacture a passing cast.
5. Run seven matching muted real commands, plus explicit Rime water-only/dry, Calm already-peaceful, blocked Surge and no-crop Rain cases.
6. Profile 61 seconds of mixed real casts, spaced to allow natural tails; require all normal suffix counts and fixed pools throughout. Sort/report max as well as mean marker time.
7. Restore runtime settings/listener state. Finish via save isolation so teardown cannot reach normal saves. Emit JSON under `Docs/Verification/StarterSpellAudioIntegration/Native/`.

Self-review: fixed stale `current` row when entering profile stage; confirmed zero `SpellFxBus.Drain` / `WorldFx.Play` / player `.Play` bypass calls. Control cases use the same owned actor/target fixture with one explicit condition changed. Every row records expectations and measured behavior. The draft checks source playback and mixer data, not subjective timbre, physical headphone routing or UI keybindings.

Remaining verification: root must compile after baseline/RED gates, then run native acceptance. Source-name and counter contract compatibility must be confirmed against final engine/export output. A failed natural lane search or source playback sample is a loud audit failure, not a skipped/pass result.
