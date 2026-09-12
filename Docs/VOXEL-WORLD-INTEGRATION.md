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
PYTHONPATH=ArtSource/VoxelTown python3 - <<'PY_COARSE'
from town_generator.native_assets import export_assets
export_assets('ArtSource/VoxelTown/Output/native-region/assets-coarse.json',
              profile='native_coarse')
PY_COARSE
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

## D1 — fewer voxels per model (2026-09-12, complete)

User feedback asks for actual geometric simplification following S1's paint-only change. Increase voxel pitch for ordinary objects, characters and scenery at their authored placements, and reduce the procedural recipes' cell counts as well. Preserve the quieter palette, gameplay ownership, camera and native rigs. Very small items and thin equipment need a lower pitch so their silhouettes remain legible.

Pre-implementation sweep corrections: the prior `.125/.2m` selector uses bounds diagonal, not longest side; all current prefab scales are 1 but future conversion must still account for scale. Merely changing a toolkit recipe's `.25m` unit and uniformly fitting it back does not reduce its cell count. The 38 recipe substitutions bypass the original-mesh baker entirely and need a genuine recipe simplification. Non-cell-dividing pitches can overhang grid seams; static cell scenery should use `.25m`. Skins must not be rescaled independently of bones, and bone groups can disappear at an overly coarse pitch. Retain fine art for thin/small bodies and verify prior rendered bone coverage.

Gate: observed RED density tests, coarser bake/export, actual cell-count reduction (not just triangle merging), source/GUID preservation, skin and hole/footprint counterchecks, followed by matched native captures and full voxel regression. Before-state files and targeted copies are recorded in `Verification/VoxelWorld/D01-coarser/prechange.json`. Independent toolkit and mesh reviews precede changes; fixes belong in generation rules rather than manually edited example objects.


D1 implementation: `VoxelWorldDensity` uses 0.25 m blocks for ordinary rigid
scenery and 0.1875 m for ordinary animated bodies. Static scenery therefore fits
four voxel steps per native one-metre cell. Equipment, objects smaller than
0.55 m, and skins thinner than 0.15 m retain the prior pitch. The selector
accounts for prefab scale without changing transforms or scaling mesh vertices
away from their bones. Of 406 native bake sources, 391 receive a larger pitch;
15 retain their previous pitch, including both thin wardlines and five long
copper pipes. Rigid objects with two axes below 0.5 m and a length at least four
times their middle dimension retain their earlier pitch; broad thin floors do
not qualify. The 60 S1 ambient
paint treatments remain applied after geometry generation.

The imported procedural props now use `assets-coarse.json`, the optional
`native_coarse` profile described in `VOXEL-TOWN-GENERATOR.md`. The 19 bound
recipes contain **1,428 → 584 occupied cells (59.1% fewer)**; the 38 native
bindings retain their identities, bottom planes and uniform fits. All 37 changed
bindings have a larger effective pitch after that fit; the minimal stool is the
unchanged control. The original fine export and all candidate layouts remain
unchanged. This avoids silently changing full-size toolkit ownership footprints.

Measured examples from actual reloaded Unity assets:

| Model | Previous cells | New cells | World pitch |
|---|---:|---:|---|
| Player `character-teal` | 556 | 210 | 0.125 → 0.1875 m |
| Herb pot | 173 | 38 | 0.125 → 0.25 m |
| Ground patch 0 | 1,628 | 1,087 | 0.2 → 0.25 m |

Cell counts use closed surface volume divided by the recorded cell volume;
triangle merging alone cannot pass this gate. The full native bake decreases
from 475,994 to 263,948 triangles before recipe overrides. Accounting for all
38 overrides, the effective catalog contains 255,428 rather than 450,644
triangles. These are asset geometry counts, not a frame-rate claim.

D1 verification and review log:

- D02: zero compiler errors, 16 observed RED cases for missing density policy
  and unchanged actual art, plus three passing baseline controls.
- D03: runner refused an unexpectedly open Editor without launching a bake;
  closed the idle Editor normally. D04 then baked all 406 meshes successfully.
