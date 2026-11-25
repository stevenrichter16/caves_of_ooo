# Local porters and visible shipments

User direction, November24,2026 (America/Chicago). **Design and architecture
with working local player-hire and selected NPC candidates.** Hire a local person to move a specific physical
load between agreed nearby locations. Show capacity, route, price and delivery
state. Selected NPCs also send real supplies by the same service, visible while
the porter travels. Preserve self-carrying, ordinary hauling, cargo sleds,
convenient shops and NPC services. This does not reopen capstan or graphics work.

Further user direction: avoid a predictable road-repair checklist on each
playthrough. Similar practical outcomes should admit meaningfully different
builds, tools and approaches. The player's problem is "move this useful load,"
not "complete the town's road project." Ordinary local porter service works
without first repairing a road or building a loading bay. Those investments
become optional when the player's chosen work makes repeated use worthwhile.

## Repeatable problems, different approaches

Use a small vocabulary of physical needs with varied local circumstances,
rather than giving every run the same ordered list of projects. Source stock,
load size, destination, existing access and available services can differ
within authored, validated bounds. Do not scramble established lore or guarantee
that every town requires a delivery. NPC shipments show ordinary work; watching
or ignoring them does not assign a quest or allow a settlement to deteriorate.

For the same repair-material load, a player might carry it personally, choose a
smaller explicitly quoted batch, hire a porter whose real free capacity admits
it, or use the existing local haul/sled route for eligible ground loads. Later,
supported portable processing might separate or transform material at the
source, leaving unwanted fractions in a real store and carrying only what that
project needs. A frequently used shortcut or clear loading space is another
investment, not the default answer. Each mode keeps its own mass, time,
capacity, access and finite-input consequences; tools are not interchangeable
keys to the same scripted reward.

One playthrough may justify a reliable workshop delivery route. Another may
favor light portable tools, smaller batches and different projects entirely.
Local materials and seasonal bonus work can suggest choices without taking
away comfortable exploration. Different methods that achieve the same physical
result receive the same resulting service benefit; no bonus is secretly tied
to pressing a particular "repair road" button. Demand must accept compatible
results through capabilities/material grades, not demand one named tool.

## Seeded roads and movable loads

The user additionally asks for procedural roads and movable items to vary
playthroughs. Design this as seeded physical opportunity composition. Keep
established places, regional identity, mandatory entrances and protected
authored geometry authoritative. At admitted local sites vary road bends,
forks, open alternatives, loading approaches, source placement and suitable
movable loads. Items have plausible provenance and finite quantities; they
are not a freshly generated reward for whichever tool the player carries.

Generate an ordinary accessible baseline first, then optionally compose extra
opportunities from actual owners and validated paths. A road is useful because
the actor/load can traverse it, not because its ground has a Road tag. Check
width, turns, doors, standing space, actor-specific hazards and access for each
advertised mode. A carryable package and a dragged beam can require different
routes. Never randomly block the only town entrance or make a basic service
depend on owning a particular tool. At least two supported approaches should
exist where a multi-approach encounter is promised; otherwise omit the optional
encounter and retain the baseline, rather than clearing foreign owners.

Example proposals: one seed offers an eligible heavy timber load beside a short
wide path, favoring ordinary haul/sled or an admitted porter mode. Another
offers smaller scattered stock, an accessible carrier service and a longer
cart approach, favoring light batches or paid carrying. A third offers a known
mixed lot with useful local fractions and an accessible processing site. The
outcome can still be supplying useful material; no road repair is mandatory.
Current sled rules are local and first porter mode is portable cargo, so a
heavy-beam porter requires a later tested hauling adapter.

The first seeded candidate uses the same kitchen service in both worlds. One
seed offers a nearby sorting lot, while another uses the existing paid clay
shipment. Both give players material for the pan and retain ordinary shop
stock. Actual play covered each path with fresh loads. The Editor supplied
the sieve after the player found the lot. This is a change in
available supply work at one kitchen. Broader road geometry and load modes
still need their own play and validation.
[Evidence](Verification/MachineSystems/WaysideClayLayouts20261125/README.md)

Use a deterministic world-seed/site/version stream independent of cosmetic
randomness. Save the resulting roads, owners, quantities and orders as real
world state. No travel, season, reload or updated generator version rerolls an
already generated site. Destruction and player relocation persist. Goods do
not respawn to keep an encounter solvable after deliberate removal. New orders
require stock that still exists. Generation never consumes a player's known
tools or changes the offer to force an unchosen build.

Native seed samples must test reachability, lawful accessible endpoints,
load-mode fit, preserved authored owners, finite budgets and deterministic
repeat results. Also test omitted/invalid layouts and loaded old sites. Actual
play must compare at least two different layouts and methods; a seed sweep
alone does not establish interesting replayability or balance. Regional road
networks and cross-zone freight need separate edge-connectivity work; the
first contract remains bounded to same-zone local routes.

## What the player encounters

