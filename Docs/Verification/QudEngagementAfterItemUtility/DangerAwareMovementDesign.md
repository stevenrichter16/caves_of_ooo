# E: ordinary movement uses existing danger costs

Implemented and natively verified, 2026-10-09. The 39 feature cases pass; comparison with 325 existing cases has no new failures. Controlled native Play passes 8/8.

## Player effect and scope

Enemies can choose a safer approach, retreat step or firing square when existing terrain rules assign danger to the direct option. This connects already playable oils, ice, gas, pools, grit and hot steam to more ordinary fights. There is no new planner, hazard definition, perception system, resistance or saved state. Current knowledge and target visibility remain the caller's responsibility; D1 still supplies observed or remembered retreat contacts.

## Implementation

- `AIHelpers.TryApproachWithPathfinding`: retain the original direct fast path when the prospective complete body has zero TerrainNavigationWeight cost. A dangerous direct step goes through existing actor-aware weighted A*. Costs remain finite and a sole hazardous path stays usable.
- `AIHelpers.TryStepAway`: leave clean-ground behavior unchanged. Only when the preferred destination is dangerous, compare at most three local outward choices, sorted by existing whole-body cost with stable ties. Pure preflight requires strict Chebyshev progress from the supplied contact, actual placement and applicable explicit-footprint corner legality. An immediately operable ordinary door or one existing matching-key unlock is eligible. A real stationary door/lock action ends the opportunity. No general escape search or hidden-target lookup is added.
- `CombatTacticsPart.TryPositionForShot`: rank the same currently legal, clear-shot destinations by the existing cost, retaining direction order for equal costs. A bounded scan/bitmask adds no candidate list and stops early at the first zero-cost candidate. The existing target visibility, first impact, ally protection, ability eligibility and one-action rules remain. Its preferred-range backstep uses the same retreat helper.

TerrainNavigationWeight remains unchanged. It accounts for actual positive gas density and matching GasImmunityPart; actual liquid contact and supported resistance; real floor slipping and grit; authoritative hot SteamEffect contact and heat resistance; and each prospective physical body cell. Decorative cloud state does not acquire invented damage. Damage resistance does not remove unrelated control/stat/slip danger. These queries inspect current terrain as the existing pathfinder already does; they do not add a knowledge map or promise perfect risk prediction.

Forced movement, push/pull, swaps, slips, traps, collision execution, action scheduling and save formats are unchanged. Existing one-cell direct movement permits a diagonal between two blocked side cells; explicit SpatialFootprint movement prohibits it. The new preflight preserves this native distinction rather than importing FindPath's separate single-cell corner rule.

## Verification evidence

- First 24 native cases:12 expected failures and 12 controls passed before production, in [first RED](../DangerAwareMovement/native-movement-red-and-claimed-supplies-green.xml).
- Additional 15-case RED:11 failures / 4 passes, in [counter RED](../DangerAwareMovement/native-counters-red.xml). One failure was an incorrect ordinary-one-cell corner premise; the corrected two-case native run [corner control](../DangerAwareMovement/native-corner-contract-before.xml) passed that ordinary control and failed only the expected explicit-footprint speculative-attempt contract. The fixture was corrected without imposing a new collision rule.
- Before-production controlled native Play [report](../SpreadDiscoveryExpeditions/Native/d2d8afe826114d2a981bdf2848b65988/report.json):7/8, only `danger_safer_actual_step` failed. Real oil selection/payment, visible film and exact saved branch were already working.
- Existing 16-class baseline:325 cases, 323 passed, 2 failures in [baseline](../DangerAwareMovement/native-affected-before.xml). Both are `MovableObstaclePursuitAuditTests.ScheduledKillGoalMustReachPlayerAcrossOpenBypass("Wall",False/True)`. Preserve these baseline failures and compare names after; this slice does not change opaque-wall knowledge/pursuit.
- Post-change native: **39/39 feature cases passed**. Combined with 325 existing cases, 364 total yielded 362 passes and the same 2 baseline opaque-wall failures. [final native XML](../DangerAwareMovement/native-affected-after.xml) and [comparison](../DangerAwareMovement/native-affected-comparison.json) record **zero newly failing cases**. The existing failures are preserved rather than reclassified or fixed in this slice.
- Post-change [native Play](../SpreadDiscoveryExpeditions/Native/ee41426c89234c4a86cd6b14190544b9/report.json): **8/8 checks, 6.49 seconds, zero failures or unexpected errors**, four paid actions and no pure-clock grants. The clean comparison reaches (41,12); after exact saved-branch restoration and real oil use, the same enemy chooses (41,13), cost 0 instead of the oily ideal (41,12), cost 30. The real film retains six turns. The coordinator inspected clean/detour screenshots and confirmed the visible purple oil film and changed actor position.

