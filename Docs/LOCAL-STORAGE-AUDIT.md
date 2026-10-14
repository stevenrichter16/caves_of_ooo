# Local source preservation and disk audit — 2026-10-13

## Scope and source safety

The user requested that local project work be backed up to main before identifying unnecessary local files. The starting main commit `0508e892b` matched freshly fetched `origin/main`. Seven registered worktrees and local branches were inspected. The preservation archive is [LocalWork-2026-10-13](Archive/LocalWork-2026-10-13/README.md).

- Commit the existing Linux SDK/cross-toolchain package declarations, three missing Unity script metas, and the historical audio delivery preparation document. Both direct dependencies and their transitive sysroot are consistent between manifest and lockfile. These declarations were already in the editor during native verification; no fresh Linux build is claimed.
- Preserve all 26 commits absent fetched remote refs in a verified incremental Git bundle. Three old skill changes already have equivalents on main; the other 23 are historical release records/tooling. Archive their original objects and final patches without reviving obsolete operational scripts.
- Preserve the two unique worktree reference documents and one unapplied comment typo, with original paths and hashes. Preserve 269 untracked historical source/model/asset snapshots and notes in a verified archive. Leave originals in place.
- Stop tracking MCP runtime logs and their metas; keep the actual files locally. Ignore regenerated Python bytecode and native crash dumps. These are diagnostics, not authored source.
- Keep the separate remotely backed `qud` reference branch separate. Do not merge its decompiled reference tree into this game's main branch.

Generated screenshots, native run directories, compressed diagnostics and build archives remain local unless already committed. This is source preservation, not a claim that every generated output is uploaded. No saves, unique source models, worktrees, branch refs or active Unity caches were deleted.

## Low-risk cleanup completed

| Operation | Content preserved | Logical reduction |
| --- | --- | ---: |
| Remove redundant older build ZIP | Every one of its 172 ordinary files SHA256-matched the retained build directory before ZIP removal; archive entries containing macOS metadata excluded from source comparisons | 202,285,522 bytes |
| Remove nine older extracted Deck builds | Retained their complete `.tar.gz` archives; compared every file's SHA256, size, permissions, directory membership and symbolic link targets before removal. Latest fullscreen-skills extracted build remains | 1,918,510,261 bytes |
| Remove 234 raw diagnostics | Every untracked raw file matched decompressed bytes of its tracked `.gz` sibling | 1,510,387,961 bytes |
| Compress 784 other old diagnostics | Inactive untracked log/XML files only; open files/current verification excluded; each archive round-tripped SHA256 and source identity/mtime rechecked before raw removal | 2,327,903,947 bytes net |

Manifests under `Docs/Verification/StorageCleanup-2026-10-13/` record exact paths and hashes. Total logical reduction before adding the preservation archive is **5,959,087,691 bytes (5.55 GiB)**. This is not a claim of equal physical disk reclamation: the filesystem reported approximately 117 MiB free initially and about 1 GiB free after cleanup and archival preparation. File sizes, shared storage and system activity make logical totals different from available-space measurements; the exact cause of the discrepancy is not established. No Data-volume snapshots were reported by `diskutil apfs listSnapshots` during inspection. The disk still needs more headroom.

## Larger candidates, not deleted

Sizes are rounded `du` measurements and are not promises of physical space recovered.

| Path | Size | Assessment and prerequisite |
| --- | ---: | --- |
| `Library/` | 11 GiB | Rebuildable Unity import/package/compilation cache. Close this project's editor first; next opening must restore packages and reimport/recompile. `Library/Bee` accounts for about 5 GiB. Kept during verification. |
| `Builds/SteamDeck/BlackWorldDiagnosis/` | 3.1 GiB | Old black-screen diagnostic builds and IL2CPP generated backup output, superseded by later deliveries. Preserve any diagnostic compiler source and receipts before clearing outputs; not the current playable release. |
| `/Users/steven/.codex/worktrees/exploration-release/caves-of-ooo` | 3.4 GiB | Clean older detached worktree, commit backed up. Remove through managed worktree lifecycle when its owning task no longer needs it; no manual recursive deletion performed. |
| `_to_delete/rc-bundle/` | 600 MiB | Two incomplete historical release-bundle chunks. Their local commit history is now preserved by the small verified bundle; remove only after its main push is confirmed. |
| `Docs/Verification/StarterSpell3D/Integration/NativeAudit/` | about 2.3 GiB | Mostly unique old frame captures, with compact receipts already tracked. Optional archival/evidence tradeoff, not a cache: do not wholesale-delete without choosing which visual evidence to retain. |
| `.git/` | 9 GiB | Repository history. Do not delete or attempt a space-hungry full repack while disk headroom is this low. |

Other build archives and model `.blend1` backups have been preserved. Python bytecode, `obj/` and temporary runner files are relatively small; they are not the primary source of disk pressure.

## Review and verification

- 🟡 Local-only historical work could be lost by blanket worktree/output deletion: archived original commits, unique source documents, patches and model snapshots before considering their removal.
- 🟡 Runtime logs dirtied the source tree continuously: removed from tracking while retaining local files and ignoring their matching metas.
- 🔵 Generated visual evidence remains intentionally local and is not described as pushed source.
- Checked archive members/hashes, incremental bundle prerequisites, package-lock consistency and Git whitespace. These preservation checks are separate from the En Garde UI fix's native tests.
- En Garde display follow-up: `b132f039f`, 78/78 native Unity EditMode tests passed after eight expected RED failures. See `COMPOSABLE-BUILDS.md` for behavior and evidence; no combat balance changed.
