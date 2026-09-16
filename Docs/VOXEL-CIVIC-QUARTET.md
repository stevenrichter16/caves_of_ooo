# Civic Quartet — Gantry, Tine, Quillhold and Tally

Status: complete and installed for fresh native generation. CQ13 passes all 433 targeted checks. CQ18 full suite: 14,476 passed, 32 unchanged baseline failures, zero C# errors and no new failures. CQ15 reproduces all 380 asset/metadata files byte for byte; CQ19 confirms installation and GUID uniqueness. CoO-original composition and art; no Qud-parity or live-FPS claim.

## Scope and readiness

| Site | Exact surface | Authored profile | Native scope |
|---|---|---|---|
| Gantry | Overworld.7.8.0 | CrossroadsExchange | Crossroads exchange, mundane records, caravan rest |
| Tine | Overworld.13.7.0 | LakesideVillage | Lake edge, piers, working yards, scribe retreat |
| Quillhold | Overworld.14.9.0 | PrimaryArchive | Public copying rooms, dry archive aisles, refectory |
| Tally | Overworld.10.14.0 | CentralExchange | Exchange courts, storage/loading, shops and rental frontages |

All four are currently authored Spread, tier 1, Village places with roads and no river. Each gets three materially distinct semantic formations, current native entity owners, restrained four-variant voxel art and runtime integration. Keep current spawn, camera, reveal mode, native services, population, repair sites, containers, stock and protected arrival paths.

🟢 Existing graph, destructible owners, conversations, trade, copying, lodging and rental systems are reusable. 🟡 Maintained archive fixtures need new ordinary blueprints instead of quest-barrier reuse. ⚪ Deep gods/audiences, fishing simulation, hireable caravans, debt-clearing and Reader channeling remain outside this surface composition wave. These are not presented as shipped mechanics.

## Verification sweep and corrections

| Premise inspected | Verified correction | Source |
|---|---|---|
| All clerks/scribes are portable | FilerClerk delivers Marrowstye courier quest; RecensionScribe describes the Drowned Ledger expedition. Do not relocate them. | FriendlyNPCs.json; Objects.json |
| Marble/shelf assets are ordinary props | LibraryMemoryMarbleWall and SealedArchiveShelf are indestructible quest barriers; Bookshelf is toppled. Author ordinary destructible public archive owners. | Objects.json |
| All trade stock comes from TradeStockBuilder | That builder filters Villagers; Quillhold/Tally residents are retagged. Keep their explicit TraderPart stock and native rental setup. | VillagePopulationBuilder; TradeStockBuilder; TraderRestockSystem |
| Scribe copying consumes ink/payment | Actual CopyGrimoire retains original and does not charge either. Preserve actual behavior rather than inventing costs in dialogue. | ConversationActions.CopyGrimoire |
| Named places inherit the map river | Current generic village pipeline adds bottom water even with no map river. The four compositions omit this unrelated strip; Tine authors a local lake. | OverworldZoneManager.CreateVillagePipeline |
| Older lore tiers are current map tiers | Actual authored rows say tier 1 for all four. Preserve table authority. | WorldMapAuthoring |
| New conversation files need explicit registration | ConversationLoader loads all TextAssets under Content/Conversations. | ConversationLoader.LoadAll |

Read sources: Lore/History/02_Geography.md; Lore/History/08_MaterialCulture.md; Lore/Factions/02_Recension.md; Lore/Factions/04_SaccharineConcord.md; FirstTent/LastCounter composition implementations; AreaCompositionScope; OverworldZoneManager; SpawnRing3DRecipes; VoxelWorldPresentation; native content cited above. Detailed per-site verification belongs in each site document.

## Milestones and gates

1. Write plan and counterchecked native/art/integration tests. Capture actual RED in isolated Unity before production. Starting baseline is TC20-full-verified: 14,074 total, 14,042 passed, 32 recorded pre-existing failures, zero C# errors. CQ00-baseline proves all 2,033 source inputs identical; this reuses a verified run, not a claim of a new run.
2. Implement independent native plans/builders and new physical content. Base priority 1000; profile 3860; current-arrival reservations 3870. Whole-stage validation before mutation; never reconstruct an existing graph.
3. Wire exact authored profiles with current own-map authority, population/drama reservations and cave footprint filter. No global-map authority for restored zones.
4. Build and import offline voxel kits; wire all current native owner aliases. Four variants, at most two colors per model, one cell footprint, sparse chunky silhouettes. No colliders, gameplay components or synthetic replacement terrain.
5. Render at least three formations per site from the gameplay camera; inspect composition/readability/repetition. Fix rules, regenerate, inspect again. Store preview gallery and audit receipts.
6. Native flow/counterchecks, dedicated adversarial fixtures, independent cold-eye and player-flow review. Fix notable findings before completion. Run focused and full suite against frozen source, compare all baseline failures by exact name/message. Audit metas and deterministic rebuilds.
7. Update living docs and scoped commit. Preserve all pre-existing dirty paths; retain exact shared-file implementation delta in evidence because those mixed files cannot be broadly staged.

## Performance and observability

Plans allocate only during fresh generation. Rendering reuses immutable combined meshes, existing content fingerprints and per-cell dirty notifications. No new Update/LateUpdate, actor-position model rebuild, collider or per-cell asset import. Scope misses return cheaply. Builders emit worldgen planned/placed/rejected/arrival diagnostics with zone and reason; tests cover rejected as well as successful paths. Use native owner destruction and lifetime hooks rather than shadow geometry state.

## Honesty bounds

Headless tests can verify native ownership, reachability, actual service actions, current graph scope, removal, deterministic geometry and rendered captures. They cannot establish interactive feel or sustained live frame pacing. Original Unity session remains untouched; all validation and renders use the isolated project.

