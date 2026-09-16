# Cinderhold and Sumphold — work, shelter and wet ground

Status: complete and installed. Two native surface places, 64 voxel model variants. WB11 passes all282 focused tests. WB14 full regression: 13,451 passes, 32 unchanged baseline failures, zero C# errors and no new failures. All 282 additional tests pass.

This is the first pair in the current four-area request, following `e6c47fdc` on `codex/voxel-town-generator`. The Drowned Ledger and Marrowstye follow as the second pair under their own verification plan. CoO-original composition and art. Current spawn, camera1.2× view, full reveal and existing native graphs are preserved.

[Six native gameplay-camera previews](Verification/VoxelWorld/WB10-refined-preview/index.html) · [Final verification](Verification/VoxelWorld/WB16-closeout/README.md).

## Intent and content readiness

| Area | Exact scope | Spatial identity and native purpose |
|---|---|---|
| Cinderhold | `Overworld.6.6.0`, current Village/PruningPost | A forest-edge working town: surveyed public frontage, factor's office, open forge/work yard, grouped goods, and quieter stone homes around a shared well. Broken green shoulders and asymmetric stone shelter masses replace a repeated tent grid. |
| Sumphold | `Overworld.15.6.0`, current Village/Boatyard | Raised dry work fingers beside shallow local wet cuts. Hull trestles face a working waterfront; rain-hooded records sit on an accessible dry apron. Peat faces, reeds and short board paths gather around the wet margins, with stone shelters and services on dry ground. |

🟢 Existing profile owners, ordinary village services, native trade, furniture,
forge commands and terrain systems are available. 🟢 Native voxel libraries
already cover common containers, campfires, role residents, marker pads, bog
plants, peat and duckboards. 🟡 New site-specific models and semantic plans need
real REDs, native realization and camera review. ⚪ Boat travel/building, toll
collection, Black-Gall extraction and missing story systems are outside scope.

Cinderhold preserves one factor, notice-board, CampGoodsT1 chest and campfire
from its native profile. It additionally gains one existing Weaponsmith,
TinkersForge and SmithAnvil in a purposeful workshop. This is explicitly new
local access to existing services, not a claim they were already guaranteed.
The factor's actual pruning-writ quest remains separate from the forge.

Sumphold preserves two BoatFrames, two PeatCutters and one TollRolls. Wet ground
must be actual native water/mire entities, not teal floor paint. Walkable dry
routes and duckboards have no hidden liquid co-occupants. Board removal does
not invent a new collapsing-bridge mechanic. Services, cave arrivals and
residents stay on dry accessible cells. Existing indestructible or descriptive
fixtures retain their native policy; new art does not silently add Parts.

## Verified source contracts and corrections

| Source | Verified correction and implementation consequence |
|---|---|
| `WorldMapAuthoring.cs`; `Lore/History/02_Geography.md:59,69` | Cinderhold is Grovelands, tier3, road=true, river=false. Sumphold is Spread, tier1, road=false, river=false. Historical lore calls them lower/higher tiers respectively. Preserve map balance and biome; local bog-edge authoring does not relabel Sumphold as Sodden. |
| `OverworldZoneManager.cs:790–910` | Generic villages get a bottom river even without mapped hydrography. This pair replaces that destructive generic river pass with its own dry/wet intent, scoped to exact profiles. Other villages remain unchanged. |
| `LandmarkBuilder.cs:822` | PruningPost supplies ConcordFactor, CinderholdNoticeBoard, a CampGoodsT1 chest and Campfire. The semantic profile assembler must preserve these once, not run beside the old random stamp. |
| `PruningContractTests`; `FriendlyNPCs.json`; RotChoir conversations | The actual quest is accepting, carrying/posting and reporting the writ, not cutting a required number of trees. Preserve the real quest facts, payment and refusal gates. |
| `LandmarkBuilder.cs:949`; `SoddenProfileTests` | Boatyard supplies two BoatFrame owners, two PeatCutter actors and one TollRolls. Their real records quote the crossing register, never reveal the skin-reading witness's face or private words. |
| `Objects.json`: BoatFrame, TollRolls, CinderholdNoticeBoard | Native examine fixtures lack Destructible/Material/Thermal Parts. Do not claim ordinary attack/fire destruction for them or infer boat driving/toll payment from their mesh. |
| `Objects.json`: TinkersForge; `ForgePart.cs` | Walkable ForgePart owner supplies actual craft/re-forge/quench actions. It has no native heat/light emitter. Preserve adjacency, item selection, skill and resource gates. |
| `Objects.json`: SmithAnvil | Separate solid 120-weight Handling fixture, minLift18, not carryable/throwable; no ForgePart. An anvil silhouette must not imply it independently opens forge actions. |
| `Objects.json`: Weaponsmith; native shop/stock services | Existing Weaponsmith uses Shop_Weaponsmith and WeaponsmithStock. Introducing it here is new native service placement; use its real stock and equipment. No BlackGall blueprint or vein is shipped. |
| `Objects.json`: WaterPuddle, PeatBank, Duckboard; `RiverChunkBuilder` | WaterPuddle has real water LiquidPool/Thermal, PeatBank is a solid flammable destructible work face, Duckboard is destructible wood but grants no suppression of co-located wet hazards. Keep dry routes genuinely dry. |
| `VillagePopulationBuilder`; previous Wellmeet phase | The main well always lands at40,12. Reserve its commons and actual entrances. The existing scoped RespectInteriorReservations flag can protect this pair without changing other villages. Repair sites are only enabled at explicitly tracked villages; this phase does not broaden that policy. |
| `AreaCompositionScope`; current renderer | Retain finite address plus weak owner-world map authority, exact owner membership, portable actor variants and reskin guards. Rendering must never replay a generation plan after damage or movement. |

