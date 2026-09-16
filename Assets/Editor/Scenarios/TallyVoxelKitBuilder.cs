using System;
using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Reproducible offline Tally art. Repeated cubes become one combined
    /// mesh asset per variant; source and generated scene objects remain separate.</summary>
    public static class TallyVoxelKitBuilder
    {
        private const string Folder = "Assets/Resources/TallyVoxel3D";
        private static readonly string[] Families = { "ground", "path", "wall", "crate" };
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
                var entries = new List<TallyVoxelKitLibrary.Entry>(16);
                foreach (string family in Families)
                {
                    for (int variant = 0; variant < TallyVoxelKitLibrary.VariantCount; variant++)
                    {
                        string id = TallyVoxelKitLibrary.ModelId(family, variant);
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
                        entries.Add(new TallyVoxelKitLibrary.Entry
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
                var library = AssetDatabase.LoadAssetAtPath<TallyVoxelKitLibrary>(libraryPath);
                if (library == null)
                {
                    library = ScriptableObject.CreateInstance<TallyVoxelKitLibrary>();
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
                case "ground":
                    float depth=.065f+variant*.008f;
                    Box(new Vector3(0,-depth*.5f,0),new Vector3(1,depth,1),64);break;
                case "path":
                    float path=.075f+variant*.008f;
                    Box(new Vector3(0,-path*.5f,0),new Vector3(1,path,1),106);break;
                case "wall":
                    // One quiet cloth mass and a recessed low support. The
                    // material break is horizontal, with no per-cell bright
                    // posts/slats competing with actors from gameplay height.
                    float h=.72f+variant*.025f;
                    Box(new Vector3(0,.065f,0),new Vector3(1,.13f,1),12);
                    Box(new Vector3(0,.13f+h*.5f,0),new Vector3(1,h,1),107);break;
                case "crate":
                    // Broad weathered timber faces and a single full lid;
                    // no narrow pale straps that read as a barcode at scale.
                    float height=.55f+variant*.04f;
                    Box(new Vector3(0,height*.5f,0),new Vector3(.78f,height,.76f),8);
                    Box(new Vector3(0,height+.04f,0),new Vector3(.80f,.08f,.78f),9);break;
                default:throw new ArgumentException("Unknown Tally art family.",nameof(family));
            }
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
