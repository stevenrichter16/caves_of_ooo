# GA03g final callback/receipt repair — independent source review

2026-09-06. Read-only review of final LoadoutPart, AutoEquipCommand and EquipCommand diffs, updated GameAuditLoadoutLifecycleTests/GameAuditLoadoutAdversarialTests, current forced cleanup/transaction consumers and the existing plan. No Assets/Docs changes, Unity calls or test execution. Root reports final RED48 = 40 PASS / 8 FAIL before the repair; the current focused/native compilation run was pending at this review.

## Verdict

No new production must-fix found in the bounded final source. The earlier cached-plan and late-enhancement findings are correctly addressed without rolling back independently completed forced removal. The four diagnostic outcomes reflect command completion and current equipment ownership, and the other-free-hand positive control protects legitimate replanning.

One small test-validity correction remains recommended before final gate: the BeforeEquip detachment fixture still relies on an assertion that the executor can swallow. This does not invalidate the root-reported earlier RED, but it leaves the current regression test able to pass without proving its intended mutation.

## Remaining concrete test correction

**File:** `Assets/Tests/EditMode/Gameplay/Entities/GameAuditLoadoutAdversarialTests.cs:72–82`, `AutoEquipRevalidatesDestinationAfterBeforeHook("detached")`.

The hook currently invokes `Assert.IsTrue(actor.GetPart<Body>().Dismember(feet))` inside BeforeEquip. InventoryCommandExecutor catches an assertion exception and converts it into equip refusal. If Dismember ever returns false, the outer grant is carried, Feet._Equipped can remain null, and Speed.Penalty can still equal MobilityPenalty—all the current outside assertions can pass while Feet remains attached. Occupied injection has already been corrected by capturing `injectedEquipped` and asserting it outside the hook; the AfterEquip-removal test likewise records `removed` and asserts it outside.

**Small correction:** record `detachedSuccess = body.Dismember(feet)` inside the hook, then assert it outside after `f.Create()`. Also assert Feet.ParentPart is null and the exact Feet reference is absent from the attached Body.GetParts list. Keep the unchanged positive case. No production behavior change is needed for this correction.

## Resolved findings and why the fixes hold

1. **BeforeEquip stale plan:** AutoEquip retains its preflight ownership/stack/body/no-displacement checks but no longer passes a cached EquipPlan into ExecuteInternal. EquipCommand now calls Planner.Build after BeforeEquip. All remaining internal callers use the new signature; the unused optional-plan plumbing is gone. The fresh plan still enforces `allowDisplacements=false` and current attached anatomy. The sole-Feet occupied/detached fixtures target actual invalidation, while `ChangedPlanUsesAnotherFreeHandWithoutDisplacingIndependentGear` requires actual independent Buckler success plus equipped Dagger on the remaining free hand. Thus the repair does not implement a blanket callback refusal.
2. **AfterEquip forced removal:** EquipCommand checks current `InventorySystem.IsEquipped(actor,itemToEquip)` after AfterEquip. When GA03f cleanup removed the item, it returns Ok before enhancement dispatch. The executor commits and discards the outer undo list, preserving the already completed forced inverse instead of subtracting generic bonuses twice. The paired test supplies actual GlowQuartz plus configured Agility:2, asserts the mutation outside the hook, exact carried/equipped aliases, Agility14/16, AppliedBonus false/true and radius0/2. Both stale activation and naive late rollback are observable failures.
3. **Result receipt truth:** LoadoutPart combines Body presence, command completion and current inventory equipment membership. Outcomes are exactly `missing_body`, `auto_equip_refused`, `equipped`, and `removed_during_equip`. The removed outcome is a completed equip followed by independently completed removal, not a failed equip attempt requiring rollback. The payload includes actual item BlueprintName, bool equipped and reason; actor and target use exact refs. Tests constrain all four branches with actor/target-filtered records and blueprint/bool/reason checks. Carry-only control confirms no equip-attempt receipt is fabricated.
4. **Grant and scope preservation:** AddObject remains outside the equipment transaction, retaining the grant on refused equip. No-Body loadouts stay carried despite AutoEquip's general legacy support. Chance/count/Pick and replay/save fields are untouched. No new world content, global equipment lifecycle relocation, or broad receipt retrofit has been introduced.

## Qud / classification confirmation

The earlier local source comparison remains valid: Qud's GameObject.AutoEquip and BodyPart.Equip route through CommandEquipObject; Inventory's PerformEquip publishes EquippedEvent and EquipperEquippedEvent after storage. CoO's corrected loadout now reaches its equivalent normal lifecycle. This is architectural alignment with intentional CoO policies, not exact Qud parity: existing-owner-only/no-displacement/multi-unit-refusal/no-Body-carry and Equip/Carry/Pick grammar remain CoO contracts.

## Scope and honesty bounds

* `InventorySystem.IsEquipped` is the established current cache identity predicate. Supported forced cleanup clears cache, body and Physics consistently; tests verify all three. This guard does not repair arbitrary raw alias corruption or an observer that edits internal dictionaries inconsistently.
* AfterEquip callbacks that independently remove equipment and return normally are covered. An observer that removes equipment and then throws can still enter the command's older exception rollback boundary; arbitrary callback exceptions/partial enhancement mutations are not claimed solved here. Keep the recorded A41 debt separate.
* The fresh body plan does not purport to solve every possible mutation during later ordinary displacement hooks. The newly repaired AutoEquip route forbids displacement, so that later branch is outside this no-displacement callback fix.
* Initial and dedicated tests are fixture-blueprint authoring/API coverage. Current shipped Resources content has no Loadout producer; do not claim an ordinary existing enemy-spawn or loot-economy repair. A native scenario should keep its fixture label.
* No per-frame responsibility is added. This review supports no visual, balance, speedup, zero-allocation, scheduler, or complete death-drop claim. Root owns executed focused/native/full results and final document log updates.

## Root execution after source review

The detachment fixture now captures success and asserts it outside the caught
callback, including ParentPart null and exclusion from the attached tree. All3
affected rows passed. Focused406GREEN; native24PASS/0unexpected and complete
teardown; full9417GREEN/0CS. Historical review execution status above is preserved.
