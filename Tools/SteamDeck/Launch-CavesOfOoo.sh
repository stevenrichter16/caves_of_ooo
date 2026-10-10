#!/bin/sh
set -eu
cd -- "$(dirname -- "$0")"
# An explicit caller choice wins. Saved in-game graphics preferences also win.
export COO_HANDHELD="${COO_HANDHELD:-1}"
exec ./CavesOfOoo.x86_64 "$@"
