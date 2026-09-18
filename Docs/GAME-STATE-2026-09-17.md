# Caves of Ooo — state of the game and the path to release

**Assessment date:** 17 September 2026, America/Chicago. Some verification receipts use 18 September UTC.

**Verified game snapshot:** local `main`, accumulated runtime integration commit `26fbe544`. Subsequent documentation commits do not change that runtime.

**Purpose:** explain what a player can actually do, distinguish partial implementations from ideas, and give the next development agent an honest release baseline.

This is a substantial playable development build with a systemic foundation, a large authored world, extensive voxel presentation, and several complete local adventures. It is not yet a demonstrated complete campaign or a release-certified product. Its largest gap is the connection between its many systems and places: there is more geographical and visual variety than sustained objective variety.

The game should be developed as a **persistent, turn-based RPG**. Death can be recovered through saving/loading; character development, relationships and world changes belong to a continuing campaign. Mandatory permadeath, disposable runs and between-run metaprogression are not the project's identity. The ambition associated with Caves of Qud is systemic possibility and an unusual, consequential world—not a requirement to copy its mechanics, presentation or release scope.

## How to read the status labels

- **Playable:** a native gameplay path exists, content reaches it, and relevant implementation or executable evidence is recorded.
- **Partial:** a useful subsystem exists, but its content, integration, reach, feedback or complete progression loop remains bounded.
- **Asset/toolkit:** art or authoring infrastructure exists; this does not establish an associated player mechanic.
- **Proposed:** a design direction or lore concept, not an available game action.
- **Unverified:** this review does not establish completion. It is not a confident claim that no implementation exists anywhere.

A screenshot can establish an appearance. A blueprint can establish a definition. A passing unit test can establish its asserted behavior. None, alone, proves that an ordinary player can find and complete the experience. This document distinguishes those kinds of evidence.

## 1. Current build, source control and verification

The formerly active `codex/voxel-town-generator` branch was 365 commits ahead of `main`. Important runtime art, presentation code, audio, generators and recent gameplay work also remained uncommitted. A preservation commit included the accumulated playable state before `main` was advanced by fast-forward. The working checkout is now on `main`; no remote push was performed.

The integration did not reset or rewrite unrelated working files. Logs, Python caches, temporary Unity test scenes, automatic Blender backups, local ProBuilder preferences and bulk historical raw verification media remain outside the preservation commit. Authored game assets and their metadata, source generators, tests, living documents and compact evidence were included. The candidate index was compared against the validation copy: **12,979 Assets/Packages files matched**, with only explicitly documented non-runtime exclusions and private clone Company/Product settings.

The accepted current-wave evidence is:

| Gate | Result | What it establishes |
|---|---|---|
| RS28 full EditMode suite | 14,901 total; 14,869 passed; 32 inherited failures; zero C# errors | No added regression from Morrowfast/regional changes; not an all-green or release-ready claim |
| RS26 focused regional suite | 100/100 passed | Request state, transactions, lifecycle, cues, notes and actual C-menu dispatch coverage |
| RS27 native journey | 56/56 checks, 877 queued native steps, nine borders | Actual new-game/bootstrap, conversation, movement, harvesting, delivery, notes and F5/F6 paths |
| Native visual review | All 17 original 1080p captures inspected | Coarse Morrowfast, five interior cutaways, cues and text in those captured views |
| Metadata audit | 6,760 metas; no collisions | Current checked GUID integrity; 117 coarse mesh ownership/catalog bindings validated |
| Live project import | New regional type loaded; no `error CS` console entries | Published scripts reached the user's editor; the editor remained open |

The native regional journey deliberately enabled F12 invincibility for its long resource trip. Its earlier cloth expedition and interior visits used ordinary damage. This run proves interactions and persistence, not balanced combat over the whole route. Profiling covered a real 60-second Editor window, but included loading, harness planning and screenshot costs.

Release stabilization has begun separately. Its **R103 candidate passed all 14,901 EditMode tests with zero C# errors**, but remains outside the accepted main runtime pending native visual, performance and final review gates. See section 19 and the accompanying handoff prompt.

## 2. What starting and playing currently looks like

The configured gameplay scene is `Assets/Scenes/Main/SampleScene.unity`. A fresh game starts in the western Grovelands chunk, **Overworld.2.6.0**, with Morrowfast one chunk east at **3.6.0**. Continuing a save restores its saved active zone instead. Sill remains a real, mechanically important village at **10.10.0**, but it is not the current scene-configured spawn.

This distinction matters because three historical concepts coexist: the serialized scene spawn, a code default associated with Morrowfast, and the older `WorldMap.StartingZoneID` used by Sill-related content. Reordering a place table or changing the old constant is not a safe way to change today's player start.

The ordinary player begins with a modest physical kit: a dagger, two healing tonics and two dried meats, plus separately granted spell and farming kits. The large crafting-material grant belongs to developer mode. It should not be used to judge normal progression or to design quests around an imaginary surplus.

A demonstrated opening now exists:

1. Travel from the western field to Morrowfast.
2. Speak with Farra and accept the dry-cloth request.
3. Return to the actual field cache, open its container and take the parcel.
4. Carry it back through a real zone transition and deliver it once.
5. Receive useful supplies, change the local supper state and retain completion after saving/loading.
6. Learn actionable regional directions from Vennit and continue into existing bell work or the new regional requests.

This is a complete early expedition, not yet an entire first act. It teaches several native verbs in context. The next release task is to confirm whether someone unfamiliar with the code understands it, finds the relevant characters, and experiences an appropriate level of danger and travel effort.

