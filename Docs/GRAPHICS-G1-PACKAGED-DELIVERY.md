# G1 — Packaged flat 16×24 delivery

**Latest delivery: 2026-10-26 08:34 UTC**, source
`8902d37316a24e3debd21e161b970f93ef1fd9d7`. The
[current kitchen/hedge delivery](Verification/Live16x24/packaged-kitchen-hedge-report.md)
includes underfoot actions, hive/court, connected hedges and current inventory.
Both builds succeeded; ordinary Mac Continue, inventory/readers, underfoot
harvest and quit/Continue were observed. All181 Linux archive files verified.
Linux execution, physical Deck and complete opening/regional coverage remain open.
Older sections below are historical and retain their original source/limits.

**Status, 2026-10-25 UTC:** current-source Linux and macOS packages built successfully. An ordinary macOS standalone launch, Continue, crop Examine and movement were observed. The Linux archive is verified and ready for transfer; Linux execution, physical Steam Deck acceptance and fresh performance measurements remain open.

**Source:** `9ffd7c973a82d1b0fa351300f2b981d703694885`. This packaging slice introduces no intentional runtime changes after that source commit. It carries the accepted native opening art, connected six-site source work and reader leading correction into a new package. It does not certify every world owner or visual state.

## 1. Purpose and boundaries

The old October 11 package predates the current flat presentation and its accepted opening improvements. The goal is to make the verified source available as a normal standalone build, and to distinguish successful compilation and archive integrity from actual target-device execution.

The normal `Assets/Scenes/Main/SampleScene.unity` entry point is used. These are non-development builds. Flat 16×24 is the default when no explicit presentation preference exists; an existing explicit choice remains respected. The previous presentation and its assets remain available. There is no package-only forced presentation, editor isolation mode, teleport or grant.

This is Caves of Ooo's own packaging and visual acceptance work, not a claim of Qud implementation parity. The target remains the approved restrained flat 16×24 direction in [Live art direction](LIVE-16X24-ART-DIRECTION.md), with the remaining release order in [Graphics completion plan](GRAPHICS-COMPLETION-PLAN.md).

## 2. Verification sweep and corrections

| Initially ambiguous or misleading point | Verified correction |
|---|---|
| A successful Linux build proves Deck compatibility | It proves build completion only. Neither Linux nor physical Deck execution occurred in this slice. |
| The macOS output might be a single-architecture player | The actual binary is universal: `x86_64` and `arm64`. The observed run used Metal on Apple M5. |
| The first macOS launch could immediately Continue the editor save | The bundle-id player save location differed from the editor's location. The first launch showed the character picker. The process was quit normally, the already-earned checkpoint was copied into the player save location, and the next launch offered Continue. |
| Screenshot names ending in `.png` describe their encoding | CUA supplied JPEG bytes. The four files were renamed `.jpg` without transcoding; no image content was changed. |
| A 1280×856 capture implies a taller game viewport | The native window includes 56 pixels of macOS chrome. The game viewport is 1280×800. |
| Successful play means a clean shutdown log | The log contains premature-thread-finalization notices and a ComputeBuffer disposal warning during shutdown. No fatal gameplay exception was observed. |
| Restoring saves also proves build-generated project settings were restored | These are separate checks. Save hashes and preference mappings have their own receipt; the later editor restoration receipt and empty `Assets`/`ProjectSettings` diff establish the separate project cleanup. |

## 3. Build and delivery receipts

| Build | Actual result | Build duration | Reported output size | Errors / warnings |
|---|---|---:|---:|---:|
| macOS `StandaloneOSX` | Succeeded; universal `x86_64` + `arm64` | 68.750329 s | 233.93 MB | 0 / 4 |
| Linux `StandaloneLinux64` | Succeeded; ELF x86-64 | 96.135980 s | 214.41 MB | 0 / 4 |

The macOS build ran from `02:07:45.702896Z` to `02:08:54.453225Z`; Linux ran from `02:17:05.468809Z` to `02:18:41.604789Z`. These durations are build times, not gameplay performance results. See [Mac build receipt](Verification/Live16x24/packaged-flat-mac-build.json) and [Linux build receipt](Verification/Live16x24/packaged-flat-linux-build.json).

Outputs:

