# Sodden native 3D sprite direction

Status: 85-model implementation and native regional verification complete; affected regression and ordinary Play checks complete; standalone performance not certified, 2026-10-09. User requests a nonstarting biome with original Qud-like sprite readability and real 3D depth, implemented and visually iterated in game. Chosen region: the Sodden and its Sumphold preparation circuit. This is an art/presentation milestone; the requested encounter/clue/finite-supply gameplay work follows it.

## Prompts and design

**Brainstorm prompt used:** Inspect the reference image and actual generated Sodden scenes. Find the largest visible differences in silhouette, palette, depth and readable function. Design original modular objects whose forms identify land, water, barriers, safe routes, inhabitants, harvests and usable equipment. Preserve the unsettling quiet bog. Do not copy Qud assets, enemies, lore or text, and do not substitute concept art for live models.

**Design prompt used:** Audit rendering authority, native recipe validation, materials, fog, batching, rigs, equipment and current biome generation. Choose an additive art library with a reproducible source. Specify scope, model families, visual hierarchy, actor/state preservation, performance budgets, native coverage and tests before production edits. Keep existing proven assets where they already fit; make the core regional change unmistakable.

**Implementation prompt:** Work in small verified steps: native failing asset/scope tests, new kit and private palette, renderer integration, generated-zone coverage, native Play interaction evidence, visual inspection and polish, adversarial/cold-eye review. A valid model ID alone is not proof that its mesh is drawn. Verify exact submitted mesh/material and native owner where possible. Fix meaningful failures before commit. Record limitations honestly and preserve unrelated changes.

### Visual target

- Ground: subdued dark tea/teal, limited local mottling, small low-contrast mineral/plant detail; broad traversable space stays readable.
- Water: deeper near-black green with restrained lighter surface detail; dry boards and banks clearly separate from a wet shortcut.
- Plants: chunky jade/moss blades, ivory straw tips and visible root clusters; irregular groups and negative space, not thin identical pin-lines.
- Snags/peat: pale broken fork silhouettes over layered dark cut banks, moss caps and restrained exposed roots. Occlusion must not hide whole neighboring lanes.
- Working places: weathered warm planks, bound bundles, pale cloth, iron tools and muted rust; recognizable working/broken/empty states remain distinct.
- Inhabitants: original strong species silhouettes, restrained bright faces/eyes, preserved native movement, attacks, equipment and hit picking. Retain player recognizability across regions.
- Crops/items: use existing authored growth/harvest and portable geometry where suitable. Never replace a real state with an attractive but false prop.

## Source verification and corrections before implementation

| Source | Confirmed contract / correction | Design consequence |
|---|---|---|
| `SoddenVoxelKitBuilder` / `SoddenVoxelLibrary` | Old kit has six families × four variants, shared ring palette, strict 24-entry validation; floor/water are single boxes. | Add a scoped library instead of weakening old kit invariants or recoloring global assets. |
| `SoddenCompositionPreviewBatch` | Can render six actual native formations over three seeds and report missing meshes, without saving a scene. | Captured 18 baseline views in `Verification/SoddenArtDirection/Before`; extend with gameplay-zoom/native Play checks. |
| `SpreadPresentationScope` / current map | Sumphold15.6 is map-classified Spread. Drowned Ledger and the three district addresses are Sodden. | Exact authenticated Sumphold exception for visual scope; no gameplay biome rewrite. |
| `SpawnRing3DRecipes` / `SoddenDistrictRecipes` | Native owner/state validation precedes art selection; repaired/harvested sites have semantic state gates. | Substitute only valid native recipes, preserving missing/invalid fallback behavior. |
| `SpawnRing3DPresenter` | Owns instantiated models, actor interpolation/Animator, equipment, input picking and fog; Spread-only art/lighting paths exist. | Add explicit Sodden scope/material ownership, not a global Spread flag. |
| `SpawnRing3DGroundPatches` | 10×5 patches batch source fragments and rebuild by fingerprint. | Integrate palette/geometry into existing patch lifecycle; unchanged FOV must not rebuild geometry. |
| `VoxelWorldPresentation` | Explicit already-voxel meshes bypass conversion. | Register new authored meshes to preserve cuboid forms. |
| `NativeZone3DRenderSurface` | Clones all material families, controls native fog/light, local shadow bias and ambient probe. | Tune owned region lighting only; never modify shared render settings/materials. |

