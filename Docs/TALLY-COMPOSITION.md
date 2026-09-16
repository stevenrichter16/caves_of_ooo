# Tally — the working Central Exchange

Status: complete and installed for fresh native generation. CQ13 passes all 433 targeted checks. CQ18 full suite: 14,476 passed, 32 unchanged baseline failures, zero C# errors and no new failures. CQ15 reproduces all 380 asset/metadata files byte for byte; CQ19 confirms installation and GUID uniqueness. CoO-original composition and art; no Qud-parity or live-FPS claim.

This CoO-original composition covers only Overworld.10.14.0, with the new CentralExchange profile. The current authored map is Spread tier 1, road true, river false. Lore/Factions/04_SaccharineConcord.md §IV describes the logistical hub; Lore/History/08_MaterialCulture.md describes practical modular construction. Lore tier 2 and Counter audience are not current native map/mechanics contracts.

Three seeded formations organize ExchangeHall, GoodsStore, RentalFrontage and RestCourt around broad receiving routes and a native well at (40,12). Floor is ordinary exterior, RoadStone marks used routes, StoneFloor is actual interior ground. TentWall buildings use low modular cloth-and-timber partitions. Native Crate storage and Bed/Chair rest furnishings establish real functions. The late profile adds stocked Chest and Campfire owners. Generic village merchants, explicit TraderPart stock, rental and village services remain authoritative; faction-dependent TradeStock behavior is not rewritten. No ConcordFactor, FilerClerk, Counter audience, caravan hire, debt or counterfeit quest destination.

Generation is staged before mutation, exact-address/profile scoped by root integration, and reserves dry approaches before native population and drama. Live 3×3 cave candidates reject wet or blocked neighbors; late arrival reservation protects actual rolled stairs. Native entity ownership controls rendering and destruction.

Four new art families (four variants each): Floor→ground, RoadStone→path, TentWall→wall, Crate→crate. Shared existing art supplies interior floor, beds, chairs, fire, chest and service actors. Every mesh uses at most two palette swatches and one-cell bounds; generated prefabs contain geometry only.

Verification: initial deterministic/scope, native circulation/role furnishings, late owner/replay and art-budget assertions written before implementation. CQ01 actual missing-type RED confirmed; current adversarial fixtures cover malformed dependencies, atomic preflight, foreign/replayed profile rejection, live wet/solid cave neighbors, 34 seed traversal and native rental stock/service-frontage integration. All-owner preview and full regression remain pending. Static assertions cannot certify gameplay-camera readability or live feel.

## Implementation log

- Verified no WoodenWall blueprint exists. Reused TentWall: actual Cloth material, thermal propagation, HP 8 and solid occupancy; wooden supports are only part of its visual construction. No shared Objects.json change.
- Added four role shelters with three role-position formations, broad offset outdoor RoadStone connections and protected interior aisles. Merchant/Quartermaster/Innkeeper expose deterministic unreserved native service cells adjacent to protected standing routes through TryGetServiceCell. Root owns the optional VillagePopulation service-placement hook.
- Base1000 stages/validates native entities before mutation; Profile3860 places one stocked Chest and one native Campfire exactly once; Arrival3870 reserves rolled stairs after the live dry 3×3 entrance filter. Existing CampGoodsT1 loot and Campfire Fuel/LightSource/Thermal/Wood contracts are retained.
- Four geometry-only families × four variants, native ring material; constant full-cell ground/path planes, low cloth partitions and bound crates. Model assets will be produced by root using TallyVoxelKitBuilder.Run.

### CQ05 visual review → CQ06 refinement

All three initial previews exposed the same four-rectangle outline and pale per-cell wall/crate stripes. CQ06 captured three actual assertion REDs: the open hall had only 3 clear entry cells instead of 21; crate mesh had 72 vertices instead of the 48 budget; wall had 96 instead of 48. The CoveredExchangeSpine now has a full loading face and a broad protected outdoor apron, genuinely changing approach circulation while keeping the other formations' small shelter entrances. Cloth wall and timber crate each become two broad masses; palette slots 12/107 and 8/9 respectively replace bright repeated trim. No native material, destruction, shade or stock semantics changed. Rebuilt-art and native regression verification pending.

### CQ08 preliminary gameplay-camera review (superseded for seed provenance)

Inspected all three refined native Tally renders (seeds 64, 0 and 1) in [CQ08 preview evidence](Verification/VoxelWorld/CQ08-refined-preview). The orange-brown crate bodies now read as single wooden goods containers; the earlier pale barcode effect is absent. The low cloth wall masses form continuous quiet boundaries without bright per-cell trim. Seed 0's broad open exchange face and loading apron are visibly different from the enclosed shelter arrangements at seeds 64 and 1. Native actors, rest beds, stocked chest and routes remain legible at the existing camera.

