# E readiness: let existing terrain costs influence ordinary movement

2026-10-09, read-only preparation after D2 review. No Assets changes or Unity calls. Recommendation: this is the next bounded player-facing slice after C/D1 release. It connects already available gas, hazardous pools, oil/ice films, grit and hot steam to ordinary enemy choices. No new tactical planner, universal consumable policy, intelligence stat, immunity, hazard definition, combat knowledge or forced-movement restriction.

## Existing counterevidence and exact gap

The game already has real hazard-aware pathfinding. FindPath.Search adds `TerrainNavigationWeight.ForStep` to a finite10/14 movement cost (`AI/FindPath.cs:139–179`). It respects physical body placements, operable doors and diagonal corners, and permits a sole hazardous route. Existing GasNavigationWeightTests, DensityTerrainNavigationTests, DensityTerrainNavigationAdversarialTests and HotSteamNavigationTests prove costs and A* behavior. Do not describe avoidance as absent or rewrite those systems.

Three frequent choices bypass it:

1. `AIHelpers.TryApproachWithPathfinding:417–428` takes the unobstructed ideal step before asking weighted A*. A creature walks straight into the first acid/gas cell even if a one-step detour is clear.
2. `AIHelpers.TryStepAway:457–471` tries one preferred direction (plus the two axis components for diagonal threats) with no cost comparison. A wounded creature can enter a known physical hazard despite an equally outward clear step.
3. `CombatTacticsPart.TryPositionForShot:265–271` takes the first legal clear-shot direction. Its preferred-range backstep delegates to the same unweighted retreat helper. An available clean firing position is ignored when an earlier direction is hazardous.

## Authoritative weights to reuse unchanged

- ForStep takes maximum over each candidate occupied body cell, combining terrain/gas. It does not double-charge projected pools or large bodies and allocates no lists/events.
- Actual gas owners with positive density add15+density/4 capped90. Exact case-sensitive GasImmunityPart gas type removes its own gas cost. Cold/heat resistances do not imply a generic gas immunity. Current gas logic is broad by GasPool type; do not change that admission in this slice.
- Nonempty liquid pools produce existing damage/action-block/negative-stat costs. Full relevant resistance removes only supported damage, including incoming negative resistance changes and same-liquid immunity. Full acid resistance does not cancel mire's stat loss or a slippery floor. Unknown/empty/healing/water pools have no invented danger.
- Real slippery floor coatings add20+SlipChance/4 unless actual grit counters the slip. A tile-only acid coating has no invented liquid-pool damage. Flying is not a special exemption because current contact/slip rules do not provide one.
- Hot steam uses the same current authoritative SteamEffect, positive density, thermal source>100C, actual physical contact perimeter and HeatResistance<100 as contact damage. A decorative SteamCloud or tile cloud has no invented heat.
- Queries are pure prospective placement checks. They neither advance time nor expose the player FOV to NPC choice. Existing A* reads the current terrain; this slice must not claim a newly implemented knowledge/fog-memory system.

## Minimal design

### E1 approach: activate the existing weighted fallback

Keep the ideal greedy fast path only if its whole-body ForStep cost is zero. With positive cost, fall through to existing actor-aware A*. Keep its existing bound, door behavior and final greedy fallback if no usable route exists. A sole dangerous passage remains traversable; no danger threshold becomes a wall. Do not perform a speculative move before choosing a route.

### E2 retreat: bounded local choice, no new search

Keep hazard-free preferred movement and all existing no-hazard cornered behavior unchanged. When the preferred outward destination has positive cost, compare a tiny outward fan: preferred step; for a diagonal preferred step, its two axis components; for a cardinal preferred step, its two adjacent diagonals. Every candidate must increase Chebyshev distance from the *supplied known contact* and remain an ordinary legal body placement. The cardinal alternatives are a deliberate limited addition only to avoid a dangerous preferred step, not a general escape search when the original direction was blocked.

