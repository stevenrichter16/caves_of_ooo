# Quillhold — public archive composition

Status: complete and installed for fresh native generation. CQ13 passes all 433 targeted checks. CQ18 full suite: 14,476 passed, 32 unchanged baseline failures, zero C# errors and no new failures. CQ15 reproduces all 380 asset/metadata files byte for byte; CQ19 confirms installation and GUID uniqueness. CoO-original composition and art; no Qud-parity or live-FPS claim.

## Scope and source verification
Exact `Overworld.14.9.0`, Village/`PrimaryArchive`; current map Spread tier 1, road present, no river. Older geography calls it tier 2; native authored tier remains unchanged. Ordinary Spread already excludes named places. Root owns profile data, exact scope and shared pipeline/renderer integration.

Sources read: `Lore/Factions/02_Recension.md` §IV; `Lore/History/08_MaterialCulture.md` memory-made-total; `Lore/History/02_Geography.md` Quillhold; `WorldMapAuthoring.cs`; `VillagePopulationBuilder.cs`; `TradeStockBuilder.cs`; `TraderRestockSystem.cs`; native Scribe dialogue and CopyGrimoire action.

### Corrections established before implementation

| Premise | Verified correction | Source |
|---|---|---|
| Quillhold is tier 2 in the active map | Current authored tier is 1; older lore numbering is retained only as history | WorldMapAuthoring TierRows, WorldGenerator.PlacePOIs |
| Existing named scribe is portable | RecensionScribe explicitly describes the Ledger expedition; Scribe_1 is portable | FriendlyNPCs.json:1536,1297 |
| Copying costs ink | CopyGrimoire charges nothing and retains the original; ink is actual trade/rental supply | ConversationActions.CopyGrimoire |
| Existing marble/shelf blueprints are ordinary archive furniture | Library variants are protected sealed content; Bookshelf is toppled and lacks Destructible | Objects.json LibraryMemoryMarbleWall, SealedArchiveShelf, Bookshelf |
| Village river represents mapped water | Generic village pipeline stamps its bottom river even at this dry mapped site | OverworldZoneManager.CreateVillagePipeline |
| Faction villagers get generic random stock | TradeStockBuilder filters Villagers; explicit TraderPart stock and non-Villager renewals survive faction retagging | VillagePopulationBuilder.WireNPC, TraderRestockSystem |

## Native contract
Three deterministic formations: CrossAisleScriptorium, PairedReadingCourts, LongArchiveSpine. Four semantic roles: CopyHall, Stacks, Refectory, Receiving. Shelves define usable aisles rather than uniform decoration. Public arrival paths join actual doors, main well at 40,12 and four chunk edges. New physical archive wall/shelf/copy desk/refectory table are ordinary destructible owners; archive shelves are real Container8 storage. Profile has six maintained archive shelves; the single native village Scribe is placed at the copying station by a scoped preferred-service-cell hook. The public copying service uses existing Scribe_1 and preserves the original grimoire. It charges no ink or drams. No Reader channel, Archive Master, First Account, Body Reading, new divine mechanic or quest is asserted.

The existing RecensionScribe belongs to the Drowned Ledger expedition; PalimpsestEcho opens a field-station dialogue. Neither is relocated. LibraryMemoryMarbleWall and SealedArchiveShelf carry protected sealed-library semantics; neither is reused. Bookshelf is toppled and lacks Destructible, so maintained shelves get their own native blueprint. CopyDesk is furniture, not the corpse-reading table.

## Implementation plan
1. Capture missing-API native/art RED.
2. Implement pure plan; atomic native base1000, profile3860, arrivals3870. Stage every owner before zone mutation; reject malformed content and foreign/replayed authority without invalidating successful state.
3. Supply four blueprint definitions as a fragment for root's surgical merge; the user's voxel-only direction makes the real 3D kit the art deliverable for this wave.
4. Build 28 coarse voxel models: four variants each of ground, path, wall, shelf, desk, scribe, table. At most two palette swatches/model; one-cell footprints; mesh batching; no prefab gameplay, lights or colliders. Ground appearance constant except buried thickness.
5. Real manager/copying/stock/destruction/arrival/adversarial gates; native camera previews and generator-rule refinements.

