**Delivery update 2026-11-02:** Source de6002171 is now in current Mac/Linux packages; see `GRAPHICS-CURRENT-PACKAGES.md` and `Verification/GraphicsExecution/BatchDe600/`. Four actual Mac launches verify bounded Ginmere/Spread use and reload; Cathedral included but not walked. Linux archive verified, hardware execution unperformed. Earlier package-lag statements below are historical. No whole-area or R11 acceptance.

# Spread field-margin composition pilot

Status: bounded Editor composition observed and independently reviewed; publication pending. Population/composition is V4. R11 and whole-area acceptance remain open; no new sprite coverage.

## Decision and bounded requirements

The seed-65 LastGleanings / BankCrossing field at `Overworld.10.15.0` has readable native rows but broad empty headlands. Add three unequal shelter groups (target 12–18 existing Tree/Bush owners) along parcel margins. Keep the quiet central crossing and working lanes. Reuse approved sprites; no new crops, services, loot, buildings or shadow expansion.

Use an optional cold-generation builder after hazards (3950), before population (4000). Require exact current LastGleanings/BankCrossing generation authority, FieldStrips plan, manager/factory/graph identity. Do not change the frozen manifest, base composition plan, crop coordinates/yields or source receipts. Cached/loaded graphs never receive additions. Existing hazards and authored features keep first claim. New owners can alter downstream actor positions/IDs; preserve actual role/source validity, not invented coordinate equality.

Each complete group must fit on empty dry Grass outside every parcel, approach, reserve, interior and existing owner. Use local deterministic selection without consuming pipeline RNG. Stage and validate the entire group before publication; reject malformed/Barren/resource-bearing owners. Preserve existing approach connectivity and all previously reachable open cells except new solids. Refusal is optional and must not reroll the zone. Repeated calls cannot stack; never clear existing owners or reserve new cells.

## Reproducible baseline

Immutable precursor: `Verification/GraphicsExecution/field-before-save/Quick.*`, compressed SHA256 `772be3ebffed1ac4b00471f660ac2908d4def292fcd4f2834422cef4b9d83f06`. Sequential Format-8 audit confirms seed65, active10.11, four cached graphs (11.10, WorldMap,10.10,10.11); target10.15 absent, frozen LastGleanings/BankCrossing disposition0. Continue, ascend, Down4, descend generates the target through ordinary gameplay. Do not use the later harvested/cached fixture to regenerate.

Actual baseline: `Verification/GraphicsExecution/FieldComposition/before-center.png` at40,12HP40; west24,12 and east46,12 views. Left16 then Right32 was attempted, but ordinary creatures blocked some eastbound steps; actual final position46,12 is recorded. First8 west steps reduced HP40→32; F12 then protected visual navigation, OFF before F5 turn2060. This does not establish encounter balance. Baseline profile149.86s includes an MCP session disconnect/reconnect and menu delay; retain it as contaminated evidence, not a clean performance comparison. No gameplay state was mutated through execute_code.

## Acceptance gates

1. Native RED precedes runtime implementation; positive seed65 plus deterministic/cold-authority/foreign-factory/cached/repeated/missing-resource/reserved/wet/crowded/collision counters pass.
2. Every pre-existing owner and position, tile state, reserves, crop/ripe count and yield stays unchanged across the new builder. Original plan signature and caller RNG stay unchanged. Existing geometry and LastGleanings source/actor tests pass.
3. Matched fresh-entry captures and west/center/east normal-zoom views show coherent separated groups, quiet lanes and improved field edges. Examine a real new owner; walk the crossing and margin; verify actual collision, F5 and fresh Continue with exact saved owner positions. No generic density or postload retrofit.
4. Clean 60–90s actual profile with no execute_code during sampling; disclose maxima and R11 limitations. Independent source and actual-image review; fix named defects before bounded acceptance.
5. Same-commit plan/matrix/coverage/polish records, exact owned staging, main push and remote verification. Current a72 packages remain the prior delivery until a later batch. Full-area acceptance, wider density and Linux/Deck execution remain open.

## Saved source corrections

- Do not add a Plan.ObjectAt mask: it would entangle original route repair/source selection.
- Priority3950 supersedes provisional3865: hazards3900 retain their candidate/RNG history.
- Protect finite sources explicitly; generic bare-ground helpers alone admit too much.
- No new GenReservedCells: retries clear entities but do not clear that collection.

- Initial test fixture used nonexistent Zone.GetEntityByID; corrected to exact-reference lookup in read-only entity collection. Compilation/empty discovery does not count as RED.

