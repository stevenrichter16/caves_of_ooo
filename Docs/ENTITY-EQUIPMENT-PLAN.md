# Entity equipment and ground-item art — implementation plan

Root status: GA03i CONTENT COMPLETE,2026-09-06; ground-item art is the next separate phase.24 kits, quiet spawn messages, restored natural creation/conservative save repair and two-hand selection guard are verified. Final9599/9599GREEN (+115),0CS; native23PASS; independent reviews and paired75s captures complete. See GA03i-REPORT and the natural activation plan for actual scope and limits.

Preparation record: the proposal below was written before implementation on 2026-09-06. Its original author performed source review only. Root subsequently read it in full, copied it here, and is recording actual changes and evidence in the implementation log below.

Authorization: the user explicitly asked to give entities more equipment, then resumed coding and requested sprite improvements along the way. Root owns implementation. This preparation is limited to source reads and this file; no Assets/Docs edits or art generation.

All repository-relative paths below resolve under /Users/steven/caves-of-ooo. Line anchors were checked during the preparation; rediscover by symbol before applying a splice because other work is concurrent.

## 1. Finite goal and phase boundaries

1. **Content phase:** append explicit Loadout Parts to the 24 reviewed ordinary-world humanoid/NPC blueprints in section 4 and make their factory-spawn equipment announcements quiet through the narrow option in section 5.1. Reuse the completed GA03g equipment lifecycle. Preserve stats, natural weapons, anatomy, factions, XP, AI, descriptions, traders, spawn weights, loot tables, and save schema. Deliberate equipped-weapon replacement and armor effects are enumerated below; they are balance changes, not a claim of stat neutrality.
2. **Sprite phase:** make existing equipped/dropped item kinds legible with six new ground families: dagger, sword, spear, boots, gloves, helmet. Also correct the existing wrong Mace-as-vial identity with a seventh family. Use the current item-body renderer seam; do not add actor equipment layering, item portraits, a new rendering subsystem, or per-frame allocation.
3. **Optional separate extension:** axe, hammer, shield, cloak ground families address already-existing glyph-only gear. This does not automatically expand the 24 kits, give the warlord a different weapon, or solve the Armorer shelf collision. Root may fold this into the art batch only after explicit asset/registration REDs, with the final scope stated accurately.

The content is CoO-original authoring, not a claim of Qud content/canon parity. It reuses the Qud-inspired lifecycle already reviewed in Docs/Verification/GameSystemAudit/GA03g-REPORT.md and Docs/LOADOUT-LIFECYCLE-PLAN.md. Reimplementing that lifecycle is out of scope. Native scenario API fixtures can prove mechanics but cannot substitute for ordinary producer evidence or visual inspection.

## 2. Canon and content-readiness constraints

Current authority is Lore/10_Bible.md plus Lore/11_SecondSpine.md, as established by Lore/README.md:15-24. Lore/TERMS.md freezes internal faction/blueprint IDs; `Palimpsest` remains the internal Recension faction ID. Do not use the parallel Docs/Lore redesign as current authority or introduce lore about the deliberately unexplained Glassblown Remnant.

Lore/History/08_MaterialCulture.md:117-126 supports mundane practical recovered-world village wear; :134-140 supports Curation gloves; :143-145 supports portable Concord travel kit; :159-163 supports practical layered Tent-Right wear. Those profiles remain subordinate to the current Bible/Second Spine. A generic cap/boots loadout does not invent a canonical uniform. The guest-cloth is ceremonial and must not be represented by generic armor or granted oath mechanics.

- GREEN source readiness: all 24 proposed IDs exist, are ordinary-world producer targets, have inherited Body/Inventory and effective Humanoid anatomy. All proposed item IDs exist. No candidate has an explicit Anatomy override or a NoDropOnDeath/Temporary/SuppressCorpseDrops tag in the merged definitions. All currently have Inventory.MaxWeight=150.
- GREEN source readiness: none of the 24 currently has Loadout or LootClass. This is actual producer integration, not a repeat of the historical false claim that sixteen current enemies already carried authored gear.
- YELLOW art readiness: existing gear uses real imported generic family art. It does not yet have the distinct families proposed in section 8. Boots/gloves/helmets currently look like tinted cuirasses; slash-painted weapons share a dropped blade.
- YELLOW balance: real equipment replaces the natural weapon on that hand; armor applies at hit locations and can reduce global DV or Speed. Class riders and stronger guard weapons must be deliberately accepted/tested, not described as cosmetic.
- YELLOW stock seam: incoming gear can merge into existing shop stock before auto-equip. The proposed trader kits avoid that overlap; Armorer is excluded pending a separate solution.
- WHITE exclusions: no base Creature/Villager loadout, frogs, beasts, oozes, vegetation, golems, special legacy characters, new spell/mineral modifications, new skills, artifacts, rented Loaners as personal equipment, or new glyph-only shovel. Technical Humanoid defaults on a beast are not evidence that it should wield a sword.

PeatCutter's description references thigh boots and a long spade. LeatherBoots plus work gloves supports the role modestly; a carried Dagger is a utility blade, not the described spade. CurationSorter is unusually strong content fit: Objects.json:29287 says "Gloves, always."

## 3. Source corrections and exact existing contracts

