# Portable utility audio pipeline (iterations 10–14)

Status: repository pipeline installed with iteration10. Saved missing-module RED preceded implementation; the offline suite passes **14/14**. All five cumulative stages have separate full-decode receipts; runtime import proceeds sequentially after each behavioral RED. See `Docs/AUDIO-POLISH-20.md` for actual native and Play evidence. Offline validation alone does not establish listening quality or physical Steam Deck output.

## Repository installation

The five pipeline files live in `Tools/Audio/`:

- `utility_audio_sources.json`
- `prepare_utility_audio.py`
- `verify_utility_audio.py`
- `test_utility_audio.py`
- `README-Utility.md`

The scripts reuse the existing unmodified sibling `prepare_world_audio.py`, `prepare_place_audio.py` and `verify_world_audio.py`. Requirements remain Python 3.9+, NumPy, ffmpeg/ffprobe with libvorbis, and curl for a missing pinned archive. Current approved bytes were reproduced with ffmpeg 8.0.1. A different encoder build is allowed to fail the byte pin; it must not silently replace a recording. The complete independent spec can be stored in Tools at iteration 10; only selected rows enter runtime metadata. No future-phase clip, credit row or cue enters the selected Assets stage.

## Sequential Assets copy map

`staged/<phase>/copy-map.json` is the exact cumulative resource list with per-file SHA-256, destination and copy policy. `copy-maps/<phase>.json` adds the five Tools paths and preparation/validation evidence destinations. Only those explicit entries are eligible for copying after the parent observes the corresponding code RED and authorizes import. Do not recursively copy this entire handoff directory.

| Through | Newly introduced Ogg files under `Utility/` | Cumulative clips | Compressed bytes | Prepared mono PCM float bytes |
|---|---|---:|---:|---:|
| 10 | `water_fill_01.ogg`, `water_pour_01.ogg` | 2 | 51,727 | 636,684 |
| 11 | `cook_prepare_01.ogg` | 3 | 64,175 | 760,320 |
| 12 | `brew_prepare_01.ogg` | 4 | 73,881 | 848,660 |
| 13 | `handle_soft_01.ogg`, `handle_wood_01.ogg`, `handle_metal_01.ogg` | 7 | 102,238 | 1,103,104 |
| 14 | `harvest_mineral_01.ogg` | 8 | 111,940 | 1,173,424 |

Every phase includes cumulative `utility-catalog.json`, `utility-provenance.json`, `UTILITY-CREDITS.txt` and their `.meta` files plus `Utility.meta`. Each selected Ogg has its own `.meta`. Metadata text/JSON replaces only the preceding utility metadata; existing clips and all existing metas must be byte-identical and retained. Phase 15 introduces no recording: physical documents reuse the original `Audio/World/Sfx/page` cue.

These files never replace `World.meta`, `Music.meta`, an earlier catalog/provenance/credits file, `Licenses`, or any prior recording. The existing full CC0 legal text is checked rather than duplicated. The original 73 are hashed against all three prior provenances before and after preparation. The full five-source-palette packaging validator remains responsible for the eventual whole-game union, including independent regional music.

## Prepare from the repository

Choose an empty staging root outside `Assets/Resources/Audio/World`. All paths are caller-supplied; scripts contain no local workspace/cache literals. Example from the repository root:

```sh
python3 Tools/Audio/prepare_utility_audio.py \
  --cache /path/to/pinned-source-cache \
  --output /path/to/stage/10 \
  --through 10 \
  --baseline-world Assets/Resources/Audio/World
python3 Tools/Audio/verify_utility_audio.py \
  --root /path/to/stage/10 --through 10 \
  --baseline-world Assets/Resources/Audio/World \
  --report /path/to/10-asset-validation.json
```

For each next phase, set `--through` and output to that phase and add `--previous /path/to/stage/<previous-phase>`. This preserves all existing clip bytes and Unity identities while adding only the next recordings. `--previous Assets/Resources/Audio/World` may instead use the already imported preceding phase. Run validation again against the actual integrated world with `--root Assets/Resources/Audio/World`; the validator requires the exact selected Utility set, so using an earlier `--through` after a later phase is deliberately rejected.

The preparer rejects overlap with baseline Assets, changed cached archives/members, unsafe namespace paths, altered earlier clips, different existing identities, output aliases and an output containing future clips. It neither deletes future assets nor repairs tampered approved recordings automatically. Preparation may leave incomplete staging output after an error; do not import it unless its independent validator passes.

To execute the portable offline suite after installation:

```sh
COO_AUDIO_SOURCE_CACHE=/path/to/pinned-source-cache \
COO_AUDIO_BASELINE_WORLD=/path/to/repo/Assets/Resources/Audio/World \
python3 Tools/Audio/test_utility_audio.py
```

Outside the repository, put the existing repository `Tools/Audio` on `PYTHONPATH`. The suite creates and removes only temporary staging folders, with actual source/decode positives plus corruption, future-phase, changed member, duplicate GUID, symlink, altered credit/catalog/importer and preserved-byte counters.

## Evidence and bounds

`portable-red.txt` records the initial missing-module error; `portable-first-green.txt` records the initial ten tests; `portable-green.txt` records the final fourteen. `10-asset-validation.json` through `14-asset-validation.json` re-decode each actual cumulative stage and include all prior hashes, selected output hashes, signal measurements, metadata checks and budgets. `staged-summary.json` is only a convenient summary, not a substitute for those full receipts. The baseline license hash and source-spec hash are recorded independently.

Sources remain the original approved CC0 Tinysized SFX by Vehicle (Jan Schupke) and 80 CC0 RPG SFX by rubberduck. Full original source/archive/member hashes, transformation descriptions, cue gains and notices remain in the spec/provenance/credits. WaterDrink is explicitly a lower-gain alias of the water-pour recording, not a new human gulp recording. No procedural music, synthetic replacement or new musical composition is included.

The integer PCM totals count prepared mono frames × four bytes; Vorbis decode padding is checked separately and per-file receipt values show actual decoded size. No subjective mix, speaker, Steam Deck, native import, gameplay timing or successful gameplay-action claim follows from these numerical checks. Parent-owned native tests and live routes remain required for each phase.
