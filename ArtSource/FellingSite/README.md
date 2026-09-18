# The Felling-Site — source art and playable scene

This folder contains the original interactive art/movement study and [extracted RGBA component package](Components/README.md). The current Unity implementation is tracked in [`../../Docs/FELLING-UNITY-SCENE-INTEGRATION.md`](../../Docs/FELLING-UNITY-SCENE-INTEGRATION.md): native assets live under `Assets/Resources/SceneArt/FellingSite`, and the real game uses the existing Felling-Site at world (3,5). The original offline plan remains in [`../../Docs/FELLING-SCENE-BUILD-PLAN.md`](../../Docs/FELLING-SCENE-BUILD-PLAN.md).

The component package now supplies **55 layers: 39 removable candidate props and 16 fixed/effect partitions**, with local inferred backing and a true RGBA prop atlas. The earlier opaque prop studies are preserved as historical attempts; they no longer describe the current atlas's transparency status.

## Open the study

From repository root:

```sh
python3 -m http.server 8769 --bind 127.0.0.1 --directory ArtSource/FellingSite
```

Open <http://127.0.0.1:8769/preview.html>. A server is required because the page fetches the layout JSON. Python's standard library is sufficient to serve it; there is no build step or package install.

Open <http://127.0.0.1:8769/Components/> for the separate component review: select, hide/restore, solo, or remove all candidate props; compare source, baseline, reconstructed scene, and base underlay; inspect masks, inferred regions, native pixels, and aligned crops. Its visibility controls are local asset-review operations, with no gameplay simulation.

Click a destination to walk, or focus the scene and use WASD/arrows. R resets; Esc cancels a route. The default view fits the whole composition. **Actual size** provides a scrollable 1536×1024 view for texture and seam inspection. Controls expose the grid, approximate collision, six positions/seventh point, foreground overlap, and an explicitly approximate FOV. Reduce motion stops ambient effects.

The current project actor is a movement proxy. Its palette and silhouette differ from the painted reference traveler. Movement does not yet trigger real game turns or interactions.

## What is prepared

| File | Purpose |
|---|---|
| `reference.png` | Exact selected reference, preserved unchanged. |
| `generated/clean-plate.png` | Imagegen traveler-removal edit. The preview samples only its 80×96 patch at (736,800), retaining the original reference elsewhere. |
| `generated/ground-repair.png` | Alternate hidden-ground study; not used by the preview and not approved for destruction underlays. |
| `generated/studies/environment-props-v1.png` | Twelve prop concepts: fossil root fragments, sundews, teal plants, fungi. Opaque study, not cutouts. |
| `generated/studies/environment-props-alpha-attempt.png` | Follow-up alpha request; also opaque. Preserved as a rejected attempt, not silently substituted for transparent sprites. |
| [`Components/README.md`](Components/README.md) | Actual RGBA package, provenance, review controls, rebuilding, and remaining limitations. |
| [`Components/build/manifest.json`](Components/build/manifest.json) | 55 composed layers, source rectangles, contact contributions, masks, provisional anchors, hashes, and ordering. |
| [`Components/build/prop-atlas.png`](Components/build/prop-atlas.png), [`prop-atlas.json`](Components/build/prop-atlas.json) | True RGBA atlas of the 39 candidate props with slice/pivot metadata; the current `environment-props` asset in `layout.json`. |
| `layout.json` | Editable source-space geometry, cell mapping, six landmarks, seventh absence, entrance, blockers, occluders, effects and camera proposal. |
| `Prepared/` | Geometry/occupancy and Unity-facing JSON export; refreshed by strict verification against current assets. |
| `Integration/Core/` | Offline-tested C# mapping, occupancy/pathing, sample ownership and owner invalidation. |
| `Integration/Unity/` | Staged importer source; `.cs.txt`, outside Unity compilation. |
| `Integration/UNITY-INTEGRATION.md` | Exact integration constraints, installation recipe and remaining Unity test matrix. |
| `imagegen-prompts.json`, `reports/alpha-attempt-prompt.json` | Exact prompts used with built-in Imagegen. |
| `asset-provenance.json`, `reports/` | Original paths, hashes, asset findings and verification evidence. |

The scene occupies columns 16–63 of the existing 80×25 zone at 32 art pixels per cell. The upper 224 pixels are visual overhang. The browser study retains its conservative 306-cell clearing. The Unity export uses [`Integration/physical-layout.json`](Integration/physical-layout.json), which adds reachable approaches to all 39 removable objects and connects 1,638 open cells across the full zone. The native deep river channel and monumental geology block movement; native liquid interactions remain available from safe adjacent cells.

## Verification and remaining work

```sh
python3 ArtTools/verify_felling_scene.py
```

This requires complete assets, runs the parent scene's offline checks, and records results. Its recorded 2026-09-05 run passed strict asset validation, refreshed `Prepared/`, and passed **61 tests: 28 Python, 16 Node, and 17 C#**. See [`reports/report.json`](reports/report.json) for the exact run and logs. Re-run after regenerating assets so the report identifies current bytes. This is the parent scene runner's result, not the separate component runner's final outcome.

Python image validation requires Pillow; JavaScript checks use Node; C# checks use a .NET 10 SDK with no external NuGet packages. Component extraction also requires NumPy and SciPy. None requires Aseprite or Unity.

The old alpha blocker has been addressed by user-authorized deterministic extraction and export. [`Components/build/prop-atlas.png`](Components/build/prop-atlas.png) is an actual RGBA image with transparent pixels; its metadata supplies source rectangles and pivots. The `requireTransparency` requirement remains in place. Neither rejected RGB study was renamed or presented as transparent artwork. This asset fact is separate from the final aggregate verification outcome and final silhouette/art approval.

To regenerate geometry and run component checks separately:

```sh
python3 ArtTools/felling_scene.py export ArtSource/FellingSite/layout.json --out ArtSource/FellingSite/Prepared --require-assets
node ArtTools/test_felling_preview.cjs
python3 -m unittest ArtTools.test_felling_components ArtTools.test_felling_components_contract ArtTools.test_felling_components_adversarial
node ArtTools/test_felling_components_viewer.cjs
python3 ArtTools/verify_felling_components.py
```

The component baseline uses the original image with the same small actor-removal patch as the movement study. Local inferred material is exposed when a candidate prop is hidden; some backing is carried by lower structural sprites. The component viewer distinguishes inferred coverage from baseline/original pixel comparisons. Texture transitions, masks, and overlaps still require final art review.

The dedicated [component extraction plan](../../Docs/FELLING-COMPONENT-EXTRACTION-PLAN.md) and [task prompt](../../Docs/FELLING-COMPONENT-EXTRACTION-PROMPT.md) record the later extraction stage.

These are visible-surface cutouts at authored placements, not complete unseen geometry for freely moved or rotated objects. Fixed partitions are broad; hiding them in the source inspector can reveal diagnostic voids rather than ground. The Unity adapter binds simulation owners, physical geometry, fog, depth and backing separately. Fixed geology stays in place in the game; clearing eligible props removes their owned contact pixels too. Current Unity test and live-play evidence is recorded in the [integration plan](../../Docs/FELLING-UNITY-SCENE-INTEGRATION.md). New animated water frames and free object relocation remain outside this integration. The older browser movement study's clipped foreground samples and ambient effects remain presentation approximations.