| False/unsafe premise | Verified source | Consequence |
| --- | --- | --- |
| Existing current enemies already have Loadout | Current Objects.json inheritance sweep found zero Loadout definitions; Docs/LOOT-OVERHAUL.md opening correction records the prior stale claim | This phase introduces real producers. Reconcile historical no-current-producer notes without rewriting old benchmark evidence as ordinary-world play |
| Appending a child override moves an inherited Part to the end | Data/Blueprints/BlueprintLoader.cs:162-181 copies parent insertion order and merges child parameters in place | Explicitly audit inherited part ordering; setting child Loadout parameters cannot move its inherited position |
| All 24 have a possible parent-Trader ordering problem | Section 5's complete inheritance sweep | No current collision: only Snapjaw is a selected parent, and its four descendants have no Trader |
| Loadout should use raw inventory Equip | Gameplay/Entities/LoadoutPart.cs:178-193 calls InventorySystem.AutoEquip, which uses normal command hooks/bonuses/no-displacement | Reuse shipped implementation. Do not replay paid modifications or add a raw-equip bypass |
| Author `Chance`/`Items`/`Contents` fields, or literal percent signs | LoadoutPart.cs:42-50 and ParseCarry:206-259 | Exact string fields are Equip, Carry, Pick. `LeatherCap:20`, not `LeatherCap:20%`. Bare names mean one guaranteed unit |
| Gear is additional melee damage on top of the same hand's claw | Gameplay/Combat/CombatSystem.cs:613-650 | Equipped melee wins that hand; another hand can retain its natural behavior. Do not count each item as a new attack |
| Dagger is Cutting/ShortBlades, or Spear is Cutting | Inheritance-resolved Objects.json:291,6214 | Both are Piercing only. Changing claws/ritual knives changes universal class riders |
| Dice/penetration equality means behavior equality | Gameplay/Combat/OnHitClassEffects.cs:44-54,80-91 | Cutting has 25% bleed; Piercing 10% confusion; Bludgeoning 15% stun. Compare actual damage Attributes |
| Sum GetAV means every limb receives every armor piece | CombatSystem.cs:1567-1586 GetPartAV; :679 GetDV | Natural armor plus struck-location armor; armor DV accumulates globally. Test locations separately |
| Personal gear can simply precede Trader | Gameplay/Economy/TraderPart.cs:94; LandmarkBuilder.cs:1355 | Existing raw carried items suppress opening-stock roll. Trader must run first for this content-only approach |
| Gear matching shelf stock is guaranteed to equip after Trader | LoadoutPart.cs:178 then InventoryPart.cs:87-113; StackerPart.cs:24 MaxStack=99 | AddObject may consume the incoming reference into a resident stack; AutoEquip then refuses it. Avoid overlap in kits |
| Successfully equipped gear prevents shelf restocking | Normal equipment removes it from Objects; TraderRestockSystem.cs:76 counts Objects only | Successfully equipped gear neither counts as shelf stock nor participates in later AddObject merges. Carried items do |
| AutoEquip's false message argument makes factory grants silent | EquipCommand.cs:174 unconditionally logs success; AutoEquipCommand.cs:113/:129 sets only emitPlanFailureMessage=false | Authored NPC spawns now emit equipment prose before placement/FOV. This is existing API behavior newly reached by real content; capture its actual impact and add RED before changing it |
| New Loadout only affects possessions | Gameplay/World/Generation/LootDropSystem.cs:119-126 | Loadout also selects the Humanoid death-loot class unless an explicit tag overrides it. All 24 currently lack that tag |
| New blueprints automatically backfill existing saved NPCs | SaveSystem.cs:826 loads stored entity bodies; load hooks at :241 do not fire ObjectCreated | Fresh entities receive the kit. Existing saved actors retain their exact inventory/equipment and do not regrant on load |
| Current sprites are gear-specific or visible worn overlays | EnvironmentSpriteRenderer.cs:1732,1751 | Current art is generic dropped blade/cuirass. New ground families do not add worn equipment layering |
| Copy any existing item metadata for new art | item_armor.png.meta:49,97-109 is Multiple with cropped10x9 rect; SpriteImportPostprocessor.cs:32-60 leaves Environment mode alone | Author explicit Single metadata with a fresh32hex GUID; use the verified Single template settings, not armor's cropped slice |

## 4. Proposed 24-blueprint roster and surgical JSON boundaries

Append one `{"Name":"Loadout","Params":[...]}` to each listed object's existing Parts array. Include **all three string keys** on every row: empty Carry/Pick are literal `""`, not omitted. For four Snapjaw descendants, explicit empty fields prevent parent settings from surviving future edits. Do not globally reserialize Objects.json; preserve all unrelated field ordering/formatting and concurrent hunks.

The Name/Parts-end anchors below are the current source locations in Assets/Resources/Content/Blueprints/Objects.json. `Mass` is the maximum extra carried+equipped weight, including optional pieces; it is not the entire NPC's inventory. `Value` is maximum additional Commerce.Value, not guaranteed sell proceeds or a calibrated economy result. Tier is the blueprint tag, not an exclusive zone restriction.

| Blueprint | Tier | Name / Parts-end lines | Equip | Carry | Pick | Mass | Value |
| --- | ---: | --- | --- | --- | --- | ---: | ---: |
| Snapjaw | 1 | 597 / 643 | Dagger;LeatherCap:20 | "" | "" | 5 | 18 |
| SnapjawScavenger | 2 | 1224 / 1257 | LeatherGloves:35 | "" | 1;Dagger;ShortSword | 6 | 23 |
| SnapjawHunter | 2 | 1282 / 1324 | Spear;LeatherBoots:35 | "" | "" | 10 | 30 |
| SnapjawChieftain | 2 | 14510 / 14543 | ShortSword;LeatherArmor;LeatherCap | "" | "" | 21 | 53 |
| SnapjawWarlord | 3 | 14580 / 14613 | LeatherArmor;IronHelmet;IronshodBoots | "" | "" | 25 | 83 |
| DesertBandit | 1 | 13394 / 13427 | ShortSword;LeatherCap:30 | "" | "" | 6 | 23 |
| RuinScavenger | 1 | 13964 / 13997 | Dagger;LeatherGloves:35 | "" | "" | 5 | 18 |
| SkeletalSentry | 2 | 14036 / 14128 | IronHelmet | "" | "" | 6 | 25 |
| AmbushBandit | 1 | 23005 / 23089 | ShortSword;LeatherArmor:35 | "" | "" | 20 | 45 |
| RuneCultist | 2 | 23549 / 23603 | Dagger;LeatherGloves:35 | "" | "" | 5 | 18 |
| Warden | 2 | 17169 / 17262 | LongSword;LeatherArmor;LeatherBoots | "" | "" | 26 | 67 |
| Quartermaster | 1 | 9197 / 9286 | Spear;LeatherArmor;LeatherBoots | "" | "" | 25 | 60 |
| Weaponsmith | 1 | 7941 / 7996 | LeatherGloves;LeatherBoots | "" | "" | 4 | 20 |
| Tinker | 1 | 8932 / 9034 | Dagger;LeatherGloves | "" | "" | 5 | 18 |
| Farmer | 1 | 19654 / 19722 | LeatherBoots;LeatherGloves | "" | "" | 4 | 20 |
| WellKeeper | 1 | 19583 / 19651 | LeatherBoots;LeatherCap | "" | "" | 4 | 20 |
| PeatCutter | 1 | 29302 / 29321 | LeatherBoots;LeatherGloves | Dagger | "" | 8 | 30 |
| Elder | 1 | 5803 / 5927 | LeatherCap;LeatherBoots | "" | "" | 4 | 20 |
| Scribe | 1 | 20624 / 20739 | LeatherGloves;LeatherCap | "" | "" | 2 | 16 |
| Merchant | 1 | 9073 / 9162 | ShortSword;LeatherBoots | "" | "" | 8 | 27 |
| TentRightHost | 1 | 28428 / 28453 | LeatherBoots;LeatherCap | Dagger | "" | 8 | 30 |
| SaltMaster | 1 | 28526 / 28559 | LeatherGloves;LeatherBoots | "" | "" | 4 | 20 |
| RecensionScribe | 1 | 29242 / 29261 | LeatherGloves;LeatherBoots | "" | "" | 4 | 20 |
| CurationSorter | 1 | 29272 / 29291 | LeatherGloves;LeatherCap | "" | "" | 2 | 16 |

