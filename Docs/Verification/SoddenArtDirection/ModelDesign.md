# Sodden native art: model design and source contract

Status: design and source audit, 2026-10-08. Original CoO art; no borrowed creature identities or copied reference-game assets. Gameplay generation, native entity roots, occupancy, interaction and save identity remain authoritative. The next work gate is failing native asset tests, then implementation, then actual gameplay-camera review.

## What the existing kit does, and why the new pass must look different

The current Sodden builder generates six families with four variants each using the shared ring palette ([builder:14](/Users/steven/caves-of-ooo/Assets/Editor/Scenarios/SoddenVoxelKitBuilder.cs:14)). Its floor variants vary buried thickness; the visible top is identical. Mire also uses one flat box ([builder:96](/Users/steven/caves-of-ooo/Assets/Editor/Scenarios/SoddenVoxelKitBuilder.cs:96)). The current bodies are eight-box prone figures; trees eight-box bare forks. This is genuine mesh geometry, but extremely sparse geometry.

The existing test contract deliberately requires exactly 24 entries, at most 240 vertices, one/two palette swatches per model, and single-box ground/water ([SoddenVoxelKitTests.cs:14](/Users/steven/caves-of-ooo/Assets/Tests/EditMode/Presentation/Rendering/SoddenVoxelKitTests.cs:14), [SoddenVoxelKitTests.cs:47](/Users/steven/caves-of-ooo/Assets/Tests/EditMode/Presentation/Rendering/SoddenVoxelKitTests.cs:47)). Replacing its palette or expanding its silhouettes in place would break a still-valid legacy library contract. Preserve it and introduce an additive art override.

Reviewed native-pipeline baseline images [OpenMire-64](/Users/steven/caves-of-ooo/Docs/Verification/SoddenArtDirection/Before/OpenMire-64.png) and [DrownedCopse-1729](/Users/steven/caves-of-ooo/Docs/Verification/SoddenArtDirection/Before/DrownedCopse-1729.png). They show a broad olive/yellow ground, thin red-tipped stems and largely uniform angular water fields. Copse forks are visible but resemble repeated posts. The primary improvement must be visible across the whole screen: dark ground, deeper water, legible clustered vegetation and skeletal branching. Tiny prop details alone will not satisfy this brief.

## Visual direction

Use the reference scene's block silhouettes and restrained palette, with actual height, overlap and cast shadows. Keep the Sodden its own place:

- **Land:** dark tea/teal (`#092D29`, `#103A33`) with sparse low moss fragments. Broad quiet surfaces remain; no raised border around every tile and no random checkerboard.
- **Mire:** nearly black blue-green (`#071C20`) with a few broad muted reflections. Water must remain distinguishable from walkable land at normal zoom and in remembered visibility. Decorative fragments never assert a bridge or safe tile.
- **Cut peat:** wet grey faces (`#374D4A`, `#52625A`) with darker horizontal cuts and a moss cap. Existing bank height remains visually substantial.
- **Plants:** dark rooted bases, jade upright fingers, occasional ivory seed heads. Shapes must read in the gameplay camera without thin alpha cards. Three to seven grouped stems with asymmetry, negative space and a few stepped branches beat dozens of hairlike blades.
- **Snags:** pale skeletal angular branches (`#CBD1B1`) rising from dark saturated bases. Fork geometry and broken tips differ across four variants; existing 2.7–3.25-cell landmark height is retained.
- **Worked wood:** worn warm brown boards with dark wet ends and dull fastenings; broad crosswise planks communicate the actual traversable route.
- **Bodies:** unmistakably prone humanoid proportions, separated head/forearms/lower legs and water-dark clothing. Ordinary `BogTakenBody` remains an ordinary bog body; do not add Curation salts, labels or ritual props that imply unrelated lore.
- **Actors:** preserve signature colors, especially the existing scarlet/black Bandfrog. Player readability and held equipment take precedence over making every visitor green.

A private 24–32-color point-filtered atlas allows this without repainting other biomes. Proposed broad role allocation: 4 ground, 2 mire, 3 peat/stone, 3 pale wood/bone, 4 vegetation, 3 worked wood, 2 metal, 3 signals. Source declares exact chosen palette size; importer and validator agree rather than guessing existing UV dimensions.

