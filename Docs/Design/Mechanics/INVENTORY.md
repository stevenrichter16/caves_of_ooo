# Caves of Qud Inventory Mechanics Deep Dive

This document maps how Caves of Qud implements player and NPC inventory behavior in the decompiled source.

Focus areas:
- Item containment model (carried inventory, equipped gear, cybernetics, embedded items)
- Command and event pipelines for take/drop/equip/unequip
- Player UI paths and action menus
- NPC reequip logic and merchant/container transfer logic
- Stacks, weight, overburden, ownership/theft, restocking, persistence

---

## 1) System Architecture

At runtime, inventory is not a single list. It is a composition of subsystems:

1. **Bag inventory (`Inventory` part)**
- File: `XRL.World.Parts/Inventory.cs`
- Stores objects in `Inventory.Objects`.
- Attached to a `GameObject` as `ParentObject.Inventory`.
- Implements `IInventory`.

2. **Equipment and anatomy (`Body` + `BodyPart`)**
- Files: `XRL.World.Parts/Body.cs`, `XRL.World.Anatomy/BodyPart.cs`
- Equipped state is stored per body part (`_Equipped`, `_DefaultBehavior`, `_Cybernetics`).
- Multi-slot equipment can occupy multiple `BodyPart`s.

3. **Generic inventory target interface (`IInventory`)**
- File: `XRL.World/IInventory.cs`
- Methods for add/remove/containment and inventory location.
- Implemented by both `Inventory` and `Cell`.
- This is why drop/transfer code can target containers or map cells uniformly.

4. **Embedded/secondary item containers implemented by item parts**
- `EnergyCellSocket` contains a slotted cell.
- `MagazineAmmoLoader` contains loaded ammo.
- Many of these expose contents via `GetContentsEvent` rather than regular bag listings.

---

## 2) Core Data Model and Context States

