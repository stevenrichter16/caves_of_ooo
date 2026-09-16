using System;
using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Reproducible offline Cinderhold art. Repeated cubes become one combined
    /// mesh asset per variant; source and generated scene objects remain separate.</summary>
    public static class CinderholdVoxelKitBuilder
    {
        private const string Folder = "Assets/Resources/CinderholdVoxel3D";
        private static readonly string[] Families = { "ground", "wall", "factor", "notice", "forge", "anvil", "smith", "stall", "token", "path" };
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
                var entries = new List<CinderholdVoxelKitLibrary.Entry>(40);
                foreach (string family in Families)
                {
                    for (int variant = 0; variant < CinderholdVoxelKitLibrary.VariantCount; variant++)
                    {
                        string id = CinderholdVoxelKitLibrary.ModelId(family, variant);
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
                        entries.Add(new CinderholdVoxelKitLibrary.Entry
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
                var library = AssetDatabase.LoadAssetAtPath<CinderholdVoxelKitLibrary>(libraryPath);
                if (library == null)
                {
                    library = ScriptableObject.CreateInstance<CinderholdVoxelKitLibrary>();
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
                case "factor": Factor(variant); break;
                case "notice": Notice(variant); break;
                case "forge": Forge(variant); break;
                case "anvil": Anvil(variant); break;
                case "smith": Smith(variant); break;
                case "stall": Stall(variant); break;
                case "token": Token(variant); break;
                case "path": Path(variant); break;
                default: throw new ArgumentException("Unknown Cinderhold art family.", nameof(family));
            }
        }

        private static void Ground(int variant)
        {
            float thickness = .065f + variant * .008f;
            Box(new Vector3(0, -thickness * .5f, 0), new Vector3(1, thickness, 1), 75);
        }

        private static void Wall(int variant)
        {
            // Broad native masonry stays low enough to expose working rooms.
            // Full-cell strata compose at corners without per-cell decorative caps.
            float lower = .47f + variant * .04f;
            Box(new Vector3(0, lower * .5f, 0), new Vector3(1, lower, 1), 12);
            float upper = 1.10f - lower;
            Box(new Vector3(0, lower + upper * .5f, 0), new Vector3(1, upper, 1), 56);
        }

        private static void Factor(int variant)
        {
            // The factor administers the native writ contract, not a new shop.
            float width = .44f + variant * .015f;
            Box(new Vector3(-.12f, .25f, 0), new Vector3(.17f, .50f, .23f), 12);
            Box(new Vector3(.12f, .25f, 0), new Vector3(.17f, .50f, .23f), 12);
            Box(new Vector3(0, .73f, 0), new Vector3(width, .90f, .34f), 19);
            Box(new Vector3(0, 1.34f, .04f), new Vector3(.30f, .34f, .28f), 19);
            Box(new Vector3(-.29f, .87f, .04f), new Vector3(.13f, .40f, .18f), 19);
            Box(new Vector3(.29f, .93f, .12f), new Vector3(.13f, .18f, .31f), 19);
            Box(new Vector3(-.075f, 1.37f, .197f), new Vector3(.04f, .04f, .025f), 12);
            Box(new Vector3(.075f, 1.37f, .197f), new Vector3(.04f, .04f, .025f), 12);
            Box(new Vector3(.12f, .94f, .24f), new Vector3(.36f, .28f, .07f), 12);
        }

        private static void Notice(int variant)
        {
            // The actual board description carries its wording and scraped date.
            // Pale posted sheets remain deliberately free of invented text.
            Box(new Vector3(-.30f, .54f, 0), new Vector3(.12f, 1.08f, .13f), 8);
            Box(new Vector3(.30f, .54f, 0), new Vector3(.12f, 1.08f, .13f), 8);
            Box(new Vector3(0, 1.15f, -.07f), new Vector3(.88f, .64f, .14f), 8);
            float shift = (variant - 1.5f) * .018f;
            Box(new Vector3(-.19f, 1.26f + shift, .014f), new Vector3(.28f, .26f, .028f), 19);
            Box(new Vector3(.18f, 1.13f - shift, .014f), new Vector3(.30f, .40f, .028f), 19);
        }

        private static void Forge(int variant)
        {
            // This is the walkable native ForgePart station. Coal color is art;
            // no Light, Thermal, collision or new fire-damage component is added.
            Box(new Vector3(0, .16f, .10f), new Vector3(.76f, .20f, .72f), 12);
            Box(new Vector3(0, .29f, .10f), new Vector3(.48f, .08f, .44f), 49);
            Box(new Vector3(-.32f, .43f, .10f), new Vector3(.17f, .32f, .72f), 12);
            Box(new Vector3(.32f, .43f, .10f), new Vector3(.17f, .32f, .72f), 12);
            Box(new Vector3(0, .43f, -.20f), new Vector3(.48f, .32f, .12f), 12);
            Box(new Vector3(0, .43f, .40f), new Vector3(.48f, .32f, .12f), 12);
            float height = 1.48f + variant * .035f;
            Box(new Vector3(0, height * .5f, -.32f), new Vector3(.28f, height, .28f), 12);
        }

        private static void Anvil(int variant)
        {
            // Heavy solid Handling fixture, not a second ForgePart. Local +X
            // is its horn; its narrow waist distinguishes it from a stone block.
            Box(new Vector3(0, .09f, 0), new Vector3(.58f, .18f, .50f), 12);
            float width = .20f + variant * .015f;
            Box(new Vector3(0, .35f, 0), new Vector3(width, .34f, .24f), 12);
            Box(new Vector3(0, .60f, 0), new Vector3(.68f, .16f, .35f), 13);
            Box(new Vector3(.38f, .57f, 0), new Vector3(.20f, .11f, .17f), 13);
        }

        private static void Smith(int variant)
        {
            // Existing Weaponsmith retains actual shop stock and equipment;
            // broad bare forearms and the apron identify the working silhouette.
            float width = .47f + variant * .02f;
            Box(new Vector3(-.12f, .25f, 0), new Vector3(.18f, .50f, .24f), 8);
            Box(new Vector3(.12f, .25f, 0), new Vector3(.18f, .50f, .24f), 8);
            Box(new Vector3(0, .79f, 0), new Vector3(width, .63f, .36f), 8);
            Box(new Vector3(0, 1.30f, .04f), new Vector3(.32f, .34f, .29f), 32);
            Box(new Vector3(-.34f, .86f, .06f), new Vector3(.20f, .40f, .25f), 32);
            Box(new Vector3(.34f, .86f, .06f), new Vector3(.20f, .40f, .25f), 32);
            Box(new Vector3(-.08f, 1.33f, .201f), new Vector3(.045f, .045f, .026f), 8);
            Box(new Vector3(.08f, 1.33f, .201f), new Vector3(.045f, .045f, .026f), 8);
        }

        private static void Stall(int variant)
        {
            // The native solid stall remains one owner. A broad awning, low
            // counter and open serving gap establish its trading silhouette.
            Box(new Vector3(-.38f, .72f, -.25f), new Vector3(.10f, 1.44f, .10f), 8);
            Box(new Vector3(.38f, .72f, -.25f), new Vector3(.10f, 1.44f, .10f), 8);
            Box(new Vector3(-.38f, .72f, .25f), new Vector3(.10f, 1.44f, .10f), 8);
            Box(new Vector3(.38f, .72f, .25f), new Vector3(.10f, 1.44f, .10f), 8);
            Box(new Vector3(0, 1.49f, 0), new Vector3(1, .10f, .80f), 35);
            Box(new Vector3(0, .64f, .04f), new Vector3(.86f, .16f, .58f), 8);
            float width = .16f + variant * .018f;
            Box(new Vector3(-.20f, .79f, .04f), new Vector3(width, .14f, .18f), 35);
            Box(new Vector3(.18f, .77f, .07f), new Vector3(.20f, .10f, .19f), 35);
        }

        private static void Token(int variant)
        {
            // A carved name-token with an open cord loop, not a generic item
            // alias. Dynamic quest identity is verified by the native renderer.
            float width = .34f + variant * .025f;
            Box(new Vector3(0, .055f, .20f), new Vector3(width, .11f, .30f), 9);
            Box(new Vector3(0, .025f, -.37f), new Vector3(.42f, .05f, .06f), 19);
            Box(new Vector3(-.18f, .025f, -.19f), new Vector3(.06f, .05f, .36f), 19);
            Box(new Vector3(.18f, .025f, -.19f), new Vector3(.06f, .05f, .36f), 19);
            Box(new Vector3(0, .03f, -.005f), new Vector3(.42f, .06f, .06f), 19);
            Box(new Vector3(0, .065f, .035f), new Vector3(.10f, .13f, .09f), 19);
        }

        private static void Path(int variant)
        {
            // A mid-value road joins native public routes quietly. Variation
            // stays buried so wide junctions cannot become a tile checkerboard.
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