## Implementation sequence

1. Snapshot shared source and initial status. Reuse the immediately preceding
   complete FC22 run only after source synchronization is verified: 13,201
   tests, 13,169 passes and the same32 known baseline failures, zero C# errors.
2. Capture the current two native profiles at three seeds before changing
   generation. Include actual late service actors, terrain, glyphs, Parts and
   stock so art budgeting follows the native graph.
3. Author pure deterministic CinderholdCompositionPlan and SumpholdCompositionPlan.
   Expose logical rooms/work areas, ground/object cells, wet/interior/approach
   reservations and exact profile placements. Use different spatial rules;
   shared well coordinates do not justify copying Wellmeet's five-tent layout.
4. Write and capture actual core/adversarial REDs before production builders.
   Builders stage and validate actual native entities before mutation, reject
   populated/foreign graphs, emit explicit success/refusal diagnostics, and
   realize the old profile exactly once in a late semantic placement pass.
5. Wire exact village profiles into the manager. Preserve population, trade,
   containers, cave rolls/connections and house drama; keep late occupants off
   real stair/door/service reservations and off Sumphold's wet cells.
6. Author and capture art/renderer REDs. Reuse appropriate shipped libraries;
   add four variants per new family, at most two swatches, one-cell horizontal
   bounds and240 vertices per combined mesh. No per-voxel objects or colliders.
7. Verify real quest, forge/stock and terrain/player flows with both positive
   and negative controls; inspect native destruction and ownership refresh.
   Run separate bug-class adversarial files and an independent cold-eye review.
8. Render actual manager-generated chunks at three seeds. Fix generation/art
   rules rather than manually arranging the demonstrations. Close with full
   coverage, metadata/rebuild/source audits, exact full-suite comparison,
   living docs and a scoped commit. Then start the requested second pair.

## Performance and honesty bounds

Plans and staging collections allocate only during fresh generation. New
rendering decisions use the current dirty-cell/owner pipeline and cheap exact
lookups; no new frame/turn scan or LINQ allocation in a hot path. Read
`Docs/PERF-FOUNDATION.md` before renderer changes; use existing combined meshes
and static patch batching. Record generation observations without describing
them as measured gameplay FPS.

Can verify headlessly: actual native layouts, Parts, interaction consequences,
stock, ownership, routes, strict scope, static model coverage and material/mesh
budgets. Static gameplay-camera previews cannot establish live input feel,
animation quality or sustained frame rate. A reusable disposable native preview
command must leave the user's scene and current settlement registry intact.

## Implementation log

- 2026-09-15: Read CLAUDE.md, adversarial/performance methodology and current
  composition coverage. Two independent source surveys recommend Cinderhold
  and Sumphold. Corrected Sumphold's biome and both mapped tiers, generic river
  assumptions, forge/anvil semantics and the absence of boat/toll mechanics.
  These corrections precede new production. The user authorizes independent
  decisions and two further areas after this pair.

- Captured unchanged native census at three seeds (WB01). Visible late owners
  include village quest reskins (`b`, `h`, `f`, `c`, `P`, `p`) and Warren
  quest dirtgnomes; rendering must recognize actual conversation/quest/death-fact
  identity rather than suppressing them or accepting arbitrary glyph changes.
- Actual preimplementation gates: WB02 missing Sumphold types; WB03 missing
  Cinderhold types; WB04 missing both voxel-library types. Each run checks
  compiler errors before XML. WB05 isolated rendering assertion RED: 41 tests,
  6 passes / 35 expected failures / zero C# errors. Native and art fixtures were
  temporarily omitted only from this isolated RED project to expose renderer
  assertions; normal explicit synchronization restores them before GREEN.
- Verified correction: pruning posting currently changes Rot Choir reputation
  by **−9**, not the old historical −15. The existing live conversation remains
  authoritative; this phase changes neither dialogue nor reputation values.
