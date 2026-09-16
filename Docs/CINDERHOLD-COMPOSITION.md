# Cinderhold — the surveyed line and the working yard

Status: complete and installed. All 85 owned native checks pass within WB11’s282/282 focused run. WB14 full regression: 13,451 passes, 32 unchanged baseline failures, zero C# errors and no new failures. All 282 additional tests pass.

CoO-original named-site composition. See [the aggregate plan](CINDERHOLD-SUMPHOLD-COMPOSITION-PLAN.md) for source corrections, implementation, visual review and exact integration records.

## Scope and source verification

Only `Overworld.6.6.0`, with the current native Village/PruningPost authority,
receives the new town. The plan itself accepts only that canonical address;
the manager adds the runtime POI/profile gate. Renaming the same native profile
does not change its identity. Copied profiles at other coordinates, changed
profiles, other POI types and underground levels retain their own pipelines.

`Lore/History/02_Geography.md:59` describes a practical Concord-dominated mining
town. Current `WorldMapAuthoring` instead determines gameplay balance: Grovelands,
tier3, road present and no mapped river. The historical tier1/2 wording does
not authorize changing the shipped world map. The new architecture emphasizes
existing work and the Concord's surveyed boundary without inventing Black-Gall
blueprints or excavation rules.

| Verified native contract | Consequence for this feature |
|---|---|
| `StampCatalog.PruningPost` | Preserve one ConcordFactor, CinderholdNoticeBoard, CampGoodsT1 chest and Campfire. Replace the old random stamp instead of adding another copy beside it. |
| `FriendlyNPCs.json`, `RotChoir.json`, PruningContract storylet | This is a delivery/posting/reporting errand. Current posting costs −9 RotChoir, reporting pays20 drams/+10 Concord, and refusal costs−5 Concord once. The historical W4 doc's−15 is stale. No tree-kill quota is introduced. |
| Weaponsmith/TraderPart/WeaponsmithStock | Add an existing working equipment merchant as new local service availability. The factor remains a different native person and quest giver. |
| TinkersForge/ForgePart | Real forging, re-forging and quenching use the existing marked-item/adjacency/skill/resource gates. The station is walkable and has no native light or heat emitter. |
| SmithAnvil/HandlingPart | A separate solid weight120 hauling object, minimum lift18, neither carryable nor throwable. It is not another ForgePart and has no DestructiblePart. |
| PhysicalObject inheritance | TinkersForge inherits Examinable; absence of an explicit local Examinable declaration is not absence of the actual Part. |
| CinderholdNoticeBoard | Keep its own survey/pruning text; do not import LastCounterSign's twice-retreated history. Native examination and solid collision remain unchanged. |
| VillagePopulationBuilder | The main well still arrives at40,12. Preserve ordinary village people, services, trade, rentals, shrine and container content. Interior population recognizes exact StoneFloor; the existing opt-in reservation policy protects authored routes. |
| SettlementSiteDefinitions | This phase does not extend Wellmeet's paid well/oven/lantern repair tracking to Cinderhold. |
| Generic village river | The old unconditional bottom river is not supported by this address's mapped hydrography. Exact composed Cinderhold omits it; unrelated village pipelines keep their behavior. |

## Semantic layout

`CinderholdCompositionPlan` holds four unequal rooms with explicit roles,
rectangular bounds and three-cell doorways:

- LedgerOffice: a smaller northwestern public room with the factor, paper and
  goods. Its notice stands outside on the approach; its campfire is a real
  native fire near the public frontage.
- ForgeWorkshop: a larger northeastern room opening onto broad working ground.
  The forge, anvil and weaponsmith have separate reachable standing spaces.
- WorkersHouse and RestHouse: smaller southern rooms with native beds and
  chairs, away from the working frontage.

The existing main well's commons stays physically open until village population
places the well and its markers. Routes bend through useful courts and branch
to doors and service frontages. Interior StoneFloor marks actual interiors;
outdoor RoadStone is packed road, with Grass in quieter gaps. Four broken
shoulders gather coarse native trees and walkable bushes. Trees are separated
so they cannot encircle isolated one-cell pockets. Seed variation changes room
widths, placement and vegetation; it does not claim an arbitrary town graph.

All geometry remains associated with individual native cell owners. Native
destructible walls, vegetation and containers keep their Parts and wreckage;
art does not grant destruction to protected/descriptive fixtures. No mesh owns
a hidden wall, bridge, stair, inventory or second actor.

## Generation and lifetime

