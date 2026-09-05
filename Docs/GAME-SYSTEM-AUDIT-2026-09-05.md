# Whole-game system audit and repairs — 2026-09-05

Status: **INITIAL45-SYSTEM SCAN COMPLETE; WAVE1 SHIPPED; WAVE2a COMPLETE; WAVE2b COMPLETE; WAVE2c COMPLETE; WAVE2d NEXT**. Authorized by the user after completing
Felling W6. Baseline `599a042d`, branch `claude/game-lore-analysis-jqa7ur`,
7674/7674 tests GREEN; W6 native14/14. The daily change ledger is
`Docs/WORK-LOG-2026-09-05.md`.

## Contract and finite scope

Inventory covers all741 C# files under Assets/Scripts (578 Gameplay,
163 Data/Presentation/Shared/Scenarios), plus22 editor-support sources and
runtime-loaded JSON/art mappings. The source manifest is
`Docs/Verification/GameSystemAudit/source-inventory.json`. A category being
inventoried is not a claim that its behavior has been reviewed or is correct.

First scan every category and record evidence, counterexamples and repair
plans. Then implement the verified plans in bounded TDD waves, followed by
independent review, adversarial gates, appropriate native/performance checks
and same-commit living docs. No user intervention is needed for ordinary
repairs. Root implements; delegated work is read-only analysis/review.

Existing/concurrent work is snapshotted at
`/tmp/codex-game-audit-preexisting` (1027 paths). Never reset or stage it
wholesale. Shared tracked files may receive only proven incremental repairs;
staging must preserve the prior diff. Earlier uncommitted spell-animation
work is included in observation, not silently adopted as this audit's work.
Intentional future features are not dead-code bugs. A dead-code removal needs
consumer/registration/reflection/save reach checked before deletion.

## System checklist

All45 categories received targeted producer/consumer and existing-test review.
Detailed coverage, limitations and repair candidates are recorded below. This
is not an assertion that every one of741 files was read exhaustively.

| ID | System / principal surface | Runtime entry or consumer | Existing test anchor | Review |
|---|---|---|---|---|
| G01 | Entity/event substrate | Entity, Part, pooled GameEvent | EntitySystem, FireEventMutationSafety | initial scan complete; findings below |
| G02 | Opening and progression | NewGameLoadout, LevelingSystem | AlphaOnboarding, LevelingSystem | initial scan complete; findings below |
| G03 | Scheduler and world clock | TurnManager, TickEnd, AdvanceClock | TurnManagerAdversarial, WorldClock | initial scan complete; findings below |
| G04 | Movement/physics/slipping/hauling | MovementSystem, PhysicsPart, DragSystem | TurnMovement, LiquidSlipAdversarial | initial scan complete; findings below |
| G05 | Anatomy and equipment | Body, equipment plans, limb maintenance | BodyPartSystem, Tier1BodyRoundTrip | initial scan complete; findings below |
| G06 | Combat and death | CombatSystem, on-hit, death rewards | CombatSystemSpec, CombatAuditBugfixAdversarial | initial scan complete; findings below |
| G07 | Skills and abilities | SkillsPart, ActivatedAbilitiesPart, dispatcher | Wsp84SkillSystemAdversarial, SkillCrossInteraction | initial scan complete; findings below |
| G08 | Grimoires/rites/resonance | GrimoirePart, SpellSkillPart, ResonanceSystem | ConsumingRiteHypothesis, RitesPort | initial scan complete; findings below |
| G09 | Status lifecycle | StatusEffectsPart, Effect hooks | StatusApplicationFacade, EffectTickOnApplyTurn | initial scan complete; findings below |
| G10 | Thermal/material reactions | ThermalPart, MaterialSimSystem, resolver | MaterialSystem, StatusGateUnification | initial scan complete; findings below |
| G11 | Gas simulation | GasSystemPart, OnTickEnd, gas behaviors | GasSystemAdversarial, GasCrossPhaseAudit | initial scan complete; findings below |
| G12 | Liquids and coatings | LiquidPoolPart, LiquidCoveredEffect | LiquidCoatingAdversarial, LiquidConsequences | initial scan complete; findings below |
| G13 | Durable terrain state | ZoneTileStateSystem, reaction/propagation | ZoneTileState, TileReaction, ColdTileBridge | initial scan complete; findings below |
| G14 | Inventory ownership/transactions | InventorySystem, commands, validation | InventorySystem, EnhancementDispatchTransaction | initial scan complete; findings below |
| G15 | Consumables/harvesting/locks | InventoryAction handlers | ThrowableTonic, LockKeyBumpUnlock, BiomeHarvest | initial scan complete; findings below |
| G16 | Enhancements | ItemEnhancing, equipped modifiers | EnhancementDeepAudit, E3MineralsAdversarial | initial scan complete; findings below |
| G17 | Brewing | BrewingService, rule resolver, knowledge | BrewingAdversarial, BrewingBatch | initial scan complete; findings below |
| G18 | Tinkering | TinkeringService, bits/schematics | TinkeringService, TinkeringCommand | initial scan complete; findings below |
| G19 | Forging and tempering | WeaponForgingService, WeaponTemperingService | WeaponcraftAdversarial, ForgeCommands | initial scan complete; findings below |
| G20 | Commerce and rentals | TradeSystem, RentalSystem, restock | RentalSystemDeepAdversarial, TraderStock | initial scan complete; findings below |
| G21 | AI/navigation/companions | BrainPart, goals, pathfinding, party transit | GoalStack, CombatPathfinding, FollowerSystemAdversarial | initial scan complete; findings below |
| G22 | Factions/law/reputation | FactionManager, PlayerReputation, GroveLaw | FactionAI, GroveLaw, OathAdversarial | initial scan complete; findings below |
| G23 | Grid/cache/generation/population | Zone, ZoneManager, builders, habitat | GridSystem, ZoneGeneration, UndergroundGeneration | initial scan complete; findings below |
| G24 | Traversal and discovery | WorldMapTraversal, stairs, HelmwoodPassages | WorldMapTraversal, StairTravel, HelmwoodPassage | initial scan complete; findings below |
| G25 | Destruction/loot/triggers | DestructionSystem, drops, TriggerOnStep | DestructionAdversarial, LootOverhaulAdversarial | initial scan complete; findings below |
| G26 | Authored W1–W6 places/ecology | profiles/formations/nests/barrenness/archive | W5Adversarial, FellingW6Adversarial | initial scan complete; findings below |
| G27 | Look/light/world actions | LookQueryService, WorldInteractionSystem, FOV | LookQueryService, WorldInteractionSystem, LightMap | initial scan complete; findings below |
| G28 | Farming | CropSystemPart, seed/plot/access/growth | CropsWateringAdversarial, FarmingAuditFix | initial scan complete; findings below |
| G29 | Dialogue | ConversationManager, predicates/actions | Conversation, NoFightConversation, NarrativeConversation | initial scan complete; findings below |
| G30 | Facts/knowledge/quests/storylets | NarrativeStatePart, world parts, storylets | NarrativeReactor, QuestObjectiveHypothesis | initial scan complete; findings below |
| G31 | House dramas | HouseDramaRuntime, witness/pressure facts | HouseDramaRuntime, HouseDramaValidator | initial scan complete; findings below |
| G32 | Settlement services/rest | SettlementManager, RestSystem, wells/plumes | SettlementManager, BiomeRest, FoundingRest | initial scan complete; findings below |
| G33 | Persistence/session restoration | GameSessionState, SaveGameService, finalize | SaveLoadAdversarialSweep, SharedReferenceIdentity | initial scan complete; findings below |
| P01 | Blueprint data/factory binding | BlueprintLoader.Bake, EntityFactory | EntitySystem, blueprint part-order checks | initial scan complete; findings below |
| P02 | Conversation/faction content loading | ConversationLoader, FactionLoader | Conversation, CanonFactionRoster | initial scan complete; findings below |
| P03 | Population/loot registries | PopulationTable, LootTableRegistry | BiomeLootTable, LootOverhaulAdversarial | initial scan complete; findings below |
| P04 | Bootstrap and restored-runtime wiring | GameBootstrap.DoStart/ApplyLoadedGame | Alpha/save integration, scenario benches | initial scan complete; findings below |
| P05 | Input, boot/death/save/pause state | InputHandler and pure controllers | SaveLoad/BootMenu/DeathScreen/PauseMenu | initial scan complete; findings below |
| P06 | Gameplay UI and modal flows | inventory, trade, dialogue, targeting panels | InteractionReturnState, WorldActionReach | initial scan complete; findings below |
| P07 | World rendering/invalidation | ZoneRenderer, ZoneRenderHooks, CP437 | Rendering policy, stale-background, state builders | initial scan complete; findings below |
| P08 | Sprite/environment/camera presentation | visual catalog, sprite layers, CameraFollow | Environment/EntityVisual/CameraFollow tests | initial scan complete; findings below |
| P09 | Spell/world FX and settings | coordinator/playback/renderers/settings | WorldFxCoordinator, SpellFxShowcase | initial scan complete; findings below |
| P10 | Diagnostics/performance/utilities | Diag, DiagQuery, profiler markers, dice | Gameplay/Diagnostics, PerformanceDiagnostics | initial scan complete; findings below |
| P11 | Scenario infrastructure | ScenarioRunner/context/builders | ScenarioCustomSmoke, scenario harness | initial scan complete; findings below |
| P12 | Editor adapters/menus/native launchers |22 support sources, explicit scenario launch | deterministic native verification reports | initial scan complete; findings below |