- Shared integration now routes only the two exact address/profile pairs into
  their semantic bases and late profile/arrival passes. It keeps ordinary
  population, native cave rolls, stock, containers and drama, and reuses current
  native owner rendering without reconstructing missing terrain.

- WB06 exported the initial48 models with zero C# errors. WB07 compiled cleanly
  and ran253 focused cases:243passed,10failed. The failures exposed Cinderhold's
  rejected-call lifecycle (2), a Sumphold doorway path ending on the future well
  for one seed (1), absent drama reservation opt-in (4), and a late MarketStall
  missing art (3). Fixes preserve the original well and native owners.
- Review additions: scope drama reservation opt-in to these named sites, keeping
  the default policy unchanged elsewhere; add four MarketStall models and four
  carved quest-token models. The latter requires the actual native pickup
  objective, because distributable village quests can differ across hash
  domains. The art budget becomes56 models; no blueprint/content JSON changes.

- WB12 full run compiled cleanly:13,483 tests,13,447passed,36failed. Thirty-two
  failures match the baseline; four new manager-level rendering cases depended
  on leftover global loot tables from earlier suites. Unlike the native
  Cinderhold gameplay tests and integration fixtures, those four cases had not
  loaded the shipped stock registry. Their fixture now initializes actual
  content and resets it afterward. This is a harness isolation correction,
  not weakening the production builder’s stock validation. WB13 then passed all124 rendering/loot/trade isolation cases; WB14 confirmed the full run returns to exactly the32 original failure names and messages.

## Final review and verification

- WB09 confirms the lifecycle, well-frontage and drama reservation fixes. Its
  new art assertions fail before adding stall/token/path/water families. One
  player-flow test had an incorrect premise: Zone.ProjectPool already projects
  a live pool into permanent TileState, and UnprojectPool removes the projection
  with its last owner. The corrected test exercises actual MovementSystem wet
  contact, dry-board countercontrol and last-owner removal. No native liquid
  behavior was changed to satisfy an appearance test.
- WB10 exports64 models and renders the two native areas at three seeds, with
  zero missing meshes and zero unmodeled visible owners in all six captures.
  Independent review accepted low cutaway walls, quieter packed roads and the
  distinct teal wet cuts. Models remain one-cell owners, with small faces/tools
  necessarily small at full-chunk framing.
- WB11 passes all282 focused tests. Actual pruning acceptance/posting/payment,
  shop purchase/restocking, forge versus anvil, terrain destruction, wet/dry
  movement, runtime profile vetoes, late population, cave arrivals, reference
  identity, palette/bounds/corruption and ownership refresh are exercised.
- WB14 full regression: 13,451 passes, 32 unchanged baseline failures, zero C# errors and no new failures. All 282 additional tests pass.
- WB15 successfully rebuilds all262 final asset/metadata files byte for byte.
  The initial48 models and their metadata remain identical; only the two library
  assets expanded their entry arrays before the final64-model rebuild.
- Installed all64 models under the two build-visible Resources folders. No
  task GUID collision exists among 6,137 scanned metadata files.
  Final source/input hashes match the isolated Unity validation project.
- Shared files already containing other work remain installed and are captured
  as this phase’s exact `implementation.patch`. Initially clean shared files
  and new task-owned sources/assets/docs are committed directly. The patch’s
  reverse check passes; unrelated changes are not staged.

| Severity | Finding and resolution |
|---|---|
| 🟡 Native lifecycle | A refused base call cleared a valid Cinderhold plan. Rejection now preserves the successful owner and pending late-profile authority. |
| 🟡 Circulation | One seed aligned a Sumphold doorway with the future solid well. Its route now joins the clear northern frontage instead. |
| 🟡 Late population | House drama ignored reservations. Both exact named sites opt into reserved-cell exclusion for interior and fallback placement; ordinary villages retain their default. |
| 🟡 Coverage/readability | Late MarketStall and dynamic quest tokens needed models. Roads were too bright and pool surfaces looked like grass. Exact native mappings and16 additional coarse models fix the gaps. |
| 🔵 Source corrections | Current mapped tiers/biomes, native pruning−9 reputation, forge/anvil differences, descriptive boats/rolls and owner-projected water supersede earlier assumptions. |
| 🧪 Visual limits | Native headless contracts, static gameplay-camera composition and model coverage are verified. Live input feel, animation and sustained FPS were not measured. |
| ⚪ Native scope | No new boat driving, toll collection, Black-Gall mining, curing, save migration or global biome replacement is claimed. Existing fixture policies remain authoritative. |

Files: two logical plans and three builders per named site; core/adversarial
native fixtures; two voxel libraries, two offline art builders and art fixtures;
shared manager/scope/renderer/catalog/presentation integration; scoped drama
placement; a disposable native preview menu and gallery helper;64 generated
models, copied metadata, living docs and verification receipts. Exact file
paths and before/after hashes are in the integration manifest.
