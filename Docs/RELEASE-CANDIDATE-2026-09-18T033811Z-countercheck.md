# Caves of Ooo — release-candidate countercheck, 2026-09-18T03:38:11Z

**Branch:** `release-candidate`, three commits on top of `main` at `41820270`: `7622d71f` (candidate), `fe24212d` (docs), and the commit that adds this file (countercheck). **Not yet on `origin`.**
**Companion:** [RELEASE-CANDIDATE-2026-09-18T032818Z](RELEASE-CANDIDATE-2026-09-18T032818Z.md) is the primary record of what was applied. This file records an independent countercheck of that record and one process finding. It adds nothing to the runtime.

## 1. Process finding: the scheduled task fired at least three times, concurrently

At least three unattended runs of the same "release-candidate" scheduled task executed against `/Users/steven/caves-of-ooo` within the same few minutes (all from isolated Linux VMs). The first run created the branch, applied the candidate, wrote the living-doc updates and the dated report, and committed. A second run stood down, verified, and published the phone-readable status page ("Ooo Release Candidate"). This third run detected the first run's work while it was still in progress — the reflog showed `checkout: moving from main to release-candidate` and the 99 candidate paths appeared in the working tree at 03:26:39 UTC — waited for the tree to settle, verified independently, and committed only this countercheck record.

Why this matters: two agents racing in one checkout can corrupt the index, double-commit, or amend each other's commits. It happened to be harmless here only because the later runs watched instead of writing. Recommended: check the scheduled task's configuration for a duplicate entry or overlapping schedule before it fires again, and consider a lock file (or a branch-exists check) at the top of the task prompt.

## 2. What was independently verified on the first run's branch

All checks ran against Git objects at `release-candidate`, not the working tree.

| Check | Result |
|---|---|
| Manifest `before` hashes vs `main` blobs (83 modified paths) | 83/83 match — the candidate was built against exactly this `main` |
| Manifest says 16 new paths | 16/16 absent from `main`; present at branch tip |
| Manifest `candidate` hashes vs blobs at branch tip | 99/99 match |
| `R1-candidate.zip` contents vs manifest (path, SHA-256, byte size) | 99/99 match; inner `candidate-manifest.json` identical; no extra files |
| Commit `7622d71f` file list | Exactly the 99 candidate paths — nothing else, nothing missing |
| Commit `fe24212d` (amended once by the first run) | Six doc files only: the dated report plus status sections in `RELEASE-STABILIZATION`, `EQUIPMENT-GROUND-SPRITES-PLAN`, `BUILDING-BLOCKS-3D`, `RELEASE-AGENT-PROMPT`, `ReleaseHandoff/README` |
| `main` is an ancestor of `release-candidate` | Yes; `main` unchanged at `41820270` |
| Working tree after all runs | Clean apart from the two pre-existing `Assets/UnityMCP/Log/*.log` modifications and pre-existing untracked reports |
| Seven sprite `.meta` GUIDs | Each appears exactly once across `Assets/**/*.meta` |
| Ten exact blueprint routes in `EnvironmentSpriteRenderer.ResolveItemBody` | `Dagger`, `ShortSword`, `LongSword`, `Spear`, `LeatherBoots`, `IronshodBoots`, `LeatherGloves`, `LeatherCap`, `IronHelmet`, `Mace` all exist in `Assets/Resources/Content/Blueprints/Objects.json` and match the adversarial fixture's table |
| APIs referenced by corrected fixtures / opening checks | `WorldMapAuthoring.PlaceAt(int,int)` and `DebugInvincibility.IsEnabled(Entity)` exist in live scripts |

Source review of the renderer diff (three hunks): the preload list gains the seven bodies; `ChooseTile` returns `null` (original glyph fallback) for a reviewed equipment identity whose exact resource is missing instead of borrowing a generic body; the route switch adds the ten names. No other renderer behavior changed. The two fixture diffs replace only the obsolete `Places[0]`-spawn and plain-Morrowfast premises and add presence/uniqueness assertions. This agrees with the first run's description.

## 3. Push status — needs one manual command

None of the runs could push. The device VM has no GitHub credential; the cloud sandbox's git proxy answered `403: stevenrichter16/caves_of_ooo is not in this session's authorized repository set` on a dry-run push (reads work, writes do not). From a normal terminal on the Mac:

```sh
cd /Users/steven/caves-of-ooo
git push -u origin release-candidate
```

Expect roughly 1 GB to upload: `origin/main` is 368 commits behind local `main`, so the branch push carries those ancestors. It does not move `origin/main`.

## 4. Leftovers to delete by hand

The first run could not delete inside the connected folder, so it left:

- `_to_delete/release-candidate.bundle` — 1.06 GB, untracked, at the repository root. Not needed once the push succeeds.
- `.git/_to_delete/` — 76 renamed lock/`tmp_obj_*` files. Safe to delete.

## 5. Unchanged conclusions

Nothing in this run changes the release picture: R103's 14,901/14,901 remains clone-only evidence; the branch checkout has never been compiled or run in Unity; the sprite A/B bench, the two native opening checks, imported-block visual review and cold-eye review remain the gates before `release-candidate` can merge into `main`. See section 4 of the companion report.
