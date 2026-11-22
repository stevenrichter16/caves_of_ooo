# Shared Beating exposure: bounded proposed slice

November22 prerequisite correction: [well source/transaction receipt](Verification/MachineSystems/WellSourceConsistency20261122/README.md)
aligns phase/mixing/fault/existing-oath admission between direct and skin draws,
and makes the established FREE direct-well row transactional. Native133/133 and
ordinary trough drink/reader/fresh1770 qualify that bounded correction. Legacy
one-unit/full cure remains shipped; finite dose/batch/paid-action policy below
is still a deliberate future change. No exposure/AI enrollment or new save fields.

November21,2026 (America/Chicago). User-requested follow-up to the Beating audit
and survival brainstorm, added to the active hourly task. Source baseline
14c4750c1. This is a proposed design/native-audition contract, not shipped
behavior, a balanced survival simulation or permission to activate entire NPC
populations without the gates below. No runtime or ordinary-save changes here.

Two bounded independent source/design critiques were incorporated. The first
identified chronology/undo, saturated debt, path-arrival forecast, finite-source
sustainability and clothing/save migration traps. The refinement specifies dose
normalization, preventive/partial drinking, cooling and isolated enrollment.
Final review found zero P1/P2 with the explicit source-level dose rule below:
all consumers of a normalized source pay identically. Review was read-only; no
runtime, tests or Unity operation were performed by the reviewer.

## Player problem and smallest useful outcome

Sun currently weakens only the player. An unprotected bandit, animal or follower
is exempt regardless of equipment or physiology. The goal is a readable common
rule: a visitor uses the same awning and actual water as the player; a host's
clothing and preparation matter; a specifically adapted animal differs for an
explicit reason. A follower warns and takes sensible precautions rather than
silently dying, wasting supplies or abandoning the party.

The first audition has FIVE actors: player, visiting human, covered Tent-Right
host, explicitly adapted SunStriker and recruited follower. Use an ordinary
Beating campsite with a real camp awning, a public cistern with its existing
Stock3, and ordinary three-charge waterskins. No hostile suppression, infinite
carried water, direct time jumps or player-only shelter rules may replace native
actions after the authored fixture boots. Fixture supplies are not acquisition
or settlement-sustainability evidence.

## Verified seams and required corrections

| Current source | What it supplies / what it does not |
| --- | --- |
| `BeatingGlareSystem`, `InputHandler:1337` | One player callback and one static exposure counter. It cannot simply loop over NPCs; head-slot presence currently counts as full cover. |
| `WorldClock`, `TurnManager.Tick/AdvanceClock` | Single saved TickCount; 300-tick bands, 1,200-tick day. NotifyPlayerTurnEnd reports endpoints, not every skipped interval. NPC EndTurn frequency is not elapsed time. |
| `RestSystem`, `WorldMapTravelCostPart` | Rest and map movement can advance the clock outside ordinary actor-energy ticks. Map movement adds10 through AfterMove before the normal paid-turn path; do not infer total cost from that constant alone. Transactional rest advances only after commit. |
| `ShadePart`, `Body`, `EquipmentChangeBus` | Real installed shade geometry, actual worn slots, monotonic equipment version. The bus is a version bump, NOT an existing after-commit subscriber event. Validate actual equipment; a failed equip/undo must not cache protection. |
| `ParchedEffect`, `TonicPart`, companion care | Persistent1–3 stacks, −1 Strength/Agility each. Currently standing on a water-family tile cures everything; drinking tonics/care cure one. These recovery paths need a shared, transactional normalization for enrolled actors. |
| `WaterskinPart`, `WaterVesselService` | Capacity3, charge-spending drink, finite source transfers and rollback. One charge relieves one stack. No body-water reservoir or exposure integration exists. |
| `WellPart`, `WellDrinkingService` | Stock−1 ordinary wells, finite cisterns, stock/pool validation. Currently ONE direct-source unit clears ALL Parched stacks, while the same unit in a skin clears one. The new finite-dose proposal below is an intentional correction, not current behavior. |
| `TentRightOath.RefusesWater` | Existing player oathbreaker refusal; it explicitly checks Player. It is not a general permission/ownership policy for all NPC sources. |
| `AIBehaviorPart`, goals, `FindPath`, terrain weights | Shared movement and finite danger costs. No shade/thirst planning. AIWellVisitor randomly walks beside literal Well and does not drink or recognize a cistern. |
| `EntityFactory`, `SaveGraphSerializer` | Part discovery and public-field saving are available. New state still needs real old-save/default/roundtrip tests; dictionary/name discovery alone is not save acceptance. |

