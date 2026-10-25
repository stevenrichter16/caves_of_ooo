# Nella ground — actual-view independent review

2026-10-25 UTC. **The native material change is visibly present and is a restrained improvement. No demonstrated P1/P2 art blocker in these four actual after captures.** Prepared dry soil remains a small, quiet cue; this is bounded soil/state acceptance, not a complete scene, new layout, package or Deck approval. No Unity calls or production/image edits by this reviewer.

## Actual views and state

Inspected the untouched 1280×800 PNGs and their JSON receipts:

- `nella-ground-arrival`: Nella17/18,6 beds before permission, player18,9, tick2640,50drams. Both real reserve ground owners are cultivated; Pitchpod9187 is stage2 and dry. The accepted keeper/crops/cords remain the foreground grouping.
- `nella-ground-paid-beds`: player18,7, tick2670,42drams, claim Granted. The eight-dram payment is reflected in state and the log; the soil and neutral twine do not turn into permission-colored UI.
- `nella-ground-empty-dry`: player18,7, tick2710. Pitchpod9187 is gone and inventory contains two resin plus one seed. Cell18,6 still contains cultivated Grass7516 and CropRow7517. The readout correctly identifies prepared planting bed. There is no white target outline around the bed in this capture.
- `nella-ground-rain-replant`: player18,7, tick2760. New Pitchpod9193 occupies18,6; it and Marlroot9186 at17,6 have moisture39 and growthunits1. The actual log reports planting followed by Rain. The old row remains a real lower owner; wet native soil is not permission, standing water or a promised harvest.

The earlier `prepared-bed-restored-focus.png` is a valid visual baseline at the same plot coordinates, but has different actor/build, owner IDs, tick1570 and a white selection outline. It is not a pixel-identical simulation before/after pair; compare material/hierarchy rather than all changed pixels.

## Material readability and hierarchy

1. **Old field versus prepared dry:** the surrounding rows now carry sparse irregular soil flecks. The two reserve beds show the existing more ordered vertical furrow texture. The unselected empty18,6 bed has real furrow/ridge pixels, unlike its flat baseline fill; the treatment is not only the selection outline or the new text. At normal1× scale the difference is understated and reads when looking at the working row. It is not a prominent long-range destination marker, and should not be described as a major composition transformation.
2. **Wet state:** the paired reserve-bed fills darken clearly after Rain while neighboring ordinary old field at19,6 keeps its prior brown fill. The ordered pattern remains faintly visible and the neutral twine still outlines the physical beds. There is no observed blank/fog-looking hole or invented water sparkle. The public crop beds also darken through their existing generic moisture route; that alone must not be described as a new prepared/cultivated state.
3. **Bright strokes over soil:** the strongest repeated amber marks in both beds are mostly the accepted reserve twine. Read-only comparison at the exact32 opaque coordinates of north-plot frame7 finds25 identical amber screen pixels `(147,109,41)` in each reserve cell, unchanged dry→wet. The remaining cut-row stroke at18,6 is real foreground. Neither proves that native ground is missing. Erasing that owner or dimming the established cords solely to reveal texture would change the accepted hierarchy without necessity.
4. **Foreground and whole scene:** Nella, the player, tray/signs, crop state marks and cords remain readable. Plain green still supplies breathing room and the soil does not compete with the keeper. The broad repeated field fragments and small, loosely composed service grouping remain visually weak at scene scale. This slice has improved a genuine state cue, not solved those larger composition limits; no unrelated filler props are justified by these images.

A read-only sample of the actual screenshot cells supports the visual observation: dry reserve interiors contain base/furrow/ridge screen colors around `(69,47,19)`, `(51,38,19)` and `(81,60,30)`, while Rain interiors contain approximately `(34,30,16)`, `(24,24,14)` and `(46,40,20)`. These are captured display colors, not claims of exact atlas-byte output after the camera/color pipeline. No generated or altered image is used as gameplay evidence.

## Why the receipt reported null background tiles

`BgTilemap` is the correct actual background layer. The capture helper queried the wrong tile coordinates:

```csharp
// Existing /tmp/nella-ground-*.cs helper (incorrect):
new Vector3Int(b.x, -b.y, 0)

// Actual world-to-tile convention:
var at = new Vector3Int(b.x, CavesOfOoo.Core.Zone.Height - 1 - b.y, 0);
var tile = r.BgTilemap.GetTile<UnityEngine.Tilemaps.Tile>(at);
```

For bed17/18,6 the tile positions are `(17,18,0)` and `(18,18,0)`. `EnvironmentSpriteRenderer` computes that flipped position before `ResolveCell`, which calls `PaintBackgroundClaim` on its `_bgTilemap`. `ZoneRenderer.FlatPixelPaint.RenderFlatCellCore` writes the same passed tile position to the same background layer. The new regression fixture already uses this correct mapping. Existing `painted:null` JSON values are diagnostic-query mistakes, not proof of a missing live sprite. Preserve the old receipts and add a corrected read-only lookup rather than relabeling their null values as valid paint evidence.

## Acceptance bounds / next evidence

These views support the presence and relative readability of old/dry/wet materials with unchanged foregrounds and true crop state. The ordinary paid-access, harvest, replant and Rain sequence is documented by the receipts and log, but this reviewer did not replay it. Save/load, leave/return, wet exhaustion back to dry, and seed65/other opening-site checks are not established by these four after captures. No new atlas revision is required from the observed images; retain the accepted source and finish the remaining state/continuity gates.

## Image hashes

- `Docs/Verification/Live16x24/prepared-bed-restored-focus.png`: `0c692c6b56025296a62ef3e1e98bdbcfbd01d6cc91c7cf5d4255fa2e552d5b13`
- `Docs/Verification/Live16x24/nella-ground-arrival.png`: `b05cdbca53fc2da5fd4e18ee5d4ce0ac056d9b584f99c50c12a119b86593b6a5`
- `Docs/Verification/Live16x24/nella-ground-paid-beds.png`: `7e7864cb66121cc7371de914cf0d82feef7d7cf1fe0c49aa3e0c7f652a76d646`
- `Docs/Verification/Live16x24/nella-ground-empty-dry.png`: `0af036c8d9fa6779b79dc5e3beb0e8999c7a3a468920b2917c44543e63d87dea`
- `Docs/Verification/Live16x24/nella-ground-rain-replant.png`: `a70420d8b52a8d3da7df9eb555b2c154ccf1ecd22f5938c422f65227b48aa68a`
