using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Positions the camera to show the entire 80x25 zone at once,
    /// matching Caves of Qud's fixed-screen display. The camera never
    /// scrolls — it snaps to show the full zone, and on zone transition
    /// the display hard-cuts to the new zone.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public Entity Player { get; set; }
        public Zone CurrentZone { get; set; }

        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        /// <summary>
        /// Fit the camera to show the entire zone. Called once on setup
        /// and again on each zone transition.
        /// </summary>
        public void SnapToPlayer()
        {
            if (_camera == null)
                _camera = GetComponent<Camera>();
            if (_camera == null)
                return;

            // Center camera on the zone (using ZoneRenderer's Y-inversion)
            float centerX = (Zone.Width - 1) * 0.5f;
            float centerY = (Zone.Height - 1) * 0.5f;
            transform.position = new Vector3(centerX, centerY, transform.position.z);

            // Set orthographic size to fit the entire zone on screen
            // Need to fit both dimensions: pick whichever requires a larger size
            float sizeForHeight = Zone.Height * 0.5f;
            float sizeForWidth = Zone.Width * 0.5f / _camera.aspect;
            _camera.orthographicSize = Mathf.Max(sizeForHeight, sizeForWidth);
        }
    }
}
