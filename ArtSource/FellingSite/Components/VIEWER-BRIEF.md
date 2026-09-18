# Felling-Site component review viewer

Status: historical interaction brief written before implementation. The [viewer](index.html) and [manifest](build/manifest.json) now exist; the [package README](README.md) describes their current behavior, provenance, and limits. The requirements below preserve the design rationale rather than asserting that implementation is still pending.

## Purpose and scope

Provide a separate offline review tool for extracted scene components and the ground restored beneath them. A reviewer must be able to compare the original image with the reconstructed scene, remove a component from the composition, inspect the ground it concealed, and identify which pixels were copied from the source versus inferred during repair.

The existing `ArtSource/FellingSite/preview.html`, its movement study, and its supporting files remain unchanged. The new viewer lives entirely under `ArtSource/FellingSite/Components/`. Its title should identify it as **Component & underlay review**, with a persistent note: **Offline asset review · Unity validation pending**.

There is no traveler proxy, movement, collision, combat, destruction simulation, or ambient animation. Visibility controls change the review composition in browser memory. They do not demonstrate implemented gameplay or modify exported assets.

## Initial presentation

- Start in **Reconstructed** view, with all components visible, fitted to the available viewport, and review overlays off. If required reconstruction assets are missing, show the failure and allow Original view; do not silently substitute the original image as a completed reconstruction.
- Keep the artwork central. Use the restrained dark colors and typography of the existing viewer, with a compact top toolbar and a narrow component inspector. On smaller screens the inspector moves below the artwork.
- At 1280 × 720, fit the entire 3:2 composition, essential controls, and current asset status without cutting off the scene. Native-size inspection uses a bounded scroll frame rather than widening the page.
- Preserve the 1536 × 1024 image coordinate system in normal scene modes. Keep nearest-neighbor rendering available at native size; clearly distinguish native pixels from a downsampled fit view.
- Include a link back to the existing scene study. Do not replace its entry point.

## Primary controls

| Control | Behavior |
| --- | --- |
| Original / Reconstructed / Underlay | Mutually exclusive source views with clearly visible active state. |
| Fit / Actual size | Fit the complete composition, or render native pixels in a scrollable frame. Preserve the inspected point when practical. |
| Masks | Show authored extraction coverage and component boundaries, with a legend. |
| Inferred regions | Identify repaired underlay pixels and any inferred component pixels from manifest provenance. |
| Exploded layers | Inspect how components are separated and ordered; this is explicitly a presentation transform. |
| Remove all mutable | Hide every component the manifest identifies as mutable. Leave static components present. |
| Restore all | Clear visibility changes and solo mode, returning to the complete reconstruction. |

Keep selected-component actions together in the inspector: **Hide / Restore**, **Solo**, and **Locate**. Prefer these direct actions over a permanently visible toolbar full of secondary options.

### View semantics

**Original** draws the exact original image. It does not draw extracted components, repair patches, or the old movement-study traveler proxy on top. Explicitly requested mask/provenance annotations may be drawn as review overlays. Hiding or soloing components does not alter this source comparison; those actions either switch to Reconstructed with an announcement or remain disabled with a concise explanation. Choose one behavior consistently in implementation.

**Reconstructed** draws the actual exported underlay and the visible extracted components in their authored order and placement. It must use the same compositing rules that the export validator checks. If a component is hidden, the underlying repaired image becomes visible. Do not imitate removal by covering an object with a flat color, repeating the original crop, or merely reducing its opacity.

**Underlay** shows the actual base image used by reconstruction. If immovable cliff/tree artwork is intentionally retained in the base, label it **Base underlay** and state that retained features remain; do not imply it is featureless or fully cleared terrain. Component visibility settings are retained for a return to Reconstructed, but do not change this view.

An asset failure remains visible. For example, a missing sprite should be named in the component list and in the scene status; it must not be reported as hidden by the user. A missing underlay disables reconstruction and removal review until it loads.

## Selecting and inspecting components

