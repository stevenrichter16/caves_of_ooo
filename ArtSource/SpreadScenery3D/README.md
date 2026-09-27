# Spread missing scenery source

Original cuboid geometry for 27 exact native owner families, with two deterministic variants each and two extra unlit WatchLantern variants (56 models). `build_kit.py` reproduces `kit.json` without simulation randomness. `preview_kit.py` makes an optional offline gallery; it does not establish Unity visibility, fog, lighting, batching or owner authority.

The 24 color swatches are exactly the accepted ReferenceGlade3D palette. The importer borrows that library's existing material; it creates no new shader/material/texture and does not change the borrowed assets. All bounds remain within one native cell. Shapes add no collider, scripts, synthetic light, inventory, mechanics, quest text or lore symbols. Ground markers remain low marks, and the empty weapon rack shows no invented loot.

`CavesOfOoo.Editor.SpreadSceneryBuilder.Run()` is an explicit idle-Editor import. It verifies the reviewed source SHA, complete exact roster, finite nondegenerate cuboids, palette, borrowed library and every destination type before writing only `Assets/Resources/SpreadScenery3D`. Its temporary objects use a preview scene and are disposed. Generated native meshes and prefabs must pass the dedicated tests before use.

Only StoneFloor models have catalog kind `ground`, which suppresses the native fallback grass for that cell. Other models, including ground markers, remain `entity`. Actual live renderer refinement is a separate narrow integration: only supported current Spread/committed-lair owners with an existing `unmodeled-native-blueprint` result may use these models. Existing successful and refused recipes retain their original authority.