The current display uses the approved tilted orthographic view and **1.2× viewing scale**. Full 3D-zone reveal is intentionally enabled at the user's request. F12 toggles debug invincibility. These settings must be distinguished from the intended release balance: visual reveal does not automatically remove native attack, AI or targeting rules, and optional invincibility is not ordinary player power.

## 3. Mechanics inventory at a glance

| System | Current status | Available now | Important missing or bounded part |
|---|---|---|---|
| Turns and movement | Playable | Grid movement, actions, zone traversal, native occupancy | Whole-game pacing and every interaction's cost still need release review |
| Combat | Playable foundation | Melee, spells, targeting, effects, damage, defenses, deaths | Encounter purpose, build balance and readable failure across a campaign |
| Skills and progression | Playable | Six starter abilities, XP/levels, purchases and prerequisites | Tested long-term build diversity and complete progression pacing |
| Equipment/anatomy | Playable foundation | Body/equipment model, actor loadouts, weapon handling, item lifecycle | Some presentation gaps; full roster/build acceptance is broader than recent tests |
| Materials/environment | Playable foundation | Liquids, heat, gases, coatings and elemental interactions | Not a general autonomous ecosystem or evolving climate |
| Inventory/containers | Playable | Pickup, drop, carrying, stacks, containers and transactional actions | Release-wide usability and every destructive edge case |
| Trade/services | Playable | Drams, real stocks, eligible restocking, rentals, copying and repairs | No general production/caravan/price simulation |
| Crafting | Playable foundation | Weapon components, alchemy, tinkering and recipes | Discoverable normal-supply progression and sustained ingredient choices |
| Farming | Partial but usable | Planting, watering and active-zone crop growth | Offscreen farming simulation and a regional agricultural economy |
| Finite field harvesting | Playable | Selected ripe rows and persistent spent state | Not every field-like object is harvestable or renewable |
| Destruction/hauling | Playable foundation | Native destructible owners, spilled contents, constrained movement of loads | Not everything that looks solid is destructible; no general house-building loop |
| Multi-cell entities | Bounded playable rollout | Shared occupancy, destructible scenery, moving pipe and large-creature pilot | Not universal conversion; orientation/zone-straddling limits remain |
| Regional requests | Playable first wave | Two templates, five authored bindings, stock/rewards/notes/release | No general event director or broad ecological recovery cycle |
| Conversations/storylets | Playable foundation | Conditions, actions, objectives and persistent state | Complete campaign architecture and all critical-loss alternatives |
| Factions | Partial | Reputation, some contracts, law and hospitality consequences | Uneven bespoke arcs, services and lasting regional aftermath |
| House Drama | Partial | Branching characters, disclosures and path state | Not independent evolving political simulation in every town |
| World exploration | Playable | Surface biomes, towns, caves, stairs, named vertical sites and map travel | Purpose and payoff vary substantially by destination |
| Voxel graphics | Extensive partial conversion | All six ordinary surface-biome families and many authored sites | Surface exceptions, ordinary underground and owner/state coverage gaps |
| Building blocks | Asset/toolkit | Modular models, prefabs and a demonstrative assembly | No established general player construction system |
| Spell visuals/audio | Implemented presentation | Starter-effect assets and layered audio integration | Full-game consistency, mix, accessibility and worst-case performance |
| Saving/loading | Playable foundation | Persistent entities/zones/notes and tested current-state round trips | Release-grade interruption recovery and broad compatibility policy |
| Knowledge/endings | Lore-rich, partially implemented | Readable places, several memories/flags and small enacted stories | No verified complete ordinary-play ending chain |
| Vein Pressure | Proposed, deferred | Design note only | No replacement pressure/wound/adrenaline system |

## 4. Combat, abilities and character development

Combat is already an actual system rather than an animation demo. Native attacks and skills interact with targets, defenses, damage, status effects and turn processing. The game has specialized skills across weapon and elemental families. Movement, geometry, hazards and preparation can change the value of an action.

The starting kit contains **Ember Spit, Flaming Hands, Jet Blast, Ground Surge, Rime Grip and Calm**. Their broad roles are distinct: a repeatable fire projectile, close fire offense, water setup, electrical payoff, cold control and pacification. The kit makes elemental combinations discoverable early instead of requiring the player to acquire several schools before the interaction grammar becomes available. The exact skill classes and current costs/cooldowns remain the authority; explanatory comments can lag behind expanded kits.

Kills award experience, levels grant development resources, and the skill screen enforces purchases and prerequisites. Equipment, consumables and crafting give additional routes to capability. This is a substantial basis for build expression. It does not yet prove that several builds remain viable from the first hour through an ending, that every attractive skill works well in ordinary encounters, or that investment choices avoid mandatory picks.

Release development should focus on concrete encounter questions: can the player recognize a dangerous windup; distinguish a resisted attack from a targeting mistake; use cover or terrain intentionally; retreat; protect an objective; recover something; or solve a problem without eliminating everyone? A different monster model is valuable, but repeated encounters need different tactical reasons as well.

The approved larger spell effects and layered sound should reinforce those decisions. Ember Spit has a distinct projectile/tail and subtler impact direction than Flaming Hands. A visually large effect must not misrepresent its actual area or conceal the next turn's state. The starter presentation work is not a certification of every spell, animation state, sound mix or target-hardware frame budget.

**Potential work:** clearer telegraphs; encounters with escape, defense and recovery goals; stronger noncombat preparation; build-diversity playtests; complete damage explanations; consistent resource/cooldown communication. These should extend the current combat grammar before replacing it.

## 5. Bodies, equipment, inventory and physical ownership

Entities are built from native Parts rather than being only visual objects. The architecture includes body/equipment state, damage-bearing actors, inventories, stackable items and containers. Many NPCs have authored equipment and carried goods. A character's model, a ground item's appearance, an inventory object and an equipped item are related but separate representations.

