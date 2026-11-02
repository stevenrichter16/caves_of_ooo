# Rim Forest canopy experiment — parked, disabled

Both actual gameplay candidates failed the visual objective. The approved original trees and published contact shadows remain active. No new coverage or area-polish acceptance is claimed.

The first candidate filled179-pixel trees to213–219pixels but made repeated round lollipops. The second introduced broad/leaning/tall outlines, uneven shoulders, exposed Y-forks and separated foliage planes;187/194/204pixels remain. Independent normal-zoom comparison found no materially stronger presence than the original. Fine notches added texture without fixing forest stature or sparse composition. Do not resume small pixel iterations as a priority.

Built-in imagegen supplied concept.png from actual RimTreeShadow/after.png. prompt.txt preserves the exact prompt; concept-critique.md rejects oversized crowns, fine texture, changed lighting and UI. No generated pixels were sampled. The generator creates original editable integer Blender quads within16x24 cells. Final source/native/contact and reopened blend pixels match exactly. The first candidate is retained in pass1.

Reproduce authoring with Blender --background --python Tools/Live16x24/build_rim_canopy.py and python3 ArtSource/Live16x24/rim-canopy/verify.py. The verifier's original200pixel minimum was revised to greater than the original179 after visual review required branch gaps; pixel count is not acceptance. Source-cell topology, native bounds and exact colors remain enforced.

The optional atlas, metadata, test source and runtime patch are parked in parked-integration, outside Unity Assets. The saved importer is an explicit authoring tool, not automatically run. Applying it alone does not enable integration. Live Stump catalog is byte-identical to published844f63353; approved original sprite/generator hashes are unchanged. Fresh actual restored gameplay is pixel-identical to the approved baseline.

See Docs/GRAPHICS-RIM-CANOPY.md and Docs/Verification/GraphicsExecution/RimCanopy/verification.json for native test corrections, review, actual evidence and limits.
