using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Shared layout math for centered modal popups that render inside the
    /// gameplay column while the sidebar remains visible.
    /// </summary>
    public static class CenteredPopupLayout
    {
        public const int GridWidth = 80;
        public const int GridHeight = 45;

        public static RectInt GetCenteredRect(int popupWidth, int popupHeight)
        {
            int clampedWidth = Mathf.Clamp(popupWidth, 1, GridWidth);
            int clampedHeight = Mathf.Clamp(popupHeight, 1, GridHeight);
            int originX = Mathf.Max(0, (GridWidth - clampedWidth) / 2);
            int originY = Mathf.Max(0, (GridHeight - clampedHeight) / 2);
            return new RectInt(originX, originY, clampedWidth, clampedHeight);
        }

        public static int GetCenteredOriginX(int popupWidth)
        {
            return GetCenteredRect(popupWidth, 1).xMin;
        }

        public static int GetCenteredTopY(int popupHeight)
        {
            RectInt rect = GetCenteredRect(1, popupHeight);
            return rect.yMin + rect.height - 1;
        }

        public static float ComputeCellWidth(float gameplayAspect)
        {
            return Mathf.Max(0.01f, GridHeight * Mathf.Max(0.01f, gameplayAspect) / GridWidth);
        }

        public static float ComputeWorldWidth(float gameplayAspect)
        {
            return GridWidth * ComputeCellWidth(gameplayAspect);
        }

        public static void ConfigureOverlayCamera(Camera camera, Rect gameplayRect, float gameplayAspect)
        {
            if (camera == null)
                return;

            float clampedAspect = Mathf.Max(0.01f, gameplayAspect);
            camera.rect = gameplayRect;
            camera.aspect = clampedAspect;
            camera.orthographic = true;
            camera.orthographicSize = GridHeight * 0.5f;
            camera.transform.position = new Vector3(
                ComputeWorldWidth(clampedAspect) * 0.5f,
                GridHeight * 0.5f,
                camera.transform.position.z);
        }

        public static bool ScreenToGrid(Camera camera, Tilemap tilemap, Vector2 screenPosition, out int gridX, out int gridY)
        {
            gridX = -1;
            gridY = -1;

            if (camera == null || tilemap == null)
                return false;

            if (!camera.pixelRect.Contains(screenPosition))
                return false;

            Vector3 world = camera.ScreenToWorldPoint(new Vector3(
                screenPosition.x,
                screenPosition.y,
                -camera.transform.position.z));
            Vector3Int cell = tilemap.WorldToCell(world);
            if (cell.x < 0 || cell.x >= GridWidth || cell.y < 0 || cell.y >= GridHeight)
                return false;

            gridX = cell.x;
            gridY = cell.y;
            return true;
        }
    }
}
