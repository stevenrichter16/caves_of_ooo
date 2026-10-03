# Equipment discoveries and build specialization

Status: implemented, cold-reviewed and verified in native Unity EditMode and an ordinary PlayMode journey (2026-10-03).

## Intent and readiness

Finding a component should suggest a different learned combat style or a different next expedition. This is original Caves of Ooo content using the existing family, equipment and forging mechanics. The reference is the player's stated desire for build-changing discoveries, not a claim of source-level Caves of Qud parity.

- 🟢 Existing forge transactions, native skills, elemental damage, equipment slots, exact geographical inventories and original cuboid art pipeline are usable.
- 🟢 Fixed authored steel family, absent elemental resistance stats and family/tradeoff preview; added exact geographical sources and 18 original model forms.
- ⚪ No new skill tree, crafting skill gate, global loot framework, resistance immunity or generated-world migration is needed.

## Verified corrections before implementation

| Premise | Verified behavior and decision |
| --- | --- |
| Forging cannot express families | `WeaponForgingService.PreviewForge` already unions component attributes; `ApplyComponentStats` uses that result. Fix content, not the assembly engine. |
| All six parts need families | Only striking heads define a family. Steel becomes Cutting LongBlades; IronSpike retains Piercing for existing short-blade gates. Hafts/bindings remain neutral. |
| A cudgel needs a new stun effect | `OnHitClassEffects` already gives Bludgeoning a 15% two-turn DC16 stun. Extra proc durations can stack. Use native behavior and learned Cudgel skills. |
| Components can impose arbitrary handling costs | Shipped assembly transfers damage, penetration, hit, strength cap, attributes and on-hit effects, not head speed/weight/reach. Use real numeric drawbacks. |
| A forge is required | `ForgeWeaponCommand` permits one weapon from the pack. Only batching requires an adjacent forge. Supply haft and binding with each regional source. |
| Elemental EquipBonuses work on Player | `EquipBonusUtility` skips missing stats; Player lacks resistances. Materialize only recognized elemental resistance stats on successful equip with signed bounds, preserve all other unknown names. |
| Resistance 50 means half of every tick | Typed damage floors positive damage at one below immunity. Do not sell acid resistance as relief from one-point mire ticks. |
| Shields give global physical armor | Body hit-location armor is local. Groundwire screen provides electrical resistance and occupies a hand; no imaginary universal shield AV. |
| Adding a component also gives it 3D art | Current portable and assembled recipes enumerate exact six/eight forms. Add a dedicated original library and held/worn resolution. |
| New stock can be restocked anywhere | Authored geographic inventories are finite first-generation supplements. Leave generic pools and merchant restock rules alone. Existing saved inventories stay literal. |

Read authorities: `WeaponComponentPart`, `WeaponForgingService`, `ForgeWeaponCommand`, `InventoryUI.Crafting`, `LongBlades_Lunge`, `SkillCombatHelpers`, `OnHitClassEffects`, `EquipBonusUtility`, `Stat`, `CombatSystem.ApplyResistanceFor`, `SoddenDistrictBuilder`, `CinderholdCompositionBuilder`, `LastCounterCompositionBuilder`, `TraderRestockSystem`, `SpreadPortableRecipes`, `SpreadEquipmentRecipes`, `Village3DEquipmentViews`; `CRAFTING-FROM-THE-PACK.md`, `LOOT-FINDS.md`, and `SODDEN-EXPEDITION-DESIGN.md`.

## Authored contracts

All three new heads retain technical Slot=Blade. Player labels become Head or Heads / blades. Hafts and bindings retain their existing modifiers, so final values must be previewed, not described as fixed whole-weapon penalties.

| Blueprint | Head contribution / role | Drawback | Guaranteed geographic source |
| --- | --- | --- | --- |
| SteelBladeComponent (existing) | 1d6, Cutting LongBlades; learned Lunge now recognizes it | Existing ordinary baseline | Existing sources unchanged |
| PeatMalletHeadComponent | 1d4, Bludgeoning Cudgel; native stun and learned control skills | Pen −1, low damage | Sodden peat works locker, Overworld.17.7.0 |
| CinderhookAxeHeadComponent | 1d8, Cutting Axe; damage and learned axe skills | Hit −2 | Cinderhold workshop Weaponsmith, Overworld.6.6.0; purchased initial stock |
| CounterweightLongBladeComponent | 1d6, Cutting LongBlades, Hit +1; accurate long blade | Pen −1 against armor | Last Counter SupplyPost chest, Overworld.18.18.0 |
| KilnfeltApron | Body, AV1, DV0, HeatResistance +50 | Speed −5 and weaker physical body protection than LeatherArmor | Cinderhold workshop Weaponsmith |
| GroundwireScreen | Hand, AV0, DV0, ElectricResistance +50 | Occupies a hand instead of shield/second weapon/two-hand grip | Sodden peat works locker |

