# G1 — Packaged flat 16×24 delivery

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
