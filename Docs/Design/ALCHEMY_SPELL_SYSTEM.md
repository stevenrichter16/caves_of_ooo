# Alchemy as Spell Category: Transmutation, Infrastructure, and Living Liquids

## Design Philosophy

Alchemy in most games is a crafting submenu: combine ingredient A + ingredient B, get potion C. It's shopping with extra steps.

In Caves of Ooo, **alchemy is a spell school**. Alchemical spells follow the same economics as all other spells (materials to learn, mana to cast, materials to amplify). But where combat spells act on enemies and repair spells act on objects, **alchemical spells act on liquids, materials, and the properties of things**.

This means alchemy isn't something you do at a workbench. It's something you cast -- on a well, on a pool, on a container, on the ground, on an enemy covered in slime. The world's liquid system (27 liquid types with physical properties, mixing, temperature, contact effects) becomes a spellcasting surface.

> Alchemy is the magic of *what things are made of*.

---

## Core Mechanic: Alchemical Transmutation Spells

### The Verb

Every alchemical spell has the same basic shape:

```
Target a liquid source (well, pool, container, creature covered in liquid)
  -> Transform its properties or composition
  -> Result depends on base power + amplification
```

### Spell Learning (Same System as All Spells)

First cast requires materials (bits). After that, mana only. Materials amplify.

What makes alchemy spells distinctive: **the LearnCost often includes a sample of the target liquid**. You can't learn to transmute water into honey without having worked with both. This creates a natural exploration loop: find new liquids in the caves, experiment with them, learn new transmutations.

---

## Well Alchemy: Changing What Wells Produce

### Base Mechanic

A functional `Well` has a `LiquidSourcePart` that defines what it outputs. Alchemical spells can **retune** a well's output.

| Spell | LearnCost | ManaCost | Effect |
|-------|-----------|----------|--------|
| Sweeten the Source | `BC` + 1 dram honey | 8 | Well produces honey-water (hydration + food value) |
| Ink the Depths | `RC` + 1 dram ink | 12 | Well produces ink (Inkbound trade good, archive restoration material) |
| Purge the Taint | `BCC` | 6 | Removes poison/contamination from a well; PoisonedWell -> Well |
| Bitter Tonic | `BC` + 1 dram acid | 10 | Well produces dilute acid-water (minor healing tonic on drink) |
| Vintner's Tap | `BGC` + 1 dram cider | 15 | Well produces cider (trade value, social lubricant with NPCs) |
| Congeal the Flow | `BCC` + 1 dram gel | 10 | Well produces gel (slippery terrain weapon, crafting ingredient) |
| Liquefy Memory | `RBC` + 1 dram ink | 20 | Well produces brain brine (grants confused visions that are actually lore fragments) |

### Amplification Effects on Wells

| Amplification Level | Effect |
|---------------------|--------|
| Mana only (base) | Well produces the new liquid, but reverts after ~200 turns |
| +1-2 matching bits | Lasts ~500 turns |
| Full LearnCost repeated | Permanent transmutation |
| Full LearnCost + extra | Permanent + enhanced purity (stronger drink effects) |

This means early-game well alchemy is temporary and experimental. Late-game, with material investment, you can permanently reshape infrastructure. A player who turns a dried well into a permanent honey-water source near a village has done something that *matters* to the world.

### Faction Reactions to Well Alchemy

| Faction | Reaction | Reasoning |
|---------|----------|-----------|
| Villagers | Approve (practical liquids), wary (exotic liquids) | "You turned our well into... cider? The elders will discuss this." |
| Palimpsest | Approve (ink wells especially) | "You've given the stone a new memory. This is the work." |
| Rot Choir | Approve of corruption, disapprove of purification | "The taint was honest. Your 'purification' is a lie painted over truth." |
| Saccharine Concord | Strongly approve (trade value) | "A honey well! Do you know what this is worth per dram?" |
| Slimes | React to slime/gel wells | Slimes are drawn to wells producing their native liquid |

