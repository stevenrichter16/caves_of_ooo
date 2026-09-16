using System;
using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Reproducible offline Marrowstye art. Repeated cubes become one combined
    /// mesh asset per variant; source and generated scene objects remain separate.</summary>
    public static class MarrowstyeVoxelKitBuilder
    {
        private const string Folder = "Assets/Resources/MarrowstyeVoxel3D";
        private static readonly string[] Families = { "ground", "wall", "coffer", "cured", "clerk", "path" };
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
                var entries = new List<MarrowstyeVoxelKitLibrary.Entry>(24);
                foreach (string family in Families)
                {
                    for (int variant = 0; variant < MarrowstyeVoxelKitLibrary.VariantCount; variant++)
                    {
                        string id = MarrowstyeVoxelKitLibrary.ModelId(family, variant);
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
                        entries.Add(new MarrowstyeVoxelKitLibrary.Entry
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
                var library = AssetDatabase.LoadAssetAtPath<MarrowstyeVoxelKitLibrary>(libraryPath);
                if (library == null)
                {
                    library = ScriptableObject.CreateInstance<MarrowstyeVoxelKitLibrary>();
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
                case "coffer": Coffer(variant); break;
                case "cured": Cured(variant); break;
                case "clerk": Clerk(variant); break;
                case "path": Path(variant); break;
                default: throw new ArgumentException("Unknown Marrowstye art family.", nameof(family));
            }
        }

        private static void Ground(int variant)
        {
            float thickness = .065f + variant * .008f;
            Box(new Vector3(0, -thickness * .5f, 0), new Vector3(1, thickness, 1), 64);
        }

        private static void Wall(int variant)
        {
            // Quiet pale cutaway stone defines the ordinary intake chambers;
            // it does not borrow the sealed archive's special wall materials.
            float lower = .47f + variant * .035f;
            Box(new Vector3(0, lower * .5f, 0), new Vector3(1, lower, 1), 13);
            float upper = 1.05f - lower;
            Box(new Vector3(0, lower + upper * .5f, 0), new Vector3(1, upper, 1), 35);
        }

        private static void Coffer(int variant)
        {
            // A solid stone block and broad lid, without chest hardware. The
            // native Handling owner can be hauled but has no Container Part.
            float width = .80f + variant * .02f;
            Box(new Vector3(0, .03f, 0), new Vector3(.78f, .06f, .64f), 106);
            Box(new Vector3(0, .30f, 0), new Vector3(width, .54f, .67f), 106);
            Box(new Vector3(0, .60f, 0), new Vector3(width + .08f, .12f, .75f), 35);
        }

        private static void Cured(int variant)
        {
            // Ordinary village dead preserved with salt, not the three ancient
            // witnesses and not the sealed courier package. Keep human limbs.
            float width = .36f + variant * .012f;
            Box(new Vector3(0, .17f, -.015f), new Vector3(width, .27f, .42f), 95);
            Box(new Vector3(0, .18f, -.34f), new Vector3(.27f, .25f, .23f), 95);
            Box(new Vector3(0, .15f, -.235f), new Vector3(.15f, .16f, .08f), 95);
            Box(new Vector3(-.23f, .13f, -.02f), new Vector3(.12f, .20f, .40f), 106);
            Box(new Vector3(.23f, .13f, -.02f), new Vector3(.12f, .20f, .40f), 106);
            Box(new Vector3(-.105f, .08f, .325f), new Vector3(.14f, .16f, .29f), 95);
            Box(new Vector3(.105f, .08f, .325f), new Vector3(.14f, .16f, .29f), 95);
            Box(new Vector3(0, .32f, -.015f), new Vector3(.24f, .06f, .24f), 106);
        }

        private static void Clerk(int variant)
        {
            // The intake window is a person: a held ledger and raised stamp
            // distinguish this conversational actor without attaching a desk.
            float width = .42f + variant * .012f;
            Box(new Vector3(-.12f, .245f, 0), new Vector3(.17f, .49f, .23f), 13);
            Box(new Vector3(.12f, .245f, 0), new Vector3(.17f, .49f, .23f), 13);
            Box(new Vector3(0, .78f, 0), new Vector3(width, .64f, .33f), 13);
            Box(new Vector3(0, 1.30f, .035f), new Vector3(.30f, .32f, .28f), 19);
            Box(new Vector3(-.28f, .87f, .12f), new Vector3(.13f, .28f, .24f), 13);
            Box(new Vector3(.29f, 1.035f, .11f), new Vector3(.13f, .18f, .23f), 19);
            Box(new Vector3(-.06f, .91f, .265f), new Vector3(.39f, .26f, .075f), 19);
            Box(new Vector3(.30f, 1.16f, .17f), new Vector3(.13f, .14f, .13f), 13);
            Box(new Vector3(-.075f, 1.33f, .191f), new Vector3(.04f, .04f, .026f), 13);
            Box(new Vector3(.075f, 1.33f, .191f), new Vector3(.04f, .04f, .026f), 13);
        }

        private static void Path(int variant)
        {
            // Real RoadStone marks the receiving route across muted earth.
            // A constant quiet surface avoids checker noise over wide aprons.
            float thickness = .065f + variant * .008f;
            Box(new Vector3(0, -thickness * .5f, 0), new Vector3(1, thickness, 1), 106);
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
