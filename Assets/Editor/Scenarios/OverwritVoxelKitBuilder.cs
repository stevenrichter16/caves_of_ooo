using System;
using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Reproducible offline Overwrit art. Repeated cubes become one combined
    /// mesh asset per variant; source and generated scene objects remain separate.</summary>
    public static class OverwritVoxelKitBuilder
    {
        private const string Folder = "Assets/Resources/OverwritVoxel3D";
        private static readonly string[] Families = { "ground", "growth", "waymarker", "bench" };
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
                var entries = new List<OverwritVoxelLibrary.Entry>(16);
                foreach (string family in Families)
                {
                    for (int variant = 0; variant < OverwritVoxelLibrary.VariantCount; variant++)
                    {
                        string id = OverwritVoxelLibrary.ModelId(family, variant);
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
                        entries.Add(new OverwritVoxelLibrary.Entry
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
                var library = AssetDatabase.LoadAssetAtPath<OverwritVoxelLibrary>(libraryPath);
                if (library == null)
                {
                    library = ScriptableObject.CreateInstance<OverwritVoxelLibrary>();
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
                case "growth": Growth(variant); break;
                case "waymarker": Waymarker(variant); break;
                case "bench": Bench(variant); break;
                default: throw new ArgumentException("Unknown Overwrit art family.", nameof(family));
            }
        }

        private static void Ground(int variant)
        {
            // The entire exposed plane is identical. Only buried thickness varies.
            float thickness = .06f + variant * .008f;
            Box(new Vector3(0, -thickness * .5f, 0), new Vector3(1, thickness, 1), 35);
        }

        private static void Growth(int variant)
        {
            // New growth stays uniformly short; position/width changes never add tall shoots.
            float shift = (variant - 1.5f) * .025f;
            Box(new Vector3(-.12f + shift, .16f, 0), new Vector3(.12f, .32f, .18f), 48);
            Box(new Vector3(.13f - shift, .16f, .06f), new Vector3(.18f, .32f, .18f), 6);
            Box(new Vector3(shift, .16f, -.13f), new Vector3(.22f, .32f, .14f), 48);
        }

        private static void Waymarker(int variant)
        {
            float width = .32f + variant * .025f;
            float height = .90f + variant * .09f;
            Box(new Vector3(0, .06f, 0), new Vector3(width, .12f, .30f), 12);
            Box(new Vector3(0, .10f + height * .5f, 0), new Vector3(.20f + variant * .018f, height, .20f), 13);
        }

        private static void Bench(int variant)
        {
            // Local +Z is the open side. Native region orientation faces the seat inward.
            float width = .82f + variant * .025f;
            float depth = .45f + variant * .02f;
            Box(new Vector3(0, .28f, 0), new Vector3(width, .12f, depth), 11);
            Box(new Vector3(0, .47f, -.24f), new Vector3(width, .42f, .12f), 11);
            Box(new Vector3(-.28f, .11f, 0), new Vector3(.13f, .22f, .38f), 15);
            Box(new Vector3(.28f, .11f, 0), new Vector3(.13f, .22f, .38f), 15);
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
