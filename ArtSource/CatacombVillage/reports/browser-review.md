# Live browser review

Reviewed 2026-09-05 in the Codex in-app browser against the actual exported scene served from `http://127.0.0.1:8772/ArtSource/CatacombVillage/`. These observations supplement, rather than replace, the independent file/state tests.

| Check | Observed result |
| --- | --- |
| Actual asset load | 81 components loaded; browser reports exact intact reconstruction across 1,572,864 pixels. |
| Desktop fit | At 1280×720, fit canvas measures 687×458 and shows the complete chamber including the entrance traveler. |
| Reset | Returned to `[24,29]`,0 steps,0 removed. |
| Keyboard | Direct Up key moved to `[24,28]`,1 step. Focusing the Navigation checkbox then Left moved to `[23,28]`,2 steps. |
| Click route | Removed `larder-jar-large`; clicked its released `[10,25]` cell. Traveler reached `[10,25]` through intermediate cells. |
| Occupied restore | Individual Restore and Restore all both refused, visibly stating that the jar footprint was occupied;1 prop remained removed. |
| Reblocking | Direct Right moved to `[11,25]`. Restored jar successfully;0 removed. Left then left the traveler at `[11,25]` and did not increment steps. |
| Artwork selection | Clicking opaque central mushroom artwork selected Hearth crown. Inventory selection and a search for lantern also worked; search returned 4 matching components. |
| Isolate | Larder jar alone rendered over checkerboard, with its source-derived cutout also visible in the inspector. Exiting isolate restored the layered view. |
| Bulk removal | Remove all produced 46 removed components; architecture remained, with clean ground under the missing props. Restore all from a clear cell returned 0 removed. |
| Masks/backing | Live screenshot showed the fixed contribution silhouettes and mutable backing-support overlay with all props removed; disabling both returned normal artwork. |
| Comparison modes | Underlay displayed ground plus fixed architecture; Baseline displayed the intact export; Scene returned live layers. |
| Exploded | Initial post-interpolation build failed with `now is not defined`. Corrected build was reloaded and visually showed the separated sprite-card inventory with no visible error. Runtime regression added. |
| Native resolution | Actual size produced a 1536×1024 canvas in a 916×458 viewport with scroll extents 1536×1024. Fit mode restored normal dimensions. |
| Mobile | At 390×844, full scene was 360×240, inventory stacked below it and document scroll width stayed 390: no horizontal page overflow. |
| Error state after final correction | The visible error region remained hidden; no captured console warnings/errors during final reviewed operation. |

All six destination routes were additionally exercised against the actual manifest by the Node fixture tests; the live browser check above records the specific route and collision transaction performed manually through UI tools. There is no claim that every route was manually walked in the browser.

The final runtime keeps smooth position interpolation with one standing pose. Dedicated actual-renderer tests now execute normal, player, exploded, masks/backing/navigation, baseline, underlay and isolated draw paths and assert finite coordinates. They supplement the rendered reviews and address the gap that originally allowed the Exploded regression through state-only tests.

The temporary viewport override was cleared and the scene reset for delivery. The retained local browser tab is the interactive deliverable. Final independent aggregate evidence is `verification.json` (140 tests; eight gates passed). No Unity was used.
