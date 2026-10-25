# Incidental equipment — final source review addendum

2026-10-25. Reviewer: `/root/projection_cold_review`; second read-only footprint/custody review: `/root/projection_cold_review/look_focus_draft_review`.

**Verdict: no remaining actionable P1/P2 found in this bounded source review.** The reviewer authored the fixture and scratch footprint counter; root authored production and owns all Unity execution. No Unity, test execution, production edits or git mutations were performed for this review.

## Reviewed scope

- `Assets/Scripts/Presentation/Rendering/FlatPixelIncidentalEquipmentCatalog.cs`: exact LeatherArmor/Hatchet identity, owned functional/render/physics parts, explicit override refusals, positive stack, single-cell geometry, optional independent 16×24 frames, cache and disposal.
- `Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs`: the current diff only—default-lazy companion field/helper, admitted world/exact-subject hooks and disposal. Existing injected constructor chains and older scoped API are unchanged.
- `Assets/Tests/EditMode/Presentation/Rendering/FlatPixelIncidentalEquipmentTests.cs`: current 33 cases, including actual factory owners, exact reader, pickup/equip/drop, top-coin/lower-item distinction, fog/current graph/custody, malformed independent resources, borrowed lifetime and four footprint cases.

## Findings resolved and preserved contracts

1. **Corrected premise:** late `Entity.AddPart` of a single-cell footprint already refreshes spatial registration (`Entity.cs:47–60`); it was never a valid reproduction. The fixture explicitly preserves this as a positive control.
2. **Confirmed defect:** after real `TryChangeFootprint` registered a two-cell shape, directly changing public `CellsRaw` to `0,0` left the index stale. World selection refused that body but the exact reader restored its native equipment image. The two new native failures demonstrate this exact portrait mismatch, rather than a missing resource or generic missing API.
3. **Narrow correction:** `SingleCell` now checks `IsFootprintCurrent` only when the owner has a spatial zone (`FlatPixelIncidentalEquipmentCatalog.cs:66–76`). It reads existing registration; it does not repair it. Unplaced/carried/equipped equipment remains valid without a zone, and actual pickup clears its spatial registration. Canonical `TryChangeFootprint` repair restores the same owner, document and cached frame.
4. **Authority retained:** the world hook still runs after strict fallback owner selection, keeping current graph/cell, visibility, top-owner and custody checks. Exact-subject selection remains semantic and relies on existing authorized reader entry points. Equipment art remains restricted to admitted local regions; no global skin expansion, generation, gameplay or save change is introduced.
5. **Fallback/lifetime retained:** exact identities do not overlap older native opening-art selectors. Semantic or resource refusal therefore retains glyph presentation. Default loading occurs only for a known local role; old injected constructors remain resource-free. Both resource failures are independent. The companion owns two cached sprites/tiles, never the input textures, and disposal is idempotent.
6. **Neutral appearance retained:** condition, repair provenance, AV/DV, weapon values, names and ordinary tint do not select new condition art. Explicit visual overrides, wrong functional ownership, invalid stack and unsupported geometry refuse native art. No new world scan, observer, event dispatch, or per-query object construction was added; this is source inspection, not an allocation benchmark.

## Native evidence inspected; execution owned by root

- `incidental-items-red.xml`: 29 total, 0 passed, 29 failed, 0 skipped; 2026-10-25 12:39:49–51Z.
- `incidental-items-green-retry.xml`: 143 total, 143 passed, 0 failed/skipped; 12:41:56–12:42:02Z. This precedes the footprint correction and is not its GREEN proof.
- `incidental-items-footprint-red.xml`: 33 total, 31 passed, 2 failed, 0 skipped; 12:53:04–07Z. Both failures are `RawFootprintMutationCannotRestoreNativePortraitBeforeSpatialRegistrationIsRepaired` for the two exact identities, at the native-texture refusal assertion.
- Root's final 147-case run was still in progress when this addendum was written. No final GREEN is inferred here, and overlapping totals above are not summed as distinct tests.

## Bounds

The new guard rejects stale **native equipment art**; it does not redesign generic reader authorization or repair malformed spatial state. It permits the existing glyph fallback rather than requiring the whole reader to vanish. Tests intentionally create malformed raw state; normal `AddPart` and `TryChangeFootprint` paths are preserved. This review does not independently accept ordinary armor/hatchet pixels, save/reload gameplay, controller comfort, hardware performance, all equipment identities, or packaged builds. Root's actual captures and final native receipt remain the respective acceptance evidence.

## Root result receipt

The final native run completed at 2026-10-25 12:54:40 UTC: 147/147 passed, zero skipped. See `incidental-items-final-green.xml` and its summary. This subsequent result does not change the review's stated source-only scope.
