**Weapon-merchant extension — November23 evening:** the shared standard/special
policy now covers genuine WeaponsmithStock outside Cinderhold. Dependable
weapons: dagger, short sword, spear; components: oak haft and leather binding;
Cinderhold retains two spikes. Two rotating slots cover the other nine original
optional goods. Existing saved promoted sword/spear specials become ordinary
stock at due rotation without withdrawal/recreation. Native220/219pass/0fail/
1Explicit utilityskip/0CS,49new; actual Sill paid purchase, strict300/310refill,
1200rotation and fresh12210qualify after independent review0P1/P2. Older
Cinderhold-only paragraphs below record the earlier increment. See the
[extension](MACHINE-WEAPON-MERCHANT-SUPPLY.md) and
[receipt](Verification/MachineSystems/WeaponMerchants20261123/README.md).
Current Mac7cpredates this and UI1fda; one coherent package follows. Trade-route
imports and automatic NPC production remain proposals; all12 machine gates open.

# Merchant staples and changing special stock

User direction, November23,2026. Bounded Editor implementations cover
FarmerStock, ArmorerStock, ProvisionerStock, genuine ScribeStock in Quillhold
and genuine WeaponsmithStock across communities; Cinderhold retains regional spikes.
A repaired local oven expands farmer consignments; the existing finite copy-room
oil project adds a drying manuscript to later scribe lots. See the
[first pilot](Verification/MachineSystems/MerchantConsignments20261123/README.md)
and [regional extension](Verification/MachineSystems/QuillholdConsignments20261123/README.md).
Route imports, other profiles and unique-item issuance remain proposals.

Merchants should have a dependable base selection of specific goods and a
separate, periodically changing selection of interesting specials. Returning to
a familiar merchant should offer both reliability and the possibility of finding
something new. Opening a trade route or building a useful machine or tool for
the merchant can expand the special selection. Such projects gain a lasting,
visible benefit through the merchant's actual business.

The user approved the following refinements in the subsequent discussion:
routes introduce regional imports; equipment enables particular local work;
the two improvements can combine. A special find can also suggest a useful
project, so projects change merchant opportunities and merchant discoveries can
give the player a reason to undertake a project. Applications must follow what
the character can know and the item's supported capabilities.

## Dependable staples

Give each participating merchant an authored core catalogue appropriate to their
occupation, community, biome and economy. The staple identities stay dependable
across special-stock rotations. Exact quantities, replenishment and the meaning
of availability after a purchase require an explicit policy; a dependable
catalogue does not imply an unlimited supply created on each menu opening.
Preserve convenient access to ordinary supplies and existing trade/barter rules.

## Changing specials

Maintain a bounded special consignment that changes periodically in game time,
with limited quantities and a saved identity for each actual item. A special can
be an unusual material, regional tool, useful component, distinctive piece of
equipment or supported modification combination. Interesting does not require
ever-increasing combat power. Different merchants should have different themes
and pools, with enough variation to reward revisiting them.

Give a consignment a reasonable opportunity to be encountered and considered,
with understandable refresh expectations. Keep merchant specialties and supply
origins recognizable. Prefer variety in utility, materials and workmanship to
constant combat-power escalation or rapid reroll checking.

The user explicitly wants potentially unique items. Distinguish an unusual
generated item from a genuinely one-off authored object. A one-off must retain
its provenance and cannot be recreated by restock, reload, merchant relocation
or another vendor's roll. Protect undisclosed item state and lore mysteries.

Separate special rotation from ordinary inventory custody. Bought items remain
the buyer's; player-sold goods, buyback stock, reserved items and goods held for
another service must not be deleted or regenerated when specials change. The
fate of unsold old specials needs a stated policy before implementation.

## Lasting projects expand opportunities

Use actual persistent route and equipment conditions to change stock eligibility:

- An opened route can admit supplies from the connected region or additional
  consignment types. Keep the merchant's existing staples available.
- A repaired forge or useful workbench can allow a smith to offer supported
  workmanship, components or refitted equipment.
- A still, preservation apparatus or suitable storage can enable additional
  preparations or goods the merchant can actually handle.
- Better transport or storage can increase a bounded consignment's variety or
  capacity where that capability genuinely supports the result.

These are examples to refine against shipped capabilities, not claims that those
projects or supply networks already exist. Use shared capabilities and world
conditions, not a separate scripted exception for each merchant and machine.
An upgrade broadens future eligible offers; it must not mint another lot whenever
the player toggles equipment, crosses a zone boundary or opens the shop.

