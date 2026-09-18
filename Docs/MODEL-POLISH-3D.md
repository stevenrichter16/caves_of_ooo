# Shipping 3D model polish

Status: baseline and first surface pass in progress. CoO-original art; no Qud code-parity claim.

Scope: inspect the 123 village, 218 ring and 51 pilot models, retaining the approved carved miniature style. First improve surface readability through controlled sculpt normals, then inspect characters/equipment and hero scenery near the current 56° camera. This is a refinement of current assets, not replacement of the native world with static scenery. Spell effects and approved audio remain intact.

## Verification sweep

| Assumption | Verified correction |
| --- | --- |
| Source custom normals automatically survive export | Both Village3D/build_scene.py and SpawnRing3D/mesh_kit.py assemble static meshes without copying corner normals. The polish postprocess must explicitly carry transformed normals through FBX. |
| Raising detail means adding triangles | Current meshes already have rich silhouettes. The surface pass preserves positions, triangles, UVs, materials, rig weights, sockets and clips exactly; low-angle normal blending reduces triangulation glare. |
| Native materials use vertex paint | Village3DCommon.hlsl samples palette UVs and imported normals; it does not consume vertex color. Vertex tint alone would be a dead art change. |
| Pilot can be smoothed indiscriminately | Sharp chips and concave occupied-cell boundaries must remain. Blend only incident faces within a bounded angle; never weld or move vertices. |
| Blender preview establishes game appearance | It does not. Actual Unity import, library tests and native captures are separate gates. |

## Implementation stages

1. Preserve current source bundles and inventory; write and run normal-contract RED probes and counterchecks.
2. Build a deterministic Blender postprocess with family-aware hard-edge protection; generate candidate bundles outside Assets. Retain full editable source and per-model changed/unchanged ledger.
3. Render matched before/after model galleries near56°. Review stone, wood, foliage, props, people, animals and equipment. Reject generic melted/plastic treatment.
4. Reimport FBX and check normals, bounds, triangles, UVs, rigs and clips. Import accepted bundles through existing Unity builders, preserving GUIDs and all native ownership.
5. Full regression, native render/readability inspection, cold-eye review and living work log.

## Performance and gates

No runtime algorithm, material slot, texture, geometry or draw-count increase for the normal pass. Imported vertices can split at hard normals; measure this rather than infer it from unchanged source vertices. Geometry-bound tests protect pathfinding/picking/displacement and independent destruction by retaining original body/collision meshes. Save migrations are excluded by user instruction. Dedicated offline adversarial probes cover malformed vectors, degeneracy, hard-edge separation, scale, winding, disconnected pieces and determinism. Existing native integration tests continue to own gameplay.

## Review / honesty

Pending. Subjective feel cannot be proven with normal-vector assertions. Before/after renders, actual imported data and native screenshots will be reported separately. Existing unrelated work is not staged or reverted.

### First candidate review

- P01 actual RED: missing sculpt-normal helper before implementation. P02:24/24 offline math/adversarial cases GREEN.
- P03 actual FBX RED: the authored30° corner normal reimported face-aligned (dot1.0). P04: both exporters retain it (dot0.866033). Static assembly now copies normals using inverse-transpose transforms, preserving nonuniform-scale semantics.
- Pilot v1 softened chipped stone but flattened tiny ochre leaves; excluded leaf/scrub/fibre/grain/feather/glint/lichen/flower detail. Ring v2 exposed the same issue on Sari scale plates. Final v2 recipe explicitly excludes those plates and blends only20% of the area-weighted correction into existing organic normals. Original organic curvature therefore remains dominant.
- Readiness correction: Village3D's source directory contains its editable master and manifest but no models directory. Its existing imported FBXs supplied the preserved baseline; no absent assets were invented. Ring and pilot have complete source model directories.
- Every model is inspected by the source ledger. Deliberate unchanged models include native ground overlays/detail patches and pieces with no useful normal change. This is a surface-refinement wave, not392 bespoke resculpts. All mesh positions/triangles/UVs and native recipes remain exact.

### Export-order findings

