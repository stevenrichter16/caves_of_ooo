# Spread discovery, wilderness situations, and a compact expedition

**Status: implemented and verified within the explicit limits below, 27 September 2026 (final native runs completed 28 September UTC).** Source baseline: `6015ba8b9c31bfe52658f8ec8b0bcf6f98218a14` (`feat(spread): complete first-hour readers and rare encounters`). Execution is authorized through `SPREAD-DISCOVERY-IMPLEMENTATION-PROMPT.md`. Current focused native evidence is M1 **54/54**, M2 **23/23 plus 80 neighboring cases**, M4 **83/83**, and Wayhouse **50/50 GREEN**, plus its separate economy census3/3. These are distinct scopes, not a summed full-suite result. The native generated-site audit now passes19/19 with both front and separately observed live-guard rear routes; the separate ordinary grain round trip passes14/14. Root inspected the report, grain/stubble, wayhouse and returned-note frames. Cargo/shelter and physical-clue views now pass in a separate9-check native card run, and bounded matched component performance is recorded. The full native sweep completed **20,632 PASS / 1 FAIL / 0 skipped out of 20,633**. Its only failure was an unchanged flicker test’s same-frame variation assumption, corrected in that test only. The subsequent focused native run passed **50/50**, including the corrected seven flicker cases and both late persistence bridges. There was no production change after the full sweep; its 2,437 recorded inputs showed zero drift during the run. The current registry has 20,635 distinct tests; **no single full 20,635-pass run is claimed**. The ordinary wayhouse journey and separate shelter combat witness were not performed. §8 records these explicit scope divergences and experiential limits; §§6 and 12 distinguish implementation from acceptance.

**Direction:** make existing content easier to find; compose three small decisions out of existing wilderness resources; then add one optional surface expedition with two approaches, a useful reward, and a persistent return visit. Keep the approved biome-wide voxel presentation. All new names, prose, compositions, and assets are CoO-original. Qud is a visual inspiration, not a source of enemy identities, assets, or a mechanics-parity target for this work.

## 1. Outcome and scope

A player should be able to hear about an unusual place, deliberately remember the report, consult it later, and choose whether to investigate. Ordinary walking should occasionally present a cargo detour, a useful remaining gleaning, or an occupied shelter. A larger optional trip should offer a short guarded approach or a longer rear approach, yield actual equipment, and remain changed when revisited.

The three steps are:

1. **Reported places:** truthful NPC reports and bounded saved notes for the existing ditch-cutters, chalk-ring latchcoil, and selected wayhouse, with physical clues that describe evidence rather than guarantee current occupants.
2. **Wilderness situations:** roadside cargo, last gleanings, and occupied shelter. Recompose existing owners and budgets; do not add another general quest or encounter framework.
3. **Turnbank wayhouse:** working content name for one small, original surface expedition. A locked front door, a key reachable outside the building, a longer rear entrance, a tier-one buckler, and saved physical aftermath. No delivery objective or obligatory giver return.

Completion means reachable, playable content with honest directions, finite rewards, current-graph authority, native save restoration, and the expected 3D appearance. A content table entry, a staged arena, or a collection of passing isolated tests alone does not establish completion.

### Explicit boundaries

- Preserve the current player spawn, saved player location, approved Spread visuals, existing quest identities, and already-generated saved graphs.
- Preserve Farra's expedition, Ellun/Hallun work, the five regional situation bindings, Morrowfast's authored owners, Stillleaf's archive ownership, and the existing rare pair/latchcoil selections.
- Keep BitLocker dev-only, GlassScorpion neutral, plain Shambler spore-free, Gin Frogs silent, and the settled Urqu rule. No protected mystery answers or new infection vectors.
- No fourth wilderness situation, new biome, underground art rollout, boss, global combat rebalance, global fire/acid change, territorial AI, reputation/crime system, or copied Qud enemy.
- Do not reopen dog fetching or the failed campaign pathfinder as prerequisites. Important item duplication, inaccessible rewards, corrupted saves, or false directions remain release gates.

## 2. Verification sweep and corrections

The original source sweep and subsequent implementation corrections are reconciled below. Focused native test receipts establish only their named invariants; an unobserved gameplay route remains open. §11 lists current source contracts and line references at this checkpoint.

| Premise to avoid | Current evidence | Plan correction |
|---|---|---|
| A frozen rare location proves the encounter exists | `SpreadRareEncounterPlan` selects addresses; `SpreadRareEncounterBuilder.TryPlace` can refuse. Refusal has diagnostics, not a durable cleared/refused state. | Reports are explicitly unconfirmed and historical. Do not infer completion from missing actors, a visited map cell, or a selected address. |
| Dialogue can inspect remote enemies to keep notes accurate | Existing village guidance can enumerate map POIs without generating destinations. Rare actors are mutable cached owners. | New reports use current eligible frozen addresses, never remote actor/stock queries. Notes retain what was heard. |
| Notes require a new journal | `QuestLogUI.AppendNotes` now also reads `SpreadDiscoveryNotes`; existing travel/situation providers remain. | Three fixed historical families use the existing Q/Tab surface. Native 54 validates logic/save boundaries; main19 and ordinary14 separately establish real conversation/remember/Q-Tab/save input and viewed readability. |
| Fields lack a harvest interaction or require relocation | `SelectGleanings` already supplies 1/2/3 ripe rows; `FieldHarvestPart` yields Emberwheat and leaves spent stubble. M3 measured 99 generated composition graphs and 212 ripe owners, all visible from at least one working lane. | Retain actual row positions, deterministic ranking and yield. The older `SPREAD-COMPOSITION.md` prose is corrected. Main19 and ordinary14 now provide viewed gameplay-distance grain/harvest/stubble frames; ordinary14 also consumes the actual earned grain and returns normally. |
| Regional supply jobs still need a generic framework | `RegionalSituations.Definitions` already has two template families at five fixed bindings. Gantry accepts two Emberwheat for six drams and a HealingTonic. | Reuse that request as an optional downstream use, not a new quest or a claim that every field belongs to Gantry. |
| Moving containers earlier is harmless | Actual execution is sorted by priority: population 4000, containers 4100. List insertion order is not execution order. | Step 2 composes already-generated owners after both builders. Do not change their rolling order. |
| After containers means after all random placement | `HaulablePropBuilder` runs at 4200; its random retry count depends on blockers and reserved cells. `TradeStockBuilder` also runs at 4100. | Run both optional composition and Wayhouse staging at 4300, after the final ordinary random placement pass, and validate against actual placed props. Otherwise the same container budget can still change later RNG/haulable output. |
| Every Spread container is freely openable | The pool is Crate/Sack/StrongBox with weights 4/3/1. StrongBox has an iron lock. T1 wilderness budget is 1–2. | Roadside cargo and shelter use only an existing rolled Crate or Sack. Do not unlock or reroll a StrongBox to satisfy a scene. |
| Container destruction guarantees recovered contents | This is not needed or established by this audit. | Opening/taking through existing actions is the supported reward path; prose does not advertise smashing as equivalent. |
| Keys are consumed | `LockPart` matches reusable `KeyPart.KeyId`; an empty key requirement unlocks without a key. | Expedition key/door use a nonempty site-specific key ID. No consumption promise, no generic iron-key bypass. |
| Village doors inherently require a village | `DoorPart.HasAuthority` checks actual owner/actor/zone/permissions; `VillageDoorPlacementBuilder` is the separate village-only placement policy. | Reuse the ordinary door owner/part in the new site through a dedicated builder. Do not broaden village aperture placement into arbitrary zones. |
| A surface reward should be T2 to feel worthwhile | Current Spread surface content is T1; Buckler is already a T1 item with AV 1/DV 1, weight 5, commerce value 20, and a hand slot. LeatherArmor is AV 3/DV −1, weight 15; ChainMail is T2. | Use one ordinary Buckler as the expedition reward. Its hand-slot cost creates a genuine loadout choice. No invented special power or guaranteed improvement for every player. |
| Ordinary return and explicit zone unload are equivalent | Cached graphs preserve ordinary aftermath; explicit ordinary unload permits regeneration. The new Wayhouse hook additionally retains one exact finally accepted cached graph, restored from its saved installation metadata. | Keep ordinary wilderness regeneration policy. No broad retention ledger, live reward reconstruction, or anchor-only authority. Actual untouched/depleted Wayhouse save controls are GREEN. |
| Prior test receipts validate this feature | The 20,406-pass run belongs to the prior first-hour baseline. This implementation has new focused native RED/GREEN receipts. | Preserve named new evidence separately; the baseline is not the final suite for this change. |

### Readiness

