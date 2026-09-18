# Eight-chunk 3D art strategy around Morrowfast

Status: **read-only planning; no ring art, ImageGen calls, Unity use or repository edits performed.** This is a CoO-original presentation extension. Root must approve the verified scope before art generation begins. The user requires a distinct ImageGen reference and actual editable Blender/FBX deliverables for each surrounding chunk; screenshots are art references, never world backgrounds or simulation authority.

The native survey is being coordinated with `/root/system_inventory`. The eight routes below are source-verified; formation choices were calculated from the shipped deterministic selector, not observed in freshly generated Unity zones. Live generated/loaded snapshots remain the next proof gate.

## 1. Verified scope and corrections

| Assumption | Inspected source / evidence | Required correction |
|---|---|---|
| Eight neighboring chunks need eight copies of the village renderer/manifest | `Village3DManifest.Validate`, `Village3DLibrary.Definition`, `Village3DAssetBuilder.Build`, `Village3DPresenter.Bind` | They deliberately require Morrowfast's manifest ID, source definition, 37.5-unit art crop and complete authored owner set. Reuse their proven rendering/export helpers, but give procedural terrain a separate model catalog/native-state adapter. Do not relax Morrowfast's strict validation into a catch-all. |
| A saved fixed scene can represent a generated chunk | `ZoneManager.GetZone`, `GenerateZone`, `ReplaceLoadedState`; formation builders | Native generation, repair, entity removal and saved references decide geometry. A Blender example scene is a preview of a captured native snapshot; runtime must bind the current zone. Never recreate current state from an artist's scene or a second RNG. |
| The world seed alone is the full geometry contract | `ZoneManager.GenerateZone` uses `WorldSeed ^ zoneID.GetHashCode()`; `FormationSelector` uses explicit FNV for formation selection | Record and consume an actual native snapshot, including generated positions. Do not assume a Python RNG/hash reproduces C# generation. Cosmetic variation should use its own explicit stable hash, with no gameplay RNG consumption. |
| Every surface water region has a water entity | `GrovelandsFormationBuilder.TendrilFen` | Fen veins are permanent `Zone.TileState` water coatings. Export/render cells, all relevant entities, and tile state; an entity-only scene dump loses the fen. |
| Generation reservations identify loaded holes | `Zone.GenReservedCells`; `SinkholeMouthBuilder` | Reservations are generation-only and not serialized. Lip entities and the real staircase are authoritative. The builder clears solids inside its ellipse but does not stamp a durable Void component. Do not turn walkable native cells into implied impassable/drop-through terrain in art. Record the pit-floor question for native verification. |
| Permanent tile coatings automatically survive saves | `Zone.TileState` documentation and repository-wide searches for save/restore references | Source currently indicates no TileState serialization. A generated→save/load probe must check fen water explicitly. Render the loaded state as it exists; do not regenerate visual water to conceal a gameplay persistence problem. Any fix is its own native RED-first task. |
| Neighboring chunks are generic forests | `OverworldZoneManager` routing and the native survey | Three Stump routes, five Grovelands routes; north is the authored Felling-Site, east/southwest are real sinkhole mouths. No world road/river flags occur in this ring, but local water, paths and landmarks do. |
| Olderdeep's surface should contain its Founding Village | Sinkhole routing | `Overworld.4.6.0` is the surface mouth. The Founding Village/Rooted chamber is below and outside this eight-surface-chunk scope. Do not plant its buildings/NPCs on the surface. |
| Names imply humanoid models | Actual Objects.json descriptions | YellowfootWayfarer is a tortoise; Wardline is an olive colubrid; CascadeFather/GlasspaneFrog/HelmwoodFrog are distinct frogs. Village hoods are suitable only for actual humanoids. |
| Every creature model needs the village's five clips/four equipment sockets | `Village3DAssetBuilder.ValidateMetadata` | That validator is specific to the existing biped kit. A new catalog needs explicit rig families/required clips; a snake, frog, eagle or rooted tendril should not receive fake humanoid hand sockets. |
| Existing palette remapping automatically supports a second atlas | `Village3DPresenter.PrepareModel` | It maps all non-water materials to one Village world material. If a ring atlas is added, introduce an explicit reviewed material-family lookup and matching instance fog/light material clones. Never let import succeed while recoloring all ring models through the wrong atlas. |

## 2. Eight distinct reference/art briefs

