# Second fifty — exploration, local services and lasting choices

Status: **complete — implemented, reviewed, native regression and controlled Play accepted.** Final combined evidence is `Verification/FiftyImprovementsII/affected-regression-final.xml` (2,578 passed, zero failed, one existing explicit census skipped) and `Native/f73a91c469ca4d4fac171f4321e94149/report.json` under the same verification directory. Earlier stage receipts below remain an implementation history.

## Source corrections and exclusions

| Initial premise | Verified current source | Design consequence |
|---|---|---|
| New water, crop, fire or reader features are needed | The first Fifty and the new preparation stream already own these | No farming, torch, ordinary rest, inventory reader or general material framework changes here |
| Lockpicking can supply a third entrance | No current lockpick command exists; `LockPart` supports keys | Item 50 uses key, paid locksmith and existing destruction, not a new skill |
| Guest-right is merely local permission | `UnderTheClothEffect` already protects against people and expires by world clock | Locker access grants storage only; no additional peace or faction manipulation |
| Scribe copying already selects a book | `ConversationActions.CopyGrimoire` uses the first eligible original | Item 31 names and revalidates an exact original |
| Rental return already selects one item | `ReturnRentals` walks all eligible carried/equipped rentals | Item 33 provides a single-item alternative; ordinary return-all remains |
| RopeAnchor already supplies travel | `RopeAnchor` is Physics/Render/Examine only; `ZoneManager.RegisterConnection` and stair markers own actual travel | Item 42 must create one real reciprocal connection; no cosmetic shortcut or wall teleport |
| Any carried light illuminates the player | `LightMap` scans equipped item lights | Item 41 produces a real handheld equip option on the same recovered jar, preserving its existing light |
| Pet retrieval needs another upgrade | `AIRetrieverPart` and `GoFetchGoal` already return real thrown objects | Approved substitution for 39: equip a donated weapon/armor on a willing NPC; no dog/fetch work |
| Sella is a trader | PeatCutter is a stationed preparation worker, not a normal Trader | The preparation stream's hood/apron go into the finite works locker, not invented vendor stock |
| New service decoration is enough | Current rendering uses authored surface/equipment libraries | Root must add compatible aliases for any new owner; shipped existing models are reused |

Primary paths below are relative to `Assets/Scripts/Gameplay/`. Read: `Conversations/ConversationActions.cs`, `Economy/RentalSystem.cs`, `RentalPart.cs`, `Items/GrimoirePart.cs`, `TonicPart.cs`, `LockPart.cs`, `ContainerPart.cs`, `Repairs/RepairablePart.cs`, `AI/SpreadCollectorPart.cs`, `SpreadTerritoryPart.cs`, `SpreadGrazerPart.cs`, `AIUndertakerPart.cs`, `Goals/DisposeOfCorpseGoal.cs`, `Effects/Concrete/UnderTheClothEffect.cs`, `World/LightMap.cs`, `World/Map/ZoneManager.cs`, `World/ZoneTransitionSystem.cs`, `World/StairTravel.cs`, `World/Generation/{Quillhold,Tally,Wellmeet,Ginmere,Olderdeep,LastCounter,SoddenDistrict}CompositionPlan.cs` (Sodden is `SoddenDistrictPlan.cs`), and the corresponding builders. Context: `Docs/FACTION-AND-SACRED-POI-AUDIT.md`, `REGIONAL-GUIDANCE.md`, `EXPLORATION-DEPTH-DISTRICT.md`, and root `Lore/Factions/07_TentRight.md`.

## Shared contract

New successful physical commands cost one ordinary action; refused and query-only commands cost none. Every selected item, patient, provider and world owner is exact and current. Service providers must be alive, nearby, willing and unengaged; no forced treatment, equipment theft, remote trade, or global peace. Use `InventoryTransaction` and existing inventory/equipment receipts. Explicit command names carry escaped exact IDs, never list indices. Small local helpers share ownership/refusal predicates, not a general quest or economy framework.

New site installation is cold-only, opt-in for fresh exploration manifest v15, with literal v14 and older save restoration retained. Existing saved graphs are never refilled or retrofitted. Optional packets use only clear validated cells and known existing owners; failure retains the original region. All ordinary borders and stair approaches remain connected. Existing source stock is conserved, including the preparation stream's two added works garments. New owners use native props/models; no full-screen indicators or new map interface.