| Area | Readiness | Current witness / remaining limit |
|---|---|---|
| Existing rare enemies, equipment, and approved models | Implemented baseline | Preserve selections, behavior and appearance. |
| M1 reports/notes | Native 54/54 GREEN, including the third family | Real eligible informant → explicit remember → complete Q/Tab notes → save restoration passed in main19 and ordinary14; the observed report/journal frames were reviewed. |
| M2 optional pair clue | Native 23/23 plus 80 neighbors GREEN | Cards9 observed the actual 10.2 sign approach and free native reader with the original pair intact. Initial player positioning was disclosed setup; no ordinary rare-site journey or fight claim. |
| M3 gleanings | Native 3 measurement cases GREEN: 99 graphs / 212 ripe owners | Existing positions retained. Main19 and ordinary14 show actual grain/harvest/spent readability; ordinary14 earns/eats one finite grain, returns to Sill and restores its spent/consumed state. |
| M4 cargo/shelter | Native 83/83 GREEN for receipts/composer, job `f0f7ee05c1034d84ab4051713c771a8f` | Natural wiring and matched native census pass: commits 3/8, 4/7, 7/10 selected chunks for seeds 1/64/1729. Cards9 observes real cache/guard models/readers and a 24-input shelter bypass; matched component timing is complete. Separate shelter combat and a Play-mode cache-looting journey were not observed. Both late native EditMode persistence bridges now pass: actual full-capacity refusal, partial taking, cached-away/full-save/exact-return state and guard gear. |
| Wayhouse source/generation/save | Native 50/50 GREEN after final goods guards, including three generated seeds; separate economy3 GREEN | Final main19 observes front and saved-baseline rear branches, actual key/reward/equip and depleted F5/F6 graph. Entry uses disclosed player transfers; cached graph reuse is verified, not a complete ordinary return journey. |
| Ordinary journey / integration | Ordinary grain loop14 GREEN; full sweep plus focused correction complete | No-transfer N → Sill reports → actual field harvest/eat → Sill return → F5/F6 is complete. The originally proposed continuous wayhouse/Buckler journey remains unverified; main transfers do not close it. Full sweep: 20,632/20,633 pass with the sole old fixture failure; post-full native50/50 closes that test correction and both persistence bridges. No runtime delta or full20,635-pass claim. |
| Performance | Current dense profile12 and 60 same-process component samples complete | Bounded CPU costs recorded with controls; allocation data unavailable and rendered UI latency/causal whole-frame neutrality remain unmeasured. See §9 and `Integration/Performance/README.md`. |

## 3. Step 1 — reported places

### 3.1 Player flow

At an eligible living Scribe's regional overview or Innkeeper's rumors node, the implementation projects up to three optional reports: the two existing rare families and the selected wayhouse. Use relative bearings and chunk distance from the actual speaker's current map location. The choice to remember is explicit. Opening the conversation does not accept work, write a note, reveal the destination, advance a turn, or generate it.

Examples of the intended meaning, with bearings filled from the current map:

- “A carrier reported ditch-cutters by an old road, [bearing and distance] from here. I haven't been there to check.”
- “There are old warnings about chalk-ring vipers in the hedges [bearing and distance]. Their bites are poisonous. I don't know what's there now.”

The full reports appear in the existing wrapped conversation body; each remember choice is a short label bounded to 52 characters. `CurrentText` appends the projection without modifying authored `Node.Text`. Do not put a paragraph in a single-row dialogue choice.

Final wording must follow the applicable Spread voice guidance and actual formation. Do not call a hedgerow an old road merely because one family can select either formation. Do not promise an enemy's weapon, a living informant at the destination, a reward, a safe route, or a current sighting.

The journal records “Reported by [speaker] at [origin]” and the reported destination. Bearings remain associated with that origin; do not display old directions as if they came from the player's new position. This is information, not an accepted quest. Include a short “unconfirmed when heard” qualification without burying directions in technical state text.

### 3.2 Admission and note contract

Implemented narrow files: `Gameplay/Conversations/SpreadDiscoveryReports.cs` and `Gameplay/World/SpreadDiscoveryNotes.cs`. `AppendChoices` captures a replaced bounded offer snapshot; `AppendText` supplies wrapped reports; `TryRemember` handles the explicit `RememberSpreadDiscovery` action. Tokens carry a family and monotonically changing revision, so refreshed or ended conversations cannot reuse an old offer.

Admission requires the exact current managed/cached graph; living attached player and speaker; correct conversation owner/backlinks; mutual non-hostility; a recognized conversation/node; a canonical selected surface address that is currently eligible. Recompute all relevant admission at action execution. A stale choice is not authority.

The implemented cap is **three**, with fixed IDs `ditch-cutters`, `chalk-ring-viper`, and `turnbank-wayhouse`, under `SpreadDiscoveryNote.v1:`. Version1 records store family, canonical destination/origin, informant ID/name and formation; reading formats the historical report from those bounded fields. JSON is capped at 4096 characters, names 160, informant IDs 128 and canonical surface IDs 32; malformed/unknown records are ignored without rewriting their bytes. Repeated remembering replaces only that family. Preserve the existing 17-destination village cap and situation notes.

The original two families' wire formats and semantics remain unchanged after the third-family extension. Ordinary player properties carry notes; no binary save-format change, remote graph cache, arbitrary-note framework or live completion tracker is added.

| Destination/speaker state | New report | Existing note |
|---|---|---|
| Eligible frozen selection, ungenerated destination | Unconfirmed report permitted | Retained as heard |
| Selected destination whose placement refused | Same historical/unconfirmed wording; no remote status query | Never marked cleared or rerolled |
| Committed destination, enemies live/dead/fled/disarmed | Same historical/unconfirmed wording | Never changed into a live stock or enemy tracker |
| Visited destination or seen warning | Visit alone changes no completion state | Retained; no automatic “done” |
| Missing/empty/malformed/ineligible selection | No new offer or backfill | Valid old note remains historical |
| Dead/foreign/hostile/moved informant or changed graph | Refuse new acquisition | Retain previously heard record |

### 3.3 Physical clues

Keep the actual latchcoil Signpost and its southern bypass wording. Do not move the snake, rewrite its wake behavior, or regenerate saved sites.

For freshly generated committed ditch-cutter sites, add at most one optional roadside Signpost describing scored shale, dragged scraps, and old warnings. Site it on a legal visible approach, outside immediate forced combat, using the approved existing model. Its text describes durable traces and makes no claim that occupants remain. Add it only after the existing pair commit succeeds; marker failure leaves the existing pair intact and emits a distinct refusal diagnostic. It must not become a new required dependency that suppresses a formerly valid pair or changes frozen selection. Do not retrofit older cached pair graphs or make every regional sign advertise rare content.

No new “remember clue” action is needed in the initial slice: NPCs provide saved reports, while signs are physical observations. A later local-recording feature would be separate scope.

### 3.4 Acceptance

- Obtain all three available report families through real eligible NPC conversations and read complete notes through the existing UI; selection/refusal controls remain distinct from source availability in one seed.
- Paired tests establish identical report wording for ungenerated, placement-refused, and changed remote graphs; opening or remembering must leave destination cache count, discovery, source items, and RNG unchanged.
- Save/load restores exact note contents, with existing village/situation notes unaffected.
- A callback that swaps map, owner, faction, destination eligibility, or player between offering and selecting causes refusal without a partial note.
- Native warning approach remains legible in the approved camera; the ditch clue cannot make the existing encounter more dangerous by blocking its approach.

## 4. Step 2 — three ordinary wilderness situations

These are small, spatially legible uses of mechanics that already work. The primary addition is a decision encountered while walking, not another loot roll.

### 4.1 Shared distribution and exclusions

Use a deterministic feature-specific hash over world seed, canonical zone ID, and a versioned salt. Cargo/shelter selection uses a feature-specific deterministic **one-in-eight hash bucket** within each specified formation. A private paired correction avalanched the hash before modulo 8 after a low-bit correlation produced no shelter in seed 64. Native composer tests are GREEN; the hash rate is not a measured committed-site frequency. Gleanings remain the existing field composition, with no new selection or relocation pass. At most one composed situation per chunk. The three formation sets are disjoint.

Exclude the starting glade, POIs, authored wilderness/multicell scenes, regional situation source and recipient zones, selected rare pair/latchcoil addresses, and the later expedition selection. Refuse unsupported maps/managers, wrong biomes/depth/tier, stale source receipts, reserved approaches, stairs, hazard/liquid cells, and protected stamp interiors/owners. Preserve the existing landmark stamp cap and geography. No universal “replace vegetation” brush.

Fresh generation may use the new composition, including an unvisited zone of an existing world when its current context qualifies. Already-cached/saved zones retain their old owners and positions. This differs deliberately from the once-per-world expedition, which requires new saved selection metadata. Do not recompose during attach, revisit, save restore, examine, or render.

### 4.2 Content cards

| Situation | Location and visible setup | Player decision | Budget and persistent result |
|---|---|---|---|
| **Roadside cargo** | Selected OldRoad chunk; an existing rolled Crate or Sack beside a connected road spur, visible before taking the detour. The main road remains open. | Inspect contents, take selected useful items, compare found equipment, or leave them for later. | Relocate exactly one eligible generated container, contents and IDs intact. Add zero stock, enemies, coins, or containers. Empty/partly emptied owner remains through ordinary return/save. StrongBox does not qualify. |
| **Last gleanings** | Selected FieldStrips chunk; the existing ripe heads are legible beside a connected working lane amid cut rows. | Eat useful food now, carry it, or retain it for a request already learned through normal play. | Exactly the existing 1/2/3 ripe-row budget. One Emberwheat per successful harvest; same saved spent stubble. No regrowth or extra grain. Do not imply Gantry owns these fields. |
| **Occupied shelter** | Selected Fallow/Hedgerow chunk with suitable existing shelter geometry, an existing generated MarlbackScrabbler group, and a rolled Crate/Sack. Open approach views and a traversable bypass outside initial detection. | Approach the cache and risk ordinary aggression, fight, or leave by the bypass. Looting without a fight may be possible if the actual geometry/AI permits it, but is not promised. | Relocate the original 1–2 Scrabblers and one container. Same gear, health, faction, contents, corpse chances, and total counts. No new guard AI, leash, ownership penalty, or guaranteed corpse. |

