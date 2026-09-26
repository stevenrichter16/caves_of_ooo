# C10 — underground layouts and lair progression

**Status:** minimal lair placement safety and second ordinary underground layout
implemented; focused standalone checks are GREEN. Persistent multi-level lairs
remain design only, and the new layout still needs native verification.
Navigation and smoke are tracked separately in C8.
Root must coordinate the shared manager/world-save hooks and boss content before
implementation. This is CoO progression design, not a claim of Qud parity.

## Verified corrections before implementation

| Plan premise | Current source and actual behavior | Consequence |
|---|---|---|
| Ordinary caves have no depth grammar | `OverworldZoneManager.CreateUndergroundPipeline`, `SolidEarthBuilder.GetMaterialsForDepth`, `StrataBuilder` | Six material bands and three CA/noise geometry bands already exist. Preserve them; the missing choice is a second layout family. |
| CaveBuilder is the ordinary underground generator | The pipeline uses SolidEarthBuilder then StrataBuilder; CaveBuilder serves other cave pipelines | Integrate at the actual underground recipe, not the unused candidate seam. |
| Depth only increases creature count | `PopulationTable.UndergroundTier` selects one bounded encounter group and adds bear/ice/rot, skeletal/charred/pale, then golem/brute options by tier | Preserve current diverse rosters and their C2 tactics; do not add a second mandatory swarm. |
| Deep resources need entirely new products | The same population table already sources quartz, salt and Choir iron by band; landmarks and container tiers also scale | Test actual placement and discovery before expanding resource tables. |
| Every POI column can use the generic new layout | Sinkhole, Ginmere, Cathedral, Stillleaf, Olderdeep and Root routing precede generic depth routing | Explicit structural and content controls must preserve these routes. Keep authored Overwrit/Stump and Choir natural formations out of an automatic roomed-ruin selector. |
| Lairs already have a vertical graph | GetPipelineForZone handles POIType.Lair only at z=0; CreateLairPipeline has no stairs; LairBuilder puts its boss at the surface center | A real multi-level feature needs persistent plan/ownership and routing before the generic depth branch. Adding stairs alone would lead to an anonymous cave. |
| Bosses already scale with POI tier | WorldGenerator records Tier but GetBossForBiome takes only biome. Current mapped bosses are tier2; AncientGuardian is tier3 but only mapped to retired Ruins | Tier selection needs an explicit grounded roster decision, not merely multiplying stats or moving the same boss deeper. |
| Four-piece breacher is already a Boss blueprint | MarlbackBreacher has tier3 and the real cleaver kit but no inherited Boss tag | Do not silently treat natural-fallback data or item tier as boss identity. Any authored tag/variant belongs to root's Objects surgery after RED. |
| Stair reciprocity has to be rebuilt | StairsUp/Down builders already reconcile deferred lower-first connections and preserve removed cached stairs | Reuse these ownership rules; do not regenerate parent zones or rewrite player-modified stairs. |
| Cached state guarantees forever-unique boss/loot | ZoneManager.UnloadZone intentionally forgets a zone and regenerates it later | A boss/reward ownership record must survive unload/death, not just revisits or cached saves. |
| POI Profile is a suitable saved final-depth field | SavePointOfInterest writes type/name/faction/tier/boss only; Profile is rederived for authored sites | Do not put new stack state into unsaved Profile or append an unversioned stream field. |

## C10.1 — second ordinary underground layout

The smallest useful implementation selects between existing StrataBuilder and a
bounded roomed layout based on existing RuinsBuilder, which already exposes its
wall/floor palette and room dimensions. Keep SolidEarth, connectivity, reciprocal
stairs, stair connector, depth landmarks, hazards, population and containers in
one shared tail. Pass the depth material palette through both geometry choices.
Prefer a narrow selector helper keyed by world seed and zone coordinates using a
stable private hash; choosing layout must not consume the population RNG stream.
No geometry is regenerated when a zone is cached or loaded.

Initial selection proposal: preserve natural depth1; from depth3, allow a modest
roomed share in ordinary non-POI Spread/Sodden/Beating columns. Keep Grovelands,
Overwrit and Stump natural and all authored POI routes unchanged. This avoids
manufactured chambers being forced into Choir formations or scraped/canonical
routes. Do not add a new generic ruins stamp or extra loot just for the layout.
A small worldgen layout receipt should name zone, depth, layout, materials and
reason; no full-zone traversal per turn is needed.

