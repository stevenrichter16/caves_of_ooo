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
        public Camera SidebarCamera { get; set; }
        public Camera HotbarCamera { get; set; }
        public Camera PopupOverlayCamera { get; set; }
        public bool HasOverrideTarget { get; private set; }
        public Vector2Int OverrideZoneCell { get; private set; }
        public float OverrideViewportMarginFraction = 0.25f;

        /// <summary>
        /// Desired number of world tiles visible vertically at normal gameplay zoom.
        /// Orthographic size is derived from this so default zoom is content-driven
        /// instead of relying on a raw magic number.
        /// </summary>
        public int TargetVisibleTileRows = 34;

        /// <summary>
        /// Legacy fallback if TargetVisibleTileRows is disabled or invalid.
        /// </summary>
        public float ZoomSize = 17f;
        public int ReservedSidebarWidthChars = 34;
        public float SidebarReferenceZoom = 20f;
        public int ReservedHotbarHeightRows = GameplayViewportLayout.DefaultHotbarRows;

        private Camera _camera;
        private bool _paused;
        private bool _centeredPopupOverlayActive;

        // Screen shake state
        private float _shakeTimeRemaining;
        private float _shakeIntensity;

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

            _camera.orthographicSize = GetGameplayZoomSize();
            _camera.backgroundColor = new Color(0.05f, 0.05f, 0.05f);
            ApplyGameplayLayout();

            FollowTrackedTarget();
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
            _centeredPopupOverlayActive = false;
            ApplyUIViewLayout();

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
        /// Expand the gameplay camera to fullscreen for centered modal overlays
        /// without changing the current world framing. This hides the sidebar
        /// while keeping the map positioned and zoomed exactly as-is underneath.
        /// </summary>
        public void SetFullscreenOverlayView()
        {
            if (_camera == null)
                _camera = GetComponent<Camera>();
            if (_camera == null)
                return;

            _paused = true;
            _centeredPopupOverlayActive = false;
            ApplyUIViewLayout();
        }

        /// <summary>
        /// Keep the split gameplay/sidebar layout intact and enable the popup
        /// overlay camera over the gameplay column.
        /// </summary>
        public void SetCenteredPopupOverlayView()
        {
            if (_camera == null)
                _camera = GetComponent<Camera>();
            if (_camera == null)
                return;

            _paused = true;
            _centeredPopupOverlayActive = true;
            ApplyGameplayLayout();
        }

        /// <summary>
        /// Enable the popup overlay camera on top of an already-established
        /// fullscreen UI view (e.g. the inventory). Unlike
        /// <see cref="SetCenteredPopupOverlayView"/>, this does NOT flip the
        /// main camera to the cropped gameplay layout — the UI view is left
        /// alone so the inventory underneath stays fullscreen instead of
        /// shrinking into black strips on the right and bottom where the
        /// sidebar and hotbar would normally sit.
        ///
        /// Used by the announcement flow when a popup fires from inside
        /// the inventory (e.g. reading a grimoire).
        /// </summary>
        public void SetCenteredPopupOverlayOverUIView()
        {
            if (_camera == null)
                _camera = GetComponent<Camera>();
            if (_camera == null || PopupOverlayCamera == null)
                return;

            // Keep the pause flag so the camera doesn't auto-follow the
            // player, but leave _centeredPopupOverlayActive false —
            // otherwise LateUpdate would reapply ApplyGameplayLayout every
            // frame and overwrite the UI view we're trying to preserve.
            _paused = true;
            _centeredPopupOverlayActive = false;

            // Configure the popup overlay camera to cover the entire
            // screen at the display's real aspect (not the cropped
            // gameplay aspect).
            float displayAspect = GameplayViewportLayout.GetDisplayAspect(_camera);
            CenteredPopupLayout.ConfigureOverlayCamera(
                PopupOverlayCamera,
                new Rect(0f, 0f, 1f, 1f),
                displayAspect);
            PopupOverlayCamera.cullingMask = GameplayRenderLayers.PopupOverlayMask;
            PopupOverlayCamera.clearFlags = CameraClearFlags.Depth;
            PopupOverlayCamera.backgroundColor = Color.clear;
            PopupOverlayCamera.enabled = true;
        }

        /// <summary>
        /// Restore normal camera follow behavior after UI overlay closes.
        /// </summary>
        public void RestoreGameView()
        {
            _paused = false;
            _centeredPopupOverlayActive = false;
            if (PopupOverlayCamera != null)
                PopupOverlayCamera.enabled = false;
            SnapToPlayer();
        }

        public void SetOverrideTargetCell(int x, int y)
        {
            OverrideZoneCell = new Vector2Int(x, y);
            HasOverrideTarget = true;

            if (!_paused)
                FollowTrackedTarget();
        }

        public void ClearOverrideTarget()
        {
            HasOverrideTarget = false;
        }

        /// <summary>
        /// Trigger a screen shake effect. Intensity is the max pixel offset,
        /// duration is in seconds.
        /// </summary>
        public void Shake(float intensity = 0.15f, float duration = 0.15f)
        {
            _shakeIntensity = intensity;
            _shakeTimeRemaining = duration;
        }

        private void LateUpdate()
        {
            if (_paused)
            {
                if (_centeredPopupOverlayActive)
                    ApplyGameplayLayout();
                return;
            }
            ApplyGameplayLayout();
            FollowTrackedTarget();

            // Apply screen shake offset after positioning
            if (_shakeTimeRemaining > 0f)
            {
                float t = _shakeTimeRemaining; // use as a simple seed
                float offsetX = (Mathf.PerlinNoise(t * 40f, 0f) - 0.5f) * 2f * _shakeIntensity;
                float offsetY = (Mathf.PerlinNoise(0f, t * 40f) - 0.5f) * 2f * _shakeIntensity;
                transform.position += new Vector3(offsetX, offsetY, 0f);
                _shakeTimeRemaining -= Time.deltaTime;
            }
        }

        private void FollowTrackedTarget()
        {
            if (CurrentZone == null || _camera == null)
                return;

            int zoneX;
            int zoneY;
            bool useOverride = HasOverrideTarget;

            if (useOverride)
            {
                zoneX = OverrideZoneCell.x;
                zoneY = OverrideZoneCell.y;
            }
            else
            {
                if (Player == null)
                    return;

                var pos = CurrentZone.GetEntityPosition(Player);
                if (pos.x < 0)
                    return;

                zoneX = pos.x;
                zoneY = pos.y;
            }

            float halfH = _camera.orthographicSize;
            float halfW = halfH * _camera.aspect;
            float targetX = zoneX + 0.5f;
            float targetY = Zone.Height - zoneY - 0.5f;
            float desiredX = targetX;
            float desiredY = targetY;

            if (useOverride)
            {
                float marginFraction = Mathf.Clamp01(OverrideViewportMarginFraction);
                float marginX = halfW * marginFraction;
                float marginY = halfH * marginFraction;
                float currentX = transform.position.x;
                float currentY = transform.position.y;

                float left = currentX - halfW + marginX;
                float right = currentX + halfW - marginX;
                float bottom = currentY - halfH + marginY;
                float top = currentY + halfH - marginY;

                desiredX = currentX;
                desiredY = currentY;

                if (targetX < left)
                    desiredX -= left - targetX;
                else if (targetX > right)
                    desiredX += targetX - right;

                if (targetY < bottom)
                    desiredY -= bottom - targetY;
                else if (targetY > top)
                    desiredY += targetY - top;
            }

            float minX = halfW - 0.5f;
            float maxX = (Zone.Width - 1) + 0.5f - halfW;
            float minY = halfH - 0.5f;
            float maxY = (Zone.Height - 1) + 0.5f - halfH;

            // If zone fits entirely within camera view on an axis, center it
            float clampedX = minX <= maxX ? Mathf.Clamp(desiredX, minX, maxX) : Zone.Width * 0.5f;
            float clampedY = minY <= maxY ? Mathf.Clamp(desiredY, minY, maxY) : Zone.Height * 0.5f;

            transform.position = new Vector3(clampedX, clampedY, transform.position.z);
        }

        private float GetGameplayZoomSize()
        {
            if (TargetVisibleTileRows > 0)
                return TargetVisibleTileRows * 0.5f;

            return ZoomSize;
        }

        private void ApplyGameplayLayout()
        {
            if (_camera == null)
                return;

            GameplayScreenLayout layout = GameplayViewportLayout.Measure(
                _camera,
                SidebarReferenceZoom,
                ReservedSidebarWidthChars,
                ReservedHotbarHeightRows);

            ConfigureCameraRect(_camera, layout.MapRect, layout.MapAspect);
            _camera.orthographic = true;
            _camera.cullingMask = GameplayRenderLayers.GameplayCameraMask;

            ConfigureSidebarCamera(layout);
            ConfigureHotbarCamera(layout);
            ConfigurePopupOverlayCamera(layout);
        }

        private void ApplyUIViewLayout()
        {
            if (_camera == null)
                return;

            float displayAspect = GameplayViewportLayout.GetDisplayAspect(_camera);
            ConfigureCameraRect(_camera, new Rect(0f, 0f, 1f, 1f), displayAspect);
            _camera.cullingMask = GameplayRenderLayers.GameplayCameraMask;

            if (SidebarCamera != null)
                SidebarCamera.enabled = false;
            if (HotbarCamera != null)
                HotbarCamera.enabled = false;
            if (PopupOverlayCamera != null)
                PopupOverlayCamera.enabled = false;
        }

        private void ConfigurePopupOverlayCamera(GameplayScreenLayout layout)
        {
            if (PopupOverlayCamera == null)
                return;

            PopupOverlayCamera.enabled = _centeredPopupOverlayActive && layout.GameplayRect.width > 0f;
            PopupOverlayCamera.cullingMask = GameplayRenderLayers.PopupOverlayMask;
            // URP overlay cameras composite over the base gameplay camera via camera stacking.
            PopupOverlayCamera.clearFlags = CameraClearFlags.Depth;
            PopupOverlayCamera.backgroundColor = Color.clear;
            CenteredPopupLayout.ConfigureOverlayCamera(
                PopupOverlayCamera,
                layout.MapRect,
                layout.MapAspect);
        }

        private void ConfigureSidebarCamera(GameplayScreenLayout layout)
        {
            if (SidebarCamera == null)
                return;

            SidebarCamera.enabled = layout.SidebarRect.width > 0f;
            SidebarCamera.orthographic = true;
            SidebarCamera.backgroundColor = Color.black;
            SidebarCamera.clearFlags = CameraClearFlags.SolidColor;
            SidebarCamera.cullingMask = GameplayRenderLayers.SidebarMask;
            SidebarCamera.transform.position = new Vector3(
                layout.SidebarWorldWidth * 0.5f,
                _camera.orthographicSize,
                SidebarCamera.transform.position.z);
            SidebarCamera.orthographicSize = _camera.orthographicSize;
            ConfigureCameraRect(SidebarCamera, layout.SidebarRect, layout.SidebarAspect);
        }

        private void ConfigureHotbarCamera(GameplayScreenLayout layout)
        {
            if (HotbarCamera == null)
                return;

            HotbarCamera.enabled = layout.HotbarRect.width > 0f && layout.HotbarRect.height > 0f;
            HotbarCamera.cullingMask = GameplayRenderLayers.HotbarMask;
            HotbarCamera.clearFlags = CameraClearFlags.SolidColor;
            HotbarCamera.backgroundColor = Color.black;
            GameplayHotbarLayout.ConfigureOverlayCamera(
                HotbarCamera,
                layout.HotbarRect,
                layout.HotbarAspect);
        }

        private static void ConfigureCameraRect(Camera camera, Rect rect, float aspect)
        {
            if (camera == null)
                return;

            camera.rect = rect;
            camera.aspect = Mathf.Max(0.01f, aspect);
        }
    }
}
