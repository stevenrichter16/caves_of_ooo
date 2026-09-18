# Morrowfast style and reusable regional situations

Status: coarse-art and first regional wave implemented and verified, 2026-09-17. Publication preserves the open Unity session. User authorized implementation, autonomous review/fix cycles, and decisions. This is CoO-original content/composition using existing mechanics; no new Qud-parity claim.

## Goal and ordering

First give Morrowfast the broad silhouettes and restrained surfaces of the surrounding voxel regions. Then ship the first bounded reusable regional-situation slice: optional native supply/recovery work with protected-resource alternatives, genuine hazards, useful persistent deliveries and observable local ecological consequences. Keep quiet transit chunks and all authored towns, quest homes and the starting expedition intact.

## Starting workspace

Branch `codex/voxel-town-generator`, starting HEAD `00b5d655`, extensively dirty before this request. Preserve previous work; never reset or stage it wholesale. Exact source/art copies and hashes: `Docs/Verification/RegionalSituations/prechange.json`, backup `/tmp/coo-regional-situations-start-20260917`. RS01 records the fresh full baseline. The previous turn ended at 14,728 passing with 32 known failures, not a clean all-green suite.

## Content readiness and verification corrections

| Status | Checked source / premise | Actual contract and decision |
|---|---|---|
| green | Village3DPresenter, VoxelWorldPresentation, VoxelWorldMeshBuilder, ObjectPalette, native-bindings.json | Morrowfast already has voxel substitution and a two-color object budget. Detailed source surfaces survive the voxel bake. Replace semantic shapes rather than reduce color count again or globally increase voxel pitch. |
| green | Village3DManifest, MorrowfastSceneDefinition, Village3DProjection | Five shells, roofs and doors retain distinct owners. Generate architecture against native cells/entrances, transform to existing prefab coordinates. Old AABB fitting alone cannot preserve door gaps. |
| green | Village3DPresenter.AddView | Selection boxes derive from rendered bounds. Art review must check native reach/collision and selection bounds, not only intact screenshots. |
| green | ZoneManager.GetZone / OnZoneGenerated / OnZoneAttached | Generation runs only for fresh chunks. Attachment also runs after cache/load and must never refill jobs, cargo or rewards. |
| green | StoryletPart and QuestCueStateQuery | Global quest IDs are not reusable instance IDs. Do not clone global quest templates under fabricated IDs without a definition resolver. Use explicit template/instance separation for the initial slice. |
| green | GroveLaw and HarvestablePart | Native mineral harvesting costs -15 RotChoir standing on Choir ground; player ignition costs -40 per ignited object. Crops/berries are not digging; no existing permission exemption. Alternative supply must use outside/bought material, not invented permission. |
| green | BurnOffGasPart, BloomingFruitingBody, PeatBog, MirePool | Heat/Fire damage releases actual spores/marsh gas. Broad EcologyDamaged/UrquActive flags lack a normal producer/cached-zone response; do not advertise those as a working regional cycle. |
| green | SaveSystem entity Properties/IntProperties and reflected Part fields | Save stable strings/numbers and native IDs; no static-only completion state. No save migrations requested. |
| green | WorldMap BiomeRows / Places / sinkhole reservations | Initial 2.5 source is Stump, not Grovelands; corrected Orrit's source to ordinary Grovelands 1.5 before implementation. |
| green | Regional-situation population and art | Reuse real blueprint bodies, existing item/prop models and native actions. Preflight required parts and complete placement before publication. Final template/placement contract is recorded before implementation below. |
| green | MorrowfastExpedition, NativeQuestCueViews, RegionalTravelNotes | Preserve completed supper expedition, real merchant stocks, available/active quest cues and saved travel notes. Keep camera angle, 1.2x view and full reveal. |

## Milestones

