# Voxel world: four native chunks

Status: four-chunk native integration implemented and verified, 2026-09-12. Branch: `codex/voxel-town-generator`. Toolkit changes are independently committable; native integration remains installed in the mixed working tree because its earlier 3D foundations are themselves untracked. See the preservation section below.

## Goal and scope

Continue the reusable Blender toolkit and bring its voxel visual language into four connected, playable native chunks: Morrowfast (`Overworld.3.6.0`), its western field (`Overworld.2.6.0`), the southern Stump foothills (`Overworld.3.7.0`), and the southeastern grove (`Overworld.4.7.0`). Preserve the native owners of collision, destruction, inventories, interactions, quests, movement, and saves. The user explicitly excludes save migrations; current-session saves and revisits still must retain mutations.

This is CoO-original presentation/generation work. Qud provides the user's visual/semantic reference, not a source-code parity claim. Reference: `ArtSource/VoxelTown/Reference/oasis-town.png` and the user's procedural toolkit specification, summarized in `Docs/VOXEL-TOWN-GENERATOR.md`.

## Verification sweep and corrections (before implementation)

| Assumption | Verified correction / consequence |
|---|---|
| A square Blender demo can be dropped into a game chunk. | Native `Zone` is 80 by 25 cells, one metre per cell; the demo is square. Add a rectangular semantic adapter; never squash it. |
| The configured starting zone is the actual new-game spawn. | `GameBootstrap.GenerateStartingZone` explicitly starts at Morrowfast 3.6, preferred (40,23). `WorldMap.StartingZoneID` remains Sill. Preserve bootstrap. |
| The south chunk is a generic wetland. | 3.7 is the shipped multi-cell Stump foothills pilot. Its native footprint owners and holes must remain authoritative. |
| Four chunks should be a 2 by 2 rectangle. | Nearby 4.6 and 2.7 contain named sinkholes. A connected west/south/southeast route gives four chunks without replacing those places. |
| A single building mesh can become one solid collision owner. | Interior floors would block interiors. Existing walls, doors, furnishings and footprint owners remain independent native objects. |
| A static voxel NPC is enough. | Existing presenters retain Generic skeletons, Idle/Walk/Attack/Hit/Interact clips, spell cast overrides and equipment sockets. Swap baked meshes while preserving hierarchy and bone contracts. |
| Runtime mesh conversion on refresh is acceptable. | Ground/FOV refresh is frequent. Bake mesh assets in the editor, cache catalogue lookup, borrow immutable assets at runtime. Ground batching resolves templates before combining. |
| Vertex colours will display correctly. | Existing native fog shaders use palette UVs, not vertex colour. Baked meshes retain material/submesh/UV contracts. |
| `Cell.Objects` includes every covered cell. | Multi-cell owners require `Cell.Occupants` / `Zone.GetOccupiedCells`; renderer changes must not replace simulation occupancy. |
| The old suite number is a current green baseline. | U00 baseline: 11,542 total, 11,510 pass, 32 fail, zero compiler errors. 29 equipment-sprite failures, one building-kit variant failure and two older world/place assertions precede this work. |

References read: `CLAUDE.md`; `Docs/PERF-FOUNDATION.md`; `ADVERSARIAL_TESTING.md`; `Zone.cs`; `WorldMapAuthoring.cs`; `GameBootstrap.cs`; `MorrowfastStartingGarden`; native village/ring/pilot libraries and presenters; `NativeZone3DRenderSurface.cs`; `SpawnRing3DGroundPatches.cs`; `Village3DEquipmentViews.cs`; `NativeSpellCastPlayer.cs`; existing native audit batch runners and `NativeSaveIsolation`.

## Content readiness

- 🟢 Existing native chunk ownership, quests, destruction, actor rigs, camera and fog contracts.
- 🟢 Toolkit M1: reusable semantic model, deterministic layout, voxel recipes, Blender export and safe regeneration.
- 🟢 Native rectangular adapter, neutral recipe export, 280 pure tests, 95 varied regions and four Blender scene gates.
- 🟢 Native catalogue: 406 voxel meshes, including 28 skins; 38 bindings use actual toolkit recipes. Four-zone hooks are installed and exercised.
- 🟢 Four-chunk native audit: 44 checks, six border crossings, destruction/revisit, native save/reload, equipment, casting and seven screenshots. All 167 new voxel EditMode cases pass.
- 🧪 Runtime profiling is measured, with long-frame outliers explicitly retained; this is not a hitch-free performance acceptance.
- ⚪ No replacement of Morrowfast's canonical layout or residents. New procedural layout rules produce reusable candidates; they do not silently overwrite a saved or authored settlement.