Native inventory commands are consequential infrastructure. A successful transfer must leave one owner; a rejected action must not consume an item, pay money, change quest state or report success. Recent regional work joins the existing outer inventory transaction so late failure rolls back cargo, stock, notes and completion together. Informational messages now wait until commit.

The current release failure set exposes one real visual identity gap: seven equipment sprite families were planned but never installed. In fallback rendering, a mace can borrow vial art, footwear/headwear can borrow torso armor, and some weapons share generic art. This is not evidence that 29 independent inventory mechanics are broken; 29 tests are blocked by a small unfinished presentation feature. The seven-family repair is in the R1 candidate, not the accepted main runtime at this document's starting snapshot.

Containers, dropped cargo, picked-up cargo and cargo stored in another chunk are distinct lifecycle paths. The completed regional wave now handles restored cargo references, including retrieval from a container after a town has already cached its absence. A failed retrieval still leaves the container authoritative. This is the kind of cross-system edge case that must remain covered as the game grows.

**Potential work:** audit all ground/equipped/corpse representations, explain weight and ownership consistently, smooth multi-step inventory actions, and verify interruption/reentry behavior across every release-critical service. Avoid adding a second parallel inventory just for quests or construction.

## 6. Environmental interactions and biome hazards

The game already supports meaningful interactions among materials, liquids, gases, coatings, damage and turns. Wetness can alter elemental tactics; burning or heating native material can create a secondary hazard; terrain affects movement and exposure. These are stronger foundations for systemic play than decorative hazard labels.

Examples with concrete current behavior include Grovelands mining/fire consequences, Beating glare/exposure, Sodden bog-mire and marsh gas, corrosive or rooting plants, and flora/fauna whose response depends on the actual type of contact. The point of a wetland detour is not merely that it looks different: a shorter path may change coating, damage, mobility and risk.

Keep the boundaries explicit. A generation variant called “receding water” is not evidence of a flood clock. A reclamation stage chosen at generation is not an evolving ecosystem. A predicate that reads `UrquActive` does not establish that normal play can cause a manifest period. A visual tree shadow is not automatically native shade.

The new Sumphold request connects a local environmental fact to a useful activity. It uses real peat/mire owners and native heat/gas behavior, and pays a modest optional preservation bonus only when its bound habitat remains intact. Missing, moved or damaged owners cannot count as untouched. It does not introduce habitat regeneration, global pollution, population recovery or offscreen simulation.

**Potential work:** a few observable ecological disturbances with bounded responses, clear warning language, understandable recovery or preservation choices, and selected revisit changes. Prefer a small causal loop the player can observe over broad world-state flags with no normal producer or visible consumer.

## 7. Destruction, multi-cell objects and construction

The physical-world architecture has moved beyond the original assumption that every visible object must be one cell. Shared occupancy queries and footprint-aware rules support the pilot's destructible scenery, movable three-cell pipe and two-by-two creature. Pathfinding, arrival, displacement, contact, targeting and duplicate exposure/damage have dedicated integration coverage.

The important invariant is a **single native owner with an explicit physical footprint**. Touching several cells of one object should not multiply one attack into several hits. Conversely, separate casts, different hazard families and legitimate repeated attacks must remain separate. Destruction must remove the real owner and its occupancy, not simply hide one mesh. Transfer failure must leave the entity in its original valid place.

This rollout has limits. It is not proof that every old skill, every unusual footprint or every newly authored large creature is ready. Physical orientation is fixed in the accepted pilot, and an entity does not simultaneously straddle multiple zone graphs. Existing ordinary single-cell behavior is an important countercheck, not something to discard during expansion.

The modular building-block kit is a different layer. It supplies wood/stone architectural parts and an editable assembly made from independent pieces. It does **not** establish a shipped player loop for acquiring blocks, previewing placements, validating access, constructing a house, paying resources and persisting destruction. The installed kit currently has a variant-publication defect; source outputs appear ahead of installed FBXs.

**Potential construction milestone:** place and remove a small native wall/floor/door family in one controlled chunk, with real inventory cost, footprint validation, access checks, undo/refusal semantics, destruction and save/load. Expand only after the complete loop works. Do not describe importing a prefab as implementing construction.

## 8. Crafting, gathering and food production

Players can gather finite resources and use several existing production systems. Weapon-component crafting, alchemy and tinkering are distinct routes with their own parts, recipes, materials or bits. This provides meaningful reasons to seek ingredients, visit services and choose what to carry.

The release question is not simply whether recipes exist. Can a normal player discover a useful recipe, understand its requirements, find the materials without developer grants, reach the station or service, and obtain a result that changes what they can do? Do important ingredients have interesting competing uses? Are failures and unavailable recipes explained accurately?

Farming is implemented but bounded. The player can plant seeds, water crops and advance growth while the zone is active. It is not an offscreen agricultural economy. The world also contains authored CropRow scenery, which is not the same thing as a player-planted CropPart. Recent work introduced sparse actual ripe rows and persistent spent states; it did not turn every visually cultivated cell into renewable food.

The distinction should remain visible in examination and available actions. A decorative field can communicate land use, but it should not misleadingly promise harvest. Conversely, a deliberately finite ripe row should have a clear result when harvested and should not silently refill because the player revisits or reloads.

**Potential work:** useful supply expeditions, better recipe discovery, explicit growth-time rules, a modest set of processing chains, and revisits tied to documented causes. There is no need to build a complete supply-chain economy before the existing crafting paths become rewarding.

## 9. Trade, rentals, repair and settlement services

