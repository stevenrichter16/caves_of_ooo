# Chunk gameplay audit

Status: audit complete; recommendations not implemented, 2026-09-16. User request: assess actual chunk gameplay,
engagement and meaningful player activity across biomes and named areas.
Read-only audit: recommendations will not be represented as implemented work.

## Method and scope

Use the source-matched 412-chunk native census from the faction/POI audit, actual
blueprints, generation routing, interaction Parts, conversation actions and
quest wiring. Inspect all six ordinary biomes, settlement families, named
vertical sites, mysteries and generic caves. Count a capability as available
only when its content is placed and its gameplay path is wired.

For each area ask: what draws me in; what can I notice and do; what alternatives
matter; what risk/resource/tradeoff occurs; what reward or persistent outcome
follows; and what makes the next similar chunk or a revisit worthwhile?

Separate implemented evidence, design judgment and proposals. A visually
interesting object, dialogue tree, map marker or POI count alone does not prove
an engaging activity. Likewise, missing bespoke quests do not make systemic
combat, gathering and environmental problem-solving worthless.

Engagement assessments are design inferences from source and native content,
not measured player enjoyment or a claim of a new live playthrough. Current
full 3D-zone reveal and optional debug invincibility are assessed as configuration
modifiers, not permanent changes to the intended game balance.

## Verification plan

- Verify the previous census's 1,161-source fingerprint against the current tree.
- Independently audit ordinary biomes, settlement gameplay and cross-cutting
  combat/economy/progression; inspect vertical destinations and content density.
- Cross-check claims of absent/complete content against production wiring and
  tests. Record misleading visual promises and dormant mechanics explicitly.
- Review findings from the player's sequence of actions, not only file-by-file.
- Write a ranked improvement plan with concrete gameplay outcomes and success
  criteria. This deliverable is analysis; proposed gameplay changes remain unimplemented.

Qud reference: none required; this assesses Caves of Ooo on its own implemented
player experience. TDD is not applicable to documentation-only analysis.

## Overall assessment

**The world has more visual and geographical variety than objective variety.**
There is already a substantial systemic game: combat, elemental interactions,
equipment progression, harvesting, several crafting routes, trading, farming,
hauling, destructible structures, faction consequences and authored quests.
The problem is not that everything is scenery. It is that many differently
composed chunks offer the same immediate sequence: cross the terrain, fight,
open containers, collect something, leave.

The strongest places connect several verbs to a local need and an observable
outcome. The weaker places suggest an activity through their art or lore but
stop at examination text, an inaccessible destination, or a state predicate
that normal gameplay never changes. A new layout is initially interesting;
it does not indefinitely replace a new decision.

This is a design assessment, not an enjoyment score. Combat timing, encounter
balance, readability at the current camera, travel fatigue and actual first-hour
comprehension require a player session. They cannot be measured from a census.

## What a player can actually do now

| Working loop | Actual implementation | Practical limit for chunk engagement |
|---|---|---|
| Fight and develop a build | Six starter skills; cooldowns; kills award XP; levels grant HP, MP and SP; X-screen skill purchases enforce costs/prerequisites | Different terrain can change tactics, but many encounters lack a distinct local objective beyond killing/looting |
| Harvest and make equipment/consumables | One-shot sources, weapon component crafting, alchemy at appropriate facilities, separate tinkering recipes/bits | Starting kit already grants two of every 13 production reagents and six weapon components; helpful experimentation may weaken the first regional supply expedition |
| Trade, recover and prepare | Real inventories/drams, eligible trader restocking, safe rest, wells, sanctuary donations | Shared services dominate many settlements; replenishment is not a simulated regional economy |
| Change the physical space | Destructible owners, container contents spill, constrained hauling with movement cost | Destruction depends on the owner's Parts; voxel appearance alone does not confer it. No general player-facing construct-a-house loop was found |
| Cultivate plants | Seeds, plantable ground, watering and crop growth | Growth processes the active zone; leaving a farm does not simulate its growth. Most authored field rows are a different, non-harvestable object |
| Take quests and make social choices | Bespoke town errands, global quest state, faction reputation, hospitality, repair/cargo contracts | Generic village quest/cast selection is much smaller than the number of town layouts |
| Explore, read, descend | Named sites, lore dialogue, stairs, lairs, deeper strata and fauna | Discovery and examination do not always culminate in an action, unlock or lasting outcome |