## 31 — Choose the grimoire the scribe copies

**Outcome/design:** a live Scribe offers a named command for each carried original. Selecting the second book copies that book's actual knowledge/skill/messages, leaving the original and other books intact. Use one carried InkVial and 5 drams for the new explicit service; expose that price in the action. The existing legacy conversation route is not silently repurposed. Quillhold's real Scribe and copy desk give the destination a practical choice.

**Source/files:** `ConversationActions` first-original branch; `GrimoirePart`; new `ScribeCopyServicePart` and bounded copy helper. **Acceptance/counters:** two different originals, select second, exactly one correctly titled/payload-preserving copy; copied books, insufficient ink/drams, stale target and failed outer action do not spend or create. Copy fees and exact output survive save; no free grant from reading a menu.

## 32 — Pay a craftsperson to supply repair materials

**Outcome/design:** Cinderhold's actual Weaponsmith can repair one selected supported damaged portable item for 8 drams while consuming the recipe's real materials from the smith's stock. The player may instead retain money and use their own ordinary repair. Service handles the current three portable repair recipes only; no whole-object HP restoration or unsupported item upgrade.

**Source/files:** `RepairablePart.TryRepair`, recipe registry, existing `InventorySystem` receipts; new `ArtisanRepairServicePart`. **Acceptance/counters:** same Broken item loses its actual penalties, provider material decreases, coins transfer; empty provider stock, sound gear, rental/foreign/equipped aliases and callback refusal preserve exact state. Any narrow repair helper extraction must preserve the direct repair command and its existing adversarial suite.

## 33 — Return only the rental you choose

**Outcome/design:** a Quartermaster's local action lists individual carried or worn rentals and their existing Ink refund. Return one exact owner; keep the other loan and its equipment bindings. Return-all dialogue remains available.

**Source/files:** `RentalSystem.TryReturn`, `RentalPart`, `ConversationActions.ReturnRentals`; new `RentalDeskPart`. **Acceptance/counters:** two different rentals, one worn, selected one returns/refunds once; wrong lessor, stale choice and capacity refusal retain item and Ink. Replacement save preserves the remaining loan.

## 34 — Buy out a useful rental

**Outcome/design:** keep one rented weapon permanently by paying the Quartermaster its current full ordinary buy quote in drams. The prior Ink rental fee is not refunded or converted. Remove only that item's RentalPart after payment; it becomes ordinary owned equipment and can later be sold. This is an explicit new alternative, not a retroactive rental price change.

**Source/files:** `RentalSystem`, `RentalPart.CanBeTraded`, `TradeSystem.GetBuyPrice`; `RentalDeskPart`. **Acceptance/counters:** exact worn weapon, body binding unchanged, full money conserved to provider, rental flag removed once; wrong lessor, underpayment, post-selection price change or outer refusal leaves rental and wallets unchanged.

## 35 — Claim a guest locker at Wellmeet

**Outcome/design:** a new finite empty chest in Wellmeet's guest shelter can be claimed while a living player actually has valid guest-right. Claim opens that exact locker permanently for ordinary deposit/retrieval; expiration later never traps possessions. Stored goods are literal container contents, not a remote bank or insured inventory. Breaking the chest retains normal physical consequences.

**Source/files:** `UnderTheClothEffect`, `PutInContainerCommand`, `TakeFromContainerCommand`, Wellmeet plan/profile; new `GuestLockerPart`. **Acceptance/counters:** cloth permits one claim and real saved deposits; absent/expired/broken oath does not claim, no free items are granted, moved/destroyed locker is not recreated, later expiry does not delete access or goods.

## 36 — Hire a nearby locksmith

**Outcome/design:** an explicitly configured living craftsperson can open one exact ordinary locked container within two cells of them for 6 drams. This gives the player a paid alternative when the correct key is unavailable. Exclude doors, quest/unique locks and any special Curation/Stillleaf authority; never teleport containers or open every lock in a zone.

**Source/files:** `LockPart`, `ContainerPart.IsLocked`, local provider pattern in `SoddenPreparationPart`; new `LocksmithServicePart`. **Acceptance/counters:** selected ordinary chest unlocks, other locks unchanged; wrong provider, remote/unique lock, already-open owner and insufficient funds are free refusals. Rollback restores lock and money.

## 37 — Ask a willing blocker to step aside