1. Hovering a visible component outlines its actual mask and shows its short name. Do not rely on a bounding rectangle alone when transparency or a dedicated mask is available.
2. Clicking selects the topmost visible component whose mask covers the pointer. Selection remains after the pointer leaves the canvas.
3. When several components overlap, expose the overlapping names in a small inspector list so the reviewer can select a lower layer without hiding unrelated artwork.
4. The component list is the keyboard-accessible alternative to canvas selection. It includes hidden components so they remain restorable and inspectable.
5. **Locate** scrolls native-size view to the selected component. In fit view it briefly emphasizes the selected footprint without changing the composition.
6. **Hide** affects only the selected component. **Restore** reverses that change. A selected hidden component remains selected and is shown with an explicit Hidden state.
7. **Solo** shows only the selected component against a transparency checkerboard, with a faint source-footprint guide if requested. It is a separate inspection state, not a mutation of the user's saved visibility choices; exiting solo restores the prior composition.

The inspector should show the component's name, stable identifier, category, source rectangle, draw order, current visibility, mutable/static classification, extraction status, and provenance status. Treat mutable/static as authoring data proposed for later integration, not evidence that game destruction already exists.

Hiding a static component for inspection is permitted if the control is clearly a review operation. **Remove all mutable** must still act strictly on the manifest's mutable set. This distinction lets a reviewer inspect the source assets without conflating review visibility with game rules.

## Selected-component comparison

Provide aligned **Source**, **Sprite**, and **Underlay** comparisons for the selected component. These can appear as three compact crops in a desktop inspector or as tabs on a narrow screen.

- **Source** is a crop of the untouched reference at the component's authored location.
- **Sprite** is the exported extracted component over a transparency checkerboard. Use the same crop coordinates, padding, and scale as the other comparisons.
- **Underlay** is the same location in the actual restored base image, with optional inferred-region coverage.
- A mask toggle exposes the alpha/mask edge, including holes and detached details. Do not smooth the mask into a cleaner silhouette for presentation.
- Native-size inspection must be available for all three comparisons. A magnified crop can aid edge review, but must display its scale rather than call the result native size.

If an exported component contains inferred pixels, display their coverage separately from pixels taken directly from the source. If only a cropped source patch was extracted, report that accurately; do not describe it as an independently authored complete object with recovered hidden surfaces.

## Provenance and fidelity

Use short, evidence-backed status labels such as **Source pixels copied**, **Source pixels transformed**, **Inferred repair**, **Mixed**, and **Unverified**. A generated asset is not automatically faithful to the source.

The main status should distinguish two separate questions:

1. Does the full reconstruction reproduce the visible source image within the validator's stated tolerance?
2. What happens beneath a removed component, where the original image supplies no ground truth?

A perfect reconstruction of visible pixels does not verify the accuracy of hidden ground. Show the validator's measured result only when it is supplied by the manifest/report and is associated with the current assets. Include the comparison basis: exact RGB, RGBA, alpha-aware, or another explicitly defined method. Do not invent a fidelity percentage in the viewer.

For inferred-region overlays:

- Highlight repaired pixels that are currently exposed in the composition.
- If showing repairs underneath still-visible components, use an outline or otherwise distinct treatment and label them as covered underlay regions. Do not imply those repair pixels are currently visible in the reconstruction.
- Provide a textual legend and selected-component description; color alone is insufficient.
- Missing provenance is **Unverified**, not **Source faithful**.
- Inference coverage should come from an authored/exported mask or declared exact rectangle. Do not guess it from differences between two images at runtime.

## Masks and exploded layers

**Masks** should make extraction coverage easy to audit. Use deterministic, distinguishable colors for components, draw the selected mask more strongly, and retain the component name and source location. Include transparent holes and edge coverage. A dedicated mask must not silently disagree with the alpha used for compositing or hit testing.

**Exploded layers** should separate artwork enough to show component membership and stacking, using stable offsets and leader lines back to original footprints. Label the mode **Exploded review · offsets are illustrative**. The underlying asset placements remain unchanged.

