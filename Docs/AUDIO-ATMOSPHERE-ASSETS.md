# Further atmosphere assets: plan and source sweep

Status: third-palette assets prepared and offline-verified, 2026-10-11; live iteration acceptance remains coordinator-owned. Parent owns Unity, runtime decisions, live testing after each iteration and commits. Initial research-only stage completed; parent approved the exact ten-family contract below before production. Old assets, catalogs and pipelines remain unchanged. The third catalog will be `Audio/World/atmospheric-catalog.json`; exact runtime IDs are confirmed below.

## Saved task prompt

> Audit the original source caches and existing 53 clips. Propose and source a small CC0 palette that meaningfully separates windblown reeds/leaves, sheltered wood rooms, dry stone underground, droplets only near actual water, and real forge/repair/harvest actions. Use fresh distinct short textures rather than renamed existing clips. Save plan and sweep before production; execute offline RED before creating assets. Preserve old catalogs, pipelines and shipped assets. The coordinator will test the actual game after each meaningful iteration.

## Plan and verified sweep

Read CLAUDE.md, both existing audio source manifests, Tools/Audio/README.md, the place preparation/verification scripts and AUDIO-PLACE-ASSETS.md. Enumerated actual archive members against shipped source/member pairs. Existing53 contain birds, crickets, frogs, dogs, filtered open-field wind, water laps, fire, two Kenney creaks, two mining ticks and melee Foley. Renaming these would not provide the requested new texture.

| Premise checked | Correction / scope decision |
| --- | --- |
| Cached Kenney RPG has `creak1/2/3`, `chop`, metal pot/latch/click. | Creak1/2 already ship; creak3 alone is too small a distinction for a new sheltered-room identity. Prefer new organic wood recordings. `chop` is genuinely unused and suitable only for an actual cutting/harvest action. |
| Cached Kenney Impact has mining and wood/metal impact variants. | Mining001/002 already ship. Unused variants are available but do not by themselves create dry underground atmosphere. Prefer sparse new stone/wood friction rather than more random hammer-like ticks. |
| Cached rubberduck water pack has splash and continuous loop members. | The two existing footsteps use splash06/09; unused splashes are not droplets. Find a real drop recording, and keep it tied to actual positive nearby water. |
| The current cave bed includes wet cave ambience. | A dry stone identity must not promise water through a universal drip layer. New dry texture placement belongs to the parent runtime iteration. |
| Real workshop actions require actual committed gameplay activity. | Do not attach hammering to an idle forge model or generic town biome. Source action clips only after the coordinator confirms a reachable action seam. |

## Candidate palette and primary evidence

All licenses must be verified on individual creator pages. CC0 full legal text is already shipped in Audio/World/Licenses. Any selected archive/member will be pinned by source hash and exact member hash; original caches stay outside the repository.