1. **Morrowfast coarse scenery:** RED tests; explicit offline semantic mesh builder; limited-color broad walls/terraces, quiet ground/routes, chunky functional furnishings. Original FBXs/rigs and simulation graph stay authoritative. Native gameplay-camera inspection with roofs closed/open and existing journey regression.
2. **Reusable situations:** plan concrete templates and outcomes from verified native systems; RED tests; fresh-zone deterministic placement with bounded density and access validation; persistent instance records, native C actions and readable journal directions. Stage dependencies before mutations, reject forged/remote/dead/stale actors and duplicate rewards.
3. **Review and acceptance:** meaningful counterchecks, dedicated adversarial fixture, independent cold-eye Q1-Q4 review; fix material findings. Full suite compared to exact baseline names, native player-flow proof, screenshot review, 60-second real-session profiler capture, metadata audit and scoped change report.

## Performance

Read `Docs/PERF-FOUNDATION.md`: geometry is baked offline and shared, no runtime per-cube objects. Generation can allocate bounded cold-path work lists; no new per-frame world scans. Situation interactions use current-zone identity and saved records, with cell-dirty hooks for visual changes. Journal strings are rebuilt on opening/changing tabs, not each frame. Preserve the existing profiled presenter path. Native ProfilerRecorder evidence reports maximum as well as average; editor capture/save overhead is not standalone game performance.

## First situation wave: concrete contract

Two shared template families, five authored bindings (not a random quest in every chunk):

| Binding | Native recipient | Source | Decision and result |
|---|---|---|---|
| morrowfast-iron | Orrit Coilstitch, Morrowfast 3.6 | Grovelands 1.5 | One ChoirIron: bring/trade a unit, or harvest the native vein and accept existing Choir law. Actual merchant stock, eight drams and fire clay. |
| cinderhold-iron | Existing Weaponsmith, Cinderhold 6.6 | Grovelands 5.6 | The same supply template in a separate instance; neither completion pays or closes the other. |
| gantry-grain | Existing Merchant, Gantry 7.8 | Spread 7.7 | Two Emberwheat from finite ripe rows or an outside source become real food stock. Six drams and a healing tonic. |
| sumphold-oil | Existing Merchant, Sumphold 15.6 | Sodden 16.6 | Carry an identified sealed lamp-oil consignment from dry ground beside native peat/mire. Deliver two WardOil into actual stock; twelve drams and silver sand. Bound untouched habitat earns a modest bonus; heat releases native marsh gas. |
| wellmeet-filters | Existing Merchant, Wellmeet 8.16 | Beating 8.17 | Carry an identified filter-sand consignment; existing Height/headgear/shade rules govern exposure. Deliver two SilverSand into stock; twelve drams and fire clay. |

Native C actions read/record the request, deliver or release it. Requests have instance keys containing world seed plus binding ID, separate from global storylets. Readable Q/Tab notes persist independently of recipient/cargo destruction. Completion is latched in both recipient state and player state before reward observers. Notes do not overwrite destination directions.

Source placement runs only on fresh generation, vetoes runtime POIs/changed biomes, never clears existing scenery and checks final reachable ground. Reading a request may generate its explicitly selected source once to verify an actual site; rendering/cue queries never generate anything. An unavailable/destroyed cargo source is reported truthfully and is never rebuilt on load/acceptance. Supplies can still be brought from elsewhere. Recipient identities and existing services remain intact.

The wetland outcome is local: damage to bound habitat is latched, missing habitat cannot count as preserved, and real BurnOffGas determines gas behavior. This does not claim autonomous ecology, repopulation or an Urqu state cycle. Desert visual shadows are not promised as shade; actual native interior cells and head equipment are the existing protection.

Delivery uses the existing inventory transaction mechanism with exact ownership, positive quantities, stack payload preservation, recipient-capacity rollback, independent completion latches, bounded rewards and dropped reward overflow. Save/load correctness is required; legacy migrations are not.

## Review and honesty bounds

