# Raw material field uses — passes19–29

**Status:** implemented and native EditMode verified: **36/36 main + 30/30 adversarial tests pass** in `native-final-meal-review-red.xml`. Its filename refers to another group’s remaining review; all 66 material cases are GREEN. Live player-facing verification is coordinated by the parent task. **Classification:** CoO-original, existing simulation connections rather than Qud parity.

## Numeric design and ordinary sources

### 19 — FireMoss
Kindle a dry, unlit non-creature whose own material burns and whose ignition threshold is crossed by 600 J; alternatively ignite an adjacent oil film through the existing heat reaction. Refuse wet fuel, exhausted authored fuel, stone, already burning targets and thick/cold fuel this dose cannot ignite.

**Ordinary source:** Current loot/merchant tables: ApothecaryStock, ReagentCommon. Ordinary Villager-faction trade stock also selects this exact ID; eligible world pipelines run TradeStockBuilder.

**Existing use retained:** Alchemy input profile heat:2, volatile:1. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Mishap: reagent consumed, no item, and up to 2 self-damage clamped to leave 1 HP; combine with combustible to make burning throwable.

**Source references:** `Assets/Resources/Content/Blueprints/Objects.json:5311`, `Assets/Resources/Content/Data/Loot/LootTables.json:746`, `Assets/Scripts/Gameplay/Economy/TraderPart.cs:70`

### 20 — FrostLichen
Write one cold unit to one adjacent visible water-coated tile and resolve the existing freeze_water reaction once. Result is four-turn ice and the native Frozen effect on an occupant. Dry ground is refused; no new bridge or permanent ice. Explicit ally risk.

**Ordinary source:** Current loot/merchant tables: ApothecaryStock, ReagentCommon. Ordinary Villager-faction trade stock also selects this exact ID; eligible world pipelines run TradeStockBuilder.

**Existing use retained:** Alchemy input profile cold:2. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Frozen coating potency 2 (effect cold caps at 1).

**Source references:** `Assets/Resources/Content/Blueprints/Objects.json:5417`, `Assets/Resources/Content/Data/Loot/LootTables.json:750`, `Assets/Scripts/Gameplay/Economy/TraderPart.cs:70`

### 21 — GlacierSalt
Apply −300 J to an adjacent target with a valid ThermalPart that is above ambient. Uninvited cooling provokes outsiders because crossing freezing can hurt or immobilize them. Native cooling can extinguish or freeze depending on starting temperature and heat capacity. It is cooling, not guaranteed flame removal.

**Ordinary source:** Current loot/merchant tables: ReagentRare, UrnT2. Ordinary Villager-faction trade stock also selects this exact ID; eligible world pipelines run TradeStockBuilder.

**Existing use retained:** Alchemy input profile cold:3, binding:1. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Frozen potency 3 plus Stoneskin reduction 1.

**Source references:** `Assets/Resources/Content/Blueprints/Objects.json:5451`, `Assets/Resources/Content/Data/Loot/LootTables.json:935`, `Assets/Scripts/Gameplay/World/Generation/ContainerPlacementService.cs:185`

### 22 — EmberFruit
Apply +150 J to an adjacent actually Frozen target with valid ThermalPart; native thermal thaw reduces Cold proportional to temperature gain. Partial thaw is meaningful. No action while the user is frozen; no cures for Rooted or Stunned.

**Ordinary source:** Current loot/merchant tables: ReagentRare, DrifterStock. Ordinary Villager-faction trade stock also selects this exact ID; eligible world pipelines run TradeStockBuilder.

**Existing use retained:** Alchemy input profile heat:1, sweet:1. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: 1d4 healing snack; supplies heat to mixed brews.

**Source references:** `Assets/Resources/Content/Blueprints/Objects.json:5383`, `Assets/Resources/Content/Data/Loot/LootTables.json:945`, `Assets/Scripts/Gameplay/Economy/TraderPart.cs:70`

### 23 — GlimmerBrine
Spread eight-turn brine coating on one adjacent passable tile. Conduction only: no charged energy, no recoverable physical pool and no body coat/resistance buffs from merely standing on the tile.

**Ordinary source:** Current loot/merchant tables: ReagentRare, WellKeeperStock, CuratorStock, EchoStock. Ordinary Villager-faction trade stock also selects this exact ID; eligible world pipelines run TradeStockBuilder.

