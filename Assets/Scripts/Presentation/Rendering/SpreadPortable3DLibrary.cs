using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
namespace CavesOfOoo.Rendering
{
    /// <summary>Persistent portable art. Recipe selection describes native item
    /// state; the receiving renderer retains world/inventory/body ownership.</summary>
    public sealed class SpreadPortable3DLibrary : ScriptableObject
    {
        public const string ResourcePath = "SpreadPortable3D/Library";
        public const string Folder = "Assets/Resources/SpreadPortable3D";
        public string SourceSha256;
        public Material Material;
        public Texture2D Palette;
        [Serializable]
        public sealed class Entry
        {
            public string Id;
            public GameObject Prefab;
            public Mesh Mesh;
            public SpawnRing3DCatalog.Model Spec;
        }
        public Entry[] Entries;
        private Dictionary<string, Entry> index;
        private HashSet<Mesh> meshes;
        public static SpreadPortable3DLibrary Load() => Resources.Load<SpreadPortable3DLibrary>(ResourcePath);
        public static bool TryRecipe(Entity owner, out string modelId) => SpreadPortableRecipes.TryRecipe(owner, out modelId);
        public Entry Find(string id)
        {
            if (id == null || !id.StartsWith("spread-portable-", StringComparison.Ordinal)) return null;
            if (index == null) Validate();
            return index.TryGetValue(id, out var entry) ? entry : null;
        }
        public bool ContainsMesh(Mesh mesh)
        {
            if (mesh == null) return false;
            if (index == null) Validate();
            return meshes.Contains(mesh);
        }
        public void Validate()
        {
            index = null; this.meshes = null;
            var ring = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            if (SourceSha256 != SpreadPortableModelIds.SourceSha256 || Entries == null
                || Entries.Length != SpreadPortableModelIds.All.Count || Material == null || Palette == null
                || !Palette.isReadable || Palette.width != 42 || Palette.height != 1
                || Palette.filterMode != FilterMode.Point || Palette.wrapMode != TextureWrapMode.Clamp
                || ring?.WorldMaterial == null || Material == ring.WorldMaterial || Material.shader != ring.WorldMaterial.shader
                || Material.GetTexture("_BaseMap") != Palette || Material.GetColor("_BaseColor") != Color.white
                || !Material.HasProperty("_FogLight") || !Material.HasProperty("_Transient"))
                throw new InvalidOperationException("Incomplete or foreign portable library.");
            var expected = new HashSet<string>(SpreadPortableModelIds.All, StringComparer.Ordinal);
            var candidates = new Dictionary<string, Entry>(StringComparer.Ordinal);
            var meshes = new HashSet<Mesh>(); var prefabs = new HashSet<GameObject>();
            foreach (var entry in Entries)
            {
                if (entry == null || entry.Id == null || !expected.Remove(entry.Id) || entry.Mesh == null
                    || !meshes.Add(entry.Mesh) || !entry.Mesh.isReadable || entry.Mesh.vertexCount < 3
                    || entry.Mesh.vertexCount > 65535 || entry.Mesh.subMeshCount != 1 || entry.Mesh.bindposeCount != 0
                    || entry.Prefab == null || !prefabs.Add(entry.Prefab) || entry.Spec == null)
                    throw new InvalidOperationException("Duplicate or malformed portable entry.");
                var root = entry.Prefab; var mesh = entry.Mesh; var spec = entry.Spec;
                var filter = root.GetComponent<MeshFilter>(); var renderer = root.GetComponent<MeshRenderer>();
                if (filter == null || filter.sharedMesh != mesh || renderer == null || renderer.sharedMaterial != Material
                    || renderer.sharedMaterials.Length != 1 || root.transform.childCount != 0
                    || root.GetComponents<Renderer>().Length != 1 || root.GetComponents<Collider>().Length != 0
                    || root.GetComponents<MonoBehaviour>().Length != 0 || root.GetComponent<Animator>() != null
                    || root.transform.localPosition != Vector3.zero || root.transform.localRotation != Quaternion.identity
                    || root.transform.localScale != Vector3.one || spec.id != entry.Id
                    || spec.path != Folder + "/" + entry.Id + ".prefab" || spec.kind != "entity"
                    || spec.materialFamily != "spread-portable" || spec.rigged || spec.rigFamily != "none"
                    || spec.clips == null || spec.clips.Length != 0 || spec.sockets == null || spec.sockets.Length != 0
                    || spec.boundsCenter != mesh.bounds.center || spec.boundsSize != mesh.bounds.size
                    || spec.triangles != (int)mesh.GetIndexCount(0) / 3)
                    throw new InvalidOperationException("Portable prefab or bounds mismatch: " + entry.Id);
                var xyz = mesh.vertices; var uv = mesh.uv;
                if (uv.Length != xyz.Length || mesh.normals.Length != xyz.Length)
                    throw new InvalidOperationException("Incomplete portable buffers: " + entry.Id);
                for (int i = 0; i < xyz.Length; i++)
                    if (!Finite(xyz[i].x) || !Finite(xyz[i].y) || !Finite(xyz[i].z)
                        || Mathf.Abs(xyz[i].x) > .501f || Mathf.Abs(xyz[i].z) > .501f
                        || xyz[i].y < -.0251f || xyz[i].y > 1.001f || !Finite(uv[i].x) || !Finite(uv[i].y)
                        || uv[i].x <= 0 || uv[i].x >= 1 || uv[i].y != .5f)
                        throw new InvalidOperationException("Invalid portable native vertex: " + entry.Id);
                candidates.Add(entry.Id, entry);
            }
            if (expected.Count != 0) throw new InvalidOperationException("Missing portable models.");
            this.meshes = meshes; index = candidates;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private void OnValidate() { index = null; meshes = null; }
    }
}
