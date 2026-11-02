# Marrowstye ground readability pilot

Status: bounded Editor verified; full settlement polish remains in progress. Source delivery pending this commit; current Mac/Linux packages are source56cfa2e72 and exclude this slice.

## Problem and scope

Marrowstye12.12 packed-earth yards and roads read as similar green planes. Earned baseline: Workshop/after-save, seed224401440, turn4040, player47,20 HP40. Main Camera post-processing is enabled. Approved town-floor base is RGB72,67,58; catalog and both painters submit white, but Spread's Jungle filter(.85,1.05,.90), saturation12 shifts the whole world green. A second actual reader at47,11 identifies the remaining green strip as packed road, not grass; its old opening-ground base is RGB49,65,45.

The bounded pilot uses a neutral filter and saturation0 for the current canonical Marrowstye Intake site in native flat mode; existing contrast5, vignette0 and .8-second transition remain. Its packed roads reuse approved Beating tiles32–33, base RGB104,100,84. Other settlements retain their existing grade and roads. No art bytes, geometry, placement, mechanics, save schema or population change. Layered walls and quieter floors remain straight and two dimensional.

## Implementation and acceptance

BiomeColorPatcher checks Spread, native mode, renderer/current graph identity, exact12.12 address and Marrowstye Intake POI authority. Same-biome travel, legacy/sprites-off, lost profile and replacement graphs restore the appropriate existing palette. Only the runtime Volume instance changes; shared source profiles remain intact. FlatPixelSettlementGroundCatalog shares the road selection across world, remembered terrain and reader portraits. Missing approved road resources retain fallback and suppress native-only wall shading.

Acceptance passed for this scope: neutral yard/road distinction in matched actual play; correct packed-road reader; unchanged readable actors, walls and UI; ordinary same-biome travel restores each grade; native save/fresh Continue preserves the scene; counters enforce authority/fallback; independent source and visual review found no remaining P1/P2 within this slice. No whole-area, packaged-performance or global grade acceptance is claimed.

## Reproducible evidence

All paths below are under Docs/Verification/GraphicsExecution/SettlementGrade. before.png and after-final.png share the original earned47,20 checkpoint. after.png is the intermediate grade-only result. reader.png shows actual floor identity; path-reader.png identifies the initially green packed road; road-reader-final.png shows the final matching neutral road portrait. after-junction-2x.png is the intermediate grade-only closer view, not the final road selection.

Actual UI travel12.12→12.13→12.12 restored Spread's(.85,1.05,.90)/saturation12 outside the site and white/saturation0 on return; travel-grade.json records read-only Volume queries. F5 saved turn4210 at39,11 HP40. AfterCheckpoint preserves that earned save. saved.png versus fresh-continue.png has0/639200 changed world pixels, crop(0,0,940,680). ui-comparison.json records zero sidebar/hotbar differences for the matched grade-only comparison. Final visual review also found no UI regression. Finish Live Greybox restored the normal root/preferences; Play stopped, save override null, scene clean.

Native RED/GREEN: initial grade6 genuine failures/12 passes; grade+adjacent76 passes; profile authority1 genuine failure/4 passes; final grade19 passes. Road scope4 genuine failures/19 passes. Final combined green-combined-final.xml is81/81 (19 grade +62 settlement), job4e1582db69eb41fcba1f60892fe401d5. Runs are non-additive. Test-source correction: internal shadow-receiver seam uses existing reflection pattern across assemblies rather than widening production visibility. Independent review found a missing exact Intake profile guard; saved regression failed before the production correction, then passed.

## Review, performance and remaining work

coverage_priorities independently reviewed source/counters, including the profile correction and final road live/memory/reader/resource/shadow paths: no remaining P1/P2. capture_route reviewed matched final images: neutral packed road separates from quieter yard, warm masonry and oven/player remain readable, existing fog/shadows/UI intact. An initial visual inference that the strip was grass was corrected by actual Examine. Reports are source/visual reviews, not independent replay of every action.

The110.301-second actual manual Editor profile after-profile.json was recorded for the grade-only iteration: renderer p95 .094ms/max69.212917ms; MainThread max1937.84475ms; GC max437539611 bytes. No execute_code calls occurred during that window. The final road selection reuses the same paint structure and unchanged art, but this is not a final-road or packaged profile and is not a causal baseline comparison. R11 remains open. The scoped per-frame authority check is not allocation-free.

Broad quiet yards, repeated masonry, remaining object glyphs, full-chunk/interior coverage and population review remain open. Existing wall shadows are retained, not expanded. Current source56cfa packages still omit this pilot; batch delivery after further accepted scene work. Next compare current ordinary Sodden16.5 peat/brine sources against already integrated flood water; do not count old wet-ground fallback screenshots as new gaps. Crops and optional animation stay deferred.
