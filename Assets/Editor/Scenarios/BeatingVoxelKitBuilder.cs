using System;
using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Reproducible offline Beating art authoring. Generated meshes are
    /// combined assets, never independent cube objects. No scene is saved.</summary>
    public static class BeatingVoxelKitBuilder
    {
        private const string Folder = "Assets/Resources/BeatingVoxel3D";
        private static readonly string[] Families = { "sand", "pan", "road", "crust", "dune", "ruin", "vein", "brine", "bones", "sign", "rubble", "briar" };
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
                var entries = new List<BeatingVoxelLibrary.Entry>(48);
                foreach (string family in Families)
                {
                    for (int variant = 0; variant < BeatingVoxelLibrary.VariantCount; variant++)
                    {
                        string id = BeatingVoxelLibrary.ModelId(family, variant);
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
                        entries.Add(new BeatingVoxelLibrary.Entry
                        {
                            Id = id, Prefab = prefab, Mesh = mesh,
                            Spec = new SpawnRing3DCatalog.Model
                            {
                                id = id, path = prefabPath, kind = family == "sand" || family == "pan" || family == "road" ? "ground" : "entity",
                                materialFamily = "ring-palette", rigFamily = "none",
                                boundsCenter = mesh.bounds.center, boundsSize = mesh.bounds.size,
                                triangles = Triangles.Count / 3, clips = Array.Empty<string>(), sockets = Array.Empty<string>()
                            }
                        });
                    }
                }
                string libraryPath = Folder + "/Library.asset";
                var library = AssetDatabase.LoadAssetAtPath<BeatingVoxelLibrary>(libraryPath);
                if (library == null)
                {
                    library = ScriptableObject.CreateInstance<BeatingVoxelLibrary>();
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
                case "sand": Floor(variant, 32); break;
                case "pan": Floor(variant, 19); break;
                case "road": Floor(variant, 64); break;
                case "brine":
                    float depth = .015f + variant * .004f;
                    Box(new Vector3(0, .048f - depth * .5f, 0), new Vector3(1, depth, 1), 25);
                    break;
                case "crust": Crust(variant); break;
                case "dune": Dune(variant); break;
                case "ruin": Ruin(variant); break;
                case "vein": Vein(variant); break;
                case "bones": Bones(variant); break;
                case "sign": Sign(variant); break;
                case "rubble": Rubble(variant); break;
                case "briar": Briar(variant); break;
                default: throw new ArgumentException("Unknown Beating art family.", nameof(family));
            }
        }

        private static void Floor(int variant, int palette)
        {
            // Exposed tops stay identical. The variant is buried, so broad surfaces remain quiet.
            float thickness = .06f + variant * .008f;
            Box(new Vector3(0, -thickness * .5f, 0), new Vector3(1, thickness, 1), palette);
        }

        private static void Crust(int variant)
        {
            float shift = (variant - 1.5f) * .018f;
            Plate(new Vector3(-.22f, 0, -.16f + shift), new Vector2(.46f, .56f), .10f + variant * .015f);
            Plate(new Vector3(.23f, 0, -.10f - shift), new Vector2(.40f, .64f), .14f + variant * .01f);
            Plate(new Vector3(shift, 0, .33f), new Vector2(.72f, .24f), .085f + variant * .012f);
        }

        private static void Plate(Vector3 position, Vector2 size, float height)
        {
            Box(position + new Vector3(0, height * .5f, 0), new Vector3(size.x, height, size.y), 13);
            Box(position + new Vector3(0, height + .025f, 0), new Vector3(size.x, .05f, size.y), 26);
        }

        private static void Dune(int variant)
        {
            // Native neighbors select flank/shoulder/crest/peak. Full-X slabs join
            // equal-height owners without a decorative groove at every cell boundary.
            float peak = variant == 0 ? .54f : variant == 1 ? .85f : variant == 2 ? 1.25f : 1.58f;
            float baseHeight = peak * .52f;
            float upperHeight = peak - baseHeight;
            Box(new Vector3(0, baseHeight * .5f, 0), new Vector3(1, baseHeight, 1), 7);
            Box(new Vector3(0, baseHeight + upperHeight * .5f, .125f), new Vector3(1, upperHeight, .75f), 32);
        }

        private static void Ruin(int variant)
        {
            float leftHeight = 1.65f + variant * .15f;
            float rightHeight = 1.28f + (3 - variant) * .11f;
            float shift = (variant - 1.5f) * .012f;
            // The continuous foundation keeps the native wall legible; upper losses tell its age.
            Box(new Vector3(0, .12f, 0), new Vector3(.98f, .24f, .98f), 12);
            Box(new Vector3(0, .66f, 0), new Vector3(.98f, 1.08f, .70f), 76);
            Box(new Vector3(-.25f, leftHeight * .5f, shift), new Vector3(.46f, leftHeight, .66f), 76);
            Box(new Vector3(.25f, rightHeight * .5f, -shift), new Vector3(.46f, rightHeight, .66f), 76);
            Box(new Vector3(-.25f, leftHeight - .06f, shift), new Vector3(.42f, .12f, .64f), 12);
            Box(new Vector3(0, .77f + variant * .025f, .358f), new Vector3(.91f, .065f, .024f), 12);
        }

        private static void Vein(int variant)
        {
            float shift = (variant - 1.5f) * .025f;
            Box(new Vector3(0, .16f, 0), new Vector3(.83f, .32f, .72f), 12);
            float h = 1.22f + variant * .08f;
            Box(new Vector3(shift, h * .5f, .08f), new Vector3(.25f, h, .29f), 26);
            h = .83f + ((variant + 1) % 4) * .08f;
            Box(new Vector3(-.27f, h * .5f, -.06f), new Vector3(.22f, h, .24f), 26);
            h = .68f + ((variant + 2) % 4) * .08f;
            Box(new Vector3(.26f, h * .5f, -.12f), new Vector3(.24f, h, .25f), 26);
        }

        private static void Bones(int variant)
        {
            float shift = (variant - 1.5f) * .018f;
            Box(new Vector3(shift, .095f, -.08f), new Vector3(.10f, .12f, .67f), 19);
            for (int rib = 0; rib < 3; rib++)
            {
                float z = -.24f + rib * .18f;
                float width = .22f + ((rib + variant) % 3) * .025f;
                Box(new Vector3(-width * .5f + shift, .14f, z), new Vector3(width, .10f, .085f), 19);
                Box(new Vector3(width * .5f + shift, .14f, z), new Vector3(width, .10f, .085f), 19);
            }
            Box(new Vector3(shift, .16f, .335f), new Vector3(.28f, .25f, .25f), 19);
            Box(new Vector3(shift - .073f, .291f, .34f), new Vector3(.055f, .022f, .055f), 10);
            Box(new Vector3(shift + .073f, .291f, .34f), new Vector3(.055f, .022f, .055f), 10);
        }

        private static void Sign(int variant)
        {
            float side = variant % 2 == 0 ? 1 : -1;
            float h = 1.70f + variant * .07f;
            Box(new Vector3(0, h * .5f, 0), new Vector3(.18f, h, .18f), 15);
            Box(new Vector3(0, h * .86f, .02f), new Vector3(.78f, .24f, .17f), 11);
            Box(new Vector3(side * .405f, h * .86f, .02f), new Vector3(.15f, .13f, .17f), 11);
            Box(new Vector3(-side * .12f, h * .65f, -.005f), new Vector3(.56f, .18f, .17f), 11);
            Box(new Vector3(-side * .405f, h * .65f, -.005f), new Vector3(.15f, .09f, .17f), 11);
        }

        private static void Rubble(int variant)
        {
            float shift = (variant - 1.5f) * .02f;
            Box(new Vector3(-.20f, .17f + variant * .02f, -.12f), new Vector3(.43f, .34f + variant * .04f, .52f), 76);
            Box(new Vector3(.23f, .12f, -.18f + shift), new Vector3(.39f, .24f, .40f), 12);
            Box(new Vector3(shift, .10f, .30f), new Vector3(.62f, .20f, .30f), 76);
            Box(new Vector3(.20f, .28f + variant * .015f, .05f), new Vector3(.32f, .18f, .29f), 76);
        }

        private static void Briar(int variant)
        {
            float side = variant % 2 == 0 ? 1 : -1;
            float h = .69f + variant * .08f;
            Box(new Vector3(0, h * .5f, 0), new Vector3(.11f, h, .11f), 12);
            Box(new Vector3(0, h * .48f, 0), new Vector3(.70f, .105f, .11f), 12);
            Box(new Vector3(-side * .28f, h * .52f, -.08f), new Vector3(.10f, h * .51f, .10f), 12);
            Box(new Vector3(side * .28f, h * .43f, .08f), new Vector3(.10f, h * .42f, .10f), 12);
            Box(new Vector3(0, h + .045f, 0), new Vector3(.25f, .09f, .20f), 19);
            Box(new Vector3(-side * .28f, h * .775f + .025f, -.08f), new Vector3(.23f, .10f, .19f), 19);
            Box(new Vector3(side * .28f, h * .64f + .025f, .08f), new Vector3(.23f, .10f, .19f), 19);
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