## Risks and verification gates
Native faction wiring makes residents Palimpsest, so Villagers-only TradeStockBuilder random goods do not apply. Explicit TraderPart stock plus existing non-Villager restock are retained. The unconditionally generated bottom village river is omitted only in this exact composed scope. Native settlement records, repair sites, population, containers, cave exits and house drama remain. Main well position and its apron are reserved before population.

Counterchecks cover malformed IDs, wrong map type/profile, cloned foreign cells, dependency corruption, profile replay, missing original/copy-only grimoire, stock factory gates, unsafe neighboring cave cells, four-edge access and open pockets over 32 seeds plus extremes.

## Review / limits
Implementation and the bounded native/art/camera review are complete. Aggregate full-suite verification remains pending in this document. Headless native renders can verify scene-owner coverage and composition, not live camera feel or frame time. No exhaustive all-seed proof is claimed.

## Implementation log
- Source sweep corrected tier, river, copying fee and unsafe reusable-owner premises before implementation.
- CQ01 actual missing-type RED captured by root before production; no test execution claimed for the compile-blocked run.
- Pure plan, staged native builders, four-blueprint merge fragment and 28-model art generator authored. Copying service uses the single native Scribe via preferred-service-cell hook; no extra actor or stock system. Six shelves contain four ink vials and two existing utility grimoires (Ward Gleam and Drying Breeze).

### First-pass self-review
- Native late profile owns only six shelves; the single guaranteed Scribe remains population-owned and uses the existing copying dialogue. Its preferred CopyHall cell is unreserved interior and distinct from furniture/profile/path.
- Native destruction tests cover all four new furniture types; shelf contents retain entity identity and spill once. The protected sealed-library wall is an explicit opposite control.
- At first pass, core plus dedicated adversarial source defined 73 native cases and art defined 15. Current refinements bring the source to 81 native cases and 16 art cases. CQ03 passing claims apply to the first pass. CQ10 subsequently passed all 97 current Quillhold cases (81 native and 16 art), verified directly from its XML.
- Seven copied source/test metadata GUIDs audited against all Assets metadata: no collisions.
- The final generated camera views have been reviewed; headless assessment still cannot establish live movement feel, every gameplay zoom or long-session performance.

### Cold-eye refinement
Root review found a real role mismatch: the planned Refectory contained beds and a chair, communicating dormitory rather than communal eating. Three native composition assertions and one art-family assertion now require two individually owned dining-table cells with four chairs, no beds in the refectory, and a separate courier rest bed in Receiving. CQ04 captured all three native role assertions failing with zero C# errors; root authorized the table correction. Two native table cells and four chairs now occupy the Refectory, with a separate bed in Receiving. The fourth physical blueprint is QuillholdRefectoryTable; no new food/cooking service is implied.

The latest user direction explicitly converts content to voxels; this wave's real 3D assets satisfy the content-art requirement. No new 16px art is promised. Existing legacy renderer fallback remains a known presentation limit for the new physical owners.

- CQ03 integrated native run: 261 total / 260 passed / one missing Tally kit asset failure, zero C# errors. All current Quillhold native cases passed. CQ04 subsequently recorded the refectory role failure before its correction.

- The independent Scribe-frontage review found its actor cell protected by native population selection, but no adjacent standing cell reserved. A 34-seed assertion recorded the missing apron; CQ06 captured its actual RED before the reservation fix.

