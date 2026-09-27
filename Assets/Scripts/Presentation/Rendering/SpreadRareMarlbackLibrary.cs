using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Two explicit physical variants on the existing original Marlback
    /// rig. Missing optional art never disables unrelated Spread presentation.</summary>
    public sealed class SpreadRareMarlbackLibrary : ScriptableObject
    {
        public const string ResourcePath = "SpreadRareMarlback3D/Library";
        public const string SourceModel = "ring-snapjaw";
        public static readonly string[] ModelIds = { "spread-rare-hurdle-cutter", "spread-rare-ditch-mate" };
        static readonly string[] Clips = { "Idle", "Walk", "Interact", "Attack", "Hit" };
        [Serializable] public sealed class Entry
        {
            public string Id;
            public GameObject Prefab;
            public Mesh Mesh;
            public SpawnRing3DCatalog.Model Spec;
        }
        public Entry[] Entries;
        public GameObject SourcePrefab;
        public Mesh SourceMesh;
        public Material Material;
        Dictionary<string, Entry> index;
        public static SpreadRareMarlbackLibrary Load() => Resources.Load<SpreadRareMarlbackLibrary>(ResourcePath);
        void OnValidate() { index = null; }
        public static string Blueprint(string id)
            => id == ModelIds[0] ? "SpreadHurdleCutter" : id == ModelIds[1] ? "SpreadDitchMate" : null;
        public static string Model(string blueprint)
            => blueprint == "SpreadHurdleCutter" ? ModelIds[0] : blueprint == "SpreadDitchMate" ? ModelIds[1] : null;
        public static bool IsBlueprint(string blueprint) => Model(blueprint) != null;
        public Entry Find(string id)
        {
            if (id == null || Blueprint(id) == null) return null;
            if (index == null) Validate();
            return index.TryGetValue(id, out var entry) ? entry : null;
        }
        public void Validate()
        {
            index = null;
            var ring = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            var glade = ReferenceGladeVoxelLibrary.Load();
            var paint = glade?.ActorPaints?.SingleOrDefault(p => p.ModelId == SourceModel);
            if (ring == null || glade == null || paint == null || SourcePrefab != ring.FindModel(SourceModel)
                || SourceMesh != paint.Painted || Material != glade.Material || Entries == null || Entries.Length != 2)
                throw new InvalidOperationException("Exact original Marlback source and approved palette required.");
            var sourceSkin = SourcePrefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
            var sourceAnimator = SourcePrefab.GetComponentInChildren<Animator>(true);
            var next = new Dictionary<string, Entry>(StringComparer.Ordinal);
            foreach (var entry in Entries)
            {
                if (entry == null || Blueprint(entry.Id) == null || next.ContainsKey(entry.Id)
                    || entry.Prefab == null || entry.Prefab == SourcePrefab || entry.Mesh == null || entry.Spec == null)
                    throw new InvalidOperationException("Invalid rare Marlback entry.");
                var skins = entry.Prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var animators = entry.Prefab.GetComponentsInChildren<Animator>(true);
                if (skins.Length != 1 || animators.Length != 1 || skins[0].sharedMesh != entry.Mesh
                    || skins[0].sharedMaterials.Length != 1 || skins[0].sharedMaterial != Material
                    || animators[0].runtimeAnimatorController != sourceAnimator.runtimeAnimatorController
                    || animators[0].avatar != sourceAnimator.avatar || animators[0].applyRootMotion
                    || entry.Prefab.GetComponentsInChildren<Collider>(true).Length != 0
                    || entry.Prefab.GetComponentsInChildren<Rigidbody>(true).Length != 0
                    || entry.Prefab.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                    throw new InvalidOperationException("Rare body must preserve the real native rig and inert asset contract.");
                var skin = skins[0];
                if (skin.bones.Length != sourceSkin.bones.Length || skin.rootBone == null || !skin.rootBone.IsChildOf(entry.Prefab.transform))
                    throw new InvalidOperationException("Incomplete rare bone ownership.");
                for (int i = 0; i < skin.bones.Length; i++)
                    if (Path(skin.bones[i], entry.Prefab.transform) != Path(sourceSkin.bones[i], SourcePrefab.transform))
                        throw new InvalidOperationException("Foreign or reordered rare bone.");
                foreach (string socket in new[] { "Equipment.Head", "Equipment.Hand.L", "Equipment.Hand.R", "Equipment.Back" })
                {
                    var expected = SourcePrefab.GetComponentsInChildren<Transform>(true).Single(t => t.name == socket);
                    var actual = entry.Prefab.GetComponentsInChildren<Transform>(true).Single(t => t.name == socket);
                    if (Path(actual, entry.Prefab.transform) != Path(expected, SourcePrefab.transform)
                        || actual.localPosition != expected.localPosition || actual.localRotation != expected.localRotation || actual.localScale != expected.localScale)
                        throw new InvalidOperationException("Rare model changed an equipment socket.");
                }
                var clips = animators[0].runtimeAnimatorController.animationClips;
                if (clips.Length != 5 || Clips.Any(n => clips.Count(c => c != null && c.name == n && c.length > 0) != 1))
                    throw new InvalidOperationException("Five original moving clips required.");
                ValidatePair(SourceMesh, entry.Mesh);
                var spec = entry.Spec;
                if (spec.id != entry.Id || spec.sourceBlueprint != Blueprint(entry.Id) || !spec.rigged
                    || spec.rigFamily != "humanoid" || spec.kind != "actor" || spec.materialFamily != "reference-glade-palette"
                    || spec.path != "Assets/Resources/SpreadRareMarlback3D/" + entry.Id + ".prefab"
                    || spec.triangles != (int)entry.Mesh.GetIndexCount(0) / 3
                    || spec.clips == null || !spec.clips.SequenceEqual(Clips))
                    throw new InvalidOperationException("Rare model metadata differs from its actual body.");
                next.Add(entry.Id, entry);
            }
            if (next.Count != 2 || !ModelIds.All(next.ContainsKey)) throw new InvalidOperationException("Both rare bodies required.");
            index = next;
        }
        public static void ValidatePair(Mesh source, Mesh variant)
        {
            if (source == null || variant == null || source == variant || !source.isReadable || !variant.isReadable
                || source.subMeshCount != 1 || variant.subMeshCount != 1 || source.blendShapeCount != 0 || variant.blendShapeCount != 0
                || variant.vertexCount <= source.vertexCount + 23 || variant.vertexCount > source.vertexCount + 512
                || !source.vertices.SequenceEqual(variant.vertices.Take(source.vertexCount))
                || !source.normals.SequenceEqual(variant.normals.Take(source.vertexCount))
                || !source.uv.SequenceEqual(variant.uv.Take(source.vertexCount))
                || !source.boneWeights.SequenceEqual(variant.boneWeights.Take(source.vertexCount))
                || !source.bindposes.SequenceEqual(variant.bindposes)
                || !source.triangles.SequenceEqual(variant.triangles.Take(source.triangles.Length)))
                throw new InvalidOperationException("Variant changed original body buffers or lacks physical additions.");
            foreach (int index in variant.triangles.Skip(source.triangles.Length))
                if (index < source.vertexCount || index >= variant.vertexCount) throw new InvalidOperationException("Invalid added physical-form triangle.");
            if (variant.boneWeights.Length != variant.vertexCount || variant.uv.Length != variant.vertexCount)
                throw new InvalidOperationException("Incomplete rare geometry buffers.");
            foreach (var weight in variant.boneWeights)
                if (weight.weight0 != 1 || weight.weight1 != 0 || weight.weight2 != 0 || weight.weight3 != 0
                    || weight.boneIndex0 < 0 || weight.boneIndex0 >= source.bindposeCount)
                    throw new InvalidOperationException("Physical marks must follow actual rigid bones.");
            foreach (var uv in variant.uv)
            {
                float swatch = uv.x * 24 - .5f;
                if (!Finite(uv.x) || !Finite(uv.y) || uv.y != .5f || Mathf.Abs(swatch - Mathf.Round(swatch)) > .0001f || swatch < 0 || swatch > 23)
                    throw new InvalidOperationException("Rare body left approved palette swatches.");
            }
            foreach (var point in variant.vertices)
                if (!Finite(point.x) || !Finite(point.y) || !Finite(point.z)) throw new InvalidOperationException("Nonfinite rare body.");
        }
        static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
        static string Path(Transform bone, Transform root)
        {
            if (bone == null || !bone.IsChildOf(root)) throw new InvalidOperationException("Foreign rare bone.");
            string path = bone.name;
            while (bone.parent != root) { bone = bone.parent; path = bone.name + "/" + path; }
            return path;
        }
        internal static SpawnRing3DRecipe Refine(Zone zone, Entity owner, SpawnRing3DRecipe native)
        {
            string model = Model(owner?.BlueprintName);
            if (model == null || !SpreadPresentationScope.IsActive(zone) || !ReferenceEquals(native.Owner, owner)
                || native.Failure != null && native.Failure != "unmodeled-native-blueprint") return native;
            var render = owner.GetPart<RenderPart>(); var physics = owner.GetPart<PhysicsPart>(); var brain = owner.GetPart<BrainPart>();
            var cell = zone.GetEntityCell(owner);
            if (cell == null || !cell.Objects.Contains(owner) || render == null || render.ParentEntity != owner || !render.Visible
                || render.RenderString != "s" || render.ColorString != "&w" || !string.IsNullOrEmpty(render.VisualID)
                || !string.IsNullOrEmpty(render.VisualVariant) || !string.IsNullOrEmpty(render.GlyphVariants)
                || physics == null || physics.ParentEntity != owner || physics.Takeable || physics.InInventory != null || physics.Equipped != null
                || brain == null || brain.ParentEntity != owner || !owner.HasTag("Creature") || owner.HasTag("Item")
                || owner.HasPart<SpatialFootprintPart>() || owner.HasPart<MultiCellPilotPropPart>()) return native;
            // A missing optional pack refuses just these owners through native
            // fallback. It never becomes an ordinary biome bind prerequisite.
            if (Load()?.Find(model) == null) return native;
            return new SpawnRing3DRecipe(owner, model, native.ComponentId,
                Village3DProjection.CellCentre(cell.X, cell.Y), true, false, quarterTurns: native.QuarterTurns);
        }
    }
}
