# Next item utility passes: medicine, food and equipment

Status: source-grounded proposals only, 2026-10-09. These are candidates for the user's requested next 35 **other** items, not implemented behavior. Current cord / water-vessel / fire-clay work and prior frog-oil / silver-sand / veilpuff work are excluded. This file deliberately separates a new interaction from an existing payload; a generic verb shared by several items is one mechanical system, even when several distinct item roles gain that verb.

## Verified premises

* Existing healing/cure crops already have throwable tonic payloads. Do not describe throwing a knitmoss pad at an ally as a new invention: `TonicPart.HasThrowablePayload` and `ApplyTo` (`Assets/Scripts/Gameplay/Items/TonicPart.cs:78-143`), plus `ThrowItemCommand`, already support it.
* A reliable adjacent **treat this ally** interaction is absent from `TonicPart.GetInventoryActions`: it only provides the self-use `ApplyTonic` action (`TonicPart.cs:37-48,65-75`). A targeted treatment can remove the strength/range/trajectory/scatter uncertainty of a throw in exchange for approaching and spending an action. It must use exact target ownership, preserve one-unit cost, and retain existing throwing as the risky ranged option.
* Food currently targets only the eater. `FoodPart.DoEat` obtains the actor from the inventory event, consumes their item, heals their HP, then applies a prepared meal to that same actor (`Assets/Scripts/Gameplay/Items/FoodPart.cs:61-107`). A feed/share service would be a genuine new recipient decision.
* Prepared meals already compete for one meal effect. Applying a meal to a follower must preserve that replacement rule, not stack every food bonus (`FoodPart.cs:88-107`; `Assets/Scripts/Gameplay/Effects/Concrete/PreparedMealEffect.cs`).
* Dropped raw and dried meat already divert explicitly configured furrowstalkers. It is neither a universal enemy pacifier nor an unimplemented idea (`Assets/Scripts/Gameplay/AI/SpreadPredatorPart.cs:25-31,306-326,339-402`). Poisoning bait would be a new consumer; no such consumption payload is presently called by the meat diversion.
* Do not use `HobbledEffect` to promise slowing: it applies only a DV penalty (`Assets/Scripts/Gameplay/Effects/Concrete/HobbledEffect.cs:8-13,33-45`).
* Forced movement deliberately bypasses ordinary `BeforeMove` veto (`Assets/Scripts/Gameplay/Turns/MovementSystem.cs:178-204`). A bracing item needs a narrow new physical-force check; changing voluntary Rooted behavior is not sufficient.

## Twelve item passes

### 1. KnotflaxBandage: bind another creature's wound

**Situation:** a companion is bleeding and cannot survive waiting for a safe throw. Open the bandage and choose the adjacent bleeding recipient. Spend one bandage and one action; remove their existing Bleeding effect, preserving poison, fire and beneficial effects. No action is offered for an unbleeding target.

**Actual source:** prepare one `KnotflaxCord` in inventory. Cord is stocked by the Morrowfast mender and grown at the western Morrowfast allotment. Existing blueprint and preparation source are recorded in `Docs/Verification/ItemUtility2026-10-08/catalog.json` under `KnotflaxBandage` / `KnotflaxCord`.

**New decision / drawback:** approach the threatened ally and spend your own attack/move; preserve the longer-range thrown option. This does not introduce an additional cure or make bleeding immunity.

**Hooks:** `TonicPart.ApplyTo`; `CureTonicPart.HandleEvent` (`Assets/Scripts/Gameplay/Items/CureTonicPart.cs:20-59`); exact nearby predicates and transaction shape from `WorldResourceActions` and `EmergencyDousingActions`. Limit the first service to a vetted positive payload allowlist, not arbitrary mixed brews.

### 2. SumpsievePad: draw poison from an adjacent ally

**Situation:** an ally has escaped a gas pocket but is still poisoned. Spend a pad on that ally immediately. Preserve its already-implemented ordinary **and gas-poison** cures.

**Actual source:** harvest ripe `SumpsieveCrop` in current Sodden biome patches, two pads plus one seed; seed-dependent patch selection, not a guaranteed spawn in every Sodden zone (`BiomeCrops.json:79`; `BiomeCropPlan.cs:29-43`; `BiomeCropPlacement.cs:64-70`).

**New decision / drawback:** choose whose poisoning to treat with a scarce field supply. Standing in gas can poison them again. Do not advertise respiratory prevention or fungal cure. Fix the existing crop text that incorrectly says gas poison is excluded when shipping this role.

**Hooks:** same positive-treatment service; `CureTonicPart.cs:46-47` already treats `PoisonedByGasEffect` alongside `PoisonedEffect`.

### 3. AbsentmintLeaf: clear an ally's confusion

**Situation:** a confused companion cannot reliably contribute or escape. Apply the leaf at close range to restore their normal decision-making without hurting them.

