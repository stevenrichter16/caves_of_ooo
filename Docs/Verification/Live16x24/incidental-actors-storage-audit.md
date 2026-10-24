# Incidental actors — pre-push storage inventory

**Status: pre-push inventory only. Cleanup is not authorized until root verifies the new remote push.** Inspected at **2026-10-24T20:45:37.336100+00:00** while root was finishing actual Grazer testing. No Unity control, deletion, staging or commit. This report is the only repository write. Root will append final post-push status separately.

## Capacity and recommendation

Free disk space: **2,357,260,288 bytes (~2.20 GiB)**. Total 994,610,155,520; used 992,252,895,232. This is a point-in-time filesystem snapshot, not a promise of build capacity.

**No confirmed worthwhile safe cleanup candidate found; proposed deletion0 bytes, actual freed0 bytes.** Unity and its current caches are active. No new superseded package exists in the bounded known build containers. Do not remove active caches or original evidence/source directories to make the checkout look clean.

Local HEAD at inspection was `47236211c3710f9781bd48111cd198ae4b13bf54`. This is not the pending incidental-actor push proof and does not replace root's later remote verification.

## Current build and backups

Retain `Builds/SteamDeck/2026-10-11-fullscreen-skills-current/CavesOfOoo-SteamDeck-fullscreen-skills-current.tar.gz`. Fresh check: **128,049,954 bytes**, SHA256 **`debc85bfacc1c018b24dc8c742b35956768c131571df0e0cb01221a9badf001b`**. Exact match to the prior preservation receipt. This is the latest retained delivered package, not a newly built version of the pending graphics work.

The immediate package inventory in the known SteamDeck build directories contains only that archive. All seven exact superseded package paths from the earlier recovery receipt remain absent. The October24 source/evidence archive, October13 historical-source archive and Git bundle, and pre-build save-backup root still exist. No contents were removed; this bounded pass does not repeat a complete historical byte audit. All current source, saves/checkpoints and backups remain protected.

## Exact unrelated PNG preservation

Both current originals still match their already-pushed October24 backup manifest. Preserve them and keep them out of this owned feature commit.

| Path | Bytes | Current SHA256 | Matches backup manifest |
| --- | ---: | --- | --- |
| `Docs/Verification/AllotmentNotice/ControlledRender/notice-hidden-control.png` | 120,508 | `b6b8a30da59ded391126a9f526052bb4da5acd98692b3587160be164b3a059db` | Yes |
| `Docs/Verification/AllotmentNotice/ControlledRender/notice-visible.png` | 120,837 | `3fb8194882da152137cf2f8493b3800f8a3c937e138798e742ce44237af6b811` | Yes |

## Active cache inventory

Project-relative paths. Sizes are allocated KiB from `du -sk`, not logical bytes or guaranteed APFS reclamation. Directory mtimes do not establish whether children are unused.

| Path | Allocated KiB | Directory mtime UTC | Recommendation |
| --- | ---: | --- | --- |
| `Library/Bee` | 211,648 | 2026-10-24T20:26:42.108463+00:00 | Keep |
| `Library/BurstCache` | 117,784 | 2026-10-24T13:02:18.033153+00:00 | Keep |
| `Library/Search` | 61,000 | 2026-10-24T20:42:41.024233+00:00 | Keep |
| `Temp` | 11,768 | 2026-10-24T20:37:05.483101+00:00 | Keep |

Bee/Search/Temp were modified during the current editor session; BurstCache is also current editor state. No exact idle cache subtree was established. A deliberate editor shutdown and reassessment is separate from this inventory and must be coordinated by root. No broad Library/history/evidence scan or unrelated Downloads cleanup occurred.

## Post-push decision — 2026-10-24T20:51:01.268303+00:00

Root fetched/rebased and pushed `3e67ab983a543a4fd1851a89e07df863bd603975` to main. Independent server ref
readback matched local HEAD and all84owned file blobs; no source
cleanup preceded that verification. Both unrelated notice variants were backed
up during rebase and restored exactly. Subsequent disk/cache/build inspection
found no new stale candidate. Current archive still matches the SHA above.

**Removed: nothing. Space recovered:0bytes.** Free space at this follow-up:
2,349,445,120bytes (2.19GiB). Native Play is stopped and save/view isolation
restored; the editor remains open and its useful caches are preserved. All source,
editable assets, saves, backup evidence and current delivered package remain.
This is a bounded safe-cleanup decision, not a claim that the entire Mac has no
other reclaimable storage. No new build or hardware acceptance.
