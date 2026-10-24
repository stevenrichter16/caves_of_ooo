using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Borrows the shared main grid and camera while the flat pixel presentation
    /// owns the zone. Fullscreen pages temporarily restore the captured grid;
    /// leaving the mode or disposing restores the borrowed metrics, projection
    /// source and native-pixel framing setting.
    /// </summary>
    public sealed class FlatPixelWorldProjection : IDisposable
    {
        private readonly Tilemap _main;
        private readonly Grid _grid;
        private readonly CameraFollow _follow;
        private readonly Tilemap _previousCameraMap;
        private readonly int _previousPixelsPerUnit;
        private readonly Vector3 _originalCellSize;
        private readonly Action _onMetricsChanged;
        private bool _disposed;

        /// <summary>
        /// Captures the main tilemap's Grid metrics and the camera's existing
        /// projection without changing them. A missing camera is permitted;
        /// the main tilemap must belong to an existing Grid.
        /// </summary>
        public FlatPixelWorldProjection(Tilemap main, CameraFollow follow, Action onMetricsChanged)
        {
            if (main == null) throw new ArgumentNullException(nameof(main));
            _grid = main.GetComponentInParent<Grid>();
            if (_grid == null) throw new ArgumentException("The world tilemap requires a parent Grid.", nameof(main));
            _main = main;
            _follow = follow;
            _originalCellSize = _grid.cellSize;
            _previousCameraMap = follow != null ? follow.WorldTilemap : null;
            _previousPixelsPerUnit = follow != null ? follow.WorldPixelsPerUnit : 0;
            _onMetricsChanged = onMetricsChanged;
        }

        /// <summary>
        /// Applies geometry synchronously, before painting or framing the next
        /// view. Enabled flat gameplay uses 1-by-1.5 cells in every world zone;
        /// fullscreen pages and disabled presentation use the captured layout.
        /// Native regional art admission does not affect geometry. The callback runs
        /// after an actual cell-size change and completed camera binding only.
        /// Calls after disposal are harmless.
        /// </summary>
        public void Apply(bool enabled, bool fullscreen)
        {
            if (_disposed) return;
            ApplyWorld(enabled && !fullscreen && _main != null && _grid != null);
        }

        private void ApplyWorld(bool pixelWorld)
        {
            if (_follow != null)
            {
                _follow.WorldTilemap = pixelWorld ? _main : _previousCameraMap;
                _follow.WorldPixelsPerUnit = pixelWorld ? 16 : _previousPixelsPerUnit;
            }

            if (_grid == null) return;
            Vector3 desired = pixelWorld
                ? new Vector3(1f, 1.5f, _originalCellSize.z)
                : _originalCellSize;
            if (_grid.cellSize == desired) return;
            _grid.cellSize = desired;
            _onMetricsChanged?.Invoke();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            ApplyWorld(false);
        }
    }
}
