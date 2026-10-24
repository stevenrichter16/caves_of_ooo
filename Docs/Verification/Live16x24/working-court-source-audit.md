# Working-court framing — source sweep, 2026-10-24

Status: source-only audit; no Unity, gameplay or native test execution by this agent. Root selected sparse stepping stones after the Barren-ground premise below was disproved. Production remains untouched until actual native RED. Parent owns the living plan, rendering material, Play and commits.

## Decision and corrected premises

- Use a real, compact nine-StoneWall back edge and fifteen real StoneFloor stepping-stone owners, fitted only during the exact connected kitchen's cold generation. Keep the eight functional owners at their existing relative offsets and preserve the approved art. Exact coordinates are the visual agent's next contract, not invented in this audit.
- StoneFloor is **not** Barren. Its content is ordinary non-solid Terrain with a dot, earth tint and authored GlyphVariants (`Objects.json:8278–8313`). Grass beneath it remains Plantable/Vegetation. The local instance should say **worn stepping stones**, with prose explicitly retaining grassy joints; this is not a fully paved or sterile court, a road, or prepared planting soil.
- A proposed `Barren` overlay on unchanged Grass was invalid. `Zone.AddEntity:417–419` clears rooted vegetation before installing any Barren owner; `BarrenGroundRules:8–19` includes Grass, and `Zone:760`/`RepairLoadedCell` repeat this on load. That would remove original Grass, violate the exact-source receipt, and need rollback that the current resident builder does not provide. Do not add Barren, remove Grass, or weaken these existing rules.
- Existing source-ground replacement (`RuinsBuilder:172–185`, `VillageBuilder:344–353`) is not an additive preservation precedent. It explicitly removes Terrain. Sparse ordinary non-Barren Terrain co-location is supported by `DoorPart.IsBareGround` and already acknowledged by `SpreadExplorationResidents:267–278`.
- A StoneFloor owner alone does not currently produce a stone background in flat admitted zones. `FlatPixelGreyboxCatalog.IsPlainFloor:516–523` only recognizes exact `Floor`; `GroundOf:568–576` therefore leaves StoneFloor's fallback dot on green. Root owns the separately tested exact court-floor native material/routing. Keep actual crop/moisture, prepared soil and hazards ahead of this material; the glade-only ground atlas need not expand.

## Exact cold-generation seam

`SpreadExplorationResidents.TryPlaceConnected(Zone, EntityFactory, SpreadCompositionBuilder, SpreadExplorationFamily, Func<bool>, out Entity[], out Func<bool>, bool connected, string worldKey=null)` is at lines24–25. The current staged specs at46–57 are the smallest seam. Add the new stamp only when connected, WaysideKitchen, exact `Overworld.12.11.0`, non-Legacy topology and the actual plan's Hedgerow formation. Keep the existing `Identity`/`authority` checks (34–45) and the generation caller's manager/plan/factory/current-attempt authority (`SpreadExplorationBuilder:24–26,65–76`). No generation-version bump or OnLoad hook is necessary.

Existing service offsets before 0/90-degree rotation (`At:266`): Cook(0,0), Oven(-2,0), cot(2,0), chair(2,-1), pan(0,-2), escrow(1,-2), pickup(2,-2), ripe Claspbean(-2,2). Old cold shelter is two walls at(-2,-2),(-1,-2). Replace those two specs only in the guarded new stamp; do not mutate installed owners. Floor has role `court-floor`, unique coordinates, no overlap with services, wall or crop, and must fit actual bare dry ground. No Interior/roof/road/reserved tags or new stateful parts.

The private stock RNG and captured factories are restored at82–91/201–202. The packet graph cap is64 including held stock (`UniqueGraph:242–258`); 9walls+15stones+8services+5stock is below it. Preserve exact five Cook goods,35drams and no replenishment.

## Passability and every service

The current geometric proof is deliberately conservative but not sufficient for the new material:

