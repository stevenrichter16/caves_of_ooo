# Tine: a lived-in lake shore

Status: complete and installed for fresh native generation. CQ13 passes all 433 targeted checks. CQ18 full suite: 14,476 passed, 32 unchanged baseline failures, zero C# errors and no new failures. CQ15 reproduces all 380 asset/metadata files byte for byte; CQ19 confirms installation and GUID uniqueness. CoO-original composition and art; no Qud-parity or live-FPS claim.

## Purpose

Give Tine three genuinely different relationships between a local lake, dry working fingers, the ordinary scribe's retreat, storage and homes. A wide quiet water mass and weathered gray shore structures distinguish this settlement from Sumphold's peat work cuts. The lake is authored locally; the world map has no river here. Keep all ordinary village services, their inventories, repair sites, cave rolls and distributed quests alive. No fishing system, functioning boat, retired Reader dialogue or new god lore is added or claimed.

## Verified contract / corrections

| Read source | Verified result / design consequence |
|---|---|
| CLAUDE.md | Actual RED before production; atomic generation, countercontrols, cold-eye/adversarial review and living docs are mandatory. |
| WorldMapAuthoring.cs:214 and literal biome/tier/road/river tables | Before this slice Tine was Spread tier1, road=true, river=false, ordinary Village profile=null. This slice adds the specific LakesideVillage profile without changing balance or map hydrology. |
| Lore/History/02_Geography.md:60; current Lore/10_Bible.md | Quiet fishing settlement on a flood-remnant lake; retired scribe is lore context. No matching retired-scribe/fishing runtime owner exists. This slice retains ordinary real copy service and provides a lakeside retreat; it does not fabricate the unshipped narrative system. |
| OverworldZoneManager.CreateVillagePipeline | Plain villages currently receive an unconditional decorative river. Root replaces that for this precise profile with the local lake and preserves later population/trade/containers/drama. |
| VillagePopulationBuilder.cs:146,800–842 | One ordinary Scribe has Scribe_1 and two InkVials added by native StockScribe. Keep this sole owner/service instead of authoring a duplicate pseudo-scribe; plan exposes a retreat anchor for root's optional service-placement hook. |
| Objects.json / WaterPuddle | Actual120-volume water LiquidPool, material Water and Thermal. Zone pool projection owns the water coating; art is only a view of current owners. |
| Objects.json / Duckboard | Native wood/flammable, destructible HP8, nonsolid. Place on actual dry fingers, not as a false movement bridge over a still-present pool. Destroying it leaves native underlying ground. |
| Objects.json / BoatFrame | Solid, nonsafe-to-take PhysicalObject, examinable upside-down hull on trestles. Description says Sumphold-built; reused as an imported work frame. No Vehicle, Container or Destructible Part. |
| Objects.json / Bookshelf | Existing bookshelf is toppled. Do not pretend it is an upright scribe archive; ordinary chairs/storage suffice. |
| Completed FirstTent/Sumphold builders and kits | Staged base/profile/arrival; exact finite address; current3×3 dry arrival checks; four variants, shared material, at most2 colors and240 vertices within one-cell XZ. |

## Logical formations

- **SouthernReach:** broad southern water, two dry fingers from the inhabited north bank, western low store and northern retreat/homes.
- **EasternCove:** tall eastern cove, two eastward work fingers, west-side homes and a separated northern scribe retreat.
- **NorthernInlet:** broad northern water, fingers from the southern occupied shore, retreat and store facing the lake from below.

Seeded shape jitter changes contours, room dimensions and colony placement within each formation. Shared main well at40,12 stays dry/open. All four edge arrival directions have connected dry routes. Water is excluded from initial population/caves and never counted as a dry route. Native LakeHouse/ShoreStore/ScribeRetreat interiors use actual StoneFloor and SandstoneWall; mapped approaches use RoadStone, outdoors Floor. Docks use actual Duckboard cells. Reeds group at shore; a small number of Trees/Bushes frame dry edges. Avoid blanket clutter and four equal rectangular corners.

## Ownership and staging

`TineCompositionPlan` exposes ZoneID/ProfileID/FormationName, immutable Rooms/Profile collections, GroundAt/ObjectAt/IsInterior/IsApproach/IsReserved/IsWet, ScribeX/Y, and Signature. Only intent is stored; runtime rendering must use actual current owners.

Base priority1000 validates every required native blueprint (including late work frames), stages all actual entities, then publishes owners/flags/reservations atomically. Profile priority3860 places exactly two BoatFrame owners once, before population. Arrival priority3870 reserves actual dry stair neighbors. CanPlaceCaveEntrance verifies the current realized zone and each cell in the3×3 footprint: exterior, unreserved, nonblocking, no current liquid pool, never a foreign graph or detached cell. No replay regenerates destroyed fixtures.

