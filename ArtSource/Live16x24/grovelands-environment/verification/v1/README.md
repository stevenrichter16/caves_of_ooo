# Grovelands forest floor — first actual trial candidate

Four original opaque 16×24 cells in a new 64×24 grovelands-ground.png. This is a separate resource; no approved atlas was copied or repainted. Only the intended ordinary Grass presentation is addressed. Runtime ownership, optional inputs, water priority and import are root's separate work.

## Reference and native parts

The source author viewed the actual Grove and Fen before captures and the generated reference/parts critique. Adopted material direction: muted olive-brown forest soil, restrained grey-green moss and occasional warm leaf fragments. Rejected reference features: dense orange dots, continuous high-resolution mottling, altered sprites/HUD, invented roots or new vegetation. No imagegen pixels were traced, sampled or downscaled into this asset.

Frame0 is exact quiet soil 3A3C2E, with no motif. Frame1 is an asymmetric low moss/decomposed-soil field; frame2 contains sparse leaf and decayed-wood fragments; frame3 is a different soft soil/litter field. All borders use the common base and each frame retains at least 280 of 384 base pixels. Warm detail is confined to small areas of the detailed frames. These are flat surface colors, not collectible leaves, rocks, holes, raised objects or new terrain owners.

The contact presents all four cells. The joined study uses the existing coordinate selector (approximately 75% quiet frame0), not a new coarse patch grid. It is source-only, without the game's grade, actors, water or real map geometry. It does not prove whole-scene material quality or fullness.

**Open source-review concern:** root observed that the dark soil motif can read as a small rock/hole with three parallel highlight bars, and the moss has a bright center. The source author also identified isolated-shape risk. Root explicitly requested holding this first candidate for actual normal-zoom Grove/Fen comparison before another version. No aesthetic acceptance is claimed. Actual grading, flat-material reading, repetition, hazard/actor contrast and residual sparseness remain gates.

## Editable source and checks

Tools/Live16x24/build_grovelands_environment.py reuses existing named unit-quad model, camera, instance and PNG timing-canonicalization helpers. It writes only this new source folder. editable.blend contains original named pixel geometry and emission colors; no raster authoring or post-render image manipulation is used. Pillow only reads rendered pixels for verification.

source-red.json records the genuine missing generated source failure before the first build. source-verification.json checks all 1,536 native RGBA pixels, full opacity, plain frame0, common boundary colors, and 867,840 direct cell pixels in native/enlarged contacts and study. Four meaningful corruptions fail: stamped quiet base, transparent floor, changed border, wrong moss pixel. Eighteen relevant approved PNG/source/helper/reference/before pins remain unchanged; no foreign .meta files or whole-worktree snapshot is involved.

Two fresh builds produce byte-identical atlas, contacts, joined study, source cells and palette. Both saved blends reopen to 1,536 exact unit quads / 15 named mesh groups, exact integer coordinates/parts/palette, and no animation. Saved blend hashes differ, so binary .blend identity is not claimed. The first build and proof are under verification/rebuild-first; verification/rebuild-identity.json binds both exports. No old-atlas duplicate archive is created.

## Reproduce

Run from the repository root with Blender and the existing Python Pillow dependency:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_grovelands_environment.py -- --build
python3 Tools/Live16x24/build_grovelands_environment.py --verify
/Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Live16x24/grovelands-environment/editable.blend --threads 4 --python Tools/Live16x24/build_grovelands_environment.py -- --inspect-blend
```

Do not repeat --snapshot; it refuses to replace original input pins. Intended import: centered 16 PPU, Point/Clamp, no mipmaps/compression. frozen-handoff.json binds the candidate. This art author's source review is self-review; root/another reviewer owns independent source and actual judgments. No Unity, Git, runtime/test changes, gameplay or package execution was performed by this art task.
