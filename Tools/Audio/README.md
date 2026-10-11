# World audio preparation

These tools rebuild the small licensed set described in `Docs/AUDIO-ASSET-RESEARCH.md`. They never invoke Unity or modify the existing StarterSpells set or runtime catalog.

Requirements: Python3, numpy, ffmpeg/ffprobe and curl on PATH. Authoritative URLs and SHA256 hashes are pinned in `world_audio_sources.json`. A changed download fails rather than silently accepting a replacement; review the author/license again before updating that hash.

From the repository root:

```sh
python3 Tools/Audio/prepare_world_audio.py --cache /tmp/coo-world-audio-sources
python3 Tools/Audio/verify_world_audio.py --report /tmp/world-audio-verification.json
python3 Tools/Audio/verify_world_audio.py --counterchecks
```

Preparation retains the full music compositions, attenuates only if necessary for headroom, and converts to stereo Vorbis. Ambience receives a declared short overlap seam; river audio uses a declared90s excerpt. Short Foley is mono with guarded silence trim, edge fades and small silence pads. Original sources stay in the external cache; provenance records download/member/output hashes, actual gains, frame counts, license/source/credit and changes. Existing asset GUIDs are retained on reruns. The ffmpeg version is recorded; different codec versions need not produce identical compressed bytes.

Long clips use Streaming/no preload; short clips use Decompress On Load/preload, retaining a compressed Vorbis representation in the game package. All normalize flags are off because gain decisions are explicit. Music has distinct runtime mute because author Content ID can affect gameplay uploads. Full attribution, author requests and video-credit directions are in the shipped `Assets/Resources/Audio/World/CREDITS.txt`.

The verifier reads and decodes actual shipped files. It checks provenance, metadata, complete music duration, silence/nonfinite/clipping, sample format, short length, loop endpoint jumps and compressed/decoded budgets. Exact container duration is checked separately from short Vorbis decoder block padding. These checks are not a subjective listening audit or proof of Unity runtime behavior.