- CQ06: 443 combined cases /404 passed/39 failed, zero C# errors. New refectory native role checks passed; Quillhold art still expected a rebuild from24 to28 models. The new standing-apron assertion failed at seed0 as expected; the generator now reserves the free interior cell immediately on the Scribe standing side, reflected together with the southern layout.
- Initial CQ05 native renders independently inspected at64/0/729490642: clean routes in the visible layouts, but six shelves were isolated ornaments in a wide hall. New 34-seed topology assertion requires two connected straight runs of three and a clear crossing aisle, preserving six actual containers and existing loot budget. CQ07 captured six disconnected shelf components versus the required two, before the rule changed. Two horizontal three-cell bookcase runs now define a three-cell crossing aisle; Stacks footprints are reduced (wide21–23×9, tall14–15×15) to fit the six real containers. No loot or actor count changes.

## Files and integration contract
New Quillhold-prefixed files: native plan, staged base/profile/arrival builders; core and dedicated adversarial fixtures; strict voxel library, reproducible editor kit builder and art fixture; this living document. The four-blueprint JSON fragment is staged under the persistent civic-quartet validation directory for root's surgical merge. Shared manager/profile data, population service placement hook, scope and presenter remain root-owned.

```csharp
var archive = new QuillholdCompositionBuilder(worldSeed); // priority 1000
// Native connectivity and filtered cave placement precede the profile.
pipeline.AddBuilder(new QuillholdProfileBuilder(archive)); // 3860
pipeline.AddBuilder(new QuillholdArrivalReservationBuilder(archive)); // 3870
// Existing VillagePopulation uses archive.TryGetServiceCell for Scribe only.
```

CQ07 isolated refinement run: two tests, two expected failures (Quillhold shelf connectivity and separate shared reed rendering), zero compile errors. Shelf generator correction authored only after that result. CQ10 later passed all Quillhold cases, and CQ12 supplies the corrected final native views.

### Review findings and verification boundary
- 🟡 Resolved in source after CQ04 RED: refectory furnished as a dormitory. Communal tables/chairs replace beds; Receiving retains a separate rest corner.
- 🟡 Resolved in source after CQ06 RED: native Scribe's own selected cell was protected, but its standing access was not. One adjacent free interior cell is now reserved before layout reflection.
- 🟡 Resolved in source after CQ07 RED: six isolated cabinets failed the promised archive aisle grammar. Two connected three-cell bookcase runs and compact storage footprints replace the scatter.
- 🟢 CQ10 passes all 97 current Quillhold tests. CQ12 verifies final native visual coverage for three actual formations with matching effective seeds. CQ05/CQ08 zero-seed labels remain historical invalid preview evidence, superseded by CQ12. Aggregate full-suite close-out is not inferred from these focused results.
- ⚪ Deferred by scope: Reader channeling, First Account, archive-master quest arc, industrial ink manufacture and paid copying. Existing native copying remains free; no diagram or decorative object pretends to implement these mechanics.

Can verify headlessly: exact owner identity/counts, native services and stock, adjacency and four-edge traversal in tested seeds, successful destruction/contents spill, strict kit references and scripted native recipe coverage. Cannot verify from these runs: live movement feel, readability under every gameplay camera/zoom, long-session frame time or every possible seed.

### CQ08 refined camera review
All three Quillhold PNGs inspected independently. Seeds64 and729490642 show the intended connected bookcase runs, smaller Stacks, shared dining table with four chairs, and separate receiving-room bed. Native report has zero missing/unmodeled owners. No new material layout issue found in those two formations.

🟡 Preview harness finding, subsequently corrected after CQ11 RED: Quillhold-0.png shows the mirrored paired-court layout while its receipt labels LongArchiveSpine. ZoneManager treats seed0 as Environment.TickCount; preview labels were computed from literal0. This is a non-deterministic preview/metadata mismatch, not a pure-plan generation defect. Root is responsible for selecting nonzero representative seeds and asserting requested/effective seed agreement. A corrected actual LongArchiveSpine view is required before claiming three-formation visual coverage; neither CQ05 nor CQ08 seed0 proves it.