“Last gleanings” needs no new interaction. The existing ripe/stubble rendering has now been inspected at gameplay distance in the main and ordinary native runs. The executed M3 census covers 99 composition graphs and 212 ripe owners in seeds 1/64/1729. Every ripe owner is visible from at least one working lane; the maximum Chebyshev lane distances are 6/6/7 respectively. Keep the current deterministic row selection, positions, counts and yield. No row-ranking or relocation production change is justified. Those source measurements alone do not establish camera readability; the separate viewed native grain/stubble frames now supply that evidence. A prior BerryBush image remains unrelated to grain acceptance.

“Occupied shelter” means enemies occupying a place. It does not establish territory, a challenge dialogue, warning-before-attack behavior, or a rule that they defend only their cache. If the ordinary population roll produces Vipers instead, or the rolled container is unsuitable, leave the chunk ordinary. Never reroll for a preferred composition.

### 4.3 Small implementation seam

Use the existing population and container builders unchanged for rolling. The implemented optional per-build receipts capture exact successfully produced owners, tied to the source zone/reference/revision and factory. Capture also verifies that the produced BlueprintName still equals the rolled blueprint after factory callbacks. Population receipts must distinguish the ordinary hostile group from ambient actors and rare sources. Container receipts must exclude landmark containers. Receipts are transient generation data, not a serialized population registry.

The implemented `SpreadWildernessSituationBuilder` has priority 4300, after haulables 4200, and may re-site only those exact owners. Its native 83 scope validates receipt/composer behavior; natural pipeline registration and the separate matched native whole-pipeline census now pass. These samples establish conservation for their measured cohorts, not a gameplay route. It takes the two source builders/receipts explicitly; it cannot scan the whole zone by blueprint name and assume ownership. For cargo choose the first qualifying container in deterministic generation order, not the most valuable. For shelter preflight the group, container, destinations, sightlines, and bypass as one transaction, with already-placed heavy props included as obstacles. Do not move, delete, or reroll those props to admit a situation.

Preflight the complete footprint before moving any owner. Preserve exact original positions for rollback. Recheck graph, part backlinks, eligibility, original contents/equipment, destination legality, and source revision after callbacks. If admission or final verification fails, restore only owners still owned by this transaction; never delete or move a foreign replacement. Refusal is a successful optional no-op, not a failed zone-generation attempt. `ZoneGenerationPipeline` can retry/clear a whole zone when a builder returns false, so that is the wrong signal for an optional composition.

No extra caller-RNG draws: ordinary population/container/haulable rolls occur before composition with their existing sequence. Within one generated graph, capture exact owner/item IDs and values before relocation and prove they remain afterward. Across separately generated baseline/candidate cases, compare seeded roll/content shapes, haulable counts/positions, and the next RNG draw after the complete pipeline; unrelated newly allocated GUIDs need not match. Accepted relocation deliberately changes only selected situation-owner positions. A source-receipt change must be inert in every other biome and every unselected Spread chunk.

If clean source receipts require a broad population rewrite, stop that approach. Keep the simple field slice, document the obstacle, and redesign the receipt seam before touching unrelated generators.

### 4.4 Acceptance

- Census selected/eligible/committed/refused counts separately; never advertise the hash rate as realized frequency.
- Cargo and shelter preserve rolled counts, item identities, actual contents/value, loadouts, and caller RNG. Empty/full/locked/wrong-source controls prove the selection is not vacuous.
- Native views show approach and the available choice for all three cards. The observed grain route includes earned use and spent aftermath; cargo/shelter cards preserve stock and show actual cache readers, not partial looting. Cards9 crosses the shelter bypass in 24 real paid inputs. Screenshot emptiness alone is insufficient; separate shelter combat/cache-looting aftermath remains an unverified experiential limit in §8.
- Full inventory leaves a recoverable item/state according to the existing action contract, with no duplicated harvest or lost cache content.
- Partial looting, harvesting, death, travel, and save/load preserve the spent state. Explicit regeneration of ordinary wilderness is still the existing regeneration policy; do not claim a new global depletion ledger.

## 5. Step 3 — Turnbank wayhouse expedition

### 5.1 Experience and distinction

An old roadside wayhouse has a locked front entrance and an open rear service approach. A single ordinary MarlbackScrabbler occupies the outside key station. Inside is one buckler left in a crate. An exterior notice describes the rear approach and that the front key was kept at the outside station; it does not promise that supplies or occupants still survive.

The short route approaches the key station, risks combat, takes the physical key, and opens the front door. The longer route follows an unbroken exterior lane around an opaque wall to the rear opening and reaches the same interior reward without requiring a kill, a key, a consumable, fire, or a special skill. The reward is earned by navigation or confrontation rather than returned to a giver for payment.

This differs from the shelter card through a deliberately composed building, a key/door interaction, two connected routes to one finite item, an optional shortcut, and a protected return state. It differs from Farra and the regional recovery jobs because nothing must be delivered and no second payout exists. It is not another sealed archive or unique artifact vault.

### 5.2 Placement and layout contract

Select at most one eligible OldRoad/Fallow Spread T1 surface zone in a new world, using its own deterministic salt after existing rare selections are frozen. Exclude both rare addresses, authored areas, glade/POIs, regional bindings, protected route sources, and ineligible geometry. Prefer a destination within a small walking region of the starting settlement (target 2–4 map steps) if an eligible candidate exists; otherwise admit a farther eligible candidate and report its true distance. Do not force a fake nearby coordinate or move the player. Census actual travel distance before finalizing the rank rule.

Save selection under a separate versioned world property such as `SpreadWayhouse.Selection.v1`. Freeze empty selection explicitly. Missing/malformed metadata on an old save means disabled; no retroactive selection or cached-graph backfill. A selection whose later placement refuses stays selected/unrealized; no reroll to a new address. Existing pair/viper rank/order/properties must remain byte-for-byte unchanged.

The implemented mask uses a **7×7 room plus separated exterior notice/key/guard stations** within bounded candidate margins of the 80×25 zone. Empty gaps between stations do not claim reserved working lanes. The earlier 23×15 proposal was not imposed as a solid footprint. Geometry belongs to the builder, not the renderer. Choose the actual dry footprint at priority 4300, after ordinary hazards, population 4000, containers/trade stock 4100 and haulables 4200. Source receipts identify the ordinary owners that may be replaced; no early suppression/credit flags are needed. Require both external approaches and an interior reward-access cell. Use existing StoneWall/ground/VillageDoor/Signpost/Crate/Sack/IronKey/Buckler/MarlbackScrabbler owners and their approved presentation.

The final layout must prove:

1. Front notice can be read before initial forced detection/attack.
2. Outside key station is reachable without entering the locked building. The key is never behind its own lock or carried by the only actor that might fail to drop it.
3. The longer rear lane reaches the reward with the front door locked and the guard alive. Test actual sight/occlusion and turn movement; do not assume that “farther away” means undetected.
4. The guard uses normal aggression and may move/chase. The rear route is initially viable, not guaranteed safe forever after the player alerts it.
5. Neither door nor solid reward crate can seal the sole return path. The player can leave by the same rear route.
6. Late staging validates already-placed hazards/props, and final acceptance after fresh-generation callbacks verifies both approaches, the door aperture and interaction cells remain usable.
7. Previously connected border exits, reserved cross-chunk routes, and stairs remain connected after the complete pipeline and fresh-generation callbacks. Compare their connectivity before site staging and afterward; two working internal entrances alone do not prove the new walls preserve the wider zone.

Do not clear existing structures, ripe/harvestable rows, liquids, stairs, or quest owners to fit the site. Initially admit only a complete supported footprint using a narrowly enumerated disposable ground/scrub set; snapshot any permitted replacements for rollback. If this produces too many refusals, reduce the footprint or change candidate selection after evidence, rather than bulldozing protected content.

### 5.3 Owners, reward, and economy

Create exactly one ordinary MarlbackScrabbler guard with its actual normal loadout. The original ordinary hostile group has already rolled and been placed. Successful late site commitment replaces only its exact receipt owners; no early population suppression occurs. Frozen rare/wayhouse source exclusions are rechecked at commitment. Optional refusal preserves the original group and graph.

The site reward is **one actual Buckler**, in a deliberately stocked Crate. It is not a new blueprint, a global loot-table change, or a second reward roll. The outside Sack contains exactly one IronKey instance whose `KeyPart.KeyId` is set to the site's nonempty key ID. Preserve the key's existing NoTrade tag. Apply the matching key ID to a LockPart on the front ordinary door; leave the rear opening walkable. Keep the door owner unowned (`OwnerId` empty), closed/locked initially, and correctly oriented. Use existing operation authority and successful action costs.

