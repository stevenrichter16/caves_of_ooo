# World audio: source research and asset preparation

Status: source research verified 2026-10-10; coordinator approved a compact three-track licensed music set plus CC0 ambience/Foley. No Unity tools are used by this asset workstream. Runtime/catalog/settings belong to the runtime agent; existing StarterSpells assets remain unchanged. Numerical signal checks do not establish subjective listening quality.

## Direction and inspected sources

Craig Hamilton's [official composer site](https://www.craigoryhamilton.com/) describes contemplative, cosmic and primal dimensions of his game scoring. The [official Caves of Qud soundtrack](https://store.steampowered.com/app/1247760/Caves_of_Qud_Soundtrack/) credits Hamilton and advertises32 science-fantasy tracks. These establish the mood reference only; no Qud soundtrack file, composition, melody or sample is copied or licensed here.

Our direction: spacious and restrained, organic recordings blended with an uncanny electronic background; long passages that leave room for reading and thought. Avoid a heroic orchestral loop or music reacting to every turn. The three proposed tracks are actual independently composed Scott Buckley works; no procedural tones are being presented as professional licensed music. Their fit below is a selection inference from the composer's descriptions, pending listening in the actual mix.

| Asset | Primary source / verified grant | Intended role |
|---|---|---|
| Shadows and Dust — Scott Buckley | [Composer track page](https://www.scottbuckley.com.au/library/shadows-and-dust/), explicitly CC-BY4.0, commercial projects permitted | Sparse dry/open exploration; synth/wind/string texture |
| Decoherence — Scott Buckley | [Composer track page](https://www.scottbuckley.com.au/library/decoherence/), explicitly CC-BY4.0, commercial projects permitted | Caverns, ruins and uncanny exploration; evolving synth/viola |
| Ephemera — Scott Buckley | [Composer track page](https://www.scottbuckley.com.au/library/ephemera/), explicitly CC-BY4.0, commercial projects permitted | Organic/wet/refuge passages; cello and transformed Australian beach recordings |
| Kenney RPG Audio / Impact Sounds / Interface Sounds | [RPG](https://kenney.nl/assets/rpg-audio), [Impact](https://kenney.nl/assets/impact-sounds), [Interface](https://kenney.nl/assets/interface-sounds), all official CC0 pages | Small selected action/UI/footstep set, not wholesale library import |
| Birds and Wind — ambient-only version, Spring Spring | [Author submission](https://opengameart.org/node/93987), CC0; names bird sources isaiah658, syncopika, pauliuw | Natural exterior bed; choose the version without synth instruments |
| Sea and river wave sounds — RandomMind | [Author submission](https://opengameart.org/content/sea-and-river-wave-sounds), CC0, author's Vistula river recording | Water-adjacent bed; bounded excerpt with seam treatment |
| Loopable Dungeon Ambience — JaggedStone | [Author submission](https://opengameart.org/content/loopable-dungeon-ambience), CC0 | Cavern wind and water-drip bed |

## Licensing and distribution decision

The composer's [using-this-music page](https://www.scottbuckley.com.au/library/using-this-music/) expressly permits adapting music for projects including commercial use with attribution. Each selected track repeats that permission. [CC-BY4.0](https://creativecommons.org/licenses/by/4.0/) grants commercial copying/adaptation with creator credit, license link and identification of changes. Preserve the full supplied credit line for each track, source URL, license text and modification notice in the distributed resources, manifest and game credits. Encode full-length music without cutting the composition; no Qud audio is used.

The composer also asks that music not be resold in isolation/reuploaded to music streaming services and that remixes not be enrolled in Content ID. Our use is a game soundtrack synchronized with gameplay, not a separate music product or fingerprint registration. YouTube uploads can attract Content ID claims without the track credit in the video description. Supply ready-to-copy video credits and a clear independent music-mute option; game credits alone do not guarantee that a player's uploaded video will avoid claims. No blanket claim of streamer-safe music is made.

[Kenney's official FAQ](https://kenney.nl/support) permits commercial use and confirms optional attribution for its CC0 assets. [CC0](https://creativecommons.org/publicdomain/zero/1.0/) also permits adaptation and redistribution of the three independently sourced ambience works; keep all named contributors anyway. Include exact author/source/file identities rather than claiming all files are original CoO recordings.

## Preparation plan and acceptance gates (before asset import)

1. Resolve actual official download URLs and download to an isolated temporary source cache; record original bytes, hashes and archive member identities. Preserve license text. No account, purchase, contact or external message is needed.
2. Write and execute a failing asset-contract check before importing: required exact Resources paths, valid GUIDs, proper streaming/decompressed load policy, provenance/credits and bounded decoded signal/file sizes.
3. Convert only the selected subset with a reproducible script. Full-length music: stereo Vorbis with streaming import, no preload. Ambience: bounded loop beds and streaming import. Short action/UI sounds: mono, bounded duration, decompressed-on-load for low-latency playback and small total resident PCM. Do not touch existing spell audio.
4. Verify actual decode/duration/channel count, peak/RMS, finite samples, loop seam metrics and bundle budget. Keep false-case checks for missing/truncated files and incorrect importers; these metrics are engineering evidence, not a substitute for listening.
5. Publish exact resource paths to the runtime agent, then coordinator performs native import/play/mixer checks. Record any audible/subjective evidence honestly; do not infer it from signal metrics.

Files owned: this research log; selected `Assets/Resources/Audio/World/{Music,Ambience,Sfx}` clips and metadata; source/credit/license manifest; a reproducible preparation/verifier under `Tools/Audio`. Runtime agent owns `catalog.json`, runtime C# and settings. No commits or Unity imports before coordination.

## Implemented set and reproducibility

Prepared22 clips: three full-length music tracks, three CC0 ambience beds and16 CC0 Foley/UI clips. The actual compressed total is **22,906,583 bytes (21.85MiB)**. Combined short-effect decoded float PCM is **1,140,428 bytes (1.09MiB)**; long clips stream. These are offline file measurements, not a claim about Unity's total audio memory.

Music remains408.380s (`ShadowsAndDust`),618.088730s (`Decoherence`) and338.361633s (`Ephemera`), about22m45s total. The exact original track credit lines appear in `Audio/World/CREDITS.txt`, audio metadata and `provenance.json`. Original source ID3 tags were also inspected: title/artist plus album/genre/date, and composer where present; no additional source copyright notice was found. Full CC-BY4.0/CC0 texts and all three original Kenney license notices accompany the clips. Runtime owns a reachable audio-credits reader and the independent music-mute control.

Ambience resource IDs are `Audio/World/Ambience/{forest,water,cave}`. Forest is the ambient-only version from Spring Spring, with all three named bird-recording contributors preserved; cave is JaggedStone's wind/drips; water is a90s excerpt starting30s into RandomMind's Vistula field recording. Each receives a0.75s overlap at the loop boundary, producing83.032/89.250/93.560s clips. No generic synthesized tone is substituted for a sourced composition or nature recording.

Effects under `Audio/World/Sfx/`: paired `step_soft_01/02`, `step_stone_01/02`, `step_water_01/02`, plus `swing`, `hit`, `pickup`, `equip`, `door_open`, `door_close`, `use`, `ui_confirm`, `ui_cancel`, `page`. Water uses two short splashes from [rubberduck's CC0 pack](https://opengameart.org/content/40-cc0-water-splash-slime-sfx); it is a designed water-step cue, not a claim of a literal recorded footstep. Kenney's cloth handling supplies `use`, and paper handling supplies `page`; these restrained cues do not claim item-specific Foley for every action.

`Tools/Audio/world_audio_sources.json` pins every original download and selected archive member with SHA256. `prepare_world_audio.py` downloads only that set, preserves existing GUIDs, writes source/output provenance, copies author notices, and encodes with ffmpeg. `verify_world_audio.py` independently decodes the shipped files and checks importers, credits, duration/sample format, hashes, signal limits, loop jumps and disk/PCM budgets. [Unity's importer reference](https://docs.unity3d.com/6000.0/Documentation/Manual/class-AudioClip.html) informed Streaming for long clips versus Decompress On Load for short clips; all store Vorbis, music/ambience disable preload and normalize, short clips use mono and preload.

Observed asset-contract RED before import: all22 clips missing, with missing provenance/license failures (`Verification/WorldAudio/asset-contract-red.json`). First preparation check caught omitted canonical CC0 credit sentences, even though separate creator/license fields were present; fixed by retaining both. That raw check remains `asset-contract-initial-check.json`. Short Vorbis decoders expose up to one padded block beyond/before the declared granule duration; the initial exact raw-frame assertion failed. Evidence: prepared8788 / declared8788 / decoded9792 for soft-step01, and prepared5501 / declared5501 / decoded5373 for stone-step02. The corrected contract pins exact container frames for every clip, exact decoded long-clip duration, and a1152-frame bound only for short decoded padding (`asset-contract-decoder-padding.json`). No musical content is cut to satisfy a threshold.

Final offline **22/22 asset contract GREEN**, zero findings, in `Verification/WorldAudio/asset-contract-green.json`; **13 adversarial counterchecks** pair valid signal/importers with silent, clipped, nonfinite, wrong-rate, too-long, discontinuous-loop, invalid-GUID and wrong-load/compression/preload variants. Assets frozen after this gate for coordinator native verification. No Unity imports/tests or subjective listening were performed by this workstream.

## Asset ledger: passes25–44

These are20 distinct research, preparation and engineering-validation passes. They are **not20 listening passes** and do not claim an expert subjective music review. Runtime passes01–24 and coordinator passes45–50 are recorded in `AUDIO-WORLD-SOUND.md`.

| Pass | Targeted check / change | Actual outcome and evidence |
|---|---|---|
| 25 | Establish legitimate mood reference and exclude copied Qud material | Composer site and official soundtrack checked; source manifest contains no Qud audio. |
| 26 | Select sparse, nonheroic composed music with distinct roles | Three composer-described ambient works selected; actual files downloaded, compositions retained; subjective fit remains for listening. |
| 27 | Confirm each selected music grant and attribution | All three official pages explicitly name CC-BY4.0/commercial use; original credit sentences retained. |
| 28 | Check streamer/distribution implications | Official usage page reviewed; Content-ID/video-credit notice shipped; mute and reachable credits requested from runtime owner. |
| 29 | Verify exterior-bed source rather than infer from title | Spring Spring confirms a version without instruments; selected that exact file and preserved bird contributors. |
| 30 | Verify distinct water/cave source identities | Author-owned Vistula recording and JaggedStone cave-wind/drip submission both CC0; no collection-page license inference. |
| 31 | Inspect Foley license files and avoid incompatible candidates | Three official Kenney archives retain original CC0 notices; Jute's GPL footsteps excluded; rubberduck CC0 splashes selected. |
| 32 | Pin reproducible downloads and archive members | Ten source files plus every selected archive member hashed; preparation rejects source/member hash drift. |
| 33 | Bound the imported subset | Only22 clips imported; existing94 spell WAVs/catalog preserved, no whole-library bulk import. |
| 34 | Preserve complete music duration and dynamics | All source/prepared long-music frame counts equal; attenuation only for headroom, no crop or melodic substitution. |
| 35 | Prepare compact supported stereo music | 44.1kHz stereo Vorbis; actual durations and decode verified, peaks around−2dBFS. |
| 36 | Correct ambience loop topology | Tail/head0.75s overlap rotates to adjacent original samples at the repeat seam; river excerpt bounded90s. |
| 37 | Check decoded loop seams | Forest0.000329, water0.010899, cave0.000448 full-scale sample jumps; all below0.015 numerical gate. Listening remains separate. |
| 38 | Improve water-cue selection using actual envelopes | Original splash01 had33.7dB crest over RMS; replaced with shorter06/09 source events at22.6/24.4dB before preparation. |
| 39 | Prepare responsive bounded short cues | All16 mono effects below1s; trimmed exterior silence with guards, applied5ms edge fades and10ms silence pads. |
| 40 | Check decoded signal health and transient headroom | Every clip decodes finite/nonzero; no clipped peaks; short peaks approximately−9dBFS. This does not prove pleasing sound. |
| 41 | Validate long-clip importer and memory policy | Music/ambience Streaming, no preload, background loading; no giant long-clip PCM resident request. |
| 42 | Validate short-clip importer, identity and budgets | Decompress On Load/preload, unique32-hex GUIDs; total21.85MiB and short float PCM1.09MiB under32/2MiB gates. |
| 43 | Verify shipped credits and source/output provenance | Canonical credit-string omission caught/fixed; complete license files, selected member hashes and output hashes all checked GREEN. |
| 44 | Execute independent failure counters and state honest limits | 13 distinct adverse checks pass; RED and intermediate failures retained, native importer/play/listening gates explicitly outside offline proof. |

## Self-review and remaining integration gates

🔵 Source classification remains precise: independently composed licensed music, CC0 sound design/field recordings, and declared conversions. No claim of a new original CoO musical composition or Qud parity. 🔵 Q1–Q4 reviewed source/spec/provenance agreement, full music versus bounded ambience, Streaming versus short preload, and documentation/credits against actual files. 🟡 Fixed the canonical CC0-credit omission and the false premise equating short Vorbis decoded padding with removed musical frames. 🧪 Subjective hearing, final mix, native load latency, suspend/resume behavior, actual output capture and hardware feel must be reported by the coordinator/runtime audit. Suggested initial gains were sent as engineering estimates, not auditioned master levels.

## Performance/audio delivery preparation

2026-10-10: coordinator requested a packaging script only; no build or package is executed by this workstream. Adapt the prior System Depth II packager into `Tools/SteamDeck/package-performance-audio.py`, use a new performance-audio folder/archive, and preserve all older deliveries. Required inputs are explicit final HEAD, successful Linux build, final native NUnit XML and a complete `world-audio` Play report. Validate individual results against summary counts; retain skipped tests and hardware/listening limitations.

Inspected sweep: the prior packager checks ELF identity, build timestamps/output, clean production scopes, archive aliases/digests and old-archive preservation. The asset contract contains22 exact resource paths and decoded-signal evidence; source/output hashes reside in `provenance.json`, while full license texts are separate Resources files. These are source/importer checks, not independent proof of Unity's compiled resource archive. The native scenario reports `world-audio`, finalized errors and individual PASS strings; report paths must be supplied after the final run. `Village3DSettings` already accepts Steam's `SteamDeck=1` when no explicit `COO_HANDHELD` value exists; saved preferences remain authoritative. The wrapper sets `COO_HANDHELD=1` only when the caller has not supplied it.

Prepare validation-only counterchecks before implementing new receipt gates. Ship a readable `AUDIO-CREDITS.txt`, full `Audio-Licenses/`, source/provenance and verification records beside the executable without duplicating raw music files. Require all22 current asset hashes/catalog references, canonical credits and complete license files. The delivery README will explain independent music/ambience/SFX settings and Content ID/video attribution. Performance evidence is native Mac/editor evidence only; physical Deck frame rate, latency, thermals, battery, physical controller feel and subjective listening remain unverified unless separately recorded. Root reviews and executes the final packaging script.

Delivery preparation result: observed RED was the validation fixture's missing new packager module (`FileNotFoundError`) before implementation. The new script and ten receipt-counter tests then passed; final validation adds six audio-gate tests, including five temporary-copy mutations for credits, full license bytes, clip bytes, importer policy and catalog references. **16 unittest methods GREEN**, with five named mutation subcases and a restored-positive counter; no build, tar creation, package operation or Unity call occurred. The imported-script cache created by the first check was removed; tests are run with `PYTHONDONTWRITEBYTECODE=1`.

Q1–Q4 self-review: final mode is exactly `world-audio`, required source hash equals full HEAD, test/native individual outcomes match counts, skipped NUnit cases stay explicit, all22 catalog paths equal the approved set, complete licenses have pinned hashes, README copies without runtime rewriting, and source/receipt correspondence limitations remain explicit. Archive publication uses a same-directory exclusive hard link after verification so an existing archive cannot be replaced even if it appears after the initial check. New folder and archive names retain the coordinator-requested `2026-10-10` identity across midnight. 🧪 Final native evidence is complete:1359/1359 broad,232/232 focused and18/18 live audio checks. The coordinator subsequently built Linux successfully with zero errors and verified170 payload files in the115,215,140-byte transfer archive. See AUDIO-WORLD-SOUND.md and Verification/WorldAudio/Delivery for receipts and hardware/listening bounds.

Prepared command (replace the placeholders after final source commit/build):

```sh
PYTHONDONTWRITEBYTECODE=1 python3 Tools/SteamDeck/package-performance-audio.py FINAL_WORLD_AUDIO_REPORT.json \
  --source-commit FULL_FINAL_HEAD \
  --build-report FINAL_LINUX_BUILD_REPORT.json \
  --editmode-report FINAL_NATIVE_EDITMODE.xml \
  --asset-report Docs/Verification/WorldAudio/asset-contract-green.json
```
