# Caves of Qud Body Part and Anatomy System Deep Dive

This document is a source-code deep dive into how Caves of Qud models bodies, anatomy, body-part-based equipment slots, limb loss, regeneration, and dynamic body changes from mutations/cybernetics/equipment.

Scope emphasis:
- Equipment slots tied to body parts (`Head`, `Face`, `Arm`, `Hand`/`Hands`, `Feet`, `Back`, etc.)
- Runtime body graph and slot occupancy
- Limb loss (dismemberment + unsupported limb loss)
- Regeneration/recovery and missing-limb reporting
- Dynamic anatomy changes (mutations, implants, worn items)
- Player vs NPC behavior differences around equip/reequip

---

## 1) Architecture: anatomy is a runtime body tree, not a fixed slot enum

Qud does **not** use a single hardcoded equipment-slot enum.

It uses:
1. **Anatomy definitions** (`Anatomy`, `BodyPartType`, `AnatomyPart`) loaded from XML
2. A per-creature **runtime `Body` tree** of `BodyPart` nodes
3. Item-side equipability rules (`QueryEquippableListEvent`)
4. Body-side slot query and equip logic (`QuerySlotListEvent`, `BodyPart.DoEquip`)

Core files:
- `XRL.World.Anatomy/Anatomies.cs`
- `XRL.World.Anatomy/Anatomy.cs`
- `XRL.World.Anatomy/BodyPartType.cs`
- `XRL.World.Anatomy/BodyPart.cs`
- `XRL.World.Parts/Body.cs`
- `XRL.World.Parts/Inventory.cs`

Consequence: a slot like `Face` exists only because body part types and body trees include it. The equip system asks "which body parts on this actor can accept this item right now?" rather than indexing into fixed global slots.

---

## 2) Data layer: anatomy and body-part-type schema

### 2.1 Anatomy/type initialization and mod layering

`Anatomies.CheckInit()` lazily initializes global tables, calling `Init()` only when needed (`Anatomies.cs:61-66`).

`Init()` loads every XML file rooted at `Bodies` (`Anatomies.cs:75`) and processes body part types, variants, and anatomies.

Mod-friendly removal/override is built in:
- `removebodyparttype` (`Anatomies.cs:176-197`)
- `removebodyparttypevariant` (`Anatomies.cs:230-257`)

So anatomy schema is intentionally mutable at data/mod load time.

### 2.2 `BodyPartType`: semantic contract for each part type

`BodyPartType` carries behavior-defining metadata, not just display strings (`BodyPartType.cs:3-67`):
- topology/dependency: `ImpliedBy`, `ImpliedPer`, `RequiresType`, `RequiresLaterality`
- sever/regrow semantics: `Appendage`, `Integral`, `Abstract`, `Mortal`, `Extrinsic`
- naming/position behavior: `Plural`, `Mass`, `Contact`, `IgnorePosition`
- generation and equipment defaults: `DefaultBehavior`, `LimbBlueprintProperty`, `LimbBlueprintDefault`
- slot ecology hints: `UsuallyOn`, `UsuallyOnVariant`, `Branching`

`BodyPartType.ApplyTo(BodyPart)` stamps these defaults onto runtime parts (`BodyPartType.cs:117-181`).

### 2.3 Anatomy part nodes and explicit dependencies

`AnatomyPart` stores per-node overrides and nested subparts, then applies them recursively to the runtime parent (`AnatomyPart.cs:50-78`).

Important dependency fields on anatomy nodes (`AnatomyPart.cs:11-16`):
- `SupportsDependent` (provider anchor)
- `DependsOn` (concrete dependency pointer)
- `RequiresType`/`RequiresLaterality` (abstract dependency)

### 2.4 Runtime anatomy application

`Anatomy.ApplyTo(Body)` (`Anatomy.cs:31-64`) does a full reconstruction:
- marks body unbuilt
- clears `DismemberedParts`
- creates new root body part
- applies all anatomy parts recursively
- auto-adds `Thrown Weapon` and `Floating Nearby` unless disabled
- applies overall category
- marks all as native
- calls `UpdateBodyParts()`

This means anatomy changes are structural, not cosmetic.

---

## 3) Runtime body graph model

### 3.1 `Body` and `BodyPart`

`Body` stores:
- `_Body` root tree (`Body.cs:90`)
- `DismemberedParts` detached-part list (`Body.cs:98`)

