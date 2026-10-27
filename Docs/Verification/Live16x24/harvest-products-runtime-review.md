# Harvest-product runtime independent review

Reviewed 2026-10-27T08:07:35.467771+00:00. Read saved production sources and landed tests against the preimplementation/source review and existing public world/reader path. No Unity, native test execution, Git or active code edits by this reviewer. Current verdict: **bounded source pass, native submission acceptance pending the retained two-failure trial and its corrections.**

## Verified source behavior

- The exact17 BlueprintName set maps to the agreed8 new and9 reused frames. Common guards require Item, sole-owned visible Render/Physics/Stacker/Commerce, takeable/non-solid body, positive count within MaxStack and no explicit visual/glyph variant. Creature/Player/Terrain/Crop aliases refuse. No original product needs a fabricated Material/Thermal part or layer5 assertion.
- Food/Cookable, infusion mineral, Bone Handling, prepared-material recipe and reagent gates match actual inherited shapes. GroveRed requires both positive vital and toxic; Tepuibone uses its shape_head recipe rather than MineralPreparation. Named/mutable-price/quantity states remain the same visual identity. Recipe scanning matches exact comma tokens without per-query Split allocations. Reagent queries use the owner's invalidated cache. No harvesting, preparation, rendering event, ownership mutation or simulation command is called by the catalog.
- Sole-part enumeration rejects a second matching part or foreign ParentEntity. Optional footprints must be current, one cell and zero offset. Carried items can have no spatial zone. Owner status color override refuses neutral native pixels without firing Render events. This is semantic eligibility only; exact custody and disclosure remain with existing public documents.
- All four shared entry routes use the companion: legacy TryResolve, legacy semantic TryGetSubjectSprite, continuous TryResolvePresentation and current-graph TryGetPresentationSubjectSprite. Known17-role refusal is authoritative, so local ItemFrameOf/WorksProductFrameOf cannot resurrect weaker native art. Global routing stores productArt separately after optional regional queries. Other identities keep prior routes; IsAdmitted is unchanged.
- Public world routing first uses the current selected fallback owner and its current graph/cell visibility/custody checks. Public reader routing retains IsCurrentSubject and presentation graph checks before the semantic catalog. A legitimate covered exact subject may be examined without changing the selected painted owner. Direct raw semantic APIs intentionally do not grant inventory access.
- Default resource loading is lazy by exact product role. Three injected groups are independent, malformed dimensions fail locally, and legacy injected opening/works textures can be borrowed through a null-main companion without hidden Resources loading. Known-product missing-resource refusal also owns local routing. Tiles/sprites are cached; Dispose is idempotent and destroys generated frames only, preserving borrowed textures and independent peer catalogs. No new per-frame geometry, texture readback or blanket regional admission.

## Explicit old-contract change

`FlatPixelOpeningItemsTests.cs:107–113` previously expected native SalvagedTimber for a legitimate two-cell body in local legacy TryResolve. The plan's stricter one-cell product rule now deliberately sends that modified body to truthful fallback while retaining the real physical footprint. I flagged this before the broad run; root confirmed that policy and authorized a paired old-test correction plus documentation. This must remain explicit rather than weakening the new guard or silently calling the old expectation a production bug.

## Retained native evidence and unresolved acceptance

Read `harvest-products-native-red.xml`:123 total,0 passed,123 failed before production. Read `harvest-products-native-trial.xml`:399 total,397 passed,2 failed. These overlapping runs must not be added as distinct tests. Failures are the old multi-cell timber expectation above and `SubmittedFullThenDirtyDropAndPickupPaintAndRemoveActualProductWithoutFullRefresh` at its post-drop frame assertion (glyph texture instead of injected native texture). Root/test author are diagnosing the latter. A catalog source pass does not excuse a failed actual submission path; record its precise cause and rerun before claiming native GREEN.

Earlier fixture cold-reading found no compile/API blocker after public-footprint/recipient corrections, but did not predict this submission failure. Reader/public authority tests and semantic positives do not substitute for the missing submitted result. Final normal-game inventory/details/drop/pickup, cross-area transport, save/reload, packaging and complete item/biome coverage remain root-owned acceptance gates. No native performance or zero-allocation claim is made.

## Reviewed source bindings

```json
{
  "Assets/Scripts/Presentation/Rendering/FlatPixelHarvestProductsCatalog.cs": "da0b49f628eaf2ae481378b143c47caec814ab42fda130293586e01abb49fb22",
  "Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs": "fb2d0a36f67a755e57b21dc2087f27eb19b551dbe2c4f279c912042f0029e375",
  "Assets/Tests/EditMode/Presentation/Rendering/FlatPixelHarvestProductsTests.cs": "da62ca3743fd4670e2acd26d4b638d0ba7d8a1fbb7db6ca2816f8a2d72daaef1"
}
```


## Corrections independently cold-reviewed — 2026-10-27T08:09:04.189449+00:00

Read the saved two test corrections. The timber case now begins with a real native single-cell positive, expands through public TryChangeFootprint to two cells, verifies the actual physical count and owner at both occupied cells remain, requires exact fallback atlas/rectangle rather than native item art, then contracts back and proves native restoration. This documents the agreed presentation policy without changing body geometry to pass the test.

The submitted test retains actual inventory Drop/Pickup transactions and actor movement. After each real membership change it marks the exact affected cell through the public ZoneRenderer.MarkCellDirty API, checks no full redraw was requested, executes LateUpdate, and verifies actual overlay native painting/removal with unchanged FullPasses. Added pre-submission assertions distinguish correct semantic owner/art from the stale tilemap. The only helper addition is one explicit Dirty method; no production change, texture substitution or new framework was made to fix this fixture.

Source confirms these raw one-cell commands are not the UI repaint dispatcher. Normal Inventory close/InputHandler issues its own redraw request. Therefore the corrected case proves submitted incremental paint after proper dirty notification, **not** that raw commands or ordinary UI always generate incremental dirtiness automatically. Root must still demonstrate the ordinary drop/pickup controls in actual gameplay. Native rerun remains pending at this append; no GREEN is inferred from the source correction.