Keep import and production unlocks distinct. For example, a route can introduce
an unfamiliar regional material; a restored smith's equipment can enable
supported finished goods made using appropriate materials. Their combined
eligibility should remain subject to the same bounded consignment and resource
rules. An unlocked catalogue alone does not establish automatic NPC production.

Explain a meaningful unlock to the player: the relevant project is finished,
which kind of goods can now arrive or be made, and whether it affects the current
consignment or a later one. Preserve alternate merchants, direct crafting,
gathering and services. Special-stock improvements should be useful optional
projects, without making a particular machine mandatory for basic supplies.

## Original source footholds (before the shipped pilots)

`TraderPart` already authors a StockTable, starting purse and opening allocation.
`TraderRestockSystem` already stores LastRestockTurn and, on eligible zone entry,
tops up buying power and rolls the existing themed table when its shelf falls
below the low-water threshold. Its legacy interval guard is300ticks. The shipped opted-in profiles now use
independent staple/special layers, as recorded below; other merchants retain
the legacy low-shelf policy. Specials use1200game ticks from actual installation,
while staple top-ups retain the strict greater-than300tick guard.

`PlaceConsequences.OnZoneEntered` already tops up listed goods when an authored
place condition holds, with its own saved interval stamp. This is a possible
integration seam, not a verified trade-route graph or special-consignment system.
Concord carriage holds actual entrusted goods; merchant supply should not be
confused with transmitting information or the player's freight parcel custody.

The shipped pilots verified shared game time, shelf accounting, generated
payloads, valuation, save graphs and bought/sold custody through the existing
trade infrastructure. New profiles must qualify those contracts for every new
payload and preserve the documented limits.

## First bounded implementation and acceptance

Start with one existing merchant: retain explicit staples and add a small themed
special lot with saved quantities, identities and rotation state. Demonstrate a
second contrasting merchant profile or place before claiming broad reuse. Add
one supported machine/project condition that expands future eligibility, with a
real alternative to doing that project. Route-based eligibility follows a
verified route condition; no invented network is accepted merely by this note.

Use one authoritative game-time policy and stable persisted rotation decisions.
Menu reads, buy/sell, re-entry, travel and reload cannot reroll a current lot.
Evaluate long absences with bounded catch-up, not an unbounded backlog of free
consignments. No wall-clock stock economy, machine-toggle duplication, infinite
unique-item creation or purchase/resale profit loop. The refresh cadence, repeat
avoidance, unsold-item disposition, upgrade timing and old-save defaults remain
design choices to resolve explicitly rather than silently assume.

Obtain bounded independent design/source critique; write compiling native RED
and countercases; implement the smallest complete player benefit; run focused
trade/restock/ownership/valuation and save/reload checks. Actual GameBootstrap
shopping must show staple reliability, meaningful special variation, purchasing
the exact displayed item, free cancellation, saved purchases/current stock,
elapsed-time rotation and a visible lasting-project unlock. Tests and offline
roll samples do not replace that play. Update this note, the living dependency
map and continuation with accepted implementation and evidence.

Preserve all twelve machine statuses. This direction connects merchant variety
to lasting projects; it does not complete a machine or accept any hypothetical
route, production, NPC labor or economy simulation. Graphics remain cancelled,
capstan/cultures parked and Deck deferred.

## First pilot contract

The bounded pilot opts in the existing FarmerStock
and ArmorerStock declarations through a small authored profile catalogue. Existing
shelves and old saves retain all their goods. Missing saved consignment state is
initialized on authoritative arrival or explicit shop opening, never while loading
an entity or painting a screen. Other merchants retain their current behavior.

Farmer staples: three CandyCarrotSeed and two EmberwheatSeed. Armorer staples:
one LeatherArmor, LeatherCap and LeatherBoots. Top up only deficits after the
existing strict300tick interval, with a saved per-profile stamp; no instant
replacement on purchase. Existing purses, barter and prices stay authoritative.
The opted-in profile replaces that merchant's legacy low-shelf table reroll;
ordinary held/player-sold goods remain. Other conditional supplies remain under
their existing authority and do not overlap the new special bonus.

Each pilot consignment has two single-unit, audited existing items. Farmer
choices are KnotflaxSeed, HearthbulbSeed, SeamleafSeed and PitchpodSeed. Armorer
choices are FilterHood, ColdwardCloak, KilnfeltApron, AcidworkerApron and
GroundwireScreen. A stable merchant/profile hash and saved generation rotate
through the authored choices without replacing both slots with an identical
successive lot. These are ordinary distinctive goods, not globally unique relics.
True one-off issuance and a trade-route network remain later increments.