**Existing use retained:** Alchemy input profile corrosive:2, conductive:2. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Acidic potency 2 plus Electrified charge 2.

**Source references:** `Assets/Resources/Content/Blueprints/Objects.json:5485`, `Assets/Resources/Content/Data/Loot/LootTables.json:940`, `Assets/Scripts/Gameplay/Economy/TraderPart.cs:70`

### 24 — SparkRoot
Add one charge unit to an adjacent conducting tile, then run the native bounded network propagation/reaction. Refuse dry nonconductors and already maximally charged tiles. This can damage/stun anybody connected, including user/allies.

**Ordinary source:** Current loot/merchant tables: ReagentCommon. Ordinary Villager-faction trade stock also selects this exact ID; eligible world pipelines run TradeStockBuilder.

**Existing use retained:** Alchemy input profile conductive:3. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Electrified charge 3.

**Source references:** `Assets/Resources/Content/Blueprints/Objects.json:5519`, `Assets/Resources/Content/Data/Loot/LootTables.json:917`, `Assets/Scripts/Gameplay/World/Generation/ContainerPlacementService.cs:185`

### 25 — PrismreedPith
Consume one pith to remove exactly one selected finite liquid coating from an adjacent passable tile. Reject permanent coatings, actual nonempty pools, matching renewing TileStateSource and solid ice/lava. Other films, residues, gas and energy remain; no collected liquid is created.

**Ordinary source:** Harvest PrismreedCrop in current Stump biome patches: 2 PrismreedPith and 1 PrismreedSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**Existing use retained:** Alchemy input profile conductive:1. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Electrified tonic charge 1.

**Source references:** `Assets/Resources/Content/Blueprints/Objects.json:51043`, `Assets/Resources/Content/Data/Farming/BiomeCrops.json:394`, `Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs:86`

### 26 — LampOil
Spread eight-turn oil film on one adjacent passable tile. Same physical oil surface as FrogOil:40% slip and flammable; no body-coat fire vulnerability or recoverable pool. This is intentionally one item pass sharing existing slick mechanics, while retaining refueling and brewing.

**Ordinary source:** Current loot/merchant tables: CaveSupplyT1, DeepSupplyT2, ProvisionerStock. Ordinary Villager-faction trade stock also selects this exact ID; eligible world pipelines run TradeStockBuilder.

**Existing use retained:** Alchemy input profile combustible:3, viscous:1. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Wet coating potency 1; add heat for burning. Inventory refuel action consumes one measure to add up to 25 fuel (capped) to carried, unlit, partially depleted TorchLight/Fuel items, including WickrushCandle.

**Source references:** `Assets/Resources/Content/Blueprints/Objects.json:5345`, `Assets/Resources/Content/Data/Loot/LootTables.json:158`, `Assets/Scripts/Gameplay/Economy/TraderPart.cs:70`

### 27 — SlipsedgeGel
Spread eight-turn gel film on one adjacent passable tile. Actual gel is50% slippery, conductive100 and nonflammable0. Distinct hazard tradeoff from oil. No recovered pool or automatic body coat.

**Ordinary source:** Harvest SlipsedgeCrop in current Sodden biome patches: 2 SlipsedgeGel and 1 SlipsedgeSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**Existing use retained:** Alchemy input profile viscous:1. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Wet coating moisture 1.

**Source references:** `Assets/Resources/Content/Blueprints/Objects.json:46049`, `Assets/Resources/Content/Data/Farming/BiomeCrops.json:124`, `Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs:86`

### 28 — PitchpodResin
Smear one adjacent creature with pitch LiquidCoveredEffect amount28:−2Agility/−3DV and Heat vulnerability from real pitch. Lasts4 ordinary cool end-turn dry steps at7amount/turn; heat dries faster. Does not immobilize. Refuse any already-coated target to avoid arbitrary mixing/erasing valuable liquids. Uninvited use provokes.

**Ordinary source:** Harvest PitchpodCrop in current Spread biome patches: 2 PitchpodResin and 1 PitchpodSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**Existing use retained:** Alchemy input profile heat:1,combustible:1. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Burning coating intensity 1. Botanical ink desk service uses 2 SootrootPulp + 1 PitchpodResin + 3 drams to create InkVial while living attendant is present.

