# General sieving: useful mixed stocks before more recipes

November24local/25UTC,2026. Living content design plus bounded G1 field traction
and G2 size screening/finite clay-spoil loop, now Editor qualified.
The user asked
for a general size-sorting tool, then explicitly asked to brainstorm the missing
content and uses for its fractions. Current shipped sieve is the bounded
QuarrySweepings preparation remains separate. G1 adds a suitable coarse-material
consumer; G2 below now implements a bounded shared size partition and one real
finite source. Other mixed streams and material grades remain proposals.

## Player purpose

Ordinary travel should reveal optional mixed stocks worth taking apart: a
workshop's grading bin, an extra spoil pocket, rough traded crop stock or a
known cold fire residue. Sifting can reduce bulk, supply two useful materials,
prepare a grade for a project, or concentrate a fraction for another tool.
Both the retained and passing fractions can be valuable. Readers and previews
state contents, actual quantities, mesh choice, losses and destinations.

The same physical size rule should work across material identities and places.
Many materials can occupy one size band. A mesh is not an assay, density pan,
sterilizer or chemical purifier. Overlapping particle sizes stay mixed until
another appropriate operation separates them. Sorting never removes a harmful
property merely because a smaller fraction is collected.

## Source corrections from the first content pass

These are direct source findings, not new gameplay observations.

| Candidate | Actual current foundation | Consequence for proposed content |
| --- | --- | --- |
| Fire-clay workings | RepairClayBank is a finite Harvestable source of exactly4FireClay. FireClay repairs a well lining or batch pan for2units and is a hand-pump ingredient. | Strong existing fine-fraction consumers. Preserve the ordinary4clay harvest. Optional dry spoil still needs its own finite source/size contract; G1 now supplies bounded traction for explicitly suitable grit. Ordinary soil is not automatically fire clay. |
| Old wood-fire residue | AshPile is rendering-only. AshBed is a hot thermal/tile source. FuelPart has FuelMass and ExhaustProduct but no declared recoverable charcoal/ash manifest or feed identity. | First content would be a newly authored finite known-wood residue lot. No free mining of decorative ash or retrospective output for every spent fuel unit. Charcoal fuel and ash soap/soil consumers are new work, not shipped hooks. |
| Grain and seed cleaning | Emberwheat is already an edible2d4 **sheaf**, directly Cookable into ToastedEmberwheat. Knotflax harvest already strips/twists stems into2cord and returns1seed. | Preserve these convenient yields/actions. A loose detached viable-seed lot must be new authored stock; screening does not thresh a sheaf or reconstruct one from grain. Never force ordinary crops through cleaning to regain their current benefit. Chaff has no demonstrated packing/fuel consumer yet. |
| Workshop sweepings | RecoveredIronBlank and declared damaged-tool manifests already feed bounded cold processing. Existing material names and composition never imply extra recoverable stock. | New sweepings need actual declared metal identity/quantities and an outlet for both grades. Dust is not another whole iron blank, and a sieve does not concentrate metals of identical particle size. |
| Peat bank | PeatBank has a solid body, Peat material and10HP Destructible; it has no recoverable constituent manifest. | Cutting/destroying it is not evidence of a peat fuel/fibre yield. Recovery and both consumers remain proposals. |
| Grove compost | CompostRow describes reclaimed bodies; CompostCache yields a finite pocket find of1–3coins at60percent. | Exclude these from garden amendment feedstock. Any mature gardening compost must be a separately authored ordinary material, with no incidental change to the Grove's bodies or their meaning. |
| Renewable source budget | SourceStockPart refills on saved world-day intervals. | Do not use it for finite mineral, clay or fire-residue lots. A new optional weather deposit also needs a finite per-event/source budget and no travel/reload refill. |

Primary source paths:

- Assets/Resources/Content/Blueprints/Objects.json: RepairClayBank, FireClay,
  AshPile/AshBed, Emberwheat/Crop, KnotflaxCrop, PeatBank, CompostRow/Cache.
- Assets/Resources/Content/Data/Repairs/Tier1Repairs.json: clay-well-lining and
  clay-batch-pan; Assets/Scripts/Gameplay/Repairs/RepairRecipeRegistry.cs.
- Assets/Scripts/Gameplay/Items/HarvestablePart.cs: saved once-only source
  consumption, quantity range, claims and output overflow/rollback.