`BodyPart` is the recursive node model with:
- identity/type fields (`Type`, `VariantType`, etc.)
- dependency fields (`SupportsDependent`, `DependsOn`, `RequiresType`)
- positional/laterality fields (`Position`, `Laterality`, `RequiresLaterality`)
- flags for semantics and dedupe (`BodyPart.cs:20-53`)
- equipment references:
  - `_Equipped` (worn/held item)
  - `_Cybernetics` (implant)
  - `_DefaultBehavior` (natural/default gear behavior)

### 3.2 First-slot dedupe flags for multi-slot occupancy

When one object occupies multiple body parts, Qud marks exactly one slot as primary for that object instance:
- `FirstSlotForEquipped`
- `FirstSlotForCybernetics`
- `FirstSlotForDefaultBehavior`

These are recalculated with:
- `RecalculateFirstEquipped` (`BodyPart.cs:2092-2108`)
- `RecalculateFirstCybernetics` (`BodyPart.cs:2110-2126`)
- `RecalculateFirstDefaultBehavior` (`BodyPart.cs:2128-2144`)
- wrapped at body level (`Body.cs:2250-2266`)

This prevents duplicate counting/event dispatch for the same object across multiple occupied parts.

### 3.3 Where dedupe actually matters

Key places gated by `FirstSlot*`:
- body carried/equipped weight aggregation (`BodyPart.cs:2352-2365`)
- equipped object counting (`BodyPart.cs:2895-2922`)
- `ForeachEquippedObject` dispatch (`BodyPart.cs:3060-3072`)
- event cascade forwarding to equipped/cyber/default (`BodyPart.cs:6476-6524`)
- armor stat contribution/recalc (`BodyPart.cs:5982-6023`)

Without this, a two-handed or multi-slot item would double/triple count stats and event effects.

---

## 4) Position/index model (including dismembered siblings)

Body part order is position-based (`Position`) and insertion-aware.

`AddPart(...)`:
- chooses/normalizes insertion position
- inserts in `Parts` list by position
- assigns and potentially renumbers siblings (`BodyPart.cs:3773-3814`)

The important subtlety: position occupancy checks include detached siblings in `DismemberedParts`:
- `PositionOccupied` checks body + dismembered (`BodyPart.cs:3678-3716`)
- `AssignPosition` can renumber dismembered positions (`BodyPart.cs:3718-3736`)
- `NextPosition` considers last dismembered sibling (`BodyPart.cs:3741-3756`)

Body-side helpers implement these detached-position queries/renumbering (`Body.cs:2050-2080`, `2158-2183`).

Why this matters: when limbs regrow/reattach or dynamic parts are inserted, positional identity remains stable and ordering remains coherent even through dismemberment.

---

## 5) Slot semantics: body-part names are the slots

### 5.1 Slot type is stringly typed and body-driven

Slot compatibility is keyed by body-part type strings (`Hand`, `Head`, `Face`, `Back`, `Feet`, etc.), not enum constants.

Item side exposes requirements via:
- `Physics.UsesSlots` (tag/property-backed) (`Physics.cs:431-440`)
- per-part `QueryEquippableListEvent` handlers (`MeleeWeapon`, `MissileWeapon`, `Shield`, `Armor`, `Physics`)

Body side enumerates candidate slots by walking actual body parts:
- `QuerySlotListEvent.GetFor(...)` (`QuerySlotListEvent.cs:31-56`)
- `Body.HandleEvent(QuerySlotListEvent)` delegates into tree (`Body.cs:3219-3226`)
- `BodyPart.ProcessQuerySlotList` checks each part (`BodyPart.cs:6549-6581`)

### 5.2 Default behavior can veto equip-over

`ProcessQuerySlotList` only offers a slot when default-behavior replacement is allowed:
- if default behavior has a `CanEquipOverDefaultBehavior` event and vetoes, slot is excluded (`BodyPart.cs:6551`)

This hook lets natural equipment/custom behavior reserve slots.

### 5.3 How item parts declare slot compatibility

Examples:
- `Shield` adds item if `E.SlotType == WornOn` and conditions pass (`Shield.cs:60-96`)
- `Armor` uses `WornOn` or `*`, with special handling for `Floating Nearby` (`Armor.cs:439-468`)
- `MeleeWeapon` checks `Slot` and desirability/possibility constraints (`MeleeWeapon.cs:191-227`)
- `MissileWeapon` checks `ValidSlotType(...)` (`MissileWeapon.cs:1231-1267`)
- `Physics` allows self-equip into `Thrown Weapon` slot for throwable items (`Physics.cs:2618-2638`)

