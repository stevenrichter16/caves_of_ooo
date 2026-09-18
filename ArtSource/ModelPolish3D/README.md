# Sculpt-normal polish for the shipping miniature kits

The accepted wave audits392 models:74 of123 village,196 of218 ring and47 of51
pilot models receive surface refinements.75 stay unchanged. This is restrained
surface polish, not a wholesale resculpt or a change to native cell ownership.

`sculpt_normals.py` contains the deterministic math; `polish_bundle.py` is a
Blender-only postprocess of a **fresh ordinary kit build** into a new output
folder. It retains carved edges, blends shallow flat-face seams, and keeps80%
of the existing organic normal when applying a weighted correction. Leaf,
feather, scale-plate, mineral-fibre and flower highlights are deliberately
excluded. Ground/detail overlays and separate water geometry retain their
original shading. No extra triangles, textures, material slots or runtime
scripts are introduced by this pass.

```
python3 -m unittest discover -s ArtSource/ModelPolish3D -p test_normals.py
/Applications/Blender.app/Contents/MacOS/Blender -b --threads 2 --python-exit-code 1 --python ArtSource/ModelPolish3D/polish_bundle.py -- --kit SpawnRing3D --source /path/to/fresh-ring-build --output /path/to/new-polished-bundle --render
/Applications/Blender.app/Contents/MacOS/Blender -b --threads 2 --python-exit-code 1 --python ArtSource/ModelPolish3D/validate_bundle.py -- /path/to/new-polished-bundle
```

Use Village3D or MultiCellPilot3D for the other kits. Run their existing builders
first; the saved accepted masters are already polished and intentionally reject
another pass to avoid accumulating smoothing. Existing source recipes create
fresh originals. The completed village source now also retains its model FBXs.

The existing static exporters now carry inverse-transpose-transformed corner
normals through consolidation. The village builder also updates pending object
transforms before measuring/exporting: this fixes the original barrel0 hoops
at floor level and the olive character's stale bounds metadata. The accepted
barrel reflects the already-correct editable master; no native footprint changes.

`test_fbx_normals.py` exercises real FBX corner-normal/translation roundtrip.
`audit_semantics.py` compares actual original/final FBXs for positions, topology,
UV/colors/material assignments, weights, rest bones, sockets and sampled
animation curves. It excludes only intentionally changed normals/smooth flags;
barrel0's documented part translations are the sole allowed geometry exception.
`reexport_pilot_water.py` records the one-time candidate-material correction;
fresh builds use explicit PilotTar lookup in the main recipe.

Accepted editable masters, FBXs, palette textures and before/after galleries
live in each kit's usual ArtSource directory. Source/roundtrip ledgers are in
`reports/polish.json` and `reports/polish-roundtrip.json`; runtime acceptance is
tracked in Docs/MODEL-POLISH-3D.md and Docs/Verification/ModelPolish3D. Original
bundles are preserved under the absolute archive recorded in adoption.json.

Gallery renders are Blender studies near56° (about55.4°); they do not establish
Unity lighting/FOV appearance or subjective game feel. No photograph, baked
reference scenery or non-destructible replacement chunk is used.
