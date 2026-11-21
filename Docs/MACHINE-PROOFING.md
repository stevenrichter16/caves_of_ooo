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

## 3a. Where the reagents come from [V: blueprints and TradeStockBuilder]

| Element | Reagents (levels) | Trade stock | Grown or harvested | Carried by the caster who deals it |
|---|---|---|---|---|
| heat | fire moss (2), ember fruit (1), pitchpod resin (1) | fire moss, ember fruit | pitchpod crop | Marlback Cindercaller carries fire moss; a Glowmaw's corpse gives ember fruit |
| cold | glacier salt (3), frost lichen (2), chillcress tip (1) | frost lichen, glacier salt | frost lichen patch, chillcress crop | none carries one |
| shock | spark root (3), glimmer brine (2), prismreed pith (1) | spark root, glimmer brine | prismreed crop | Marlback Stormbinder carries spark root |
| acid | glimmer brine (2), sourmantle fold (1) | glimmer brine | sourmantle crop | Marlback Soursprayer carries glimmer brine |

The traders' stock is the renewable source (the alpha economy pass made brew reagents a trade good so they do not run out), the crops are the home source, and three of the four casters drop the reagent that turns their own element: beating one hands you the next fight's proofing. Cold has no caster who carries its reagent, which fits that cold harm is the rarest of the four.

## 4. What the player reads

- **Reagent** (examine, press rows): "press into a flask (cold-proofing, 2 coats; 0→2/12)"; the hint says one reagent is spent, what a coat does, what status it blunts and that no fire or bench is needed. A flask with too little room is a disabled row with the numbers; no suitable flask says what one needs.
- **Flask** (examine): "It burns readily..." style trait sentence (3p), then "Coat a garment from it: one unit gives +35 cold resistance for 100 worn turns, and blunts freezing. A trough proofs one for good with 4 units and 2 fire clay." Rows: "coat <garment>" for every garment held (worn ones say "(worn)"), with "(adds 100 turns to 40)" or "(replaces 60 turns of heat coat)"; refusals ("two treatments", "as much cold coat as it can") are disabled rows.
- **Garment** (examine): the coat reads "Coated against cold: +35 cold resistance while worn, 63 more turns of wear (of 100). It also blunts freezing. Cold still does at least 1 damage per hit until your resistance reaches 100."; the lasting one reads "Proofed against cold for good: +25...". The status panel names the proofing and what is left.
- **Trough** (examine, rows): what it does, what you carry (units per element, fire clay, garments that could be proofed); rows "proof <garment> against <element> for good (4 <element>-proofing + 2 fire clay)", disabled with the numbers when short; one disabled row says what to do when you carry no proofing.
- **Refusals** never spend anything: a short purse of units or clay, a garment that is already proofed, a full pair of slots, an armor-less thing.

## 5. Rules in one place

| | Coat (bottle) | Lasting (trough) |
|---|---|---|
| Needs | a flask of the element's proofing | 4 units of it and 2 fire clay, at a standing trough |
| Strength | +35 | +25 |
| Lasts | 100 worn turns a unit, up to 300; paused off the body | for good |
| Per garment | one coat (same element adds turns, another replaces) | one lasting proofing |
| Slots | one of the garment's two | one of the garment's two |
| Armor | any armor, worn or not, never a creature | the same |

Resistance 100 or more is immunity to the status and the hit still does at least 1 damage until then. Coat + lasting + a +50 garment is 110: that stack is the point.

## 6. Hand-testing

Home menu, **Camp Machines (wild ground)**: it starts with a trough, five flasks (two of mire), a leather armor, an apron, a Coldward cloak, fire clay and a reagent for each element. The scenario's own text walks the cold demo: press three frost lichens into a flask, proof the leather armor for good at the trough, wear it with the cloak (75 cold), coat the cloak (110), then try to freeze yourself. A test runs exactly that flow against the shipped loadout.

## 7. Honest limits and what is not done

- Not played in the Editor. Strengths and costs (35, 100 turns, 300 cap, 25, 4 units, 2 clay) are proposals.
- The reagent level is the only thing that scales: a better reagent makes more coats, never a stronger one. Reagent properties other than the element (volatile, combustible, toxic, sweet) have no effect on proofing yet, so every reagent of an element is the same except in coats per reagent.
- Status dampening applies to Burning, Frozen, Electrified and Acidic. It does not touch poison, bleeding, the Beating's glare (the awning's job) or steam contact (which already checks heat immunity itself).
- The explicit rollback of a replaced coat is also covered by the world receipt in these tests, so the undo closure is not separately pinned.
- A coat on a garment does not change what a merchant pays for it. A flask's value is flat, so bottled proofing creates no money.
- The old steeping (resin and clay) and clay-wash (2 clay) rows at the trough are gone; garments steeped before keep working and read as coats ("fire" reads as heat).