- Assets/Scripts/Gameplay/Materials/FuelPart.cs and
  Assets/Scripts/Gameplay/Alchemy/ProofingSources.cs: distinguish spent fuel
  from a recoverable material manifest and renewing source stock.
- Assets/Scripts/Gameplay/Preparation/PreparationRecipePart.cs and the
  MaterialProcess family: keep current convenient preparation/transactions.
- Assets/Scripts/Gameplay/Economy/TraderPart.cs, TraderRestockSystem.cs and
  MerchantStockService.cs; Assets/Scripts/Gameplay/World/MorrowfastContent.cs:
  distinguish one-time opening allocations from renewing themed stock tables.
- Lore/History/08_MaterialCulture.md, Lore/README.md, Lore/10_Bible.md as amended
  by Lore/11_SecondSpine.md and Lore/MYSTERY-LEDGER.md remain authority. Size
  grading creates no new ward, universal power source or protected revelation.

## First useful content bundle to qualify

**Clay-working spoil is the leading candidate**, because appropriate fine clay
already has concrete repair consumers. Its ordinary bank remains useful without
a sieve. Proposed extra dry loose spoil contains declared fine FireClay and
coarser aggregate, each finite. The proposed three-weight lot below supplies
initial quantities; access and placement remain unselected. A saved old bank
gains neither a new lot nor extra harvest output.

Coarse aggregate needs a practical use before this becomes a complete playable
addition. G1 below now qualifies bounded suitable-grit field traction. A lasting
workpad/drainage project still needs a concrete service benefit. Ordinary water is already walkable;
do not revive a fictitious bridge/access gate merely to consume gravel. Plain
aggregate does not become SilverSand, and its material alone grants no recipe.

Source correction: CombatUtilityActions currently admits **SilverSand only**
and writes grit for12turns. G1's separate explicit shared material capability
now admits suitable coarse grit, with clay/private-payload negatives and
unchanged SilverSand controls. It is the first bounded coarse consumer;
a project is deferred until a distinct lasting benefit has been qualified.

The next broadening candidates are a separately authored rough seed/crop lot
and a declared workshop residue. Fire residue remains useful design, but both
its provenance/budget and the fine ash consumer must be solved first. These
candidate lots are not three completed capabilities or three earned sources.

## Proposed content and uses

November24,2026 (America/Chicago). The user asked to continue developing the
content and its uses. These are content designs, not additional shipped yields,
projects, services or sieve operations. Favor four contrasting streams first;
the wider catalogue is a choice of future work, not one implementation batch.

### Dry clay-working spoil

A traveler sees a discarded dry sorting heap beside an admitted ordinary clay
working. Its reader identifies known fire clay and coarse suitable grit, states
the finite amount, and names nearby uses. The existing four-clay bank harvest
stays available without a sieve. Permission to gather the extra spoil must be
stated separately from permission to harvest or work at the bank.

Initial tuning proposal: one optional three-weight lot contains two one-weight
FireClay and one one-weight CoarseGrit. It is an additional authored reserve,
never inferred from an old bank's material or remaining harvest. A participating
fresh working receives its declared lot once; saved workings are not repopulated.
The mixture must already be dry and its eligible sizes distinct. Screening
does not dry wet sediment or make ordinary earth into fire clay.

Two clay already pay for a well-lining, batch-pan or field-rig repair; clay also
enters the hand-pump construction. Coarse-grit traction has bounded field
qualification under G1 below. A player could keep the clay, keep the grit, sell the unsorted lot,
or leave it. Portable screening should save paid work compared with an explicit
manual alternative; an ordinary container of individually takeable products
would not establish this benefit. Manual work and NPC-service costs remain
unselected until their actual action contracts are checked.

The lasting outcome is an existing repaired installation. Temporary footing
is a separate useful field use of the other fraction. Hauling several admitted
lots gives the sled a reason to be present without increasing a source's budget.

### Graded abrasive stock

Quarry and stoneworking offcuts can supply a new explicitly identified abrasive
stock with several grain sizes. An artisan's grade samples and a workbench
reader explain what coarse, medium and fine material each does. A finer mesh
retains a different grade; it does not make a more powerful mineral or identify
unlike stones whose sizes overlap. Appropriate hardness and treatment are
declared independently of size. Glow-Quartz is not a universal abrasive or fuel.

