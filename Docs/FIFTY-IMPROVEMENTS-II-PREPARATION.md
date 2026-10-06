# Fifty improvements II — preparation and build choices (16–30)

Status: **complete — implemented, reviewed, native regression and controlled Play accepted.** Final combined evidence is `Verification/FiftyImprovementsII/affected-regression-final.xml` (2,578 passed, zero failed, one existing explicit census skipped) and `Native/f73a91c469ca4d4fac171f4321e94149/report.json` under the same verification directory. Earlier stage receipts below remain an implementation history.

These are 15 player outcomes, not an assertion count. Root owns Unity, commits, Objects.json and presentation aliases. Existing repair, meal, cultivation, physical-liquid and reader improvements are the baseline. This is original Caves of Ooo design without a numerical Qud-parity claim.

## Verified source corrections

| Initial premise | Verified source | Decision |
|---|---|---|
| Crafting needs a new station framework | `WeaponForgingService.cs:38`, `ForgeWeaponCommand`, `BrewingService.TryBrew`, ordinary crafting UI already produce single items from the pack; batching/re-forging/quenching retain station rules. | Reuse inventory commands, transactions, previews and existing forge/still admission. |
| Common materials can already produce weapon parts | `BotanicalProcessingPart.cs:15` only converts one input into one configured output kind; cord also needs a bandage alternative. | One bounded preparation recipe table exposes six explicit recipes through ordinary item actions. No bits or universal craft graph. |
| Tepuibone is animal bone | Objects.json explicitly calls it sandstone cut from the Root, weight 12. | Shape a stone striking head; do not add butcher/bone fiction. |
| Components support weight/reach/speed | `WeaponComponentPart.cs:19–53` and `WeaponForgingService.cs:364–415` transfer damage, hit, penetration, maximum Strength contribution, attributes and proc text. | New components use only these existing numbers. No invented reach or speed costs. |
| GasMask already works as wearable gear | `GasMaskPart.cs:24–29` documents that it currently lives on the creature. | Forward only its two supported events from exact current body-owned equipment; no permanent actor Part grants or removal of innate defenses. |
| Cooking can prepare arbitrary reagents | `CookingService.cs:42` requires a FoodPart output. | Concentration/detoxification use the preparation service at an actual alchemy still, not Cookable. |
| A Sodden keeper can sell a hood | The dressing keeper is a PeatCutter with a bounded service, not a generic Trader. | Both Sodden defenses are one-unit finite locker stock, integrated by the world owner. |
| Crop claims travel with arbitrary relocated plants | `LocalGatheringClaims` derives reserve permission from exact saved soil/source owners. | Transplant refuses claimed beds; it does not invent transferable land ownership. Seed harvest keeps existing claim capture/publication. |
| Mineral recipes enable ordinary tinkering | `TinkeringService` requires BitLocker even for zero-bit recipes; ordinary play has no BitLocker. | Add a separate physical forge action invoking existing enhancement rules with one real mineral. Never grant bits, knowledge or BitLocker. |
| The original Sodden builder includes every later stock supplement | `OverworldZoneManager.CommitGeneratedZone` installs versioned `SecondExplorationSites` only after the original destination is accepted. Direct builder fixtures bypass this stage. | Test the two new garments through the actual current cold manager, then acquire and save their exact owners. Do not inject stock into an old builder or retrofit old saves. |
| New items automatically receive native models | `EquipmentDiscoveryRecipes` and `SpreadEquipmentRecipes` use explicit identities and exact assembly component IDs. | Root adds compatible original model aliases for every new loose/equipped item and assembled combination. No silent 2D fallback. |

Baseline sources read: previous `FIFTY-IMPROVEMENTS*.md`, `EQUIPMENT-DISCOVERIES-DESIGN.md`, `BIOME-CROPS.md`, current blueprint/loot catalogs, WeaponCraftingOperation, WeaponForgingService, WeaponTemperingService, BotanicalProcessingPart, CookingService, BrewResolver, BrewingService, GasMaskPart, InventoryPart, CropPart, CropTime, CropYieldService, LocalGatheringClaims, TorchLightPart, ItemEnhancing and mineral enhancement shims. Older alchemy docs are historical where they claim station-only single brewing or an ordinary developer starter kit.

