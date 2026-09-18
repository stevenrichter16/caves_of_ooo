using System;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class Village3DProjectionTests
    {
        [TestCase(0, 0)] [TestCase(79, 24)] [TestCase(40, 12)]
        [TestCase(21, 0)] [TestCase(58, 24)] [TestCase(40, 24)]
        public void CellCentreRoundTripsWithoutChangingTheLogicalGrid(int x, int y)
        {
            Vector3 p = Village3DProjection.CellCentre(x, y, 1.25f);
            Assert.AreEqual(x + .5f, p.x);
            Assert.AreEqual(1.25f, p.y);
            Assert.AreEqual(24.5f - y, p.z);
            Assert.IsTrue(Village3DProjection.TryWorldToCell(p, out int rx, out int ry));
            Assert.AreEqual(x, rx); Assert.AreEqual(y, ry);
        }

        [TestCase(-1, 0)] [TestCase(80, 0)] [TestCase(0, -1)] [TestCase(0, 25)]
        public void InvalidCellCannotBecomeAValidVisualAnchor(int x, int y)
            => Assert.Throws<ArgumentOutOfRangeException>(() => Village3DProjection.CellCentre(x, y));

        [TestCase(-.01f, 1f)] [TestCase(80f, 1f)]
        [TestCase(1f, -.01f)] [TestCase(1f, 25f)]
        [TestCase(float.NaN, 1f)] [TestCase(1f, float.PositiveInfinity)]
        public void OutsideAndNonfiniteWorldCoordinatesDoNotAliasTheMap(float x, float z)
        {
            Assert.IsFalse(Village3DProjection.TryWorldToCell(new Vector3(x, 0, z), out int cx, out int cy));
            Assert.AreEqual(-1, cx); Assert.AreEqual(-1, cy);
        }

        [TestCase(0f, 0f, 0, 24)]
        [TestCase(79.999f, 24.999f, 79, 0)]
        [TestCase(40f, 12f, 40, 12)]
        public void GroundCellEdgesFollowTheExistingXYTilemapConvention(float x, float z, int cx, int cy)
        {
            Assert.IsTrue(Village3DProjection.TryWorldToCell(new Vector3(x, 9, z), out int rx, out int ry));
            Assert.AreEqual(cx, rx); Assert.AreEqual(cy, ry);
        }

        [TestCase(0.5f)] [TestCase(1f)] [TestCase(1.5f)] [TestCase(2.5f)]
        public void OverheadCameraProjectionMatchesExistingMapAndRespectsViewport(float aspect)
        {
            var host = new GameObject("Village 3D projection control");
            try
            {
                var camera = host.AddComponent<Camera>();
                camera.orthographic = true; camera.orthographicSize = 13f;
                camera.transform.position = new Vector3(40, 12.5f, -10);
                camera.aspect = aspect;
                Rect rect = new Rect(80, 120, 780 * aspect, 780);
                var cellPoint = new Vector3(40.5f, 12.5f, 0);
                Vector3 uv = camera.WorldToViewportPoint(cellPoint);
                var screen = new Vector2(rect.x + uv.x * rect.width, rect.y + uv.y * rect.height);
                Assert.IsTrue(Village3DProjection.TryScreenToCell(screen, rect, camera.transform.position,
                    camera.orthographicSize, out int x, out int y));
                Assert.AreEqual(40, x); Assert.AreEqual(12, y);
                Assert.IsFalse(Village3DProjection.TryScreenToCell(new Vector2(rect.xMax, rect.center.y),
                    rect, camera.transform.position, camera.orthographicSize, out _, out _), "Sidebar begins at the exclusive right edge.");
                Assert.IsFalse(Village3DProjection.TryScreenToCell(new Vector2(rect.center.x, rect.yMin - 1),
                    rect, camera.transform.position, camera.orthographicSize, out _, out _), "Hotbar is not world input.");
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }

        [TestCase(0f, 780f, 13f)] [TestCase(1000f, 0f, 13f)]
        [TestCase(1000f, 780f, 0f)] [TestCase(1000f, 780f, float.NaN)]
        public void InvalidViewportAndZoomCannotGenerateCommands(float width, float height, float size)
            => Assert.IsFalse(Village3DProjection.TryScreenToCell(new Vector2(50, 50), new Rect(0, 0, width, height),
                new Vector3(40, 12.5f, -10), size, out _, out _));

        [Test]
        public void HeightDoesNotMoveTheGroundSelectionButInvalidHeightIsRefused()
        {
            Assert.IsTrue(Village3DProjection.TryWorldToCell(new Vector3(40.5f, 100, 12.5f), out int x, out int y));
            Assert.AreEqual(40, x); Assert.AreEqual(12, y);
            Assert.IsFalse(Village3DProjection.TryWorldToCell(new Vector3(40.5f, float.NaN, 12.5f), out _, out _));
        }
    }

    public sealed class Village3DVisibilityTests
    {
        [TestCase(false, false, 0f)] [TestCase(true, false, .5f)] [TestCase(true, true, 1f)]
        public void ShaderDataDistinguishesUnseenRememberedAndVisible(bool explored, bool visible, float alpha)
        {
            var zone = new Zone(); var cell = zone.GetCell(4, 5);
            cell.Explored = explored; cell.IsVisible = visible;
            Color actual = Village3DVisibility.SampleCell(cell, null, false);
            Assert.AreEqual(alpha, actual.a);
            if (!explored) Assert.AreEqual(Color.clear, actual);
        }

        [Test]
        public void LightMapAffectsVisibleGeometryAndAmbientChangesAreObservable()
        {
            var zone = new Zone(); var cell = zone.GetCell(4, 5);
            cell.Explored = cell.IsVisible = true;
            var light = new LightMap(); zone.AmbientLevel = .2f; light.Compute(zone);
            Color dim = Village3DVisibility.SampleCell(cell, light, false);
            zone.AmbientLevel = .8f; light.Compute(zone);
            Color bright = Village3DVisibility.SampleCell(cell, light, false);
            Assert.Greater(bright.r, dim.r + .4f);
            Assert.AreEqual(1f, dim.a); Assert.AreEqual(1f, bright.a);
        }

        [Test]
        public void MovingLightOutsideCurrentSightDoesNotRevealRememberedState()
        {
            var zone = new Zone(); var cell = zone.GetCell(4, 5);
            cell.Explored = true; cell.IsVisible = false;
            var light = new LightMap(); light.Compute(zone);
            Color before = Village3DVisibility.SampleCell(cell, light, false);
            var lamp = new Entity(); lamp.AddPart(new LightSourcePart { Radius = 5, Intensity = 1f });
            zone.AddEntity(lamp, 4, 5); light.Compute(zone);
            Assert.AreEqual(before, Village3DVisibility.SampleCell(cell, light, false));
            cell.IsVisible = true;
            Assert.AreNotEqual(before, Village3DVisibility.SampleCell(cell, light, false));
        }

        [Test]
        public void ShowcaseRevealDoesNotMutateExplorationOrVisibility()
        {
            var zone = new Zone(); var cell = zone.GetCell(4, 5);
            Assert.AreEqual(1, Village3DVisibility.SampleCell(cell, null, true).a);
            Assert.IsFalse(cell.Explored); Assert.IsFalse(cell.IsVisible);
            Assert.AreEqual(Color.clear, Village3DVisibility.SampleCell(cell, null, false));
            Assert.AreEqual(Color.clear, Village3DVisibility.SampleCell(null, null, true));
        }
    }
}
