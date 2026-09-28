# Quest-free exploration: a varied, persistent wilderness

**Status: E0 baseline frozen; E1/E2 implemented and locally verified in native Unity; broader exploration acceptance remains open.** Prepared against `8b01bf02b743d75463baa504e055c1b10b9823d5` on 2026-09-27 (local date), with execution authorized in the following user turn. The original planning pass read source and prior evidence without running a new census, suite or playtest. New implementation receipts are recorded below as they are actually obtained. Execution instructions are in `QUEST-FREE-EXPLORATION-EXECUTION-PROMPT.md`.

## 1. Player outcome and scope

A player can leave the starting settlement, ignore every quest, choose a direction, and find places worth understanding. Terrain changes the approach. Animals and people have visible reasons to be there. Resources, equipment, danger, useful information, and opportunities interact. Fighting, observing, trading, gathering, detouring, and leaving something alone are legitimate responses. Returning reveals the consequences of earlier actions.

The target is the depth of ordinary wandering associated with Caves of Qud, expressed through **original Caves of Ooo creatures, places, assets, and lore**. This is a design direction, not a claim of Qud mechanics parity. The [official Qud description](https://cavesofqud.com/) is reference context for interacting physical and social systems; no decompiled reference implementation was examined for this plan.

The Spread is the first complete proving ground. Its identity remains worked river country: fields, hedges, roads, ordinary fauna, scraps of habitation, and trouble at the margins. Completion requires variety throughout eligible wilderness, not another isolated showcase beside spawn. After the Spread passes the gates below, apply the same design discipline to other biomes with their own ecology and decisions.

The ordinary loop is **notice → understand enough to choose → act or bypass → use what was learned or obtained → encounter the changed place later**. A successful bypass counts when the player recognized a reason to avoid the place. An unnoticed decoration does not count as a decision. No quest acceptance, journal check, report conversation, or hidden completion flag unlocks this content. Existing rumors may help orient the player but remain optional and historical.

### Boundaries that remain in force

- Preserve the approved biome-wide 3D presentation, player appearance, spawn, existing routes, authored sites, quest bindings, rare selections, and Turnbank wayhouse. This is not a return to the old 2D presentation or a camera redesign.
- Keep BitLocker tinkering dev-only; GlassScorpion neutral; plain Shambler without spores; Gin Frogs silent. Keep the shipped one-shot Sill omen/ending rule for `UrquActive`.
- Use original enemies. Do not reintroduce Snapjaws or copy Qud names, creature identities, models, or lore. New names below are working CoO designs, subject to a repository/lore collision check before authoring.
- Follow `PROJECT-IDENTITY.md`: one persistent RPG character and recoverable death. No run resets, permadeath assumptions, or separate metaprogression.
- Do not resolve protected mysteries, put manufactured loot in Choir natural caches, turn friendly service NPCs into profitable targets, or put loaner/crafted/unique gear in generic finds.
- Retain native decay, consumption, and destruction where already intended. Persistence means consequences survive; it does not mean every corpse lasts forever.
- Broad combustion-scale normalization and general fire-bridge changes remain deferred pending native balance evidence. Dog fetching and unrelated minor defects do not block wilderness content.

## 2. Audit findings and corrected assumptions

The audit follows **definition → factory/registration → ordinary source → action/AI → feedback → persistence**. A class existing in the repository is not proof that players meet it, and a staged scene is not evidence of ordinary exploration quality.

| Prior premise / tempting shortcut | Current evidence | Planning consequence |
|---|---|---|
| “We still need core ranged combat, liquid use, food and equipment.” | The density completion ledger records live tactical kits, visible equipment, cooking/water, carry/pour, finite harvest and other completed slices. Current `PopulationTable.SpreadTier1` nevertheless selects just 1–2 Vipers or 1–2 MarlbackScrabblers as its ordinary hostile group. | Broaden sources and combinations. Reuse working actions; do not rebuild whole systems on stale gap lists. |
| “There are already many ordinary situations.” | `SpreadWildernessSituationPlan.Select` uses a one-in-eight selection bucket, then formation restrictions. Its late builder realizes cargo and shelter only. Gleanings are existing field composition, not a third newly generated encounter. | Twelve families below are a proposed total content budget, including three expanded existing families. They are not twelve already shipped features or twelve cosmetic arrangements. |
| “Six formations remove spatial repetition.” | `SpreadCompositionPlan` has six identities and three husbandry conditions, but uses three parcels, a focal clearing, and four converging approaches as its shared grammar. | Preserve formation identity and boundary continuity while adding meaningfully different interior topology. |
| “An occupied shelter implies territorial behavior.” | Existing ordinary guards can chase and return; the current shelter only relocates existing Scrabblers. It has no new warning, ownership, territorial leash, or negotiation rule. | Introduce explicit local territory behavior only where a family requires it. Do not promise it through description alone. |
| “Hoarders already build treasure nests; animals naturally hunt one another.” | Existing hoarding is loose-object collection/return, not a deposit transaction. Shared Beasts allegiance does not supply predator–prey relationships. | Foraging, predation and deposition are named implementation work with ownership and faction controls, not data-only variants. |
| “More things on every screen will solve repetition.” | The ordinary table already has ambient fauna, flora, gear and a guaranteed hostile-group selection. Three existing seeds also show appreciable crop density. | Redistribute actor pressure and resources. Measure decisions and traversal, not entity count. Quiet country is intentional. |
| “One-in-eight means one successful scene every eight chunks.” | Previous native selection/commit samples were 3/8, 4/7 and 7/10 selected chunks, with geometry/source refusals. | Track eligible, selected, committed, visible and acted-on separately. Selection probabilities alone are not player experience. |
| “Cached return is permanent world retention.” | `ZoneManager.UnloadZone` removes ordinary graphs; later access regenerates. Current lairs and the wayhouse have specific retention rules. There is no automatic ordinary wilderness eviction in the inspected manager. | Make a bounded persistence decision before shipping new finite situations; do not invent a disk-streaming project without evidence it is needed. |
| “Spread T2/T3 tables mean stronger Spread regions are already reachable.” | The current authored map has 142 Spread surface cells, all T1. Higher-tier Spread tables exist, but the map does not select them on ordinary Spread surfaces. | Complete varied T1 country first. Any higher-tier Spread branch is a separate explicit geography/progression decision, not a silent distance-based retier. |
| “Prior native runs prove the new exploration loop.” | The ordinary grain trip proves normal travel, harvesting, consumption and saved aftermath. Cargo/shelter views and wayhouse branches used disclosed setup transfers. | A new consecutive, quest-free walking sample is required. Existing tests and frames remain historical evidence only. |

### Source index at this checkpoint

Line numbers are navigation aids at the recorded commit; recheck symbols before implementation.

| Source | Relevant seam |
|---|---|
| `Assets/Scripts/Data/Tables/PopulationTable.cs:360` | `SpreadTier1`, ambient rows and the two-choice `SpreadTier1Encounter` group; adjacent methods define higher tiers. |
| `Assets/Scripts/Gameplay/World/Generation/SpreadWildernessSituationPlan.cs:14` | Eligibility, versioned hash selection, and transient exact-owner generation receipts. |
| `Assets/Scripts/Gameplay/World/Generation/Builders/SpreadWildernessSituationBuilder.cs:31` | Cargo/shelter admission, bounded geometry, one-time receipt consumption and rollback. |
| `Assets/Scripts/Gameplay/World/Generation/SpreadCompositionPlan.cs:43` | Interior topology, shared boundary coordinates, land-use masks. |
| `Assets/Scripts/Gameplay/World/Generation/Builders/SpreadCompositionBuilder.cs` | Native terrain realization and finite ripe-row selection. |
| `Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs:158` | Pipeline wiring and rare-hook dependency on the `SpreadTier1` table name. |
| `Assets/Scripts/Gameplay/World/Map/ZoneManager.cs:45` | Get/cache/attach, final generation acceptance and explicit unload. |
| `Assets/Scripts/Gameplay/Save/SaveSystem.cs:348` | World metadata binding; `SaveGraphSerializer` saves actual cached graphs and entity references. |
| `Assets/Scripts/Gameplay/AI/BrainPart.cs`, `AIBehaviorPart.cs`, `AIGuardPart.cs`, `AIHoarderPart.cs` | Existing goal priorities, ambient behavior, guarding and collection. |
| `Assets/Scripts/Gameplay/AI/FactionManager.cs`, `CombatTacticsPart.cs`, `AISelfPreservationPart.cs` | Faction relationships, current combat kits and retreat. |
| `Assets/Scripts/Gameplay/Farming/FieldHarvestPart.cs` | Real finite harvest and recoverable overflow, already working. |
| `Assets/Scripts/Gameplay/World/Generation/Builders/PopulationBuilder.cs`, `ContainerBuilder.cs`, `TradeStockBuilder.cs`, `HaulablePropBuilder.cs` | Actual roll/placement order and stock ownership. |
| `Assets/Scripts/Gameplay/World/Generation/ZoneGenerationPipeline.cs` | Optional refusal must not trigger whole-zone destructive retries. |
| `Assets/Resources/Content/Blueprints/Objects.json`, `Assets/Resources/Content/Data/Loot/LootTables.json` | Blueprint inheritance, actual models/parts, loadouts and tiered sources. |

Read alongside this plan: `DENSITY-COMPLETION-PLAN.md` (current ledger), `DENSITY-LOOT-COMPLETION.md`, `LOOT-FINDS.md` (historical tables explicitly qualified), `SPREAD-COMPOSITION.md`, `DENSITY-SPREAD-PRESENTATION-SCOPE.md`, `SPREAD-DISCOVERY-AND-EXPEDITIONS-PLAN.md`, `PERF-FOUNDATION.md`, `PROJECT-IDENTITY.md`, `../CLAUDE.md`, `../ADVERSARIAL_TESTING.md`, `../Lore/MYSTERY-LEDGER.md`, and `../Lore/Voices/VOICE-CARDS.md`.

### Existing evidence is bounded

`Verification/SpreadDiscoveryExpeditions/Integration/README.md` records a full native run of 20,633 cases: 20,632 passed and one pre-existing lighting fixture failed. A later test-only correction and two persistence cases passed in a separate 50-case follow-up. The resulting registry had 20,635 distinct cases; there was **no single all-green 20,635-case full sweep**. These are previous-tranche receipts, not verification of this plan. Likewise, the standalone runner is a core pre-check, not proof of Unity visuals, inputs, exact native seeded maps, or performance.

## 3. Situation catalog

The initial budget is twelve families: three existing ideas expanded and nine new ones. This is enough to test several different motives and actions without committing to an unbounded procedural-content framework. Each family needs at least two decision-changing variants before final completion; the first playable tranche may ship one sound variant of each of its four families. Names and numbers remain design proposals until measured.

### Catalog overview

| ID | Family | Main decision | First delivery |
|---|---|---|---|
| F1 | Road-spill salvage | Weight, equipment usefulness and detour | E2; expands cargo |
| F2 | Occupied bank | Respect, avoid or challenge a visible territory | E2; expands shelter |
| F3 | Last gleanings | Gather now, watch the grazer, or retain supplies | E2; expands fields |
| F4 | Watering margin | Refill, observe wildlife, or avoid exposure | E2; new composition |
| F5 | Snake-side forage | Food versus poison risk and approach distance | E3 |
| F6 | Collector's return | Follow a collector, compete for loose goods, or leave them | E3 |
| F7 | Separated work gang | Isolate cooperating enemies, control them, or bypass | E3 |
| F8 | Roadside exchange | Turn a carried find into a useful supply | E3; expands existing travellers |
| F9 | Heavy salvage | Haul useful material or leave the burden | E4 |
| F10 | Cooling work patch | Use a real cooking source or leave a hazardous margin | E4 |
| F11 | Hunt through cover | Observe, interrupt, exploit a distraction, or retreat | E4 |
| F12 | Broken field passage | Take the long route or open a persistent local shortcut | E4 |

F1–F4 are deliberately more than the old cargo/shelter/gleaning arrangement plus another snake. The first tranche must deliver a noncombat animal activity, a territorial response, a liquid-use opportunity and different route geometry. That supplies an early test of the intended systemic direction. Families can share an action, but must differ in the reason, timing or consequence of using it. If the walk shows F3 and F5 feel like the same harvest-under-threat episode, change their setup/behavior before counting both as successful variety.

### F1. Road-spill salvage

**Place and activity:** an OldRoad bend or verge contains legitimately rolled cargo or a dropped ordinary tool. Abandonment is a physical arrangement; no invented living owner, theft law or precise historical explanation is asserted. **Opportunity and complication:** inspect actual supplies/gear, carry only useful items, detour through cover, or leave the load. Road exposure and carrying burden do the work; enemies are optional.

**Variants:** a visible loose tool with a short exposed approach versus a container on a longer sheltered spur; light consumables versus bulky equipment already produced by normal rolls. Empty stock remains possible. These change approach and carrying decisions, not just box color. **Reuse/new work:** native pickup, comparison and partial container transfer; add explicit loose-item provenance beyond the current container-only receipt. Do not unbox/re-roll contents for appearance.

**Budget/placement:** one existing eligible cache OR the eligible loose-item allowance, zero added value; no locked, quest or unique cargo, no road blockage. **Aftermath:** exact stock, taken quantities and left-behind items survive return/load. **Presentation:** existing sack/crate/tool models and actual held/dropped gear; show the object from the approach before selecting it. No new cart model is needed unless a cart actually becomes a physical object with a purpose.

### F2. Occupied bank

**Place and activity:** an ordinary Marlback tool-holder uses a bank or fallow shelter. It returns to a visible work post when unoccupied. **Opportunity and complication:** a route or existing useful stock lies near its territory. The player can stay outside, read its warning and leave, take a longer path, use available control, or enter and fight. A cache is not mandatory.

**Variants:** a narrow defended spur with a broad bypass versus a wider work patch with two approaches; an ordinary tool loadout versus a shield/tool combination only if the actual budget and equipment system admit it. Leaving the claimed area stops this role's territorial pursuit under its explicit contract; attacking it creates the normal personal-hostility exception. **Reuse/new work:** existing post/return, equipment and combat; new scoped territory boundary, warning and pursuit release. Current `AIGuardPart` alone is insufficient.

**Budget/placement:** replace the ordinary group with one routine T1 holder initially; count its actual equipment/death loot, use at most an existing cache, exclude rare/quest guards. Place outside arrival sight/attack pressure with an observed bypass. **Aftermath:** damaged, pacified, fled or dead holder and looted stock remain independent states. **Presentation:** approved Marlback anatomy with a readable work-post arrangement and real tool; factual warning/log and reader state, not a new global crime UI.

### F3. Last gleanings

**Place and activity:** worked strips contain existing finite ripe rows. A proposed small **reedback grazer** may feed at a separate designated patch, flee close approach, and seek nearby cover. This is a new original animal, not a renamed copied enemy. **Opportunity and complication:** harvest now, spend turns reaching a safer edge, watch the animal move, or leave food for later. Its slow finite feeding makes delay meaningful without erasing all nearby food immediately.

**Variants:** a quiet after-harvest strip with a few easy rows versus a wider strip where a grazer approaches one designated ripe source; a short open approach versus a sheltered working lane. **Reuse/new work:** native field harvest/stubble and overflow; new bounded forage goal, actual consume transaction and safe flight. Feeding can consume at most one allocated row in the first active visit and cannot consume the last remaining player-accessible row in the initial family budget. This explicit finite rule is saved, not rerolled on reentry. Do not spawn bonus food when the grazer dies to compensate.

**Budget/placement:** retain the current 1–3 ripe-row budget. At most one grazer in this family, substituting an ambient slot; quiet versions have no new hostile group. Do not create field ownership or regrowth. **Aftermath:** which row was eaten/harvested, loose grain and the animal's spent feeding allowance persist. **Presentation:** preserve approved ripe/stubble art; one compact new grazer model with readable head-down feeding, flight movement and corpse mapping. No antler/animal model may imply an unimplemented charge or reward.

The feeding variant requires at least two exact ripe-row owners reachable from an approved entry when realized. Allocate one as the grazer target and reserve a distinct row for the player. Recheck both exact living/unspent source owners at consumption; if the reserve was harvested or removed, feeding refuses without consuming stock. One-row fields can still host a passing animal but cannot claim a feeding decision. Cache the realization proof and revalidate owners locally; do not run whole-zone reachability every animal turn. Do not regenerate rows to maintain the rule.

### F4. Watering margin

**Place and activity:** a RiverMeadow or otherwise genuinely sourced water margin has dry approaches, cover and occasionally the grazer or existing ordinary fauna nearby. **Opportunity and complication:** fill a carried vessel, drink through supported actions, observe a moving animal revealing a nearby source, or continue without the detour. The value is liquid utility, exposure and carried weight.

**Variants:** a longer sheltered approach to the finite draw point versus a shorter exposed bank; quiet water versus an animal approaching/leaving it. Finite poured residue is a separate variant only when its actual source and quantity are legitimate; do not place endless water by relabeling a puddle. **Reuse/new work:** native liquid fill/pour and actual terrain; source-aware placement and a grazer visit-water activity. Descriptions must identify real liquid composition and cannot promise universal safety or healing. Existing permanent sources elsewhere keep their existing rules and are not implied by this new family.

**Budget/placement:** no free vessel or gear payout; reuse the eligible source, at most one ambient participant, no mandatory wet/hazardous exit. Do not invent swimming, flood damage or a continuous river. **Aftermath:** actual vessel contents, finite volumes, actor location and other native liquid state survive. **Presentation:** current water palette and models plus an unobstructed bank approach; source/refusal text, normal fill UI and visible animal behavior. Showing blue geometry alone does not certify fillability.

**Source correction and decision:** current RiverMeadow water is permanent tile coating (`SpreadCompositionBuilder:34`); `LiquidVesselService.AddActions` enumerates actual `LiquidPoolPart` owners. Therefore F4 needs more than moving a bank. Add one explicitly owned finite water-pool source at an eligible wet-bank cell, using the existing pool/fill contract, as a deliberate utility addition. Initial proposed quantity is one ordinary waterskin capacity, read from the actual vessel definition; no unlimited refill or free vessel. Track it separately from the background visual coating, with a visibly distinct draw-point model/state and saved volume. Exhaustion must remain recognizable even while the surrounding coating still looks wet. If an existing generated compatible pool supplies the family, use that exact owner instead and add no second source. Validate purity, overlap and renderer semantics; preserve well/river contracts elsewhere.

### F5. Snake-side forage

**Place and activity:** an ordinary Viper near a berry bush, hive or hedge hollow creates an optional dangerous harvest. The snake's actual awake behavior supplies danger first. **Opportunity and complication:** gather from a visible safer edge, wait or take another approach, fight for the resource, or bypass poison risk.

**Variants:** exposed berries versus a honey source behind cover; a distant awake snake versus a separately authored ordinary sleeper with an actual saved wake rule. The sleeper must not copy the unique chalk-ring/latchcoil encounter. **Reuse/new work:** current poison, harvest, sight and cover; new exact harvest-source adapter and optional sleeper child/wake binding. Viper's fast, poisonous bite is not treated as harmless merely because HP is low.

**Budget/placement:** existing 1–2 Viper allowance and existing yield/chance; no extra guaranteed venom, bees, smoke pacification or meat. No forced adjacent arrival, poison corridor or simultaneous added gang. **Aftermath:** consumed source, actual snake state, chance-based corpse/yield and picked-up products persist. **Presentation:** approved snake/forage models; cue and approach visible before danger, and actual sleeper state if adopted. Keep ordinary snake identity distinct from the rare one.

### F6. Collector's return

**Place and activity:** a passive original collector follows a small local circuit, picks up eligible loose **noncurrency** scraps, and deposits them at its home. **Opportunity and complication:** take an item first, follow the collector to an actual cache, inspect/leave the scraps, or ignore it. No quest delivery is involved.

**Variants:** a visible pick-up and homeward route versus already deposited goods with a collector still returning; an open route versus a short screened route the player can follow. **Reuse/new work:** existing hoarder search/return supplies part of the behavior; new narrow eligible-goods tag, deposit transaction, home reference, capacity handling and no-target fallback. Existing GoldCoin converts to drams on pickup; it cannot be represented as a same-ID physical nest coin. Keep existing Magpie behavior intact; a new scoped role may reuse its approved body only if its distinguishing behavior reads honestly.

**Budget/placement:** replace at most one ambient slot and one eligible loose-goods/cache allowance. Exclude currency, quest/unique goods, equipped/owned items, essential supplies and player inventory. Never mint goods on return. **Aftermath:** the goods have one owner throughout collection, deposit, death, theft, capacity refusal and save/load. A destroyed home causes safe release or retained inventory, never reconstruction. **Presentation:** visible carried scrap, home and depleted state; no claimed nest until deposit works. Do not revive the unrelated dog-fetch project.

The carried scrap is explicit new presentation work: hoarder inventory is not equipped gear, and the existing humanoid equipment rig does not automatically fit a bird. Add a scoped attachment tied to the actual collected owner and collector anatomy, with cleanup on deposit, loss, death, unload/load and pooling. Test that no ghost or duplicated item remains. If an approved collector body cannot support a readable attachment, author the required small socket/pose or choose another original body; do not count an invisible inventory change as a readable collecting scene.

### F7. Separated work gang

**Place and activity:** two original ordinary Marlback roles occupy a broken hedge work patch with real equipment. **Opportunity and complication:** separate their lines of sight, use control/cooldowns, fight a pair, or use the bypass. Cooperation occurs only through the existing opt-in, faction, willingness and sight checks.

**Variants:** mutually visible posts versus cover interrupting assistance; actual reach weapon/skill versus a short-weapon partner. **Reuse/new work:** current supported tactics and assistance, retreat, ordinary child blueprints and exact art/loadouts. No new flanking, radio alarm or lunge movement is implied: current Lunge is a reach attack. Keep unique ditch-cutter identities/kit rare.

**Budget/placement:** at most two hostile actors replacing the ordinary group, both gear/death values counted; no added poison or compulsory bottleneck. Refuse if the opening cannot communicate the pair and a viable bypass. **Aftermath:** separated, dead, controlled or fleeing actors keep individual state; drops are actual gear. **Presentation:** approved bodies and real weapons with distinguishable stances/equipment; observed assistance and blocked-assistance feedback, not recolors presented as new AI.

### F8. Roadside exchange

**Place and activity:** a legitimately generated traveller sells supplies on a usable road verge. **Opportunity and complication:** use carried finds to buy food, liquid capacity or other ordinary stock; keep money and move on; return knowing stock may change through its real restock rule. The player need not ask for a job.

**Variants:** different actual finite stock roles and a safe verge versus visible distant trouble with a clear safe exit. **Reuse/new work:** current traveller entry, Merchant stock/purse and trade. Current travellers use an entry roll and a three-encounter journey cap. First compose only actual existing travellers; then make a deliberate measured decision whether that cap/frequency supplies enough ordinary social variety. Raising it is new economy/source work, not just placement. A family selection with no eligible traveller records a refusal; it must not fabricate one after the fact.

**Budget/placement:** at most one existing service actor in the initial version; no free gift, best-gear guarantee, added purse, forced combat or merchant-as-loot bait. Do not make it a guaranteed service every few chunks. **Aftermath:** actual trade stock, purse and timed restock persist under native rules; leaving does not recreate the seller. **Presentation:** existing merchant body, real displayed equipment/stock and normal trade reader. No extra stall art unless it has a gameplay purpose.

The cold planner may reserve a compatible verge, but the actual traveller does not exist until `WorldTravellers.OnZoneEntered` runs after a successful transfer. Add a separate narrow accepted-entry hook for F8, revalidating the current managed graph and exact newly created traveller. Failure leaves the ordinary traveller at its legitimate position. Do not let the late cold-generation composer claim a dynamic actor receipt or count reserved terrain as a committed exchange.

### F9. Heavy salvage

**Place and activity:** an abandoned work spot contains an explicitly handling-compatible prop and possibly its already-budgeted portable companion. **Opportunity and complication:** haul something useful along a broad lane, take a small piece, or leave the burden. A longer roomy route may be better than a tight corner.

**Variants:** a haulable object useful as cover/obstruction under actual collision rules versus portable salvage; broad lane versus a detour around a corner. **Reuse/new work:** existing DragRules/DragSystem, real Handling definitions, source receipts and route placement. Generic crates, trees and rooted objects do not become draggable through flavor text. Validate actual cover utility before describing it; sale value alone is not sufficient justification for heavy freight.

**Budget/placement:** one existing haulable allowance, no new currency windfall or living hauls. The starting character must have at least one viable option, including leaving; do not require increased Strength for mandatory travel. **Aftermath:** exact moved object remains where released, with honest grip loss and ownership. **Presentation:** approved compatible object, dragging motion and actual footprint; verify corners and arrival cells, not just a model gallery.

### F10. Cooling work patch

**Place and activity:** a small abandoned work/cooking spot provides an actual usable heat source beside a safe approach. **Opportunity and complication:** cook carried ingredients, use the place while safe, salvage an already-budgeted item or leave the heat alone. It is valuable even without a fight or new chest.

**Variants:** a usable low-risk cooking source versus a warmer margin with a clear outside path; player carrying raw ingredients versus no useful recipe/input. **Reuse/new work:** actual cooking, ingredient consumption, thermal rules and source state. If a new fuel resource is needed, implement finite quantity and depletion explicitly; an oven mesh is not proof of a valid cooking source. No broad combustion rewrite.

**Budget/placement:** one small source; initially no additional hostile group or resource gift. Prefer recipes/ingredients the player already carries. Keep heat away from arrivals and required exits, and test that autonomous burning does not turn the entire chunk into unavoidable damage. **Aftermath:** ingredients/cooked products, actual fuel and thermal changes persist; no relighting from scene recreation. **Presentation:** existing fire/food/scenery where accurate, actual safe/hot boundary and current action/refusal text. No fake smoke-to-pacify interaction.

### F11. Hunt through cover

**Place and activity:** a proposed original **furrowstalker** follows a reedback grazer through local scrub. The player can witness approach, pursuit, feeding or escape without starting the event. **Opportunity and complication:** observe a natural distraction, pass behind the hunter, interrupt the hunt, defend the grazer without a scripted reward, or retreat. A witnessed kill may leave an actual harvestable corpse under authored rules.

**Variants:** hunter and prey separated by sight-breaking cover versus near an open grazing lane; escape possible through a narrow opening versus longer open flight. No forced winner or pre-rolled “battle aftermath” claims. **Reuse/new work:** movement, sight, combat and corpse harvesting; new scoped prey selection/pursuit and feeding rules. Generic Beasts currently ally, so pairwise prey behavior and its interaction with factions must be deliberate and tested. Do not globally make all beasts hostile to each other.

**Budget/placement:** one hunter replaces the ordinary hostile allowance and one grazer replaces an ambient slot. Avoid initial attacks on the player at entry; no extra gang, pet victim, civilian stock farm or guaranteed corpse payout. Limit local hunting frequency; no breeding or offscreen food-web simulation. **Aftermath:** the actual survivor, kill source, spent feeding state and corpse/depletion survive. **Presentation:** a second new original animal body with readable stalking/strike posture; actual grazer flight and corpse feedback. This is a later milestone because it adds behavior, not merely a prefab pair.

### F12. Broken field passage

**Place and activity:** a fallow enclosure or minor ordinary field structure interrupts a useful local line of travel. A long open route remains. **Opportunity and complication:** follow the bypass, operate a real ordinary gate, or remove an actually destructible obstruction to create a shortcut for later travel. The reward is a changed route, not always loot.

**Variants:** an operable closed gate versus a destructible hedge obstruction with a genuine action/cost; a short crossing with exposure versus a longer covered perimeter. **Reuse/new work:** native door state, destructible terrain, movement and saved geometry; source-aware small layout and connectivity validation. Do not add a second key-and-buckler wayhouse or claim arbitrary wall-breaking tools work.

**Budget/placement:** no new enemy or guaranteed gear; use an ordinary scenery allowance, exclude authored locks/keys, protected stamps and zone exits. Both initial bypass and opened route must connect useful points. **Aftermath:** opened/destroyed boundary remains changed after travel/load. **Presentation:** approved hedge/gate/wall models with distinct open/broken states, correct collision and line of sight. Verify a player can recognize the shorter route before committing an action.

## 4. Actor behavior and readable interactions

Use a few reusable motives to create many combinations rather than one bespoke script for each site. Proposed behavior work is scoped to its own role/part; existing creatures and faction tables keep their behavior unless a specific correction is justified by a RED test.

| Role | Behavior order and bounds | Must prove before advertising |
|---|---|---|
| Territorial tool-holder | Immediate threat/personal hostility and party control take precedence; otherwise warn/defend only its saved local territory and return to its post. | Outside/inside boundary, player attacks first, pacification, disarm, fleeing, moved/destroyed post, saved pursuit and nonparticipants. A warning must offer time/space to leave. |
| Reedback grazer | Threat/flight before optional feeding or water visit. One local target, bounded retry, finite feeding allowance. | Actual consumption once, depleted/replaced target, full/absent source, close approach, no-path, leaving territory and load. It cannot spend every turn rescanning all objects. |
| Collector | Threat/party control before collection; collect only explicit eligible loose goods; deposit atomically at a valid home. | Home/item IDs and backlinks, stack merges, capacity refusal, death, destroyed home, two collectors racing and load during carry. Currency remains separate. |
| Cooperative ordinary pair | Reuse current combat/assistance admission, actual supported skills and equipment. | Occlusion, unwilling/controlled/dead partner, foreign faction, missing weapon, cooldown and safe fallback. Do not grant every skill to every enemy. |
| Local hunter | Threat and normal self-preservation before bounded prey pursuit; target only authored local prey under a specific relationship. | No pet/service/party targets; stale/dead/foreign prey; aborted pursuit; corpse provenance; no global faction change; successful escape as well as kill. |
| Passing seller | Existing service and entry/stock rules, unaffected by combat-family replacement. | Ordinary availability, real trade, purse/quantity changes, restock timing and no duplicate actor on return. |

Use existing goals where their actual semantics fit. New parts need factory registration, saved state and normal world sources in the same completed milestone. Prefer explicit state with a small bounded target reference over ad hoc strings or a universal behavior interpreter. Invalid saved targets fall back to a safe idle/reacquire path without creating resources. No per-turn remote-zone access, blanket faction rewrite or full-world ecology tick.

Two priority seams are mandatory implementation work. `BoredGoal` checks ordinary faction hostility before dispatching bored behavior: a territorial Raider therefore needs scoped target admission **before** that path, plus release from its own pursuit when appropriate. Define warning/grace state and meaningful room to retreat; do not clear unrelated personal hostility or make a normal hostile attacker safe through a post tag. Likewise, existing `FleeGoal` ends according to health-based `ShouldFlee`; healthy grazer proximity flight needs its own bounded condition/lifetime. Calm, party control, current combat, disabled/dead actors and cross-boundary targets need explicit precedence tests in both changes.

Resource/behavior steps must charge exactly the intended AI action once. `BrainPart` can immediately execute a newly pushed child goal; feeding, depositing, visiting water and hunting must not gain free repeat operations or double-charge through goal chaining. Test paid action/energy counts along with visible results. For territory, “immediate threat” means actual attack/personal hostility or an admitted target, not generic Raider faction hostility outside its warning boundary.

Feedback uses the current Look reader, action menu, comparison and logs. A cue states something observable now: a head lowered over grain, an actual carried tool, a warning at a bank, an empty cache. Historical signs cannot report live remote stock or guarantee a resident. A new identity requires body/rig, actual gear attachment, motion states, corpse and dropped-item coverage where applicable. Do not replace approved palettes/camera to rescue an unreadable single model.

## 5. Spatial composition and repetition control

### A finite regional plan, not independent scene rolls everywhere

Add a narrow `SpreadExplorationPlan` beside the existing Spread plans. Its input is the world seed, the actual manager map and POIs (including seeded placements), supported canonical tier/formation data, a generator version and already-frozen protected selections. It considers only the finite supported surface addresses, intersecting current map eligibility with the supported authored geography. It must never call `GetZone`, inspect live enemy health or stock, accept quests, consume caller RNG, or use the order in which the player visits chunks.

Use separate deterministic salts for region activity, family, topology, actor package and reward package. A stable canonical candidate order and deterministic constraint pass assign each address at most one principal situation. Keep the result stable if a local placement later refuses; do not shuffle its neighbors or reroll another family until the desired payout appears.

Initial distribution targets, to be calibrated in the baseline/census milestone:

- Roughly 30–40% of eligible chunks are quiet: geography, ambient life and existing limited gathering, without a forced hostile package or principal situation.
- Roughly 40–50% offer a routine principal situation, often optional or noncombat. The remainder carry higher tension appropriate to tier. These are weights to tune jointly, not independent additive rolls.
- A chunk has at most one principal family and at most one compatible minor environmental complication. Existing protected rare/quest/wayhouse sites are outside this budget.
- Avoid the same principal family on edge-adjacent eligible chunks. Track variety within fixed 3×3 coordinate neighborhoods clipped to the eligible mask: where at least six situations are assigned, target at least four families. This diversity target is a census/tuning measure, not an exhaustive search over every possible connected set.
- Target no more than two consecutive high-pressure assignments on the frozen audit routes and report longer streaks elsewhere in the bounded neighborhood census. Treat route streaks as tuning evidence; hard rules are adjacency exclusions, legal ecology and protected sites. No identical family + activity + topology + reward-role signature among immediate neighbors. Never use a mutable “last encounter the player saw” list.
- Never guarantee an encounter each screen or all twelve families on one twenty-chunk walk. A family absent from eligible formations is reported as absent, not forced into the wrong ecology.

Implement canonical ranked greedy assignment, followed by at most two deterministic repair passes over the finite address list. For each cell/pass consider each active family at most once and check only its four neighbors plus cached 3×3 counts. If no valid family remains, choose quiet and record why; do not violate an exclusion. This bounds work by addresses × active families × a fixed small number of checks. Prove deterministic results under reversed generation and visit order. Do not add an open-ended solver or backtracking search to every chunk build.

### More than the same three rectangles

Maintain current formation signatures and shared edge portals. Separate their interior topology from the existing parcel count/focal-star pattern. First implement three alternative grammars alongside the current one:

1. **Offset lanes:** two working corridors with sparse cross-links; resources on one side, a quicker exposed route on the other.
2. **Broken enclosure chain:** a few irregular hedge/copse pockets connected by broad breaches; local cover matters without a mandatory locked bottleneck.
3. **Bank and crossing:** an asymmetric backwater with two dry approaches and an optional shorter hazardous margin. Water behavior must use actual liquid/terrain rules, not an invented swimming requirement.

Later add open meadow islands and a winding road/copse edge if the first three still read similarly in play. Rotation, mirror, color, and different prop positions alone do not count as additional grammars. Vary number, area and relationship of open spaces; route length/exposure; sight lines; and where resources sit relative to danger. Use the current ground/hedge/field/flower/reed kit wherever it communicates these differences.

Adjacent chunks share boundary decisions. Extend a short road/hedge/bank segment through the same matched edge port; do not claim a continuous river across the map when current water is an inset backwater. Do not seal another biome's entry or create water/walls over a known transition. Preserve a legal traversal route through every ordinary zone, and a bypass for any optional dangerous situation. The bypass must be tested against actual detection and movement, not just an empty geometric lane.

### Generation and transaction contract

Select a family before generating its zone; roll the baseline ordinary sources once, then stage any explicitly budgeted replacement package and realize it using exact builder outputs. Retain the existing exact-owner receipt discipline for already-rolled owners. The present late composer deliberately cannot create new actors or stock: do not smuggle new spawning into it while claiming its conservation proof still holds.

Introduce a small, explicit per-zone population/resource package for new families. A package specifies which ordinary encounter allowance it replaces, allowed ambient actors, any replacement container, actual gear and maximum finite resources. It is generation-only data, not a new entity registry. Generate each owner once through the normal factory, capture its origin and snapshot, and let a bounded realization step place only those owners.

For deliberate new-role substitution, retain the original ordinary package until the replacement packet passes final acceptance. Build the bounded candidate once with a scoped feature RNG/factory context, then atomically replace only the designated source owners; refusal disposes only still-owned candidates and preserves the original package. Do not reroll baseline stock or dispatch death/loot rewards when replacing a generation-only actor. Test factory/global-context restoration and callback side effects. This is additional transaction work beyond relocation, justified by the new roles; extend the existing Wayhouse replacement pattern rather than claiming old M4 conservation tests already cover it.

Protected sources keep their pipeline and random stream behavior. In particular, renaming population tables must not disconnect the name-based rare hook. Either retain that name or replace the hook with an explicit capability and test both branches. Do not mutate a shared static table. A v2 chunk may deliberately differ from its v1 rolls; unrelated biomes and excluded old paths must not.

Preflight the entire footprint and bypass against final terrain, landmarks and haulables. Move or replace only authorized ordinary package owners, never nearest-by-blueprint objects. Refuse incomplete stock, wrong factories, stale revisions, repeated receipts, duplicate IDs, foreign owners, wrong equipment/backlinks, active saved graphs and protected cells. Preserve baseline source owners on optional failure. Validate the staged packet again after generation callbacks and before final cache acceptance. Roll back only references still owned by the transaction. Optional refusal returns success/no-op, not pipeline failure/whole-zone retry. Emit a bounded, named reason.

## 6. Population, rewards and lasting aftermath

### Budget policy

The previous cargo/shelter tranche conserved every rolled owner and added no rewards. That remains its historical guarantee; it is not a universal ban on new content. This expansion deliberately revises ordinary distribution, with explicit per-family allowances and before/after measurements.

Start with these provisional bounds for tier-one principal situations:

- Replace the ordinary 1–2 hostile group; never add a whole new encounter on top. At most two initial hostile combatants in routine T1 situations. A territorial or hunting actor may begin neutral/occupied, but still consumes danger capacity if it can attack.
- Allow up to three small nonhostile participants only where their behavior matters. Count resident ambient fauna, travellers and summons when checking actual density; do not simply add three to the existing maximum.
- Usually use zero or one existing open cache. A specialized nest/camp resource container replaces an eligible allowance; no free second chest. An actor's equipment is also part of the reward budget.
- Finite food resources initially remain near existing 1–3 field yield units. Any additional harvest must displace an equivalent source allowance or carry a documented, measured new economy cost. Do not reduce familiar ripe fields merely to disguise arbitrary reward inflation.
- Tier-one gear stays in the current tier-one source pools. A dangerous family does not justify T3/T4 gear in the starting country. Unique, forged, loaner and fixed expedition rewards keep their provenance.
- No automatic money, quest XP, reputation or repeated completion payout for noticing, entering, waiting, reopening a cache, or saving. Kill rewards remain the native system's responsibility.

Balance across a walking region, not by making every situation worth the same base-value number. Measure generated value, accessible stock, player-acquired units, consumed utility, actual sale proceeds, healing/supply expenditure and carried burden separately. Existing merchant prices and carried equipment matter. A useful waterskin or route can be valuable without a new currency payout.

Before locking weights, compare matched worlds and normal walks with the current baseline. Investigate a greater than 25% rise in median acquired sale proceeds or hostile turns per twenty eligible chunks; this is a tuning trigger, not permission to delete useful food or an assertion that all builds should earn the same amount. Record tails and lethal spikes, not just means. A change can be justified explicitly if it improves choices without trivializing local purchases or survival.

### Persistence and compatibility decision

**Chosen first implementation:** a versioned manifest for newly created worlds, with bounded exact-graph retention for its supported Spread surface addresses. Retain graphs only once successfully generated or restored, not by eagerly generating the region. Reuse the existing full graph serializer and Wayhouse-style final acceptance/retention discipline. Normal travel already caches graphs; the added contract prevents explicit unload from recreating an opted-in ordinary location and refilling rewards. It does not introduce a new eviction system or claim to solve arbitrary world streaming.

The manifest's capacity is bounded by the supported authored map (20×20 addresses, with 142 current Spread surface cells before exclusions). Record actual opted-in count, retained count, memory and save size during the full walk. No event-per-turn history, unbounded respawn log or new disk archive is needed initially. If measured retention is unacceptable, reduce the rollout or implement a separately tested archival strategy before release; never silently evict and regenerate finite stock to satisfy a memory cap.

Bind manifest version/seed/assignments to the world before the first new ordinary zone is generated; serialize it through a bounded world-property record or a narrow schema addition justified by actual serializer limits. Keep immutable generator assignments separate from mutable installation/disposition records and actual graph authority. A saved address, anchor name or family tag alone cannot authorize recreating rewards. At final generation acceptance capture the exact managed graph; restore retained authority only from a validated saved graph and compatible metadata. Reject duplicate/out-of-range/foreign records. Malformed or unsupported v2 data must fail candidate hydration cleanly or refuse affected missing-graph access with a diagnostic; it must not fall through to legacy generation and refill rewards. Preserve loaded contents and the prior live session when candidate hydration fails.

**Old saves without the manifest remain on their legacy generation path**, including missing cached zones. A missing graph is not proof that the player never visited/unloaded it. Existing characters/locations are preserved; the new distribution is initially available in newly created worlds. Do not reset the player's save or quietly adopt an old world. Optional adoption of demonstrably unvisited territory is future migration work requiring reliable visit history; it is not part of this tranche. Make this availability boundary explicit in implementation reports.

Keep **placement eligibility** and **persistence coverage** as distinct frozen masks. Preserve all opted-in ordinary Spread graphs, including quiet/no-site outcomes and ordinary glade/rare addresses excluded from new placement; the Wayhouse retains its existing authority. Settlements and specialized POIs retain their explicit lifecycle rules rather than being blanket-pinned by biome label. This is not a promise that merchant stock never legitimately restocks. Bind after frozen rare/wayhouse selection and before first access. During load, the detached manager's constructor must not leave a new-world plan active: restore or disable the manifest before any graph access or global activation. An installed record without its saved graph is an integrity failure, never an instruction to regenerate stock.

Do not equate `activate:false` with a saved-world restore: fresh detached preview/census worlds also use it. Use an explicit fresh-world initialization versus unbound restore state. `ReplaceLoadedState` can attach graphs before entity bodies and world metadata are hydrated, so attachment must not infer installation/retention from a temporary constructor plan. Only final validated hydration establishes restored authority.

Unknown future manifest versions preserve already-loaded graphs but do not generate replacement v2 rewards. Existing rare/wayhouse/lair metadata and retention remain independent. Test full save/load while away from a partially changed site, direct unload attempts, cache reuse, active-zone handling, ownership references and connection counts. Stock cannot reappear, an actor cannot duplicate into a second graph, and changes cannot disappear merely because the family was not observed by the player.

## 7. Implementation sequence

Dependencies are **E0 → E1 → E2 → E3/E4 → E5**. E3 and E4 may have separate workers for art or isolated tests, but shared generation/save/blueprint edits need one owner and ordered integration. E6 follows successful Spread acceptance. Do not publish twelve empty registrations as twelve playable families.

Every behavior change follows the repository workflow: meaningful failing test → minimal implementation → positive/negative controls → dedicated adversarial coverage → native/source/visual checks as applicable → Q1–Q4 review → living document update → scoped commit using `CLAUDE.md` §2.3. New `.cs` files under `Assets/` need hand-written `.meta` files with fresh 32-hex GUIDs. Edit `Objects.json` surgically; parse before/after and compare blueprint objects to prove only intended definitions changed. Validate changed loot through `LootTableRegistry.Validate`. Do not count placeholder part presence or self-mirroring implementation tests as behavioral proof.

For any new creature, check the actual natural weapon, anatomy, faction, loadout, corpse and source path. Update exact-list pins such as `GameAuditNaturalWeaponActivationAdversarialTests` only for intentional additions, with an actual attack/behavior witness; do not loosen them or reuse the historical roster count. A new predator cannot ship with an accidental unarmed fallback while its description promises a bite.

### E0 — Freeze a useful baseline and measurement vocabulary

**Outcome:** distinguish absent opportunities, invisible cues, repetitive decisions and inconvenient controls before tuning. Recheck the source inventory at actual implementation HEAD; save three fixed metadata cohorts and current full-pipeline counts. Run an ordinary seed64 pilot, then capture the seed1/1729 baseline walks before changing production behavior so all three candidate comparisons are paired. If a baseline walk cannot be completed, retain its partial record and label the corresponding candidate evidence unpaired; do not claim a measured improvement for that route. If a baseline test driver is unreliable, record the limitation rather than spending days building a campaign bot.

**Files:** extend existing `SpreadWildernessPipelineCensusTests.cs` and add proposed `SpreadExplorationCensusTests.cs` / a narrowly scoped scenario observer under the existing test/scenario folders. Store receipts in `Docs/Verification/QuestFreeExploration/` with seed, version, current input hashes, setup and scope. This is instrumentation reuse, not new content.

**RED/controls:** a diagnostic/measurement change gets a test showing a known refused selection incorrectly counted as a commitment, or a known repeated-family route escaping its summary. Pair with a real committed owner and empty/quiet chunk. Read-only baseline gathering needs no artificial failing test. **Native/perf:** initial unaided approach views, actual choices, cold entry/save size/retained graph count. **Completion/commit:** raw baseline, formulas and known limits recorded; instrumentation commit only. Baseline documentation alone does not complete the first playable tranche.

### E1 — Regional assignments, geography and durable state

**Outcome:** one finite manifest with declared active families, protected exclusions, quiet/intensity policy, matched boundaries, and saved aftermath. Implement three additional interior grammars, exact accepted-graph retention and explicit new-world/legacy load behavior before adding rewards. Enable the first four families only when E2 is ready; intermediate scaffolding may be exercised by fixtures without presenting empty features to players.

**Files:** proposed `SpreadExplorationPlan.cs` / small family definition table; `SpreadCompositionPlan.cs`, `SpreadCompositionBuilder.cs`, `OverworldZoneManager.cs`, `ZoneManager.cs` only for the narrow retention seam if required, `SaveSystem.cs`, and normal bootstrap initialization. Keep existing v1 entry points intact.

**RED:** changed world visitation order must not change assignments; a quiet or depleted opted-in graph must not regenerate after explicit unload; restored old-save absence must not retain constructor-created v2 metadata; neighbor entries must agree and remain traversable. **Controls/adversarial:** zero candidates, valid empty manifest, duplicate/out-of-bounds/unknown versions, map/plan swapped in callbacks, installed record with missing graph, foreign same-ID clone, generated-but-uncommitted packet, legacy unload, settlement lifecycle, and rare/wayhouse preservation. Assert no new `GetZone`, factory or caller-RNG side effect from plan reads. Full graph determinism is separate from stable assignments: current runtime string hashing and source RNGs are not automatically fixed by this manifest.

**Native/perf/save:** inspect all new grammars at the current camera, cross both sides of representative boundaries, partially harvest/loot and return through real travel and load. Measure finite planning work, cold generation, full-manifest retention/save size and unchanged-turn overhead. **Completion/commits:** one planning/persistence commit and one geometry commit with their tests/doc evidence; no growth-dependent hot loop, no stock reconstruction, no old-world retrofit. Use one composer-wide maximum of 256 layout candidates initially, not 256 per family, and bounded regional repair passes.

### E2 — First playable four-family tranche

**Outcome:** F1 cargo, F2 territory, F3 grazer/gleanings and F4 water access occur through normal new-world Spread sources. At least one variant of each works; distinct activity/route/resource choices are visible without a report or quest. Preserve unaffected ordinary source paths.

**Files:** `PopulationTable.cs`, existing producer receipts and `SpreadWildernessSituationBuilder.cs`; proposed scoped territory/forage/healthy-flight parts/goals in `Gameplay/AI`; `FieldHarvestPart.cs` only for a shared finite-consume primitive if required; liquid source blueprint/binding using `LiquidVesselService`'s real contract; surgical `Objects.json`, factory registration, approved rendering libraries and tests. Exact class names should follow neighboring conventions when source is rechecked.

**RED:** healthy grazer actually approaches/consumes an allowed source once; the remaining row cannot vanish; holder warns outside/inside territory before its normal faction chase; leaving territory has the correct personal-hostility exception; actual fill moves finite source volume into a normal vessel; family refusal retains original owners. **Controls/adversarial:** stale/dead/cross-zone targets, calm/party control, factory morph/reentrancy, full inventory, two harvesters, second consumption, wrong liquid/mixed source, removed post, repeated receipts, staged replacement rollback, gear/death accounting and exact saved aftermath. New roles have dedicated adversarial fixtures, not only census tests.

**Native/visual:** new game with ordinary character, find these opportunities by real movement in a multi-chunk pilot, use one earned supply and voluntarily bypass one danger. Inspect grazer idle/feed/flight, holder warning/attack/return, water empty/full and partial cache states with before-Look views. Staged mechanism branches are permitted as separate evidence, never substituted for discovery. **Perf/save:** profile active animals and territory queries in a busy chunk; save during feeding/pursuit and return to partial resources. **Completion/commits:** separate territory, grazer, source/composition and visual integration commits; ship the bundle only when the four-family route and controls work. If F4 has only a blue coating or F2 only a generic chase, the tranche is incomplete.

### E3 — Collection, tactical separation and social usefulness

**Outcome:** add F5 snake forage, F6 collector return, F7 work gang and F8 exchange, expanding decisions without adding a hostile pair to every chunk. Give E2 families their second meaningful variants.

**Files:** typed forage/loose-item/ambient receipts, scoped collector/deposit logic near `AIHoarderPart`, ordinary role blueprints/loadouts, family definitions/composer, and the dynamic accepted-entry hook in `WorldTravellers.cs`. Use current trade, faction and combat services; no universal theft-law system.

**RED/controls:** actual loose item transfers to collector and then home exactly once; home destruction/capacity/stack merges and two collectors preserve ownership. Assistance succeeds with real kit/sight and refuses when occluded, controlled or unwilling. Snake harvest remains optional and uses actual chance/yield. Exchange occurs only after legitimate traveller creation; absent/capped/refused entry cannot mint a seller or stock. Keep currency-conversion, legacy Magpie, unique pair, unrelated population and existing traveller-cap controls.

**Native/visual:** watch a collector without directing it, isolate or bypass a gang, choose a poison-risk approach and complete one actual trade using carried goods. Compare two family variants through real views, complete one naturally chosen return and separate staged edge cases. **Perf/save:** no zone-wide hoarder scan each turn per creature; save during carried-item state, trade and controlled-combat state. **Completion/commits:** collection, tactical/forage, dynamic trade and integration boundaries, each with measured stock/danger delta. Document any justified traveller-frequency revision with actual transaction/economy data; no silent cap lift.

### E4 — Environmental utility, hunting and changed routes

**Outcome:** F9 hauling, F10 cooking, F11 hunting and F12 persistent passage. Different terrain and native systems now change where the player goes, not just what they kill. Complete meaningful second variants for this set.

**Files:** haulable/hazard/scenery producer adapters, existing drag/cooking/door/thermal services only where genuine fixes are required; new scoped hunter/prey parts/goals and original model/blueprint bindings; family table/composer. Use existing corpse provenance and harvest.

**RED/controls:** a real handling-compatible prop can traverse the chosen lane and remain where released; rooted/live/protected owners refuse. Cooking consumes actual ingredients only with a valid source. A healthy prey can escape a real hunter; a kill generates only native authorized corpse/yield. Party/pet/service and generic allied Beasts remain untouched. Open/broken shortcut persists with correct collision/sight; initial and changed exits stay reachable. Include destroyed target, no path, insufficient strength, heat spread, full capacity, simultaneous actor/source callbacks and reload while pursuing.

**Native/visual:** ordinary inputs for drag, cook, observe/interrupt/bypass a hunt, and use a shortcut again later. Show hazards before commitment and actual changed models/footprints. **Perf/save:** local pursuit bounded by existing active-zone work, actual heat ticks profiled; no offscreen simulation. **Completion/commits:** environmental utility, hunt, passage and integration are separate reviewable units. If one later optional variant becomes a low-value detour, record and cut that variant; do not count a broken family as complete or suppress a serious save/ownership problem.

### E5 — Whole-Spread composition, tuning and final acceptance

**Outcome:** all twelve families have actual sources and at least two decision-changing variants; regional occurrence, quiet space and rewards make the three fixed ordinary walks less repetitive. Close gaps revealed by observation rather than filling the map with chests.

**Files:** family/weight/layout data, any narrowly justified behavior or cue fix, dedicated `QuestFreeExplorationAdversarialTests.cs`, scenario/census evidence and this living plan. Finalize active catalog/version deliberately; do not mutate existing saved manifests to force newly added families into accepted graphs. Development cohorts use fresh worlds for each generator version.

**Gates:** full native Spread source census plus the twenty-chunk walks in §8; every family has genuine success and refusal evidence, ordinary source availability and all required art coverage. Run focused RED/GREEN for tuning defects, then a stable full native EditMode sweep and cold-eye Q1–Q4 review. Compare affected core runner before/after when used, then native checks for excluded presentation/input paths. Investigate performance/economy triggers, record all seed failures and unresolved experiential limitations. **Completion/commit:** final integration/evidence commit with exact test counts and play scope, followed by fetch/rebase, relevant checks after any rebased change, and the already-authorized `git push origin HEAD:main` when implementing. This planning pass itself does not push gameplay.

### E6 — Biome-specific expansion

**Outcome:** apply §9 after Spread succeeds. Audit each biome's actual ordinary sources, create its own cards, budgets, art and twenty-chunk-compatible route protocol, then use the same RED/native/persistence workflow. This is planned follow-on work, not an excuse to mark uninspected biomes complete. No invented calendar estimates or promised total creature count replace the evidence gates.

## 8. Verification and acceptance

### Automated and source coverage

Use focused behavioral tests for each milestone and a dedicated adversarial file for state/ownership interactions. Meaningful counterchecks include changing the claimed source, severing actual target admission, replaying consumption, substituting a same-name entity and loading a legacy save. Tests should fail if the promised player behavior is removed, not merely assert that the implementation assigned its own flags.

For seeds **1, 64 and 1729**, census the actual final supported Spread pipelines. Record denominators per family/formation: eligible → assigned → compatible source → legal layout → final accepted → visible → noticed → acted on or deliberately bypassed. The last three require native observation and cannot be inferred from generated data. Record refusals, relaxation reasons, work counts, ordinary counts/value, route connectivity and protected-site conservation. Every enabled family must have at least one real source success and one honest refusal across the corpus. Missing families stay open work; never search only successful seeds and erase the misses.

Initially target at least 80% realization **among source-compatible selections** for static geometry families. This is a placement-tuning target, not a claim about total occurrence; F8's actual entry availability is a distinct denominator. Do not add stock or erase obstacles merely to hit it. Use a broader fixed source-only seed corpus after the three-seed pilot to catch scarce families and constraint/path edge cases; freeze the corpus before checking results. Stable regional assignments and runtime-specific whole-graph generation are separate claims.

The standalone runner can pre-check core rules, JSON, generation, serialization and diagnostics. Its hashing patch, stubs and excluded Unity files limit its proof. Compare before/after result sets with `diff_results.py` when using its full sweep; “newly failing” must be empty, and environmental failures must match individually. Native Unity remains required for final regression, actual seed maps, import, rendering, UI and real actions. Keep independent run counts separate and retain failure receipts. No production changes or such new tests were executed in this planning pass.

### Freeze an ordinary walking sample

1. For each seed, create only normal world-map metadata and identify the real starting zone. Select a connected cohort of **20 unique Spread surface chunks** by cardinal breadth-first expansion, with stable seed/ID tie-breaks. Do this before generating or inspecting their content. Retain naturally included POIs/rare/protected chunks but label them; do not count their existing quests as new exploration variety. If fewer than twenty connected nodes exist, report that limit.
2. Save IDs and a spanning tree as evidence. Walk that connected route using ordinary on-foot boundary travel; connector revisits are recorded but not counted as new chunks. The walk need not be a simple path. The player can take local detours and choose actions from visible information. Do not transfer the player, scout hidden stock to choose the route, grant gear, change HP/stats, force RNG, move sources, or replace inconvenient seeds.
3. Start with seed64 as a pilot and preserve that outcome. Use one continuing ordinary character/save per cohort, without accepting quests or visiting the report conversation for instructions. Normal shops, rest, food and optional social interaction remain allowed. Record incidental tutorial/progression changes instead of clearing them for the experiment. If death occurs, record it and any ordinary recovery/load; do not silently grant safety.
4. Run the same frozen spatial cohorts against baseline/candidate versions with matched starting configuration. Keep the source census in separate worlds so inspecting it does not direct the live walk. Observations can occur in 30–45 minute blocks using the same save; report actual time and reached chunks. If only part is completed, label it partial.

### Decision measurement and experiential gates

A **decision episode** needs a perceived cue, at least two presently feasible options and a meaningful difference in route, risk, turns, supplies, carrying/equipment or lasting state. Opening/taking/eating one cache is one episode, not three. Looking at an unrecognized prop is not proof of a choice. Awareness that cannot be established is recorded as unknown.

For each episode retain seed/address, entry/approach view, cue, feasible alternatives, choice and reason when observable, paid actions/ticks, health/status/supply/burden deltas, actual obtained/used/equipped/sold goods, and aftermath. Record useful refusals and boring repeats too. Report episodes per twenty unique chunks and per ten active minutes; longest gap; repeated-family streak; action/decision diversity; opportunities that went unnoticed; and whether finds changed later choices.

Provisional completion targets, calibrated after the baseline without hiding earlier goals:

- Across each completed twenty-chunk cohort, observe at least four distinct decision families, including noncombat resource/service or route choices. This does not require all twelve situations per seed.
- At least two episodes per cohort must change a later resource, equipment or route decision beyond automatic pickup. A coin count alone is not evidence of utility.
- Across the three cohorts, witness actual bypass/retreat, resource use, an equipment or trade choice, autonomous actor activity and a meaningful environmental/route interaction. Controlled mechanism demonstrations may fill test coverage but not this ordinary-observation requirement.
- Investigate stretches longer than three newly entered chunks with no perceived optional choice and dominance of one repetitive decision. Quiet intervals can remain when they have a legible place in the journey; document the reason rather than forcing an encounter everywhere.
- Revisit at least one naturally changed site through ordinary travel in each completed cohort, with one partial/depleted resource and saved carried-state bridge. Check altered routes/actor state where observed. A remote cached API read is not a return journey.
- Obtain a short neutral retrospective: which places were distinguishable, what prompted a detour or refusal, and what felt repeated. Agent/automation observations can establish available choices but cannot alone certify human enjoyment. If no human play observation is available, leave that experience claim open explicitly.

These are proposed engineering/design gates, not statistically proven measures of fun. Report per-seed failures and baseline/candidate differences. Learning the baseline can bias the candidate walk; use a fresh observer or counterbalanced order when possible, otherwise disclose it. Do not claim a scripted successful route establishes player preference.

### Visual and interaction acceptance

Use the real approved gameplay scene/camera, not only an asset gallery. For every new affordance capture approach **before Look/selection**, identified reader/action state, actual use and aftermath. Existing cargo/shelter evidence had selected targets with an outline/sidebar; it does not prove unaided discovery. Include one busy/occluded angle and exhausted/unavailable state. Check long text, wrong-owner removal, memory/fog presentation and return from menus.

For new animals/role children require exact blueprint → body/model/rig → current equipment → animation → drop/corpse mappings. Native import errors, generic fallback glyphs, uncolored placeholders and missing actual equipment are failures. Existing scene/biome coverage must remain intact across neighboring chunks, spawn, NPCs, items and terrain. Root/human image inspection complements style/submission tests; an in-frustum object can still be unreadable behind foliage.

### Performance and memory acceptance

Follow `PERF-FOUNDATION.md`. Profile 60–90 seconds of real busy movement/turns and a separate reader window with matched settings. Measure cold entries and retained revisits separately. Report median, p95/p99/max, long frames and **redraw-conditioned** time; whole-frame p95 can conceal paid-action spikes. Capture actual render dimensions, MSAA/filter, vSync/frame cap, focus/background, warmup, time scale and profiler settings. Keep graph dumps/screenshots outside timed windows and do not sum nested markers.

Use granular `ZoneRenderHooks`/visual invalidation for changed cells, existing batching and fingerprints. No new per-frame plan building, full-world scans, LINQ/collection allocation in new turn loops, or expensive unsuccessful cache rebuilds. Bound target searches and reuse appropriate scratch state with reentrancy guards. Run a positive allocation control before interpreting counters; unavailable is not zero.

Measure retained graphs, memory, save bytes and save/load time after the ordinary walk and after a separate full supported-manifest census. Report growth by visited cells and connection counts. Any repeatable perceptible hitch, unbounded growth, or ≥2× regression in the affected path requires investigation before release. Baseline limits and absolute costs matter; passing a relative ratio alone is insufficient. Historical dense glade measurements are context, not a matched performance baseline for the new animals/geography.

## 9. Expansion beyond the Spread

Do not copy twelve families into every biome with different colors. Reuse the planner, provenance and measurement contracts only after the Spread passes. For each next biome, audit current sources and identify at least four local player decisions, two distinct actor motives and two resource/environment interactions that cannot be reproduced by merely changing a Spread blueprint name.

Recommended order is **Sodden → Beating → Grovelands**, then a separate Stump/underground pass. Sodden should derive choices from its existing wet terrain, disease/exposure and local fauna; Beating from its own terrain, inhabitants and recoverable material; Grovelands from its existing biological resources and faction ecology. These are audit directions, not assertions that missing flood, infection, growth or harvesting mechanics already work. Produce biome-specific cards and RED tests after inspecting those paths. Each rollout preserves authored mysteries, lairs, protected sites and its own visual identity. Overwrit's deliberate absence and authored strangeness must not be filled to satisfy a universal density quota.

Higher-tier Spread is an optional later design decision, not a prerequisite or a current reachability claim. All 142 current Spread surface cells are T1. If a future authored branch legitimately uses the existing T2/T3 tables, give it different compositions, patrol/territory behavior and tactical relationships, not merely more HP. Prove its actual source and return route. Do not silently retier the current map, substitute distance for authored strangeness, or add higher-tier monsters to T1 to inflate the roster.

## 10. Risks, decisions and deferrals

- **Procedural sameness disguised as quantity:** reject variants that leave the same approach, action and payoff. Family counts are inventory, not acceptance evidence.
- **Hostility swallows ecology:** ordinary combat/assistance rules must not make every observer, grazer or trader join a fight. Scoped participants and per-relationship tests are required.
- **Faction farming or free stock:** visible ownership, real transfer, finite resources and full state preservation are ship gates. A new territory mechanic cannot grant arbitrary aggression rewards.
- **Unreadable danger:** a beautiful model does not prove warning/readability. Test at the actual gameplay camera and approach distance, with UI readers and sensible detection margins.
- **Overpromised systems:** nest deposit, predation, warning territories and moving-service behavior are new work unless their complete ordinary path passes. Cut a costly optional variant if needed; never relabel a decorative substitute as working.
- **Save and route damage:** corrupted ownership, duplicated rewards, blocked exits, missing loaded actors or rewritten saved locations are significant and must be fixed before affected content ships.
- **Minor detours:** after one focused reproduction/fix attempt and roughly 45–60 minutes without meaningful progress, classify the issue. Record reproduction, impact, workaround and follow-up. Defer a low-impact animation, dog-fetch behavior, or unnecessary harness convenience; continue an independent milestone. Time elapsed does not waive a serious defect.
- **Offscreen simulation:** no world-wide per-turn ecosystem, reproduction, respawn economy or season simulation in this plan. Offscreen places preserve state under the chosen retention policy; existing intended systems keep their rules. Predation is local and active-zone bounded.
- **Later possibilities:** seasonal regrowth, long-distance migrating herds, transferable land ownership, broad theft law, generated history and global combustion normalization are separate proposals. None are required to make this twelve-family slice useful.

## 11. Review and living execution ledger

Three independent source audits covered actor/reward contracts, generation/persistence and native experience/presentation. Their cold-eye review of this draft produced the following resolved plan corrections. These are planning checks, not gameplay-test passes.

| Review angle | Finding and resolution |
|---|---|
| Q1 — symmetry | Exact accepted/restored graph authority is separate from placement eligibility. Missing old metadata means legacy; damaged v2 metadata or an installed missing graph must not regenerate rewards. Load constructors/early attachment cannot confer new-world authority. |
| Q2 — cross-system consistency | Territorial warnings precede ordinary faction targeting; healthy flight is new behavior. Collector inventory needs an actual carried-item visual. Traveller composition is after accepted entry. Water coating is not a fill-capable owner, so the finite source/visual addition is explicit. |
| Q3 — counters and finite work | Grazer preserves a specific unspent row and charges one paid action. Replacement generates baseline first and stages candidates. Regional repair has two bounded passes; neighborhood diversity is a measured target rather than combinatorial search. Legacy, foreign-owner, refusal and callback branches have named tests. |
| Q4 — documentation versus implementation | All 142 Spread surface cells are currently T1; later-tier reachability is not assumed. Historical native20633/follow-up50 results remain separate. Baseline walks for all three comparisons are explicit, and incomplete comparisons must be labeled unpaired. No production work is described as completed. |

The recommended **first implementation tranche** is E0–E2: baseline and source diagnostics; stable bounded assignments and saved aftermath; three additional route grammars; then F1–F4 with the new grazer, actual territorial holder and finite fill source. Begin by rechecking current HEAD/status and source drift, freezing the three metadata cohorts, and writing the E1 failing tests for visit-order independence, old-save constructor leakage and opted-in unload/refill. Follow with the E2 real-action RED cases before behavior edits. Keep unrelated art/evidence/log changes out of scoped commits.

| Milestone | State | Actual evidence / deviation |
|---|---|---|
| Planning source audit and design | Complete | Three independent audits and cold-eye plan review; corrections above incorporated. Source inspection and earlier receipts only; no new production delta. |
| E0 baseline and measurement | Initial planning checkpoint (superseded by §12) | At planning time HEAD equaled origin/main at8b01bf02; no new receipts had yet been obtained. |
| E1 regional planning and durable state | Implemented candidate; see §12 | Native persistence/geometry/integration evidence, with experience gates still open. |
| E2 first four playable families | Implemented candidate; see §12 | Four connected families, native core/art checks; bounded live observation still underway. |
| E3 second four families | Private implementation in progress | F5/F7 source/placement and v3 save-compatibility work; no E3 publication yet. |
| E4 final four families | Not started | No new behavior shipped. |
| E5 full Spread breadth, tuning and acceptance | Not started | No twenty-chunk acceptance sample executed. |
| E6 other biomes | Deferred until Spread acceptance | Needs per-biome source audit and content design. |

The original planning handoff called for E0 followed by E1/E2 as one reviewable first tranche; §12 now records actual execution. Updating documents or scaffolding the planner alone does not complete that tranche. This document grants no new verification claims; record the actual scope, limitations and receipts as the work lands.


## 12. Execution ledger — 27 September 2026

The reusable instruction is `QUEST-FREE-EXPLORATION-EXECUTION-PROMPT.md`; it is now being executed. Work began at main `8b01bf02` after fetch confirmed no remote delta. Unrelated MCP logs and existing untracked art remain outside the implementation staging scope.

| Area | Current status and evidence | Remaining acceptance |
|---|---|---|
| E0 source baseline | Frozen20-node cohorts for seeds1/64/1729,60 actual native source rows,18 exact legacy signatures; accepted serializer V2 and rejected incomplete V1 retained under `Verification/QuestFreeExploration/E0`. Source/assembly archive preserved privately before changes. | Native ordinary walks were partial: seed64=20 inputs/0 transitions, seed1=102/1, seed1729=37/1; all stopped at the observer's moving-owner replan cap with0 recorded failures. None is a complete20-chunk walk or human awareness/decision evidence. |
| E1 geography | Three new interior grammars, matching neighbor ports, formation identity and finite resource budgets.12 RED+3 controls →15GREEN;2 budget regressions caught then19GREEN;18 frozen native signature controls bring standalone fixture to37GREEN. | Integrated native pipeline and legacy signature checks passed. Gameplay-camera layout review, matched costs and full regression acceptance remain required. |
| E1 persistence | Explicit opt-in fresh world vs unbound restore; finite assignment and exact graph retention, protected exclusions and malformed-state refusal. Agent's first63 core cases green, matched legacy runner comparison has no new failure. | Native74 manifest/save/adversarial cases pass; strict exact-attempt final validator implemented. Normal new-game activation is now ON in the candidate and native4 activation cases pass. Final composer late-geometry and existing-water reuse supplement is published; focused native verification is running. |
| E2 behaviors | Scoped warning/territory and finite grazer feeding/healthy proximity flight.52 standalone behavior cases green;52 completed without failures in a native mixed job. | Source placement, full actor graph controls, original art and feed callback are connected. Local native Play proof is pending. Healthy flight steps away; named-cover seeking and water-visiting remain unimplemented. |
| E2 sources |10 failing-first source-budget tests →10GREEN;5 new receipt tests RED then green;3 added blueprints only, verified by parsed diff.4 content tests RED, then3GREEN/1 missing-empty-readout RED, then4GREEN. | Native13 pipeline cases realize all4 families across seeds1/64/1729; local frozen60-cell sample realizes14 sites but no feeding site. Live action/visual proof and final acceptance remain open. |
| E3–E6 | Still planned; no new completion claim. | Follow the dependency/acceptance gates above. |

### Additional verified corrections and decisions

- Population tables live in `Data/Tables/PopulationTable.cs`. A hostile-group receipt does not authorize loose tools or ambient birds; separate exact/revision-bound receipts are added.
- OccupiedBank deliberately replaces the rolled1–2 hostile allowance with one ordinary MarlbackScrabbler. LastGleanings substitutes at most one actually rolled Magpie for the original ReedbackGrazer. The source table roll itself remains intact; post-roll generation RNG consumption can differ in opted-in worlds. Legacy generation remains unchanged.
- The ordinary waterskin has `WaterskinPart.Capacity=3`, not `LiquidVesselPart`. Both existing water-specific and general-liquid services conserve finite LiquidPool volume. Permanent bare tile coating alone is not a fill source. The new draw point is a persistent3-unit source with an explicit empty readout; no free vessel or renewing source is introduced.
- Ordinary accepted graphs, including quiet/protected reference-glade graphs inside the persistence mask, must retain state. Placement eligibility is narrower than retention. Source receipts remain transient; saved manifest disposition distinguishes ungenerated, generated/no site and committed.
- E0 private-DLL JsonUtility silently dropped custom nested fields despite a successful collector return. V1 is rejected. V2 uses actual serialized-count validation; all60 rows/60 cohort nodes/18 signatures were independently parsed from disk before acceptance.
- Native compilation requires importing new Assets before requesting script compilation. A0-test tool result during import is not a pass. Root test-only namespace/internal-access compile errors were corrected before meaningful native reruns; standalone compilation cannot prove Unity test-assembly boundaries.

### Current self-review

🟡 Resolved: new layout crop counts exceeded the existing bound; reduce parcel heights, retain original test limit.

🟡 Resolved in candidate: exact final owner snapshots, captured-attempt validators and original-route geometry checks reject replaced/depleted packets, later relocated packet owners and covered draw points. The final geometry/reused-water supplement passed38 private cases and is published; focused native verification is running.

🟡 Resolved: actor placement/final packet snapshots now preserve exact parts, stats, inventory, body slots, child owners, spatial graph, personal enemies and goal sequence/fields. Paired mutation controls executed before publication.

🧪 Open: ordinary exploration enjoyment/awareness, all20 unique chunks per observed route, native new-model animation/depletion views, matched memory/save/frame costs and full regression acceptance. No prototype/source test substitutes for these claims.

### Connected E2 checkpoint

The first4 families now connect actual cold generation to saved owners. Native136/136 (13 family pipeline +74 manifest +49 placement) passed before the late-geometry supplement. Actor/placement supplemental115 core cases and finite-water100 core cases passed privately. Native115/115 subsequently passed normal new-game activation, all4 family pipeline, manifest/adversarial, imported art and interaction hooks. The separate gallery/role native run passed65/65. These overlapping jobs are separate receipts, not an invented aggregate suite count.

Normal new games now enable v2. The two-argument detached preview intentionally remains legacy for existing tools/baseline fixtures; the explicit boolean overload selects either current or legacy correctly. Restore remains unbound until saved metadata is read. Activation tests were3RED +1 legacy control, then4GREEN. Existing saves do not adopt new placement.

Four original imported forms are present: ReedbackGrazer with five moving clips, its remains, and full/empty finite stone draw point. Each native prefab uses the approved palette and actual body/volume/source identity. The first post-import job completed198 with one failed feeding-height test: the fixture measured rotated mesh-local depth as world height. A read-only native probe showed actual head height .2875→.104689 and unchanged Idle height; correcting the coordinate measurement with an Idle countercheck made the later art run pass. No animation or threshold was weakened. Gallery images were reviewed for form/material readability; galleries are staged and do not prove live feeding.

Finite source filling now marks the exact still-current source cell dirty only after the transaction commits, retaining persistent empty owners. Both ordinary waterskins and general flasks use the same rule; stale/moved/replaced owners and outer rollback cannot redraw or retire another owner. The normal inventory close already caused a redraw; this fixes the missing direct-service notification rather than claiming every keyboard draw was previously stale.

Full-native regression and disclosed local live action/camera proof are in progress. The intermediate frozen60-cell census records21 selected /14 committed sites, unchanged ripe-row totals, no feeding site in that local cohort and zero generation/restoration errors. Individual cold timings are preliminary; retained-memory/save/frame acceptance remains open.

### Cold-eye review checkpoint (before final integration)

Q1: both liquid-fill services use the same exact-owner postcommit notification and preserve rollback. New source ownership seals and geometry checks must be symmetric at initial admission and final graph acceptance. Q2: normal new-game, explicit legacy preview and unbound save restoration are distinct; no saved delegates or source receipts are introduced. Q3: deep actor mutation, foreign same-ID owner, late critical-route blocker, harmless added obstacle, depleted water reuse, missing replacement blueprint and actual body/volume rendering are paired controls. Q4: this is original CoO exploration design, not source-verified Qud parity. Only one variant of the first4 families is targeted in this tranche. No cover-seeking, water-visiting, twelve-family delivery or completed20-chunk experiential acceptance is claimed.

The full native sweep completed20,967 cases:20,961 passed,6 failed,0 skipped in1,477 seconds. Exact held Unity results are in `E2/native-full-sweep-before-finalization.json`; the MCP status wrapper suppresses completed failed-job summaries, so the held summary was read without modifying the plugin. Five failures concern legacy fixture assumptions: ambiguous overload lookup, a controlled generation test omitting the new acceptance-attempt builder, and three v1 census cases using the now-v2 normal constructor. The sixth catches the new draw-point terrain render classification. Each is being inspected and corrected with its assertions retained; this sweep is not reported as full-green. It predates the final composer/readout supplement, whose independent RED/coreGREEN receipts are retained.

### Follow-on scope requested during execution

After the current tranche is verified, assess both deeper combinations of the delivered systems and their reuse in other areas. Compare player value, source/content readiness, model coverage and persistence costs; propose biome-specific ecology, territorial behavior and resource interactions rather than copying the same Spread situations. This is a requested follow-on assessment; E6 delivery still requires its own source audit and the Spread acceptance gate. Continue current implementation first.

### Final integration receipts

Final composer/readout/replacement handling passed66 native cases. The same job included the two not-yet-published live-launcher restoration tests, which failed as intended; publishing the launcher yielded2/2 nativeGREEN. The full sweep’s six failures all pass after scoped fixture corrections in a122/122 focused native run, including the complete affected legacy fixture groups, terrain coverage, imported art and final-composition tests. The original full sweep remains a20961/20967 receipt followed by this correction run, not a newly invented full-green sweep. The draw point already has approved full/empty 3D forms; its legacy2D terrain inventory needed the same explicit object-glyph classification used by other finite object sources, rather than falsely turning its empty pocket into ground water.

First local Play witness retained an honest failure before any paid input: none of the first eight canonical seed64 LastGleanings addresses satisfied all source/view conditions. Its report collapsed source absence and observation-cell refusal, so feeding remains unverified and the observer is being improved to record those reasons and run independent families. No seed, source or encounter allowance is being changed to rescue the demonstration. The launcher restored the clean SampleScene and left Play mode. The actual normal-start screenshot was viewed and retains the approved 3D presentation.

A bounded native cost probe completed330 validated observations with exact restoration and no errors: frozen20-zone cohorts for three seeds, current legacy/opted-in pairs, generation/save/load and synthetic dense-role actions. The allocation positive control returned0, so allocation numbers are unavailable, not zero. It does not measure actual game-frame/GPU cost, ordinary walking or precise retained heap. Detailed interpretation is recorded in `E2/Performance/native-interpretation.md`: median paired cold-generation cost +2.052 ms (1.0814×), save +19.614 ms (1.0221×), and territory warning +0.0847 ms (4.1366×). The latter is a one-actor synthetic branch, not whole-frame cost. Five cold pairs exceeded2× and were inspected; extra bounded source/route validation is plausible, but this probe cannot attribute CPU time. No timing-based guard removal follows.

The requested follow-on assessment is saved in `QUEST-FREE-EXPANSION-ASSESSMENT.md`. Source readiness favors deeper Spread forage/exchange first, then a conditional Beating scarcity/salvage pilot before Sodden access. This refines §9’s initial biome order; it does not waive the Spread acceptance gate or promise regional actor/art coverage from existing static terrain kits.

### X0 source/action correction before implementation

The third same-seed/same-candidate live witness (`E2/NativeStates/33cb14e2fcf643cebd5dd6e4e77e69b0`) completed with0 failures and4 paid inputs: two normal waits to fund then execute the newly entered holder’s first turn, one outward step, and one real inventory Fill. Warning, safe withdrawal, exact finite source3→0/skin0→3, empty-owner persistence and full/empty readouts passed. Feeding remains explicitly unverified. The observer’s original one-wait premise was wrong; gameplay scheduling was not changed. Root viewed the warning and full/empty images; the low basin silhouette could be clearer beside grain, so a bounded model polish is requested.

Actual source audit explains low grazing realization. Population at4000 snapshots all ambient owners; TradeStockBuilder at4100 legitimately stocks remaining Villagers-faction Magpies. The whole ambient receipt is therefore stale by the4300 composer even though the substituted ReedbackGrazer is unchanged. Add a separate exact0/1 substituted-owner receipt and retain the original ambient receipt. Capture the other actual ambient owners after legitimate stocking, before composition, to preserve their final stock/positions. Never refresh or forgive an already-stale whole source packet.

The reserve-row distance≤12 is an implementation-only constraint absent from the parent F3 contract. The animal approaches its designated meal; the distinct reserved row is player supply, not a second destination. Retain the target approach limit and both initial entry-reachability proofs, but permit the player’s exact unspent reserve elsewhere in the same zone. This expands use of existing fields without creating or moving food. Removed, harvested, foreign and inaccessible reserves remain negative controls; at most one meal is still saved. These corrections are approved for failing-first private implementation before shared publication.


### Model polish and follow-on execution checkpoint

The requested assessment, planning prompt, applied expansion plan and execution prompt are now saved as `QUEST-FREE-EXPANSION-{ASSESSMENT,PLANNING-PROMPT,PLAN,EXECUTION-PROMPT}.md`. X0 is active; the remaining parent E3–E6 gates are unchanged.

The actual full/empty draw-point view prompted a bounded source-model change: taller stone rim and a wider raised water surface, retaining the original footprint, exact common stone geometry and finite volume. Source shape tests recorded2 RED with9 existing controls, then11 GREEN. Native import succeeded and21 art/gallery tests passed (`E2/draw-polish-native-tests.json`). All14 generated grazer/remains assets remained byte-identical (`E2/draw-polish-animal-preservation.json`). Same-camera gameplay readability after this import is still pending; the staged gallery is not that proof.

The next original creature is the Tatterjay, a broad teal avian collector with pale bill/chest and uneven tail. Its privately reviewed source model has a dedicated bill attachment and authored pickup/deposit/carry gestures. Real source-item identity, native import, animation and gameplay visibility remain acceptance gates. The new role is independently tested in private; no collector has yet been added to normal world generation. Existing Magpie behavior/art and general dog fetching remain unchanged.


### X0 accepted local interaction checkpoint

The exact grazer receipt/reserve correction passed253/253 native tests (31 new plus222 neighboring cases), after25 failing feature cases and4 controls in the initial29-case private run. This keeps ordinary stocking intact, the separate receipt one-use, unrelated ambient state fixed across final callbacks, the actual meal approach bounded, and the reserved player row reachable and unspent at admission.

The unchanged bounded seed64 native witness now completes:13 checks,0 failures,6 paid inputs. Run `E2/NativeStates/1eb32b959c5f44d7b91d697146f9e457` records actual feeding and stubble with a head-down Interact pose, territorial warning and safe withdrawal, and exact source3→0/skin0→3. Root viewed the grazer and polished full/empty basin images and verified the clean scene restored. This closes local source/action/model acceptance for X0. It remains a disclosed setup-transfer witness, not a20-chunk ordinary walk or human-awareness result.

Q1: near/far reserve checks retain the same exact-owner/one-meal rule, and filled/empty meshes share the same stone form. Q2: only the substituted ambient owner gains a new receipt; old whole-group receipts remain stale after legitimate stocking. Q3: missing/duplicate/foreign sources, source revisions, altered other-bird stock/position and inaccessible/depleted reserves remain explicit counters. Q4: source witnesses and snapshots prove local actions; performance allocation and ordinary walking remain unverified as stated. No serious unresolved X0 finding remains; remaining experiential breadth belongs to E3–E5.