### 5.4 Dynamic slot count requirement

If `UsesSlots` isn’t explicit, required slot count is computed via `GetSlotsRequiredEvent`:
- wrapper: `GameObject.GetSlotsRequiredFor` (`GameObject.cs:16674-16682`)
- event math: base + bitshift increases/decreases (`GetSlotsRequiredEvent.cs:47-103`)

Important handlers:
- `Physics.UsesTwoSlots` increments `Increases` (`Physics.cs:2547-2553`)
- `RequireSlots` mutates base/increase/decrease (`RequireSlots.cs:19-27`)
- `ModGigantic` can increase requirement for non-gigantic users (`ModGigantic.cs:186-192`)
- `GiantHands` can reduce hand/missile requirements for affected actors (`GiantHands.cs:10`, `26-33`)
- `NaturalEquipment`/`Cursed` can disable reduction (`NaturalEquipment.cs:36-42`, `Cursed.cs:55-61`)

So slot cost is a dynamic negotiation, not a fixed item stat.

---

## 6) Equip execution pipeline (player and NPC use same core)

### 6.1 Command path (`Inventory`)

`CommandEquipObject` flow (`Inventory.cs:1617+`):
1. Validate item viability (`Takeable`, not graveyard/invalid)
2. Check mobility constraints (unless forced)
3. Pull item into inventory if needed
4. Handle ownership warning for player
5. Query candidate body parts via `QuerySlotListEvent` (`Inventory.cs:1709`)
6. If player + multiple slots + no forced target, show slot picker (`Inventory.cs:1751-1779`)
7. Fire `BeginEquip` and item `BeginBeingEquipped`
8. Remove from previous context
9. Fire `PerformEquip`
10. On failure, rollback to previous context (`Inventory.cs:1981-1998`)

### 6.2 Perform path

`PerformEquip` (`Inventory.cs:1379-1483`):
- optionally unequips current item on target body part first
- delegates actual occupancy to `BodyPart.DoEquip(... UnequipOthers:true ...)` (`Inventory.cs:1437`)
- emits equip events/sound/messages

### 6.3 `BodyPart.DoEquip`: the hard logic

`DoEquip` (`BodyPart.cs:1701-2090`) handles:
- gigantic vs non-gigantic compatibility checks
- `UsesSlots` parsing (comma list + laterality adjectives)
- free-slot counting and optional auto-unequip of blockers
- informative failure messages when slots are insufficient
- choosing additional occupied parts by nearest position distance
- fallback dynamic slot count (`GetSlotsRequiredFor`) if no `UsesSlots`
- assignment into `_Equipped` or `DefaultBehavior`
- first-slot recomputation and cache flush

Notable behavior:
- laterality adjectives are parsed from slot text via `Laterality.GetCodeFromAdjective` (`BodyPart.cs:1733`, `Laterality.cs:219-284`)
- if item spans N slots, the same `GameObject` reference is written into N body parts; dedupe flags prevent multi-count side effects.

---

## 7) Laterality and anatomical side logic

Laterality is a bitmask system (`Laterality.cs`):
- left/right: `1/2`
- upper/lower: `4/8`
- fore/mid/hind: `16/32/64`
- plus inside/outside/inner/outer axes

`BodyPart.ChangeLaterality` updates:
- stored laterality bitmask
- name/description adjectives
- `RequiresLaterality` consistency (`BodyPart.cs:822-857`)

`Laterality.Match` is used heavily in slot/body-part queries (`Laterality.cs:361-380`).

This powers distinctions like left hand/right hand, fore/hind limbs, and mutation logic that retags existing limbs when adding new sets.

---

## 8) Dependency model and unsupported-limb collapse

Qud has two dependency channels:
- **Concrete** dependency: `DependsOn` references some other part’s `SupportsDependent`
- **Abstract** dependency: `RequiresType` + `RequiresLaterality`

Checks live in `BodyPart`:
- `IsConcretelyUnsupported` (`BodyPart.cs:5801-5816`)
- `IsAbstractlyUnsupported` (`BodyPart.cs:5818-5833`)
- `IsUnsupported` (`BodyPart.cs:5835-5842`)

Body-level maintenance:
- `CheckUnsupportedPartLoss()` finds unsupported parts and cuts them (`Body.cs:2268-2288`)
- `CheckPartRecovery()` reattaches recoverable parts when support returns (`Body.cs:2290-2305`)

