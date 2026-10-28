# Sill environment — nine original native material cells

First source candidate, pending actual graded exterior/interior acceptance. This separate144×24 atlas contains Grass0–3, StoneFloor4–7 and intact StoneWall8. All cells are fully opaque16×24. No borrowed opening-cellar, cellar-floor or sill-envelope pixels are repainted; the five root-approved source/resource pins remain exact.

## Reference and parts

The author viewed the current exterior/interior before captures, the generated scene reference and root's critique. The actual interior has woven/VineWall boundaries: the concept's replacement masonry, invented approach paving, changed occupants and raised floor boxes are rejected. Adopted material direction is quiet matte turf, distinct flat worn warm-gray floor and internal layered masonry relief. No concept pixels are traced, sampled or downscaled.

Turf0 is plain muted344639; three minority variants use broad low-contrast flat mottles, without bright central tufts, dark rings or loose-object silhouettes. Floor4 is plain605E53; variants5–7 use shallow wear and sparse broken seams with shared flat boundary colors. Neither ground group has a tile bevel or raised edge. Wall8 has broad grey-green stone faces, lower planes, recessed mortar and short worn highlights inside the same straight cell. It does not imply an exposure mask, projected shadow, new wall or changed collision.

The joined source study uses the existing75% quiet selector and a fabricated simple stone room to inspect material joins. It is not the actual Sill room, which retains its genuine woven walls. Self-review watch: minority surface mottles and repeated single wall courses may remain visible under night grade. Actual normal-zoom turf/floor separation, room readability, door/actor/water precedence and repetition are root's acceptance gates. This sheet does not establish whole-settlement fullness or polish.

## Original editable source and verification

Tools/Live16x24/build_sill_environment.py narrowly reuses the established Grovelands native source workflow and shared Blender model/camera/instance helpers. It authors named integer unit-quads with emission palette materials, exports the native atlas, contact and joined study, and saves editable.blend. Pillow is used only for read-only pixel verification; no raster authoring or post-render pixel editing is performed.

source-red.json retains the missing-source failure before the first build, not a Unity/native-test claim. Source verification checks all3,456 native RGBA pixels, nine opaque frames, exact quiet0/4, common ground boundary colors and956,160 direct contact/study pixels. Four corruptions are rejected: quiet-turf stamp, transparent wall, changed floor border and wrong turf pixel. Sixteen approved-source/helper/reference/before pins are exact; no foreign metadata or whole-worktree guards are included.

Two fresh builds have identical native/contact/study/cells/palette bytes. Both saved blends reopen to3,456 exact integer unit-quads in24 named groups with the correct parts, colors and no animation. Binary blend identity is not claimed. The first build and its proof are retained once under verification/rebuild-first. The final identity receipt and frozen handoff bind current outputs.

## Reproduction

From repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_sill_environment.py -- --build
python3 Tools/Live16x24/build_sill_environment.py --verify
/Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Live16x24/sill-environment/editable.blend --threads 4 --python Tools/Live16x24/build_sill_environment.py -- --inspect-blend
```

Do not repeat --snapshot or replace the original input pins. Root owns resource import, runtime, Unity, real captures and publication. The source author did not edit Assets, approved shared art, runtime/tests or use Unity/Git. Art-source self-review is not independent visual acceptance.
