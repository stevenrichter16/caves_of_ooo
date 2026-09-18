# Catacomb source sprite package

Status: **51 extracted and visually reviewed assets, ready for offline scene assembly.** These are separate PNG sprites from the supplied Catacomb Village sheet. This directory contains source art only; scene mechanics and Unity integration are outside this extraction package.

## Rebuild and verification

From the repository root:

```sh
python3 ArtTools/catacomb_extract.py
python3 -m unittest ArtTools.test_catacomb_extract -v
```

`catalog.json` lists every sprite, its native dimensions, source crop, foot anchor, normalized pivot and SHA256. Sprite paths are relative to this directory. `source.png` is a byte-identical copy of the supplied sheet. `contact-sheet.png` shows all 51 exports over a checkerboard. `extraction-report.json` records the actual source, authoring and extractor hashes, zero changed visible RGB pixels, and zero source-pixel ownership overlaps.

The source is 1536×1024 RGBA. Its alpha ranges from 0 to 254: most illustrated background has very low alpha, while object interiors mostly have alpha near 250. The exporter uses **alpha ≥128 →255, lower alpha →0**, then applies the reviewed source exclusions. This preserves each visible pixel's RGB, normalizes opacity, keeps interior holes and removes the sheet haze. Transparent RGB is zeroed to prevent later texture filtering from leaking the sheet color.

No asset is resized. The 32 pixels-per-unit value is a proposed import convention, not a statement that these illustrated modules occupy one logical tile. Scene assembly can derive other sizes explicitly with nearest-neighbor sampling and its own provenance.

## Asset contract

Each entry in `catalog.json` has:

- `id`, `name`, `kind`, `path` and `spriteSha256`.
- `sourceBounds` / `bounds`: `[x,y,width,height]` in the source sheet's top-left pixel convention.
- `sourceRoi`: the authoring rectangle before trimming and anchor padding.
- `width`, `height`: exported native pixel dimensions.
- `foot`: local pixel anchor measured from the sprite's top-left corner.
- `pivot`: normalized bottom-left anchor; `[footX / width, 1 - footY / height]`.
- `visiblePixels`, `rgbChangedVisiblePixels`, `alphaThreshold`, `maskMethod` and exclusion count.

The foot is a visual placement anchor. It does not define a collider or a complete ground footprint. The gardener includes its connected coral and ground patch, and its anchor belongs to that complete vignette.

## Pre-implementation verification and corrections

| Initial assumption | Inspection result | Implemented correction |
|---|---|---|
| The sheet may need hand-built silhouettes from opaque RGB. | It already contains useful alpha, though no pixel reaches 255. | Preserve RGB and normalize authoritative alpha instead of inventing contours. |
| The lantern rack and lower shelf are independent sprites. | Their art overlaps: the lower jar occludes part of the upper rack's wood. | Export one complete `lantern-display` asset, plus the separate `lantern-hanging` asset. No hidden wood is invented. |
| Rectangular ROIs isolate all neighboring art. | A few ROIs include adjacent fungus, stair, bowl, jar, bed or spoon pixels. | Source-scale inspection and exclusion polygons remove those exact neighbors. |
| Pixel checks alone guarantee correct extraction. | They cannot distinguish a desired prop from a neighboring fragment. | Inspect the full contact sheet and selected enlarged crops; additionally reject shared source-pixel ownership. |

## Verification history

1. **RED:** 13 unit scenarios were written before the extractor existed; the initial run failed with the missing-module import error. The raw result is `reports/extraction-red.log`.
2. **GREEN:** the first exporter passed all 13 scenarios, including alpha holes, exact RGB, source immutability, anchor placement and malformed-input rejection.
3. **Adversarial RED:** three additional tests exposed accepted crossed/degenerate polygons, an unchecked declared canvas, and source ownership overlap. Four assertion failures (including two polygon subcases) are recorded in `reports/extraction-adversarial-red.log`.
4. **Final GREEN:** all **16 tests pass** in `reports/extraction-green.log`. The exporter also checks every exported PNG for both transparent and opaque pixels and compares every visible RGB pixel against the source. The 51 assets retain **628,959 uniquely owned source pixels**.
5. **Visual iteration:** all contact-sheet exports were checked. Neighbor fragments were corrected in fungus gardens, front stairs, the lantern group, the food tray, the purple bed, the dolmen gate, the gardener and the large honey jar. The final full contact sheet was reviewed after those corrections.

## Self-review and limits

- Fixed: rectangles alone captured neighboring source art; exclusions and a unique-ownership gate now cover the observed cases.
- Fixed: crossed and degenerate exclusion polygons were initially accepted; the exporter now uses the existing strict polygon validator.
- Fixed: stale extractor code was not initially represented in provenance; its SHA256 is now included in both catalog and report.
- Deliberate: source alpha is normalized, so this is exact **visible RGB** preservation rather than byte-identical preservation of the source RGBA data.
- Deliberate: exterior low-alpha glow and haze are discarded. Any new animated or emissive glow belongs to a separate scene effect layer.
- Deliberate: illustrated floor swatches have painted edges and perspective. They are not certified seamless autotiles.
- Deliberate: some source sprites contain attached plants, fixed furniture or ground. Those connected assets remain one visual component. Arbitrary sub-object animation would require another extraction and reconstruction pass.
- Not established here: runtime collisions, locomotion, dialogue, resource collection, damage, save persistence, depth sorting in Unity or unseen sides of these objects.

## Files changed for extraction

- `ArtTools/catacomb_extract.py`: deterministic extraction, catalog, contact sheet and pixel verification.
- `ArtTools/test_catacomb_extract.py`: 16 unit, malformed-input and source ownership checks.
- `ArtSource/CatacombVillage/source-authoring.json`: 51 asset ROIs, foot anchors and reviewed exclusions.
- `ArtSource/CatacombVillage/inspection/`: source inspection crops and intermediate visual review artifacts.
- This source package: byte-copied source, 51 PNG sprites, catalog, report, contact sheet and raw test logs.

This is a CoO-original art preparation task. It makes no Qud behavior-parity claim and modifies no Unity `Assets` files.
