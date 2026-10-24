# Works graphics storage audit — 2026-10-24

Read-only snapshot at **2026-10-24T17:04:52.935718Z**. No deletions, staging, repository writes or Unity actions. Root reports server verification of main; this audit independently checked local refs and committed backup bytes.

**Recommendation: skip cleanup this pass; 0 bytes proposed for deletion.** No superseded package remains. Unity is still running, and its regenerated caches are in current use. Repeatedly deleting them would interrupt current graphics validation for a temporary reduction.

Free space from `shutil.disk_usage(/Users/steven/caves-of-ooo)` at the timestamp above:

- **3,327,246,336 bytes free** (approximately 3.10 GiB).
- Total 994,610,155,520 bytes; used 991,282,909,184 bytes.
- This is one filesystem snapshot; subsequent imports and captures can change it.

## Preservation verified

- HEAD and local origin/main both `ee0f180cfc098c0c7eee4d44c41d29d687f70e62`. Root separately verified the server and 103 owned committed files; this audit does not duplicate or enlarge that claim.
- Tracked modifications remain exactly the two known AllotmentNotice PNGs. Both current original hashes still match the six-entry October 24 archive manifest. All six archive members were re-read and independently matched their recorded lengths/SHA256 and current originals.
- `Docs/Archive/LocalWork-2026-10-24/manifest.json` and `local-source-and-evidence-variants.tar.gz` are byte-identical to HEAD blobs. The preserved ProBuilder setting, three historical packaging/launch scripts and two PNG variants remain recoverable in main.
- All seven previously removed superseded build archive paths remain absent. No new `.tar.gz` package exists under Builds besides the retained one below.

**Retain:** `/Users/steven/caves-of-ooo/Builds/SteamDeck/2026-10-11-fullscreen-skills-current/CavesOfOoo-SteamDeck-fullscreen-skills-current.tar.gz`

128,049,954 bytes; SHA256 `debc85bfacc1c018b24dc8c742b35956768c131571df0e0cb01221a9badf001b`. Matches the original retention receipt exactly. This is the latest retained delivered build, not a new package of the latest graphics. Its adjacent receipts, save backups and authored diagnostic/packaging inputs are not disposable old builds.

## Only meaningful cache candidates, deferred

All paths below are relative to `/Users/steven/caves-of-ooo`; sizes are logical bytes, not promised physically reclaimable space.

| Path | Logical bytes | Evidence / decision |
| --- | ---: | --- |
| `Library/Bee` | 213,005,603 | Rebuilt/updated 16:36:45 UTC; current compilation cache. Keep during ongoing editor work. |
| `Library/BurstCache` | 119,429,532 | Updated 16:36:07 UTC; active Unity open handles. Keep. |
| `Library/Search` | 62,445,460 | Updated 16:54:29 UTC; active Unity open handles. Keep. |
| `Temp` | 11,459,328 | Updated 16:38:56 UTC; editor/import workers still running. No proof of nonuse. Keep. |

The main editor and two import workers are present. Open Library files also include ArtifactDB, SourceAssetDB, package dependencies, shader database and undo state. Do not remove Library wholesale or parts of PackageCache while the editor is active. If space becomes critical, root can deliberately stop Unity and reassess generated caches; no such shutdown or deletion is recommended solely for this bounded hourly pass.

No broad re-audit or deletion of historical evidence/source archives, user Downloads, Git history or other projects was undertaken.