## Full-biome coverage checklist: 30 model groups

These are coverage groups, not a target for an arbitrary number of newly created assets. Existing adequate original models may be adopted with a scoped palette treatment; stage/state variants are never chosen randomly. New scenery gets four genuinely different silhouettes unless noted. Triangle budgets below assume combined cuboids at 12 triangles each; they are ceilings, not quotas.

| # | Group / native content | Required readable form and variation | Per-model budget |
|---|---|---|---|
| 1 | Grass/Floor ground | Continuous low top, sparse coarse moss/pebbles; four distributions | 24–120 triangles |
| 2 | MirePool/PeatBog | Dark continuous plane; occasional low reflection marks with no perimeter rims | 12–96 |
| 3 | PeatBank | Stepped wet cut, broken cap, exposed dark seam; four cut profiles | 240–576 |
| 4 | DeadTree snag | Broad angular dead forks, varied broken crown, dark root toes | 300–720 |
| 5 | Duckboard | Three/four crosswise top boards, sleepers, restrained missing corners | 180–384 |
| 6 | BogTakenBody | Prone head/torso/limbs, clothing seams, four arm/leg arrangements | 300–576 |
| 7 | Reeds | Compact jade/ivory groups, heavy rectangular stems, uneven crown | 300–600 |
| 8 | Native bushes / low wet growth | Broad connected low tufts with dark bases; exact blueprint aliases only | 240–480 |
| 9 | Native fungi / forage clusters | Stepped caps distinct from upright reeds; preserve harvest state | 240–576 |
| 10 | Rubble / worked stone fragments | Low sparse chips and one dominant broken block | 180–420 |
| 11 | StoneWall / StoneFloor | Wet grey masonry, heavier corners, native damage/state respected | 240–960 |
| 12 | Timber walls / doors where already present | Posts and open/closed leaf silhouette; no decorative door on solid wall | 240–720 |
| 13 | Actual chests / barrels / containers | Banded wood, clearly readable lids; exact existing interaction owners | 360–720 |
| 14 | SoddenDressingBench | Broken and repaired topology, pads/tools on functional surface | 480–960 |
| 15 | SoddenWorksSalvage | Timber stack with visibly separate recoverable members; removed after harvest | 240–600 |
| 16 | Route notice / native signs | Warm upright frame with pale board; no hovering mission icon | 180–384 |
| 17 | Existing cooking fire | Lit/cooled state from actual heat; model adds no real light | 240–576 |
| 18 | Native beds/chairs/workstation furniture | Low human scale, purposeful lumber frame, leg clearance | 240–600 |
| 19 | Sumpsieve | Broad layered sponge pads; seed/growth/mature plus dry/wet states | 144–720 |
| 20 | Drowsebell | Hanging swollen bell/bladder silhouette, low stem cluster | 144–720 |
| 21 | Chillcress | Low pale-jade rosette, sharp upward tips, visually unlike reeds | 144–720 |
| 22 | Slipsedge | Bowed wide blade clump and visible slick nodes | 144–720 |
| 23 | Peatlantern | Cupped pale center above dark peat stalk; native light owns emission | 144–720 |
| 24 | Greatdew | Non-solid bent stem with clear sticky beads, hooked pale tip and jade mound; two distinct static forms | 240–720 |
| 25 | Reedfrog / GinFrog | Small separated head/body/legs, distinct species palette and silhouette | existing rig; target ≤2,400 |
| 26 | Bandfrog | Retain scarlet/black warning anatomy and native frog rig | existing rig; target ≤3,600 |
| 27 | MawToad | Low broad jaw and heavy hindquarters; not a scaled Bandfrog | existing rig; target ≤3,600 |
| 28 | Viper | Continuous segmented curved body, lifted readable head; native motion retained | existing rig; target ≤2,400 |
| 29 | Player / PeatCutter / named workers / visitors | Compact pale head, readable coat/limbs; retained bone hierarchy, clips, sockets and equipment | existing rig; target ≤4,800 |
| 30 | Portable yields / tools / equipment | Existing 3D item silhouettes remain recognizable in hands and on ground; no disappearing carried-source paint | reuse current budgets |