Proposed consumers are rough shaping of a new component, edge finishing, and
polishing an appropriate repair surface. These require actual separate work
capabilities; current weapon repairs do not already accept arbitrary grit.
Begin with one optional rough salvaged component that can receive a lasting
documented finish. Do not add routine weapon dulling or make established gear
depend on polishing. A finished component retains its real identity and cannot
be finished repeatedly for stacking bonuses or resale profit.

A public finishing bench could support the same work for the player and a paid
NPC service. Restoring it could broaden a suitable merchant's later eligible
tool or component consignments. Ordinary gear remains purchasable; the current
consignment is not rerolled. The bench, work operation and catalogue extension
are three dependencies, not three benefits already present in abrasive stone.

### Carpentry waste and packing materials

A permitted workshop offers a finite offcut sack from known untreated timber.
It contains differently sized sound splinters, curls/shavings and fine sawdust.
Screening grades the stock; it does not cut wedges or turn dust into structural
SalvagedTimber. Bark, treated wood, metal-contaminated waste and decayed material
retain their own identities and need different admission.

Proposed destinations: suitable splinters can be cut into wedges; clean shavings
can fill a padded freight insert; sawdust with an actual binder can make a
nonstructural filler. Kindling is another potential destination only after a
finite fuel rating and preparation cost are declared. Dust cannot substitute
for the sound lengths currently required by a gate or bench repair.

The first useful consumer should be a packing insert made from actual shavings
and cloth, not three new unused commodities. A packing station could enable
optional delicate-freight contracts with stated packaging requirements and a
known payment. Existing freight does not acquire new breakage rolls or upkeep.
The insert's contents, reuse or consumption and any return deposit must be
explicit in the contract; they cannot be refunded twice after delivery/reload.

The player chooses whether to prepare packaging, buy an insert, use a packing
service or take ordinary freight instead. A lasting station continues offering
this work without a repeating repair bill. Splinter and filler recovery waits
until those consumers are worthwhile; it need not burden the first sack.

### Supplemental detached seed lots

An optional rough seed sack appears at an admitted farmer or seed exchange.
Its reader identifies the crop and says whether these are viable detached
seeds. It is separate from ordinary harvested Emberwheat sheaves and the
existing Knotflax two-cord/one-seed yield. No new cleaning step is inserted into
either existing harvest. Only debris with a meaningfully different size can be
removed by this operation; equal-sized foreign seeds remain a mixed grade.

Existing planting is the immediate consumer. A regionally unfamiliar but
supported seed also gives a practical reason to carry the cleaned lot to another
community or base. Chaff can supply an explicitly admitted packing preparation
only if its capacity and treatment suit the job. Otherwise it has a stated
leave-behind destination rather than becoming compulsory inventory clutter.

A proposed seed-exchange project could admit additional regional seed varieties
to future ordinary paid shipments after a genuine route or equipment condition.
Keep existing staple seeds available; no calendar gate on farming, automatic
extra harvest or free production by an NPC. Seasonal gathering periods change
which optional lots are offered, not whether old seeds remain viable by default.
Winnowing is a distinct air-separation capability, not a synonym for screening.

## Wider content catalogue

All rows are proposals. Require an honest source budget and an implemented
consumer before adding an inventory commodity. Several source instances can
use the same capability; a row is not a new recipe per community.

