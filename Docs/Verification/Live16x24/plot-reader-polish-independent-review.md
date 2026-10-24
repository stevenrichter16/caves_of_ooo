# Independent Look refresh source review — 2026-10-24

Reviewed `/tmp/coo-look-focus-draft/production.patch`, tests patch/full fixture and sweep against existing ZoneRenderer, LookQueryService, HotbarSaveFixture, HaulingContentScope, ZoneTileStateSystem and PerformanceDiagnostics source. No Unity or repository writes.

No actionable P1/P2 source finding. The dirty trigger is captured before consumption, while the query runs after world/FOV paint and before sidebar submission. Current cursor mode, exact zone/player references, canonical cell and disclosure gates prevent the stated stale-reference paths. Missing snapshots recover on reveal; focused/full dirty work refreshes; unrelated and quiet frames retain identity. Explicit snapshots without a bound cursor remain compatible.

Fixture setup uses owned inactive renderer/tiles/camera, actual FOV before entering Look, real menu command dispatch and current production APIs. Target headers fit the submitted sidebar. The stale-FOV test deliberately establishes previous visibility and changes FOV via full repaint. Same-ID zone replacement and canonical-cell replacement distinguish references from coordinates. Borrowed world hooks and factories are restored by existing fixture scopes; extra tile render weak binding/callbacks, random and performance snapshots are restored locally.

Source readiness was sent to root and projection_cold_review. Native compilation, discovery, behavioral RED/GREEN and ordinary Play remain root-owned gates; this review makes no claim they ran. Coverage is narrow Look-cache repair, not a redesign of LookQueryService authority or all UI timing.

Separate save/reload receipt is `/tmp/coo-plot-save-reload-comparison.json`: 197 owner rows compare as exact multisets (all recorded fields and duplicate multiplicity); 10 inventory rows compare exactly in order; all non-label top-level values match. Only label and owner enumeration order differ. This proves equality of the bounded observation records, not complete save internals, owner identity or every zone.


# Cold review — ordinary PaleSalt Examine regression pin

Reviewed the exact working-tree diff in Assets/Tests/EditMode/Presentation/UI/MaterialGuidanceInventoryTests.cs, its real announcement handoff/dismiss helpers, unchanged Snapshot assertions, current factory-authored PaleSalt prose, and existing generic Examine identity tests. No repository edits or Unity access.

No actionable P1/P2 finding. The removed assertion encoded the old accidental absence of an ordinary Examine popup, rather than the intended invariant that unrelated materials do not receive repair guidance. Its replacement tests actual modal rendering and dismiss-to-inventory, correct PaleSalt description and blueprint, absence of well/oven advice, and zero MaterialExamined events. Existing one-Examine/drop/no-examine_material checks and the complete unchanged-state snapshot still execute. The Dagger, renamed Dagger, LampOil and supported material branches are untouched.

Wording/coverage bound: the changed PaleSalt assertion proves blueprint identity, not exact subject-reference identity. Existing DensityItemExamineInventoryTests checks exact Subject/SubjectViewer and IsCurrentSubject for ordinary seeds and GoldCoin. Root was advised either to add the same exact-reference assertion for PaleSalt if making that specific claim, or describe this pin as correct blueprint/prose. This is a small strengthening opportunity, not a runtime defect or reason to broaden implementation.

Root reported 57/57 focused native GREEN and prior regression 126/127 with this outdated pin as the sole failure. This review did not execute tests; updated native regression remains root-owned.


Inventory route cross-review: the independent Look reviewer checked the generic command, exact carried/equipped custody, pre/post callback provider identity, one structured snapshot and unchanged specialized routes; no actionable P1/P2 found. This is source review, not ordinary input acceptance.