Each row gets its **own actual ImageGen reference**, exact prompt/receipt/hash, and editable scene `.blend` plus hierarchical `scene-preview.fbx`, using the accepted village's rounded miniature style. These are provisional prompt briefs, not claims that images have already been made. Each reference uses strict orthographic overhead framing, the same scale/readability as the village, real geometry rather than a photograph on a plane, no painted UI/text, and no invented roads, water, entrances, blockers or creatures outside the captured native state.

| Direction / zone | Native identity | Distinct visual brief and reusable kit focus |
|---|---|---|
| NW `Overworld.2.5.0` | Stump tier 4, Foothills, CascadeGorge | Pink-grey worn tepui stone, east-stepping low shelves, cold spray pools in the actual generated gorge, damp moss and small grouped pale flowers. The gorge and pool positions come from the snapshot. CascadeFather's back-borne young and glasspane-frog form are distinct model details, not extra static population. |
| N `Overworld.3.5.0` | Stump tier 5, Slopes, bespoke Felling-Site | A monumental petrified trunk/root circle, pale/cyan stems, red branching growth, pink fungi and real native water/approaches. Honor all 55 component layers (39 mutable), six bare strike positions and the seventh position at (40,8). This is a named authored-owner adapter, not the generic Slopes terrain generator. |
| NE `Overworld.4.5.0` | Stump tier 4, Slopes, ButtressRidge | Broad pink stone fingers radiating south from the north edge, walkable gaps, thin pale tepuibone seams, fungal pockets. The formation uses actual GrainRidge entities; preserve the mountain's east–west material grain even as ridge paths fan. Baseline art must not populate SariSnake/SkySari when UrquActive is false. |
| W `Overworld.2.6.0` | Grovelands tier 3, CompostingField | Warm damp soil, deliberate interrupted compost rows, lace-like cloth/old boot/chitin silhouettes, pockets of red/violet growth, soft canopy frame. Rows and harvestable CompostCache objects are distinct native entities; spent caches disappear/change only with native state. |
| E `Overworld.4.6.0` | Grovelands tier 4, Olderdeep surface mouth; FoundingVillage profile below | Mature inward-leaning canopy, wet rounded mouth lip and the true east descent gap, Grove columns/seep/sign wherever current generation left them. Restrained warm/white mycelium gives this reference a quiet ancestral character; no surface founding-village houses or Rooted character. |
| SW `Overworld.2.7.0` | Grovelands tier 3, Ginmere surface mouth | A distinct cooler, denser canopy composition around the actual sinkhole lip, softer violet/green growth and damp stone; use only real water cells. Reuse the lip kit but vary actual surrounding growth and lighting palette within native biome/light constraints. Do not invent a moat from the name Ginmere. |
| S `Overworld.3.7.0` | Grovelands tier 3, TendrilFen | Two braided water-coating lanes, violet/ochre fruiting shelves and wine-dark sundews, pale filaments, guaranteed native ChoirTendril residents. Plant clusters yield to the actual wet/open-cell masks; the reference must show a braid rather than one decorative creek. |
| SE `Overworld.4.7.0` | Grovelands tier 3, Grove | Gapped pale mycelial columns around the true clean seep and grown east-entry sign, red growth at real edges, generous readable open floor. The source law “everything is path” means attractive foliage cannot visually seal native openings. |

The native survey found all eight within Manhattan distance two of Morrowfast, excluding random *world POI* camp/lair placement there. This does **not** exclude ambient `LandmarkBuilder` stamps, hazards, loose containers, haulables, injected entities or dropped items. Those require the generated snapshot inventory and runtime fallback coverage below.

## 3. Reusable asset families and bounded new art

Reuse the accepted village's modelling/export foundations: rounded-stone functions, grouped opaque leaf crowns, granular palette painting, closed low-polygon foliage, water group, consolidation on export, tested units/axes, master-library collections, explicit FBX files and semantic rebuild audit. The source file currently executes the entire village on import; first extract only the needed factories/export utilities into an import-safe shared module, with no module-level world generation. Keep a regression rebuild of the village before adopting that extraction.

