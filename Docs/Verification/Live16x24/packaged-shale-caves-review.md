# Shale caves — packaged delivery review

Reviewed 2026-10-27. **Bounded archive integrity and seven-image Mac presentation review: PASS.** No significant new native-material or reader regression was visible in this evidence. Final standalone play/log and restoration receipts were still pending when this initial review was written; those are separate gates below. This is not whole-area polish or Linux/device gameplay acceptance.

## Scope and attribution

The reviewer inspected saved artifacts and actual screenshots, ran independent read-only archive/receipt/pixel checks, and did not operate Unity, Git, a game executable, package creation or restoration. Root built and played the application. The reviewer authored this phase's finite test fixture and adapted its packaging helper; those source reviews are self-review. The archive audit below used a separate read-only stream comparison, not an invocation of that helper. Runtime and native-art authors were separate agents.

Source is `a10e1330098c68039e560740cde07137dadc2ff3`. Remote presence is the root operator's recorded verification, not a new Git check by this reviewer. The source-freeze receipt, successful output paths, receipt hashes and ordering agree. This does not independently prove a reproducible source-to-binary build.

## Fresh build and Linux archive integrity

The source freeze is `2026-10-27T19:37:39.169969+00:00`. The Mac receipt starts after it at `19:37:39.213796Z`, succeeds after **63.792429 seconds**, and reports **344.4 MB, zero errors and four warnings**. Linux starts at `19:39:11.507344Z`, succeeds after **59.206929 seconds**, and reports **324.86 MB, zero errors and four warnings**. Warnings are not silently treated as zero.

Verified fresh paths:

- Mac: `Builds/Verification/2026-10-27-shale-caves-a10e13300-current/CavesOfOoo.app`.
- Linux tree: `Builds/SteamDeck/2026-10-27-shale-caves-a10e13300-current/CavesOfOoo-current`.
- Archive: `Builds/SteamDeck/2026-10-27-shale-caves-a10e13300-current/CavesOfOoo-SteamDeck-flat16x24-current.tar.gz`.

The archive is **128,807,783 bytes**, SHA-256
`a5af2a83280e7e891667d7e9117c947f058d314b39abb1efe7d23be96f35c9f7`.
Independent verification read every tar entry and file payload, compared the complete archive and unpacked-tree path sets against the receipt, and checked every file hash/size/mode and every directory mode. All **202 members = 189 regular files + 13 directories** matched. Regular payload totals **341,137,101 bytes**. No absolute/traversal paths, duplicate members, symbolic/hard links or unsupported entry types were admitted. The separate gzip reader drained the complete **341,463,040-byte** decompressed tar stream to EOF, validating its CRC, size and footer rather than stopping at the tar end marker.

All **188 internal SHA256SUMS entries** cover every other file exactly. Archived build-info equals the external build-info receipt; archived Linux and Mac reports match their external receipt bytes and pinned hashes. The ELF header is 64-bit little-endian x86-64 and the executable/launcher modes match; neither was executed. Six credit texts, five license texts, starter-spell sources, provenance files and the README are present and hash-covered. This is an integrity/presence check, not a new legal audit. The previous limestone archive still matches its earlier recorded SHA-256.

The two native XMLs were independently rehashed and parsed: **36 + 431 = 467 distinct, disjoint Passed identities**, with zero failed, skipped or inconclusive cases. They are earlier root-run EditMode evidence, not tests rerun by packaging or this review. Pins:

- `shale-caves-native-green.xml`: `2ebd61f7f19d5649183710da71754d88d4dce1c39e987e03ae5014ff9b658e09`.
- `shale-caves-related-green.xml`: `73aa27761ff010c25c7932afa748a8bad73b8ef57c966a2cc0bc657082dc943f`.

## Actual Mac image review

Opened all seven JPEGs directly. Each is **1280×856**, including the 56-pixel window title area, and each has distinct file bytes. These are actual standalone images, not concepts or contact sheets.

