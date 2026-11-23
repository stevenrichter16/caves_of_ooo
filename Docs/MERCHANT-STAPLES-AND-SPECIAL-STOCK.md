# Merchant staples and changing special stock

User direction, November 23, 2026. The first bounded implementation now covers
FarmerStock and ArmorerStock, with a repaired local oven expanding the farmer's
next consignment. Native and actual Editor verification are recorded in the
[pilot receipt](Verification/MachineSystems/MerchantConsignments20261123/README.md).
Route imports, other merchant profiles and unique-item issuance remain proposals.

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

## Current source footholds

`TraderPart` already authors a StockTable, starting purse and opening allocation.
`TraderRestockSystem` already stores LastRestockTurn and, on eligible zone entry,
tops up buying power and rolls the existing themed table when its shelf falls
below the low-water threshold. Its current interval guard is 300 turns; that is
not a selected cadence for new specials. The implementation currently uses one
themed shelf policy rather than the proposed independent staple/special layers.

`PlaceConsequences.OnZoneEntered` already tops up listed goods when an authored
place condition holds, with its own saved interval stamp. This is a possible
integration seam, not a verified trade-route graph or special-consignment system.
Concord carriage holds actual entrusted goods; merchant supply should not be
confused with transmitting information or the player's freight parcel custody.

Verify the shared authoritative time path, shelf accounting, generated item
state, valuation, save graph, sold-item custody and existing conditional stock
before selecting a minimal implementation. Reuse existing trade infrastructure.

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
