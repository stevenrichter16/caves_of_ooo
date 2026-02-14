using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Zoomed-in camera that follows the player within a zone.
    /// Clamps to zone bounds so no area outside the zone is visible.
    /// On zone transitions, snaps to the player's arrival position.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public Entity Player { get; set; }
        public Zone CurrentZone { get; set; }

        /// <summary>
        /// Orthographic half-height. Smaller = more zoomed in.
        /// Default 10 shows ~36x20 tiles on a 16:9 screen.
        /// </summary>
        public float ZoomSize = 17f;

        private Camera _camera;
        private bool _paused;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        /// <summary>
        /// Set up zoom and snap camera to player. Called on setup
        /// and after each zone transition.
        /// </summary>
        public void SnapToPlayer()
        {
            if (_camera == null)
                _camera = GetComponent<Camera>();
            if (_camera == null)
                return;

            _camera.orthographicSize = ZoomSize;
            _camera.backgroundColor = new Color(0.05f, 0.05f, 0.05f);

            FollowPlayer();
        }

        /// <summary>
        /// Position and zoom the camera to show a full 80x25 tile grid for UI overlays.
        /// Centers on the grid and calculates orthographic size based on aspect ratio
        /// so all tiles are visible regardless of screen dimensions.
        /// </summary>
        public void SetUIView(int gridWidth, int gridHeight)
        {
            if (_camera == null)
                _camera = GetComponent<Camera>();
            if (_camera == null)
                return;

            _paused = true;

            // Center camera on the grid (tiles span [0, gridSize), center at gridSize/2)
            float centerX = gridWidth * 0.5f;
            float centerY = gridHeight * 0.5f;
            transform.position = new Vector3(centerX, centerY, transform.position.z);

            // Calculate orthographic size to fit both dimensions
            float halfHeight = gridHeight * 0.5f;
            float halfWidth = gridWidth * 0.5f;
            float sizeForHeight = halfHeight;
            float sizeForWidth = halfWidth / _camera.aspect;

            // Small padding prevents edge tiles from bleeding off-screen
            // due to aspect ratio rounding or sub-pixel alignment
            _camera.orthographicSize = Mathf.Max(sizeForHeight, sizeForWidth) + 0.15f;
        }

        /// <summary>
        /// Restore normal camera follow behavior after UI overlay closes.
        /// </summary>
        public void RestoreGameView()
        {
            _paused = false;
            SnapToPlayer();
        }

        private void LateUpdate()
        {
            if (_paused) return;
            FollowPlayer();
        }

        private void FollowPlayer()
        {
            if (Player == null || CurrentZone == null || _camera == null)
                return;

            var pos = CurrentZone.GetEntityPosition(Player);
            if (pos.x < 0) return;

            // Convert roguelike coords (0,0 = top-left) to world coords (Y-inverted)
            float targetX = pos.x;
            float targetY = Zone.Height - 1 - pos.y;

            // Clamp so camera edges stay within zone tile bounds
            float halfH = _camera.orthographicSize;
            float halfW = halfH * _camera.aspect;

            float minX = halfW - 0.5f;
            float maxX = (Zone.Width - 1) + 0.5f - halfW;
            float minY = halfH - 0.5f;
            float maxY = (Zone.Height - 1) + 0.5f - halfH;

            // If zone fits entirely within camera view on an axis, center it
            float clampedX = minX <= maxX ? Mathf.Clamp(targetX, minX, maxX) : (Zone.Width - 1) * 0.5f;
            float clampedY = minY <= maxY ? Mathf.Clamp(targetY, minY, maxY) : (Zone.Height - 1) * 0.5f;

            transform.position = new Vector3(clampedX, clampedY, transform.position.z);
        }
    }
}