| Mixed content and encounter | Useful fractions and destinations | Condition or dependency |
| --- | --- | --- |
| Road or quarry gravel pocket | Suitable grit for temporary footing; larger aggregate for a new admitted workpad or drainage installation. | A workpad must confer a concrete service or work capability. Ordinary walkable water is not a fictitious blocked route. |
| Known masonry demolition stock | Reusable chips for a suitable patch; graded aggregate/filler for a supported binder preparation. | Screening cannot restore a fired brick or replace structural stone with dust. |
| Already-crushed known ore | Smaller admitted ore-bearing stock for processing; retained pieces for further crushing. | Mesh sorts size, not metal identity or ore richness. Further crushing and reclamation require real work. |
| Declared clean metalworking offcuts | Large turnings for reclamation; filings for a separately qualified consolidation process. | No universal workshop manifest or whole blank created from arbitrary dust. |
| Cold known-wood residue | Remaining charcoal for a rated fuel use; eligible wood ash for a separately designed washing/soap process. | Fine ash consumer and total fuel/residue budget are missing. No arbitrary AshPile harvest. |
| Already-milled grain mixture | Fine flour for a new supported preparation; coarse meal for another dish. | New flour/meal consumers first. Screening is not milling; existing edible sheaves stay convenient. |
| Ordinary mature garden compost | Fine seedbed amendment; coarse unfinished material returned to a real compost destination. | Separate from Grove bodies; no instant extra crop/seed or perpetual conversion loop. |
| Prepared peat stock | Appropriate coarse fibres for absorbent packing; fines for a separately rated fuel preparation. | New finite recovery, drying and both consumers; PeatBank material alone grants none. |
| Broken known salt crystals | Fine salt for suitable measured preparations; larger salt for admitted curing/trade stock. | Dirt and contamination remain. No automatic equivalence to existing whole PaleSalt units. |
| Crushed pigment stone | Fine powder for a paint or finish; retained material for further grinding or a textured finish. | Declare pigment identity first. Bower-associated craft is a candidate setting, not a new sacred material fact. |
| Known abrasive quarry stock | Appropriate graded stock for shaping, finishing and polishing. | Hardness is separate from grain size; new finishing consumers precede extra grades. |
| Clean glass-workshop cullet | Larger suitable fragments for a supported repair/display; smaller stock for qualified remelting. | Sharpness remains; no glass-making capability inferred from an ordinary hearth. |
| Wet ordinary papermaking fibre | Screen-retained fibre for pressed/dried sheets; explicitly captured water or stated effluent. | Wet screening, pressing and drying are additional work. No pure-water output from dirty slurry. |
| Prepared known herb fragments | A suitable grade for a sachet or measured infusion; coarser material for its supported preparation. | No detoxification, potency multiplication or new medicinal effect inferred from finer powder. |

## How players encounter and reuse the work

Discoveries should carry their own explanation in existing Examine/action
surfaces. Examples of proposed presentation, to receive the relevant local
voice pass before becoming dialogue:

- A clay working exposes a red dry sorting heap. Its reader names two measures
  of known clay and one of suitable grit, and mentions the cracked local pan.
  The player can use the ordinary bank instead, screen the optional spoil,
  buy clay, or obtain a repair service where available.
- A smith's finishing bench shows coarse and fine samples beside a rough
  component. Inspecting it explains the lasting finish and the materials it
  needs. An abrasive find now has a visible destination, even if the player
  encountered the stone first.
- A carrier offers an optional delicate consignment and explains the padding
  requirement before custody or payment. Shavings, cloth, a packing service
  and a finished insert are practical alternative approaches.
- A farmer's optional rough seed lot names an already supported crop and
  planting use. At a later community the same seed can be planted or exchanged;
  there is no compulsory cleaning of the player's ordinary harvest.

These are persistent opportunities rather than four mandatory quest stages.
Placed useful installations, goods in merchant specials, work orders and
recognizable discarded stock provide multiple entry points. Recovery and use
should remain worthwhile without first reading a distant quest notice.

Seasons change available work and local materials. A wet period can expose a
bounded extra bank reserve; actual dryness determines whether dry screening is
available later. Harvest periods can bring supplemental seed lots, and local
work schedules can bring appropriate offcuts. Weather may reduce a supported
drying process's work or expose a bonus reserve. The calendar neither creates
material on demand nor makes ordinary exploration uncomfortable. Underground
supplies follow their own actual local sources rather than direct surface-rain
credit. A new weather event requires its own saved finite allowance.

## Recommended breadth and dependency order

1. Qualify shared suitable-grit traction, then the one finite dry-clay lot and
   genuine size-based screening. Earn an existing repair plus a useful field
   consequence in actual play. Preserve the legacy quarry recipe separately.
2. Qualify one lasting finishing consumer and graded abrasive stock. Demonstrate
   more than one admitted grain size without adding routine equipment wear.
3. Qualify one packing insert and optional freight consumer, then shavings
   recovery. Add wedges/filler only when their independent uses are ready.
4. Qualify an optional detached seed lot against existing planting. A seed
   exchange, regional shipment and weather eligibility remain later extensions.

