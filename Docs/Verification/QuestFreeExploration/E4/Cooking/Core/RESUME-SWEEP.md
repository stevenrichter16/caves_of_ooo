# F10 resumed source/usefulness checkpoint — private, 2026-09-28

Production and shared tests remain on hold until F12 integration. No new test run, Unity, gameplay, asset, or Git operation is part of this checkpoint. Existing private tests remain30 =3 controls PASS/27 RED (24 missing opt-in API and3 current post-factory source-authority failures), with actual-reference compile0.

## Small useful contract

Introduce one explicitly finite utility owner only in a future F10 assignment. It inherits the ordinary cooking service and transaction, accepts food the player actually carries, and saves its real heat/fuel. It creates no ingredient, loot container, currency, vessel or enemy. Keep `CampfirePart.FiniteCooking=false` as the default, including old saved parts and every existing authored Campfire/hearth/oven. Free rest remains unchanged.

The concrete reward is existing food preparation: RawMeat has `Cookable.Into=CookedMeat` and healing1d4; the result heals3d4 at the same weight2 (`Objects.json:19843–20050`). Starapple has `Cookable.Into=RoastedStarapple`, healing2d4, weight1 (`Objects.json:1557–1622`); the result heals3d4 at weight1 (`:20054` onward). Food heals only on later actual consumption (`Gameplay/Items/FoodPart.cs:49–79`), so cooking alone is not healing. Cooking transforms one whole carried stack into equal units atomically (`Gameplay/Items/CookingService.cs:19–66`) and is one ordinary paid inventory action when successful (`Presentation/UI/InventoryUI.cs:1428–1433`). No hunger/progression system is being invented.

Important ingredient correction: `RipeCropRow` yields Emberwheat (`Objects.json:29167`), but Emberwheat has no Cookable part (`:3382` onward). Do not advertise harvested F3 grain as cookable. Actual existing ingredients include ProvisionerStock's1–3 Starapples/60% RawMeat (`Data/Loot/LootTables.json:765–789`), FarmerStock55% Starapple (`:2490–2508`) and existing beast-death RawMeat rolls35/45/55% by tier (`:1241–1275`), plus established actual corpse harvest sources. These are source possibilities, not guaranteed player ownership or permission to mint food beside the site. No source availability or ordinary acquisition claim follows without an actual route witness.

## Thermal and usability boundaries

The existing Campfire starts at500°C/Fuel200 and has no initial Burning effect (`Objects.json:18368–18479`). Nonburning heated scenery receives real local EndTurn cooling (`Gameplay/Materials/MaterialSimSystem.cs:43–59,87–103`); Thermal defaults decay2% toward25 (`ThermalPart.cs:232–241`). Thus the proposed150 threshold is crossed after67 local material ticks with unchanged fuel absent outside interactions. This is a source-derived bound under specified defaults, not67 world-map/rest/offline ticks. InputHandler calls material simulation after local player turns (`Presentation/Input/InputHandler.cs:1002`).

The finite resource is stored heat over local actions. No per-cook invented fuel charge and no mandatory Burning. Fuel must still be an actual owned finite positive quantity, and Thermal an actual owned finite temperature>=150. `150` is a deliberately new inventory permission value aligned with separate ground-food recipe data; it was not the existing inventory rule. All legacy authored stations continue to work regardless of these values.

A hot nonburning initial source does not automatically emit adjacent fire damage. Do not call the low-risk cooling variant a dangerous fire encounter. The planned warmer-margin variant must use an actual independently generated hazard, or receive a separately tested deliberate Burning source contract before activation. Neither hazard nor additional food should be added merely to make a scripted acceptance pass.

## Source placement and truthful state before activation

Root owns family/version/assignment and one-utility budget. There is no ordinary Spread Campfire receipt to relocate: Campfire producers are settlement/authored content. VillageDecor requests one plus an optional second Campfire with3/7 chance (minimum1 and weighted optional roll), not an all-or-none ambient fire chance (`Data/Tables/PopulationTable.cs:1061–1071`, `:138–148`; actual placement `VillagePopulationBuilder.cs:50–78`). Preserve these town owners.

Suggested smallest layout: one unheld, non-takeable finite owner on a dry clear footprint, reachable adjacent standing cell, outside reserved/POI/arrival cells, with original critical routes and a safe bypass preserved. Pin exact source ID/parts/current anchor and the generation attempt after factory and at final acceptance. No extra salvage unless an existing exact rolled owner can supply it without changing budgets. Report selected/committed/refused separately and do not reroll missing sources. Existing retention/save ownership must keep a cooled source cooled on return.

Shared read-only station-state logic should drive Cook admission/refusal, current Examine text and scoped hot/cold representation. Today CampfirePart always flickers/crackles, CampfireEmberRenderer stores fire anchors, and LightSourceFlicker is independent of cooking state; therefore a cold finite source needs explicit suppression of hot cues and a cold form. Legacy presentation stays unchanged. Verify actual dirty invalidation across the hot→cold boundary; changing temperature alone is not proof a static batched recipe refreshes. The current four-family world cue does not include Cook; do not add misleading `C,direction cook` text when the real verb belongs to carried food's inventory menu. A small source description can name that actual flow.

## Exact output-factory authority

Current CookingService chooses a Campfire owner and later rechecks distance only (`:23,41–44,71–76`). The private tests genuinely reproduce removed/replaced CampfirePart and same-owner adjacent relocation during actual CookedMeat factory creation. Capture the selected station/zone/anchor/Physics/Campfire and finite Fuel/Thermal parts before factories; revalidate the same references, ownership, reach, opt-in and usability afterward, before ingredient mutation. Never substitute a different adjacent station. Skip an unusable finite station during initial selection so it cannot mask a usable legacy station. Preserve existing InventoryTransferSnapshot/outer rollback and callback-owned graph changes.

## Minimum next gates, after root releases F12 hold

1. Root imports existing private30 only and executes meaningful native RED; implement the small opt-in/current-source contract afterward. Do not treat24 missing-field failures as already-executed downstream thermal/save assertions.
2. Paired hot/cold/spent/legacy/malformed and real output-factory ownership, actual local cooling, old-wire/defaultfalse, and full saved heat/fuel/part identities. Existing whole-stack merge/capacity/outer transaction tests remain required neighbors.
3. One new source/data allowance and current cold/hot Examine/art/embers/light plus dirty boundary proof; finite-only, no global fire rewrite.
4. One bounded actual-key Cook opportunity with real carried food and exact consumed/produced units, then observed cooldown/refusal/return. Label any supplied staged setup. Natural source or ingredient absence is honest incomplete acceptance; no campaign bot or grants.