| Family | Bounded kit | Ownership / variability rule |
|---|---|---|
| Existing generic dressing | `tree-0..3`, `shrub-0..3`, `rocks-0..3`, `grass-0..3`; appropriate barrels/crates/rope/bench/cart parts | Reference existing prefabs/shared meshes, not eight duplicate FBX exports. Native Tree/Bush/destructible rocks get independent entity views. Tiny noninteractive moss/flowers may be combined per patch only inside approved native ground masks. |
| General terrain | Four moss/earth ground treatments and four pink-grey stone ground treatments; low soil/wet edge detail | Shared meshes/materials; positions from current cells. Ground can be patch-combined, but cell classification survives for dirty updates. No one 80×25 flattened world mesh. |
| Blocking vegetation | Four VineWall/root-wall silhouettes, four native tree silhouettes | Real blocking entities decide occupied cells. Neighbor-aware caps/corners must not bridge a walkable gap; overhanging leaves may be reduced/hidden where they obscure an actor or entrance. |
| Stump stone | Four TepuiWall outcrops, four GrainRidge segments, three walkable DescentLedge shelves, three small SprayPool edge/ripple arrangements | Reuse one grain direction and material family. An entity's current presence decides each solid ridge/outcrop. Do not elevate a walkable ledge into a visual wall. |
| Mineral/harvest accents | Three TepuiboneVein arrangements; three ChoirIronVein arrangements | Preserve exact blueprint identity and harvesting/removal; no static replacement ore after collection. Distinct pale bone vs dark ringing ore silhouettes. |
| Grove core | Three or four MycelialColumn silhouettes, three FruitingBody shelves, three GroveRedGrowth clumps; one legible grown GroveSign; three small GroveSeep rims | Four repeated variants, not one tiled mushroom. The actual column LightSourcePart feeds the existing native light map; a decorative emissive model does not grant gameplay light. The sign text remains authoritative in native inspect UI. |
| Compost | Four row pieces, three distinguishable cache pieces | Mesh parts communicate cloth/boot/chitin/soil; no speculative usable loot. Keep cache model separate from the underlying row for harvest state. |
| Sinkhole mouth | Four wet lip segments, three inward-root clusters, real stairs/descent entrance model, subdued interior basin/floor treatment pending native void review | Assemble from current lip/stair entities and approved native footprint policy. Never close the native east gap or use temporary GenReservedCells as saved geometry. |
| Water/hazards | Three low surface edge/ripple pieces per necessary family; shared native-state overlay geometry | Water, brine, tar, peat, ash, steam and dry-brush appearances must remain distinguishable. Pick only actual generated hazard types; do not recast every liquid as scenic water. Clouds/heat/residues need a real layered view or preserved native overlay. |
| Named Felling components | Petrified trunk + root structures as unique hero models; four red-branch variants, four cyan-stem variants, three pink fungi, three pale-column/tube variants, two rosettes; reuse rocks where appropriate | Bind all actual Felling component IDs independently, including all 39 mutable pieces and seven landmark states. Shared family prototypes are instanced under those owner IDs; do not flatten the circle. Waterfall/mist require their own FOV-safe, bounded effects. |
| Ambient structures | Reuse wood/stone/container props; add only missing exact blueprint models found in actual stamp inventory | Grovelands can stamp GroveShrine, MendleafGarden and HuntersBlind; Stump currently uses the Cave stamp catalog. The nearby-world-POI exclusion does not make these impossible. Sign/altar/fire/container content remains native and independently updateable. |

Do not promise an exact final model count before the actual eight-zone blueprint inventory, hazards and ambient stamps are extracted. A reasonable first production slice is two reusable terrain families plus roughly 40–60 new environment prototypes, with Felling hero/components and creatures budgeted separately. The number is a planning range, not authorization to invent content or a completion claim.

## 4. Creature models: share skeleton families, preserve species identity

The first-pass population source union (before optional landmark spawns) is:

- Foothills: CascadeFather, GlasspaneFrog, YellowfootWayfarer, Wardline, MawToad. CascadeFather forbids EcologyDamaged.
- Slopes: Wardline, YellowfootWayfarer, MawToad; SariSnake and SkySari require UrquActive.
- Grovelands tier 3 table (also used by tier 4 routing): HelmwoodFrog, Shambler, Mosshulk, WineLeafSundew, Rotling, ChoirTendril. TendrilFen additionally seats ChoirTendril through the formation builder. Habitat filtering still determines whether a table candidate actually appears.

Use five bounded articulation families: amphibian (four distinct silhouettes/material identities for the three named frogs plus MawToad), tortoise (refine the existing tortoise model for YellowfootWayfarer), segmented serpent (Wardline and SariSnake have visibly different slim/heavy profiles), avian (SkySari), and fungal bodies (separate shambler/rotling/mosshulk scales/proportions plus rooted sundew/tendril). Do not represent all of them with village hoods, or recolor one frog for every species without shape differences. Read full canon/examine content before sculpting each unclear fungal form.