A porter leaves a workshop carrying a labeled clay hamper, passes the player
on the road, and puts it at the repair yard's receiving bench. Examine says
"Clay hamper for Orrit's yard; delivering" while visible. At the yard, the same
two clay are available for the declared repair or trade. Talking to the porter
offers a similar local job for the player's own material. Watching ordinary
work teaches the service without a compulsory tutorial or a new crafting menu.

A hire previews the selected stack/box and quantities, total carried weight
including its container, named pickup/drop points, porter capacity remaining
after personal gear, agreed corridor, fee, approximate walking actions and
interruption terms. Example tuning, not balance acceptance: "your 12-weight
timber box; clay bank loading stone → workshop receiving bench; 32 clear
walking steps; 20 weight free; 5 drams, of which 1 is earned at pickup."
No paid order is silently split into extra trips or widened to nearby goods.

The route is an agreement about endpoints and an allowed corridor, with a
bounded walkable path recomputed inside it. A nearby obstacle may cause a
detour; a dangerous or inaccessible route causes a visible pause and revised
offer, not a surprise surcharge. A blocked doorstep cannot count as delivery.

## Reuse verified source, distinguish missing behavior

| Current source | Reuse or explicit limit |
| --- | --- |
| `AI/SpreadCollectorPart.cs` | Already moves an exact finite object from ground into a creature's inventory and then a container, with saved phase and action budgets. Its three-blueprint eligibility and passive scavenger role are specific. Reuse transaction/identity lessons; do not grant merchant freight by adding names to that whitelist. |
| `AI/Goals/GoFetchGoal.cs`, `MoveToGoal.cs`, `FindPath.cs` | Existing saved fetch phases, pathfinding and ordinary movement are foundations. They do not supply contracts, fees, reserved loads, destination access or safe cross-zone deliveries. Do not treat a timed-out movement goal as successful delivery. |
| `Items/ContainerPart.cs`, `Exploration/SecondExplorationObjects.cs:ContainerLoadPart`, `Items/HandlingService.cs` | Actual container contents and recursive mass, including positive Handling weight, already exist. Container MaxItems is an entry limit, not a carrying-weight budget. Both limits and normal actor capacity must pass. |
| `Items/CargoSledPart.cs` and normal drag rules | The same sled assists an eligible local ground load; it adds mass and does not grant cross-zone movement. First porter slice carries a portable package. Sled-assisted NPC hauling is a later mode using the same order, not an assumed existing ability. |
| `World/RegionalCargoPart.cs`, `RegionalSituations.cs` | Authored sealed recovery consignments and exact recipient bindings already exist. They are not a general moving supply service; protected consignments stay excluded until their own adapter and acceptance. |
| `Economy/MerchantStockService.cs`, `MerchantStockPart.cs`, `TraderRestockSystem.cs` | Existing staples/specials are abstract supply with their own game-tick windows. Physical delivery must have a separate exact transfer adapter; it cannot both add cargo and trigger an extra stock roll. |
| `AI/AISelfPreservationPart.cs`, `AI/Goals/BoredGoal.cs`, `AI/BrainPart.cs` | Existing wounded retreat is an idle behavior, not universal survival arbitration. A delivery goal must explicitly yield to danger, incapacity and supported survival needs; registration order of idle Parts is insufficient. |
| `Save/SaveSystem.cs` | Public fields, Entity references, lists and typed records provide a serialization route; native graph/F5/fresh tests must prove the concrete new records. Creation/load hooks never award goods, fees or completed trips. |
| `Presentation/Rendering/SpreadCollectorCarryView.cs` | Existing carry visualization is tied to a collector and three exact rendered objects. It is not a general cargo renderer. First slice uses existing actor/item rendering plus a functional visible carrying label/reader; no new art program. |

Paths above are beneath `Assets/Scripts/Gameplay/` except the presentation
entry. These are source findings, not new porter gameplay observations.

## Composition and authority

Definitions describe service profiles and demand. Instance Parts hold actual
participants, condition and commitments. P1 currently uses
`LocalPorterPart`, `LocalPorterLedgerPart`, `LocalPorterReservationPart` and
`DeliveryStoragePart`. The table describes the intended responsibility split
when a second caller needs it. Player hires and NPC orders should share the
physical transaction and movement rules.

