# Repair and cultivation forms

Original editable cuboid source using the established 24-color glade palette. No downloaded meshes, external textures, scene changes or gameplay collision are included. `build_source.py` generates the reviewed `kit.json` consumed by Unity.

The finite pack contains 37 forms:

- Knotflax, hearthbulb and seamleaf: seed, sprout and standing mature, each with dry/wet soil. Moisture changes palette only. Knotflax has tall pale fiber heads; hearthbulb grows an exposed ochre bulb; seamleaf has broad pale-veined leaves.
- Cracked lining and snapped-line wells: broken/restored geometry. The crack faces the normal overhead camera; the missing line and loose bucket distinguish the rope fault.
- Timber gate: jammed broken, repaired closed and repaired open. Native Door state and axis select the leaf form; art never changes passability.
- Three seed packets, cord, raw/roasted bulb, seamleaf sprig and salvaged timber; three finite world supply sources.
- Sparse furrows appended above the original terrain mesh. Harvest removes the plant and leaves this saved, reusable bed; there is no decorative harvested plant or terrain replacement.

Regenerate and validate from the repository root:

```sh
python3 ArtSource/RepairCultivation3D/build_source.py
python3 ArtSource/RepairCultivation3D/test_source.py
```

After reviewing source changes, update `RepairCultivation3DLibrary.ReviewedSourceSha256`. Import from an idle Unity editor with **Tools > Caves of Ooo > Art > Import Repair and Cultivation**, or `CavesOfOoo.Editor.RepairCultivationBuilder.Build(reportPath)`. The importer validates every source and destination first, writes only `Assets/Resources/RepairCultivation3D`, preserves existing GUIDs, and never opens or saves a scene. It borrows the approved palette material.

Optional editable Blender review:

```sh
/Applications/Blender.app/Contents/MacOS/Blender -b --python ArtSource/RepairCultivation3D/preview_kit.py -- --output /path/to/review
```

This produces labeled oblique/top PNGs and an editable `.blend` from the exact JSON. Blender previews verify source silhouettes, not native camera readability, gameplay state, inputs or saves. Those require the real Unity renderer and player route.