Each shipped creature gets actual mesh/FBX art. Shared simple rigs may use Idle/Move/Attack/Hit where meaningful; rooted plant/tendril movement should not imply locomotion that its native Brain does not have. Animation responds to native confirmed movement/action hooks, not root motion or new AI. Humanoid landmark residents may share the village rig/equipment sockets when their native anatomy supports them. Species identity is one model contract per real blueprint; 3–4 shape variants are for repeated scenery, not an excuse to create fake creature types.

For optional creatures, injected/summoned entities, corpses, dropped equipment and future content, maintain the existing native sprite/glyph representation under the same XY↔XZ projection until a real mesh exists. World entities cannot disappear behind the 3D composite with only a diagnostic counter. Register a visible fallback plus actor/item/blueprint/reason diagnostics, with native inventory and inspect data remaining authoritative.

## 5. Native-state scene adapter and source snapshot contract

Proposed names below are **new interfaces**, not shipped APIs. Reuse actual APIs (`Zone.GetCell`, `GetReadOnlyEntities`, `GetEntityCell`, `EntityVersion`, `TileState.Get`, `ZoneRenderHooks`, `EntityVisualHooks`, current LightMap) through an adapter.

A read-only native exporter captures one active/cached zone after native generation or load:

```text
ZoneArtSnapshot
  schema/version, zoneID, worldSeed (provenance), native generation/save provenance
  biome/tier/Stump band, formation choice, current world flags
  cells[80×25]: x,y; terrain/entity references; solid/opaque flags;
                 complete coatings/residues/heat/cold/charge/cloud state
  entities: stable saved ID, exact blueprint, position, Render visibility/layer,
            known native owner/component IDs and multi-cell footprints,
            relevant native harvest/destruction/door/crop/animation-state selectors
  connections: actual source/target zone+cell+type
  native named scene definition identity/revision, when applicable
```

Export rendered appearance inputs only; no inventory payloads, stats or unrelated private state are needed for scenery. The export must not advance turns, consume RNG, write visited/explored flags, or apply spawn filters itself. For a prepared reference scene, include the real snapshot hash and mark any FullReveal preview as art-only.

Runtime uses an independent catalog mapping exact blueprint/native scene component/terrain semantics to a model family and explicit state selector. For generated terrain, build 5×5 or similarly bounded render patches from *current* cell state; for any mutable entity, keep a reference-keyed owned view independent from static dressing. Use dirty-cell/owner notifications and cached versions to update only affected views. A broad EntityVersion alone is insufficient for harvest state, mutable part state and tile coatings; full native Refresh remains a correctness fallback until exact dirty contracts are proven.

Choose cosmetic variation by a documented stable hash: static ground uses zoneID+worldSeed+cell+family+art-version; persistent mobile entities use saved entity ID+family so they keep their appearance while moving and across save/load. Do not use current position, Unity instance ID, runtime object hash or gameplay RNG for mobile appearance. A regenerated new entity may legitimately get a new appearance; reproducibility claims must identify whether the comparison uses the same saved snapshot or a new generation.

Retain the proven coordinate contract `(x+.5,height,25-y-.5)` and square one-unit cells. Full 80×25 terrain is required in every chunk. Runtime camera follows the actual gameplay viewport; eight pretty 37.5×25 cropped scenes cannot substitute for complete worlds. An image reference can focus on a native landmark, but source maps/Blender scenes also show the complete zone and border transitions.

For Felling, use the actual `FellingSceneDefinition`/`FellingSceneRuntime.FindOwner`, not the Morrowfast owner loader. Its art source transform (32 pixels/cell, originX16, groundOffset224) differs from Morrowfast (40.96 pixels/cell, originX21.25). Native cell anchors/footprints are the conversion authority; do not reuse source-image offsets blindly.

## 6. Artist pipeline and deliverables

