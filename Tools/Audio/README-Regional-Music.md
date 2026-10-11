# Regional music preparation and verification

This independently licensed expansion adds complete Scott Buckley compositions:

| Iteration | New composition | Context |
|---|---|---|
|16|Signal to Noise, author-supplied No Piano Melody mix|Sodden|
|16|Cirrus|Grovelands|
|17|Meanwhile|Additional Spread/organic sequence choice|
|19|Hiraeth|Installed authored Morrowfast preparation town|

The independent `regional_music_sources.json` pins each official download URL, original size/SHA256, complete decoded frame count, author/page/license/credit, supplied mix, context/gain and rollout iteration. Full CC-BY4.0 attribution and the complete existing license accompany the game. No Caves of Qud recording is included. These files establish provenance and transformations, not subjective listening quality or artist endorsement.

## Portable preparation

Requirements: Python3 with numpy, ffmpeg/ffprobe, curl. All paths are explicit or relative to this tool; source caches stay outside Assets. From the repository root, choose a local cache and an output staging directory, then run:

```sh
python3 Tools/Audio/prepare_regional_music.py --cache "$COO_MUSIC_CACHE" --output "$COO_MUSIC_STAGE" --through-iteration 16
```

Repeat with17 or19 only when that stage is authorized. Catalog, provenance and credits are cumulative through the selected stage. Earlier waveforms are byte-stable with the same pinned ffmpeg version; existing `.meta` files are preserved. When incrementally importing prepared assets, copy only the newly authorized clip+meta; update the three cumulative text files while retaining their existing GUIDs. Never replace an existing World/Music folder meta or old soundtrack assets. Existing base, place, atmosphere and utility catalogs remain separate.

Preparation downloads and hashes the reviewed full MP3, decodes stereo44100Hz float PCM to a temporary disk file, computes peak/RMS in ten-second chunks, applies attenuation only when needed to reach a .75 peak ceiling, and encodes complete Ogg Vorbis at quality4. It preserves the author's composition and timing; it does not excerpt, normalize loudness, add instruments or boost quiet masters. The decoded Ogg must retain the exact source frame count and remain below .99 peak. Individual tracks are limited to8MiB; the full expansion to32MiB. Importers are streamed stereo, background-load enabled, preload disabled and normalization disabled.

## Verify existing assets without preparing them again

```sh
python3 Tools/Audio/verify_regional_music.py --root Assets/Resources/Audio/World --report Docs/Verification/AudioPolish20/19/music-assets.json
```

For a staging directory, pass its path with `--root` and point `--license-file` to the repository's existing `Assets/Resources/Audio/World/Licenses/CC-BY-4.0.txt`. The validator never downloads, encodes, changes a clip or enters Unity. It compares the independent spec with cumulative catalog/provenance/credits, output bytes/hashes, actual probe rate/channels, exact decoded full-composition frames, signal/headroom, GUIDs and importer settings, plus the complete license. It owns only the selected regional rows; the final delivery validator checks the union with older palettes.

Receipts use the established `passed`, `assetCount`, `audioBytes`, `findings`, `assets:[{path,errors,...}]` schema. `subjectiveListeningVerified:false` and `unityImportVerified:false` are explicit. Keep these independent from actual Unity native/import/live receipts. The validator can reject corrupt/missing metadata without manufacturing a success result.

```sh
python3 -m unittest discover -s Tools/Audio -p 'test_regional_music*.py' -v
```

Tests use temporary files and explicitly mocked decoding for artifact adversaries. They validate stage ordering, exact source attribution, source hashes, bounded transforms, stable metadata, full catalog comparison, duplicate rows, independent metadata, wrong actual rate/channels and malformed/missing metadata/license. They do not establish audio playback, physical output or listening quality.

## Credits and video use

`REGIONAL-MUSIC-CREDITS.txt` includes each exact author attribution line, supplied mix, primary source URL, CC-BY4.0 link and transformation description. Scott Buckley's library uses YouTube Content ID; video creators should include full attribution for heard compositions in their video description. In-game credit alone is not a guarantee against a video claim. The game's independent music volume/mute remains available.