Each component source also supplies one OakHaftComponent and one LeatherBindingComponent. Regional finds are not added to ComponentAny, generic merchant stock, manufactured wilderness pools or natural Grovelands containers. Lore framing: peat-packing tool and old pump-earth screen; extraction hook and kiln apron; frontier counterweighted fencing stock. Distinct silhouettes: broad mallet, hooked axe, slender counterweight blade, copper mesh screen and layered ash/red apron.

## Saves and failure behavior

Generated/saved source inventories remain literal; no free replenishment on entry. Fresh sources use existing exact-address/profile admission and staged publication. New stock must participate in existing receipt/dependency validation. New elemental stats have zero base and signed limits; unequip removes only equipment contribution. Failed equip must leave no effective bonus. Save/reload, occupied-slot replacement and body-part removal must preserve symmetry.

Old steel components and known steel-headed assemblies receive only the missing LongBlades token through a narrowly identified load repair. Preserve saved numerical values, modifications, tempering and other attributes. Unknown/custom heads remain untouched. New equipment does not grant learned skills.

## Milestones and acceptance

1. **Contracts RED:** real authored steel forge/learned Lunge; exact regional find acquisition; absent-stat elemental equip/damage; exact art lookup. Save test output before production.
2. **Family and defense correctness:** preview-to-result parity, family-specific learned gates, reforge replacement/rollback, save repair counterexamples; real Player equip/unequip, typed/untyped damage, slot conflict, zero-base signed stats and save/body-part lifecycle.
3. **Discoverable content and presentation:** all three real source inventories include companion parts, finite depletion/save/revisit, wrong location/profile counterchecks; local text names finds and costs; all new dropped/held/worn identities resolve to original models and composed variants.
4. **Native verification and cold review:** Unity EditMode neighbors, ordinary crafting/equipment scenario with saved evidence and screenshots, inspect visual results, Q1 symmetry/Q2 consistency/Q3 counterchecks/Q4 doc drift, repair significant issues. Distinguish staged test setup from ordinary acquisition and visual inspection from feel.
5. Update this living doc and relevant system docs, commit with CLAUDE.md §2.3, fetch/rebase and publish to authorized main.

## Evidence, self-review and implementation log

### Recorded checkpoints

- Before production: native Unity RED, 121 cases, 24 controls passed / 97 failed (`Verification/EquipmentDiscoveries/RED.xml`). Actual steel Lunge returned false; all four absent elemental stat tests observed zero instead of 50 resistance. Missing source/model checks also failed as expected.
- First integrated pass: 149 cases, 136 passed / 13 failed (`intermediate.xml`). This independently reproduced an existing equipment transaction error: AfterEquip injury already removed the bonus, and later rollback subtracted it again (screen became −50 electrical resistance; a preexisting agility buckler lost two additional agility). Four no-injury controls passed. Corrected undo to check actual current ownership before removing bonus/gear.
- Other intermediate failures: the mallet cap extended .01 below its validated floor (raised actual geometry); controlled Lunge test rolled penetration too low to damage even AV0 (corrected deterministic noncritical/nonexploding roll, kept real HP-loss assertion); restock test omitted the separate periodic restock factory (wired/restored that fixture dependency, kept real refill/exclusion checks).
- Unity's script import queue stalled after compilation requests; restarting the idle, clean-scene editor restored compilation. A transient import error referred only to the continuously written MCP log. These are environment observations, not passing feature evidence.

