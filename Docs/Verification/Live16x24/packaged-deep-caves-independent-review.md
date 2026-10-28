# Deep-caves batch — independent package review

Reviewed source `7e2ed7a30dbd363331480321933b36fc67c58659`. This is an independent read-only helper, archive and actual-image review; I did not run Unity, Git, either build, the restoration helper, or either packaged executable. **Bounded archive/Mac integration and final restoration-receipt review pass; no new P1/P2 finding.** The original normal Editor state changed during packaging and was preserved explicitly, as detailed below.

## Helper and native evidence

- Package helper SHA-256 `fee94989ff27720ec8c3fcdd3270e7b870f11e13668f821c94f64b08f9c977b7`: nine generic hash/path/build/archive functions are AST-identical to the reviewed Slate helper. Fresh output/source binding, collision refusal, streamed member checks and complete gzip-footer checks remain intact.
- Native evidence was independently rehashed and parsed: 419 stone cases plus 19 focused and 189 related Steam cases give **627 Passed occurrences, 102 repeated identities, 525 distinct Passed identities**. No failed, skipped or inconclusive rows occur in those retained results. These are sequential source checkpoints, **not one final-source combined 525-case run**. Actual null/summary-null job responses remain separately pinned; all 28 selected current source/resource/evidence pins agree. Build provenance is the root operator's source/freeze assertion, not an independently reproduced binary.
- Original restore helper SHA-256 `7c670678169ba02c308c9c9c6ef0a39317b8ca3eb47f83d34e92ddc72f93b6b2` retains the Slate guards with exact phase names and eleven owned metadata paths; metadata restoration still permits only trailing whitespace differences. The later preservation exception is reviewed separately below.

## Archive and builds

Independently streamed `Builds/SteamDeck/2026-10-28-deep-caves-7e2ed7a30-current/CavesOfOoo-SteamDeck-flat16x24-current.tar.gz`: **128,815,464 bytes**, SHA-256 **`5b778120e5fe4bb55d70720c29536ca93d051889698eb4015664e6723c0b0bc3`**. All 202 unique safe members comprise 189 regular files and 13 directories; file content hashes, sizes and permission modes match both the receipt and unpacked tree. Unpacked file bytes total 341,322,891. All 188 internal checksum entries cover exactly every other file. Separate gzip reading reached EOF and validated the outer footer. No links or special members were admitted.

Archived build-info and build-report bytes match the durable evidence. Six credit sections, their aggregate, five license texts and starter-spell source notes are present and checksum-bound. The executable header is ELF64 little-endian x86-64; it was not executed. Mac build receipt reports 87.924338 seconds / 344.57 MB; Linux reports 99.381405 seconds / 325.03 MB. Both report zero errors and four warnings, with fresh output paths and start times after the asserted freeze.

## Actual Mac image review

Opened all twelve `packaged-deep-caves-mac-01` through `12` captures at their actual 1280×856 window size. The vent reader shows the same rimmed natural aperture as the world body, with Terrain/Steam and the existing one-turn cloud. The on-vent view retains the player and HP 28. Pillar and broken-column readers show the distinct shaft and low fragment bodies; Pillar remains Stone, solid, 48/50, while the fragment remains Terrain. The traversed fragment retains continuous actual Quartzite floor backing. These are successful body/reader and route observations, not fresh packaged destruction tests.

Quartzite depth 9 and 10 views retain quiet connected floor and straight solid boundaries. The separate Obsidian branch is visibly darker and purple-brown under current grading; its reader correctly shows the inherited damaged wall at 69/70, solid, rather than claiming fresh damage in this package. Capture 12 shows the subsequent ordinary move and save at the upstairs. This separate loaded fixture branch must not be described as continuous travel from the Quartzite branch.

I independently compared capture 06 (saved 129) with 07 (fresh Continue): world rectangle `[0,96,940,696]`, **564,000 RGB pixels, zero changed**. This excludes title bar and HUD/log differences; it is not a full-frame identity claim.

All twelve image hashes match `packaged-deep-caves-actual-play.json` (SHA-256 `0ed259ec87ae8b7a4573e5e8582ec33448ae6adc04cfca954dd585d071cf079b`). Saved metadata independently agrees with fixture 129 at depth 9 / turn 13850 / HP 28, fixture 130 at depth 9 / turn 13910 / HP 28, and the separate Obsidian fixture 131 at depth 11 / turn 13600 / HP 28. Process execution and ordinary inputs are root-observed evidence; my independent checks are the retained files, images and pixel comparison.

No new obvious native-body or reader-layout regression is visible in these bounded views. Remaining polish debt is substantial: Quartzite's tiled pinwheel-like mineral faces and Obsidian's repeated angular chevrons, warm grading, small muted fragments, sparse visible room interiors, mixed glyphs and abrupt hazard backing. Fog/memory are not empty playable space. These captures do not establish whole-chunk fullness, every deeper address, visual parity, new combat/status/destruction coverage, audio quality, Linux execution, physical Deck behavior or sustained performance.

All three retained Mac logs contain shutdown diagnostics: respectively five, six and six premature thread-finalization messages, plus one ComputeBuffer disposal message each. A clean-log result is not claimed.

## Preservation exception and completed receipt

The original restoration guard correctly refused a new normal Editor save and four changed Editor preference keys. Their ownership is unknown. The reviewed replacement `/tmp/coo-deep-caves-preserve-editor-restore.py` (SHA-256 `8f2adcf079c05e13c63a51988132ca6745aaf82e84b47abb901726bdd7b6de70`) freezes the exact current Editor preference hash and save tree before applying anything, requires every original save entry to remain equal, and asserts the frozen current state again afterward. It does not write Editor preferences or Editor saves. The original state remains in the snapshot and the new state has a separate preservation backup.

Only reviewed build serialization and owned standalone test state were restoration targets. The revised receipt deliberately distinguishes changed original Editor state from preserved current Editor state. Source review found no concrete regression in this narrow exception; I did not import or execute it.

Read the completed `packaged-deep-caves-restoration.json` (SHA-256 `533a31bbe234b3273465b644b6aeccb7851fb9032a0660a57cd400be776e40df`), applied at **2026-10-28 01:49:30.761850 UTC**. It binds the reviewed manifest SHA `1a203a485e3a2ce617ad6e8e87d827261bb9308462075358e7fdc8a57598f620`, reports all **52,272** tracked snapshot objects equal and **49** preexisting dirty paths preserved, and restores seven reviewed generated files (four settings and three atlas metadata files). Prior builds are preserved. Owned standalone test saves were archived, original standalone save-root absence and preferences restored, and the exact newly frozen current normal Editor save/preferences retained. The receipt explicitly reports `editor_preferences_and_saves_unchanged: false`; the new normal save is not discarded or relabeled as ours. No cleanup/deletion occurred.

This is review of root's completed preservation assertion, not an independent repeat of the full snapshot check after documentation resumed. The three durable `deep-caves-{package-audit,preserve-editor-restore,build-snapshot}.py` copies match their reviewed scratch bytes. The restorer still imports `/tmp/coo-sill-build-snapshot.py`; these are retained procedural evidence with that explicit dependency, not a self-contained portable tool distribution.
