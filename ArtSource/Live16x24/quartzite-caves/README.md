# Original Quartzite cave material source

**Frozen six-cell source candidate. Root reviewed the corrected native contacts and approved an actual integration trial. This is material coverage preparation, not gameplay, visual parity, whole-area fullness or polish acceptance. Assets remain untouched by this author and builder.**

## Source and review

The exact built-in imagegen prompt, unmodified generated scene and input/output hashes are retained in `scene-prompt-01.txt`, `scene-reference-01.png` and `scene-reference-01.json`. The actual input captures were Quartzite before07 depth10 entry and before06 depth9 down-stair view, both read before generation. `PARTS-AND-CONSTRAINTS.md` preserves the extraction, rejected drift and root's later authorization; its earlier pending text is historical. Only compact granular material, internal2D shading and quiet floor contrast were accepted. Cyan endcaps, invented fog-boundary walls, noisy floor pebble carpeting, relighting, boulders, UI/camera/geometry drift and output resampling were rejected.

All production cells are original named integer-plane geometry. Neither the builder nor its model helpers read, trace, crop, resize or sample concept pixels. The final atlas is 96×24: four exact16×24 QuartziteFloor cells0–3 and two QuartziteWall cells4–5. All 2,304 pixels are fully opaque, with straight, uniform material edges. The ungraded contact is directly rendered from the same geometry at 1× and 8×; it is not a scaled image or screenshot.

| Role | Palette | Essential meaning |
| --- | --- | --- |
| Floor base/light/dark |`#687478`, `#727E82`, `#5C686E` | Frozen RGB104/116/120 base plus 4,6,7,8 accent pixels respectively; more than 97.9% quiet base in every cell. No invented material/structural/resource/glow state. |
| Wall mass/shadow/seam/face/grain |`#79888B`, `#485A62`, `#647A80`, `#9AA9A8`, `#CCD4CA` | Four irregular internal faces and six sparse bright grains per variant, retained in a continuous opaque mass. No extrusion, detached boulder, paired Slate slabs, outside shadow or alpha/edge mask. |

QuartziteWall retains one body while its real structure survives at 1–55HP/max55/hardness5. Real destruction removes the owner and leaves the existing Rubble identity. Existing Rubble, Stalagmite and stair cells remain separate unchanged controls; Pillar, BrokenColumn, SteamVent and other materials are not aliases. Native16PPU, centered pivot, Point/Clamp/no-mip/uncompressed import is the expected downstream contract; root creates/imports metadata separately.

## Reproduce and inspect

Run from the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_quartzite_caves.py -- --build
python3 Tools/Live16x24/build_quartzite_caves.py --verify
/Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Live16x24/quartzite-caves/quartzite-caves.blend --threads 4 --python Tools/Live16x24/build_quartzite_caves.py -- --inspect-blend
```

The recorded tool is Blender 5.2.1 LTS, build 9e2066aef7ef. It uses existing `build_opening_base` camera/emission/text helpers, `build_cindercaller.model` for named unit-cell mesh groups and `build_north_plot` instancing. Every frame has one source cell per original 1×1 quad at z0; saved Blender verification checks named parts, exact integer coordinates, actual emission colors, no object transforms and six native instances at x0,16,…80. The `Material.use_nodes` future-deprecation notice remains in the logs; no clean-warning claim is made.

PNG canonicalization removes only volatile `cycles.*_time` text chunks and checks their CRCs; every image chunk is retained byte-for-byte. Pillow is decode-only in verification. No raster image drawing/resampling is used.

## Retained evidence

- `source-red.txt`: actual verifier failure for absent native atlas before the first build. This is source validation, separate from root's native runtime RED.
- `verification/first-render/`: first build, editable file, contacts, code snapshot and hashes retained before correcting the contact's stale SLATE label. The header correction changed no atlas or source-cell bytes.
- `source-green.txt` and `source-verification.json`: all 2,304 native pixels and 748,800 directly rendered contact pixels checked against authored cells; opaque edges, sparse floor and six unique variants checked. Ten corruption counters reject wrong dimensions, transparent/partial alpha, edge/color changes, swapped frames, duplicate source, renamed parts and changed importer filtering.
- `verification/rebuild-before/` plus `repeat-build-verification.json`: two fresh builds using the corrected script produce byte-identical native atlas,1× contact,8× contact and source-cells. Both saved Blender files are retained/reopened. Their binary hashes differ, so binary `.blend` determinism is not claimed; both recover exactly 2,304 unit faces and 30 named mesh groups with matching colors/instances.
- `protected-inputs.json`: 30 finite exact input hashes and 10 borrowed PNG-metadata pairs, including approved Slate art, editable source and builder. Metadata pins explicitly normalize trailing horizontal whitespace only and check importer fields; all ten raw metadata bytes also remained unchanged during this production. No broad all-repository preservation claim follows from this finite guard.

The final atlas SHA256 is `47b8a6984d02da33b000628e61daee9cd21a22fd5d4160293267a902204340eb`; source-cells is `84df5a646fdcd9c4cc53110090d2c4bdfc0f21819fe57ce7384fb3c806f1a68b`. `frozen-handoff.json` binds exact current files and status. Future root import checks may create a later verification receipt; preserve this pre-import source evidence rather than silently treating it as actual gameplay.

## Remaining gate

Root must inspect actual normal-zoom Quartzite 9/10 scenes under unchanged Cave grading, readers, collision/destruction, transition and restart. The contacts show a deliberately regular two-variant repeated study; it does not certify repetition quality in a full zone. Excessive wall repeat, relief contrast, warm grading, remaining glyph owners and whole-chunk composition stay explicit potential polish debt. This author has not run Unity, native tests, gameplay, builds, packaging or Git for this packet.