## In-phase review

Review complete. Resolved findings and bounded verification are recorded below and in each site document. No unresolved material finding remains from the independent source, player-flow and final camera passes.

## Implementation log (newest last)

- 2026-09-16: Surveyed remaining named places, selected four complementary surface districts. Saved original status, exact shared-file snapshots and 2,033-source baseline equivalence under civic-quartet cache. No production edits before RED.

## Files changed

Per-area composition plans/builders, voxel kit libraries/editor generators, native/adversarial/art tests, the reusable native preview harness and gallery are new. Shared map routing, service placement, scope, recipes and presentation are recorded in `CQ19-closeout/integration.json` and `implementation.patch`; installed resource hashes are in `installed-artifacts.json`. Nine blueprint definitions and one ordinary registrar conversation are included.

### Review findings and corrections

- 🟡 CQ04: refectory contained beds instead of communal dining surfaces. Added actual individual destructible table cells/four chairs and separate courier rest.
- 🟡 CQ06: scribe had no protected standing neighbor; now reserves one free interior approach without reserving the service actor cell.
- 🟡 CQ06: over-bright/repeated Tine/Tally art and bedroom-sized exchange entrances. Quieter two-color models, open Gantry exchange, Tally loading face/apron; unchanged native stock and geometry ownership.
- 🟡 CQ07: Tine reeds were unmodeled; exact native alias now uses existing voxel reeds. Six scattered archive cabinets now form two coherent runs with a crossing aisle and tighter room scale.
- 🟡 CQ09 player-flow hypotheses: travelling Scribe and TentRightHost changed bodies at region boundaries. Civic recipes now share their exact body families and instance-stable variants; native membership and reskin rejection still apply. Registrar travel was already correct.
- ⚪ Old plain-village controls selected newly converted Gantry/Tine. After actual RED, controls explicitly set ordinary Village POIs with no profile at the same addresses; retained every negative assertion. The existing Morrowfast table failure remains baseline debt.
- Independent taxonomy/player-flow review inspected service claims before decor, current-map authority, atomic publication, current dry cave neighborhoods, actual copying/rental, no foreign quest imports and scope-bound rendering. Reviewed final CQ12 gameplay-camera captures of all twelve actual formations. No unresolved notable finding remains from those bounded passes; final full regression passed with unchanged baseline debt.


## Final integration and verification

The pending gates in earlier chronological entries are closed. CQ14 exposed one additional literal equipment-list failure: GantryRegistrar was missing from the expected kit table. The retained equipment ownership/loot exclusions now include its intentional gloves and boots; CQ17 verifies the complete equipment fixtures before the final full run. CQ13 passes all 433 phase checks, including 219 cases in dedicated adversarial fixtures. CQ18 adds 434 passing cases to the verified TC20 baseline: all 32 existing failures have identical test names and failure messages. One former Tine negative control was renamed and both Tine/Gantry plain-village controls now explicitly select an ordinary profile; their negative assertions remain intact.

[Final twelve-view gallery](Verification/VoxelWorld/CQ12-final-preview/index.html) covers all three actual formations in each of the four towns. All requested seeds are nonzero and match each native manager's effective seed. Every capture has zero missing meshes and zero unmodeled visible owners. CQ05/CQ08 seed-zero formation labels are superseded evidence. Independent source, player-flow and final visual reviews are complete.

The phase adds 92 combined-mesh models, with four variants per family and no more than two palette colors per model. CQ15 rebuild reproduces all 380 asset and metadata files byte for byte. CQ19 checks the installed resources, GUID uniqueness and exact synchronization of 2,099 source/content/assembly/meta inputs between the working project and the isolated validation project. [Close-out evidence](Verification/VoxelWorld/CQ19-closeout/README.md) records the case comparison, asset hashes and exact shared-source delta.

Fresh native generation receives the new rules at Gantry (7,8), Tine (13,7), Quillhold (14,9) and Tally (10,14), surface depth 0. Existing graph instances remain intact. Native trade, containers, copying, lodging, rental frontages, water contact, destruction and entity lifetimes remain authoritative. The current spawn, 1.2x camera and full reveal are unchanged. The original Unity editor was not restarted. Static captures establish scene composition and coverage; they do not measure live input feel, animation or sustained frame rate.

New files and originally clean shared files are committed directly. Exact changes in already-mixed shared files are installed in the working tree and preserved in the close-out implementation patch, without committing unrelated work. This is a scoped checkpoint of the existing mixed workspace, not a standalone clean-checkout claim.

### Final corrections and ship log

- 🟡 CQ11 preview failure: seed zero invoked the game's nondeterministic sentinel while the image label used literal zero. The preview now chooses nonzero representative seeds, asserts the effective native seed, and records it. CQ13 passes the four preview regression cases; CQ12 replaces the invalid historical views.
- 🟡 CQ09 travelling actor discontinuity: Scribes and Tent-Right hosts now retain the same model family and instance-stable variant across all four towns. Their native identity, conversation and inventory are unchanged.
- ⚪ CQ14: a literal NPC equipment allowlist predated the new Gantry registrar. Added its intended LeatherGloves/LeatherBoots row, retaining all exact-kit, real-ownership and humanoid-loot exclusions; this automatically adds one real-equipment creation test. CQ17 and CQ18 close that compatibility gate.
- 🟢 2026-09-16: finished four native areas, twelve formations, nine new blueprint definitions and 92 voxel models. Completed focused/full regression, deterministic art rebuild, independent review, source/GUID audit, living docs and scoped checkpoint.
- 🧪 Deferred evidence only: live movement feel, long-session frame pacing and exhaustive all-seed aesthetics require interactive observation. No substitute claim is made from static captures.
