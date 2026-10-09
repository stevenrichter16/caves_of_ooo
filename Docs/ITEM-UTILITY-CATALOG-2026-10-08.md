# Current obtainable items: item-by-item utility catalog

**Later implementation:** [Combat inventory improvisation](COMBAT-INVENTORY-IMPROVISATION.md) adds ground uses for FrogOil/SilverSand and sight-obscuring Veilpuff with bounded enemy pursuit. The entries below preserve the audit snapshot at `c7e154db`; they are not a second audit of those changes.

Audit date: 8 October 2026. Source: main `c7e154dbcdafcbef882ce3dac5a7fe9cec21773a`.

Read the [assessment and findings](ITEM-UTILITY-AUDIT-2026-10-08.md) first for the overall judgment. This catalog records **293 source-traced obtainable portable item families**: 286 static blueprints and seven runtime-created or made-portable families. It covers every resolved takeable candidate after accounting for eight templates and four unreachable concrete definitions. It separately covers eight movable world-object families and all 26 liquid definitions, of which six have a current working collection route.

“Obtainable” means a traced normal-play source, including crafting from obtainable inputs. A random table, optional layout, hostile owner or finite resource is not a guarantee that every save or visit has the item. Fresh-world additions can be absent from previously generated saved zones. This is source analysis, not a new Unity playtest. Generated brew/weapon/book/corpse variants are grouped by family.

The per-item source identifies at least one route. Some rows list other table memberships as context; mere membership is not independent proof that each listed table is live. No item is admitted solely from an unplaced merchant or a development grant. IDs support exact source lookup; displayed names can vary at runtime.

## Navigation

| Group | Obtainable families |
|---|---:|
| [Weapons](#weapons) | 35 |
| [Armor and clothing](#armor-and-clothing) | 19 |
| [Weapon components](#weapon-components) | 14 |
| [Manufactured gas grenades](#manufactured-gas-grenades) | 3 |
| [Tonics, cures and dressings](#tonics-cures-and-dressings) | 28 |
| [Reagents](#reagents) | 24 |
| [Foods and meals](#foods-and-meals) | 16 |
| [Seeds](#seeds) | 40 |
| [Other botanical outputs, materials and fuels](#other-botanical-outputs-materials-and-fuels) | 24 |
| [Vessels](#vessels) | 6 |
| [Grimoires and learned abilities](#grimoires-and-learned-abilities) | 34 |
| [Repair guides](#repair-guides) | 3 |
| [Tinkering schematics](#tinkering-schematics) | 3 |
| [Readable codex volumes](#readable-codex-volumes) | 13 |
| [Light, ink and currency](#light-ink-and-currency) | 4 |
| [Corpses, bones and trophies](#corpses-bones-and-trophies) | 8 |
| [Keys, documents, parcels and story goods](#keys-documents-parcels-and-story-goods) | 19 |

[Movable world objects](#movable-world-objects) · [Collectible liquids](#collectible-liquids) · [Excluded portable definitions](#excluded-portable-definitions) · [Other liquid definitions](#other-liquid-definitions)

## Shared rules for interpreting utility

- Selling is an economic use, not proof of a unique mechanic. Authored Commerce value is a pricing input, not a guaranteed sale price. Trader funds, willingness, capacity, NoTrade, rental and event vetoes still apply. Runtime variants can change the base data.
- Throwing needs an explicit Handling part, takeability, access, Strength and range. A named item being portable does not by itself make it throwable. A Throw action does not imply a special damage payload.
- An item supporting a learned skill does not grant that skill. Weapon family tags matter. Short-blade skills currently check Piercing; long-blade, axe and cudgel skills check their separate attributes.
- Armor AV normally protects its struck body slot; DV is accumulated across equipped gear. Hand armor occupies a hand and does not add a separate blocking subsystem. Cloak's AV has a specific location-selection gap described in its row.
- NPC equipment use is distinguished from merchant stock and ordinary NPC possession. Self-medication, gifts, predator diversion and local requests require the particular configured actors/actions. The general player inventory API is not proof of universal NPC AI behavior.
- Food restores HP; there is no nutrition/hunger payoff to assume. Prepared meals add their specified temporary stat effect. Waterskin drinking instead relieves one Parched stack per charge, not HP. Liquid flasks have no Drink action.
- Single weapon assembly and single brewing work in the field. Batch forging needs a forge; only non-Food brewing batches need a still. Weapon reforging/tempering needs a forge. Physical mineral infusion works in normal play without BitLocker; bit tinkering does not.
- Brewed status mixtures can temper a weapon regardless of their named form. Healing-only mixtures and fixed stock StatusTonic items do not qualify. Tempering costs item maximum HP, has a two-use limit, and beneficial/unsupported status strings can make it ineffective or counterproductive.
- Grown crop items are included under the kind of use they actually have, so botanical weapons, clothing, lights and gas pods also appear below. Seeds need their configured ground and moisture. Growth reconciles time spent away; ripe seed harvest trades produce for renewable seed supply.

Shared action evidence: [trade gates](../Assets/Scripts/Gameplay/Economy/TradeSystem.cs#L364), [handling gates](../Assets/Scripts/Gameplay/Items/HandlingService.cs#L34), [throw execution](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176), [tempering](../Assets/Scripts/Gameplay/Weaponcraft/WeaponTemperingService.cs#L47), [brewing command](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/BrewReagentsCommand.cs#L109). Item-specific evidence follows each entry.

## Weapons

### dagger — `Dagger`

**Where it comes from:** Loot/shop tables: MorrowfastMenderStock, WeaponsmithStock, FindWeaponT1, CrateT1, BoneCacheT2, BoneCacheT3, WeaponRackT1, MerchantStock; Standard new-game loadout; random friendly-villager trade; Gleaners cellar; Actual equipped NPC loadouts/death recovery (some chance/pick gated): MarlbackScrabbler, MarlbackGleaner, MarlbackPatchbearer, Tinker, RuinScavenger, RuneCultist, SootGremlin, DirtGnome; NPC carried, not proven wielded: TentRightHost, PeatCutter

**What it actually does:** Melee/throw: 1d4 damage per penetration; penetration +1; hit +0; Strength penetration cap 3; attributes Piercing; slots Hand.

**Limits and gaps:** Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. Weight 4. Item HP 5; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 10; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:2525](../Assets/Resources/Content/Blueprints/Objects.json#L2525); [LootTables.json:4](../Assets/Resources/Content/Data/Loot/LootTables.json#L4); [LootTables.json:596](../Assets/Resources/Content/Data/Loot/LootTables.json#L596); [LootTables.json:1008](../Assets/Resources/Content/Data/Loot/LootTables.json#L1008); [LootTables.json:1492](../Assets/Resources/Content/Data/Loot/LootTables.json#L1492); [LootTables.json:1859](../Assets/Resources/Content/Data/Loot/LootTables.json#L1859); [LootTables.json:1892](../Assets/Resources/Content/Data/Loot/LootTables.json#L1892); [LootTables.json:2358](../Assets/Resources/Content/Data/Loot/LootTables.json#L2358); [LootTables.json:2778](../Assets/Resources/Content/Data/Loot/LootTables.json#L2778); [LoadoutPart.cs:99](../Assets/Scripts/Gameplay/Entities/LoadoutPart.cs#L99); [Objects.json:2868](../Assets/Resources/Content/Blueprints/Objects.json#L2868); [Objects.json:3539](../Assets/Resources/Content/Blueprints/Objects.json#L3539); [Objects.json:3653](../Assets/Resources/Content/Blueprints/Objects.json#L3653); [Objects.json:12470](../Assets/Resources/Content/Blueprints/Objects.json#L12470); [Objects.json:17639](../Assets/Resources/Content/Blueprints/Objects.json#L17639); [Objects.json:27779](../Assets/Resources/Content/Blueprints/Objects.json#L27779); [Objects.json:38713](../Assets/Resources/Content/Blueprints/Objects.json#L38713); [Objects.json:38834](../Assets/Resources/Content/Blueprints/Objects.json#L38834); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### forged weapon — `ForgedWeapon`

**Where it comes from:** Consume one Blade/head + one Haft + one Binding; single assembly anywhere, batch needs nearby forge

**What it actually does:** Assembled weapon: head determines damage dice; all three components sum penetration/hit bonuses, maximum component Strength cap wins; attributes and on-hit specs combine. Reforge at forge swaps a component and returns the old one; removes tempering. Salvage at forge returns recorded head + haft and destroys binding/upgrades.

**Limits and gaps:** Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 6. Item HP 8; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 25; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:4228](../Assets/Resources/Content/Blueprints/Objects.json#L4228); [ForgeWeaponCommand.cs:92](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/ForgeWeaponCommand.cs#L92); [WeaponForgingService.cs:333](../Assets/Scripts/Gameplay/Weaponcraft/WeaponForgingService.cs#L333); [WeaponSalvagePart.cs:21](../Assets/Scripts/Gameplay/Preparation/WeaponSalvagePart.cs#L21); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### long sword — `LongSword`

**Where it comes from:** Loot/shop tables: LairLoot, WarbandLootT2, WeaponsmithStock, FindWeaponT2, WeaponRackT2, QuartermasterStock; Actual equipped NPC loadouts/death recovery (some chance/pick gated): MarlbackWallkeeper, Warden

**What it actually does:** Melee/throw: 1d8 damage per penetration; penetration +2; hit +0; Strength penetration cap uncapped; attributes Cutting LongBlades; slots Hand.

**Limits and gaps:** Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. Weight 8. Item HP 10; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 25; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:8561](../Assets/Resources/Content/Blueprints/Objects.json#L8561); [LootTables.json:104](../Assets/Resources/Content/Data/Loot/LootTables.json#L104); [LootTables.json:312](../Assets/Resources/Content/Data/Loot/LootTables.json#L312); [LootTables.json:596](../Assets/Resources/Content/Data/Loot/LootTables.json#L596); [LootTables.json:1038](../Assets/Resources/Content/Data/Loot/LootTables.json#L1038); [LootTables.json:2378](../Assets/Resources/Content/Data/Loot/LootTables.json#L2378); [LootTables.json:2822](../Assets/Resources/Content/Data/Loot/LootTables.json#L2822); [LoadoutPart.cs:99](../Assets/Scripts/Gameplay/Entities/LoadoutPart.cs#L99); [Objects.json:18219](../Assets/Resources/Content/Blueprints/Objects.json#L18219); [Objects.json:21071](../Assets/Resources/Content/Blueprints/Objects.json#L21071); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### battleaxe — `Battleaxe`

**Where it comes from:** Loot/shop tables: WeaponsmithStock, FindWeaponT2, WeaponRackT2

**What it actually does:** Melee/throw: 2d6 damage per penetration; penetration +3; hit +0; Strength penetration cap uncapped; attributes Cutting Axe; slots Hand,Hand.

**Limits and gaps:** Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 14. Item HP 15; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 40; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:8714](../Assets/Resources/Content/Blueprints/Objects.json#L8714); [LootTables.json:596](../Assets/Resources/Content/Data/Loot/LootTables.json#L596); [LootTables.json:1038](../Assets/Resources/Content/Data/Loot/LootTables.json#L1038); [LootTables.json:2378](../Assets/Resources/Content/Data/Loot/LootTables.json#L2378); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### greatsword — `Greatsword`

**Where it comes from:** Loot/shop tables: FindWeaponT3, WeaponRackT3

**What it actually does:** Melee/throw: 1d12 damage per penetration; penetration +4; hit +0; Strength penetration cap uncapped; attributes Cutting LongBlades; slots Hand,Hand.

**Limits and gaps:** Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 12. Item HP 12; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 50; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:8893](../Assets/Resources/Content/Blueprints/Objects.json#L8893); [LootTables.json:1084](../Assets/Resources/Content/Data/Loot/LootTables.json#L1084); [LootTables.json:2402](../Assets/Resources/Content/Data/Loot/LootTables.json#L2402); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### short sword — `ShortSword`

**Where it comes from:** Loot/shop tables: BanditCacheT2, WeaponsmithStock, FindWeaponT1, WeaponRackT1; Actual equipped NPC loadouts/death recovery (some chance/pick gated): MarlbackGleaner, MarlbackPatchbearer, Merchant, DesertBandit, AmbushBandit, SpreadHurdleCutter

**What it actually does:** Melee/throw: 1d6 damage per penetration; penetration +1; hit +0; Strength penetration cap uncapped; attributes Cutting LongBlades; slots Hand.

**Limits and gaps:** Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. Weight 5. Item HP 8; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 15; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:9234](../Assets/Resources/Content/Blueprints/Objects.json#L9234); [LootTables.json:180](../Assets/Resources/Content/Data/Loot/LootTables.json#L180); [LootTables.json:596](../Assets/Resources/Content/Data/Loot/LootTables.json#L596); [LootTables.json:1008](../Assets/Resources/Content/Data/Loot/LootTables.json#L1008); [LootTables.json:2358](../Assets/Resources/Content/Data/Loot/LootTables.json#L2358); [LoadoutPart.cs:99](../Assets/Scripts/Gameplay/Entities/LoadoutPart.cs#L99); [Objects.json:3539](../Assets/Resources/Content/Blueprints/Objects.json#L3539); [Objects.json:3653](../Assets/Resources/Content/Blueprints/Objects.json#L3653); [Objects.json:12628](../Assets/Resources/Content/Blueprints/Objects.json#L12628); [Objects.json:17022](../Assets/Resources/Content/Blueprints/Objects.json#L17022); [Objects.json:27181](../Assets/Resources/Content/Blueprints/Objects.json#L27181); [Objects.json:39544](../Assets/Resources/Content/Blueprints/Objects.json#L39544); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### mace — `Mace`

**Where it comes from:** Loot/shop tables: WeaponsmithStock, FindWeaponT2, WeaponRackT2

**What it actually does:** Melee/throw: 1d8+1 damage per penetration; penetration +3; hit +0; Strength penetration cap uncapped; attributes Bludgeoning Cudgel; slots Hand.

**Limits and gaps:** Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 10. Item HP 15; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 20; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:9365](../Assets/Resources/Content/Blueprints/Objects.json#L9365); [LootTables.json:596](../Assets/Resources/Content/Data/Loot/LootTables.json#L596); [LootTables.json:1038](../Assets/Resources/Content/Data/Loot/LootTables.json#L1038); [LootTables.json:2378](../Assets/Resources/Content/Data/Loot/LootTables.json#L2378); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### spear — `Spear`

**Where it comes from:** Loot/shop tables: MorrowfastMenderStock, WeaponsmithStock, FindWeaponT1, WeaponRackT2, WardenStock; Actual equipped NPC loadouts/death recovery (some chance/pick gated): MarlbackTunnelguard, Quartermaster, Morrowfast guards (runtime authored loadout)

**What it actually does:** Melee/throw: 1d6+1 damage per penetration; penetration +2; hit +0; Strength penetration cap uncapped; attributes Piercing; slots Hand.

**Limits and gaps:** Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. Weight 7. Item HP 8; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 18; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:9518](../Assets/Resources/Content/Blueprints/Objects.json#L9518); [LootTables.json:4](../Assets/Resources/Content/Data/Loot/LootTables.json#L4); [LootTables.json:596](../Assets/Resources/Content/Data/Loot/LootTables.json#L596); [LootTables.json:1008](../Assets/Resources/Content/Data/Loot/LootTables.json#L1008); [LootTables.json:2378](../Assets/Resources/Content/Data/Loot/LootTables.json#L2378); [LootTables.json:2729](../Assets/Resources/Content/Data/Loot/LootTables.json#L2729); [LoadoutPart.cs:99](../Assets/Scripts/Gameplay/Entities/LoadoutPart.cs#L99); [Objects.json:3690](../Assets/Resources/Content/Blueprints/Objects.json#L3690); [Objects.json:12769](../Assets/Resources/Content/Blueprints/Objects.json#L12769); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### hatchet — `Hatchet`

**Where it comes from:** Loot/shop tables: WeaponsmithStock, FindWeaponT1, WeaponRackT1; Also loose Spread goods (collector may carry them); Actual equipped NPC loadouts/death recovery (some chance/pick gated): MarlbackScrabbler

**What it actually does:** Melee/throw: 1d6 damage per penetration; penetration +2; hit +0; Strength penetration cap uncapped; attributes Cutting Axe; slots Hand.

**Limits and gaps:** Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. Weight 6. Item HP 8; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 12; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:9649](../Assets/Resources/Content/Blueprints/Objects.json#L9649); [LootTables.json:596](../Assets/Resources/Content/Data/Loot/LootTables.json#L596); [LootTables.json:1008](../Assets/Resources/Content/Data/Loot/LootTables.json#L1008); [LootTables.json:2358](../Assets/Resources/Content/Data/Loot/LootTables.json#L2358); [LoadoutPart.cs:99](../Assets/Scripts/Gameplay/Entities/LoadoutPart.cs#L99); [Objects.json:2868](../Assets/Resources/Content/Blueprints/Objects.json#L2868); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### claymore — `Claymore`

**Where it comes from:** Loot/shop tables: FindWeaponT3, WeaponRackT3

**What it actually does:** Melee/throw: 2d8 damage per penetration; penetration +4; hit +0; Strength penetration cap uncapped; attributes Cutting LongBlades; slots Hand,Hand.

**Limits and gaps:** Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 14. Item HP 15; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 65; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:9738](../Assets/Resources/Content/Blueprints/Objects.json#L9738); [LootTables.json:1084](../Assets/Resources/Content/Data/Loot/LootTables.json#L1084); [LootTables.json:2402](../Assets/Resources/Content/Data/Loot/LootTables.json#L2402); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### cudgel — `Cudgel`

**Where it comes from:** Loot/shop tables: FindWeaponT1; Also loose Spread goods (collector may carry them); Actual equipped NPC loadouts/death recovery (some chance/pick gated): MarlbackScrabbler, MarlbackStormbinder, RuinScavenger, SpreadDitchMate, MarlbackCindercaller, MarlbackSoursprayer

**What it actually does:** Melee/throw: 1d4+2 damage per penetration; penetration +2; hit +0; Strength penetration cap uncapped; attributes Bludgeoning Cudgel; slots Hand.

**Limits and gaps:** Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. Weight 8. Item HP 12; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 8; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:9853](../Assets/Resources/Content/Blueprints/Objects.json#L9853); [LootTables.json:1008](../Assets/Resources/Content/Data/Loot/LootTables.json#L1008); [LoadoutPart.cs:99](../Assets/Scripts/Gameplay/Entities/LoadoutPart.cs#L99); [Objects.json:2868](../Assets/Resources/Content/Blueprints/Objects.json#L2868); [Objects.json:491](../Assets/Resources/Content/Blueprints/Objects.json#L491); [Objects.json:17639](../Assets/Resources/Content/Blueprints/Objects.json#L17639); [Objects.json:39619](../Assets/Resources/Content/Blueprints/Objects.json#L39619); [Objects.json:40458](../Assets/Resources/Content/Blueprints/Objects.json#L40458); [Objects.json:40561](../Assets/Resources/Content/Blueprints/Objects.json#L40561); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### loaner dagger — `LoanerDagger`

**Where it comes from:** Village quartermaster rental rack/conversation; finite physical weapon, priced in Ink

**What it actually does:** Melee/throw: 1d4 damage per penetration; penetration +1; hit +0; Strength penetration cap uncapped; attributes Piercing; slots Hand.

**Limits and gaps:** Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. Rental costs ceil(current buy price×.25) Ink; return gives floor(InkPaid×.5); rented item cannot be sold. Exchange rental desk permits permanent buyout in drams; no expiry timer in RentalPart. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 3. Item HP 5; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 30; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:12914](../Assets/Resources/Content/Blueprints/Objects.json#L12914); [VillagePopulationBuilder.cs:855](../Assets/Scripts/Gameplay/World/Generation/Builders/VillagePopulationBuilder.cs#L855); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533); [RentalSystem.cs:92](../Assets/Scripts/Gameplay/Economy/RentalSystem.cs#L92); [RentalPart.cs:37](../Assets/Scripts/Gameplay/Economy/RentalPart.cs#L37); [SecondExplorationServices.cs:70](../Assets/Scripts/Gameplay/Exploration/SecondExplorationServices.cs#L70).

### loaner spear — `LoanerSpear`

**Where it comes from:** Village quartermaster rental rack/conversation; finite physical weapon, priced in Ink

**What it actually does:** Melee/throw: 1d6+1 damage per penetration; penetration +2; hit +0; Strength penetration cap uncapped; attributes Piercing; slots Hand.

**Limits and gaps:** Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. Rental costs ceil(current buy price×.25) Ink; return gives floor(InkPaid×.5); rented item cannot be sold. Exchange rental desk permits permanent buyout in drams; no expiry timer in RentalPart. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 5. Item HP 8; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 55; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:13007](../Assets/Resources/Content/Blueprints/Objects.json#L13007); [VillagePopulationBuilder.cs:856](../Assets/Scripts/Gameplay/World/Generation/Builders/VillagePopulationBuilder.cs#L856); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533); [RentalSystem.cs:92](../Assets/Scripts/Gameplay/Economy/RentalSystem.cs#L92); [RentalPart.cs:37](../Assets/Scripts/Gameplay/Economy/RentalPart.cs#L37); [SecondExplorationServices.cs:70](../Assets/Scripts/Gameplay/Exploration/SecondExplorationServices.cs#L70).

### loaner longsword — `LoanerLongsword`

**Where it comes from:** Village quartermaster rental rack/conversation; finite physical weapon, priced in Ink

**What it actually does:** Melee/throw: 1d8+1 damage per penetration; penetration +3; hit +0; Strength penetration cap uncapped; attributes Cutting LongBlades; slots Hand.

**Limits and gaps:** Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. Rental costs ceil(current buy price×.25) Ink; return gives floor(InkPaid×.5); rented item cannot be sold. Exchange rental desk permits permanent buyout in drams; no expiry timer in RentalPart. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 7. Item HP 12; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 120; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:13100](../Assets/Resources/Content/Blueprints/Objects.json#L13100); [VillagePopulationBuilder.cs:857](../Assets/Scripts/Gameplay/World/Generation/Builders/VillagePopulationBuilder.cs#L857); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533); [RentalSystem.cs:92](../Assets/Scripts/Gameplay/Economy/RentalSystem.cs#L92); [RentalPart.cs:37](../Assets/Scripts/Gameplay/Economy/RentalPart.cs#L37); [SecondExplorationServices.cs:70](../Assets/Scripts/Gameplay/Exploration/SecondExplorationServices.cs#L70).

### warhammer — `Warhammer`

**Where it comes from:** Loot/shop tables: WeaponsmithStock, FindWeaponT2, FindWeaponT3, WeaponRackT3; Also Bloom Front weapon placement

**What it actually does:** Melee/throw: 2d4+1 damage per penetration; penetration +5; hit +0; Strength penetration cap uncapped; attributes Bludgeoning Cudgel; slots Hand,Hand.

**Limits and gaps:** Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 16. Item HP 20; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 45; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:13193](../Assets/Resources/Content/Blueprints/Objects.json#L13193); [LootTables.json:596](../Assets/Resources/Content/Data/Loot/LootTables.json#L596); [LootTables.json:1038](../Assets/Resources/Content/Data/Loot/LootTables.json#L1038); [LootTables.json:1084](../Assets/Resources/Content/Data/Loot/LootTables.json#L1084); [LootTables.json:2402](../Assets/Resources/Content/Data/Loot/LootTables.json#L2402); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### choir spine — `ChoirSpine`

**Where it comes from:** Loot/shop tables: ZigguratVaultT2

**What it actually does:** Melee/throw: 1d4+1 damage per penetration; penetration +1; hit +0; Strength penetration cap uncapped; attributes Piercing; slots Hand.

**Limits and gaps:** No special named/narrative power beyond listed stats, class hooks and material properties. Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 3. Item HP 4; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 12; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:13308](../Assets/Resources/Content/Blueprints/Objects.json#L13308); [LootTables.json:384](../Assets/Resources/Content/Data/Loot/LootTables.json#L384); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### old world pipe — `OldWorldPipe`

**Where it comes from:** Loot/shop tables: FindWeaponT1; Actual equipped NPC loadouts/death recovery (some chance/pick gated): RuinScavenger

**What it actually does:** Melee/throw: 1d6 damage per penetration; penetration +2; hit +0; Strength penetration cap uncapped; attributes Bludgeoning Cudgel; slots Hand.

**Limits and gaps:** Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. Weight 9. Item HP 10; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 6; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:13426](../Assets/Resources/Content/Blueprints/Objects.json#L13426); [LootTables.json:1008](../Assets/Resources/Content/Data/Loot/LootTables.json#L1008); [LoadoutPart.cs:99](../Assets/Scripts/Gameplay/Entities/LoadoutPart.cs#L99); [Objects.json:17639](../Assets/Resources/Content/Blueprints/Objects.json#L17639); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### sporeblade — `Sporeblade`

**Where it comes from:** Loot/shop tables: ZigguratVaultT2

**What it actually does:** Melee/throw: 1d8+1 damage per penetration; penetration +3; hit +0; Strength penetration cap uncapped; attributes Cutting LongBlades; slots Hand.

**Limits and gaps:** No special named/narrative power beyond listed stats, class hooks and material properties. Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 7. Item HP 8; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 35; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:13548](../Assets/Resources/Content/Blueprints/Objects.json#L13548); [LootTables.json:384](../Assets/Resources/Content/Data/Loot/LootTables.json#L384); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### flaming sword — `FlamingSword`

**Where it comes from:** Loot/shop tables: FindWeaponT2

**What it actually does:** Melee/throw: 1d8 damage per penetration; penetration +2; hit +0; Strength penetration cap uncapped; attributes Cutting Fire LongBlades; slots Hand. 30% Burning intensity 1 on positive hit; light radius 4. Non-fuel victims ordinarily burn 3 turns (encoded 5 is ignored).

**Limits and gaps:** Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 6. Item HP 8; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 40; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:13674](../Assets/Resources/Content/Blueprints/Objects.json#L13674); [LootTables.json:1038](../Assets/Resources/Content/Data/Loot/LootTables.json#L1038); [OnHitEffectFactory.cs:29](../Assets/Scripts/Gameplay/Items/OnHitEffectFactory.cs#L29); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### ice sword — `IceSword`

**Where it comes from:** Loot/shop tables: FindWeaponT2

**What it actually does:** Melee/throw: 1d8 damage per penetration; penetration +2; hit +0; Strength penetration cap uncapped; attributes Cutting Ice LongBlades; slots Hand. 30% Frozen cold 1 on positive hit (action blocked until thaw; encoded 3 is ignored); light radius 4.

**Limits and gaps:** Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 6. Item HP 8; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 40; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:13817](../Assets/Resources/Content/Blueprints/Objects.json#L13817); [LootTables.json:1038](../Assets/Resources/Content/Data/Loot/LootTables.json#L1038); [OnHitEffectFactory.cs:29](../Assets/Scripts/Gameplay/Items/OnHitEffectFactory.cs#L29); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### cryolance — `CryoLance`

**Where it comes from:** Loot/shop tables: FindWeaponT2

**What it actually does:** Melee/throw: 1d6+2 damage per penetration; penetration +3; hit +0; Strength penetration cap uncapped; attributes Piercing Ice LongBlades; slots Hand. 30% Frozen cold 1 on positive hit (action blocked until thaw; encoded 3 is ignored).

**Limits and gaps:** Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 5. Item HP 9; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 45; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:13960](../Assets/Resources/Content/Blueprints/Objects.json#L13960); [LootTables.json:1038](../Assets/Resources/Content/Data/Loot/LootTables.json#L1038); [OnHitEffectFactory.cs:29](../Assets/Scripts/Gameplay/Items/OnHitEffectFactory.cs#L29); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### ember spear — `EmberSpear`

**Where it comes from:** Loot/shop tables: FindWeaponT2

**What it actually does:** Melee/throw: 1d6+1 damage per penetration; penetration +2; hit +0; Strength penetration cap uncapped; attributes Piercing Fire; slots Hand. 30% Burning intensity 1 on positive hit; non-fuel victims ordinarily burn 3 turns (encoded 5 is ignored).

**Limits and gaps:** Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 6. Item HP 8; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 35; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:14086](../Assets/Resources/Content/Blueprints/Objects.json#L14086); [LootTables.json:1038](../Assets/Resources/Content/Data/Loot/LootTables.json#L1038); [OnHitEffectFactory.cs:29](../Assets/Scripts/Gameplay/Items/OnHitEffectFactory.cs#L29); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### acidic dagger — `AcidicDagger`

**Where it comes from:** Loot/shop tables: FindWeaponT2

**What it actually does:** Melee/throw: 1d4+1 damage per penetration; penetration +1; hit +0; Strength penetration cap 3; attributes Piercing Acid; slots Hand. 30% Acidic corrosion 1 on positive hit (acid damage and material degradation each turn; corrosion loses .05/turn; encoded duration 5 ignored).

**Limits and gaps:** Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 4. Item HP 5; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 30; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:14212](../Assets/Resources/Content/Blueprints/Objects.json#L14212); [LootTables.json:1038](../Assets/Resources/Content/Data/Loot/LootTables.json#L1038); [OnHitEffectFactory.cs:29](../Assets/Scripts/Gameplay/Items/OnHitEffectFactory.cs#L29); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### poison dagger — `VenomDagger`

**Where it comes from:** Loot/shop tables: HunterCacheT1, TombVaultT2

**What it actually does:** Melee/throw: 1d4+1 damage per penetration; penetration +1; hit +0; Strength penetration cap 3; attributes Piercing Poison; slots Hand. 50% Poisoned for 6 turns, 1d4 per tick on positive hit.

**Limits and gaps:** Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 4. Item HP 5; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 30; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:14342](../Assets/Resources/Content/Blueprints/Objects.json#L14342); [LootTables.json:214](../Assets/Resources/Content/Data/Loot/LootTables.json#L214); [LootTables.json:344](../Assets/Resources/Content/Data/Loot/LootTables.json#L344); [OnHitEffectFactory.cs:29](../Assets/Scripts/Gameplay/Items/OnHitEffectFactory.cs#L29); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### thunder hammer — `ThunderHammer`

**Where it comes from:** Loot/shop tables: FindWeaponT2

**What it actually does:** Melee/throw: 1d8+1 damage per penetration; penetration +3; hit +0; Strength penetration cap uncapped; attributes Bludgeoning Lightning Cudgel; slots Hand. 30% Electrified charge 1 on positive hit (normally 2 turns, 2 Lightning damage/tick; wetness amplifies; encoded duration 3 ignored); light radius 3.

**Limits and gaps:** Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 12. Item HP 10; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 50; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:14472](../Assets/Resources/Content/Blueprints/Objects.json#L14472); [LootTables.json:1038](../Assets/Resources/Content/Data/Loot/LootTables.json#L1038); [OnHitEffectFactory.cs:29](../Assets/Scripts/Gameplay/Items/OnHitEffectFactory.cs#L29); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### temporal shard — `TemporalShard`

**Where it comes from:** One-time Palimpsest conversation gift after witnessing memory, choosing heartbreak and accepting; PalimpsestGaveGift guard

**What it actually does:** Melee/throw: 1d6+2 damage per penetration; penetration +2; hit +2; Strength penetration cap uncapped; attributes Piercing; slots Hand.

**Limits and gaps:** No special named/narrative power beyond listed stats, class hooks and material properties. Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 5. Item HP 5; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 40; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:14741](../Assets/Resources/Content/Blueprints/Objects.json#L14741); [Palimpsest.json:261](../Assets/Resources/Content/Conversations/Palimpsest.json#L261); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### severance edge — `SeveranceEdge`

**Where it comes from:** Loot/shop tables: SealedVaultT3

**What it actually does:** Melee/throw: 1d8 damage per penetration; penetration +3; hit +0; Strength penetration cap uncapped; attributes Cutting LongBlades; slots Hand.

**Limits and gaps:** No special named/narrative power beyond listed stats, class hooks and material properties. Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 8. Item HP 14; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 38; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:14867](../Assets/Resources/Content/Blueprints/Objects.json#L14867); [LootTables.json:424](../Assets/Resources/Content/Data/Loot/LootTables.json#L424); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### glassblown stiletto — `GlassblownStiletto`

**Where it comes from:** Loot/shop tables: DrifterStock

**What it actually does:** Melee/throw: 1d4+2 damage per penetration; penetration +2; hit +3; Strength penetration cap 2; attributes Piercing; slots Hand.

**Limits and gaps:** No special named/narrative power beyond listed stats, class hooks and material properties. Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 2. Item HP 3; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 28; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:14951](../Assets/Resources/Content/Blueprints/Objects.json#L14951); [LootTables.json:2988](../Assets/Resources/Content/Data/Loot/LootTables.json#L2988); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### dissolution maul — `DissolutionMaul`

**Where it comes from:** Loot/shop tables: FindWeaponT3, WeaponRackT3

**What it actually does:** Melee/throw: 2d6+2 damage per penetration; penetration +5; hit +0; Strength penetration cap uncapped; attributes Bludgeoning Cudgel Acid; slots Hand,Hand. 40% Acidic on positive hit; declared corrosion 1.5 is clamped to 1, so same corrosion strength as acidic dagger; two hands.

**Limits and gaps:** Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 18. Item HP 20; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 55; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:15081](../Assets/Resources/Content/Blueprints/Objects.json#L15081); [LootTables.json:1084](../Assets/Resources/Content/Data/Loot/LootTables.json#L1084); [LootTables.json:2402](../Assets/Resources/Content/Data/Loot/LootTables.json#L2402); [OnHitEffectFactory.cs:29](../Assets/Scripts/Gameplay/Items/OnHitEffectFactory.cs#L29); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### first root glaive — `FirstRootGlaive`

**Where it comes from:** Loot/shop tables: ZigguratVaultT2

**What it actually does:** Melee/throw: 2d6+1 damage per penetration; penetration +4; hit +0; Strength penetration cap uncapped; attributes Cutting Glaive; slots Hand,Hand.

**Limits and gaps:** No special named/narrative power beyond listed stats, class hooks and material properties. Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 11. Item HP 15; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 60; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:15237](../Assets/Resources/Content/Blueprints/Objects.json#L15237); [LootTables.json:384](../Assets/Resources/Content/Data/Loot/LootTables.json#L384); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### palimpsest blade — `PalimpsestBlade`

**Where it comes from:** Loot/shop tables: SealedVaultT3

**What it actually does:** Melee/throw: 1d10+1 damage per penetration; penetration +4; hit +1; Strength penetration cap uncapped; attributes Cutting LongBlades; slots Hand.

**Limits and gaps:** No special named/narrative power beyond listed stats, class hooks and material properties. Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 6. Item HP 8; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 70; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:15381](../Assets/Resources/Content/Blueprints/Objects.json#L15381); [LootTables.json:424](../Assets/Resources/Content/Data/Loot/LootTables.json#L424); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### wedge-cleaver — `BreacherCleaver`

**Where it comes from:** Loot/shop tables: WarbandLootT2; Actual equipped NPC loadouts/death recovery (some chance/pick gated): MarlbackBreacher

**What it actually does:** Melee/throw: 1d10 damage per penetration; penetration +2; hit +0; Strength penetration cap uncapped; attributes Cutting Axe; slots Hand.

**Limits and gaps:** Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. Weight 8.

**Base affordances:** Base Commerce value 35; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:18574](../Assets/Resources/Content/Blueprints/Objects.json#L18574); [LootTables.json:312](../Assets/Resources/Content/Data/Loot/LootTables.json#L312); [LoadoutPart.cs:99](../Assets/Scripts/Gameplay/Entities/LoadoutPart.cs#L99); [Objects.json:18345](../Assets/Resources/Content/Blueprints/Objects.json#L18345); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### tempered long sword — `TemperedLongSword`

**Where it comes from:** Loot/shop tables: FindDeepEquipmentT4; May receive one random tier-1 enhancement when drawn into native DeepReliquaryT4 (25% opportunity)

**What it actually does:** Melee/throw: 1d10+1 damage per penetration; penetration +4; hit +0; Strength penetration cap uncapped; attributes Cutting LongBlades; slots Hand.

**Limits and gaps:** Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 9. Item HP 10; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 90; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:38955](../Assets/Resources/Content/Blueprints/Objects.json#L38955); [LootTables.json:3100](../Assets/Resources/Content/Data/Loot/LootTables.json#L3100); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### counterweight maul — `CounterweightMaul`

**Where it comes from:** Loot/shop tables: FindDeepEquipmentT4; May receive one random tier-1 enhancement when drawn into native DeepReliquaryT4 (25% opportunity)

**What it actually does:** Melee/throw: 2d6+2 damage per penetration; penetration +6; hit +0; Strength penetration cap uncapped; attributes Bludgeoning Cudgel; slots Hand,Hand.

**Limits and gaps:** Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 22. Item HP 20; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 110; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:39021](../Assets/Resources/Content/Blueprints/Objects.json#L39021); [LootTables.json:3100](../Assets/Resources/Content/Data/Loot/LootTables.json#L3100); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### blunt salt rake — `CurationSaltRake`

**Where it comes from:** Marrowstye receiving hall locked tool cabinet (CurationCounterfoil key opens intake-tools lock)

**What it actually does:** Melee/throw: 1d4+2 damage per penetration; penetration +2; hit +0; Strength penetration cap uncapped; attributes Bludgeoning Cudgel; slots Hand.

**Limits and gaps:** No special named/narrative power beyond listed stats, class hooks and material properties. Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 6. Item HP 12; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 8; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:41658](../Assets/Resources/Content/Blueprints/Objects.json#L41658); [CurationReceivingBuilder.cs:99](../Assets/Scripts/Gameplay/World/Generation/Builders/CurationReceivingBuilder.cs#L99); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

## Armor and clothing

### filter hood — `FilterHood`

**Where it comes from:** Sodden works locker finite stock added by SecondExplorationSites

**What it actually does:** Equip in Head: AV +0, DV +0, Speed penalty 0. Any worn head covering stops Beating Height glare exposure accumulation. Worn mask reduces respiratory intake by 50 and Gas-tagged damage by 10% (integer-rounded); partial protection, not immunity.

**Limits and gaps:** AV applies to the selected hit slot containing this equipment; DV contributes to overall dodge. Requires matching available body slot. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 2. Item HP 10; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 35; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:1063](../Assets/Resources/Content/Blueprints/Objects.json#L1063); [SecondExplorationSites.cs:111](../Assets/Scripts/Gameplay/Exploration/SecondExplorationSites.cs#L111); [BeatingGlareSystem.cs:82](../Assets/Scripts/Gameplay/World/BeatingGlareSystem.cs#L82); [GasMaskPart.cs:47](../Assets/Scripts/Gameplay/Materials/GasMaskPart.cs#L47); [InventoryPart.cs:591](../Assets/Scripts/Gameplay/Inventory/InventoryPart.cs#L591); [CombatSystem.cs:1597](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L1597); [CombatSystem.cs:695](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L695); [EquipBonusUtility.cs:53](../Assets/Scripts/Gameplay/Inventory/Commands/Equipment/EquipBonusUtility.cs#L53).

### acidworker apron — `AcidworkerApron`

**Where it comes from:** Sodden works locker finite stock added by SecondExplorationSites

**What it actually does:** Equip in Body: AV +1, DV -1, Speed penalty 5; AcidResistance:50 while equipped.

**Limits and gaps:** AV applies to the selected hit slot containing this equipment; DV contributes to overall dodge. Requires matching available body slot. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 12. Item HP 15; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 35; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:1229](../Assets/Resources/Content/Blueprints/Objects.json#L1229); [SecondExplorationSites.cs:111](../Assets/Scripts/Gameplay/Exploration/SecondExplorationSites.cs#L111); [CombatSystem.cs:1597](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L1597); [CombatSystem.cs:695](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L695); [EquipBonusUtility.cs:53](../Assets/Scripts/Gameplay/Inventory/Commands/Equipment/EquipBonusUtility.cs#L53).

### coldward cloak — `ColdwardCloak`

**Where it comes from:** Last Counter finite supply chest

**What it actually does:** Equip in Back: AV +0, DV -1, Speed penalty 0; ColdResistance:50 while equipped.

**Limits and gaps:** AV applies to the selected hit slot containing this equipment; DV contributes to overall dodge. Requires matching available body slot. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 4. Item HP 15; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 35; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:1386](../Assets/Resources/Content/Blueprints/Objects.json#L1386); [LastCounterCompositionBuilder.cs:168](../Assets/Scripts/Gameplay/World/Generation/Builders/LastCounterCompositionBuilder.cs#L168); [CombatSystem.cs:1597](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L1597); [CombatSystem.cs:695](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L695); [EquipBonusUtility.cs:53](../Assets/Scripts/Gameplay/Inventory/Commands/Equipment/EquipBonusUtility.cs#L53).

### kilnfelt apron — `KilnfeltApron`

**Where it comes from:** Cinderhold weaponsmith finite regional opening shelf

**What it actually does:** Equip in Body: AV +1, DV +0, Speed penalty 5; HeatResistance:50 while equipped.

**Limits and gaps:** AV applies to the selected hit slot containing this equipment; DV contributes to overall dodge. Requires matching available body slot. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 12. Item HP 15; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 45; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:4659](../Assets/Resources/Content/Blueprints/Objects.json#L4659); [CinderholdCompositionBuilder.cs:196](../Assets/Scripts/Gameplay/World/Generation/Builders/CinderholdCompositionBuilder.cs#L196); [CombatSystem.cs:1597](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L1597); [CombatSystem.cs:695](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L695); [EquipBonusUtility.cs:53](../Assets/Scripts/Gameplay/Inventory/Commands/Equipment/EquipBonusUtility.cs#L53).

### groundwire screen — `GroundwireScreen`

**Where it comes from:** Sodden works locker finite stock

**What it actually does:** Equip in Hand: AV +0, DV +0, Speed penalty 0; ElectricResistance:50 while equipped.

**Limits and gaps:** AV applies to the selected hit slot containing this equipment; DV contributes to overall dodge. Requires matching available body slot. Uses a hand; no separate block/parry action or ShieldPart found. Armor/DV/resistance are the implemented protection. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 8. Item HP 12; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 35; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:4778](../Assets/Resources/Content/Blueprints/Objects.json#L4778); [SoddenDistrictBuilder.cs:99](../Assets/Scripts/Gameplay/World/Generation/Builders/SoddenDistrictBuilder.cs#L99); [CombatSystem.cs:1597](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L1597); [CombatSystem.cs:695](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L695); [EquipBonusUtility.cs:53](../Assets/Scripts/Gameplay/Inventory/Commands/Equipment/EquipBonusUtility.cs#L53).

### leather armor — `LeatherArmor`

**Where it comes from:** Loot/shop tables: MorrowfastMenderStock, LairLoot, ArmorerStock, FindArmorT1, CrateT2, StrongBoxT1, MerchantStock; Actual equipped NPC loadouts/death recovery (some chance/pick gated): MarlbackGleaner, MarlbackPatchbearer, Quartermaster, MarlbackWallkeeper, MarlbackBreacher, Warden, AmbushBandit, Morrowfast guards (runtime authored loadout)

**What it actually does:** Equip in Body: AV +3, DV -1, Speed penalty 0.

**Limits and gaps:** AV applies to the selected hit slot containing this equipment; DV contributes to overall dodge. Requires matching available body slot. Weight 15. Item HP 15; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 30; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:7242](../Assets/Resources/Content/Blueprints/Objects.json#L7242); [LootTables.json:4](../Assets/Resources/Content/Data/Loot/LootTables.json#L4); [LootTables.json:104](../Assets/Resources/Content/Data/Loot/LootTables.json#L104); [LootTables.json:654](../Assets/Resources/Content/Data/Loot/LootTables.json#L654); [LootTables.json:1106](../Assets/Resources/Content/Data/Loot/LootTables.json#L1106); [LootTables.json:1527](../Assets/Resources/Content/Data/Loot/LootTables.json#L1527); [LootTables.json:2041](../Assets/Resources/Content/Data/Loot/LootTables.json#L2041); [LootTables.json:2778](../Assets/Resources/Content/Data/Loot/LootTables.json#L2778); [LoadoutPart.cs:99](../Assets/Scripts/Gameplay/Entities/LoadoutPart.cs#L99); [Objects.json:3539](../Assets/Resources/Content/Blueprints/Objects.json#L3539); [Objects.json:3653](../Assets/Resources/Content/Blueprints/Objects.json#L3653); [Objects.json:12769](../Assets/Resources/Content/Blueprints/Objects.json#L12769); [Objects.json:18219](../Assets/Resources/Content/Blueprints/Objects.json#L18219); [Objects.json:18345](../Assets/Resources/Content/Blueprints/Objects.json#L18345); [Objects.json:21071](../Assets/Resources/Content/Blueprints/Objects.json#L21071); [Objects.json:27181](../Assets/Resources/Content/Blueprints/Objects.json#L27181); [CombatSystem.cs:1597](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L1597); [CombatSystem.cs:695](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L695); [EquipBonusUtility.cs:53](../Assets/Scripts/Gameplay/Inventory/Commands/Equipment/EquipBonusUtility.cs#L53).

### chain mail — `ChainMail`

**Where it comes from:** Loot/shop tables: LairLoot, WarbandLootT2, ArmorerStock, FindArmorT2, FindArmorT3, CrateT3, StrongBoxT2, QuartermasterStock

**What it actually does:** Equip in Body: AV +5, DV -2, Speed penalty 0.

**Limits and gaps:** AV applies to the selected hit slot containing this equipment; DV contributes to overall dodge. Requires matching available body slot. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 25. Item HP 25; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 60; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:7383](../Assets/Resources/Content/Blueprints/Objects.json#L7383); [LootTables.json:104](../Assets/Resources/Content/Data/Loot/LootTables.json#L104); [LootTables.json:312](../Assets/Resources/Content/Data/Loot/LootTables.json#L312); [LootTables.json:654](../Assets/Resources/Content/Data/Loot/LootTables.json#L654); [LootTables.json:1136](../Assets/Resources/Content/Data/Loot/LootTables.json#L1136); [LootTables.json:1162](../Assets/Resources/Content/Data/Loot/LootTables.json#L1162); [LootTables.json:1570](../Assets/Resources/Content/Data/Loot/LootTables.json#L1570); [LootTables.json:2076](../Assets/Resources/Content/Data/Loot/LootTables.json#L2076); [LootTables.json:2822](../Assets/Resources/Content/Data/Loot/LootTables.json#L2822); [CombatSystem.cs:1597](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L1597); [CombatSystem.cs:695](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L695); [EquipBonusUtility.cs:53](../Assets/Scripts/Gameplay/Inventory/Commands/Equipment/EquipBonusUtility.cs#L53).

### buckler — `Buckler`

**Where it comes from:** Loot/shop tables: ArmorerStock, FindArmorT1, UrnT2, WardenStock, QuartermasterStock; Also Gleaners cellar, Spread wayhouse reward, Sodden works locker

**What it actually does:** Equip in Hand: AV +1, DV +1, Speed penalty 0.

**Limits and gaps:** AV applies to the selected hit slot containing this equipment; DV contributes to overall dodge. Requires matching available body slot. Uses a hand; no separate block/parry action or ShieldPart found. Armor/DV/resistance are the implemented protection. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 5. Item HP 10; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 20; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:9964](../Assets/Resources/Content/Blueprints/Objects.json#L9964); [LootTables.json:654](../Assets/Resources/Content/Data/Loot/LootTables.json#L654); [LootTables.json:1106](../Assets/Resources/Content/Data/Loot/LootTables.json#L1106); [LootTables.json:1701](../Assets/Resources/Content/Data/Loot/LootTables.json#L1701); [LootTables.json:2729](../Assets/Resources/Content/Data/Loot/LootTables.json#L2729); [LootTables.json:2822](../Assets/Resources/Content/Data/Loot/LootTables.json#L2822); [CombatSystem.cs:1597](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L1597); [CombatSystem.cs:695](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L695); [EquipBonusUtility.cs:53](../Assets/Scripts/Gameplay/Inventory/Commands/Equipment/EquipBonusUtility.cs#L53).

### iron helmet — `IronHelmet`

**Where it comes from:** Loot/shop tables: LairLoot, TombVaultT2, ArmorerStock, FindArmorT2, FindArmorT3, WardenStock, QuartermasterStock; Actual equipped NPC loadouts/death recovery (some chance/pick gated): SkeletalSentry, MarlbackBreacher

**What it actually does:** Equip in Head: AV +2, DV +0, Speed penalty 0. Any worn head covering stops Beating Height glare exposure accumulation.

**Limits and gaps:** AV applies to the selected hit slot containing this equipment; DV contributes to overall dodge. Requires matching available body slot. Weight 6. Item HP 12; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 25; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:10058](../Assets/Resources/Content/Blueprints/Objects.json#L10058); [LootTables.json:104](../Assets/Resources/Content/Data/Loot/LootTables.json#L104); [LootTables.json:344](../Assets/Resources/Content/Data/Loot/LootTables.json#L344); [LootTables.json:654](../Assets/Resources/Content/Data/Loot/LootTables.json#L654); [LootTables.json:1136](../Assets/Resources/Content/Data/Loot/LootTables.json#L1136); [LootTables.json:1162](../Assets/Resources/Content/Data/Loot/LootTables.json#L1162); [LootTables.json:2729](../Assets/Resources/Content/Data/Loot/LootTables.json#L2729); [LootTables.json:2822](../Assets/Resources/Content/Data/Loot/LootTables.json#L2822); [LoadoutPart.cs:99](../Assets/Scripts/Gameplay/Entities/LoadoutPart.cs#L99); [Objects.json:17728](../Assets/Resources/Content/Blueprints/Objects.json#L17728); [Objects.json:18345](../Assets/Resources/Content/Blueprints/Objects.json#L18345); [BeatingGlareSystem.cs:82](../Assets/Scripts/Gameplay/World/BeatingGlareSystem.cs#L82); [CombatSystem.cs:1597](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L1597); [CombatSystem.cs:695](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L695); [EquipBonusUtility.cs:53](../Assets/Scripts/Gameplay/Inventory/Commands/Equipment/EquipBonusUtility.cs#L53).

### leather boots — `LeatherBoots`

**Where it comes from:** Loot/shop tables: ArmorerStock, FindArmorT1; Also Sodden works locker; loose Spread goods (collector may carry them); Actual equipped NPC loadouts/death recovery (some chance/pick gated): MarlbackTunnelguard, Elder, Weaponsmith, Merchant, Quartermaster, DesertBandit, MarlbackWallkeeper, Warden, WellKeeper, Farmer, AmbushBandit, TentRightHost, SaltMaster, RecensionScribe, StillleafSearcher, PeatCutter, GantryRegistrar

**What it actually does:** Equip in Feet: AV +1, DV +0, Speed penalty 0.

**Limits and gaps:** AV applies to the selected hit slot containing this equipment; DV contributes to overall dodge. Requires matching available body slot. Weight 3. Item HP 8; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 12; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:10174](../Assets/Resources/Content/Blueprints/Objects.json#L10174); [LootTables.json:654](../Assets/Resources/Content/Data/Loot/LootTables.json#L654); [LootTables.json:1106](../Assets/Resources/Content/Data/Loot/LootTables.json#L1106); [LoadoutPart.cs:99](../Assets/Scripts/Gameplay/Entities/LoadoutPart.cs#L99); [Objects.json:3690](../Assets/Resources/Content/Blueprints/Objects.json#L3690); [Objects.json:9050](../Assets/Resources/Content/Blueprints/Objects.json#L9050); [Objects.json:11462](../Assets/Resources/Content/Blueprints/Objects.json#L11462); [Objects.json:12628](../Assets/Resources/Content/Blueprints/Objects.json#L12628); [Objects.json:12769](../Assets/Resources/Content/Blueprints/Objects.json#L12769); [Objects.json:17022](../Assets/Resources/Content/Blueprints/Objects.json#L17022); [Objects.json:18219](../Assets/Resources/Content/Blueprints/Objects.json#L18219); [Objects.json:21071](../Assets/Resources/Content/Blueprints/Objects.json#L21071); [Objects.json:23618](../Assets/Resources/Content/Blueprints/Objects.json#L23618); [Objects.json:23706](../Assets/Resources/Content/Blueprints/Objects.json#L23706); [Objects.json:27181](../Assets/Resources/Content/Blueprints/Objects.json#L27181); [Objects.json:32885](../Assets/Resources/Content/Blueprints/Objects.json#L32885); [Objects.json:33000](../Assets/Resources/Content/Blueprints/Objects.json#L33000); [Objects.json:33739](../Assets/Resources/Content/Blueprints/Objects.json#L33739); [Objects.json:33786](../Assets/Resources/Content/Blueprints/Objects.json#L33786); [Objects.json:33883](../Assets/Resources/Content/Blueprints/Objects.json#L33883); [Objects.json:36934](../Assets/Resources/Content/Blueprints/Objects.json#L36934); [CombatSystem.cs:1597](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L1597); [CombatSystem.cs:695](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L695); [EquipBonusUtility.cs:53](../Assets/Scripts/Gameplay/Inventory/Commands/Equipment/EquipBonusUtility.cs#L53).

### leather gloves — `LeatherGloves`

**Where it comes from:** Loot/shop tables: ArmorerStock, FindArmorT1; Also Curation recovery cabinet; Gleaners cellar; Actual equipped NPC loadouts/death recovery (some chance/pick gated): MarlbackScrabbler, MarlbackGleaner, MarlbackPatchbearer, Weaponsmith, Tinker, RuinScavenger, Farmer, Scribe, RuneCultist, SaltMaster, RecensionScribe, StillleafSearcher, CurationSorter, StillleafIndexer, PeatCutter, GantryRegistrar

**What it actually does:** Equip in Handwear: AV +1, DV +0, Speed penalty 0.

**Limits and gaps:** AV applies to the selected hit slot containing this equipment; DV contributes to overall dodge. Requires matching available body slot. Weight 1. Item HP 5; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 8; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:10268](../Assets/Resources/Content/Blueprints/Objects.json#L10268); [LootTables.json:654](../Assets/Resources/Content/Data/Loot/LootTables.json#L654); [LootTables.json:1106](../Assets/Resources/Content/Data/Loot/LootTables.json#L1106); [LoadoutPart.cs:99](../Assets/Scripts/Gameplay/Entities/LoadoutPart.cs#L99); [Objects.json:2868](../Assets/Resources/Content/Blueprints/Objects.json#L2868); [Objects.json:3539](../Assets/Resources/Content/Blueprints/Objects.json#L3539); [Objects.json:3653](../Assets/Resources/Content/Blueprints/Objects.json#L3653); [Objects.json:11462](../Assets/Resources/Content/Blueprints/Objects.json#L11462); [Objects.json:12470](../Assets/Resources/Content/Blueprints/Objects.json#L12470); [Objects.json:17639](../Assets/Resources/Content/Blueprints/Objects.json#L17639); [Objects.json:23706](../Assets/Resources/Content/Blueprints/Objects.json#L23706); [Objects.json:24783](../Assets/Resources/Content/Blueprints/Objects.json#L24783); [Objects.json:27779](../Assets/Resources/Content/Blueprints/Objects.json#L27779); [Objects.json:33000](../Assets/Resources/Content/Blueprints/Objects.json#L33000); [Objects.json:33739](../Assets/Resources/Content/Blueprints/Objects.json#L33739); [Objects.json:33786](../Assets/Resources/Content/Blueprints/Objects.json#L33786); [Objects.json:33811](../Assets/Resources/Content/Blueprints/Objects.json#L33811); [Objects.json:33858](../Assets/Resources/Content/Blueprints/Objects.json#L33858); [Objects.json:33883](../Assets/Resources/Content/Blueprints/Objects.json#L33883); [Objects.json:36934](../Assets/Resources/Content/Blueprints/Objects.json#L36934); [CombatSystem.cs:1597](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L1597); [CombatSystem.cs:695](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L695); [EquipBonusUtility.cs:53](../Assets/Scripts/Gameplay/Inventory/Commands/Equipment/EquipBonusUtility.cs#L53).

### leather cap — `LeatherCap`

**Where it comes from:** Loot/shop tables: ArmorerStock, FindArmorT1, VillagerStock; Actual equipped NPC loadouts/death recovery (some chance/pick gated): MarlbackScrabbler, MarlbackTunnelguard, Elder, DesertBandit, RuinScavenger, MarlbackWallkeeper, WellKeeper, Scribe, TentRightHost, CurationSorter, StillleafIndexer, SootGremlin, DirtGnome, SpreadHurdleCutter

**What it actually does:** Equip in Head: AV +1, DV +0, Speed penalty 0. Any worn head covering stops Beating Height glare exposure accumulation.

**Limits and gaps:** AV applies to the selected hit slot containing this equipment; DV contributes to overall dodge. Requires matching available body slot. Weight 1. Item HP 10; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 8; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:10362](../Assets/Resources/Content/Blueprints/Objects.json#L10362); [LootTables.json:654](../Assets/Resources/Content/Data/Loot/LootTables.json#L654); [LootTables.json:1106](../Assets/Resources/Content/Data/Loot/LootTables.json#L1106); [LootTables.json:2540](../Assets/Resources/Content/Data/Loot/LootTables.json#L2540); [LoadoutPart.cs:99](../Assets/Scripts/Gameplay/Entities/LoadoutPart.cs#L99); [Objects.json:2868](../Assets/Resources/Content/Blueprints/Objects.json#L2868); [Objects.json:3690](../Assets/Resources/Content/Blueprints/Objects.json#L3690); [Objects.json:9050](../Assets/Resources/Content/Blueprints/Objects.json#L9050); [Objects.json:17022](../Assets/Resources/Content/Blueprints/Objects.json#L17022); [Objects.json:17639](../Assets/Resources/Content/Blueprints/Objects.json#L17639); [Objects.json:18219](../Assets/Resources/Content/Blueprints/Objects.json#L18219); [Objects.json:23618](../Assets/Resources/Content/Blueprints/Objects.json#L23618); [Objects.json:24783](../Assets/Resources/Content/Blueprints/Objects.json#L24783); [Objects.json:32885](../Assets/Resources/Content/Blueprints/Objects.json#L32885); [Objects.json:33811](../Assets/Resources/Content/Blueprints/Objects.json#L33811); [Objects.json:33858](../Assets/Resources/Content/Blueprints/Objects.json#L33858); [Objects.json:38713](../Assets/Resources/Content/Blueprints/Objects.json#L38713); [Objects.json:38834](../Assets/Resources/Content/Blueprints/Objects.json#L38834); [Objects.json:39544](../Assets/Resources/Content/Blueprints/Objects.json#L39544); [BeatingGlareSystem.cs:82](../Assets/Scripts/Gameplay/World/BeatingGlareSystem.cs#L82); [CombatSystem.cs:1597](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L1597); [CombatSystem.cs:695](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L695); [EquipBonusUtility.cs:53](../Assets/Scripts/Gameplay/Inventory/Commands/Equipment/EquipBonusUtility.cs#L53).

### ironshod boots — `IronshodBoots`

**Where it comes from:** Loot/shop tables: ArmorerStock, FindArmorT2, FindArmorT3; Actual equipped NPC loadouts/death recovery (some chance/pick gated): MarlbackBreacher

**What it actually does:** Equip in Feet: AV +2, DV +0, Speed penalty 5.

**Limits and gaps:** AV applies to the selected hit slot containing this equipment; DV contributes to overall dodge. Requires matching available body slot. Weight 4. Item HP 10; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 28; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:10456](../Assets/Resources/Content/Blueprints/Objects.json#L10456); [LootTables.json:654](../Assets/Resources/Content/Data/Loot/LootTables.json#L654); [LootTables.json:1136](../Assets/Resources/Content/Data/Loot/LootTables.json#L1136); [LootTables.json:1162](../Assets/Resources/Content/Data/Loot/LootTables.json#L1162); [LoadoutPart.cs:99](../Assets/Scripts/Gameplay/Entities/LoadoutPart.cs#L99); [Objects.json:18345](../Assets/Resources/Content/Blueprints/Objects.json#L18345); [CombatSystem.cs:1597](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L1597); [CombatSystem.cs:695](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L695); [EquipBonusUtility.cs:53](../Assets/Scripts/Gameplay/Inventory/Commands/Equipment/EquipBonusUtility.cs#L53).

### warded cloak — `WardedCloak`

**Where it comes from:** Loot/shop tables: ArmorerStock, FindArmorT2, FindArmorT3, UrnT3, ReliquaryT2, ElderStock, CuratorStock, EchoStock, MarcelineStock

**What it actually does:** Equip in Back: AV +0, DV +2, Speed penalty 0.

**Limits and gaps:** AV applies to the selected hit slot containing this equipment; DV contributes to overall dodge. Requires matching available body slot. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 2. Item HP 10; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 40; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:10554](../Assets/Resources/Content/Blueprints/Objects.json#L10554); [LootTables.json:654](../Assets/Resources/Content/Data/Loot/LootTables.json#L654); [LootTables.json:1136](../Assets/Resources/Content/Data/Loot/LootTables.json#L1136); [LootTables.json:1162](../Assets/Resources/Content/Data/Loot/LootTables.json#L1162); [LootTables.json:1736](../Assets/Resources/Content/Data/Loot/LootTables.json#L1736); [LootTables.json:2162](../Assets/Resources/Content/Data/Loot/LootTables.json#L2162); [LootTables.json:2756](../Assets/Resources/Content/Data/Loot/LootTables.json#L2756); [LootTables.json:2946](../Assets/Resources/Content/Data/Loot/LootTables.json#L2946); [LootTables.json:3036](../Assets/Resources/Content/Data/Loot/LootTables.json#L3036); [LootTables.json:3075](../Assets/Resources/Content/Data/Loot/LootTables.json#L3075); [CombatSystem.cs:1597](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L1597); [CombatSystem.cs:695](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L695); [EquipBonusUtility.cs:53](../Assets/Scripts/Gameplay/Inventory/Commands/Equipment/EquipBonusUtility.cs#L53).

### iron buckler — `IronBuckler`

**Where it comes from:** Loot/shop tables: ArmorerStock, FindArmorT2, FindArmorT3, UrnT3

**What it actually does:** Equip in Hand: AV +2, DV +1, Speed penalty 0.

**Limits and gaps:** AV applies to the selected hit slot containing this equipment; DV contributes to overall dodge. Requires matching available body slot. Uses a hand; no separate block/parry action or ShieldPart found. Armor/DV/resistance are the implemented protection. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 5. Item HP 10; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 45; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:10648](../Assets/Resources/Content/Blueprints/Objects.json#L10648); [LootTables.json:654](../Assets/Resources/Content/Data/Loot/LootTables.json#L654); [LootTables.json:1136](../Assets/Resources/Content/Data/Loot/LootTables.json#L1136); [LootTables.json:1162](../Assets/Resources/Content/Data/Loot/LootTables.json#L1162); [LootTables.json:1736](../Assets/Resources/Content/Data/Loot/LootTables.json#L1736); [CombatSystem.cs:1597](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L1597); [CombatSystem.cs:695](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L695); [EquipBonusUtility.cs:53](../Assets/Scripts/Gameplay/Inventory/Commands/Equipment/EquipBonusUtility.cs#L53).

### plate armor — `PlateArmor`

**Where it comes from:** Loot/shop tables: TombVaultT2, SealedVaultT3, ArmorerStock, FindArmorT3, StrongBoxT3, ReliquaryT3

**What it actually does:** Equip in Body: AV +8, DV -3, Speed penalty 0.

**Limits and gaps:** AV applies to the selected hit slot containing this equipment; DV contributes to overall dodge. Requires matching available body slot. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 40. Item HP 40; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 100; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:10742](../Assets/Resources/Content/Blueprints/Objects.json#L10742); [LootTables.json:344](../Assets/Resources/Content/Data/Loot/LootTables.json#L344); [LootTables.json:424](../Assets/Resources/Content/Data/Loot/LootTables.json#L424); [LootTables.json:654](../Assets/Resources/Content/Data/Loot/LootTables.json#L654); [LootTables.json:1162](../Assets/Resources/Content/Data/Loot/LootTables.json#L1162); [LootTables.json:2119](../Assets/Resources/Content/Data/Loot/LootTables.json#L2119); [LootTables.json:2205](../Assets/Resources/Content/Data/Loot/LootTables.json#L2205); [CombatSystem.cs:1597](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L1597); [CombatSystem.cs:695](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L695); [EquipBonusUtility.cs:53](../Assets/Scripts/Gameplay/Inventory/Commands/Equipment/EquipBonusUtility.cs#L53).

### cloak — `Cloak`

**Where it comes from:** Loot/shop tables: BanditCacheT2, ArmorerStock, FindArmorT1, UndertakerStock; Actual equipped NPC loadouts/death recovery (some chance/pick gated): RuneCultist

**What it actually does:** Equip in Back: AV +1, DV +1, Speed penalty 0.

**Limits and gaps:** AV applies to the selected hit slot containing this equipment; DV contributes to overall dodge. Requires matching available body slot. Weight 3. Item HP 6; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon. Normal humanoid Back has TargetWeight0, so its +1AV is not consulted by ordinary location-based melee/thrown attacks; DV+1 remains effective. Total AV display nevertheless counts it.

**Base affordances:** Base Commerce value 18; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:10827](../Assets/Resources/Content/Blueprints/Objects.json#L10827); [LootTables.json:180](../Assets/Resources/Content/Data/Loot/LootTables.json#L180); [LootTables.json:654](../Assets/Resources/Content/Data/Loot/LootTables.json#L654); [LootTables.json:1106](../Assets/Resources/Content/Data/Loot/LootTables.json#L1106); [LootTables.json:2652](../Assets/Resources/Content/Data/Loot/LootTables.json#L2652); [LoadoutPart.cs:99](../Assets/Scripts/Gameplay/Entities/LoadoutPart.cs#L99); [Objects.json:27779](../Assets/Resources/Content/Blueprints/Objects.json#L27779); [CombatSystem.cs:1597](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L1597); [CombatSystem.cs:695](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L695); [EquipBonusUtility.cs:53](../Assets/Scripts/Gameplay/Inventory/Commands/Equipment/EquipBonusUtility.cs#L53); [AnatomyFactory.cs:82](../Assets/Scripts/Gameplay/Anatomy/AnatomyFactory.cs#L82); [BodyPart.cs:63](../Assets/Scripts/Gameplay/Anatomy/BodyPart.cs#L63); [CombatSystem.cs:1571](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L1571).

### fine-ring mail — `FineRingMail`

**Where it comes from:** Loot/shop tables: FindDeepEquipmentT4; May receive one random tier-1 enhancement when drawn into native DeepReliquaryT4 (25% opportunity)

**What it actually does:** Equip in Body: AV +6, DV -1, Speed penalty 0.

**Limits and gaps:** AV applies to the selected hit slot containing this equipment; DV contributes to overall dodge. Requires matching available body slot. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 24. Item HP 25; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 125; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:39112](../Assets/Resources/Content/Blueprints/Objects.json#L39112); [LootTables.json:3100](../Assets/Resources/Content/Data/Loot/LootTables.json#L3100); [CombatSystem.cs:1597](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L1597); [CombatSystem.cs:695](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L695); [EquipBonusUtility.cs:53](../Assets/Scripts/Gameplay/Inventory/Commands/Equipment/EquipBonusUtility.cs#L53).

### riveted plate — `RivetedPlate`

**Where it comes from:** Loot/shop tables: FindDeepEquipmentT4; May receive one random tier-1 enhancement when drawn into native DeepReliquaryT4 (25% opportunity)

**What it actually does:** Equip in Body: AV +9, DV -4, Speed penalty 0.

**Limits and gaps:** AV applies to the selected hit slot containing this equipment; DV contributes to overall dodge. Requires matching available body slot. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 46. Item HP 40; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Base affordances:** Base Commerce value 140; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:39178](../Assets/Resources/Content/Blueprints/Objects.json#L39178); [LootTables.json:3100](../Assets/Resources/Content/Data/Loot/LootTables.json#L3100); [CombatSystem.cs:1597](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L1597); [CombatSystem.cs:695](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L695); [EquipBonusUtility.cs:53](../Assets/Scripts/Gameplay/Inventory/Commands/Equipment/EquipBonusUtility.cs#L53).

## Weapon components

### rough field haft — `FieldHaftComponent`

**Where it comes from:** Prepare one SalvagedTimber from inventory (no station)

**What it actually does:** Forge/replacement component: Slot=Haft, MaxStrengthBonus=1.

**Limits and gaps:** Consume one Blade/head + Haft + Binding per weapon; cannot wield the component itself. Single assembly anywhere; batch and component replacement require nearby forge. Reforging returns displaced component but clears tempering. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 1.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:666](../Assets/Resources/Content/Blueprints/Objects.json#L666); [PreparationRecipePart.cs:38](../Assets/Scripts/Gameplay/Preparation/PreparationRecipePart.cs#L38); [WeaponForgingService.cs:333](../Assets/Scripts/Gameplay/Weaponcraft/WeaponForgingService.cs#L333); [ForgeWeaponCommand.cs:92](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/ForgeWeaponCommand.cs#L92); [ReforgeWeaponCommand.cs:66](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/ReforgeWeaponCommand.cs#L66).

### plain knotflax binding — `PlainCordBindingComponent`

**Where it comes from:** Prepare one KnotflaxCord from inventory (no station)

**What it actually does:** Forge/replacement component: Slot=Binding.

**Limits and gaps:** Consume one Blade/head + Haft + Binding per weapon; cannot wield the component itself. Single assembly anywhere; batch and component replacement require nearby forge. Reforging returns displaced component but clears tempering. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 1.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:743](../Assets/Resources/Content/Blueprints/Objects.json#L743); [PreparationRecipePart.cs:39](../Assets/Scripts/Gameplay/Preparation/PreparationRecipePart.cs#L39); [WeaponForgingService.cs:333](../Assets/Scripts/Gameplay/Weaponcraft/WeaponForgingService.cs#L333); [ForgeWeaponCommand.cs:92](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/ForgeWeaponCommand.cs#L92); [ReforgeWeaponCommand.cs:66](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/ReforgeWeaponCommand.cs#L66).

### tepuibone striking head — `TepuiboneHeadComponent`

**Where it comes from:** Prepare one Tepuibone from inventory (no station)

**What it actually does:** Forge/replacement component: Slot=Blade, BaseDamage=1d3, PenBonus=-1, Attributes=Bludgeoning Cudgel.

**Limits and gaps:** Consume one Blade/head + Haft + Binding per weapon; cannot wield the component itself. Single assembly anywhere; batch and component replacement require nearby forge. Reforging returns displaced component but clears tempering. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 3.

**Base affordances:** Base Commerce value 5; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:816](../Assets/Resources/Content/Blueprints/Objects.json#L816); [PreparationRecipePart.cs:40](../Assets/Scripts/Gameplay/Preparation/PreparationRecipePart.cs#L40); [WeaponForgingService.cs:333](../Assets/Scripts/Gameplay/Weaponcraft/WeaponForgingService.cs#L333); [ForgeWeaponCommand.cs:92](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/ForgeWeaponCommand.cs#L92); [ReforgeWeaponCommand.cs:66](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/ReforgeWeaponCommand.cs#L66).

### braced haft — `BracedHaftComponent`

**Where it comes from:** Cinderhold weaponsmith finite regional opening shelf

**What it actually does:** Forge/replacement component: Slot=Haft, MaxStrengthBonus=5, HitBonus=-1.

**Limits and gaps:** Consume one Blade/head + Haft + Binding per weapon; cannot wield the component itself. Single assembly anywhere; batch and component replacement require nearby forge. Reforging returns displaced component but clears tempering. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 3.

**Base affordances:** Base Commerce value 25; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:901](../Assets/Resources/Content/Blueprints/Objects.json#L901); [CinderholdCompositionBuilder.cs:196](../Assets/Scripts/Gameplay/World/Generation/Builders/CinderholdCompositionBuilder.cs#L196); [WeaponForgingService.cs:333](../Assets/Scripts/Gameplay/Weaponcraft/WeaponForgingService.cs#L333); [ForgeWeaponCommand.cs:92](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/ForgeWeaponCommand.cs#L92); [ReforgeWeaponCommand.cs:66](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/ReforgeWeaponCommand.cs#L66).

### guard lashing — `GuardLashingComponent`

**Where it comes from:** Last Counter finite supply chest

**What it actually does:** Forge/replacement component: Slot=Binding, HitBonus=2, PenBonus=-1.

**Limits and gaps:** Consume one Blade/head + Haft + Binding per weapon; cannot wield the component itself. Single assembly anywhere; batch and component replacement require nearby forge. Reforging returns displaced component but clears tempering. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 3.

**Base affordances:** Base Commerce value 25; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:982](../Assets/Resources/Content/Blueprints/Objects.json#L982); [LastCounterCompositionBuilder.cs:168](../Assets/Scripts/Gameplay/World/Generation/Builders/LastCounterCompositionBuilder.cs#L168); [WeaponForgingService.cs:333](../Assets/Scripts/Gameplay/Weaponcraft/WeaponForgingService.cs#L333); [ForgeWeaponCommand.cs:92](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/ForgeWeaponCommand.cs#L92); [ReforgeWeaponCommand.cs:66](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/ReforgeWeaponCommand.cs#L66).

### peat-packing mallet head — `PeatMalletHeadComponent`

**Where it comes from:** Sodden works locker finite stock

**What it actually does:** Forge/replacement component: Slot=Blade, BaseDamage=1d4, PenBonus=-1, HitBonus=0, Attributes=Bludgeoning Cudgel.

**Limits and gaps:** Consume one Blade/head + Haft + Binding per weapon; cannot wield the component itself. Single assembly anywhere; batch and component replacement require nearby forge. Reforging returns displaced component but clears tempering. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 3.

**Base affordances:** Base Commerce value 24; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:4377](../Assets/Resources/Content/Blueprints/Objects.json#L4377); [SoddenDistrictBuilder.cs:99](../Assets/Scripts/Gameplay/World/Generation/Builders/SoddenDistrictBuilder.cs#L99); [WeaponForgingService.cs:333](../Assets/Scripts/Gameplay/Weaponcraft/WeaponForgingService.cs#L333); [ForgeWeaponCommand.cs:92](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/ForgeWeaponCommand.cs#L92); [ReforgeWeaponCommand.cs:66](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/ReforgeWeaponCommand.cs#L66).

### cinderhook axe head — `CinderhookAxeHeadComponent`

**Where it comes from:** Cinderhold weaponsmith finite regional opening shelf

**What it actually does:** Forge/replacement component: Slot=Blade, BaseDamage=1d8, PenBonus=0, HitBonus=-2, Attributes=Cutting Axe.

**Limits and gaps:** Consume one Blade/head + Haft + Binding per weapon; cannot wield the component itself. Single assembly anywhere; batch and component replacement require nearby forge. Reforging returns displaced component but clears tempering. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 3.

**Base affordances:** Base Commerce value 42; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:4471](../Assets/Resources/Content/Blueprints/Objects.json#L4471); [CinderholdCompositionBuilder.cs:196](../Assets/Scripts/Gameplay/World/Generation/Builders/CinderholdCompositionBuilder.cs#L196); [WeaponForgingService.cs:333](../Assets/Scripts/Gameplay/Weaponcraft/WeaponForgingService.cs#L333); [ForgeWeaponCommand.cs:92](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/ForgeWeaponCommand.cs#L92); [ReforgeWeaponCommand.cs:66](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/ReforgeWeaponCommand.cs#L66).

### counterweight long blade — `CounterweightLongBladeComponent`

**Where it comes from:** Last Counter finite supply chest

**What it actually does:** Forge/replacement component: Slot=Blade, BaseDamage=1d6, PenBonus=-1, HitBonus=1, Attributes=Cutting LongBlades.

**Limits and gaps:** Consume one Blade/head + Haft + Binding per weapon; cannot wield the component itself. Single assembly anywhere; batch and component replacement require nearby forge. Reforging returns displaced component but clears tempering. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 3.

**Base affordances:** Base Commerce value 42; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:4565](../Assets/Resources/Content/Blueprints/Objects.json#L4565); [LastCounterCompositionBuilder.cs:168](../Assets/Scripts/Gameplay/World/Generation/Builders/LastCounterCompositionBuilder.cs#L168); [WeaponForgingService.cs:333](../Assets/Scripts/Gameplay/Weaponcraft/WeaponForgingService.cs#L333); [ForgeWeaponCommand.cs:92](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/ForgeWeaponCommand.cs#L92); [ReforgeWeaponCommand.cs:66](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/ReforgeWeaponCommand.cs#L66).

### steel blade — `SteelBladeComponent`

**Where it comes from:** Loot/shop tables: WorkshopCacheT2, WeaponsmithStock, ComponentAny, OreCacheT3; Also Cinderhold/Quillhold exploration smith stock, regional exploration chest, random villager trade

**What it actually does:** Forge/replacement component: Slot=Blade, BaseDamage=1d6, Attributes=Cutting LongBlades.

**Limits and gaps:** Consume one Blade/head + Haft + Binding per weapon; cannot wield the component itself. Single assembly anywhere; batch and component replacement require nearby forge. Reforging returns displaced component but clears tempering. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 3.

**Base affordances:** Base Commerce value 8; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:4897](../Assets/Resources/Content/Blueprints/Objects.json#L4897); [LootTables.json:491](../Assets/Resources/Content/Data/Loot/LootTables.json#L491); [LootTables.json:596](../Assets/Resources/Content/Data/Loot/LootTables.json#L596); [LootTables.json:970](../Assets/Resources/Content/Data/Loot/LootTables.json#L970); [LootTables.json:1809](../Assets/Resources/Content/Data/Loot/LootTables.json#L1809); [WeaponForgingService.cs:333](../Assets/Scripts/Gameplay/Weaponcraft/WeaponForgingService.cs#L333); [ForgeWeaponCommand.cs:92](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/ForgeWeaponCommand.cs#L92); [ReforgeWeaponCommand.cs:66](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/ReforgeWeaponCommand.cs#L66).

### iron spike — `IronSpikeComponent`

**Where it comes from:** Loot/shop tables: WorkshopCacheT2, ComponentAny, OreCacheT2, TinkerStock, GnomeStock; Also dismantle second-exploration metal grate for one spike; random villager trade

**What it actually does:** Forge/replacement component: Slot=Blade, BaseDamage=1d4, PenBonus=1, Attributes=Piercing.

**Limits and gaps:** Consume one Blade/head + Haft + Binding per weapon; cannot wield the component itself. Single assembly anywhere; batch and component replacement require nearby forge. Reforging returns displaced component but clears tempering. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 3.

**Base affordances:** Base Commerce value 8; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:4939](../Assets/Resources/Content/Blueprints/Objects.json#L4939); [LootTables.json:491](../Assets/Resources/Content/Data/Loot/LootTables.json#L491); [LootTables.json:970](../Assets/Resources/Content/Data/Loot/LootTables.json#L970); [LootTables.json:1788](../Assets/Resources/Content/Data/Loot/LootTables.json#L1788); [LootTables.json:2704](../Assets/Resources/Content/Data/Loot/LootTables.json#L2704); [LootTables.json:2886](../Assets/Resources/Content/Data/Loot/LootTables.json#L2886); [WeaponForgingService.cs:333](../Assets/Scripts/Gameplay/Weaponcraft/WeaponForgingService.cs#L333); [ForgeWeaponCommand.cs:92](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/ForgeWeaponCommand.cs#L92); [ReforgeWeaponCommand.cs:66](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/ReforgeWeaponCommand.cs#L66).

### oak haft — `OakHaftComponent`

**Where it comes from:** Loot/shop tables: WeaponsmithStock, ComponentAny, TinkerStock; Also Sodden works locker, Cinderhold smith, Last Counter chest, random villager trade

**What it actually does:** Forge/replacement component: Slot=Haft, MaxStrengthBonus=3.

**Limits and gaps:** Consume one Blade/head + Haft + Binding per weapon; cannot wield the component itself. Single assembly anywhere; batch and component replacement require nearby forge. Reforging returns displaced component but clears tempering. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 3.

**Base affordances:** Base Commerce value 8; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:4985](../Assets/Resources/Content/Blueprints/Objects.json#L4985); [LootTables.json:596](../Assets/Resources/Content/Data/Loot/LootTables.json#L596); [LootTables.json:970](../Assets/Resources/Content/Data/Loot/LootTables.json#L970); [LootTables.json:2704](../Assets/Resources/Content/Data/Loot/LootTables.json#L2704); [WeaponForgingService.cs:333](../Assets/Scripts/Gameplay/Weaponcraft/WeaponForgingService.cs#L333); [ForgeWeaponCommand.cs:92](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/ForgeWeaponCommand.cs#L92); [ReforgeWeaponCommand.cs:66](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/ReforgeWeaponCommand.cs#L66).

### willow haft — `WillowHaftComponent`

**Where it comes from:** Loot/shop tables: ComponentAny; Also random friendly-villager trade

**What it actually does:** Forge/replacement component: Slot=Haft, MaxStrengthBonus=2, HitBonus=1.

**Limits and gaps:** Consume one Blade/head + Haft + Binding per weapon; cannot wield the component itself. Single assembly anywhere; batch and component replacement require nearby forge. Reforging returns displaced component but clears tempering. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 3.

**Base affordances:** Base Commerce value 8; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:5023](../Assets/Resources/Content/Blueprints/Objects.json#L5023); [LootTables.json:970](../Assets/Resources/Content/Data/Loot/LootTables.json#L970); [WeaponForgingService.cs:333](../Assets/Scripts/Gameplay/Weaponcraft/WeaponForgingService.cs#L333); [ForgeWeaponCommand.cs:92](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/ForgeWeaponCommand.cs#L92); [ReforgeWeaponCommand.cs:66](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/ReforgeWeaponCommand.cs#L66).

### leather binding — `LeatherBindingComponent`

**Where it comes from:** Loot/shop tables: WeaponsmithStock, ComponentAny, TinkerStock; Also Sodden works locker, Cinderhold smith, Last Counter chest, exploration smith, random villager trade

**What it actually does:** Forge/replacement component: Slot=Binding, HitBonus=1.

**Limits and gaps:** Consume one Blade/head + Haft + Binding per weapon; cannot wield the component itself. Single assembly anywhere; batch and component replacement require nearby forge. Reforging returns displaced component but clears tempering. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 3.

**Base affordances:** Base Commerce value 8; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:5065](../Assets/Resources/Content/Blueprints/Objects.json#L5065); [LootTables.json:596](../Assets/Resources/Content/Data/Loot/LootTables.json#L596); [LootTables.json:970](../Assets/Resources/Content/Data/Loot/LootTables.json#L970); [LootTables.json:2704](../Assets/Resources/Content/Data/Loot/LootTables.json#L2704); [WeaponForgingService.cs:333](../Assets/Scripts/Gameplay/Weaponcraft/WeaponForgingService.cs#L333); [ForgeWeaponCommand.cs:92](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/ForgeWeaponCommand.cs#L92); [ReforgeWeaponCommand.cs:66](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/ReforgeWeaponCommand.cs#L66).

### serrated edge kit — `SerratedEdgeComponent`

**Where it comes from:** Loot/shop tables: WeaponsmithStock, ComponentAny; Also random friendly-villager trade

**What it actually does:** Forge/replacement component: Slot=Binding, PenBonus=1, OnHitEffectSpec=Bleeding,20,1d2,15,0.

**Limits and gaps:** Consume one Blade/head + Haft + Binding per weapon; cannot wield the component itself. Single assembly anywhere; batch and component replacement require nearby forge. Reforging returns displaced component but clears tempering. This is a Binding slot component, not EnhancementSerrated. Its independent 20% bleed spec yields 1d2 bleeding with default save15; encoded duration15 is ignored for Bleeding. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 3.

**Base affordances:** Base Commerce value 8; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:5103](../Assets/Resources/Content/Blueprints/Objects.json#L5103); [LootTables.json:596](../Assets/Resources/Content/Data/Loot/LootTables.json#L596); [LootTables.json:970](../Assets/Resources/Content/Data/Loot/LootTables.json#L970); [WeaponForgingService.cs:333](../Assets/Scripts/Gameplay/Weaponcraft/WeaponForgingService.cs#L333); [ForgeWeaponCommand.cs:92](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/ForgeWeaponCommand.cs#L92); [ReforgeWeaponCommand.cs:66](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/ReforgeWeaponCommand.cs#L66).

## Manufactured gas grenades

### poison gas grenade — `PoisonGasGrenade`

**Where it comes from:** Loot/shop tables: BanditCacheT2, ProvisionerStock, FindOffenseT1, FindOffenseT2, FindOffenseT3, DeathHumanoidT3, AlchemyShelfT3

**What it actually does:** Throw consumable: poison-vapor density20, level1 in a 3×3 area at impact. Poison vapor: immediate floor((intake+1)/20), minimum1, plus PoisonedByGas (2 damage/turn for random1–10 turns at level1); repeated exposure refreshes.

**Limits and gaps:** Consumed on impact even on ground/wall; no projectile accuracy roll for gas payload. Gas affects allies and thrower; creature/respiration/immunity gates apply. Gas diffuses/decays. No authored NPC grenade-throw behavior found. Weight 2.

**Base affordances:** Base Commerce value 18; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:28774](../Assets/Resources/Content/Blueprints/Objects.json#L28774); [LootTables.json:180](../Assets/Resources/Content/Data/Loot/LootTables.json#L180); [LootTables.json:842](../Assets/Resources/Content/Data/Loot/LootTables.json#L842); [LootTables.json:1192](../Assets/Resources/Content/Data/Loot/LootTables.json#L1192); [LootTables.json:1234](../Assets/Resources/Content/Data/Loot/LootTables.json#L1234); [LootTables.json:1276](../Assets/Resources/Content/Data/Loot/LootTables.json#L1276); [LootTables.json:1421](../Assets/Resources/Content/Data/Loot/LootTables.json#L1421); [LootTables.json:2476](../Assets/Resources/Content/Data/Loot/LootTables.json#L2476); [GasGrenadePart.cs:52](../Assets/Scripts/Gameplay/Materials/GasGrenadePart.cs#L52); [ThrowItemCommand.cs:187](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L187); [GasPoisonPart.cs:51](../Assets/Scripts/Gameplay/Materials/GasPoisonPart.cs#L51).

### sleep gas grenade — `SleepGasGrenade`

**Where it comes from:** Loot/shop tables: CultCacheT2, FindOffenseT1, FindOffenseT2, FindOffenseT3

**What it actually does:** Throw consumable: sleep-vapor density20, level1 in a 3×3 area at impact. Sleep vapor: sleep3 turns at level1, refreshed on exposure; wakes on damage.

**Limits and gaps:** Consumed on impact even on ground/wall; no projectile accuracy roll for gas payload. Gas affects allies and thrower; creature/respiration/immunity gates apply. Gas diffuses/decays. No authored NPC grenade-throw behavior found. Weight 2.

**Base affordances:** Base Commerce value 18; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:28849](../Assets/Resources/Content/Blueprints/Objects.json#L28849); [LootTables.json:528](../Assets/Resources/Content/Data/Loot/LootTables.json#L528); [LootTables.json:1192](../Assets/Resources/Content/Data/Loot/LootTables.json#L1192); [LootTables.json:1234](../Assets/Resources/Content/Data/Loot/LootTables.json#L1234); [LootTables.json:1276](../Assets/Resources/Content/Data/Loot/LootTables.json#L1276); [GasGrenadePart.cs:52](../Assets/Scripts/Gameplay/Materials/GasGrenadePart.cs#L52); [ThrowItemCommand.cs:187](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L187); [GasSleepPart.cs:26](../Assets/Scripts/Gameplay/Materials/GasSleepPart.cs#L26).

### stun gas grenade — `StunGasGrenade`

**Where it comes from:** Loot/shop tables: WarbandLootT2, WeaponsmithStock, FindOffenseT1, FindOffenseT2, FindOffenseT3

**What it actually does:** Throw consumable: stun-vapor density20, level1 in a 3×3 area at impact. Stun vapor: stun2 turns at level1, refreshed on exposure.

**Limits and gaps:** Consumed on impact even on ground/wall; no projectile accuracy roll for gas payload. Gas affects allies and thrower; creature/respiration/immunity gates apply. Gas diffuses/decays. No authored NPC grenade-throw behavior found. Weight 2.

**Base affordances:** Base Commerce value 18; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:28924](../Assets/Resources/Content/Blueprints/Objects.json#L28924); [LootTables.json:312](../Assets/Resources/Content/Data/Loot/LootTables.json#L312); [LootTables.json:596](../Assets/Resources/Content/Data/Loot/LootTables.json#L596); [LootTables.json:1192](../Assets/Resources/Content/Data/Loot/LootTables.json#L1192); [LootTables.json:1234](../Assets/Resources/Content/Data/Loot/LootTables.json#L1234); [LootTables.json:1276](../Assets/Resources/Content/Data/Loot/LootTables.json#L1276); [GasGrenadePart.cs:52](../Assets/Scripts/Gameplay/Materials/GasGrenadePart.cs#L52); [ThrowItemCommand.cs:187](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L187); [GasStunPart.cs:39](../Assets/Scripts/Gameplay/Materials/GasStunPart.cs#L39).

## Tonics, cures and dressings

### knotflax bandage — `KnotflaxBandage`

**Where it comes from:** Inventory preparation of 1 KnotflaxCord, no station, produces one KnotflaxBandage.

**What it actually does:** Apply consumes one carried unit and applies the payload to self; throwing shatters the payload on the impact cell and can affect other actors. Removes BleedingEffect instances.

**Limits and gaps:** Ordinary use targets self, including harmful payloads; consuming/throwing spends it. Benefits do not imply general NPC self-use AI.

**Base affordances:** Base Commerce value 2; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:1543](../Assets/Resources/Content/Blueprints/Objects.json#L1543); [TonicPart.cs:37](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L37); [TonicPart.cs:93](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L93); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176); [CureTonicPart.cs:27](../Assets/Scripts/Gameplay/Items/CureTonicPart.cs#L27); [PreparationRecipePart.cs:41](../Assets/Scripts/Gameplay/Preparation/PreparationRecipePart.cs#L41); [PreparationRecipePart.cs:45](../Assets/Scripts/Gameplay/Preparation/PreparationRecipePart.cs#L45).

### sumpsieve field dressing — `SoddenFieldDressing`

**Where it comes from:** Repair Sella’s SoddenDressingBench with two timber; then living adjacent nonhostile keeper converts one SumpsievePad + one KnotflaxCord + 2 drams into one dressing. Also Curation recovery cabinet contains one.

**What it actually does:** Inventory Apply is offered only when afflicted. Removes all ordinary PoisonedEffect and BleedingEffect instances together.

**Limits and gaps:** Does not heal, confer immunity, cure poison gas, or fungal infection. Converting SumpsievePad to a dressing loses the raw pad’s actual gas-poison cure capability.

**Base affordances:** Base Commerce value 5; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:2043](../Assets/Resources/Content/Blueprints/Objects.json#L2043); [SoddenPreparationPart.cs:57](../Assets/Scripts/Gameplay/World/SoddenPreparationPart.cs#L57); [SoddenPreparationPart.cs:97](../Assets/Scripts/Gameplay/World/SoddenPreparationPart.cs#L97); [CurationReceivingBuilder.cs:63](../Assets/Scripts/Gameplay/World/Generation/Builders/CurationReceivingBuilder.cs#L63); [SoddenDressingPart.cs:14](../Assets/Scripts/Gameplay/Items/SoddenDressingPart.cs#L14).

### healing tonic — `HealingTonic`

**Where it comes from:** Current loot/merchant tables: MorrowfastProvisionerStock, LairLoot, CaveSupplyT1, BanditCacheT2, HunterCacheT1, CampGoodsT1, WarbandLootT2, DeepSupplyT2, ApothecaryStock, DeathHumanoidT2, DeathHumanoidT3, CrateT2, CrateT3, StrongBoxT1, AlchemyShelfT1, VillagerStock, InnkeeperStock, WellKeeperStock, WardenStock, ElderStock, MerchantStock, QuartermasterStock, HermitStock, EnvoyStock.

**What it actually does:** Drink consumes one carried unit and applies the payload to self; throwing shatters the payload on the impact cell and can affect other actors. Instant 4d6+4 HP healing capped at maximum. Also removes one Parched stack (the shared tonic path does this on a thrown target too). Actual NPC AI: MarlbackPatchbearer uses carried HealingTonic at ≤40% HP against a hostile threat, sacrificing that movement/attack turn. Unspent stock drops on death.

**Limits and gaps:** Ordinary use targets self, including harmful payloads; consuming/throwing spends it. Benefits do not imply general NPC self-use AI.

**Base affordances:** Base Commerce value 15; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:3999](../Assets/Resources/Content/Blueprints/Objects.json#L3999); [LootTables.json:72](../Assets/Resources/Content/Data/Loot/LootTables.json#L72); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [TonicPart.cs:37](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L37); [TonicPart.cs:93](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L93); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176); [Objects.json:3653](../Assets/Resources/Content/Blueprints/Objects.json#L3653); [FieldMedicinePart.cs:99](../Assets/Scripts/Gameplay/AI/FieldMedicinePart.cs#L99); [KillGoal.cs:49](../Assets/Scripts/Gameplay/AI/Goals/KillGoal.cs#L49); [FleeGoal.cs:42](../Assets/Scripts/Gameplay/AI/Goals/FleeGoal.cs#L42); [SpreadExplorationWorksites.cs:189](../Assets/Scripts/Gameplay/World/Generation/Builders/SpreadExplorationWorksites.cs#L189).

### poison tonic — `PoisonTonic`

**Where it comes from:** Current loot/merchant tables: CultCacheT2, FindOffenseT1, FindOffenseT2, FindOffenseT3.

**What it actually does:** Drink consumes one carried unit and applies the payload to self; throwing shatters the payload on the impact cell and can affect other actors. Also removes one Parched stack (the shared tonic path does this on a thrown target too). PoisonedEffect: 1d2 damage each owner turn for 6 turns.

**Limits and gaps:** Ordinary use targets self, including harmful payloads; consuming/throwing spends it. Benefits do not imply general NPC self-use AI.

**Base affordances:** Base Commerce value 18; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:4068](../Assets/Resources/Content/Blueprints/Objects.json#L4068); [LootTables.json:536](../Assets/Resources/Content/Data/Loot/LootTables.json#L536); [ContainerPlacementService.cs:185](../Assets/Scripts/Gameplay/World/Generation/ContainerPlacementService.cs#L185); [TonicPart.cs:37](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L37); [TonicPart.cs:93](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L93); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176); [StatusTonicPart.cs:28](../Assets/Scripts/Gameplay/Items/StatusTonicPart.cs#L28); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35).

### fire tonic — `FireTonic`

**Where it comes from:** Current loot/merchant tables: CultCacheT2, FindOffenseT1, FindOffenseT2, FindOffenseT3, DrifterStock.

**What it actually does:** Drink consumes one carried unit and applies the payload to self; throwing shatters the payload on the impact cell and can affect other actors. Also removes one Parched stack (the shared tonic path does this on a thrown target too). BurningEffect at intensity 1; ongoing fire damage and thermal interactions.

**Limits and gaps:** Ordinary use targets self, including harmful payloads; consuming/throwing spends it. Benefits do not imply general NPC self-use AI.

**Base affordances:** Base Commerce value 20; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:4150](../Assets/Resources/Content/Blueprints/Objects.json#L4150); [LootTables.json:540](../Assets/Resources/Content/Data/Loot/LootTables.json#L540); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [TonicPart.cs:37](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L37); [TonicPart.cs:93](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L93); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176); [StatusTonicPart.cs:28](../Assets/Scripts/Gameplay/Items/StatusTonicPart.cs#L28); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35).

### strange brew — `BrewedTonic`

**Where it comes from:** Any successful current reagent mix through Crafting Brew/still action; single mixtures and Food batches work anywhere, non-Food batches above one require a still. Output retains this blueprint ID with runtime name/payload.

**What it actually does:** Drink consumes one carried unit and applies the payload to self; throwing shatters the payload on the impact cell and can affect other actors. Also removes one Parched stack (the shared tonic path does this on a thrown target too). Variable payload: all matched statuses apply; healing rules become max potency d4. Any status-bearing BrewItem mixture (tonic/coating/throwable form) may quench melee weapons at a forge, giving each brewed status a 20+10*potency percent on-hit chance capped at 50%. Healing-only brews lack BrewItem and cannot quench; fixed stock StatusTonic items also cannot quench.

**Limits and gaps:** Ordinary use targets self, including harmful payloads; consuming/throwing spends it. Benefits do not imply general NPC self-use AI. Form is not a separate item ID. Shared Tonic UI still says Drink, even for coating/throwable/food-form names; plain development-spawned BrewedTonic has no payload.

**Base affordances:** Base Commerce value 12; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:6380](../Assets/Resources/Content/Blueprints/Objects.json#L6380); [TonicPart.cs:37](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L37); [TonicPart.cs:93](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L93); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176); [BrewingService.cs:138](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L138); [BrewingService.cs:325](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L325); [BrewItemPart.cs:61](../Assets/Scripts/Gameplay/Alchemy/BrewItemPart.cs#L61); [WeaponTemperingService.cs:88](../Assets/Scripts/Gameplay/Weaponcraft/WeaponTemperingService.cs#L88); [WeaponTemperingService.cs:159](../Assets/Scripts/Gameplay/Weaponcraft/WeaponTemperingService.cs#L159); [TemperWeaponCommand.cs:51](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/TemperWeaponCommand.cs#L51).

### acid tonic — `AcidTonic`

**Where it comes from:** Current loot/merchant tables: FindOffenseT1, FindOffenseT2, FindOffenseT3.

**What it actually does:** Drink consumes one carried unit and applies the payload to self; throwing shatters the payload on the impact cell and can affect other actors. Also removes one Parched stack (the shared tonic path does this on a thrown target too). AcidicEffect corrosion 1; ongoing typed acid damage and material degradation.

**Limits and gaps:** Ordinary use targets self, including harmful payloads; consuming/throwing spends it. Benefits do not imply general NPC self-use AI.

**Base affordances:** Base Commerce value 22; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:6509](../Assets/Resources/Content/Blueprints/Objects.json#L6509); [LootTables.json:1208](../Assets/Resources/Content/Data/Loot/LootTables.json#L1208); [ContainerPlacementService.cs:185](../Assets/Scripts/Gameplay/World/Generation/ContainerPlacementService.cs#L185); [TonicPart.cs:37](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L37); [TonicPart.cs:93](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L93); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176); [StatusTonicPart.cs:28](../Assets/Scripts/Gameplay/Items/StatusTonicPart.cs#L28); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35).

### lightning tonic — `LightningTonic`

**Where it comes from:** Current loot/merchant tables: FindOffenseT1, FindOffenseT2, FindOffenseT3.

**What it actually does:** Drink consumes one carried unit and applies the payload to self; throwing shatters the payload on the impact cell and can affect other actors. Also removes one Parched stack (the shared tonic path does this on a thrown target too). ElectrifiedEffect charge 1; lightning damage and chaining; wet targets amplify charge.

**Limits and gaps:** Ordinary use targets self, including harmful payloads; consuming/throwing spends it. Benefits do not imply general NPC self-use AI.

**Base affordances:** Base Commerce value 22; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:6587](../Assets/Resources/Content/Blueprints/Objects.json#L6587); [LootTables.json:1212](../Assets/Resources/Content/Data/Loot/LootTables.json#L1212); [ContainerPlacementService.cs:185](../Assets/Scripts/Gameplay/World/Generation/ContainerPlacementService.cs#L185); [TonicPart.cs:37](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L37); [TonicPart.cs:93](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L93); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176); [StatusTonicPart.cs:28](../Assets/Scripts/Gameplay/Items/StatusTonicPart.cs#L28); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35).

### frost tonic — `FrostTonic`

**Where it comes from:** Current loot/merchant tables: FindOffenseT1, FindOffenseT2, FindOffenseT3.

**What it actually does:** Drink consumes one carried unit and applies the payload to self; throwing shatters the payload on the impact cell and can affect other actors. Also removes one Parched stack (the shared tonic path does this on a thrown target too). FrozenEffect cold 1; blocks actions until thaw, extinguishes burning; thaw depends on heat/time.

**Limits and gaps:** Ordinary use targets self, including harmful payloads; consuming/throwing spends it. Benefits do not imply general NPC self-use AI.

**Base affordances:** Base Commerce value 22; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:6665](../Assets/Resources/Content/Blueprints/Objects.json#L6665); [LootTables.json:1204](../Assets/Resources/Content/Data/Loot/LootTables.json#L1204); [ContainerPlacementService.cs:185](../Assets/Scripts/Gameplay/World/Generation/ContainerPlacementService.cs#L185); [TonicPart.cs:37](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L37); [TonicPart.cs:93](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L93); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176); [StatusTonicPart.cs:28](../Assets/Scripts/Gameplay/Items/StatusTonicPart.cs#L28); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35).

### water tonic — `WaterTonic`

**Where it comes from:** Current loot/merchant tables: MorrowfastProvisionerStock, ProvisionerStock, WellKeeperStock.

**What it actually does:** Drink consumes one carried unit and applies the payload to self; throwing shatters the payload on the impact cell and can affect other actors. Also removes one Parched stack (the shared tonic path does this on a thrown target too). WetEffect moisture 1; suppresses ignition when wet enough and increases some cold/electric interactions.

**Limits and gaps:** Ordinary use targets self, including harmful payloads; consuming/throwing spends it. Benefits do not imply general NPC self-use AI.

**Base affordances:** Base Commerce value 12; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:6743](../Assets/Resources/Content/Blueprints/Objects.json#L6743); [LootTables.json:82](../Assets/Resources/Content/Data/Loot/LootTables.json#L82); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [TonicPart.cs:37](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L37); [TonicPart.cs:93](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L93); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176); [StatusTonicPart.cs:28](../Assets/Scripts/Gameplay/Items/StatusTonicPart.cs#L28); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35).

### bleed tonic — `BleedTonic`

**Where it comes from:** Current loot/merchant tables: FindOffenseT1, FindOffenseT2, FindOffenseT3.

**What it actually does:** Drink consumes one carried unit and applies the payload to self; throwing shatters the payload on the impact cell and can affect other actors. Also removes one Parched stack (the shared tonic path does this on a thrown target too). BleedingEffect deals 1d2 each turn; configured duration 12 is actually Toughness save DC 12, NOT a fixed duration.

**Limits and gaps:** Ordinary use targets self, including harmful payloads; consuming/throwing spends it. Benefits do not imply general NPC self-use AI.

**Base affordances:** Base Commerce value 18; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:6825](../Assets/Resources/Content/Blueprints/Objects.json#L6825); [LootTables.json:1216](../Assets/Resources/Content/Data/Loot/LootTables.json#L1216); [ContainerPlacementService.cs:185](../Assets/Scripts/Gameplay/World/Generation/ContainerPlacementService.cs#L185); [TonicPart.cs:37](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L37); [TonicPart.cs:93](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L93); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176); [StatusTonicPart.cs:28](../Assets/Scripts/Gameplay/Items/StatusTonicPart.cs#L28); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35).

### antidote — `Antidote`

**Where it comes from:** Current loot/merchant tables: HunterCacheT1, TombVaultT2, ApothecaryStock, AlchemyShelfT2, WellKeeperStock, UndertakerStock, HermitStock.

**What it actually does:** Drink consumes one carried unit and applies the payload to self; throwing shatters the payload on the impact cell and can affect other actors. Also removes one Parched stack (the shared tonic path does this on a thrown target too). Removes PoisonedEffect instances. Also removes PoisonedByGasEffect. Actual NPC AI: current MarlbackPatchbearer opts into Antidote; it treats ordinary/gas poison before healing if carried.

**Limits and gaps:** Ordinary use targets self, including harmful payloads; consuming/throwing spends it. Benefits do not imply general NPC self-use AI.

**Base affordances:** Base Commerce value 30; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:6977](../Assets/Resources/Content/Blueprints/Objects.json#L6977); [LootTables.json:230](../Assets/Resources/Content/Data/Loot/LootTables.json#L230); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [TonicPart.cs:37](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L37); [TonicPart.cs:93](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L93); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176); [CureTonicPart.cs:27](../Assets/Scripts/Gameplay/Items/CureTonicPart.cs#L27); [Objects.json:3653](../Assets/Resources/Content/Blueprints/Objects.json#L3653); [FieldMedicinePart.cs:61](../Assets/Scripts/Gameplay/AI/FieldMedicinePart.cs#L61).

### burn salve — `BurnSalve`

**Where it comes from:** Current loot/merchant tables: MorrowfastProvisionerStock, ApothecaryStock, AlchemyShelfT2, UndertakerStock, HermitStock.

**What it actually does:** Drink consumes one carried unit and applies the payload to self; throwing shatters the payload on the impact cell and can affect other actors. Also removes one Parched stack (the shared tonic path does this on a thrown target too). Removes BurningEffect instances. Also lowers thermal temperature to at most ambient to prevent immediate reignition.

**Limits and gaps:** Ordinary use targets self, including harmful payloads; consuming/throwing spends it. Benefits do not imply general NPC self-use AI. FieldMedicine supports opted-in BurnSalve, but no current blueprint opts into it; do not claim autonomous NPC use just from the supported branch.

**Base affordances:** Base Commerce value 25; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:7055](../Assets/Resources/Content/Blueprints/Objects.json#L7055); [LootTables.json:77](../Assets/Resources/Content/Data/Loot/LootTables.json#L77); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [TonicPart.cs:37](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L37); [TonicPart.cs:93](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L93); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176); [CureTonicPart.cs:27](../Assets/Scripts/Gameplay/Items/CureTonicPart.cs#L27); [FieldMedicinePart.cs:64](../Assets/Scripts/Gameplay/AI/FieldMedicinePart.cs#L64).

### panacea — `Panacea`

**Where it comes from:** Current loot/merchant tables: ApothecaryStock, SackT3, BasketT3, StrongBoxT3, ReliquaryT3, AlchemyShelfT3, ElderStock.

**What it actually does:** Drink consumes one carried unit and applies the payload to self; throwing shatters the payload on the impact cell and can affect other actors. Also removes one Parched stack (the shared tonic path does this on a thrown target too). Removes all TYPE_NEGATIVE effects; keeps positive boons/oaths.

**Limits and gaps:** Ordinary use targets self, including harmful payloads; consuming/throwing spends it. Benefits do not imply general NPC self-use AI.

**Base affordances:** Base Commerce value 100; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:7129](../Assets/Resources/Content/Blueprints/Objects.json#L7129); [LootTables.json:734](../Assets/Resources/Content/Data/Loot/LootTables.json#L734); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [TonicPart.cs:37](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L37); [TonicPart.cs:93](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L93); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176); [CureTonicPart.cs:27](../Assets/Scripts/Gameplay/Items/CureTonicPart.cs#L27).

### speed tonic — `SpeedTonic`

**Where it comes from:** Current loot/merchant tables: BanditCacheT2, ApothecaryStock, SackT2, StrongBoxT2, MerchantStock, EnvoyStock.

**What it actually does:** Drink consumes one carried unit and applies the payload to self; throwing shatters the payload on the impact cell and can affect other actors. Speed:20 for 20 owner turns; reapplication refreshes/merges the timed surge. Also removes one Parched stack (the shared tonic path does this on a thrown target too).

**Limits and gaps:** Ordinary use targets self, including harmful payloads; consuming/throwing spends it. Benefits do not imply general NPC self-use AI.

**Base affordances:** Base Commerce value 20; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:10921](../Assets/Resources/Content/Blueprints/Objects.json#L10921); [LootTables.json:192](../Assets/Resources/Content/Data/Loot/LootTables.json#L192); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [TonicStatSurgeEffect.cs:12](../Assets/Scripts/Gameplay/Effects/Concrete/TonicStatSurgeEffect.cs#L12); [TonicPart.cs:37](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L37); [TonicPart.cs:93](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L93); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176).

### strength tonic — `StrengthTonic`

**Where it comes from:** Current loot/merchant tables: ApothecaryStock, StrongBoxT2, QuartermasterStock.

**What it actually does:** Drink consumes one carried unit and applies the payload to self; throwing shatters the payload on the impact cell and can affect other actors. Strength:4 for 20 owner turns; reapplication refreshes/merges the timed surge. Also removes one Parched stack (the shared tonic path does this on a thrown target too).

**Limits and gaps:** Ordinary use targets self, including harmful payloads; consuming/throwing spends it. Benefits do not imply general NPC self-use AI.

**Base affordances:** Base Commerce value 22; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:10998](../Assets/Resources/Content/Blueprints/Objects.json#L10998); [LootTables.json:726](../Assets/Resources/Content/Data/Loot/LootTables.json#L726); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [TonicStatSurgeEffect.cs:12](../Assets/Scripts/Gameplay/Effects/Concrete/TonicStatSurgeEffect.cs#L12); [TonicPart.cs:37](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L37); [TonicPart.cs:93](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L93); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176).

### stoneskin tonic — `StoneskinTonic`

**Where it comes from:** Current loot/merchant tables: ApothecaryStock, StrongBoxT3, AlchemyShelfT3.

**What it actually does:** Drink consumes one carried unit and applies the payload to self; throwing shatters the payload on the impact cell and can affect other actors. Also removes one Parched stack (the shared tonic path does this on a thrown target too). StoneskinEffect subtracts 2 from every incoming damage event for 30 turns, before resistances.

**Limits and gaps:** Ordinary use targets self, including harmful payloads; consuming/throwing spends it. Benefits do not imply general NPC self-use AI.

**Base affordances:** Base Commerce value 25; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:11075](../Assets/Resources/Content/Blueprints/Objects.json#L11075); [LootTables.json:730](../Assets/Resources/Content/Data/Loot/LootTables.json#L730); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [TonicPart.cs:37](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L37); [TonicPart.cs:93](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L93); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176); [StatusTonicPart.cs:28](../Assets/Scripts/Gameplay/Items/StatusTonicPart.cs#L28); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35).

### claspbean pulp — `ClaspbeanPulp`

**Where it comes from:** Harvest ClaspbeanCrop in current Spread biome patches: 2 ClaspbeanPulp and 1 ClaspbeanSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Apply consumes one carried unit and applies the payload to self; throwing shatters the payload on the impact cell and can affect other actors. Removes BleedingEffect instances. Ingredient in paid FieldMeal batch (1 pulp + 2 Emberwheat).

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Ordinary use targets self, including harmful payloads; consuming/throwing spends it. Benefits do not imply general NPC self-use AI.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:43796](../Assets/Resources/Content/Blueprints/Objects.json#L43796); [BiomeCrops.json:4](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L4); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [TonicPart.cs:37](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L37); [TonicPart.cs:93](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L93); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176); [CureTonicPart.cs:27](../Assets/Scripts/Gameplay/Items/CureTonicPart.cs#L27); [KitchenBatchPart.cs:108](../Assets/Scripts/Gameplay/World/KitchenBatchPart.cs#L108).

### sumpsieve pad — `SumpsievePad`

**Where it comes from:** Harvest SumpsieveCrop in current Sodden biome patches: 2 SumpsievePad and 1 SumpsieveSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Apply consumes one carried unit and applies the payload to self; throwing shatters the payload on the impact cell and can affect other actors. Removes PoisonedEffect instances. Also removes PoisonedByGasEffect.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Ordinary use targets self, including harmful payloads; consuming/throwing spends it. Benefits do not imply general NPC self-use AI. CONTENT DRIFT: catalog and item text say it does not treat poison gas, but shared CureTonic actually removes PoisonedByGasEffect. Execution is broader than text.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:45229](../Assets/Resources/Content/Blueprints/Objects.json#L45229); [BiomeCrops.json:79](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L79); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [TonicPart.cs:37](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L37); [TonicPart.cs:93](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L93); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176); [CureTonicPart.cs:27](../Assets/Scripts/Gameplay/Items/CureTonicPart.cs#L27); [CureTonicPart.cs:45](../Assets/Scripts/Gameplay/Items/CureTonicPart.cs#L45).

### shalebean wax — `ShalebeanWax`

**Where it comes from:** Harvest ShalebeanCrop in current Beating biome patches: 1 ShalebeanWax and 1 ShalebeanSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Apply consumes one carried unit and applies the payload to self; throwing shatters the payload on the impact cell and can affect other actors. StoneskinEffect subtracts 1 from every incoming damage event for 6 turns, before resistances.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Ordinary use targets self, including harmful payloads; consuming/throwing spends it. Benefits do not imply general NPC self-use AI.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:46880](../Assets/Resources/Content/Blueprints/Objects.json#L46880); [BiomeCrops.json:169](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L169); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [TonicPart.cs:37](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L37); [TonicPart.cs:93](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L93); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176); [StatusTonicPart.cs:28](../Assets/Scripts/Gameplay/Items/StatusTonicPart.cs#L28); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35).

### cinderpea oil — `CinderpeaOil`

**Where it comes from:** Harvest CinderpeaCrop in current Beating biome patches: 1 CinderpeaOil and 1 CinderpeaSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Apply consumes one carried unit and applies the payload to self; throwing shatters the payload on the impact cell and can affect other actors. BurningEffect at intensity 0.35; ongoing fire damage and thermal interactions.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Ordinary use targets self, including harmful payloads; consuming/throwing spends it. Benefits do not imply general NPC self-use AI.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:47459](../Assets/Resources/Content/Blueprints/Objects.json#L47459); [BiomeCrops.json:199](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L199); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [TonicPart.cs:37](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L37); [TonicPart.cs:93](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L93); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176); [StatusTonicPart.cs:28](../Assets/Scripts/Gameplay/Items/StatusTonicPart.cs#L28); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35).

### knitmoss pad — `KnitmossPad`

**Where it comes from:** Harvest KnitmossCrop in current Grovelands biome patches: 2 KnitmossPad and 1 KnitmossSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Apply consumes one carried unit and applies the payload to self; throwing shatters the payload on the impact cell and can affect other actors. Instant 2d4 HP healing capped at maximum.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Ordinary use targets self, including harmful payloads; consuming/throwing spends it. Benefits do not imply general NPC self-use AI.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:48315](../Assets/Resources/Content/Blueprints/Objects.json#L48315); [BiomeCrops.json:244](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L244); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [TonicPart.cs:37](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L37); [TonicPart.cs:93](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L93); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176).

### absentmint leaf — `AbsentmintLeaf`

**Where it comes from:** Harvest AbsentmintCrop in current Overwrit biome patches: 2 AbsentmintLeaf and 1 AbsentmintSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Apply consumes one carried unit and applies the payload to self; throwing shatters the payload on the impact cell and can affect other actors. Removes ConfusedEffect instances.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Ordinary use targets self, including harmful payloads; consuming/throwing spends it. Benefits do not imply general NPC self-use AI.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:49403](../Assets/Resources/Content/Blueprints/Objects.json#L49403); [BiomeCrops.json:304](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L304); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [TonicPart.cs:37](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L37); [TonicPart.cs:93](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L93); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176); [CureTonicPart.cs:27](../Assets/Scripts/Gameplay/Items/CureTonicPart.cs#L27).

### margincress ribbon — `MargincressRibbon`

**Where it comes from:** Harvest MargincressCrop in current Overwrit biome patches: 2 MargincressRibbon and 1 MargincressSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Apply consumes one carried unit and applies the payload to self; throwing shatters the payload on the impact cell and can affect other actors. Removes BleedingEffect instances.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Ordinary use targets self, including harmful payloads; consuming/throwing spends it. Benefits do not imply general NPC self-use AI.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:49681](../Assets/Resources/Content/Blueprints/Objects.json#L49681); [BiomeCrops.json:319](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L319); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [TonicPart.cs:37](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L37); [TonicPart.cs:93](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L93); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176); [CureTonicPart.cs:27](../Assets/Scripts/Gameplay/Items/CureTonicPart.cs#L27).

### scarlet sundew dew — `ScarletSundewDew`

**Where it comes from:** Harvest ScarletSundewCrop in current Stump biome patches: 1 ScarletSundewDew and 1 ScarletSundewSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Apply consumes one carried unit and applies the payload to self; throwing shatters the payload on the impact cell and can affect other actors. AcidicEffect corrosion 0.35; ongoing typed acid damage and material degradation.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Ordinary use targets self, including harmful payloads; consuming/throwing spends it. Benefits do not imply general NPC self-use AI.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:51312](../Assets/Resources/Content/Blueprints/Objects.json#L51312); [BiomeCrops.json:409](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L409); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [TonicPart.cs:37](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L37); [TonicPart.cs:93](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L93); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176); [StatusTonicPart.cs:28](../Assets/Scripts/Gameplay/Items/StatusTonicPart.cs#L28); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35).

### knucklecap wax — `KnucklecapWax`

**Where it comes from:** Harvest KnucklecapCrop in current Cave biome patches: 1 KnucklecapWax and 1 KnucklecapSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Apply consumes one carried unit and applies the payload to self; throwing shatters the payload on the impact cell and can affect other actors. StoneskinEffect subtracts 2 from every incoming damage event for 4 turns, before resistances.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Ordinary use targets self, including harmful payloads; consuming/throwing spends it. Benefits do not imply general NPC self-use AI.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:52477](../Assets/Resources/Content/Blueprints/Objects.json#L52477); [BiomeCrops.json:469](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L469); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [TonicPart.cs:37](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L37); [TonicPart.cs:93](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L93); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176); [StatusTonicPart.cs:28](../Assets/Scripts/Gameplay/Items/StatusTonicPart.cs#L28); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35).

### sootroot pulp — `SootrootPulp`

**Where it comes from:** Harvest SootrootCrop in current Cave biome patches: 2 SootrootPulp and 1 SootrootSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Apply consumes one carried unit and applies the payload to self; throwing shatters the payload on the impact cell and can affect other actors. Removes BurningEffect instances. Also lowers thermal temperature to at most ambient to prevent immediate reignition. Botanical ink desk service uses 2 SootrootPulp + 1 PitchpodResin + 3 drams to create InkVial while living attendant is present.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Ordinary use targets self, including harmful payloads; consuming/throwing spends it. Benefits do not imply general NPC self-use AI.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:52763](../Assets/Resources/Content/Blueprints/Objects.json#L52763); [BiomeCrops.json:484](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L484); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [TonicPart.cs:37](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L37); [TonicPart.cs:93](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L93); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176); [CureTonicPart.cs:27](../Assets/Scripts/Gameplay/Items/CureTonicPart.cs#L27); [BotanicalInkDeskPart.cs:64](../Assets/Scripts/Gameplay/World/BotanicalInkDeskPart.cs#L64).

### brinebutton paste — `BrinebuttonPaste`

**Where it comes from:** Harvest BrinebuttonCrop in current Cave biome patches: 1 BrinebuttonPaste and 1 BrinebuttonSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Apply consumes one carried unit and applies the payload to self; throwing shatters the payload on the impact cell and can affect other actors. Removes FungalInfectionEffect instances.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Ordinary use targets self, including harmful payloads; consuming/throwing spends it. Benefits do not imply general NPC self-use AI.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:53314](../Assets/Resources/Content/Blueprints/Objects.json#L53314); [BiomeCrops.json:514](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L514); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [TonicPart.cs:37](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L37); [TonicPart.cs:93](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L93); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176); [CureTonicPart.cs:27](../Assets/Scripts/Gameplay/Items/CureTonicPart.cs#L27).

## Reagents

### concentrated mendleaf — `ConcentratedMendleaf`

**Where it comes from:** Inventory preparation of 2 MendleafSprig, near an alchemy still, produces one ConcentratedMendleaf.

**What it actually does:** Alchemy input profile vital:2. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: 2d4 healing.

**Limits and gaps:** A reagent is not directly edible/drinkable. Duplicate quantities do not add potency: max property strength wins. Recipe output can be harmful on self-use.

**Base affordances:** Base Commerce value 6; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:1625](../Assets/Resources/Content/Blueprints/Objects.json#L1625); [BrewingService.cs:80](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L80); [BrewReagentsCommand.cs:109](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/BrewReagentsCommand.cs#L109); [BrewResolver.cs:49](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L49); [BrewResolver.cs:134](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L134); [AlchemyStillPart.cs:38](../Assets/Scripts/Gameplay/Alchemy/AlchemyStillPart.cs#L38); [InventoryUI.Crafting.cs:407](../Assets/Scripts/Presentation/UI/InventoryUI.Crafting.cs#L407); [BrewRules.json:3](../Assets/Resources/Content/Data/Alchemy/BrewRules.json#L3); [BrewingService.cs:325](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L325); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35); [PreparationRecipePart.cs:42](../Assets/Scripts/Gameplay/Preparation/PreparationRecipePart.cs#L42); [PreparationRecipePart.cs:45](../Assets/Scripts/Gameplay/Preparation/PreparationRecipePart.cs#L45).

### cleansed grove pulp — `CleansedGrovePulp`

**Where it comes from:** Inventory preparation of 1 GroveRed, near an alchemy still, produces one CleansedGrovePulp.

**What it actually does:** Alchemy input profile vital:2. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: 2d4 healing.

**Limits and gaps:** A reagent is not directly edible/drinkable. Duplicate quantities do not add potency: max property strength wins. Recipe output can be harmful on self-use.

**Base affordances:** Base Commerce value 12; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:1698](../Assets/Resources/Content/Blueprints/Objects.json#L1698); [BrewingService.cs:80](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L80); [BrewReagentsCommand.cs:109](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/BrewReagentsCommand.cs#L109); [BrewResolver.cs:49](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L49); [BrewResolver.cs:134](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L134); [AlchemyStillPart.cs:38](../Assets/Scripts/Gameplay/Alchemy/AlchemyStillPart.cs#L38); [InventoryUI.Crafting.cs:407](../Assets/Scripts/Presentation/UI/InventoryUI.Crafting.cs#L407); [BrewRules.json:3](../Assets/Resources/Content/Data/Alchemy/BrewRules.json#L3); [BrewingService.cs:325](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L325); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35); [PreparationRecipePart.cs:43](../Assets/Scripts/Gameplay/Preparation/PreparationRecipePart.cs#L43); [PreparationRecipePart.cs:45](../Assets/Scripts/Gameplay/Preparation/PreparationRecipePart.cs#L45).

### fire-moss — `FireMoss`

**Where it comes from:** Current loot/merchant tables: ApothecaryStock, ReagentCommon. Ordinary Villager-faction trade stock also selects this exact ID; eligible world pipelines run TradeStockBuilder.

**What it actually does:** Alchemy input profile heat:2, volatile:1. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Mishap: reagent consumed, no item, and up to 2 self-damage clamped to leave 1 HP; combine with combustible to make burning throwable.

**Limits and gaps:** A reagent is not directly edible/drinkable. Duplicate quantities do not add potency: max property strength wins. Recipe output can be harmful on self-use.

**Base affordances:** Base Commerce value 6; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:5311](../Assets/Resources/Content/Blueprints/Objects.json#L5311); [LootTables.json:746](../Assets/Resources/Content/Data/Loot/LootTables.json#L746); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [BrewingService.cs:80](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L80); [BrewReagentsCommand.cs:109](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/BrewReagentsCommand.cs#L109); [BrewResolver.cs:49](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L49); [BrewResolver.cs:134](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L134); [AlchemyStillPart.cs:38](../Assets/Scripts/Gameplay/Alchemy/AlchemyStillPart.cs#L38); [InventoryUI.Crafting.cs:407](../Assets/Scripts/Presentation/UI/InventoryUI.Crafting.cs#L407); [BrewRules.json:3](../Assets/Resources/Content/Data/Alchemy/BrewRules.json#L3); [TradeStockBuilder.cs:35](../Assets/Scripts/Gameplay/World/Generation/Builders/TradeStockBuilder.cs#L35); [TradeStockBuilder.cs:95](../Assets/Scripts/Gameplay/World/Generation/Builders/TradeStockBuilder.cs#L95); [OverworldZoneManager.cs:438](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L438); [BrewingService.cs:325](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L325); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35); [BrewReagentsCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/BrewReagentsCommand.cs#L176).

### lamp oil — `LampOil`

**Where it comes from:** Current loot/merchant tables: CaveSupplyT1, DeepSupplyT2, ProvisionerStock. Ordinary Villager-faction trade stock also selects this exact ID; eligible world pipelines run TradeStockBuilder.

**What it actually does:** Alchemy input profile combustible:3, viscous:1. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Wet coating potency 1; add heat for burning. Inventory refuel action consumes one measure to add up to 25 fuel (capped) to carried, unlit, partially depleted TorchLight/Fuel items, including WickrushCandle.

**Limits and gaps:** A reagent is not directly edible/drinkable. Duplicate quantities do not add potency: max property strength wins. Recipe output can be harmful on self-use. Not a LiquidVessel: cannot directly pour it as a puddle. FrogOil/WardOil do not substitute in refueling.

**Base affordances:** Base Commerce value 6; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:5345](../Assets/Resources/Content/Blueprints/Objects.json#L5345); [LootTables.json:158](../Assets/Resources/Content/Data/Loot/LootTables.json#L158); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [BrewingService.cs:80](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L80); [BrewReagentsCommand.cs:109](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/BrewReagentsCommand.cs#L109); [BrewResolver.cs:49](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L49); [BrewResolver.cs:134](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L134); [AlchemyStillPart.cs:38](../Assets/Scripts/Gameplay/Alchemy/AlchemyStillPart.cs#L38); [InventoryUI.Crafting.cs:407](../Assets/Scripts/Presentation/UI/InventoryUI.Crafting.cs#L407); [BrewRules.json:3](../Assets/Resources/Content/Data/Alchemy/BrewRules.json#L3); [TradeStockBuilder.cs:35](../Assets/Scripts/Gameplay/World/Generation/Builders/TradeStockBuilder.cs#L35); [TradeStockBuilder.cs:95](../Assets/Scripts/Gameplay/World/Generation/Builders/TradeStockBuilder.cs#L95); [OverworldZoneManager.cs:438](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L438); [BrewingService.cs:325](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L325); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35); [TorchRefuelPart.cs:13](../Assets/Scripts/Gameplay/Preparation/TorchRefuelPart.cs#L13); [TorchRefuelPart.cs:30](../Assets/Scripts/Gameplay/Preparation/TorchRefuelPart.cs#L30).

### ember-fruit — `EmberFruit`

**Where it comes from:** Current loot/merchant tables: ReagentRare, DrifterStock. Ordinary Villager-faction trade stock also selects this exact ID; eligible world pipelines run TradeStockBuilder.

**What it actually does:** Alchemy input profile heat:1, sweet:1. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: 1d4 healing snack; supplies heat to mixed brews.

**Limits and gaps:** A reagent is not directly edible/drinkable. Duplicate quantities do not add potency: max property strength wins. Recipe output can be harmful on self-use.

**Base affordances:** Base Commerce value 6; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:5383](../Assets/Resources/Content/Blueprints/Objects.json#L5383); [LootTables.json:945](../Assets/Resources/Content/Data/Loot/LootTables.json#L945); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [BrewingService.cs:80](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L80); [BrewReagentsCommand.cs:109](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/BrewReagentsCommand.cs#L109); [BrewResolver.cs:49](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L49); [BrewResolver.cs:134](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L134); [AlchemyStillPart.cs:38](../Assets/Scripts/Gameplay/Alchemy/AlchemyStillPart.cs#L38); [InventoryUI.Crafting.cs:407](../Assets/Scripts/Presentation/UI/InventoryUI.Crafting.cs#L407); [BrewRules.json:3](../Assets/Resources/Content/Data/Alchemy/BrewRules.json#L3); [TradeStockBuilder.cs:35](../Assets/Scripts/Gameplay/World/Generation/Builders/TradeStockBuilder.cs#L35); [TradeStockBuilder.cs:95](../Assets/Scripts/Gameplay/World/Generation/Builders/TradeStockBuilder.cs#L95); [OverworldZoneManager.cs:438](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L438); [BrewingService.cs:325](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L325); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35).

### frost lichen — `FrostLichen`

**Where it comes from:** Current loot/merchant tables: ApothecaryStock, ReagentCommon. Ordinary Villager-faction trade stock also selects this exact ID; eligible world pipelines run TradeStockBuilder.

**What it actually does:** Alchemy input profile cold:2. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Frozen coating potency 2 (effect cold caps at 1).

**Limits and gaps:** A reagent is not directly edible/drinkable. Duplicate quantities do not add potency: max property strength wins. Recipe output can be harmful on self-use.

**Base affordances:** Base Commerce value 6; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:5417](../Assets/Resources/Content/Blueprints/Objects.json#L5417); [LootTables.json:750](../Assets/Resources/Content/Data/Loot/LootTables.json#L750); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [BrewingService.cs:80](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L80); [BrewReagentsCommand.cs:109](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/BrewReagentsCommand.cs#L109); [BrewResolver.cs:49](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L49); [BrewResolver.cs:134](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L134); [AlchemyStillPart.cs:38](../Assets/Scripts/Gameplay/Alchemy/AlchemyStillPart.cs#L38); [InventoryUI.Crafting.cs:407](../Assets/Scripts/Presentation/UI/InventoryUI.Crafting.cs#L407); [BrewRules.json:3](../Assets/Resources/Content/Data/Alchemy/BrewRules.json#L3); [TradeStockBuilder.cs:35](../Assets/Scripts/Gameplay/World/Generation/Builders/TradeStockBuilder.cs#L35); [TradeStockBuilder.cs:95](../Assets/Scripts/Gameplay/World/Generation/Builders/TradeStockBuilder.cs#L95); [OverworldZoneManager.cs:438](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L438); [BrewingService.cs:325](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L325); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35).

### glacier salt — `GlacierSalt`

**Where it comes from:** Current loot/merchant tables: ReagentRare, UrnT2. Ordinary Villager-faction trade stock also selects this exact ID; eligible world pipelines run TradeStockBuilder.

**What it actually does:** Alchemy input profile cold:3, binding:1. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Frozen potency 3 plus Stoneskin reduction 1.

**Limits and gaps:** A reagent is not directly edible/drinkable. Duplicate quantities do not add potency: max property strength wins. Recipe output can be harmful on self-use.

**Base affordances:** Base Commerce value 6; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:5451](../Assets/Resources/Content/Blueprints/Objects.json#L5451); [LootTables.json:935](../Assets/Resources/Content/Data/Loot/LootTables.json#L935); [ContainerPlacementService.cs:185](../Assets/Scripts/Gameplay/World/Generation/ContainerPlacementService.cs#L185); [BrewingService.cs:80](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L80); [BrewReagentsCommand.cs:109](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/BrewReagentsCommand.cs#L109); [BrewResolver.cs:49](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L49); [BrewResolver.cs:134](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L134); [AlchemyStillPart.cs:38](../Assets/Scripts/Gameplay/Alchemy/AlchemyStillPart.cs#L38); [InventoryUI.Crafting.cs:407](../Assets/Scripts/Presentation/UI/InventoryUI.Crafting.cs#L407); [BrewRules.json:3](../Assets/Resources/Content/Data/Alchemy/BrewRules.json#L3); [TradeStockBuilder.cs:35](../Assets/Scripts/Gameplay/World/Generation/Builders/TradeStockBuilder.cs#L35); [TradeStockBuilder.cs:95](../Assets/Scripts/Gameplay/World/Generation/Builders/TradeStockBuilder.cs#L95); [OverworldZoneManager.cs:438](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L438); [BrewingService.cs:325](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L325); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35).

### glimmer-brine — `GlimmerBrine`

**Where it comes from:** Current loot/merchant tables: ReagentRare, WellKeeperStock, CuratorStock, EchoStock. Ordinary Villager-faction trade stock also selects this exact ID; eligible world pipelines run TradeStockBuilder.

**What it actually does:** Alchemy input profile corrosive:2, conductive:2. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Acidic potency 2 plus Electrified charge 2.

**Limits and gaps:** A reagent is not directly edible/drinkable. Duplicate quantities do not add potency: max property strength wins. Recipe output can be harmful on self-use.

**Base affordances:** Base Commerce value 6; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:5485](../Assets/Resources/Content/Blueprints/Objects.json#L5485); [LootTables.json:940](../Assets/Resources/Content/Data/Loot/LootTables.json#L940); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [BrewingService.cs:80](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L80); [BrewReagentsCommand.cs:109](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/BrewReagentsCommand.cs#L109); [BrewResolver.cs:49](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L49); [BrewResolver.cs:134](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L134); [AlchemyStillPart.cs:38](../Assets/Scripts/Gameplay/Alchemy/AlchemyStillPart.cs#L38); [InventoryUI.Crafting.cs:407](../Assets/Scripts/Presentation/UI/InventoryUI.Crafting.cs#L407); [BrewRules.json:3](../Assets/Resources/Content/Data/Alchemy/BrewRules.json#L3); [TradeStockBuilder.cs:35](../Assets/Scripts/Gameplay/World/Generation/Builders/TradeStockBuilder.cs#L35); [TradeStockBuilder.cs:95](../Assets/Scripts/Gameplay/World/Generation/Builders/TradeStockBuilder.cs#L95); [OverworldZoneManager.cs:438](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L438); [BrewingService.cs:325](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L325); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35).

### spark-root — `SparkRoot`

**Where it comes from:** Current loot/merchant tables: ReagentCommon. Ordinary Villager-faction trade stock also selects this exact ID; eligible world pipelines run TradeStockBuilder.

**What it actually does:** Alchemy input profile conductive:3. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Electrified charge 3.

**Limits and gaps:** A reagent is not directly edible/drinkable. Duplicate quantities do not add potency: max property strength wins. Recipe output can be harmful on self-use.

**Base affordances:** Base Commerce value 6; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:5519](../Assets/Resources/Content/Blueprints/Objects.json#L5519); [LootTables.json:917](../Assets/Resources/Content/Data/Loot/LootTables.json#L917); [ContainerPlacementService.cs:185](../Assets/Scripts/Gameplay/World/Generation/ContainerPlacementService.cs#L185); [BrewingService.cs:80](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L80); [BrewReagentsCommand.cs:109](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/BrewReagentsCommand.cs#L109); [BrewResolver.cs:49](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L49); [BrewResolver.cs:134](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L134); [AlchemyStillPart.cs:38](../Assets/Scripts/Gameplay/Alchemy/AlchemyStillPart.cs#L38); [InventoryUI.Crafting.cs:407](../Assets/Scripts/Presentation/UI/InventoryUI.Crafting.cs#L407); [BrewRules.json:3](../Assets/Resources/Content/Data/Alchemy/BrewRules.json#L3); [TradeStockBuilder.cs:35](../Assets/Scripts/Gameplay/World/Generation/Builders/TradeStockBuilder.cs#L35); [TradeStockBuilder.cs:95](../Assets/Scripts/Gameplay/World/Generation/Builders/TradeStockBuilder.cs#L95); [OverworldZoneManager.cs:438](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L438); [BrewingService.cs:325](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L325); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35).

### venom gland — `VenomGland`

**Where it comes from:** Current loot/merchant tables: ApothecaryStock, ReagentRare. Ordinary Villager-faction trade stock also selects this exact ID; eligible world pipelines run TradeStockBuilder.

**What it actually does:** Alchemy input profile toxic:2. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Poison potency 2: 7 turns at default 1d3 damage.

**Limits and gaps:** A reagent is not directly edible/drinkable. Duplicate quantities do not add potency: max property strength wins. Recipe output can be harmful on self-use.

**Base affordances:** Base Commerce value 6; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:5553](../Assets/Resources/Content/Blueprints/Objects.json#L5553); [LootTables.json:742](../Assets/Resources/Content/Data/Loot/LootTables.json#L742); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [BrewingService.cs:80](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L80); [BrewReagentsCommand.cs:109](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/BrewReagentsCommand.cs#L109); [BrewResolver.cs:49](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L49); [BrewResolver.cs:134](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L134); [AlchemyStillPart.cs:38](../Assets/Scripts/Gameplay/Alchemy/AlchemyStillPart.cs#L38); [InventoryUI.Crafting.cs:407](../Assets/Scripts/Presentation/UI/InventoryUI.Crafting.cs#L407); [BrewRules.json:3](../Assets/Resources/Content/Data/Alchemy/BrewRules.json#L3); [TradeStockBuilder.cs:35](../Assets/Scripts/Gameplay/World/Generation/Builders/TradeStockBuilder.cs#L35); [TradeStockBuilder.cs:95](../Assets/Scripts/Gameplay/World/Generation/Builders/TradeStockBuilder.cs#L95); [OverworldZoneManager.cs:438](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L438); [BrewingService.cs:325](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L325); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35).

### bog sap — `BogSap`

**Where it comes from:** Current loot/merchant tables: ReagentCommon, HollowLogT1. Ordinary Villager-faction trade stock also selects this exact ID; eligible world pipelines run TradeStockBuilder.

**What it actually does:** Alchemy input profile toxic:1, viscous:2. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Poison potency 1 (5 turns 1d3) plus Wet potency 2.

**Limits and gaps:** A reagent is not directly edible/drinkable. Duplicate quantities do not add potency: max property strength wins. Recipe output can be harmful on self-use.

**Base affordances:** Base Commerce value 6; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:5587](../Assets/Resources/Content/Blueprints/Objects.json#L5587); [LootTables.json:907](../Assets/Resources/Content/Data/Loot/LootTables.json#L907); [ContainerPlacementService.cs:185](../Assets/Scripts/Gameplay/World/Generation/ContainerPlacementService.cs#L185); [BrewingService.cs:80](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L80); [BrewReagentsCommand.cs:109](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/BrewReagentsCommand.cs#L109); [BrewResolver.cs:49](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L49); [BrewResolver.cs:134](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L134); [AlchemyStillPart.cs:38](../Assets/Scripts/Gameplay/Alchemy/AlchemyStillPart.cs#L38); [InventoryUI.Crafting.cs:407](../Assets/Scripts/Presentation/UI/InventoryUI.Crafting.cs#L407); [BrewRules.json:3](../Assets/Resources/Content/Data/Alchemy/BrewRules.json#L3); [TradeStockBuilder.cs:35](../Assets/Scripts/Gameplay/World/Generation/Builders/TradeStockBuilder.cs#L35); [TradeStockBuilder.cs:95](../Assets/Scripts/Gameplay/World/Generation/Builders/TradeStockBuilder.cs#L95); [OverworldZoneManager.cs:438](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L438); [BrewingService.cs:325](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L325); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35).

### heartroot — `CandyHeartRoot`

**Where it comes from:** Current loot/merchant tables: ZigguratVaultT2, BasketT2, EnvoyStock. Ordinary Villager-faction trade stock also selects this exact ID; eligible world pipelines run TradeStockBuilder.

**What it actually does:** Alchemy input profile vital:2, sweet:1. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: 2d4 healing; duplicate healing rules use maximum, not 3d4.

**Limits and gaps:** A reagent is not directly edible/drinkable. Duplicate quantities do not add potency: max property strength wins. Recipe output can be harmful on self-use.

**Base affordances:** Base Commerce value 6; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:5621](../Assets/Resources/Content/Blueprints/Objects.json#L5621); [LootTables.json:408](../Assets/Resources/Content/Data/Loot/LootTables.json#L408); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [BrewingService.cs:80](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L80); [BrewReagentsCommand.cs:109](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/BrewReagentsCommand.cs#L109); [BrewResolver.cs:49](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L49); [BrewResolver.cs:134](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L134); [AlchemyStillPart.cs:38](../Assets/Scripts/Gameplay/Alchemy/AlchemyStillPart.cs#L38); [InventoryUI.Crafting.cs:407](../Assets/Scripts/Presentation/UI/InventoryUI.Crafting.cs#L407); [BrewRules.json:3](../Assets/Resources/Content/Data/Alchemy/BrewRules.json#L3); [TradeStockBuilder.cs:35](../Assets/Scripts/Gameplay/World/Generation/Builders/TradeStockBuilder.cs#L35); [TradeStockBuilder.cs:95](../Assets/Scripts/Gameplay/World/Generation/Builders/TradeStockBuilder.cs#L95); [OverworldZoneManager.cs:438](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L438); [BrewingService.cs:325](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L325); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35).

### mendleaf sprig — `MendleafSprig`

**Where it comes from:** Current loot/merchant tables: ApothecaryStock, ReagentRare, HermitStock. Ordinary Villager-faction trade stock also selects this exact ID; eligible world pipelines run TradeStockBuilder.Harvest current MendleafPlant world objects; consumes the finite source, yields carried items or drops overflow.

**What it actually does:** Alchemy input profile vital:1. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: 1d4 healing. Prepare two sprigs at a still into one ConcentratedMendleaf (vital:2); this is the explicit route for increasing healing potency.

**Limits and gaps:** A reagent is not directly edible/drinkable. Duplicate quantities do not add potency: max property strength wins. Recipe output can be harmful on self-use.

**Base affordances:** Base Commerce value 6; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:5655](../Assets/Resources/Content/Blueprints/Objects.json#L5655); [LootTables.json:738](../Assets/Resources/Content/Data/Loot/LootTables.json#L738); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [BrewingService.cs:80](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L80); [BrewReagentsCommand.cs:109](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/BrewReagentsCommand.cs#L109); [BrewResolver.cs:49](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L49); [BrewResolver.cs:134](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L134); [AlchemyStillPart.cs:38](../Assets/Scripts/Gameplay/Alchemy/AlchemyStillPart.cs#L38); [InventoryUI.Crafting.cs:407](../Assets/Scripts/Presentation/UI/InventoryUI.Crafting.cs#L407); [BrewRules.json:3](../Assets/Resources/Content/Data/Alchemy/BrewRules.json#L3); [TradeStockBuilder.cs:35](../Assets/Scripts/Gameplay/World/Generation/Builders/TradeStockBuilder.cs#L35); [TradeStockBuilder.cs:95](../Assets/Scripts/Gameplay/World/Generation/Builders/TradeStockBuilder.cs#L95); [OverworldZoneManager.cs:438](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L438); [BrewingService.cs:325](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L325); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35); [PreparationRecipePart.cs:42](../Assets/Scripts/Gameplay/Preparation/PreparationRecipePart.cs#L42); [Objects.json:19273](../Assets/Resources/Content/Blueprints/Objects.json#L19273); [HarvestablePart.cs:66](../Assets/Scripts/Gameplay/Items/HarvestablePart.cs#L66).

### stoneburr seed — `StoneburrSeed`

**Where it comes from:** Current loot/merchant tables: ReagentCommon, FarmerStock. Ordinary Villager-faction trade stock also selects this exact ID; eligible world pipelines run TradeStockBuilder.

**What it actually does:** Alchemy input profile binding:2. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Stoneskin reduction 2 for 30 turns.

**Limits and gaps:** A reagent is not directly edible/drinkable. Duplicate quantities do not add potency: max property strength wins. Recipe output can be harmful on self-use.

**Base affordances:** Base Commerce value 6; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:5698](../Assets/Resources/Content/Blueprints/Objects.json#L5698); [LootTables.json:912](../Assets/Resources/Content/Data/Loot/LootTables.json#L912); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [BrewingService.cs:80](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L80); [BrewReagentsCommand.cs:109](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/BrewReagentsCommand.cs#L109); [BrewResolver.cs:49](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L49); [BrewResolver.cs:134](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L134); [AlchemyStillPart.cs:38](../Assets/Scripts/Gameplay/Alchemy/AlchemyStillPart.cs#L38); [InventoryUI.Crafting.cs:407](../Assets/Scripts/Presentation/UI/InventoryUI.Crafting.cs#L407); [BrewRules.json:3](../Assets/Resources/Content/Data/Alchemy/BrewRules.json#L3); [TradeStockBuilder.cs:35](../Assets/Scripts/Gameplay/World/Generation/Builders/TradeStockBuilder.cs#L35); [TradeStockBuilder.cs:95](../Assets/Scripts/Gameplay/World/Generation/Builders/TradeStockBuilder.cs#L95); [OverworldZoneManager.cs:438](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L438); [BrewingService.cs:325](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L325); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35).

### blastcap spore — `BlastcapSpore`

**Where it comes from:** Current loot/merchant tables: CultCacheT2, ReagentRare. Ordinary Villager-faction trade stock also selects this exact ID; eligible world pipelines run TradeStockBuilder.

**What it actually does:** Alchemy input profile volatile:2, combustible:1. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Mishap: reagent consumed, no item, and up to 2 self-damage clamped to leave 1 HP; combine with heat to make burning throwable.

**Limits and gaps:** A reagent is not directly edible/drinkable. Duplicate quantities do not add potency: max property strength wins. Recipe output can be harmful on self-use.

**Base affordances:** Base Commerce value 6; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:5732](../Assets/Resources/Content/Blueprints/Objects.json#L5732); [LootTables.json:544](../Assets/Resources/Content/Data/Loot/LootTables.json#L544); [ContainerPlacementService.cs:185](../Assets/Scripts/Gameplay/World/Generation/ContainerPlacementService.cs#L185); [BrewingService.cs:80](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L80); [BrewReagentsCommand.cs:109](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/BrewReagentsCommand.cs#L109); [BrewResolver.cs:49](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L49); [BrewResolver.cs:134](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L134); [AlchemyStillPart.cs:38](../Assets/Scripts/Gameplay/Alchemy/AlchemyStillPart.cs#L38); [InventoryUI.Crafting.cs:407](../Assets/Scripts/Presentation/UI/InventoryUI.Crafting.cs#L407); [BrewRules.json:3](../Assets/Resources/Content/Data/Alchemy/BrewRules.json#L3); [TradeStockBuilder.cs:35](../Assets/Scripts/Gameplay/World/Generation/Builders/TradeStockBuilder.cs#L35); [TradeStockBuilder.cs:95](../Assets/Scripts/Gameplay/World/Generation/Builders/TradeStockBuilder.cs#L95); [OverworldZoneManager.cs:438](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L438); [BrewingService.cs:325](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L325); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35); [BrewReagentsCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/BrewReagentsCommand.cs#L176).

### grove-red — `GroveRed`

**Where it comes from:** Harvest GroveRedGrowth (red growth) at Grovelands grove edges; its HarvestablePart yields 1–2.

**What it actually does:** Alchemy input profile vital:3, toxic:1. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Poison potency 1; its vital:3 is blocked by toxic and does not heal. Prepare one at a still into CleansedGrovePulp, removing toxic from the ingredient and leaving vital:2.

**Limits and gaps:** A reagent is not directly edible/drinkable. Duplicate quantities do not add potency: max property strength wins. Recipe output can be harmful on self-use. Not food: no Eat action. Raw brewing is poisonous by design; healing claims in flavor text are not its untreated effect.

**Base affordances:** Base Commerce value 18; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:35119](../Assets/Resources/Content/Blueprints/Objects.json#L35119); [BrewingService.cs:80](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L80); [BrewReagentsCommand.cs:109](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/BrewReagentsCommand.cs#L109); [BrewResolver.cs:49](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L49); [BrewResolver.cs:134](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L134); [AlchemyStillPart.cs:38](../Assets/Scripts/Gameplay/Alchemy/AlchemyStillPart.cs#L38); [InventoryUI.Crafting.cs:407](../Assets/Scripts/Presentation/UI/InventoryUI.Crafting.cs#L407); [BrewRules.json:3](../Assets/Resources/Content/Data/Alchemy/BrewRules.json#L3); [BrewingService.cs:325](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L325); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35); [Objects.json:35153](../Assets/Resources/Content/Blueprints/Objects.json#L35153); [HarvestablePart.cs:66](../Assets/Scripts/Gameplay/Items/HarvestablePart.cs#L66); [PreparationRecipePart.cs:43](../Assets/Scripts/Gameplay/Preparation/PreparationRecipePart.cs#L43); [GrovelandsFormationBuilder.cs:150](../Assets/Scripts/Gameplay/World/Generation/Builders/GrovelandsFormationBuilder.cs#L150).

### shambler spore-sac — `ShamblerSporeSac`

**Where it comes from:** Harvest current Shambler/SporeShambler corpse: one sac at 80%/100% respectively after guaranteed corpse; ordinary Grovelands/deep population includes those creatures.

**What it actually does:** Alchemy input profile toxic:2, volatile:1. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Poison potency 2 throwable (7 turns at default 1d3).

**Limits and gaps:** A reagent is not directly edible/drinkable. Duplicate quantities do not add potency: max property strength wins. Recipe output can be harmful on self-use.

**Base affordances:** Base Commerce value 13; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:35241](../Assets/Resources/Content/Blueprints/Objects.json#L35241); [BrewingService.cs:80](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L80); [BrewReagentsCommand.cs:109](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/BrewReagentsCommand.cs#L109); [BrewResolver.cs:49](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L49); [BrewResolver.cs:134](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L134); [AlchemyStillPart.cs:38](../Assets/Scripts/Gameplay/Alchemy/AlchemyStillPart.cs#L38); [InventoryUI.Crafting.cs:407](../Assets/Scripts/Presentation/UI/InventoryUI.Crafting.cs#L407); [BrewRules.json:3](../Assets/Resources/Content/Data/Alchemy/BrewRules.json#L3); [BrewingService.cs:325](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L325); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35); [Objects.json:35186](../Assets/Resources/Content/Blueprints/Objects.json#L35186); [Objects.json:19853](../Assets/Resources/Content/Blueprints/Objects.json#L19853); [PopulationTable.cs:650](../Assets/Scripts/Data/Tables/PopulationTable.cs#L650); [PopulationTable.cs:930](../Assets/Scripts/Data/Tables/PopulationTable.cs#L930); [CorpsePart.cs:127](../Assets/Scripts/Gameplay/Entities/CorpsePart.cs#L127); [HarvestablePart.cs:66](../Assets/Scripts/Gameplay/Items/HarvestablePart.cs#L66).

### seamleaf sprig — `SeamleafSprig`

**Where it comes from:** Harvest ripe knotflax/hearthbulb/seamleaf at the western Morrowfast allotment (Overworld.2.6.0): two produce plus one corresponding seed; replant prepared beds.

**What it actually does:** Alchemy input profile vital:2. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: 2d4 healing.

**Limits and gaps:** A reagent is not directly edible/drinkable. Duplicate quantities do not add potency: max property strength wins. Recipe output can be harmful on self-use.

**Base affordances:** Base Commerce value 5; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:42650](../Assets/Resources/Content/Blueprints/Objects.json#L42650); [BrewingService.cs:80](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L80); [BrewReagentsCommand.cs:109](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/BrewReagentsCommand.cs#L109); [BrewResolver.cs:49](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L49); [BrewResolver.cs:134](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L134); [AlchemyStillPart.cs:38](../Assets/Scripts/Gameplay/Alchemy/AlchemyStillPart.cs#L38); [InventoryUI.Crafting.cs:407](../Assets/Scripts/Presentation/UI/InventoryUI.Crafting.cs#L407); [BrewRules.json:3](../Assets/Resources/Content/Data/Alchemy/BrewRules.json#L3); [BrewingService.cs:325](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L325); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35); [RepairCultivationSite.cs:13](../Assets/Scripts/Gameplay/World/RepairCultivationSite.cs#L13); [RepairCultivationSite.cs:19](../Assets/Scripts/Gameplay/World/RepairCultivationSite.cs#L19); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105).

### pitchpod resin — `PitchpodResin`

**Where it comes from:** Harvest PitchpodCrop in current Spread biome patches: 2 PitchpodResin and 1 PitchpodSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Alchemy input profile heat:1,combustible:1. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Burning coating intensity 1. Botanical ink desk service uses 2 SootrootPulp + 1 PitchpodResin + 3 drams to create InkVial while living attendant is present.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. A reagent is not directly edible/drinkable. Duplicate quantities do not add potency: max property strength wins. Recipe output can be harmful on self-use.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:44074](../Assets/Resources/Content/Blueprints/Objects.json#L44074); [BiomeCrops.json:19](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L19); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [BrewingService.cs:80](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L80); [BrewReagentsCommand.cs:109](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/BrewReagentsCommand.cs#L109); [BrewResolver.cs:49](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L49); [BrewResolver.cs:134](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L134); [AlchemyStillPart.cs:38](../Assets/Scripts/Gameplay/Alchemy/AlchemyStillPart.cs#L38); [InventoryUI.Crafting.cs:407](../Assets/Scripts/Presentation/UI/InventoryUI.Crafting.cs#L407); [BrewRules.json:3](../Assets/Resources/Content/Data/Alchemy/BrewRules.json#L3); [BrewingService.cs:325](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L325); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35); [BotanicalInkDeskPart.cs:64](../Assets/Scripts/Gameplay/World/BotanicalInkDeskPart.cs#L64).

### chillcress tip — `ChillcressTip`

**Where it comes from:** Harvest ChillcressCrop in current Sodden biome patches: 2 ChillcressTip and 1 ChillcressSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Alchemy input profile cold:1. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Frozen coating cold 1.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. A reagent is not directly edible/drinkable. Duplicate quantities do not add potency: max property strength wins. Recipe output can be harmful on self-use.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:45780](../Assets/Resources/Content/Blueprints/Objects.json#L45780); [BiomeCrops.json:109](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L109); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [BrewingService.cs:80](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L80); [BrewReagentsCommand.cs:109](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/BrewReagentsCommand.cs#L109); [BrewResolver.cs:49](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L49); [BrewResolver.cs:134](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L134); [AlchemyStillPart.cs:38](../Assets/Scripts/Gameplay/Alchemy/AlchemyStillPart.cs#L38); [InventoryUI.Crafting.cs:407](../Assets/Scripts/Presentation/UI/InventoryUI.Crafting.cs#L407); [BrewRules.json:3](../Assets/Resources/Content/Data/Alchemy/BrewRules.json#L3); [BrewingService.cs:325](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L325); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35).

### slipsedge gel — `SlipsedgeGel`

**Where it comes from:** Harvest SlipsedgeCrop in current Sodden biome patches: 2 SlipsedgeGel and 1 SlipsedgeSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Alchemy input profile viscous:1. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Wet coating moisture 1.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. A reagent is not directly edible/drinkable. Duplicate quantities do not add potency: max property strength wins. Recipe output can be harmful on self-use.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:46049](../Assets/Resources/Content/Blueprints/Objects.json#L46049); [BiomeCrops.json:124](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L124); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [BrewingService.cs:80](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L80); [BrewReagentsCommand.cs:109](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/BrewReagentsCommand.cs#L109); [BrewResolver.cs:49](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L49); [BrewResolver.cs:134](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L134); [AlchemyStillPart.cs:38](../Assets/Scripts/Gameplay/Alchemy/AlchemyStillPart.cs#L38); [InventoryUI.Crafting.cs:407](../Assets/Scripts/Presentation/UI/InventoryUI.Crafting.cs#L407); [BrewRules.json:3](../Assets/Resources/Content/Data/Alchemy/BrewRules.json#L3); [BrewingService.cs:325](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L325); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35).

### sourmantle fold — `SourmantleFold`

**Where it comes from:** Harvest SourmantleCrop in current Grovelands biome patches: 2 SourmantleFold and 1 SourmantleSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Alchemy input profile corrosive:1. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Acidic tonic corrosion 1.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. A reagent is not directly edible/drinkable. Duplicate quantities do not add potency: max property strength wins. Recipe output can be harmful on self-use.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:48588](../Assets/Resources/Content/Blueprints/Objects.json#L48588); [BiomeCrops.json:259](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L259); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [BrewingService.cs:80](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L80); [BrewReagentsCommand.cs:109](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/BrewReagentsCommand.cs#L109); [BrewResolver.cs:49](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L49); [BrewResolver.cs:134](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L134); [AlchemyStillPart.cs:38](../Assets/Scripts/Gameplay/Alchemy/AlchemyStillPart.cs#L38); [InventoryUI.Crafting.cs:407](../Assets/Scripts/Presentation/UI/InventoryUI.Crafting.cs#L407); [BrewRules.json:3](../Assets/Resources/Content/Data/Alchemy/BrewRules.json#L3); [BrewingService.cs:325](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L325); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35).

### binderroot knot — `BinderrootKnot`

**Where it comes from:** Harvest BinderrootCrop in current Overwrit biome patches: 1 BinderrootKnot and 1 BinderrootSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Alchemy input profile binding:1. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Stoneskin reduction 1 for 30 turns.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. A reagent is not directly edible/drinkable. Duplicate quantities do not add potency: max property strength wins. Recipe output can be harmful on self-use.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:49959](../Assets/Resources/Content/Blueprints/Objects.json#L49959); [BiomeCrops.json:334](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L334); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [BrewingService.cs:80](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L80); [BrewReagentsCommand.cs:109](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/BrewReagentsCommand.cs#L109); [BrewResolver.cs:49](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L49); [BrewResolver.cs:134](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L134); [AlchemyStillPart.cs:38](../Assets/Scripts/Gameplay/Alchemy/AlchemyStillPart.cs#L38); [InventoryUI.Crafting.cs:407](../Assets/Scripts/Presentation/UI/InventoryUI.Crafting.cs#L407); [BrewRules.json:3](../Assets/Resources/Content/Data/Alchemy/BrewRules.json#L3); [BrewingService.cs:325](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L325); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35).

### prismreed pith — `PrismreedPith`

**Where it comes from:** Harvest PrismreedCrop in current Stump biome patches: 2 PrismreedPith and 1 PrismreedSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Alchemy input profile conductive:1. Select carried reagent(s) in the Crafting Brew panel: one brew works anywhere; non-Food batches above one require a nearby still, while Food batches work anywhere. One of each selected stack is consumed per completed brew. Brewed alone: Electrified tonic charge 1.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. A reagent is not directly edible/drinkable. Duplicate quantities do not add potency: max property strength wins. Recipe output can be harmful on self-use.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:51043](../Assets/Resources/Content/Blueprints/Objects.json#L51043); [BiomeCrops.json:394](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L394); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [BrewingService.cs:80](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L80); [BrewReagentsCommand.cs:109](../Assets/Scripts/Gameplay/Inventory/Commands/Actions/BrewReagentsCommand.cs#L109); [BrewResolver.cs:49](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L49); [BrewResolver.cs:134](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L134); [AlchemyStillPart.cs:38](../Assets/Scripts/Gameplay/Alchemy/AlchemyStillPart.cs#L38); [InventoryUI.Crafting.cs:407](../Assets/Scripts/Presentation/UI/InventoryUI.Crafting.cs#L407); [BrewRules.json:3](../Assets/Resources/Content/Data/Alchemy/BrewRules.json#L3); [BrewingService.cs:325](../Assets/Scripts/Gameplay/Alchemy/BrewingService.cs#L325); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35).

## Foods and meals

### starapple — `Starapple`

**Where it comes from:** Current loot/merchant tables: ProvisionerStock, FarmerStock.

**What it actually does:** Eat one carried unit: restores 2d4 HP, capped at maximum. Cook this carried stack beside a cooking station into RoastedStarapple.

**Limits and gaps:** HP healing, not hunger/nutrition. Can be spent at full HP. No general autonomous NPC eating AI found.

**Base affordances:** Base Commerce value 5; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:3851](../Assets/Resources/Content/Blueprints/Objects.json#L3851); [LootTables.json:856](../Assets/Resources/Content/Data/Loot/LootTables.json#L856); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [FoodPart.cs:39](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L39); [FoodPart.cs:67](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L67); [CookablePart.cs:15](../Assets/Scripts/Gameplay/Items/CookablePart.cs#L15); [CookingService.cs:14](../Assets/Scripts/Gameplay/Items/CookingService.cs#L14).

### gladroot — `CandyCarrot`

**Where it comes from:** Plant its seed and water CandyCarrotCrop; legacy crop automatically releases produce on completing second stage.

**What it actually does:** Eat one carried unit: restores 1d4 HP, capped at maximum.

**Limits and gaps:** HP healing, not hunger/nutrition. Can be spent at full HP. No general autonomous NPC eating AI found.

**Base affordances:** Base Commerce value 8; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:6050](../Assets/Resources/Content/Blueprints/Objects.json#L6050); [FoodPart.cs:39](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L39); [FoodPart.cs:67](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L67); [Objects.json:5904](../Assets/Resources/Content/Blueprints/Objects.json#L5904); [CropTime.cs:76](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L76); [CropSystem.cs:1](../Assets/Scripts/Gameplay/Farming/CropSystem.cs#L1).

### toasted emberwheat — `ToastedEmberwheat`

**Where it comes from:** Current loot/merchant tables: WaysideCookStock. Cook Emberwheat stack beside an eligible campfire/hearth/stove via Cookable → CookingService; output count equals input count.

**What it actually does:** Eat one carried unit: restores 3d4 HP, capped at maximum. Prepared meal: +20 HeatResistance for 100 owner turns, replacing previous prepared meal.

**Limits and gaps:** HP healing, not hunger/nutrition. Can be spent at full HP. No general autonomous NPC eating AI found.

**Base affordances:** Base Commerce value 12; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:6213](../Assets/Resources/Content/Blueprints/Objects.json#L6213); [LootTables.json:3193](../Assets/Resources/Content/Data/Loot/LootTables.json#L3193); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [FoodPart.cs:39](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L39); [FoodPart.cs:67](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L67); [FoodPart.cs:89](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L89); [PreparedMealEffect.cs:1](../Assets/Scripts/Gameplay/Effects/Concrete/PreparedMealEffect.cs#L1); [Objects.json:6298](../Assets/Resources/Content/Blueprints/Objects.json#L6298); [CookablePart.cs:15](../Assets/Scripts/Gameplay/Items/CookablePart.cs#L15); [CookingService.cs:27](../Assets/Scripts/Gameplay/Items/CookingService.cs#L27).

### emberwheat sheaf — `Emberwheat`

**Where it comes from:** Current loot/merchant tables: WaysideCookStock. Plant its seed and water EmberwheatCrop; legacy crop automatically releases produce on completing second stage.

**What it actually does:** Eat one carried unit: restores 2d4 HP, capped at maximum. Cook this carried stack beside a cooking station into ToastedEmberwheat. 2 grain + claspbean + fee starts FieldMeal batch; Gantry supply request consumes 2 for 6 drams + HealingTonic.

**Limits and gaps:** HP healing, not hunger/nutrition. Can be spent at full HP. No general autonomous NPC eating AI found.

**Base affordances:** Base Commerce value 10; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:6298](../Assets/Resources/Content/Blueprints/Objects.json#L6298); [LootTables.json:3183](../Assets/Resources/Content/Data/Loot/LootTables.json#L3183); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [FoodPart.cs:39](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L39); [FoodPart.cs:67](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L67); [CookablePart.cs:15](../Assets/Scripts/Gameplay/Items/CookablePart.cs#L15); [CookingService.cs:14](../Assets/Scripts/Gameplay/Items/CookingService.cs#L14); [Objects.json:5977](../Assets/Resources/Content/Blueprints/Objects.json#L5977); [CropTime.cs:76](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L76); [CropSystem.cs:1](../Assets/Scripts/Gameplay/Farming/CropSystem.cs#L1); [KitchenBatchPart.cs:108](../Assets/Scripts/Gameplay/World/KitchenBatchPart.cs#L108); [RegionalSituations.cs:47](../Assets/Scripts/Gameplay/World/RegionalSituations.cs#L47); [RegionalRequestPart.cs:164](../Assets/Scripts/Gameplay/World/RegionalRequestPart.cs#L164).

### mushroom — `Mushroom`

**Where it comes from:** Current loot/merchant tables: MorrowfastProvisionerStock, ProvisionerStock, SackT2, BasketT1, VillagerStock, GnomeStock, WaysideCookStock. Harvest current MushroomRing world objects; consumes the finite source, yields carried items or drops overflow.

**What it actually does:** Eat one carried unit: restores 1d4 HP, capped at maximum. Cook this carried stack beside a cooking station into RoastedMushroom.

**Limits and gaps:** HP healing, not hunger/nutrition. Can be spent at full HP. No general autonomous NPC eating AI found.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:11157](../Assets/Resources/Content/Blueprints/Objects.json#L11157); [LootTables.json:62](../Assets/Resources/Content/Data/Loot/LootTables.json#L62); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [FoodPart.cs:39](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L39); [FoodPart.cs:67](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L67); [CookablePart.cs:15](../Assets/Scripts/Gameplay/Items/CookablePart.cs#L15); [CookingService.cs:14](../Assets/Scripts/Gameplay/Items/CookingService.cs#L14); [Objects.json:28607](../Assets/Resources/Content/Blueprints/Objects.json#L28607); [HarvestablePart.cs:66](../Assets/Scripts/Gameplay/Items/HarvestablePart.cs#L66).

### dried meat — `DriedMeat`

**Where it comes from:** Current loot/merchant tables: MorrowfastProvisionerStock, CaveSupplyT1, HunterCacheT1, CampGoodsT1, ProvisionerStock, DeathHumanoidT1, CrateT1, SackT1, VillagerStock, MerchantStock, WaysideCookStock.

**What it actually does:** Eat one carried unit: restores 3d4 HP, capped at maximum. Can be dropped on safe public ground to divert an eligible configured furrowstalker; finite feeding consumes meat and does not heal the predator.

**Limits and gaps:** HP healing, not hunger/nutrition. Can be spent at full HP. No general autonomous NPC eating AI found.

**Base affordances:** Base Commerce value 7; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:11235](../Assets/Resources/Content/Blueprints/Objects.json#L11235); [LootTables.json:67](../Assets/Resources/Content/Data/Loot/LootTables.json#L67); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [FoodPart.cs:39](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L39); [FoodPart.cs:67](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L67); [SpreadPredatorPart.cs:306](../Assets/Scripts/Gameplay/AI/SpreadPredatorPart.cs#L306); [SpreadPredatorPart.cs:394](../Assets/Scripts/Gameplay/AI/SpreadPredatorPart.cs#L394); [SpreadExplorationBuilder.cs:94](../Assets/Scripts/Gameplay/World/Generation/Builders/SpreadExplorationBuilder.cs#L94).

### raw meat — `RawMeat`

**Where it comes from:** Current loot/merchant tables: ProvisionerStock, DeathBeastT1, DeathBeastT2, DeathBeastT3, WaysideCookStock.

**What it actually does:** Eat one carried unit: restores 1d4 HP, capped at maximum. Cook this carried stack beside a cooking station into CookedMeat. Can be dropped on safe public ground to divert an eligible configured furrowstalker; finite feeding consumes meat and does not heal the predator.

**Limits and gaps:** HP healing, not hunger/nutrition. Can be spent at full HP. No general autonomous NPC eating AI found.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:22938](../Assets/Resources/Content/Blueprints/Objects.json#L22938); [LootTables.json:865](../Assets/Resources/Content/Data/Loot/LootTables.json#L865); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [FoodPart.cs:39](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L39); [FoodPart.cs:67](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L67); [CookablePart.cs:15](../Assets/Scripts/Gameplay/Items/CookablePart.cs#L15); [CookingService.cs:14](../Assets/Scripts/Gameplay/Items/CookingService.cs#L14); [SpreadPredatorPart.cs:306](../Assets/Scripts/Gameplay/AI/SpreadPredatorPart.cs#L306); [SpreadPredatorPart.cs:394](../Assets/Scripts/Gameplay/AI/SpreadPredatorPart.cs#L394); [SpreadExplorationBuilder.cs:94](../Assets/Scripts/Gameplay/World/Generation/Builders/SpreadExplorationBuilder.cs#L94).

### cooked meat — `CookedMeat`

**Where it comes from:** Current loot/merchant tables: InnkeeperStock. Cook RawMeat stack beside an eligible campfire/hearth/stove via Cookable → CookingService; output count equals input count.

**What it actually does:** Eat one carried unit: restores 3d4 HP, capped at maximum. Prepared meal: +2 Toughness for 100 owner turns, replacing previous prepared meal.

**Limits and gaps:** HP healing, not hunger/nutrition. Can be spent at full HP. No general autonomous NPC eating AI found.

**Base affordances:** Base Commerce value 8; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:23080](../Assets/Resources/Content/Blueprints/Objects.json#L23080); [LootTables.json:2601](../Assets/Resources/Content/Data/Loot/LootTables.json#L2601); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [FoodPart.cs:39](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L39); [FoodPart.cs:67](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L67); [FoodPart.cs:89](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L89); [PreparedMealEffect.cs:1](../Assets/Scripts/Gameplay/Effects/Concrete/PreparedMealEffect.cs#L1); [Objects.json:22938](../Assets/Resources/Content/Blueprints/Objects.json#L22938); [CookablePart.cs:15](../Assets/Scripts/Gameplay/Items/CookablePart.cs#L15); [CookingService.cs:27](../Assets/Scripts/Gameplay/Items/CookingService.cs#L27).

### roasted starapple — `RoastedStarapple`

**Where it comes from:** Current loot/merchant tables: InnkeeperStock, EnvoyStock. Cook Starapple stack beside an eligible campfire/hearth/stove via Cookable → CookingService; output count equals input count.

**What it actually does:** Eat one carried unit: restores 3d4 HP, capped at maximum. Prepared meal: +1 DV for 100 owner turns, replacing previous prepared meal.

**Limits and gaps:** HP healing, not hunger/nutrition. Can be spent at full HP. No general autonomous NPC eating AI found.

**Base affordances:** Base Commerce value 9; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:23191](../Assets/Resources/Content/Blueprints/Objects.json#L23191); [LootTables.json:2607](../Assets/Resources/Content/Data/Loot/LootTables.json#L2607); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [FoodPart.cs:39](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L39); [FoodPart.cs:67](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L67); [FoodPart.cs:89](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L89); [PreparedMealEffect.cs:1](../Assets/Scripts/Gameplay/Effects/Concrete/PreparedMealEffect.cs#L1); [Objects.json:3851](../Assets/Resources/Content/Blueprints/Objects.json#L3851); [CookablePart.cs:15](../Assets/Scripts/Gameplay/Items/CookablePart.cs#L15); [CookingService.cs:27](../Assets/Scripts/Gameplay/Items/CookingService.cs#L27).

### wild berries — `WildBerries`

**Where it comes from:** Current loot/merchant tables: BasketT1, BasketT2, VillagerStock, FarmerStock, GnomeStock. Harvest current BerryBush world objects; consumes the finite source, yields carried items or drops overflow.

**What it actually does:** Eat one carried unit: restores 1d3 HP, capped at maximum.

**Limits and gaps:** HP healing, not hunger/nutrition. Can be spent at full HP. No general autonomous NPC eating AI found.

**Base affordances:** Base Commerce value 2; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:28258](../Assets/Resources/Content/Blueprints/Objects.json#L28258); [LootTables.json:1932](../Assets/Resources/Content/Data/Loot/LootTables.json#L1932); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [FoodPart.cs:39](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L39); [FoodPart.cs:67](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L67); [Objects.json:28382](../Assets/Resources/Content/Blueprints/Objects.json#L28382); [HarvestablePart.cs:66](../Assets/Scripts/Gameplay/Items/HarvestablePart.cs#L66).

### honeycomb — `Honeycomb`

**Where it comes from:** Current loot/merchant tables: BasketT1, BasketT3, InnkeeperStock, EnvoyStock. Harvest current Beehive world objects; consumes the finite source, yields carried items or drops overflow.

**What it actually does:** Eat one carried unit: restores 1d6 HP, capped at maximum.

**Limits and gaps:** HP healing, not hunger/nutrition. Can be spent at full HP. No general autonomous NPC eating AI found.

**Base affordances:** Base Commerce value 6; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:28320](../Assets/Resources/Content/Blueprints/Objects.json#L28320); [LootTables.json:1936](../Assets/Resources/Content/Data/Loot/LootTables.json#L1936); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [FoodPart.cs:39](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L39); [FoodPart.cs:67](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L67); [Objects.json:28492](../Assets/Resources/Content/Blueprints/Objects.json#L28492); [HarvestablePart.cs:66](../Assets/Scripts/Gameplay/Items/HarvestablePart.cs#L66).

### saltbriar sprig — `SaltbriarSprig`

**Where it comes from:** Harvest current Saltbriar world objects; consumes the finite source, yields carried items or drops overflow.

**What it actually does:** Eat one carried unit: restores 1d2 HP, capped at maximum.

**Limits and gaps:** HP healing, not hunger/nutrition. Can be spent at full HP. No general autonomous NPC eating AI found.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:32718](../Assets/Resources/Content/Blueprints/Objects.json#L32718); [FoodPart.cs:39](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L39); [FoodPart.cs:67](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L67); [Objects.json:32751](../Assets/Resources/Content/Blueprints/Objects.json#L32751); [HarvestablePart.cs:66](../Assets/Scripts/Gameplay/Items/HarvestablePart.cs#L66); [PopulationTable.cs:473](../Assets/Scripts/Data/Tables/PopulationTable.cs#L473).

### roasted mushroom — `RoastedMushroom`

**Where it comes from:** Cook Mushroom stack beside an eligible campfire/hearth/stove via Cookable → CookingService; output count equals input count.

**What it actually does:** Eat one carried unit: restores 3d4 HP, capped at maximum. Prepared meal: +20 AcidResistance for 100 owner turns, replacing previous prepared meal.

**Limits and gaps:** HP healing, not hunger/nutrition. Can be spent at full HP. No general autonomous NPC eating AI found.

**Base affordances:** Base Commerce value 8; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:38623](../Assets/Resources/Content/Blueprints/Objects.json#L38623); [FoodPart.cs:39](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L39); [FoodPart.cs:67](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L67); [FoodPart.cs:89](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L89); [PreparedMealEffect.cs:1](../Assets/Scripts/Gameplay/Effects/Concrete/PreparedMealEffect.cs#L1); [Objects.json:11157](../Assets/Resources/Content/Blueprints/Objects.json#L11157); [CookablePart.cs:15](../Assets/Scripts/Gameplay/Items/CookablePart.cs#L15); [CookingService.cs:27](../Assets/Scripts/Gameplay/Items/CookingService.cs#L27).

### hearthbulb — `Hearthbulb`

**Where it comes from:** Harvest ripe knotflax/hearthbulb/seamleaf at the western Morrowfast allotment (Overworld.2.6.0): two produce plus one corresponding seed; replant prepared beds.

**What it actually does:** Eat one carried unit: restores 1d4 HP, capped at maximum. Cook this carried stack beside a cooking station into RoastedHearthbulb.

**Limits and gaps:** HP healing, not hunger/nutrition. Can be spent at full HP. No general autonomous NPC eating AI found.

**Base affordances:** Base Commerce value 5; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:42483](../Assets/Resources/Content/Blueprints/Objects.json#L42483); [FoodPart.cs:39](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L39); [FoodPart.cs:67](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L67); [CookablePart.cs:15](../Assets/Scripts/Gameplay/Items/CookablePart.cs#L15); [CookingService.cs:14](../Assets/Scripts/Gameplay/Items/CookingService.cs#L14); [RepairCultivationSite.cs:13](../Assets/Scripts/Gameplay/World/RepairCultivationSite.cs#L13); [RepairCultivationSite.cs:19](../Assets/Scripts/Gameplay/World/RepairCultivationSite.cs#L19); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105).

### roasted hearthbulb — `RoastedHearthbulb`

**Where it comes from:** Cook Hearthbulb stack beside an eligible campfire/hearth/stove via Cookable → CookingService; output count equals input count.

**What it actually does:** Eat one carried unit: restores 2d4 HP, capped at maximum. Prepared meal: +20 ColdResistance for 100 owner turns, replacing previous prepared meal.

**Limits and gaps:** HP healing, not hunger/nutrition. Can be spent at full HP. No general autonomous NPC eating AI found.

**Base affordances:** Base Commerce value 7; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:42565](../Assets/Resources/Content/Blueprints/Objects.json#L42565); [FoodPart.cs:39](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L39); [FoodPart.cs:67](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L67); [FoodPart.cs:89](../Assets/Scripts/Gameplay/Items/FoodPart.cs#L89); [PreparedMealEffect.cs:1](../Assets/Scripts/Gameplay/Effects/Concrete/PreparedMealEffect.cs#L1); [Objects.json:42483](../Assets/Resources/Content/Blueprints/Objects.json#L42483); [CookablePart.cs:15](../Assets/Scripts/Gameplay/Items/CookablePart.cs#L15); [CookingService.cs:27](../Assets/Scripts/Gameplay/Items/CookingService.cs#L27).

### wrapped field meal — `FieldMeal`

**Where it comes from:** Commission repaired ConnectedBatchPan with living worker: 2 Emberwheat + 1 ClaspbeanPulp + 2 drams; wait 120 world ticks; collect resulting physical FieldMeal from pickup container.

**What it actually does:** Eat one: heal 3d4 HP capped at max and remove one ordinary BleedingEffect.

**Limits and gaps:** No prepared-meal stat buff. Batch interruption may return remaining inputs while fee stays spent.

**Base affordances:** Base Commerce value 18; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:53619](../Assets/Resources/Content/Blueprints/Objects.json#L53619); [KitchenBatchPart.cs:108](../Assets/Scripts/Gameplay/World/KitchenBatchPart.cs#L108); [KitchenBatchPart.cs:299](../Assets/Scripts/Gameplay/World/KitchenBatchPart.cs#L299); [FieldMealPart.cs:27](../Assets/Scripts/Gameplay/Items/FieldMealPart.cs#L27); [SpreadExplorationResidents.cs:57](../Assets/Scripts/Gameplay/World/Generation/Builders/SpreadExplorationResidents.cs#L57).

## Seeds

### gladroot seed — `CandyCarrotSeed`

**Where it comes from:** Current loot/merchant tables: ProvisionerStock, FarmerStock, SeedKeeperStock.

**What it actually does:** Plant one carried seed into CandyCarrotCrop on empty plantable ground. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 20 authored units (=10 wet world ticks per unit), yielding 2 CandyCarrot.

**Limits and gaps:** Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:5766](../Assets/Resources/Content/Blueprints/Objects.json#L5766); [LootTables.json:878](../Assets/Resources/Content/Data/Loot/LootTables.json#L878); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### emberwheat seed — `EmberwheatSeed`

**Where it comes from:** Current loot/merchant tables: ProvisionerStock, FarmerStock, SeedKeeperStock.

**What it actually does:** Plant one carried seed into EmberwheatCrop on empty plantable ground. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 35 authored units (=10 wet world ticks per unit), yielding 2 Emberwheat.

**Limits and gaps:** Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 5; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:5835](../Assets/Resources/Content/Blueprints/Objects.json#L5835); [LootTables.json:883](../Assets/Resources/Content/Data/Loot/LootTables.json#L883); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### knotflax seed — `KnotflaxSeed`

**Where it comes from:** Current loot/merchant tables: MorrowfastProvisionerStock, SeedKeeperStock. Harvest ripe knotflax/hearthbulb/seamleaf at the western Morrowfast allotment (Overworld.2.6.0): two produce plus one corresponding seed; replant prepared beds.

**What it actually does:** Plant one carried seed into KnotflaxCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 16 authored units (=10 wet world ticks per unit), yielding 2 KnotflaxCord and 1 KnotflaxSeed.

**Limits and gaps:** Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 4; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:42723](../Assets/Resources/Content/Blueprints/Objects.json#L42723); [LootTables.json:87](../Assets/Resources/Content/Data/Loot/LootTables.json#L87); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72); [RepairCultivationSite.cs:13](../Assets/Scripts/Gameplay/World/RepairCultivationSite.cs#L13); [RepairCultivationSite.cs:19](../Assets/Scripts/Gameplay/World/RepairCultivationSite.cs#L19); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105).

### hearthbulb seed — `HearthbulbSeed`

**Where it comes from:** Current loot/merchant tables: MorrowfastProvisionerStock, SeedKeeperStock. Harvest ripe knotflax/hearthbulb/seamleaf at the western Morrowfast allotment (Overworld.2.6.0): two produce plus one corresponding seed; replant prepared beds.

**What it actually does:** Plant one carried seed into HearthbulbCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 20 authored units (=10 wet world ticks per unit), yielding 2 Hearthbulb and 1 HearthbulbSeed.

**Limits and gaps:** Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 4; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:42893](../Assets/Resources/Content/Blueprints/Objects.json#L42893); [LootTables.json:92](../Assets/Resources/Content/Data/Loot/LootTables.json#L92); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72); [RepairCultivationSite.cs:13](../Assets/Scripts/Gameplay/World/RepairCultivationSite.cs#L13); [RepairCultivationSite.cs:19](../Assets/Scripts/Gameplay/World/RepairCultivationSite.cs#L19); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105).

### seamleaf seed — `SeamleafSeed`

**Where it comes from:** Current loot/merchant tables: MorrowfastProvisionerStock, SeedKeeperStock. Harvest ripe knotflax/hearthbulb/seamleaf at the western Morrowfast allotment (Overworld.2.6.0): two produce plus one corresponding seed; replant prepared beds.

**What it actually does:** Plant one carried seed into SeamleafCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 24 authored units (=10 wet world ticks per unit), yielding 2 SeamleafSprig and 1 SeamleafSeed.

**Limits and gaps:** Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 4; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:43063](../Assets/Resources/Content/Blueprints/Objects.json#L43063); [LootTables.json:97](../Assets/Resources/Content/Data/Loot/LootTables.json#L97); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72); [RepairCultivationSite.cs:13](../Assets/Scripts/Gameplay/World/RepairCultivationSite.cs#L13); [RepairCultivationSite.cs:19](../Assets/Scripts/Gameplay/World/RepairCultivationSite.cs#L19); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105).

### claspbean seed — `ClaspbeanSeed`

**Where it comes from:** Harvest ClaspbeanCrop in current Spread biome patches: 2 ClaspbeanPulp and 1 ClaspbeanSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into ClaspbeanCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 20 authored units (=10 wet world ticks per unit), yielding 2 ClaspbeanPulp and 1 ClaspbeanSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:43626](../Assets/Resources/Content/Blueprints/Objects.json#L43626); [BiomeCrops.json:4](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L4); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### pitchpod seed — `PitchpodSeed`

**Where it comes from:** Harvest PitchpodCrop in current Spread biome patches: 2 PitchpodResin and 1 PitchpodSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into PitchpodCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 24 authored units (=10 wet world ticks per unit), yielding 2 PitchpodResin and 1 PitchpodSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:43904](../Assets/Resources/Content/Blueprints/Objects.json#L43904); [BiomeCrops.json:19](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L19); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### drawgourd seed — `DrawgourdSeed`

**Where it comes from:** Harvest DrawgourdCrop in current Spread biome patches: 1 DrawgourdShell and 1 DrawgourdSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into DrawgourdCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 28 authored units (=10 wet world ticks per unit), yielding 1 DrawgourdShell and 1 DrawgourdSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:44173](../Assets/Resources/Content/Blueprints/Objects.json#L44173); [BiomeCrops.json:34](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L34); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### wickrush seed — `WickrushSeed`

**Where it comes from:** Harvest WickrushCrop in current Spread biome patches: 1 WickrushCandle and 1 WickrushSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into WickrushCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 22 authored units (=10 wet world ticks per unit), yielding 1 WickrushCandle and 1 WickrushSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:44442](../Assets/Resources/Content/Blueprints/Objects.json#L44442); [BiomeCrops.json:49](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L49); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### marlroot seed — `MarlrootSeed`

**Where it comes from:** Harvest MarlrootCrop in current Spread biome patches: 2 MarlrootClod and 1 MarlrootSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into MarlrootCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 26 authored units (=10 wet world ticks per unit), yielding 2 MarlrootClod and 1 MarlrootSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:44786](../Assets/Resources/Content/Blueprints/Objects.json#L44786); [BiomeCrops.json:64](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L64); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### sumpsieve seed — `SumpsieveSeed`

**Where it comes from:** Harvest SumpsieveCrop in current Sodden biome patches: 2 SumpsievePad and 1 SumpsieveSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into SumpsieveCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 24 authored units (=10 wet world ticks per unit), yielding 2 SumpsievePad and 1 SumpsieveSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:45059](../Assets/Resources/Content/Blueprints/Objects.json#L45059); [BiomeCrops.json:79](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L79); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### drowsebell seed — `DrowsebellSeed`

**Where it comes from:** Harvest DrowsebellCrop in current Sodden biome patches: 1 DrowsebellBladder and 1 DrowsebellSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into DrowsebellCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 30 authored units (=10 wet world ticks per unit), yielding 1 DrowsebellBladder and 1 DrowsebellSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:45337](../Assets/Resources/Content/Blueprints/Objects.json#L45337); [BiomeCrops.json:94](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L94); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### chillcress seed — `ChillcressSeed`

**Where it comes from:** Harvest ChillcressCrop in current Sodden biome patches: 2 ChillcressTip and 1 ChillcressSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into ChillcressCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 22 authored units (=10 wet world ticks per unit), yielding 2 ChillcressTip and 1 ChillcressSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:45610](../Assets/Resources/Content/Blueprints/Objects.json#L45610); [BiomeCrops.json:109](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L109); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### slipsedge seed — `SlipsedgeSeed`

**Where it comes from:** Harvest SlipsedgeCrop in current Sodden biome patches: 2 SlipsedgeGel and 1 SlipsedgeSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into SlipsedgeCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 24 authored units (=10 wet world ticks per unit), yielding 2 SlipsedgeGel and 1 SlipsedgeSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:45879](../Assets/Resources/Content/Blueprints/Objects.json#L45879); [BiomeCrops.json:124](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L124); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### peatlantern seed — `PeatlanternSeed`

**Where it comes from:** Harvest PeatlanternCrop in current Sodden biome patches: 1 PeatlanternCup and 1 PeatlanternSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into PeatlanternCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 32 authored units (=10 wet world ticks per unit), yielding 1 PeatlanternCup and 1 PeatlanternSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:46148](../Assets/Resources/Content/Blueprints/Objects.json#L46148); [BiomeCrops.json:139](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L139); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### sunbladder seed — `SunbladderSeed`

**Where it comes from:** Harvest SunbladderCrop in current Beating biome patches: 1 SunbladderShell and 1 SunbladderSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into SunbladderCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 32 authored units (=10 wet world ticks per unit), yielding 1 SunbladderShell and 1 SunbladderSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:46441](../Assets/Resources/Content/Blueprints/Objects.json#L46441); [BiomeCrops.json:154](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L154); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### shalebean seed — `ShalebeanSeed`

**Where it comes from:** Harvest ShalebeanCrop in current Beating biome patches: 1 ShalebeanWax and 1 ShalebeanSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into ShalebeanCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 30 authored units (=10 wet world ticks per unit), yielding 1 ShalebeanWax and 1 ShalebeanSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:46710](../Assets/Resources/Content/Blueprints/Objects.json#L46710); [BiomeCrops.json:169](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L169); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### shadefan seed — `ShadefanSeed`

**Where it comes from:** Harvest ShadefanCrop in current Beating biome patches: 1 ShadefanHood and 1 ShadefanSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into ShadefanCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 28 authored units (=10 wet world ticks per unit), yielding 1 ShadefanHood and 1 ShadefanSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:46996](../Assets/Resources/Content/Blueprints/Objects.json#L46996); [BiomeCrops.json:184](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L184); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### cinderpea seed — `CinderpeaSeed`

**Where it comes from:** Harvest CinderpeaCrop in current Beating biome patches: 1 CinderpeaOil and 1 CinderpeaSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into CinderpeaCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 32 authored units (=10 wet world ticks per unit), yielding 1 CinderpeaOil and 1 CinderpeaSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:47289](../Assets/Resources/Content/Blueprints/Objects.json#L47289); [BiomeCrops.json:199](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L199); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### spurgrass seed — `SpurgrassSeed`

**Where it comes from:** Harvest SpurgrassCrop in current Beating biome patches: 2 SpurgrassSpine and 1 SpurgrassSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into SpurgrassCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 26 authored units (=10 wet world ticks per unit), yielding 2 SpurgrassSpine and 1 SpurgrassSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:47575](../Assets/Resources/Content/Blueprints/Objects.json#L47575); [BiomeCrops.json:214](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L214); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### choirwick seed — `ChoirwickSeed`

**Where it comes from:** Harvest ChoirwickCrop in current Grovelands biome patches: 1 ChoirwickBranch and 1 ChoirwickSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into ChoirwickCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 30 authored units (=10 wet world ticks per unit), yielding 1 ChoirwickBranch and 1 ChoirwickSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:47852](../Assets/Resources/Content/Blueprints/Objects.json#L47852); [BiomeCrops.json:229](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L229); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### knitmoss seed — `KnitmossSeed`

**Where it comes from:** Harvest KnitmossCrop in current Grovelands biome patches: 2 KnitmossPad and 1 KnitmossSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into KnitmossCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 24 authored units (=10 wet world ticks per unit), yielding 2 KnitmossPad and 1 KnitmossSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:48145](../Assets/Resources/Content/Blueprints/Objects.json#L48145); [BiomeCrops.json:244](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L244); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### sourmantle seed — `SourmantleSeed`

**Where it comes from:** Harvest SourmantleCrop in current Grovelands biome patches: 2 SourmantleFold and 1 SourmantleSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into SourmantleCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 28 authored units (=10 wet world ticks per unit), yielding 2 SourmantleFold and 1 SourmantleSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:48418](../Assets/Resources/Content/Blueprints/Objects.json#L48418); [BiomeCrops.json:259](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L259); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### murmurpod seed — `MurmurpodSeed`

**Where it comes from:** Harvest MurmurpodCrop in current Grovelands biome patches: 1 MurmurpodBladder and 1 MurmurpodSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into MurmurpodCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 34 authored units (=10 wet world ticks per unit), yielding 1 MurmurpodBladder and 1 MurmurpodSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:48687](../Assets/Resources/Content/Blueprints/Objects.json#L48687); [BiomeCrops.json:274](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L274); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### sealbark seed — `SealbarkSeed`

**Where it comes from:** Harvest SealbarkCrop in current Grovelands biome patches: 2 SealbarkSlat and 1 SealbarkSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into SealbarkCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 34 authored units (=10 wet world ticks per unit), yielding 2 SealbarkSlat and 1 SealbarkSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:48960](../Assets/Resources/Content/Blueprints/Objects.json#L48960); [BiomeCrops.json:289](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L289); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### absentmint seed — `AbsentmintSeed`

**Where it comes from:** Harvest AbsentmintCrop in current Overwrit biome patches: 2 AbsentmintLeaf and 1 AbsentmintSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into AbsentmintCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 30 authored units (=10 wet world ticks per unit), yielding 2 AbsentmintLeaf and 1 AbsentmintSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:49233](../Assets/Resources/Content/Blueprints/Objects.json#L49233); [BiomeCrops.json:304](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L304); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### margincress seed — `MargincressSeed`

**Where it comes from:** Harvest MargincressCrop in current Overwrit biome patches: 2 MargincressRibbon and 1 MargincressSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into MargincressCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 26 authored units (=10 wet world ticks per unit), yielding 2 MargincressRibbon and 1 MargincressSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:49511](../Assets/Resources/Content/Blueprints/Objects.json#L49511); [BiomeCrops.json:319](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L319); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### binderroot seed — `BinderrootSeed`

**Where it comes from:** Harvest BinderrootCrop in current Overwrit biome patches: 1 BinderrootKnot and 1 BinderrootSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into BinderrootCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 34 authored units (=10 wet world ticks per unit), yielding 1 BinderrootKnot and 1 BinderrootSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:49789](../Assets/Resources/Content/Blueprints/Objects.json#L49789); [BiomeCrops.json:334](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L334); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### greybladder seed — `GreybladderSeed`

**Where it comes from:** Harvest GreybladderCrop in current Overwrit biome patches: 1 GreybladderCup and 1 GreybladderSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into GreybladderCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 34 authored units (=10 wet world ticks per unit), yielding 1 GreybladderCup and 1 GreybladderSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:50058](../Assets/Resources/Content/Blueprints/Objects.json#L50058); [BiomeCrops.json:349](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L349); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### hollowchime seed — `HollowchimeSeed`

**Where it comes from:** Harvest HollowchimeCrop in current Overwrit biome patches: 1 HollowchimePod and 1 HollowchimeSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into HollowchimeCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 38 authored units (=10 wet world ticks per unit), yielding 1 HollowchimePod and 1 HollowchimeSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:50331](../Assets/Resources/Content/Blueprints/Objects.json#L50331); [BiomeCrops.json:364](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L364); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### raingourd seed — `RaingourdSeed`

**Where it comes from:** Harvest RaingourdCrop in current Stump biome patches: 1 RaingourdCup and 1 RaingourdSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into RaingourdCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 30 authored units (=10 wet world ticks per unit), yielding 1 RaingourdCup and 1 RaingourdSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:50604](../Assets/Resources/Content/Blueprints/Objects.json#L50604); [BiomeCrops.json:379](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L379); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### prismreed seed — `PrismreedSeed`

**Where it comes from:** Harvest PrismreedCrop in current Stump biome patches: 2 PrismreedPith and 1 PrismreedSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into PrismreedCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 34 authored units (=10 wet world ticks per unit), yielding 2 PrismreedPith and 1 PrismreedSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:50873](../Assets/Resources/Content/Blueprints/Objects.json#L50873); [BiomeCrops.json:394](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L394); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### scarlet sundew seed — `ScarletSundewSeed`

**Where it comes from:** Harvest ScarletSundewCrop in current Stump biome patches: 1 ScarletSundewDew and 1 ScarletSundewSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into ScarletSundewCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 32 authored units (=10 wet world ticks per unit), yielding 1 ScarletSundewDew and 1 ScarletSundewSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:51142](../Assets/Resources/Content/Blueprints/Objects.json#L51142); [BiomeCrops.json:409](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L409); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### cloudwick seed — `CloudwickSeed`

**Where it comes from:** Harvest CloudwickCrop in current Stump biome patches: 1 CloudwickTuft and 1 CloudwickSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into CloudwickCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 34 authored units (=10 wet world ticks per unit), yielding 1 CloudwickTuft and 1 CloudwickSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:51428](../Assets/Resources/Content/Blueprints/Objects.json#L51428); [BiomeCrops.json:424](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L424); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### gripfrond seed — `GripfrondSeed`

**Where it comes from:** Harvest GripfrondCrop in current Stump biome patches: 1 GripfrondWrap and 1 GripfrondSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into GripfrondCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 36 authored units (=10 wet world ticks per unit), yielding 1 GripfrondWrap and 1 GripfrondSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:51721](../Assets/Resources/Content/Blueprints/Objects.json#L51721); [BiomeCrops.json:439](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L439); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### lampvein seed — `LampveinSeed`

**Where it comes from:** Harvest LampveinCrop in current Cave biome patches: 1 LampveinFan and 1 LampveinSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into LampveinCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 32 authored units (=10 wet world ticks per unit), yielding 1 LampveinFan and 1 LampveinSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:52014](../Assets/Resources/Content/Blueprints/Objects.json#L52014); [BiomeCrops.json:454](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L454); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### knucklecap seed — `KnucklecapSeed`

**Where it comes from:** Harvest KnucklecapCrop in current Cave biome patches: 1 KnucklecapWax and 1 KnucklecapSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into KnucklecapCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 38 authored units (=10 wet world ticks per unit), yielding 1 KnucklecapWax and 1 KnucklecapSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:52307](../Assets/Resources/Content/Blueprints/Objects.json#L52307); [BiomeCrops.json:469](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L469); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### sootroot seed — `SootrootSeed`

**Where it comes from:** Harvest SootrootCrop in current Cave biome patches: 2 SootrootPulp and 1 SootrootSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into SootrootCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 28 authored units (=10 wet world ticks per unit), yielding 2 SootrootPulp and 1 SootrootSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:52593](../Assets/Resources/Content/Blueprints/Objects.json#L52593); [BiomeCrops.json:484](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L484); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### veilpuff seed — `VeilpuffSeed`

**Where it comes from:** Harvest VeilpuffCrop in current Cave biome patches: 1 VeilpuffBladder and 1 VeilpuffSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into VeilpuffCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 40 authored units (=10 wet world ticks per unit), yielding 1 VeilpuffBladder and 1 VeilpuffSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:52871](../Assets/Resources/Content/Blueprints/Objects.json#L52871); [BiomeCrops.json:499](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L499); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

### brinebutton seed — `BrinebuttonSeed`

**Where it comes from:** Harvest BrinebuttonCrop in current Cave biome patches: 1 BrinebuttonPaste and 1 BrinebuttonSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Plant one carried seed into BrinebuttonCrop on empty plantable ground with a prepared/cultivated soil marker. Inventory offers plant here and neighboring prepared beds. Water is required for growth. Grows for two stages of 40 authored units (=10 wet world ticks per unit), yielding 1 BrinebuttonPaste and 1 BrinebuttonSeed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Growth reconciles elapsed saved world time on return; dry time gives no growth. No autonomous NPC planting/harvesting inferred from this item API.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:53144](../Assets/Resources/Content/Blueprints/Objects.json#L53144); [BiomeCrops.json:514](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L514); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [SeedPart.cs:49](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L49); [SeedPart.cs:131](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L131); [SeedPart.cs:201](../Assets/Scripts/Gameplay/Farming/SeedPart.cs#L201); [CropTime.cs:72](../Assets/Scripts/Gameplay/Farming/CropTime.cs#L72).

## Other botanical outputs, materials and fuels

### silver sand — `SilverSand`

**Where it comes from:** Current village Merchant guaranteed repair stock; fouled-well merchant stock; regional Wellmeet filter consignment/reward paths.

**What it actually does:** With WellMaintenanceManual carried, consumes one to make a damaged settlement well StableRepair. Two provenance-bound regional cargo units can complete Wellmeet’s recovery request for 12 drams + FireClay.

**Limits and gaps:** No direct inventory Use action; interact with the well or request recipient. Generic sand bought elsewhere is not automatically valid recovery cargo.

**Base affordances:** Base Commerce value 1; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:23875](../Assets/Resources/Content/Blueprints/Objects.json#L23875); [VillagePopulationBuilder.cs:790](../Assets/Scripts/Gameplay/World/Generation/Builders/VillagePopulationBuilder.cs#L790); [TradeStockBuilder.cs:103](../Assets/Scripts/Gameplay/World/Generation/Builders/TradeStockBuilder.cs#L103); [SettlementManager.cs:139](../Assets/Scripts/Gameplay/Settlements/SettlementManager.cs#L139); [RegionalSituations.cs:49](../Assets/Scripts/Gameplay/World/RegionalSituations.cs#L49); [RegionalRequestPart.cs:164](../Assets/Scripts/Gameplay/World/RegionalRequestPart.cs#L164).

### fire clay — `FireClay`

**Where it comes from:** Current loot/merchant tables: MorrowfastMenderStock. Merchant repair stock; harvest RepairClayBank (4); prepare MarlrootClod (1); Morrowfast supply/reward sources.

**What it actually does:** 2 clay repairs clay-lined well or connected batch pan; legacy oven repair consumes 1 clay while OvenBuildersGuide is carried. 1 clay quiets Morrowfast arch bell after cord diagnosis.

**Limits and gaps:** Repair action lives on target, not raw material. Each recipe has its own quantity and state gate.

**Base affordances:** Base Commerce value 1; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:25158](../Assets/Resources/Content/Blueprints/Objects.json#L25158); [LootTables.json:32](../Assets/Resources/Content/Data/Loot/LootTables.json#L32); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [Objects.json:43410](../Assets/Resources/Content/Blueprints/Objects.json#L43410); [RepairCultivationSite.cs:17](../Assets/Scripts/Gameplay/World/RepairCultivationSite.cs#L17); [BotanicalProcessingPart.cs:80](../Assets/Scripts/Gameplay/Farming/BotanicalProcessingPart.cs#L80); [Tier1Repairs.json:14](../Assets/Resources/Content/Data/Repairs/Tier1Repairs.json#L14); [RepairablePart.cs:1](../Assets/Scripts/Gameplay/Repairs/RepairablePart.cs#L1); [MorrowfastQuests.cs:125](../Assets/Scripts/Gameplay/World/MorrowfastQuests.cs#L125); [SettlementManager.cs:189](../Assets/Scripts/Gameplay/Settlements/SettlementManager.cs#L189).

### ward oil — `WardOil`

**Where it comes from:** Current village Merchant guaranteed repair stock; regional Sumphold recovery consignment has two physical units.

**What it actually does:** With LanternOilRecipe carried, consumes one to make settlement watch lantern StableRepair. Two provenance-bound cargo units complete Sumphold’s request for 12 drams + SilverSand.

**Limits and gaps:** Not a pourable oil vessel, not LampOil refuel; use is at settlement/request context.

**Base affordances:** Base Commerce value 5; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:26863](../Assets/Resources/Content/Blueprints/Objects.json#L26863); [VillagePopulationBuilder.cs:790](../Assets/Scripts/Gameplay/World/Generation/Builders/VillagePopulationBuilder.cs#L790); [SettlementManager.cs:249](../Assets/Scripts/Gameplay/Settlements/SettlementManager.cs#L249); [RegionalSituations.cs:49](../Assets/Scripts/Gameplay/World/RegionalSituations.cs#L49); [RegionalRequestPart.cs:164](../Assets/Scripts/Gameplay/World/RegionalRequestPart.cs#L164).

### pale-salt — `PaleSalt`

**Where it comes from:** Current loot/merchant tables: DeepSupplyT2, ArcanistStock, DeathConstructT2, UrnT1, OreCacheT1. Harvest PaleSaltVein: 1–2 actual mineral items per finite vein.

**What it actually does:** Inventory Infuse action consumes one near forge, uses one modification slot on compatible carried unstacked equipment, and adds +4 bonus damage against Undead-tagged targets on valid weapon hits. Same mineral also supports the authored tinkering infusion recipe. Give one to SaltMaster for +5 TentRight reputation through mineral trade.

**Limits and gaps:** No duplicate same enhancement; compatibility/slot budget enforced. Requires item carried, not equipped for direct preparation.

**Base affordances:** Base Commerce value 12; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:28130](../Assets/Resources/Content/Blueprints/Objects.json#L28130); [LootTables.json:580](../Assets/Resources/Content/Data/Loot/LootTables.json#L580); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [Objects.json:27992](../Assets/Resources/Content/Blueprints/Objects.json#L27992); [HarvestablePart.cs:66](../Assets/Scripts/Gameplay/Items/HarvestablePart.cs#L66); [MineralPreparationPart.cs:13](../Assets/Scripts/Gameplay/Preparation/MineralPreparationPart.cs#L13); [MineralPreparationPart.cs:27](../Assets/Scripts/Gameplay/Preparation/MineralPreparationPart.cs#L27); [MineralInfusionTinkerModification.cs:166](../Assets/Scripts/Gameplay/Tinkering/Mods/MineralInfusionTinkerModification.cs#L166); [EnhancementTagBonusBase.cs:54](../Assets/Scripts/Gameplay/Items/EnhancementTagBonusBase.cs#L54); [Objects.json:33000](../Assets/Resources/Content/Blueprints/Objects.json#L33000); [WantsMineralPart.cs:137](../Assets/Scripts/Gameplay/AI/WantsMineralPart.cs#L137).

### choir-iron ore — `ChoirIron`

**Where it comes from:** Current loot/merchant tables: ZigguratVaultT2, ArcanistStock, DeathConstructT3, OreCacheT2, OreCacheT3, ChoirStock. Harvest ChoirIronVein: 1–2 actual mineral items per finite vein.

**What it actually does:** Inventory Infuse action consumes one near forge, uses one modification slot on compatible carried unstacked equipment, and adds +6 bonus damage against Fungal-tagged targets on valid weapon hits. Same mineral also supports the authored tinkering infusion recipe. Deliver one for current Morrowfast/Cinderhold supply requests: 8 drams + FireClay.

**Limits and gaps:** No duplicate same enhancement; compatibility/slot budget enforced. Requires item carried, not equipped for direct preparation.

**Base affordances:** Base Commerce value 18; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:28194](../Assets/Resources/Content/Blueprints/Objects.json#L28194); [LootTables.json:404](../Assets/Resources/Content/Data/Loot/LootTables.json#L404); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [Objects.json:28061](../Assets/Resources/Content/Blueprints/Objects.json#L28061); [HarvestablePart.cs:66](../Assets/Scripts/Gameplay/Items/HarvestablePart.cs#L66); [MineralPreparationPart.cs:13](../Assets/Scripts/Gameplay/Preparation/MineralPreparationPart.cs#L13); [MineralPreparationPart.cs:27](../Assets/Scripts/Gameplay/Preparation/MineralPreparationPart.cs#L27); [MineralInfusionTinkerModification.cs:176](../Assets/Scripts/Gameplay/Tinkering/Mods/MineralInfusionTinkerModification.cs#L176); [EnhancementTagBonusBase.cs:54](../Assets/Scripts/Gameplay/Items/EnhancementTagBonusBase.cs#L54); [RegionalSituations.cs:43](../Assets/Scripts/Gameplay/World/RegionalSituations.cs#L43); [RegionalRequestPart.cs:164](../Assets/Scripts/Gameplay/World/RegionalRequestPart.cs#L164).

### glow-quartz — `GlowQuartz`

**Where it comes from:** Current loot/merchant tables: ArcanistStock, DeathConstructT2, DeathConstructT3, OreCacheT1, OreCacheT2, OreCacheT3. Harvest GlowQuartzVein: 1–2 actual mineral items per finite vein.

**What it actually does:** Inventory Infuse action consumes one near forge, uses one modification slot on compatible carried unstacked equipment, and adds +2 equipped-item light radius. Same mineral also supports the authored tinkering infusion recipe.

**Limits and gaps:** No duplicate same enhancement; compatibility/slot budget enforced. Requires item carried, not equipped for direct preparation.

**Base affordances:** Base Commerce value 15; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:28710](../Assets/Resources/Content/Blueprints/Objects.json#L28710); [LootTables.json:812](../Assets/Resources/Content/Data/Loot/LootTables.json#L812); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [Objects.json:27923](../Assets/Resources/Content/Blueprints/Objects.json#L27923); [HarvestablePart.cs:66](../Assets/Scripts/Gameplay/Items/HarvestablePart.cs#L66); [MineralPreparationPart.cs:13](../Assets/Scripts/Gameplay/Preparation/MineralPreparationPart.cs#L13); [MineralPreparationPart.cs:27](../Assets/Scripts/Gameplay/Preparation/MineralPreparationPart.cs#L27); [MineralInfusionTinkerModification.cs:186](../Assets/Scripts/Gameplay/Tinkering/Mods/MineralInfusionTinkerModification.cs#L186); [EnhancementGlowQuartz.cs:84](../Assets/Scripts/Gameplay/Items/EnhancementGlowQuartz.cs#L84).

### frog oil — `FrogOil`

**Where it comes from:** Harvest Reedfrog or MawToad corpse: Reedfrog yields 1–2 at 80%; MawToad 2–4 at 90%, after 100% corpse-drop configuration. Both creatures have current Sodden population.

**What it actually does:** Generic trade/sale value only (Commerce.Value 8). No cooking, brewing, torch refuel, liquid pour or medicinal effect attached.

**Limits and gaps:** Name and liquid-like presentation are not oil mechanics. It is a separate plain trade item, not LampOil or liquid oil.

**Base affordances:** Base Commerce value 8; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:33618](../Assets/Resources/Content/Blueprints/Objects.json#L33618); [Objects.json:33377](../Assets/Resources/Content/Blueprints/Objects.json#L33377); [Objects.json:33483](../Assets/Resources/Content/Blueprints/Objects.json#L33483); [PopulationTable.cs:568](../Assets/Scripts/Data/Tables/PopulationTable.cs#L568); [PopulationTable.cs:590](../Assets/Scripts/Data/Tables/PopulationTable.cs#L590); [CorpsePart.cs:127](../Assets/Scripts/Gameplay/Entities/CorpsePart.cs#L127); [HarvestablePart.cs:66](../Assets/Scripts/Gameplay/Items/HarvestablePart.cs#L66); [TradeSystem.cs:364](../Assets/Scripts/Gameplay/Economy/TradeSystem.cs#L364).

### tepuibone — `Tepuibone`

**Where it comes from:** Current loot/merchant tables: MorrowfastMenderStock. Harvest TepuiboneVein: 1–2 actual mineral items per finite vein.

**What it actually does:** Prepare into TepuiboneHeadComponent (1); tender one at Morrowfast FoundingPlaque for +50 CatacombFolk trust; one of three stones consumed at Root sealing ending.

**Limits and gaps:** Not one of the three direct MineralPreparation infusion inputs.

**Base affordances:** Base Commerce value 30; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:35584](../Assets/Resources/Content/Blueprints/Objects.json#L35584); [LootTables.json:27](../Assets/Resources/Content/Data/Loot/LootTables.json#L27); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [Objects.json:35562](../Assets/Resources/Content/Blueprints/Objects.json#L35562); [HarvestablePart.cs:66](../Assets/Scripts/Gameplay/Items/HarvestablePart.cs#L66); [PreparationRecipePart.cs:40](../Assets/Scripts/Gameplay/Preparation/PreparationRecipePart.cs#L40); [FoundingTrustService.cs:25](../Assets/Scripts/Gameplay/Settlements/FoundingTrustService.cs#L25); [EndingRoutes.cs:26](../Assets/Scripts/Gameplay/World/EndingRoutes.cs#L26); [EndingRoutes.cs:69](../Assets/Scripts/Gameplay/World/EndingRoutes.cs#L69).

### salvaged timber — `SalvagedTimber`

**Where it comes from:** Current loot/merchant tables: MorrowfastMenderStock. Harvest RepairTimberPile; dismantle GleanersTimberPallet; Prepare SealbarkSlat. Current mender trade stock.

**What it actually does:** Repair wood gates/wicket/dressing bench (2), damaged wood gear (1); Shape haft into FieldHaftComponent (1); permanently jam supported Spike/Fire/Bear/PressurePlate trap (1); feed finite cooking coals +10 fuel capped (1).

**Limits and gaps:** A specific native target must opt into the repair/jam/fuel action; not arbitrary construction.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:42319](../Assets/Resources/Content/Blueprints/Objects.json#L42319); [LootTables.json:37](../Assets/Resources/Content/Data/Loot/LootTables.json#L37); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [Objects.json:43482](../Assets/Resources/Content/Blueprints/Objects.json#L43482); [Objects.json:53995](../Assets/Resources/Content/Blueprints/Objects.json#L53995); [RepairCultivationSite.cs:18](../Assets/Scripts/Gameplay/World/RepairCultivationSite.cs#L18); [PreparationRecipePart.cs:38](../Assets/Scripts/Gameplay/Preparation/PreparationRecipePart.cs#L38); [Tier1Repairs.json:4](../Assets/Resources/Content/Data/Repairs/Tier1Repairs.json#L4); [TrapJammingPart.cs:34](../Assets/Scripts/Gameplay/Entities/TrapJammingPart.cs#L34); [CampfireWorkActions.cs:70](../Assets/Scripts/Gameplay/Settlements/CampfireWorkActions.cs#L70).

### knotflax cord — `KnotflaxCord`

**Where it comes from:** Current loot/merchant tables: MorrowfastMenderStock. Harvest ripe knotflax/hearthbulb/seamleaf at the western Morrowfast allotment (Overworld.2.6.0): two produce plus one corresponding seed; replant prepared beds.

**What it actually does:** Repair hoist well line (1); braid PlainCordBindingComponent (1); prepare KnotflaxBandage (1); make SoddenFieldDressing with pad and fee; rig authored rope shortcut using 2 cord after both endpoint zones visited.

**Limits and gaps:** The shortcut is a paired authored route, not arbitrary rope placement.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:42401](../Assets/Resources/Content/Blueprints/Objects.json#L42401); [LootTables.json:42](../Assets/Resources/Content/Data/Loot/LootTables.json#L42); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [RepairCultivationSite.cs:13](../Assets/Scripts/Gameplay/World/RepairCultivationSite.cs#L13); [RepairCultivationSite.cs:19](../Assets/Scripts/Gameplay/World/RepairCultivationSite.cs#L19); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [Tier1Repairs.json:34](../Assets/Resources/Content/Data/Repairs/Tier1Repairs.json#L34); [PreparationRecipePart.cs:39](../Assets/Scripts/Gameplay/Preparation/PreparationRecipePart.cs#L39); [SoddenPreparationPart.cs:97](../Assets/Scripts/Gameplay/World/SoddenPreparationPart.cs#L97); [SecondExplorationRoutes.cs:28](../Assets/Scripts/Gameplay/Exploration/SecondExplorationRoutes.cs#L28).

### wickrush candle — `WickrushCandle`

**Where it comes from:** Harvest WickrushCrop in current Spread biome patches: 1 WickrushCandle and 1 WickrushSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Light radius 3, intensity 0.45, color &Y; works on ground or equipped in hand, not stowed. Light near an ignition source; extinguish to save fuel; can refuel with LampOil.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Consumes finite fuel, is disabled by sufficient wetness/freezing; starts unlit.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:44612](../Assets/Resources/Content/Blueprints/Objects.json#L44612); [BiomeCrops.json:49](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L49); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [LightMap.cs:117](../Assets/Scripts/Gameplay/World/LightMap.cs#L117); [LightMap.cs:147](../Assets/Scripts/Gameplay/World/LightMap.cs#L147); [TorchLightPart.cs:17](../Assets/Scripts/Gameplay/Items/TorchLightPart.cs#L17); [TorchLightPart.cs:44](../Assets/Scripts/Gameplay/Items/TorchLightPart.cs#L44); [TorchRefuelPart.cs:35](../Assets/Scripts/Gameplay/Preparation/TorchRefuelPart.cs#L35).

### marlroot clod — `MarlrootClod`

**Where it comes from:** Harvest MarlrootCrop in current Spread biome patches: 2 MarlrootClod and 1 MarlrootSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Inventory Prepare consumes one unit and makes 1 FireClay in the pack.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. No station needed; requires output carrying capacity.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:44956](../Assets/Resources/Content/Blueprints/Objects.json#L44956); [BiomeCrops.json:64](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L64); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [BotanicalProcessingPart.cs:21](../Assets/Scripts/Gameplay/Farming/BotanicalProcessingPart.cs#L21); [BotanicalProcessingPart.cs:80](../Assets/Scripts/Gameplay/Farming/BotanicalProcessingPart.cs#L80).

### drowsebell bladder — `DrowsebellBladder`

**Where it comes from:** Harvest DrowsebellCrop in current Sodden biome patches: 1 DrowsebellBladder and 1 DrowsebellSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Throw to consume it and emit a 3x3 gas cloud: {'GasId': 'sleep-vapor', 'Density': '20', 'Level': '1'}. Sleep/confusion/stun affect eligible breathing targets; cryo deals cold damage and freezes without respiratory gating.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Gas disperses and affects allies as well as foes; no throw accuracy roll for the grenade detonation. No NPC grenade-use AI established for these plant products.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:45507](../Assets/Resources/Content/Blueprints/Objects.json#L45507); [BiomeCrops.json:94](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L94); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [GasGrenadePart.cs:52](../Assets/Scripts/Gameplay/Materials/GasGrenadePart.cs#L52); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176); [GasSleepPart.cs:22](../Assets/Scripts/Gameplay/Materials/GasSleepPart.cs#L22).

### peatlantern cup — `PeatlanternCup`

**Where it comes from:** Harvest PeatlanternCrop in current Sodden biome patches: 1 PeatlanternCup and 1 PeatlanternSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Light radius 3, intensity 0.45, color &Y; works on ground or equipped in hand, not stowed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:46318](../Assets/Resources/Content/Blueprints/Objects.json#L46318); [BiomeCrops.json:139](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L139); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [LightMap.cs:117](../Assets/Scripts/Gameplay/World/LightMap.cs#L117); [LightMap.cs:147](../Assets/Scripts/Gameplay/World/LightMap.cs#L147).

### shadefan hood — `ShadefanHood`

**Where it comes from:** Harvest ShadefanCrop in current Beating biome patches: 1 ShadefanHood and 1 ShadefanSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Equip on Head; zero AV/DV, but counts as head cover and prevents Beating glare buildup.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Uses the head slot; any qualifying head cover supplies this glare benefit.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:47166](../Assets/Resources/Content/Blueprints/Objects.json#L47166); [BiomeCrops.json:184](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L184); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [BeatingGlareSystem.cs:55](../Assets/Scripts/Gameplay/World/BeatingGlareSystem.cs#L55).

### spurgrass spine — `SpurgrassSpine`

**Where it comes from:** Harvest SpurgrassCrop in current Beating biome patches: 2 SpurgrassSpine and 1 SpurgrassSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Throw as a recoverable physical weapon: 1d3 base damage, +1 penetration, strength bonus cap 2, Piercing. A positive piercing hit that leaves its target alive can also trigger the shared 10% chance of 2-turn confusion.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. No Equippable part: cannot be wielded as a melee weapon despite MeleeWeapon payload; current intended use is throwing.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:47745](../Assets/Resources/Content/Blueprints/Objects.json#L47745); [BiomeCrops.json:214](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L214); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533); [EquipCommand.cs:52](../Assets/Scripts/Gameplay/Inventory/Commands/Equipment/EquipCommand.cs#L52); [OnHitClassEffects.cs:49](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L49).

### choirwick branch — `ChoirwickBranch`

**Where it comes from:** Harvest ChoirwickCrop in current Grovelands biome patches: 1 ChoirwickBranch and 1 ChoirwickSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Light radius 3, intensity 0.45, color &M; works on ground or equipped in hand, not stowed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:48022](../Assets/Resources/Content/Blueprints/Objects.json#L48022); [BiomeCrops.json:229](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L229); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [LightMap.cs:117](../Assets/Scripts/Gameplay/World/LightMap.cs#L117); [LightMap.cs:147](../Assets/Scripts/Gameplay/World/LightMap.cs#L147).

### murmurpod bladder — `MurmurpodBladder`

**Where it comes from:** Harvest MurmurpodCrop in current Grovelands biome patches: 1 MurmurpodBladder and 1 MurmurpodSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Throw to consume it and emit a 3x3 gas cloud: {'GasId': 'confusion-vapor', 'Density': '20', 'Level': '1'}. Sleep/confusion/stun affect eligible breathing targets; cryo deals cold damage and freezes without respiratory gating.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Gas disperses and affects allies as well as foes; no throw accuracy roll for the grenade detonation. No NPC grenade-use AI established for these plant products.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:48857](../Assets/Resources/Content/Blueprints/Objects.json#L48857); [BiomeCrops.json:274](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L274); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [GasGrenadePart.cs:52](../Assets/Scripts/Gameplay/Materials/GasGrenadePart.cs#L52); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176); [GasConfusionPart.cs:22](../Assets/Scripts/Gameplay/Materials/GasConfusionPart.cs#L22).

### sealbark slat — `SealbarkSlat`

**Where it comes from:** Harvest SealbarkCrop in current Grovelands biome patches: 2 SealbarkSlat and 1 SealbarkSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Inventory Prepare consumes one unit and makes 1 SalvagedTimber in the pack.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. No station needed; requires output carrying capacity.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:49130](../Assets/Resources/Content/Blueprints/Objects.json#L49130); [BiomeCrops.json:289](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L289); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [BotanicalProcessingPart.cs:21](../Assets/Scripts/Gameplay/Farming/BotanicalProcessingPart.cs#L21); [BotanicalProcessingPart.cs:80](../Assets/Scripts/Gameplay/Farming/BotanicalProcessingPart.cs#L80).

### hollowchime pod — `HollowchimePod`

**Where it comes from:** Harvest HollowchimeCrop in current Overwrit biome patches: 1 HollowchimePod and 1 HollowchimeSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Throw to consume it and emit a 3x3 gas cloud: {'GasId': 'stun-vapor', 'Density': '15', 'Level': '1'}. Sleep/confusion/stun affect eligible breathing targets; cryo deals cold damage and freezes without respiratory gating.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Gas disperses and affects allies as well as foes; no throw accuracy roll for the grenade detonation. No NPC grenade-use AI established for these plant products.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:50501](../Assets/Resources/Content/Blueprints/Objects.json#L50501); [BiomeCrops.json:364](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L364); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [GasGrenadePart.cs:52](../Assets/Scripts/Gameplay/Materials/GasGrenadePart.cs#L52); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176); [GasStunPart.cs:22](../Assets/Scripts/Gameplay/Materials/GasStunPart.cs#L22).

### cloudwick tuft — `CloudwickTuft`

**Where it comes from:** Harvest CloudwickCrop in current Stump biome patches: 1 CloudwickTuft and 1 CloudwickSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Light radius 3, intensity 0.45, color &C; works on ground or equipped in hand, not stowed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:51598](../Assets/Resources/Content/Blueprints/Objects.json#L51598); [BiomeCrops.json:424](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L424); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [LightMap.cs:117](../Assets/Scripts/Gameplay/World/LightMap.cs#L117); [LightMap.cs:147](../Assets/Scripts/Gameplay/World/LightMap.cs#L147).

### gripfrond wrap — `GripfrondWrap`

**Where it comes from:** Harvest GripfrondCrop in current Stump biome patches: 1 GripfrondWrap and 1 GripfrondSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Equip as Handwear; AV 1, DV 0 for the covered body slot(s).

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Location-based armor, not a global point of damage reduction.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:51891](../Assets/Resources/Content/Blueprints/Objects.json#L51891); [BiomeCrops.json:439](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L439); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [CombatSystem.cs:1589](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L1589).

### lampvein fan — `LampveinFan`

**Where it comes from:** Harvest LampveinCrop in current Cave biome patches: 1 LampveinFan and 1 LampveinSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Light radius 4, intensity 0.45, color &B; works on ground or equipped in hand, not stowed.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:52184](../Assets/Resources/Content/Blueprints/Objects.json#L52184); [BiomeCrops.json:454](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L454); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [LightMap.cs:117](../Assets/Scripts/Gameplay/World/LightMap.cs#L117); [LightMap.cs:147](../Assets/Scripts/Gameplay/World/LightMap.cs#L147).

### veilpuff bladder — `VeilpuffBladder`

**Where it comes from:** Harvest VeilpuffCrop in current Cave biome patches: 1 VeilpuffBladder and 1 VeilpuffSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Throw to consume it and emit a 3x3 gas cloud: {'GasId': 'cryo-mist', 'Density': '10', 'Level': '1'}. Sleep/confusion/stun affect eligible breathing targets; cryo deals cold damage and freezes without respiratory gating.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Gas disperses and affects allies as well as foes; no throw accuracy roll for the grenade detonation. No NPC grenade-use AI established for these plant products.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:53041](../Assets/Resources/Content/Blueprints/Objects.json#L53041); [BiomeCrops.json:499](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L499); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [GasGrenadePart.cs:52](../Assets/Scripts/Gameplay/Materials/GasGrenadePart.cs#L52); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176); [GasCryoPart.cs:22](../Assets/Scripts/Gameplay/Materials/GasCryoPart.cs#L22).

## Vessels

### waterskin — `Waterskin`

**Where it comes from:** Current loot/merchant tables: MorrowfastProvisionerStock, ProvisionerStock, WellKeeperStock.

**What it actually does:** Reusable water-only vessel, capacity 3 drinks; starts with 0. Fill from nearby usable well/cistern or clean unfrozen water; each drink relieves one Parched stack. Water can also be transferred to another water vessel, spent watering a nearby crop, or used to douse eligible burning objects.

**Limits and gaps:** Water only; no general liquid mixing/drinking. Drawing finite pools spends volume; wells/renewing source terrain differ. Vessel survives empty.

**Base affordances:** Base Commerce value 8; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:38528](../Assets/Resources/Content/Blueprints/Objects.json#L38528); [LootTables.json:57](../Assets/Resources/Content/Data/Loot/LootTables.json#L57); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [WaterskinPart.cs:20](../Assets/Scripts/Gameplay/Items/WaterskinPart.cs#L20); [WaterVesselService.cs:29](../Assets/Scripts/Gameplay/Items/WaterVesselService.cs#L29); [WaterVesselService.cs:83](../Assets/Scripts/Gameplay/Items/WaterVesselService.cs#L83); [CropWateringService.cs:20](../Assets/Scripts/Gameplay/Farming/CropWateringService.cs#L20); [WaterTransferActions.cs:43](../Assets/Scripts/Gameplay/Items/WaterTransferActions.cs#L43); [DouseWorldActions.cs:43](../Assets/Scripts/Gameplay/Items/DouseWorldActions.cs#L43).

### liquid flask — `LiquidFlask`

**Where it comes from:** Current loot/merchant tables: MorrowfastProvisionerStock, ProvisionerStock, WellKeeperStock.

**What it actually does:** Reusable single-liquid vessel, capacity 12 units. Fill from nearby finite physical pools; pour one unit or all onto nearby ground to create/extend a real pool and coating. Water content can transfer into water vessels, irrigate crops and douse eligible burning objects.

**Limits and gaps:** Has no direct drink action even when filled with water. No mixing unlike liquids; finite-volume draws; liquids must exist as real pools and be registered. Current collectible IDs proven from unblocked physical pools: water, oil, acid, convalessence, memory-bath, choir-mirror-mucilage. BrinePool and MirePool have unlike water TileStateSource metadata and are rejected by the mixed-source safety gate; the other 18 liquid registry IDs have no authored physical pool source. See separate liquid appendix.

**Base affordances:** Base Commerce value 8; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:39269](../Assets/Resources/Content/Blueprints/Objects.json#L39269); [LootTables.json:52](../Assets/Resources/Content/Data/Loot/LootTables.json#L52); [TraderPart.cs:70](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L70); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87); [OverworldZoneManager.cs:1061](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1061); [LiquidVesselPart.cs:15](../Assets/Scripts/Gameplay/Items/LiquidVesselPart.cs#L15); [LiquidVesselService.cs:29](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L29); [LiquidVesselService.cs:141](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L141); [WaterTransferActions.cs:43](../Assets/Scripts/Gameplay/Items/WaterTransferActions.cs#L43); [CropWateringService.cs:20](../Assets/Scripts/Gameplay/Farming/CropWateringService.cs#L20); [DouseWorldActions.cs:43](../Assets/Scripts/Gameplay/Items/DouseWorldActions.cs#L43); [LiquidVesselService.cs:234](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L234); [LiquidVesselService.cs:297](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L297); [Objects.json:30812](../Assets/Resources/Content/Blueprints/Objects.json#L30812); [Objects.json:33100](../Assets/Resources/Content/Blueprints/Objects.json#L33100).

### drawgourd shell — `DrawgourdShell`

**Where it comes from:** Harvest DrawgourdCrop in current Spread biome patches: 1 DrawgourdShell and 1 DrawgourdSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Reusable water-only vessel, capacity 3 drinks; starts with 0. Fill from nearby usable well/cistern or clean unfrozen water; each drink relieves one Parched stack. Water can also be transferred to another water vessel, spent watering a nearby crop, or used to douse eligible burning objects.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Water only; no general liquid mixing/drinking. Drawing finite pools spends volume; wells/renewing source terrain differ. Vessel survives empty.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:44343](../Assets/Resources/Content/Blueprints/Objects.json#L44343); [BiomeCrops.json:34](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L34); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [WaterskinPart.cs:20](../Assets/Scripts/Gameplay/Items/WaterskinPart.cs#L20); [WaterVesselService.cs:29](../Assets/Scripts/Gameplay/Items/WaterVesselService.cs#L29); [WaterVesselService.cs:83](../Assets/Scripts/Gameplay/Items/WaterVesselService.cs#L83); [CropWateringService.cs:20](../Assets/Scripts/Gameplay/Farming/CropWateringService.cs#L20); [WaterTransferActions.cs:43](../Assets/Scripts/Gameplay/Items/WaterTransferActions.cs#L43); [DouseWorldActions.cs:43](../Assets/Scripts/Gameplay/Items/DouseWorldActions.cs#L43).

### sunbladder shell — `SunbladderShell`

**Where it comes from:** Harvest SunbladderCrop in current Beating biome patches: 1 SunbladderShell and 1 SunbladderSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Reusable water-only vessel, capacity 2 drinks; starts with 1. Fill from nearby usable well/cistern or clean unfrozen water; each drink relieves one Parched stack. Water can also be transferred to another water vessel, spent watering a nearby crop, or used to douse eligible burning objects.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Water only; no general liquid mixing/drinking. Drawing finite pools spends volume; wells/renewing source terrain differ. Vessel survives empty.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:46611](../Assets/Resources/Content/Blueprints/Objects.json#L46611); [BiomeCrops.json:154](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L154); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [WaterskinPart.cs:20](../Assets/Scripts/Gameplay/Items/WaterskinPart.cs#L20); [WaterVesselService.cs:29](../Assets/Scripts/Gameplay/Items/WaterVesselService.cs#L29); [WaterVesselService.cs:83](../Assets/Scripts/Gameplay/Items/WaterVesselService.cs#L83); [CropWateringService.cs:20](../Assets/Scripts/Gameplay/Farming/CropWateringService.cs#L20); [WaterTransferActions.cs:43](../Assets/Scripts/Gameplay/Items/WaterTransferActions.cs#L43); [DouseWorldActions.cs:43](../Assets/Scripts/Gameplay/Items/DouseWorldActions.cs#L43).

### greybladder cup — `GreybladderCup`

**Where it comes from:** Harvest GreybladderCrop in current Overwrit biome patches: 1 GreybladderCup and 1 GreybladderSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Reusable single-liquid vessel, capacity 6 units. Fill from nearby finite physical pools; pour one unit or all onto nearby ground to create/extend a real pool and coating. Water content can transfer into water vessels, irrigate crops and douse eligible burning objects.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Has no direct drink action even when filled with water. No mixing unlike liquids; finite-volume draws; liquids must exist as real pools and be registered. Current collectible IDs proven from unblocked physical pools: water, oil, acid, convalessence, memory-bath, choir-mirror-mucilage. BrinePool and MirePool have unlike water TileStateSource metadata and are rejected by the mixed-source safety gate; the other 18 liquid registry IDs have no authored physical pool source. See separate liquid appendix.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:50228](../Assets/Resources/Content/Blueprints/Objects.json#L50228); [BiomeCrops.json:349](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L349); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [LiquidVesselPart.cs:15](../Assets/Scripts/Gameplay/Items/LiquidVesselPart.cs#L15); [LiquidVesselService.cs:29](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L29); [LiquidVesselService.cs:141](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L141); [WaterTransferActions.cs:43](../Assets/Scripts/Gameplay/Items/WaterTransferActions.cs#L43); [CropWateringService.cs:20](../Assets/Scripts/Gameplay/Farming/CropWateringService.cs#L20); [DouseWorldActions.cs:43](../Assets/Scripts/Gameplay/Items/DouseWorldActions.cs#L43); [LiquidVesselService.cs:234](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L234); [LiquidVesselService.cs:297](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L297); [Objects.json:30812](../Assets/Resources/Content/Blueprints/Objects.json#L30812); [Objects.json:33100](../Assets/Resources/Content/Blueprints/Objects.json#L33100).

### raingourd cup — `RaingourdCup`

**Where it comes from:** Harvest RaingourdCrop in current Stump biome patches: 1 RaingourdCup and 1 RaingourdSeed per ripe plant; seed-only harvest gives 3 seeds instead. Cold generation selects the species from the five-species biome catalog; one of two placed plants is already ripe. Replanting is a continuing source.

**What it actually does:** Reusable water-only vessel, capacity 4 drinks; starts with 0. Fill from nearby usable well/cistern or clean unfrozen water; each drink relieves one Parched stack. Water can also be transferred to another water vessel, spent watering a nearby crop, or used to douse eligible burning objects.

**Limits and gaps:** Patch allocation is seed-dependent and only added during fresh generation; existing saved zones are not retrofitted. Safe-ground/route placement can refuse a patch. Water only; no general liquid mixing/drinking. Drawing finite pools spends volume; wells/renewing source terrain differ. Vessel survives empty.

**Base affordances:** Base Commerce value 3; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:50774](../Assets/Resources/Content/Blueprints/Objects.json#L50774); [BiomeCrops.json:379](../Assets/Resources/Content/Data/Farming/BiomeCrops.json#L379); [OverworldZoneManager.cs:86](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L86); [BiomeCropPlan.cs:40](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L40); [BiomeCropPlan.cs:62](../Assets/Scripts/Gameplay/World/BiomeCropPlan.cs#L62); [BiomeCropPlacement.cs:64](../Assets/Scripts/Gameplay/World/BiomeCropPlacement.cs#L64); [CropPart.cs:105](../Assets/Scripts/Gameplay/Farming/CropPart.cs#L105); [CropYieldService.cs:47](../Assets/Scripts/Gameplay/Farming/CropYieldService.cs#L47); [CultivationPreparationService.cs:14](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L14); [WaterskinPart.cs:20](../Assets/Scripts/Gameplay/Items/WaterskinPart.cs#L20); [WaterVesselService.cs:29](../Assets/Scripts/Gameplay/Items/WaterVesselService.cs#L29); [WaterVesselService.cs:83](../Assets/Scripts/Gameplay/Items/WaterVesselService.cs#L83); [CropWateringService.cs:20](../Assets/Scripts/Gameplay/Farming/CropWateringService.cs#L20); [WaterTransferActions.cs:43](../Assets/Scripts/Gameplay/Items/WaterTransferActions.cs#L43); [DouseWorldActions.cs:43](../Assets/Scripts/Gameplay/Items/DouseWorldActions.cs#L43).

## Grimoires and learned abilities

### watering grimoire — `WateringGrimoire`

**Where it comes from:** Loot/merchant table routes: SeedKeeperStock. Normal fresh characters receive one in the farming starter kit; old saves lacking farming access receive the one-time compatibility grant. Also current Spread seed-keeper stock.

**What it actually does:** Teaches Conjure Rain: waters crops in radius 3 to at least 40 moisture ticks; cooldown 5. Does not create drinking water.

**Limits and gaps:** Read is repeatable and does not consume the book; requires SkillsPart and a valid, not-yet-known skill. Learned skill remains after sale/drop. Cast obeys ability targeting/cooldown and effect immunities. No ink charge cost for this learned skill.

**Base affordances:** Base Commerce value 30; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:6123](../Assets/Resources/Content/Blueprints/Objects.json#L6123); [LootTables.json:3148](../Assets/Resources/Content/Data/Loot/LootTables.json#L3148); [GrimoirePart.cs:42](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L42); [Hydromancy_ConjureRain.cs:19](../Assets/Scripts/Gameplay/Skills/Hydromancy_ConjureRain.cs#L19); [SpreadExplorationResidents.cs:229](../Assets/Scripts/Gameplay/World/Generation/Builders/SpreadExplorationResidents.cs#L229); [GameBootstrap.cs:1193](../Assets/Scripts/Presentation/Bootstrap/GameBootstrap.cs#L1193); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### Grimoire of Kindle — `KindleGrimoire`

**Where it comes from:** Loot/merchant table routes: ArcanistStock.

**What it actually does:** Teaches Kindle: range-5 1d4 Fire projectile, applies 600 J heat and neighboring warmth, enabling ignition on susceptible creatures/scenery.

**Limits and gaps:** Read is repeatable and does not consume the book; requires SkillsPart and a valid, not-yet-known skill. Learned skill remains after sale/drop. Cast obeys ability targeting/cooldown and effect immunities. No ink charge cost for this learned skill.

**Base affordances:** Base Commerce value 75; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:25392](../Assets/Resources/Content/Blueprints/Objects.json#L25392); [LootTables.json:764](../Assets/Resources/Content/Data/Loot/LootTables.json#L764); [GrimoirePart.cs:42](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L42); [Pyromancy_Kindle.cs:16](../Assets/Scripts/Gameplay/Skills/Pyromancy_Kindle.cs#L16); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### Grimoire of Quench — `QuenchGrimoire`

**Where it comes from:** Loot/merchant table routes: ArcanistStock.

**What it actually does:** Teaches Quench: range-5 1d3 projectile, Wet 0.8 and -150 J cooling on impact; supports dousing and status preparation.

**Limits and gaps:** Read is repeatable and does not consume the book; requires SkillsPart and a valid, not-yet-known skill. Learned skill remains after sale/drop. Cast obeys ability targeting/cooldown and effect immunities. No ink charge cost for this learned skill.

**Base affordances:** Base Commerce value 75; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:25472](../Assets/Resources/Content/Blueprints/Objects.json#L25472); [LootTables.json:768](../Assets/Resources/Content/Data/Loot/LootTables.json#L768); [GrimoirePart.cs:42](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L42); [Hydromancy_Quench.cs:16](../Assets/Scripts/Gameplay/Skills/Hydromancy_Quench.cs#L16); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### Grimoire of Conflagration — `ConflagrationGrimoire`

**Where it comes from:** Loot/merchant table routes: SealedVaultT3.

**What it actually does:** Teaches Conflagration: radius-2 2d6 Heat to other creatures, Burning 1.5 on survivors, 250 J scenery heat sweep; cooldown 15.

**Limits and gaps:** Read is repeatable and does not consume the book; requires SkillsPart and a valid, not-yet-known skill. Learned skill remains after sale/drop. Cast obeys ability targeting/cooldown and effect immunities. No ink charge cost for this learned skill.

**Base affordances:** Base Commerce value 150; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:25552](../Assets/Resources/Content/Blueprints/Objects.json#L25552); [LootTables.json:442](../Assets/Resources/Content/Data/Loot/LootTables.json#L442); [GrimoirePart.cs:42](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L42); [Pyromancy_Conflagration.cs:17](../Assets/Scripts/Gameplay/Skills/Pyromancy_Conflagration.cs#L17); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### Grimoire of Ice Lance — `IceLanceGrimoire`

**Where it comes from:** Loot/merchant table routes: SealedVaultT3.

**What it actually does:** Teaches Ice Lance: range-6 1d6 Cold, -300 J cooling and cold applied to impact ground/water; cooldown 8.

**Limits and gaps:** Read is repeatable and does not consume the book; requires SkillsPart and a valid, not-yet-known skill. Learned skill remains after sale/drop. Cast obeys ability targeting/cooldown and effect immunities. No ink charge cost for this learned skill.

**Base affordances:** Base Commerce value 120; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:25632](../Assets/Resources/Content/Blueprints/Objects.json#L25632); [LootTables.json:434](../Assets/Resources/Content/Data/Loot/LootTables.json#L434); [GrimoirePart.cs:42](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L42); [Cryomancy_IceLance.cs:16](../Assets/Scripts/Gameplay/Skills/Cryomancy_IceLance.cs#L16); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### Grimoire of Acid Spray — `AcidSprayGrimoire`

**Where it comes from:** Loot/merchant table routes: SealedVaultT3.

**What it actually does:** Teaches Acid Spray: range-4 1d4 Acid projectile and Acidic 0.8 via object-status rules; cooldown 10.

**Limits and gaps:** Read is repeatable and does not consume the book; requires SkillsPart and a valid, not-yet-known skill. Learned skill remains after sale/drop. Cast obeys ability targeting/cooldown and effect immunities. No ink charge cost for this learned skill.

**Base affordances:** Base Commerce value 140; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:25712](../Assets/Resources/Content/Blueprints/Objects.json#L25712); [LootTables.json:438](../Assets/Resources/Content/Data/Loot/LootTables.json#L438); [GrimoirePart.cs:42](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L42); [Corrosion_AcidSpray.cs:14](../Assets/Scripts/Gameplay/Skills/Corrosion_AcidSpray.cs#L14); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### Grimoire of Arc Bolt — `ArcBoltGrimoire`

**Where it comes from:** Loot/merchant table routes: SealedVaultT3.

**What it actually does:** Teaches Arc Bolt: range-5 1d8 Electric projectile and Electrified 1.0 on compatible impact targets; cooldown 7.

**Limits and gaps:** Read is repeatable and does not consume the book; requires SkillsPart and a valid, not-yet-known skill. Learned skill remains after sale/drop. Cast obeys ability targeting/cooldown and effect immunities. No ink charge cost for this learned skill.

**Base affordances:** Base Commerce value 120; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:25792](../Assets/Resources/Content/Blueprints/Objects.json#L25792); [LootTables.json:430](../Assets/Resources/Content/Data/Loot/LootTables.json#L430); [GrimoirePart.cs:42](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L42); [Galvanism_ArcBolt.cs:15](../Assets/Scripts/Gameplay/Skills/Galvanism_ArcBolt.cs#L15); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### Grimoire of Rime Nova — `RimeNovaGrimoire`

**Where it comes from:** Loot/merchant table routes: SealedVaultT3, BookshelfT3.

**What it actually does:** Teaches Rime Nova: radius-2 1d6 Cold to creatures, Frozen 0.6 survivors, -200 J scenery sweep and freezes ground water; cooldown 15.

**Limits and gaps:** Read is repeatable and does not consume the book; requires SkillsPart and a valid, not-yet-known skill. Learned skill remains after sale/drop. Cast obeys ability targeting/cooldown and effect immunities. No ink charge cost for this learned skill.

**Base affordances:** Base Commerce value 220; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:25872](../Assets/Resources/Content/Blueprints/Objects.json#L25872); [LootTables.json:446](../Assets/Resources/Content/Data/Loot/LootTables.json#L446); [GrimoirePart.cs:42](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L42); [Cryomancy_RimeNova.cs:16](../Assets/Scripts/Gameplay/Skills/Cryomancy_RimeNova.cs#L16); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### Grimoire of Thunderclap — `ThunderclapGrimoire`

**Where it comes from:** Loot/merchant table routes: SealedVaultT3, BookshelfT3.

**What it actually does:** Teaches Thunderclap: radius-2 2d6 Electric, doubled against Wet moisture >0.2; electrifies survivors and conductive scenery; cooldown 18.

**Limits and gaps:** Read is repeatable and does not consume the book; requires SkillsPart and a valid, not-yet-known skill. Learned skill remains after sale/drop. Cast obeys ability targeting/cooldown and effect immunities. No ink charge cost for this learned skill.

**Base affordances:** Base Commerce value 240; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:25952](../Assets/Resources/Content/Blueprints/Objects.json#L25952); [LootTables.json:450](../Assets/Resources/Content/Data/Loot/LootTables.json#L450); [GrimoirePart.cs:42](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L42); [Galvanism_Thunderclap.cs:24](../Assets/Scripts/Gameplay/Skills/Galvanism_Thunderclap.cs#L24); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### Grimoire of Ember Vein — `EmberVeinGrimoire`

**Where it comes from:** Loot/merchant table routes: SealedVaultT3.

**What it actually does:** Teaches Ember Vein: range-7 beam, 2d6 Heat to creatures along it and heat applied to scenery; cooldown 12.

**Limits and gaps:** Read is repeatable and does not consume the book; requires SkillsPart and a valid, not-yet-known skill. Learned skill remains after sale/drop. Cast obeys ability targeting/cooldown and effect immunities. No ink charge cost for this learned skill.

**Base affordances:** Base Commerce value 200; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:26032](../Assets/Resources/Content/Blueprints/Objects.json#L26032); [LootTables.json:454](../Assets/Resources/Content/Data/Loot/LootTables.json#L454); [GrimoirePart.cs:42](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L42); [Pyromancy_EmberVein.cs:18](../Assets/Scripts/Gameplay/Skills/Pyromancy_EmberVein.cs#L18); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### Grimoire of Kindle Flame — `KindleFlameGrimoire`

**Where it comes from:** Loot/merchant table routes: LibraryShelfT1, ArcanistStock.

**What it actually does:** Teaches Kindle Flame: 150 J to adjacent non-creature scenery with FlameTemperature <250; cooldown 2. No direct creature attack.

**Limits and gaps:** Read is repeatable and does not consume the book; requires SkillsPart and a valid, not-yet-known skill. Learned skill remains after sale/drop. Cast obeys ability targeting/cooldown and effect immunities. No ink charge cost for this learned skill.

**Base affordances:** Base Commerce value 35; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:26112](../Assets/Resources/Content/Blueprints/Objects.json#L26112); [LootTables.json:242](../Assets/Resources/Content/Data/Loot/LootTables.json#L242); [GrimoirePart.cs:42](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L42); [Pyromancy_KindleFlame.cs:18](../Assets/Scripts/Gameplay/Skills/Pyromancy_KindleFlame.cs#L18); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### Grimoire of Drying Breeze — `DryingBreezeGrimoire`

**Where it comes from:** Loot/merchant table routes: LibraryShelfT1, ArcanistStock. Quillhold authored Scribe stock.

**What it actually does:** Teaches Drying Breeze: removes Wet from entities in radius 1 (including caster) and temporary ground-water coatings; cooldown 3.

**Limits and gaps:** Read is repeatable and does not consume the book; requires SkillsPart and a valid, not-yet-known skill. Learned skill remains after sale/drop. Cast obeys ability targeting/cooldown and effect immunities. No ink charge cost for this learned skill.

**Base affordances:** Base Commerce value 40; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:26192](../Assets/Resources/Content/Blueprints/Objects.json#L26192); [LootTables.json:246](../Assets/Resources/Content/Data/Loot/LootTables.json#L246); [GrimoirePart.cs:42](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L42); [Hydromancy_DryingBreeze.cs:14](../Assets/Scripts/Gameplay/Skills/Hydromancy_DryingBreeze.cs#L14); [QuillholdCompositionBuilder.cs:106](../Assets/Scripts/Gameplay/World/Generation/Builders/QuillholdCompositionBuilder.cs#L106); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### Grimoire of Hearthwarm — `HearthwarmGrimoire`

**Where it comes from:** Loot/merchant table routes: LibraryShelfT1, ArcanistStock.

**What it actually does:** Teaches Hearthwarm: adjacent target cell receives a 3-turn stationary hearth aura, 60 J heat per pulse; cooldown 4.

**Limits and gaps:** Read is repeatable and does not consume the book; requires SkillsPart and a valid, not-yet-known skill. Learned skill remains after sale/drop. Cast obeys ability targeting/cooldown and effect immunities. No ink charge cost for this learned skill.

**Base affordances:** Base Commerce value 45; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:26272](../Assets/Resources/Content/Blueprints/Objects.json#L26272); [LootTables.json:254](../Assets/Resources/Content/Data/Loot/LootTables.json#L254); [GrimoirePart.cs:42](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L42); [Pyromancy_Hearthwarm.cs:16](../Assets/Scripts/Gameplay/Skills/Pyromancy_Hearthwarm.cs#L16); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### Grimoire of Conjure Water — `ConjureWaterGrimoire`

**Where it comes from:** Loot/merchant table routes: LibraryShelfT1, ArcanistStock.

**What it actually does:** Teaches Conjure Water: range-2 directional stream creates a WaterPuddle and wets occupants (0.5); reevaluates burning reactions; cooldown 4.

**Limits and gaps:** Read is repeatable and does not consume the book; requires SkillsPart and a valid, not-yet-known skill. Learned skill remains after sale/drop. Cast obeys ability targeting/cooldown and effect immunities. No ink charge cost for this learned skill.

**Base affordances:** Base Commerce value 50; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:26352](../Assets/Resources/Content/Blueprints/Objects.json#L26352); [LootTables.json:258](../Assets/Resources/Content/Data/Loot/LootTables.json#L258); [GrimoirePart.cs:42](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L42); [Hydromancy_ConjureWater.cs:25](../Assets/Scripts/Gameplay/Skills/Hydromancy_ConjureWater.cs#L25); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### Grimoire of Chill Draft — `ChillDraftGrimoire`

**Where it comes from:** Loot/merchant table routes: LibraryShelfT1, ArcanistStock.

**What it actually does:** Teaches Chill Draft: -100 J to thermal entities in radius 1, cooling scenery/actors; cooldown 5.

**Limits and gaps:** Read is repeatable and does not consume the book; requires SkillsPart and a valid, not-yet-known skill. Learned skill remains after sale/drop. Cast obeys ability targeting/cooldown and effect immunities. No ink charge cost for this learned skill.

**Base affordances:** Base Commerce value 45; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:26432](../Assets/Resources/Content/Blueprints/Objects.json#L26432); [LootTables.json:250](../Assets/Resources/Content/Data/Loot/LootTables.json#L250); [GrimoirePart.cs:42](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L42); [Cryomancy_ChillDraft.cs:16](../Assets/Scripts/Gameplay/Skills/Cryomancy_ChillDraft.cs#L16); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### Grimoire of Ward Gleam — `WardGleamGrimoire`

**Where it comes from:** Loot/merchant table routes: LibraryShelfT1, ArcanistStock, BookshelfT2, ScribeStock. Quillhold authored Scribe stock.

**What it actually does:** Teaches Ward Gleam: removes Acidic and Charred from carried/equipped items; cooldown 15. No heal or repair of already lost stats/objects.

**Limits and gaps:** Read is repeatable and does not consume the book; requires SkillsPart and a valid, not-yet-known skill. Learned skill remains after sale/drop. Cast obeys ability targeting/cooldown and effect immunities. No ink charge cost for this learned skill.

**Base affordances:** Base Commerce value 60; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:26512](../Assets/Resources/Content/Blueprints/Objects.json#L26512); [LootTables.json:262](../Assets/Resources/Content/Data/Loot/LootTables.json#L262); [GrimoirePart.cs:42](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L42); [Spellcraft_WardGleam.cs:15](../Assets/Scripts/Gameplay/Skills/Spellcraft_WardGleam.cs#L15); [QuillholdCompositionBuilder.cs:106](../Assets/Scripts/Gameplay/World/Generation/Builders/QuillholdCompositionBuilder.cs#L106); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### Ditchkeepers' Footwork — `DitchkeepersFootwork`

**Where it comes from:** Connected Spread satellite finite supply containers (wet/dry variants), via ConnectedSpreadSatellite.

**What it actually does:** Teaches Vault: move two cells across one blocker with an unoccupied clear landing and a brief melee accuracy opportunity; cooldown 15. Cannot cross sealed archive barriers.

**Limits and gaps:** Read is repeatable and does not consume the book; requires SkillsPart and a valid, not-yet-known skill. Learned skill remains after sale/drop. Cast obeys ability targeting/cooldown and effect immunities. No ink charge cost for this learned skill.

**Base affordances:** Base Commerce value 20; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:53798](../Assets/Resources/Content/Blueprints/Objects.json#L53798); [ConnectedSpreadSatellite.cs:44](../Assets/Scripts/Gameplay/World/Generation/Builders/ConnectedSpreadSatellite.cs#L44); [GrimoirePart.cs:42](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L42); [Acrobatics_Vault.cs:26](../Assets/Scripts/Gameplay/Skills/Acrobatics_Vault.cs#L26); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432).

### Rite of the Storm Anvil — `StormAnvilGrimoire`

**Where it comes from:** Loot/merchant table routes: ArcanistStock, ScribeStock, CuratorStock, EchoStock.

**What it actually does:** Read teaches Rites_StormAnvil. Radius 2 Electric hit (base 4); consumes up to 2 resonance statuses per victim; status riders can stun 3 turns, electrify, or break. The physical book provides 10 initial ink charges usable by any known consuming rite.

**Limits and gaps:** Read is repeatable and does not consume the book; requires SkillsPart and a valid, not-yet-known skill. Learned skill remains after sale/drop. Cast obeys ability targeting/cooldown and effect immunities. Each successful cast spends 1 charge from the first carried inked book (not necessarily its own title). Re-ink carried singleton with 1 InkVial for up to 5 charges, capped 10. Marks are actual compatible status effects and are consumed; no mark means weak base effect. Targets are creatures, self for Veil; range/shape/cooldown rules apply.

**Base affordances:** Base Commerce value 20; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:30054](../Assets/Resources/Content/Blueprints/Objects.json#L30054); [LootTables.json:820](../Assets/Resources/Content/Data/Loot/LootTables.json#L820); [GrimoirePart.cs:42](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L42); [Rites_StormAnvil.cs:11](../Assets/Scripts/Gameplay/Skills/Rites_StormAnvil.cs#L11); [ConsumingRiteSkillBase.cs:157](../Assets/Scripts/Gameplay/Skills/ConsumingRiteSkillBase.cs#L157); [GrimoireInk.cs:29](../Assets/Scripts/Gameplay/Magic/GrimoireInk.cs#L29); [GrimoireInkService.cs:24](../Assets/Scripts/Gameplay/Magic/GrimoireInkService.cs#L24); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### Rite of the Hanging Bolt — `HangingBoltGrimoire`

**Where it comes from:** Loot/merchant table routes: ArcanistStock, CuratorStock.

**What it actually does:** Read teaches Rites_HangingBolt. Single target range 6, flat 2 Electric damage; consumes up to 2 marks for 2 paralysis turns each. The physical book provides 10 initial ink charges usable by any known consuming rite.

**Limits and gaps:** Read is repeatable and does not consume the book; requires SkillsPart and a valid, not-yet-known skill. Learned skill remains after sale/drop. Cast obeys ability targeting/cooldown and effect immunities. Each successful cast spends 1 charge from the first carried inked book (not necessarily its own title). Re-ink carried singleton with 1 InkVial for up to 5 charges, capped 10. Marks are actual compatible status effects and are consumed; no mark means weak base effect. Targets are creatures, self for Veil; range/shape/cooldown rules apply.

**Base affordances:** Base Commerce value 20; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:30113](../Assets/Resources/Content/Blueprints/Objects.json#L30113); [LootTables.json:824](../Assets/Resources/Content/Data/Loot/LootTables.json#L824); [GrimoirePart.cs:42](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L42); [Rites_HangingBolt.cs:11](../Assets/Scripts/Gameplay/Skills/Rites_HangingBolt.cs#L11); [ConsumingRiteSkillBase.cs:157](../Assets/Scripts/Gameplay/Skills/ConsumingRiteSkillBase.cs#L157); [GrimoireInk.cs:29](../Assets/Scripts/Gameplay/Magic/GrimoireInk.cs#L29); [GrimoireInkService.cs:24](../Assets/Scripts/Gameplay/Magic/GrimoireInkService.cs#L24); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### Rite of Rendered Steam — `RenderedSteamGrimoire`

**Where it comes from:** Loot/merchant table routes: ArcanistStock, ScribeStock, CuratorStock.

**What it actually does:** Read teaches Rites_RenderedSteam. Radius 2 Heat hit (base 5), up to 2 marks; consuming both Wet and Burning adds 1.5 multiplier and 3-turn confusion. The physical book provides 10 initial ink charges usable by any known consuming rite.

**Limits and gaps:** Read is repeatable and does not consume the book; requires SkillsPart and a valid, not-yet-known skill. Learned skill remains after sale/drop. Cast obeys ability targeting/cooldown and effect immunities. Each successful cast spends 1 charge from the first carried inked book (not necessarily its own title). Re-ink carried singleton with 1 InkVial for up to 5 charges, capped 10. Marks are actual compatible status effects and are consumed; no mark means weak base effect. Targets are creatures, self for Veil; range/shape/cooldown rules apply.

**Base affordances:** Base Commerce value 20; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:30172](../Assets/Resources/Content/Blueprints/Objects.json#L30172); [LootTables.json:828](../Assets/Resources/Content/Data/Loot/LootTables.json#L828); [GrimoirePart.cs:42](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L42); [Rites_RenderedSteam.cs:12](../Assets/Scripts/Gameplay/Skills/Rites_RenderedSteam.cs#L12); [ConsumingRiteSkillBase.cs:157](../Assets/Scripts/Gameplay/Skills/ConsumingRiteSkillBase.cs#L157); [GrimoireInk.cs:29](../Assets/Scripts/Gameplay/Magic/GrimoireInk.cs#L29); [GrimoireInkService.cs:24](../Assets/Scripts/Gameplay/Magic/GrimoireInkService.cs#L24); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### Rite of the Scalding Veil — `ScaldingVeilGrimoire`

**Where it comes from:** Loot/merchant table routes: ArcanistStock, EchoStock.

**What it actually does:** Read teaches Rites_ScaldingVeil. Self, requires Wet; consumes Wet to grant 8-turn ScaldingVeil that scalds attackers for 3 and confuses for 2. The physical book provides 10 initial ink charges usable by any known consuming rite.

**Limits and gaps:** Read is repeatable and does not consume the book; requires SkillsPart and a valid, not-yet-known skill. Learned skill remains after sale/drop. Cast obeys ability targeting/cooldown and effect immunities. Each successful cast spends 1 charge from the first carried inked book (not necessarily its own title). Re-ink carried singleton with 1 InkVial for up to 5 charges, capped 10. Marks are actual compatible status effects and are consumed; no mark means weak base effect. Targets are creatures, self for Veil; range/shape/cooldown rules apply.

**Base affordances:** Base Commerce value 20; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:30231](../Assets/Resources/Content/Blueprints/Objects.json#L30231); [LootTables.json:832](../Assets/Resources/Content/Data/Loot/LootTables.json#L832); [GrimoirePart.cs:42](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L42); [Rites_ScaldingVeil.cs:12](../Assets/Scripts/Gameplay/Skills/Rites_ScaldingVeil.cs#L12); [ConsumingRiteSkillBase.cs:157](../Assets/Scripts/Gameplay/Skills/ConsumingRiteSkillBase.cs#L157); [GrimoireInk.cs:29](../Assets/Scripts/Gameplay/Magic/GrimoireInk.cs#L29); [GrimoireInkService.cs:24](../Assets/Scripts/Gameplay/Magic/GrimoireInkService.cs#L24); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### Fulmination — `FulminationGrimoire`

**Where it comes from:** Loot/merchant table routes: ArcanistStock, CuratorStock, EchoStock.

**What it actually does:** Read teaches Rites_Fulmination. Single target range 5 Electric hit (base 3), up to 1 mark; writes 2 electric ground charge even if target dies. The physical book provides 10 initial ink charges usable by any known consuming rite.

**Limits and gaps:** Read is repeatable and does not consume the book; requires SkillsPart and a valid, not-yet-known skill. Learned skill remains after sale/drop. Cast obeys ability targeting/cooldown and effect immunities. Each successful cast spends 1 charge from the first carried inked book (not necessarily its own title). Re-ink carried singleton with 1 InkVial for up to 5 charges, capped 10. Marks are actual compatible status effects and are consumed; no mark means weak base effect. Targets are creatures, self for Veil; range/shape/cooldown rules apply.

**Base affordances:** Base Commerce value 20; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:30340](../Assets/Resources/Content/Blueprints/Objects.json#L30340); [LootTables.json:836](../Assets/Resources/Content/Data/Loot/LootTables.json#L836); [GrimoirePart.cs:42](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L42); [Rites_Fulmination.cs:13](../Assets/Scripts/Gameplay/Skills/Rites_Fulmination.cs#L13); [ConsumingRiteSkillBase.cs:157](../Assets/Scripts/Gameplay/Skills/ConsumingRiteSkillBase.cs#L157); [GrimoireInk.cs:29](../Assets/Scripts/Gameplay/Magic/GrimoireInk.cs#L29); [GrimoireInkService.cs:24](../Assets/Scripts/Gameplay/Magic/GrimoireInkService.cs#L24); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### Rite of the Shattered Rime — `ShatteredRimeGrimoire`

**Where it comes from:** Loot/merchant table routes: LibraryShelfT1, ZigguratVaultT2, CrateT2, CrateT3, BookshelfT1, BookshelfT2. Also northern Gleaners Cellar reward; connected Marrowstye Junior Indexer carries one for trade.

**What it actually does:** Read teaches Rites_ShatteredRime. Range-3 cone hit (base 6), up to 2 Cold-compatible marks; consuming any mark shatters survivor armor. The physical book provides 10 initial ink charges usable by any known consuming rite.

**Limits and gaps:** Read is repeatable and does not consume the book; requires SkillsPart and a valid, not-yet-known skill. Learned skill remains after sale/drop. Cast obeys ability targeting/cooldown and effect immunities. Each successful cast spends 1 charge from the first carried inked book (not necessarily its own title). Re-ink carried singleton with 1 InkVial for up to 5 charges, capped 10. Marks are actual compatible status effects and are consumed; no mark means weak base effect. Targets are creatures, self for Veil; range/shape/cooldown rules apply.

**Base affordances:** Base Commerce value 20; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:30399](../Assets/Resources/Content/Blueprints/Objects.json#L30399); [LootTables.json:272](../Assets/Resources/Content/Data/Loot/LootTables.json#L272); [GrimoirePart.cs:42](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L42); [Rites_ShatteredRime.cs:9](../Assets/Scripts/Gameplay/Skills/Rites_ShatteredRime.cs#L9); [ConsumingRiteSkillBase.cs:157](../Assets/Scripts/Gameplay/Skills/ConsumingRiteSkillBase.cs#L157); [GrimoireInk.cs:29](../Assets/Scripts/Gameplay/Magic/GrimoireInk.cs#L29); [GrimoireInkService.cs:24](../Assets/Scripts/Gameplay/Magic/GrimoireInkService.cs#L24); [GleanersCellarBuilder.cs:110](../Assets/Scripts/Gameplay/World/Generation/Builders/GleanersCellarBuilder.cs#L110); [CurationReceivingBuilder.cs:112](../Assets/Scripts/Gameplay/World/Generation/Builders/CurationReceivingBuilder.cs#L112); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### Rite of the Still Heart — `StillHeartGrimoire`

**Where it comes from:** Loot/merchant table routes: TombVaultT2, SealedVaultT3, UrnT3, ReliquaryT2, BookshelfT3.

**What it actually does:** Read teaches Rites_StillHeart. Single target range 5 Cold hit (base 2); up to 2 consumed marks grant 8 turns sleep each, broken on attack. The physical book provides 10 initial ink charges usable by any known consuming rite.

**Limits and gaps:** Read is repeatable and does not consume the book; requires SkillsPart and a valid, not-yet-known skill. Learned skill remains after sale/drop. Cast obeys ability targeting/cooldown and effect immunities. Each successful cast spends 1 charge from the first carried inked book (not necessarily its own title). Re-ink carried singleton with 1 InkVial for up to 5 charges, capped 10. Marks are actual compatible status effects and are consumed; no mark means weak base effect. Targets are creatures, self for Veil; range/shape/cooldown rules apply.

**Base affordances:** Base Commerce value 20; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:30458](../Assets/Resources/Content/Blueprints/Objects.json#L30458); [LootTables.json:373](../Assets/Resources/Content/Data/Loot/LootTables.json#L373); [GrimoirePart.cs:42](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L42); [Rites_StillHeart.cs:10](../Assets/Scripts/Gameplay/Skills/Rites_StillHeart.cs#L10); [ConsumingRiteSkillBase.cs:157](../Assets/Scripts/Gameplay/Skills/ConsumingRiteSkillBase.cs#L157); [GrimoireInk.cs:29](../Assets/Scripts/Gameplay/Magic/GrimoireInk.cs#L29); [GrimoireInkService.cs:24](../Assets/Scripts/Gameplay/Magic/GrimoireInkService.cs#L24); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### Rite of the Verdigris Bloom — `VerdigrisBloomGrimoire`

**Where it comes from:** Loot/merchant table routes: ZigguratVaultT2, CultCacheT2, DeepSupplyT2, StrongBoxT2, ReliquaryT2, AlchemyShelfT1.

**What it actually does:** Read teaches Rites_VerdigrisBloom. Radius 2 Acid hit (base 5); up to 2 consumed marks, with shatter armor and Acidic 0.6 on marked survivors. The physical book provides 10 initial ink charges usable by any known consuming rite.

**Limits and gaps:** Read is repeatable and does not consume the book; requires SkillsPart and a valid, not-yet-known skill. Learned skill remains after sale/drop. Cast obeys ability targeting/cooldown and effect immunities. Each successful cast spends 1 charge from the first carried inked book (not necessarily its own title). Re-ink carried singleton with 1 InkVial for up to 5 charges, capped 10. Marks are actual compatible status effects and are consumed; no mark means weak base effect. Targets are creatures, self for Veil; range/shape/cooldown rules apply.

**Base affordances:** Base Commerce value 20; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:30517](../Assets/Resources/Content/Blueprints/Objects.json#L30517); [LootTables.json:414](../Assets/Resources/Content/Data/Loot/LootTables.json#L414); [GrimoirePart.cs:42](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L42); [Rites_VerdigrisBloom.cs:9](../Assets/Scripts/Gameplay/Skills/Rites_VerdigrisBloom.cs#L9); [ConsumingRiteSkillBase.cs:157](../Assets/Scripts/Gameplay/Skills/ConsumingRiteSkillBase.cs#L157); [GrimoireInk.cs:29](../Assets/Scripts/Gameplay/Magic/GrimoireInk.cs#L29); [GrimoireInkService.cs:24](../Assets/Scripts/Gameplay/Magic/GrimoireInkService.cs#L24); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### Rite of the Hollow Coin — `HollowCoinGrimoire`

**Where it comes from:** Loot/merchant table routes: BanditCacheT2, SealedVaultT3, WorkshopCacheT2, StrongBoxT3, ReliquaryT3.

**What it actually does:** Read teaches Rites_HollowCoin. Single target range 4 hit (base 3), consumes up to 3 marks; three marks add Broken. The physical book provides 10 initial ink charges usable by any known consuming rite.

**Limits and gaps:** Read is repeatable and does not consume the book; requires SkillsPart and a valid, not-yet-known skill. Learned skill remains after sale/drop. Cast obeys ability targeting/cooldown and effect immunities. Each successful cast spends 1 charge from the first carried inked book (not necessarily its own title). Re-ink carried singleton with 1 InkVial for up to 5 charges, capped 10. Marks are actual compatible status effects and are consumed; no mark means weak base effect. Targets are creatures, self for Veil; range/shape/cooldown rules apply.

**Base affordances:** Base Commerce value 20; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:30576](../Assets/Resources/Content/Blueprints/Objects.json#L30576); [LootTables.json:208](../Assets/Resources/Content/Data/Loot/LootTables.json#L208); [GrimoirePart.cs:42](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L42); [Rites_HollowCoin.cs:10](../Assets/Scripts/Gameplay/Skills/Rites_HollowCoin.cs#L10); [ConsumingRiteSkillBase.cs:157](../Assets/Scripts/Gameplay/Skills/ConsumingRiteSkillBase.cs#L157); [GrimoireInk.cs:29](../Assets/Scripts/Gameplay/Magic/GrimoireInk.cs#L29); [GrimoireInkService.cs:24](../Assets/Scripts/Gameplay/Magic/GrimoireInkService.cs#L24); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### Rite of the Sundering Word — `SunderingWordGrimoire`

**Where it comes from:** Loot/merchant table routes: LairLoot, SealedVaultT3, CultCacheT2, ReliquaryT3, BookshelfT3.

**What it actually does:** Read teaches Rites_SunderingWord. Radius 2 hit (base 4), consumes up to 2 marks; marked survivors receive Broken and Weakened. The physical book provides 10 initial ink charges usable by any known consuming rite.

**Limits and gaps:** Read is repeatable and does not consume the book; requires SkillsPart and a valid, not-yet-known skill. Learned skill remains after sale/drop. Cast obeys ability targeting/cooldown and effect immunities. Each successful cast spends 1 charge from the first carried inked book (not necessarily its own title). Re-ink carried singleton with 1 InkVial for up to 5 charges, capped 10. Marks are actual compatible status effects and are consumed; no mark means weak base effect. Targets are creatures, self for Veil; range/shape/cooldown rules apply.

**Base affordances:** Base Commerce value 20; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:30635](../Assets/Resources/Content/Blueprints/Objects.json#L30635); [LootTables.json:130](../Assets/Resources/Content/Data/Loot/LootTables.json#L130); [GrimoirePart.cs:42](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L42); [Rites_SunderingWord.cs:10](../Assets/Scripts/Gameplay/Skills/Rites_SunderingWord.cs#L10); [ConsumingRiteSkillBase.cs:157](../Assets/Scripts/Gameplay/Skills/ConsumingRiteSkillBase.cs#L157); [GrimoireInk.cs:29](../Assets/Scripts/Gameplay/Magic/GrimoireInk.cs#L29); [GrimoireInkService.cs:24](../Assets/Scripts/Gameplay/Magic/GrimoireInkService.cs#L24); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### Rite of the Bloodletter's Ledger — `BloodletterLedgerGrimoire`

**Where it comes from:** Loot/merchant table routes: LibraryShelfT1, TombVaultT2, UrnT2, BookshelfT2, ReliquaryT1.

**What it actually does:** Read teaches Rites_BloodletterLedger. Single target range 5 hit (base 4), up to 2 consumed marks; adds Bleeding and heals caster 4 HP per mark, even if victim dies. The physical book provides 10 initial ink charges usable by any known consuming rite.

**Limits and gaps:** Read is repeatable and does not consume the book; requires SkillsPart and a valid, not-yet-known skill. Learned skill remains after sale/drop. Cast obeys ability targeting/cooldown and effect immunities. Each successful cast spends 1 charge from the first carried inked book (not necessarily its own title). Re-ink carried singleton with 1 InkVial for up to 5 charges, capped 10. Marks are actual compatible status effects and are consumed; no mark means weak base effect. Targets are creatures, self for Veil; range/shape/cooldown rules apply.

**Base affordances:** Base Commerce value 20; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:30694](../Assets/Resources/Content/Blueprints/Objects.json#L30694); [LootTables.json:276](../Assets/Resources/Content/Data/Loot/LootTables.json#L276); [GrimoirePart.cs:42](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L42); [Rites_BloodletterLedger.cs:11](../Assets/Scripts/Gameplay/Skills/Rites_BloodletterLedger.cs#L11); [ConsumingRiteSkillBase.cs:157](../Assets/Scripts/Gameplay/Skills/ConsumingRiteSkillBase.cs#L157); [GrimoireInk.cs:29](../Assets/Scripts/Gameplay/Magic/GrimoireInk.cs#L29); [GrimoireInkService.cs:24](../Assets/Scripts/Gameplay/Magic/GrimoireInkService.cs#L24); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### Grimoire of Ward Gleam — `QuillholdLoanWardGleam`

**Where it comes from:** Quillhold loan shelf; SecondExplorationSites requires Exploration enabled/version >=15, only installed in newly generated accepted zone.

**What it actually does:** Teaches Ward Gleam: removes Acidic and Charred from carried/equipped items; cooldown 15. No heal or repair of already lost stats/objects. Reading permanently teaches the skill even after returning the physical loan.

**Limits and gaps:** Costs rental Ink; keeps RentalPart restrictions until returned/bought out. Shelf borrowing itself has no turn-based expiry. No charge part.

**Base affordances:** Base Commerce value 60; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:126](../Assets/Resources/Content/Blueprints/Objects.json#L126); [SecondExplorationSites.cs:66](../Assets/Scripts/Gameplay/Exploration/SecondExplorationSites.cs#L66); [SecondExplorationObjects.cs:123](../Assets/Scripts/Gameplay/Exploration/SecondExplorationObjects.cs#L123); [GrimoirePart.cs:82](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L82); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432).

### Grimoire of Drying Breeze — `QuillholdLoanDryingBreeze`

**Where it comes from:** Quillhold loan shelf; SecondExplorationSites requires Exploration enabled/version >=15, only installed in newly generated accepted zone.

**What it actually does:** Teaches Drying Breeze: removes Wet from entities in radius 1 (including caster) and temporary ground-water coatings; cooldown 3. Reading permanently teaches the skill even after returning the physical loan.

**Limits and gaps:** Costs rental Ink; keeps RentalPart restrictions until returned/bought out. Shelf borrowing itself has no turn-based expiry. No charge part.

**Base affordances:** Base Commerce value 40; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:147](../Assets/Resources/Content/Blueprints/Objects.json#L147); [SecondExplorationSites.cs:66](../Assets/Scripts/Gameplay/Exploration/SecondExplorationSites.cs#L66); [SecondExplorationObjects.cs:123](../Assets/Scripts/Gameplay/Exploration/SecondExplorationObjects.cs#L123); [GrimoirePart.cs:82](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L82); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432).

### Water-Keeper's Grimoire — `PurifyWaterGrimoire`

**Where it comes from:** VillageGrimoireChest placed in ordinary generated villages; current Morrowfast uses a separate pipeline.

**What it actually does:** Read grants KnowsPurifyWater; enables temporary well restoration through settlement dialogue/world repair system.

**Limits and gaps:** Nonconsuming book. This is a settlement knowledge flag, not a hotbar combat spell. Temporary repair relapses; Sill and non-Morrowfast village pipelines supply the books and repair targets.

**Base affordances:** Base Commerce value 50; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:24539](../Assets/Resources/Content/Blueprints/Objects.json#L24539); [LootTables.json:139](../Assets/Resources/Content/Data/Loot/LootTables.json#L139); [VillagePopulationBuilder.cs:262](../Assets/Scripts/Gameplay/World/Generation/Builders/VillagePopulationBuilder.cs#L262); [SettlementManager.cs:124](../Assets/Scripts/Gameplay/Settlements/SettlementManager.cs#L124); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### Mending Rite Grimoire — `MendingRiteGrimoire`

**Where it comes from:** VillageGrimoireChest placed in ordinary generated villages; current Morrowfast uses a separate pipeline.

**What it actually does:** Read grants KnowsMendingRite; enables temporary oven restoration through settlement dialogue/world repair system.

**Limits and gaps:** Nonconsuming book. This is a settlement knowledge flag, not a hotbar combat spell. Temporary repair relapses; Sill and non-Morrowfast village pipelines supply the books and repair targets.

**Base affordances:** Base Commerce value 50; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:25023](../Assets/Resources/Content/Blueprints/Objects.json#L25023); [LootTables.json:142](../Assets/Resources/Content/Data/Loot/LootTables.json#L142); [VillagePopulationBuilder.cs:262](../Assets/Scripts/Gameplay/World/Generation/Builders/VillagePopulationBuilder.cs#L262); [SettlementManager.cs:176](../Assets/Scripts/Gameplay/Settlements/SettlementManager.cs#L176); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### Kindle Rite Grimoire — `KindleRiteGrimoire`

**Where it comes from:** VillageGrimoireChest placed in ordinary generated villages; current Morrowfast uses a separate pipeline.

**What it actually does:** Read grants KnowsKindleRite; enables temporary lantern restoration through settlement dialogue/world repair system.

**Limits and gaps:** Nonconsuming book. This is a settlement knowledge flag, not a hotbar combat spell. Temporary repair relapses; Sill and non-Morrowfast village pipelines supply the books and repair targets.

**Base affordances:** Base Commerce value 50; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:25312](../Assets/Resources/Content/Blueprints/Objects.json#L25312); [LootTables.json:145](../Assets/Resources/Content/Data/Loot/LootTables.json#L145); [VillagePopulationBuilder.cs:262](../Assets/Scripts/Gameplay/World/Generation/Builders/VillagePopulationBuilder.cs#L262); [SettlementManager.cs:234](../Assets/Scripts/Gameplay/Settlements/SettlementManager.cs#L234); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### grimoire copy — `GrimoireCopy`

**Where it comes from:** Loot routes: ReliquaryT2, BookshelfT1, BookshelfT2, BookshelfT3, ReliquaryT1, ScribeStock, ElderStock, CuratorStock, ChoirStock, EchoStock. Scribe dialogue makes a payload copy; Quillhold selected-copy service costs 5 drams + 1 InkVial.

**What it actually does:** A scribe-created copy teaches the copied skill/knowledge. Any GrimoireCopy-tagged copy can be consumed to improve a stably repaired oven/lantern caretaker. Can also be handed to the well keeper.

**Limits and gaps:** Raw loot/merchant copies have empty skill and knowledge and Read says pages are blank. Well-keeper donation only consumes the copy and displays dialogue; it does not actually teach the NPC purification or improve site state. Copies of rites have no GrimoireCharge and cannot supply ink. Donating accepts tag regardless of copied content.

**Base affordances:** Base Commerce value 20; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:24619](../Assets/Resources/Content/Blueprints/Objects.json#L24619); [LootTables.json:2179](../Assets/Resources/Content/Data/Loot/LootTables.json#L2179); [GrimoirePart.cs:101](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L101); [ConversationActions.cs:256](../Assets/Scripts/Gameplay/Conversations/ConversationActions.cs#L256); [SecondExplorationServices.cs:36](../Assets/Scripts/Gameplay/Exploration/SecondExplorationServices.cs#L36); [SettlementManager.cs:213](../Assets/Scripts/Gameplay/Settlements/SettlementManager.cs#L213); [FriendlyNPCs.json:872](../Assets/Resources/Content/Conversations/FriendlyNPCs.json#L872); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

## Repair guides

### well maintenance manual — `WellMaintenanceManual`

**Where it comes from:** Given by relevant well keeper/farmer/warden conversation; also village Elder guidance dialogue.

**What it actually does:** Carry guide and consume one SilverSand in settlement well repair conversation to make stable repair. Guide remains.

**Limits and gaps:** No Read/Study action or standalone recipe teaching. Its useful effect is inventory possession gating a specific settlement repair. Already stable/improved site refuses.

**Base affordances:** Base Commerce value 20; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:23930](../Assets/Resources/Content/Blueprints/Objects.json#L23930); [FriendlyNPCs.json:929](../Assets/Resources/Content/Conversations/FriendlyNPCs.json#L929); [Villagers.json:624](../Assets/Resources/Content/Conversations/Villagers.json#L624); [SettlementManager.cs:146](../Assets/Scripts/Gameplay/Settlements/SettlementManager.cs#L146).

### oven builder's guide — `OvenBuildersGuide`

**Where it comes from:** Given by relevant well keeper/farmer/warden conversation; also village Elder guidance dialogue.

**What it actually does:** Carry guide and consume one FireClay in settlement oven repair conversation to make stable repair. Guide remains.

**Limits and gaps:** No Read/Study action or standalone recipe teaching. Its useful effect is inventory possession gating a specific settlement repair. Already stable/improved site refuses.

**Base affordances:** Base Commerce value 20; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:25103](../Assets/Resources/Content/Blueprints/Objects.json#L25103); [FriendlyNPCs.json:1122](../Assets/Resources/Content/Conversations/FriendlyNPCs.json#L1122); [Villagers.json:539](../Assets/Resources/Content/Conversations/Villagers.json#L539); [SettlementManager.cs:198](../Assets/Scripts/Gameplay/Settlements/SettlementManager.cs#L198).

### lantern oil recipe — `LanternOilRecipe`

**Where it comes from:** Given by relevant well keeper/farmer/warden conversation; also village Elder guidance dialogue.

**What it actually does:** Carry guide and consume one WardOil in settlement lantern repair conversation to make stable repair. Guide remains.

**Limits and gaps:** No Read/Study action or standalone recipe teaching. Its useful effect is inventory possession gating a specific settlement repair. Already stable/improved site refuses.

**Base affordances:** Base Commerce value 20; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:26808](../Assets/Resources/Content/Blueprints/Objects.json#L26808); [Wardens.json:54](../Assets/Resources/Content/Conversations/Wardens.json#L54); [Villagers.json:567](../Assets/Resources/Content/Conversations/Villagers.json#L567); [SettlementManager.cs:256](../Assets/Scripts/Gameplay/Settlements/SettlementManager.cs#L256).

## Tinkering schematics

### schematic: honed edge — `SchematicHonedEdge`

**Where it comes from:** WorkshopCacheT2 loot and ArcanistStock.

**What it actually does:** Current ordinary character: trade commodity. Study is advertised, but refuses without BitLockerPart (normal bootstrap does not attach it).

**Limits and gaps:** DEV/legacy character with BitLockerPart only: Study teaches mod_sharp_melee for later tinkering: Sharp: +1 melee weapon penetration; costs bits BC. Not consumed. Needs BitLockerPart; already-known/unknown recipe refuses. Learning does not grant bits, target item or an instant modification; compatible target and mod rules still apply.

**Base affordances:** Base Commerce value 25; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:26592](../Assets/Resources/Content/Blueprints/Objects.json#L26592); [LootTables.json:494](../Assets/Resources/Content/Data/Loot/LootTables.json#L494); [SchematicPart.cs:55](../Assets/Scripts/Gameplay/Tinkering/SchematicPart.cs#L55); [Recipes_V1.json:20](../Assets/Resources/Content/Data/Tinkering/Recipes_V1.json#L20); [SharpTinkerModification.cs:8](../Assets/Scripts/Gameplay/Tinkering/Mods/SharpTinkerModification.cs#L8); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### schematic: reinforced plating — `SchematicReinforcedPlating`

**Where it comes from:** WorkshopCacheT2 loot and ArcanistStock.

**What it actually does:** Current ordinary character: trade commodity. Study is advertised, but refuses without BitLockerPart (normal bootstrap does not attach it).

**Limits and gaps:** DEV/legacy character with BitLockerPart only: Study teaches mod_reinforced_plating_armor for later tinkering: Reinforced Plating: +1 armor AV, -1 DV; costs BC. Not consumed. Needs BitLockerPart; already-known/unknown recipe refuses. Learning does not grant bits, target item or an instant modification; compatible target and mod rules still apply.

**Base affordances:** Base Commerce value 25; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:26664](../Assets/Resources/Content/Blueprints/Objects.json#L26664); [LootTables.json:498](../Assets/Resources/Content/Data/Loot/LootTables.json#L498); [SchematicPart.cs:55](../Assets/Scripts/Gameplay/Tinkering/SchematicPart.cs#L55); [Recipes_V1.json:30](../Assets/Resources/Content/Data/Tinkering/Recipes_V1.json#L30); [ReinforcedPlatingTinkerModification.cs:6](../Assets/Scripts/Gameplay/Tinkering/Mods/ReinforcedPlatingTinkerModification.cs#L6); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### schematic: duelist's cut — `SchematicDuelistCut`

**Where it comes from:** WorkshopCacheT2 loot and ArcanistStock.

**What it actually does:** Current ordinary character: trade commodity. Study is advertised, but refuses without BitLockerPart (normal bootstrap does not attach it).

**Limits and gaps:** DEV/legacy character with BitLockerPart only: Study teaches mod_duelist_cut_armor for later tinkering: Duelist Cut: -1 AV, +2 Agility while equipped; costs CG. Not consumed. Needs BitLockerPart; already-known/unknown recipe refuses. Learning does not grant bits, target item or an instant modification; compatible target and mod rules still apply.

**Base affordances:** Base Commerce value 35; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:26736](../Assets/Resources/Content/Blueprints/Objects.json#L26736); [LootTables.json:502](../Assets/Resources/Content/Data/Loot/LootTables.json#L502); [SchematicPart.cs:55](../Assets/Scripts/Gameplay/Tinkering/SchematicPart.cs#L55); [Recipes_V1.json:60](../Assets/Resources/Content/Data/Tinkering/Recipes_V1.json#L60); [DuelistCutTinkerModification.cs:6](../Assets/Scripts/Gameplay/Tinkering/Mods/DuelistCutTinkerModification.cs#L6); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

## Readable codex volumes

### copy of From the Reading: the Felling, collated — `Codex01`

**Where it comes from:** Current data table route: LibraryShelfT1.

**What it actually does:** Read displays the full shipped canonical document in an announcement with UI pagination. Informational/story value; also Commerce value 8.

**Limits and gaps:** Repeatable; does not consume item, turn, health, RNG, quests or knowledge flags. Must be carried/equipped or within one cell in the same zone; no mechanical skill unlock.

**Base affordances:** Base Commerce value 8; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:37527](../Assets/Resources/Content/Blueprints/Objects.json#L37527); [LootTables.json:279](../Assets/Resources/Content/Data/Loot/LootTables.json#L279); [ReadableDocumentPart.cs:30](../Assets/Scripts/Gameplay/Items/ReadableDocumentPart.cs#L30); [CodexDocuments.json:4](../Assets/Resources/Content/Data/Documents/CodexDocuments.json#L4); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### copy of The telling of the man who said no — `Codex02`

**Where it comes from:** Current data table route: DrifterStock.

**What it actually does:** Read displays the full shipped canonical document in an announcement with UI pagination. Informational/story value; also Commerce value 8.

**Limits and gaps:** Repeatable; does not consume item, turn, health, RNG, quests or knowledge flags. Must be carried/equipped or within one cell in the same zone; no mechanical skill unlock.

**Base affordances:** Base Commerce value 8; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:37604](../Assets/Resources/Content/Blueprints/Objects.json#L37604); [LootTables.json:3010](../Assets/Resources/Content/Data/Loot/LootTables.json#L3010); [ReadableDocumentPart.cs:30](../Assets/Scripts/Gameplay/Items/ReadableDocumentPart.cs#L30); [CodexDocuments.json:11](../Assets/Resources/Content/Data/Documents/CodexDocuments.json#L11); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### copy of Internal memorandum: the founding provisions — `Codex03`

**Where it comes from:** Current data table route: BookshelfT2.

**What it actually does:** Read displays the full shipped canonical document in an announcement with UI pagination. Informational/story value; also Commerce value 8.

**Limits and gaps:** Repeatable; does not consume item, turn, health, RNG, quests or knowledge flags. Must be carried/equipped or within one cell in the same zone; no mechanical skill unlock.

**Base affordances:** Base Commerce value 8; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:37681](../Assets/Resources/Content/Blueprints/Objects.json#L37681); [LootTables.json:2314](../Assets/Resources/Content/Data/Loot/LootTables.json#L2314); [ReadableDocumentPart.cs:30](../Assets/Scripts/Gameplay/Items/ReadableDocumentPart.cs#L30); [CodexDocuments.json:18](../Assets/Resources/Content/Data/Documents/CodexDocuments.json#L18); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### copy of The oath of the cloth — `Codex04`

**Where it comes from:** Current data table route: ScribeStock.

**What it actually does:** Read displays the full shipped canonical document in an announcement with UI pagination. Informational/story value; also Commerce value 8.

**Limits and gaps:** Repeatable; does not consume item, turn, health, RNG, quests or knowledge flags. Must be carried/equipped or within one cell in the same zone; no mechanical skill unlock.

**Base affordances:** Base Commerce value 8; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:37758](../Assets/Resources/Content/Blueprints/Objects.json#L37758); [LootTables.json:2700](../Assets/Resources/Content/Data/Loot/LootTables.json#L2700); [ReadableDocumentPart.cs:30](../Assets/Scripts/Gameplay/Items/ReadableDocumentPart.cs#L30); [CodexDocuments.json:25](../Assets/Resources/Content/Data/Documents/CodexDocuments.json#L25); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### copy of Readings from a plaque-wall — `Codex05`

**Where it comes from:** Current data table route: UndertakerStock.

**What it actually does:** Read displays the full shipped canonical document in an announcement with UI pagination. Informational/story value; also Commerce value 8.

**Limits and gaps:** Repeatable; does not consume item, turn, health, RNG, quests or knowledge flags. Must be carried/equipped or within one cell in the same zone; no mechanical skill unlock.

**Base affordances:** Base Commerce value 8; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:37835](../Assets/Resources/Content/Blueprints/Objects.json#L37835); [LootTables.json:2672](../Assets/Resources/Content/Data/Loot/LootTables.json#L2672); [ReadableDocumentPart.cs:30](../Assets/Scripts/Gameplay/Items/ReadableDocumentPart.cs#L30); [CodexDocuments.json:32](../Assets/Resources/Content/Data/Documents/CodexDocuments.json#L32); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### copy of Standard contract of carriage (short form) — `Codex06`

**Where it comes from:** Current data table route: MerchantStock.

**What it actually does:** Read displays the full shipped canonical document in an announcement with UI pagination. Informational/story value; also Commerce value 8.

**Limits and gaps:** Repeatable; does not consume item, turn, health, RNG, quests or knowledge flags. Must be carried/equipped or within one cell in the same zone; no mechanical skill unlock.

**Base affordances:** Base Commerce value 8; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:37912](../Assets/Resources/Content/Blueprints/Objects.json#L37912); [LootTables.json:2818](../Assets/Resources/Content/Data/Loot/LootTables.json#L2818); [ReadableDocumentPart.cs:30](../Assets/Scripts/Gameplay/Items/ReadableDocumentPart.cs#L30); [CodexDocuments.json:39](../Assets/Resources/Content/Data/Documents/CodexDocuments.json#L39); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### copy of File: OTHREN. Selected entries. — `Codex07`

**Where it comes from:** Current data table route: CuratorStock.

**What it actually does:** Read displays the full shipped canonical document in an announcement with UI pagination. Informational/story value; also Commerce value 8.

**Limits and gaps:** Repeatable; does not consume item, turn, health, RNG, quests or knowledge flags. Must be carried/equipped or within one cell in the same zone; no mechanical skill unlock.

**Base affordances:** Base Commerce value 8; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:37989](../Assets/Resources/Content/Blueprints/Objects.json#L37989); [LootTables.json:2984](../Assets/Resources/Content/Data/Loot/LootTables.json#L2984); [ReadableDocumentPart.cs:30](../Assets/Scripts/Gameplay/Items/ReadableDocumentPart.cs#L30); [CodexDocuments.json:46](../Assets/Resources/Content/Data/Documents/CodexDocuments.json#L46); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### copy of HAVE YOU THOUGHT ABOUT BEING KEPT? — `Codex08`

**Where it comes from:** Current data table route: BookshelfT1.

**What it actually does:** Read displays the full shipped canonical document in an announcement with UI pagination. Informational/story value; also Commerce value 8.

**Limits and gaps:** Repeatable; does not consume item, turn, health, RNG, quests or knowledge flags. Must be carried/equipped or within one cell in the same zone; no mechanical skill unlock.

**Base affordances:** Base Commerce value 8; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:38066](../Assets/Resources/Content/Blueprints/Objects.json#L38066); [LootTables.json:2275](../Assets/Resources/Content/Data/Loot/LootTables.json#L2275); [ReadableDocumentPart.cs:30](../Assets/Scripts/Gameplay/Items/ReadableDocumentPart.cs#L30); [CodexDocuments.json:53](../Assets/Resources/Content/Data/Documents/CodexDocuments.json#L53); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### copy of The dimming song — `Codex09`

**Where it comes from:** Current data table route: TombVaultT2.

**What it actually does:** Read displays the full shipped canonical document in an announcement with UI pagination. Informational/story value; also Commerce value 8.

**Limits and gaps:** Repeatable; does not consume item, turn, health, RNG, quests or knowledge flags. Must be carried/equipped or within one cell in the same zone; no mechanical skill unlock.

**Base affordances:** Base Commerce value 8; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:38143](../Assets/Resources/Content/Blueprints/Objects.json#L38143); [LootTables.json:380](../Assets/Resources/Content/Data/Loot/LootTables.json#L380); [ReadableDocumentPart.cs:30](../Assets/Scripts/Gameplay/Items/ReadableDocumentPart.cs#L30); [CodexDocuments.json:60](../Assets/Resources/Content/Data/Documents/CodexDocuments.json#L60); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### copy of Transcript of a reading: the scribe Ollun, folio three — `Codex10`

**Where it comes from:** Current data table route: BookshelfT2.

**What it actually does:** Read displays the full shipped canonical document in an announcement with UI pagination. Informational/story value; also Commerce value 8.

**Limits and gaps:** Repeatable; does not consume item, turn, health, RNG, quests or knowledge flags. Must be carried/equipped or within one cell in the same zone; no mechanical skill unlock.

**Base affordances:** Base Commerce value 8; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:38220](../Assets/Resources/Content/Blueprints/Objects.json#L38220); [LootTables.json:2315](../Assets/Resources/Content/Data/Loot/LootTables.json#L2315); [ReadableDocumentPart.cs:30](../Assets/Scripts/Gameplay/Items/ReadableDocumentPart.cs#L30); [CodexDocuments.json:67](../Assets/Resources/Content/Data/Documents/CodexDocuments.json#L67); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### copy of The sign at the grove-edge — `Codex11`

**Where it comes from:** Current data table route: ChoirStock.

**What it actually does:** Read displays the full shipped canonical document in an announcement with UI pagination. Informational/story value; also Commerce value 8.

**Limits and gaps:** Repeatable; does not consume item, turn, health, RNG, quests or knowledge flags. Must be carried/equipped or within one cell in the same zone; no mechanical skill unlock.

**Base affordances:** Base Commerce value 8; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:38297](../Assets/Resources/Content/Blueprints/Objects.json#L38297); [LootTables.json:3032](../Assets/Resources/Content/Data/Loot/LootTables.json#L3032); [ReadableDocumentPart.cs:30](../Assets/Scripts/Gameplay/Items/ReadableDocumentPart.cs#L30); [CodexDocuments.json:74](../Assets/Resources/Content/Data/Documents/CodexDocuments.json#L74); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### copy of How they tell it in Sill — `Codex12`

**Where it comes from:** Current data table route: ElderStock.

**What it actually does:** Read displays the full shipped canonical document in an announcement with UI pagination. Informational/story value; also Commerce value 8.

**Limits and gaps:** Repeatable; does not consume item, turn, health, RNG, quests or knowledge flags. Must be carried/equipped or within one cell in the same zone; no mechanical skill unlock.

**Base affordances:** Base Commerce value 8; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:38374](../Assets/Resources/Content/Blueprints/Objects.json#L38374); [LootTables.json:2774](../Assets/Resources/Content/Data/Loot/LootTables.json#L2774); [ReadableDocumentPart.cs:30](../Assets/Scripts/Gameplay/Items/ReadableDocumentPart.cs#L30); [CodexDocuments.json:81](../Assets/Resources/Content/Data/Documents/CodexDocuments.json#L81); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### copy of Condition report: the First Account — `Codex13`

**Where it comes from:** Current data table route: SealedVaultT3.

**What it actually does:** Read displays the full shipped canonical document in an announcement with UI pagination. Informational/story value; also Commerce value 8.

**Limits and gaps:** Repeatable; does not consume item, turn, health, RNG, quests or knowledge flags. Must be carried/equipped or within one cell in the same zone; no mechanical skill unlock.

**Base affordances:** Base Commerce value 8; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:38451](../Assets/Resources/Content/Blueprints/Objects.json#L38451); [LootTables.json:487](../Assets/Resources/Content/Data/Loot/LootTables.json#L487); [ReadableDocumentPart.cs:30](../Assets/Scripts/Gameplay/Items/ReadableDocumentPart.cs#L30); [CodexDocuments.json:88](../Assets/Resources/Content/Data/Documents/CodexDocuments.json#L88); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

## Light, ink and currency

### torch — `Torch`

**Where it comes from:** Morrowfast mender sells finite/replenished shop stock; cave/camp/deep supplies, provisioner/warden/merchant tables; authored dungeon supplies and Reference Glade.

**What it actually does:** Hand equipment or loose ground light (radius 6, intensity .6). Light/extinguish menu; lit equipped torch counts as an ignition source for other controlled fires. One LampOil refuels an unlit carried singleton by 25 fuel, capped at maximum.

**Limits and gaps:** Only singleton equipped torch or adjacent loose torch can light/extinguish; pack stack cannot. Lighting needs nearby fire, usable fuel, not frozen and moisture <=.35. Fuel 50 at .3 burn rate; stowed torch preserves fuel/state but supplies no light. No unique melee weapon part.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:23302](../Assets/Resources/Content/Blueprints/Objects.json#L23302); [LootTables.json:7](../Assets/Resources/Content/Data/Loot/LootTables.json#L7); [MorrowfastContent.cs:79](../Assets/Scripts/Gameplay/World/MorrowfastContent.cs#L79); [TorchLightPart.cs:44](../Assets/Scripts/Gameplay/Items/TorchLightPart.cs#L44); [TorchLightPart.cs:176](../Assets/Scripts/Gameplay/Items/TorchLightPart.cs#L176); [IgnitionSources.cs:12](../Assets/Scripts/Gameplay/Items/IgnitionSources.cs#L12); [TorchRefuelPart.cs:13](../Assets/Scripts/Gameplay/Preparation/TorchRefuelPart.cs#L13); [InventoryPart.cs:615](../Assets/Scripts/Gameplay/Inventory/InventoryPart.cs#L615); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### vial of ink — `InkVial`

**Where it comes from:** Quillhold Scribe, trade/loot tables: CampGoodsT1, ArcanistStock, BookshelfT1, BookshelfT2, BookshelfT3, AlchemyShelfT1, ReliquaryT1, ScribeStock, CuratorStock, ChoirStock, EchoStock. BotanicalInkDesk can produce a vial from carried processing inputs.

**What it actually does:** Consume Use for +25 rental Ink; alternatively consume vial to re-ink a carried rite book +5 charges (cap 10), or pay the Quillhold copying service.

**Limits and gaps:** Rental Ink wallet and book charges are separate. Use decants into wallet, not automatically into a book; re-inking selects book. Vial consumed by each use.

**Base affordances:** Base Commerce value 10; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:2416](../Assets/Resources/Content/Blueprints/Objects.json#L2416); [LootTables.json:300](../Assets/Resources/Content/Data/Loot/LootTables.json#L300); [InkVialPart.cs:39](../Assets/Scripts/Gameplay/Items/InkVialPart.cs#L39); [GrimoireInkService.cs:27](../Assets/Scripts/Gameplay/Magic/GrimoireInkService.cs#L27); [SecondExplorationServices.cs:36](../Assets/Scripts/Gameplay/Exploration/SecondExplorationServices.cs#L36); [BotanicalInkDeskPart.cs:82](../Assets/Scripts/Gameplay/World/BotanicalInkDeskPart.cs#L82); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### gold coin — `GoldCoin`

**Where it comes from:** Broad cache/death/container loot tables, plus procedural treasure budgets.

**What it actually does:** Ordinary ground pickup removes each coin and credits 5 drams directly to the purse. Shiny tag is a target for Magpie AI hoarding if that actor is present.

**Limits and gaps:** Container retrieval is ordinary item transfer (no GoldCoin conversion there); carried coin has Commerce value 1 until dropped/re-picked. Not a separate gold spending system. Magpie is in current population tables; spawning/hoarding is probabilistic, not an item-use command.

**Base affordances:** Base Commerce value 1; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:2360](../Assets/Resources/Content/Blueprints/Objects.json#L2360); [LootTables.json:172](../Assets/Resources/Content/Data/Loot/LootTables.json#L172); [PickupCommand.cs:139](../Assets/Scripts/Gameplay/Inventory/Commands/Acquisition/PickupCommand.cs#L139); [TakeFromContainerCommand.cs:3](../Assets/Scripts/Gameplay/Inventory/Commands/Acquisition/TakeFromContainerCommand.cs#L3); [AIHoarderPart.cs:35](../Assets/Scripts/Gameplay/AI/AIHoarderPart.cs#L35); [PopulationTable.cs:385](../Assets/Scripts/Data/Tables/PopulationTable.cs#L385); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### beetle-jar — `BeetleJar`

**Where it comes from:** Runtime portable variant: unhook first authored descent lamp in Overworld.4.6.1; requires SecondExploration enabled/version >=15.

**What it actually does:** Same BeetleJar becomes Takeable, weight 2, Hand-equippable lamp; can carry portable light away from seat.

**Limits and gaps:** Ordinary BeetleJar blueprint is scenery and cannot be picked up. Only configured RecoverableLamp owner supports this one-time conversion; no fuel consumption configured.

**Source evidence:** [Objects.json:34326](../Assets/Resources/Content/Blueprints/Objects.json#L34326); [SecondExplorationSites.cs:91](../Assets/Scripts/Gameplay/Exploration/SecondExplorationSites.cs#L91); [SecondExplorationObjects.cs:7](../Assets/Scripts/Gameplay/Exploration/SecondExplorationObjects.cs#L7).

## Corpses, bones and trophies

### bone — `Bone`

**Where it comes from:** TombVaultT2/BoneCacheT1-3/UndertakerStock; harvest SkeletalSentry or CharredHusk corpse (1-2).

**What it actually does:** Commodity (value 1) and ordinary throwable. Allied PetDog can fetch a landed thrown item; this is generic item behavior, not a bone-specific command. PetDog is placed at Reference Glade (33,23), so this actor has a current authored source.

**Limits and gaps:** No bone-specific crafting, feeding, taming or equipment mechanic located. PetDog fetch requires allied thrower and notice radius (10).

**Base affordances:** Base Commerce value 1; throwable, subject to handling/action gates. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:2292](../Assets/Resources/Content/Blueprints/Objects.json#L2292); [CorpsePart.cs:241](../Assets/Scripts/Gameplay/Entities/CorpsePart.cs#L241); [LootTables.json:368](../Assets/Resources/Content/Data/Loot/LootTables.json#L368); [AIRetrieverPart.cs:44](../Assets/Scripts/Gameplay/AI/AIRetrieverPart.cs#L44); [ReferenceGladePlan.cs:56](../Assets/Scripts/Gameplay/World/Generation/ReferenceGladePlan.cs#L56); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### corpse — `CreatureCorpse`

**Where it comes from:** Death drop from many creature/NPC blueprints; generic default usually 100%, SootGremlin/DirtGnome 70%.

**What it actually does:** Corpse commodity (value 2); carry/drop, bury at configured Graveyard; undertaker AI may collect bodies. When the dead species declares a harvest, spawned corpse gains one-shot Harvest: CaveSlime -> BogSap 1-1 (100%); CaveBear -> RawMeat 2-3 (100%); Glowmaw -> EmberFruit 1-1 (100%); Scorpion -> VenomGland 1-1 (75%); SandWurm -> RawMeat 2-3 (100%); GiantSpider -> VenomGland 1-1 (75%); Viper -> VenomGland 1-1 (75%); JungleApe -> RawMeat 2-3 (100%); SkeletalSentry -> Bone 1-2 (100%); StoneGolem -> GlowQuartz 1-1 (35%); ObsidianBrute -> GlowQuartz 1-2 (35%); Mosshulk -> MendleafSprig 2-2 (100%); VaultSentinel -> GlowQuartz 1-1 (35%); SporeShambler -> ShamblerSporeSac 1-1 (100%); CharredHusk -> Bone 1-2 (100%); SunStriker -> RawMeat 1-2 (100%); Reedfrog -> FrogOil 1-2 (80%); MawToad -> FrogOil 2-4 (90%); SariSnake -> VenomGland 1-1 (100%); Shambler -> ShamblerSporeSac 1-1 (80%); SpreadLatchcoil -> VenomGland 1-1 (75%).

**Limits and gaps:** Corpse source drops may be suppressed. Burial transfers the same body into a graveyard; no payment/reputation reward. Corpse blueprint alone has no harvest; runtime source determines it. Harvest allowed carried or adjacent, costs one action, spent even on failed yield; no universal butcher-all mechanic.

**Base affordances:** Base Commerce value 2; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:2989](../Assets/Resources/Content/Blueprints/Objects.json#L2989); [CorpsePart.cs:134](../Assets/Scripts/Gameplay/Entities/CorpsePart.cs#L134); [CorpsePart.cs:241](../Assets/Scripts/Gameplay/Entities/CorpsePart.cs#L241); [HarvestablePart.cs:43](../Assets/Scripts/Gameplay/Items/HarvestablePart.cs#L43); [SecondExplorationServices.cs:226](../Assets/Scripts/Gameplay/Exploration/SecondExplorationServices.cs#L226).

### marlback remains — `MarlbackCorpse`

**Where it comes from:** Death drop from MarlbackScrabbler, MarlbackGleaner, MarlbackPatchbearer, MarlbackTunnelguard, MarlbackWallkeeper, MarlbackBreacher, SpreadHurdleCutter, SpreadDitchMate, MarlbackCindercaller, MarlbackSoursprayer.

**What it actually does:** Corpse commodity (value 3); carry/drop, bury at configured Graveyard; undertaker AI may collect bodies.

**Limits and gaps:** Corpse source drops may be suppressed. Burial transfers the same body into a graveyard; no payment/reputation reward. No HarvestBlueprint on these source species, so these remains have no standard butcher yield.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:3061](../Assets/Resources/Content/Blueprints/Objects.json#L3061); [CorpsePart.cs:134](../Assets/Scripts/Gameplay/Entities/CorpsePart.cs#L134); [CorpsePart.cs:241](../Assets/Scripts/Gameplay/Entities/CorpsePart.cs#L241); [HarvestablePart.cs:43](../Assets/Scripts/Gameplay/Items/HarvestablePart.cs#L43); [SecondExplorationServices.cs:226](../Assets/Scripts/Gameplay/Exploration/SecondExplorationServices.cs#L226).

### stormbinder remains — `MarlbackStormbinderCorpse`

**Where it comes from:** Death drop from MarlbackStormbinder.

**What it actually does:** Corpse commodity (value 3); carry/drop, bury at configured Graveyard; undertaker AI may collect bodies.

**Limits and gaps:** Corpse source drops may be suppressed. Burial transfers the same body into a graveyard; no payment/reputation reward. No HarvestBlueprint on these source species, so these remains have no standard butcher yield.

**Base affordances:** Base Commerce value 3; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:638](../Assets/Resources/Content/Blueprints/Objects.json#L638); [CorpsePart.cs:134](../Assets/Scripts/Gameplay/Entities/CorpsePart.cs#L134); [CorpsePart.cs:241](../Assets/Scripts/Gameplay/Entities/CorpsePart.cs#L241); [HarvestablePart.cs:43](../Assets/Scripts/Gameplay/Items/HarvestablePart.cs#L43); [SecondExplorationServices.cs:226](../Assets/Scripts/Gameplay/Exploration/SecondExplorationServices.cs#L226).

### reedback grazer remains — `ReedbackGrazerCorpse`

**Where it comes from:** Death drop from ReedbackGrazer.

**What it actually does:** Corpse commodity (value 2); carry/drop, bury at configured Graveyard; undertaker AI may collect bodies.  The configured Spread predator can eat its exact grazer prey corpse; this is AI-side feeding, not a player Eat action.

**Limits and gaps:** Corpse source drops may be suppressed. Burial transfers the same body into a graveyard; no payment/reputation reward. No HarvestBlueprint on these source species, so these remains have no standard butcher yield.

**Base affordances:** Base Commerce value 2; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:39862](../Assets/Resources/Content/Blueprints/Objects.json#L39862); [CorpsePart.cs:134](../Assets/Scripts/Gameplay/Entities/CorpsePart.cs#L134); [CorpsePart.cs:241](../Assets/Scripts/Gameplay/Entities/CorpsePart.cs#L241); [HarvestablePart.cs:43](../Assets/Scripts/Gameplay/Items/HarvestablePart.cs#L43); [SecondExplorationServices.cs:226](../Assets/Scripts/Gameplay/Exploration/SecondExplorationServices.cs#L226); [SpreadPredatorPart.cs:214](../Assets/Scripts/Gameplay/AI/SpreadPredatorPart.cs#L214).

### furrowstalker remains — `FurrowstalkerCorpse`

**Where it comes from:** Death drop from Furrowstalker.

**What it actually does:** Corpse commodity (value 2); carry/drop, bury at configured Graveyard; undertaker AI may collect bodies.

**Limits and gaps:** Corpse source drops may be suppressed. Burial transfers the same body into a graveyard; no payment/reputation reward. No HarvestBlueprint on these source species, so these remains have no standard butcher yield.

**Base affordances:** Base Commerce value 2; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:40031](../Assets/Resources/Content/Blueprints/Objects.json#L40031); [CorpsePart.cs:134](../Assets/Scripts/Gameplay/Entities/CorpsePart.cs#L134); [CorpsePart.cs:241](../Assets/Scripts/Gameplay/Entities/CorpsePart.cs#L241); [HarvestablePart.cs:43](../Assets/Scripts/Gameplay/Items/HarvestablePart.cs#L43); [SecondExplorationServices.cs:226](../Assets/Scripts/Gameplay/Exploration/SecondExplorationServices.cs#L226).

### tatterjay remains — `TatterjayCorpse`

**Where it comes from:** Death drop from Tatterjay.

**What it actually does:** Corpse commodity (value 2); carry/drop, bury at configured Graveyard; undertaker AI may collect bodies.

**Limits and gaps:** Corpse source drops may be suppressed. Burial transfers the same body into a graveyard; no payment/reputation reward. No HarvestBlueprint on these source species, so these remains have no standard butcher yield.

**Base affordances:** Base Commerce value 2; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:40270](../Assets/Resources/Content/Blueprints/Objects.json#L40270); [CorpsePart.cs:134](../Assets/Scripts/Gameplay/Entities/CorpsePart.cs#L134); [CorpsePart.cs:241](../Assets/Scripts/Gameplay/Entities/CorpsePart.cs#L241); [HarvestablePart.cs:43](../Assets/Scripts/Gameplay/Items/HarvestablePart.cs#L43); [SecondExplorationServices.cs:226](../Assets/Scripts/Gameplay/Exploration/SecondExplorationServices.cs#L226).

### severed limb (part-specific name) — `SeveredLimb`

**Where it comes from:** Runtime item produced by Body dismemberment, no JSON blueprint.

**What it actually does:** Physical trophy/body-part metadata; pickup/carry/drop, generic interactions.

**Limits and gaps:** No reattachment using this item. Body.RegenerateLimb restores its saved BodyPart record independently and never consumes SeveredLimb. No Commerce, consumable or equipment part.

**Source evidence:** [SeveredLimbFactory.cs:20](../Assets/Scripts/Gameplay/Anatomy/SeveredLimbFactory.cs#L20); [Body.cs:293](../Assets/Scripts/Gameplay/Anatomy/Body.cs#L293); [Body.cs:346](../Assets/Scripts/Gameplay/Anatomy/Body.cs#L346); [SeveredLimbPart.cs:7](../Assets/Scripts/Gameplay/Anatomy/SeveredLimbPart.cs#L7).

## Keys, documents, parcels and story goods

### iron key — `IronKey`

**Where it comes from:** Authored landmark key placements; Spread wayhouse sack has a runtime instance-keyed IronKey. TinkerStock/MerchantStock contain keys but NoTrade prevents buying those stock entries.

**What it actually does:** Carried matching KeyId unlocks matching doors/containers; repeatable reusable key.

**Limits and gaps:** Base key matches iron only. Spread wayhouse key overrides KeyId to its generated specific door, so same blueprint does not guarantee same lock. NoTrade. No generic lockpick action.

**Base affordances:** Trade refused by NoTrade tag; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:24323](../Assets/Resources/Content/Blueprints/Objects.json#L24323); [LootTables.json:2723](../Assets/Resources/Content/Data/Loot/LootTables.json#L2723); [LandmarkBuilder.cs:220](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L220); [SpreadWayhouseBuilder.cs:78](../Assets/Scripts/Gameplay/World/Generation/Builders/SpreadWayhouseBuilder.cs#L78); [LockPart.cs:172](../Assets/Scripts/Gameplay/Items/LockPart.cs#L172); [TradeSystem.cs:369](../Assets/Scripts/Gameplay/Economy/TradeSystem.cs#L369); [LandmarkBuilder.cs:875](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L875); [TraderPart.cs:87](../Assets/Scripts/Gameplay/Economy/TraderPart.cs#L87).

### counter store key — `CounterStoreKey`

**Where it comes from:** Abandoned Counter A, placed near bones by SecondExplorationSites.CounterStore (Exploration >=15 fresh generation).

**What it actually does:** Reusable SecondCounterStore key opens the authored store chest.

**Limits and gaps:** NoTrade; only matching key ID. Optional version-gated scene.

**Base affordances:** Trade refused by NoTrade tag; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:393](../Assets/Resources/Content/Blueprints/Objects.json#L393); [SecondExplorationSites.Regional.cs:76](../Assets/Scripts/Gameplay/Exploration/SecondExplorationSites.Regional.cs#L76); [LockPart.cs:172](../Assets/Scripts/Gameplay/Items/LockPart.cs#L172).

### Stillleaf keeper's key — `StillleafKey`

**Where it comes from:** Salt-Vault cabinet beside Halm/Stillleaf Indexer; cabinet release requires dialogue terms after finding archive words; forced cabinet break is alternate route.

**What it actually does:** Opens sealed Stillleaf library; also required to reseal library with register left inside, concluding that custody choice.

**Limits and gaps:** Reusable, NoTrade; one exact archive lock key ID. Reseal requires outside adjacency, empty doorway, undecided custody, register genuinely inside rather than carried.

**Base affordances:** Trade refused by NoTrade tag; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:24387](../Assets/Resources/Content/Blueprints/Objects.json#L24387); [StillleafSaltVault.cs:80](../Assets/Scripts/Gameplay/World/StillleafSaltVault.cs#L80); [StillleafSaltVault.cs:166](../Assets/Scripts/Gameplay/World/StillleafSaltVault.cs#L166); [StillleafCustody.cs:199](../Assets/Scripts/Gameplay/World/StillleafCustody.cs#L199).

### the Stillleaf register — `StillleafRegister`

**Where it comes from:** Deterministically placed at deepest free sealed-library floor by StillleafArchive after accepted library generation.

**What it actually does:** Quest/custody object: deliver to Hollin at Quillhold (+10 Palimpsest standing, Curation penalty if prior promise), file with Halm (+10 Curation), or leave/reseal in archive. Destruction can be reported as loss to close chain.

**Limits and gaps:** Not a ReadableDocument: story text is Examine. Only exact authored register identity is accepted. One custody outcome. Delivery/file physically transfers same entity; not replaceable by another copy.

**Base affordances:** Trade refused by NoTrade tag; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:24411](../Assets/Resources/Content/Blueprints/Objects.json#L24411); [StillleafArchive.cs:57](../Assets/Scripts/Gameplay/World/StillleafArchive.cs#L57); [StillleafCustody.cs:81](../Assets/Scripts/Gameplay/World/StillleafCustody.cs#L81); [StillleafCustody.cs:199](../Assets/Scripts/Gameplay/World/StillleafCustody.cs#L199).

### pruning writ — `PruningWrit`

**Where it comes from:** Concord Factor work dialogue gives one with PruningContract.

**What it actually does:** Post via Choir Tendril conversation: consumes writ, records posting, -9 RotChoir standing. Report to Factor for 20 drams and +10 SaccharineConcord, completes quest.

**Limits and gaps:** NoTrade; active PruningContract required for posting. No standalone Read action; Examine supplies notice wording.

**Base affordances:** Trade refused by NoTrade tag; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:15970](../Assets/Resources/Content/Blueprints/Objects.json#L15970); [FriendlyNPCs.json:1756](../Assets/Resources/Content/Conversations/FriendlyNPCs.json#L1756); [FriendlyNPCs.json:1780](../Assets/Resources/Content/Conversations/FriendlyNPCs.json#L1780); [RotChoir.json:31](../Assets/Resources/Content/Conversations/RotChoir.json#L31).

### sealed bog-taken body — `SealedBogTakenBody`

**Where it comes from:** Curation Sorter at Drowned Ledger gives it on accepting BogBodyCourier.

**What it actually does:** Deliver through Marrowstye Filer Clerk dialogue, consumes body, gives 25 drams, +10 PaleCuration, quest completion and narrative fact.

**Limits and gaps:** 30 weight, NoTrade; not tagged Corpse and not a butcherable/revivable body. Requires active courier quest.

**Base affordances:** Trade refused by NoTrade tag; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:33930](../Assets/Resources/Content/Blueprints/Objects.json#L33930); [FriendlyNPCs.json:1597](../Assets/Resources/Content/Conversations/FriendlyNPCs.json#L1597); [FriendlyNPCs.json:1668](../Assets/Resources/Content/Conversations/FriendlyNPCs.json#L1668).

### woven doll — `WovenDoll`

**Where it comes from:** Authored WovenDoll stamp at Overworld.1.6.0.

**What it actually does:** Environmental story object: inspect wording about a column growing around its arm; pick up and keep as memento.

**Limits and gaps:** No unique interaction, quest, skill, or Commerce part. Any fallback trade/throw is generic, not the narrative promise of a mechanical effect.

**Base affordances:** No authored Commerce value; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:35064](../Assets/Resources/Content/Blueprints/Objects.json#L35064); [OverworldZoneManager.cs:735](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L735); [LandmarkBuilder.cs:1051](../Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs#L1051).

### memory-marble — `MemoryMarble`

**Where it comes from:** Hollin/Stillleaf Searcher offers one in StillleafArchive conversation while ending is unset and its one-time-given flag absent.

**What it actually does:** Carry with one Tepuibone and the other name-holding stone to Root face; Seal consumes all three and enacts Kept ending, writes ending fact, closes act and shows epilogue. Also commodity value 30.

**Limits and gaps:** All three required in top-level consumable inventory; exact Root face adjacency, living Player, story ledger and no prior ending. No current crafting recipe for this mineral. Epilogue world-stasis promises are narrative; this handler updates flags/face/quest, not a universal decay/healing freeze.

**Base affordances:** Base Commerce value 30; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:35613](../Assets/Resources/Content/Blueprints/Objects.json#L35613); [StillleafArchive.json:229](../Assets/Resources/Content/Conversations/StillleafArchive.json#L229); [EndingRoutes.cs:52](../Assets/Scripts/Gameplay/World/EndingRoutes.cs#L52); [EndingRoutes.cs:99](../Assets/Scripts/Gameplay/World/EndingRoutes.cs#L99).

### mute-stone — `MuteStone`

**Where it comes from:** Halm/Stillleaf Indexer offers one in StillleafArchive conversation while ending is unset and its one-time-given flag absent.

**What it actually does:** Carry with one Tepuibone and the other name-holding stone to Root face; Seal consumes all three and enacts Kept ending, writes ending fact, closes act and shows epilogue. Also commodity value 30.

**Limits and gaps:** All three required in top-level consumable inventory; exact Root face adjacency, living Player, story ledger and no prior ending. No current crafting recipe for this mineral. Epilogue world-stasis promises are narrative; this handler updates flags/face/quest, not a universal decay/healing freeze.

**Base affordances:** Base Commerce value 30; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:35633](../Assets/Resources/Content/Blueprints/Objects.json#L35633); [StillleafArchive.json:551](../Assets/Resources/Content/Conversations/StillleafArchive.json#L551); [EndingRoutes.cs:52](../Assets/Scripts/Gameplay/World/EndingRoutes.cs#L52); [EndingRoutes.cs:99](../Assets/Scripts/Gameplay/World/EndingRoutes.cs#L99).

### cutting of the Choir — `ChoirCutting`

**Where it comes from:** Choir Tendril conversation gives one when agreeing to carry it toward Root (GatherCuttingGiven guard).

**What it actually does:** Carry to Root face: Gather consumes cutting, enacts Gathered ending, changes face text/facts, closes act, shows epilogue.

**Limits and gaps:** Top-level consumable inventory, exact Root face adjacency and no prior ending. No planting, grafting, healing or active summon command. Promised world transformation is narrated/flagged, not wholesale NPC/terrain conversion by this handler.

**Base affordances:** Base Commerce value 0; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:35653](../Assets/Resources/Content/Blueprints/Objects.json#L35653); [RotChoir.json:639](../Assets/Resources/Content/Conversations/RotChoir.json#L639); [EndingRoutes.cs:52](../Assets/Scripts/Gameplay/World/EndingRoutes.cs#L52).

### stamped receiving counterfoil — `CurationCounterfoil`

**Where it comes from:** Marrowstye accepted CurationReceiving scene: receiving index awards after the two exact hauled bodies are correctly placed in their numbered bays, both clerks alive and nonhostile.

**What it actually does:** Reusable key (marrowstye-intake-tools) opens tool cabinet containing salt rake, inspection key and two documents.

**Limits and gaps:** Certification is exact physical arrangement; counterfoil is one finite physical owner, no repeat spawn/reward.

**Base affordances:** No authored Commerce value; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:40990](../Assets/Resources/Content/Blueprints/Objects.json#L40990); [CurationReceivingBuilder.cs:55](../Assets/Scripts/Gameplay/World/Generation/Builders/CurationReceivingBuilder.cs#L55); [CurationReceivingBuilder.cs:99](../Assets/Scripts/Gameplay/World/Generation/Builders/CurationReceivingBuilder.cs#L99); [CurationIntakePart.cs:85](../Assets/Scripts/Gameplay/World/CurationIntakePart.cs#L85); [LockPart.cs:172](../Assets/Scripts/Gameplay/Items/LockPart.cs#L172).

### quarantine inspection key — `CurationInspectionKey`

**Where it comes from:** Marrowstye accepted CurationReceiving scene: locked released-tools cabinet, opened with receiving counterfoil.

**What it actually does:** Reusable key (marrowstye-quarantine) unlocks disused quarantine gate, enabling access to annex goods and half-set occupant.

**Limits and gaps:** Unlocking is distinct from opening; opening releases danger. Does not cure or pacify occupant.

**Base affordances:** No authored Commerce value; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:41063](../Assets/Resources/Content/Blueprints/Objects.json#L41063); [CurationReceivingBuilder.cs:55](../Assets/Scripts/Gameplay/World/Generation/Builders/CurationReceivingBuilder.cs#L55); [CurationReceivingBuilder.cs:99](../Assets/Scripts/Gameplay/World/Generation/Builders/CurationReceivingBuilder.cs#L99); [CurationIntakePart.cs:85](../Assets/Scripts/Gameplay/World/CurationIntakePart.cs#L85); [LockPart.cs:172](../Assets/Scripts/Gameplay/Items/LockPart.cs#L172).

### Marrowstye carriage docket — `CurationTransferDocket`

**Where it comes from:** Marrowstye accepted CurationReceiving scene: locked released-tools cabinet, opened with receiving counterfoil.

**What it actually does:** Read full carriage/salt accounting document; narrative/information purpose.

**Limits and gaps:** No quest/skill/stat unlock; nonconsuming read. No Commerce part. Reached after physical intake/cabinet access.

**Base affordances:** No authored Commerce value; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:41846](../Assets/Resources/Content/Blueprints/Objects.json#L41846); [CurationReceivingBuilder.cs:55](../Assets/Scripts/Gameplay/World/Generation/Builders/CurationReceivingBuilder.cs#L55); [CurationReceivingBuilder.cs:99](../Assets/Scripts/Gameplay/World/Generation/Builders/CurationReceivingBuilder.cs#L99); [CurationIntakePart.cs:85](../Assets/Scripts/Gameplay/World/CurationIntakePart.cs#L85); [ReadableDocumentPart.cs:30](../Assets/Scripts/Gameplay/Items/ReadableDocumentPart.cs#L30); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432).

### misplaced-last-words report — `CurationDiscrepancyReport`

**Where it comes from:** Marrowstye accepted CurationReceiving scene: locked released-tools cabinet, opened with receiving counterfoil.

**What it actually does:** Read full misfiled last-words report; narrative/information purpose.

**Limits and gaps:** No quest/skill/stat unlock; nonconsuming read. No Commerce part. Reached after physical intake/cabinet access.

**Base affordances:** No authored Commerce value; no authored Throw action. Runtime variants and restrictions above take precedence.

**Source evidence:** [Objects.json:41919](../Assets/Resources/Content/Blueprints/Objects.json#L41919); [CurationReceivingBuilder.cs:55](../Assets/Scripts/Gameplay/World/Generation/Builders/CurationReceivingBuilder.cs#L55); [CurationReceivingBuilder.cs:99](../Assets/Scripts/Gameplay/World/Generation/Builders/CurationReceivingBuilder.cs#L99); [CurationIntakePart.cs:85](../Assets/Scripts/Gameplay/World/CurationIntakePart.cs#L85); [ReadableDocumentPart.cs:30](../Assets/Scripts/Gameplay/Items/ReadableDocumentPart.cs#L30); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432).

### wrapped supper cloth — `Sack:MorrowfastDryGoods`

**Where it comes from:** Runtime Sack variant: marked wrapped supper cloth inside supply basket in Overworld.2.6.0.

**What it actually does:** Return exact parcel to Farra in Morrowfast; gives 15 drams + FireClay, completes expedition and physically lays cloth on supper table.

**Limits and gaps:** Blueprint ID is Sack; runtime ID morrowfast-dry-goods:parcel. Container and stack parts removed, weight12, destructible. No storage utility; ordinary sack cannot replace. Story optional; loss can be released.

**Source evidence:** [MorrowfastExpedition.cs:32](../Assets/Scripts/Gameplay/World/MorrowfastExpedition.cs#L32); [MorrowfastExpedition.cs:46](../Assets/Scripts/Gameplay/World/MorrowfastExpedition.cs#L46); [MorrowfastExpedition.cs:188](../Assets/Scripts/Gameplay/World/MorrowfastExpedition.cs#L188).

### sealed lamp-oil consignment — `Sack:sumphold-oil`

**Where it comes from:** Runtime Sack recovery cargo: Overworld.16.6.0 -> Sumphold merchant Overworld.15.6.0.

**What it actually does:** Recover/deliver exact consignment to regional recipient: 12 drams plus SilverSand; preservation bonus can apply.

**Limits and gaps:** Blueprint ID Sack with seed-specific RegionalCargo identity, named sealed lamp-oil consignment. Weight6; container/stack removed, destructible. Contents cannot be extracted as oil/sand. Ordinary items cannot substitute the physical recovery cargo.

**Source evidence:** [RegionalSituations.cs:49](../Assets/Scripts/Gameplay/World/RegionalSituations.cs#L49); [RegionalSituations.cs:192](../Assets/Scripts/Gameplay/World/RegionalSituations.cs#L192); [RegionalRequestPart.cs:13](../Assets/Scripts/Gameplay/World/RegionalRequestPart.cs#L13).

### sealed filter-sand consignment — `Sack:wellmeet-filters`

**Where it comes from:** Runtime Sack recovery cargo: Overworld.8.17.0 -> Wellmeet merchant Overworld.8.16.0.

**What it actually does:** Recover/deliver exact consignment to regional recipient: 12 drams plus FireClay; preservation bonus can apply.

**Limits and gaps:** Blueprint ID Sack with seed-specific RegionalCargo identity, named sealed filter-sand consignment. Weight6; container/stack removed, destructible. Contents cannot be extracted as oil/sand. Ordinary items cannot substitute the physical recovery cargo.

**Source evidence:** [RegionalSituations.cs:51](../Assets/Scripts/Gameplay/World/RegionalSituations.cs#L51); [RegionalSituations.cs:192](../Assets/Scripts/Gameplay/World/RegionalSituations.cs#L192); [RegionalRequestPart.cs:13](../Assets/Scripts/Gameplay/World/RegionalRequestPart.cs#L13).

### carved name-token — `CrunchyLocket`

**Where it comes from:** Direct-created runtime item at Tine Overworld.13.7.0, canonical CrunchyLocket village quest.

**What it actually does:** Picking up or possessing completes find_locket objective; report to Belis completes quest, grants 130 XP +55 drams.

**Limits and gaps:** Display name carved name-token. No Read, wearable or special active. Report dialogue does not TakeItem: keeps physical token despite return wording; only stage-gated completion is actual behavior.

**Source evidence:** [VillagePopulationBuilder.cs:989](../Assets/Scripts/Gameplay/World/Generation/Builders/VillagePopulationBuilder.cs#L989); [VillagePopulationBuilder.cs:1036](../Assets/Scripts/Gameplay/World/Generation/Builders/VillagePopulationBuilder.cs#L1036); [Crunchy_Quest.json:37](../Assets/Resources/Content/Conversations/Crunchy_Quest.json#L37).

### witness-book — `DetectiveNotebook`

**Where it comes from:** Direct-created at current starting village Sill (Overworld.10.10.0) by VillagePopulationBuilder.PlaceStartingVillageQuestGiver; Morrowfast is a separate destination.

**What it actually does:** Picking up/possessing witnesses find_notebook objective of Hallun/RootBeerGuyCase; together with gremlin resolution permits report reward of 150 XP +60 drams.

**Limits and gaps:** Display name witness-book; no ReadableDocument or actual notebook text. No standalone informational read. Report dialogue grants rewards without taking the notebook; exact source is Sill, not Morrowfast.

**Source evidence:** [VillagePopulationBuilder.cs:63](../Assets/Scripts/Gameplay/World/Generation/Builders/VillagePopulationBuilder.cs#L63); [VillagePopulationBuilder.cs:920](../Assets/Scripts/Gameplay/World/Generation/Builders/VillagePopulationBuilder.cs#L920); [RootBeerGuyCase.json:26](../Assets/Resources/Content/Data/Storylets/RootBeerGuyCase.json#L26); [OverworldZoneManager.cs:1106](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L1106); [RootBeerGuy_Quest.json:37](../Assets/Resources/Content/Conversations/RootBeerGuy_Quest.json#L37).

## Movable world objects

These are additional world interactions, not eight more portable inventory items. Hauling itself can change a route; it does not prove a prop has a crafting or storage function.

### stout barrel — `HaulBarrel`

**Classification:** current-movable-world-object.

**Where it comes from:** Optional haulable pools in Spread, Sodden and Grovelands; selected Spread hauling encounters.

**What it actually does:** Drag a solid obstruction to alter a local route; configured heavy-salvage layout provides a shorter passage after moving it. Has Wood/Thermal/Destructible, so can also burn or break.

**Limits and gaps:** Weight75; sufficient Strength required. No ContainerPart: its description of being full does not provide accessible contents. Cannot carry in pack or throw. Moving it does not provide timber.

**Source evidence:** [HaulablePropBuilder.cs:48](../Assets/Scripts/Gameplay/World/Generation/Builders/HaulablePropBuilder.cs#L48); [DragRules.cs:131](../Assets/Scripts/Gameplay/World/DragRules.cs#L131); [DragSystem.cs:31](../Assets/Scripts/Gameplay/World/DragSystem.cs#L31); [SpreadExplorationHauling.cs:16](../Assets/Scripts/Gameplay/World/Generation/SpreadExplorationHauling.cs#L16).

### fallen beam — `FallenBeam`

**Classification:** current-movable-world-object.

**Where it comes from:** Optional haulable pools in Spread, Sodden, Grovelands, ruins and caves; selected Spread hauling encounters.

**What it actually does:** Drag solid beam to change local movement; configured hauling layout rewards removing the obstruction with a shorter route.

**Limits and gaps:** Weight60; cannot carry or throw. Plain FallenBeam has no harvest, Material, Thermal or Destructible part; do not confuse it with the separate finite salvage node that yields SalvagedTimber.

**Source evidence:** [HaulablePropBuilder.cs:48](../Assets/Scripts/Gameplay/World/Generation/Builders/HaulablePropBuilder.cs#L48); [DragRules.cs:131](../Assets/Scripts/Gameplay/World/DragRules.cs#L131); [DragSystem.cs:31](../Assets/Scripts/Gameplay/World/DragSystem.cs#L31); [SpreadExplorationHauling.cs:16](../Assets/Scripts/Gameplay/World/Generation/SpreadExplorationHauling.cs#L16).

### mill-stone — `MillStone`

**Classification:** current-movable-world-object.

**Where it comes from:** Optional Spread and Stump haulable pools.

**What it actually does:** Drag its solid physical obstacle when strong enough.

**Limits and gaps:** Weight150 (requires at least Strength19 under weight<=Strength*8). No milling, grain processing, crafting, container or destruction part found.

**Source evidence:** [HaulablePropBuilder.cs:48](../Assets/Scripts/Gameplay/World/Generation/Builders/HaulablePropBuilder.cs#L48); [DragRules.cs:131](../Assets/Scripts/Gameplay/World/DragRules.cs#L131); [DragSystem.cs:31](../Assets/Scripts/Gameplay/World/DragSystem.cs#L31).

### salt-cured body — `SaltCuredBody`

**Classification:** current-movable-world-object.

**Where it comes from:** Optional Beating haulables; two specific authored bodies in Marrowstye receiving hall.

**What it actually does:** Drag physical body. Exact two configured receiving bodies can be placed in their matching bays and certified at the intake index, awarding the counterfoil that opens the tool cabinet.

**Limits and gaps:** Weight90. Ambient bodies cannot substitute for the bound receiving subjects. Not takeable; not an ordinary Corpse-tagged corpse, cannot butcher it or put it through ordinary burial. No universal preservation crafting.

**Source evidence:** [HaulablePropBuilder.cs:48](../Assets/Scripts/Gameplay/World/Generation/Builders/HaulablePropBuilder.cs#L48); [DragRules.cs:131](../Assets/Scripts/Gameplay/World/DragRules.cs#L131); [DragSystem.cs:31](../Assets/Scripts/Gameplay/World/DragSystem.cs#L31); [CurationIntakePart.cs:48](../Assets/Scripts/Gameplay/World/CurationIntakePart.cs#L48); [CurationIntakePart.cs:85](../Assets/Scripts/Gameplay/World/CurationIntakePart.cs#L85); [CurationReceivingBuilder.cs:27](../Assets/Scripts/Gameplay/World/Generation/Builders/CurationReceivingBuilder.cs#L27).

### stone coffer — `StoneCoffer`

**Classification:** current-movable-world-object.

**Where it comes from:** Optional Beating/Stump/ruins/cave haulable pools; Marrowstye hall and profile stamps.

**What it actually does:** Drag its solid obstacle when strong enough.

**Limits and gaps:** Weight110; no ContainerPart, storage, opening, loot or Destructible part. Name and lid do not make it a functional chest.

**Source evidence:** [HaulablePropBuilder.cs:48](../Assets/Scripts/Gameplay/World/Generation/Builders/HaulablePropBuilder.cs#L48); [DragRules.cs:131](../Assets/Scripts/Gameplay/World/DragRules.cs#L131); [DragSystem.cs:31](../Assets/Scripts/Gameplay/World/DragSystem.cs#L31); [MarrowstyeCompositionBuilder.cs:63](../Assets/Scripts/Gameplay/World/Generation/Builders/MarrowstyeCompositionBuilder.cs#L63).

### anvil — `SmithAnvil`

**Classification:** current-movable-world-object.

**Where it comes from:** Optional Overwrit/ruins haulables, Cinderhold worksite and Bloom Front placement.

**What it actually does:** Drag its solid obstacle with Strength18 or greater.

**Limits and gaps:** Weight120; no ForgePart or metalworking action. TinkersForge is a different object and remains necessary for station-gated operations.

**Source evidence:** [HaulablePropBuilder.cs:48](../Assets/Scripts/Gameplay/World/Generation/Builders/HaulablePropBuilder.cs#L48); [DragRules.cs:131](../Assets/Scripts/Gameplay/World/DragRules.cs#L131); [DragSystem.cs:31](../Assets/Scripts/Gameplay/World/DragSystem.cs#L31); [CinderholdCompositionBuilder.cs:89](../Assets/Scripts/Gameplay/World/Generation/Builders/CinderholdCompositionBuilder.cs#L89); [BloomFrontBuilder.cs:144](../Assets/Scripts/Gameplay/World/Generation/Builders/BloomFrontBuilder.cs#L144).

### cutters’ surviving locker — `SoddenWorksLocker`

**Classification:** current-movable-world-object.

**Where it comes from:** Authored Sodden works, Overworld.17.7.0, amended by current exploration v15 installation.

**What it actually does:** Open actual finite container, remove equipment/supplies individually, or drag locker with cargo still inside. Unpacking reduces the effective drag weight.

**Limits and gaps:** 40-pound shell plus recursively owned contents; not packable. Optional installation must succeed; older saved locker graphs do not receive the handling upgrade automatically. Stock is finite.

**Source evidence:** [SecondExplorationSites.cs:108](../Assets/Scripts/Gameplay/Exploration/SecondExplorationSites.cs#L108); [SecondExplorationObjects.cs:66](../Assets/Scripts/Gameplay/Exploration/SecondExplorationObjects.cs#L66).

### Morrowfast supply basket — `WovenBasket:MorrowfastDryGoods`

**Classification:** current-movable-world-object.

**Where it comes from:** Cold-generated supply field Overworld.2.6.0, west of Morrowfast.

**What it actually does:** Open to recover wrapped supper cloth for Farra; drag basket within its field. Real destructible container spills surviving contents when broken.

**Limits and gaps:** 35-pound shell; cannot carry. Dragging stops at chunk boundaries; carry the exact parcel to deliver. Ordinary baskets are not all granted these handling rules.

**Source evidence:** [MorrowfastExpedition.cs:23](../Assets/Scripts/Gameplay/World/MorrowfastExpedition.cs#L23); [MorrowfastExpedition.cs:38](../Assets/Scripts/Gameplay/World/MorrowfastExpedition.cs#L38).

## Collectible liquids

Six registry IDs have source-traced, finite physical pools that pass the current collection gate. Flasks can move them and pour physical pools; walking into a pool applies contact behavior. Quantities, purity and short-lived coatings limit their use. This is not autonomous NPC flask use.

### acid — `acid`

**Classification:** current-collectible-liquid.

**Where it comes from:** AcidPool (60): current Sodden and underground hazard tables.

**What it actually does:** Creature contact applies liquid coat; deals 3 typed Acid damage at each coated creature turn-start until dry, subject to normal damage/resistance handling.

**Limits and gaps:** Physical source must be within one cell, positive-volume, not carried/takeable/creature, and unmixed. Draws conserve finite volume. No flask Drink action; coat amount is exposure, not recoverable units. Coat dries by fluidity+evaporation each owner turn; most benefits/hazards are short-lived. No autonomous NPC flask filling/pouring found.

**Source evidence:** [acid.json:1](../Assets/Resources/Content/Data/LiquidDefinitions/acid.json#L1); [LiquidVesselPart.cs:15](../Assets/Scripts/Gameplay/Items/LiquidVesselPart.cs#L15); [LiquidVesselService.cs:29](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L29); [LiquidVesselService.cs:234](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L234); [LiquidPoolPart.cs:133](../Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs#L133); [LiquidCoveredEffect.cs:204](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L204); [Objects.json:21984](../Assets/Resources/Content/Blueprints/Objects.json#L21984); [HazardTerrainBuilder.cs:104](../Assets/Scripts/Gameplay/World/Generation/Builders/HazardTerrainBuilder.cs#L104); [HazardTerrainBuilder.cs:149](../Assets/Scripts/Gameplay/World/Generation/Builders/HazardTerrainBuilder.cs#L149); [OverworldZoneManager.cs:547](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L547); [LiquidCoveredEffect.cs:267](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L267).

### choir-mirror mucilage — `choir-mirror-mucilage`

**Classification:** current-collectible-liquid.

**Where it comes from:** MirrorMucilagePool (60): current Grovelands hazard table and cave population.

**What it actually does:** Creature coat reflects 50% of post-resistance incoming damage, rounded down, as untyped damage to a nonself attacker; original damage is still taken. Null attacker breaks reflection loops.

**Limits and gaps:** Physical source must be within one cell, positive-volume, not carried/takeable/creature, and unmixed. Draws conserve finite volume. No flask Drink action; coat amount is exposure, not recoverable units. Coat dries by fluidity+evaporation each owner turn; most benefits/hazards are short-lived. No autonomous NPC flask filling/pouring found.

**Source evidence:** [choir-mirror-mucilage.json:1](../Assets/Resources/Content/Data/LiquidDefinitions/choir-mirror-mucilage.json#L1); [LiquidVesselPart.cs:15](../Assets/Scripts/Gameplay/Items/LiquidVesselPart.cs#L15); [LiquidVesselService.cs:29](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L29); [LiquidVesselService.cs:234](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L234); [LiquidPoolPart.cs:133](../Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs#L133); [LiquidCoveredEffect.cs:204](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L204); [Objects.json:29155](../Assets/Resources/Content/Blueprints/Objects.json#L29155); [HazardTerrainBuilder.cs:113](../Assets/Scripts/Gameplay/World/Generation/Builders/HazardTerrainBuilder.cs#L113); [OverworldZoneManager.cs:926](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L926); [PopulationTable.cs:926](../Assets/Scripts/Data/Tables/PopulationTable.cs#L926); [LiquidCoveredEffect.cs:467](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L467).

### convalessence — `convalessence`

**Classification:** current-collectible-liquid.

**Where it comes from:** ConvalescencePool (80): current underground hazard table and cave populations.

**What it actually does:** Creature contact applies liquid coat; heals 4 HP per coated creature turn-start, capped at maximum. Can pour on self or another occupant; stepping into the pool also coats.

**Limits and gaps:** Physical source must be within one cell, positive-volume, not carried/takeable/creature, and unmixed. Draws conserve finite volume. No flask Drink action; coat amount is exposure, not recoverable units. Coat dries by fluidity+evaporation each owner turn; most benefits/hazards are short-lived. No autonomous NPC flask filling/pouring found.

**Source evidence:** [convalessence.json:1](../Assets/Resources/Content/Data/LiquidDefinitions/convalessence.json#L1); [LiquidVesselPart.cs:15](../Assets/Scripts/Gameplay/Items/LiquidVesselPart.cs#L15); [LiquidVesselService.cs:29](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L29); [LiquidVesselService.cs:234](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L234); [LiquidPoolPart.cs:133](../Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs#L133); [LiquidCoveredEffect.cs:204](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L204); [Objects.json:28999](../Assets/Resources/Content/Blueprints/Objects.json#L28999); [HazardTerrainBuilder.cs:150](../Assets/Scripts/Gameplay/World/Generation/Builders/HazardTerrainBuilder.cs#L150); [OverworldZoneManager.cs:547](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L547); [PopulationTable.cs:787](../Assets/Scripts/Data/Tables/PopulationTable.cs#L787); [LiquidCoveredEffect.cs:283](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L283); [LiquidVesselService.cs:208](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L208).

### memory bath — `memory-bath`

**Classification:** current-collectible-liquid.

**Where it comes from:** MemoryBathPool (60): current underground hazard table and deep cave population.

**What it actually does:** Creature contact creates a temporary one-shot death anchor: incoming pre-resistance lethal damage is nullified and HP raised to at least 50% maximum, then coat consumed. Does not make a saved respawn point.

**Limits and gaps:** Physical source must be within one cell, positive-volume, not carried/takeable/creature, and unmixed. Draws conserve finite volume. No flask Drink action; coat amount is exposure, not recoverable units. Coat dries by fluidity+evaporation each owner turn; most benefits/hazards are short-lived. No autonomous NPC flask filling/pouring found.

**Source evidence:** [memory-bath.json:1](../Assets/Resources/Content/Data/LiquidDefinitions/memory-bath.json#L1); [LiquidVesselPart.cs:15](../Assets/Scripts/Gameplay/Items/LiquidVesselPart.cs#L15); [LiquidVesselService.cs:29](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L29); [LiquidVesselService.cs:234](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L234); [LiquidPoolPart.cs:133](../Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs#L133); [LiquidCoveredEffect.cs:204](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L204); [Objects.json:29077](../Assets/Resources/Content/Blueprints/Objects.json#L29077); [HazardTerrainBuilder.cs:151](../Assets/Scripts/Gameplay/World/Generation/Builders/HazardTerrainBuilder.cs#L151); [OverworldZoneManager.cs:547](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L547); [PopulationTable.cs:955](../Assets/Scripts/Data/Tables/PopulationTable.cs#L955); [LiquidCoveredEffect.cs:389](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L389).

### oil — `oil`

**Classification:** current-collectible-liquid.

**Where it comes from:** OilSlick (80) and TarSeep (95) are current hazard-terrain pools, including Beating and underground tables. Separate OilSeep terrain and Oilmark ability write only a coating and cannot themselves be bottled.

**What it actually does:** Pour creates physical oil pool and four-turn ground oil coating. Creature contact amplifies incoming Heat damage by 45% before resistance; oil ground may cause a 40% slip roll, and oil+heat terrain reactions can burn it. This liquid is separate from LampOil, WardOil, FrogOil and LanternOil items.

**Limits and gaps:** Physical source must be within one cell, positive-volume, not carried/takeable/creature, and unmixed. Draws conserve finite volume. No flask Drink action; coat amount is exposure, not recoverable units. Coat dries by fluidity+evaporation each owner turn; most benefits/hazards are short-lived. No autonomous NPC flask filling/pouring found.

**Source evidence:** [oil.json:1](../Assets/Resources/Content/Data/LiquidDefinitions/oil.json#L1); [LiquidVesselPart.cs:15](../Assets/Scripts/Gameplay/Items/LiquidVesselPart.cs#L15); [LiquidVesselService.cs:29](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L29); [LiquidVesselService.cs:234](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L234); [LiquidPoolPart.cs:133](../Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs#L133); [LiquidCoveredEffect.cs:204](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L204); [Objects.json:21890](../Assets/Resources/Content/Blueprints/Objects.json#L21890); [Objects.json:30973](../Assets/Resources/Content/Blueprints/Objects.json#L30973); [HazardTerrainBuilder.cs:124](../Assets/Scripts/Gameplay/World/Generation/Builders/HazardTerrainBuilder.cs#L124); [HazardTerrainBuilder.cs:141](../Assets/Scripts/Gameplay/World/Generation/Builders/HazardTerrainBuilder.cs#L141); [OverworldZoneManager.cs:547](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L547); [LiquidCoveredEffect.cs:428](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L428); [LiquidSlipSystem.cs:74](../Assets/Scripts/Gameplay/Turns/LiquidSlipSystem.cs#L74).

### water — `water`

**Classification:** current-collectible-liquid.

**Where it comes from:** WaterPuddle (120), PeatBog (70), GroveSeep (40), SprayPool (40), HelmwoodWaterPassage (60), and planned SpreadDrawPoint (usually 3; selected plan adjusts budget) are real current physical sources. Grove and Stump formations, current settlement layouts, and Spread exploration place them. Separate wells/renewing source terrain can fill Waterskin then transfer water into a flask.

**What it actually does:** Pour/contact applies full WetEffect and temporarily reduces incoming Heat damage to 60% before resistance; conductivity doubles direct Electric damage if ElectrifiedEffect is not already handling amplification. Flask water also irrigates, transfers, and douses. Transfer into a Waterskin-type vessel enables drinking to remove one Parched stack.

**Limits and gaps:** Physical source must be within one cell, positive-volume, not carried/takeable/creature, and unmixed. Draws conserve finite volume. No flask Drink action; coat amount is exposure, not recoverable units. Coat dries by fluidity+evaporation each owner turn; most benefits/hazards are short-lived. No autonomous NPC flask filling/pouring found.

**Source evidence:** [water.json:1](../Assets/Resources/Content/Data/LiquidDefinitions/water.json#L1); [LiquidVesselPart.cs:15](../Assets/Scripts/Gameplay/Items/LiquidVesselPart.cs#L15); [LiquidVesselService.cs:29](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L29); [LiquidVesselService.cs:234](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L234); [LiquidPoolPart.cs:133](../Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs#L133); [LiquidCoveredEffect.cs:204](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L204); [Objects.json:21812](../Assets/Resources/Content/Blueprints/Objects.json#L21812); [Objects.json:31252](../Assets/Resources/Content/Blueprints/Objects.json#L31252); [Objects.json:34033](../Assets/Resources/Content/Blueprints/Objects.json#L34033); [Objects.json:34917](../Assets/Resources/Content/Blueprints/Objects.json#L34917); [Objects.json:35673](../Assets/Resources/Content/Blueprints/Objects.json#L35673); [Objects.json:40065](../Assets/Resources/Content/Blueprints/Objects.json#L40065); [HazardTerrainBuilder.cs:96](../Assets/Scripts/Gameplay/World/Generation/Builders/HazardTerrainBuilder.cs#L96); [GrovelandsFormationBuilder.cs:135](../Assets/Scripts/Gameplay/World/Generation/Builders/GrovelandsFormationBuilder.cs#L135); [StumpFormationBuilder.cs:167](../Assets/Scripts/Gameplay/World/Generation/Builders/StumpFormationBuilder.cs#L167); [SpreadExplorationBuilder.cs:319](../Assets/Scripts/Gameplay/World/Generation/Builders/SpreadExplorationBuilder.cs#L319); [OverworldZoneManager.cs:926](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs#L926); [LiquidCoveredEffect.cs:695](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L695); [LiquidCoveredEffect.cs:417](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L417); [CropWateringService.cs:83](../Assets/Scripts/Gameplay/Farming/CropWateringService.cs#L83); [WaterTransferActions.cs:43](../Assets/Scripts/Gameplay/Items/WaterTransferActions.cs#L43).

## Excluded portable definitions

These entries do not contribute to the 293 obtainable families. Their implementation is shown only to distinguish absent inputs, development content and templates from current player options.

### echo knife — `EchoKnife`

**Classification:** excluded-unreachable.

**Where it comes from:** Only MarcelineStock. No current production Marceline/Ondis placement found by root global source census; EchoKnife has no other references outside its blueprint and that stock entry.

**What it actually does:** Melee/throw: 1d4+3 damage per penetration; penetration +1; hit +3; Strength penetration cap uncapped; attributes Cutting Sonic; slots Hand.

**Limits and gaps:** Implemented weapon, excluded from obtainable catalog because its sole supplier is not currently spawned. No special named/narrative power beyond listed stats, class hooks and material properties. Melee needs equipped free compatible hand(s); ordinary throw requires handling strength/range and Agility accuracy, then armor penetration; thrown weapon lands recoverably. Skills require separately learned skill and matching attributes. No authored NPC use found for this exact blueprint (shop stock is not equipment use). Weight 3. Item HP 6; tempering reduces max HP by2 per quench (floor1), max2 quenches, when a melee weapon.

**Source evidence:** [Objects.json:14615](../Assets/Resources/Content/Blueprints/Objects.json#L14615); [LootTables.json:3075](../Assets/Resources/Content/Data/Loot/LootTables.json#L3075); [CombatSystem.cs:239](../Assets/Scripts/Gameplay/Combat/CombatSystem.cs#L239); [OnHitClassEffects.cs:31](../Assets/Scripts/Gameplay/Combat/OnHitClassEffects.cs#L31); [ThrowItemCommand.cs:533](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L533).

### inert sludge — `InertSludge`

**Classification:** unobtainable-current-source-gap.

**Where it comes from:** NO CURRENT NORMAL SOURCE. BrewingService can create it, but every nonempty combination of all 24 authored reagent profiles produces Brew or volatile Mishap. Exhaustive static union probe: 1,471 profiles, zero sludge.

**What it actually does:** If supplied externally/old save: world Compost action on fresh stage-0 cultivated crop spends one and changes stage duration to ceil(3/4 old), once.

**Limits and gaps:** Implemented compost UI/execution is unreachable using current obtainable ingredients; multipliers, stacks and repeated ingredients cannot change property-presence branch. Modded/changed serialized reagents are outside this proof.

**Source evidence:** [Objects.json:6449](../Assets/Resources/Content/Blueprints/Objects.json#L6449); [BrewResolver.cs:42](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L42); [BrewResolver.cs:77](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L77); [BrewResolver.cs:134](../Assets/Scripts/Gameplay/Alchemy/BrewResolver.cs#L134); [CultivationPreparationService.cs:48](../Assets/Scripts/Gameplay/Preparation/CultivationPreparationService.cs#L48); [reagent_reachability_probe.py:1](../Docs/Verification/ItemUtility2026-10-08/reagent_reachability_probe.py#L1).

### charred tonic — `CharredTonic`

**Classification:** unobtainable-development-only.

**Where it comes from:** NO CURRENT NORMAL SOURCE. Exact name occurs in its blueprint, render catalog and development TonicTestBench only; no current world/loot/merchant/craft source found.

**What it actually does:** Drink consumes one carried unit and applies the payload to self; throwing shatters the payload on the impact cell and can affect other actors. Also removes one Parched stack (the shared tonic path does this on a thrown target too). CharredEffect is indefinite and multiplies Material.Combustibility by 0.3; this is not direct fire-damage resistance.

**Limits and gaps:** Ordinary use targets self, including harmful payloads; consuming/throwing spends it. Benefits do not imply general NPC self-use AI. Working payload in a development-only item is not an obtainable mechanic.

**Source evidence:** [Objects.json:6903](../Assets/Resources/Content/Blueprints/Objects.json#L6903); [TonicPart.cs:37](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L37); [TonicPart.cs:93](../Assets/Scripts/Gameplay/Items/TonicPart.cs#L93); [ThrowItemCommand.cs:176](../Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs#L176); [StatusTonicPart.cs:28](../Assets/Scripts/Gameplay/Items/StatusTonicPart.cs#L28); [TonicEffectFactory.cs:35](../Assets/Scripts/Gameplay/Items/TonicEffectFactory.cs#L35); [TonicTestBench.cs:68](../Assets/Scripts/Scenarios/Custom/TonicTestBench.cs#L68); [CharredEffect.cs:26](../Assets/Scripts/Gameplay/Effects/Concrete/CharredEffect.cs#L26).

### flask of lantern oil — `LanternOil`

**Classification:** unobtainable-development-only.

**Where it comes from:** NO CURRENT NORMAL SOURCE. Village material-sandbox layout places three flasks only when DevMode.Enabled and the zone is StartingVillageZoneId; no current loot/merchant/craft source found.

**What it actually does:** If supplied externally: flammable physical oil object (Combustibility 0.9, Volatility 0.8, FuelMass 40, burn rate 1.5, heat output 2.5). Heating can ignite it; fuel/burning mechanics radiate fire and eventually exhaust it into ash. Generic trade value 8.

**Limits and gaps:** No explicit inventory Light action, TorchRefuel, Reagent, Tonic or LiquidVessel part. Cannot use this item as LampOil refuel, WardOil repair, or pourable liquid oil. Existing thermal behavior does not make the item normally obtainable.

**Source evidence:** [Objects.json:22816](../Assets/Resources/Content/Blueprints/Objects.json#L22816); [VillagePopulationBuilder.cs:100](../Assets/Scripts/Gameplay/World/Generation/Builders/VillagePopulationBuilder.cs#L100); [VillagePopulationBuilder.cs:421](../Assets/Scripts/Gameplay/World/Generation/Builders/VillagePopulationBuilder.cs#L421); [ThermalPart.cs:124](../Assets/Scripts/Gameplay/Materials/ThermalPart.cs#L124); [FuelPart.cs:28](../Assets/Scripts/Gameplay/Materials/FuelPart.cs#L28); [BurningEffect.cs:114](../Assets/Scripts/Gameplay/Effects/Concrete/BurningEffect.cs#L114).

### rite grimoire — `RiteGrimoire`

**Classification:** template-not-obtainable.

**Where it comes from:** No current direct source found; inherited template of the eleven named rite books.

**What it actually does:** Template itself has blank pages and no ink charges.

**Limits and gaps:** Exclude as an obtainable concrete item unless another runtime route is found.

**Source evidence:** [Objects.json:24703](../Assets/Resources/Content/Blueprints/Objects.json#L24703); [GrimoirePart.cs:101](../Assets/Scripts/Gameplay/Items/GrimoirePart.cs#L101); [InventorySystem.cs:266](../Assets/Scripts/Gameplay/Inventory/InventorySystem.cs#L266); [InventoryUI.cs:1432](../Assets/Scripts/Presentation/UI/InventoryUI.cs#L1432).

### Item — `Item`

**Classification:** template-not-obtainable.

**Where it comes from:** Inheritance template; no direct current-world acquisition route.

**What it actually does:** Supplies shared parts/parameters to concrete descendants; not a separately available player item.

**Limits and gaps:** Descendants are individually counted in the obtainable catalog.

**Source evidence:** [Objects.json:2266](../Assets/Resources/Content/Blueprints/Objects.json#L2266).

### WeaponComponentItem — `WeaponComponentItem`

**Classification:** template-not-obtainable.

**Where it comes from:** Inheritance template; no direct current-world acquisition route.

**What it actually does:** Supplies shared parts/parameters to concrete descendants; not a separately available player item.

**Limits and gaps:** Descendants are individually counted in the obtainable catalog.

**Source evidence:** [Objects.json:4317](../Assets/Resources/Content/Blueprints/Objects.json#L4317).

### ArmorItem — `ArmorItem`

**Classification:** template-not-obtainable.

**Where it comes from:** Inheritance template; no direct current-world acquisition route.

**What it actually does:** Supplies shared parts/parameters to concrete descendants; not a separately available player item.

**Limits and gaps:** Descendants are individually counted in the obtainable catalog.

**Source evidence:** [Objects.json:7207](../Assets/Resources/Content/Blueprints/Objects.json#L7207).

### TonicItem — `TonicItem`

**Classification:** template-not-obtainable.

**Where it comes from:** Inheritance template; no direct current-world acquisition route.

**What it actually does:** Supplies shared parts/parameters to concrete descendants; not a separately available player item.

**Limits and gaps:** Descendants are individually counted in the obtainable catalog.

**Source evidence:** [Objects.json:3959](../Assets/Resources/Content/Blueprints/Objects.json#L3959).

### ReagentItem — `ReagentItem`

**Classification:** template-not-obtainable.

**Where it comes from:** Inheritance template; no direct current-world acquisition route.

**What it actually does:** Supplies shared parts/parameters to concrete descendants; not a separately available player item.

**Limits and gaps:** Descendants are individually counted in the obtainable catalog.

**Source evidence:** [Objects.json:5247](../Assets/Resources/Content/Blueprints/Objects.json#L5247).

### MeleeWeapon — `MeleeWeapon`

**Classification:** template-not-obtainable.

**Where it comes from:** Inheritance template; no direct current-world acquisition route.

**What it actually does:** Supplies shared parts/parameters to concrete descendants; not a separately available player item.

**Limits and gaps:** Descendants are individually counted in the obtainable catalog.

**Source evidence:** [Objects.json:280](../Assets/Resources/Content/Blueprints/Objects.json#L280).

### FoodItem — `FoodItem`

**Classification:** template-not-obtainable.

**Where it comes from:** Inheritance template; no direct current-world acquisition route.

**What it actually does:** Supplies shared parts/parameters to concrete descendants; not a separately available player item.

**Limits and gaps:** Descendants are individually counted in the obtainable catalog.

**Source evidence:** [Objects.json:3825](../Assets/Resources/Content/Blueprints/Objects.json#L3825).

## Other liquid definitions

Brine and bog mire exist in the world and affect contact, but their own water TileStateSource makes the purity check reject collection. The remaining 18 definitions have no current physical collection source. Some have world-coating or spell roles; a definition or coating is not portable liquid inventory.

### bog mire — `bog-mire`

**Classification:** current-world-liquid-blocked-collection.

**Where it comes from:** MirePool (60) is current Sodden formation/composition, district and Ginmere terrain. However, this same object has TileStateSource Coating=water, CoatingTurns=4, so the flask mixed-source gate rejects it.

**What it actually does:** World contact still coats: 1 Acid damage per turn and -2 Agility; FireDampen=50 halves incoming Heat damage before resistance. Uncollectible through the normal flask gate.

**Limits and gaps:** Physical source must be within one cell, positive-volume, not carried/takeable/creature, and unmixed. Draws conserve finite volume. No flask Drink action; coat amount is exposure, not recoverable units. Coat dries by fluidity+evaporation each owner turn; most benefits/hazards are short-lived. No autonomous NPC flask filling/pouring found. Unlike-coating metadata is on the source itself, so even a solitary source on otherwise clean ground is rejected. No production assignment/removal that clears this TileStateSource was found; drying the tile cannot bypass the direct field check.

**Source evidence:** [bog-mire.json:1](../Assets/Resources/Content/Data/LiquidDefinitions/bog-mire.json#L1); [LiquidVesselPart.cs:15](../Assets/Scripts/Gameplay/Items/LiquidVesselPart.cs#L15); [LiquidVesselService.cs:29](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L29); [LiquidVesselService.cs:234](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L234); [LiquidPoolPart.cs:133](../Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs#L133); [LiquidCoveredEffect.cs:204](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L204); [Objects.json:33100](../Assets/Resources/Content/Blueprints/Objects.json#L33100); [SoddenFormationBuilder.cs:88](../Assets/Scripts/Gameplay/World/Generation/Builders/SoddenFormationBuilder.cs#L88); [SoddenDistrictBuilder.cs:176](../Assets/Scripts/Gameplay/World/Generation/Builders/SoddenDistrictBuilder.cs#L176); [LiquidVesselService.cs:297](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L297); [LiquidCoveredEffect.cs:267](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L267); [LiquidCoveredEffect.cs:428](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L428); [LiquidCoveredEffect.cs:579](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L579).

### bower-resin amber — `bower-resin-amber`

**Classification:** definition-only-not-collectible.

**Where it comes from:** No authored LiquidPool or initially filled LiquidVessel with this ID, and no production runtime pool-construction route found. Registry definition alone is not an obtainable item.

**What it actually does:** Conditional engine configuration only: {"Conductivity":0,"Combustibility":40,"FireDampen":0,"FlameTemperature":280,"Adsorbence":100,"Fluidity":3,"Evaporativity":1,"Staining":1,"Slippery":false,"Sticky":true,"StatModifiers":[{"Stat":"DV","Delta":3},{"Stat":"AV","Delta":2}]}. Do not count this as a current player consumable.

**Limits and gaps:** Development scenarios and externally supplied/saved/modded pools are excluded. Shared liquid engine may implement these values, but this audit makes no claim of a current acquisition route.

**Source evidence:** [bower-resin-amber.json:1](../Assets/Resources/Content/Data/LiquidDefinitions/bower-resin-amber.json#L1); [LiquidVesselPart.cs:15](../Assets/Scripts/Gameplay/Items/LiquidVesselPart.cs#L15); [LiquidVesselService.cs:29](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L29); [LiquidVesselService.cs:234](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L234); [LiquidPoolPart.cs:133](../Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs#L133); [LiquidCoveredEffect.cs:204](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L204).

### brine — `brine`

**Classification:** current-world-liquid-blocked-collection.

**Where it comes from:** BrinePool (90) exists in multiple current hazard tables and Beating basins. However, this same object has TileStateSource Coating=water, CoatingTurns=4, so the flask mixed-source gate rejects it.

**What it actually does:** World contact still coats: +15 HeatResistance, -15 ElectricResistance; its conductivity doubles direct Electric damage when ElectrifiedEffect does not already own amplification. Uncollectible through the normal flask gate.

**Limits and gaps:** Physical source must be within one cell, positive-volume, not carried/takeable/creature, and unmixed. Draws conserve finite volume. No flask Drink action; coat amount is exposure, not recoverable units. Coat dries by fluidity+evaporation each owner turn; most benefits/hazards are short-lived. No autonomous NPC flask filling/pouring found. Unlike-coating metadata is on the source itself, so even a solitary source on otherwise clean ground is rejected. No production assignment/removal that clears this TileStateSource was found; drying the tile cannot bypass the direct field check.

**Source evidence:** [brine.json:1](../Assets/Resources/Content/Data/LiquidDefinitions/brine.json#L1); [LiquidVesselPart.cs:15](../Assets/Scripts/Gameplay/Items/LiquidVesselPart.cs#L15); [LiquidVesselService.cs:29](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L29); [LiquidVesselService.cs:234](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L234); [LiquidPoolPart.cs:133](../Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs#L133); [LiquidCoveredEffect.cs:204](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L204); [Objects.json:30812](../Assets/Resources/Content/Blueprints/Objects.json#L30812); [HazardTerrainBuilder.cs:79](../Assets/Scripts/Gameplay/World/Generation/Builders/HazardTerrainBuilder.cs#L79); [BeatingCompositionPlan.cs:194](../Assets/Scripts/Gameplay/World/Generation/BeatingCompositionPlan.cs#L194); [LiquidVesselService.cs:297](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L297); [LiquidCoveredEffect.cs:417](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L417); [LiquidCoveredEffect.cs:579](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L579).

### carapace ichor — `carapace-ichor`

**Classification:** definition-only-not-collectible.

**Where it comes from:** No authored LiquidPool or initially filled LiquidVessel with this ID, and no production runtime pool-construction route found. Registry definition alone is not an obtainable item.

**What it actually does:** Conditional engine configuration only: {"Conductivity":0,"Combustibility":0,"FireDampen":0,"FlameTemperature":99999,"Adsorbence":100,"Fluidity":8,"Evaporativity":6,"Staining":0,"Slippery":false,"Sticky":false,"StatModifiers":[{"Stat":"AV","Delta":4}],"ResistanceModifiers":[{"Stat":"ColdResistance","Delta":-20}]}. Do not count this as a current player consumable.

**Limits and gaps:** Development scenarios and externally supplied/saved/modded pools are excluded. Shared liquid engine may implement these values, but this audit makes no claim of a current acquisition route.

**Source evidence:** [carapace-ichor.json:1](../Assets/Resources/Content/Data/LiquidDefinitions/carapace-ichor.json#L1); [LiquidVesselPart.cs:15](../Assets/Scripts/Gameplay/Items/LiquidVesselPart.cs#L15); [LiquidVesselService.cs:29](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L29); [LiquidVesselService.cs:234](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L234); [LiquidPoolPart.cs:133](../Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs#L133); [LiquidCoveredEffect.cs:204](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L204).

### Choir wort — `choir-wort`

**Classification:** definition-only-not-collectible.

**Where it comes from:** No authored LiquidPool or initially filled LiquidVessel with this ID, and no production runtime pool-construction route found. Registry definition alone is not an obtainable item.

**What it actually does:** Conditional engine configuration only: {"Conductivity":0,"Combustibility":10,"FireDampen":0,"FlameTemperature":99999,"Adsorbence":100,"Fluidity":12,"Evaporativity":3,"Staining":2,"Slippery":false,"Sticky":false,"PerTurnDamage":{"Amount":4,"Type":"Acid"},"StatModifiers":[{"Stat":"Toughness","Delta":-3}]}. Do not count this as a current player consumable.

**Limits and gaps:** Development scenarios and externally supplied/saved/modded pools are excluded. Shared liquid engine may implement these values, but this audit makes no claim of a current acquisition route.

**Source evidence:** [choir-wort.json:1](../Assets/Resources/Content/Data/LiquidDefinitions/choir-wort.json#L1); [LiquidVesselPart.cs:15](../Assets/Scripts/Gameplay/Items/LiquidVesselPart.cs#L15); [LiquidVesselService.cs:29](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L29); [LiquidVesselService.cs:234](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L234); [LiquidPoolPart.cs:133](../Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs#L133); [LiquidCoveredEffect.cs:204](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L204).

### felling-counter resin — `felling-counter-resin`

**Classification:** definition-only-not-collectible.

**Where it comes from:** No authored LiquidPool or initially filled LiquidVessel with this ID, and no production runtime pool-construction route found. Registry definition alone is not an obtainable item.

**What it actually does:** Conditional engine configuration only: {"Conductivity":15,"Combustibility":35,"FireDampen":0,"FlameTemperature":99999,"Adsorbence":100,"Fluidity":5,"Evaporativity":3,"Staining":3,"Slippery":false,"Sticky":false,"HpRewindOnTurnEnd":true}. Do not count this as a current player consumable.

**Limits and gaps:** Development scenarios and externally supplied/saved/modded pools are excluded. Shared liquid engine may implement these values, but this audit makes no claim of a current acquisition route.

**Source evidence:** [felling-counter-resin.json:1](../Assets/Resources/Content/Data/LiquidDefinitions/felling-counter-resin.json#L1); [LiquidVesselPart.cs:15](../Assets/Scripts/Gameplay/Items/LiquidVesselPart.cs#L15); [LiquidVesselService.cs:29](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L29); [LiquidVesselService.cs:234](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L234); [LiquidPoolPart.cs:133](../Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs#L133); [LiquidCoveredEffect.cs:204](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L204).

### gel — `gel`

**Classification:** definition-only-not-collectible.

**Where it comes from:** No authored LiquidPool or initially filled LiquidVessel with this ID, and no production runtime pool-construction route found. Registry definition alone is not an obtainable item.

**What it actually does:** Conditional engine configuration only: {"Conductivity":100,"Combustibility":0,"FireDampen":0,"FlameTemperature":99999,"Adsorbence":100,"Fluidity":5,"Evaporativity":1,"Staining":0,"Slippery":true,"SlipChance":50,"Sticky":false}. Do not count this as a current player consumable.

**Limits and gaps:** Development scenarios and externally supplied/saved/modded pools are excluded. Shared liquid engine may implement these values, but this audit makes no claim of a current acquisition route.

**Source evidence:** [gel.json:1](../Assets/Resources/Content/Data/LiquidDefinitions/gel.json#L1); [LiquidVesselPart.cs:15](../Assets/Scripts/Gameplay/Items/LiquidVesselPart.cs#L15); [LiquidVesselService.cs:29](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L29); [LiquidVesselService.cs:234](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L234); [LiquidPoolPart.cs:133](../Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs#L133); [LiquidCoveredEffect.cs:204](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L204).

### held-breath lacquer — `held-breath-lacquer`

**Classification:** definition-only-not-collectible.

**Where it comes from:** No authored LiquidPool or initially filled LiquidVessel with this ID, and no production runtime pool-construction route found. Registry definition alone is not an obtainable item.

**What it actually does:** Conditional engine configuration only: {"Conductivity":10,"Combustibility":5,"FireDampen":5,"FlameTemperature":99999,"Adsorbence":100,"Fluidity":2,"Evaporativity":1,"Staining":0,"Slippery":false,"Sticky":false,"PreventDeath":true,"BlockAction":true}. Do not count this as a current player consumable.

**Limits and gaps:** Development scenarios and externally supplied/saved/modded pools are excluded. Shared liquid engine may implement these values, but this audit makes no claim of a current acquisition route.

**Source evidence:** [held-breath-lacquer.json:1](../Assets/Resources/Content/Data/LiquidDefinitions/held-breath-lacquer.json#L1); [LiquidVesselPart.cs:15](../Assets/Scripts/Gameplay/Items/LiquidVesselPart.cs#L15); [LiquidVesselService.cs:29](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L29); [LiquidVesselService.cs:234](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L234); [LiquidPoolPart.cs:133](../Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs#L133); [LiquidCoveredEffect.cs:204](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L204).

### honey — `honey`

**Classification:** definition-only-not-collectible.

**Where it comes from:** No authored LiquidPool or initially filled LiquidVessel with this ID, and no production runtime pool-construction route found. Registry definition alone is not an obtainable item.

**What it actually does:** Conditional engine configuration only: {"Conductivity":0,"Combustibility":60,"FireDampen":0,"FlameTemperature":300,"Adsorbence":25,"Fluidity":10,"Evaporativity":1,"Staining":1,"Slippery":false,"Sticky":true,"StatModifiers":[{"Stat":"Agility","Delta":-2},{"Stat":"DV","Delta":-3}]}. Do not count this as a current player consumable.

**Limits and gaps:** Development scenarios and externally supplied/saved/modded pools are excluded. Shared liquid engine may implement these values, but this audit makes no claim of a current acquisition route.

**Source evidence:** [honey.json:1](../Assets/Resources/Content/Data/LiquidDefinitions/honey.json#L1); [LiquidVesselPart.cs:15](../Assets/Scripts/Gameplay/Items/LiquidVesselPart.cs#L15); [LiquidVesselService.cs:29](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L29); [LiquidVesselService.cs:234](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L234); [LiquidPoolPart.cs:133](../Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs#L133); [LiquidCoveredEffect.cs:204](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L204).

### ice — `ice`

**Classification:** definition-only-not-collectible.

**Where it comes from:** No authored LiquidPool or initially filled LiquidVessel with this ID, and no production runtime pool-construction route found. Registry definition alone is not an obtainable item. Ice is separately present as current frozen/slippery tile state; frozen water is explicitly undrawable, not converted into bottled ice.

**What it actually does:** Conditional engine configuration only: {"Conductivity":0,"Combustibility":0,"FireDampen":0,"FlameTemperature":99999,"Adsorbence":0,"Fluidity":0,"Evaporativity":0,"Staining":0,"Slippery":true,"SlipChance":50,"Sticky":false}. Do not count this as a current player consumable.

**Limits and gaps:** Development scenarios and externally supplied/saved/modded pools are excluded. Shared liquid engine may implement these values, but this audit makes no claim of a current acquisition route.

**Source evidence:** [ice.json:1](../Assets/Resources/Content/Data/LiquidDefinitions/ice.json#L1); [LiquidVesselPart.cs:15](../Assets/Scripts/Gameplay/Items/LiquidVesselPart.cs#L15); [LiquidVesselService.cs:29](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L29); [LiquidVesselService.cs:234](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L234); [LiquidPoolPart.cs:133](../Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs#L133); [LiquidCoveredEffect.cs:204](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L204); [LiquidSourcePhase.cs:20](../Assets/Scripts/Gameplay/Items/LiquidSourcePhase.cs#L20); [LiquidSlipSystem.cs:131](../Assets/Scripts/Gameplay/Turns/LiquidSlipSystem.cs#L131).

### iron-gall ink — `iron-gall-ink`

**Classification:** definition-only-not-collectible.

**Where it comes from:** No authored LiquidPool or initially filled LiquidVessel with this ID, and no production runtime pool-construction route found. Registry definition alone is not an obtainable item.

**What it actually does:** Conditional engine configuration only: {"Conductivity":60,"Combustibility":0,"FireDampen":0,"FlameTemperature":99999,"Adsorbence":100,"Fluidity":8,"Evaporativity":4,"Staining":3,"Slippery":false,"Sticky":false,"PerTurnDamage":{"Amount":2,"Type":"Acid"}}. Do not count this as a current player consumable.

**Limits and gaps:** Development scenarios and externally supplied/saved/modded pools are excluded. Shared liquid engine may implement these values, but this audit makes no claim of a current acquisition route.

**Source evidence:** [iron-gall-ink.json:1](../Assets/Resources/Content/Data/LiquidDefinitions/iron-gall-ink.json#L1); [LiquidVesselPart.cs:15](../Assets/Scripts/Gameplay/Items/LiquidVesselPart.cs#L15); [LiquidVesselService.cs:29](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L29); [LiquidVesselService.cs:234](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L234); [LiquidPoolPart.cs:133](../Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs#L133); [LiquidCoveredEffect.cs:204](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L204).

### lantern-beetle ichor — `lantern-beetle-ichor`

**Classification:** definition-only-not-collectible.

**Where it comes from:** No authored LiquidPool or initially filled LiquidVessel with this ID, and no production runtime pool-construction route found. Registry definition alone is not an obtainable item.

**What it actually does:** Conditional engine configuration only: {"Conductivity":0,"Combustibility":5,"FireDampen":0,"FlameTemperature":99999,"Adsorbence":100,"Fluidity":5,"Evaporativity":3,"Staining":1,"Slippery":false,"Sticky":false,"LightRadius":6,"LightColor":"&Y"}. Do not count this as a current player consumable.

**Limits and gaps:** Development scenarios and externally supplied/saved/modded pools are excluded. Shared liquid engine may implement these values, but this audit makes no claim of a current acquisition route.

**Source evidence:** [lantern-beetle-ichor.json:1](../Assets/Resources/Content/Data/LiquidDefinitions/lantern-beetle-ichor.json#L1); [LiquidVesselPart.cs:15](../Assets/Scripts/Gameplay/Items/LiquidVesselPart.cs#L15); [LiquidVesselService.cs:29](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L29); [LiquidVesselService.cs:234](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L234); [LiquidPoolPart.cs:133](../Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs#L133); [LiquidCoveredEffect.cs:204](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L204).

### lava — `lava`

**Classification:** definition-only-not-collectible.

**Where it comes from:** No authored LiquidPool or initially filled LiquidVessel with this ID, and no production runtime pool-construction route found. Registry definition alone is not an obtainable item.

**What it actually does:** Conditional engine configuration only: {"Conductivity":90,"Combustibility":0,"FireDampen":0,"FlameTemperature":99999,"Adsorbence":100,"Fluidity":15,"Evaporativity":0,"Staining":0,"Slippery":false,"Sticky":false,"PerTurnDamage":{"Amount":8,"Type":"Heat"},"ResistanceModifiers":[{"Stat":"HeatResistance","Delta":-25}]}. Do not count this as a current player consumable.

**Limits and gaps:** Development scenarios and externally supplied/saved/modded pools are excluded. Shared liquid engine may implement these values, but this audit makes no claim of a current acquisition route.

**Source evidence:** [lava.json:1](../Assets/Resources/Content/Data/LiquidDefinitions/lava.json#L1); [LiquidVesselPart.cs:15](../Assets/Scripts/Gameplay/Items/LiquidVesselPart.cs#L15); [LiquidVesselService.cs:29](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L29); [LiquidVesselService.cs:234](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L234); [LiquidPoolPart.cs:133](../Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs#L133); [LiquidCoveredEffect.cs:204](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L204).

### lumen-slime — `lumen-slime`

**Classification:** definition-only-not-collectible.

**Where it comes from:** No authored LiquidPool or initially filled LiquidVessel with this ID, and no production runtime pool-construction route found. Registry definition alone is not an obtainable item.

**What it actually does:** Conditional engine configuration only: {"Conductivity":0,"Combustibility":0,"FireDampen":0,"FlameTemperature":99999,"Adsorbence":100,"Fluidity":4,"Evaporativity":1,"Staining":2,"Slippery":false,"Sticky":false,"StatModifiers":[{"Stat":"DV","Delta":-3}]}. Do not count this as a current player consumable.

**Limits and gaps:** Development scenarios and externally supplied/saved/modded pools are excluded. Shared liquid engine may implement these values, but this audit makes no claim of a current acquisition route.

**Source evidence:** [lumen-slime.json:1](../Assets/Resources/Content/Data/LiquidDefinitions/lumen-slime.json#L1); [LiquidVesselPart.cs:15](../Assets/Scripts/Gameplay/Items/LiquidVesselPart.cs#L15); [LiquidVesselService.cs:29](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L29); [LiquidVesselService.cs:234](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L234); [LiquidPoolPart.cs:133](../Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs#L133); [LiquidCoveredEffect.cs:204](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L204).

### pebble-sundew dew — `pebble-sundew-dew`

**Classification:** definition-only-not-collectible.

**Where it comes from:** No authored LiquidPool or initially filled LiquidVessel with this ID, and no production runtime pool-construction route found. Registry definition alone is not an obtainable item.

**What it actually does:** Conditional engine configuration only: {"Conductivity":35,"Combustibility":5,"FireDampen":10,"FlameTemperature":99999,"Adsorbence":100,"Fluidity":6,"Evaporativity":4,"Staining":1,"Slippery":true,"SlipChance":30,"Sticky":false,"KnockbackOnHit":true}. Do not count this as a current player consumable.

**Limits and gaps:** Development scenarios and externally supplied/saved/modded pools are excluded. Shared liquid engine may implement these values, but this audit makes no claim of a current acquisition route.

**Source evidence:** [pebble-sundew-dew.json:1](../Assets/Resources/Content/Data/LiquidDefinitions/pebble-sundew-dew.json#L1); [LiquidVesselPart.cs:15](../Assets/Scripts/Gameplay/Items/LiquidVesselPart.cs#L15); [LiquidVesselService.cs:29](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L29); [LiquidVesselService.cs:234](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L234); [LiquidPoolPart.cs:133](../Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs#L133); [LiquidCoveredEffect.cs:204](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L204).

### pitch — `pitch`

**Classification:** definition-only-not-collectible.

**Where it comes from:** No authored LiquidPool or initially filled LiquidVessel with this ID, and no production runtime pool-construction route found. Registry definition alone is not an obtainable item.

**What it actually does:** Conditional engine configuration only: {"Conductivity":0,"Combustibility":90,"FireDampen":0,"FlameTemperature":250,"Adsorbence":100,"Fluidity":5,"Evaporativity":2,"Staining":0,"Slippery":false,"Sticky":true,"StatModifiers":[{"Stat":"Agility","Delta":-2},{"Stat":"DV","Delta":-3}]}. Do not count this as a current player consumable.

**Limits and gaps:** Development scenarios and externally supplied/saved/modded pools are excluded. Shared liquid engine may implement these values, but this audit makes no claim of a current acquisition route.

**Source evidence:** [pitch.json:1](../Assets/Resources/Content/Data/LiquidDefinitions/pitch.json#L1); [LiquidVesselPart.cs:15](../Assets/Scripts/Gameplay/Items/LiquidVesselPart.cs#L15); [LiquidVesselService.cs:29](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L29); [LiquidVesselService.cs:234](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L234); [LiquidPoolPart.cs:133](../Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs#L133); [LiquidCoveredEffect.cs:204](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L204).

### sap — `sap`

**Classification:** definition-only-not-collectible.

**Where it comes from:** No authored LiquidPool or initially filled LiquidVessel with this ID, and no production runtime pool-construction route found. Registry definition alone is not an obtainable item.

**What it actually does:** Conditional engine configuration only: {"Conductivity":0,"Combustibility":70,"FireDampen":0,"FlameTemperature":250,"Adsorbence":25,"Fluidity":3,"Evaporativity":1,"Staining":2,"Slippery":false,"Sticky":true,"StatModifiers":[{"Stat":"Agility","Delta":-2}]}. Do not count this as a current player consumable.

**Limits and gaps:** Development scenarios and externally supplied/saved/modded pools are excluded. Shared liquid engine may implement these values, but this audit makes no claim of a current acquisition route.

**Source evidence:** [sap.json:1](../Assets/Resources/Content/Data/LiquidDefinitions/sap.json#L1); [LiquidVesselPart.cs:15](../Assets/Scripts/Gameplay/Items/LiquidVesselPart.cs#L15); [LiquidVesselService.cs:29](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L29); [LiquidVesselService.cs:234](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L234); [LiquidPoolPart.cs:133](../Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs#L133); [LiquidCoveredEffect.cs:204](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L204).

### sundew mucilage — `sundew-mucilage`

**Classification:** definition-only-not-collectible.

**Where it comes from:** No authored LiquidPool or initially filled LiquidVessel with this ID, and no production runtime pool-construction route found. Registry definition alone is not an obtainable item.

**What it actually does:** Conditional engine configuration only: {"Conductivity":0,"Combustibility":20,"FireDampen":0,"FlameTemperature":99999,"Adsorbence":100,"Fluidity":2,"Evaporativity":1,"Staining":1,"Slippery":false,"Sticky":true,"StatModifiers":[{"Stat":"Agility","Delta":-4},{"Stat":"DV","Delta":-5}]}. Do not count this as a current player consumable.

**Limits and gaps:** Development scenarios and externally supplied/saved/modded pools are excluded. Shared liquid engine may implement these values, but this audit makes no claim of a current acquisition route.

**Source evidence:** [sundew-mucilage.json:1](../Assets/Resources/Content/Data/LiquidDefinitions/sundew-mucilage.json#L1); [LiquidVesselPart.cs:15](../Assets/Scripts/Gameplay/Items/LiquidVesselPart.cs#L15); [LiquidVesselService.cs:29](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L29); [LiquidVesselService.cs:234](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L234); [LiquidPoolPart.cs:133](../Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs#L133); [LiquidCoveredEffect.cs:204](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L204).

### tepuibone slurry — `tepuibone-slurry`

**Classification:** definition-only-not-collectible.

**Where it comes from:** No authored LiquidPool or initially filled LiquidVessel with this ID, and no production runtime pool-construction route found. Registry definition alone is not an obtainable item.

**What it actually does:** Conditional engine configuration only: {"Conductivity":0,"Combustibility":0,"FireDampen":0,"FlameTemperature":99999,"Adsorbence":100,"Fluidity":8,"Evaporativity":12,"Staining":0,"Slippery":false,"Sticky":false,"StatModifiers":[{"Stat":"AV","Delta":6},{"Stat":"Toughness","Delta":4}],"ResistanceModifiers":[{"Stat":"HeatResistance","Delta":25},{"Stat":"ColdResistance","Delta":25},{"Stat":"ElectricResistance","Delta":25},{"Stat":"AcidResistance","Delta":25}]}. Do not count this as a current player consumable.

**Limits and gaps:** Development scenarios and externally supplied/saved/modded pools are excluded. Shared liquid engine may implement these values, but this audit makes no claim of a current acquisition route.

**Source evidence:** [tepuibone-slurry.json:1](../Assets/Resources/Content/Data/LiquidDefinitions/tepuibone-slurry.json#L1); [LiquidVesselPart.cs:15](../Assets/Scripts/Gameplay/Items/LiquidVesselPart.cs#L15); [LiquidVesselService.cs:29](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L29); [LiquidVesselService.cs:234](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L234); [LiquidPoolPart.cs:133](../Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs#L133); [LiquidCoveredEffect.cs:204](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L204).

### veined pulse mycelium — `veined-pulse-mycelium`

**Classification:** definition-only-not-collectible.

**Where it comes from:** No authored LiquidPool or initially filled LiquidVessel with this ID, and no production runtime pool-construction route found. Registry definition alone is not an obtainable item.

**What it actually does:** Conditional engine configuration only: {"Conductivity":0,"Combustibility":10,"FireDampen":0,"FlameTemperature":99999,"Adsorbence":100,"Fluidity":3,"Evaporativity":2,"Staining":4,"Slippery":false,"Sticky":false,"ImmuneElement":"Electric"}. Do not count this as a current player consumable.

**Limits and gaps:** Development scenarios and externally supplied/saved/modded pools are excluded. Shared liquid engine may implement these values, but this audit makes no claim of a current acquisition route.

**Source evidence:** [veined-pulse-mycelium.json:1](../Assets/Resources/Content/Data/LiquidDefinitions/veined-pulse-mycelium.json#L1); [LiquidVesselPart.cs:15](../Assets/Scripts/Gameplay/Items/LiquidVesselPart.cs#L15); [LiquidVesselService.cs:29](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L29); [LiquidVesselService.cs:234](../Assets/Scripts/Gameplay/Items/LiquidVesselService.cs#L234); [LiquidPoolPart.cs:133](../Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs#L133); [LiquidCoveredEffect.cs:204](../Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs#L204).
