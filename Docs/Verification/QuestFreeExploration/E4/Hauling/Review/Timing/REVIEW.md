# F9 timing triage — read-only source review

29 September 2026. **No plausible source-level explanation was found for the reported threefold legacy coverage slowdown.** This is a narrow exclusion review, not proof that the candidate has no performance regression.

Baseline: `d0fb582ca14131d3828384a701e46d6bdeb0e37f`. Candidate: root's frozen F9 working-tree source. [source-hashes.json](source-hashes.json) records exact SHA-256 for every examined baseline/candidate file, with null baseline for newly added files. No source, test, settings, thresholds or production assets were changed; no tests were run for this review.

## Trigger and evidence provenance

Root reported the ongoing native full run at17,866/21,862 with no functional failures, the disabled diagnostics benchmark at295ns against its existing<200ns ceiling, and all three142-surface Spread coverage cases exceeding180s at approximately213–215s. Root also reported complete census outputs with zero geometry/style failures and owner counts313232/313350/316241, matching the prior baseline. Those current results and counts are parent-reported interim evidence; this review did not export or independently inspect their native raw output.

The retained baseline raw file was independently read: [native-workspot-full-suite-green.json](../../../Integration/native-workspot-full-suite-green.json), case `CavesOfOoo.Tests.SpreadBiomeCoverageTests.EveryActualSurfaceAndCommittedLairHasNativeGeometry`:

| Seed | Baseline native duration | State |
|---|---:|---|
|64|66.5578474s|Passed|
|1|71.7013697s|Passed|
|1729|64.0075169s|Passed|

Root previously reported focused F9 native257/257 in102.7367s. That supports the separate focused behavior gate; it neither explains nor dismisses the broad timing failures. The desktop being locked is an unproven possible condition, not an established cause.

## Source exclusions

1. **The142-surface coverage uses legacy generation.** `Assets/Tests/EditMode/Presentation/Rendering/SpreadBiomeCoverageTests.cs:45` calls the two-argument `CreateDetached`. `Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs:54` passes `enableExploration:false`, and line61 installs `SpreadExplorationPlan.Legacy`. `Assets/Scripts/Gameplay/World/Generation/SpreadExplorationPlan.cs:66` creates the disabled legacy plan; lines116–121 refuse placement without an enabled current entry. The F9 source flags in `OverworldZoneManager.cs:75,78` are inside the placement branch at70 and additionally require the selected HeavySalvage family/version. The F9 helper and new Hedge/haul receipt capture therefore do not run across these142 surfaces. The fixture's constructor separately creates one initial ordinary Grove graph (`SpawnRing3DIntegrationTests.cs:53–57`); that does not opt the later coverage manager into F9.
2. **The ID-count optimization is not new work on the legacy path.** `Assets/Scripts/Gameplay/World/Generation/SpreadWildernessSituationPlan.cs:72–85` builds a fresh per-validation ID dictionary only for `CaptureFinalState`. Production caller search found only the opt-in exploration builder, cooking, passage and hauling helpers. Legacy `Assets/Scripts/Gameplay/World/Generation/Builders/SpreadWildernessSituationBuilder.cs:86–88,104–110` uses `IsCurrent`/`MatchesOwnedState`, which continue to call `OwnerSnapshot.Matches` with a null dictionary (`SpreadWildernessSituationPlan.cs:61–67,115–119`). Its ID uniqueness check still uses the original zone scan, with an added null branch; no cross-callback cache, extra dictionary scan or widened source capture occurs here.
3. **The added hauling cue is outside the coverage render path.** `Assets/Tests/EditMode/Presentation/Rendering/SpawnRing3DIntegrationTests.cs:48–57,84–90` directly creates and binds `SpawnRing3DPresenter`, computes light, refreshes it and calls its frame method. It creates no InputHandler/ZoneRenderer world-affordance binding. `Assets/Scripts/Gameplay/Interaction/WorldAffordanceQuery.cs:139–157` adds the Haul/Let Go query; it cannot run through this fixture's direct presenter calls. Presenter and ZoneRenderer sources are byte-identical to baseline.
4. **No changed diagnostics implementation or benchmark.** `Assets/Scripts/Shared/Utilities/Diag.cs` and `Assets/Tests/EditMode/Gameplay/Diagnostics/DiagPerfTests.cs` are byte-identical. The latter explicitly resets diagnostics at50, disables its measured category at60, warms up at61–62, and asserts zero records at71–73. New hauling test scope (`Assets/Tests/EditMode/Gameplay/World/SpreadHaulingSourceTests.cs:11–21`) and its pipeline fixture do not alter diagnostics channel settings. This does not rule out unrelated process/session effects; it excludes an observed F9 diagnostics source change.
5. **The unconditional generation difference is tiny.** `Assets/Scripts/Gameplay/World/Generation/Builders/HaulablePropBuilder.cs:74` resets one receipt reference and increments a per-builder revision. Receipt creation at102 stays conditional on `CaptureSourceReceipts`. Other branch changes preserve the original spawn call and RNG. This does not provide a plausible mechanism for an additional roughly150s over142 zones. The fresh manager briefly creates current metadata before the detached overload replaces it with Legacy; changing its version/family selection does not add per-surface runtime work.

Matching owner counts support unchanged census population, but do not prove identical timing, native resource state or render work. This inspection did not measure allocation, locks, GC, editor focus/background throttling, native graphics stalls or heat/power conditions, and it did not audit every unrelated test for state leaks.

## Smallest next check

After the full run finishes, repeat **one identical failing142-surface coverage case and the disabled diagnostics benchmark** with the frozen source, same settings and unchanged thresholds, in the quiet session. Preserve raw results and report their actual environment. If the slowdown persists, perform the same bounded case against baseline and candidate under matched session conditions; separate generation from native render/inspection time if needed. Do not increase timeout ceilings, disable rendering, change quality settings or rewrite production to make the tests green without establishing the cause.

Root owns those repetitions and any resulting diagnosis. This note deliberately does not claim an environmental failure, a performance fix or a passing full regression.
