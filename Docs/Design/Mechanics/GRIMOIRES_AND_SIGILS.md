# Grimoires and Sigils: Spell Equipment and Enhancement System

## Design Philosophy

Grimoires are not just "spell scrolls you read and forget." They are **equipment** -- physical objects with weight, durability, and upgrade slots. The spell system splits into two tiers based on whether you need the book in hand to cast: cantrips (memorized permanently) and bound spells (require the grimoire equipped). This creates meaningful inventory tension and build identity.

Sigils are slottable modifiers that attach to grimoires, not to individual spells. This keeps the enhancement system manageable -- you're upgrading 1-2 equipped grimoires, not dozens of spells.

---

## Two-Tier Spell System

### Cantrips (Memorized Permanently)

- Learned by **consuming** a grimoire (the book is destroyed in the process -- a one-time knowledge transfer)
- Low-power, utility-focused
- Cost MP to cast, no other resource
- Always available regardless of equipment
- The player's reliable baseline -- they're never helpless

**Design intent:** Cantrips are the "boring spells that save your life" -- individually weak, always useful. A player with 4-5 cantrips has a versatile toolkit without needing any equipment slots. The grimoire being consumed means finding a cantrip grimoire is a permanent upgrade moment: "do I learn this now, or save the grimoire to trade?"

**Representative example:** *Mend* -- repair 1-2 durability on a held item. Slow but free maintenance. Utility that never stops being relevant.

### Bound Spells (Require Grimoire Equipped)

- Grimoire must be equipped in an **off-hand or belt slot** to cast its spells
- Heavy hitters -- offensive, defensive, and advanced utility
- The grimoire is a piece of equipment with its own weight, durability, rarity, and sigil slots
- You're literally reading incantations from the book mid-combat
- Grimoire is NOT consumed -- it persists as long as it has durability

**Equipment tension:** Sword + shield, or sword + grimoire? Two grimoires but no melee? Grimoire + lantern? Every slot given to a grimoire is a slot NOT given to something else. This is the core strategic decision.

**Multi-spell grimoires:** Each grimoire contains 2-3 thematically linked spells. You're not equipping one spell -- you're equipping a spell *school*. Choosing between grimoires is choosing a playstyle.

**Representative example:** *Grimoire of Conflagration* -- contains Fireball (AoE fire damage) and Flame Wall (line of fire lasting N turns). A coherent fire-themed offensive kit.

### Cantrip vs. Bound: Decision Framework

| Factor | Cantrip | Bound Spell |
|--------|---------|-------------|
| **Power** | Low | Medium-High |
| **Availability** | Always | Only with grimoire equipped |
| **Equipment cost** | None (memorized) | Off-hand or belt slot |
| **Enhancement** | Cannot be sigiled | Enhanced by grimoire's sigils |
| **Grimoire fate** | Consumed on learning | Persists as equipment |
| **Scaling** | Flat (no sigil interaction) | Scales with sigils and grimoire quality |
| **Role** | Utility baseline, quality-of-life | Build-defining power, combat identity |

---

## Grimoire Properties

Grimoires are items with the following properties:

| Property | Description |
|----------|-------------|
| **Weight** | Heavier = more powerful spells, but eats carry capacity |
| **Durability** | Degrades with use; at 0, spells fail until repaired. Cantrip grimoires break on learning. |
| **Sigil Slots** | 1 (common), 2 (uncommon), 3 (rare). Determines enhancement capacity. |
| **Attunement** | Some grimoires require minimum INT or faction reputation to equip |
| **Faction Origin** | Grimoires from different factions have different spell schools and aesthetics |

### Grimoire Sources

| Source | Type | Notes |
|--------|------|-------|
| Loot drops (dungeon) | Random | Found in chests, on shelves, dropped by mages |
| Faction vendors | Curated | Each faction sells grimoires aligned with their philosophy |
| Archive stones | Restored | Repairing archive stones can yield lost grimoires |
| Quest rewards | Unique | Named grimoires with special properties |
| Crafting (tinkering) | Player-made | Blank grimoire + spell inscription (advanced tinkering) |

### Grimoire Durability and Maintenance