- macOS: `Builds/Verification/2026-10-25-flat16x24-current/CavesOfOoo.app`.
- Linux directory: `Builds/SteamDeck/2026-10-25-flat16x24-current/CavesOfOoo-current/`.
- Linux executable: `CavesOfOoo.x86_64` inside that directory.
- Transfer archive: `Builds/SteamDeck/2026-10-25-flat16x24-current/CavesOfOoo-SteamDeck-flat16x24-current.tar.gz`.

The Linux package uses Unity `6000.3.4f1`, Mono and the configured Vulkan graphics backend. Vulkan is configuration evidence here, not a witnessed Linux renderer. The current README names the new flat build, explains existing presentation preferences, and points Steam's executable and working directory at this new extracted folder. Keep the entire extracted folder together. The old October 11 archive was preserved.

The [Linux build information](Verification/Live16x24/packaged-flat-linux-build-info.json) records the source, configuration, README checksum and acceptance limits. Its references to prior native reader tests and editor play are source verification; they are not tests run inside the Linux executable.

### Archive integrity

The [archive receipt](Verification/Live16x24/packaged-flat-linux-archive.json), recorded at `02:20:50.327610+00:00`, verifies:

- Archive size: **128,610,942 bytes**.
- SHA-256: `76806cdcaf37c49883820f160fa2076963760b9d27fa5d27c4f243127c586bd6`.
- **181 regular files**, totaling **225,309,796 unpacked file bytes**.
- Every archived file's hash, size and mode matched its packaged source; no unsafe paths or links were present.
- The Linux executable and UnityPlayer library retain executable mode `0755`.
- Audio credits and the relevant CC-BY, CC0 and Kenney license texts are included, along with starter spell source information.

An independent read of the archive matched the receipt's byte count and SHA-256. This establishes that the named transfer file matches the inspected package; it does not establish that it runs on a Deck.

## 4. Actual macOS standalone play

Evidence: [play summary](Verification/Live16x24/packaged-flat-mac-play-summary.json) and [standalone player log](Verification/Live16x24/packaged-flat-mac-player.log).

The first launch showed the character picker because the standalone bundle used its own save location. After a normal quit, the existing earned six-site checkpoint was copied into that location. On relaunch the game offered Continue. Actual keyboard `C` resumed it, with the native kitchen court, player, crop and accepted actors visible in the normal game view.

The checkpoint file hash was `c9add40b3996a82f5d7d9d196e78ba19c7c7a29e1a25ceec33f0e7dfd701db56`. This was the already-earned checkpoint documented by the [connected six-site journey](GRAPHICS-G3-SIXSITE-JOURNEY.md), not a newly spawned or edited test character.

Ordinary keyboard interaction opened the nearby crop's action/Examine flow, displayed the Claspbean reader, closed it and moved the player right from `(25,14)` to `(26,14)`. The resumed character remained at 42/42 HP in the captured play. The crop reader shows the native portrait and the accepted increased body leading. No gameplay script, state grant, teleport or forced presentation mode was used to produce these screens.

All four captures are the original CUA JPEG bytes. Their dimensions are 1280×856 including window chrome, with a 1280×800 game viewport. Their byte counts and hashes were independently checked against the play summary: **4/4 match**.

| Actual screen | What it proves | Screenshot |
|---|---|---|
| Relaunch Continue prompt | The standalone recognizes the copied earned checkpoint and exposes its normal Continue control. This is not the initial character-picker launch. | [Continue prompt](Verification/Live16x24/packaged-flat-mac-start.jpg) |
| Continued kitchen | The normal standalone renders the native working court and resumed character at `(25,14)`. | [Continued play](Verification/Live16x24/packaged-flat-mac-continued.jpg) |
| Crop Examine | The standalone opens the actual Claspbean reader with portrait, text and facts. | [Crop reader](Verification/Live16x24/packaged-flat-mac-reader.jpg) |
| Closed reader and moved | Ordinary input returns to the world and moves to `(26,14)`. | [After movement](Verification/Live16x24/packaged-flat-mac-moved.jpg) |

The log shows ordinary bootstrap, renderer/input wiring and action-menu activity. It also records shutdown notices about prematurely finalized threads and `GarbageCollector disposing of ComputeBuffer. Please use ComputeBuffer.Release() or .Dispose() to manually release the buffer.` This remains a named resource-lifecycle follow-up. Its origin and practical impact were not established by this smoke check; the run must not be described as warning-free.

## 5. Restoration and reproducibility

The [save/preferences restoration receipt](Verification/Live16x24/packaged-flat-mac-restoration.json) was recorded at `02:16:21.771032+00:00`:

