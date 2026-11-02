# Sodden native cartographic terrain

Sodden's63 authored map coordinates now reuse approved quiet wetland art instead of cyan wave glyphs on black. This is map presentation, not local water coverage or a traversal change.

## Contract

`FlatPixelWorldMapCatalog` borrows `sodden-flood-water`2048×24 and creates eight independent16×24views at frames0,16,…112. These are bank-mask zero variants; no neighbor state is read and no shoreline is invented. Coordinate hash selects the view without RNG. Existing Spread/Beating constructors, frames and tint remain unchanged. Sodden uses the existing.65quiet-ground tint; marker colors stay authored. Exact current graph/coordinates/owner/parts/appearance, visibility/exploration and empty tile state remain required. Hidden sinkholes match ordinary terrain. Plain-terrain reader shares the native ground; POI reader/foreground retains symbols. Owned views dispose, borrowed textures survive; missing/malformed resources decline independently. No save, geometry, memory, legacy or mechanics changes.

## Verification

RED:90completed, confirmed Sodden missing-ground failures, capped failure list/full result null. GREEN:87Passed and3Inconclusive allocation tests (positive control undetected),90scheduled; full receipt retained. No failures. Native controls cover exact identity, disclosed POIs, hidden sites, player coverage, incremental/full parity, altered state/graph/resources, disposal and bankless neighbor independence. Independent source and visual reviews found0 actionable P1/P2.

Actual GameBootstrap1280×800normal1× baseline from WorldMapBeating/AfterCheckpoint at47,16HP38Dark. Final comparison23,860changed world pixels/rightUI0. Walk47,9, Look48,9theSodden, descendOverworld.17.6.0at40,12, returnWorldMap47,9. F5turn3540, freshContinue47,9HP38Dark:world0/639200diff;21,663UI/logpixels differ. No F12/injected gameplay. Saved metadata confirms38HP; the small raster8 was initially misread as3 during preview and corrected before acceptance. Checkpoint50e670c30b6d48feb82756df64bd1cbc preserved.

After-only75.387s Editor profile includes tools/readers/travel:renderer p95.070083ms/max664.645750ms;Mainmax6376.015583ms;batchesmax44. Profile reports editor2321×1285 versus fixed1280×800captures. Unmatched baseline and transition/tool spikes do not establish performance improvement or R11 acceptance.

## Remaining work

Visual review accepts continuous dark-teal wetland distinct from Spread/Beating, with merchant/lair/village markers readable. Repetitive ripples, subdued village contrast and abrupt neighboring ASCII remain. Other mapterrain:Grovelands43/Stump22/Overwrit23; next batch these88cells through approved material reuse, then leave cartography for local environment/shadow gaps. No whole-map polish, localSodden, population, newshadow, Linux/Deck or R11 acceptance. Current5a5d packages exclude this and colonnadea828daf until next batch. Normal saves,77foreign modifications and priorbuilds preserved; no cleanup.
