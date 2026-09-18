# Browser review — 2026-09-05

Surface: Codex in-app browser, local HTTP server at `127.0.0.1:8769`, default 1280×720 viewport. A temporary 390×844 override was used for a narrow layout check and reset afterward. No Unity editor/runtime was involved.

## Observed

- Default fit view shows the complete reference composition, southern approach and actor, with controls and readout visible at 1280×720. The cliff and six positions retain their reference locations.
- Narrow 390×844 view wraps controls/readout and retains the complete image without horizontal page overflow.
- Actual-size mode displays a 1536×1024 canvas inside a scrollable frame, initially showing the traveler. Click-to-walk in this view reached cell (40,14); pointer mapping still accounted for scroll position.
- The original painted traveler is absent from the cleaned floor when the proxy moves away. The 80×96 replacement was inspected at native size; it is serviceable for the study. Final texture/seam approval is still pending. The other source pixels are selected directly from the reference by the compositor.
- Keyboard movement updated the cell readout. An intermittent missed quick-tap defect was found and fixed: a press now queues an intent even if keyup arrives before the next frame. After reload, two quick W presses moved (40,20) to (40,18). Five regression cases were added; the final Node suite has 16 passing cases.
- Click-to-path reached (42,10) from the lower clearing. Clicking the left river at (21,12) produced a visible blocked/unreachable message and did not move the actor into the river.
- Grid, collision polygons and six/seventh landmark overlays appeared in the correct scene coordinate space. Optional FOV displayed an explicitly labeled approximation. This is not the game's actual FOV or a completed ownership mask.
- Reference comparison initially retained the movable proxy as well as the painted traveler; this was corrected. Final reference/fallback rendering suppresses the proxy, ambient animation and route overlay. Explicit review overlays remain selectable.
- Missing-clean-plate fallback was tested by temporarily holding the newly created file, refreshing assets, and observing the disabled Clean plate control and visible reference-fallback explanation. Screenshot showed only the painted traveler. The file was restored immediately; Refresh assets returned to the cleaned study and proxy.

## Art findings and limits

- Existing player sprite is intentionally a proxy; its blue-green clothing and silhouette do not match the reference's moss cloak. Actor animation/facing frames and final art matching are not implemented by this study.
- Two generated prop sheets were decoded and inspected. Both are RGB PNGs with a painted checkerboard. Neither is a transparent sprite atlas. Required `generated/environment-props.png` stays pending.
- Ground repair is an alternate source, not a verified underlay for every baked root/plant. Exact destruction masks, slicing rectangles, pivots and object-by-object occlusion silhouettes require further art work.
- Foreground overlap is implemented as clipped samples with foot-depth rules. This review did not certify every edge or behind/in-front configuration; that remains in the Unity PlayMode matrix.
- No Unity imports, renderer tests, true field of view, combat/turn input, save/load, HUD framing, performance or production-world routing were executed.

## Reproducible evidence

`report.json` records 28 Python + 16 Node + 17 C# tests passing (61 total). The complete-assets validation is a separate **failure** for the pending transparent props; the aggregate runner correctly exits 1. `Prepared/` was exported with pending assets explicit; current geometry contains 306 walkable cells and eight required destination routes. Exact 1,200-cell occupancy parity is covered by Node tests. Browser observations above are manual UI checks, not additional automated tests.
