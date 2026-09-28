# Bounded exploration cost probe

Private implementation and execution complete:330 samples, errors[], restoration true. Actual Unity-reference compile0. Peer source read by combat_density is clear on scope/restore/validated counters. **Root executed the actual Unity probe: all330 rows complete, errors[], restoration true.** See `native-interpretation.md` and `native-summary.json`. The native allocation counter failed its64KiB positive control and is unavailable. The result wrapper timed out, but the validated disk report completed normally.

Source: `QuestFreeExplorationPerformance.cs`. Entry point:
`CavesOfOoo.Experiments.QuestFreeExplorationPerformance.Run(frozenCohortDirectory, outputPath, runtimeLabel)`.
Compiled native-reference assembly: `/tmp/coo-questfree-implementation/performance/QuestFreeExplorationPerformance.dll`; SHA256 and every explicit input are in `provenance.json`. It references the currently loaded gameplay API and reflection-loads the existing fixture classes. No Assets installation is required. Root can invoke the static method from the DLL; all three arguments are strings.

Use cohort directory `/Users/steven/caves-of-ooo/Docs/Verification/QuestFreeExploration/E0/NativeBaseline/20260928T020315Z-c231faba`. Do not choose a new cohort after inspecting content. Suggested native output path `/Users/steven/caves-of-ooo/Docs/Verification/QuestFreeExploration/E2/Performance/native-report.json` and label `Unity actual current runtime, isolated fixture`.

## What it measures

- Three frozen20-zone cohorts × explicit disabled/current arms. Arm order alternates by seed. One pair per seed, not statistical significance or matched archived-binary performance.
-6 constructor,120 actual cold GetZone,6 in-memory save20 and6 load20 observations. Fixture creation, content setup, source assertions and graph signatures are outside measured sections.
-32 independently fresh samples per territory warning, grazer feeding and healthy flight, with each matched to the same no-role fixture;192 one-action scheduler observations. Each zone has2000 inert floor owners outside the timer, original fixture actors/source rows, and validates exactly one actual actor End/energy plus the exercised feature branch. These are deliberately synthetic dense-cell mechanism timings, not natural generated encounter or whole game-frame timings.
-Thread allocation counter is accepted only after a retained64KiB allocation produces at least65536 bytes. Unsupported/untrustworthy counter reports null. No forced collection. Process heap before/after is a noisy global estimate, not exact retained-graph size or a supported-world RAM guarantee.
-Save validation compares exact graph counts/mode, replacement references, canonical per-cell tile fields and current owner IDs/blueprints/positions/pool amounts/field states. This projection does not claim every stat/gear/goal is equal; separate save fixtures own that acceptance.

## Observer corrections retained

The initial private build found only missing `Application.unityVersion` in the standalone stub, followed by a stale copied binary discovering0 host tests. That result is explicitly rejected (`rejected-first-*`), never GREEN evidence. The source now reads that optional version property reflectively and the runner stops after any compilation failure.

The first executing save projection compared raw `ZoneTileState.ToSaveString` dictionary order and refused otherwise identical loaded state. `dictionary-order-*` preserves the exact differing1680/1601 key order. The final probe canonicalizes only the outer Entries numeric key; all tile state/layer fields and layer order still compare exactly. No gameplay repair was made.

First valid330 and final330 metadata-corrected results are retained. The latter records fixture RNG seed1 separately from sample iteration rather than mislabelling iteration as seed. Private runner uses stable zone hash and stubs; actual Unity has different source layouts and timings. Private generation uses the final peer-reviewed composer candidate, while `provenance.json` records the still-frozen shared preimage. The actual Unity run is now complete; its separate raw report and interpretation retain these runtime/layout distinctions.

## Isolation

Existing DensityLootTestScope owns temporary save/pref/native fixture setup; CensusDramaScope detaches original live drama objects. Outer exact mutable-registry/delegate/clock/bus snapshots restore in reverse order. Each arm and each role sample disposes its fixture. Serialized sample count is checked. Errors/partial rows persist in the output; no fallback cohort, source grant or production setting mutation is used.
