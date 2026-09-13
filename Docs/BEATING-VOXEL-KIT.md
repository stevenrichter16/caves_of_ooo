# Beating voxel art kit

Status: implemented and installed; final BT14 verification passes all60 art cases and all120 Beating feature cases. The full suite retains exactly32 previously recorded failures, with no new failures. CoO-original visual art, not a Qud source port.

The Beating is sun-exposed old country. Wide quiet sand, pale salt, weathered stone remnants and sparse roadside remains should read before detail. The generator supplies twelve single-cell families with four variants each. It produces reusable mesh/prefab assets; no manually positioned demonstration scenery. Every object uses one renderer, at most two constant ring-palette UV swatches, and at most ten combined boxes. Native entities retain all gameplay ownership.

| Family | Native alias | Design |
|---|---|---|
| sand | Sand | Continuous muted ochre base; one color. |
| pan | Contextual Sand recipe in SaltPan only | Continuous pale salt surface; no global blueprint alias. |
| road | RoadStone, SandstoneFloor | Quiet worn gray-brown route/interior floor; no repeated cobblestone noise. |
| crust | SaltCrust | Low fractured pale plates, below 0.3 cells. |
| dune | DuneCrest | Broad asymmetric stepped slope with flank/shoulder/crest/peak grades from 0.54 to 1.58 cells; muted warm stone and sand. |
| ruin | SandstoneWall | Broken russet-gray wall fragment, 1.5–2.3 cells high. |
| vein | PaleSaltVein | Pale upright salt crystal faces on dark host stone, 1.1–1.7 cells high; distinct from flat pan. |
| brine | BrinePool | Continuous blue-green surface with no decorative rim or fixed coating. |
| bones | Bones | Small bleached rib-and-skull cluster, not a claim about the remains' species. |
| sign | Signpost | Coarse weathered directional waypost, 1.6–2.0 cells high. |
| rubble | Rubble | Low broken sandstone blocks sharing the ruin palette; wall wreckage stays modeled. |
| briar | Saltbriar | Sparse angular gray branches and few salt-rimed leaf masses; native harvesting remains visible. |

Rock and DryBrush keep their existing art. Other ambient buildings, wells and actors remain outside this kit. The tenth fire and other authored places are excluded by the parent wilderness system, not inferred by the asset library.

## Verification sweep

| Reference | Confirmed contract / correction |
|---|---|
| CLAUDE.md | Tests precede production, plan and reference sweep precede authoring, generator fixes precede manual scene edits, native review and verification gates remain mandatory. |
| SoddenVoxelLibrary / KitBuilder / KitTests | Additive asset pattern supports strict identifiers, material/prefab/metadata checks and source-preserving corruption controls. Combined cube meshes use the existing WorldMaterial. |
| SpawnRing3DCatalog.Model | Three floor families use kind=ground; all other families use kind=entity. Bounds, triangle count, empty rig/clip/socket metadata and asset path are checked. |
| Objects.json | Exact aliases verified. SandstoneWall destruction yields Rubble; Saltbriar harvests SaltbriarSprig and has HP5. DuneCrest and Signpost are solid. |
| BrinePool blueprint | Uses LiquidPool brine and a four-turn renewable TileStateSource coating. The old formation comment's permanent-coating claim is stale; art must not make liquid permanent. |
| Lore/History/03_History.md | Beating ruins are archaeological evidence of what was built, not testimony explaining what it meant. Art contains no invented explanatory inscription. |
| Saltbriar blueprint | Describes gray briar with salt-rimed leaves, so art uses sparse gray branches and a few pale leaf masses rather than a lush green bush. |
| SpawnRingPalette.png | Constant sampled UVs preserve a quiet color per block despite the underlying textured atlas. Ground/water variants differ only below their shared exposed top, preventing cell checkerboards. |

## Tests and performance

Sixty art cases cover exact 48-entry coverage, four distinct meshes per family, two-swatches maximum, one-cell bounds, no gameplay scripts/colliders, one renderer, matching metadata and prefab references, quiet contiguous surfaces, readable height hierarchy, contextual pan mapping, strict invalid-input rejection and twelve independent corruption controls. Corruption fixtures clone the library and revalidate the untouched source.

All meshes are authored offline. Runtime model identifiers are precomputed, known-family lookups allocate nothing, and the fixed dictionary is populated only by explicit validation. No per-frame geometry generation or per-cube GameObjects. The parent integration borrows source meshes through native dirty-patch batching and owns traversal, mutation, renderer, regression and visual gates.

## Reproduction and review

Build entry point: `CavesOfOoo.Editor.BeatingVoxelKitBuilder.Run`, only inside a disposable/isolated Unity project. It writes `Assets/Resources/BeatingVoxel3D`, preserves existing generated GUIDs, and never saves a scene. Parent camera renders check silhouettes at gameplay pitch using whole-chunk overviews; live gameplay zoom/lightmap feel remains a separate look pass. Numerical tests prove geometry and references, not visual quality or play feel.

