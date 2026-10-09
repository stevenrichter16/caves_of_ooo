# Next 35 item passes: presentation/materials audit proposals

**Status:** read-only brainstorming, not implemented or promised. Twelve other obtainable item candidates, grouped by reusable mechanics. No reuse of FrogOil, SilverSand, VeilpuffBladder, cord, clay or water vessels. These are CoO-original design proposals; no Qud parity claim.

## Recommended ordering

Start with FireMoss/FrostLichen and PrismreedPith: they expose already-implemented terrain reactions and let players change an escape route. Add targeted thaw/extinguishing and real conductive chains next. Treat resin/wrap and ink as conditional work because their shared hooks or ordinary enemy use still need design. Make each completed pass a concrete interaction or cross-system counter-case; do not count 35 cosmetic aliases.

## Candidate inventory

### 1. FireMoss — Thermal tools
- **Proposed new use:** Kindle one adjacent flammable prop or an already-oiled tile. Opens a retreat through dry growth or starts an existing oil trap without learning pyromancy.
- **Cost / drawback:** One unit and one turn; no free heat on empty ground, ignition respects wetness and material veto; accidental fire and GroveLaw remain consequences.
- **Actual existing source:** Current loot/merchant tables: ApothecaryStock, ReagentCommon. Ordinary Villager-faction trade stock also selects this exact ID; eligible world pipelines run TradeStockBuilder.
- **Preserve:** Alchemy input profile heat:2, volatile:1. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Mishap: reagent consumed, no item, and up to 2 self-damage clamped to leave 1 HP; combine with combustible to make burning throwable.
- **Implementation / confidence:** Use actual ApplyHeat/FireDose on the selected physical prop, or AddHeat + ResolveAfterAbility for a coated tile. Do not silently create fuel. Low implementation risk.
- **Source audit evidence:** `Assets/Resources/Content/Blueprints/Objects.json:5311`, `Assets/Resources/Content/Data/Loot/LootTables.json:746`, `Assets/Scripts/Gameplay/Economy/TraderPart.cs:70`

### 2. FrostLichen — Thermal tools
- **Proposed new use:** Rub one adjacent wet tile with cold to turn that real puddle into slippery ice. This is route control using terrain the player has found or prepared.
- **Cost / drawback:** One unit; only real water coating; no instant ice from dry dirt and no ice bridge across deep pools. Freeze reaction can catch the occupant, including allies.
- **Actual existing source:** Current loot/merchant tables: ApothecaryStock, ReagentCommon. Ordinary Villager-faction trade stock also selects this exact ID; eligible world pipelines run TradeStockBuilder.
- **Preserve:** Alchemy input profile cold:2. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Frozen coating potency 2 (effect cold caps at 1).
- **Implementation / confidence:** ZoneTileStateSystem.ApplyColdToTile then explicit ResolveAfterAbility. The existing freeze_water reaction also freezes the occupant. Low risk; do not overstate as nondamaging harmless cooling.
- **Source audit evidence:** `Assets/Resources/Content/Blueprints/Objects.json:5417`, `Assets/Resources/Content/Data/Loot/LootTables.json:750`, `Assets/Scripts/Gameplay/Economy/TraderPart.cs:70`

### 3. GlacierSalt — Thermal tools
- **Proposed new use:** Pack a burning adjacent object with a strong cold dose; useful for saving an obstructing gate, a burning ally or a still-valuable cache. Different target and tactical purpose from lichen ice.
- **Cost / drawback:** Consumed; stronger cooling can freeze the rescued target, so the player exchanges fire danger for a short immobility risk. Cannot use inventory while already fully frozen.
- **Actual existing source:** Current loot/merchant tables: ReagentRare, UrnT2. Ordinary Villager-faction trade stock also selects this exact ID; eligible world pipelines run TradeStockBuilder.
- **Preserve:** Alchemy input profile cold:3, binding:1. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Frozen potency 3 plus Stoneskin reduction 1.
- **Implementation / confidence:** ThermalPart ApplyHeat with negative finite dose; verify crossing semantics and derive a target-sensitive dose rather than blindly assigning FrozenEffect. Medium: extinguishing depends on threshold, not merely adding cold.
- **Source audit evidence:** `Assets/Resources/Content/Blueprints/Objects.json:5451`, `Assets/Resources/Content/Data/Loot/LootTables.json:935`, `Assets/Scripts/Gameplay/World/Generation/ContainerPlacementService.cs:185`