Give the exact key instance the display name “Turnbank wayhouse key” and an examination description of its matching front-door mark. Add an instance ExaminablePart if the existing key lacks one. Preserve blueprint, glyph/model, NoTrade, and the site's actual matching KeyId. Generic iron keys must remain visibly distinguishable. Save/drop/pickup tests preserve both the identifying text and unlock behavior; do not rename all IronKey blueprints.

The crate replaces the first exact owner in the ordinary container receipt and that owner's already-rolled stock; the key-only sack is explicit functional furnishing. With a normal T1 allowance of 1–2, zero or one original ordinary containers remain. The allowance and random stock roll exactly once before late replacement. There are no future stock passes for these staged furnishings, and attach/revisit/load never call `StockContainer` for them.

Record this as a deliberate local economy change: a fixed value 20 T1 Buckler instead of one random container's contents, plus the nontradeable key, once per world. Count the site furniture separately from random loot containers. Compare total acquired/stocked value and gear count against the same seed baseline; do not claim “zero economy impact” merely because the container count is bounded. Native sale price remains the actual merchant quote, not commerce value 20.

A player can compare/equip the buckler, keep it, leave it, or sell it through normal trade. The plan promises no universal AV/DV gain: hand occupancy and current equipment matter. The guard drops only its actual carried/equipped items plus existing death behavior; no special buckler duplicate on death.

### 5.4 Transaction, discovery, and persistence

Use a narrow `SpreadWayhousePlan` plus `SpreadWayhouseBuilder`, not an extensible dungeon framework. Stage all required owners, validate shapes/backlinks/unique IDs, preflight the entire footprint and budget packet, then commit. Recheck after factory callbacks. On any failure roll back owned changes and continue ordinary generation, with no half-door, duplicate key, orphan reward, consumed container allowance, or suppressed ordinary hostile group.

Distinguish the builder's provisional packet from final installation. Source receipts and staged owner/removal snapshots are attempt-local; no new early budget-credit or suppression state exists. A retry must not carry an abandoned packet into another graph or alter another builder's reservations. After the whole pipeline and `OnZoneGenerated`, validate the surviving packet in the existing `CommitGeneratedZone` acceptance seam, preserving existing lair acceptance. Only a finally accepted/cached graph supplies durable installation authority; do not write a world-level committed latch from the early builder or count an abandoned staged anchor. If a later callback corrupts the packet so an ordinary fallback cannot be safely restored, reject the unsafe graph through final acceptance and diagnose it, rather than cache a partial site or silently reroll it. That rare failure remains a defect to resolve, not an ordinary optional-placement refusal. Save binding serializes only accepted state.

The third historical report family is integrated with the site selection contract and covered by native 54 M1 tests. A report is still unconfirmed when heard; it must not create the zone or promise that a selected footprint successfully built. The exterior notice is local material guidance. No new quest ID, quest reward latch, automatic acceptance, or remote completion label.

The inert local anchor stores role, layout coordinates and version; it does not store a second ledger of live owner IDs or reconstruct inventory. Durable installation metadata plus the exact finally accepted cached graph establish retention authority, independent of guard/container survival. Actual door lock/open state, key, crate contents, damage, corpses, and dropped equipment remain on the normal entity graph. Do not rebuild them from the anchor on attach.

Extend `OverworldZoneManager.CanUnloadZone` narrowly to retain the exact one committed wayhouse graph belonging to its saved selection/current manager, in addition to existing lair retention. A local marker alone cannot pin a graph. Missing/malformed saved selection/installation metadata, a wrong address or a foreign graph cannot obtain the accepted graph's retention authority. Keep an already committed graph retained even if later map eligibility changes; eligibility controls new installation, not deletion of saved aftermath. This closes the explicit-unload duplication path for the once-per-world fixed reward. Refusal triggers no automatic retry or source move. If an unrealized ordinary zone is explicitly regenerated under the existing policy, it may try the same frozen address; successful commitment then activates retention. No new global “cleared” flag or reward respawner.

Save/load must preserve locked/unlocked and open/closed states separately, exact item IDs and ownership, partial theft of contents, destroyed/missing owners, living/dead guard, and the anchor. The note is historical even if all physical evidence is destroyed. Saving an old world must not silently initialize the new selection.

### 5.5 Acceptance

- Both approaches work from an intact site with actual gameplay camera/input. The executed main mode uses a real saved-baseline rear branch, then restores that baseline for the front branch; it does not claim two independently generated fresh journeys. The observed rear route needs no kill or supplied debug item.
- Wrong/no key does not unlock the front; the actual site key does; generic iron keys do not. Opening/unlocking uses normal successful action costs and refuses stale/remote/blocked actors.
- Site-key name/examination survives inventory transfer and save/load; a generic key remains distinct. Whole-zone border/stair connectivity survives the site, including later placements and callbacks.
- One buckler exists initially; acquisition, drop, trade, death, save/load, return, and refused explicit unload do not produce another. No remote receipt reconstructs it.
- A full inventory, occupied doorway, missing blueprint, failed placement, and factory callback graph swap all preserve valid ownership and budgets.
- Original experiential target: one ordinary discovery → wayhouse travel → chosen approach → Buckler acquisition/equip or sale → travel back → save/load → return-to-site journey. This exact journey was not performed. The bounded accepted witness splits into the real ordinary grain loop14 and the disclosed-transfer wayhouse main19; §8 records this scope divergence rather than marking the original journey complete.

## 6. Implementation sequence and ownership

Each row is a reviewable milestone, not permission to skip its RED or native gate. Later work must build on the finished contract of earlier work.

| Milestone | Work and likely files | Depends on | Exit gate / approximate size |
|---|---|---|---|
| **M0 — audit/census** | Baseline3-seed source census and initial source-selection RED | None | Native 3 census cases pass; selected/eligible/committed meanings stay distinct. |
| **M1 — reports/notes** | Implemented report/note classes; narrow conversation/actions/journal hooks and4 fixtures | M0; final third family also depends on WayhousePlan | Native54 GREEN; real informant/journal and notes save/load witnessed in both completed runs. |
| **M2 — rare clue** | `SpreadRareEncounterBuilder.TryAddPairClue`; current Signpost; clue fixture | Existing pair source | Native 23+80 neighbors GREEN. Cards9 observes the actual sign approach/read with the original pair intact; initial player transfer is disclosed. |
| **M3 — gleanings measurement** | Measurement fixture/3 receipts; correction to `SPREAD-COMPOSITION.md` | M0 | Existing99 graphs / 212 owners satisfy lane visibility; no production relocation. Native harvest/spent views and ordinary earned-grain consumption/return/save-load witnessed. |
| **M4 — cargo/shelter** | Receipt helper,4 producer/terrain hooks, late composer, fixtures, root-owned pipeline wiring | M0/M3; excludes Wayhouse selection | Native 83 GREEN. Natural wiring and matched 3-seed native census GREEN; cards9 and its actual 24-input shelter bypass are complete; separate shelter combat/cache-looting witness remains unverified. |
| **M5 — Wayhouse** | Plan/builder, world save/manager hooks, source/packet/route/save fixtures | Receipt foundation; existing rare selections | Native 50 GREEN after final goods guards, including three generated seeds. Both native approaches and front key/reward/equip/depleted save-load witnessed in main19; economy census3 GREEN. Continuous ordinary wayhouse travel/return is unverified. |
| **M6 — discovery/acquisition witness** | Existing third report family; root-run finite native driver/launcher, exact restoration rows and viewed images | M1–M5 | Main19/19 passes with disclosed field/site transfers and a separately verified rear branch; ordinary14/14 uses normal travel for a complete grain acquisition/use/return loop. Cards9 separately observes cargo/shelter/sign layouts and a real shelter bypass; §8 names the outstanding experiential differences. |
| **M7 — integration/closeout** | Full current native suite, integrated census/performance, exact review and docs | M1–M6 | Verified within stated limits: full20,633 returned20,632 PASS/1 old fixture FAIL; focused50/50 then passed the test-only correction and late persistence2. Recorded runtime inputs stayed unchanged. Current profile/component measurements are complete; no full20,635-pass result or unperformed wayhouse/combat journey is claimed. |

### Reviewable commit boundaries

These are dependency boundaries, not instructions to stage unresolved changes. The accompanying exact path inventory records each source owner and current hash; shared manager/save/restoration files need hunk-aware review.

1. **Local discovery/clue and retained field facts:** M2 source/tests plus M3 measurement/doc correction are independently reviewable and introduce no Wayhouse type dependency.
2. **Historical reports and complete Wayhouse source/save:** commit the final M1 three-family code together with the receipt foundation and Wayhouse plan/builder/manager/save contracts. Final M1 references `manager.Wayhouse`, so do not split it ahead of that type. Include their actual RED/GREEN and final authority correction receipts. M6 records the tested split scope: main19 verifies the generated site with disclosed entry transfers; ordinary14 verifies a separate grain round trip. A complete ordinary wayhouse journey remains unverified.
3. **Natural cargo/shelter integration:** composer plus exact pipeline wiring/selection/exclusions, its tests, before/after census and bounded search evidence; depends on the receipt foundation. Native component83 is evidence for the composer, not permission to omit natural wiring or scene acceptance.
4. **Cross-feature native acceptance and closeout:** owned driver/launcher/restoration additions, actual run reports, inspected images, full current suite and final living docs. If kept in earlier feature commits, preserve the same dependencies and honest pending status. Do not stage unrelated scene/art/editor/log changes or private candidates.