- All **770 original save files**, totaling **93,965,260 bytes**, retained their original paths and hashes after restoration.
- The logical preference mappings for `com.DefaultCompany.2D-URP` and `unity.DefaultCompany.caves-of-ooo` were restored.
- Temporary earned checkpoints remain outside the repository for bounded continuation.

Those checks establish preservation of the original saves and preferences. They do not erase the fact that the standalone proof required placing the existing checkpoint in its distinct save location.

The separate [editor restoration receipt](Verification/Live16x24/packaged-flat-editor-restored.json) at `02:23:11.806286Z` confirms the normal `SampleScene`, `StandaloneOSX`, no Play/build/compile activity, no isolation and a null save-root override. Root reviewed and preserved the transient macOS/Linux generated diffs outside the repository, normalized 23 metadata files whose changes were whitespace-only, and restored four generated configuration files to HEAD. A subsequent `git diff -- Assets ProjectSettings` was empty; this documentation review independently confirmed its empty diff summary. The build used Unity's transient serialization of URP references/prefilters, Input System preloading and Standalone settings. Those generated changes are disclosed rather than represented as intentional runtime edits to source `9ffd7c973`.

To reproduce the bounded packaged proof, launch the normal macOS output with the earned checkpoint available in the player's save location, use Continue, open the nearby crop's actual Examine action, close it and move one cell. To test Linux/Deck, extract the new archive, use its native executable and working directory, then repeat those actions on that target. Merely copying the archive does not close those execution gates.

## 6. Acceptance and honesty bounds

### Verified from receipts and actual observation

- Both current-source builds completed with zero errors and four reported warnings each.
- Archive content, paths, modes, byte count and checksum match the packaged Linux files.
- The macOS binary is universal and the observed standalone run used Metal.
- A normal standalone Continue, actual native world rendering, crop reader and one-cell movement worked at the captured viewport.
- Four untouched captures match their recorded hashes.
- Original saves and preference mappings were restored as recorded; the separate editor/project restoration check also completed.

### Still open

- Linux execution, Vulkan behavior on Linux, physical Steam Deck controls, display, audio and performance.
- A full connected six-site journey inside either packaged executable; that journey was previously performed in the editor.
- A new-character full journey, every input path, all visual states and all world art.
- A fresh frame-time, memory, loading or battery benchmark. Build duration and an interactive screenshot are not performance measurements.
- Shutdown resource-lifecycle warnings.
- Physical-device readability and comfort. The actual Mac captures support a bounded visual review, not a claim about Deck viewing conditions.

The existing reader native suite reported 108 distinct passing cases with no failures or skips before packaging. It is useful source evidence and is not counted again as Linux or standalone test execution. This delivery documentation introduces no production behavior and adds no new behavioral test claim.

## 7. Self-review and continuation

- **🟡 Resolved — stale package naming.** The new delivery folder ends in `-current`, and the archive is named `CavesOfOoo-SteamDeck-flat16x24-current.tar.gz`; the package README now describes the current flat presentation. The older archive is retained separately.
- **🟡 Resolved — misleading screenshot extension.** Original JPEG captures use `.jpg`; no recompression or visual editing occurred.
- **🧪 Open — target-device acceptance.** A successful Linux build and byte-verified archive are not physical Deck execution. Keep this gate open until the actual target is tested.
- **🧪 Open — shutdown cleanup.** Preserve the ComputeBuffer/thread notices as a named investigation; do not attribute a cause or claim a repair from this delivery pass.
- **🟡 Resolved — generated settings cleanup.** Normal editor state was restored at `02:23:11.806286Z`; `Assets` and `ProjectSettings` have no tracked diff. This is separate from the completed save/preferences restoration.
- **⚪ Scope bound.** No new runtime implementation or new art packet belongs to this packaging slice. Existing art and presentation preferences remain available.

The next bounded visual correction is the **prepared-bed readout cue** found on the ordinary reserve route: after a real harvest, the historical decorative name “cut emberwheat row” can mask the current cultivated-bed context. Keep that historical name, disclose current bed state through the shared readout, and verify the change with native RED/counters plus ordinary play. Do not add unrelated props or new mechanics to address this disclosure issue. Package/device gates above remain separate.

## 8. Files and implementation log

