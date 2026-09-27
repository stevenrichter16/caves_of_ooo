# M3 equipment comparison — readiness and corrections

Scope: a free, factual inventory reader using current equipment identities. No simulation, temporary equip, RNG, actor stat projection, world-reader/layout edits, content changes, save schema, or new equip path. Work starts privately; root owns publication and native verification.

## Verified source corrections before implementation

- `EquipPlanner.Build` and its rules are read-only with respect to the actor: temporary plans contain concrete claimed body parts and every displaced item/body-part pair. Auto selection prefers an empty slot, then the first occupied slot. Targeted plans are needed to make left/right alternatives explicit. Displacing one hand of a two-hand weapon releases both actual slots.
- `InventoryPart.Contains` alone is insufficient proof: it accepts a cache entry. Comparison must also verify the candidate's Physics owner, exact current body/cache relationships and current part backlinks. A bodyless legacy actor gets the existing planner's unavailable reason, not invented body slots.
- A natural attack is `BodyPart._DefaultBehavior`, not an inventory item or an empty worn slot. It may lack the Item tag. Reuse the item-mechanics formatter through an equipment-only entry point rather than fabricating inventory ownership for fists.
- Item weight is `HandlingService.GetWeight` per unit. Inventory total weight includes stack quantity; comparison describes one equipped unit and must say so. Current item fields already include directly applied item modifiers; enhancement descriptions stay conditional and are never added a second time.
- `InventoryUI` already sends free inspection text through `MessageLog.AddAnnouncement`; M2 keeps that reader contract. M3 adds one comparison action and regenerates its text when selected. Actual equip remains its existing command with current validation and veto callbacks.

## Intended acceptance

Core: candidate/current mechanics; auto and distinct targeted choices; all slots displaced by multi-hand items; empty versus natural attack; incompatible and bodyless reasons; conditional/unknown effects; exact live ownership/backlinks; read purity and stale selection. Native UI: real menu action, existing reader, free open/close, live changes and refusals, actual equip still separately revalidates. Native keyboard readability remains root's final gate.

A comparison is an inspection snapshot, not an equip guarantee: later changes and BeforeEquip vetoes still apply. No aggregate AV/DV, DPS or better-item score. Unmodeled effects are explicitly not totaled. No runtime persistent state is introduced.

## Evidence

Initial and counterexample RED receipts are preserved below. Current actual native result: comparison46/46 (24 core,14 adversarial,8 inventory) in `../reader-comparison-green.xml.gz`; existing inventory examination15/15 in `../source-art-save-paging-01.xml.gz`. Standalone core runner remains only a precheck for native UI behavior.

## Implemented privately / verification checkpoint

- New `EquipmentComparisonService` verifies current inventory, Physics, part and Body/cache identities, then uses the real planner for auto and distinct targeted choices. It lists all displaced slots and renders each item once. Current natural-hand admission follows the existing combat resolver, including shield fallback and suppression on a supporting two-hand slot. Bodyless legacy equipment explicitly labels **slot comparison** unavailable; legacy equip remains usable.
- `ItemExamineService.TryDescribeEquipmentDetails` reuses its existing factual weapon/armor formatter for equipment and natural attacks. Effective slots/conditional equip bonuses also cover nonweapon wearables. Existing item examination retains flavor/tonic/liquid integration.
- Private InventoryUI change adds one **Compare equipment** action for equippables, computes fresh text on selection and queues the existing free reader. Rejected stale owners stay in the menu with a reason. It neither equips nor closes/pays an everyday action. Actual equip remains command-owned.

Evidence: initial core24 missing-service assertion RED →24 GREEN. Dedicated adversarial12 exposed two foreign natural-weapon owner cases (34/36 passed before guard); after repair36/36 GREEN. Matched runner sweep with the existing69 examination cases: before69/105, candidate105/105; newly failing0, newly passing36. Current actual Unity-reference runtime and test compilation both have0 errors. The root executed native menu8 against old production:7 intended absent-action RED and1 non-equippable control PASS, recorded in `../M2/native-red.json` (job cca9dc475ce9422890af775063b6cf20).

Cold-eye: standalone agent confirmed the planner/ownership/natural-slot direction and identified misleading bodyless wording, now corrected with legacy-equip positive control. Core adversarial probes cover cross-owner/stale graphs, shared natural attacks, multi-slot displacement, same-name different identities, malformed bonus parsing, extreme speed values, and event/equipment/quantity/stat purity. No persistent state is introduced, so no new save roundtrip contract exists. Source and tests do not prove native keyboard readability or native modal operation: at this historical checkpoint native execution was pending; the completed native/UI gate is recorded below.

Q1–Q4: same existing planner and mechanics formatter; no new equip path; positive/negative ownership, natural and displacement pairs; scoped claims remain item contributions and an inspection snapshot. Conditional enhancement descriptions are shown as conditional, never added again to item values. Arbitrary event-defined effects have no predicted totals. Candidate exact8-path publication manifest is `candidate-manifest.json`; native8 fixture/meta were separately published by root before implementation.

Renderer's independent UI diff review found no concrete blocker: only the item-menu action and fresh free-reader branch change; actual equip, input routing and AnnouncementUI remain unchanged. The subsequent actual native8 gate is recorded below.

## Native integration finding

The first integrated native run (233 cases:226 PASS,7 FAIL) exposed a real InventoryUI initialization error in all seven comparison-positive cases: the new row was added through `_itemActionPopup.Actions` before the popup existed. `OpenItemActionPopup` builds the local `actions` list and constructs its popup afterward. Core review and compile did not catch this; native assertions remain unchanged. Exact repair changes only that receiver to `actions.Add`. The one-file delta is `/tmp/coo-first-hour-m3/menu-initialization-manifest.json`; Unity-reference runtime compile0errors. Root's `../focused-integration-01.json` preserves the actual failing run. The repaired eight inventory cases subsequently passed; see final checkpoint below.

## Final native and callback correction

The repaired comparison menu passed all eight actual native inventory cases in `../focused-integration-02-red.json` (other E1 art cases were intentionally RED). Two new pure counterexamples then confirmed a description callback could remove the candidate or current equipment before stale comparison text was accepted. The private final revalidation rejects both without equipping or repairing anything: 107/107 matched core/neighbour cases pass, full current-runtime reference compilation has zero errors, and independent source reread is clear. The published two-path delta is `description-callback-manifest.json`; actual native `../reader-comparison-green.xml.gz` includes both new adversarial cases, all14 adversarial PASS.

One old native neighbour case removed only Dagger's MeleeWeapon and expected no details. M3 deliberately supports actual Equippable slot facts, so that premise is obsolete. Its private one-file correction removes both supported parts for the unsupported control and adds a positive remaining-slot-facts control. No production behavior changes for this correction; all15 actual native inventory examination cases pass in `../source-art-save-paging-01.xml.gz`.

## Current user-facing closure

Root and M2 owner viewed all11 actual UI frames from run `77cf480c9c004615a8b078373f3cf7ea`; `../M2/native-ui-visual-review.json` records complete comparison first/last pages, free return flow, and zero out-of-viewport measured glyphs. This was an explicitly staged carried ShortSword inspection with real native keys, not naturally acquired gear or an equip/balance claim. Current M3 code is published; actual comparison46 and inspection15 native cases pass. No M3 pending implementation remains.
