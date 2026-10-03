using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Original regional components, twelve actual assemblies and defensive equipment.
    /// Persistent meshes use the native ring palette; fitted apron carries one bind bone.</summary>
    public sealed class EquipmentDiscoveryArtLibrary : ScriptableObject
    {
        public const string ResourcePath = "EquipmentDiscovery3D/Library", Folder = "Assets/Resources/EquipmentDiscovery3D";
        public const string Prefix = EquipmentDiscoveryRecipes.Prefix;
        static readonly Dictionary<string, string> blueprints = BuildIdentities();
        static readonly IReadOnlyList<string> ids = new List<string>(blueprints.Keys).AsReadOnly();
        public static IReadOnlyList<string> ModelIds => ids;
        [Serializable] public sealed class Entry { public string Id, Slot; public GameObject Prefab; public Mesh Mesh; public SpawnRing3DCatalog.Model Spec; }
        public Entry[] Entries;
        public Material Material;
        Dictionary<string, Entry> index;
        public static EquipmentDiscoveryArtLibrary Load() => Resources.Load<EquipmentDiscoveryArtLibrary>(ResourcePath);
        static Dictionary<string, string> BuildIdentities()
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string blueprint in new[] { "PeatMalletHeadComponent", "CinderhookAxeHeadComponent", "CounterweightLongBladeComponent" }) {
                string head = EquipmentDiscoveryRecipes.HeadForm(blueprint); result.Add(Prefix + "head-" + head, blueprint);
                foreach (string haft in new[] { "oak", "willow" }) foreach (string binding in new[] { "leather", "serrated" })
                    result.Add(Prefix + "forged-" + head + "-" + haft + "-" + binding, "ForgedWeapon");
            }
            result.Add(Prefix + "groundwire-screen", "GroundwireScreen"); result.Add(Prefix + "kilnfelt-apron", "KilnfeltApron"); result.Add(Prefix + "worn-kilnfelt-apron", "KilnfeltApron");
            return result;
        }
        public static bool IsModelId(string id) => id != null && blueprints.ContainsKey(id);
        public static string Blueprint(string id) => id != null && blueprints.TryGetValue(id, out string blueprint) ? blueprint : null;
        public static bool IsWorn(string id) => id == Prefix + "worn-kilnfelt-apron";
        public Entry Find(string id) { if (!IsModelId(id)) return null; if (index == null) Validate(); return index.TryGetValue(id, out var entry) ? entry : null; }
        void OnValidate() => index = null;
        public void Validate()
        {
            index = null;
            var ring = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            if (Material == null || ring == null || Material != ring.WorldMaterial || Entries == null || Entries.Length != ids.Count)
                throw new InvalidOperationException("Complete original regional equipment kit required.");
            var next = new Dictionary<string, Entry>(StringComparer.Ordinal); var meshes = new HashSet<Mesh>(); var prefabs = new HashSet<GameObject>();
            foreach (var entry in Entries) {
                if (entry == null || !IsModelId(entry.Id) || next.ContainsKey(entry.Id) || entry.Prefab == null || !prefabs.Add(entry.Prefab)
                    || entry.Mesh == null || !meshes.Add(entry.Mesh) || !entry.Mesh.isReadable || entry.Mesh.vertexCount < 72 || entry.Mesh.vertexCount > 4000 || entry.Spec == null)
                    throw new InvalidOperationException("Malformed or duplicate regional equipment form.");
                var mesh = entry.Mesh; var root = entry.Prefab; bool worn = IsWorn(entry.Id);
                var filters = root.GetComponentsInChildren<MeshFilter>(true); var renderers = root.GetComponentsInChildren<Renderer>(true);
                if (filters.Length != 1 || filters[0].gameObject != root || filters[0].sharedMesh != mesh || renderers.Length != 1 || !(renderers[0] is MeshRenderer)
                    || renderers[0].gameObject != root || !root.activeSelf || !renderers[0].enabled || renderers[0].forceRenderingOff || renderers[0].sharedMaterials.Length != 1
                    || renderers[0].sharedMaterial != Material || root.transform.localPosition != Vector3.zero || root.transform.localRotation != Quaternion.identity || root.transform.localScale != Vector3.one
                    || root.GetComponentsInChildren<Collider>(true).Length != 0 || root.GetComponentsInChildren<Rigidbody>(true).Length != 0 || root.GetComponentsInChildren<MonoBehaviour>(true).Length != 0
                    || root.GetComponentsInChildren<Light>(true).Length != 0 || root.GetComponentsInChildren<Animator>(true).Length != 0 || mesh.subMeshCount != 1 || mesh.GetTopology(0) != MeshTopology.Triangles
                    || mesh.GetIndexCount(0) == 0 || mesh.normals.Length != mesh.vertexCount || mesh.uv.Length != mesh.vertexCount || mesh.bindposeCount != (worn ? 1 : 0)
                    || entry.Slot != (worn ? "Body" : "")) throw new InvalidOperationException("Equipment prefab or attachment contract differs: " + entry.Id);
                var vertices = mesh.vertices; var bounds = new Bounds(vertices[0], Vector3.zero);
                foreach (var point in vertices) {
                    if (!Finite(point.x) || !Finite(point.y) || !Finite(point.z) || Math.Abs(point.x) > .501f || Math.Abs(point.z) > .501f
                        || point.y < (worn ? -.60f : -.001f) || point.y > .601f) throw new InvalidOperationException("Unbounded equipment mesh: " + entry.Id);
                    bounds.Encapsulate(point);
                }
                foreach (var uv in mesh.uv) if (!Finite(uv.x) || !Finite(uv.y) || uv.x <= 0 || uv.x >= 1 || uv.y <= 0 || uv.y >= 1) throw new InvalidOperationException("Invalid equipment palette coordinates.");
                if (worn) {
                    var weights = mesh.boneWeights; if (weights.Length != mesh.vertexCount || mesh.bindposes[0] != Matrix4x4.identity) throw new InvalidOperationException("Apron requires its original body bind.");
                    foreach (var weight in weights) if (weight.boneIndex0 != 0 || weight.weight0 != 1 || weight.weight1 != 0 || weight.weight2 != 0 || weight.weight3 != 0) throw new InvalidOperationException("Invalid apron skin weight.");
                }
                var spec = entry.Spec;
                if ((bounds.center - mesh.bounds.center).sqrMagnitude > .000001f || (bounds.size - mesh.bounds.size).sqrMagnitude > .000001f
                    || spec.id != entry.Id || spec.sourceBlueprint != Blueprint(entry.Id) || spec.path != Folder + "/" + entry.Id + ".prefab" || spec.kind != "entity" || spec.materialFamily != "ring-palette"
                    || spec.rigged || spec.rigFamily != "none" || spec.boundsCenter != mesh.bounds.center || spec.boundsSize != mesh.bounds.size || spec.triangles != (int)mesh.GetIndexCount(0) / 3
                    || spec.clips == null || spec.clips.Length != 0 || spec.sockets == null || spec.sockets.Length != 0) throw new InvalidOperationException("Equipment measured metadata differs: " + entry.Id);
                next.Add(entry.Id, entry);
            }
            index = next;
        }
        static bool Finite(float x) => !float.IsNaN(x) && !float.IsInfinity(x);
    }
}
