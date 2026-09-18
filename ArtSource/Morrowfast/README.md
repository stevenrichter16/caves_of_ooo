# Morrowfast — the Stillcord watch

An Imagegen settlement study for world **(3,6)**, immediately south of the Felling. The Stillcord are a proposed local watch and rescue fellowship. The settlement has five houses, two outdoor market counters, a communal cistern, six visible residents, a creek frog and Mossfoot the tortoise. Its detailed design includes eight named residents, three optional quests, shop stock and ordinary household disagreements.

This delivery contains the art, extracted components, reconstructed hidden surfaces and a working authoring preview. **Morrowfast has not been added to Unity in this pass.** Dialogue, trading, reputation, creature AI and the proposed quests still require the native integration described below. The existing Felling Unity scene remains separate.

## Open and use

Open [the workbench](http://127.0.0.1:8772/ArtSource/Morrowfast/) while the repository's local server is running. To serve the package yourself, run `python3 -m http.server 8772 --bind 127.0.0.1` from the repository root. A different free port works as well.

- **Walk:** WASD or arrow keys; click open ground to follow a cardinal route. The small diamond is an authoring surveyor.
- **Inspect:** click a visible component or choose its name. Search finds houses, residents, creatures and furniture.
- **Lift roof:** expose that house's furnished interior. Its original walls and door remain in place.
- **Open door:** open its collision channel and reveal the inferred threshold. Closing onto the surveyor is refused.
- **Lift component / Restore:** hide or restore all layers owned by that object, including its contact contribution. A removed bridge withdraws its creek crossing; removal is refused under the surveyor or a queued route.
- **Original / Backing / Reconstruction:** compare states. Masks and walkable-ground overlays show extraction and navigation.
- Workbench state persists in this browser and can be exported/imported. **Reset scene** restores the intact image and arrival position. These are authoring states, not a Unity save file.

## Delivered assets

| File or directory | Contents |
| --- | --- |
| [reference.png](reference.png) | Approved 1536×1024 high overhead master; shallow front facades retain the illustrated RPG style |
| [build/manifest.json](build/manifest.json) | **71 semantic owners**, **145 cropped visual layers**, five rooms, source coordinates, world anchors, state dependencies and collision/support cells |
| [build/sprites/](build/sprites/) | Actual RGBA components: source subjects, contact collars, five roofs, five original doors, fixed wall shells, inferred floors and concealed neighbour fragments |
| [build/masks/](build/masks/) | One full-canvas ownership mask per component |
| [build/base.png](build/base.png) | Reconstructed outdoor backing; only declared support regions differ from the source |
| [build/reassembled.png](build/reassembled.png) | Intact exported-layer reconstruction, **zero differing source pixels** |
| [build/interiors.png](build/interiors.png) | All five roofs lifted; **21 independently extracted furnishings** |
| [build/all-removed.png](build/all-removed.png) | All mutable owners removed; retained wall shells over empty rooms and cleared ground |
| [build/review/components.png](build/review/components.png) | Component contact sheet |
| [build/review/removal-sheet.png](build/review/removal-sheet.png) | 66 individual removal/reveal inspections; matching crops in `build/review/removals/` |
| [authoring/](authoring/) | Reviewed silhouettes, exclusions, furniture offsets and 35 authored outdoor content cards |
| [prompts/](prompts/) and [provenance.json](provenance.json) | Eight preserved built-in Imagegen prompts and hashed generation outputs |
| [reports/](reports/) | Test history, current verification, visual review and PNG hashes |

The 71 owners comprise **35 outdoor objects/inhabitants**, **21 interior furnishings**, **five roofs**, **five doors** and **five inspectable wall shells**. Shells stay fixed in the preview. Tiny ground grains, indistinct growth, peripheral rock formations and perimeter fence sections remain environmental texture rather than individual inventory objects. The arch's ornaments are part of one arch owner; stall displays are part of their counter, not invented individually purchasable sprites.

## Reconstruction rules

The approved reference is immutable. Intact assembly from exported PNGs reproduces all **1,572,864 source pixels** exactly. The initial oblique generation is retained as an iteration, not substituted for the approved overhead image.

Ground hidden by props, indoor rooms and surfaces concealed by adjacent objects are **newly inferred**. Plate edits are clipped to declared masks, so Imagegen changes elsewhere in a full plate do not leak into the composition. Backing contributions behind a basket or stove belong to the surviving barrel/counter. Original door positions and masonry are retained; furniture offsets free the entrances. The cistern's repaired paving and the empty room floors received a second visual pass to remove former shadows and abrupt patch boundaries.

These are component extractions for this authored placement. Characters and creatures are single poses; animation sets and unseen sides are not supplied. Source contact collars contain local ground texture, so a moving actor needs a separate runtime contact/shadow. Roof-hidden interiors use a cutaway convention; fully free-camera 3D geometry is outside this asset's scope.

## Design and Unity handoff

- [Detailed settlement, residents, shops, quirks and quests](../../Docs/FELLING-SOUTH-SETTLEMENT-DESIGN.md)
- [Build plan and completed stages](../../Docs/FELLING-SOUTH-BUILD-PLAN.md)
- [Component and surface contract](../../Docs/FELLING-SOUTH-COMPONENT-CONTRACT.md)
- [Unity integration handoff](../../Docs/FELLING-SOUTH-UNITY-HANDOFF.md)

The proposed native placement uses **40.96 pixels per world unit**, a uniform **37.5×25** footprint centered at world x40 in the existing 80×25 zone. Its north/south road connects at x40. The fine **192×128** authoring grid is not silently used as Unity's native collision grid. Native generation, faction/actor registration, actual commerce and quest state, renderer sorting, save persistence and play-mode validation are the next integration stage.

## Rebuild and verify

From the repository root, with Python, Pillow, NumPy and SciPy available:

```sh
python3 -m ArtTools.morrowfast_components
python3 -m ArtTools.verify_morrowfast
python3 -m unittest ArtTools.test_morrowfast_components -v
node --test ArtTools/test_morrowfast_preview.cjs
```

The build writes generated files only inside this package. Tests read the current exported manifest; they do not silently regenerate it. Current checks cover exact reconstruction, source/support isolation, ownership and PNG dimensions, navigation to every component, closed-room exclusion, bridge removal, collision-safe restoration and validated state persistence. These checks establish art-preview behavior, not native Unity gameplay.