Example literal payload for the Scavenger:

```json
{
  "Name": "Loadout",
  "Params": [
    { "Key": "Equip", "Value": "LeatherGloves:35" },
    { "Key": "Carry", "Value": "" },
    { "Key": "Pick", "Value": "1;Dagger;ShortSword" }
  ]
}
```

Role intent: hostile bands get modest one-hand weapons and optional small armor; rare leaders get reliable armor. Warlord and SkeletalSentry keep their existing stronger natural weapon definitions. Guards/rental staff gain actual defense. Artisans/workers get practical light gear, not universal swords. Neutral portable goods remain governed by current theft/trade/death/reputation rules; this phase does not invent personal-item ownership locks.

## 5. Independent inheritance, order, stock and capacity review

The complete current Objects.json graph was read and a read-only model of BlueprintLoader.Bake's ordered merge was applied to these proposed appends. Result:

- Snapjaw is the only selected parent. Its only direct descendants are SnapjawScavenger, SnapjawHunter, SnapjawChieftain and SnapjawWarlord. Those have no descendants and no Trader. The inherited Loadout would remain at zero-based effective Part index12 for all five. This is safe for the current graph, not a promise about future Snapjaw trader subclasses.
- All other selected IDs have no descendants. No unselected blueprint would acquire Loadout through this proposal. Do not promote the common gear to Villager or Creature merely to reduce JSON repetition.
- All **nine** selected Trader actors retain Trader before Loadout: Warden15<16, Quartermaster13<14, Weaponsmith13<14, Tinker14<15, Farmer13<16, WellKeeper13<15, Elder15<16, Scribe15<16, Merchant13<14. Farmer/WellKeeper demonstrate why raw JSON last-part inspection alone is insufficient: inherited Trader retains its original effective position before child AI.
- Future subclass warning: a new Trader on a Snapjaw descendant would come after inherited Loadout. Merely overriding Loadout params at the bottom would not move the Part. A future producer must arrange creation order deliberately or get a separate narrowly tested authoring solution.

Opening-stock overlap audit uses Assets/Resources/Content/Data/Loot/LootTables.json:493 WeaponsmithStock, :1961 FarmerStock, :2017 WellKeeperStock, :2063 ScribeStock, :2090 TinkerStock, :2115 WardenStock, :2142 ElderStock, :2163 MerchantStock, :2206 QuartermasterStock. ComponentAny and ReagentCommon references contain no proposed personal weapon/armor collision for these kits.

- Warden kit avoids its shelf Spear, IronHelmet and Buckler; uses LongSword/body armor/boots instead.
- Quartermaster kit avoids its shelf LongSword, ChainMail, IronHelmet and Buckler. Ordinary Spear differs by BlueprintName from later LoanerSpear, which is not a personal loadout grant.
- Weaponsmith receives only gloves/boots; both are absent from WeaponsmithStock. Merchant uses ShortSword rather than its stock Dagger and does not wear its stock LeatherArmor.
- ArmorerStock:551 contains every verified sprite-backed armor family. Exclude Armorer from this content-only roster. A later solution must preserve shelf stock AND equip an actual distinct personal unit without bypassing GA03g's stacked-unit refusal; writing Armor before Trader is not a valid fix.
- Successful equipment removes the item from Inventory.Objects. Inventory.GetCarriedWeight:428 includes equipped aliases once; TraderRestockSystem:76 counts only Objects; AddObjectCore merges only Objects. Equipped kits therefore do not starve normal shelf restocking or merge with later stock.
- Non-Trader PeatCutter/Host carried Daggers remain ordinary carried objects; the new content does not add Drams, TraderPart or recurring stock to them.
- Conservative capacity ceilings are safe: Weaponsmith opening stock upper bound86 +kit4=90. Quartermaster's deliberately loose stock bound65 +kit25 +loaners19=109. Warden25+26=51. Merchant's loose stock bound38 +kit8 +six repair-stock units6=52. Loose bounds sum all nested possibilities even when PickOne chooses just one, so do not describe them as observed stock totals. None requires increasing MaxWeight150 or changing stocking behavior.

Loadout runs Equip, then Pick, then Carry (LoadoutPart:101-110). All guaranteed equipment is a single unit and uses distinct slots. No two-hand weapons, offhand shields, random consumable counts, or paid modifications are required in the content phase. Preserve current refusal behavior for synthetic full-pack/occupied-slot/multistack controls.

### 5.1 Required quiet factory-spawn equipment publication

Root accepted this as part of the content wave, with RED required before any runtime edit. The source-proved trigger is actual factory creation of a Loadout actor: Factory.CreateEntity fires ObjectCreated before returning/zone placement, Loadout.GrantOne calls the public AutoEquip facade, and EquipCommand.cs:174 unconditionally writes `<actor> equips <item>.` to MessageLog. The existing `emitPlanFailureMessage:false` argument affects only plan refusal prose. InventoryCommandExecutor itself does not emit that success prose. Thus the stock/gear integration would add offscreen spawn announcements even though the lifecycle was already correct as a supported API.

**Recommended narrow API shape, all options transient and internal except existing public entrypoints:**

