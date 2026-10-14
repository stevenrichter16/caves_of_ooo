# Local work preserved before disk cleanup — 2026-10-13

This is a preservation archive, not an implementation proposal or a new acceptance record. It protects unique historical work found during the user's request to push local project work to main before identifying disk cleanup candidates. Nothing here was applied to gameplay, release tooling, or the original worktrees. Historical instructions, host limitations, status statements, and test claims remain source material; they do not describe the current project's state or authorize new actions.

## Audit scope and decisions

The main checkout was `/Users/steven/caves-of-ooo` at `0508e892b2b000b374c0bb46465b4e839c9b24e3`. Seven registered Git worktrees were inspected without changes. The main checkout had the known package/log edits and untracked files. Of its six secondary worktrees, four were clean; the two secondary worktrees with unique local changes are preserved below. Read-only Git inspections used `GIT_OPTIONAL_LOCKS=0`.

Of 26 commits absent all fetched remote refs at the audit, 23 belong to local `release-candidate`. Three older local skill commits (`7dc4ee3ba`, `14bc26824`, `444baee71`) already have patch-equivalent changes on main, established by `git cherry main <branch>` returning `-`; they are not unapplied features. Other branches with commits absent main are already backed up by remote branches and were not merged. In particular, branch `qud` contains the decompiled reference project and is deliberately separate from this game's source tree.

## Archived files

| File | Origin and purpose |
| --- | --- |
| `release-candidate-net.patch.gz` | Exact `git diff --binary d98af95f79c836da6700ca660f1802ee86516855 7ae674ebabdead836765b8c8814d58d70a1c8255`. Preserves the final 24-path change from the old release branch's merge base; it is not a patch against current main. |
| `release-candidate-commits.txt.gz` | Full commit metadata/messages in oldest-first order for the 23 commits in `0508e892b2b000b374c0bb46465b4e839c9b24e3..7ae674ebabdead836765b8c8814d58d70a1c8255`, including the branch's merge commit. This listing plus net patch preserves final authored changes and provenance; the separate bundle below also preserves the original commit objects. |
| `friendly-robinson-comment.patch.gz` | Uncommitted `git diff --binary` of `Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs` in `/Users/steven/caves-of-ooo/.claude/worktrees/friendly-robinson-a6630a`, whose HEAD was `b265f8e7c0909d0dcd25eb2ff2f5ed3eb00b6a9e`. The sole change inserts `Ï` into the XML comment identifier `PIERCING_CONFUSE_CHANCE_PERCENT`. Preserved as found; applying it would introduce a documentation typo. |
| `QUD-VERTICAL-COMBAT-SWING.md` | Byte-exact copy of the previously untracked file `/Users/steven/caves-of-ooo/.claude/worktrees/vibrant-mirzakhani-ff24cd/Docs/QUD-VERTICAL-COMBAT-SWING.md`. Historical authored analysis of the separate reference codebase. |
| `qud-combat-swing.html` | Byte-exact copy of the previously untracked file `/Users/steven/caves-of-ooo/.claude/worktrees/vibrant-mirzakhani-ff24cd/Docs/qud-combat-swing.html`. Historical interactive companion to that analysis. |
| `manifest.json` | File lengths/SHA256 hashes, commit identities, and verified hashes for the two duplicate main-checkout release reports described below. |

The reference documents were copied without rewriting their original paths, links, or code references. Paths into the separate decompiled checkout can be unavailable on other machines; this archive does not include that checkout and does not claim those references were revalidated.

## Why the release branch was archived

`release-candidate` ended at `7ae674ebabdead836765b8c8814d58d70a1c8255`, with merge base `d98af95f79c836da6700ca660f1802ee86516855`. Its 23 unique commits comprise 22 historical records/tooling commits and one merge. Its net delta changes 24 paths (1,034 insertions, 32 deletions) and contains no game runtime code. Its scripts document obsolete host permissions, temporary Git lock workarounds, and a cloud credentials route. The current main also changed `Docs/RELEASE-STABILIZATION.md` after the shared base. Archiving preserves this history without restoring old operational assumptions or overwriting the current release record.