Automated tests can establish geometry budgets, palette/owner coverage, real state transitions, inventory/currency changes and save/revisit behavior. Gameplay-camera screenshots are required for visual fit/readability. Test success alone does not establish balance or long-session enjoyment. Deferred broad ecology cycles, simulation outside active chunks and additional template families must be named rather than silently implied.

## Implementation log (newest at bottom)

- Preserved 10,003 source/art/document files from the existing workspace. Confirmed ordinary Unity editor idle with clean scene before closing for RS01. Three independent source audits identified art-source detail, quest-instance lookup and ecological-state limitations before implementation.

- RS01 fresh full baseline: 14,760 total, 14,728 passed, exactly the same 32 failures as CG18, zero C# errors. RS02 actual art RED: 29 failing assertions for the absent coarse generator, zero compiler errors. Art implementation now authorized by that RED evidence. Native acceptance extended to actual open/walk/close cycles and captures of all five interiors.

- RS03 review RED: 28 passed, five failed, zero compiler errors. Four real shape findings pinned: roofs floated above wall tops; fence, planter and bed families repeated geometry. Fifth failure was the expected not-yet-installed overlay. Fixes precede the first offline bake. Independent review also requires baked/imported child-transform parity and preserves overlay on public mesh-bake entry points. Publication prevalidates outputs but is not an atomic filesystem transaction; a failed bake must be retried and verified.
- Transaction verification found `Do` registers undo after invoking mutation and `SplitStack` decrements before cloning. Delivery must register rollback before either operation, claim the request before callbacks, and separately restore saved latch/note properties on failure.

- RS04 first coarse bake passed: 111 models / 117 mesh bindings, 289 unrelated catalog bindings preserved, zero compiler errors and zero unexpected art changes. Existing source assets are byte-identical by runner hash verification.
- RS05 installed review: 66 passed / 10 failed, zero compiler errors. Twenty dedicated adversarial cases and imported-child frame equivalence passed. Two new failures establish missing dry-bank path approaches: bridge support was absent from routing. Eight legacy assertions require previous exact geometry/path selections and are being narrowly replaced for verified coarse Village bindings; whole-object two-color and rig controls remain.
- Native UI verification correction: `WorldInteractionSystem.GatherActions(target,actor)` provides no Zone parameter. Request action gathering must recover authoritative context rather than depend on a test-only Zone. `PerformInventoryActionCommand` and nested commands normally own separate transactions; the request must participate in its native outer transaction so post-action failure rolls back cargo, completion and deferred currency together.

- RS06/RS07b: repaired dry bridge approaches; 295/295 focused art and Morrowfast tests passed, zero compiler errors. User requested Unity remain available, so further implementation moved to an independent APFS copy at `/tmp/coo-regional-verification-20260917`, with separate company/product preferences and private saves. Live source/art hashes at freeze are `/tmp/coo-regional-live-at-freeze.json`; publish only owned, conflict-checked changes.
- RS08b exposed submesh bounds lost by in-place copying (one real RED, one fresh-mesh control GREEN). Scoped coarse-builder descriptor preservation fixed it. RS09 bake and RS10 78/78 art tests passed. RS12 repeated bake changed zero art bytes and preserved all GUIDs. Unique scenery vertices: 272,856 before → 8,832 now (not placed frame totals or an FPS claim).
- RS11 regional initial RED recorded missing `RegionalRequestPart` compiler errors before production. Two fixtures (15 situation cases and eight presentation/action/journal cases) are temporarily staged outside Assets during the frozen native art run; implementation proceeds outside Assets in parallel and will be imported after native verification.

