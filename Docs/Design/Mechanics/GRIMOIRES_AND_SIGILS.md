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

**Examples:**
| Cantrip | Effect | Notes |
|---------|--------|-------|
| Spark | 1-2 fire damage, chance to ignite | Cheapest offensive option; doubles as a lighter |
| Minor Light | Illuminate a small radius for N turns | Exploration utility; replaces torch dependency |
| Detect Traps | Briefly reveal traps in FOV | Risk reduction; doesn't disarm |
| Minor Telekinesis | Move a small object 2-3 tiles | Pull items, trigger pressure plates at range |
| Mend | Repair 1-2 durability on a held item | Slow but free maintenance |

**Design intent:** Cantrips are the "boring spells that save your life" -- individually weak, always useful. A player with 4-5 cantrips has a versatile toolkit without needing any equipment slots. The grimoire being consumed means finding a cantrip grimoire is a permanent upgrade moment: "do I learn this now, or save the grimoire to trade?"

### Bound Spells (Require Grimoire Equipped)

- Grimoire must be equipped in an **off-hand or belt slot** to cast its spells
- Heavy hitters -- offensive, defensive, and advanced utility
- The grimoire is a piece of equipment with its own weight, durability, rarity, and sigil slots
- You're literally reading incantations from the book mid-combat
- Grimoire is NOT consumed -- it persists as long as it has durability

**Equipment tension:** Sword + shield, or sword + grimoire? Two grimoires but no melee? Grimoire + lantern? Every slot given to a grimoire is a slot NOT given to something else. This is the core strategic decision.

**Examples of bound grimoire spells:**
| Grimoire | Spell | Effect |
|----------|-------|--------|
| Grimoire of Conflagration | Fireball | AoE fire damage in a radius |
| Grimoire of Conflagration | Flame Wall | Create a line of fire lasting N turns |
| Grimoire of the Deep Tide | Waterbolt | Ranged water damage, pushes target |
| Grimoire of the Deep Tide | Flood | Fill a small area with water |
| Grimoire of Mending | Greater Restore | Heal a significant amount of HP |
| Grimoire of Mending | Purify Body | Remove poison/disease status effects |
| Grimoire of the Pale Ward | Shield Rune | Absorb next N points of damage |
| Grimoire of the Pale Ward | Banish | Force a creature to teleport away |

**Multi-spell grimoires:** Each grimoire contains 2-3 thematically linked spells. You're not equipping one spell -- you're equipping a spell *school*. Choosing between grimoires is choosing a playstyle.

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

---

## Sigil System

### Core Concept

Sigils are inscribed modifiers that attach to a **grimoire**, not to individual spells. Every spell cast from that grimoire is affected by its sigils. This keeps the system simple while creating deep build variety.

### Sigil Slot Rules

- Each grimoire has 1-3 sigil slots based on rarity
- Sigils can be removed, but there is a **20% chance the sigil shatters on removal** -- this creates commitment without permanent lock-in
- Multiple sigils on the same grimoire stack/interact
- Sigils are physical items (found, bought, crafted) that occupy inventory until slotted

### Sigil Categories

#### Elemental Sigils (Change Damage/Effect Type)

Transform what a spell *does* at a fundamental level. These are the most dramatic modifiers -- they can turn utility spells into offensive tools and vice versa.

| Sigil | Effect | Interaction Examples |
|-------|--------|---------------------|
| **Sigil of Immolation** | Spells gain fire damage component, chance to ignite | Telekinesis throws a *burning* object. Light cantrip becomes a flash-burn. |
| **Sigil of Frost** | Spells slow targets, water tiles freeze when hit | Waterbolt becomes ice bolt. Flood creates an ice rink. |
| **Sigil of Rot** | Spells apply poison DoT, healing reduced on target | Fireball leaves a toxic cloud. Mending spell on enemies poisons them. |
| **Sigil of Static** | Spells chain to one adjacent target at half power | Flame Wall arcs lightning between flames. Shield Rune retaliates on hit. |
| **Sigil of Void** | Spells pull targets 1-2 tiles toward impact point | Fireball becomes implosion. Banish pulls before pushing. |

#### Mechanical Sigils (Change Spell Behavior)

These don't change what element a spell uses -- they change the *shape* of how it works.

