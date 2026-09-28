# E4 — stable hauled beam identity candidate

Status: actual native baseline **1 failed /1 control passed** in root’s32-case initial E4 job. Failure is the same native FallenBeam changing`ring-fallen-beam-0`→`ring-fallen-beam-1` after actual DragSystem movement. The unrelated generated Grass proof and ungrabbed-beam control passed. Exact raw source receipt and selected results are linked in`native-red-extract.json`. Final private runtime and full test-reference compilation each completed with0 errors. Root adopted the exact two-file candidate, then native job e83d9210d878418c977cb10edb60c046 completed **71/71 PASS**,0 failures/skips,21.7683361 seconds. This includes all4 identity cases,43 recipe cases,12 exact-style cases and12 drag-follow cases. Root owns shared publication and Unity.

Root reviewed the narrow exact-blueprint branch and independent peer review found no blocker. Full native follow-up is retained in `../Integration/native-beam-and-neighbors-green.json`.

## Minimum correction

Only the exact native FallenBeam blueprint AND its exact FallenBeam catalog binding use deterministic saved blueprint/ID hash inputs, with neutral zone/cell inputs. No saved property, identity assignment, cache, RNG call, prefab or geometry change is introduced. All other branches retain their existing coordinate-based variant selection, sacred-scar exception, named refusals, pose and batch/transient behavior. Drag release does not switch back to a position hash.

Existing saved objects retain their native graph, IDs and positions. Their old *cosmetic* variant was not serialized: a previously position-chosen beam may choose a different shape once under the corrected identity rule. Thereafter that same ID keeps its chosen shape across movement and reload. Preserving every prior positional appearance while also surviving movement would require new persistent state; this bounded correction intentionally does not add it.

## Tests and scope

The original two native test cases are unchanged. Added controls:

1. Actual grab/move/release→full GameSessionState save/load→replacement owner retains moved position, current model, approved source mesh/material and exact preexisting Properties; stale old reference cannot claim current style. Uses the existing SpawnRing integration scope, whose nested NewGameSaveFixture restores active TurnManager and save/global scope. This is a staged command/save graph test, not paid UI travel.
2. An unrelated actual TepuiStone owner is explicitly relocated only as a diagnostic fixture control; its current position-dependent variant and exact source mesh still change. It does not claim the player can haul that ground owner. Existing unmoved Grass control remains too.

Recommended bounded native filters are recorded in`validation.json`: updated4-case identity fixture, current recipe tests(including the sacred scar and current-owner refusals), style proof12 and actual drag-follow neighbors. Broaden only for a concrete new failure. Save schema, renderer interpolation, zone-boundary dragging, all-haulable variant redesign and art polish are outside this fix.

## Q1–Q4 and in-phase review

- Q1 intent: one real hauled timber must keep its physical shape while location changes. Concrete native failure establishes the defect.
- Q2 implementation: exact two-name branch guard; reuse deterministic FNV helper with stable ID inputs. No gameplay/saved-field mutation.
- Q3 evidence: actual old2-case RED/control and reference compilation are complete; updated native4+67 neighboring cases all passed. The actual native saved replacement graph preserves the same moved model and rejects the old owner reference.
- Q4 limits: one-time cosmetic re-selection on upgrade; no live drag-motion or screenshot acceptance claimed. Existing discrete batched movement remains unchanged.
- 🟡 Confirmed beam shape instability repaired and verified by the matched native command/save tests; no current unresolved blocker in this bounded slice.
- 🔵 Preserve unrelated positional scenery and current authority: added exact counter plus existing native recipe neighbors.
- ⚪ Smooth hauling animation/pixel polish deferred; no witness here establishes a separate defect.
