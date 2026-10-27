# Original native Slate material source

Status: revised source accepted by root for one actual gameplay trial on
2026-10-27. This is not reference parity, completed material readability, whole
chunk fullness or area-polish acceptance. Root owns runtime, Unity import and
ordinary before/after gameplay. This source author changed no Assets files.

## Six cells and material meaning

The atlas is 96×24: six 16×24 cells, fully opaque, centered pivot, 16PPU, Point,
Clamp, no mipmaps and uncompressed. All outer pixels use the family's base
color. Straight rectangular wall silhouettes and existing collision remain
unchanged; there are no adjacency masks, transparent recesses or extra objects.

| Frames | Exact owner | Native source and essential state |
| --- | --- | --- |
|0–3|SlateFloor|Quiet original seam/scuff variants; passable actual floor. Frame0 has 5 accent pixels; each other frame has 6, of 384.|
|4–5|SlateWall|Two unequal internal cleavage planes with restrained lips and short recess cues. Surviving structural HP 1–38 / max 38 / hardness 3 has the same body; actual destruction removes it and produces the existing Rubble.|

Floor palette: base `#4E5260` (78,82,96), light `#565A68`, dark `#444856`.
Wall palette: mass `#536174`, shadow `#303A49`, seam `#445164`, plane `#65758A`,
highlight `#8190A1`. The five wall values shade original flat color regions;
they are not physical lights, raised geometry, cast shadows or normal maps.
Rubble, Stalagmite, stairs, actors, hazards and shared props keep their existing
sources and separate eligibility. Optional owner-free ambient backing uses the
floor base only through root's guarded runtime; it does not create a floor owner.

## Actual inputs, concept extraction and the one correction

The author directly viewed actual
`Docs/Verification/Live16x24/slate-materials-before-05-entry8.png` and
`slate-materials-before-06-wide8.png`, plus `scene-reference-01.png` and root's
exact `prompt-01.txt`. Root produced the latter with built-in `image_gen`;
this generated reference is not a gameplay capture or exported production art.
The invocation's original external tool identifier is not independently recorded
by this source author. The exact retained output and prompt bytes are pinned in
`protected-inputs.json`; no provenance identifier is invented.

Accept the cool material separation and internal layered wall shading. Reject
new walls painted on fog boundaries, changed geometry or visibility, the
concept's 1586px-wide scale/camera/UI drift, large floor cracks, noisy marks and
extrusion. No image is traced, cropped, rescaled, sampled or painted into the
native source. `SOURCE-DIRECTION.md` records the extraction before production.

The first rendered walls read as three detached rounded chips. Root and the
independent source reviewer confirmed this concrete defect. `draft-v1/` retains
that original source, Blender file, atlas, contacts, checks and logs unchanged.
`WALL-REVISION-01.md` records the bounded correction: two broader unequal planes
near opposite edges, shorter recess cues and a corrected SLATE contact heading.
All 1,536 floor pixels, eight colors and family border pixels remain unchanged.
The source guard verifies the exact historical inventory and floor cells.

The revised native and 8× contacts were inspected directly. The three-tier stack
is reduced, and the material is sufficient for a matched in-game trial. A
remaining watch is repeated paired light slabs in the adjacency study. Root must
judge normal-zoom repetition, wall/floor collision contrast and actual Cave
grading before further changes. Contacts are ungraded authored arrangements,
not cave geometry or gameplay screenshots. No global lighting fix is implied.

## Editable geometry and reproducible commands

`Tools/Live16x24/build_slate_caves.py` adapts the six-cell Limestone wrapper.
It reuses unchanged `build_opening_base.camera_for/emission_material/add_label`,
`build_cindercaller.model`, and `build_north_plot.instance/plane`. Blender 5.2.1 LTS
(hash 9e2066aef7ef) renders one original unit quad for each of 2,304 native pixels.
The final saved scene has 31 named material/part mesh clusters, all at z0; six
native instances have unit scale, no rotation and x 0,16,32,48,64,80.
`source-cells.json` retains every named part/color cell and `manifest.json`
retains exact frame rectangles, palette, ownership, contact placement and limits.

Run from the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_slate_caves.py -- --build
python3 Tools/Live16x24/build_slate_caves.py --verify
/Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Live16x24/slate-caves/slate-caves.blend --threads 4 --python Tools/Live16x24/build_slate_caves.py -- --inspect-blend
```

The nativePNG and both 1×/8× contacts render directly from the same geometry at
their output resolution. Pillow only decodes rendered bytes for verification.
Only volatile `cycles.*_time` PNG text chunks are removed, preserving all image
chunks without decoding/recompression. Two fresh builds produced identical
nativePNG, both contact PNGs and source-cell JSON. Saved `.blend` session bytes
differ; both saved files were reopened and every unit face, named part, actual
emission color and native instance checked. Binary Blend reproducibility is
explicitly not claimed. The Blender 6 `Material.use_nodes` deprecation warning
is retained in inspection logs; current 5.2.1 validation passes.

## Retained verification and handoff

- `source-red.txt`: absent atlas before first render, distinct from root's native
  runtime RED. `wall-revision-source-red.txt`: old source/render rejected after
  the named wall revision, before its rebuild.
- `source-verification.json` and `source-green.txt`: all 2,304 native RGBA pixels,
  all 748,800 directly rendered contact-cell pixels, opacity, borders, unique
  variants, quiet-floor threshold and preserved floor source checked.
- Ten corruption controls reject wrong canvas, transparent hole, partial alpha,
  altered floor edge, altered wall edge, changed original accent, swapped floor
  pixels, duplicate source variant, renamed source part and importer filter drift.
- `protected-inputs.json`: 26 exact input pins plus nine borrowed PNG-metadata
  records. Metadata normalization removes only trailing spaces/tabs before
  existing line endings and separately checks mip/filter/wrap/PPU/compression
  and GUID syntax. Borrowed files are never rewritten or automatically repinned.
- `blend-geometry-verification.json`, inspection logs,
  `verification-first-rebuild/`, `rebuild-before.json`, `reproducibility.json`
  and `rebuild.log` retain both fresh source validations.
- `frozen-handoff.json` binds the final source/export/readme receipts.
  `post-install-reconciliation.json` confirms that all18 original dependencies
  already matched: the final repeat verification and `source-green.txt` both
  recorded the installed runtime PNG. Root's copy preceded that final check;
  these are not pre-import receipts. Both existing files remain byte-identical
  and unchanged, and no missing final-v2 pre-import receipt is fabricated.

Final native atlas SHA256:
`e3fe86854fb139721f42058585bd5d000f266fde58f2b426451dff6ad1dfb1ff`.
Source author produced no Unity import metadata, runtime changes or gameplay
acceptance. Root's separate RED/GREEN, actual routes/readers/destruction/restart,
protection and package evidence remain required.

## Root's actual follow-up, separate from source validation

Root reports bounded material integration after native tests and ordinary
play. The retained actual views include
`Docs/Verification/Live16x24/slate-materials-after-01-wide8.png`,
`slate-materials-after-02-wall-reader8.png`,
`slate-materials-after-07-floor-reader8.png` and
`slate-materials-after-15-continue8.png`. These are root-owned gameplay evidence,
not additional source-author tests. Residual paired-slab repetition and weak
blue cast under the normal warm Cave grade remain separate polish work;
neither this reconciliation nor the source contact declares parity or area
polish complete. The post-install check read the runtime PNG and importer file
only and verified all finite protected inputs without changing original art.