| Sigil | Effect | Strategic Use |
|-------|--------|---------------|
| **Sigil of Echoes** | Spells cast twice at half power (same target) | Double-tap damage; double-tap healing. Effective against high-armor targets (two smaller hits vs one big hit). |
| **Sigil of Diffusion** | Single-target spells become AoE at reduced effect | Turn a single-target heal into a party heal. Turn a focused nuke into crowd control. |
| **Sigil of Siphoning** | Spells drain HP from target to caster, but cost +50% MP | Sustain-mage playstyle. Every offensive spell is also a heal. |
| **Sigil of Inversion** | Offensive spells can target self as buffs; healing spells become damage | Fireball yourself for fire resistance. Cast heal on an undead for massive damage. High risk/reward. |
| **Sigil of Delay** | Spells trigger after a 2-turn delay but at +75% power | Area denial, trap-setting. Cast fireball on a chokepoint, enemies walk into it. |
| **Sigil of Reach** | Spell range doubled, but power reduced by 25% | Sniper-mage. Engage before enemies close distance. |

#### Cost Sigils (Alter the Casting Economy)

The most strategically impactful category. These change *what resource you spend* to cast, enabling fundamentally different playstyles.

| Sigil | Effect | Strategic Use |
|-------|--------|---------------|
| **Sigil of Blood** | Spells cost HP instead of MP | Glass cannon mage. Pair with Siphoning to sustain the HP cycle. Dangerous but frees MP for other grimoire. |
| **Sigil of Patience** | Spells cost 0 MP but gain a cooldown (N turns) | Rewards diverse spell loadouts -- rotate cooldowns across spells. Terrible for spamming one spell. |
| **Sigil of Greed** | Spells cost drams (gold) instead of MP | Lategame money-sink power option. "Pay to win" as an in-universe mechanic. The Saccharine Concord's favorite. |
| **Sigil of Sacrifice** | Spells cost grimoire durability instead of MP | Burn through your book for burst damage. Desperate power at the cost of your equipment. |
| **Sigil of Ritual** | Spells cost double MP but have double duration/effect | Slow-and-steady mage. Fewer casts, each one matters more. |

---

## Sigil Combinations and Emergent Strategy

The sigil system's depth comes from multi-sigil interactions on the same grimoire. Some powerful combos:

### The Blood Engine (Blood + Siphoning)
Spells cost HP to cast (Blood) but drain HP from targets (Siphoning). Net result: as long as you're hitting enemies, casting is "free." Miss or fight HP-immune targets and you bleed out. High skill ceiling.

### The Delayed Diffusion (Delay + Diffusion)
Single-target spells become delayed AoE. Cast a fireball that detonates in 2 turns across a wide area. Insane area denial for corridor fights. Terrible in chaotic melee.

### The Generous Healer (Diffusion + Ritual)
AoE heals at double power. The ultimate support build. But you're spending 2 sigil slots on a healing grimoire, leaving your offensive grimoire with only 1 slot.

### The Glass Turret (Reach + Echoes)
Double-range, double-cast. Snipe from across the room with two hits. But at half power each and -25% from Reach, individual damage is low. Works on squishies, bad against armor.

### The Entropic Mage (Rot + Inversion)
Poison your enemies normally. Invert to poison yourself for... a poison resistance buff? Or invert a healing spell to deal poison damage. The Rot Choir approves of this build philosophically.

---

## Faction-Aligned Sigils

Each faction has signature sigils that reflect their philosophy. These can only be obtained through faction reputation or faction-specific loot.

| Faction | Signature Sigil | Effect |
|---------|----------------|--------|
| **Palimpsest** | Sigil of Inscription | Spells leave a permanent mark on the target tile (ink glyph). Marked tiles give the caster +1 range when standing on them. Build your "territory." |
| **Rot Choir** | Sigil of Entropy | Spells have +30% effect on damaged/degraded targets. Rewards finishing blows and targeting weakened enemies. |
| **Saccharine Concord** | Sigil of Commerce | Killing with a sigiled spell drops +50% drams from the target. The merchant-mage build. |
| **Pale Curation** | Sigil of Precision | Spells ignore 50% of target's magic resistance, but cannot be AoE (overrides Diffusion). Quality over quantity. |
| **Slime Collective** | Sigil of Absorption | Spells that kill a target heal the caster for 10% of target's max HP. The slime way: consume to grow. |

---

## Grimoire Durability and Maintenance

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
- Sigil of Sacrifice actively burns durability -- powerful but consumes the grimoire faster

---

## Cantrip vs. Bound: Decision Framework