Specials stay for at least one complete game day after the current lot is
installed:1200 TurnManager ticks, using WorldClock.DayLengthTicks. Reconcile a
late visit once and give the new lot its full window; do not materialize missed
lots. No wall clock, current-turn random seed or runtime string hash. Opening a
shop can reconcile a due consignment once, but reads/repaint/cancel/reopen cannot
reroll it. An open shop or pending deal defers withdrawal of live offers.

The first lasting-project eligibility is the merchant's existing local
VillageOven stable repair or improved saved site state (including Sill). It adds one RoastedHearthbulb to the farmer's next
special lot; it does not replace the current lot. Existing PlaceConsequences
ToastedEmberwheat stock stays separate. This is eligibility for abstract merchant
supply, not a claim of simulated NPC ingredient consumption or production. The
oven's existing repair methods and ordinary cooking are alternatives. Read the existing site with TryGetExistingSite; StableRepair does not require
the caretaker-only ImprovedOven condition. No newly invented route flag.

Save exact special-member entity references, generation/install tick, membership
and bounded audited payload keys. CaptureIdentity excludes stack quantity and
inert bookkeeping; separately require a single unit and exact current custody. Install each special without merging. Prevent
incoming ordinary/sold stacks from merging into a current special. Both drams
and barter transfers must relinquish withdrawal eligibility transactionally;
rollback restores it, committed purchases permanently leave it, and buyback is
ordinary stock. Rotation withdraws only demonstrably untouched, unreserved units
still owned by that merchant. Changed or escaped owners stay intact as ordinary
goods; no world-owned/purchased object is deleted. Withdrawn untouched generated
ordinary stock leaves the simulated shelf as a returned consignment.

Acquire a merchant operation claim and a reentry guard before factory or weight
callbacks. Keep metadata undo separate from inventory receipts. Admit only
audited bounded item payloads, validate participants/configuration again before
commit, and preserve independent callback injury, sales, item changes and goods.
Use the existing exact-owner insertion and transfer snapshots; no generic save
or transaction rewrite. Failed/capacity-limited preparation keeps the old lot.

Source corrections motivating this contract: current restock stamps after
callbacks; matching-blueprint counts do not encode provenance; ordinary AddObject
merges; and BarterDeal records offers before it claims their goods. The source
review requires independent safeguards for this new opt-in policy. It does not
claim a general repair of every legacy shop or a finished merchant economy.

The independent pilot critique also requires a trade-in-progress lease that
defers reconciliation while preserving independent-item nested trades, including
the same merchant. A blanket merchant claim in ordinary Buy/Sell/Barter would
change that existing contract; reserve the exclusive claim for stock generation.
Pending offers must be explicitly released on close/switch/success rather than
waiting for garbage collection. These corrections were implemented after native RED.

Usable staple counts require exact base-payload keys: modified, untradeable,
unsupported, empty or foreign-owned goods remain real goods but do not hide a
deficit in ordinary usable supply. Canonical staples merge through the existing
stacking routine only when every potential recipient matches that audited key;
special consignments stay separate. The exact GasMaskPart.Power field has a
merchant-only fingerprint path for filter hoods; ordinary inventory grouping and
physical stacking permissions were not expanded.

All factory work precedes shelf publication under a stock-operation guard.
Fresh goods must have the requested blueprint, valid distinct identity, owned
Parts and no world/inventory/equipment custody. Recheck prepared goods, usable
staple baselines, project eligibility, views and offers after callbacks and again
before commit. Independent injury or changes to stock remain intact when a
preparation is refused. Null withdrawal keys preserve the old good and let the
next lot proceed without retiring it.

The shop marks special rows with an asterisk, preserves existing equipment
readers, and reserves a complete second status line for the local oven. Its
states are repair guidance, repaired and awaiting the next consignment, and
baking included. Painting those lines reads saved state without restocking.