---

## Alchemy Spell Categories

### Category 1: Transmutation (Liquid -> Different Liquid)

The signature alchemy verb. Change what a liquid *is*.

| Spell | LearnCost | Effect | Notes |
|-------|-----------|--------|-------|
| Water to Wine | `BC` + water | Convert water pool/container to wine | Classic. Trade value. Social uses. |
| Calcify | `CC` + salt | Convert any liquid to salt (solid) | Defensive -- solidify a pool blocking a passage |
| Render to Oil | `BC` + any organic liquid | Convert blood/slime/sap to oil | Fuel, fire traps, slippery terrain |
| Distill Essence | `RBC` + any liquid | Extract 1 dram of "pure" version from a mixed pool | Key utility for getting clean ingredients from contaminated sources |
| Primordial Reduction | `RRBC` + any liquid | Convert liquid to primordial soup (proteangunk) | The universal solvent of transmutation; precursor for advanced recipes |
| Ink Synthesis | `RC` + blood or sap | Convert organic liquid to ink | Inkbound faction trade good; archive stone restoration material |

### Category 2: Property Alteration (Same Liquid, Different Behavior)

Don't change *what* a liquid is -- change *how* it behaves.

| Spell | LearnCost | Effect | Notes |
|-------|-----------|--------|-------|
| Thicken | `CC` | Increase viscosity (lower Fluidity) | Slow-flowing liquids for traps, barriers |
| Thin | `BC` | Decrease viscosity (higher Fluidity) | Make honey flow like water, spreading further |
| Chill | `BC` | Lower temperature toward freeze point | Create ice barriers, freeze pools for traversal |
| Kindle | `RC` | Raise temperature toward vapor point | Steam creation, cooking, evaporating obstacles |
| Insulate | `BCC` | Reduce thermal conductivity | Protect a liquid from temperature changes |
| Suppress Combustion | `BCC` | Lower combustibility | Make oil safe to store near fire |
| Electrify | `RC` | Increase electrical conductivity | Turn a pool into a trap, or make oil conductive |

### Category 3: Infusion (Add Properties to Non-Liquid Things)

The most unusual alchemy category. Apply liquid properties *to solid objects or terrain*.

| Spell | LearnCost | Effect | Notes |
|-------|-----------|--------|-------|
| Wax Seal | `CC` + wax | Make a container permanently sealed | Preservation, trap-proofing |
| Acid Etch | `BC` + acid | Weaken a wall, door, or solid object | Alternative to brute-force entry |
| Oil Coat | `BC` + oil | Make a surface slippery | Tactical terrain modification |
| Honey Bind | `BC` + honey | Make a surface sticky | Trap creation, climbing aid |
| Ink Mark | `RC` + ink | Inscribe a permanent mark on terrain | Wayfinding, claiming territory for Inkbound reputation |
| Salt Ward | `BCC` + salt | Create a line of salt that repels certain creatures | Ward creation, classic folklore defense |

### Category 4: Living Alchemy (Liquids That Do Things on Their Own)

The most advanced and strange category. Enchant liquids so they have autonomous behavior. This is where alchemy stops being chemistry and starts being the Frieren "living spell" concept.

| Spell | LearnCost | ManaCost | Effect |
|-------|-----------|----------|--------|
| Seeking Water | `RBCC` | 20 | A pool of water slowly flows toward the nearest dry well, filling it |
| Hungry Acid | `RBC` | 15 | A pool of acid slowly eats through walls in a chosen direction |
| Guardian Gel | `RBCC` | 18 | A pool of gel moves to block hostile creatures entering an area |
| Messenger Ink | `RRC` | 25 | A trail of ink flows from your location to a target NPC, leaving a readable message |
| Tidal Memory | `RRBC` | 30 | A pool of brain brine replays a "memory" of events that happened at its location |

