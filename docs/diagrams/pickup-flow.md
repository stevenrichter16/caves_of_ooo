# Dagger Pickup Flow: Ground to Inventory

Traces every step from when a caller invokes `InventorySystem.Pickup(actor, dagger, zone)`
through to the dagger being committed into the actor's inventory.

```
 CALLER
   |
   |  InventorySystem.Pickup(actor, dagger, zone)
   v
 InventorySystem  (static facade)
   |
   |  1. Creates:  new PickupCommand(dagger)
   |  2. Creates:  new InventoryContext(actor, zone)
   |               -> resolves actor.GetPart<InventoryPart>()
   |               -> resolves actor.GetPart<Body>()
   |  3. Delegates to CommandExecutor.Execute(command, context)
   v
 InventoryCommandExecutor.Execute
   |
   |
   |===========================|
   |   PHASE 1: VALIDATION     |
   |===========================|
   |
   |  command.Validate(context)
   v
 PickupCommand.Validate
   |
   |  [x] context.Actor != null?        --> InvalidActor
   |  [x] context.Zone != null?         --> InvalidZone
   |  [x] context.Inventory != null?    --> MissingInventoryPart
   |  [x] dagger != null?               --> InvalidItem
   |  [x] dagger.PhysicsPart.Takeable?  --> NotTakeable
   |
   |  All checks pass --> InventoryValidationResult.Valid()
   v
 Back in InventoryCommandExecutor
   |
   |  validation.IsValid == true, so continue
   |  Creates:  new InventoryTransaction()
   |
   |===========================|
   |   PHASE 2: EXECUTION      |
   |===========================|
   |
   |  command.Execute(context, transaction)
   v
 PickupCommand.Execute
   |
   |  .-----------------------------------------------------.
   |  |  EVENT: "BeforePickup" fired on actor                |
   |  |  - Any part on the actor can veto (return false)     |
   |  |  - If vetoed --> FAIL "Pickup was cancelled."        |
   |  '-----------------------------------------------------'
   |
   |  .-----------------------------------------------------.
   |  |  EVENT: "BeforeBeingPickedUp" fired on dagger        |
   |  |  - Any part on the dagger can veto (return false)    |
   |  |  - If vetoed --> FAIL "Item pickup was cancelled."   |
   |  '-----------------------------------------------------'
   |
   |  Record dagger's zone position:
   |    originalCell = zone.GetEntityCell(dagger)
   |    save (originalX, originalY) for undo
   |
   |  .------------------.
   |  |  ZONE REMOVAL     |
   |  '------------------'
   |    zone.RemoveEntity(dagger)     <--- dagger leaves the ground
   |
   |    transaction.Do(
   |      apply: null,                <--- already applied above
   |      undo: zone.AddEntity(dagger, originalX, originalY)
   |    )
   |
   |  .------------------.
   |  |  INVENTORY ADD     |
   |  '------------------'
   |    inventory.AddObject(dagger)
   |      |
   |      |  1. Weight check:
   |      |     if MaxWeight >= 0 and carriedWeight + daggerWeight > MaxWeight
   |      |       --> return false ("too heavy!")
   |      |
   |      |  2. Stack merge attempt:
   |      |     dagger has no StackerPart --> skip
   |      |     (if stackable, would merge into matching stack)
   |      |
   |      |  3. Add to Objects list:
   |      |     inventory.Objects.Add(dagger)
   |      |
   |      |  4. Update physics refs:
   |      |     dagger.PhysicsPart.InInventory = actor
   |      |     dagger.PhysicsPart.Equipped = null
   |      |
   |      |  return true
   |      v
   |    transaction.Do(
   |      apply: null,
   |      undo: inventory.RemoveObject(dagger)
   |    )
   |
   |    MessageLog: "Player picks up Dagger."
   |
   |  .-------------------------------------------.
   |  |  AUTO-EQUIP ATTEMPT (non-fatal)            |
   |  '-------------------------------------------'
   |    new AutoEquipCommand(dagger).Execute(context, transaction)
   |      |
   |      |  - Checks dagger has EquippablePart
   |      |  - Checks not a stack
   |      |  - If actor has Body:
   |      |      EquipPlanner.Build(actor, dagger)
   |      |        run rules: TargetPartCompatibility
   |      |                    --> SlotCount
   |      |                    --> SlotAvailability
   |      |                    --> Displacement
   |      |      If plan valid AND no displacements:
   |      |        equip dagger to body part (e.g. Hand)
   |      |      Else: fail silently (stays carried)
   |      |  - If no Body (legacy):
   |      |      Check slot is empty --> equip to slot
   |      |
   |      |  Failure here does NOT fail the pickup.
   |      |  Dagger just stays in carried inventory.
   |      v
   |
   |  .-----------------------------------------------------.
   |  |  EVENT: "AfterPickup" fired on actor                 |
   |  |  - Informational only, cannot veto                   |
   |  '-----------------------------------------------------'
   |
   |  return InventoryCommandResult.Ok()
   v
 Back in InventoryCommandExecutor
   |
   |  result.Success == true
   |
   |===========================|
   |   PHASE 3: COMMIT         |
   |===========================|
   |
   |  transaction.Commit()
   |    - Clears all registered undo actions
   |    - Marks IsCommitted = true
   |    - State changes are now permanent
   |
   |  return result
   v
 Back in InventorySystem.Pickup
   |
   |  return result.Success  -->  true
   v
 DONE. Dagger is in the actor's inventory.
```

## What the transaction protected

If anything had failed mid-execution, `transaction.Rollback()` would have
undone steps in reverse order:

```
  Rollback order (reverse of registration):
  2. inventory.RemoveObject(dagger)   <-- remove from inventory
  1. zone.AddEntity(dagger, x, y)     <-- put back on the ground
```

This ensures the dagger is never in a broken state (e.g. removed from the zone
but not yet in inventory).

## Key participants

| Class                      | Role                                             |
|----------------------------|--------------------------------------------------|
| `InventorySystem`          | Static facade -- routes to command executor       |
| `PickupCommand`            | Encapsulates all pickup validation + mutation     |
| `InventoryCommandExecutor` | Validate -> Execute -> Commit/Rollback pipeline   |
| `InventoryTransaction`     | Tracks undo actions, commits or rolls back        |
| `InventoryPart`            | Data model -- `Objects` list, weight, stack merge |
| `AutoEquipCommand`         | Optional post-pickup equip (non-fatal)            |
| `EquipPlanner`             | Rules engine deciding where equipment goes        |