Trading uses real goods and drams. Eligible traders restock according to an existing bounded policy. Morrowfast's traders were brought into that policy rather than being presented as permanently renewable without actual stock-table support. Regional deliveries now place the requested goods into actual merchant stock.

Other native services include grimoire copying, quartermaster rentals, paid rest, wells, repair sites and sanctuary support. Copying preserves the original grimoire. Rentals and returns have their own costs/refunds. Wellmeet's repair sites distinguish temporary magic, permanent manual repair and teaching. A shrine donation currently provides limited damage reduction; it is not a generic healing fountain.

Many settlements share these services. That makes them useful, but does not necessarily make them different destinations. A forge, a host, a copying service, distinctive goods, a faction rule or a route advantage should give players practical reasons to select a town. Town identity should eventually be visible in both preparation and consequences.

There is **no established general autonomous caravan economy**. Stock renewal is not proof that farms produce, caravans transport, shops price dynamically and households consume goods. Boat frames, toll rolls and counter paperwork do not imply operational transport or financial systems.

**Potential work:** selected shortages, deliveries that restore a named service, different preparation specialties, honest failure/loss rules and a small number of meaningful economic dilemmas. Start with bounded local state that can be tested; avoid making every town dependent on a new global simulator.

## 10. Quests, local situations and practical consequences

The game has native conversations with predicates/actions, quest objectives and persistent storylet state. Morrowfast contains several small authored errands and the completed cloth expedition. Other settlements have complete contracts and pilgrimages. These are not merely journal prose: some change objects, stock, reputation, hospitality or recognized state.

Six older generic stories previously appeared to repeat across villages while sharing global quest identities and related facts. The completed correction gives each story a canonical home. It does not pretend that prepending a town name to an ID creates fully independent quest instances.

The new regional layer is explicitly instance-based. It supplies two finite template families across five locations:

| Recipient town | Request/source | Actual choice and outcome |
|---|---|---|
| Morrowfast 3.6 | Orrit's iron; Grovelands 1.5 | Bring/trade one ChoirIron or mine locally and accept existing Choir law; real stock, eight drams and fire clay |
| Cinderhold 6.6 | Weaponsmith iron; Grovelands 5.6 | Independent instance of the iron supply pattern; completion does not close Morrowfast's work |
| Gantry 7.8 | Two Emberwheat; Spread 7.7 | Use finite ripe rows or outside goods; real food stock, six drams and a healing tonic |
| Sumphold 15.6 | Sealed lamp oil; Sodden 16.6 | Carry the exact consignment through actual wetland hazards; oil stock, reward and optional intact-habitat bonus |
| Wellmeet 8.16 | Sealed filter sand; Beating 8.17 | Carry the identified consignment while existing exposure/preparation rules apply; useful stock and reward |

The player uses **C** to read, deliver or release work and **Q → Tab** for Field Notes. Instance identity includes the world seed and binding. The source is finite; destroyed cargo is not recreated on acceptance or reload. Bought/carried outside material is valid where the supply contract allows it. The actual named recipient and native inventory ownership remain authoritative.

These bindings are authored on fresh generation. A new world is required to guarantee all of them; already cached towns are not migrated. Persistence of new work is tested even though legacy save migration is out of scope.

**Potential work:** more consequential responses, fewer near-identical errands, refusal that is understandable without becoming punitive, service restoration, discoveries that enable new actions, and a director only if a small manual set proves the desired gameplay. The current wave is a starting vocabulary, not a complete regional-story system.

## 11. Factions, hospitality and social state

Reputation and faction-sensitive behavior exist, as do several authored social choices. Grovelands law makes extraction and fire different from harmless gathering. Cinderhold's pruning writ trades Choir standing against Concord reward. Guest-right has real duration and protection rules. Catacomb hearth damage carries genuine consequences. Olderdeep links a material offering, trust, a particular place and resting to remembered recognition.

These working examples should guide future faction development. A faction is more compelling when it offers a useful but costly form of care, preservation, exchange or belonging than when it is simply a differently colored reputation bar. The world already has the language for incompatible obligations; release content needs to enact them.

House Drama includes branching narrative state, disclosures and path choices, but repeated casts do not establish independent ongoing campaigns in every town. An end-state evaluator or narrative definition is not proof that a settlement currently transforms in response.

The lore's release triage is useful: **five deep factions, two mid-depth factions and three background factions**. It is a scope strategy, not a checklist of ten equally large campaigns. The deep work centers on the Rot Choir, Recension, Pale Curation, Tent-Right and catacomb villagers; Concord and Bower receive mid-depth treatment; Bloom, Catchers and Inkbound remain more limited for the scoped release.

**Potential work:** at least one complete middle-game faction chain, alternatives for changed allegiance or lost actors, recognizable access/service changes, and a coherent record of enacted commitments. Do not add repeatable reputation rewards without testing whether they can be farmed to trivialize the intended choice.

## 12. World structure, navigation and voxel coverage

The authored surface uses **400 coordinate slots**. Ordinary composition systems exist for all six surface biomes: Grovelands, Spread, Beating, Sodden, Stump and Overwrit. Named settlements, reserved scenes and sinkholes can override ordinary biome treatment. A generic biome renderer supporting an address does not prove that every special owner in it has an appropriate model.

The latest completed accessibility inventory recorded **386/400 surface addresses within the voxel selector**, with 14 explicit exceptions. It also recorded eight converted named lower levels: depths one and two under Ginmere, Cathedral, Stillleaf and Olderdeep. Ordinary underground remains the largest existing-content conversion gap. These are dated source/dispatch counts, not a fresh visual certification of every chunk.