- RS13/RS14 clone native art journeys: all 38 gameplay checks and 13 captures completed, but each attempt recorded one UnityEditor.Search startup exception. Both runs remain failed acceptance; no exception waiver. The CLI runner now waits for editor startup to settle before Play; the extended regional journey will verify this. Ordinary user editor stayed open.
- RS15/RS16 compile gates caught missing Data namespace imports in the new production/test files; no stale XML was used. RS17 found a fixture field retaining its previous zone between NUnit cases; SetUp now explicitly resets it.
- RS18: 58 cases, 51 passed, seven failures, zero C# errors. Actual adversarial REDs exposed equipped/foreign-owned supplies accepted as delivery, destroyed recovery cargo retaining an available cue, and protected habitat damage not latched. Two full-world reload cases exposed a fixture assumption that manager serialization includes the separately saved player; native save contract is being followed before attributing this to production. Six separate player-facing usability cases now precede any feedback changes.

- RS19: 64 cases, 59 passed/five usability REDs, zero C# errors. All original 58 cases now pass. Corrected structural test dispatch (`RouteDamage`, not creature-only `ApplyDamage`) and complete `GameSessionState` serialization; neither required production damage/save changes. Fixed genuine invalid-payment and destroyed-cargo availability defects.
- Usability fixes: native item/person/town names, exact requested quantities and sealed-cargo directions, truthful exhausted supply notes with outside goods still accepted. The RED post-action rollback exposed premature success reporting: request messages/diagnostics now run only after the shared inventory transaction commits. Separate commit/rollback/overflow/observer/reentrancy counterchecks added. RS20 was compile-invalid because these tests called internal transaction methods directly; tests now inspect that internal seam through reflection without widening production access.
- Independent art Q1–Q4 review: all thirteen prior native frames examined; no additional material visual finding. Audit of 6,754 Assets/Packages metas found zero collisions or invalid scoped GUIDs, and each of 117 coarse meshes has exactly one catalog reference. Prior native runs remain failed overall due to startup Search exceptions.

- RS21: 91 regional/receipt/guidance cases, 90 passed/one genuine lifecycle RED, zero compiler errors. All five usability gaps and eight receipt counterchecks pass. The remaining failure occurred after saving a real consignment in a third cached chunk: an early town query cached null and stayed stale after native pickup. Fixed restored lookup through persisted cargo tags across already-cached zone indexes and one cold inventory pass; retain the exact owner reference for O(1) subsequent cues. No source generation or replenishment is used to repair a missing parcel. RS22 full regression is now running.

- RS22 full regression: 14,892 cases, 14,859 passed/33 failed, zero compiler errors. All regional tests, including restored dropped-away cargo, pass in the whole suite. Failures are the exact 32 pre-existing baseline names plus one old ambient-art geometry identity assertion for an intentionally replaced Morrowfast grass mesh. Narrow coarse-owned exemption is being reviewed; all other ambient-channel expectations stay.
- Final independent Q1–Q4 review confirmed transaction/receipt symmetry and found one additional real player flow to test: cargo stored in a container on reload can also produce a cached absence before native retrieval. Separate two-case container fixture now exercises native Pickup/Put/Take, full session reload and missing-owner control. No speculative cargo fix before its RED run.

- RS23 confirmed the container scenario: 93 regional/guidance/receipt cases, 92 passed/one missing-cue RED, zero compiler errors. New `RegionalCargoPart` observes native `Taken` from pickup/container retrieval and validates the existing owner/world before updating only the derived reference. A failed-outer-take then retry test proves rollback still hides the nested owner and later successful retrieval restores the cue.
- RS24 final full regression: 14,895 total, 14,863 passed, exactly the same 32 baseline failure names, zero compiler errors. Net +135 tests; no added failure. The ambient paint assertion now preserves every geometry channel for the 12 unchanged ring bindings and separately verifies the 53 explicitly owned coarse Village bindings with their palette/water contract. Detailed baseline comparison is `Docs/Verification/RegionalSituations/full-regression-comparison.json`. Native RS25 is underway on these frozen sources.