**Outcome/design:** a friendly nearby idle civilian can take one real safe adjacent movement step away from the player's current line. Offer only when a valid cell exists; never move hostile, dead, immobile, engaged, dragged or footprint-ambiguous actors. Use ordinary movement vetoes; no swap or teleport. The player spends one action only when the NPC actually moves.

**Source/files:** `BrainPart`, `MovementSystem.TryMoveDetailed`, `SpatialQuery`; new `CivilianCourtesyPart`. **Acceptance/counters:** blocked doorway becomes passable after a real step; sealed neighbors, movement veto, hazardous destination or newly hostile NPC do not move or charge. No new persistent AI duty is created.

## 38 — Treat a willing NPC with your medicine

**Outcome/design:** choose one own carried HealingTonic, Antidote or SoddenFieldDressing and apply its beneficial existing effect to one nearby willing living patient. Patient must need the benefit; no harmful/ambiguous tonic payload, hostile or ongoing fight. Donor spends one real unit, patient receives the existing effect, and no allegiance/reputation reward is invented.

**Source/files:** `TonicPart.ApplyTo(target,user,zone,...)`, existing cure/dressing use paths and `FieldMedicinePart` as actual tonic example; new `CivilianAidPart`. **Acceptance/counters:** patient HP/effect improves, donor stock drops, donor receives no benefit; full health/no applicable status, dressing on gas poison only, hostile patient and outer refusal preserve stock/state. This does not edit enemy medicine AI.

## 39 — Give an NPC equipment they actually wear

**Outcome/design:** select a supported spare carried weapon or armor for a willing living NPC. The whole exact item transfers and ordinary equipment validation installs it on a compatible free Body slot. Occupied slots, forced unequip, rental/quest goods and incompatible anatomy refuse. This offers practical support, not follower recruitment or an automatic best-equipment AI.

**Source/files:** `InventorySystem.Equip`, `Body`, `InventoryPart`, ordinary transfer receipts; new `CivilianEquipmentGiftPart`. **Acceptance/counters:** donor loses exact item and NPC's true Body/equipment bindings gain it, existing gear is unchanged; failed equip, no hand/head/body, reentry and full inventory roll back the gift. Save retains actual worn owner.

## 40 — Inter a carried body at a graveyard

**Outcome/design:** a visible local graveyard offers an explicit respectful deposit for one exact carried ordinary corpse. Move it into the real graveyard container, retaining its identity and any contents; do not destroy it for currency, reputation or a new moral flag. Existing undertakers and deliberate later retrieval remain physical behaviors.

**Source/files:** `AIUndertakerPart`, `DisposeOfCorpseGoal`, `PutInContainerCommand`; new `BurialPart`. **Acceptance/counters:** selected corpse leaves pack once and remains in saved graveyard; living actor, quest/unique specimen, foreign corpse and full/locked graveyard refuse. Outer failure restores source and destination exactly.

## 41 — Recover a light and leave a darker descent

**Outcome/design:** an explicitly marked existing Olderdeep descent BeetleJar can be unhooked and retained as the same portable hand-equipped light. Reuse its existing enabled/radius/intensity data. The old cell loses that light and it is never replaced; player must spend a hand slot to carry useful light. No fuel system is invented.

**Source/files:** `OlderdeepCompositionPlan.UsedDescent`, `LightMap` equipped-light scan, pickup/equip; new `RecoverableLampPart`, compatible BeetleJar equipment alias. **Acceptance/counters:** same owner transfers, ground light disappears, true equipped light works; full pack, occupied/invalid bindings, non-opted jars and outer refusal keep original jar/light. Save/revisit retains dark original seat and real portable owner.

## 42 — Rig one Ginmere rope shortcut

**Outcome/design:** spend two KnotflaxCord at one authored Ginmere anchor to install an exact reciprocal stair connection between two safe authored landings in the current world. Both endpoint graphs/owners must exist and be validated before consumption/registration. Existing ordinary stairs remain; no forced destination generation from menus, no arbitrary-coordinate teleport and no falling simulation. The installed line is permanent and does not return cord.