- D05: 210/211 focused tests passed, but the character's occupied volume was
  non-integral. Raw serialized inspection confirmed 4,520 old vertex positions
  paired with indices for only 2,116 new vertices. This was a real publication
  bug, not a tolerance problem. The all-skins exact-bake regression added next
  also failed in D06 (26/28 density tests passed).
- 🟡 Fixed: `EditorUtility.CopySerialized` retained an old skinned vertex stream
  when rebaking an existing mesh asset. Both generators now clear the destination
  layout and explicitly write their emitted mesh channels, retaining its asset
  identity and GUID. No borrowed FBX, rig or source material is rewritten.
- D07: final bake/import succeeded, zero compiler errors, all source dependencies
  unchanged and existing output GUIDs preserved. All 38 procedural bindings use
  the exact final coarse export hash recorded in the toolkit log.
- D08: **220/220 voxel cases pass**, including all 28 published skins matching
  fresh bakes exactly (vertices, normals, UVs, weights, bindposes, submesh indices
  and bounds), prior visible bone coverage, actual voxel counts and unchanged
  small-equipment geometry.
- 🔵 Review gap closed: an occupancy-only hole check cannot prove that geometry
  stays open. Eight new tests cast vertical rays through the actual presenter
  MeshColliders of every NE/SE ridge variant. Each requires an occupied-cell hit
  and misses in the adjacent gap and empty corner, all inside the overall bounds.
  All eight already passed in D06 and remain green in D08; these are regression
  pins, not claims of eight bugs fixed.
- D09: **44/44 native checks**, four active chunk binds with zero missing mesh
  mappings, six normal-key border crossings, seven real GameView captures and
  exact native save/load snapshots. Runtime sources stayed frozen throughout.
  Independent receipt inspection recomputes all 25,365 recorded frames and
  validates hashes, save restoration and distinct checks. Run ID:
  `56aa9606627046169cc525f753c0a6d3`.
- Cold-eye review: selector exclusions, scale conversion, generated-channel
  symmetry, cleanup ownership and source/GUID preservation reviewed independently.
  No unresolved production blocker remains. Toolkit counterchecks caught and
  fixed lost bed cloth and a low-rock fit that canceled the pitch gain; explicit
  bed underside gaps strengthen the existing correct silhouette gate.

Visual assessment: the actual spawn capture has broader character silhouettes,
simpler wall/well edges and larger individual prop blocks. Western-field trees
and shrubs have fewer, broader lobes. The visible southern ridges retain their
irregular outlines; all eight geometric hole probes pass independently of fog.
The grove screenshot only covers its normally visible entry area. The paired
Blender asset image separately exposes all 19 fine/coarse recipes at matched
owner envelopes. Existing CRT pixelation, fog, ground colors and camera remain,
so coarse geometry does not remove every source of visual noise.

Can verify: reduced actual cells, exact persisted skin geometry, full catalog
coverage, silhouette counterchecks, eight open collider gaps, native targeting,
destruction, hauling, spell/animation signals, four chunk binds and source/save
restoration. Cannot verify: every possible future asset or layout, subjective
long-session readability or a general frame-rate improvement. D09's four pilot
phases have engine-frame p95 around 3.42–5.94 ms and maxima 16.70–46.45 ms; peak
recorded frame allocation is 24.96 MB. This bounded Mac/Metal Editor recording is
not a controlled performance comparison, and hitches remain outside this art pass.

Native changes remain installed in the mixed working tree under the same
preservation boundary as the prior integration. D1's exact helper/test additions
and three existing-file edits are recorded in
`Verification/VoxelWorld/D01-coarser/implementation.patch`; the old modified files
were verified against their before-state hashes. The independent coarse toolkit,
its tests/export/preview, and the living verification record form the scoped
commit without absorbing unrelated native foundations.


D1 full-suite refinement:

- D10 full regression: 11,763 total, 11,729 pass, 34 fail, zero compiler errors.
  Thirty-two failures exactly match U15. Two additional existing hidden-contact
  picking tests exposed a real interaction regression: the coarser long pipe
  widened its raised bounds and filled an intentionally empty click corner.
- 🟡 Fixed in the density rule, leaving those gameplay tests unchanged: preserve
  the original resolution of slender rigid objects with two narrow axes. The
  rule is independent of model names and orientation, and does not exempt wide
  thin terrain. Current content adds exactly five copper-pipe exceptions; staff
  equipment was already protected.
