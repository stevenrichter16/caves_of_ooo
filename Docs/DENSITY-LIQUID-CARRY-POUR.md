# C3 follow-up — finite liquid carrying and pouring

## State and invariant

Status: reviewed gameplay, content, stock, tests and native acceptance harness are now published in the shared working tree after the confirmed native UI RED. Native GREEN and the separate identity-aware renderer are pending; this is not yet a visually accepted feature. Historical private-stage evidence below is preserved in `Docs/Verification/DensityCompletion/Everyday/Liquids/core`. The existing clean-water waterskin remains a separate drink vessel. A reusable flask preserves the exact registered liquid identity and transfers a finite volume between an explicit nearby source, itself and the ground. Unlike liquids never turn into clean water or a guessed dominant component. Unsupported mixing refuses without spending contents.

## Verified source sweep

- `WaterskinPart` and `WaterVesselService` implement three drinks, actual finite pool depletion, renewable wells/cistern sources, Parched relief, transaction rollback, nonstacked carriage and one-successful-action UI turn. This is already live and must not be replaced.
- `LiquidPoolPart` holds one `LiquidId` and scalar `Volume`; `LiquidCoveredEffect` is the existing creature contact/exposure abstraction. Entering a pool currently does not subtract physical pool volume. The new transfer contract conserves recoverable pool/flask units; it does not claim conservation of exposure coats, tile timers, heat, or the entire liquid simulation.
- `Docs/LIQUID-COATING-SYSTEM-PLAN.md` explicitly defers proportional mixtures. Existing coating dominance must not be reused as a storage conversion rule: doing so would let a dangerous mixture become pure water. Unlike pools and active coatings on a selected source/destination are refused conservatively.
- Alchemy `BrewingService` consumes reagent items to create finished `BrewedTonic`/sludge outcomes. There is no reusable general liquid flask hidden in that subsystem. This proposal adds no brewing recipes, reagent conversions, bottled status payloads, bottle drinking or shatter-on-throw behavior.
- Inventory actions already accept exact opaque command strings and execute within `InventoryTransaction`, with actor Before/AfterInventoryAction callbacks and post-commit observers. The source and destination selections can therefore retain IDs/cells through the existing inventory menu; a new targeting state is unnecessary.
- `ZoneTileStateSystem.WriteCoating/ResolveAfterAbility` is the existing ground-reaction seam. Pouring can feed current oil/heat, water/heat, water/charge and other data-defined rules. Immediate contact with creatures already on the destination uses the pool's current contact event. Scenery-specific direct dousing is not claimed; this pass uses existing creature-contact and ground reactions.
- Lore/MYSTERY-LEDGER.md is unchanged. Liquid IDs and names come from existing LiquidDefinitions. The new vessel's practical description answers none of the protected mysteries, and no exotic liquid property or explanation is invented.

## Bounded behavior

`LiquidVesselPart`: Capacity12, Volume0, LiquidId empty by default, plain public saved fields. Nonstacking carried vessel; no drink action. Fill consumes min(free capacity, selected finite pool volume), with the pool and vessel both claimed until transaction completion. Repeated same-liquid fills work; unlike fills refuse.

Menus identify each actual nearby pool by ID and expected liquid identity. Pour choices identify the held identity/amount, zone and one of the current/eight adjacent cells. Execution rechecks those selections, actual carriage, physical reach, life state, capacity, registry identity, purity and integer bounds. A missing source cannot fall back to a different nearby pool. A changed flask/zone cannot redirect a stale pour.

Pour empties the flask into one same-liquid pool, or a new generic `PouredLiquidPool`; zero-volume fields on the flask become canonical empty state. Recoverable volume moves inside the transaction; contact/reaction and success publication wait for commit. Staging a new pool also creates its normal tile projection, and rollback removes that projection and restores any prior same-liquid coating lease. Refused or rolled-back transfers leave no new coating, damage or success receipt. Successful pour publishes the existing contact and tile reaction path, followed by success diagnostics/prose. Reactions can change effects/ground presentation; they do not silently alter liquid identity.

The new puddle has no renewing TileStateSource. Its finite recoverable pool volume and ground coating are distinct existing models. Zone.AddEntity already projects a pool as a permanent coating; the explicit four-turn contact/reaction write does not shorten that lease. This pass does not introduce evaporation or change the existing projection lifetime. Contact still uses LiquidPoolPart's current once-on-enter semantics. No proportional mixing, decanting between flasks, custom amount picker, dynamic liquid weight or liquid container breakage is claimed. The flask's authored carrying weight follows the current waterskin convention.