Two untracked main-checkout files were verified byte-identical to their blobs at the archived release tip:

- `Docs/RELEASE-CANDIDATE-2026-09-18T044334Z-r1-accepted-branch-synced.md`
- `Docs/RELEASE-CANDIDATE-2026-09-18T054039Z-main-merged-guard-topology.md`

Their final contents are already in the net patch, and their SHA256 hashes are recorded in `manifest.json`. No original copies were removed by this archival step.

## Other local files

The separate historical `Docs/AUDIO-ATMOSPHERE-DELIVERY.md` remains at its original path for normal preservation. Its dated preparation status and prior test claims were not promoted into current acceptance claims. Three untracked `.cs.meta` files correspond to tracked game scripts and are normal source-control candidates, not cleanup targets. Linux cross-build package additions likewise remain in the normal package files. Generated logs, Unity test scenes, Python caches, and crash reports were not copied into this archive. The local ProBuilder preferences were subsequently preserved in the historical snapshot archive described below.

`Docs/Verification` was not treated as wholly disposable: it also contains hand-written patches, preserved inputs, draft source, import notes, and model files. No worktree, branch, original source, test evidence, or generated file was deleted by this archival step.

## Verification and bounds

- Re-read every archived file after creation and matched its bytes to its source or Git command output.
- Recorded deterministic SHA256 hashes and byte lengths in `manifest.json`.
- Confirmed both duplicate release reports exactly match the archived branch blobs.
- Reviewed the release branch path delta and the isolated comment change before deciding not to apply them.
- No tests, builds, gameplay, asset imports, Git staging, commits, or remote pushes were run by this archival step. These are preservation checks, not gameplay or historical-test reruns.

## Exact commit recovery

`local-only-commits.bundle` contains all 26 commits absent the fetched remote refs, including the three patch-equivalent skill commits. Created with `git bundle create ... --branches --not --remotes`; `git bundle verify` passed. It includes four branch tips and requires five prerequisite commits already present in the remote history. This 89 KiB incremental bundle preserves the original commit objects without applying obsolete changes to main. In a full clone with remote history fetched, run `git bundle verify Docs/Archive/LocalWork-2026-10-13/local-only-commits.bundle` before recovery. Inspect its heads with `git bundle list-heads`; fetch a desired head into a new recovery branch rather than overwriting main.

## Historical source snapshots

`historical-source-snapshots.tar.gz` preserves 269 previously untracked integration patches/notes, model and Unity asset snapshots, Blender automatic backups and the local ProBuilder preferences. `historical-source-snapshots.json` lists each original repository-relative path, length and SHA256; every tar member was read back and matched to its source. This is an inert archive, not newly imported game content. Original files remain in place. Generated screenshots, runtime logs and test outputs are not presented as source work. The two standalone historical patches above use lossless gzip so their exact whitespace survives without being treated as new source formatting.

## Ignored Steam Deck diagnostic inputs

`steam-deck-diagnostic-source.tar.gz` separately preserves four source/configuration files under the ignored `Builds/SteamDeck/BlackWorldDiagnosis/SteamDeckDiagnosticCompiler/` directory: `SteamDeckSysroot.cs`, `SteamDeckDiagnosticCompiler.asmdef`, and their two `.meta` files. These paths were not tracked by Git. They are historical diagnostic inputs adapting Unity sysroot tooling, not new game code or a supported compiler integration. No executable, package payload, `bin`, or `obj` contents were included.

`steam-deck-diagnostic-source.json` records each original repository-relative path, byte length and SHA256. All four members were read back and matched to their source bytes; archive and manifest hashes also appear in `manifest.json`. Nothing was installed under `Assets`, and the original files remain in place. This addition does not modify the larger historical snapshot archive.

An independent preservation review re-read and hash-checked the standalone archived files, the two decompressed patches, all 269 historical snapshot members and their original source files, and the four additional diagnostic inputs. `git bundle verify` passed again. Safe relative paths and unique member names were checked before any potential future extraction. This confirms recoverability of the archived data; it is not a measured claim of reclaimed disk space or evidence that the historical code still compiles.
