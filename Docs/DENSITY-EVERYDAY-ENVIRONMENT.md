# Everyday and environmental density — verified continuation plan

Status: C3 water/cooking and C4 time/rest first slice implemented, 26 September
2026. Core and adversarial prechecks pass; native GREEN and real player journey
remain acceptance gates. Further C4 furniture/light/grammar and C8 environmental
work are not complete. CoO-original, extending `DAY-TO-DAY.md` and density analysis Phase 3;
no claim of a decompile-verified Qud port.

## Verification corrections

| Premise | Verified implementation | Scope consequence |
|---|---|---|
| Nothing carries water | No Waterskin, LiquidContainer, or fill/pour command exists. `LiquidPoolPart` carries one liquid ID and volume but only coats on cell entry. `LiquidDefinition` has no drink contract. | A water-only reusable vessel is a bounded first slice; arbitrary liquid ingestion needs authored semantics, not guessed coating effects. |
| Cooking does not exist | Material reaction rows `fire_plus_raw_meat` and `fire_plus_raw_starapple` swap sufficiently hot/dry ground food into existing cooked products. `MaterialSimSystem` evaluates reactions on hot props. | Add a convenient inventory cooking verb; preserve thermal cooking. The private `MaterialReactionResolver.SwapBlueprint` is zone-only and does not preserve stack count, so it is not an inventory transaction API. |
| Corpse harvesting must be built | `CorpsePart` attaches `HarvestablePart` from per-creature yield fields; harvest consumes the carcass even when its yield roll fails, and overflow drops locally. | Do not duplicate butchering or erase species yields. Per-family corpse art/names are a separate content concern. |
| Rest is missing | `RestSystem.TryRest` heals, removes Bleeding, advances 60 ticks, blocks hostiles within eight cells. Campfires, paid inn/hermit dialogue, Morrowfast guest beds and founding plumes already call it. | Add next-band selection through these existing authorization routes. Do not grant free paid-inn rest by attaching a generic bed action. |
| Time needs persistence | `WorldClock` derives four 300-tick bands from saved `TurnManager.TickCount`. Sidebar currently shows no band. | Add a derived vital line; no new clock or save field. Existing vital-line fingerprint already invalidates rendering. |
| Steam is inert | Entity `SteamEffect` cools adjacent entities and wets them; `ScaldingVeilEffect` already deals targeted retaliatory heat. Tile-cloud steam from `TileReactions/Reactions.json` is only a visual/lifetime mark with zero damage. | Distinguish harmless/wetting vapor from an explicitly hot cloud. Do not globally reverse SteamEffect's existing cooling rule. |
| AI has no hazard avoidance | `GasNavigationWeight.ForCell` adds finite, density-scaled, immunity-aware gas cost to `FindPath.Search` (line 179). | Extend existing path cost for proven harmful terrain. Combat's open-cell greedy shortcut in `AIHelpers.TryApproachWithPathfinding` bypasses that cost and must be tested as an actual approach, not merely an A* search. |
| Light needs a new system | `LightMap` includes lights on equipped items and listens to equipment-version changes. Torch already has light/fuel/thermal parts but no equip or light verb. Chairs/beds already reserve NPC seats. | Reuse equipment lighting and reservations. Player light/sit/sleep is its own slice, not a renderer rewrite. |
| All material fields share percent units | `MaterialPart` comment says 0–100, but current authored combustibility has 61 zero, 64 fractional positive (≤1), and four large values (OilSlick 90, TarSeep 95, DryBrush 85, PeatBog 60). `ThermalPart` reads volatility as a fraction; TarSeep has 30. Acid subtracts a fractional combustibility amount. | Do not mass-convert every material field or simply change a threshold. Fire normalization needs a consumer and save-compatibility design plus native spread playtest. |
| The fire models are entirely disconnected | Fire abilities already write tile heat through `ZoneTileStateSystem`; tile oil ignites and hurts occupants; burning objects radiate `ApplyHeat` to neighboring entities. Tile embers do not generally heat entity ThermalPart, and burning entities do not generally seed tile heat. | Bridge these particular seams with bounded one-pass transfer, rather than replace both systems. Prevent double hits and feedback amplification. |

## First slice: usable ordinary hours

### Water vessel