## Individual plans and designs

### 16. Reclaim a forged weapon's useful parts

**Outcome:** Retire one assembled weapon at a forge and recover its recorded head and haft; the binding and all tempering/enhancements are lost. This creates salvage value without making assembly cycles free.

**Design/cost/source:** New `WeaponSalvagePart` on the existing ForgedWeapon declares `SalvageForgedWeapon`. One carried, unequipped unit with valid WeaponAssembly provenance is consumed; factory-created head/haft must match the saved blueprints and actual Blade/Haft slots. Existing weapons/forges are the source; no free supply. One paid action, no BitLocker. The result never copies weapon modifications or forged public fields onto components.

**Acceptance/counters:** Exact recorded head/haft recovered once; binding absent; the second weapon in a stack and its payload remain unchanged. Refuse native/non-forged weapons, equipped/foreign/zero stock, unknown or wrong-slot recorded blueprints, no forge and inadequate capacity. AfterInventoryAction failure restores payment and outputs. Replacement save retains provenance and does not restore spent binding.

### 17. Shape common materials into a complete basic assembly

**Outcome:** A player can build a modest weapon from real repair stock instead of waiting for three component drops.

**Design/cost/source:** Ordinary material actions: one SalvagedTimber → FieldHaftComponent (Strength cap 1); one KnotflaxCord → PlainCordBindingComponent (no combat bonus); one Tepuibone → TepuiboneHeadComponent (1d3, Pen −1, Bludgeoning Cudgel). These three recipes together are one outcome. Each preparation costs a turn; pack forging consumes the three resulting units normally. Supplies already exist in Morrowfast mender stock, finite timber salvage, knotflax crops and tepuibone sources. Values cannot create a buy/shape/sell profit loop. Existing oak/leather and regional heads remain better finds.

**Acceptance/counters:** Real authored materials produce exact components and an ordinary forge produces the previewed Cudgel-family weapon. Wrong material/empty/foreign stock or capacity failure cannot mint parts. Preparing one unit from a stack preserves the remainder. All three prepared parts and their assembly resolve compatible native models.

### 18. A haft for a high-Strength build

**Outcome:** BracedHaftComponent allows Strength modifier up to 5, at Hit −1.

**Design/source:** One initial finite unit on Cinderhold's existing Weaponsmith, beside current forge stock. No restock-pool change. Normal forge/re-forge carries the choice into actual melee stats. It adds no reach, weight transfer, speed modifier or skill grant.

**Acceptance/counters:** Same head/binding with oak versus braced haft demonstrates cap 3 versus 5 and exactly −1 hit. Below-cap Strength gains no imaginary damage. Actual source count, depletion, revisit and save remain finite.

### 19. Accurate binding at an armor-penetration cost

**Outcome:** GuardLashingComponent gives Hit +2 and Pen −1, competing with leather's Hit +1 and serration's penetration/bleed.

**Design/source:** One finite unit in Last Counter's existing SupplyPost chest. Plain appearance may reuse the existing wrapped binding form; description states the exact tradeoff. Forge and re-forge share their current preview/result pipeline.

**Acceptance/counters:** Same head/haft gains exactly one hit and loses one penetration relative to leather; no additive duplicate slot or free old-binding recovery. Preview, saved assembly and native recipe resolution agree.

### 20. Wear a filter instead of a helmet

**Outcome:** FilterHood occupies Head with AV 0/DV 0 and GasMask Power 10. It reduces respiratory intake by 50 and typed gas damage by 10%; it is not gas immunity or a poison cure.

**Design/source:** Exactly one hood in the finite Sodden peat-works locker. Forward GetRespiratoryPerformance and BeforeTakeDamage only from exact current equipped owners, once per physical item, using GasMaskPart itself. Preserve innate actor GasMask behavior and all unrelated effects. No saved actor-side duplicate bonus. Existing leather-equipment-stitch repair is opt-in, with explicit Leather composition, reactive leather material, thermal properties and hit points.

