# Inkbound Lore Integration: Repair & Restoration Systems

## Overview

This document proposes integrating Inkbound lore themes into Caves of Ooo through **repair and restoration mechanics** -- specifically repairing infrastructure like wells, columns, and magical systems. The goal is to make repair feel like a lore-rich activity (preserving memory, maintaining civilization) rather than a generic crafting action.

The three source pillars that converge on this design:
1. **Inkbound lore**: memory preservation, post-Siege infrastructure, knowledge as infrastructure
2. **Frieren everyday magic**: infrastructure weaving, spell archaeology, enchantment entropy
3. **Existing tinkering system**: bit-based repair costs, skill gating, success/failure outcomes

---

## Core Design Thesis

> Repairing a well isn't plumbing. It's archaeology. Every broken thing in the caves was built by someone for a reason, and fixing it means understanding that reason.

Repair is framed as an act of **reading the world** -- understanding what something was, why it broke, and what restoring it means socially and magically. This directly echoes the Inkbound ethos where knowledge preservation is civilizational survival.

---

## Proposed Repairable Infrastructure Objects

### 1. Wells (Priority: High)

**Current state**: The `Well` blueprint exists as a solid, non-interactive object (`Objects.json:1980`).

**Proposed states**:

| State | Display | Render | Behavior |
|-------|---------|--------|----------|
| `Well` | "well" | `O` cyan | Provides fresh water when interacted with |
| `DriedWell` | "dried well" | `O` brown | Non-functional; repairable |
| `PoisonedWell` | "fouled well" | `O` dark red | Produces tainted water; purifiable |
| `RestoredWell` | "restored well" | `O` bright cyan | Enhanced water output; faction reputation reward |

