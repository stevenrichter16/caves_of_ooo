using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class SeedKeeperFlatAdversarialTests
    {
        static SeedKeeperFlatSource Source() => new SeedKeeperFlatSource
        {
            schemaVersion = 1, width = 64, height = 80, palette = new[] { "#234567" },
            pixels = Enumerable.Range(0, 100).Select(i => new SeedKeeperFlatSource.Pixel { x = i % 10, y = i / 10, color = 0 }).ToArray()
        };
        [Test] public void BoundedUniquePixelsAreAcceptedWithoutChangingTheirData()
        {
            var source = Source(); string before = JsonUtility.ToJson(source); source.Validate();
            Assert.AreEqual(before, JsonUtility.ToJson(source));
        }
        [Test] public void Native16By24PixelsAreAcceptedWithoutExpandingTheirCanvas()
        {
            var source = Source(); source.width = 16; source.height = 24;
            string before = JsonUtility.ToJson(source); source.Validate();
            Assert.AreEqual(before, JsonUtility.ToJson(source));
        }
        [TestCase(16, 80)][TestCase(64, 24)][TestCase(0, 24)][TestCase(16, 25)]
        public void CanvasDimensionsMustMatchOneCompleteSupportedFormat(int width, int height)
        {
            var source = Source(); source.width = width; source.height = height;
            Assert.Throws<ArgumentException>(() => source.Validate());
        }
        [TestCase(5, 24, false)][TestCase(14, 24, true)][TestCase(16, 7, false)]
        public void ImportPreflightRejectsAnInvalidFittedEnvelopeWithoutTouchingAssets(int columns, int rows, bool accepted)
        {
            var source = Source(); source.width = 16; source.height = 24;
            source.pixels = Enumerable.Range(0, columns * rows)
                .Select(i => new SeedKeeperFlatSource.Pixel { x = i % columns, y = i / columns, color = 0 }).ToArray();
            source.Validate(); string before = JsonUtility.ToJson(source);
            var builder = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("CavesOfOoo.Editor.SeedKeeperFlatBuilder", false)).FirstOrDefault(t => t != null);
            Assert.NotNull(builder);
            var preflight = builder.GetMethod("ValidatePixelStep", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(preflight, "Fit both dimensions before replacing any live keeper assets.");
            if (accepted)
            {
                float step = (float)preflight.Invoke(null, new object[] { source });
                Assert.AreEqual(1.6f / 24, step, .000001f);
            }
            else
            {
                var error = Assert.Throws<TargetInvocationException>(() => preflight.Invoke(null, new object[] { source }));
                Assert.IsInstanceOf<InvalidOperationException>(error.InnerException);
            }
            Assert.AreEqual(before, JsonUtility.ToJson(source), "Preflight must not rewrite source pixels.");
        }
        [TestCase("schema")][TestCase("width")][TestCase("height")][TestCase("null-palette")]
        [TestCase("empty-palette")][TestCase("large-palette")][TestCase("null-color")][TestCase("named-color")]
        [TestCase("alpha-color")][TestCase("bad-color")][TestCase("null-pixels")][TestCase("few-pixels")]
        [TestCase("many-pixels")][TestCase("null-pixel")][TestCase("duplicate")][TestCase("left")]
        [TestCase("right")][TestCase("top")][TestCase("bottom")][TestCase("negative-swatch")][TestCase("missing-swatch")]
        public void MalformedBlenderExportIsRejectedBeforeAnyAssetMutation(string change)
        {
            var source = Source();
            switch (change)
            {
                case "schema": source.schemaVersion = 2; break;
                case "width": source.width = 65; break;
                case "height": source.height = 0; break;
                case "null-palette": source.palette = null; break;
                case "empty-palette": source.palette = Array.Empty<string>(); break;
                case "large-palette": source.palette = Enumerable.Repeat("#234567", 257).ToArray(); break;
                case "null-color": source.palette[0] = null; break;
                case "named-color": source.palette[0] = "red"; break;
                case "alpha-color": source.palette[0] = "#23456700"; break;
                case "bad-color": source.palette[0] = "#zz4567"; break;
                case "null-pixels": source.pixels = null; break;
                case "few-pixels": source.pixels = source.pixels.Take(99).ToArray(); break;
                case "many-pixels": source.pixels = new SeedKeeperFlatSource.Pixel[5121]; break;
                case "null-pixel": source.pixels[0] = null; break;
                case "duplicate": source.pixels[1].x = source.pixels[0].x; break;
                case "left": source.pixels[0].x = -1; break;
                case "right": source.pixels[0].x = 64; break;
                case "top": source.pixels[0].y = -1; break;
                case "bottom": source.pixels[0].y = 80; break;
                case "negative-swatch": source.pixels[0].color = -1; break;
                case "missing-swatch": source.pixels[0].color = 1; break;
            }
            Assert.Throws<ArgumentException>(() => source.Validate());
        }
        [TestCase("vertex")][TestCase("swatch")][TestCase("winding")]
        public void LibraryRejectsGeometryThatNoLongerMatchesItsBlenderPixels(string mutation)
        {
            var source = SeedKeeperFlatLibrary.Load(); Assert.NotNull(source); source.Validate();
            var candidate = Object.Instantiate(source); var mesh = Object.Instantiate(source.Mesh); var prefab = Object.Instantiate(source.Prefab);
            try
            {
                candidate.Mesh = mesh; candidate.Prefab = prefab; prefab.GetComponent<MeshFilter>().sharedMesh = mesh;
                candidate.Validate();
                if (mutation == "vertex") { var values = mesh.vertices; values[0].x += .003f; mesh.vertices = values; mesh.RecalculateBounds(); }
                if (mutation == "swatch") { var values = mesh.uv; values[0].x += 1f / ((Texture2D)source.Material.GetTexture("_BaseMap")).width; mesh.uv = values; }
                if (mutation == "winding") { var values = mesh.triangles; int t = values[1]; values[1] = values[2]; values[2] = t; mesh.triangles = values; }
                Assert.Throws<InvalidOperationException>(() => candidate.Validate(), "Planarity/counts alone do not prove the submitted Blender art.");
                source.Validate();
            }
            finally { Object.DestroyImmediate(prefab); Object.DestroyImmediate(mesh); Object.DestroyImmediate(candidate); }
        }

        [TestCase("copied-mesh")][TestCase("changed-palette")]
        public void SubmittedKeeperCannotMasqueradeAsTheApprovedArt(string mutation)
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var owner = f.Add("SpreadSeedKeeper"); f.Refresh(); var root = f.View(owner);
                var filter = root.GetComponent<MeshFilter>(); var original = filter.sharedMesh;
                var renderer = root.GetComponent<MeshRenderer>(); var material = renderer.sharedMaterial;
                var color = material.GetColor("_BaseColor"); Mesh copied = null;
                var args = new object[] { owner, null }; Assert.True((bool)f.Call("TryGetApprovedStyle", args));
                try
                {
                    if (mutation == "copied-mesh") { copied = Object.Instantiate(original); filter.sharedMesh = copied; }
                    else material.SetColor("_BaseColor", Color.magenta);
                    Assert.False((bool)f.Call("TryGetApprovedStyle", args));
                    Assert.AreEqual(mutation == "copied-mesh" ? "submitted-mesh-mismatch" : "submitted-palette-mismatch", ((SpreadBiomeStyleEvidence)args[1]).Failure);
                }
                finally { filter.sharedMesh = original; material.SetColor("_BaseColor", color); if (copied != null) Object.DestroyImmediate(copied); }
                Assert.True((bool)f.Call("TryGetApprovedStyle", args));
            }
        }
    }
}
