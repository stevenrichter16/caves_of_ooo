# Sumphold voxel kit

Status: complete and installed, 24 combined-mesh voxel variants. WB11 passes the actual asset and native-owner coverage tests. WB15 rebuild preserves all262 combined asset/metadata files byte for byte. WB10’s six native captures have zero missing meshes or unmodeled visible owners.

CoO-original art with at most two palette swatches and240 vertices per model. [Native contracts and complete verification](CINDERHOLD-SUMPHOLD-COMPOSITION-PLAN.md).

## Verified contracts and corrections

| Source | Actual native contract | Art consequence |
|---|---|---|
| WorldMapAuthoring / Geography | Sumphold is currently Spread tier1 without mapped river, while lore describes a raised bog-edge town. | Local wet margins retain current biome/balance; do not relabel it Sodden or import a global river. |
| SumpholdBoatyard stamp | Exactly two BoatFrame owners, two PeatCutter actors and one TollRolls. | Each fixture remains one cell. No giant hull spanning several owners and no consolidation of separate records/people. |
| BoatFrame | Upside-down shallow wide hull on trestles, solid non-takeable examinable fixture. No vehicle, crafting, destruction or material Part. | Bare ribs, long raised keel and two trestles with actual mesh gaps; no boat driving, launching or construction verbs. |
| TollRolls | Bound century-deep crossing records under a rain-hood. Examinable only, no container, toll or reading Part. | Sheltered pale bound stacks; do not invent text, coin slots or reveal the private skin-reading witness. |
| PeatCutter | `@` Creature, existing conversation, thigh boots and long spade in description. Actual loadout supplies boots/gloves and Dagger. | Boots and spade form the silhouette, but no inventory spade or digging action is created. |
| WaterPuddle / PeatBank / Duckboard / Reeds | Real native liquid in wet cuts; solid destructible peat; destructible boards do not suppress co-located water. | Dedicated quiet water and suitable existing peat, boards and reed art follow actual owners. Zone.ProjectPool projects the native permanent water coating on add; UnprojectPool clears it when the last pool owner leaves. Art does not write those mechanics. Dry routes are native generation's responsibility. |
| WB08 native camera | Converted borrowed WaterPuddle art showed a green checker pattern that read as grass. | A single continuous deep-teal24 surface, constant height across variants; art-only change; existing native owner-projected water and coating remain authoritative. |
| SandstoneWall / Floor / StoneFloor | Dry outdoor Floor, shaded StoneFloor, real masonry walls. | Muted raised bog-earth and low damp masonry; do not portray stone as combustible timber or Floor as fake water. |

## Public contract and palettes

`SumpholdVoxelKitLibrary.ResourcePath = "SumpholdVoxel3D/Library"`.
The strict `Load`/`Find`/`Validate`/`Family`/`ModelId` API follows the existing kits.
Only exact aliases are claimed; unsupported owners return null. Families and
variants0–3 are validated, never silently redirected to another mesh.

| Ordered family | Exact alias | Palette / identity |
|---|---|---|
| ground | Floor | Quiet slot68 matching existing Sodden earth; full-cell top with buried variation. |
| wall | SandstoneWall | Two full-cell damp masonry strata12/48, height1.05. |
| hull | BoatFrame | Weathered ribs/keel9 and dark trestles12, nine coarse boxes inside one cell. |
| rolls | TollRolls | Wood8 rain hood and pale19 bound records visible below it. |
| cutter | PeatCutter | Dark12 thigh boots and gray13 clothing/blade, stable actor variants. |
| water | WaterPuddle | One continuous deep-teal24 top at.012, buried variation only; follows the removable native liquid owner. |

IDs are `sumphold-{family}-{0…3}`, 24 entries. Ground alone has kind`ground`;
fixtures and the actor remain entity models. This kit deliberately does not claim
Duckboard, PeatBank, Reeds, Grass, StoneFloor or generic village
services. Parent integration reuses shipped native art for those exact owners,
including the existing Wellmeet furniture/resident/marker kit and appropriate
Sodden/Spread models. No shape is recreated by replaying the old layout at runtime.

## Offline generation and budgets