- `Geometry.Place` requires dry, unreserved, noninterior bare/current space; constructor rejects liquids/gas/triggers and solids/creatures (`SpreadWildernessSituationBuilder:173–197`). Preserve these admission checks for every new owner.
- `Geometry.PreservesAgainst:201–207` treats **all supplied destinations as blockers** and rejects duplicate positions. `Fits:284,294` currently passes all resident positions, including non-solid cot/trays/crop. Keep explicit all-position uniqueness, but pass only the correct guarded-stamp obstruction set to the route proof so fifteen passable stones do not become an imaginary barrier. Do not globally change Geometry or unrelated stamp behavior.
- Current `Fits:300` checks approaches only to the first four kitchen owners. For this stamp check every functional non-wall/non-floor owner, the ten agreed working-front cells, and the new walkable strip behind the trays; preserve four original border ports and existing critical-cell connectivity. Bed requires its **own cell** to be reachable, not merely two neighbors.
- Pure actual-action distances: player repair and batch command<=1 (`RepairablePart:69–103`, `KitchenBatchPart:121–124`); Cook must stay<=2 of pan (`KitchenBatchPart:112–114`); cooking station<=1 (`CookingService:128–138`); bed use distance0 (`PlayerBedService:32–33`). Preserve real pan/escrow/pickup frozen locations and IDs (`KitchenBatchPart.Configure:62–77`, `Bound:92–105`). Walls are genuine Solid+Wall,HP40/hardness3 with Rubble wreckage, and therefore affect collision/LOS/cover. Never fake them in the renderer.
- Floor cells may still accept an ordinary ground-compatible seed through Grass. They are not CultivatedSoil; adjacent/soil-requiring seeds remain constrained by existing rules. Claspbean's actual cultivated bed remains separate, with no StoneFloor beneath it, and its harvest must still remove only crop while preserving bed.

## Transaction, saves and restoration

Stage all new walls/stones before publication with exact owned parts/custody and role fields. Existing `Fresh`, `UniqueGraph`, source equality and placed receipts must remain. The only authorized mutation of original owners stays the existing crop-bed marker (`140–154`); rejected factory callbacks must not delete or rewrite source owners. Rollback at205–214 may remove only still-exact new owners and its own soil marker. New malformed floor/wall factory content must fail before any packet commits.

`ZoneManager.GetZone:44–59` returns cached graphs first. `SpreadExplorationPlan.TryGetGenerationEntry:255–277` rejects already installed/cached attempts. `OnZoneAttached` does not rebuild the resident stamp. Thus visited/saved repaired, depleted or rearranged graphs stay literal; an unvisited chunk in an older world may receive today's cold-generation layout. No migration, reconciliation, version pin or live service repositioning.

## Meaningful tests before production

New focused fixture should prove exact 9wall/15stone coordinates on real seed64/65 generation (plus a bounded additional seed), all eight functional owner offsets/bindings and native source semantics. Pair with Legacy/nonconnected/other-address cases retaining two walls and no court-floor. Every floor must retain the identical original Grass owner, not carry Barren/Plantable/Cultivated/repair/container functionality, and be passable; crop and all service cells have no stone overlay. Verify plantable joints truthfully rather than asserting paving blocks seeds.

Access counters must use actual built cells and ordinary solids, not only plan masks: four ports, ten front cells, back strip,>=2 reachable adjacent cells for each appropriate service, and reachable bed cell. An intentionally blocked candidate or source callback must refuse/try another site rather than accept unusable service. Malformed StoneFloor/StoneWall, revoked authority and callback source mutation must leave no partial new packet and never erase callback-owned changes.

Roundtrip/cache counter retains exact wall/floor positions/IDs, removed floor or wall, depleted Cook stock, repaired pan and harvested crop; no replay/top-up. Existing native fixtures to include once: SpreadWorkingLandscapeTests, SpreadExplorationResidentsTests, ConnectedResidentBindingTests, ConnectedRequiredServiceTests, ConnectedKitchenTests/ConnectedKitchenAdversarialTests, plus relevant generation receipt/refusal fixtures. Root's rendering fixture separately proves current owned `court-floor` semantics under actors, fog and higher-priority soil/hazards; ordinary1x/2x captures remain the visual gate.

## Bounds

No Qud implementation claim: this is source-backed CoO scene composition preserving the existing Qud-like pixel style. This audit verifies source contracts only, not the new layout's feasibility or visual success. Native RED/GREEN and fresh actual keyboard kitchen routes still required; no sprite, renderer or game source was changed while preparing this record.

