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

Status: implemented; isolated GREEN 55/55 (17 new cases plus 38 existing comparison cases). Observed distinct RED was 14 failed / 3 passed, before production; the initial inherited-fixture run duplicated core cases and is preserved separately. Receipts: `Docs/Verification/SystemDepthII/iteration07-red-distinct.xml`, `iteration07-green.xml`. Native `FiftyClarityDetailsTests` intentionally updates two obsolete global-AV text pins; the comparison fixtures and updated native pins passed in root job95265fe24ccb4cfbb232cc1c4d5c0819 (`native-consumption-red-and-integration.xml`).

Implementation: `AddArmorChanges` uses the union of actual claimed and released slots, reads each prior occupant and candidate ArmorPart, and reports a long-valued delta only for normal hit locations. Already worn armor reports its live coverage. New Abstract/TargetWeight snapshots reject changed-body descriptions. API and all equip semantics are unchanged.

Self-review: 🔵 long arithmetic preserves extreme deltas; callback mutation, untouched natural AV, released shield slots and no-equipment mutation are checked. 🧪 native display/layout and player readability await root. Independent read-only review by the companion-equipment owner found no actionable issue; its caller can keep the same API. No scope divergence.

Files: `EquipmentComparisonService.cs`, `SlotArmorComparisonTests.cs` (+ meta), two intentional assertions in `FiftyClarityDetailsTests.cs`, this log and raw receipts.

## Iteration 8 — a real shove threat

Plan: grant the existing ditch mate Cudgel_Slam through CombatTacticsPart. Exact adjacent contact and actual equipped cudgel are required; preview the bounded push path conservatively to avoid colliding with a nonhostile creature. One registered skill action/cooldown; reject unsupported/hidden/party targets. Existing physical brace is a real counter, not damage/stun immunity. Modify only this blueprint's skill and warning, with parsed before/after proof.

Status: implemented; isolated GREEN113/113 (32 new plus81 existing AI cases). Observed RED28/28 before production (`iteration08-red.xml`). Independent review found personally hostile party collision partners needed the same explicit party protection as the primary target: dedicated review RED1/1, then the 113-case GREEN (`iteration08-review-red.xml`, `iteration08-green.xml`). Parsed content proof records only SpreadDitchMate's kit and warning (`iteration08-content-diff.json`). Both new Slam fixtures passed root native job95265fe24ccb4cfbb232cc1c4d5c0819 (`native-consumption-red-and-integration.xml`).

Implementation: registered owned Cudgel_Slam, existing exact-adjacent selection, actual equipped cudgel, locally visible contact, then the same three prospective footprint placements as the skill. At the first obstruction, any nonhostile or party-aligned creature vetoes the AI cast; terrain and hostile collision remain real. No traversal past a blocking wall, no temporary movement, no new skill behavior. Existing chance/cooldown/normal-action fallback are preserved.

Sweep correction: the boot brace requires authored Terrain underfoot; an empty fixture zone cannot establish it. The corrected positive setup asserts a current brace before the real Slam; the unbraced control moves three cells. A nonexistent Dirt blueprint was corrected to the real Terrain base and its insertion is asserted. These fixture repairs do not change production brace behavior.

Self-review: 🟡 fixed party-hostility asymmetry after independent review with executed RED. 🔵 remote body-edge selection and a collision in another footprint row have positive/negative coverage; duplicate grants, removed actor, eight directions, saved cooldown and no-extra-melee schedule are checked. 🧪 preview is conservative at invocation time; arbitrary later movement callbacks can alter the world. No broad invalidation or collision rewrite added. Player feel/encounter frequency and native rendering remain root-owned. No scope divergence.

Files: `CombatTacticsPart.cs`, only SpreadDitchMate in `Objects.json`, `DitchMateSlamTests.cs` (+meta), this log and raw receipts.

## Iteration 9 — schematic availability

Plan: read actual actor access and recipe availability before advertising Study; inspection explains dev-only unavailable use without granting access. Preserve authorized developer learning, known/missing recipes and consumable ownership. A refused command must not report successful study. Keep schematic sale/ownership and ordinary loot unchanged.

