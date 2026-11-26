# Clone verification: run EditMode tests without touching the open Editor

Why: the open Unity Editor is the owner's (or another agent's) play surface, and a compile or a test run in it can wipe a play session. Instead, keep warm copies of the project ("clones") and run the tests there in Unity batch mode. This is how every machine/camp/tool slice from 2026-11-17 on was verified. The whole method is described in [Docs/MACHINE-PROGRAM-HANDOFF.md](../../Docs/MACHINE-PROGRAM-HANDOFF.md) section 6.

Layout (defaults in `env.sh`, overridable with environment variables):

| Path | What |
|---|---|
| `/Users/steven/coo-verification/clone`, `clone2` ... `clone5` | Warm project copies (each ~13 GB nominal; APFS copy-on-write). One Unity run at a time per clone. By convention clone2 runs the long full-suite runs and clone3/clone4 run mutation batches. |
| `/Users/steven/coo-verification/results/` | Result XML + Unity log per run (`<name>.xml`, `<name>.log`). Durable; use a NEW name for every run. |
| `/Users/steven/coo-verification/baselines/full_s9.xml` | The accepted baseline of a full run (39,500 tests, 91 pre-existing failures, 2026-11-19). |

Scripts (all take a clone name such as `clone3`, and refuse to start when that clone already has a Unity process):

| Script | Use |
|---|---|
| `clone_run.sh <clone> <filter> <name>` | Sync the working tree into the clone and run the filter (a regex such as `CavesOfOoo.Tests.XTests\|CavesOfOoo.Tests.YTests`; `CavesOfOoo` means everything, about 3 hours). Prints `compile errors: N` (N above 0 means the run proves nothing), the totals and each failure. |
| `mutation_run.sh <clone> <filter> <name> <spec.json>` | Apply every mutation in the spec to the clone's files, run the filter once. |
| `each_mutant.sh <clone> <spec.json> <filter> <prefix>` | One clean run per mutant, then a KILLED / SURVIVED / INVALID table (`mutant_report.py`). Run in the background. Never edit production code in the working tree while it runs (every run re-syncs). |
| `red_run.sh <clone> <filter> <name> <file>...` | RED proof: restore the listed files from git HEAD in the clone, run the new tests; they must fail (and compile). |
| `at_commit.sh <clone> <commit> <filter> <name>` | Run tests against what an earlier commit contained (bisecting; "did this already fail?"). |
| `compare_full.py <new.xml> [baseline.xml]` | New failures against the baseline; marks timeouts. Re-run every new failure ALONE before believing it. |
| `new_clone.sh <name> [from]` | Make another clone (about 20 s) from an idle one. |
| `summarize.py <xml>`, `env.sh`, `coo_mcp_keeper.py` | Helpers. The keeper keeps the Unity MCP server alive between batch runs (the server dies when any Unity quits; a batch run reconnects to it and fails with WebSocket errors if it is gone). `pkill -f coo_mcp_keeper.py` ends it. |

Mutation spec: `[{"id": "m1", "file": "Assets/Scripts/...cs", "old": "exact text", "new": "changed text"}]`. A good mutation changes behaviour in a way a test should notice (flip a condition, drop a guard, return early); a mutation that fails to compile is INVALID.

Rules that cost time to learn:
- Two concurrent Unity runs make time-sensitive tests exceed their 180 s timeout; re-run a suspect alone.
- A log containing `error CS` means the compile failed and the old assembly may have run.
- A frozen test count after an edit means a stale assembly: re-run.
- New files need their `.meta` copied from the clone (Unity generates them there) before committing.
- Never sync the working tree into a clone while a batch is running in it.
- The scripts copy only `Assets`, `Packages`, `ProjectSettings`, `Docs` and four small `ArtSource` folders (`SpreadScenery3D`, `SpreadNativeStyle3D`, `SpreadPortable3D`, `SpreadEquipment3D`) that tests read; add a folder to `coo_sync` in `env.sh` if a new test reads another source folder.
- I did not record how `clone` itself was first made. To rebuild one: copy the project without `Builds/`, `obj/`, `Temp/` and the large `ArtSource` folders, then run a tiny batch test run to import it (the first import is slow); then `new_clone.sh` makes the rest.

There is also a standalone, Unity-free runner in `Tools/EditModeRunner` (a pre-check on plain .NET; not a replacement for the Unity run).

## When a long run ends without a result

Keep a single pinned source snapshot and its native EditMode discovery
manifest. Run disjoint filters sequentially and compare each completed
XML’s full case names with the manifest. Accept each receipt only when
its cases match exactly and no compiler errors occurred. Keep retries
separate. A case absent from the current XML or reported as skipped or
inconclusive does not clear a historical failure.

Completed partitions can establish coverage of the discovered cases.
Use a whole-process run to check failures caused by state accumulating
across fixtures. The two kinds of result are recorded separately.