Fit the union of the exploded presentation into the review viewport so layers do not disappear off the canvas. If that presentation uses a different scale from source pixels, show it clearly. Native pixel fidelity should be inspected in normal scene mode or the aligned component comparisons; an auto-fitted exploded view must not be labeled 1:1.

Exploded view can be static. If transitions are added, honor the operating system's reduced-motion setting and an explicit Reduce motion control; disabling motion must not remove any information or functionality.

## Accessibility and input behavior

- Use actual buttons, grouped view controls, labelled checkboxes, visible focus indicators, and an accessible component list.
- Provide keyboard equivalents for selection, hide/restore, solo, locating a component, and leaving inspection modes. Avoid intercepting ordinary page-scrolling keys globally.
- Make the native-size viewport keyboard-focusable for scrolling. Canvas pointer coordinates must account for both CSS scaling and scroll offsets.
- Announce deliberate state changes such as a selection, hiding a component, removing all mutable components, and returning from solo. Do not announce continuous pointer movement.
- Explain disabled controls next to the relevant asset or action rather than using a generic failure dialog.
- Preserve selection and normal visibility choices across source view changes. Resetting all restores authored defaults explicitly.
- Reopening or refreshing the viewer should load authored defaults unless persistence is deliberately added and visibly disclosed. Visibility changes must never be written back to source assets implicitly.

## Original manifest requirements

The following semantic requirements guided the agreed manifest. Current field names are defined by [`build/manifest.json`](build/manifest.json) and the implemented viewer, rather than this earlier requirements table:

| Area | Required agreement |
| --- | --- |
| Canvas | Reference dimensions and image coordinate convention. |
| Source assets | Original reference, actual reconstruction underlay, and any expected composite/report paths, all resolved relative to a documented base. |
| Component identity | Stable id, display name, category, mutable/static flag, authored default visibility. |
| Placement | Cropped-image versus full-frame convention; source rectangle, destination location, scale, pivot if applicable, and stable z-order. |
| Extraction | Exported sprite path, optional separate mask path, alpha handling, and hit-test mask threshold. |
| Repair coverage | Inferred/retained-source regions in the underlay; whether masks use full-scene or local coordinates. |
| Component provenance | Source-copied versus transformed/inferred regions, extraction method, and current verification status. |
| Fidelity evidence | Report path and its metric/tolerance, source and output hashes or equivalent identity checks, and validation status. |
| Incomplete assets | Explicit pending, failed, or unavailable states, with no success implied by file existence alone. |
| Grouping | Optional parent/group ownership if one review object consists of several sprites; whether hide/solo targets a group or an individual part. |

The resulting package keeps fixed structures independently layered, distinguishes sprite inference from removal-backing inference, and reports opaque-RGB comparisons separately from full-assembly RGBA comparison. See the current README for the resolution of these original schema questions.

## Review acceptance checks

- Original mode displays the untouched source without duplicate actors, component sprites, or ambient overlays.
- Full reconstruction uses the exported underlay and all authored visible components in the agreed order.
- Hiding one mutable component exposes the actual repaired underlay and leaves unrelated component visibility unchanged.
- Restore and exit-solo recover the previous composition predictably. Remove-all-mutable leaves every static component in its authored state.
- Mask hit testing selects the correct topmost component; transparent holes do not steal selection from lower layers.
- Fit view shows the entire scene at 1280 × 720. Actual size uses native pixels inside the frame, and selection still works after horizontal and vertical scrolling.
- Source, sprite, and underlay crops align at the same scene location and scale.
- Inferred regions and unverified regions are clearly distinguished from source-copied pixels.
- Missing assets and stale or absent fidelity reports are visible, and the viewer never labels a fallback as a completed reconstruction.
- Keyboard users can select a component, hide and restore it, inspect solo, and return to the full scene without relying on canvas pointing.
- Reduced motion preserves all review functions. No interaction claims to demonstrate implemented Unity gameplay.

This brief is retained as design history. Current implementation and verification evidence belong to the [package README](README.md), viewer, manifest, and associated reports.