**Source references:** `Assets/Resources/Content/Blueprints/Objects.json:44074`, `Assets/Resources/Content/Data/Farming/BiomeCrops.json:19`, `Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs:86`

### 29 — Honeycomb
Smear one adjacent creature with honey LiquidCoveredEffect amount22:−2Agility/−3DV and lesser Heat vulnerability than pitch. Lasts2 ordinary cool end-turn dry steps at11amount/turn; heat dries faster. Gives up edible healing1d6. Refuse already-coated target; uninvited use provokes.

**Ordinary source:** Current loot/merchant tables: BasketT1, BasketT3, InnkeeperStock, EnvoyStock. Harvest current Beehive world objects; consumes the finite source, yields carried items or drops overflow.

**Existing use retained:** Eat one carried unit: restores 1d6 HP, capped at maximum.

**Source references:** `Assets/Resources/Content/Blueprints/Objects.json:28320`, `Assets/Resources/Content/Data/Loot/LootTables.json:1936`, `Assets/Scripts/Gameplay/Economy/TraderPart.cs:70`

## Verification sweep corrections

| Assumption | Verified actual source | Design correction |
|---|---|---|
| All combustible materials use one scale | MaterialPart.cs documents mixed percentage/fraction authoring; Bush0.6,WoodenBarrel0.7; TryIgnite only checks>0 | Scenery kindling uses actual thermal/material ignition contract, not TilePropagation’s50 threshold. No fire-scale migration in this tranche. |
| General flammable film + heat burns | Reactions.json defines ignite_oil_heat; pitch/honey/gel do not have equivalent film reactions | FireMoss ground option only targets existing oil; pitch/honey are actual creature coats. |
| Cold helper resolves itself | ZoneTileStateSystem.ApplyColdToTile only AddCold | ResolveAfterAbility after commit, explicitly once. |
| Cold always extinguishes | ThermalPart requires downward flame-threshold crossing | Label GlacierSalt as cold pack; report actual cooling, with freeze risk. Refuse active below-threshold flame when not above ambient. |
| Brine film gives body resistance | LiquidCoveredEffect applies body mods; tile layers only route ground systems | Thin brine does not grant+15HeatResistance/−15ElectricResistance to creatures. |
| Honey immobilizes | honey.json Sticky=true, but LiquidCoveredEffect itself applies only statmods | Advertise−2Agility/−3DV, no rooting/slowing claim. |
| All coat amounts equal durations | pitch dry7;honey dry11; temperature>50 accelerates | Amount28 and22 yield approximate4/2coolturns, not hard durations. |
| AfterInventoryAction cannot invalidate pending effects | Existing transaction lacked before-commit validator | Root adds BeforeCommit(Func<bool>); services revalidate exact origin, quantity, target/parts and material state before irreversible reactions. |

## API / transaction plan

`MaterialFieldActions.IsCommand(string)`, `AddActions(actor,item,zone,actions)`, internal `TryAct(actor,item,zone,command,transaction)`. Parent owns native dispatch, paid-menu allowlist and dynamic help. Command schema: `MaterialField|verb|escapedZone|originX|originY|x|y|escapedTargetId|quantity|escapedLayer`. Valid verbs are bound to exact item IDs, never caller-selected arbitrary element IDs. Entity targets bind their anchor coordinates; reach/visibility additionally require an actual nearby visible occupied contact. Ground actions bind tile coordinates.

Shared torch helper contract: `KindlingJoules=600f`, `CanKindle(Entity)`, `Kindle(actor,target,zone)`, `CanKindleTile(zone,x,y)` and `KindleTile(actor,zone,x,y)`. Equipment group supplies its own exact worn/held ownership, fuel payment and current target gates.

Consumption uses InventoryTransferSnapshot plus claims. Failures or exceptions before commit restore the original carried unit/quantity. Finite film writes/removals, thermal application and damaging elemental reactions occur only after commit. Coats stage through the normal lifecycle so effect vetoes can refuse payment; rollback removes only the newly added effect and its stat deltas, preserving unrelated layers/effects. In all cases, root’s pre-commit validator rejects state changes by outer callbacks. Diagnostics, world notification and text are separate commit callbacks so one observer cannot suppress the rest. No command claims guarantees against arbitrary exceptions inside an already-committed simulation callback.