- Stack rollback review: after dismemberment independently dropped a split unit, rollback used to merge its count into the carried stack and zero the dropped unit. `split-stack-RED.xml` observed two failures / ten controls passed. Ownership-aware stack restoration fixed this without reversing the independent injury. An earlier four-failure fixture attempt accidentally added a second StackerPart; the corrected fixture uses the inherited stack and retains exact count/owner assertions.
- Broad native Unity run: **1476/1476 passed**, zero skipped, 218.24 seconds (`native-neighbors-GREEN.xml`). This includes source generation, inventory, forging, UI, equipment and rendering neighbors; it is a selected native suite, not the complete repository suite or the headless runner.
- Final displacement review: `displacement-RED.xml`, **three failures / three controls passed**. Replacing an equipped screen during an independent limb loss restored resistance onto a severed slot; a separate committed equip during AfterUnequip could block ownership restoration but still restore old bonuses. The bounded fix validates every captured slot against the current Body and restores bonuses/enhancements only after ownership restoration succeeds, symmetrically for unequip and throw detachment.

### Visual review

The native builder generated 18 mesh/prefab forms: three loose heads, twelve three-part assemblies, two dropped defenses and one fitted apron. Three gallery cases passed in `art-and-core-intermediate.xml`; the four failures in that run were the disclosed unrelated split-stack fixture setup, not gallery failures. Each gallery report records exact factory Player, native equip commands, submitted meshes/materials/bones, and five frames (front/side/back Idle, Walk, Attack). Root and presentation reviewer inspected multiple frames across every weapon and both defenses: distinct broad mallet, hooked axe, slender weighted blade, open copper screen and fitted layered apron. No significant geometry, palette or attachment problem was found. The far-hand weapon is partly occluded by the arm/body in the quarter-angle attack frame; this is not an all-animation or normal-zoom readability proof.

Gallery setup is staged EditMode art evidence. Ordinary acquisition and live game rendering are verified separately below.

### Cold review (Q1–Q4)

- **Q1 symmetry — fixed 🟡:** absent-stat application/removal, native injury, rollback and split ownership checked together. Fixed double removal, split duplication and displaced-item ghost bonuses; retained no-injury, occupied-slot, veto and existing-stat controls. No new damage or crafting framework was necessary.
- **Q2 consistency — verified:** three heads use the existing Blade slot, canonical family tokens and unchanged assembly arithmetic; UI labels are Head/Heads / blades. Each source gives companion parts, uses existing purchase/container operations and remains finite. Ground/equipped rendering resolves actual owners and exact assembly variants.
- **Q3 counterchecks — verified:** learned versus unlearned skills; wrong weapon family; absent/existing/unrecognized stats; typed versus untyped damage; correct/wrong location and profile; depleted versus saved inventories; exact versus foreign/custom render identity; surviving versus lost limb; independent slot takeover versus untouched rollback.
- **Q4 documentation — corrected:** one pack-forged weapon is a free inventory action, not a paid turn or station requirement. Resistance 50 is not immunity and minimum positive damage remains one. Final weapon values include haft/binding modifiers. New inventories are not retroactively refilled, and skills are not granted by gear.
- **⚪ Deliberate boundary:** a scavenger's separate carry-display library does not show these regional items in its carry socket. Native inventory ownership and dropped/equipped rendering work. Extending the cosmetic carry catalog is lower priority than the verified player-facing paths.
- **⚪ Explicit developer unload:** the manager can discard/regenerate Cinderhold and Last Counter if a debug scenario explicitly unloads them. No shipped ordinary caller reaches that operation for either town: settlement repair currently tracks only Morrowfast/Wellmeet. Ordinary travel, cached revisit, save/load and smith restock preserve finite specialty stock. No speculative settlement lifecycle rewrite was added.
- **🧪 Remaining playtest question:** long-term balance and whether the three discoveries substantially change player expedition choices require ordinary repeated play, beyond deterministic capability tests.

### Scope decisions and divergences

| Design concern | Shipped decision and reason |
| --- | --- |
| Skill-bearing equipment | Reuse authored family tags and existing learned skills, without granting abilities. |
| Cudgel control | Existing Bludgeoning class stun, no duplicate proc. |
| Defensive alternatives | Two distinct alternatives rather than additional resistance clones: body heat protection with speed/AV cost; hand electrical protection with slot cost. |
| Old saves | Narrow steel family-only correction; no generic item rebuild and no refill of generated source stock. |
| Failure review | Expanded to the reproduced equip/unequip/split rollback defects because they corrupt these equipment benefits and ownership. |
| Verification | Real Unity tests and staged art gallery plus a native Sodden acquisition journey. Cinderhold/Last Counter acquisition is exercised through native command tests, not a claimed continuous keyboard expedition. |