Source review of the apron found no new pocket-forming obstacle: the rule removes the hall's front wall cells and marks otherwise empty exterior cells as reserved walkable RoadStone. It does not add a barrier, move the native well, widen a mesh outside its owning cell, or claim interior shade outside the original hall. All final circulation and native service assertions must still pass after late population.

Bounded visual acceptance: no material composition/readability regression observed in these three static views. They do not certify movement feel, actor motion, unseen seeds, dynamic fire/occlusion or live trade UI. Gantry's three CQ08 views were independently inspected as a cross-review; open exchange fronts, registry and guest-rest spaces remained readable, with no material visual defect identified. Full final tests and installation remain root-owned verification steps.

### CQ12 final camera assessment and seed-provenance correction

CQ11 exposed a preview-only seed-0 sentinel bug: passing zero to the native manager requests a generated world seed rather than the intended deterministic seed. Therefore CQ08's image labelled Tally-0 is useful only as a preliminary visual observation, not evidence that the native scene corresponds to plan seed 0. Root corrected preview selection and checks the manager's actual WorldSeed; the CQ12 receipt now records matching requested/actual nonzero seeds.

Inspected final Tally-5 (CoveredExchangeSpine) and all three final Gantry renders (64 RegistryCrossing, 1729 CrossingExchange, 2 CaravanForecourt). Tally's actual seed-5 loading frontage remains clearly open and connects directly to its broad apron, with continuous quiet boundary masses and distinct wooden goods. No material visual regression was found. CQ12's receipt includes Tally 64 ForkedExchangeApron, 1 OffsetLoadingCourts and 5 CoveredExchangeSpine; all requested seeds match actualWorldSeed and each has zero missing meshes and zero unmodeled native owners. Final evidence is [CQ12 preview receipt](Verification/VoxelWorld/CQ12-final-preview/preview-receipt.json), superseding CQ08 seed-provenance claims.

Independent final source review of shared integration found no material defect: actual native membership/visibility and site profile remain rendering gates; moving Scribe/Registrar/guest-host bodies retain their shared model identities within the four-area scope; all borrowed host/scribe meshes are registered before presentation. This review does not claim unrestricted art coverage outside the composed scope. Final full-suite results and installation remain separate root verification.


## Final integration and verification

The pending gates in earlier chronological entries are closed. CQ14 exposed one additional literal equipment-list failure: GantryRegistrar was missing from the expected kit table. The retained equipment ownership/loot exclusions now include its intentional gloves and boots; CQ17 verifies the complete equipment fixtures before the final full run. CQ13 passes all 433 phase checks, including 219 cases in dedicated adversarial fixtures. CQ18 adds 434 passing cases to the verified TC20 baseline: all 32 existing failures have identical test names and failure messages. One former Tine negative control was renamed and both Tine/Gantry plain-village controls now explicitly select an ordinary profile; their negative assertions remain intact.

[Final twelve-view gallery](Verification/VoxelWorld/CQ12-final-preview/index.html) covers all three actual formations in each of the four towns. All requested seeds are nonzero and match each native manager's effective seed. Every capture has zero missing meshes and zero unmodeled visible owners. CQ05/CQ08 seed-zero formation labels are superseded evidence. Independent source, player-flow and final visual reviews are complete.

The phase adds 92 combined-mesh models, with four variants per family and no more than two palette colors per model. CQ15 rebuild reproduces all 380 asset and metadata files byte for byte. CQ19 checks the installed resources, GUID uniqueness and exact synchronization of 2,099 source/content/assembly/meta inputs between the working project and the isolated validation project. [Close-out evidence](Verification/VoxelWorld/CQ19-closeout/README.md) records the case comparison, asset hashes and exact shared-source delta.

Fresh native generation receives the new rules at Gantry (7,8), Tine (13,7), Quillhold (14,9) and Tally (10,14), surface depth 0. Existing graph instances remain intact. Native trade, containers, copying, lodging, rental frontages, water contact, destruction and entity lifetimes remain authoritative. The current spawn, 1.2x camera and full reveal are unchanged. The original Unity editor was not restarted. Static captures establish scene composition and coverage; they do not measure live input feel, animation or sustained frame rate.

New files and originally clean shared files are committed directly. Exact changes in already-mixed shared files are installed in the working tree and preserved in the close-out implementation patch, without committing unrelated work. This is a scoped checkpoint of the existing mixed workspace, not a standalone clean-checkout claim.
