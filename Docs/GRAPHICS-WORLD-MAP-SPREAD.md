# World map Spread terrain pilot

The Spread now reads as one quiet region instead of isolated commas. The native adapter reuses approved sill-environment turf frames 0–3 and changes only presentation. Actual seed 65 gameplay resolved 142 cells, including terrain beneath disclosed location markers and the player.

## Rules and source

The adapter requires the current cached WorldMap, valid map arrays, a visible explored cell, matching world coordinates and exactly owned canonical Render, Physics and WorldMapCell parts. Changed render identity, duplicate map owners/parts, foreign graphs, live tile state and missing/malformed resources decline safely. Four cached views borrow the approved texture and own only their sprites/tiles. No new art, geometry, save data, RNG or gameplay behavior was added.

Ordinary Spread and undiscovered sinkholes use identical terrain. Discovered sites retain the existing exact glyph, color, label and reader portrait. The map floor remains beneath covered players; only the ordinary terrain comma is suppressed. Memory and legacy presentation remain unchanged.

## Iteration and evidence

Initial material improved regional shape but left black holes beneath POIs. Extending the same terrain beneath canonical markers removed holes but caused a visual P2: tan villages, red lairs and especially the green merchant symbol lost contrast. The final uniform 0.65 ground-only tint restores marker hierarchy without marker-specific boxes. Independent source review found no P1/P2; independent visual review accepted the corrected bounded view. Earlier candidates remain in the evidence directory.

Native checks: 23 genuine RED; 45/45 first GREEN; expanded resource/lifecycle tests 27/27 GREEN; POI revision 38 completed with zero reported failures, full receipt null; final tint 5/5 GREEN. These overlapping runs are not additive. The allocation probe completed without a reported failure but its full receipt was null; no allocation-budget certification is claimed.

Actual 1280×800 normal-zoom GameBootstrap comparison at map49,17, HP38, Dusk changes 53,748 world pixels with the right UI exactly unchanged. Walked to47,15, focused adjacent46,15 in Look (the Spread), descended into Overworld.17.12.0, returned, saved at turn3340 and freshly continued at47,15 HP38 Dark. Saved/reloaded world rectangle has 0/639,200 changed pixels; UI logs differ. Return/x in map Look did not open an Examine modal: actual modal portrait is not claimed; exact sprite/owner reader behavior is covered by native tests.

The 129.207-second Editor profile includes Continue, tool activity and travel: renderer p95 0.079875ms, max324.726ms; Main max18438.765ms. This unmatched instrumented profile does not close R11. A launcher attempt immediately after tests was refused while Unity restored its temporary test scene; the later clean SampleScene launch succeeded. One CUA timeout was resolved by observing the actual zone before issuing the missing ascend input. No F12 or injected movement/visibility.

## Acceptance limits and continuation

This is bounded Spread material acceptance, not whole-map polish, population, shadows or hardware acceptance. Other biome glyphs, map scale and overall cartography remain open; the green merchant marker remains the weakest accepted symbol. Current a69d Mac/Linux packages exclude this slice until the next delivery batch. Preserve the rejected Rim canopy experiment disabled. Next package the accepted map change, then choose the largest evidenced ordinary-world environmental gap.

Evidence and reproducible checkpoint: Docs/Verification/GraphicsExecution/WorldMapSpread. Normal saves, approved art, unrelated files and required builds are preserved; no storage cleanup.