Sources: [starter spells](../Assets/Scripts/Gameplay/Skills/StartingSpellKit.cs),
[starter materials](../Assets/Scripts/Gameplay/Items/CraftingStarterKit.cs),
[crafting UI](../Assets/Scripts/Presentation/UI/InventoryUI.Crafting.cs),
[trader restock](../Assets/Scripts/Gameplay/Economy/TraderRestockSystem.cs),
[crop clock](../Assets/Scripts/Gameplay/Farming/CropSystemPart.cs),
[village population](../Assets/Scripts/Gameplay/World/Generation/Builders/VillagePopulationBuilder.cs).

Reactive terrain is an important counterexample to purely decorative variation:
clustered hazards and conductor runs can change where elemental attacks are
useful or dangerous. That supports improvised combat decisions even without a
bespoke quest. Its encounter balance and readability still need playtesting.
[Hazard placement](../Assets/Scripts/Gameplay/World/Generation/Builders/HazardTerrainBuilder.cs).

One useful correction: `SanctuaryPart` still has a stale "pure marker" header,
but its live handler offers a **5-dram donation for 150 turns of damage reduction
by 1**. River shrines are a real preparation choice, not merely decoration and
not an HP-healing service. NPC flight-to-shrine behavior also exists.
[Actual handler](../Assets/Scripts/Gameplay/Settlements/SanctuaryPart.cs).

## Ordinary biome experience

### Grovelands: the most integrated ordinary wilderness

**Player sequence:** notice a seep, luminous grove or Choir formation → choose
conversation/trade, gathering, extraction or a route through it → weigh combat
tools against local law → leave with supplies, loot or changed reputation.

Working actions include water drawing, GroveRed gathering, tendril conversation
and trade, compost finds, rare Choir Iron extraction and hazardous sundews.
Mining Choir Iron costs 15 RotChoir reputation; player-caused ignition costs
40 per ignited object. These are actual consequences, so clearing a path with
fire is not equivalent to walking around it. Known traders and unspent resources
give some reason to return. A tendril also serves as a real destination for
Cinderhold's pruning writ, connecting wilderness to an authored faction choice.

**Design judgment:** strongest combination of environmental identity and
meaningful choices. It still needs clearer activities beyond repeated gathering
and caches. FruitingBody scenery is not harvestable, compost rewards are ordinary
coins, and visible reclamation stages are generation states rather than a
developing regional process. Do not advertise those as simulated ecology.

Evidence: [formations](../Assets/Scripts/Gameplay/World/Generation/Builders/GrovelandsFormationBuilder.cs),
[Grove law](../Assets/Scripts/Gameplay/Factions/GroveLaw.cs),
[blueprints](../Assets/Resources/Content/Blueprints/Objects.json).

### Beating: preparation and route planning, then a relatively easy solution

**Player sequence:** check the time and headgear → select a route between relief
points and resources → manage exposure/contact/combat → recover salt, loot and
access to another destination.

During Height, ten exposed turns add Parched, up to −3 Strength/Agility. Any
equipped head item prevents exposure; marked interior cells reset its streak;
water/wells cure it and tonics reduce it. Pale Salt and saltbriar are useful
resources, and scorpions/hounds change encounters.

**Design judgment:** a real regional preparation loop, but one head item largely
solves its signature pressure. Dunes are solid blockers, not climbable slopes;
salt-crust sharpness is not an implemented hazard; visual ruin cover is not
automatically shade because the rule checks `IsInterior`. Once prepared, many
chunks return to the shared scavenge/fight/traverse loop.

Evidence: [glare](../Assets/Scripts/Gameplay/World/BeatingGlareSystem.cs),
[Parched](../Assets/Scripts/Gameplay/Effects/Concrete/ParchedEffect.cs),
[composition](../Assets/Scripts/Gameplay/World/Generation/BeatingCompositionPlan.cs).

### Sodden: meaningful movement choices, less differentiated payoff

**Player sequence:** read dry banks and causeways → choose a safe detour or a wet
shortcut → handle coating, ambush/contact hazards or peat fire → recover loot,
FrogOil and onward access.

Bog-mire coating applies −2 Agility and 1 Acid damage per turn while it persists.
Greatdew can root and corrode; Bandfrogs punish adjacent physical attacks through
caustic skin (not simple proximity or distant/elemental damage); stationary MawToads
change approaches. Burning peat can produce poisonous gas. These make different
formations tactically relevant, rather than simply different silhouettes.

