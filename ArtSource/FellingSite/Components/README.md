# Felling-Site component package

This is the scene's extracted RGBA artwork and local hidden-ground backing package. It contains **55 independently composed layers: 39 removable candidate props and 16 fixed or effect partitions**, plus a base image and a transparent prop atlas. These source files remain outside `Assets/`. The Unity export copies their exact bytes into `Assets/Resources/SceneArt/FellingSite`; current implementation and validation are tracked in the [Unity integration plan](../../../Docs/FELLING-UNITY-SCENE-INTEGRATION.md).

The two earlier Imagegen prop sheets remain preserved as rejected opaque studies. Their transparency limitation has been superseded by the actual RGBA extractions in this folder. The current atlas is [`build/prop-atlas.png`](build/prop-atlas.png), with explicit rectangles and pivots in [`build/prop-atlas.json`](build/prop-atlas.json). The parent [`layout.json`](../layout.json) now points its `environment-props` asset to this atlas.

## Open the component review

From the repository root:

```sh
python3 -m http.server 8769 --bind 127.0.0.1 --directory ArtSource/FellingSite
```

Open <http://127.0.0.1:8769/Components/>. The separate [movement study](../preview.html) remains available at <http://127.0.0.1:8769/preview.html>.

The component viewer provides Original, Baseline, Reconstructed, and Underlay comparisons; alpha-aware selection; a searchable component list; Hide/Restore, Solo, Locate, Remove all mutable, and Restore all; mask and inference overlays; illustrative exploded layers; fitted and scrollable native-size views; and aligned source/sprite/base crops. Keyboard users can select from the list or focus the canvas and use H to hide/restore, S for solo, L to locate, and Escape to leave solo. It is a static review tool with no gameplay or animation simulation.

Visibility changes affect only the browser's current composition. A mutable flag identifies a proposed removable art object; it does not grant harvesting, destruction, dragging, or any other game interaction. Hiding a fixed layer is an inspection operation.

## Package contents

| Path | Purpose |
| --- | --- |
| [`authoring.json`](authoring.json) | Editable inventory, source bounds, extraction parameters, protected regions, and provisional anchors. |
| [`imagegen-prompts.json`](imagegen-prompts.json) | Exact prompt for the generated empty-environment backing study. |
| [`sources/empty-environment-v1.png`](sources/empty-environment-v1.png) | Preserved Imagegen source for plausible material behind removed props. It does not replace the complete scene. |
| [`build/manifest.json`](build/manifest.json) | Current scene/component contract, paths, hashes, placement, ordering, provenance, and contribution flags. All paths inside this manifest resolve relative to the manifest itself. |
| [`build/reference.png`](build/reference.png) | Untouched selected reference, including its painted traveler. |
| [`build/baseline.png`](build/baseline.png) | Reference with only the original traveler-removal patch composed into it; the intact assembly comparison target. |
| [`build/base.png`](build/base.png) | Actual base contribution. Fixed structures are separate layers, so their absence can leave diagnostic transparency. |
| [`build/cleaned-environment.png`](build/cleaned-environment.png) | Composition with all 39 candidate removable props absent, including the retained fixed/effect contributions. |
| [`build/inference-mask.png`](build/inference-mask.png) | Full-canvas grayscale coverage of inferred backing: 0 outside, 255 inside. |
| `build/sprites/`, `build/contacts/`, `build/masks/` | Cropped RGBA sprites, optional source-position contact contributions, and local grayscale removal-support masks. Each uses its component's declared bounds. |
| [`build/prop-atlas.png`](build/prop-atlas.png), [`build/prop-atlas.json`](build/prop-atlas.json) | The 39 candidate prop sprites in a true RGBA atlas, with top-left source rectangles, Unity bottom-left rectangles, pivots, source hashes, and 32 PPU metadata. |
| `build/comparisons/`, `inspection/` | Source/removal comparisons and source-scale inspection material. Some diagnostic contact sheets use a drawn checkerboard; those previews are not the actual sprite assets. |
| [`build/reports/verification.json`](build/reports/verification.json) | Producer measurements for intact assembly, local changes, protected regions, inferred coverage, and documented limits. |
| [`index.html`](index.html), [`viewer.js`](viewer.js), [`viewer.css`](viewer.css) | Offline component review tool. |
| [`INTEGRATION-CONTRACT.md`](INTEGRATION-CONTRACT.md) | The more complete runtime contract still needed for ownership, state, persistence, depth, and gameplay integration. |

## What is copied and what is inferred

The baseline keeps the original reference everywhere except the **80×96 patch at (736,800)** sampled from [`../generated/clean-plate.png`](../generated/clean-plate.png). That patch removes the painted traveler. Matching the baseline is therefore different from matching every original-reference pixel.

