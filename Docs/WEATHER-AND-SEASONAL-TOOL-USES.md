# Weather opportunities and seasonal tool uses

Living design, November23,2026 (America/Chicago). Bounded W1a collection and
manual watering are Editor accepted; automatic crop rain, distinct seasonal
reuse and wholeW1 remain open. See the [W1a receipt](Verification/MachineSystems/WeatherCollection20261123/README.md). The governing user
direction is that weather makes selected activities more rewarding; skipping an
opportunity usually means missing a bonus while ordinary exploration remains
comfortable. Some familiar tools should acquire different practical uses as
seasons change. These uses must follow observable conditions and capabilities.

## The player experience

A traveller notices rain collecting along a cloth edge, ripe seed heads moving
in a breeze, or a shallow bank softened by showers. Their flask, knife or work
frame suggests something useful. They can take the opportunity, pursue another
approach, or continue their journey. Collected supplies and completed work remain
valuable after the weather changes.

Weather supplies opportunities; it does not assign a maintenance checklist.
Do not reduce baseline harvests, ordinary crafting, travel comfort or existing
service availability to make a weather bonus attractive. No new routine exposure
damage, thirst, equipment decay, food spoilage or repeated shelter obligation.
Existing authored hazards remain separate and do not expand through this design.
Ordinary purchases, gathering, skills and NPC services remain practical.

A season changes the available work and the landscape's material state. Weather
creates shorter favorable periods within it. A knife used on flexible stems in
one period may separate ripe seed heads in another; both uses follow the knife
and the material, rather than a permission granted by the calendar alone.

### A shorter early-season resin trip near Posy

The ordinary seed 64 trip took nineteen steps from surface arrival to a
ripe pitchpod. The player gathered two resin and one seed, earned regard
through Posy’s existing held-composition approach and lacquered the cloak
they were wearing. One resin raised its armor value from one to two.
The other resin, the seed and the treatment survived a fresh Continue.

Use the existing early growing period to offer one wild pitchpod bead on
the near shoulder of an admitted pitchpod patch west of Posy. One hand
harvest gives one measure of ordinary pitchpod resin. The regular two plants
keep their growth and yield rules. A shorter walk makes the bead useful to a
traveler who needs a single measure. A full harvest still gives planting
stock and more resin. The bead is one saved source and remains available
in a later early period if nobody has gathered it.

Place the source only when both paths are real on the finished ground and
the bead saves at least three dry walking steps from ordinary arrival.
Use the existing seasonal harvest action and game clock. The ready and
dormant readers describe the visible state of this plant. The patch’s
existing retained record prevents a spent site from refilling on travel
or reload. Keep v27 and cached sites on their original layout.

The wild plant shows a bright bead in the existing early window and
its dry stalk outside it. Both appearances precede Examine and share
the hand harvest’s exact source and season. The two ordinary plants
use their existing map art and growth.
[Same-position map playtest](Verification/MachineSystems/PosyPitchpodWorldCue20261127/README.md)

## Current foundations and required additions

Source qualification refers to committed runtime7edc5a4c2. Source support alone
is not an actual-play claim for a new combination.

| Existing foundation | Boundary for this design |
| --- | --- |
| WorldClock derives four 300-tick bands from TurnManager.TickCount; one day is 1200 ticks. | Derive weather and seasons from this saved game clock. There is no wall-clock progression. |
| CropTime reconciles bounded elapsed growth/moisture; carried water can add 40 moisture; Conjure Rain is a spell. | Natural rain needs its own bounded watering adapter. Do not invoke the whole spell or create a second growth timer. |
| LiquidFlask carries up to 12 units of one liquid through existing custody/transfer rules. | A flask does not create rainfall, capture from an arbitrary surface or purify runoff. A collector must declare a finite clean source. |
| CampAwning declares Shade and PackableFixture. | Rain collection, a drying frame and a windscreen are additional capabilities, not existing awning functions. |
| Dagger declares CuttingTool.FlexibleStock. Hatchet has combat cutting attributes but no CuttingTool Part. | Flexible-stock cutting does not imply wood splitting, seed separation or permission to cut living plants. Define those work contracts explicitly. |
| Harvestable sources and crop yields have finite identities and publication rules. | A favorable period must not reset Harvested, duplicate a seed, or recreate a depleted deposit when revisiting. |
| NaturalHeat supports low-heat mire settling, not clay baking. | Sunshine is not kiln heat, free forge work or an implicit energy source for every process. |
| Fish traps use place-bound fish stock and held capacity. | A future fishing bonus may change access or work within that stock; it must not manufacture catches. |
| Merchant consignments retain exact lots and game-time refresh boundaries. | Seasonal eligibility applies to a later shipment. It cannot reroll current shelves or remove dependable essentials. |

