# Grovelands forest floor — v2 actual trial candidate

Four original opaque 16×24 cells in a new 64×24 grovelands-ground.png. This is a separate resource; no approved atlas was copied or repainted. Only the intended ordinary Grass presentation is addressed. Runtime ownership, optional inputs, water priority and import are root's separate work.

## Reference and native parts

The source author viewed the actual Grove and Fen before captures and the generated reference/parts critique. Adopted material direction: muted olive-brown forest soil, restrained grey-green moss and occasional warm leaf fragments. Rejected reference features: dense orange dots, continuous high-resolution mottling, altered sprites/HUD, invented roots or new vegetation. No imagegen pixels were traced, sampled or downscaled into this asset.

Frame0 is exact quiet soil 3A3C2E, with no motif. Frame1 is a broad asymmetric moss surface field using only the subdued moss-soft color, without a raised highlight center; frame2 contains sparse leaf and decayed-wood fragments; frame3 is a broad low-contrast soil-wear field without a dark core or outlined edge. All borders use the common base and each frame retains at least 280 of 384 base pixels. Warm detail is confined to small areas of the detailed frames. These are flat surface colors, not collectible leaves, rocks, holes, raised objects or new terrain owners.

The contact presents all four cells. The joined study uses the existing coordinate selector (approximately 75% quiet frame0), not a new coarse patch grid. It is source-only, without the game's grade, actors, water or real map geometry. It does not prove whole-scene material quality or fullness.

**Actual v1 rejection and bounded v2 correction:** grovelands-environment-v1-01-grove.png confirmed the small rock/footprint reading: the soil had a dark rim and parallel highlight bars, while moss had a bright center. V2 removes those cues and broadens the low-contrast surface wear. Exactly 56 pixels in frame1 and 77 in frame3 change; quiet frame0, sparse warm-leaf frame2, all palette values and all boundary pixels remain exact. The v1 editable, builder, atlas/cells, contacts and evidence are retained under verification/v1; the original first-build packet remains at verification/rebuild-first rather than being duplicated. Actual graded Grove/Fen comparison is still root’s gate. The source contact does not establish full-area quality, repetition acceptance or fullness.

## Editable source and checks

Tools/Live16x24/build_grovelands_environment.py reuses existing named unit-quad model, camera, instance and PNG timing-canonicalization helpers. It writes only this new source folder. editable.blend contains original named pixel geometry and emission colors; no raster authoring or post-render image manipulation is used. Pillow only reads rendered pixels for verification.

source-red.json records the genuine missing generated source failure before the first build. source-verification.json checks all 1,536 native RGBA pixels, full opacity, plain frame0, common boundary colors, and 867,840 direct cell pixels in native/enlarged contacts and study. Four meaningful corruptions fail: stamped quiet base, transparent floor, changed border, wrong moss pixel. Eighteen relevant approved PNG/source/helper/reference/before pins remain unchanged; no foreign .meta files or whole-worktree snapshot is involved.

Two fresh builds produce byte-identical atlas, contacts, joined study, source cells and palette. Both v2 saved blends reopen to 1,536 exact unit quads / 12 named mesh groups, exact integer coordinates/parts/palette, and no animation. Saved blend hashes differ, so binary .blend identity is not claimed. The v2 first build and proof are under verification/v2-rebuild-first; verification/rebuild-identity.json binds both current exports. verification/v2-revision.json records the exact frame/palette preservation. The original v1 identity receipt is preserved under verification/v1. No old-atlas duplicate archive is created.

## Reproduce

Run from the repository root with Blender and the existing Python Pillow dependency:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_grovelands_environment.py -- --build
python3 Tools/Live16x24/build_grovelands_environment.py --verify
/Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Live16x24/grovelands-environment/editable.blend --threads 4 --python Tools/Live16x24/build_grovelands_environment.py -- --inspect-blend
```

Do not repeat --snapshot; it refuses to replace original input pins. Intended import: centered 16 PPU, Point/Clamp, no mipmaps/compression. frozen-handoff.json binds the candidate. This art author's source review is self-review; root/another reviewer owns independent source and actual judgments. No Unity, Git, runtime/test changes, gameplay or package execution was performed by this art task.