These are expensive, high-tier spells. They're also the most Frieren-like: "someone spent 80 years perfecting a spell that makes water find its way home."

**Lore connection**: Living alchemy spells are the remnants of what the original cave builders used. The dried wells had "seeking water" enchantments. The archive stones were maintained by "messenger ink" spells. The player isn't inventing these -- they're rediscovering them.

---

## Alchemy Interactions with Existing Systems

### Tinkering Integration

Alchemy and tinkering share the bit economy but serve different purposes:

| System | Acts On | Creates | Resource Flow |
|--------|---------|---------|---------------|
| Tinkering | Items | Items | Bits -> finished goods |
| Alchemy | Liquids, terrain, infrastructure | Transformed world state | Bits + liquid samples -> spell knowledge |

They complement rather than compete. A tinkerer repairs a well's *structure*. An alchemist changes what the well *produces*. Both cost bits, but in different contexts.

### Cooking Integration

Alchemy-produced liquids feed directly into the cooking system:

- A well transmuted to produce honey-water gives cooks a reliable ingredient source
- Distill Essence lets cooks extract clean ingredients from contaminated dungeon pools
- Property-altered liquids (thickened, chilled) could enable new cooking recipes
- An ink well near a Palimpsest settlement enables their cuisine (ink-stained foods as cultural dishes)

### Faction Economy Integration

Each faction's economic signature (from Inkbound lore) maps to alchemical preferences:

| Faction | Alchemy Interest | Why |
|---------|-----------------|-----|
| Inkbound | Ink Synthesis, Messenger Ink, Liquefy Memory | Ink is their lifeblood; memory-liquids are sacred |
| Slimes | Any slime/gel transmutation | They're an "alchemical ecosystem" -- they *are* living alchemy |
| Saccharine Concord | Honey wells, wine transmutation | Trade goods; luxury liquids have high economic value |
| Rot Choir | Primordial Reduction, Living Alchemy | They value transformation and entropy, not preservation |
| Pale Curation | Distill Essence, property alteration | Precise, controlled, selective -- matches their curatorial identity |
| Demons | Kindle, Electrify, acid work | Destructive transformations; brimstone is their trade good |

### Combat Integration

Alchemy is not a combat school, but alchemical preparation shapes combat:

- Oil Coat the floor before a fight, then ignite it
- Thicken a water pool so enemies get stuck wading
- Electrify a pool an enemy is standing in
- Salt Ward a chokepoint against undead
- Guardian Gel to protect your flank

This is the Frieren "boring spell that saved the world" principle -- the mage who spent 10 turns pre-fight alchemizing the terrain wins more decisively than the one who spent 10 turns casting fireballs.

---

## Alchemy Learning Paths

### Path 1: Experimentation (Self-Taught)

Find liquids in the world. Mix them. Observe results. Spend bits to formalize a spell.

The caves are full of liquid pools (Qud-style zone generation already places them). Each pool is a potential lesson. A player who finds a slime pool, an acid pool, and a water source in the same zone has three "textbooks" available.

### Path 2: Archive Stones (Inkbound Knowledge)

Restored archive stones can teach alchemy spells directly (bypassing the material LearnCost). This ties alchemy to the repair system: fixing infrastructure gives you the knowledge to transform infrastructure.

### Path 3: Faction Mentorship

NPCs teach faction-aligned alchemy:
- Palimpsest Echo teaches Ink Synthesis and Purge the Taint
- Rot Choir Tendril teaches Primordial Reduction (and refuses to teach purification spells)
- Saccharine Envoy teaches Sweeten the Source (for a price)
- Villager Tinker teaches basic Chill/Kindle (practical, unglamorous)

### Path 4: Slime Communion (Unique)

Slimes *are* alchemy. A Slime Elder who trusts you (Liked reputation) can teach Living Alchemy spells that no other faction knows. But their teaching method is experiential -- they pour themselves over you, and you learn by *being* the liquid for a moment.