## Implementation log

- BT02-art-red: parent confirmed explicit missing BeatingVoxelLibrary compile failures with no production library present.
- Added strict library and offline combined-mesh builder for 48 models. All aliases remain exact; `pan` is available only through explicit contextual model selection, not a fictitious SaltPan blueprint.
- Sand, pan, road and brine use one box each and one constant swatch. Dunes and mineral veins use four boxes, ruins six, crust six, bones ten, signs five, rubble four, briars seven. Maximum geometry is 240 vertices; all X/Z dimensions fit one native cell.
- Color vocabulary: muted sand 32, pan 19, road 64; initial dunes 14/21 (retired; final palette7/32); ruins and rubble 12/76; crystals 12/26; brine 25; bones 19/10; wood 15/11; saltbriar 12/19. No per-object random shade noise is introduced.
- Source-side review checked alias symmetry, exact ground-kind assignment for three floor families, metadata/prefab/material matching, atomic cache rejection, single-cell bounds and finite offline allocations. Actual native-camera inspection and generated metadata collision audit remain parent gates.

- Parent built all 48 assets with zero C# compile errors before renderer integration. Added five renderer gate cases during the integration wave: exact repeated borrowing of all coarse meshes (zero replacements and zero missing mappings), settings/underground controls, and actual native presenter removal for walls, salt veins, brine and wayposts. Removal tests check local patch vertex reduction equals exactly the deleted owner's mesh, unchanged distant geometry/control identity, and borrowed source survival; an empty dirty set must still reconcile membership. Tests dispose the integration fixture and restore presentation settings.

## Dune and ruin refinement proposal

The BT06 DuneBelt-1729 native render shows uniformly tall bright gold courses that read as a wall. The parent generation pass will broaden ridges and taper their ends. The asset generator will supply four useful height grades, selected by the parent recipe from current native neighboring dune occupancy: low flank 0.4–0.65, shoulder 0.65–1.0, crest 1.1–1.4, peak 1.4–1.7 cells. All four retain four coarse boxes and two swatches. Proposed palette: muted warm stone 7=(154,144,109), plus the existing sand's 32=(189,156,109); bright gold 14/21 is retired from this family. Adjacent ruins also need a 0.98×0.98 lower foundation so vertical native wall runs do not show ground-level visual gaps.

Six new tests precede these source changes: four explicit height-band cases, two-swatch muted palette, and low foundation X/Z coverage measured only below y=0.3. The existing general dune height envelope expands to include flanks. The builder was held unchanged for parent RED confirmation; BT07 and the resulting changes are recorded below.

BT07 confirmed the intended art differences RED: flank, shoulder and crest heights, old bright palette, and narrow ruin foundation failed; the existing peak height was a passing control. The builder now uses heights 0.54/0.85/1.25/1.58 and palette 7/32 while retaining four boxes; ruin lower foundations now cover 0.98×0.98. Added a real presenter cross-patch height reconciliation gate. Source sweep corrected the proposed x15/16 boundary: the renderer uses 10×5-cell patches, so the test instead uses x19/20 and explicitly asserts distinct patch roots. Removing the right neighbor must update the surviving center from peak to crest and rebuild its patch with an empty dirty set, preserving distant model/mesh identities.

## Final dune seam simplification proposal

The BT09 native DuneBelt-1729 render still exposes a tiled lattice: tapering every individual model to widths 0.98/0.90/0.86 leaves repeated longitudinal grooves. Final proposed generator change: two full-X-span slabs per grade, retaining heights 0.54/0.85/1.25/1.58 and palette 7/32. The lower slab covers one complete native cell; the upper slab spans one cell east/west and 0.75 north/south, aligned toward +Z. Adjacent equal-grade cells then meet without artificial X gaps. Native occupancy and per-cell ownership remain unchanged.

Four new RED cases require every distinct horizontal vertex level to span exactly X=-0.5 to +0.5 without overhang. The earlier exact 96-vertex assertion becomes a <=96 geometry budget, permitting fewer blocks while retaining two swatches and all four height grades. Source builder is intentionally unchanged until the parent completes its current full run and confirms the new RED gate.

BT12 confirmed all four seam tests RED (observed left X=-0.49 instead of -0.5), with 56 other kit cases passing and zero compile errors. The builder now generates two stacked full-width slabs per dune grade: a 1×1 lower slab at 52% of total height, and a 1×0.75 upper slab aligned toward +Z. Each model drops from 96 to 48 vertices, while all four heights and both muted swatches remain unchanged. Parent rebuild, final native render and regression verification remain pending.


Final art state: dune geometry is two combined slabs (48vertices/model), with
four height grades selected from current native neighbors. BT12 supplied four
explicit seam failures before the final source edit. BT13 rebuilt the complete
48-entry library and rendered18 native-pipeline scenes, zero C# errors and zero
unmapped voxel sources. Independent inspection of the final dunes, brine lenses
and ruin streets found no remaining material visual issue in these static views.
The parent composition document records the final full regression and GUID audit.