Full bounded audits: `Verification/SoddenArtDirection/ModelDesign.md` and `CoverageAudit.md` (in progress at initial planning).

## Delivery milestones

1. **Asset contract:** additive deterministic kit/private palette; preserve GUIDs on rebuild; exact model manifest; finite geometry, bounds, UV and renderer validation. New `.cs` assets receive metadata. Native RED before asset generation.
2. **Live regional integration:** authenticated current region/town scope, validated recipe substitutions, shared functional actors/props, patch batching and lighting. Countercases include another biome, fake/stale zone, missing art, hidden/dead/moved owner and changed map authority.
3. **Coverage and visual iterations:** all six formations × at least three seeds; three district destinations; Sumphold/Drowned Ledger; gameplay-zoom inspection of silhouettes, wet/dry routes, actors and working/broken/harvest states. Refine source, regenerate and rerender after each meaningful visual finding.
4. **Functional release checks:** native movement, targeting, interaction/harvest, equipped items and zone revisit/FOV remain functional. Run affected visual/gameplay regression; separate adversarial pass; record current live errors, image evidence and performance samples. Review visible quality at actual zoom, not just an asset contact sheet.
5. **Commit and follow-on:** living doc and evidence with source, fetch/rebase/push main. Then detailed design and iterative implementation of encounter combinations, expedition clues and finite NPC supplies, as requested. Continue high-value improvements based on observed gaps rather than arbitrary feature counts.

## Performance

Follow `PERF-FOUNDATION.md`: all mesh authoring/import happens offline, no new per-frame geometry or global scans. Cache library lookup once per bind; missing IDs are cheap. Reuse dirty patch fingerprints and existing animation scheduler. New static kit targets modest cuboid counts (typically 8–40, hard budget documented in library); distant terrain detail must remain batchable. Capture native frame/refresh samples before and after over real gameplay; report max and scope, not only average, and distinguish Editor from build performance. No performance claim until measured.

## Readiness and honesty bounds

- 🟢 Existing Sodden generation, real stateful district services and old 3D baseline are available.
- 🟢 New 85-model visual kit, regional integration and current surface coverage are implemented; 224 focused native checks pass.
- 🟡 Production readiness requires submitted geometry, native Play, visual review and regression evidence, not only successful import.
- 🧪 Automated assertions verify identity, state, coverage and outcomes; screenshots support bounded visual judgment. They cannot establish every hardware configuration, long-term balance or universal player preference. A standalone build gate must be reported separately if not run.

## Implementation log / self-review

2026-10-08: completed preceding local Qud investigation, captured 18 native baseline formation images. Inspected renderer scope, batching, material ownership and old kit constraints. No production art changes at initial plan publication. Identified Sumphold map classification and shared-asset contamination risks before implementation.

2026-10-08 RED gates: `native-integration-population-red.xml` records 35 cases (12 already-correct counterchecks, 23 expected failures: absent regional scope/submitted-art proof, frogs/viper/worker, portable supplies, placed utilities and Greatdew). `native-assets-red.xml` records 22 expected failures against the absent additive library. Production editing began only after both runs. Original old-kit tests remain unchanged.

Implementation correction: Greatdew is a non-solid Vegetation/PhysicalObject with a real step snare, not a creature despite its population-table source. It receives a static hooked/beaded plant model, preserving its actual trap and destruction state. The new art route never invents an enemy, attack, collider or trigger.

2026-10-08 first native review: the 30-model kit imported successfully. Native asset (22), population (24) and adversarial (28) cases passed. The combined 100-case review found 16 failures: four real admission/terrain/fallback gaps, nine missing regional transient-visual cases, and three gas-fixture failures caused by absent definitions (fixture corrected before rechecking behavior). Actual scope is 63 current Sodden surfaces, including named sites and lair mouths; the old static wilderness index is insufficient. Sumphold remains an authenticated visual exception with unchanged gameplay biome.

