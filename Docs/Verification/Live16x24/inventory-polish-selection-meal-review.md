# Independent cold review: inventory selection and meal polish

Read-only review of the pending diff against HEAD24491663, 2026-10-25. Scope: InventoryUI.Rebuild/ClampCursor, ItemTacticalUseDescription shared suffix, authored FieldMeal category, InventoryLedgerInteractionTests additions and ItemUtility35GuidanceTests additions. No Unity execution or code changes. Craft-feedback changes excluded.

**Verdict: no significant new correctness or ownership regression found.** Root reports selection RED24/6 failing, guidance RED41/2 failing and GREEN104/104; these counts are supplied execution evidence, not independently rerun here. Corrected ordinary-play screenshots remain a separate acceptance gate.

## Selection and authority

- `InventoryUI.cs:1840`: capture the exact selected Entity before rebuilding rows; resolve it by ReferenceEquals in the newly built filtered rows. This prevents spending/removing earlier rows from silently replacing the selection with an adjacent lookalike. Removed/replaced references cannot retain their old title by numerical row position.
- `InventoryUI.cs:2290`: the fallback chooses the first actual non-header item, resets empty-list scroll, clamps negative scroll and brings the cursor into the old navigation window. The retained painter subsequently clamps against its actual LedgerListRows capacity (`InventoryUI.LedgerView.cs:237`), so the differing legacy navigation window does not leave the selected row unpainted.
- Open still starts on Equipment. It resets cursor0 before Rebuild; row0 in a non-empty previous list is a category header, so old inventory selection is not inadvertently resurrected on reopen. Exact item-action reopening still resolves the requested object after Rebuild.
- Search already tracks the exact current and return subject in `InventoryUI.Clarity.cs:164–185`; this diff is compatible with query rebuilding, zero results, accepting a match and clearing a filter. While typing, retained subject/document publication remains suppressed. Existing current-owner checks in SelectedDecisionPresentation and LedgerSelectionPresentation remain unchanged.
- The seven new interaction cases exercise keyboard Tab, native-pad Y and painted tab entry; exact owner after earlier-row removal; removed selected owner; empty→nonempty pack; and search no-match→match. Assertions include actual retained selection stripe, document subject/currentness, title and no pending action/turn. Repeated repaint must reuse the inspection snapshot. No test appears to rely on only a string mirror of the implementation.
- The additional exact-reference search is bounded by pack rows during existing Rebuild, not added to per-frame painting. No extra item command, turn payment or world mutation is introduced.

## Meal content and behavior

- `ItemTacticalUseDescription.cs:72`: dropping the generic promise that throwing remains available removes an unsupported claim without changing handling or actions. Actual throw capability continues to be described by the handling service. Sharing restrictions, own-action cost and self-use text remain.
- `Objects.json:54797`: only Physics.Category=Food is added. Parsed before/after comparison independently confirms **712 blueprints in both versions, only FieldMeal changed**. Inheritance stays Item, and FieldMealPart remains the sole Eat handler; no FoodPart or second generic Eat route is introduced. Category is consumed by inventory display/sorting; no gameplay category-dependent behavior was found in the scoped source search.
- Guidance tests independently assert actual nonthrowability/no Throw action for FieldMeal, a genuinely throwable DriedMeat control, custom Eat uniqueness (including removal/restoration of FieldMealPart), ordinary FoodPart food control and an unrelated weapon category. The category reaches both authored Physics and InventoryScreenData grouping. These counterchecks meaningfully protect behavior rather than merely matching prose.

## Reviewed file hashes

- InventoryUI.cs: d8433df3b404b76fdf5c65835e63d82fa62634b486f35643bbe67e72d5123ac9
- ItemTacticalUseDescription.cs: f21ec16df8f467f203631fd7d73d85d01c3c919b53a08103e59c260f45280c96
- InventoryLedgerInteractionTests.cs: 1295039b3942386439b467c1912576547727efe901ed37f830417312708a8e27
- ItemUtility35GuidanceTests.cs: 30cb341aea2963a58aaf6e9d66ac3ba0b2ca6d2ef82d4b44fd6c9ad19c5fbd05

No additional scope or blocking change recommended. This review does not cover physical Deck controls, final screenshot truth, whole-save migration or concurrently authored craft-feedback work.
