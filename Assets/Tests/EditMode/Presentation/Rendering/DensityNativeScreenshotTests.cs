using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Owned capture resources, actual GPU color readback and error
    /// cleanup. The real ScreenCapture backend also needs the paired Play probe.</summary>
    public sealed class DensityNativeScreenshotTests
    {
        private string _directory;
        private RenderTexture _previous, _borrowed;

        [SetUp]
        public void Setup()
        {
            _directory = Path.Combine(Path.GetTempPath(), "coo-density-capture-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _previous = RenderTexture.active;
            _borrowed = new RenderTexture(8, 8, 0); _borrowed.Create();
            RenderTexture.active = _borrowed;
        }

        [TearDown]
        public void Cleanup()
        {
            RenderTexture.active = _previous;
            _borrowed.Release(); Object.DestroyImmediate(_borrowed);
            Directory.Delete(_directory, true);
        }

        private static void Capture(string path, int width, int height, Action<RenderTexture> capture)
        {
            var type = typeof(CavesOfOoo.Scenarios.Custom.DensityPhase1BenchPlayer).Assembly
                .GetType("CavesOfOoo.Scenarios.Custom.DensityNativeScreenshot");
            Assert.NotNull(type, "An owned color-only capture path must replace the failing file-oriented screenshot API.");
            var method = type.GetMethod("CaptureToFile", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(string), typeof(int), typeof(int), typeof(Action<RenderTexture>) }, null);
            Assert.NotNull(method);
            try { method.Invoke(null, new object[] { path, width, height, capture }); }
            catch (TargetInvocationException error) { throw error.InnerException ?? error; }
        }

        private static int OwnedTextureCount() => Resources.FindObjectsOfTypeAll<Texture>()
            .Count(texture => texture.name.StartsWith("Density screenshot ", StringComparison.Ordinal));

        [TestCase(false)]
        [TestCase(true)]
        public void CaptureWritesActualPixels_WithoutDepthOrMemorylessStorage_AndRestoresBorrowedTarget(bool repeat)
        {
            int before = OwnedTextureCount(), calls = 0;
            RenderTexture owned = null;
            string path = Path.Combine(_directory, "capture.png");
            Action<RenderTexture> capture = target =>
            {
                owned = target; calls++;
                Assert.AreEqual(0, target.depth);
                Assert.AreEqual(RenderTextureMemoryless.None, target.memorylessMode);
                Assert.AreEqual(1, target.antiAliasing);
                Assert.IsTrue(target.IsCreated());
                RenderTexture.active = target;
                GL.Clear(false, true, repeat ? Color.blue : Color.green);
            };
            Capture(path, 24, 16, capture);
            if (repeat) Capture(path, 24, 16, capture);
            Assert.AreEqual(repeat ? 2 : 1, calls);
            Assert.AreSame(_borrowed, RenderTexture.active);
            Assert.IsTrue(_borrowed.IsCreated()); Assert.IsTrue(owned == null, "Owned target must be destroyed in Edit mode");
            Assert.AreEqual(before, OwnedTextureCount());
            var pixels = new Texture2D(1, 1);
            try
            {
                Assert.IsTrue(pixels.LoadImage(File.ReadAllBytes(path)));
                Assert.AreEqual(24, pixels.width); Assert.AreEqual(16, pixels.height);
                foreach (var color in pixels.GetPixels32())
                    Assert.AreEqual(repeat ? new Color32(0, 0, 255, 255) : new Color32(0, 255, 0, 255), color);
            }
            finally { Object.DestroyImmediate(pixels); }
        }

        [Test]
        public void CaptureFailureReleasesOwnedTarget_RestoresBorrowedTarget_AndWritesNoImage()
        {
            int before = OwnedTextureCount(); RenderTexture owned = null;
            string path = Path.Combine(_directory, "capture.png");
            var failure = Assert.Throws<InvalidOperationException>(() => Capture(path, 24, 16, target =>
            {
                owned = target; RenderTexture.active = target;
                throw new InvalidOperationException("deliberate capture failure");
            }));
            StringAssert.Contains("deliberate capture failure", failure.Message);
            Assert.AreSame(_borrowed, RenderTexture.active); Assert.IsTrue(owned == null);
            Assert.AreEqual(before, OwnedTextureCount()); Assert.IsFalse(File.Exists(path));
        }

        [Test]
        public void FileFailureReleasesReadbackAndTarget_AndPreservesBorrowedTarget()
        {
            int before = OwnedTextureCount(); RenderTexture owned = null;
            Assert.Throws<DirectoryNotFoundException>(() => Capture(Path.Combine(_directory, "missing", "capture.png"), 24, 16, target =>
            {
                owned = target; RenderTexture.active = target; GL.Clear(false, true, Color.green);
            }));
            Assert.AreSame(_borrowed, RenderTexture.active); Assert.IsTrue(owned == null);
            Assert.AreEqual(before, OwnedTextureCount()); Assert.IsTrue(_borrowed.IsCreated());
        }

        [TestCase(0, 16)]
        [TestCase(24, 0)]
        [TestCase(-1, 16)]
        public void InvalidDimensionsRejectBeforeCaptureAndLeaveBorrowedTargetIntact(int width, int height)
        {
            int calls = 0, before = OwnedTextureCount();
            Assert.Throws<ArgumentOutOfRangeException>(() => Capture(Path.Combine(_directory, "capture.png"), width, height, target => calls++));
            Assert.AreEqual(0, calls); Assert.AreEqual(before, OwnedTextureCount());
            Assert.AreSame(_borrowed, RenderTexture.active);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ScreenRowOrderFollowsGraphicsOrigin_AndPreservesEveryPixel(bool graphicsUVStartsAtTop)
        {
            var pixels = new Texture2D(3, 2, TextureFormat.RGBA32, false, false);
            var source = new[]
            {
                new Color32(1, 2, 3, 4), new Color32(5, 6, 7, 8), new Color32(9, 10, 11, 12),
                new Color32(13, 14, 15, 16), new Color32(17, 18, 19, 20), new Color32(21, 22, 23, 24)
            };
            try
            {
                pixels.SetPixels32(source); pixels.Apply(false, false);
                var type = typeof(CavesOfOoo.Scenarios.Custom.DensityPhase1BenchPlayer).Assembly
                    .GetType("CavesOfOoo.Scenarios.Custom.DensityNativeScreenshot");
                var method = type?.GetMethod("RestoreScreenRowOrder", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.NotNull(method, "Screen capture must account for backend row origin, not unconditionally flip every image.");
                method.Invoke(null, new object[] { pixels, graphicsUVStartsAtTop });
                var actual = pixels.GetPixels32();
                for (int y = 0; y < 2; y++)
                    for (int x = 0; x < 3; x++)
                        Assert.AreEqual(source[(graphicsUVStartsAtTop ? 1 - y : y) * 3 + x], actual[y * 3 + x]);
                Assert.AreEqual(3, pixels.width); Assert.AreEqual(2, pixels.height);
            }
            finally { Object.DestroyImmediate(pixels); }
        }
    }
}
