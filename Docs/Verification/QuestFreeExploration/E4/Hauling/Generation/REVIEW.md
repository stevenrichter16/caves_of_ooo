# F9 private implementation review

Base d0fb582c. Shared production was not edited by this agent. Root owns Unity, actual input/render evidence and publishing. This package uses existing original beam/barrel/hedge art; no blueprint or art changes.

## Source sweep and final scope

The retained proposal is implemented as one 13-cell boundary and broad straight-pull shoulder, four rotations, from exactly one ordinary 4200 beam/barrel plus twelve original undamaged terrain Hedges. At most13owners move; existing owners already on a final anchor stay. Candidate centers are ordered by distance to the original load, then y/x. Cheap required-cell eligibility precedes the32eligible-center cap; at most4full trials per center. Complete virtual occupancy includes vacated original anchors. Equal-cost eight-direction shortest paths match single-cell movement, and both blocked/parked layouts preserve original critical components. Actual blocked-state border reach is explicitly required.

Corrections before implementation: capture does not exist until added; do not scan for a matching blueprint. Prospective v7 selection must be frozen before source generation, including changed adjacency. Beam has no DestructiblePart; no beam Break behavior was added. Zone.MoveEntity has no mutation callback for these single-cell owners; tests exercise the supplied authority callback after a real move, rather than inventing a footprint hook. Stock, actors, materials, owned content and grounded plants receive no new live-moving privilege. Terrain relocation is cold composition only.

Version7 appends HeavySalvage11; Hedgerow allocation changes from thirds to quarters. Literal2–6 retain their branches and round-trip without graphs/backfill. New current pins updated; literal fixtures unchanged. No population, source chance/pool/RNG, item quantity, resource, save-schema, handling/speed or model behavior changed. A normal beam/barrel can be moved with the existing commands. Destruction is tested only for a source with its existing Destructible part; absence is valid for a removed beam.

## Test-first and findings

- Restart erased old /tmp proof. Fresh persisted baseline initial RED:23 total,5 prior-family controls pass,18 missing-feature fail,0skip. Native corresponding receipt is root-owned.
- Layout/source baseline RED:38 total,5controls pass,33missing-feature fail,0skip. Test-file split changes no cases.
- First candidate39/39 passed; fixed15selected addresses,6compatible ordinary sources,all6committed across3seeds. Other outcomes:3millstone,6absent. Caller and loadout/trader/death RNG tails matched disabled composer; exact original owner set and nonselected positions/parts/stock projections retained.
- Yellow finding1, fixed: a mid-mutation authority callback adding an off-route owner could commit. Executed adversarial20=19pass1fail, then complete owner-set guard in own mutation window. Keep independent owner in rollback; fail cold attempt if original graph cannot be restored.
- Yellow finding2, root review, fixed: final authority callback after the move loop could bypass the last complete-owner check. Executed paired2=1pass1fail. One-time commit now rechecks Others after Final; the retained later finalState intentionally permits normal resident naming/unrelated off-route content.
- Yellow finding3, fixed: first authority callback could add an owner before initial packet capture. Executed paired2=1pass1fail; capture and revalidate initial set/non-ground state before it.
- Test harness finding, fixed: census probes incorrectly ran for the away reference glade, whose pipeline has no haul builder. Save tests now probe selected HeavySalvage graphs only. This was an observer TargetException, not a gameplay bug.
- Legacy current pins measured RED before updating:161total,121pass40version/catalogpin failures,0skip. Literal saved-version assertions were left intact.

## Q1–Q4

Q1 symmetry: exact owner proof before/after each real relocation; original origin and intended destination tracked separately. Rollback only restores still-current, unchanged exact owners at their expected moved anchors. Independently moved/replaced/transferred/removed/damaged owners and new foreign objects remain untouched. Final own commit checks all original owners; later final validation checks the selected packet plus geometry, matching the preexisting ordinary naming phase boundary.

Q2 consistency: reuse SpreadGenerationReceipt for 0/1 haul claim and per-Hedge sources; no second ledger/save token framework. Helper returns owner plus transient final validator like cooking and passage. Optional refusal leaves ordinary sources; changed graph fails the generation attempt. Source capture default false, reset every Build, and produces no extra factory/RNG calls.

Q3 counters: exact source versus matching preexisting/replaced/moved/rebuilt/disabled; old mappings and literal wires versus new family; positive two-pull route versus invalid authority/source/space/hazard; final proof versus source/hedge/route mutation; clean rollback versus independent mutations at initial, per-move and final authority seams. Dedicated24 adversarial cases; actual accepted full saves exercise untouched, held, parked and absent load, plus retained graph return. Current neighbor family tests and fixed actual pipeline census are included.

Q4 honesty: standalone .NET10.0.5 uses deterministic patched hashing that differs from Unity. Corpus source counts/timings are private pre-checks, not native availability. No keyboard, survival, journey/discovery, visual fit, animation or scheduler-time claim here. Native floor and actual witness remain root gates. The source cohort is fixed at seeds1/64/1729; no seed shopping or forced load. Claim only the tested explicit stock/gear/parts projection in paired census, not a full serialization comparison of every unrelated private field.

Final executed private regression:228/228,0fail/skip,71.683seconds, including67new focused tests and161 current-version/literal/field/cooking neighbors. Native scope remains root-owned. Fixed corpus final remains6/6compatible,6/15selected,across3seeds; every no-source/millstone outcome retained. Full-save4/4 covers untouched/inactive,held/active,parked/inactive,removed/inactive and cached return.
