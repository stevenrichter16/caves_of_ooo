# Current item utility audit — 8 October 2026

**Later implementation:** [Combat inventory improvisation](COMBAT-INVENTORY-IMPROVISATION.md) adds ground uses for FrogOil/SilverSand and sight-obscuring Veilpuff with bounded enemy pursuit. The entries below preserve the audit snapshot at `c7e154db`; they are not a second audit of those changes.

Status: source audit complete at main `c7e154dbcdafcbef882ce3dac5a7fe9cec21773a` (remote main fetched and matched). This is an analysis of current implementation, not a new content pass. No gameplay or content definitions were changed.

The result is **293 obtainable portable item families**, plus a separate review of eight movable world objects and six collectible liquid types. The [complete item-by-item catalog](ITEM-UTILITY-CATALOG-2026-10-08.md) records each source, current use, restriction and supporting code. A [structured inventory](Verification/ItemUtility2026-10-08/catalog.json) preserves the same entries for follow-up work.

## Scope and standard of proof

An item counts as obtainable only when its current definition has a route through live world generation, merchant stock, creature equipment/death, harvesting, a reachable conversation/service, or production from obtainable inputs. Craft outputs are identified as such; merely having a blueprint, test fixture, development grant, merchant table with no merchant, or uncalled factory method is insufficient. Regional additions refer to fresh current-version worlds; existing saved graphs retain their old contents. Optional placements can fail their geometry/admission checks and random stock is not a guarantee for every seed or visit.

The census resolves all 709 object definitions using the loader's parent/child parameter merge and identifies 298 takeable candidates before excluding abstract definitions and inaccessible items. It also checks runtime-created quest goods, corpse payloads, recoverable lamps, movable furniture and harvestable liquids, which a blueprint-only inventory would miss. Variants of a forged weapon, mixed brew, copied book, modified item or severed limb are grouped by their actual generating system, not counted as an infinite number of different items.

The portable census reconciles as follows: 298 resolved takeable candidates minus eight inheritance templates and four unavailable concrete items gives 286 obtainable static blueprints. Seven runtime-created or recovered item families bring the total to 293. The unavailable concrete items are EchoKnife, InertSludge, CharredTonic and LanternOil. These are documented as exclusions, not counted as usable content. Three obtainable schematics with a blocked Study action remain in the obtainable count because finding and trading them actually works.

For each item, the catalog gives a source, actual use, restrictions and source references. A sale is a legitimate economic use, reading can be a legitimate information use, and an ordinary weapon can be useful without a unique power. Those uses are identified explicitly rather than treated as proof of multiple interacting systems. Names, examine text and design documents are evidence of intended communication, not proof of functionality.

This audit reads current source and content and performs a bounded exhaustive reagent-profile calculation. It does **not** claim a fresh Unity playthrough of every item, a measured spawn probability for every item, or a subjective judgment proven by tests. Earlier implementation test receipts are historical evidence, not new verification performed for this audit.

## Overall assessment

The game already has a substantial working item economy. Its strongest examples are items that compete between several purposes: cord for repairs, bandages, bindings and a rope route; fire clay for local works and structures; ink for rental currency, charged magic and copying; meat for food, preparation and a specific predator diversion; equipment components and minerals for build choices.

The depth is uneven. Many objects have exactly one good use. Several differently named crops resolve to the same small set of payloads. Some evocative weapons are ordinary stat variants. A few items advertise functionality that the normal player cannot access. NPC use is narrower than player use: equipment, configured medicine, food/salvage exchange and specific local requests are implemented, but a general society that autonomously crafts, repairs and repurposes all these items is not.

## What “generic usefulness” really means here

