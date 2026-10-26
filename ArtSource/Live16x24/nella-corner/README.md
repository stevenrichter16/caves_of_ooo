# Nella's pipe and peat corner — editable native source

Status: source production and visual review complete; Unity tests, world
integration and actual gameplay acceptance are root-owned. The contact and
corner images here are direct Blender geometry renders, **not game screenshots**.

The actual before image is
`Docs/Verification/Live16x24/current-scene-49-nella-corner-before-2x.png`.
It shows five CopperPipe owners at (27,1)..(27,5) and three PeatBog owners at
(27,6), (27,7), (28,7). The built-in imagegen interpretation is preserved unchanged
as `concept.png`; `concept-prompt.txt` contains its exact prompt. Both images were
viewed before authoring. The concept's continuous copper run and quiet peat/water
composition inform this source; its gradients, lighting, glyph remnants and
implied discharge were rejected. No reference raster was sampled or edited into
the native asset.

## Extracted owners, topology and state

| Frames | Actual owner | Native visual contract |
| --- | --- | --- |
| 0 | Isolated CopperPipe | Exact editable source cells from the existing East pipe frame 15. |
| 1–15 | CopperPipe | Cardinal occupied-neighbor mask N=1, E=2, S=4, W=8. Four-pixel copper bands meet exactly at shared cell edges; absent directions terminate inside the cell. |
| 16 | PeatBog | One neutral low peat mat with two unequal, attached-fiber masses and 46 opaque pixels. Existing ground beneath it owns the live water state. |

The five-owner run is shown as masks 4,5,5,5,1 in the source corner study. Pipe
connections describe adjacency, not fluid transport. The source does not add
irrigation, discharge, power, charge, temperature or stock animation. CopperPipe
is a nonportable conductor; its real charge/heat effects retain existing runtime
overlays. PeatBog remains finite water, peat material and a real fuel/hazard
owner. This foreground does not depict its remaining volume, force a wet state,
or override frozen/mixed/dry ground rules. Runtime authority, disclosure and
neighbor eligibility remain separate from the atlas.

The source corner study reuses the original editable water frames 11,12,7 under
the three peat owners. Those study frames are not new runtime artwork and do not
establish live ground admission. No keeper, crop, soil, or previously approved
asset was changed.

## Native production and iteration

`Tools/Live16x24/build_nella_corner.py` builds named, editable integer-cell planar
meshes using the established Blender orthographic emission helpers. The native
atlas is 272×24, with seventeen 16×24 frames and binary alpha. Copper uses four
existing East colors (`8E5B36`, `B67A49`, `C99565`, `758972`); peat uses three
existing East colors (`342E25`, `5F4830`, `435A35`). There are no antialiased edges,
soft shadows, gradients, baked water pixels or extra plants.

`source-cells.json` retains each named part and palette cell; `editable.blend`
retains the source collections plus native, contact and corner-study scenes.
`contact-native.png` and `corner-native.png` permit 1× review. The 6× contact and
4× corner study are rendered directly from geometry at integer scale.

Pass 1 solved the continuous copper run, but its diagonal peat fiber linked small
marks into a twig-like silhouette. That named defect prompted pass 2: two broader
unequal low masses with short horizontal fibers, keeping the water gaps open.
The complete pass-1 source and contact evidence remains in `pass1/`. Final native
and enlarged review found a readable copper run with restrained couplings and a
quiet peat foreground; no further art change is proposed before actual Play.

## Verification and transfer

`source-red.txt` records the verifier failing before the source outputs existed.
`source-green.txt` / `verification.json` record the final direct Blender output:
17 frames, zero source/render or contact/render pixel mismatches, 128 matching
cardinal seam pairs, exact preservation of the original isolated East elbow,
binary alpha, importer settings, and unchanged hashes for the approved East and
ground source/runtime files. The verifier reads pixels; it does not author or
edit raster images. It also checks runtime bytes against the source copy.

Final native SHA-256:
`f25c11f1f5b91153d5455e283745f47adde7ed5206bf14cc2d966264bd6c9b68`.

The final pass-2 PNG and metadata were transferred before root's Unity test
freeze. No Assets writes are needed for the current source. Further source
rebuilds default to **source-only** so they cannot interrupt a native test/import
cycle. Coordinate any later transfer with root:

```
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Live16x24/build_nella_corner.py
# Only in an explicitly released Unity import window:
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Live16x24/build_nella_corner.py -- --copy-resource
python3 ArtSource/Live16x24/nella-corner/verify.py
```

The runtime target is `Assets/Resources/Sprites/Live16x24/nella-corner.png` with
16 PPU, centered pivot, Point filtering, Clamp wrapping, no mipmaps/compression.
The source verifier does not prove Unity import success, live topological routing,
selection, hazard semantics, performance or actual game readability. Root's real
scene capture and native tests provide those separate acceptance gates.