The actual Sodden population includes Reedfrog, GinFrog, Bandfrog, Viper, Greatdew and MawToad ([PopulationTable.cs:550](/Users/steven/caves-of-ooo/Assets/Scripts/Data/Tables/PopulationTable.cs:550)). Do not invent leeches or substitute Grovelands' Shambler merely because they seem swamp-like. The five crop identities are already authored ([BiomeCrops.json:79](/Users/steven/caves-of-ooo/Assets/Resources/Content/Data/Farming/BiomeCrops.json:79)); their library already supplies three stages × wet/dry plus seed/yield models, so state coverage must survive ([BiomeCropSource.cs:34](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/BiomeCropSource.cs:34)).

## Exact additive architecture

**Source:** `ArtSource/SoddenNativeArt3D/build_kit.py` generates deterministic `kit.json`; source schema contains palette and exact existing model IDs, family/variant, cuboids and optional source rig reference. Record SHA-256 in native import receipt. All variants must change exposed silhouette/distribution, not just buried geometry. Artist-readable source parameters permit iteration without runtime generation. Optional Blender review builds from this same JSON; it is not the game artifact or acceptance gate.

**Runtime asset contract:** new `SoddenNativeArtLibrary` at `Resources/SoddenNativeArt3D/Library`, exposing `Load()`, `Validate()`, `Find(string existingModelId)`, `Material`, public `Entries`, and `Entry { Id, Prefab, Mesh, Spec }`. `Id` is the existing validated native recipe model ID. `Spec` measures the replacement mesh; prefab paths live exclusively in the new owned folder. Start with every `sodden-{ground,mire,peat,snag,boards,body}-{0..3}` plus `spread-reeds-{0..3}`: 28 replacement meshes, plus two new `sodden-native-greatdew-{0..1}` entries admitted by a separate exact native-owner recipe. Preserve the old library and strict tests. Then resolve additional exact IDs from the supported-zone census before adding prop/crop/actor overrides. Sumphold's effective recipe IDs may already be Spread refinements; census the effective IDs rather than assuming its floors use the old Sodden IDs.

**Importer:** new explicit editor-only `SoddenNativeArtKitBuilder`. Follow the existing deterministic glade pipeline: preflight all source and palette data; make a private fog-aware material using the existing shader; create persistent meshes and inert prefabs; update existing generated assets in place to preserve GUIDs; validate; save only owned assets; write a measured receipt. The glade already implements source hashing, palette creation and combined-cuboid mesh import ([ReferenceGladeVoxelKitBuilder.cs:26](/Users/steven/caves-of-ooo/Assets/Editor/Scenarios/ReferenceGladeVoxelKitBuilder.cs:26)). No scene saves, no runtime cubes, no collider/light/behavior on static art.

**Renderer integration, separately owned:** native recipes validate entity identity/state first, then the private library may override that exact model ID on the owned presentation instance or batch. Existing code resolves Sodden native families before fallback catalog binding ([SpawnRing3DRecipes.cs:560](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/SpawnRing3DRecipes.cs:560)). Ground batching needs the new palette registered and a current-owner material stream; do not create invisible duplicate ground or disable fog. Presenter registration currently differs between Spread/glade and Sodden district ([SpawnRing3DPresenter.cs:137](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/SpawnRing3DPresenter.cs:137)); merely installing assets will not update native play.

**Scope:** use verified current Sodden biome/zone authority, with explicit named-site inclusion where intended. Sumphold is documented as a Spread POI; do not infer “all Sodden” from its name or recolor the whole Spread ([SODDEN-COMPOSITION.md](/Users/steven/caves-of-ooo/Docs/SODDEN-COMPOSITION.md)). Six wilderness formations already exist; retain them and their routes. A saved owner removed/harvested/destroyed must remove its image, never be reconstructed from a generation plan.

**Actors:** do not flatten skinned actors into the static library. Prefer the existing original frog model and rigid cuboid skins. If new actor geometry is needed, use persistent source-mesh→authored-mesh replacements that preserve exact bindposes, owning bones, clips, equipment sockets and animation envelopes, as the glade already does ([ReferenceGladeVoxelLibrary.cs:46](/Users/steven/caves-of-ooo/Assets/Scripts/Presentation/Rendering/ReferenceGladeVoxelLibrary.cs:46)). Public actor override API is deferred until actual source coverage is known; static renderer work must not depend on a placeholder method. All source assets remain unchanged.