| Factor | Cantrip | Bound Spell |
|--------|---------|-------------|
| **Power** | Low | Medium-High |
| **Availability** | Always | Only with grimoire equipped |
| **Equipment cost** | None (memorized) | Off-hand or belt slot |
| **Enhancement** | Cannot be sigiled | Enhanced by grimoire's sigils |
| **Grimoire fate** | Consumed on learning | Persists as equipment |
| **Scaling** | Flat (no sigil interaction) | Scales with sigils and grimoire quality |
| **Role** | Utility baseline, quality-of-life | Build-defining power, combat identity |

This split means a "pure martial" character still benefits from learning 2-3 cantrips (Spark for lighting, Mend for gear maintenance, Detect Traps for safety) without committing equipment slots to magic. A dedicated mage builds around 1-2 grimoires with carefully chosen sigils.

---

## Open Questions

1. **Can sigils be crafted, or only found/bought?** If craftable, tinkering gains a new output category. If found-only, sigils become a loot chase. Hybrid (common sigils craftable, rare ones loot-only) might be best.

2. **Should cantrips scale with player level?** If yes, they stay relevant but blur the line with bound spells. If no, they're a reliable baseline that you eventually outgrow offensively (but utility cantrips like Detect Traps never stop being useful).

3. **Grimoire sets?** Two grimoires from the same "school" could grant a set bonus when both are equipped (one in each hand). This rewards specialization but costs both equipment slots. E.g., both Conflagration grimoires equipped = passive fire resistance.

4. **Can enemies use sigiled grimoires?** An enemy mage with Blood + Siphoning is a terrifying sustain fight. Enemies with Delay + Diffusion force the player to read telegraphed attacks and reposition. This would make enemy mages feel distinct based on their loadout.

5. **Sigil discovery vs. sigil identification?** Found sigils could be unidentified until used or appraised, adding a gamble element. Or they could always be identified to keep the decision space clean.

6. **Interaction with the alchemy spell system?** Alchemy spells (see ALCHEMY_SPELL_SYSTEM.md) are cast on liquids and terrain. If alchemy spells are bound to grimoires, then sigils modify alchemy too: a Frost sigil on an alchemy grimoire means your Transmute Water spell freezes the result. An Immolation sigil on Kindle makes it explosive. This could be very powerful.

---

## Grimoire Schools (Classification System)

Grimoires are classified by **what the spell acts on**, not by vibes or theme. This determines which sigils can be slotted.

### Why Classification Matters

A Sigil of Immolation on a "Purify Water" grimoire is nonsensical. A Sigil of Diffusion ("single-target becomes AoE") on a spell that already targets terrain is meaningless. Universal sigils break down immediately when applied across fundamentally different spell types. But too many classifications (4+) fragments the loot pool and forces players to learn parallel systems. Three schools plus universal cost sigils is the sweet spot.

### The Three Schools

#### 1. Martial Grimoires -- Act on Creatures

Damage, healing, status effects, buffs/debuffs, summoning. The target is always an entity (enemy, ally, self, or summoned creature).

**Combat examples:** Fireball, Waterbolt, Shield Rune, Banish
**Healing examples:** Greater Restore, Purify Body
**Summoning examples:** See "Summoning Grimoires" section below

#### 2. Shaping Grimoires -- Act on the World

Terrain modification, liquid manipulation, infrastructure. The target is cells, tiles, liquids, objects. This naturally absorbs the entire alchemy spell school (see ALCHEMY_SPELL_SYSTEM.md).

**Terrain examples:** Flame Wall, Flood, Oil Coat, Salt Ward
**Alchemy examples:** Purify Water, Transmute Well, Seeking Water, Calcify
**Infrastructure examples:** Freeze Pool (create traversable ice), Erode Wall (alternative entry)

#### 3. Inscription Grimoires -- Act on Items/Knowledge

Enchanting, identification, repair, crafting augmentation, lore extraction. The target is an item or information.

**Repair examples:** Greater Mend, Fortify (increase durability)
**Knowledge examples:** Identify, Extract Lore (read archive stones)
**Enchanting examples:** Inscribe Rune (add property to weapon), Sharpen (temporary damage bonus)

### Classification Rule

The test is unambiguous: if the spell targets a creature, it's Martial. If it targets terrain/liquid, it's Shaping. If it targets an item or knowledge, it's Inscription. No gray areas.

---

## School-Specific Sigil Pools

### Martial Sigils (creature-targeting spells only)

