# Cathedral voxel kit

Status: complete and installed. Cathedral art: 20 model variants passes final targeted and
full-suite checks. SC14 has 537/537 focused passes; SC17 has 12,825 passes and
exactly the 32 unchanged baseline failures, with zero C# errors and no new
failures. All 287 new tests pass. Eighteen native camera previews have complete
model coverage; live input feel and sustained FPS remain outside this evidence.
The aggregate implementation log and review are in
`CATHEDRAL-STILLLEAF-COMPOSITION-PLAN.md`.

## Identity and native contract

The Cathedral grows its architecture. Pale masses join into broad ribs, one
muted violet course passes through them, and the dark floor leaves the nave
readable. An elder has a composed face above a broad substrate casing; a tendril
has rooted branches with pale terminal knots. These are native living creatures,
not scenery standing in for people. The node remains a native light fixture.

| Family / indices | Exact native aliases | Geometry and palette slots |
|---|---|---|
| `ground`, 0–3 | `SandstoneFloor`, `StoneFloor` | One full-cell dark plane, slot 12; four buried thicknesses, identical exposed top. |
| `vault`, 4–7 | `SubstrateVault` | Three full-cell strata, constant 1.1-cell cutaway top; pale 19 and muted violet 30. Only the broad internal course position varies. |
| `node`, 8–11 | `ChoirNode` | Rooted knot with a 2.28-high, .9-wide pale crown and converging violet support; seven boxes, 19/30. |
| `elder`, 12–15 | `EncasedElder` | Broad casing, shoulders, exposed pale face and paired violet eyes; seven boxes, 19/30. Face points local +Z. |
| `tendril`, 16–19 | `ChoirTendril` | Three coarse branching terminal knots on a green living root; nine boxes, 48/19. |

The kit deliberately does not claim Grass, trees, descent cliffs, ledges, ropes,
supplies or unrelated actors. Existing Stump/Ginmere/ring assets supply those
native owners through scoped integration. All 20 models stay within one cell in
X/Z. One combined readable mesh and one root MeshRenderer per prefab use the
existing ring palette material; no colliders, scripts, lights, particle systems,
rigs or hidden gameplay components are added. Individual cubes are merged during
export, not instantiated separately at runtime.

## Verification sweep before production

| Source | Verified consequence / correction |
|---|---|
| `CLAUDE.md` and the two-area composition plan | Tests and real RED precede the library and generator. Native preview and independent review remain required after numerical checks. |
| `Objects.json`: `SubstrateVault`, `ChoirNode` | Grown solid terrain, with no Destructible Part. The node has native light; art does not grant network travel. |
| `Objects.json`: `EncasedElder` | A passive conversational Creature; its face is left above the encasement. Do not make a standing free-bodied humanoid or a corpse prop. |
| `Objects.json`: `ChoirTendril` | Creature/Trader/Conversation/NaturalWeapon remain authoritative. The new appearance is portable body identity, not a ground coating. |
| `ChoirCathedralBuilder` and the new native plan | Existing native owners determine the mass and aisle. No per-cell arch gap or random top height should fragment adjacent vaults. |
| `Lore/10_Bible.md`, `Lore/Factions/01_RotChoir.md`, `FELLING-WORLD-DESIGN.md` | Pale ochre/violet grown substrate fits canon. The Choir has no manufactured material culture. A local elder is not the Wedded's original body. Wedded audience and Wall-Catching are outside this art task. |
| Native agent's actual stack contract | Fresh Cathedral ground is Grass/SandstoneFloor, not exclusively StoneFloor. The kit accepts both exact stone floor aliases and leaves Grass to the existing native mapping. |

## Reproducible build and API

Run `CavesOfOoo.Editor.CathedralVoxelKitBuilder.Run` in the isolated Unity project.
It writes only `Assets/Resources/CathedralVoxel3D`, with the library at
`CathedralVoxel3D/Library`. Mesh assets update in place and prefab saves preserve
existing GUIDs. The builder creates temporary geometry and destroys it without
saving the open scene. No manually positioned demonstration objects are inputs.

`CathedralVoxelLibrary.ModelId(family, variant)` returns `cathedral-<family>-<0–3>`
from validated family/variant inputs; unknown families and out-of-range variants
throw. `Family(blueprint)` is an exact-name lookup and returns null for unclaimed
owners. `Validate` checks the complete set and all prefab, mesh, material, bounds,
triangle count, kind, path and rig metadata before publishing its lookup.

```csharp
string id = CathedralVoxelLibrary.ModelId("elder", 2);
var nativeArt = CathedralVoxelLibrary.Load().Find(id);
// Runtime recipes retain the native actor as the sole owner and face it into the aisle.
```

## Gates and self-review

`CathedralVoxelKitTests` pins exact aliases and counterexamples, four shapes per
family, quiet flat floors, full-width joining vault strata, a shared top color,
actual face/eye geometry and branching actor silhouettes. Corrupted library
copies test missing/duplicate identities and wrong mesh/material/metadata without
mutating shipped assets. Bounds tests enforce one-cell ownership and at most ten
boxes / 240 vertices per model.