**Design judgment:** good terrain problems, especially when carrying something
valuable. Ordinary rewards and objectives distinguish it less than its hazards.
BogTakenBody is examinable scenery, not an identification/recovery interaction;
duckboards do not imply a player bridge-building system. "High water" and
"receding water" are fixed generation conditions, not a changing flood clock.

Evidence: [bog-mire](../Assets/Resources/Content/Data/LiquidDefinitions/bog-mire.json),
[liquid contact](../Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs),
[composition](../Assets/Scripts/Gameplay/World/Generation/SoddenCompositionPlan.cs).

### Spread: approachable travel and gathering; fields overpromise

**Player sequence:** follow roads or explore hedged parcels → forage, breach a
hedge, fight/avoid fauna and inspect containers → acquire supplies and travel on.
Player-planted farming is possible, but most visually cultivated fields do not
participate in it.

**Design judgment:** useful connective country and an accessible introduction
to wilderness verbs; comparatively weak local purpose after initial discovery.
The audited seed contains **3,277 CropRow owners in 30 of its 132 POI-free Spread
surface chunks**, but CropRow has neither CropPart nor Harvestable. "Tended",
"after harvest" and "returning scrub" describe generation variants, not an
evolving agricultural economy. These rows are currently scenery/terrain, not
3,277 harvesting activities.

Evidence: [Spread composition](../Assets/Scripts/Gameplay/World/Generation/SpreadCompositionPlan.cs),
[seed interaction](../Assets/Scripts/Gameplay/Farming/SeedPart.cs),
[blueprints](../Assets/Resources/Content/Blueprints/Objects.json), native census.

### Stump: distinctive ecology with several dormant promises

**Player sequence:** read elevation bands and formations → navigate ridges,
observe endemics, fight, seek Tepuibone or a descent → gain minerals/discovery.
The Brocchinia-Sentinel's retreat into nearby bromeliads is genuinely wired.

**Design judgment:** excellent basis for observation-led exploration, but the
most evocative regional state changes are incomplete. TankBrocchinia describes
drinkable water without a drinking/well action; SprayPool lacks liquid contact;
DescentLedge supplies no climbing verb. Singer silence is not yet a functioning
alarm. `UrquActive` and `EcologyDamaged` are read by spawn rules, but no production
C#/JSON writer was found. Tests can enable them; that is not an ordinary player
route to manifest-period snakes/eagles or an ecology response. The audited
ordinary Stump rows contain no SariSnake/SkySari.

Evidence: [bestiary predicates](../Assets/Scripts/Data/Tables/PopulationTable.cs),
[sentinel retreat](../Assets/Scripts/Gameplay/AI/AIBromeliadRetreatPart.cs),
[Stump composition](../Assets/Scripts/Gameplay/World/Generation/StumpCompositionPlan.cs),
[W6 deferrals](FELLING-W6-PLAN.md).

### Overwrit: atmospheric absence; the thinnest current activity

**Player sequence:** cross the blank → examine sparse new growth or find a
descent → leave. In this seed, **22 of 23 surface chunks contain only ground,
new growth and, in seven cases, stairs**. No ordinary creatures, harvestables,
containers, traders or pilgrimage markers occur in those 22 composed rows.
Memory bleeds remain deferred.

The exception is Unsaying (2.11.0), whose legacy pipeline supplies monsters and
chests. That is not evidence of a completed Hush settlement or cultural loop.

**Design judgment:** quiet is valuable, but 22 chunks of largely unchanging
absence provide little sustained choice or payoff. The answer should preserve
that mood through a few coherent, discoverable events—not fill every blank
with ordinary enemies, barrels and chores.

Evidence: [routing](../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs),
[composition](../Assets/Scripts/Gameplay/World/Generation/OverwritCompositionPlan.cs),
[measured rows](Verification/ChunkGameplay/affordance-proxy.json).

## Named and vertical destinations

