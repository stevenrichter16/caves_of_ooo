# Iteration 3: discoverable compost supply

Status: implemented; standalone and native payment/input checks GREEN; live review pending. CoO extension, no Qud parity claim.

Add two inert sludge units to the ordinary SeedKeeper and Morrowfast provisioner
stock. Item examination and the existing allotment notice explain buying sludge,
planting a crop, composting once before wet growth, and still needing water.
The existing preparation transaction owns the exact one-unit payment and saved
25% shorter stage duration. No new scenery, recipe or alchemy rule is needed.

## Verification sweep / corrections

| Premise | Current evidence | Decision |
|---|---|---|
| Failed brewing supplies compost | All authored property unions have a brew or mishap result; no ordinary loot emits InertSludge | Supply finite trader stock; do not distort existing brew rules. |
| Existing CompostCache might supply sludge | Its Harvestable yields GoldCoin; CompostRow is scenery | Preserve both existing objects. |
| Add a new harvest owner | Ordinary seed sellers already provide the cultivation supply route and existing sludge art | Use their bounded inventory and keep allotment placement unchanged (root approved). |
| Table change alone reaches every provisioner | MorrowfastContent keeps a factory-less opening-stock fallback | Update fallback quantities and existing exact stock regression together. |
| Crops only grow while acting locally | CropTime reconciles elapsed world ticks and finite moisture | Correct the notice; dry plants still pause. |

## Acceptance / evidence

Actual authored merchant creation, exact buy price and ownership, real seed
planting and compost command, single unit and single committed callback,
repeat/ownership/veto rollback, binary persistence, and no free water/growth.
`iteration03-red.xml`: 27 cases, 27 failed for absent supply or clue. These are
content-precondition failures, not 27 separate crop bugs. `iteration03-green.xml`:
111 cases, 110 passed, 0 failed, 1 native-only visual skip. It includes all 27
new compost cases (21 dedicated adversarial), 31 new restock, 28 key, 18 existing
Morrowfast quantity tests and 7 existing trader content tests. The initial
compost-only GREEN was 27/27 before broadening to the related sources.

`iteration03-content-diff.json`: only InertSludge changes in Objects.json, with
all pre-existing parts preserved and one examination part added. Only
MorrowfastProvisionerStock and SeedKeeperStock change in the table file; removing
the new sludge row reproduces each old Entries list exactly.

The four-case `CompostSupplyInputTests` fixture buys actual stock, uses native
controller confirmation in the inventory popup to plant, advances the real
scheduler through InputHandler.Update, then selects the real compost world
command. It asserts still-dry zero growth/remainder after planting, exactly one
paid compost action, and free stale/before/after rejection. All four passed in
parent-owned native execution. These are synthetic native input checks, not a
claim about real-time input cadence, rendered appearance, play feel or physical
Deck operation.

Native integration correction: `native-first-integration.xml` records all 27
core compost cases passing, but all four input cases stopped before the planting
confirmation paid a turn (tick stayed 17). Manual EditMode input updates do not
advance `Time.time`; InputHandler's repeat-delay gate precedes inventory handling.
The fixture now expires its input timers for each synthetic event, matching the
existing `QudControllerGameplayTests` seam. It still queues real gamepad events
and calls InputHandler.Update, with added checks that neutral release retains the
popup, no crop exists before A, and A closes inventory. No production timing or
transaction code changed.

Final native job `941ad3a3628542c19f5fda2b0a9e49b4`, recorded in
`native-final-integration.xml`, passed 810/810 with no skips. Direct XML checks
confirm all 27 core compost cases and all 4 native input cases passed. The input
cases retain the actual gamepad A → inventory confirmation → InputHandler turn
payment route, with exact energy accounting and zero dry growth after planting;
valid compost pays once, while stale, vetoed and rolled-back actions pay nothing.

## Review / files

🔵 Self-review: current seller stock, table stock, factory-less stock and the
notice agree. Original seeds/items and all 13 allotment owners are retained.
Purchased units are ordinary inventory objects; no new grants, hidden cache or
save migration. Positive compost tests assert exact ownership/payment and one
committed callback; failures assert no further consumption and unchanged crop
flags/duration. Serialization preserves one-time state and spent supply.
⚪ Scope refinement: use existing shops rather than adding a scenery harvest
owner. Each opening/refill roll supplies two units, with existing restock rules.
Old saved inventories/sign text are preserved; future normal refill supplies the
new stock and the item itself carries the actionable clue.
🧪 The source-only brew-property census motivated supply selection; it is not
claimed as an executed native proof of every possible brewing input.

Changed files:
- `Assets/Resources/Content/Data/Loot/LootTables.json`: two finite stock rows.
- `Assets/Resources/Content/Blueprints/Objects.json`: InertSludge usage clue.
- `Assets/Scripts/Gameplay/World/MorrowfastContent.cs`: matching fallback stock.
- `Assets/Scripts/Gameplay/World/RepairCultivationSite.cs`: actionable notice and
  correction of the old local-only growth claim; placement is unchanged.
- `Assets/Tests/EditMode/Gameplay/World/MorrowfastMerchantRestockTests.cs`:
  retain exact quantity checks with the intentional two-unit addition.
- New `CompostSupplyTests.cs`, `CompostSupplyAdversarialTests.cs` under Gameplay/
  Preparation, and `CompostSupplyInputTests.cs` under Presentation/Input, plus metadata.
- This log, RED/GREEN receipts and parsed content diff proof.