Call `CavesOfOoo.Editor.SumpholdVoxelKitBuilder.Run()` in the isolated editor.
It rebuilds meshes/prefabs in place under `Assets/Resources/SumpholdVoxel3D`,
retains existing GUIDs, validates the library and writes no scene. Each prefab
has one combined readable mesh, the shared ring material and identity transform.
There are no colliders, scripts, Unity lights, rigs, clips or sockets in the kit.

The fixed ceiling is ten boxes / 240 vertices and two swatches per mesh, with
horizontal bounds contained by one native cell. The long hull axis is localX;
the rain hood and actor face local+Z. Runtime/native placement owns orientation
and portable actor identity. The visible hull frame stays open between its ribs,
keel and trestles; it is not a hidden solid shell or a bridge implementation.

## Verification and self-review

- WB01 actual three-seed census and native blueprint/Part source informed the
  five-family budget; late reskinned residents are guarded in shared integration.
- WB04 captured both missing kit classes before production. Initial contracts and
  meaningful anatomy tests were authored before geometry.
- Tests now pin all24 assets, four distinct shapes, strict aliases and foreign-owner
  controls, one-cell/vertex/swatch limits, quiet ground, prefab/material/metadata
  identity, malformed-copy rejection and forbidden extra Lights.
- Ray tests require a raised keel, gaps in the bare hull and two actual trestle
  supports, with open-space and occupied controls. The records remain below the
  rain hood with an open working front; cutters show paired tall boots and a
  separate low spade blade. Wall tests pin broad continuity and muted colors.
- Current source-only combined audit:64 meshes across both kits, max240 vertices/two
  swatches/single-cell bounds and six unique copied source metas. Exported-asset
  and final native visual verification are separate parent-owned gates.
- WB07 original art tests passed. Independent WB08 Sumphold-64 review confirmed
  the converted water looked like green checked ground. WB09 captured the
  missing water family and24-versus20 count RED before production. New tests
  pin deep-teal color, full-cell continuity, constant low height and true owner
  metadata, with an outside-cell empty-space countercontrol.
- WB10 rebuilt all 64 pair models with zero C# errors and exit 0; all six native
  preview rows report zero missing meshes and zero unmodeled owners. Independent
  inspection covered Sumphold seeds 64 and 1729. Pools read as deep water, without
  the old green checker texture; dry boards, dark banks, reeds and work areas
  remain distinct. No serious art/material defect was found in these views.
- 🧪 One-cell tools, faces and record bindings remain small at full-chunk framing.
  Static review cannot establish live input feel, native light animation or
  sustained FPS. Those claims are not made from source or static art tests.
- ⚪ The descriptive hull, toll stand and cutter spade receive no new gameplay
  Parts; existing native water/destruction/interaction rules remain authoritative.

## Implementation log

- 2026-09-15: Read CLAUDE.md, pair plan, Sumphold native plan, blueprints, actual
  profile and current map. Corrected biome/tier, vehicle/toll/destruction claims,
  dry-board semantics and descriptive-spade versus actual-loadout assumptions.
- 2026-09-15: Wrote RED asset/anatomy contracts; root captured actual WB04 compile
  RED. Authored five four-variant model families and strict source validation.
  Source audit passes; first isolated build/preview and tests are now parent-owned.
- 2026-09-15: Original kit passed WB07; inspected WB08 Sumphold-64 and recorded
  the water readability defect. Added tests, root captured WB09 actual RED, then
  appended four water variants. The existing 20 geometry methods are untouched.
- 2026-09-15: WB10 exported the expanded kits and produced six complete native
  preview rows. Independently inspected both areas at seeds 64 and 1729; the
  water readability correction is accepted within static-view limits. Root
  owns final test totals and the existing-art rebuilt-byte comparison.

Files: SumpholdVoxelKitLibrary.cs, SumpholdVoxelKitBuilder.cs,
SumpholdVoxelKitTests.cs, their three metas, this document and generated resources
exported by the parent task. No shared integration source is edited by this kit.

Build preservation receipt: root captured the actual first48 generated assets and
metadata (198 files) from the isolated WB06 export in
`cache/workshop-bog/initial48-artifacts.json` before the expansion. The main
project had no generated kit directories at this point; its empty snapshot was
not accepted as evidence. Root owns the rebuilt-byte comparison.