This is deeply weird, faction-specific, and memorable. It's also the only way to learn certain high-tier living alchemy spells.

---

## Well Alchemy as Endgame Infrastructure

The ultimate expression of this system: a player who has mastered alchemy can reshape the cave's liquid infrastructure.

**Scenario**: A dried-out settlement ruin at Z=18. Three dried wells. Broken aqueduct connections.

1. Repair all three wells (tinkering/repair spells)
2. Transmute Well A to produce honey-water (food source)
3. Transmute Well B to produce ink (Inkbound trade good)
4. Leave Well C as fresh water (essential)
5. Repair the aqueduct connections (multi-step quest)
6. Cast Seeking Water on a deep pool to route water to Well C permanently
7. The settlement becomes inhabitable. NPCs migrate there. New trade routes open.

This is not a cutscene. It's not a quest reward. It's the player using alchemy + repair to **build something** in the world. The Inkbound lore frame gives it meaning: you've restored a piece of civilization. The Frieren frame gives it texture: you did it with a bunch of small, specific, "boring" spells, not a single grand gesture.

---

## What Makes This "Alchemy" Different from Other Games

| Typical Game Alchemy | Caves of Ooo Alchemy |
|---------------------|---------------------|
| Combine ingredients at a workbench | Cast spells on liquids in the world |
| Produces consumable potions | Transforms persistent world state |
| Separate crafting UI | Same casting interface as combat spells |
| No spatial component | Deeply spatial -- pools, wells, terrain |
| Static recipes | Property-based -- alter viscosity, temperature, conductivity |
| Disposable output | Infrastructure output -- transmuted wells last |
| No faction meaning | Faction-aligned alchemy preferences |
| Power = better potions | Power = how long and how pure the transmutation holds |
| Learning = find recipe scroll | Learning = work with the liquid firsthand, or be taught by someone who has |

The key differentiator: **alchemy acts on the world, not on your inventory**. You don't make potions (the tonic system already handles consumables). You change what the world is made of.

---

## Suggested Implementation Order

| Priority | Task | Depends On |
|----------|------|------------|
| 1 | `LiquidSourcePart` on wells | Well repair system |
| 2 | `TransmuteLiquid` spell template | Spell cost model (materials-first) |
| 3 | Well retransmutation (temporary, mana-only) | LiquidSourcePart, TransmuteLiquid |
| 4 | Permanent well transmutation (amplified) | Amplification system |
| 5 | Property alteration spells (Thicken, Chill, Kindle) | Liquid property system |
| 6 | Infusion spells (Oil Coat, Salt Ward) | Terrain interaction system |
| 7 | Faction alchemy teaching | Conversation system, spell learning |
| 8 | Living Alchemy spells | Autonomous liquid behavior (advanced) |
| 9 | Alchemy + cooking integration | Cooking system |
| 10 | Slime Communion learning path | Slime faction reputation |

---

## Open Questions

1. **Should transmuted wells attract creatures?** A honey well might draw slimes or insects. An ink well might attract Inkbound NPCs. This would make well alchemy a faction-manipulation tool -- powerful but risky.

2. **Can enemies use alchemy?** A Rot Choir Tendril who corrupts a village's well into producing ooze would be a compelling quest hook. Reversing enemy alchemy with your own spells adds strategic depth.

3. **Should mixed-liquid wells be possible?** A well producing 60% water / 40% honey is mechanically interesting but might complicate the UI. Start with pure transmutations, add mixing later.

4. **How does alchemy interact with depth tier scaling?** Deeper zones have more exotic liquids (brain brine, neutron flux, primordial soup). Alchemy spells learned from deep-zone liquids should feel qualitatively different from surface-level water/honey work.

5. **Should there be alchemical "accidents"?** Failed transmutations that produce unexpected results. A botched Water to Wine might produce vinegar (new liquid type?). This connects to the Frieren "spell drift" concept -- alchemy gone slightly wrong over time.
