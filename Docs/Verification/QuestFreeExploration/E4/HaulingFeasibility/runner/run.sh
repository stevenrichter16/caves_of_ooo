#!/bin/sh
# Build and run the selected EditMode tests outside Unity, single-threaded.
#   run.sh [nunit-where-expr] [result.xml]
# Select tests first:  python3 select_tests.py --all   (or list files)
set -e
cd "$(dirname "$0")"
WHERE="${1:-class =~ /Tests/}"
RESULT="${2:-results.xml}"
python3 patch.py
if dotnet build -nologo -v q > build.log 2>&1; then :; else
  grep " error " build.log | sed -E 's/ \[.*//' | sort -u | head -20; exit 2; fi
set +e
COO_QUIET=${COO_QUIET:-1} dotnet bin/Debug/net8.0/EditModeRunner.dll --workers=1 \
  --result="$RESULT" --where "$WHERE" > run.log 2>&1
grep -E "^[0-9]+\) (Failed|Error)" run.log | sed -E 's/^[0-9]+\) (Failed|Error) : //; s/CavesOfOoo\.Tests\.//'
grep -E "Test Count|Overall result" run.log