Relevant source: [WorldClock](../Assets/Scripts/Gameplay/Turns/WorldClock.cs),
[CropTime](../Assets/Scripts/Gameplay/Farming/CropTime.cs),
[CropWateringService](../Assets/Scripts/Gameplay/Farming/CropWateringService.cs),
[ShadePart](../Assets/Scripts/Gameplay/Items/ShadePart.cs),
[CuttingToolPart](../Assets/Scripts/Gameplay/Preparation/CuttingToolPart.cs),
[NaturalHeat](../Assets/Scripts/Gameplay/Alchemy/NaturalHeat.cs),
[FishTrapPart](../Assets/Scripts/Gameplay/Items/FishTrapPart.cs),
[MerchantStockService](../Assets/Scripts/Gameplay/Economy/MerchantStockService.cs).

## Concrete weather opportunities

The first and third rows are now bounded Editor behavior at the fresh
western Morrowfast allotment. The remaining rows describe extensions unless
called out below. The reward is useful because it feeds
existing or explicitly planned consumers; quantity, effort and opportunity are
different benefits and should not become universal item-stat multipliers.

New North Works shelves also provide one paid fair-weather action that
removes up to 25 moisture points from a carried item. Actual play used
it to restore ordinary flint ignition. The shelf retains normal storage
and uses the existing weather clock. The fibre-batch opportunity below
remains proposed. [Native shelf evidence](Verification/MachineSystems/NorthFairWeatherDrying20261126/README.md)

The public seed frames signal when the clean mesh and dry ground
can retain chaff. One ordinary wait out of a shower changed both
visible frames in the live allotment, and the new quote retained
chaff from the single bundle alongside the plantable seed. Rain
still shows the shared water status and plain winnowing still yields
one seed. [Cue and winnowing play](Verification/MachineSystems/SeedFrameCue20261127/README.md)

| Opportunity and discovery cue | Choice and benefit | Accounting and ordinary alternative |
| --- | --- | --- |
| A mild shower passes an installed, clean catch-cloth with a collecting edge. Its reader offers collection and names a receiver. | Gather a few units of clean water for crop tending or other supported water uses while travelling. | Shared finite local rain allowance, exact compatible receiver and paid action. Carry water, use a well/source or buy supplies instead. Shade alone grants no collection. |
| Showers reach an exposed planted bed; the crop's reader identifies the rainfall contribution. | Save some tending water while doing another activity nearby. | A bounded moisture top-up, no extra produce or seed grant. Existing vessel watering and Conjure Rain remain available. A roofed bed does not receive open-sky rain. |
| During rain and shortly afterward, a visible slump leaves loose sediment below an eligible clay bank. | Collect a finite mixed deposit, dry it, then screen it for fire clay and coarse grit. | At the western allotment the shower exposes one authored reserve. The ordinary bank and dry sorting heap are separate. [Native playtest](Verification/MachineSystems/RainClaySeam20261126/README.md) |
| Clear dry air reaches a loaded drying rack. Its preview shows reduced work for the selected batch. | Prepare carried flexible fibres or selected harvested material with fewer work steps. | Same finite input and output; a proposed 3-step baseline may become 2 steps, never zero. A rack/process contract must exist first. Hand preparation or a town service remains. No damp-weather penalty is added to the baseline. |
| A steady breeze moves chaff in a prepared grain or seed bundle. | A mesh screen or suitable tray can winnow it, making planting or cooking supplies from an alternative material fraction. | Consume the exact mixed bundle once; allocate its declared grain/seed/chaff budget. Do not grant an extra full harvest on top. Hand separation or milling is the alternative. |
| A wet-growth period leaves optional fresh flexible stems on an eligible plant. | A cutting tool can take a fibre preparation instead of that source's alternative harvest, supplying cord or binding work. | Plant-specific finite material manifest and gathering rights. No universal tree cutting and no duplicated seed/fibre allocation. Ordinary cord and workshop supplies remain. |
| Seed heads ripen during a gathering period and are visibly distinguishable. | The same suitable knife now separates a seed preparation from a carried harvested head for future planting or exchange. | Saved head identity and finite seed allocation. Existing seeds remain obtainable at other times; this is not a seasonal gate on farming. |
| Dry fallen branch stock becomes available at a permitted gathering place. | An explicitly suitable splitting tool prepares a compact kindling bundle for selected hearth work or a portable frame. | Finite fallen stock; never cut a protected living tree automatically. Fuel value and any weight reduction need an authored recipe with stated loss. Ordinary timber and purchased fuel remain. |
| During a cool clear period, a supported condenser has favorable ambient conditions. | A selected future still recipe can recover an additional declared fraction normally lost to the process, or reduce its paid work. | Pick one benefit per recipe and account for the retained fraction. Do not change existing4:1 mire settling or oil quality silently. No sunshine-to-clay conversion; new process qualification is required. |
| A local harvest period supports a particular prepared food or material. The cook or merchant explains its origin. | An optional recipe or later special shipment gives another way to prepare for an expedition. | Real ingredients for local preparation; existing abstract consignments retain their separate supply authority. No automatic NPC production or exclusive seasonal progression item. |

