# Liquid glyph color preservation

Status: complete within this bounded legacy Tilemap rendering slice. Actual Unity EditMode verification passed **77/77**, with no failures or skips (`native77-green.xml.gz`, job `9f8e6eb97ddc403883e46ba3dd87b360`, 2.3414901 seconds). No additional production change or repeated sweep is required for this slice.

## Problem and verified scope

Acid, oil and other liquids share the `~` glyph with water. The ambient renderer admitted that glyph without checking liquid identity, then replaced the correct authored/registry color with water's blue/cyan shimmer or flowing-water layers. A cached reflection could also keep painting after its source changed. The ordinary source-cell dirty path does not repaint the neighboring bank: simply refusing further reflection writes left an old blue tint visible.

The repair is CoO-original rendering ownership work, not a Qud parity claim. `LiquidPoolPart.Initialize` already supplies the correct liquid glyph/color; the ambient painter now honors it. Actual adopted native 3D liquid meshes, colors, registrations, liquid quantities, gameplay and saves are unchanged.

## Pre-implementation sweep corrections

| Assumption | Verified correction and consequence |
|---|---|
| `~` means water | Acid, oil, other registered liquids and unrelated vapor can use it. Admission requires an actual visible RenderPart and nonempty LiquidPoolPart with correct owner backlinks and exact `water` ID. |
| Cache admission alone is sufficient | A live source can change identity or be covered/removed without rebuilding the cache. The same semantic check must run during replay. |
| A dropped old cache row cannot leave art | Fine-water overlays can outlive a removed row; clear invalid old fine tiles before rebuilding the cache. |
| Preventing invalid reflection writes restores the flank | Source-only dirty rendering leaves an already-painted flank untouched. Record whether the row painted, then dirty precisely that flank once so the ordinary baseline painter restores its current authored background/fog. |
| A color comparison proves the visible native scene | Native Tilemap state proves the actual renderer writes and cleanup. It is not a GPU capture, visual judgment or whole-gameplay acceptance claim. |

## Implementation

`ZoneRenderer.IsAmbientWater` is shared by cache admission and replay. It requires actual current top-owner Render/LiquidPool parts, valid backlinks, visible `~`, exact water and positive volume. Other owners retain their base painter. Cached reflections retain the source identity and coordinates and require the same current visible flowing water source.

An existing reflection row records a transient `Painted` flag. Invalidating a row that painted queues `MarkCellDirty` for its exact flank once, clears that flag and stops repainting. Valid water renews the flag on a real write. The normal next dirty pass owns restoration; the ambient loop does not erase arbitrary competing backgrounds. This adds no full-zone scan, persistent cache, material write or serialized field. Existing row/cache and hot-loop behavior remains bounded.

## Executed evidence and counter-checks

| Gate | Actual result |
|---|---|
| Original semantic renderer fixture | 21 native cases: 15 RED, 6 controls PASS before the first production repair; `native-red.xml.gz`. |
| First repair and neighboring checks | Liquid cases passed in `Integration/native-liquid91-green-corpse2-red.xml.gz`; mixed selection was 91 PASS plus 2 deliberate unrelated missing corpse-launcher API failures. This was not final stale-flank acceptance. |
| Supplemental actual stale-flank cases | 29 native cases: 23 PASS, 6 intended RED before cleanup; `native-supplement-red.xml.gz`. Removed, empty and acid-switched source each paired with empty/authored-red flank, plus two unchanged-flow controls. |
| Final repair | 77/77 native PASS: LiquidAmbientColor 29, WaterShimmer 7, ZoneRendererStaleBg 3, DensityPouredLiquidRendering 26 and TerrainRenderCoverage 12. Exact XML is authority. |

The fixture calls the real `RenderCellCore`, `RefreshWaterCache`, `UpdateAmbientAnimations` and dirty repaint on owned native Tilemaps. It covers real natural/poured water, factory acid/oil and registry liquids, both flow directions, stationary water, water→acid changes with/without cache rebuild, fog/covering owners, unknown/empty liquid and false backlinks. Supplemental controls first prove that the actual blue reflection was painted, then verify its exact baseline tile and color return through source-only dirty rendering. Registry/content setup and owned Unity objects are restored. No synthetic shadow or duplicate implementation stands in for these rendering methods.

## In-phase and cold-eye review

- **Q1 — symmetry:** read cache construction, replay, fine-layer invalidation and reflection retirement together. Both admission points share semantic water ownership; valid writes renew the row and invalidation retires it once. Full redraw already clears backgrounds before cache reset.
- **Q2 — consistency:** foreground, fine-water layer and flank reflections use the same current source identity. Base authored/registry color and the current background/fog painter retain authority; native 3D is untouched.
- **Q3 — counters/adversarial cases:** nonwater/unknown/empty/stale backlink, identity swap, covering/fog, detached source and source-only dirty cleanup are paired with actual water and retained-flow positives. The peer's previously-painted flank finding was reproduced as six native REDs and fixed before completion. Native neighboring shimmer, stale background, terrain and 3D liquid assertions all pass.
- **Q4 — documentation:** final status/counts were checked against actual native XML and the current source. Historical `PLAN.md`, first test snapshot and publication receipts remain evidence of the earlier cadence; they are not the final implementation specification.
- **🟡 resolved:** semantic tilde misclassification and stale reflected output both have executed native RED→GREEN evidence. Independent peer review of the cleanup found no remaining concrete blocker.
- **🧪 bound:** no new full gameplay/GPU screenshot is claimed for the legacy fallback. The tests prove native Tilemap writes, cleanup, exact selected 3D preservation and fixture restoration, not overall visual feel or exhaustive renderer correctness.

## Files and evidence

Production: `Assets/Scripts/Presentation/Rendering/ZoneRenderer.cs` only. New fixture: `Assets/Tests/EditMode/Presentation/Rendering/LiquidAmbientColorTests.cs` plus fresh `.meta`. This living document and the exact receipts listed in `Docs/Verification/DensityCompletion/LiquidGlyphColor/owned-current-manifest.json` belong to the separate liquid checkpoint. No files are staged by this audit.
