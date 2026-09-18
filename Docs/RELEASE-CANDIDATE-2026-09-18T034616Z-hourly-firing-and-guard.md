# Caves of Ooo — release-candidate run, 2026-09-18T03:46:16Z: hourly firing diagnosed, guard added

**Branch:** `release-candidate`, local tip before this run `e5af1c70` (three commits on top of `main` at `41820270`). **Still not on `origin`.**
**Companions:** [RELEASE-CANDIDATE-2026-09-18T032818Z](RELEASE-CANDIDATE-2026-09-18T032818Z.md) (what was applied) and [RELEASE-CANDIDATE-2026-09-18T033811Z-countercheck](RELEASE-CANDIDATE-2026-09-18T033811Z-countercheck.md) (independent verification). This file adds nothing to the runtime. It records the fourth unattended run of the same scheduled prompt, the reason it keeps firing, and the one concrete process fix the earlier runs recommended but did not build.

## 1. Nothing to implement twice — what this run checked and left alone

The scheduled prompt asks the run to see what is already implemented before implementing anything. The R1 candidate (all three repair groups covering the 32 inherited RS28 failures) was already applied to this branch by the 03:26 UTC run and independently verified by the 03:38 UTC run. This run re-verified from Git objects with the new guard script (section 3): `main` is an ancestor of `release-candidate`, exactly three commits ahead, and all **99/99** manifest paths at the branch tip match their `candidate` SHA-256. Nothing from the candidate was re-applied, no source file under `Assets/` was touched, and `main` remains at `41820270`.

The remaining release gates listed in section 4 of the 03:28 report are unchanged and still cannot be run from the isolated Linux VM the scheduled task uses: fresh full EditMode suite on this branch checkout, the two native opening checks executing, the ground-sprite A/B bench, imported-block visual review, cold-eye review, merge into `main`. This run did not attempt to author new C# or bench scenarios blind; on an unattended run with no compiler available, adding uncompiled code to a 14,901-test project would be a liability, not an improvement.

## 2. Why the task keeps firing: one hourly scheduled task, prompt replaced mid-hour

The earlier runs suspected a duplicate scheduled task. This run read the account's scheduled-task list. There is **exactly one** task:

| Field | Value |
|---|---|
| Name | `game ideas` |
| Schedule | `39 * * * *` (every hour at :39 UTC) |
| Created | 2026-09-14 03:39 UTC |
| Prompt last updated | 2026-09-18 03:23:45 UTC — replaced with the release-candidate handoff prompt |
| Last recorded run | fired 03:39:12 UTC, this session |
| Next run | 04:39 UTC, then every hour |

So the sequence is: the prompt was swapped at 03:23, one or more runs fired immediately around 03:26 (the branch was created then), a second and third overlapped, and the regular hourly slot fired at 03:39 (this run). Until the task is edited, **the same prompt will fire again every hour at :39**, each time re-reading the same handoff document, finding the branch already done, failing the push for the same credential reason, and producing another dated report and another phone notification. That is the "game ideas" hourly cadence inherited by a prompt that describes a one-off job.

Recommended (this run did not change the task itself — that is the user's decision): either disable the task once the manual push is done, convert it to a one-shot, or rewrite its prompt to a genuinely recurring check that first runs `Tools/Release/release_candidate_status.sh` and stops if the exit code is 2 (another run active) or the branch is already on `origin`.

## 3. Concurrency evidence and the guard that was added

When this run started at 03:40:18 UTC, the reflog showed the countercheck commit being **amended at 03:40:59 and 03:41:16** — after this run had begun. A previous firing was still writing to the checkout. This run therefore made no git writes until the reflog had been quiet for ten minutes, matching the risk the countercheck described (index corruption, double commits, amended-over commits).

Both earlier reports recommended "a lock file or a branch-exists check at the top of the task prompt" but neither built one. This run adds a read-only guard:

`Tools/Release/release_candidate_status.sh [--quiet-minutes N] [--no-remote] [--no-manifest]`

It never writes to the repository and only reads from the remote (`git ls-remote`), so it is safe from any host including a credential-less VM. It reports four things and encodes them in the exit code:

1. **Concurrent-run check** — git lock files and the age of the most recent HEAD reflog write; younger than the quiet window (default 10 minutes) means *stand down* (exit 2).
2. **Branch topology** — `release-candidate` exists, `main` is its ancestor, how many commits ahead, and whether `origin/release-candidate` exists and matches the local tip (push pending otherwise).
3. **Manifest** — SHA-256 of every committed blob at the branch tip against the 99 `candidate` hashes in `Docs/ReleaseHandoff/R1-candidate-manifest.json`; any miss is exit 3.
4. **Working tree** — tracked modifications (the two `Assets/UnityMCP/Log/*.log` files are the expected noise) and the untracked-file count, with the reminder never to stage by wildcard.

First live run on this checkout at 03:45:19 UTC: it correctly returned exit 2 because the amend was four minutes old, reported 99/99 manifest matches, three commits ahead of `main`, and `origin` without the branch. A later run after the quiet window returned exit 0 before this run's commit was made (see section 5).

A lock *file* was deliberately not implemented: the VM cannot delete inside the connected folder, so any lock it took could never be released and would block every later run, including the human's. The reflog-age check gives the same protection without a resource that has to be cleaned up.

## 4. Push status — unchanged, one manual command

Re-tested this run from the VM: `git config credential.helper` is unset, there is no `~/.git-credentials` or `gh` configuration, and `git push --dry-run` fails with `could not read Username for 'https://github.com'`. Reads (`git ls-remote`) work; `origin/main` is at `c7fdd5a7`, still 368 commits behind local `main`. The cloud sandbox cannot help either: the earlier run recorded its git proxy's 403 for this repository, and the ~1 GB of objects are not in the sandbox anyway. Not retried from the cloud.

From a normal terminal on the Mac:

```sh
cd /Users/steven/caves-of-ooo
git push -u origin release-candidate
```

After it succeeds, `Tools/Release/release_candidate_status.sh` will report `origin/release-candidate is up to date`, and the leftovers named in the countercheck (`_to_delete/release-candidate.bundle`, `.git/_to_delete/`) can be deleted.

## 5. What this run committed

By explicit path only, after the quiet window: this file, `Tools/Release/release_candidate_status.sh`, one dated bullet in `Docs/RELEASE-STABILIZATION.md`, and a two-line note in `Docs/RELEASE-AGENT-PROMPT.md` telling the next agent to run the guard first. The two MCP log modifications and the ~4,900 untracked report/cache files remain uncommitted, as in every earlier run.

The phone-readable copy of this status is the existing artifact "Ooo Release Candidate", updated in place rather than published as a third page.

## 6. Explicitly not done

- No Unity, tests or native harness; the user's Editor was not touched.
- No change to `main`, spawn `Overworld.2.6.0`, camera, 1.2× view, reveal or any gameplay setting.
- No change to the scheduled task's schedule, prompt or enabled state.
- No new C#, scenario, blueprint or asset work; no post-R1 milestone started.