| Destination | What actually happens | Assessment from the player's perspective |
|---|---|---|
| Deepest Cathedral (5.4.0–2) | Descend through native zones; combat/loot; converse with Encased Elders; Choir trade; encounter luminous node architecture | Strong first arrival and lore. Repeated elders use the same conversation. The node is not a travel action; Wedded audience/original-body arc is deferred. A pilgrimage currently has less payoff than its presentation promises |
| Stillleaf (2.4.0–2) | Reach sealed archive surroundings; encounter ordinary cave danger/loot; examine library | Key/reading quest is deferred. Synthetic test keys do not provide a normal acquisition route. Shelves are not readable books or loot containers. The archive's central activity remains unavailable; the entire area is not inaccessible |
| Ginmere (2.7.0–2) | Explore drowned sima, observe frogs, fight/loot, discover a warning-bearing gecko nest | The nest is a real choice: disturbing it can spawn 16 hostile geckos with actual turns. Helmwood passage is a seed-selected shortcut to an existing floor, not a guaranteed new destination; absent in this census seed |
| Felling-Site (3.5.0) | Investigate six positions and the empty seventh; standing at the seventh causes brief nonstacking confusion | Memorable foreshadowing and local effect. Not a completed ceremony, god audience or ending; do not sell it as one |
| Ridge/multi-cell pilot (3.7.0) | Interact with native destructible scenery, move suitable loads, encounter a hermit and hazards | Useful systemic experimentation and traversal. No distinct larger quest was found; impressive geometry alone does not make it an adventure |
| Lairs | Boss room, guards, loot and side rooms | Focused combat expeditions. Loot placement does not guarantee the boss must be defeated before it is taken. Few lairs cannot carry the whole exploration loop |
| Generic underground | Stairs, changing strata, tiered creatures, resources/containers; conditional galleries and reliquaries at depth | Supports expeditions and build progression. Descending farther eventually exceeds the tier cap; more depth does not imply indefinitely new objectives or an implemented final resolution |
| Woven doll and Tenth Fire | Discover a takeable strange doll or an untended thermal/light source | Legitimate small mysteries. Their value can be atmosphere and memory; no quest is required. Preserve intentional ambiguity rather than manufacturing an explanatory reward |

Sources: [Cathedral builder](../Assets/Scripts/Gameplay/World/Generation/Builders/ChoirCathedralBuilder.cs),
[Choir dialogue](../Assets/Resources/Content/Conversations/RotChoir.json),
[Stillleaf builder](../Assets/Scripts/Gameplay/World/Generation/Builders/SealedLibraryBuilder.cs),
[nest](../Assets/Scripts/Gameplay/Entities/PricklebrowNestPart.cs),
[Helmwood selection/travel](../Assets/Scripts/Gameplay/World/HelmwoodPassages.cs),
[seventh position](../Assets/Scripts/Gameplay/World/SeventhPositionPart.cs),
[multi-cell runtime](../Assets/Scripts/Gameplay/World/MultiCellPilotRuntime.cs),
[lair population](../Assets/Scripts/Gameplay/World/Generation/Builders/LairPopulationBuilder.cs).

## Settlements: services are real; unique local work is uneven

The audit covers all **17 surface villages plus Olderdeep, Lampwell and
Spivenor**. Ordinary surface villages share merchants, quartermaster rentals,
scribe copying, paid rest, furnishings, utility rites and a distributed quest;
Sill has its special starter package and Morrowfast is bespoke. Rentals take ink
and refund on return; copying creates another grimoire while preserving the
original. Eligible traders refill after time and zone re-entry. These are useful
services, but are usually not unique reasons to select one town over another.