| Sigil | Effect |
|-------|--------|
| **Sigil of Immolation** | Spells gain fire damage component, chance to ignite |
| **Sigil of Frost** | Spells slow targets, water tiles freeze when hit |
| **Sigil of Rot** | Spells apply poison DoT, healing reduced on target |
| **Sigil of Static** | Spells chain to one adjacent target at half power |
| **Sigil of Void** | Spells pull targets 1-2 tiles toward impact point |
| **Sigil of Echoes** | Spells cast twice at half power (same target) |
| **Sigil of Diffusion** | Single-target spells become AoE at reduced effect |
| **Sigil of Siphoning** | Spells drain HP from target to caster, +50% MP cost |
| **Sigil of Inversion** | Offensive spells can target self as buffs; heals become damage |
| **Sigil of Delay** | Spells trigger after 2-turn delay at +75% power |
| **Sigil of Reach** | Double range, -25% power |

### Shaping Sigils (world-targeting spells only)

| Sigil | Effect | Design Reasoning |
|-------|--------|-----------------|
| **Sigil of Permanence** | Shaping effects last 3x longer (permanent at high amplification) | The core question for world spells is "how long does this last?" Most valuable shaping sigil. |
| **Sigil of Expansion** | Area of effect doubled | Flood fills 8 tiles instead of 4. Oil Coat covers a whole corridor. |
| **Sigil of Purity** | Transmutation results are higher quality | Purified well produces cleaner water. Frozen floor is more slippery. Quality over quantity. |
| **Sigil of Sympathy** | Shaping spell chains to adjacent similar targets | Purify one well, connected wells also purify. Freeze one water tile, adjacent water freezes too. |
| **Sigil of Reversal** | Spell can undo its own previous effect (toggle on/off) | Freeze a pool now, unfreeze it later. Create flame wall, dismiss it when allies need to pass. |
| **Sigil of Attunement** | Reduced MP cost for spells cast on previously shaped terrain | The more you work an area, the cheaper it gets. Rewards establishing and maintaining a home base. |

### Inscription Sigils (item-targeting spells only)

| Sigil | Effect | Design Reasoning |
|-------|--------|-----------------|
| **Sigil of Thoroughness** | Repair/enchant affects entire inventory stack, not one item | Mend all gear in one cast. Identify all items in a container. Batch processing. |
| **Sigil of Insight** | Identification reveals hidden properties (curses, secret uses, lore) | Normal Identify shows stats. With Insight, shows who made it, why it exists, what it's really for. |
| **Sigil of Imprint** | Enchantments can be copied to one additional item of same type | Fire rune on one sword, duplicate to a second. Constrained duplication. |
| **Sigil of Preservation** | Targeted items gain +50% durability | Everything you touch lasts longer. The careful craftsman's sigil. |
| **Sigil of Reclamation** | Repair spells recover enchantments lost to degradation | When items hit 0 durability, enchantments are normally lost. This recovers them. Niche but powerful for high-value gear. |

### Universal Sigils (work on any grimoire school)

Cost sigils modify the *casting economy*, not the spell effect. "This spell costs HP instead of MP" is equally meaningful whether casting Fireball or Purify Water.

| Sigil | Effect |
|-------|--------|
| **Sigil of Blood** | Spells cost HP instead of MP |
| **Sigil of Patience** | Spells cost 0 MP but gain a cooldown (N turns) |
| **Sigil of Greed** | Spells cost drams (gold) instead of MP |
| **Sigil of Sacrifice** | Spells cost grimoire durability instead of MP |
| **Sigil of Ritual** | Spells cost double MP but have double duration/effect |

### Sigil Budget

~6 sigils per school + ~5 universal cost sigils = ~23 total sigils. Manageable loot pool where each drop is meaningful, and the player only needs to learn one school's sigils deeply for their build.

---

## Summoning Grimoires

Summoning is a **subtype of Martial grimoires**, not a separate school. Summoned creatures are entities, so Martial sigils apply naturally and create interesting interactions without needing a dedicated sigil pool.

### Summoning Spells

| Grimoire | Spell | Summon |
|----------|-------|--------|
| Grimoire of the Hollow Pack | Call Shade | Summon a shadow creature that flanks your target |
| Grimoire of the Hollow Pack | Shadow Swarm | Summon 3 weak shades that overwhelm |
| Grimoire of Living Stone | Conjure Sentinel | Summon a slow, high-HP stone defender |
| Grimoire of Living Stone | Earthen Wall | Summon a line of destructible stone pillars (quasi-summon/terrain hybrid) |
| Grimoire of Spore and Root | Fungal Ally | Summon a mushroom creature that releases poison spores on death |
| Grimoire of Spore and Root | Entangle | Summon grasping roots at target location (immobilize) |