New `Assets/Scripts/Gameplay/Items/WaterskinPart.cs` plus a small water service
should hold three drinks in plain public saveable fields. It exposes Fill and
Drink through the existing item-action event/command flow. Execution rechecks
actual carried ownership and source reach, even if the menu was built earlier.
Drink spends one charge and calls `ParchedEffect.ReduceOneStack`; an empty vessel
stays in inventory. A non-parched drink may still spend a charge, matching an
intentional consumable action. Empty/full or invalid-source refusals are visible
and mutate nothing.

Fill from a reachable `WellPart` (including cistern owners) or real standing
water: a positive `LiquidPoolPart` whose ID is exactly `water`, or an authored
water `TileStateSourcePart`. Do not silently treat brine, oil, acid, or a thin
temporary spell coating as potable supply. A source should be within one cell
of the actor using occupied-cell reach for composed furniture. World wells keep
their existing immediate drink action. No water-currency conversion.

New mutable vessels must not merge or duplicate charges through stacking/split
paths (`StackerPart` currently explicitly protects grimoire charges). Fill/drink
must register undo and publish success messages/diagnostics only after the
enclosing `InventoryTransaction` commits. The generic command's existing
snapshot does not capture custom part charges or effect bookkeeping by itself.

Root-owned blueprint/stock requests: one `Waterskin` item with three-drink part,
handling, commerce, flavor/examine text and no effective stacking; add one
guaranteed unit to `MorrowfastProvisionerStock`, `ProvisionerStock`, and
`WellKeeperStock`. Exact price/weight follows local provisioner items during the
implementation sweep. No existing-save migration or blueprint injection.

### Cooking

New `CookablePart`/`CookingService` offers Cook for carried raw foods beside a
`CampfirePart` owner, covering campfire, hearth, oven, and Morrowfast stove. Cook
the whole selected stack in one turn. Preflight the output blueprint, exact
source ownership/count, available output capacity and station reach; then join
the existing item command transaction. Preserve quantity, carry accounting and
all unrelated inventory. Missing factory, station, capacity, stale source, or
an exception from an after-action listener must not consume raw food or leave
an unreported product. Repeated action on the old source must refuse.

Root-owned data: `Cookable.Into` on RawMeat→CookedMeat,
Starapple→RoastedStarapple, Mushroom→new RoastedMushroom. Existing first two
products heal 3d4; new roasted mushrooms likewise heal 3d4 as `DAY-TO-DAY.md`
specifies. Retain existing authored raw-food thermal reactions and flavor tags.

### Time and timed rest

`SidebarStateBuilder.Build` can append the current band's depth-aware name to
`VitalLines`; the existing snapshot/fingerprint path already sees that line.
Keep all combat vitals and status rows legible inside the seven-line limit.
Only widen/change the renderer if the real layout test requires it.

Extend `RestSystem` with a next-band operation (1–300 ticks to the next boundary)
while retaining the old 60-tick rest unchanged. Add a second campfire action;
add a paid dialogue option/command reusing the inn's price and WellRested rule;
add a Morrowfast guest-bed action through its exact owner/reach gates. Do not
replace paid rest with a generic free BedPart action or alter NPC reservations.
World time advances by the requested jump, not hundreds of simulated actors'
actions. Check the surrounding action-energy cost so it cannot overshoot the
advertised boundary or charge an unsuccessful rest.

Likely modified files: `Settlements/RestSystem.cs`, `CampfirePart.cs`,
`Conversations/ConversationActions.cs`, `World/MorrowfastQuests.cs`,
`Presentation/Rendering/SidebarStateBuilder.cs`; authored conversation rows in
`Innkeeper.json` and optionally `Hermits.json`. New public APIs document their
contracts. All new Assets C# files receive fresh hand-written metadata.

### Verification before accepting this slice

Write and execute RED first. Add focused ordinary-hour tests and a dedicated
adversarial fixture; retain existing BiomeRest, WorldClock, inventory command,
material-reaction and save suites. Required counter-pairs:

- Same water source at reach one versus two; positive water versus empty pool,
  brine/oil, temporary coating, removed source and null zone. Charges 0/1/3,
  two separate skins, stale foreign ownership, inventory/drop/re-pickup and save
  roundtrip. Three parched stacks lose exactly one per drink; unrelated stats
  and other actors remain unchanged.