The original shower work covers catchment and the proposed crop moisture
benefit. Other opportunities qualify as separate material or work loops.
Each needs a complete process contract before it changes the world.

### Weather exposes a workable seam

Rain can give a player a reason to change their plans. At an eligible clay
working, the bank slumps and leaves a visible apron of loose sediment.
It stays workable during the shower and briefly afterward. Examine can
say “freshly loosened,” then “starting to set” as the window closes.
Once collected, the same packet can travel and be screened later in a
better place. A player who left their sieve at camp still has a choice.

Begin with one separately budgeted deposit at a source whose geology
supports the existing fire-clay and coarse-grit fractions. Screen it
through the same admitted mesh rules. Use the clay for a physical repair
and compare the grit on a slick step. The ordinary bank still gives its
existing yield, and the current dry spoil remains its own stock. Rain
changes access to the seam. A wet packet needs either a qualified wet
screening method or existing supported drying before dry screening.
[Current screening rules](MACHINE-GENERAL-SIFTING-CONTENT.md)

Some materials can have genuinely weather-exclusive wild collection
windows. Give those materials optional, distinctive uses that a player
might deliberately wait for. Ordinary repairs and exploration keep
their existing supplies. A low-water spell could expose a gravel bar;
frost might work a thin plate loose along an existing split. Each cue and
yield needs a physical source and a specific downstream use.

Use the saved local game clock for the shower and its short grace period.
An uninterrupted shower has one end. Once the seam is gathered, later
showers, crossing the zone boundary and reloading keep it spent. Queries
read the existing source and weather without creating stock. Verify the
same bank in dry weather, while raining, shortly afterward and after the
grace period. Then verify a late collection, processing after the window,
both useful fractions and the option to walk past comfortably. One actual
western allotment is qualified below. Other placements and wet screening
remain proposals.

Source check, November 26. The existing weather observation ends at the
next 600-tick interval. Consecutive wet intervals must count as one
shower. A short grace can be evaluated in the first fair interval by
checking the preceding interval and the elapsed game ticks. It starts at
the actual wet-to-fair boundary even if the player arrives late. Reuse
the fixed, one-use harvest ownership and transaction checks for the seam.
The existing screened spoil is explicitly dry. Directly yielding that
packet from newly wetted ground would remove a material condition. The
first complete seam trial needs an explicitly qualified wet screening
or drying step and a visible source cue.

The implemented trial uses the outdoor clay working at the western Morrowfast
allotment, when that field generates with room for an extra deposit. The
shower loosens one separate three-pound packet. After the rain it stays
workable for 150 world ticks. In fair weather before the next shower the
uncollected bank settles again. Taking the packet leaves a visible scraped
seam. Both the ordinary bank and the dry sorting heap keep their own stock.