| Proposed component | Owner and responsibility |
| --- | --- |
| `LocalCarrierPart` | Attach to any suitable NPC with Brain, Inventory and ordinary movement. Declares a bounded service area, availability, carrying mode and quoted service rate. One active order initially. Capacity is queried from actual actor/gear state, never a job-title multiplier. |
| `DeliveryPointPart` | Attach to a loading stone, yard container, receiving bench or other admitted owner. Declares permitted standing/loading cells, source or receiving role, access policy and actual storage endpoint. A label alone grants no access or storage. |
| `ConsignmentPart` | Attach to a physical nonstacking package with Container and ContainerLoad. Identifies the order, legal owner, permitted custodian and recipient. Its contents are real Entity references and quantities, not blueprint promises. A loose selected stack is packed by an explicit transfer, with packing mass/cost previewed. |
| `LocalDeliveryLedgerPart` | One bounded saved service ledger on an established zone state owner, independently of porter/package destruction. Stores order records, escrow, revisions and completion receipt. Existing state owner must have proved save/cache retention; otherwise introduce and qualify one before orders. No duplicate authoritative order on package or AI goal. |
| `LocalDeliveryOrder` record | Contains exact source/destination/porter/package references, unique order ID, selected item references and quantities, phase, observed locations, fees, route limits, last game tick, blockage reason and idempotent handover/payment flags. IDs aid diagnostics and saved rebinding validation; a same-ID replacement is not authority. |
| `DeliveryGoal` | Reads the current ledger order and performs approach, load, ordinary steps and handover through shared commands. Keeps only disposable path caches. It cannot create cargo or settle fees itself. Reconstructing a missing goal reads saved phase and never replays pickup. |
| `LocalDeliveryService` | Quotes, claims participants, reserves an exact physical package, and atomically commits booking, loading, handover and settlement. Adapts ground/container/inventory endpoints through small explicit contracts. Reuses inventory transactions and emits committed events. It does not own pathfinding, merchant catalogue generation or crafting. |
| `ShipmentDemandPart` | Optional NPC/worksite demand definition: finite desired units, eligible source/provider, destination adapter, and at most one outstanding request. Queues the same order after reserving actual source stock and a real budget. Ordinary vendors without this Part do not acquire simulated dependencies. |

Legal ownership, physical custody and receiving permission remain separate.
Wearing the porter role grants neither ownership nor permission to sell the
cargo. Existing Owned tags/properties have several scoped consumers; this slice
needs a tested custody/access adapter, not an assumption that they already
form one global ownership service. Job participants cannot independently trade,
consume, equip, split or reassign committed units through ordinary menus.
Supported theft/forced drops use explicit interruption and custody transitions;
do not make the cargo indestructible or invisibly restore it after theft.

Initial booking accepts an already packed real container at a lawful delivery
point. Loose selected units must first be packed through an explicit supported
transfer into an available physical container; packing supplies/mass are never
created for free. This keeps durable reservations on one package, not partial
quantities spread across every source stack and consumer. The saved consignment
permission outlives a short InventoryTransaction claim. Ordinary pickup, trade,
opening, unloading and consumption must consult that shared custody/access
policy, with native counters proving it. Removing a menu row alone is not a
reservation. The contents cannot change independently while booked.

Use common validated transitions, provisionally named `DeliveryQuoted`,
`BeforeCargoHandover`, `CargoCustodyChanged`, `DeliveryPaused` and
`DeliveryCompleted`. Before hooks can veto before payment/transfer; completion
notifications occur after commit and cannot repeat it. Machines and merchants
subscribe through receiving adapters. Neither a name match nor a completed
animation counts as transferred stock. Custom effects use shared hooks without
editing a switch for each merchant/porter pair.

## Finite order lifecycle

1. **Quote:** read-only selection and route/capacity/access preview. Cancel is
   free. A stale quote cannot choose replacement cargo, porter or destination.
2. **Booked / approaching pickup:** reserve the exact packed load and porter once,
   debit the agreed fee into ledger escrow in the same transaction. Goods stay
   physically at source. No goods creation or duplicate booking.
3. **Loading:** adjacent porter and both access policies revalidate the reserved
   package and its actual contents; move the same package from the source into
   the porter's inventory. It is absent from its former store. Publish pickup
   and the departure fee once. Do not repack, substitute contents or split a
   different source stack after booking.
4. **In transit:** each ordinary movement action uses real mass, condition,
   obstacles, doors and known hazards. Normal personal gear stays outside cargo.
   The package can remain in its actual inventory while pausing safely.
5. **Awaiting handover:** porter reaches a permitted standing cell beside the
   exact receiving endpoint. Recheck permission, storage space and identity.
   A full receiver pauses with cargo still carried; it does not drop a duplicate.
6. **Delivered:** one transaction moves the actual package/contents to the agreed
   receiving store, applies the destination's receipt and releases remaining fee.
   Mark settled once and release reservations. Unpacking is an explicit endpoint
   policy; an empty package is returned/reused only if the quote says so.

Paused reasons include obstruction, changed access, full receiver, injury,
unsafe conditions, changed load and unavailable endpoint. Show the exact reason
and real last-known location. Resume revalidates; a paid revision may change the
route or endpoint only through a fresh player agreement. No silent rerouting to
the player's changing position. A porter may have ordinary social life between
jobs; one job never freezes every other NPC in the settlement.

Pausing pops/releases the delivery goal so the ordinary threat/retreat behavior
can actually run. The ledger retains the order and cargo authority. A bounded
resume check may enqueue the goal again after the stated blocker clears; it
must not overwrite survival goals or leave a waiting delivery goal atop them.