The complete audit and native evidence remain in
[BEATING-ENVIRONMENT-AUDIT-2026-11-21.md](BEATING-ENVIRONMENT-AUDIT-2026-11-21.md).

## Proposed common state and rates

Use small composable Parts and one coordinating interval service; the names below
are provisional API names. No per-species switch inside the service.

- `SunExposurePart`: saved Strain, fixed-point Remainder, LastSettledTick,
  condition/contract version and enrollment state. Parched remains the saved
  consequence; do not add hunger, general metabolism or a second thirst meter.
- `SunTolerancePart`: authored gain scale. The ordinary starting scale is1;
  audition SunStriker is1/2. This is a test proposal, not a canon biological fact.
  It changes sun accumulation only, not fire resistance or immunity.
- `SolarCoverPart`: authored coverage on actually worn, relevant equipment.
  Audition LeatherCap and ShadefanHood give full solar cover. Carried, removed,
  destroyed, wrong-slot or merely head-shaped objects do not. Existing headwear
  needs an explicit compatibility inventory before any production player switch.
- `SeekSunSafetyPart`: generic behavior requests ordinary movement/equipment/
  water actions. It does not manufacture water, directly clear effects or own an
  independent clock. Protective NPC behavior is required before enrollment.

Starting test calibration: unprotected ordinary actor gains one strain unit per
elapsed tick at surface Beating Height. At100 strain, add at most one Parched
stack and subtract100. This preserves the old approximate ten normal actions as
a comparison point, not a final balance endorsement. Use integer fixed point
(for example denominator256) and save the remainder, so half-rate behavior does
not depend on chunk size. Readers show understandable condition, not raw units.

Installed shade, full functional solar cover, interiors, underground, leaving
the Beating and non-Height time give gain0 and cool strain at1 per elapsed tick.
Partial cover reduces gain; it does not additionally grant full cooling. The
state never goes below0. One instant of shade does not erase accumulated strain.
Existing Parched stacks remain until a real hydration action relieves them.

At Parched3, discard further incoming strain rather than banking a hidden debt
that immediately defeats the next drink. No HP loss is added by this slice;
existing fire/poison and other world systems retain their own consequences.

Hydration relieves existing Parched and clears bounded SUN strain. It does not
lower ThermalPart temperature, extinguish fire or grant HeatResistance. After
relief an ordinary actor needs another100 fully exposed ticks for its next stack;
remaining debt and altered tolerance still matter. Do not promise a timed immunity
buff. Shade never restores water or clears dehydration.

## One time policy; intervals, not NPC turns

Integrate committed intervals `[oldTick,newTick)` at the shared normal Tick and
AdvanceClock boundaries, using the same TickCount already saved by the game.
TakeTurn/EndTurn, menus, F1 readers and real wall time do not independently add
exposure. Status-forced time still counts. Water services consume their ordinary
actor action; they must not also advance time or end a second turn.

Use the actor's KNOWN conditions during each interval, not the current destination
applied to the whole past. Local AfterMove marks the new physical condition for
subsequent elapsed ticks; it cannot retroactively protect preceding ticks. A
known long rest uses its real shelter throughout. World-map travel needs an
ordered route/condition descriptor, with actual cost and boundary changes, before
it qualifies. Do not double-charge its explicit and ordinary clock advances.

Split analytically at Height and other condition boundaries. Preserve ordering:
adding all sunlight and subtracting all nighttime cooling as an unordered net
would lose Parched stacks earned before cooling. A long constant-condition gap
must use bounded arithmetic/cycle handling, not one loop per elapsed tick/day.
Test every long-jump result against a fine-grained reference and saturated debt.