- Whole raw stack versus one unit, cooked product versus raw source; missing
  station/product/factory and full pack reject atomically. After-event exception,
  reentrant transfer, same-blueprint different IDs and existing product merges
  preserve conservation; success receipts appear only on commit.
- Clock 299/300/1199/1200, underground/surface names, adjacent band transition
  versus same-band no redraw; hostile distance 8 versus 9 and a wall between
  hostile/resting actor. Paid insufficient funds, occupied/foreign guest bed,
  old 60-tick action unchanged and no charge/heal/time on refusal.

Native acceptance should use the existing isolated launcher pattern: buy the
real stocked vessel, fill at a real well, cure one stack on the Beating leg,
cook carried meat at Morrowfast's stove, and rest to a shown band. Record exact
quantities/time/currency, refusal controls, input routing and raw console,
inspect actual popup/sidebar images, then prove save/settings restoration.
Numerical evidence cannot prove pacing or survival balance.

## Later environmental slices, separately gated

1. **General liquid handling:** introduce a single-liquid container only after
   specifying volume conservation, renewable terrain versus finite pool sources,
   safe/nonpotable drink definitions, and mixing refusal. Pour into the existing
   `ZoneTileStateSystem.WriteCoating`/reaction path rather than spawn an unlimited
   replenishing terrain source. Fill/drink/put/drop/split/save transactions must
   keep one physical vessel's payload. Water-only work must not pretend to ship
   all 26 liquid ingestion rules.
2. **Smoke visibility:** a shared cloud-occlusion query should gate both player
   `FieldOfView` and AI `HasLineOfSight`; both currently inspect solid geometry
   only. Use tile cloud lifetime and invalidate visibility on creation/expiry.
   Pair smoke/non-smoke, before/after decay, player/AI symmetry, walls unchanged,
   origin/target conventions and no hidden creature disclosure. A visual still
   must show occlusion and reopening, not merely assert a cloud string exists.
3. **Hot steam:** preserve existing cooling/wetting SteamEffect; add an explicit
   hot-cloud cause/dose rather than make every vapor mark damaging. Reuse Heat
   resistance and ObjectStatusMatrix contracts, once per exposed body per player
   turn including multi-cell actors. Counter-check harmless steam, resistance,
   negative/expired duration and no duplicate damage from reaction+cloud tick.
4. **Hazard routes:** extend the existing finite navigation cost, avoiding acid,
   active fire/electrified water only when harmful to the actor. Check the greedy
   combat approach before A*, cached routes after hazards change, forced-only
   corridors, immunity, multi-cell footprint cells and null/actorless callers.
   Preserve gas behavior; do not introduce per-node event allocations.
5. **Fire scale/bridge:** first inventory all consumers of combustibility,
   volatility, charred/acid mutation and save restoration. Choose explicit unit
   handling that preserves old saves; avoid heuristic re-scaling on every read
   (degrading a percent below 1 must not multiply it by 100). Keep the canonical
   unit conversion separate from a bounded tile↔entity heat bridge. Maintain
   existing FireDose ignition pins, dry/wet and stone counter-checks, source
   attribution/GroveLaw, per-body deduplication, extinguishing and finite fuel.
   This remains **playtest-gated**: native dry brush/oil/tree versus stone/wet
   controls, ignition latency, spread radius, extinguish latency, frame/turn
   cost and a route the player can actually escape.
6. **Player furniture/light:** reuse existing chair/bed occupancy and equipped
   light collection. Respect Owner and occupied reservations; stand/move/hit
   cleanup must free the exact seat. Torch should gain a deliberate hand equip
   and lit/unlit state whose light-cache invalidation is tested; do not infer
   fuel consumption while held from ground-only material ticks. Light remains
   a presentation feature until darkness/FOV rules are separately specified.

## Design review and files

🟡 Avoided duplicate systems: existing corpse harvesting, rest, cooking reactions,
gas A*, doors and equipment lighting remain authoritative.

🟡 Transaction risk identified before implementation: generic action rollback
does not automatically restore new charges/effects/products. New services must
join the transaction and test post-event failure, not only happy-path events.

⚪ Fire/steam semantics and feel require their separate native acceptance; do
not label the everyday slice a unified environmental simulation.

Files changed in this audit: this plan only. No Assets edits, tests, Unity calls,
commits or pushes are claimed.

## Execution log