## Concrete safety defect found during RED

An existing waterskin samples a water pool even if acid occupies the same physical cell as another pool or active coating. Both counterexamples executed and failed before changes. `LiquidSourceSafety` is shared by the new vessel source gate and finite-water pools and uncontained renewing springs in WaterVesselService. It checks every occupied source cell; unlike physical pools, authored coating sources or current active coatings refuse sampling. Contained wells deliberately remain clean-water draws despite an unrelated spill around their base. Pure pools and unpolluted renewing springs retain their existing behavior. Portable, inventory-owned and living pool owners are not ground sources for either vessel type. This prevents the new pour path from becoming a route to separated clean water.

## Reviewed files and data

New runtime files: `Gameplay/Items/LiquidVesselPart.cs`, `LiquidVesselService.cs` + fresh metas. Existing runtime edits: bounded ground-source/purity predicates in `WaterVesselService.cs` and read-only live contents in `ItemExamineService.cs`. Planned follow-up: successful command-prefix routing in InventoryUI's existing everyday-action turn branch. These UI lines require native RED before publication.

Root-owned Objects proposals, after review:

1. `LiquidFlask`, inherits PhysicalObject, Item+Tier1 tags, takeable Weight1, one-hand/light handling, glyph `!`, glass-colored render, Commerce8, LiquidVessel Capacity12. No Stacker/Tonic/Waterskin/Reagent/BrewItem. Suggested flavor: “Thick glass and a tight stopper. The narrow neck lets you pour without wetting your hands.” Live content/volume belongs in truthful inspection details.
2. `PouredLiquidPool`, inherits Terrain, non-takeable/non-solid, Render default “poured liquid”/`~`; LiquidPool fields are configured from the exact transferred registry identity/volume. No renewing coating source, structural container loot, rental or crafted provenance. Runtime display uses the existing liquid name/glyph/color.
3. One empty flask in MorrowfastProvisionerStock, ProvisionerStock and WellKeeperStock, following existing guaranteed waterskin rows. No normal BitLocker, hostile item grants or manufactured Choir source.

## Test evidence so far

Initial private core RED:13 executed,12 failed/1 passed (`verification/red.xml`). Ten failed because the general vessel part did not exist; two failed on overlapping unsafe waterskin sources. Pure-water fill/drink was the passing control. Candidate core:13/13 GREEN (`verification/first-green.xml`,0.357437s). Sources cover water/oil/acid, exact selected source, partial finite transfer, same-liquid merge, no unlike-liquid conversion, stale missing/changed source, mixed destination refusal, immediate water contact, and pure-water compatibility.

The staged evidence below records the private progression before publication; later content, UI RED and shared-publication checkpoints supersede its pending statuses. This initial GREEN alone was insufficient to publish. The private standalone runner uses stubbed Unity and stable string hashing and cannot prove native input, actual serialization, renderer presentation, turn cost or player experience.

## Required acceptance gates

- Malformed/unknown/mixed/stacked/dead/detached/refused actions preserve exact contents and volume; field overflow and malicious factory defaults cannot mint liquid.
- Save/load keeps separate flask identities, capacities and contents. Old waterskins remain water-only.
- Transaction rollback and actor veto/throw/reentry preserve both sides; observers cannot undo committed liquid or suppress the failure receipt.
- Actual menu fill/pour spends exactly one normal turn on success, zero on refusal. It closes/reopens naturally; long menu rows remain usable. Live inspection shows identity and volume without mutation or a drink action.
- Real factory content/stock source tests, mixed-water controls and the current everyday/liquid suites pass.
- Native isolated keyboard replay buys a real empty flask, fills an actual source and pours a real destination; record source/vessel/ground quantities and old waterskin compatibility. Natural-route balance remains separate from any disclosed travel shortcut.

## Adversarial review and evidence

| Finding | Actual RED | Bounded repair/control |
|---|---|---|
| Renewing-water spring bypassed unsafe-cell purity | Acid and oil pairs failed | All occupied spring cells checked; clean spring and contained well controls pass |
| Factory puddle began with recoverable volume or another identity | Positive initial volume and nonempty ID failed | Require zero volume and empty ID, fresh nonportable Physics owner before payment |
| Stale pool destination became carried/equipped/takeable | Three ownership cases failed | Reject those destinations before volume mutation |
| Rollback reset LiquidId before removing new pool | Ghost permanent coating and lost prior lease failed | Remove with staged identity first; restore prior same-liquid lease, then restore fields |
| General flask/clean waterskin sampled portable or living pool | Five owner cases failed | Both source routes refuse non-ground owners |
| Inspection omitted current contents | Six missing/malformed detail cases failed | Read live ID/volume/capacity; invalid state says unavailable without repair |

