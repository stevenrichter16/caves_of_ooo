# G3 inventory follow-up polish

Status: implemented, independently reviewed, and accepted within the bounded ordinary Unity menu replay on 2026-10-25. Two native runs cover245 distinct passing cases (233+12), zero skipped. Root owns Unity and actual gameplay verification. This follows `Docs/GRAPHICS-G3-INVENTORY-REDESIGN.md`; the accepted ledger, artwork, transaction behavior and input release fix are preserved.

## 1. Select an actual item on Inventory entry

**Confirmed cause.** `InventoryUI.Open` initializes `_cursorIndex=0` (`InventoryUI.cs:281`), while `BuildItemRows` inserts a category header first (`:1862`). `ClampCursor` (`:2283`) cannot escape a header at index zero. Keyboard Equipment→Inventory (`:650–669`), native Menu Y/Tab, and the painted tab (`InventoryUI.LedgerInput.cs:88`) can therefore show Inventory with no item highlight or inline facts. The retained view correctly treats a header as no subject; do not weaken that authority.

**Minimal seam.** In `InventoryUI.Rebuild`, capture the previously selected exact Entity reference before replacing `_rows`; after the single existing rebuild, restore that reference if still present in the filtered rows, otherwise select the first non-header actual item. Repair `ClampCursor`’s header-zero/bounds case and ensure the selected row remains in the current scroll window. Empty/no-match rows stay neutral. Do not use IDs/names, introduce description queries, automatically select a crafted output, or change Equipment’s initial focus. Keep `ReopenItemActionPopupFor`’s explicit owner selection and search restoration behavior.

**Tests first.** Extend `InventoryLedgerInteractionTests` using its existing real Rig/painted hit rectangles, with native devices isolated as in its current input cases:

1. Stock a real pack; enter Inventory by painted tab, physical Tab and native Y. Assert a real current item row, selection stripe and already-rendered item facts/title; no popup, command, energy or inventory change. These should expose the header defect before production.
2. Select a later exact owner, leave the panel, remove an earlier row and rebuild; returning must retain that owner despite its new index. Remove the selected owner and rebuild: remaining real selection, no stale prior details. Preserve these as lifecycle counters if they do not fail initially.
3. Empty pack and unmatched search remain neutral; active search does not expose details, and clearing it restores a current matching item.

Inspect cached labels rather than calling `SelectedDecisionPresentation` merely to assert the test: that can itself invoke mutable providers. Retain `ExamineLedgerInventoryTests` and the provider-removal/replacement `InventoryLedgerViewTests` in regression. Their single-snapshot behavior must not regress.

## 2. Show truthful completed craft feedback

**Confirmed cause and API correction.** `InventoryUI.Crafting.cs:358–435` clears action status, executes Forge/Brew, rebuilds and renders. Failures populate the existing status row; success does not. `InventoryCommandResult` has no produced-item/count payload. A preview or requested batch size is not evidence of completed output. `BrewReagentsCommand.Execute` already receives `produced`, `results` and `madeCount` from `BrewingService.TryBrewBatch` but discards them. Success can mean a partial batch; command-level outcomes also include sludge or a mishap without an item. The Crafting preview normally refuses non-Brew mixes, but feedback must not invent flasks from a generic Success boolean. Actual single Forge/Brew is allowed away from a station; only the applicable batches need one, despite stale command summaries.

**Smallest robust implementation.** Retain the Brew command instance and expose a small last-execution receipt of completed attempts and actual outcome counts from those existing service results (reset per execution). Prefer counts over rescanning inventory or assuming a surviving/unique output recipient. `ForgeWeaponCommand.ForgedWeapons.Count` already records produced units, including repeated references for stack merges. No broad `InventoryCommandResult` redesign or new events are required.

Use the existing `_actionStatus` context/lifetime with an explicit success-versus-failure style. The retained label at `InventoryUI.LedgerView.cs:184–187` currently always uses red; successful completion should use the existing restrained affirmative palette. Set status only after the real command succeeds; e.g. “Brewed 1 item.” / “Forged 1 weapon.” and “Brewed 1 of 2 requested items.” for a genuine short batch. Actual sludge/mishap counts must be named accurately if reached. No automatic output selection or duplicate MessageLog publication. Keep the next-recipe preview truthful and let the separate status acknowledge the completed action.

Preserve “Forged weapon; quench failed: …” rather than overwriting it with unconditional success. Do not claim the whole forged batch was quenched: current code tempers one unit. Keep failure color/reason, clipping, modal ordering and action legend unchanged. Clear completion when leaving its mode, closing, or making a new recipe choice; redraw alone must retain it and never repeat the command.

**Tests first.** Reuse `GameAuditActionFeedbackTests`/`ActionFeedbackFixture` and its real command setup plus retained label bounds:

1. Successful real single brew and forge must show a visible non-error completion in the existing status row after rebuilding, with exact ingredient/output deltas and no second action on redraw. Include output merging: report units made, not the total resulting stack size.
2. Reuse `PartialBrewSuccessDoesNotBecomeAFalseAllOrNothingFailure`’s existing callback-driven partial batch: requested two, genuinely completed one; acknowledge one, preserve committed output and the remaining dropped reagent. Full two-unit counterpart stays truthful.
3. Refused recipe remains red with no consumption; successful forge plus failed quench retains its partial-outcome failure; changing mode/recipe or closing retires old completion. Cover long status clipping and unobscured footer with the existing label bounds helper.

Intentional old pins requiring correction after RED are the craft-specific “success means empty Status” assertions in `IncompleteForgeShowsReasonAndCompletedKitClearsIt`, `OffStationBatchRefusesVisiblyWhileSingleForgeRemainsValid`, `BrewPreviewGateShowsFailureWithoutConsumingInvalidReagents`, and the partial-brew test. Preserve their gameplay assertions; do not change unrelated seed/drop/tinkering status contracts.

## Execution order and acceptance

Run selection RED → smallest selection change → focused GREEN, then craft-feedback RED → narrow command receipt/UI status → focused GREEN. Root may combine the final affected regression once: ledger interaction/view, mutable-provider inventory readers, GameAudit crafting/action-feedback and adversarial groups, BrewReagentsBatchCommandTests, ForgeCommandsTests and GameAuditCraftReceiptTests. Determine exact case counts from the frozen fixtures; no test result is claimed by this plan.

After isolated Play is available, use actual stock to capture initial Inventory entry with an item highlighted, one successful brew/forge with readable completion, and unchanged costs/output. Keep physical Steam Deck and packaged build acceptance separate. No new art, transaction rules, recipe eligibility, controls or general UI framework belong to these two slices.

## Verification log

08:03:23–25 UTC: native InventoryLedgerInteractionTests RED24,18passed,6failed,
0skipped. Three entry routes and empty→stock focus select a header; surviving
selection changes owner after earlier-row removal; removed selection stays out
of bounds. Search counter and17existing cases passed. No production changed
before these failures. The transient missing-helper compiler errors during
test authoring are not counted as behavioral RED.

## Separate meal readout correction found during ordinary kitchen Play

Actual FieldMeal reader promises throwing although its authoritative handling
refuses it. Companion-care suffix in ItemTacticalUseDescription.cs is generic;
remove unsupported promise and preserve self-use. FieldMeal is categorized as
Miscellaneous because Physics has no category. Set its explicit category to
Food without changing Item inheritance or adding FoodPart, which would compete
with FieldMealPart's atomic Eat. Tests first cover actual factory/action list,
throw refusal and throwable counter, sole Eat owner/removal, and grouping.
Edit Objects.json surgically and compare parsed blueprint objects. No gameplay
transaction, heal, throwing or recipe rule changes are authorized by this slice.

## Self-review and limits

Native GREEN, independent source reviews and actual menu replay are complete.
The storage interruption and recovery are recorded below. Current
packages predate this work; no physical Deck or whole-world graphics acceptance.

- 🟡 Fixed: initial category header left Inventory with no selected item.
- 🟡 Fixed: completed crafts had no visible acknowledgement, and a preview
  quantity would misrepresent partial batches or merged output stacks.
- 🟡 Fixed: the FieldMeal reader promised unsupported throwing; newly created
  meals now group as Food while retaining their sole custom Eat provider.
- 🧪 Open: native Menu Y input passed; physical Steam Deck and packaged UI
  have not been replayed for this follow-up. Old saved meals retain their
  serialized category; this change does not migrate saves.
- 🧪 Open: whole-scene opening composition remains the next graphics gate.

### Focused native results before final playtest

- Selection RED24:18pass/6fail. Meal RED41:39pass/2fail.
- Selection/reader/meal GREEN104/104 distinct cases,0skipped at08:06UTC.
- First craft request loaded73old compiled cases and passed. This is explicitly
  stale coverage, not RED. After refresh, current80cases returned63pass/17fail
  at08:11:46–55UTC: absent completed-output feedback, including partials.
- Receipt RED11:7pass/4fail at08:14:43–44UTC. Reflection found missing last-
  execution counters after real brew/sludge/mishap output was already checked.
- One-source parsed comparison finds712blueprints, only FieldMeal differs:
  explicit Physics.Category Food. Its custom Eat, inheritance and other data
  are unchanged. Sources and tests reviewed independently.

