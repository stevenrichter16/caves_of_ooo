# Isolated shale parity — native source candidate v5b

Status: frozen editable v5b sources and exact runtime copies are published for
root's controlled actual trial. Root approved the source preview after the new
underlay native RED; no visual parity, gameplay or whole-scene acceptance follows
from this source approval. Broad coverage remains paused under the user's current
isolated shale focus. No original art, normal saves or other regions are changed.

## Fixed atlas and current faceted-edge contract

`shale-parity.png` is1152x24,72native16x24cells: floors0..7 and wall cells
`8 + exposureMask*4 + variant`. Four wall fields use the same N1/E2/S4/W8 mask
meaning: only an actual disclosed eligible neighboring floor supplies an exposed
side. Mask0 and all eight floors are fully opaque. Other walls have binary-alpha
1–3pixel slow-stepped outer cuts only on exposed sides. Adjacent cuts take their
union; the central x3..12/y3..20 rectangle stays opaque. Every opaque fragment
joins that core; every transparent pixel has an unbroken ray to an eligible outer
edge. There are no interior holes or detached corner islands. A perpendicular
exposure may legitimately cut a nonexposed corner.

This alpha contract was explicitly authorized after the user requested more
faceted wall outlines with floor continuing beneath inward recesses. It supersedes
the earlier all-opaque restriction only for this isolated sibling source. Root's
runtime paints eligible candidate floor behind the actual wall as presentation
only: no LocalGround claim, new floor owner, changed collision or new disclosure.
Unsafe current state, competing meaning or missing resource/underlay retains the
original opaque fallback. Source art cannot establish those runtime safeguards.

`shale-parity-rubble.png` remains a separate16x16 optional body for the real
selected Rubble owner. It contains70opaque pixels in three unequal gray fractured
plates with flat/notched caps and shallow contact shade. Its v5a geometry/palette
is held unchanged in v5b. It creates no loot, new terrain or obstacle. The original
Environment/rubble.png and original semantic glyph/color remain untouched.

## Material changes and editable provenance

V4's varied floor silhouettes/positions are preserved exactly, with the same
63/68/74 base and the same15–46 nonbase pixels percell (average34.5/384=8.98%).
Only the local chip colors strengthen to87/92/98 face,105/110/116 edge and51/56/62
shallow shade. This addresses actual15's faint markings without extra density,
centered peaks or a global floor/lighting change.

Wall palette remains17/18/20 crevice,29/32/37 deep,39/43/51 mass,49/55/63 slab,
61/67/76 facet,74/81/89 edge and84/91/99 cut. All seven roles are now applied.
Four fields use long horizontal strata, many extending beyond native tile sides
before exact clipping, unequal interior facets and interrupted1–2row undercuts.
There are no standalone large recess pockets or enclosing masonry outlines.
Actual normal-zoom appearance remains the deciding gate. Source joined-wall means
are about45.86–46.88/50.60–51.72/58.05–59.36; the potential brightness increase,
repeated horizontal slots and scalloped-edge rhythm are explicit actual-view risks.

`source-cells.json` and `rubble-source-cells.json` are editable named part/color
cell data. Null wall cells are deliberate exposed-edge transparency. The saved
`shale-parity.blend` contains original unit-quad geometry, exact emission colors
and direct native/contact scenes. The16x16 Rubble uses the established24-high
helper coordinate system with a negative8 native-instance offset; no raster crop
or downsample occurs. No reference bitmap was traced, recolored or sampled into
production. Only volatile Cycles timing text is canonicalized in rendered PNGs;
pixels and other PNG chunks are not edited.

Contacts are source studies, never game screenshots. `material-study.png` shows
a synthetic material room with geometry-rendered floor under exposed wall cuts,
not the real map or runtime authority. `contact-native.png` and direct4x contact
show silhouette masks against charcoal; direct8x Rubble contact uses the floor
base. Root owns actual capture, controls, performance observations, tests and Git.

## Source verification and preserved iterations

Current checks cover27,648terrain pixels (including4,576transparent wall pixels),
256Rubble pixels and1,319,424placed contact pixels. All72cells are unique. Floors
match v4 silhouettes/base, the Rubble matches held v5a geometry, and all64wall
mask/variant topologies pass independent connected-core, boundary-ray and corner
checks. Seventeen negative controls include interior holes, wrong-side cuts,
mask0 transparency, disconnected corner pieces and enclosed edge pockets, as well
as canvas/alpha/source/palette/import errors.

Two fresh builds match eight deterministic source/cell/contact files exactly.
Both reopened saved blends pass geometry/emission checks; the final contains
23,142unit faces and2,464named clusters. Binary blend reproducibility is not claimed.
Published resources must match source bytes, and existing metadata/GUID bytes
were retained exactly. Point/Clamp/no-mipmap/no-NPOT/uncompressed16PPU metadata
is checked; root creates cached centered FullRect Sprite/Tile views.

All150original input pins (including49preexisting dirty files),14v1 pins,30v2
pins,45v3/original-Rubble pins,60v4/capture pins and60rejected v5a source pins remain
unchanged. Frozen prior candidates retain their original source/build/runtime
receipts, not merely contact images. No automatic repinning occurs.

The first v5b topology sweep caught four variant2 NW cases with a disconnected
corner sliver. A source build mistakenly continued after that preflight command
failed; its rejected outputs and exact failure are retained in
candidate-v5b-invalid-corners/. They never reached runtime. One north-profile
entry1->2 corrected the corner. All64 corrected topology cases then passed, and
iteration5b-source-red.txt records the corrected source refusing the prior output
before the successful rebuild. The workflow mistake is not presented as a valid
RED gate. See PARTS-AND-REFERENCE.md and iteration5b-corner-red.json for details.

## Current hashes and handoff

Terrain/source/runtime SHA256:
`a9af1d33a36ffc786beffcb295b801b18f958605151a6a3e13997b376a3a11e3`

Rubble/source/runtime SHA256:
`2f6b8dcc8c3eaeef7ebb9e7aa9f3b818e1c9574d8544a8f1e4f1822c784d5e17`

Original unchanged Rubble SHA256:
`dfb2097e05743e87e5c21aaa022e3bed9f5c4647383819a0c27027e7fff63172`

publication.json records the root-authorized copy after its new native gate.
source-verification.json, blend-geometry-verification.json, reproducibility.json,
source-metrics.json and frozen-handoff.json bind the final artifacts and limits.
Native test results and actual parity remain root-owned; this source task ran no
Unity or Git operation.

## Reproduction commands

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_shale_parity.py -- --build
python3 Tools/Live16x24/build_shale_parity.py --verify-source
/Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Live16x24/shale-parity/shale-parity.blend --threads 4 --python Tools/Live16x24/build_shale_parity.py -- --inspect-blend
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_shale_parity.py -- --publish
python3 Tools/Live16x24/build_shale_parity.py --verify
```

Do not rebuild accepted pixels merely to refresh receipts. Publish only in root's
authorized import window. Root and independent source reviewer support an actual
trial; neither accepts the scene from the synthetic study. Scalloped boundary
rhythm, repeated slots, brightness and Rubble angularity still require actual
normal-zoom review, alongside visibility, state, reader, traversal and restart.