- ⚪ CoO-original procedural art; no Qud source-parity claim.
- ⚪ Constant visible floor and vault top are deliberate exceptions to random
  top variation; four authored forms still differ below or within those surfaces.
- 🧪 Mesh assertions verify references, scale and intended geometric features;
  they do not establish camera readability, animation feel or live frame rate.
  Parent-owned native camera previews and final integration tests must close that gap.

## Implementation log

- 2026-09-15: Verified native blueprints, builders and canon, coordinated stack
  aliases with the native agent, and wrote kit contracts and corruption controls.
- SC03: Parent confirmed actual missing `CathedralVoxelLibrary` compile RED from
  the saved art fixture before production.
- Implemented the 20-model source generator and strict library. No shared runtime
  files or user scenes were edited by this art task.
- SC05: Parent reported the initial Cathedral and Stillleaf art fixtures passing.
  Native owner census found additional unrelated Stillleaf cave families, addressed
  additively in the Stillleaf kit.
- Source-only audit: all 68 combined models (20 Cathedral + 48 Stillleaf) satisfy
  one-cell bounds, at most 240 vertices and at most two swatches. Six copied source
  metas differ only by GUID and all six GUIDs are unique across project Assets.
  This numerical audit does not replace the isolated Unity asset tests.

Files: `CathedralVoxelLibrary.cs`, `CathedralVoxelKitBuilder.cs`,
`CathedralVoxelKitTests.cs`, their `.meta` files, and this document. Parent export
owns generated `Resources/CathedralVoxel3D` assets and their validation receipts.

### SC07 camera review and pending SC09 correction

Native `GrownNave` views at seeds 64, 1729 and 729490642 show that south-row
elders become tiny head tips behind the 1.8-high foreground vaults. The node at
the eastern end reads about the size of an ordinary inhabitant. Approved
source-rule refinement: cutaway vault height 1.1 (native solidity unchanged),
node crown about 2.28 high and .9 wide, preserving seven boxes and two colors.
Tests now assert actual exposed-eye geometry clears the wall by .1 and a broad
node crown rises above the elder and vault. Updated height contracts are saved
before production; parent SC09 RED is pending. No layout is manually moved.

- SC09: Parent captured actual cutaway/vault height, node height and geometry
  RED with zero C# errors. Implemented vault top 1.1 and node crown 2.28 high /
  .9 wide while keeping the same color pairs and seven node boxes. All 72
  combined source models pass one-cell/two-swatch/240-vertex source audit.
- Corrected the eye test's selection region from front z>.22 to z>.30. The
  earlier region also selected shoulder corners at y1.17; actual eye boxes are
  at y1.2475–1.2925, z.304–.344. Tightening the region makes deleting eye geometry
  fail rather than passing on shoulders. The wall+.1 clearance assertion is
  unchanged, and no valid face geometry was moved to accommodate a test error.
- Final isolated rebuild and camera review are pending parent SC10 execution.

### SC10 static camera review and SC11 verification

Inspected `GrownNave-64.png`, `GrownNave-1729.png` and
`GrownNave-729490642.png` in `Verification/VoxelWorld/SC10-refined-preview`,
comparing them with the earlier SC07 views.

| Observed feature | Result at the existing gameplay camera |
|---|---|
| Foreground elders | Heads and upper violet casings now project visibly above the lower vaults in all three seeds. Seed 729490642 shows all three southern stations, where SC07 showed only small head tips. |
| Facial orientation | Northern elders show pale front faces. Southern elders face inward, away from this camera, so these fixed views establish upper-body visibility rather than legible frontal eyes from every orientation. Do not claim all eye details were visually verified. |
| Choir node | The eastern node now has a visibly taller, broader pale crown and reads separately from ordinary elder/tendril silhouettes in all three seeds. Its footprint remains compact. |
| Architectural coherence | Broad ribs retain their continuous tops and thin violet course; lowering the cutaway exposes conversation stations without adding per-cell detail or gaps in native ownership. |

The `build-receipt.json` reports Unity exit 0 with no compile errors. The complete
preview receipt has 18 rows, zero missing meshes and zero unmodeled owners. Parent
SC11 reports **527/527 targeted checks passing**, including final art contracts.

⚪ No additional art change is required by these six static review views. The
parent's separate elder action/damage-facing hypothesis concerns runtime pose
reconciliation and is not verified by still images.

🧪 Can verify: static silhouette hierarchy, reduced foreground obstruction,
continuous masses and reported native model coverage. Cannot verify: live input
feel, motion after damage, or sustained frame rate. The full regression run is
still pending; targeted success is not reported as a full-suite pass. This review
changed documentation only, with no production or test edits.

## Final verification

SC17 confirms the exact baseline failure names and messages are unchanged.
SC13 rebuilt all 294 combined artifact/metadata files byte for byte. The final
GUID audit covers 5,735 Unity metadata files and finds no task collision.
`Verification/VoxelWorld/SC16-closeout/regression.json` records the comparison;
`SC10-refined-preview/index.html` contains all eighteen actual native renders.
Earlier pending statements in the implementation log describe those earlier
runs; this final verification supersedes them. New layout rules apply on fresh
native generation; existing cached/saved graphs are not rewritten.