The packet carries ordinary moisture when gathered. Spread it out to dry,
or use an existing drying service, then screen two measures of fire clay
and one of coarse grit. The sieve keeps the solids and refuses wet stock
through its existing rules. Moisture uses the same scale as wet gear and
does not supply a collectible liquid. The bank's appearance follows the
same local conditions as gathering. The source keeps its spent state through
travel and reload. Its geology participates in the world's local weather as
soon as the fresh field is visited. Already cached fields receive no added
deposit. [Native playtest and limits](Verification/MachineSystems/RainClaySeam20261126/README.md)

## Different seasonal uses for familiar tools

The four columns are planning periods, not approved canonical season names or
a requirement that every region has the same temperate climate. All new uses
in this table require implementation and qualification.

| Tool or reusable apparatus | Growth period | Bright or dry period | Gathering period | Cool or quiet period |
| --- | --- | --- | --- | --- |
| Suitable cutting knife | Strip an eligible harvested flexible stem for fibre. | Cut prepared cloth into collector ties or frame lashings. | Separate ripe seed heads from a carried harvest. | Dress an explicitly recoverable leather offcut for binding work. |
| Suitable hatchet or splitting tool | Prepare permitted fallen branch stock into short stakes for a plant support or collector. | Split the appropriate dry stock into kindling. | Size fallen poles for a storage/drying frame. | Recover usable timber members from an opted-in damaged wooden object. |
| Awning frame and compatible cloth | Rig a collecting edge above an exact water receiver. | Use its existing shade, or fit a separately supported drying surface. | Fit mesh to the frame for a winnowing station in a suitable breeze. | Rig an explicitly supported windscreen beside a chosen work/rest spot. |
| Flask or other compatible vessel | Receive collected rainwater or an existing plant preparation. | Measure water/reagents into a supported coating or preparation. | Receive oil from a separately supported finite pressing/clarification process. | Hold a selected cold-soak batch or condenser output where that recipe supports it. |
| Mesh screen or tray, proposed portable tool | Wash an eligible sediment batch with finite water to separate declared coarse and fine portions. | Sift an already-dry prepared material. | Winnow a finite grain/seed bundle in a breeze. | Sort a dismantled object's explicitly recoverable small fragments. |

These are distinct work choices, not seasonal restrictions on the tools. A
suitable knife can still cut an available compatible stem outside the growth
period; seasons alter which material opportunities are locally present. None
of these rows lets a combat tag stand in for a work capability. The salt rake's
preservation description does not make it a general-purpose sieve or harvester.

The portable mesh screen is a proposed new tool. The awning's cloth frame does
not become a fish trap or a universal machine through imagination alone: changed
configurations require their actual cloth/mesh/fittings and keep finite contents.
No capstan or culture-vat implementation is brought forward by this table.

## Regional character and lasting projects

Player direction, November 26. A short walk from home should turn up many
useful materials through distinct actions and conditions. Start with a
few recognizable nearby places such as a bank, a path, a hedge and a
working yard. Each can have ordinary stock, an occasional environmental
opportunity or an output from someone's actual work. A familiar walk
changes when water falls, a plant ripens, a worker finishes a batch or
the player removes a real obstruction. Home placement makes those nearby
sources convenient. It does not generate a new material reserve.

Different home regions should offer different mixes. Give each area
practical ways to get everyday repair stock, binding material, food or
fuel. Where an ingredient is unusual, give it a distinctive use and a
visible collection cue. Claimed items keep their usefulness after the
condition passes. Ground stocks only return through an explicit source,
such as actual regrowth, a new paid batch or a separate deposited lot.

Use the current generated biomes for implementation authority and the actual
places/cultures for descriptions. The lore's place-network is not a mandate to
replace the shipped map. Proposed regional emphasis:

