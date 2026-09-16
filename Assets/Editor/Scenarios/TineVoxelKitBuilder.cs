using System;
using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Reproducible offline Tine art. Repeated cubes become one combined
    /// mesh asset per variant; source and generated scene objects remain separate.</summary>
    public static class TineVoxelKitBuilder
    {
        private const string Folder = "Assets/Resources/TineVoxel3D";
        private static readonly string[] Families = { "ground", "wall", "water", "pier", "path" };
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
                var entries = new List<TineVoxelKitLibrary.Entry>(20);
                foreach (string family in Families)
                {
                    for (int variant = 0; variant < TineVoxelKitLibrary.VariantCount; variant++)
                    {
                        string id = TineVoxelKitLibrary.ModelId(family, variant);
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
                        entries.Add(new TineVoxelKitLibrary.Entry
                        {
                            Id = id, Prefab = prefab, Mesh = mesh,
                            Spec = new SpawnRing3DCatalog.Model
                            {
                                id = id, path = prefabPath, kind = family == "ground" || family == "path" ? "ground" : "entity",
                                materialFamily = "ring-palette", rigFamily = "none",
                                boundsCenter = mesh.bounds.center, boundsSize = mesh.bounds.size,
                                triangles = Triangles.Count / 3, clips = Array.Empty<string>(), sockets = Array.Empty<string>()
                            }
                        });
                    }
                }
                string libraryPath = Folder + "/Library.asset";
                var library = AssetDatabase.LoadAssetAtPath<TineVoxelKitLibrary>(libraryPath);
                if (library == null)
                {
                    library = ScriptableObject.CreateInstance<TineVoxelKitLibrary>();
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
                case "water": Water(variant); break;
                case "pier": Pier(variant); break;
                case "path": Path(variant); break;
                default: throw new ArgumentException("Unknown Tine art family.", nameof(family));
            }
        }

        private static void Ground(int variant)
        {
            // One calm shore color, with differences buried below the surface.
            float thickness = .065f + variant * .008f;
            Box(new Vector3(0, -thickness * .5f, 0), new Vector3(1, thickness, 1), 106);
        }
        private static void Wall(int variant)
        {
            // Two broad courses, no repeated crenels or decorative cut grooves.
            float lower = .47f + variant * .035f;
            Box(new Vector3(0, lower * .5f, 0), new Vector3(1, lower, 1), 56);
            float upper = 1.05f - lower;
            Box(new Vector3(0, lower + upper * .5f, 0), new Vector3(1, upper, 1), 107);
        }
        private static void Water(int variant)
        {
            // Current native water owners share one uninterrupted dark teal top.
            // Variation is buried; neither a mesh nor the plan retains water
            // after the actual LiquidPool owner has been removed.
            float thickness = .07f + variant * .008f;
            Box(new Vector3(0, -thickness * .5f, 0), new Vector3(1, thickness, 1), 24);
        }
        private static void Pier(int variant)
        {
            // Three wide planks and two lower sleepers; native Duckboard is
            // walkable/destructible wood over an actual dry finger of land.
            Box(new Vector3(0, .045f, -.32f), new Vector3(.96f, .09f, .12f), 106);
            Box(new Vector3(0, .045f, .32f), new Vector3(.96f, .09f, .12f), 106);
            float length = .98f - variant * .012f;
            Box(new Vector3(-.31f, .13f, 0), new Vector3(.28f, .08f, length), 64);
            Box(new Vector3(0, .13f, 0), new Vector3(.28f, .08f, length), 64);
            Box(new Vector3(.31f, .13f, 0), new Vector3(.28f, .08f, length), 64);
        }

        private static void Path(int variant)
        {
            // Worn earth reads slightly warmer than the gray-green shore,
            // without the bright gold strip that overpowered the first preview.
            float thickness = .065f + variant * .008f;
            Box(new Vector3(0, -thickness * .5f, 0), new Vector3(1, thickness, 1), 64);
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
