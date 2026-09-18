# Morrowfast: the Stillcord settlement south of the Felling

Status: **new content proposal and production design; generated source inventory reconciled**, 2026-09-06. This document specifies the settlement requested by the user. It does not amend canonical lore and does not claim that the proposed NPCs, quests, shops, or access rules are implemented in Unity. The accompanying scene and component package are the first production deliverables; their final manifest is the authority for actual image coordinates and extracted objects.

Morrowfast is a small inhabited watch settlement at world **(3,6)**, immediately south of the existing Felling-Site at **(3,5)**. Its people make a living from the dangerous road: they mend equipment, feed visitors, keep beds dry, and go looking when somebody fails to return. Their gate is held by **the Stillcord**, a local fellowship of several families. They have gradually begun deciding who is ready to go north. The player meets people doing necessary work whose authority has grown beyond the work that earned it.

The first impression is a warm, crowded pocket below an enormous, indifferent mountain: mossy ochre roofs edged with pink stone, two cloth market awnings, rescue cords and a small patient tortoise beside the bread oven. The north road remains visible from the southern arrival. The Felling itself, its circle, and the seventh position are outside this image. Steam, drying laundry and a loaded pack-animal are later polish proposals, not features claimed to exist in the generated reference.

## Produced art inventory

The inspected [reference](../ArtSource/Morrowfast/reference.png) is 1536×1024. It contains **five houses, an open entrance arch, two separate outdoor market stalls, six visible adult residents, one creek-bank frog and one tortoise**. The western house is the guesthouse; its inferred interior has **two independent beds**, not the three proposed before generation. The northwest house is the keeper's house, northeast is the archive, southwest is the rope shop, and southeast is the kitchen. All five original house doors face south. The arch has posts and hanging ornaments, with a walkable central opening; no hinged gate leaves were generated.

The [building authoring](../ArtSource/Morrowfast/authoring/buildings.json) and [interior inventory](../ArtSource/Morrowfast/authoring/interior-objects.json) record the inspected source boundaries and **21 individual interior owners**. Original visible masonry and door positions are retained when roofs lift. Generated interior furnishings are reconstructed designs, not discoveries about unseen original pixels. Small furniture placement offsets keep the original entrances and open floor usable. The eight named residents below remain the narrative cast proposal; six visible source figures can take the first six roles, while Farra and Edden need later actor placement/art. Single-pose extracted animals do not yet constitute native animated creatures.

## Authority and the limits of the proposal

The older [Docs/Lore/v2 index](Lore/v2/README.md) calls that directory a parallel redesign. It is not the Felling setting's authority. The current chain is [Lore/README.md](../Lore/README.md): [the Bible](../Lore/10_Bible.md) §IV, amended by [the Second Spine](../Lore/11_SecondSpine.md), with [the Mystery Ledger](../Lore/MYSTERY-LEDGER.md) protecting questions from resolution. Phase-history and faction documents apply where consistent with that chain.

| Established material | Use here | Boundary |
| --- | --- | --- |
| [Felling world design](FELLING-WORLD-DESIGN.md) §§2.2, 3.6: Felling-Site (3,5), Olderdeep (4,6), root-slope geology, blackwater creeks, pink-grey petrified wood grain | A foothill settlement occupies the previously unnamed chunk south of the Site; a small eastern footpath points toward Olderdeep | Morrowfast is a proposed local addition, not a previously attested canonical town. It does not move either named place. |
| [Geography](../Lore/History/02_Geography.md): journeys and cross-tier shortcuts matter; the Felling's seven positions are specific physical locations | The road, side crossing, southern escape, and adjacent chunk transitions are actual routes | No new supernatural barrier or retroactive lock on existing Felling access. |
| [Material culture](../Lore/History/08_MaterialCulture.md): everyday timber, stone and thatch; faction identities expressed through objects | Domestic roofs and utensils beside a small Recension desk and a visiting Bower arrangement | A mixed settlement is not a new god-founded empire. Its households retain individual beliefs. |
| [Root-slope bestiary design](../sarisarinama_bestiary_design.md), existing `GlasspaneFrog` and `YellowfootWayfarer` content | Creek frogs and a pack tortoise inhabit suitable wet-bank and dry-verge cells | No invented ecological role for Gin Frogs; no summit species transplanted to a lowland market merely to fill space. |
| [Voice cards](../Lore/Voices/VOICE-CARDS.md): Recension attribution; Bower attention to placement | Vennit's and Farra's sample dialogue follows those registers | The local Stillcord speech card below is a proposal. It is not silently added to the canonical card set. |
| [Mystery Ledger](../Lore/MYSTERY-LEDGER.md) §§1–3, 7–9 | Contradictory visitor accounts can coexist | No answer to why Naro refused, no account of the Root's other dreams, no assertion of what the Bower sees, no mapped prior world. Urqu receives no intent. |