| Region or community | Useful emphasis | Practical recurring connection |
| --- | --- | --- |
| Spread farming and river communities such as Sill | Mild showers, clear intervals, visible seed and grain work. | Collected/tended water -> crops -> food or seed; a shared collecting edge or winnowing frame gives a town project a continuing purpose. |
| Sodden peat communities and Peat Works | Rain, mist and especially valuable dry breaks. | Rain supplies optional water; a dry break makes a qualified drying process attractive. Existing frog-oil clarification and mire preparation remain useful throughout. No season drains the bog or exposes preserved witnesses. |
| Grovelands and Bower-associated places | Sheltered growth and authorized gathering. | Seasonal fibre/seed choices support preparation and binding; consent and held-place rules still govern harvesting and installation. A season never grants permission to take living material. |
| Beating settlements and exposed routes | Dry air, steady breezes and optional mild shower opportunities where locally authored. | Shade retains its existing function; supported screens, catchment and finite material work offer additional reasons to stop. Do not increase glare, dehydration or NPC exposure to fund these benefits. |
| Quillhold and other inhabited workshops | Forecasts framed as practical local advice and later appropriate consignments. | A project enables another work choice, storage or future special selection. The existing copy-room project is not rewritten as a seasonal maintenance obligation. |
| Underground places | Their own light, heat, water and cultural cycles. | No direct surface rain credit below ground. A physical seep, entrance draft or collector connection needs an authored source/destination adapter; weather does not identify hidden chambers or explain mysteries. |

Examples of future lasting projects are a communal catchment, a shared frame
with interchangeable cloth/mesh fittings and a workshop's improved drying space.
Their utility persists. Weather can make one configuration particularly useful;
the project never breaks merely because the player did not visit this season.
Players can use a town installation, carry a suitable kit, use a service or skip
the activity. Multiple player bases remain a later construction/storage scope.

Lore authority is [Lore/README](../Lore/README.md), the Bible as amended by
Second Spine, and the Mystery Ledger; History/02_Geography and
History/08_MaterialCulture supply compatible place/material detail. Meteorology
does not turn Urqu's pressure metaphor into agency, a forecastable anomaly
detector or an answer to a protected question. No universal Glow-Quartz energy.

## Cadence and discovery

Prototype tuning proposal: one season phase lasts 8game days, four phases form
a 32-day cycle. Weather is sampled in 600-tick regional windows; repeated windows
may continue the same condition. These numbers are test parameters, not canon
or accepted pacing. Regions weight conditions differently; the Sodden need not
look like a snowy temperate winter. Use stable region identity rather than
rerolling weather independently on every zone entry.

Show current local conditions, the practical opportunity and a coarse near-term
outlook through existing HUD/Examine surfaces. A collector might say:
"Shower. This catchment can supply 3 water this interval. Choose a flask."
A supported screen might say: "Steady breeze: separate this seed bundle here."
No red emergency countdown, repeated popup or compulsory tutorial interruption.

An installed apparatus can explain its normal use and a different seasonal use
without showing an overwhelming catalogue. Local workers can mention one nearby
opportunity; merchant goods can suggest a preparation or project. Forecasts
describe ordinary conditions only, not unseen actors, contents or mysteries.

### Show the change in the world

Loose materials must also show the end of an existing material condition.
Stationary drying now refreshes the visible item when its wet effect ends.
The same packet keeps its identity and normal dry color through Continue.
[Native wet-to-dry comparison](Verification/MachineSystems/WetGroundAppearance20261127/README.md)

Every environmental material opportunity needs a visible cue in normal
exploration at the usual viewing scale. The cue comes from what changed
in that place. A bank gains a broken edge and a loose apron of sediment.
A falling river leaves a wet line and exposes stones with distinct shapes.
A windblown pile gathers in a visible streak against a wall. A cracked
frost plate lifts at one edge. These shapes, textures and motions can
carry the meaning alongside color.

Where a collection window has a closing phase, show that too. At the clay
seam the fresh apron looks loose, the closing apron starts to crust over,
and a gathered seam shows bare ground. Each look follows the same local
state that controls collection. Appearance follows actual visibility and
the current saved material state. Examine explains what the player saw,
the likely yield and any tool or timing requirements.

The existing western Morrowfast knotflax stand also shows its condition
on the map. Green stems cue the early cut for cord, a bright branch
marks the ripe seed heads, and muted stalks mark dormant growth. The
player can see these before selecting tools. Harvest clears the stand as
before. [Knotflax map playtest](Verification/MachineSystems/KnotflaxWorldCue20261126/README.md)

