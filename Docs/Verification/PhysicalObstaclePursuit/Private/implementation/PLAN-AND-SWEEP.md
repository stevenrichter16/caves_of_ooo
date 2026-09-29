# Physical obstacle pursuit — narrow implementation slice

Status: private preparation. Root must publish F11, adopt tests only and execute native RED before adopting production. No shared Assets, Unity or Git changes. No tactical hauling variant, model, combat ability or content budget expansion.

## Goal

Ordinary single-cell pursuers must take a real available detour around stationary beams, barrels and hedges. `ignoreCreatures:true` permits planning through temporary Creature-tagged actors, not stationary Physics.Solid props. Actual barriers may delay or deny passage. This repair grants no ability to move through, destroy or use props as sight/projectile shields.

## Verification sweep before implementation

All ten audited source/content hashes remain identical; FindPath is `a17334766c9cf75dcd535d7cc09024774672ece05c28737d99297daca493f724`. The comparison is retained in `verified-before-source-hashes.json`. CLAUDE.md, ADVERSARIAL_TESTING.md, PERF-FOUNDATION.md and runner honesty bounds were read.

| Premise | Verified correction | Scope consequence |
|---|---|---|
| Every physical obstacle has Solid tag | Actual beam/barrel/Hedge do not. Movement blocks them; IsPassable deliberately uses tags/doors. | Fix path occupancy, not content tags or LOS. |
| ignoreCreatures skips only creatures | Legacy single-cell branch skips its entire Physics check; misleading helper sees every Physics.Solid owner. | Always run the predicate, omitting only Creature-tagged owners when requested. |
| Usable A* proves actual reachability | It can start inside a real prop every time. A separate ten-step detour is fully executable. | Test ordinary movement and scheduler endpoints. |
| Every lone prop strands pursuit | Thin props escape via fallback diagonals. | Retain thin-owner controls and open-ended complete-boundary cases. |
| Wide body can preflight to occupied player anchor | Wrong: actual KillGoal uses ToContact. | Original four setup failures remain rejected; corrected fixture uses contact semantics. |
| Doors require new behavior | Capable stationary opening, locked/incapable refusal already pass. | Preserve DoorStepCost and one-action contract. |
| Multi-cell code shares the defect | Zone.CanPlaceFootprint correctly distinguishes creatures from props. | Preserve footprint/contact branch. |
| Creature tag overrides structural tag gates | Not assumed: existing Solid check runs first. | Preserve Solid tags, doors and legacy goal-cell handling. |
| Private logic proof is native verification | False; .NET10/stubs are a pre-check. | Separate actual-reference compilation, native tests and native inputs. |
| New cache/events are needed | This is one existing loop/predicate, not a new action. | No retained routes or per-node diagnostic payloads. Existing AI thoughts/movement/turn lifecycle and explicit native position traces remain action witnesses. |

## Exact proposed change

Only FindPath.cs changes. Rename its private broad Physics helper to describe physical occupancy, pass the existing ignoreCreatures flag, and invoke it regardless of that flag. In its indexed occupant loop, skip only Creature-tagged owners when requested; otherwise respect Physics.Solid. Update misleading comments. Preserve the preceding Solid/door checks, goal-cell exemption, footprint/contact branch, costs/heuristic, diagonal policy and execution pipeline. No new allocation, saved field or content change. With ignoreCreatures false, the physical truth table is unchanged.

## TDD / native sequence

1. Preserve original evidence: initial43=27P16F includes four rejected wide-body preflight errors; corrected43=31P12F exposes one defect. Maintained test bodies stay identical; only class comment clarifies scope. Reuse the already unique hand-authored meta. Compile actual runtime and separate full native-reference test assembly before handoff.
2. After F11 publication root adopts only `baseline-test-only-manifest.json`, runs43 unchanged and exports genuine native RED. No production adoption before it.
3. Privately run the one-file candidate against43 cases, then affected compilable neighbors. Do not repair unrelated environment failures. Retain baseline/candidate evidence separately.
4. After native RED root may adopt production only on exact matching preimage. Run43 plus selected existing path/goal/door/terrain/body/turn/haul/F11 callers. No loosened thresholds or disabled counter replaces the actual route.
5. Native player witness: real existing pursuer/prop/open bypass, bounded actual paid inputs, actual positions/outcome. Disclose observer setup. Expect a detour, not frozen AI called tactical success. No new models/content for this proof.
6. Update dedicated living doc and priority record in the same eventual commit, using CLAUDE §2.3. State this is CoO's existing navigation repair, not a copied-Qud parity port. Preserve source premises, setup correction, private/native limitations, actual counts and source/test/doc list.

## Counter mapping (existing43 cases, no speculative expansion)

| Method(s) | Contract and counter |
|---|---|
| ActualBlueprintBlocksMovementWithoutBeingAVisibleWall | Three actual props block real movement while remaining transparent to the tested sight ray. |
| SearchAvoidsStationaryBlockersWithEitherCreaturePolicy | Both creature policies respect static props; ordinary Wall control remains correct. |
| IgnoreCreaturesChangesOnlyTheOccupyingActorRoute | True can plan through a Creature; false detours; real move into occupant still refuses. |
| SingleOwnerStillHasAnExecutableFallbackAroundIt | Thin props preserve movement; failed direct attempt is not success. |
| OrdinaryApproachMustUseTheRealOpenEndOfABoundary | Actual boundary detour versus otherwise-identical Solid-tagged owners. |
| ScheduledKillGoalMustReachPlayerAcrossOpenBypass | Brain/energy scheduler endpoint versus wide-body/Wall controls; no HP or energy duplication. |
| OneCellExplicitFootprintPreservesPhysicalRoute | Legacy and explicitly declared one-cell body both need executable first step. |
| ClosedDoorKeepsItsExplicitOperationContract | Capable actor opens stationary then moves; locked/incapable refuse. |
| SealedPhysicalBoundaryMustNotReturnAUsableRoute | Sealed physical and ordinary Wall boundaries both reject. |
| LoadInHedgeThroatRequiresRealDetourInsteadOfPermanentImmunity | Plugged throat detour versus removing only the load. |
| RespectingPhysicalOwnersProducesAnEntireExecutableBypass | Entire measured route executes actual movement around each of three authored barriers. |

Eleven methods contain43 cases. Twelve failures identify one defect, not twelve bugs;31 controls must stay green. Existing FindPathTests.PathToSolidGoal_IsUsable pins the intentional legacy endpoint exemption. Scoped neighbors cover doors, costs, contact geometry, following, drag lifecycle and finite F11 callers. Retained diagnostic/source snapshots and result traces are separate from FPS, native input and complete world-service claims.
