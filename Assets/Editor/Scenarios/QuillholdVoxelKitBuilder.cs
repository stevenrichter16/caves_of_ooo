using System;
using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Reproducible offline Quillhold art. Repeated cubes become one combined
    /// mesh asset per variant; source and generated scene objects remain separate.</summary>
    public static class QuillholdVoxelKitBuilder
    {
        private const string Folder = "Assets/Resources/QuillholdVoxel3D";
        private static readonly string[] Families = { "ground", "path", "wall", "shelf", "desk", "scribe", "table" };
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
                var entries = new List<QuillholdVoxelKitLibrary.Entry>(28);
                foreach (string family in Families)
                {
                    for (int variant = 0; variant < QuillholdVoxelKitLibrary.VariantCount; variant++)
                    {
                        string id = QuillholdVoxelKitLibrary.ModelId(family, variant);
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
                        entries.Add(new QuillholdVoxelKitLibrary.Entry
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
                var library = AssetDatabase.LoadAssetAtPath<QuillholdVoxelKitLibrary>(libraryPath);
                if (library == null)
                {
                    library = ScriptableObject.CreateInstance<QuillholdVoxelKitLibrary>();
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

        private static void BuildFamily(string family,int variant)
        {
            switch(family)
            {
                case "ground":Ground(variant,106);break;
                case "path":Ground(variant,64);break;
                case "wall":Wall(variant);break;
                case "shelf":Shelf(variant);break;
                case "desk":Desk(variant);break;
                case "scribe":Scribe(variant);break;
                case "table":RefectoryTable(variant);break;
                default:throw new ArgumentException("Unknown Quillhold family.",nameof(family));
            }
        }
        private static void Ground(int variant,int palette)
        {
            // Identical visible stone ground: buried slab depth supplies the
            // four technical variants without checkerboard material noise.
            float thickness=.06f+variant*.008f;
            Box(new Vector3(0,-thickness*.5f,0),new Vector3(1,thickness,1),palette);
        }
        private static void Wall(int variant)
        {
            // Full-cell native masonry, cut low enough to see working rooms.
            // One subdued cap; no seams masquerading as writing/hidden doors.
            Box(new Vector3(0,.44f,0),new Vector3(1,.88f,1),56);
            Box(new Vector3(0,.94f,0),new Vector3(1,.12f,1),13);
            float joint=-.27f+variant*.17f;
            Box(new Vector3(joint,.53f,.486f),new Vector3(.035f,.58f,.024f),13);
        }
        private static void Shelf(int variant)
        {
            // Three strong shelf levels and three tied manuscript bundles.
            // This owner holds books; there is no decorative second person.
            Box(new Vector3(-.42f,.41f,0),new Vector3(.08f,.82f,.40f),8);
            Box(new Vector3(.42f,.41f,0),new Vector3(.08f,.82f,.40f),8);
            foreach(float y in new[]{.06f,.39f,.74f})Box(new Vector3(0,y,0),new Vector3(.88f,.08f,.44f),8);
            float offset=(variant-1.5f)*.035f;
            Box(new Vector3(-.18f+offset,.23f,.025f),new Vector3(.34f,.22f,.32f),19);
            Box(new Vector3(.22f,.22f,0),new Vector3(.22f,.20f,.30f),19);
            Box(new Vector3(offset,.56f,.015f),new Vector3(.57f,.23f,.31f),19);
        }
        private static void Desk(int variant)
        {
            Box(new Vector3(-.30f,.22f,0),new Vector3(.13f,.44f,.52f),8);
            Box(new Vector3(.30f,.22f,0),new Vector3(.13f,.44f,.52f),8);
            Box(new Vector3(0,.46f,0),new Vector3(.86f,.10f,.64f),8);
            float offset=(variant-1.5f)*.04f;
            Box(new Vector3(offset,.525f,.02f),new Vector3(.43f,.035f,.39f),19);
        }
        private static void RefectoryTable(int variant)
        {
            // A broad communal eating surface, without copying folios. Two
            // adjacent native table owners compose the longer refectory table.
            float inset=.31f+variant*.012f;
            foreach(float x in new[]{-inset,inset})foreach(float z in new[]{-.31f,.31f})
                Box(new Vector3(x,.24f,z),new Vector3(.11f,.48f,.11f),8);
            Box(new Vector3(0,.51f,0),new Vector3(.98f,.10f,.90f),64);
        }
        private static void Scribe(int variant)
        {
            // Ordinary standing scribe with a held folio, not the Reader.
            float shift=(variant-1.5f)*.012f;
            Box(new Vector3(-.12f,.24f,0),new Vector3(.18f,.48f,.24f),22);
            Box(new Vector3(.12f,.24f,0),new Vector3(.18f,.48f,.24f),22);
            Box(new Vector3(0,.81f,0),new Vector3(.46f,.68f,.34f),22);
            Box(new Vector3(shift,1.30f,.02f),new Vector3(.29f,.31f,.28f),32);
            Box(new Vector3(shift,1.49f,0),new Vector3(.35f,.10f,.34f),22);
            Box(new Vector3(-.28f,.80f,.09f),new Vector3(.14f,.38f,.19f),22);
            Box(new Vector3(.27f,.82f,.10f),new Vector3(.14f,.34f,.20f),22);
            Box(new Vector3(0,.86f,.31f),new Vector3(.47f,.33f,.12f),32);
            Box(new Vector3(-.20f,.85f,.34f),new Vector3(.12f,.12f,.15f),32);
            Box(new Vector3(.20f,.85f,.34f),new Vector3(.12f,.12f,.15f),32);
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
