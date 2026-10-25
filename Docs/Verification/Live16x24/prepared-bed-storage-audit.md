# Prepared-bed post-push storage audit

Read-only snapshot: **2026-10-25 02:57:14 UTC**. Local HEAD and fresh `git ls-remote origin refs/heads/main` both equal **37bbc54f4f5be5c6ad3eba3da37c6cd495cfbbd2**. Root reports 43 owned files byte-matched to that remote tip; this audit independently verifies tip equality, not all 43 blobs. Only the two previously preserved AllotmentNotice PNGs remain tracked modifications.

Free filesystem space: **3,826,450,432 bytes (3.56 GiB)**. Changes in free space are not attributed to this audit: **0 files deleted, 0 bytes freed**.

## Bounded measurements and decision

| Path | Allocated size (KiB, `du -sk`) | Decision |
|---|---:|---|
| `Library/Bee` | 501,540 | Preserve active/useful build cache |
| `Library/BurstCache` | 322,804 | Preserve new Mac/Linux build cache |
| `Library/Search` | 61,028 | Preserve active editor cache |
| `Temp` | 60,024 | Preserve active editor/session data |
| `Builds/SteamDeck/2026-10-25-flat16x24-current` | 346,004 | Preserve current archive and unpacked delivery |
| `Builds/Verification/2026-10-25-flat16x24-current` | 239,892 | Preserve current independently exercised Mac package |
| `Builds/SteamDeck/2026-10-11-fullscreen-skills-current` | 125,288 | Preserve explicitly protected prior delivery archive |

Unity processes remain active; root will resume Play. No active cache was deleted or interrupted. Growth of Bee/Burst caches after the two fresh builds is expected useful output, not proof of staleness. A bounded direct-child inventory of SteamDeck build folders found no superseded unpacked player: historical folders retain checksums, diagnostics, compiler source and/or save backups. The Oct11 current folder contains its protected archive/checksum plus a small historical Burst diagnostic; there is no large old player directory to reclaim. No confirmed worthwhile isolated unused temporary artifact was found. **Recommended safe worthwhile cleanup: none.** No broad disk crawl, download or Unity command was performed.

## Exact archive preservation

Fresh hashes match the prior accepted receipts:

- `Builds/SteamDeck/2026-10-25-flat16x24-current/CavesOfOoo-SteamDeck-flat16x24-current.tar.gz`: **128,610,942 bytes**, SHA256 **76806cdcaf37c49883820f160fa2076963760b9d27fa5d27c4f243127c586bd6**.
- `Builds/SteamDeck/2026-10-11-fullscreen-skills-current/CavesOfOoo-SteamDeck-fullscreen-skills-current.tar.gz`: **128,049,954 bytes**, SHA256 **debc85bfacc1c018b24dc8c742b35956768c131571df0e0cb01221a9badf001b**.

The new packaged runtime remains source `9ffd7c973a82d1b0fa351300f2b981d703694885`; it predates the prepared-bed follow-up. No rebuild, archive substitution or expanded package acceptance is implied by pushing the later source commit.

## Sources and unrelated work

Presence rechecked for `Docs/Archive/LocalWork-2026-10-24/local-source-and-evidence-variants.tar.gz`, its manifest, `Docs/Archive/LocalWork-2026-10-13/historical-source-snapshots.tar.gz`, and `Builds/SteamDeck/2026-10-10-4c3b0c1d7/audit/Saves-before-build`. This bounded check is not an exhaustive rehash of every source/save. All sources, editable artwork, saves and temporary acceptance checkpoints were left untouched.

Both unrelated images under `Docs/Verification/AllotmentNotice/ControlledRender/` retain their exact preserved bytes:

- `notice-hidden-control.png`: 120,508 bytes; SHA256 `b6b8a30da59ded391126a9f526052bb4da5acd98692b3587160be164b3a059db`.
- `notice-visible.png`: 120,837 bytes; SHA256 `3fb8194882da152137cf2f8493b3800f8a3c937e138798e742ce44237af6b811`.

Only this audit document was written. No staging, commit, source change or deletion was performed.
