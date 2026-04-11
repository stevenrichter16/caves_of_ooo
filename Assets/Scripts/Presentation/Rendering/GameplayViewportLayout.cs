using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Shared layout math for the gameplay viewport and persistent right sidebar.
    /// </summary>
    public struct GameplayViewportMetrics
    {
        public GameplayViewportMetrics(
            float scale,
            float worldLeft,
            float worldRight,
            float worldTop,
            float worldBottom,
            float sidebarWidthWorld,
            float gameplayRightWorld,
            float charWidthWorld,
            int sidebarStartCharX,
            int topTextY,
            int visibleRowCount)
        {
            Scale = scale;
            WorldLeft = worldLeft;
            WorldRight = worldRight;
            WorldTop = worldTop;
            WorldBottom = worldBottom;
            SidebarWidthWorld = sidebarWidthWorld;
            GameplayRightWorld = gameplayRightWorld;
            CharWidthWorld = charWidthWorld;
            SidebarStartCharX = sidebarStartCharX;
            TopTextY = topTextY;
            VisibleRowCount = visibleRowCount;
        }

        public float Scale { get; }
        public float WorldLeft { get; }
        public float WorldRight { get; }
        public float WorldTop { get; }
        public float WorldBottom { get; }
        public float SidebarWidthWorld { get; }
        public float GameplayRightWorld { get; }
        public float CharWidthWorld { get; }
        public int SidebarStartCharX { get; }
        public int TopTextY { get; }
        public int VisibleRowCount { get; }
        public int BottomTextY => TopTextY - (VisibleRowCount - 1);
    }

    public static class GameplayViewportLayout
    {
        public static GameplayViewportMetrics Measure(Camera camera, float referenceZoom, int sidebarWidthChars)
        {
            if (camera == null)
                return default;

            float scale = camera.orthographicSize / Mathf.Max(0.01f, referenceZoom);
            float halfH = camera.orthographicSize;
            float halfW = halfH * camera.aspect;
            float worldLeft = camera.transform.position.x - halfW;
            float worldRight = camera.transform.position.x + halfW;
            float worldTop = camera.transform.position.y + halfH;
            float worldBottom = camera.transform.position.y - halfH;
            float charWidthWorld = 0.5f * scale;
            float sidebarWidthWorld = Mathf.Max(0, sidebarWidthChars) * charWidthWorld;
            float gameplayRightWorld = worldRight - sidebarWidthWorld;
            int sidebarStartCharX = Mathf.FloorToInt(worldRight / Mathf.Max(0.001f, charWidthWorld)) - Mathf.Max(0, sidebarWidthChars);
            int topTextY = Mathf.FloorToInt((worldTop - 0.5f * scale) / Mathf.Max(0.001f, scale));
            int visibleRowCount = Mathf.Max(1, Mathf.FloorToInt((worldTop - worldBottom) / Mathf.Max(0.001f, scale)));

            return new GameplayViewportMetrics(
                scale,
                worldLeft,
                worldRight,
                worldTop,
                worldBottom,
                sidebarWidthWorld,
                gameplayRightWorld,
                charWidthWorld,
                sidebarStartCharX,
                topTextY,
                visibleRowCount);
        }

        public static float GetReservedRightWorldWidth(Camera camera, float referenceZoom, int sidebarWidthChars)
        {
            return Measure(camera, referenceZoom, sidebarWidthChars).SidebarWidthWorld;
        }
    }
}