## Verification sweep and constraints

CLAUDE.md and ADVERSARIAL_TESTING.md are the methodology sources. This is
CoO-original repair work, with no automatic Qud-parity claim. Historical
ALPHA/PALIMPSEST/audit backlog rows must be reconciled with current consumers;
their text alone does not prove a current defect.

| Recorded fact / risk | Required correction to audit assumptions |
|---|---|
| No standalone Core folder; namespace code lives throughout Gameplay. | Use the source manifest and runtime chains, not directory names alone. |
| EntityFactory ignores unknown parameters/conversion failures. | Validate actual authored keys and instantiated values; existence of a blueprint is insufficient. This failure mode is already recorded. |
| Saves are strict version7 and omit self-describing reflected fields. | Treat v8/migration as explicit planned compatibility work, not a silent schema change. |
| Native/MCP GUI tests are unreliable and WebSocket errors poison tests. | Keep the user's settled-server headless loop; compiler errors invalidate result XML. |
| Fungal self-cloud and ns-scale diagnostic performance tests are recorded flaky. | Preserve raw failures and inspect causality; do not hide/relabel them as this audit's new discoveries. |
| W6 already repaired fresh depth3+ stair pairing and preserves cached removed markers. | Do not rediscover that repair; inspect remaining generic/surface boundary contracts separately. |
| W6 deliberately defers Singer alarm, advanced flight, library quest/key and god-clock endings. | Do not count intentional later-phase content as accidental dead mechanics. |

Recorded repair candidates to reconcile: Wall-Catching's three blockers,
ShouldInterrupt LOS, follower war-laundering, descent climb cost, v8 saved
fields, destroyed-camera teardown, ChargingStrike refusal after movement,
raw skill movement hooks, ordinary Physics-only solidity disagreement,
material replacement placement failures, and the known flaky fungal fixture.
Recorded visual/design debt: summit cloud, Tank-Brocchinia affordance, GrainRidge
glyph divergence and live look-passes. These are not newly discovered bugs.

## Findings and plans

Pending the category scans and player-flow reproductions. Each accepted item
will contain trigger, observed/expected outcome, code evidence, countercheck,
smallest repair, validation, related recorded debt and status. Scope is finite:
review every checklist row, implement accepted repairs, then close with explicit
limits. A passing scan does not prove absence of every possible bug.


## Initial source findings — reproduction queue

These are source-backed repair candidates, **not yet test-confirmed or fixed**.
A RED fixture must establish the real consumer path and paired control before
implementation. IDs remain stable across waves; a refuted item is retained
with its counterevidence. All paths below are relative to Assets unless noted.

### Inventory, crafting and session scan (root)

| ID / surface | Trigger and evidence | Smallest planned repair / countercheck |
|---|---|---|
| A01 / G14–15 | DropCommand and DropPartialCommand ignore Zone.AddEntity false. A carried FlowerCharm is vegetation; dropping on Felling barren ground removes it from inventory and reports success without placing it. | Honor refusal and roll back the existing transaction, including stack/equipment state. Actual flower/barren RED; ordinary floor, full/partial stack and equipped controls. |
| A02 / G15 | TonicPart advertises world Apply/Drink; ConsumeItem only removes from inventory. The last ground tonic can apply repeatedly. SeedPart already guards the same world-action seam. | Require carried ownership for consuming inventory actions (or explicitly implement one-unit ground consumption if the world contract requires it); retain nonconsuming thrown payload. Actual world-menu/control and repeated-use RED. |
| A03 / G14/G17 | StackerPart compares blueprint/display name, but BrewingService names omit potency. Same-effect/form brews at different potency can merge and replace one payload. | Compare functional brewed payload (including healing dice) in stack identity; identical payload must still merge and split correctly. Real resolver recipes + payload application RED. |
| A04 / G20 | TradeSystem.SellToTrader ignores trader Inventory.AddObject false, yet transfers currency and announces sale. Buy and rental return have rollback. | Refuse/roll back transfer without payment or equipment loss; ordinary capacity success and NoTrade controls. Verify current authored trader capacity before classifying runtime vs API defense. |
| A05 / G14/G33 | InventoryPart._appliedHandlingCarryPenalty is private and omitted by explicit save handler, but Speed.Penalty is serialized. Next inventory mutation after load applies the carried penalty again. Stack decrement/split also skips refresh. | Reconstruct the applied tracker from loaded carried items without reapplying persisted stat change; refresh when stack quantities change. Carry/drop/save/reload/repeat controls and unrelated penalty preservation. No save-format change. Current blueprint scan found no authored CarryMovePenalty values; this is an existing supported handling mechanic/bench seam, not a claim about every shipped pickup. |
| A06 / P04–05/G33 | Boot N only dismisses menu after ResolveActiveGameIDOnBoot selected the previous slot. New world can quickload/death-load the prior character before its first save. | Commit new-session slot selection and initial save when N starts the new world; preserve old slot. Controller + bootstrap integration RED with isolated test save root/preferences. |
| A07 / P04–05/G33 | Bootstrap CaptureGameSessionState hardcodes selectedHotbarSlot=0; loaded value is never applied to InputHandler._selectedHotbarSlot. Serialization test only proves field round-trip. | Capture live selection and restore through validity-checked input API, refresh display. Nonzero/empty/invalid slot and replacement-actor controls. |
| A08 / G33 | GameSessionState.Load clears FX, changes TurnManager.Active/log/reputation before validating final footer. A late corrupt save can fail after mutating the old live session. | Stage or restore global state on failed decode; publish loaded state only after validation. Corrupt footer/truncated late body preserves old globals; valid load restores saved ones. The separate apply-callback partial-failure debt must be classified honestly. |

