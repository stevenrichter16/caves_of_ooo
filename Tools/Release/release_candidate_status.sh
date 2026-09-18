#!/usr/bin/env bash
# release_candidate_status.sh — read-only idempotency guard for the release-candidate branch.
#
# Purpose
#   Unattended (scheduled) agent runs against this checkout have fired concurrently and
#   re-derived the same state several times. Run this FIRST, before any git write, to learn:
#     1. whether another run is still active in this checkout (recent reflog writes, lock files);
#     2. whether `release-candidate` exists, sits on top of `main`, and is already on `origin`;
#     3. whether the branch tip still carries the exact 99-file R1 candidate from
#        Docs/ReleaseHandoff/R1-candidate-manifest.json (content SHA-256 of the committed blobs);
#     4. which tracked files are modified in the working tree.
#
#   It never writes to the repository and never touches the remote except for a read
#   (`git ls-remote`), so it is safe to run from any host, including a VM with no credential.
#
# Usage
#   Tools/Release/release_candidate_status.sh [--quiet-minutes N] [--no-remote] [--no-manifest]
#
# Exit codes
#   0  quiet and consistent — safe to proceed with explicit-path writes
#   2  another run appears to be ACTIVE in this checkout — stand down, re-run later
#   3  branch/manifest inconsistency — investigate before writing
#   4  usage / environment error
set -u

QUIET_MINUTES=10
CHECK_REMOTE=1
CHECK_MANIFEST=1
while [ $# -gt 0 ]; do
  case "$1" in
    --quiet-minutes) QUIET_MINUTES="${2:-}"; shift 2 ;;
    --no-remote) CHECK_REMOTE=0; shift ;;
    --no-manifest) CHECK_MANIFEST=0; shift ;;
    -h|--help) sed -n '2,25p' "$0"; exit 0 ;;
    *) echo "unknown argument: $1" >&2; exit 4 ;;
  esac
done

ROOT="$(git -C "$(dirname "$0")" rev-parse --show-toplevel 2>/dev/null)" || { echo "not inside a git repository" >&2; exit 4; }
cd "$ROOT" || exit 4
BRANCH=release-candidate
MANIFEST=Docs/ReleaseHandoff/R1-candidate-manifest.json
STATUS=0
now_epoch=$(date -u +%s)

say()  { printf '%s\n' "$*"; }
warn() { printf 'WARN  %s\n' "$*"; }
bad()  { printf 'FAIL  %s\n' "$*"; }

say "release-candidate status — $(date -u +%Y-%m-%dT%H:%M:%SZ)"
say "repo: $ROOT"
say "HEAD: $(git rev-parse --abbrev-ref HEAD) @ $(git rev-parse --short HEAD)"

