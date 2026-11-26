**Causeway field use.** The linked steam vent's Examine text should connect its
visible crust to the use of scraped vent scale. Reserve the same catch for
water when that matters more, or use fire-moss for the same garment benefit.
The clue belongs to the source and remains useful when that day's supply is
spent. A flask of proofing should offer garment coats immediately after
Examine, worn gear first, with ordinary pouring still available below.

**November21 menu-access qualification:** [bounded receipt](Verification/MachineSystems/ProofingMenuAccess20261121/README.md)
removes coat12/trough12/unpick6 caps. Native107/107 plus revised unpick1/1; actual
late coat/plate selection, finite payments, worn35cold resistance/paused old coat,
and fresh100/120/150 verified. No new acquisition/hazard/all-machine claim.

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

A vulnerability changes nothing here (the damage side already amplifies). Only the matching stat counts. A forced application bypasses it, like every other check. An immune body says so ("You shrug off the freezing.", "The ice wight shrugs off the burning.": freezing, burning, shock, corrosion) so a proofed player can tell the status was refused and not missed. The damage tick is still resisted by the damage rule, so the two compound: that is what makes proofing worth the trouble.

### 2.2 Element comes from the reagent

`Content/Data/Alchemy/Proofing.json`: heat (property `heat`), cold (`cold`), electric (`conductive`), acid (`corrosive`). A reagent that carries one of these properties proofs a garment against that element; its level for the property is how many coats it makes. `GlimmerBrine` carries two and offers both. A new reagent with one of the properties needs no new recipe; a new element is one row, one liquid and one resistance stat.

### 2.3 Temporary: a coat from a bottle (no machine)

- **Press** (reagent verb): one reagent into a flask that is empty or already holds that proofing, with room for its coats. The flask then holds `heat-proofing`, `cold-proofing`, `shock-proofing` or `acid-proofing`: one unit is one coat.
- **Coat** (flask verb): one unit onto a carried garment, worn or not: +35 resistance for 100 worn turns (`EnhancementSteeped`, now any armor). The same element tops the turns up to 300; another element replaces. Wear pauses when it is taken off.

### 2.4 Permanent: work at the trough

The trough stops doing temporary treatments. It gives a garment one lasting proofing (`EnhancementProofed`): +25 resistance, for 4 units of that element's proofing and 2 fire clay. Smaller than a coat on purpose and below a purchased +50 garment; it stacks with a coat, a meal and gear toward immunity (100).

## 3. Worth it, and used

