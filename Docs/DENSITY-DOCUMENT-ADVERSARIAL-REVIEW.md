# Independent C5 readable-document review

Status: 32 new adversarial cases plus the original 22 core cases pass after the production owner fixed physical reach: 54/54 GREEN. Standalone RED/GREEN receipts: `/tmp/coo-density-readable-adversarial-red.xml` and `/tmp/coo-density-readable-adversarial-green.xml`. Native focused verification also confirmed both reach cases; the root corrected Unity-specific null-array catalog parsing separately.

## Scope and verified contract

Reviewed `ReadableDocumentPart`, `ReadableDocumentCatalog`, `DensityReadableDocumentTests`, the C5 completion plan, actual `Codex01` content and the inventory/interaction/save APIs. Production files remain owned by the root agent; this reviewer changed only the new test file, its metadata and this report.

The catalog is an immutable projection of shipped Resources data. It rejects missing fields and duplicate IDs/blueprints, keeps the first valid row, skips malformed rows without reserving their identities and returns a copy of errors. Reading queues complete title/text and is repeatable. It accepts actual carried/equipped references without requiring a world zone; world copies require current shared-zone membership and nearby physical reach. Reading does not mutate knowledge, properties, health, item quantities or gameplay RNG. Existing token-graph save hooks preserve the document ID and actual inventory ownership.

## Findings

### 🟢 Closed: physical reach incorrectly used the save anchor

`ReadableDocumentPart.CanReach` compares `GetEntityCell` coordinates. These are canonical anchors, whereas the game supports bodies whose physical footprint extends beyond or excludes the anchor. Two non-vacuous tests demonstrate opposite errors:

- Positive contact: actor anchor `(10,10)`, occupied cells `(10,10),(11,10),(12,10)`, book `(13,10)`. Reading should succeed at one-cell physical distance, but refuses.
- Counter-check: actor anchor `(10,10)`, occupied cell `(13,10)` only, book `(11,10)`. Reading should refuse at two-cell physical distance, but succeeds because the empty anchor is nearby.

Applied by the production owner: retained the actual-zone checks and replaced anchor Chebyshev distance with authoritative `SpatialQuery.Distance`, including the invalid sentinel guard. Both counter-checks now pass; ordinary one-cell actors and owned inventory remain covered.

### 🟢 Fixture error corrected: duplicate Stacker part

The first stack-purity test added a new Stacker to a book that already inherits one. `GetPart` correctly returned the existing stack, so the test saw one instead of three. The test now modifies the actual existing Stacker to three before placement, then reads three times and proves count, ownership and state remain unchanged. This was not a production finding.

## Surfaces covered

| Surface | Positive / adversarial coverage |
|---|---|
| Ownership and staleness | Transfer between real inventories; physics backreference alone; world removal after menu creation; equipped item; carried copy without zone |
| Actor validity | Null actor, zero HP, committed death with positive HP, detached readable part |
| World contact | Foreign zone at matching coordinates; null-zone inference versus wrong explicit zone; both footprint/anchor counter-checks |
| Purity and rereading | Three full queued copies in order; three-unit stack unchanged; all actor stat values, properties, integer flags and HP unchanged; supplied gameplay RNG untouched |
| Persistence | Real token-graph actor/inventory restoration; stable catalog ID; full canonical text on reread after load; no new knowledge flags |
| Catalog cache | Reloading a missing catalog entry refuses stale menu; reset reloads canonical Resources |
| Malformed data | Null/empty/broken JSON, absent/null Documents array, each missing mandatory field, null row, invalid row followed by valid same identity |
| Duplicates | Repeated ID and repeated blueprint independently preserve the first valid artifact and allow later unrelated rows |
| Immutability and fidelity | Read-only collection cannot be cleared; mutating validation result cannot corrupt cache; Unicode, punctuation, newlines, indentation and tabs remain exact |
| Diagnostics | Success and refusal identify actual reader/book and distinguish inaccessible state |

## Cold-eye questions

- Q1: reading does not need an undo hook because it only queues presentation text. Save/load preserves the stable ID rather than serializing the catalog copy, matching the documented source-of-truth design.
- Q2: public lookup results and fields are immutable; validation returns a defensive copy. Read success/refusal use the standard `event` channel and actor/target envelope.
- Q3: required actor, reach, catalog validity, ownership and no-consumption branches have useful counters. The physical-contact pair is intentionally stronger than matching the implementation's current anchor formula.
- Q4: current core does not supply remembered reading state, as permitted by the plan. It does not claim document provenance alone establishes an in-world source. Placement, pagination and live input are separate root-owned acceptance gates.

## Honesty bounds

These 32 tests cover the listed bug classes, not every possible future interaction. The private .NET runner proves core mechanics, JSON behavior within its Unity stubs, save graph behavior and diagnostics. It cannot establish Unity's malformed-JSON implementation, actual pagination/input/frame behavior, visual text fidelity, natural discovery or sustained play balance. Native EditMode and input-driven document-page checks remain required. No source-text/lore rewrite was proposed or made.

Native compatibility correction: Unity's NUnit version does not provide `[NonParallelizable]`; root removed that optional fixture attribute. Unity normalizes a null catalog array to an empty array, unlike the private runner stub. Root therefore made empty catalogs report a validation issue. The private runner alone could not prove either Unity-specific behavior.