These are run inside `UpdateBodyParts()` (`Body.cs:2353-2363`), so topology repairs/loss happen as part of regular body updates.

This is a major mechanic: you can "lose use" of parts from support loss even without direct dismember attacks.

---

## 9) Severability, mortal parts, and decapitation semantics

### 9.1 What is severable?

`BodyPart.IsSeverable()` requires (`BodyPart.cs:5844-5863`):
- not `Abstract`
- `Appendage == true`
- not `Integral`
- not dependent (`DependsOn`/`RequiresType`)

### 9.2 Mortal parts

`SeverRequiresDecapitate()` maps to `Mortal` (`BodyPart.cs:5865-5868`).

Axe skills treat mortal severing as decapitation path:
- `Axe_Dismember` routes to `Axe_Decapitate` for mortal parts (`Axe_Dismember.cs:85-88`)
- `Axe_Decapitate` kills if mortal parts are all gone (`Axe_Decapitate.cs:95-98`)

Body helpers:
- `AnyDismemberedMortalParts()` (`Body.cs:2035-2047`)
- `AnyMortalParts()` wrapper (`Body.cs:1084-1087`)

So mortal part loss is structurally integrated into death logic.

---

## 10) Dismemberment lifecycle in `Body.Dismember`

`Body.Dismember` (`Body.cs:2490-2619`) performs a full sequence:

1. **Gate check**
- `BeforeDismemberEvent.Check(...)` can veto (`Body.cs:2498`; event class `BeforeDismemberEvent.cs`)

2. **Pre-cut cleanup**
- stop movement (`2502`)
- unimplant any implant on that part (`2504`)
- unequip the part subtree (`2505`)

3. **Severed limb object creation** (unless extrinsic/obliterated)
- chooses blueprint from body-part-type limb metadata (`2509-2510`)
- assigns display, colors, description, properties
- serializes part metadata via `DismemberedProperties.SetFrom(Part)` (`2557`; part class in `DismemberedProperties.cs`)
- handles face special case (`2577-2594`)
- attaches carried implant payload if present (`2571-2575`)
- adds dropped limb to target inventory/cell (`2598`)

4. **Post events/messages**
- `AfterDismemberEvent.Send(...)` (`2601`)
- player popup + journal (`2602-2610`)

5. **Structural detach + follow-up**
- `CutAndQueueForRegeneration(Part)` (`2614`)
- `UpdateBodyParts()` and armor recalc (`2616-2617`)

`CutAndQueueForRegeneration` recursively detaches children, stores non-extrinsic detached parts in `DismemberedParts`, and clears primary flags (`Body.cs:2366-2402`).

---

## 11) Regeneration/recovery model

### 11.1 Regenerable vs recoverable

`BodyPart` semantics:
- `IsRegenerable()` => not abstract and not dependent (`BodyPart.cs:5870-5881`)
- `IsRecoverable()` => true for supported dependents or abstract-use restoration cases (`BodyPart.cs:5883-5890`)

Body query helpers:
- `FindRegenerablePart(...)` (`Body.cs:2114-2127`)
- `FindRecoverableParts()` (`Body.cs:2134-2156`)

### 11.2 Regrowth flow

`RegenerateLimb(...)` (`Body.cs:2621-2678`):
- finds eligible detached part by optional filters
- reattaches via `DismemberedPart.Reattach`
- supports whole-limb recursive reattachment
- triggers user-visible messages/sounds
- updates body afterward

Events:
- `RegenerateLimbEvent` and `AnyRegenerableLimbsEvent` both derive from `ILimbRegenerationEvent` and are handled by `Body` (`Body.cs:3263-3290`, event files).

### 11.3 Unsupported-part recovery

Even without explicit regen events, `UpdateBodyParts()` runs `CheckPartRecovery()` to reattach parts whose dependencies/support have returned (`Body.cs:2290-2305`).

---

## 12) Mobility penalties from limb loss

Mobility is per-part data (`BodyPart.Mobility`) aggregated by body.

When parts are dismembered, `CalculateMobilitySpeedPenalty` computes penalty from intact vs detached mobility contribution (`Body.cs:2406-2454`).

`UpdateMobilitySpeedPenalty` applies/removes stat bonus and `MobilityImpaired` effect (`Body.cs:2462-2487`).

Key constants:
- full mobility baseline = `2`
- max penalty clamp = `60` (`Body.cs:83-86`)

So limb loss has immediate locomotion consequences, not just flavor.

---

## 13) Missing-limb description synthesis