Before changing production, tests must fail for both missing layout outcomes in
a deterministic multi-seed corpus, with all excluded/authored routes as controls.
After selection, generate both layouts across material bands and verify connected
stair approaches and terrain exits, legal stair cells, no solid creature/trap on
an entry cell, actual depth resource/container semantics and no guard multiplication.
Use the existing real generated graph and occupied-cell API, not synthetic floor
counts alone. Keep the 5-seed C0 definitions and record any corpus expansion.

Potential files: new UndergroundLayoutPlan helper; the narrow manager recipe;
new DensityUndergroundLayoutTests and adversarial fixture (+metas). Reuse or wrap
RuinsBuilder without changing surface ruins' behavior. No Objects edit is needed
for this first milestone. Preserve direct terrain/map snapshots before source
changes so the final census can distinguish layout changes from loot changes.

## C10.2 — persistent multi-level lairs

Use a separately staged, versioned world-part ledger following the existing
LocalPeople ownership precedent, with bounded records keyed by surface zone ID.
A record must include plan version, biome, recorded final depth, selected boss,
and successful boss/reward ownership. Choose a small two- or three-level stack
(surface plus one/two deeper floors); one final boss, one final reward, and a
legal return path at every level. Final depth must not be recomputed from future
content or mutable POI tier on each visit.

Claim a new plan only for fresh lair generation and publish its successful state
after generation succeeds. An old saved/cached surface lair must keep its boss,
contents and one-level route unless a deliberate migration is separately approved.
If a lower lair zone is reached laterally first, resolve the same plan without
recursively generating the whole column. Final floors have no automatic down
stair to an unbounded series of new boss floors. Unload, removed stairs, deaths,
failed generation and failed save/load must not release ownership to a new clone.

Reuse natural biome palettes and container pools. Grovelands continues to receive
natural caches, not manufactured strongboxes. Guards use a bounded per-level
budget rather than copying today's full guard/loot roll onto every new level.
Keep arrival/stair approaches reserved from later population, hazards and traps;
validate complete footprints. Final boss placement must check success and record
failure, instead of silently continuing when the content or legal footprint is
missing. Tier-aware boss mapping requires root review of concrete current/new
blueprints and a new exact roster test. Do not repurpose the AncientGuardian as a
universal deep biome boss simply because it is tier3.

Shared ownership needed: OverworldZoneManager routing and success hook; narrow
SaveSystem ledger bind/restore; WorldGenerator boss choice; LairBuilder and
LairPopulationBuilder; any content remains root-owned. Prefer a new plan service
and new builder variants to expanding every existing direct-call fixture's API.

RED/acceptance matrix: fresh one/two-deep plans; surface/intermediate have no boss;
final has exactly the selected boss and reward; cached revisit and saved graph
preserve IDs; death, removal and unload cannot clone ownership; old absent-ledger
saves retain legacy lairs; two managers never share claims; malformed ledger
fails atomically; lower-first generation yields reciprocal stairs; blocked/missing
endpoints do not teleport or delete a player; retry/failure does not leave claims
or duplicate connections; mixed-size bodies/traps never occupy arrival cells.
Each success needs a flipped absent/stale/blocked control and channel-off diag
counter-check. Native ordinary-stat travel must descend, inspect the intermediate
layout, encounter the final guardian, then ascend and revisit the same graph.

## In-phase design review

- 🟡 Avoid duplicated rewards: a room count or new floor must not multiply today's
  full lair loot/guard budget. Establish counts and sale values before/after.
- 🟡 Avoid legacy boss cloning: saved old surface lairs need an explicit legacy
  record/adoption path; their lower anonymous caves must not gain a second boss.
- 🟡 Avoid false saved identity: final-depth Profile/string conventions are not
  serialized by the current POI writer. Use the existing world part graph with
  narrow atomic attach/restore hooks, after ownership coordination.
- 🔵 Depth grammar already works in several systems. This phase extends geometry
  and vertical ownership; it must report those existing systems as reused.
- ⚪ Higher-tier bespoke bosses and enhanced finds are separate C11 content; this
  design does not pretend that existing tier2 bosses satisfy that expansion.