Root may consolidate boundaries 2–4 after the integrated gates to avoid intermediate advertised-but-unverified content. Exact per-path ownership accompanies this private revision; it does not grant ownership of whole directories or unrelated hunks.

Implementation delegation may audit notes, content compositions, and expedition tests independently. One owner coordinates shared Objects.json, pipeline, save hooks, and Unity Editor use. Do not run simultaneous Editor mutations/tests from different agents. Every new Assets file receives a fresh `.meta`; if blueprints change, edit surgically and compare parsed before/after objects. Default design needs no new enemy, item, or model blueprint.

### Current architecture and remaining integration boundary

```text
new world -> existing rare selections -> independent wayhouse selection -> saved world properties

fresh eligible Spread zone:
  terrain/composition -> connectivity/stairs/landmarks/hazards (existing sorted priorities)
  -> ordinary population 4000 (existing rare-source replacement remains separate)
  -> ordinary container stock + trade stock 4100 -> haulable props4200
  -> selected wayhouse 4300: exact receipt replacement or unchanged ordinary fallback
     OR selected cargo/shelter 4300: exact owner relocation (root wiring and matched 3-seed native census GREEN)
  -> OnZoneGenerated -> original staged-plan final validation -> accepted cache/attachment

conversation -> current eligible map/selection projection into wrapped body + short choices
explicit remember -> exact context/token revalidation -> one of3 historical player notes
read journal -> stored historical note only
return/load -> accepted saved graph only; no placement, reward refill or regeneration
```


## 7. Tests and adversarial matrix

For each implementation milestone: write and execute a failing behavioral test first, implement the minimum, run GREEN, then paired counter-checks and a dedicated adversarial pass. Add new Assets test `.meta` files. Do not inflate the suite with tests that only repeat constants or implementation text.

Actual focused suites: `SpreadDiscoveryReportsTests`, `SpreadDiscoveryTextTests`, `SpreadDiscoveryAdversarialTests`, `SpreadDiscoveryWayhouseTests`; `SpreadRarePairClueTests`; `SpreadGleaningLaneMeasurementTests`; `SpreadGenerationReceiptTests`, `SpreadWildernessSituationTests`, `SpreadWildernessSituationAdversarialTests`, `SpreadWildernessPipelineTests`, `SpreadWildernessPipelineCensusTests`; `SpreadWayhouseTests`, `SpreadWayhouseAdversarialTests`. Source/save controls live in those fixtures; no nonexistent standalone Notes/WayhouseSave fixture is claimed. Native driver/restoration and natural pipeline tests remain separate evidence.

| Invariant | Positive witness | Paired rejection / adversarial coverage |
|---|---|---|
| Current authority | Attached live same-graph player/NPC/source | Same IDs on a foreign graph, stale cache/map, wrong part parent, dead/hostile/blocked actor, moved target; swap context inside callbacks |
| Reading is free | Open dialogue/examine/Q without state change | Repeated opening and malformed notes; no note write, RNG consumption, time advance, hidden map reveal, or destination generation |
| Reports remain truthful | Correct canonical destination and origin-relative bearing | Old/empty/ineligible selection, refused placement, dead remote owners, changed map; no live-status leak or “cleared” inference |
| Notes are bounded | Exactly three fixed families, prior two formats preserved | Duplicate actions, invalid enum/family/address, excessive strings, malformed JSON, corrupted prefix; preserve other note providers |
| Composition spends no new budget | Same owner/item multiset and RNG as baseline | StrongBox/Viper roll, absent receipt, wrong build revision, stamp-owned container, protected footprint, no candidate; refuse rather than reroll |
| Grain is finite | Existing ripe row→one actual grain→spent row | Repeated action, inventory full, owner removed/swapped, harvest before/after save; no extra ripe rows or regrowth |
| Placement is atomic | All owners/lane cells valid at final commit | Factory failure after first owner, add/move failure, callback ownership theft, ID collision, graph replacement; rollback only still-owned objects |
| Installation survives only accepted generation | Manager retains the original staged plan until final acceptance | Plan replacement, repeated build, changed packet and callback rejection cannot erase validation or save a committed latch; border/stair connectivity preserved |
| Two approaches are real | Front key route and rear no-key route | Front locked with guard alive; no consumables/gear grants; blocked rear footprint refuses site rather than making a combat-only room |
| Door/key semantics | Matching carried site key, adjacent permitted actor | No/wrong/generic key, inventory alias, remote actor, closed/occupied aperture, incompatible owner; key not consumed |
| Reward identity | One actual Buckler acquired/equipped in main19; no native sale claimed | Reopen, repeated pickup, inventory overflow, death, explicit unload, load/attach, destroyed cache; never regenerate stock |
| Saved source is stable | Nonempty and explicit-empty new-world metadata restore | Missing old property, malformed version, changed biome/POI, stale derived references; no late selection/backfill |
| Retention is bounded | One exact committed wayhouse survives unload | Foreign/forged anchor, wrong address/revision, other ordinary zones and existing lairs; no broad cache pinning |
| Visual coverage is complete | Notice, guard, key, door states, crate, buckler, stubble/corpse seen | Missing model/fallback, wrong orientation, hidden approach, stale spent view; actual game camera not just an asset gallery |

Also inspect every applicable category in `ADVERSARIAL_TESTING.md`, including aliases, reentrancy, turn charging, overflow, teardown/static contamination, source invalidation, and reload. Record a justified N/A for categories that do not apply; do not manufacture unrelated mechanic work to fill a checklist.

## 8. Native proof and evidence discipline

Use focused native EditMode tests during each slice. At final integration, run the current full native suite once code/data/art settle and compare named failures against a current baseline. Do not assert a fixed expected count from the old 20,406 receipt. If only the standalone runner is available, use its current README and a controlled before/after named-result comparison; do not judge by raw environment failures or claim it proves Unity import, rendering, input, or real-time performance.

The original Play-mode acceptance targets were the following. Their executed disposition follows immediately; this list is not a claim that every experiential branch was performed:

1. Real Scribe/Innkeeper report → explicit remember → complete Q note → travel changes origin context → save, change state, load exact restoration.
2. Cargo approach/open/partial take/return; field harvest/spent view; shelter approach/bypass and separate actual combat case. Capture before-choice/after-choice frames with exact zone/seed and owner IDs.
3. Wayhouse front key route, including wrong-key refusal; separate rear route with front still locked and guard alive. Observe movement and actual detection, not a static path claim.
4. Ordinary start → discover a legitimate lead → travel without teleport/debug health/items → acquire the real buckler → compare/equip or sell to an actual living merchant → return → save/load → revisit depleted site. Record any legitimate supplies used and merchant price. If a living merchant cannot be reached, the equip/use branch is sufficient for this journey; do not invent a sale receipt.

### Executed scope and explicit divergences

| Original target | Actual completed evidence | Unverified limit / scope divergence |
|---|---|---|
| Reports, notes and save restoration | Main19 (final `0cc2ff0982444546a7ebfc9a1fccef03`) and ordinary14 (`c3bd858b36ad4da3bd1c7205e20359b4`) use real NPC choices, all three notes and native F5/F6. Actual text/choice/journal frames reviewed. | One seed and observed UI sizes; no universal display/name-length claim. |
| Cargo, field and shelter decisions | Cards9 (`ca59dcb2ac6f46b59474d5b5ee753e8d`) observes real cargo6.9 and shelter10.7 current cache/guard models and free readers. It crosses the shelter from `(25,0)` to `(25,24)` in 24 real paid keys with exact selected stock/gear retained. Main/ordinary show real field harvest/stubble; ordinary consumes the earned grain. | Cargo/shelter initial views and shelter starting border use disclosed player-only transfers. No Play-mode cargo/shelter cache-looting/return journey or separate shelter combat run was performed. The two late native EditMode bridges now prove actual full-capacity refusal, partial taking, cached-away/full-save/exact-return anchors/stock/gear; these are source/action/save proofs, not substitutes for those experiential witnesses. |
| Wayhouse front/rear approaches and finite reward | Final main19 uses actual native front combat/key/unlock/open/reward/equip, plus a separately saved/reloaded live-guard rear branch with the front initially locked. It restores exact depleted graph/notes with F5, a real unsaved step and F6. | Main field/site entries are disclosed original-player-only transfers. Rear/front share an actual saved baseline; cached `GetZone` reuse is not an observed exit-and-return trip. No merchant sale claimed. |
| One continuous ordinary discovery/acquisition/use/return journey | Ordinary14 completes N → Sill actual reports/notes → native field11.8 → one actual harvest/Eat → native Sill return → F5/F6. It uses no transfers or source/HP/item grants. | This intentionally uses the finite grain loop to close the bounded ordinary acquisition/use witness. The original continuous **wayhouse/Buckler** journey, including ordinary travel back and return-to-site, remains unverified. Eating at full HP proves consumption, not healing benefit. |
| Optional physical pair clue | Cards9 reads the actual 10.2 historical Signpost with its original pair intact; root viewed its native reader. | Initial player positioning is disclosed setup. No rare-pair fight or natural discovery journey is claimed. |

