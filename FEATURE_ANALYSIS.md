# Feature Implementation Analysis: LIQUIDS.md & INVENTORY.md

Analysis of which Caves of Qud features from the liquids and inventory documentation
would be easiest to implement in Caves of Ooo, and what foundational systems must
exist before any of them can work.

---

## Current State (What Already Exists)

The game has 9 phases implemented (271 tests passing):

| Phase | System | Key Classes |
|-------|--------|-------------|
| 1 | Entity/Part/Event/Stat | Entity, Part, GameEvent, Stat, Blueprint, EntityFactory |
| 2 | 80x25 Grid & Rendering | Cell, Zone, ZoneRenderer, CP437TilesetGenerator |
| 3 | Turn System & Movement | TurnManager (energy-based), MovementSystem, PhysicsPart |
| 4 | Zone Generation | IZoneBuilder pipeline, CellularAutomata, 7 builders |
| 5 | Melee Combat | CombatSystem (hit/pen/damage), MeleeWeaponPart, ArmorPart, DiceRoller |
| 6 | Inventory & Items | InventoryPart, EquippablePart, InventorySystem (pickup/drop/equip/unequip), weight limits |
| 7 | Mutations & Abilities | ActivatedAbility, BaseMutation, MutationsPart, 3 mutations |
| 8 | Factions & AI | FactionManager, AIHelpers (LOS, pathfinding), BrainPart (state machine) |
| 9 | World Map & Transitions | WorldMap (10x10, 4 biomes), ZoneTransitionSystem, OverworldZoneManager |

---

## Prerequisites: What Must Be Built Before Features From Either Doc

These foundational systems are referenced repeatedly by both LIQUIDS.md and
INVENTORY.md features. Without them, most non-trivial features will be blocked.

### 1. Status Effects System (CRITICAL — blocks the most features)

**Referenced by:** Overburdened, Confused, Poisoned, Stuck, Slippery, Swimming,
Wading, LiquidCovered, Frozen, Intoxication, Disease, and dozens more.

**What it needs:**
- `BaseEffect` abstract class with duration, turn tick, saving throws
- Effect list on Entity (add/remove/query/tick)
- `StatShifter` helper for temporary stat modifications that auto-revert
- Saving throw system (stat vs DC to resist/shake off effects)
- Basic effects: Stuck, Confused, Poisoned as initial implementations

**Why it's the biggest blocker:** Nearly every interesting interaction from both docs
terminates in "apply effect X." Drinking acid applies damage. Drinking slime applies
Confused. Stepping in honey applies Stuck. Overburden blocks movement. Without an
effect system, these features have nowhere to land.

**Estimated scope:** ~300-400 lines of core code + tests. Fits naturally into the
existing Part/Event architecture.

### 2. Body/Anatomy System (CRITICAL — blocks advanced equipment and liquid contact)

**Referenced by:** Multi-slot equipment, equipment slot queries, cybernetics,
liquid contact/smearing (body part exposure), dismemberment.

**What it needs:**
- `BodyPart` class with type, laterality, parent/child tree
- `Body` Part that holds the root body part tree
- Slot query system (which body parts can equip which items)
- Multi-slot equipment support (items occupying >1 body part)

**Why it matters:** The current InventoryPart uses a simple `Dictionary<string, Entity>`
for equipment slots. INVENTORY.md describes a full body part tree where equipment is
stored per-body-part. LIQUIDS.md's contact system distributes liquid exposure across
body parts weighted by mobility. Both docs assume this system exists.

**Estimated scope:** ~500-700 lines of core code + tests. Requires refactoring
InventoryPart's equipment handling.

### 3. Temperature System (Required for liquid phase changes)

**Referenced by:** Freeze/vaporize mechanics, lava contact, wax/asphalt effects,
gas creation from boiling liquids.

**What it needs:**
- Temperature stat or property on entities
- Temperature change events and propagation
- Threshold checking (compare against freeze/vapor/flame temperatures)