- Preimplementation correction: `InventoryUI` generic successful actions do not
  currently consume a player turn. Fill/Drink/Cook require a narrow successful
  action signal to `InputHandler`; existing action timing stays unchanged.
- Finite water pools lose exactly the units transferred (partial fill allowed).
  Wells and authored water sources are renewable. No temporary coating refill.
- The root owns shared blueprint/UI edits; a private runner copy validates core
  contracts before integration. Native UI/input and Play acceptance remain due.

### First implementation and counterchecks

- Initial core RED: 35/35 fail before parts/content/time APIs. Water/cooking
  first pass: 30/35 pass; the remaining five are the not-yet-written rest routes.
- Rest RED expanded to 43 cases: 33 pass / 10 fail. After next-band routes and
  their original payment/hostility/ownership guards: 43/43 pass.
- Native UI RED: 16 expected failures (missing successful-action close/handoff,
  absent pending-turn API and absent band vital). Root preserved native XML in
  `Verification/DensityCompletion/Documents/native-ui-red.xml.gz`; this agent
  inspected every fixture failure. No test setup failure was accepted as RED.
- Adversarial RED: 67/70 pass, three failures prove a throwing post-commit
  message listener suppressed each success receipt. Success diagnostic and text
  now have separate commit observers; 70/70 then pass.
- Additional rollback RED: two failures prove absolute penalty restoration
  erased an independent Weakness modifier. Water undo now restores only the
  drink's penalty delta and the original Parched object, preserving unrelated
  effects. Core/adversarial combined: 75/75 pass.
- Stock RED 0/3 -> GREEN 3/3. Only the three named stock tables gained one
  guaranteed empty Waterskin. Parsed comparison proves all prior rows unchanged;
  shared LootTables ownership then returned to root for its separate book work.
- No stacker on Waterskin, no save migration, plain public charge/capacity fields.
  Save graph tests preserve two distinct vessels with different charges. General
  liquid containers, pouring, mixing and unsafe-liquid drinking remain separate.
- Cooking claims actor/source, prepares one real product per unit before removal,
  revalidates source/reach, and joins a receipt covering original list/order,
  counts, product merges, owners and carry accounting. Capacity uses actual
  destination-stack weight, not merely output blueprint weight. Recipes remain
  RawMeat -> CookedMeat, Starapple -> RoastedStarapple, Mushroom -> RoastedMushroom.
  Existing ground material reactions were not edited.
- Next-band rest is 1–300 pure clock ticks, refuses invalid/missing placement and
  overflow before healing, and retains the eight-cell hostile check. Campfires
  recheck physical reach; Morrowfast guest beds retain exact authored owner gates;
  inn/hermit options retain existing prices and WellRested. Ordinary rest stays
  60 ticks. These world/dialogue routes add no player-action tick beyond the jump.
- Root owns InventoryUI/InputHandler integration: successful FillWaterskin,
  DrinkWaterskin and Cook close inventory and hand off exactly one normal turn;
  rejected actions retain their menu and queue none. No direct clock increment
  is hidden inside food/water services. This requires the pending native test.
- Sidebar appends the depth-aware band as a fifth vital line. The previous exact
  four-line test pin now includes the derived fifth line. Existing fingerprint
  already hashes every vital line; no renderer or new clock state is necessary.

Public diagnostic receipts are `event/WaterskinFilled`, `WaterskinDrunk`,
`FoodCooked`, `WaterskinRejected`, `CookingRejected`, and existing
`furniture/Rested` / `RestBlocked` with actual clock delta. Names differ from the
old tentative DAY-TO-DAY examples; tests assert current production names.

Evidence lives in `Verification/DensityCompletion/Everyday/`. The standalone
runner exercises content, transactions and save logic, not UI input, native
rendering, gameplay feel or Unity-identical world seeds. Parent runs the native
suite and finite journey; those receipts must be linked before acceptance.

### C4 follow-up preimplementation sweep (not yet implemented)

1. `MessageLog.Add` stores and publishes strings unchanged. Many dynamic subjects
   are the player's literal display name `you`, producing `you is`, `you picks`,
   and `you's`. Use a narrow first-token normalizer with explicit verb pairs;
   preserve other subjects, already-correct text, quotes, multi-line prose and
   raw announcements. Do not run a blanket s-suffix stemmer over English.