The Stillcord's ropes and bells are practical equipment. They are not a new anti-Urqu field, a magical naming system, or a duplicate of Tent-Right's three-day hospitality oath. There is no seventh-shaped emblem, secret seventh founder, numbered circle of buildings, or quest reward that decodes the Felling. The watch has two people at the gate and one relief worker; it is not an army occupying the Felling's convergence of factions.

## The Stillcord

**Local name:** the Stillcord. **Settlement name:** Morrowfast. **Proposed internal faction ID:** `Stillcord`; initial player reputation 0. This is a design identifier, not a registered faction yet. Suggested reputation span uses the existing engine's tiers, rather than a new economy.

They originated as neighbors who shared a rescue line after repeated slips on the upper road. Membership now requires taking an ordinary watch shift, learning the crossings, and contributing work to the common equipment. Adoption into a household is common; membership is neither hereditary nor restricted to a species. A short pale cord worn loose at the wrist means the wearer is on shift. It is removed at supper. The slack matters because a worker needs to be able to take it off.

Their public promise is concrete: maintain a clear road, warn visitors of observed hazards, and attempt a search when agreed. Their disputed practice is also concrete: Captain Hesta has begun delaying visitors she judges unprepared, and Vennit's voluntary return record is starting to look compulsory. Some residents want a stronger gate after recent rescues. Others think keeping people dependent on Morrowfast is becoming a business.

The fellowship has no policy toward the cosmic endings and no special evidence about the Site. Individual members may disagree with every doctrine the player brings. Reputation comes from paying agreed debts, protecting residents, returning loaned goods, repairing public equipment, and respecting an explicit refusal. It is lost through witnessed theft, attacks, or knowingly abandoning an accepted rescue obligation. Declining an offered job is an ordinary closed conversation, not a moral failure.

**Proposed local speech card:** short practical sentences; ordinary place and tool names; ask what happened before explaining what it means. Humor comes from work, muddle, and familiar neighbors. Avoid prophetic riddles and abstract lore speeches. A guard can say, “That board is loose. Step over it.” They do not say, “The threshold remembers those who cross.”

## Layout and visual direction

Use an **above, top-down orthographic view** of the entire chunk. Keep all ground scale consistent. Shallow roof thickness and small wall rims may clarify buildings, but do not turn the image into an isometric diorama. North is the image top. Match the Felling's muted pink sandstone, dark olive ground, worn cobbles and dense small texture. The produced settlement has warm ochre-and-moss roofs with stone cap borders, green/ochre and violet awnings, pale cords and small amber hearths. Sparse cyan plants and additional domestic cloth are optional later dressing.

The percentages below now describe the generated composition approximately; exact pixel masks, doors and collision anchors live in the authoring JSON. Do not force these overview rectangles onto a painted object.

