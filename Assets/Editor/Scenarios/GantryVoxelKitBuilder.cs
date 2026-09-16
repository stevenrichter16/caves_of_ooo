using System;
using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Reproducible offline Gantry art. Repeated cubes become one combined
    /// mesh asset per variant; source and generated scene objects remain separate.</summary>
    public static class GantryVoxelKitBuilder
    {
        private const string Folder = "Assets/Resources/GantryVoxel3D";
        private static readonly string[] Families = { "ground", "path", "wall", "desk", "counter", "registrar", "wayboard" };
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
                var entries = new List<GantryVoxelKitLibrary.Entry>(28);
                foreach (string family in Families)
                {
                    for (int variant = 0; variant < GantryVoxelKitLibrary.VariantCount; variant++)
                    {
                        string id = GantryVoxelKitLibrary.ModelId(family, variant);
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
                        entries.Add(new GantryVoxelKitLibrary.Entry
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
                var library = AssetDatabase.LoadAssetAtPath<GantryVoxelKitLibrary>(libraryPath);
                if (library == null)
                {
                    library = ScriptableObject.CreateInstance<GantryVoxelKitLibrary>();
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

        private static void BuildFamily(string family,int variant)
        {
            switch(family)
            {
                case "ground":case "path":
                    float thickness=.07f+variant*.008f;
                    Box(new Vector3(0,-thickness*.5f,0),new Vector3(1,thickness,1),family=="ground"?111:64);break;
                case "wall":
                    // One broad board plane, two uprights. The native single-cell
                    // wall owns solidity and burning; no tiny alternating bricks.
                    Box(new Vector3(0,.48f,0),new Vector3(1,.96f,.58f),102);
                    float post=.13f+variant*.015f;
                    Box(new Vector3(-.40f,.53f,0),new Vector3(post,1.06f,.68f),12);
                    Box(new Vector3(.40f,.53f,0),new Vector3(post,1.06f,.68f),12);break;
                case "desk":
                    Box(new Vector3(0,.66f,0),new Vector3(.96f,.15f,.76f),102);
                    Box(new Vector3(-.32f,.30f,0),new Vector3(.16f,.60f,.60f),102);
                    Box(new Vector3(.32f,.30f,0),new Vector3(.16f,.60f,.60f),102);
                    Box(new Vector3(-.08f+variant*.04f,.755f,.06f),new Vector3(.40f,.04f,.32f),108);break;
                case "counter":
                    Box(new Vector3(0,.38f,0),new Vector3(.90f,.76f,.78f),102);
                    Box(new Vector3(0,.82f,0),new Vector3(1,.14f,.92f),32);
                    Box(new Vector3(-.27f+variant*.04f,.96f,.12f),new Vector3(.29f,.14f,.30f),32);break;
                case "registrar":
                    Box(new Vector3(-.12f,.245f,0),new Vector3(.17f,.49f,.23f),12);
                    Box(new Vector3(.12f,.245f,0),new Vector3(.17f,.49f,.23f),12);
                    Box(new Vector3(0,.77f,0),new Vector3(.44f+variant*.012f,.65f,.33f),35);
                    Box(new Vector3(0,1.29f,.035f),new Vector3(.30f,.32f,.28f),35);
                    Box(new Vector3(0,1.46f,.025f),new Vector3(.35f,.07f,.31f),12);
                    Box(new Vector3(-.29f,.89f,.12f),new Vector3(.13f,.26f,.22f),35);
                    Box(new Vector3(.29f,.89f,.12f),new Vector3(.13f,.26f,.22f),35);
                    Box(new Vector3(0,.92f,.26f),new Vector3(.40f,.30f,.09f),12);
                    Box(new Vector3(-.08f,1.32f,.19f),new Vector3(.04f,.04f,.03f),12);
                    Box(new Vector3(.08f,1.32f,.19f),new Vector3(.04f,.04f,.03f),12);break;
                case "wayboard":
                    Box(new Vector3(0,.80f,0),new Vector3(.16f,1.60f,.18f),12);
                    Box(new Vector3(-.12f,1.60f+variant*.025f,0),new Vector3(.76f,.21f,.19f),32);
                    Box(new Vector3(.12f,1.25f+variant*.02f,0),new Vector3(.76f,.21f,.19f),32);
                    Box(new Vector3(-.10f,.93f,0),new Vector3(.70f,.18f,.19f),32);break;
                default:throw new ArgumentException("Unknown Gantry family",nameof(family));
            }
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
