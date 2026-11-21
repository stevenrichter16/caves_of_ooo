# Proofing: temporary coats from a bottle, permanent work at the trough

Provenance: user direction, 2026-11-20 ("go with property-driven reagents and the status dampening ... temporary resistances driven by coating clothing or armor in a reagent in a bottle you can carry, and permanent resistances driven by modifying the clothing or armor with a machine. go for as long as you can"). It generalises the steeping trough and clay-wash ([MACHINE-INVENTED-USES.md](MACHINE-INVENTED-USES.md) 3h and 3l). Evidence tags: **[V]** read in code or data this session.

## 1. What the game had [V]

- Four elemental resistance stats: Acid, Heat, Cold, Electric (`CombatSystem.ApplyResistances`). Light, Disintegration and Bludgeoning have damage flags and no stat; poison and bleeding have no resistance.
- A resistance scaled only the damage numbers: `(100 - r)` percent, at least 1 until 100, a vulnerability amplifies. **Burning, Electrified, Frozen and Acidic never read it.** Frozen does no damage at all, so cold resistance had almost nothing to resist.
- One +50 garment per element (Acidworker apron, Coldward cloak, Kilnfelt apron, Groundwire screen, 35-45 drams, one find each) and a +20 meal for three of them (100 turns).
- A property-driven reagent system: 24 reagents carry properties with levels (`heat:2`, `cold:3`, `conductive:3`, `corrosive:1`...), several of them 3-dram stock items (`BrewProperties`, `ReagentPart`).
- The trough as two hard-coded recipes (acid: 1 resin + 1 clay; heat: 2 clay), each +35 for 100 worn turns on a leather or cloth garment you were not wearing.

## 2. The design

### 2.1 Status dampening (one rule)

`ElementalStatusResistance`, applied once where an effect is put on a target (`StatusEffectsPart.ApplyEffectInternal`), before it stacks or runs:

| Status | Resistance leaves (100 - r) percent of | Immune at 100 |
|---|---|---|
| Frozen | `Cold` (floored at `FREEZE_FLOOR` 0.10) | yes |
| Burning | duration, on a thing with no fuel (3 turns per intensity) | yes |
| Electrified | duration and the contact stun | yes |
| Acidic | `Corrosion` | yes |

A vulnerability changes nothing here (the damage side already amplifies). Only the matching stat counts. A forced application bypasses it, like every other check. The damage tick is still resisted by the damage rule, so the two compound: that is what makes proofing worth the trouble.

### 2.2 Element comes from the reagent

`Content/Data/Alchemy/Proofing.json`: heat (property `heat`), cold (`cold`), electric (`conductive`), acid (`corrosive`). A reagent that carries one of these properties proofs a garment against that element; its level for the property is how many coats it makes. `GlimmerBrine` carries two and offers both. A new reagent with one of the properties needs no new recipe; a new element is one row, one liquid and one resistance stat.

### 2.3 Temporary: a coat from a bottle (no machine)

- **Press** (reagent verb): one reagent into a flask that is empty or already holds that proofing, with room for its coats. The flask then holds `heat-proofing`, `cold-proofing`, `shock-proofing` or `acid-proofing`: one unit is one coat.
- **Coat** (flask verb): one unit onto a carried garment, worn or not: +35 resistance for 100 worn turns (`EnhancementSteeped`, now any armor). The same element tops the turns up to 300; another element replaces. Wear pauses when it is taken off.

### 2.4 Permanent: work at the trough

The trough stops doing temporary treatments. It gives a garment one lasting proofing (`EnhancementProofed`): +25 resistance, for 4 units of that element's proofing and 2 fire clay. Smaller than a coat on purpose and below a purchased +50 garment; it stacks with a coat, a meal and gear toward immunity (100).

## 3. Worth it, and used

- **Worth it:** it beats the +20 meal; it works on armor you already like (no DV or speed penalty of the special garments); the status dampening makes cold and shock worth proofing at all; immunity is reachable by stacking.
- **Accessible:** the bottle is made anywhere in one action; a coat is one action; wear pauses off the body, so you prepare at the edge of the place and put it on when needed; each garment carries its own element, so a body piece, a cloak and a screen can be proofed against different things.

(Sections for readers, scenario and limits are added as each part lands.)