### Final CQ10/CQ11/CQ12 verification and visual assessment
- CQ10-focused-green: 429/429 combined cases passed, zero compile errors. Direct XML inspection identifies all 97 Quillhold cases as Passed (81 native, 16 art); none skipped.
- CQ11-preview-seed-red: four cases, two pass and two expected zero-seed failures for Quillhold/Tally, zero compile errors. The preview then switched to nonzero representative seeds, asserted `manager.WorldSeed == requestedSeed`, and recorded `actualWorldSeed`. The game's existing seed-zero behavior was not changed.
- CQ12-final-preview: all three final Quillhold PNGs independently inspected, including actual LongArchiveSpine seed2. Native receipts agree with image composition and effective seed.

| Formation | Seed / actualWorldSeed | Native visible contract | Missing / unmodeled |
|---|---|---|---|
| CrossAisleScriptorium | 64 /64 | Six shelves, two copying desks, two dining-table cells, one bed, one Scribe, one well | 0 /0 |
| PairedReadingCourts | 729490642 /729490642 | Same fixed owner contract; receiving and communal eating rooms north of the route | 0 /0 |
| LongArchiveSpine | 2 /2 | Compact tall Stacks on the west, east-facing aisle entrance; copying hall and shared dining room east | 0 /0 |

The final views communicate separate functions: two connected bookcase runs with crossing access; pale folios on low copying desks; a broad joined dining table surrounded by four native chairs; and a separate bed beside receiving/storage goods. Low marble walls expose the interiors and doorways. The compact western Stacks makes LongArchiveSpine visibly distinct from the two court layouts. Ground stays quiet; color variation does not add a checkerboard. All scenery still belongs to individual native cells and preserves native destruction/storage.

No additional material composition issue found in these final three views. Native movement and destruction conclusions come from the tests, not the image alone. The open courts remain deliberately spare; this is a public archive district, not the deferred divine/deep archive content. The seed-zero visual-review blocker is resolved. Live movement feel, arbitrary camera/zoom readability, all possible seeds and frame-time guarantees remain outside this headless signoff.


## Final integration and verification

The pending gates in earlier chronological entries are closed. CQ14 exposed one additional literal equipment-list failure: GantryRegistrar was missing from the expected kit table. The retained equipment ownership/loot exclusions now include its intentional gloves and boots; CQ17 verifies the complete equipment fixtures before the final full run. CQ13 passes all 433 phase checks, including 219 cases in dedicated adversarial fixtures. CQ18 adds 434 passing cases to the verified TC20 baseline: all 32 existing failures have identical test names and failure messages. One former Tine negative control was renamed and both Tine/Gantry plain-village controls now explicitly select an ordinary profile; their negative assertions remain intact.

[Final twelve-view gallery](Verification/VoxelWorld/CQ12-final-preview/index.html) covers all three actual formations in each of the four towns. All requested seeds are nonzero and match each native manager's effective seed. Every capture has zero missing meshes and zero unmodeled visible owners. CQ05/CQ08 seed-zero formation labels are superseded evidence. Independent source, player-flow and final visual reviews are complete.

The phase adds 92 combined-mesh models, with four variants per family and no more than two palette colors per model. CQ15 rebuild reproduces all 380 asset and metadata files byte for byte. CQ19 checks the installed resources, GUID uniqueness and exact synchronization of 2,099 source/content/assembly/meta inputs between the working project and the isolated validation project. [Close-out evidence](Verification/VoxelWorld/CQ19-closeout/README.md) records the case comparison, asset hashes and exact shared-source delta.

Fresh native generation receives the new rules at Gantry (7,8), Tine (13,7), Quillhold (14,9) and Tally (10,14), surface depth 0. Existing graph instances remain intact. Native trade, containers, copying, lodging, rental frontages, water contact, destruction and entity lifetimes remain authoritative. The current spawn, 1.2x camera and full reveal are unchanged. The original Unity editor was not restarted. Static captures establish scene composition and coverage; they do not measure live input feel, animation or sustained frame rate.

New files and originally clean shared files are committed directly. Exact changes in already-mixed shared files are installed in the working tree and preserved in the close-out implementation patch, without committing unrelated work. This is a scoped checkpoint of the existing mixed workspace, not a standalone clean-checkout claim.
