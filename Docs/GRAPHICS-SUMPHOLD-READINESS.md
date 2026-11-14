# Sumphold visual review preparation

Status: source audit complete; three-view actual baseline recorded2026-11-14. Previous editor blocker resolved. No visual implementation or area acceptance.

**User direction — 2026-11-14:** Focus on graphics. Steam Deck-specific controls, resolution, Steam Input, physical hardware qualification and Deck delivery work are deferred until reactivated by the user; preserve completed work/builds. Hourly graphics task `overnight-clearing-art-pipeline` is ACTIVE, every hour, with this priority saved. Deferred Deck gates do not block graphics progress.

## Current actual baseline and next action

Evidence: `Docs/Verification/GraphicsExecution/SumpholdBaseline/2026-11-14/baseline.json`. Actual GameBootstrap, earned Sodden checkpoint, ordinary map travel to15.6, center39,11, occupied stone-floor room39,6, waterfront39,15. Three native-view1280x800 PNGs; a read-only Screen query differed from PNG dimensions, so no physical-display qualification. A15.7 route detour is excluded from Sumphold evidence. Time progresses Height toDusk: these views are not a matched palette comparison.

Center/interior were unprotected. Waterfront used F12 invincibility; a sari-snake died during ordinary movement and XP rose42→84. No combat/balance acceptance. F12 was disabled before F5, turn3130, HP19/40 at39,15; checkpoint retained. Fresh Continue remains unverified.

Independent visual review (`sumphold_baseline_review`) agrees that stone room floor, exterior floor and narrow waterfront detail have weak olive material separation. Gold brick shapes, occupied room, teal water and reeds are visible. No new atlas or population is justified. Shadows are secondary. Next inspect actual selected floor/board owners and loaded biome (matrix registration label is not runtime authority), freeze matched position/visibility/time, then compare canonical grade against the bounded candidate below. Preserve outside-area controls, fog, routes and genuine owners. Whole-chunk service/interior audit remains open.

Restoration: all79 pre-existing dirty files, normal save bytes and gameplay preferences preserved. One Unity Insights session event and four Unity telemetry preference keys changed as session bookkeeping; retained and disclosed in preservation.json. Isolated saves archived locally; editor stopped, original LastGameID restored, root null and clean SampleScene. No build/package change or storage reclamation.

The previous blocker section below is historical. CUA could bind Unity on2026-11-14, and localhost8080 MCP connected to the correct idle project; the stale remote connector404 was bypassed through the existing local MCP server. Local `mcp__unity` tools subsequently became available.

## Original baseline route (retained for reproduction)

Use the earned `Docs/Verification/GraphicsExecution/SoddenSources/AfterCheckpoint/Quick.{json,sav.gz}` at16.5 in the existing isolated LiveFlatPixelGreybox workflow, then ordinary map travel Left1, Down1 to Sumphold15.6. Capture center, waterfront and an occupied interior at1280×800 normal1×, with2× supplementary. Inspect selected owners before calling any family ASCII or missing. Review routes, water/board transitions, walls, doors and service cues before proposing population.

## Confirmed source and candidate rules

Independent `environment_gaps_audit` review, root spot-check at source8c4573ae5:

- `Assets/Scripts/Presentation/Rendering/FlatPixelSettlementGroundCatalog.cs` already admits the current cached Sumphold Village and excludes saved lairs. Floor uses town-floor, StoneFloor uses the Sill room floor, sandstone uses Beating wall art. WaterPuddle, Duckboard, PeatBank and Reeds already have adapters. A new art atlas is not justified without an observed defect.
- `BiomeColorPatcher.cs` has no Sumphold exception. Canonical Sodden grading is(.82,.86,.80), contrast6, saturation+4. Only if a matched scene shows muddy material separation, trial white/contrast6/saturation0 scoped to current flat15.6, exact Sumphold/Boatyard authority; preserve surrounding Sodden. Reuse Morrowfast Volume/transition tests but do not copy its biome or scene-runtime requirement.
- `EnvironmentSpriteRenderer.FlatShadow.cs` has no Sumphold sandstone admission. If the baseline warrants it, reuse existing2px south/16% shade with disclosed canonical dry receivers. Reject water, boards, entrances, occupied or competing cells. The shared receiver's `intakeRoad: !gantry` deliberately uses Beating road resources for other sites; do not copy it into Sumphold. Exclude unobserved roads or use their actual material route.
- Existing `BiomeColorPatcherMorrowfastGradeTests` and `FlatPixelCinderholdShadowTests` supply relevant admission, transition, disclosure, resource-loss and full/incremental controls. Scope tests to the selected change after baseline; no redundant suite run now.

Population and shadows remain separate decisions. Existing Sumphold is substantial; improve missing/readability presentation before adding owners. No geometry, art, runtime source, normal saves or builds changed during this preparation.

## Historical tool blocker and recovery (resolved2026-11-14)

Root launched Unity PID44659 after confirming no editor/player. The process remains present but cannot be observed through CUA: repeated native binding timed out, then `cgWindowNotFound`, including after a CUA reset. A one-second process sample shows `EditorDialog::DisplayComplexDecisionDialogNative` → `NSAlert runModal`; dialog contents are unknown and no blind acceptance/key was sent. No Play session was started.

The local MCP HTTP endpoint initially refused connections. Root started the existing installed server on127.0.0.1:8080 (PID46263, exec session97386). The existing connector retained an invalid session; a fresh read-only SDK session initialized successfully but returned zero Unity instances and `no_unity_session`. This establishes a disconnected editor, not a game failure. Do not repeatedly rerun these probes without a changed window/connection state.

Recovery: expose the Unity window/dialog, inspect its actual contents, resolve only within authorization, connect MCP and verify correct project/idle/clean scene before launching the isolated baseline. Do not infer a licence/terms choice or force-dismiss an unseen dialog. Current root editor/server remain open. Sample is at `/tmp/coo-unity-44659-sample.txt`; no useful data/cache deletion.

## Preservation and reporting

The77 pre-existing dirty tracked paths remain excluded. Unity additionally serialized trailing spaces in frost-vent.png.meta and mirror-mucilage.png.meta; reviewed diff is whitespace-only, retained unstaged. No other source changes are claimed. Current published Deck source16afcc497 and delivery8c4573ae5 remain valid; Linux/physical Deck execution and R11 remain open. Next useful action is actual Sumphold baseline, not an assumed material patch.
