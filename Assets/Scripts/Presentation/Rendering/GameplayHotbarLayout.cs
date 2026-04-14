using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Shared layout math for the dedicated gameplay hotbar strip.
    /// </summary>
    public static class GameplayHotbarLayout
    {
        public const int GridWidth = 80;
        public const int GridHeight = 6;
        public const int SlotCount = 10;
        public const int SlotWidth = GridWidth / SlotCount;

        public static float ComputeCellWidth(float hotbarAspect)
        {
            return Mathf.Max(0.01f, GridHeight * Mathf.Max(0.01f, hotbarAspect) / GridWidth);
        }

        public static float ComputeWorldWidth(float hotbarAspect)
        {
            return GridWidth * ComputeCellWidth(hotbarAspect);
        }

        public static void ConfigureOverlayCamera(Camera camera, Rect hotbarRect, float hotbarAspect)
        {
            if (camera == null)
                return;

            float clampedAspect = Mathf.Max(0.01f, hotbarAspect);
            camera.rect = hotbarRect;
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

        public static bool TryGetSlotAtScreenPosition(Camera camera, Tilemap tilemap, Vector2 screenPosition, out int slot)
        {
            slot = -1;

            if (!ScreenToGrid(camera, tilemap, screenPosition, out int gridX, out _))
                return false;

            slot = Mathf.Clamp(gridX / SlotWidth, 0, SlotCount - 1);
            return true;
        }
    }
}
