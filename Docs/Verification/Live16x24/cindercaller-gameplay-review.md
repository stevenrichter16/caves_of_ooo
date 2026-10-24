# Independent Cindercaller gameplay-image review

2026-10-24. Initial bounded review of `cindercaller-native-world.png` and `cindercaller-native-reader.png` with their paired read-only observations, compared with the actual baseline and native family contact. No Unity interaction, runtime edits, tests, staging, push or deletion was performed by this reviewer. Movement and save/reload evidence was added below after root’s separate ordinary live run.

**Final bounded decision: accept the demonstrated static Cindercaller world/reader integration, ordinary relocation and save/reload presentation. No actionable P2-or-higher visual defect found in the five reviewed captures.** This is not whole-release, casting or animation acceptance.

## World and reader

Both actual PNG files independently measure **1280×800**. The baseline still has a red `g` at (78,7). The new world screenshot replaces it with the small, narrow-capped stone/rust/tawny body in that same location, retaining the surrounding real terrain, actors, HUD and field of view. It is modest in size like the other native actors; the screenshot does not use the concept’s oversized placement. At this ordinary view, outline and lighter legs carry recognition. The tiny face/seam is not a legible standalone detail at that scale, which matches the source design’s stated hierarchy.

The selected world cell has a readable red outline in the reader screenshot; the focus panel identifies “marlback cindercaller,” HP12/12, hostile, at (78,7). The shared reader shows the matching enlarged native silhouette with its narrow crown, scorched side and separated limbs. The reader portrait is larger for inspection; it is not evidence that the world actor is rendered at that portrait size. Existing prose identifies a ranged fire-worker and its relationship to the Scrabbler without changing the static body into a permanently firing or equipped pose.

Paired observations agree exactly on seed65, zone `Overworld.12.10.0`, tick5250, player(55,23), playerHP40, flat mode and the complete recorded actor list. Both identify current visible/top Cindercaller **4983** at(78,7), HP12, with submitted texture **`cindercaller`** and full-frame rect **(0,0,16,24)**. It currently carries FireMoss4985×1, but the art contains no fixed moss/weapon/flame: the body does not promise that mutable stock will always remain. The reader observation does not advance the tick or alter the recorded actors/inventories.

Player and Scrabbler have null overlay texture in these receipts because their motion bodies use a separate renderer, as the observer explicitly documents. This is not evidence that their actual visible bodies are absent.

## Scope still open

The initial pair establishes current real-world placement, selection, actual reader content and matching portrait. It does not yet establish movement retirement/repaint, save/reload persistence, death/corpse handling, consumed-fire-supply appearance, animations, facing in motion, combat readability, ordinary packaged startup, physical Steam Deck input/performance or every hidden/stale/foreign owner countercase. Native source integrity and unchanged approved-art hashes are documented separately in `cindercaller-source-review.md`; this review does not replace runtime tests with a contact sheet.

No new-art movement/reload outcome is asserted until its actual captures/receipts are reviewed. No asset extension or unrelated style change is requested by this initial visual review.

## Follow-up: ordinary movement and save/reload

Reviewed `cindercaller-native-moved.png`, `cindercaller-native-saved.png`, `cindercaller-native-reloaded.png`, all three paired observations, and `cindercaller-save-comparison.json`. These three additional actual PNGs are each **1280×800**.

Root’s recorded ordinary held-Right input advances tick5250→5310 and moves the player (55,23)→(61,23). The read-only observations independently show Cindercaller4983 naturally changing (78,7)→(78,5), retaining HP12, its native `cindercaller` full-frame tile and one FireMoss4985. The moved screenshot visibly relocates the same small body upward, with no duplicate body remaining at its old location. This is a static sprite relocating through the real simulation; it does not claim a newly authored walk cycle.

Scrabbler4986 approaches (62,23); player HP drops40→29. The actual world log records its 11-damage cudgel hit. The run therefore includes ordinary hostile consequences, not a posed scene with enemies disabled. No Cindercaller projectile, spent supply, death or direct melee attack is demonstrated by these frames.

The saved and reloaded observations match in **every recorded field except timestamp and label**: seed65, zone, tick5310, player position/HP, flat mode, all six complete actor rows and all18 nested inventory rows. This independently confirms the narrower equality claimed by `cindercaller-save-comparison.json`. The actual saved/reloaded world viewport (pixels x0–939, y0–675) is also **pixel-identical** to the moved capture: zero differing pixels in either comparison. Thus the displayed current actor, its new cell, nearby objects and visible terrain remain intact after F5/F6; the log changes outside that viewport show saving/loading. This is not a complete serialization or every-world-owner audit.

## Native tests and profile bounds

Independent XML name-set comparison confirms **334 distinct passes**, no overlap between focused45 and regression289, no failures or skips in those final two runs. The first two requirements were demonstrated RED; the additional43 focused countercases are GREEN coverage, not separately demonstrated RED. This reviewer read saved results and did not run tests.

`cindercaller-profile.json` spans250.9343seconds and205,844frames, but every recorder stream wrapped at its131,072-sample capacity. Its statistics describe the retained tail, **not the full run**. Retained `COO.ZoneRenderer.LateUpdate` mean0.056639ms, p990.112250ms, max41.667250ms; retained Main Thread max1041.511833ms. The sample includes Editor, UI and capture/tool work and has no matched prior-build baseline. It cannot establish a speedup, absence of hitches, packaged performance or physical Deck performance.

The initial “pending” movement/reload limits above are now resolved only to the extent demonstrated here. Still unaccepted by this slice: Cindercaller action animation/casting, arbitrary inventory overlays, lethal/corpse presentation, all hidden/stale-owner paths in actual Play, other seeds/regions, packaged launch, Deck controls and complete graphics-release coverage. No further asset change is requested by this bounded review.