An initial dead-actor test used an integer property rather than CombatSystem's actual `_DeathHandled` tag. Correcting the fixture made that control pass without a production death change. An initial rollback assertion incorrectly expected no pool projection while a transaction was still staged, including a preexisting pool; the revised test checks the actual committed/rolled-back invariant. Both false premises are preserved in the first adversarial receipt.

- `adversarial-red.xml`: 35 total,29 pass,6 fail (includes the two fixture premises above).
- `adversarial-confirmed-red.xml`: 40 total,31 pass,9 confirmed failures.
- `adversarial-first-green.xml`:40/40.
- `followup-red.xml`:53 total,51 pass,2 portable/living source failures.
- `ownership-red.xml`:57 total,52 pass,5 source-owner failures.
- `core-final-green.xml`:57/57, including real token-graph save round trips for empty/water/acid and distinct instances; factory callback and owner changes; read-only menu controls.
- `examine-red.xml`:64 total,58 pass,6 missing-detail failures; `examine-green.xml`:64/64.
- Existing-corpus differential:13 existing test/helper files,287/287 before and287/287 after; **NEWLY FAILING0, NEWLY PASSING0**. Files and raw outputs are in `verification/nearby-*`. Baseline is this slice's copied foundation production with original WaterVesselService and ItemExamineService restored and new vessel classes absent. Current resources/test corpus are identical on both sides. This is a bounded affected regression, not a repeat full-world suite.

At the first native UI checkpoint, eight test-only cases were published: two successful encoded fill/pour cases were expected to fail and six refusal/incomplete/unrelated/old-drink controls to pass. The observed native result is recorded below. Production routing was held until that RED; offline reference compilation was not counted as execution.

## Historical pre-content checkpoint and evidence boundary

Before the content milestone below, the two blueprints and three stock rows still required source/stock RED and surgical review, and the flask had no normal-play source. Those gates have since completed; native player acquisition remains pending. The core runner proves the finite transfer, current authority, rollback, pure-water compatibility and token-graph contracts under its engine stubs. It does not prove Unity input, native save startup/reload, native rendering, exposed inventory timing, actual merchant purchase, visual clarity or feel. No bitlocker or alchemy change is proposed. No global fire scale or steam scheduling change is included in this liquid slice.

## Real content and UI route follow-up

Native UI RED ran under root: exactly2 successful transfer timing failures and6 control passes. Preserved root receipt: `Docs/Verification/DensityCompletion/ReferenceGlade/Art/native-fourth-before-sizing-repair.{json,xml.gz}` (combined run148). The resulting InventoryUI branch recognizes LiquidVesselService's encoded commands only after successful execution. It is now published; native GREEN remains pending.

Actual content RED:11 executed,10 missing blueprint/source failures,1 existing registry validation control. The private append-only Objects edit adds LiquidFlask and PouredLiquidPool, changing **zero of498 existing objects**. Exactly three of98 loot tables gain one guaranteed flask; every prior row is preserved. Parsed hashes/proposals are in `verification/content-parsed-diff.json` and `objects-proposals.json`. `apply_content.py` appends only the two objects and inserts only those three compact stock rows; root owns shared application.

The source sweep also found MorrowfastContent's explicit no-global-TraderFactory fallback. That path now includes the same one flask privately. Both wired and unwired real Morrowfast residents, the real Provisioner and WellKeeper factory objects, and actual TradeSystem purchases pass. Every guarantee is rolled over64 seeds. Direct source enumeration pins exactly the three normal supply tables, excluding manufactured Choir/crafted/rental additions. Native travel/purchase is still pending.

Content+core:75/75 GREEN. Six old exact Morrowfast food-stock pins failed as expected; the pin update adds only LiquidFlask×1, preserving every old item/count and all craft-merchant controls. Combined candidate+restock suite: **93/93 GREEN**. Final affected existing-corpus differential includes those stock tests: **305/305 before,305/305 after; NEWLY FAILING0, NEWLY PASSING0**. Before uses original source/content and original pins; after uses the candidate and only the intentional additional-flask stock pins. This is still a bounded regression, not full native acceptance.

