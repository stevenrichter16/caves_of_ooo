# Caves of Qud Tinkering System Analysis

## Architecture Overview

Qud's tinkering has 6 interconnected subsystems:
1. **Bit System** — crafting currency (colored data fragments)
2. **Recipes (TinkerData)** — what can be built/modded
3. **Modification System** — mods applied to items as Parts
4. **Skill Tree** — gates access to tinkering tiers
5. **Disassembly** — breaks items into bits, reverse-engineers recipes
6. **Data Disks** — physical items that teach recipes

---

## 1. Bit System (Crafting Currency)

Bits are colored data fragments organized into tiers 0–8. Each is a single `char`:

| Char | Tier | Name |
|------|------|------|
| `R` | 0 | scrap power systems |
| `G` | 0 | scrap crystal |
| `B` | 0 | scrap metal |
| `C` | 0 | scrap electronics |
| `r` | 1 | phasic power systems |
| `g` | 2 | flawless crystal |
| `b` | 3 | pure alloy |
| `c` | 4 | pristine electronics |
| `K` | 5 | nanomaterials |
| `W` | 6 | photonics |
| `Y` | 7 | AI microcontrollers |
| `M` | 8 | metacrystal |

**BitCost** — extends `Dictionary<char, int>`, constructed from a string like `"BBCr"` (2 scrap metal + 1 scrap electronics + 1 phasic power).

**BitLocker** — Part on the player entity storing accumulated bits as `Dictionary<char, int>`. Key methods:
- `AddBits(string)` — from disassembly
- `UseBits(string)` — spend for crafting
- `HasBits(string)` — affordability check

**Bit cost templates** — Items define costs as template strings (e.g., `"1BC"`). Digits are tier levels resolved to a random bit of that tier via `BitType.ToRealBits()` using a seed based on blueprint name. So the same item type always costs the same bits.

---

## 2. Recipes (TinkerData)

Core record:

```
TinkerData:
  DisplayName   — human-readable name
  Blueprint     — object blueprint ID, or "[mod]PartName" for mods
  Category      — "utility", "weapon", etc.
  Type          — "Build" or "Mod"
  Tier          — 0–8, determines skill requirement
  Cost          — resolved bit cost string (e.g., "BBCr")
  Ingredient    — optional required blueprint (comma-separated)
```

**Two recipe types:**
- **"Build"** — creates a new item. Sourced from blueprints with `TinkerItem.CanBuild=true`.
- **"Mod"** — applies a modification. Blueprint format `"[mod]ModPartName"`. Sourced from `ModificationFactory.ModList` where `TinkerAllowed=true`.

**Static collections:**
- `TinkerData.TinkerRecipes` — all possible recipes (lazy-loaded)
- `TinkerData.KnownRecipes` — recipes the player has learned (persisted in save)

**Learning recipes via:**
- Data Disks (physical items)
- Reverse Engineering during disassembly
- Skill progression (Tinker I/II/III each grant a recipe choice)

---

## 3. TinkerItem Part (on craftable items)

```
TinkerItem:
  CanDisassemble = true     — can be taken apart
  CanBuild = false          — can be crafted from bits
  BuildTier = 1             — tier required to build
  NumberMade = 1            — count produced per craft
  Ingredient = ""           — required ingredient blueprint(s)
  SubstituteBlueprint       — alt blueprint for bit cost lookup
  RepairCost                — bit cost to repair
```

**Skill tier gating:**
- Tier 0–3 → requires Tinker I
- Tier 4–6 → requires Tinker II
- Tier 7–8 → requires Tinker III

---

## 4. Modification System

### ModEntry (data record, from Mods.xml)

```
ModEntry:
  Part              — C# class name (e.g., "ModSharp")
  Tables            — comma-separated table names (e.g., "MeleeWeapons,Armor")
  Rarity            — 0=Common, 1=Uncommon, 2=Rare, 3=R2, 4=R3
  MinTier / MaxTier — item tier range
  NativeTier        — tier for weighting
  TinkerTier        — skill tier needed to apply
  Value             — commerce value multiplier
  TinkerDisplayName — UI name
  Description       — description text
  TinkerIngredient  — required ingredient
  TinkerCategory    — UI category
  TinkerAllowed     — can be applied via tinkering
  BonusAllowed      — can randomly appear on generated items
```

### IModification Base Class (extends Part)

```
IModification : IActivePart : IPart
  int Tier

  Configure()                 — set WorksOnSelf, ChargeUse, etc.
  TierConfigure()             — tier-dependent config
  GetModificationSlotUsage()  — default 1
  ModificationApplicable(obj) — can apply to this item?
  BeingAppliedBy(obj, who)    — pre-apply check
  ApplyModification(obj)      — do the modification
  GetModificationDisplayName()
```

### Mod Compatibility

