# Working-court post-push storage audit — 2026-10-24

Bounded inspection at **2026-10-24T20:09:52.909496+00:00**, after source and gameplay evidence were pushed. No source/save/build removal, Git staging or commit, or Unity control occurred. This report is the only repository write.

## Decision and capacity

**Cleanup skipped. Freed bytes: 0.** No worthwhile, confirmed-unused cache or superseded package was found within the scoped project paths. Unity remains open although Play is stopped; removing its current caches would disrupt the next validation pass.

Filesystem snapshot for `/Users/steven/caves-of-ooo`:

- Free: **2,418,880,512 bytes (~2.25 GiB)**.
- Used: 992,191,275,008 bytes; total: 994,610,155,520 bytes.
- Free space is 291,786,752 bytes below the previous 19:07Z snapshot. This bounded filesystem observation does not attribute that change to any specific project file or claim that it is reclaimable. Capacity remains limited; avoid unnecessary builds/downloads and reassess with the editor deliberately closed if it becomes critical.

## Push and preservation proof

Independent `git ls-remote origin refs/heads/main` returned:

```text
679ae9e2b6fae1b46eff8430b9e9066fc15cb331 refs/heads/main
```

HEAD and local origin/main match that commit. Root's post-push receipt additionally records **104 owned files compared to committed blobs**, all matching; this audit does not repeat that full source comparison. The unrelated modified AllotmentNotice PNGs still match their already-pushed October24 archive manifest SHA256 values and remain untouched.

The current delivered archive was freshly rehashed:

`Builds/SteamDeck/2026-10-11-fullscreen-skills-current/CavesOfOoo-SteamDeck-fullscreen-skills-current.tar.gz`

- **128,049,954 bytes**.
- SHA256 **`debc85bfacc1c018b24dc8c742b35956768c131571df0e0cb01221a9badf001b`**.
- Unchanged from the recovery preservation receipt. This is the latest retained delivered package, not a new build containing today's graphics. Keep it.

Only that archive appears at the immediate package level in the known SteamDeck build containers. All seven exact superseded package paths in the earlier recovery-cleanup receipt remain absent. Remaining build folders include unique source inputs, diagnostics and save backups; they were not treated as disposable outputs.

The October24 source/evidence archive, October13 historical-source archive and Git bundle, and pre-build save-backup root all still exist. Prior byte-verification remains the preservation evidence for their contents; no broad historical or save scan was repeated. All source, saves, checkpoints and backups remain untouched.

## Current caches, retained

These paths are relative to the project. `du -sk` reports allocated KiB, not logical-byte totals or guaranteed APFS reclamation. Directory mtimes do not establish child-file inactivity.

| Path | Allocated KiB | Directory mtime UTC | Action |
| --- | ---: | --- | --- |
| `Library/Bee` | 211,300 | 2026-10-24T19:38:04.796342+00:00 | Keep |
| `Library/BurstCache` | 117,776 | 2026-10-24T13:02:18.033153+00:00 | Keep |
| `Library/Search` | 61,000 | 2026-10-24T20:05:06.599682+00:00 | Keep |
| `Temp` | 11,768 | 2026-10-24T19:57:36.769785+00:00 | Keep |

Process inspection found the main Unity editor and two import workers. Open file handles remain under BurstCache, Search, PackageCache, ArtifactDB, SourceAssetDB, shader database and undo state. Bee and Temp also belong to the active session and were updated recently. No exact inactive subtree was established. Do not remove Library wholesale or delete active caches.

A later cleanup may deliberately close Unity and reassess generated caches, but this pass neither shuts down the editor nor claims a capacity improvement.
