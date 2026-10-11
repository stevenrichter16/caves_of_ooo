# Third atmosphere palette

This additive set provides five local detail families and five work-action
families, two distinct short takes each. It does not modify either previous
catalog, pipeline or any of the original 53 clips. See
`Docs/AUDIO-ATMOSPHERE-ASSETS.md` for the saved prompt, plan, primary sources,
selection corrections and verification receipts.

Requirements: Python3, numpy, ffmpeg/ffprobe, curl and bsdtar with 7z support.
The preparation reuses the existing tested mono conversion, guarded trim,
filter and metadata helpers without editing them. Originals remain outside the
repository; every download and selected archive member is hash-pinned.

```sh
PYTHONDONTWRITEBYTECODE=1 python3 Tools/Audio/prepare_atmospheric_audio.py --cache /tmp/coo-atmosphere-audio-sources
PYTHONDONTWRITEBYTECODE=1 python3 Tools/Audio/verify_atmospheric_audio.py --report /tmp/atmospheric-audio-verification.json
PYTHONDONTWRITEBYTECODE=1 python3 Tools/Audio/test_atmospheric_audio.py
PYTHONDONTWRITEBYTECODE=1 python3 Tools/Audio/test_atmospheric_audio_adversarial.py
```

Only `Atmospheric/`, `atmospheric-catalog.json`, `atmospheric-provenance.json`,
`ATMOSPHERIC-CREDITS.txt` and their metadata are written under Audio/World. All
clips use mono 44.1kHz Vorbis, DecompressOnLoad, preload on, normalizer off. Detail
phrases must be at most 3.5s to leave space within the shared minimum 4s cadence;
actual longest is 2.522s. The approved budget is 2MiB compressed and 4MiB short
float PCM. Existing GUIDs are preserved when rebuilding.

Leaves are organic recordings used as designed reed/harvest Foley; they are not
advertised as recordings of actual windblown reeds. Forge/repair cues are for
committed work only. Source identity, decode checks and waveform measurements
cannot prove pleasant mix, spatial correctness or live gameplay routing. Root's
native iteration reports are separate evidence; physical Deck listening remains
unverified unless explicitly recorded there.