Root coverage reviewed: G14 command validator/executor/transaction, pickup/drop/
partial/container/equip/throw and custom action lifecycle; G15 Tonic/StatusTonic/
CureTonic/Harvestable/Seed/Lock consumers; G17 resolver/service/preview/batch/
rollback; G18 craft/mod/disassembly and bit yield; G19 forge/reforge/temper/
preview; G20 buy/sell/rent/return/restock; G28 planting/access/growth/moisture/
maturity; G33 token graph/explicit inventory serialization/session decode/
slot service plus P04 bootstrap and P05 input/boot/death/pause controllers.

Additional scoped observations: authored SpeedTonic/StrengthTonic have no
positive Duration; permanent StatBoost is not being relabeled a missing timed
refund. CropSystemPart already gates actor TickEnd to player cadence; CropSystem
and CropPart prose still says every actor and needs correction. Current crop
produce is not vegetation, so maturity's ignored placement return is a latent
extension risk, not a proved current harvest loss. Reforge rollback can fail
to remove a returned component after it merged, but the failure requires a
missing recorded component blueprint; classify after a current save/content
reproduction, not from hypothetical corruption alone.

### Shared substrate, combat and AI candidates (read-only review)

| ID / surface | Trigger and evidence | Smallest planned repair / countercheck |
|---|---|---|
| A09 / G04 | DragSystem.ValidateLink has no production callers. DragPart.AfterMove can re-add a removed load or transfer it into another zone while it remains indexed in the old zone. Existing tests manually call validation. | Release links at removal/transition and validate before following; clear speed penalty. Destroyed/removed actor and load, cross-zone and healthy-link controls. |
| A10 / G05/G16 | Body.UnequipSubtree clears limb/drop state but does not reverse equip stat/armor penalties, enhancement hooks or dismembered EquippedItems cache entries. | One forced cleanup per distinct item; correct destination and equipment notification. Multi-slot, engraved/lacquered/bonus item and untargeted-limb controls. |
| A11 / G05/G06 | Mortal Body.Dismember calls HandleDeath with null killer while HP may be positive. Input death detection uses HP; resulting removed player can miss death UI and NPC cuts lose kill attribution. Axe_Dismember applies bleeding after mortal removal. | Normalize death state and pass source through actual dismember paths; suppress postmortem status. Mortal/nonmortal, sourced/unsourced and ordinary HP death controls. |
| A12 / G03/G09 | BeginTakeAction death removes actor but scheduler still returns dead player/input or dispatches NPC TakeTurn/EndTurn. BrainPart itself already refuses removed actors, so zombie AI attack is not the claim. | Revalidate actor after begin hook before dispatch/input; dead-player and side-effecting NPC probe with surviving controls. |
| A13 / G25/G06 | RuneFlameTriggerPart/RuneFrostTriggerPart use untyped damage while neighboring traps use Heat/Cold attributed damage. | Use typed damage and avoid status after lethal hit; resistant/nonresistant/immunity/source controls. Furniture trigger eligibility remains a separate design hypothesis. |
| A14 / G21/G22 | Existing KillGoal never rechecks UnderTheCloth protection; prior mid-hunt test only queries nearest-hostile discovery. | Recheck oath protection before existing pursuit attacks, clear protected target. Actual queued goal/turn RED; beast, expired oath and intentional faction-blind Bloom controls. |
| A15 / G21/G24 | WorldMapTraversal ascends/descends only player; ordinary transitions transfer party. Recruited follower is stranded after map travel to a different parasang. | Explicit map-travel companion carry/placement policy preserving saved ownership; no free-moving map creatures. Real route, same-cell return, unrelated NPC and refused arrival controls. |
| A16 / G27 | LookQueryService and world-action cursor can read live hidden cell contents/status regardless visibility. | Gate live inspection/action listing by visibility; preserve approved remembered terrain/map information. Hidden/visible/explored/unexplored and reach/action controls. |
| A17 / G32 | SettlementManager.ConsumeInventoryItem removes whole matching stack for one repair; authored SilverSand/FireClay/WardOil inherit Stacker. | Consume exactly one and refresh inventory state. Count1/count3, wrong item and no-repair controls. |
| A18 / G29 | CopyGrimoire ignores AddObject false and announces receipt; authored Scribe advances success dialogue. | Make delivery failure visible and keep retry/result prose consistent. Full pack and successful gift controls, no duplicate reward. |
| A19 / G31 | IsPathClosed has no production callers; AdvancePressurePoint ignores closed paths. Vex harvest closes FounderLetter:LetterBurned but Kess threat remains available and executes. | Enforce path closure in runtime and matching conversation predicate/action feedback. Actual authored sequence before/after closure; unrelated path controls. |

### Presentation/data/editor candidates (read-only review)

| ID / surface | Trigger and evidence | Smallest planned repair / countercheck |
|---|---|---|
| A20 / P10/P12 | DiagQuery.Count omits CauseTraceId used by Apply; three query/count/assert adapters expose no cause filter. | Matching predicate and cause_trace_id parsing across adapters. Matched/unmatched/omitted cause count and sample parity. |
| A21 / P10/P07 | PerformanceDiagnostics.BeginFrame in renderer LateUpdate resets counters recorded earlier in Update. | Own reset before producers or frame-aware lazy accounting. Early producer counted once; next idle frame0; session totals preserved. |
| A22 / P11 | Scenario EntityBuilder/ZoneBuilder ignore placement refusal and may register phantom creatures. | Return refusal before post-placement work/registration. Barren vegetation vs ordinary cell, no phantom roster entry. |
| A23 / P06 | Pickup/ContainerPicker/Dialogue display letter choices that collide with G/J/K controls; authored elder has12 choices. | Shared collision-free label/dispatch map, preserve navigation/cancel. Every advertised choice reachable; Escape/arrows controls. |
| A24 / P06 | Inventory command refusal only logs Debug warning while fullscreen UI hides world log and closes popup. | Persist visible error/status until appropriate next action; success clears stale failure. Refusal keeps state; exact reason and success controls. |
| A25 / P07 | HotbarRenderer clears/writes identical two-row tilemap each renderer frame, without required content fingerprint. Cost not yet measured. | Native baseline first, then fingerprint/layout invalidation if justified; selection/cooldown/viewport/Clear redraw and identical-frame no-work controls. |
| A26 / P12 | DiagQueryTool reports UTF16 string length as byte budget. | Count UTF8 bytes. Multibyte/ASCII boundaries and truncation controls. |
| A27 / P12 | Pass7/Pass8 sprite importer menus reference24 missing old paths; live assets moved under Resources. | Verify atlas intent then repair paths or explicitly retire obsolete menus; never rerun destructive imports blindly. All paths resolve, slicing retained. |
| A28 / P12 | DefaultSceneLoader considers an untitled scene unusable even with roots and opens default scene without dirty/save guard on reload. | Preserve authored/dirty untitled scenes; only select default for genuinely empty clean startup. Editor harness reproduction and pristine-scene control. |
| A29 / P11/P12 | CropFarmShowcase/RentalTestBench are visible attributed scenarios with no launch-menu consumer. | Add explicit launch entries if intended visible; hidden helpers excluded by menu coverage pin. |
| A30 / P02/P06 | Five __end__ targets close through unknown-node fallback instead of supported End; completed quest log displays ID where active view displays title. | Surgical sentinel edits/content validation; share quest display-name resolver with missing-definition fallback. Low severity, no change to quest outcomes. |
| A31 / P08/P09 | Already-recorded destroyed-camera teardown callback uses CLR null-conditional on destroyed Unity object. | Unity-aware validity guard; cancellation/disposal with destroyed and live camera controls. Earlier spell work remains protected. |