## Gates and counter-checks

1. RED native tests: missing new library; initial 28 replacement IDs plus two Greatdew IDs; private material; visible geometry/contrast constraints; malformed entries rejected. Reflection or serialized inspection allows these tests to compile before the new type exists.
2. Asset GREEN: each mesh's bounds/triangles/UVs match its Spec, all static prefabs have one renderer and no gameplay components, all four exposed variants differ, no unexpected source paths or global palette changes. New C# files have fresh `.meta` GUIDs.
3. Integration: exact current owners obtain overrides only in admitted scope. Non-Sodden counter-zone remains unchanged; hidden, removed, destroyed, harvested and transferred objects obey native state; crops retain stages; doors/bench retain state; portable items and equipment remain present.
4. Native views: repeat all six formations across the same three seeds, plus district preparation stop, works and crossing; include normal gameplay camera, zoom, darkness/memory and actors. Compare the same location before/after. Inspect an actual walk, border change and reload, not just a gallery.
5. Acceptance: dry route/water separation, recognizable ordinary body, reeds and snags with three-dimensional silhouette, existing actors readable against dark land, working item selection and no unexplained fallback sprites in the measured coverage census. Gameplay/spawn/camera changes are not substitutes for model quality.

Performance remains bounded: static cuboids combine offline into one mesh per model, batched terrain uses existing dirty regions, and no per-frame procedural vertices or per-voxel GameObjects. A rough 2,000-cell zone at the ground ceiling contributes at most 240,000 terrain triangles before batching/culling; keep typical ground much lower (24–60 per cell) and measure actual visible workloads. Native frame measurements, not these estimates, decide whether density needs reducing.

## Honesty bounds

This design reviewed source and two existing native preview images. No new models, gameplay code or source assets were written for the design step. Numeric geometry tests can establish height, colors and contracts; they cannot establish resemblance, readability, motion quality or frame rate. Those require the later native visual and live interaction gates.

Pre-implementation correction: Greatdew appears in population tables but is **not a Creature**. Its blueprint inherits PhysicalObject, has Vegetation, non-solid physics and GreatdewSnare; the inspection text specifies a bent stem carrying clear beads ([Objects.json:33577](/Users/steven/caves-of-ooo/Assets/Resources/Content/Blueprints/Objects.json:33577)). It needs a static authored model and exact native-owner admission, not an actor rig or generic creature fallback.

## First implementation candidate

- Native asset RED confirmed 22/22 expected failures before new source/library/builder work; evidence `native-assets-red.xml` in this directory. Reflection-based tests compiled while the new library was absent.
- Added deterministic original source and explicit native importer. The first candidate has 30 models, 657 cuboids and 7,884 triangles across the complete asset set; source SHA `745eeeb065e1c18c7050099c81c8d07b2df2bf3da68f42ace9a36c90e9b5dc2b`.
- Per-model measured source triangles: ground 60–84; mire 36–48; peat/snags 360; boards 240; bodies 312; reeds 408–432; Greatdew 336. Heights and horizontal bounds pass offline preflight; native import/tests and visual review remain pending.
- This first candidate replaces the six existing Sodden families and reed family, plus adds Greatdew. The 30 coverage groups above remain a coverage checklist, not a claim that all actor/prop/crop groups have newly authored replacements.
- Static ownership review: new assets contain one mesh/renderer, no collider/light/behavior and no runtime generation. Private palette and generated folder are additive. Existing library pins, source rigs and gameplay blueprint data remain unchanged.

## Iteration 1 native visual review

Native import succeeded and all **22 asset tests passed**. The integration suite has separate scope/overlay issues being addressed by its owners; this is not whole-feature acceptance. Reviewed ten native captures under `Iteration1`, including ReedMaze/DrownedCopse/BogFace/PeatCuts primary views, OpenMire overview, Causeway primary, two actor views and DressingShelter crop view.

