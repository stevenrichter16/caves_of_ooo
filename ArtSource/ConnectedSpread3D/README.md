# Connected Spread scenery

Fourteen original cuboid forms use the unchanged 24-swatch reference-glade palette. The source and the inert Unity meshes are the model authority; Blender renders are independent source review, not evidence of in-game readability or collision.

The contact sheet reads left to right:

1. Cracked pan, repaired empty pan, covered working pan, finished pan with cover set aside.
2. Empty pantry, actual ingredients held in pantry, empty pickup tray, actual finished parcel in pickup tray.
3. Cord-marked reserve tray, wrapped field meal, mundane botanical ink desk, Ditchkeepers' Footwork manual.
4. Heavy timber hoist frame, occupying one native tile with a passable route supplied by generation; permanent cord-marked tilled soil for the exact two claimed reserve beds.

The first review caught the pantry roof hiding its contents at the ordinary oblique angle. The final source uses an open shelf and narrow rear lintel, exposing the sack and pulp vessel. The four pan states change geometry, not just colour. The pickup parcel appears only when the actual container holds the matching owner; rendering never completes a kitchen job or regenerates stock.

## Reproduce

Run `python3 ArtSource/ConnectedSpread3D/build_source.py` and `python3 ArtSource/ConnectedSpread3D/test_source.py`. For source review, run Blender in background mode with `--python ArtSource/ConnectedSpread3D/preview_source.py -- --output <review-directory>`.

Unity import is explicit: `CavesOfOoo.Editor.ConnectedSpread3DBuilder.Run()` in idle Edit mode. It checks the reviewed SHA-256 and source geometry before writing only `Assets/Resources/ConnectedSpread3D`, borrowing the existing glade material. It creates no scene, actor, simulation component, collider or animation. Source changes require a reviewed hash update before reimport.

The existing cook, seedkeeper and Curation rigs retain their ordinary clips. There is no continuous work animation for an uncommissioned station. Existing crop stage meshes remain their actual crop-system owners.

Python source tests were recorded failing before generation and pass 5/5 after it. Native missing-library RED was recorded by the coordinator before library/import code. Native imported-state tests and ordinary camera acceptance are separate coordinator evidence; these source renders make neither claim.

The reserve bed overlay keeps its four stakes and cord after harvest. It is selected only for actual cultivated ground bound to the current local keeper, never for a copied marker or unrelated public bed. Bare soil has no moisture state of its own; the existing crop meshes continue to show real crop wet/dry states.