Items declare a `Mods` tag (e.g., `Mods="MeleeWeapons"`). Each ModEntry declares `Tables="MeleeWeapons"`. The intersection determines which mods can apply. Max mod slots controlled by `MAXIMUM_ITEM_MODS`, each mod uses `GetModificationSlotUsage()` slots.

### Example Mods

| Mod | Type | Effect |
|-----|------|--------|
| **ModSharp** | Passive | +1 penetration on melee weapons with blade/axe skills |
| **ModFlaming** | Active (powered) | Fire damage on hit, requires EnergyCellSocket, scales with tier |
| **ModSturdy** | Passive | +25% HP, prevents breakage, blocks Broken/Shattered effects |
| **ModReinforced** | Passive | +1 AV, body/back armor only |

---

## 5. Disassembly

Multi-turn action. Algorithm per item:

1. Require `Tinkering_Disassemble` skill
2. Calculate `BitChance` (base 50% + bonuses)
3. For each bit in the item's cost string:
   - Last bit always yielded
   - Other bits yield with `BitChance` probability
   - If `NumberMade > 1`, each bit has additional `1/(NumberMade+1)` chance
4. If player has `Tinkering_ReverseEngineer`:
   - 15% base chance to learn the item's build recipe
   - Also checks for learnable mod recipes from mods on the item
5. Item destroyed, bits added to `BitLocker`

---

## 6. Data Disks

Physical items with a `DataDisk` Part containing a `TinkerData`. Two actions:
- **"Learn"** — adds recipe to `KnownRecipes`, destroys disk
- **"Build"** — directly builds the item (costs bits, doesn't require knowing recipe)

On creation, picks a random recipe weighted by tier. Commerce value scales with tier (50–450).

---

## 7. Skill Tree

| Skill | Effect |
|-------|--------|
| **Tinkering** (root) | Base skill, `LearnNewRecipe()` helper |
| **Disassemble** | Break items into bits |
| **Tinker I** | Build tier 0–3 items, Recharge ability, grants 1 recipe |
| **Tinker II** | Build tier 4–6 items, grants 1 recipe |
| **Tinker III** | Build tier 7–8 items, grants 1 recipe |
| **Reverse Engineer** | Learn recipes by disassembling items |
| **Gadget Inspector** | +5 identification bonus |
| **Scavenger** | Find items in trash/rubble |
| **Deploy Turret** | Place missile weapons as turrets |
| **Repair** | Fix broken/rusted items using bits |
| **Lay Mine** | Deploy grenades as mines |

---

## 8. Random Mod Generation (on item creation)

```
ModificationFactory.ApplyModifications(item, blueprint, bonusChance):
  Base 3% chance per slot (up to 3)
  Read item's Mods tag → match to ModTable entries
  Filter by tier range, applicability
  Weighted random by rarity:
    Common=100000, Uncommon=40000, Rare=10500, R2=1500, R3=150
  Multiply by tier proximity weight
  Instantiate mod Part → ApplyModification
```

---

## 9. Data Flow

### Building an Item
```
Select recipe from KnownRecipes
→ Check skill tier (Tinker I/II/III)
→ Check BitLocker.HasBits(cost)
→ Check ingredient in inventory (optional)
→ BitLocker.UseBits(cost)
→ Consume ingredient
→ Create entity from blueprint × NumberMade
→ Add to inventory
```

### Disassembling an Item
```
Select item with TinkerItem part
→ Require Disassemble skill
→ Roll bits from cost string (last guaranteed, others at ~50%)
→ If ReverseEngineer: 15% to learn build recipe + mod recipes
→ Destroy item
→ BitLocker.AddBits(yielded bits)
```

### Applying a Mod
```
Select mod recipe + target item
→ Check item Mods tag matches mod's Tables
→ Check mod slots < max
→ Check ModificationApplicable()
→ Calculate cost (mod tier bit + item tier bit)
→ BitLocker.UseBits(cost)
→ Instantiate mod Part, add to item
→ Adjust commerce value × mod's Value multiplier
→ Call ApplyModification()
```

---

## 10. Key Design Patterns

1. **Bits as strings** — costs are string-encoded (each char = one bit). Compact, serializable.
2. **Two recipe types, one data structure** — `TinkerData.Type` = "Build" or "Mod".
3. **Table-based mod compatibility** — items declare tables they accept; mods declare tables they belong to. Clean many-to-many.
4. **Tier gating via integer comparison** — simple, no complex prerequisite trees.
5. **Mods as Parts** — each mod is a full Part participating in the event system. Mods can respond to combat events, naming events, etc.
6. **Seeded randomness** — bit cost resolution seeded by blueprint name, so same item type always has same cost.
7. **Static recipe caches** — `TinkerRecipes` (all) and `KnownRecipes` (learned) are game-scoped.