| Capture | Visible evidence and bounded judgement |
| --- | --- |
| 01 restored wreckage | Player at `[39,12]`, HP33/40, focus lists Rubble and You. Native shale walls and quiet floor remain distinct. The inherited wall-strike/destruction log is not new packaged destruction. |
| 02 Rubble reader | Actual `[39,12]` Rubble reader has a clear native pile portrait, readable description and Terrain type. World Rubble remains visible beneath the modal; no new reader clipping is apparent. |
| 03 limestone return | Paler limestone floor/walls and down-stair cell are visibly distinct from shale. Focus reports `[39,13]`, limestone floor/stairs/You and HP33. |
| 04 shale reentry | Darker layered shale returns consistently at the up-stair cell `[39,13]`; the previously created Rubble remains visible north of the player. |
| 05 depth6 entry | Broad disclosed shale chamber, continuous native ground, dark wall bands, native Rubble/Stalagmite/stair shapes and separately unresolved actor/vegetation glyphs. HP remains33. This is one viewport, not the full depth6 area. |
| 06 saved | Return to depth5 down-stair cell `[40,13]`, HP33, game-saved log. Quiet floor/backdrop and surrounding wall/Rubble composition remain coherent. |
| 07 fresh restart | Same `[40,13]` focus, HP33 and visible world arrangement after the root-reported new-process Continue. Log layout differs; world comparison is explicitly bounded below. |

Root reports first process PID58078, full quit with process absence checked, then PID58510 and ordinary Continue. The reported journey is earned fixture95 → Rubble reader → **5→4→5→6→5**, followed by F5 fixture97 at turn12590, HP33 and `[40,13]`. The first combined control sequence missed Look and returned to `[39,12]`; separated keys then reached the actual reader. The miss is not counted as successful examination. This review did not independently issue those controls or inspect live PIDs. No fresh packaged wall destruction or combat is claimed; old log entries come from the earned fixture.

Independently decoded images06 and07 as RGB and compared the exact rectangle **`[0,96,940,696]`**: **564,000 pixels, zero changed pixels, no difference bounding box**. This supports unchanged captured world presentation across this save/restart, not equality of every UI pixel or all underlying simulation state. It is separate from the earlier Editor07/08 comparison, which had8,729 changed pixels and was not called identical.

The flat native materials and Rubble reader are readable at the captured normal zoom. Whole-view limitations remain obvious: repeated rubble and wall laminae, broad sparse chamber patches, generic wall/vegetation/actor glyphs, and incomplete props, services and hazards. Brown/dark undisclosed or remembered areas are not counted as bare explored chunks. The current seven images do not justify full80×25, all-column, worldwide or polished-region acceptance. No physical Deck, Linux execution, new damage/Wet/death interaction, audio-quality or performance/FPS result is established here.

## Evidence pins and pending final receipts

| Receipt | SHA-256 |
| --- | --- |
| source-freeze | `d0916055850d156c79d4949ae3e924f0495c74059de2d584f6cad08ccb40fa27` |
| Mac build | `5c313326c2578271a1dfc043c479e75371b0a2c31024fbebf63a5bd765eae162` |
| Linux build | `971352663fcbeabfdf9cf7537839e95ff17b37885ad46f750c1ad806c3eb6b39` |
| Linux build-info | `1574fcbe8ea376f6a3a962bc42bd8753f57c1031eba177e9619a5a7c4bd71612` |
| Linux archive receipt | `3f851e583ae8b9b30bd60253619d5fa99eb727e2d4b573fff8efe4831915fd94` |

At initial write, `packaged-shale-caves-mac-play.json` and `packaged-shale-caves-restoration.json` were absent. Final log/state and normal-save/preference/foreign-work restoration acceptance remains pending those receipts. No cleanup or restoration was performed by this reviewer.

## Final play, log and restoration verification

