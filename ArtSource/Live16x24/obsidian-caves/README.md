# Original Obsidian cave source

**Frozen six-cell candidate, approved by root for an actual integration trial.** No gameplay, reference-parity, whole-area fullness or polish acceptance follows. This author changed only this new packet and `Tools/Live16x24/build_obsidian_caves.py`; Assets, runtime, tests, Unity and Git were untouched.

Exactly one built-in imagegen call used the two actual 11/12 captures. `scene-prompt-01.txt`, `scene-reference-01.png`, its JSON and `PARTS-AND-CONSTRAINTS.md` preserve the exact prompt, unedited output, input hashes, owner semantics and rejection boundaries. Root accepted dark glassy material/quiet floor only; pyramid/star bevels, invented fog borders, noisy texture, relighting and UI/geometry drift are rejected. No generated image pixels were read, sampled, traced or resized for production.

## Six original native cells

Atlas 96×24, frames 0–3 ObsidianFloor, frames 4–5 ObsidianWall. Each 16×24 cell is fully opaque with a uniform straight family edge. Floor uses base`#383242` (56/50/66), light`#403A4C` and dark`#2E2A38`, with 4/6/7/8 accent pixels respectively. Wall mass`#504A61`, shadow`#2C2B3C`, worn face`#403C52`, lit face`#666178` and pale edge`#9693AB` form three broad asymmetric fracture faces and six restrained edge pixels per variant. No alpha mask, outside shadow, extrusion, glow, resource or new Glass mechanic is introduced.

The surviving wall body covers real structure 1–70HP/MaxHP70/Hardness6; destruction leaves the unchanged Rubble identity. Floor remains Terrain without authored Material/Thermal/Destructible. Existing Rubble, Stalagmite and stairs remain independent borrowed views. Source band is all depth>10; actual 11/12 are samples, and runtime admission is root-owned.

## Reproduction and proof

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_obsidian_caves.py -- --build
python3 Tools/Live16x24/build_obsidian_caves.py --verify
/Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Live16x24/obsidian-caves/obsidian-caves.blend --threads 4 --python Tools/Live16x24/build_obsidian_caves.py -- --inspect-blend
```

Blender 5.2.1 LTS 9e2066aef7ef uses the unchanged opening-base camera/emission/label helpers, Cindercaller named-cell mesh builder and north-plot instancing. All 2,304 unit quads are original integer geometry at z0. Native and 1×/8× contacts render directly from the geometry. Pillow only decodes/checks pixels; PNG normalization removes volatile timing text without changing image chunks. Expected downstream import is centered16PPU, Point/Clamp, no mipmaps, uncompressed; root owns import metadata.

`source-red.txt` retains the pre-render missing-atlas failure. GREEN verifies 2,304 native and 748,800 contact pixels, family edges, palette and sparse ground; ten corruption counters reject dimensions, alpha/color/edge drift, swapped/duplicate/source-part frames and importer filtering. `protected-inputs.json` binds 34 finite exact inputs and 11 metadata pairs, including approved Quartzite and Slate resources/editables/builders. Metadata normalization is explicitly trailing horizontal whitespace only; all 11 raw metadata files also stayed unchanged. This finite guard is not an all-repository audit.

Two fresh builds yield identical native PNG, both contacts and source-cells. Both saved Blender files are retained and reopened, checking 2,304 unit faces, 22 named mesh groups, exact materials and six untransformed instances. Binary Blend hashes may differ from session metadata and are not claimed deterministic. See `verification/rebuild-before/`, `repeat-build-verification.json` and saved inspection logs. The future `Material.use_nodes` deprecation warning remains documented in logs. No `.blend1` was created.

Root viewed the native/enlarged contacts and approved the first candidate for actual play. Dark triangular internal faces and visible repeated diagonals are explicit actual-grade/readability watches. No optional art iteration was made. Whole-scene wall/floor/fog contrast, collision/destruction/readers, normal travel/restart and broader composition remain actual integration gates. This is broad material coverage, not acceptance of every deep area.