```csharp
// InventorySystem.cs:167 — retain existing public signature/behavior.
public static bool AutoEquip(Entity actor, Entity item)
    => AutoEquip(actor, item, emitSuccessMessage: true);

internal static bool AutoEquip(Entity actor, Entity item, bool emitSuccessMessage)
{
    var result = ExecuteCommand(new AutoEquipCommand(item, emitSuccessMessage), actor);
    return result.Success;
}

// AutoEquipCommand.cs:9-16 — preserve public constructor callers.
private readonly bool _emitSuccessMessage;
public AutoEquipCommand(Entity item) : this(item, emitSuccessMessage: true) { }
internal AutoEquipCommand(Entity item, bool emitSuccessMessage)
{
    _item = item;
    _emitSuccessMessage = emitSuccessMessage;
}

// EquipCommand.ExecuteInternal:80 — append one default-true internal option.
// Existing arguments remain unchanged; both AutoEquip branches pass their flag.
// ..., bool emitPlanFailureMessage, bool emitSuccessMessage = true)

// At the current unconditional success message, preserving surrounding order:
if (emitSuccessMessage)
    MessageLog.Add($"{actor.GetDisplayName()} equips {itemToEquip.GetDisplayName()}.");

// LoadoutPart.GrantOne:186 — only this caller opts out.
bool completed = hasBody && InventorySystem.AutoEquip(
    ParentEntity, item, emitSuccessMessage: false);
```

Propagate `_emitSuccessMessage` through **both** AutoEquipCommand calls to EquipCommand.ExecuteInternal (current body path:107 and legacy path:123), alongside `allowDisplacements:false, emitPlanFailureMessage:false`. The ordinary EquipCommand.Execute call at:71 uses default true. No InventoryContext field, global silence flag, saved Part parameter, new public boolean overload, or MessageLog callback suppression is necessary. All command validation/storage/undo/EquipBonusUtility/AfterEquip/enhancement calls retain their exact order. LoadoutEquipResult remains emitted after the command with the same actor/target/reason payload.

Caller sweep: production LoadoutPart:186 is the only facade caller needing silence. PickupCommand.cs:166 uses `new AutoEquipCommand(_item)` directly; its default must remain audible. Public InventorySystem.AutoEquip and the public command constructor preserve ordinary player/API messages. The two existing GA03g native re-equip calls also stay audible. No namespace-wide or non-Player heuristic should suppress normal NPC/player commands: the source of the command, not actor tags or FOV state, is the narrow distinction.

**Initial RED/counter fixtures (proposed):**

1. Before JSON integration, use a fresh local factory and an explicitly labeled supported-API fixture blueprint with Loadout Equip IronshodBoots. Capture MessageLog callback/history after setup. Creation must equip the real item, apply Speed penalty5, fire BeforeEquip/AfterEquip, retain LoadoutEquipResult, and emit no automatic equip-success prose. Current source should fail only the last expectation; do not claim this fixture proves current authored world reach.
2. Identical setup with Pick `1;IronshodBoots` exercises the same quiet grant path; Carry produces no equip and no success prose.
3. Ordinary InventorySystem.Equip and public AutoEquip on a real carried singleton still emit the expected message and effects. Test the public AutoEquipCommand constructor directly through the normal executor as another default-true control if needed; avoid relying on cross-test-assembly access to the new internal constructor.
4. Actual PickupCommand auto-equip remains audible; it must not inherit a prior loadout's silence.
5. During a quiet grant's one-shot BeforeEquip probe, perform a separately committed normal player equip of a different owned item. The inner message remains, the outer automatic-spawn message is absent, and both ownership/bonus outcomes are correct. This catches a tempting global-mute implementation.
6. Quiet grant veto/refusal retains successfully granted gear carried, emits LoadoutEquipResult with its existing reason, applies no bonuses, and does not print a success. No test should demand silence from arbitrary custom event handlers: only the automatic command success announcement is suppressed.
7. Once the24 JSON entries are authored, repeat the no-announcement assertion through actual Warden/Quartermaster creation with real stock and through a real village build. These are the shipped-producer controls, independent of the initial synthetic authoring fixture.

If implemented differently, record why and keep the same boundaries. In particular, do not solve this by muting all MessageLog output around factory creation: it would hide independent callbacks, diagnostics conveyed through other prose, and legitimate ordinary nested commands.

## 6. Source attack and defense ledger

Natural definitions are in Gameplay/Anatomy/NaturalWeaponFactory.cs:15-26, :51-54, :78-91, :98-100. EntityFactory.InitializeAnatomy:451-507 defaults to Humanoid and applies NaturalWeapon to hand defaults. The selected mobs' existing parts are not removed or rewritten.

| Proposed affected actor(s) | Existing natural hand | Equipped hand proposal | Deliberate effect |
| --- | --- | --- | --- |
| Snapjaw, RuinScavenger, RuneCultist | 1d4/Pen1 Cutting (Animal on claws) | Dagger1d4/Pen1 Piercing | Same base dice/pen, different universal rider: bleed becomes confusion on that hand |
| SnapjawScavenger | 1d4/Pen1 Cutting Animal | Dagger as above OR ShortSword1d6/Pen1 Cutting LongBlades | Dagger changes rider; sword increases dice while retaining Cutting. Actual choice must be tested |
| SnapjawHunter | 1d6/Pen2 Cutting Animal | Spear1d6+1/Pen2 Piercing | +1 base damage and bleed-to-confusion class change |
| SnapjawChieftain | 1d4/Pen1 Cutting Animal | ShortSword1d6/Pen1 Cutting LongBlades | More base damage; retains Cutting rider |
| DesertBandit, AmbushBandit | BanditBlade1d6/Pen1 Cutting | ShortSword1d6/Pen1 Cutting LongBlades | Base dice/pen/class rider retained; adds LongBlades attribute |
| Warden | DefaultFist1d2/Pen0 Bludgeoning Unarmed | LongSword1d8/Pen2 Cutting LongBlades | Significant defensive-combat upgrade; stun-class becomes bleed-class on equipped hand |
| Quartermaster | DefaultFist1d2/Pen0 Bludgeoning Unarmed | Spear1d6+1/Pen2 Piercing | Stronger defense and confusion-class |
| Tinker | DefaultFist1d2/Pen0 Bludgeoning Unarmed | Dagger1d4/Pen1 Piercing | Small blade defense and confusion-class |
| Merchant | DefaultFist1d2/Pen0 Bludgeoning Unarmed | ShortSword1d6/Pen1 Cutting LongBlades | Travel defense and bleed-class |
| Warlord | WarlordCleaver2d5/Pen2 Cutting Axe | No equipped weapon | Preserve stronger current cleaver and Axe attributes |
| SkeletalSentry | BoneBlade1d6+1/Pen1 Cutting | No equipped weapon | Preserve blade rather than accidentally weaken it with a short sword |
| Other 10 selected NPCs: Weaponsmith, Farmer, WellKeeper, PeatCutter, Elder, Scribe, TentRightHost, SaltMaster, RecensionScribe, CurationSorter | Their existing hand defaults | Armor only; PeatCutter/Host Daggers are carried | No planned direct attack replacement |