Do not implement all four by spending a batch on one large composition or job
framework. Use small complete capabilities with medium depth; new sources and
grades count as content for those capabilities, not additional feature totals.
Exact yields outside the proposed clay lot, paid-work savings, access, placement,
service prices and lasting-project costs remain tuning/design decisions. The
general sorting and source plans remain proposed. G1's separately qualified
field consumer below does not make those content streams implemented.

## Minimum mechanical contract to critique

Use a few readable size bands rather than simulating particles. Authored or
properly acquired instance stock states actual constituent identities, finite
quantities and size ranges. The same mesh comparison handles every admitted
stock; adding an appropriate material should not require a new tool-specific
blueprint pairing. A lot containing overlapping sizes produces a mixed grade,
not falsely pure ingredients. Do not invent quantities from a material tag.

The first prototype can refuse lots with overlapping/uncertain grades rather
than implement nested mixed fractions. Work on complete batches and integral
commodity units; incomplete units remain accounted stock, never rounded into
whole outputs. Empty/all-passing/all-retained passes create no falsely purified
new products. Repeated passes, stack splitting, interruption and reload cannot
reset the original finite constituents. The current quarry packet also has
declared rock-dust loss but no size manifest: do not silently reinterpret its
legacy preparation as a proven general mesh partition.

Keep this within existing Parts and supervised action transactions. Qualify
actual source ownership, mesh/tool identity, both output capacities, partial
stock, callback interruption, exact custody and save/reload. Preserve private
state rather than erase contamination, origin, treatment or identification.
Prefer existing quantity/claim/output-admission contracts to a generic job,
particle, terrain-excavation or composition simulation framework.

Manual selection is an alternative where individual pieces can reasonably be
picked out; NPC preparation and finished stock remain alternatives elsewhere.
A mixed sack whose contents can already be individually taken in one action
does not by itself earn a new sorting tool. Before coding, demonstrate what
the sieve saves or enables beyond the existing container menu.

## Acceptance and scope

### G2 concrete contract: shared size partition and one finite source

Working runtime checkpoint: f156/156native,0CS after the two Handling-weight
and optional source-ID review counters plus a real same-ID rollback RED.
Mass uses the actual positive Handling override or Physics shell rule. Heap
identity must be nonempty, unique before/after placement and unchanged. A late
remaining-source alias refuses; scoped limited rollback retracts products and
preserves the independent alias without inserting a duplicate saved identity.
Other receipt callers retain their original restoration contract. Actual guided
seed65 craft20/harvest200/screen210/repair240/free well drink240/grit250 and
selected F5/fresh210/250 are complete, retaining the same sieve and ordinary
bank4. Source and independent recorded-play/artifact review0remainingP1/P2.
Seed64 failed before acquisition; construction materials/forge and initial
address are supplied/guided, no mixed stock/products. This qualifies bounded
size screening, not arbitrary mixtures, material-grade substitution, ordinary
interregional discovery, slippery-ground comparison or whole machines.
See [G2 evidence](Verification/MachineSystems/GeneralScreening20261124/README.md).

Implemented bounded contract, November24local/25UTC. One general screening
capability, not a separate recipe for every material. An explicitly authored
SiftableStock Part declares a small finite per-packet constituent manifest:
commodity blueprint, integral unit count and size band. Initial grammar is
semicolon-separated blueprint:count:band entries, bounded to four entries and
64total units; invalid/empty/duplicate-ambiguous/incomplete input refuses.
Every output is an audited plain canonical commodity. The source, unit counts,
declared dry treatment and all creation/payload permissions remain exact.

The existing hand sieve has one fixed physical aperture between bands1and2:
bands0/1 pass, band2 stays. Keep ScreeningTool's saved shape unchanged; adjustable
meshes or new tool grades are outside this first increment. Both sides must be
nonempty and each must resolve to exactly one commodity identity. Equal-sized
unlike constituents are refused rather than silently separated; no nested mixed
fraction framework. The same commodity on both sides is also refused because
the current plain output would erase its grade distinction. Repeat identical
entries on one side may combine only as the same integral commodity. No input
may name itself as an output or exceed its declared mass/finite stock.

DryClaySpoil is the initial three-weight packet: FireClay:2:0;CoarseGrit:1:2,
base value at least the declared four-value outputs. Screening consumes one
actual packet and retains the same sound carried sieve. Both actual fractions
enter the ordinary pack through existing output-admission/stack/capacity rules.
The existing quarry preparation and its declared dust loss remain separate.
There is no new material identity inferred from soil, a container description
or a material tag. A second differently named canonical native-test stock and
different output identities must use the same operation without a registry row.

