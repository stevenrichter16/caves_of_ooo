# Caves of Qud: Body & Anatomy System — Deep Source Code Dive

This document is a comprehensive source-code analysis of how Caves of Qud implements its body and anatomy system, covering the body part tree, equipment slots, limb severing and regrowth, cybernetics, mutations, and all related mechanics.

**Source files referenced:** All paths relative to the decompiled project root.

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [Data Layer: Anatomy Definitions](#2-data-layer-anatomy-definitions)
3. [The BodyPart Class — Complete Field Reference](#3-the-bodypart-class)
4. [The Body Part Tree](#4-the-body-part-tree)
5. [Body Part Types and Categories](#5-body-part-types-and-categories)
6. [The Flag System (Bitmask Properties)](#6-the-flag-system)
7. [Laterality System](#7-laterality-system)
8. [Equipment Slot System](#8-equipment-slot-system)
9. [The Equip Pipeline](#9-the-equip-pipeline)
10. [DefaultBehavior / Natural Weapons](#10-defaultbehavior--natural-weapons)
11. [Cybernetics and Body Parts](#11-cybernetics-and-body-parts)
12. [Dismemberment — Complete Lifecycle](#12-dismemberment)
13. [Regeneration and Regrowth](#13-regeneration-and-regrowth)
14. [Dependency System (SupportsDependent / DependsOn)](#14-dependency-system)
15. [Implied Parts Engine](#15-implied-parts-engine)
16. [Mobility and Movement Penalties](#16-mobility-and-movement-penalties)
17. [Mutations That Modify Body Parts](#17-mutations-that-modify-body-parts)
18. [Equipment That Modifies Body Parts](#18-equipment-that-modifies-body-parts)
19. [Cybernetics That Modify Anatomy](#19-cybernetics-that-modify-anatomy)
20. [Special Slot Types](#20-special-slot-types)
21. [Shield System](#21-shield-system)
22. [Body Rebuild (Full Anatomy Swap)](#22-body-rebuild)
23. [Chimera System](#23-chimera-system)
24. [Source File Index](#24-source-file-index)

---

## 1. Architecture Overview

Qud does **not** use a fixed equipment-slot enum. Instead it uses:

1. **Anatomy definitions** (`Anatomy`, `BodyPartType`, `AnatomyPart`) loaded from XML at startup
2. A per-creature **runtime tree** of `BodyPart` nodes managed by a `Body` component
3. **Item-side equipability rules** via `QueryEquippableListEvent` handlers
4. **Body-side slot query and equip logic** via `QuerySlotListEvent` and `BodyPart.DoEquip`

A slot like "Face" exists only because the body part tree includes a node of type "Face". The equip system asks *"which body parts on this actor can accept this item right now?"* rather than indexing into global slots.

**Core source files:**
- `XRL.World.Anatomy/Anatomies.cs` — Global anatomy/type registry, XML loading
- `XRL.World.Anatomy/Anatomy.cs` — Anatomy template definition
- `XRL.World.Anatomy/AnatomyPart.cs` — Part node in an anatomy template
- `XRL.World.Anatomy/BodyPartType.cs` — Metadata contract for each part type
- `XRL.World.Anatomy/BodyPart.cs` — Runtime body part node (7,108 lines, 156KB)
- `XRL.World.Anatomy/BodyPartCategory.cs` — Category constants and utilities
- `XRL.World.Parts/Body.cs` — Body component managing the tree (3,858 lines, 99KB)
- `XRL.World.Capabilities/Laterality.cs` — Laterality bitmask system

---

## 2. Data Layer: Anatomy Definitions

### 2.1 Initialization (`Anatomies.cs`)

`Anatomies.CheckInit()` lazily loads all XML files rooted at `"Bodies"`:

```csharp
// Anatomies.cs:61-86
public static void CheckInit()
{
    if (_BodyPartTypeTable == null)
        Loading.LoadTask("Loading Bodies.xml", Init);
}

private static void Init()
{
    _BodyPartTypeTable = new Dictionary<string, BodyPartType>(32);
    _BodyPartTypeList = new List<BodyPartType>(32);
    _AnatomyTable = new Dictionary<string, Anatomy>(64);
    _AnatomyList = new List<Anatomy>(64);
    foreach (DataFile item in DataManager.GetXMLFilesWithRoot("Bodies"))
        ProcessBodiesXmlFile(item, item.IsMod);
}
```

The system supports mod-friendly removal/override via `removebodyparttype` and `removebodyparttypevariant` XML directives.

### 2.2 Anatomy Class (`Anatomy.cs`)

An `Anatomy` is a named template with a list of `AnatomyPart` nodes:

```csharp
public class Anatomy
{
    public string Name;
    public List<AnatomyPart> Parts = new List<AnatomyPart>();
    public int? Category;
    public string BodyType = "Body";        // Root part type
    public int? BodyCategory;
    public int? BodyMobility;
    public string ThrownWeapon = "Thrown Weapon";    // Auto-added abstract slot
    public string FloatingNearby = "Floating Nearby"; // Auto-added abstract slot
}
```

### 2.3 Applying an Anatomy to a Body (`Anatomy.ApplyTo`)

```csharp
// Anatomy.cs:31-64
public void ApplyTo(Body body)
{
    body.built = false;
    body._Anatomy = Name;
    body.DismemberedParts = null;
    // Create root body part
    BodyPart bodyPart = (body._Body = new BodyPart(BodyType, body, Parts.Count));
    if (BodyCategory.HasValue) bodyPart.Category = BodyCategory.Value;
    if (BodyMobility.HasValue) bodyPart.Mobility = BodyMobility.Value;
    // Apply all template parts recursively
    foreach (AnatomyPart part in Parts)
        part.ApplyTo(bodyPart);
    // Auto-add abstract slots
    if (!ThrownWeapon.IsNullOrEmpty()) bodyPart.AddPart(ThrownWeapon);
    if (!FloatingNearby.IsNullOrEmpty()) bodyPart.AddPart(FloatingNearby);
    // Apply category to all parts
    if (Category.HasValue) bodyPart.CategorizeAll(Category.Value);
    body.MarkAllNative();
    body.built = true;
    body.UpdateBodyParts();
}
```

### 2.4 AnatomyPart — Template Nodes (`AnatomyPart.cs`)

Each `AnatomyPart` carries a `BodyPartType` reference plus per-node overrides:

```csharp
public class AnatomyPart
{
    public BodyPartType Type;
    public string SupportsDependent;    // Dependency provider anchor
    public string DependsOn;            // Concrete dependency pointer
    public string RequiresType;         // Abstract dependency
    public string DefaultBehavior;      // Override natural weapon blueprint
    public int? Category, Laterality, RequiresLaterality, Mobility;
    public bool? Integral, Mortal, Abstract, Extrinsic, Plural, Mass, Contact, IgnorePosition;
    public List<AnatomyPart> Subparts = new List<AnatomyPart>(0);
}
```

`ApplyTo(BodyPart parent)` creates a new runtime `BodyPart` from the type and recursively applies subparts:

```csharp
public void ApplyTo(BodyPart parent)
{
    BodyPart newPart = parent.AddPart(Type, Laterality.GetValueOrDefault(), DefaultBehavior,
        SupportsDependent, DependsOn, RequiresType, /*Manager*/null, Category,
        RequiresLaterality, Mobility, /*Appendage*/null, Integral, Mortal, Abstract,
        Extrinsic, /*Dynamic*/null, Plural, Mass, Contact, IgnorePosition);
    foreach (AnatomyPart subpart in Subparts)
        subpart.ApplyTo(newPart);
}
```

---

## 3. The BodyPart Class

`BodyPart` (`XRL.World.Anatomy/BodyPart.cs`, 7,108 lines) is the fundamental runtime node. Every field is listed below.

### 3.1 Serialized Fields

| Field | Type | Default | Purpose |
|---|---|---|---|
| `Type` | `string` | — | Part type key (e.g., `"Hand"`, `"Head"`, `"Body"`) |
| `VariantType` | `string` | — | Original type before variant resolution |
| `Description` | `string` | — | Display description (e.g., `"Right Hand"`) |
| `DescriptionPrefix` | `string` | — | Prefix for display |
| `Name` | `string` | — | Lowercase name (e.g., `"right hand"`) |
| `SupportsDependent` | `string` | — | Declares "I support a dependent part with this tag" |
| `DependsOn` | `string` | — | "I need a part with `SupportsDependent == this`" |
| `RequiresType` | `string` | — | Abstract dependency: need a part of this type |
| `Manager` | `string` | — | Manager ID (tracks which mutation/system added this part) |
| `Category` | `int` | `1` | Material category (Animal=1, Cybernetic=6, etc.) |
| `Laterality` | `int` | `0` | Bitmask: left/right/upper/lower/fore/hind |
| `RequiresLaterality` | `int` | `65535` | Laterality mask required from environment (65535=any) |
| `Mobility` | `int` | `0` | Movement speed contribution |
| `Flags` | `int` | `4096` | Bitmask for boolean properties (Contact=true by default) |
| `DefaultBehaviorBlueprint` | `string` | — | Blueprint ID for auto-creating natural weapon |
| `Position` | `int` | `-1` | Ordering value within parent's children |
| `_ID` | `int` | auto | Unique ID from `The.Game.BodyPartIDSequence` |

### 3.2 Runtime References (Non-Serialized)

| Field | Type | Purpose |
|---|---|---|
| `ParentBody` | `Body` | The `Body` component this part belongs to |
| `ParentPart` | `BodyPart` | Parent in the tree hierarchy |
| `Parts` | `List<BodyPart>` | Child body parts (null if leaf) |
| `_Equipped` | `GameObject` | Regular equipment item in this slot |
| `_Cybernetics` | `GameObject` | Cybernetic implant installed here |
| `_DefaultBehavior` | `GameObject` | Natural weapon/default item (fist, hoof, horn) |

---

## 4. The Body Part Tree

Body parts form a tree rooted at `Body._Body`. Each node has:
- `ParentPart` — reference to immediate parent
- `Parts` — list of children (sorted by `Position`)
- `ParentBody` — reference to owning `Body` component

### Tree Traversal Methods

```csharp
// Depth-first: self + all descendants
public IEnumerable<BodyPart> LoopParts()

// Descendants only (not self)
public IEnumerable<BodyPart> LoopOtherParts()

// Direct children only
public IEnumerable<BodyPart> LoopSubparts()

// Recursive flatten into lists
public void GetParts(List<BodyPart> Return)          // self + all descendants
public void GetConcreteParts(List<BodyPart> Return)   // skips Abstract parts
public void GetAbstractParts(List<BodyPart> Return)    // only Abstract parts

// Navigation
public BodyPart GetParentPart()
public BodyPart GetPreviousPart()
public BodyPart GetNextPart()
public bool IsAncestorPartOf(BodyPart FindPart)       // recursive contains
```

### Example: Standard Humanoid Body Tree

```
Body (root)
├── Head [Mortal, Appendage]
│   └── Face [DependsOn: Head]
├── Arm (right) [Appendage]
│   └── Hand (right) [DependsOn: Arm, DefaultBehavior: "DefaultFist"]
│       └── Hands (right) [DependsOn: Hand, RequiresType: Hand]
├── Arm (left) [Appendage]
│   └── Hand (left) [DependsOn: Arm, DefaultBehavior: "DefaultFist"]
│       └── Hands (left) [DependsOn: Hand, RequiresType: Hand]
├── Back [Appendage]
├── Body [SupportsDependent: "Body"]
├── Feet [Appendage, Mobility]
├── Missile Weapon [Abstract]
├── Thrown Weapon [Abstract]
└── Floating Nearby [Abstract]
```

### Position System

Children are ordered by integer `Position`. Key behaviors:
- `AddPart()` assigns `NextPosition()` (one beyond highest existing) if position not specified
- `AssignPosition()` bumps existing positions up if collision occurs
- **Dismembered parts retain position reservations** — checked via `DismemberedPartHasPosition()`
- This ensures stable ordering through dismemberment/regeneration cycles

---

## 5. Body Part Types and Categories

### 5.1 BodyPartType (`BodyPartType.cs`)

`BodyPartType` carries behavior-defining metadata loaded from XML:

```csharp
public class BodyPartType
{
    public string Type;              // Type key (e.g., "Hand")
    public string FinalType;         // Resolved type after variant application
    public string Description;       // Display name
    public string Name;              // Lowercase name
    public string DefaultBehavior;   // Blueprint for natural weapon (e.g., "DefaultFist")
    public string UsuallyOn;         // Where this type is usually attached
    public string UsuallyOnVariant;  // Variant-specific attachment
    public string ImpliedBy;         // This type is implied by N of another type
    public int? ImpliedPer;          // How many implying parts create one implied (default 2)
    public string RequiresType;      // Abstract dependency
    public string LimbBlueprintProperty = "SeveredLimbBlueprint";
    public string LimbBlueprintDefault = "GenericLimb";
    public int? ChimeraWeight;       // Weight for random chimeric body part selection
    public bool? Appendage;          // Can be severed
    public bool? Integral;           // Cannot be severed even if appendage
    public bool? Abstract;           // Not physical (Missile Weapon, Thrown Weapon)
    public bool? Mortal;             // Severing requires decapitation; kills
    public bool? Extrinsic;          // Added externally, not part of base anatomy
    public int[] Branching;          // Branching topology hints
}
```

`ApplyTo(BodyPart)` stamps all defined values onto a runtime part.

### 5.2 BodyPartCategory (`BodyPartCategory.cs`)

Categories define the material nature of body parts:

| Constant | Value | Electrical Conductivity | Live? | Can Receive Implants? |
|---|---|---|---|---|
| `ANIMAL` | 1 | 80 | Yes | **Yes** |
| `ARTHROPOD` | 2 | 70 | Yes | No |
| `PLANT` | 3 | 90 | Yes | No |
| `FUNGAL` | 4 | 60 | Yes | No |
| `PROTOPLASMIC` | 5 | 90 | Yes | No |
| `CYBERNETIC` | 6 | 70 | No | No |
| `MECHANICAL` | 7 | 90 | No | No |
| `METAL` | 8 | 100 | No | No |
| `WOODEN` | 9 | 20 | No | No |
| `STONE` | 10 | 10 | No | No |
| `GLASS` | 11 | 0 | No | No |
| `LEATHER` | 12 | 0 | No | No |
| `BONE` | 13 | 0 | No | No |
| `CHITIN` | 14 | 10 | No | No |
| `PLASTIC` | 15 | 0 | No | No |
| `CLOTH` | 16 | 0 | No | No |
| `PSIONIC` | 17 | 30 | No | No |
| `EXTRADIMENSIONAL` | 18 | 80 | Yes | No |
| `MOLLUSK` | 19 | 80 | Yes | No |
| `JELLY` | 20 | 90 | Yes | No |
| `CRYSTAL` | 21 | 10 | No | No |
| `LIGHT` | 22 | 0 | No | No |
| `LIQUID` | 23 | 100 | No | No |

**Only Category 1 (ANIMAL) + non-Extrinsic parts can receive cybernetic implants.**

```csharp
public bool CanReceiveCyberneticImplant()
{
    if (Extrinsic) return false;
    if (Category != 1) return false;
    return true;
}
```

---

## 6. The Flag System

All boolean properties are stored in the `Flags` bitmask field (default `4096` = Contact only):

| Constant | Value | Property | Meaning |
|---|---|---|---|
| `FLAG_DEFAULT_PRIMARY` | 1 | `DefaultPrimary` | Default primary slot for its type |
| `FLAG_PREFERRED_PRIMARY` | 2 | `PreferredPrimary` | Player manually set as preferred primary |
| `FLAG_PRIMARY` | 4 | `Primary` | Currently the primary slot (computed) |
| `FLAG_NATIVE` | 8 | `Native` | Part of original body anatomy |
| `FLAG_APPENDAGE` | 16 | `Appendage` | Can be dismembered/severed |
| `FLAG_INTEGRAL` | 32 | `Integral` | Cannot be severed even if appendage |
| `FLAG_MORTAL` | 64 | `Mortal` | Severing = decapitation; killing blow |
| `FLAG_ABSTRACT` | 128 | `Abstract` | Not physical (Missile Weapon, Thrown Weapon slots) |
| `FLAG_EXTRINSIC` | 256 | `Extrinsic` | Added externally; skipped during DeepCopy |
| `FLAG_DYNAMIC` | 512 | `Dynamic` | Dynamically added (mutation-added parts) |
| `FLAG_PLURAL` | 1024 | `Plural` | Grammatically plural (e.g., "Feet") |
| `FLAG_MASS` | 2048 | `Mass` | Mass body part |
| `FLAG_CONTACT` | 4096 | `Contact` | In contact with ground/environment |
| `FLAG_IGNORE_POSITION` | 8192 | `IgnorePosition` | Don't ordinalize in display |
| `FLAG_FIRST_EQUIPPED` | 16384 | `FirstSlotForEquipped` | Primary slot for multi-slot equipped item |
| `FLAG_FIRST_CYBERNETIC` | 32768 | `FirstSlotForCybernetics` | Primary slot for multi-slot cybernetic |
| `FLAG_FIRST_DEFAULT` | 65536 | `FirstSlotForDefaultBehavior` | Primary slot for multi-slot default behavior |

### First-Slot Deduplication

When one object occupies multiple body parts (e.g., a two-handed weapon in two Hand slots), exactly one slot is marked as primary via the `FirstSlotFor*` flags. This prevents:
- Double-counting weight and armor stats
- Duplicate event dispatch
- Multiple counts in equipped object enumeration

---

## 7. Laterality System

Laterality is a bitmask system (`XRL.World.Capabilities/Laterality.cs`):

| Constant | Value | Adjective |
|---|---|---|
| `NONE` | 0 | (none) |
| `LEFT` | 1 | "left" |
| `RIGHT` | 2 | "right" |
| `UPPER` | 4 | "upper" |
| `LOWER` | 8 | "lower" |
| `FORE` | 16 | "fore" |
| `MID` | 32 | "mid" |
| `HIND` | 64 | "hind" |
| `INSIDE` | 128 | "inside" |
| `OUTSIDE` | 256 | "outside" |
| `INNER` | 512 | "inner" |
| `OUTER` | 1024 | "outer" |
| `ANY` | 65535 | (wildcard) |

These compose via bitwise OR. A "fore right hand" has laterality `RIGHT | FORE = 18`.

**Axes** group related values:
- Lateral axis: LEFT (1) / RIGHT (2), mask 3
- Vertical axis: UPPER (4) / LOWER (8), mask 12
- Longitudinal axis: FORE (16) / HIND (64) / MID (32), mask 112
- Superficial axis: INSIDE (128) / OUTSIDE (256), mask 384
- Stratal axis: INNER (512) / OUTER (1024), mask 1536

`ChangeLaterality()` updates the laterality bitmask, regenerates name/description with proper adjective, and optionally applies recursively to children:

```csharp
public BodyPart ChangeLaterality(int NewLaterality, bool Recursive = false)
```

---

## 8. Equipment Slot System

### 8.1 Three Equipment Channels Per Body Part

Each body part can hold three independent `GameObject` references:

1. **`_Equipped`** — Regular equipment (swords, armor, shields)
2. **`_Cybernetics`** — Cybernetic implant installed in this slot
3. **`_DefaultBehavior`** — Natural weapon/default item (fists, hooves, horns)

These coexist unless the implant has the `CyberneticsUsesEqSlot` tag, which forces it to also occupy the equipment slot.

### 8.2 Slot Compatibility is String-Based

Slot compatibility is keyed by body-part type strings (`"Hand"`, `"Head"`, `"Face"`, `"Back"`, `"Feet"`, `"Body"`, etc.), not enum constants.

**Item side** declares requirements via:
- `Physics.UsesSlots` — comma-separated slot type list
- Per-part `QueryEquippableListEvent` handlers (`MeleeWeapon`, `MissileWeapon`, `Shield`, `Armor`)

**Body side** enumerates candidate slots by walking the body part tree:
- `QuerySlotListEvent.GetFor(...)` queries all available slots
- `BodyPart.ProcessQuerySlotList` checks each part and filters by availability

### 8.3 Dynamic Slot Count

If `UsesSlots` isn't explicit, required slot count is computed via `GetSlotsRequiredEvent`:
- `Physics.UsesTwoSlots` increments requirement
- `RequireSlots` can mutate base/increase/decrease
- `ModGigantic` increases requirement for non-gigantic users
- `GiantHands` reduces hand/missile requirements
- `NaturalEquipment` prevents reduction

Slot cost is a dynamic negotiation, not a fixed item stat.

### 8.4 Multi-Slot Items

Items can occupy multiple body parts simultaneously. The same `GameObject` reference is written into N body parts. The `FirstSlotFor*` flags ensure events only fire once.

When finding additional slots for a multi-slot item, the system picks the **closest available slot by position distance** (`Math.Abs(bodyPart2.Position - Position)`).

---

## 9. The Equip Pipeline

### 9.1 Command Path (`Inventory.cs`)

`CommandEquipObject` flow:
1. Validate item viability (Takeable, not graveyard/invalid)
2. Check mobility constraints
3. Pull item into inventory if needed
4. Handle ownership warning for player
5. Query candidate body parts via `QuerySlotListEvent`
6. If player + multiple slots + no forced target: show slot picker UI
7. Fire `BeginEquip` and `BeginBeingEquipped`
8. Remove from previous context
9. Fire `PerformEquip`
10. On failure, rollback to previous context

### 9.2 BodyPart.DoEquip — The Core Logic

`DoEquip` (`BodyPart.cs:1701-2090`) handles:

1. **Gigantic compatibility checks** — Gigantic creatures can only equip gigantic equipment (with exceptions for Hand, Floating Nearby, natural items). Non-gigantic creatures cannot equip gigantic equipment.

2. **UsesSlots parsing** — Comma-separated list with laterality adjectives parsed via `Laterality.GetCodeFromAdjective`

3. **Free-slot counting** — Checks availability, optionally auto-unequips blockers if `UnequipOthers` is true

4. **Multi-slot allocation** — For items needing 2+ slots, finds closest available slots by position distance

5. **DefaultBehavior routing** — If `GO.EquipAsDefaultBehavior()` returns true, routes to `DefaultBehavior` instead of `_Equipped`

6. **Post-equip updates** — Recalculates first-slot flags, flushes caches

### 9.3 Unequip Methods

```csharp
// Fires CommandUnequipObject — can be vetoed
public bool TryUnequip(bool Silent = false, bool SemiForced = false, ...)

// Fires CommandForceUnequipObject — stronger than TryUnequip
public bool ForceUnequip(bool Silent = false, ...)

// Low-level: clears Equipped on ALL parts referencing the same item
public void Unequip()
```

---

## 10. DefaultBehavior / Natural Weapons

### 10.1 How Natural Weapons Work

Each body part type can declare a `DefaultBehavior` blueprint in XML (e.g., `"DefaultFist"` for Hands). When the body is initialized or updated, `Body.RegenerateDefaultEquipment()` creates `GameObjects` from these blueprints:

```csharp
// Body.cs:2307-2351
public void RegenerateDefaultEquipment()
{
    GetParts(list);
    foreach (BodyPart item in list)
    {
        GameObject gameObject = item.DefaultBehavior;
        // Destroy old if it matches blueprint or is temporary
        if (gameObject != null && ((item.DefaultBehaviorBlueprint != null &&
            gameObject.Blueprint == item.DefaultBehaviorBlueprint) ||
            gameObject.HasTagOrProperty("TemporaryDefaultBehavior")))
        {
            gameObject.Obliterate();
            gameObject = (item._DefaultBehavior = null);
        }
        // Recreate from blueprint if missing
        if (gameObject == null && item.DefaultBehaviorBlueprint != null)
        {
            item.DefaultBehavior = GameObject.CreateUnmodified(item.DefaultBehaviorBlueprint);
            item.FirstSlotForDefaultBehavior = true;
        }
    }
    RegenerateDefaultEquipmentEvent.Send(ParentObject, this);
    DecorateDefaultEquipmentEvent.Send(ParentObject, this);
}
```

### 10.2 EquipAsDefaultBehavior Check

An item is routed to `DefaultBehavior` (instead of `_Equipped`) if:

```csharp
public bool EquipAsDefaultBehavior()
{
    if (!IsNatural()) return false;         // Must have "Natural" or "NaturalGear" tag
    if (HasPart<Armor>() && !HasTagOrProperty("AllowArmorDefaultBehavior")) return false;
    if (HasPart<Shield>() && !HasTagOrProperty("AllowShieldDefaultBehavior")) return false;
    if (HasTagOrProperty("NoDefaultBehavior")) return false;
    return true;
}
```

### 10.3 NaturalEquipment Part (`NaturalEquipment.cs`)

Prevents normal unequip of natural weapons:

```csharp
public override bool HandleEvent(CanBeUnequippedEvent E)
{
    if (!E.Forced) return false;  // Block normal unequip
    return base.HandleEvent(E);
}

public override bool HandleEvent(GetSlotsRequiredEvent E)
{
    if (E.Object == ParentObject)
        E.AllowReduction = false;  // Cannot reduce slot count
    return base.HandleEvent(E);
}
```

### 10.4 Weapon Selection Priority in Combat

`ScanForWeapon` determines which weapon is used. Priority hierarchy:

| Priority | Condition |
|---|---|
| 20 | `PreferredPrimary` but not `DefaultPrimary` |
| 10 | `Primary` with equipped item |
| 9 | Non-primary with `PreferredDefault` tag |
| 8 | `Primary` with default behavior only |
| 7 | Hand type slot |
| 6 | Non-hand, non-root |
| 5 | Body root |
| -4 penalty | `UndesirableWeapon` tag on DefaultBehavior |

`PreferDefaultBehaviorEvent.Check()` can override to use DefaultBehavior over Equipped.

---

## 11. Cybernetics and Body Parts

### 11.1 The Three-Channel Model

Each body part independently tracks `_Equipped`, `_Cybernetics`, and `_DefaultBehavior`. They coexist unless `CyberneticsUsesEqSlot` forces overlap:

```csharp
// BodyPart.cs:1639-1646
public void Implant(GameObject Cybernetic, ...)
{
    if (_Cybernetics != null) Unimplant();
    _Cybernetics = Cybernetic;
    if (Cybernetic.HasTag("CyberneticsUsesEqSlot"))
    {
        if (_Equipped != null && !TryUnequip()) ForceUnequip(Silent);
        Equip(Cybernetic, ...); // Also occupies equipment slot
    }
    ImplantedEvent.Send(...);
}
```

### 11.2 Implantability Rules

```csharp
public bool CanReceiveCyberneticImplant()
{
    if (Extrinsic) return false;   // Can't implant into externally-added parts
    if (Category != 1) return false; // Only ANIMAL (biological) category
    return true;
}
```

This prevents implanting into cybernetically-added parts (Category 6), temporary parts, or non-biological anatomy.

### 11.3 CyberneticsBaseItem (`CyberneticsBaseItem.cs`)

Each implant declares target body part types via the `Slots` field:

```csharp
public string Slots;  // e.g., "Head", "Head,Face", "Arm", "Hand"
```

Key behaviors:
- Blocks normal unequip/remove while implanted
- Declares license point cost
- Shows "Only compatible with True Kin genotypes" in description

### 11.4 Event Cascading for Cybernetics

Events cascade through all three channels independently. Cybernetics get their own event dispatch (separate from equipment):

```csharp
// BodyPart.cs:3460-3470
if (Cybernetics != null && Cybernetics != Equipped && FirstSlotForCybernetics)
{
    if (!Cybernetics.FireEvent(E)) flag = false;
    if (E.ID == "EndTurn") Cybernetics?.CleanEffects();
}
```

The `Cybernetics != Equipped` check prevents double-firing when a cybernetic also occupies the equipment slot.

### 11.5 True Kin vs Mutant

- True Kin genotypes have positive `CyberneticsLicensePoints` — can freely install implants
- Mutant genotypes have 0 points — the Cybernetic Rejection Syndrome system is coded but effectively disabled in the base game (the chance calculation always returns 0)

---

## 12. Dismemberment

### 12.1 Sources of Dismemberment

| Source | File | Trigger |
|---|---|---|
| `Axe_Dismember` | `XRL.World.Parts.Skill/Axe_Dismember.cs` | Activated ability (30-turn CD) + passive 3%/6% on penetrating axe hit |
| `Axe_Decapitate` | `XRL.World.Parts.Skill/Axe_Decapitate.cs` | Redirected from Dismember when target part is Mortal |
| `ModSerrated` | `XRL.World.Parts/ModSerrated.cs` | 3% per hit on long blades/axes; 1/1000 decap chance |
| `ModGlazed` | `XRL.World.Parts/ModGlazed.cs` | `10 + Tier*2`% chance on hit |
| `DismemberAdjacentHostiles` | `XRL.World.Parts/DismemberAdjacentHostiles.cs` | Passive check all adjacent hostiles each turn |
| `Physic_AmputateLimb` | `XRL.World.Parts.Skill/Physic_AmputateLimb.cs` | Player picks target + part; requires axe/serrated/glazed; 3000 energy |
| Cooking effect | `XRL.World.Effects/CookingDomain...` | 50% chance dismember or slam |

### 12.2 Severability Check

```csharp
public bool IsSeverable()
{
    if (Abstract) return false;       // Can't sever abstract parts
    if (!Appendage) return false;     // Must be an appendage
    if (Integral) return false;       // Integral parts can't be severed
    if (IsDependent()) return false;  // Dependent parts can't be severed directly
    return true;
}

public bool SeverRequiresDecapitate() => Mortal;
```

### 12.3 IntegralAnatomy (`IntegralAnatomy.cs`)

This part marks body parts as `Integral = true` on creation, preventing severance. Can filter by:
- `IncludeTypes` — comma-separated whitelist of types
- `ExcludeTypes` — types to exclude
- `MortalOnly` — only protect mortal parts

### 12.4 Mortal Parts and Death

Parts with the `Mortal` flag (typically heads) require `Axe_Decapitate` to sever. If all mortal parts are dismembered and none remain:

```csharp
// Axe_Decapitate.cs:95-98
if (body.AnyDismemberedMortalParts() && !body.AnyMortalParts())
{
    Defender.Die(Attacker, ...);
}
```

### 12.5 The Dismember Flow (`Axe_Dismember.Dismember`)

```csharp
public static bool Dismember(GameObject Attacker, GameObject Defender, ...)
{
    // 1. Pick random dismemberable part if none specified
    if (LostPart == null)
        LostPart = GetDismemberableBodyPart(Defender, Attacker, Weapon, ...);

    // 2. If mortal, redirect to Decapitate
    if (LostPart.SeverRequiresDecapitate())
        return Axe_Decapitate.Decapitate(...);

    // 3. Call Body.Dismember — THE CORE METHOD
    if (Defender.Body.Dismember(LostPart, Attacker, Where, ...) == null)
        return false;

    // 4. Apply bleeding, play sound
    Defender.ApplyEffect(new Bleeding(BleedDamage, BleedSave, Attacker, Stack: true));
    return true;
}
```

### 12.6 Body.Dismember — The Core Method

```csharp
// Body.cs:2490-2619
public GameObject Dismember(BodyPart Part, GameObject Actor = null,
    IInventory Where = null, bool Obliterate = false, ...)
{
    // 1. GATE CHECK — BeforeDismemberEvent can veto (unless Obliterate)
    if (!BeforeDismemberEvent.Check(...) && !Obliterate) return null;

    ParentObject.StopMoving();

    // 2. UNIMPLANT CYBERNETICS from the part
    GameObject gameObject2 = Part.Unimplant();

    // 3. UNEQUIP ALL ITEMS on this part and children
    Part.UnequipPartAndChildren(Silent: false, Where);

    // 4. CREATE SEVERED LIMB OBJECT (unless Extrinsic or Obliterate)
    if (!Part.Extrinsic && !Obliterate)
    {
        // Blueprint from BodyPartType metadata (default: "GenericLimb")
        gameObject = GameObject.Create(
            ParentObject.GetPropertyOrTag(bodyPartType.LimbBlueprintProperty,
                                          bodyPartType.LimbBlueprintDefault));

        // Name: "Mehmet's left arm"
        render.DisplayName = Grammar.MakePossessive(displayName) + ' ' + Part.Name;

        // Description: "Dried blood crusts on the severed arm of a snapjaw."
        // Color properties copied from source creature
        // DismemberedProperties component attached (stores source info)
        gameObject.RequirePart<DismemberedProperties>().SetFrom(Part);

        // Gigantic creatures produce gigantic limbs
        if (ParentObject.IsGiganticCreature)
            gameObject.IsGiganticEquipment = true;

        // SPECIAL: Face parts become equippable Face armor
        if (Part.Type == "Face")
        {
            Armor armor = gameObject.RequirePart<Armor>();
            armor.WornOn = "Face";
            // Ego bonus based on creature level
            // Faction reputation modifiers
        }

        // If cybernetics were on it, attach for butchering recovery
        if (gameObject2 != null)
        {
            gameObject.AddPart(new CyberneticsButcherableCybernetic(gameObject2));
            gameObject.RemovePart<Food>();  // Can't eat a cybernetic limb
        }

        // Drop the limb into the world
        inventory?.AddObjectToInventory(gameObject, ...);
    }

    // 5. POST EVENTS
    AfterDismemberEvent.Send(Actor, ParentObject, gameObject, Part, ...);

    // 6. PLAYER MESSAGES
    if (ParentObject.IsPlayer() && !Obliterate)
        Popup.Show("Your " + ordinalName + " is dismembered!");

    // 7. QUEUE FOR REGENERATION
    if (!Obliterate)
        CutAndQueueForRegeneration(Part);

    // 8. UPDATE BODY
    UpdateBodyParts();
    RecalculateArmor();

    return gameObject;  // The severed limb object
}
```

### 12.7 Equipment Handling During Severing

`UnequipPartAndChildren` processes each part in the subtree:

- **Real non-natural equipment** (swords, armor): Force-unequipped and **dropped** to ground/inventory
- **Natural equipment** (fists, hooves): Simply unequipped and destroyed
- **Cybernetics**: Unimplanted separately; attached to the severed limb object as `CyberneticsButcherableCybernetic` for butchering recovery

### 12.8 DismemberedProperties

The severed limb object carries metadata about its source:

```csharp
public class DismemberedProperties : IPart
{
    public string SourceID;         // ID of source creature
    public string SourceBlueprint;  // Blueprint of source creature
    public string SourceGenotype;   // Genotype (mutant, true kin, etc.)
    public string SourceBlood;      // Bleed liquid
    public BodyPart BodyPart;       // Deep copy of body part at dismemberment time
}
```

### 12.9 BodyPartInventory — Pre-Populating Severed Limbs

`BodyPartInventory` creates severed body parts as inventory items for merchants. Uses `BodyPart.MakeSeveredBodyPart()` which creates a temporary creature, finds a severable part, dismembers it, and returns the limb object.

---

## 13. Regeneration and Regrowth

### 13.1 CutAndQueueForRegeneration

After dismemberment, each severed part (and its children recursively) is stored for future regeneration:

```csharp
// Body.cs:2366-2402
public void CutAndQueueForRegeneration(BodyPart Part)
{
    BodyPart parentPart = Part.GetParentPart();
    // Extrinsic parts are NOT queued for regeneration
    DismemberedPart dismemberedPart = (Part.Extrinsic ? null : new DismemberedPart(Part, parentPart));

    // Recursively process children first
    if (Part.Parts != null && Part.Parts.Count > 0)
    {
        foreach (BodyPart item in new List<BodyPart>(Part.Parts))
            CutAndQueueForRegeneration(item);
        Part.Parts = null;
    }

    parentPart?.RemovePart(Part, DoUpdate: false);

    if (dismemberedPart != null)
    {
        DismemberedParts ??= new List<DismemberedPart>(1);
        DismemberedParts.Add(dismemberedPart);
    }

    Part.ParentBody = null;
    Part.Primary = false;
    Part.PreferredPrimary = false;
    Part.DefaultPrimary = false;
}
```

### 13.2 DismemberedPart Record

```csharp
public class DismemberedPart
{
    public BodyPart Part;      // The actual body part data
    public int ParentID;       // ID of parent part it was attached to

    public bool IsReattachable(Body ParentBody)
    {
        return ParentBody.GetPartByID(ParentID) != null;  // Parent still exists
    }

    public void Reattach(Body ParentBody)
    {
        BodyPart parent = ParentBody.GetPartByID(ParentID);
        ParentBody.DismemberedParts.Remove(this);
        parent.AddPart(Part, Part.Position, DoUpdate: false);
    }
}
```

**Critical constraint:** A dismembered part can only regenerate if its parent part still exists (`IsReattachable`).

### 13.3 Regenerability Check

```csharp
public bool IsRegenerable()
{
    if (Abstract) return false;      // Abstract parts can't regenerate
    if (IsDependent()) return false;  // Dependent parts recover automatically
    return true;
}

public bool IsRecoverable(Body UseParentBody = null)
{
    if (!IsDependent() || IsUnsupported(UseParentBody))
        return Abstract;
    return true;  // Supported dependents can be recovered
}
```

### 13.4 RegenerateLimb

```csharp
// Body.cs:2621-2683
public bool RegenerateLimb(bool WholeLimb = false, DismemberedPart Part = null, ...)
{
    if (Part == null)
        Part = FindRegenerablePart(ParentID, Category, ...);
    if (Part == null) return false;

    Part.Reattach(this);  // Re-add to body tree

    if (ParentObject.IsPlayer())
        AddPlayerMessage("You regenerate your " + Part.Part.GetOrdinalName() + "!", 'G');

    // If WholeLimb, recursively regenerate all children
    if (WholeLimb && Part.Part.HasID())
    {
        DismemberedPart dp;
        while ((dp = FindRegenerablePart(Part.Part.ID, ...)) != null)
            RegenerateLimb(WholeLimb, dp, Part.Part.ID, ...);
    }

    if (DoUpdate) UpdateBodyParts();
    return true;
}
```

### 13.5 ILimbRegenerationEvent System

```csharp
public abstract class ILimbRegenerationEvent : MinEvent
{
    public GameObject Object;      // Creature to regenerate
    public GameObject Actor;       // Who initiated
    public GameObject Source;       // Source item/effect
    public bool Whole;             // Regenerate entire limb subtree
    public bool All;               // Regenerate ALL missing limbs
    public bool IncludeMinor;      // Include minor regeneration (e.g., Glotrot tongue)
    public bool Voluntary;
    public int? ParentID;          // Limit to specific parent part
    public int? Category;          // Limit by category
}
```

`Body` handles `RegenerateLimbEvent`:

```csharp
public override bool HandleEvent(RegenerateLimbEvent E)
{
    if (E.All)
    {
        int max = Math.Max(100, GetDismemberedPartCount() * 2);
        int count = 0;
        while (RegenerateLimb(E) && ++count < max) { }
    }
    else
        RegenerateLimb(E);
}
```

### 13.6 Sources of Regeneration

| Source | File | Behavior |
|---|---|---|
| **Regeneration mutation** | `XRL.World.Parts.Mutation/Regeneration.cs` | Each turn: `Level * 10`% chance per turn. At level 10+: immune to decapitation |
| **Ubernostrum Tonic** | `XRL.World.Effects/Ubernostrum_Tonic.cs` | After 10 turns: regenerates one **whole** limb tree |
| **Regen Tank** | `XRL.World.Parts/RegenTank.cs` | Requires 2/3+ convalescence + cloning draught. Default: regenerates **all** limbs |
| **Fix-It Spray** | `XRL.World.Parts/RegenMedication.cs` | True Kin: all limbs individually. Mutants: one whole limb tree |
| **Glotrot** | `XRL.World.Effects/Glotrot.cs` | Special: regrows tongue when `IncludeMinor` is true |

### 13.7 Unsupported Part Recovery

Even without explicit regeneration events, `UpdateBodyParts()` runs `CheckPartRecovery()` to automatically reattach parts whose dependencies/support have returned.

---

## 14. Dependency System

Qud has two dependency channels that control automatic part loss and recovery:

### 14.1 Concrete Dependency (`DependsOn` / `SupportsDependent`)

- `SupportsDependent` on part A: "I support dependent parts tagged with this value"
- `DependsOn` on part B: "I need a part with `SupportsDependent == this` to exist"

```csharp
public bool IsConcretelyDependent() => DependsOn != null;

public bool IsConcretelyUnsupported(Body UseParentBody = null)
{
    if (DependsOn == null) return false;
    return ParentBody.GetPartBySupportsDependent(DependsOn) == null;
}
```

Example: A Face `DependsOn` the Head. If the Head is severed, the Face becomes unsupported.

### 14.2 Abstract Dependency (`RequiresType`)

- `RequiresType` on part B: "I need a part of this Type to exist"
- Combined with `RequiresLaterality` to match laterality

```csharp
public bool IsAbstractlyDependent() => RequiresType != null;

public bool IsAbstractlyUnsupported(Body UseParentBody = null)
{
    if (RequiresType == null) return false;
    return ParentBody.GetFirstPart(RequiresType, RequiresLaterality) == null;
}
```

Example: A "Hands" (two-handed weapon) slot has `RequiresType = "Hand"`. If both Hands are severed, the Hands slot becomes unsupported.

### 14.3 Combined Checks

```csharp
public bool IsDependent() => IsConcretelyDependent() || IsAbstractlyDependent();
public bool IsUnsupported() => IsConcretelyUnsupported() || IsAbstractlyUnsupported();
```

### 14.4 Impact on Severability and Regeneration

- Dependent parts **cannot be severed** directly (`IsSeverable()` returns false)
- Dependent parts **cannot be regenerated** directly (`IsRegenerable()` returns false)
- Dependent parts **are recovered automatically** when their support returns (`IsRecoverable()` returns true)

### 14.5 Body-Level Maintenance

`UpdateBodyParts()` runs:
- `CheckUnsupportedPartLoss()` — finds unsupported parts and cuts them
- `CheckPartRecovery()` — reattaches recoverable parts when support returns

This is how "losing use" of parts cascades automatically from support loss.

---

## 15. Implied Parts Engine

Beyond explicit dependencies, Qud has an inferred-part system driven by `BodyPartType.ImpliedBy` and `ImpliedPer`.

If `ImpliedBy = "Arm"` and `ImpliedPer = 2`, then for every 2 Arms present, one instance of this type should exist (e.g., "Hands" is implied by 2 Arms).

### CheckImpliedParts (`Body.cs:3700-3805`)

```csharp
public void CheckImpliedParts(int Depth = 0)
{
    // Count implying parts and implied parts
    // For each type with ImpliedBy:
    //   - Count how many of the implying type exist
    //   - Count how many of the implied type exist
    //   - If implied count < expected: add missing parts
    //   - If implied count > expected: remove excess parts
    // If any changes: UpdateBodyParts() + RecalculateArmor()
}
```

This runs as part of `UpdateBodyParts()`, so body structure auto-expands/shrinks from implication rules.

---

## 16. Mobility and Movement Penalties

### 16.1 Mobility Per Body Part

Each body part has a `Mobility` int value. Legs/feet typically have positive mobility. The body aggregates these via `GetTotalMobility()`.

### 16.2 Dismemberment Penalty Calculation

```csharp
// Body.cs:2406-2454
public int CalculateMobilitySpeedPenalty(out bool AnyDismembered)
{
    int totalMobility = GetTotalMobility();
    if (totalMobility == 0)
        return 60;  // Maximum penalty if NO mobility limbs remain

    int dismemberedMobility = 0;
    foreach (DismemberedPart dp in DismemberedParts)
        dismemberedMobility += dp.Part.GetTotalMobility();

    if (totalMobility >= 2 && dismemberedMobility > 0)
        return 60 * dismemberedMobility / (totalMobility + dismemberedMobility + 2);
    // ...
}
```

- Maximum penalty: **60 move speed**
- Losing all legs: `MobilityImpaired` effect applied
- Electrical conductivity of mobility parts matters for EMP/shock effects

---

## 17. Mutations That Modify Body Parts

Mutations interact with body parts through three main patterns:

### Pattern 1: Structural Body Part Additions

These mutations add/remove actual body part nodes to/from the tree.

#### MultipleArms (`XRL.World.Parts.Mutation/MultipleArms.cs`)

**Adds:** Arm + Hand + Hands structures per rank

**Laterality management:**
- First pair **reuses** existing arms by modifying their laterality: adds `| 4` (upper flag), marking them as "upper" while new arms are added below
- Uses `ChangeLaterality()` on existing parts and marks with `ChangesManagerID`

**Manager IDs:**
- `AdditionsManagerID` = `"{ObjectID}::MultipleArms::Add"` for new parts
- `ChangesManagerID` = `"{ObjectID}::MultipleArms::Change"` for modified parts

**Level effect:** `7 + Level * 3`% chance per extra arm for additional melee attack

**Unmutate:** `RemoveBodyPartsByManager(AdditionsManagerID, EvenIfDismembered: true)`, restore laterality on changed parts

#### MultipleLegs (`XRL.World.Parts.Mutation/MultipleLegs.cs`)

**Adds:** Feet parts per rank

**Laterality:** First pair: existing feet → `FORE` (16), new feet → `HIND` (64)

**Level effect:** Move speed bonus `-Level * 20`, carry capacity `Level + 5`%

**Manager IDs:** `"{ObjectID}::MultipleLegs::Add"` / `"::Change"`

#### TwoHeaded (`XRL.World.Parts.Mutation/TwoHeaded.cs`)

**Adds:** Head + Face pair

**Laterality:** Existing head → `RIGHT` (2), new head → `LEFT` (1)

**Special mechanics:**
- 50% chance to shake off negative mental effects
- Reduces mental action costs by `15 + 5 * Level`%
- `FindExtraHead()` requires `GetPartCount("Head") >= 2`

#### Stinger (`XRL.World.Parts.Mutation/Stinger.cs`)

**Adds:** Tail body part via static `AddTail()` method (reusable by SlogGlands)

**DefaultBehavior item:** Creates "Stinger" weapon with `MeleeWeapon` configured for damage/penetration

**Tail management:**
- First tries to claim an existing unmanaged "Tail" part
- If existing tail: lateralizes existing to RIGHT (2), adds new with LEFT (1)
- If no tail: adds after "Feet" or before "Roots"/"Thrown Weapon"/"Floating Nearby"

#### Wings (`XRL.World.Parts.Mutation/Wings.cs`)

**Adds:** Body part determined by variant (default "Back")

**DefaultBehavior:** Creates Wings armor item as `Part.DefaultBehavior`

**Level effects:** Charge distance `2 + Level/3`, jump distance `1 + Level/3`, travel speed `50 + 50*Level`%

#### MultiHorns (`XRL.World.Parts.Mutation/MultiHorns.cs`)

**Adds:** Multiple Head + Face pairs (default 2 extra heads)

**Equipment:** Creates "Horns Single" objects force-equipped on every Head

### Pattern 2: DefaultBehavior Equipment

These mutations create items that become the "natural" weapon/armor of existing body parts.

| Mutation | Target Part | Item Type | Notes |
|---|---|---|---|
| `BurrowingClaws` | Hand (up to 2) | Melee weapon | Wall penetration bonus, digging mode |
| `Beak` | Face | Melee weapon + Armor | +1 Ego via StatShifter |
| `Wings` | Back | Armor | Flight source, charge/jump bonuses |
| `FlamingRay` | Hands/Face/Feet (variant) | Armor | Adds `Flaming` part to all natural weapons |
| `FreezingRay` | Hands/Face/Feet (variant) | Armor | Adds `Icy` part to all natural weapons |
| `Crystallinity` | Quincunx parts | Melee weapon | Scales from 1d2 to 1d12 by level |

All mark with `TemporaryDefaultBehavior` string property for cleanup during `RegenerateDefaultEquipment()`.

### Pattern 3: Force-Equipped Items

These mutations create items and force-equip them onto existing parts.

| Mutation | Target Part | Item Type |
|---|---|---|
| `Horns` | Head | Melee weapon + Armor |
| `Stinger` | Tail | Melee weapon |
| `SlogGlands` | Tail | "Bilge Sphincter" |
| `Quills` | Body (root) | Armor (replaces body armor) |
| `Carapace` | Body (root) | Armor (AV `3 + Level/2`, DV -2) |
| `HooksForFeet` | Feet | Equipment |

### Shared Infrastructure

- **Manager ID strings** (`"{ObjectID}::MutationName"`) track which parts belong to which mutation
- `RemoveBodyPartsByManager()` for cleanup on unmutate
- `CleanUpMutationEquipment()` for equipment cleanup
- `WantToReequip()` signals the body to redistribute equipment after structural changes
- `BaseDefaultEquipmentMutation` provides registered slot system and hooks into `RegenerateDefaultEquipmentEvent` / `DecorateDefaultEquipmentEvent`
- Body part `Category` always inherited from `body.Category` (body root)

---

## 18. Equipment That Modifies Body Parts

Worn items can dynamically modify body topology:

| Part | File | Effect |
|---|---|---|
| `ArmsOnEquip` | `XRL.World.Parts/ArmsOnEquip.cs` | Adds configurable extra arm systems on equip, removes on unequip |
| `HelpingHands` | `XRL.World.Parts/HelpingHands.cs` | Adds robotic arms/hands |
| `Waldopack` | `XRL.World.Parts/Waldopack.cs` | Adds a servo arm when powered/equipped |

All follow the same Manager ID pattern for cleanup.

---

## 19. Cybernetics That Modify Anatomy

Several cybernetic implants dynamically add/transform body parts:

### CyberneticsEquipmentRack (`CyberneticsEquipmentRack.cs`)

Adds an "Equipment Rack" body part after "Back":

```csharp
public override bool HandleEvent(ImplantedEvent E)
{
    BodyPart body = E.Part.ParentBody.GetBody();
    body.AddPartAt("Equipment Rack", 0, ..., Manager: ManagerID, ...,
        InsertAfter: "Back",
        OrInsertBefore: ["Missile Weapon", "Hands", "Feet", "Roots", "Thrown Weapon"]);
    E.Implantee.WantToReequip();
}
```

### CyberneticsGunRack (`CyberneticsGunRack.cs`)

Adds two "Hardpoint" body parts (Category 6 = CYBERNETIC, Integral = true) for mounting missile weapons:

```csharp
// Two hardpoints with different laterality (left=1, right=2)
body.AddPartAt("Hardpoint", 2, ..., Category: 6, Integral: true, ...);
body.AddPartAt(insertAfter, "Hardpoint", 1, ..., Category: 6, Integral: true, ...);
```

Also blocks dismemberment of its own body part via `BeforeDismemberEvent`.

### CyberneticsMotorizedTreads (`CyberneticsMotorizedTreads.cs`)

The most complex — both adds new parts AND transforms an existing body part:

```csharp
// Add two Tread body parts (Category 6, Integral)
part.AddPartAt("Tread", 2, ...);
part.AddPartAt(insertAfter, "Tread", 1, ...);

// TRANSFORM the implanted part:
// Save all original properties for undo
PartName = E.Part.Name;  // e.g., "feet"
PartDescription = E.Part.Description;
// ...
// Replace with new identity:
E.Part.Name = "lower body";
E.Part.Category = 6;  // CYBERNETIC
E.Part.Mobility = 0;
E.Part.Integral = true;
```

On unimplant, all saved properties are restored.

### CyberneticsGraftedMirrorArm (`CyberneticsGraftedMirrorArm.cs`)

Adds an extra "Thrown Weapon" body part slot.

### CyberneticsMagneticCore

Adds a "Floating Nearby" body part slot.

### CyberneticsFistReplacement (`CyberneticsFistReplacement.cs`)

Replaces `DefaultBehavior` (natural fists) on Hand parts with custom weapons (e.g., cybernetic carbide hand bones). Preserves original blueprint for restoration on unimplant.

---

## 20. Special Slot Types

### 20.1 "Missile Weapon" (Abstract)

- Abstract body part type — not physical, cannot be severed
- Auto-added by all anatomies
- `MissileWeapon.SlotType` defaults to `"Missile Weapon"`
- Missile weapons can fire from both Equipped AND Cybernetics independently
- Hand and Missile Weapon types are exempt from gigantic size restrictions

### 20.2 "Thrown Weapon" (Abstract)

- Auto-added by all anatomies via `Anatomy.ThrownWeapon`
- Items with `ThrownWeapon` part equip here
- Has its own simplified equip/size-check branch

### 20.3 "Floating Nearby" (Abstract)

Items in this slot are considered orbiting/hovering near the creature. Key special behaviors:

1. **Bypasses gigantic size restrictions** — both gigantic and non-gigantic creatures can equip any Floating Nearby item
2. **Missile weapons auto-equippable** — `MissileWeapon` treats Floating Nearby as always equippable
3. **`ModMagnetized`** converts items to use Floating Nearby instead of their normal slot
4. **Armor with `WornOn = "Floating Nearby"`** is restricted to only that slot

---

## 21. Shield System

Shields are **not** a special body part type. They are regular equipment items with the `Shield` part:

```csharp
public class Shield : IPart
{
    public string WornOn = "Arm";  // Default: equipped on Arm slots
    public int AV;                  // Shield AV (applied on block)
    public int DV;                  // DV modifier (applied when equipped)
    public int SpeedPenalty;        // Speed penalty
}
```

Key behaviors:
- Shields require `Shield_Block` skill to be considered "desirable" for AI auto-equip
- Shield detection scans ALL body parts recursively
- Shield block preference is based on AV: `Preference += AV * 100`
- `WornOn = "*"` makes a shield equippable on any slot type

---

## 22. Body Rebuild

`Body.Rebuild(asAnatomy)` (`Body.cs:2817-3073`) is the comprehensive body migration routine used when anatomy changes fundamentally:

1. **Snapshot** top-level dynamic parts using `BodyPartPositionHint`
2. **Unimplant** and temporarily stash cybernetics
3. **Unequip** and stash equipped items
4. **Unmutate** body/equipment-generating mutations
5. **Apply** new anatomy template
6. **Reinsert** dynamic parts using ranked position hints
7. **Remutate** saved mutations
8. **Reimplant** cybernetics
9. **Auto-equip** stashed gear
10. **Cleanup** orphan references and update body

`BodyPartPositionHint` scoring finds best parent/position matches across topology changes to preserve part placement.

---

## 23. Chimera System

The Chimera mutation enables random body part growth when gaining new mutations.

### AddChimericBodyPart (`Mutations.cs`)

```csharp
public BodyPart AddChimericBodyPart(bool Silent = false, string Manager = "Chimera",
    BodyPart AttachAt = null)
{
    // 1. Get random body part type (weighted by ChimeraWeight)
    BodyPartType type = Anatomies.GetRandomBodyPartType(IncludeVariants: true,
        UseChimeraWeight: true);

    // 2. Determine attachment point
    BodyPart attachPoint = GetChimericBodyPartAttachmentPoint();

    // 3. Create new BodyPart with Dynamic = true, Manager = "Chimera"
    // 4. Find usual child parts via Anatomies.FindUsualChildBodyPartTypes()
    //    and add them as children
    // 5. Adjust laterality to match parent
    // 6. If placement doesn't match UsuallyOn, modify name/description
    // 7. Attach to body tree
}
```

**Attachment point selection:**
- Standard: tries to place on `UsuallyOn` body part type (e.g., arms go on body)
- Random: `ChimericBodyPartRandomFromBodyChance` to attach to body root; otherwise picks from non-abstract, contact, non-extrinsic, live-category parts

**Trigger:** When a Chimera gains a mutation, ~33% chance to also call `AddChimericBodyPart()`

---

## 24. Source File Index

### Core Anatomy System
| File | Purpose |
|---|---|
| `XRL.World.Anatomy/Anatomies.cs` | Global registry, XML loading, body part type lookup |
| `XRL.World.Anatomy/Anatomy.cs` | Anatomy template definition and application |
| `XRL.World.Anatomy/AnatomyPart.cs` | Part node in anatomy template |
| `XRL.World.Anatomy/BodyPartType.cs` | Metadata contract for part types |
| `XRL.World.Anatomy/BodyPart.cs` | Runtime body part node (7,108 lines) |
| `XRL.World.Anatomy/BodyPartCategory.cs` | Category constants, conductivity, live checks |
| `XRL.World.Anatomy/BodyPartPositionHint.cs` | Position matching for body rebuild |
| `XRL.World.Anatomy/BodyPartPositionSpec.cs` | Position specification |
| `XRL.World.Parts/Body.cs` | Body component, tree management, dismember/regen (3,858 lines) |
| `XRL.World.Capabilities/Laterality.cs` | Laterality bitmask system |

### Equipment System
| File | Purpose |
|---|---|
| `XRL.World.Parts/Inventory.cs` | Equip/unequip command pipeline |
| `Qud.API/EquipmentAPI.cs` | Equipment API |
| `XRL.World.Parts/NaturalEquipment.cs` | Blocks unequip of natural weapons |
| `XRL.World.Parts/IntegralAnatomy.cs` | Marks parts as un-severable |
| `XRL.World/QuerySlotListEvent.cs` | Slot query event |
| `XRL.World/QueryEquippableListEvent.cs` | Item-side slot admissibility |
| `XRL.World/GetSlotsRequiredEvent.cs` | Dynamic slot count |

### Dismemberment & Regeneration Events
| File | Purpose |
|---|---|
| `XRL.World/BeforeDismemberEvent.cs` | Gate check before dismemberment |
| `XRL.World/AfterDismemberEvent.cs` | Post-dismemberment notification |
| `XRL.World/CanBeDismemberedEvent.cs` | Query: can this creature be dismembered? |
| `XRL.World/ILimbRegenerationEvent.cs` | Base event for limb regeneration |
| `XRL.World/RegenerateLimbEvent.cs` | Trigger actual regeneration |
| `XRL.World/AnyRegenerableLimbsEvent.cs` | Query: any limbs to regenerate? |

### Dismemberment Sources
| File | Purpose |
|---|---|
| `XRL.World.Parts.Skill/Axe_Dismember.cs` | Axe dismember skill (3%/6% passive + active) |
| `XRL.World.Parts.Skill/Axe_Decapitate.cs` | Decapitation (mortal parts) |
| `XRL.World.Parts.Skill/Physic_AmputateLimb.cs` | Self/ally amputation |
| `XRL.World.Parts/ModSerrated.cs` | 3% dismember on long blades/axes |
| `XRL.World.Parts/ModGlazed.cs` | `10 + Tier*2`% dismember |
| `XRL.World.Parts/DismemberAdjacentHostiles.cs` | Passive adjacent dismember |

### Regeneration Sources
| File | Purpose |
|---|---|
| `XRL.World.Parts.Mutation/Regeneration.cs` | `Level * 10`% per turn; decap immunity at 10+ |
| `XRL.World.Effects/Ubernostrum_Tonic.cs` | One whole limb tree |
| `XRL.World.Parts/RegenTank.cs` | All limbs (requires convalescence + cloning draught) |
| `XRL.World.Parts/RegenMedication.cs` | True Kin: all individually; Mutants: one whole |
| `XRL.World.Effects/Glotrot.cs` | Tongue regeneration (minor) |

### Mutations That Modify Body Parts
| File | Adds |
|---|---|
| `XRL.World.Parts.Mutation/MultipleArms.cs` | Arm + Hand + Hands pairs |
| `XRL.World.Parts.Mutation/MultipleLegs.cs` | Feet pairs |
| `XRL.World.Parts.Mutation/TwoHeaded.cs` | Head + Face |
| `XRL.World.Parts.Mutation/MultiHorns.cs` | Multiple Head + Face pairs |
| `XRL.World.Parts.Mutation/Stinger.cs` | Tail + stinger weapon |
| `XRL.World.Parts.Mutation/SlogGlands.cs` | Tail (reuses Stinger.AddTail) |
| `XRL.World.Parts.Mutation/Wings.cs` | Back part + wings armor |
| `XRL.World.Parts.Mutation/Horns.cs` | Force-equips on Head |
| `XRL.World.Parts.Mutation/BurrowingClaws.cs` | DefaultBehavior on Hands |
| `XRL.World.Parts.Mutation/Beak.cs` | DefaultBehavior on Face |
| `XRL.World.Parts.Mutation/Quills.cs` | Force-equips on Body root |
| `XRL.World.Parts.Mutation/Carapace.cs` | Force-equips on Body root |
| `XRL.World.Parts.Mutation/HooksForFeet.cs` | Force-equips on Feet |
| `XRL.World.Parts.Mutation/FlamingRay.cs` | DefaultBehavior + Flaming decoration |
| `XRL.World.Parts.Mutation/FreezingRay.cs` | DefaultBehavior + Icy decoration |
| `XRL.World.Parts.Mutation/Crystallinity.cs` | DefaultBehavior on Quincunx parts |
| `XRL.World.Parts.Mutation/Chimera.cs` | Random body parts via `AddChimericBodyPart()` |

### Cybernetics That Modify Anatomy
| File | Adds |
|---|---|
| `XRL.World.Parts/CyberneticsEquipmentRack.cs` | "Equipment Rack" part |
| `XRL.World.Parts/CyberneticsGunRack.cs` | Two "Hardpoint" parts (Category 6, Integral) |
| `XRL.World.Parts/CyberneticsMotorizedTreads.cs` | Two "Tread" parts + transforms implanted part |
| `XRL.World.Parts/CyberneticsGraftedMirrorArm.cs` | Extra "Thrown Weapon" slot |
| `XRL.World.Parts/CyberneticsMagneticCore.cs` | Extra "Floating Nearby" slot |
| `XRL.World.Parts/CyberneticsFistReplacement.cs` | Replaces DefaultBehavior fists |
| `XRL.World.Parts/CyberneticsBaseItem.cs` | Base implant infrastructure |

### Equipment That Modifies Body Parts
| File | Adds |
|---|---|
| `XRL.World.Parts/ArmsOnEquip.cs` | Extra arm systems on equip |
| `XRL.World.Parts/HelpingHands.cs` | Robotic arms/hands |
| `XRL.World.Parts/Waldopack.cs` | Servo arm when powered |

### Other Related
| File | Purpose |
|---|---|
| `XRL.World.Parts/DismemberedProperties.cs` | Metadata on severed limb objects |
| `XRL.World.Parts/BodyPartInventory.cs` | Pre-populates severed limbs for merchants |
| `XRL.World.Parts/Brain.cs` | NPC reequip automation |
| `XRL.World/AfterPlayerBodyChangeEvent.cs` | Player body change notification |
| `XRL.World/BodyPositionChangedEvent.cs` | Body position change notification |

---

## Key Design Principles

1. **Topology is authoritative** — if a part node exists, it is a potential slot candidate
2. **Equipment occupancy is per-body-part reference**, not global slot count
3. **First-slot flags are required** for correctness under multi-slot occupancy
4. **Detached parts remain structurally tracked** via `DismemberedParts` with parent IDs and positions
5. **Body updates are maintenance passes** that regenerate default equipment, recalculate firsts, enforce support loss/recovery, check implied parts, and apply mobility penalties
6. **"Losing use" and "physically severed" are distinct states** — dependency loss vs actual dismemberment
7. **Everything is event-driven and data-driven** — mods/parts can alter slot eligibility, slot counts, dismemberability, and equip-over-default behavior without changing core classes
8. **Manager IDs** provide ownership tracking so mutations, cybernetics, and equipment can cleanly add and remove body parts