Condition changes become effective only when their movement/equipment/install
action succeeds. Read projections are pure. Clock/exposure history is committed
chronology, not part of a later drink's reversible inventory snapshot. If care
is vetoed, undo only that action's quantity/cure changes, never independently
accrued time or another actor's effects. Reconcile committed time before choosing
the drink preview and revalidate at commit.

Unrecorded intervals grant neither new exposure nor cooling: preserve strain,
remainder and debt, mark the limit honestly, and do not claim transport acceptance.
Unknown-gap freezing is NOT a production solution; repeated zone transitions
cannot become an exposure escape. Audition acceptance is restricted to known
intervals, with route/cross-zone tests a mandatory gate before wider activation.

## Water: finite quantities, convenient actions

The proposed common hydration unit relieves ONE stack and the first consumed
unit also clears sun strain. With zero debt, a preventive drink spends one unit
and clears strain; no extra charge is levied for strain alongside an existing
stack. A full skin still has3 charges and drinking one spends one.

For enrolled finite wells/cisterns, direct drinking batches the needed units in
ONE ordinary paid action, up to available stock: debt3/stock3 spends3 and clears3;
debt3/stock1 spends1 and clears1. Preview the partial result accurately. Filling
and direct drinking therefore cannot multiply relief by moving the same water
through a container. An ordinary Stock−1 public well retains convenient one-action
full recovery. Finite-source normalization needs native RED and regression cases;
it must not silently change unrelated production cistern behavior during audition.
The normalized source has ONE source-level dose contract for every consumer,
including an actor without exposure enrollment. Never select a cheaper legacy
price because the actor is a player, NPC or missing a profile. The isolated
audition source carries the proposed contract; migration of production sources
is separate. Actor enrollment changes exposure state, not the price of the same
water at the same source.

Declared drinking-tonic hydration remains one dose per consumed item, alongside
its other authored effects. TonicPart and companion care must use the same relief
operation and preserve existing payment/recipient/range semantics. NPC routine
care chooses ordinary water; precious healing tonics remain combat/emergency
choices. A follower can receive a real vessel through ordinary inventory transfer
then drink it on its own turn; no free remote sharing or new companion-feed UI
is necessary for this slice.

Wading and a water-family ground mark no longer cure dehydration for enrolled
audition actors. Existing wetness/thermal reactions continue separately. Aquatic
or finite wet-cloth solar cooling is later work requiring a declared capability
and accounting, not an implicit full cure from a liquid tag. This is an intentional
future semantic change with compatibility gates, not already shipped behavior.

Eligibility, range, quantity, contamination/phase, current Part ownership, actor
life/action state and access are checked for preview and again at commit. Claims
on the actor, vessel, source and exact recipient prevent two consumers taking the
last unit. Stale source, partial payment, veto and callback failure restore only
owned action deltas. Failure spends no water or voluntary action. The first source
is explicitly public/consenting; private sources cannot be enabled merely because
existing IsFreshSource returns true. Generic permission integration is a rollout
gate, with existing oathbreaker refusal preserved.

## NPC preparation and fallback

Shade first: forecast strain at ARRIVAL using travel time and gain rate, not only
current strain. Starting warning50 is a test point; a slow or blocked actor needs
an earlier choice when the route takes longer than its safe horizon. Protect the
head with usable owned clothing; choose reachable cover; drink carried water or
an allowed source when justified. A successful drink can also buy an exposed
worker enough time to reach shelter, at a real finite cost.

At most three shelter candidates and one bounded path attempt per participating
actor decision; set a total decision/path budget per world advance. Reserve an
actual reachable cell in the shade footprint, not an entire awning as one slot.
Reservations are hints, rechecked on arrival/commit, and released on interruption,
death, travel, occupancy change or target removal. They never substitute for a
transactional water claim. Replan on a relevant changed dependency or a bounded
cooldown, not every failed AI action; do not retry a dry source indefinitely.