**Can verify from this sweep:** exact current routing/builders, saved POI fields,
resource/population bands, existing boss tags, stair ownership and unload behavior.
**Cannot verify yet:** new geometry distribution/connectivity, safe generated
multi-floor travel, new economy, final boss tier mapping or native appearance.
Those remain implementation gates, not results of a source review.

## Confirmed pre-progression placement defect

A private real-factory probe builds a Spread lair, runs its connectivity pass and
forces the first guard's legal integer placement roll onto the already-placed
boss cell. **One expected failure and one matched passing control:** the occupied
cell ends with both MarlbackWallkeeper and GiantSpider; the same geometry with
no boss correctly holds one guard. LairPopulationBuilder's IsPassable list does
not exclude the boss, and Zone.AddEntity checks physical collision only for
explicit multi-cell footprints. This confirms a generation bug, not a hypothetical
input exploit. No production fix has been made in this C10 audit.

Evidence: `Underground/lair-occupancy-red.json`, compressed XML and exact private
probe. A bounded repair before progression should select only actually clear
creature cells and verify a prospective complete footprint; retries must remain
finite, avoid reserved/trap/stair cells, and record skipped placement. Preserve
ordinary item placement without treating all walkable terrain as an occupant.
Do not broaden Zone.AddEntity's global legacy contract to solve one builder.

## Minimal placement repair — private implementation gate

Root approved fixing the confirmed overlap before any new world geometry. A
dedicated private 25-case RED had 22 failures and three passing controls. The
bounded LairPopulationBuilder repair materializes the prospective entity once,
then enumerates legal anchors against current occupants/reservations and every
physical body cell. It selects uniformly among eligible anchors using one
placement draw and consumes the full occupied footprint from the placement pool.
Non-terrain occupants, traps and stairs stay clear. Missing content or no legal
body is skipped with a worldgen refusal record; successful placement records its
zone, blueprint, position and body size. No global Zone placement rule changes.

The post-GREEN malformed-input sweep caught one candidate regression: calling
ContainsKey(null) bypassed BuilderSpawn's old null guard. A new null/empty/missing
blueprint triplet first produced RED (26 pass, one fail), then a bounded null/empty
check restored the original refusal contract. Final private **77/77 GREEN**:
27 placement checks, 22 existing statistical/room/ambush cases, three biome-boss
cases and 25 ExaminablePart cases. The latter includes the C5 chest pin correction,
which now names the full reviewed prose and asserts no weapon/armor statistics,
no changed lock and unchanged contained-item identity/ownership.

Receipts: `Underground/lair-placement-red.json`,
`lair-placement-adversarial-red.json`, `lair-placement-private-green.json` and
compressed XML. Production/test publication is held for root's native-window
release; standalone success does not mean the new placement has run in Unity.

Q1/Q2: initial candidate collection and final physical-cell revalidation share
one safety predicate; success/refusal diagnostic shapes use the same zone and
blueprint. Q3 covers occupied/empty boss cells, reserved/solid/creature/item/trap/
stair blockers, positive/negative clipped footprints, malformed bodies, null/
missing content, callback mutation, first/last eligible selection and channel
on/off. Q4: authored ambush thresholds are unchanged and the existing statistical
bounds pass; individual spawn positions and placement RNG ranges may change.
This does not promise old-seed identical maps or implement multi-level progression.

### Shared publication and final focused result

After the native window released, the verified source, new
`DensityLairPlacementSafetyTests.cs`/meta and updated `ExaminablePartTests` chest
pin were published. The same private runner then read shared Assets directly,
with no production override: **81/81 GREEN** (the 77 above plus four actual
generated lair pipelines across the live biomes). Those four retain sparse traps
without occupied-cell overlap. `Underground/lair-placement-shared-green.json` and
compressed XML preserve the final receipt. No new geometry, stairs, boss mapping
or lair rewards have been introduced. Final native integration remains pending.


## C10.1 — private implementation and verification