Reject unchanged films with equal/longer lifetime; no new coating pool entities. Wicking selects one layer by ID, verifies current exact layer/duration and rejects source-backed replenishment. Creature smears reject existing LiquidCoveredEffect instead of invoking dominance mixing; this keeps coat undo exact and prevents deleting rare beneficial coatings. Render uses existing liquid/transient status appearances; no new models or persistent Parts required.

## Performance

Inventory menu/actions only: bounded3×3 ground scan plus visible nearby-owner enumeration. No new Update/LateUpdate/turn listeners, caches or full-zone render invalidation. Reuse tile dirty hooks and mark recipient cell after heat/coat changes. Native terrain propagation retains its existing bounded flood and pools.

## Evidence / review

Native RED: `native-three-groups-red.xml` records 36 material cases (10 passed counter-checks; 26 failed because the new native commands were absent). Production and a separate 30-case `MaterialFieldAdversarialTests` fixture are now on disk. Native GREEN, isolated UI checks and final source review remain pending. Tests must cover no-op, stale selections, payment callback and post-action mutation, duplicate/replayed commands, source-backed pools, elemental reactions, friendly-fire/provocation, ownership aliases, save/expiry and exact unrelated-state retention.

### In-phase review

- 🟡 Found a pre-apply callback race: a separately applied body coat could be merged by the pending smear before payment was rejected. Add an adversarial callback case and guard the pre-intrinsic apply seam; preserve the independent coat untouched. Confirmed RED in `native-review-red.xml` (expected water, got pitch); fixed through a read-only current-recipient check at the pre-intrinsic apply boundary, before any OnStack merge. Native GREEN confirmed: all 30 material adversarial cases pass in `native-final-meal-review-red.xml`.
- 🔵 Ground films and wicking publish after commit instead of staging reversible tile mutations. This avoids free reactions when an outer inventory observer refuses the action and preserves unrelated callback-written tile layers.
- 🔵 Explicit limits: raw cold can freeze and raw heat may crack brittle material; these use existing thermal behavior, not guaranteed cleanses. Pitch/honey spoil dodge and increase fire damage but do not root.

First native attempt: all 29 material adversarial cases passed. Two of the 36 main cases failed because the existing-coat counter-test asked whether *any* smear was offered: the uncoated player was still a valid recipient. Corrected the fixture to bind the coated target ID and explicitly assert the player remains eligible. This is test precision, not a production behavior change. `native-first-green-attempt.xml` retains the raw result. Added the independent pre-apply coat race as the 30th adversarial case for a targeted native RED before the fix.

Targeted native review: the corrected 36 main material tests pass. The new 30th adversarial case failed as predicted: a pre-apply water coat became pitch despite a refunded smear. Production now uses the existing intrinsic effect receipt callback to reject a changed recipient before stacking mutates it; no shared status lifecycle changes are added by this module.

### Practical promise review

All eleven uses have ordinary sources and a native target witness. FireMoss targets real combustible scenery or oil; the fixed small dose deliberately refuses thick/cold objects it cannot kindle (a normal 25° tree with capacity2.5 and ignition360° needs more than600 J). FrostLichen targets **water** coating specifically; brine, gel and mud are not silently treated as water. Both ordinary water and other conductors have authored electrical reactions (`electrify_water`, `electrify_conductor`), so brine/metal preparation works without an invented radius attack. GlacierSalt follows the target’s actual thermal thresholds; EmberFruit can partly thaw without instantly curing freezing. Pith absorbs finite layers rather than draining source pools. Pitch/honey penalties and dry-down read the real liquid definitions. No additional ordinary-target blocker found in the source review.

Final material test evidence: parsed `native-final-meal-review-red.xml` directly: `MaterialFieldTests` **36 passed / 36 total**, `MaterialFieldAdversarialTests` **30 passed / 30 total**. The independent pre-apply coat race now passes, preserving the separately applied water coat and refunding the refused smear. These native EditMode results verify commands, simulation, ownership, rollback and persistence; they do not independently establish visual readability, menu feel or live expedition balance. Parent-controlled live verification remains separate. No C# files changed during this documentation update.
