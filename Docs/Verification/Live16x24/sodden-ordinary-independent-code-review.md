# Ordinary Sodden scope review

Bounded read-only source pass: no actionable correctness defect found in the now-applied single-catalog runtime patch and imported ordinary-region tests. Native functional regression is verified below; actual post-integration gameplay acceptance remains with root.

## Reviewed scope

Read the plan, `/tmp/coo-sodden-ordinary-runtime.patch`, the imported22 ordinary cases, actual `SoddenCompositionPlan`/builder and receiving-manager pipeline, `WorldLocationContext`, the lair read-only query/restore behavior, and shipped material/thermal/coating definitions. No Unity, Git, active source edit or test execution was performed by this reviewer.

## Runtime assessment

- The finite dictionary constructs canonical addresses once from the actual authored wilderness whitelist plus exact district IDs. It stores coordinates only, uses ordinal keys, and cannot grow with negative lookups. It replaces both old district qualification and old fixed-row coordinate lookup together.
- Current receiving-manager/cached-graph reference, exact map-array shape, current biome and current POI remain checked at the selected coordinate on each query. A warmed positive is not cached authority. Canonical authored Places/mouths and foreign authored biome addresses remain excluded even if current map labels change.
- `LairStacks.HasSavedColumn` reads the existing weak-table ledger and record dictionary without initializing/adopting a ledger. Both modern and legacy saved claims are excluded after their POI label disappears. The rendering patch does not use generation or save-restore operations.
- Original owner/state/resource/subject/footprint/fog guards and actual substrate rules are unchanged. Ordinary builders instantiate the same blueprint materials. Mire starts at10 degrees with freeze default0 and flame400; wood/tree/bank defaults remain above freeze and below their flame thresholds. `EffectiveCoating` converts a literal aqueous MirePool's legacy water source to bog-mire, while Copse's independently written plain water stays water. No expected ordinary-owner default unexpectedly fails the accepted predicates.

## Test assessment

The six positive cases bind the actual captured seed65 addresses, verify unchanged map/formation eligibility and use the full cold generation pipeline. Selection uses the real fallback owner rather than assuming the first matching entity is visible/selected. Tests retain reader/material identity, dry substrate, state/version stability, custom-variant refusal and restoration.

Mutable biome/POI/array shape, unbound/replaced graphs, current/legacy saved lairs, static authored exclusions and a noncanonical alias have explicit countercases. The lair absence test addresses the earlier review caveat: it uses a manually attached fresh graph and directly checks CWT presence before/after public queries without invoking an initializing helper. Copse tests use actual generated water-coated Grass/DeadTree and submitted hazard tiles. The Causeway test removes a real neighboring board through destruction and checks the exact remaining connection mask, quiet subsequent frame and no extra full redraw.

Explicit bounded decision: the separate ordinary missing-atlas case is omitted from these22 additions. The resource path, constructor, optional-group handling and submitted rendering are unchanged and covered by existing district injection tests; the scope extension adds no scope-specific resource branch. This omission is documented rather than counted as new ordinary resource evidence. Root accepted this boundary; no duplicate resource matrix is required for this patch.

## Evidence limits

Read retained `sodden-ordinary-native-red.xml` directly:22 total,0 passed,22 failed,0 inconclusive/skip, against the district-only runtime. Root then applied the single-catalog runtime patch. Read retained `sodden-ordinary-native-green.xml` directly:186 total,183 passed,0 failed,3 existing allocation probes inconclusive,0 skipped. All22 newly added ordinary cases passed. The existing allocation probes are not successful zero-allocation measurements, regardless of the aggregate suite status. Previously observed six ordinary before layouts are baseline gameplay, not proof of new native coverage. New post-integration screenshots, plain-water distinction, whole-area fullness/residuals, travel, save/revisit, final native results and main/package delivery remain root-owned. Allocation probes on this Editor were previously inconclusive; dictionary construction being bounded/one-time is source evidence, not a measured zero-allocation claim. Six formations do not certify all Sodden owners or other biomes.

## Review binding

Recorded 2026-10-27T05:59:41.153034+00:00.

- `/Users/steven/caves-of-ooo/Assets/Tests/EditMode/Presentation/Rendering/FlatPixelSoddenTravelTests.cs` — SHA256 `9017bfce0ad762f6462d74c3d756ad4fe127292d7dfc339b8ab2c658317a6463`
- `/tmp/coo-sodden-ordinary-runtime.patch` — SHA256 `aeacead00b7dff05d1adbbaacd652da5a54e49ebcd975f9203af928cb18e30ab`
- `/Users/steven/caves-of-ooo/Docs/Verification/Live16x24/sodden-ordinary-next-plan.md` — SHA256 `1c8973ffa32e75c6f6e93cab9e0e188b2bcd77ed72caef11eb38b499ee825473`

Applied runtime reviewed 2026-10-27T05:59:58.254613+00:00.
- `/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/FlatPixelSoddenTravelCatalog.cs` — SHA256 `a5cc6c23f9fcfc5112909cbdcb3289a3b10c5b14b066c6ea34dee53753fe538b`
- `/Users/steven/caves-of-ooo/Docs/Verification/Live16x24/sodden-ordinary-native-red.xml` — SHA256 `3f4cc657c5da4eeefbe8a3ebbfea7d7a35209b687b7feec09ad9fc79a59a1332`

Native result updated 2026-10-27T06:06:02.989796+00:00.
- `/Users/steven/caves-of-ooo/Docs/Verification/Live16x24/sodden-ordinary-native-green.xml` — SHA256 `f38c2ca8decdfa2981b599eab909255dca5f7f18ea40d97471173edf62bd4a84`
