# Component browser review

Reviewed the actual exported package at `http://127.0.0.1:8769/Components/index.html` in the Codex in-app browser on 2026-09-05. This is browser/art evidence; Unity was not executed.

- Default browser assembly independently compared 1,572,864 RGBA pixels with the loaded actor-free baseline and reported an exact match.
- Selected `boulder-west-approach`, hid it, and used Locate: its stone silhouette disappeared and the composed underlying ground remained. Restoring and Solo showed the source cutout against transparency. The inspector measured 2,045 opaque sprite pixels matching the baseline and original reference.
- Removed all mutable components: 39 hidden, fixed/effect sections retained. Restored all to the baseline assembly.
- Enabled masks and inferred-region overlays. Small backing regions remained distinct from broad fixed-section alpha coverage. The six marks and seventh absence remained outside removal coverage.
- Exploded inspection separated the actual exported layers; restoring ended exploded/solo inspection.
- Actual-size mode exposed a 1536×1024 scrollable artwork area within a 948×653 viewport.
- Checked the layout at 1280×900 and 390×844. At narrow width the controls wrapped, the full fitted artwork remained visible, and the component inspector followed beneath it. Temporary viewport override was reset.
- Browser console inspection returned no warning/error entries.

The first accessibility-index automation attempt selected a fixed layer unexpectedly; its diagnostic hidden state was restored. Subsequent checks used the observed DOM control identities and verified the selected component explicitly. This was a review automation correction, not an unexplained success claim.

The browser's exact default comparison is not a semantic test of hidden surfaces. Every individual source/removal strip and the cutout contact sheet received separate raster review. Color-guided visible-pixel masks can retain a narrow original-background fringe, and broad fixed-section boundaries still need real actor/FOV/depth integration.
