using System;
using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Reproducible offline Stillleaf art. Repeated cubes become one combined
    /// mesh asset per variant; source and generated scene objects remain separate.</summary>
    public static class StillleafVoxelKitBuilder
    {
        private const string Folder = "Assets/Resources/StillleafVoxel3D";
        private static readonly string[] Families = { "ground", "floor", "tepuibone", "marble", "iron", "door", "open-door", "shelf", "bear", "slime", "spring", "boots", "wall" };
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
                var entries = new List<StillleafVoxelLibrary.Entry>(52);
                foreach (string family in Families)
                {
                    for (int variant = 0; variant < StillleafVoxelLibrary.VariantCount; variant++)
                    {
                        string id = StillleafVoxelLibrary.ModelId(family, variant);
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
                        entries.Add(new StillleafVoxelLibrary.Entry
                        {
                            Id = id, Prefab = prefab, Mesh = mesh,
                            Spec = new SpawnRing3DCatalog.Model
                            {
                                id = id, path = prefabPath, kind = (family == "ground" || family == "floor") ? "ground" : "entity",
                                materialFamily = "ring-palette", rigFamily = "none",
                                boundsCenter = mesh.bounds.center, boundsSize = mesh.bounds.size,
                                triangles = Triangles.Count / 3, clips = Array.Empty<string>(), sockets = Array.Empty<string>()
                            }
                        });
                    }
                }
                string libraryPath = Folder + "/Library.asset";
                var library = AssetDatabase.LoadAssetAtPath<StillleafVoxelLibrary>(libraryPath);
                if (library == null)
                {
                    library = ScriptableObject.CreateInstance<StillleafVoxelLibrary>();
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
                case "floor": Floor(variant); break;
                case "tepuibone": Tepuibone(variant); break;
                case "marble": Marble(variant); break;
                case "iron": Iron(variant); break;
                case "door": Door(variant); break;
                case "open-door": OpenDoor(variant); break;
                case "shelf": Shelf(variant); break;
                case "bear": Bear(variant); break;
                case "slime": Slime(variant); break;
                case "spring": Spring(variant); break;
                case "boots": Boots(variant); break;
                case "wall": Wall(variant); break;
                default: throw new ArgumentException("Unknown Stillleaf art family.", nameof(family));
            }
        }

        private static void Ground(int variant)
        {
            float thickness = .065f + variant * .008f;
            Box(new Vector3(0, -thickness * .5f, 0), new Vector3(1, thickness, 1), 64);
        }

        private static void Floor(int variant)
        {
            float thickness = .065f + variant * .008f;
            // The native excluded-arrival floor stays dark and continuously flat.
            Box(new Vector3(0, -thickness * .5f, 0), new Vector3(1, thickness, 1), 12);
        }

        private static void Tepuibone(int variant)
        {
            // Broad pale courses; each full cell joins its native neighbor.
            float lower = .24f + variant * .035f;
            Box(new Vector3(0, lower * .5f, 0), new Vector3(1, lower, 1), 79);
            float middle = 1.62f - lower;
            Box(new Vector3(0, lower + middle * .5f, 0), new Vector3(1, middle, 1), 19);
            Box(new Vector3(0, 1.71f, 0), new Vector3(1, .18f, 1), 79);
        }

        private static void Marble(int variant)
        {
            // One broad cloud band instead of mottled per-voxel marble noise.
            float lower = .50f + variant * .05f;
            Box(new Vector3(0, lower * .5f, 0), new Vector3(1, lower, 1), 125);
            Box(new Vector3(0, lower + .16f, 0), new Vector3(1, .32f, 1), 35);
            float upper = 1.8f - lower - .32f;
            Box(new Vector3(0, lower + .32f + upper * .5f, 0), new Vector3(1, upper, 1), 125);
        }

        private static void Iron(int variant)
        {
            // Vertical dull ribs are broad material bands of a continuous wall,
            // not tiny rivets or protruding pieces outside the native cell.
            float inner = .18f + variant * .015f;
            float outer = inner + .14f;
            Box(new Vector3(0, .10f, 0), new Vector3(1, .20f, 1), 12);
            Box(new Vector3(0, .95f, 0), new Vector3(inner * 2, 1.5f, 1), 12);
            Box(new Vector3(-(inner + outer) * .5f, .95f, 0), new Vector3(.14f, 1.5f, 1), 13);
            Box(new Vector3((inner + outer) * .5f, .95f, 0), new Vector3(.14f, 1.5f, 1), 13);
            float edge = .5f - outer;
            Box(new Vector3(-.5f + edge * .5f, .95f, 0), new Vector3(edge, 1.5f, 1), 12);
            Box(new Vector3(.5f - edge * .5f, .95f, 0), new Vector3(edge, 1.5f, 1), 12);
            Box(new Vector3(0, 1.75f, 0), new Vector3(1, .10f, 1), 12);
        }

        private static void Door(int variant)
        {
            // The sealed iron panel is flush in a pale frame. Local travel axis Z;
            // the native recipe rotates the west-facing doorway as one owner.
            Box(new Vector3(-.41f, .86f, 0), new Vector3(.18f, 1.72f, .44f), 19);
            Box(new Vector3(.41f, .86f, 0), new Vector3(.18f, 1.72f, .44f), 19);
            Box(new Vector3(0, 1.77f, 0), new Vector3(1, .16f, .44f), 19);
            Box(new Vector3(0, .84f, 0), new Vector3(.64f, 1.64f, .15f), 12);
            Box(new Vector3(.13f + variant * .015f, .84f, .09f), new Vector3(.09f, .09f, .06f), 19);
        }

        private static void OpenDoor(int variant)
        {
            // The same unlocked native owner is a low threshold. Omitting a tall
            // lintel keeps the newly walkable cell unambiguously open at this camera.
            Box(new Vector3(0, .05f, 0), new Vector3(1, .10f, .44f), 19);
            Box(new Vector3(0, .13f, 0), new Vector3(.64f, .06f, .26f + variant * .025f), 12);
        }

        private static void Shelf(int variant)
        {
            // Fixed shallow stone shelves with two tied bundles; no invented
            // Container/Readable mechanics or speculative book loot.
            float bundleHeight = .76f + variant * .04f;
            Box(new Vector3(0, .10f, 0), new Vector3(.90f, .20f, .46f), 19);
            Box(new Vector3(-.405f, .65f, 0), new Vector3(.09f, 1.10f, .46f), 19);
            Box(new Vector3(.405f, .65f, 0), new Vector3(.09f, 1.10f, .46f), 19);
            Box(new Vector3(0, .65f, -.20f), new Vector3(.72f, 1.10f, .06f), 19);
            Box(new Vector3(0, 1.20f, 0), new Vector3(.90f, .12f, .46f), 19);
            Box(new Vector3(-.19f, .20f + bundleHeight * .5f, .015f), new Vector3(.25f, bundleHeight, .30f), 15);
            Box(new Vector3(.19f, .20f + bundleHeight * .5f, .015f), new Vector3(.25f, bundleHeight, .30f), 15);
            Box(new Vector3(-.19f, .56f, .18f), new Vector3(.27f, .09f, .06f), 19);
            Box(new Vector3(.19f, .56f, .18f), new Vector3(.27f, .09f, .06f), 19);
        }

        private static void Bear(int variant)
        {
            // Broad brown quadruped with short ears and a lighter projecting muzzle.
            // Four low paws, rather than a humanoid rig, retain the actual B identity.
            float width = .60f + variant * .025f;
            Box(new Vector3(0, .38f, -.12f), new Vector3(width, .40f, .60f), 8);
            Box(new Vector3(0, .51f, .07f), new Vector3(.74f, .40f, .34f), 8);
            Box(new Vector3(0, .64f, .255f), new Vector3(.40f, .38f, .27f), 8);
            Box(new Vector3(0, .62f, .43f), new Vector3(.28f, .16f, .12f), 11);
            Box(new Vector3(-.14f, .84f, .24f), new Vector3(.12f, .13f, .10f), 11);
            Box(new Vector3(.14f, .84f, .24f), new Vector3(.12f, .13f, .10f), 11);
            Box(new Vector3(-.25f, .14f, -.22f), new Vector3(.19f, .28f, .18f), 8);
            Box(new Vector3(.25f, .14f, -.22f), new Vector3(.19f, .28f, .18f), 8);
            Box(new Vector3(-.25f, .14f, .22f), new Vector3(.19f, .28f, .18f), 8);
            Box(new Vector3(.25f, .14f, .22f), new Vector3(.19f, .28f, .18f), 8);
        }

        private static void Slime(int variant)
        {
            // A low living pseudopod cluster, not another static pool surface.
            float shift = (variant - 1.5f) * .045f;
            Box(new Vector3(0, .08f, 0), new Vector3(.80f, .16f, .66f), 16);
            Box(new Vector3(shift, .23f, 0), new Vector3(.56f, .30f, .48f), 16);
            Box(new Vector3(shift, .42f, .035f), new Vector3(.28f, .16f, .28f), 60);
            Box(new Vector3(-.29f, .10f, .24f), new Vector3(.28f, .16f, .28f), 16);
        }

        private static void Spring(int variant)
        {
            // Actual ConvalescencePool owns the exposed surface and liquid state.
            float thickness = .014f + variant * .003f;
            Box(new Vector3(0, .048f - thickness * .5f, 0), new Vector3(1, thickness, 1), 123);
        }

        private static void Boots(int variant)
        {
            // Two leather shafts with iron toes/collars, separated at the center.
            float height = .30f + variant * .025f;
            Box(new Vector3(-.17f, .14f + height * .5f, -.065f), new Vector3(.20f, height, .20f), 10);
            Box(new Vector3(.17f, .14f + height * .5f, -.065f), new Vector3(.20f, height, .20f), 10);
            Box(new Vector3(-.17f, .08f, .04f), new Vector3(.22f, .16f, .42f), 13);
            Box(new Vector3(.17f, .08f, .04f), new Vector3(.22f, .16f, .42f), 13);
            Box(new Vector3(-.17f, .155f + height, -.065f), new Vector3(.22f, .06f, .22f), 13);
            Box(new Vector3(.17f, .155f + height, -.065f), new Vector3(.22f, .06f, .22f), 13);
        }

        private static void Wall(int variant)
        {
            // Stillleaf's broad wind-cut masses need joined slabs, not the shared
            // Stump wall's individual cap. Native TepuiWall remains the sole owner.
            float lower = .65f + variant * .05f;
            Box(new Vector3(0, lower * .5f, 0), new Vector3(1, lower, 1), 56);
            float upper = 2.0f - lower;
            Box(new Vector3(0, lower + upper * .5f, 0), new Vector3(1, upper, 1), 79);
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
