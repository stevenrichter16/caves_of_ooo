using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Split-screen layout metrics for the gameplay map and persistent sidebar.
    /// </summary>
    public readonly struct GameplayScreenLayout
    {
        public GameplayScreenLayout(
            Rect gameplayRect,
            Rect sidebarRect,
            float displayAspect,
            float gameplayAspect,
            float sidebarAspect,
            float scale,
            float charWidthWorld,
            float sidebarWorldWidth)
        {
            GameplayRect = gameplayRect;
            SidebarRect = sidebarRect;
            DisplayAspect = displayAspect;
            GameplayAspect = gameplayAspect;
            SidebarAspect = sidebarAspect;
            Scale = scale;
            CharWidthWorld = charWidthWorld;
            SidebarWorldWidth = sidebarWorldWidth;
        }

        public Rect GameplayRect { get; }
        public Rect SidebarRect { get; }
        public float DisplayAspect { get; }
        public float GameplayAspect { get; }
        public float SidebarAspect { get; }
        public float Scale { get; }
        public float CharWidthWorld { get; }
        public float SidebarWorldWidth { get; }
    }

    /// <summary>
    /// Camera-space text layout metrics for the dedicated sidebar camera.
    /// </summary>
    public readonly struct SidebarCameraMetrics
    {
        public SidebarCameraMetrics(
            float scale,
            float charWidthWorld,
            int startCharX,
            int topTextY,
            int visibleRowCount)
        {
            Scale = scale;
            CharWidthWorld = charWidthWorld;
            StartCharX = startCharX;
            TopTextY = topTextY;
            VisibleRowCount = visibleRowCount;
        }

        public float Scale { get; }
        public float CharWidthWorld { get; }
        public int StartCharX { get; }
        public int TopTextY { get; }
        public int VisibleRowCount { get; }
        public int BottomTextY => TopTextY - (VisibleRowCount - 1);
    }

    public static class GameplayViewportLayout
    {
        public static GameplayScreenLayout Measure(Camera camera, float referenceZoom, int sidebarWidthChars)
        {
            if (camera == null)
                return default;

            return Measure(GetDisplayAspect(camera), camera.orthographicSize, referenceZoom, sidebarWidthChars);
        }

        public static GameplayScreenLayout Measure(
            float displayAspect,
            float orthographicSize,
            float referenceZoom,
            int sidebarWidthChars)
        {
            float clampedAspect = Mathf.Max(0.01f, displayAspect);
            float scale = orthographicSize / Mathf.Max(0.01f, referenceZoom);
            float charWidthWorld = 0.5f * scale;
            float sidebarWorldWidth = Mathf.Max(0, sidebarWidthChars) * charWidthWorld;
            float totalWorldWidth = orthographicSize * 2f * clampedAspect;
            float sidebarWidthFraction = sidebarWorldWidth <= 0f || totalWorldWidth <= 0f
                ? 0f
                : Mathf.Clamp01(sidebarWorldWidth / totalWorldWidth);

            Rect gameplayRect = new Rect(0f, 0f, 1f - sidebarWidthFraction, 1f);
            Rect sidebarRect = sidebarWidthFraction > 0f
                ? new Rect(gameplayRect.width, 0f, sidebarWidthFraction, 1f)
                : Rect.zero;

            float gameplayAspect = clampedAspect * gameplayRect.width;
            float sidebarAspect = clampedAspect * sidebarRect.width;

            return new GameplayScreenLayout(
                gameplayRect,
                sidebarRect,
                clampedAspect,
                Mathf.Max(0.01f, gameplayAspect),
                Mathf.Max(0.01f, sidebarAspect),
                scale,
                charWidthWorld,
                sidebarWorldWidth);
        }

        public static SidebarCameraMetrics MeasureSidebarCamera(Camera camera, float referenceZoom, int sidebarWidthChars)
        {
            if (camera == null)
                return default;

            float scale = camera.orthographicSize / Mathf.Max(0.01f, referenceZoom);
            float halfH = camera.orthographicSize;
            float halfW = halfH * camera.aspect;
            float worldLeft = camera.transform.position.x - halfW;
            float worldTop = camera.transform.position.y + halfH;
            float worldBottom = camera.transform.position.y - halfH;
            float charWidthWorld = 0.5f * scale;
            int startCharX = Mathf.RoundToInt(worldLeft / Mathf.Max(0.001f, charWidthWorld));
            int topTextY = Mathf.FloorToInt((worldTop - 0.5f * scale) / Mathf.Max(0.001f, scale));
            int visibleRowCount = Mathf.Max(1, Mathf.FloorToInt((worldTop - worldBottom) / Mathf.Max(0.001f, scale)));

            return new SidebarCameraMetrics(
                scale,
                charWidthWorld,
                startCharX,
                topTextY,
                visibleRowCount);
        }

        public static float GetDisplayAspect(Camera camera)
        {
            if (camera == null)
                return 16f / 9f;

            Rect rect = camera.rect;
            float rectWidth = Mathf.Max(0.0001f, rect.width);
            float rectHeight = Mathf.Max(0.0001f, rect.height);
            return Mathf.Max(0.01f, camera.aspect * rectHeight / rectWidth);
        }
    }

    internal static class GameplayRenderLayers
    {
        public const int DefaultLayer = 0;
        public const int WorldLayer = 8;
        public const int SidebarLayer = 9;
        public const int PopupOverlayLayer = 10;

        public static int DefaultMask => 1 << DefaultLayer;
        public static int WorldMask => 1 << WorldLayer;
        public static int SidebarMask => 1 << SidebarLayer;
        public static int PopupOverlayMask => 1 << PopupOverlayLayer;
        public static int GameplayCameraMask => WorldMask | DefaultMask;

        public static void SetLayerRecursive(GameObject gameObject, int layer)
        {
            if (gameObject == null)
                return;

            gameObject.layer = layer;

            Transform transform = gameObject.transform;
            for (int i = 0; i < transform.childCount; i++)
                SetLayerRecursive(transform.GetChild(i).gameObject, layer);
        }
    }
}