# ---- 1. concurrency ------------------------------------------------------------------------
say ""
say "[1] concurrent-run check (quiet window: ${QUIET_MINUTES} min)"
locks=$(ls .git/*.lock .git/refs/heads/*.lock 2>/dev/null)
if [ -n "$locks" ]; then
  warn "git lock files present:"; printf '      %s\n' $locks
  STATUS=2
fi
last_reflog_epoch=$(git reflog --format='%ct' -1 2>/dev/null || echo 0)
last_reflog_line=$(git reflog --format='%h %gd %gs' --date=iso -1 2>/dev/null)
age_min=$(( (now_epoch - last_reflog_epoch) / 60 ))
say "      last HEAD reflog write: ${age_min} min ago — ${last_reflog_line}"
if [ "$age_min" -lt "$QUIET_MINUTES" ]; then
  warn "HEAD was written less than ${QUIET_MINUTES} minutes ago; another run may still be active"
  STATUS=2
fi
if [ "$STATUS" -eq 2 ]; then
  say "      => STAND DOWN: do not commit, amend, checkout or reset in this checkout right now."
else
  say "      ok: no locks, reflog quiet"
fi

# ---- 2. branch topology --------------------------------------------------------------------
say ""
say "[2] branch topology"
if ! git show-ref --verify --quiet "refs/heads/$BRANCH"; then
  bad "local branch $BRANCH does not exist"
  [ "$STATUS" -eq 0 ] && STATUS=3
else
  rc_sha=$(git rev-parse "$BRANCH")
  main_sha=$(git rev-parse main 2>/dev/null || echo "")
  say "      $BRANCH: ${rc_sha:0:8}   main: ${main_sha:0:8}"
  if [ -n "$main_sha" ] && git merge-base --is-ancestor main "$BRANCH"; then
    ahead=$(git rev-list --count "main..$BRANCH")
    say "      main is an ancestor of $BRANCH (${ahead} commit(s) ahead)"
    git log --format='        %h %s' "main..$BRANCH"
  else
    bad "main is NOT an ancestor of $BRANCH (or main missing) — history diverged"
    [ "$STATUS" -eq 0 ] && STATUS=3
  fi
  if [ "$CHECK_REMOTE" -eq 1 ]; then
    remote_line=$(GIT_TERMINAL_PROMPT=0 timeout 45 git ls-remote --heads origin "$BRANCH" 2>/dev/null)
    if [ -z "$remote_line" ]; then
      warn "origin has no $BRANCH — push pending:  git push -u origin $BRANCH"
    else
      remote_sha=${remote_line%%[[:space:]]*}
      if [ "$remote_sha" = "$rc_sha" ]; then
        say "      origin/$BRANCH is up to date (${remote_sha:0:8})"
      else
        warn "origin/$BRANCH = ${remote_sha:0:8}, local = ${rc_sha:0:8} — push or fetch needed"
      fi
    fi
  fi
fi

# ---- 3. manifest ---------------------------------------------------------------------------
if [ "$CHECK_MANIFEST" -eq 1 ] && git show-ref --verify --quiet "refs/heads/$BRANCH"; then
  say ""
  say "[3] R1 candidate manifest vs blobs at $BRANCH tip"
  if [ ! -f "$MANIFEST" ]; then
    warn "manifest $MANIFEST not found; skipping"
  elif ! command -v python3 >/dev/null 2>&1; then
    warn "python3 unavailable; skipping manifest check"
  else
    python3 - "$BRANCH" "$MANIFEST" <<'PY'
import hashlib, json, subprocess, sys
branch, manifest = sys.argv[1], sys.argv[2]
files = json.load(open(manifest))["files"]
ok = missing = mismatch = 0
for f in files:
    path, want = f["path"], f["candidate"]
    r = subprocess.run(["git", "cat-file", "blob", f"{branch}:{path}"], capture_output=True)
    if r.returncode != 0:
        missing += 1; print(f"      MISSING   {path}"); continue
    got = hashlib.sha256(r.stdout).hexdigest()
    if got == want: ok += 1
    else:
        mismatch += 1; print(f"      MISMATCH  {path}")
print(f"      {ok}/{len(files)} match candidate hashes; {missing} missing; {mismatch} mismatched")
sys.exit(0 if (missing == 0 and mismatch == 0) else 3)
PY
    rc=$?
    if [ "$rc" -ne 0 ]; then
      bad "candidate content at $BRANCH tip differs from the manifest"
      [ "$STATUS" -eq 0 ] && STATUS=3
    fi
  fi
fi

# ---- 4. working tree -----------------------------------------------------------------------
say ""
say "[4] tracked files modified in working tree (untracked files not listed)"
mods=$(git status --porcelain --untracked-files=no)
if [ -z "$mods" ]; then
  say "      none"
else
  printf '      %s\n' "$mods"
fi
untracked_n=$(git status --porcelain --untracked-files=all | grep -c '^??' || true)
say "      untracked files: ${untracked_n} (reports/caches/logs are expected; never stage by wildcard)"

say ""
case "$STATUS" in
  0) say "RESULT: quiet and consistent (exit 0)";;
  2) say "RESULT: another run may be active — stand down (exit 2)";;
  3) say "RESULT: inconsistency detected — investigate before writing (exit 3)";;
esac
exit "$STATUS"