At North Works the shallow seasonal lichen shows a low crust as
the dormant shape, with full frosted growth during the existing harvest
phase. The deeper ordinary patch supplies lichen all year and keeps its
own appearance. Both appearances follow the existing bound source and
visibility rules. The player still gathers one pinch from the shallow
source and uses the same lichen in the field or at a still.
[North Works playtest](Verification/MachineSystems/NorthLichenWorldCue20261127/README.md)

Design opportunities to last long enough for noticing and trying. Where a bonus
exposes a material, the claimed physical material persists after the weather.
Where it advances work, bank only validated progress on that exact paid batch.
Changing weather does not destroy the baseline batch, confiscate output or
expire a project. Prepared supplies have no newly introduced spoilage timer.

## Shared rules and bounded state

The minimal architecture is a pure regional weather context over the existing
world seed, saved clock and an explicit policy version. A process or source
adapter asks for the local context plus actual exposure, material state,
capabilities and access. The weather service does not know every tool name or
own every recipe. Parts retain physical quantities, configuration and ownership.

Capture the authority used for a preview, then revalidate before commitment.
Do not consume a different flask, tool or ingredient if the selected one moves.
Weather changing after a completed action affects later actions, not the output
already earned. Cancellation, refusal and pure readers spend no action or stock.
Future cost benefits contribute before preview/payment; completion effects occur
after the existing transaction commits. Never apply a global second multiplier.
For an initial process with several favorable modifiers, use the strongest
qualified reduction on each cost axis rather than multiplying reductions. A
weather benefit cannot reduce a paid operation to zero work or negative fuel.

Any catchment requires a shared finite local allowance. Multiple collectors,
stack splitting, packing/reinstalling, travelling and saving/loading must not
recreate it. Water volume remains ordinary water with ordinary compatibility;
do not hide a different value or magical quality behind the same inventory row.
Verify commerce implications instead of assuming renewable water cannot profit.

Loaded and unloaded reconciliation must agree at the same game tick. No scan of
all world zones each turn and no iteration over every elapsed tick. Query only
registered local consumers or reconcile a visited owner's bounded intervals.
Persist versioned progress/allowance only where mutation needs it; pure weather
queries create no zones, sites or game state. New save fields require explicit
legacy defaults and saved-Part contract coverage. Old saves receive no
retroactive harvest, water, work or changed historical weather reward.
Save the weather-policy version and activation boundary explicitly when enabling
the system on an existing world; a later rules update cannot quietly regenerate
earlier collection windows. Use checked wide arithmetic for window boundaries
and refuse invalid time/state before committing resources.

Initial showers do not call Conjure Rain wholesale, apply WetEffect to actors,
alter fires or enroll creatures in new exposure. An existing crop/collector can
receive a bounded weather benefit without asserting a complete rain simulation.
Any later consequential exposure expansion needs explicit animal tolerances,
NPC survival behavior and player/NPC-consistent shelter rules first.

## First playable increment

W1 is now split. W1a manual finite collection is accepted in the Editor, with
actual collected-water crop use and travel/fresh-save persistence. W1b automatic
crop rainfall remains unimplemented; the wholeW1 gates below still apply.
W1a fixes one public plot to an exact notice/ground/world binding and two fixed
frames. It adds no portable collector entitlement or cached retrofit. See the
[implementation contract](WEATHER-SHOWER-COLLECTION.md) and linked receipt.

November 24 source qualification: general W1b is not a small adapter over the
current crop clock. `Cell.IsInterior` can change directly; crop/cover movement
does not supply a settled historical exposure boundary. Current fixed-collector
validation and Shade cannot establish that history. A strictly enforced immutable
outdoor plot could be a narrower pilot; it would not qualify general rainfall.
Before general W1b RED, specify exposure changes and chronological aggregation,
including partial first/last windows, initial moisture above 40, consecutive
showers, wet remainder and once-per-window handling when no top-up is needed.
Do not call Water recursively inside CropTime reconciliation. Independent
bounded source critique recommends another medium-depth material/tool connection
first; existing W1a/manual watering remains useful. No new behavior is shipped by
this qualification. See the [current continuation](WEATHER-DESIGN-CONTINUATION-2026-11-23.md).