- D11 observed RED: all five new policy cases failed before the thin-object
  guard; the original 13 policy cases still passed. They cover axis permutations,
  effective scale, long-object previous pitch, and broad/compact counterexamples.
  An additional real-asset gate pins all five pipe shapes against fresh old-pitch
  bakes, including geometry, paint and bounds.

- D12 final rebuild succeeds with zero compiler errors and preserved source/GUID
  contracts. Exactly five pipe meshes return to their original 0.125 m pitch;
  all other D07 native meshes and the coarse recipe export are unchanged.
  The final bake/effective-catalog triangle counts above include these exceptions.

- D13 final full regression: **11,769 total, 11,737 pass, 32 pre-existing failures,
  zero compiler errors**. Failing names and messages exactly match U15; no new
  failure remains. All 226 voxel cases and the unchanged hidden-contact gameplay
  tests pass. This adds 34 density cases to S1's 192 voxel cases.
- Preservation/GUID audit: 2,728 captured files checked, only expected generated
  outputs and four existing implementation/export files changed, no changed old
  metas. All 1,658 independently recorded borrowed asset dependencies are
  byte-identical; 5,032 valid GUIDs are unique. All 406 catalog binding identities
  and 38 toolkit GUID/ownership pins survive. Receipts now reference D12.

- D14 post-review countercheck: the same slender dimensions marked as skinned
  still use the character pitch. All 18 policy tests pass, zero compiler errors.
  This is an added assertion in existing cases after D13; production/source art
  are unchanged from the successful full regression.
- The exact implementation patch passes Git's reverse-application check without
  modifying the tree. Missing final newlines in generated metas are represented
  correctly in the patch; no metadata rewrite was needed.

- D15 final native audit: **44/44 checks pass**, zero compiler/runtime errors,
  all four chunk binds active with zero missing models, six keyboard seams and
  exact save restoration. Sources stayed frozen. Independent inspection validates
  all hashes and recomputes 24,509 profile frames. Final run:
  `d437ca34fa414ebe9d5521de418a7f3b`. The final spawn capture was inspected again;
  the successful chunky forms remain after preserving the five pipe silhouettes.
  This supersedes D09 as the final native source/art verification.
- Closed the audit Editor and reopened the normal project in Unity with its
  existing SampleScene. No further code or asset changes followed verification.

Final D1 evidence: `D01-coarser/density-summary.json`,
`D01-coarser/full-regression-comparison.json`, preservation/GUID receipts,
`D13-coarse-final-full-regression/receipt.json`, and
`D15-coarser-final-native/independent-inspection.json`, all under
`Verification/VoxelWorld`. The procedural comparison is
`ArtSource/VoxelTown/Output/native-region/coarse-comparison.png` (fine left,
coarse right). The final native spawn image is
`Verification/VoxelWorld/VWN-d437ca34fa414ebe9d5521de418a7f3b-spawn-town.png`.


## P1 — two-color object paint (2026-09-12, complete)

User feedback: colors and shades remain too noisy; bring each object down to a
couple of colors. Scope: deterministic offline two-color paint for native voxel
objects and a two-color exported visual palette for the procedural coarse profile.
Keep D1 geometry/pitch, source textures, authored object ownership, rigs, camera,
visibility/status signals and existing simulation intact. This is CoO-original art
direction, not a Qud mechanics parity claim. Content readiness is green for the
native palette objects and neutral toolkit export; the southern flat ground's
separate non-atlas texture needs explicit visual assessment rather than an assumed
catalog match.

Pre-implementation verification:

| Finding | Verified correction / decision |
|---|---|
| S1 only changed 60 ambient models and three bright swatches | A new object-wide paint cap is required; geometry reduction alone cannot remove these colors |
| Toolkit recipes bypass the native mesh bake | Cap exported face/triangle/run materials after geometry generation; keep construction recipes and all geometry unchanged |
| Same source mesh may have different renderer materials | Preflight material/texture agreement before publishing a shared mesh; never recolor a foreign shared source silently |
| Existing density tests pin UVs as well as geometry | Preserve the geometry/rig/contact assertions, update only superseded exact-paint expectations with a separate two-color invariant |
| WorldCamera disables postprocessing, but composited Main Camera still grades the result | Two paint colors are not a claim of exactly two final screen pixels after lighting/fog; inspect the real gameplay view |
| Water/tar use solid base tints and waves; pilot flat ground uses world UV over a non-atlas texture outside the catalog | Do not mistake either for a standard object palette or break their visibility/material contract |

