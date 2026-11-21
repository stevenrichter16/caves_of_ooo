#!/bin/bash
# Shared settings and helpers for the clone-verification scripts. Source it (". env.sh"); do not run it.
#
# The method: never test in the user's open Unity Editor. Keep warm copies ("clones") of the project under
# $COO_CLONES, sync the working tree into one, and run the EditMode tests there in Unity batch mode. Results
# (XML + Unity log) go to $COO_RESULTS, which is durable (unlike a session scratchpad).

COO_MAIN="${COO_MAIN:-/Users/steven/caves-of-ooo}"
COO_CLONES="${COO_CLONES:-/Users/steven/coo-verification}"
COO_RESULTS="${COO_RESULTS:-$COO_CLONES/results}"
COO_BASELINE="${COO_BASELINE:-$COO_CLONES/baselines/full_s9.xml}"
COO_UNITY="${COO_UNITY:-/Applications/Unity/Hub/Editor/6000.3.4f1/Unity.app/Contents/MacOS/Unity}"
COO_TOOLS="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# The keeper keeps the Unity MCP server alive across batch runs; prefer the copy in ~/bin (the plugin refuses to
# stop a server whose command line names that path), fall back to the copy next to these scripts.
COO_KEEPER="${COO_KEEPER:-$HOME/bin/coo_mcp_keeper.py}"
[ -f "$COO_KEEPER" ] || COO_KEEPER="$COO_TOOLS/coo_mcp_keeper.py"
mkdir -p "$COO_RESULTS"

# coo_busy <clone-path>: true when a Unity process already has that project open. NEVER start a second Unity on a
# clone that is mid-run: the second instance only bounces off the project lock, but the rsync that precedes it
# still rewrites the sources under the first one (this happened twice).
coo_busy() { pgrep -f "Unity.*-projectPath $1 " > /dev/null; }

# coo_need_clone <name>: echo the clone path, or exit with a message when it is missing or busy.
coo_need_clone() {
  local n="$COO_CLONES/$1"
  [ -d "$n/Assets" ] || { echo "no such clone: $n (make one with new_clone.sh)" >&2; exit 4; }
  if coo_busy "$n"; then echo "clone $1 is busy (a Unity run is in progress there); pick another or wait" >&2; exit 4; fi
  echo "$n"
}

# coo_ensure_mcp: every Unity editor's MCP plugin stops the local MCP server when that editor quits, and a batch
# run reconnects to it when the user's editor left the resume flag set; a dead server fails tests with WebSocket
# errors. Start the keeper when nothing answers on :8080.
coo_ensure_mcp() {
  if [ "$(curl -s -o /dev/null -w '%{http_code}' http://127.0.0.1:8080/mcp)" = "000" ]; then
    python3 -c "import subprocess;subprocess.Popen(['/Users/steven/unity-mcp/Server/.venv/bin/python','$COO_KEEPER'],stdout=open('/tmp/mcp-server.log','a'),stderr=subprocess.STDOUT,stdin=subprocess.DEVNULL,start_new_session=True)"
    for i in $(seq 1 30); do [ "$(curl -s -o /dev/null -w '%{http_code}' http://127.0.0.1:8080/mcp)" != "000" ] && break; sleep 1; done
  fi
}

# coo_sync <clone-path>: copy the working tree into the clone (what the tests read: Assets, Packages,
# ProjectSettings, Docs and four small ArtSource folders; the rest of ArtSource is 1.5 GB and not needed).
coo_sync() {
  local n="$1" d
  for d in Assets Packages ProjectSettings Docs; do rsync -a --delete --exclude 'InitTestScene*' "$COO_MAIN/$d/" "$n/$d/"; done
  for d in SpreadScenery3D SpreadNativeStyle3D SpreadPortable3D SpreadEquipment3D; do mkdir -p "$n/ArtSource/$d"; rsync -a --delete "$COO_MAIN/ArtSource/$d/" "$n/ArtSource/$d/"; done
}

# coo_unity_run <clone-path> <filter regex> <result-name>: run EditMode tests in the clone and summarise.
# A "compile errors" line above zero means the run proves nothing (the old assembly may have run).
coo_unity_run() {
  "$COO_UNITY" -batchmode -projectPath "$1" -runTests -testPlatform EditMode -testFilter "$2" \
    -testResults "$COO_RESULTS/$3.xml" -logFile "$COO_RESULTS/$3.log" > /dev/null 2>&1
  echo "compile errors: $(grep -c 'error CS' "$COO_RESULTS/$3.log")"
  grep 'error CS' "$COO_RESULTS/$3.log" | sort -u | head -3
  python3 "$COO_TOOLS/summarize.py" "$COO_RESULTS/$3.xml"
}