**Lore hook**: Wells in the caves predate the current inhabitants. Dried wells have residual water-seeking enchantments (Frieren's "dead spells") that can be reactivated. The Palimpsest faction views well restoration as recovering lost infrastructure. The Rot Choir views dried wells as nature reclaiming its own.

**Repair flow**:
1. Player examines a `DriedWell` -> message: "This well has been dry for ages. Faint runes circle the rim -- a water-drawing enchantment, long faded."
2. Tinkering check (Repair skill + bit cost, e.g. `BCC`) to restore the enchantment
3. Success -> well becomes functional, nearby NPCs react, faction reputation changes
4. Exceptional success -> well becomes `RestoredWell` with bonus output

**Faction implications**:
- Restoring a well near Palimpsest territory: +reputation with Palimpsest ("You fold time back upon itself. The Palimpsest approves.")
- Restoring a well near Rot Choir territory: -reputation with Rot Choir ("The breaking was the best thing that ever happened to the world.")
- Restoring a well near Villagers: +reputation with Villagers (practical gratitude)

### 2. Broken Columns (Priority: Medium)

**Current state**: `BrokenColumn` exists as non-interactive terrain (`Objects.json:1958`).

**Proposed states**:

| State | Display | Render | Behavior |
|-------|---------|--------|----------|
| `BrokenColumn` | "broken column" | `,` white | Rubble; no function |
| `RestoredColumn` | "restored column" | `|` white | Load-bearing; may reveal hidden areas or support unstable ceilings |

**Lore hook**: Some columns are load-bearing for entire cave sections. Others were part of ancient magical relay networks (like the Inkbound "stacks" -- archive infrastructure). Restoring a column might stabilize a dangerous area, or reactivate a forgotten information relay that reveals lore.

**Repair flow**:
1. Examine -> "This column once bore weight -- or meaning. Its base still shows tool marks and traces of binding glyphs."
2. Repair cost: `BBCC` (heavier than wells -- structural work)
3. Success -> column restored, area effect (ceiling stabilized, or nearby hidden passage revealed)

### 3. Archive Fragments (Priority: Medium -- New Object)

**Proposed new object** directly inspired by Inkbound's archival identity.

| State | Display | Render | Behavior |
|-------|---------|--------|----------|
| `DamagedArchiveStone` | "damaged archive stone" | `=` dark cyan | Faded inscriptions; repairable |
| `RestoredArchiveStone` | "archive stone" | `=` bright cyan | Readable; grants lore/recipes/faction intel |

**Lore hook**: The Inkbound once maintained a network of archive stones throughout the caves -- carved slabs that recorded local conditions, faction movements, and spell specifications. Most are damaged or erased. Restoring one is literally an act of memory preservation.

**Repair flow**:
1. Examine -> "A flat stone inscribed with layered text. Most is illegible -- water damage, or deliberate erasure."
2. Repair cost: `RBC` (the R bit represents the scholarly/magical component)
3. Success -> stone becomes readable, revealing one of: a tinkering recipe, a faction lore entry, a map annotation, or a historical note about the Siege
4. Exceptional success -> stone reveals a rare recipe or hidden faction relationship

**Faction implications**:
- Inkbound/Palimpsest factions strongly approve of archive restoration
- Some stones contain information that specific factions want suppressed (e.g., Rot Choir doesn't want pre-breaking history preserved)

### 4. Enchanted Aqueducts (Priority: Low -- Future Content)

Inspired directly by Frieren's "Infrastructure Weaving" concept. Broken magical water/flow systems connecting wells and inhabited areas. Repairing a full aqueduct chain would be a multi-step quest linking several well restorations together.

---

## Implementation Approach

### Step 1: Repairable Object Part

Create a new `RepairablePart` component that can be attached to any blueprint:

```
RepairablePart
  |- RepairedBlueprint: string     // what this becomes when repaired (e.g. "Well")
  |- RepairCost: string            // bit cost (e.g. "BCC")
  |- RepairSkillTier: int          // minimum Tinker skill required
  |- ExamineText: string           // flavor text when examined
  |- RepairText: string            // message on successful repair
  |- FactionRepChanges: dict       // faction -> reputation delta on repair
```

This builds directly on the existing `TinkerItemPart` (which already has `RepairCost`) and `TinkeringService` (which already handles `TryCraft`/`TryDisassemble`).

### Step 2: Blueprint Additions

Add new blueprints to `Objects.json`:

```json
{
  "Name": "DriedWell",
  "Inherits": "PhysicalObject",
  "Parts": [
    { "Name": "Render", "Params": [
      { "Key": "DisplayName", "Value": "dried well" },
      { "Key": "RenderString", "Value": "O" },
      { "Key": "ColorString", "Value": "&W" },
      { "Key": "RenderLayer", "Value": "1" }
    ]},
    { "Name": "Physics", "Params": [{ "Key": "Solid", "Value": "true" }] },
    { "Name": "Repairable", "Params": [
      { "Key": "RepairedBlueprint", "Value": "Well" },
      { "Key": "RepairCost", "Value": "BCC" },
      { "Key": "ExamineText", "Value": "This well has been dry for ages. Faint runes circle the rim -- a water-drawing enchantment, long faded." }
    ]}
  ],
  "Tags": [
    { "Key": "Solid", "Value": "" },
    { "Key": "Repairable", "Value": "" }
  ]
}
```

### Step 3: Repair Command

Add a `TryRepair` method to `TinkeringService` alongside the existing `TryCraft` and `TryDisassemble`:

```
TryRepair(Entity repairer, Entity target):
  1. Check target has RepairablePart
  2. Check repairer has sufficient bits (via BitLockerPart)
  3. Check repairer meets skill tier requirement
  4. Deduct bits
  5. Roll repair outcome (Success / Exceptional / Partial / Failure / Critical)
  6. On success: replace target entity with RepairedBlueprint
  7. Apply faction reputation changes
  8. Log contextual message
```

### Step 4: Well Functionality

Enhance the `Well` blueprint so restored wells are actually useful:

```
LiquidSourcePart
  |- LiquidType: string           // "FreshWater"
  |- MaxUses: int                 // -1 for infinite, or limited
  |- RefreshRate: int             // turns between refills (0 = never depletes)
```

Interaction: player walks up to a well and uses it to fill a container or drink directly.

### Step 5: World Generation Hooks

Place `DriedWell` and `DamagedArchiveStone` objects in appropriate zones during generation:
- Shallow underground (Z=11-15): occasional dried wells near settlement ruins
- Mid underground (Z=16-25): archive stones near Palimpsest/Inkbound territory
- Deep underground (Z=26+): rare, high-tier repairable infrastructure

### Step 6: Faction Dialogue Integration

Add repair-related conversation branches to existing faction NPCs:

**Palimpsest Echo** (existing NPC):
- New branch: "I found a dried well nearby." -> "Then you have found a memory of water. The enchantment remembers what the stone forgets. Restore it, and the Palimpsest will note your contribution."

**Rot Choir Tendril** (existing NPC -- already has `RepairSubstrate` dialogue):
- Extend existing repair dialogue: "You restored a well in our territory?" -> "You push the clock backward. We remember who does this."

**Villager Tinker** (existing NPC -- already mentions repair):
- Add: "I found something broken in the caves." -> "Bring me the details. If it's within my skill, I can tell you what bits you'll need."

---

## Lore Resonance Map

How each repair action connects to Inkbound's setting pillars:

| Inkbound Pillar | Repair Action | Expression |
|-----------------|---------------|------------|
| Memory vs. erasure | Restoring archive stones | Literally recovering lost records |
| Memory vs. erasure | Repairing wells | Reactivating "dead spells" -- enchantments that remember their purpose |
| Order vs. breach | Restoring columns | Stabilizing infrastructure damaged in the Siege |
| Order vs. breach | Completing aqueduct chains | Reconnecting systems that breach severed |
| Faction identity | Repair reputation effects | Different factions view restoration differently |
| Economy reflects worldview | Repair bit costs | The materials needed reflect the object's cultural origin |

---

## Faction Attitudes Toward Repair

| Faction | Attitude | Reasoning |
|---------|----------|-----------|
| Palimpsest | Strongly approves | Their identity is folding time back, restoring what was lost |
| Villagers | Approves | Practical benefit; infrastructure means safety |
| Pale Curation | Approves selectively | Interested in preserving specific artifacts, not everything |
| Saccharine Concord | Neutral/transactional | Repaired infrastructure has trade value |
| Glassblown Remnant | Ambivalent | Some things are beautiful broken |
| Rot Choir | Disapproves | Breaking was liberation; restoration is regression |
| Snapjaws | Indifferent | They scavenge, not preserve |

---

## Frieren "Everyday Magic" Tie-Ins

These repair mechanics directly implement several Frieren concepts from the brainstorm doc:

1. **Infrastructure Weaving** (Set 1, #4): "The bakery district's ovens all went cold -- trace the heat-distribution spell back to its anchor stone." Wells and aqueducts are exactly this.

2. **Spell Archaeology** (Set 2, #1): "A dried-up well still has a water-seeking spell pointing toward a shifted aquifer." This is literally the DriedWell repair description.

3. **Enchantment Entropy** (Set 1, #9): "Every spell in the world is slowly decaying." Dried wells, broken columns, and faded archive stones are entropy made tangible.

4. **The Demonstration** (Set 1, #10): "A town's well has gone bitter? Three mages each propose a solution." The PoisonedWell variant directly implements this scenario.

5. **Mage Infrastructure Decay** (Set 3, #2): "This infrastructure is aging. The original mages who built it are dead." The archive stones embody this -- systems built by a faction that may no longer exist in the same form.

---

## Spell Economics: Materials to Learn, Mana to Cast, Materials to Amplify

### Design Principle

Spells (including repair spells) follow a three-phase resource model:

1. **Learning/First Cast**: Requires full material (bit) cost. You are constructing the spell from raw components -- understanding its structure through physical engagement with its ingredients.
2. **Subsequent Casts**: Requires only mana. You've internalized the spell; your understanding replaces the scaffolding.
3. **Amplified Casts**: If you voluntarily spend materials alongside mana, the spell is more powerful. The materials aren't required, but they resonate with the spell's original composition.

This creates a natural progression: early game is resource-constrained and every spell feels expensive. Late game, you're casting freely but choosing when to invest materials for critical moments.

### Lore Justification

This maps directly onto the Inkbound/Frieren themes:

- **Inkbound**: The first cast is like copying a manuscript by hand -- you must work through every word to understand it. After that, you carry the knowledge. But having the original manuscript open while you work (materials) produces a more faithful result.
- **Frieren**: "No spell is useless, only unstudied." Studying a spell means working through its material composition once. After that, the spell is *yours*. The 1000-year spellbook problem is real -- you have hundreds of spells you learned centuries ago, and they cost nothing to cast, but you've forgotten how good they can be when you feed them.

### Mechanical Design

#### Mana Pool

A new resource on the player entity, alongside HP and MP:

```
Mana (stat)
  |- Current: int          // current mana available
  |- Max: int              // maximum mana pool
  |- RegenRate: int        // mana restored per turn (or per rest)
```

Mana is a **general-purpose casting resource** -- not per-spell, not per-school. Simple and legible.

#### Spell Cost Structure

Each spell/mutation/repair ability has:

```
SpellCost
  |- LearnCost: string          // bit cost for first cast (e.g. "BCC")
  |- ManaCost: int              // mana cost for subsequent casts
  |- Learned: bool              // has the player paid the LearnCost?
  |- AmplificationMaterials: string  // which bits can amplify (e.g. "BC")
```

#### Casting Flow

```
CastSpell(spell, optionalMaterials):
  if not spell.Learned:
    require bits >= spell.LearnCost
    consume bits
    spell.Learned = true
    // first cast proceeds at base power
  else:
    require mana >= spell.ManaCost
    consume mana

  power = spell.BasePower

  if optionalMaterials provided:
    require bits >= optionalMaterials
    consume bits
    power += CalculateAmplification(spell, optionalMaterials)

  execute spell at power level
```

#### Amplification Scaling

Materials amplify power based on **relevance** and **quantity**:

| Factor | Effect |
|--------|--------|
| **Matching bits** (bits in `AmplificationMaterials`) | +25% power per matching bit spent |
| **Extra matching bits** (above LearnCost quantity) | +10% per additional bit (diminishing) |
| **Non-matching bits** | +5% per bit (they help, but less efficiently) |
| **Full LearnCost repeated** | +50% power (you're recasting at full material fidelity) |

Example: A well-repair spell has `LearnCost: "BCC"` and `AmplificationMaterials: "BC"`.
- Cast with mana only: base power (well becomes functional)
- Cast with 1 extra C bit: +25% (well has slightly better output)
- Cast with full "BCC" again: +50% (well becomes a RestoredWell with enhanced properties)

This means players are never *stuck* for lack of materials after learning a spell, but hoarders and scavengers are rewarded for investing materials into critical moments.

#### How This Changes Repair

Repair spells become a natural subset of this system:

| Repair Outcome | Condition |
|----------------|-----------|
| Learn the repair spell | First time: pay full bit cost at the object |
| Basic repair (mana only) | Functional restoration -- well works, column stands |
| Amplified repair (mana + materials) | Exceptional restoration -- RestoredWell with bonus output, column reveals hidden passage |

This resolves Open Question #1 elegantly: repair always succeeds once learned (you understand the spell), but **quality scales with investment**. A mana-only repair gets the well working. Dumping materials into it makes it a landmark.

#### Interaction with Archive Stones

Archive stones become even more interesting under this system. Repairing a `DamagedArchiveStone` could:
- Teach you a new spell (the stone contained the spell's material formula)
- Reveal the `AmplificationMaterials` for a spell you already know (the stone documented the original mage's preferred components)
- Grant a one-time material cache (the stone was a supply record -- you now know where to find rare bits)

#### Impact on Existing Systems

| System | Change |
|--------|--------|
| `ActivatedAbility` | Add `ManaCost`, `LearnCost`, `Learned`, `AmplificationMaterials` fields |
| `BaseMutation` | Override `LearnCost` and `AmplificationMaterials` per mutation |
| `BitLockerPart` | Already supports `HasBits`/`UseBits` -- no changes needed |
| `TinkeringService` | `TryRepair` uses this spell-cost model instead of flat bit consumption |
| Entity stats | Add `Mana` stat alongside HP/MP |
| `ActivatedAbilitiesPart` | Validation checks mana before allowing ability use |
| Cooldowns | Remain separate from mana -- a spell can be off cooldown but you lack mana, or vice versa |

#### Design Tensions to Watch

1. **Learn cost shouldn't gate content too hard.** If a repair spell costs `BBCC` to learn and you don't have those bits, you can't fix the well at all. Mitigation: make learn costs modest, and let NPCs teach spells (Tinker NPC, Palimpsest Echo) so materials aren't the only path.

2. **Mana regen rate controls pacing.** Too fast and materials never matter. Too slow and the game feels stingy. Start conservative (slow regen, rest-based) and tune from playtesting.

3. **Amplification shouldn't feel mandatory.** Base-power spells should always be *sufficient*. Amplification is a bonus, not a requirement. No content should be locked behind amplified casts.

4. **This system should feel like the Frieren ethos**: the master mage who spent 80 years perfecting a single mundane spell isn't powerful because of raw mana -- she's powerful because she knows exactly which materials to feed the spell at the right moment.

---

## Suggested Implementation Order

| Priority | Task | Depends On | Lore Value |
|----------|------|------------|------------|
| 1 | Mana stat on player entity | Entity stats system | Foundation for all spell costs |
| 2 | Spell cost model (LearnCost / ManaCost / Learned flag) | Mana stat, ActivatedAbility | Core mechanic -- materials-first, mana-after |
| 3 | `RepairablePart` component | Spell cost model | Foundation for repair spells |
| 4 | `DriedWell` + `Well` repair flow | RepairablePart, TinkeringService | High -- tangible, satisfying |
| 5 | Material amplification system | Spell cost model, BitLockerPart | Rewards resource investment |
| 6 | `DamagedArchiveStone` blueprints | RepairablePart | High -- lore delivery + spell teaching |
| 7 | `TryRepair` in TinkeringService | RepairablePart, spell cost model | Core repair mechanic |
| 8 | Faction reputation on repair | Player reputation system (Phase 2 from FACTION_SYSTEM.md) | Connects repair to social world |
| 9 | Repair-related dialogue branches | Existing conversation system | Makes NPCs react to your restoration work |
| 10 | NPC spell teaching (learn without materials) | Conversation system, spell cost model | Alternative learning path |
| 11 | World-gen placement of repairable objects | Zone generation | Content distribution |
| 12 | `LiquidSourcePart` for functional wells | Inventory/liquid systems | Makes wells actually useful |
| 13 | Multi-step aqueduct chain quest | All above | Endgame infrastructure quest |

---

## Open Questions

1. **Should repair always succeed with sufficient bits, or use the full success/failure table from TINKERING_AND_CRAFTING.md?** The failure table adds tension but may frustrate players repairing unique world objects. Suggestion: world infrastructure repair always succeeds (you spent the bits), but quality varies (basic vs. exceptional restoration).

2. **Should repaired objects persist across save/load?** This requires the save system (P0 prerequisite in FACTION_SYSTEM.md). Until then, repaired objects could reset -- which actually fits the "enchantment entropy" theme (nothing stays fixed forever).

3. **Should the Rot Choir actively sabotage repairs?** Their lore strongly opposes restoration. This could create interesting tension: repair a well in Rot Choir territory and they might send a Tendril to re-break it.

4. **How many archive stones should exist?** Too few and they're forgettable. Too many and they dilute lore. Suggestion: 1-2 per major zone, with content tailored to the zone's faction presence.
