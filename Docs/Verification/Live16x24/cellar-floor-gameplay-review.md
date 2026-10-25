# Cellar floor independent gameplay review

Reviewed 2026-10-25. Seed 64 route evidence and seed 65 lower-passage evidence
are visually acceptable within the captures below. No floor-art adjustment is
recommended. This review used saved PNGs and JSON receipts; the reviewer did not
operate Unity or rerun native tests.

Reviewed actual 1280×800 captures and corresponding receipts:
`cellar-floor-64-entry`, `cellar-floor-64-young-bed`,
`cellar-floor-64-bed-actions`, `cellar-floor-64-harvested-bed`,
`cellar-floor-64-pallet-removed`, `cellar-floor-64-floor-examine`,
`cellar-floor-64-reloaded-store`, `cellar-floor-64-glade-return`, and
`cellar-floor-64-revisited`. Entry was compared with
`fresh-opening-cellar-entry.png` and its receipt.

## Visual findings

- Sparse seams are faint but visible at capture scale. They remain subordinate
  to actors, masonry, the crate, pallet and loose items. No bright grid,
  misleading item-like floor marks, enclosing tile borders or visible coverage
  defect was found. Useful walking space remains clear.
- Both cultivated beds remain distinct brown soil under crops, the player and
  dropped seeds. Soil persists after the ordinary seed harvest; it does not
  silently become stone. The action produced three sootroot seeds and no
  produce. This is not evidence of a ripe produce harvest.
- Ordinary pallet harvesting removes the pallet and yields two salvaged timber.
  The subsequent player position at 65,8 retains native floor underneath.
  Floor Examine identifies ordinary Terrain without a misleading interaction.
- Reload and revisit retain the same quiet material appearance. Fogged passages
  do not expose conspicuous seam detail. Surface return shows the glade's
  distinct existing ground presentation.

## Receipt checks

Entry and baseline both record player 40,12, tick 240, 40 HP and the same visible
object positions. Entry submits 119 native floor tiles: 83 blank frames and 36
marked frames. Its visible cultivated cell at 65,10 uses the existing soil
fallback. The baseline and new capture differ in surrounding darkness and wall
hue, so they are not an isolated lighting regression comparison.

All 120 shared entry/revisit floor records have identical owner IDs, texture
names, frame rectangles and soil flags. The post-pallet and reloaded-store
receipts have identical complete floor records, inventory, HP and tick. These
checks support material stability through the captured save/load and return
route without claiming that every scene pixel is unchanged.

## Seed 65 opposite branch

Independently reviewed `cellar-floor-65-entry.png` and its JSON receipt. The
second seed visibly uses the lower branch where seed 64 used the upper branch.
At player 40,12, tick 260, 40 HP, the receipt records 125 native floor tiles:
87 blank and 38 marked. The visible cultivated cell at 67,14 retains soil
fallback. Seams remain quiet and irregular across the changed geometry;
the actor, scrabbler, walls and stairs retain clear visual priority. No floor
coverage or readability defect was found.

Also reviewed `cellar-floor-65-lower-passage.png` and its receipt after ordinary
movement to 47,19, tick 400, 40 HP. The receipt records 39 native floor tiles,
31 blank and eight marked. The lower route stays readable beneath the player
and nearby beam; the foreground receipt at 48,19 identifies `FallenBeam`.
Seams neither suggest an obstacle nor compete with the foreground. The
remembered upper passage remains dark. This accepts the shown
opposite-branch entry and traversal composition, not a complete second-seed tour.

## Active idle observation

The nested results in `cellar-floor-active-idle-before.json` and
`cellar-floor-active-idle-after.json` span 29.076 seconds. Their environment
render frames, full/incremental passes, cells, writes, accumulated render ticks
and maximum ticks are identical. Both report 12 cached floor tiles and 11,712
atlas bytes. The capture operator confirmed the world was active with no pause
overlay and Unity unpaused. This supports no extra redraw or cached-resource
growth during that observed stationary interval; it is not an FPS or device
performance benchmark.

## Bounds

These captures do not establish controller comfort, physical Deck performance,
package delivery or whole-opening acceptance. The reported 23.27-second idle observation occurred
with the pause overlay open; it is not active-world idle or benchmark evidence.
Native test results are maintained by the implementation owner separately.
