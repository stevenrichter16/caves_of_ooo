# E1 — bounded exploration manifest, save and exact graph retention

Implementation readiness after the preserved E0 baseline. Shared activation remains **off** until the source/role tranche is ready. This package is infrastructure plus explicit fixture opt-in; it does not claim that four new families are available to players.

## Sweep and decisions

`SWEEP.md` was recorded before production. The actual lifecycle owner is `Assets/Scripts/Gameplay/World/Map/ZoneManager.cs`. Normal travel caches graphs; explicit `UnloadZone` removes them and their connections. No new cache, disk archive, automatic eviction or global turn work was added. The existing full graph serializer remains authoritative.

The manifest binds after current rare/wayhouse selection. Its finite supported 20×20 surface domain is intersected with the actual map and POIs, then stores separate frozen placement and persistence masks. ReferenceGlade is deliberately retained but never receives a new principal situation. Ordinary quiet/rare/regional addresses retain exact accepted graphs; settlements and specialized POIs retain their existing lifecycle. The four nonquiet names are the closed first-tranche catalog, not twelve speculative live families.

Assignment is one ranked greedy pass with separate stable actor/reward salts, legal formation-family admission, a 35% initial quiet draw and strict same-family edge exclusion. No repair pass is needed to satisfy the hard exclusions. This does **not** promise a final 35% quiet rate: ecological exclusions and neighbor refusals add quiet outcomes. Neighborhood diversity, realized placement rates, topology visuals and memory/save-size measurements remain subsequent census/native gates. This does not make existing full generation/loadout RNG visit-order independent.

Load construction is explicitly unbound, distinct from fresh detached construction. Graph attachment during decode does not install retention. Only post-body, pre-global-activation metadata validation binds exact restored references. Absent metadata stays legacy, including missing old chunks; valid empty metadata stays empty. Malformed/future/foreign/duplicate records or an installed row missing its saved graph reject the candidate. There is no migration or silent reroll.

The first generation guard captures exact plan/map/rare/wayhouse references and selections before producer callbacks. Every retry replaces its weakly keyed attempt. Only final acceptance installs the exact graph; current-address clones and failed attempts cannot acquire retention. Installed graphs remain retained if the current map later changes. Lost/replaced retained graphs fail access/save instead of refilling.

## Integration API

- `OverworldZoneManager.Exploration`; existing default constructors remain disabled.
- Explicit fresh fixture opt-in: `CreateDetached(factory, seed, true)`; refuses late manifest creation after graph access.
- `Entries` and `Find(id)` expose immutable saved assignments without generation.
- `TryGetPlacement(manager,id,out entry)` checks current placement admission.
- `TryGetGenerationEntry(manager,zone,out entry)` additionally requires the exact active cold-generation token. Composers must use this.
- `TryMarkPlacementCommitted(manager,zone,exactEntry)` succeeds once, only for a nonquiet actual active entry. Call after the supported transaction succeeds; failure must abort the staged graph. Final acceptance still validates the captured authority.
- `DispositionFor(id)`: 0 not accepted, 1 accepted/no-site, 2 accepted placement. No read causes generation. Saved graph authority and immutable assignments remain separate.
- The root-owned `SpreadExplorationTopology` and builder field are dependencies; this package sets the typed field and leaves legacy entry points intact. Future role/composer wiring is a separate change.

## Test-first evidence and corrections

1. Initial core fixture: `manifest-red.xml`, **29 cases / 28 expected feature RED / 1 legacy control PASS**, followed by `manifest-first-green.xml` **29/29 PASS**. Actual-reference test compilation succeeded against the original runtime using reflection.
2. Callback/restore controls: initial actual54-case run `adversarial-matched.xml` was **53 PASS / 1 setup failure**. The failed legacy post-load generation used the existing helper's intentionally null factory. Corrected that one setup to use the actual ordinary factory in `SaveReader`; this was not a production defect.
3. Seven exact placement-token/disposition checks: `disposition-red.xml` **54 PASS / 7 missing-API RED** before the marker/query implementation; then all61 feature checks passed.
4. Numeric-culture counter: `culture-red.xml` **1 genuine wire-format RED / 1 control PASS**. Negative world seed formatting used the mutable current culture; fixed the seed field to invariant formatting. The two tests restore the prior culture.
5. Final `final280.xml`: **280 total / 279 PASS / 1 unchanged private-runner failure**. Both new fixture classes total **63/63 PASS**. Matched original-manager/save baseline `baseline-neighbors.xml`: **217 total / 216 PASS / 1 identical Wayhouse seed1729 source-placement failure**. `differential.json`: newly failing **0**. The runner deliberately substitutes deterministic .NET string hashing and is not Unity's seeded map outcome.
6. Final actual Unity-reference runtime and full EditMode assembly compilations: **0 errors**. This is compilation only, not native execution.

Historical compile/setup mistakes are retained and labeled: `first-compile.log` (initial positional World argument), `adversarial-first-invalid-stale-run.txt` (an attempted chained run used the older DLL after a fixture compile failure), and `adversarial-premise-correction.md`. None is counted as valid passing adversarial evidence. The exact carried-item check now asserts the source cannot merge with the actual starter inventory before claiming its ID.

## Independent review and Q1–Q4

Combat peer read found no remaining concrete restore/current-authority blocker. It reviewed canonical bounded wire, explicit unbound restore, exact cached references, attempt snapshots, legacy separation and disposition validation. The carried-stack premise was tightened in response to peer review. No reviewer ran Unity for this package.

- **Q1 symmetry:** no-site/quiet and committed graphs share exact retention; only successful final acceptance can publish either. Fresh opt-in versus old absent, installed versus attached clone, immutable assignment versus current admission, and same versus replacement graph are paired.
- **Q2 cross-feature:** rare/wayhouse/lair lifecycle remains independent. The prior nullable-manager save boundary is unchanged. Save/load keeps complete current graphs and stock; there is no new loot, no player grants, no owner repair or factory work in metadata queries. Native full decode and post-load hooks remain separate execution gates.
- **Q3 counters:** bounded empty/invalid/duplicate/foreign wire, wrong seed/enums/masks, missing graph, same-ID clone, map/plan/rare/wayhouse changes, retry, exact token replay, foreign entry, quiet token, failed final callback, loaded replay and culture have explicit controls. The full paired legacy neighbor failure is reported rather than suppressed.
- **Q4 scope:** 63 core checks and compiler success do not prove ordinary travel, visual topology, realized family variety, first-hour balance, native exact seed results, retained memory or save size. Default activation is deliberately pending E2. Root owns native tests, current E0 walk, geometry and final activation.

Exact publication ownership is the eight paths in `production-manifest.json`; root's already-shared topology files are dependencies, not claimed as this agent's publication. No commits, staging, Unity calls or user-save writes were performed by this package.