Gate order: snapshot and plan; observed RED palette/counter/adversarial tests;
implement cap and neutral export; rebuild; verify actual <=2 sampled paint colors
and exact geometry/rig/GUID/source preservation; review native captures, fix rules
if needed; final scoped tests and live audit; living docs and scoped commit. The
last D13 full baseline is 11,737 pass / 32 exact pre-existing failures, zero C#
errors. P01-two-colors/prechange.json captures 973 target files and exact copies
before implementation. Unity was confirmed idle, with no unsaved scene marker,
and closed normally before headless work.

Performance: palette analysis runs offline, not per frame. Reuse loaded image
pixels per texture; shared baked meshes stay shared. No runtime voxelization,
per-cube GameObjects, simulation RNG or save migration. Build reports will include
paint counts so the cap can be audited independently of a screenshot. A focused
native live recording verifies the rendering result, not a general FPS claim.

P1 implementation and verification log:

- P02: 21 observed RED cases for the absent object-palette helper, zero C#
  errors. P03 refused to launch while an unrelated idle Editor was open; it is
  not test evidence. Closed that Editor normally, then resumed headless checks.
- `VoxelWorldObjectPalette` selects one or two deterministic frequency-weighted
  representative RGB colors from the object's actual opaque samples. UV0 moves
  to canonical texel centers. No new atlas, runtime shader or random stream is
  introduced. Invalid dimensions, nonfinite/out-of-range UVs and sampled alpha
  reject before asset publication; unused transparent padding is harmless.
- The native builder groups opaque and water children by source FBX, giving a
  whole object two paint colors. Fourteen objects with water reserve one tint
  for that child and one for the opaque structure; the fifteenth water renderer
  is a standalone surface. Shared foreign textures, transforms or tints veto
  object recoloring. Temporary baked meshes are owned before palette IO and
  cleaned in the existing failure/finally path.
- P04: 28/32 new cases passed; the actual catalog and water-sibling color caps
  failed before regeneration. A nonwhite shared material correctly exposed a
  missing eligibility guard, now fixed. A test counted only the 14 paired water
  renderers; corrected it to include the standalone fifteenth water surface.
- P05 rebuild: zero C# errors, all 406 native meshes and 38 procedural bindings
  published with old GUIDs and borrowed assets intact. Native opaque meshes use
  one color (28) or two (363), with 15 unchanged solid water meshes. Native raw
  bake geometry remains 263,948 triangles; the toolkit uses the final two-color
  coarse export SHA256 recorded in the toolkit's P1 phase.
- P06: **258/258 voxel tests pass**, zero C# errors. New tests enumerate all 392
  complete prefab objects after recipe overrides and count actual RGB values,
  including water tints. All 368 native-baked catalog meshes match fresh geometry
  and skin channels exactly; all 15 water meshes also retain exact UVs.
- Updated four superseded exact-paint assertions in older density/S1 art tests.
  Geometry, rig, bounds, pitch, picking-gap and source-water assertions remain;
  the new complete-object RGB gate now owns the requested paint expectation.
- 🟡 Independent review found that a shared tinted or UV-transformed material
  vetoed P1 but could still be recolored by the earlier S1 ambient pass. The
  expanded shared-use fixture tests ambient and ordinary objects in both
  encounter orders. P07 confirmed six failing ambient cases and six passing
  controls, zero C# errors. Both passes now require the same supported atlas,
  white tint and identity UV transform; an unsupported use permanently vetoes
  both passes for that source. No current shipped material uses the excluded
  tint/transform, but the builder must remain safe for future shared usage.
- P09 final rebuild: zero C# errors. All 892 captured generated art/catalog/meta
  files are byte-identical to P05 after the shared-material guard fix; all 406
  mesh report rows retain their geometry, pitch and paint metrics.
