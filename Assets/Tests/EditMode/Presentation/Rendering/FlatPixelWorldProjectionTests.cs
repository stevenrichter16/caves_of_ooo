using System;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelWorldProjectionTests
    {
        private GameObject _world, _cameraHost, _previousWorld;
        private Grid _grid;
        private Tilemap _main, _previous;
        private CameraFollow _follow;
        private FlatPixelWorldProjection _projection;
        private int _changes;
        private readonly Vector3 _originalSize = new Vector3(1f, 1f, 0f);
        private readonly Vector3 _pixelSize = new Vector3(1f, 1.5f, 0f);

        [SetUp]
        public void SetUp()
        {
            _world = new GameObject("Flat pixel projection world", typeof(Grid));
            _grid = _world.GetComponent<Grid>();
            _grid.cellSize = _originalSize;
            var mainHost = new GameObject("Main world", typeof(Tilemap));
            mainHost.transform.SetParent(_world.transform, false);
            _main = mainHost.GetComponent<Tilemap>();
            _previousWorld = new GameObject("Previous camera projection", typeof(Grid));
            var previousHost = new GameObject("Previous map", typeof(Tilemap));
            previousHost.transform.SetParent(_previousWorld.transform, false);
            _previous = previousHost.GetComponent<Tilemap>();
            _cameraHost = new GameObject("Flat pixel projection camera", typeof(Camera), typeof(CameraFollow));
            _follow = _cameraHost.GetComponent<CameraFollow>();
            _follow.WorldTilemap = _previous;
            _changes = 0;
        }

        [TearDown]
        public void TearDown()
        {
            _projection?.Dispose();
            if (_world != null) Object.DestroyImmediate(_world);
            if (_cameraHost != null) Object.DestroyImmediate(_cameraHost);
            if (_previousWorld != null) Object.DestroyImmediate(_previousWorld);
        }

        [Test]
        public void AdmissionAppliesRectangularWorld_AndInactiveCallPreservesOriginal()
        {
            CreateProjection();
            _projection.Apply(false, false);
            Assert.AreEqual(_originalSize, _grid.cellSize);
            Assert.AreSame(_previous, _follow.WorldTilemap);
            Assert.Zero(_changes);

            _projection.Apply(true, false);
            Assert.AreEqual(_pixelSize, _grid.cellSize);
            Assert.AreSame(_main, _follow.WorldTilemap);
            Assert.AreEqual(1, _changes);
        }

        [Test]
        public void FullscreenEntryRestoresOriginalBeforeReturn_AndReentryIsIdempotent()
        {
            CreateProjection();
            _projection.Apply(true, false);
            _projection.Apply(true, true);
            Assert.AreEqual(_originalSize, _grid.cellSize, "Shared UI must become square synchronously, before painting.");
            Assert.AreSame(_previous, _follow.WorldTilemap);
            Assert.AreEqual(2, _changes);

            _projection.Apply(true, true);
            Assert.AreEqual(2, _changes, "A nested inventory announcement must not churn the world cache.");
            _projection.Apply(true, false);
            Assert.AreEqual(_pixelSize, _grid.cellSize);
            Assert.AreSame(_main, _follow.WorldTilemap);
            Assert.AreEqual(3, _changes);
        }

        [Test]
        public void LeavingAdmissionRestoresCapturedState_AndSecondVisitReappliesIt()
        {
            CreateProjection();
            for (int visit = 0; visit < 2; visit++)
            {
                _projection.Apply(true, false);
                Assert.AreEqual(_pixelSize, _grid.cellSize);
                _projection.Apply(false, false);
                Assert.AreEqual(_originalSize, _grid.cellSize);
                Assert.AreSame(_previous, _follow.WorldTilemap);
            }
            Assert.AreEqual(4, _changes);
        }

        [Test]
        public void DisableWhileFullscreen_DoesNotRestorePixelSpacingOnClose()
        {
            CreateProjection();
            _projection.Apply(true, false);
            _projection.Apply(true, true);
            _projection.Apply(false, true);
            _projection.Apply(false, false);
            Assert.AreEqual(_originalSize, _grid.cellSize);
            Assert.AreSame(_previous, _follow.WorldTilemap);
            Assert.AreEqual(2, _changes);
        }

        [Test]
        public void RepeatedApplyAndCameraOnlyRebinding_DoNotInvalidateGeometry()
        {
            CreateProjection();
            _projection.Apply(true, false);
            _projection.Apply(true, false);
            _follow.WorldTilemap = null;
            _projection.Apply(true, false);
            Assert.AreSame(_main, _follow.WorldTilemap);
            Assert.AreEqual(1, _changes);

            _grid.cellSize = _originalSize;
            _projection.Apply(true, false);
            Assert.AreEqual(_pixelSize, _grid.cellSize);
            Assert.AreEqual(2, _changes, "An actual externally changed metric must invalidate the cache once.");
        }

        [Test]
        public void DisposeRestoresOnce_AndLaterCallsCannotReacquireWorld()
        {
            CreateProjection();
            _projection.Apply(true, false);
            _projection.Dispose();
            Assert.AreEqual(_originalSize, _grid.cellSize);
            Assert.AreSame(_previous, _follow.WorldTilemap);
            Assert.AreEqual(2, _changes);
            _projection.Dispose();
            _projection.Apply(true, false);
            Assert.AreEqual(_originalSize, _grid.cellSize);
            Assert.AreSame(_previous, _follow.WorldTilemap);
            Assert.AreEqual(2, _changes);
        }

        [Test]
        public void OriginalDepthIsPreserved_AndCallbackObservesCompletedProjection()
        {
            _grid.cellSize = new Vector3(1f, 1f, 2f);
            _projection = new FlatPixelWorldProjection(_main, _follow, () =>
            {
                _changes++;
                Assert.AreEqual(2f, _grid.cellSize.z);
                Assert.AreSame(_grid.cellSize.y == 1.5f ? _main : _previous, _follow.WorldTilemap);
            });
            _projection.Apply(true, false);
            Assert.AreEqual(new Vector3(1f, 1.5f, 2f), _grid.cellSize);
            _projection.Apply(false, false);
            Assert.AreEqual(new Vector3(1f, 1f, 2f), _grid.cellSize);
            Assert.AreEqual(2, _changes);
        }

        [Test]
        public void DestroyedGridDoesNotPreventRestoringBorrowedCamera()
        {
            CreateProjection();
            _projection.Apply(true, false);
            Object.DestroyImmediate(_world);
            Assert.DoesNotThrow(() => _projection.Dispose());
            Assert.AreSame(_previous, _follow.WorldTilemap);
            Assert.AreEqual(1, _changes, "Destroyed metrics cannot change or require a cache callback.");
        }

        [Test]
        public void DestroyedCameraDoesNotPreventRestoringBorrowedGrid()
        {
            CreateProjection();
            _projection.Apply(true, false);
            Object.DestroyImmediate(_cameraHost);
            Assert.DoesNotThrow(() => _projection.Dispose());
            Assert.AreEqual(_originalSize, _grid.cellSize);
            Assert.AreEqual(2, _changes);
        }

        [Test]
        public void CapturedNondefaultMetricsAreRestored_WithoutHardcodingUnitCells()
        {
            var captured = new Vector3(2f, 0.75f, 0f);
            _grid.cellSize = captured;
            CreateProjection();
            _projection.Apply(true, false);
            Assert.AreEqual(_pixelSize, _grid.cellSize);
            _projection.Apply(true, true);
            Assert.AreEqual(captured, _grid.cellSize);
        }

        [Test]
        public void OptionalCameraAndCallbackPermitProjectionOnlyUse()
        {
            _projection = new FlatPixelWorldProjection(_main, null, null);
            _projection.Apply(true, false);
            Assert.AreEqual(_pixelSize, _grid.cellSize);
            _projection.Dispose();
            Assert.AreEqual(_originalSize, _grid.cellSize);
            Assert.AreSame(_previous, _follow.WorldTilemap, "The unbound camera remains untouched.");
        }

        [Test]
        public void MissingMapOrGridFailsBeforeMutatingBorrowedCamera()
        {
            Assert.Throws<ArgumentNullException>(() => new FlatPixelWorldProjection(null, _follow, null));
            _main.transform.SetParent(null, false);
            try
            {
                Assert.Throws<ArgumentException>(() => new FlatPixelWorldProjection(_main, _follow, null));
                Assert.AreSame(_previous, _follow.WorldTilemap);
            }
            finally { _main.transform.SetParent(_world.transform, false); }
        }

        private void CreateProjection()
            => _projection = new FlatPixelWorldProjection(_main, _follow, () => _changes++);
    }
}
