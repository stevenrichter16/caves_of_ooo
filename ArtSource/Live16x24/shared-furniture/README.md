# Shared furniture: concept and unchanged native reuse

This packet preserves an imagegen scene study and the provenance of two
**existing** native body sprites. It is not a new art kit, an actual after
screenshot, runtime acceptance or area-polish approval. The implementation
contract remains [GRAPHICS-SHARED-FURNITURE.md](../../../Docs/GRAPHICS-SHARED-FURNITURE.md).

## Actual input and generated study

Root supplied the actual south-room before capture and the immutable
`east-kitchen.png` atlas to built-in imagegen. The exact root-owned prompt is
[prompt-01.txt](prompt-01.txt). The original generated output is preserved
byte-for-byte as [scene-reference-01.png](scene-reference-01.png); input/output
paths, dimensions, byte sizes and SHA256 values are in
[reference-manifest.json](reference-manifest.json).

The study broadly places distinct small Bed and Chair bodies at the two actual
room locations. That supports the existing identity direction only. It changes
the actual1280×800 image to1586×992, redraws parts of the UI, subtly changes
lighting/materials, and appears to enlarge the furniture bodies. Its contours,
colors and uneven pixel scale are not exact native pixels. Those changes are
rejected as production instructions. No generated pixel is copied into runtime.

## Extracted parts: two already-approved identities

| Real owner | Unchanged source rectangle | Interpretation |
| --- | --- | --- |
| Bed | East atlas frame2, `(32,0,16,24)` | One vertical cot body; no sleeping actor or claimed-use cue. |
| Chair | East atlas frame3, `(48,0,16,24)` | One chair body; no occupant, reservation or availability cue. |

Use centered FullRect cached views at16PPU on the existing16×24 grid. There is
no new PNG crop, atlas, recoloring, resizing, derivative or editable geometry.
Bed has184 opaque pixels/four colors; Chair has113/three. Both retain binary
alpha and a transparent final row. The two384-pixel rectangles were checked
read-only against the original editable `source-cells.json`: alpha and every
opaque color match. The full source/resource PNG bytes match as well.

Owned, reserved and occupied furniture keeps the same neutral body. Sleep,
Sit, Stand, movement, access and NPC reservations remain mechanics; sprite
selection must not query or mutate a service. In particular, no lasting bed
occupancy/sleep pose is inferred from the completed synchronous Rest action.
The currently selected actor remains the visible foreground where appropriate.

## Preservation and verification bounds

All existing files under `ArtSource/Live16x24/east-kitchen/`, its original
builder, runtime PNG and current metadata are hashed and left unchanged.
The exact .blend, source cells, original manifest and prior verification
records remain their original editable sources; this task did not reopen or
rerender the .blend or rerun its historical verifier. The recorded metadata
hash is a current-file pin, not a claim about Git HEAD. The actual screenshot,
generated source output and root prompt were also unchanged by preservation.

Only this new packet's reference copy, README and manifest were written.
No Unity/Git operation, runtime/test edit, native image change or shared-ledger
promotion is part of this task. Root owns integration, genuine RED/GREEN and
actual gameplay/service comparisons. Broad coverage and later separate
biome/area polish remain open.