W1 is optional shower water with two practical consumers. It adds a regional
weather context, a qualified catch-cloth on an installed awning or communal
frame, and natural watering of eligible exposed planted crops. Existing
ShadePart stays separate. No drying jobs, seasonal harvest spawns, new exposure,
merchant repricing or generalized physical rainfall simulation in W1.

Prototype collection contract: a visible local collection plot holds a shared
allowance of 3 water per 600-tick rain window. This is a small authored catchment
abstraction. All collectors drawing from that same plot share its saved use;
additional nonoverlapping plots represent additional collecting area. A clean,
properly installed collecting edge enables one ordinary paid draw action into
an exact compatible carried receiver, up to its free capacity and the remaining
allowance. Uncollected water is not stored or retroactively awarded. In W1 this
is an active collection opportunity, with no passive collector economy.

Before W1 RED, fix the collection plot's saved identity, footprint and overlap
authority. Moving or reinstalling a collector cannot create a new plot or
allowance. Public/community collection and private collection must obey their
actual permissions. A clean collecting cloth is a qualified configuration, not
a new dirt meter that requires periodic maintenance. Collector coverage also
affects rain reach: a covered bed cannot simultaneously receive direct open-sky
rain through that collecting cloth. Shade alone does not decide rain coverage.

Natural crop watering contributes at most one top-up to 40 moisture per eligible
crop per rain window, with no addition above 40 and no direct yield publication.
It is an alternative source of the existing moisture resource. Credit applies
only while that exact physical crop is present and exposed during the window;
never credit rain before planting or while its bed is roofed. Split the crop's
elapsed interval at the bounded credit point so old dry time does not grow and
rain arriving at the end does not water the past. The exact credit schedule and
save adapter must be finalized in W1 source/design critique before native RED.
Forty moisture represents up to 400 world ticks through CropTime's ten-tick
units. Settle earlier growth before a rain credit or exposure change and retain
the existing wet-tick remainder. Finalize bounded interval integration before
implementation; neither per-history-window replay nor lost unloaded benefits
qualifies. A roof/exposure change is an actual event, not something inferred
retroactively from the current appearance of a bed.

W1 acceptance requires normal wandering to reveal a visible catchment, an
actual paid collection, a useful finite-water action elsewhere, and a separate
observed crop benefiting from rain. Demonstrate a dry-weather/manual alternative,
two collectors sharing allowance, changed receiver/callback rejection, partial
capacity, save/reload of spent allowance and identical loaded/unloaded crop
results. Compare a baseline walk that ignores the weather: no added health,
comfort, travel or equipment cost. Tests alone cannot accept this increment.

W2 gives one apparatus a second actual use: fit a qualified mesh or drying
surface to the same reusable frame for a finite seed/fibre batch. Choose one
process after observing its ordinary alternative and actual consumer. This is
the first acceptance gate for the user's distinct seasonal-tool-use request;
W1 alone does not satisfy it. W3 can introduce one regional material choice or
lasting workshop project after W1/W2 play establishes useful frequency.
For W2, demonstrate two materially different jobs on the same physical frame
under distinct seasonal/material conditions. A second unchanged year-round
menu verb alone does not prove seasonal reuse. Materials remain usable after
their gathering season; calendar changes do not revoke tool permissions.

## Acceptance and continuation

Bounded independent design/source critique found zero publication-blocking
P1/P2 findings. Its required W1 prerequisites are recorded above: fixed catchment
authority and coverage, exact crop chronology/remainder, bounded catch-up,
ownership and clean-cloth qualification. These are implementation prerequisites,
not shipped defects. W2 must prove seasonal reuse of the same physical frame.
The reviewer read the design and relevant source; no tests, Unity operations or
gameplay were performed for this design review.

Implementation follows native RED/countercases, ordinary GameBootstrap movement,
readers/actions, interrupted work and fresh save/reload, then independent source
and gameplay-evidence review. Preserve exact owned files and all foreign work.
Runtime acceptance and current Mac delivery are separate from this design.