**Accepted direction:** the dark land and deeper mire are immediately different across the frame. Reed clumps now have broad pale tips, three-dimensional stems and readable shadows. The native camera can read their shape rather than a few old thin lines. Keep this palette/hierarchy; no camera/spawn manipulation is necessary to demonstrate the improvement.

**Polish recommendation 1 — peat currently looks manufactured.** [PeatCuts primary](/Users/steven/caves-of-ooo/Docs/Verification/SoddenArtDirection/Iteration1/PeatCuts-64-primary.png) and [BogFace primary](/Users/steven/caves-of-ooo/Docs/Verification/SoddenArtDirection/Iteration1/BogFace-64-primary.png) show repeated bright green vertical slats on a dark rectangular face, like an industrial vent. The source's five long parallel moss strips create the slat effect. Worse, source face detail is only at +Z, while the native camera primarily sees -Z. Proposed correction: offset horizontal wet-earth strata on both opposing faces, chipped upper ledges, and a few short irregular moss patches rather than five long strips. Preserve the native cell, bank height and geometry ceiling. Add a RED check for visible near-face strata and broad/short rather than long/narrow cap growth before changing source.

**Polish recommendation 2 — boards currently look like parquet.** [Causeway primary](/Users/steven/caves-of-ooo/Docs/Verification/SoddenArtDirection/Iteration1/Causeway-64-primary.png) clearly communicates a traversable wooden route, but each cell's three-by-three segments form a repeated checkerboard. Replace three segments per plank with three long crosswise boards, retaining only small end wear, shallow scratches and fastenings. A RED long-board extent check should precede the source change; route width/occupancy remain native.

**Polish recommendation 3 — snag topology can vary more.** [DrownedCopse primary](/Users/steven/caves-of-ooo/Docs/Verification/SoddenArtDirection/Iteration1/DrownedCopse-64-primary.png) has visible pale three-dimensional forks, but the repeated Y/cactus anatomy remains apparent. Different broken crown/branch height arrangements would be more useful than further small color variation. This is secondary to peat/boards; no added trees or population change is needed.

**Coverage observations, not requests to fake native state:** the pale-green P shape in BogFace actor view is three actual `BrinePool` owners (`ring-brine-pool-1`) in the owners receipt, not a failed Greatdew. The bright neon patch in OpenMire is real acid; future material improvement must keep those hazards distinct. `MawToad` still uses `ring-maw-toad`, with much simpler anatomy than the newer native frogs. DressingShelter's low crops reflect actual current growth stages; do not make immature crops visually ripe. Sumphold's existing pale tiled floor/water remains a larger consistency issue than adding detail to small props.

No source polish was applied during this review step. The next iteration requires agreement on the bounded changes, failing contracts first, reimport and the same native images.

## Second candidate and Iteration 2 visual review

The nine peat/board/snag polish contracts failed before source changes (`native-polish-transients-red.xml`). Separate generated-owner regional tests established missing named-site replacements before the 45-entry expansion (`native-regional-red-content.xml`). The second import passed: **75 models, 1,341 cuboids, 16,092 triangles**, maximum 36 cuboids per model, source SHA `7b15437971547039bd6ec9de9b5d6344ffa0eccbeb8ae93e09df0da1f4ad9fc4`; measured receipt `native-kit-import-second.json`. The broad native regression is still pending at the time of this review.

The 45 additions are four variants each of Sumphold paving, shared named-site floor, Sumphold masonry and ordinary water, Drowned Ledger boards, straight/corner canvas, preserved witnesses, stakes and reading tables, plus the district's two bench states, works locker, recoverable salvage and route notice. Existing effective IDs, owner validation, orientations and native interaction states remain authoritative. Ground kinds are explicit for both added floor families; other additions remain entity models. The private palette is unchanged.

Reviewed twelve actual `Iteration2` captures, including Sumphold primary/overview, DressingShelter primary, DrownedLedger primary/actor, PeatWorks primary, PeatCuts primary, BogFace primary, DrownedCopse primary/actor, Causeway primary and OpenMire overview.