`GetMissingLimbsDescription` (`Body.cs:2737-2815`) distinguishes:
- physically missing concrete limbs
- abstract parts that represent "lost use" caused by support loss

It summarizes counts/names with grammar-aware phrases and includes both physical and functional loss when relevant.

This is why status text can report both missing anatomy and the cascading loss of use of dependent systems.

---

## 14) Implied parts engine (`ImpliedBy` / `ImpliedPer`)

Qud includes an inferred-part subsystem driven by `BodyPartType.ImpliedBy` and `ImpliedPer`.

Core methods in `Body`:
- `GetBodyPartsImplying` (`Body.cs:3303-3370`)
- `GetBodyPartCountImplyingInternal` (`3389-3441`)
- `DoesPartImplyPartInternal` (`3594-3651`)
- `ShouldRemoveDueToLackOfImplicationInternal` (`3524-3581`)

`CheckImpliedParts()` (`Body.cs:3700-3805`):
- computes missing implied parts and adds them
- removes no-longer-implied parts
- if changes happened, triggers body update + armor recalc

This is another dynamic topology system: body structure can auto-expand/shrink from implication rules.

---

## 15) Rebuild mechanics (full anatomy swap with preservation)

`Body.Rebuild(asAnatomy)` (`Body.cs:2817-3073`) is the most comprehensive body migration routine.

High-level sequence:
1. Snapshot top-level dynamic parts using `BodyPartPositionHint` (`2819-2835`)
2. Unimplant and temporarily stash cybernetics (`2837-2849`)
3. Unequip and stash equipped items (`2851-2875`, `2896-2917`)
4. Unmutate body/equipment-generating mutations (`2876-2894`)
5. Apply new anatomy template (`2918`)
6. Reinsert dynamic parts using ranked position hints (`2921-2941`)
7. Remutate saved mutations (`2944-2969`)
8. Reimplant cybernetics (`2971-3037`)
9. Auto-equip stashed gear (`3038-3044`)
10. Cleanup orphan references and update body (`3062-3070`)

`BodyPartPositionHint` scoring (`BodyPartPositionHint.cs:31-84`, `121-218`) is used to find best parent/position matches across topology changes.

---

## 16) Dynamic body-part producers: mutations, implants, and worn gear

A strong recurring pattern:
- create parts with deterministic `ManagerID`
- remove them via `RemoveBodyPartsByManager(..., EvenIfDismembered:true)`
- call `WantToReequip()` after structural changes

### 16.1 Mutation examples

- `MultipleArms` adds arm/hand/hands structures, may relateralize existing limbs, and tracks changed limbs via separate manager IDs (`MultipleArms.cs:14-17`, `58-130`, `147-158`)
- `MultipleLegs` adds/adjusts feet structures similarly (`MultipleLegs.cs:13-15`, `89-121`, `128-142`)
- `TwoHeaded` adds extra head/face and may relateralize existing head/face (`TwoHeaded.cs:13-16`, `163-199`, `201-212`)
- `Wings` uses default-equipment mutation flow and may add a new body part slot dynamically (`Wings.cs:341-370`, `396-418`)
- `Stinger` adds/reuses a tail part and equips mutation weapon behavior into that part (`Stinger.cs:251-284`, `303-363`)

### 16.2 Equipment part examples

Worn items can also mutate body topology:
- `ArmsOnEquip` adds configurable extra arm systems on equip, removes on unequip (`ArmsOnEquip.cs:119-177`)
- `HelpingHands` adds robo-arms/hands (`HelpingHands.cs:74-108`)
- `Waldopack` adds a servo arm when powered/equipped (`Waldopack.cs:104-130`)

### 16.3 Cybernetics examples

- `CyberneticsGraftedMirrorArm` adds `Thrown Weapon` slot (`CyberneticsGraftedMirrorArm.cs:29-33`)
- `CyberneticsGunRack` adds two integral `Hardpoint` parts and vetoes dismember of implant-bearing part (`CyberneticsGunRack.cs:25-40`, `50-56`)
- `CyberneticsEquipmentRack` adds an `Equipment Rack` body part (`CyberneticsEquipmentRack.cs:30-37`)
- `CyberneticsMotorizedTreads` adds tread parts and transforms implanted part metadata (`CyberneticsMotorizedTreads.cs:45-83`, `85-103`)
- `CyberneticsMagneticCore` adds `Floating Nearby` slot (`CyberneticsMagneticCore.cs:24-33`)