**Actual source:** `AbsentmintCrop` in ordinary Overwrit biome patches; harvest one seed and its authored leaf yield. The existing biome catalog and inherited tonic payload establish the role; do not claim every Overwrit visit has it.

**New decision / drawback:** spend a turn rescuing the companion instead of attacking the enemy that confused them; repeated vapor exposure can reapply confusion. No global pacification and no hostile mind control.

**Hooks:** `CureTonicPart`, `ConfusedEffect`, exact adjacent treatment. Counter-check that another actor's unrelated conditions and the healer's own confusion remain unchanged.

### 4. KnitmossPad: treat another creature's injuries

**Situation:** an ally is holding a narrow route. Apply their existing 2d4 healing directly, spending one pad and one of your actions.

**Actual source:** harvest `KnitmossCrop` in Grovelands patches, two pads and one seed (`Objects.json:48315`, `BiomeCrops.json:244`, snapshot line numbers).

**New decision / drawback:** close-range support versus retaining your personal emergency heal. Refuse at full HP or death; never revive a dead owner. A target is not guaranteed a maximum roll.

**Hooks:** `TonicPart.ApplyHealing` / `ApplyTo`. The public `ApplyTo` can consume the healer's item while applying to a different target, but it currently returns success even when a payload achieves nothing. The new interaction must precheck actual need and capture the recipient's HP for outer rollback, not rely only on the current command's actor-stat snapshot.

### 5. CookedMeat: share toughness preparation with a companion

**Situation:** prepare the companion who will occupy the exposed doorway, instead of keeping the stronger constitution for yourself.

**Actual source:** cook `RawMeat` beside a valid station; also ordinary innkeeper stock. Existing effect is +2 Toughness for 100 owner turns, plus healing.

**New decision / drawback:** one recipient per consumed meal, one action, adjacent willing living party member only. The companion's existing prepared meal is replaced, including a potentially useful elemental resistance.

**Hooks:** extract a reusable food payload service from `FoodPart.DoEat` rather than forging an inventory actor event whose item belongs to somebody else. Use `BrainPart.ArePartyAligned` (`Assets/Scripts/Gameplay/AI/BrainPart.cs:280-299`) for willingness. The recipient's actual owner-turns must expire the effect.

### 6. ToastedEmberwheat: fire preparation for another party member

**Situation:** send a companion into a hot corridor or against a fire user without requiring the player to wear or consume every countermeasure personally.

**Actual source:** cook grown `Emberwheat`; wayside-cook stock. Existing meal gives +20 HeatResistance for 100 recipient turns.

**New decision / drawback:** replaces their previous meal; provides resistance, not fire immunity. Part of the same food-sharing system as item 5, not a separate new engine feature.

**Hooks:** `FoodPart` + `PreparedMealEffect`; include native actual-damage comparison with an otherwise identical companion fed a different meal.

### 7. RoastedMushroom: acid preparation for a companion

**Situation:** protect a support companion who must cross acid-spattered ground or face an acid user, while you retain another resistance meal.

**Actual source:** cook obtainable mushrooms from mushroom rings, provisioners, villages and camp stock. Existing meal gives +20 AcidResistance for 100 owner turns.

**New decision / drawback:** mutual exclusion with other prepared meals; no acid cleanup and no protection for loose equipment or structures. Shared food engine, distinct encounter preparation.

**Hooks:** same food-sharing service; verify typed Acid damage reduction, exact consumed owner, and save/load of the recipient's meal modifiers.

### 8. RoastedHearthbulb: cold preparation for a companion

**Situation:** prepare an ally for cryo exposure before throwing your own cold-control item near a fight.

**Actual source:** cook `Hearthbulb`, grown in the western Morrowfast allotment. Existing meal gives +20 ColdResistance for 100 owner turns.

**New decision / drawback:** reduces cold damage but must not claim freeze immunity; the current freeze state is governed separately from typed cold resistance. Replaces the companion's previous meal.

**Hooks:** food sharing and `PreparedMealEffect`; explicitly test that ColdResistance does not silently erase `FrozenEffect`.

### 9. RawMeat: prepare a poisoned bait portion

**Situation:** trade one food ration and one poison tonic for a placed bait that can weaken an eligible predator while buying time to leave.

**Actual source:** ordinary provisioners, beast loot and wayside cooks; `PoisonTonic` has an obtainable source in the item census. Existing raw-meat diversion is already implemented and must remain available without poison.

**New decision / drawback:** two consumables, a preparation action, a placement action, and an animal must actually choose/consume it. Threat priority still overrides eating. No attraction through walls, immunity bypass, instant poisoning on merely seeing meat, or NPC memory wipe.