**Source/files:** `GinmereCompositionPlan`, `ZoneManager.RegisterConnection/RemoveConnection`, `StairsUpPart/StairsDownPart`, `ZoneTransitionSystem`; new `RopeShortcutPart` plus cold endpoint binding. **Acceptance/counters:** real down/up traversal returns to exact reciprocal landing, registry gains only intended edges once; blocked/missing/replaced endpoint, wrong manager, one cord and failed outer action leave no half-connection. Full session save preserves connection/markers/consumption.

## 43 — Salvage a disabled spike mechanism

**Outcome/design:** an already permanently jammed ordinary SpikeTrap can be dismantled for one existing IronSpikeComponent, removing that exact owner. The consumed jam timber stays spent. Armed traps cannot be safely salvaged through this action. Other magical/living trap types are outside scope.

**Source/files:** `TrapJammingPart.IsSupported/IsJammed`, native `HarvestablePart` finite output/rollback pattern; new `TrapSalvagePart`. **Acceptance/counters:** one jammed owner gives one exact output and disappears; armed, removed, duplicate-trigger or second attempt yields nothing. Factory mutation and outer failure preserve trap and quantity; save cannot respawn salvage.

## 44 — Strip an abandoned cloth screen

**Outcome/design:** one authored abandoned textile screen near a frontier store is genuine sight-blocking cover, or can be dismantled into two KnotflaxCord. Removing it changes actual local LOS and exposes the approach. Only this new abandoned owner has the verb; occupied Tent-Right homes are not globally harvestable.

**Source/files:** cloth-material `TentWall`, destructible/thermal cover behavior, `HarvestablePart`; new finite screen blueprint and sparse frontier placement. **Acceptance/counters:** two cord exactly, old cover actually absent; fire/destruction does not also pay dismantle yield, non-opted walls unchanged, rollback and saved depletion hold. Safe ordinary border route remains.

## 45 — Move the works supplies as one physical load

**Outcome/design:** the existing Sodden works locker receives explicit hauling handling, preserving its exact finite contents. The player can open it where it stands and carry chosen goods, or drag the intact loaded container toward shelter at actual strength/speed cost. There is no extra cache or loot grant and no unpack/repack duplication.

**Source/files:** `SoddenDistrictBuilder.worksStock/StockValid`, `DragRules`, `DragSystem`, `ContainerPart`; new local handling opt-in on the exact locker. **Acceptance/counters:** contents remain exact owners through two real haul steps/release/open/save; insufficient strength cannot haul but ordinary opening still works. Partial looting changes subsequent carried load weight consistently, not a fixed empty-shell exploit. Preparation's one FilterHood and one AcidworkerApron join this same stock authority.

## 46 — Pay for one local guarded passage

**Outcome/design:** a new physically present guard at a dry Sodden cutbank side approach can accept 4 drams for one crossing of its small marked territory. Bind permission to that actor, post and player, consuming it only on actual exit after entry. It is not global peace: personal enemies, other hostiles and the existing frog retain normal behavior. The existing long dry route and wet shortcut remain valid alternatives.

**Source/files:** `SpreadTerritoryPart` exact post/rectangle, `FactionManager`, movement/turn events; new `LocalPassagePermitPart` and sparse guard/post placement. **Acceptance/counters:** paid player crosses this one duty zone without duty attack; another player, exhausted pass, moved/dead post, personal hostility and other enemies remain unaffected. Save between payment/entry/exit retains exact remaining state; no second charge on reentry callback.

## 47 — Open a real grazer pen

**Outcome/design:** one sparse optional field pen has an ordinary gate and one actual finite-grazing animal. Opening it lets existing avoidance/grazing behavior act on the real outside terrain; leaving it closed protects the outside row while confining the animal. No simulated herd reproduction, remote grazing or liberation currency reward.

**Source/files:** `DoorPart`, `SpreadGrazerPart.Configure`, finite row sources and existing generation geometry; new cold pen packet. **Acceptance/counters:** closed gate truly contains the animal under actual eight-way movement; opened route is usable, one row is consumed at most once, reserved row remains. Blocked optional fit falls back without moving unrelated owners; gate/animal/row outcomes persist literally.

## 48 — Exchange food for the collector's real find

**Outcome/design:** an explicitly configured passive collector offers its actual carried salvage for one carried edible unit. The deal transfers that exact salvage, consumes one food, then stops the finite collector trip. Do not synthesize loot, buy from an empty bird, remove goods already deposited, or grant speech/lore to all wildlife.

