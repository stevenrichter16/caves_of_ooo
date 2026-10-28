# Original Buckler native source

Source frozen for root's integration trial. This packet contains one original static 16×24 Buckler body. It does not establish actual gameplay, custody, reader, performance or area-polish acceptance. The art author performed source self-review; a separate reviewer and root viewed the revised contact and found no source-contact blocker. Root owns runtime publication and the actual-world gate.

The existing actual baseline is `Docs/Verification/Live16x24/buckler-before-04-ground-reader.png`: the legitimate dropped Buckler2700 in earned fixture167. The saved imagegen reference and prompt are design evidence only. Their invented UI/text and non-native pixels were not transplanted, traced, sampled or downscaled.

## Parts and revision

The authored body occupies `[2,9,14,23]` in top-left coordinates: 12×14 pixels, last opaque row22, row23 clear. A quiet grey-olive face is enclosed by a dark stepped round rim. Two upper-rim highlights and a clipped-corner 4×4 central boss give restrained flat relief. Its raised center is 2×2 pixels with one upper-left glint. There is no crest, glow, ground shadow, rear-grip object or extra animation/state frame. Appearance does not invent a material part, blocking effect, damage level or equipped pose.

Root rejected the first narrow five-row boss as a vertical eye and identified the stale GREATDEW contact label. `verification/v1/` preserves that verified native candidate, its source/builder, hashes and rejection. V2 changes only the boss and label; the 12×14 outline, face and palette remain fixed. The revised round boss was approved for an actual integration trial, not final scene acceptance.

## Editable production and checks

`source-cells.json` records original named parts and palette indices. `editable.blend` contains 144 integer unit quads at z0 in 10 named mesh groups, with exact emission materials. The original body and both contact instances are rendered directly from that geometry. The contacts show the same body over two plain backing colors; neither is a gameplay screenshot.

`source-verification.json` checks all384 native RGBA pixels, 49,920 direct-contact pixels, binary transparency, connected silhouette, exact bounds/baseline, 12 boss pixels and palette/source agreement. Four altered in-memory exports are rejected: missing boss, wrong face color, partial rim alpha and opaque padding. Pillow only decodes rendered PNGs for these checks; it never authors or edits production pixels. The genuine pre-build missing-output failure is retained in `source-red.json`.

Both fresh v2 builds produce byte-identical native PNG, native/enlarged contacts and source cells. Both saved Blender files were reopened and independently checked against every source coordinate, part and palette value; the geometry has no animation actions. Saved `.blend` bytes differ between the two builds, so binary Blender-file reproducibility is not claimed. The first revised build and its reopening proof remain in `verification/rebuild-first/`; the comparison is `verification/reproducibility.json`.

`protected-inputs.json` pins 13 relevant existing PNG, concept, baseline and helper inputs. All remain byte-identical. It deliberately excludes `.meta` files and unrelated dirty work. Root's wider preservation audit is separate. This builder never writes `Assets/`, imports resources or operates Unity/Git.

## Reproduction

Run from the repository root with the already supplied `protected-inputs.json`; do not silently repin inputs.

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_buckler.py -- --build
python3 Tools/Live16x24/build_buckler.py --verify
/Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Live16x24/buckler/editable.blend --threads 4 --python Tools/Live16x24/build_buckler.py -- --inspect-blend
```

Blender reuses the established native model, orthographic camera, contact-instance and deterministic PNG-timing helpers; no new rendering framework is introduced. Runtime import contract for root: complete `(0,0,16,24)` frame, centered pivot,16PPU, Point/Clamp, no mipmaps or compression, transparent padding preserved.

Final native PNG SHA256: `b626ae3aba231dfd8077ed395b887404c4de7a71334390ed0bd592277cbe79fe`.
Final editable blend SHA256: `577d1f0fc4c13121f3f71ba3618a4a03b9fff95f6e5ab3ef0a0c06603642c003`.

Actual graded ground scale, carried/equipped/ground reader identity, occlusion, removal and source-preserving restart remain root's next checks. This single equipment body does not resolve sparse chunks or whole-scene composition.