Implementation keeps exact Entity selection across row rebuilding, chooses
a real item for new/empty→stock selection, and keeps empty searches neutral.
Crafting uses actual Forge recipients and Brew completed outcome counters,
not preview quantities or whole merged-stack size. Completion is green and
retained until a mode/recipe/context change; failures remain red. Failed quench
retains its partial-outcome message; no command is repeated on repaint.
Mishaps are attempts without items and sludge is counted separately.

### Combined native result and storage interruption

08:16:40–08:17:05 UTC: 233/233 distinct native Unity cases passed, zero skipped.
The XML contains nine affected fixtures: BrewReagentsBatchCommandTests,
ExamineLedgerInventoryTests, both GameAuditActionFeedback fixtures,
GameAuditCraftReceiptTests, InventoryActionReleaseTests,
InventoryLedgerInteractionTests, InventoryLedgerViewTests, and
ItemUtility35GuidanceTests. The requested short ForgeCommandsTests namespace
matched no fixture; its correct Gameplay.Weaponcraft namespace still needs
the separate regression. It is not counted in 233.

Root launched SampleScene from the earned kitchen checkpoint and selected
ordinary Continue. No items, turns or presentation were injected. Final
Inventory capture then failed because the disk would not create image or
temporary files (ENOSPC despite df reporting 117 MiB). Stop Play succeeded
through Unity MCP; the restoration snippet subsequently timed out. Root sent
SIGTERM only to the owned project Editor process, recovering write capacity.
The earned after-kitchen checkpoint and earlier actual route captures remain
preserved. This is a storage interruption, not evidence of successful final
visual acceptance. Verify isolation cleanup on next launch, finish the actual
menu replay, then record the new capture and command counters.

Source reviews are preserved in `Docs/Verification/Live16x24/` as
`inventory-polish-selection-meal-review.md` and
`inventory-polish-craft-review.md`. No significant correctness regression
was found. Test XML, result summaries, parsed blueprint comparison and route
evidence accompany this commit. Changes affect six production files, five
existing test files, this living record and the graphics continuation records;
there is no new art or new gameplay transaction rule in this follow-up.


### Recovered ordinary gameplay acceptance — 2026-10-25 08:43 UTC

Root resumed SampleScene with normal Continue from the earned kitchen save,
without injecting items, time, locations or a presentation override. Five actual
1280×800 PNG/JSON pairs named `inventory-polish-*` record this replay:

- `initial`: physical I then Tab selects the actual frost-quenched sword row;
  its native portrait and current facts are already visible.
- `meal-reader`: physical search and F1 show the corrected self-use/share prose
  and authoritative “cannot be thrown” handling without a competing promise.
  This saved meal still groups as Miscellaneous; new Food grouping is native
  tested, not a migration claim.
- `before-brew` / `brew-complete`: physical B, Space, Return consumes exactly
  stoneburr6970 count1 and creates stoneskin tonic13724 count1. The green
  “Brewed 1 item.” appears after completion. Remaining items, sword/coating,
  HP42,40drams and tick5830 are unchanged.
- `return`: clicking Inventory restores the meal search/selection, clears the
  completed status and does not brew again. F5 saves the earned result.

Correctly qualified ForgeCommandsTests passed12/12 at08:31UTC. Its full test
names have no overlap with the earlier233, giving245 distinct native passes,
zero failed/skipped across those two runs. The independent gameplay review
checks actual PNGs/readouts and precise inventory deltas. Physical Deck,
latest packaged UI, long-batch feel and complete opening artwork remain open.

The post-polish checkpoint is `/tmp/coo-fresh-opening-after-inventory-polish/`.
At08:43:10, Play/isolation/pending/view-borrowed flags were cleared, the save
root returned to null, and SampleScene was restored. Normal save and backup
hashes remain unchanged. After the earlier ENOSPC restart, LastGameID still
pointed at the isolated audit; root restored the exact pre-audit preference
from the04:13 preservation record before the new session. Final restoration
verifies that same original preference. No save file was rewritten for this.

An Editor-only cleanup defect logged “Invalid index 0 7 1” while removing the
owned custom Game view size. The selected view and isolation were restored,
but the extra audit resolution may remain. This is separately queued for a
native test-first fix; do not describe this console as error-free. Metal and
UnityMCP log-import warnings are also preserved in the console notes.

Source and route work was verified on origin/main at f69e5f6a2 before deleting
the two closed-Editor PlayerDataCache platform directories. Their reported
allocation was261,492,736bytes; measured free-space gain was only9,453,568bytes.
Do not equate logical/allocation totals with APFS space recovered. A separately
audited exact stale-clone manifest identifies191 redundant raw logs (identical
compressed versions already tracked remotely) and four unused generated-cache
directories. Those candidates are not deleted until this new evidence is pushed.