Cancellation before pickup returns all escrow and releases the reserved goods.
After pickup, request a return to an agreed accessible point; the same package
travels back and a stated departure fee remains earned. Example 5-dram quote:
1 paid at pickup, 4 held until delivery or refund on settled cancellation. Death,
lost cargo or a destroyed destination leaves a recoverable failed order and its
actual surviving goods; refund unearned escrow once. No implicit insurance,
replacement shipment, duplicated purse or cargo resurrection. A safe reachable
fallback drop requires explicit agreement and custody/ownership protection.

## Expected dates, missing porters and search parties

Additional user direction, November24local/25UTC: recipients expect a package
by an agreed in-game date. If it does not arrive, the recipient can go looking
for the porter, or send available followers/employees when their resources and
social authority support that. The player who killed a porter can later stumble
upon that recipient or search party along the actual route. This is proposed
social consequence, not implemented crime detection or an automatic rescue quest.

The same delivery order stores ExpectedByTick and an agreed grace interval,
displayed as the game's date/time. Quote realistic route/work expectations;
never accept an impossible deadline without saying so. A saved recipient-noticed
flag and one case reference prevent repeated dispatch on reload or every tick.
Missing the date opens concern/inquiry; it is not proof of death or player guilt.

Add one bounded missing-delivery case to the authoritative local ledger,
referencing the order rather than copying its cargo/payment state. Save the
recipient, actual assigned searchers, expense/provision allowance, known leads,
last genuine report, visited areas, phase and closure reason. A search behavior
visits pickup, destination and admitted parts of the agreed corridor/last
reported place, through ordinary movement and survival. It cannot spawn actors
beside the player, teleport to an unseen corpse or reconstruct lost goods.

A capable recipient may search personally. A wealthier or better staffed NPC
can assign real available employees/followers, with finite funds/provisions and
authority to ask them. Wealth does not conjure guards or make an employee
invulnerable. Reserve those people against simultaneous deliveries/searches;
cap active parties and preserve essential settlement availability. A search can
pause, retreat, return or end unsuccessfully when its bounded leads/actions run
out. Failed searches do not create endless replacements.

Initial knowledge is limited to lateness, expected package, agreed endpoints,
corridor and reports actually received. Finding the porter alive and blocked,
discovering the body, recognizing actual cargo or hearing a witness can update
the case. Evidence of murder or theft must come from supported observations;
the player merely being nearby or holding an unrelated similar item proves
nothing. The party may ask questions, recognize its own labeled consignment or
react to a witnessed attack under normal social/combat rules. Accusations,
justice and long-distance pursuit are separate later systems, not free powers
of this case. A player can return goods, help, lie through supported dialogue,
avoid the group or attack, with only the corresponding implemented consequences.

Committed late delivery, the porter's return, lawful cargo recovery or the
recipient's death closes/revises the case once. Distant searchers learn changed
facts through an actual meeting or another supported communication method;
they do not instantly know a remote handover. Their next supported report or
return resolves their assignment. Timeliness may affect explicitly agreed
contract terms, but compatible late-delivered materials still provide the same
physical repair/service result. No secret named-tool bonus or unsolicited late
fee is introduced.

Reconcile deadlines with simulation scope before enabling this phase. Use the
same saved game clock, never wall time. With P1's unloaded local pause, a due
date can pass but there is no fabricated offscreen walking, harm or discovery;
the recipient can notice and start physically searching at the next active
opportunity. State service timing/pause terms in the agreement and add no
automatic player penalty for that simulation limit. Believable continued
offscreen deliveries/searches require a separately qualified bounded unloaded
route policy; do not imply that P1 already supplies it.

The wayside candidate supplies an exact boundary for the first deadline slice.
The existing `WorldClock.CurrentTick` reads the persisted turn clock, with
1,200 ticks per game day. The order already saves a unique ID and the handover
commits when the shelf takes the physical parcel. Unpacking and sale are
separate receiving actions. A delivered parcel waiting for Orven to unpack
cannot open a missing-porter case.

The first agreement budgets ten world ticks for each quoted walking step plus
one pickup and one handover, then allows forty ticks of grace. These are terms
for the bounded local service. The order keeps its original estimate when the
path changes. The marker and receiving shelf can display the day, band and
tick within that band. Older shipments have no retroactive agreement.

After the grace period, the living assigned recipient can notice the missing
shelf during active local play when in reach. An incapacitated or absent
recipient delays that observation. The first observation saves one concern
against the order and can give a local message. A committed physical receipt
ends transit and closes any open concern regardless of unpacking or trade.
A settled cancellation also closes the concerned case. A carrier's death can
end the physical delivery but gives the recipient no information by itself.
Physical shipment status and knowledge of what went wrong remain separate.
Add search movement after the late-case state round-trips and the ordinary
delivered case stays quiet.

