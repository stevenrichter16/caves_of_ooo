# Kitchen service post-push storage audit

Observed 2026-10-24 22:17:59 UTC. Bounded read-only audit; the only repository write was this report. No Unity control, cache/build deletion, staging or commit was performed.

## Source push and retained data

Independent `git ls-remote origin refs/heads/main` equals local HEAD **`3303d6240ab830bb79d701c3d7ca7f9683bf9566`**. Root’s `/tmp/coo-kitchen-push-result.json` records verification of all **133 owned files** against that tip; this audit independently rechecked remote identity, not all 133 blobs again.

Retained delivered package:

`Builds/SteamDeck/2026-10-11-fullscreen-skills-current/CavesOfOoo-SteamDeck-fullscreen-skills-current.tar.gz`

- Exact size: **128,049,954 bytes**.
- Recomputed SHA256: **`debc85bfacc1c018b24dc8c742b35956768c131571df0e0cb01221a9badf001b`**, unchanged.
- Only package found in the inspected `Builds/SteamDeck/*/*.tar.gz` locations. All seven superseded package paths recorded in `recovery-cleanup-20261024.json` remain absent. The retained package is the previous delivered build, not a new build of this graphics slice.

The October 24 source/evidence archive and manifest, October 13 historical source archive, and `Builds/SteamDeck/2026-10-10-4c3b0c1d7/audit/Saves-before-build` remain present. This bounded pass checked their presence, not a fresh archive-member revalidation. No source, save, checkpoint, evidence or backup was proposed for removal.

The two unrelated AllotmentNotice variants still match their preserved hashes:

| File under `Docs/Verification/AllotmentNotice/ControlledRender/` | Bytes | SHA256 |
| --- | ---: | --- |
| `notice-hidden-control.png` | 120508 | `b6b8a30da59ded391126a9f526052bb4da5acd98692b3587160be164b3a059db` |
| `notice-visible.png` | 120837 | `3fb8194882da152137cf2f8493b3800f8a3c937e138798e742ce44237af6b811` |

## Disk snapshot and cleanup decision

Filesystem total **994,610,155,520 bytes**, used **992,870,182,912 bytes**, free **1,739,972,608 bytes (about 1.62 GiB)**. These are the observed filesystem figures; ongoing Editor/system activity can change them.

Unity Editor/import processes remain open (PIDs 79930, 80684, 80687). No active cache was treated as unused merely because it can be regenerated.

| Preserved path | Allocated KiB from `du -sk` | Directory mtime, UTC |
| --- | ---: | --- |
| `Library/Bee` | 210508 | 22:02:20 |
| `Library/BurstCache` | 117800 | 13:02:18 |
| `Library/Search` | 61004 | 22:09:27 |
| `Temp` | 11768 | 22:03:20 |

Directory mtime does not prove inactivity of children, especially Burst. These are useful caches for the active Editor. No full Library traversal or broad historical deletion was attempted.

**Recommendation: skip additional deletion in this pass. Confirmed safe worthwhile stale candidates: none; proposed reclaim: 0 bytes; actually deleted/freed by this audit: 0 bytes.** The already removed old packages provide no further reclaim, and keeping the working Editor caches avoids another rebuild while disk space is constrained. Any later cache purge requires root to first establish a closed, inactive Editor and weigh the rebuild cost; it is not proposed or performed by this report.