Iteration1 captures 23 generated scenes (six formations × three seeds plus five inhabited/district destinations). All 58,332 observed visible owners received authored geometry; 3,697 observations across 38 blueprints lacked approved style evidence. This is not a missing-model count. Visual review confirmed a substantial dark-bog improvement but identified bright settlement paving, plain masonry, repetitive snag crowns, vent-like peat caps and parquet-like boards as meaningful remaining work. Regional asset overrides and a second capture are required before visual completion. The census did not include all crop stages or loose items; separate source-backed fixtures will cover those instead of claiming universal coverage from these samples.

2026-10-08 second native RED: `native-polish-transients-red.xml` confirms nine visible-shape defects (peat caps/strata, board continuity and crown silhouettes) and twelve region-excluded transient cases. After scope repair, gas and elemental cases pass; finite-film tests also needed explicit liquid-registry isolation in their fixture. `native-regional-red-content.xml` records56 cases:43passed, seven new regional-art expectations failed, one earlier Ledger board expectation remained unapproved, and five fixture assumptions required correction (three uninitialized liquid definitions, auto-equipped Peatlantern cup, and a non-paving negative-control floor). These fixture fixes are not production bug claims. User explicitly permits regional sprites and art direction to differ from the Spread.

2026-10-08 verification recovery: a broad 2,699-case native rendering run stopped producing results after reporting progress through 1,008 cases. The editor remained busy and unresponsive for more than ten minutes; no completed XML was produced. The editor was restarted with no active user Play session or unsaved user scene. This run is **interrupted, not a passing regression result**; bounded affected batches replace it. Evidence: `native-broad-interrupted.json` and `native-broad-process-sample.txt`. Intentional historical unsupported-Sodden controls now use unsupported Beating instead; native owner assertions remain. PeatCutter's district pin changes from its older static town form to the shared original articulated worker already selected by the new regional route.

2026-10-09 native Play first run (`SpreadDiscoveryExpeditions/Native/e1f258aed37649188a5c4e51e3e90fb4/report.json`) reached nine passing checks: original start, Sumphold lead, broken shelter, real harvest, dry crossing, finite stock, forge preview, forged assembly and actual equipped mesh proof. It then failed the observer's historical starting-dagger guard during return combat, because the same harness had deliberately equipped the earned mallet. The audit is corrected to accept only its exact recorded earned mallet plus native equipment/family checks; no player state, AI, route clearance or combat budget is relaxed. This first run remains failed evidence.

Final visual iteration: the same 23 generated cases retain all 58,332 authored/rendered observations, with exact-style gaps reduced to 157 shared existing props (from 3,697 initially). The separate seed64 census covers all 63 map-Sodden surfaces plus authenticated Sumphold: 162,518 visible owner observations, zero unmodeled/unrendered owners, and 1,148 observations of shared original art without regional style approval. This is not a claim that every prop was newly modeled; detailed residuals belong to CoverageAudit. Independent final pixel review accepts the bounded regional direction; optional naturalism remains in cut-bank tile rhythm and some simpler shared actors.

Performance observation: the first ordinary journey recorded 90.03 seconds / 2,635 frames with native ProfilerRecorder markers. Renderer mean24.59ms/p9562.65ms/max317.34ms; main-thread mean34.14ms/p9575.04ms/max1,547.97ms; recorded allocation max4,772,371,951 bytes. These are high readings, not a performance pass. They include zone transitions, native AI, screenshots and serialized audit reports, with no pre-change baseline. The observer records valid flags, units and raw samples. A controlled attribution/baseline pass is still needed before any standalone or general production-performance claim. No current claim of an FPS improvement.


## Final native regression and review