The deadline candidate has actual movement, readers, travel outside the
shipment area, one local overdue notice, physical late closure and two fresh
Continue comparisons. The earlier carrying save retained its existing terms.
[Evidence](Verification/MachineSystems/WaysideDeadline20261125/README.md)

The first searcher can follow a bounded walk from the shelf toward the exact
loading marker when their own idle AI and current safety permit. They can
then return to the kitchen. The carrier already uses checked ordinary
movement and explicit endpoint approaches. Save the actual assignment,
phase and attempts against the case ID so a reload resumes from the
resident's saved position. The exact shelf supplies the return destination.
One short search can start forty ticks after a notice. Paid meal work and
ordinary survival decisions have priority. Both legs have a fixed attempt
limit, and the actor remains where their last physical step put them when a
route is exhausted. If the original loading point moves, the outbound walk
turns back to the original shelf. Losing the shelf ends the walk in place.
Finishing a check leaves the late case open until
receipt, cancellation or another supported resolution. The player can meet
and examine the same resident along the route.

The first personal search has a controlled Editor witness from the earned
overdue save. The real recipient made both legs, resumed after a fresh load
and returned to his existing trade inventory. The delay was supplied with
an ordinary Rooted effect on that branch.
[Evidence](Verification/MachineSystems/WaysideSearch20261125/README.md)

The existing death-witness effect conveys distress and has no durable report
payload. The corpse spawner records a source identity, and a visible real
box can also provide a lead. Inspect evidence in the searcher's ordinary
sight and along the admitted route. Identifying a body can establish identity.
Accusations require a separately supported observation or report. Prove
recognition and reporting after a first unresolved search and return.

The wayside porter inherits the existing creature corpse spawner and its
`CreatureCorpse` outcome. The native death flow spills the carried box
before the death event creates a corpse. For the first recognition slice,
record one physically seen lead. A nearby corpse bearing the actual carrier
identity can support recognition. The exact spilled crate can support a
separate lead if it is visible on the ground. Save the observation's kind,
location and tick. A later report or lawful transfer carries its own proof.
Include an ordinary player-caused scene and an obscured or wrong-body
control. The existing corpse's killer metadata is internal.

Start with Orven recognizing the porter's body during the existing outbound
walk. Native death drops inventory before the corpse spawner records its
source ID. The terminal ledger retains its worker and crate references, and
the saved entity graph can retain references off the map. That gives the
searcher an order identity to compare when he actually sees a ground corpse.
Use his own sight range and line of sight. Record the observation once in the
search assignment and surface it at the kitchen when he actually returns.
The first cases should cover a body behind a sight blocker, a different
porter's body, a live carrier and a fresh load after an observation. Leave
cargo and the ordinary recovery path in place throughout.

The local candidate now has that first player-caused encounter. Orven's
witnessed reaction temporarily took over. He later resumed the same walk,
recognized the actual corpse in sight, and returned before the shelf posted
his report. The resulting save preserves the same crate and clay beside the
body. The report adds information while the shipment remains open.
[Evidence](Verification/MachineSystems/WaysideRecognition20261125/README.md)

For physical recovery, use the normal deposit of the exact surviving crate
onto the available shelf. The failed order has already released custody and
settled the original fare. The receiver can recognize the original measures
by their saved parcel and content references, then record receipt and stock
them through the existing trade path. Preserve the failed transit record and
avoid paying a second fare. A lawful substitute or an altered parcel requires
its own policy. A same-looking crate, a partial parcel or stock on the ground
should leave the case open. Qualify ordinary pickup and deposit from the
preserved aftermath, plus one downstream use and a reload.

The local candidate now connects ordinary pickup, the original shelf and
Orven's existing trade inventory. Actual play left the corpse on the ground,
returned the intact crate, bought the same recovered lot and repaired the pan.
The returned crate stayed on the shelf and the recipient retained his original
stock. Fresh Continue matched stock-in and downstream repair checkpoints.
[Evidence](Verification/MachineSystems/WaysideRecovery20261125/README.md)

Successful withdrawal records that the box was unpacked. An original stack
can retain its identity after a compatible refill, so exact stack identity
alone does not establish the original custody. A refilled box can go on the
shelf and be picked up with its contents. It leaves the inquiry and the
cook's stock unchanged. Reading and refused transfers preserve eligibility.
[Recovery and storage verification](Verification/MachineSystems/PorterParcelIntegrity20261125/README.md)

First proof after ordinary P1/P2 deliveries: one overdue shipment, one recipient
personally following known leads, and a player encounter with that same moving
searcher after killing the porter. Second proof: one existing employee searches
for a wealthier recipient through the same case machinery. Test living delayed
porters, late legitimate delivery, no witness, conflicting reports, recipient
death, staff unavailability, budgets and mid-search save/fresh; no automatic
perfect guilt inference or repeated dispatch.