**Source/files:** `SpreadCollectorPart.CurrentCarriedItem`, `Phase/Target/Home`, inventory transfer receipts; new `CollectorBarterPart` only on authored collectors. **Acceptance/counters:** exact item reaches player, food loses one unit and collector no longer deposits it; Seeking/Deposited/Stopped-without-carried-owner, hostile/threatened collector, full pack and callback races refuse/rollback. Replacement save cannot clone the exchanged salvage.

## 49 — Borrow one finite library volume

**Outcome/design:** a small Quillhold loan shelf holds two explicitly authored, singleton existing utility grimoires. Borrow/return uses the existing Ink rental economy and rental sale restriction; the exact physical book returns to stock; these selected ordinary utility volumes grant permanent knowledge and have no charge counter. Empty stock is not replenished by reopening the shelf. Existing public archive contents remain untouched.

**Source/files:** `QuillholdProfileBuilder`, `RentalSystem`, `RentalPart`, grimoire study/stack restrictions; new `LoanShelfPart` and two explicit loan-book blueprints. **Acceptance/counters:** loan removes one exact shelf item, reading teaches the native permanent skill, return refunds once and returns the same physical book; empty shelf/insufficient Ink/wrong owner/duplicate return refuse. Ordinary save preserves shelf and active rental graph.

## 50 — Open one abandoned Counter store by different means

**Outcome/design:** one finite store at an existing abandoned Counter has a correct key available through a sparse reachable exterior clue/cache; a willing local locksmith can instead sell an opening, and existing physical destruction remains possible. All routes expose the same original finite container stock. Breaking the container preserves ordinary spill/destruction consequences, not an extra payout. No lockpick skill or new quest is created.

**Source/files:** existing abandoned Counter landmark placement, `LockPart/KeyPart/ContainerPart/DestructiblePart`, new #36 locksmith service; one cold optional store packet. **Acceptance/counters:** each route reaches the same stock in independent fixtures, wrong key and insufficient fee do not unlock, no duplicate grant after changing routes. All critical borders remain safe and connected; old saves and failed placement retain their original graph.

## Verification and performance

RED tests first use ordinary inventory actions and reflection only to instantiate not-yet-present optional Parts. They assert changed player/NPC/ground state, quantities, fees and opposite refusal cases rather than mere type existence. Later dedicated adversarial cases cover callbacks, aliases, exact owner graphs, payment rollback, multi-cell access and save replacement. Root alone imports/runs native tests. A `FiftySecondExplorationBench` will expose `ExpectedCases`, `RunId`, `Cases`, `Failures`, `Audit` and `Observations`, restore all detached fixture globals, and label direct command/scheduler/save evidence separately from native UI discovery.

Queries perform bounded local scans and never call factories, mutate AI, advance time or generate zones. New events run only on commands or existing bounded actor actions; no Update loop or global world simulation. Dirty only changed owner/cells and affected lighting when a jar moves. Optional placement uses bounded candidates and existing geometry proofs; no rebuilding a live zone on visit.

The initial implementation risks were source-bound articulation of the rope pair, loaded container weight, existing repair/rental receipt composition and actual cold geometry admission. The scoped source and adversarial receipts now exercise those contracts; integrated affected-system and controlled Play acceptance remain separate. A successful command fixture alone does not prove natural discovery, economy balance, all-camera readability or long-term enjoyment.


## Frozen source sweep addendum and integration (before production)

- The selected ordinary utility `GrimoirePart` volumes have no charge field (other charged-book blueprints remain unchanged). Item 49 lends the exact physical volume and reading teaches permanently; Ink rental cost and partial return refund remain native. No spell-charge economy is promised.
- There is no Bandage blueprint. Item 38 permits HealingTonic, Antidote and SoddenFieldDressing. Native Antidote cures ordinary **and gas** poison; the dressing cures ordinary poison and bleeding only. Treatment of a full-health/unaffected patient refuses.
- A PeatCutter is friendly, while `SpreadTerritoryPart` normally selects faction-hostile visitors. Item 46 needs an explicit local unpaid-entry predicate on its new guard. The personal-enemy branch stays prior to the duty exemption, and no `FactionManager` relationship is changed.
- Item 43 produces exactly one existing **IronSpikeComponent**, not valuable ChoirIron ore and not refunded timber.
- `EquipCommand.ExecuteInternal(... allowDisplacements: false)` supplies the ordinary equipment gift operation in the caller's receipt; no independent equipment planner is needed.
- `HandlingService.GetWeight` currently reads only Handling/Physics shell weight. A new opt-in `ContainerLoadPart` makes the works locker's real cargo contribute to hauling weight, including quantities. Ordinary unrelated containers keep their established handling behavior; malformed/cyclic cargo fails closed.
- Existing `AbandonedCounter` is only a small wall-and-bones stamp. The store installer must bind to a newly recorded actual stamp provenance, not infer a store from arbitrary nearby bones or overwrite the old stamp.

