# Movement test contract review

Status: six stale expectations reproduced in both standalone and native integration; test-only correction complete and standalone/native GREEN. No movement production change is warranted.

## Source and history sweep

- The old multi-cell audit deliberately preserved `BeforeMove` bypass while adding body-edge entry dispatch (`Docs/MULTI-CELL-PILOT-INTEGRATION-AUDIT.md:324–350`). Its six tests combined two then-valid contracts.
- Later commit `766289497` explicitly changed **all three** voluntary skills — Vault, Disengage and Charging Strike — from `ForceMoveTo` to `TryMoveTo`. `Docs/FIFTY-IMPROVEMENTS-COMBAT.md:38–42,171` explains the intended rooted/voluntary permission fix. The later II Vault commit adds poise, not this movement-gate change.
- `FiftyCombatMovementTests.RootedActorCannotUseVoluntaryMobilityButUnrootedControlCan` already tests the current rule, including the unchanged positive controls. Adjacent rooted Charge can still attack without moving, and external forced displacement can still move a rooted victim.
- The six original multi-cell tests set a veto and demanded success. Native integration reproduced the same failures; the isolated pre-Frostbind baseline `iteration07-legacy-movement-baseline.xml` also reproduces them. This is historical test drift, not a new spell/companion regression.

## Bounded correction

Keep the six unvetoed body-edge entry and exact movement-event-count assertions. Add six separately named veto-refusal cases for each skill and single/body-sized actor: no movement, no entry/AfterMove, no attacking the still-distant charge target or granting Vault poise. Remove the veto within each case and require successful movement as a positive control. Keep archive-crossing, occupancy and callback interruption tests unchanged. No production code is edited.

Root executed the final native Unity run; both standalone and native results are recorded below.

## Verification and review

- `review-movement-contract-green.xml`: 77/77 standalone, comprising 62 multi-cell consumer cases (56 prior plus six distinct refusal cases) and 15 newer FiftyCombatMovement cases.
- The original native and standalone six-failure receipts remain historical evidence; they were not overwritten or relabelled. The corrected expectations now pass in the final native run below.
- Independent rendering/navigation-agent review verified the exact `766289497` changes and all retained positive assertions. No production rollback or weakened movement contract is warranted.
- Self-review: 🔵 retain body-edge/remote counterchecks, archive boundary, step counts and callback interruption while testing voluntary permission separately; 🔵 final native EditMode passed. This is an authorized test contract repair outside the ten production iterations, not an additional mechanic.

- Final native Unity EditMode: [native-final-integration.xml](native-final-integration.xml), job `941ad3a3628542c19f5fda2b0a9e49b4`, **810/810 passed, 0 failed, 0 skipped**. All 62 MultiCellAbilityConsumer and 15 FiftyCombatMovement cases passed (77 relevant cases). No movement production change was required.
