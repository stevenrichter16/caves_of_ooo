using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class FellingScenePresenterTests
    {
        [TestCase(0, 0, 16, 32)]
        [TestCase(1536, 1024, 64, 0)]
        [TestCase(768, 224, 40, 25)]
        public void ImageCoordinatesPreserveNativeScale(float x, float y, float wx, float wy)
        {
            Vector2 world = FellingScenePresenter.ImageToWorld(new Vector2(x, y));
            Assert.AreEqual(new Vector2(wx, wy), world);
            Assert.AreEqual(new Vector2(x, y), FellingScenePresenter.WorldToImage(world));
        }

        [TestCase(0, 224, true, 16, 0)]
        [TestCase(1535, 1023, true, 63, 24)]
        [TestCase(768, 223, false, -1, -1)]
        [TestCase(1536, 500, false, -1, -1)]
        [TestCase(-1, 500, false, -1, -1)]
        [TestCase(500, 1024, false, -1, -1)]
        public void GroundPickingRejectsOverhangAndOutsideSamples(float x, float y, bool valid, int cx, int cy)
        {
            Assert.AreEqual(valid, FellingScenePresenter.TryImageToCell(new Vector2(x, y), out int ax, out int ay));
            Assert.AreEqual(cx, ax);
            Assert.AreEqual(cy, ay);
        }

        [Test]
        public void NonfinitePickingNeverAliasesGround()
        {
            Assert.IsFalse(FellingScenePresenter.TryImageToCell(new Vector2(float.NaN, 300), out _, out _));
            Assert.IsFalse(FellingScenePresenter.TryImageToCell(new Vector2(300, float.PositiveInfinity), out _, out _));
        }

        [Test]
        public void OverhangUsesItsOwnColumnBoundaryRatherThanOneTrunkAnchor()
        {
            int[] rows = new int[48];
            rows[0] = 4; rows[47] = 9;
            Assert.AreEqual(new Vector2Int(16, 4), FellingScenePresenter.FogOwnerForImageCell(0, 0, rows));
            Assert.AreEqual(new Vector2Int(63, 9), FellingScenePresenter.FogOwnerForImageCell(47, 6, rows));
            Assert.AreEqual(new Vector2Int(16, 4), FellingScenePresenter.FogOwnerForImageCell(0, 7, rows));
            Assert.AreEqual(new Vector2Int(16, 4), FellingScenePresenter.FogOwnerForImageCell(0, 11, rows));
            Assert.AreEqual(new Vector2Int(16, 5), FellingScenePresenter.FogOwnerForImageCell(0, 12, rows));
            Assert.AreEqual(new Vector2Int(63, 24), FellingScenePresenter.FogOwnerForImageCell(47, 31, rows));
        }

        [Test]
        public void FootDepthAgreesWithActorFeetAndOrdersFrontObjectsCloser()
        {
            Assert.AreEqual(-20 * 0.001f, FellingScenePresenter.DepthForFootPixel(224 + 21 * 32), 0.000001f);
            Assert.Less(FellingScenePresenter.DepthForFootPixel(960), FellingScenePresenter.DepthForFootPixel(900));
        }

        [TestCase(1.5f, 16f)]
        [TestCase(1f, 24f)]
        [TestCase(2f, 16f)]
        public void SceneCameraFitsBothDimensionsInMapViewport(float aspect, float expected)
        {
            Assert.AreEqual(expected, FellingScenePresenter.CameraHalfHeight(aspect), 0.0001f);
            float halfHeight = FellingScenePresenter.CameraHalfHeight(aspect);
            Assert.GreaterOrEqual(halfHeight, 16);
            Assert.GreaterOrEqual(halfHeight * aspect, 24);
        }

        [Test]
        public void InvalidAspectFallsBackToFiniteReferenceFraming()
        {
            Assert.AreEqual(16, FellingScenePresenter.CameraHalfHeight(0));
            Assert.AreEqual(16, FellingScenePresenter.CameraHalfHeight(float.NaN));
        }

        [Test]
        public void UnboundPresenterDoesNotClaimOrPickOrdinaryZone()
        {
            var go = new GameObject("FellingPresenterTest");
            try
            {
                var presenter = go.AddComponent<FellingScenePresenter>();
                Assert.IsFalse(presenter.ClaimsCell(40, 12));
                Assert.IsFalse(presenter.TryPickImage(new Vector2(768, 500), out _, out _));
                presenter.Bind(new Zone("ordinary"));
                Assert.IsFalse(presenter.IsReady);
                Assert.IsFalse(presenter.ClaimsCell(40, 12));
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