These are explicit acceptance-scope divergences under the bounded execution priority, not silent substitutions or proof that the unperformed routes are broken. The implementation retains the actual ordinary mechanics; the remaining limits concern what this milestone observed. Raw input reports, exact setup disclosures and reviewed images are under `Docs/Verification/SpreadDiscoveryExpeditions/Native/` and `Integration/native-visual-review.json`. Integrated evidence is now the completed20,633-case sweep (20,632 pass; one old fixture failure) plus post-full native50/50 for the test-only correction and late persistence bridges. No production changed between them; the current20,635 registry is not a single full-pass result.

The ordinary journey should be a bounded input route, not another general campaign bot. Two failed harness approaches or roughly 20–30 minutes without new useful evidence trigger a severity review. A harness failure alone is an unverified witness, not proof the game is broken. Keep that completion gate open and continue independent work; do not mark M7 fully complete without a genuine route or an explicitly reported outstanding requirement.

Restore the user's editor/play/save/input state after tests. Existing permission permits stopping the play session for testing when necessary; do not repeatedly ask. Inspect actual retained frames. Raw state assertions can establish ownership/turn/save facts; they cannot establish visual quality, readability, feel, or balance without the corresponding views/play observation.

Evidence directory: `Docs/Verification/SpreadDiscoveryExpeditions/`, with source/seed manifest, RED/GREEN receipts, native input/output, inspected frames, save identity checks, census, and a short limitations README. Avoid checking enormous incidental logs into the content commit.

## 9. Performance, balance, and diagnostic budgets

All new planning/placement belongs to cold generation; report projection belongs to explicit dialogue; notes belong to explicit reads. No full-world/zone scans per frame, per render item, or every turn. No destination generation from a report. Bound reports to three, site selection to the existing map size, candidate footprint search to the local zone, and relocation to the tiny ordinary source receipt.

Diagnostics should explain selection and refusal without logging every empty tile: one structured summary per attempted situation/site with family, zone, selection revision, committed/refused reason, source IDs, intended versus actual counts, and relevant budget substitution. A selected address, committed source, and observed player action are separate events. Reuse existing door/unlock/loot/harvest diagnostics where adequate.

Before/after census for seeds 1, 64, 1729 records eligible chunks per family, selected/realized/refused sources, container/group/ripe-row counts, equipment/content identities and stock value, exclusions, route distance, and ordinary route reachability. Add synthetic no-candidate/callback/refusal cases separately. Three seeds are acceptance samples, not a balance claim or proof of every generated world.

The prior first-hour performance capture measured frame mean 5.112 vs 4.252 ms, frame p95 7.199 vs 4.014 ms, and renderer redraw p95 53.865 vs 43.007 ms in noncontemporaneous Editor captures. This is an unresolved measured follow-up, not demonstrated attribution to rare selection. Preserve it in reporting. Current evidence adds a dense native profile12 and 60 alternating same-process component sample rows covering report projection, note reading and cold generation. These satisfy the bounded component measurement, not rendered dialogue/journal latency or an old-build whole-frame comparison. Native allocation counters are unavailable; zero counter output is not zero allocation. `Integration/Performance/README.md` records exact samples and limits. Performance neutrality remains unclaimed; a causal claim would require matched scene/camera/settings/actions and source hashes. If a new hot-path scan/rebuild is found, fix it before shipping. Do not derail all content work chasing noise without a reproducible significant defect.

## 10. Risk triage and shipping procedure

**Must fix before declaring the affected milestone complete:** save corruption, duplicate fixed reward, inaccessible required key/reward/exit, stale-owner mutation, partial placement changing ordinary budgets, altered old rare selection, global rendering fallback, unreadable required directions, or a rear route that cannot actually avoid forced initial combat.

**Bounded follow-ups:** decorative prop variety, optional clue failure with the original encounter intact, low realized composition frequency that still meets an honestly revised design target, noncritical animation polish, and unrelated dog-fetch/harness defects. Record severity, reproduction, failed approaches, player impact, evidence, and next experiment. Do not spend repeated unbounded attempts on these while independent useful work remains.

If the footprint or bypass repeatedly fails its feasibility tests, reduce this one site's geometry and re-run the route proofs. Do not add teleporters, invulnerability, forced pacification, extra player health, or a global AI change to manufacture success. If the key/door flow reveals a substantial authority defect, fix the narrow underlying defect with its own RED/counter-checks; that is significant functionality.

Implementation is authorized and underway. Inspect current git/editor state before each publication and preserve unrelated changes. Root coordinates all shared source publication and native editor execution. Use coherent commits with this living document updated in the same commit, the `CLAUDE.md` §2.3 template, explicit scope divergences, actual test evidence, and in-phase Q1–Q4 review. Existing authorization permits main pushes after fetching/rebasing and verifying the integrated result. A private documentation candidate or path inventory is not staging permission; root reviews exact integrated paths/hunks and commits after gates.

## 11. Source inventory for implementation recheck

Read symbols, not just historical summaries; line numbers can move. The following implementation references were checked at this checkpoint. Original dependency paths remain listed afterward. Native evidence is described separately from source inspection.

| Current implementation (line at checkpoint) | Contract |
|---|---|
| `Assets/Scripts/Gameplay/Conversations/SpreadDiscoveryReports.cs:42` | AppendChoices / AppendText / TryRemember: 3 bounded current offers, wrapped body, explicit revision-bound write |
| `Assets/Scripts/Gameplay/World/SpreadDiscoveryNotes.cs:11` | Separate v1 fixed-family historical properties; Read formats without remote generation |
| `Assets/Scripts/Gameplay/World/Generation/SpreadWildernessSituationPlan.cs:20` | Versioned selection and exact produced-owner receipt contract |
| `Assets/Scripts/Gameplay/World/Generation/Builders/SpreadWildernessSituationBuilder.cs:14` | Late 4300 no-factory/no-RNG exact-owner relocation, virtual geometry and rollback |
| `Assets/Scripts/Gameplay/World/Generation/SpreadWayhousePlan.cs:17` | Staged final validation; independent saved selection/installation and exact graph retention |
| `Assets/Scripts/Gameplay/World/Generation/Builders/SpreadWayhouseBuilder.cs:32` | Late 4300 receipt replacement; fresh stock admission and bounded two-route packet |
| `Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs:165` | Mutually exclusive selected Wayhouse/composer registration; final acceptance pins original StagedWayhouse |
| `Assets/Scripts/Gameplay/Save/SaveSystem.cs:348` | World metadata save and restored graph binding; no reward reconstruction |
| `Assets/Scripts/Gameplay/World/Generation/Builders/SpreadRareEncounterBuilder.cs:59` | Optional clue after pair commit with separate provenance; earlier latchcoil warning preserved |
| `Assets/Scripts/Gameplay/World/Generation/Builders/SpreadCompositionBuilder.cs:28` | Existing finite row selection retained; exact SourceZone receipt context added only |