November 25 source check for delegated work. The current wayside potter is an
actual independent supplier at the loading point. Nothing in his saved role
makes him Orven’s employee or puts him at the receiving shelf. The personal
search and reader both identify the recipient directly. A paid delegated
trial needs an explicit offer, an existing eligible worker who can actually
meet the recipient, one reserved case and a real returned report. Preserve
the personal search when the offer cannot be funded or staffed. Plan that
small trial before extending search routes across settlements.

### Delegated search trial

The first delegated trial is one kitchen hand in eligible fresh delivery
layouts. The hand is present before any overdue event. Their saved employer
is the actual cook. The sorting layout, established saves and delivery layouts
without a safe place for the hand keep the existing service and personal
search. The supplier remains an independent potter.

After the existing notice and grace period, the cook can offer one two-dram
search from his current purse if both parties can meet by the shelf and the
hand has a safe, bounded route. Payment and assignment commit together.
The cook stays at the kitchen, and the hand travels using ordinary search,
movement and survival behavior. If no eligible hand accepts, the cook can
use the existing personal search. If the hand misses the agreed return
time, the cook can follow up personally without learning why the hand is
late. The paid hand heads back once the agreed search time has passed.
No reward is created by reloading,
returning the parcel late or claiming a second search.

A hand carries observations back. The shelf receives a saved report only
when the hand can speak to the cook in person by the kitchen. An unavailable
cook delays reporting. A lost or interrupted hand conveys no news. After a
real report, the notice retains it independently of the person who delivered
it. Receipt and case closure still follow physical delivery or recovery.
Before implementation, independent source critique confirmed that the
current layout has no eligible employee and identified the 24-attempt search
limit, line of sight at the handover and unintended merchant restock as
requirements for this trial.

The v22 trial has a guided fresh-layout proof. On the actual route, the
working kitchen stayed approachable for trade. The paid hand returned to
the safe standing position after delivering his report. The delayed parcel
subsequently closed the same case and stocked the same physical clay lot.
The imposed delay and other verification limits are recorded with the play.
[Evidence](Verification/MachineSystems/WaysideDelegatedSearch20261125/README.md)

## Routes, survival and time

The P1 candidate quotes up to 32 walk edges for one portable package and
one active order at its loading marker. The first selected NPC shipment can
use the same bound. Larger budgets for multiple simultaneous porters or
longer routes require evidence from their own slice.
Reject cyclic/nested/ambiguous custody and packages the actor cannot actually
carry. No party recruitment requirement, magical capacity or cross-zone warp.

Quote/revalidate an actor-specific traversable path, accepting at least one
usable standing cell at each endpoint. Reserve cargo, not the entire road.
Local sidesteps/waits prevent porters permanently blocking doorways. Replan on
relevant changed cells/endpoint version or bounded retries; do not run a full
search for every world entity change. Exhausted retries pause instead of
burning unbounded turns or manufacturing a detour through a hazard.

Danger, incapacitation and supported survival decisions outrank delivery. Do
not put the delivery above threat checks or repeatedly clear flee/retreat goals.
If an actual exposure policy requires water/protection, its ordinary finite
water, clothing and shelter rules apply to this NPC too. A job may decline an
unsafe route. It never promises immortality, free water or cargo-based healing.
Ordinary calm settlement routes remain comfortable; seasonal conditions mostly
offer extra optional work rather than causing unattended settlement decline.

All progress uses authoritative game ticks. The first same-zone slice **pauses
when its zone is not actively simulated**, and says so in the service reader.
No wall-clock work, catch-up delivery, unseen injury, automatic endpoint
generation or reload reset. Test that pause honestly. Adjacent-zone routes are
a later separately qualified increment requiring explicit edge handoff, unique
actor/package custody and bounded unloaded work; do not advertise them early.

## Selected NPC shipments and readable world behavior

Begin with one repair supplier and one vendor whose actual source stores are
known. Reserve material that exists. Do not drain unrelated merchant inventory
or create a crate every morning because a role has a demand string. Demand
queries once at a bounded game-tick interval, caps outstanding work and reserves
the import budget before dispatch. Finite sources stay exhausted; later
production needs its own finite inputs/time/waste rules.

For an opted-in merchant, a delivered import is a distinct tracked batch of
ordinary sellable items, transferred once from cargo to their real store. It
does not call an opening-stock roll, advance specials generation or expire with
an unrelated special-stock rotation. Existing baseline staples/abstract supply
remain convenient; an actual item cannot simultaneously belong to virtual
supply and a physical shipment. The first import should be additional regional
stock or a project input with a distinct benefit, not the vendor's basic food.

Resolve import merging explicitly before P2. First receipt targets a dedicated
real receiving container, with a shared trade/work adapter exposing its goods.
Compatible imports stack there through ordinary payload-safe rules. They cannot
merge into an abstract special-stock stack that a rotation would later delete.
The import receipt/units and physical container remain outside special-stock
retirement and automatic refill accounting; viewing them never grants another
allocation. If the first vendor has no such receiving adapter, use the worksite
receiver until it exists rather than spawning a pretend merchant delivery.
Wider unified-inventory merging needs quantity-aware allocation accounting and
its own tests; preserving physical import goods takes priority over an unsafe
automatic merge.