| Area | Approximate image area | Composition and use |
| --- | --- | --- |
| North approach | x42–59%, y0–24% | Open road aligned with the Felling's southern approach. Two arch posts with hanging ornaments frame the clear central opening; two keepers stand to either side. No invented gate leaves. |
| Gate House | x28–39%, y6–26% | Small northwest house, with a bench, supply chest and duty desk in the inferred room. The original door faces south at pixel (552,263). A packed rescue sled is a future quest asset. |
| Guesthouse, **The Dry Hem** | x18–36%, y32–57% | Largest roof, south-facing entrance and flower trough. The inferred room contains two independent beds, a warming stove, table and three independently owned stools. Laundry and drying boots remain later dressing proposals. |
| Witness cottage, **The Return Desk** | x69–82%, y6–29% | Northeast dwelling with two record shelves, a writing desk and storage barrel in its inferred interior. Original south entry remains at (1149,295). |
| Cookshop, **The Second Bowl** | x77–90%, y63–86% | Southeast kitchen has a hearth, preparation table and two separate produce barrels. Its outdoor stove and chopping block are independent props. The separate violet food stall stands to its west. |
| Mender's home and shop, **Orrit's Long Loop** | x20–36%, y64–86% | Southwest house with an interior workbench, reserve cord and barrel. The source also provides a separate outdoor worktable, handcart and ground coils. Original door faces south. |
| Two market stalls | x60–74%, y40–85% | Green-and-ochre provision stall in the east-center and violet food stall farther south, each with independently owned surrounding containers. They supplement the five houses. |
| Southern arrival | x39–65%, y83–100% | Clear south road with the tortoise west of the lane, beside the separate stone bread oven. A pack shelter and trough were not generated and remain optional later additions. |
| Public rain cistern | x47–58%, y41–58% | Circular utilitarian stone well/cistern with dark water and a separate bucket. It has no numbered stations or ritual markings. Keep circulation on both sides. |
| Creek and western footbridge | x0–18%, y0–100% | Dark water continues the Felling stream downstream; irregular banks, scattered stones, one short pedestrian bridge west of the guesthouse. No hazardous stepping-stone route is the only exit. |
| Peripheral paths and garden | edge verges | Moss, edible fungus beds, occasional flowers, repaired drainage and a small Bower display. Preserve readable margins and a narrow eastern path toward Olderdeep. |

Reserve a generous central circulation loop. Roofs should have air between them; house massing, stalls, signs and creatures must not merge into a single silhouette. The image can be richly detailed without making every ground pixel compete for attention. Lighting is diffuse and directionally consistent. Bake only contact contributions that can be owned and removed with their components; do not bake bright supernatural illumination into hidden ground.

The produced image shows six adults outdoors: two entrance keepers and four residents working or standing around the village. Later schedules can put some indoors; do not fill every room and stall with duplicates of the same source person. One frog and one tortoise are the produced wildlife inventory. Any second frog or pack-animal equipment is a separately authored addition.

## Eight residents

All eight named residents are adults. These names were checked against the current local lore/content for exact collisions; they are new proposals.

| Resident | Place and role | Wants, friction, and example line | Interaction |
| --- | --- | --- | --- |
| **Hesta Brack** | Gate captain; one of the gate-side watch positions | Wants fewer preventable rescues; has started treating caution as permission. “You can go up. First, tell me which way you mean to come back.” | Talk, hear observed hazards, register or decline a return plan, discuss access, de-escalate disputes; native combat if attacked. |
| **Nemm Hushbell** | Relief watch; west gate post and notice rail | Has a loud voice but wraps the bell because the clapper startles the frogs. Resents being treated as an apprentice. “I can shout farther than that bell carries. Orrit says that isn't the point.” | Talk, demonstrate alarm choices, inspect cord wear; may witness the bell quest. |
| **Orrit Coilstitch** | Mender and tool seller, southwest | Wants his work paid for; gives away fixes he believes somebody cannot safely leave without. “You brought me two ends. That's nearly a rope.” | Trade existing equipment; examine workbench; proposed repair and rescue-line service; return or settle loans. |
| **Pell Drowse** | Guesthouse keeper, west | Wants people to sleep; collects abandoned boots and insists every pair is temporary. “Dry socks first. Tell the story after you've stopped dripping on it.” | Rent bed, discuss board, access lawful storage, return lost property. Rental is a proposal until the native service is authored. |
| **Sella Kettle** | Cook, southeast | Wants the public table kept open despite costs. Calls every new recipe yesterday's soup. “Sit where you like. The tortoise has already tried that chair.” | Trade food and tonics; buy or contribute ingredients; use the public water fixture; observe hearth state. |
| **Vennit Rusk** | Visiting Recension field scribe, northeast | Wants accurate return accounts; accepts that some visitors do not want names entered. “You told Hesta you were expected. May I enter that, or only that you passed?” | Talk, read competing witness reports, give or withdraw personal testimony. Uses the canonical Recension voice card. |
| **Farra Sprig** | Bower gatherer renting a guesthouse corner | Wants an arrangement people willingly inhabit; keeps forgetting that supper interrupts it. “The red bowl beside the wet stone. Yes. You can take it when you've eaten.” | Talk, purchase or move agreed arrangement objects, participate or decline; native theft rules still apply to owned stock. Uses the Bower voice card. |
| **Edden Brack** | Porter and Hesta's adult sibling; southern arrival beside the bread oven | Was recently marked overdue while privately visiting someone elsewhere. Wants a route home without accounting for every hour. “I said I'd be back. I didn't say I'd come back the same way.” | Talk, give routes and local evidence, help at the crossing, carry an agreed package using normal inventory/quest state. |

