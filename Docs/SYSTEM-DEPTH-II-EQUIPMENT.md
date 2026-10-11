# System depth II — equipment iterations 7–9

Baseline `428cb4be9`. CoO extensions; no Qud parity claim. Root owns native Unity and final Play. Standalone checks use an isolated copy of the tracked runner and establish gameplay, not rendering/input/feel.

## Sweep and boundaries

| Premise | Verified source | Decision |
|---|---|---|
| Item AV is global | GetPartAV reads only the struck part; Back has target weight0, Handwear5. | Report each actual planner-affected physical slot; explain untargetable slots; preserve global DV/speed/resistance. |
| Candidate can be temporarily equipped to compare | EquipPlanner supplies actual claims/displacements without mutation. | Use the plan and retained owner snapshot only; keep TryDescribe API for companion management. |
| Every cudgel enemy can Slam | CombatTacticsPart supports8 powers, excludes Slam. SpreadDitchMate equips Cudgel but grants no skill. | Add one audited exact-adjacent preview and one authored kit/warning. Reject a predicted collision with nonhostile creature; preserve normal skill rules. |
| Schematic Study is available | SchematicPart offers it without BitLocker and rejects later; normal players deliberately lack it. | Honest read-only description and executable-action eligibility; no normal tinkering unlock. |

## Iteration 7 — physical-slot comparison

Plan: retain item facts and the exact auto/manual EquipPlan choices. Replace aggregate ArmorPart AV changes with equipment-only AV before/after for every claimed or displaced targetable slot. Explain non-targetable slots rather than imply their AV protects another part. Already-equipped armor describes actual worn coverage. Natural armor/effects remain outside the item delta. Preserve global DV, speed and equip-bonus deltas, including multi-slot deduplication. Snapshot new coverage metadata against extensible description mutation.

RED/core: real authored cloak/body/head/handwear, real equip outcomes and current worn coverage, two-slot release, ordinary global tradeoffs, no-body fallback. Counter/adversarial: zero/abstract/currently changed targetability, metadata callback mutation, mixed slots, exact ownership, no mutation and extreme arithmetic. Native inventory/companion callers retain the same API.

Status: implemented; isolated GREEN 55/55 (17 new cases plus 38 existing comparison cases). Observed distinct RED was 14 failed / 3 passed, before production; the initial inherited-fixture run duplicated core cases and is preserved separately. Receipts: `Docs/Verification/SystemDepthII/iteration07-red-distinct.xml`, `iteration07-green.xml`. Native `FiftyClarityDetailsTests` intentionally updates two obsolete global-AV text pins; native execution remains root-owned and pending.

Implementation: `AddArmorChanges` uses the union of actual claimed and released slots, reads each prior occupant and candidate ArmorPart, and reports a long-valued delta only for normal hit locations. Already worn armor reports its live coverage. New Abstract/TargetWeight snapshots reject changed-body descriptions. API and all equip semantics are unchanged.

Self-review: 🔵 long arithmetic preserves extreme deltas; callback mutation, untouched natural AV, released shield slots and no-equipment mutation are checked. 🧪 native display/layout and player readability await root. Independent read-only review by the companion-equipment owner found no actionable issue; its caller can keep the same API. No scope divergence.

Files: `EquipmentComparisonService.cs`, `SlotArmorComparisonTests.cs` (+ meta), two intentional assertions in `FiftyClarityDetailsTests.cs`, this log and raw receipts.

## Iteration 8 — a real shove threat

Plan: grant the existing ditch mate Cudgel_Slam through CombatTacticsPart. Exact adjacent contact and actual equipped cudgel are required; preview the bounded push path conservatively to avoid colliding with a nonhostile creature. One registered skill action/cooldown; reject unsupported/hidden/party targets. Existing physical brace is a real counter, not damage/stun immunity. Modify only this blueprint's skill and warning, with parsed before/after proof.

Status: observed isolated RED: all 28 new cases fail before production because Slam is not granted/eligible; receipt `iteration08-red.xml`. Tests cover scheduler replacement, authored content, collision friend/hostile/solid, no gear/chance/cooldown, brace, eight directions, stale/multicell owners and save.

## Iteration 9 — schematic availability

Plan: read actual actor access and recipe availability before advertising Study; inspection explains dev-only unavailable use without granting access. Preserve authorized developer learning, known/missing recipes and consumable ownership. A refused command must not report successful study. Keep schematic sale/ownership and ordinary loot unchanged.

Status: queued after8. Tests cover actual ordinary/dev actors, readable reason, no item/recipe/BitLocker mutation from inspection, known/missing recipe, positive consumption, stale ownership and registry changes.