Rendering review confirmed generic pools currently get only registry glyph/color fallback. That is insufficient for native liquid-identity acceptance. Root authorized render_completion to prepare a separate strict exact-PouredLiquidPool cached flat-pool recipe using existing liquid colors, known positive volume/current nonportable ownership and explicit missing/unknown/zero/stale countercases. This rendering implementation and native screenshots remain pending; the generic pool must not be advertised as visually complete before them.

Private publication manifest: `verification/proposed-publication.json`; narrow shared-source/pin diff: `verification/production-and-pin.patch`. Two runtime classes and three core fixtures have fresh hand-written metas. This manifest was prepared while shared gameplay and Objects were unchanged. The later authorized publication is recorded below.

## Finite poured-source lifecycle follow-up

Independent renderer review found that collecting the last unit of a PouredLiquidPool left its empty owner and permanent TileState projection in place; the renderer correctly refused zero volume but the ground-water sheet still had an owner lease. Eleven paired lifecycle checks ran6 intended RED/5 controls. The private fix retires only the exact current, nonportable, nonrenewing PouredLiquidPool owner after a successful transaction commit; Zone.RemoveEntity removes its owned projection and retains another live same-liquid owner's coating. Authored natural sources keep their existing identity/lifetime policy. Both general flask fill and clean waterskin fill use the same bounded cleanup. Rollback retains original owner, volume and projection.

Final focused/content/stock corpus is104/104 GREEN. A fixture correction replaced substring source-ID matching with exact escaped command-token matching after the co-located natural-source control exposed ambiguity; this was a test selector issue, not a production source-selection defect. The private native driver now asserts exhausted-owner and permanent-projection absence, saves that absence, creates a new actual puddle by native pour, and verifies both old/extraneous puddle IDs are absent after F6. Observation records include sourcePresent and captures label liquid plus owner ID. Native-reference driver compile has zero errors; no native liquid Play run or rendered acceptance is claimed. The exact existing comparison remains305/305 before and305/305 after this cleanup, with0 newly failing and0 newly passing.

## Shared publication checkpoint

The authorized narrow publication adds two runtime classes, three core fixtures, five small existing source/pin changes, the four-file native driver/launcher, and two launcher-restoration controls. `published-data/content-parsed-diff.json` proves exactly two new blueprints with all498 prior objects untouched, and exactly three normal-supply tables changed with the other95 untouched. The manual Morrowfast fallback matches the table supply. No SaveSystem, door, renderer, alchemy or BitLocker changes are included.

Final private checks are104/104 focused and305/305 on each matched existing side, with zero newly failing. Root executed the earlier native UI RED (two intended timing failures, six controls). The separate renderer26 native RED has two positive failures and24 refusal/natural controls; its implementation is owned independently and pending here. The native driver uses real provisioner stock and real finite source pools with a labelled travel shortcut, normal money/HP, exact fill/pour/rollback-compatible state, and real F5→native mutation→F6 replacement graph checks. No native liquid Play or rendering acceptance is claimed yet.

Q1–Q4 closeout: both flask and clean-water waterskin retire only exhausted exact poured owners after commit, retain rollback and natural-source identities, and preserve other live projection owners; all public command tokens are revalidated without fallback. Current identity/volume inspection is read-only and malformed contents refuse. Existing stock pins changed only by the one actual flask, with source and fallback controls. Remaining native visual/input/source proof is explicitly a gate, not inferred from private tests.

## First native acquisition replay and harness repair

Run `NativeLiquids/e17faf63283a4e36b544a11fdc9ca722` passed17 named checks: ordinary purchase of the actual flask/waterskin, finite water and oil transfer, inspection, unlike-menu refusal, and true F5/native mutation/F6. It stopped at the bounded acid-source precondition;21-check completion is **not** claimed. The raw report, exact editor-log range and restoration remain preserved. Water’s first screenshot framed the earlier merchant area despite the new player/puddle coordinates, so its filename and measured volume did not establish visual acceptance.

The harness search incorrectly spent12 of24 acid slots in Beating, whose surface table has no AcidPool; only the other12 Sodden slots could succeed. Missing per-owner refusal data prevents assigning that remaining miss to absence, purity or approach safety. The harness-only correction searches the actual bounded Sodden surface/ordinary underground domains and records each matching owner’s quantity and refusal. It retains the original purity and32-cell hostile-clearance rules.