Order viable choices by current ForStep cost, stable preferred/legacy order on ties. Costs remain finite: if no safer candidate can be used, take the original hazardous route when legal. Never stand indefinitely because every choice is dangerous. Recheck before actual movement as normal; use the real TryMoveDetailed result so opening/unlocking a door consumes one stationary opportunity and stops the scan. Candidate evaluation is pure and must never call move/door operations as a probe. Do not change FleeGoal/D1 memory, counterattack, ages or control checks.

This conservative hazard-triggered fan avoids altering all existing cardinal cornered encounters or D1's newly verified blocker cases. A later general escape planner would be a separate design.

### E3 firing position: rank existing valid shot steps

Keep present target visibility/hostility/ability eligibility and every first-impact/friendly-fire preview. Of the same8 legal clear-shot candidates already considered, try lower ForStep cost first, with original direction order breaking ties. Preserve finite-cost movement if all legal firing positions are hazardous. Backstep uses E2 automatically. Hold-range/current-clear-shot behavior stays unchanged; this is not a new general “escape a hazard under your feet” policy. Do not cast and move together. No new per-opportunity list/closure beyond current method's existing allocations; a bounded scan/bitmask can rank8 candidates if needed.

## Movement and compatibility limits

Use actual source and complete prospective footprint. CanPlaceFootprint can admit an ordinary operable DoorPart when explicitly requested, but Solid/Physics-only obstacles retain their own rules. MovementSystem.TryMoveDetailed returns Moved or ActionPerformed for real door/lock actions; either means the opportunity is spent. Don't silently substitute TryMoveTo where the existing helper can open doors. Ranged reposition currently uses TryMoveTo and excludes occupied/solid candidates; retain that contract.

MovementSystem.ForceMoveTo, swaps, pushes, pulls, slips, creature contact events and trap activation remain unchanged: choosing a less dangerous voluntary step is not immunity to being shoved into a hazard. Do not call TerrainNavigationWeight to veto those mechanics. No new save state is needed.

## Native RED and counterchecks

Draft fixture `/tmp/coo-danger-aware-movement-tests.cs` exercises public current helpers and real CombatTactics/skills; it needs native compilation and actual RED before production. It does not invent APIs or mirror a future implementation. Some controls should already pass.

Required tests: approach avoids an immediately adjacent real acid/gas/ice cost with a clear alternative; full appropriate immunity restores direct preference, partial/wrong immunity does not; water/no-hazard remains direct. Secondary-body danger also changes choice. A sole hazardous passage remains usable and repeated approach reaches the known goal without oscillation. Retreat chooses a lower-cost outward option for diagonal and cardinal threat contacts, preserves clean preferred direction, traverses a sole dangerous exit and still performs at most one door action. Cooling ranged caster repositions through the cleaner actual firing square, with no damage or cooldown spent; immune and all-hazard controls retain legal action. Add actual grit and hot-steam integration controls using existing fixture APIs if this advances to implementation.

Affected suites: CombatPathfinding, MovableObstaclePursuitAudit, DensityGeneratedDoorAdversarial, the four weight/steam fixtures, FiftySecondCombatAI + adversarial, D1 FairRetreat + lifecycle, pursuit/multi-cell movement and force-move tests. Native Play should use an actual actor and actual oil/gas/pool placed by a real player command; show one enemy taking a clear alternative and a forced/sole route still working. Source/fixture tests alone do not prove normal in-game readability or performance.

## Scope and performance

E1 invokes existing A* more often only when the immediate ideal body step is dangerous. E2 is at most3 local candidates. E3 already scans8 shot candidates; ranking adds cheap pure costs. This is bounded but still measure a small before/after actor-turn sample; do not claim a speed improvement. Avoid arbitrary callback identity/renaming adversaries unless they expose real movement lifecycle violations. Ship the practical gap, then return to the finite-cover command prerequisite as its own slice.