P-row review coverage: P01 static schema454 blueprints/4555 bindings no malformed
bindings found (approximate source parser, not factory runtime proof); P02
63 conversations/13 factions, registrations/references checked; P03 85 loot
 tables and population gates reviewed, no new authored reach defect; P06 full
modal/command/render paths; P07 dirty/FOV/lightmap/world and HUD lifecycle;
P08 13 visual profiles and asset dimensions checked; P09 53 spell definitions,
pooling/cancellation/FOV/settings paths; P10 record/query/count/performance;
P11 scenario lifecycle and builders; P12 all22 editor adapters/menus/launchers.
This scan does not establish visual feel or absence of all possible bugs.


## Repair wave ordering and pre-implementation corrections

Implementation remains root-only; reviewers stay read-only. Each wave runs
RED→minimum repair→focused GREEN→dedicated adversarial gate→independent
cold-eye/design review→fix→full suite→commit with the daily ledger updated.
Player-facing waves also receive a deterministic native scenario; hot-path
changes get the required75-second before/after evidence. No visual-feel claim
is inferred from numeric success.

1. **Diagnostics correctness**: A20/A26. Exact cause filter across runtime
   count/query and three actual editor adapters; UTF8 response budget. No
   per-frame/per-turn hooks or gameplay behavior, so no hot-path/native gameplay
   profile is required. Tests call the actual HandleCommand adapter via reflection
   because EditModeTests.asmdef intentionally excludes editor/Newtonsoft refs.
2. **Inventory and transaction integrity**: A01–A05/A17/A18, including real
   brew and repeated mineral-infusion stack identity. Bounded transaction rollback,
   loss/refusal/ownership and save-derived handling tracker. No new art/content.
3. **Actor lifecycle and combat**: A09–A14, with killing-blow rider semantics
   preserved. Distinguish ordinary weapon riders (intentionally dispatch on
   killing blows) from a skill attaching new status after its own decapitation.
4. **World/social mechanics**: A15/A16/A19 plus generic depth2 pairing and
   recorded faction/travel contracts once current evidence is reconciled.
5. **Session persistence and restoration**: A06–A08 and save-state debts with
   explicit compatibility decisions. Never silently change strictv7 bytes.
6. **Presentation/editor reach and performance**: A21–A25/A27–A31, including
   observed-cost baseline before any hotbar optimization and Unity teardown.
7. **Remaining material/movement/dead-mechanic repair plans**: findings from
   final category review and recorded-debt verification. Intentional later-phase
   content remains excluded; genuine disconnected current contracts get wired
   and tested. Close only after accepted repairs and final coverage review.

| Sweep correction | Evidence and decision before code |
|---|---|
| Count and Apply intentionally have different limits. | Count ignores Filter.Limit; retain uncapped count while matching predicates. |
| Query budget covers the inner payload, not the MCP envelope. | DiagQueryTool serializes responseData; test exact UTF8 size of that same contract, preserve envelope. |
| Editor adapter tests cannot directly import editor/Newtonsoft assemblies. | EditModeTests.asmdef explicit references; reflection invokes real adapters without modifying shared assembly config. |
| Generic diagnostic docs overclaim array-kind, Unix windows, projection and cursor. | Current parameter schemas implement scalar kinds and turn windows only. Correct shipped-vs-planned prose alongside A20, without silently implementing unrequested schemas. |
| Body combat deliberately fires on-hit riders on killing blows. | CombatSystem SM7/B1 comment; do not globally suppress riders while fixing mortality. |
| Mineral stacking defect has an actual paid path. | Repeated PaleSalt tinker infusion allows2 Parts but suppresses duplicate adjective; once/twice infused dagger has same name and different damage. Include functional enhancement identity in A03. |
| Wall-Catching's third recorded blocker is already fixed. | OverworldZoneManager.PrepareZoneForAccess warms sinkhole depths1–2; real Cathedral access has stairs. Only one Cathedral and no fall trigger remain from that list. |
| Generic depth2-first ordinary caves still lack pairing. | Sinkhole warmup is scoped; StairsUp preregistration starts>=3. Expand to>=2 after RED, preserve depth1 surface contract/cached removed endpoints. |

A32 / G24: ordinary non-Sinkhole depth2 generated before depth1 lacks Up; later
parent omits Down when cached child has no Up. Repair starts fresh deferred
pairing at depth2, since parent1 is underground. Counterchecks top-first,
parent-after-save, removed cached marker, and no invented depth1 surface mouth.

A33 / G30: StoryletPart snapshots current-stage eligible objectives but advances
immediately after the last required objective, before later optional entries
can finish. EnchiridionQuest JSON demonstrates the order but default-world
producer reach is unproven; classify as registered engine contract, not an
observed normal-world lost reward. Stage-scoped batching/deferred advance must
also prevent same-ID snapshot crossover into a later stage. Counterchecks
optional false, manual old-stage completion refused, and exactly-once rewards.


Diagnostics sweep correction A20b: Diag.BufferCapacity is8192, but Apply,
Count and InspectRecord snapshot only5000 based on a stale1024-capacity
comment. Retained older records are invisible to all three tools. Include
full-capacity snapshot scans in wave1, with early retained record and causal
ancestor REDs, ring-rotation eviction controls and Count ignoring Limit.
Eight hex trace characters are32 bits, not16; correct the stale comment
without changing identity generation.

World/social category scan now complete (G21–24/G26–27/G29–32). Producer,
consumer and existing-test paths were read; no new independent W1–W6 authored
place defect beyond cross-row findings. A17 also covers ConversationActions
TakeItem/TakeItemWithTag (one keeper donation currently removes a GrimoireCopy
stack). A33 additionally probes NPC-caused objective completion awarding the
player's reward to the NPC; shipped normal-world attachment unproven, label
engine contract rather than observed default-world failure.


Recorded movement verification (before repairs):
- A34: ChargingStrike commits movement then returns no_target false; actual
  SkillsPart/InputHandler pipeline therefore skips cooldown and turn cost.
  Repair result reflects committed movement; blocked zero-cell activation remains
  a free refusal unless the existing design explicitly requires a spent attempt.
- A35: Charge/Disengage/Vault/Slam/Tumble use raw Zone mutations and miss movement
  completion effects. Use voluntary/forced movement facades as appropriate;
  atomic Tumble needs a shared completion seam after both placements commit.
  Revalidate position/liveness after each step, because water/runes/slip/hauling
  can move or kill the actor. Vault skips intervening hazards but checks archive
  geometry. Counterchecks cover actual pool/rune/render/drag entry and forced
  stunned targets vs voluntary refusal.
- A36: movement callsites use terrain IsSolid and miss Physics-only furniture,
  while BlocksMovement honors it. Repair movement legality consumers, not the
  meaning of IsSolid for unrelated generation/LOS. Real Millstone, wall, loose
  loot, ignored-mover and archive controls.
- A37: Tumble declares AdjacentCell but ignores ctx.TargetCell and selects first
  adjacent Creature. Actual input resolves chosen cell. Honor chosen target;
  two-neighbor/direction/control and invalid-target no-swap RED.
- Wall-Catching remains a deliberately unimplemented feature requiring a second
  authored Cathedral and fall trigger; correcting its stale third blocker is
  accepted documentation work, not permission to invent the unresolved design.
- CoO active Charge/Tumble design is local, not exact Qud parity: local Qud
  ChargingStrike is a priority marker and Tumble is passive DV. Jump's Qud
  landing-event path supports the completion-hook principle only.

