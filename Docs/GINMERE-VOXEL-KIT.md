# Ginmere voxel art kit

Status: complete and installed. Final targeted gate 396/396. Full suite 12,570 total / 12,538 passed / 32 unchanged baseline failures / zero C# errors. All 307 new feature cases pass; the suite also adds one surrounding equipment countercontrol.

Ginmere's sixty-four models form sixteen four-variant families. Broad gray strata enclose low dry shelves and continuous dark water. Small indigenous fauna and an expedition's ordinary supplies give the descent readable activity without disguising native travel or collision. The kit is reusable source generation, not a manually arranged preview scene.

| Family | Exact blueprint | Art rule |
|---|---|---|
| ground | SandstoneFloor | Full-cell quiet gray floor, fixed surface/swatches; buried variation only. |
| cliff | SandstoneWall | Two full-cell strata; heights1.50/1.70/1.90/2.10. |
| rim | SinkholeLip | Two full-cell strata; heights0.60/0.78/0.96/1.14. |
| ledge | DescentLedge | Broad low shelf below0.25cells, visually distinct from the blocking rim. |
| water | MirePool | One contiguous tea-dark blue-green plane with identical top height/color across variants. |
| anchor | RopeAnchor | Iron pin with a short knotted rope stub; no ladder or implied climb mechanism. |
| nest | PricklebrowNest | Low litter hollow with four coarse pale egg clusters. The native nest owns the count of sixteen eggs and defender activation. |
| gecko | PrickleBrowGecko | Indigo body, amber throat, long tail, four limbs and paired raised brow spines. |
| frog | GinFrog | Squat golden frog with wide hind legs and paired dark eyes. |
| torch | Torch | Small wooden shaft with a coarse warm head; native light/thermal/fuel parts remain responsible for behavior. |
| meat | DriedMeat | Low cured strips with a restrained lighter edge. |
| tonic | HealingTonic | Small green bottle with a cork, separate from its actual native healing effects. |
| frost | FrostVent | Low pale rime rim around a dark open aperture; no synthetic plume or liquid. |
| ice | IceSheet | Quiet full-cell pale blue-green sheet, fixed top at0.056cells; one swatch. |
| stalagmite | Stalagmite | Three broad narrowing stone tiers, tall enough to read as a solid obstacle. |
| cache | BoneCache | Low bone ribs and a coarse skull over a darker hollow; native five-item container. |

Only ground has `kind=ground`; all remaining assets are static `entity` meshes. The parent renderer keeps fauna and carried/dropped supplies transient. There are no rigs or animations claimed. Each prefab contains one combined mesh using the existing ring palette and no scripts or colliders. `GinmereVoxelLibrary` provides `Load`, `Validate`, `Find`, cached strict `ModelId(family,variant)`, exact `Family(blueprint)`, `VariantCount=4`, and resource path `GinmereVoxel3D/Library`.

The parent may reuse existing coarse assets for native vegetation, stairs, Sack, Bones and Rubble. Those are not invented aliases inside this sixteen-family library. Root integration verifies all ordinary native owners and dropped supplies, rather than relying only on missing-mesh counters.

## Verified corrections and boundaries

| Premise | Source and correction |
|---|---|
| Any water-colored scenery can represent the pool | `Objects.json` gives MirePool real LiquidPool, TileStateSource, Thermal, Destructible and BurnOffGas parts. SprayPool is descriptive scenery and is explicitly excluded. Removal must remove the water model. |
| The cleared rim interior is a mechanical abyss | Native review corrected this premise: the currently shipped interior remains walkable. The native refinement uses real SandstoneFloor owners there; no fake abyss/fall mechanic or renderer-only ground is introduced. |
| Rope anchors implement climbing | RopeAnchor is non-solid examinable scenery: iron pin plus a hand's length of old cut rope. No climb action ships. |
| The gecko name can be inferred | The exact blueprint is `PrickleBrowGecko`; `PricklebrowGecko` is rejected. Its description supplies indigo body, amber throat and brow spines. |
| GinFrog already has the bestiary's brood armor | The actual blueprint is passive golden fauna with4HP; it has no brood part. No babies, armor or sound behavior are implemented by this art. |
| Nest egg count requires noisy individual cubes | Native PricklebrowNest owns sixteen eggs/defenders. Four broad egg clusters provide the low-detail visual cue; geometry does not simulate, enumerate or spawn eggs. |
| Individual rounded wall cells form natural larger walls | Earlier Stump previews showed repeated narrow caps create a waffle pattern. Both Ginmere solid families use full X/Z strata; parent recipes may derive height grades from current visible cardinal neighbors. |

References read: `CLAUDE.md`, `Docs/OVERWRIT-GINMERE-COMPOSITION-PLAN.md`, `DrownedSimaBuilder`, `SinkholeMouthBuilder`, `SinkholeDescentBuilder`, all sixteen exact shipped blueprints, `sarisarinama_bestiary_design.md`'s Gin Frog/Pricklebrow entries, `Docs/PERF-FOUNDATION.md`, Stump kit source/tests, `SpawnRing3DCatalog.Model`, and existing palette samples. Implemented palette: ground12, cliff/rim/ledge12/13, water24, anchor12/11, nest10/19, gecko58/21, frog21/10, torch15/49, meat20/19, tonic48/11, frost125/12, ice125, stalagmite12/13, cache19/12. Shared palette files remain untouched.