`CyberneticsBaseItem` enforces implant context and blocks normal unequip/remove while implanted (`CyberneticsBaseItem.cs:34-51`, `120-133`, `206-213`).

---

## 17) Player vs NPC behavior around the same body system

Core body/equip mechanics are shared, but orchestration differs.

### 17.1 Player-facing differences

- manual slot choice UI when multiple candidate body parts exist (`Inventory.cs:1751-1779`)
- explicit failure popup strings and ownership prompts (`Inventory.cs:1687-1703`, `1714-1727`, `1949-1975`)
- dismemberment popup/journal accomplishments (`Body.cs:2602-2610`)

### 17.2 NPC reequip automation

Non-player actors can request reequip after body changes:
- `GameObject.WantToReequip()` forwards to brain for non-player (`GameObject.cs:18723-18728`)
- `Brain.WantToReequip()` sets `DoReequip` (`Brain.cs:4347-4352`)
- `Brain.PerformReequip()` runs slot-wise equipment selection using inventory slot queries (`Brain.cs:3888+`, `3955-3958`)

So mutation/cybernetic/body-part changes stay synchronized with NPC equipment loadouts.

---

## 18) Body-part inventory generation (NPC content)

`BodyPartInventory` populates severed body parts into an actor’s inventory at creation time (`BodyPartInventory.cs:7-10`, `33-44`).

It uses `BodyPart.MakeSeveredBodyParts(...)`, which creates temporary creatures, dismembers valid severable parts, and transfers resulting limb items (`BodyPart.cs:6680-6733+`).

This is how NPCs can stock body-part items as trade goods/loot.

---

## 19) Important invariants and design implications

### 19.1 Invariants

1. **Topology is authoritative**: if a part node exists, it is a potential slot candidate.
2. **Equipment occupancy is per-body-part reference**, not global slot count.
3. **First-slot flags are required** for correctness under multi-slot occupancy.
4. **Detached parts remain structurally tracked** via `DismemberedParts` with parent IDs and positions.
5. **Body updates are maintenance passes** that regenerate default equipment, recalc firsts, enforce support loss/recovery, and apply mobility penalties.

### 19.2 Practical consequences

- Adding one new body-part type can ripple through equipability, implication, dependency, and dismemberment rules.
- Slot behavior is highly moddable because it is event- and data-driven.
- "Losing use" and "physically severed" are distinct states represented by dependency and abstract-part semantics.
- Rebuild/mutation/cybernetic operations must preserve positional and manager metadata to avoid equipment and dependency corruption.

---

## 20) Quick source map (highest-value anchors)

- Anatomy load and schema parsing: `XRL.World.Anatomy/Anatomies.cs`
- Anatomy application to runtime body: `XRL.World.Anatomy/Anatomy.cs`
- Body part type metadata contract: `XRL.World.Anatomy/BodyPartType.cs`
- Runtime body node behavior: `XRL.World.Anatomy/BodyPart.cs`
- Body-level orchestration and dismember/regrow: `XRL.World.Parts/Body.cs`
- Equip/unequip command pipeline: `XRL.World.Parts/Inventory.cs`
- Slot query event: `XRL.World/QuerySlotListEvent.cs`
- Item-side slot admissibility event: `XRL.World/QueryEquippableListEvent.cs`
- Dynamic slot requirement event: `XRL.World/GetSlotsRequiredEvent.cs`
- Laterality mechanics: `XRL.World.Capabilities/Laterality.cs`
- NPC reequip: `XRL.World.Parts/Brain.cs`
- Mutation/cybernetic slot/body extension examples:
  - `XRL.World.Parts.Mutation/MultipleArms.cs`
  - `XRL.World.Parts.Mutation/MultipleLegs.cs`
  - `XRL.World.Parts.Mutation/TwoHeaded.cs`
  - `XRL.World.Parts.Mutation/Wings.cs`
  - `XRL.World.Parts.Mutation/Stinger.cs`
  - `XRL.World.Parts/CyberneticsGunRack.cs`
  - `XRL.World.Parts/CyberneticsGraftedMirrorArm.cs`

---

## 21) Notes on this decompiled source snapshot

- Anatomy data is loaded from `Bodies` XML (`Anatomies.cs:75`), but XML data files are not included in this code tree snapshot; this document therefore focuses on engine mechanics and execution flow in code.
- The system is heavily event-driven; mods/parts can alter slot eligibility, slot counts, dismemberability, and equip-over-default behavior without changing core body classes.
