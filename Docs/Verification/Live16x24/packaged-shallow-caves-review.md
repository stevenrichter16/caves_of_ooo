# Shallow caves packaged delivery — independent evidence review

Reviewed 2026-10-27. Runtime source: `1ae80a3555e13ce77ff440a6054b732f00888dca`; source freeze recorded as 17:55:55 UTC. **Bounded delivery check passes; this does not accept whole-area polish or deeper cave coverage.**

## Review role and evidence boundaries

This reviewer authored the shallow-cave native test fixture and adapted the package/restore helpers. Those source checks are therefore self-review. A separate agent independently cold-reviewed the helper adaptations, including unchanged generic archive/build guards and exact introduced preference aliases. The archive audit below used a separate read-only stream inspection, without importing or executing the packaging or restoration helpers. Root executed native tests, builds, Mac play and restoration. No Unity, Git, Linux executable or restoration action was invoked by this review.

## Fresh builds and Linux archive

Both build reports bind to the exact source above and start after its recorded freeze. The Mac build succeeded in 65.794 seconds and the Linux build in 77.111 seconds, each with **0 errors and 4 warnings**. Their output paths are the new `2026-10-27-shallow-caves-1ae80a355-current` directories under `Builds/Verification` and `Builds/SteamDeck`; the Mac app and Linux executable exist at the reported destinations.

Independently streamed `CavesOfOoo-SteamDeck-flat16x24-current.tar.gz`: **128,797,392 compressed bytes**, SHA-256 `da967f63f6ce3980aa4e9453978a4c87c106b24efb86dc45c9394b8e9cd7de05`. All **202 members (189 regular files, 13 directories)** agree with the unpacked tree in paths, file bytes, sizes and modes; no unexpected, duplicate or unsafe members were admitted. Unpacked file bytes total **341,107,533**. All **188 internal SHA256SUMS entries** match; the checksum file itself is intentionally outside its own list. The sibling archive checksum agrees. A separate bounded gzip read reached EOF and verified CRC/ISIZE/footer integrity.

Build-info, source provenance, launcher/README, native XML pins, six credit files and five license files were checked against the archive and unpacked tree. The executable has the expected ELF header; it was not run. The previously delivered Grovelands archive still matches its recorded SHA-256.

Current native evidence is **56 + 339 = 395 distinct Passed cases**, with zero name overlap, failures, skips or inconclusives. Exact XML hashes:

- `shallow-caves-backdrop-green.xml`: `189ebe9792c41cc8e7d765a2c2e6d84b604aa611a83d6affefc341d1c5cdd5cb`.
- `shallow-caves-backdrop-related-green.xml`: `ad2353650ad520a64d6df46e16b86a5a654448b353c54fba216dc14d8d31c345`.

Earlier overlapping runs are not added to this total. Native tests are not packaged Linux gameplay evidence.

## Seven actual Mac captures

Opened all seven `packaged-shallow-caves-01` through `07` JPGs and independently verified their receipt hashes and 1280×856 dimensions (including the 56-pixel window titlebar). The actual start is retained depth-two fixture `shallow-caves-depth2-85`, not the earlier proposed entry fixture.

- **01 / 04:** depth-two continuation and stair reentry show quiet tan floor/backing, darker planar rock walls, distinct low rubble and pointed stalagmites. The eligible walkable field no longer has the earlier black pit-like gaps. This is a readable material foundation, with conspicuous repeated wall formations and remaining glyph owners still visible.
- **02:** the down-stair reader is legible and its native portrait matches the selected recessed stair body. Both stairs become separately visible when the player moves aside in the saved view.
- **03:** the normal upward return shows a narrower currently visible corridor. Black remembered areas with glyphs remain a visible presentation transition; they are not evidence of empty chunks or undisclosed native terrain.
- **05 / 06 / 07:** the player is at 42,12, HP 33, with the saved floor/stair arrangement retained. The second F5 confirmation changes the log, not the world. Independent decoded RGB comparison of 06 versus 07 at rectangle `[0,96,940,696]` found **0 changed pixels across 564,000 pixels**.

There are seven files but **six unique byte sequences**: 05 and 07 are byte-identical. Root's receipt supplies the separate-process evidence (initial PID 34272, confirmed full quit, fresh PID 34541 and ordinary Continue); identical screenshots alone do not establish process restart. The earned `packaged-shallow-caves86` save and metadata hashes match the receipt; metadata records `Overworld.16.13.2`, turn 11950 and HP 33/40.

No obvious new sprite or reader clipping defect appears in these views. The inherited combat log does not demonstrate new packaged combat. No new packaged wall destruction, hazard application, sustained performance measurement or full chunk acceptance is claimed. Root's later limestone depth-three visit is a separate fallback baseline, not acceptance of native depth-three art.

## Logs and preservation

Both retained player log hashes match the Mac receipt. Current/previous logs contain respectively **5 / 6 premature-thread-finalization warnings**, and each contains **one ComputeBuffer disposal warning**. No line containing `Exception` was found by the bounded log check. This is not a warning-free runtime claim; shutdown/resource-lifetime warnings remain recorded.

Reviewed root's completed restoration receipt and independently matched its reviewed-manifest hash. It reports **51,086 tracked snapshot objects equal**, **46 pre-existing dirty tracked files preserved**, normal saves/preferences restored, previous builds preserved and the originally absent standalone save root archived. Exactly four reviewed build serializations were restored: the owned shallow-caves PNG metadata, URP settings, URP global settings and ProjectSettings. The helper permits the sole introduced literal `CavesOfOoo.LastGameID` key alongside previously supported exact LastGameID aliases; it does not permit arbitrary preference keys. The retained standalone test-save archive exists. This reviewer did not repeat the full restoration or private preference audit. No cache cleanup/deletion occurred.

The package is a verified delivery of the bounded ordinary depth-one/two six-owner foundation and ambient backing correction. Region-wide fullness, all cave owners, all columns, deeper caves, Linux play and physical Steam Deck acceptance remain open.
