# F10 grain and exact cooking-source core — private review receipt

This is a bounded core candidate, not an activated exploration family. No shared Assets, Unity, Git, or root living documents were changed by this package. Root owns publication and native execution.

## Outcome

`CampfirePart.FiniteCooking` is a public saved bool defaulting to false. Established authored stations (including ordinary Campfires with Fuel200, cold stations and thermalless Morrowfast additions) retain their heat-independent cooking and rest policy. An opted-in finite source needs its actual current owned Fuel and Thermal parts, finite positive FuelMass, and temperature at least `CookingService.MinimumFiniteCookingTemperature` (150). This newly selected cooking threshold is distinct from ignition. There is no Burning requirement, cooking fuel debit, automatic ignition, refuel action or decay change.

The chosen station's exact owner, anchor, Campfire/Physics/Fuel/Thermal references and finite flag are captured before output factories. It must still occupy that same anchor, have owned parts and no carrier, and remain reachable/current/eligible before food transfer. A newly adjacent replacement station cannot substitute for it. The input inventory, Cookable, Stacker and Physics must likewise remain the original owned current parts, with exact recipe/quantity and legitimate carried membership. Refusal does not undo an independent callback's station movement or part replacement. Existing InventoryTransferSnapshot, transaction claims, rollback and AfterCommit receipts are retained.

The only parsed content changes are a Cookable recipe on `Emberwheat` and a new independent `FoodItem` child `ToastedEmberwheat`. One raw unit becomes one prepared unit, weight1 unchanged; healing2d4→3d4, authored commerce10→12. That is +1 healing die (unclamped mean5→7.5), and +2 base commerce per unit, not a quoted sale/income claim. RipeCropRow's one-unit harvest and all ingredient spawning/yield budgets are unchanged. Prepared grain has no Cookable, so it cannot retoast itself. Existing saved entities are not retrofitted.

## Executed evidence

- Initial complete30 core cases:3 PASS/27 RED before implementation;24 missing-field failures and3 actual post-factory source bugs.
- Grain+core37 unchanged-source run:4 PASS/33 RED. Six of the seven new grain cases fail for missing recipe/output; the missing-station conservation control passes.
- Callback subset6 before repair:1 unchanged callback PASS/5 real RED. The two added cases replace actual current Cookable/Stacker parts with same-valued replacements; both originally consumed incorrectly, alongside removed/replaced Campfire and same-adjacent station movement.
- Final matched39 against unchanged shared source:4 PASS/35 RED,0 errors (`final39-red.xml`). No failing output was relabeled as green.
- Final candidate39 plus97 unchanged neighboring cases:136/136 PASS,0 errors (`final-core-neighbors-green.xml`). Neighbors are DensityEverydayTests, DensityEverydayAdversarialTests, DensityEverydayIndependentReviewTests, DensityCookingReceiptReviewTests and CampfirePartTests.
- Full actual Unity-reference runtime and EditMode source assemblies compile with0 errors (`runtime-compile.log`, `tests-compile.log`). This is compiler compatibility, not native test execution.

The isolated runner uses existing presentation/Unity stubs and explicit patched manager/hash copies, recorded in `core-source-inputs.json`. Full graph roundtrips use the actual core serializer in that runtime. The legacy zero-field CampfirePart wire is constructed in the current runtime; it is not historical Unity binary compatibility evidence. Native39 RED→GREEN and the affected neighbors remain root gates.

## Q1 — Symmetry

Read the unchanged transfer/rollback path beside the new admission checks. All new refusal checks run before InventoryTransferSnapshot captures/applies writes. No new success record is emitted early. Both finite and legacy sources use the same exact current owner/part/anchor proof; only the deliberate heat/fuel gate differs. Existing independent-drop-after-refusal and outer-transaction rollback controls pass.

## Q2 — Cross-feature consistency

The public opt-in is false for old serialized data and ordinary blueprint construction. The named temperature constant is available for later finite source presentation; no second readiness service is introduced. Cooking remains preparation, not healing; Eat consumes one prepared unit through existing Food behavior. Rest actions and pure rest-clock behavior remain unchanged. Ground material recipes remain independent.

## Q3 — Counter-checks

Paired coverage includes150/149.99, fueled/spent, missing/nonfinite parts, finite/legacy, thermalless, unchanged/replaced/moved source, same-valued replaced recipe/stack, unusable first neighbor/usable second, positive/negative saved source states, old zero-field wire, one/three-unit exact capacity, actual finite row harvest, absent station, actual Eat, no repeat recipe, and saved replacement food identity/quantity. Callback mutations survive refusal; outputs and raw quantities stay conserved. The complete neighboring cooking rollback, capacity, merging, dead-actor and old-food tests stay green.

## Q4 — Documentation and activation bounds

The earlier merchant-only raw-food recommendation is marked superseded, while its availability census is retained as historical evidence. Grain previously had no Cookable; the new transformation is an explicit content addition, not a restored old behavior.

The sole production scenery material-pass caller is InputHandler.EndTurnAndProcess:1002 after EndTurn+ProcessUntilPlayerTurn. It is not a global scheduler tick subscription. Directly executed66/67 material passes prove the default stored-heat crossing; they do not prove arrival timing, actions across zone travel or offscreen cooling. The optional .005 decay proposal is not implemented. Before a finite workspot is enabled, root must verify useful travel/approach time under actual native scheduling, current finite source placement/food access and cold persistence.

ToastedEmberwheat still needs its original portable/world art binding. No finite workspot blueprint, family version, site composition, hot/cold model, Examine wording, ember/flicker suppression or native keyboard route is included here. Existing Campfire presentation currently flickers/crackles/emits embers regardless heat, so new finite source activation must wait for truthful cold-state presentation. Existing authored station presentation must remain compatible. No grain, actor, loot or supplies are granted by this core package.

Independent peer review: render_completion read the final three-path diff and paired fixtures; no concrete blocker in station/input callback authority, saved default-false compatibility, finite boundary or1:1 grain seams. Root's bounded source review also reported no concrete blocker. Exact publication candidates are in `test-manifest.json` and `production-manifest.json`; root must recheck current preimages before adoption.
