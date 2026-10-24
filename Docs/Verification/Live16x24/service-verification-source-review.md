# Paid kitchen service — bounded source verification

2026-10-24. Read-only source review of `Docs/GRAPHICS-G3-KITCHEN-SERVICE.md`
while root owns the ordinary Play session. No runtime/art edits, Unity calls,
test execution or gameplay mutation. The route remains source-correct, subject
to its stated current-stock, traversal and live-worker conditions.

## Control clarifications for the actual run

- `C` + direction opens the real world action menu. Select the displayed repair,
  harvest, batch or Open action; arrows/Return avoid shortcut ambiguity.
- The batch needs2 raw **Emberwheat**,1 **ClaspbeanPulp**,2 drams, a repaired pan,
  current friendly Cook within2 cells, player within1, and both trays empty.
  Seed/toasted-grain substitutions do not qualify. An unavailable batch action
  is absent, so inspect the pan's recipe and the real ingredients first.
- A row harvested through `FieldHarvestService` packs its yield if capacity
  permits; overflow stays on the actual cut row. Check custody, not only the
  success message. Each authored glade ripe row has1 grain.
- Select the actual pickup meal row and Return. The last successful take closes
  the pickup popup and pays a turn; `ClosePickup` then reaches ordinary batch
  reconciliation. If other contents remain, close the popup before expecting
  the next turn boundary. Do not take escrow ingredients during the happy path.
- Pickup-popup F1 is **text-only**. For the native carried meal portrait, use
  `I` → `Tab` to carried inventory → select the meal → `F1`, or Enter → Examine.
  Inventory initially opens Equipment; Right is not a reliable one-step tab
  switch because it can navigate equipment slots instead.
- The inventory action popup uses positional letters. Select the displayed
  **Eat** row with arrows/Return; do not assume the part's `e` hotkey is the
  popup binding. Enter on the inventory row first opens the action menu.
  Successful eating consumes one real unit, closes inventory and pays one turn.
- 120 ticks is approximately12 normal-speed actions; the start action already
  advances time. Leave popup/Look state before ordinary `.` waits. Observe the
  actual countdown/Ready result instead of injecting time or assuming12 presses.

Sources: `KitchenBatchPart.cs:135–180`; `FieldHarvestService.cs:9–55`;
`InputHandler.cs:1242–1243,1943–1948,3347–3366,3419–3455,4053–4056`;
`PickupUI.cs:188–224`; `InputHandler.Clarity.cs:37–47`;
`InventoryUI.cs:270,635–654,747–750,1181,1299–1304,1474–1485`;
`SelfConsumptionTurnProof.cs:26–48`.

## Read-only receipt fields

Record plain public fields/current object relationships. Do not call
`TryStart`, `Reconcile`, `Configure`, transfer, repair, creation, save/load or
clock methods from the capture helper. Do not fire gameplay events to get a
receipt. `Describe()`/`CanStart()` are documented pure, but neither is needed
when recording the fields below.

| Group | Fields needed |
| --- | --- |
| Context | label/UTC, seed, current zone ID, current tick, flat mode, screenshot filename, player ID/position/HP/maxHP; current graph and canonical cell identity booleans |
| Current service owners | actual pan/Cook/escrow/pickup IDs and positions; expected saved IDs; exact reference matches; owned parts and ground membership; repaired flag/recipe; Cook HP and pan distance |
| Payment | player and Cook `TradeSystem.GetDrams` immediately before/after starting; optional Cook `LastRestockTurn` for arrival provenance |
| Job | `Configured`, `ZoneID`, `StationID`, `WorkerID`, `EscrowID`, `PickupID`, `State`, `BlockedReason`, `JobOrdinal`, `StartTick`, `DueTick`, `CommissionerID`; Commissioner reference matches current player |
| Inputs | each parallel `Inputs[i]`, `InputIDs[i]`, `InputCounts[i]`; current ID/blueprint/stack count; exact reference occurrence in escrow; `Physics.InInventory == Escrow`, spatial-zone null, equipped null, owned Physics/Stacker |
| Containers | owned Container reference, lock/capacity, actual content rows with ID/blueprint/count and custody; distinguish physical row count from ingredient units |
| Output | `OutputID`, actual Output ID/reference, blueprint/count/owned FieldMealPart; exact reference occurrence in pickup, inventory custody, equipped/spatial state; `KitchenBatch.Station`, `.Customer`, `.Ordinal` provenance |
| Carried items | current inventory rows, IDs/counts/custody, including raw grain, pulp, clay and meal; reference and same-ID occurrence counts if a discrepancy appears |

The Cook's original35 drams is **not** the payment baseline: ordinary zone
arrival invokes `TraderRestockSystem.RestockZone` and may update his purse.
Capture immediately before commissioning, then require player−2/Cook+2.
The checkpoint player's50→48 expectation applies only if no other spending
occurred. Source: `InputHandler.cs:1205`; `TraderRestockSystem.cs:48–102`;
`TradeSystem.cs:20,101–104`; `KitchenBatchPart.cs:218–221`.

Do not require the escrow pulp to retain carried ID7227. Splitting a larger
stack creates a separate actual owner; `Inputs` records that committed owner.
Two grain units may occupy one or two rows, so the full recipe has2 or3 physical
escrow rows. Expected quantities are always2 grain+1 pulp.

## Expected comparisons and honesty limits

1. **Before → Working:** successful start increments ordinal once, subtracts
   ingredients from the pack into exact escrow custody, charges2 drams once,
   and records due=start+120. No Output yet. Neither reader opening nor a failed
   start should spend anything.
2. **Working → Ready:** at an ordinary reconciliation boundary at/after due,
   exact inputs leave escrow, one actual FieldMeal enters pickup, Output and
   provenance agree. Do not infer completion merely from due time; unavailable
   worker or blocked pickup can keep the job waiting.
3. **Ready → Collected:** retain the pre-take Output ID. After successful take
   and normal popup closure it is the carried meal; pickup/escrow are empty and
   the batch is Idle. `ResetJob` clears Output/Commissioner/start/due/input lists
   but **retains JobOrdinal**. The carried meal keeps its provenance. Historical
   `OwnsCommissionedMeal` must not be used alone as proof it remains on the tray.
4. **Save → load:** compare persistent IDs, role relations, custody, counts,
   provenance, repair, Idle state/ordinal and money. Managed object references
   cannot be compared across deserialization; compare restored relationships
   within each graph. No claim of unchanged array order is needed.
5. **Eat:** one carried unit disappears. Full HP with no ordinary bleed proves
   consumption only, not positive healing or bleed removal. No intentional
   injury/status grant is needed for this gate.

Existing tray frames remain deliberately neutral; pair world screenshots with
the real pan reader and actual pickup row. This audit does not accept new state
art, a paid service execution, save persistence or meal use before root observes
them. No new gameplay or broader mechanics audit is proposed.

Main review: `/root/projection_cold_review`. Independent controls review:
`/root/projection_cold_review/look_focus_draft_review`,2026-10-24. No material
false route assumption found; the input/purse/identity clarifications above are
the relevant execution cautions. Only this evidence note was written.
