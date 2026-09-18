#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>The offline object palette changes sampled paint only. Its two
    /// colors remain actual opaque atlas colors, with deterministic mapping.</summary>
    public sealed class VoxelWorldObjectPaletteTests
    {
        static readonly Color32 Red = new Color32(184, 47, 31, 255);
        static readonly Color32 Green = new Color32(43, 116, 54, 255);
        static readonly Color32 Blue = new Color32(38, 76, 172, 255);
        static Vector2 Center(int index, int width = 4, int height = 2)
            => new Vector2((index % width + .5f) / width, (index / width + .5f) / height);
        static Color32[] Atlas() => new[] {Red, Green, Blue, Red, Green, Blue,
            new Color32(237, 208, 97, 255), new Color32(18, 18, 18, 255)};
        static Vector2[] Reduce(Vector2[] uv, Color32[] pixels, int width = 4, int height = 2)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CavesOfOoo.Editor.VoxelWorldObjectPalette"))
                .FirstOrDefault(t => t != null);
            Assert.NotNull(type, "Object color reduction must be an explicit offline step.");
            var method = type.GetMethod("Reduce", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);
            try { return (Vector2[])method.Invoke(null, new object[] {uv, pixels, width, height}); }
            catch (TargetInvocationException error) { throw error.InnerException ?? error; }
        }
        static Vector2[] Budget(Vector2[] uv, Color32[] pixels, int maximum)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CavesOfOoo.Editor.VoxelWorldObjectPalette"))
                .FirstOrDefault(t => t != null);
            Assert.NotNull(type); var method = type.GetMethod("ReduceToBudget", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);
            try { return (Vector2[])method.Invoke(null, new object[] {uv, pixels, 4, 2, maximum}); }
            catch (TargetInvocationException error) { throw error.InnerException ?? error; }
        }
        static int Pixel(Vector2 uv, int width = 4, int height = 2)
            => Mathf.Min(height - 1, Mathf.FloorToInt(uv.y * height)) * width
                + Mathf.Min(width - 1, Mathf.FloorToInt(uv.x * width));
        static int Rgb(Color32 color) => color.r << 16 | color.g << 8 | color.b;
        static string StallPath(int variant) => "Assets/Art3D/Village/Models/market-stall-" + variant + ".fbx";
        static int Slot(Vector2 uv) => Mathf.Min(7, Mathf.FloorToInt(uv.y * 8)) * 8
            + Mathf.Min(7, Mathf.FloorToInt(uv.x * 8));
        static Vector2 SlotSample(int slot, int corner = 0)
            => Center((slot / 8 * 2 + corner / 2) * 16 + slot % 8 * 2 + corner % 2, 16, 16);
        static Color32[] NativeAtlas() => Enumerable.Range(0, 256).Select(i => {
            int slot = i / 32 * 8 + i % 16 / 2, corner = i / 16 % 2 * 2 + i % 2;
            return new Color32((byte)(25 + slot * 3), (byte)(20 + corner * 30), (byte)(210 - slot * 2), 255);
        }).ToArray();
        static Vector2[] Native(Vector2[] input, Color32[] pixels, string sourcePath,
            string texturePath = "Assets/Art3D/Village/Textures/VillagePalette.png")
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CavesOfOoo.Editor.VoxelWorldObjectPalette"))
                .FirstOrDefault(t => t != null);
            Assert.NotNull(type);
            var method = type.GetMethod("ReduceNativeObject", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "Authored stall canopies need their semantic cloth/wood mapping before generic clustering.");
            Assert.AreEqual(6, method.GetParameters().Length, "Semantic atlas roles require the source texture's identity as well as the model path.");
            try { return (Vector2[])method.Invoke(null, new object[] {input, pixels, 16, 16, sourcePath, texturePath}); }
            catch (TargetInvocationException error) { throw error.InnerException ?? error; }
        }
        static void Canonical(Vector2[] output, Color32[] pixels, int width = 4, int height = 2)
        {
            foreach (var uv in output)
            {
                Assert.IsFalse(float.IsNaN(uv.x) || float.IsInfinity(uv.x) || float.IsNaN(uv.y) || float.IsInfinity(uv.y));
                Assert.That(uv.x, Is.InRange(0f, 1f)); Assert.That(uv.y, Is.InRange(0f, 1f));
                int index = Pixel(uv, width, height);
                Assert.That(Vector2.Distance(Center(index, width, height), uv), Is.LessThan(.000001f));
                Assert.AreEqual(255, pixels[index].a, "Every returned center must sample opaque paint.");
            }
        }

        [Test]
        public void ThreeColorObjectsReduceToAtMostTwoExistingColorsAndKeepEachFaceConstant()
        {
            var pixels = Atlas();
            var input = Enumerable.Range(0, 3).SelectMany(i => Enumerable.Repeat(Center(i), 4)).ToArray();
            var output = Reduce(input, pixels);
            Assert.AreEqual(input.Length, output.Length); Assert.AreNotSame(input, output);
            var before = input.Select(uv => Rgb(pixels[Pixel(uv)])).Distinct().ToArray();
            var after = output.Select(uv => Rgb(pixels[Pixel(uv)])).Distinct().ToArray();
            Assert.AreEqual(3, before.Length); Assert.That(after.Length, Is.InRange(1, 2));
            CollectionAssert.IsSubsetOf(after, before, "Do not invent blended RGB values or borrow an unsampled accent.");
            for (int face = 0; face < 3; face++)
                for (int corner = 1; corner < 4; corner++) Assert.AreEqual(output[face * 4], output[face * 4 + corner]);
            Canonical(output, pixels);
        }

        [TestCase(1)] [TestCase(2)]
        public void AlreadyCompliantObjectsPreserveEverySampledRgb(int colors)
        {
            var pixels = Atlas();
            var input = Enumerable.Range(0, 12).Select(i => Center(i % colors) + new Vector2(.013f, -.021f)).ToArray();
            var output = Reduce(input, pixels);
            CollectionAssert.AreEqual(input.Select(uv => Rgb(pixels[Pixel(uv)])),
                output.Select(uv => Rgb(pixels[Pixel(uv)])));
            Assert.AreEqual(colors, output.Select(uv => Rgb(pixels[Pixel(uv)])).Distinct().Count());
            Canonical(output, pixels);
        }

        [Test]
        public void EqualRgbFromSeveralTexelsUsesOneCanonicalColorWithoutLosingThatColor()
        {
            var pixels = Enumerable.Repeat(Red, 8).ToArray();
            var input = Enumerable.Range(0, 8).Select(i => Center(i) + new Vector2(.013f, -.021f)).ToArray();
            var output = Reduce(input, pixels);
            Assert.AreEqual(1, output.Distinct().Count());
            Assert.IsTrue(output.All(uv => Rgb(pixels[Pixel(uv)]) == Rgb(Red)));
            Canonical(output, pixels);
        }

        [Test]
        public void RepeatedAndReorderedInputHasTheSamePerSampleMappingAndIsIdempotent()
        {
            var pixels = Atlas(); var input = new[] {Center(0), Center(1), Center(2), Center(2), Center(1), Center(0)};
            var output = Reduce(input, pixels); var order = new[] {4, 2, 0, 5, 3, 1};
            CollectionAssert.AreEqual(output, Reduce(input, pixels));
            CollectionAssert.AreEqual(output, Reduce(output, pixels));
            var shuffled = Reduce(order.Select(i => input[i]).ToArray(), pixels);
            for (int i = 0; i < order.Length; i++) Assert.AreEqual(output[order[i]], shuffled[i], "Input order cannot break color-frequency ties.");
        }

        [Test]
        public void ReductionDoesNotMutateBorrowedArraysOrConsumeUnityRandom()
        {
            var pixels = Atlas(); var input = new[] {Center(0), Center(1), Center(2)};
            var oldPixels = pixels.ToArray(); var oldInput = input.ToArray(); var state = UnityEngine.Random.state;
            var result = Reduce(input, pixels);
            Assert.AreNotSame(input, result); CollectionAssert.AreEqual(oldInput, input); CollectionAssert.AreEqual(oldPixels, pixels);
            Assert.AreEqual(state, UnityEngine.Random.state);
        }

        [TestCase(0)] [TestCase(254)]
        public void NonopaqueSampleRejectsWhileUnusedTransparentAtlasPaddingIsAllowed(int alpha)
        {
            var pixels = Atlas(); pixels[1] = new Color32(Green.r, Green.g, Green.b, (byte)alpha);
            var input = new[] {Center(0), Center(1)}; var old = input.ToArray(); var oldPixels = pixels.ToArray();
            Assert.Catch<ArgumentException>(() => Reduce(input, pixels));
            CollectionAssert.AreEqual(old, input); CollectionAssert.AreEqual(oldPixels, pixels);
            var valid = Reduce(new[] {Center(0)}, pixels);
            Assert.AreEqual(Rgb(Red), Rgb(pixels[Pixel(valid[0])])); Canonical(valid, pixels);
        }

        [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] [TestCase(float.NegativeInfinity)]
        [TestCase(-.001f)] [TestCase(1.001f)]
        public void NonfiniteOrOutOfRangeUvRejectsWithoutEditingEarlierValidSamples(float bad)
        {
            foreach (int axis in new[] {0, 1})
            {
                var invalid = Center(1); invalid[axis] = bad; var input = new[] {Center(0), invalid};
                var before = input.ToArray(); var pixels = Atlas(); var oldPixels = pixels.ToArray();
                Assert.Catch<ArgumentException>(() => Reduce(input, pixels));
                for (int i = 0; i < input.Length; i++) for (int component = 0; component < 2; component++)
                    Assert.AreEqual(BitConverter.SingleToInt32Bits(before[i][component]), BitConverter.SingleToInt32Bits(input[i][component]));
                CollectionAssert.AreEqual(oldPixels, pixels);
            }
        }

        [Test]
        public void UvBoundaryCoordinatesResolveToActualEdgeTexels()
        {
            var pixels = Atlas(); var input = new[] {Vector2.zero, Vector2.one}; var output = Reduce(input, pixels);
            Assert.AreEqual(Rgb(pixels[0]), Rgb(pixels[Pixel(output[0])]));
            Assert.AreEqual(Rgb(pixels[7]), Rgb(pixels[Pixel(output[1])])); Canonical(output, pixels);
        }

        [TestCase(0, 2, 4)] [TestCase(-1, 2, 4)] [TestCase(2, 0, 4)]
        [TestCase(2, 2, 3)] [TestCase(2, 2, 5)] [TestCase(int.MaxValue, 2, 4)]
        public void MalformedTextureDimensionsRejectBeforeSamplingOrAllocating(int width, int height, int length)
        {
            var pixels = Enumerable.Repeat(Red, length).ToArray();
            Assert.Catch<ArgumentException>(() => Reduce(new[] {Vector2.one * .5f}, pixels, width, height));
        }

        [Test]
        public void NullBuffersRejectExplicitly()
        {
            Assert.Catch<ArgumentException>(() => Reduce(null, Atlas()));
            Assert.Catch<ArgumentException>(() => Reduce(new[] {Center(0)}, null));
        }

        [Test]
        public void WaterReservationCanReduceTheOpaqueSiblingToOneExistingColor()
        {
            var pixels = Atlas(); var input = new[] {Center(0), Center(1), Center(2)};
            var output = Budget(input, pixels, 1);
            Assert.AreEqual(1, output.Select(uv => Rgb(pixels[Pixel(uv)])).Distinct().Count());
            CollectionAssert.IsSubsetOf(output.Select(uv => Rgb(pixels[Pixel(uv)])).Distinct().ToArray(),
                input.Select(uv => Rgb(pixels[Pixel(uv)])).Distinct().ToArray());
            CollectionAssert.AreEqual(output, Budget(output, pixels, 1)); Canonical(output, pixels);
            CollectionAssert.AreEqual(Reduce(input, pixels), Budget(input, pixels, 2));
        }

        [TestCase(0)] [TestCase(-1)] [TestCase(3)] [TestCase(int.MaxValue)]
        public void UnsupportedColorBudgetRejectsWithoutMutatingSamples(int maximum)
        {
            var pixels = Atlas(); var input = new[] {Center(0), Center(1)};
            var before = input.ToArray(); var oldPixels = pixels.ToArray();
            Assert.Catch<ArgumentException>(() => Budget(input, pixels, maximum));
            CollectionAssert.AreEqual(before, input); CollectionAssert.AreEqual(oldPixels, pixels);
        }

        [TestCase(0)] [TestCase(1)]
        public void NativeStallCanopyAndWoodUseTwoSampledAnchorsDeterministicallyWithoutMutation(int variant)
        {
            int accent = variant == 0 ? 22 : 23;
            int[] cloth = variant == 0 ? new[] {21, 22, 49} : new[] {23, 30};
            var input = cloth.SelectMany(slot => Enumerable.Repeat(SlotSample(slot), 4))
                .Concat(new[] {SlotSample(accent, 3), SlotSample(8), SlotSample(8, 3), SlotSample(9),
                    SlotSample(10), SlotSample(11), SlotSample(12), SlotSample(14), SlotSample(19) }).ToArray();
            var pixels = NativeAtlas(); var oldInput = input.ToArray(); var oldPixels = pixels.ToArray();
            var state = UnityEngine.Random.state;
            var output = Native(input, pixels, StallPath(variant));
            Assert.AreEqual(input.Length, output.Length); Assert.AreNotSame(input, output);
            var accentPixels = input.Where(uv => Slot(uv) == accent).Select(uv => Pixel(uv, 16, 16)).ToArray();
            var woodPixels = input.Where(uv => Slot(uv) >= 8 && Slot(uv) <= 11).Select(uv => Pixel(uv, 16, 16)).ToArray();
            var paintedCloth = output.Where((uv, index) => cloth.Contains(Slot(input[index]))).Distinct().ToArray();
            var paintedBody = output.Where((uv, index) => !cloth.Contains(Slot(input[index]))).Distinct().ToArray();
            Assert.AreEqual(1, paintedCloth.Length); Assert.AreEqual(1, paintedBody.Length);
            CollectionAssert.Contains(accentPixels, Pixel(paintedCloth[0], 16, 16), "Use an actually sampled teal/violet texel, not gold or a fabricated center.");
            CollectionAssert.Contains(woodPixels, Pixel(paintedBody[0], 16, 16), "All remaining faces share one actually sampled wood texel.");
            Assert.AreNotEqual(Rgb(pixels[Pixel(paintedCloth[0], 16, 16)]), Rgb(pixels[Pixel(paintedBody[0], 16, 16)]));
            Canonical(output, pixels, 16, 16);
            CollectionAssert.AreEqual(output, Native(input, pixels, StallPath(variant)));
            CollectionAssert.AreEqual(output, Native(output, pixels, StallPath(variant)), "An already consolidated canopy remains stable.");
            var order = Enumerable.Range(0, input.Length).Reverse().ToArray();
            var shuffled = Native(order.Select(i => input[i]).ToArray(), pixels, StallPath(variant));
            for (int i = 0; i < order.Length; i++) Assert.AreEqual(output[order[i]], shuffled[i]);
            CollectionAssert.AreEqual(oldInput, input); CollectionAssert.AreEqual(oldPixels, pixels);
            Assert.AreEqual(state, UnityEngine.Random.state);
        }

        [TestCase(0, true)] [TestCase(0, false)] [TestCase(1, true)] [TestCase(1, false)]
        public void SupportedNativeStallMissingSampledAccentOrWoodFailsClosed(int variant, bool missingAccent)
        {
            int accent = variant == 0 ? 22 : 23, otherCloth = variant == 0 ? 21 : 30;
            var input = new[] {SlotSample(otherCloth), SlotSample(missingAccent ? 8 : accent), SlotSample(12)};
            var pixels = NativeAtlas(); var before = input.ToArray(); var oldPixels = pixels.ToArray();
            Assert.Catch<ArgumentException>(() => Native(input, pixels, StallPath(variant)));
            CollectionAssert.AreEqual(before, input); CollectionAssert.AreEqual(oldPixels, pixels);
        }

        [TestCase("Assets/Art3D/SpawnRing/Models/market-stall-0.fbx")]
        [TestCase("Assets/Art3D/Village/Models/market-stall-0.fbx.backup")]
        [TestCase("Assets/Art3D/Village/Models/market-stall-2.fbx")]
        public void NativeCanopyMappingRequiresAnExactSupportedSourcePath(string path)
        {
            var pixels = NativeAtlas();
            var input = new[] {SlotSample(21), SlotSample(22), SlotSample(49), SlotSample(8), SlotSample(12)};
            CollectionAssert.AreEqual(Reduce(input, pixels, 16, 16), Native(input, pixels, path));
        }

        [TestCase("Assets/Art3D/SpawnRing/Textures/SpawnRingPalette.png")]
        [TestCase("Assets/Art3D/MultiCellPilot/Textures/PilotPalette.png")]
        [TestCase("Assets/Art3D/Village/Textures/VillagePalette.png.backup")]
        public void AStallWithAnotherAtlasRetainsGenericReduction(string texture)
        {
            var pixels = NativeAtlas();
            var input = new[] {SlotSample(21), SlotSample(22), SlotSample(49), SlotSample(8), SlotSample(12)};
            CollectionAssert.AreEqual(Reduce(input, pixels, 16, 16), Native(input, pixels, StallPath(0), texture));
            CollectionAssert.AreNotEqual(Reduce(input, pixels, 16, 16), Native(input, pixels, StallPath(0)),
                "The identical fixture with the correct atlas must actually exercise the semantic rule.");
        }

        [TestCase("herb-pot", "tint", false)] [TestCase("herb-pot", "tint", true)]
        [TestCase("herb-pot", "texture", false)] [TestCase("herb-pot", "texture", true)]
        [TestCase("ground-patch-0", "tint", false)] [TestCase("ground-patch-0", "tint", true)]
        [TestCase("ground-patch-0", "scale", false)] [TestCase("ground-patch-0", "scale", true)]
        [TestCase("ground-patch-0", "offset", false)] [TestCase("ground-patch-0", "offset", true)]
        [TestCase("ground-patch-0", "texture", false)] [TestCase("ground-patch-0", "texture", true)]
        public void ASharedForeignUsagePermanentlyVetoesBothPaintPasses(string model, string unsupported, bool excludedFirst)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CavesOfOoo.Editor.VoxelWorldMeshBuilder"))
                .FirstOrDefault(t => t != null);
            Assert.NotNull(type); var sourceType = type.GetNestedType("Source", BindingFlags.NonPublic);
            var add = type.GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(sourceType); Assert.NotNull(add);
            var dictionaryType = typeof(System.Collections.Generic.Dictionary<,>).MakeGenericType(typeof(Mesh), sourceType);
            var rows = (System.Collections.IDictionary)Activator.CreateInstance(dictionaryType);
            var library = Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath); Assert.NotNull(library);
            var prefab = library.FindModel(model); Assert.NotNull(prefab);
            var mesh = prefab.GetComponentInChildren<MeshFilter>(true).sharedMesh;
            var obj = new GameObject("disposable palette eligibility fixture"); var material = new Material(library.WorldMaterial);
            try
            {
                var renderer = obj.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
                void Add(System.Collections.IDictionary target)
                {
                    try { add.Invoke(null, new object[] {mesh, renderer, false, target}); }
                    catch (TargetInvocationException error) { throw error.InnerException ?? error; }
                }
                string Texture(System.Collections.IDictionary target)
                    => (string)sourceType.GetField("ObjectTexture").GetValue(target[mesh]);
                int Columns(System.Collections.IDictionary target)
                    => (int)sourceType.GetField("PaintColumns").GetValue(target[mesh]);
                void Configure(bool excluded)
                {
                    material.SetColor("_BaseColor", Color.white);
                    material.SetTexture("_BaseMap", library.WorldMaterial.GetTexture("_BaseMap"));
                    material.SetTextureScale("_BaseMap", Vector2.one);
                    material.SetTextureOffset("_BaseMap", Vector2.zero);
                    if (!excluded) return;
                    switch (unsupported)
                    {
                        case "tint": material.SetColor("_BaseColor", Color.gray); break;
                        case "texture": material.SetTexture("_BaseMap", null); break;
                        case "scale": material.SetTextureScale("_BaseMap", new Vector2(2f, 1f)); break;
                        case "offset": material.SetTextureOffset("_BaseMap", new Vector2(.125f, 0f)); break;
                        default: Assert.Fail("Unsupported fixture mutation: " + unsupported); break;
                    }
                }
                int expectedColumns = model == "ground-patch-0" ? 8 : 0;
                Configure(false); Add(rows);
                Assert.AreEqual("Assets/Art3D/Village/Textures/VillagePalette.png", Texture(rows));
                Assert.AreEqual(expectedColumns, Columns(rows), "The ambient fixture must positively exercise S1 eligibility.");
                rows.Clear();
                foreach (bool excluded in new[] {excludedFirst, !excludedFirst})
                { Configure(excluded); Add(rows); }
                Assert.IsNull(Texture(rows), "A shared unsupported usage must veto the complete-object pass.");
                Assert.AreEqual(0, Columns(rows), "An object-palette veto must also prevent the earlier ambient pass from recoloring the shared mesh.");
                Configure(false); Add(rows);
                Assert.IsNull(Texture(rows), "A later valid usage must not undo an earlier object-palette veto.");
                Assert.AreEqual(0, Columns(rows), "A later valid usage must not reactivate ambient paint either.");
                var fresh = (System.Collections.IDictionary)Activator.CreateInstance(dictionaryType);
                Add(fresh);
                Assert.AreEqual("Assets/Art3D/Village/Textures/VillagePalette.png", Texture(fresh), "A fresh source with only supported usage still qualifies.");
                Assert.AreEqual(expectedColumns, Columns(fresh), "The veto belongs to the shared source's uses, not a global material or mesh flag.");
            }
            finally { UnityEngine.Object.DestroyImmediate(obj); UnityEngine.Object.DestroyImmediate(material); }
        }
    }
}
#endif