| Place (surface unless depth shown) | Concrete activity and outcome | Depth and limits |
|---|---|---|
| Morrowfast 3.6 | Carry a permitted assurance; choose and fit a quiet bell sleeve (FireClay) or free loud clapper; move a supper stool without blocking the room; opt into/withdraw record consent; rest and shop | Three journalled local errands and persistent object/social changes. Strong authored introduction nearby the actual spawn. No configured XP/dram reward for these errands; bell awareness/feedback is not an elaborate resident emergency simulation |
| Sill 10.10 | Hallun's witness-book/soot-gremlin errand; Ellun's old-stump visit; five shops; forge/alchemy; well/oven/lantern repairs; one-time water story changes dialogue | Richest mechanical onboarding hub, geographically detached from the current fresh spawn |
| Wellmeet 8.16 | Three-day guest-right, PaleSalt for reputation, three repair sites | Temporary magic, permanent manual repair and teaching have distinct persistence. Strong small maintenance/preparation loop |
| First Tent 5.17 | Guest-right, salt exchange, wells and marked interior shade | Useful oath/water stop; keeper shares ordinary host behavior. No unique historical trial or Wellmeet repair package |
| Cinderhold 6.6 | Take factor's writ, post through a Choir tendril, return for 20 drams/+10 Concord; posting costs 9 Choir reputation. Refusal costs 5 Concord once. Forge and weaponsmith | Complete journey with a faction tradeoff and repeatable equipment service. Not a mining-management system |
| Drowned Ledger 17.5 → Marrowstye 12.12 | Sorter supplies a separate sealed parcel, weight 30; deliver to the actual intake clerk for 25 drams/+10 Pale Curation | Strong travel/hauling objective, one-shot. Three ancient witnesses remain untouched; no playable body-reading, preservation-fee service or ongoing courier economy |
| Quillhold 14.9 | Six usable shelf containers: WardGleam, DryingBreeze, four inks; copying/trade | Real first-visit texts and a learning service. No Reader audience or major archive faction quest |
| Gantry 7.8 | Shared exchange/rental/copy/rest; caravan-host guest-right; registrar gives Marrowstye direction | Good service junction. Paperwork dialogue is not a document/dispute transaction; no hired caravan |
| Tine 13.7 | Ordinary services and copying amid functional shoreline/boardwalk terrain | Distinct setting, no fishing, usable boat or special retired-scribe arc |
| Sumphold 15.6 | Services and body-trade dialogue; real dry access/work-cut terrain | Boat frames and toll rolls are examinable props, not boat building/travel or toll payments |
| Tally 10.14 | Stocked exchange, rentals/copy/rest and campfire | Useful trade stop; debt clearing, Counter audience/fees and caravan hiring remain absent |
| Last Counter 18.18 | Envoy stock, chest, campfire and ordinary village services | Frontier provisioning works; no distinct expedition commission or delivery guarantee system |
| Posy 5.9, Salt-Vault 15.15, Quiet's Door 16.1, Slip 16.11 | Shared village services/quest/drama with faction assignment | Weakest bespoke identities: no implemented resin-art economy, indexed Salted archive, Quiet admission trial or localized name-unsaying activity |
| Olderdeep 4.6.2 | Offer Tepuibone for Catacomb trust; stand on founding plume and rest; obtain RootedMet and 14 days of patch-bloom recognition/dialogue | A complete small pilgrimage linking resource, relationship, position and remembered outcome. Hearth damage has real severe faction consequences |
| Lampwell 12.3.2, Spivenor 16.4.2 | Shared stranded-settlement plaques, hearth, warden/tender dialogue and hearth-damage law | Atmospheric discoveries, little distinct ongoing work. No native shop economy, Lampwell hatchery/light trade, or bespoke Spivenor admission loop |

Evidence: [Morrowfast](../Assets/Scripts/Gameplay/World/MorrowfastQuests.cs),
[repair eligibility](../Assets/Scripts/Gameplay/Settlements/SettlementSiteDefinitions.cs),
[repair state](../Assets/Scripts/Gameplay/Settlements/SettlementManager.cs),
[contracts/hosts](../Assets/Resources/Content/Conversations/FriendlyNPCs.json),
[Choir posting](../Assets/Resources/Content/Conversations/RotChoir.json),
[Quillhold shelves](../Assets/Scripts/Gameplay/World/Generation/Builders/QuillholdCompositionBuilder.cs),
[Olderdeep trust](../Assets/Scripts/Gameplay/Settlements/FoundingTrustService.cs),
[founding dream](../Assets/Scripts/Gameplay/Entities/FoundingPlumePart.cs),
[stranded settlements](../Assets/Scripts/Gameplay/World/Generation/Builders/StrandedSettlementBuilder.cs).

### Repetition is partly shared state, not just similar writing

Six bare quest IDs are distributed across ordinary nonstarting villages:
CrunchyLocket, HiddenShrine, ClearTheWarren, TheCandyTax, MessageForHermit and
StrongestInOoo. Active and completed state are keyed only by that ID. Dialogue
predicates/actions suppress a new offer once active or completed. A second town
with the same quest therefore does **not** supply an independent local version.
Failure may permit retry; completion does not start another village's copy.

House Drama generation selects Vex or Thresker and registers state by global
drama ID. It has real branching narrative choices, disclosures and path locking,
but repeated casts do not produce sixteen independent town campaigns. No
production consumer of EvaluateEndState transforming a settlement was found.