Grimoires degrade with use. This creates a maintenance loop and prevents any single grimoire from being "equip and forget forever."

| Durability State | Effect |
|------------------|--------|
| 100-75% | Full power, no issues |
| 74-50% | Spells have a 5% fizzle chance (cast fails, MP still spent) |
| 49-25% | 15% fizzle chance, sigil effects at 75% power |
| 24-1% | 30% fizzle chance, sigil effects at 50% power |
| 0% | Cannot cast. Grimoire is "exhausted" until repaired. |

**Repair options:**
- Mend cantrip: slow, 1-2 durability per cast
- Tinkering workbench: faster, costs bits
- Inkbound restorers: pay drams for professional repair (faction service)

---

## Grimoire Schools (Classification System)

Grimoires are classified by **what the spell acts on**, not by vibes or theme. This determines which sigils can be slotted.

### Why Classification Matters

A Sigil of Immolation on a "Purify Water" grimoire is nonsensical. A Sigil of Diffusion ("single-target becomes AoE") on a spell that already targets terrain is meaningless. Universal sigils break down immediately when applied across fundamentally different spell types. But too many classifications (4+) fragments the loot pool and forces players to learn parallel systems. Three schools plus universal cost sigils is the sweet spot.

### The Three Schools

#### 1. Martial Grimoires -- Act on Creatures

Damage, healing, status effects, buffs/debuffs, summoning. The target is always an entity (enemy, ally, self, or summoned creature).

**Representative example:** *Fireball* -- AoE fire damage targeting creatures in a radius.

#### 2. Shaping Grimoires -- Act on the World

Terrain modification, liquid manipulation, infrastructure. The target is cells, tiles, liquids, objects. This naturally absorbs the entire alchemy spell school (see ALCHEMY_SPELL_SYSTEM.md).

**Representative example:** *Purify Water* -- cleanse a contaminated well or pool, changing its liquid properties.

#### 3. Inscription Grimoires -- Act on Items/Knowledge

Enchanting, identification, repair, crafting augmentation, lore extraction. The target is an item or information.

**Representative example:** *Extract Lore* -- read an archive stone to recover knowledge or a spell.

### Classification Rule

The test is unambiguous: if the spell targets a creature, it's Martial. If it targets terrain/liquid, it's Shaping. If it targets an item or knowledge, it's Inscription. No gray areas.

---

## Sigil System

### Core Concept

Sigils are inscribed modifiers that attach to a **grimoire**, not to individual spells. Every spell cast from that grimoire is affected by its sigils. This keeps the system simple while creating deep build variety.

### Sigil Slot Rules

- Each grimoire has 1-3 sigil slots based on rarity
- Sigils can be freely removed and re-slotted (no destruction penalty)
- Multiple sigils on the same grimoire stack/interact
- Sigils are physical items (found, bought, crafted) that occupy inventory until slotted

### School-Specific Sigil Pools

Each school has its own pool of sigils. A Martial sigil cannot be slotted into a Shaping grimoire. This prevents nonsensical combinations and ensures every sigil makes sense in context.

**Martial Sigils** (creature-targeting spells only)
Modify how spells interact with entities -- damage type, targeting shape, delivery timing.
*Representative example:* **Sigil of Echoes** -- spells cast twice at half power. Double-tap damage; double-tap healing. Effective against high-armor targets (two smaller hits vs one big hit).

**Shaping Sigils** (world-targeting spells only)
Modify how spells reshape the environment -- duration, area, quality, cascading.
*Representative example:* **Sigil of Permanence** -- shaping effects last 3x longer (permanent at high amplification). The core question for world spells is "how long does this last?"

**Inscription Sigils** (item-targeting spells only)
Modify how spells interact with objects and knowledge -- batch processing, depth of insight, duplication.
*Representative example:* **Sigil of Insight** -- identification spells reveal hidden properties (curses, secret uses, lore fragments). Normal Identify shows stats; with Insight, it shows who made it and what it's really for.

### Universal Cost Sigils (work on any grimoire school)

Cost sigils modify the *casting economy*, not the spell effect. "This spell costs HP instead of MP" is equally meaningful whether casting Fireball or Purify Water. These are the only sigils that cross school boundaries.

