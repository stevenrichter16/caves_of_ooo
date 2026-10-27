# MawToad packaged release review

## Archive and helper review — 2026-10-27

**PASS within the archive-integrity scope. No actionable defect found.** This
review independently streamed the finished archive and hashed the fresh unpacked
Linux tree; it did not trust the package helper's success flags or execute the
game. The reviewer authored the helper adaptation, so its source checks below
are self-review, distinct from the independent verification of archive bytes.

- Runtime source recorded by both package records: `751d9247cf05f55e0baf8f8f8cb02ded0eb931ae`.
- Archive: `Builds/SteamDeck/2026-10-27-maw-toad-751d9247c-current/CavesOfOoo-SteamDeck-flat16x24-current.tar.gz`.
- Size: **128,791,022 bytes**; SHA256
  `517541b827fce5397700ae39b940a55ed26b0ab85072d9563a70c11f84242ac1`. The sidecar agrees exactly.
- All **202 unique members = 189 regular files + 13 directories** match the
  receipt and current unpacked tree in exact path sets, SHA256, size and mode.
  Total regular-file bytes: **341,091,469**. One safe `CavesOfOoo-current`
  root; no duplicate names, traversal, links, special files or special mode bits.
- The gzip wrapper was read to EOF, checking its CRC, uncompressed size and
  footer. Internal `SHA256SUMS` covers all **188 other files** exactly.
  Launcher and ELF64 x86-64 executable both have mode `0755`; the executable
  header was inspected without execution.
- Archived `build-info.json` equals the external build-info record and embedded
  archive receipt. Both embedded build reports are byte-identical to their
  retained originals and name the exact fresh source-labelled outputs. Mac job
  `build-57fd800310` and Linux job `build-da1f463de2` each succeeded with **0 errors
  and 4 warnings**, after the recorded `2026-10-27T15:30:03Z` freeze boundary.
  This validates receipt consistency, not independently reproduced binary/source
  provenance; the source/freeze assertion remains the root build operator's.
- Retained native XML bytes and every case identity were independently parsed:
  `maw-toad-green.xml` **20 passed** plus `maw-toad-related-green.xml`
  **285 passed**, with zero overlap, failures, skips or inconclusives: **305
  distinct passed identities**. Both end before the freeze. Earlier RED or
  initial fixture-correction executions are not added; this review ran no tests.
- All six audio credit sections, five license texts, starter-spell source text,
  launcher and Deck README match their current repository sources. Combined
  credits contain each section exactly. The prior canopy archive still matches
  its recorded size and hash; broader save/preference/build restoration belongs
  to the separate protection receipts.

The narrow `package-maw-toad.py` adaptation passed in-memory syntax compilation.
Ten generic guard functions have identical ASTs to `package-stump-canopy.py`;
main control flow is unchanged apart from release strings. The retained native
pins are `f29585a9ccf4c4b3e93199c8b6188f82f22436a52fb96a83c0684a218afd0eab`
and `78a3041ab42446404db04c80aedaeaf7db7f19cc8c9520281f199ca73a3fa44d`.
Earlier bounded read-only validation accepted these receipts and rejected wrong
hashes and counts. No helper main, packaging, build, Unity or Git operation was
performed by this review.

**Limits:** Linux was not executed and no physical Steam Deck was tested.
Archive validity and native tests do not establish gameplay, visual quality,
controls, audio, sustained FPS or whole-area polish. The following Mac visual
review, if appended, has its own separate observed scope.

Evidence: [archive receipt](packaged-maw-toad-linux-archive.json),
[Linux build](packaged-maw-toad-linux-build.json),
[Mac build](packaged-maw-toad-mac-build.json),
[20 native cases](maw-toad-green.xml),
[285 related cases](maw-toad-related-green.xml).


## Independent Mac image review — 2026-10-27

**Bounded visual PASS; no new MawToad body or reader defect identified in these four captures.** A separate reviewer opened all four actual `1280×856` JPEGs, including the 56-pixel window title bar. This review made no Unity, game-control, Git, build or packaging calls.

- `packaged-maw-toad-01-continue.jpg` shows the native squat MawToad beside the player on the quiet Grainfield substrate. Its broad mouth, tucked feet and compact earthy silhouette remain recognizable at the displayed gameplay scale; the body is neither stretched nor replaced by its former glyph. The player HUD shows **23/40 HP**.
- `packaged-maw-toad-02-reader.jpg` independently makes the identity and state readable: focus **[34,1] maw-toad, 15/22 HP, hostile**, and an authorized Creature/Flesh portrait matching the world body's mouth, feet and palette. The portrait has the intentional transparent space above its low native body; no new clipping, frame selection or aspect defect is apparent. Existing descriptive prose is not evidence that the creature cannot pursue.
- `packaged-maw-toad-03-saved.jpg` and `04-restarted.jpg` preserve the same visible bodies, terrain and **23/40 player HP**. The reviewer independently decoded both JPEGs to RGB and compared the exact world rectangle **[0,96,940,696]**: **564,000 pixels are identical**. The retained `Fixtures/packaged-maw-toad78/Quick.json` independently identifies **Overworld.2.2.0, turn 9540, HP 23/40**. The screenshot's retained focus coordinates are not used to infer the player's post-step position.

Process identity and input history remain the root operator's evidence: the fresh source-labelled Mac app used PID **6617**, continued earned fixture 77 at player **35,1** / MawToad **34,1**; one ordinary Right step then F5 produced fixture 78 at player **36,1**. Root reports a full quit, absence of the prior process, and fresh exact-path PID **6949** before normal Continue. The pixel comparison corroborates visual restart continuity; it does not independently reproduce those process/control observations.

The log's 7-damage hit and 6-damage bite are **historical combat restored from the earned fixture**, not new combat in this packaged run. This Mac review therefore accepts the observed native body, authorized reader and bounded movement/save/restart presentation only. It does **not** newly verify a damage flash, Wet effect, death/corpse/harvest, whole journey, audio, allocation or sustained performance. Linux execution and physical Deck testing remain absent.

The visible Grainfield still has broad sparse lanes, restrained low-contrast solid formations and some black-backed glyph props. These are separate area/fullness and remaining coverage work; a readable MawToad does not certify the chunk, all Stump areas or worldwide polish. No additional decorations or source changes are justified by this bounded body review alone.

| Capture | SHA256 |
| --- | --- |
| `packaged-maw-toad-01-continue.jpg` | `4318176acd1fa0a132a8a18ddfbc2225a92603814554be3ee85986d476a72ee8` |
| `packaged-maw-toad-02-reader.jpg` | `f08efe7c82b0f5b3f64d23a400ed6dc3c86e6428238fcc58c3664781995e7f73` |
| `packaged-maw-toad-03-saved.jpg` | `4d3bb7cc67d30536d45bcc273fbbe73dd432ba642dbde310d800094f4ba67af1` |
| `packaged-maw-toad-04-restarted.jpg` | `1bee0a873bcc33602c992b5cfa18b4d84fa11bf0869f8ae98955cfeef40e70a8` |

Save/preference/build restoration is outside this image review and must be assessed from the separate root protection receipt once saved. This supplement preserves the preceding archive review unchanged.
