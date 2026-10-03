using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Six original combined-cuboid forms, built offline by
    /// SoddenDistrictKitBuilder. Borrowed art never owns native interactions.</summary>
    public sealed class SoddenDistrictArtLibrary : ScriptableObject
    {
        public const string ResourcePath = "SoddenDistrict3D/Library", Folder = "Assets/Resources/SoddenDistrict3D";
        public const string BrokenBench = "sodden-district-bench-broken", WorkingBench = "sodden-district-bench-working",
            Salvage = "sodden-district-works-salvage", Locker = "sodden-district-works-locker",
            Notice = "sodden-district-route-notice", Dressing = "sodden-district-field-dressing";
        static readonly string[] ids = { BrokenBench, WorkingBench, Salvage, Locker, Notice, Dressing };
        public static IReadOnlyList<string> ModelIds => ids;
        [Serializable] public sealed class Entry
        { public string Id; public GameObject Prefab; public Mesh Mesh; public SpawnRing3DCatalog.Model Spec; }
        public Entry[] Entries;
        Dictionary<string, Entry> index;
        public static SoddenDistrictArtLibrary Load() => Resources.Load<SoddenDistrictArtLibrary>(ResourcePath);
        public static string Blueprint(string id)
        {
            switch (id)
            {
                case BrokenBench: case WorkingBench: return "SoddenDressingBench";
                case Salvage: return "SoddenWorksSalvage";
                case Locker: return "SoddenWorksLocker";
                case Notice: return "SoddenRouteNotice";
                case Dressing: return "SoddenFieldDressing";
                default: return null;
            }
        }
        public static bool IsModelId(string id) => Blueprint(id) != null;
        public Entry Find(string id)
        {
            if (!IsModelId(id)) return null;
            if (index == null) Validate();
            return index.TryGetValue(id, out var entry) ? entry : null;
        }
        void OnValidate() => index = null;
        /// <summary>Publish the lookup only when every submitted mesh, material,
        /// prefab and measured metadata record agrees with the authored source.</summary>
        public void Validate()
        {
            index = null;
            var material = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath)?.WorldMaterial;
            if (material == null || Entries == null || Entries.Length != ids.Length)
                throw new InvalidOperationException("Six Sodden district forms and the native ring material are required.");
            var next = new Dictionary<string, Entry>(StringComparer.Ordinal);
            foreach (var entry in Entries)
            {
                if (entry == null || !IsModelId(entry.Id) || next.ContainsKey(entry.Id) || entry.Prefab == null
                    || entry.Mesh == null || !entry.Mesh.isReadable || entry.Mesh.vertexCount < 72 || entry.Mesh.vertexCount > 720 || entry.Spec == null)
                    throw new InvalidOperationException("Missing, duplicate or malformed Sodden district form.");
                var prefab = entry.Prefab; var mesh = entry.Mesh;
                var filters = prefab.GetComponentsInChildren<MeshFilter>(true); var renderers = prefab.GetComponentsInChildren<Renderer>(true);
                if (filters.Length != 1 || filters[0].gameObject != prefab || filters[0].sharedMesh != mesh
                    || renderers.Length != 1 || !(renderers[0] is MeshRenderer) || renderers[0].gameObject != prefab
                    || !prefab.activeSelf || !renderers[0].enabled || renderers[0].forceRenderingOff
                    || renderers[0].sharedMaterials.Length != 1 || renderers[0].sharedMaterial != material
                    || prefab.transform.localPosition != Vector3.zero || prefab.transform.localRotation != Quaternion.identity
                    || prefab.transform.localScale != Vector3.one || prefab.GetComponentsInChildren<Collider>(true).Length != 0
                    || prefab.GetComponentsInChildren<Rigidbody>(true).Length != 0 || prefab.GetComponentsInChildren<MonoBehaviour>(true).Length != 0
                    || prefab.GetComponentsInChildren<Light>(true).Length != 0 || prefab.GetComponentsInChildren<Animator>(true).Length != 0
                    || mesh.subMeshCount != 1 || mesh.GetTopology(0) != MeshTopology.Triangles || mesh.GetIndexCount(0) == 0
                    || mesh.uv.Length != mesh.vertexCount || mesh.normals.Length != mesh.vertexCount)
                    throw new InvalidOperationException("Sodden district prefab must submit exactly its inert original mesh: " + entry.Id);
                var vertices = mesh.vertices; var bounds = new Bounds(vertices[0], Vector3.zero);
                foreach (var point in vertices)
                {
                    if (!Finite(point.x) || !Finite(point.y) || !Finite(point.z) || point.x < -.5001f || point.x > .5001f
                        || point.z < -.5001f || point.z > .5001f || point.y < -.0001f || point.y > 2.5f)
                        throw new InvalidOperationException("Sodden district geometry exceeds its actual native cell.");
                    bounds.Encapsulate(point);
                }
                foreach (var uv in mesh.uv)
                    if (!Finite(uv.x) || !Finite(uv.y) || uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
                        throw new InvalidOperationException("Invalid Sodden district palette coordinate.");
                if ((bounds.center - mesh.bounds.center).sqrMagnitude > .000001f || (bounds.size - mesh.bounds.size).sqrMagnitude > .000001f)
                    throw new InvalidOperationException("Sodden district mesh bounds do not match its vertices.");
                var spec = entry.Spec;
                if (spec.id != entry.Id || spec.sourceBlueprint != Blueprint(entry.Id) || spec.path != Folder + "/" + entry.Id + ".prefab"
                    || spec.kind != "entity" || spec.materialFamily != "ring-palette" || spec.rigFamily != "none" || spec.rigged
                    || spec.boundsCenter != mesh.bounds.center || spec.boundsSize != mesh.bounds.size || spec.triangles != (int)mesh.GetIndexCount(0) / 3
                    || spec.clips == null || spec.clips.Length != 0 || spec.sockets == null || spec.sockets.Length != 0)
                    throw new InvalidOperationException("Sodden district metadata differs from its submitted geometry: " + entry.Id);
                next.Add(entry.Id, entry);
            }
            foreach (string id in ids) if (!next.ContainsKey(id)) throw new InvalidOperationException("Missing Sodden district form: " + id);
            index = next;
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