**Mossfoot** is proposed as a named `YellowfootWayfarer`; the current image provides one small tortoise beside the bread oven. One pale creek-bank frog was generated. For native Unity integration, map suitable creature art to actual health, AI, examination and combat, and validate the frog's appearance against its intended species before calling it a `GlasspaneFrog`. The current extractions are single poses. Directional animation, a second frog, panniers, cargo transfer and animal recruitment are separate additions; none is inferred from the static reference.

## Houses, shops and inventories

The following identifiers were verified in `Assets/Resources/Content/Blueprints/Objects.json` on 2026-09-06. Reuse appropriate behavior, but author local descriptions and shop stock separately: `ReadingTable`, for example, currently describes a body-reading table and should not be presented unchanged as an ordinary household ledger desk.

| Building | Initial stock or service proposal | Existing mechanical foundations | New authoring required |
| --- | --- | --- | --- |
| **Gate House** | Free briefing, optional return record, signed loan of rescue equipment; no mandatory toll | `Signpost`, `RopeAnchor`, existing conversations and faction reputation; `LockedDoor` only for the separate house door | Local watch dialogue, optional loan terms and a nonmagical return ledger. The source entrance arch stays open. `RopeAnchor` is currently examinable scenery, not a complete rope system. |
| **The Dry Hem** | Two beds; ordinary rest and dry storage; daily board bundled with food purchase | `Bed`, `WoodenBarrel`, `Sack`, `Crate`; existing rest and inventory | Local rental rules, two distinct bed owners, lawful player storage, clear inventory ownership. Do not silently confer instant healing or supernatural safety. |
| **Orrit's Long Loop** | Small stock: 3 `Torch`, 2 `Dagger`, 2 `Spear`, 1 `LeatherArmor`, 3 `Tepuibone` | Existing commerce, equipment, containers, repair-adjacent world interactions | Merchant stock and prices through normal commerce. Portable rescue rope and general repair service need explicit new content/mechanics; no fake purchasable sprite. |
| **The Second Bowl** | 8 `Mushroom`, 5 `DriedMeat`, 2 `HealingTonic`, 2 `BurnSalve`; one `WaterTonic` on the shelf | Existing food, tonics, `MushroomRing`, `WaterPuddle`/liquid storage, fuel and thermal systems | Local merchant and harvest permissions; actual public water amount/purity. A `WaterTonic` is an existing tonic, not an alias for drinking water. Soup and a kettle recipe are new content if sold as distinct items. |
| **The Return Desk** | Local directions, attributed visitor reports, evidence submission; optional paid copying | Existing conversation and knowledge systems, `Bookshelf`, `ReadingTable`, `Chest` | New readable records and local desk variant; no memory restoration, true-history revelation, or special game-state census hidden behind dialogue. |

Farra's small outdoor display is a side stall rather than a sixth full shop. Initially use existing `CharmFlowers`/`FlowerField` art and examine behavior, plus individually owned bowls or pots only when their corresponding item content exists. Selling “color”, “a view”, or arrangement work requires an actual service outcome; a trade UI with empty promises is not sufficient.

Stock quantities above are initial balancing proposals. Restocking should occur through the existing merchant cadence with finite budgets and consumed stock, not on entering the chunk. Purchased goods leave the merchant inventory; visible display proxies either follow that inventory or are explicitly non-sale furnishings. The same mushroom must never be both a free harvest yield and paid merchant stock. Mandatory quest supplies have a free local route or a clean refusal path.

## Three local quests

### 1. The return notch

Hesta has marked Edden overdue. His wet packing strap is caught at the footbridge, but he is alive beside the bread oven, angry about the public record. Hesta needs to know a rescue is unnecessary; Vennit wants the record accurate; Edden wants to keep his private visit private.

The player examines the strap, speaks with Edden, and can report only that he is safe; persuade him to correct the record himself; or refuse to mediate. No solution requires reading his private pack. The visible resolution is the returned cord taken off the search rail and the rescue sled unpacked. If the player knowingly says he is still missing, a short ordinary search uses a watch shift and the lie can later be discovered. No random offscreen death is attached to a timer. Reward is a modest shop allowance or one night of board, paid once. Edden remains an ordinary resident afterward, not a permanently rescued quest statue.