- `Docs/GRAPHICS-G1-PACKAGED-DELIVERY.md`: this bounded build/play/delivery record.
- `Docs/Verification/Live16x24/packaged-flat-mac-build.json`: macOS build result.
- `Docs/Verification/Live16x24/packaged-flat-mac-play-summary.json`, four linked JPEGs and `packaged-flat-mac-player.log`: actual standalone input, pixels and log evidence.
- `Docs/Verification/Live16x24/packaged-flat-mac-restoration.json`: original save/preferences restoration.
- `Docs/Verification/Live16x24/packaged-flat-editor-restored.json`: normal editor state after build-generated project cleanup.
- `Docs/Verification/Live16x24/packaged-flat-linux-build.json`, `packaged-flat-linux-build-info.json` and `packaged-flat-linux-archive.json`: Linux configuration, successful build and exact transfer archive verification.

Implementation in this slice is packaging and evidence preservation. No runtime snippet is introduced. The plan's source acceptance is carried forward, while target execution and broader graphics release acceptance remain explicitly unfulfilled.


## 9. Fresh delivery — 2026-10-26 04:05 UTC

**Frozen runtime source:** `c893ae58fea438d1d2bb6f803ace1e499df79acd`.
The October26 delivery carries the intervening popup sampling fix,inventory
redesign/combat column,native equipment art and bounded Nella/north composition
work. This is a normal non-development `SampleScene` build with the existing
flat default and reversible presentation preference. No new artwork or gameplay
behavior is introduced by this packaging pass.

| Platform and receipt | Result | Duration | Reported size | Errors/warnings |
| --- | --- | ---: | ---: | ---: |
| [macOS](Verification/Live16x24/packaged-flat-20261026-mac-build.json) | Succeeded; universal x86_64+arm64 |46.919501s|233.97MB|0/4|
| [Linux](Verification/Live16x24/packaged-flat-20261026-linux-build.json) | Succeeded; ELF64 x86-64 |33.879608s|214.47MB|0/4|

New outputs, separate from the preserved October25 packages:

- Mac: `Builds/Verification/2026-10-26-flat16x24-current/CavesOfOoo.app`.
- Linux executable: `Builds/SteamDeck/2026-10-26-flat16x24-current/CavesOfOoo-current/CavesOfOoo.x86_64`.
- Transfer archive: `Builds/SteamDeck/2026-10-26-flat16x24-current/CavesOfOoo-SteamDeck-flat16x24-current.tar.gz`.

The [archive receipt](Verification/Live16x24/packaged-flat-20261026-linux-archive.json)
records **128,641,647archive bytes**,181regular files and225,372,292unpacked file
bytes. SHA256:
`aa64b959de5c477f3dd10893f4207d4c81f469afe816cdd46f6f5a9b108b7618`.
Every file's hash,size and mode matched; no unsafe paths or links were admitted.
A separate read independently matched the archive size and SHA256. Launcher,
README,build report,all six audio-credit sections,license texts,provenance and
starter-spell source notes are included. The
[build information](Verification/Live16x24/packaged-flat-20261026-linux-build-info.json)
records Mono and configured Vulkan; Linux execution was not observed. It
explicitly avoids recycling historical native-test or Mac-smoke claims.

### Current actual Mac smoke test

The [reviewed play report](Verification/Live16x24/packaged-flat-20261026-mac-play-report.md)
and [hashed summary](Verification/Live16x24/packaged-flat-20261026-mac-play-summary.json)
record two ordinary Mac/Metal sessions. Six original JPEGs are1280×856 with
56pixels of macOS chrome and a1280×800 game viewport:

1. Normal launch offers Continue. C resumes the already-earned north checkpoint
   at73,17,15/40HP and50drams. This is not the separate40HP Nella replay.
2. I opens the current inventory. Long sword shows`1d8`,buckler`1 AV +1 DV`,
   and each equipped item has`[E]`at the far right. F1 opens the sword's native
   portrait and decision details on a solid backing. Closing menus and ordinary
   movement reach72,17; F5 saves, then the player quits normally.
3. A separate fresh Warden starts in the flat opening and moves40,12→41,12.
   The retained new-character save records tick20 and40/40HP. This is fresh
   startup/input evidence, not a full packaged six-site journey.

Direct captures: [inventory](Verification/Live16x24/packaged-flat-20261026-mac-inventory.jpg),
[item reader](Verification/Live16x24/packaged-flat-20261026-mac-reader.jpg),
[fresh opening](Verification/Live16x24/packaged-flat-20261026-mac-new-game.jpg).
Both player logs complete bootstrap and Input System shutdown, with no explicit
exception/error/failed/fatal line in the review's keyword scan. **Both retain
six premature-thread-finalization notices and one ComputeBuffer-disposal
notice.** No warning-free shutdown,diagnosed cause or cleanup fix is claimed.
Build warnings are reported but not individually identified in the player logs.