- **Accepted:** Sumphold and the district now share subdued wet-grey floors and readable layered masonry instead of mismatched bright paving. The preparation bench stands out against the floor; the works locker reads as timber furniture. Ledger's canvas preserves its open entrance, and the table and separate preserved witness remain individually visible. Long continuous causeway planks read as worked boards rather than parquet. Snag variants now differ in crown direction/height. Dark mire and ordinary water remain distinct.
- **Remaining conspicuous art issue:** peat's front strata improved, but three cap columns with two aligned green patches each still resemble rows of seats or vent modules. This repeats in PeatCuts, BogFace and Sumphold. A bounded follow-up should interrupt the grey top border and stagger a few moss patches across broad uneven ledges, changing exposed width/height across variants. Merely passing the short-moss test does not establish a convincing natural bank.
- **Secondary limitation:** MawToad still uses the simpler original `ring-maw-toad` form. Its scale and native animation remain valid, but the newer frogs have much stronger anatomy. This is a model-quality follow-up, not a missing fauna mapping.
- **Intentional retained states:** bright acid is actual dangerous acid, the pale-green pool is actual brine, immature crops stay immature, and the native formation remains sparse where authored generation is sparse. No decorative hazards, creatures or interactive clutter were invented for the screenshots.

This review is based on native still images, not a claim that movement, save/reload, targeting, fog transitions or frame rate have been accepted. Those gates are separately owned by the live verification and regression passes. No asset or code was changed during the review.

## Bounded final art iteration plan

The same 23-zone census leaves 307 exact-style proof failures after the 75-entry pass. They are already rendered native models, not missing content. The strongest visual outliers are 91 vine-wall fragments and 33 brine/acid pool observations; steam vents add 26. Add the complete four-variant vine-wall and brine families plus the single acid and steam forms: **10 new IDs, 85 total entries**. Preserve original brush/log models rather than adding a partial set of their four variants. MawToad remains an existing compatible rig; no actor-skeleton work is part of this slice.

| Existing model IDs / owner | Source verification and intended form | Geometry ceiling |
| --- | --- | --- |
| `ring-vine-wall-{0..3}` / VineWall | `Objects.json:7709`: real destructible Plant wall. Dark rooted interwoven vertical stems, offset side branches, broad jade leaf clusters; enough body to read as a blocking thicket, not harmless reeds. Preserve original 1.2–1.32-cell broad height and one-cell bounds. | 40 cuboids/model |
| `ring-brine-pool-{0..3}` / BrinePool | `Objects.json:30812`: native brine LiquidPool and high-conductivity material. Cyan-grey liquid below small pale mineral/crystal traces, distinct from dark mire and ordinary water; no raised impassable rim. | 16 cuboids/model |
| `density-pool-acid` / AcidPool | `Objects.json:21984`: real acid LiquidPool with bright-green warning. Green surface with sparse contrasting yellow-green raised bubbles/foam; preserve warning identity rather than matching safe teal water. | 16 cuboids |
| `ring-steam-vent` / SteamVent | `Objects.json:31563`: real Steam material/Thermal owner. Dark open mouth surrounded by wet mineral stone and pale deposits; no decorative live gas entity, light or collider. Existing real gas/heat visuals remain separate. | 28 cuboids |

Keep the private 24-swatch palette and exact existing native selections. New entries remain entity models. Bounds, inert prefabs, source hash and regional-owner admission use the same importer/library contracts. Brine retains pale cyan paint, acid retains clear green/yellow paint, and both must be measurably brighter than dark mire in source without increasing the whole biome's brightness.

Peat receives no new IDs: remove three long narrow cap columns and the repeated three-by-two moss matrix; replace with broad overlapping terraces whose exposed edges, widths and heights vary. Use three or four staggered short moss patches. Retain bilateral front strata, existing height envelope, cell occupancy and the previous short-moss positive test. Add four explicit RED cap-profile counterchecks before source edits so replacing thin slats with chunky slats cannot silently satisfy the entire art gate again. Visual inspection of the final same-location captures remains necessary even when numeric checks pass.

Test/import sequence: wait for active native run; add the 14 asset tests only; record native RED; update deterministic source/library manifest and reviewed hash; reimport; rerun asset/integration regressions; inspect PeatCuts, BogFace, ReedMaze and OpenMire at their previous locations. This is the final bounded static-art iteration, not a mandate to remodel every pre-existing item or prop.