Tests cover direct danger and harmless controls, exact gas immunity/case, acid99/100 resistance, secondary feet, repeated progress, finite sole paths, cardinal/diagonal outward choices, unequal-axis contact, stable ties, legal bodies, no speculative blocked movement, door/key action boundaries, real steam, sanded ice, ranged cooldown/friendly safety and forced movement.

The Play route is a controlled fixture: ordinary duelist bootstrap, disclosed enclosed ground, one full-health factory marlback and one supplied FrogOil. Real F5/wait/F6 compares clean direct approach with the same saved enemy approaching after a real oil-use menu action. No post-setup HP/items/goals/energy/RNG/FOV grants. It proves only witnessed voluntary approach and input/payment/presentation; it is not ordinary acquisition, generated encounter frequency, difficulty, normal-fog discovery or a general performance benchmark. Retreat, firing, immunity and sole-route/force controls rely on separate native tests.

## Cost observation

The identical detached 500-helper-call sample changed from clean 1.6732 ms/danger 6.8151 ms [before](../DangerAwareMovement/native-before-micro-sample.json) to clean 2.1184 ms/danger 35.207 ms [after](../DangerAwareMovement/native-after-micro-sample.json). The after danger case is about 70.4 microseconds per call and actually chooses the clear diagonal; the old sample always used the dangerous direct step. The sample includes position resets and movement events. Extra weighted A* work is expected. This is a small helper microbenchmark, not FPS, a population/whole-turn profiler, an optimization claim or a hardware-wide performance guarantee.

## D2 remains deferred

The [finite Veilpuff readiness audit](FiniteCoverReadiness.md) records the deferred escape lesson and exact source/command evidence. The lesson is still useful, and historical native evidence shows a real 11.11 soursprayer. Current source admission still needs a fresh fixed-address probe: a worksite family alone does not guarantee the existing two-actor budget or a practical injured 4–6-cell retreat window.

The shared throw command currently applies gas/cloud effects before the inventory transaction commits. A rare exception in a later observer can restore the extracted item without restoring already merged gas/cloud. This is an exceptional-callback prerequisite for the proposed NPC feature, not evidence of an ordinary-play crash or an emergency defect. An external BeforeCommit wrapper cannot repair already applied effects. D2 awaits a separately tested narrow paid-release design; this movement slice introduces no general throw/gas transaction framework. Other verified item uses and independent C/D1 work remain valid.

## Source sweep and cold-eye review

The [pre-implementation readiness record](../DangerAwareMovement/readiness.md) retains the original source sweep, corrections, bounded design and test plan as written before production. This final record supersedes its pending implementation status.

- 🟡 Corrected scratch retreat candidates that could preserve distance on unequal-axis bearings; candidates now strictly increase distance from the supplied contact. Tests cover both bearings and reflections.
- 🟡 Corrected the review fixture's one-cell corner assumption. Native before evidence established the actual distinction; production preserves it rather than altering the global collision contract.
- 🔵 Independent cold-eye review compared approach/retreat/firing with actual movement, lock and body-placement code and found no further material issue. Native door/key controls verify one action; unchanged whole-body weights and finite costs preserve immunity, forced routes and contact rules.
- 🧪 The two baseline opaque-wall tests and bounded performance/Play limitations remain explicit above. D2 is deferred, not silently claimed complete.

Files changed: `Gameplay/AI/AIHelpers.cs`, `CombatTacticsPart.cs`; new `DangerAwareMovementTests.cs` and `DangerAwareMovementCounterTests.cs`; new controlled `DangerAwareApproachScenario.cs` and `SpreadDiscoveryNativePlayer.DangerMovement.cs`; minimal existing native launcher/player dispatch; fresh metadata and native evidence. No blueprint, content source, damage, save schema or visibility preference change. The temporary editor test-progress observer was removed after verification and native compilation remained clean.