The [Mac restoration receipt](Verification/Live16x24/packaged-flat-20261026-mac-restored.json)
records restored preference domains and removal of the temporary bundle save
location,which did not previously exist. Archived original/restored preference
maps independently compare equal; earned and fresh checkpoints remain outside
the repository. Separate [editor restoration](Verification/Live16x24/packaged-flat-20261026-editor-restored.json)
at04:08:34.54461UTC confirms StandaloneOSX,normal SampleScene,no
Play/compile/import/build/isolation/art launch/borrowed view and null save root.
The [project restoration](Verification/Live16x24/packaged-flat-20261026-project-restored.json)
records29reviewed generated files:26whitespace-only sprite metadata files and
3Unity build-serialization assets. Independent rereading matches all29restored
hashes,finds no tracked`Assets`/`ProjectSettings`diff,and confirms all four
protected normal editor save/backup hashes unchanged. These checks are separate
from Mac preference restoration.

Platform switching initially stalled in Mono domain unload for aboutroughly9minutes.
Root sampled the owned idle editor,stopped it with TERM,restarted targeting
Linux64,preserved the offered scene backups,completed the build and switched
back to OSX. This was operational recovery,not a source-code fix or proof that
the reload problem cannot recur. The successful Linux build and final restoration
receipts establish the recovered state; they do not erase the interruption.

### Scope and next action

Approximately430GiB is available. No cache/build deletion was needed or performed
in this delivery pass; older packages,sources,editable assets and saves remain
protected. Storage is no longer the reason to defer builds or longer playtests.

Linux execution,physical Deck controls/readability/audio/performance,the full
packaged journey,all owner/state art and new frame-time/memory/loading measures
remain open. Mac screenshots and archive integrity do not close these gates.
The next root-owned graphics design is an **uneven vegetation-drift composition
for the glade**,using the actual fresh-game capture above. Preserve functional
placements,stock and approaches. A design concept has now been generated,but
no new glade runtime changes,artwork or layout are admitted yet. Accepted
sprites remain stable while that proposal passes the existing actual
capture→concept→owner/state→native implementation→ordinary Play loop.


## 10. Kitchen/hedge current delivery — 2026-10-26

Source8902d3731 is now packaged; see the [complete report](Verification/Live16x24/packaged-kitchen-hedge-report.md),
[Mac play summary](Verification/Live16x24/packaged-kitchen-hedge-mac-play-summary.json)
and [Linux archive receipt](Verification/Live16x24/packaged-kitchen-hedge-linux-archive.json).
The previous dce87081d six-zone package remains preserved; this newer kitchen
replay does not replace its journey history with a new full-journey claim.

Current archive:
`Builds/SteamDeck/2026-10-26-kitchen-hedge-8902d3731-current/CavesOfOoo-SteamDeck-flat16x24-current.tar.gz`.
SHA-256:`80ebb5305d98d564369a9640d4c113d9979dc4d789d10e31cca12946b44c41e2`.
Both builds report0errors/4warnings. Known shutdown notices remain documented.
776 saves, both logical preference domains,24765 tracked source files and two
foreign PNGs were restored/verified. Storage inspection only; no deletion.


## 11. Meadow/water current delivery — 2026-10-26

The [current report](Verification/Live16x24/packaged-meadow-water-report.md)
freezes source2a8e71044 in dated Mac/Linux outputs. Both builds succeeded with
0errors/4warnings.17actual Mac images establish earned water crossing and
save/relaunch/return, inventory, fresh meadow generation/forge access and a
clean new-game start/movement. Older delivery sections remain historical.

Current archive:
`Builds/SteamDeck/2026-10-26-meadow-water-2a8e71044-current/CavesOfOoo-SteamDeck-flat16x24-current.tar.gz`.
SHA256:`e0ddfc99a23e4446db3e2bfbb65f2efb3e7805c95994eb5322cd46fddb6b32b3`.
All181files independently verified. Earlier outputs and normal saves remain
intact;32generated build files restored,47580prebuild tracked hashes checked.
No cache deletion. Fullopening/regional and physicalDeck/Linuxexecution gates
remain open; next is current-package Nella state/frontage review.
