# Shared-storage package — independent delivery review

Reviewed 2026-10-27. **Independent Linux archive integrity and bounded Mac image/receipt review: PASS.** The final supplement below verifies actual crate transfer/restart images and reads the successful normal-state restoration receipt; earlier pending statements are retained as review history. This review executes neither the Linux product nor Unity, builds, packaging helpers, restoration helpers or Git. Only this new review file is written.

## Fresh archive and unpacked tree

Source asserted by the root build operator and consistently identified in both fresh build receipts and package metadata: `d9f757841d7809b69ec859d0924bd0fba62016ca`. Root reports this source remotely verified. This reviewer checked the saved receipts and exact current tested source files, not Git or source-to-binary reproducibility.

Archive:
`Builds/SteamDeck/2026-10-27-shared-storage-d9f757841-current/CavesOfOoo-SteamDeck-flat16x24-current.tar.gz`

- Compressed size: **128,810,187 bytes**.
- SHA-256: `536f9904de493d4b289f8b4c3ebf54618fc0655b74d3ced774dfe760d429a35d`.
- **202 members: 189 regular files and 13 directories**, including the top directory.
- Unpacked regular-file payload: **341,142,245 bytes**.

Independently streamed every archive member without extraction. Every file's streamed SHA-256, declared/observed size and permission mode matches both the receipt and the actual unpacked `CavesOfOoo-current` tree. Every directory and mode also matches, with exact file/directory set equality. Paths are unique and remain beneath the one expected root; no absolute/traversal paths, links, special entries or special permission bits were accepted. The executable has an executable mode and ELF64 little-endian x86-64 header; it was not launched.

Separately drained the complete gzip wrapper to EOF, checking CRC/ISIZE/footer rather than relying on tar's terminator. Internal `SHA256SUMS` contains exactly **188 distinct entries**, covering every other regular file and matching all bytes. The archived/unpacked `build-info.json` equals both its durable copy and the archive receipt's metadata. Build-report copies match their pinned durable receipt bytes; the README hash matches. All six source credit sections are present, the combined AUDIO-CREDITS text matches their ordered concatenation, all five listed license texts are present and nonempty, and starter-spell source credits are retained. These are presence/content-integrity checks, not a new licensing opinion.

| Evidence | SHA-256 |
| --- | --- |
| `packaged-shared-storage-linux-archive.json` | `538e4022bdd1e54feeb8956ad0f981a41b7f390b858d42c153c585b476874a0c` |
| `packaged-shared-storage-linux-build-info.json` | `33551c8cdb2bc2d52f251fd272f9dfb2bd496a2caf7ddf25dca63ba97e357f70` |
| Mac build receipt | `7da94c54d7a5a3dde12f5757f3d4c4abd99ecefaff4f56f40b17bdb2d86c9bab` |
| Linux build receipt | `eb642c83706bd95ab1b7d2fe51b358a1c2f10db57bbb7bfb0f11a47d6bf57fcc` |

## Build and native-evidence boundaries

Both receipts report successful fresh outputs under source-specific `2026-10-27-shared-storage-d9f757841-current` directories, with starts after the asserted20:46:11UTC source/build boundary. Independently recomputed durations from the saved timestamps:

| Target | Start UTC | Duration | Receipt size | Errors / warnings |
| --- | --- | ---: | ---: | --- |
| Mac StandaloneOSX | 20:46:11.956487 | 71.050367 s | 344.41 MB | 0 / 4 |
| Linux StandaloneLinux64 | 20:47:51.980410 | 66.387200 s | 324.87 MB | 0 / 4 |

Warnings remain present; successful compilation/build is not a warning-free or gameplay result. This review binds receipts to their actual output paths and archive contents, but cannot independently establish reproducible source-to-binary provenance.

Independently checked the package's byte-pinned native evidence: **136 focused/compatibility Passed +470 related Passed =606 distinct Passed identities**, with no overlap, failure, skip or inconclusive result. The focused set is43 storage,39 existing anchors and54 existing cellar cases. The raw Result JSONs and separately retained actual successful job metadata match the package pins and precede the build boundary. RED history is not added to this aggregate. The tested-source summary and exact current fixture, companion and Greybox file hashes match the three recorded source pins. Native tests were executed by root earlier; this audit reruns none.

The packager itself also received an independent cold review before execution: nine generic archive/hash/build/collision guard functions remained AST-identical to the prior reviewed helper; the new raw-JSON parser binds actual job IDs/counts/times separately from result hashes and exact tested source. No concrete regression was found. The new restoration helper's three function bodies were likewise unchanged after normalizing the exact receipt filename; its owned metadata permission is limited to the new storage catalog/test metadata, with no new PNG permission. These source reviews do not claim the helpers' later execution or restoration succeeded.