Sources: [quest pool](../Assets/Scripts/Gameplay/World/Generation/Builders/VillagePopulationBuilder.cs),
[quest state](../Assets/Scripts/Gameplay/Storylets/StoryletPart.cs),
[offer gates](../Assets/Scripts/Gameplay/Conversations/ConversationPredicates.cs),
[start action](../Assets/Scripts/Gameplay/Conversations/ConversationActions.cs),
[drama state](../Assets/Scripts/Gameplay/HouseDrama/HouseDramaRuntime.cs).

### Focused defects and integration risks for a follow-up fix pass

- **Confirmed incomplete guidance:** Scribe RegionOverview offers only Back after
  promising four places. No regional-choice injector supplies the missing choices.
- **Confirmed shared quest identity:** repeated generated givers share global
  completion. Resolve intentionally by unique local instances or nonduplicated
  placement, rather than simply writing more repeated dialogue.
- **Confirmed restock exclusion:** Morrowfast's two traders have empty declared
  stock tables and Stillcord faction; the restock filter excludes them from both
  purse and inventory renewal. Initial shopping works; generic town restocking
  should not be promised here.
- **Reputation exploit candidate:** the Envoy has repeatable, ungated +5 Concord
  dialogue choices whose action directly adjusts the ledger. Source supports the
  loop; an executable regression should establish it before changing behavior.
- **Voxel presentation risk:** QuestBeacon changes the old Render-event ColorString;
  the inspected voxel presentation has no matching tint path. Verify from the
  actual camera whether quest cues are lost. This is not a claim that NPC models
  themselves are invisible.
- **Promise mismatch:** BeetleJar describes crushing it to alert wardens, but no
  corresponding native alarm/destruction path was found. Distinguish flavor from
  a usable emergency tool in text or implement the promised behavior.

Sources: [merchant creation](../Assets/Scripts/Gameplay/World/MorrowfastContent.cs),
[restock eligibility](../Assets/Scripts/Gameplay/Economy/TraderRestockSystem.cs),
[Envoy choices](../Assets/Resources/Content/Conversations/Factions.json),
[quest cue](../Assets/Scripts/Gameplay/Storylets/QuestBeaconPart.cs).

## Measurement: presence is not depth

The prior native run generated **400 surface chunks plus 12 named belowground
chunks**, seed **141343545**, with zero generation/runtime errors and zero
diagnostic drops. This audit reuses that census after confirming **all 1,161
recorded source fingerprints still match**. It does not claim another Unity run
or that this is the player's already visited/destroyed save state.

The reproducible [affordance join](Verification/ChunkGameplay/affordance_proxy.py)
combines actual blueprint occurrences with inherited authored Parts/Tags.
Selected surface counts, **including settlements**, are:

| Biome | Total chunks | At least one container owner | At least one harvestable owner | At least one conversation owner |
|---|---:|---:|---:|---:|
| Spread | 142 | 142 | 133 | 39 |
| Beating | 107 | 106 | 35 | 59 |
| Sodden | 63 | 63 | 11 | 14 |
| Grovelands | 43 | 41 | 27 | 16 |
| Stump | 22 | 20 | 9 | 6 |
| Overwrit | 23 | 1 | 0 | 0 |

These are occurrence counts, **not completed activity, stocked loot, unique
dialogue or fun counts**. Runtime-injected owners/Parts are outside the static
join and explicitly listed unresolved; Morrowfast is an important example
requiring manual inspection. Creature tags include passive fauna. Named sites
and legacy exceptions also prevent equating a biome total with composition scope.

Interpretation: container opportunities are already widespread. Adding another
generic chest is unlikely to address the missing purpose of a chunk. Overwrit's
22-row absence is additionally checked directly against its native owner lists,
so that finding does not depend on the static-capability proxy.

## Why roaming can feel less eventful than the world inventory suggests

1. **Discovery lacks useful direction.** Innkeeper rumors give generic cave/ore
   advice. The Scribe's RegionOverview asks which of four places the player wants
   to hear about, but only offers Back. A town or landmark existing somewhere
   does not give the player a reason to seek it or a usable lead.
2. **Different compositions often share the same purpose.** Ruin shapes, crop
   stages, high/low water and reclamation vary at generation. Most do not become
   ongoing local stories. Visual variation remains valuable, but its gameplay
   contribution is mostly navigation/encounter geometry.
