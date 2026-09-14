using System;
using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Reproducible offline Stump art authoring. Generated meshes are
    /// combined assets, never independent cube objects. No scene is saved.</summary>
    public static class StumpVoxelKitBuilder
    {
        private const string Folder = "Assets/Resources/StumpVoxel3D";
        private static readonly string[] Families = { "ground", "wall", "grain", "dome", "ledge", "spray", "tank", "vein", "bone", "tree", "bush", "singer", "sentinel", "key" };
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
                var entries = new List<StumpVoxelLibrary.Entry>(56);
                foreach (string family in Families)
                {
                    for (int variant = 0; variant < StumpVoxelLibrary.VariantCount; variant++)
                    {
                        string id = StumpVoxelLibrary.ModelId(family, variant);
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
                        entries.Add(new StumpVoxelLibrary.Entry
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
                var library = AssetDatabase.LoadAssetAtPath<StumpVoxelLibrary>(libraryPath);
                if (library == null)
                {
                    library = ScriptableObject.CreateInstance<StumpVoxelLibrary>();
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
                case "grain": Grain(variant); break;
                case "dome": Dome(variant); break;
                case "ledge": Ledge(variant); break;
                case "spray": Spray(variant); break;
                case "tank": Tank(variant); break;
                case "vein": Vein(variant); break;
                case "bone": Bone(variant); break;
                case "tree": Tree(variant); break;
                case "bush": Bush(variant); break;
                case "singer": Singer(variant); break;
                case "sentinel": Sentinel(variant); break;
                case "key": Key(variant); break;
                default: throw new ArgumentException("Unknown Stump art family.", nameof(family));
            }
        }

        private static void Ground(int variant)
        {
            float thickness = .06f + variant * .008f;
            // The buried base varies while all exposed tops remain the same quiet pink-gray.
            Box(new Vector3(0, -thickness * .5f, 0), new Vector3(1, thickness, 1), 76);
        }

        private static void Wall(int variant)
        {
            float h = 1.72f + variant * .14f;
            Box(new Vector3(0, .25f, 0), new Vector3(1, .50f, 1), 56);
            Box(new Vector3(0, h * .5f, .055f), new Vector3(1, h, .85f), 76);
            Box(new Vector3((variant - 1.5f) * .03f, h + .08f, .06f), new Vector3(.78f, .16f, .65f), 76);
        }

        private static void Grain(int variant)
        {
            // Native footprints give the grain its east-west direction. Full-cell
            // strata let neighboring owners compose a broad rib without decorative rails.
            float h = variant == 0 ? .62f : variant == 1 ? .82f : variant == 2 ? 1.08f : 1.32f;
            Box(new Vector3(0, h * .25f, 0), new Vector3(1, h * .5f, 1), 56);
            Box(new Vector3(0, h * .75f, 0), new Vector3(1, h * .5f, 1), 79);
        }

        private static void Dome(int variant)
        {
            // One large knuckle comes from the native mass and current-neighbor
            // height grades, not a grid of individually narrowed miniature domes.
            float h = .72f + variant * .18f;
            Box(new Vector3(0, h * .25f, 0), new Vector3(1, h * .5f, 1), 56);
            Box(new Vector3(0, h * .75f, 0), new Vector3(1, h * .5f, 1), 79);
        }

        private static void Ledge(int variant)
        {
            float lower = .08f + variant * .012f;
            Box(new Vector3(0, lower * .5f, 0), new Vector3(1, lower, .92f), 56);
            Box(new Vector3(0, lower + .04f, .055f), new Vector3(1, .08f, .76f), 76);
        }

        private static void Spray(int variant)
        {
            float depth = .015f + variant * .004f;
            // A native scenery surface, not an invented LiquidPool or wetness source.
            Box(new Vector3(0, .048f - depth * .5f, 0), new Vector3(1, depth, 1), 28);
        }

        private static void Tank(int variant)
        {
            float h = .55f + variant * .06f;
            // Four raised leaves leave a real open cup. The colored surface is descriptive art only.
            Box(new Vector3(-.31f, h * .5f, 0), new Vector3(.18f, h, .48f), 16);
            Box(new Vector3(.31f, h * .5f, 0), new Vector3(.18f, h, .48f), 16);
            Box(new Vector3(0, h * .46f, -.31f), new Vector3(.48f, h * .92f, .18f), 16);
            Box(new Vector3(0, h * .46f, .31f), new Vector3(.48f, h * .92f, .18f), 16);
            Box(new Vector3(0, .24f, 0), new Vector3(.42f, .035f, .42f), 28);
            Box(new Vector3(-.39f, .18f, 0), new Vector3(.20f, .12f, .50f), 16);
            Box(new Vector3(.39f, .18f, 0), new Vector3(.20f, .12f, .50f), 16);
            Box(new Vector3(0, .18f, -.39f), new Vector3(.50f, .12f, .20f), 16);
            Box(new Vector3(0, .18f, .39f), new Vector3(.50f, .12f, .20f), 16);
        }

        private static void Vein(int variant)
        {
            float h = .79f + variant * .10f;
            Box(new Vector3(0, h * .5f, 0), new Vector3(.94f, h, .78f), 76);
            Box(new Vector3(0, h * .47f, .394f), new Vector3(.94f, .11f, .035f), 19);
            Box(new Vector3(0, h + .045f, -.02f), new Vector3(.90f, .09f, .56f), 19);
            Box(new Vector3((variant - 1.5f) * .03f, h + .14f, -.04f), new Vector3(.63f, .10f, .41f), 76);
        }

        private static void Bone(int variant)
        {
            float h = .17f + variant * .018f;
            // Tepuibone is a heavy cut stone, never anatomical bone.
            Box(new Vector3(0, h * .5f, 0), new Vector3(.43f, h, .32f), 19);
            Box(new Vector3(0, h * .57f, .165f), new Vector3(.39f, .045f, .03f), 76);
            Box(new Vector3((variant - 1.5f) * .012f, h + .032f, -.015f), new Vector3(.29f, .064f, .24f), 19);
        }

        private static void Tree(int variant)
        {
            float offset = (variant - 1.5f) * .025f;
            float h = 1.52f + variant * .10f;
            // Overlapping coarse canopy tiers remain attached throughout the dwarf-height range.
            Box(new Vector3(offset, h * .30f, 0), new Vector3(.20f, h * .60f, .20f), 8);
            Box(new Vector3(0, h * .625f, 0), new Vector3(.92f, h * .29f, .78f), 48);
            Box(new Vector3(-.12f, h * .81f, .025f), new Vector3(.65f, h * .20f, .65f), 48);
            Box(new Vector3(offset, h * .92f, .035f), new Vector3(.48f, h * .16f, .50f), 48);
        }

        private static void Bush(int variant)
        {
            float h = .36f + variant * .065f;
            Box(new Vector3(0, h * .5f, 0), new Vector3(.81f, h, .66f), 48);
            Box(new Vector3((variant - 1.5f) * .04f, h + .07f, -.025f), new Vector3(.54f, .14f, .45f), 16);
            Box(new Vector3(-.22f, h * .68f, .13f), new Vector3(.35f, .19f, .35f), 16);
        }

        private static void Singer(int variant)
        {
            // Canon is a small brown frog. Static pose variants add no call/alarm mechanic.
            Box(new Vector3(0, .23f, -.015f), new Vector3(.40f, .27f, .48f), 9);
            float headHeight = .24f + variant * .02f;
            Box(new Vector3(0, .34f, .25f), new Vector3(.34f, headHeight, .27f), 9);
            Box(new Vector3(-.23f, .13f, -.15f), new Vector3(.22f, .22f, .28f), 9);
            Box(new Vector3(.23f, .13f, -.15f), new Vector3(.22f, .22f, .28f), 9);
            Box(new Vector3(0, .06f, .255f), new Vector3(.52f, .10f, .15f), 9);
            // Five touching pixel masses make the dark W over the back.
            for (int pixel = 0; pixel < 5; pixel++)
                Box(new Vector3((pixel - 2) * .07f, .378f, pixel % 2 == 0 ? -.03f : -.13f), new Vector3(.075f, .026f, .11f), 10);
            float eyeY = .34f + headHeight * .5f + .01f;
            Box(new Vector3(-.10f, eyeY, .30f), new Vector3(.055f, .03f, .065f), 10);
            Box(new Vector3(.10f, eyeY, .30f), new Vector3(.055f, .03f, .065f), 10);
        }

        private static void Sentinel(int variant)
        {
            float shift = (variant - 1.5f) * .015f;
            Box(new Vector3(0, .14f, .015f), new Vector3(.27f, .16f, .43f), 31);
            Box(new Vector3(shift, .17f, .29f), new Vector3(.25f, .16f, .22f), 31);
            Box(new Vector3(shift, .10f, -.295f), new Vector3(.13f, .10f, .21f), 31);
            Box(new Vector3(shift * 2, .08f, -.44f), new Vector3(.085f, .065f, .10f), 31);
            Box(new Vector3(-.20f, .07f, .13f), new Vector3(.22f, .07f, .105f), 31);
            Box(new Vector3(.20f, .07f, .13f), new Vector3(.22f, .07f, .105f), 31);
            Box(new Vector3(-.20f, .07f, -.15f), new Vector3(.22f, .07f, .105f), 31);
            Box(new Vector3(.20f, .07f, -.15f), new Vector3(.22f, .07f, .105f), 31);
            Box(new Vector3(shift - .084f, .263f, .33f), new Vector3(.045f, .026f, .055f), 21);
            Box(new Vector3(shift + .084f, .263f, .33f), new Vector3(.045f, .026f, .055f), 21);
        }

        private static void Key(int variant)
        {
            // Seven coarse iron pieces make a real open bow, long shaft and two
            // separate teeth. The native portable entity owns pickup and movement.
            float thickness = .08f + variant * .01f;
            float y = thickness * .5f;
            Box(new Vector3(-.27f, y, -.14f), new Vector3(.36f, thickness, .10f), 13);
            Box(new Vector3(-.27f, y, .14f), new Vector3(.36f, thickness, .10f), 13);
            Box(new Vector3(-.40f, y, 0), new Vector3(.10f, thickness, .22f), 13);
            Box(new Vector3(-.14f, y, 0), new Vector3(.10f, thickness, .22f), 13);
            Box(new Vector3(.145f, y, 0), new Vector3(.57f, thickness, .095f), 13);
            float toothDepth = .15f + variant * .02f;
            Box(new Vector3(.22f, y, .115f), new Vector3(.105f, thickness, toothDepth), 12);
            Box(new Vector3(.40f, y, .115f), new Vector3(.105f, thickness, toothDepth), 12);
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