Towns genuinely exist and can be reached. The earlier report of roaming without finding one was investigated against a copied save: all 17 named villages were present; none of the player's 24 cached ground chunks was a town. Navigation help was incomplete. It now explains that surface **< / Shift+comma** opens the world map, **> / Shift+period** enters a selected chunk, and named settlement markers use **!**. Guidance resolves real destinations rather than inventing coordinates.

The accessibility audit exercised actual edge transfers, map descent and reciprocal stairs. Existing saved graphs can still differ from a newly generated chunk because destruction and earlier generation persist. That is distinct from a routing bug.

Fourteen remaining surface exceptions are Sill, Posy, Slip, Salt-Vault, Quiet's Door, Lampwell's mouth, Spivenor's mouth, the Root reservation, Felling-Site, Unsaying, Woven Doll grove, Tenth Fire and two abandoned Counter sites. Some have older 3D presentation; “not converted to this voxel system” does not mean “absent from the game.”

**Potential work:** reusable underground depth-band grammar; small truthful treatments for the restrained mysteries; native service-preserving village conversions; complete owner/state coverage ledgers. Avoid handcrafting hundreds of caves independently or turning an unimplemented lore destination into a misleading decorative substitute.

## 13. Biome-by-biome player experience

### Grovelands

This is among the strongest ordinary regions because environmental identity already connects to choices. Water, native trade, gathering, ChoirIron, composting remains, plants and local law can change a route or technique. Mining costs Choir standing; player ignition can be substantially more provocative. The new iron requests give a practical reason to consider extraction versus outside supply.

What remains weak is repeated purpose. Another grove formation is not automatically another decision. Generated reclamation appearances are not a changing regional ecology. Preserve its useful systemic interactions while adding a few clear, locally consequential needs and discoveries.

### Spread

Spread provides roads, fields, hedges, foraging and approachable travel. It can serve as connective country and an introduction to ordinary gathering and farming. Sparse finite ripe rows now address part of the old visual promise mismatch.

Most field composition remains land-use scenery rather than a simulated agricultural system. Release improvement should make the distinction legible and attach selected fields to real supply/maintenance decisions. Quiet transit is valuable; not every parcel needs a quest, but repeated parcels should not all promise the same inaccessible harvest.

### Beating

Beating has a real preparation and route-planning problem through Height exposure, headgear, interior relief, water and remedies. Pale Salt and native hazards supply reasons to visit. Wellmeet's new recovery request uses those existing rules.

The signature pressure is heavily mitigated by suitable head equipment. That can be a satisfying preparation payoff, but then the region needs additional choices beyond a solved exposure tax. Solid dunes are not automatically climbable; visual cover is not automatically shade; salt crust should not be advertised as damaging unless it has that native behavior.

### Sodden

Sodden's dry banks, wet shortcuts, coatings, corrosive plants, frog responses, MawToads and peat gas make terrain consequential. Carrying valuable cargo gives those approaches a useful purpose.

High/low/receding water currently describes generated conditions, not an evolving flood model. Bodies, boats and work sites often remain examination rather than recovery/transport industries. The next good extension is a bounded disturbance or recovery with clear alternatives, not a world-wide water simulation introduced to justify the art.

### Stump

The Stump has elevation bands, geology, endemic creatures, named descents and a strong ecological identity. Brocchinia-Sentinel retreat is real. Recent interaction work made appropriate bromeliad water drinkable and spray pools participate in native water/contact behavior.

Several evocative promises remain incomplete. Singer silence is not established as a complete alarm system. `UrquActive` and `EcologyDamaged` spawning predicates do not have a verified ordinary production loop in the audited world. Descent-ledge imagery is not a general climb action. The right next work connects observation to an action or consequence without falsely announcing a global manifest-period cycle.

### Overwrit

Overwrit's absence is intentional atmosphere, but the audited seed had 22 of 23 surface chunks dominated by ground/new growth and occasional stairs, without ordinary resource, trade or conversation opportunities. Unsaying's legacy content is an exception, not a finished cultural settlement.

The answer should preserve quiet while giving a few absences meaning: a trace with an actionable implication, a return that has changed for a reason, a deliberate refusal, or a discovery that matters elsewhere. Memory bleeds and a full name-unsaying practice remain future work. Filling it uniformly with monsters and crates would erase its identity without solving its dramatic problem.

## 14. Settlements and significant destinations

There are 17 authored surface villages and three inhabited catacomb floors in the completed census. That is not 20 equally developed campaigns. The following examples distinguish the strongest current activities from their larger promises.

| Place | Current practical identity | What should not be advertised as complete |
|---|---|---|
| Morrowfast | Bespoke residents, cloth expedition, bell/stool/consent work, trade, rest and new iron request | A complete first act or dynamic resident emergency simulator |
| Sill | Existing shop/service package, witness/stump errands and repairs | The current fresh-game spawn |
| Cinderhold | Forge/weaponsmith, pruning writ with faction tradeoff, iron supply | Mining management or a full Concord campaign |
| Wellmeet | Guest-right, salt exchange, repairs, desert recovery | General regional water-economy simulation |
| First Tent | Oath, water and shelter preparation | A unique trial or all of Wellmeet's repair systems |
| Gantry | Exchange/rental/copy/rest junction, host, directions, grain supply | Caravan hiring or a paperwork dispute simulator |
| Tine | Shoreline village with ordinary services | Fishing, operable boats or a completed retired-scribe arc |
| Sumphold | Wetland services and lamp-oil recovery | Boat construction, toll operation or full body-trade industry |
| Quillhold | Useful shelf containers, texts, ink, copying and trade | Reader audience and a complete archive faction quest |
| Tally / Last Counter | Exchange/frontier provisioning and ordinary services | Debt-clearing institution, guarantees or hired caravans |
| Drowned Ledger → Marrowstye | Actual sealed body-parcel delivery to the native clerk | Full preservation/body-reading economy |
| Olderdeep | Tepuibone/trust/founding-plume/rest pilgrimage with remembered recognition | A substitute for every catacomb settlement's missing story |
| Lampwell / Spivenor | Hearths, plaques, wardens/tenders and existing local law | Hatcheries, distinct admission policies or native shop economies |
| Posy / Salt-Vault / Slip / Quiet's Door | Reachable villages with shared services and faction context | Their much larger lore-specific institutions |

