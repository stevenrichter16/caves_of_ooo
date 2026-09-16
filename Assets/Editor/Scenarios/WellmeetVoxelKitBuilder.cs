using System;
using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Reproducible offline Wellmeet art. Repeated cubes become one combined
    /// mesh asset per variant; source and generated scene objects remain separate.</summary>
    public static class WellmeetVoxelKitBuilder
    {
        private const string Folder = "Assets/Resources/WellmeetVoxel3D";
        private static readonly string[] Families = { "ground", "floor", "tent", "cloth", "well", "host", "salt", "bed", "chair", "oven", "shrine", "lantern", "rack", "adult", "child", "corner", "path", "shelf", "marker" };
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
                var entries = new List<WellmeetVoxelLibrary.Entry>(76);
                foreach (string family in Families)
                {
                    for (int variant = 0; variant < WellmeetVoxelLibrary.VariantCount; variant++)
                    {
                        string id = WellmeetVoxelLibrary.ModelId(family, variant);
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
                        entries.Add(new WellmeetVoxelLibrary.Entry
                        {
                            Id = id, Prefab = prefab, Mesh = mesh,
                            Spec = new SpawnRing3DCatalog.Model
                            {
                                id = id, path = prefabPath, kind = family == "ground" || family == "floor" || family == "path" ? "ground" : "entity",
                                materialFamily = "ring-palette", rigFamily = "none",
                                boundsCenter = mesh.bounds.center, boundsSize = mesh.bounds.size,
                                triangles = Triangles.Count / 3, clips = Array.Empty<string>(), sockets = Array.Empty<string>()
                            }
                        });
                    }
                }
                string libraryPath = Folder + "/Library.asset";
                var library = AssetDatabase.LoadAssetAtPath<WellmeetVoxelLibrary>(libraryPath);
                if (library == null)
                {
                    library = ScriptableObject.CreateInstance<WellmeetVoxelLibrary>();
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
                case "tent": Tent(variant); break;
                case "cloth": Cloth(variant); break;
                case "well": Well(variant); break;
                case "host": Host(variant); break;
                case "salt": Salt(variant); break;
                case "bed": Bed(variant); break;
                case "chair": Chair(variant); break;
                case "oven": Oven(variant); break;
                case "shrine": Shrine(variant); break;
                case "lantern": Lantern(variant); break;
                case "rack": Rack(variant); break;
                case "adult": Adult(variant); break;
                case "child": Child(variant); break;
                case "corner": Corner(variant); break;
                case "path": Path(variant); break;
                case "shelf": Shelf(variant); break;
                case "marker": Marker(variant); break;
                default: throw new ArgumentException("Unknown Wellmeet art family.", nameof(family));
            }
        }

        private static void Ground(int variant)
        {
            float thickness = .065f + variant * .008f;
            Box(new Vector3(0, -thickness * .5f, 0), new Vector3(1, thickness, 1), 32);
        }

        private static void Floor(int variant)
        {
            float thickness = .065f + variant * .008f;
            Box(new Vector3(0, -thickness * .5f, 0), new Vector3(1, thickness, 1), 12);
        }

        private static void Tent(int variant)
        {
            // Cutaway cloth plane spans one native wall cell. Native topology
            // determines quarter-turns; this mesh does not supply a hidden roof.
            Box(new Vector3(0, .49f, 0), new Vector3(1, .98f, .16f), 7);
            Box(new Vector3(0, 1.02f, 0), new Vector3(1, .08f, .14f), 10);
            float post = -.43f + variant * .03f;
            Box(new Vector3(post, .55f, 0), new Vector3(.10f, 1.10f, .24f), 10);
        }

        private static void Cloth(int variant)
        {
            // Three-day hospitality belongs to the host's native oath, not this
            // examinable cloth. No additional insignia, god figure or safe aura.
            Box(new Vector3(-.30f, .96f, 0), new Vector3(.10f, 1.92f, .10f), 19);
            Box(new Vector3(.04f, 1.82f, 0), new Vector3(.78f, .08f, .10f), 19);
            float height = .50f + variant * .025f;
            Box(new Vector3(.04f, 1.75f - height * .5f, .01f), new Vector3(.70f, height, .08f), 10);
        }

        private static void Well(int variant)
        {
            // Variants are native repair stages: fouled, temporary, stable,
            // maintained. The normal camp well uses stable (2). This shallow
            // inset is contained well water, not a new walkable liquid cell.
            float height = .54f + variant * .025f;
            int water = variant == 0 ? 48 : variant == 3 ? 28 : 25;
            Box(new Vector3(-.40f, height * .5f, 0), new Vector3(.20f, height, .60f), 64);
            Box(new Vector3(.40f, height * .5f, 0), new Vector3(.20f, height, .60f), 64);
            Box(new Vector3(0, height * .5f, -.40f), new Vector3(1, height, .20f), 64);
            Box(new Vector3(-.20f, height * .5f, .40f), new Vector3(.60f, height, .20f), 64);
            Box(new Vector3(.45f, height * .5f, .40f), new Vector3(.10f, height, .20f), 64);
            float repaired = variant == 0 ? .24f : height;
            Box(new Vector3(.25f, repaired * .5f, .40f), new Vector3(.30f, repaired, .20f), 64);
            Box(new Vector3(0, .10f, 0), new Vector3(.60f, .08f, .60f), water);
            if (variant == 3)
                Box(new Vector3(0, .12f, -.40f), new Vector3(.86f, .24f, .20f), 64);
        }

        private static void Host(int variant)
        {
            float shift = (variant - 1.5f) * .012f;
            Box(new Vector3(-.12f, .25f, 0), new Vector3(.17f, .50f, .23f), 8);
            Box(new Vector3(.12f, .25f, 0), new Vector3(.17f, .50f, .23f), 8);
            Box(new Vector3(0, .82f, 0), new Vector3(.47f, .68f, .32f), 8);
            Box(new Vector3(shift, 1.34f, .04f), new Vector3(.29f, .31f, .27f), 32);
            Box(new Vector3(shift, 1.51f, 0), new Vector3(.36f, .09f, .34f), 8);
            Box(new Vector3(-.29f, .81f, .08f), new Vector3(.14f, .39f, .18f), 8);
            Box(new Vector3(.30f, .81f, .20f), new Vector3(.15f, .13f, .40f), 8);
            Box(new Vector3(.30f, .81f, .39f), new Vector3(.15f, .13f, .09f), 32);
            Box(new Vector3(shift - .07f, 1.37f, .184f), new Vector3(.04f, .04f, .025f), 8);
            Box(new Vector3(shift + .07f, 1.37f, .184f), new Vector3(.04f, .04f, .025f), 8);
        }

        private static void Salt(int variant)
        {
            // Pale dust reaches the elbows; the native mineral exchange still
            // requires the actual actor and inventory, with no decorative altar.
            float shift = (variant - 1.5f) * .012f;
            Box(new Vector3(-.12f, .25f, 0), new Vector3(.17f, .50f, .23f), 64);
            Box(new Vector3(.12f, .25f, 0), new Vector3(.17f, .50f, .23f), 64);
            Box(new Vector3(0, .82f, 0), new Vector3(.47f, .68f, .32f), 64);
            Box(new Vector3(shift, 1.34f, .04f), new Vector3(.29f, .31f, .27f), 19);
            Box(new Vector3(-.29f, .79f, .10f), new Vector3(.15f, .38f, .20f), 19);
            Box(new Vector3(.29f, .79f, .10f), new Vector3(.15f, .38f, .20f), 19);
            Box(new Vector3(shift, 1.48f, 0), new Vector3(.33f, .10f, .31f), 64);
            Box(new Vector3(shift - .07f, 1.37f, .184f), new Vector3(.04f, .04f, .025f), 64);
            Box(new Vector3(shift + .07f, 1.37f, .184f), new Vector3(.04f, .04f, .025f), 64);
        }

        private static void Bed(int variant)
        {
            Box(new Vector3(0, .15f, 0), new Vector3(.74f, .30f, .96f), 8);
            Box(new Vector3(0, .35f, 0), new Vector3(.69f, .10f, .90f), 19);
            float width = .47f + variant * .035f;
            Box(new Vector3(0, .44f, -.30f), new Vector3(width, .08f, .24f), 19);
            Box(new Vector3(0, .23f, -.45f), new Vector3(.76f, .46f, .10f), 8);
        }

        private static void Chair(int variant)
        {
            float width = .48f + variant * .025f;
            Box(new Vector3(0, .39f, 0), new Vector3(width, .12f, .54f), 9);
            Box(new Vector3(-.20f, .17f, -.17f), new Vector3(.10f, .34f, .10f), 8);
            Box(new Vector3(.20f, .17f, -.17f), new Vector3(.10f, .34f, .10f), 8);
            Box(new Vector3(-.20f, .17f, .17f), new Vector3(.10f, .34f, .10f), 8);
            Box(new Vector3(.20f, .17f, .17f), new Vector3(.10f, .34f, .10f), 8);
            Box(new Vector3(0, .70f, -.23f), new Vector3(width, .54f, .10f), 9);
        }

        private static void Oven(int variant)
        {
            float height = .84f + variant * .025f;
            int core = variant == 0 ? 12 : variant == 1 ? 8 : variant == 2 ? 20 : 49;
            Box(new Vector3(-.35f, height * .5f, 0), new Vector3(.26f, height, .80f), 64);
            Box(new Vector3(.35f, height * .5f, 0), new Vector3(.26f, height, .80f), 64);
            Box(new Vector3(0, height * .5f, -.34f), new Vector3(.44f, height, .12f), 64);
            Box(new Vector3(0, height - .12f, 0), new Vector3(.44f, .24f, .80f), 64);
            Box(new Vector3(0, .04f, 0), new Vector3(.44f, .08f, .80f), 64);
            Box(new Vector3(0, .23f, -.23f), new Vector3(.40f, .20f, .08f), core);
            float chimney = .26f + variant * .025f;
            Box(new Vector3(-.28f, height + chimney * .5f, -.20f), new Vector3(.24f, chimney, .24f), 64);
        }

        private static void Shrine(int variant)
        {
            // Existing functional Sanctuary is preserved; First Tent's separate
            // monument does not authorize deleting this village service.
            Box(new Vector3(0, .07f, 0), new Vector3(.88f, .14f, .70f), 64);
            Box(new Vector3(0, .22f, -.05f), new Vector3(.60f, .16f, .40f), 64);
            float width = .25f + variant * .025f;
            Box(new Vector3(0, .43f, -.07f), new Vector3(width, .28f, .22f), 19);
        }

        private static void Lantern(int variant)
        {
            // Core stage and silhouette track the same repaired native owner.
            // LightSource and flicker remain native, with no duplicate Unity light.
            int core = variant == 0 ? 13 : variant == 1 ? 49 : variant == 2 ? 26 : 19;
            Box(new Vector3(0, .77f, 0), new Vector3(.14f, 1.54f, .14f), 12);
            Box(new Vector3(0, .06f, 0), new Vector3(.40f, .12f, .40f), 12);
            float width = .31f + variant * .025f;
            Box(new Vector3(0, 1.52f, 0), new Vector3(width, .43f, width), core);
            Box(new Vector3(0, 1.29f, 0), new Vector3(width + .06f, .08f, width + .06f), 12);
            Box(new Vector3(0, 1.79f, 0), new Vector3(width + .10f, .12f, width + .10f), 12);
        }

        private static void Rack(int variant)
        {
            Box(new Vector3(-.39f, .53f, -.10f), new Vector3(.13f, 1.06f, .20f), 8);
            Box(new Vector3(.39f, .53f, -.10f), new Vector3(.13f, 1.06f, .20f), 8);
            Box(new Vector3(0, .76f, -.10f), new Vector3(.90f, .14f, .18f), 8);
            Box(new Vector3(0, .13f, 0), new Vector3(.96f, .14f, .50f), 8);
            float height = 1.02f + variant * .04f;
            Box(new Vector3(-.20f, height * .5f + .12f, .08f), new Vector3(.10f, height, .12f), 13);
            Box(new Vector3(.20f, .64f, .08f), new Vector3(.12f, 1.02f, .12f), 13);
            Box(new Vector3(.20f, .47f, .08f), new Vector3(.31f, .07f, .14f), 13);
        }

        private static void Adult(int variant)
        {
            // Stable role contract: 0 goods, 1 ledger, 2 tool, 3 plain traveler.
            // No random coordinate selection may change a service person's role.
            Box(new Vector3(-.12f, .25f, 0), new Vector3(.17f, .50f, .23f), 48);
            Box(new Vector3(.12f, .25f, 0), new Vector3(.17f, .50f, .23f), 48);
            Box(new Vector3(0, .81f, 0), new Vector3(.47f, .66f, .32f), 48);
            Box(new Vector3(0, 1.33f, .04f), new Vector3(.29f, .31f, .27f), 32);
            Box(new Vector3(-.29f, .77f, .04f), new Vector3(.13f, .42f, .17f), 48);
            Box(new Vector3(.29f, .77f, .04f), new Vector3(.13f, .42f, .17f), 48);
            Box(new Vector3(-.07f, 1.36f, .184f), new Vector3(.04f, .04f, .025f), 48);
            Box(new Vector3(.07f, 1.36f, .184f), new Vector3(.04f, .04f, .025f), 48);
            switch (variant)
            {
                case 0: Box(new Vector3(0, .58f, .29f), new Vector3(.44f, .34f, .28f), 32); break;
                case 1: Box(new Vector3(0, .89f, .25f), new Vector3(.44f, .09f, .30f), 32); break;
                case 2:
                    Box(new Vector3(.37f, .94f, .05f), new Vector3(.07f, .67f, .09f), 32);
                    Box(new Vector3(.37f, 1.28f, .05f), new Vector3(.22f, .13f, .19f), 32);
                    break;
                case 3: break;
            }
        }

        private static void Child(int variant)
        {
            float shift = (variant - 1.5f) * .015f;
            Box(new Vector3(-.10f, .16f, 0), new Vector3(.14f, .32f, .18f), 21);
            Box(new Vector3(.10f, .16f, 0), new Vector3(.14f, .32f, .18f), 21);
            Box(new Vector3(0, .49f, 0), new Vector3(.34f, .40f, .25f), 21);
            Box(new Vector3(shift, .81f, .03f), new Vector3(.27f, .28f, .25f), 32);
            Box(new Vector3(-.22f, .47f, 0), new Vector3(.11f, .28f, .13f), 21);
            Box(new Vector3(.22f, .47f, 0), new Vector3(.11f, .28f, .13f), 21);
            Box(new Vector3(shift - .06f, .84f, .165f), new Vector3(.035f, .035f, .025f), 21);
            Box(new Vector3(shift + .06f, .84f, .165f), new Vector3(.035f, .035f, .025f), 21);
        }

        private static void Corner(int variant)
        {
            // A real L joins local +X and +Z neighbors. The opposite quadrant
            // stays open; actual native adjacency supplies the quarter-turn.
            Box(new Vector3(.25f, .49f, 0), new Vector3(.50f, .98f, .16f), 7);
            Box(new Vector3(0, .49f, .25f), new Vector3(.16f, .98f, .50f), 7);
            Box(new Vector3(.25f, 1.02f, 0), new Vector3(.50f, .08f, .14f), 10);
            Box(new Vector3(0, 1.02f, .25f), new Vector3(.14f, .08f, .50f), 10);
            float width = .10f + variant * .012f;
            Box(new Vector3(0, .55f, 0), new Vector3(width, 1.10f, width), 10);
        }

        private static void Path(int variant)
        {
            // RoadStone is native packed earth over old stone, distinct from
            // interior StoneFloor. Constant top/color avoids speckled paving.
            float thickness = .065f + variant * .008f;
            Box(new Vector3(0, -thickness * .5f, 0), new Vector3(1, thickness, 1), 7);
        }

        private static void Shelf(int variant)
        {
            // Native AlchemyShelf is a wooden container. Broad bottles identify
            // its contents without assigning them additional gameplay owners.
            Box(new Vector3(-.41f, .61f, -.04f), new Vector3(.14f, 1.22f, .54f), 8);
            Box(new Vector3(.41f, .61f, -.04f), new Vector3(.14f, 1.22f, .54f), 8);
            Box(new Vector3(0, .12f, -.04f), new Vector3(.82f, .12f, .54f), 8);
            Box(new Vector3(0, .59f, -.04f), new Vector3(.82f, .10f, .54f), 8);
            Box(new Vector3(0, 1.16f, -.04f), new Vector3(.82f, .12f, .54f), 8);
            float width = .18f + variant * .015f;
            Box(new Vector3(-.20f, .37f, .015f), new Vector3(width, .34f, .24f), 22);
            Box(new Vector3(-.20f, .55f, .015f), new Vector3(.09f, .06f, .13f), 22);
            Box(new Vector3(.20f, .82f, .015f), new Vector3(width, .32f, .24f), 22);
            Box(new Vector3(.20f, 1.01f, .015f), new Vector3(.09f, .08f, .13f), 22);
        }

        private static void Marker(int variant)
        {
            // Existing per-cell damp/soot/lamplit markers are shallow worn pads,
            // never piled ash stars or miniature copies of the service fixture.
            float width = .60f + variant * .035f;
            Box(new Vector3(0, .011f, 0), new Vector3(width, .022f, .50f), 64);
            Box(new Vector3(.22f, .012f, .22f), new Vector3(.36f, .024f, .30f), 64);
            float shift = -.14f + variant * .02f;
            Box(new Vector3(shift, .026f, -.10f), new Vector3(.28f, .012f, .20f), 12);
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
