# Refined ring: six actual Unity screenshots

Reviewed read-only on 2026-09-10 by the source-art author. Personally inspected all six `R3D-ee8303b92cc344c9860afc21e19422a4-profile-full-*.png` screenshots under `/Users/steven/caves-of-ooo/Docs/Verification/SpawnRing3D`, the corresponding exact ImageGen references, and current refined Blender previews. No Unity, Blender, rendering, asset or production-source changes were made during this review.

**Assessment:** the three bounded refinements are visible in the game and improve readability. I do not see a new missing-material or broken-mesh defect in the visible regions. This is not full reference-fidelity acceptance: the reference remains substantially richer and more organic.

| Zone | Actual screenshot observation |
| --- | --- |
| NW, Overworld.2.5.0 | Warm stone ground is clearly distinct from Grove. Repeated black ground flecks are gone. Ledges show varied outline/value; surrounding rock families still repeat. |
| N, Overworld.3.5.0 | Root grain flows along the large forms instead of reading as dense small cards. All seven bare scar circles remain visibly empty. Broad root surfaces and moss transitions remain simple. |
| NE, Overworld.4.5.0 | The new stone paint carries through. Existing rows of rounded wall blocks still read as separate repeated pieces, much simpler than the reference's layered rock ridges. |
| W, Overworld.2.6.0 | Ground clutter is quieter, making the actor and compost rows easier to separate. Fine crown edges remain dark and busy at native camera scale. |
| E, Overworld.4.6.0 | Ground and visible room outline remain readable; native local lighting is plainly visible. Foliage and rock borders remain more repetitive than the reference. |
| SW, Overworld.2.7.0 | The same ground improvement survives a different layout. Repeating border-stone profiles and dark crown outlines remain; no new art regression is apparent. |

## Scope triage and known visual limits

The initial review labeled the following three quality differences yellow. That classification was too broad: none establishes an observable defect in the adopted bounded refinement or native contract. They are retained as documented visual limits rather than required corrective work. The current guide already records faint ground-paint repetition, repeated native rock-cluster profiles, simplified root/moss transitions and the absence of an exact ImageGen-fidelity claim. See `/tmp/codex-spawn-ring-refinement/final/README.md`, Known visual and validation limits.

The accepted scope was quieter ground and distinct tepui paint, varied ledge silhouettes, and fewer rounded root ridges without changing native placements or increasing triangle budgets. The screenshots visibly support those changes; the source/import/placement receipts establish their structural contracts. No mandatory yellow defect remains from this six-image review.

**🧪 V1 — Known foliage appearance difference; no proved defect.** Most evident in W/E/SW: visible crowns contain many nearly black tiny contours, while the refined Blender preview retains more overlapping rounded leaf volume. The reference uses softer grouped leaves with clearer crown-scale light/shadow. The foliage models were unchanged by this refinement, and the screenshots do not establish an incorrect shader, normal, material, or new regression. Shared authored style and native lighting/sampling differ from the reference. This is an appearance limit; a later controlled investigation would be needed before classifying it as a fixable defect.

**⚪ V2 — Recorded stone/earth fidelity limit within the native layout.** NW/NE now have the right biome distinction, but long open areas remain largely uniform paint with repeated faint crack/patch motifs. Existing walls and boulder clusters repeat their unit-sized profiles. The reference interleaves exposed stone, accumulated moss, and irregular debris more convincingly. This gap also exists in the refined Blender preview and is already recorded in the guide. Sparse native placements are authoritative, and repeated families intentionally use bounded variants. The reviewed scoped improvement is present; no missing-content or misplaced-geometry defect is demonstrated. Any discretionary later art work must preserve native occupied cells and the clean scar rather than add implied obstacles.

**⚪ V3 — Recorded root/moss fidelity limit; scoped ridge refinement is present.** The longer rounded ridges remove the earlier card repetition and read coherently at native scale. The large underlying surfaces and their transitions into moss/shelves remain smoother and more regular than the reference's deeply integrated petrified bark. Existing dynamic contact shadows are strong along some root boundaries; there is no visible cap hole or new geometry break. Simplified root-to-moss transitions are already recorded in the guide and do not contradict the bounded ridge refinement. Do not label the current result an exact match to the reference.

**🔵 Confirmed improvements:** repeated raised ground speckling removed; tepui/Grove palette separation visible; four ledge silhouettes visibly less uniform; Felling ridges follow the root direction. No new magenta/missing-material surface, exploded model, or obvious cap hole was seen in these six screenshots.

## Honesty bounds

- Black unobserved/occluded cells are native FOV, not evidence of missing art. The large square brightness steps follow native cell lighting/visibility; this review does not classify them as a simulation defect or recommend revealing hidden cells.
- These are six static frames after movement/actions, not matched before/after captures with identical light or entity state. They establish visible appearance, not animation quality, frame time, equipment behavior, save integrity, collision correctness or complete owner coverage.
- The blocked S profiling lane is not diagnosed from these images. S/SE final Unity screenshots were not present for this review.
- Functional/import/test outcomes belong to the parent-owned receipts. This review neither expands the frozen art scope nor asserts that existing visual debt has been fixed.

## Triage conclusion

No asset or code correction is justified by these screenshots alone. Retain the known visual limits in the release documentation; do not claim full reference fidelity. Continue the parent-owned native/performance gate, including the still-unreviewed S/SE frames, without treating these observations as confirmed shader or content bugs.