2. `ChairPart` and `BedPart` already reserve `Occupied` when offering an NPC idle
   goal. `SittingEffect.OnRemove` releases either part; only `BoredGoal` currently
   stands an actor. Player sit must preserve the same owner/tag restrictions,
   physical reach and outstanding NPC reservations, and must have a reliable
   stand/movement/combat cleanup before it is usable.
3. Only chairs receive generated owner IDs (`VillagePopulationBuilder`). Beds
   default to unowned, so adding free healing to every empty-owner bed would
   bypass inn payment and assign consent where no authored public bed exists.
   Keep Morrowfast's existing exact guest-bed route. Any additional free bed must
   opt in explicitly; default beds do not become free inns.
4. Torch has LightSource, Thermal (450), Fuel (50), but no Equippable/action.
   Equipped light aggregation already exists. A new toggle must update both
   illumination/cache state and actual burning/thermal behavior, preserve fuel,
   refuse exhausted lights, preserve statics/save fields, and leave glowing
   swords, living Glowmaws and authored sacred lights alone. A visually dark
   torch that still burns is not an acceptable implementation.
5. Generated village buildings create door gaps; only native Morrowfast has a
   door part. Door placement needs its own aperture/topology and pathfinding
   sweep before adding collision to those gaps; not bundled with grammar.

Planned counterchecks: literal player vs NPC/quoted/raw prose; owned/foreign and
occupied/free chair; rejected effect application releases a newly claimed seat;
stand/move/death release only the actor's own seat; public guest bed vs private
ordinary bed; lit/extinguished/exhausted torch; carried vs actually equipped;
lightmap before/after change at unchanged position; save/load both switch states.

### Native first-slice results and follow-up RED

- Parent native focused run: all 75 everyday core/adversarial cases and all 16
  everyday UI cases passed. Receipt:
  `Verification/DensityCompletion/Integration/native-focused-first.xml.gz`.
  The broader integration run had one separate document-catalog failure, retained
  and repaired by its owner; this is not a claim that the complete suite passed.
- Wider private regression: 363/368 pass; five failures were the intentional
  Morrowfast provisioner stock pin. Updating that pin with exactly one Waterskin
  exposed a real unwired-construction omission: 17/18 restock cases pass, one
  fails. The fallback builder now includes the same one empty vessel.
- Grammar RED: 21 cases, 10 controls pass / 11 player-subject failures. The
  implementation repairs only a literal leading `you`/`you's`/`you’s` and a
  fixed known verb list. Quotes, other subjects, multiline prose and all raw
  announcements remain byte-identical. Real ParchedEffect has player/NPC tests.
  Four existing crafting message tests require the new `You brew/craft` text;
  their exception injection predicates were updated so they still inject errors.
- Chair RED: 15 cases, 7 controls pass / 8 missing-feature failures. No chair
  production is present at this entry. Sit will require actually standing on the
  chair cell (no hidden movement/teleport into hazards); existing same-cell
  interaction selects it. The explicit refusal explains how to reach the seat.
  Chair reservations gain a player occupant identity so removing one player's
  effect cannot release a different occupant's seat. NPC idle behavior remains.

### Independent C3 review — open corrections

A separate reviewer built thirteen private probes before any repair. Three
living-actor controls pass and ten tests fail: six dead-actor fill/drink/cook
commands are still accepted; two dead next-band rests revive the actor; two
rollback cases lose or duplicate a newly applied Parched exposure. Original
receipt copied to `Everyday/independent-review-red.xml`. These are required
corrections, not environment failures. The focused native GREEN predates these
extra cases and does not close them.

The same-class correction must undo one stack as a bounded delta to the current
Parched instance (or reinsert the original only when there is none), matching
penalty changes to the actual restored stack. Dead actor checks precede resource,
effect or clock mutation. A separate callback probe will exercise the double
cooking-receipt restore on capacity refusal. Assets remain frozen for the parent's
native journey while these repairs are prepared.

The capacity-refusal hypothesis is confirmed by two additional standalone tests
(`Everyday/cooking-receipt-red.xml`), one with a service-owned transaction and one
with a caller-owned transaction. After the immediate local inventory restore, a
refusal-message observer legitimately drops the old cooked-food resident; the
second enrolled restore resurrects that entity in inventory while it remains on
the floor. The corrective receipt closure must restore at most once. This is a
concrete duplicate-ownership bug, not a speculative reentrancy warning. The new
source stays outside Assets until the native run releases its import freeze.