3. **Regional supplies are not yet the strongest early motivator.** Broad starter
   materials make experimenting easy. Without clear resource goals, the player
   has less reason to cross a dangerous region for a particular ingredient.
4. **Consequences persist more often than opportunities evolve.** Cached chunks
   preserve looting and destruction instead of rebuilding. Trader restocking,
   specific quests and repairs are exceptions; an ordinary revisit is not a new
   encounter roll. Farming advances only in the active zone.
5. **The chosen viewing configuration makes this especially visible.** The scene
   enables full 3D-zone reveal. It removes visual uncertainty, not native combat
   targeting/AI rules. Optional F12 invincibility, when enabled, additionally
   removes much of danger's contribution. Neither setting was changed here.

Current fresh spawn is **Grovelands 2.6.0**, one chunk west of Morrowfast. Sill at
10.10 remains the historical starter-village content hub; its richer introductory
services/quests should not be assumed to surround the current fresh spawn.

Sources: [Innkeeper](../Assets/Resources/Content/Conversations/Innkeeper.json),
[Scribe](../Assets/Resources/Content/Conversations/FriendlyNPCs.json),
[scene](../Assets/Scenes/Main/SampleScene.unity),
[zone cache](../Assets/Scripts/Gameplay/World/Map/ZoneManager.cs),
[coverage audit](VOXEL-WORLD-COVERAGE.md).

## Recommended development order — proposals, not implemented changes

First isolate the focused defects above into small regression-driven fixes.
Source-confirmed behavior, a likely exploit and a visual risk require different
verification: action tests for identity/reputation/restock, and a real voxel-camera
check for quest cues. Do not conflate these with the larger design work below.

### 1. Make the current opening teach a complete expedition loop

Use 2.6.0 → nearby Morrowfast → an actual nearby need/resource/contact → a return
with a visible consequence. Introduce conversation, harvesting or hauling, and
one environmental tradeoff through existing systems. Give specific truthful
leads, not a generic instruction to explore the entire map.

**Success criteria:** a new player can identify a purpose and destination without
a debug map; the lead references a real generated target; journal and dialogue
agree; destroyed/unavailable targets fail gracefully; completion changes an
object, relationship or service and points toward a second voluntary activity.

### 2. Repair misleading affordances and finite-content discovery

Make selected authored crops participate in harvesting/farming, implement the
promised bromeliad/spray-water interactions, and distinguish decorative growth
from usable sources. Populate Scribe regional guidance from real known places.
Audit quest discoverability in the voxel renderer and repeated quest identities.

**Success criteria:** examination text matches available verbs; assets communicate
whether an object is usable; a repeat visit to a second quest town cannot reset
or silently consume another town's objective; cues are visible in the actual
voxel camera, not only a legacy Render color event. Do not turn every decoration
into a mandatory collectible.

### 3. Finish one major destination's payoff before adding another destination

Stillleaf is the clearest incomplete promise: author a lore-consistent route from
learning of the seal to acquiring access, reading/using its contents and receiving
a consequence. Alternatively prioritize the Cathedral's missing relationship
or audience loop, but finish one complete chain before multiplying entrances.

**Success criteria:** every prerequisite is obtainable in ordinary play; the
quest survives different action orders; the main interaction is reachable;
reward changes a player option or relationship; intended mysteries remain
unexplained where canon requires that. A synthetic test key is not sufficient.

### 4. Add a small set of biome-specific problems, with alternatives

Build reusable situations from current verbs, rather than uniform extra clutter:

| Biome | Proposed situation | Meaningful alternative and payoff |
|---|---|---|
| Grovelands | Needed resource inside protected growth | Negotiate/trade, seek another source, or extract and accept Choir cost; reward reflects the chosen approach |
| Spread | A cultivated parcel has a concrete local need | Supply, harvest/help, or take resources at an appropriate ownership cost; field and recipient change |
| Beating | Delivery or salvage across exposure and relief points | Prepare gear, wait for a safer phase or take a costly detour; arrival condition matters |
| Sodden | Recover and haul an identified load along fragile dry access | Longer safe haul, hazard mitigation, or repair where supported; a named recipient and permanent result |
| Stump | Observable disturbance affects an endemic habitat | Protect, exploit or observe; Singer behavior and permitted fauna changes make the state legible |
| Overwrit | Sparse linked evidence creates an investigation route | Decide what to preserve/carry/report; absence and discontinuity convey progress without ordinary mob/chest density |

