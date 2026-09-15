using System;
using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Reproducible offline Ginmere art. Repeated cubes become one combined
    /// mesh asset per variant; source and generated scene objects remain separate.</summary>
    public static class GinmereVoxelKitBuilder
    {
        private const string Folder = "Assets/Resources/GinmereVoxel3D";
        private static readonly string[] Families = { "ground", "cliff", "rim", "ledge", "water", "anchor", "nest", "gecko", "frog", "torch", "meat", "tonic", "frost", "ice", "stalagmite", "cache" };
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
                var entries = new List<GinmereVoxelLibrary.Entry>(64);
                foreach (string family in Families)
                {
                    for (int variant = 0; variant < GinmereVoxelLibrary.VariantCount; variant++)
                    {
                        string id = GinmereVoxelLibrary.ModelId(family, variant);
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
                        entries.Add(new GinmereVoxelLibrary.Entry
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
                var library = AssetDatabase.LoadAssetAtPath<GinmereVoxelLibrary>(libraryPath);
                if (library == null)
                {
                    library = ScriptableObject.CreateInstance<GinmereVoxelLibrary>();
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
                case "cliff": Cliff(variant); break;
                case "rim": Rim(variant); break;
                case "ledge": Ledge(variant); break;
                case "water": Water(variant); break;
                case "anchor": Anchor(variant); break;
                case "nest": Nest(variant); break;
                case "gecko": Gecko(variant); break;
                case "frog": Frog(variant); break;
                case "torch": Torch(variant); break;
                case "meat": Meat(variant); break;
                case "tonic": Tonic(variant); break;
                case "frost": Frost(variant); break;
                case "ice": Ice(variant); break;
                case "stalagmite": Stalagmite(variant); break;
                case "cache": Cache(variant); break;
                default: throw new ArgumentException("Unknown Ginmere art family.", nameof(family));
            }
        }

        private static void Ground(int variant)
        {
            float thickness = .065f + variant * .008f;
            Box(new Vector3(0, -thickness * .5f, 0), new Vector3(1, thickness, 1), 12);
        }

        private static void Cliff(int variant) { Strata(1.50f + variant * .20f); }
        private static void Rim(int variant) { Strata(.60f + variant * .18f); }
        private static void Ledge(int variant) { Strata(.14f + variant * .02f); }

        private static void Strata(float height)
        {
            // Full-cell strata join into the current native mass; there are no per-cell caps.
            Box(new Vector3(0, height * .25f, 0), new Vector3(1, height * .5f, 1), 12);
            Box(new Vector3(0, height * .75f, 0), new Vector3(1, height * .5f, 1), 13);
        }

        private static void Water(int variant)
        {
            float thickness = .014f + variant * .003f;
            // A constant exposed surface belongs only to the actual MirePool owner.
            Box(new Vector3(0, .048f - thickness * .5f, 0), new Vector3(1, thickness, 1), 24);
        }

        private static void Anchor(int variant)
        {
            // Ordinary iron pin and old cut rope, not a functional ladder or climbable chain.
            Box(new Vector3(0, .24f, 0), new Vector3(.095f, .48f, .09f), 12);
            Box(new Vector3(0, .49f, 0), new Vector3(.21f, .07f, .16f), 12);
            Box(new Vector3(.12f, .37f, .05f), new Vector3(.19f, .11f, .12f), 11);
            Box(new Vector3(.19f, .20f, .05f), new Vector3(.09f, .29f + variant * .02f, .10f), 11);
        }

        private static void Nest(int variant)
        {
            float width = .84f + variant * .02f;
            float height = .24f + variant * .02f;
            float edge = width * .5f - .07f;
            Box(new Vector3(0, .03f, 0), new Vector3(width, .06f, width), 10);
            Box(new Vector3(-edge, height * .5f, 0), new Vector3(.14f, height, width), 10);
            Box(new Vector3(edge, height * .5f, 0), new Vector3(.14f, height, width), 10);
            Box(new Vector3(0, height * .5f, -edge), new Vector3(width - .20f, height, .14f), 10);
            Box(new Vector3(0, height * .5f, edge), new Vector3(width - .20f, height, .14f), 10);
            // Four pale clusters communicate a communal clutch without sixteen tiny cubes.
            // The native nest part owns the actual sixteen-egg state and defender activation.
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    Box(new Vector3(x * .14f, .12f, z * .14f), new Vector3(.17f, .12f, .17f), 19);
        }

        private static void Gecko(int variant)
        {
            float shift = (variant - 1.5f) * .01f;
            Box(new Vector3(0, .15f, .015f), new Vector3(.26f, .16f, .43f), 58);
            Box(new Vector3(0, .205f, .30f), new Vector3(.25f, .17f, .23f), 58);
            Box(new Vector3(shift, .11f, -.33f), new Vector3(.12f, .10f, .30f), 58);
            Box(new Vector3(-.205f, .09f, .13f), new Vector3(.22f, .08f, .11f), 58);
            Box(new Vector3(.205f, .09f, .13f), new Vector3(.22f, .08f, .11f), 58);
            Box(new Vector3(-.21f, .085f, -.14f), new Vector3(.22f, .09f, .13f), 58);
            Box(new Vector3(.21f, .085f, -.14f), new Vector3(.22f, .09f, .13f), 58);
            // The paired supraciliary spines and amber throat distinguish the native species.
            Box(new Vector3(-.085f, .34f, .29f), new Vector3(.045f, .18f + variant * .01f, .075f), 58);
            Box(new Vector3(.085f, .34f, .29f), new Vector3(.045f, .18f + variant * .01f, .075f), 58);
            Box(new Vector3(0, .15f, .354f), new Vector3(.17f, .11f, .15f), 21);
        }

        private static void Frog(int variant)
        {
            // Native GinFrog is golden, passive fauna; no brood armor is invented by this mesh.
            float headHeight = .26f + variant * .01f;
            Box(new Vector3(0, .24f, -.035f), new Vector3(.47f, .28f, .43f), 21);
            Box(new Vector3(0, .345f, .235f), new Vector3(.40f, headHeight, .31f), 21);
            Box(new Vector3(-.30f, .14f, -.175f), new Vector3(.24f, .24f, .32f), 21);
            Box(new Vector3(.30f, .14f, -.175f), new Vector3(.24f, .24f, .32f), 21);
            Box(new Vector3(-.20f, .14f, .22f), new Vector3(.14f, .24f, .28f), 21);
            Box(new Vector3(.20f, .14f, .22f), new Vector3(.14f, .24f, .28f), 21);
            float eyeY = .345f + headHeight * .5f + .02f;
            Box(new Vector3(-.12f, eyeY, .32f), new Vector3(.08f, .055f, .10f), 10);
            Box(new Vector3(.12f, eyeY, .32f), new Vector3(.08f, .055f, .10f), 10);
        }

        private static void Torch(int variant)
        {
            Box(new Vector3(0, .28f, 0), new Vector3(.10f, .56f, .10f), 15);
            Box(new Vector3(0, .56f, 0), new Vector3(.18f, .14f, .16f), 15);
            Box(new Vector3(0, .68f, 0), new Vector3(.22f, .17f, .18f), 49);
            Box(new Vector3((variant - 1.5f) * .018f, .79f, 0), new Vector3(.10f, .14f, .10f), 49);
        }

        private static void Meat(int variant)
        {
            Box(new Vector3(0, .06f, 0), new Vector3(.46f, .12f, .22f), 20);
            Box(new Vector3((variant - 1.5f) * .03f, .075f, .12f), new Vector3(.38f, .10f, .16f), 20);
            Box(new Vector3(0, .12f, -.085f), new Vector3(.42f, .045f, .045f), 19);
        }

        private static void Tonic(int variant)
        {
            Box(new Vector3(0, .16f, 0), new Vector3(.24f, .32f, .24f), 48);
            Box(new Vector3(0, .335f, 0), new Vector3(.18f, .08f, .18f), 48);
            Box(new Vector3(0, .40f, 0), new Vector3(.10f, .10f, .10f), 48);
            Box(new Vector3(0, .46f + variant * .005f, 0), new Vector3(.12f, .07f + variant * .01f, .12f), 11);
        }

        private static void Frost(int variant)
        {
            // Pale rime surrounds a low dark opening. Native cold/thermal parts
            // remain responsible for the hazard; this mesh adds no plume or liquid.
            float width = .78f + variant * .03f;
            float height = .28f + variant * .025f;
            float edge = width * .5f - .07f;
            Box(new Vector3(-edge, height * .5f, 0), new Vector3(.14f, height, width), 125);
            Box(new Vector3(edge, height * .5f, 0), new Vector3(.14f, height, width), 125);
            Box(new Vector3(0, height * .5f, -edge), new Vector3(width - .20f, height, .14f), 125);
            Box(new Vector3(0, height * .5f, edge), new Vector3(width - .20f, height, .14f), 125);
            Box(new Vector3(0, .05f, 0), new Vector3(.34f, .04f, .34f), 12);
        }

        private static void Ice(int variant)
        {
            float thickness = .02f + variant * .003f;
            // One quiet plane composes a sheet; there are no repeating white rims.
            Box(new Vector3(0, .056f - thickness * .5f, 0), new Vector3(1, thickness, 1), 125);
        }

        private static void Stalagmite(int variant)
        {
            float height = 1.20f + variant * .15f;
            float shift = (variant - 1.5f) * .025f;
            Box(new Vector3(0, height * .18f, 0), new Vector3(.70f, height * .36f, .64f), 12);
            Box(new Vector3(shift, height * .555f, .02f), new Vector3(.44f, height * .43f, .42f), 13);
            Box(new Vector3(shift, height * .865f, .02f), new Vector3(.18f, height * .27f, .17f), 13);
        }

        private static void Cache(int variant)
        {
            // An ordinary native container under coarse bone ribs, distinct from
            // loose Bones. No fabricated lock, destructibility or loot is attached.
            float width = .76f + variant * .02f;
            float height = .22f + variant * .02f;
            float edge = width * .5f - .07f;
            Box(new Vector3(0, .06f, 0), new Vector3(width, .12f, .65f), 12);
            Box(new Vector3(-edge, .10f + height * .5f, 0), new Vector3(.14f, height, .65f), 19);
            Box(new Vector3(edge, .10f + height * .5f, 0), new Vector3(.14f, height, .65f), 19);
            Box(new Vector3(0, .10f + height * .5f, -.255f), new Vector3(width - .14f, height, .14f), 19);
            Box(new Vector3(0, .10f + height * .5f, .255f), new Vector3(width - .14f, height, .14f), 19);
            Box(new Vector3(.13f, height + .17f, -.13f), new Vector3(.22f, .18f, .20f), 19);
            Box(new Vector3(.08f, height + .19f, -.02f), new Vector3(.04f, .05f, .04f), 12);
            Box(new Vector3(.18f, height + .19f, -.02f), new Vector3(.04f, .05f, .04f), 12);
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