### 4. EmberFruit — Thermal tools
- **Proposed new use:** Crush warm pulp against an adjacent frozen companion or frozen interactable to thaw it before natural recovery.
- **Cost / drawback:** Consumed; warmth can ignite exceptionally flammable targets. No autonomous self-use while Frozen blocks actions; explicit neighbor action only.
- **Actual existing source:** Current loot/merchant tables: ReagentRare, DrifterStock. Ordinary Villager-faction trade stock also selects this exact ID; eligible world pipelines run TradeStockBuilder.
- **Preserve:** Alchemy input profile heat:1, sweet:1. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: 1d4 healing snack; supplies heat to mixed brews.
- **Implementation / confidence:** ThermalPart ApplyHeat already calls FrozenEffect.Thaw for positive temperature deltas. Preserve its reagent heat:1,sweet:1 profile. Medium: dose must make useful thaw without ordinary flesh ignition.
- **Source audit evidence:** `Assets/Resources/Content/Blueprints/Objects.json:5383`, `Assets/Resources/Content/Data/Loot/LootTables.json:945`, `Assets/Scripts/Gameplay/Economy/TraderPart.cs:70`

### 5. GlimmerBrine — Conduction and drying
- **Proposed new use:** Paint a short conductive wet strip onto dry walkable ground to connect existing water/metal to a planned electrical strike.
- **Cost / drawback:** Finite thin layer; consumes reagent; never yields a recoverable liquid pool. Conducts against everyone and does not create the strike itself.
- **Actual existing source:** Current loot/merchant tables: ReagentRare, WellKeeperStock, CuratorStock, EchoStock. Ordinary Villager-faction trade stock also selects this exact ID; eligible world pipelines run TradeStockBuilder.
- **Preserve:** Alchemy input profile corrosive:2, conductive:2. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Acidic potency 2 plus Electrified charge 2.
- **Implementation / confidence:** A brine coating needs verification against TilePropagation conductivity and electroconductive reaction gates: do not assume generic LiquidDefinition conductivity automatically powers every tile reaction. Medium; test actual propagation, dry counter-case and multi-cell victims.
- **Source audit evidence:** `Assets/Resources/Content/Blueprints/Objects.json:5485`, `Assets/Resources/Content/Data/Loot/LootTables.json:940`, `Assets/Scripts/Gameplay/Economy/TraderPart.cs:70`

### 6. SparkRoot — Conduction and drying
- **Proposed new use:** Crack a charged root against an adjacent real conductor to discharge through the existing wet/metal network. The utility is exploiting the route, stunning a pursuer across water and risking the player on that same network.
- **Cost / drawback:** One use; cannot act as a ranged bolt through dry nonconductive floor. Existing lightning damage is retained, not marketed as pure nondamage.
- **Actual existing source:** Current loot/merchant tables: ReagentCommon. Ordinary Villager-faction trade stock also selects this exact ID; eligible world pipelines run TradeStockBuilder.
- **Preserve:** Alchemy input profile conductive:3. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Electrified charge 3.
- **Implementation / confidence:** ZoneTileStateSystem.AddCharge then ResolveAfterAbility/ResolveWorld; bounded existing propagation. Do not directly apply Electrified to everyone in a radius. Low-medium integration risk.
- **Source audit evidence:** `Assets/Resources/Content/Blueprints/Objects.json:5519`, `Assets/Resources/Content/Data/Loot/LootTables.json:917`, `Assets/Scripts/Gameplay/World/Generation/ContainerPlacementService.cs:185`

### 7. PrismreedPith — Conduction and drying
- **Proposed new use:** Use absorbent pith to wick away one thin coating from an adjacent tile, making a dry break in a wet electrical path or clearing oil from an escape foothold.
- **Cost / drawback:** One unit; cannot remove an actual pool, permanent liquid source, cloud, residue or all layers at once. No recovered liquid; a poisoned or burning tile may stay dangerous.
- **Actual existing source:** Harvest PrismreedCrop in current Stump biome patches: 2 PrismreedPith and 1 PrismreedSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.
- **Preserve:** Alchemy input profile conductive:1. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Electrified tonic charge 1.
- **Implementation / confidence:** ZoneTileState.RemoveCoating is exact-layer capable. Require existing finite removable coating and compare before/after; retain brewing conductive:1 profile. Medium: source re-seeding must be rejected explicitly.
- **Source audit evidence:** `Assets/Resources/Content/Blueprints/Objects.json:51043`, `Assets/Resources/Content/Data/Farming/BiomeCrops.json:394`, `Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs:86`

