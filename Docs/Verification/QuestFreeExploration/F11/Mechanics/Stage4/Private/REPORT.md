# F11 stage4 — retained flight search scratch

## Result

Private, approved two-file delta on immutable stage3. A genuine allocation regression failed before production:14cases,2expected allocation failures (5,080/5,184bytes vs512),12controls passed. The same14 pass after the change. Separate actual Unity-reference runtime and full EditModeTests assemblies compile successfully; this verifies the counter API/assembly boundaries but is not a Unity test run. Root owns native RED/GREEN and adoption.

The affected sweep passes359/359: all336 prior cases retained,14new, plus9 preexisting CreatureCorpseBlueprintTests included by the broader class filter. No newly failing or missing prior case. Prior stages and both18-site observations remain unchanged.

## Scope and implementation

SpreadGrazerPart retains lazy per-owner Queue and HashSet of integer tuples. Capacity covers the81 possible radius4 cells; both collections clear before each search and before the real movement call. Queue traversal, candidate order, scores, saved memory, threat ownership and one action remain unchanged. There is no new entity/terrain cache and no saved field. Read-only search invokes no external callback; movement begins after all scratch consumption, so nested movement needs no behavior-changing rejection guard.

The same role checks the current goal stack with existing indexed access instead of allocating a snapshot. Parent membership and exact permitted goal types remain identical. SpreadPredatorPart replaces only its local AIBehavior Any with a parts loop; no shared helper or global AI rewrite.

## Evidence and limits

The14 tests pair movement with changed blockers; remembered coordinates with different hidden live hunter positions, with/without replacement save; first/warmed save state; two separate grazers; sequential/reentrant movement; and exact parent-stack membership. Positive allocation control proves the counter records a1024bytearray; empty control is0.64warmups precede64 measured actual calls; preparation is outside measurements and each measured call must move the grazer to the expected tile and spend exactly one memory action. The hard512-byte allowance is unchanged.

Additional private .NET10 attribution (512calls) has median120bytes ordinary flight and144 pacing, versus prior5,080/5,184. A single ordinary sample reaches560bytes in each longer timed run (otherwise120), also with tiered compilation disabled. The cause is not established. All receipts are retained; no threshold loosening or claim of zero allocation/native frame performance. The64-action test remains green, and actual Unity native gate plus real-gameplay profiling remain required. The original source-plan boundedness alone was insufficient; root should record the concrete performance patterns in the living plan.

## Review

Q1: scratch changes storage only; same BFS dequeue/direction order and same route ties, hidden history and memory decrement. Q2: private nonserialized fields preserve save graph/public role schema; runtime and actual test assembly compile separately. Q3:2allocation RED cases and12 controls before/after; original336 priority/save/loot/role cases retained. Q4: bounded hunt and no offscreen ecology stay unchanged; no native performance success claimed. Renderer independent read found no concrete blocker (f11/review/flight-scratch/evidence-manifest.json).

Manifest files are the adoption authority: test-delta-manifest.json (two test/meta paths), production-delta-manifest.json (two runtime paths). Sources below stages2/3 were not modified.

## Native measurement correction — retain invalid receipt

Root native baseline job7ea91263dc7b4c65b32168f97d7056d6 returned91passes/1failure across92cases: generation78passed; the allocation positive control measured0 for a1024bytearray. Both warmed allocation cases also returned0 and therefore passed vacuously. Those are NOT valid native allocation RED/GREEN. Root retains raw f11/integration/generation-green-allocation-red-result.json. Stage4 production remains unadopted pending a bounded documented ProfilerRecorder counter diagnostic (native-counter/). Genuine managed RED/GREEN and native semantic controls are distinct evidence.