### 2. A bell that can be heard

Nemm's wrapped bell is quiet enough for the frogs but unreliable in rain; Hesta wants the cloth removed. Orrit shows a worn joint that is the real mechanical problem. The player can help repair the joint and test a less abrupt clapper; keep the muffling and arrange a paid second watch at the crossing; or restore the loud bell with a clear warning that it disturbs the bank. These are different practical costs, not a single correct doctrine.

Resolution changes the actual bell component, its interaction text/sound, and an agreed watch schedule. A bounded local test verifies that the gatekeeper can hear the chosen signal; it does not invent a permanent global stealth buff. Cloth, a clapper and any consumed repair material need genuine owned components. The player may later revise the arrangement by paying the outstanding material/work cost. Native frog behavior should only change if a real authored response exists, not solely because dialogue claims it did.

### 3. Room for supper

Farra has borrowed three colored vessels for an arrangement across Sella's table. Sella needs the table in time to feed returning porters. Farra proposes asking diners to stay in the arrangement after they eat. The player can move the display to a side shelf with Farra's agreement, help make an arrangement of objects that leaves seats free, or tell Sella they will not negotiate. The last choice returns responsibility to the two residents and closes the player's promise.

The objects actually change owners/positions or remain where the player leaves them. No reward requires trapping an NPC, forcing a pose, or stealing tableware. Farra reacts to the arrangement's observable composition without claiming to speak for what the Bower sees. Reward is a small pigment object if new item content has been authored, otherwise an existing food allowance; never an unimplemented item name. The free seating, dishes and subsequent meal are the visible consequences.

## Access and durable world state

The main route is a physical north–south lane beneath the source's **open arch**. Its posts have collision; the open center remains passable. A free first conversation gives observed warnings and offers a return record. The player may accept the record, decline politely, or come back later; declining does not make them hostile or require a purchase. The Stillcord's control is exercised by people and local procedure, not invented hinged art. The five separate house doors do change collision when opened. The watch can warn and provide service without owning the player's late-game progression.

Optional work earns trust, discounts, or a faster greeting. No quest, reputation minimum, named NPC survival, purchased permit, or unexplained “readiness” flag is the only way to the existing Felling. Preserve normal world-map descent at (3,5), southern edge travel between (3,6) and (3,5), and the Felling's other existing neighboring routes. Morrowfast's watch governs its road locally; it cannot be advertised as sealing all possible approaches.

If a guard is attacked, ordinary combat and witnessed reputation changes apply. A clear withdraw/de-escalate route remains to the south. Later restitution can settle repairable local hostility through an available resident; do not require resurrecting a dead captain. The open arch cannot become a save-persisted locked barrier merely because a keeper dies or leaves. Owned property still has consequences. A footbridge/back walk provides circulation and an ordinary alternate way around a stalled animal; it is not a hidden magical bypass.

Persist unique identities and state for buildings, doors, containers, residents, fauna, harvests and movable components. Keep a local settlement revision, house-door states, quest accepted/closed/refused state, explicit testimony consent, paid rewards, loan balances and chosen bell arrangement. Save inventory ownership, not only a decorative “removed” bit. Exiting, sleeping, reloading, or returning from the Felling must not refill purchased stock, resurrect actors, repeat rewards, replace collected objects, erase repairs or reopen a deliberately closed conversation. Daily schedule changes derive from the existing world clock; they do not silently advance inactive-zone simulation.

## Small routines that make it inhabited

- At dawn, Orrit turns the hanging boots so the toes drain. Pell immediately moves one pair back because it belongs to somebody expected soon.
- At height, Sella sets one damaged bowl beneath the roof drip. It has been “going to the mender” for several years.
- At dusk, the watch cords come off wrists and hang individually on the rail. A cord left out means somebody has agreed to stay on duty; it does not mean a person has vanished cosmologically.
- Farra keeps moving a red stool to catch the light. The worn patch on the paving records where Sella keeps putting it back.
- Mossfoot pauses beside the warm drain. The porter route has room to go around; its quirk never blocks the only shop doorway.
- The frogs call near the creek. Their sound can stop because the player or another ordinary creature approaches; do not make every pause a supernatural omen.
- Vennit uses two unequal stones to hold the register open. One carries a food stain over a day's blank entries. Examining it supplies a mundane anecdote, not a hidden correct Felling account.