Final native verification: 234 tests, 233 passes, zero failures, one explicitly
skipped golden-regeneration utility, zero compiler errors; 64 new feature and
countercase executions. Actual GameBootstrap shopping covered both profiles,
readers, cancellation, an unaffordable armor purchase, exact seed purchase,
elder-guide acquisition, oven repair with one carried fire clay, unchanged current
lot, a later consignment adding an extra meal, and that meal's ordinary inventory
consumption for healing and cold resistance. Fresh loads at4630 and4870 preserved
the project, quantities, exact current stock, purchased seed and consumed meal.
The inherited strong/provisioned fixture is not acquisition or economy proof.
Mac artifact/startup delivery is recorded in the
[current receipt](Verification/MachineSystems/MacCandidate73FB59DA4/README.md).
Controlled packaged play, route projects, other profiles and wider survival/
performance acceptance remain separate gates. All twelve complete-machine statuses stay open.

## Next regional pilot: Quillhold copy-room supply

Bounded Editor acceptance after native checks, actual play and independent
source/gameplay-evidence review. Delivery is tracked separately. Source qualification found no opened-route
graph: Concord carriage currently connects two authored posts and gates service
by the player's ledger, not a repaired route. Do not call that a route unlock.
Quillhold has an existing finite project with a direct workmanship connection:
the Dark Copy-Desks concern consumes three actual LampOil items and persists
Quillhold.DesksLit. Its alternate collation sets ReportCollated, not DesksLit.

Opt in only a genuine ScribeStock declaration in authored Quillhold, retaining
other scribes' behavior and all old shelves. Dependable baseline: one InkVial,
one GrimoireCopy and one Codex04. A blank grimoire copy is a material for existing
copying/caretaker services, not a teaching book. Codex04 remains available as in
the old guaranteed ScribeStock entry. Merchant-only fingerprints may audit the
exact InkVialPart.InkAmount and ReadableDocumentPart.DocumentId; ordinary grouping
and physical stack permissions must stay unchanged.

Two-item special choices: WardGleamGrimoire, WateringGrimoire,
PurifyWaterGrimoire and MendingRiteGrimoire. Supplied copy-room lamps add one
DryingBreezeGrimoire to the next lot. Keep the existing game-day window,
bounded catch-up, slot/key custody and transaction rules. A pure read of an
already saved local condition must not backfill settlements. Revalidate both
place eligibility and condition after factory callbacks and before publication.
No new saved Part/field, timetable or production/route framework is proposed.

The existing lit-desks PlaceConsequences top-up-to-three InkVial authority
remains separate: both floors count held vials; never grant another conditional
three vials in the new service. Ordinary base supply is one, lit room supply is
up to three through that existing authority. Custom/invalid payloads remain
real goods and do not hide a deficit of ordinary usable baseline stock.

Existing Quillhold copying and Ward Gleam/Drying Breeze loan services remain
alternatives. Buying a special provides ownership/availability, not an exclusive
skill gate. These are abstract manuscript consignments; supplying oil does not
prove NPC ink/paper consumption or a newly simulated lighting cycle. No protected
document or mystery is disclosed by catalogue generation.

Independent bounded source/design critique requires compiling RED for exact
place/declaration eligibility, pure reads, all authored choices, baseline/codex
preservation, ink-floor coexistence, saved lots, callback condition changes,
next-lot delay and the existing custody/rollback rules. Actual qualification
must acquire and supply three real LampOil items, retain the current lot, verify
saved lighting, buy the later exact bonus book, read it and use its supported
drying outside the shop. Drying Breeze removes Wet and temporary water coating;
it does not drain pools, remove mire/acid or implement book preservation.
Generic liquid oil and pitchpod resin are not LampOil by analogy. Ordinary
supplier/straining access needs actual evidence; acquisition/economy acceptance
cannot be inferred from supplied fixture materials.

November23 working-tree qualification: compiling RED24 total/17failed/7passed;
first attempt had a test-access compilation error and is excluded. Independent
review reproduced fresh factory-payload corruption with four new failing cases
(empty/negative ink and missing/wrong document). Fresh canonical ink25 and
Codex04/codex-04 admission corrected it; final affected208total/207passed/0failed/
1Explicit utility skip/0compiler errors. Independent bounded source re-review
found no remaining P1/P2. This does not replace the open gameplay gate.

Actual partial GameBootstrap branch from retained4870: visited Sill provisioner
and Quillhold merchant without finding LampOil, then acquired two actual FrogOil
gourds by harvesting Reedfrog22842 at17.6. A second ordinary frog harvest at16.6
gave no oil. No LampOil has been created, bought or supplied in this branch.
Quillhold's two scribes have saved local profiles, base goods, starred specials
and the complete project hint. Verified F5/archive/stopped/fresh7100 preserves
both exact lots,2FrogOil,20drams,218/400HP and DesksLit=false. Archive:
/private/tmp/coo-quillhold-ordinary-supply-partial-earned-20261123. The strong
inherited loadout, read-only coordinate/supplier scouting, wall strikes and
ordinary combat/environmental costs are disclosed; no discovery, balance or
acquisition-chain acceptance. Bog-mire bodily coating continues causing damage.
Water's actual popup offers drenching, not washing; neither drenching nor
Drying Breeze has a mire-washing contract. That recovery gap is separate debt.

