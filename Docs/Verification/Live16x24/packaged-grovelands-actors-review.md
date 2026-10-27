# Grovelands actors — independent packaged-delivery review

## Linux archive integrity

2026-10-27. **PASS within the archive/evidence scope; no significant packaging defect found.** The reviewer independently streamed the archive and its gzip wrapper, hashed the unpacked tree and examined build/native receipts and included credits. Neither the packaging helper nor any game, Unity, Git, Linux executable or test runner was executed by this review.

- Source named consistently: `4dbdc9be95658341260efa10307dadb784ba8392`. Source freeze: **16:25:24 UTC**. Root reports that revision preserved on main; this read-only review does not independently query Git or prove source-to-binary reproducibility.
- Archive: `Builds/SteamDeck/2026-10-27-grovelands-actors-4dbdc9be9-current/CavesOfOoo-SteamDeck-flat16x24-current.tar.gz`.
- Exact compressed size **128,791,876 bytes**; SHA256 **`8ed7261f523993000e80124bcd0c10b2e889524604a46961217684c156339a21`**. The adjacent checksum sidecar agrees.
- **202 unique tar members: 189 regular files and 13 directories**, containing **341,096,764 regular-file bytes**. Every member's path, type, byte count, hash and mode matches both the retained manifest and the actual unpacked tree. No extra/missing entry, unsafe traversal, duplicate path, link, special file or special permission bit was accepted.
- The reviewer separately drained gzip to EOF after tar verification, validating the wrapper CRC/ISIZE/footer rather than stopping at tar's terminator. All **188 internal SHA256SUMS entries** cover exactly every other packaged regular file.
- The executable header is little-endian **ELF64 x86-64** with mode `0755`; it was **not executed**.

The archived `build-info.json` is identical to its external evidence copy and the archive receipt's embedded object. The archived Linux and Mac build-report bytes match the corresponding pinned external reports. Both receipts explicitly name the source above, have distinct concrete build job IDs, successful zero-error results, and output paths bound to the fresh source-named directories. Both start after the source freeze and have ordered completion times:

| Build | UTC interval | Duration | Errors / warnings |
| --- | --- | --- | --- |
| Mac `build-e53f88a632` | 16:25:32.898228–16:26:38.533880 | 65.635652 s | 0 / 4 |
| Linux `build-f8920eef28` | 16:26:45.389768–16:27:45.910246 | 60.520478 s | 0 / 4 |

Native evidence was independently parsed again: **44 focused + 363 related = 407 distinct Passed identities**, with no overlap, duplicate, failed, skipped or inconclusive cases. Both exact XML hashes match the package pins and both executions finish before the source freeze. Earlier trials are not added. This archive review did not rerun those tests.

The six individual credit sections, their combined `AUDIO-CREDITS.txt`, all five complete audio license texts, the packaged audio provenance files, starter-spell source record, launcher and Steam Deck README match their source files byte-for-byte. The prior MawToad archive still matches its own retained SHA256; this is a check of that prior archive, not a new assertion about every older build tree. The current packager's broader original-build preservation statement remains its own recorded check.

Evidence identities at review:

| Record | SHA256 |
| --- | --- |
| `packaged-grovelands-actors-linux-archive.json` | `20dde10f5ae9430c2c42c58ac84e3618f25ac00cb0171dd7b6c1cf3fcb48d675` |
| `packaged-grovelands-actors-linux-build-info.json` | `3d6e968bc63c3cba94f790dc6cfd0c104d73feb5f53534918fcae2d2c13b11a4` |
| Focused native XML | `c5132632f1b697ea2cebf78a364c3d0d9ac47971e93bbf0defa5ea23dabdcaed` |
| Related native XML | `a6e08ab1c60b2962572aca64c8b965806ca0774bd4d4af03f93f4d59c47ac4fe` |

This verifies delivery integrity, not Linux gameplay, physical Deck behavior, sustained performance, whole Grovelands coverage or area fullness/polish. Mac actual-capture review and post-run save/preferences restoration are separate pending evidence at this point; they must not be inferred from a successful build or archive.

## Actual Mac package — nine-image review

