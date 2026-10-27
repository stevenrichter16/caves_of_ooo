# Overwrit original native material kit

2026-10-27. Eight static cells for four existing owners, authored as original
integer-cell Blender geometry. Source render and decode checks are complete;
actual Unity import, native runtime tests and gameplay acceptance belong to
the parent integration pass. This packet does not claim an implemented or
accepted whole biome.

## Source decisions and essential states

| Cells | Owner | Authored identity and limits |
| --- | --- | --- |
| 0–3 | `OverwritGround` | Opaque smooth pale warm-grey. Cell 0 is completely uniform and is intended for the existing 75% base selection. Each rare cell changes only three of 384 pixels. No stones, tiled border, illumination, pebbles or crack grid. Actual cultivated source floors need their existing truthful soil/crop fallback. |
| 4–5 | `OverwritNewGrowth` | Three low stems share native top-left row 15 in both variants. Lower leaves and root paths differ. Flat clipped tips have no separate flower heads, fruit, seeds or harvest promise. Row 23 stays transparent. |
| 6 | `OverwritPilgrimBench` | A single-cell wooden side silhouette. Its back is on the right/east and the open sitting side faces west/inward, matching all five current Pilgrimage scopes. It has no resting/service symbol. One-pixel grain and two grounded feet keep it distinct from stone. |
| 7 | `OverwritWaymarker` | Short cool-grey stone with a blank face. No arrow, lettering, service or lore symbol. No naturally encountered marker was recorded in the current baseline; this frame has source coverage only. |

All four owners already exist. This packet changes no topology, collision,
health, materials, interactions, rendering authority, crop placement or
selection rules. Partially damaged extant structures do not need a new pose;
gone, changed or otherwise unsupported states remain the runtime adapter's
responsibility. No animation is added.

The muted ground is `#A7A292`; nearby scuffs are `#9E998A` and `#ACA797`.
Shoots use `#465343`, `#65745A`, `#99A084`; wood uses `#4A4033`,
`#75644B`, `#948064`; stone uses `#62665E`, `#83877B`, `#9CA091`.
The manifest records the same exact palette and all eight frame rectangles.

## References and extraction

The root-owned `references.json`, concept images, exact prompts and
`concepts/critique-and-parts.md` are unchanged. Actual baseline captures are
`Docs/Verification/Live16x24/overwrit-before-*`; the source preflight is
`Docs/Verification/Live16x24/overwrit-native-preflight.md`.

The author directly inspected actual bench approach 06 and the v2 concept,
having independently inspected06 / 07 / 08 in the earlier baseline review. The
v2 concept supplies the quiet pale field and compact young-growth hierarchy.
Rejected features include image gradients, repainted player/UI, enlarged
bench, changed placements and uneven flower-like shoot tips. Native source
uses no concept pixels or raster sampling. Every colored cell is an original
named integer quad. The approved player in the contact study is rebuilt from
its unchanged existing `base/source-cells.json` frame 0 for contrast only; it
is not part of the new atlas.

## Reproduction and editable outputs

From the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_overwrit_materials.py
python3 Tools/Live16x24/build_overwrit_materials.py --verify
```

The builder reuses the approved integer-quad model, orthographic camera,
emission-material and collection-instance helpers. It never reads a raster
image. The editable `.blend` contains the named part meshes, exact native
atlas camera and direct 1× / 8× contact scenes. `source-cells.json` records every
part and palette assignment in top-left coordinates. Contact images are
rendered directly from the geometry at each resolution; they are not resized
versions of the native PNG. The contact study uses exactly 36 blank tiles and
four of each rare variation. It is a source comparison, not a game capture or
a new map layout.

The final atlas is 128×24, eight 16×24 cells, binary alpha and uncompressed RGBA.
Ground cells are opaque; foreground cells have transparent margins. Import
settings are 16 PPU, centered pivot, Point/Clamp, no mipmaps or compression.
`overwrit-materials.png.meta` is a source import template with fresh GUID and
sprite ID. The builder itself writes only this packet; it never refreshes
Assets or drives Unity. After root approved the source contact for an integration trial, the native
PNG was copied byte-for-byte to its new Assets path. The parent owns actual
Assets metadata and Unity import; neither was touched by this author.

`source-red.txt` records the genuinely absent source packet before the first
render. `source-green.txt` and `verification.json` record decode-only checks:
exact dimensions, binary alpha through exact RGBA matching, all 3,072 pixels
matching their source cells, eight distinct cells, blank favored ground,
three same-height tips in both variants and transparent bottom margins.
`reproduction.json` records the bounded second-build byte comparison. Native
PNG, source cells, manifest and import template are identical. Direct contact
scene bytes differed; contact/vector-text and `.blend` byte determinism is
not claimed. The source atlas remains exact.
These checks are artifact validation, not Unity EditMode or gameplay tests.

`preserved-inputs.json` pins 294 existing native/source PNGs, source-cell files,
editable Blender files, generators and runtime PNGs. The generator checks
those byte hashes before and after authoring. It deliberately excludes
runtime `.meta` files that Unity may serialize under the parent's ownership.
No accepted pixels or source geometry were changed. Existing references and
concepts are separately pinned in the frozen handoff.

## Remaining acceptance gates

The root must inspect actual Green Rim, Blank, Pilgrimage and Dry Rim views,
bench approach/reader, ordinary movement, save and Continue. The first source
contact does not prove day/night lighting, whole-view fullness, a west-facing
bench read in the actual game, runtime exclusions or packaged delivery.
Deliberately sparse ordinary generation can still look unfinished; this
packet does not replace that whole-scene judgment with a frame count.
Stairs and observed Hollowchime, Margincress and Binderroot crops remain
separate coverage gaps. Marker gameplay visibility remains unobserved.

Independent source review also found no material blocker for a gameplay trial.
It named two actual-view checks: linked shoot bases might read cactus-like in
clusters, and the side-on bench might read as a chair at 1×. Keep those as
scene-review questions rather than silently claiming resolved art.