The candidate now selects the existing RuinsBuilder for 25% of eligible addresses,
using a stable, separate address hash. Depths 1–2, all POI columns, the four authored
wilderness columns, and Grovelands/Overwrit/Stump retain their existing geometry.
The roomed builder uses each depth's existing wall/floor palette and four to seven
rooms. Missing room-decoration content falls back to the existing natural recipe.
No item, creature, blueprint, resource band, container pool or extra population
pass was added. The selector carries surface-biome and ordinary-column context
for C11, but this C10 receipt does not include C11's still-private T4 data.

A required source correction emerged from actual generation. The initial geometry
and connectivity checks passed, but later landmarks, population and containers
could occupy a sole exit. Five C0 seeds exposed five failing generated-route cases;
a six-zone builder-by-builder probe isolated the late-tail closures. This affected
one natural layout too, so merely increasing room connectivity was not a correct
fix. Root approved a cold-generation route reservation after StairConnector and
before landmarks. It walks existing clear cells once, reserves paths from actual
stairs to every border and each other stair, then reserves clear stair approaches.
It carves nothing, consumes no random draw and creates no owner. Missing stairs,
blocked stairs or an absent route return false before publishing any reservations.

A follow-up exclusion test found that the first reservation insertion also entered
POI and canon-excluded columns: four intended RED cases. The final condition is
ordinary non-POI/non-authored Spread, Sodden or Beating only, with both stair
blueprints available. Minimal factories and Root/Choir/sinkhole pipelines retain
existing behavior. This restriction applies to reservation as well as room choice.

Final private **188/188 GREEN** comprises 44 layout/pipeline/generated-route cases,
22 independent route/save cases, one structural census capture and 121 existing
underground, sinkhole, biome-cave and world-map cases. The generated-route cases
cover both layouts at depths 3, 7 and 11 for all five C0 seeds (30 actual zones).
The adversarial fixture covers missing/blocked endpoints, no RNG use, reservation
atomicity, existing reservations, multi-cell and offset stair bodies, three actual
guards placed off protected routes, diagnostic channel on/off, lower-first stairs,
and full-session save/load after chest depletion and stair removal.

The structural census records 20 actual zones (five C0 seeds × depths 1/3/7/11).
Four select roomed geometry and sixteen select natural geometry. Final terrain
fingerprints differ for all twenty because protected routes also alter late-tail
placement. Placed creatures change from 39 to 40; container owners remain 90.
These are structural counts, not opened-loot value or a complete economy census.
The current shipped underground population has no multi-cell blueprint; the
reservation tests explicitly cover stair footprints and actual single-cell guards.
They do not claim a general repair of every builder's prospective modded footprint
placement contract.

Q1/Q2: both layout families reuse exactly one shared landmarks/hazards/population/
containers tail. Route reservations are staged before publication; refusals and
success use the worldgen channel. Q3: exclusions, missing content, alternate seed,
mutated cached graph, removed stairs and disabled diagnostics are matched controls.
Q4: natural geometry selection remains unchanged, while subsequent placements and
RNG ranges can change when protected cells are excluded. Saves load their existing
native graph without replaying the selector or rebuilding destroyed content.

Durable receipts in `Verification/DensityCompletion/Underground/` include
`layout-api-red.log.gz`, `layout-routing-red.xml.gz`,
`layout-unprotected-routes-red.xml.gz`, `layout-tail-stage-failures.json`,
`layout-reservation-api-red.log.gz`, `layout-exclusion-red.xml.gz`, and
`layout-private-regression-green.xml.gz`. The before/after structural JSON,
`layout-structural-delta.json` and `layout-sample-choices.json` preserve the census.
The API RED receipts are compile failures for deliberately absent new classes;
the routing, late-tail and exclusion RED receipts are executed behavioral failures.

**Publication/native status:** source and tests remain private during root's native
window. This runner uses its documented stable string-hash adapter; these seeds
are not Unity map snapshots. DepthAmbientTests was not in this private corpus
because its renderer dependency requires native assemblies. Native compile,
focused tests and ordinary-stat descent through both layouts remain required.

### C10.1 shared publication

After root released Assets, the two new production files, three fixtures and fresh
metas were published with only four narrow manager edits. The private runner then
read shared production and repeated the same **188/188 GREEN** result, preserved
as `Underground/layout-shared-regression-green.xml.gz`. Root's C14 content-pack
authority and fallback were retained. C11's context-aware landmark call is a
separate coordinated change; it is not included in this shared C10 receipt.
Native compile and focused execution are root's next gate.