- RS25 native RED: unchanged frozen sources, zero compiler errors/no unhandled exceptions. The actual C world menu bypasses `PerformInventoryActionCommand` and fires `InventoryAction` directly; the transaction-required request correctly refused. Previous integration tests covered gathering plus the command but not this dispatch seam. Scope the fix to `RegionalRequest:*` in `InputHandler.ExecuteWorldActionSelection`, retain ordinary world actions and costs, and add actual InputHandler dispatch tests.
- RS25 additionally exposed a verification-route assumption: 1.6 is the deliberately excluded Woven Doll special chunk, not a voxel wilderness presenter. The journey now goes via 2.5 (the Stump) to the unchanged valid source 1.5, with the same six regional border crossings. This is a harness route correction, not a new conversion or removal of the voxel gate. CLI startup settling removed the prior Search enumeration exception; native success is still pending the menu fix.

- RS26 actual world-menu coverage: 100/100 regional cases passed, zero C# errors. Six tests exercise `InputHandler.ExecuteWorldActionSelection`, including native before/after events, rollback then retry, stale reach, unchanged Examine and menu return state. The new branch routes only `RegionalRequest:*` through the existing command transaction; generic actions and zero-turn policy stay intact.
- RS27 final native acceptance passed: 56/56 checks, 17 actual 1080p GameView captures, 877 queued native steps across nine borders. Request → native protected harvest (-15 Choir standing) → delivery (stock +1, iron -1, +8 drams, fire clay) → no repeat payment → Q/Tab notes → F5/F6 restoration, including the absent mined owner. All five coarse interiors were entered through actual doors. Zero C# errors, zero unhandled log exceptions, zero unexpected errors during the gameplay audit. Sources were frozen; private saves/preferences/scene cleanup verified. `RS27-final-native/receipt.json` and run `102e6487317d4ca0a04bef6a3db886ca` hold the raw evidence.
- Native bounds: F12 invincibility is explicitly enabled only for the long regional journey and disabled before save. This verifies controls and outcomes, not combat balance. Pre-Play log still contains the inherited TagManager parse warning/error and MCP initial-connect messages; these are not represented as a clean whole-editor log. Prior startup Search exceptions did not recur. Woven Doll 1.6 remains outside this conversion and was not used to weaken the voxel gate.
- Final input-route Q1–Q4 independent review found no additional material defect. Final metadata audit scanned 6,760 metas: zero malformed GUIDs/collisions; 117 coarse mesh ownership tags and unique catalog references valid. The separate adversarial and player-flow hypotheses earlier in this log exposed and fixed ownership, receipt, cargo-cache and native-menu bugs; this zero-finding narrow review is not a substitute for those gates.

## Final profiler evidence

RS27 records 60.002 seconds of actual native Editor gameplay (17,272 frames). This includes route planning, scene/zone loads, screenshots and existing systems; it is neither a standalone FPS benchmark nor isolated regional-cue cost.

| Recorder | Average | Maximum |
|---|---:|---:|
| Main Thread | 3.469 ms | 440.502 ms |
| COO.Input.Update | 0.191 ms | 417.469 ms |
| COO.ZoneRenderer.LateUpdate | 1.225 ms | 26.833 ms |
| GC Allocated In Frame | 47,749 bytes | 43,611,140 bytes |

These aggregate spikes remain a release-performance investigation target. No new per-frame full-world scan was introduced; derived cargo/recipient lookups are cached and state-checked, while cold restoration and explicit generation do bounded setup work. This evidence does not attribute existing input/load spikes to a particular subsystem.

## Player entry and limits

Start a **fresh world** to obtain the new authored requests reliably. Already generated or saved towns are not retrofitted. Morrowfast is at (3,6); approach Orrit Coilstitch and use **C** to read, deliver or release the iron request. **Q → Tab** opens Field Notes. The other four bindings and real source coordinates are listed above. The original Farra expedition and Vennit directions remain available.