**Acceptance/counters:** Real body equip changes intake and typed damage; stowing, dropping, losing the head slot and save replacement remove/reconstruct only equipment contribution. Foreign/equipped-pointer-only impostors and duplicated body slots do not protect. Untyped damage and existing poison stay unchanged.

### 21. Wear acid protection at a physical-defense cost

**Outcome:** AcidworkerApron gives AcidResistance +50, Body AV 1/DV −1 and SpeedPenalty 5; it competes with physical armor and the existing heat apron.

**Design/source:** One finite unit in the Sodden peat-works locker. Weight 12; explicit Leather composition/material, thermal properties and hit points admit existing leather repair. Reuse the original apron model with explicit identity mapping. A one-point damage floor still applies below immunity.

**Acceptance/counters:** Actual equip halves a ten-point acid hit but not untyped/heat damage; one-point acid still hurts. Unequip, body loss and replacement save are symmetric. Source does not replenish.

### 22. Choose cold protection over a dodging cloak

**Outcome:** ColdwardCloak gives ColdResistance +50, Back AV 0/DV −1, weight 4. It gives up the ordinary cloak's defensive contribution.

**Design/source:** One finite unit in Last Counter's SupplyPost chest, using the existing cloak model. Explicit Leather,Fiber composition identifies the repairable leather fastening seams; Cloth reactive material and thermal fields retain its fabric behavior. No immunity or extra inventory slot.

**Acceptance/counters:** Typed cold protection, DV cost, slot displacement and save/unequip symmetry are actual; other elements and untyped damage are unchanged. Depletion and revisit retain literal stock.

### 23. Use lamp oil to keep a torch useful

**Outcome:** Spend one carried LampOil on one current unlit torch to add 25 fuel, capped at its authored MaxFuel, without igniting it.

**Design/source:** `RefuelTorch|<id>` rows on the oil use ordinary carried inventory targets and real TorchLight/Fuel/LightSource Parts. Full, lit or unusable torches refuse for free. At least some capacity must remain; the whole oil unit is spent even for a partial top-up. Oil remains an alchemy reagent, creating a preparation tradeoff. Existing lamp-oil acquisition is unchanged.

**Acceptance/counters:** One unit/turn yields the exact capped fuel; light remains off; subsequent existing lighting/burning spends that fuel normally. Wrong liquid, lit/full torch, foreign target, duplicate ID, zero stock and after-event rollback do not refill or consume.

### 24. Make an emergency cord bandage

**Outcome:** One KnotflaxCord can become one KnotflaxBandage, an applied bleeding treatment with no healing, poison cure or future immunity.

**Design/source:** The second cord preparation choice competes with bindings, repairs and the existing staffed Sodden dressing (which also treats poison). New bandage uses existing Tonic/CureTonic behavior; no second general medicine engine. Preparation and actual application each cost their normal action; the known tonic convention can spend the bandage when no bleeding is present and must be stated honestly.

**Acceptance/counters:** Preparation spends one cord; application spends one bandage and removes ordinary BleedingEffect. Existing PoisonedEffect and HP remain unchanged. Refused preparation is atomic; the original Sodden recipe/access remains intact.

### 25. Concentrate weak healing herbs before a fight

**Outcome:** Two MendleafSprig from the selected stack become one ConcentratedMendleaf with vital:2 at an adjacent alchemy still.

**Design/cost:** The raw two sprigs can make two separate 1d4 brews; the concentrate makes one 2d4 brew with lower carried bulk and one fewer combat drinking action, after paying the additional preparation action. Duplicate reagents still combine by MAX, not sum. No potency above tier 2. Existing Mendleaf plants/drying yard are sources.

**Acceptance/counters:** Actual two-to-one payment and resolver/tonic healing dice match; one sprig, distant/broken still, capacity refusal and outer rollback preserve ingredients. Ordinary field brewing remains allowed and unchanged. No potency gain by repeatedly concentrating the output.

### 26. Make a dangerous grove reagent safe

