# Ground equipment native acceptance

`run_native.py NAME --mode after` prints the command. Add `--execute` only in an isolated verification project: it restarts the local Unity MCP server, launches an owned native Editor, and archives one newly stamped report. It never closes another Editor. The shared launcher refuses conflicting Editors; keep the user's interactive project separate. Never restart MCP in the middle of a running audit.

The scenario creates its own empty scene, private save root and 1920×1080 GameView, then restores all captured scene/view/preferences/Editor settings. It refuses an active Play session or dirty scene. Its camera uses the same default/world layer mask as normal gameplay. Imported resources remain unchanged.

The workload has three 25-second phases: idle; 100 native moves/full redraws; 50 native pickups and 50 drops/incremental redraws. A minimal invisible carrier isolates item claims. These direct gameplay API calls are synthetic evidence, not keyboard-play evidence. Four actual captures show normal/dim light at 16 screen pixels per cell and enlarged nearest-neighbor versions. Pixel guards complement logical claims; inspect the images too.

The five non-wrapping profiler recorders consume samples once, report actual invocation counts, and distinguish zero work from an invalid handle. Raw observations, work counters and all marker summaries are retained. This is an Editor workload comparison, not a player-build FPS benchmark or isolated allocation proof.

For a BEFORE control, prepare the original renderer **only in the verification copy**, preserve its candidate bytes, and restore it in `finally`. `--mode before` verifies the expected old mappings; it does not rewrite mappings or hide files. A valid comparison holds every other source/asset and the entire measurement harness constant. The R1 pair reconstructs the original renderer from immutable `26fbe544` after implementation; it is explicitly a retrospective control, not a historical preimplementation capture.

Accept only a new receipt with zero compiler/unhandled errors, `pass:true`, `sourceFrozen:true`, valid reports/artifacts/counterchecks, and successful private-state cleanup. Failed runs remain failed evidence. See `Docs/RELEASE-R1-ACCEPTANCE.md` for the development failures and accepted R106e/R107b pair.