Implemented scope is two finite template families in five authored locations, using existing art and mechanics. This adds useful merchant stock and rewards, protected-resource alternatives, hazardous cargo retrieval and an optional local habitat-preservation payment. It does not add an autonomous economy, global ecology recovery, new biome conversions or an ending campaign.

The full release proposal and its evidence are separate in `Docs/RELEASE-VISION.md` and `Docs/RELEASE-VISION-EVIDENCE.md`. They preserve CoO's persistent RPG identity and protected lore mysteries, and are proposals rather than extra systems silently claimed as implemented.

## Changed files and review scope

- `Assets/Editor/Art/MorrowfastCoarseVoxelBuilder.cs`: semantic coarse architecture, furnishings, terrain routes and stable in-place mesh/submesh publication. `VoxelWorldToolkitImporter.cs` and `VoxelWorldMeshBuilder.cs` retain the overlay on bake entry points.
- `Assets/Art3D/VoxelWorld/Morrowfast/` and the native voxel catalog: 111 models / 117 native bindings; original rigs, FBXs and unrelated catalog entries preserved. New .meta GUIDs checked against the complete project.
- `Assets/Scripts/Gameplay/World/RegionalSituations.cs`, `RegionalRequestPart.cs`, `RegionalSituationNotes.cs`, `RegionalHabitatPart.cs`, `RegionalCargoPart.cs`: definitions, authority, finite source placement, stateful native actions, notes, habitat and cargo lifecycle.
- `OverworldZoneManager.cs`, `PerformInventoryActionCommand.cs`, `InventoryTransaction.cs`: fresh-generation hook, shared action transaction and commit-only informational observers.
- `QuestCueStateQuery.cs`, `QuestLogUI.cs`, `InputHandler.cs`: native availability/active cues, saved Field Notes and transactional regional C actions.
- Dedicated regional, receipt, lifecycle, input, coarse-art, adversarial and submesh fixtures, plus narrowly scoped existing voxel art contracts. Fixture corrections use the real structural damage and full-session save APIs.
- `ChunkGameplayNativeAudit.cs`, `ChunkGameplayNativeAuditBatch.cs`, `Tools/ChunkGameplay/run_native.py`: ordinary-input regional acceptance, 17 frames, complete exception accounting and settled CLI startup.
- This living document, raw receipts, final visual/metadata reviews and the two release-vision documents. Clone-only runner adaptations and private ProjectSettings are excluded from publication.

No broad git staging or reset is permitted in this pre-existing dirty workspace. A conflict-checked publication manifest records the exact before/after hashes and backup paths. Subsequent user-authorized main integration preserved the accumulated playable state in `26fbe544` and fast-forwarded local `main`; see [MAIN-INTEGRATION-2026-09-17](MAIN-INTEGRATION-2026-09-17.md). Unrelated logs/cache/media remain outside that commit, so this is not a claim of a completely clean working directory.

## Final acceptance — 2026-09-17

RS28 fresh full regression: **14,901 cases, 14,869 passed, exactly the same 32 baseline failures, zero C# errors**. This is +141 cases over RS01, with no added failure. Exact failure-name comparison is in `Verification/RegionalSituations/full-regression-comparison.json`; the overall suite is not all green. The 32 inherited failures remain a release-quality blocker until separately triaged.

RS27 native acceptance and independent inspection of all 17 original-resolution captures passed. The documented visual bounds include one seed, all five Morrowfast interiors and Orrit's regional journey; the other bindings have integration/adversarial coverage rather than separate native playthroughs. `Verification/RegionalSituations/native-visual-review.md` distinguishes those evidence types.

Publication is restricted to owned files after comparing live hashes to the freeze baseline. The main editor is neither closed nor forced out of Play. Import/reload is held during copying and, if necessary, released automatically after the current Play session ends. Clone Company/Product settings and runner adaptations are excluded. `Verification/RegionalSituations/publication.json` records all copied hashes and backups. A fresh generated world is required for guaranteed presence of all new requests; no migration is attempted.