## Component and interaction requirements

The source image is a composition reference; **a cropped house roof is not an enterable house**. Each building needs independent roof, walls, doorway, threshold, shutters, floor/backing, furniture, possessions and contact contributions to support the promised actions. Removal of a roof must reveal a complete plausible room, not a blurred approximation or a black rectangle. The final extraction manifest should state exactly which of these states actually exists.

| Component family | Required ownership and states | Player-facing result |
| --- | --- | --- |
| Buildings | Separate roof/interior visibility owner; wall collision; door open/closed state; floor and hidden corner reconstruction | Enter/exit, view rooms, open doors; inspect fixed construction. Roof hiding is a presentation rule, not a spell that destroys a house. |
| Entrance arch and cords | Open arch, posts, hanging cords and ornaments, separate owned contact shadows; later bell-quest cloth/clapper need their own assets | Keep the visible center passable; examine equipment; use the bell only when its authored action exists. The arch is not an open/close door. |
| Shops and furniture | Stall/counter, stocked items, containers, stools, trays, signs, awnings | Trade with the real merchant inventory; take permitted loose items; sit/use only where a real native action exists. |
| Domestic objects | Beds, laundry, boots, dishes, tools, firewood, books and storage | Individually examine; move/take meaningful portable objects; theft rules and native destruction apply where appropriate. Decorative microtexture remains a surface, not a fake object. |
| Ecology | Individual plants/fungi, loose rocks, creek banks, water, animals | Harvest, collect, liquid interaction, collision, native animal movement/combat; suitable backing remains when an owner departs. |
| Effects | Steam, smoke, ripples, small light accents | Follow their source's existence and active state; no steam from a removed cold kettle or contact shadow left after pickup. |

Every interactive owner needs a stable ID, source bounds, exact alpha mask, physical footprint, reachable interaction point, depth anchor, permitted actions, backing coverage and state/persistence mapping. Components hidden by other components need reconstructed unseen pixels at the declared movement/removal extent. If an object is allowed to move arbitrarily, export a complete object; a source crop missing its back is only safe for removal at the original position. Treat tiny inseparable marks as a named surface detail, not a fabricated collectible.

Fixed cliff/terrain may stay in a grounded surface layer with examination regions and collision, as in the Felling. That does not excuse baking houses, shop goods, residents or creatures into the background. Avoid coupling unrelated owners in one rectangle: a mushroom beside a crate must remain when the crate is removed.

## Production sequence and acceptance

1. Generate the top-down settlement reference with Imagegen using the composition above and the Felling's palette/material reference. Inspect cardinal road continuity, visible entrances, scale, roof separability and creek routing before extraction.
2. Inventory the actual image. Reconcile generated details to this proposal; do not pretend missing objects or interiors were painted. Author an ID map and extraction boundaries before cutting.
3. Cut components with exact source pixels where available. Create an actor-free and prop-free base; reconstruct each hidden surface and each promised roof-off room using Imagegen plus explicitly authorized image-processing/export scripts. Track generated backing as generated evidence rather than original source.
4. Export native-size sprites, masks, contacts, backing patches, complete room layers, source placements, colliders/anchors and provenance. Keep source composite reconstruction separate from hidden-surface quality review.
5. Build a layered reconstruction and interaction preview: individually select owners; hide/remove/restore them; move the player through the connected road and house entrances; test all reachable anchors. Show only actions genuinely supported by that preview, and distinguish them from planned Unity dialogue, economy and AI.
6. Review at gameplay scale: source reconstruction, all-removable-objects absent, each roof off, each house door open/closed, the unchanged passable arch center, creature departure, overlapping props removed in both orders, and lantern/hearth inactive. Look for halo edges, repeat texture, painted-in shadows, furniture cut by extraction masks and unexplained ground color seams.
7. For later Unity implementation, bind owners to ordinary simulation entities and existing systems, add scoped new content where listed, and perform real native-input testing. Mirror Felling verification for source/owner identity, collision, real action cost, inventory transactions, save/re-entry, native fauna, all interaction approaches and ordinary player access from the south and the Felling.

Acceptance for the art/component delivery means a faithful reconstructed exterior, complete declared backing, a readable connected settlement, per-object interaction metadata and explicit remaining scope. Acceptance for a playable Unity settlement additionally requires real doors, shops, residents, quests, schedules and persistence; an image or clickable browser mock-up is not proof of those systems.
