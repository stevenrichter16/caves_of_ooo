# Hourly source and disk preservation audit — 2026-10-24, 15:00 UTC window

Read-only audit in `/Users/steven/caves-of-ooo`. No Unity operations, deletions, staging, fetches or repository edits. Main editor and import workers are running. Root owns remote verification and Unity.

## Recommendation

**No worthwhile additional deletion now.** The seven superseded packaged builds are already gone. The only remaining `.tar.gz` under Builds is the retained delivered Deck package. Existing build folders mainly contain source inputs, diagnostics, checksums and 700 save backup files; they must not be broadly removed. Active Unity caches have regenerated as expected. Deleting them again during work is unsafe or counterproductive and offers only temporary space.

Filesystem free space at inspection: approximately 3.8 GiB. The project Library is 3,246,269,291 logical bytes across 53,059 files. That total is not proposed reclamation: current imports, package dependencies and active databases occupy it. Logical byte counts do not equal APFS physically reclaimable space.

## Current source protections

- HEAD and local origin/main both `41cd8b4567696263996fca09dea4db325525f834`. This is local ref equality; root must confirm the server. No tracked code/config edits at this snapshot; the only tracked modifications are the two known AllotmentNotice PNG variants.
- All six entries in `Docs/Archive/LocalWork-2026-10-24/manifest.json` independently verified against both archive members and current original bytes. Includes both PNGs, ProBuilder settings and three historical packaging/launch scripts. The archive is tracked in main; original files remain in place.
- All 269 entries of the October 13 historical-source archive re-read and matched length/SHA256. Its local-only-commits Git bundle also passes `git bundle verify`. No claim of a new audit of all worktree branch heads or every ignored source.
- Git status still has 5,212 collapsed untracked entries (5,153 Docs, 52 ArtSource, 5 Assets, one ProjectSettings folder and `_to_delete`). These are largely already-audited historical evidence/source backups; absence from the index is not evidence they are disposable. No broad `git clean` or staging is warranted.

## Retained current delivered build

`/Users/steven/caves-of-ooo/Builds/SteamDeck/2026-10-11-fullscreen-skills-current/CavesOfOoo-SteamDeck-fullscreen-skills-current.tar.gz`

- 128,049,954 bytes.
- SHA256 `debc85bfacc1c018b24dc8c742b35956768c131571df0e0cb01221a9badf001b`, exactly matches earlier cleanup receipt.
- Source commit `244177619cffae76fb452cce40e3c49cc5dfcb2f` is an ancestor of current HEAD.
- This is the latest retained **delivered** build, not a fresh package of today's graphics. Keep it until root deliberately replaces it with a verified delivery.
- All seven archive paths in `Docs/Verification/Live16x24/recovery-cleanup-20261024.json` remain absent. Adjacent audit data and checksums remain.
- `Builds/SteamDeck/2026-10-10-4c3b0c1d7/audit/Saves-before-build`: 700 files, 85,115,201 bytes, preserved.

## Cache candidates assessed, all deferred

| Exact project-relative path | Logical bytes | Evidence / decision |
| --- | ---: | --- |
| `Library/Bee` | 212,898,342 | Compiler cache regenerated today around 14:38 UTC; useful for ongoing compile/test work. Delete only after Unity/compilation stops if space becomes critical; otherwise keep. |
| `Library/BurstCache` | 119,423,990 | Regenerated around 14:36 UTC; editor has open handles inside. Keep while active. |
| `Library/Search` | 62,443,780 | Rebuilt/updated around 14:51 UTC; editor has open handle inside. Keep while active. |
| `Library/ShaderCache` | 3,993,424 | Regenerable but too small to justify shader recompile; active editor also has ShaderCache.db open. Keep. |
| `Temp` | 11,459,328 | Current editor/test/Burst temporary outputs, latest around 14:39 UTC. No evidence of nonuse while editor/workers live. Keep. |
| `Logs` | 236,014 | Current import/compiler diagnostics, negligible size. Keep. |
| `_to_delete` | 574,951 | Earlier audit proved stale Git object duplicates; tiny benefit. Not worth expanding cleanup during recovery. Keep this pass. |

Main editor also has ArtifactDB, SourceAssetDB, PackageCache and Undo files open. Do not remove Library wholesale or package-cache components. The earlier removal receipt reclaimed 1,253,801,016 logical bytes at that time; it explicitly warned caches regenerate. This pass makes no new reclamation claim.