**Outcome:** At a still, one GroveRed can be processed into one CleansedGrovePulp (vital:2, no toxic), sacrificing its raw vital:3 potential/toxic weapon use for reliable healing.

**Design/source:** Existing grove-red drops/harvest supply the original; no new universal forage. The original toxic recipe and its lore remain unchanged. Output cannot be detoxified again. The existing resolver now yields healing instead of its toxic-only result because the original mending rule forbids toxic.

**Acceptance/counters:** Raw GroveRed still cannot make a healing brew; processed pulp produces 2d4 healing without a hidden poison effect. One input/output, station gate, rollback and save payload are exact. No claim of curing the Grovelands or narrative corruption.

### 27. Recover use from an inert failed brew

**Outcome:** Spend one InertSludge to compost a newly planted crop once, reducing both stage lengths to ceil(75% of their current value), minimum 1.

**Design/cost:** Requires a current adjacent cultivated crop at stage 0 with zero accumulated growth; no time or moisture is granted. Record a saved Composted flag on CropPart, so repeated doses refuse. Reconcile the crop's elapsed time before validating the fresh-stage requirement. Sludge remains the existing finite paid brewing failure product; toxic or active brews are never interchangeable.

**Acceptance/counters:** The saved one-time reduction makes actual moist growth finish earlier; dry growth still pauses. Refuse ripe/progressed/previously composted crops, no-benefit stage lengths (1–3), foreign/stale crop, wrong reagent and unprepared/barren/flooded ground. Rollback restores the stage length/flag and sludge.

### 28. Harvest seed stock instead of immediate produce

**Outcome:** A ripe cultivated crop with a real authored seed yield can sacrifice all produce to yield three times its normal seed count, capped at the shared yield limit.

**Design/cost:** Explicit `HarvestCropSeeds` beside the existing harvest/gather rows. Reuse the actual CropYieldService publication/rollback and LocalGatheringClaims receipt. The crop is removed once, the bed remains, seeds are real saved items. This trades today's useful produce for expanding cultivation; it does not raise normal harvest yields.

**Acceptance/counters:** Normal harvest remains two produce plus one seed for knotflax; seed-focused harvest gives only three seed units. No-seed, unripe, malformed seed blueprint/count, stale owner, claimed reserve consequence and factory/transfer failure are paired controls. Saved depletion prevents second harvest.

### 29. Move a young plant without cloning it

**Outcome:** Move one unclaimed young crop into an adjacent empty prepared bed while preserving its identity, stage, progress, moisture and clock fields.

**Design/cost:** `TransplantCrop|x|y` actions enumerate only currently reachable legal destinations within one cell of the player and source. One paid action; no seed refund, produce, watering or time rewind. Refuse claimed source or destination beds instead of creating a transferable land-claim system. Both source and destination render cells become dirty.

**Acceptance/counters:** Same owner ID/Parts arrive in the new cell; former bed remains empty/prepared; saved growth continues once. Ripe crops, existing destination crop/solid owner, fog/foreign/barren/flooded destination, moved actor, source mismatch and rollback refuse or restore without duplicates. Existing claim-managed crops remain in place.

### 30. Infuse a real mineral without unlocking bits tinkering

**Outcome:** At a forge, spend one carried PaleSalt, ChoirIron or GlowQuartz to apply its existing corresponding enhancement to one explicitly selected compatible carried singleton.

**Design/cost:** Mineral inventory rows `InfuseMineral|<target-id>` show compatible current owned targets and the existing rule/cap; no hidden automatic choice. Use ItemEnhancing/mineral modification semantics and normal two-enhancement cap. The mineral is spent only on committed success. No BitLocker, learned schematic, abstract bits, arbitrary enhancement name or repeat-tier stacking. Restrict targets to unequipped singleton units for the first bounded route; normal equip applies effects afterward. Existing mineral trade/tribute and loot sources provide competing uses.

**Acceptance/counters:** Real enhancements affect the corresponding target tags/light on later equip; wrong target, full cap, duplicate enhancement, insufficient source, foreign/stale target and outer rollback leave both item and mineral unchanged. Save retains the exact enhancement/slot count. Ordinary Player remains without BitLocker before and after.

