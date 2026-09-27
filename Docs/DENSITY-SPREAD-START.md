# Whole-Spread presentation and ordinary start: C15 integration

Status: connectivity passed its native gate; the reviewed starter policy, bootstrap integration and ordinary N/Continue native harness are now published for native GREEN and Play verification. Rendering/profile/portable coverage belongs to the coordinated C15 milestones in DENSITY-COMPLETION-PLAN.md. This is an ordinary generated biome, not repeated ReferenceGlade stamps.

## Verified source and scope

The current authored map has142 actual Spread surface addresses, all currently tier1. Historical prose about nearby higher-tier sites does not establish a higher actual tier in the current table. The seed varies generated contents and opportunistic POIs. ReferenceGlade is one protected Spread/no-POI address at11,10, requiring its actual blueprint content. The ordinary game must still validate the live map before preferring that address, fall back deterministically to another safe actual Spread chunk when needed, and retain explicit scene/audit start addresses.

SampleScene currently saves FreshGameZoneID=Overworld.2.6.0; GameBootstrap's field initializer is Morrowfast. Neither has been changed by the connectivity slice. The proposed ordinary-policy sentinel is an empty setting. Continue retains its saved graph/location/seed through ApplyLoadedGame. Normal seed0 resolves through the existing clock-based world seed; no fixed normal-play seed is proposed. Normal stats, loadout, farming/spells and disabled BitLocker remain unchanged.

## Connectivity correction and verification sweep

| Premise | Verified behavior / correction |
|---|---|
| All physical solids are cleared by connectivity | FloodFill uses BlocksMovement, while clearing only inspected anchor Wall/Solid tags. Real Hedge is Physics.Solid without the Solid tag. |
| Carving eventually reaches the disconnected region | The old unbounded loop could repeat without progress. Actual seed64 Spread8.2 stopped the complete census after12 surface rows. Its exact pre-connectivity owners are preserved in Verification/DensityCompletion/SpreadBiome/connectivity-source.json. |
| An anchor list is complete physical occupancy | Secondary SpatialFootprint cells require Occupants and whole-owner Zone.RemoveEntity. |
| Every noncreature physical solid is disposable terrain | False. First candidate removed the guaranteed GroveSign at nearby seed8. Old nearby87/87 passed; first candidate86/87. The final guard preserves Creature/Furniture/Item owners, containers, stairs and ordinary/authored door authorities. |
| A failed builder should force its way through | ZoneGenerationPipeline already has a bounded retry contract. Connectivity now returns false and emits ConnectivityRejected for no passable cell, no progress or a blocked requested edge. |
| Failed generation can silently supply an empty starter zone | Current ZoneManager.GenerateZone returns null unless both generation and commit succeed; successful caching occurs afterward. Starter controls must retain this boundary. |

The final implementation removes only unprotected tag/physical blockers from an occupied-owner snapshot, never duplicates an existing floor, requires strictly increasing reachable-cell count after each corridor, and verifies each of the four actual selected edge mouths after carving. Existing successful-path RNG draws and order are retained. It reports ConnectivityConnected on success. This is a bounded correction to CoO's existing ForceConnections-inspired builder, not a new claim of exact Qud parity.

The first11 cases were6RED/5controls; three edge/diagnostic cases raised that to9RED/5controls, then14GREEN. The semantic-owner follow-up added9 intended RED, then23/23GREEN. Nearby ZoneGeneration, UndergroundGeneration, SpreadFormation, GrovelandsFormation and BloomFront tests compare87/87 before with87/87 final. Combat and root independently reviewed the final owner/progress boundaries. Actual Unity23/23 connectivity and28/28 GrovelandsFormation tests passed after a typed JsonUtility fixture repair; the initial private compiler had broader references than the real native tests asmdef. See Integration/native-spread-start-animal-coverage-red and SpreadBiome/native-fixture-parser-repair.json.

## Complete generated content census

With the final private connectivity candidate, the standalone census generated426/426 surface rows (every142 current Spread address across seeds64,1,1729) and four associated Spread-lair rows, with no generation failures. Surface rows contain386 Wilderness,27 Village,6 MerchantCamp,3 Sinkhole and4 Lair entries. Observed surface identities comprise113 ground blueprints (133 render variants),87 gear blueprints and43 container-stock blueprints; these are unions, not independent acquisition totals.

The preserved census observes actual ground owners, render identity, equipment/carriage, stock and generated pipelines. The failed pre-repair12-row attempt and first candidate receipts remain historical evidence. The runner uses a stable string-hash adapter, so identical seeds do not establish identical Unity maps. It does not prove native rendering, natural travel/acquisition, all rare possible rolls or player-build performance. Source-defined actor/portable closure and native representative transitions supplement, rather than replace, this census. The four deep rows are separately labelled and never compared as a surface-only economy claim.

## Ordinary starter integration