Only visible porters expose current public labels, broad load type, recipient
and activity. Sealed cargo keeps unknown contents unknown; knowledge of a label
is not an assay or ownership proof. After losing sight, the journal says
"last seen" at its observed location and tick, not a live unexplored map dot.
Actual delivered goods can be traded/used normally after the receiver commits.
Do not stage an empty walking loop while separately spawning goods at the shop.

Reusable compositions, all proposed:

| Need and place | Same service, different Parts/data | Useful lasting project |
| --- | --- | --- |
| Morrowfast repair yard | Portable known clay/timber package, bank/yard delivery points, repair-store receiver | Optionally clear a loading approach for a larger load; ordinary smaller loads and self-hauling already work. |
| Surface farmer → provisioner | Finite harvested seed/produce batch, public recipient label, merchant-import receiver | A receiving shelf adds actual storage headroom; a road repair shortens the walk. Food staples remain available without it. |
| Cinderhold workshop | Known mineral/components package, workmanship-sensitive receiver and lawful source permission | A loading pad or later sled service admits heavier work without weightless loads; smith tools determine processing, not porter title. |
| Sumphold raised stores | Dry storage access and safe raised path, ordinary crate; wet/boat modes require later adapters | Boards/accessible landing provide a supported route where actual terrain needs one. Do not invent a water barrier where walking already works. |
| Catacomb settlement, later | Same custody and handover with locally appropriate light goods, quiet work/receiving customs | Protected loading space benefits permitted living-light cargo after its finite containment policy exists. Glow-Quartz is not a universal battery. |

One farmer's delivered raw goods can feed a real mill/still/other admitted
machine; its outputs can become another physical order. Each leg has inputs and
a receiver, never a free multiplier for connecting nodes. Seasonal surplus can
offer extra paid loads. An improved road lowers observed time/quoted work; a
larger bay increases compatible load size; a machine expands accepted demand
only when its real processing capability exists. None imposes permanent upkeep.

Local voice and practice follow `Lore/README.md`, `10_Bible.md` as amended by
`11_SecondSpine.md`, `MYSTERY-LEDGER.md`, `History/08_MaterialCulture.md`,
`History/02_Geography.md`, faction profiles and `Voices/VOICE-CARDS.md`. The
Concord carriage form (`Codex/06_ConcordCarriageContract.md`) supports terms,
consignments and bureaucratic humor; it is not evidence of implemented global
insurance or a license to make all cultures Concord clones. No protected
mystery is settled by tracking goods or testing a package.

## Small complete increments and acceptance

### First physical handover at Gleaners'

The public manual handover is Editor-qualified. Actual play deposited two
prepared measures in the real box, saved and loaded the full shelf, withdrew
that same stock, and spent it to reopen the well. The ordinary route supports
self-carry. The paid worker and eligibility under live hazards remain open.
[Evidence](Verification/MachineSystems/LocalReceiving20261125/README.md)

Start near the western cellar landing. A small portable wooden box takes two
measures of lining clay. It can carry two separate cellar measures, a prepared
stack from Orven or a mix. Packing happens on the ground and uses the actual
carried items. Shell and contents count toward the load. The open shelf near
the well takes one full box. Use the ordinary container actions to deposit and
retrieve it. The player can also carry loose clay directly to the well.

The shelf admits only that bounded cargo and requires a live adjacent actor
and exact custody. Both the parcel and the clay remain recoverable. A partial
box can still be carried and unpacked. The public shelf has no hidden inventory
or private claim. Work consumes the recovered measures using the existing well
repair, so a handover alone leaves a broken well broken.

These are optional fresh-site fittings. Admit them only when their own physical
placement and approaches fit around the already committed district. Keep the
stairs and the ordinary repair route available on older or obstructed sites.
The first carrier will use this same physical transfer and disposal rules, with
reserved custody and an explicit fare. The seed 64 play branch witnessed a
villager killed near the southern road, so an ordinary nearby resident cannot
serve as a promised porter by proximity alone.

**P1: one paid local physical delivery.** One player hire, one package and two
real endpoints; normal capacity/path/access, free quote/cancel, loading,
visible ordinary walking, handover, fee settlement, interruption and F5/fresh.
Demonstrate a second compatible NPC/load composition without changing the
transaction engine. Self-carrying/hauling remains a usable alternative.

Start the paid route with a porter beside the payer and an actual loaded box
at the loading position. A free quote reads the carried load, remaining weight
allowance and a short ordinary walking path to a clear standing cell beside
the shelf. The first public route accepts dry open ground and a porter who can
take the work. The fare is a fixed hiring amount plus a charge for distance
and loaded weight. A route refusal leaves manual carriage available. The
worker's current danger and the path must be checked again before taking the
fare and each time the worker moves. A quote cannot reserve cargo or promise
its eventual delivery. Keep the accepted order and escrow on a separate saved
owner. Commit the existing physical shelf receipt before settling the worker's
pay. Interruptions leave the surviving parcel where it actually is.

