# Olderdeep voxel kit

Status: complete and installed, 40 combined-mesh model variants. The two kits total 116 models. FC21b rebuild preserved all 470 asset and metadata files byte for byte; final art tests and owner-coverage gates pass. CoO-original art for native owners, not a new gameplay system or Qud port. Final results and visual limits are recorded in [the area plan](OLDERDEEP-WELLMEET-COMPOSITION-PLAN.md) and [verification receipt](Verification/VoxelWorld/FC23-closeout/README.md).

## Verified native contracts and art corrections

| Shipped source | Verified contract | Art consequence |
|---|---|---|
| `Objects.json`: TheRooted; `FoundingVillageBuilder` | One indestructible PhysicalObject fixture. Flat shins, bent knees, arched torso, chest fungus and open arms reaching east; native body cell and eastern gap remain authoritative. | A single-cell static human pose reaches local +X. No giant sculpture, animation rig, pedestal across the gap, or boss identity. |
| `FoundingVillageBuilder`; `FoundingPlumePart` | Exactly eleven walkable plume owners. Each is destructible and emits native green light. Real plume-cell sleep is session, life and reputation guarded. | Low overlapping pale clusters, one combined mesh per owner. Removing one owner removes that patch. No permanent blanket or additional light component. |
| `Objects.json`: NicheHome | A solid, examinable wall home with bedding and lamp-hook description; no bed/rest/container Part. | An open-front shallow recess with bedding. The visual opening points local +Z toward the actual chamber; it grants no new rest action. |
| `Objects.json`: PlaqueWall / PlaqueOldest | The oldest inscriptions sit at floor level; their native descriptions carry the reading. | Broad upper plaque and distinctly lower oldest plaque. No invented alphabet or text texture. |
| `Objects.json`: BeetleJar | A non-solid, non-takeable native light fixture; the descriptive crush alarm has no destruction/alarm Part. | Hanging stopped jar, dark support and warm bulb. No art-driven alarm, new light, or destructibility change. |
| FoundingListener / FoundingPlaqueTender | Living conversational actors with canonical `@` glyphs. The tender's real mineral exchange is guarded by native trust and inventory services. | Standing wall-listener with raised palm; lower kneeling/tending pose. Body variants remain stable for each native owner. |

## Public kit contract

`OlderdeepVoxelLibrary.ResourcePath` is `OlderdeepVoxel3D/Library`.
`ModelId(family, variant)` accepts exactly the listed families and variants 0–3;
invalid authoring throws. `Family(blueprint)` uses exact blueprint names and
returns null for unsupported content. `Load`, `Find` and `Validate` follow the
established native voxel-library contract.

| Ordered family | Exact native aliases | Models | Shape / palette |
|---|---|---|---|
| ground | StoneFloor, SandstoneFloor | olderdeep-ground-0…3 | Flat full-cell dark floor, slot 12; only buried thickness varies. |
| rooted | TheRooted | olderdeep-rooted-0…3 | Ten broad boxes, warm body 79 and pale chest fungus 19; open arms toward +X. |
| plume | FoundingPlume | olderdeep-plume-0…3 | Four broad overlapping forms, low green 48 / muted pale 50; .27 top and broken cell corners. |
| niche | NicheHome | olderdeep-niche-0…3 | Six-box cutaway recess, stone 56 and pale bedding 19; 1.09 top. |
| plaque | PlaqueWall | olderdeep-plaque-0…3 | Broad standing name-face, 56 / 19; 1.0 top. |
| oldest | PlaqueOldest | olderdeep-oldest-0…3 | Worn low plaque, 56 / 19; .39 top. |
| jar | BeetleJar | olderdeep-jar-0…3 | Hanging bulb below a simple support, 12 / 49; 1.34 top. |
| listener | FoundingListener | olderdeep-listener-0…3 | Standing raised palm and face, 56 / 19; 1.47 top. |
| tender | FoundingPlaqueTender | olderdeep-tender-0…3 | Low working hands and stooped face, 56 / 19; 1.10 top. |
| wall | None; scoped current SandstoneWall owners only on the founding floor | olderdeep-wall-0…3 | Two full-cell muted cave strata, 12 / 13; cutaway top 1.10. |

All 40 entries have one readable combined mesh and one identity-transform prefab.
There are no colliders, MonoBehaviours, rigs, clips or sockets in this art kit.
Only ground has metadata kind `ground`; the plume remains a native entity even
though it is walkable. Materials reference the existing ring palette directly.
No shared palette source is modified.

## Reproduction and performance

The isolated editor calls `CavesOfOoo.Editor.OlderdeepVoxelKitBuilder.Run()`.
It updates mesh and prefab assets in place under
`Assets/Resources/OlderdeepVoxel3D`, preserving existing asset GUIDs on rebuild.
It writes no scene. The generator operates offline; presentation borrows these
already-coarse combined meshes without subdividing them into cubes again.

The fixed budget is four variants per family, at most two palette swatches and
240 vertices per mesh, with horizontal bounds inside ±.5 cells. Plume cells
retain broken outer contours and a low changing profile; broad quiet lobes are
preferred to repeated tiny caps or a continuous rectangular floor patch.
Variants change shape, not the native owner.

## Verification and self-review

- FC02 captured actual missing Olderdeep/Wellmeet library compile RED before
  either production library or generator existed.
