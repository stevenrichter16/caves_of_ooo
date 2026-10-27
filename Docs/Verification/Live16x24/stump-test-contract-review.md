# Stump material test contract review

2026-10-27. Bounded source review of `FlatPixelStumpMaterialsTests.cs` and
`Docs/GRAPHICS-STUMP-MATERIALS.md` against the actual blueprints, liquid/well/
harvest services and submitted renderer. No concrete API or fixture blocker
was found in the reviewed initial92-case contract. This reviewer did not author
that initial fixture or the production Stump adapter and did not run Unity.

The meaningful preconditions match source: exact authored TepuiStone glyph
variants; fresh detached generation for natural formation tests; real LiquidFlask
fill command/adjacency; natural SprayPool remaining wet at zero stock; Tank's
Well without a pool and default Repairable refusal; actual Tepuibone yield/removal;
and positive partial damage. The submitted fixture restores the prior render
binding and cell/sight callbacks as well as renderer performance counters. Liquid
registry, audio/message sinks and Harvestable factory restoration are present.

Two review findings were corrected in the saved test source at root's request:

- **Missing main atlas must preserve water feedback.** The existing null and
  malformed atlas cases now place a real SprayPool, retain its permanent water
  state, and execute the production `ZoneRenderer` submitted-paint path. They
  require the same selected source, no native water/local ground, the actual
  CP437 water-wave mark, and unchanged coating serialization/stock. This also
  keeps the prior independently available Tree/Bush checks. A non-null fallback
  sprite alone no longer satisfies this water boundary.
- **Earned product identity must be graphical.** The actual vein-harvest case
  now builds an authorized inventory document for each genuinely earned
  Tepuibone and calls public `ZoneRenderer.TryGetFlatExaminePortrait` while the
  inventory canvas is paused. It requires actual actor custody and the existing
  `world-harvest-products` atlas, frame6 UV, white tint and a sprite rather than
  a model. Blueprint/count checks alone no longer satisfy its native-identity
  claim. No product is injected to obtain that assertion.

These additions extend existing case bodies; they add no new test-case
attributes, runtime behavior, alternate painter or framework. The source atlas
contract remains26 cells plus two unchanged vegetation reuses. Native execution
and final results remain root-owned; this note claims neither compilation nor
GREEN. No whole-scene, ordinary action, package or device acceptance follows
from source review.

Changed paths for this review follow-up: the existing Stump test file and this
new note only. Runtime, art, other living records, Unity, Git and builds were
not modified by this reviewer.