## C10.2 — actual save-graph contract and implementation plan

The read-only save sweep confirmed a concrete distinction from LocalPeople:
`LocalPeopleLedgerPart` persists claims, not its runtime `LiveOwners`. A copied
ID-only approach would suppress a duplicate after unload but discard a surviving
boss. `ZoneManager.UnloadZone` removes the native graph and source-owned edges;
`SaveOverworldZoneManager` writes only `CachedZones`. `SaveZone` carries placed
entity references, the entity queue carries their real parts/equipment/abilities,
and `SessionTileStateSerializer` requires every cached zone exactly once. Keeping
only the boss entity would also lose consumed loot, removed stairs and tile state.

A private four-case baseline probe executed before progression production: cached
revisit preserves the living boss and its damage (control); unload replaces that
owner, respawns a committed dead boss, and discards an emptied reward owner (three
intended REDs). `Underground/lair-persistence-design-red.xml.gz` and the compressed
probe preserve this source correction. The death probe directly supplies the
already-committed death tag/removal; it does not claim to exercise combat death.

Root approved the minimal retention approach. Generated floors belonging to a
new stack remain in the existing cache when unload is requested, through a narrow
virtual unload veto. The request records an explicit retention outcome and leaves
active-zone/connection ownership untouched. Other zone unloading remains exactly
as it is. Actual worldgen creates three to five lairs, so surface plus at most two
lower floors retains at most fifteen such graphs in an ordinary world. This is
bounded retention, not a new disk-eviction system. Native saved entity and tile
serialization then preserves actual damage, equipment, cooldowns, empty caches,
dropped/carried loot and removals without blueprint reconstruction.

The new world part must always save a version marker even when it owns no plans.
Otherwise a current-version save made before any lair visit would look like a
pre-feature save on load. A genuinely absent marker adopts the saved map's lair
columns as legacy single-floor records; existing cached lower anonymous caves do
not gain a second boss. A fresh runtime encountering a cached unclaimed lair
column likewise adopts it conservatively. No old native owner is moved, renamed,
healed, restocked or rebuilt. The empty marker may create a World entity using
the same binding convention as LocalPeople; foreign-manager ledgers are refused.

Each new record is keyed by canonical surface address and fixes version, biome,
surface tier, selected existing boss blueprint, final depth (one or two below
surface), generated-floor bitmask, successful boss/reward IDs and endpoint IDs.
It does not infer death from an absent actor and does not release a claim when an
owner dies, is removed or is picked up. Final depth and boss choice remain the
recorded values if a mutable POI tier/content table later changes. Boss tier
expansion is separate: this milestone uses the actual current biome kits.

Fresh generation is staged. A provisional plan may be read by the new stack
builder but is not a durable claim. The builder places local native endpoints,
validates complete footprints and return paths, and stages a result; it must not
register global connections or claim an entity during pipeline attempts. A narrow
successful-generation commit hook runs after all existing generation callbacks
and publishes only a complete accepted record/edges. A failed/retried floor
cannot burn ownership or leave an extra route. There is no automatic descent
below the recorded final floor.

Lower-first access resolves the same immutable plan and generates only the
requested floor. Endpoint coordinates are part of that plan, so the later parent
can form its reciprocal route without recursive world generation. An already
cached counterpart whose stairs were removed keeps them removed; neither side
repairs a player's deletion. Existing vertical travel already refuses a physical
stair when its return endpoint is absent and uses atomic footprint transfer.

The approved pacing revision gives each earlier floor one ordinary biome cache
and leaves one designated reward cache on the final floor: two or three real
containers across the whole stack. Each selected cache uses its own existing
`TablePrefix`; approach stock progresses from T1 to T2 without exceeding the
lair's tier, and final stock clamps to T1–T3. Empty rolls use the same small
GoldCoin fallback as ordinary container placement. Natural caches never receive
the generic manufactured `LairLoot` pool. The bounded stock is redistributed
across these floors; the full old loot/ambush pass is never repeated per floor.
Each floor gets at most two existing biome guards; original ambusher/mimic and
trap passes run once on the final floor. The single final boss/reward claim and
all earlier cache depletion persist through the retained native graph. Higher-tier
boss expansion remains separate. The measured budget and scope correction follow.

