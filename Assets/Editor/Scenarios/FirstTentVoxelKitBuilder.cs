using System;
using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Reproducible offline FirstTent art. Repeated cubes become one combined
    /// mesh asset per variant; source and generated scene objects remain separate.</summary>
    public static class FirstTentVoxelKitBuilder
    {
        private const string Folder = "Assets/Resources/FirstTentVoxel3D";
        private static readonly string[] Families = { "ground", "path", "tent", "corner", "cloth", "host" };
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
                var entries = new List<FirstTentVoxelKitLibrary.Entry>(24);
                foreach (string family in Families)
                {
                    for (int variant = 0; variant < FirstTentVoxelKitLibrary.VariantCount; variant++)
                    {
                        string id = FirstTentVoxelKitLibrary.ModelId(family, variant);
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
                        entries.Add(new FirstTentVoxelKitLibrary.Entry
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
                var library = AssetDatabase.LoadAssetAtPath<FirstTentVoxelKitLibrary>(libraryPath);
                if (library == null)
                {
                    library = ScriptableObject.CreateInstance<FirstTentVoxelKitLibrary>();
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
                case "path": Path(variant); break;
                case "tent": Tent(variant); break;
                case "corner": Corner(variant); break;
                case "cloth": Cloth(variant); break;
                case "host": Host(variant); break;
                default: throw new ArgumentException("Unknown FirstTent art family.", nameof(family));
            }
        }

        private static void Ground(int variant)
        {
            // Constant visible sand; variants change buried thickness only.
            float thickness = .065f + variant * .008f;
            Box(new Vector3(0, -thickness * .5f, 0), new Vector3(1, thickness, 1), 64);
        }

        private static void Path(int variant)
        {
            float thickness = .065f + variant * .008f;
            Box(new Vector3(0, -thickness * .5f, 0), new Vector3(1, thickness, 1), 106);
        }

        private static void Tent(int variant)
        {
            // A woven goat-hair cutaway, not masonry or a hidden roof. The
            // existing native wall neighbors choose the actual orientation.
            Box(new Vector3(0, .49f, 0), new Vector3(1, .98f, .16f), 12);
            Box(new Vector3(0, 1.02f, 0), new Vector3(1, .08f, .14f), 64);
            float post = -.43f + variant * .03f;
            Box(new Vector3(post, .55f, 0), new Vector3(.10f, 1.10f, .24f), 64);
        }

        private static void Corner(int variant)
        {
            // Join local +X and +Z neighbors from the center of this owner.
            // The empty quadrant is an actual opening, not painted floor.
            Box(new Vector3(.25f, .49f, 0), new Vector3(.50f, .98f, .16f), 12);
            Box(new Vector3(0, .49f, .25f), new Vector3(.16f, .98f, .50f), 12);
            Box(new Vector3(.25f, 1.02f, 0), new Vector3(.50f, .08f, .14f), 64);
            Box(new Vector3(0, 1.02f, .25f), new Vector3(.14f, .08f, .50f), 64);
            float width = .10f + variant * .012f;
            Box(new Vector3(0, .55f, 0), new Vector3(width, 1.10f, width), 64);
        }

        private static void Cloth(int variant)
        {
            // A dark, unmarked guest-cloth: the host's native oath supplies
            // hospitality. This nonsolid examinable owner gains no altar/aura.
            Box(new Vector3(-.30f, 1.10f, 0), new Vector3(.10f, 2.20f, .10f), 32);
            Box(new Vector3(.04f, 2.06f, 0), new Vector3(.80f, .08f, .10f), 32);
            float height = .54f + variant * .03f;
            Box(new Vector3(.04f, 2.01f - height * .5f, .01f), new Vector3(.72f, height, .08f), 12);
        }

        private static void Host(int variant)
        {
            // Human keeper, weathered cap and an open receiving hand. No
            // ritual pedestal or inherited deity imagery is attached.
            float shift = (variant - 1.5f) * .012f;
            Box(new Vector3(-.12f, .25f, 0), new Vector3(.17f, .50f, .23f), 12);
            Box(new Vector3(.12f, .25f, 0), new Vector3(.17f, .50f, .23f), 12);
            Box(new Vector3(0, .82f, 0), new Vector3(.47f, .68f, .32f), 12);
            Box(new Vector3(shift, 1.34f, .04f), new Vector3(.29f, .31f, .27f), 32);
            Box(new Vector3(shift, 1.51f, 0), new Vector3(.36f, .09f, .34f), 12);
            Box(new Vector3(-.29f, .81f, .08f), new Vector3(.14f, .39f, .18f), 12);
            Box(new Vector3(.30f, .81f, .20f), new Vector3(.15f, .13f, .40f), 12);
            Box(new Vector3(.30f, .81f, .39f), new Vector3(.15f, .13f, .09f), 32);
            Box(new Vector3(shift - .07f, 1.37f, .184f), new Vector3(.04f, .04f, .025f), 12);
            Box(new Vector3(shift + .07f, 1.37f, .184f), new Vector3(.04f, .04f, .025f), 12);
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