### Changed file groups

- `Objects.json`: one existing steel attribute change and five new original blueprints; parsed semantic diff proves 670 preexisting blueprints unchanged.
- `Gameplay/Weaponcraft` + `InventoryUI.Crafting`: narrowly scoped saved-steel repair and family/tradeoff preview.
- `Inventory/Commands/Equipment`: elemental stat materialization and ownership-aware rollback fixes.
- `World/Generation/Builders/{SoddenDistrict,CinderholdComposition,LastCounterComposition}Builder`: finite exact sources, companions, local clues and staged validation.
- `EquipmentDiscoveryArtLibrary`, `EquipmentDiscoveryRecipes`, `EquipmentDiscoveryKitBuilder`, rendering integrations and `Resources/EquipmentDiscovery3D`: authored original assets and real-owner ground/held/worn wiring.
- `SpreadDiscoveryNativePlayer.Equipment` and `.Sodden`: isolated ordinary acquisition, keyboard crafting/equip and save/load audit.
- Ten new EquipmentDiscovery test fixtures (164 cases), two exact existing pins, this design/prompt record and system-document follow-ups.

### Final lifecycle verification

`lifecycle-GREEN.xml`: **433/433 native Unity EditMode cases passed**, zero skipped, after the final fix. Includes all eight displacement tests (the original six plus two throwing symmetry checks), 19 defense and 12 defense-adversarial cases, equipment/loadout lifecycle, equipment adversarial, transfer adversarial, inventory and grenade throwing neighbors. This is an overlapping regression selection, not 433 additional unique tests over the broad run.

### Ordinary journey evidence

First run, `SpreadDiscoveryExpeditions/Native/2e8fd45a67d54823b6ac8722e8ad0955/report.json`, reached and looted the works, consumed the actual three parts and passed the family/preview/forged checks. Its equip assertion failed because the harness used automatic replacement twice with both starting hands occupied: the screen correctly replaced the newly equipped mallet in the same first hand. This was a harness assumption, not a production item failure. The revised journey explicitly stows the original two hand items through normal inventory actions before equipping the new pair; model assertions remain strict.

Final run, `SpreadDiscoveryExpeditions/Native/c23ecfadb4074ca682959c13af8aa0b8/report.json`: **14/14 checks passed, zero failures, zero unexpected errors**, 168.83 seconds. Ordinary seed64 Duelist, 529 submitted keyboard actions, no grants, transfers, clock edits or AI suppression. The exact generated mallet/haft/binding were consumed into one weapon; the starting hand gear was stowed and mallet/screen equipped through inventory. The actual submitted models were `equipment-discovery-forged-peatmallet-oak-leather` and `equipment-discovery-groundwire-screen`. Return repair/service still worked. F5, one unsaved step and F6 replaced the actual graph, retained both equipped items and ElectricResistance 50, and left the works locker empty. The isolated launcher restored SampleScene and exited PlayMode.

**Can verify (script-observable):** actual source, consumed identities, family and numerical preview, native UI actions, equipped owners, exact submitted mesh/palette/attachment, zero skill grant, resistance statistic, saved ownership/depletion and native return services.

**Cannot verify from this journey:** electrical combat reduction or learned-skill damage (controlled native tests cover those); Cinderhold/Last Counter continuous keyboard acquisition; blind discovery, all-seed travel balance, broad animation transitions or long-term choice quality. No enemies or damage were injected into this player journey.

**Independent visual review:** opened the ordinary crafting preview and equipped-works screenshot. Family Cudgel, damage 1d4, red Pen −1, green Hit +1 and strength cap 3 are readable. Both equipment attachments are visible on the small player at ordinary world zoom; fine model detail is judged from the separately disclosed close-up gallery. Original UI truncates long composed names. No significance sufficient to delay this feature was found. Fifteen staged gallery frames, their reports, the failed harness run and the successful native journey are retained as distinct evidence.

**Final selected test total:** 1575 unique native passing cases across the overlapping broad/lifecycle selections and three gallery cases, including **164 new cases**. `Verification/EquipmentDiscoveries/verification-summary.json` records the accounting. All significant review findings are fixed; the bounded cosmetic/debug/playtest limitations above remain explicit.

Publication: commit this code, assets, prompts, design and evidence together using the repository template; fetch/rebase and push the authorized main branch.