| Existing source | Relevant contract |
|---|---|
| `Assets/Scripts/Gameplay/Conversations/RegionalGuidance.cs` — Build/BuildDestinations/TryRemember/RegionalTravelNotes | Existing village offers, four-destination projection, explicit remember, existing note cap |
| `Assets/Scripts/Gameplay/Conversations/FirstHourGuidance.cs` — TryContext/Live | Exact graph/part/backlink/liveness discipline |
| `Assets/Scripts/Gameplay/Conversations/ConversationManager.cs` and `ConversationActions.cs` | Choice injection and action dispatch |
| `Assets/Scripts/Presentation/UI/QuestLogUI.cs` — AppendNotes | Existing note aggregation/wrapping/paging |
| `Assets/Scripts/Gameplay/Entities/RegionalSignpostPart.cs` | Geographic read-only sign directions; no notes/quest assignment |
| `Assets/Scripts/Gameplay/World/Generation/SpreadRareEncounterPlan.cs` | Independent frozen selections, current eligibility, missing/empty restore semantics |
| `Assets/Scripts/Gameplay/World/Generation/Builders/SpreadRareEncounterBuilder.cs` | Atomic rare-source placement/refusal, actual gear, latchcoil sign, source provenance |
| `Assets/Scripts/Gameplay/World/Generation/Builders/PopulationBuilder.cs` | Priority 4000, ordinary roll, success-only rare hostile-group replacement |
| `Assets/Scripts/Data/Tables/PopulationTable.cs` — SpreadTier1/SpreadTier1Encounter | Existing ordinary group and ambient population |
| `Assets/Scripts/Gameplay/World/Generation/Builders/ContainerBuilder.cs` and `Assets/Scripts/Gameplay/World/Generation/ContainerPlacementService.cs` | Priority 4100, weighted kinds, one allowance, stock/fallback, tier budget |
| `Assets/Scripts/Gameplay/World/Generation/Builders/HaulablePropBuilder.cs` and `TradeStockBuilder.cs` | Later generation/RNG dependencies; haulables 4200, trade stock 4100 |
| `Assets/Scripts/Gameplay/World/Generation/Builders/SpreadCompositionBuilder.cs` — SelectGleanings | Current 1–3 finite ripe-row selection, no caller RNG |
| `Assets/Scripts/Gameplay/Farming/FieldHarvestPart.cs` | Actual harvested item and spent-row persistence |
| `Assets/Scripts/Gameplay/World/Generation/Builders/LandmarkBuilder.cs` | Existing Spread stamps, cap, protected footprint concerns |
| `Assets/Scripts/Gameplay/World/Generation/ZoneGenerationPipeline.cs` | Sorted priorities and whole-zone retry behavior |
| `Assets/Scripts/Gameplay/World/Map/ZoneManager.cs` — GetZone/UnloadZone/CanUnloadZone | Cached graph reuse versus explicit regeneration |
| `Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs` — CreateSpreadPipeline/CreateSurfacePipeline/OnZoneGenerated/CanUnloadZone | Exact current map/source pipeline, fresh-only hooks, exact Wayhouse staged acceptance and existing lair/Wayhouse retention |
| `Assets/Scripts/Gameplay/World/RegionalSituations.cs` and `RegionalSituationNotes.cs` | Five authored bindings, source placement, historical transaction notes |
| `Assets/Scripts/Gameplay/World/MorrowfastExpedition.cs` | Existing voluntary marked-parcel journey and single reward; preserve it |
| `Assets/Scripts/Gameplay/World/DoorPart.cs` and `Generation/Builders/VillageDoorPlacementBuilder.cs` | Ordinary operation authority vs village-only placement policy |
| `Assets/Scripts/Gameplay/Items/LockPart.cs`, `KeyPart.cs`, and `ContainerPart.cs` | Reusable exact-key matching, lock state, actual container interaction |
| `Assets/Scripts/Gameplay/Save/SaveSystem.cs` | World-selection binding, player/entity properties, normal graph serialization |
| `Assets/Resources/Content/Blueprints/Objects.json` | Actual Marlback, door, sign, container, key, Buckler/armor, and crop definitions; edit surgically only |
| `Assets/Resources/Content/Data/Loot/LootTables.json` — FindArmorT1 | Existing tier-one finished armor; no new general pool required |

Planning constraints and context: `CLAUDE.md`, `ADVERSARIAL_TESTING.md`, `Docs/SPREAD-FIRST-HOUR-PLAN.md`, `Docs/LOOT-FINDS.md`, `Docs/SPREAD-COMPOSITION.md`, `Docs/REGIONAL-GUIDANCE.md`, `Docs/MORROWFAST-STYLE-AND-REGIONAL-SITUATIONS.md`, `Docs/DENSITY-SPREAD-PRESENTATION-SCOPE.md`, `Docs/Verification/SpreadFirstHour/Performance/README.md`, `Lore/MYSTERY-LEDGER.md`, and `Lore/Voices/VOICE-CARDS.md`. These establish current scope and protected behavior, not new test evidence. Read the applicable voice card again when writing final dialogue.

## 12. Planning review and living implementation log

### Q1–Q4 implementation checkpoint

- **Q1 — Symmetry:** creation/staging/final acceptance/save/restore/retention are distinct. The final review reproduced replaceable-plan/repeated-build validation loss and post-factory foreign-container mutation; root repaired them before native 42 GREEN. `StagedWayhouse` pins the original pending plan, and stock freshness is checked before `AddItem`.
- **Q2 — Cross-feature consistency:** all three reports use one historical contract, short choices and a wrapped body. Wayhouse selection is not live installation authority. Packet validation now applies actual inventory/equipment/body backlinks as well as container backlinks. Late receipts replace early suppression/credit design without changing ordinary roll order.
- **Q3 — Counters:** M1 native 54, M2 native 23 plus 80 neighbors, M4 native 83 and final Wayhouse native 50 cover their named source/ownership/save boundaries; M3 native 3 measures existing placement. Main19, ordinary14 and cards9 now separately prove the recorded native routes/readers/aftermath, including a real rear branch and 24-step shelter bypass. Matched component 60 and current dense profile 12 are recorded with limits. Full native20,633 completed20,632 PASS/1 old fixture FAIL/0 skipped; focused native50/50 then closed the test-only flicker correction and late persistence2, with no runtime delta. The 2,437 recorded run inputs showed zero drift. No test count closes the unperformed ordinary wayhouse or separate shelter combat witness, and no full20,635-pass run is claimed.
- **Q4 — Drift:** this revision removes obsolete 3880 ordering, future-only M1 APIs, proposed row relocation and anchor owner-ID ledger language. CoO-original content and existing approved visuals remain the contract; no Qud assets/enemies or mechanics-parity claim is introduced. §8 explicitly records the accepted split between the ordinary grain loop and transfer-assisted wayhouse/cards, plus unperformed shelter combat/cache-looting and ordinary wayhouse travel/return.

Resolved design/implementation corrections are preserved in raw evidence: existing gleanings require no new content; sorted priorities require late 4300 composition; report paragraphs belong in wrapped text; actual receipt blueprint/owner snapshots must survive factory callbacks; Wayhouse final acceptance must survive plan replacement/repeated invocation and reject borrowed furniture/gear. Optional failed placement remains ordinary fallback, while a corrupted already-staged final packet is rejected before caching. Native unit proof is not a whole ordinary journey.

| Date / stage | Work and evidence | Files changed / limitations |
|---|---|---|
| 2026-09-27 planning | Inspected current baseline, source/data, docs, and independent source audits for steps 1 and 2. Two independent final draft reviews identified four substantive corrections, incorporated above. Specified implementation order, ownership, tests, native gates, and stop conditions. | This document only. No new Unity run, gameplay change, test run, commit, or push. Third delegated source audit was unavailable; expedition design/source checks were completed by the primary agent and its draft received independent review. |

**Current disposition / next step:** implementation and recorded verification are complete within §8’s explicit experiential limits. The sole full-sweep failure is closed by a test-only correction and actual focused50/50; both late persistence bridges pass. Exact owned-path review is complete; publication uses the dependency boundaries below. No additional runtime fix or broad sweep is implied by this fixture correction; ordinary wayhouse travel/return and separate shelter combat remain unverified follow-ups.

### Execution log

- Saved the implementation prompt; preserving unrelated dirty Unity MCP logs and existing untracked work. Editor initially idle in ReferenceGlade, no play session.
- M0 native census and initial wayhouse RED: job `93879f4465ce4b96b38aade7f89baaed`, six tests executed. Three census cases pass; all three source-selection cases fail because the manager has no wayhouse source yet, before production edits. XML and native baseline samples retained under `Docs/Verification/SpreadDiscoveryExpeditions/`.
- Implementation source correction: broad reserved working lanes required a smaller 7×7 building plus separated exterior stations. Empty gaps preserve the lanes; whole-zone connectivity is validated rather than clearing reservations.
- Parallel ownership: reports/notes; optional pair clue and gleaning clarity; exact generation receipts and late wilderness composition. Primary agent owns shared manager/save hooks, expedition, and all native editor runs.
- M0 sample: seeds 1/64/1729 have128/128/127 eligible ordinary Spread addresses; the fixed formation cohort generated20/18/20 zones respectively (58 total). Stock and locked-container counts are observations, not player acquisition or balance evidence.
- UI sweep correction: conversation choices are single rows, while the body text wraps. Reports belong in the existing wrapped body, with short explicit remember choices, rather than long report text in a clipped option.
- Expedition implementation refinement: reuse the exact generation receipts and place the wayhouse after all ordinary random placement, atomically replacing one original container and the original hostile group. This spends the same local container/group allowances without early suppression/credit hooks and validates routes against actual later props. Existing random stock is rolled once, then that one source's stock is replaced deliberately by the fixed reward; accepted content is an explicit economy change. Optional refusal leaves the original graph untouched. Final acceptance and bounded retention still apply. The main architecture in §§5–6 now reflects this implemented 4300 receipt design; the earlier 3880 proposal is historical only.

- M1 initial 44 native cases passed; third-family extension first recorded 7 feature RED/3 controls and then native 54/54 GREEN, job `675459b3bf4043c9985f8ffcaf21db4d`, `Tests/m1-wayhouse-green-key-route-red.xml`. A separate root key-route RED in that batch is not an M1 failure. Exclude the earlier stale-assembly root run from M1 evidence.
- M2 native 23 clue cases plus 80 neighbors GREEN; M3 native 3 measured 99 composition graphs and 212 existing ripe rows, maximum lane distance 6/6/7 for seeds 1/64/1729. Gleaning positions/ranking/yields are retained. Measurements are in `Docs/Verification/SpreadDiscovery/M3/Measurements/current-field-lanes-{1,64,1729}.json`.
- Latest Wayhouse/M4 gate: job `f0f7ee05c1034d84ab4051713c771a8f`, `Tests/wayhouse-m4-green-launcher-red.xml`: Wayhouse 42/42 and M4 83/83 GREEN; two missing-launcher RED cases are separate expected harness gates. Root's paired repairs close the final-plan/repeated-build, foreign-container stocking, and inventory/equipment backlink findings recorded in `Integration/cold-review-combat.md/json`.
- Historical pre-run checkpoint: the bounded native driver was explicitly transfer-assisted for field/site scenes; front and optional rear observations were then pending. Later entries below record completed main19/ordinary14/cards9 results separately. This historical checkpoint did not establish a completed journey, image, full-suite or performance result.