| Proposed ID | Concrete source candidate | Intended use and limit |
| --- | --- | --- |
| `leaf-rustle` / `reed-rustle` | [20 Rustles of dry leaves, qubodup](https://opengameart.org/content/20-rustles-dry-leaves), CC0, 20 stereo FLAC takes recorded in a kitchen. [Random Sounds Samples, Brandon Morris](https://opengameart.org/content/random-sounds-samples), CC0 option, recorded leaf movement. | Different short organic leaf phrases; reed use is designed Foley, not a claimed field recording of reeds. Only actual wind-exposed vegetation; no indoor leaf rustling. |
| `wood-settle` | [100 CC0 metal and wood SFX, rubberduck](https://opengameart.org/content/100-cc0-metal-and-wood-sfx), includes wood squeaks/cracks; or [Fantasy Sound Effects / Tinysized SFX, Vehicle](https://opengameart.org/content/fantasy-sound-effects-tinysized-sfx), organic unprocessed creaks. | Sparse sheltered timber texture with long silence. Must not sound like an unseen opening door or worker. Exact members pending inspection. |
| `stone-shift` | [Stone, rock or wood moved without tick, omegagreg / pauliuw](https://opengameart.org/content/stonerockorwoodmovednotick), CC0 friction effect; new archive stone members if a cleaner take exists. | Dry stone friction, no water tail; not a universal earthquake or fabricated NPC movement. Primary source lineage must remain visible. |
| `water-drop` | [Fantasy Sound Effects / Tinysized SFX, Vehicle](https://opengameart.org/content/fantasy-sound-effects-tinysized-sfx), CC0, 96 organic unprocessed files include water drops. | Single drops only beside actual water; no acid/oil/dry-underground drips. Prefer isolated drops over a 10MB generic Water Drops stream. |
| `forge-work` | [Blacksmith’s Hammer, VishwaJai](https://opengameart.org/content/blacksmiths-hammer), CC0, hammer striking a mid-sized anvil. | Actual successful forge work only; not an ambience loop. |
| `repair-work` | [68 Workshop Sounds, bart](https://opengameart.org/content/68-workshop-sounds), CC0, recorded real garage tools. | Actual committed repair; select restrained hand-tool movement, never power-drill ambience. Some source takes contain amplified background noise, per author comments; inspect candidates. |
| `harvest-cut` | Existing hash-pinned [Kenney RPG Audio](https://kenney.nl/assets/rpg-audio), unused `Audio/chop.ogg`; potentially distinct leaf-stem Foley. | Actual cutting/harvest success. No new music or generic UI confirmation masquerading as work. |

## Milestones and acceptance gates

1. Finalize a compact exact palette with the coordinator; confirm reachable action seams and string/enum contracts. Download only candidate originals to a separate temporary cache; inspect archive members and signal data.
2. Write and execute failing offline contract tests before production clips: required new IDs, real distinct source members, no byte duplicates of the previous53, complete credits/provenance/importers, old 53 preserved. Retain RED receipt.
3. Prepare only accepted clips in a new Atmospheric directory and explicit third catalog/provenance/credits. No old manifest or existing output mutation. Aim under 1MiB compressed and under 3MiB added preloaded float PCM; justify if a meaningful clip needs more.
4. Verify actual decoder output, bounded duration/headroom, source hashes, unique GUIDs, metadata, complete catalog coverage and adversarial missing/corrupt/wrong-source/wrong-importer counters. Rebuild in a temporary directory and compare deterministic same-codec outputs.
5. Freeze Assets for coordinator native import and an actual in-game route after each runtime iteration. Record the corresponding prompt, RED/GREEN receipts and live outcome; do not turn one bulk final route into a claim of testing after each iteration.

## Honesty and self-review

Can verify offline: files decode, unique source/member identity, duration/format/headroom, license/provenance retention, importer policy, preserved prior bytes and reproducibility with the same codec. Cannot verify offline: perceived realism, pleasant balance, spatial placement, correct live runtime triggers or physical Steam Deck sound/performance. No listening claim will be inferred from signal measurements.

🟡 Existing clip renames and redundant mining/creak variants were pruned as inadequate scope. 🧪 Source selection, approved IDs and offline RED/GREEN are complete; live iteration gates remain pending here. No production changes existed at plan creation.


## Coordinator-approved contract and exact pre-production selection

Approved details: leaf-rustle, reed-rustle, wood-settle, stone-shift, water-drop. Approved action enum names: WorkForge, WorkRepairWood, WorkRepairStone, WorkRepairMetal, HarvestPlant. Two distinct takes per ID; 20 mono short clips, under 2MiB compressed  / 4MiB decoded float PCM. There is no new bed or music.

Actual member sweep resolved the tentative choices before production:

| Cue | Exact source members | Correction / intended character |
| --- | --- | --- |
| leaf-rustle | qubodup `rustle/rustle01.flac`, `rustle02.flac` | Short dry leaf movements, not previously shipped. |
| reed-rustle | qubodup `rustle/rustle03.flac`, `rustle04.flac` | Different longer takes with conservative 100Hz high-pass / 2200Hz low-pass; designed soft reed Foley from leaves, not a reed field-recording claim. |
| wood-settle | Vehicle `sfx-cc0/floor-creak-01.wav`; rubberduck `wood_squeak_01.ogg` | Organic sheltered timber details distinct from existing Kenney creaks. |
| stone-shift | rubberduck80RPG `stones_01.ogg`, `stones_02.ogg` | Dry loose-stone textures; no water tail. Omegagreg short MP3 candidate pruned in favor of two complete source takes. |
| water-drop | Vehicle `sfx-cc0/water-drop-01.wav`, `water-drop-02.wav` | Isolated organic drops; no continuous cave-water bed. |
| WorkForge | VishwaJai `blacksmithhammer.wav`; Vehicle `sfx-cc0/metal-hammer-hit-01.wav` | Separate recorded hammer contacts for actual successful forge action. |
| WorkRepairWood | rubberduck `wood_hammer_01.ogg`, `wood_hammer_02.ogg` | Subdued wooden knocks, no anvil clang. |
| WorkRepairStone | rubberduck80RPG `item_stone_01.ogg`, `item_stone_02.ogg` | Short stone placement/contact, distinct from ambient stone-shift takes and existing mining cues. |
| WorkRepairMetal | Vehicle `sfx-cc0/metal-knife-scrape-01.wav`, `metal-knife-scrape-02.wav` | Restrained organic metal abrasion, no invented motorized workshop. |
| HarvestPlant | qubodup `rustle/rustle09.flac`, `rustle10.flac` | Brief handling/pluck Foley; avoids axe chop for bare-handed plant harvest. |

Additional primary source: [80 CC0 RPG SFX, rubberduck](https://opengameart.org/content/80-cc0-rpg-sfx); creator lists four stone and 21 item sounds, under CC0. The approved set uses five original archives/files. Workshop pack was inspected but pruned: organic Tinysized metal and rubberduck wood/stone already cover the actual material distinctions without powered-tool noise. No selected source/member pair appears in the existing 53 manifests.

Offline RED executed before production: 4 tests, 3 expected missing-third-catalog failures and 1 old 53-byte-preservation pass. Raw receipt `Docs/Verification/AtmosphericAudio/assets-red.txt`. Parent controls runtime iteration ordering and live tests; asset readiness alone does not mark an iteration live-tested.


## Offline implementation and receipts

All evidence is under `Docs/Verification/AtmosphericAudio/`.

| Check | Actual outcome |
| --- | --- |
| Initial contract RED | 4 tests: 3 missing-third-catalog failures, 1 preserved-old 53 pass. |
| Prepared contract GREEN | 4/4 tests pass, including exact approved IDs, two different source/member takes per family, full credits/importers, no previous output hash duplicates and old 53 preservation. |
| Actual-file verifier | 20/20 decode/importer/provenance/headroom/duration checks pass. 257,409 compressed bytes (251.38KiB), 3,107,736 short decoded float bytes (2.96MiB). |
| Duration guard RED | A 3.6s detail passed the earlier 4s bound; dedicated test failed before tightening only the new verifier to 3.5s. Existing shorter assets did not change. |
| Adversarial GREEN | 14/14 tests, including missing/corrupt output, wrong member/source hash, unknown ID/gain, streaming importer, duplicate GUID, missing credits/license/provenance, extra unreferenced file and malformed/silent/clipped/wrong-rate/too-long signals. |
| Repeat preparation | All 20 Ogg bytes and all 3 metadata text outputs reproduce exactly in an independent temporary directory with the same local codec. New temporary GUIDs excluded; production GUIDs preserved. |
| Prior palettes | Full original 22 and prior 31 verifiers are run unchanged; their raw receipts are retained separately. |

New clips are mono 44.1kHz Vorbis, DecompressOnLoad, preload on, normalizer off. Detail peaks target 0.25; action peaks target 0.35 before catalog gain. Longest detail 2.522s; longest action 1.551s. Gain values are restrained starting points for the live mix; file-level peak checks cannot certify pleasant listening. No additional source is played because its corresponding workshop object merely exists.

The third catalog is `Audio/World/atmospheric-catalog`; all resources are under `Audio/World/Atmospheric/`. Full credits live at `Audio/World/ATMOSPHERIC-CREDITS`. The complete existing CC0 legal text is retained. Vehicle's original Tinysized archive readme is copied into this evidence directory; it confirms CC0 and the creator's contact/site. Unselected workshop, stone-friction and unused large candidates remain temporary research downloads only.

Assets were frozen after preparation and remain untouched during coordinator native/Play runs. No Unity tools, package/build tools or commits were executed by this agent. Live evidence after each meaningful runtime iteration must be linked by the coordinator; this offline table makes no claim that those iterations have already passed.

Files owned: new `Tools/Audio/atmospheric_audio_sources.json`, `prepare_atmospheric_audio.py`, `verify_atmospheric_audio.py`, `test_atmospheric_audio.py`, `test_atmospheric_audio_adversarial.py`, `README-Atmospheric.md`; new Atmospheric assets/folder and third catalog/provenance/credits with metadata; this doc and focused verification receipts. Prior manifests, preparation scripts and shipped bytes are unchanged.

Self-review: 🟡 No single-take duplication to inflate variety: all 20 source/member identities differ and are absent from the old 53 source tables. 🟡 Two material-specific repair sets use quiet wood/stone contacts, with metal abrasion kept separate; no metal anvil cue is assigned to wood/stone. 🟡 Ambient stone texture is dry loose-stone Foley, not the originally considered tiny friction MP3 or existing mining strikes. 🧪 Sound character and live mix remain a listening/live-route gate, not a numerical assertion.


## Delivery packager plan and sweep — before implementation

Coordinator prompt: prepare a new `Tools/SteamDeck/package-atmosphere.py` and offline tests for `Builds/SteamDeck/2026-10-11-atmosphere-current`, archive `CavesOfOoo-SteamDeck-atmosphere-current.tar.gz`. Preserve both historical packagers. Bind all 73 files from three manifests and three independent signal receipts, retain all complete credits/provenance/specifications, and require a manifest linking four fresh live iteration receipts. Do not execute packaging, build, Unity or commits here.

Verified premises: the previous place packager strictly accepts its original22 + place31 union, so adding20 must be a separate tool rather than weakening its historical guard. Short-cue receipt keys differ: original uses `sfxDecodedFloatBytes`; both expansions use `shortDecodedFloatBytes`. Native iteration3 emits `native-repair-and-cultivation`, while iterations1/2/4 emit `world-audio`. Reports have raw duration but no UTC/revision stamp. The final world-audio report is the same selected report as iteration4, per coordinator agreement.

Plan: write and execute an absent-module RED first; implement exact22/31/20 counts, disjoint union, full third-catalog/importer/source/license/receipt verification and combined credits; add explicit `--iterations-manifest` and `--atmosphere-asset-report`. Manifest entries bind iteration, mode, report path, SHA256 and run ID; four distinct runs must pass their individual checks and retain their limits. `reportModifiedUtc` plus `timeBasis=report-file-mtime-estimate` records filesystem evidence, checked against each actual file; any estimated interval uses only raw `seconds`. The coordinator supplies task-start `notBeforeUtc`. These are explicitly estimated evidence intervals, not independently logged execution times or source-version identities. Preserve each receipt/count separately. Tests use synthetic receipt fixtures and temporary asset copies, never run a player or write Assets. Root supplies final native NUnit/build receipts and performs delivery.

Self-review gates: counter-check wrong third palette, missing/duplicate/corrupt/failed/stale/misordered report, wrong iteration3 mode, old final report substitution, copied-but-changed bytes, complete credits and unchanged old tools. Source HEAD/clean production tree/fresh Linux ELF/build output/no-overwrite/archive readback checks remain inherited. Sixteen-source runtime limit is not expanded by packaging, and no performance percentage or listening/Deck claim is introduced. README remains unchanged until coordinator requests final content.