Implementation order: (1) versioned ledger/legacy and always-empty save tests;
(2) staged routing, lower-first endpoints, finite placement and success commit;
(3) narrow unload retention, real combat death and carried-loot save round trips;
(4) seed/budget/connectivity sweep and adversarial malformed/foreign/stale controls.
Required saved controls include injured living boss with equipped-item IDs and
cooldowns; final reward emptied then removed; item transferred into the player;
boss killed through CombatSystem; destroyed return stairs; save before any lair
visit; legacy absent-ledger world; rejected generation and failed decode; distinct
managers with the same seed; diagnostic channel on/off. Native descent/return,
ordinary-stat boss encounter and save/reload remain root's later acceptance gate.

Shared publication remains prohibited until root reviews this separate milestone.
The approved C10.1 source and its native verification are independent of this
C10.2 private implementation.

### C10.1 native integration result

Root's affected native run passed **580/580** cases, including the published C10.1
fixtures; see `Verification/DensityCompletion/Integration/native-refinement-and-integration-green.xml.gz`.
The snapshot fixture's initial Newtonsoft compile dependency was removed rather
than adding a package. Its small deterministic JSON writer is also separately
standalone GREEN and emits twenty parseable records. This native result supersedes
the earlier pending compile/test status; an ordinary-stat Play descent through
both underground layouts is still a separate visual/travel acceptance item.


## C10.2 — private implementation review and receipts

Status: **private, not published or native-tested**. Root requested a separate
follow-up commit after the current integrated content checkpoint. Candidate
ownership is recorded in `Underground/LairStacks/candidate-manifest.json`; copies
of the two managers contain the standalone hash adapter and must never replace
the shared files wholesale.

The initial API compile RED and 10 routing REDs preceded the implementation.
The private affected regression now passes **500/500**, including **140 dedicated
new core/adversarial/review/reward/pacing cases** and 360 existing save, lair,
people, underground, world and neighbouring cases. The actual-content census is
included in that total; its portable-output follow-up also passes 1/1. These are standalone engine-core results, not Unity input/rendering
or native seed equivalence. Durable compressed raw XML/logs are under
`Docs/Verification/DensityCompletion/Underground/LairStacks/`.

| Sweep/review correction | RED evidence | Resolution |
| --- | --- | --- |
| ID-only ownership loses a living boss when its zone unloads. | Four-case baseline: three RED, one cached control. | Retain only committed stack floors and adopted legacy surfaces; serialize the existing graph. |
| Removed counterpart left a stale vertical edge. | `stale-edge-red.xml.gz`, 1 RED. | Remove that owned edge without recreating either physical endpoint. |
| Initial stack omitted the existing ambusher/mimic and trap passes. | `ambush-trap-red.xml.gz`, 5 RED. | Run the existing passes once on the final floor; retain their actual biome chances. A surface DuneLurker is an existing guard, so it is not misclassified as the dedicated ambush pass. |
| Missing palette and contradictory saved claims were accepted. | `malformed-red.xml.gz`, 6 RED. | Required palette checks and staged bounded ledger validation reject before mutation. |
| End-of-builder IDs alone do not survive later `OnZoneGenerated` mutations. | `root-review-red.xml.gz`, ownership subset. | Commit revalidates staged object identity, ID, role, native full-body membership and obstruction after all generation callbacks. |
| Hardcoded routes ignored a moved cached stair. | Same 19-case root-review RED receipt. | Build reciprocal edges from the actual owned physical endpoint; removed/renamed/retyped/blocked counterparts refuse atomically. |
| A natural cache was receiving manufactured `LairLoot`. | Same root-review receipt, four biome cases. | Use the selected biome pool entry's existing tiered table and ordinary empty-roll coin fallback. |
| A generation callback could invalidate the cached counterpart after builder preflight. | `counterpart-callback-red.xml.gz`, 4 RED. | Revalidate both edge ends before publishing any floor bit, owner or edge. |
| The new empty World marker had no ID, so save/load/save grew after identity repair. | `regression.xml.gz` plus 5/5 hook-baseline controls. | Give a newly created metadata world an ID before its first save; preserve an existing ID. |
| The intentional empty marker changed an old no-world test pin. | Same affected regression. | Assert empty LairStackLedger and no LocalPeopleLedger/no false claim; the populated LocalPeople control is retained. |
| The existing route diagnostic queried the first 100 shared records. | Same affected regression. | Query the test's own causal trace without clearing other diagnostic state. |
| One small final cache removed most of the old stock; consolidating all stock still left empty approach floors. | `reward-distribution-red.xml.gz`, 12 RED; `pacing-red.xml.gz`, 16 RED. | Calibrate bounded biome/tier rolls, then redistribute them into one ordinary cache on each earlier floor and one final cache. Preserve the same exact final claim. |
| A loose reward source could be omitted or invalidated after placement. | `reward-loose-source-red.xml.gz`, 6 RED; `reward-loose-callback-red.xml.gz`, 3 RED. | One optional existing-tier Sodden equipment find is staged and revalidated with other owners; its native identity and removal persist. |
| A custom factory could return a cache or item already owned by another zone/inventory. | Foreign-owner cases in `pacing-red.xml.gz`, followed by paired fresh-owner controls. | Refuse before changing capacity, contents, placement or inventory ownership; final/approach/loose/item paths all check freshness. |

