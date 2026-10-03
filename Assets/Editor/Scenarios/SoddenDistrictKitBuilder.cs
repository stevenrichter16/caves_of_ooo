using System;
using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Original Sodden district source. All cuboids become one saved
    /// mesh per physical form; no gameplay objects or scene are saved.</summary>
    public static class SoddenDistrictKitBuilder
    {
        static readonly List<Vector3> vertices = new List<Vector3>(480);
        static readonly List<Vector2> uvs = new List<Vector2>(480);
        static readonly List<Color> colors = new List<Color>(480);
        static readonly List<int> triangles = new List<int>(720);
        static Vector3[] cubeVertices;
        static int[] cubeTriangles;

        [MenuItem("Caves of Ooo/Art/Build Sodden District Kit")]
        public static void Run()
        {
            var material = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath)?.WorldMaterial;
            if (material == null) throw new InvalidOperationException("Native ring palette material is unavailable.");
            Directory.CreateDirectory(SoddenDistrictArtLibrary.Folder); AssetDatabase.Refresh();
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var cube = primitive.GetComponent<MeshFilter>().sharedMesh;
            cubeVertices = cube.vertices; cubeTriangles = cube.triangles;
            try
            {
                var entries = new List<SoddenDistrictArtLibrary.Entry>();
                foreach (string id in SoddenDistrictArtLibrary.ModelIds)
                {
                    vertices.Clear(); uvs.Clear(); colors.Clear(); triangles.Clear(); Build(id);
                    string path = SoddenDistrictArtLibrary.Folder + "/" + id;
                    var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path + ".asset");
                    if (mesh == null) { mesh = new Mesh { name = id }; AssetDatabase.CreateAsset(mesh, path + ".asset"); }
                    else mesh.Clear();
                    mesh.SetVertices(vertices); mesh.SetUVs(0, uvs); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0);
                    mesh.RecalculateNormals(); mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh);
                    var root = new GameObject(id); GameObject prefab;
                    try
                    {
                        root.AddComponent<MeshFilter>().sharedMesh = mesh;
                        root.AddComponent<MeshRenderer>().sharedMaterial = material;
                        prefab = PrefabUtility.SaveAsPrefabAsset(root, path + ".prefab");
                    }
                    finally { UnityEngine.Object.DestroyImmediate(root); }
                    entries.Add(new SoddenDistrictArtLibrary.Entry
                    {
                        Id = id, Prefab = prefab, Mesh = mesh,
                        Spec = new SpawnRing3DCatalog.Model
                        {
                            id = id, sourceBlueprint = SoddenDistrictArtLibrary.Blueprint(id), path = path + ".prefab",
                            kind = "entity", materialFamily = "ring-palette", rigFamily = "none",
                            boundsCenter = mesh.bounds.center, boundsSize = mesh.bounds.size, triangles = triangles.Count / 3,
                            clips = Array.Empty<string>(), sockets = Array.Empty<string>()
                        }
                    });
                }
                string libraryPath = SoddenDistrictArtLibrary.Folder + "/Library.asset";
                var library = AssetDatabase.LoadAssetAtPath<SoddenDistrictArtLibrary>(libraryPath);
                if (library == null)
                {
                    library = ScriptableObject.CreateInstance<SoddenDistrictArtLibrary>();
                    AssetDatabase.CreateAsset(library, libraryPath);
                }
                library.Entries = entries.ToArray(); library.Validate(); EditorUtility.SetDirty(library); AssetDatabase.SaveAssets();
                Debug.Log("[SoddenDistrictArt] Built six original inert meshes and their measured prefab catalogue.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(primitive); cubeVertices = null; cubeTriangles = null;
            }
        }

        static void Build(string id)
        {
            switch (id)
            {
                case SoddenDistrictArtLibrary.BrokenBench: Bench(false); break;
                case SoddenDistrictArtLibrary.WorkingBench: Bench(true); break;
                case SoddenDistrictArtLibrary.Salvage: Salvage(); break;
                case SoddenDistrictArtLibrary.Locker: Locker(); break;
                case SoddenDistrictArtLibrary.Notice: Notice(); break;
                case SoddenDistrictArtLibrary.Dressing: Dressing(); break;
                default: throw new ArgumentException("Unknown original Sodden district form.", nameof(id));
            }
        }

        static void Bench(bool repaired)
        {
            // A raised cloth-working frame, not a merchant stall. The damaged
            // upright and split worktop physically straighten after repair.
            Box(-.36f, .36f, -.25f, .14f, .72f, .14f, 8);
            Box(.36f, repaired ? .36f : .19f, -.25f, .14f, repaired ? .72f : .38f, .14f, 8);
            Box(-.36f, .36f, .25f, .14f, .72f, .14f, 8);
            Box(.36f, repaired ? .36f : .19f, .25f, .14f, repaired ? .72f : .38f, .14f, 8);
            if (repaired)
            {
                Box(0, .76f, 0, .96f, .14f, .76f, 9);
                Box(-.36f, 1.17f, -.28f, .13f, .84f, .13f, 8);
                Box(.36f, 1.17f, -.28f, .13f, .84f, .13f, 8);
                Box(0, 1.60f, -.28f, .94f, .13f, .15f, 9);
                Box(-.09f, 1.32f, -.22f, .44f, .48f, .075f, 19);
                Box(.14f, .88f, .16f, .38f, .10f, .28f, 19);
                Box(.14f, .94f, .16f, .07f, .04f, .29f, 12);
            }
            else
            {
                Box(-.21f, .73f, 0, .45f, .14f, .76f, 9);
                Box(.20f, .53f, 0, .45f, .14f, .76f, 9, 0, 0, -20);
                Box(-.36f, 1.05f, -.28f, .13f, .66f, .13f, 8);
                Box(.36f, .75f, -.28f, .13f, .45f, .13f, 8);
                Box(-.03f, .12f, .26f, .85f, .13f, .15f, 9);
                Box(-.16f, .84f, .08f, .34f, .07f, .36f, 19);
                Box(.17f, .65f, .11f, .30f, .07f, .31f, 19, 0, 0, -20);
                Box(-.16f, .89f, .08f, .06f, .03f, .37f, 12);
            }
        }

        static void Salvage()
        {
            // Finite collapsed frame wood. Broad stacked beams stay low enough
            // to walk over, while one broken upright gives the works a silhouette.
            Box(0, .11f, -.26f, .94f, .18f, .19f, 10);
            Box(-.02f, .13f, .19f, .90f, .18f, .20f, 8);
            Box(-.05f, .32f, -.01f, .18f, .22f, .90f, 9);
            Box(.24f, .27f, -.03f, .17f, .20f, .79f, 8);
            Box(-.33f, .60f, -.26f, .20f, 1.00f, .19f, 8);
            Box(-.26f, 1.08f, -.26f, .31f, .13f, .19f, 11);
            Box(-.12f, .16f, .33f, .65f, .10f, .13f, 11);
            Box(.32f, .47f, .23f, .13f, .46f, .14f, 9);
        }

        static void Locker()
        {
            // Closed exterior deliberately makes no claim about remaining loot.
            Box(0, .12f, 0, .86f, .24f, .72f, 8);
            Box(0, .66f, 0, .82f, .90f, .68f, 9);
            Box(0, 1.14f, 0, .94f, .14f, .79f, 8);
            Box(-.29f, .68f, .355f, .095f, .95f, .045f, 12);
            Box(.29f, .68f, .355f, .095f, .95f, .045f, 12);
            Box(0, .69f, .37f, .12f, .30f, .055f, 12);
            Box(0, .70f, .412f, .17f, .08f, .035f, 13);
            Box(0, .17f, .375f, .90f, .10f, .04f, 12);
        }

        static void Notice()
        {
            // Two real directional boards; their words remain the native examine
            // description. No waypoint glow or fabricated navigable geometry.
            Box(0, .96f, 0, .18f, 1.92f, .18f, 8);
            Box(-.06f, 1.69f, .11f, .86f, .30f, .13f, 9);
            Box(.06f, 1.30f, .11f, .86f, .28f, .13f, 9);
            Box(-.24f, 1.69f, .185f, .30f, .065f, .025f, 19);
            Box(.24f, 1.30f, .185f, .30f, .065f, .025f, 19);
            Box(0, 1.93f, 0, .28f, .10f, .26f, 8);
            Box(0, .055f, 0, .40f, .11f, .40f, 10);
        }

        static void Dressing()
        {
            // A portable cream wrap, tied with green plant fibre; no bottle or
            // generic tonic silhouette, and no loose extra inventory objects.
            Box(0, .13f, 0, .62f, .24f, .42f, 19);
            Box(0, .28f, 0, .48f, .12f, .39f, 19);
            Box(0, .215f, 0, .11f, .43f, .45f, 16);
            Box(.06f, .43f, 0, .23f, .08f, .13f, 14);
            Box(-.24f, .17f, .005f, .045f, .11f, .34f, 14);
        }

        static void Box(float x, float y, float z, float w, float h, float d, int palette, float rx = 0, float ry = 0, float rz = 0)
        {
            int offset = vertices.Count; var center = new Vector3(x, y, z); var size = new Vector3(w, h, d);
            var rotation = Quaternion.Euler(rx, ry, rz);
            var uv = new Vector2((palette % 16 + .5f) / 16f, (palette / 16 + .5f) / 8f);
            foreach (var vertex in cubeVertices)
            { vertices.Add(center + rotation * Vector3.Scale(vertex, size)); uvs.Add(uv); colors.Add(Color.white); }
            foreach (int triangle in cubeTriangles) triangles.Add(offset + triangle);
        }
    }
}
