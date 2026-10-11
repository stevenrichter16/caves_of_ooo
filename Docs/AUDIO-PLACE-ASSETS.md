# Place and combat audio asset expansion

Status: 31 additional clips prepared and offline-verified, 2026-10-11; native import and a22-check actual place-playback route passed; subjective mix remains unclaimed (see AUDIO-PLACE-DESIGN.md). This is original CoO sound placement using independently licensed assets; no Qud recordings or parity claim. Existing 22 world clips, their source manifest, catalog, credits and verifier stay unchanged.

## Plan and verification sweep before implementation

Read `CLAUDE.md`, `Tools/Audio/README.md`, `prepare_world_audio.py`, `verify_world_audio.py`, `world_audio_sources.json`, and the creator pages below. Parent owns Unity and place routing; HUD agent owns actual melee outcome routing. This agent owns a separate expansion source manifest, reproducible preparation/verification, importer metadata, `places-catalog.json`, expansion credits/provenance, and this document. No commits without parent coordination.

| Verified premise | Decision / correction |
| --- | --- |
| Original preparation has hard-coded water excerpt and a 50MB source limit; original verifier pins 22 clips and 30–120s beds. | Keep original pipeline untouched. Separate generic excerpt contract permits bounded larger raw field recordings, small shipped excerpts, and appropriate short nature loops. |
| Existing Kenney archives are hash-pinned CC0 and have multiple physical Foley takes. | Reuse their source archives; no extra downloaded music or duplicate legacy assets. |
| Thimras wind and river are actual park field recordings under CC0; river includes birds, frogs and wildlife. | Ship only a 60s open-field wind excerpt with 45Hz high-pass and 650Hz low-pass. Marsh aliases this wind at lower gain. River candidate was downloaded for inspection but is not shipped; no river-everywhere shortcut. Wind is not certified wildlife-free. |
| AntumDeluge Mutant Frog is a human imitation; Aj_ frog and LokiF swamp are synthesized. | Pruned. Root found EZduzziteh Ribbit Frog Sounds, individually CC0. Ship two short frog effects; creator does not specify recording method, so no field-recording or exact-species claim. |
| Existing game has PetDog, Magpie and several frog creatures; CascadeFather explicitly has no call (parent sweep). | Dog is eligible only for actual appropriate visible entities. No generic town crowds, invented workshop activity or silent-species call. |
| Melee routing can prove physical attack family, not shield/metal armor contact. | Seven agreed cue IDs, no armor cue. Keep generic Swing/Hit fallback. |

## Selected / candidate primary sources

All licenses were checked on the individual author/submission page, not a collection page. CC0 legal text: <https://creativecommons.org/publicdomain/zero/1.0/>. Source hash, exact excerpt/member, changes and source-page links will be retained in the expansion provenance. Numerical checks do not establish subjective listening quality.