*Representative example:* **Sigil of Blood** -- spells cost HP instead of MP. Frees MP for your other grimoire but puts your health at risk. Pair with life-drain effects for a sustain-mage playstyle.

---

## Summoning Grimoires

Summoning is a **subtype of Martial grimoires**, not a separate school. Summoned creatures are entities, so Martial sigils apply naturally and create interesting interactions without needing a dedicated sigil pool.

Each summoning grimoire contains 2-3 thematically linked summon spells. Martial sigils modify summons in intuitive ways -- Echoes summons two copies at half stats, Immolation makes summons deal fire damage and explode on death, Inversion turns a summon spell into a charm/mind-control effect on an existing enemy.

The Inversion interaction is particularly interesting -- it turns every summoning grimoire into a conditional mind-control book. High risk (charm might fail on strong enemies), high reward (their best creature fights for you).

---

## Reflexes and Stances: Automated Response Systems

### The Problem

Managing equipment swaps and potion usage mid-combat is tedious. The player knows "when my HP drops below 30%, I want to drink a health potion" but executing that requires: notice HP, open inventory, find potion, use potion. That's not strategic depth -- it's busywork that punishes slow reaction time.

But fully automating combat decisions (scripting equipment swaps, ability rotations, etc.) removes tension and rewards system mastery over game mastery. If the player can AFK fights via scripts, combat stops being engaging -- wins feel hollow and losses feel like scripting errors rather than gameplay failures.

### Solution: Two Separate Systems

#### Reflexes (Automatic, Limited)

Reflexes are trained auto-responses to resource thresholds. Think muscle memory, not tactical genius. They are **cantrip-only** -- you can only automate weak, memorized spells, never bound grimoire power.

