# Clearing modular kit

Sixteen original props modeled as editable extruded voxel solids in Blender,
using `../concepts/modular-props-v2.png` as visual guidance. These are deliberately
simple companions for the approved Seed Keeper, cook, Tatterjay, grazer and jar.
The concept raster was not sampled, cropped or automatically converted to 3D.

## Native assets

Every gameplay PNG is exactly **16×24 RGBA**, except the striped seed shelter,
which is **48×48**: three 16-pixel columns by two 24-pixel rows. Import at 16 PPU,
bottom-center pivot, Point filtering, no mipmaps, no compression, no resizing,
full rectangle. Do not trim the transparent margins. Objects share one native
pixel of clearance below their baseline, including the shelter. Each asset uses
four to seven flat opaque colors and binary alpha.

`manifest.json` records dimensions, palette, footprint, occupied source cells,
SHA-256 and mesh statistics for each prop. `verification.json` independently
compares every actual PNG pixel against the authored voxel grid: zero mismatches
for all sixteen renders, including transparent cells and exact sRGB values.

`kit-overview.png` is a separate, enlarged Blender render of the editable meshes.
It includes labels and a dark green ground for inspection; do not import it as
gameplay art. Native PNGs contain neither labels nor ground.

## Editable source and rebuilding

`clearing-modular-kit.blend` stores the named components, shared palette materials
and front orthographic camera. Each prop has its own collection. Horizontal voxel
runs are merged into closed rectangular solids, grouped into meaningful named
parts. Their real depth is retained even though the shipping PNGs are flat.
The full kit contains 7,648 mesh vertices before overview ground/text.

From the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Clearing16x24/build_kit.py
```

The deterministic builder creates the meshes, renders each asset directly at its
native resolution, arranges the editable models into a shelf, renders the
overview and saves the source. It disables Blender backup versions so repeated
builds do not accumulate `.blend1` files. Wall-clock/date stamp metadata is also
disabled; two consecutive full builds produced byte-identical native PNGs.
Palette values are converted from sRGB
to linear shader values; Standard output, one sample, no denoising/dither and a
0.01-pixel reconstruction filter preserve the intended native colors and edges.

## Review and limits

The inspected overview shows distinct oven, well, canopy, table, rack, basket,
sack, masonry, both fence directions, bare tilled soil, two crops, grass, stool
and firewood. The canopy intentionally leaves its center beneath the roof empty.
The well is a low ring so it does not compete with the keeper silhouette. Crops
remain separate from bare soil so the scene can reuse either independently.

The first actual Unity clearing capture exposed a misleading wood-crate reading
in the original tilled bed. The reviewed revision removes the rectangular border,
reduces the ground geometry to shallow slabs and uses four dedicated darker,
muted earth colors. Three interrupted furrows and irregular clods replace the
continuous frame. Every other prop retains its exact original rendered RGBA.

Can verify: native source/render identity, opaque palette limits, binary alpha,
editable depth-bearing meshes, small asset footprint and a coherent visual kit.
Cannot verify from these source renders: Unity import, real object identity,
passability, harvesting, cooking, water access or player interaction. Those are
integration checks; no gameplay code, scene or blueprint was changed here.