Reuse MaterialProcessService's payment/output/creation/interruption work rather
than duplicate an output transaction. MaterialProcessPlan may accept only this
explicit closed screening provider as an alternative definition source. Capture
its exact Part, manifest and derived immutable definition; currentness retains
the same source permission, tool, factory and canonical templates. Registered
definitions retain their existing registry-identity checks. Transient derived
definitions are never inserted into the global registry or saved as jobs.
MaterialProcessActions can share its existing weak owner-quote cache and command
dispatch, retaining a current derived quote across repeated menu reads without
accepting changed manifest/count/tool/ownership. Explicit MaterialProcessInput
denial still wins. SiftableStock owns the inventory receipt so late cancellation
does not rewind unrelated actor state. Exact policy admission and processing
stack/split tracking include the new data Part without granting unknown payloads.

Ordinary supply is one separately authored DryClaySpoilHeap beside a fresh
western Morrowfast allotment's actual clay working. Reuse an exact once-only
Harvestable yielding one packet; the existing bank still yields its four clay.
Do not append a mandatory entry to the site's atomic Specs list: inability to
place the optional heap must never remove existing wells/crops/materials. Add a
bounded optional placement after the original installation succeeds, choosing
real bare reachable ground beside the unchanged bank without clearing terrain.
Missing content or no suitable place leaves the original site intact. Existing
Role/anchor retention prevents load/re-entry/reload reseeding; an old site never
gains a heap. Harvest removes the heap through existing source custody rules.

Actual play needs an honest route to this source. The ordinary default start is
11.10, so do not assume it starts in Morrowfast. A declared editor scenario may
request an explicit fresh starting zone through the existing fresh-start policy,
like the existing declared wild-start control. If this minimal debug setting is
added, an explicit scene destination wins, normal games and continued saves stay
unchanged, and the one-shot request is cleared after scenario application. This
is a guided starting fixture, not ordinary random discovery or runtime teleport.
Prefer fresh2.6 with normal18Strength/40HP/50drams and explicitly supplied sieve
construction materials/forge only; source packet and finished sieve are not
granted. Obtain the real heap, forge the sieve, screen away from the forge, use
the clay for an existing lasting repair and demonstrate the retained grit or a
consequence of the repaired installation. Ordinary four-clay harvest, purchased
stock and existing services remain alternatives. This source is not a container
of individually takeable finished fractions: the mesh separates the actual lot.

Required native RED/countercases: data-driven second stock, both groups and
quantities, overlap/all-pass/all-retained/refused self-output, malformed/bounded
manifest, mass, private/active state, stack/split/repeated passes, lost tool,
retained quote and changed manifest, exact callback custody/capacity/output undo,
independent injury, source once-only/old-site/no-place retention, fixed legacy
quarry behavior and normal/scene/wild/continued start controls. Then actual
source/reader/menu/cancel/work/repair/F5all-four/fresh and independent reviews.
No foundation or whole sieve acceptance is earned by this contract alone.

### G1 concrete first increment: useful coarse grit

G1 implements the coarse consumer independently of the later general sieve
partition. CoarseGrit is a plain one-weight,
base-value2 stackable commodity. An explicit GritMaterial capability lets any
canonically declared suitable commodity scatter one unit into the existing
12-turn grit residue. Existing shared slip rules then steady oil/ice for all
ordinary movers; other liquid, heat and contact hazards stay active. Fine clay
and unqualified dust do not gain this capability. No physical sieve operation,
extra clay yield, grade manifest or complete drainage project is claimed byG1.

The new capability owns its command receipt, uses actual carried stock and
claims, audited payload admission and bounded one-unit payment. Repeated quotes,
callback interruption, independent injury/custody/quantities, exact staged-layer
undo and save must retain their existing consequences. Legacy SilverSand keeps
its current actions and tariff. This need not replace its action producer.

Initial ordinary supply uses TraderPart's existing one-time
OpeningStockBlueprint/OpeningStockUnits allocation for two CoarseGrit on the
fresh Morrowfast mender. MorrowfastContent.CreateResident builds this Trader
from Creature at resident index4 and now sets the two opening parameters there.
MorrowfastMenderStock and its existing four FireClay are unchanged. Native
controls qualify two fresh units, no old-save allocation or restock refill,
factory-less ordinary fallback and a differently named canonical commodity.
Actual ordinary purchase/discovery remains open: the first native trial's
ordinary starting region contained no Morrowfast mender.