### Martial Sigils on Summoning Grimoires

| Sigil | Summoning Interaction |
|-------|----------------------|
| **Echoes** | Summon two copies at half HP/damage each |
| **Diffusion** | Summon appears at a random position within a radius instead of targeted (weaker control, but can surprise-flank) |
| **Siphoning** | Summon drains HP from enemies it attacks and heals the caster |
| **Immolation** | Summon deals fire damage on contact and explodes on death |
| **Frost** | Summon slows enemies it strikes; leaves frost on tiles it walks over |
| **Delay** | Summon appears after 2 turns but at +75% stats |
| **Reach** | Summon appears at double range from caster |
| **Void** | Summon pulls nearby enemies toward itself (taunt-like) |
| **Inversion** | Instead of summoning an ally, temporarily *charm* an enemy into fighting for you |

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

**Configuration:** Player sets condition + response pairs:

| Condition | Cantrip Response | Example |
|-----------|-----------------|---------|
| HP below X% | Healing cantrip | "When HP < 25%, cast Mend Self" |
| MP below X | MP recovery cantrip | "When MP < 5, cast Meditate" |
| Adjacent enemy count > N | Defensive cantrip | "When 3+ enemies adjacent, cast Spark (knockback)" |
| Status effect applied | Cleanse cantrip | "When poisoned, cast Minor Purify" |
| Ally HP below X% | Ally healing cantrip | "When ally HP < 20%, cast Mend Other" |

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

**Example loadouts:**

| Stance | Main Hand | Off-Hand | Belt | Use Case |
|--------|-----------|----------|------|----------|
| "Battle" | Iron Sword | Shield | Health Tonic | Melee combat, defensive |
| "Arcane" | Grimoire of Conflagration | Grimoire of Mending | Mana Tonic | Full caster, dual grimoire |
| "Scout" | Dagger | Torch | Antidote | Exploration, trap-heavy zones |

**Why stances instead of scripted swaps:**
- The player decides *when* to switch, preserving tactical agency
- The turn cost makes switching a real decision ("do I spend this turn swapping or fighting?")
- Pre-combat stance selection is strategic ("this zone has undead, I'll start in Arcane stance")
- No automation removes tension -- you still have to read the situation

**Why NOT scripted equipment swaps:**
- "When MP = 0, equip sword" sounds useful but removes the interesting decision: maybe you should keep the grimoire and use Blood sigil, or drink a mana potion, or retreat. The game is about making those calls under pressure.
- Free automatic swaps make equipment slots meaningless -- you effectively have access to everything simultaneously.
- Scripted swaps that cost a turn are strictly worse than player-decided swaps (the script can't read context like "there's a wall behind me" or "the enemy is one hit from death").

### Reflexes + Stances Interaction

These two systems complement each other:
- Reflexes handle **reactive micro** (auto-heal when low, auto-cleanse when poisoned) -- things where the optimal response is always the same
- Stances handle **proactive macro** (switch to Battle stance for this fight, switch to Scout stance for exploration) -- things where context determines the right choice

A player in "Arcane" stance with reflexes set to auto-Mend at 25% HP and auto-Spark at 3+ adjacent enemies has a coherent build: cast from range, auto-heal if hurt, auto-knockback if swarmed, manually switch to "Battle" stance if MP runs dry. Every part of that involves a real decision except the two cantrip-tier reflexes.

---

## Open Questions (Continued)

7. **Summoning duration/limits:** Should summons be permanent until killed, or timed? Timed creates more tactical pressure. Permanent makes summoners feel like pet classes. A middle ground: summons last N turns but Sigil of Permanence (if allowed cross-school, or a Martial variant) extends them.

8. **Reflex learning curve:** Should reflexes be available from the start, or unlocked via a skill/item? Starting with 1 reflex slot and unlocking more through gameplay (level-ups, trainer NPCs) creates a progression curve. But gating a QoL feature behind progression can feel punishing.

9. **Stance swap cost:** Should stance swapping always cost a full turn? A half-turn (act after swapping but at reduced effectiveness) might feel less punishing. Or: certain items/skills could reduce swap cost (e.g., a "Quick Draw" skill that makes stance swaps free once every 10 turns).

10. **Can enemies have reflexes?** An enemy with auto-heal at 25% HP changes how you fight them (burst them down past the threshold, or drain their MP first). Enemy reflexes make fights more puzzle-like.

11. **Stance persistence across zones?** Does your stance carry between floors, or reset to a default? Carrying rewards preparation; resetting forces re-evaluation of each zone.