## Iteration record

Native initial RED:10 failures/2 counters passing (12 tests), then12/12 GREEN. Independent review found pre-staging source snapshot and staged identity gaps. Seven callback cases reproduced native RED in24-test run (17 passed). Corrected by preserving original owners/membership, tile state and reserves before creation, rejecting foreign graph changes, and validating all staged payloads and unique nonempty IDs before any publication. Invalid optional resources decline normally; external source mutation fails the cold attempt while preserving callback-owned changes. Review also identified the existing ThrowingState relocation callback path; its focused counter reproduced RED before the source correction.

Bridge disconnected during baseline profiling at23:06Z; Unity remained responsive. Reconnected the existing local session through its UI. Storage inspection23:19Z:327Gi available; no deletion. Current delivery-record commitacf617784 is remotely verified; packages a72 remain unchanged.

Native wider regression:212/212 passed (24 margin tests plus188 adjacent composition/geometry/actor cases). Third review issue reproduced with one failing ThrowingState relocation-callback test, then explicit scenery exclusion added. Final focused25/25 passed; together with the unchanged188 adjacent cases,213 distinct tests cover the final narrow guard revision. Seed65 placements observed in native test output:6owners around32,7;5around41,7;4around54,17; three Trees and12Bushes. This is native integration evidence, not actual gameplay acceptance.

## Actual result and independent review — 2026-11-01

Fresh ordinary cold travel from the immutable precursor produced all15 real owners, IDs12104–12118: three Tree and12Bush. The matched entry is40,12HP40. Native1× west24,12/center/east46,12 views and2× close inspection show three separated margin groups without filling the crossing. Tree and Bush Examine use correct approved bodies. Walked through Bush41,9 to41,8; Up remained41,8 and struck Tree12110, structure30→28. Existing hostile snakes blocked several attempted steps; ordinary right/down and west-lane bypasses reached both comparison viewpoints. No teleport, FOV injection or execute_code gameplay mutation was used. F12 protected this visual traversal, then was disabled before F5turn2280 at46,12HP40. This is not encounter-balance acceptance.

Fresh isolated Continue preserves all15 sorted IDs, positions and structural health, including the damaged tree. Saved/reloaded world pixels differ0/639200; collection enumeration order changes on load and was sorted for comparison. Evidence: `FieldComposition/actual-state.json`, `reload-comparison.json`, `after-save/Quick.*`, `after-saved.png`, `after-fresh-continue.png`. Three failed read-only observations (wrong Cell method, wrong save-service type) changed no gameplay; corrected against saved source.

Independent source reviewer `coverage_priorities` found0 remaining P1/P2 after the three targeted corrections. Independent actual reviewer `capture_route` found no new visible obstruction across center/west/east/2× and confirmed the bounded reload. Existing southwest/northeast vegetation remains stronger than the new groups. The result is modest field-margin dressing, not a substantial populated destination. Repetitive crop rectangles, broad green gaps and unrelated black-backed glyphs remain. Secondary cold-generation props/actors and side-view time bands differ from baseline; this is a composition comparison, not vegetation-only pixel parity. All current art, original generation receipts, routes and source yields remain protected by native controls.

## Performance and delivery limits

The clean after observation lasted66.97s (21,315 samples), with ordinary movement/readers and no execute_code in its sampling window. Renderer p95 .202ms, max181.27ms; Editor MainThread max2.004s; max allocated-in-frame649,677,184bytes. These spikes remain unexplained. The initial149.86s baseline includes bridge reconnection; the first after attempt includes input-focus recovery. Both are retained and excluded from clean comparison. Separate cached pre-change graph observation lasted65.47s: renderer p95 .085ms/max175.38ms; MainThread p95 1.890ms/max185.38ms; GC max42,419,168bytes; batches47 versus after45. The cached baseline begins later in the encounter at46,12HP32 and uses a different short movement/Grass-reader route, so this is a bounded control, not matched proof of regression or improvement; no speedup, packaged responsiveness or hardware claim is made. R11 remains open.

Current Mac/Linux packages are sourcea72a22983 and do not yet include this composition. Exact reviewed source/evidence publication is pending, followed by a later batched delivery. Preserve75 unrelated tracked hashes; no storage cleanup. Next compare Ginmere/Deepest Cathedral StoneFloor versus saved-lair Grass and shared WoodenBarrel gaps. Existing evidence favors the continuous sinkhole floor and approved Sill stone identity; actual baseline must confirm it before implementation. Crop mechanics and optional animation stay deferred.