### Independent review corrections implemented

Expanded independent RED is preserved under `EverydayIndependent/expanded-red.xml.gz`:
16 cases, 3 live controls pass and 13 failures (including ordinary-rest death
checks and the capped third Parched exposure). The two cooking receipt cases
failed separately. `Everyday/review-green.xml` now passes all 93 core, adversarial,
independent-review and cooking-receipt cases.

- Water/cooking reject explicit zero-or-negative HP and handled death before
  transaction/resource mutation. Actors without an HP stat preserve the existing
  utility/test actor contract. Shared `TryRestFor` also rejects dead actors, so
  both ordinary and next-band rest cannot revive them.
- Drink undo restores one bounded stack to whichever Parched effect is currently
  active, restoring its own original instance only if none remains. Strength and
  Agility recover precisely the stack delta actually restored, including zero at
  the cap. New same-class exposures and unrelated Weakness effects survive.
- Cooking's enrolled/local receipt restore is idempotent. Independent transfers
  made by the refusal observer survive outer rollback; a dropped cooked stack
  cannot reappear in the inventory while still on the floor.

These repairs do not migrate saved parts or alter the agreed clock-only rest
model. Native first-slice receipts precede the review repair; the parent will run
updated native tests before committing.

### Player chair implementation

Expanded RED (`Everyday/chair-expanded-red.xml`): 19 cases, 9 refusal/control
passes and 10 missing-feature failures. Player chair actions now use the existing
inventory command transaction and SittingEffect. They require actual occupancy
of the seat cell, preserve owner ID/tag checks and outstanding NPC reservations,
and add a saved player occupant identity. Successful movement, damage or death
releases that player's seat; blocked movement retains it. A stale effect on
another actor cannot release the current occupant's chair. Standing rollback
restores the exact previous effect and reservation. The shared presentation owner
will wire successful world actions to one turn; this entry does not yet claim
that UI timing hook or native chair acceptance.

### Torch interaction sweep and bounded plan (before implementation)

`Torch` is already authored hot (450), with 50 fuel, a 0.3 burn rate and light,
but has neither an equipment slot nor an action. `BurningEffect` is destructive
fire: it damages its own target, so attaching it to a 5-HP torch would burn the
entire tool away in a few turns. The controlled torch flame therefore needs a
scoped part using existing Fuel/Thermal values, separate from destructive fire;
this does not change general combustibility, propagation or the deferred fire
scale. Existing magical/living/permanent lights do not acquire torch behavior.

Minimum implementation: one new TorchLight part and hand equipment on fresh Torch
blueprints; extinguish/re-light only an actual single equipped torch (or a single
reachable loose torch), refuse dead actors and stale ownership, require a nearby
actual hot fire to relight and refuse wet/exhausted fuel. Lit torches project
through existing equipped-light aggregation. A light-enabled field must gate the
lightmap and flicker, invalidate the existing equipment-light cache on change,
and survive save/load. Controlled fuel ticks once per held actor turn or loose
material tick, with no self-damage; extinction cools to ambient and stops light.
Torch payload comparison must keep different switch/fuel/thermal state apart.
Fresh identical torches remain stackable; normal equipment splitting creates the
single torch. Stowed lights retain state but neither project nor tick fuel; this
is a bounded equipment-light rule, not full inventory heat simulation.

RED/counterchecks will cover hand reachability and splitting, actual equipped vs
stowed light, on/off cache recompute without moving, flicker after extinction,
nearby flame/wet/exhausted refusal, dead/stale actor, fuel conservation and no
self-damage, rollback, save graph, and stack-state separation. Native darkness
and turn-input proof remain required; software lightmap tests alone cannot prove
the perceived brightness of the native voxel scene.

Torch first GREEN: `Everyday/torch-first-green.xml`, 24/24 after a 24-case RED
with 17 failures and 7 controls. This is provisional: a separate private
adversarial fixture then ran 19 cases with 15 controls passing and four real
failures (`Everyday/torch-adversarial-red.xml`). A destructively burning loose
torch draws fuel twice; a frozen torch relights while still frozen; wet/dry
states merge; and the existing `Entity.CloneForStack` drops the private effect
list when equipping one wet torch from a stack. Those are open review findings,
not accepted limitations. Source and additional probes remain outside Assets
during the parent's native UI import freeze. The environmental split repair must
remain scoped to the new torch behavior and preserve effect ownership/lifetime,
without pretending to redesign every item's status/stacking contract.

