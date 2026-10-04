using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Full and empty medicine harnesses on the original Marlback rig.
    /// The body is native anatomy; the bottle reflects actual carried inventory.</summary>
    public sealed class PatchbearerArtLibrary : ScriptableObject
    {
        public const string ResourcePath = "Patchbearer3D/Library";
        public const string SourceModel = "ring-snapjaw";
        public const string Full = "patchbearer-full", Empty = "patchbearer-empty";
        public const string ActorBlueprint = "MarlbackPatchbearer";
        public static readonly string[] ModelIds = { Full, Empty };
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
        public static PatchbearerArtLibrary Load() => Resources.Load<PatchbearerArtLibrary>(ResourcePath);
        void OnValidate() { index = null; }
        public static bool IsModelId(string id) => id == Full || id == Empty;
        public static string Blueprint(string id) => IsModelId(id) ? ActorBlueprint : null;
        public bool ContainsMesh(Mesh mesh) => mesh != null && Entries != null && Entries.Any(e => e != null && e.Mesh == mesh);
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
                    throw new InvalidOperationException("Invalid patchbearer entry.");
                var skins = entry.Prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var animators = entry.Prefab.GetComponentsInChildren<Animator>(true);
                if (skins.Length != 1 || animators.Length != 1 || skins[0].sharedMesh != entry.Mesh
                    || skins[0].sharedMaterials.Length != 1 || skins[0].sharedMaterial != Material
                    || animators[0].runtimeAnimatorController != sourceAnimator.runtimeAnimatorController
                    || animators[0].avatar != sourceAnimator.avatar || animators[0].applyRootMotion
                    || entry.Prefab.GetComponentsInChildren<Collider>(true).Length != 0
                    || entry.Prefab.GetComponentsInChildren<Rigidbody>(true).Length != 0
                    || entry.Prefab.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                    throw new InvalidOperationException("Patchbearer body must preserve the real native rig and inert asset contract.");
                var skin = skins[0];
                if (skin.bones.Length != sourceSkin.bones.Length || skin.rootBone == null || !skin.rootBone.IsChildOf(entry.Prefab.transform))
                    throw new InvalidOperationException("Incomplete patchbearer bone ownership.");
                for (int i = 0; i < skin.bones.Length; i++)
                    if (Path(skin.bones[i], entry.Prefab.transform) != Path(sourceSkin.bones[i], SourcePrefab.transform))
                        throw new InvalidOperationException("Foreign or reordered patchbearer bone.");
                foreach (string socket in new[] { "Equipment.Head", "Equipment.Hand.L", "Equipment.Hand.R", "Equipment.Back" })
                {
                    var expected = SourcePrefab.GetComponentsInChildren<Transform>(true).Single(t => t.name == socket);
                    var actual = entry.Prefab.GetComponentsInChildren<Transform>(true).Single(t => t.name == socket);
                    if (Path(actual, entry.Prefab.transform) != Path(expected, SourcePrefab.transform)
                        || actual.localPosition != expected.localPosition || actual.localRotation != expected.localRotation || actual.localScale != expected.localScale)
                        throw new InvalidOperationException("Patchbearer model changed an equipment socket.");
                }
                var clips = animators[0].runtimeAnimatorController.animationClips;
                if (clips.Length != 5 || Clips.Any(n => clips.Count(c => c != null && c.name == n && c.length > 0) != 1))
                    throw new InvalidOperationException("Five original moving clips required.");
                ValidatePair(SourceMesh, entry.Mesh);
                var spec = entry.Spec;
                if (spec.id != entry.Id || spec.sourceBlueprint != Blueprint(entry.Id) || !spec.rigged
                    || spec.rigFamily != "humanoid" || spec.kind != "actor" || spec.materialFamily != "reference-glade-palette"
                    || spec.path != "Assets/Resources/Patchbearer3D/" + entry.Id + ".prefab"
                    || spec.triangles != (int)entry.Mesh.GetIndexCount(0) / 3
                    || spec.clips == null || !spec.clips.SequenceEqual(Clips))
                    throw new InvalidOperationException("Patchbearer model metadata differs from its actual body.");
                next.Add(entry.Id, entry);
            }
            if (next.Count != 2 || !ModelIds.All(next.ContainsKey)) throw new InvalidOperationException("Both harness states required.");
            index = next;
        }
        public static void ValidatePair(Mesh source, Mesh variant) => SpreadRareMarlbackLibrary.ValidatePair(source, variant);
        static string Path(Transform bone, Transform root)
        {
            if (bone == null || !bone.IsChildOf(root)) throw new InvalidOperationException("Foreign patchbearer bone.");
            string path = bone.name;
            while (bone.parent != root) { bone = bone.parent; path = bone.name + "/" + path; }
            return path;
        }
        /// <summary>Only the current actual actor graph can claim this outfit.
        /// FindCarriedMedicine is the same read-only stock query used by native AI.</summary>
        public static string ResolveModel(Zone zone, Entity owner)
        {
            if (zone == null || owner?.BlueprintName != ActorBlueprint || !AreaCompositionScope.Allows(zone)) return null;
            var cell = zone.GetEntityCell(owner); var render = owner.GetPart<RenderPart>();
            var physics = owner.GetPart<PhysicsPart>(); var brain = owner.GetPart<BrainPart>();
            var body = owner.GetPart<Body>(); var medicine = owner.GetPart<FieldMedicinePart>();
            var manager = WorldLocationContext.For(zone);
            if (manager == null || !manager.CachedZones.TryGetValue(zone.ZoneID, out var current) || current != zone
                || cell == null || cell.ParentZone != zone || owner.SpatialZone != zone || !cell.Objects.Contains(owner)
                || render?.ParentEntity != owner || !render.Visible || render.RenderString != "s" || render.ColorString != "&w"
                || !string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant) || !string.IsNullOrEmpty(render.GlyphVariants)
                || physics?.ParentEntity != owner || physics.Takeable || physics.InInventory != null || physics.Equipped != null
                || brain?.ParentEntity != owner || body?.ParentEntity != owner || medicine?.ParentEntity != owner
                || !owner.HasTag("Creature") || owner.HasTag("Item") || owner.HasPart<SpatialFootprintPart>() || owner.HasPart<MultiCellPilotPropPart>()) return null;
            string model = medicine.FindCarriedMedicine()?.BlueprintName == "HealingTonic" ? Full : Empty;
            return Load()?.Find(model) == null ? null : model;
        }
    }
}