## Stages and acceptance gates

1. Extend the toolkit with deterministic rectangular native chunk data and shared route portals. RED → GREEN pure tests, multiple seeds, Blender inspection; fix rules, not one scene.
2. Bake native models into reusable voxel meshes. Preserve UV/material slots, silhouette, skin weights/bind poses, sockets and original asset identity. Tests cover closed/open/thin geometry, invalid inputs, disconnected components, repeated application and original-asset immutability.
3. Enable the catalogue in exactly the four native chunks, before model preparation and ground batching. Retain existing actors, equipment, spell animation and picking. Unknown models retain their native fallback and are reported; never generate gameplay objects during a rendering refresh.
4. Native Unity acceptance: ordinary new game, all four chunks/crossings, native mutation and revisit, multi-cell selection/destruction and duplicate damage, fog/lighting, equipment and cast clips, rendered captures and bounded runtime measurements. Compare the full suite against the recorded baseline.
5. Cold-eye review (ownership/lifecycle, symmetry, counter-check completeness, docs/code agreement), hypothesis-driven adversarial tests, fixes, living docs and scoped commits.

## Performance and diagnostics contract

No per-cube GameObjects, runtime voxelization, or geometry rebuild on a pure fog refresh. Generated meshes are shared assets; existing 10 by 5-cell ground patches remain batched. Each owned model conversion reports its replaced and unresolved source count to the audit; catalogue validation reports rejected assets before presentation. Runtime capture records drawables/vertices, ground rebuild stability, frame statistics and cleanup. User-facing performance claims require actual Unity samples, not Blender timing.

## Implementation log

- 2026-09-12: read rules and native contracts, captured pre-existing work hashes/status in `Docs/Verification/VoxelWorld/prechange.json`. Closed clean idle Unity before running the documented MCP restart/settle sequence. Baseline receipt: `Docs/Verification/VoxelWorld/U00-baseline-launch/receipt.json` (32 pre-existing failures). No game code changed before that run.
- 2026-09-12: split independent toolkit, mesh baking and native-audit work across three agents; root owns presentation wiring and integration review.
- 2026-09-12: extended the generator with native rectangular candidates and 59 recipes. Four crate and barrel variants differ geometrically. Reduced broad paths, clustered activity clutter, improved wet margins and role-specific proportions through generation rules. Pure suite grew from 209 to 280; all pass. Final seed/size sweep passes 95/95, and real Blender validates all four candidate scenes.
- 2026-09-12: U02 confirmed 35 initial missing-implementation failures before adding the native mesh catalog/presenter adapter. U04 passed those 35 and exposed missing importer/audit contracts. U05 and U07 exposed real mesh/key/import validation bugs; U08 passed all 148 then-present voxel cases. Compiler-only fixture corrections remain documented in their raw receipts and are not represented as observed behavior failures.
- 2026-09-12: baked 406 meshes from 392 native prefabs, retaining 28 animated skins. The initial mesh bake reduces 621,017 source triangles to 475,994 voxel triangles before the 38 toolkit overrides; these are asset counts, not a measured frame-rate improvement. Native actors retain their rigs, clips and equipment sockets.
- 2026-09-12: U09 regeneration rejected valid earlier outputs because Unity normalizes mesh names to filenames. Replaced the ownership heuristic with explicit importer metadata. U10 successfully regenerated all 38 toolkit substitutions, retaining their GUIDs and source dependencies.
- 2026-09-12: U11 completed the native audit with frozen runtime sources, 44 passing gates, seven real GameView captures and 23,894 recorded frames. An independent receipt inspector verified artifact hashes, checks, exact save snapshots and raw performance calculations.
- 2026-09-12: U12 full regression exposed ten old exact-mesh expectations plus 13 intentionally RED pitch-preflight cases. Updated expected mesh identity only within the enabled region, retaining exact socket/material/ownership checks and adding an outside-region control. Validated pitch conversion during preparation before publication. One unrelated unseeded Glowmaw failure passed its isolated U13 rerun without code changes.
- 2026-09-12: U14 reimported the final recipe export. Runtime source/art hashes still exactly match the successful U11 native workload; the final export differs from U11 only in an unused wall recipe. U15 full regression: 11,710 total, 11,678 pass, 32 fail, zero compiler errors. All 32 failing names and messages match U00 exactly; all 167 voxel cases and the added outside-region case pass.

