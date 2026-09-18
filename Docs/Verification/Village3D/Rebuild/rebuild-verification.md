# Village 3D rebuild verification — 2026-09-09

PASS: the adopted repository sources rebuilt independently into `/tmp/codex-village3d-rebuild` using Blender 5.2.1 LTS and art seed 9042026. No repository file or Unity state was changed by this audit.

- The adopted builder and both adjacent input snapshots match the delivered source hashes exactly; the source is self-contained with those inputs and Blender's bundled Python modules.
- Parsed manifests are identical across the original delivery, adopted source copy and rebuild: 123 runtime models, 71 native owners and 111 static placements.
- The palette PNG is byte-identical across all three locations. SHA-256: `7409346db43b43cf60e82f81d75ad186b0055b353be08380ab2f2c1b82031c7f`.
- All 123 runtime models passed a Blender FBX roundtrip comparison of world-space vertices, polygon topology/normals, smooth flags, UV/color layers, material assignments, vertex weights, skeleton rest matrices, sockets, animation take names/ranges and sampled curve keys. Numeric values were normalized to six decimal places.
- All four character variants preserve five takes and the exact four equipment socket names.
- The separate diagnostic probe preserves identical named-marker geometry and topology. Its accepted delivery was intentionally copied from the earlier GREEN Unity fixture; regenerated palette UVs reflect later art polish.
- None of the 123 FBX binaries is byte-identical. FBX timestamps/source paths/container metadata differ, so only exported content determinism is established.

The detailed per-model hashes and checks are in `rebuild-verification.json`. This audit does not establish Unity shader appearance, skin playback, native gameplay binding or measured performance; those remain separate integration gates.