- Original imported barrel0's iron vertices all occupied z−.037..+.037: both hoops were at floor level. Correct source-master hoops occupy .093..167 and .683..757, centered .13/.72. Static export now updates pending world matrices before reading them; catalog measurement also updates before the first model. The barrel0 record now matches its .923height and .4615center. Footprint and triangle count remain unchanged.
- Olive character's original manifest claimed height .83 centered at0, while both original and polished FBXs measure height1.73 centered .845, identical to the other three palette variants. Corrected this stale bounds record; its geometry/rig did not change.
- P11/P12 translation probes initially checked mesh-local coordinates after FBX axis conversion. That was an invalid coordinate-space assertion. P13 uses world coordinates and deliberately removes the update guard, reproducing the lost translation; P14 passes both exporters with the guard. This corrected probe followed the fix; the actual barrel asset diagnosis preceded it. No false claim of pristine TDD for that corrected assertion.
- Roundtrip inspector now clears FBX-import-selected animation state before measuring bind-pose bounds. The olive mismatch persisted and was traced to metadata, not dismissed as animation. Failed receipts are retained.

Source topology and UVs remain unchanged. Runtime geometry differs only for barrel0's previously misplaced component transforms; bounds metadata also corrects the olive character. These are explicit exceptions to the original blanket preservation target.

### Strict imports and semantic review

All392 real FBX reimports pass topology counts, UV presence, finite unit normals, bind bounds, rig/sockets and five clip contracts. All three Unity builders passed with0 C# errors. Native tests/look-pass remain pending.

Independent original-vs-candidate FBX comparison passed all123 village and218 ring assets, preserving positions, polygon topology, UV/colors/material assignment, skin weights, skeleton/rest/socket matrices and sampled animation curves within2e-5. Barrel0's known translated pieces are the sole geometry exception. Pilot's first comparison caught4 Tar material regressions: generic Water-name fallback had selected PilotPalette. Replaced it with explicit required PilotTar lookup; never silently substitute a material. Reexported only those4 models and repeated their whole-kit semantic/import gates. No shader behavior was changed to disguise an asset issue.

Imported unique triangle count remains621,017 and renderer count406. Imported vertex count changes829,297→848,265(+2.29%) because hard normal splits affect Unity vertices; this is not a claim of zero vertex cost or improved frame rate. Latest historical strict-import receipts provide the comparison. Sustained native workload remains the performance gate.

### Native acceptance and closeout of this asset wave

- The final pilot semantic audit passes all51 assets after the explicit Tar
  material fix. All392 source FBXs match their imported copies byte-for-byte;
  no metadata changed since import baseline, zero GUID collisions, main scene
  unchanged. Unity's automatic QualitySettings schema rewrite was restored only
  after verifying exact normalization against the preserved original.
- Village native initially passed34/35: the old audit counted only pending buses
  and sprite atoms inside the cast callback, missing committed native3D atoms.
  Added native-backend observation during the ordinary post-cast input frames.
  This is an observer correction, not a spell/gameplay change. The next native
  run passes35/35, real FX, all screenshots,75-second workload and cleanup.
- That run's strict wrapper remains **FAIL** due to MCP startup connection and
  Unity Package Manager authentication errors in the complete Editor log.
  Gameplay report,35 named assertions and all wrapper receipt checks pass;
  neither fact erases the log-level failure. Raw archive:
  native-village/20260912T034618Z-native-b3ffdeecbd7141cd85bf8f2bfe91f05f.
- Pilot native run f366498a6c9f458e816cb91a73eeaac4 passes26 checks and the
  four20-second measured workload phases, including independent destruction,
  transfer and restoration. Cleanup confirms private save deletion, preferences
  and scene restoration,0 unexpected gameplay errors and0 C# errors.
- P15 full EditMode:11,537 total,11,506 pass,31 failures. Exact failure names AND
  messages match A00; zero C# errors. This is unchanged baseline, not all-green.
- Viewed actual village new-game and pilot south-entry GameViews. They show
  current camera, native visibility and imported models; the selected pilot
  entry frame sees only part of the chunk through FOV. Blender galleries verify
  surface changes more clearly. No subjective feel or frame-rate improvement
  is inferred from these screenshots or the unpaired sustained profile.

Cold-eye findings resolved: leaf/scale smoothing, static normal loss, original
barrel component transforms, stale olive metadata, candidate Tar material
assignment and stale native-FX audit observer. Geometry/UV/material/rig semantic
comparison,24 normal probes and real FBX mutation checks complement the native
regression. No new simulation behavior or save schema is introduced.

The317-model polish wave is imported and gameplay-verified with the explicit
Editor-log qualification above. The user's expanded all-world conversion is a
separate ongoing feature in WORLD-ART-3D-CONVERSION.md; this closeout does not
claim all2D content has become3D.
