# East-route storage audit — 2026-10-24

Bounded, read-only snapshot at **2026-10-24T18:09:38.716549Z**. No Unity control, deletions, staging or repository edits. No full Library/history/evidence scan.

**Recommendation: skip cleanup; 0 bytes proposed for deletion.** Root reports Play stopped with the editor still open. No exact unused cache or superseded package was identified in the scoped check.

## Disk snapshot

- Free: **3,023,925,248 bytes** (approximately **2.82 GiB**).
- Total: 994,610,155,520 bytes; used: 991,586,230,272 bytes.
- This is 303,321,088 bytes less free than the 17:04:52Z snapshot; this bounded check does not attribute that filesystem-wide change to a particular project artifact.

## Protection checks

- HEAD and local origin/main both `8d02f27fc53cb6ff2e8c534b299701c16ed1871a`. Root's `/tmp/coo-east-push-result.json` independently records matching server head and verification of 143 owned files. This audit reads that receipt without duplicating the remote or source audit.
- Retained delivered package: `/Users/steven/caves-of-ooo/Builds/SteamDeck/2026-10-11-fullscreen-skills-current/CavesOfOoo-SteamDeck-fullscreen-skills-current.tar.gz`. Freshly rehashed: **128,049,954 bytes**, SHA256 **`debc85bfacc1c018b24dc8c742b35956768c131571df0e0cb01221a9badf001b`**. Unchanged from the preservation receipt. Keep. It is the latest retained delivered build, not a new package containing today's changes.
- Immediate package files inside dated SteamDeck build directories show only that retained archive. All seven specific superseded package paths from the earlier cleanup receipt remain absent. No broad deletion of the remaining audit/source directories is appropriate.
- October 24 six-source archive and pre-build save-backup root still exist. Their contents were byte-verified in the prior audit; this pass intentionally does not repeat expensive backup or historical-source scans.
- Root's receipt preserves the two unrelated AllotmentNotice variants; no source changes or new preservation claims were made by this audit.

## Small cache usage check

Sizes below are allocated KiB reported by `du -sk`, **not logical-byte totals or guaranteed reclaimable capacity**. Directory modification times do not prove child-file inactivity. Paths are under `/Users/steven/caves-of-ooo`.

| Path | Allocated KiB | Directory mtime UTC | Decision |
| --- | ---: | --- | --- |
| `Library/Bee` | 210224 | 17:59:04 | Recent current compile cache; keep with editor open. |
| `Library/BurstCache` | 117736 | 13:02:18 | Current Burst cache; old root mtime is not evidence children are unused. Keep. |
| `Library/Search` | 61000 | 18:05:13 | Recently updated editor index; keep. |
| `Temp` | 11768 | 18:00:36 | Recent active-session temporary state; keep. |

No current build, source, save, evidence archive or active cache should be deleted in this pass. If free space becomes critical, root can deliberately close the editor and reassess generated caches; repeated removal during active iteration would cause recompilation/import churn.