Deepest Cathedral is a real multi-level Choir destination with inhabitants, dialogue, trade, combat and architecture. Its great-node art does not establish a travel verb or the Wedded's completed audience/original-body arc.

Stillleaf is reachable, but the normal acquisition/reading chain for the sealed archive is not established. Synthetic keys in tests are not player progression. Felling-Site has the six positions, empty seventh and local confusion effect, not a finished god ceremony or ending.

Ginmere supplies a drowned sima and genuine nest disturbance, including the possibility of a hostile swarm. Its Helmwood passage is a seed-selected shortcut to an existing floor, not a guaranteed additional destination. Woven Doll and Tenth Fire can remain small mysteries; they do not need explanatory quest rewards to justify their presence.

The earlier faction/sacred-POI census found 124 qualifying chunks in one seed, or 127 including Ginmere as an ecological destination. Those are deduplicated physical chunks, not 124 unique adventures. Occurrence counts are useful for reach and distribution, not as engagement scores.

## 15. Lore, knowledge and the campaign that is still missing

The lore is far ahead of the complete enacted campaign. That is an asset and a risk: rich concepts can guide coherent mechanics, but can also make scenery and conversation sound more operational than they are.

The release vision should preserve several firm constraints:

- Urqu is pressure, not a scheming villain whose final monologue explains everything.
- The closure-ledger distinguishes carried-through, deliberately refused and silently abandoned acts. A principled no can close an obligation.
- Protected questions in the Mystery Ledger remain unresolved by authoritative ending text or a collectible “true answer.”
- Selen's name is setup for giving, bargaining or withholding; the climax is an enactment, not an ordinary fetch-item turn-in.
- Naro can be interpreted and acted upon without developer-certified certainty.
- Campaign scope follows the existing v1 dramatic triage rather than attempting every lore paragraph at equal depth.

A complete ordinary-play chain through knowledge acquisition, god contact/agreements, closure state, final action and epilogue has not been verified. Existing flags, dialogue, architectural sites and lore descriptions do not prove that chain. This is a central release gap.

The next campaign milestone should be one connected middle-game journey: a real settlement need, a reason to descend or investigate, a preparation decision, conflicting testimony or loyalties, an enacted choice and a visible aftermath. Test it with different builds and action orders, including refusal and losing a participant. Only then multiply large arcs.

Ending development needs explicit mechanical clarity without cosmological certainty. The player should understand what they are committing to, even when the world's ultimate truth remains uncertain. Optional content must not silently become a universal obligation; cheap errands must not wash away unrelated abandonment.

## 16. Presentation, sound, controls and accessibility

The voxel direction is now a broad visual system rather than one attractive demo. Coarse shapes and restrained palettes have replaced increasingly noisy detail in many areas. Morrowfast's latest restyle supplies broad walls/roofs, quieter ground/routes and simpler furnishings while keeping native doors, interior ownership, actor rigs and interactions.

For that scenery wave, unique model vertices dropped from 272,856 to 8,832. This is an asset statistic, not a placed-frame total or a measured FPS improvement. Bulk variants and material restraint should serve legibility rather than become arbitrary numeric targets.

Some older detailed 3D scenes and fallback sprites remain. Ground items, corpses, state changes, reskins and special POIs need individual coverage checks. A shared material or one generic body should not conceal an identity mismatch. The first release repair therefore retains honest glyph fallback when a specifically mapped equipment sprite is unavailable.

Starter spell visuals and layered audio are integrated work. The broader release still needs a coherent mix, distinct cues, readable effects at the chosen zoom, appropriate variation and testing under simultaneous effects. The codebase and assets do not establish that all sounds are licensed/documented or that every content family has complete audio; a release provenance/mix audit is a separate gate.

Controls, targeting, inventory, journal and world navigation need newcomer testing. Useful directions and quest markers now exist, but discoverability is not finished because a help page lists the right key. Release accessibility should cover remapping, text scale, contrast, non-color cues and audio controls. Do not imply all those options already exist.

## 17. Persistence, technical foundation and release quality

The entity/Part/event architecture supports saved world graphs rather than regenerating a fresh world every visit. Current-save tests cover important identity and lifecycle behavior: destroyed sources remain gone, delivered work does not repay, notes survive, and native cargo ownership survives travel and reload.

The user explicitly does not require legacy save migrations. That removes a compatibility burden; it does not excuse broken saves created by the new build. Reliable ordinary saves, interruption handling, graph reconstruction and clear failure recovery still matter for a persistent RPG.

The test suite is large and valuable, but its size is not a release metric by itself. Tests can miss input dispatch seams, as the regional C menu demonstrated. They can also retain obsolete assumptions, as the Sill table-order and plain-Morrowfast pins demonstrate. Preserve useful assertions and fix the cause rather than treating all failures as either production defects or disposable old tests.

Recorded debt includes Wall-Catching blockers, line-of-sight interruption behavior, follower reputation laundering, descent/climb cost, self-describing save-field work, summit cloud presentation and other deferred W6 concerns. These are recorded issues to triage against the release path, not discoveries to repeat or invisible promises to count as fixed.