FreshGamePlacement tests began with24 missing-API RED cases. Candidate selection added12 missing-API RED cases. Peer review found that one adjacent step still admitted a sealed two-cell pocket; two additional RED cases led to a finite reverse flood from safe full-footprint edge anchors. Current48/48 standalone tests include five actual generated defaults and a failed first generation falling through to a successful second Spread candidate without caching the failure. Two further RED probes now refuse a wrong-zone cached alias and retain ordinary Spread generation as the final fallback when reference-specific content is unavailable and no other valid Spread address exists. The helper preserves owners, hazards and placed/foreign/dead/carried/equipped actors; initial threat radius3 is a placement gate, not combat immunity. An explicitly configured enclosed scenario can now refuse rather than force overlap; native authored-route controls remain required.

DensitySpreadBootstrapNativeTests adds10 actual bootstrap/default/placement/N/Continue cases. Actual native10 executed8RED/2controls against the old bootstrap before implementation. The reviewed helpers, GameBootstrap integration and saved SampleScene empty default sentinel are now published; explicit authored/audit addresses remain explicit. The forest-to-Morrowfast route test is named as an explicit route, while the dedicated bootstrap fixture owns the saved empty-scene sentinel assertion. Actual native280/280 integrated cases passed, including core48, bootstrap10, explicit Morrowfast/forest routes and neighboring behavior; receipt Integration/native-spread-start-animal-harvest-green. The ordinary native Play gate remains pending. The published native Play audit uses one isolated save root, normal requested seed0, real N, ordinary kit/stats, a native movement/F5 checkpoint, then a labelled real scene reload and native C into that exact saved graph. It will compare replacement graph, location, seed, HP/gear, turn state and unchanged saved bytes. This proves scene restart/Continue, not a full process quit/relaunch.

## Self-review and honesty bounds

- Q1 symmetry: physical reachability and carving now both inspect occupied owners; semantic owners are protected even when malformed as Solid. Failure follows the existing pipeline retry path.
- Q2 cross-feature consistency: whole-owner removal preserves secondary occupancy, while actor, furniture, loot, item, stair and door authority remains in its owning systems.
- Q3 counter-checks: tag/physics, harmless/solid footprint, protected creature/furniture/item/door/container/stair, complete/no-progress/blocked-edge and authored real-source controls are explicit. The GroveSign differential is retained as a caught regression, not weakened away.
- Q4 documentation: full426-row census is standalone generated stock/owner evidence only. Native23 is GREEN; actual new default/Continue and whole-biome 3D acceptance remain separate pending gates. Existing native door110 mechanics/28render tests do not make an unsupported Sodden village a rendered source.

Performance: all new allocations/floods occur during zone generation or fresh-game placement, outside frame/turn rendering paths. Reachable count increases at most2000 times; each carve and edge pass is bounded. No hot-path cache or per-frame work was added.

Files in this slice: ConnectivityBuilder.cs and ConnectivityProgressTests; FreshGamePlacement/FreshGameStart and their48 tests; GameBootstrap; the SampleScene default sentinel; DensitySpreadBootstrapNativeTests; explicit Morrowfast/accessibility fixture corrections; DensitySpreadStartNativePlayer/Batch; this living document and receipts. New C# files have fresh metadata. No blueprint, loot, save-format, fixed seed, stat or global thermal changes. Exact precondition and publication hashes are in SpreadBiome/starter-publication.json.

## Native full-map generation evidence

Root also generated every142 actual Spread surface address and one committed associated lair under Unity’s native seed64 hash:313,192 owner rows, with no generation, presenter or mutation failures reported. The native report is SpreadBiome/NativeCoverage/64-all-current-Spread-1bca750768dd4015a8f997b02f5f00f1.json.gz; native64-gap-summary.json records1,776 named presentation gaps for the remaining model work. This is a separate native world/corpus from the standalone426 rows and does not turn those gaps into completed visual coverage. Native profile8 subsequently passed; actual whole-biome visual acceptance remains pending.

## Starter publication review

Root and combat independently reviewed safe full-footprint placement, bounded edge connectivity, current-map candidate authority and unchanged loaded-game path. Full private runtime and current native test source compilation passed before publication. The two wording-only fixture changes at publication separate the empty ordinary setting from an explicit forest route; native execution will validate the integrated assembly. The native launcher preserves one NativeSaveIsolation lifetime across a real scene reload; that is deliberately not an application quit/restart. The copied 48-case result and full exact19-file publication receipt preserve what was actually verified.

## Actual ordinary N / Continue acceptance

Native run9a769074b2664292a1487893236fe0b1 passed12/12 with zero unexpected errors and exact launcher restoration. The two real bootstraps used ordinary clock-derived seeds37186489 and37189185; C restored the first saved graph after the second scene bootstrap. The report, full actual log interval and four images are preserved under SpreadBiome/NativeStart. The exact04-native-continue-restored.png was visually inspected at full resolution: the world/player, GRIMOIRES line and hotbar are present. An initial suspected blank-HUD reading was a display-inspection false alarm; no UI change was made. This remains scene-restart evidence and a single safe native step, not an application relaunch or whole-biome visual acceptance.