**Why it matters:** About a third of LIQUIDS.md's complexity comes from temperature
interactions. Freezing creates solid objects, vaporizing creates gas objects,
and temperature drives many special reactions.

**Estimated scope:** ~200-300 lines. Can be deferred if you implement liquids
without phase changes first.

### 4. Gas System (Required for liquid vaporization)

**Referenced by:** Steam from boiling water, acid gas, poison gas, confusion gas,
miasma from ooze.

**What it needs:**
- Gas Part with density and spread behavior
- Gas entities placed in cells
- Turn-based dissipation
- Contact effects when creatures enter gas cells

**Estimated scope:** ~300 lines. Only needed once temperature/vaporization is in scope.

---

## LIQUIDS.md Features: Ranked by Implementation Ease

### Tier 1 — Easy (can build now with minimal prerequisites)

#### 1. BaseLiquid Registry + Simple Liquid Types
**What:** Abstract `BaseLiquid` class with property fields (Weight, Fluidity,
Combustibility, etc.) plus an `[IsLiquid]` attribute for reflection-based discovery.
Initial types: water, blood, oil, acid (4 types to start).

**Why easy:** Pure data classes with no gameplay interaction. Mirrors the existing
`EntityFactory.RegisterPartsFromAssembly()` pattern exactly. No dependencies.

**Prerequisite:** None.

#### 2. LiquidVolume Part (Core Container)
**What:** A Part with `Volume`, `MaxVolume`, `ComponentLiquids` dictionary
(string→int, 1000-point proportions), `Primary`/`Secondary` tracking,
proportion normalization, purity checks.

**Why easy:** Self-contained data management. The proportion math is well-documented
and purely arithmetic. No rendering or interaction needed yet.