## APIs, ownership and verification sequence

Implemented action Parts/helpers: PreparationRecipePart/PreparationRecipeService, WeaponSalvagePart, TorchRefuelPart, MineralPreparationPart, CultivationPreparationService and the shared PreparationActions receipt/command predicates. Handlers take actor, actual selected owners, Zone and the existing InventoryTransaction; commands use ordinary GetInventoryActions/InventoryAction routing. IDs are re-resolved uniquely at execution. No unrelated world generation, global always-on overlays or per-frame crafting scans.

Preparation owns new files/tests/scenario, narrow InventoryPart gas-event forwarding, CropPart hooks/Composted saved field, CropYieldService bounded seed-only option, and Cinderhold/LastCounter stock supplements. World owns broader SoddenDistrictBuilder and integrates the two agreed locker items. Root owns all Objects.json and native visual identity/assembly aliases; exact blueprint proposal is `/tmp/fifty-ii-preparation-blueprint-spec.json`. Shared input command timing is one coordinated predicate hook after RED.

Test fixtures use actual factory blueprints and existing action/transaction entry points; only the previously absent saved compost field used a reflection presence guard so all streams compiled before production. Pair normal success with changed owner/quantity/context refusals; add mutation/after-event rollback, real body equip, temporal growth and full save replacement cases. Dedicated adversarial fixtures cover behavior, not assertion-count padding. Root observes native RED before any production patch. A detached `FiftySecondPreparationBench` exposes ExpectedCases/RunId/Cases/Failures/Audit/Observations; root integrates it into the protected common native launcher. It proves commands/scheduling/save, not ordinary discovery or feel. Ordinary source and all new portable/equipped/assembled model admissions need separate tests and selected screenshots.

## Performance and review

New preparation work runs only on explicit menu queries/actions. Gas forwarding traverses the small current body tree without per-event heap collections; one item is counted once. Crop mutations dirty only changed cells. No new Update/LateUpdate loop, global cache or tick scan is introduced; existing CropTime/CropSystem own growth. Before publication, review Q1 payment/undo symmetry, Q2 shared action ownership/timing, Q3 positive/counter pairs and Q4 exact claims versus native evidence. All balance numbers above are authored starting choices, not demonstrated fun or campaign balance.


## First implementation evidence and remaining gates

- Native RED: [combat-preparation-red.xml](Verification/FiftyImprovementsII/combat-preparation-red.xml), 74 preparation cases: 36 passes and 38 failures. Production began only after root observed this receipt.
- First integration: [integration-first.xml](Verification/FiftyImprovementsII/integration-first.xml), 66 of 74 preparation cases passed. Three gas fixture probes placed Intake in the integer dictionary while the existing GasMask event reads its object dictionary; the fixture now uses the same boxed integer as real gas. Three stock cases successfully acquired their item, then wrongly required a depleted profile replay to succeed; they now check that replay does not replenish stock and still perform replacement-save checks. Two Sodden stock cases awaited the other stream's authorized integration.
- Additional source review found that stage lengths 2 and 3 also round back to their original length at ceil(75%). Two new counters pinned free refusal for no benefit; root observed both REDs in the second integration receipt before the gate changed from >1 to >3.
- `FiftySecondPreparationBench` is a detached, controlled 18-check witness: authored item transformations, actual command payment through the owner scheduler, equipment effects, finite costs and refusals, elapsed crop growth, and replacement save. A companion test checks caller inventory, HP, clock, factories and active-zone preservation. Supplies, stations and crop conditions are explicit fixtures. This does not prove ordinary acquisition, keyboard operation, unaided visual recognition or balance; source fixtures and root's separate visual/native work supply those distinct receipts.
- Full runtime and EditMode source compiler preflight passed after these additions. It is not a Unity result. Root remains the sole Unity operator. All 78 preparation cases have now passed across receipts; final combined regression and Play-session UI/model evidence remain pending. The bounded Q1–Q4 review is recorded below.

