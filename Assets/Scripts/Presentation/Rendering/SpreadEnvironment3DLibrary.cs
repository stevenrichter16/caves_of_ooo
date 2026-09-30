using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Rendering;
namespace CavesOfOoo.Rendering
{
    /// <summary>Borrowed persistent scenery art. Gameplay owns harvesting,
    /// collision, visibility, quests and light; this library changes none of them.</summary>
    public sealed class SpreadEnvironment3DLibrary : ScriptableObject
    {
        public const string ResourcePath = "SpreadEnvironment3D/Library";
        public const string Folder = "Assets/Resources/SpreadEnvironment3D";
        public const string ReviewedSourceSha256 = "a0848ed10570af674ab1a76e14775119180036853bd3a81534cb5d48de784f5b";
        public string SourceSha256;
        public Material Material;
        [Serializable] public sealed class Entry
        {
            public string Id;
            public GameObject Prefab;
            public Mesh Mesh;
            public SpawnRing3DCatalog.Model Spec;
        }
        public Entry[] Entries;
        private Dictionary<string, Entry> index;
        private HashSet<Mesh> meshes;
        public static SpreadEnvironment3DLibrary Load() => Resources.Load<SpreadEnvironment3DLibrary>(ResourcePath);
        public Entry Find(string id)
        {
            if (!SpreadEnvironmentSource.IsModelId(id)) return null;
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
            index = null; meshes = null;
            var glade = ReferenceGladeVoxelLibrary.Load();
            if (SourceSha256 != ReviewedSourceSha256 || Entries == null || Entries.Length != SpreadEnvironmentSource.ModelIds.Count
                || glade == null || Material == null || Material != glade.Material)
                throw new InvalidOperationException("Incomplete or foreign scenery library.");
            glade.Validate();
            var remaining = new HashSet<string>(SpreadEnvironmentSource.ModelIds, StringComparer.Ordinal);
            var next = new Dictionary<string, Entry>(StringComparer.Ordinal);
            var nextMeshes = new HashSet<Mesh>(); var prefabs = new HashSet<GameObject>();
            foreach (var entry in Entries)
            {
                if (entry == null || entry.Id == null || !remaining.Remove(entry.Id) || entry.Mesh == null
                    || !nextMeshes.Add(entry.Mesh) || entry.Prefab == null || !prefabs.Add(entry.Prefab) || entry.Spec == null)
                    throw new InvalidOperationException("Invalid scenery model identity.");
                var mesh = entry.Mesh; var root = entry.Prefab; var spec = entry.Spec;
                var filter = root.GetComponent<MeshFilter>(); var renderer = root.GetComponent<MeshRenderer>();
                if (!mesh.isReadable || mesh.vertexCount < 24 || mesh.vertexCount > 100 * 24 || mesh.vertexCount % 24 != 0
                    || mesh.subMeshCount != 1 || mesh.bindposeCount != 0 || mesh.GetTopology(0) != MeshTopology.Triangles
                    || mesh.GetIndexCount(0) != (uint)(mesh.vertexCount / 24 * 36)
                    || filter == null || filter.sharedMesh != mesh || renderer == null || renderer.sharedMaterials.Length != 1
                    || renderer.sharedMaterial != Material || !renderer.enabled || !root.activeSelf || root.transform.childCount != 0
                    || root.GetComponents<Renderer>().Length != 1 || root.GetComponents<Collider>().Length != 0
                    || root.GetComponents<MonoBehaviour>().Length != 0 || root.GetComponent<Animator>() != null
                    || root.transform.localPosition != Vector3.zero || root.transform.localRotation != Quaternion.identity
                    || root.transform.localScale != Vector3.one || spec.id != entry.Id || spec.path != Folder + "/" + entry.Id + ".prefab"
                    || spec.kind != SpreadEnvironmentSource.KindForModel(entry.Id) || spec.materialFamily != "reference-glade-palette" || spec.rigged || spec.rigFamily != "none"
                    || spec.clips == null || spec.clips.Length != 0 || spec.sockets == null || spec.sockets.Length != 0
                    || spec.boundsCenter != mesh.bounds.center || spec.boundsSize != mesh.bounds.size
                    || spec.triangles != mesh.vertexCount / 24 * 12)
                    throw new InvalidOperationException("Scenery prefab or geometry contract differs: " + entry.Id);
                var vertices = mesh.vertices; var uv = mesh.uv; var normals = mesh.normals;
                if (uv.Length != vertices.Length || normals.Length != vertices.Length)
                    throw new InvalidOperationException("Incomplete scenery mesh buffers.");
                foreach (var point in vertices)
                    if (!Finite(point.x) || !Finite(point.y) || !Finite(point.z)
                        || Mathf.Abs(point.x) > .501f || Mathf.Abs(point.z) > .501f || point.y < -.0351f || point.y > 1.8501f)
                        throw new InvalidOperationException("Scenery mesh leaves native cell envelope.");
                foreach (var point in uv)
                    if (!Finite(point.x) || point.y != .5f || point.x <= 0 || point.x >= 1
                        || Mathf.Abs(point.x * 24f - .5f - Mathf.Round(point.x * 24f - .5f)) > .0001f)
                        throw new InvalidOperationException("Scenery palette cell mismatch.");
                foreach (var normal in normals)
                    if (!Finite(normal.x) || !Finite(normal.y) || !Finite(normal.z) || normal.sqrMagnitude < .99f)
                        throw new InvalidOperationException("Invalid scenery normal.");
                next.Add(entry.Id, entry);
            }
            if (remaining.Count != 0) throw new InvalidOperationException("Missing scenery family.");
            meshes = nextMeshes; index = next;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private void OnValidate() { index = null; meshes = null; }
    }
}