### Torch adversarial repairs and current verification

Final adversarial RED expanded to 27 cases: 15 controls pass / 12 failures,
including all supported environmental split payloads, unknown-effect refusal
through equip/partial split/remove-one, and direct unsafe clone rejection. Raw
receipt: `Everyday/torch-adversarial-final-red.xml`; exact RED fixture copy is
preserved beside it. The repair was first verified in an isolated source copy
(`torch-private-candidate-green.xml`, 51/51; broader candidate 499/499), then
applied to shared production. `Everyday/nearby-torch-final-green.xml` passes all
499 current nearby tests, including the 51 torch cases. Candidate receipts do not
stand in for the later shared-source run.

Repairs:

- A destructively burning torch retains the existing BurningEffect damage and
  fuel tick. Controlled-light ticking does not draw fuel a second time.
- Frozen torches extinguish on their next tick and cannot be relit until thawed.
- Torches with any status effect refuse merging. Wet/Frozen/Burning splits copy
  independent effect objects and their actual intensity/moisture/cold, lifetime,
  ownership, application flag and cause; existing source references/random stream
  are retained. Copying fields directly avoids duplicate porosity/heating/stat
  application. Unknown effect classes (including subclasses with extra state)
  refuse splitting before quantity changes. Direct unsafe clones fail before
  mutation; the equip command explicitly handles split refusal.
- The source/status split hook is scoped to TorchLight. It does not silently
  broaden the cloning contract of every other stackable item.

Parent native UI integration passed 141/141 checks, including the actual chair
C-action transaction/turn/rollback path and torch inventory success/refusal turn
routing. That native run preceded the final environmental-split repairs above;
updated full native coverage and actual torch scene brightness remain acceptance
gates. Existing C3 live journey passed 13/13 with seven captures, normal starting
HP/money, real merchant/water/fire/book sources and explicit travel shortcuts:
`NativeEveryday/1e465f7fd24d4b828b7fc3b82bef35dc`. The parent owns pixel review and
raw editor-log/state receipts; those verify the first everyday loop, not arbitrary
liquid pouring, general fire balance or new chair animation.

### Full-regression correction: player prose pins and chair status facade

The shared before/after sweep exposed four newly failing cases in three tests.
`Everyday/C4Regression/assigned-red.json` names the cases and preserves both raw
full XML receipts. The two planting cases still expected third-person “You
plants”; the drop rollback observer only recognized the old “drops” spelling,
so its intended exception never happened. These are stale assertions after the
narrow player-subject grammar repair. The planting assertion will continue to
name exactly one raw blueprint unit; the transfer test will explicitly count
the injected post-placement exception and keep exact identity/count/position
rollback plus successful retry. The container branch remains a matched control.

The source-scanning status facade test found a real production violation:
`PlayerSeatService` directly invoked `StatusEffectsPart.ApplyEffect`. The minimum
repair uses `actor.ApplyEffect`, preserving the central admission/FX capture
contract. Removal and restoration still use identity-based receipt undo; they
are not fresh status applications. No facade allowlist exception is justified.
The existing chair veto, transaction exception, exact standing rollback, death,
movement and ownership cases will run beside the facade and inventory suites.
Private candidate verification precedes shared publication during the native
scene import freeze. Native scene/UI acceptance remains owned by the parent.

The corrected private candidate passed581/581, and the same27-fixture selection
against published shared source also passed581/581. Receipts are
`Everyday/C4Regression/{candidate-green,shared-green}.{xml.gz,log.gz}`. The drop
and put observers each inject exactly one exception; the retained retry proves
claims are released. Both seed quantities still assert one crop, one payment,
one after-action and one correctly named single-unit message. The facade scanner
is unchanged and now passes; chair behavior/rollback controls pass alongside it.
Self-review: 🟡 direct status admission bypass fixed; 🔵 two stale grammar pins
updated without dropping their conservation/rollback contracts. No new gameplay
semantics beyond using the existing central effect facade. These are standalone
logic checks; the parent's final native sweep remains required.