### Qualified provisioner supply prerequisite

Observed oil scarcity motivates dependable supply at genuine ProvisionerStock
keepers through the existing consignment contract. Qualified base floors retain
every previously guaranteed line: empty LiquidFlask1, Waterskin1, DriedMeat2,
Starapple1, Torch1 and CandyCarrotSeed1, adding LampOil3. Two daily specials
cycle through the original optional goods: Mushroom, RawMeat, EmberwheatSeed,
WaterTonic, CampHearth and PoisonGasGrenade. No Speed/Strength tonic expansion.
All existing goods stay; optional lottery rolls deliberately become saved
cycling availability. Ordinary wells, harvesting, straining, lights and crafting
remain alternatives. Purchases still pay real prices; this is abstract merchant
supply, not NPC production or a simulated shipping route.

Independent pre-RED source critique identified eight exact merchant-only scalar
audits: ReagentPart(PropertiesRaw,FlavorText), LightSourceFlickerPart(two finite
floats), CookablePart(Into), CampfirePart(two flags), HearthGlowPart and
HearthRestPart(type presence), PackableFixturePart(Kind,SetTick,CampName), and
all FIVE GasGrenadePart fields including SightCloud/SightCloudTurns. Never invoke
provider/reader/flicker methods or broaden ordinary grouping. Fresh hearths must
be cold, unfuelled, uninstalled kits; altered returned goods survive withdrawal.
Native RED/GREEN covers full catalogue/floors, canonical fresh corruption,
modified residents, sight-cloud changes, hearth lifecycle, exact subclass
refusal, old saves, reservations and bounded late refresh. Actual chain now passes: purchased3oil for49, supplied lamps, retained g2
through fresh8040, bought later bonus book25644 for107, learned the skill and
used it to clear finite-water Wet in the world. Fresh9320 retained book, skill,
project, exact remaining lot and water0/2/2. Final affected237/236pass/0fail/
1Explicit skip/0CS;59newcases. Independent source/gameplay-evidence review found
0remainingP1/P2. Fixture financing and staged finite-water demonstration do not
prove normal affordability, discovery or combat balance. See the regional receipt.


Current runtime publication203023f315f4b0e3942b2ffd251937e56abb35ac and universal
Mac artifact/startup delivery are recorded in the
[delivery receipt](Verification/MachineSystems/MacCandidate203023F31/README.md).
Editor qualification and packaged startup have distinct limits; controlled
packaged gameplay/performance and complete-machine gates stay open.

## Source qualification before the Cinderhold increment — November23

[The Cinderhold source critique](MACHINE-CINDERHOLD-SUPPLY-QUALIFICATION.md)
identifies a concrete promise gap: the pump concern names smith-sold spikes,
but WeaponsmithStock has no spike entry. One finite reclaimed blank makes only
one spike, while the lasting repair requires two. Actual pump/shop/census and
old-save baseline must precede any local dependable smith profile. Existing
pump commerce benefits remain; no new instrument-production or route claim.
At that source-only stage this was a candidate. The accepted bounded supply
contract follows; no complete machine is accepted.

## Cinderhold promised repair supply — accepted Editor increment

Cinderhold-only smith staples: Dagger1, IronSpikeComponent2, OakHaftComponent1,
LeatherBindingComponent1. All11other original optional identities cycle as two
single-unit specials; legacy stun roll1–2 becomes one per special slot. No
regional opening grant repeated, ordinary goods removed, or other smith policy
changed. Canonical fresh8component/5stun payload fields are bound; no new saved
field or generic grouping/stacking permission. Existing game clocks/custody
remain. [Native receipt](Verification/MachineSystems/CinderholdSmithSupply20261123/README.md)
records200/199pass/0fail/1Explicit/49new, old-town gap and alternate Tinker,
actual finite reclaim+bought spike into lasting pump repair, existing commerce
benefit and fresh10660. Fixture financing and modal/combat/level-up detours are
explicit. Current Mac7c248f7f0 includes this addition with separate artifact/
startup verification; no complete pump/route/NPC production or full-program acceptance.