The reviewer opened all nine final `packaged-grovelands-actors-01` through `09` JPEGs individually and checked them against `packaged-grovelands-actors-mac-play.json`. **Bounded Mac body/reader, restored-service, travel and save/restart checkpoint passes; no new material actor-art or geometry regression is visible.** Each image is 1280×856, including the 56-pixel window title bar above the 1280×800 game view. All nine recorded file hashes match. Root controlled the exact fresh app and reports process **16871**, full quit with the old process absent, then process **17885** for the fresh Continue. The retained logs independently identify this source-named app directory; process history remains root's observation.

- **01/02:** the continued Fen retains player `(20,16)`, HP 23/40 and purse 32. Choir at `(19,17)` reads HP 80/80, neutral, with the same narrow branching body and matching square portrait.
- **03/04:** normal Chat retains that portrait and offers Trade. The restored merchant has **118 drams and two stock entries** (ink/codex), while the player has **32 drams and the venom gland** purchased earlier in the Editor checkpoint. This is persistence/service-opening evidence, **not a new packaged purchase**. The clipped faction label and cramped conversation layout remain visible; the long codex trade label also truncates. They are retained UI polish debt, not silently accepted as fixed.
- **05:** Mosshulk at `(14,8)` reads HP 55/55, hostile. Its broad green square portrait remains distinct from Choir, and the reader still reports the actual water underfoot and its hazard meaning. This is not an applied Wet/status-cue test.
- **06/07:** root's ordinary four-east/one-north map route reaches Grove `(4,7)`. The two mauve Shambler bodies remain legible against green ground; the examined body at `(22,10)` has HP 16/16 and a matching attached-tendril portrait. The displayed name “spore shambler” remains the authored Shambler label; these images do not change the source catalog's exclusion of the separate SporeShambler blueprint. No new windup/combat interaction is established by this package reader.
- **08/09:** after one normal Right step, the saved and fresh-Continue views both show player `(41,12)` with HP 23. The reviewer independently decoded and compared `[0,96,940,696]`: **564,000 RGB pixels, zero differences**, excluding the title bar, sidebar/log and lower UI. Both retained fixture hashes match; `Fixtures/packaged-grovelands-actors82/Quick.json` confirms **Overworld.4.7.0, turn 10420, HP 23/40**.

The unchanged limitations are apparent in the package too: green Fen actors have lower contrast against vegetation, the Grove contains broad repetitive floor patches, and uncovered glyph-backed props/source cells remain. Quiet travel space is useful, but these bounded views do not establish whole-chunk fullness, all areas or a completed polish pass. Undisclosed/fog regions are not classified as bare terrain. The inherited purchase/combat narrative in the log is not a new packaged event. No new packaged damage flash, Wet application, actor death, Linux/Deck play or sustained performance trial is claimed.

Both retained Player logs match their recorded lengths and hashes and were read. No exception or error stack trace was found in these two bounded logs. `Player-prev.log` does contain **six premature-thread-finalization diagnostics and one ComputeBuffer disposal warning at shutdown**. These remain recorded caveats; the logs are not warning-free, and this evidence does not attribute those diagnostics to the three-actor change. The current Player log records normal startup. Build warnings remain the four-per-platform counts above.

## Post-run preservation receipt

The reviewer read the final root-owned `packaged-grovelands-actors-restoration.json`. It reports **50,963 tracked files matching the source snapshot**, **45 dirty tracked files preserved**, normal Editor saves unchanged, normal standalone save-tree absence restored, both preference domains logically restored, and the prior Mac/Linux builds plus archive preserved. Exactly five reviewed generated serializations were backed up/restored: the new Shambler importer, the two URP settings assets, ProjectSettings and UnityConnectSettings. The introduced test-only LastGameID key was the sole remaining preference difference after import and was removed to restore its prior absence; no unrelated preference reset is asserted. Test-session saves remain archived, no cache was deleted, and the final editor target is StandaloneOSX. These are checks reported by root against its retained baseline; this image/archive review did not rerun the entire preservation audit or manipulate preferences/saves.

The earlier archive section's “Mac actual-capture review and restoration pending” statement is superseded by this section. Actual Mac execution, archive integrity and preservation are bounded delivery evidence for these three bodies and their existing mechanics. Broader coverage/polish and physical-device acceptance remain open.