### 2.1 Context fields
A `GameObject` item generally lives in one of these contexts:
- `CurrentCell` (on the map)
- `InInventory` (inside some object's `Inventory`)
- `Equipped` (equipped by an actor)

Most transfer operations call `RemoveFromContext(...)` before adding to new context.

### 2.2 `Inventory` part fundamentals
Key behavior in `XRL.World.Parts/Inventory.cs`:
- `Attach()` assigns `ParentObject.Inventory = this`.
- `AddObject(...)` validates item (`takeable`, not graveyard, not invalid), then:
  - appends to `Objects`
  - sets `Object.Physics.InInventory = ParentObject`
  - removes from cell if needed
  - flushes weight/transient caches
  - sends `AddedToInventoryEvent` and `EncumbranceChangedEvent`
- `RemoveObject(...)` clears `InInventory`, updates weight cache, sends encumbrance change.

### 2.3 Hidden inventory items
Most user-facing inventory list APIs exclude items with tag `HiddenInInventory`.
- `Inventory.GetObjects*` filters hidden items.
- `GetObjectsDirect*` can include direct contents without visibility filtering.

### 2.4 Equipment/cyber/default behavior storage
`BodyPart` has separate references:
- `_Equipped` for normal equipment
- `_DefaultBehavior` for natural/default equipment behavior
- `_Cybernetics` for installed implants

`GameObject.EquipAsDefaultBehavior()` only returns true for natural gear and certain allowed cases (`XRL.World/GameObject.cs`).

---

## 3) Inventory as Events (Command Pipeline)

Inventory mechanics are event-driven. The `Inventory` part registers and handles command events such as:
- `CommandGet`, `CommandGetFrom`
- `CommandTakeObject`
- `CommandRemoveObject`
- `CommandEquipObject`, `CommandForceEquipObject`
- `CommandUnequipObject`, `CommandForceUnequipObject`
- `PerformTake`, `PerformDrop`, `PerformEquip`, `PerformUnequip`

This pattern is critical:
- `Command*` does validation, permissions, prechecks, ownership prompts, etc.
- `Perform*` executes state mutation.
- `Begin*`/`BeginBeing*` events provide mod/extensibility veto points.

---

## 4) Player Pickup / Take Flow

### 4.1 UI entry
`CommandGet` / `CommandGetFrom` (`Inventory.FireEvent`) does:
1. Resolve target cell or direction.
2. Build interactable list via `GetInteractableObjects(...)`.
3. If one item and quick-pick enabled: fire `CommandTakeObject` directly.
4. Otherwise open `PickItem` dialog (`GetItemDialog`).

`CommandGetFrom` can include nearby interactables that are not directly takeable but have inventory actions (via `EquipmentAPI.CanBeTwiddled`).

### 4.2 `CommandTakeObject`
Main checks in order:
1. Prevent containment loops (`MovingIntoWouldCreateContainmentLoop`).
2. Frozen/stasis checks.
3. Source container hooks (`BeforeContentsTaken` / `AfterContentsTaken`).
4. Ownership prompts in some contexts.
5. Fire `BeginTake` on actor.
6. Fire `BeginBeingTaken` on item.
7. Fire `PerformTake`.
8. Spend energy (default 1000 unless overridden).

### 4.3 `PerformTake`
- Removes object from old context.
- Shows messaging (plain take, take from container, put-by actor context).
- Adds object with `AddObject(... NoStack:true ...)`.
- Sends `TakenEvent` and `TookEvent`.
- If stacking allowed, runs `CheckStacks()` to merge.

---

## 5) Drop Flow

Two main paths:

1. **Inventory action path** (`Inventory.HandleEvent(InventoryActionEvent)`)
- Handles `CommandDropObject` and `CommandDropAllObject`.
- Prompts for partial quantity on stack drops (unless forced/drop-all path).
- Fires `BeginDrop` / `BeginBeingDropped`.
- Fires `PerformDrop`.
- Chooses destination `IInventory` target:
  - `E.InventoryTarget` or `E.CellTarget` or actor current cell.

2. **`CommandRemoveObject` path**
- Used by trade/equip internals.
- Also drives `BeginDrop` -> `BeginBeingDropped` -> `PerformDrop`.

`PerformDrop` removes from inventory and sends `DroppedEvent`.

---

## 6) Equip Flow

### 6.1 Command-level behavior
`CommandEquipObject` / `CommandForceEquipObject` in `Inventory.FireEvent`:

1. Validate target item (takeable, not graveyard/invalid).
2. Unless forced/semi-forced, enforce mobility constraints (stuck/extremities).
3. If item isn't already in actor inventory/equipped, split-from-stack and `ReceiveObject` first.
4. Ownership warning for player if equipping non-owned external items.
5. Resolve possible slots via `QuerySlotListEvent.GetFor(...)`.
6. For player/manual equip with multiple slots, prompt slot picker.
7. Fire `BeginEquip` (actor), then `BeginBeingEquipped` (item).
8. Remove object from old context via `CommandRemoveObject` silent.
9. Fire `PerformEquip`.
10. On failure: rollback item to original cell/inventory context.
11. Spend equip energy (`1000` default, `0` for thrown-weapon slot/no current cell unless overridden).

### 6.2 `PerformEquip`
- If stack count > 1, equip one item (`RemoveOne`).
- If target body part already has equipment, first run `CommandUnequipObject` for that part.
- Calls `BodyPart.DoEquip(...)` with `UnequipOthers:true`.
- On success, emits equip messaging/sounds and sends `EquippedEvent` + `EquipperEquippedEvent`.

### 6.3 Slot query and admissibility
`QuerySlotListEvent`:
- Fired on the actor.
- `Body.HandleEvent(QuerySlotListEvent)` delegates to body tree.
- `BodyPart.ProcessQuerySlotList` asks each candidate slot whether object is equippable there via `QueryEquippableListEvent`.
- If default behavior exists in slot, `CanEquipOverDefaultBehavior` can veto replacement.

---

## 7) Unequip Flow

`CommandUnequipObject` / force variant:

1. Resolve target body part (from explicit body part or object).
2. Unless forced/semi-forced, enforce stuck/frozen constraints.
3. Fire `BeginUnequip`.
4. Fire item-side `BeginBeingUnequipped` (`BeginBeingUnequippedEvent` wrapper).
5. Fire `PerformUnequip`.
6. If still valid and no `NoTake` flag:
   - free-take item back into inventory via cached `CommandTakeObject` with energy 0, silent.
   - if item has `DestroyWhenUnequipped`, destroy instead.

`PerformUnequip` calls `BodyPart.Unequip()` to clear all slots occupied by that equipped object.

---

## 8) Anatomy Slot Mechanics (Deep Rules)

Most equip complexity is in `BodyPart.DoEquip(...)`.

### 8.1 Multi-slot and laterality from `UsesSlots`
If item has `UsesSlots`:
- Parse comma-delimited slot descriptors.
- Laterality adjectives are parsed (left/right/etc).
- Ensure required free slots exist beyond the initiating slot.
- If `UnequipOthers` enabled, try to free required slots by unequipping current occupants.
- On failure, returns explicit failure messages like needing additional slots.

### 8.2 Dynamic slot requirement (`GetSlotsRequiredFor`)
For items without explicit `UsesSlots`, slot count can be dynamic via `GetSlotsRequiredEvent`:
- Base value modified by `Increases` / `Decreases` bit shifts.
- Can be actor- and item-modified through event handling.
- Enables equipment size/trait-driven slot scaling.

### 8.3 Gigantic-size compatibility
`DoEquip` enforces gigantic creature/equipment compatibility:
- Gigantic creatures reject too-small gear except some slots (floating, natural exceptions).
- Non-gigantic creatures reject gigantic gear except hand/missile/floating exceptions.

### 8.4 First-slot flags and de-duplication
When one item occupies multiple parts, body recalculates first-slot markers:
- `FirstSlotForEquipped`
- `FirstSlotForCybernetics`
- `FirstSlotForDefaultBehavior`

These prevent duplicated counting/dispatch in certain operations.

---

## 9) Default Behavior vs Equipped vs Cybernetics

`BodyPart` can carry all three references with overlap rules:
- Natural weapons/gear often become `DefaultBehavior`.
- Installed implants set `_Cybernetics`; some implants also consume equip slots (`CyberneticsUsesEqSlot`) and force equip behavior.
- `Body` and `BodyPart` provide APIs to enumerate:
  - equipped only
  - installed cybernetics only
  - equipped + cybernetics
  - default behavior traversal

Important: enumeration methods often de-duplicate by object identity (`Return.Contains`) because one object can occupy multiple body parts.

---

## 10) Auto-Equip Logic

### 10.1 Player/NPC API
`GameObject.AutoEquip(...)`:
- Ensures item is in inventory first.
- Chooses strategy by inventory category (`Shields`, `Armor`, `Missile Weapons`, `Ammo`, `Melee Weapons`, fallback).
- Repeatedly fires `CommandEquipObject` with incrementing `AutoEquipTry`.
- Tracks displaced items in `WasUnequipped` for feedback.
- On failure, restores original context (cell/inventory) and surfaces failure details.

### 10.2 NPC reequip (`Brain.PerformReequip`)
NPC/companion reequip is broader than single-item autoequip:
- Gathers inventory + body state.
- Sorts candidates (`GearSorter` / weapon comparers).
- Handles primary hand/dual wield/shield/light-source heuristics.
- Skips restricted items (`NoAIEquip`, merchant `_stock` inventory).
- Uses helper `Equip(...)` that drives command equip events.
- Optionally picks a preferred primary limb after reequip.

---

## 11) Stacks and Quantity Semantics

`Stacker` part controls quantity behavior:
- `StackCount` / `Number` field semantics.
- Merge-on-enter-cell and merge-on-added-to-inventory (unless `NoStack`).
- Equip of stacked items auto-unstacks one and keeps remainder in inventory.
- `SplitStack(count)` clones/splits stacks for partial drop/trade.
- `BeforeDestroyObjectEvent` decrements stack instead of destroying whole stack unless obliterate.

`Inventory.CheckStacks()` runs merge passes after many take operations (bounded loop of 100 iterations).

---

## 12) Weight, Capacity, and Overburden

### 12.1 Carried weight composition
- `Inventory.GetWeight()` cached sum of inventory contents.
- `Body.GetWeight()` from body tree equipment.
- Actor `GetCarriedWeight()` uses `GetCarriedWeightEvent` and includes both.

### 12.2 Max carry
`GetMaxCarriedWeight()`:
- Base = `Strength * RuleSettings.MAXIMUM_CARRIED_WEIGHT_PER_STRENGTH`.
- `MAXIMUM_CARRIED_WEIGHT_PER_STRENGTH = 15` (`XRL.Rules/RuleSettings.cs`).
- Gigantic creatures get multiplier (x2 in `GetMaxCarriedWeightEvent`).
- Further modifiable by legacy and min-events.

### 12.3 Overburden behavior
`GameObject.IsOverburdened()` only applies to **player-controlled** objects.
- NPCs are effectively exempt from overburden checks.

`Inventory.CheckOverburdened()` applies/removes `Overburdened` effect.
- Effect blocks movement (`IsMobile` event false).
- Effect blocks switching to flying movement mode.

`CheckOverburdened()` is invoked in inventory turn ticks and on relevant stat/capacity changes.

---

## 13) Ownership, Trespass, Theft, and Social Response

Multiple paths enforce ownership prompts and social consequences:

- Taking non-owned item (pickup/equip paths) can prompt confirmation and call `BroadcastForHelp`.
- Opening non-owned container prompts and broadcasts `HelpCause.Trespass`.
- Removing owned items from owned container can broadcast theft with severity scaling by stolen value.
- Drinking from non-owned liquid container prompts and can broadcast for help.

This means inventory mechanics are tightly coupled to world simulation and faction response.

---

## 14) Containers and Trade as Inventory Transfer Engines

### 14.1 `Container` open behavior
`Container.AttemptOpen(...)`:
- Creature containers route to trade UI.
  - Player-led companion: transfer mode (`costMultiple = 0`).
  - Merchant: normal trade.
- Non-creature containers:
  - Empty prompt can directly open container transfer mode.
  - Non-empty uses `PickItem` get dialog and twiddle actions.
- Uses `Preposition` for messaging (`in`, etc).
- Tracks `StoredByPlayer` state and can backup inventory serialization.

### 14.2 `TradeUI` transfer mechanics
`TradeUI` has two modes:
- `Trade` (value exchange in drams of water)
- `Container` (transfer semantics)

Key logic:
- `ValidForTrade` filters natural gear, protected tags, acceptance constraints, ownership loops.
- `GetObjects` builds categorized side lists.
- `PerformOffer`:
  - handles price delta and trader credit/debt (`TraderCreditExtended`)
  - validates water container capacity for received drams
  - removes items via silent `CommandRemoveObject` (`TryRemove`)
  - moves items using `TakeObject`/`ReceiveObject` or `CommandTakeObject` in container mode
  - marks/unmarks merchant stock (`_stock`) and storage provenance (`StoredByPlayer`, `FromStoredByPlayer`)
  - exchanges water currency

### 14.3 Merchant restock and stock tagging
`GenericInventoryRestocker`:
- Initializes/restocks stock on `StartTradeEvent` and periodic ticks.
- New stock marked `_stock`.
- Previous restockable `_stock` removed on restock.
- Initial non-stock items may be marked `norestock`.

This stock tagging integrates with NPC reequip logic to avoid merchants equipping sale inventory.

---

## 15) UI Surface to Mechanics Mapping

### 15.1 Inventory screen
`InventoryScreen` hotkeys map to actions:
- `Ctrl+D` -> `InventoryActionEvent("CommandDropObject")`
- `Ctrl+A` -> `Eat`
- `Ctrl+R` -> `Drink`
- `Ctrl+P` -> `Apply`
- Right/`Ctrl+E` can trigger `AutoEquip`
- `Space` opens twiddle action menu (`EquipmentAPI.TwiddleObject`)

### 15.2 Equipment screen
`EquipmentScreen`:
- Shows body-part slots and equipped/default/cybernetics views.
- Unequip sends `CommandUnequipObject`.
- Empty slot can open manual equip picker (`ShowBodypartEquipUI`) and send `CommandEquipObject`.

### 15.3 Pickup dialog
`PickItem` in get-item mode:
- Uses twiddle menu for each item action.
- `Take All` either:
  - queues AutoAct pickup loop when local/adjacent, or
  - directly iterates `Actor.TakeObject(item)` for eligible `ShouldTakeAll()` items.
- Warns player before crossing carry limit.

---

## 16) Inventory Action Framework (Extensibility Layer)

This layer defines how item interaction menus are assembled and executed.

### 16.1 Action collection
`EquipmentAPI.TwiddleObject(...)` aggregates actions from:
- Legacy string events: `GetInventoryActions`, `GetInventoryActionsAlways`, `OwnerGetInventoryActions`
- Min-events: `GetInventoryActionsEvent`, `GetInventoryActionsAlwaysEvent`, `OwnerGetInventoryActionsEvent`

### 16.2 Action model
`InventoryAction` fields control:
- command id
- hotkey/display/default/priority
- where to fire (`FireOnActor` / `FireOn`)
- distance/telekinesis/telepathy usability
- dispatch path (`AsMinEvent`)

### 16.3 Dispatch mode split
- `AsMinEvent = true`: executes via `InventoryActionEvent.Check(...)`.
- `AsMinEvent = false`: fires raw string command event directly.

Notable consequence:
- Some core physics actions (`Get`, `AutoEquip`, manual `CommandEquipObject`) are installed as raw command actions (`AsMinEvent:false`), while many other item-specific actions use min-event dispatch.

### 16.4 Item parts adding behavior
Representative examples:
- `Food`: adds and handles `Eat`.
- `LiquidVolume`: adds `Drink`, `Pour`, `Fill`, `Collect`, `Seal/Unseal`, `AutoCollect`, `CleanWithLiquid`.
- `EnergyCellSocket`: adds `Replace/Install Cell`, supports slotted cell actions.
- `MagazineAmmoLoader`: adds `Load Ammo` / `Unload Ammo`.

---

## 17) Persistence, Integrity, and Recovery

Inventory robustness includes explicit corruption safeguards:

1. **Backup snapshot** (`Inventory.TryStoreBackup`)
- Serializes inventory list to Base64 string property `InventoryBase64`.

2. **Restore snapshot** (`Inventory.TryRestoreBackup`)
- Decodes and rebuilds object list.
- Rebinds each restored object's `Physics._InInventory`.

3. **Verification** (`Inventory.VerifyContents`)
- Detects invalid/pool placeholders (`"Object"`, `"*PooledObject"`).
- Attempts backup restore; otherwise purges invalid entries.
- Logs metrics with zone/object context.

4. **When verification runs**
- On zone thaw (`ZoneThawedEvent`)
- After game load (`AfterGameLoadedEvent`)
- Container open path calls verify before presenting contents.

---

## 18) Death / Strip / Drop Semantics

### 18.1 Body phase
`Body.HandleEvent(BeforeDeathRemovalEvent)`:
- Force-unequips body and children (`UnequipPartAndChildren`) unless no-drop/temporary.
- For death path, can drop equipment to `GetDropInventory()` target.

### 18.2 Inventory phase
`Inventory.HandleEvent(BeforeDeathRemovalEvent)`:
- If configured (`ClearOnDeath`, `DropOnDeath`) and drops allowed:
  - moves inventory objects to drop inventory/cell
  - applies `DropOnDeathEvent` per item
  - sends `DroppedEvent`

### 18.3 Strip path
`StripContentsEvent` obliterates inventory/equipment/cybernetics subject to filters.

---

## 19) Notable Implementation Invariants and Gotchas

1. **Inventory is event-first, not direct mutation-first**
- Most gameplay paths use command events with begin/perform phases.

2. **`IInventory` abstraction is central**
- Cells and inventories can both receive dropped/transferred items.

3. **Player-only overburden enforcement**
- NPCs can carry arbitrarily without overburden lockouts.

4. **Ownership checks happen in multiple command handlers**
- Taking, equipping, opening, drinking, and container/trade transfers may independently trigger theft/trespass logic.

5. **Multi-slot equipment and dynamic slot counts are first-class**
- Slot requirements come from both static `UsesSlots` strings and dynamic `GetSlotsRequiredEvent`.

6. **Menu action path matters**
- Raw command vs min-event (`AsMinEvent`) changes which handlers fire first.

7. **Stock flags influence AI behavior**
- Merchant `_stock` inventory is deliberately avoided by AI equip logic.

8. **Container backups are conditional and practical**
- Backup/restore is used as recovery for corrupted stored containers, especially around player storage flows.

---

## 20) Quick Call-Flow Reference

### Take item
1. UI (`CommandGet` / picker) -> `CommandTakeObject`
2. `BeginTake` + `BeginBeingTaken`
3. `PerformTake`
4. `AddObject` + stack merge + energy

### Drop item
1. `InventoryActionEvent("CommandDropObject")`
2. `BeginDrop` + `BeginBeingDropped`
3. `PerformDrop`
4. destination `IInventory.AddObjectToInventory`

### Equip item
1. `CommandEquipObject`
2. ownership/mobility checks + slot query
3. `BeginEquip` + `BeginBeingEquipped`
4. `PerformEquip` -> `BodyPart.DoEquip`
5. equip events + energy

### Unequip item
1. `CommandUnequipObject`
2. stuck/frozen checks + begin hooks
3. `PerformUnequip` -> `BodyPart.Unequip`
4. optional free-take back to inventory

### Trade/container transfer
1. Build valid offer lists (`TradeUI.GetObjects`)
2. Remove items via `CommandRemoveObject`
3. Move items with `TakeObject` / `CommandTakeObject`
4. apply stock flags and water exchange

---

## 21) Primary Source File Index

Core:
- `XRL.World.Parts/Inventory.cs`
- `XRL.World/GameObject.cs`
- `XRL.World/IInventory.cs`
- `XRL.World/Cell.cs`

Anatomy/equipment:
- `XRL.World.Parts/Body.cs`
- `XRL.World.Anatomy/BodyPart.cs`
- `XRL.World/QuerySlotListEvent.cs`
- `XRL.World/QueryEquippableListEvent.cs`
- `XRL.World/GetSlotsRequiredEvent.cs`

Actions/UI:
- `Qud.API/EquipmentAPI.cs`
- `XRL.World/InventoryAction.cs`
- `XRL.World/InventoryActionEvent.cs`
- `XRL.World/IInventoryActionsEvent.cs`
- `XRL.World.Parts/Physics.cs`
- `XRL.UI/InventoryScreen.cs`
- `XRL.UI/EquipmentScreen.cs`
- `XRL.UI/PickItem.cs`

Trade/container/NPC:
- `XRL.UI/TradeUI.cs`
- `XRL.World.Parts/Container.cs`
- `XRL.World.Parts/GenericInventoryRestocker.cs`
- `XRL.World/StartTradeEvent.cs`
- `XRL.World.Parts/Brain.cs`
- `XRL.World.AI.GoalHandlers/GoFetch.cs`
- `XRL.World.AI.GoalHandlers/GoFetchGet.cs`

Stacks/weight/effects:
- `XRL.World.Parts/Stacker.cs`
- `XRL.World/GetCarriedWeightEvent.cs`
- `XRL.World/GetMaxCarriedWeightEvent.cs`
- `XRL.World.Effects/Overburdened.cs`
- `XRL.Rules/RuleSettings.cs`

Embedded inventory-like parts:
- `XRL.World.Parts/Food.cs`
- `XRL.World.Parts/LiquidVolume.cs`
- `XRL.World.Parts/EnergyCellSocket.cs`
- `XRL.World.Parts/MagazineAmmoLoader.cs`

---

If you want, I can follow this with a second document that is purely a **sequence-diagram style trace** for each major command (`CommandTakeObject`, `CommandEquipObject`, `PerformOffer`) including every relevant event and rollback branch.
