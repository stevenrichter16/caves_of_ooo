# Contextual hauling cues — private plan and source sweep

Status: private implementation passed its matched checks after actual native RED; root native GREEN and pixels remain pending. Root owns shared publication and all Unity execution. Original CoO UI extension, not a new hauling mechanic or a Qud-parity claim. Follows `Docs/QUEST-FREE-HAULING-IMPLEMENTATION.md`; exact query, marker and menu remain existing systems.

Goal: a visible reachable haulable offers the same actual `HandlingPart.HaulCommand` as its menu; the exact live load in the player's grip offers `ReleaseCommand` instead. Normal uses existing `C, direction: menu / haul` or `let go`; focused Look uses `Enter: menu / ...`. Opening the menu and actual existing command still own execution. No autograb, new key, second marker, source mutation or paid action in the query.

## Pre-implementation corrections

| Premise | Source / correction |
|---|---|
| Menu declaration proves all world prerequisites | `HandlingPart.AddHaulAction` deliberately omits reach; `DragSystem.Evaluate` also checks another holder. Cue uses existing current visible owner/reach gates plus actual holder state. |
| Read the real menu every frame | `WorldInteractionSystem.GatherActions` fires callbacks and allocates. Preserve query's no-event contract. Reuse sealed Handling metadata, `DragRules.CanDrag` and direct exact grip references; tests compare to actual menu rows separately. |
| Let Go uses current strength or carry eligibility | Existing grip's release is independent of changed Strength/weight. Require exact reciprocal current parts/owner, not a fresh grab admission. Never repair a stale link during read. |
| A beam/barrel is a fabricated fixture stand-in | Use actual factory `FallenBeam`/`HaulBarrel`, Strength18 player parameters, actual TryGrab/Release and actual gathered menu rows. No generation or paid-key claim follows. |
| New hint may supersede old actions | Append hauling after existing Harvest/Container/Door/Read priority. Retain old cell/order/layer rules and single cue budget. This may leave another existing eligible verb as the chosen cue. |
| Pure query means arbitrary custom event veto is predicted | No: as existing cues, this is a current built-in prerequisite hint; the real menu/command retains callback authority. Do not advertise a direct action key. |

Read: CLAUDE always-on/TDD and performance rules; `WorldAffordanceQuery`/query+adversarial+UI tests; `HandlingPart`, `DragRules`, `HandlingService`, `DragSystem`, `WorldInteractionSystem`; current F9 living plan. Root's actual native RED precedes production publication; private executed RED is retained separately.

## Bounded tests and change

Two new focused fixtures: command parity/flip, real strength and min-grip boundaries, hidden/stale/foreign/carried/equipped/too-heavy/other-grip refusal, current release despite strength loss, no callbacks/no query state mutation, retained-cue command flip, malformed reciprocal source refusal without repair, and existing action priority. Reuse ordinary cue tests as neighbors. No new renderer needed: the existing marker/HUD consumes the same command/hint struct.

Expected production scope is `WorldAffordanceQuery.cs` only: two command/hint rows and one narrow hauling predicate. No DragSystem/HandlingPart behavior changes. Keep ordinary indices stable. Current geometry remains the query's existing adjacent anchored-owner boundary; do not widen multi-cell selection in this slice.

Performance: no world scan, new cache, menu callback, per-query collection or marker. Evaluate only existing inspected cell owners and use current rules. `DragRules.CanDrag` calls existing `HandlingService.CanLift`, which can format a refusal string; do not claim zero allocations or add an unsolicited handling refactor. Root can measure under the existing native witness if a meaningful cost is observed.

Acceptance: executed private and native RED, minimal paired GREEN and focused old query/UI controls, peer review; native F9 witness should show normal Haul hint before grab, Let Go while held, and Haul again after release when still reachable, with menu closed cue and actual unchanged free command costs. Source geometry/art and hauling movement/save remain F9's separate gates. Can verify exact current query/menu ownership and no side effects; pixels and actual discovery require root's native view.

Production remains a private one-file query candidate. Root received and executed the original test-only package before implementation.

## Executed private checkpoint

New27 core cases:10 expected missing-current-cue failures,17 refusal/priority controls passed. Existing query30 and adversarial11 both pass; combined68 =58PASS/10FAIL/0skip. Actual-reference full EditModeTests compilation exited0 with no errors. Retained `private-red.xml`, `private-red-summary.json`, runner build/run logs and `reference-compile.log` distinguish private execution from Unity. The first compiler invocation used an obsolete compiler path and was corrected before the successful reference compile; no source was changed for that tooling correction.

Test-only four-path manifest SHA256 `f6d7154acdc6a5cca75d6c6f92753f0df32f32a3ecc0164e1e3ba6937ba9fadc`. Current query preimage `7606cfca7fee69a0188d58e942e01eea451d3747571d0f38fc1131e3cf9e0ed9`. This is the historical pre-implementation checkpoint. Root subsequently executed native44:34PASS/10 expected missing-cue failures. See README and verification-summary.json for corrected private baseline and final candidate.