MeleeWeaponPart.MaxStrengthBonus defaults to -1; Dagger authors3. StatUtils.GetModifier is floor((score-16)/2), not a D&D16=+3 rule. The cap is not binding on the current selected Dagger users' authored Strength; preserve it instead of rewriting the item. Do not calculate expected full HP loss from dice alone: defense, penetration count, crit and class effects remain active.

Recommended item mechanics and anchors in Objects.json:

| Item | Definition line | Slot / attributes | Weight | Commerce.Value |
| --- | ---: | --- | ---: | ---: |
| Dagger | 291 | Hand;1d4 Pen1;Piercing;Strength cap3 | 4 | 10 |
| ShortSword | 5970 | Hand;1d6 Pen1;Cutting LongBlades | 5 | 15 |
| LongSword | 5385 | Hand;1d8 Pen2;Cutting LongBlades | 8 | 25 |
| Spear | 6214 | Hand;1d6+1 Pen2;Piercing | 7 | 18 |
| LeatherArmor | 4142 | Body;AV3,DV-1 | 15 | 30 |
| LeatherCap | 6942 | Head;AV1,DV0 | 1 | 8 |
| LeatherGloves | 6857 | Handwear;AV1,DV0 | 1 | 8 |
| LeatherBoots | 6772 | Feet;AV1,DV0 | 3 | 12 |
| IronHelmet | 6687 | Head;AV2,DV0 | 6 | 25 |
| IronshodBoots | 7027 | Feet;AV2,DV0;SpeedPenalty5 | 4 | 28 |

All weapons use OneHand handling; all selected armor aside from IronshodBoots has SpeedPenalty0. No CarryMovePenalty, EquipBonuses, rarity field, new save field, or enhancement is authored in this phase.

Hit-location examples (natural armor remains): Warden Body6/Feet4; Quartermaster Body5/Feet3; Chieftain Body7/Head5; Warlord Body7/Head6/Feet6; Sentry Head7. Other unarmored locations retain natural AV. LeatherArmor gives global DV-1 whenever present; Warlord boots add Speed.Penalty5. Do not assert these numbers through summed GetAV as uniform body protection.

## 7. Actual ordinary-world producer evidence

| IDs | Current producer and runtime consumer |
| --- | --- |
| Snapjaw/Scavenger/Hunter | Data/Tables/PopulationTable.cs:287,310-335 Spread; :425-427 CaveT1; GetBiomeTable:206 routes actual biome/tier populations. CaveT1 already includes blueprint-tier2 variants, so gear tiers cannot rely on late-zone exclusivity |
| Chieftain | Gameplay/World/Generation/WorldGenerator.cs:105-128 authors lair POIs; GetBossForBiome:179-187 chooses Chieftain for Cave/default; Builders/LairBuilder.cs:102 creates the boss; Map/OverworldZoneManager.cs:762 installs the lair population route |
| Warlord | PopulationTable.cs:770 CaveT3; LandmarkBuilder.cs:337 warband camp marker |
| DesertBandit | PopulationTable.cs:582 DesertT1, :686 T2, :796 T3; OverworldZoneManager.cs:375 uses biome population |
| RuinScavenger / SkeletalSentry | PopulationTable.cs:632 RuinsT1; :732-734 RuinsT2; :853 RuinsT3. OverworldZoneManager.cs:401-405 installs Ruins landmark/population builders |
| AmbushBandit | LandmarkBuilder.cs:449-466 BanditDugout, MinTier2; LairPopulationBuilder.cs:108 additional biome ambusher route. Its blueprint Tier1 is not a guarantee of a T1 dugout |
| RuneCultist | LandmarkBuilder.cs:1045-1063 Ruins rune-cult dig site; real Ruins catalog is returned by StampCatalog.For:72 |
| Warden, Quartermaster, Tinker, Farmer, WellKeeper, Elder, Scribe, Merchant | VillagePopulationBuilder.cs:104-146. Elder/Merchant/Quartermaster/Scribe are routine roles; Tinker70%; Warden guaranteed with lantern site otherwise60%; Farmer/WellKeeper when mainWell exists. OverworldZoneManager.cs:738 installs builder |
| Weaponsmith | LandmarkBuilder.cs:146 TownStamps shop:Weaponsmith:WeaponsmithStock; OverworldZoneManager.cs:717 installs starting-town shops; LandmarkBuilder.cs:1335-1366 performs actual factory creation/stock handling |
| TentRightHost / SaltMaster | WorldMapAuthoring.cs:219-220 names Wellmeet(8,16) and First Tent(5,17) profiles; OverworldZoneManager.cs:669-674 dispatches TentRightProfileCamp; LandmarkBuilder.cs:745-762 spawns Host and SaltMaster. Ambient Host also appears in Beating's TentRightCamp |
| PeatCutter | WorldMapAuthoring.cs:222 Sumphold(15,6), profileBoatyard; OverworldZoneManager.cs:689-690; LandmarkBuilder.cs:949-966 SumpholdBoatyard |
| RecensionScribe / CurationSorter | WorldMapAuthoring.cs:223 Drowned Ledger(17,5), profileExcavationCamp; OverworldZoneManager.cs:682-683; LandmarkBuilder.cs:892-916 ExcavationCamp |

This is source-proved eligibility/producer connectivity, not a claim that a single random world has every spawn. Stump's actual authored spine replaces older generic population paths; do not cite the stale default Cave alias as a new Stump warband source. Undertaker has a blueprint but no verified ordinary producer in this sweep, so it is omitted.

## 8. Bounded sprite improvement plan

### Existing seam and exact registration paths

Production file: Assets/Scripts/Presentation/Rendering/EnvironmentSpriteRenderer.cs.

- SpriteRoot:569 is `Sprites/Environment/`.
- `_itemBodyTiles`:287 is the existing dictionary; BuildTiles:882-889 loads each body through LoadSingle:751 (`Resources.Load<Sprite>`) and makes its Tile.
- ChooseTile:1622-1629 checks ResolveItemBody before generic slash weapon:1732 and bracket armor:1751. `authoredColor` stays false so the existing glyph-color copy supplies tint/light state.
- Public ResolveItemBody:1822 currently handles tonic/book/gem/seed/etc. Add exact blueprint cases there; do not use `EndsWith("Boots")` or `Contains("Sword")` across all content without reviewing the full matching domain.
- New PNG/metadata path is Assets/Resources/Sprites/Environment/<body>.png(.meta). Exact resource keys below omit path/extension. These proposed assets are currently absent.

