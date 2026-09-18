# Corrected export + native ground material/culling review

## Corrected raw FBX gate

Read-only scan of all218 adopted `ArtSource/SpawnRing3D/models/*.fbx` passed: no zero-area polygon-fan triangles, no wholly zero-area polygons; every triangle count matches current catalog. Comparison with first failed-import before snapshot proves **exactly four FBXs changed**, the four GlowQuartzVein variants. All four now contain210 nondegenerate triangles with no coincident polygon points. Catalog SHA256 is exactly `2086b05ccf5c800ae2a8276a6c96ea337e805a3b9824f18319b5eff0d80bc16a`. See `corrected-all-source-topology-scan.json` for individual hashes. No Unity/Blender process or repository write was performed.

## Bounded source review: no source-proved must-fix

Read complete `SpawnRing3DGroundPatches`, material ownership/preparation/camera portions of `NativeZone3DRenderSurface`, ring binding/recipe material policy, shared HLSL, corrected catalog/raw FBX channels and actual initially imported bush/brine prefabs/material assets.

- All141 catalog ground/entity FBXs have normals and UV0. All141 use SpawnRingPalette; only four SprayPool variants have a second SpawnRingWater mesh. Details in `ground-source-channel-scan.json`. This reads raw source channels, not in-memory Unity Mesh buffers.
- Initially imported bush/brine prefab overrides point to the exact ring palette material GUID269a9cb52f6714d59b2dd8f8cc2dd7c0. Ring water has distinct GUID1d95786395bec4325803d3faf7c690a0. Palette material references actual ring atlas; water intentionally uses white default BaseMap multiplied by its opaque teal BaseColor. Both bind the80×25 default fog asset. No village-atlas substitution in these owned assets.
- GroundPatches validates actual readable MeshRenderer meshes, exact material/submesh count, triangle topology and nonempty indices. It resolves each **actual material identity**, not merely catalog primary family, before splitting palette/water combine lists. This handles a SprayPool with palette rim and water body independently. Unsupported families fail visibly.
- Surface.MaterialFor maps both borrowed source material and its owned clone to the same clone. Thus GroundPatches obtaining clones before AddRenderer and then calling PrepareModel is intentionally idempotent; it does not throw on already-remapped materials or repeatedly clone them. Clones carry instance fog/exposure. No borrowed mesh/material mutation is present.
- Source-fragment matrices strip the prefab root and reapply its local rotation/scale once; native cell placement and patch world-to-local matrix are composed before CombineMeshes. Ordinary ground rotates only by90-degree steps. Initial imported wrapper transforms are identity; no evident double-axis transform or half-cell offset was found.
- New combined meshes use UInt32 indices and recalculate their bounds after transformed geometry is copied. Patch roots stay stable while staged child geometry/meshes replace only after successful build. Bounds span actual10×5 geometry including source overhang, so there is no hard10×5 bounding-box crop of edge foliage.
- Camera uses layer12, real forward renderer, strict overhead XZ projection and ordinary orthographic frustum. Occlusion culling is explicitly disabled; normal frustum culling still applies to combined patch bounds. There is **no per-cell CPU FOV culling or mesh LOD**: ordinary FOV is fragment clipping. Consequently catalog whole-zone triangles or visible-cell counts cannot be used to infer actual submitted triangle count.
- All palette/water shaders consume positions, normals and UV0. Source vertex colors are not read by these shaders. Generated water meshes also have normals/UV0; heterogeneous source COLOR channels do not change the intended shader inputs. Whether Unity emits any CombineMeshes channel warning remains a native/actual-Mesh gate, not source proof.
- World-space fog sampling uses floor(XZ), exact80×25 dimensions and current native coordinates. Unseen fragments are clipped, remembered RGB avoids live lighting, depth uses the same mask and shadows require currently visible alpha. PrepareModel sets static patch `_Transient=0`; transient actors/takeables are excluded from ground batches. Per-instance fog material clones preserve this separation.
- Native permanent-coating water uses the16 cardinal masks with current state; fingerprints include masks and Mark includes cardinal neighbour patches. No save seed replay is performed.

## Do not conflate two water owners

The actual `SprayPool` blueprint is Terrain with Material Water; it has **no LiquidPool part** and is not generated as a TileState-only pool. Its authored rounded basin is a native entity-owned mesh, removed with that entity/recipe. The procedural cell-wide overlay is independently owned by current permanent TileState water. Therefore retaining a SprayPool entity while removing a separate coating can legitimately retain its basin; the TileState-only fen erasure test does not prove nor require removal of the entity's authored appearance. Conversely an entity deletion must remove its basin fragments even if other current water remains. No native mechanics or ownership changes are recommended from this review.

## Concrete remaining actual-runtime controls

1. Combine a real imported SprayPool plus a native permanent-water cell in the same patch; inspect actual combined water/palette Mesh normals and UV counts, cloned material identities and Unity warnings. This exercises the imported/generated channel mixture, not just independent metadata.
2. Delete the real SprayPool entity while retaining a separate coating, then remove the coating while retaining a SprayPool negative control. Assert exact respective geometry changes and no mutation to borrowed source meshes/materials.
3. Place/import a real tall or overhanging static at a patch edge; verify rebuilt Mesh bounds enclose transformed vertices and camera-frustum edges do not slice source geometry. Pair visible/explored/unseen cells across that edge and inspect native screenshot/depth/shadow results.
4. Capture native full/low presentation with actual UI and ordinary FOV. Read real draw/triangle/CPU/GPU counters; low detail currently changes RT resolution and shadows, not mesh density. Existing shader GPU gates cover shader contracts but not aggregate ring submission cost or final visual quality.

These are bounded follow-up acceptance cases, not newly proven bugs. This review does not claim actual combined Mesh/GPU/native performance success while parent import is running.
