# Voxel world accessibility audit

Status: complete. VA07: 14544 passing tests, 32 identical baseline failures, no new failures or C# errors; all 28 added cases pass. User reports exploring ordinary biome chunks without encountering towns. Audit the installed game's complete voxel area coverage, actual native generation and travel, and a read-only copy of the current saved world. CoO-original audit; no Qud parity claim.

## Plan and verification sweep

1. Inventory every installed voxel composition/library and its normal world dispatch. Distinguish biome-wide coverage, exact named places, the original four chunks, and underground destinations. Do not equate an asset or preview with accessible game content.
2. Copy the latest save into an isolated validation directory. Load only that copy through `SaveGameService.LoadState`; census authored POIs and already cached zones before requesting any new generation. Compare relevant saved chunks with a new same-seed manager.
3. Exercise real edge transitions, world-map ascent/descent, and authored stair connections. Pin adversarial wrong-place/underground counterchecks. Inspect native owners and destination scope, not just matching zone names.
4. Fix confirmed defects with RED-before-production tests; make existing navigation discoverable if the audit confirms missing instructions. Review independent findings, rerun relevant/full verification as appropriate, and record precise coverage and limits.

| Initial premise | Verified source / correction |
|---|---|
| Fresh games start at the old Sill constant | SampleScene serializes `Overworld.2.6.0`, the west forest. Morrowfast is one chunk east. Continuing a save restores its active zone. |
| Added town profiles may never reach loaded maps | SaveSystem calls WorldMap.RehydrateAuthoredProfiles, but only for existing same-name village POIs. Cached entity graphs remain unchanged. Test the actual save before assigning causality. |
| Recreated towns are preview-only | Source inventory finds normal dispatcher branches; executable native travel and saved-state checks remain required. |
| The controls explain settlement navigation | `< >` currently says only stairs. Surface `<` also opens the world map; `>` enters the selected chunk. Villages have visible named `!` markers. |
| Recreated wilderness graphics imply every POI has authored settlement graphics | Early broad-biome renderer predicates and opportunistic POI dispatch can differ. Audit this boundary explicitly. |

References: WorldMapAuthoring, OverworldZoneManager, ZoneManager, ZoneTransitionSystem, WorldMapTraversalSystem, SaveSystem, GameBootstrap, SampleScene, ControlsReference, SpawnRing3DRecipes/Presenter, Village3DPresenter and all composition plan/kit registrations. Source inventory is independently reviewed by three agents. Initial baseline: DI08, 14,548 tests / 14,516 passing / 32 pre-existing failures / no compiler errors.

## Safeguards, performance and honesty bounds

Keep the user's original Unity session and saves untouched. All executable inspection runs in the isolated Unity project against copied save bytes. Census and navigation audits run on demand, not in gameplay hot paths. No renderer, camera, spawn or save migration changes are assumed necessary. New tests initially passing are correctness pins, not claimed RED fixes.

Script-observable: native dispatch, profiles, owner graphs, physical graph connectivity, transitions, map markers, renderer recipes and actual resource resolution. Not yet verified: live camera appearance in the user's running session, OS-level keyboard interception, navigation feel. Existing stale layouts, if found, must be reported distinctly from generation defects.

## Findings, review and implementation log

- VA01 loaded a copy of the 2026-09-16 22:11 UTC save (seed 141343545), active
  16.6.0. Of 25 cached graphs, 24 are ground and none is a town. All 17 named
  villages exist with correct profiles. Their newly generated blueprint censuses
  exactly match fresh same-seed controls. Stale town caches are not this report's
  cause. Sumphold lies immediately west and native entry succeeds.
- VA02 actual RED: 28 total, 20 pass, eight missing navigation-help assertions
  fail, no compiler errors. The 18 native travel tests and null-sink controls
  were correctness pins from the start, not claimed bug fixes.
- Updated ControlsReference: both full help and four-line boot summary teach
  surface < / Shift+comma, > / Shift+period, and ! settlement markers.
- VA03: all 28 new cases pass. Seven older WorldMapTraversal tests emit unknown
  Crate errors with their minimal factory; VA06 repeats exactly those failures
  when that fixture runs alone. Retained the tests and recorded their filter
  dependence rather than suppressing logs or changing gameplay.
- 🟡 Resolved measurement issue: the first batch recipe audit omitted the pilot
  catalogue. VA04/VA05 supply it; the original 3.7 pilot has no recipe misses.
- 🟡 Resolved test isolation: preserve prior TurnManager/World, service factories,
  loot-table dictionary contents and initialized state around native walking.
- VA05: 37 destination graphs per saved/fresh manager; 29 surface map descents
  and two edge transfers succeed (31/31). Full native walking tests cover all
  eleven recreated towns and all four reciprocal two-level stair stacks.
- ⚪ Nine sampled blueprint types still use fallback rendering; captured in
  VOXEL-CONVERSION-BACKLOG.md. Source address coverage is 386/400, not a claim
  that every object in those addresses has voxel art. Fourteen surface
  exceptions, ordinary underground and procedural POIs require separate work.
- 🧪 Independent source/asset/travel reviews complete. Browser-inspected snapshot
  guide renders correctly and selecting Sumphold reports one chunk west. No
  active Unity play-session screenshot or physical F12/<> key claim.
- Final full-suite result and 2,113-input source freeze are in VA08-closeout.

Files: ControlsReference.cs; new VoxelWorldAccessibilityTests.cs,
NavigationDiscoveryTests.cs, VoxelWorldAccessibilityAuditBatch.cs and their
metadata; accessibility/coverage/backlog documents; native receipts and the
standalone saved-world navigation guide. No blueprint, save format, terrain,
renderer, spawn, camera or asset changes in this audit phase.

## Final verification

VA07: 14576 tests / 14544 passing / 32 unchanged baseline failures / zero compiler errors. All 28 added tests pass after final isolation fixes. VA08 confirms identical failure names/messages, 2,113 unchanged main/clone inputs, unique new GUIDs, 31/31 native transfers and original-save byte preservation. Independent production/help and measurement reviews complete; no unresolved material defect in the help change.
