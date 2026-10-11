# Iteration 8 — companion navigation

Status: standalone RED → GREEN, native Unity GREEN and bounded arranged Play checks complete.

## Scope and sweep

Recruited companions should use the existing actor-aware movement policy when following around buildings or hazardous terrain. This is a CoO integration change, not a new Qud parity claim.

| Premise | Verified source | Decision |
|---|---|---|
| Following already uses combat navigation | `FollowLeaderGoal.TakeAction` calls `TryStepToward`, a greedy direction with lateral fallbacks | Route the full follow goal through existing `TryApproachWithPathfinding`. |
| A new planner/cache is needed | `AIHelpers.TryApproachWithPathfinding` already provides a cheap harmless direct step, actor-aware weighted A*, normal door actions, then unreachable fallback | Reuse it without adding path state or allocations. |
| Every hazard must be forbidden | Existing terrain costs allow the only available hazardous route and honor resistance | Preserve that rule; test both detour and forced-route cases. |
| Follow timeout must change | Existing `FollowLeaderGoalTests` explicitly assert timeout and persistent close/cross-zone semantics | Retain lifetime behavior; this slice changes route choice only. |

Performance: the harmless direct step remains cheap; existing pooled `FindPath` handles obstructed/costly steps, with no new cache or per-turn collection. See `Docs/PERF-FOUNDATION.md`. Combat assist is another agent's adjacent iteration; only movement/comment hunks are owned here.

## Verification plan

Run actual follower `TakeTurn` events with a live leader, party link and follow goal: route around a U-shaped obstruction, choose an available clear hazard detour, preserve direct movement for harmless/immune actors, allow a sole hazardous passage, and avoid teleporting through an unreachable enclosure. Door capability/lock counterchecks preserve one stationary open action. Run in an isolated standalone runner first, then root-owned native Unity checks. Standalone checks cannot establish native rendering, serialization edge cases or play feel.

## Implementation log

- Plan and source sweep recorded before production.
- Initial test setup omitted Strength/Toughness, so acid had no actual coating exposure; corrected the fixture and reran RED before production. This was a fixture correction, not a relaxed expectation.
- Observed RED (`iteration08-red.xml`): 32 tests, 30 passed, 2 failed exactly on building-wall routing and acid detour. All 22 existing follow-goal cases and 8 new counterchecks already passed.
- Changed only follow movement to the existing `TryApproachWithPathfinding` helper and its stale explanatory comments.
- Observed GREEN (`iteration08-green.xml`): 32/32 passed, including 10 new full-goal cases. Runtime: .NET 10.0.5, macOS, isolated `Tools/EditModeRunner` copy; not native Unity.
- Root native final integration job `941ad3a3628542c19f5fda2b0a9e49b4`: **810/810 related tests GREEN**, including all **10 navigation cases**. Raw receipt `native-final-integration.xml`; focused extraction `iteration08-native-green.json`.
- Files: `FollowLeaderGoal.cs`, `CompanionNavigationTests.cs` plus meta, this log and paired XML receipts.

## Self-review

- 🟢 No new planner, mutable route cache, lifetime rule, hostile-target selection or save layout.
- 🟢 Counterchecks cover water, actual acid immunity, sole hazardous passage, moving leader, close idle, unreachable enclosure and capable/incapable/locked doors.
- 🟢 Existing follow goal and party link remain the exact same objects after building navigation.
- 🟢 Native compilation and all navigation cases are GREEN.
- 🟢 Root Play run `4d1ed0c4ed82479096c14632d2ad67e6` completed **14/14 checks with zero unexpected errors**. Ordinary two-cell paid walks visibly show the arranged companion staying and resuming follow; root reviewed all eight screenshots.
- ⚪ This short arranged route does not prove natural recruitment, all-zone navigation, campaign balance or physical Steam Deck feel/performance.