G13 A38: ZoneTileState.Tick emits OnCellChanged only when the entire cell
state becomes empty. Expiring embers over retained ice/water produces no dirty
notification after blanket end-turn redraw removal. Notify once for any changed
visible layer/energy state. Mixed-layer expiry, fully empty, permanent unchanged
and one-notification-per-cell controls; actual rendered selected layer check.


### Wave1 RED checkpoint

2026-09-05 21:14:41–42UTC:12 focused tests,11 expected failures and1
unfiltered control GREEN, zero compiler errors. Actual adapter reflection
resolved correctly. Failures confirm cause-filter count/schema/adapter gaps,
full-retained-buffer query/causal reach and UTF8 budget refusal. Raw
`Verification/GameSystemAudit/GA01-red.xml.gz` retained unchanged. Production
repair now follows these confirmed invariants; no test failure was an assembly
or fixture-loading error.


G10–G13 final scan complete: thermal/fuel/reaction lifecycle;9 gas definitions
and player-gated spread/exposure;26 liquids with coating/status/reflect/rewind
consumers; sparse tile writes/propagation/reactions/decay/render/save.13 material
and7 tile reaction IDs resolve. No additional source-proved gas release blocker.

A39 / G10/G13: conductivity/combustibility authored in historical fractions
(OldWorldPipe .95, ordinary metal .8, Tree .45, barrel .7) conflicts with current
percentage/gate consumers (conduction /100; tile capability >=50; new CopperPipe
100/DryBrush85). Define each field separately and reconcile actual content after
RED. Do not globally scale brittleness/volatility, whose consumers remain
fractional. Intended fire capability at Tree45 versus barrel70 must be explicit.

A40 / G12: LiquidCoveredEffect.OnStack reconciles stats/water when dominant ID
changes, but not owned light. Stronger lantern coating fails to light; replacement
of lantern retains stale light. Repair owned-light transition only, preserving
preexisting source. Both directions/weaker overwrite/save/end controls. Lantern
is authored showcase content; default-world access remains unproven.

Initial scan completion precedes production repairs. RED diagnostic tests were
written as reproduction work while the last read-only report was finishing;
no production implementation occurred before every category report arrived.


### Wave1 focused/adversarial checkpoint

Focused12/12 GREEN at21:16:13UTC; expanded69/69 GREEN at21:18:52UTC,
including29 dedicated adversarial cases and28 existing diagnostic tests.
No compiler errors. Dedicated cases cover all cause/filter intersections,
null-turn exclusions, actual editor adapter conjunctions, exact empty causes,
query-vs-count limit boundaries, nested scopes, retained/evicted ring history
and budget narrowing/override. These29 are already-correct regression pins
after the confirmed repairs, not29 additional bug discoveries.

### Inventory verification corrections (before wave2)

A02 extends to real FoodPart and InkVialPart: both advertise ground actions,
apply reward/healing and only remove the last unit from inventory. Repeated
world use can heal or mint rental Ink. Share carried-consumption ownership and
single-unit helper; enforce at execution before any reward, hide invalid world
rows, and retain nonconsuming throwable tonic payload. Schematic consuming-study
requires the same contract if enabled; nonconsuming study is intentionally
allowed to remain readable.

A01 rollback must preserve identity: AddObject can merge a restored equipped
item into a carried stack, leaving a zero-count reference for the later equip
undo. Restore the exact removed reference/index/back-pointers without merging
or capacity checks, then undo equipment. A04 sale must keep unequip+transfer
in one transaction; the existing public UnequipItem commits separately and
cannot provide refusal atomicity.


Further inventory sweep evidence: actual trader inheritance caps inventory at
150, so A04 selling to a full merchant is current-world reachable. Actual A03
brewing pairs: GlimmerBrine alone Acidic2/Electrified2 versus +SparkRoot
Acidic2/Electrified3 have identical names; MendleafSprig+EmberFruit and
CandyHeartRoot both name “mending & mending tonic” but heal1d4 versus2d4.
Compare effect payload, tonic healing, and enhancement multiplicity/config,
not just the first enhancement Part. A paid twice-infused PaleSalt item is
meaningfully different from once-infused despite its identical adjective.


A01 reachability correction before implementation: the actual FlowerCharm Part
belongs to FlowerField, whose Physics.Takeable defaults false. No blueprint
named FlowerCharm exists. A deliberately carried flower fixture proves Drop's
placement/rollback API contract, but does not prove a legitimate default-world
pickup route. Keep the refusal repair and label this boundary honestly; do not
claim a normal picked-up flower was lost in observed gameplay.

Wave1 full suite:7715/7715 GREEN at21:19:34–21:21:04UTC, zero compiler
errors, +41 tests from7674 (12 regression,29 adversarial). Asset GUID audit:
2306 unique, zero collisions. Independent cold-eye review pending; only comment
and documentation accuracy edits followed the full run.


Wave1 independent cold-eye: no must-fix code defect. Corrected stale SinceTurn
wall-clock guidance and wrote linked report before commit. A20/A26 are verified
complete, including full retained-buffer subfinding A20b. Full7715GREEN;
41 added tests. Next: split inventory wave into small coherent repair commits,
starting carried consumable ownership, then transfer/stack/donation integrity.


### Wave2a plan and verification sweep — carried consumables

Scope: A02's reusable ground Tonic/Food/InkVial, plus the supported optional
single-use Schematic contract. Shared InventoryPart one-unit consumption will
also serve later handovers; A17/A18 and transfer/stack identity ship separately.

Invariant: a consuming action needs one positive unit actually carried by its
user, and spends it before delivering its benefit. Ground/other-owner/stale/
zero-count references cannot heal, boost stats, apply status, grant Ink or teach
single-use recipes. Refused action returns an unhandled event/false command
result, not false-handler-as-success. Actorless action declaration can remain
available for metadata callers; actual actor-aware world menus hide the row.
Nonconsuming Tonic.ApplyTo remains the thrown payload path; ordinary reusable
schematic study remains readable without consumption.

Verified APIs: InventorySystem.GetActions supplies Actor; WorldInteractionSystem
also supplies Actor. Most existing action tests already insert items into carried
Objects. Inventory.Contains includes equipped items, so it is too broad for this
gate. Public StackCount stays a field for v7 reflection compatibility. Unit
consumption refreshes handling penalties; last-unit removal keeps its detached
entity count convention. No blueprint/art/save-format changes.

RED cases use actual HealingTonic/StrengthTonic/PoisonTonic, Starapple and InkVial
blueprints in real zones and action facades; menu absence and repeat direct
execution, carried count1/3 controls, public consuming Tonic bypass and thrown
payload controls. Dedicated adversarial cases cover other owners, stale
backreferences, invalid quantities, removed callbacks, save/reload consumption,
full inventory, immunity and unrelated action dispatch. Native keyboard scenario
will verify visible world-menu refusal and pickup→consume through runtime input;
observable checks are separated from visual feel.


Wave2a initial RED at21:29:10–11UTC:19 tests,16 expected failures and3
controls GREEN, zero C# errors. Ground menu/direct-action/public consuming-API
failures reproduce the ownership exploit; stale last Food/InkVial references
also remain reusable. Tonic's nested ApplyTonic probe confirms benefit callbacks
currently run before payment (the command executor turns the probe assertion
into a failed result). Added separate RED single-use schematic/ordinary-study
controls before touching that supported optional path. Raw runs are archived.