- P10 final full regression: **11,811 total, 11,779 pass, 32 pre-existing failures,
  zero compiler errors**. All failing names and messages exactly match D13;
  no new failure remains. All **268 voxel cases** pass, including 42 new palette
  cases and the expanded 12-case shared-use countercheck. The independent pure
  toolkit suite passes **383/383**, adding 68 palette gates to its prior 315.
- P08 final preservation audit: all 444 generated meshes retain every serialized
  channel except UV0, with 390 changed UV buffers. Catalog bytes, 406 recorded
  pitches, all 1,658 borrowed asset dependencies, 38 toolkit identity pins and
  all old metas remain intact. All **5,035 GUIDs are valid and unique**. The
  native implementation/test delta passes Git's reverse-application check.
- P11 native audit: 44/44 checks, zero compiler/runtime errors, four chunk binds
  without missing models, six normal-key borders and restored saves. Sources
  stayed frozen. Run `2a7a6e0451754b12b732ea6ba40627a7` captures the first complete
  two-color pass; it is superseded by the following visual refinement.
- 🟡 Native look pass: roofs, the well and field vegetation are visibly calmer,
  but frequency-only RGB reduction turned both market awnings brown. Their cloth
  silhouettes no longer communicated the two distinct stalls clearly. Add an
  offline semantic rule for the two authored Village market-stall models:
  one sampled teal/violet cloth color and one sampled wood color, still exactly
  two per complete object. Source proof is the stable Village atlas and `stall`
  recipe in `ArtSource/Village3D/build_scene.py` (palette and lines 410–449).
  This changes the generation rule, not individual scene placement. All other
  native models keep generic reduction. Southern flat ground remains the separate
  existing world-UV texture; its painted strata are outside this object-paint cap.
- P12 confirmed 11 new RED cases with 42 existing palette cases still passing,
  zero C# errors. The new cases pin sampled cloth/wood anchors, exact source
  identity, missing-anchor rejection, determinism, unchanged inputs, stable
  reapplication and original-vertex correspondence in both real stall assets.
- P13 refinement rebuild succeeds; exactly two of the 444 generated meshes
  differ from the preceding native pass. Each canopy now uses its existing
  teal/violet sample, while the remaining structure uses one sampled wood tone.
- 🔵 Review hardened the same rule against a swapped known atlas: semantic
  reduction now receives the actual source texture path and requires the exact
  Village palette, not merely dimensions divisible by eight. P14 confirmed three
  RED cases for the missing texture-provenance contract; each pairs another
  atlas with an otherwise identical genuine-Village positive control. Other
  supported textures retain generic two-color reduction.
- P15 final rebuild succeeds, zero C# errors. The exact two cloth-refined meshes
  are the only generated asset changes after P09; all geometry and other objects
  stay intact. Exact source and atlas checks were cold-eye reviewed independently
  with no remaining blocker.
- P16 final full regression after refinement: **11,825 total, 11,793 pass, 32 exact
  pre-existing failures, zero compiler errors**. Failure names and messages still
  match D13 exactly. All **282 voxel tests** pass, including all **56 new palette
  cases**. This supersedes P10 as the final full-suite result. Pure toolkit remains
  at its verified 383/383; its code/export did not change during native refinement.
- P17 final native acceptance: **44/44 checks pass**, zero compiler/runtime
  errors, all four chunk bindings active without missing meshes, six normal-key
  border crossings and exact save restoration. Runtime sources stayed frozen.
  Final run: `13560571d1354f60ae6b7f2c3b2460f1`.

Final visual assessment: the actual gameplay view has broader, quieter paint on
roofs, the well and ground objects. The two stalls again read as teal and violet
cloth against wooden frames. Western field foliage uses clearer green forms
against earth; southern rocks retain their outlines with simpler paint. The grove
capture covers its normally visible entry area, not a fully revealed map. The
separate southern flat ground retains its existing painted strata texture; the
object-color cap does not claim to quantize that ground image or the entire screen.