Morrowfast uses a separate authored presenter. The same honestly purchased, filled flask is therefore carried to a real supported ordinary dry zone for the pool-model gate. Native Look/Escape resets stale merchant camera focus; native Look focuses the poured owner. Each pool image requires its current native owner, exact water/oil/acid model, submitted geometry and fully enclosed positive viewport bounds. The original remote water source remains part of the F6 quantity proof. No scene authority, source, quantity, item, money, HP or AI changes are made by this repair. Only the driver and receipts changed; reference compilation passes, native retry remains pending.

### Actual Unity core/UI result

The native 494-case integration selection passed the exact liquid fixtures: 62 adversarial, 11 content, 13 service and 8 inventory-UI cases (94 total). The poured-model fixture passed 25 of 26; its remaining failure compared float components by exact object equality after material serialization (0.33 versus 0.329999983). Native round-trip precision was measured and the fixture now uses a component tolerance of 0.000001; production color validation was already using Unity color equality. Corrected native GREEN and the repaired full acquisition replay remain pending.

### Native follow-up selection: 193/193 GREEN

Actual Unity job `4ccacd6728a04704aab763829b3c2f23` completed all 193 cases with zero failures or skips: 137 material/outlier/neighbor checks, 26 poured-liquid rendering checks, 4 seventh-grass geometry checks and 26 combat-witness checks. Raw XML is preserved at `Verification/DensityCompletion/Integration/native-seventh-liquid-material-combat-green.xml.gz`. This closes those focused unit gates; the separate Play routes and visual comparison are still required.


## Native integration and registry-model correction

The authoritative494-case integration run confirms **86 liquid core plus8 liquid UI cases,94/94 GREEN**. This count does not fold in unrelated stock, bed or restoration fixtures. The source is `Integration/native-seventh-liquid-bed-door-material-red.json` and its by-fixture extraction; expected door/material RED remained separate.

Retry `87d076a84c094281a9334ac067a5ca43` completed6 positive checks, including real purchases and conserved water fill/pour, then stopped at its model expectation. Actual poured owner4614 in supported `Overworld.2.6.0` had LiquidId water, glyph `~`, color `&c`, volume12 and native model `poured-liquid-63`. This matches the current authored water definition. The harness incorrectly expected `poured-liquid-42`, derived from an earlier mistaken `&B` premise. The private repair reads the exact registry definition and compares its selected model; it leaves identity, quantity, source safety and viewport checks intact. Original failed report, images and log remain evidence; no successful final liquid replay or water screenshot acceptance is claimed.

For this retry the parent observed restored settings, preferences and scene, but ordinary ReferenceGlade Play subsequently reentered without an audit driver. The playing state differed; its cause remains unresolved, so this report does **not** claim exact final restoration. The editor owner is investigating before further native activity.


Publication checkpoint: the reviewed follow-up is now in the shared working tree; native GREEN/replay remains pending. The door mesh-registration production hunk and all harvest timing production remain held for their native RED gates. The registry-only liquid audit correction is published; no optional visual guards were included.

### Current native source-selection correction

Run `e28fb38d1f45474b880faf99e1b69c01` passed 17 checks through purchased
water handling, exact save/load, mixed-liquid refusal and oil carry/pour/model
inspection. Its acid search found 30 genuine AcidPool owners but rejected every
one because the audit demanded 32 cells of hostile clearance; this is a harness
assumption failure, not a missing source. All raw candidates and the failed run
remain preserved, with exact editor restoration and full log range.

For source sampling only, the audit now requires a named 12-cell minimum
separation and records every actual hostile's identity, position, speed and
configured tactics. Filling costs one ordinary action before transfer away.
The separate visual inspection site retains the original 32-cell guard. No
hostile, terrain, HP, item or scheduler behavior changes; per-key ordinary HP
checks and exact 40HP finish remain. This is not a guarantee against ranged
attacks, and any actual damage or failure must remain visible in the next run.
Native success after this correction is still pending.

The corrected actual route completed **21/21 PASS**, zero unexpected errors,
46.14seconds: `a513c5b79c7c4c83982ee93ec3db5d0b`. It selected a real AcidPool
at Spread-adjacent Sodden12,2 with nearest hostile17cells away; no source or
hostile was rewritten. Ordinary40/40HP and exact purchased inventory identity
survived water, oil, acid, F5/checkpoint mutation/F6 and all transfer costs.
The report includes every selected-source hostile and preserves the earlier
32-cell over-filter failure. Exact editor restoration and full log are archived.
Rendered water/oil/acid models are script-verified; visual frame review is
recorded separately in the poured-art document.