Wave2a expanded RED:22 cases,17 failures/5 controls at21:30:15UTC.
Minimum GREEN335/335 at21:32:07UTC. Dedicated33 adversarial cases initially
GREEN with22 regressions (55/55 at21:38:59UTC). Independent review found that
refreshing carry penalties during consumption exposes A05's transaction rollback
tracker mismatch: count1 restoration can double the penalty immediately; count3
restoration leaves a stale tracker that doubles on a later refresh. Add four
outer-rollback RED tests before repairing structure→stats→tracker restoration.
This is pulled forward from A05 because it directly affects this helper.
Review also requires explicit rejection diagnostics on actual mutation attempts,
with no menu-query spam; add reason/quantity/ownership counterchecks.

Wave2a review RED at21:42:01–02UTC:12/12 failures, zero compiler errors.
All four corrected carry rollback cases reproduce; the first run's two tonic
fixtures added a duplicate HandlingPart, so they failed their own precondition.
Corrected fixtures configure the already authored Part; both raw runs retained.
Two save/load quantity cases also reproduce A05's missing loaded tracker; this
portion is included now because consuming a loaded handling-bearing stack uses
that same derived state. FinalizeLoad rebases after the graph is available,
without new serialized fields or another stat delta. Six rejection-diagnostic
cases reproduce the missing observability. No menu-query records are emitted.


Wave2a native first batch9/9 passed (run037765ef099c43438d92cac880a211a6,
3.542090542s). Full7793 run had7792 pass and one existing Campfire flicker
wiring test failure: its30 Render events run synchronously at the same Time.time,
despite the fixture's comment claiming30 frames. Investigate/pin deterministically;
raw failed run is retained, not reported GREEN.

Native cold-eye found the ordinary menu launch skipped isolated save setup,
so N could become movement and bootstrap could save the synthetic arena on a
no-save run. Share isolation/cleanup for menu launch and test no-boot-menu input
as a native RED branch. Category scenario is disabled by default; add a RED
record counter with enabled-channel control, scope its enablement to each record.
Correct single failed native assertion double-counting in the driver.

Broader preexisting A41 transaction debt: action snapshots restore item + actor
stats but omit Ink properties, effects/cures and learned recipes. No production
Before/AfterInventoryAction callback was found, so normal-world triggering remains
unproven; test exception/outer-transaction contracts as a separate repair wave.
Direct public consuming Tonic.ApplyTo has no transaction: exceptions may spend
payment. Normal UI uses the command facade. Do not claim complete effect rollback.

Native no-menu fixture correction: omitting the marker did not produce a clean
boot; ResolveActiveGameIDOnBoot falls back to another saved expedition. The
9/9 result580c1aa7c72f4330865a52697bf12dc2 is archived as a fallback control,
not no-menu proof. Keep the isolated marker for every launch. The no-menu
fixture now explicitly dismisses that modal with a native N during setup;
the actual audit must then avoid sending N into normal movement.

### Wave2b verification plan — single-unit handovers and honest dialogue

Next scope: A17/A18. Read SettlementManager.ConsumeInventoryItem, the actual
ConversationActions TakeItem/TakeItemWithTag/GiveItem/CopyGrimoire handlers,
ConversationManager.SelectChoice, authored Farmer_1/Warden_Lantern_1/Scribe_1/
WellKeeper_1 and courier action chains. Qud corroboration is local
`/Users/steven/qud-decompiled-project/XRL.World.Conversations.Parts/TakeItem.cs`:
Amount defaults1, required failure prevents entering the success element, and
SplitStack/removal must actually supply the requested quantity. CoO keeps its
own donation semantics; no NPC-receives-item parity claim.

Corrections before implementation:

- Logging GetDisplayName after decrement describes remaining `(x2)` rather
  than one donated unit. Use a unit name independently of the remaining count.
- Authored repair confirmations can be stale: ResolveSettlementSite returns
  false internally, but void action lists still enter AfterOvenRebuild or
  AfterReforge, then award Villagers+10. Count-only repair is insufficient.
- TeachBaker/TeachWarden split stage change from a later TakeItemWithTag.
  Their copy requirement and single-unit payment must be checked/committed
  together. TeachCaretaker is intentionally free; WellKeeper's separate
  donation remains a distinct handover.
- Existing GiveItem feet fallback ignores Zone.AddEntity refusal. A shared
  delivery helper must distinguish carried/ground/refused and check both
  placement results. Ordinary GrimoireCopy is a valid nonvegetation Item;
  barren-vegetation refusal is a generic-helper control, not a real book rule.
- Preserve Register/Execute/ExecuteAll compatibility: StoryletPart and legacy
  quest OnEnter also call the void API. Add result-bearing required actions
  and stop later dialogue actions/navigation on failure. Do not claim rollback
  of already committed StoryletPart transitions.

RED fixtures: actual material repairs count1/count3 and stable/no-op state;
losing material/copy after choice display; authored teaching count1/count3;
free caretaker control; TakeItem/TakeItemWithTag positive stacks after a zero
stack; actual courier delivery side effects blocked when payment fails; Scribe
copy with full pack and real ground, plus missing zone/listener membership.
Preserve copied knowledge and SkillClassName. Native conversation/menu checks
and a dedicated adversarial file follow successful minimum fixes.

Wave2a final focused94/94 GREEN (21:57:11UTC). Final new case count83:
22 regression +56 dedicated adversarial +5 staging/record cases. Native active
boot modal10/10 and already-dismissed isolated modal10/10 GREEN; raw IDs and
honesty bounds are in `Verification/GameSystemAudit/GA02a-REPORT.md`.
Both independent reviewers cleared their must-fix findings after correction.
The complete full run is in progress; no full GREEN claim yet.

Wave2a complete: full7798/7798 GREEN,21:59:24–22:00:58UTC, zero C# errors,
+83 tests from7715. A02 complete; A05 load/rollback tracker portions complete,
other quantity-mutating services remain queued. A42 campfire Render wiring
fixture is now deterministic (no production flicker change). Independent
inventory/native reviews cleared all must-fix findings. Final native10/10 in
both isolated startup states. Next implement the verified wave2b plan above.


Wave2b RED implementation starts after reading the cited CoO/Qud sources and
actual FriendlyNPCs/Wardens confirmation/reward chains. Further verified seam:
IfHaveItem/IfHaveItemWithTag currently accept zero stacks; align positive-unit
queries with execution, retaining valid later matches. Entity.GetDisplayName
unconditionally appends `(xN)` for stacks, so one-unit prose must use its base
Render/blueprint/ID fallback without mutating quantity to format a name.
First RED suite uses actual blueprint items and loaded authored conversations:
6 material repairs,4 donations,4 stale repair confirmations,6 teaching payment
cases and3 copy delivery outcomes (23 cases). No production changes yet.

Wave2b initial23-case RED:15 confirmed failures/8 controls GREEN, zero C#
errors (22:07:11UTC). Full content-chain sweep also found two quest offers
start their quest BEFORE GiveItem (CurationSorter/ConcordFactor). Required
handover failure would still leave those starts committed. Add RED total-
refusal and valid-feet controls, then reorder those two authored chains so
cargo delivery precedes quest start. Other authored handover chains either
put payment first or are the two teaching chains already covered. Add paired
zero/positive predicate tests before changing item availability queries.

Wave2b expanded31-case RED:19 failures/12 controls at22:09:57UTC. Minimum
focused118/118 GREEN at22:13:45–46UTC, zero C# errors. Required handlers
return local rejection reasons; TryExecuteAll stops later dialogue actions,
while legacy void ExecuteAll keeps its established contract. Default content
uses required item actions only in conversations, not Storylet OnEnter arrays.
Independent cold-eye found one authored location contradiction: Palimpsest's
TemporalShard gift follow-up still says in-your-hand after ground fallback.
A paired full/available-pack test is written before neutralizing that message.