**The two pending gates above are now closed for this bounded delivery review.** Read the final Mac play, reviewed-restoration, restoration and owned-plan-preservation receipts. Their source revision, capture hashes, fixture hashes and archive reference agree with the independently checked artifacts above. No additional significant package discrepancy was found.

Independently rehashed all seven captures, both retained player logs and both earned fixture97 files. `Quick.json` confirms `Overworld.16.13.5`, turn12590 and HP33/40; screenshots and root's observation supply cell40,13. The saved compressed simulation was hash-checked, not independently deserialized for owner IDs. The prior independently decoded06/07 world comparison remains zero differences across564,000 RGB pixels. The actual PID/quit/Continue account is supported by the root receipt and exact new-app paths in both logs; this reviewer did not query live processes.

Read both full logs. `Player-prev.log` records transitions4→5→6→5; `Player.log` records the subsequent separate6→7→6 baseline route. Both identify the fresh shale app path. They contain **five and six thread-finalization warnings respectively, plus one ComputeBuffer disposal warning in each**. No exception line is present in these two retained logs; this does not establish absence of every possible runtime fault, and the warnings are not called harmless or resolved. The next-phase slate7 entry/return and fixture98 at turn12620/HP33 remain baseline evidence only: no depth8 visit or slate material acceptance is credited to this package.

The restoration receipt reports application at `2026-10-27T19:44:55.668983+00:00`, exact equality of **51,317 tracked snapshot objects** and preservation of **48 preexisting dirty files**:46 foreign files plus the two previously owned shallow/limestone importer whitespace edits. The reviewer did not repeat the entire51,317-object sweep. The following narrower protection checks were independently repeated read-only:

- All48 dirty objects match their snapshot type, bytes, mode and hashes.
- All five generated files now match their snapshot, and their archived pre-restoration generated bytes match the exact reviewed hashes: shale PNG metadata, UniversalRP, URP global settings, ProjectSettings and UnityConnectSettings.
- Both normal save trees match the prebuild snapshot, including original absence of normal standalone Saves. The separately archived test save tree exactly matches the reviewed manifest.
- Both gameplay preference domains have the same logical values as their saved baseline; no preference contents are reproduced here.
- All three prior-current package objects—the limestone Mac tree, Linux tree and archive—match the protected snapshot.
- `Docs/GRAPHICS-SHARED-STORAGE.md` and its held backup have the exact same reviewed SHA-256 `e5c6da7a8fc35a0941da963dcb6a92edff762da2eac9c8b251ea905fcc2b52e5`.

The last item is an intentional post-snapshot source-plan edit. Root preserved its bytes, temporarily restored the snapshot version for the narrow generated-file restorer, then reapplied the preserved plan. Thus the receipt's full-snapshot equality describes the restoration boundary; it is not a claim that the subsequently reapplied authorized plan is still snapshot-identical. The dedicated preservation receipt and current/backup hashes make that distinction explicit. No source/pixel/GUID change or drive/cache cleanup is attributed to restoration. Root's Editor-idle assertion remains operator evidence.

Final receipt pins:

| Receipt | SHA-256 |
| --- | --- |
| Mac play | `7b5883eb76f4229c11c26e254f0c6f85905fd760ea99137545c2391d065677e3` |
| Restoration reviewed manifest | `9c2ff0615ce011814f5eb336896488f372cc2ff6beed7fd3d10a24a67db89307` |
| Applied restoration | `604249037f75d1e89c29dcb81595c27d0f9f6aa8afdbf7bc744f8efd5f1491ed` |
| Owned plan preservation | `977bd97d5a852863902ca94b110869302bee5c54db0fbf40b574368f7354cfec` |

Final result remains **PASS for fresh archive integrity, the documented ordinary Mac journey/restart and scoped protection checks**, with the retained warning and coverage limits above. No Linux execution, physical Deck, full-area polish or performance acceptance is added.