- Second integration: [integration-second.xml](Verification/FiftyImprovementsII/integration-second.xml), 73 of 77 preparation cases passed. All main mechanics and the controlled witness passed; the two no-benefit compost counters failed as predicted, and two source cases still awaited Sodden stock. The stage-length correction followed that observed RED.
- Independent cold review identified a mineral rollback boundary: undo must remove only enhancement instances introduced by the failed Apply call, not a different enhancement added by a later callback. Root observed the added counter fail in [integration-third.xml](Verification/FiftyImprovementsII/integration-third.xml), with 44 of 45 preparation adversarial cases passing. Rollback now records the exact enhancement instances introduced during Apply, before later action callbacks, and removes only those instances.

- Fourth integration: [integration-fourth.xml](Verification/FiftyImprovementsII/integration-fourth.xml), 76 of 78 preparation cases passed. Both compost counters and the mineral callback counter now pass. The remaining two failures came from the source fixture invoking only the base Sodden builder. The world stream’s current cold-manager tests already admit exactly one of each garment; this fixture now uses that same real cold route while preserving actual transfer, cached revisit and replacement-save depletion assertions. The subsequent [source rerun](Verification/FiftyImprovementsII/review-red.xml) passed all six preparation source cases, including both actual Sodden acquisitions and their saved depletion.

## Q1–Q4 self-review before final acceptance

**Q1 — Distinct player decisions.** The 15 outcomes are separate preparation/build choices. Basic shaping is deliberately one outcome despite three inputs. Existing repairs and meal buffs are used as integration controls rather than counted again. New regional gear loses armor, dodge or speed to gain specific protection; new assembly parts exchange accuracy, penetration and Strength contribution. Tests verify the authored arithmetic and actual equipment/brew/crop effects; balance and the frequency of finding each option remain playtest hypotheses.

**Q2 — Source and persistence.** The source tests acquire real generated item owners through existing purchase/container commands and replacement-save their depletion. No renewable loot pools, developer BitLocker grants, or migration of spent saved shelves are added. The first source-test failure after a successful acquisition was an incorrect replay-return assumption, not a broken purchase path. All stock claims remain bounded to newly generated finite regional shelves and lockers. Rendering uses existing compatible original geometry under explicit aliases, not newly authored silhouettes.

**Q3 — Architecture, rollback and performance.** Preparation reuses ordinary menu events, whole-action receipts and the existing forge/brew/enhancement/growth implementations. Compost state uses a reflected public field; transplant moves the exact crop owner and refuses managed reserve beds. There is no new growth clock or inventory capacity bypass. The GasMask bridge traverses the actual body without allocating per-damage equipment collections. New recipe/menu allocation occurs on explicit queries/actions, not Update or a global per-turn scan. The two cold-review findings are bounded by observed RED gates; both corrections followed their observed RED gates.

**Q4 — Honest evidence.** The 18-check witness uses authored items with controlled supplies, stations, crop conditions and elapsed growth, actual commands, paid owner scheduling and full replacement serialization. Its EditMode wrapper passed and verifies caller state preservation. It does not prove a natural campaign journey, keyboard operation, unaided visual identity, combat balance or final Play-session execution. Root's final combined regression and separate Play-session UI/model receipts remain required before claiming those surfaces complete. No Unity operation or commit was performed by this stream.


### Native Play and menu polish

`Verification/FiftyImprovementsII/Native/f73a91c469ca4d4fac171f4321e94149/report.json` passes all 18 controlled preparation checks in actual Play, preserves the active campaign snapshot, and has zero unexpected errors. The ordinary inventory action popup also shapes the supplied timber through native row selection/Enter, consuming exactly one material and one action. Supplies are controlled, so this is not an ordinary source-discovery claim. Root inspected the native capture, shortened new recipe/seed/compost/torch/salvage labels where costs or consequences clipped, and reran the completed Play witness. The revised field-haft row shows its full material cost and Strength cap.

Final combined acceptance: all focused cases from this stream passed together with the 116-fixture affected regression; no requested fixture was unmatched. The final native Play receipt and its controlled-fixture limitations are recorded above and in the master ledger. No production changes followed final Play acceptance.