Wave2b adversarial RED62:56 passed/6 failed (22:18:41–42UTC). One real new
failure is Palimpsest's contradictory in-hand follow-up. Five are fixture
corrections: SealedBogTakenBody is authored nonstacking (three courier setups
incorrectly required a Stacker), and FlowerField has zero weight (two placement
cases never reached capacity refusal). Correct courier to one actual body;
its empty-quantity case explicitly models a malformed extension. Force the
zero-weight flower's generic fallback with an already over-capacity pack, in
both barren/open controls. No authored default-world body-stack claim.
Palimpsest's action message becomes location-neutral; dialogue voice is unchanged.

Wave2b corrected62/62 GREEN,22:19:59–22:20:00UTC, zero C# errors. Native plan uses actual Scribe/Farmer conversations in StartingVillage, full carried pack with MendingRiteGrimoire/OvenBuildersGuide/FireClay3, ground-copy delivery and a successful-then-no-op oven repair. Native keyboard selections resolve authored targets/actions and actual UI reveal state. Six staging/diagnostic tests precede scenario implementation. No ordinary per-frame/per-turn production work is added; the temporary native coroutine is removed with its isolated play session. No performance-improvement claim.

Wave2b native staging six missing-type tests confirmed RED22:27:05UTC; implemented bench/temporary keyboard driver/isolated batch and menu launcher. Focused68/68 GREEN22:30:19UTC. Independent final production review cleared all must-fix findings; all22 authored required-action chains contain their one required action first. The native run is in progress.

Wave2b native review caught a fixture-only modal assumption: dialogue queues announcements until it closes, so the driver cannot drain the copy/repair notice while talking. Native first run confirms this failure; preserve raw report/log. Correct driver to Escape before draining when leaving a conversation, and leave queued notices alone during the Farmer reward/refusal sequence. Production modal policy remains unchanged.

Corrected native modal route exits0. Keep this run as a control; reviewer identified a nonvacuity gap: unchanged node/state alone cannot prove repeat Enter executed. Add a fresh matching ConversationActionRejected record requirement for the exact player/Farmer/action/argument/reason, with event-channel preference restoration, plus literal KnowsMendingRite knowledge checks before final native claim.

Wave2b final native31/31 PASS, run71429a9c069d48cf9c57d7831e8f1929,7.724744625seconds,exit0/zero C# errors; independent native review clears all findings. First full7865/7866 has only the pre-recorded fungal self-cloud flaky failure22:34:12–22:35:50UTC; preserve raw failure and repeat full unchanged.


## Wave2c — transfer conservation (verification sweep / planned next)

Scope: A01 dropped-item placement and transfer rollback; A04 trader capacity
refusal; A05 refreshes directly in these transfer/split rollback paths. Broader
crafting quantity refresh and functional stack identity remain separate waves.
No new content/art/save fields. This is local ownership integrity, not an exact
Qud trade-capacity port. Existing sprite-bearing Dagger/Torch/Sack/Merchant
provide real positive/refusal fixtures. No ordinary hot-path expansion planned.

Root read DropCommand, DropPartialCommand, PutInContainerCommand, ContainerPart,
InventoryPart, InventoryTransaction/Executor, UnequipCommand, equipment split
rollback, TradeSystem and TradeUI.ExecuteTrade. Qud Inventory.AddObject's NoStack
option (235–305) corroborates preserving identity when required; its broader
receive/event framework differs. Current CoO has a hard trader MaxWeight150.

Corrections before implementation:

| Earlier assumption | Verified correction / impact |
|---|---|
| Returning a refused item with AddObject restores it. | It can merge an equipped unit into a matching carried stack or refuse capacity; restore exact identity/index/count/backreferences before equipment undo. |
| Removing an inserted item undoes container insertion. | Fully merged input is no longer in Contents; partial merge also changes preexisting counts. Capture/restore destination structure and quantities. |
| Chest can be the native underfoot-container fixture. | Chest is solid; actual generated Sack is nonsolid, MaxItems6 and usable through the at-feet inventory menu. |
| Vegetation provides an ordinary pickup/drop failure. | All31 such fixtures are not takeable. Deliberately carried FlowerField plus barren ground is a synthetic API refusal control, not a normal pickup claim. |
| Equipped sale is an ordinary TradeUI row. | UI lists carried Objects only. Equipped sale remains a supported API/rollback control; full trader + carried Dagger is native-reachable. |
| Sale refusal already has accurate UI feedback. | TradeUI hardcodes cannot-afford for every failure. Preserve bool compatibility and propagate the actual refusal reason to its status text. |
| Throw quantity restoration still needs refreshing. | Current throw paths already refresh. Limit initial A05 repair to confirmed partial-drop/equipment split restoration; other crafting paths stay queued. |
| A notifying StackCount property is a safe global fix. | Public-field reflection is used by save/cloning; retain the field and refresh authoritative owners at mutation seams. No authored CarryMovePenalty exists, so handling values in controls are explicitly synthetic. |

RED before production: real Dagger3 equip1 → full Sack refusal preserves
carried2/equipped1 exact identities; ordinary space control succeeds. Actual
Torch2 + input3 and Torch98 + input3 container insertion followed by injected
outer failure restores both sides; commit controls retain intended merges.
Full/partial drop respects rejected barren placement and valid nonvegetation/
open-ground counterparts. Actual Merchant carrying50 Torches refuses a Dagger
at150 weight without payment/loss;48 Torches accepts at148 and pays once.
Equipment sale controls cover bonuses, matching carried stacks and unequip
veto. Transfer quantity/undo checks retain unrelated Speed penalty7 and assert
handling3×2 →2×2 on successful partial drop, restored on refusal/rollback.

Then dedicated20–60 adversarial cases, native full-container/full-trader paths,
independent taxonomy/Qud-contract review, full suite and same-commit docs.

Wave2b COMPLETE: unchanged full repeat7866/7866 GREEN,22:36:28–22:37:58UTC, zero C# errors. +68 cases; native31/31 and both independent reviews clear. A17/A18 closed within the report’s explicit ordered-refusal bounds. Proceed to wave2c transfer-conservation RED fixtures.

Wave2c begins afterb468e614 (7866 full GREEN). Root's required inverse-source
read also found the same merged-destination undo in Pickup/TakeFromContainer,
and GoldCoin's special credit path ignores zone membership/removal. Record
**A43 — acquisition source/rollback integrity** as a follow-up producer/consumer
verification task; loose-object API assumptions and actual UI reach still need
confirmation. Do not silently broaden the initial disposition/sale wave.

Wave2c initial20-case RED confirms9 failures/11 controls,22:43:22UTC, zero C# errors: trader capacity2, merged container undo2, equipped-unit identity1, handling refresh2, rejected drop placement2. Add paired actual TradeUI sale-status tests before its failure-feedback change.

Wave2c expanded22-case RED confirms10 failures/12 controls22:44:41UTC; added
TradeUI capacity-message failure. Minimum implementation adds a short-lived
item-list rollback receipt (entries, counts, backreferences; no payload/effect
rollback claim), uses it after unequip and before disposition, checks ground
placement, and joins sale delivery/equipment/wallets in one command transaction.
Partial drop and equipment split rollback refresh the authoritative inventory.
TradeUI receives the actual sale refusal explanation through a bool-compatible
overload. Initial focused verification is next; adversarial/review/native remain.