Can verify (script-observable): all 392 native prefab objects and all 59 coarse
export records meet their two-base-color caps; water reserves its own tint;
all geometry/rig/collision inputs and GUIDs remain intact; fresh-bake comparison,
shared-use vetoes, deterministic reduction, targeting, destruction, hauling,
animation signals and four chunk transitions pass their recorded gates.
Cannot verify from this bounded run: exactly two final screen shades after
lighting, fog and compositing; every future asset/scene; subjective long-session
readability or a general performance improvement. No runtime color processing,
new per-frame work, camera change, image editing or simulation change is added.

P1 cold-eye/adversarial close-out: independent reviews covered shared-material
symmetry, both encounter orders, source/texture identity, role anchors, deterministic
ties, exception cleanup, geometry preservation, whole-prefab water budgets and the
native screenshots. The confirmed shared-use guard and cloth-identity issues were
fixed before commit. No unresolved production blocker remains in this scope.

The native changes remain installed in the mixed working tree under D1's existing
preservation boundary. Their exact helper/test additions and three existing-file
edits are recorded in `Verification/VoxelWorld/P01-two-colors/implementation.patch`;
Git's reverse-application check passes without altering the tree. The scoped commit
contains the independent Python toolkit change, its export/tests, the native delta,
verification records and final spawn image without absorbing unrelated foundations.

Files: new native `VoxelWorldObjectPalette` and two palette test fixtures;
modified native `VoxelWorldMeshBuilder`, density-art and ambient-paint tests;
new toolkit `native_palette.py` and `test_native_palette.py`, modified
`native_assets.py`, coarse export, README and both living docs. Final evidence:
`P01-two-colors/palette-summary.json`, `P08-preservation/audit.json`,
`P16-final-palette-full-regression/receipt.json`,
`P01-two-colors/full-regression-comparison.json`, and
`P17-final-palette-native/receipt.json`, all under `Verification/VoxelWorld`.
Final image: `Verification/VoxelWorld/VWN-13560571d1354f60ae6b7f2c3b2460f1-spawn-town.png`.

P17 independent inspection validates the artifact hashes, seven 1920×1080
captures, four chunk bindings, six borders, source/save restoration and all
21,580 recorded profile frames. Its bounded southern workload is about 80 seconds;
p95 engine frames are 5.39–5.82 ms, but four frames exceed 100 ms and the maximum
is 743.13 ms. Hitches remain; this offline color change makes no smoothness claim.
The receipt is `P17-final-palette-native/independent-inspection.json`.

Reopened the normal Unity project after all headless/native runs. Verified
SampleScene is idle with no unsaved scene marker and zero console error/warning
indicators. No further code or art changes followed final verification.

## R1 — temporary full 3D reveal (2026-09-12, complete)

User request: disable the hiding of objects outside player line of sight for now.
Reuse the existing native presenters' FullReveal display mode. The normal main
scene enables ZoneRenderer.RevealEntire3DZone; its default is false for other
scenes and isolated fixtures. SyncVillagePresentation forwards it to both town
and ring presenters before binding/refresh, so it follows chunk changes and
restarts. The camera, combat LOS, AI and native cell visibility/exploration data
remain unchanged. Switch the scene option off to restore normal hiding.

Verification sweep: both presenters already implement whole-ground fog reveal,
transient-entity visibility and full-reveal picking, with existing positive and
negative tests. Runtime component tooling reported a play-mode scene-dirty error
after assigning the properties; readback confirmed both true, but the play session
subsequently ended. The persistent scene option avoids relying on transient state.
This is reversible configuration plumbing for existing behavior, not a new FOV
algorithm or Qud parity claim. No added test suite for the configuration; run the
existing FullReveal counterchecks and inspect native play before/after. The prior
ordinary-FOV audit's !FullReveal assertions deliberately do not describe this
requested temporary display mode. Performance: no new allocations or per-frame
loop; existing full-reveal presentation can display more objects.

R1 verification: existing FullReveal tests pass 3/3 with zero compiler errors
(`R02-existing-full-reveal-tests/receipt.json`). Review of the exact two-file
delta confirms the option reaches both presenters before Bind, defaults off in
isolated fixtures, and changes no native FOV/exploration calculations. Existing
adversarial coverage exercises restoring hiding and preserving unexplored cells.
No additional code findings in this bounded review; no whole-game audit claim.