**Root-owned data:** `/tmp/fifty-ii-exploration-blueprint-spec.json` supplies ten exact names and compatible model families. `WellmeetGuestLocker`→Chest; `QuillholdLoanShelf`→QuillholdArchiveShelf; `QuillholdLoanWardGleam`/`QuillholdLoanDryingBreeze`→their original grimoires; `FrontierClothScreen`→TentWall; `SoddenPassagePost`→SoddenRouteNotice; `SoddenPassageGuard`→PeatCutter family; `FrontierPenGate`→SpreadFieldGate; `CounterStoreChest`→Chest; `CounterStoreKey`→IronKey. The gate must use real open/closed state. The recovered BeetleJar remains that same owner/blueprint and uses compatible held-light appearance.

**Cold integration:** exploration owns manifest v15 and literal v14 restore admission. After `CommitGeneratedZone`'s existing lair/Sodden/Wayhouse/Spread finalizers accept, a bounded optional `SecondExplorationSites.Install(manager, zone)` adds its sparse packet. Missing blueprints, blocked placement, changed source or failed packet leave the original region. No access/attachment/load hook generates content. A fixed v15 destination retention predicate also joins `CanUnloadZone`, preserving deliberately emptied destinations.

**Fixed locations:** Quillhold 14.9 for copy and library; Cinderhold 6.6 for repair; Tally 10.14 for selected rentals; Wellmeet 8.16 for guest storage; Olderdeep 4.6 depth 1 for one recovered descent jar; Ginmere 2.7 depths 1 and 2 for the rope; Sodden crossing 16.7 for the local cutbank and works 17.7 for the same loaded locker; Spread 12.10 for an optional grazer pen; existing abandoned Counter 19.18 for one store/screen/locksmith packet. All positions are native cells selected deterministically from the actual source layout; source assertions use ordinary seed 64, with geometry/blocked-source counters added before final acceptance. No new settlement or map POI is created.

**Command contract for root input:** `SecondExplorationActions.IsCommand` recognizes successful paid physical commands: CopyVolume, ArtisanRepair, ReturnRental, BuyRental, ClaimGuestLocker, LocksmithOpen, StepAside, TreatPatient, DonateEquipment, InterCorpse, RecoverLamp, RigRopeShortcut, SalvageJammedTrap, StripClothScreen, PermitPassage, BarterCollector and BorrowVolume (selected commands carry escaped exact IDs). Ordinary door, hauling, key, destruction and travel retain their current input timing.

### Initial RED inventory

`SecondExplorationServicesTests`: 22 cases. `SecondExplorationSitesTests`: 13 cases. `SecondExplorationSourceTests`: 17 cases. These compile without production types via reflection and then use real native inventory commands. The source fixtures exercise ordinary generated destinations, actual finite stock/pen references, literal version 14 and an already cached graph. Standalone Roslyn compilation completed with 0 errors; this is **not** native execution or GREEN. This was the pre-production inventory; root subsequently observed its RED receipt, detailed below. Dedicated adversarial, reciprocal rope/territory transitions and replacement-save cases now exist.


## Implementation and verification ledger

The initial native receipt `Verification/FiftyImprovementsII/exploration-visual-red.xml` observed 52 exploration cases: 4 passed and 48 failed at the missing behaviors/source assertions. Production then added the optional service Parts, exact-owner inventory receipt helpers, physical route/object Parts and a v15-only cold installer; root owns blueprint/rendering and ordinary input payment integration.

The first service implementation had 20 / 22 passing service cases and all 13 object/site command cases passing. The two repair fixtures incorrectly used ShortSword, which has not opted into portable repair; they now use the existing Dagger with an asserted native portable recipe. No repair blueprint or supported-recipe authority was broadened to satisfy that fixture. A separate advertised locksmith range-two counter reproduced the accidental range-one predicate before its correction. The next receipt has all 24 service cases and all 13 object/site cases passing.