Implemented native identities: Floor, StoneFloor, RoadStone, SandstoneWall, WaterPuddle, Duckboard, Reeds, Tree, Bush, Bed, Chair, Crate; profile BoatFrame×2. Generic later population supplies Scribe/Merchant/Quartermaster/Innkeeper/Elder and real repair/service owners. No new blueprint fragment is necessary.

## Art contract

New TineVoxelKitLibrary/Builder/Tests:20 models,5 families×4variants. `tine-ground`→Floor; `tine-wall`→SandstoneWall; `tine-water`→WaterPuddle; `tine-pier`→Duckboard; `tine-path`→RoadStone. Quiet continuous ground/water tops, broad low gray walls and coarse plank piers. Reuse ordinary service actors, interior floor, frames, flora and furnishing from already verified kits. No new textures, colliders, scripts or lights; native owners govern visibility/interaction/destruction.

## Gates and review

Tests first: strict finite address/profile; deterministic seed and three semantic relationships; connected dry shores/doors/four edges; real water/wood Parts; profile exactly-once and foreign/replayed rejection; atomic malformed-content failure; retry lifecycle; real current3×3 arrival footprint; source-owned scope; actual later services/Scribe ink stock; pool removal and native duckboard destruction; imported exact references,2colors/240vertices/onecell, variant uniqueness and corruption controls. Root captures missing-type RED before production, then targeted/native previews across all formations, independent cold-eye, full suite, source/GUID/byte-rebuild audits.

Can verify with code/tests: owners/geometry/stock/reservations/connectivity/current water projection and reference integrity. Camera previews can verify static composition/readability. Cannot infer live input feel, animation or sustained FPS from headless tests/screenshots.

## Implementation log

- Preproduction: surveyed raw native content and existing presentation. Root accepted Tine as one of four additional area systems; no new fictional owner/verbs are needed.
- CQ01 actual RED: root captured missing-type compile failure with all four site test fixtures present. Compilation stopped at Quillhold/Gantry type signatures before method-body analysis; no executed-tests or per-type missing-error claim is made. Root authorized production after this receipt.
- Implemented the three semantic lake formations, atomic native base/profile/arrival builders, four service anchors and initial16-model art generator. Nothing was manually placed in a demonstration scene.
- CQ03 native gate: root reports 261 total /260 pass, zero C# errors; the sole failure was not-yet-generated Tally art. Every executed Tine native test passed.
- Additional pure C# logical check (outside Unity, no game-state mutation): 516 seeds including integer extremes passed dry-pocket, service-anchor and water/board-count checks. Representative pools: seed64 EasternCove296 water/87board cells;1729 SouthernReach368/42;729490642 NorthernInlet371/42. This supplements, rather than replaces, native graph tests.
- Native contact and real material-destruction hypotheses added after the first gate: movement into water vs dry boards, removal of the owner projection, destroyed plank/wall/tree preserving native ground and distant frames. These are existing-mechanic regression pins; all passed in CQ06.

- CQ05 first actual native camera review across seeds64/1729/729490642: broad lake and distinct pier directions readable, but shared bright gold RoadStone and pale wall95 tops dominated. Added a local path family and subdued stone luminance assertions BEFORE changing art. Parent found missing Reeds alias separately; shared coverage fix is parent-owned.
- CQ06 actual refinement RED:443 total404 pass39 fail, zero C# errors. Intended Tine failures included missing path/count20 and four wall-top brightness cases; every native Tine case, including added contact/destruction hypotheses, passed. Authorized source correction adds four quiet RoadStone64 variants and changes only existing wall UV slots13/95→56/107. Other twelve model geometries/materials stay identical. Rebuilt native camera captures reviewed in CQ08; focused/full final gates pending.

- CQ08 refined native captures: independently inspected all three Tine images. Seed64 has the eastern cove with northward retreat and two eastward fingers;1729 has southern water with two southward fingers and western store;729490642 has northern water with south-side retreat/store and northward fingers. Walls/doors, direct dry public paths and furniture grouping match the logical plan. Reeds now render from their real shoreline owners. Storage/household functions read from crates/beds; small NPCs retain native examination/dialogue identity rather than visually legible per-role labels. No material static visual defect remains. Parent reports zero missing/unmodeled owners across all12 captures; no live FPS or input-feel inference.