The extraction producer uses source-scale masks and preserves the visible sprite pixels assigned to each candidate object. Its repair material comes from the generated backing source, aligned and adjusted locally within authored removal support. It does not paste the full generated environment over the reference. The six barren positions and seventh absence have protected regions in the authoring data.

Fixed sections are partitioned from the cleaned baseline. A fixed structural sprite can therefore contain inferred backing that was hidden by a removable prop. Its `extraction.inferredPixelCount` describes that coverage; `repair.inferred: false` describes the absence of its **own removal backing**. These fields answer different questions. Hiding a small prop may expose inferred pixels carried by a lower structural sprite as well as pixels in `base.png`.

The viewer's Inferred regions overlay intersects the full-scene mask with actual base or sprite alpha and draws it at the corresponding layer depth. An intact source prop can cover the inferred backing beneath it. The selected sprite crop and export metadata expose structural inference separately from the component's own removal mask. Transparent diagnostic holes remain transparent.

The viewer compares the default reconstruction with the loaded baseline and reports exact RGBA differences. It also compares each selected sprite's opaque RGB pixels with the baseline and original reference; partial-alpha samples are excluded explicitly. A visible-image match does not recover or verify hidden ground.

## Compositing and state

Draw the base first. Then draw components with `contributesToComposite: true` in ascending `(depth, id)` order, drawing each optional contact contribution immediately before its sprite. A sprite, contact contribution, and repair mask all share that component's local `[width,height]` and source-space `[x,y,width,height]` bounds.

Hiding a candidate prop removes its visible contribution and its associated contact contribution at the original location. The viewer redraws from the current visibility state; it does not progressively paint repairs into the source image. The prop atlas supplies sprites only. Atlas placement alone does not reproduce the component/contact/backing state model.

Contact detail belongs to the original ground location. It must not travel with a moved object. Exported props show only the surfaces visible in this reference; they do not contain complete unseen sides, rotations, or newly exposed rear geometry. Removing an object for this local study is a narrower capability than moving it freely through the game world.

## Review and integration limits

- The inventory covers readable objects and groups, not every decorative speck. `authoring.json` and the producer report preserve disabled or ambiguous entries.
- The 16 fixed/effect assets are broad source-space partitions. Their boundaries are proposed depth/inspection boundaries, not complete standalone geological objects. Hiding them can expose a checkerboard diagnostic void; that is not authored traversable ground.
- Hidden material is inferred only within the prepared local support. Silhouette quality, thin stems, contact edges, overlaps, material continuity, and seams need visual review at native size.
- Each component currently has one provisional anchor cell in `footprintCells`. This is not a validated collision footprint, complete multi-cell visibility ownership, a liquid definition, or a physics binding.
- The manifest itself does not implement game behavior. The native Unity adapter supplies turns, guarded clearing, save/load, actor depth, FOV, shaders and import behavior. Its tests and live evidence are separate from this source package's raster checks; see the integration plan above.
- Full source-pixel fidelity at the authored placement does not establish fidelity after arbitrary movement, camera changes, rescaling, rotation, or gameplay state combinations that expose unprepared surfaces.

## Rebuild and verify

The user authorized deterministic image scripts for extraction and export after the earlier Imagegen-only study stage. The producer reads preserved sources and authored masks, then writes the generated build package. Rebuilding replaces generated files under the selected output folder; it does not edit the source reference or open Unity.

From repository root:

```sh
python3 ArtTools/felling_components.py
python3 -m unittest ArtTools.test_felling_components ArtTools.test_felling_components_contract ArtTools.test_felling_components_adversarial
node ArtTools/test_felling_components_viewer.cjs
python3 ArtTools/verify_felling_components.py
python3 ArtTools/verify_felling_scene.py
```

The image producer and its tests require Python, Pillow, NumPy, and SciPy (`scipy.ndimage`). The viewer tests require Node. The parent scene runner also uses the .NET toolchain for its isolated C# contract checks. None requires Aseprite or Unity.

Use the current [`build/reports/verification.json`](build/reports/verification.json), component [`reports/`](reports/), and parent [`reports/report.json`](../reports/report.json) for recorded results and the final aggregate outcome. Build measurements, test results, browser review, artistic approval, and Unity validation are separate evidence. The final offline run passed 100 Python tests, 19 Node tests and 153 strict asset checks, with zero intact-reassembly pixel differences. The final ten corrected removal crops were visually reviewed after rebuilding. Artistic limits above and Unity validation remain separate from those passes.

The dedicated [component extraction plan](../../../Docs/FELLING-COMPONENT-EXTRACTION-PLAN.md) and [task prompt](../../../Docs/FELLING-COMPONENT-EXTRACTION-PROMPT.md) specify this stage. The broader scene plan is [`../../../Docs/FELLING-SCENE-BUILD-PLAN.md`](../../../Docs/FELLING-SCENE-BUILD-PLAN.md). Historical opaque Imagegen attempts remain listed in [`../asset-provenance.json`](../asset-provenance.json).