Whole-body and offset-body controls cover boss/stair placement; oversized bodies
refuse without a durable claim. Real CombatSystem death, carried equipment,
cooldown state, removed stairs and depleted/removed reward owners round-trip
through the actual serializer. The absent-ledger legacy path and always-saved
empty-ledger path are paired. Lower-first generation does not recursively create
parents, and saved removed endpoints are never reconstructed.

### Whole-stack content budget

The census uses every actual lair in world seeds 1, 64, 1729, 2026 and 729490642:
18 matched columns, rather than selected favourable coordinates. The old control
adopts the map through the genuine absent-ledger legacy path and generates its
single surface floor. The candidate generates every floor in each new stack.
Stock includes generated container contents, carried/equipped gear, and loose
items regardless of access; commerce is item value, not a claim of sale income.
Loadout RNG is reset per floor, and the runner uses stable hashes that differ
from native Unity layouts.

| Observed total | Old single-floor lairs | New whole stacks |
| --- | ---: | ---: |
| Floors | 18 | 39 |
| Creatures / bosses | 111 / 18 | 117 / 18 |
| Real cache owners (excluding mimics) | 66 | 39 |
| Container owners including mimics | 81 | 55 |
| Mimic containers | 15 | 16 |
| Traps | 27 | 25 |
| Item entities / stacked units | 212 / 449 | 194 / 452 |
| Cache commerce | 1712 | 2151 |
| Loose-item commerce | 775 | 260 |
| **Cache plus loose commerce** | **2487** | **2411 (−3.1%)** |
| Carried/equipped commerce | 1077 | 966 |

Each candidate stack has 5–9 creatures, one true cache per floor, its existing
possible mimics, one final boss, and 1–2 final traps. The final floor may have one
loose item only in Sodden: a 75% gate selects one current-tier weapon or armor
find. Other biomes consume no RNG for this source, and Grovelands never receives
manufactured gear. This retains Sodden's measured equipment incidence without
making a valuable artifact guaranteed. It is a stock-source change, not a new
boss drop; actual carried/equipped gear remains whatever the existing kits grant.

The first rejected single-cache draft produced 329 cache value and no loose
stock. A calibrated consolidated draft produced 2697 cache-plus-loose value but
put all deliberate finds on the final floor. Root approved the subsequent pacing
change after inspecting the contents. These are superseded drafts, retained in
raw receipts rather than presented as current results. The current budget is
2411 and includes the approach caches; it does not add those caches on top of
the calibrated final stock.

`LairRewardBudget` has named T1/T2/T3 nominal roll budgets:
Spread **6/3/2**, Sodden **6/3/1**, Beating **8/4/2**, Grovelands **4/4/3**.
Each approach gets one ordinary stock roll. The final cache uses
`max(1, biomeTierBudget − earlierCacheCount)` rolls so every final reward remains
nonempty. Its tier and selected container's own table family drive each roll;
higher-grade richer tables need fewer rolls. The final cache kind is the most
substantial of the old budget's 3–4 ordinary pool draws. This keeps a meaningful
final find without value-based rejection sampling or an unbounded fill loop.
Only that generated final instance has capacity 32; ordinary approach capacities
and all blueprint defaults are unchanged. The maximum source loop is eight.