## Gates, performance and self-review

RED specifications cover sixty-four complete entries, four distinct meshes per family, exact prefab/mesh/material metadata, one-cell bounds, ≤240 vertices, one renderer, at most two colors and no gameplay components. Twelve source-preserving corruption controls test failure paths. Readability checks require fixed quiet floor/water, full-X/Z solid strata, explicit height grades, low ledges, central pale nest clusters, long-tail/raised-brow gecko anatomy, broad-haunched frog anatomy, short rope anchor and distinct small supplies.

Build entry point is `CavesOfOoo.Editor.GinmereVoxelKitBuilder.Run`, writing `Assets/Resources/GinmereVoxel3D`. Meshes are authored offline, one combined asset per variant, without runtime cube GameObjects. Rebuild retains GUIDs and never saves a scene. Runtime IDs are precomputed, lookup follows the finite validated-library pattern and parent integration owns current-owner batching and native dirty reconciliation.

- 🔵 Library exclusions preserve exact species identity, descriptive-water distinctions and scoped blueprint authority.
- ⚪ Native sixteen-egg state is shown by coarse clusters rather than sixteen tiny independent cubes.
- ⚪ No new falling, climbing, brood armor, hidden-water passages or renderer-only floor is introduced.
- 🧪 Static anatomy/geometry checks do not prove live readability, animation feel, performance or travel. Parent native integration and previews remain pending.

## Implementation log

- 2026-09-15: Read methodology/plan, native builders, content and bestiary. Coordinated nine core families with the native agent and added three real expedition supply families at the parent's request. Wrote RED tests and metadata before production; awaiting parent RED.

- AR03: Parent confirmed missing-type RED for both new libraries (186 C# error lines including the independent native plan). Only then authored the strict library and offline combined-mesh builder. All six new script/test metadata files have distinct GUIDs; source geometry stays inside one cell and uses constant two-color-or-fewer palette samples. Parent owns build, native previews and Unity validation.

- AR08 generated the first48 models with zero C# errors. AR09 passes46/46 Ginmere art cases. Inspected ExpeditionTerraces-64 and DrownedBasin-64: continuous joined gray strata, intact dark basin and distinguishable small fauna. The native-owner census separately found unsupported cold hazards (FrostVent/IceSheet); initial clean art tests do not establish complete native coverage. Expansion contracts remain under review.

- AR10 native-owner census confirmed six unsupported identities before production: FrostVent, IceSheet, Stalagmite, BoneCache, SnapjawScavenger and SnapjawHunter. The root reuses the exact Snapjaw base body for its two native descendants, preserving their real glyph/loadout. After writing new exact-alias, shape and native-part contracts, appended four required new art families (sixteen models) at offsets48/52/56/60. The earlier48 geometry methods remain unchanged. FrostVent/IceSheet have native Thermal/TileStateSource but no LiquidPool or Destructible; Stalagmite is solid and BoneCache is a solid five-item container without Destructible. The art adds no parts. Rebuild and expanded GREEN are pending.

- Mouth-floor correction from the native reviewer: the cleared lip interior is currently walkable native terrain, not a shipped fall/abyss mechanism. After a separate native RED, the upcoming mouth refinement will use ordinary SandstoneFloor inside the lip. This already maps to the kit's ground family; no new blueprint or art family is necessary. The earlier no-synthetic-ground invariant still applies: art must follow that real floor owner and keep a removed owner absent. Native recipe/plan updates belong to the parent and native agent.

- AR12 rebuilt the expanded64-model Ginmere kit alongside Overwrit (80 total), zero C# errors. AR13 art/rendering run reports138 total,137 pass and only the unrelated intended Overwrit source-color RED; all four appended Ginmere families, native-owner census and runtime-POI veto cases pass. Final full regression and final-layout previews remain parent-owned.

- Final source/metadata cold-eye: checked every new family against exact native alias/height/ownership rules, connected limb/support geometry, one-cell bounds, two-color budget and strict library failure paths. No additional notable source finding remains after the growth/color corrections. Read-only audit of the AR12 isolated resource directory found64 meshes,64 prefabs and129 unique asset metadata entries, plus the unique folder metadata. Every prefab contains exactly one GameObject, Transform, MeshFilter and MeshRenderer; no extra component, collider, script or transform offset. All new resource/folder GUIDs occur once across isolated Assets. The original project resource export remains parent-owned. This audit precedes the final neutral-floor rebuild and does not claim final live visuals/performance.

## Final close-out

AR21 targeted:396/396. AR22 full:12,570 total,12,538 pass,32 failures whose
names and messages exactly match AR01; zero new failures and zero C# errors.
All307 new feature cases pass, plus one added surrounding equipment control.
Final AR18 native gallery contains21 views with zero missing meshes and zero
unmodeled visible owners. Main-project art is installed;405 source/art files
match the isolated copy and196 task metadata GUIDs have no collisions. Static
captures do not establish live input/HUD/lighting feel or sustained FPS. See
`OVERWRIT-GINMERE-COMPOSITION-PLAN.md` for the exact mixed-file commit boundary.