## Accepted coordinate contract and tests-only handoff

Visual agent's frozen local coordinates, all transformed through existing `At`:
- Nine walls: (-3,-4),(-2,-4),(-1,-4),(0,-4),(1,-4),(2,-4),(-3,-3),(-3,-2),(-3,-1).
- Fifteen stepping stones: (-1,-3),(0,-3),(1,-3),(2,-3),(-1,-2),(-1,-1),(0,-1),(1,-1),(1,0),(0,1),(1,1),(2,1),(3,1),(3,-1),(3,0).
- Ten mandatory court cells: (0,-3),(1,-3),(2,-3),(0,-1),(1,-1),(3,-2),(3,-1),(3,0),(1,0),(0,1). Rear walking row x=-2..3,y=-3 remains reachable; endpoints are grass, not additional stones. Existing service offsets remain.

Saved new `Assets/Tests/EditMode/Gameplay/World/SpreadWorkingCourtTests.cs` and fresh meta,17 expected cases. No production changed. Parent runs actual native RED; no compile/native claim yet. A source-only child review is pending. Before publication, staged floor semantics must reject Barren,Vegetation,solid/takeable,crop/liquid hazards even though generic Fresh currently permits them; this is essential to prevent destructive AddEntity side effects, not a general gameplay-validation refactor.

## Native RED and minimum generation implementation

Root confirmed actual native RED `working-court-red.xml`, 2026-10-24 19:30:46–19:31:03 UTC:39 combined cases,4pass/35fail; all17 generation cases discovered,13 intended generation failures and4 outside controls passing. No zero-case import is counted. The generation fixture was source-reviewed independently by child `/root/projection_cold_review/look_focus_draft_review`, with no compile/setup/source-contract blocker found (source-only, not native execution).

Implemented and froze only `SpreadExplorationResidents.cs` after that RED. New layout guard is connected exact12.11 WaysideKitchen/non-Legacy/Hedgerow; old branches retain their stamps. All destinations stay unique; only actual new-stamp resident/oven/pan/shelter roles obstruct its flood. New floor is admitted only above one exact current plantable Grass, no prepared soil; prepublication `CourtPayload` rejects Barren and hazardous/malformed floor semantics before AddEntity can clear Grass. New services preserve actual physical passability, including cot occupancy. Pan source correction: it has Physics.Solid=true but no Solid tag, so obstruction validation does not require that tag on actual solid owners. No original-ground replacement, extra stock, active update loop or new gameplay system.

Child independent settled-code review requested; native GREEN and visual acceptance remain parent gates. These source records do not claim gameplay success.

## First GREEN and independent counter-check

Root's native first GREEN run (19:34:43–19:35:04 UTC) passed all17 generation cases. This does not claim scene acceptance. Independent child `/root/projection_cold_review/look_focus_draft_review` identified a bounded P2: `CourtPayload` did not exclude closed Door/SealedLibraryBarrier or TileStateSource parts on a floor that the geometric proof treated as walkable. Three focused malformed-floor cases were added only after that run, plus one already-staged floor changed to Barren after publication begins; the latter checks the existing repeated pre-Add validation and is expected already green. Child source-reviewed all four setups with no blocker. Generation fixture now21 cases, production frozen pending fresh native RED for the actual gap.

Root confirmed `working-court-review-red.xml`,19:36:31–19:37:00 UTC:49 combined cases,46passed/3failed, exactly the new closed-door/barrier/tile-source admissions. The late-publication Barren mutation control passed. After that evidence, added the narrow common walkable-role exclusion of those three Part types to `CourtPayload`; old branches and obstruction roles are unchanged. Sources frozen, no test edits; final native49+affected regression and ordinary Play remain root-owned gates. Child final source-review closure requested.

Final independent source closure (2026-10-24): child `/root/projection_cold_review/look_focus_draft_review` re-read the narrow correction and confirmed its P2 closed: every role counted as passable rejects door, barrier and tile-source parts before publication. No new material finding in that bounded reread. Child did not compile, run native tests or operate Unity; parent receipts remain the native evidence.