| Proposed new body key | Exact initial blueprint mapping | Current wrong/generic state |
| --- | --- | --- |
| item_dagger | Dagger | generic slash blade |
| item_sword | ShortSword, LongSword | same slash blade as dagger/spear |
| item_spear | Spear | generic slash blade |
| item_boots | LeatherBoots, IronshodBoots | generic cuirass |
| item_gloves | LeatherGloves | generic cuirass |
| item_helmet | LeatherCap, IronHelmet | generic cuirass |
| item_mace | Mace | wrong vial, because RenderString! falls through to item_vial |

This is **six legibility families plus one wrong-identity correction**, not seven item-specific portraits. Short/long swords deliberately share a readable sword silhouette; leather/iron variants use current tints. LeatherArmor already has suitable body-armor silhouette and remains item_armor. No claim is made that current color alone fully depicts materials. If root elects distinct variants, enumerate and test them before expanding the asset batch.

Optional extension, separate explicit asset/RED scope: `item_axe`→Hatchet/Battleaxe; `item_hammer`→Warhammer; `item_shield`→Buckler (review IronBuckler separately); `item_cloak`→Cloak (review WardedCloak separately). Existing glyphsP/T/)/( otherwise lack these identities. Do not map Warhammer to a tree or Mace to a tonic. ForgedWeapon, elemental named weapons, shields with different silhouettes and rental aliases remain generic until explicitly mapped; new gear should not be silently swept into broad suffix rules.

### Art/import contract

Use the existing small pixel-art ground-item style: transparent background, clean readable silhouette at native tile size, neutral values compatible with current tint and lighting, no labels or background tile. Dagger must be short blade, sword longer blade/hilt, spear visible shaft/head, boots a boot/pair, gloves a hand garment, helmet a head garment, mace a blunt head/shaft. These are proposed visual requirements, not inspected new assets.

Use explicit Single sprite import,16 pixels per unit, centered pivot, point filtering, no mipmaps, uncompressed, Clamp, transparency. Assets/Editor/SpriteImportPostprocessor.cs:32-60 sets pixel settings but only forces Single in SpellFx; it does not repair Environment sprite mode. Follow a verified Single metadata shape such as weapon_ground.png.meta while generating a **new unique32hex GUID**. Do not copy item_armor.png.meta: it is Multiple with a cropped10x9 sprite at(3,3), which would clip new full-frame art. Validate texture dimensions/alpha and rendered appearance after import; do not claim metadata alone proves visual quality.

### Missing-asset, precedence and lifecycle gates

Today a recognized item-body key whose asset is unavailable falls through to glyph-family mapping. For new equipment this can resurrect a wrong identity (Mace→vial) or defeat the distinct-family promise (boots→cuirass). Add a RED for recognized equipment with missing dictionary registration/asset and use the narrow minimum that preserves its own CP437 glyph rather than a different family. Avoid silently changing fallback for every unrelated existing item-body type without counterpart tests.

Keep existing actor/fixture precedence, fog/visibility, foreground tint, flipped coordinate conventions, claim restoration and incremental dirty-cell handling. Static mapping changes require no new gameplay dirty hooks and no per-frame collection creation. Do not restore an old glyph into a cell that now contains a different top entity. A gear item hidden under a live NPC should not paint over that NPC; examine/drops can reveal it only through normal top-entity selection.

## 9. Initial RED and dedicated adversarial gates — proposed, not run

Read CLAUDE.md and ADVERSARIAL_TESTING.md before authoring; retain initial compile mistakes separately from real behavioral RED. Use actual shipped JSON for the positive content cases. Do not insert fixture Loadout into the positive actor and then claim the shipped producer is connected.

### Content initial RED/control target

- Twenty-four parameterized creation cases, one per proposed ID, asserting the exact literal authored fields and actual positive equipped/carried entities after Factory.CreateEntity. Use a deterministic RNG implementing Next(max) and Next(min,max) within valid ranges; do not hardcode an out-of-range roll for the two-entry Pick.
- Separate integration assertions for initial Trader stock retention, Snapjaw descendant override/order, Warlord location/Speed effects, Chieftain hit-location armor, positive natural attack preservation, and ordinary producer creation. Some controls will already pass before JSON changes; record actual counts after running rather than treating this list as results.
- Only after recorded RED, append the24 Parts. Run content cases plus current Loadout lifecycle, equipment, inventory, world-generation, trade/stock, save and combat neighbors. Any tests/bench notes stating no current authored Loadout must be reconciled as historical fixture bounds; do not weaken their actual lifecycle assertions.

### Thirty-three bounded adversarial cases/groups

1. All optional pieces forced present; exact quantity1, slots, owners and maximum kit weight.
2. Optional pieces forced absent; guaranteed main weapon/armor remains and nonexistent optional object is not counted.
3. Both valid Scavenger Pick positions produce one weapon, never both; no parent Dagger residue.
4. All four Snapjaw descendants override Equip/Carry/Pick completely; no inherited optional cap leaks into Hunter/Warlord.
5. Effective Part ordering for all nine Trader candidates is Trader-before-Loadout, with real stock still present.
6. Full inheritance census: exactly the24 selected IDs gain the Part, no beast/frog/baseVillager/legacy descendant leakage.
7. Forced maximal Weaponsmith stock plus gloves/boots fits capacity and both equip; no opening stock was suppressed.
8. Forced maximal Quartermaster stock plus three village Loaners; personal Spear is a separate blueprint and loaners remain rentable/singleton.
9. Warden stock Spear/Helmet/Buckler remains carried while distinct personal LongSword/armor/boots equip.
10. Merchant stock Dagger/body armor remains shelf stock; personal ShortSword/boots equip.
11. Armorer overlap synthetic counterexample pins existing merge/refusal boundary; do not author it silently and report success.
12. Successful equipment absent from raw Objects; later restock can create the same ordinary blueprint on shelf without merging into equipped unit.
13. Carry-only PeatCutter/Host Dagger has owner carriage and is not in any body slot; their natural attack remains.
14. Chieftain Body AV7/Head5 and unarmored limb natural AV4; summed GetAV cannot satisfy this test accidentally.
15. Warlord Body7/Head6/Feet6, DV-1 and Speed penalty5; bare comparison retains natural AV4 and no slowdown.
16. Warlord boots unequip/re-equip removes/reapplies exactly5, preserving an unrelated penalty.
17. Boots-bearing actor foot dismemberment drops the exact item and removes applicable bonus once; other-limb cut does not.
18. Dismember veto preserves the exact equipped item, aliases and bonuses.
19. Actual melee uses Dagger's Piercing on selected hand; no accidental Cutting attribute inherited from claw.
20. Bandit ShortSword retains1d6/Pen1 Cutting; surviving offhand default remains identifiable.
21. Hunter Spear1d6+1/Pen2 and Piercing reaches actual damage dispatch; control natural Hunter claw remains Cutting.
22. Warlord2d5/Pen2 Cutting Axe and Sentry1d6+1/Pen1 Cutting remain actual gathered weapons despite armor.
23. Dagger cap3 versus natural uncapped and correct StatUtils(score-16)/2 assumptions; no fabricated numeric damage total.
24. Ordinary death deposits exact owned equipment and carry quantities once at the death cell; no natural weapon drop.
25. Repeated death call cannot duplicate gear or arbitrary death-loot roll.
26. Synthetic Temporary and NoDropOnDeath controls suppress owned gear and class loot; normal control drops it.
27. Loadout-derived Humanoid death table is deliberate; explicit LootClass/LootTable control still wins.
28. Full GameSessionState save/load preserves body/map/Physics aliases, item IDs, quantities and Speed, with no ObjectCreated regrant.
29. Older serialized actor without Loadout remains as saved after decoding against the updated factory; do not backfill unseen gear.
30. Save/load followed by death/unequip cleans each applicable contribution once and does not restock personal kit.
31. Actual starting village and selected named-place profile/stamp construction yields the intended IDs with actual Loadout; absent-POI/failed-chance controls are not reinterpreted as success.
32. Real Resources item sprites/renderer claims exist for every selected family; no glyph-only or wrong-vial claim masquerades as completed art.
33. Required quiet factory-spawn announcement contract and the ordinary/nested/pickup controls in section 5.1. Capture current-source RED before the narrow option; retain LoadoutEquipResult, equipment hooks and normal messages. This is a source-proved newly reachable integration issue; actual RED/native outcomes still need recording.

These are bounded group targets; parameterization may change executable case counts. Keep assertions about exact resident references and quantities, not only `GetPart<LoadoutPart>() != null` or display strings.

### Sprite initial RED/control and adversarial target

Before art/renderer edits, write actual-family mapping and harness assertions for the seven new keys. Current generic blade/cuirass/vial behavior must produce genuine RED. Asset-absence checks are expected RED for assets that do not yet exist; distinguish them from runtime bugs.

Use20 bounded counter/adversarial scenarios: (1-7) each proposed family reaches a real imported Sprite/Tile; (8) dagger differs from sword and spear; (9) boots/gloves/helmet differ from torso armor; (10) Mace never becomes a vial with art present; (11) missing Mace equipment asset retains glyph; (12) missing boots asset does not falsely prove distinct boots; (13) unrelated FireTonic still yields tinted vial; (14) Chest and Lantern preserve fixture precedence; (15) foreground color/tinted dim-light comparison; (16) hidden item in remembered fog is not revealed; (17) stationary item replacement clears old claim; (18) removal exposes correct underlying glyph/entity; (19) incremental change preserves distant claims; (20) unmapped forged/unknown blueprint retains documented fallback and does not gain equipment art by a loose suffix match. Add per-extension cases only if extension assets ship.

Useful existing harness anchors: Assets/Tests/EditMode/Presentation/Rendering/EnvironmentSpriteRendererHarnessTests.cs:26 setup; :911 FireTonic_BecomesARedTintedVial; :761/:789 dirty-neighborhood tests; :980 ResolveItemBody_FamilyPins. Tests use actual Resources and tilemaps, with tile y=Zone.Height-1-zoneY. Restore original static hooks and owned objects; do not clear production registries permanently.

### Fixture/isolation requirements

- Fresh EntityFactory loaded from actual Objects.json; set and restore old LoadoutPart.Factory/Rng, TraderPart.Factory/Rng and LootDropSystem/Corpse factories. Initialize and restore the actual LootTableRegistry when stock/death tables are under test. Do not use null TraderPart.Factory to prove a no-collision merchant kit.
- Existing Assets/Tests/EditMode/Gameplay/Anatomy/GameAuditEquipmentLifecycleTests.cs:13 EquipmentLifecycleFixture and Gameplay/Entities/GameAuditLoadoutLifecycleTests.cs:13 LoadoutLifecycleFixture provide reusable body/equipment checks and global-scope patterns. Use their isolation ideas without retaining the old fixture-only blueprint as a positive content test.
- Preserve MessageLog callback/history, diagnostics filters/state, EquipmentChangeBus.GlobalVersion, EntityVisualHooks/ZoneRenderHooks, reputation/current settlement/runtime aliases and TurnManager.Active wherever a chosen fixture mutates them. Full-save controls should use the existing HotbarSaveFixture/GameSessionState graph patterns.
- Fixtures that override AI, HP, RNG, suppress secondary loot, add a veto observer or make a zone deterministic must label those stimuli. No synthetic override should secretly create the very content link being tested.
- No new saved fields, enum values or external API are required. Fixed mapping tables are runtime presentation state only.

## 10. Native and performance gates

### Content native gate

Author a new self-auditing scenario/driver/batch only after missing-arena RED and root's source review. Reuse current Assets/Scripts/Scenarios/Custom/GameAuditLoadoutLifecycleBench{,Player}.cs, GameAuditEquipmentLifecycleBench{,Player}.cs and Assets/Editor/Scenarios/NativeSaveIsolation.cs patterns; preserve owned save-root cleanup and batch failure exit semantics. The old GA03g bench remains evidence of supported API fixtures, not proof that these new producers existed then.

Proposed finite observed route: actual New Game creates initial town with equipped Warden/Quartermaster/Weaponsmith; inspect exact world actors and shelf/personal separation; in a separate explicitly staged combat lane create an actual authored bandit and warlord through the unmodified factory; use ordinary movement attack/pickup/inventory commands for one death→drop→pickup→equip loop; verify a deterministic API combat control separately for class/attack preservation; F5/F6 reload exact equipment; recovery load does not regrant; actual faction profile creates Host/Sorter gear where practical. Use source-verified existing input driver conventions when authoring, not guessed popup indices. Do not claim a custom arena is random encounter-frequency testing.

Record exact items/IDs/quantities, body slots, stat deltas, stock, death cell, diagnostics and before/after save aliases. Expected 16-24 substantive groups are adequate; final count comes from the authored driver, never from this proposal. Capture before/after screenshots only as native verification, and explicitly distinguish script-observable mechanics from visual conclusions.

### Sprite native/visual gate

Use a readable separated ground-item display with actual gear entities, not manually supplied fake sprites. Check each selected family under normal and dim light, next to a tonic/chest and after pickup/removal/replacement. Confirm actual rendered silhouettes, transparency, tint, alignment and intact pixel edges at the game's normal scale. A script checking a tile name cannot prove the asset is good or even recognizable; inspect captures/visible output. This phase proves ground-item art only, not NPC equipment overlays.

### Performance

Content adds finite factory/equipment work at spawn and can increase ordinary combat/drop volume; it introduces no new Update loop. Keep kit counts bounded and use normal equipment caches. Do not optimize lifecycle code without a measured problem.

Sprite resolver changes touch a per-cell path. Follow Docs/PERF-FOUNDATION.md: preloaded body tiles, exact switches/dictionary lookups, no per-frame/turn collections, no broad full-zone dirty hook, retain current incremental claim logic. Record a comparable60-90s (75s preferred) native baseline before renderer changes and after: populated-zone idle, walking/redraw and ground-item pickup/drop; retain frame/main-thread/GC p99 and max plus environment-renderer work metrics. Do not infer a speedup from asset import success or a short unit test. Root owns all runs; none has been performed for this proposal.

## 11. Completion and reporting checklist

- Root copies/reconciles this proposal to living Docs before implementation, preserving the distinction between historical and new producer coverage.
- Record corrected RED, minimum GREEN, dedicated adversarial results, independent cold-eye findings, native/visual evidence and performance comparison where applicable.
- Resolve any yellow-or-higher introduced behavior before commit; document deliberately scoped stock/art aliases, generic fallback, old-save behavior, balance changes and independent debts.
- Content diff is limited to the 24 explicit Parts insertions, the four-file transient quiet-success option in LoadoutPart/InventorySystem/AutoEquipCommand/EquipCommand, and necessary tests/docs/scenario files. Art diff is its explicit asset/metadata set and narrow renderer registration/resolution/tests; no unrelated spell FX edits or broad Objects.json formatting.
- State actual final scope. Seven ground families do not mean seven unique item portraits, a roster of24 does not mean every world spawns24 types, and native API checks do not mean ordinary keyboard play for every branch.

## Root verification corrections before code

The preparation cited Docs/GA03g-REPORT.md; the actual proof path is
Docs/Verification/GameSystemAudit/GA03g-REPORT.md, corrected above. New art must
be16x16 with binary alpha and shared outline(30,32,28); metadata copies a
verified Single template and changes only its GUID, per the standing user rule.
The two phases keep content claims separate from later ground-art claims.
Root read actual Loadout/AutoEquip/Equip/Inventory facade, ordered Blueprint
merge, anatomy/default weapons, typed loot registry and existing fixture scopes.
Source-backed producer/reference checks continue before their respective tests.


## Root implementation log — 2026-09-06

- Confirmed initial RED: 34 cases, 27 failures/7 passing, zero compiler errors. The 24 actual blueprint cases had no Loadout; Equip/Pick and nested creation leaked success prose. Evidence: GA03i-red.xml.gz.
- Authored exactly 24 appended Loadout Parts using surgical string splices. Parsed JSON and removed only the new rows in memory to compare the complete document semantically with the original: equal. No existing actor/item fields changed. Added the internal per-command success flag without changing public defaults, hooks, bonuses, diagnostics or transactions. Separate fixture RNGs and registry contents are restored.
- A guarded edit script stopped on an ambiguous signature match. An accidentally launched test compile on the partial change failed CS1739; its log is retained as GA03i-incomplete-edit-compile.log.gz, and no stale XML was trusted. The exact signature was completed. Initial34 then passed, zero compiler errors.
- Added the dedicated40 equipment cases. Focused74 returned66PASS/8FAIL, exposing truly missing natural weapons. Root and two independent readers confirmed there is no ordinary lazy initialization. The prior attack ledger describes intended authored definitions, not previously active runtime attacks. Public first-swing tests observed generic1d2 without Cutting/Axe on Warlord/Sentry. This is now included as a separate documented activation repair.
- The first natural test RNG accidentally returned exploding penetration10 forever. That fixture run was terminated, retained as GA03i-natural-rng-fixture-stall.log.gz, and produced no fresh XML. A bounded RNG now chooses legal non-exploding9; subsequent public10 cases returned5PASS/5FAIL before the natural fix. This was a fixture error, not a claimed production fix.
- Factory-end natural initialization and missing-only attached/detached save repair made all84 focused tests green. The first full run returned9567PASS/1FAIL out of9568, zero compiler errors. Saved custom body flags require narrower preservation. Independent review also identified shared-natural detached alias duplication after healing; its new test confirms RED. Both are being fixed before completion.
- Native 23-group scenario added after root read all three prepared sources. It uses actual 24 actors, starting village/named profiles, stock, first natural attacks, native N/F5/F6 and ground pickup. No forced default generation is allowed in positive fixtures. Initial native run16a132ba899c4d77b953f458fb14774e passed23/23 with0unexpected errors and verified private-save cleanup; final rerun follows the two-handed repair.
- Sprite preparation: built-in image generation produced a dagger concept, 1254×1254 RGBA with nonbinary alpha. It fails the final16×16/binary-alpha asset contract and is not copied to Assets or registered. Keep it as a rejected production candidate while preparing a compliant sprite phase; no art improvement is claimed from this image.

- Saved flags/shared detached aliases and unknown missing recipes are repaired conservatively; focused95 passed before the final26-case natural adversarial sweep. That sweep found one real two-handed regression: the occupied secondary hand added a natural attack. Its minimal guard is implemented after a true75s native BEFORE. AFTER preserves free offhands and omits the extra two-handed swing. Exact provenance proves only this guard differs between captures; final full/native gates remain pending.

## Equipment phase close-out — GA03i

Status: CONTENT/NATURAL ACTIVATION COMPLETE; SEVEN GROUND-SPRITE FAMILIES NEXT. Exactly24 kits, per-command quiet spawn grants, completed natural creation/legacy repair and two-hand selection guard ship together after actual RED discovery. Final9599/9599GREEN (+115),0CS; focused121/121; final native23PASS/0unexpected; independent source and paired-profile reviews clear. See Verification/GameSystemAudit/GA03i-REPORT.md for complete receipts, failed hypotheses/fixture errors, gameplay/balance divergences and measured bounds. Ground art has not shipped in this phase.
