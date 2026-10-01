# Regional botanical forms

Original editable cuboid botany for35 new plants: five each for Spread, Sodden, Beating, Grovelands, Overwrit, Stump and underground Cave. This independent280-form pack preserves every existing crop, the previous37 repair/cultivation forms and their source hash.

Each species has three native growth stages (seed, sprout, standing ripe), each with damp/dry soil crumbs, plus a loose seed and useful harvest model. Harvest removes the plant; the existing CultivatedSoil owner supplies the remaining reusable bed. Wild plants do not get an invented fourth spent stage or terrain replacement. The plants contain no collision, lamps, animation controllers or gameplay scripts.

`build_source.py` authors35 separate pod, leaf, root, reed, cup and fungal structures. Stage1 uses their unopened form at sprout scale. Wet/dry pairs share geometry and change the soil palette only. Actual harvested materials retain the recognizable plant organ; no identical recolored sacks. Source `roster.json` is the approved art-design snapshot; gameplay identity remains authoritative in `BiomeCropCatalog` and the importer verifies every exact seed/crop/yield mapping against it. The24 native glade colors are shared with existing assets.

```sh
python3 ArtSource/BiomeCrops3D/build_source.py
python3 ArtSource/BiomeCrops3D/test_source.py
```

Review the source and update `BiomeCrop3DLibrary.ReviewedSourceSha256` before import. Use **Tools > Caves of Ooo > Art > Import Biome Crops**, or `CavesOfOoo.Editor.BiomeCropBuilder.Build(reportPath)`, from idle Edit mode. The importer preflights source, identities, mesh bounds and every typed output path before writes; it borrows the existing palette material, preserves GUIDs on reimport and does not touch user scenes. Output is limited to `Assets/Resources/BiomeCrops3D`.

```sh
/Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/BiomeCrops3D/preview_kit.py -- --output /path/to/review
```

This produces seven labeled PNG sheets and editable Blender scenes from the exact kit. It establishes source silhouette and state distinctions, not native gameplay, input, camera or save correctness. Those are checked with native Unity tests and Play evidence. Both wilderness and village renderers retain the real owners; imported seeds render independently of their origin biome. Ordinary cave admission requires the exact current cached graph and the receiving manager's protected-site predicate, leaving unmanaged addresses and authored places on their existing paths.
