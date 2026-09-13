using System;
using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Reproducible offline Sodden art authoring. Generated meshes are
    /// combined assets, never independent cube objects. No scene is saved.</summary>
    public static class SoddenVoxelKitBuilder
    {
        private const string Folder = "Assets/Resources/SoddenVoxel3D";
        private static readonly string[] Families = { "ground", "mire", "peat", "snag", "boards", "body" };
        private static readonly List<Vector3> Vertices = new List<Vector3>(240);
        private static readonly List<Vector2> UVs = new List<Vector2>(240);
        private static readonly List<Color> Colors = new List<Color>(240);
        private static readonly List<int> Triangles = new List<int>(360);
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
                var entries = new List<SoddenVoxelLibrary.Entry>(24);
                foreach (string family in Families)
                {
                    for (int variant = 0; variant < SoddenVoxelLibrary.VariantCount; variant++)
                    {
                        string id = SoddenVoxelLibrary.ModelId(family, variant);
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
                        entries.Add(new SoddenVoxelLibrary.Entry
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
                var library = AssetDatabase.LoadAssetAtPath<SoddenVoxelLibrary>(libraryPath);
                if (library == null)
                {
                    library = ScriptableObject.CreateInstance<SoddenVoxelLibrary>();
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
                case "ground":
                    // Exposed tops stay identical. Buried base variation creates no grid noise.
                    float thickness = .06f + variant * .008f;
                    Box(new Vector3(0, -thickness * .5f, 0), new Vector3(1, thickness, 1), 68);
                    break;
                case "mire":
                    float depth = .015f + variant * .004f;
                    Box(new Vector3(0, .048f - depth * .5f, 0), new Vector3(1, depth, 1), 24);
                    break;
                case "peat": Peat(variant); break;
                case "snag": Snag(variant); break;
                case "boards": Boards(variant); break;
                case "body": Body(variant); break;
                default: throw new ArgumentException("Unknown Sodden art family.", nameof(family));
            }
        }

        private static void Peat(int variant)
        {
            float offset = (variant - 1.5f) * .015f;
            float h = .88f + variant * .07f;
            // Broad raised cut faces carry the landmark silhouette at the gameplay zoom.
            // Both earth swatches differ from the flat olive floor, including the cap.
            Box(new Vector3(0, h * .5f, 0), new Vector3(.98f, h, .94f), 10);
            Box(new Vector3(offset, h + .095f, -.015f), new Vector3(.94f, .19f, .90f), 8);
            Box(new Vector3(-offset, h + .245f, -.04f), new Vector3(.82f, .11f, .76f), 8);
            Box(new Vector3(offset, h * .43f, .48f), new Vector3(.90f, .11f, .035f), 8);
        }

        private static void Snag(int variant)
        {
            float side = variant % 2 == 0 ? 1 : -1;
            float h = 2.70f + variant * .14f;
            float z = (variant - 1.5f) * .025f;
            // Taller bare forks keep a drowned tree readable without enlarging its native cell.
            Box(new Vector3(0, h * .5f, z), new Vector3(.30f, h, .30f), 12);
            Box(new Vector3(side * .22f, h * .60f, z), new Vector3(.42f, .24f, .24f), 12);
            Box(new Vector3(side * .36f, h * .73f, z), new Vector3(.20f, h * .31f, .21f), 12);
            Box(new Vector3(-side * .21f, h * .47f, z - .08f), new Vector3(.38f, .22f, .24f), 12);
            Box(new Vector3(-side * .33f, h * .62f, z - .08f), new Vector3(.20f, h * .31f, .21f), 12);
            Box(new Vector3(0, h + .04f, z), new Vector3(.29f, .08f, .29f), 13);
            Box(new Vector3(side * .36f, h * .885f + .03f, z), new Vector3(.20f, .06f, .21f), 13);
            Box(new Vector3(-side * .33f, h * .775f + .03f, z - .08f), new Vector3(.20f, .06f, .21f), 13);
        }

        private static void Boards(int variant)
        {
            // Sleepers follow west/east travel; broad top planks cross it north/south.
            Box(new Vector3(0, .045f, -.32f), new Vector3(.96f, .09f, .12f), 15);
            Box(new Vector3(0, .045f, .32f), new Vector3(.96f, .09f, .12f), 15);
            for (int plank = 0; plank < 3; plank++)
            {
                float length = .98f - ((variant + plank) % 4) * .018f;
                float offset = ((variant + plank) % 3 - 1) * .008f;
                Box(new Vector3((plank - 1) * .31f, .13f, offset), new Vector3(.285f, .08f, length), 11);
            }
        }

        private static void Body(int variant)
        {
            float shift = (variant - 1.5f) * .014f;
            float side = variant % 2 == 0 ? 1 : -1;
            // A prone, clothed ordinary bog body, not a generic loot container.
            Box(new Vector3(0, .145f, -.015f), new Vector3(.38f, .24f, .46f), 12);
            Box(new Vector3(shift, .205f, .32f), new Vector3(.24f, .23f, .23f), 7);
            Box(new Vector3(-.13f + shift, .095f, -.335f), new Vector3(.14f, .15f, .25f), 12);
            Box(new Vector3(.13f, .095f, -.325f), new Vector3(.14f, .15f, .26f), 12);
            Box(new Vector3(side * .15f, .29f, .06f + shift), new Vector3(.23f, .10f, .14f), 12);
            Box(new Vector3(-side * .12f, .285f, -.09f - shift), new Vector3(.21f, .10f, .13f), 12);
            Box(new Vector3(side * .04f, .31f, .065f + shift), new Vector3(.09f, .08f, .10f), 7);
            Box(new Vector3(-side * .025f, .305f, -.09f - shift), new Vector3(.085f, .08f, .09f), 7);
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