The completed affected native EditMode batches total **666/666 passed** (509 existing/control cases plus157 new cases): regional assets/authority/state/content224, shared profile/equipment106, shared scenery/transients218, and camera/core/legacy Spread118. Raw XML is in `Verification/SoddenArtDirection/native-{final-regional,shared-profile-equipment,shared-scenery-transients,shared-core}-green.xml`. This is an affected-suite result, not a claim that all26,194 project tests ran successfully. The two earlier broad attempts were interrupted without complete XML; later bounded runs demonstrate that slow editor fixtures and remote-control timeouts do not alone establish a deadlock.

Cold-eye review:

- 🟡 Resolved — preserve native owner/state rejection before art overrides; authenticate current world surfaces and exact Sumphold exception. Adversarial tests cover mutated map identity, stale graphs, hidden/moved/destroyed owners and altered submitted meshes/materials.
- 🟡 Resolved — shared actor/crop/item and transient admission previously excluded the region. Exact identity/state and existing false controls now pass; historical unsupported-region controls use Beating rather than the newly supported Sodden.
- 🟡 Resolved — Peat caps, settlement paving, masonry, boards and residual vine/brine/acid/vent geometry were visually iterated through85 original models. Existing functional states and rigs remain native.
- 🔵 Review — new geometry uses the existing patch fingerprint/lifetime; release destroys added owned meshes/materials while shared source assets remain untouched. Other biome profiles keep their original palette and shadow policy.
- 🧪 Deferred — Quiet's Door (16.1) and Spivenor (16.4) account for724 of1,148 remaining exact regional style-proof gaps in the all-surface census. They render native3D, but were not individually camera-reviewed in this pass. Shared containers, vegetation and named-site furniture are not falsely counted as new models.
- 🧪 Deferred — overlapping bind-time library validation copies mesh buffers and deserves controlled measurement. No observed source change establishes it as the cause of the high journey timings. Profile a fixed zone with audit image/JSON work disabled and an actual old/new baseline before a performance or standalone production-readiness claim.

Files changed: `ArtSource/SoddenNativeArt3D` reproducible source; `Resources/SoddenNativeArt3D` imported85-model kit and private palette; `SoddenNativeArtLibrary`, `SoddenPresentationScope`, `SoddenPopulationArtRecipes` authority/asset contracts; existing presenter/recipes/patches/style evidence/voxel and shared actor/item/transient admission; two Editor kit/preview tools; six new regional test classes and explicit existing negative-control pins; native journey equipment guard and observational profiler; this living document and source/capture/test evidence. No gameplay blueprint, save migration, population, item stock or world generation rule changes.


### Ordinary Play correction: territorial self-defense

The second ordinary journey (`1848f6cae6dd4bbe8a799aceb0de9612`) also reached nine checks, then found a real gameplay bug during the return crossing: the neutral cutbank guard could enforce its territory through combat, but ordinary movement input could not attack it because no permanent faction hostility existed. The zero-action native clock assertion correctly refused this input. The issue was fixed separately with a pure exact-current enforcement query and shared permit/hospitality protection; the route, stock, player health and clock requirements were not weakened. See `TERRITORY-SELF-DEFENSE.md`. Native21 new checks plus102 affected controls pass. The repeated route remains a separate gate.


### Final ordinary Play result

The repeated journey `29dcc15952cb499abeabcccca9733cf1` passed **14/14** checks in237.40seconds, with zero failures and zero unexpected runtime errors. Actual input exercised the Sumphold lead, broken shelter, crop harvest, dry crossing, finite works stock, pack-forging preview/assembly, exact equipped mallet/screen geometry, returning territorial self-defense, timber repair, paid preparation and F5/unsaved-step/F6 persistence. Final restored-bench and saved-return screenshots were reviewed at gameplay zoom; the live3D owners, repaired state and readable board routes are visible.

**Can verify:** those named script-observable outcomes and the bounded captured regional appearance. **Cannot verify:** all-seed balance, every named-site prop's art approval, every hardware configuration, independent player preference or standalone build performance. The high, audit-contaminated Editor readings remain a follow-up, with concrete source inspection and a controlled measurement procedure in `Verification/SoddenArtDirection/PerformanceReview.md`. No FPS improvement or universal production-readiness claim is made.