Wave2c minimum338/338 GREEN22:48:12–13UTC, zero C# errors. Independent
cold-eye found two introduced callback regressions: whole-list undo resurrects
an independently dropped second item, and nested same-item sale during
AfterUnequip can commit then be re-equipped by outer rollback. Also add missing
disposition outcome records. Dedicated adversarial RED cases precede corrections:
limit receipts to transfer-caused membership/count changes; guard participating
items across nested disposition/equipment calls while a transaction remains
active; preserve different-item independent work. Probe malformed quantities,
self-sale, save/capacity, multi-slot bonuses, vetoes and exception rollback too.

Wave2c dedicated33-case adversarial sweep plus22 regressions:43/55 passed,
12 failed22:56:02–03UTC. Six missing outcome-record assertions, three callback
regressions (same-item sale/drop and unrelated-item resurrection), two invalid-
quantity sales and one self-sale confirmed. Corrections seal receipts around
only the immediate Add/Remove mutation; restore changed entries rather than
entire lists. Transaction-scoped item claims reject separate reentrant commands
for participating items, allow direct same-transaction equipment operations,
and release on commit/rollback. Weak-key claims avoid retaining abandoned
entity graphs. Required new gates record reentrancy/disposition/sale reasons.
No public Part/save fields change. Claim placement also covers equip/unequip
and fresh equipment split identities. Further focused verification follows.

Wave2c corrected371/371 focused GREEN23:00:18UTC, zero C# errors. Native
plan preflight: actual Sack6 distinct items at feet, Dagger3 equip1 leaves
distinct carried2/equipped1; full Put refusal then native loot SilverSand to
free a slot and successful put. Underfoot Player+Sack opens pile summary, so
use native PickCell → PickTarget:Sack → OpenContainer. Actual Merchant holds
Starapple99+51 at authored capacity150; failed sale carries an exact status
and new rejection record; buy51 then sell Dagger2 with real price/wallet checks.
Seven staging/diagnostic RED tests precede native scenario implementation.

Final protocol review added a concrete independent-command affordability
case: a different-item nested sale during AfterUnequip legitimately commits,
but can exhaust the same trader's purse before the outer sale pays. Add RED
insufficient-for-both plus sufficient-for-both controls before a late funds
recheck. Add a claim-before-CanBeTraded callback pin and strengthen the existing
TradeUI case to retry on the SAME refused screen. Native staging7/7 missing-
type RED23:02:33UTC is confirmed; scenario implementation waits for this fix.


### Follow-up acquisition and stack-identity preparation (read-only complete)

A43: facade documents pickup from the zone; inspected positive fixtures place
the item. Preserve adjacent pickup, reject absent/wrong-zone/self-carried/other-
owned sources, and revalidate after pickup hooks. No ordinary repeated-key
gold duplication is demonstrated because successful popup rows disappear. Stale
references remain API regressions: GoldCoin3 wallet20 →35 once; repeated input
refuses, distinct coin succeeds. Reject count0/−1 and checked overflow; gold
currently skips pickup hooks, ignores RemoveEntity and lacks transaction undo.
Outer failure must restore wallet and exact coin/cell. If hooks are unified,
pin gold vetoes and callback source revalidation before altering semantics.

Acquisition receipts: Starapple carried2/source3 and carried98/source3 fit
default capacity150; outer rollback must restore both sides of full/partial
merges. Preserve committed merges. Merchant Starapple99+1 with player52 refuses
purchase99 at151 weight but current source re-add turns original99→1/sibling1→99.
Player51 is the success control at150. Container take has the same exact-source
refusal issue. Adopt transaction item claims in these acquisition paths rather
than assuming GA02c guards raw lists or every existing command. Existing Taken
quest/effect callback rollback remains the separately recorded broader boundary.

A03 concrete real collisions: GlimmerBrine alone vs +SparkRoot both display
acidic & electrified tonic but EffectsRaw charge2 vs3. MendleafSprig+EmberFruit
vs CandyHeartRoot both display mending & mending tonic; these healing-only brews
have no BrewItemPart, and Tonic.Healing differs1d4/2d4. Actual paid tinkering
mod_palesalt_infuse once vs twice suppresses duplicate adjective but carries
one vs two Tier2/BonusDamage4 enhancement Parts (4 vs8 undead damage). Transfer
the modified weapons to provoke restacking; modification alone does not restack.

Minimum semantic comparator is computed, symmetric, no saved fingerprint/field
change: keep blueprint/display and charged-book veto; compare presence and
configured BrewItem EffectsRaw/Form, Tonic Effect/Duration/Healing/StatBoost/
Drink/Message, StatusTonic EffectName/EffectDuration/EffectDamageDice/
EffectMagnitude, CureTonic CureEffect, plus every enhancement occurrence in
actual Parts order. Enhancement fields: runtime type/Tier, PaleSalt/ChoirIron
BonusDamage, Serrated ChancePercent/SaveTarget/DamageDice, Lacquered AvBonus/
AppliedBonus, GlowQuartz RadiusBonus/AppliedBonus, Engraved Faction/RepDelta/
AppliedBonus. Unknown enhancement types should conservatively refuse. Do not
compare ownership/IDs/count or blindly sort dispatch-significant effects.

Qud GameObject.SameAs:10696 checks parts/stats/effects; Stacker.SameAs ignores
quantity, explicit modification comparators inspect Tier/config. CoO duplicates
Parts intentionally, so a first-part-only comparator is insufficient. Preserve
identical recipes and equivalent CandyHeartRoot vs +Mendleaf (MAX merge gives
same2d4), once/once and twice/twice infusion, split/remerge and token-graph reload.
Test ACTUALLY CARRIED outputs after AddObject; using orphan returned producedItem
with consumeItem=false conceals the current merge defect. This is payload-family
identity, not a blanket equivalence claim for all HP/thermal/material state.

Wave2c payment RED58:57 passed/1 insufficient-for-both failure23:04:01–02UTC. Late affordability gate after unequip now preserves the independent sale and refuses the outer transfer before delivery. Native bench/driver/isolated launcher implemented after seven confirmed staging REDs. Focused GREEN verification follows.

Wave2c staging/payment GREEN385/38523:08:09–10UTC, zero C# errors. First
native keyboard audit35/35 PASS, runf740aef9c8474b56b3bfbd63483ea53f,11.219860417s,
exit0. Actual inventory→full Sack refusal→loot one filler→successful Put, then
Merchant dialogue→full-trader sale refusal→buy51→successful sale all passed.
Raw JSON/log retained. Recorded A31 destroyed-camera shutdown errors recur after
the successful audit; no claim of clean global FX teardown. Independent native
review identified a verification gap: dictionary/Physics equipment checks did
not inspect BodyPart._Equipped used by combat. Add original body-slot capture,
assert restored on refusal and absent on success before final native repeat.

Final taxonomy review found no remaining must-fix production regression. Add8
sequential same-transaction and conflicting-vs-independent destination-merge
controls, plus veto/exception retries and an explicit callback-entered pin.
These are hypothesis/countercheck probes; classify their first results honestly.

Wave2c COMPLETE:73/73 final checks, then7939/7939 full GREEN23:18:03–23:19:33UTC,
zero C# errors. Strengthened native35/35 PASS, run4621440f68954790b6086b80724a72a7,
10.452218166s, exit0. Eight final hypotheses pinned correct behavior; independent
taxonomy/Qud-contract/native review clear after body-equipment assertions.
2326 unique GUIDs,zero collisions. See GA02c-REPORT.md for exact evidence and
limits. A01/A04 and transfer-specific A05 closed; proceed A43 acquisition.