### 8. StoneburrSeed — Contact surfaces
- **Proposed new use:** Scatter a visible hooked-burr patch that briefly hobbles the next grounded creature, helping allies land a hit as the creature crosses the patch.
- **Cost / drawback:** One unit, one crossing; everybody including planter can trigger. Hobbled is−3DV, explicitly not slowed movement. Boots/large-body eligibility needs a deliberately authored rule rather than assumed real-world protection.
- **Actual existing source:** Current loot/merchant tables: ReagentCommon, FarmerStock. Ordinary Villager-faction trade stock also selects this exact ID; eligible world pipelines run TradeStockBuilder.
- **Preserve:** Alchemy input profile binding:2. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Stoneskin reduction 2 for 30 turns.
- **Implementation / confidence:** Reuse deployed-supply single-consume/ground-contact architecture, but apply a short nonextending HobbledEffect instead of Rooted. Low-medium; distinct tactical purpose from cord snare is hit setup, not retreat delay.
- **Source audit evidence:** `Assets/Resources/Content/Blueprints/Objects.json:5698`, `Assets/Resources/Content/Data/Loot/LootTables.json:912`, `Assets/Scripts/Gameplay/Economy/TraderPart.cs:70`

### 9. PitchpodResin — Equipment preparation
- **Proposed new use:** Dress the grip of one held weapon so one subsequent successful disarm is resisted; sticky residue is spent defending the grip.
- **Cost / drawback:** Consume resin and preparation turn; temporary one-charge protection tied to the exact weapon; dropping or changing grip does not transfer benefit to a different weapon. Keep ordinary brewing use.
- **Actual existing source:** Harvest PitchpodCrop in current Spread biome patches: 2 PitchpodResin and 1 PitchpodSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.
- **Preserve:** Alchemy input profile heat:1,combustible:1. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Burning coating intensity 1. Botanical ink desk service uses 2 SootrootPulp + 1 PitchpodResin + 3 drams to create InkVial while living attendant is present.
- **Implementation / confidence:** Requires shared pre-disarm query at Cudgel_Disarm before Unequip/Transfer, plus saved finite one-use modifier. Medium new mechanic, not currently supported by raw resin. Do not count adding aliases as extra depth passes.
- **Source audit evidence:** `Assets/Resources/Content/Blueprints/Objects.json:44074`, `Assets/Resources/Content/Data/Farming/BiomeCrops.json:19`, `Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs:86`

### 10. GripfrondWrap — Equipment preparation
- **Proposed new use:** Tighten worn gripfronds for a brief steady-grip stance that reduces the chance of being disarmed while the player keeps that handwear equipped.
- **Cost / drawback:** Occupies handwear; spends an action; short cooldown and no stacking with resin guarantee. Replacing wrap immediately ends its benefit.
- **Actual existing source:** Harvest GripfrondCrop in current Stump biome patches: 1 GripfrondWrap and 1 GripfrondSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.
- **Preserve:** Equip as Handwear; AV 1, DV 0 for the covered body slot(s).
- **Implementation / confidence:** Share the same real disarm gate as resin; make this a reusable equipment tradeoff versus a consumable weapon preparation. Medium. Need current NPC disarm availability checked before shipping: if ordinary enemies never disarm, this is player-versus-NPC armor support rather than a useful player tool.
- **Source audit evidence:** `Assets/Resources/Content/Blueprints/Objects.json:51891`, `Assets/Resources/Content/Data/Farming/BiomeCrops.json:439`, `Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs:86`

### 11. GlowQuartz — Visibility tools
- **Proposed new use:** Crack a quartz piece into a temporary bright ground beacon; lights a hazardous room from a distance and frees the hand otherwise occupied by a torch.
- **Cost / drawback:** Sacrifice a valuable infusion mineral; finite light lifetime; no stealth aggro/lure claim because enemy vision is not generally lighting-sensitive. Must really be throwable or gain a deliberate targeted toss action.
- **Actual existing source:** Current loot/merchant tables: ArcanistStock, DeathConstructT2, DeathConstructT3, OreCacheT1, OreCacheT2, OreCacheT3. Harvest GlowQuartzVein: 1–2 actual mineral items per finite vein.
- **Preserve:** Inventory Infuse action consumes one near forge, uses one modification slot on compatible carried unstacked equipment, and adds +2 equipped-item light radius. Same mineral also supports the authored tinkering infusion recipe.
- **Implementation / confidence:** Real LightSourcePart plus saved finite lifetime, native ground owner and existing LightMap/FOV refresh. Low-medium; ground model and expiry must be visible. A practical exploration/combat-readability aid, not control AI.
- **Source audit evidence:** `Assets/Resources/Content/Blueprints/Objects.json:28710`, `Assets/Resources/Content/Data/Loot/LootTables.json:812`, `Assets/Scripts/Gameplay/Economy/TraderPart.cs:70`

