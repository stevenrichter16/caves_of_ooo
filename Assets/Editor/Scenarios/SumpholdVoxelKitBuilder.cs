using System;
using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Reproducible offline Sumphold art. Repeated cubes become one combined
    /// mesh asset per variant; source and generated scene objects remain separate.</summary>
    public static class SumpholdVoxelKitBuilder
    {
        private const string Folder = "Assets/Resources/SumpholdVoxel3D";
        private static readonly string[] Families = { "ground", "wall", "hull", "rolls", "cutter", "water" };
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
                var entries = new List<SumpholdVoxelKitLibrary.Entry>(24);
                foreach (string family in Families)
                {
                    for (int variant = 0; variant < SumpholdVoxelKitLibrary.VariantCount; variant++)
                    {
                        string id = SumpholdVoxelKitLibrary.ModelId(family, variant);
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
                        entries.Add(new SumpholdVoxelKitLibrary.Entry
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
                var library = AssetDatabase.LoadAssetAtPath<SumpholdVoxelKitLibrary>(libraryPath);
                if (library == null)
                {
                    library = ScriptableObject.CreateInstance<SumpholdVoxelKitLibrary>();
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
                case "hull": Hull(variant); break;
                case "rolls": Rolls(variant); break;
                case "cutter": Cutter(variant); break;
                case "water": Water(variant); break;
                default: throw new ArgumentException("Unknown Sumphold art family.", nameof(family));
            }
        }

        private static void Ground(int variant)
        {
            // Dry raised Floor owners retain bog-earth color; water remains
            // a separate real native liquid owner in the surrounding cuts.
            float thickness = .065f + variant * .008f;
            Box(new Vector3(0, -thickness * .5f, 0), new Vector3(1, thickness, 1), 68);
        }

        private static void Wall(int variant)
        {
            // Native SandstoneWall remains damp masonry, never painted timber.
            float lower = .42f + variant * .045f;
            Box(new Vector3(0, lower * .5f, 0), new Vector3(1, lower, 1), 12);
            float upper = 1.05f - lower;
            Box(new Vector3(0, lower + upper * .5f, 0), new Vector3(1, upper, 1), 48);
        }

        private static void Hull(int variant)
        {
            // An upside-down shallow hull on two trestles, not a vehicle.
            // Local X follows its keel. Bare ribs leave actual gaps in the mesh.
            Box(new Vector3(-.28f, .175f, 0), new Vector3(.12f, .35f, .76f), 12);
            Box(new Vector3(.28f, .175f, 0), new Vector3(.12f, .35f, .76f), 12);
            Box(new Vector3(0, .46f, -.28f), new Vector3(.92f, .16f, .12f), 9);
            Box(new Vector3(0, .46f, .28f), new Vector3(.92f, .16f, .12f), 9);
            Box(new Vector3(0, .73f, 0), new Vector3(.96f, .12f, .12f), 9);
            float rib = .235f + variant * .015f;
            Box(new Vector3(-rib, .56f, 0), new Vector3(.11f, .18f, .52f), 9);
            Box(new Vector3(-rib, .66f, 0), new Vector3(.11f, .12f, .30f), 9);
            Box(new Vector3(rib, .56f, 0), new Vector3(.11f, .18f, .52f), 9);
            Box(new Vector3(rib, .66f, 0), new Vector3(.11f, .12f, .30f), 9);
        }

        private static void Rolls(int variant)
        {
            // Bound crossing records under a rain hood. The native examine text
            // owns their contents; no new toll payment or private witness text.
            Box(new Vector3(-.34f, .60f, -.12f), new Vector3(.12f, 1.20f, .13f), 8);
            Box(new Vector3(.34f, .60f, -.12f), new Vector3(.12f, 1.20f, .13f), 8);
            Box(new Vector3(0, .53f, 0), new Vector3(.86f, .10f, .54f), 8);
            Box(new Vector3(0, 1.22f, -.04f), new Vector3(1, .12f, .78f), 8);
            Box(new Vector3(0, .035f, 0), new Vector3(.85f, .07f, .60f), 8);
            float width = .24f + variant * .015f;
            Box(new Vector3(-.19f, .79f, .02f), new Vector3(width, .32f, .30f), 19);
            Box(new Vector3(.19f, .75f, .02f), new Vector3(width, .24f, .28f), 19);
            Box(new Vector3(-.19f, .79f, .02f), new Vector3(.055f, .34f, .32f), 8);
            Box(new Vector3(.19f, .75f, .02f), new Vector3(.055f, .26f, .30f), 8);
        }

        private static void Cutter(int variant)
        {
            // Thigh boots and the described long spade identify the profession.
            // The native Loadout still supplies its actual boots/gloves/Dagger;
            // this silhouette does not create an inventory item or digging verb.
            float shift = (variant - 1.5f) * .012f;
            Box(new Vector3(-.12f, .36f, 0), new Vector3(.18f, .72f, .26f), 12);
            Box(new Vector3(.12f, .36f, 0), new Vector3(.18f, .72f, .26f), 12);
            Box(new Vector3(0, .95f, 0), new Vector3(.44f, .46f, .32f), 13);
            Box(new Vector3(shift, 1.34f, .04f), new Vector3(.30f, .32f, .28f), 13);
            Box(new Vector3(-.28f, .93f, .02f), new Vector3(.12f, .32f, .16f), 13);
            Box(new Vector3(.29f, .93f, .02f), new Vector3(.15f, .32f, .16f), 13);
            Box(new Vector3(shift - .075f, 1.37f, .197f), new Vector3(.04f, .04f, .025f), 12);
            Box(new Vector3(shift + .075f, 1.37f, .197f), new Vector3(.04f, .04f, .025f), 12);
            Box(new Vector3(.40f, .82f, 0), new Vector3(.055f, 1.26f, .075f), 12);
            Box(new Vector3(.40f, .19f, .01f), new Vector3(.18f, .27f, .24f), 13);
        }

        private static void Water(int variant)
        {
            // This mesh follows the actual WaterPuddle owner. A constant deep
            // teal surface reads as water without a converted grass checker;
            // removing the native pool also removes this surface.
            float thickness = .075f + variant * .008f;
            Box(new Vector3(0, .012f - thickness * .5f, 0), new Vector3(1, thickness, 1), 24);
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