The RS27 profile recorded approximately 3.469 ms average Main Thread time with a 440.502 ms maximum; input averaged 0.191 ms with a 417.469 ms maximum; renderer LateUpdate averaged 1.225 ms with a 26.833 ms maximum. These uncapped Editor observations include other work and do not establish stable frame delivery. The large retained spikes warrant investigation before performance claims.

Release quality also needs packaging, supported-platform checks, first-launch behavior, audio/asset provenance, controls, crash/save recovery and an explicit known-issue policy. This review does not establish a shipped installer, a tested platform matrix, a final license ledger or completed external playtesting.

## 18. Potential mechanics: priorities rather than a wish list

| Proposed direction | Why it could improve the game | Smallest useful implementation | Why it is not already available |
|---|---|---|---|
| Service-restoring regional work | Makes delivery visibly useful | One request changes a real service or selected stock/access state | Current stock/reward delivery is only the first step |
| Ecological disturbance response | Makes hazards and preservation matter on revisits | One local cause, persistent evidence and bounded response | Generated stages and predicate readers are not a complete cycle |
| Actionable archive knowledge | Gives lore a practical consequence | One normally obtainable record enables a real route/introduction/choice | Stillleaf's acquisition/reading spine remains incomplete |
| Faction commitment chain | Connects existing towns into a campaign | One complete choice with alternative/recovery paths and aftermath | Current local contracts do not form a full middle game |
| Closure and ending enactment | Gives campaign decisions a destination | One complete ordinary-play ending route before expansion | Lore designs and flags do not establish entry-to-credits play |
| Player construction | Makes modular blocks a mechanic | Small native placement/cost/removal/save loop | Kit/prefab assembly is authoring infrastructure |
| Underground voxel grammar | Brings existing deep gameplay into the style | One depth band with faithful owners, hazards, stairs and variants | Ordinary caves remain largely outside current conversion |
| Selected revisit evolution | Gives changed places reasons to return | A handful of causal service/ecology/story changes | Persistence usually retains the same looted/damaged state |
| Transport/fishing/economic industries | Could deepen some settlements | One justified loop tied to existing native objects | Boats, shoreline art and paperwork currently overstate these possibilities |
| Vein Pressure | Could make damage state a preparation gamble | A separate bounded prototype with observable pressure states | It is deferred design, not an implemented damage-model replacement |

Vein Pressure deserves special restraint. The proposed design makes wounds leak systemic pressure; low pressure trades offense/stamina for clotting, coldness and slower toxin spread, while high pressure gives power/speed at the risk of catastrophic bleeding and rapid poisoning. That is an interesting concept, but it touches many established systems at once. The user asked to return to it later. Do not silently replace HP or block a release on it; prototype it separately only when explicitly prioritized.

## 19. Release work started at this handoff

The accepted main runtime still has the exact 32 RS28 failures. R1 work is in the independent copy at `/tmp/coo-regional-verification-20260917`, with live hashes preserved in `/tmp/coo-release-r1-freeze.json`.

The failures are three repair groups, not 32 unrelated engine bugs:

1. **Twenty-nine equipment sprite cases:** seven missing PNGs/preload/exact routes block the intended presentation contract. The candidate now contains seven authored 16×16 sprites and ten blueprint mappings. Existing controls, visibility/tint/dirty-cell behavior and honest missing-resource fallback remain required.
2. **Two stale world expectations:** Morrowfast and four other villages now have explicit profiles; Sill does not have to be first in the place array. Candidate tests preserve exact authored profiles, ordinary-village controls and Sill's address/biome/tier. They do not change the configured spawn.
3. **One building-kit variation assertion:** six installed families have fewer than four distinct mesh/UV variants. Independent Blender reimport reproduced the installed deficiency and found the source outputs already contain distinct variants. The repair should publish the source kit, not weaken uniqueness or hand-edit one mesh.

The first R1 focused run, **R101**, ran 79 cases: **72 passed, seven failed, zero C# errors**. It intentionally included the seven new sprites and exact routes while excluding the proposed missing-resource guard. The seven failures now reach the real fallback branch, rather than failing at missing-registration preconditions. The guard has subsequently been restored in the candidate for GREEN verification. This is intermediate evidence, not a claim that R1 is complete.

**Final checkpoint, 17 September at 22:05 CDT / 18 September at 03:05 UTC:** R102 imported the regenerated 72-piece building kit successfully with unchanged metadata. The subsequent **R103 full candidate suite passed 14,901/14,901, with zero failures, zero skipped tests and zero C# errors**. This resolves all 32 inherited failures in the isolated candidate. The [R103 receipt](Verification/VoxelWorld/R103-release-full-candidate/receipt.json) and compressed XML/log preserve the actual run, which took about 390 seconds. These are candidate results, not a fresh green claim for unchanged main Assets.

Two observational checks were also added to the candidate native journey: the opening must start vulnerable, and the cloth expedition plus five interiors must finish alive without F12 or synthetic healing. They compiled with R103 but have **not yet been executed in the native journey**. The longer regional section continues to label its later F12 use honestly.

The original ground-sprite plan additionally requires a dedicated before/after native sprite workload and visual inspection. Existing equipment benchmarks measure combat/loadouts; they are not a substitute. That dedicated bench has not been completed. Its acceptance, the updated native opening, imported-art visual inspection and final cold-eye review remain outstanding. The [candidate archive and manifest](ReleaseHandoff/README.md) preserve 99 draft files without installing them into live Assets; the [handoff prompt](RELEASE-AGENT-PROMPT.md) records the exact next steps.

## 20. Recommended release sequence and definition of done

**First: make the baseline trustworthy.** Finish R1, preserve adversarial tests, obtain a fresh full-suite result, and verify the actual normal opening. Classify any remaining failures individually. Do not use “unchanged from baseline” as release approval.

