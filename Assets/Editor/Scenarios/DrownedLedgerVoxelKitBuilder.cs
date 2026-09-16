using System;
using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Reproducible offline DrownedLedger art. Repeated cubes become one combined
    /// mesh asset per variant; source and generated scene objects remain separate.</summary>
    public static class DrownedLedgerVoxelKitBuilder
    {
        private const string Folder = "Assets/Resources/DrownedLedgerVoxel3D";
        private static readonly string[] Families = { "preserved", "stake", "table", "scribe", "sorter", "parcel", "boards" };
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
                var entries = new List<DrownedLedgerVoxelKitLibrary.Entry>(28);
                foreach (string family in Families)
                {
                    for (int variant = 0; variant < DrownedLedgerVoxelKitLibrary.VariantCount; variant++)
                    {
                        string id = DrownedLedgerVoxelKitLibrary.ModelId(family, variant);
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
                        entries.Add(new DrownedLedgerVoxelKitLibrary.Entry
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
                var library = AssetDatabase.LoadAssetAtPath<DrownedLedgerVoxelKitLibrary>(libraryPath);
                if (library == null)
                {
                    library = ScriptableObject.CreateInstance<DrownedLedgerVoxelKitLibrary>();
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
                case "preserved": Preserved(variant); break;
                case "stake": Stake(variant); break;
                case "table": Table(variant); break;
                case "scribe": Scribe(variant); break;
                case "sorter": Sorter(variant); break;
                case "parcel": Parcel(variant); break;
                case "boards": Boards(variant); break;
                default: throw new ArgumentException("Unknown DrownedLedger art family.", nameof(family));
            }
        }

        private static void Preserved(int variant)
        {
            // One complete native witness, never a body attached to another
            // owner's reading table. Clothes, intact head and separated feet
            // distinguish this person from an ordinary wrapped courier parcel.
            float width = .32f + variant * .012f;
            Box(new Vector3(0, .12f, -.015f), new Vector3(width, .20f, .42f), 13);
            Box(new Vector3(0, .145f, -.34f), new Vector3(.25f, .21f, .22f), 79);
            Box(new Vector3(0, .12f, -.235f), new Vector3(.14f, .13f, .08f), 79);
            Box(new Vector3(-.20f, .105f, -.02f), new Vector3(.11f, .16f, .40f), 13);
            Box(new Vector3(.20f, .105f, -.02f), new Vector3(.11f, .16f, .40f), 13);
            Box(new Vector3(-.105f, .065f, .325f), new Vector3(.14f, .13f, .29f), 13);
            Box(new Vector3(.105f, .065f, .325f), new Vector3(.14f, .13f, .29f), 13);
            Box(new Vector3(.15f, .235f, .12f), new Vector3(.09f, .025f, .10f), 79);
        }

        private static void Stake(int variant)
        {
            // Numbering remains in the examination text; the gray-painted
            // head is a readable field marker without fabricated lettering.
            float height = 1.04f + variant * .06f;
            Box(new Vector3(0, height * .5f, 0), new Vector3(.13f, height, .13f), 8);
            Box(new Vector3(0, height - .15f, .006f), new Vector3(.24f, .28f, .16f), 13);
            Box(new Vector3(0, .035f, 0), new Vector3(.19f, .07f, .18f), 8);
        }

        private static void Table(int variant)
        {
            // Bare central working surface. Padding, straps and the folio
            // stand sit at the sides; the separately owned witness is adjacent.
            Box(new Vector3(-.32f, .34f, -.27f), new Vector3(.11f, .68f, .11f), 8);
            Box(new Vector3(.32f, .34f, -.27f), new Vector3(.11f, .68f, .11f), 8);
            Box(new Vector3(-.32f, .34f, .27f), new Vector3(.11f, .68f, .11f), 8);
            Box(new Vector3(.32f, .34f, .27f), new Vector3(.11f, .68f, .11f), 8);
            Box(new Vector3(0, .725f, 0), new Vector3(.88f, .11f, .84f), 19);
            Box(new Vector3(-.29f, .82f, -.10f), new Vector3(.13f, .08f, .24f), 19);
            Box(new Vector3(.29f, .82f, .10f), new Vector3(.13f, .08f, .24f), 19);
            Box(new Vector3(-.29f, .87f, -.10f), new Vector3(.15f, .025f, .07f), 8);
            Box(new Vector3(.29f, .87f, .10f), new Vector3(.15f, .025f, .07f), 8);
            float height = .19f + variant * .025f;
            Box(new Vector3(.245f, .78f + height * .5f, -.30f), new Vector3(.27f, height, .10f), 8);
        }

        private static void Scribe(int variant)
        {
            // Teal field clothing and an open held folio. The native actor
            // keeps its conversation and loadout; no table moves with it.
            float width = .41f + variant * .014f;
            Box(new Vector3(-.12f, .245f, 0), new Vector3(.17f, .49f, .23f), 22);
            Box(new Vector3(.12f, .245f, 0), new Vector3(.17f, .49f, .23f), 22);
            Box(new Vector3(0, .77f, 0), new Vector3(width, .63f, .32f), 22);
            Box(new Vector3(0, 1.29f, .04f), new Vector3(.29f, .34f, .27f), 19);
            Box(new Vector3(-.285f, .87f, .10f), new Vector3(.13f, .34f, .22f), 22);
            Box(new Vector3(.285f, .96f, .13f), new Vector3(.13f, .16f, .28f), 22);
            Box(new Vector3(.045f, .94f, .265f), new Vector3(.47f, .17f, .075f), 19);
            Box(new Vector3(-.07f, 1.32f, .188f), new Vector3(.04f, .04f, .024f), 22);
            Box(new Vector3(.07f, 1.32f, .188f), new Vector3(.04f, .04f, .024f), 22);
        }

        private static void Sorter(int variant)
        {
            // A pale gloved coat and tucked dark ledger identify Curation's
            // field worker separately from the Recension's teal reader.
            float width = .43f + variant * .013f;
            Box(new Vector3(-.12f, .24f, 0), new Vector3(.17f, .48f, .23f), 12);
            Box(new Vector3(.12f, .24f, 0), new Vector3(.17f, .48f, .23f), 12);
            Box(new Vector3(0, .80f, 0), new Vector3(width, .72f, .34f), 35);
            Box(new Vector3(0, 1.32f, .04f), new Vector3(.30f, .30f, .28f), 35);
            Box(new Vector3(-.29f, .90f, .11f), new Vector3(.13f, .30f, .23f), 35);
            Box(new Vector3(.29f, .83f, .15f), new Vector3(.13f, .18f, .30f), 35);
            Box(new Vector3(.10f, .91f, .255f), new Vector3(.34f, .30f, .075f), 12);
            Box(new Vector3(-.075f, 1.35f, .195f), new Vector3(.04f, .04f, .025f), 12);
            Box(new Vector3(.075f, 1.35f, .195f), new Vector3(.04f, .04f, .025f), 12);
        }

        private static void Parcel(int variant)
        {
            // The real weight-30 NoTrade quest item is fully wrapped, waxed
            // and corded. It contains no exposed face or fourth ancient witness.
            float width = .43f + variant * .014f;
            Box(new Vector3(0, .135f, 0), new Vector3(width, .27f, .66f), 12);
            Box(new Vector3(0, .14f, -.36f), new Vector3(.34f, .25f, .20f), 12);
            Box(new Vector3(0, .11f, .355f), new Vector3(.38f, .21f, .21f), 12);
            Box(new Vector3(0, .15f, -.18f), new Vector3(width + .025f, .30f, .065f), 19);
            Box(new Vector3(0, .15f, .18f), new Vector3(width + .025f, .30f, .065f), 19);
            Box(new Vector3(0, .29f, 0), new Vector3(.065f, .04f, .67f), 19);
            Box(new Vector3(.07f, .315f, -.04f), new Vector3(.13f, .035f, .13f), 19);
        }

        private static void Boards(int variant)
        {
            // Local excavation access is native dry Duckboard. Broad weathered
            // planks cross the sleepers without the inherited golden-road hue.
            Box(new Vector3(0, .045f, -.32f), new Vector3(.96f, .09f, .12f), 106);
            Box(new Vector3(0, .045f, .32f), new Vector3(.96f, .09f, .12f), 106);
            float length = .98f - variant * .012f;
            Box(new Vector3(-.31f, .13f, 0), new Vector3(.28f, .08f, length), 64);
            Box(new Vector3(0, .13f, 0), new Vector3(.28f, .08f, length), 64);
            Box(new Vector3(.31f, .13f, 0), new Vector3(.28f, .08f, length), 64);
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