### Final source candidate

Native RED completed before this candidate: 105 cases, 87 passed and exactly 18 intended final-art failures (14 numeric asset contracts and four actual native-owner integration cases). The other transient/content/previous-art cases passed. The earlier broad 2,699-case attempt hung and was interrupted; it is **not** a passing regression result.

Implemented the agreed 85-entry source, preserving all prior IDs and the 24-color palette. Ten additions measure: vine walls 33 cuboids each; brine pools 10 each; acid 12; steam vent 16. The four peat variants use three staggered broad terraces and three moss patches, retain both opposing cut faces, and remove all full-depth narrow cap columns. Total source: **1,529 cuboids / 18,348 triangles**, maximum 36 cuboids/model. Reviewed SHA `e5eb1e3fa457642eebdf8d3d6e3e269964f714101ebbab2404b5545f6159c432` matches the runtime library. Offline deterministic generation, model uniqueness, family budgets, every axis bound and the four cap-profile checks pass. Native import, regression and final visual acceptance remain pending for this candidate.

The importer needs no new API or per-frame work. Same-ID regional replacements use the existing material/mesh submission and owner-validation routes. No native blueprint, population, loot, hazard, actor rig, world state, collision or scene asset is edited by this source pass.

### Final native art review

The final native import passed and the coordinated focused suite passed **224/224**. Reviewed twelve `Iteration3` images, including primary PeatCuts/BogFace/Sumphold, ReedMaze/OpenMire overviews, BogFace's close brine and vine-wall views, the OpenMire vent close-up, and supplementary reed/water/actor views. **Accepted for this bounded regional art pass:**

- [PeatCuts](/Users/steven/caves-of-ooo/Docs/Verification/SoddenArtDirection/Iteration3/PeatCuts-64-primary.png) and [Sumphold](/Users/steven/caves-of-ooo/Docs/Verification/SoddenArtDirection/Iteration3/Sumphold-64-primary.png): broad staggered top ledges and sparse moss replace the six-slot/vent pattern. The low wet cut faces remain readable, and the settlement's grey masonry/floors and warm boards remain coherent.
- [Vine-wall close-up](/Users/steven/caves-of-ooo/Docs/Verification/SoddenArtDirection/Iteration3/BogFace-729490642-actor.png): a dense, rooted, three-dimensional green barrier distinct from the pale thin reeds. Its native opening and nearby occupant remain visible; art does not invent access through the wall.
- [Brine close-up](/Users/steven/caves-of-ooo/Docs/Verification/SoddenArtDirection/Iteration3/BogFace-64-actor.png): cyan water with small pale mineral traces, clearly different from dark mire and subdued ordinary water. The high-contrast liquid cue is deliberate.
- [OpenMire overview](/Users/steven/caves-of-ooo/Docs/Verification/SoddenArtDirection/Iteration3/OpenMire-64-overview.png): acid stays green/yellow with surface flecks rather than the old flat neon block, and remains distinct from both cyan brine and dark mire. This is an overview-scale color/readability judgement; the capture does not provide a close acid detail view.
- [Vent close-up](/Users/steven/caves-of-ooo/Docs/Verification/SoddenArtDirection/Iteration3/OpenMire-729490642-wet-dry.png): a dark open mineral mouth surrounded by pale deposits, with depth and shadow. This static model makes no claim that a live plume is present at every moment.

**Remaining optional art polish, not a blocker:** peat banks still reveal the repeated native tile rhythm and rectangular cut-earth strata; shared MawToad and some small props retain simpler original geometry. More natural contours and a stronger MawToad jaw silhouette could improve a later art pass, but they do not justify another asset iteration before gameplay verification. No additional geometry changes are proposed here.

The `Iteration3` receipt repeats the same 23 sampled zones and **58,332** current visible owner observations: all authored/rendered, zero missing meshes or unmodeled owners. Exact-style failures decline **307→157**, the expected 150 observations covered by the ten additions. The remaining shared original models are explicit; this pass does not claim every asset in the game is newly remodeled. Separate all-surface census, native Play, fog/state transitions and performance measurements belong to their own verification records. These still images and 224 passing tests alone do not prove them.
