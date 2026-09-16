using System;
using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Reproducible offline Olderdeep art. Repeated cubes become one combined
    /// mesh asset per variant; source and generated scene objects remain separate.</summary>
    public static class OlderdeepVoxelKitBuilder
    {
        private const string Folder = "Assets/Resources/OlderdeepVoxel3D";
        private static readonly string[] Families = { "ground", "rooted", "plume", "niche", "plaque", "oldest", "jar", "listener", "tender", "wall" };
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
                var entries = new List<OlderdeepVoxelLibrary.Entry>(40);
                foreach (string family in Families)
                {
                    for (int variant = 0; variant < OlderdeepVoxelLibrary.VariantCount; variant++)
                    {
                        string id = OlderdeepVoxelLibrary.ModelId(family, variant);
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
                        entries.Add(new OlderdeepVoxelLibrary.Entry
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
                var library = AssetDatabase.LoadAssetAtPath<OlderdeepVoxelLibrary>(libraryPath);
                if (library == null)
                {
                    library = ScriptableObject.CreateInstance<OlderdeepVoxelLibrary>();
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
                case "rooted": Rooted(variant); break;
                case "plume": Plume(variant); break;
                case "niche": Niche(variant); break;
                case "plaque": Plaque(variant); break;
                case "oldest": Oldest(variant); break;
                case "jar": Jar(variant); break;
                case "listener": Listener(variant); break;
                case "tender": Tender(variant); break;
                case "wall": Wall(variant); break;
                default: throw new ArgumentException("Unknown Olderdeep art family.", nameof(family));
            }
        }

        private static void Ground(int variant)
        {
            float thickness = .065f + variant * .008f;
            Box(new Vector3(0, -thickness * .5f, 0), new Vector3(1, thickness, 1), 12);
        }

        private static void Rooted(int variant)
        {
            // Native single-cell fixture, fixed eastward (+X) embrace. No pedestal,
            // animation, or sculpture across the real empty eastern chamber gap.
            Box(new Vector3(-.35f, .04f, -.14f), new Vector3(.26f, .08f, .12f), 79);
            Box(new Vector3(-.35f, .04f, .14f), new Vector3(.26f, .08f, .12f), 79);
            Box(new Vector3(-.24f, .205f, -.16f), new Vector3(.20f, .19f, .14f), 79);
            Box(new Vector3(-.24f, .205f, .16f), new Vector3(.20f, .19f, .14f), 79);
            Box(new Vector3(0, .32f, 0), new Vector3(.34f, .27f, .30f), 79);
            Box(new Vector3(.11f, .515f, 0), new Vector3(.22f, .22f, .22f), 79);
            Box(new Vector3(.285f, .37f, -.24f), new Vector3(.41f, .13f, .13f), 79);
            Box(new Vector3(.285f, .37f, .24f), new Vector3(.41f, .13f, .13f), 79);
            Box(new Vector3(0, .64f, -.02f), new Vector3(.14f, .26f, .16f), 19);
            float crownWidth = .28f + variant * .02f;
            Box(new Vector3(0, .78f, -.02f), new Vector3(crownWidth, .12f, .22f), 19);
        }

        private static void Plume(int variant)
        {
            // Eleven native owners remain separate soft clusters. Broken square
            // corners and broad overlapping lobes avoid the former flat carpet.
            float shift = (variant - 1.5f) * .012f;
            Box(new Vector3(0, .035f, 0), new Vector3(.76f, .07f, .78f), 48);
            Box(new Vector3(-.14f + shift, .11f, -.13f), new Vector3(.60f, .16f, .58f), 50);
            Box(new Vector3(.14f + shift, .17f, .10f), new Vector3(.60f, .20f, .62f), 50);
            Box(new Vector3(-.10f, .10f, .29f + shift), new Vector3(.60f, .13f, .30f), 50);
        }

        private static void Niche(int variant)
        {
            // Recessed home opens local +Z toward the native chamber interior.
            // Bedding is art; the native examinable niche supplies no sleep verb.
            Box(new Vector3(0, .06f, 0), new Vector3(1, .12f, .96f), 56);
            Box(new Vector3(-.43f, .53f, 0), new Vector3(.14f, .94f, .90f), 56);
            Box(new Vector3(.43f, .53f, 0), new Vector3(.14f, .94f, .90f), 56);
            Box(new Vector3(0, .53f, -.40f), new Vector3(.72f, .94f, .12f), 56);
            Box(new Vector3(0, 1.03f, .12f), new Vector3(.72f, .12f, .66f), 56);
            float beddingWidth = .52f + variant * .035f;
            Box(new Vector3(0, .19f, -.10f), new Vector3(beddingWidth, .14f, .52f), 19);
        }

        private static void Plaque(int variant)
        {
            Box(new Vector3(0, .06f, 0), new Vector3(1, .12f, .44f), 56);
            Box(new Vector3(0, .53f, -.08f), new Vector3(.90f, .94f, .20f), 56);
            float width = .64f + variant * .04f;
            Box(new Vector3(0, .64f, .034f), new Vector3(width, .40f, .028f), 19);
        }

        private static void Oldest(int variant)
        {
            // Pre-alphabet names remain close to the floor, without invented text.
            Box(new Vector3(0, .055f, 0), new Vector3(.94f, .11f, .46f), 56);
            float width = .66f + variant * .04f;
            Box(new Vector3(0, .24f, -.06f), new Vector3(width, .30f, .18f), 19);
        }

        private static void Jar(int variant)
        {
            // A stopped hanging jar. Only the native LightSource emits light;
            // its descriptive alarm is not implemented as a new mesh component.
            Box(new Vector3(-.28f, .64f, 0), new Vector3(.10f, 1.28f, .10f), 12);
            Box(new Vector3(-.04f, 1.29f, 0), new Vector3(.58f, .10f, .10f), 12);
            Box(new Vector3(.18f, 1.10f, 0), new Vector3(.05f, .30f, .05f), 12);
            float width = .28f + variant * .025f;
            Box(new Vector3(.18f, .82f, 0), new Vector3(width, .36f, .28f), 49);
            Box(new Vector3(.18f, 1.02f, 0), new Vector3(.18f, .07f, .18f), 12);
            Box(new Vector3(.18f, .63f, 0), new Vector3(width, .07f, .28f), 12);
        }

        private static void Listener(int variant)
        {
            float shift = (variant - 1.5f) * .012f;
            Box(new Vector3(-.12f, .24f, 0), new Vector3(.17f, .48f, .23f), 56);
            Box(new Vector3(.12f, .24f, 0), new Vector3(.17f, .48f, .23f), 56);
            Box(new Vector3(0, .78f, 0), new Vector3(.48f, .66f, .33f), 56);
            Box(new Vector3(shift, 1.30f, .04f), new Vector3(.29f, .34f, .27f), 19);
            Box(new Vector3(-.29f, .74f, 0), new Vector3(.13f, .42f, .16f), 56);
            Box(new Vector3(.28f, 1.00f, .21f), new Vector3(.14f, .15f, .50f), 56);
            Box(new Vector3(.28f, 1.02f, .45f), new Vector3(.14f, .19f, .08f), 19);
            Box(new Vector3(shift - .075f, 1.34f, .184f), new Vector3(.045f, .04f, .025f), 56);
            Box(new Vector3(shift + .075f, 1.34f, .184f), new Vector3(.045f, .04f, .025f), 56);
        }

        private static void Tender(int variant)
        {
            float shift = (variant - 1.5f) * .015f;
            Box(new Vector3(-.15f, .12f, -.03f), new Vector3(.22f, .24f, .46f), 56);
            Box(new Vector3(.15f, .12f, -.03f), new Vector3(.22f, .24f, .46f), 56);
            Box(new Vector3(0, .51f, .02f), new Vector3(.45f, .57f, .34f), 19);
            Box(new Vector3(shift, .94f, .14f), new Vector3(.30f, .32f, .28f), 56);
            Box(new Vector3(-.28f, .53f, .20f), new Vector3(.13f, .14f, .40f), 19);
            Box(new Vector3(.28f, .42f, .24f), new Vector3(.13f, .14f, .42f), 19);
            Box(new Vector3(.28f, .40f, .44f), new Vector3(.13f, .13f, .09f), 56);
            Box(new Vector3(shift - .075f, .98f, .288f), new Vector3(.04f, .04f, .025f), 19);
            Box(new Vector3(shift + .075f, .98f, .288f), new Vector3(.04f, .04f, .025f), 19);
        }

        private static void Wall(int variant)
        {
            // Only the sacred floor's current native wall owners borrow this
            // cutaway. Keep Ginmere cave stone, not the Cathedral's pale vault.
            float lower = .40f + variant * .055f;
            Box(new Vector3(0, lower * .5f, 0), new Vector3(1, lower, 1), 12);
            float upper = 1.10f - lower;
            Box(new Vector3(0, lower + upper * .5f, 0), new Vector3(1, upper, 1), 13);
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
