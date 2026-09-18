# The Dream-Lit Catacomb

A new layered scene assembled from the supplied catacomb tileset. This package reaches the selected Felling **browser study** stage: movement, collision, independent removal/restoration, complete backing surfaces, component inspection and reproducible verification.

## Open and use

From the repository root, serve the files over HTTP:

```sh
python3 -m http.server 8772 --bind 127.0.0.1
```

Open <http://127.0.0.1:8772/ArtSource/CatacombVillage/>. A local server is already running at delivery. If that port is occupied by the existing server, use its URL rather than starting another copy. Opening `index.html` directly with `file:` will not support the manifest/image loading workflow.

- **Walk:** WASD/arrows or click reachable ground. Diagonal travel respects corners. The source-derived traveler moves smoothly between grid cells and sorts against object feet.
- **Inspect:** click opaque artwork or choose a named inventory item. Remove/restore mutable props, isolate them, or locate their bounds. Fixed architecture remains inspectable.
- **Remove all / Restore all:** bulk operations use the same state rules as individual operations. Restoration onto the traveler is refused atomically; move away and retry.
- **Navigation, Masks, Backing:** inspect declared collision cells, contribution masks and possible exposed substrate. **Exploded** shows separate component cards; **Actual size** permits native-resolution scrolling.
- **Scene / Baseline / Underlay:** compare current layers, the intact export and the composition with all mutable props removed. **Reset scene** restores objects and the entrance spawn.

Changes are an in-memory study state; reload/reset returns to the authored scene. Water and residents are static artwork. Removal demonstrates component and navigation behavior, without implementing gameplay rewards or destruction systems.

## What was built

| Item | Count / size |
| --- | --- |
| Native extracted source assets | 51 |
| Distinct source assets used in composition | 46 |
| Placed scene components | 81 |
| Independently removable components | 46 |
| Fixed components | 35 |
| Canvas | 1536 × 1024 |
| Study navigation | 48 × 32 cells; 32 pixels/cell |
| Initially open/reachable cells | 652 / 652 |
| Authored destination checks | 6 |
| Component atlas | 1024 × 1056; 81 entries |

The fungal hearth, niche dwellings, lantern tending, food storage and welcoming threshold follow the catacomb material culture in `Lore/10_Bible.md` and `catacomb_village_design.md`. They introduce no new canonical quest or Felling landmark rules.

## Art and hidden surfaces

`source-assets/` contains native-resolution extractions from the provided `05-catacomb-tileset.png`, a catalog, ownership masks and review sheets. The sheet already had alpha. Authored bounds/exclusions plus a documented alpha threshold remove labels, neighbors and almost-transparent background. Selected source RGB is preserved exactly; opacity is normalized to binary alpha. There are no invented hidden sides of these objects.

`generated/empty-cavern-v1.png` is the untouched Imagegen output of the exact prompt saved in `imagegen-prompts.json`. It supplies a **complete empty substrate before any objects are placed**. Removing a component exposes existing ground; it never progressively paints over the baseline. This is new scene construction, so no claim is made to recover ground from an original flattened scene. Ambient illumination is part of the substrate; individual lamps do not own dynamic lighting or shadows.

`build/sprites/` contains nearest-neighbor resized placements, with source identity and scale in `build/scene.json`. These remain palette-preserving derivatives, separate from the native extracted assets. `build/masks/` records each placement's alpha support. All contact layers are null: there are no separately baked object shadows to erase.

`build/baseline.png` is the intact new composition, `build/reconstructed.png` is reassembled from disk, `build/base.png` is the complete floor, and `build/underlay.png` is floor plus fixed components. `build/comparisons/` holds all 46 individual removal comparisons. `build/inference-mask.png` identifies the potential support revealed by mutable components.

## Edit, rebuild and verify

Requirements: Python 3 with Pillow and NumPy, and Node.js for browser state tests. No npm packages, Aseprite or Unity are needed for this study.

```sh
python3 ArtTools/catacomb_extract.py
python3 ArtTools/catacomb_scene.py
python3 ArtTools/verify_catacomb_scene.py
```

Run from the repository root. Edit `source-authoring.json` to change extraction, and `scene-layout.json` to change scene placement/collision. Re-extract only when the source/extraction changes; always rebuild after changing layout or the scene producer. The saved generated substrate makes rebuilds deterministic; Imagegen need not run again. Reload the browser after successful verification.

The independent verifier reads existing exports and exits nonzero on failed/skipped tests, changed inputs, malformed assets, stale hashes or mismatched pixels. Its current result is `reports/verification.json`; raw logs and per-gate JSON are beside it. See `reports/README.md` for exactly what the gates establish and `reports/browser-review.md` for live interaction checks.

## Coordinates and future integration

All canvas positions/bounds and mask pixels use a top-left origin. Bounds are `[x, y, width, height]`. Components sort by `(depth, id)`; depth normally equals the authored foot's canvas Y. Collision uses explicit grid cells rather than the whole sprite rectangle, so a gate opening can remain traversable.

Component/catalog pivots and atlas pivots use normalized **bottom-left** coordinates, with both top-left source rectangles and Unity bottom-left atlas rectangles exported. The browser player has an explicitly tagged **top-left** pivot; its foot is anchored at `[(cellX + 0.5) × 32, (cellY + 1) × 32]`. Converting that Y convention twice will misplace the actor.

The traveler uses one standing source pose, interpolated in position; there is no directional walk sheet. This package does not import into Unity, run Unity tests, create a Unity scene or claim equivalence to the repository's separate W6.5 Felling gameplay. The selected browser mechanics are complete; Unity integration remains a subsequent task.

The production prompt, milestones and completion evidence are in `Docs/CATACOMB-SCENE-PLAN.md`.
