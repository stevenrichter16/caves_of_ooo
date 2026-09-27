# Grove lantern-moth: native silhouette refinement

Scope: only GroveLanternMoth's source sculpt and exact source asset's offline
voxel density. Keep its five bones, Idle/Walk clips, passive/no-light gameplay,
current source/native asset keys, two-color object budget and every other model.
The parent opened a narrow publication window after the recorded native RED. Source and editor changes are published; native import and visual acceptance remain pending.

## Verified source corrections before implementation

| Observation | Verified cause / bounded seam |
|---|---|
| First live native moth is a pale square | Native receipt records .125 world pitch, .715 source width, .105 thickness: about six cells across. |
| Source contains three bands but live image does not | Each band is .024 wide, below the adopted pitch; .01 antennae also vanish. |
| Fore/hind wing outlines merge | Source lobes overlap along the body axis; fine names/bones do not establish a visible notch. |
| Global density must remain coarse | Keep SelectWorldPitch unchanged. A new source-aware wrapper narrows only the exact skinned moth asset path, not filename prefix, all thin creatures or equipment. |
| Native geometry tests rebuild every actual skin | The finer mesh must remain an exact ordinary voxel bake at its recorded pitch; no hand-authored replacement can bypass that gate. |
| Source atlas / object paint remain governed by existing code | Use existing pale/dark swatches, no atlas update; keep two-color reduction and all face/bone/palette controls. |

## Test-first plan

Execute real source function using lightweight geometry recording stubs, before
editing: assert a real fore/hind notch, at least .05-wide bars, and dark raised
body above wings. Pair with unchanged five bones and complete other-source hash
proof. Record RED, then sculpt only that function. Source geometry can prove
sizes/topology intentions, not final voxel shape.

Native fixture will pin exact moth-only source pitch, ordinary same-size foreign
asset / wrong-case / rigid / held controls, invalid dimensions preserving prior
rejection, adopted live source binding/bone coverage, palette budget, actual
fresh-bake equality and a visible notch in sampled occupied voxel cells. Root
owns actual native RED/GREEN and native isolated scene capture. First stage repair
keeps the untouched player separate; it does not hide overlap by deleting owners.

## Publishing bounds

No changes to Objects.json or native map generation. Generator/export outputs
stay under /tmp until a verified one-row catalog merge, one FBX export and source
preservation hash report are ready. Parent imports exactly the moth, then bakes
only its exact source asset. No unrelated model regeneration/publication.


## Implementation / evidence

Actual source RED:4 cases,3 failures (fore/hind overlap, .024-wide bars, body only
.021 above wings) and the unchanged-five-bone control passes. Source minimum
repair passes4/4; all8 existing original-creature source checks also pass.
Actual native RED executes7 cases:6 intended failures and the existing
bone/palette control passes (`OriginalEnemies/Art/native-moth-refinement-red.*`).
The native test-only fixture compiles against actual imported Unity assemblies;
its pixel/geometry assertions still require native GREEN.

The generator changes only lantern_moth(). Four separate pale wing lobes have a
real fore/hind notch; six .060-wide dark bands and a raised dark body survive a
candidate .0625-cell bake more plausibly than the previous six-cell-wide wafer.
The existing five bones/bindposes, sockets, common exported clips, passive native
blueprint and no-light gameplay are unchanged. Existing source palette bytes
match exactly; no new atlas or per-frame geometry is introduced.

SelectNativeWorldPitch calls the unchanged generic validator/policy first, then
uses .0625 only for the exact case-sensitive moth FBX path when actually skinned
and not equipment. Foreign/similarly named/rigid/held sources retain ordinary
policy. The offline builder passes the actual source path. Independent read-only
review found no concrete P0-P2 issue in this scope guard or validation ordering.
Offline compilation of wrapper/builder against actual Unity assemblies passed
with zero errors; it does not establish native baking or appearance.

The private export wrote one FBX. A one-row catalog merge preserves all other
221 records. 444 unrelated source/native FBX and palette hashes match the
pre-change snapshot. The editable master blend replaces only the moth collection;
all221 unrelated collection geometry/UV/normal/weight/bone signatures match.
Evidence is under `OriginalEnemies/Art/MothRefinement/`. The source preview shows
sculpt geometry only; it is not the adopted voxel mesh or gameplay screenshot.

Parent native import, in order:

1. `SpawnRing3DAssetBuilder.Build("ArtSource/SpawnRing3D", "Docs/Verification/DensityCompletion/OriginalEnemies/Art/MothRefinement/native-source-import.json", new[]{"ring-grove-lantern-moth"})`
2. `VoxelWorldMeshBuilder.Build("Docs/Verification/DensityCompletion/OriginalEnemies/Art/MothRefinement/native-voxel-import.json", new[]{"Assets/Art3D/SpawnRing/Models/ring-grove-lantern-moth.fbx"})`

Both builders are in CavesOfOoo.Editor. Import only in clean Edit mode, after
compilation. Run new seven-case native fixture plus existing density/palette/
original-enemy rendering controls, then the separated real-owner audit. Actual
baked notch, dark/light separation, wing motion and unobstructed visual clarity
remain acceptance gates. The source preview and passing rig checks cannot close
them. The glade's five Marlback appearance assets are not rebuilt by this change.

## Actual imported and native visual acceptance

The exact one-source and one-mesh import passed with preserved borrowed assets
and GUIDs. The moth has816 voxel triangles, five bones, two paint colors and
.0625 world pitch. All7 new native cases pass, and the broader actual native
density/palette/original-art selection passes134/134 (MothRefinement/native-related-green).

Native/600fc89c6b1843efb4f399b603f962b7 completes75/75 checks,0 subscribed errors,
23 captures in8.436s. Root viewed all six separated world-idle images and the
moth walking frame. Owners no longer overlap the player; real carried gear is
visible. The moth now shows separate wing lobes with a contrasting body rather
than the previous solid pale square. These limited visual corrections are
accepted; the broader reference-glade humanoid/ground pass remains separate.
The live model/bindpose/animation/picking/visibility checks still use native
owners and the original loadouts. This staged harness is not combat or encounter
balance evidence. Exact editor/save/input restoration and actual log byte range
are archived beside the report.