- CQ10 focused GREEN verified directly from receipt/XML:429/429 passed, zero C# errors, Unity exit0. Owned Tine fixtures contribute112 passing cases (20 core,55 adversarial,37 imported-art). They exercise real native movement into water vs dry boards, destruction preserving ground/other owners, current3×3 cave landings, the full village pipeline's sole stocked Scribe at the retreat anchor, and strict imported mesh/prefab/material contracts. Parent rendering tests additionally pass actual removed-owner geometry, unchanged travelling actor identity and no floor reconstruction. These are native graph/action tests in EditMode, not a claim of manual live play or PlayMode.
- CQ12 final build receipt: Unity exit0, zero compile errors; all12 native captures have zero missing meshes and zero unmodeled owners. Independently viewed all three final Tine images again: quieter paths/walls, continuous water, visible reeds and service rooms remain accepted. The Scribe's shared canonical body now stays consistent across the four new areas after the parent-owned travel identity fix. Water counts remain296/368/371 with two native frames per formation.
- Independent filesystem audit `/tmp/civic-quartet-independent-mesh-audit.json` decoded all92 imported meshes across four kits (Gantry28,Tine20,Quillhold28,Tally16). Positions/indices/bounds, maximum240vertices, at most2 palette swatches, four distinct variants/family, exact prefab/mesh/material/library references and script/collider/light-free prefabs all pass. Tine remains20models/max120vertices. No asset or production source mutated in this audit. Root owns final GUID collision and deterministic rebuild gates.

## Current self-review

- 🔵 Exact source identities and inherited Parts checked against native content. Physical boat frames retain imported Sumphold wording; there is no operating boat/fishing interaction.
- 🔵 Service anchors are distinct unreserved StoneFloor, with one protected adjacent standing cell. Root's ordinary population delegate claims them before clutter, retains stock and wiring, and checks current ownership.
- 🔵 Logical plan and renderer authority remain separate. Live pools supply water projection; unprojected cells are not reconstructed by art. The low walls/boards do not alter collision or damage.
- 🧪 Initial actual camera review caught excessive path/wall brightness; the correction is covered by actual RED before source changes. CQ08 refined static camera review accepted. Imported focused tests pass; final full/rebuild gates remain. No FPS/play-feel claim.

## Changed files

- New TineCompositionPlan and TineCompositionBuilder (including profile and arrival classes), native core and dedicated adversarial tests.
- New TineVoxelKitLibrary, editor TineVoxelKitBuilder, TineVoxelKitTests; copied metadata with unique GUIDs.
- This plan and TINE-VOXEL-KIT.md. Shared routing, service placement and rendering changes are parent-owned and documented in the four-area phase plan.


## Final integration and verification

The pending gates in earlier chronological entries are closed. CQ14 exposed one additional literal equipment-list failure: GantryRegistrar was missing from the expected kit table. The retained equipment ownership/loot exclusions now include its intentional gloves and boots; CQ17 verifies the complete equipment fixtures before the final full run. CQ13 passes all 433 phase checks, including 219 cases in dedicated adversarial fixtures. CQ18 adds 434 passing cases to the verified TC20 baseline: all 32 existing failures have identical test names and failure messages. One former Tine negative control was renamed and both Tine/Gantry plain-village controls now explicitly select an ordinary profile; their negative assertions remain intact.

[Final twelve-view gallery](Verification/VoxelWorld/CQ12-final-preview/index.html) covers all three actual formations in each of the four towns. All requested seeds are nonzero and match each native manager's effective seed. Every capture has zero missing meshes and zero unmodeled visible owners. CQ05/CQ08 seed-zero formation labels are superseded evidence. Independent source, player-flow and final visual reviews are complete.

The phase adds 92 combined-mesh models, with four variants per family and no more than two palette colors per model. CQ15 rebuild reproduces all 380 asset and metadata files byte for byte. CQ19 checks the installed resources, GUID uniqueness and exact synchronization of 2,099 source/content/assembly/meta inputs between the working project and the isolated validation project. [Close-out evidence](Verification/VoxelWorld/CQ19-closeout/README.md) records the case comparison, asset hashes and exact shared-source delta.

Fresh native generation receives the new rules at Gantry (7,8), Tine (13,7), Quillhold (14,9) and Tally (10,14), surface depth 0. Existing graph instances remain intact. Native trade, containers, copying, lodging, rental frontages, water contact, destruction and entity lifetimes remain authoritative. The current spawn, 1.2x camera and full reveal are unchanged. The original Unity editor was not restarted. Static captures establish scene composition and coverage; they do not measure live input feel, animation or sustained frame rate.

New files and originally clean shared files are committed directly. Exact changes in already-mixed shared files are installed in the working tree and preserved in the close-out implementation patch, without committing unrelated work. This is a scoped checkpoint of the existing mixed workspace, not a standalone clean-checkout claim.