The table was derived from 256 draws in each of twelve biome/tier strata, then
checked against all eighteen real generated columns. The old algorithm used T2
ordinary caches even in T1 lairs. Current approach/final tiers deliberately fix
that source mismatch. The twelve-stratum medians, P10/P90, equipment incidence
and explicit old Grovelands manufactured-scatter exclusion are recorded in
[the distribution receipt](Verification/DensityCompletion/Underground/LairStacks/reward-distribution.md).
The final Sodden T2 budget was adjusted from two to three after the pacing sweep
showed too much loss; its mean is now 94.9 versus the old 99.9, with equipment
incidence 87.9% versus 88.3%. None of these source draws proves native balance.

The current physical contents were also checked. The 21 approach caches have
**1–5 entries (median 2), 1–11 units (median 3), and weight 1–22 (median 3)**.
The 18 final caches have **1–10 entries (median 5.5), 1–65 units (median 14),
and weight 1–100 (median 28.5)**. The largest stack is 56 coins; the largest food
stack is two. Final contents cover materials/utility (30 entries), weapons (19),
tonics (17), currency (15), armor (12), food (5), and one grimoire. Approach caches
likewise include materials, currency, food, weapons, a tonic and armor rather
than a single repeated category. Weight uses `InventoryPart.GetItemWeight`;
there is no invented volume metric. Values include locked contents and are not
a promise the player can immediately take or carry everything.

The main RED sequence and exact limits are preserved in compressed XML/logs.
`reward-wiring-red.xml.gz` contains seven faulty setup failures that named a
nonexistent MarshHorror; those are not counted as implementation evidence. The
corrected existing-boss fixture subsequently passed, and independent feature-off
counterchecks established the six loose-source and three callback-validation
REDs above. Portable census output now defaults to the system temp directory and
supports `COO_LAIR_CENSUS_OUTPUT`; no test requires this session's scratch path.

### Native audit candidate and remaining gate

The separate `DensityLairNativeBatch` / `DensityLairNativePlayer` candidate uses
the repository's actual `Assets/Scenes/Main/SampleScene.unity`, isolated disposable
save root, ordinary seed64 new-game actor (40 HP and starting dagger), and actual
Beating tier3 lair at `Overworld.10.17.0`. The earlier guessed scene path was stale;
the source sweep corrected it before review. One labelled startup transfer places
the unchanged actor at the generated surface stair. Every subsequent move,
descent, examination, collection, save/load, ascent and revisit uses native
keyboard input with active AI and ordinary visibility. The driver can drink at
most two actually owned starting tonics through the inventory UI; it never edits
stats, adds equipment, heals directly or suppresses enemies. Death, missing UI
access, an unopenable cache or a failed route fails the audit rather than being
bypassed. The staged start is not evidence of natural lair discovery.

The finite script proposes twelve observable checks and seven screenshots:
surface/intermediate/final ownership, real boss examine text, native cache pickup,
F5/F6 IDs/HP/gear/depletion, return to surface and same-owner revisit. Its launcher
restores save preferences, prior scene/start configuration, keyboard/input settings
and owned capture state; a watchdog handles interrupted runs. Actual Unity
runtime and editor reference-set compilation passes with zero errors (pre-existing
warnings only), but this is **compiler evidence, not a Unity EditMode or Play run**.
Root must review, publish and run it after the separate C10.2 production review.

Can verify now: finite core generation, full-body routes, staged authority,
bounded source budgets, actual serializer identity/depletion, and private-source
compilation against Unity's references. Cannot verify now: native seed geometry,
keyboard route success, ordinary-stat survival, rendered readability, discovery,
or whether the encounter is enjoyable. The 500-case runner uses its documented
stable-hash adapter; it cannot substitute for those native checks.

Self-review: the concrete 🟡 identity, callback, endpoint, palette, cache provenance,
foreign-owner, stock-loss, pacing and save-shape findings above are fixed privately.
🧪 Native ordinary-stat passage and balance remain open, as does the separately
scoped higher-tier boss roster; no combat rebalance is implied. No hot turn/frame
hook is added: generation validates bounded entities/edges and at most eight stock
rolls, unload performs one ledger lookup, and save/load walks the retained graph.