**Hooks / prerequisite:** add saved payload/provenance to the one prepared portion and consume it through the actual `SpreadPredatorPart` meat-consumption seam (`:390-402`). `PublicMeat` currently admits only exact RawMeat/DriedMeat blueprints (`:306-326`), so creating a new bait blueprint alone is insufficient. Deliberate allowlist and consumption callback required. Before broader animal support, author diet admission; do not imply a general food AI from this scoped predator behavior.

### 10. GripfrondWrap: brace against one physical shove or pull

**Situation:** wear the gripping wraps, take a preparation action, and cling to adjacent solid terrain while a telegraphed force attack resolves.

**Actual source:** harvest `GripfrondCrop` in Stump patches. Existing handwear AV1 occupies the handwear slot (`Objects.json:51891`, `BiomeCrops.json:439`, snapshot lines).

**New decision / drawback:** requires a nearby solid handhold, worn wraps and a short stationary preparation; moving voluntarily ends it. Counters one physical displacement, not stun, damage, teleportation or falling between zones. Occupied handwear slot and preparation turn matter.

**Hooks / prerequisite:** a narrow physical force resistance query at force callers (`SkillCombatHelpers.TryPush`, `HookedEffect.DragTowardHooker`) rather than vetoing all of `MovementSystem.ForceMoveTo` or pretending `Rooted` already provides this. This is a medium-sized prerequisite, lower priority than 1–8.

### 11. GroundwireScreen: discharge your current electrical charge

**Situation:** after surviving an electric hit, use the held screen to bleed off the lingering charge before it keeps damaging or chaining through the group.

**Actual source:** finite Sodden works locker, already geographically discoverable. Existing hand item supplies ElectricResistance50 while equipped.

**New decision / drawback:** must be in a real hand, costs an action, treats existing Electrified only, does not prevent incoming electric hits or cancel a stun that already landed. Preserve wetness and all other conditions. A new shock can reapply the danger.

**Hooks:** exact equipped item ownership and `ElectrifiedEffect.Charge/Duration`; removal through `StatusEffectsPart` so visible aura/state cleanup stays coherent. Query action-blocking before offering it: a fully stunned user cannot discharge themselves until able to act. Test cold, wet and fire remain.

### 12. IronshodBoots: plant your feet for a known force attack

**Situation:** spend the preparation action instead of retreating from a telegraphed shove; heavy boots let you keep the favorable tile.

**Actual source:** ordinary armorer / tier2-3 armor tables and recoverable marlback-breacher equipment. Existing Feet AV2 and Speed penalty5 provide a concrete mobility drawback.

**New decision / drawback:** same narrow force-resistance foundation as item10, different positioning requirement: feet on stable dry ground instead of an adjacent handhold. No protection while on oil/ice, no standing-on-water exception, no freeze or teleport immunity. One force resisted or brief expiry; stepping ends it.

**Hooks / prerequisite:** physical-force query plus real `LiquidSlipSystem.FindSlipperyLiquid` contacts across the complete body. This should not be called complete merely by adding a status icon; test the actual push/pull result and the identical oil-covered-ground counter-case.

## Scope pruning and priorities

1. First implement the shared **direct adjacent treatment** verb for a small positive allowlist, with exact target transaction support. It creates an immediate rescue decision from four already obtainable botanical/medical supplies.
2. Then implement **food sharing** for willing party members. The four listed meals have different existing preparation effects. Count this honestly as four improved items using one new interaction system. Other plain foods can receive the same feeding affordance without pretending they each add a distinct new mechanic.
3. Poison bait is a separate finite AI extension; its existing meat diversion scope must remain explicit. If work expands beyond the intended encounter family, split that expansion from the item pass.
4. Equipment bracing/discharge offers meaningful active roles but needs the verified prerequisites above. Prefer one force-resistance foundation reused consistently to item-specific patches in each skill.

Pruned: new direct thrown cures (already work); generic "food distracts every enemy" (false current AI premise); wet cloth as reliable poison-gas immunity (would be both misleading and mechanically broad); salt neutralizes acid (poor physical intuition); calling Hobbled a movement slowdown (false code premise); another oil slick or smoke item (duplicates the prior shipped tranche).

## Minimum verification for any accepted proposal

For each item: source acquisition evidence; actual native inventory action; exact one-unit payment; unsuccessful/no-op/stale use costs nothing; usable recipient and range; actor incapacitation; inventory and target rollback after observer exception; no unintended cure; save/load; action-time debit; and a native scenario showing the claimed recipient consequence. Never claim autonomous NPC selection just because a shared entity can carry the item.

Can verify by script: payment, recipient identity, HP/effects/modifiers, physical movement, AI bait state, turn budget and save graph. Cannot verify without playtesting: whether companion rescue feels worth the turn, whether a player notices the options naturally, and whether bait/brace tuning produces sufficient opportunity across natural encounters.