**Prerequisite:** BaseLiquid registry (#1).

#### 3. Proportion Math + Liquid Mixing
**What:** `MixWith()` method that recalculates proportions when two LiquidVolumes
combine. Weighted average formula: `new_prop = (vol_a * prop_a + vol_b * prop_b) / (vol_a + vol_b)`.

**Why easy:** Pure math with a clear algorithm documented in LIQUIDS.md section 7.
No UI, no effects, no events beyond basic notification.

**Prerequisite:** LiquidVolume Part (#2).

#### 4. Liquid Value/Economy Data
**What:** `GetValuePerDram()` and `GetPureLiquidValueMultiplier()` on each
BaseLiquid. Water gets the 100x pure multiplier (universal currency).

**Why easy:** Just data fields on existing BaseLiquid classes. No trade system needed
yet — this just establishes the data.

**Prerequisite:** BaseLiquid registry (#1).

#### 5. Container vs Open Volume Distinction
**What:** `MaxVolume > 0` = container (waterskin, vial), `MaxVolume == -1` = open
volume (puddle, pool). `IsOpenVolume()`, depth thresholds (puddle/wading/swimming).

**Why easy:** Single field check + threshold constants. Already part of LiquidVolume.

**Prerequisite:** LiquidVolume Part (#2).

#### 6. Basic Drinking (Water Only)
**What:** Drink action consumes 1 dram from a LiquidVolume. Water adds hydration
(via event). Check sealed state, fire Before/After events.

**Why easy:** Follows the exact same pattern as existing pickup/equip event chains.
Water-only means no status effects needed yet.

**Prerequisite:** LiquidVolume Part (#2). A hydration/thirst stat on the player
(just a Stat, trivial to add).

### Tier 2 — Moderate (need one small system or some refactoring)

#### 7. Liquid Pools in Zone Generation
**What:** `LiquidPoolsBuilder` that uses noise to place puddle entities with
LiquidVolume Parts at varying depths.

**Why moderate:** Fits the existing IZoneBuilder pipeline perfectly. Needs LiquidVolume
Part and puddle blueprints. Some rendering work for puddle glyphs.

**Prerequisite:** LiquidVolume Part (#2), new puddle blueprints in Objects.json.

#### 8. Sealed/Unsealed Container Mechanics
**What:** `Sealed` flag + `EffectivelySealed()` check. When sealed: block drinking,
pouring, filling. Manual seal/unseal toggle.

**Why moderate:** Flag-based logic, straightforward. Needs a way to trigger seal/unseal
(inventory action or key press).

**Prerequisite:** LiquidVolume Part (#2).

#### 9. Navigation Weights for Liquids
**What:** AI pathfinding considers liquid danger. Acid gets weight 30, lava gets 99,
most liquids get 0. Modifies AI movement decisions.

**Why moderate:** Small addition to `AIHelpers` or `BrainPart`. Query cells for
LiquidVolume, ask the BaseLiquid for its navigation weight.

**Prerequisite:** LiquidVolume Part (#2), liquid pool placement (#7).

#### 10. Basic Pouring (Into Cell)
**What:** Pour liquid from a container onto an adjacent cell. Creates a puddle entity
or merges with existing liquid volume.

**Why moderate:** Needs direction targeting (already exists for mutations) and puddle
entity creation. Must handle existing liquid merging.

**Prerequisite:** LiquidVolume Part (#2), mixing (#3), puddle blueprints.

#### 11. Liquid Rendering (Primary/Secondary Colors)
**What:** LiquidVolume tells the renderer which color/glyph to use based on Primary
liquid type. Puddles use `,` or `~` glyphs.

**Why moderate:** Requires ZoneRenderer changes and RenderPart coordination. The
color system (QudColorParser) already exists.

**Prerequisite:** LiquidVolume Part (#2), Primary/Secondary tracking.

### Tier 3 — Hard (need major prerequisite systems)

#### 12. Drinking Effects (Acid, Slime, Honey, Wine, etc.)
**Needs:** Status Effects system (Confused, Poisoned, Intoxication tracking).

#### 13. Contact & Smearing (Body Part Exposure)
**Needs:** Body/Anatomy system + Status Effects system.

#### 14. Temperature Effects (Freeze/Vaporize)
**Needs:** Temperature system + Gas system.

#### 15. Slippery/Sticky Mechanics
**Needs:** Status Effects system (Stuck, forced movement).

#### 16. Evaporation
**Needs:** Turn-tick processing on LiquidVolume (moderate), plus the mixed-liquid
evaporativity sorting logic.

#### 17. Liquid Spreading/Flowing
**Needs:** Multi-cell simulation, performance-conscious ground liquid merging.

#### 18. Special Reactions (Neutron Flux, Warm Static, Acid Container Damage)
**Needs:** Multiple systems (explosions, transmutation, material properties).

#### 19. Liquid Producer Part
**Needs:** LiquidVolume + turn-tick + distribution priority logic.

---

## INVENTORY.md Features: Ranked by Implementation Ease

### Tier 1 — Easy (can build now on top of Phase 6)

#### 1. Item Stacking (Stacker Part)
**What:** `Stacker` Part with `StackCount`. Merge identical items on pickup
(same BlueprintName). Split single item from stack on equip. `SplitStack(count)`
for partial operations.

**Why easy:** Self-contained Part. Merge-on-add hooks into existing
`InventoryPart.AddObject()`. Split is a clone + count adjustment.
No other systems needed.

**Prerequisite:** None beyond current Phase 6.

#### 2. Drop-on-Death Semantics
**What:** When an entity dies (existing `Died` event), drop all inventory contents
and unequip all equipment onto the death cell.

**Why easy:** Already have the Died event, Drop logic, and zone placement.
Just wire them together in a new Part or in InventoryPart's HandleEvent.

**Prerequisite:** None beyond current Phase 6.

#### 3. Hidden Inventory Items
**What:** Items with tag `HiddenInInventory` are excluded from user-facing inventory
listings but still exist in the Objects list.

**Why easy:** Single tag check in any inventory display/query method. Trivial.

**Prerequisite:** None.

#### 4. IInventory Abstraction
**What:** Interface with `AddObject()`/`RemoveObject()`/`Contains()` implemented
by both `InventoryPart` and `Cell`. Enables uniform drop targets (drop into
a container OR onto the ground with the same code path).

**Why easy:** Both Cell and InventoryPart already have add/remove methods.
Extract the interface and adjust InventorySystem to use it.

**Prerequisite:** None. Refactor only.

#### 5. Overburden Movement Block
**What:** When carried weight exceeds `Strength * 15`, block movement.
Check on every pickup/drop/equip/unequip and on stat changes.

**Why easy:** Already have weight tracking, movement blocking (PhysicsPart handles
`BeforeMove`), and stat access. Can be done purely with events — no Status Effects
system strictly required (though it's cleaner with one).

**Prerequisite:** None if implemented via BeforeMove event check. Status Effects
system if you want a proper Overburdened effect.

### Tier 2 — Moderate (need some new infrastructure)

#### 6. Container Part (Openable World Containers)
**What:** Chests, barrels, etc. that the player can open to access contents.
A Part that holds an internal InventoryPart and responds to an Open command.

**Why moderate:** Needs a way to present contents to the player (at minimum,
a message log listing; ideally a picker UI).

**Prerequisite:** IInventory abstraction (#4) helps. Some form of item selection UI.

#### 7. Partial Stack Operations
**What:** Drop/trade/pour a specific quantity from a stack instead of all-or-nothing.

**Why moderate:** Needs Stacker Part (#1) and a way to prompt for quantity.

**Prerequisite:** Stacker Part (#1).

#### 8. NPC Reequip Logic
**What:** NPCs evaluate their inventory and equip the best available gear.
Sort weapons by damage, armor by AV, prefer shields if available.

**Why moderate:** Needs item comparison heuristics and integration with BrainPart.
Current BrainPart has Idle/Wander/Chase but no equipment management.

**Prerequisite:** None strictly, but benefits from item tier/quality metadata.

#### 9. Equipment Slot Improvements
**What:** Multiple slots of the same type (two Hand slots for dual-wield),
slot validation (weapons go in Hand, armor goes in Body), slot listing.

**Why moderate:** Current system uses a flat string→Entity dictionary. Needs
refactoring to support multiple Hand slots and proper validation.

**Prerequisite:** None strictly, but Body/Anatomy system replaces this entirely.

### Tier 3 — Hard (need major prerequisite systems)

#### 10. Body/Anatomy System (Full Body Part Tree)
**Needs:** Major new system (~500-700 lines). Refactors equipment entirely.

#### 11. Multi-Slot Equipment
**Needs:** Body/Anatomy system (#10).

#### 12. Trade/Merchant System
**Needs:** Economy (water currency), merchant inventory, trade UI, price calculation.

#### 13. Ownership/Theft Mechanics
**Needs:** Faction response system integration, ownership tracking, social consequences.

#### 14. Cybernetics Equipment Slots
**Needs:** Body/Anatomy system (#10) + cybernetics installation logic.

#### 15. Inventory Action Framework (Twiddle Menus)
**Needs:** UI framework for context-sensitive item action menus.

#### 16. Persistence/Backup System
**Needs:** Serialization infrastructure for save/load.

---

## Recommended Implementation Order

Based on the analysis above, here is a suggested sequence that maximizes
feature delivery while respecting dependency chains:

### Phase 10: Liquid Foundation + Easy Inventory Wins
1. **BaseLiquid class + registry** (LIQUIDS Tier 1 #1)
2. **LiquidVolume Part** with proportions, mixing, purity checks (LIQUIDS Tier 1 #2-5)
3. **Item Stacking (Stacker Part)** (INVENTORY Tier 1 #1)
4. **Drop-on-Death** (INVENTORY Tier 1 #2)
5. **Hidden Inventory Items** tag filter (INVENTORY Tier 1 #3)
6. **Basic water drinking** (LIQUIDS Tier 1 #6)

*No new prerequisite systems needed. All build on existing architecture.*

### Phase 11: Status Effects System
7. **BaseEffect + Effect list on Entity** (PREREQUISITE #1)
8. **StatShifter** for temporary stat mods
9. **Saving throw system** (stat vs DC)
10. **Initial effects:** Stuck, Confused, Poisoned, Overburdened

*Unlocks: drinking effects, slippery/sticky, overburden, and dozens of future features.*

### Phase 12: Liquids in the World
11. **Liquid pool zone builder** (LIQUIDS Tier 2 #7)
12. **Liquid rendering** (LIQUIDS Tier 2 #11)
13. **Sealed/unsealed mechanics** (LIQUIDS Tier 2 #8)
14. **Basic pouring** (LIQUIDS Tier 2 #10)
15. **AI navigation weights for liquids** (LIQUIDS Tier 2 #9)
16. **Drinking effects** for acid, slime, honey, wine (LIQUIDS Tier 3 #12 — now unblocked)

### Phase 13: Body/Anatomy + Advanced Equipment
17. **Body/Anatomy system** (PREREQUISITE #2)
18. **Multi-slot equipment** (INVENTORY Tier 3 #11)
19. **Liquid contact/smearing** (LIQUIDS Tier 3 #13 — now unblocked)
20. **NPC reequip logic** (INVENTORY Tier 2 #8)
21. **Overburden as proper effect** (INVENTORY Tier 1 #5 — refined)

### Phase 14: Temperature + Advanced Liquids
22. **Temperature system** (PREREQUISITE #3)
23. **Gas system** (PREREQUISITE #4)
24. **Freeze/vaporize** (LIQUIDS Tier 3 #14)
25. **Slippery/sticky mechanics** (LIQUIDS Tier 3 #15)
26. **Evaporation** (LIQUIDS Tier 3 #16)
27. **Special reactions** (LIQUIDS Tier 3 #18)

---

## Summary Table

| Feature | Source | Tier | Key Prerequisite |
|---------|--------|------|------------------|
| BaseLiquid registry | LIQUIDS | 1-Easy | None |
| LiquidVolume Part (core) | LIQUIDS | 1-Easy | BaseLiquid registry |
| Liquid mixing math | LIQUIDS | 1-Easy | LiquidVolume |
| Liquid value data | LIQUIDS | 1-Easy | BaseLiquid registry |
| Container vs pool | LIQUIDS | 1-Easy | LiquidVolume |
| Water drinking | LIQUIDS | 1-Easy | LiquidVolume |
| Item stacking | INVENTORY | 1-Easy | None |
| Drop-on-death | INVENTORY | 1-Easy | None |
| Hidden inventory items | INVENTORY | 1-Easy | None |
| IInventory abstraction | INVENTORY | 1-Easy | None (refactor) |
| Overburden (basic) | INVENTORY | 1-Easy | None |
| Liquid pool generation | LIQUIDS | 2-Medium | LiquidVolume + builders |
| Sealed containers | LIQUIDS | 2-Medium | LiquidVolume |
| AI liquid navigation | LIQUIDS | 2-Medium | LiquidVolume + pools |
| Pouring into cells | LIQUIDS | 2-Medium | LiquidVolume + mixing |
| Liquid rendering | LIQUIDS | 2-Medium | LiquidVolume |
| World containers | INVENTORY | 2-Medium | IInventory + UI |
| Partial stack ops | INVENTORY | 2-Medium | Stacker Part |
| NPC reequip | INVENTORY | 2-Medium | Item comparison |
| Drinking effects | LIQUIDS | 3-Hard | **Status Effects system** |
| Contact/smearing | LIQUIDS | 3-Hard | **Body/Anatomy + Effects** |
| Freeze/vaporize | LIQUIDS | 3-Hard | **Temperature + Gas** |
| Slippery/sticky | LIQUIDS | 3-Hard | **Status Effects** |
| Body/Anatomy | INVENTORY | 3-Hard | Major new system |
| Multi-slot equip | INVENTORY | 3-Hard | **Body/Anatomy** |
| Trade/merchants | INVENTORY | 3-Hard | Economy + UI |
| Ownership/theft | INVENTORY | 3-Hard | Faction response |