**Rules:**
- Only cantrips can be set as reflexes (not bound spells, not items, not equipment swaps)
- One reflex triggers per turn maximum (no chaining)
- The reflex consumes the cantrip's normal MP cost
- The reflex consumes your "reaction" for that turn (you can't also dodge, block, or counter)
- Maximum 3 active reflexes at a time (force prioritization)

**Configuration:** Player sets condition + response pairs.
*Representative example:* "When HP < 25%, cast Mend Self" -- auto-heal at low health using a cantrip.

**Why cantrip-only:** Cantrips are weak enough that automating them doesn't break the game. Auto-casting Spark (1-2 damage) when surrounded is a survival reflex, not a win condition. Auto-casting Fireball when surrounded would be a win condition -- that's why bound spells can't be reflexes.

**Strategic depth:** The 3-reflex limit forces prioritization. Do you auto-heal, auto-cleanse, AND auto-knockback? Or drop the knockback for auto-meditation (MP recovery)? Your reflex loadout tells a story about what you're afraid of.

#### Stances (Manual Swap, Pre-Configured Loadouts)

Instead of scripting equipment swaps, the player defines named loadout presets they switch between with a single keypress. The swap still costs a turn but it's one keypress instead of multiple inventory actions.

**Rules:**
- Maximum 3 defined stances
- Switching stance costs 1 full turn (you're rearranging equipment)
- Each stance defines: main hand, off-hand, and optionally belt slot
- Items must be in inventory to equip (stances don't conjure items)
- Stance names are player-defined for personalization

**Representative example:**
| Stance | Main Hand | Off-Hand | Belt |
|--------|-----------|----------|------|
| "Battle" | Iron Sword | Shield | Health Tonic |
| "Arcane" | Grimoire of Conflagration | Grimoire of Mending | Mana Tonic |
| "Scout" | Dagger | Torch | Antidote |

**Why stances instead of scripted swaps:**
- The player decides *when* to switch, preserving tactical agency
- The turn cost makes switching a real decision ("do I spend this turn swapping or fighting?")
- Pre-combat stance selection is strategic ("this zone has undead, I'll start in Arcane stance")
- No automation removes tension -- you still have to read the situation

**Why NOT scripted equipment swaps:**
- "When MP = 0, equip sword" sounds useful but removes the interesting decision: maybe you should keep the grimoire and use Blood sigil, or drink a mana potion, or retreat. The game is about making those calls under pressure.
- Free automatic swaps make equipment slots meaningless -- you effectively have access to everything simultaneously.

### Reflexes + Stances Interaction

These two systems complement each other:
- Reflexes handle **reactive micro** (auto-heal when low, auto-cleanse when poisoned) -- things where the optimal response is always the same
- Stances handle **proactive macro** (switch to Battle stance for this fight, switch to Scout stance for exploration) -- things where context determines the right choice

---

## Open Questions

1. **Can sigils be crafted, or only found/bought?** If craftable, tinkering gains a new output category. If found-only, sigils become a loot chase. Hybrid (common sigils craftable, rare ones loot-only) might be best.

2. **Should cantrips scale with player level?** If yes, they stay relevant but blur the line with bound spells. If no, they're a reliable baseline that you eventually outgrow offensively (but utility cantrips like Detect Traps never stop being useful).

3. **Can enemies use sigiled grimoires?** An enemy mage with Blood + Siphoning is a terrifying sustain fight. Enemies with Delay + Diffusion force the player to read telegraphed attacks and reposition. This would make enemy mages feel distinct based on their loadout.

4. **Interaction with the alchemy spell system?** Alchemy spells (see ALCHEMY_SPELL_SYSTEM.md) are cast on liquids and terrain. If alchemy spells are bound to Shaping grimoires, then Shaping sigils modify alchemy -- Permanence on a well transmutation makes it last forever. Expansion on a Flood fills a larger area. This could be very powerful.

5. **Summoning duration/limits:** Should summons be permanent until killed, or timed? Timed creates more tactical pressure. Permanent makes summoners feel like pet classes. A middle ground: summons last N turns but can be extended.

6. **Reflex learning curve:** Should reflexes be available from the start, or unlocked via a skill/item? Starting with 1 reflex slot and unlocking more through gameplay creates progression. But gating QoL behind progression can feel punishing.

7. **Stance swap cost:** Should stance swapping always cost a full turn? Certain items/skills could reduce swap cost (e.g., "Quick Draw" makes stance swaps free once every 10 turns).

8. **Can enemies have reflexes?** An enemy with auto-heal at 25% HP changes how you fight them (burst past the threshold, or drain their MP first). Enemy reflexes make fights more puzzle-like.

---

## Brainstorm Appendix: Specific Content Ideas

> These are brainstormed examples, NOT final designs. Specific grimoires, sigils, spells, and numbers should be determined once core gameplay systems are more fleshed out and playtested. Preserved here for reference.

### Cantrip Ideas
Spark (minor fire), Minor Light, Detect Traps, Minor Telekinesis, Mend, Meditate (MP recovery), Minor Purify (cleanse status)

### Bound Grimoire Ideas
Conflagration (fire), Deep Tide (water), Mending (healing), Pale Ward (defense/banish), Hollow Pack (shadow summons), Living Stone (earth summons/defense), Spore and Root (poison/nature summons)

### Martial Sigil Ideas
Immolation (fire), Frost (slow/freeze), Rot (poison DoT), Static (chain), Void (pull), Echoes (double cast), Diffusion (AoE), Siphoning (life drain), Inversion (self-target/reverse), Delay (timed), Reach (range)

### Shaping Sigil Ideas
Permanence (duration), Expansion (area), Purity (quality), Sympathy (cascade to adjacent), Reversal (toggle on/off), Attunement (cheaper in shaped areas)

### Inscription Sigil Ideas
Thoroughness (batch processing), Insight (hidden info), Imprint (copy enchant), Preservation (durability boost), Reclamation (recover lost enchants)

### Universal Cost Sigil Ideas
Blood (HP cost), Patience (cooldown, no MP), Greed (dram cost), Sacrifice (durability cost), Ritual (double cost + double effect)

### Faction-Aligned Sigil Ideas
Palimpsest: territory marking. Rot Choir: bonus vs damaged targets. Saccharine Concord: extra dram drops. Pale Curation: precision/anti-AoE. Slime Collective: kill-healing.

### Emergent Combo Ideas
Blood + Siphoning (HP cycling sustain mage), Delay + Diffusion (delayed AoE denial), Diffusion + Ritual (AoE heals at double power), Reach + Echoes (long-range double shots), Rot + Inversion (self-poison for resistance?)
