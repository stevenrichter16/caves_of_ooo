using System;
using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Reproducible offline LastCounter art. Repeated cubes become one combined
    /// mesh asset per variant; source and generated scene objects remain separate.</summary>
    public static class LastCounterVoxelKitBuilder
    {
        private const string Folder = "Assets/Resources/LastCounterVoxel3D";
        private static readonly string[] Families = { "ground", "wall", "sign", "envoy" };
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
                var entries = new List<LastCounterVoxelKitLibrary.Entry>(16);
                foreach (string family in Families)
                {
                    for (int variant = 0; variant < LastCounterVoxelKitLibrary.VariantCount; variant++)
                    {
                        string id = LastCounterVoxelKitLibrary.ModelId(family, variant);
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
                        entries.Add(new LastCounterVoxelKitLibrary.Entry
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
                var library = AssetDatabase.LoadAssetAtPath<LastCounterVoxelKitLibrary>(libraryPath);
                if (library == null)
                {
                    library = ScriptableObject.CreateInstance<LastCounterVoxelKitLibrary>();
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
                case "wall": Wall(variant); break;
                case "sign": Sign(variant); break;
                case "envoy": Envoy(variant); break;
                default: throw new ArgumentException("Unknown LastCounter art family.", nameof(family));
            }
        }

        private static void Ground(int variant)
        {
            // The mapped outpost has no road. Its actual outdoor Floor stays
            // quiet and continuous; this mesh does not create a paved route.
            float thickness = .065f + variant * .008f;
            Box(new Vector3(0, -thickness * .5f, 0), new Vector3(1, thickness, 1), 64);
        }

        private static void Wall(int variant)
        {
            // Broad weathered sandstone cutaway keeps the working supply
            // court visible. Native walls retain their own solid/destruction.
            float lower = .47f + variant * .035f;
            Box(new Vector3(0, lower * .5f, 0), new Vector3(1, lower, 1), 56);
            float upper = 1.05f - lower;
            Box(new Vector3(0, lower + upper * .5f, 0), new Vector3(1, upper, 1), 79);
        }

        private static void Sign(int variant)
        {
            // One planed board, current disclaimer above two smaller older
            // inscriptions. Coarse bands signal three readings; native Examine
            // owns the exact wording. No tiny fake lettering or new menu.
            Box(new Vector3(-.32f, .45f, 0), new Vector3(.12f, .90f, .16f), 12);
            Box(new Vector3(.32f, .45f, 0), new Vector3(.12f, .90f, .16f), 12);
            float height = .90f + variant * .03f;
            Box(new Vector3(0, 1.20f, 0), new Vector3(.96f, height, .20f), 35);
            Box(new Vector3(0, 1.40f, .12f), new Vector3(.76f, .09f, .08f), 12);
            Box(new Vector3(0, 1.15f, .12f), new Vector3(.58f, .07f, .08f), 12);
            Box(new Vector3(0, .90f, .12f), new Vector3(.42f, .05f, .08f), 12);
        }

        private static void Envoy(int variant)
        {
            // The native Concord trader remains a movable person. A neat
            // ochre coat and carried ledger distinguish service from a sign.
            float width = .42f + variant * .012f;
            Box(new Vector3(-.12f, .245f, 0), new Vector3(.17f, .49f, .23f), 12);
            Box(new Vector3(.12f, .245f, 0), new Vector3(.17f, .49f, .23f), 12);
            Box(new Vector3(0, .78f, 0), new Vector3(width, .64f, .33f), 49);
            Box(new Vector3(0, 1.30f, .035f), new Vector3(.30f, .32f, .28f), 49);
            Box(new Vector3(0, 1.47f, .025f), new Vector3(.35f, .08f, .31f), 12);
            Box(new Vector3(-.28f, .87f, .12f), new Vector3(.13f, .28f, .24f), 49);
            Box(new Vector3(.28f, .87f, .12f), new Vector3(.13f, .28f, .24f), 49);
            Box(new Vector3(0, .91f, .265f), new Vector3(.40f, .26f, .075f), 12);
            Box(new Vector3(-.075f, 1.33f, .191f), new Vector3(.04f, .04f, .026f), 12);
            Box(new Vector3(.075f, 1.33f, .191f), new Vector3(.04f, .04f, .026f), 12);
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