Natural-discovery trials must include an ordinary route without a prepared
recipe checklist. Record noticeable clues, usable sources, reachable consumers,
what equipment was actually acquired, alternatives and opportunities skipped.
For each environmental opportunity, compare the ordinary and changed looks
from the same approach before opening Examine. Show that the visible cue
still matches readiness, closing or spent state after leaving and reloading.
Test both taking and ignoring a bonus. Tune encounter frequency and window
length from those observations; a directed success does not prove density.

All twelve complete-machine dependency statuses and the original failed full
regression remain open. Reclamation/regional processing stays the implementation
focus; weather supports useful loops within it. Graphics cancelled,
capstan/cultures parked, Deck deferred. No prior accepted branch or package is
repeated merely to prepare this design.

## One seasonal knotflax stand, two practical uses

Roadmap 33. Early in the existing growth cycle a sound cutting tool
can cut and twist one unit of cord from the optional wild stand. Its
flexible stems have value now. Leaving it standing preserves the existing
later seed-head harvest and public frame use. Either choice spends the
same finite stand. Ordinary planted crops, shop stock and seeds are still
available throughout the rest of the game.

The early row carries a quote for the exact stand, growth window,
ground and chosen cutter. The late yield retains its authored bundle. Both
use the same harvest receipt and source depletion. A moved, broken or foreign cutter
and a new window invalidate that quote. An existing coherent unspent
stand can use the choice. Its already spent counterpart stays absent.

The Editor trial took early cord to ordinary bandage work. A separate uncut
copy of the same stand supplied a late seed-head bundle and plantable seed.
The comparison advanced the owned game clock directly to the next growth
phase. Normal play uses the saved clock and ages the stand's options over time.
[Evidence](Verification/MachineSystems/SeasonalKnotflaxStems20261125/README.md)

## Sheltered winnowing with carried tools

A traveler can keep the chaff from a gathered knotflax bundle by working with a
sound hand sieve and directed air on a dry interior tile. This gives the bellows
a harvest use and makes a guesthouse or cave floor useful during rain. Standing
water and wet coatings close the assisted choice. Rubbing the heads through a
sieve still yields one seed and discards the chaff. The public frame still keeps
chaff on fair, dry days without requiring carried tools.

The assisted choice previews one seed and one unit of dry chaff for one bundle
and one action. It keeps both tools. The existing material receipt must check
the exact tools, batch, work cell and canonical products through commit. Losing
shelter or either tool closes the choice without spending the bundle. The work
uses no gas action and adds no wind simulation. The seed can be planted and
the chaff can fuel an eligible hearth.

The Editor trial offered only the sieve outdoors and both methods inside
The Dry Hem. It kept both outputs in one action and planted the exact seed
in a vacated bed. Fresh Continues preserved the products and then the crop.
The test supplied two tools and gathered the heads ordinarily. The outdoor
comparison occurred after the shower. [Evidence](Verification/MachineSystems/WinnowBellows20261127/README.md)

## Sill's shallow settling pocket

One ordinary shallow pocket can form on bare ground on Sill's north bank. The
regular river and the court stay intact. A pale streak under the water gives
the player a reason to stand on the bank and choose where the water goes.
A carried flask can collect twelve units. A pump can move those same units
to a clear outlet. Either physical result exposes the same pale sand. A
partial draw leaves the sand covered. Ordinary trade and other sources
still supply the same sand for Sill's existing filtration repair.

One fixed deposit keeps its spent state when the owner leaves the area.
The same cleared surface controls the mark, Examine and the gather action.
A refill or unsafe coating covers an unspent deposit. A fresh gather must
check the exact ground and state through transfer. One ordinary measure
of silver sand can join an existing compatible stack or remain in the
world if the pack is full. A spent deposit keeps its scrape after refill.

Five detached Unity layouts admitted bare grass at inlet 42,18, standing
cell 42,17 and outlet 41,17, with a dry path from the well approach. One
ordinary trial bought an empty flask, drew twelve water and saw the deposit
change before opening the action menu. The exact sand became a saved trail
note for water. Native tests verified the pump as a second approach.
[Evidence](Verification/MachineSystems/SillSettlingPocket20261127/README.md)