| Source and author | License and evidence | Proposed use |
| --- | --- | --- |
| [Ambient Bird Sounds, isaiah658](https://opengameart.org/content/ambient-bird-sounds) | CC0; author describes camera-mic recording and cleanup; invites excerpting chirps. | `bird` variants outdoors; no species-authenticity claim. |
| [Random Sounds Samples, Augmentality / Brandon Morris](https://opengameart.org/content/random-sounds-samples) | CC0 option explicitly listed alongside OGA-BY; author says recorded, EQed and made stereo. | `night-field` cricket bed, `insect` phrases, `dog` for real PetDog. |
| [Park ambiences, Thimras](https://opengameart.org/content/park-ambiences) | CC0; author identifies Adelaide park field recordings, open-field wind and river including frogs/birds. | `dry-wind` and low-gain `marsh` alias; original wind 74MB stays outside repository. River 99MB rejected for this palette. |
| [Fire Crackling, AntumDeluge](https://opengameart.org/content/fire-crackling) | CC0; creator confirms public domain; fireplace pops/crackles. | Sparse `fire` anchored to actual active visible fire. |
| [Sea and river wave sounds, RandomMind](https://opengameart.org/content/sea-and-river-wave-sounds) | Existing pinned CC0 source. | `water-lap` excerpts beside visible positive freshwater pools. |
| [RPG Audio, Kenney](https://kenney.nl/assets/rpg-audio) | Existing pinned CC0 archive and original License.txt. | Restrained `wood-creak`; knife/chop attack Foley. |
| [Impact Sounds, Kenney](https://kenney.nl/assets/impact-sounds) | Existing pinned CC0 archive and original License.txt. | `stone-tick`, distinct contact family takes; Foley, not literal injury recordings. |
| [Swish — bamboo stick weapon swishes, qubodup](https://opengameart.org/content/swish-bamboo-stick-weapon-swhoshes) | CC0; creator explicitly recorded waving bamboo in front of microphone. | Distinct blade/blunt/natural movement takes; no synthesized substitute. |
| [Ribbit Frog Sounds, EZduzziteh](https://opengameart.org/content/ribbit-frog-sounds) | Individual page explicitly CC0; three short MP3s; recording method unspecified. | Two `frog` takes; no SummitSinger or CascadeFather claim. |

## Bounded palette contract

Expansion catalog retains schemaVersion 1, with empty music, optional ambience rows, `details` string IDs and `sounds` enum-name IDs. Resource paths omit extensions and live under `Audio/World/Places/` (beds/details) and `Audio/World/Combat/` (14 short takes). Every catalog row must correspond to pinned provenance; every prepared asset must be referenced. Gains are conservative starting values requiring native listening/mix review.

Place detail IDs: `bird`, `insect`, `water-lap`, `fire`, `wood-creak`, `stone-tick`, `dog`, `frog`. Beds: `night-field`, `dry-wind`, `marsh` (same wind resource, lower gain). Combat IDs: SwingBlade, SwingBlunt, SwingNatural, HitCutting, HitPiercing, HitBlunt, HitNatural; two takes each. No fake town workshop loop and no extra animal family without a real entity.

## Verification gates

1. Execute failing offline contract before expansion production: required catalog IDs/resources missing, original 22 unchanged; save receipt.
2. Pin source/container/member hashes and license references; prepare mono short cues / stereo streaming beds with explicit excerpts, edge fades, loop overlap, bounded gain and normalizer disabled.
3. Verify actual decode, silence/headroom, sample format, exact declared frame lengths, metadata, unique GUIDs, catalog coverage, source/credit consistency and bounded compressed/preloaded budgets. Reject missing/corrupt samples and wrong importers in dedicated counterchecks.
4. Re-run original verifier and compare original tracked byte hashes to prove preservation. Native import, actual audibility, spatial placement and mix remain parent-owned gates.

## Implementation log / self-review

- Plan/sweep saved before production. Five actual asset contract tests executed: 4 expected failures for missing expansion and 1 original 22 preservation pass; after preparation 5/5 GREEN. Receipts: `Docs/Verification/PlaceAudio/assets-{red,green}.txt`.
- 🧪 Subjective listening remains unclaimed; native import/playback passed in the coordinator route. Header/duration/signal checks are not substitutes for hearing the result.
- ⚪ Frog method unspecified and no species authenticity claimed; exact clip filenames and source credits retained. No bat, kitchen, generic crowd or imaginary forge work imported.


## Final offline evidence and import policy

- 31 unique new Ogg files: 2 stereo beds, 15 mono detail phrases and 14 mono combat takes. The three bed catalog IDs share only the declared marsh→wind alias. Total compressed 1,319,315 bytes (1.26MiB); short decoded float PCM 5,344,120 bytes (5.10MiB), below the separate 4MiB compressed / 6MiB short budget. Original22 remain 22,906,583 compressed bytes and 1,140,428 decoded short bytes.
- `asset-contract-green.json`: 31/31 actual-file decodes, frame lengths, headroom, non-silence, loop seams, full row provenance, credit text, importer metadata and unique GUIDs. `original22-green.json`: original complete22 verifier still passes. These are independent checks, not Unity import evidence.
- `wind-filter-red.txt`: missing real filter failure observed before filter implementation. `adversarial-green.txt`: 16/16 tests after final preparation, including bad source hash, actual missing/corrupt asset, unknown/extra catalog data, gain drift, changed excerpt, preload/GUID/credit/license failure, unrelated clip and malformed/silent/clipped signals. Filter counter retains 300Hz while attenuating 15Hz and 4kHz synthetic probes; this does not prove the field recording has no wildlife.
- `rebuild-green.json`: an independent temporary output directory rebuilt all 31 compressed files and all 3 catalog/provenance/credit texts byte-identically with the same local ffmpeg version. Newly allocated temporary GUIDs are intentionally excluded; production reruns preserve existing GUIDs. Cross-codec byte identity is not promised.
- Beds: Streaming, preload off, load in background, stereo44.1kHz. Detail and combat: DecompressOnLoad, preload on, mono44.1kHz Vorbis; normalizer off. Short phrases retain silence guards and fade/pad edges. Details are up to 3.02s; combat up to 0.596s. An 11.774s cricket bed is permitted by this separate manifest, not falsely stretched to the old 30s bed minimum.
- Original source/asset/catalog/credit files and historical preparation/verifier/packager were not edited. `PLACES-CREDITS.txt` retains every author, source, license and transformation, plus the full original qubodup archive notice. Complete CC0 and original Kenney notices are reused from the existing Licenses directory.

## Reproduction and packaging

```
PYTHONDONTWRITEBYTECODE=1 python3 Tools/Audio/prepare_place_audio.py --cache /tmp/coo-place-audio-sources
PYTHONDONTWRITEBYTECODE=1 python3 Tools/Audio/verify_place_audio.py --report Docs/Verification/PlaceAudio/asset-contract-green.json
PYTHONDONTWRITEBYTECODE=1 python3 Tools/Audio/test_place_audio.py
PYTHONDONTWRITEBYTECODE=1 python3 Tools/Audio/test_place_audio_adversarial.py
```

Preparation needs Python3/numpy, ffmpeg/ffprobe, curl and bsdtar (7z read support). Downloads are exact-byte/hash pinned; the wind's large uncompressed original is cached externally. No Unity call or game build occurs in these tools.

`Tools/SteamDeck/package-place-audio.py` is a new delivery tool; historical `package-performance-audio.py` stays unchanged. Observed missing-module RED preceded implementation; 15 tests GREEN include exact current 53-file union, separate receipts, source/importer/catalog/credit/license mutation counters and all inherited native/NUnit finality/count gates. New delivery path is `Builds/SteamDeck/2026-10-11-place-audio-current`, archive `CavesOfOoo-SteamDeck-place-audio-current.tar.gz`; no package was built or produced by this agent.

```
PYTHONDONTWRITEBYTECODE=1 python3 Tools/SteamDeck/package-place-audio.py FINAL_NATIVE_REPORT_JSON \
  --source-commit FULL_FINAL_HEAD \
  --build-report FINAL_LINUX_BUILD_REPORT_JSON \
  --editmode-report FINAL_NATIVE_NUNIT_XML \
  --asset-report Docs/Verification/PlaceAudio/original22-green.json \
  --place-asset-report Docs/Verification/PlaceAudio/asset-contract-green.json
```

The package concatenates complete original/expansion credits into readable AUDIO-CREDITS.txt and also includes separate PLACES-CREDITS.txt, both source manifests, both provenances, both offline receipts, and full license texts. It requires the final committed source, fresh successful Linux build at the exact new path, completed error-free world-audio native report, matching individual NUnit counts, and an absent archive. All payload bytes are hashed and read back from the archive; existing archives are never overwritten. Neither source/receipt counters nor packaging establish physical Deck performance or subjective listening.

Self-review: 🟡 Corrected the historical 22-only recursive Ogg check to validate the exact union of both manifests, retaining rejection of unrelated files. 🟡 Detail clips explicitly share the short-cue import policy; they are not accidentally treated as streaming beds. 🟡 Wind high-frequency content is filtered and honestly labelled, not certified absent. 🧪 Actual generated-glade detail playback passed; subjective mix, all-biome listening and physical Deck verification remain unclaimed. No numeric performance improvement claimed.

Independent read-only review of the new package tool found no actionable issue in the exact union, pinned catalog, credits, imports, receipt copies, alias guards or archive readback. Acceptance caveat: the native mode remains `world-audio`; root must supply the fresh place/combat route receipt. Mode alone does not prove that new coverage. No packaging was executed during review.