### 12. InkVial — Visibility tools
- **Proposed new use:** Splash an adjacent sighted enemy in the face to shorten sight briefly, allowing a turn to get out of view.
- **Cost / drawback:** Consumes ink otherwise used for rites/rentals, requires actual eligible sensory anatomy and visible adjacent target; does not blind eyeless creatures; no infinite stacking.
- **Actual existing source:** Quillhold Scribe, trade/loot tables: CampGoodsT1, ArcanistStock, BookshelfT1, BookshelfT2, BookshelfT3, AlchemyShelfT1, ReliquaryT1, ScribeStock, CuratorStock, ChoirStock, EchoStock. BotanicalInkDesk can produce a vial from carried processing inputs.
- **Preserve:** Consume Use for +25 rental Ink; alternatively consume vial to re-ink a carried rite book +5 charges (cap 10), or pay the Quillhold copying service.
- **Implementation / confidence:** Higher risk: there is no verified generic BlindEffect. A new sensory penalty must influence player FOV, ordinary AI and authored ranged aim consistently, preserve adjacent tactile danger and already-committed attacks. Reuse last-seen pursuit once real LOS/sight range is lost. Do later unless the full sight seam can be covered.
- **Source audit evidence:** `Assets/Resources/Content/Blueprints/Objects.json:2416`, `Assets/Resources/Content/Data/Loot/LootTables.json:300`, `Assets/Scripts/Gameplay/Items/InkVialPart.cs:39`

## Source verification and corrections

| Premise | Actual source | Consequence |
|---|---|---|
| Hobbled slows movement | `Gameplay/Effects/Concrete/HobbledEffect.cs:4–18`:−3DV only | Burrs are hit-setup, never a promised escape slow. |
| Acid corrodes metal locks | `Gameplay/Effects/Concrete/AcidicEffect.cs:40–50`: damage is Organic-only | Deferred SourmantleFold lock-solvent idea; would require an actual new corrosion/destructibility feature. |
| One-cell cold resolves immediately | `Gameplay/World/Map/ZoneTileStateSystem.cs:256`: AddCold only | Explicitly resolve after writing; multi-cell helper already resolves once. |
| Any cooling extinguishes | `Gameplay/Materials/ThermalPart.cs:82–84`: only downward flame-threshold crossing | Test actual hot and below-threshold-burning cases; avoid saying generic cooling guarantees cure. |
| Heat can thaw existing freeze | `Gameplay/Materials/ThermalPart.cs:75`: positive delta calls FrozenEffect.Thaw | Warmth can be a real rescue use; must account for heat capacity and ignition. |
| Freeze is a harmless slow | `Gameplay/Effects/Concrete/FrozenEffect.cs:26–32`: any Cold>0 blocks actions | Cold tools need explicit lockout risk and cannot self-rescue through blocked action input. |
| Disarm already has resistance hook | `Gameplay/Skills/Cudgel_Disarm.cs:42–100`: finds weapon, checks dimensions, then proceeds | Add one authoritative gate, not UI-only prevention. |
| Wet/charge interaction is nondamage | `Content/Data/TileReactions/Reactions.json`: electrify_water damage5 + Electrified | Preserve and warn of self/friendly danger; never market it as harmless distraction. |
| Render preview proves gameplay | Native source tests and separate root-owned import/live harness | Offline geometry is not ordinary combat balance or in-game discoverability proof. |

## Shared implementation guardrails

- The existing audit snapshot is at c7e154dbc. Revalidate the exact item blueprint and currently called source table immediately before implementation; crop sources are optional in seed-selected patches, not guaranteed in every chunk.
- Use one paid inventory-action transaction per use; exact item ownership, stack quantity, zone/origin and target state in the command token. One successful delta consumes one item; stale or physically meaningless actions consume nothing.
- Stage state reversibly, then run damage/status reactions after commit; reentrant callbacks must not gain free effects or leave erased tile layers.
- Preserve reagent, forging, food and repair uses. Treat alternative uses as opportunity costs, not automatic conversions.
- Add finite saved state and concrete marks to ground tools. Existing scenery/source library is appropriate for deployed physical tools; transient volumes are appropriate for actual coats/energies. No invented quest-marker overlays.
- Counter-check dry/solid/hidden/source-backed tiles, player versus enemy symmetry, allies, existing stronger status, expired state, moved owner, save/load and body footprints. UI choice must close inventory and cost exactly one real action.
- Record original behavior RED before production; native positive/counter tests plus isolated real-menu scenario; only then advertise the verb in dynamic item help.