- **Trade:** an item with `Commerce.Value > 0` can normally be sold to a willing trader with enough drams and capacity; `NoTrade`, rental and event restrictions still apply. Drams are an actor property. Ordinary ground pickup converts each GoldCoin into 5 drams. Taking coins directly from a container currently leaves physical coins in inventory; dropping and picking them up performs the same 5-dram conversion. Selling those carried coins instead uses their lower Commerce value. Water is not the trade currency. See [TradeSystem](../Assets/Scripts/Gameplay/Economy/TradeSystem.cs#L24), [trade restrictions](../Assets/Scripts/Gameplay/Economy/TradeSystem.cs#L364).
- **Throwing:** pickup eligibility does not imply throwable. It requires a `Handling` definition with `Throwable=true`, accessible ownership, sufficient Strength and range. Do not count “throw it at something” for every loose object. See [HandlingService](../Assets/Scripts/Gameplay/Items/HandlingService.cs#L34), [ThrowItemCommand](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L73).
- **Equipment:** actual equipment parts, weapon-family tags, body slots and implemented effects decide usefulness. Material names do not grant imaginary resistances. A shield occupying a hand has a real cost; a sword named for time does not thereby manipulate time.
- **Crafting and repairs:** specific accepted inputs and authored composition/repair recipes matter. `TinkersForge` supports forging; a movable `SmithAnvil` has no `ForgePart`. A world prop called `StoneCoffer` has no container part. All materials are not interchangeable simply because their descriptions sound suitable.
- **Environment:** light, water transfer, liquid pools, temperature, coatings and movable solidity are real where the specific parts and action consumers exist. A code hook that no obtainable item can reach is recorded as a gap.
- **NPCs:** selling an item to someone does not prove that their AI knows how to use it. The catalog distinguishes explicit equipment/medicine/civilian services from hypothetical NPC capability.

## Source checks that changed the initial reading

| Initial risk | Current source finding | Consequence |
|---|---|---|
| The new Morrowfast village might have replaced all old shops and repair contracts | Morrowfast is `3.6`; Sill remains `10.10`. Sill still takes the five-shop starting-town route. Other villages still run `VillagePopulationBuilder`. | Themed shop stocks, the witness-book quest and settlement repair manuals remain current. |
| A weapon appearing in `MarcelineStock` must be buyable | No current production placement of Marceline was found. | A sole dependency on that table is not accepted as a live source. |
| Compost is reachable because `InertSludge` has a use and brewing has a sludge branch | All 1,471 distinct nonempty property unions from current authored reagents yield 1,468 brews and three mishaps, zero sludge. No separate live sludge source was found. | Compost is implemented but currently lacks its input. |
| Every liquid registry entry is a bottled substance | Only six have working physical collection sources. Brine and bog mire have pools, but their own unlike water TileStateSource fails the purity check. Eighteen other definitions have no physical collection source. | Liquid contact/coating behavior is distinguished from a portable player resource. |
| Corpse utility can be read directly from the corpse blueprint | Death attaches harvesting payloads copied from the creature. | Butchery must be traced from the victim, not inferred from the generic corpse definition. |
| All grimoire copies teach a spell | Generic loot copies are blank; copies made by the scribe carry the selected original's teaching payload. | Two materially different uses share a blueprint. |
| Listed weapon status durations are their real duration | Effect factories sometimes use intensity and effect-specific decay instead of the duration encoded in the weapon JSON. | The catalog reports the actual effect path, with mismatches recorded. |

Source routing: [Sill definition](../Assets/Scripts/Gameplay/World/Map/WorldMapAuthoring.cs#L212), [village pipeline](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L939), [shop installation](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061), [village services](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1106), [current manifest](../Assets/Scripts/Gameplay/World/Generation/SpreadExplorationPlan.cs#L39). Underground ziggurats, Beating/Overwrit libraries/workshops/vaults, curator galleries and regional traders have live catalog routes; their optional placement is not a promise that every world visit contains one.

## Strong working examples of item depth

| Item or family | Current choices beyond picking it up |
|---|---|
| Salvaged timber | Repair opted-in wooden structures/equipment; jam a supported trap permanently; shape a weapon haft; feed cooking coals. These compete for the same supply. |
| Knotflax cord | Repair a well line; make a weapon binding or bleeding bandage; make a Sodden dressing with its other input/service cost; rig the specific two-ended Ginmere shortcut. |
| Fire clay | Repair clay-lined wells and the connected kitchen pan; rebuild a settlement oven with its guide; quiet Morrowfast's bell after diagnosis. |
| Pale-salt / choir-iron / glow-quartz | Spend physical mineral at a forge for actual equipment enhancements; salt and iron also have local social/supply uses. Slots, compatibility and finite regional sources constrain the choice. |
| Ink vial | Convert to rental Ink; refill consuming-rite book charges; spend on a selected grimoire copy. These are distinct systems, not three names for one currency. |
| Raw / dried meat | Heal by eating; raw meat cooks into a better food; safe dropped meat can divert the specifically configured predator. Food also supports collector barter where that service exists. |
| Waterskin and crop-grown water vessels | Drink to relieve one Parched stack per charge, refill from valid sources, move water into eligible crops/other vessels, and douse compatible world fire. Charges and source purity matter. |
| Liquid flask / greybladder cup | Collect actual compatible pool liquid and pour it to create/replenish a physical pool; water-transfer/dousing actions also exist. They have no drinking verb. |
| Collected pool liquids | Oil exposes targets to more Heat damage; acid harms; convalessence heals; choir-mirror mucilage reflects part of incoming damage; memory bath supplies a temporary one-use death anchor. Pouring allows contact with creatures, including the player. Coatings dry; this is not permanent protection or a saved respawn point. |
| Forging heads, hafts and bindings | Combine different accuracy, penetration, damage, Strength-cap and class choices; reforge; salvage recorded components; use a suitable brew to temper a weapon, with item-HP cost and possible harmful payloads. |
| Original and copied grimoires | Original teaches an ability; charged originals also carry finite rite fuel. A valid copy teaches the selected original's payload; a blank generic copy instead has caretaker-donation uses. Loaned utility books can be studied before return. |
| Corpse and source species | Harvest the actual creature's configured material/meat; sell or bury ordinary corpses; specific predators/undertakers have their own corpse behavior. This is not a universal butchery table. |
| Specialized protection | Hood, apron, screen and cloak choose respiratory or elemental protection against slots, physical coverage, dodge, speed or weight. Their benefits are real but do not imply total environmental immunity. |

The full catalog gives exact item values, sources and restrictions. Short-blade skills currently key off **Piercing**, so a spear can support that tree; long-blade, axe and cudgel skills use their separate family attributes. Discovering that difference is a real build choice, although the category name alone does not communicate it.

## Gaps and misleading affordances

| Priority | Finding | What is actually available today |
|---|---|---|
| High | Three tinkering schematics circulate, but normal characters lack the required BitLocker. | They can be bought/found/sold; advertised Study refuses. Their intended upgrades are not counted as ordinary-player uses. Preserve the deliberate normal-play tinkering restriction when choosing a future correction. |
| High | Some keys appear in merchant stock despite being untradeable. | IronKey works when found; a stock entry does not mean purchase succeeds. |
| High | Well-keeper book donation promises instruction without changing the NPC or well. | The copy is consumed and dialogue proceeds. Separate oven/lantern caretaker improvements really change settlement state. |
| High | Cloak's listed AV is not used by ordinary humanoid location-based melee/thrown hits. | Back has zero target weight and only the struck slot contributes armor. Cloak's DV still works; displaying aggregate AV can overstate protection. |
| Medium | Resource/use connection is missing for sludge. | Compost action exists, but no authored reagent mixture or normal source supplies InertSludge. |
| Medium | Two actual wetland liquids refuse collection. | BrinePool and MirePool carry a water TileStateSource in addition to their different LiquidPool IDs. The flask purity gate rejects them; removing a visible coating does not remove that metadata. World-contact effects still work. |
| Medium | Generic grimoire copies are stocked with no teaching payload. | Reading says blank pages. These still work for supported caretaker donations; a scribe-produced copy is different. |
| Medium | Item descriptions and runtime effect rules disagree. | For example, SumpsievePad also cures poison-gas poisoning despite its text, while processing it into a field dressing loses that cure and adds bleeding treatment; elemental weapon durations follow effect constructors/decay rather than all declared JSON durations; DissolutionMaul corrosion 1.5 clamps to 1. |
| Medium | Several unusual weapon names imply a richer identity than their payload supplies. | TemporalShard, SeveranceEdge, Sporeblade, FirstRootGlaive and PalimpsestBlade remain legitimate weapons, but have no special time, severance, spore, root or memory mechanic of their own. |
| Medium | Frog oil is sold as an item with no oil behavior. | Current purpose is a commodity. It is not a refuel supply, liquid flask content, reagent, food or salve. LampOil and WardOil are different items with real specific consumers. |
| Medium | Some heavy props are functionally only movable blockers. | MillStone does not process grain; SmithAnvil is not a forge; StoneCoffer and HaulBarrel are not storage containers. Hauling itself works and selected layouts give it a route payoff. |
| Low | Some recoverable objects remain after their one-time objective is resolved. | The carved name-token and witness-book are not taken during the report dialogue. A severed limb has no reattachment item action. These become souvenirs/trophies, not reusable system inputs. |

These priorities are judgments about the player's expectation and the project's requested depth, not a claim that every single-purpose item is defective. Food that heals or a sword that trades damage against hands/weight can be sufficient. The more consequential gaps are advertised actions that fail, named properties that do not affect the result, and obtainable objects that have no consumer beyond selling.

## NPC and entity side of the inventory

Actual authored NPC equipment affects their fights and drops through the ordinary ownership path. Configured enemies use limited carried medicine and recover a disarmed weapon; the audit does not extend those behaviors to every species or every healing item. Selected friendly-service NPCs accept useful medicine or equipment for a free compatible slot. Predator diversion uses actual raw/dried meat; collector exchange consumes actual food for its currently carried salvage; magpies can hoard shiny goods. Local requests, burial, repair labor, contracts and endings consume specific items and update their supported state.

Not found: general autonomous potion throwing, brewing, weapon forging, crop-output processing, repair or inventory experimentation by all NPCs. Sharing entity/Part code with the player does not establish those AI behaviors. It is important to keep this distinction when describing emergent depth.

## Audit review and verification bounds

The three category audits were crosschecked against a fourth inheritance/source census, and major gaps received independent counter-readings. In particular, the sludge conclusion was checked with the complete property parser rather than relying solely on the simpler enumeration script; current authored profiles give the same result. Runtime-created items were reconciled separately. All referenced file paths and line numbers were checked for existence. Audit corrections include restoring the still-live Sill shops, distinguishing runtime corpse harvest from template parts, distinguishing physical oil items from pool liquid, and documenting the drop/pickup conversion route for container coins.

No production behavior changed and no new Unity tests or full-world sweep were run for this analysis. Source reachability establishes a path through current normal-play code; it does not establish that an optional source appears in every seed, that the player will notice it, or that all loops are balanced. There is no numerical or source-code parity claim with Caves of Qud; its relevance here is the user's criterion that modest finds should support informed play.

## Interpretation of the requested depth

The strongest item decisions arise where an ordinary resource has competing consumers: spend cord to cross, bind a weapon, treat bleeding or repair; spend ink on a spell reserve, rental or copying. These create reasons to remember where something grows, carry it through a dangerous trip, and return to a service. The collectible liquid system also provides actual target manipulation beyond monetary value.

Single-purpose items are not automatically shallow. A readable document, a key or a deliberately ordinary weapon can have a clear role. The weaker areas are advertised functions with no working connection, unusually named equipment with no distinct behavior beyond numbers, repeated crop payloads with different names, and props whose appearance implies a function absent from their parts. The NPC economy is also selective: a few authored behaviors use items intelligently, while most player combinations have no autonomous NPC equivalent.

On the user's standard, the game has real examples of useful minor finds, but it is not yet consistent about them. Closing the specific source/action/description mismatches above would improve trust in exploration before simply increasing item count. This is an assessment, not a change to the deliberately restricted tinkering policy.

## Audit work log and files

1. Resolved blueprint inheritance, then traced normal production callers rather than treating data-table membership as sufficient.
2. Divided the item census into equipment, consumables/cultivation/materials, and tools/books/story goods; reconciled all candidates and separately traced runtime families.
3. Read action consumers and command gates, including ownership, stations, charges, valid targets and NPC AI opt-in.
4. Counter-read the important exclusions and alleged no-ops: Sill/Morrowfast routing, corpse harvest attachment, mineral infusion without BitLocker, physical coins from containers, book donation state changes, heavy props and reagent parsing.
5. Repeated the reagent calculation with the complete current property-list parser and saved hashes/results. It produced 1,471 profiles: 1,468 brews, three mishaps, zero sludge. The command-layer mishap still consumes ingredients and attempts up to two nonlethal self-damage; the probe tests only outcome classification.
6. Validated inventory coverage, duplicate IDs, every cited path/line, JSON parsing and Markdown whitespace. These checks concern the audit, not production gameplay execution.

Self-review: 🔵 Initial assumptions about sludge, old village routing and coin conversion were corrected before publication. 🧪 Static source paths and a translated finite calculation do not replace native interaction tests or a distribution sweep; those are outside this analysis. ⚪ Gameplay issues identified here are recorded, not silently fixed in an analysis-only request.

Files created:

- [This assessment](ITEM-UTILITY-AUDIT-2026-10-08.md): scope, findings, corrections and judgment.
- [Complete catalog](ITEM-UTILITY-CATALOG-2026-10-08.md): every obtainable item and the separately marked exclusions, liquids and movable objects.
- [Structured catalog](Verification/ItemUtility2026-10-08/catalog.json): canonical per-item rows and counts.
- [Reagent probe](Verification/ItemUtility2026-10-08/reagent_reachability_probe.py) and [receipt](Verification/ItemUtility2026-10-08/reagent_reachability.json): bounded source calculation and hashed inputs.
- [Verification notes](Verification/ItemUtility2026-10-08/README.md) and [audit validation](Verification/ItemUtility2026-10-08/audit_validation.json): method, count/link checks and limitations.

The reference is the user's qualitative goal for meaningful finds, not a claim that Caves of Qud's implementation was inspected or reproduced. No implementation snippet or RED/GREEN gameplay cycle applies: this change consists only of an audit and its reproducible source-analysis evidence.
