#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Counts authored base paint, not lighting/shadow pixels. The cap
    /// belongs to the complete prefab, including separate water renderers.</summary>
    [Category("VoxelWorldRealArt")]
    public sealed class VoxelWorldObjectPaletteArtTests
    {
        const string PaletteShader = "CavesOfOoo/Village3D/Palette";
        const string WaterShader = "CavesOfOoo/Village3D/Water";
        const string ToolkitRoot = "Assets/Art3D/VoxelWorld/Toolkit/";
        static readonly HashSet<string> PalettePaths = new HashSet<string>(StringComparer.Ordinal) {
            "Assets/Art3D/Village/Textures/VillagePalette.png",
            "Assets/Art3D/SpawnRing/Textures/SpawnRingPalette.png",
            "Assets/Art3D/MultiCellPilot/Textures/PilotPalette.png"
        };
        sealed class Pixels { public Color32[] Colors; public int Width, Height; }
        readonly Dictionary<string, Pixels> palettes = new Dictionary<string, Pixels>(StringComparer.Ordinal);
        VoxelWorldMeshCatalog catalog;
        GameObject[] prefabs;
        Dictionary<Mesh, VoxelWorldMeshCatalog.Binding> bindings;

        [OneTimeSetUp]
        public void BorrowAllNativeLibraries()
        {
            catalog = Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath);
            Assert.NotNull(catalog); catalog.Validate();
            var village = Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);
            var ring = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            var pilot = Resources.Load<MultiCellPilot3DLibrary>(MultiCellPilot3DLibrary.ResourcePath);
            Assert.NotNull(village); Assert.NotNull(ring); Assert.NotNull(pilot);
            village.Validate(); ring.Validate(); pilot.Validate();
            prefabs = village.Models.Select(m => m.Prefab).Concat(ring.Models.Select(m => m.Prefab))
                .Concat(pilot.Models.Select(m => m.Prefab))
                .Concat(ring.EquipmentLibrary.Models.Select(m => m.Prefab)).Distinct().ToArray();
            bindings = catalog.Bindings.ToDictionary(b => b.Source);
            Assert.AreEqual(392, prefabs.Length); Assert.AreEqual(406, bindings.Count);
            var sources = new HashSet<Mesh>(prefabs.SelectMany(p => p.GetComponentsInChildren<Renderer>(true)).Select(Source));
            Assert.IsFalse(sources.Contains(null)); CollectionAssert.AreEquivalent(sources, bindings.Keys);
        }

        static Mesh Source(Renderer renderer)
            => renderer is SkinnedMeshRenderer skin ? skin.sharedMesh : renderer.GetComponent<MeshFilter>()?.sharedMesh;
        static bool Water(Renderer renderer) => renderer.sharedMaterial.shader.name == WaterShader;
        static int Rgb(Color32 value) => value.r << 16 | value.g << 8 | value.b;

        Pixels ReadPalette(string path)
        {
            if (palettes.TryGetValue(path, out var pixels)) return pixels;
            var copy = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.IsTrue(ImageConversion.LoadImage(copy, File.ReadAllBytes(path)), path);
                pixels = new Pixels { Colors = copy.GetPixels32(), Width = copy.width, Height = copy.height };
                palettes.Add(path, pixels); return pixels;
            }
            finally { Object.DestroyImmediate(copy); }
        }

        HashSet<int> Colors(Renderer renderer)
        {
            Assert.AreEqual(1, renderer.sharedMaterials.Length, renderer.name);
            var material = renderer.sharedMaterial; Assert.NotNull(material);
            Assert.IsTrue(material.HasProperty("_BaseColor"));
            var tint = material.GetColor("_BaseColor"); Assert.AreEqual(1f, tint.a, material.name);
            if (Water(renderer))
            {
                Assert.IsNull(material.GetTexture("_BaseMap"), "Water contributes its existing solid base color, not an invented palette sample.");
                return new HashSet<int> {Rgb((Color32)tint)};
            }
            Assert.AreEqual(PaletteShader, material.shader.name, renderer.name);
            Assert.AreEqual(Color.white, tint, "Only the verified white-tint native palette contract is supported.");
            string path = AssetDatabase.GetAssetPath(material.GetTexture("_BaseMap"));
            Assert.IsTrue(PalettePaths.Contains(path), "Unexpected source atlas: " + path);
            var pixels = ReadPalette(path); var mesh = bindings[Source(renderer)].Voxel; var uv = mesh.uv;
            Assert.AreEqual(mesh.vertexCount, uv.Length); var colors = new HashSet<int>();
            foreach (var coordinate in uv)
            {
                Assert.That(coordinate.x, Is.InRange(0f, 1f)); Assert.That(coordinate.y, Is.InRange(0f, 1f));
                int x = Mathf.Min(pixels.Width - 1, Mathf.FloorToInt(coordinate.x * pixels.Width));
                int y = Mathf.Min(pixels.Height - 1, Mathf.FloorToInt(coordinate.y * pixels.Height));
                Color32 pixel = pixels.Colors[y * pixels.Width + x]; Assert.AreEqual(255, pixel.a, renderer.name);
                colors.Add(Rgb((Color32)((Color)pixel * tint)));
            }
            Assert.Greater(colors.Count, 0); return colors;
        }

        [Test]
        public void All392CompleteNativeObjectsUseAtMostTwoAuthoredBaseColors()
        {
            var failures = new List<string>(); int twoColorObjects = 0, visited = 0;
            foreach (var prefab in prefabs)
            {
                var colors = new HashSet<int>(); var renderers = prefab.GetComponentsInChildren<Renderer>(true);
                Assert.Greater(renderers.Length, 0, prefab.name);
                foreach (var renderer in renderers) colors.UnionWith(Colors(renderer));
                if (colors.Count > 2) failures.Add(prefab.name + "=" + colors.Count);
                if (colors.Count == 2) twoColorObjects++;
                visited++;
            }
            Assert.AreEqual(392, visited);
            Assert.IsEmpty(failures, "Whole-object palette violations: " + string.Join(", ", failures));
            Assert.Greater(twoColorObjects, 0, "The gate must not pass by flattening every object to one color.");
        }

        [Test]
        public void FourteenMixedWaterObjectsReserveOneColorAndStandaloneWaterKeepsItsSolidColor()
        {
            int checkedObjects = 0, standalone = 0;
            foreach (var prefab in prefabs)
            {
                var renderers = prefab.GetComponentsInChildren<Renderer>(true);
                var water = renderers.Where(Water).ToArray(); if (water.Length == 0) continue;
                Assert.AreEqual(1, water.Length, prefab.name);
                Assert.AreEqual(1, Colors(water[0]).Count, prefab.name + " solid water color");
                if (renderers.Length == 1) { standalone++; continue; }
                Assert.AreEqual(2, renderers.Length, prefab.name);
                var opaque = renderers.Single(r => !Water(r));
                Assert.AreEqual(1, Colors(opaque).Count, prefab.name + " must reserve the other color for its water renderer.");
                checkedObjects++;
            }
            Assert.AreEqual(14, checkedObjects, "Do not omit water-bearing prefabs from the complete-object contract.");
            Assert.AreEqual(1, standalone, "The standalone water object remains part of the complete-library check.");
        }

        [TestCase(0)] [TestCase(1)]
        public void BothNativeStallsRetainTheirAuthoredCanopyIdentityAndOneWoodColor(int variant)
        {
            string path = "Assets/Art3D/Village/Models/market-stall-" + variant + ".fbx";
            var row = catalog.Bindings.Single(b => AssetDatabase.GetAssetPath(b.Source) == path);
            if (MorrowfastCoarseArtContract.IsVerified(row))
            {
                var village = Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);
                var prefab = village.FindModel("market-stall-" + variant);
                var filter = prefab.GetComponentsInChildren<MeshFilter>(true).Single();
                var transform = prefab.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                var points = row.Voxel.vertices.Select(transform.MultiplyPoint3x4).ToArray(); var uv = row.Voxel.uv;
                Assert.LessOrEqual(points.Length, 240);
                Assert.AreEqual(2, uv.Distinct().Count());
                int SlotOf(Vector2 coordinate) => Mathf.FloorToInt(coordinate.y * 8) * 8 + Mathf.FloorToInt(coordinate.x * 8);
                var crown = Enumerable.Range(0, points.Length).Where(i => points[i].y > 1.9f).ToArray();
                var structure = Enumerable.Range(0, points.Length).Where(i => points[i].y < 1.7f).ToArray();
                Assert.IsNotEmpty(crown); Assert.IsNotEmpty(structure);
                foreach (int i in crown) Assert.AreEqual(variant == 0 ? 22 : 23, SlotOf(uv[i]), "The entire high canopy retains its native cloth identity.");
                foreach (int i in structure) Assert.AreEqual(8, SlotOf(uv[i]), "The separate lower structure remains weathered wood.");
                Assert.AreEqual(2, Colors(filter.GetComponent<MeshRenderer>()).Count);
                return;
            }
            var fresh = VoxelWorldMeshBaker.Bake(row.Source, row.VoxelSize);
            try
            {
                var pixels = ReadPalette("Assets/Art3D/Village/Textures/VillagePalette.png");
                int Pixel(Vector2 uv) => Mathf.Min(pixels.Height - 1, Mathf.FloorToInt(uv.y * pixels.Height)) * pixels.Width
                    + Mathf.Min(pixels.Width - 1, Mathf.FloorToInt(uv.x * pixels.Width));
                int Slot(Vector2 uv) => Mathf.Min(7, Mathf.FloorToInt(uv.y * 8)) * 8
                    + Mathf.Min(7, Mathf.FloorToInt(uv.x * 8));
                int accent = variant == 0 ? 22 : 23;
                int[] cloth = variant == 0 ? new[] {21, 22, 49} : new[] {23, 30};
                var original = fresh.uv; var painted = row.Voxel.uv;
                Assert.AreEqual(original.Length, painted.Length);
                CollectionAssert.AreEqual(fresh.vertices, row.Voxel.vertices, "Original UV order must describe these exact final vertices.");
                CollectionAssert.AreEqual(fresh.triangles, row.Voxel.triangles, "Per-face correspondence must survive publication.");
                var accentPixels = new HashSet<int>(original.Where(uv => Slot(uv) == accent).Select(Pixel));
                var woodPixels = new HashSet<int>(original.Where(uv => Slot(uv) >= 8 && Slot(uv) <= 11).Select(Pixel));
                Assert.IsNotEmpty(accentPixels, path + " source accent anchor"); Assert.IsNotEmpty(woodPixels, path + " source wood anchor");
                var canopy = new HashSet<int>(); var body = new HashSet<int>();
                for (int i = 0; i < painted.Length; i++)
                {
                    bool isCloth = cloth.Contains(Slot(original[i])); int pixel = Pixel(painted[i]);
                    Assert.IsTrue((isCloth ? accentPixels : woodPixels).Contains(pixel),
                        path + " vertex " + i + " authored slot " + Slot(original[i]) + " must use a sampled " + (isCloth ? "canopy accent" : "wood") + " texel.");
                    var center = new Vector2((pixel % pixels.Width + .5f) / pixels.Width, (pixel / pixels.Width + .5f) / pixels.Height);
                    Assert.That(Vector2.Distance(center, painted[i]), Is.LessThan(.000001f));
                    (isCloth ? canopy : body).Add(pixel);
                }
                Assert.AreEqual(1, canopy.Count, path + " contiguous canopy color");
                Assert.AreEqual(1, body.Count, path + " consolidated wooden structure");
                Assert.AreNotEqual(Rgb(pixels.Colors[canopy.Single()]), Rgb(pixels.Colors[body.Single()]));
            }
            finally { Object.DestroyImmediate(fresh); }
        }

        [Test]
        public void ColorReductionPreservesEveryGenericGeometryAndSkinBindingOutsideVerifiedShapeOverlays()
        {
            int checkedMeshes = 0, toolkit = 0, coarse = 0;
            foreach (var row in catalog.Bindings)
            {
                if (AssetDatabase.GetAssetPath(row.Voxel).StartsWith(ToolkitRoot, StringComparison.Ordinal))
                { toolkit++; continue; }
                if (MorrowfastCoarseArtContract.IsVerified(row)) { coarse++; continue; }
                var fresh = VoxelWorldMeshBaker.Bake(row.Source, row.VoxelSize);
                try
                {
                    CollectionAssert.AreEqual(fresh.vertices, row.Voxel.vertices, row.Source.name + " vertices");
                    CollectionAssert.AreEqual(fresh.normals, row.Voxel.normals, row.Source.name + " normals");
                    CollectionAssert.AreEqual(fresh.boneWeights, row.Voxel.boneWeights, row.Source.name + " weights");
                    CollectionAssert.AreEqual(fresh.bindposes, row.Voxel.bindposes, row.Source.name + " bindposes");
                    Assert.AreEqual(fresh.bounds, row.Voxel.bounds, row.Source.name + " bounds");
                    Assert.AreEqual(fresh.subMeshCount, row.Voxel.subMeshCount, row.Source.name + " material slots");
                    for (int sub = 0; sub < fresh.subMeshCount; sub++)
                        CollectionAssert.AreEqual(fresh.GetTriangles(sub), row.Voxel.GetTriangles(sub), row.Source.name + " indices");
                    checkedMeshes++;
                }
                finally { Object.DestroyImmediate(fresh); }
            }
            Assert.AreEqual(15, toolkit, "Active ring recipe overrides retain their independent topology/fit gate.");
            Assert.AreEqual(117, coarse, "Only the complete native-coordinate Morrowfast scenery overlay is exempt from the old surface bake.");
            Assert.AreEqual(274, checkedMeshes);
        }

        [Test]
        public void WaterGeometryAndUvsRemainExactlyOutsideThePaintRewrite()
        {
            var water = prefabs.SelectMany(p => p.GetComponentsInChildren<Renderer>(true)).Where(Water).ToArray();
            Assert.AreEqual(15, water.Length, "All fourteen mixed-object waters and the standalone water retain their geometry.");
            int coarse = 0, generic = 0;
            foreach (var renderer in water)
            {
                var row = bindings[Source(renderer)];
                Assert.IsNull(renderer.sharedMaterial.GetTexture("_BaseMap"));
                if (MorrowfastCoarseArtContract.IsVerified(row))
                {
                    // The town's five exact creek masks and one open cistern
                    // now use broad quiet surfaces. Native-cell rays and actual
                    // imported transforms are pinned by the dedicated fixture.
                    Assert.AreEqual(1, row.Voxel.uv.Distinct().Count());
                    Assert.AreEqual(1, Colors(renderer).Count); coarse++; continue;
                }
                var fresh = VoxelWorldMeshBaker.Bake(row.Source, row.VoxelSize);
                try
                {
                    Assert.IsNull(renderer.sharedMaterial.GetTexture("_BaseMap"));
                    CollectionAssert.AreEqual(fresh.uv, row.Voxel.uv, renderer.name + " water UVs");
                    CollectionAssert.AreEqual(fresh.vertices, row.Voxel.vertices, renderer.name + " water geometry");
                    CollectionAssert.AreEqual(fresh.triangles, row.Voxel.triangles, renderer.name + " water faces");
                    Assert.AreEqual(fresh.bounds, row.Voxel.bounds, renderer.name + " water bounds");
                    generic++;
                }
                finally { Object.DestroyImmediate(fresh); }
            }
            Assert.AreEqual(6, coarse); Assert.AreEqual(9, generic);
        }
    }
}
#endif