## Pending actual delivery evidence

Mac screenshots are arriving separately. At this first checkpoint, ordinary fresh-process Continue, transfer/save/restart, player logs and normal-save/preference/prior-build restoration have not yet been independently reviewed. Archive integrity does not certify those results. The previous tracked source/Editor review remains frozen.

This package is a body-only integration for five genuine storage families plus a separate guarded living Mimic branch. It does not certify an actual Mimic encounter, successful-key interaction, every container, complete lock/awake information disguise, full chunks or regional polish. No Linux gameplay, physical Steam Deck run or performance certification is claimed. The already recorded sparse surface/Stump composition, small cave-body contrast and mixed remaining glyph coverage remain open.


## Final Mac and preservation supplement

Independently opened all eight actual Mac PNGs at1280×856, including the56-pixel window title bar. Capture01 is the ordinary startup/Continue prompt over the bootstrap view; it is not the saved Stump fixture. Captures02–08 show the continued Stump scene. The compact crate remains crisp, in its correct cell, with the same boarded geometry and matching Wood10/10 reader as the Editor checkpoint. No new body stretching, clipping or reader mismatch is visible. Broad sparse ground and subtle dome separation remain composition limitations; this pass does not promote Stump polish.

The actual UI sequence supports the narrow functional result: Open initially lists Healing Tonic and Stun Gas Grenade; after the recorded take, one grenade remains and the action log names the tonic transfer. The ordinary save/fresh-process Continue returns player45,2 at HP29 with the same native crate. Reopening it after restart still shows one Stun Gas Grenade. The receipt records two process runs with full quit between them; this reviewer did not launch or inspect processes. No in-process entity inspector was used in standalone, so exact item IDs come from the prior Editor fixture, while these packaged checks directly establish displayed names/counts/action log and save metadata.

All eight capture hashes and both files in `Fixtures/shared-storage-packaged106` independently match `packaged-shared-storage-actual-play.json` (SHA-256 `3c11bf852a2ffe545bf08d2eac0442da7ee6d3050920e004958b603da1119aee`). The earned metadata records Stump3.4.0, turn9110, HP29. Independent decoded RGB comparison of clean saved06 versus clean fresh-Continue07 rectangle `(0,96,940,696)` gives **zero differing pixels out of564,000**. This is a valid matched-world comparison, separate from the rejected Editor Pause05/clean06 pair. It does not assert whole-window/HUD/log equality.

The retained restart log hash matches the actual-play receipt. It contains zero case-insensitive `error|exception|failed` matches, but **six “may have been prematurely finalized” thread lines and one ComputeBuffer disposal warning remain**. Thus a clean substring count is not a warning-free shutdown or proof that these existing lifecycle concerns are fixed. No actual package Mimic, successful-key unlock, other-family access/destruction, new combat/status experiment, Linux execution, Deck execution or sustained performance test occurred in this bounded package journey.

Read the successful root-executed restoration receipt, SHA-256 `e1b58709a9688e49656bd8d765c121238e3153f999148787361138da32ed3316`, completed20:55:53UTC. It reports all **51,426 tracked snapshot objects equal**, **49 preexisting dirty tracked files preserved**, prior current builds preserved, normal Editor saves/preferences unchanged, and original standalone-save absence plus preference state restored after archiving the owned standalone session. Exactly four listed generated settings were restored: UniversalRP, UniversalRenderPipelineGlobalSettings, ProjectSettings and UnityConnectSettings. No cache deletion occurred. This reviewer did not independently repeat the entire51,426-object restore or execute a restoration helper.

The first narrow preference guard correctly refused newly introduced window-placement keys. Independent source comparison of the specifically authorized replacement helper (`3a231c68f3530d9300c6c1c7d48961c08daf36c6e27f8dcda310dd5fbd91389f`) confirms that, beyond documentation, its only change adds the exact names `UnitySelectMonitor`, `Screenmanager Window Position X` and `Screenmanager Window Position Y` to the existing full-match LastGameID alternatives. Exact observed-extra-set equality and all other restoration guards remain unchanged; this is not a wildcard preference permission. The final receipt reports four introduced keys, including the existing LastGameID addition.

This closes the in-flight storage delivery review at bounded archive/build/native and actual Crate transfer/save/restart/preservation evidence. It leaves the previously stated visual/access/platform limits intact. Subsequent development is the user's isolated Shale reference-parity task; no further storage development is implied by this completion.
