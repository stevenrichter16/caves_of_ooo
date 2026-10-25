# Independent flat16x24 package review

Read-only review completed 2026-10-25 02:24 UTC. Runtime source is `9ffd7c973a82d1b0fa351300f2b981d703694885`; packaging/documentation push is still pending. This review writes only this report. It did not extract or execute the archive, control Unity, alter saves/source/settings, or delete anything.

## Archive verification

Reviewed `Builds/SteamDeck/2026-10-25-flat16x24-current/CavesOfOoo-SteamDeck-flat16x24-current.tar.gz` independently of the coordinator's earlier check:

- **128,610,942 bytes**, SHA256 **76806cdcaf37c49883820f160fa2076963760b9d27fa5d27c4f243127c586bd6**; matches `packaged-flat-linux-archive.json`.
- Streamed all **181 regular files**, totaling **225,309,796 unpacked bytes**. Every file's digest, size and mode matched the receipt.
- `SHA256SUMS` covers exactly the other **180 files**, with no missing/extra path and every digest matching freshly streamed bytes. The checksum file itself matches the outer receipt.
- Members stay under `CavesOfOoo-current`; no absolute or traversal paths, symbolic links or hard links were found.
- Launcher and `CavesOfOoo.x86_64` have mode **0755**. Launcher and README are byte-identical to the current repository sources. Launcher changes into its own directory, preserves an explicit caller handheld choice, defaults `COO_HANDHELD=1`, forwards arguments and invokes the correct executable name.
- Executable header identifies **ELF64 little-endian x86_64**. `UnityPlayer.so`, `CavesOfOoo_Data/data.unity3d` and the **9,353,216-byte `CavesOfOoo_Data/Managed/CavesOfOoo.dll`** are present. Presence/hash checking is not Linux execution or dependency resolution.
- Shipped raw `build-report.json` is byte-identical to `packaged-flat-linux-build.json`; its data matches build-info: job `build-ab3961cea9`, successful StandaloneLinux64, **0 errors / 4 warnings**, completed 02:18:41 UTC. Build-info identifies the correct runtime commit, normal SampleScene, Mono/Vulkan, no development build and honest unverified Linux/Deck limits.

## Credits and instructions

All six individual World credits (`CREDITS`, `PLACES-CREDITS`, `ATMOSPHERIC-CREDITS`, `UTILITY-CREDITS`, `REGIONAL-MUSIC-CREDITS`, `DEFENSE-CREDITS`) match current source bytes and each appears intact in combined `AUDIO-CREDITS.txt`. All five license texts, five provenance JSONs and `STARTER-SPELLS-SOURCES.md` also match their repository sources byte-for-byte. This is provenance retention, not a new legal review or audit of every compiled sound.

README now accurately names flat16x24 as the default when there is no saved explicit preference, identifies the six named sites and unfinished world coverage, and gives the actual **World view: 16x24** label. It tells users to update the shortcut to the new directory, retain the complete folder and leave forced Proton off. Physical Deck operation and performance are explicitly not certified.

## Bounded Mac evidence

The separately reviewed Mac build succeeded with 0 errors / 4 warnings. Actual standalone captures show the populated flat world, a moved player and the claspbean examine portrait; the Player log reaches normal blueprint/bootstrap/renderer initialization. The binary is universal x86_64+arm64. Shutdown logs include thread-finalization and ComputeBuffer disposal warnings, so warning-free execution must not be claimed.

CUA capture payloads originally saved with `.png` names are JPEG at 1280x856 including the 56-pixel macOS title bar; the gameplay viewport is 1280x800. These are ordinary app captures rather than raw Unity render exports. Root's restoration receipt reports all 770 original save paths/hashes and both preference mappings restored; this reviewer did not traverse private saves independently. Mac evidence cannot prove Linux graphics, controller mappings on a physical Deck, performance or whole-world visual completion.

## Storage and small packaging note

At 02:24:16 UTC, free space was **3,571,830,784 bytes (3.33 GiB)**. The old Oct11 archive remains **128,049,954 bytes**, independently rehashed to `debc85bfacc1c018b24dc8c742b35956768c131571df0e0cb01221a9badf001b`.

The old `2026-10-11-fullscreen-skills-current` directory has **no unpacked player output** to reclaim. Its remaining contents are the protected archive/checksum and one 235,639-byte historical Burst text diagnostic. Preserve those rather than claiming a meaningful cleanup. The new Mac package is the current executable acceptance artifact, and the new Linux unpacked output is the current delivery source: neither is a stale build. Active/useful editor and build caches have no confirmed unused candidate. **Recommended worthwhile reclaimable bytes: 0; removed files/bytes: 0.** Source, saves, checkpoints and both archives remain protected.

Low-priority packaging observation: the new tar also contains `caves-of-ooo_BurstDebugInformation_DoNotShip/Data/Plugins/lib_burst_generated.txt` (235,639 bytes). It is unnecessary runtime diagnostic text, not a blocker or meaningful disk saving. It can be excluded in a future packaging-only pass while retaining the local diagnostic; doing so requires regenerating checksums/archive receipts. Do not delete or silently change the already verified archive.

## Verdict

No significant package-integrity, launch-script, missing-credit or source-label defect found. The package is structurally ready for transfer with the stated bounds. Linux execution and physical Steam Deck acceptance remain open; the review does not claim that the full graphical release is complete.