- **Worth it:** it beats the +20 meal; it works on armor you already like (no DV or speed penalty of the special garments); the status dampening makes cold and shock worth proofing at all; immunity is reachable by stacking.
- **The measured worth** (`ProofingWorthTests`, the game's own rules; one hit of the kind named, resistance of the stat that answers it):

  | resistance | acid: HP lost to one Soursprayer-class corrosion | cold: turns frozen by a 0.6 freeze | heat: turns a 1.5-intensity burn lasts | shock: turns stunned |
  |---|---|---|---|---|
  | +0 | 35 | 6 | 5 | 2 |
  | +25 (lasting) | 15 | 5 | 4 | 2 |
  | +35 (a coat) | 11 | 4 | 3 | 1 |
  | +60 (coat + lasting) | 7 | 3 | 2 | 1 |
  | +85 (+ a +25 meal or part of a +50 garment) | 3 | 1 | 1 | 1 |
  | +100 | 0 | 0 | 0 | 0 |

  A coat cuts the corrosion of a spray by about two thirds, where the damage rule alone gave a little under a half (the dossier's 37 to about 18); a freeze that lost you six turns loses you four. The burn's damage per turn is resisted again on top of the shorter burn, which these columns do not show. Short stuns barely move until immunity, so shock is the weakest of the four to proof against on its own.
- **Accessible:** the bottle is made anywhere in one action; a coat is one action; wear pauses off the body, so you prepare at the edge of the place and put it on when needed; each garment carries its own element, so a body piece, a cloak and a screen can be proofed against different things.

## 3a. Where the reagents come from [V: blueprints and TradeStockBuilder]

| Element | Reagents (levels) | Trade stock | Grown or harvested | Carried by the caster who deals it |
|---|---|---|---|---|
| heat | fire moss (2), ember fruit (1), pitchpod resin (1) | fire moss, ember fruit | pitchpod crop | Marlback Cindercaller carries fire moss; a Glowmaw's corpse gives ember fruit |
| cold | glacier salt (3), frost lichen (2), chillcress tip (1) | frost lichen, glacier salt | frost lichen patch, chillcress crop | none carries one |
| shock | spark root (3), glimmer brine (2), prismreed pith (1) | spark root, glimmer brine | prismreed crop | Marlback Stormbinder carries spark root |
| acid | glimmer brine (2), sourmantle fold (1) | glimmer brine | sourmantle crop | Marlback Soursprayer carries glimmer brine |

The bottle itself is a plain `LiquidFlask`: a Provisioner's and a Well Keeper's stock each hold one, the Morrowfast merchant sells them and two loot tables carry one, and a flask holds 12 units (a full flask is twelve coats). The Tinker stocks the trough (20%), not flasks (changing a stock table moves every later draw of that zone's generation, so it is left alone).

The traders' stock is the renewable source (the alpha economy pass made brew reagents a trade good so they do not run out), the crops are the home source, and three of the four casters drop the reagent that turns their own element: beating one hands you the next fight's proofing. Cold has no caster who carries its reagent, which fits that cold harm is the rarest of the four.

## 4. What the player reads

- **Reagent** (examine, press rows): "press into a flask (cold-proofing, 2 coats; 0→2/12)"; the hint says one reagent is spent, what a coat does, what status it blunts and that no fire or bench is needed. A flask with too little room is a disabled row with the numbers; no suitable flask says what one needs.
- **Flask** (examine): "It burns readily..." style trait sentence (3p), then "Coat a garment from it: one unit gives +35 cold resistance for 100 worn turns, and blunts freezing. A trough proofs one for good with 4 units and 2 fire clay." Rows: "coat <garment>" for every garment held (worn ones say "(worn)"), with "(adds 100 turns to 40)" or "(replaces 60 turns of heat coat)"; refusals ("two treatments", "as much cold coat as it can") are disabled rows.
- **Garment** (examine): the coat reads "Coated against cold: +35 cold resistance while worn, 63 more turns of wear (of 100). It also blunts freezing. Cold still does at least 1 damage per hit until your resistance reaches 100."; the lasting one reads "Proofed against cold for good: +25...". The status panel names the proofing and what is left.
- **Trough** (examine, rows): what it does, what you carry (units per element, fire clay, garments that could be proofed); rows "proof <garment> against <element> for good (4 <element>-proofing + 2 fire clay)", disabled with the numbers when short; one disabled row says what to do when you carry no proofing.
- **Many garments:** coat, trough and unpick menus list every eligible held garment/element choice through their existing scrolling menus, including real refusals. No stashing is required to reach later choices. Worn-first and existing candidate sorting remain. See [November21 correction](PROOFING-MENU-ACCESS.md).
- **What you have seen:** "You have seen heat (steam vent), acid (Marlback soursprayer) in this place." in the trough reader and "You have seen heat here (steam vent)." in press and coat hints: creatures only while in view and with an elemental skill (Pyromancy, Cryomancy, Galvanism, Corrosion), terrain once seen and declaring a steady heat, cold or charge, pools only while in view (their volume changes) and by the damage type they do.
- **What it would do for you:** the coat and proof rows end their hint with your resistance before and after ("Your cold resistance: 50 → 85.", "once worn" for a garment you are not wearing, "(immune to freezing)" at 100 or more; the readout stops at 100 even though gear and proofing can sum higher), and say nothing of the kind for a top-up of a coat you already count.
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

### The workshop

Home menu, **Proofing Workshop (everything, 10 armor sets, hazards, elemental foes)**. A wild start with a test character (Strength 70, 400 health) carrying: the trough, hearth, still, bench and box; eight of each of the ten reagents; twelve flasks (four of bog-mire); 24 fire clay; 30 timber, a striker and torches; seeds for the four reagent crops; six frost and six lightning tonics; the arc bolt, ice lance, ember vein and acid spray books; and **ten full sets of armor** (body, head, hands, feet, back: leather, warded leather, chain, chain with a buckler, plate, plate with an iron buckler, a kilnfelt set, an acidworker set with the groundwire screen...). Pieces repeat across sets, so the pack holds stacks, and a coat or a lasting proofing goes on one unit of a stack (it is split off, treated and kept as its own single; an interruption puts it back).

Beside you stands a **proving post**: press C by it to call an enemy of each element (heat: Marlback cindercaller, charred husk; cold: ice wight, rune cultist; shock: Marlback stormbinder, vault sentinel; acid: Marlback soursprayer, cave slime; at most six at once), send away everything it called, or mend yourself (full health, harmful statuses cleared, coats and proofings kept). Around you are four hazard ranges at least twelve cells apart: heat to the north-east (steam vents, ash beds, fire traps), cold to the north-west (frost vents, ice, frost runes), acid to the south-east (acid pools, bog mire) and shock to the south-west (brine, with a lightning tonic or an arc bolt to charge it).

The post and the range only place objects and call creatures through the ordinary spawn rules; the post is stateless (it finds what it called by a mark on the creature) and nothing else in the game places one.

## 7. Honest limits and what is not done

- Bounded Editor coat/trough/equipment/reload checks are now recorded in the November21 receipt above, with [Posy coat+lacquer integration](Verification/MachineSystems/PosyBenchIntegration20261121/README.md). This does not qualify all hazards/elements or balance; strengths and costs (35, 100 turns, 300 cap, 25, 4 units, 2 clay) remain provisional.
- The reagent level is the only thing that scales: a better reagent makes more coats, never a stronger one. Reagent properties other than the element (volatile, combustible, toxic, sweet) have no effect on proofing yet, so every reagent of an element is the same except in coats per reagent.
- Status dampening applies to Burning, Frozen, Electrified and Acidic. It does not touch poison, bleeding, the Beating's glare (the awning's job) or steam contact (which already checks heat immunity itself).
- The explicit rollback of a replaced coat is also covered by the world receipt in these tests, so the undo closure is not separately pinned.
- A coat on a garment does not change what a merchant pays for it. A flask's value is flat, so bottled proofing creates no money.
- A garment stack is treated one unit at a time; a stack that refuses to split (a protected input) is not offered.
- The old steeping (resin and clay) and clay-wash (2 clay) rows at the trough are gone; garments steeped before keep working and read as coats ("fire" reads as heat).

## 8. Reassessment, and the settling tray (2026-11-20)

**What the first build left.** Read against the brief ("worth it" and "accessible enough that someone uses it when they know they are about to meet an element"), the system holds up, with these holes:

- **Reagents are all bought, grown or looted.** Every unit of proofing comes from a trader, a crop or a caster's pack. A player who knows the hazard ahead (a vent field, a brine flat, a bog of acid) has nothing to do *there*: the place itself gives nothing. That is the reverse of the design intent (the place that threatens you should also be where you prepare).
- **Shock is the weakest** to proof against alone (a two-turn stun), as the worth table shows. Not changed: it is a fact about the status, and the coat still helps against the damage.
- **Cold has no caster who carries its reagent**, and (found while building the tray) **no source on the surface at all**: the frost vent and the ice sheet are in the hazard builder's underground table only, and camp kits cannot be set down underground in v1 (the column audit, PB7, is pending). So cold stays supplied by traders (frost lichen, glacier salt) and the chillcress crop until PB7, or until something puts a cold source on the surface.
- **Reagent properties other than the element still do nothing** (volatile, toxic, sweet). Not changed; a reagent of an element is the same as another except in coats per unit.
- **One lasting proofing per garment, with no undo** (the trough's rows only added). A wrong choice filled a slot for good. Fixed in this pass, on the trough rather than in a new machine: it now unpicks a lasting proofing (below).
- **Resistances show only on the inventory status panel.** Not changed; the trough reader's "what you have seen" line already names the elements you have met in a place.

**The invention: the settling tray.** One kit for all four elements (the brief's "not a machine per resistance"). It is a shallow clay pan on short legs. Set down within **two cells** of a source, it settles one **level-one reagent** of that source's element every **half day**, at most **four** at a time, and holds them until you gather them. A source is whatever the world declares, not a name (`ProofingSources`, the same idea as `NaturalHeat`):

| Element | Catches | A source is | Where the hazard builder places it |
|---|---|---|---|
| heat | vent scale (heat 1) | a terrain declaring a steady heat (nothing chilling it, no fuel to burn): steam vent, ash bed | Spread, Sodden, Beating, Stump surface; many others |
| cold | rime flake (cold 1) | a terrain declaring a steady cold: frost vent, ice sheet | **underground only** (see above) |
| shock | brine crust (conductive 1) | a pool of brine, with water in it | Spread, Sodden, Beating, Stump surface |
| acid | acid crust (corrosive 1) | a pool of acid or bog-mire, with liquid in it | Sodden (acid pools and the mire of its districts) |

The element table (`Proofing.json`) carries the `Collects`, `SourceTile` and `SourceLiquids` per element, so a new element is still one row, one liquid, one stat and one reagent.

**Rules that keep it honest.**

- **Finite and shared.** A source's stock (`SourceStockPart`) is full at first, 3 to 5 by a stable hash of its zone, blueprint and cell, and one catch back per day, never above capacity. **A source is a whole blob, not a cell:** hazards grow one entity a cell, so the stock is kept on the lowest (y, x) cell of the eight-way connected stretch of cells that are sources of the same element (`ProofingSources.StockHolder`, as a pond's stock is kept on its anchor). Trays at different edges of one pool, or one tray moved along its shore, draw on the same supply, and drawing a cell dry does not move it; a different element beside it, or a pool across the map, is a different source. So more trays buy no more reagent. A spent source gives a tray nothing until the next day. What a reader promises is exactly what a gather gives, even for a source nobody has drawn on (its capacity is computed, not assumed). Reading a tray changes nothing, makes no stock and creates no entity (names come from the blueprint); time is reconciled when the tray is gathered from, taken up or moved.
- **No money, no conjured source.** The four catches are `NoTrade` and worth nothing in drams, so a free reagent is not a way to mint money (a plain reagent such as fire moss still trades). **That holds all the way down:** a tonic brewed with a `NoTrade` reagent is itself `NoTrade` (found by review: a free cold or shock reagent alone brews a 12-dram tonic), and an item that cannot be traded never stacks with one that can, in either direction, so a free tonic cannot launder itself into a tradable stack. **A pool you poured is never a source** (the same exclusion fishing uses for poured water) and neither is a thin film, so a flask of brine, acid or mire cannot make a source, and with it a fresh stock, out of a carried liquid. A fire that burns fuel is never a source either, so the tray cannot turn your timber into scale.
- **One element at a time.** A pan holding one element's catch will not add another's until it is emptied (move a tray from a vent to a frost vent and it says so). Emptying it frees it for the next source it stands by (the clock toward the next catch keeps running).
- **Weak on purpose.** Level one is one coat a reagent, against two or three for the traded and grown reagents. A lasting proofing (4 units) from a source alone is four catches, two days of waiting, or one full pan.

**What the player reads.** The tray's reader names what it is settling and from what ("Settling vent scale from the steam vent. Holds 2 (it takes 4). The source looks plentiful. The next is due around Dusk."), never a count of the source's stock ("plentiful", "well drawn but not spent", "thin", "spent for now"). A full pan says so and promises nothing; a spent source says "a little more each day". The gather row ("gather the vent scale (2)") is disabled with a reason that names the rule when there is nothing to take (no source within two cells; nothing yet, due around...; a source spent for now). The camp note says "settling tray holds 2 by now" (read-only projection). **The set-down rows name each cell's catch** ("set down east (catches cold)", "set down west (nothing to catch here)") so choosing the cell is choosing the element (the placement rules refuse the cell beside a hazard, and for an ash bed or a frost vent, which warm or chill the cells they touch, the cell in line beyond it; a cell a little off the line works), and **"make camp"** sets a tray down only where a source is in reach (otherwise it stays in the pack).

**Where it comes from.** A Tinker stocks it at 20% like the trough, trap and awning; the workshop scenario carries two.

**Unpicking a lasting proofing.** The trough now has a row per proofed garment ("unpick the cold proofing from leather armor (worn)", worn ones first, all held proofed garments reachable). It removes only the lasting proofing (a coat on the same garment stays), takes the resistance off a wearer at once, frees the slot, and returns **nothing** (the units and the clay stay spent), so it can never mint; an interrupted unpick leaves the garment exactly as it was. The trough reader mentions it only when you hold a proofed garment.

**Tests.** `SettlingTrayTests`, run against the real set-down and gather verbs, the Full reader and a save round trip: element table and reagent properties, sources and what is not one (poured pools, creatures, fuelled fires, chilled or cooled vents, dry pools, unnamed liquids), nearest and tie-breaking, the shared and finite stock, every source blueprint's catch pressing into the right proofing, the rollback of an interrupted gather, a full pack, taking the tray up, the mixing rule, the reader and the rows, make camp, the workshop having a source for each element, one stock for a whole multi-cell pool (trays at three edges, a tray moved along the shore, the anchor cell drawn dry, two pools, two elements side by side), a reader that promises exactly what the gather gives (including a source of capacity three nobody has drawn on), a brewed tonic and a stack of free items staying unsellable, and a look that creates no entity. `ProofingTroughTests` adds the unpick cases (worn and carried, coat kept, interruption, stale or foreign commands, crowded-list access and exact late-item removal). Mutation and review results are in MACHINE-INVENTED-USES.md section 3r.

**Honest limits.** Settling-tray catching has not been played in the Editor. Cold cannot be caught on the surface until a surface cold source exists or underground set-down is allowed (PB7). Acid is caught in the Sodden only. Timings (half a day, four held, 3 to 5 stock, one a day) are proposals. Wording is placeholder pending voice review.

