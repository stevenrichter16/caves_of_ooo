using System;
using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Reproducible offline Cathedral art. Repeated cubes become one combined
    /// mesh asset per variant; source and generated scene objects remain separate.</summary>
    public static class CathedralVoxelKitBuilder
    {
        private const string Folder = "Assets/Resources/CathedralVoxel3D";
        private static readonly string[] Families = { "ground", "vault", "node", "elder", "tendril" };
        private static readonly List<Vector3> Vertices = new List<Vector3>(288);
        private static readonly List<Vector2> UVs = new List<Vector2>(288);
        private static readonly List<Color> Colors = new List<Color>(288);
        private static readonly List<int> Triangles = new List<int>(432);
        private static Vector3[] cubeVertices;
        private static int[] cubeTriangles;

        public static void Run()
        {
            var ring = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            if (ring == null || ring.WorldMaterial == null)
                throw new InvalidOperationException("Native ring material library is unavailable.");
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var cube = primitive.GetComponent<MeshFilter>().sharedMesh;
            cubeVertices = cube.vertices;
            cubeTriangles = cube.triangles;
            try
            {
                var entries = new List<CathedralVoxelLibrary.Entry>(20);
                foreach (string family in Families)
                {
                    for (int variant = 0; variant < CathedralVoxelLibrary.VariantCount; variant++)
                    {
                        string id = CathedralVoxelLibrary.ModelId(family, variant);
                        Vertices.Clear(); UVs.Clear(); Colors.Clear(); Triangles.Clear();
                        BuildFamily(family, variant);
                        string meshPath = Folder + "/" + id + ".asset";
                        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                        if (mesh == null)
                        {
                            mesh = new Mesh { name = id };
                            AssetDatabase.CreateAsset(mesh, meshPath);
                        }
                        else mesh.Clear();
                        mesh.SetVertices(Vertices); mesh.SetUVs(0, UVs); mesh.SetColors(Colors);
                        mesh.SetTriangles(Triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                        EditorUtility.SetDirty(mesh);
                        var root = new GameObject(id);
                        root.AddComponent<MeshFilter>().sharedMesh = mesh;
                        root.AddComponent<MeshRenderer>().sharedMaterial = ring.WorldMaterial;
                        string prefabPath = Folder + "/" + id + ".prefab";
                        GameObject prefab;
                        try { prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath); }
                        finally { UnityEngine.Object.DestroyImmediate(root); }
                        entries.Add(new CathedralVoxelLibrary.Entry
                        {
                            Id = id, Prefab = prefab, Mesh = mesh,
                            Spec = new SpawnRing3DCatalog.Model
                            {
                                id = id, path = prefabPath, kind = family == "ground" ? "ground" : "entity",
                                materialFamily = "ring-palette", rigFamily = "none",
                                boundsCenter = mesh.bounds.center, boundsSize = mesh.bounds.size,
                                triangles = Triangles.Count / 3, clips = Array.Empty<string>(), sockets = Array.Empty<string>()
                            }
                        });
                    }
                }
                string libraryPath = Folder + "/Library.asset";
                var library = AssetDatabase.LoadAssetAtPath<CathedralVoxelLibrary>(libraryPath);
                if (library == null)
                {
                    library = ScriptableObject.CreateInstance<CathedralVoxelLibrary>();
                    AssetDatabase.CreateAsset(library, libraryPath);
                }
                library.Entries = entries.ToArray();
                library.Validate(); EditorUtility.SetDirty(library); AssetDatabase.SaveAssets();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(primitive);
                cubeVertices = null; cubeTriangles = null;
            }
        }

        private static void BuildFamily(string family, int variant)
        {
            switch (family)
            {
                case "ground": Ground(variant); break;
                case "vault": Vault(variant); break;
                case "node": Node(variant); break;
                case "elder": Elder(variant); break;
                case "tendril": Tendril(variant); break;
                default: throw new ArgumentException("Unknown Cathedral art family.", nameof(family));
            }
        }

        private static void Ground(int variant)
        {
            // Quiet continuous floor. Only buried thickness changes across variants.
            float thickness = .065f + variant * .008f;
            Box(new Vector3(0, -thickness * .5f, 0), new Vector3(1, thickness, 1), 12);
        }

        private static void Vault(int variant)
        {
            // Grown native cells assemble the nave: no miniature per-cell arch,
            // gaps, overhangs or random top heights can fragment the shared mass.
            float lower = .47f + variant * .055f;
            Box(new Vector3(0, lower * .5f, 0), new Vector3(1, lower, 1), 19);
            Box(new Vector3(0, lower + .06f, 0), new Vector3(1, .12f, 1), 30);
            float upper = 1.1f - lower - .12f;
            Box(new Vector3(0, lower + .12f + upper * .5f, 0), new Vector3(1, upper, 1), 19);
        }

        private static void Node(int variant)
        {
            // A taller broad knot is the nave's focal point above its cutaway ribs.
            // Native ChoirNode owns the light; no travel or particle systems are added.
            float shift = (variant - 1.5f) * .022f;
            Box(new Vector3(0, .15f, 0), new Vector3(.78f, .30f, .78f), 30);
            Box(new Vector3(0, .675f, 0), new Vector3(.34f, .90f, .34f), 30);
            Box(new Vector3(0, 1.35f, 0), new Vector3(.90f, .75f, .82f), 19);
            Box(new Vector3(shift, 1.98f, 0), new Vector3(.68f, .60f, .62f), 19);
            Box(new Vector3(shift, 1.54f, .435f), new Vector3(.18f, .24f, .10f), 30);
            Box(new Vector3(-.26f, .39f, 0), new Vector3(.22f, .39f, .26f), 30);
            Box(new Vector3(.26f, .39f, 0), new Vector3(.22f, .39f, .26f), 30);
        }

        private static void Elder(int variant)
        {
            // A living person's composed face remains above the encasement.
            // Local +Z faces the aisle; the native recipe owns orientation.
            float width = .80f + variant * .025f;
            float shift = (variant - 1.5f) * .009f;
            Box(new Vector3(0, .21f, 0), new Vector3(width, .42f, .62f), 30);
            Box(new Vector3(0, .70f, 0), new Vector3(.68f, .78f, .50f), 30);
            Box(new Vector3(0, 1.0f, 0), new Vector3(.74f, .34f, .46f), 30);
            Box(new Vector3(shift, 1.23f, .16f), new Vector3(.32f, .34f, .30f), 19);
            Box(new Vector3(0, 1.37f, -.06f), new Vector3(.44f, .17f, .22f), 30);
            Box(new Vector3(shift - .085f, 1.27f, .324f), new Vector3(.055f, .045f, .04f), 30);
            Box(new Vector3(shift + .085f, 1.27f, .324f), new Vector3(.055f, .045f, .04f), 30);
        }

        private static void Tendril(int variant)
        {
            // Branching living trader, not a wall fixture or manufactured altar.
            float reach = variant * .025f;
            Box(new Vector3(0, .09f, 0), new Vector3(.75f, .18f, .70f), 48);
            Box(new Vector3(0, .54f, 0), new Vector3(.22f, .90f, .22f), 48);
            Box(new Vector3(-.14f, .39f, 0), new Vector3(.35f, .16f, .20f), 48);
            Box(new Vector3(-.29f, .70f, 0), new Vector3(.13f, .82f, .16f), 48);
            Box(new Vector3(.14f, .47f, 0), new Vector3(.35f, .16f, .20f), 48);
            Box(new Vector3(.29f, .78f + reach * .5f, 0), new Vector3(.13f, .86f + reach, .16f), 48);
            Box(new Vector3(0, 1.15f, 0), new Vector3(.32f, .40f, .30f), 19);
            Box(new Vector3(-.31f, 1.12f, 0), new Vector3(.22f, .22f, .22f), 19);
            Box(new Vector3(.31f, 1.28f + reach, 0), new Vector3(.22f, .22f, .22f), 19);
        }

        private static void Box(Vector3 center, Vector3 size, int palette)
        {
            int offset = Vertices.Count;
            var uv = new Vector2((palette % 16 + .5f) / 16f, (palette / 16 + .5f) / 8f);
            foreach (var vertex in cubeVertices)
            {
                Vertices.Add(center + Vector3.Scale(vertex, size)); UVs.Add(uv); Colors.Add(Color.white);
            }
            foreach (int triangle in cubeTriangles) Triangles.Add(offset + triangle);
        }
    }
}
