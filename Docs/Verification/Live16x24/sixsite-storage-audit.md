# Six-site delivery: post-push storage and package audit

## Verified push and preservation

At 2026-10-25 02:06:16 UTC, local HEAD and a fresh read-only `git ls-remote origin refs/heads/main` both returned **9ffd7c973a82d1b0fa351300f2b981d703694885**. Root's `/tmp/coo-sixsite-push-result.json` records **81 owned files** byte-matched to this tip. This audit independently rechecked the tip equality, not all 81 blobs. Tracked modifications are still only the two unrelated AllotmentNotice PNGs; their original preserved hashes remain unchanged:

- `notice-hidden-control.png`: 120,508 bytes; SHA256 `b6b8a30da59ded391126a9f526052bb4da5acd98692b3587160be164b3a059db`.
- `notice-visible.png`: 120,837 bytes; SHA256 `3fb8194882da152137cf2f8493b3800f8a3c937e138798e742ce44237af6b811`.

Their paths are under `Docs/Verification/AllotmentNotice/ControlledRender/`. The Oct24 local-source/evidence archive and manifest, Oct13 historical-source archive (77,459,056 bytes), and `Builds/SteamDeck/2026-10-10-4c3b0c1d7/audit/Saves-before-build` remain present. The prepared, tempered, kitchen-final and tween-replay checkpoint directories in `/tmp/coo-*` remain present. This bounded check confirms presence, not a new exhaustive rehash of every historical source/save. No source, save, checkpoint, backup or build was changed or removed by this audit.

## Storage decision

Free filesystem space at that snapshot: **5,029,412,864 bytes (4.68 GiB)**. Free space fluctuated during read-only inspection; no increase is attributed to cleanup. Root reports Play stopped and normal state restored at 02:02:13 UTC; the Editor remains active. Useful/active caches remain in place:

| Path | Allocated usage (`du -sk`, KiB) | Decision |
|---|---:|---|
| `Library/Bee` | 211,748 | Preserve; active editor/build cache |
| `Library/BurstCache` | 117,832 | Preserve; regenerable but useful to next build |
| `Library/Search` | 61,008 | Preserve while editor is active |
| `Temp` | 11,768 | Preserve active session data |

**Confirmed worthwhile safe reclaimable candidates: none. Files removed: 0. Bytes freed: 0.** Existing build directories also contain receipts, scripts, diagnostics and save backups; a folder date is not evidence that its entire contents are disposable. Previously deleted archives are not counted again. There is no reason to purge useful caches immediately before packaging. No broad disk scan, downloads, Unity controls or deletion was performed.

## Current delivered package retained exactly

`Builds/SteamDeck/2026-10-11-fullscreen-skills-current/CavesOfOoo-SteamDeck-fullscreen-skills-current.tar.gz`

- Size: **128,049,954 bytes**.
- SHA256, freshly recomputed: **debc85bfacc1c018b24dc8c742b35956768c131571df0e0cb01221a9badf001b**.
- Streaming archive inspection found 197 regular files totaling 224,464,208 logical unpacked bytes; nothing was extracted.
- Embedded build-info identifies source **244177619cffae76fb452cce40e3c49cc5dfcb2f**, Unity 6000.3.4f1, Linux x86_64, built 2026-10-12T04:10:18Z. It records no physical Steam Deck test.

This is the previous delivered build. It does **not** contain the newly committed six-site graphics work and must not be renamed or presented as an updated package.

## Smallest next delivery step

The required player modules are physically installed; no download is needed:

- Linux Mono: `/Applications/Unity/Hub/Editor/6000.3.4f1/PlaybackEngines/LinuxStandaloneSupport/Variations/linux64_player_nondevelopment_mono`. Linux support is outside `Unity.app`; checking only the app's PlaybackEngines directory would falsely report it missing.
- Mac x86_64 Mono: `/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/PlaybackEngines/MacStandaloneSupport/Variations/macos_x64_player_nondevelopment_mono`. This matches root's fresh tool read: target StandaloneOSX, Mono, architecture x86_64/raw 0.
- Mac ARM64 Mono: `/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/PlaybackEngines/MacStandaloneSupport/Variations/macos_arm64_player_nondevelopment_mono`.

Project version is 6000.3.4f1. Enabled production scene is `Assets/Scenes/Main/SampleScene.unity`. Linux graphics APIs explicitly select Vulkan. Existing MCP `manage_build` supports `osx` and `linux64`, explicit scene/output and nondevelopment builds; no new builder implementation is needed. Root should capture actual backend/architecture/settings before switching target and restore them afterward.

1. Build the exact verified tip into a new, distinct **Mac Mono** output using ordinary SampleScene, no audit scene/runtime configuration. The current x86_64 architecture has an installed matching player; retaining it avoids a settings change, although launch on Apple silicon requires working Rosetta (not verified here). ARM64 is an installed alternative for a native launch but requires a deliberate temporary architecture change and restoration; do not silently assume the project already selects it. Use a nondevelopment build and manual launch so save protection is established before input. Capture the actual packaged menu and world after the protection below; verify normal launch chooses flat 16x24 and the native graphics/reader appear. This is separate from existing Editor screenshots.
2. Build the same exact tip as **Linux x86_64 Mono/Vulkan** into a new date/graphics `-current` directory. Keep the Oct11 archive unchanged. Use the established executable basename `CavesOfOoo.x86_64` and production scene; avoid IL2CPP, parallel builds or `clean_build` without evidence they are necessary.
3. Include `Tools/SteamDeck/Launch-CavesOfOoo.sh`, current Steam Deck README, required license/audio attribution and build report. Preserve launcher executable mode. The launcher already sets `COO_HANDHELD=1`; it runs `CavesOfOoo.x86_64` from its own directory.
4. Record full commit/source status, build result/errors/warnings, platform/backend/graphics API, packaged files, actual Mac smoke evidence, Linux execution limits and SHA256 in fresh build-info. Archive and stream-validate the result before identifying it as the new current delivery. Do not feed these unrelated graphics receipts through milestone-specific `package-audio-polish20.py` or other old audio validators and claim their contracts were satisfied.

The retained package is ~224 MB unpacked and current Resources sources total ~220 MB (13,350 files including metadata). Sequential fresh Mac then Linux outputs plus a compressed archive appear plausible with ~4.7 GiB available. **Peak target-import, shader/Bee scratch, and current output size are unmeasured**; this is feasibility, not guaranteed capacity. Check free space between phases, avoid unnecessary clean rebuilds and retain the current archive. An unexplained disk decrease is a reason to inspect the exact growing output, not to delete source or useful active caches.

## Save protection and acceptance limits

`SaveSystem.cs` uses `Application.persistentDataPath/Saves` unless the in-process `SaveRootOverride` is set. Editor NativeSaveIsolation and requested-seed helpers do not isolate a separately launched package. No package `COO_SAVE_ROOT`/CLI save override was found. Before packaged New/Continue, root must back up and restore the actual user saves and relevant LastGameID/preferences, preserving any new acceptance checkpoint separately. A boot-menu-only smoke is safe bounded evidence but does not establish gameplay acceptance. Do not assume an undocumented environment variable or `-user-data-dir` works.

The new package has not been built or run by this audit. Existing native tests and real Editor journey evidence do not prove packaged launch. Linux cannot be executed natively on this Mac; actual physical Steam Deck behavior/performance remains unverified until tested there. No whole-world/release completion is claimed.

Read-only automation check: `overnight-clearing-art-pipeline` remains **ACTIVE**, with `RRULE:FREQ=MINUTELY;INTERVAL=10`. No schedule change was made.
