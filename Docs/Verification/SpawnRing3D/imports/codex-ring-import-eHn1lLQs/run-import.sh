#!/bin/bash
# Draft only: root reviews/adopts the small Editor audit wrapper before execution.
# Run this script in the background; all evidence uses a new unique directory.
set -euo pipefail
repo=/Users/steven/caves-of-ooo
source_root=${1:-/tmp/codex-spawn-ring-art/final}
audit_tools=$(cd "$(dirname "$0")" && pwd)
unity=/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/MacOS/Unity
wrapper="$repo/Assets/Editor/Art/SpawnRing3DImportAudit.cs"
if [ ! -f "$wrapper" ]; then echo 'Adopt/review SpawnRing3DImportAudit.cs after the Assets freeze before running.' >&2; exit 2; fi
if pgrep -x Unity >/dev/null; then echo 'An existing Unity process is active. Root must finish/stop its owned run first.' >&2; exit 2; fi
python3 - "$source_root" <<'PY'
import json,pathlib,sys
root=pathlib.Path(sys.argv[1]).resolve(); d=json.loads((root/'catalog.json').read_text())
assert len(d['models'])==218 and len(d['blueprints'])==73 and len(d['fellingOwners'])==55
for relative in [d['paletteTexture']]+[m['path'] for m in d['models']]:
 p=(root/relative).resolve();assert p.is_relative_to(root) and p.is_file(),p
assert all(m['triangles']>0 and all(m['boundsSize'][k]>0 for k in ['x','y','z']) for m in d['models'])
print('Completed source structural preflight passed; actual import validation remains required.')
PY
run_dir=$(mktemp -d /tmp/codex-ring-import-XXXXXXXX)
printf '%s\n' "$run_dir" > "$audit_tools/last-run-directory.txt"
cp "$audit_tools/run-import.sh" "$audit_tools/receipt.py" "$run_dir/"
cp "$wrapper" "$run_dir/SpawnRing3DImportAudit.cs"
python3 "$audit_tools/receipt.py" snapshot "$repo" "$source_root" "$run_dir/before.json"
python3 "$audit_tools/receipt.py" preflight "$run_dir/before.json"
# Required server-first ordering; restart happens before Unity, never mid-import/test.
pkill -f '[m]cp-for-unity' || true
sleep 2
(cd /Users/steven/unity-mcp/Server && nohup uv run mcp-for-unity --transport http > "$run_dir/mcp.log" 2>&1 &)
sleep 5
if ! pgrep -f '[m]cp-for-unity' >/dev/null; then echo 'MCP server did not stay up; refusing to launch Unity.' >&2; exit 2; fi
# No user saves or scenes are deleted. Only stale editor lock after no-Unity check.
rm -f "$repo/Temp/UnityLockfile"
set +e
"$unity" -batchmode -quit -projectPath "$repo" \
  -executeMethod CavesOfOoo.Editor.SpawnRing3DImportAudit.RunFromCommandLine \
  -spawnRing3dSource "$source_root" -spawnRing3dReport "$run_dir/import.json" \
  -spawnRing3dSceneReport "$run_dir/scenes.json" -logFile "$run_dir/unity.log"
unity_exit=$?
set -e
# FIRST inspect compile errors before reading/accepting any report.
cs_errors=$(grep -c 'error CS' "$run_dir/unity.log" || true)
printf '%s\n' "$unity_exit" > "$run_dir/unity-exit.txt"
printf '%s\n' "$cs_errors" > "$run_dir/compile-error-count.txt"
echo "Compile error count: $cs_errors. Evidence: $run_dir"
python3 "$audit_tools/receipt.py" snapshot "$repo" "$source_root" "$run_dir/after.json"
if [ "$cs_errors" -ne 0 ]; then echo 'Compile errors: reports are not accepted.' >&2; exit 1; fi
python3 "$audit_tools/receipt.py" check "$run_dir" "$unity_exit"