- Natural pipeline follow-up: native 97/97 GREEN, job `e4d4cd49dafc48bc913f304257f86dd9`, `Tests/full-pipeline-wayhouse-focused-green.xml`. It contains M0 census 3, Wayhouse 42, structural pipeline 3 and composer 49; these overlap earlier suites and must not be summed as unique tests. Native 45/45 GREEN, job `be0866b1212f43b88fc822987d069d08`, `Tests/native-census-launcher-green.xml`, contains the full-manager matched census 3 plus restoration 42. Actual composition commits/selected are 3/8,4/7,7/10 for seeds 1/64/1729 (14/25 combined); use these native results rather than earlier private counts. Equal final caller-RNG witnesses, actual owner references/state projections and allowed coordinate changes are checked in each paired graph. No acquisition or balance claim follows from this cold census.

- Native generated-site audit `b02fc0a76b7648f686962d169f44de8b`:19/19 required checks pass, zero failures/errors,57.31s. Real Sill reports/notes, finite harvested row, notice, front key/unlock/open, one Buckler/equip, exact depleted graph/notes after F5→unsaved step→F6. A separate saved/reloaded branch records `REAR LIVE GUARD PATH VERIFIED`. Field/site entry used disclosed original-player-only transfers; this is not an ordinary continuous wayhouse expedition. Root independently viewed the key gameplay frames, recorded in `Integration/native-visual-review.json`.
- Separate ordinary native run `c3bd858b36ad4da3bd1c7205e20359b4`:14/14, zero failures/errors,30.79s;53 local inputs and5 map steps. N start→Sill actual informant/three notes→actual FieldStrips11.8→one actual grain harvested/eaten→native return Sill→F5/unsaved step/F6. No transfer helper or debug grants; food consumption at full HP is not a claimed healing benefit. Saved spent row, consumed item absence and original note contents restore exactly.
- Two earlier native observer attempts remain honest partial receipts: `ce588e5c90894520beeda2a18f1e4394` misidentified UI glyph names (`Text_` versus actual `UI_`); `1d50380d963f4287b24fa61c7a1fa8a4` refused a transient moving-NPC approach. The observer now uses actual atlas identity and bounded current-informant replanning/six safe ordinary waits. No gameplay rule or source geometry changed to pass these checks.
- Actual native Wayhouse economy census3/3 GREEN, job `8af0c58eb20a4e2b81c4bd1fd0adc1e7`, `Tests/wayhouse-economy-green.xml`. All three selected sites installed. Generated stock/gear deltas for seeds1/64/1729 are+4/−9/+4; the reward is always20 and key is NoTrade. This is total generated commerce value, not sale price/player income/campaign balance. Unrelated source owners/positions/stock/gear remain equal. Native and private-runner source geometry/loot differ; use `WayhouseEconomy/Native/seed-*.json` for Unity claims.

- Final cold-eye goods contract repair: actual generated packet validation now requires a single value20 Buckler and single NoTrade key at both staging and final acceptance. A callback could previously mutate those values. The first39-case probe had6 real failures plus2 false-premise duplicate-Stacker witnesses; after correction to the actual current Stacker, pre-fix native39=31PASS/8RED (`67b0f628415f4b26bfd2dfc7625a9e8a`). Current native53/53 GREEN (`5a0d27b78e814f2abc426a27adddb7eb`) includes Wayhouse50 plus economy3. Corrected counters assert exposed quantity2 before expecting refusal; source owners remain unchanged and no malformed packet is cached/saved. `Tests/wayhouse-goods-corrected-red.xml` and `wayhouse-final-goods-green.xml` are decisive; earlier partial receipts are retained with their limitations.
- Main native audit rerun on the final goods validation: `0cc2ff0982444546a7ebfc9a1fccef03`,19/19, zero failures/errors,61.208s; separately records `REAR LIVE GUARD PATH VERIFIED`. The stricter guards preserve the valid current generated site and real reward/key actions.
- Existing dense native movement profile `284ca63df6b04a20a00d36e7396d4c53`:12/12 checks, zero errors;60.2356 seconds,14,355 frames, mean4.202ms/p954.152ms in the Editor. Matching marker parser output is `Integration/current-dense-profile.json`; historic captures are not a causal before/after control. This is a current observed route, not whole-game/GPU performance neutrality. At that profile checkpoint, bounded same-process disabled-feature comparisons were still pending; the subsequent completed 60-row measurement is recorded below.

- Bounded native component performance completed60 sampled rows with restored scene/save/last-game state. Five alternating same-process disabled-feature/current pairs, after warm-up, measured report refresh/text, note reading, cargo, shelter, refusal and Wayhouse generation. Medians: reports0.0793ms/current versus0.0541ms/control; three-note reads0.0177ms; whole cold cargo43.18ms versus31.67ms, shelter38.66ms versus30.76ms, refusal30.47ms versus30.18ms, Wayhouse50.92ms versus29.64ms. These are fixture CPU observations, not UI/GPU draw or old-build whole-game comparisons. Native allocation counters are unavailable: a known1MiB array reports0 delta (`Integration/Performance/native-allocation-probe.json`), so zero output is never treated as zero allocation. Exact samples/restoration are under `Integration/Performance/`.

- Native layout cards `ca59dcb2ac6f46b59474d5b5ee753e8d`:9/9,zero failures/errors,23.773s. Actual fresh cargo6.9 and shelter10.7 receipts bind original cache/guard IDs and contents; approved models/viewport/native readers verified. Shelter11.2 generated but refused the bounded camera-view setup, so the next predeclared candidate was used without changing it. The10.7 shelter bypass was crossed in24 paid normal inputs without player contact/damage and with the exact selected source stock/gear retained. Unrelated ambient AI remained live. The optional actual ditch-cutter sign10.2 was also read, with its original pair untouched. All setup transfers are disclosed; these are staged visual/route cards, not an ordinary continuous trip. Root independently viewed cargo/shelter/bypass-end/pair-reader frames; the visual-review receipt now records14 exact frame hashes.


### Final integration and severity review

- Full native job `d484895eae954485bd835761a823c4e9` ran **20,633** cases: **20,632 passed, one failed, zero skipped**, from `2026-09-28 00:27:14Z` to `00:41:51Z`, duration **876.8094541 seconds**. Exact results are `Integration/full-native-first.json`, `Integration/full-native-first.xml.gz` and `Integration/full-native-job.json`. The recorded2,437 suite-input files had zero changed/missing entries through completion (`Integration/full-run-input-drift.json`). This is a completed full run with one retained failure, not an all-green result.
- The sole failure was existing, unchanged `LightSourceFlickerPartTests.HandleEvent_RenderFires_UpdatesIntensity`. Its thirty synchronous events repeatedly use the same `Time.time`; they cannot guarantee a noise sample away from base intensity. The exact failed time/noise value was not captured, so no particular midpoint or numerical sample is claimed. This was classified as a **low-severity fixture reliability defect**, with no new runtime or player-visible lighting defect established.
- The narrow fixture correction primes the real current-frame `UpdateIntensityAt` to initialize phase/base and capture expected output, assigns an impossible sentinel, then requires the actual Render handler to restore that same sample; the event is released in `finally`. Separate explicit-time variation, bounds and determinism controls remain unchanged. Private execution passed7/7; severing only the Render hook produced1 RED with6 controls passing. No lighting/runtime source changed.
- After the full sweep, only that existing test and the new two-case `SpreadWildernessPersistenceTests` plus its meta were published. `Integration/post-full-test-only-delta.json` records this exact boundary. Native job `79b6620947d042e5b89a39f37c32761c` then passed **50/50**, zero failures/skips, in **7.5276215 seconds**: persistence bridge2, `RegionalSituationContainerLifecycleTests`2, `SpreadWayhouseAdversarialTests`39 and flicker7. The authoritative XML is `Tests/final-persistence-lighting-green.xml`.
- The two late persistence cases exercise real cargo/shelter source actions: full-capacity refusal, partial taking, cached-away return and full save restoration, exact original anchors/content identities and guard equipment. They close the source/action/save bridge; they do not claim an ordinary movement journey or separate shelter combat.
- The current registry contains **20,635 distinct cases**. Do not add overlapping focused counts to the full result or report a single full20,635-pass run. The verification disposition is the retained full20,633 result plus the narrowly scoped successful post-full50, with production unchanged. Main19, ordinary14, cards9, viewed images and performance limits remain separate evidence.

### Publication boundaries

M2/M3 are committed as `12d25617`: optional pair clue, source counters and retained gleaning measurements, with the living `SPREAD-COMPOSITION.md` correction in the same commit. The following integrated commit consolidates boundaries2–4: historical reports, receipt-based cargo/shelters, Wayhouse selection/source/save, native harness, final focused/full receipts and this living plan. This avoids temporarily exposing reports or pipeline references without their dependent Wayhouse types. Exact ownership is recorded under `Integration/owned-final.json`; incidental logs, transient test scenes and unrelated art remain excluded.