These are design proposals requiring lore review, not claims those scenarios
already exist. Generation should bind actual actors/objects/locations to each
situation and validate access. Keep quiet transit chunks and optional mystery
beats; not every chunk needs a quest.

**Success criteria:** each selected situation has a readable cue, at least two
viable approaches, a specific payoff and persistent resolution; no duplicate
global IDs; no stranded mandatory actor; seed variation changes a decision or
relationship, not just prop positions. Resource and reward balance must be tested
in play rather than inferred from counts.

### 5. Connect one regional state cycle and a few worthwhile revisits

For Stump, define the normal-play producer of Urqu/ecology state, observable
warnings, duration/recovery, and behavior in already visited chunks. For towns,
prefer repairs opening a service, changed contacts or consequences of a completed
delivery over indiscriminate respawns. Decide deliberately whether unattended
crop growth should advance and what it costs.

**Success criteria:** an ordinary player can cause or witness both sides of the
cycle; existing cached zones and fresh zones agree; signals precede dangerous
changes; spawns do not duplicate; outcomes survive leaving/returning; the player
has a stated reason to revisit and sees that reason fulfilled.

### 6. Validate the experience through short player journeys

Run the current opening and one excursion per biome with the chosen full reveal
camera, plus a combat assessment without invincibility. Record actions/decisions,
time to first purposeful interaction, travel without a decision, reward usage,
and whether the player can explain what changed. These are proposed measurements,
not results from this source audit. Use them to tune cadence and clue quality
before expanding world size or decorative density again.

## Verification and self-review

- **Source verification:** 1,161 matching SHA-256 fingerprints; zero added,
  removed or changed paths in the corresponding Scripts/Editor C# and Content
  JSON sets. [Receipt](Verification/ChunkGameplay/source-verification.json).
- **Reproducible analysis:** run `python3 Docs/Verification/ChunkGameplay/affordance_proxy.py`
  from the repository root. It verifies source provenance, resolves inherited
  blueprints, checks all 400 surface rows and emits the proxy/receipt. Direct
  counterchecks cover Overwrit owners, Spread crop counts and Helmwood selection.
- **Independent cold-eye reviews:** global player loop/proxy, all six biomes,
  and settlements. Review challenged the repetition thesis using real reactive
  combat, faction law, shrine preparation, hauling and complete social outcomes.
- **🟡 Corrected before commit:** container-owner presence had been described as
  loot presence; changed to container opportunities. Contents were not measured.
- **🔵 Corrected before commit:** one source-link path, current-zone versus
  world-map reveal wording, and Bandfrog retaliation trigger precision. Added
  explicit reactive-terrain and pruning-expedition counterexamples.
- **🧪 Deferred verification:** no new Unity run, hands-on playthrough, exploit
  regression or voxel-camera quest-cue check was performed. These are identified
  follow-up work, not implied passes. Source confirmation does not measure fun,
  balance or actual time between meaningful decisions. One seed is not every seed.
- **⚪ Scope:** no production behavior, game assets, scene, save or editor state
  changed. Feature TDD/adversarial runtime gates do not apply to this docs-only
  deliverable; proposed fixes must pass them when implemented. Existing unrelated
  workspace changes remain outside this commit.

## Implementation log

- Read CLAUDE.md. Confirmed prior census source fingerprint has zero drift.
- Assigned independent read-only reviews of wilderness, settlements and global
  gameplay loops; began vertical-site and native-content analysis.
- Cross-checked actual handler behavior rather than stale marker comments;
  verified global quest identity, missing regional guidance, Morrowfast restock
  eligibility and active-zone farming boundaries.
- Joined inherited blueprint affordances to native census owners; separately
  checked the sparse Overwrit and decorative Spread field claims.
- Completed all six biome assessments, 17 surface/three underground settlement
  coverage, named-site analysis and prioritized plans with acceptance criteria.
- Completed three independent review passes and incorporated every report issue;
  validated linked source paths and regenerated the analysis successfully.

Files in this documentation-only change:

- `Docs/CHUNK-GAMEPLAY-AUDIT.md`
- `Docs/Verification/ChunkGameplay/affordance_proxy.py`
- `Docs/Verification/ChunkGameplay/affordance-proxy.json`
- `Docs/Verification/ChunkGameplay/source-verification.json`
