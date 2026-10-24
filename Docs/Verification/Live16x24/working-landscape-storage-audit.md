# Working-landscape storage audit — 2026-10-24

Bounded read-only snapshot: **2026-10-24T19:07:12.304650+00:00**. No Unity control, deletions, staging or repository changes. Only known build containers, exact prior deletion paths, four small cache usage summaries and protected roots were checked. No broad historical or Library crawl.

**Recommendation: 0 bytes safe to reclaim in this scoped pass.** The editor remains open. The useful compiler, Burst, search and session caches must remain. No new superseded package appeared.

Free disk space: **2,710,667,264 bytes (~2.52 GiB)**, total 994,610,155,520, used 991,899,488,256. This is a point-in-time filesystem reading. It is 313,257,984 bytes below the previous 18:09Z snapshot; this scoped audit does not attribute that change.

HEAD and local origin/main both `9728a7c652609d0c706ad3f797af8a710491a08e`. Root separately reports server verification and 95 owned blobs; this read-only audit does not repeat that remote/source review.

## Protected package and source

`/Users/steven/caves-of-ooo/Builds/SteamDeck/2026-10-11-fullscreen-skills-current/CavesOfOoo-SteamDeck-fullscreen-skills-current.tar.gz`

Fresh size/hash: **128,049,954 bytes**; **`debc85bfacc1c018b24dc8c742b35956768c131571df0e0cb01221a9badf001b`**. Unchanged from retention receipt. Keep this latest retained delivered build; it is not a fresh package of the latest graphics.

Only that package appears at the immediate package level in known SteamDeck build containers. All seven exact superseded archive paths from the recovery receipt remain absent. Existing audit/source/save directories are preserved; do not classify them as disposable generated builds.

The October 24 six-source archive, October 13 historical-source archive and Git bundle, and original pre-build save-backup root all still exist. Their full contents were verified previously; this intentionally bounded pass does not rehash them or claim a new full source audit.

## Active cache usage

Paths are under `/Users/steven/caves-of-ooo`. Sizes are allocated KiB from `du -sk`, not logical bytes or guaranteed APFS reclamation. Root directory mtime is not proof that children are unused.

| Path | Allocated KiB | Directory mtime UTC | Recommendation |
| --- | ---: | --- | --- |
| `Library/Bee` | 210,684 | 2026-10-24T18:31:55.497167+00:00 | Keep: current editor session |
| `Library/BurstCache` | 117,748 | 2026-10-24T13:02:18.033153+00:00 | Keep: current editor session |
| `Library/Search` | 61,000 | 2026-10-24T19:01:58.181204+00:00 | Keep: current editor session |
| `Temp` | 11,768 | 2026-10-24T18:50:54.992976+00:00 | Keep: current editor session |

No exact unused cache was identified. Closing Unity and reassessing generated caches is a separate deliberate action if space becomes critical; deleting active caches now would add rebuild churn. All source, saves, backups and the retained build remain untouched.