Live follow-up found the graphics preference was off: an earlier F11 refresh
attempt had left the game in its original 2D mode, whose FOV mask still applies.
Restored 3D through F11 with the Game tab focused; readback confirms Enabled=true,
persisted preference=1, both FullReveal flags=true, and the southern ring presenter
active/ready with no failure. Visually inspected the running southern chunk:
voxel geometry is restored and the former LOS wedge is gone. Camera unchanged;
left the user's session playing. Evidence: `R01-full-reveal/live-verification.json`.

Preservation boundary: the implementation remains installed in the mixed native
working tree. `R01-full-reveal/implementation.patch` records only this request's
scene/renderer delta; reverse-apply check passed. This commit records the patch,
verification and living doc without absorbing unrelated native changes. The
ordinary-FOV native audit was not rerun because its normal-hiding assertions are
intentionally incompatible with this temporary display option.

## S1 — western voxel chunk fresh-game spawn (2026-09-12, complete)

Request: start in the voxel-tree chunk directly west of Morrowfast. The native
region audit identifies that neighbor as Overworld.2.6.0; Morrowfast remains at
Overworld.3.6.0. Configure the main scene's fresh-game destination independently
of town identity. Keep the existing center/outward open-cell placement for the
western chunk and the southern-road placement when explicitly starting in town.

Verification correction: the town-specific garden preparation currently rejects
other zones and aborts bootstrap. Gate it to authored Morrowfast; the existing
general FarmPlotSeeder still runs after placement for the new western start.
This is original game configuration, with no Qud parity claim or new hot loop.
Validate actual fresh-zone generation plus placement in both destinations, run
existing start/garden checks, then inspect a fresh native game. Save loading keeps
its existing saved location. Preserve unrelated mixed-tree changes as an exact
implementation delta, consistent with R1.

Implemented `GameBootstrap.FreshGameZoneID`, defaulting to Morrowfast for existing
fixtures/other scenes; Main/SampleScene selects Overworld.2.6.0. The paired fresh
generation/placement test failed before implementation (2/2 missing-field
assertions, zero compiler errors), then passed with existing start/garden suites:
41/41 green, zero compiler errors. Receipts: S01-west-spawn-red and
S02-west-spawn-green. No save schema or world-coordinate relocation was needed.

Self-review: 🟡 town-only garden rejection would abort a western start; fixed by
conditioning preparation on authored town identity. Counter-check retains town
entry (40,23); western start uses existing open-cell search. No new per-turn or
per-frame work. Exact native delta reverse-apply check passed. Existing broader
native audits assume a town start and require explicit town configuration when
rerun; this bounded change does not claim those workloads were rerun.

Live validation: fresh Play + N starts at Overworld.2.6.0, cell (40,12), confirmed
walkable. Voxel presenter is active with 33 replacements, zero missing meshes,
and no failure. Full reveal and saved 3D mode remain enabled. Visually inspected
the woodland chunk from the unchanged gameplay camera; Unity remains playing.
Can verify: actual bootstrap destination, passability and renderer state.
Cannot infer: long-session balance or traversal quality from this start check.
Receipt: S03-west-spawn/live-verification.json. Changed native files are
GameBootstrap.cs and Main/SampleScene.unity; only the exact installed patch is
recorded to preserve the unrelated native working-tree changes. New paired test
and its Unity metadata are included directly with this doc and receipts.

## G1 — Grovelands composition (2026-09-12, complete)

Integrated a deterministic spatial planner and native terrain realization for
ordinary wilderness Grovelands. Four formation recipes now consume planned
clearings/approaches; compost rows are bounded beds, canopy is clustered, fen
water and inhabitants share planned veins. Extended voxel coverage to wilderness
Grovelands while preserving towns, mouths, special sites and summit recipes.
Existing native owners retain destruction/harvesting behavior. Detailed design,
review findings and reproduction instructions: `GROVELANDS-COMPOSITION.md`.

357 targeted checks passed; final full suite 11,831/11,863 with exactly the same
32 recorded baseline failures, no introduced failures or compiler errors. Reviewed
12 actual voxel previews and a normal fresh-game spawn. Unity left playing in the
new western composition, with full reveal and 3D enabled. Exact installed native
delta and live evidence: `Verification/VoxelWorld/G10-composition-final`.