**Second: make the regional loop worth repeating.** Playtest the five requests and improve their consequences or variations before adding many more. Keep quiet travel. Make costs, ownership, sources, refusal and aftermath understandable.

**Third: connect one full middle-game chain.** Use already reachable towns and vertical sites. Give knowledge a use, preparation a role, and faction commitments consequences. Establish ordinary acquisition and failure recovery before multiplying arcs.

**Fourth: implement the ending spine.** Define undertaken/refused/abandoned semantics; complete one enacted ending route; add the remaining advertised routes through the same robust state structure. Protect lore uncertainty while making mechanical commitments clear.

**Fifth: finish scoped breadth.** Complete the chosen faction depth, needed underground/owner art, useful services and build progression. Defer features whose main effect is to make the project larger rather than the player's next decision better.

**Finally: certify the actual product.** Test unfamiliar players, representative builds and seeds, normal saves, accessibility, audio, worst-case performance and clean installations. Every advertised ending needs a normal entry-to-credits playthrough. Every advertised industry, interaction and travel mode needs an actual player path. A release needs a bounded known-issue list and a reproducible build, not merely many passing tests and attractive screenshots.

The desired player account is concrete: “I needed something, understood enough to choose an approach, used my abilities and relationships, saw what changed, and had a reason to continue.” The current game already provides examples of that experience. Release work should connect and deepen them.

## Sources and maintenance

Primary project identity: [PROJECT-IDENTITY](PROJECT-IDENTITY.md). Runtime/task verification: [Morrowfast and regional situations](MORROWFAST-STYLE-AND-REGIONAL-SITUATIONS.md), [release stabilization](RELEASE-STABILIZATION.md), [main integration](MAIN-INTEGRATION-2026-09-17.md), [RS28 comparison](Verification/RegionalSituations/full-regression-comparison.json), [RS27 native receipt](Verification/ChunkGameplayImplementation/RS27-final-native/receipt.json).

Current-world analysis: [chunk gameplay implementation](CHUNK-GAMEPLAY-IMPLEMENTATION.md), [dated chunk audit](CHUNK-GAMEPLAY-AUDIT.md), [canonical quest homes](CANONICAL-VILLAGE-QUESTS.md), [regional guidance](REGIONAL-GUIDANCE.md), [accessibility](VOXEL-WORLD-ACCESSIBILITY.md), [conversion backlog](VOXEL-CONVERSION-BACKLOG.md), [faction/POI census](FACTION-AND-SACRED-POI-AUDIT.md), [multi-cell pilot](MULTI-CELL-PILOT.md), [building blocks](BUILDING-BLOCKS-3D.md), [equipment sprite plan](EQUIPMENT-GROUND-SPRITES-PLAN.md).

Design authority: [Bible](../Lore/10_Bible.md), [Second Spine](../Lore/11_SecondSpine.md), [Mystery Ledger](../Lore/MYSTERY-LEDGER.md), [v1 dramatic scope](../Lore/Design/V1-DramaticCore.md), [release vision](RELEASE-VISION.md), [release-vision evidence](RELEASE-VISION-EVIDENCE.md), [deferred Vein Pressure](VEIN-PRESSURE-DESIGN.md).

Read older audits as dated snapshots. Later implementation can resolve a listed gap—drinkable bromeliads, actual spray water, sparse ripe rows, guidance, stock renewal and cues are examples. When updating this document, trace the current native action and content placement, then cite the newer executable evidence. Never infer completion solely from a class name, an asset, a test fixture or a lore paragraph.

## Subsequent checkpoint — 18 September 2026

This document remains the detailed dated inventory. Its32inherited failures have since been repaired in R1 and promoted to local main; see [R1 acceptance](RELEASE-R1-ACCEPTANCE.md) for exact full-suite, timing-flake retry, native vulnerable opening, equipment art and block import evidence. This is stabilization of the existing game, not implementation of the potential campaign/ending systems described above. R2 regional receipts are the next release slice.

### R2 accepted — 18 September 2026

Completed regional work now retains a truthful payment/stock/habitat receipt; released notes no longer advertise active delivery. All five finite bindings are covered, including rollback and full saves. Full suite14,931/14,931, native62/62, zero compiler errors; actual post-reload receipt and trader stock were inspected. See [RELEASE-REGIONAL-OUTCOMES](RELEASE-REGIONAL-OUTCOMES.md). No new economic simulation or ending spine is implied.

## 18 September checkpoint: fullscreen UI ownership repair

Trade and Faction now paint the main fullscreen canvas; pausing for Inventory/Journal/Trade/Faction discards stale terrain snapshots rather than restoring them over empty menu rows. Ten paired regression cases, 107 focused checks, 65 native checks with 21 captures, and full R117 **14,941/14,941** passed with no compiler errors. Root inspected clean trade/faction pages and restored voxel world. No gameplay ownership, inventory, camera preference or save-format changes. See [repair and evidence](RELEASE-FULLSCREEN-UI.md). Next: explicit opt-in regional requests.

## 18 September checkpoint: explicit regional opt-in

Reading any of the five regional offers now previews actual goods, quantity, recipient, reward and known/unknown source status without accepting, generating a distant chunk or altering notes. A separate Accept action records the undertaking. Released notes survive reading until explicit reacceptance, and unavailable recovery previews do not advertise unavailable actions. Existing exact cargo, outside-goods, habitat, transaction and historical receipt behavior stays verified. New21-case matrix plus an extra stale C-accept control; focused110/110, native70/70 with23captures, fullR123 **14963/14963**, zero compiler errors. See [plan and acceptance](RELEASE-REGIONAL-OPT-IN.md). Next: useful material descriptions in Inventory.
