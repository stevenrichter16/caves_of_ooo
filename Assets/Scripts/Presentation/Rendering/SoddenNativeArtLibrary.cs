using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CavesOfOoo.Rendering
{
    /// <summary>Borrowed original Sodden art, keyed by already validated native
    /// model identity. Static assets own no collision, state, light or interaction.</summary>
    public sealed class SoddenNativeArtLibrary : ScriptableObject
    {
        public const string ResourcePath = "SoddenNativeArt3D/Library";
        public const string Folder = "Assets/Resources/SoddenNativeArt3D";
        public const string ReviewedSourceSha256 = "e5eb1e3fa457642eebdf8d3d6e3e269964f714101ebbab2404b5545f6159c432";
        public string SourceSha256;
        public Material Material;
        [Serializable] public sealed class Entry
        { public string Id; public GameObject Prefab; public Mesh Mesh; public SpawnRing3DCatalog.Model Spec; }
        public Entry[] Entries;
        Dictionary<string, Entry> index;
        static readonly string[] families = { "ground", "mire", "peat", "snag", "boards", "body", "reeds", "greatdew" };
        static readonly string[] regionalPrefixes = { "spread-environment-paving", "wellmeet-floor", "sumphold-wall",
            "sumphold-water", "drownedledger-boards", "wellmeet-tent", "wellmeet-corner", "drownedledger-preserved",
            "drownedledger-stake", "drownedledger-table", "ring-vine-wall", "ring-brine-pool" };
        static readonly string[] regionalBlueprints = { "StoneFloor", "StoneFloor", "StoneWall", "WaterPuddle", "Duckboard",
            "TentWall", "TentWall", "PreFellingBody", "SurveyStake", "ReadingTable", "VineWall", "BrinePool" };
        static readonly string[] singleIds = { "sodden-district-bench-broken", "sodden-district-bench-working",
            "sodden-district-works-locker", "sodden-district-works-salvage", "sodden-district-route-notice",
            "density-pool-acid", "ring-steam-vent" };
        static readonly string[] singleBlueprints = { "SoddenDressingBench", "SoddenDressingBench",
            "SoddenWorksLocker", "SoddenWorksSalvage", "SoddenRouteNotice", "AcidPool", "SteamVent" };
        public static IReadOnlyList<string> ModelIds { get; } = Array.AsReadOnly(BuildIds());
        public static readonly string[] PaletteHex = {
            "#092D29", "#103A33", "#19463B", "#275447", "#071C20", "#102C2E",
            "#273D3A", "#41534E", "#617269", "#596B5C", "#A1AF91", "#D0D5B6",
            "#184A31", "#347345", "#589459", "#A9BA7A", "#3E392B", "#786347",
            "#AA9064", "#344846", "#81948A", "#485651", "#74B6B5", "#D8E1CB"
        };
        static string[] BuildIds()
        {
            var ids = new List<string>(85);
            foreach (string family in families)
                for (int i = 0; i < (family == "greatdew" ? 2 : 4); i++) ids.Add(ModelId(family, i));
            foreach (string prefix in regionalPrefixes) for (int i = 0; i < 4; i++) ids.Add(prefix + "-" + i);
            ids.AddRange(singleIds);
            return ids.ToArray();
        }
        public static string ModelId(string family, int variant)
        {
            if (Array.IndexOf(families, family) < 0 || variant < 0 || variant >= (family == "greatdew" ? 2 : 4))
                throw new ArgumentException("Unknown Sodden native art family or variant.");
            return (family == "reeds" ? "spread-" : family == "greatdew" ? "sodden-native-" : "sodden-") + family + "-" + variant;
        }
        public static SoddenNativeArtLibrary Load() => Resources.Load<SoddenNativeArtLibrary>(ResourcePath);
        void OnValidate() => index = null;
        public Entry Find(string existingModelId)
        {
            if (existingModelId == null) return null;
            if (index == null) Validate();
            return index.TryGetValue(existingModelId, out var value) ? value : null;
        }
        public static string Blueprint(string modelId)
        {
            foreach (string family in families)
                for (int i = 0; i < (family == "greatdew" ? 2 : 4); i++)
                    if (modelId == ModelId(family, i))
                    {
                        switch (family)
                        {
                            case "ground": return "Grass";
                            case "mire": return "MirePool";
                            case "peat": return "PeatBank";
                            case "snag": return "DeadTree";
                            case "boards": return "Duckboard";
                            case "body": return "BogTakenBody";
                            case "reeds": return "Reeds";
                            case "greatdew": return "Greatdew";
                        }
                    }
            for (int i = 0; i < regionalPrefixes.Length; i++)
                for (int variant = 0; variant < 4; variant++)
                    if (modelId == regionalPrefixes[i] + "-" + variant) return regionalBlueprints[i];
            for (int i = 0; i < singleIds.Length; i++) if (modelId == singleIds[i]) return singleBlueprints[i];
            return null;
        }
        public static string KindForModel(string modelId) => modelId != null
            && (modelId.StartsWith("sodden-ground-", StringComparison.Ordinal)
                || modelId.StartsWith("spread-environment-paving-", StringComparison.Ordinal)
                || modelId.StartsWith("wellmeet-floor-", StringComparison.Ordinal)) ? "ground" : "entity";
        public void Validate()
        {
            index = null;
            var ring = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            var palette = Material == null ? null : Material.GetTexture("_BaseMap") as Texture2D;
            if (SourceSha256 != ReviewedSourceSha256 || Entries == null || Entries.Length != ModelIds.Count
                || ring?.WorldMaterial == null || Material == null || Material == ring.WorldMaterial
                || Material.shader != ring.WorldMaterial.shader || !Material.HasProperty("_FogLight") || !Material.HasProperty("_Transient")
                || Material.GetColor("_BaseColor") != Color.white || palette == null || !palette.isReadable
                || palette.width != PaletteHex.Length || palette.height != 1 || palette.filterMode != FilterMode.Point
                || palette.wrapMode != TextureWrapMode.Clamp)
                throw new InvalidOperationException("Complete reviewed Sodden native kit and private fog-aware palette required.");
            for (int i = 0; i < PaletteHex.Length; i++)
                if (!ColorUtility.TryParseHtmlString(PaletteHex[i], out var expected)
                    || !((Color32)palette.GetPixel(i, 0)).Equals((Color32)expected))
                    throw new InvalidOperationException("Sodden palette differs from authored source.");
            var remaining = new HashSet<string>(ModelIds, StringComparer.Ordinal);
            var next = new Dictionary<string, Entry>(StringComparer.Ordinal);
            var meshes = new HashSet<Mesh>(); var prefabs = new HashSet<GameObject>();
            foreach (var entry in Entries)
            {
                if (entry == null || entry.Id == null || !remaining.Remove(entry.Id) || entry.Mesh == null
                    || !meshes.Add(entry.Mesh) || entry.Prefab == null || !prefabs.Add(entry.Prefab) || entry.Spec == null)
                    throw new InvalidOperationException("Missing, duplicate or foreign Sodden native model.");
                var mesh = entry.Mesh; var root = entry.Prefab; var spec = entry.Spec;
                var filter = root.GetComponent<MeshFilter>(); var renderer = root.GetComponent<MeshRenderer>();
                if (!mesh.isReadable || mesh.vertexCount < 48 || mesh.vertexCount > 60 * 24 || mesh.vertexCount % 24 != 0
                    || mesh.subMeshCount != 1 || mesh.bindposeCount != 0 || mesh.GetTopology(0) != MeshTopology.Triangles
                    || mesh.GetIndexCount(0) != (uint)(mesh.vertexCount / 24 * 36)
                    || filter == null || filter.sharedMesh != mesh || renderer == null || renderer.sharedMaterials.Length != 1
                    || renderer.sharedMaterial != Material || !renderer.enabled || renderer.forceRenderingOff || !root.activeSelf
                    || root.transform.childCount != 0 || root.GetComponents<Renderer>().Length != 1
                    || root.GetComponents<Collider>().Length != 0 || root.GetComponents<Rigidbody>().Length != 0
                    || root.GetComponents<MonoBehaviour>().Length != 0 || root.GetComponent<Animator>() != null || root.GetComponent<Light>() != null
                    || root.transform.localPosition != Vector3.zero || root.transform.localRotation != Quaternion.identity
                    || root.transform.localScale != Vector3.one || spec.id != entry.Id || spec.sourceBlueprint != Blueprint(entry.Id)
                    || spec.path != Folder + "/" + entry.Id + ".prefab" || spec.materialFamily != "sodden-native-palette"
                    || spec.kind != KindForModel(entry.Id)
                    || spec.rigFamily != "none" || spec.rigged || spec.clips == null || spec.clips.Length != 0
                    || spec.sockets == null || spec.sockets.Length != 0 || spec.boundsCenter != mesh.bounds.center
                    || spec.boundsSize != mesh.bounds.size || spec.triangles != mesh.vertexCount / 24 * 12)
                    throw new InvalidOperationException("Sodden native mesh/prefab/metadata contract differs: " + entry.Id);
                var points = mesh.vertices; var uv = mesh.uv; var normals = mesh.normals;
                if (uv.Length != points.Length || normals.Length != points.Length)
                    throw new InvalidOperationException("Sodden native mesh buffers are incomplete.");
                var bounds = new Bounds(points[0], Vector3.zero);
                foreach (var point in points)
                {
                    if (!Finite(point.x) || !Finite(point.y) || !Finite(point.z) || Mathf.Abs(point.x) > .5001f
                        || Mathf.Abs(point.z) > .5001f || point.y < -.0501f || point.y > 3.2501f)
                        throw new InvalidOperationException("Sodden native mesh exceeds its cell or height envelope.");
                    bounds.Encapsulate(point);
                }
                if ((bounds.center - mesh.bounds.center).sqrMagnitude > .000001f || (bounds.size - mesh.bounds.size).sqrMagnitude > .000001f)
                    throw new InvalidOperationException("Sodden native mesh bounds differ from its actual vertices.");
                foreach (var at in uv)
                    if (!Finite(at.x) || at.y != .5f || at.x <= 0 || at.x >= 1
                        || Mathf.Abs(at.x * PaletteHex.Length - .5f - Mathf.Round(at.x * PaletteHex.Length - .5f)) > .0001f)
                        throw new InvalidOperationException("Sodden native UV is not an exact palette swatch.");
                foreach (var normal in normals)
                    if (!Finite(normal.x) || !Finite(normal.y) || !Finite(normal.z) || normal.sqrMagnitude < .99f)
                        throw new InvalidOperationException("Sodden native normal is malformed.");
                next.Add(entry.Id, entry);
            }
            if (remaining.Count != 0) throw new InvalidOperationException("Sodden native kit omits an authored model.");
            index = next;
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