## Installed behavior and how to play

The existing native game opens normally in Unity. With its 3D presentation enabled, a new game starts at Morrowfast (3,6), which now uses voxel meshes. Travel west to (2,6), or south to the Stump foothills (3,7), then east to the grove (4,7). The camera angle, fog rules and canonical starting layout remain the current game's settings. Existing saves in these zones use the same visual adapter when bound.

`VoxelWorldPresentation.ForZone` creates one adapter for a supported zone. Presenters call `Apply` on owned model instances before creating selection colliders; ground patches resolve borrowed templates before batching. The adapter preserves materials, native owners, transforms, bones and sockets. Reapplying it is idempotent. Unknown source meshes retain their exact original references and are counted diagnostically; none were missing in the four audited chunk binds.

This is a native voxel **presentation** integration over the existing cell-based simulation. Native walls, doors, props, actors and multi-cell objects retain their existing interaction/destruction contracts. It does not make each rendered sub-cell voxel individually mineable or introduce vertical voxel physics. The toolkit's four `candidateOnly:true` procedural plans remain editable exports; they do not overwrite Morrowfast's authored quests or the southern pilot's owners. Terrain and structures use baked native voxel geometry, while 38 compatible prop bindings use 19 unique toolkit recipes directly. Actor meshes use the offline voxel bake to retain their animation rigs.

### Regeneration and repeatable gates

From the repository root, with Unity closed for the batch runners:

```sh
PYTHONPATH=ArtSource/VoxelTown python3 -m town_generator.native_generate \
  --seed 41 --output ArtSource/VoxelTown/Output/native-region
python3 Tools/VoxelWorld/build_assets.py my-voxel-rebuild
python3 Tools/VoxelWorld/run_editmode.py my-voxel-tests --filter CavesOfOoo.Tests.VoxelWorld
python3 Tools/VoxelWorld/run_native.py my-voxel-native --execute
```

Use unique receipt names. The native runner performs a save-isolated, visible Unity workload, restores the original scene/preferences/saves, and closes its own Editor. Omit `--execute` to inspect its plan. The full regression uses `run_editmode.py` without a filter. `build_assets.py --recipes-only` can update recipe bindings without repeating the source-mesh bake. See the toolkit README for Blender render commands and all seed/configuration controls.

## Verification receipts

| Gate | Result / evidence |
|---|---|
| Toolkit pure suite | 280/280, `Verification/VoxelTown/native-23-final-suite.log` |
| Seed/size sweep | 95/95, `Verification/VoxelTown/native-region-24-final-multiseed.log` |
| Actual Blender scenes | Four valid scenes, safe regeneration and camera bounds; `Verification/VoxelTown/native-blender-25-final-probe.log` and `native-blender-27-final-renders.log` |
| Native integration | `Verification/VoxelWorld/U11-native-first/independent-inspection.json`: 44 distinct checks, 7 screenshots, 6 native keyboard border crossings |
| Final full regression | `Verification/VoxelWorld/U15-final-full-regression/receipt.json`: 11,678 pass / 32 existing failures, zero C# errors |
| Audited runtime equals final runtime | `Verification/VoxelWorld/U14-final-recipes/native-equivalence.json`: no changed runtime files |
| Existing work preservation | `Verification/VoxelWorld/final-preservation.json`: 385/390 monitored files unchanged; exactly 5 intentional integration/test edits |
| Asset GUIDs | `Verification/VoxelWorld/final-guid-audit.json`: all 5,026 project metas have distinct valid GUIDs; all 38 toolkit ownership tags and GUID pins match |

Native bind counts: Morrowfast 193 swapped meshes, western field 34, southern foothills 156, grove 45. Every bind was active with zero missing mappings, a visible player, and normal fog (`fullReveal:false`). These counters describe owned instances visited by the bind, not every cell becoming visible.

## Self-review and honesty bounds

Cold-eye and adversarial review are complete for this implementation. Dedicated suites probe mesh topology/ownership, importer boundaries, presentation lifecycle and audit false-positive resistance. Findings were fixed and their failing cases retained:

- 🟡 Fixed reversed-winding voxel boundary inflation, unsafe/duplicate output keys, skin/bindpose mismatches, malformed/nonplanar/self-crossing quads, coerced topology indices and non-affine transforms.
- 🟡 Fixed destination preflight ordering, generated-output ownership on regeneration, and zero/nonfinite/overflowing voxel-pitch metadata before publication. The 13 pitch tests are GREEN in U15.
- 🔵 Preserved exact native source identity outside the enabled region; inside it, equipment assertions require the exact catalog replacement rather than accepting arbitrary geometry. Double preparation, disable/rebind and unknown-mesh fallback have counterchecks.
- 🔵 Scene/render source changes do not touch simulation RNG, occupants, targeting, inventories, actor AI, quest placement or save formats. Multi-cell destruction/damage dedup and native hauling remain exercised through the existing systems.
- 🧪 Visual bounds: actual GameView captures retain the current CRT/pixelation/color grading and unexplored black fog. Four Blender candidate renders show the generator, not installed native layouts. Reference-level architectural wear, irregular/multi-room buildings and broader environment families remain future generator work.
- 🧪 Performance bounds: U11 measures only the southern pilot's pristine/restored idle and walking phases, 20 seconds each, on this Mac/Metal Editor. Engine-frame p95 is approximately 4.57–6.05 ms, but phase maxima reach 22.8–684.2 ms, with a 58.83 MB peak allocation. Earlier native pilot recordings also contain large hitches, but they have different cache state and are not a controlled comparison. No frame-budget or hitch-free claim is made; render-thread timing was unavailable.
- ⚪ The 32 pre-existing full-suite failures remain: 29 equipment-sprite cases, one earlier building-kit variation case and two old place/world-map assumptions. Their names and messages are unchanged from the baseline.

Can verify: compiled contracts, four native binds, exact keyboard seam destinations, damage/removal/dedup, source immutability, native F5/F6 snapshot equality, resource counters and recorded render frames. Border tests explicitly position the actor at prepared edges before issuing the ordinary key; they are not a complete player-led traversal of every intervening cell. Cannot verify from these bounded gates: every possible future seed/asset, subjective long-session comfort, a GPU-independent performance target, or complete visual parity with the reference.

## Preservation and commit scope

The earlier native 3D presenters, their FBXs/libraries and many existing tests are pre-existing untracked work. Committing only the new dependent C# files would make an incomplete clean-checkout feature; committing their dependencies would absorb the user's unrelated work. Therefore the independent toolkit extension is the scoped commit, while the installed native integration and its generated assets remain in the working tree. This is a version-control boundary, not a disabled integration.

`Verification/VoxelWorld/native-integration-hunks.patch` records only the five small changes to those existing files. `prechange.json` and `final-preservation.json` retain hashes for the 390 monitored pre-existing C#/ProjectSettings files. Three changes wire the presentation adapter; two update exact equipment-mesh expectations. The rest of that monitored set is unchanged. No global staging, reset, stash, source-model rewrite or save migration was performed. Full raw Unity receipts remain local beside the integration; the compact report identifies their exact locations.

## Files changed

- Toolkit: `ArtSource/VoxelTown/town_generator/{assets,native_assets,native_region,native_scene,native_generate}.py`, six native test/probe files, `native-bindings.json`, README, final native-region JSON/Blender/previews and `Docs/VOXEL-TOWN-GENERATOR.md`.
- New runtime: `Assets/Scripts/Presentation/Rendering/VoxelWorldMeshCatalog.cs`, `VoxelWorldPresentation.cs`.
- New editor tooling: `Assets/Editor/Art/VoxelWorldMeshBuilder.cs`, `VoxelWorldToolkitImporter.cs`.
- New native audit: three `Assets/Scripts/Scenarios/Custom/VoxelWorldNativeAudit*.cs` files and `Assets/Editor/Scenarios/VoxelWorldNativeAuditBatch.cs`.
- New tests: nine `VoxelWorld*Tests.cs` files under `Assets/Tests/EditMode`, 167 cases in total; corresponding unique metas.
- Generated Unity content: `Assets/Art3D/VoxelWorld/{Meshes,Toolkit}` and `Assets/Resources/VoxelWorld/Library.asset`, with metas and ownership metadata.
- Surgical existing hooks/test changes: the five files listed in `final-preservation.json`; reviewable as `native-integration-hunks.patch`.
- Runners/review: four scripts in `Tools/VoxelWorld`, this plan, raw RED/GREEN/native/profile receipts and independent review documents under `Docs/Verification/VoxelWorld`.

## S1 — quieter voxel ground (2026-09-12, complete)