- Art tests pin exact aliases, quiet floors, one-cell bounds, combined geometry,
  source prefab/material/metadata links, strict input validation and malformed
  entry controls. Ray tests include occupied and open-space controls for the
  Rooted embrace and niche opening; the plume tests probe inside/outside cells.
- Final source-only audit: all 116 models across both kits have at most 240 vertices,
  two swatches and one-cell horizontal bounds; all six source metas match their
  same-kind templates except GUID, with no GUID collisions. This is not a Unity
  asset-test or visual pass.
- FC17 final FoundingChamber-64 was inspected independently after the earlier
  three-seed FC13 review. Muted cave stone replaces the cream/violet material;
  no remaining blocking material defect was identified. Asset/test totals are
  recorded in the parent plan, separately from this visual observation.
- 🧪 Static previews cannot establish live feel, lighting animation or FPS.
- ⚪ No new gameplay power is inferred from art: notably niche rest, jar alarm,
  Rooted combat, guest protection and native plume sleep remain separate owners.

## Implementation log

- 2026-09-15: Read the native blueprint/builders/Parts and FC01 three-seed census;
  confirmed nine families and corrected fixture/actor, plume ownership, eastward
  pose and descriptive-only jar semantics before production.
- 2026-09-15: Wrote both actual failing art fixtures; root captured FC02 RED.
  Added anatomy, openings, low-plaque and pose invariants before authoring their
  generator geometry. Library, builder, tests and six matching metas were ready for the first export.

Files: new `OlderdeepVoxelLibrary.cs`, `OlderdeepVoxelKitBuilder.cs`,
`OlderdeepVoxelKitTests.cs`, their metas and this document. Asset export and shared
recipe/presenter integration are performed by the parent task.

### FC06 native look review — initial defect and RED proposal

FC06 exported the initial kit successfully and rendered actual native chunks.
Viewed FoundingChamber-64, UsedDescent-64 and WellmeetCamp-64. The shallow joined
plume contradicted the desired organic bed: its eleven pale square tops merged
into a bright rectangular floor blanket that overwhelmed the one-cell Rooted.
The earlier continuous-flat-plume requirement was therefore the wrong art rule.
The proposed tests required three or more upward-facing height levels, broken cell corners,
a maximum .30 top, at most four broad boxes, and muted pale slot 50 over green 48.
Native per-cell ownership stayed unchanged. Production refinement was held until
FC08 captured the actual RED. Broad descent stopping ledges were readable;
foreground wall occlusion of lower niches was retained for integration review.

- 2026-09-15: FC08 captured the actual clustered-plume failure before production.
  Applied the four-form profile and pale 50/green 48 palette only after RED. The
  108-model source audit (36 Olderdeep +72 Wellmeet) again passes one-cell bounds,
  at most 240 vertices, two swatches and unique copied source metas. Refined
  generated assets and final Unity/camera verification were still pending at FC08.

### FC13 independent native camera review

Reviewed FoundingChamber64/1729/729490642, ShelteredMouth64 and UsedDescent64.
The plume now has low changing contours and separated cell edges. At the fixed
camera scale its eleven-cell grouping is still visibly regular; it no longer
forms a single flat sheet. The one-cell Rooted and eastward gap remain intact.
The used-descent platforms and mouth rim remain legible native structures.

The integration's borrowed Cathedral cutaway exposed the southern niche backs,
but its pale cream/violet palette made the entire chamber rock much too bright.
This was a material regression, not a reason to change native walls. The proposed
Olderdeep `wall` quartet kept height 1.1 and reused Ginmere cliff palette 12/13
in two full-cell strata. `Family("SandstoneWall")` must remain null so generic
Olderdeep descent cliffs keep their height; integration chooses this model only
for the founding floor. The 40-entry and actual palette/height/continuity tests
were saved for FC15 RED; production was held until that run. Southern niche fronts face north
away from the camera, so visible niche backs are not a claim of frontal bedding
visibility. Static images do not establish live input feel or frame rate.

### Final source handoff after FC15

FC15 captured actual wall/marker/alias RED with zero C# errors before production.
The final Olderdeep library contains 40 entries. Added only the scoped wall
family and its two muted full-cell strata; generic SandstoneWall remains unclaimed
so the descent keeps its tall native cliff presentation. Parent integration owns
that finite floor selection and generated-asset execution. The other 36 source
models are unchanged by this last addition.

The final source audit spans 116 meshes across both kits: maximum 240 vertices,
maximum two swatches, every horizontal bound inside one native cell, six source
metas matching same-kind templates except unique GUID, and no detected collisions.
No additional visual refinements are proposed after this material correction.
At the FC15 source handoff, final export, tests and image acceptance were still
pending. The source audit was not represented as those runtime results.

### FC17 final visual acceptance

Independently viewed [FoundingChamber-64](Verification/VoxelWorld/FC17-final-preview/FoundingChamber-64.png).
The founding chamber now retains the muted cave-stone palette while its cutaway
height exposes the lower row of niche backs. The cream/violet material regression
is absent. Pale low plume clusters remain the local visual accent; the Rooted's
small native body and eastern gap remain distinct. Its eleven-cell grouping is
still regular at this camera scale, consistent with the retained native cells.
No remaining blocking material defect was identified in this final view.

This final pass covers this image only; the three-seed composition review is the
separate FC13 record above. Southern niche fronts face away from the camera, so
visible backs do not establish frontal bedding visibility. No live feel, animation
quality or sustained frame-rate claim follows from these static images. Parent
verification owns the final asset and test totals.