1. `CinderholdCompositionBuilder` at1000 creates the pure plan, validates every
   native base/profile dependency, and stages actual ground and object owners.
   Collision, visibility, canonical glyphs, material/destruction and service
   Parts are checked before owner, interior or reservation mutation.
2. Native connectivity and the existing cave roll remain manager-owned. Cave
   placement must avoid planned interiors and reservations.
3. `CinderholdProfileBuilder` at3860 stages the seven actual profile/workshop
   owners on their intended cells. It refuses blocked cells, stairs, wet owners,
   missing/malformed services and repeat application. Each owner receives the
   existing SettlementId linkage.
4. `CinderholdArrivalReservationBuilder` at3870 reserves actual native stairs
   and their open neighboring cells before later population/container placement.
   It does not clear objects, carve terrain, invent stairs or repair saved towns.
   Success and refusal have distinct worldgen diagnostics.

Base generation requires an empty zone and retains its realized zone identity.
A legitimate cleared retry on that same instance creates fresh owners. A new
zone object with the same string address does not inherit another instance's
profile capability. Successful profile placement has its own replay guard.

When the native loot registry is initialized, stage validation checks the
actual CampGoodsT1 and WeaponsmithStock dependencies. Profile stocking uses
those tables; a Weaponsmith already stocked by TraderPart is not stocked twice.
Headless native callers historically allow an absent loot registry; the new
builders preserve that convention and never initialize or replace global loot
state. Tests asserting actual commerce explicitly load the real registry.

## Verification and in-phase review

Current specification:24 core cases and61 adversarial cases. These cover exact
and malformed address scope, deterministic variation, four semantic rooms,
native interiors, one-owner profile counts, actual quest consequences and
refusal, funded/unfunded shop transfers and stock renewal, strict staging
failures, foreign-owner/retry behavior, runtime profile authority, all-open-cell
flooding across26 independent seeds, complete native services across3 seeds,
and actual cave arrivals plus diagnostics.

🟡 Fixed first cold-eye lifecycle finding: an invalid base call cleared its previously
successful Plan, poisoning that legitimate owner's pending late profile. Added
two counterexamples (foreign-zone call and nonempty same-zone call) that require
preserving the valid Plan and still allow the original late profile. Both failed
in WB07's actual compiled run; the correction now preserves the previously
committed Plan on refusal. Post-fix verification is pending. The earlier test assumption
that any rejection must clear the Plan was removed; reconstruction remains
prohibited by actual owner/replay guards.

⚪ Black-Gall extraction, boat mechanics, unrelated repair economies and full
mining-town lore are outside this bounded native composition. Existing material,
light and destruction semantics take precedence over what a silhouette suggests.

Can verify headlessly: native ownership, movement/frontages, Parts, loaded
conversation consequences, stock transfers, malformed-content refusal and
generation diagnostics. Static previews cannot establish live control feel,
animation quality, sustained FPS or how the town reads during actual combat.
No live or final visual approval is claimed yet.

## Implementation log

- 2026-09-15: Read CLAUDE.md, the aggregate plan, authoritative geography/faction
 sources, native profile/blueprints, village pipeline and existing quest/shop
 tests. Corrected current map tier, −9 posting consequence, forge/anvil split,
 absent Black-Gall and repair-site availability before implementation.
- 2026-09-15: Wrote core and separate adversarial fixtures first. Parent captured
 WB03:32 unique missing-type errors printed96 times; no independent API mismatch
 appeared in that receipt. Implemented the new plan and three native builders
 only after that actual RED and authorization.
- 2026-09-15: Added the pending-profile lifecycle regression pair during first
 cold-eye review. WB07 completed253 total tests with243 passes,10 failures and
 zero C# errors. Cinderhold's only failures were those two lifecycle cases;
 its other83 cases passed, including real pruning/report/refusal and funded/
 unfunded trade plus restocking. Removed Plan invalidation after the actual
 RED. No final GREEN or full-suite result is claimed yet.

## Owned files

- `Assets/Scripts/Gameplay/World/Generation/CinderholdCompositionPlan.cs`
- `Assets/Scripts/Gameplay/World/Generation/Builders/CinderholdCompositionBuilder.cs`
- `Assets/Tests/EditMode/Gameplay/World/CinderholdCompositionTests.cs`
- `Assets/Tests/EditMode/Gameplay/World/CinderholdCompositionAdversarialTests.cs`
- Copied `.meta` files with fresh GUIDs and this document.

Shared manager/rendering integration, voxel art and final receipts are tracked
in the aggregate plan and voxel-kit documentation by their respective owners.