The cold installer source receipt in `Verification/FiftyImprovementsII/integration-fourth.xml` passed 17 / 18 cases. This includes actual seed-64 Quillhold, Cinderhold, Tally, Wellmeet, Olderdeep, Ginmere, Sodden and Counter owners, the actual finite two-garment works stock, v14 exclusion and cached-graph non-retrofit. The sole pen assertion read raw `Physics.Solid`, although ordinary `DoorPart` owns closed-door movement blocking; the fixture now asserts `IsClosed` and actual cell movement blocking. No gate physics or global door behavior was changed.

Source corrections made explicit during implementation:

| Earlier assumption | Actual source / resulting design |
| --- | --- |
| Existing graveyard generation already supplies item 40. | Undertaker behavior and the Graveyard blueprint exist, but no current generation source supplied it. Root approved one ordinary corpse-storage Graveyard near Tally's rest court, with a subtle factual cue and no reputation/reward claim. The actual cold source has its own assertion. |
| Any manifest whose version is current is an enabled new world. | Legacy/unbound manifests also default their version field to current. A dedicated disabled-manifest counter is staged before adding the required explicit Enabled admission/retention condition. |
| Direct SoddenDistrictBuilder construction includes all v15 stock. | The new stock belongs to the post-acceptance current-world installer. Preparation source fixtures now use the actual detached current manager and preserve ordinary finite acquisition/revisit/replacement-save assertions. |
| Raw door Physics.Solid proves the pen closed. | Ordinary DoorPart and actual cell collision are authoritative; the named gate remains the same real owner and uses existing open/closed rendering. |

`SecondExplorationAdversarialTests` adds 29 dedicated cross-owner, action-boundary, callback, cargo, reciprocal-rope, local-permit and replacement-save cases. The native `review-red.xml` receipt observed all five intended service failures: stunned service users (copy and lamp), aliased/counted factory copies, and harmful tonic mutation between acceptance and commit. Its separate disabled-manifest source case also failed as expected. The local execution-only action veto, exact singleton/identity copy checks, immutable accepted-treatment revalidation and explicit enabled-manifest gate are now implemented. Refused aid beneficiaries still spend already committed medicine but gain no substituted effect; a rejection receipt states why the benefit was skipped. Queries remain pure.

`FiftySecondExplorationBench` exposes 21 bounded native-runtime checks covering the twenty outcomes plus replacement finite aftermath. It uses detached fixture actors, real inventory commands, ordinary owner-turn advancement, real movement/hauling, native stair transition, actual finite grazer behavior and replacement session save. Setup deliberately supplies owners, placement, strength and finite inputs and does not register the helper NPCs in an ordinary encounter schedule. Its explicit scope is controlled runtime behavior, not a natural discovery journey, NPC combat balance, economy balance or broad visual polish. At that stage native execution was pending; the observed final Play receipt is recorded below.


The reciprocal-rope review fixture also exposed a test premise error: `ZoneManager.RegisterConnection` indexes each directed edge under both endpoints. Two reciprocal edges therefore appear as two entries in each endpoint's `GetConnections`, with exactly one outgoing edge per endpoint. Corrected assertions pin both counts and repeat them after replacement save; no connection implementation was changed to satisfy that fixture.

Independent cold review covered service receipts, post-commit beneficiary checks, actual source retention and the controlled native bench. The bench review found no concrete API or detached-state defect; its stated controlled-fixture limits remain. The independent ambient-trap retention counter reproduced a finite-reward addition to an otherwise unloadable graph. Optional salvage is now only installed in an exact already accepted and retained Spread graph or one of the fixed v15 destinations. The new pre-cache identity predicate does not weaken ordinary manifest access validation. Removing the last trap cannot make explicit unload regenerate its finite salvage. Unretained ambient vault traps keep their existing jamming behavior and receive no new salvage verb. This is a scope restriction, not a new persistence framework or another feature count.


### Final review corrections awaiting integrated acceptance

`Verification/FiftyImprovementsII/regional-mesh-red.xml` reported all 29 dedicated adversarial and 19 source cases passing after the first review corrections. The source count includes the disabled-manifest counter, corrected physical pen declaration and actual Tally burial owner. Preparation's six source tests also passed after using the actual post-acceptance world path for Sodden stock.