1. **Finish source survey and native snapshot proof.** Capture at least three explicit seeds, a loaded save, an altered zone (harvested/destroyed/moved objects), and UrquActive/EcologyDamaged counter-states. Catalog all actual and optional blueprint families. Decide the sinkhole floor/persistence questions with native tests before promising visual semantics.
2. **Produce eight distinct ImageGen references.** Load the imagegen skill when executing this later step. Each request uses the accepted village image for style and its own verified chunk brief/native schematic for composition. Preserve reference PNG, prompt, receipt/hash and native snapshot ID per chunk. A concept image never overwrites a generated layout.
3. **Author a shared editable kit.** Extract import-safe geometry/palette/export helpers; preserve the village rebuild contracts. Store shared kit `.blend` and explicit FBX assets once. Reuse existing material/prefab references. Add a ring palette only if needed, with explicit palette-family metadata/import/render mapping; do not overwrite village palette indices.
4. **Assemble one editable `.blend` per chunk from its native snapshot.** Include visible reusable collections, individual landmark/owner roots, terrain patches, exact reference image as non-rendered authoring guide, camera, lights, and distinct preview renders. Embed/link shared geometry portably; document dependencies. Keep roofs, harvestables, mutable Felling components, actors and stairs separate.
5. **Export library plus scene manifests.** Each chunk also gets an actual `scene-preview.fbx` for editor/art inspection: separate terrain-patch, landmark and entity roots, never one flattened mesh. It depicts the recorded sample snapshot and is not a runtime replacement for generated/saved state. Shared runtime FBX models have stable IDs/GUID-preserving paths. Per-zone manifests describe example-scene/snapshot provenance and named-asset policies; they never act as saved runtime object graphs. Include full bounds/UV/material/rig/clip validation and relative dependencies. No baked reference-background quads.
6. **Integrate in bounded slices.** Prove one seed-varying Grovelands chunk and one Stump chunk first, including load/mutation/fallback. Then mouth adapters, then the Felling authored-owner adapter, then the remaining ring themes. Keep the original presentation available for any zone/catalog that is not fully ready.
7. **Native acceptance and closure.** Compare each final Unity chunk to its own reference and Blender render at actual game size, with ordinary FOV/light and all UI/input effects. Rebuild/reimport and profile before broad activation. Update living docs with exact differences and today’s implemented art/mechanics/fixes.

Suggested source layout:

```text
ArtSource/World3DRing/
  shared/{kit.blend,mesh_kit.py,export_fbx.py,model-catalog.json,textures/}
  refs/{nw-cascade,n-felling,ne-buttress,w-compost,e-olderdeep,sw-ginmere,s-fen,se-grove}/
    reference.png, prompt.txt, receipt.json
  snapshots/<run-id>/<zone-id>.json
  scenes/<zone-id>/{scene.blend,scene-preview.fbx,build_scene.py,manifest.json,renders/,reports/}
  exports/{shared-models/,landmark-models/,textures/,catalog.json}
```

## 7. Performance, tests and self-review gates

The village crop already estimates 328,705 source triangles; a dense procedural forest can greatly exceed it if every Tree gets the highest-detail crown. Treat the actual generated population as the budget, not a single pretty sample. Provide 3–4 artistic variants and at least a separate simple low-detail mesh for high-repeat tree/vine-wall/stone families; variants and detail levels solve different problems. Cache shared meshes/materials, combine only immutable patch decoration, and keep mutable entities individually addressable. Start with opaque foliage, one palette material per family and one water group; avoid one Renderer per leaf, one Material per actor or eight copies of the same atlas.

Do not assume dynamic batching/instancing succeeds with arbitrary material property blocks. Measure native draw calls/triangles/shadow passes/allocations/load times and matched 75-second frame profiles. Cap or simplify cosmetic scatter without removing native blockers, loot, creatures or interactables. Visibility and shadow masks remain per actual cell and transient owner; decorative lights/mist cannot reveal hidden content.

RED-first behavior tests and flipped-condition controls must cover: changing seed changes actual placements without stale fixed art; identical snapshot rebuild is semantically stable; movement preserves entity variant; equal-blueprint instances stay distinct; save/load rebinds new references; destroyed/harvested objects stay absent; hidden entities cast no visible shadows/pick hits; cell-state water exists without pool entities; loaded missing coating is not regenerated by art; native mouth gap/zone-edge transitions stay open; Felling's 39 mutable components/seventh state survive load/removal; unmapped live entities remain visibly represented; palette family and instance material isolation; missing bundle restores native presentation; repeated zone changes dispose all owned objects/hooks.

Add 6–12 real player-flow hypotheses after implementation: leave Morrowfast by all four edges and walk diagonally through adjacent chunks; find the fen braid, talk to its tendril, harvest a red growth or ore, inspect/descend both real mouths, change UrquActive and observe only native arrivals, return to a modified chunk after save/load, use combat/targeting/equipment with an unmapped dropped item, and compare low/full detail without loss of information. These are planned probes, not runtime results.

Self-review: 🟡 corrected hardcoded-village reuse assumptions, entity-only water extraction, humanoid creature guesses, future/hidden underground content and generation-reservation misuse. 🧪 pending actual generated/loaded seed survey, tile-state persistence outcome, mouth-floor semantics, native rendering/performance and completed ImageGen/art assets. ⚪ eight surface neighbors only; Olderdeep/Ginmere lower floors and a whole-world 3D conversion remain separate scope. No ring art is being built from this strategy until root confirms the verified scope.