Combat safety outranks walking blindly into a hostile to reach shade. Duty can
delay comfort but must have a fallback: request relief, shelter nearby, stop an
outdoor job or retreat. Followers warn/request supplies and prefer nearby cover
inside their leash; they do not silently abandon the party. If no safe solution
exists, retain honest strained/Parched state and report why; never grant immunity,
water or teleportation as an AI fallback.

Tent-Right preparedness is clothing, shelter and hospitality, not a faction
immunity flag. A host can help a guest reach cover or transfer real water. Duty
does not create stock. Consult Lore/Factions/07_TentRight.md for the well-keeper,
guest and nomadic-water context; these proposals add no canonical biology.

## Stable settlements without a maintenance economy

No background basal thirst in this slice. Sheltered idle residents do not drain
water indefinitely simply because the player waited. Explicit outdoor jobs or
journeys can incur modeled exposure and spend reserved supply; unsupported jobs
are not silently simulated. A cistern3 is a limited waysite, not an inexhaustible
town source. Forecasting alone cannot make finite stock sustainable.

Before a whole settlement participates, author supply/storage/demand/reserve and
shelter capacity. A supported spring/well or real water trade can sustain the
population; converting ordinary Stock−1 wells into yield/recharge simulations is
separate reviewed work. Do not pretend stock automatically replenishes today.
Shortages first change work hours, close exposed jobs, move services under cover,
seek available trade/help or relocate. Ordinary services remain practically
accessible. Do not make keeping settlements alive the player's compulsory chore.

For the bounded audition, unloaded residents have a declared sheltered-idle policy
and no offscreen work/output/water spending. Strain can cool only for explicitly
recorded shelter intervals, never by assuming a destination or resetting on load.
Detached/exposed actors without a known itinerary preserve state and retain an
unsupported-interval diagnostic; that is a coverage limit, not a claim of simulated
settlement survival. Population rollout waits for its supported policies.

## Native RED, countercases and actual acceptance sequence

1. Time: ordinary and half-rate actors get identical results for equal known
   elapsed intervals at different speeds/population counts, split advances and
   large rest jumps. Crossing299/300/599/600, saved fractional strain, saturation,
   same-tick shade toggles and zero/negative deltas have explicit controls.
2. Protection: worn cap/hood versus carried, wrong-slot, removed, stale or rolled-
   back equipment; occupied/destroyed/carried awning; no generic head-object or
   native/faction exemption. Thermal/fire behavior must remain independent.
3. Water: preventive dose;1/2/3 debt with0/1/3 source units; skin capacity3;
   equal finite doses at source/in vessel; two consumers/one last unit; custody,
   recipient, contamination, oath/access, veto and callback rollback. Include
   tonic/care and ground-water-cure migration countercases.
4. AI: slow approach, blocked path, crowded shade, empty well, stolen vessel,
   missing clothing, interrupted duty/combat, follower warning/leash, exhausted
   decision budget and reservation cleanup. No infinite failed-action or stock
   reset loop; no hidden emergency healing/potion consumption.
5. Migration: persist strain/remainder/LastTick/profile and existing Parched;
   paired clock/actor load does not reset or apply penalties twice. Old saves
   enroll once at current tick without retroactive debt; later reloads preserve
   partial strain. Enumerate all shipped headwear before the player writer swaps.
6. Actual GameBootstrap audition: player and visitor exposed, host covered,
   animal half-rate, follower warning; shared awning; finite drink/fill and last-
   unit contention; real interruption; leave/re-enter shade; fresh Continue.
   Demonstrate recovery through ordinary water outside a machine, and a combined
   clothing/shelter/companion outcome. Readers and previews must match actions.

Prototype enrollment is explicit and isolated, using reusable Parts rather than
site/name scripts. Keep the old production writer outside this audition until
compatibility and chronology gates pass; never run both writers on one actor.
Do not present this development scoping as a finished universal world rule.
Broader activation requires source and gameplay review, known transport/unloaded
policies, source access, sustainable locations and safe NPC behavior together.

Next runnable step after this design pass: prepare the bounded prototype's native
RED set and exact owned source plan when that implementation slice is taken up.
The standing remaining machine-play queue may resume meanwhile. No new runtime,
build, complete-machine acceptance or settlement-stability claim follows from
publishing this plan.