`Verification/FiftyImprovementsII/travel-retention-red.xml` contains five cases: three passed and two failed at the intended behavior. The unretained SealedVault acquired a new finite salvage reward, and ordinary vertical travel landed at the old staircase `(40,10)` instead of the new rope `(11,10)`. The generated retained dispatch-yard positive control already passed actual jam → salvage → explicit unload refusal → replacement save → unload refusal, with one spike and permanently spent timber.

The fixes are now on disk: salvage admission uses the existing exact retained graph, and rope installation adds the ordinary `StairsDown`/`StairsUp` tags alongside its Parts and reciprocal registry edges. Both marker/tag authorities roll back together if the outer action fails. The native rope witness now includes competing existing tagged stairs, so it cannot pass merely through the empty-zone coordinate fallback. Source compilation completed with 0 errors; all new metadata has valid 32-character GUIDs and the owned source diff is whitespace-clean.

The final exploration fixture inventory is 88 cases across `SecondExplorationServicesTests` (24), `SecondExplorationSitesTests` (13), `SecondExplorationSourceTests` (19), `SecondExplorationAdversarialTests` (29), `SecondExplorationRetentionTests` (2) and `SecondExplorationRopeTravelTests` (1). The 21-case controlled Play witness now passes; integrated native regression remains pending. The 88-case inventory is an authored count until the root records its final affected-suite result.


### First controlled Play receipt and visible-menu polish

The first Play report, `Verification/FiftyImprovementsII/Native/e57bcc38312f455880a418c6796369c8/report.json`, reached the driver checks and recorded **20 / 21** exploration witness cases. The collector case stopped with a fixture NullReferenceException: Magpie has no intrinsic SpreadCollectorPart; authored collectors receive that optional configured role. The fixture now supplies/configures the actual existing role before staging its already-carried salvage, as the earlier passing command test already did. This correction changes no collector gameplay. Root reported no unexpected native errors and restored EditMode.

Root inspected the six actual native captures and found action rows clipped at about forty characters, hiding late prices and consequences. New exploration labels now put drams/Ink refunds and costs first and use shorter fixed consequences. Copying explicitly names **one ink vial**, distinct from the capitalized **Ink** rental currency. Only the exact `Grimoire of ` prefix is omitted from the copy/library menu label; the real owner title, full identity and command ID remain unchanged. Cloth states “+2 cord, lose cover”; lamp, rope and trap rows preserve their physical consequence/cost. Full source examination remains available. This is narrow observed-view polish, not a new UI framework or an all-camera readability claim. The final Play receipt below includes the corrected collector fixture and compact labels; affected regression remains pending.


### Final controlled Play acceptance

[`Native/f73a91c469ca4d4fac171f4321e94149/report.json`](Verification/FiftyImprovementsII/Native/f73a91c469ca4d4fac171f4321e94149/report.json) reports `complete: true`, **0 errors, 0 failures**, and **21 / 21 exploration checks**. The same run passed the 11 driver checks, including four actual key-driven menus, alongside 15 combat and 18 preparation benchmark checks. Root inspected all six final screenshots and confirmed that the compact price/consequence labels were visible. This is recorded human-visible capture inspection, not an automated pixel-quality assertion.

The exploration witness covers exact copy/repair/rental/guest/locksmith/courtesy/aid/equipment/burial commands, retained hand light, competing-stair rope roundtrip, finite trap/cloth output, loaded physical hauling, the local paid passage lifecycle, a real gate and finite grazer row, the collector's exact carried find, permanent study with exact book return, the original keyed cache, and replacement save of finite aftermath. These are controlled detached fixtures with declared supplied resources and placements. Cold generated source admission and existing-system regressions are separate native fixtures. Neither report proves an ordinary expedition, full dialogue discovery, balance, long-term enjoyment, novice comprehension or all-camera polish.

Final independent narrow source review confirmed the retained-graph guard and stair tag/Part rollback match their bounded contracts; no significant remaining issue was identified in that review. The root's 116-fixture affected regression is currently running against this final source. Assets remain frozen through completion.

Final combined acceptance: all focused cases from this stream passed together with the 116-fixture affected regression; no requested fixture was unmatched. The final native Play receipt and its controlled-fixture limitations are recorded above and in the master ledger. No production changes followed final Play acceptance.
