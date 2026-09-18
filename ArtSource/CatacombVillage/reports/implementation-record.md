# Catacomb implementation and review record

Date: 2026-09-05. Scope chosen explicitly by the user: the earlier layered Felling browser scene's movement, component removal/restoration, backing surfaces and verification.

## Test-first implementation

- Source extractor: 16 tests; initial missing-module failure before implementation, then passing extraction/alpha/source ownership invariants. Source review refined nearby overlapping objects without reconstructing unseen sides.
- Scene producer: 17 tests; initial missing producer module failed before implementation. Tests cover immutable composition, nearest-neighbor behavior, invalid scale/IDs/state, atlas export and navigation/corner rules.
- Independent verifier: 53 adversarial tests. Raw red/green logs are in this directory; `README.md` distinguishes initial import failures from executed counterexamples. Covers confinement including symlinks, malformed manifests, strict PNG and alpha rules, source derivation, atlas bytes and placed derivatives.
- Browser: test-first pure state/input/geometry checks, then actual scene fixture tests. Adversarial cases caught hidden-prop hit testing in Underlay, encoded traversal paths, player scale validation and source/browser ID ordering differences. Player top-left foot anchoring and input while checkboxes are focused each received failing regressions before fixes. Position interpolation received tests before implementation.

The aggregate verification report and its raw logs are authoritative for the final test count and file snapshot; development counts above explain the workflow, not extra independent passing suites.

## Scene iteration

1. Inspected source RGBA and corrected the initial visual assumption about an opaque sheet background. Source alpha offered actual usable contours.
2. Extracted 51 assets, retaining selected source RGB. Kept native assets separate from placed nearest-neighbor derivatives.
3. Used Imagegen once to create a complete empty cavern support surface. Saved exact prompt/output. Composed 81 source-derived components over it.
4. The first navigation build failed its own destination assertions. Fixed bottom-aligned collision placement and two authored destination positions. All 652 initially open cells are now connected and the six destinations reachable.
5. Independent visual review found three mutable details that disappeared behind fixed architecture. Moved `threshold-south-right`, `garden-detail-09` and `garden-detail-13` to visible edges. All 46 individual removal comparisons now show a visible change, with zero changes outside the removed sprite's alpha support.
6. Replaced scattered pavers with a connected entrance path through the dolmen.
7. Corrected the source bottom-left pivot conversion for the browser player's top-left anchor. The actor now meets the ground at the intended grid foot position.
8. Improved desktop fit after live review showed the initial full-width canvas placed the traveler below the first viewport. At 1280×720 the complete scene is 687×458; native view remains 1536×1024 and scrollable. At 390×844 the fit canvas is 360×240 without horizontal page overflow.
9. Added smooth position interpolation using the existing single standing pose. Live review then exposed an undefined animation-time variable in Exploded view; this was returned for a runtime drawing regression and correction before delivery.

## Independent review and evidence

The art reviewer inspected the source cutouts, complete baseline and all 46 removal strips. Follow-up reviewed the changed strips and connected pavers; no remaining extraction contamination or fully hidden mutable detail was found. See `../inspection/composition-review.md`.

The verifier reconstructs selected source pixels, placed nearest-neighbor assets, whole-scene layering and atlas content independently of the producer. Passing unit tests alone are insufficient: live browser checks and rendered image reviews are recorded separately in `browser-review.md`.

No Unity execution, import or gameplay changes occurred. Existing unrelated repository work remains outside this package. Source alpha normalization and nearest-neighbor scaling are explicit; only native selected RGB preservation and authored-baseline reassembly are claimed exact.
