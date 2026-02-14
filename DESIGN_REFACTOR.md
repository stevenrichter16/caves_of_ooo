**Proposed Changes (no edits yet)**

1. Build an inventory transaction pipeline.  
I’d add a command layer around pickup/equip/unequip/drop so each action runs as `Validate -> Execute -> Rollback on failure` instead of mutating state in many places.  
Primary target: `/Users/steven/caves-of-ooo/Assets/Scripts/Core/InventorySystem.cs`  
Why: prevents half-applied states between inventory, body parts, stacks, and zone.

2. Add an equip planner + rule specifications.  
I’d move slot legality into composable rules (`SlotType`, `Laterality`, `SlotsRequired`, `BodyDependency`, `Size/Gigantic`, `ConflictPolicy`) and have a planner return an equip plan before applying anything.  
Primary targets: `/Users/steven/caves-of-ooo/Assets/Scripts/Core/Body.cs`, `/Users/steven/caves-of-ooo/Assets/Scripts/Core/Anatomy/BodyPart.cs`, `/Users/steven/caves-of-ooo/Assets/Scripts/Core/EquippablePart.cs`  
Why: this is the biggest gap with Qud-like equipment robustness.

3. Replace string-event hot paths with typed domain events (while keeping compatibility).  
I’d keep your event-driven style, but add typed wrappers for critical inventory flows and gradually migrate string IDs.  
Primary targets: `/Users/steven/caves-of-ooo/Assets/Scripts/Core/InventorySystem.cs`, `/Users/steven/caves-of-ooo/Assets/Scripts/Core/InventoryAction.cs`  
Why: fewer typo bugs, easier refactors, better tooling support.

4. Introduce a centralized stat modifier resolver.  
Instead of directly editing stat fields from equipment/mutations/consumables, I’d aggregate modifiers from sources and compute final values in one pass.  
Primary targets: `/Users/steven/caves-of-ooo/Assets/Scripts/Core/InventorySystem.cs`, `/Users/steven/caves-of-ooo/Assets/Scripts/Core/MutationsPart.cs`  
Why: avoids stacking/removal bugs and makes balancing easier.

5. Refactor inventory UI/input into a formal state machine.  
I’d model states explicitly (`Closed`, `ListFocus`, `EquipPopup`, `ItemActionPopup`, `BodyPartPicker`) and route input by state.  
Primary targets: `/Users/steven/caves-of-ooo/Assets/Scripts/Rendering/InventoryUI.cs`, `/Users/steven/caves-of-ooo/Assets/Scripts/Rendering/InputHandler.cs`  
Why: current behavior already has state transitions; formalizing them reduces edge-case input bugs.

6. Rework trading into an offer aggregate with commit.  
I’d split trading into `DraftOffer`, `Validate`, `Preview delta`, and atomic `Commit` (items + currency + ownership consequences).  
Primary target: `/Users/steven/caves-of-ooo/Assets/Scripts/Core/TradeSystem.cs`  
Why: needed for Qud-like bartering depth and safe complex transfers.

7. Add regression tests before refactor rollout.  
I’d add tests for stacked equip/unequip, multi-slot conflicts, rollback correctness, and overburden edge cases.  
Primary targets: inventory/core test project paths in your Unity test assemblies.  
Why: these systems are interconnected and easy to break during upgrades.

If you want, I can next draft the exact class/interface skeletons and migration order (still no file edits).