**P2: selected NPC shipment.** A separately reserved finite source supplies one
real vendor/worksite through the same order and movement. Watch actual pickup,
road passage, receipt and a downstream trade/repair using those goods. Confirm
unselected vendors/staples are unaffected and the abstract stock system cannot
double-supply the delivered batch. P1 is not acceptance of P2.

The existing merchant reconciliation creates staples for configured floors and
creates and retires its own tracked special lots. Those memberships are poor
receiving authority for a physical shipment. The first selected shipment needs
its own receipt location and an exact-owner transfer into service or trade.
If sold directly, the delivered entity must enter the real trade inventory
without joining the special-lot retirement list. Pick a finite source and
recipient with an existing reader and useful consumer. Keep the worker's pay,
the cargo and ordinary replenishment independently accountable.

The current Gleaners' candidate demonstrates a player booking and a public
receipt. It has no NPC supply sponsor at its loading point. Orven's prepared
stock is real, but it starts in another zone. Treat cross-zone delivery as a
separate qualification before tying that cook to this route.

For the first NPC candidate, use Orven's own kitchen zone at
`Overworld.12.11.0`. Stage a separate optional local supply point with one
actual packed lot of compatible clay, a dedicated finite purse, and a lawful
route to one actual receiving shelf. Do not reserve Orven's opening trade lot
or his kitchen escrow. Omit the optional shipment if the ordinary kitchen
footprint or access cannot be preserved.

`TradeSystem.GetTraderStock` reads actual inventory objects with
`CommercePart`. Both fire clay and prepared lining clay have that Part and
the existing pan repair admits both. Once the sealed box reaches the shelf,
move the exact clay into Orven's trade inventory through a separate committed
receipt. The kitchen's stock table supplies its existing foods and Orven's
prepared clay comes from a separate opening-stock declaration. Keep the
shipment out of staple floors, legacy stock-table quantities and the
rotating-special membership. At the first receipt boundary test that no goods
enter trade before delivery, the original lot remains intact, and reloading
or crossing an ordinary restock boundary never awards the imported batch again.

Start at the receiving boundary. One shelf opts in to stocking an exact
parcel for an adjacent trader. Save the receiving association and original
measure references with the shelf. Delivery and stocking are separate
transactions. A committed delivery leaves the parcel reserved in the shelf
until the trader can take both measures into their existing trade pack. The
stocking receipt claims both custody lists, preserves each lot and its ID, and
records the delivery ID once. The empty box stays on the shelf and becomes
available for reuse. A full pack, absent trader or replaced parcel leaves the
arrived cargo in place. This boundary can be tested with a physical receipt
fixture before the supplier and visible route are installed.

The first source uses two prepared lining-clay measures in one parcel. Orven
already sells this commodity, so leaving his exact opening stack untouched is
a useful control for the no-merge transfer. Stock the arrived stack as a
separate ordinary item. The existing foods, clay-opening allotment and coin
balance are separate from the supplier's cargo and fare. No shop restock,
catalogue rotation or trade view initiates the transfer.

This composition has a working candidate. Its first generated placement
exposed a shelf Orven could not reach from his regular chair. The corrected
site selects a shelf within ordinary reach of his standing spot and chair
after the existing kitchen commits. Seed 64 carried, stocked, sold and spent
the original lot in the batch pan, with separate fresh carrier and repaired
pan comparisons. [Evidence](Verification/MachineSystems/WaysideShipment20261125/README.md)

**P3: alternative approaches to one practical outcome.** Move useful material
with at least two genuinely different supported methods and explain their real
tradeoffs. An optional lasting improvement may provide a measured shorter route
or newly admitted load at a real pad, while another approach works without it.
Do not author a required road-repair quest to demonstrate the service. Show
another use outside the new service. Cross-zone freight, sled mode, multi-trip scheduling, hazardous/live
cargo, dynamic markets and a universal job scheduler remain later work.

Before runtime implementation: bounded independent source/design critique,
native REDs and exact acceptance cases. Cases must include insufficient purse,
capacity including shell/contents/gear, partial stacks, same-ID aliases, competing
reservations, reentrant callbacks, veto/independent injury, full/removed/moved
receiver, blocked door, danger/yield, death/forced drop/theft, escrow exactly-once,
old-save compatibility, mid-phase saves and unloaded-zone pause. Actual
GameBootstrap play with handfuls of useful screenshots, independent source and
recorded-play review, then exact-owned publication and coherent Mac delivery.
Design documents and native tests alone do not earn a porter implementation.

Bounded independent source/design review completed November24local/25UTC:
0remaining material P1/P2 after clarifying durable packed-load reservation,
separate physical import storage/stacking and release of paused delivery goals.
This approves the proposed contract only. The P1 code and play evidence are
tracked in the execution plan. P2 placement, economics, actual route play
and final review have their own gates.