Independent review corrected the earlier proposal to append grit to the stock
table: TraderRestockSystem re-rolls low shelves, and MerchantStockService has no
replacement profile for this mender. Such a table entry would be renewable,
not finite opening stock. TraderPart.ApplyOpeningStock is the separate finite
allocation seam, used by this implementation.

The bounded G1 implementation critique adds two receipt prerequisites. Freeze
the finite opening blueprint, count, factory and current trader/pack before
creation; callbacks cannot enlarge or redirect that allocation. A deliberately
hostile declared creation adapter supplies the adversarial counterfixture, not
an assertion that ordinary grit creation has side effects. Retain the once-only
fence when an allocation refuses. For grit placement, capture the exact written
residue layer before the synchronous OnCellChanged notification. Looking it up
after notification can adopt an independent replacement for rollback. Preserve
replaced layers, stronger leases and the existing max-duration write rule.
These checks are implementation gates, not additional player capabilities.

Native e139/139 covers34new cases and105retained,0CS; post-label-polish f32/32
overlaps e. A real two-to-five opening-allocation RED and late same-ID stack
alias RED were fixed; exact before-notification layer receipts preserve
independent replacements. Independent source/recorded-play review:0remainingP1/P2. Actual
supplied ground pickup20, free readers/cancel20, scatter30, step40/HP40 and
last-unit refresh50 retain oil; F5/all12files across three archives and two
selected fresh pairs are preserved. See [bounded evidence](Verification/MachineSystems/GritMaterial20261124/README.md).
Only this field consumer is qualified; ordinary stock acquisition, the finite
clay source, general sieve partition and repair combination remain open. The
current delivered Mac995d excludes G1; build at the broader playable milestone.

Independent bounded content/source critique by capability_audit is complete.
It supports optional dry clay-working spoil plus a new useful coarse-grit
consumer as the smallest coherent candidate. It does not accept a new source,
generic sieve operation or implementation. A further bounded review of this
content pass identified the stock-table renewal P2 above; the proposal now uses
the existing one-time allocation after checking the current mender construction.
The narrow independent recheck found no remaining P1/P2 in this content design;
it accepts no runtime operation or source. Three design risks become gates:

- P1 transmutation: size must preserve actual identity, dryness, purity,
  treatment and viability; screening is not threshing, washing or assay.
- P1 multiplication: integral quantities, partial stocks, repeated passes and
  fuel-derived residues cannot duplicate commodity units or usable energy.
- P2 compulsory chores/useless outputs: preserve current harvests and services;
  qualify a purpose for each useful fraction before adding byproducts.

The G1 field consumer does not accept a new mixed-source yield, general sorting
or any complete-machine gate. The next implementation needs finite inputs, meaningful destinations
for both useful fractions and a reason to use size sorting. It then requires
native RED/countercases, actual ordinary menu/movement/use/F5/fresh evidence and
independent source/recorded-play review. A broad framework or a list of new
recipe IDs alone does not satisfy the user's general-tool direction.

## Leaving a coarse fraction at the worksite

Roadmap 14 trial. The existing one-action hand screening puts both products
in the pack. A second quoted action can screen the same known clay spoil
and leave its one-pound coarse-grit fraction as a normal pickup at the
player’s feet. Two one-pound clay measures go in the pack. The sieve stays
with the player. The coarse output can be collected or used later through
the existing actions. The original clay bank remains an ordinary alternative.

The work uses the shared material transaction and live product templates.
The optional destination is the exact visible clear floor occupied by the
player when quoting. A live pool, active coating or new physical blockage
invalidates that offer. The other screening action retains its normal
conditions. The extra choice removes a separate inventory step and leaves
any already carried grit alone. Both screening choices cost one action,
and ordinary dropping is free in game time.

Native countercases cover the stale destination, capacity, a late veto,
ordinary pickup and independent death spills. In Editor play the player
collected the existing heap, left the coarse fraction, reloaded it, and
used the clay at the well before returning for the grit.
[Evidence](Verification/MachineSystems/GroundScreening20261125/README.md)