Status: implemented; isolated GREEN105/105 (29 new cases, 7 existing schematic, 69 existing item-inspection). Original observed RED20 failed/3 passed (`iteration09-red.xml`). The first23 new cases passed root native job95265fe24ccb4cfbb232cc1c4d5c0819 (`native-consumption-red-and-integration.xml`); the six follow-up callback cases and final identifier fix await root's final native sweep.

Implementation: `StudyRefusal` binds current carried ownership, positive quantity, matching backreferences, live recipe and existing BitLocker access before advertising or accepting Study. Rejection remains unhandled. Supported direct test/developer events use a local inventory receipt; ordinary commands join the existing outer receipt. Consumption is reversible until commit; recipe knowledge is published only after callbacks and payment commit, using the captured recipe identifier. Current source/part/recipe identity and identifier are rechecked before commit. Inspection uses the existing shared ItemExamineService route and says: “Tinkering is unavailable in this build. This schematic does not unlock it; it can still be kept or traded.” No new ordinary access, economic/content changes or UI mapping.

Review/counters: independent read-only review confirmed ownership and transaction boundaries; both reviewers identified mutable recipe.ID as a reference-identity gap. Observed follow-up RED had two failures; the added-stack test accidentally appended a second inherited Stacker. Corrected to replace the actual part and tightened the reentrant positive to retain one real carried unit. Corrected RED29 had exactly one failure (mutated recipe.ID), then GREEN105/105 (`iteration09-review-red.xml`, `iteration09-review-red-corrected.xml`, `iteration09-green.xml`). All raw receipts retained.

Self-review: 🟡 bound learned ID to the selected recipe through callback validation and publication. 🔵 missing/known recipes, no reader/access, stale carriage, replaced part/physics/stack, callback throws, registry replacement, post-action return, consumption flag changes, reentrancy, real persistence and read-only economic ownership are checked. Existing actorless Study advertisement test intentionally now supplies an authorized reader with the carried schematic. 🧪 final native callbacks/UI and player feel remain root-owned. No scope divergence: BitLocker stays an explicit developer fixture facility, never an ordinary unlock.

Files: `SchematicPart.cs`, `ItemExamineService.cs`, `SchematicAvailabilityTests.cs` (+meta), intentional eligibility setup in `SchematicPartTests.cs`, this log and raw receipts.

### Iteration 9 — legacy consumable policy pins

The wider material regression sweep surfaced four older schematic expectations from `a0dd91c4a`: failed known/missing/no-access Study returned handled success, and a nonconsuming ground schematic could be studied. These are deliberate policies replaced by iteration9, not evidence that the old tests were originally incorrect. Root explicitly approved aligning the pins with current carried-access eligibility and honest refusal.

Reproduced unchanged pins: isolated114 total,110 passed, exactly4 failed (`iteration09-legacy-pins-red.xml`). Updated only the two named schematic test methods in `GameAuditConsumableTests` and `GameAuditConsumableAdversarialTests`. The carriage matrix now includes the missing carried/nonconsuming positive, separately proving that eligibility requires carriage while consumption remains conditional. Refused known/missing/no-access cases now assert false and absent Study action, retaining unchanged supply assertions. No production changes.

GREEN: all115 combined legacy consumable and new/existing schematic cases pass (`iteration09-legacy-pins-green.xml`). Final native integration must include both GameAuditConsumable fixtures so these pins cannot be silently omitted. 🔵 Self-review: all other consumable policy/assertions remain unchanged; ordinary and authorized schematic counterchecks retained.

## Final integration closure

Final native Unity integration passed **1,606/1,606** selected cases in job
`e80e1a3999bb4459883d4c3902c5c0fe`, including every fixture named above and the
updated legacy consumable pins. Raw: `Verification/SystemDepthII/native-final-integration.xml`.
Actual Play passed **14/14**, zero unexpected errors, run
`78c52afbb3da4f72940713ffabe0a4fd`; eight original-resolution captures were
visually reviewed. This closes the historical pending native/Play notes above.
See `SYSTEM-DEPTH-TEN-ITERATIONS-II.md` for the retained first-run failures,
subsequent fixes, arranged-fixture bounds and requested Deck package status.
No physical Steam Deck test or whole-project test claim.