User feedback: preserve the successful voxel look, but simplify its visual noise a little. The actual spawn capture shows bright small flecks and many neighboring ground tones competing with characters and buildings. Scope is an offline paint simplification for explicit ambient terrain/detail models. Keep geometry, camera, gameplay owners, actor/equipment art, shared source textures and toolkit recipes intact.

Verification sweep: VillagePalette is an 8×8 atlas; SpawnRingPalette is 16×8. Baked faces already have constant UVs, but each samples a different textured point. One representative texel per swatch can remove this unnecessary variation without changing geometry. Ground-only highlight variants can resolve to their base grass/moss/cobble swatches. A source shared with an excluded material/use must remain unchanged. Pilot ground has its own material and is excluded from this palette-specific treatment. No global voxel-size or postprocessing change is needed.

Gate: RED tests for palette selection, strict ambient scope, swatch identity, idempotence and malformed inputs; implement; rebuild using the normal recipe pipeline; verify geometry/rigs/source dependencies/GUIDs and excluded mesh hashes; run voxel tests and matched native gameplay captures. Independent read-only reviewers agree on the bounded scope. Before-state hashes are in `Verification/VoxelWorld/S01-simplify/prechange.json`; final evidence and visual assessment follow below.

Implementation: `VoxelWorldPaintSimplifier` chooses the existing opaque texel nearest each atlas swatch's mean color, then assigns that same UV to ambient faces using the swatch. Only light moss, cobble and grass merge into their base swatches (indices 2→1, 37→36, 61→60). Exact model/texture contracts admit 48 village ground/detail/grass models and 12 ring terrain models; any mismatched or excluded shared usage vetoes the conversion. The normal mesh builder applies this after baking and before publication, so regeneration retains the style. There is no new texture, shader, runtime allocation or geometry simplification.

Results: 60 generated meshes changed, affecting 241,628 UV entries. Every other previously recorded mesh/meta is byte-identical. Actor, equipment, water, toolkit recipe and pilot-ground assets are excluded. Geometry, normals, topology, bounds and material slots compare exactly against a fresh unpainted bake in the real-asset gate. All 5,028 current GUIDs are unique. Borrowed source dependencies remain unchanged.

Review and verification:

- S03: 22 intended missing-implementation RED cases, zero C# errors. S02 refused a still-open Editor without launching tests; its empty directory is not a test result.
- S04: a local variable-name collision prevented compilation; fixed before a successful bake. S05 rebuilt all 406 native meshes and reapplied all 38 toolkit recipe bindings with zero compiler errors and unchanged source dependencies/GUIDs.
- S06: 190/192 focused cases passed; two new test-fixture mistakes incorrectly included a water-material sibling and compared NaN vectors by ordinary equality. Corrected those expectations without changing production behavior. S07: **192/192 voxel tests pass**, including 25 new paint cases and both orders of mixed shared-material usage.
- 🟡 Cold-eye finding fixed: a palette exception after baking could leak an unregistered temporary Mesh. The builder now registers cleanup ownership immediately after allocation. This was source-confirmed; no destructive malformed-file experiment was performed against borrowed assets.
- S08 native audit: **44/44 checks**, four supported chunk binds, six native border crossings, seven screenshots, source freeze and save restoration all pass. Run `6e3fde3722764cdcaefd83639b40702a`; zero C# errors and no unexpected runtime errors.
- 🧪 Visual assessment: the matched spawn capture has calmer green ground and more continuous, readable paths. Flowers, building silhouettes, market colors and characters remain prominent. This is deliberately a modest surface-paint reduction; tiny decorative geometry and the existing CRT treatment remain. The pilot's different ground material is unchanged. This pass does not claim a frame-rate improvement or a newly all-green whole-game suite; U15 remains the prior full-suite baseline, and S07/S08 are the scoped revalidation.

Native comparison: `Verification/VoxelWorld/VWN-6e3fde3722764cdcaefd83639b40702a-spawn-town.png` versus the prior U11 spawn capture. Receipts live in `S03-paint-red`, `S05-quieter-ground-bake`, `S07-paint-voxel-green`, and `S08-quieter-ground-native`. `S01-simplify/preservation.json` lists exactly the changed pre-existing files. `S01-simplify/implementation.patch` contains the new helper/tests/metas and the small builder edits; its reconstructed builder before-state matches the captured SHA256. As with the original native integration, source/art remain installed in the mixed working tree, while this scoped patch and verification documentation preserve the change without absorbing the untracked native foundations.
